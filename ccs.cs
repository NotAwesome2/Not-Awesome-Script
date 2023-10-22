//pluginref __item.dll
//pluginref _extralevelprops.dll
//pluginref _na2lib.dll

//reference System.Core.dll
//reference System.dll

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Security.Policy;
using MCGalaxy;
using MCGalaxy.Maths;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerDBEvents;
using MCGalaxy.Commands;
using MCGalaxy.Network;
using BlockID = System.UInt16;
using ScriptAction = System.Action;
using ExtraLevelProps;
using NA2;

namespace PluginCCS {
    
    public class CmdTempBlock : Command2 {        
        public override string name { get { return "TempBlock"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
		public override bool LogUsage { get { return false; } }

        public override void Use(Player p, string message, CommandData data) {      
            
            if (!(p.group.Permission >= LevelPermission.Operator)) {
                if (!Hacks.CanUseHacks(p)) {
                    if (data.Context != CommandContext.MessageBlock) {
                        p.Message("%cYou cannot use this command manually when hacks are disabled.");
                        return;
                    }
                }
            }
            
            BlockID block = p.GetHeldBlock();
            int x = p.Pos.BlockX, y = (p.Pos.Y - 32) / 32, z = p.Pos.BlockZ;

            try {
                string[] parts = message.Split(' ');
                switch (parts.Length) {
                    case 1:
                        if (message == "") break;
                        
                        if (!CommandParser.GetBlock(p, parts[0], out block)) return;
                        break;
                    case 3:
                        x = int.Parse(parts[0]);
                        y = int.Parse(parts[1]);
                        z = int.Parse(parts[2]);
                        break;
                    case 4:
                        if (!CommandParser.GetBlock(p, parts[0], out block)) return;
                        
                        x = int.Parse(parts[1]);
                        y = int.Parse(parts[2]);
                        z = int.Parse(parts[3]);
                        break;
                    default: p.Message("Invalid number of parameters"); return;
                }
            } catch { 
                p.Message("Invalid parameters"); return; 
            }

            if (!CommandParser.IsBlockAllowed(p, "place ", block)) return;
            
            x = Clamp(x, p.level.Width);
            y = Clamp(y, p.level.Height);
            z = Clamp(z, p.level.Length);
            
            p.SendBlockchange( (ushort)x, (ushort)y, (ushort)z, block);
            //string blockName = Block.GetName(p, block);
            //Player.Message(p, "{3} block was placed at ({0}, {1}, {2}).", P.X, P.Y, P.Z, blockName);
        }
        
        static int Clamp(int value, int axisLen) {
            if (value < 0) return 0;
            if (value >= axisLen) return axisLen - 1;
            return value;
        }
        
        public override void Help(Player p) {
            p.Message("%T/TempBlock [block] <x> <y> <z>");
            p.Message("%HPlaces a client-side block at your feet or <x> <y> <z>");
        }
        
    }
    public class CmdTempChunk : Command2 {        
        public override string name { get { return "tempchunk"; } }
        public override string shortcut { get { return "tempc"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
		public override bool LogUsage { get { return false; } }

        public override void Use(Player p, string message, CommandData data) {      
            
            if (p.group.Permission < LevelPermission.Operator && !Hacks.CanUseHacks(p)) {
                if (data.Context != CommandContext.MessageBlock) {
                    p.Message("%cYou cannot use this command manually when hacks are disabled.");
                    return;
                }
            }
            
            if (message == "") { Help(p); return; }
            string[] words = message.Split(' ');
            if (words.Length < 9) {
                p.Message("%cYou need to provide all 3 sets of coordinates, which means 9 numbers total.");
                return;
            }
            
            int x1 = 0, y1 = 0, z1 = 0, x2 = 0, y2 = 0, z2 = 0, x3 = 0, y3 = 0, z3 = 0;

            bool mistake = false;
            if (!CommandParser.GetInt(p, words[0], "x1", ref x1, 0, p.Level.Width -1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[1], "y1", ref y1, 0, p.Level.Height -1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[2], "z1", ref z1, 0, p.Level.Length -1)) { mistake = true; }
            
            if (!CommandParser.GetInt(p, words[3], "x2", ref x2, 0, p.Level.Width -1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[4], "y2", ref y2, 0, p.Level.Height -1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[5], "z2", ref z2, 0, p.Level.Length -1)) { mistake = true; }
            
            if (!CommandParser.GetInt(p, words[6], "x3", ref x3, 0, p.Level.Width -1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[7], "y3", ref y3, 0, p.Level.Height -1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[8], "z3", ref z3, 0, p.Level.Length -1)) { mistake = true; }
            if (mistake) { return; }
            
            if (x1 > x2) { p.Message("%cx1 cannot be greater than x2!"); mistake = true; }
            if (y1 > y2) { p.Message("%cy1 cannot be greater than y2!"); mistake = true; }
            if (z1 > z2) { p.Message("%cz1 cannot be greater than z2!"); mistake = true; }
            if (mistake) { p.Message("%HMake sure the first set of coords is on the minimum corner (press f7)"); return; }
            bool allPlayers = false;
            if (words.Length > 9) {
                CommandParser.GetBool(p, words[9], ref allPlayers);
                if (data.Context != CommandContext.MessageBlock && allPlayers) {
                    p.Message("%cYou can't send the tempchunk to all players unless the command comes from a message block.");
                    return;
                }
            }
            
            //    95, 33, 73, 99, 36, 75, 97, 37, 79
            
            BlockID[] blocks = GetBlocks(p, x1, y1, z1, x2, y2, z2);
            

            PlaceBlocks(p, blocks, x1, y1, z1, x2, y2, z2, x3, y3, z3, allPlayers);
            
        }
        
        public BlockID[] GetBlocks(Player p, int x1, int y1, int z1, int x2, int y2, int z2) {          
            
            int xLen = (x2 - x1) +1;
            int yLen = (y2 - y1) +1;
            int zLen = (z2 - z1) +1;
            
            BlockID[] blocks = new BlockID[xLen * yLen * zLen];
            int index = 0;
            
            for(int xi = x1; xi < x1+xLen; ++xi) {
                for(int yi = y1; yi < y1+yLen; ++yi) {
                    for(int zi = z1; zi < z1+zLen; ++zi) {
                        blocks[index] = p.level.GetBlock((ushort)xi, (ushort)yi, (ushort)zi);
                        index++;
                    }
                }
            }
            return blocks;
        }
        
        public void PlaceBlocks(Player p, BlockID[] blocks, int x1, int y1, int z1, int x2, int y2, int z2, int x3, int y3, int z3, bool allPlayers = false ) {
            
            int xLen = (x2 - x1) +1;
            int yLen = (y2 - y1) +1;
            int zLen = (z2 - z1) +1;
            
            Player[] players = allPlayers ? PlayerInfo.Online.Items : new [] { p };
            
            foreach (Player pl in players) {
                if (pl.level != p.level) continue;
                
                BufferedBlockSender buffer = new BufferedBlockSender(pl);
                int index = 0;
                for(int xi = x3; xi < x3+xLen; ++xi) {
                    for(int yi = y3; yi < y3+yLen; ++yi) {
                        for(int zi = z3; zi < z3+zLen; ++zi) {
                            int pos = pl.level.PosToInt( (ushort)xi, (ushort)yi, (ushort)zi);
                            if (pos >= 0) buffer.Add(pos, blocks[index]);
                            index++;
                        }
                    }
                }
                // last few blocks 
                buffer.Flush();
            }
            
        }
        
        public override void Help(Player p) {
            p.Message("%T/TempChunk %f[x1 y1 z1] %7[x2 y2 z2] %r[x3 y3 z3] <true/false>");
            p.Message("%HCopies a chunk of the world defined by %ffirst %Hand %7second%H coords then pastes it into the spot defined by the %rthird %Hset of coords.");
            p.Message("%HThe last option is optional, and defaults to false. If true, the tempchunk changes are sent to all players in the map.");
        }
        
    }

    public class CmdStuff : Command2 {
        public override string name { get { return "Stuff"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return "other"; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public override CommandPerm[] ExtraPerms {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can get/take stuff without message blocks") }; }
        }
        
        public override void Use(Player p, string message, CommandData data) {
            p.lastCMD = "nothing2";
            if (message.Length == 0) {
                DisplayItems(p, false);
                _Help(p);
                return;
            }
            string[] args = message.SplitSpaces(2);
            string function = args[0].ToUpper();
            string functionArgs = args.Length > 1 ? args[1] : "";

            if (function == "DROP") {
                p.Message("%cTo delete stuff, use %b/drop [name]");
                return;
            }
            if (function == "LOOK" || function == "EXAMINE") {
                UseExamine(p, functionArgs);
                return;
            }
            Action unknownMsg = () => { p.Message("&WUnknown &T/stuff &Warg \"{0}\"", function); };
            
            if (functionArgs != "") {
                if ( !(data.Context == CommandContext.MessageBlock || HasExtraPerm(p, p.Rank, 1)) ) {
                    unknownMsg();
                    return;
                }
                UseGiveTake(p, functionArgs, data);
                return;
            }
            
            unknownMsg();
            Help(p);
        }
        
        static void UseExamine(Player p, string itemName) {
            if (itemName == "") {
                p.Message("Please specify something to examine.");
                return;
            }
            Item item = Item.MakeInstance(p, itemName);
            if (item == null) { return; }
            
            if (!item.OwnedBy(p.name) || item.isVar) {
                p.Message("&cYou dont have any stuff called \"{0}\".", item.displayName);
                return;
            }
            string[] desc = item.ItemDesc; //cache
            if (desc.Length == 0 ) {
                p.Message("You don't notice anything particular about the {0}%S.", item.ColoredName); return;
            }
            
            p.Message("You examine the {0}%S...", item.ColoredName);
            Thread.Sleep(1000);
            p.MessageLines(desc.Select(line => "&e" + line));
        }
        
        void UseGiveTake(Player p, string message, CommandData data) {
            if (message == "") { p.Message("Expected arg LIST, GET/GIVE [ITEM_NAME], or TAKE/REMOVE [ITEM_NAME]"); return; }
            string[] args = message.ToUpper().SplitSpaces(2);
            string function = args[0];
            
            if (function == "LIST") { DisplayItems(p, true); return; }
            
            string itemName = args.Length > 1 ? args[1] : "";
            Item item = Item.MakeInstance(p, itemName);
            if (item == null) { return; }
            
            if (function == "GET" || function == "GIVE") { item.GiveTo(p); return; }
            
            if (function == "TAKE" || function == "REMOVE") { item.TakeFrom(p); return; }
            
            p.Message("Function &c" + function + "%S was unrecognized."); return;
        }
        
        static void DisplayItems(Player p, bool showVars) {
            p.Message("%eYour stuff:");
            
            Item[] items = Item.GetItemsOwnedBy(p.name);
            
            List<string> coloredItems = new List<string>(items.Length);
            int amountDisplayed = 0;
            for (int i = 0; i < items.Length; i++) {
                if (items[i].isVar && !showVars) { continue; }
                amountDisplayed++;
                coloredItems.Add(items[i].ColoredName);
            }
            if (amountDisplayed == 0) {
                p.Message("You have no stuff!"); return;
            }
            p.Message(String.Join(" &8• ", coloredItems));
        }
        
        public override void Help(Player p) {
            p.Message("%T/Stuff");
            p.Message("%HLists your stuff.");
            _Help(p);
            if (!HasExtraPerm(p, p.Rank, 1)) { return; }
            p.Message("%T/Stuff [anyword] GET [item]");
            p.Message("%T/Stuff [anyword] TAKE [item]");
            p.Message("%HStaff only -- get/take any item.");
            p.Message("%H[anyword] is required for backwards compatibility with the obsolete password system.");
        }
        
        static void _Help(Player p) {
            p.Message("%eUse %b/stuff look [item name] %eto examine items.");
            p.Message("%HTo delete stuff, use %b/drop [item name]");
        }
        
    }
    
    public class CmdDrop : Command2 {
        public override string name { get { return "drop"; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override string type { get { return "other"; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        
        public override void Use(Player p, string message, CommandData data)
        {
            if (message == "") { Help(p); return; }
            
            Item item = Item.MakeInstance(p, message);
            if (item == null) { return; }
            
            if (!item.OwnedBy(p.name) || item.isVar) {
                p.Message("&cYou dont have any stuff called \"{0}\".", item.displayName);
                return;
            }
            
            item.TakeFrom(p);
        }
        
        public override void Help(Player p) {
            p.Message("%T/Drop [stuff]");
            p.Message("%HDrops the /stuff you specify.");
        }
    }
    
    public class CmdReplyTwo : Command2 {
        public override string name { get { return "ReplyTwo"; } }
        public override string shortcut { get { return ""; } }
        public override bool MessageBlockRestricted { get { return false; } }
        public override string type { get { return "other"; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
		public override bool LogUsage { get { return false; } }
        public const int maxReplyCount = 6;
        const CpeMessageType line1 = CpeMessageType.BottomRight3;
        const CpeMessageType line2 = CpeMessageType.BottomRight2;
        const CpeMessageType line3 = CpeMessageType.BottomRight1;
        
        const CpeMessageType line4 = CpeMessageType.Status1;
        const CpeMessageType line5 = CpeMessageType.Status2;
        const CpeMessageType line6 = CpeMessageType.Status3;
        
        public const PersistentMessagePriority replyPriority = PersistentMessagePriority.Highest;
        
        public override void Use(Player p, string message, CommandData data)
        {
            if (message == "") { Help(p); return; }
            int replyNum = -1;
            if (!CommandParser.GetInt(p, message, "Reply number", ref replyNum, 1, maxReplyCount)) { return; }
            ScriptData scriptData = Core.GetScriptData(p);
            ReplyData replyData = scriptData.replies[replyNum-1]; //reply number is from 1-6 but the array is indexed 0-5, hence -1
            if (replyData == null) { p.Message("There's no reply option &f[{0}] &Sat the moment.", replyNum); return; }
            
            //reset the replies once you choose one
            scriptData.ResetReplies();
            
            //the script has to be ran as if from a message block
            CommandData cmdData = default(CommandData); cmdData.Context = CommandContext.MessageBlock;
            
            if (replyData.isOS) {
                Core.osRunscriptCmd.Use(p, replyData.labelName, cmdData);
            } else {
                Core.runscriptCmd.Use(p, replyData.scriptName+" "+replyData.labelName, cmdData);
            }
        }
        public static void SetUpDone(Player p) {
            p.Message("&e(say a number to choose a response from the right →→)");
        }
        
        public static CpeMessageType GetReplyMessageType(int i) {
            if (i == 1) { return line1; }
            if (i == 2) { return line2; }
            if (i == 3) { return line3; }
            if (i == 4) { return line4; }
            if (i == 5) { return line5; }
            if (i == 6) { return line6; }
            return CpeMessageType.Normal;
        }
        
        public override void Help(Player p) {
            p.Message("&T/Reply [num]");
            p.Message("&HReplies with the option you specify.");
            p.Message("&HYou'll be prompted to use it during adventure maps.");
        }

        public static void OnPlayerChat(Player p, string message) {
            ScriptData scriptData = Core.GetScriptData(p);
            bool replyActive = false;
            bool notifyPlayer = false;
            for (int i = 0; i < maxReplyCount; i++) {
                if (scriptData.replies[i] != null) {
                    if (scriptData.replies[i].notifyPlayer) { notifyPlayer = true; }
                    replyActive = true;
                }
            }
            if (!replyActive) { return; }

            string msgStripped = message.Replace("[", "");
            msgStripped = msgStripped.Replace("]", "");
            int reply = -1;
            if (Int32.TryParse(msgStripped, out reply)) {
                CommandData data2 = default(CommandData); data2.Context = CommandContext.MessageBlock;
                p.HandleCommand("replytwo", msgStripped, data2);
                p.cancelchat = true;
                return;
            }
            //notify player how to reply if they don't choose one and one is available
            if (notifyPlayer) { SetUpDone(p); }
        }
    }
    
    public class CmdItems : Command2 {
        public override string name { get { return "Items"; } }
        public override string type { get { return "other"; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, CommandData data) {
            Core.GetScriptData(p).DisplayItems();
        }
        public override void Help(Player p) {
            p.Message("%T/Items");
            p.Message("%HLists your items.");
            p.Message("Notably, items are different from &T/stuff&S because they will disappear if you leave this map.");
        }
    }
    
    public class CmdUpdateOsScript : Command2 {
        public override string name { get { return "OsUploadScript"; } }
        public override string shortcut { get { return "osus"; } }
        public override bool MessageBlockRestricted { get { return false; } }
        public override string type { get { return "other"; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool UpdatesLastCmd { get { return false; } }
        
        public override void Use(Player p, string message, CommandData data) {
            if (!LevelInfo.IsRealmOwner(p.name, p.level.name)) { p.Message("You can only upload scripts to maps that you own."); return; }
            if (message.Length == 0) { p.Message("%cYou need to provide a url of the file that will be used as your map's script."); return; }
            
            HttpUtil.FilterURL(ref message);
            byte[] webData = HttpUtil.DownloadData(message, p); 
            if (webData == null) { return; }

            Script.Update(p, Script.OS_PREFIX + p.level.name.ToLower(), webData);
        }
        
        public override void Help(Player p) {
            p.Message("&T/OsUploadScript [url]");
            p.Message("&HUploads [url] as the script for your map.");
        }
    }
    
    public sealed class Core : Plugin {
        
        public override string creator { get { return "Goodly"; } }
        public override string name { get { return "ccs"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.9"; } }
        
        static readonly object scriptDataLocker = new object();
        
        private static Dictionary<string, ScriptData> scriptDataAtPlayer = new Dictionary<string, ScriptData>();
        public static ScriptData GetScriptData(Player p) {
            lock (scriptDataLocker) { //only one thread should access this at a time
                ScriptData sd;
                if (scriptDataAtPlayer.TryGetValue(p.name, out sd)) { return sd; }
                else {
                    //throw new System.Exception("Tried to get script data for "+p.name+" but there was NOTHING there");
                    Logger.Log(LogType.SystemActivity, "ccs: This shouldn't happen, but; FAILED to GetScriptData for " + p.name+", returning a new one.");
                    scriptDataAtPlayer[p.name] = new ScriptData(p);
                    return scriptDataAtPlayer[p.name];
                }
            }
        }
        
        public static Command tempBlockCmd;
        public static Command tempChunkCmd;
        public static Command stuffCmd;
        public static Command dropCmd;
        public static Command runscriptCmd;
        public static Command osRunscriptCmd;
        public static Command debugScriptCmd;
        public static Command replyCmd;
        public static Command itemsCmd;
        public static Command updateOsScriptCmd;
        public static Command tp;
        public static Command downloadScriptCmd;
        
        
        static Command _boostCmd = null;
        public static Command boostCmd {
            get { if (_boostCmd == null) { _boostCmd = Command.Find("boost"); } return _boostCmd; }
        }
        
        static Command _effectCmd = null;
        public static Command effectCmd {
            get { if (_effectCmd == null) { _effectCmd = Command.Find("effect"); } return _effectCmd; }
        }
        
        
        public override void Load(bool startup) {

            Script.PluginLoad();
            
            tempBlockCmd      = new CmdTempBlock();
            tempChunkCmd      = new CmdTempChunk();
            stuffCmd          = new CmdStuff();
            dropCmd           = new CmdDrop();
            runscriptCmd      = new CmdScript();
            osRunscriptCmd    = new CmdOsScript();
            debugScriptCmd    = new CmdDebugScript();
            replyCmd          = new CmdReplyTwo();
            itemsCmd          = new CmdItems();
            updateOsScriptCmd = new CmdUpdateOsScript();
            downloadScriptCmd = new CmdDownloadScript();
            tp = Command.Find("tp");
            
            Command.Register(tempBlockCmd);
            Command.Register(tempChunkCmd);
            Command.Register(stuffCmd);
            Command.Register(dropCmd);
            Command.Register(runscriptCmd);
            Command.Register(osRunscriptCmd);
            Command.Register(debugScriptCmd);
            Command.Register(replyCmd);
            Command.Register(itemsCmd);
            Command.Register(updateOsScriptCmd);
            Command.Register(downloadScriptCmd);
            
            OnPlayerFinishConnectingEvent.Register(OnPlayerFinishConnecting, Priority.High);
            OnInfoSwapEvent.Register(OnInfoSwap, Priority.Low);
            OnJoiningLevelEvent.Register(OnJoiningLevel, Priority.High);
            OnJoinedLevelEvent.Register(OnJoinedLevel, Priority.High);
            OnLevelRenamedEvent.Register(OnLevelRenamed, Priority.Low);
            OnLevelUnloadEvent.Register(OnLevelUnload, Priority.Low);
            OnPlayerChatEvent.Register(OnPlayerChat, Priority.High);
            OnPlayerCommandEvent.Register(OnPlayerCommand, Priority.High);
            
            OnPlayerDisconnectEvent.Register(OnPlayerDisconnect, Priority.Low);
            
            foreach (Player pl in PlayerInfo.Online.Items) {
                OnPlayerFinishConnecting(pl);
            }
            //placing this after the foreach is my attempt to make sure ScriptData is generated before OnPlayerMove which relies on it
            OnPlayerMoveEvent.Register(OnPlayerMove, Priority.Low);
        }
        public override void Unload(bool shutdown) {
            Command.Unregister(tempBlockCmd);
            Command.Unregister(tempChunkCmd);
            Command.Unregister(stuffCmd);
            Command.Unregister(dropCmd);
            Command.Unregister(runscriptCmd);
            Command.Unregister(osRunscriptCmd);
            Command.Unregister(debugScriptCmd);
            Command.Unregister(replyCmd);
            Command.Unregister(itemsCmd);
            Command.Unregister(updateOsScriptCmd);
            Command.Unregister(downloadScriptCmd);
            
            OnPlayerFinishConnectingEvent.Unregister(OnPlayerFinishConnecting);
            OnInfoSwapEvent.Unregister(OnInfoSwap);
            OnJoiningLevelEvent.Unregister(OnJoiningLevel);
            OnJoinedLevelEvent.Unregister(OnJoinedLevel);
            OnLevelRenamedEvent.Unregister(OnLevelRenamed);
            OnPlayerChatEvent.Unregister(OnPlayerChat);
            OnPlayerCommandEvent.Unregister(OnPlayerCommand);
            
            OnPlayerDisconnectEvent.Unregister(OnPlayerDisconnect);
            
            OnPlayerMoveEvent.Unregister(OnPlayerMove);
            
            
            SaveAllPlayerData();
            scriptDataAtPlayer.Clear();
        }
        static void SaveAllPlayerData() {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players) {
                SavePlayerData(p);
            }
        }
        
        static void OnPlayerFinishConnecting(Player p) {
            //Logger.Log(LogType.SystemActivity, "ccs CONECTING " + p.name + " : " + Environment.StackTrace);
            
            lock (scriptDataLocker) { //only one thread should access this at a time
                if (scriptDataAtPlayer.ContainsKey(p.name)) {
                    //this happens when they Reconnect.
                    scriptDataAtPlayer[p.name].UpdatePlayerReference(p);
                    Logger.Log(LogType.SystemActivity, "ccs ScriptData already exists for player: " + p.name);
                    return;
                }
                scriptDataAtPlayer[p.name] = new ScriptData(p);
            }
        }
        static void OnPlayerDisconnect(Player p, string reason) {
            if (reason.StartsWith("(Reconnecting")) {
                Logger.Log(LogType.SystemActivity, "ccs is not clearing scriptdata due to player reconnecting: " + p.name);
                return;
            }
            
            SavePlayerData(p);
            scriptDataAtPlayer.Remove(p.name);
        }
        static void SavePlayerData(Player p) {
            ScriptData data;
            if (!scriptDataAtPlayer.TryGetValue(p.name, out data)) { return; }
            data.WriteSavedStringsToDisk();
        }
        static void OnJoiningLevel(Player p, Level lvl, ref bool canJoin) {
            Script.OnJoiningLevel(p, lvl, ref canJoin);
        }
        static void OnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce) {
            //clear all persistent chat lines at the priority used by cpemessage in script
            for (int i = 1; i < CmdReplyTwo.maxReplyCount+1; i++) {
                CpeMessageType type = CmdReplyTwo.GetReplyMessageType(i);
                if (type != CpeMessageType.Normal) { p.SendCpeMessage(type, "", ScriptRunner.CpeMessagePriority); }
            }
            ScriptData data;
            if (scriptDataAtPlayer.TryGetValue(p.name, out data)) { data.OnJoinedLevel(); }
        }
        static void OnInfoSwap(string source, string dest) {
            string sourcePath = ScriptData.savePath+source;
            string destPath = ScriptData.savePath+dest;
            string backupPath = ScriptData.savePath+"temporary-info-swap";
            //Chat.MessageOps("info swap event: "+sourcePath+" and "+destPath+"");
            
            if (Directory.Exists(sourcePath)) {
                Directory.Move(sourcePath, backupPath);
            }
            if (Directory.Exists(destPath)) {
                Directory.Move(destPath, sourcePath);
            }
            if (Directory.Exists(backupPath)) {
                Directory.Move(backupPath, destPath);
            }
        }
        static void OnLevelRenamed(string srcMap, string dstMap) {
            Script.OnLevelRenamed(srcMap, dstMap);
        }
        static void OnLevelUnload(Level lvl, ref bool cancel) {
            Script.OnLevelUnload(lvl, ref cancel);
        }
        static void OnPlayerChat(Player p, string message) {
            CmdReplyTwo.OnPlayerChat(p, message);
        }
        static void OnPlayerCommand(Player p, string cmd, string message, CommandData data) {
            Script.OnPlayerCommand(p, cmd, message, data);
        }
        static void OnPlayerMove(Player p, Position next, byte yaw, byte pitch, ref bool cancel) {
            ScriptData scriptData = GetScriptData(p); if (scriptData == null) { return; }
            if (scriptData.stareCoords != null) {
                ScriptRunner.LookAtCoords(p, (Vec3S32)scriptData.stareCoords);
            }
        }
        
    }
    
    public class CmdScript : Command2 {
        public override string name { get { return "Script"; } }
        public override string shortcut { get { return ""; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override string type { get { return "other"; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool UpdatesLastCmd { get { return false; } }
		public override bool LogUsage { get { return false; } }
        public override CommandPerm[] ExtraPerms {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can run scripts without message blocks") }; }
        }
        
        //script #tallyEggs
        //script egg2021 #tallyEggs repeatable
        //script egg2021 #tallyEggs|some|run|args repeatable
        public override void Use(Player p, string message, CommandData data) {
            if ( !(data.Context == CommandContext.MessageBlock || CheckExtraPerm(p, data, 1)) ) { return; }
            
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces();
            int argsOffset = 0;
            string scriptName;
            if (args[0].StartsWith("#")) { //if the script name is omitted, assume it is identical to map name
                scriptName = p.level.name;
                argsOffset = -1;
            } else {
                if (args.Length < 2) { p.Message("You need to provide a script name and a label, or only a label."); return; }
                scriptName = args[0];
            }
            
            string startLabel = args[1+argsOffset];
            bool repeatable = (args.Length > 2+argsOffset && args[2+argsOffset] == "repeatable");
            ScriptRunner.PerformScript(p, scriptName, startLabel, false, repeatable, data);
        }
        
        public const string labelHelp = "%HLabels are case sensitive and must begin with #";
        public override void Help(Player p) {
            p.Message("%T/Script <script name> [starting label]");
            p.Message("%HRuns a script at the given label.");
            p.Message("If no script name is given, the map's name is used as the script name.");
            HelpBody(p);
        }
        public static void HelpBody(Player p) {
            p.Message(labelHelp);
            p.Message("This command is used in adventure map message blocks for extended functionality.");
            p.Message("In order to use this command you need to write and upload a script to your map.");
            p.Message("For more information, please read the guide:");
            p.Message("https://dl.dropbox.com/s/tp9tr21k0dr2qpq/ScriptGuide2.txt");
        }
    }
    public class CmdOsScript : CmdScript {
        public override string name { get { return "OsScript"; } }
        public override string shortcut { get { return "oss"; } }
        public override bool MessageBlockRestricted { get { return false; } }
		public override bool LogUsage { get { return false; } }
        
        //osscript #tallyEggs repeatable
        //osscript #tallyEggs|some|run|args repeatable
        public override void Use(Player p, string message, CommandData data) {
            if ( !(data.Context == CommandContext.MessageBlock || LevelInfo.IsRealmOwner(p.name, p.level.name) || p.group.Permission >= LevelPermission.Operator) ) {
                p.Message("You can only use %b/{0} %Sif it is in a message block or you are the map owner.", name);
                return;
            }
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces();
            string scriptName = Script.OS_PREFIX + p.level.name;
            string startLabel = args[0];
            bool repeatable = (args.Length > 1 && args[1] == "repeatable");
            
            ScriptRunner.PerformScript(p, scriptName, startLabel, true, repeatable, data);
        }
        
        public override void Help(Player p) {
            p.Message("%T/OsScript [starting label]");
            p.Message("%HRuns the os map's script at the given label.");
            HelpBody(p);
        }
    }
    public class CmdDebugScript : Command2 {
        public override string name { get { return "DebugScript"; } }
        public override string shortcut { get { return ""; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override string type { get { return "other"; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool UpdatesLastCmd { get { return false; } }
        
        public override void Use(Player p, string message, CommandData data) {
            if ( !(LevelInfo.IsRealmOwner(p.name, p.level.name) || p.group.Permission >= LevelPermission.Operator) ) {
                p.Message("You can only use %b/{0} %Sif you are the map owner.", name);
                return;
            }
            
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces();
            
            bool debug = false;
            int delay = 0;
            if (!CommandParser.GetBool(p, args[0], ref debug)) { return; }
            if (args.Length > 1 && !CommandParser.GetInt(p, args[1], "debug millisecond delay", ref delay, 0)) { return; }
            
            ScriptData scriptData = Core.GetScriptData(p);
            scriptData.debugging = debug;
            scriptData.debuggingDelay = delay;
            p.Message("Script debugging mode is now {0}&S{1}", debug ? "&atrue" : "&cfalse", debug ? " with &b"+delay+"&S added delay." : "");
        }
        
        public override void Help(Player p) {
            p.Message("&T/DebugScript [true/false] <milliseconds>");
            p.Message("&HStarts or stops script debugging mode");
            p.Message("&HMakes script print every action it does to chat with optional added <milliseconds> delay after each action.");
        }
    }
    public class CmdDownloadScript : CmdScript {
        static string hashword = "CHANGETHIS";
        public override string name { get { return "DownloadScript"; } }
        public override string shortcut { get { return ""; } }
        public override bool MessageBlockRestricted { get { return true; } }
		public override bool LogUsage { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        
        public override void Use(Player p, string message, CommandData data) {
            if (!Server.Config.Name.StartsWith("Not Awesome 2")) {
                p.Message("This command can't be used on &b{0}&S because it uses unique web services that are only available on Not Awesome 2. Sorry!", Server.Config.Name);
                return;
            }
            if ( !(data.Context == CommandContext.MessageBlock || LevelInfo.IsRealmOwner(p.name, p.level.name) || p.group.Permission >= LevelPermission.Operator) ) {
                p.Message("You can only use %b/{0} %Sif you are the map owner.", name);
                return;
            }
            if (!Directory.Exists(directory)) { Directory.CreateDirectory(directory); }
            string scriptPath = Script.FullPath(Script.OS_PREFIX + p.level.name.ToLower());
            if (!File.Exists(scriptPath)) { p.Message("&cNo os script has been uploaded to {0}&c.", p.level.ColoredName); return; }
            
            try {
                File.Copy(scriptPath, fullPath(p.level), true);
            } catch (System.Exception e) {
                p.Message("&cAn error occured while retrieving your script:");
                p.Message(e.Message);
                return;
            }
            p.Message("Your {0}&S script has been prepared for download. You may download the copy from here:", p.level.ColoredName);
            p.Message(url(p.level));
        }
        
        
        static string fullPath(Level level) { return directory + fileName(level); }
        static string directory { get { return "/home/na2/Website-Files/" + folder; } }
        static string folder { get { return "osscripts/"; } }
        static string fileName(Level level) { return level.name.ToLower() + "-" + GetMD5(level) + Script.EXT; }
        static string url(Level level) { return "https://notawesome.cc/" + folder + fileName(level); }
        
        static string GetMD5(Level level) {
            string input = (level.name.ToLower() + hashword).GetHashCode().ToString();
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                string end = "";
                int i2 = 0;
                foreach (char c in sb.ToString().ToCharArray()) {
                    if (i2 % 6 == 0) end += c;
                    i2++;
                }
                return end;
            }
        }
        
        public override void Help(Player p) {
            p.Message("%T/DownloadScript");
            p.Message("%HAllows you to download the os script of your map.");
        }
    }
    
    
    public class Script {

        public const string EXT = ".nas";
        const string PATH = "scripts/";
        public const string OS_PREFIX = "os/";
        public const string PATH_OS = PATH + OS_PREFIX;

        public static string FullPath(string scriptName) { return PATH + scriptName + EXT; }

        /// <summary>
        /// Key is script name, e.g. os/goodlyay+ or fbcommon, value is Script
        /// </summary>
        static Dictionary<string, Script> loadedScripts = new Dictionary<string, Script>();
        static readonly object locker = new object();

        public static void PluginLoad() {
            Directory.CreateDirectory(PATH);
            Directory.CreateDirectory(PATH_OS);
        }
        public static Script Get(Player p, string scriptName) {
            scriptName = scriptName.ToLower();
            Script script = null;

            string fullPath = FullPath(scriptName);
            if (!File.Exists(fullPath)) { p.Message("&cThere is no script \"{0}\"!", scriptName); return null; }

            lock (locker) {
                DateTime fileTime = File.GetLastWriteTimeUtc(fullPath);

                if (loadedScripts.TryGetValue(scriptName, out script)) {
                    if (script.compileDate > fileTime) return script;
                }
            }

            script = LoadScript(p, fullPath);
            if (script != null) {
                lock (locker) { loadedScripts[scriptName] = script; }
                //p.Message("Compiled script {0}.", scriptName);
            }

            return script;
        }
        /// <summary>
        /// Path must be fully qualified including extension
        /// </summary>
        static Script LoadScript(Player p, string path) {

            Script script = new Script();

            string[] scriptLines = new string[] { };
            try {
                scriptLines = File.ReadAllLines(path);
            } catch (IOException e) {
                p.Message("&cCompiling script error: file read/write error: ({0})", e.Message); return null;
            }

            if (scriptLines.Length == 0) { p.Message("&cCompiling script error: script \"{0}\" has no contents!", path); return null; }

            int lineNumber = 0;
            foreach (string lineRead in scriptLines) {
                lineNumber++;
                string line = lineRead.Trim();
                if (line.Length == 0 || line.StartsWith("//")) continue;

                if (line[0] == '#') {
                    if (line.Contains('|')) { p.Message("&cError when compiling script on line " + lineNumber + ": Declaring labels with pipe character | is not allowed."); return null; }
                    if (script.labels.ContainsKey(line)) { p.Message("&cError when compiling script on line " + lineNumber + ": Duplicate label \"" + line + "\" detected."); return null; }
                    script.labels[line] = script.actions.Count;
                } else {
                    ScriptLine scriptLine = new ScriptLine();
                    scriptLine.lineNumber = lineNumber;
                    string lineTrimmedCondition = line;
                    if (line.StartsWith("if")) {
                        Action LackingArgs = () => {
                            p.Message("&cError when compiling script on line " + lineNumber + ": Line \"" + line + "\" does not have enough arguments.");
                        };

                        string[] ifBits = line.SplitSpaces();
                        if (ifBits.Length < 3) { LackingArgs(); return null; }
                        string logic = ifBits[0];
                        string type = ifBits[1];

                        if (logic == "ifnot") {
                            scriptLine.conditionLogic = ScriptLine.ConditionLogic.IfNot;
                        } else if (logic == "if") {
                            scriptLine.conditionLogic = ScriptLine.ConditionLogic.If;
                        } else {
                            p.Message("&cError when compiling script on line " + lineNumber + ": Logic \"" + logic + "\" is not recognized."); return null;
                        }

                        if (type == "item") {
                            //[if]0 [item]1 [SKULL_COIN]2 [msg you have skull]3
                            scriptLine.conditionType = ScriptLine.ConditionType.Item;
                            string[] ugh = line.SplitSpaces(4); if (ugh.Length < 4) { LackingArgs(); return null; }
                            scriptLine.conditionArgs = ugh[2];
                            lineTrimmedCondition = ugh[3];
                        } else {
                            //[if]0 [talkedWitch]1 [msg i dont like you]2
                            scriptLine.conditionType = ScriptLine.ConditionType.Val;
                            string[] ugh = line.SplitSpaces(3);
                            scriptLine.conditionArgs = ugh[1];
                            lineTrimmedCondition = ugh[2];
                        }
                    }

                    string actionType = lineTrimmedCondition.Split(new char[] { ' ' })[0];
                    switch (actionType) {
                        case "msg":
                            scriptLine.actionType = ScriptRunner.ActionType.Message;
                            break;
                        case "cpemsg":
                            scriptLine.actionType = ScriptRunner.ActionType.CpeMessage;
                            break;
                        case "delay":
                            scriptLine.actionType = ScriptRunner.ActionType.Delay;
                            break;
                        case "goto":
                            scriptLine.actionType = ScriptRunner.ActionType.Goto;
                            break;
                        case "jump":
                            scriptLine.actionType = ScriptRunner.ActionType.Jump;
                            break;
                        case "call":
                            scriptLine.actionType = ScriptRunner.ActionType.Call;
                            break;
                        case "set":
                            scriptLine.actionType = ScriptRunner.ActionType.Set;
                            break;
                        case "setadd":
                            scriptLine.actionType = ScriptRunner.ActionType.SetAdd;
                            break;
                        case "setsub":
                            scriptLine.actionType = ScriptRunner.ActionType.SetSub;
                            break;
                        case "setmul":
                            scriptLine.actionType = ScriptRunner.ActionType.SetMul;
                            break;
                        case "setdiv":
                            scriptLine.actionType = ScriptRunner.ActionType.SetDiv;
                            break;
                        case "setmod":
                            scriptLine.actionType = ScriptRunner.ActionType.SetMod;
                            break;
                        case "setrandrange":
                            scriptLine.actionType = ScriptRunner.ActionType.SetRandRange;
                            break;
                        case "setrandrangedecimal":
                            scriptLine.actionType = ScriptRunner.ActionType.SetRandRangeDecimal;
                            break;
                        case "setrandlist":
                            scriptLine.actionType = ScriptRunner.ActionType.SetRandList;
                            break;
                        case "setround":
                            scriptLine.actionType = ScriptRunner.ActionType.SetRound;
                            break;
                        case "setroundup":
                            scriptLine.actionType = ScriptRunner.ActionType.SetRoundUp;
                            break;
                        case "setrounddown":
                            scriptLine.actionType = ScriptRunner.ActionType.SetRoundDown;
                            break;
                        case "quit":
                            scriptLine.actionType = ScriptRunner.ActionType.Quit;
                            break;
                        case "terminate":
                            scriptLine.actionType = ScriptRunner.ActionType.Terminate;
                            break;
                        case "show":
                            scriptLine.actionType = ScriptRunner.ActionType.Show;
                            break;
                        case "kill":
                            scriptLine.actionType = ScriptRunner.ActionType.Kill;
                            break;
                        case "cmd":
                            scriptLine.actionType = ScriptRunner.ActionType.Cmd;
                            break;
                        case "resetdata":
                            scriptLine.actionType = ScriptRunner.ActionType.ResetData;
                            break;
                        case "item":
                            scriptLine.actionType = ScriptRunner.ActionType.Item;
                            break;
                        case "freeze":
                            scriptLine.actionType = ScriptRunner.ActionType.Freeze;
                            break;
                        case "unfreeze":
                            scriptLine.actionType = ScriptRunner.ActionType.Unfreeze;
                            break;
                        case "look":
                            scriptLine.actionType = ScriptRunner.ActionType.Look;
                            break;
                        case "stare":
                            scriptLine.actionType = ScriptRunner.ActionType.Stare;
                            break;
                        case "newthread":
                            scriptLine.actionType = ScriptRunner.ActionType.NewThread;
                            break;
                        case "env":
                            scriptLine.actionType = ScriptRunner.ActionType.Env;
                            break;
                        case "motd":
                            scriptLine.actionType = ScriptRunner.ActionType.MOTD;
                            break;
                        case "setspawn":
                            scriptLine.actionType = ScriptRunner.ActionType.SetSpawn;
                            break;
                        case "reply":
                            scriptLine.actionType = ScriptRunner.ActionType.Reply;
                            break;
                        case "replysilent":
                            scriptLine.actionType = ScriptRunner.ActionType.ReplySilent;
                            break;
                        case "tempblock":
                            scriptLine.actionType = ScriptRunner.ActionType.TempBlock;
                            break;
                        case "tempchunk":
                            scriptLine.actionType = ScriptRunner.ActionType.TempChunk;
                            break;
                        case "reach":
                            scriptLine.actionType = ScriptRunner.ActionType.Reach;
                            break;
                        case "setblockid":
                            scriptLine.actionType = ScriptRunner.ActionType.SetBlockID;
                            break;
                        case "definehotkey":
                            scriptLine.actionType = ScriptRunner.ActionType.DefineHotkey;
                            break;
                        case "undefinehotkey":
                            scriptLine.actionType = ScriptRunner.ActionType.UndefineHotkey;
                            break;
                        case "placeblock":
                            scriptLine.actionType = ScriptRunner.ActionType.PlaceBlock;
                            break;
                        case "changemodel":
                            scriptLine.actionType = ScriptRunner.ActionType.ChangeModel;
                            break;
                        case "setdirvector":
                            scriptLine.actionType = ScriptRunner.ActionType.SetDirVector;
                            break;
                        case "boost":
                            scriptLine.actionType = ScriptRunner.ActionType.Boost;
                            break;
                        case "effect":
                            scriptLine.actionType = ScriptRunner.ActionType.Effect;
                            break;
                        default:
                            p.Message("&cError when compiling script on line " + lineNumber + ": unknown Action \"" + actionType + "\" detected.");
                            return null;
                    }

                    if (lineTrimmedCondition.Split(new char[] { ' ' }).Length > 1) {
                        scriptLine.actionArgs = lineTrimmedCondition.SplitSpaces(2)[1];

                        //p.Message(scriptLine.actionType + " " + scriptLine.actionArgs);
                    } else {
                        //p.Message(scriptLine.actionType + ".");
                    }

                    script.actions.Add(scriptLine);

                }
            }


            return script;
        }

        public static void Update(Player p, string scriptName, byte[] data) {
            try {
                File.WriteAllBytes(FullPath(scriptName), data);
            } catch (IOException e) {
                p.Message("Problem while uploading script: ({0})", e.Message);
                return;
            }
            p.Message("Done! Uploaded %f" + scriptName + "%S.");
        }

        public static void OnJoiningLevel(Player p, Level lvl, ref bool canJoin) {
            string filePath = PATH + lvl.name + EXT;
            if (!File.Exists(filePath)) { return; }

            //TODO: This can probably be simplified to avoid running an entire command
            CommandData data2 = default(CommandData); data2.Context = CommandContext.MessageBlock;
            ScriptData scriptData = Core.GetScriptData(p);
            scriptData.SetString("denyAccess", "", false, lvl.name);
            Command.Find("script").Use(p, lvl.name + " #accessControl", data2); //not using p.HandleCommand because it needs to all be in one thread

            if (scriptData.GetString("denyAccess", false, lvl.name).ToLower() == "true") {
                canJoin = false;
                lvl.AutoUnload();
            }
        }
        public static void OnLevelRenamed(string srcMap, string dstMap) {
            string srcPath = PATH_OS + srcMap + EXT;
            string dstPath = PATH_OS + dstMap + EXT;
            if (!File.Exists(srcPath)) { return; }
            File.Move(srcPath, dstPath);
        }
        public static void OnLevelUnload(Level level, ref bool cancel) {
            if (!IsOs(level)) { return; } //only care about OS maps

            bool removed;
            lock (locker) { removed = loadedScripts.Remove(OS_PREFIX + level.name); }
            if (removed) { Logger.Log(LogType.BackgroundActivity, "Unloaded OS map script {0}", level.name); }
        }
        public static void OnPlayerCommand(Player p, string cmd, string message, CommandData data) {
            string calledLabel;
            bool async;
            if (cmd.CaselessEq("input")) { calledLabel = "#input"; async = false; } else if (cmd.CaselessEq("inputAsync")) { calledLabel = "#inputAsync"; async = true; } else { return; }

            string scriptCmdName;
            string scriptName = p.level.name;
            bool isOS;
            if (IsOs(p.level)) { //assume os script
                scriptCmdName = Core.osRunscriptCmd.name;
                scriptName = OS_PREFIX + scriptName;
                isOS = true;
            } else {
                scriptCmdName = Core.runscriptCmd.name;
                scriptName = p.level.GetExtraPropString("input", p.level.name);
                isOS = false;
            }

            if (!File.Exists(FullPath(scriptName))) {
                //This message can't show up anymore because we need to let Cmdinput run for old maps if this event doesn't trigger anything
                //p.Message("%T/Input &Sis not used in {0}.", p.level.name);

                //don't want to show "unknown command" message
                if (async) { p.cancelcommand = true; }
                return;
            }

            string[] runArgs = message.SplitSpaces(2);
            string runArg1 = runArgs[0];
            string runArg2 = runArgs.Length > 1 ? runArgs[1] : "";
            runArg1 = runArg1.Replace(" ", "_");
            runArg2 = runArg2.Replace(" ", "_");
            string args = calledLabel + "|" + runArg1 + "|" + runArg2;
            if (!isOS) { args = scriptName + " " + args; } //if not OS, add possibly custom script name to command args
            if (async) { args = args + " repeatable"; }

            data.Context = CommandContext.MessageBlock;
            p.HandleCommand(scriptCmdName, args, data);

            // this must be placed after HandleCommand because it runs GetCommand which sets p.cancelcommand back to true
            p.cancelcommand = true;
        }

        static bool IsOs(Level level) { return level.name.Contains("+"); }

        private Script () {
            compileDate = DateTime.UtcNow;
        }
        private DateTime compileDate;
        private Dictionary<string, int> labels = new Dictionary<string, int>();
        public bool HasLabel(string label) { return labels.ContainsKey(label); }
        public bool GetLabel(string label, out int actionIndex) { return labels.TryGetValue(label, out actionIndex); }

        public List<ScriptLine> actions = new List<ScriptLine>();

    }

    //Constructor
    public partial class ScriptRunner {
        
        public static ScriptRunner Create(Player p, string scriptName, bool isOS, CommandData data, bool repeatable, string thisBool, string[] runArgs) {
            ScriptRunner runner = new ScriptRunner();

            runner.script = Script.Get(p, scriptName);
            if (runner.script == null) { return null; }

            runner.p = p;
            runner.scriptData = Core.GetScriptData(runner.p);
            runner.startingLevel = p.level;
            runner.startingLevelName = p.level.name;
            runner.scriptName = scriptName;
            runner.isOS = isOS;
            runner.data = data;
            runner.repeatable = repeatable;
            runner.thisBool = thisBool;
            runner.runArgs = runArgs;
            
            return runner;
        }
    }
    //Fields
    public partial class ScriptRunner {
        
        static char[] pipeChar = new char[] { '|' };
        
        const int actionLimit = 61360 * 4;
        const int actionLimitOS = 61360;
        const int newThreadLimit = 20;
        const int newThreadLimitOS = 10;
        
        const CpeMessageType bot1 = CpeMessageType.BottomRight3;
        const CpeMessageType bot2 = CpeMessageType.BottomRight2;
        const CpeMessageType bot3 = CpeMessageType.BottomRight1;
        const CpeMessageType top1 = CpeMessageType.Status1;
        const CpeMessageType top2 = CpeMessageType.Status2;
        const CpeMessageType top3 = CpeMessageType.Status3;
        
        static CpeMessageType GetCpeMessageType(string type) {
            if (type == "bot1") { return bot1; }
            if (type == "bot2") { return bot2; }
            if (type == "bot3") { return bot3; }
            if (type == "top1") { return top1; }
            if (type == "top2") { return top2; }
            if (type == "top3") { return top3; }
            if (type == "announce") { return CpeMessageType.Announcement; }
            if (type == "bigannounce") { return CpeMessageType.BigAnnouncement; }
            if (type == "smallannounce") { return CpeMessageType.SmallAnnouncement; }
            return CpeMessageType.Normal;
        }

        Script script;

        Player p;
        ScriptData scriptData;
        Level startingLevel;
        string startingLevelName;
        string scriptName;
        bool isOS = false;
        bool repeatable = false;
        string thisBool;
        CommandData data;
        int actionIndex;
        List<int> comebackToIndex = new List<int>();
        List<Thread> newthreads = new List<Thread>();
        int actionCounter;
        int newThreadNestLevel;
        int lineNumber = -1;
        bool hasCef = false;
        int amountOfCharsInLastMessage = 0;
        string[] runArgs;
    }
    //Misc functions
    public partial class ScriptRunner {
        
        static Random rnd = new Random();
        static readonly object rndLocker = new object();
        static int RandomRange(int inclusiveMin, int inclusiveMax) {
            lock (rndLocker) { return rnd.Next(inclusiveMin, inclusiveMax+1); }
        }
        static double RandomRangeDouble(double min, double max) {
            lock (rndLocker) { return (rnd.NextDouble() * (max - min) + min); }
        }
        static string RandomEntry(string[] entries) {
            lock (rndLocker) { return entries[rnd.Next(entries.Length)]; }
        }
        
        static byte? GetEnvColorType(string type) {
            if (type.CaselessEq("sky"))    { return 0; }
            if (type.CaselessEq("cloud"))  { return 1; }
            if (type.CaselessEq("clouds")) { return 1; }
            if (type.CaselessEq("fog"))    { return 2; }
            if (type.CaselessEq("shadow")) { return 3; }
            if (type.CaselessEq("sun"))    { return 4; }
            if (type.CaselessEq("skybox")) { return 5; }
            return null;
        }
        static byte? GetEnvWeatherType(string type) {
            if (type.CaselessEq("sun"))  { return 0; }
            if (type.CaselessEq("rain")) { return 1; }
            if (type.CaselessEq("snow")) { return 2; }
            return null;
        }
        static EnvProp? GetEnvMapProperty(string prop) {
            if (prop.CaselessEq("maxfog")) { return EnvProp.MaxFog; }
            if (prop.CaselessEq("expfog")) { return EnvProp.ExpFog; }
            if (prop.CaselessEq("cloudsheight") || prop.CaselessEq("cloudheight")) { return EnvProp.CloudsLevel; }
            if (prop.CaselessEq("cloudspeed") || prop.CaselessEq("cloudspeed")) { return EnvProp.CloudsSpeed; }
            return null;
        }
        void SetEnv(string message) {
            if (message.Length == 0) { Error(); p.Message("&cNo args provided for env"); return; }
            string[] args = message.SplitSpaces(2);
            if (args.Length < 2) { Error(); p.Message("&cYou must provide an argument for type of env property to change as well as value"); return; }
            string prop = args[0];
            string valueString = args[1];
            byte? type = GetEnvColorType(prop);
            if (type != null) {
                p.Session.SendSetEnvColor((byte)type, valueString);
                return;
            }
            if (prop.CaselessEq("weather")) {
                type = GetEnvWeatherType(valueString);
                if (type != null) { p.Session.SendSetWeather((byte)type); return; }
                Error(); p.Message("&cEnv weather type \"{0}\" is not currently supported.", valueString);
                return;
            }
            
            EnvProp? envPropType = GetEnvMapProperty(prop);
            if (envPropType != null) {
                if (envPropType == EnvProp.ExpFog) {
                    bool yesno = false;
                    if (CommandParser.GetBool(p, valueString, ref yesno)) {
                        p.Send(Packet.EnvMapProperty((EnvProp)envPropType, yesno ? 1 : 0) );
                    }
                    return;
                }
                int value = 0;
                
                if (CommandParser.GetInt(p, valueString, "env int value", ref value)) {
                    if (envPropType == EnvProp.CloudsSpeed) { value *= 256; }
                    p.Send(Packet.EnvMapProperty((EnvProp)envPropType, value));
                }
                return;
            }
            
            
            Error(); p.Message("&cEnv property \"{0}\" is not currently supported.", prop);
        }

        
        bool ValidateCommand(ref string cmdName, ref string cmdArgs, out Command cmd) {
            cmd = null;
            Command.Search(ref cmdName, ref cmdArgs);
            if (isOS && (cmdName.CaselessEq("runscript") || cmdName.CaselessEq(Core.runscriptCmd.name))) {
                Error(); p.Message("%cCommand \"{0}\" is blacklisted from being used in scripts.");
                return false;
            }
            cmd = Command.Find(cmdName);
            if (cmd == null) {
                Error(); p.Message("&cCould not find command \"{0}\"", cmdName);
                return false;
            }
            return true;
        }
        CommandData GetCommandData() {
            CommandData data = default(CommandData);
            data.Context = CommandContext.MessageBlock;
            data.Rank = isOS ? LevelPermission.Guest : LevelPermission.Nobody;
            data.MBCoords = this.data.MBCoords;
            return data;
        }
        void DoCmd(Command cmd, string args) {
            CommandData data = GetCommandData();
            if (isOS && cmd.MessageBlockRestricted) {
                Error(); p.Message("&c\"{0}\" cannot be used in message blocks.", cmd.name);
                p.Message("&cTherefore, it cannot be ran in a script.");
                return;
            }
            CommandPerms perms = CommandPerms.Find(cmd.name);
            if (!perms.UsableBy(data.Rank)) {
                Error(); p.Message("&cOs scripts can only run commands with a permission of member or lower.", data.Rank);
                p.Message("&cTherefore, \"{0}\" cannot be ran.", cmd.name);
                return;
            }
            try {
                cmd.Use(p, args, data);
            } catch (Exception ex) {
                Logger.LogError(ex);
                Error(); p.Message("&cAn error occured and command {0} could not be executed.", cmd.name);
            }
        }
        void DoCmdNoPermChecks(Command cmd, string args) {
            try {
                cmd.Use(p, args, GetCommandData());
            } catch (Exception ex) {
                Logger.LogError(ex);
                Error(); p.Message("&cAn error occured and command {0} could not be executed.", cmd.name);
            }
        }
        void DoNewThreadRunScript(ScriptRunner script, string startLabel) {
            Thread thread = new Thread(
                    () => {
                        try {
                            script.Run(startLabel, newThreadNestLevel+1);
                        } catch (Exception ex) {
                            Logger.LogError(ex);
                            Error(); p.Message("&cAn error occured and newthread {0} could not be executed.", startLabel);
                        }
                    }
                );
            thread.Name = "MCG_RunscriptNewThread";
            thread.IsBackground = true;
            thread.Start();
            newthreads.Add(thread);
        }
        static bool GetBoolResultFromIntCompare(double doubleValue, string condition, double doubleValueCompared, out bool scriptError) {
            scriptError = false;
            if (condition == "=")  { return doubleValue == doubleValueCompared; }
            if (condition == "!=") { return doubleValue != doubleValueCompared; }
            if (condition == "<")  { return doubleValue <  doubleValueCompared; }
            if (condition == "<=") { return doubleValue <= doubleValueCompared; }
            if (condition == ">")  { return doubleValue >  doubleValueCompared; }
            if (condition == ">=") { return doubleValue >= doubleValueCompared; }
            scriptError = true;
            return false;
        }
        // Used in OnPlayerMove when player is forced to stare
        public static void LookAtCoords(Player p, Vec3S32 coords) {
            //we want to calculate difference between player's (eye position)
            //and block's position to use in GetYawPitch
            
            //convert block coords to player units
            coords*= 32;
            //center of the block
            coords+= new Vec3S32(16, 16, 16);
            
            int dx = coords.X - p.Pos.X;
            int dy = coords.Y - (p.Pos.Y - Entities.CharacterHeight + ModelInfo.CalcEyeHeight(p));
            int dz = coords.Z- p.Pos.Z;
            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            dir = Vec3F32.Normalise(dir);
            
            byte yaw, pitch;
            DirUtils.GetYawPitch(dir, out yaw, out pitch);
            byte[] packet = new byte[4];
            packet[0] = Opcode.OrientationUpdate; packet[1] = Entities.SelfID; packet[2] = yaw; packet[3] = pitch;
            p.Send(packet);
        }
        
        static void TryReplaceRunArgs(string[] newRunArgs, ref string[] runArgs) {
            if (newRunArgs.Length < 2) { return; }
            
            ReplaceUnderScoreWithSpaceInRunArgs(ref newRunArgs);
            
            string originalStartLabel = runArgs[0];
            runArgs = (string[])newRunArgs.Clone();
            runArgs[0] = originalStartLabel;
        }
        static string runArgSpaceSubstitute = "_";
        static void ReplaceUnderScoreWithSpaceInRunArgs(ref string[] runArgs) {
            for (int i = 0; i < runArgs.Length; i++) {
                if (i == 0) { continue; } //do not replace underscores with spaces in startLabel which is always runArgs[0]
                runArgs[i] = runArgs[i].Replace(runArgSpaceSubstitute, " ");
            }
        }
    }
    //Main body
    public partial class ScriptRunner {
        
        public static void PerformScript(Player p, string scriptName, string startLabel, bool isOS, bool repeatable, CommandData data) {
            //pass in string arguments after label saparated by | characters
            string[] runArgs = startLabel.Split('|');
            startLabel = runArgs[0];
            ReplaceUnderScoreWithSpaceInRunArgs(ref runArgs);
            
            string thisBool = "RunningScript" + scriptName + startLabel;
            if (!repeatable && p.Extras.GetBoolean(thisBool)) {
                //p.Message("can't repeat while running");
                return;
            }
            try {
                if (!repeatable) p.Extras[thisBool] = true;
                ScriptRunner scriptRunner = ScriptRunner.Create(p, scriptName, isOS, data, repeatable, thisBool, runArgs);
                if (scriptRunner != null) {
                    scriptRunner.Run(startLabel);
                }
            } finally {
                if (!repeatable) p.Extras[thisBool] = false;
            }
        }
        
        void Run(string startLabel, int newThreadNestLevel = 1) {
            
            actionCounter = 0;
            this.newThreadNestLevel = newThreadNestLevel;
            
            if (newThreadNestLevel > (isOS ? newThreadLimitOS : newThreadLimit)) {
                p.Message("&cScript error: You cannot call a newthread from another newthread more than 10 times in a row.");
                return;
            }
            if (!script.GetLabel(startLabel, out actionIndex)) {
                if (startLabel == "#input") {
                    p.Message("%T/Input &Sis not used in {0}.", p.level.name);
                    return;
                }
                if (startLabel == "#accessControl") {
                    return;
                }
                p.Message("&cScript error: unknown starting label \"" + startLabel + "\".");
                p.Message(CmdScript.labelHelp);
                return;
            }
            
            
            //if player has cef do:
            //bool cef true
            if (p.Session.appName != null && p.Session.appName.Contains("+ cef")) {
                hasCef = true;
                SetString("cef", "true");
            }
            if (p.Session.appName != null && (p.Session.appName.CaselessContains("mobile") || p.Session.appName.CaselessContains("android"))) {
                SetString("mobile", "true");
            }
            if (p.Session.appName != null && p.Session.appName.CaselessContains("web")) {
                SetString("webclient", "true");
            }
            
            SetString("msgdelaymultiplier", "50");
            
            if (scriptData.debugging) {
                p.Message("&lDebug: &6starting {0} at {1}", scriptName, startLabel);
            }
            
            ScriptLine lastAction = new ScriptLine();
            lastAction.actionType = ScriptRunner.ActionType.None;
            while (actionIndex < script.actions.Count) {
                
                if (p.Socket.Disconnected) { return; }
                
                if (startingLevelName != p.level.name.ToLower()) {
                    //cancel script if the action isn't quit or terminate
                    if (!(
                        script.actions[actionIndex].actionType == ScriptRunner.ActionType.Quit ||
                        script.actions[actionIndex].actionType == ScriptRunner.ActionType.Terminate
                        )
                        ) {
                        p.Message("%eScript note: Script cancelled due to switching maps.");
                    }
                    return;
                }
                
                lastAction = script.actions[actionIndex];
                
                //RunScriptLines increments actionIndex, but it may also modify it (goto, call, quit)
                RunScriptLine(script.actions[actionIndex]);
                
                
                //we need to make sure scriptActions is still in range, because quit from RunScriptLine sets the index to scriptActions.Count
                if (actionIndex < script.actions.Count) {
                    //if the last action was Reply and this action isn't, that means we're done setting up replies for now and should show the text of how to reply.
                    if (
                        script.actions[actionIndex].actionType != ScriptRunner.ActionType.Reply &&
                                        lastAction.actionType == ScriptRunner.ActionType.Reply
                        ) {
                        //only tell if it's not being cleared
                        if (!lastAction.actionArgs.CaselessEq("clear")) { CmdReplyTwo.SetUpDone(p); }
                    }
                }
                
                actionCounter++;
                int limit = isOS ? actionLimitOS : actionLimit;
                if (actionCounter > limit) {
                    p.Message("&cScript error: Over {0} actions have been performed! Assuming endless loop and aborting.", limit);
                    p.Message("&cLast line number ran was: {0}", lineNumber);
                    break;
                }
            }
            //p.Message("Script finished after performing {0} actions.", actionCounter);

        }
        
        bool ShouldDoLine(ScriptLine line) {
            if (line.conditionLogic == ScriptLine.ConditionLogic.None) { return true; }
            //p.Message("&b{0}:&r {1}, {2}, \"{3}\", \"{4}\"", line.lineNumber, line.conditionLogic, line.conditionType, line.conditionArgs, line.actionArgs);
            
            string parsedConditionArgs = ParseMessage(line.conditionArgs);
            bool doAction = false;
            
            //handle item
            if (line.conditionType == ScriptLine.ConditionType.Item) {
                doAction = scriptData.HasItem(parsedConditionArgs, isOS);
                goto end;
            }
            
            
            string[] ifBits = parsedConditionArgs.Split(pipeChar);
            string packageName = ifBits[0];
            
            
            //handle bool
            if (ifBits.Length == 1) {
                if (GetString(packageName).CaselessEq("true")) { doAction = true; }
                goto end;
            }
            
            if (ifBits.Length != 3) {
                Error(); p.Message("&c\"" + parsedConditionArgs + "\" is not a valid if statement.");
                p.Message("&cIf statement only accepts 1 argument or 3 arguments separated by |.");
                return false;
            }
            string equality = ifBits[1];
            string comparedToArg = ifBits[2];
            
            
            //handle double
            double packagedoubleValue;
            double doubleValueCompared;
            if (GetDouble(packageName, out packagedoubleValue, false) && GetDoubleRawOrVal(comparedToArg, "", out doubleValueCompared, false)) {
                bool scriptError = false;
                doAction = GetBoolResultFromIntCompare(packagedoubleValue, equality, doubleValueCompared, out scriptError);
                if (scriptError) { Error(); p.Message("&cInvalid number comparison equality \"" + equality + "\"."); return false; }
                goto end;
            }
            
            //handle string
            if (equality == "=") {
                doAction = GetString(packageName).CaselessEq(GetString(comparedToArg)); goto end;
            } else {
                Error(); p.Message("You can only use = as a comparison equality when comparing non-numbers."); return false;
            }

            
            end:
            if (line.conditionLogic == ScriptLine.ConditionLogic.IfNot) { doAction = !doAction; }
            return doAction;
        }
        
        string args;
        string cmdName;
        string cmdArgs;
        void RunScriptLine(ScriptLine line) {
            actionIndex++;
            lineNumber = line.lineNumber;
            if (!ShouldDoLine(line)) { return; }
            
            args = ParseMessage(line.actionArgs);
            string[] bits = args.SplitSpaces(2);
            cmdName = bits[0];
            cmdArgs = bits.Length > 1 ? bits[1] : "";
            
            if (scriptData.debugging) {
                p.Message("&lDebug: {0}", line.actionType.ToString());
                
                if (line.actionArgs != args) { p.Message("  {0}", line.actionArgs); }
                                               p.Message("  {0}", args);
            }
            Actions[(int)line.actionType]();
            
            if (scriptData.debugging && scriptData.debuggingDelay > 0) { Thread.Sleep(scriptData.debuggingDelay); }
        }
        
        bool GetDouble(string valName, out double doubleValue, bool throwError = true) {
            doubleValue = 0;
            string value = GetString(valName);
            if (value.Length == 0) { return true; } //treat an empty or undefined value as 0
            if (!double.TryParse(value, out doubleValue)) {
                if (throwError) { Error(); p.Message("&c{0} = \"{1}\", therefore it cannot be used as a number.", valName, value); }
                return false;
            }
            return true;
        }
        void SetDouble(string valName, double value) {
            SetString(valName, value.ToString());
        }
        
        string GetString(string stringName) {
            if (p == null) { throw new System.Exception("Player null in GetString new pdb pls"); }
            
            if (stringName.Length > 6 && stringName.CaselessStarts("runArg")) {
                int runArgIndex;
                if (!Int32.TryParse(stringName.Substring(6), out runArgIndex) ) { goto fuckyou; }
                if (runArgIndex < runArgs.Length) { return runArgs[runArgIndex]; }
            }
            fuckyou:
            
            if (stringName.CaselessEq("mbx"     ))    { return this.data.MBCoords.X.ToString(); }
            if (stringName.CaselessEq("mby"     ))    { return this.data.MBCoords.Y.ToString(); }
            if (stringName.CaselessEq("mbz"     ))    { return this.data.MBCoords.Z.ToString(); }
            if (stringName.CaselessEq("playerx" ))    { return p.Pos.FeetBlockCoords.X.ToString(); }
            if (stringName.CaselessEq("playery" ))    { return p.Pos.FeetBlockCoords.Y.ToString(); }
            if (stringName.CaselessEq("playerz" ))    { return p.Pos.FeetBlockCoords.Z.ToString(); }
            if (stringName.CaselessEq("playerpx"))    { return p.Pos.X.ToString(); }
            if (stringName.CaselessEq("playerpy"))    { return (p.Pos.Y-Entities.CharacterHeight).ToString(); }
            if (stringName.CaselessEq("playerpz"))    { return p.Pos.Z.ToString(); }
            if (stringName.CaselessEq("playeryaw"))   { return Orientation.PackedToDegrees(p.Rot.RotY).ToString();  } //yaw
            if (stringName.CaselessEq("playerpitch")) { return Orientation.PackedToDegrees(p.Rot.HeadX).ToString(); } //pitch
            
            if (stringName.CaselessEq("msgdelay")) {
                double msgDelayMultiplier = 0;
                double.TryParse(GetString("msgDelayMultiplier"), out msgDelayMultiplier);
                return (amountOfCharsInLastMessage * msgDelayMultiplier).ToString();
            }
            
            if (stringName.CaselessEq("mbcoords")) {
                return this.data.MBCoords.X+" "+this.data.MBCoords.Y+" "+this.data.MBCoords.Z;
            }
            //feet block coords
            if (stringName.CaselessEq("playercoords")) {
                Vec3S32 pos = p.Pos.FeetBlockCoords;
                return pos.X + " " + pos.Y + " " + pos.Z;
            }
            //double coords for tempbot, particle, etc
            if (stringName.CaselessEq("playercoordsdecimal")) {
                double X = (p.Pos.X / 32f) - 0.5f;
                double Y = ((p.Pos.Y-Entities.CharacterHeight) / 32f);
                double Z = (p.Pos.Z / 32f) - 0.5f;
                return X + " " + Y + " " + Z;
            }
            //tpp units
            if (stringName.CaselessEq("playercoordsprecise")) { return p.Pos.X + " " + (p.Pos.Y-Entities.CharacterHeight) + " " + p.Pos.Z; }
            
            if (stringName.CaselessEq("epochMS")) { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(); }
            
            return scriptData.GetString(stringName, isOS, scriptName);
        }
        void SetString(string stringName, string value) {
            scriptData.SetString(stringName, value, isOS, scriptName);
        }
        
        const char beginParseSymbol = '{';
        const char endParseSymbol = '}';
        const int maxRecursions = 32;
        string ParseMessage(string message, int recursions = 0) {
            if (recursions == 0) { message = ReplaceAts(message); }
            else if (recursions > maxRecursions) {
                Error(); p.Message("More than {0} recursions occured while trying to unwrap package value(s) from: \"{1}\"", maxRecursions, message);
                p.Message("To prevent an infinite loop, this is not allowed.");
                return message;
            }
            int beginIndex = message.IndexOf(beginParseSymbol); int endIndex = message.IndexOf(endParseSymbol);
            if (beginIndex == -1 || endIndex == -1) { return message; }
            if (beginIndex > endIndex) {
                Error(); p.Message("A closing curly bracket should not appear before an opening curly bracket."); return message;
            }
            
            //       oBI                         
            //        4         9 10  12        1718       
            //H e y _ { n a m e } , _ { m o o d } ? 
            
            //Hey_{name},_{mood}?
            
            StringBuilder parsed = new StringBuilder(message.Length);
            
            int openingBracketIndex = -1;
            int indexOfEndOfLastParse = 0;
            for (int i = 0; i < message.Length; i++) {
                char curChar = message[i];
                if (curChar == beginParseSymbol) { openingBracketIndex = i; }
                else if (curChar == endParseSymbol) {
                    if (openingBracketIndex == -1) {
                        continue;
                    }
                    
                    parsed.Append(message.Substring(indexOfEndOfLastParse, openingBracketIndex - indexOfEndOfLastParse)); //"Hey_" / ",_"
                    
                    openingBracketIndex++;
                    //begin at index 5 for a length of 4 characters
                    parsed.Append( GetString(message.Substring(openingBracketIndex, i - openingBracketIndex )) ); //"name" / "mood"
                    
                    indexOfEndOfLastParse = i+1; //set the "start" to index 10
                    openingBracketIndex = -1;
                }
            }
            //last part
            parsed.Append(message.Substring(indexOfEndOfLastParse, message.Length - indexOfEndOfLastParse)); // "?"
            
            //return parsed.ToString();
            return ParseMessage(parsed.ToString(), recursions + 1);
        }
        
        string ReplaceAts(string message) {
            if (message.IndexOf('@') == -1) { return message; }
            message = message.Replace("@p", p.name);
            message = message.Replace("@nick", NameUtils.MakeNatural(p.DisplayName));
            return message;
        }
        
        void Error(bool above = false) {
            if (above) { p.Message("&c↑ The above message came from a script error on line "+lineNumber+"."); }
            else { p.Message("&cScript error on line "+lineNumber+":"); }
            Terminate();
        }
    }
    //Actions
    public partial class ScriptRunner {
        ///No one should use this except GetScript. GetScript should be used to create a new script instance.
        private ScriptRunner() {
            Actions = new ScriptAction[] {
            None, Message, CpeMessage, Delay, Goto, Jump, Call,
            Set, SetAdd, SetSub, SetMul, SetDiv, SetMod, SetRandRange, SetRandRangeDecimal, SetRandList, SetRound, SetRoundUp, SetRoundDown,
            Quit, Terminate, Show, Kill, Cmd, ResetData, Item,
            Freeze, Unfreeze, Look, Stare, NewThread, Env, MOTD, SetSpawn, Reply, ReplySilent,
            TempBlock, TempChunk, Reach, SetBlockID,
            DefineHotkey, UndefineHotkey, PlaceBlock, ChangeModel, SetDirVector,
            Boost, Effect
            };
        }
        public enum ActionType : int {
            None, Message, CpeMessage, Delay, Goto, Jump, Call,
            Set, SetAdd, SetSub, SetMul, SetDiv, SetMod, SetRandRange, SetRandRangeDecimal, SetRandList, SetRound, SetRoundUp, SetRoundDown,
            Quit, Terminate, Show, Kill, Cmd, ResetData, Item,
            Freeze, Unfreeze, Look, Stare, NewThread, Env, MOTD, SetSpawn, Reply, ReplySilent,
            TempBlock, TempChunk, Reach, SetBlockID,
            DefineHotkey, UndefineHotkey, PlaceBlock, ChangeModel, SetDirVector,
            Boost, Effect
        }
        ScriptAction[] Actions;
        
        //is this silly?
        void None() { }
        void Message() {
            if (!hasCef && args.StartsWith("cef ")) { return; }
            amountOfCharsInLastMessage = args.Length;
            p.Message(args);
        }
        public static PersistentMessagePriority CpeMessagePriority = PersistentMessagePriority.High;
        void CpeMessage() {
            if (cmdName == "") { Error(); p.Message("&cNot enough arguments for cpemsg"); return; }
            CpeMessageType type = GetCpeMessageType(cmdName);
            if (type == CpeMessageType.Announcement || type == CpeMessageType.BigAnnouncement || type == CpeMessageType.SmallAnnouncement) {
                amountOfCharsInLastMessage = cmdArgs.Length;
            }
            p.SendCpeMessage(type, cmdArgs, CpeMessagePriority);
        }
        bool GetIntRawOrVal(string arg, string actionName, out int value) {
            if (!Int32.TryParse(arg, out value)) {
                string stringValue = GetString(arg);
                if (stringValue == "") { stringValue = "0"; }
                if (!Int32.TryParse(stringValue, out value) )
                { Error(); p.Message("&cAction {0} only takes integer values and \"{1}={2}\" is not an integer.", actionName, arg, stringValue); return false; }
            }
            return true;
        }
        bool GetDoubleRawOrVal(string arg, string actionName, out double value, bool throwError = true) {
            if (!double.TryParse(arg, out value)) {
                string stringValue = GetString(arg);
                if (stringValue == "") { stringValue = "0"; }
                if (!double.TryParse(stringValue, out value) ) {
                    if (throwError) { Error(); p.Message("&cAction {0} only takes number values and \"{1}={2}\" is not a number.", actionName, arg, stringValue); }
                    return false;
                }
            }
            return true;
        }
        void Delay() {
            int delay;
            if (!GetIntRawOrVal(cmdName, "Delay", out delay)) { return; }
            Thread.Sleep(delay);
        }
        void Goto() {
            comebackToIndex.Clear();
            Jump();
        }
        void Jump() {
            //cmdName is label|runArg|runArg
            string[] newRunArgs = (cmdName).Split(pipeChar);
            TryReplaceRunArgs(newRunArgs, ref runArgs);
            
            if (!script.GetLabel(newRunArgs[0], out actionIndex)) {
                Error(); p.Message("&cUnknown label \"" + newRunArgs[0] + "\".");
                p.Message(CmdScript.labelHelp);
            }
        }
        void Call() {
            comebackToIndex.Add(actionIndex);
            Jump();
        }
        void NewThread() {
            if (cmdName.Length == 0) { Error(); p.Message("&cPlease specify a label and or runArgs for the newthread to run with."); return; }
            
            string[] newThreadRunArgs = (cmdName).Split(pipeChar);
            string newThreadLabel = newThreadRunArgs[0];
            if (newThreadRunArgs.Length == 1) {
                //no new runArgs were specified
                newThreadRunArgs = (string[])runArgs.Clone(); //clone it so changing args in newthread doesn't change them in this script, yikes
            } else {
                //they specified new runArgs
                newThreadRunArgs[0] = runArgs[0]; //preserve starting label from original script instance
                ReplaceUnderScoreWithSpaceInRunArgs(ref newThreadRunArgs);
            }
            
            if (!script.HasLabel(newThreadLabel)) { Error(); p.Message("&cUnknown newthread label \"" + newThreadLabel + "\"."); return; }
            ScriptRunner scriptRunner = Create(p, scriptName, isOS, data, repeatable, thisBool, newThreadRunArgs);
            if (scriptRunner == null) { Error(); return; }
            DoNewThreadRunScript(scriptRunner, newThreadLabel);
        }
        void Set() {
            SetString(cmdName, cmdArgs);
        }
        bool ValidateNumberOperation(out double doubleValue, out double doubleValue2) {
            doubleValue2 = 0;
            if (!GetDouble(cmdName, out doubleValue)) { return false; }
            if (double.TryParse(cmdArgs, out doubleValue2)) { return true; } //second arg is already a number, return its value
            return GetDouble(cmdArgs, out doubleValue2); //second arg is not a number and is therefore assumed to be a valueName. Try to get the value of the valueName.
        }
        delegate double ArithmeticOp(double a, double b);
        void SetArithmeticOp(ArithmeticOp op) {
            double a, b, result;
            if (!ValidateNumberOperation(out a, out b)) { return; }

            try {
                result = op(a, b);
            } catch (DivideByZeroException) {
                Error(); p.Message("&cCannot divide {0}={1} by {2}={3} because division by zero does not result in a real number.", cmdName, a, cmdArgs, b);
                return;
            }
            SetDouble(cmdName, result);
        }
        void SetAdd() { SetArithmeticOp((a,b) => a + b); }
        void SetSub() { SetArithmeticOp((a,b) => a - b); }
        void SetMul() { SetArithmeticOp((a,b) => a * b); }
        void SetDiv() {
            SetArithmeticOp((a,b) => { if (b == 0) throw new DivideByZeroException(); return a / b; });
        }
        void SetMod() {
            SetArithmeticOp((a,b) => {
                if (b == 0) throw new DivideByZeroException();
                
                //https://stackoverflow.com/questions/1082917/mod-of-negative-number-is-melting-my-brain
                double r = a % b;
                return r < 0 ? r + b : r;
                });
        }
        void SetRandRange() {
            string[] bits = args.SplitSpaces();
            if (bits.Length < 3) { Error(); p.Message("&cNot enough arguments for SetRandRange action"); return; }
            int min, max;
            if (!GetIntRawOrVal(bits[1], "SetRandRange", out min)) { return; }
            if (!GetIntRawOrVal(bits[2], "SetRandRange", out max)) { return; }
            if (min > max) { Error(); p.Message("&cMin value for SetRandRange must be smaller than max value."); return; }
            SetDouble(bits[0], RandomRange(min, max));
        }
        void SetRandRangeDecimal() {
            string[] bits = args.SplitSpaces();
            if (bits.Length < 3) { Error(); p.Message("&cNot enough arguments for SetRandRangeDecimal action"); return; }
            double min, max;
            if (!GetDoubleRawOrVal(bits[1], "SetRandRangeDecimal", out min)) { return; }
            if (!GetDoubleRawOrVal(bits[2], "SetRandRangeDecimal", out max)) { return; }
            if (min > max) { Error(); p.Message("&cMin value for SetRandRangeDecimal must be smaller than max value."); return; }
            SetDouble(bits[0], RandomRangeDouble(min, max));
        }
        void SetRandList() {
            if (cmdArgs == "") { Error(); p.Message("SetRandList requires a list of values to choose from separated by the | character."); return; }
            SetString(cmdName, RandomEntry(cmdArgs.Split(pipeChar)));
        }
        delegate double RoundingOp(double value);
        void DoRound(RoundingOp op) {
            double value;
            if (cmdName.Length == 0) { Error(); p.Message("&cNo value provided to round."); return; }
            if (!GetDouble(cmdName, out value)) { return; }
            SetDouble(cmdName, (int)op(value));
        }
        void SetRound()     { DoRound((value) => Math.Round(value, MidpointRounding.AwayFromZero)); }
        void SetRoundUp()   { DoRound((value) => Math.Ceiling(value)); }
        void SetRoundDown() { DoRound((value) => Math.Floor(value)); }
        void Quit() {
            if (comebackToIndex.Count == 0) {
                Terminate();
            } else {
                //make the index where it left off
                actionIndex = comebackToIndex[comebackToIndex.Count -1];
                comebackToIndex.RemoveAt(comebackToIndex.Count -1);
            }
        }
        void Terminate() {
            //make the index at the end so it's completely finished
            actionIndex = script.actions.Count;
            foreach (Thread thread in newthreads) {
                thread.Join();
            }
            //p.Message("Putting thisBool to false and quitting the entire script.");
            if (!repeatable) p.Extras[thisBool] = false;
        }
        void Show() {
            if (args.CaselessEq("every single package")) {
                scriptData.ShowAllStrings();
                return;
            }
            string[] values = args.SplitSpaces();
            bool saved;
            
            foreach (string value in values) {
                p.Message("The value of &b{0} &Sis \"&o{1}&S\".", scriptData.ValidateStringName(value, isOS, scriptName, out saved), GetString(value));
            }
        }
        void Kill() {
            p.HandleDeath(Block.Cobblestone, args, false, true);
        }
        //Can't name it Command otherwise it's ambiguous between Script.Command and MCGalaxy.Command
        void Cmd() {
            Command cmd = null;
            if (!ValidateCommand(ref cmdName, ref cmdArgs, out cmd)) {
                return;
            }
            DoCmd(cmd, cmdArgs);
        }
        void ResetData() {
            if (cmdName.CaselessStarts("packages")) {
                scriptData.Reset(true, false, cmdArgs);
                return;
            }
            if (cmdName.CaselessStarts("items")) {
                if (!isOS) { Error(); p.Message("&cCannot reset items in non-os script"); return; }
                scriptData.Reset(false, true, cmdArgs);
                return;
            }
            if (cmdName.CaselessStarts("saved")) {
                if (isOS) { Error(); p.Message("&cCannot reset saved packages in os script (because there are none)"); return; }
                scriptData.ResetSavedStrings(scriptName, cmdArgs);
                return;
            }
            Error(); p.Message("&cYou must specify what type of data to reset");
        }
        void Item() {
            if (cmdArgs == "") { Error(); p.Message("&cNot enough arguments for Item action"); return; }
            if (cmdName == "get" || cmdName == "give")    { scriptData.GiveItem(cmdArgs, isOS); return; }
            if (cmdName == "take" || cmdName == "remove") { scriptData.TakeItem(cmdArgs, isOS); return; } 
            Error(); p.Message("&cUnknown function for Item action: \"{0}\"", cmdName);
        }
        void Freeze() {
            scriptData.frozen = true;
            p.Send(Packet.Motd(p, "-hax horspeed=0.000001 jumps=0 -push"));
        }
        void Unfreeze() {
            scriptData.frozen = false;
            if (scriptData.customMOTD != null) {
                SendMOTD(scriptData.customMOTD);
            } else {
                // do not use p.SendMapMotd() because it triggers the event which makes locked model update, which we don't want
                p.Send(Packet.Motd(p, p.GetMotd()));
            }
        }
        bool GetCoords(string actionName, out Vec3S32 coords) {
            string[] stringCoords = args.SplitSpaces();
            coords = new Vec3S32();
            if (stringCoords.Length < 3) { Error(); p.Message("&cNot enough arguments for {0}", actionName); return false; }
            if (!CommandParser.GetCoords(p, stringCoords, 0, ref coords)) { Error(true); return false; }
            return true;
        }
        bool GetCoordsFloat(string actionName, out Vec3F32 coords) {
            string[] stringCoords = args.SplitSpaces();
            coords = new Vec3F32();
            if (stringCoords.Length < 3) { Error(); p.Message("&cNot enough arguments for {0}", actionName); return false; }
            if (!CommandParser2.GetCoords(p, stringCoords, 0, ref coords)) { Error(true); return false; }
            return true;
        }
        void Look() {
            Vec3S32 coords;
            if (!GetCoords("Look", out coords)) { return; } 
            LookAtCoords(p, coords);
        }
        void Stare() {
            if (args == "") { scriptData.stareCoords = null; return; }
            
            Vec3S32 coords;
            if (!GetCoords("Stare", out coords)) { return; } 
            scriptData.stareCoords = coords;
        }
        void Env() {
            SetEnv(args);
        }
        void MOTD() {
            if (args.CaselessEq("ignore")) {
                scriptData.customMOTD = null;
                // do not use p.SendMapMotd() because it triggers the event which makes locked model update, which we don't want
                p.Send(Packet.Motd(p, p.GetMotd()));
                return;
            }
            
            scriptData.customMOTD = args;
            SendMOTD(scriptData.customMOTD);
        }
        void SendMOTD(string motd) {
            p.Send(Packet.Motd(p, motd));
            if (p.Supports(CpeExt.HackControl)) {
                p.Send(Hacks.MakeHackControl(p, motd));
            }
        }
        void SetSpawn() {
            Vec3F32 coords;
            if (!GetCoordsFloat("SetSpawn", out coords)) { return; } 
            
            Position pos = new Position();
            pos.X = (int)(coords.X * 32) + 16;
            pos.Y = (int)(coords.Y * 32) + Entities.CharacterHeight;
            pos.Z = (int)(coords.Z * 32) + 16;
            //p.Message("coords {0} {1} {2}", coords.X, coords.Y, coords.Z);
            //p.Message("pos.X {0} pos.Y {1} pos.Z {2}", pos.X, pos.Y, pos.Z);
            
            if (p.Supports(CpeExt.SetSpawnpoint)) {
                p.Send(Packet.SetSpawnpoint(pos, p.Rot, p.Supports(CpeExt.ExtEntityPositions)));
            } else {
                p.SendPos(Entities.SelfID, pos, p.Rot);
                Entities.Spawn(p, p);
            }
            p.Message("Your spawnpoint was updated.");
        }
        void Reply() {
            ReplyCore(true);
        }
        void ReplySilent() {
            ReplyCore(false);
        }
        void ReplyCore(bool notifyPlayer) {
            if (cmdName.CaselessEq("clear")) { scriptData.ResetReplies(); return; }
            //reply 1|You: Sure thing.|#replyYes
            //reply 2|You: No thanks.|#replyNo
            //reply 3|You: Can you elaborate?|#replyElaborate
            string[] replyBits = args.Split(pipeChar, 3);
            if (replyBits.Length < 3) { Error(); p.Message("&cNot enough arguments to setup a reply: \"" + args + "\"."); return; }
            int replyNum = -1;
            if (!CommandParser.GetInt(p, replyBits[0], "Script setup reply number", ref replyNum, 1, CmdReplyTwo.maxReplyCount)) { Error(true); return; }
            string replyMessage        = replyBits[1];
            string labelName           = replyBits[2];
            
            //replyNum is from 1-6 but replies is indexed from 0-5
            scriptData.replies[replyNum-1] = new ReplyData(scriptName, labelName, isOS, notifyPlayer);
            
            CpeMessageType type = CmdReplyTwo.GetReplyMessageType(replyNum);
            p.SendCpeMessage(type, "%f["+replyNum+"] "+replyMessage, CmdReplyTwo.replyPriority);
        }
        void TempBlock() { DoCmdNoPermChecks(Core.tempBlockCmd, args); }
        void TempChunk() { DoCmdNoPermChecks(Core.tempChunkCmd, args); }
        void Reach() {
            double dist = 0;
            if (!GetDoubleRawOrVal(cmdName, "Reach", out dist)) { return; }
            
            int packedDist = (int)(dist * 32);
            if (packedDist > short.MaxValue) { Error(); p.Message("&cReach of \"{0}\", is too long. Max is 1023 blocks.", dist); return; }
            
            p.Send(Packet.ClickDistance((short)packedDist));
        }
        void SetBlockID() {
            string[] bits = args.SplitSpaces(4);
            if (bits.Length < 4) {
                Error();
                p.Message("&cYou need to specify a package and x y z coordinates of the block to retrieve the ID of.");
                return;
            }
            string packageName = bits[0];
            string[] stringCoords = new string[] { bits[1], bits[2], bits[3] };
            
            Vec3S32 coords = new Vec3S32();
            if (!CommandParser.GetCoords(p, stringCoords, 0, ref coords)) { Error(true); }
            SetString(packageName, ClientBlockID(p.level.GetBlock((ushort)coords.X, (ushort)coords.Y, (ushort)coords.Z)).ToString());
        }
        static BlockID ClientBlockID(BlockID serverBlockID) {
            return Block.ToRaw(Block.Convert(serverBlockID));
        }
        void DefineHotkey() {
            // definehotkey [input args]|[key name]|<list of space separated modifiers>
            // definehotkey this is put into slash input!|equals|alt shift
            
            string[] bits = args.Split(pipeChar);
            if (bits.Length < 2) { Error(); p.Message("&cNot enough arguments to define a hotkey: \"" + args + "\"."); return; }
            
            string content = bits[0];
            int keyCode = scriptData.hotkeys.GetKeyCode(bits[1]);
            if (keyCode == 0) { Error(true); return; }
            string modifierArgs = bits.Length > 2 ? bits[2].ToLower() : "";
            byte modifiers = Hotkeys.GetModifiers(modifierArgs);
            bool repeatable = modifierArgs.Contains("async");
            
            
            string action = Hotkeys.FullAction(content, repeatable);
            if (action.Length > NetUtils.StringSize) {
                Error();
                p.Message("&cThe hotkey that script is trying to send (&7{0}&c) is &e{1}&c characters long, but can only be &a{2}&c at most.", action, action.Length, NetUtils.StringSize);
                p.Message("You can remove &a{0}&S or more characters to fix this error.", action.Length - NetUtils.StringSize);
                return;
            }
            scriptData.hotkeys.Define(action, keyCode, modifiers);
        }
        void UndefineHotkey() {
            string[] bits = args.Split(pipeChar);
            int keyCode = scriptData.hotkeys.GetKeyCode(bits[0]); if (keyCode == 0) { Error(true); return; }
            
            byte modifiers = 0;
            if (bits.Length > 1) {
                string modifierArgs = bits[1];
                modifiers = Hotkeys.GetModifiers(modifierArgs);
            }
            scriptData.hotkeys.Undefine(keyCode, modifiers);
        }
        void PlaceBlock() {
            string[] bits = args.SplitSpaces();
            Vec3S32 coords = new Vec3S32();
            if (bits.Length < 4) { Error(); p.Message("&cNot enough arguments for placeblock"); return; }
            if (!CommandParser.GetCoords(p, bits, 1, ref coords)) { Error(true); return; }
            
            BlockID block = 0;
            if (!CommandParser.GetBlock(p, bits[0], out block)) return;
            
            if (!MCGalaxy.Group.GuestRank.Blocks[block]) {
                string blockName = Block.GetName(p, block);
                Error(); p.Message("&cRank {0} &cis not allowed to use block \"{1}\". Therefore, script cannot place it.", MCGalaxy.Group.GuestRank.ColoredName, blockName);
                return;
            }
            
            coords = startingLevel.ClampPos(coords);
            
            startingLevel.SetBlock(       (ushort)coords.X, (ushort)coords.Y, (ushort)coords.Z, block);
            startingLevel.BroadcastChange((ushort)coords.X, (ushort)coords.Y, (ushort)coords.Z, block);
            
        }
        void ChangeModel() {
            if (cmdName == "") {
                if (scriptData.oldModel != null) {
                    p.UpdateModel(scriptData.oldModel);
                }
                return;
            }
            string[] models = GetLockedModels(p.GetMotd());
            if (models == null) {
                //p.Message("&cchangemodel Action is only allowed when you specify model= in map MOTD.");
                return;
            }
            //don't let them change twice to same model otherwise revert doesnt work
            if (p.Model.CaselessEq(cmdName)) { return; }
            
            scriptData.oldModel = p.Model;
            scriptData.newModel = cmdName;
            
            p.UpdateModel(cmdName);
        }
        void Boost() { DoCmdNoPermChecks(Core.boostCmd, args); }
        void Effect() { DoCmdNoPermChecks(Core.effectCmd, args); }
        
        const string doubleToFourDecimalPlaces = "0.#####";
        void SetDirVector() {
            string[] argBits = args.SplitSpaces();
            if (argBits.Length < 5) { Error(); p.Message("&cNot enough arguments for setdirvector (expected 5)"); return; }
            string xResult = argBits[0];
            string yResult = argBits[1];
            string zResult = argBits[2];
            double yaw;
            if (!GetDoubleRawOrVal(argBits[3], "setdirvector", out yaw)) { return; }
            double pitch;
            if (!GetDoubleRawOrVal(argBits[4], "setdirvector", out pitch)) { return; }
            double yawRad   =   yaw * (Math.PI / 180.0f);
            double pitchRad = pitch * (Math.PI / 180.0f);
            
            Vec3F32 dir = GetDirVector(yawRad, pitchRad);
            SetString(xResult, dir.X.ToString(doubleToFourDecimalPlaces));
            SetString(yResult, dir.Y.ToString(doubleToFourDecimalPlaces));
            SetString(zResult, dir.Z.ToString(doubleToFourDecimalPlaces));
        }
        //copy pasted from DirUtils.cs. why have you abandoned me Unk
        static Vec3F32 GetDirVector(double yaw, double pitch) {
            double x =  Math.Sin(yaw) * Math.Cos(pitch);
            double y = -Math.Sin(pitch);
            double z = -Math.Cos(yaw) * Math.Cos(pitch);
            return new Vec3F32((float)x, (float)y, (float)z);
        }
        // copy pasted from lockedmodel.cs plugin
        static char[] splitChars = new char[] { ',' };
        static string[] GetLockedModels(string motd) {
            // Does the motd have 'model=' in it?
            int index = motd.IndexOf("model=");
            if (index == -1) return null;
            motd = motd.Substring(index + "model=".Length);
            
            // Get the single word after 'model='
            if (motd.IndexOf(' ') >= 0)
                motd = motd.Substring(0, motd.IndexOf(' '));
            
            // Is there an actual word after 'model='?
            if (motd.Length == 0) return null;
            return motd.Split(splitChars);
        }
    }
    
    
    public class ScriptLine {
        public int lineNumber;
        public ScriptRunner.ActionType actionType;
        public enum ConditionLogic { None, If, IfNot }
        public enum ConditionType { Invalid, Item, Val }
        
        public string actionArgs = "";

        public ConditionLogic conditionLogic = ConditionLogic.None;
        public ConditionType conditionType = ConditionType.Invalid;

        public string conditionArgs = "";
    }

    
    public class ReplyData {
        //public string replyMessage;
        public string scriptName;
        public string labelName;
        public bool isOS;
        public bool notifyPlayer;
        public ReplyData(string scriptName, string labelName, bool isOS, bool notifyPlayer) {
            this.scriptName = scriptName; this.labelName = labelName; this.isOS = isOS; this.notifyPlayer = notifyPlayer;
        }
    }
    public class ScriptData {
        public const string prefixedMarker = "@";
        public const string savedMarker = ".";
        public const string extrasKey = "ccsPlugin_ScriptData";
        public const string savePath = "text/inventory/";
        public Player p;
        public bool frozen = false;
        public string customMOTD = null;
        public Vec3S32? stareCoords = null;
        public ReplyData[] replies = new ReplyData[CmdReplyTwo.maxReplyCount];
        
        public string oldModel = null;
        public string newModel = null;
        
        private Dictionary<string, string> strings = new Dictionary<string, string>();
        private Dictionary<string, string> savedStrings = new Dictionary<string, string>();
        private Dictionary<string, bool> osItems = new Dictionary<string, bool>();
        
        public bool debugging = false;
        public int debuggingDelay = 0;
        
        public void ShowAllStrings() {
            if (strings.Count == 0) { p.Message("There are no packages to show"); return; }
            foreach (var pair in strings) {
                p.Message("The value of &b{0} &Sis \"&o{1}&S\".", pair.Key, pair.Value);
            }
        }
        
        
        public Hotkeys hotkeys;
        
        private void SetRepliesNull() {
            for (int i = 0; i < replies.Length; i++) {
                replies[i] = null;
            }
        }
        public void ResetReplies() {
            for (int i = 0; i < CmdReplyTwo.maxReplyCount; i++) {
                if (replies[i] == null) { continue; } //dont erase CPE lines that don't have a reply to clear
                p.SendCpeMessage(CmdReplyTwo.GetReplyMessageType(i+1), "", CmdReplyTwo.replyPriority); //the message type is 1-6 and we iterate 0-5, hence +1
            }
            SetRepliesNull();
        }
        //prevent empty constructor from being used
        private ScriptData() { }
        public ScriptData(Player p) {
            this.p = p;
            SetRepliesNull();
            hotkeys = new Hotkeys(p);
            
            
            string filePath, fileName;
            if (!DoesDirectoryExist(out filePath, out fileName) || !File.Exists(fileName)) { return; } //no data to load
            string value = "";
            using (StreamReader sr = new StreamReader(fileName)) {
                while ((value = sr.ReadLine()) != null) {
                    if (!value.StartsWith(prefixedMarker)) { continue; } //something went terribly wrong there should not be data that doesn't begin with a fully formed saved string name
                    string[] bits = value.SplitSpaces(2);
                    if (bits.Length < 2) { continue; }
                    savedStrings[bits[0]] = bits[1];
                }
            }
        }
        public void UpdatePlayerReference(Player p) {
            this.p = p;
            hotkeys.UpdatePlayerReference(p);
        }
        public void OnJoinedLevel() {
            
            if (debugging) p.Message("Script debugging mode is now &cfalse&S.");
            debugging = false;
            debuggingDelay = 0;
            
            
            ResetReplies();
            frozen = false;
            stareCoords = null;
            Reset(true, true, "");
            oldModel = null;
            newModel = null;
            hotkeys.UndefineAll();
            customMOTD = null;
        }
        public void Reset(bool packages, bool items, string matcher) {
            if (packages) { ResetDict(strings, matcher); }
            if (items)  { ResetDict(osItems, matcher); }
        }
        public void ResetDict<T>(Dictionary<string, T> dict, string matcher) {
            if (matcher.Length == 0) { dict.Clear(); return; }
            List<string> keysToRemove = MatchingKeys(dict, matcher);
            foreach (string key in keysToRemove) {
                //p.Message("removing key {0}", key);
                dict.Remove(key);
            }
        }
        public void ResetSavedStrings(string scriptName, string matcher) {
            string prefix = (prefixedMarker+scriptName+"_").ToUpper();
            string pattern = prefix+"*"+matcher;
            if (!(matcher.Contains("*") || matcher.Contains("?"))) {
                //if there are no special characters, it's a generic "contains" search, so add asterisk at the end
                pattern = pattern+"*";
            }
            ResetDict(savedStrings, pattern);
        }
        static List<string> MatchingKeys<T>(Dictionary<string, T> dict, string keyword) {
            var keys = dict.Keys.ToList();
            return Wildcard.Filter(keys, keyword, key => key);
        }
        
        public void WriteSavedStringsToDisk() {
            //if (savedStrings.Count == 0) { return; } //if all saved data is erased the file still needs to be written to to reset it so this should stay commented out
            
            string filePath, fileName;
            if (!DoesDirectoryExist(out filePath, out fileName)) { Directory.CreateDirectory(filePath); }
            
            using (StreamWriter file = new StreamWriter(fileName, false)) {
                foreach (KeyValuePair<string, string> entry in savedStrings) {
                    if (entry.Value.Length == 0 || entry.Value == "0" || entry.Value.CaselessEq("false")) { continue; }
                    file.WriteLine(entry.Key+" "+entry.Value);
                    //p.Message("&ewriting &0{0}&e to {1}", entry.Key+" "+entry.Value, fileName);
                }
            }
        }
        bool DoesDirectoryExist(out string filePath, out string fileName) {
            //delete wrong location of data.txt
            if (File.Exists(savePath+p.name+"/data.txt")) {
                File.Delete(savePath+p.name+"/data.txt");
                //p.Message("Script debug text: deleted wrong data.txt &[This message means it is working as intended and you will only see it once.");
            }
            
            filePath = savePath+p.name+"/data/";
            fileName = filePath+"data.txt";
            return Directory.Exists(filePath);
        }
        
        ///in OS, strings are never saved.
        ///in mod, strings are saved if they end with a period. Saved strings are always prefixed with the @scriptname_
        ///if they aren't prefixed with the script name, it is automatically added
        ///e.g. @egg2021_eggCount.
        public string ValidateStringName(string stringName, bool isOS, string scriptName, out bool saved) {
            saved = false;
            if (isOS) { return stringName; }
            if (!stringName.EndsWith(savedMarker)) { return stringName; }
            saved = true;
            if (stringName.StartsWith(prefixedMarker)) { return stringName; }
            return (prefixedMarker+scriptName+"_"+stringName);
        }
        
        public string GetString(string stringName, bool isOS, string scriptName) {
            bool saved;
            stringName = ValidateStringName(stringName, isOS, scriptName, out saved).ToUpper();
            
            var dict = saved ? savedStrings : strings;
            string value = "";
            if (dict.ContainsKey(stringName) ) { value = dict[stringName]; }
            //p.Message("getstring: saved is {0} and string {1} is being set to {2}", saved, stringName, value);
            return value;
        }
        public void SetString(string stringName, string value, bool isOS, string scriptName) {
            bool saved;
            stringName = ValidateStringName(stringName, isOS, scriptName, out saved).ToUpper();
            
            var dict = saved ? savedStrings : strings;
            dict[stringName] = value;
            //p.Message("setstring: saved is {0} and string {1} is being set to {2}", saved, stringName, value);
        }
        
        
        public bool HasItem (string itemName, bool isOS) { return isOS ? OsHasItem(itemName) : ModHasItem(itemName); }
        public void GiveItem(string itemName, bool isOS) { if (isOS) { OsGiveItem(itemName); } else { ModGiveItem(itemName); } }
        public void TakeItem(string itemName, bool isOS) { if (isOS) { OsTakeItem(itemName); } else { ModTakeItem(itemName); } }
        
        private bool ModHasItem(string itemName)  {
            try {
                return new Item(itemName).OwnedBy(p.name);
            } catch (System.ArgumentException) {
                return false;
            }
        }
        private void UseAsMB(Command cmd, string args) {
            CommandData data = default(CommandData); data.Context = CommandContext.MessageBlock;
            cmd.Use(p, args, data);
        }
        private void ModGiveItem(string itemName) {
            UseAsMB(Core.stuffCmd, "obsoleteArg" +" get "+itemName.ToUpper());
        }
        private void ModTakeItem(string itemName) {
            UseAsMB(Core.stuffCmd, "obsoleteArg" +" take "+itemName.ToUpper());
        }
        
        //#region OS
        public void DisplayItems() {
            if (osItems.Count == 0) { p.Message("&cYou have no items! &SDid you mean to use &b/stuff&S?"); return; }
            
            string[] allItems = new string[osItems.Count];
            int i = 0;
            foreach (KeyValuePair<string, bool> entry in osItems) {
                allItems[i] = "&a"+entry.Key.Replace('_', ' ');
                i++;
            }
            p.Message("&eYour items:");
            p.Message("&f> "+String.Join(" &8• ", allItems));
            p.Message("Notably, items are different from &T/stuff&S because they will disappear if you leave this map.");
        }
        
        public bool OsHasItem(string itemName) { return osItems.ContainsKey( itemName.ToUpper() ); }
        public void OsGiveItem(string itemName) {
            itemName = itemName.ToUpper();
            if (osItems.ContainsKey(itemName)) { return; } //they already have this item
            osItems[itemName] = true;
            p.Message("You found an item: &a{0}&S!", itemName.Replace('_', ' ') );
            p.Message("Check what items you have with &a/Items&S.");
        }
        public void OsTakeItem(string itemName) {
            itemName = itemName.ToUpper();
            if (osItems.Remove(itemName)) {
                p.Message("&a{0}&S was removed from your items.", itemName.Replace('_', ' '));
            }
        }
        //#endregion
        
    }
    
    public class Hotkeys {
        private static readonly Dictionary<string, int> keyCodes
            = new Dictionary<string, int>
        {
            { "NONE",         0   },
            { "ESCAPE",       1   },
            { "1",            2   },
            { "2",            3   },
            { "3",            4   },
            { "4",            5   },
            { "5",            6   },
            { "6",            7   },
            { "7",            8   },
            { "8",            9   },
            { "9",            10  },
            { "0",            11  },
            { "MINUS",        12  },
            { "EQUALS",       13  },
            { "BACK",         14  },
            { "TAB",          15  },
            { "Q",            16  },
            { "W",            17  },
            { "E",            18  },
            { "R",            19  },
            { "T",            20  },
            { "Y",            21  },
            { "U",            22  },
            { "I",            23  },
            { "O",            24  },
            { "P",            25  },
            { "LBRACKET",     26  },
            { "RBRACKET",     27  },
            { "RETURN",       28  },
            { "LCONTROL",     29  },
            { "A",            30  },
            { "S",            31  },
            { "D",            32  },
            { "F",            33  },
            { "G",            34  },
            { "H",            35  },
            { "J",            36  },
            { "K",            37  },
            { "L",            38  },
            { "SEMICOLON",    39  },
            { "APOSTROPHE",   40  },
            { "GRAVE",        41  },
            { "LSHIFT",       42  },
            { "BACKSLASH",    43  },
            { "Z",            44  },
            { "X",            45  },
            { "C",            46  },
            { "V",            47  },
            { "B",            48  },
            { "N",            49  },
            { "M",            50  },
            { "COMMA",        51  },
            { "PERIOD",       52  },
            { "SLASH",        53  },
            { "RSHIFT",       54  },
            { "MULTIPLY",     55  },
            { "LMENU",        56  },
            { "SPACE",        57  },
            { "CAPITAL",      58  },
            { "F1",           59  },
            { "F2",           60  },
            { "F3",           61  },
            { "F4",           62  },
            { "F5",           63  },
            { "F6",           64  },
            { "F7",           65  },
            { "F8",           66  },
            { "F9",           67  },
            { "F10",          68  },
            { "NUMLOCK",      69  },
            { "SCROLL",       70  },
            { "NUMPAD7",      71  },
            { "NUMPAD8",      72  },
            { "NUMPAD9",      73  },
            { "SUBTRACT",     74  },
            { "NUMPAD4",      75  },
            { "NUMPAD5",      76  },
            { "NUMPAD6",      77  },
            { "ADD",          78  },
            { "NUMPAD1",      79  },
            { "NUMPAD2",      80  },
            { "NUMPAD3",      81  },
            { "NUMPAD0",      82  },
            { "DECIMAL",      83  },
            { "F11",          87  },
            { "F12",          88  },
            { "F13",          100 },
            { "F14",          101 },
            { "F15",          102 },
            { "F16",          103 },
            { "F17",          104 },
            { "F18",          105 },
            { "KANA",         112 },
            { "F19",          113 },
            { "CONVERT",      121 },
            { "NOCONVERT",    123 },
            { "YEN",          125 },
            { "NUMPADEQUALS", 141 },
            { "CIRCUMFLEX",   144 },
            { "AT",           145 },
            { "COLON",        146 },
            { "UNDERLINE",    147 },
            { "KANJI",        148 },
            { "STOP",         149 },
            { "AX",           150 },
            { "UNLABELED",    151 },
            { "NUMPADENTER",  156 },
            { "RCONTROL",     157 },
            { "SECTION",      167 },
            { "NUMPADCOMMA",  179 },
            { "DIVIDE",       181 },
            { "SYSRQ",        183 },
            { "RMENU",        184 },
            { "FUNCTION",     196 },
            { "PAUSE",        197 },
            { "HOME",         199 },
            { "UP",           200 },
            { "PRIOR",        201 },
            { "LEFT",         203 },
            { "RIGHT",        205 },
            { "END",          207 },
            { "DOWN",         208 },
            { "NEXT",         209 },
            { "INSERT",       210 },
            { "DELETE",       211 },
            { "CLEAR",        218 },
            { "LMETA",        219 },
            { "RMETA",        220 },
            { "APPS",         221 },
            { "POWER",        222 },
            { "SLEEP",        223 }
        };
        
        public static string FullAction(string content, bool repeatable) {
            string cmd = repeatable ? "inputAsync" : "input";
            return "/"+cmd+" "+content+"◙";
        }
        public static byte GetModifiers(string modifierArgs) {
            byte ctrlFlag  = (byte)(modifierArgs.Contains("ctrl")  ? 1 : 0);
            byte shiftFlag = (byte)(modifierArgs.Contains("shift") ? 2 : 0);
            byte altFlag   = (byte)(modifierArgs.Contains("alt")   ? 4 : 0);
            return (byte)(ctrlFlag | shiftFlag | altFlag);
        }
        
        private Player p;
        private List<Hotkey> hotkeys = new List<Hotkey>();
        public Hotkeys(Player p) {
            this.p = p;
        }
        public void UpdatePlayerReference(Player p) { this.p = p; }
        
        public int GetKeyCode(string keyName) {
            int code = 0;
            if (!keyCodes.TryGetValue(keyName.ToUpper(), out code)) {
                p.Message("&cUnrecognized key name \"{0}\". Please see https://minecraft.fandom.com/el/wiki/Key_codes#Full_table for valid key names.", keyName);
                p.Message("&bRemember to use the NAME of the key and not the numerical code/value!");
            }
            return code;
        }
        
        public void Define(string action, int keyCode, byte modifiers) {
            p.Send(Packet.TextHotKey("na2 script hotkey", action, keyCode, modifiers, true));
            hotkeys.Add(new Hotkey(keyCode, modifiers));
        }
        public void Undefine(int keyCode, byte modifiers) {
            p.Send(Packet.TextHotKey("", "", keyCode, modifiers, true));
        }
        public void UndefineAll() {
            foreach (Hotkey hotkey in hotkeys) {
                Undefine(hotkey.keyCode, hotkey.modifiers);
            }
        }
    }
    public class Hotkey {
        //public string action
        public int keyCode;
        public byte modifiers;
        public Hotkey(int keyCode, byte modifiers) {
            this.keyCode = keyCode; this.modifiers = modifiers;
        }
    }
    
    public static class CommandParser2 {
        /// <summary> Attempts to parse the 3 given arguments as coordinates. </summary>
        public static bool GetCoords(Player p, string[] args, int argsOffset, ref Vec3F32 P) {
            return
                GetCoord(p, args[argsOffset + 0], "X coordinate", ref P.X) &&
                GetCoord(p, args[argsOffset + 1], "Y coordinate", ref P.Y) &&
                GetCoord(p, args[argsOffset + 2], "Z coordinate", ref P.Z);
        }
        
        static bool GetCoord(Player p, string arg, string axis, ref float value) {
            bool relative = arg.Length > 0 && arg[0] == '~';
            if (relative) arg = arg.Substring(1);
            // ~ should work as ~0
            if (relative && arg.Length == 0) return true;
            
            float cur = value;
            value   = 0;
            
            if (!CommandParser.GetReal(p, arg, axis, ref value)) return false;
            if (relative) value += cur;
            return true;
        }
    }
    
    
}