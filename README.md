# PF-vMenu
The vMenu for Project Fairness and other affiliated servers. All actions are subject to change.

# Description
The original vMenu was lacking on options and new content. I made this version as a way for people to still get the latest updates, while of course, adding new things so it feels more like the vMenu on other servers (such as RSM, Impulse99, etc) while also improving upon the original.

# DISCLAIMER 
- If you are going to use this version of PF-vMenu, make sure you set "vmenu_individual_server_id" in your permission.cfg file. Otherwise, vMenu will fail to start. This value can be any ASCII character and no spaces (ie. the name of the server, random string of letters and numbers). 
- Make sure you also update your "vmenu_menu_toggle_key" and "vmenu_noclip_toggle_key". If you're an existing vMenu/PF-vMenu user, these have been changed to use [Keyboard Controls](https://docs.fivem.net/docs/game-references/input-mapper-parameter-ids/keyboard/)
- This release is also based on the v3.6.0 release of vMenu. We had to do a whole bunch of porting from our v3.5.1 pre-release into this.

# What's New
- Chameleon Color by Wildbrick142 - [Download Chameleon Colors](https://discord.gg/D7cVc8TzPN) [here](https://discord.com/channels/948321761429184584/1055750916080422922/1089373135331151872), [Source](https://www.gta5-mods.com/misc/chameleon-paint-add-on)
- Content Updated up to v2802 & v2944
- Mosh_Notify embedded Support for vMenu's NoClip - [Download Mosh_Notify here](https://forum.cfx.re/t/release-free-mosh-notify-fivem-custom-notifications-with-loadbar/2614951)
- Los Santos Tuners goodies such as Vehicle Stance and Drift Tires (it's useless since we have vStancer and Handling Editor, but hey, it works)
- Moved Teleport Options to the front (Now, you no longer have to go to Misc Settings to teleport to your favorite place!)
- PMA-Voice Support
- (WIP) Local Time & Weather Options
- (WIP) Car Brands on Addon Vehicles menu
- Multi-Language Support [(Looking for contributors!)](https://github.com/ProjectFairnessLabs/GroupTranslationDB)
- Labels for Vehicle Extras
- Plugins menu created, added Wheelie Manager and Easy Drift Plus as default plugins
- Enhanced Camera Menu added (thank you Shrimpy)
- Enhanced version of the add-on vehicle spawner, the old version was deprecated. No need to add extra categories!
- Default vehicle blacklist was added and located within addons.json (for those RP server players... 😉)
- Allowed all non-bike vehicles to use Open-Wheel tires (now make a ridiculous car!)
- Noclip lets you go up and down by looking with your camera! Look up to go up!

# What's been changed
- Changed "vmenu_enable_animals_spawn_menu" to a new ace permission "vMenu.PlayerAppearance.AnimalPeds"
- Changed "vmenu_disable_spawning_as_default_character" to a new ace permission "vMenu.PlayerAppearance.SpawnAsDefault"
- Changed vMenu toggle and noclip to RegisterKeyMapping so it can now be changed within key binds in settings 

--------
# Translations
If you want to help out provide translations to vMenu, you can [click here](https://github.com/ProjectFairnessLabs/GroupTranslationDB) and do a pull request!

--------
# Developers
- Ribbitpoison
- RickyB505
- TristenCommunity
- 1Mosheba
- QuadrupleTurbo
- Starman0620
- UnsayingCheetah
- DeckardCain000
- Shrimpy
- L'kid & Proky0
- AlexR32
- Freedy69
- MichaelCoding25
- ChatGGS/gg781
- AvarianKnight
- AvaNOX
- Wildbrick142
- Lincoln HUX


# Feature Suggestions
- TayMcKenzieNZ
- zwrks

# Original Author
- Vespura
--------
# Discord
Please join [our Discord server](https://discord.gg/D7cVc8TzPN) if you want support in regards to this menu.

--------
# Download & Installation & Permissions

## Requirements
- [Server must be v2372 or above, click here on how to enforce serverbuild](https://docs.fivem.net/docs/server-manual/server-commands/#sv_enforcegamebuild-build)
- [mosh_notify](https://forum.cfx.re/t/release-free-mosh-notify-fivem-custom-notifications-with-loadbar/2614951) (Optional download)
- [Chameleon Colors](https://cdn.discordapp.com/attachments/1099903046701559828/1102708906234478712/pzn_chameleoncolor.7z)
- [PMA-Voice](https://github.com/AvarianKnight/pma-voice/releases)

## Download
Download this vMenu [here](https://github.com/ProjectFairnessLabs/PF-vMenu/releases)

--------

## Compiling of the PF-vMenu
Compiling the vMenu is quite easy and straight-forward, if you have Visual Studio. By grabbing the NuGet packages for CitizenFX.Core.Client and CitizenFX.Core.Server as well as the NuGet package for MenuAPI, all you have to do is compile and that's it. All support of the Project Fairness vMenu will never be receiving troubleshoot support by Vespura, so do not bother asking for support to him. This is a fork of his project. Please pull a Issue request if you have any problems with this vMenu.

--------

## Installation
Please follow the instructions over at the [PF-vMenu wiki]([https://docs.vespura.com/vmenu/installation](https://github.com/ProjectFairnessLabs/PF-vMenu/wiki/Installation)!

--------

## Support
If you really enjoy this menu, please consider supporting me on [Ko-Fi](https://ko-fi.com/projectfairnesslabs)! I'm very glad I can bring something to the table after two years of working on this menu exclusively for my server but wanted to share it with the whole world. :)

If you like Vespura's work, please consider supporting him on [**Patreon**](https://www.patreon.com/vespura). He's put a _lot_ of my time and hard work into his vMenu and other projects.

--------

## Pull Requests
We are glad at accepting Pull Requests to add more features overtime to our vMenu, meaning we can add more features that can be similar to other vMenu clients.

--------

--------

# vMenu
vMenu is server sided menu for FiveM servers, including full\* permission support.

\*(Some features do not have permissions support as they are either harmless or it'd just be silly to deny them. However, they will be disabled if you deny access to the submenu that they are a part of (eg: unlimited stamina in Player Options will be disabled if you deny `vMenu.PlayerOptions.Menu`.))

The intention of this vMenu is to create the ultimate vMenu from Vespura's resources, while also achieving the same features seen on other servers such as Rockstar Mischief and Impulse99, which have a modified vMenu, while adding and integrating new features inside of said vMenu.
--------

## Permissions 
Click [here](https://docs.vespura.com/vmenu/permissions-ref) for permissions information.

## Configuration
Click [here](https://docs.vespura.com/vmenu/configuration) for configuration options information.


--------


## MenuAPI
Starting from vMenu v2.1.0, vMenu will be using [MenuAPI (MAPI)](https://github.com/TomGrobbe/MenuAPI), a custom menu API designed specifically for vMenu by me.

vMenu v2.0.0 and earlier was [using a modified version of NativeUI](https://github.com/TomGrobbe/NativeUI), originally by [Guad](https://github.com/Guad/NativeUI), but converted to FiveM by the CitizenFX Collectives and myself (updated/refactored).


--------

## License
**For an updated license, check the license.md file. That file will always overrule anything mentioned in the readme.md**

Tom Grobbe - https://www.vespura.com/

Copyright © 2017-2022

You can use and edit this code to your liking as long as you don't ever claim it to be your own code and always provide proper credit. 
You're **not** allowed to sell vMenu or any code you take from it.
If you want to release your own version of vMenu, you have to link the original GitHub repo, or release it via a Forked repo.
