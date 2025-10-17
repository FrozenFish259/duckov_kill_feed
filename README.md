# 中文说明
> `KillFeed`是一个为游戏添加实时击杀信息显示功能的模组。  
> 当游戏中发生击杀事件时，模组会在屏幕右上角显示击杀者和被击杀者信息。  

### 配置文件位置
>..\Escape from Duckov\Duckov_Data\StreamingAssets\KillFeedModConfig.txt
### 基本设置
`fontSize`: 击杀信息字体大小

`shouldDisplayNonMainPlayerKill`: 是否显示非玩家击杀记录（true=显示所有击杀，false=只显示玩家击杀）

`maxKillFeedRecordsNum`: 同时显示的最大击杀记录数量

### 视觉效果配置
`fadeInTime`: 淡入时间（秒）

`fadeOutTime`: 淡出时间（秒）

`displayTime`: 击杀记录显示时间（秒），超过此时间的记录将开始淡出消失

`slideInTime`: 击杀记录从右侧滑入需要的时间（秒）

`recordsVerticalSpacing`: 每条击杀记录之间的垂直间距

### 位置调整
`rightMarginPercent`: 击杀记录显示区域距离屏幕右边的百分比

`topMarginPercent`: 击杀记录显示区域距离屏幕顶部的百分比

## 使用提示
* 首次运行mod会自动创建配置文件
* 修改配置后需要保存文件后重启游戏生效
* 所有数值都可以根据个人喜好调整

***
# English Guide
> `KillFeed` is a mod that adds real-time kill feed display functionality.   
>When kill events occur in the game, the mod displays kill information in the top-right corner of the screen

## Configuration File Location
The config file is located in the game's `StreamingAssets` folder:

>..\Escape from Duckov\Duckov_Data\StreamingAssets\KillFeedModConfig.txt

## Basic Settings
`fontSize`: Kill feed text font size

`shouldDisplayNonMainPlayerKill`: Whether to display non-player kill records (true=show all kills, false=only show player kills)

`maxKillFeedRecordsNum`: Maximum number of kill records displayed simultaneously

## Visual Effects Configuration
`fadeInTime`: Fade-in duration (seconds)

`fadeOutTime`: Fade-out duration (seconds)

`displayTime`: Kill record display time (seconds), records older than this will start fading out

`slideInTime`: Time needed for kill records to slide in from the right (seconds)

`recordsVerticalSpacing`: Vertical spacing between each kill record

## Position Adjustment
`rightMarginPercent`: Margin Percentage from the right edge of the screen

`topMarginPercent`: Margin Percentage from the top edge of the screen

## Tips
* Config file is automatically created on first mod run
* Restart game after modifying configuration
* All values can be adjusted according to personal preference