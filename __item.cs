//reference System.Core.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCGalaxy;

namespace NA2 {
    
    public class Item {
        
        static bool ValidItemName(string name) {
            foreach (char c in name) {
                if (!(
                (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_' ||
                c == '.' ||
                c == '-'
                )) { return false; }
            }
            return true;
        }
        
        static string ItemDirectory(string playerName) {
            string directory = "text/inventory/" + playerName + "/";
            if (!Directory.Exists(directory)) { Directory.CreateDirectory(directory); }
            return directory;
        }
        
        public static Item[] GetItemsOwnedBy(string playerName) {
            DirectoryInfo info = new DirectoryInfo(ItemDirectory(playerName));
            
            FileInfo[] allItemFiles = info.GetFiles().OrderBy(f => f.CreationTime).ToArray();
            Item[] allItems = new Item[allItemFiles.Length];
            
            for (int i = 0; i < allItems.Length; i++) {
                allItems[i] = new Item(allItemFiles[i].Name);
            }
            return allItems;
        }
        
        public static Item MakeInstance(Player p, string itemName) {
            try {
                return new Item(itemName);
            } catch (System.ArgumentException e) {
                p.Message("&W{0}", e.Message);
                return null;
            }
        }
        
        
        public readonly string name;
        public readonly string displayName;
        public readonly bool isVar;
        
        public string[] ItemDesc  { get { return GetDesc(); } }
        public string Color       { get { return (ItemDesc.Length == 0) ? "&b" : "&6"; } }
        public string ColoredName { get { return Color+displayName; } }
        
        public Item(string givenName) {
            if (String.IsNullOrWhiteSpace(givenName)) {
                throw new System.ArgumentException("Item name cannot be null or blank");
            }
            name = givenName.ToUpper().Replace(' ', '_');
            if (!ValidItemName(name)) {
                throw new System.ArgumentException("Item name \""+name+"\" may only use A-Z or _ . characters.");
            }
            
            displayName = name.Replace('_', ' ');
            isVar = name.StartsWith("VAR.");
        }
        
        
        public bool OwnedBy(string playerName) {
            return File.Exists(ItemDirectory(playerName) + name);
        }
        
        public void GiveTo(Player p) {
            if (OwnedBy(p.name)) { return; }
            
            GiveToOffline(p.name);
            if (isVar) { return; }
            
            p.Message("You found {0} {1}%S!", AOrAn(), ColoredName);
            p.Message("Check what stuff you have with &b/stuff%S.");
        }
        
        public void GiveToOffline(string playerName) {
            // Used in contest plugin
            if (OwnedBy(playerName)) { return; }
            File.WriteAllText(ItemDirectory(playerName) + name + "", "");
        }
        
        public bool TakeFrom(Player p) {
            if (!TakeFromOffline(p.name)) { return false; } //only send message if item was actually taken
            if (!isVar) { p.Message("{0}%S was removed from your stuff.", ColoredName); }
            return true;
        }
        
        public bool TakeFromOffline(string playerName) {
            if (name.Contains("/") || name.Contains("\\")) { return false; } //nice try
            if (!OwnedBy(playerName)) { return false; }
            File.Delete(ItemDirectory(playerName) + name);
            return true;
        }
        
        public string AOrAn() {
            if (name.StartsWith("A") || name.StartsWith("E") || name.StartsWith("I") || name.StartsWith("O") || name.StartsWith("U")) { return "an"; }
            return "a";
        }
        
        string[] GetDesc() {
            const string descDirectory = "text/itemDesc/";
            if (!File.Exists(descDirectory + name + ".txt")) { return new string[] { }; }
            
            string[] desc = File.ReadAllLines(descDirectory + name + ".txt");                
            return desc;
        }
        
    }
}
