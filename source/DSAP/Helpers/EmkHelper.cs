using Archipelago.Core.Util;
using DSAP.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DSAP.Helpers
{
    internal class EmkHelper
    {
        static HashSet<Tuple<int, int>> PrevEvents = [];
        static ulong prevEventHeadPtr = ulong.MinValue;
        static ulong prevEventHead = ulong.MinValue;
        internal static void CheckEventsList()
        {
            HashSet<Tuple<int, int>> ExistingEvents = [];

            ulong eventhead_ptr = AddressHelper.GetEmkHeadAddress();
            int numevents = 0;
            if (eventhead_ptr != prevEventHeadPtr)
            {
                Log.Logger.Debug($"eventheadptr changed from {prevEventHeadPtr:X} to {eventhead_ptr:X}");
                prevEventHeadPtr = eventhead_ptr;
            }   
            if (eventhead_ptr != 0)
            {
                ulong eventhead = Memory.ReadULong((ulong)eventhead_ptr);
                if (eventhead != prevEventHead)
                    Log.Logger.Debug($"eventhead changed from {prevEventHead:X} to {eventhead:X}");
                prevEventHead = eventhead;

                // read every event into the hashset
                // detect number of differences => numevents
                for (ulong thisEmk = eventhead; thisEmk != 0; thisEmk = Memory.ReadULong((ulong)thisEmk + 0x68))
                {

                    numevents++;
                    if (numevents > 2000)
                    {
                        Log.Logger.Warning($"c events:{numevents}");
                        Log.Logger.Warning($"thisemk = :{thisEmk:X}");
                        break;
                    }

                    int eventid = Memory.ReadInt(thisEmk + 0x30);
                    int eventslot = Memory.ReadByte(thisEmk + 0x34);

                    ExistingEvents.Add(new Tuple<int, int>(eventid, eventslot));
                }
            }
            

            if (!ExistingEvents.SetEquals(PrevEvents))
            {
                bool printedfirst = false;
                int eventsRemoved = 0;
                Tuple<int, int> lastemk = new Tuple<int, int>(0, 0);
                foreach (Tuple<int, int> emk in PrevEvents)
                {
                    if (!ExistingEvents.Contains(emk))
                    {
                        eventsRemoved++;
                        if (!printedfirst)
                        {
                            Log.Logger.Debug($"(F) - {emk.Item1}:{emk.Item2}");
                            printedfirst = true;
                        }
                        lastemk = emk;
                    }
                }
                if (eventsRemoved > 1)
                {
                    Log.Logger.Debug($"(L) - {lastemk.Item1}:{lastemk.Item2}");
                }
                printedfirst = false;
                int eventsAdded = 0;
                lastemk = new Tuple<int, int>(0, 0);
                foreach (Tuple<int, int> emk in ExistingEvents)
                {
                    if (!PrevEvents.Contains(emk))
                    {
                        eventsAdded++;
                        if (!printedfirst)
                        {
                            Log.Logger.Debug($"(F) + {emk.Item1}:{emk.Item2}");
                            printedfirst = true;
                        }
                        lastemk = emk;
                    }
                }
                if (eventsAdded > 1)
                {
                    Log.Logger.Debug($"(L) + {lastemk.Item1}:{lastemk.Item2}");
                }
                Log.Logger.Debug($"Events from {PrevEvents.Count} to {ExistingEvents.Count} > - {eventsRemoved} + {eventsAdded}");

                PrevEvents = new HashSet<Tuple<int, int>>(ExistingEvents);
            }
        }
        static ulong emkHookStubArea = 0;
        static ulong emkHookStructAddr = 0;
        internal static byte[] BuildEventSkipStruct(List<EmkController> emkControllers)
        {

            byte[] newEmkHookStructBytes = new byte[emkControllers.Count * 8];
            for (int i = 0; i < emkControllers.Count; i++)
            {
                EmkController emkc = emkControllers[i];
                int slot_and_id = emkc.Eventid + emkc.Eventslot;
                int active = emkc.HasKey ? 1 : 0;
                Array.Copy(BitConverter.GetBytes(slot_and_id), 0, newEmkHookStructBytes, 8 * i, sizeof(int));
                Array.Copy(BitConverter.GetBytes(active), 0, newEmkHookStructBytes, 8 * i + 4, sizeof(int));
            }
            return newEmkHookStructBytes;
        }
        // update it in place
        internal static void UpdateEventSkipStruct(List<EmkController> emkControllers)
        {
            var locked_emkControllers = emkControllers.Where(x => !x.HasKey);
            byte[] emkHookStructBytes = BuildEventSkipStruct(emkControllers);

            ulong old_addr = emkHookStructAddr;
            emkHookStructAddr = (ulong)Memory.Allocate((uint)Math.Max(1000, emkHookStructBytes.Length), Memory.PAGE_READWRITE);
            Memory.WriteByteArray(emkHookStructAddr, emkHookStructBytes); // write the structure into the allocated memory.

            // free the old area
            if (old_addr != 0)
            {
                Task.Run(() =>
                {
                    Task.Delay(200);
                    Memory.FreeMemory((nint)old_addr);
                });
            }

            byte[] emkHookStubBytes = new byte[12];
            Array.Copy(BitConverter.GetBytes(emkControllers.Count), emkHookStubBytes, sizeof(int));
            Array.Copy(BitConverter.GetBytes(emkHookStructAddr), 0, emkHookStubBytes, 4, sizeof(ulong));

            Memory.WriteByteArray(emkHookStubArea, emkHookStubBytes);

            return;
        }
        internal static void AddEventSkipHook(List<EmkController> emkControllers)
        {
            emkHookStubArea = (ulong)Memory.Allocate(1000, Memory.PAGE_READWRITE);

            UpdateEventSkipStruct(emkControllers);

            // now hook emk func 1 (process events) to ignore these slots.
            const ulong eventHook1_loc = 0x1407d40b0;
            const int eventHook1_length = 15;

            // logic:
            // 1. backup registers
            // 2. get stub area, then get count and pointer from it
            // 3. get event idslot number (add id and slot)
            // 4. loop over all slots in struct, to find the slot
            // 5. if it is found, check its "unlocked" value (currently unused, always "locked")
            // 6. if it is locked, restore registers and return to caller (e.g. skip processing the hooked function entirely)
            // 7. if it's unlocked or wasn't found, process the hooked function as normal. Restore registers, run the replaced instructions, then jmp back (jmp inserted by addhook call).

            // push rax
            // push rbx
            // push rcx
            // push rdx
            // push r8
            //
            // movabs      rdx,0x0102030405060708
            // mov    eax,DWORD PTR [rdx]  
            // mov    rbx,QWORD PTR [rdx+0x4]
            // mov edx, 0
            // mov dx, word ptr[rcx+0x34]
            // mov ecx, dword ptr[rcx+0x30]
            // add ecx, edx
            //
            // Loopstart:
            //
            // cmp ecx,dword ptr[rbx]
            // je FoundMatch
            // add rbx,8
            // dec eax
            // jnz Loopstart
            // jmp NotLocked
            //
            //
            // FoundMatch:
            // mov edx,dword ptr[rbx+4]
            // test edx,edx
            // jnz NotLocked
            // pop r8
            // pop rdx
            // pop rcx
            // pop rbx
            // pop rax
            // ret
            //
            // NotLocked:
            // pop r8
            // pop rdx
            // pop rcx
            // pop rbx
            // pop rax
            //
            // Return:
            // mov qword ptr[rsp+0x8],rbx
            // push rdi
            // sub rsp,0x30
            // movaps xmmword ptr [RSP + 0x20],XMM6
            // ; jmp back gets inserted here
            var eventHook1_bytes = new byte[]
            {
                0x50, 0x53, 0x51, 0x52, 0x41, 0x50, 0x48, 0xBA, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x8B, 0x02, 0x48, 0x8B, 0x5A, 0x04, 0xBA, 0x00, 0x00, 0x00, 0x00, 0x66, 0x8B, 0x51, 0x34, 0x8B, 0x49, 0x30, 0x01, 0xD1, 0x3B, 0x0B, 0x74, 0x0A, 0x48, 0x83, 0xC3, 0x08, 0xFF, 0xC8, 0x75, 0xF4, 0xEB, 0x0E, 0x8B, 0x53, 0x04, 0x85, 0xD2, 0x75, 0x07, 0x41, 0x58, 0x5A, 0x59, 0x5B, 0x58, 0xC3, 0x41, 0x58, 0x5A, 0x59, 0x5B, 0x58, 0x48, 0x89, 0x5C, 0x24, 0x08, 0x57, 0x48, 0x83, 0xEC, 0x30, 0x0F, 0x29, 0x74, 0x24, 0x20
            };

            // replace +8 with (ulong)stub
            Array.Copy(BitConverter.GetBytes(emkHookStubArea), 0, eventHook1_bytes, 8, sizeof(ulong));

            HookHelper.AddHook(eventHook1_loc, eventHook1_length, eventHook1_bytes, false);


        }
        static uint cached_mapid3;
        internal static void ManageEventsList(List<EmkController> emkControllers)
        {
            try
            {
                Log.Logger.Verbose($"running eventlist for {emkControllers.Count} emks");

                if (emkControllers.Count == 0)
                {
                    return;
                }
                if (!MiscHelper.IsInGame())
                    return;
                Dictionary<Tuple<int, int>, EmkController> emkdict = [];
                List<EmkController> addingEmks = [];

                foreach (EmkController emk in emkControllers)
                {
                    /* If player doesn't have key, or we've "saved ptr", examine it in list */
                    if (!emk.HasKey || emk.Saved_Ptr != 0)
                    {
                        emkdict[new Tuple<int, int>(emk.Eventid, emk.Eventslot)] = emk;
                    }
                }

                MapInfo mapInfo = MapHelper.GetMapAndXyzCoords();

                if (mapInfo.MapId3 != cached_mapid3)
                    Log.Logger.Verbose($"mapid={mapInfo.MapId3}, w={mapInfo.World}, a={mapInfo.Area}");
                cached_mapid3 = mapInfo.MapId3;

                ulong eventhead_ptr = AddressHelper.GetEmkHeadAddress();
                if (eventhead_ptr == 0)
                {
                    Log.Logger.Verbose($"eventheadptr is null");
                    ReleaseEvents(emkControllers);
                    return;
                }
                ulong eventhead = Memory.ReadULong((ulong)eventhead_ptr);
                if (eventhead == 0)
                {
                    Log.Logger.Verbose($"eventhead is null");
                    ReleaseEvents(emkControllers);
                    return;
                }

                if (emkdict.Count != 0)
                {
                    //Log.Logger.Information("Emks found, Managing event list");

                    // prevptr is address of a ptr to the "current emk"
                    // When "current emk" is pulled off list, it 
                    ulong prevptr = eventhead_ptr;
                    int numevents = 0;
                    // check every event for if it is in the list
                    for (ulong thisEmk = eventhead; thisEmk != 0; thisEmk = Memory.ReadULong((ulong)thisEmk + 0x68))
                    {
                        bool updatedEmk = true;
                        while (thisEmk != 0 && updatedEmk) // loop as long as we are pulling off events
                        {
                            updatedEmk = false;
                            numevents++;
                            if (numevents > 2000) // sanity check
                            {
                                Log.Logger.Warning($"m events:{numevents}");
                                Log.Logger.Warning($"thisemk = :{thisEmk:X}");
                                break;
                            }

                            int eventid = Memory.ReadInt(thisEmk + 0x30);
                            int eventslot = Memory.ReadByte(thisEmk + 0x34);
                            var t = new Tuple<int, int>(eventid, eventslot);
                            if (emkdict.ContainsKey(t))
                            {
                                EmkController emk = emkdict[t];
                                if (!emk.HasKey) /* Player doesn't have key -> pull it */
                                {
                                    // Only pull it if we're in the relevant map. This is to do less "pulls" in general!
                                    if (emk.MapId3 == mapInfo.MapId3) /* Compare current mapid to event's valid mapid */
                                    {
                                        ulong nextptr = Memory.ReadULong(thisEmk + 0x68);
                                        Memory.Write(prevptr, nextptr);
                                        emk.Saved_Ptr = thisEmk;
                                        emk.Deactivated = true;
                                        thisEmk = nextptr;
                                        updatedEmk = true;
                                        Log.Logger.Debug($"Pulled event: {emk.Name} at {emk.Saved_Ptr:X}");
                                    }
                                    //else
                                    //    Log.Logger.Debug($"wrong map to pull event {emk.Name}, mapid3={mapInfo.MapId3}, emkmapid3={emk.MapId3}");
                                }
                                else /* Player has event's key, but we found it in list? Destroy our "old" version, and stop interfering. */
                                {
                                    emk.Saved_Ptr = 0;
                                    emk.Deactivated = false;
                                    Log.Logger.Debug($"Un-pulled event: {emk.Name} at {emk.Saved_Ptr:X}");
                                }
                            }
                        }
                        if (thisEmk == 0) // reached end of list
                            break;
                        prevptr = thisEmk + 0x68; // save address of previous node's last spot when we move on.
                    }
                }

                foreach (EmkController emk in emkControllers)
                {
                    /* If we have a saved ptr that we need to re-insert */
                    if (emk.HasKey && emk.Saved_Ptr != 0)
                    {
                        /* If we're in the map for the event */
                        if (emk.MapId3 == mapInfo.MapId3) /* Compare current mapid to event's valid mapid */
                        {
                            addingEmks.Add(emk);
                            Log.Logger.Debug($"Re-adding event: {emk.Name} at {emk.Saved_Ptr:X}");
                        }
                    }
                }

                if (addingEmks.Count > 0)
                {
                    var firstEmk = addingEmks.First();
                    var lastEmk = addingEmks.Last();

                    if (addingEmks.Count > 1)
                    {
                        // point our saved events each to the next one in sequence, creating a "saved event sequence"
                        for (var i = 0; i < addingEmks.Count - 1; i++)
                        {
                            Memory.Write(addingEmks[i].Saved_Ptr + 0x68, addingEmks[i + 1].Saved_Ptr);
                        }
                    }
                    eventhead_ptr = AddressHelper.GetEmkHeadAddress();
                    if (eventhead_ptr == 0)
                    {
                        Log.Logger.Warning($"eventheadptr is null");
                        ReleaseEvents(emkControllers);
                        return;
                    }
                    eventhead = Memory.ReadULong((ulong)eventhead_ptr);
                    if (eventhead == 0)
                    {
                        Log.Logger.Warning($"eventhead is null");
                        ReleaseEvents(emkControllers);
                        return;
                    }
                    // point last saved event to event head
                    Memory.Write(lastEmk.Saved_Ptr + 0x68, eventhead);
                    // make our first saved event the head of the event list.
                    Memory.Write(eventhead_ptr, firstEmk.Saved_Ptr);

                    // clear all the event saved ptrs
                    foreach (var emk in addingEmks)
                    {
                        emk.Deactivated = false;
                        emk.Saved_Ptr = 0;
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Exception in manageevents: {ex.Message}\n{ex.InnerException}\n{ex.Source}");
            }
        }
        // Build a list of event controllers, which we use to lock events until player has received the items.
        // We only add events to the list if their items are in the multiworld.
        internal static List<EmkController> BuildEmkControllers(Dictionary<string, object> slotData)
        {
            List<EmkController> result = [];

            var events = MiscHelper.GetDsrEventEmks();
            var locs = App.Client.CurrentSession.Locations.AllLocations;
            foreach (var newemk in events)
            {
                if (locs.Contains(newemk.ApLocId) && newemk.Type != Enums.DsrEventType.EARLYFOGWALL)
                {
                    Log.Logger.Verbose($"Adding {newemk.Name} to list. Id:slot={newemk.Eventid}:{newemk.Eventslot}");
                    result.Add(newemk);
                }
            }

            // block event 706 for GH/DA warp safety
            {
                var newemk = new EmkController("Warping", "none", Enums.DsrEventType.GENERIC, 706, 0, 0, 0);
                newemk.MapId3 = 170;
                result.Add(newemk);
            }

            return result;
        }
        // Clear the saved ptrs of our list of "EmkControllers", because we detected there being no events in the list.
        public static void ReleaseEvents(List<EmkController> emkControllers)
        {
            int num_released = 0;
            foreach (var controller in emkControllers)
            {
                if (controller.Saved_Ptr != 0 || controller.Deactivated == true)
                {
                    controller.Saved_Ptr = 0;
                    controller.Deactivated = false;
                }
            }
            if (num_released > 0)
                Log.Logger.Debug($"Released all emks, {num_released} controllers affected.");
        }
    }
}
