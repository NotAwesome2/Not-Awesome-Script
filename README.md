# Not-Awesome-Script
Scripting plugin made for Not Awesome 2.


**Before the plugin can be loaded,** The included .dll files must be placed next to MCGalaxy.exe, and the server must be restarted. Before compiling, **you should change the password** "CHANGETHIS" otherwise anyone can give themselves any item if they know it. The password must be kept all caps (uppercase) to work properly.

Script files (.nas) should be placed in "scripts" folder, and "os" scripts (which can be automatically uploaded by players with /osus) are placed in "scripts/os" folder.

[Documentation](https://dl.dropboxusercontent.com/s/tp9tr21k0dr2qpq/ScriptGuide2.txt)


Items are stored as one file per item in "extra/inventory/playername+" folder, and other script data is stored in "extra/inventory/playername+/data/data.txt".

You can add descriptions to items (/stuff look) by adding a .txt file that matches the item name in "text/itemDesc" folder. For instance, the contents of PRETTY_GEM.txt will be displayed when the player does /stuff look pretty gem.

## Contributing

Originally, this plugin was going to stay private, as it is one of the biggest things that makes Not Awesome 2 unique. However, ultimately, it will probably do more good to the whole community if any server can create its own adventures and complex behavior without having to write complicated plugins for each scenario.

That being said, it would be very appreciated if you decide to add features to this plugin, that you contribute those features here (pull request), so that everyone can continue to use an ever improving main version, instead of being stuck choosing between various disperate forks that quickly become incompatible with each other.