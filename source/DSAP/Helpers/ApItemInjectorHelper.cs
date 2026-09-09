using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.Models;
using DSAP.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSAP.Helpers
{
    public class ApItemInjectorHelper
    {
        public const int AP_ITEM_OFFSET = 10000; // add to loc id for ap items
        private static readonly object _memAllocLock = new object();
        internal static async Task AddAPItems(Dictionary<long, ScoutedItemInfo> scoutedLocationInfo)
        {
            // Add all locations to pool, to "stub" out in-game items.
            List<KeyValuePair<long, ScoutedItemInfo>> addedEntries = scoutedLocationInfo.ToList();
            //addedEntries.Sort((a, b) => a.Key.CompareTo(b.Key));

            // adds a lot of key items
            // 11110000 to 1111**** - location items
            var added_names = addedEntries.Select(x => new KeyValuePair<long, string>(x.Key, BuildItemName(x))).ToList();
            var added_captions = addedEntries.Select(x => new KeyValuePair<long, string>(x.Key, BuildItemCaption(x))).ToList();
            var added_descriptions = addedEntries.Select(x => new KeyValuePair<long, string>(x.Key, BuildItemCaption(x))).ToList();

            // 11109961 to 11109999 - fog walls
            var added_emk_names = MiscHelper.GetDsrEventItems().Select(x => new KeyValuePair<long, string>(x.Id, $"{x.Name}\0"));
            var added_emk_captions = MiscHelper.GetDsrEventItems().Select(x => new KeyValuePair<long, string>(x.Id, BuildDsrEventItemCaption()));
            var added_emk_descriptions = MiscHelper.GetDsrEventItems().Select(x => new KeyValuePair<long, string>(x.Id, BuildDsrEventItemCaption()));

            // 11109001 to 11109002 - progressive items
            var added_progressive_names = MiscHelper.GetProgressiveItems().Select(x => new KeyValuePair<long, string>(x.Id, $"{x.Name}\0"));
            var added_progressive_captions = MiscHelper.GetProgressiveItems().Select(x => new KeyValuePair<long, string>(x.Id, BuildDsrProgressivetemCaption()));
            var added_progressive_descriptions = MiscHelper.GetProgressiveItems().Select(x => new KeyValuePair<long, string>(x.Id, BuildDsrProgressivetemCaption()));

            // 11108001 to 11108002 - keychains
            var keychain_names = MiscHelper.GetKeychainItems().Select(x => new KeyValuePair<long, string>(x.Id, $"{x.Name}\0"));
            var keychain_captions = MiscHelper.GetKeychainItems().Select(x => new KeyValuePair<long, string>(x.Id, BuildDsrKeychainCaption(x.Name)));
            var keychain_descriptions = MiscHelper.GetKeychainItems().Select(x => new KeyValuePair<long, string>(x.Id, BuildDsrKeychainDescription(x)));



            added_names.AddRange(added_emk_names);
            added_captions.AddRange(added_emk_captions);
            added_descriptions.AddRange(added_emk_descriptions);

            added_names.AddRange(added_progressive_names);
            added_captions.AddRange(added_progressive_captions);
            added_descriptions.AddRange(added_progressive_descriptions);

            added_names.AddRange(keychain_names);
            added_captions.AddRange(keychain_captions);
            added_descriptions.AddRange(keychain_descriptions);

            added_names.Sort((a, b) => a.Key.CompareTo(b.Key));
            added_captions.Sort((a, b) => a.Key.CompareTo(b.Key));
            added_descriptions.Sort((a, b) => a.Key.CompareTo(b.Key));

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // add items
            bool do_replacements = upgradeGoods(added_names);

            AddMsgs(MsgManStruct.OFFSET_ITEM_NAMES, added_names, "Item Names"); // names
            AddMsgs(MsgManStruct.OFFSET_ITEM_CAPTIONS, added_captions, "Item Captions"); // captions
            AddMsgs(MsgManStruct.OFFSET_ITEM_DESCRIPTIONS, added_descriptions, "Item Descriptions"); // info
            
            watch.Stop();
            Log.Logger.Debug($"Finished adding new items params + msg text, took {watch.ElapsedMilliseconds}ms");
            //App.Client.AddOverlayMessage($"Finished adding new items params + msg text, took {watch.ElapsedMilliseconds}ms");

            var local_ap_keys = added_emk_names.ToList();
            local_ap_keys.Sort((a, b) => a.Key.CompareTo(b.Key));
            // add item removal hook for all "location" items AND all fogwall key items - which are directly before the locations, by id.
            HookHelper.AddAPItemHook(added_progressive_names.Min(x => x.Key), scoutedLocationInfo.Max(x => x.Key));
            // add item popup removal hook for all "location" items
            HookHelper.AddAPItemPopupHook(scoutedLocationInfo.Min(x => x.Key), scoutedLocationInfo.Max(x => x.Key));
        }

        internal static string BuildItemName(KeyValuePair<long, ScoutedItemInfo> item)
        {
            const byte progression = 0b001;
            const byte useful = 0b010;
            const byte trap = 0b100;
            string color = "00EEEE";
            if ((((byte)item.Value.Flags) & progression) == progression) color = "AF99EF";
            else if ((((byte)item.Value.Flags) & useful) == useful) color = "6D8BE8";
            else if ((((byte)item.Value.Flags) & trap) == trap) color = "FA8072";

            // strip #'s out of item name and player name in case they screw up the string
            string itemnm = item.Value.ItemDisplayName.Replace('#', ' ').Replace('%', ' ');
            string playernm = item.Value.Player.Alias.Replace('#', ' ').Replace('%', ' ');

            // there seems to be a 64 character limit for item names.
            if (item.Value.Player.Slot == App.Client.CurrentSession.ConnectionInfo.Slot)
            {
                int maxlen = 63 - 17;
                if (itemnm.Length > maxlen) itemnm = itemnm.Substring(0, maxlen);
                return $"[AP] #c[{color}]{itemnm}#c\0"; // 17 chars plus item name
            }
            else
            {
                int pnlen = playernm.Length;
                int maxlen = 63 - 15 - pnlen;
                if (itemnm.Length > maxlen) itemnm = itemnm.Substring(0, maxlen);
                return $"{playernm}'s #c[{color}]{itemnm}#c\0"; // 15 chars plus item plus player name
            }   
        }
        internal static string BuildItemCaption(KeyValuePair<long, ScoutedItemInfo> item)
        {
            const byte progression = 0b001;
            const byte useful = 0b010;
            const byte trap = 0b100;
            string item_type = "#c[00EEEE]Filler#c";
            if ((((byte)item.Value.Flags) & progression) == progression) item_type = "#c[AF99EF]Progression#c";
            else if ((((byte)item.Value.Flags) & useful) == useful) item_type = "#c[6D8BE8]Useful#c";
            else if ((((byte)item.Value.Flags) & trap) == trap) item_type = "#c[FA8072]Trap#c";

            string playernm = item.Value.Player.Alias.Replace('#', ' ').Replace('%', ' ');

            if (item.Value.Player.Slot == App.Client.CurrentSession.ConnectionInfo.Slot)
                return $"{item_type} for #c[EE00EE]you#c, in this game.\0"; 
            return $"{item_type} for #c[FAFAD2]#b{playernm}#b#c's world of #b{item.Value.ItemGame}#b.\0";
        }
        internal static string BuildDsrEventItemCaption()
        {
            return "A boon from another world. Makes a fog wall passable.\0";
        }
        internal static string BuildDsrProgressivetemCaption()
        {
            return "A multiplier improver.\0";
        }
        internal static string BuildDsrKeychainCaption(string name)
        {
            if (name.Contains("Boss"))
                return "Tracks which boss fogwalls you've unlocked (see extended Description)";
            else
                return "Tracks which fogwalls you've unlocked (see extended Description)";
        }

        internal static string BuildDsrKeychainDescription(DarkSoulsItem keychain)
        {
            string builtString = "";

            List<EmkController> items;
            if (keychain.Name.Contains("Boss"))
            {
                items = App.EmkControllers.Where(x => x.Type == Enums.DsrEventType.BOSSFOGWALL).OrderBy(x => x.KeychainName).ToList();
            }
            else
            {
                items = App.EmkControllers.Where(x => x.Type == Enums.DsrEventType.FOGWALL).OrderBy(x => x.KeychainName).ToList();
            }
            if (items.Count == 0)
            {
                return "This item shouldn't exist - no events that it locks are detected in this world";
            }
            string[] builtStringArray = new string[11];
            for (int i  = 0; i < 11; i++)
            {
                string first = "";
                if (i < items.Count)
                {
                    if (items[i].HasKey)
                        first = $"[x] {items[i].KeychainName}";
                    else
                        first = $"[_] {items[i].KeychainName}";
                }
                else
                {
                    Log.Logger.Warning($"Error, size={items.Count}, keychain={keychain.Name}, id={keychain.Id}");
                }
                string second = "";
                if (i + 11 < items.Count)
                {
                    if (items[i + 11].HasKey)
                        second = $"[x] {items[i + 11].KeychainName}";
                    else
                        second = $"[_] {items[i + 11].KeychainName}";
                }
                builtStringArray[i] = first + second;
                builtString += builtStringArray[i] + "\n";
            }
            Log.Logger.Debug($"bs={builtString}");
            Log.Logger.Verbose($"debug, size={items.Count}, keychain={keychain.Name}, id={keychain.Id}");
            return builtString;
        }


        private static bool upgradeGoods(List<KeyValuePair<long, string>> addedEntries)
        {
            // Read in the Param Structure
            // Modify it,
            // Then save it back
            bool reloadRequired = ParamHelper.ReadFromBytes(out ParamStruct<EquipParamGoods> paramStruct,
                                                     EquipParamGoods.spOffset,
                                                     (ps) => ps.ParamEntries.Last().id >= 11109961);
            if (!reloadRequired)
            {
                Log.Logger.Debug("Skipping reload of EquipParamGoods");
                return false;
            }
            // if we are here, we are updating the params.

            ushort new_entries = (ushort)addedEntries.Count();

            uint goods_param_size = 0x5c;

            // Get first entry's Param (e.g. White Sign Soapstone), use it as basis for new params.
            byte[] parambytes = new byte[EquipParamGoods.Size];
            Array.Copy(paramStruct.ParamBytes, paramStruct.ParamEntries[0].paramOffset, parambytes, 0, parambytes.Length);

            parambytes[0x36] = 99; // max num
            parambytes[0x3a] = 1; // goods type = key
            parambytes[0x3b] = 0; // ref category = like key
            parambytes[0x3e] = 0; // use animation = 0
            parambytes[0x44] = 0x00; // all 'enable_<mp>' and is equip flags = 0
            parambytes[0x45] = 0x30; // only isDrop and isDeposit = 1, is only one = 0

            parambytes[0x10] = 0; // sell value byte 0
            parambytes[0x11] = 0; // sell value byte 1
            parambytes[0x12] = 0; // sell value byte 2
            parambytes[0x13] = 0; // sell value byte 3

            // For each new item, "Add Item" to ParamSt
            for (uint i = 0; i < new_entries; i++)
            {
                var entry = addedEntries.ToArray()[i];
                uint newid = (uint)entry.Key;
                byte[] stringbytes = Encoding.ASCII.GetBytes($"{entry.Value}\0");
                // set sort bytes in param based on id - not sure if this is grabbing top or bottom 2 bytes!! But filling all 4 put the items at the top instead.
                byte[] idbytes = BitConverter.GetBytes(newid);
                parambytes[0x1c] = idbytes[0]; // sort byte 0
                parambytes[0x1d] = idbytes[1]; // sort byte 1
                //parambytes[0x1e] = idbytes[2];
                //parambytes[0x1f] = idbytes[3];
                byte[] iconbytes = BitConverter.GetBytes((short)2042);
                parambytes[0x2c] = iconbytes[0]; // icon byte 0
                parambytes[0x2d] = iconbytes[1]; // icon byte 1
                // This will add the item to the array, and append its string to the NewString buffer
                paramStruct.AddParam(newid, parambytes, stringbytes);
            }

            Log.Logger.Debug($"Added {new_entries} items to EquipParamGoods from {addedEntries.First().Key} to {addedEntries.Last().Key}");

            ParamHelper.WriteFromParamSt(paramStruct, EquipParamGoods.spOffset);

            return true;
        }
        internal static void AddMsgs(int msgManOffset, List<KeyValuePair<long, string>> instrings, string msgsName)
        {
            // Read in system text FMGs
            bool reloadRequired = MsgManHelper.ReadMsgManStruct(out MsgManStruct msgManStruct,
                                                     msgManOffset,
                                                     (ps) => ps.MsgEntries.Last().id >= 99999990);
            if (!reloadRequired)
            {
                Log.Logger.Warning($"Warning: Could not reload {msgsName} msgs.");
                return;
            }

            foreach (var input in instrings)
                msgManStruct.AddMsg((uint)input.Key, input.Value);


            msgManStruct.AddMsg(99999998, ""); // add dummy message to mark that we've been here
            msgManStruct.MsgEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
            Log.Logger.Debug($"Updated {msgsName} struct");

            MsgManHelper.WriteFromMsgManStruct(msgManStruct, msgManOffset); // write the msgs update
        }
        private static void UpdateItemText(ulong strloc, int len, string newstring)
        {
            if (strloc == 0)
            {
                Log.Logger.Information($"strloc = {strloc}"); return;
            }
            byte[] ba = Memory.ReadByteArray(strloc, len);
            string su16 = Encoding.Unicode.GetString(ba);
            string[] sub16 = su16.Split("\0");
            Log.Logger.Information($"Padding to {sub16[0].Length} bytes");
            int available_space = sub16[0].Length;
            string newptxt = newstring;
            if (newstring.Length > available_space)
                newptxt = newstring.Substring(0, sub16[0].Length);

            byte[] newba = Encoding.Unicode.GetBytes(newptxt);
            Memory.WriteByteArray(strloc, newba);
            Log.Logger.Information($"String found: {su16}, \n@{strloc:X}");
            Log.Logger.Information($"Wrote string {newptxt}");
        }

        private static ulong FindMsg(ulong MsgsStart, uint id)
        {
            ulong GoodsMsgsStrTableOffset = Memory.ReadULong(MsgsStart + 0x14);
            ushort GoodsMsgsCompareEntries = Memory.ReadUShort(MsgsStart + 0xc);
            ulong GoodsMsgsCompareStart = MsgsStart + 0x1c;
            uint compareEntrySize = 0xc;
            for (uint curridx = 0; curridx < GoodsMsgsCompareEntries; curridx++)
            {
                ulong currentry = GoodsMsgsCompareStart + (compareEntrySize * curridx);
                uint low = Memory.ReadUInt(currentry + 0x4);
                uint high = Memory.ReadUInt(currentry + 0x8);
                if (low <= id && id <= high)
                {
                    uint baseoffset = Memory.ReadUInt(currentry + 0x0);
                    uint idoffset = id - low;
                    uint strEntryOffset = 4 * (idoffset + baseoffset);

                    ulong itemstroffset = Memory.ReadUInt(MsgsStart + GoodsMsgsStrTableOffset + strEntryOffset);
                    ulong itemstrloc = MsgsStart + itemstroffset;
                    return itemstrloc;
                }
            }
            return 0;
        }

        public static void ChangePrismStoneText()
        {
            var item = MiscHelper.GetAllItems().Find(x => x.Name.ToLower().Contains("prism stone"));
            uint itemid = (uint)item.Id;
            //uint itemid = 9014;

            ulong MsgMan = Memory.ReadULong(0x141c7e3e8);
            ulong GoodsMsgsStart = Memory.ReadULong(MsgMan + 0x380);
            ulong GoodsCaptionMsgsStart = Memory.ReadULong(MsgMan + 0x378);
            ulong GoodsInfoMsgStart = Memory.ReadULong(MsgMan + 0x328);
            ulong itemNameStrLoc = FindMsg(GoodsMsgsStart, itemid);
            ulong itemCaptionStrLoc = FindMsg(GoodsCaptionMsgsStart, itemid);
            ulong itemInfoStrLoc = FindMsg(GoodsInfoMsgStart, itemid);

            UpdateItemText(itemNameStrLoc, 100, "AP Item\0");
            UpdateItemText(itemCaptionStrLoc, 100, "This is an item that belongs to another world...\0");
            UpdateItemText(itemInfoStrLoc, 500, "*narrator voice* We're not sure how this got here. \nBest hold on to it. \n\nJust in case.\0");

            ulong equipGoodsParamResCap = Memory.ReadULong((ulong)(AddressHelper.SoloParamAob.Address + 0xF0));
            //upgradeGoods(equipGoodsParamResCap);
            //AddMsgs(9015, new List<string>() { "AP Item From Player 2's world" });
            return;
        }
    }
}
