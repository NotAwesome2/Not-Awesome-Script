# Not-Awesome-Script
Scripting plugin made for Not Awesome 2.

## How to load this plugin on your server

Not Awesome Script has a few extra steps compared to loading other plugins.

1. This plugin depends on [ExtraLevelProps](https://github.com/NotAwesome2/Plugins#_extralevelpropscs). You must load _extralevelprops.cs plugin before you load this ccs.cs plugin.
2. This plugin depends on __item.cs and _na2lib.cs. You must load both of these plugins before you can load this ccs.cs plugin.
3. Finally, you can load ccs itself:
4. Place ccs.cs file in *plugins* folder.
5. Use `/pcompile ccs`
6. Use `/pload ccs`

## It failed to compile/there were errors!

This plugin is usually more up-to-date than the latest MCGalaxy release. If you get an error, you probably need to download latest build from [here](https://mcgala.xyz/nightlies) and update your server using it.

To update your server from latest.zip:
1. Shut down your server if it is running
2. Extract the contents of the latest.zip file.
3. Copy MCGalaxy.exe and MCGalaxy.dll from the extracted folder
4. Paste MCGalaxy.exe and MCGalaxy.dll into your server folder, replacing the old files.

## General usage

Script files (.nas) should be placed in "scripts" folder, and "os" scripts (which can be automatically uploaded by players with /osus) are placed in "scripts/os" folder.

[Documentation for writing .nas files can be found here.](https://notawesome.cc/docs/nas/getting-started.txt)

## Player data

Items are stored as one file per item in "extra/inventory/playername+" folder, and other script data is stored in "extra/inventory/playername+/data/data.txt".

You can add descriptions to items (/stuff look) by adding a .txt file that matches the item name in "text/itemDesc" folder. For instance, the contents of PRETTY_GEM.txt will be displayed when the player does /stuff look pretty gem.

## Contributing

If you decide to add features to this plugin, please contribute those changes here (pull request), so that everyone can continue to use an ever improving main version, instead of being stuck choosing between various disperate forks that become incompatible with each other.
