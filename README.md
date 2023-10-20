# Not-Awesome-Script
Scripting plugin made for Not Awesome 2.

## How to load this plugin on your server

Not Awesome Script has a few extra steps compared to loading other plugins.

1. This plugin depends on [ExtraLevelProps](https://github.com/NotAwesome2/Plugins#_extralevelpropscs). You must load _extralevelprops.cs plugin before you load this ccs.cs plugin.
2. This plugin depends on __item.cs and _na2lib.cs. You must load both of these plugins before you can load this ccs.cs plugin.
3. **Change the password called "CHANGETHIS" within ccs.cs.** The password must be all UPPERCASE. Knowing this password allows anyone to give themselves any item.
4. Finally, you can load ccs itself:
5. Place ccs.cs file in *plugins* folder.
6. Use `/pcompile ccs`
7. Use `/pload ccs`

## General usage

Script files (.nas) should be placed in "scripts" folder, and "os" scripts (which can be automatically uploaded by players with /osus) are placed in "scripts/os" folder.

[Documentation for writing .nas files can be found here.](https://dl.dropboxusercontent.com/s/tp9tr21k0dr2qpq/ScriptGuide2.txt)

## Player data

Items are stored as one file per item in "extra/inventory/playername+" folder, and other script data is stored in "extra/inventory/playername+/data/data.txt".

You can add descriptions to items (/stuff look) by adding a .txt file that matches the item name in "text/itemDesc" folder. For instance, the contents of PRETTY_GEM.txt will be displayed when the player does /stuff look pretty gem.

## Contributing

If you decide to add features to this plugin, please contribute those changes here (pull request), so that everyone can continue to use an ever improving main version, instead of being stuck choosing between various disperate forks that become incompatible with each other.
