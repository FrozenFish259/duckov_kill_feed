[h1]中文说明[/h1]
[quote]
[i]KillFeed[/i]是一个为游戏添加实时击杀信息显示功能的模组。
当游戏中发生击杀事件时，模组会在屏幕右上角显示击杀者和被击杀者信息以及击杀所用的武器图标。
[/quote]

[h2]配置文件位置[/h2]
本mod已支持ModConfig  
订阅ModConfig模组后, 可以在游戏内通过设置打开Mod设置菜单, 非常方便
[img]https://images.steamusercontent.com/ugc/15131859468956989520/312BF0894E2D05D3636E6F038F2B45886FE775F0/?imw=5000&imh=5000&ima=fit&impolicy=Letterbox&imcolor=%23000000&letterbox=false[/img]

如果未检测到ModConfig模组, KillFeed会尝试从下面路径读取配置
[quote]
..\Escape from Duckov\Duckov_Data\StreamingAssets\KillFeedModConfig.txt
[/quote]

[h2]基本设置[/h2]

[i]fontSize[/i]: 击杀信息字体大小

[i]shouldDisplayNonMainPlayerKill[/i]: 是否显示非玩家击杀记录（true=显示所有击杀，false=只显示玩家击杀）

[i]maxKillFeedRecordsNum[/i]: 同时显示的最大击杀记录数量

[i]weaponIconSize[/i]: 武器图标大小

[i]weaponIconSpacing[/i]: 武器图标和文字间距

[h2]视觉效果配置[/h2]

[i]fadeInTime[/i]: 淡入时间（秒）

[i]fadeOutTime[/i]: 淡出时间（秒）

[i]displayTime[/i]: 击杀记录显示时间（秒），超过此时间的记录将开始淡出消失

[i]slideInTime[/i]: 击杀记录从右侧滑入需要的时间（秒）

[i]recordsVerticalSpacing[/i]: 每条击杀记录之间的垂直间距

[h2]位置调整[/h2]

[i]rightMarginPercent[/i]: 击杀记录显示区域距离屏幕右边的百分比

[i]topMarginPercent[/i]: 击杀记录显示区域距离屏幕顶部的百分比

[h2]杂项[/h2]

[i]myName[/i]: 修改击杀时主玩家的显示名称, 默认是"我自己"

[h2]使用提示[/h2]
[list]
[*]首次运行mod会自动创建配置文件
[*]修改配置后需要保存文件后重启游戏生效
[*]所有数值都可以根据个人喜好调整
[/list]