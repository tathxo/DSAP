using Archipelago.Core;
using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using DSAP.Models;
using DSAP.ViewModels;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DSAP.Enums;

namespace DSAP.Helpers
{
    public class BonfireInjectorHelper
    {
        // probably builds the list of entries
        public const ulong hook1_loc = 0x1406ed2d6;
        public const int hook1_length = 14;

        // also build the list?
        public const ulong hook2_loc = 0x1406ed7c6;
        public const int hook2_length = 20;

        // probably set "this bonfire's flag"
        public const ulong hook3_loc = 0x1406ed300;
        public const int hook3_length = 16;

        // overwrite "bonfire warp" routine for the "new" structures
        public const ulong hook4_loc = 0x1404aaf38;
        public const int hook4_length = 15;
        // overwrite "detect if we have to show the warp menu" with new structures
        public const ulong hook5_loc = 0x1406ed276;
        public const int hook5_length = 16;

        private static bool hooks_set = false;
        private static ulong bonfire_stub_area = 0;

        private static readonly object _memAllocLock = new object();
        public static void ClearHookArea()
        {
            hooks_set = false;
            bonfire_stub_area = 0;
        }
        public static async void UpdateBonfiresStruct(bool vanillaWarpNames, string warpSortOrder)
        {
            if (hooks_set && bonfire_stub_area != 0 && App.DSOptions != null)
            {
                Log.Logger.Information($"Updating bonfires to have {(vanillaWarpNames ? "vanilla" : "non-vanilla")} warp names and sort order to {warpSortOrder}");
                ulong old_bonfire_struct = Memory.ReadULong(bonfire_stub_area); // read the address of the structure

                var new_bonfire_struct = MiscHelper.GetBonfireDsrStruct(App.ControlsContext.VanillaWarpNames, App.ControlsContext.WarpSortOrder);
                Log.Logger.Warning($"bonfire struct size={new_bonfire_struct.Count}");

                // write it to a byte array
                var new_bonfire_bytes = new byte[new_bonfire_struct.Count * 12 + 4];
                for (int i = 0; i < new_bonfire_struct.Count; i++)
                {
                    var bonfire = new_bonfire_struct[i];
                    Array.Copy(BitConverter.GetBytes(bonfire.Item1), 0, new_bonfire_bytes, i * 12 + 0, 4);
                    Array.Copy(BitConverter.GetBytes(bonfire.Item2), 0, new_bonfire_bytes, i * 12 + 4, 4);
                    Array.Copy(BitConverter.GetBytes(bonfire.Item3), 0, new_bonfire_bytes, i * 12 + 8, 4);
                }
                Array.Copy(BitConverter.GetBytes(-1), 0, new_bonfire_bytes, new_bonfire_bytes.Length - 4, 4);

                // allocate a new area and write to it
                ulong new_bonfire_struct_pos = (ulong)Memory.Allocate((uint)(new_bonfire_struct.Count * 12 + 8), Memory.PAGE_EXECUTE_READWRITE);
                Memory.WriteByteArray(new_bonfire_struct_pos, new_bonfire_bytes);

                // update the bonfire stub area, which was already allocated previously.
                byte[] bonfire_stub_bytes = new byte[8 + 8 + 8 + 4];
                Array.Copy(BitConverter.GetBytes(new_bonfire_struct_pos), 0, bonfire_stub_bytes, 0, sizeof(long));
                Array.Copy(BitConverter.GetBytes(new_bonfire_struct_pos + 4), 0, bonfire_stub_bytes, 8, sizeof(long));
                Array.Copy(BitConverter.GetBytes(new_bonfire_struct_pos + (ulong)new_bonfire_bytes.Length), 0, bonfire_stub_bytes, 16, sizeof(long));
                Array.Copy(BitConverter.GetBytes(new_bonfire_struct.Count), 0, bonfire_stub_bytes, 24, sizeof(int));
                Memory.WriteByteArray(bonfire_stub_area, bonfire_stub_bytes);

                Memory.FreeMemory((nint)old_bonfire_struct); // free old struct
            }
        }
        /// <summary>
        /// Build a new bonfire area structure and stub zone, and insert hooks
        /// </summary>
        /// <returns></returns>
        internal static async Task UpdateBonfires()
        {
            // if never saved, set defaults
            {
                if (!App.DSOptions.WarpToAllBonfires)
                {
                    App.ControlsContext.VanillaWarpNames = true;
                    App.ControlsContext.WarpSortOrder = "Vanilla";
                }
                else
                {
                    App.ControlsContext.VanillaWarpNames = false;
                    App.ControlsContext.WarpSortOrder = "Alphabetical";
                }
            }
            var new_bonfire_struct = MiscHelper.GetBonfireDsrStruct(App.ControlsContext.VanillaWarpNames, App.ControlsContext.WarpSortOrder);
            Log.Logger.Debug($"bonfire struct size={new_bonfire_struct.Count}");

            // vanilla looks like:
            //    200, 1021960, 2000,
            //    205, 1011961, 2001,
            //    203, 1511960, 2002,
            //    207, 1511950, 2003,
            //    208, 1511962, 2004,
            //    204, 1601950, 2005,
            //    202, 1401960, 2006,
            //    206, 1311950, 2007,
            //    201, 1321960, 2008,
            //    209, 1211963, 2009,
            //    210, 1211961, 2010,
            //    211, 1211962, 2011,
            //    212, 1211950, 2012,
            //    213, 1211964, 2013,
            //    214, 1001960, 2014,
            //    215, 1011964, 2015,
            //    216, 1101960, 2016,
            //    217, 1311961, 2017,
            //    218, 1701960, 2018,
            //    219, 1701950, 2019,
            //    220, 1301962, 2020
            //};

            // write it to a byte array
            var new_bonfire_bytes = new byte[new_bonfire_struct.Count * 12 + 4];
            for (int i=0; i < new_bonfire_struct.Count; i++)
            {
                var bonfire = new_bonfire_struct[i];
                Array.Copy(BitConverter.GetBytes(bonfire.Item1), 0, new_bonfire_bytes, i * 12 + 0, 4);
                Array.Copy(BitConverter.GetBytes(bonfire.Item2), 0, new_bonfire_bytes, i * 12 + 4, 4);
                Array.Copy(BitConverter.GetBytes(bonfire.Item3), 0, new_bonfire_bytes, i * 12 + 8, 4);
            }
            Array.Copy(BitConverter.GetBytes(-1), 0, new_bonfire_bytes, new_bonfire_bytes.Length - 4, 4);

            // allocate a new area and write to it
            ulong new_bonfire_struct_pos = (ulong)Memory.Allocate((uint)(new_bonfire_struct.Count * 12 + 8), Memory.PAGE_EXECUTE_READWRITE);
            Memory.WriteByteArray(new_bonfire_struct_pos, new_bonfire_bytes);

            // allocate a new area and write to it. This will store the meta information for the structure; we can just update this when it needs to change.
            bonfire_stub_area = (ulong)Memory.Allocate(8 + 8 + 8 + 4); // 3 pointers and 1 integer
            byte[] bonfire_stub_bytes = new byte[8 + 8 + 8 + 4];
            Array.Copy(BitConverter.GetBytes(new_bonfire_struct_pos), 0, bonfire_stub_bytes, 0, sizeof(long));
            Array.Copy(BitConverter.GetBytes(new_bonfire_struct_pos+4), 0, bonfire_stub_bytes, 8, sizeof(long));
            Array.Copy(BitConverter.GetBytes(new_bonfire_struct_pos + (ulong)new_bonfire_bytes.Length), 0, bonfire_stub_bytes, 16, sizeof(long));
            Array.Copy(BitConverter.GetBytes(new_bonfire_struct.Count), 0, bonfire_stub_bytes, 24, sizeof(int));
            Memory.WriteByteArray(bonfire_stub_area, bonfire_stub_bytes);



            // build hook1
            var new_instructions_1 = new byte[]
            {
                0x49, 0xb8,                                     //movabs      r8,[0x0102030405060708] <- overwrite pointer to stub area
                0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,  
                0x49, 0x8b, 0x40, 0x08,                         //mov    rax,QWORD PTR [r8+0x8]  (struct start + 4)
                0x4d, 0x8b, 0x40, 0x10,                         //mov    r8,QWORD PTR [r8+0x10]  (end)
            };

            // replace +2 with (ulong)stub
            Array.Copy(BitConverter.GetBytes(bonfire_stub_area), 0, new_instructions_1, 2, sizeof(ulong));

            // build hook2
            var new_instructions_2 = new byte[]
            {
                0x48, 0xbf,                                  //mov rdi,0x0102030405060708 (stub)
                    0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                0x48, 0x8b, 0x5f, 0x08,                      //mov    rbx,QWORD PTR [r8+0x8]  (struct start + 4)
                0x44, 0x8b, 0x77, 0x18,                      //mov r14d, DWORD PTR[r8 + 0x18] (count)
                0x48, 0x8b, 0x3f,                            //mov rdi, QWORD PTR[r8]         (start)
            };

            // replace +2 with (ulong)stub
            Array.Copy(BitConverter.GetBytes(bonfire_stub_area), 0, new_instructions_2, 2, sizeof(ulong));

            // build hook3
            var new_instructions_3 = new byte[]
            {
                0x48, 0x89, 0x7c, 0x24, 0x20,                // first replaced instruction (stores rdi onto stack)
                0x48, 0x8d, 0x1c, 0x40,                      // 3rd replaced instruction - set rbx based on rax
                0x48, 0xbf,                                  //mov rdi,0x0102030405060708 (stub)
                    0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                0x48, 0x8b, 0x3f,                            //mov    rdi,QWORD PTR [rdi]
            };

            // replace +11 with (ulong)stub
            Array.Copy(BitConverter.GetBytes(bonfire_stub_area), 0, new_instructions_3, 11, sizeof(ulong));

            // build hook4
            //Push rcx
            //Push rax
            //Push rdx
            //Push r14
            //MOV RAX, QWORD PTR[0x0102030405060708]
            //add rdx, 1980
            //mov    DWORD PTR [rax],edx
            //mov edx,0x1
            //movabs r14,0x1404867e0
            //sub rsp,0x38
            //call r14
            // add rsp,0x38
            //Pop r14
            //Pop rdx
            //POP RAX
            //POP RCX
            var new_instructions_4 = new byte[]
            {
                0x51,                      					 // push   rcx
                0x50,                      					 // push   rax
                0x52,                      					 // push   rdx
                0x41, 0x56,                   				 // push   r14
                0x48, 0xa1, 0x08, 0x07, 0x06, 0x05, 0x04,    // movabs rax,ds:0x102030405060708
                0x03, 0x02, 0x01,
                0x48, 0x05, 0x34, 0x0b, 0x00, 0x00,          // add    rax,0xb34
                //0x50,                                        // push rax -> backup "target bonfire address"
                //0x8b, 0x38,                                  // mov edi,DWORD PTR [rax]  
                //0x57,                                        // push rdi -> backup old bonfire address
                0x48, 0x81, 0xc2, 0xbc, 0x07, 0x00, 0x00,    // add    rdx,0x7bc / 1980
                0x89, 0x10,                                  // mov    DWORD PTR [rax],edx
                0xba, 0x01, 0x00, 0x00, 0x00,          		 // mov    edx,0x1
                0x49, 0xbe, 0xe0, 0x67, 0x48, 0x40, 0x01,    // movabs r14,0x1404867e0
                0x00, 0x00, 0x00,
                0x48, 0x83, 0xec, 0x38,             		 // sub    rsp,0x38
                0x41, 0xff, 0xd6,                		     // call   r14
                0x48, 0x83, 0xc4, 0x38,           		     // add    rsp,0x38
                //0x5f,                                        // pop    rdi
                //0x58,                      					 // pop    rax
                //0x89, 0x38,                                  // mov    DWORD PTR [rax],edi
                0x41, 0x5e,                   				 // pop    r14
                0x5a,                      					 // pop    rdx
                0x58,                      					 // pop    rax
                0x59,                      					 // pop    rcx
                0xc3,                                        // ret
            };

            // replace +7 with (int) basec ptr
            ulong basecPtr = AddressHelper.GetBaseCOffset();
            Array.Copy(BitConverter.GetBytes(basecPtr), 0, new_instructions_4, 7, sizeof(ulong));

            // build hook 5
            var new_instructions_5 = new byte[]
            {
                0x48, 0xbd,                                     // movabs      rbp,[0x0102030405060708] (stub)
                0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                0x48, 0x8b, 0x5d, 0x08,                         // mov rbx, QWORD PTR[rbp + 0x8]        (struct+4)
                0x48, 0x8b, 0x6d, 0x10,                         // mov rbp, QWORD PTR[rbp + 0x10]       (end)
                0x33, 0xff,                                     // XOR        EDI,EDI. machine code could also be 0x31ff instead, apparently
            };

            // replace +2 with (ulong)stub
            Array.Copy(BitConverter.GetBytes(bonfire_stub_area), 0, new_instructions_5, 2, sizeof(ulong));

            AddHook(hook1_loc, hook1_length, new_instructions_1, false);
            AddHook(hook2_loc, hook2_length, new_instructions_2, false);
            
            AddHook(hook3_loc, hook3_length, new_instructions_3, false);
            // only hook "warp" routine if player has setting on. Otherwise, leave vanilla behavior, so "last bonfire" stays as source of warp
            if (App.DSOptions.WarpToAllBonfires)
                AddHook(hook4_loc, hook4_length, new_instructions_4, false);
            AddHook(hook5_loc, hook5_length, new_instructions_5, false);

            // update messages
            bool updateRequired = MsgManHelper.ReadMsgManStruct(out var msgManStruct, MsgManStruct.OFFSET_BONFIRES, x => x.MsgEntries.Any(x => x.id >= 99999998));
            //msgManStruct.PrintAllFmgs();
            if (updateRequired)
            {
                foreach (var bonfire in App.AllowedBonfireWarps)
                {
                    if (bonfire.VanillaFmgId > 2020) // if it's after "last" vanilla bonfire message
                    {
                        msgManStruct.AddMsg((uint)bonfire.VanillaFmgId, bonfire.VanillaName); // add "vanilla" names for non-vanilla bonfires option
                    }
                    msgManStruct.AddMsg((uint)bonfire.UpdatedFmgId, bonfire.Name); // add names for "non-vanilla" names option
                }

                msgManStruct.AddMsg(99999998, ""); // add dummy message to mark that we've been here
                msgManStruct.MsgEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
                MsgManHelper.WriteFromMsgManStruct(msgManStruct, MsgManStruct.OFFSET_BONFIRES);
                Log.Logger.Information("Updated Bonfire text struct");
            }
            hooks_set = true;
        }

        // creates a hook at loc_start
        // replaces loc_Start with a jmp to the given new_instructions array of bytes
        // Adds replaced_length bytes of instructions from loc_start before the new_instructions, and a jmp back.
        //  -> This means that the first jmp's code only will be actually processed, because a 2nd inserted jmp would simply jmp to the first one, which returns to the original position
        private static void AddHook(ulong loc_start, int replaced_length, byte[] new_instructions, bool include_replaced_bytes)
        {
            byte[] replaced_instructions = Memory.ReadByteArray(loc_start, replaced_length);
            ulong replacement_func_start_addr = (ulong)Memory.Allocate(1000, Memory.PAGE_EXECUTE_READWRITE);

            var jmpstub = new byte[]
            {
                0xff, 0x25, 0x00, 0x00, 0x00, 0x00,       //jmp    QWORD PTR [rip+0x0]        # 6 <_main+0x6>
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // target address
                // then the address to jump to (8 bytes)
            };
            Array.Copy(BitConverter.GetBytes(replacement_func_start_addr), 0, jmpstub, 6, 8); // target address

            var return_jmp = new byte[]
            {
                0xff, 0x25, 0x00, 0x00, 0x00, 0x00,          // jmp    QWORD PTR [rip+0x8]
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // jmp's target address
            };

            ulong next_write_pos = replacement_func_start_addr;
            // write the replaced instructions if the bool is on
            if (include_replaced_bytes)
            {
                Memory.WriteByteArray(next_write_pos, replaced_instructions); // write the replaced instructions
                next_write_pos += (ulong)replaced_instructions.Length;
            }

            Memory.WriteByteArray(next_write_pos, new_instructions); // write new instructions into its hook area
            next_write_pos += (ulong)new_instructions.Length;

            Memory.WriteByteArray(next_write_pos, return_jmp); // write the return instruction
            next_write_pos += (ulong)6; // point to return address
            Memory.WriteByteArray(next_write_pos, BitConverter.GetBytes(loc_start + (ulong)replaced_length)); // write the return address
            
            //Memory.WriteByteArray(replacement_func_start_addr + new_instructions.Length, return_jmp);
            Memory.WriteByteArray(loc_start, jmpstub); // write jmp stub (e.g. "create hook")
        }

        public static void setBonfireByLoc(int locid)
        {
            List<BonfireWarp> bonfirelocs = App.AllowedBonfireWarps;
            BonfireWarp bonfire = bonfirelocs.Find(x => x.Id == locid);
            if (bonfire != null)
            {
                if (((ulong)(long)(App.Client.CurrentSession.DataStorage[Scope.Slot, "Bonfires"]) & ((ulong) 1 << (bonfire.PersistId -1))) == 0)
                {
                    Log.Logger.Information($"Turning on bit: {bonfire.PersistId - 1}, {bonfire.Name}");
                    currentBonfiresInfo |= (long)1 << (bonfire.PersistId - 1);
                    App.Client.CurrentSession.DataStorage[Scope.Slot, "Bonfires"] += Bitwise.Or((long)1 << (bonfire.PersistId - 1));
                    givePlayerLordvesselFlag();
                }
            }
        }

        public static void givePlayerLordvesselFlag()
        {
            // Then, if player has the option to always have warping available turned on, and hasn't unlocked warping, unlock it with a cheeky message change
            if (App.DSOptions.CanWarpWithoutLordvessel)
            {
                var canwarp_eventflag = 710; // 710 is the lordvessel warp flag. Future: maybe turn on 717 (emergency warp) instead, until player has lordvessel, to prevent Frampt nomming & Ingward granting Key To the Seal?
                var baseAddress = AddressHelper.GetEventFlagsOffset();
                var canwarp_address = baseAddress + AddressHelper.GetEventFlagOffset(canwarp_eventflag).Item1;
                var canwarp_bit = AddressHelper.GetEventFlagOffset(canwarp_eventflag).Item2;
                if (!Memory.ReadBit(canwarp_address, canwarp_bit)) // if it's not already set
                {
                    // change "By the power of the Lordvessel, [etc]" -> "By the power of Archipelago"
                    MsgManHelper.ReadMsgManStruct(out var msgManStruct, MsgManStruct.OFFSET_BANNERS, x => false);
                    msgManStruct.UpdateMsg(10010620, "By the power of the multiworld, you may now warp between bonfires");
                    MsgManHelper.WriteFromMsgManStruct(msgManStruct, MsgManStruct.OFFSET_BANNERS);

                    // unlock warping
                    Memory.WriteBit(canwarp_address, canwarp_bit, true);
                }
            }
        }

        /// <summary>
        /// Start tracking bonfire list status
        /// </summary>
        /// <returns></returns>
        public static void TrackLitBonfiresAsync()
        {

            ArchipelagoClient Client = App.Client;
            Client.CurrentSession.DataStorage[Scope.Slot, "Bonfires"].Initialize(0);

            Client.CurrentSession.DataStorage[Scope.Slot, "Bonfires"].OnValueChanged -= UpdateBonfiresFromServer;
            Client.CurrentSession.DataStorage[Scope.Slot, "Bonfires"].OnValueChanged += UpdateBonfiresFromServer;


            //string storageKey = $"bonfires_{Client.CurrentSession.ConnectionInfo.Team}_{Client.CurrentSession.ConnectionInfo.Slot}";
        }
        internal static async void ResetKnownBonfires()
        {
            ctsource.Cancel();
            currentBonfiresInfo = 0;
            while (!MiscHelper.IsInGame() || !App.SaveidSet) // player is not in game
            {
                await Task.Delay(TimeSpan.FromSeconds(1)); // wait a second between checks
            }

            // only called once in game
            foreach (var bonfire in App.AllowedBonfireWarps)
            {
                if (App.CheckEventFlag(bonfire.Flag) == 1)
                {
                    currentBonfiresInfo |= (long)1 << (bonfire.PersistId - 1);
                }
            }
            // if current list doesn't match data storage, update data storage
            if (App.Client?.CurrentSession?.DataStorage[Scope.Slot, "Bonfires"] != null)
            {
                if (App.Client.CurrentSession.DataStorage[Scope.Slot, "Bonfires"] != currentBonfiresInfo)
                {
                    App.Client.CurrentSession.DataStorage[Scope.Slot, "Bonfires"] += Bitwise.Or(currentBonfiresInfo);
                }
            }
        }

        internal static CancellationTokenSource ctsource = new CancellationTokenSource();
        static long currentBonfiresInfo = 0;
        static DateTime LatestUpdate = DateTime.MinValue;
        private static async void UpdateBonfiresFromServer(JToken originalValue, JToken newValue, Dictionary<string, JToken> additionalArguments)
        {
            CancellationToken ctoken = ctsource.Token;
            // also pass in time of creation?
            var createdTime = DateTime.Now;
            LatestUpdate = createdTime;
            await Task.Run(async () =>
            {
                if ((long)newValue != currentBonfiresInfo) // if bonfire list has changed
                {
                    Log.Logger.Information($"Updating from server, {currentBonfiresInfo} to {(long)newValue | currentBonfiresInfo}");
                    List<BonfireWarp> bonfirelocs = App.AllowedBonfireWarps;
                    Dictionary<int, BonfireWarp> bonfiremap = bonfirelocs.ToDictionary(x => x.PersistId, x => x);
                    var saved_conninfo = App.Client.CurrentSession.ConnectionInfo;

                    while (!MiscHelper.IsInGame() || !App.SaveidSet) // player is not in game
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)); // wait a second between checks
                        
                        if (LatestUpdate > createdTime) // a later task like this exists - Skip this one.
                            return;
                        if (ctoken.IsCancellationRequested)
                            return;
                    }

                    // player is now in game, in a valid save, on the same connection.
                    long difference = ((long)newValue) & (currentBonfiresInfo ^ 0x7fffffffffffffff); // get all flags that are on in the new value and not in our cached value

                    int diffcount = 0;
                    for (int i = 1; i <= 64; i++)
                    {
                        if (((difference >> (i - 1)) & 0x00000001) == 1) // if (i-1) bit is on
                        {
                            if (bonfiremap.TryGetValue(i, out var bonfire)) // get the corresponding bonfire
                            {
                                diffcount++;
                                // check event flag. If it's off, set it on and put a message out
                                // then update currentBonfiresInfo
                                if (App.CheckEventFlag(bonfire.Flag) == 0)
                                {
                                    App.SetEventFlag(bonfire.Flag, true);
                                    Log.Logger.Information($"Unlocked bonfire {bonfire.Name}");
                                }
                                currentBonfiresInfo |= ((long)1 << (i - 1));
                            }
                        }
                    }
                    if (diffcount > 0)
                    {
                        Log.Logger.Information($"{diffcount} bonfires updated");
                        givePlayerLordvesselFlag();
                    }
                }
                else
                {
                    Log.Logger.Information($"Unchanged bonfire string {newValue} == {currentBonfiresInfo}");
                }
            });
        }
    }
}
