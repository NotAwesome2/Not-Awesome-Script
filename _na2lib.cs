using System;
using System.IO;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Modules.Awards;

namespace NA2 {
    
    public static class NameUtils {
        static char[] underscore = new char[] {'_'}; //char array for compatibility with old.net with no Char overload on Trim and Split
        
        public static string MakeNatural(string name) {
            name = Colors.Strip(name);
            string[] nameWords = name.SplitSpaces(2);
            if (nameWords.Length > 1) {
                name = nameWords[1]; //a split in the name means they have a flair on. remove the flair.
            }
            
            string nameNoUnder = name.TrimStart(underscore);
            string[] nameParts = nameNoUnder.Split(underscore);
            string naturalNameEnd = nameParts[0].TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            string naturalNameEndStart = naturalNameEnd.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            
            if (naturalNameEndStart == "") { naturalNameEndStart = nameParts[0]; }
            return naturalNameEndStart;
        }
        
    }
    
    public static class AwardUtils {
        public static bool GiveTo(Player p, string awardName) {
            Award award = AwardsList.FindExact(awardName);
            if (award == null) { p.Message("&WCannot give invalid award \"{0}\"", awardName); return false; }
            
            if (!GiveToOffline(p.name, award.Name)) { return false; }
            
            Chat.MessageGlobal("{0} &Swas awarded: &b{1}", p.ColoredName, award.Name);
            return true;
        }
        public static bool GiveToOffline(string playerName, string awardName) {
            Award award = AwardsList.FindExact(awardName);
            if (award == null) { throw new System.ArgumentException("Cannot give invalid award "+awardName+"."); }
            
            if (PlayerAwards.Give(playerName, award.Name)) {
                PlayerAwards.Save();
                return true;
            }
            return false;
        }
        
        public static bool HasAward(string playerName, string awardName) {
            Award award = AwardsList.FindExact(awardName);
            if (award == null) { return false; } //award does not exist
            List<string> playerAwards = PlayerAwards.Get(playerName);
            if (playerAwards == null || playerAwards.Count == 0) { return false; } //player has no awards
            return playerAwards.CaselessContains(award.Name);
        }
    }
    
    public static class NoobMain {
        
        const string fileName = "finishednoobmain";
        const string filePath = "text/" + fileName + ".txt";
        static readonly object locker = new object();
        
        public static bool HasFinished(string playerName) {
            
            string[] lines;
            lock (locker) {
                 lines = File.ReadAllLines(filePath);
            }
            
            for (int i = 0; i < lines.Length; i++) {
                if (playerName == lines[i]) { return true; }
            }
            return false;
        }
        
        public static void MarkAsFinished(Player p) {
            if (HasFinished(p.name)) { return; }
            lock (locker) {
                File.AppendAllText(filePath, p.name + Environment.NewLine);
            }
            p.Message("&oThank you for completing the tutorial!");
        }
        
    }
}
