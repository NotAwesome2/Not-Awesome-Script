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
using MCGalaxy.Modules.Awards;
using BlockID = System.UInt16;
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

            p.SendBlockchange((ushort)x, (ushort)y, (ushort)z, block);
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
            Level startingLevel = p.level;

            if (message == "") { Help(p); return; }
            string[] words = message.Split(' ');
            if (words.Length < 9) {
                p.Message("%cYou need to provide all 3 sets of coordinates, which means 9 numbers total.");
                return;
            }

            int x1 = 0, y1 = 0, z1 = 0, x2 = 0, y2 = 0, z2 = 0, x3 = 0, y3 = 0, z3 = 0;

            bool mistake = false;
            if (!CommandParser.GetInt(p, words[0], "x1", ref x1, 0, p.Level.Width - 1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[1], "y1", ref y1, 0, p.Level.Height - 1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[2], "z1", ref z1, 0, p.Level.Length - 1)) { mistake = true; }

            if (!CommandParser.GetInt(p, words[3], "x2", ref x2, 0, p.Level.Width - 1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[4], "y2", ref y2, 0, p.Level.Height - 1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[5], "z2", ref z2, 0, p.Level.Length - 1)) { mistake = true; }

            if (!CommandParser.GetInt(p, words[6], "x3", ref x3, 0, p.Level.Width - 1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[7], "y3", ref y3, 0, p.Level.Height - 1)) { mistake = true; }
            if (!CommandParser.GetInt(p, words[8], "z3", ref z3, 0, p.Level.Length - 1)) { mistake = true; }
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
            bool pasteAir = true;
            if (words.Length > 10) {
                if (!CommandParser.GetBool(p, words[10], ref pasteAir)) { return; }
            }

            //    95, 33, 73, 99, 36, 75, 97, 37, 79

            BlockID[] blocks = GetBlocks(p, x1, y1, z1, x2, y2, z2);


            PlaceBlocks(p, blocks, x1, y1, z1, x2, y2, z2, x3, y3, z3, allPlayers, pasteAir, startingLevel);

        }

        public BlockID[] GetBlocks(Player p, int x1, int y1, int z1, int x2, int y2, int z2) {

            int xLen = (x2 - x1) + 1;
            int yLen = (y2 - y1) + 1;
            int zLen = (z2 - z1) + 1;

            BlockID[] blocks = new BlockID[xLen * yLen * zLen];
            int index = 0;

            for (int xi = x1; xi < x1 + xLen; ++xi) {
                for (int yi = y1; yi < y1 + yLen; ++yi) {
                    for (int zi = z1; zi < z1 + zLen; ++zi) {
                        blocks[index] = p.level.GetBlock((ushort)xi, (ushort)yi, (ushort)zi);
                        index++;
                    }
                }
            }
            return blocks;
        }

        public void PlaceBlocks(Player p, BlockID[] blocks, int x1, int y1, int z1, int x2, int y2, int z2, int x3, int y3, int z3, bool allPlayers, bool pasteAir, Level startingLevel) {

            int xLen = (x2 - x1) + 1;
            int yLen = (y2 - y1) + 1;
            int zLen = (z2 - z1) + 1;

            Player[] players = allPlayers ? PlayerInfo.Online.Items : new[] { p };

            foreach (Player pl in players) {
                if (pl.level != p.level) { continue; }

                BufferedBlockSender buffer = new BufferedBlockSender(pl);
                int index = 0;
                for (int xi = x3; xi < x3 + xLen; ++xi) {
                    for (int yi = y3; yi < y3 + yLen; ++yi) {
                        for (int zi = z3; zi < z3 + zLen; ++zi) {
                            if (p.level != startingLevel) { return; }
                            if (!pasteAir && blocks[index] == Block.Air) { index++; continue; }
                            int pos = pl.level.PosToInt((ushort)xi, (ushort)yi, (ushort)zi);
                            if (pos >= 0) { buffer.Add(pos, blocks[index]); }
                            index++;
                        }
                    }
                }
                // last few blocks 
                buffer.Flush();
            }

        }

        public override void Help(Player p) {
            p.Message("%T/TempChunk %f[x1 y1 z1] %7[x2 y2 z2] %r[x3 y3 z3] <allPlayers?true/false> <pasteAir?true/false>");
            p.Message("%HCopies a chunk of the world defined by %ffirst %Hand %7second%H coords then pastes it into the spot defined by the %rthird %Hset of coords.");
            p.Message("%H<allPlayers?true/false> is optional, and defaults to false. If true, the tempchunk changes are sent to all players in the map.");
            p.Message("%H<pasteAir?true/false> is optional, and defaults to true. If false, the tempchunk will not paste air blocks. You need to specify the first optional arg to specify this one.");
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
                if (!(data.Context == CommandContext.MessageBlock || HasExtraPerm(p, p.Rank, 1))) {
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
            if (desc.Length == 0) {
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

        public override void Use(Player p, string message, CommandData data) {
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

        public override void Use(Player p, string message, CommandData data) {
            if (message == "") { Help(p); return; }
            int replyNum = -1;
            if (!CommandParser.GetInt(p, message, "Reply number", ref replyNum, 1, maxReplyCount)) { return; }
            ScriptData scriptData = Core.GetScriptData(p);
            ReplyData replyData = scriptData.replies[replyNum - 1]; //reply number is from 1-6 but the array is indexed 0-5, hence -1
            if (replyData == null) { p.Message("There's no reply option &f[{0}] &Sat the moment.", replyNum); return; }

            //reset the replies once you choose one
            scriptData.ResetReplies();

            //the script has to be ran as if from a message block
            CommandData cmdData = default(CommandData); cmdData.Context = CommandContext.MessageBlock;

            if (replyData.isOS) {
                Core.osRunscriptCmd.Use(p, replyData.labelName, cmdData);
            } else {
                Core.runscriptCmd.Use(p, replyData.scriptName + " " + replyData.labelName, cmdData);
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
        public static bool IsNA2 {
            get {
                return Server.Config.Name.StartsWith("Not Awesome 2");
            }
        }
        public static string NA2WebFileDirectory return "/home/na2/Website-Files/";
        public static string NA2WebURL "https://notawesome.cc/";

        public override string creator { get { return "Goodly"; } }
        public override string name { get { return "ccs"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.9"; } }

        static readonly object scriptDataLocker = new object();

        private static Dictionary<string, ScriptData> scriptDataAtPlayer = new Dictionary<string, ScriptData>();
        public static ScriptData GetScriptData(Player p) {
            lock (scriptDataLocker) { //only one thread should access this at a time
                ScriptData sd;
                if (scriptDataAtPlayer.TryGetValue(p.name, out sd)) { return sd; } else {
                    //throw new System.Exception("Tried to get script data for "+p.name+" but there was NOTHING there");
                    Logger.Log(LogType.SystemActivity, "ccs: This shouldn't happen, but; FAILED to GetScriptData for " + p.name + ", returning a new one.");
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
            ScriptActions.PluginLoad();
            Docs.PluginLoad();

            tempBlockCmd = new CmdTempBlock();
            tempChunkCmd = new CmdTempChunk();
            stuffCmd = new CmdStuff();
            dropCmd = new CmdDrop();
            runscriptCmd = new CmdScript();
            osRunscriptCmd = new CmdOsScript();
            debugScriptCmd = new CmdDebugScript();
            replyCmd = new CmdReplyTwo();
            itemsCmd = new CmdItems();
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
            OnPlayerSpawningEvent.Register(OnPlayerSpawning, Priority.High);
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
            OnPlayerSpawningEvent.Unregister(OnPlayerSpawning);
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
        static void OnPlayerSpawning(Player p, ref Position pos, ref byte yaw, ref byte pitch, bool respawning) {
            if (respawning) { return; } //Only call when spawning in a new level

            //clear all persistent chat lines at the priority used by cpemessage in script
            for (int i = 1; i < CmdReplyTwo.maxReplyCount + 1; i++) {
                CpeMessageType type = CmdReplyTwo.GetReplyMessageType(i);
                if (type != CpeMessageType.Normal) { p.SendCpeMessage(type, "", ScriptRunner.CpeMessagePriority); }
            }
            ScriptData data;
            if (scriptDataAtPlayer.TryGetValue(p.name, out data)) { data.OnPlayerSpawning(); }
        }
        static void OnInfoSwap(string source, string dest) {
            string sourcePath = ScriptData.savePath + source;
            string destPath = ScriptData.savePath + dest;
            string backupPath = ScriptData.savePath + "temporary-info-swap";
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
            if (!(data.Context == CommandContext.MessageBlock || CheckExtraPerm(p, data, 1))) { return; }

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

            string startLabel = args[1 + argsOffset];
            bool repeatable = (args.Length > 2 + argsOffset && args[2 + argsOffset] == "repeatable");
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
            if (!(data.Context == CommandContext.MessageBlock || LevelInfo.IsRealmOwner(p.name, p.level.name) || p.group.Permission >= LevelPermission.Operator)) {
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
            if (!(LevelInfo.IsRealmOwner(p.name, p.level.name) || p.group.Permission >= LevelPermission.Operator)) {
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
            p.Message("Script debugging mode is now {0}&S{1}", debug ? "&atrue" : "&cfalse", debug ? " with &b" + delay + "&S added delay." : "");
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
            if (!Core.IsNA2) {
                p.Message("This command can't be used on &b{0}&S because it uses unique web services that are only available on Not Awesome 2. Sorry!", Server.Config.Name);
                return;
            }
            if (!(data.Context == CommandContext.MessageBlock || LevelInfo.IsRealmOwner(p.name, p.level.name) || p.group.Permission >= LevelPermission.Operator)) {
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
        static string directory { get { return Core.NA2WebFileDirectory + folder; } }
        static string folder { get { return "osscripts/"; } }
        static string fileName(Level level) { return level.name.ToLower() + "-" + GetMD5(level) + Script.EXT; }
        static string url(Level level) { return Core.NA2WebURL + folder + fileName(level); }

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


                if (line.Length == 0 || line.StartsWith("//")) { continue; }
                if (script.uses.ReadLine(line)) { continue; }

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
                    scriptLine.actionType = ScriptActions.GetAction(actionType);
                    if (scriptLine.actionType == null) {
                        p.Message("&cError when compiling script on line " + lineNumber + ": unknown Action \"" + actionType + "\" detected.");
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
            if (cmd.CaselessEq("input")) {
                calledLabel = "#input"; async = false;
            } else if (cmd.CaselessEq("inputAsync")) {
                calledLabel = "#inputAsync"; async = true;
            } else { return; }

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

        private Script() {
            compileDate = DateTime.UtcNow;
        }
        private DateTime compileDate;
        private Dictionary<string, int> labels = new Dictionary<string, int>();
        public bool HasLabel(string label) { return labels.ContainsKey(label); }
        public bool GetLabel(string label, out int actionIndex) { return labels.TryGetValue(label, out actionIndex); }

        public List<ScriptLine> actions = new List<ScriptLine>();


        public Uses uses = new Uses();

        public class Uses {
            const string PREFIX = "using ";
            public bool cef;
            public bool quitResetsRunArgs;

            public bool ReadLine(string line) {
                if (!line.StartsWith(PREFIX)) { return false; }
                line = line.Substring(PREFIX.Length);

                if (line == "cef") { cef = true; } else if (line == "quit_resets_runargs") { quitResetsRunArgs = true; }

                return true;
            }
        }

    }

    //Constructor
    public partial class ScriptRunner {

        private ScriptRunner() { }

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

        public static char[] pipeChar = new char[] { '|' };

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

        public static PersistentMessagePriority CpeMessagePriority = PersistentMessagePriority.High;

        public static CpeMessageType GetCpeMessageType(string type) {
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

        public Script script;

        public Player p;
        public ScriptData scriptData;
        public Level startingLevel;
        public string startingLevelName;
        public string scriptName;
        public string startLabel;
        public bool isOS = false;
        public bool repeatable = false;
        public string thisBool;
        public CommandData data;
        public int actionIndex;
        public List<int> comebackToIndex = new List<int>();
        public List<Thread> newthreads = new List<Thread>();
        int actionCounter;
        int newThreadNestLevel;
        int lineNumber = -1;

        public static bool isCefMessage(string message) { return message.StartsWith("cef "); }
        public bool shouldSendCef { get { return hasCef && script.uses.cef; } }
        bool hasCef = false;

        public int amountOfCharsInLastMessage = 0;
        public string[] runArgs;

        bool _cancelled;
        public bool cancelled {
            get { return _cancelled || startingLevelName != p.level.name.ToLower() || p.Socket.Disconnected; }
            set {
                if (value != true) { throw new System.ArgumentException("cancelled can only be set to true"); }
                _cancelled = value;
                //p.Message("%eScript note: Script {0} {1} cancelled due to switching maps.", scriptName, startLabel);
            }
        }


    }
    //Misc functions
    public partial class ScriptRunner {

        static Random rnd = new Random();
        static readonly object rndLocker = new object();
        public static int RandomRange(int inclusiveMin, int inclusiveMax) {
            lock (rndLocker) { return rnd.Next(inclusiveMin, inclusiveMax + 1); }
        }
        public static double RandomRangeDouble(double min, double max) {
            lock (rndLocker) { return (rnd.NextDouble() * (max - min) + min); }
        }
        public static string RandomEntry(string[] entries) {
            lock (rndLocker) { return entries[rnd.Next(entries.Length)]; }
        }

        static byte? GetEnvColorType(string type) {
            if (type.CaselessEq("sky")) { return 0; }
            if (type.CaselessEq("cloud")) { return 1; }
            if (type.CaselessEq("clouds")) { return 1; }
            if (type.CaselessEq("fog")) { return 2; }
            if (type.CaselessEq("shadow")) { return 3; }
            if (type.CaselessEq("sun")) { return 4; }
            if (type.CaselessEq("skybox")) { return 5; }
            return null;
        }
        static byte? GetEnvWeatherType(string type) {
            if (type.CaselessEq("sun")) { return 0; }
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
        public void SetEnv(string message) {
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
                        p.Send(Packet.EnvMapProperty((EnvProp)envPropType, yesno ? 1 : 0));
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


        public bool ValidateCommand(out Command cmd) {
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
        public void DoCmd(Command cmd, string args) {
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
        public void DoCmdNoPermChecks(Command cmd, string args) {
            try {
                cmd.Use(p, args, GetCommandData());
            } catch (Exception ex) {
                Logger.LogError(ex);
                Error(); p.Message("&cAn error occured and command {0} could not be executed.", cmd.name);
            }
        }
        public void DoNewThreadRunScript(ScriptRunner scriptRunner, string startLabel) {
            Thread thread = new Thread(
                    () => {
                        try {
                            scriptRunner.Run(startLabel, newThreadNestLevel + 1);
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
            if (condition == "=") { return doubleValue == doubleValueCompared; }
            if (condition == "!=") { return doubleValue != doubleValueCompared; }
            if (condition == "<") { return doubleValue < doubleValueCompared; }
            if (condition == "<=") { return doubleValue <= doubleValueCompared; }
            if (condition == ">") { return doubleValue > doubleValueCompared; }
            if (condition == ">=") { return doubleValue >= doubleValueCompared; }
            scriptError = true;
            return false;
        }
        // Used in OnPlayerMove when player is forced to stare
        public static void LookAtCoords(Player p, Vec3S32 coords) {
            //we want to calculate difference between player's (eye position)
            //and block's position to use in GetYawPitch

            //convert block coords to player units
            coords *= 32;
            //center of the block
            coords += new Vec3S32(16, 16, 16);

            int dx = coords.X - p.Pos.X;
            int dy = coords.Y - (p.Pos.Y - Entities.CharacterHeight + ModelInfo.CalcEyeHeight(p));
            int dz = coords.Z - p.Pos.Z;
            Vec3F32 dir = new Vec3F32(dx, dy, dz);
            dir = Vec3F32.Normalise(dir);

            byte yaw, pitch;
            DirUtils.GetYawPitch(dir, out yaw, out pitch);
            byte[] packet = new byte[4];
            packet[0] = Opcode.OrientationUpdate; packet[1] = Entities.SelfID; packet[2] = yaw; packet[3] = pitch;
            p.Send(packet);
        }

        public void ResetRunArgs() {
            runArgs = new string[] { runArgs[0] };
        }
        public void TryReplaceRunArgs(string[] newRunArgs) {
            if (newRunArgs.Length < 2) { return; }

            ReplaceUnderScoreWithSpaceInRunArgs(ref newRunArgs);

            string originalStartLabel = runArgs[0];
            runArgs = (string[])newRunArgs.Clone();
            runArgs[0] = originalStartLabel;
        }
        static string runArgSpaceSubstitute = "_";
        public static void ReplaceUnderScoreWithSpaceInRunArgs(ref string[] runArgs) {
            for (int i = 0; i < runArgs.Length; i++) {
                if (i == 0) { continue; } //do not replace underscores with spaces in startLabel which is always runArgs[0]
                runArgs[i] = runArgs[i].Replace(runArgSpaceSubstitute, " ");
            }
        }

        public void DoReply(bool notifyPlayer) {
            if (cmdName.CaselessEq("clear")) { scriptData.ResetReplies(); return; }
            //reply 1|You: Sure thing.|#replyYes
            //reply 2|You: No thanks.|#replyNo
            //reply 3|You: Can you elaborate?|#replyElaborate
            string[] replyBits = args.Split(ScriptRunner.pipeChar, 3);
            if (replyBits.Length < 3) { Error(); p.Message("&cNot enough arguments to setup a reply: \"" + args + "\"."); return; }
            int replyNum = -1;
            if (!CommandParser.GetInt(p, replyBits[0], "Script setup reply number", ref replyNum, 1, CmdReplyTwo.maxReplyCount)) { Error(true); return; }
            string replyMessage = replyBits[1];
            string labelName = replyBits[2];

            //replyNum is from 1-6 but replies is indexed from 0-5
            scriptData.replies[replyNum - 1] = new ReplyData(scriptName, labelName, isOS, notifyPlayer);

            CpeMessageType type = CmdReplyTwo.GetReplyMessageType(replyNum);
            p.SendCpeMessage(type, "%f[" + replyNum + "] " + replyMessage, CmdReplyTwo.replyPriority);
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
            this.startLabel = startLabel;

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

            scriptData.AddActiveScript(this);

            //if player has cef do:
            //bool cef true
            if (p.Session.appName != null && p.Session.appName.Contains(" cef")) {
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

            ScriptLine lastLine = new ScriptLine();

            while (actionIndex < script.actions.Count) {

                if (cancelled) {
                    scriptData.RemoveActiveScript(this);
                    return;
                }

                lastLine = script.actions[actionIndex];

                //RunScriptLines increments actionIndex, but it may also modify it (goto, call, quit)
                RunScriptLine(script.actions[actionIndex]);


                //we need to make sure scriptActions is still in range, because quit from RunScriptLine sets the index to scriptActions.Count
                if (actionIndex < script.actions.Count) {
                    //if the last action was Reply and this action isn't, that means we're done setting up replies for now and should show the text of how to reply.
                    if (
                        lastLine.actionType != null &&
                        lastLine.GetType() == typeof(ScriptActions.Reply) &&
                        script.actions[actionIndex].actionType.GetType() != typeof(ScriptActions.Reply)

                        ) {
                        //only tell if it's not being cleared
                        if (!lastLine.actionArgs.CaselessEq("clear")) { CmdReplyTwo.SetUpDone(p); }
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

            scriptData.RemoveActiveScript(this);
            //p.Message("Script finished after performing {0} actions.", actionCounter);

        }

        bool ShouldDoLine(ScriptLine line) {
            if (line.conditionLogic == ScriptLine.ConditionLogic.None) { return true; }

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

        public string args;
        public string cmdName;
        public string cmdArgs;
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
            line.actionType.Behavior(this);

            if (scriptData.debugging && scriptData.debuggingDelay > 0) { Thread.Sleep(scriptData.debuggingDelay); }
        }

        public bool GetIntRawOrVal(string arg, string actionName, out int value) {
            if (!Int32.TryParse(arg, out value)) {
                string stringValue = GetString(arg);
                if (stringValue == "") { stringValue = "0"; }
                if (!Int32.TryParse(stringValue, out value)) { Error(); p.Message("&cAction {0} only takes integer values and \"{1}={2}\" is not an integer.", actionName, arg, stringValue); return false; }
            }
            return true;
        }

        public bool GetDoubleRawOrVal(string arg, string actionName, out double value, bool throwError = true) {
            if (!double.TryParse(arg, out value)) {
                string stringValue = GetString(arg);
                if (stringValue == "") { stringValue = "0"; }
                if (!double.TryParse(stringValue, out value)) {
                    if (throwError) { Error(); p.Message("&cAction {0} only takes number values and \"{1}={2}\" is not a number.", actionName, arg, stringValue); }
                    return false;
                }
            }
            return true;
        }

        public bool GetDouble(string valName, out double doubleValue, bool throwError = true) {
            doubleValue = 0;
            string value = GetString(valName);
            if (value.Length == 0) { return true; } //treat an empty or undefined value as 0
            if (!double.TryParse(value, out doubleValue)) {
                if (throwError) { Error(); p.Message("&c{0} = \"{1}\", therefore it cannot be used as a number.", valName, value); }
                return false;
            }
            return true;
        }

        public void SetDouble(string valName, double value) {
            SetString(valName, value.ToString());
        }

        public string GetString(string stringName) {
            if (p == null) { throw new System.Exception("Player null in GetString new pdb pls"); }

            if (stringName.Length > 6 && stringName.CaselessStarts("runArg")) {
                int runArgIndex;
                if (!Int32.TryParse(stringName.Substring(6), out runArgIndex)) { goto fuckyou; }

                if (runArgIndex < runArgs.Length) { return runArgs[runArgIndex]; }
            }
        fuckyou:

            if (stringName.CaselessEq("mbx")) { return this.data.MBCoords.X.ToString(); }
            if (stringName.CaselessEq("mby")) { return this.data.MBCoords.Y.ToString(); }
            if (stringName.CaselessEq("mbz")) { return this.data.MBCoords.Z.ToString(); }
            if (stringName.CaselessEq("playerx")) { return p.Pos.FeetBlockCoords.X.ToString(); }
            if (stringName.CaselessEq("playery")) { return p.Pos.FeetBlockCoords.Y.ToString(); }
            if (stringName.CaselessEq("playerz")) { return p.Pos.FeetBlockCoords.Z.ToString(); }
            if (stringName.CaselessEq("playerpx")) { return p.Pos.X.ToString(); }
            if (stringName.CaselessEq("playerpy")) { return (p.Pos.Y - Entities.CharacterHeight).ToString(); }
            if (stringName.CaselessEq("playerpz")) { return p.Pos.Z.ToString(); }
            if (stringName.CaselessEq("playeryaw")) { return Orientation.PackedToDegrees(p.Rot.RotY).ToString(); } //yaw
            if (stringName.CaselessEq("playerpitch")) { return Orientation.PackedToDegrees(p.Rot.HeadX).ToString(); } //pitch

            if (stringName.CaselessEq("msgdelay")) {
                double msgDelayMultiplier = 0;
                double.TryParse(GetString("msgDelayMultiplier"), out msgDelayMultiplier);
                return (amountOfCharsInLastMessage * msgDelayMultiplier).ToString();
            }

            if (stringName.CaselessEq("mbcoords")) {
                return this.data.MBCoords.X + " " + this.data.MBCoords.Y + " " + this.data.MBCoords.Z;
            }
            //feet block coords
            if (stringName.CaselessEq("playercoords")) {
                Vec3S32 pos = p.Pos.FeetBlockCoords;
                return pos.X + " " + pos.Y + " " + pos.Z;
            }
            //double coords for tempbot, particle, etc
            if (stringName.CaselessEq("playercoordsdecimal")) {
                double X = (p.Pos.X / 32f) - 0.5f;
                double Y = ((p.Pos.Y - Entities.CharacterHeight) / 32f);
                double Z = (p.Pos.Z / 32f) - 0.5f;
                return X + " " + Y + " " + Z;
            }
            //tpp units
            if (stringName.CaselessEq("playercoordsprecise")) { return p.Pos.X + " " + (p.Pos.Y - Entities.CharacterHeight) + " " + p.Pos.Z; }

            if (stringName.CaselessEq("epochMS")) { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(); }

            return scriptData.GetString(stringName, isOS, scriptName);
        }
        public void SetString(string stringName, string value) {
            scriptData.SetString(stringName, value, isOS, scriptName);
        }

        public const char beginParseSymbol = '{';
        public const char endParseSymbol = '}';
        const int maxRecursions = 32;
        string ParseMessage(string message, int recursions = 0) {
            if (recursions == 0) { message = ReplaceAts(message); } else if (recursions > maxRecursions) {
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
                if (curChar == beginParseSymbol) { openingBracketIndex = i; } else if (curChar == endParseSymbol) {
                    if (openingBracketIndex == -1) {
                        continue;
                    }

                    parsed.Append(message.Substring(indexOfEndOfLastParse, openingBracketIndex - indexOfEndOfLastParse)); //"Hey_" / ",_"

                    openingBracketIndex++;
                    //begin at index 5 for a length of 4 characters
                    parsed.Append(GetString(message.Substring(openingBracketIndex, i - openingBracketIndex))); //"name" / "mood"

                    indexOfEndOfLastParse = i + 1; //set the "start" to index 10
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

        public void Error(bool above = false) {
            if (above) { p.Message("&c↑ The above message came from a script error"); }
            //else { p.Message("&cScript error:"); }
            p.Message(ErrorTrace());
            ScriptActions.Terminate.Do(this);
        }
        string ErrorTrace() {
            return String.Format("&cScript {0} errored at line {1}, which started from {2}", scriptName, lineNumber, startLabel);
        }
    }


    public static class Wizardry {
        public static IEnumerable<Type> GetDerivedTypesFor(Type baseType) {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            return assembly.GetTypes()
                .Where(baseType.IsAssignableFrom)
                .Where(t => baseType != t);
        }
    }

    public static class ScriptActions {

        public static Dictionary<string, ScriptAction> Dic = new Dictionary<string, ScriptAction>();
        public static void PluginLoad() {
            Dic = new Dictionary<string, ScriptAction>();
            IEnumerable<Type> scriptActions = Wizardry.GetDerivedTypesFor(typeof(ScriptAction));

            foreach (Type t in scriptActions) {
                ScriptAction scriptAction;
                //Log.Msg("{0} becomes {1}", t.Name, t.Name.Substring("Action".Length).ToLowerInvariant());
                try {
                    scriptAction = (ScriptAction)Activator.CreateInstance(t);
                } catch (MemberAccessException e) {
                    //Catch exception when trying to create abstract instances. Do nothing in this case
                    continue;
                }

                Dic[scriptAction.name] = scriptAction;
            }
        }
        public static ScriptAction GetAction(string actionType) {
            ScriptAction action;
            if (!Dic.TryGetValue(actionType, out action)) { return null; }
            return action;
        }

        public abstract class ScriptAction {
            public abstract string[] documentation { get; }
            public abstract string name { get; }
            public abstract void Behavior(ScriptRunner run);
        }

        public class Message : ScriptAction {
            public override string[] documentation => new string[] {
                "[message]",
                "    sends a message to the player.",
                "    You can use @p to substitute their full player name (includes the +) or @nick to subtitute for a more natural version of their name (e.g. Mike_30+ becomes Mike)",
            };

            public override string name => "msg";

            public override void Behavior(ScriptRunner run) {
                if (!run.shouldSendCef && ScriptRunner.isCefMessage(run.args)) { return; }
                run.amountOfCharsInLastMessage = run.args.Length;
                run.p.Message(run.args);
            }
        }

        public class CpeMessage : ScriptAction {
            public override string[] documentation => new string[] {
                "[cpe message field] <message>",
                "    same as msg, but allows you to send to the other special chat fields in top right, bottom right, or center.",
                "    valid cpe message fields are: top1, top2, top3, bot1, bot2, bot3, announce, bigannounce, smallannounce",
                "    However, unlike msg, these are limited to 64 characters at most. Remember, color codes count as 2 characters!",
                "    The \"announce\" fields automatically disappears after 5 seconds.\",",
                "    The rest stay forever unless you reset them by sending a completely blank message (or the player leaves the map).",
                "    Blank example: \"cpemsg bot3\"",
            };

            public override string name => "cpemsg";

            public override void Behavior(ScriptRunner run) {
                if (run.cmdName == "") { run.Error(); run.p.Message("&cNot enough arguments for cpemsg"); return; }
                CpeMessageType type = ScriptRunner.GetCpeMessageType(run.cmdName);
                if (type == CpeMessageType.Announcement || type == CpeMessageType.BigAnnouncement || type == CpeMessageType.SmallAnnouncement) {
                    run.amountOfCharsInLastMessage = run.cmdArgs.Length;
                }
                run.p.SendCpeMessage(type, run.cmdArgs, ScriptRunner.CpeMessagePriority);
            }
        }

        public class Delay : ScriptAction {
            public override string[] documentation => new string[] {
                "[number or package]",
                "    Makes the script pause for the amount of milliseconds specified.",
            };

            public override string name => "delay";

            public override void Behavior(ScriptRunner run) {
                int delay;
                if (!run.GetIntRawOrVal(run.cmdName, "Delay", out delay)) { return; }
                Thread.Sleep(delay);
            }
        }

        public class Jump : ScriptAction {
            public override string[] documentation => new string[] {
                "[#label]",
                "    Makes the script go to the specified label and keep running from there.",
            };

            public override string name => "jump";

            public override void Behavior(ScriptRunner run) {
                //run.cmdName is label|runArg|runArg
                string[] newRunArgs = (run.cmdName).Split(ScriptRunner.pipeChar);
                run.TryReplaceRunArgs(newRunArgs);

                if (!run.script.GetLabel(newRunArgs[0], out run.actionIndex)) {
                    run.Error(); run.p.Message("&cUnknown label \"" + newRunArgs[0] + "\".");
                    run.p.Message(CmdScript.labelHelp);
                }
            }
        }

        public class Goto : Jump {
            public override string[] documentation => new string[] {
                "[#label]",
                "    This is the same as jump, but with a big exception:",
                "    If you use \"goto\" in a label that you called with the \"call\" Action, then the script will not come back to run what was after the call.",
                "    In other words, this performs a jump and then clears the call stack",
            };

            public override string name => "goto";

            public override void Behavior(ScriptRunner run) {
                run.comebackToIndex.Clear();
                base.Behavior(run);
            }
        }

        public class Call : Jump {
            public override string[] documentation => new string[] {
                "[#label]",
                "    Like jump, but once it reaches a \"quit\" in the [#label] called, instead of quitting, it will come back and run what comes after the call.",
                "    This is useful because it lets you repeat a set of actions many times without copy pasting the actions all over the place.",
            };

            public override string name => "call";

            public override void Behavior(ScriptRunner run) {
                run.comebackToIndex.Add(run.actionIndex);
                base.Behavior(run);
            }
        }

        public class Quit : ScriptAction {
            public override string[] documentation => new string[] {
                "",
                "    Typically this tells the script to stop running.",
                "    If we are in a label ran with the \"call\" Action, this causes the script to return to where it was called from.",
                "    Be careful not to forget this. Without a \"quit\", the script will keep running and do actions from other labels below.",
            };

            public override string name => "quit";

            public override void Behavior(ScriptRunner run) {
                if (run.comebackToIndex.Count == 0) { Terminate.Do(run); return; }

                //make the index where it left off
                run.actionIndex = run.comebackToIndex[run.comebackToIndex.Count - 1];
                run.comebackToIndex.RemoveAt(run.comebackToIndex.Count - 1);

                if (run.script.uses.quitResetsRunArgs) { run.ResetRunArgs(); }
            }
        }

        public class Terminate : ScriptAction {
            public override string[] documentation => new string[] {
                "",
                "    This tells the script to stop running completely, even if we are in a nested label that was called from somewhere else using \"call\"",
            };

            public override string name => "terminate";

            public override void Behavior(ScriptRunner run) {
                Do(run);
            }
            public static void Do(ScriptRunner run) {
                //make the index at the end so it's completely finished
                run.actionIndex = run.script.actions.Count;
                foreach (Thread thread in run.newthreads) {
                    thread.Join();
                }
                //p.Message("Putting thisBool to false and quitting the entire script.");
                if (!run.repeatable) run.p.Extras[run.thisBool] = false;
            }
        }

        public class NewThread : ScriptAction {
            public override string[] documentation => new string[] {
                "[#label]",
                "    Like call, but allows the script to continue running without taking into account any of the delays in the label you called.",
                "    This action may take a little bit of time to start up. If you want to make sure it always occurs BEFORE the actions you put next,",
                "    you should add a little bit of delay (around 500 perhaps) after doing a newthread action.",
            };

            public override string name => "newthread";

            public override void Behavior(ScriptRunner run) {
                if (run.cmdName.Length == 0) { run.Error(); run.p.Message("&cPlease specify a label and or runArgs for the newthread to run with."); return; }

                string[] newThreadRunArgs = (run.cmdName).Split(ScriptRunner.pipeChar);
                string newThreadLabel = newThreadRunArgs[0];
                if (newThreadRunArgs.Length == 1) {
                    //no new runArgs were specified
                    newThreadRunArgs = (string[])run.runArgs.Clone(); //clone it so changing args in newthread doesn't change them in this script, yikes
                } else {
                    //they specified new runArgs
                    newThreadRunArgs[0] = run.runArgs[0]; //preserve starting label from original script instance
                    ScriptRunner.ReplaceUnderScoreWithSpaceInRunArgs(ref newThreadRunArgs);
                }

                if (!run.script.HasLabel(newThreadLabel)) { run.Error(); run.p.Message("&cUnknown newthread label \"" + newThreadLabel + "\"."); return; }
                ScriptRunner scriptRunner = ScriptRunner.Create(run.p, run.scriptName, run.isOS, run.data, run.repeatable, run.thisBool, newThreadRunArgs);
                if (scriptRunner == null) { run.Error(); return; }
                run.DoNewThreadRunScript(scriptRunner, newThreadLabel);
            }
        }

        public class Set : ScriptAction {
            public override string[] documentation => new string[] {
                "[package] [value]",
                "    Sets the [value] of [package]. If you want to set the value of a package to the value of another package, you have to unwrap it in the value argument with { }.",
                "    For example:",
                "-       set maxHealth 10",
                "-       set myHealth {maxHealth}",
                "    This results in myHealth with a value of \"10\".",
                "    If you fail to unwrap maxHealth you would be left with a text value of \"maxHealth\" for myHealth, which is definitely not what you want in this case.",
            };

            public override string name => "set";

            public override void Behavior(ScriptRunner run) {
                run.SetString(run.cmdName, run.cmdArgs);
            }
        }

        public abstract class SetMath : ScriptAction {
            public override string[] documentation => throw new MemberAccessException();

            public override string name => throw new MemberAccessException();

            public override void Behavior(ScriptRunner run) {
                double a, b, result;
                if (!ValidateNumberOperation(run, out a, out b)) { return; }

                try {
                    result = Op(a, b);
                } catch (DivideByZeroException) {
                    run.Error(); run.p.Message("&cCannot divide {0}={1} by {2}={3} because division by zero does not result in a real number.", run.cmdName, a, run.cmdArgs, b);
                    return;
                }
                run.SetDouble(run.cmdName, result);
            }
            static bool ValidateNumberOperation(ScriptRunner run, out double doubleValue, out double doubleValue2) {
                doubleValue2 = 0;
                if (!run.GetDouble(run.cmdName, out doubleValue)) { return false; }
                if (double.TryParse(run.cmdArgs, out doubleValue2)) { return true; } //second arg is already a number, return its value
                return run.GetDouble(run.cmdArgs, out doubleValue2); //second arg is not a number and is therefore assumed to be a valueName. Try to get the value of the valueName.
            }
            protected abstract double Op(double a, double b);
        }
        public class SetAdd : SetMath {
            public override string[] documentation => new string[] {
                "[package] [number or package]",
                "    Adds the second argument to the first argument.",
                "    For example:",
                "-       set healthPotionBoost 5",
                "-       set maxHealth 10",
                "-       set myHealth {maxHealth}",
                "-       setadd myHealth healthPotionBoost",
                "    This results in myHealth with a value of \"15\".",
                "    Or use a raw number:",
                "-       setadd myHealth 3",
                "    This results in myHealth with a value of \"13\" (assuming it was 10 to begin with).",
            };

            public override string name => "setadd";

            protected override double Op(double a, double b) {
                return a + b;
            }
        }
        public class SetSub : SetMath {
            public override string[] documentation => new string[] {
                "[package] [number or package]",
                "    Same as setadd, but subtracts.",
            };

            public override string name => "setsub";

            protected override double Op(double a, double b) {
                return a - b;
            }
        }
        public class SetMul : SetMath {
            public override string[] documentation => new string[] {
                "[package] [number or package]",
                "    Same as setadd, but multiplies.",
            };

            public override string name => "setmul";

            protected override double Op(double a, double b) {
                return a * b;
            }
        }
        public class SetDiv : SetMath {
            public override string[] documentation => new string[] {
                "[package] [number or package]",
                "    Same as setadd, but divides.",
            };

            public override string name => "setdiv";

            protected override double Op(double a, double b) {
                if (b == 0) throw new DivideByZeroException();
                return a / b;
            }
        }
        public class SetMod : SetMath {
            public override string[] documentation => new string[] {
                "[package] [number or package]",
                "    Sets the first argument to the remainder of an integer division between the first argument and the second argument.",
                "    This is the equivalent of \"firstArg = firstArg % secondArg;\" in c-like languages.",
            };

            public override string name => "setmod";

            protected override double Op(double a, double b) {
                if (b == 0) throw new DivideByZeroException();

                //https://stackoverflow.com/questions/1082917/mod-of-negative-number-is-melting-my-brain
                double r = a % b;
                return r < 0 ? r + b : r;
            }
        }
        public class SetPow : SetMath {
            public override string[] documentation => new string[] {
                "[package] [number or package]",
                "    Performs a math \"power of\" operation on [package] and sets that as its new value.",
                "    For instance, with 3 as the second arg, you can calculate the 3D volume of a number.",
            };

            public override string name => "setpow";

            protected override double Op(double a, double b) {
                return Math.Pow(a, b);
            }
        }
        public class SetSin : SetMath {
            public override string[] documentation => new string[] {
                "[package] [number or package]",
                "    Calculates sine of [number or package] and inserts it into [package]",
            };

            public override string name => "setsin";

            protected override double Op(double a, double b) {
                return Math.Sin(b);
            }
        }
        public class SetCos : SetMath {
            public override string[] documentation => new string[] {
                "[package] [number or package]",
                "    Calculates cosine of [number or package] and inserts it into [package]",
            };

            public override string name => "setcos";

            protected override double Op(double a, double b) {
                return Math.Cos(b);
            }
        }
        public class SetTan : SetMath {
            public override string[] documentation => new string[] {
                "[package] [number or package]",
                "    Calculates the tangent of [number or package] and inserts it into [package].",
                "    The specified angle will be treated as radians.",
            };

            public override string name => "settan";

            protected override double Op(double a, double b) {
                return Math.Tan(b);
            }
        }

        public class SetRandRange : ScriptAction {
            public override string[] documentation => new string[] {
                "[package] [number or package] [number or package]",
                "    Sets the first argument to a random integer that is within the range of the second and third args.",
                "    For example:",
                "-       setrandrange attackDamage 1 5",
                "    attackDamage can have the value of 1, 2, 3, 4, or 5, randomly chosen.",
            };

            public override string name => "setrandrange";

            public override void Behavior(ScriptRunner run) {
                string[] bits = run.args.SplitSpaces();
                if (bits.Length < 3) { run.Error(); run.p.Message("&cNot enough arguments for SetRandRange action"); return; }
                int min, max;
                if (!run.GetIntRawOrVal(bits[1], "SetRandRange", out min)) { return; }
                if (!run.GetIntRawOrVal(bits[2], "SetRandRange", out max)) { return; }
                if (min > max) { run.Error(); run.p.Message("&cMin value for SetRandRange must be smaller than max value."); return; }
                run.SetDouble(bits[0], ScriptRunner.RandomRange(min, max));
            }
        }
        public class SetRandRangeDecimal : ScriptAction {
            public override string[] documentation => new string[] {
                "[package] [number or package] [number or package]",
                "    Sets the first argument to a random number that is within the range of the second and third args.",
                "    This is identical to setrandrange, except this time the range can truly result in any number, and will most often be something fractional with a decimal place.",
                "    For example:",
                "-       setrandrangedecimal attackDamage 1 5",
                "    attackDamage could have a value of 0.306, 2.4553, 4.853, etc.",
            };

            public override string name => "setrandrangedecimal";

            public override void Behavior(ScriptRunner run) {
                string[] bits = run.args.SplitSpaces();
                if (bits.Length < 3) { run.Error(); run.p.Message("&cNot enough arguments for SetRandRangeDecimal action"); return; }
                double min, max;
                if (!run.GetDoubleRawOrVal(bits[1], "SetRandRangeDecimal", out min)) { return; }
                if (!run.GetDoubleRawOrVal(bits[2], "SetRandRangeDecimal", out max)) { return; }
                if (min > max) { run.Error(); run.p.Message("&cMin value for SetRandRangeDecimal must be smaller than max value."); return; }
                run.SetDouble(bits[0], ScriptRunner.RandomRangeDouble(min, max));
            }
        }

        public class SetRandList : ScriptAction {
            public override string[] documentation => new string[] {
                "[package] [value1]|[value2]|[value3] etc...",
                "    Sets the first argument to one of the given values that are separated by the | symbol.",
                "    For example:",
                "-       setrandlist myWarriorName Zog the Destroyer|Kron the Cunning|Dunidas of Kas",
                "    myWarriorName could be \"Zog the Destroyer\", \"Kron the Cunning\", or \"Dunidas of Kas\", chosen randomly.",
                "    Note that if you want to use a package as one or more of the values you must unwrap it, just like the set action.",
            };

            public override string name => "setrandlist";

            public override void Behavior(ScriptRunner run) {
                if (run.cmdArgs == "") { run.Error(); run.p.Message("SetRandList requires a list of values to choose from separated by the | character."); return; }
                run.SetString(run.cmdName, ScriptRunner.RandomEntry(run.cmdArgs.Split(ScriptRunner.pipeChar)));
            }
        }

        public abstract class SetRoundCore : ScriptAction {
            public override string[] documentation => throw new MemberAccessException();

            public override string name => throw new MemberAccessException();

            public override void Behavior(ScriptRunner run) {
                double value;
                if (run.cmdName.Length == 0) { run.Error(); run.p.Message("&cNo value provided to round."); return; }
                if (!run.GetDouble(run.cmdName, out value)) { return; }
                run.SetDouble(run.cmdName, (int)Op(value));
            }

            protected abstract double Op(double value);
        }
        public class SetRound : SetRoundCore {
            public override string[] documentation => new string[] {
                "[package]",
                "    Rounds the value of the package to the nearest integer.",
                "    For example, 1.2 rounds to 1 and 1.6 rounds to 2. If the number ends with .5, it will round up.",
            };

            public override string name => "setround";

            protected override double Op(double value) {
                return Math.Round(value, MidpointRounding.AwayFromZero);
            }
        }
        public class SetRoundUp : SetRoundCore {
            public override string[] documentation => new string[] {
                "[package]",
                "    Rounds the value of the package up to the next integer. For example, 1.1 becomes 2.",
            };

            public override string name => "setroundup";

            protected override double Op(double value) {
                return Math.Ceiling(value);
            }
        }
        public class SetRoundDown : SetRoundCore {
            public override string[] documentation => new string[] {
                "[package]",
                "    Rounds the value of the package down to the next integer. For example, 1.9 becomes 1.",
            };

            public override string name => "setrounddown";

            protected override double Op(double value) {
                return Math.Floor(value);
            }
        }

        public class Show : ScriptAction {
            public override string[] documentation => new string[] {
                "[package] <another package> <another package> etc...",
                "    Displays the value of all the packages given, for testing and debug purposes.",
                "    All but the first argument is optional.",
                "show every single package",
                "    Displays every single non-saved package",
            };

            public override string name => "show";

            public override void Behavior(ScriptRunner run) {
                if (run.args.CaselessEq("every single package")) {
                    run.scriptData.ShowAllStrings();
                    return;
                }
                string[] values = run.args.SplitSpaces();
                bool saved;

                foreach (string value in values) {
                    run.p.Message("The value of &b{0} &Sis \"&o{1}&S\".", run.scriptData.ValidateStringName(value, run.isOS, run.scriptName, out saved).ToLower(), run.GetString(value));
                }
            }
        }

        public class Kill : ScriptAction {
            public override string[] documentation => new string[] {
                "<message>",
                "    Kills the player with an optional public death message.",
                "    The <message> is shown to everyone who is playing on the map.",
                "    Because of this, it's highly recommended to not use the <message> argument, and instead use msg to tell the player directly why they died.",
            };

            public override string name => "kill";

            public override void Behavior(ScriptRunner run) {
                run.p.HandleDeath(Block.Cobblestone, run.args, false, true);
            }
        }

        public class Cmd : ScriptAction {
            public override string[] documentation => new string[] {
                "[command] <command arguments>",
                "    Runs the given command with the given arguments.",
                "    You can use @p and @nick to substitute player names just like in msg.",
            };

            public override string name => "cmd";

            public override void Behavior(ScriptRunner run) {
                Command cmd = null;
                if (!run.ValidateCommand(out cmd)) {
                    return;
                }
                run.DoCmd(cmd, run.cmdArgs);
            }
        }

        public class ResetData : ScriptAction {
            public override string[] documentation => new string[] {
                "[type] <pattern>",
                "    Used to reset data.",
                "    [type] can be",
                "        \"packages\" - resets packages",
                "        \"items\" - resets items",
                "        \"saved\" - resets saved packages related to the current script (staff only)",
                "    <pattern> is an optional search pattern that only resets matching names. If not specified, everything is reset.",
                "        Use the special characters * and ? to specify the search pattern.",
                "        * is a substitute for 0 or more characters, and ? is a substitute for 1 character.",
                "        For example:",
                "-           resetdata packages oldman_*",
                "            This resets all packages that have a name that starts with the word \"oldman_\"",
                "-           resetdata packages *_oldman",
                "            This resets all packages that have a name that ends with the word \"_oldman\"",
                "        If you do not use either * or ?, it will reset all packages which contain the pattern you specify.",
                "        For example:",
                "-           resetdata packages old",
                "            This resets all packages which have \"old\" anywhere in the name.",
                "            Note: this is actually identical to",
                "-           resetdata packages *old*",
            };

            public override string name => "resetdata";

            public override void Behavior(ScriptRunner run) {
                if (run.cmdName.CaselessStarts("packages")) {
                    run.scriptData.Reset(true, false, run.cmdArgs);
                    return;
                }
                if (run.cmdName.CaselessStarts("items")) {
                    if (!run.isOS) { run.Error(); run.p.Message("&cCannot reset items in non-os script"); return; }
                    run.scriptData.Reset(false, true, run.cmdArgs);
                    return;
                }
                if (run.cmdName.CaselessStarts("saved")) {
                    if (run.isOS) { run.Error(); run.p.Message("&cCannot reset saved packages in os script (because there are none)"); return; }
                    run.scriptData.ResetSavedStrings(run.scriptName, run.cmdArgs);
                    return;
                }
                run.Error(); run.p.Message("&cYou must specify what type of data to reset");
            }
        }

        public class Item : ScriptAction {
            public override string[] documentation => new string[] {
                "[get/take] [ITEM_NAME]",
                "    Gives an item to the player or takes an item from the player.",
                "    You must use underscores instead of spaces for the item name, especially when checking if the player has an item (see \"Conditions\" further down).",
                "    This is silent if you \"get\" an item when the player already has said item, and silent if you \"take\" an item when the player doesn't have said item.",
            };

            public override string name => "item";

            public override void Behavior(ScriptRunner run) {
                if (run.cmdArgs == "") { run.Error(); run.p.Message("&cNot enough arguments for Item action"); return; }
                if (run.cmdName == "get" || run.cmdName == "give") { run.scriptData.GiveItem(run.cmdArgs, run.isOS); return; }
                if (run.cmdName == "take" || run.cmdName == "remove") { run.scriptData.TakeItem(run.cmdArgs, run.isOS); return; }
                run.Error(); run.p.Message("&cUnknown function for Item action: \"{0}\"", run.cmdName);
            }
        }

        public class Freeze : ScriptAction {
            public override string[] documentation => new string[] {
                "",
                "    Freezes the player in place. They can still fall if mid-air or swim up and down in liquid or ladders, though.",
            };

            public override string name => "freeze";

            public override void Behavior(ScriptRunner run) {
                run.scriptData.frozen = true;
                run.p.Send(Packet.Motd(run.p, "-hax horspeed=0.000001 jumps=0 -push"));
            }
        }
        public class Unfreeze : ScriptAction {
            public override string[] documentation => new string[] {
                "",
                "    Unfreezes the player.",
            };

            public override string name => "unfreeze";

            public override void Behavior(ScriptRunner run) {
                run.scriptData.frozen = false;
                if (run.scriptData.customMOTD != null) {
                    SendMOTD(run, run.scriptData.customMOTD);
                } else {
                    // do not use p.SendMapMotd() because it triggers the event which makes locked model update, which we don't want
                    run.p.Send(Packet.Motd(run.p, run.p.GetMotd()));
                }
            }
        }

        public class Look : ScriptAction {
            public override string[] documentation => new string[] {
                "[block coordinates]",
                "    Makes the player look at the given coordinates. They can move their camera afterwards.",
            };

            public override string name => "look";

            public override void Behavior(ScriptRunner run) {
                Vec3S32 coords;
                if (!GetCoords(run, "Look", out coords)) { return; }
                ScriptRunner.LookAtCoords(run.p, coords);
            }
        }
        public class Stare : ScriptAction {
            public override string[] documentation => new string[] {
                "<block coordinates>",
                "    Forces the player to continually stare at the given coordinates. You can free their camera by not providing any coordinates to this action.",
            };

            public override string name => "stare";

            public override void Behavior(ScriptRunner run) {
                if (run.args == "") { run.scriptData.stareCoords = null; return; }

                Vec3S32 coords;
                if (!GetCoords(run, "Stare", out coords)) { return; }
                run.scriptData.stareCoords = coords;
            }
        }

        public class Env : ScriptAction {
            public override string[] documentation => new string[] {
                "[property] [value]",
                "    Temporarily changes env values for the player who runs the script.",
                "    Valid properties are currently:",
                "        sky [hex color]",
                "        cloud [hex color]",
                "        cloudspeed [speed]",
                "        cloudheight [height]",
                "        fog [hex color]",
                "        shadow [hex color]",
                "        sun [hex color]",
                "        skybox [hex color]",
                "        weather [sun/rain/snow]",
                "        maxfog [distance in blocks]",
                "        expfog [on/off]",
            };

            public override string name => "env";

            public override void Behavior(ScriptRunner run) {
                run.SetEnv(run.args);
            }
        }

        public class MOTD : ScriptAction {
            public override string[] documentation => new string[] {
                "[motd arguments]",
                "    Sends an MOTD to the player to control hacks using hacks flags.",
                "    To see a list of flags you can use, type /help map motd",
                "    2021/12/11: jumpheight works too now",
                "motd ignore",
                "    Resets to the default MOTD of the map.",
            };

            public override string name => "motd";

            public override void Behavior(ScriptRunner run) {
                if (run.args.CaselessEq("ignore")) {
                    run.scriptData.customMOTD = null;
                    // do not use p.SendMapMotd() because it triggers the event which makes locked model update, which we don't want
                    run.p.Send(Packet.Motd(run.p, run.p.GetMotd()));
                    return;
                }

                run.scriptData.customMOTD = run.args;
                SendMOTD(run, run.scriptData.customMOTD);
            }
        }

        public class SetSpawn : ScriptAction {
            public override string[] documentation => new string[] {
                "[block coords]",
                "    Sets the spawn of the player to the coordinates provided.",
            };

            public override string name => "setspawn";

            public override void Behavior(ScriptRunner run) {
                Vec3F32 coords;
                if (!GetCoordsFloat(run, "SetSpawn", out coords)) { return; }

                Position pos = new Position();
                pos.X = (int)(coords.X * 32) + 16;
                pos.Y = (int)(coords.Y * 32) + Entities.CharacterHeight;
                pos.Z = (int)(coords.Z * 32) + 16;
                //p.Message("coords {0} {1} {2}", coords.X, coords.Y, coords.Z);
                //p.Message("pos.X {0} pos.Y {1} pos.Z {2}", pos.X, pos.Y, pos.Z);

                if (run.p.Supports(CpeExt.SetSpawnpoint)) {
                    run.p.Send(Packet.SetSpawnpoint(pos, run.p.Rot, run.p.Supports(CpeExt.ExtEntityPositions)));
                } else {
                    run.p.SendPos(Entities.SelfID, pos, run.p.Rot);
                    Entities.Spawn(run.p, run.p);
                }
                run.p.Message("Your spawnpoint was updated.");
            }
        }

        public class Reply : ScriptAction {
            public override string[] documentation => new string[] {
                "[option number]|[text shown to player]|[#label to call if chosen]",
                "    Sets up a reply option, which can be chosen by the player by typing [option number] in chat",
                "    For example:",
                "-       reply 1|You: Sure thing.|#replyYes",
                "-       reply 2|You: No thanks.|#replyNo",
                "-       reply 3|You: Can you elaborate?|#replyElaborate",
                "    After these actions happen, saying \"1\" will call #replyYes, and so on.",
                "    The maximum amount of replies you can setup at once is 6.",
                "    !!! However !!!, it is recommended not to use 4, 5, 6, because they appear at the top of the screen where they are hard to find and read",
                "    (especially if the sky is bright).",
                "    Hot tip: use the freeze action if you want to force the player to choose before moving on.",
                "reply clear",
                "    Clears all current replies from being visible and useable (this includes silent replies)",
            };

            public override string name => "reply";

            public override void Behavior(ScriptRunner run) {
                run.DoReply(true);
            }
        }
        public class ReplySilent : ScriptAction {
            public override string[] documentation => new string[] {
                "",
                "    Identical to reply, with two exceptions:",
                "        Does not notify the player that they should choose a response",
                "        Does not remind the player to choose a response if they chat while silent replies are active.",
                "replysilent clear",
                "    Clears all current replies from being visible and useable (this includes non-silent replies)",
            };

            public override string name => "replysilent";

            public override void Behavior(ScriptRunner run) {
                run.DoReply(false);
            }
        }

        public abstract class CommandShortcut : ScriptAction {
            public override string[] documentation => new string[] {
                "[args]",
                "    Shortcut for \"cmd " + name + " [args]\". See /help " + name + " for more info.",
                "    Has faster performance than calling the command with the cmd Action.",
            };

            public override void Behavior(ScriptRunner run) {
                run.DoCmdNoPermChecks(cmd, run.args);
            }

            protected abstract Command cmd { get; }
        }
        public class TempBlock : CommandShortcut {
            public override string name => "tempblock";

            protected override Command cmd => Core.tempBlockCmd;
        }
        public class TempChunk : CommandShortcut {
            public override string name => "tempchunk";

            protected override Command cmd => Core.tempChunkCmd;
        }
        public class Boost : CommandShortcut {
            public override string name => "boost";

            protected override Command cmd => Core.boostCmd;
        }
        public class Effect : CommandShortcut {
            public override string name => "effect";

            protected override Command cmd => Core.effectCmd;
        }

        public class Reach : ScriptAction {
            public override string[] documentation => new string[] {
                "[distance]",
                "    Temporarily sets the player's reach distance, in blocks.",
                "    A change in MOTD will reset this. For example, switching maps, switching zones, being frozen or unfrozen.",
            };

            public override string name => "reach";

            public override void Behavior(ScriptRunner run) {
                double dist = 0;
                if (!run.GetDoubleRawOrVal(run.cmdName, "Reach", out dist)) { return; }

                int packedDist = (int)(dist * 32);
                if (packedDist > short.MaxValue) { run.Error(); run.p.Message("&cReach of \"{0}\", is too long. Max is 1023 blocks.", dist); return; }

                run.p.Send(Packet.ClickDistance((short)packedDist));
            }
        }

        public class SetBlockID : ScriptAction {
            public override string[] documentation => new string[] {
                "[package] [block coordinates]",
                "    Sets the value of [package] to the ID of the block at the given [block coordinates]",
                "    IMPORTANT: this action does *not* see blocks that have been changed with tempblock or tempchunk!",
                "    It only gets the ID of the block that was there in the original map.",
                "    The ID of the block retrieved is the same as the ID of the block *clientside*, meaning something like hot_lava will be read as \"11\" from setblockid.",
            };

            public override string name => "setblockid";

            public override void Behavior(ScriptRunner run) {
                string[] bits = run.args.SplitSpaces(4);
                if (bits.Length < 4) {
                    run.Error();
                    run.p.Message("&cYou need to specify a package and x y z coordinates of the block to retrieve the ID of.");
                    return;
                }
                string packageName = bits[0];
                string[] stringCoords = new string[] { bits[1], bits[2], bits[3] };

                Vec3S32 coords = new Vec3S32();
                if (!CommandParser.GetCoords(run.p, stringCoords, 0, ref coords)) { run.Error(true); }
                run.SetString(packageName, ClientBlockID(run.p.level.GetBlock((ushort)coords.X, (ushort)coords.Y, (ushort)coords.Z)).ToString());
            }
            static BlockID ClientBlockID(BlockID serverBlockID) {
                return Block.ToRaw(Block.Convert(serverBlockID));
            }
        }

        public class DefineHotkey : ScriptAction {
            public override string[] documentation => new string[] {
                "[input args]|[key name]|<list of space separated modifiers>",
                "    This feature allows the player to run the #input label by pressing a key.",
                "    [input args] will be sent as an automatic command /input [input args]",
                "    [key name] must match a key name from the LWJGL keycode specification.",
                "    You can find the names here: https://www.dropbox.com/scl/fi/uyijnzp6thyfizd9pid3o/nasKeycodes.txt?rlkey=j58hwzqjnixcmhq7sqxpw4v4o&dl=1",
                "    <list of space separated modifiers> is optional and can be any combination of \"ctrl\" \"shift\" \"alt\" and \"async\"",
                "    async is a unique modifier that doesn't change what keys must be pressed, but allows the input to run repeatedly before the previous input is finished.",
                "    async will call the label #inputAsync instead of the label #input.",
                "    For example:",
                "-       definehotkey lose|L",
                "        If the player presses the L key, the script will run #input with runArg1 as \"lose\"",
                "-       definehotkey superjump wow|EQUALS|ctrl shift",
                "        If the player presses the equals key while holding ctrl and shift, the script will run #input with runArg1 as \"superjump\" and runArg2 as \"wow\"",
                "    See \"special labels section\" for more information on how the special label #input works.",
                "    IMPORTANT: for technical reasons, underscore will always be converted to space in the hotkey args,",
                "               so you cannot rely on checking for underscores in the runArgs it sends to #input because they will be spaces.",
            };

            public override string name => "definehotkey";

            public override void Behavior(ScriptRunner run) {
                // definehotkey [input args]|[key name]|<list of space separated modifiers>
                // definehotkey this is put into slash input!|equals|alt shift

                string[] bits = run.args.Split(ScriptRunner.pipeChar);
                if (bits.Length < 2) { run.Error(); run.p.Message("&cNot enough arguments to define a hotkey: \"" + run.args + "\"."); return; }

                string content = bits[0];
                int keyCode = run.scriptData.hotkeys.GetKeyCode(bits[1]);
                if (keyCode == 0) { run.Error(true); return; }
                string modifierArgs = bits.Length > 2 ? bits[2].ToLower() : "";
                byte modifiers = Hotkeys.GetModifiers(modifierArgs);
                bool repeatable = modifierArgs.Contains("async");


                string action = Hotkeys.FullAction(content, repeatable);
                if (action.Length > NetUtils.StringSize) {
                    run.Error();
                    run.p.Message("&cThe hotkey that script is trying to send (&7{0}&c) is &e{1}&c characters long, but can only be &a{2}&c at most.", action, action.Length, NetUtils.StringSize);
                    run.p.Message("You can remove &a{0}&S or more characters to fix this error.", action.Length - NetUtils.StringSize);
                    return;
                }
                run.scriptData.hotkeys.Define(action, keyCode, modifiers);
            }
        }
        public class UndefineHotkey : ScriptAction {
            public override string[] documentation => new string[] {
                "[key name]|<list of space separated modifiers> ",
                "    This Action compliments definehotkey by allowing you to remove hotkeys.",
                "    Note that you must include matching modifiers to undefine a hotkey that has those modifiers.",
                "    For example:",
                "        undefinehotkey L",
                "        undefinehotkey L|shift",
                "        If you have L and L with shift defined, you must also undefine L and L with shift to remove everything from the L key.",
                "    As a final note, all defined hotkeys are removed when the player switches maps, so undefining is not required if you want them to stay for the duration of the map.",
            };

            public override string name => "undefinehotkey";

            public override void Behavior(ScriptRunner run) {
                string[] bits = run.args.Split(ScriptRunner.pipeChar);
                int keyCode = run.scriptData.hotkeys.GetKeyCode(bits[0]); if (keyCode == 0) { run.Error(true); return; }

                byte modifiers = 0;
                if (bits.Length > 1) {
                    string modifierArgs = bits[1];
                    modifiers = Hotkeys.GetModifiers(modifierArgs);
                }
                run.scriptData.hotkeys.Undefine(keyCode, modifiers);
            }
        }

        public class PlaceBlock : ScriptAction {
            public override string[] documentation => new string[] {
                "[block] [block coordinates]",
                "    Used to place blocks in the map.",
                "    Unlike tempblock, these are permanently placed just like editing the map for real, so caution should be taken when using this Action.",
                "    ANYONE who runs the script in your map can potentially place blocks if the script runs this Action.",
            };

            public override string name => "placeblock";

            public override void Behavior(ScriptRunner run) {
                string[] bits = run.args.SplitSpaces();
                Vec3S32 coords = new Vec3S32();
                if (bits.Length < 4) { run.Error(); run.p.Message("&cNot enough arguments for placeblock"); return; }
                if (!CommandParser.GetCoords(run.p, bits, 1, ref coords)) { run.Error(true); return; }

                BlockID block = 0;
                if (!CommandParser.GetBlock(run.p, bits[0], out block)) { return; }


                if (!MCGalaxy.Group.GuestRank.CanPlace[block]) {
                    string blockName = Block.GetName(run.p, block);
                    run.Error();
                    run.p.Message("&cRank {0} &cis not allowed to use block \"{1}\". Therefore, script cannot place it.",
                        MCGalaxy.Group.GuestRank.ColoredName, blockName);
                    return;
                }
                BlockID deleted = run.startingLevel.GetBlock((ushort)coords.X, (ushort)coords.Y, (ushort)coords.Z);
                if (!MCGalaxy.Group.GuestRank.CanDelete[deleted]) {
                    string blockName = Block.GetName(run.p, deleted);
                    run.Error();
                    run.p.Message("&cRank {0} &cis not allowed to delete block \"{1}\". Therefore, script cannot replace it.",
                        MCGalaxy.Group.GuestRank.ColoredName, blockName);
                    return;
                }

                coords = run.startingLevel.ClampPos(coords);

                run.startingLevel.SetBlock((ushort)coords.X, (ushort)coords.Y, (ushort)coords.Z, block);
                run.startingLevel.BroadcastChange((ushort)coords.X, (ushort)coords.Y, (ushort)coords.Z, block);
            }
        }

        public class ChangeModel : ScriptAction {
            public override string[] documentation => new string[] {
                "<model>",
                "    This Action allows you to temporarily change what model people have for the current world.",
                "    Run this Action with no arguments to set the player's model back to what it was before.",
                "    This Action only works if the MOTD of the level has one or more models forced with model=[something]",
            };

            public override string name => "changemodel";

            public override void Behavior(ScriptRunner run) {
                if (run.cmdName == "") {
                    if (run.scriptData.oldModel != null) {
                        run.p.UpdateModel(run.scriptData.oldModel);
                    }
                    return;
                }
                string[] models = GetLockedModels(run.p.GetMotd());
                if (models == null) {
                    //p.Message("&cchangemodel Action is only allowed when you specify model= in map MOTD.");
                    return;
                }
                //don't let them change twice to same model otherwise revert doesnt work
                if (run.p.Model.CaselessEq(run.cmdName)) { return; }

                run.scriptData.oldModel = run.p.Model;
                run.scriptData.newModel = run.cmdName;

                run.p.UpdateModel(run.cmdName);
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

        public class Award : ScriptAction {
            public override string[] documentation => new string[] {
                "[award]",
                "    Gives the player [award]",
                "    This Action is not available in OS scripts",
            };

            public override string name => "award";

            public override void Behavior(ScriptRunner run) {
                if (run.isOS) { run.p.Message("&WThe award action is not available in OS scripts."); return; }
                Naward.GiveTo(run.p, run.args);
            }
        }

        public class SetSplit : ScriptAction {
            public override string[] documentation => new string[] {
                "[package] <splitter>",
                "    Copies the contents of [package], then splits them up into a new set of packages such that",
                "-       set myPackage Hey",
                "-       setsplit myPackage",
                "    gives you:",
                "        myPackage[0] = H",
                "        myPackage[1] = e",
                "        myPackage[2] = y",
                "        myPackage.Length = 3",
                "    <splitter> is optional and determines what character(s) the package is split up by. You can use quotes to specify a space.",
                "    For example:",
                "-       set mySentence Good morning!",
                "-       setsplit mySentence \" \"",
                "    gives you:",
                "        mySentence[0] = Good",
                "        mySentence[1] = morning!",
                "        mySentence.Length = 2",
            };

            public override string name => "setsplit";

            public override void Behavior(ScriptRunner run) {
                if (run.args == "") { run.Error(); run.p.Message("&cNot enough arguments for setsplit"); return; }
                string str = run.cmdName;
                System.Func<int, string> index = (i) => {
                    return String.Format("{0}[{1}]", str, i.ToString());
                };

                string value = run.GetString(str);

                if (run.cmdArgs == "") {
                    for (int i = 0; i < value.Length; i++) {
                        run.SetString(index(i), value[i].ToString());
                    }
                    run.SetString(str + ".Length", value.Length.ToString());
                    return;
                }

                string[] separator = new string[] { EscapeQuotes(run.cmdArgs) };

                string[] split = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < split.Length; i++) {
                    run.SetString(index(i), split[i]);
                }
                run.SetString(str + ".Length", split.Length.ToString());
            }

            static string EscapeQuotes(string input) {
                if (!input.StartsWith("\"") && !input.EndsWith("\"")) { return input; }
                return input.Substring(1, input.Length - 2);
            }
        }

        public class SetDirVector : ScriptAction {
            public override string[] documentation => new string[] {
                "[xPackage] [yPackage] [zPackage] [yaw number or package] [pitch number or package]",
                "    Sets the value of x y and z packges to a 3D direction vector based on yaw and pitch in degrees.",
                "    You can use this to get a direction (e.g. for /boost) based on where the player is looking if you use playerYaw and playerPitch packages as yaw and pitch.",
            };

            public override string name => "setdirvector";

            public override void Behavior(ScriptRunner run) {
                string[] argBits = run.args.SplitSpaces();
                if (argBits.Length < 5) { run.Error(); run.p.Message("&cNot enough arguments for setdirvector (expected 5)"); return; }
                string xResult = argBits[0];
                string yResult = argBits[1];
                string zResult = argBits[2];
                double yaw;
                if (!run.GetDoubleRawOrVal(argBits[3], "setdirvector", out yaw)) { return; }
                double pitch;
                if (!run.GetDoubleRawOrVal(argBits[4], "setdirvector", out pitch)) { return; }
                double yawRad = yaw * (Math.PI / 180.0f);
                double pitchRad = pitch * (Math.PI / 180.0f);

                Vec3F32 dir = GetDirVector(yawRad, pitchRad);
                run.SetString(xResult, dir.X.ToString(doubleToFourDecimalPlaces));
                run.SetString(yResult, dir.Y.ToString(doubleToFourDecimalPlaces));
                run.SetString(zResult, dir.Z.ToString(doubleToFourDecimalPlaces));
            }
            const string doubleToFourDecimalPlaces = "0.#####";

            //copy pasted from DirUtils.cs. why have you abandoned me Unk
            static Vec3F32 GetDirVector(double yaw, double pitch) {
                double x = Math.Sin(yaw) * Math.Cos(pitch);
                double y = -Math.Sin(pitch);
                double z = -Math.Cos(yaw) * Math.Cos(pitch);
                return new Vec3F32((float)x, (float)y, (float)z);
            }
        }

        static bool GetCoords(ScriptRunner run, string actionName, out Vec3S32 coords) {
            string[] stringCoords = run.args.SplitSpaces();
            coords = new Vec3S32();
            if (stringCoords.Length < 3) { run.Error(); run.p.Message("&cNot enough arguments for {0}", actionName); return false; }
            if (!CommandParser.GetCoords(run.p, stringCoords, 0, ref coords)) { run.Error(true); return false; }
            return true;
        }
        static bool GetCoordsFloat(ScriptRunner run, string actionName, out Vec3F32 coords) {
            string[] stringCoords = run.args.SplitSpaces();
            coords = new Vec3F32();
            if (stringCoords.Length < 3) { run.Error(); run.p.Message("&cNot enough arguments for {0}", actionName); return false; }
            if (!CommandParser2.GetCoords(run.p, stringCoords, 0, ref coords)) { run.Error(true); return false; }
            return true;
        }
        static void SendMOTD(ScriptRunner run, string motd) {
            run.p.Send(Packet.Motd(run.p, motd));
            if (run.p.Supports(CpeExt.HackControl)) {
                run.p.Send(Hacks.MakeHackControl(run.p, motd));
            }
        }
    }

    public static class Docs {
        static string[] Intro = new string[] {
            "Welcome to the documentation for /script and /oss.",
            "",
            "Below you will find the following sections:"
        };
        static List<string> DescribeSections(List<Section> sections) {
            List<string> lines = new List<string>();
            foreach (Section s in sections) {
                lines.Add("    " + s.Name + " section");
            }
            lines.Add("");
            lines.Add("");
            return lines;
        }
        public static void PluginLoad() {
            if (!Core.IsNA2) { return; }

            IEnumerable<Type> types = Wizardry.GetDerivedTypesFor(typeof(Section));
            List<Section> sections = new List<Section>();

            foreach (Type t in types) {
                Section section;
                //Log.Msg("{0} becomes {1}", t.Name, t.Name.Substring("Action".Length).ToLowerInvariant());
                try {
                    section = (Section)Activator.CreateInstance(t);
                } catch (MemberAccessException e) {
                    //Catch exception when trying to create abstract instances. Do nothing in this case
                    continue;
                }
                sections.Add(section);
            }


            List<string> docs = new List<string>();
            docs.AddRange(Intro);
            docs.AddRange(DescribeSections(sections));
            foreach (Section section in sections) { docs.AddRange(section.Lines()); }

            Write(docs);
        }
        static void Write(List<string> lines) {
            string folder = Core.NA2WebFileDirectory + "docs/nas/";
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            for (int i = 0; i < lines.Count; i++) {
                if (lines[i].Length == 0) { continue; }
                if (lines[i][0] == '-') {
                    lines[i] = lines[i].Substring(1);
                    lines[i] = "   " + lines[i];
                    continue;
                }
                lines[i] = "// " + lines[i];
            }
            File.WriteAllLines(folder + "documentation.nas", lines);
        }

        public abstract class Section {
            const string BARRIER = "|-------------------------------------------------------------------------------------------------------------------------------------------|";
            public abstract string Name { get; }
            public abstract List<string> Body();
            public List<string> Lines() {
                List<string> lines = new List<string>();
                lines.Add(BARRIER);
                lines.Add(BARRIER);
                lines.Add("    Welcome to the " + Name + " section.");
                lines.Add(BARRIER);
                lines.Add(BARRIER);
                lines.Add("");
                lines.Add("");
                lines.AddRange(Body());
                lines.Add("");
                lines.Add("");
                lines.Add("");
                return lines;
            }
        }
        public class ActionSection : Section {
            public override string Name => "Action";

            public override List<string> Body() {
                List<string> body = new List<string>();


                foreach (var pair in ScriptActions.Dic) {

                    //add the action name to the start of the documentation
                    string[] doc = (string[])pair.Value.documentation.Clone();
                    doc[0] = pair.Value.name + " " + doc[0];

                    body.AddRange(doc);
                    body.Add("");
                }
                return body;
            }
        }
        public class IfSection : Section {
            public override string Name => "If Statement";

            static string[] body = new string[] {
                "if [package] [Action]",
                "    The [Action] will only be performed if [package] has a value of \"true\".",
                "",
                "if [package]|=|[package to compare to] [Action]",
                "    The [Action] will only be performed if [package] has the same value as [package to compare to].",
                "    Note the usage of the pipe symbol | to separate the arguments.",
                "    This comparison is not case sensitive.",
                "",
                "if [package]|[operator]|[number or package] [Action]",
                "    The [Action] will only be performed if the statement is true.",
                "    Valid operators are =, >, >=, <, <=",
                "    For example:",
                "-       if myGemCount|<|bowGemPrice msg Sorry, you don't have enough gems to afford this bow.",
                "-       if myGemCount|>=|2 msg Prospector: Well I'll be; you did manage to find more than one...!",
                "",
                "if item [ITEM_NAME] [Action]",
                "    The [Action] will only be performed if the player has the given item.",
                "",
                "For any of the above, \"if\" can be substituted for \"ifnot\" to reverse the logic.",
                "    For example:",
                "-       ifnot recognized msg Shady dude: I don't know you.",
            };

            public override List<string> Body() {
                return body.ToList();
            }
        }
        public class PresetSection : Section {
            public override string Name => "Preset Packages";

            static string[] body = new string[] {
                "Has a value of \"true\" if the player has cef installed (https://github.com/SpiralP/classicube-cef-loader-plugin)",
                "    cef",
                "Has a value of \"true\" if the player is playing on the web client",
                "    webclient",
                "Has a value of \"true\" if the player is playing on a mobile device",
                "    mobile",
                "The x, y, and z coordinates of the message block that the script is ran from. These will be zero if ran from reply, /input, or hotkeys.",
                "    MBX",
                "    MBY",
                "    MBZ",
                "The x, y, and z block coordinates of the player (integer numbers, like used in /tp).",
                "    PlayerX",
                "    PlayerY",
                "    PlayerZ",
                "The x, y, and z precise coordinates of the player (1 block = 32 precise units. These are used for the command /tp -precise)",
                "    PlayerPX",
                "    PlayerPY",
                "    PlayerPZ",
                "The player's camera yaw (left and right) and camera pitch (up and down) in degrees.",
                "    PlayerYaw",
                "    PlayerPitch",
                "A number used for the delay Action that is automatically scaled based on how many characters the previous msg Action had.",
                "    msgDelay",
                "A number that determines how much scaling is applied to msgDelay. The default is 50, and it resets every time the script is run (you can change this number).",
                "    msgDelayMultiplier",
                "",
                "The coordinates of the message block that the script is ran from. These will be zero if ran from reply, /input, or hotkeys.",
                "    MBCoords",
                "The block coordinates of the player (integer numbers, like used in /tp).",
                "    PlayerCoords",
                "The block coordinates of the player (decimal numbers, like used in /tempbot add)",
                "    PlayerCoordsDecimal",
                "The precise coordinates of the player (1 block = 32 units, like used in /tp -precise)",
                "    PlayerCoordsPrecise",
                "",
                "The arguments that are passed along with the #label when the script is run.",
                "    runArg[number]",
                "The 0th runArg is always the name of the label the script started from.",
                "The following runArgs are optional and will be given the value of the extra arguments you pass along with a label.",
                "    For example: /oss #entryDenied|The_Club|you're_not_cool.",
                "-       #entryDenied",
                "-           msg You can't enter {runArg1} because {runArg2}",
                "-           kill",
                "-           // result: The player is murdered and recieves the message \"You can't enter The Club because you're not cool.\"",
                "-       quit",
                "This is extremely useful if you want to have reuseable macros that perform some set of actions many times but with only slightly different details",
                "(like a name or number difference).",
                "If you use a runArg that has not been specified, it will have no value or count as 0 if used as a number.",
                "    For example, running the above script but with just \"/oss #entryDenied\" would result in \"You can't enter  because  \"",
                "As a final note, you can change runArgs when you specify labels with goto, jump, call, newthread, and reply actions.",
                "    For example:",
                "-       #clubEntrance",
                "-           msg You approach the bouncer...",
                "-           delay 2000",
                "-           ifnot item SUNGLASSES jump #entryDenied|The_Club|you're_not_cool.",
                "-           ",
                "-           msg You're always welcome here, cool cat.",
                "-       quit",
                "The number of milliseconds that have passed since 1970-01-01",
                "    epochMS",
            };

            public override List<string> Body() {
                return body.ToList();
            }
        }
        public class LabelSection : Section {
            public override string Name => "Special Labels";

            static string[] body = new string[] {
                "#input",
                "    If it exists, this label will be called every time the player does the command /input.",
                "    The arguments the player give to /input will be passed to the packages runArg1 and runArg2. runArg1 is always the very first word, and runArg2 contains all of the rest of the words.",
                "    For example, \"/input password king of kings\" will result in:",
                "    runArg1 = password",
                "    runArg2 = king of kings",
                "    This label is also called by hotkeys defined with the definehotkey Action.",
                "    As a side note, as usual with runArgs, underscores cannot be used because they are automatically converted to spaces.",
                "",
                "#accessControl",
                "    This only works for staff scripts.",
                "    Before the player joins a map, the server checks if a script matching the map name exists.",
                "    If that script exists and has the label #accessControl, that label will be ran.",
                "    If the package \"denyAccess\" is set to \"true\" when the script quits, then the player will be denied access to the map.",
                "    It's important to note that this runs /before/ player joins the map, so it cannot act as a spawn MB that initializes temporary packages or whatnot, since joining a map resets packages.",
            };

            public override List<string> Body() {
                return body.ToList();
            }
        }
        public class NotesSection : Section {
            public override string Name => "Important Notes";

            static string[] body = new string[] {
                "All Action names are case sensitive.",
                "All labels are case sensitive.",
                "All packages are NOT case sensitive, even if they appear to use very consistent case rules in the examples and documentation.",
                "",
                "You can unwrap packages into any text of the script except for the names of Actions, \"if\", \"ifnot\", and \"#labels\" that begin a line.",
                "    THIS IS INCREDIBLY POWERFUL:",
                "    You can dynamically change what the script is going to do by using the values of packages to modify Action arguments or names of other packages.",
                "    (programming folks: can you figure out how to do arrays?)",
            };

            public override List<string> Body() {
                return body.ToList();
            }
        }
        public class StaffSection : Section {
            public override string Name => "Staff (non OS)";

            static string[] body = new string[] {
                "For a package to be \"saved\", its name should end with a period. For example, imagine we wanted to save how many eggs were collected in egg2022.",
                "We could do:",
                "-   set eggCount. 20",
                "And this package would persist even if you left the map or logged out.",
                "We can access this package in other scripts by prefixing the package with \"@scriptname_\". For example:",
                "-   show @egg2022_eggCount.",
                "IMPORTANT: The period is *part of the package name* as well as marking that it is permanently saved.",
                "For example, if you are using eggCount. to save the eggs, \"eggCount\" (no period) will not give you the correct egg count,",
                "because it is a different package once the period is removed.",
            };

            public override List<string> Body() {
                return body.ToList();
            }
        }
    }

    public class ScriptLine {
        public int lineNumber;
        public ScriptActions.ScriptAction actionType = null;
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

        private readonly object locker = new Object();
        public void AddActiveScript(ScriptRunner runner) {
            lock (locker) {
                activeScripts.Add(runner);
            }
        }
        public void RemoveActiveScript(ScriptRunner runner) {
            lock (locker) {
                activeScripts.Remove(runner);
            }
        }
        private List<ScriptRunner> activeScripts = new List<ScriptRunner>();

        public bool debugging = false;
        public int debuggingDelay = 0;

        public void ShowAllStrings() {
            if (strings.Count == 0) { p.Message("There are no packages to show"); return; }

            var all = new List<KeyValuePair<string, string>>();
            foreach (var pair in strings) {
                all.Add(new KeyValuePair<string, string>(pair.Key, pair.Value));
            }
            //alphabetical sort
            all.Sort((name1, name2) => string.Compare(name1.Key, name2.Key));

            foreach (var pair in all) {
                p.Message("The value of &b{0} &Sis \"&o{1}&S\".", pair.Key.ToLower(), pair.Value);
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
                p.SendCpeMessage(CmdReplyTwo.GetReplyMessageType(i + 1), "", CmdReplyTwo.replyPriority); //the message type is 1-6 and we iterate 0-5, hence +1
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
        public void OnPlayerSpawning() {

            //p.Message("OnPlayerSpawning {0}", p.level.name);

            lock (locker) {
                for (int i = activeScripts.Count - 1; i >= 0; i--) {
                    activeScripts[i].cancelled = true;
                    activeScripts.Remove(activeScripts[i]);
                }
            }


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
            if (items) { ResetDict(osItems, matcher); }
        }
        void ResetDict<T>(Dictionary<string, T> dict, string matcher) {
            if (matcher.Length == 0) { dict.Clear(); return; }
            List<string> keysToRemove = MatchingKeys(dict, matcher);
            foreach (string key in keysToRemove) {
                //p.Message("removing key {0}", key);
                dict.Remove(key);
            }
        }
        public void ResetSavedStrings(string scriptName, string matcher) {
            string prefix = (prefixedMarker + scriptName + "_").ToUpper();
            string pattern = prefix + "*" + matcher;
            if (!(matcher.Contains("*") || matcher.Contains("?"))) {
                //if there are no special characters, it's a generic "contains" search, so add asterisk at the end
                pattern = pattern + "*";
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
                    file.WriteLine(entry.Key + " " + entry.Value);
                    //p.Message("&ewriting &0{0}&e to {1}", entry.Key+" "+entry.Value, fileName);
                }
            }
        }
        bool DoesDirectoryExist(out string filePath, out string fileName) {
            //delete wrong location of data.txt
            if (File.Exists(savePath + p.name + "/data.txt")) {
                File.Delete(savePath + p.name + "/data.txt");
                //p.Message("Script debug text: deleted wrong data.txt &[This message means it is working as intended and you will only see it once.");
            }

            filePath = savePath + p.name + "/data/";
            fileName = filePath + "data.txt";
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
            return (prefixedMarker + scriptName + "_" + stringName);
        }

        public string GetString(string stringName, bool isOS, string scriptName) {
            bool saved;
            stringName = ValidateStringName(stringName, isOS, scriptName, out saved).ToUpper();

            var dict = saved ? savedStrings : strings;
            string value = "";
            string dicValue;
            if (dict.TryGetValue(stringName, out dicValue)) { value = dicValue; }
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


        public bool HasItem(string itemName, bool isOS) { return isOS ? OsHasItem(itemName) : ModHasItem(itemName); }
        public void GiveItem(string itemName, bool isOS) { if (isOS) { OsGiveItem(itemName); } else { ModGiveItem(itemName); } }
        public void TakeItem(string itemName, bool isOS) { if (isOS) { OsTakeItem(itemName); } else { ModTakeItem(itemName); } }

        private bool ModHasItem(string itemName) {
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
            UseAsMB(Core.stuffCmd, "obsoleteArg" + " get " + itemName.ToUpper());
        }
        private void ModTakeItem(string itemName) {
            UseAsMB(Core.stuffCmd, "obsoleteArg" + " take " + itemName.ToUpper());
        }

        //#region OS
        public void DisplayItems() {
            if (osItems.Count == 0) { p.Message("&cYou have no items! &SDid you mean to use &b/stuff&S?"); return; }

            string[] allItems = new string[osItems.Count];
            int i = 0;
            foreach (KeyValuePair<string, bool> entry in osItems) {
                allItems[i] = "&a" + entry.Key.Replace('_', ' ');
                i++;
            }
            p.Message("&eYour items:");
            p.Message("&f> " + String.Join(" &8• ", allItems));
            p.Message("Notably, items are different from &T/stuff&S because they will disappear if you leave this map.");
        }

        public bool OsHasItem(string itemName) { return osItems.ContainsKey(itemName.ToUpper()); }
        public void OsGiveItem(string itemName) {
            itemName = itemName.ToUpper();
            if (osItems.ContainsKey(itemName)) { return; } //they already have this item
            osItems[itemName] = true;
            p.Message("You found an item: &a{0}&S!", itemName.Replace('_', ' '));
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
            return "/" + cmd + " " + content + "◙";
        }
        public static byte GetModifiers(string modifierArgs) {
            byte ctrlFlag = (byte)(modifierArgs.Contains("ctrl") ? 1 : 0);
            byte shiftFlag = (byte)(modifierArgs.Contains("shift") ? 2 : 0);
            byte altFlag = (byte)(modifierArgs.Contains("alt") ? 4 : 0);
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
            value = 0;

            if (!CommandParser.GetReal(p, arg, axis, ref value)) return false;
            if (relative) value += cur;
            return true;
        }
    }


}