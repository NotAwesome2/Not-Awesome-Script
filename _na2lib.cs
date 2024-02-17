//reference System.Core.dll

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Modules.Awards;


namespace NA2 {
    
    internal sealed class _NA2Lib : Plugin {
        
        public override string name { get { return "_na2lib"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string creator { get { return "Goodly"; } }
        
        public override void Load(bool startup) {
            OnConfigUpdatedEvent.Register(OnConfigUpdated, Priority.Low);
            Naward.Init();
        }
        public override void Unload(bool shutdown) {
            OnConfigUpdatedEvent.Unregister(OnConfigUpdated);
        }
        
        public static void OnConfigUpdated() {
            Naward.Init();
        }
        
    }
    
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
    
    public class Naward {
        
        //returns false if already given before
        public static bool GiveTo(Player p, string awardName) {
            Naward naward = FindExact(awardName);
            if (naward == null) { p.Message("&WCannot give inexistent award \"{0}\"", awardName); return false; }
            
            if (!GiveToOffline(p.name, naward.award.Name)) { return false; }
            
            Chat.MessageGlobal(String.Format("{0} &Swas awarded: &6{1} - {2}", p.ColoredName, naward.category, naward.name));
            
            
            //thread safe without C# 6.0 null propagation
            var cachedEvent = onAwardGiven;
            if (cachedEvent != null) {
                cachedEvent.Invoke(p, naward);
            }
            
            return true;
        }
        //returns false if already given before
        public static bool GiveToOffline(string playerName, string awardName) {
            Naward naward = FindExact(awardName);
            if (naward == null) { throw new System.ArgumentException("Cannot give inexistent award "+awardName+"."); }

            return naward.GiveTo(playerName);
        }
        public static bool HasAward(string playerName, string awardName) {
            Award award = AwardsList.FindExact(awardName);
            if (award == null) { return false; } //award does not exist
            List<string> playerAwards = PlayerAwards.Get(playerName);
            if (playerAwards == null || playerAwards.Count == 0) { return false; } //player has no awards
            return playerAwards.CaselessContains(award.Name);
        }
        public static bool HasAll(string playerName) {
            List<string> playerAwards = PlayerAwards.Get(playerName);
            int completed, total;
            GetCompleted(playerAwards, out completed, out total);
            if (total == 0) { return false; }
            return completed == total;
        }
        public static void GetCompleted(List<string> playerAwards, out int completed, out int total) {
            total = Nawards.Count;
            completed = 0;
            foreach (Naward a in Nawards) {
                if (a.IsInList(playerAwards)) { completed++; }
            }
        }
        
    
        public static List<string> categories = new List<string>();
        public static List<Naward> Nawards = new List<Naward>();
        
        internal static void Init() {
            categories.Clear();
            Nawards.Clear();
            foreach (Award a in AwardsList.Awards) {
                Naward naward = new Naward(a);
                Nawards.Add(naward);
                
                if (categories.CaselessContains(naward.category)) { continue; } //duplicate category, dont include
                categories.Add(naward.category);
            }
            
            foreach (string cat in categories) {
                Logger.Log(LogType.SystemActivity, "Category: {0}", cat);
            }
        }
        
        public static Naward FindExact(string awardName) {
            foreach (Naward naward in Nawards) {
                if (naward.award.Name.CaselessEq(awardName)) return naward;
            }
            return null;
        }
        public static string FindCategory(Player p, string name) {
            int matches;
            string category = Matcher.Find(p, name, out matches, categories,
                                       null, c => c, "award categories");
            return category == null ? null : category;
        }
        public static List<Naward> FromCategory(string category) {
            return Nawards.Where((naward) => { return naward.category.CaselessEq(category); }).ToList();
        }
        public static string Summarise(string category, List<string> playerAwards, out int completed, out int total) {
            List<Naward> awards = FromCategory(category);
            total = awards.Count;
            completed = 0;
            foreach (Naward a in awards) {
                if (a.IsInList(playerAwards)) { completed++; }
            }
            return completed+"/"+total;
        }
        
        public delegate void OnAwardGiven(Player p, Naward naward);
        public static event OnAwardGiven onAwardGiven;
        
        
        
        // non static area ---------------------------------
        
        Award award;
        public readonly string category;
        public readonly string name;
        public readonly string Description;
        public readonly string hint = null;
        public bool secret { get { return award.Description.CaselessStarts("secret -"); } }
        
        
        public Naward(Award award) {
            if (award == null) { throw new System.ArgumentException("Award passed to Naward constructor must be non-null"); }
            this.award = award;
            Description = award.Description;
            
            int i = Description.IndexOf('|');
            if (!(i == -1 || Description.Length == i)) { //desc has hint in it
                //Desc|Hint
                Description = award.Description.Substring(0, i);
                hint = award.Description.Substring(i+1);
            }
            
            i = award.Name.IndexOf('|');
            if (i == -1 || award.Name.Length == i) {
                category = "Server"; name = award.Name;
            } else {
                //Cat|Name
                category = award.Name.Substring(0, i);
                name = award.Name.Substring(i+1);
            }
            
            //Logger.Log(LogType.SystemActivity, "Loaded award: {0} {1} {2}", category, name, Description);
            //if (hint != null) Logger.Log(LogType.SystemActivity, "Award: {0} has hint {1}", name, hint);
        }
        
        public bool IsInList(List<string> playerAwards) {
            return playerAwards.CaselessContains(award.Name);
        }
        //returns false if already given before
        public bool GiveTo(string playerName) {
            bool givenFirstTime;
            if (PlayerAwards.Give(playerName, award.Name)) {
                PlayerAwards.Save();
                return true;
            }
            return false;
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
