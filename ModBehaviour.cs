using Duckov.Modding;
using Duckov.Options;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using Sprite = UnityEngine.Sprite;

namespace KillFeed
{
    [System.Serializable]
    public class KillFeedConfig
    {

        //文字大小
        public float fontSize = 30f;

        //是否应该显示非玩家击杀记录
        public bool shouldDisplayNonMainPlayerKill = true;
        //最大的同时显示的击杀记录
        public int maxKillFeedRecordsNum = 6;

        ////////////视觉效果配置////////////
        //淡入时间
        public float fadeInTime = 0.4f;
        //淡出时间
        public float fadeOutTime = 0.7f;
        //records that exsist longer than this time will fade out and disapper
        public float displayTime = 5f;
        //击杀记录从右侧滑入需要的时间
        public float slideInTime = 0.4f;
        //每条击杀记录的垂直间距
        public float recordsVerticalSpacing = 2f;
        ////////////////////////////////////

        ////////////击杀记录位置调整: 0.0 - 1.0////////////
        //击杀记录显示区域右margin百分比
        public float rightMarginPercent = 0.05f;
        //击杀记录显示区域顶margin百分比
        public float topMarginPercent = 0.15f;
        ///////////////////////////////////////////////////

        //自定义显示名称
        public string myName = "";

        //强制更新配置文件token
        public string doNotEditThisTokenThisIsATokenThatForForceUpdatingConfigs = "";

        //武器图标配置
        public float weaponIconSize = 80f; // 武器图标大小
        public float weaponIconSpacing = 1f;     // 很小的间距，让元素靠得很近
    }

    enum ModConfigType
    {
        DropDownList = 0,
        BoolDropDownList = 1,
        InputWithSlider = 2,
    }

    class ModConfigTask
    {
        public string mod_name = ModBehaviour.MOD_NAME;

        public ModConfigType mod_config_type;
        public string key;
        public string description;
        public Type type;
        public object defaultValue;
        public Vector2? range;
        public SortedDictionary<string, object>? dropdownListDict;

        public static ModConfigTask DropDownListTask(string key, string description, Type type, object defaultValue, SortedDictionary<string, object> dropdownListDict)
        {
            ModConfigTask modConfigTask = new ModConfigTask();
            modConfigTask.mod_config_type = ModConfigType.DropDownList;

            modConfigTask.key = $"{modConfigTask.mod_name}_{key}";
            modConfigTask.description = description;
            modConfigTask.type = type;
            modConfigTask.defaultValue = defaultValue;
            modConfigTask.dropdownListDict = dropdownListDict;

            return modConfigTask;
        }
        public static ModConfigTask BoolDropDownListTask(string key, string description, bool defaultValue)
        {
            ModConfigTask modConfigTask = new ModConfigTask();
            modConfigTask.mod_config_type = ModConfigType.BoolDropDownList;

            modConfigTask.key = $"{modConfigTask.mod_name}_{key}";
            modConfigTask.description = description;
            modConfigTask.defaultValue = defaultValue;

            return modConfigTask;
        }
        public static ModConfigTask InputWithSliderTask(string key, string description, Type type, object defaultValue, Vector2? range)
        {
            ModConfigTask modConfigTask = new ModConfigTask();
            modConfigTask.mod_config_type = ModConfigType.InputWithSlider;

            modConfigTask.key = $"{modConfigTask.mod_name}_{key}";
            modConfigTask.description = description;
            modConfigTask.type = type;
            modConfigTask.defaultValue = defaultValue;
            modConfigTask.range = range;

            return modConfigTask;
        }
    }

    public class KillFeedRecord
    {
        public GameObject container; // 容器对象
        public TextMeshProUGUI killerText;
        public Image weaponIcon;
        public TextMeshProUGUI victimText;
        public float createTime;
        public bool isFadingOut = false;
        public float currentAlpha = 0f;
        public float slideProgress = 0f;
        public Vector2 startPosition;
        public Vector2 targetPosition;
        public float verticalOffset;
        public CharacterMainControl killer;
        public CharacterMainControl victim;
    }

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static string MOD_NAME = "KillFeed";

        //强制更新config原因
        private static string FORCE_UPDATE_TOKEN = "force_update_config_due_to_weapon_icon_update";

        KillFeedConfig killFeedConfig = new KillFeedConfig();

        private static string persistentConfigPath => Path.Combine(Application.streamingAssetsPath, "KillFeedModConfig.txt");

        public static Color playerColor = new Color(0.2f, 0.8f, 0.2f);
        public static Color enemyColor = new Color(0.9f, 0.2f, 0.2f);
        public static Color weaponIconColor = new Color(0.8f, 0.8f, 0.8f);
        public static Color defaultTextColor = Color.white;

        private Queue<KillFeedRecord> killFeedQueue = new Queue<KillFeedRecord>();
        private List<KillFeedRecord> activeRecords = new List<KillFeedRecord>();
        private RectTransform killFeedContainer;

        private bool TryModConfig(KillFeedConfig config)
        {
            bool initResult = ModConfigAPI.Initialize();

            if (!initResult)
            {
                Debug.LogWarning("KillFeed加载ModConfig失败!");
                return false;
            }

            List<ModConfigTask> modConfigTasks = new List<ModConfigTask>();

            SystemLanguage[] chineseLanguages = { SystemLanguage.Chinese, SystemLanguage.ChineseSimplified, SystemLanguage.ChineseTraditional };
            
            if (chineseLanguages.Contains(LocalizationManager.CurrentLanguage))
            {
                //Chinese
                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.fontSize),
                    "文字大小", typeof(float), killFeedConfig.fontSize,
                    new Vector2(10f, 100f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.BoolDropDownListTask(
                    nameof(killFeedConfig.shouldDisplayNonMainPlayerKill),
                    "显示非玩家击杀", killFeedConfig.shouldDisplayNonMainPlayerKill));

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.maxKillFeedRecordsNum),
                    "最大显示记录数", typeof(int), killFeedConfig.maxKillFeedRecordsNum,
                    new Vector2(1, 20))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.fadeInTime),
                    "淡入时间(秒)", typeof(float), killFeedConfig.fadeInTime,
                    new Vector2(0.1f, 3f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.fadeOutTime),
                    "淡出时间(秒)", typeof(float), killFeedConfig.fadeOutTime,
                    new Vector2(0.1f, 3f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.displayTime),
                    "显示时间(秒)", typeof(float), killFeedConfig.displayTime,
                    new Vector2(1f, 15f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.slideInTime),
                    "滑入时间(秒)", typeof(float), killFeedConfig.slideInTime,
                    new Vector2(0.1f, 3f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.recordsVerticalSpacing),
                    "记录垂直间距", typeof(float), killFeedConfig.recordsVerticalSpacing,
                    new Vector2(0f, 20f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.rightMarginPercent),
                    "右侧边距百分比", typeof(float), killFeedConfig.rightMarginPercent,
                    new Vector2(0f, 0.5f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.topMarginPercent),
                    "顶部边距百分比", typeof(float), killFeedConfig.topMarginPercent,
                    new Vector2(0f, 0.5f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.weaponIconSize),
                    "武器图标大小", typeof(float), killFeedConfig.weaponIconSize,
                    new Vector2(20f, 150f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.weaponIconSpacing),
                    "武器图标间距", typeof(float), killFeedConfig.weaponIconSpacing,
                    new Vector2(0f, 20f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.myName),
                    "自定义玩家名称", typeof(string), killFeedConfig.myName,
                    null)
                    );

            }
            else
            {
                //English
                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.fontSize),
                    "Font Size", typeof(float), killFeedConfig.fontSize,
                    new Vector2(10f, 100f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.BoolDropDownListTask(
                    nameof(killFeedConfig.shouldDisplayNonMainPlayerKill),
                    "Show Non-Player Kills", killFeedConfig.shouldDisplayNonMainPlayerKill));

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.maxKillFeedRecordsNum),
                    "Max Display Records", typeof(int), killFeedConfig.maxKillFeedRecordsNum,
                    new Vector2(1, 20))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.fadeInTime),
                    "FadeIn Time (s)", typeof(float), killFeedConfig.fadeInTime,
                    new Vector2(0.1f, 3f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.fadeOutTime),
                    "FadeOut Time (s)", typeof(float), killFeedConfig.fadeOutTime,
                    new Vector2(0.1f, 3f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.displayTime),
                    "Display Time (s)", typeof(float), killFeedConfig.displayTime,
                    new Vector2(1f, 15f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.slideInTime),
                    "Slide In Time (s)", typeof(float), killFeedConfig.slideInTime,
                    new Vector2(0.1f, 3f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.recordsVerticalSpacing),
                    "Records Vertical Spacing", typeof(float), killFeedConfig.recordsVerticalSpacing,
                    new Vector2(0f, 20f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.rightMarginPercent),
                    "Right Margin Percent", typeof(float), killFeedConfig.rightMarginPercent,
                    new Vector2(0f, 0.5f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.topMarginPercent),
                    "Top Margin Percent", typeof(float), killFeedConfig.topMarginPercent,
                    new Vector2(0f, 0.5f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.weaponIconSize),
                    "Weapon Icon Size", typeof(float), killFeedConfig.weaponIconSize,
                    new Vector2(20f, 150f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.weaponIconSpacing),
                    "Weapon Icon Spacing", typeof(float), killFeedConfig.weaponIconSpacing,
                    new Vector2(0f, 20f))
                    );

                modConfigTasks.Insert(0, ModConfigTask.InputWithSliderTask(
                    nameof(killFeedConfig.myName),
                    "Custom Player Name", typeof(string), killFeedConfig.myName,
                    null)
                    );
            }

            foreach (var task in modConfigTasks)
            {
                if (task.mod_config_type == ModConfigType.InputWithSlider)
                {
                    ModConfigAPI.SafeAddInputWithSlider(
                        task.mod_name,
                        task.key,
                        task.description,
                        task.type,
                        task.defaultValue,
                        task.range);
                }
                else if (task.mod_config_type == ModConfigType.BoolDropDownList)
                {
                    ModConfigAPI.SafeAddBoolDropdownList(
                        task.mod_name,
                        task.key,
                        task.description,
                        (bool)task.defaultValue);
                }
                else if (task.mod_config_type == ModConfigType.DropDownList)
                {
                    ModConfigAPI.SafeAddDropdownList(
                        task.mod_name,
                        task.key,
                        task.description,
                        task.dropdownListDict,
                        task.type,
                        task.defaultValue);
                }
            }

            return true;
        }

        void Awake()
        {

            Debug.Log("KillFeed Mod Awake!!!");

            //备用config方案
            TryLoadingConfig();

            CreateKillFeedUI();
        }

        void OnDestroy()
        {
            foreach (var record in activeRecords)
            {
                if (record.container != null)
                    Destroy(record.container);
            }
            foreach (var record in killFeedQueue)
            {
                if (record.container != null)
                    Destroy(record.container);
            }
            if (killFeedContainer != null)
                Destroy(killFeedContainer.gameObject);
        }


        private void OnModActivated(ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            if(info.name == ModConfigAPI.ModConfigName)
            {
                Debug.Log("检测到ModConfig已激活!");
                TryModConfig(killFeedConfig);
            }
        }

        void OnEnable()
        {
            OptionsManager.OnOptionsChanged += onOptionsChanged;
            ModManager.OnModActivated += OnModActivated;
            Health.OnDead += OnDead;
        }


        void OnDisable()
        {
            OptionsManager.OnOptionsChanged -= onOptionsChanged;
            ModManager.OnModActivated -= OnModActivated;
            Health.OnDead -= OnDead;
        }

        void onOptionsChanged(string key)
        {
            if (!ModConfigAPI.isInitialized)
            {
                return;
            }

            //这里需要使用OptionsManager.Load<T>(string key, T default)读取更新后的值
            //然后更新killFeedConfig的数据
            // 使用OptionsManager.Load<T>(string key, T default)读取更新后的值
            killFeedConfig.fontSize = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.fontSize)}", killFeedConfig.fontSize);
            killFeedConfig.shouldDisplayNonMainPlayerKill = OptionsManager.Load<bool>($"{MOD_NAME}_{nameof(killFeedConfig.shouldDisplayNonMainPlayerKill)}", killFeedConfig.shouldDisplayNonMainPlayerKill);
            killFeedConfig.maxKillFeedRecordsNum = OptionsManager.Load<int>($"{MOD_NAME}_{nameof(killFeedConfig.maxKillFeedRecordsNum)}", killFeedConfig.maxKillFeedRecordsNum);
            killFeedConfig.fadeInTime = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.fadeInTime)}", killFeedConfig.fadeInTime);
            killFeedConfig.fadeOutTime = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.fadeOutTime)}", killFeedConfig.fadeOutTime);
            killFeedConfig.displayTime = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.displayTime)}", killFeedConfig.displayTime);
            killFeedConfig.slideInTime = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.slideInTime)}", killFeedConfig.slideInTime);
            killFeedConfig.recordsVerticalSpacing = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.recordsVerticalSpacing)}", killFeedConfig.recordsVerticalSpacing);
            killFeedConfig.rightMarginPercent = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.rightMarginPercent)}", killFeedConfig.rightMarginPercent);
            killFeedConfig.topMarginPercent = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.topMarginPercent)}", killFeedConfig.topMarginPercent);
            killFeedConfig.weaponIconSize = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.weaponIconSize)}", killFeedConfig.weaponIconSize);
            killFeedConfig.weaponIconSpacing = OptionsManager.Load<float>($"{MOD_NAME}_{nameof(killFeedConfig.weaponIconSpacing)}", killFeedConfig.weaponIconSpacing);
            killFeedConfig.myName = OptionsManager.Load<string>($"{MOD_NAME}_{nameof(killFeedConfig.myName)}", killFeedConfig.myName);

            // 更新UI位置和样式
            if (killFeedContainer != null)
            {
                killFeedContainer.anchoredPosition = new Vector2(-Screen.width * killFeedConfig.rightMarginPercent, -Screen.height * killFeedConfig.topMarginPercent);
            }

            // 更新现有记录的样式
            foreach (var record in activeRecords)
            {
                if (record.killerText != null)
                {
                    record.killerText.fontSize = killFeedConfig.fontSize;
                    record.killerText.color = GetCharacterColor(record.killer);
                }
                if (record.victimText != null)
                {
                    record.victimText.fontSize = killFeedConfig.fontSize;
                    record.victimText.color = GetCharacterColor(record.victim);
                }
                if (record.weaponIcon != null)
                {
                    record.weaponIcon.rectTransform.sizeDelta = new Vector2(killFeedConfig.weaponIconSize, killFeedConfig.weaponIconSize);
                }
            }

            // 更新所有记录的位置
            UpdateAllRecordsPosition();
            
        }

        private void TryLoadingConfig()
        {
            try
            {
                if (File.Exists(persistentConfigPath))
                {
                    string json = File.ReadAllText(persistentConfigPath);
                    KillFeedConfig config = JsonUtility.FromJson<KillFeedConfig>(json);

                    //检查强制更新
                    if(config.doNotEditThisTokenThisIsATokenThatForForceUpdatingConfigs != ModBehaviour.FORCE_UPDATE_TOKEN)
                    {
                        //需要强制更新
                        throw new Exception("Force Update Required!");
                    }                    

                    // 应用配置到静态变量
                    killFeedConfig = config;
                }

                SaveConfig(killFeedConfig);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载配置文件失败: {e}\n已保存默认配置");
                SaveConfig(killFeedConfig);
            }
        }

        private void SaveConfig(KillFeedConfig killFeedConfig)
        {
            //记入强制更新token, 防止下一次也强制更新
            killFeedConfig.doNotEditThisTokenThisIsATokenThatForForceUpdatingConfigs = ModBehaviour.FORCE_UPDATE_TOKEN;

            string json = JsonUtility.ToJson(killFeedConfig, true); 

            File.WriteAllText(persistentConfigPath, json);
        }

        private void CreateKillFeedUI()
        {
            // 创建容器
            var containerGO = new GameObject("KillFeedContainer");
            killFeedContainer = containerGO.AddComponent<RectTransform>();

            // 设置到屏幕右侧，使用百分比定位
            // 查找HUDCanvas <- HUDManager
            var canvas = FindObjectOfType<HUDManager>();

            if (canvas == null)
            {
                //ERROR                
                return;
            }

            killFeedContainer.SetParent(canvas.transform);

            // 设置锚点到右上角
            killFeedContainer.anchorMin = new Vector2(1f, 1f);
            killFeedContainer.anchorMax = new Vector2(1f, 1f);
            killFeedContainer.pivot = new Vector2(1f, 1f);

            // 设置位置（使用配置的值）
            killFeedContainer.anchoredPosition = new Vector2(-Screen.width * killFeedConfig.rightMarginPercent, -Screen.height * killFeedConfig.topMarginPercent);
        }

        private void OnDead(Health _health, DamageInfo dmgInfo)
        {
            if (_health == null)
            {
                return;
            }

            CharacterMainControl characterKiller = dmgInfo.fromCharacter;
            CharacterMainControl characterVictim = dmgInfo.toDamageReceiver.health.TryGetCharacter();

            if (characterKiller == null || characterVictim == null)
            {
                return;
            }

            if (!killFeedConfig.shouldDisplayNonMainPlayerKill && !characterKiller.IsMainCharacter)
            {
                return;
            }

            //获取击杀用的武器图标
            Sprite weaponSprite = null;
            Item itemKilledWith = ItemAssetsCollection.GetPrefab(dmgInfo.fromWeaponItemID);
            if (itemKilledWith != null)
            {
                weaponSprite = itemKilledWith.Icon;
            }

            AddKillRecord(characterKiller, characterVictim, weaponSprite);
        }

        private string GetCharacterName(CharacterMainControl character)
        {
            if (character.IsMainCharacter)
            {
                if (killFeedConfig.myName == "")
                {
                    return "DeathReason_Self".ToPlainText();
                }
                else
                {
                    return killFeedConfig.myName;
                }

            }
            else if (character.characterPreset != null)
            {
                return character.characterPreset.DisplayName;
            }
            return "Unknown";
        }

        private Color GetCharacterColor(CharacterMainControl character)
        {
            if (character.IsMainCharacter)
            {
                return playerColor;
            }
            else
            {
                return enemyColor;
            }
        }

        private void AddKillRecord(CharacterMainControl killer, CharacterMainControl victim, Sprite weaponSprite)
        {
            if (killFeedContainer == null)
            {
                CreateKillFeedUI();
            }

            // 创建容器对象
            var containerGO = new GameObject($"KillRecord_{Time.time}");
            var containerRect = containerGO.AddComponent<RectTransform>();

            // 设置水平布局
            var horizontalLayout = containerGO.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleRight;
            horizontalLayout.spacing = killFeedConfig.weaponIconSpacing;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = false;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;

            // 设置到容器中
            containerRect.SetParent(killFeedContainer);
            containerRect.localScale = Vector3.one;
            containerRect.anchorMin = new Vector2(1f, 1f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 1f);

            // 移除内部容器，直接在主容器中添加元素
            // 1. 创建击杀者文本
            var killerTextGO = new GameObject("KillerText");
            var killerTextComp = killerTextGO.AddComponent<TextMeshProUGUI>();
            var killerRect = killerTextGO.GetComponent<RectTransform>();
            killerRect.SetParent(containerRect);

            // 设置击杀者文本样式
            var templateText = GameplayDataSettings.UIStyle.TemplateTextUGUI;
            if (templateText != null)
            {
                killerTextComp.font = templateText.font;
                killerTextComp.fontSize = killFeedConfig.fontSize;
                killerTextComp.color = GetCharacterColor(killer);
                killerTextComp.alignment = TextAlignmentOptions.Right;
            }
            killerTextComp.text = GetCharacterName(killer);

            // 2. 创建武器图标
            var weaponIconGO = new GameObject("WeaponIcon");
            var weaponIconComp = weaponIconGO.AddComponent<Image>();
            var weaponIconRect = weaponIconGO.GetComponent<RectTransform>();
            weaponIconRect.SetParent(containerRect);

            float weaponIconSize = killFeedConfig.weaponIconSize;
            weaponIconRect.sizeDelta = new Vector2(weaponIconSize, weaponIconSize);
            weaponIconComp.preserveAspect = true;

            if (weaponSprite != null)
            {
                weaponIconComp.sprite = weaponSprite;
            }
            weaponIconComp.color = weaponIconColor;

            // 3. 创建受害者文本
            var victimTextGO = new GameObject("VictimText");
            var victimTextComp = victimTextGO.AddComponent<TextMeshProUGUI>();
            var victimRect = victimTextGO.GetComponent<RectTransform>();
            victimRect.SetParent(containerRect);

            if (templateText != null)
            {
                victimTextComp.font = templateText.font;
                victimTextComp.fontSize = killFeedConfig.fontSize;
                victimTextComp.color = GetCharacterColor(victim);
                victimTextComp.alignment = TextAlignmentOptions.Left;
            }
            victimTextComp.text = GetCharacterName(victim);

            // 设置容器大小 - 简化计算
            float containerHeight = Mathf.Max(killFeedConfig.fontSize + 10, weaponIconSize);
            containerRect.sizeDelta = new Vector2(400, containerHeight);

            // 新记录放在最底部，所以偏移量是当前记录数 * 间距
            float verticalOffset = activeRecords.Count * killFeedConfig.recordsVerticalSpacing;

            // 初始位置设置为屏幕外右侧
            Vector2 startPos = new Vector2(400f, -verticalOffset);
            Vector2 targetPos = new Vector2(0f, -verticalOffset);

            containerRect.anchoredPosition = startPos;

            // 创建记录
            var record = new KillFeedRecord
            {
                container = containerGO,
                killerText = killerTextComp,
                weaponIcon = weaponIconComp,
                victimText = victimTextComp,
                createTime = Time.time,
                currentAlpha = 0f,
                slideProgress = 0f,
                startPosition = startPos,
                targetPosition = targetPos,
                verticalOffset = verticalOffset,
                killer = killer,
                victim = victim
            };

            // 设置初始透明度
            SetRecordAlpha(record, 0f);

            // 添加到活动记录列表的末尾（新的在最下面）
            activeRecords.Add(record);

            // 如果超过最大数量，移除最老的（第一个）
            if (activeRecords.Count > killFeedConfig.maxKillFeedRecordsNum)
            {
                RemoveOldestRecord();
            }
            else
            {
                // 只有不超过最大数量时才需要更新位置
                UpdateAllRecordsPosition();
            }
        }

        private void SetRecordAlpha(KillFeedRecord record, float alpha)
        {
            if (record.killerText != null)
                record.killerText.color = new Color(record.killerText.color.r, record.killerText.color.g, record.killerText.color.b, alpha);

            if (record.weaponIcon != null)
                record.weaponIcon.color = new Color(record.weaponIcon.color.r, record.weaponIcon.color.g, record.weaponIcon.color.b, alpha);

            if (record.victimText != null)
                record.victimText.color = new Color(record.victimText.color.r, record.victimText.color.g, record.victimText.color.b, alpha);
        }

        private void UpdateAllRecordsPosition()
        {
            // 重新计算所有记录的位置
            for (int i = 0; i < activeRecords.Count; i++)
            {
                var record = activeRecords[i];

                // 重要修复：重新计算每个记录的垂直偏移
                // 修复重叠问题：考虑容器实际高度
                float containerHeight = Mathf.Max(killFeedConfig.fontSize + 10, killFeedConfig.weaponIconSize);
                float newOffset = i * (containerHeight + killFeedConfig.recordsVerticalSpacing);
                record.targetPosition = new Vector2(0f, -newOffset);
                record.verticalOffset = newOffset;

                // 如果记录已经完成滑动，直接设置到新位置
                if (record.slideProgress >= 1f && record.container != null)
                {
                    record.container.GetComponent<RectTransform>().anchoredPosition = record.targetPosition;
                }
            }
        }

        private void RemoveOldestRecord()
        {
            if (activeRecords.Count > 0)
            {
                // 移除最老的记录（列表中的第一个）
                var oldestRecord = activeRecords[0];
                activeRecords.RemoveAt(0);
                if (oldestRecord.container != null)
                    Destroy(oldestRecord.container);

                // 移除后重新计算所有记录的位置
                UpdateAllRecordsPosition();
            }
        }

        private void Update()
        {
            // 优化: 只在有活动记录时才执行动画更新
            if (activeRecords.Count > 0)
            {
                UpdateRecordsAnimation();
            }
        }

        private void UpdateRecordsAnimation()
        {
            float currentTime = Time.time;

            for (int i = activeRecords.Count - 1; i >= 0; i--)
            {
                var record = activeRecords[i];
                float timeSinceCreation = currentTime - record.createTime;

                if (record.container == null)
                {
                    activeRecords.RemoveAt(i);
                    UpdateAllRecordsPosition();
                    continue;
                }

                var rectTransform = record.container.GetComponent<RectTransform>();

                // 更新目标位置（使用当前计算好的偏移）
                record.targetPosition = new Vector2(0f, -record.verticalOffset);

                // 滑动阶段
                if (timeSinceCreation < killFeedConfig.slideInTime)
                {
                    record.slideProgress = Mathf.Clamp01(timeSinceCreation / killFeedConfig.slideInTime);
                    float easedProgress = EaseOutCubic(record.slideProgress);

                    Vector2 currentPosition = Vector2.Lerp(
                        record.startPosition,
                        record.targetPosition,
                        easedProgress
                    );

                    rectTransform.anchoredPosition = currentPosition;
                }
                else
                {
                    record.slideProgress = 1f;
                    rectTransform.anchoredPosition = record.targetPosition;
                }

                // 淡入阶段
                if (timeSinceCreation < killFeedConfig.fadeInTime)
                {
                    record.currentAlpha = Mathf.Clamp01(timeSinceCreation / killFeedConfig.fadeInTime);
                }
                // 显示阶段
                else if (timeSinceCreation < killFeedConfig.fadeInTime + killFeedConfig.displayTime)
                {
                    record.currentAlpha = 1f;
                }
                // 淡出阶段
                else if (timeSinceCreation < killFeedConfig.fadeInTime + killFeedConfig.displayTime + killFeedConfig.fadeOutTime)
                {
                    float fadeOutProgress = (timeSinceCreation - killFeedConfig.fadeInTime - killFeedConfig.displayTime) / killFeedConfig.fadeOutTime;
                    record.currentAlpha = Mathf.Clamp01(1f - fadeOutProgress);
                    record.isFadingOut = true;
                }
                // 结束，准备移除
                else
                {
                    record.currentAlpha = 0f;
                    record.isFadingOut = true;
                }

                // 应用透明度到所有UI元素
                SetRecordAlpha(record, record.currentAlpha);

                // 移除已经完全淡出的记录
                if (record.isFadingOut && record.currentAlpha <= 0f)
                {
                    Destroy(record.container);
                    activeRecords.RemoveAt(i);
                    UpdateAllRecordsPosition();
                }
            }
        }

        private float EaseOutCubic(float x)
        {
            return 1f - Mathf.Pow(1f - x, 3f);
        }

    }
}