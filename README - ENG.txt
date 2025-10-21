[h1]English Guide[/h1]
[quote]
[i]KillFeed[/i] is a mod that adds real-time kill feed display functionality.
When kill events occur in the game, the mod displays kill information in the top-right corner of the screen
[/quote]

[h2]Configuration File Location[/h2]
ModConfig is supported, it allows you to change mod settings in-game  
You can change mod settings in game's settings menu if ModConfig is installed
[img]https://images.steamusercontent.com/ugc/15131859468956989520/312BF0894E2D05D3636E6F038F2B45886FE775F0/?imw=5000&imh=5000&ima=fit&impolicy=Letterbox&imcolor=%23000000&letterbox=false[/img]
The config file is located in the game's [i]StreamingAssets[/i] folder
KillFeed will try loading this config if ModConfig not found
[quote]
..\Escape from Duckov\Duckov_Data\StreamingAssets\KillFeedModConfig.txt
[/quote]

[h2]Basic Settings[/h2]

[i]fontSize[/i]: Kill feed text font size

[i]shouldDisplayNonMainPlayerKill[/i]: Whether to display non-player kill records (true=show all kills, false=only show player kills)

[i]maxKillFeedRecordsNum[/i]: Maximum number of kill records displayed simultaneously

[i]weaponIconSize[/i]: Weapon icon size

[i]weaponIconSpacing[/i]: Spacing between weapon icon and text

[h2]Visual Effects Configuration[/h2]

[i]fadeInTime[/i]: Fade-in duration (seconds)

[i]fadeOutTime[/i]: Fade-out duration (seconds)

[i]displayTime[/i]: Kill record display time (seconds), records older than this will start fading out

[i]slideInTime[/i]: Time needed for kill records to slide in from the right (seconds)

[i]recordsVerticalSpacing[/i]: Vertical spacing between each kill record

[h2]Position Adjustment[/h2]

[i]rightMarginPercent[/i]: Margin Percentage from the right edge of the screen

[i]topMarginPercent[/i]: Margin Percentage from the top edge of the screen

[h2]Misc[/h2]

[i]myName[/i]: Change this to customize your name.

[h2]Tips[/h2]
[list]
[*]Config file is automatically created on first mod run
[*]Restart game after modifying configuration
[*]All values can be adjusted according to personal preference
[/list]
