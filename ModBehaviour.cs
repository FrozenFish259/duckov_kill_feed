using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using System.Collections.Generic;
using System.IO;
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
        public bool shouldDisplayNonMainPlayerKill = true;
        public int maxKillFeedRecordsNum = 6;

        public float fadeInTime = 0.3f;
        public float displayTime = 25f;
        public float fadeOutTime = 0.6f;
        public float slideInTime = 0.4f;

        public float itemSpacing = 50f;

        public float rightMarginPercent = 0.05f;
        public float topMarginPercent = 0.15f;
        public float fontSize = 30f;
    }

    public class KillFeedRecord
    {
        public TextMeshProUGUI textElement;
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
        KillFeedConfig killFeedConfig = new KillFeedConfig();

        private static string ConfigPath => Path.Combine(Application.streamingAssetsPath, "KillFeedModConfig.txt");

        public static Color playerColor = new Color(0.2f, 0.8f, 0.2f);
        public static Color enemyColor = new Color(0.9f, 0.2f, 0.2f);
        public static Color killedTextColor = new Color(0.8f, 0.8f, 0.8f);
        public static Color defaultTextColor = Color.white;

        private Queue<KillFeedRecord> killFeedQueue = new Queue<KillFeedRecord>();
        private List<KillFeedRecord> activeRecords = new List<KillFeedRecord>();
        private RectTransform killFeedContainer;

        void Awake()
        {
            Debug.Log("KillFeed Mod Loaded!!!");

            // 加载配置
            TryLoadingConfig();

            CreateKillFeedUI();
        }

        void OnDestroy()
        {
            foreach (var record in activeRecords)
            {
                if (record.textElement != null)
                    Destroy(record.textElement.gameObject);
            }
            foreach (var record in killFeedQueue)
            {
                if (record.textElement != null)
                    Destroy(record.textElement.gameObject);
            }
            if (killFeedContainer != null)
                Destroy(killFeedContainer.gameObject);
        }

        void OnEnable()
        {
            Health.OnDead += OnDead;
        }

        void OnDisable()
        {
            Health.OnDead -= OnDead;
        }

        private void TryLoadingConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    KillFeedConfig config = JsonUtility.FromJson<KillFeedConfig>(json);

                    // 应用配置到静态变量
                    killFeedConfig = config;

                }
                else
                {
                    Debug.Log("配置文件不存在，使用默认配置");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载配置文件失败: {e}");
            }
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
            CharacterMainControl characterVictim = dmgInfo.toDamageReceiver?.health.TryGetCharacter();

            if (characterKiller == null || characterVictim == null)
            {
                return;
            }

            if (!killFeedConfig.shouldDisplayNonMainPlayerKill && !characterKiller.IsMainCharacter)
            {
                return;
            }

            AddKillRecord(characterKiller, characterVictim);
        }

        private string GetCharacterName(CharacterMainControl character)
        {
            if (character.IsMainCharacter)
            {
                return "DeathReason_Self".ToPlainText();
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

        private void AddKillRecord(CharacterMainControl killer, CharacterMainControl victim)
        {
            if (killFeedContainer == null)
            {
                CreateKillFeedUI();
            }

            // 创建新的文本元素
            var textGO = new GameObject($"KillRecord_{Time.time}");
            var textComp = textGO.AddComponent<TextMeshProUGUI>();

            // 复制样式
            var templateText = GameplayDataSettings.UIStyle.TemplateTextUGUI;
            if (templateText != null)
            {
                textComp.font = templateText.font;
                textComp.fontSize = killFeedConfig.fontSize; // 使用配置的字体大小
                textComp.color = defaultTextColor;
                textComp.alignment = TextAlignmentOptions.Right;
                textComp.richText = true;
            }

            // 设置富文本格式的击杀信息
            string killerName = GetCharacterName(killer);
            string victimName = GetCharacterName(victim);
            Color killerColor = GetCharacterColor(killer);
            Color victimColor = GetCharacterColor(victim);

            string formattedText = FormatKillMessage(killerName, victimName, killerColor, victimColor);
            textComp.text = formattedText;

            // 设置到容器中
            var rectTransform = textGO.GetComponent<RectTransform>();
            rectTransform.SetParent(killFeedContainer);
            rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = new Vector2(500, 100);
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);

            // 新记录放在最底部，所以偏移量是当前记录数 * 间距
            float verticalOffset = activeRecords.Count * killFeedConfig.itemSpacing;

            // 初始位置设置为屏幕外右侧
            Vector2 startPos = new Vector2(400f, -verticalOffset);
            Vector2 targetPos = new Vector2(0f, -verticalOffset);

            rectTransform.anchoredPosition = startPos;

            // 创建记录
            var record = new KillFeedRecord
            {
                textElement = textComp,
                createTime = Time.time,
                currentAlpha = 0f,
                slideProgress = 0f,
                startPosition = startPos,
                targetPosition = targetPos,
                verticalOffset = verticalOffset,
                killer = killer,
                victim = victim
            };

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

        private string FormatKillMessage(string killerName, string victimName, Color killerColor, Color victimColor)
        {
            // 将颜色转换为16进制
            string killerHex = ColorUtility.ToHtmlStringRGB(killerColor);
            string victimHex = ColorUtility.ToHtmlStringRGB(victimColor);
            string killedHex = ColorUtility.ToHtmlStringRGB(killedTextColor);

            float iconSize = killFeedConfig.fontSize - 3;

            // 富文本格式
            return $"<color=#{killerHex}>{killerName}</color> " +
                   $"<color=#{killedHex}><i><size={iconSize}> killed </size></i></color> " +
                   $"<color=#{victimHex}>{victimName}</color>";
        }

        private void UpdateAllRecordsPosition()
        {
            // 重新计算所有记录的位置
            for (int i = 0; i < activeRecords.Count; i++)
            {
                var record = activeRecords[i];

                // 重要修复：重新计算每个记录的垂直偏移
                float newOffset = i * killFeedConfig.itemSpacing;
                record.targetPosition = new Vector2(0f, -newOffset);
                record.verticalOffset = newOffset;

                // 如果记录已经完成滑动，直接设置到新位置
                if (record.slideProgress >= 1f)
                {
                    record.textElement.rectTransform.anchoredPosition = record.targetPosition;
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
                if (oldestRecord.textElement != null)
                    Destroy(oldestRecord.textElement.gameObject);

                // 移除后重新计算所有记录的位置
                UpdateAllRecordsPosition();
            }
        }

        private void Update()
        {
            UpdateRecordsAnimation();
        }

        private void UpdateRecordsAnimation()
        {
            float currentTime = Time.time;

            for (int i = activeRecords.Count - 1; i >= 0; i--)
            {
                var record = activeRecords[i];
                float timeSinceCreation = currentTime - record.createTime;

                if (record.textElement == null)
                {
                    activeRecords.RemoveAt(i);
                    UpdateAllRecordsPosition();
                    continue;
                }

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

                    record.textElement.rectTransform.anchoredPosition = currentPosition;
                }
                else
                {
                    record.slideProgress = 1f;
                    record.textElement.rectTransform.anchoredPosition = record.targetPosition;
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

                // 应用透明度到整个文本元素
                Color color = record.textElement.color;
                color.a = record.currentAlpha;
                record.textElement.color = color;

                // 移除已经完全淡出的记录
                if (record.isFadingOut && record.currentAlpha <= 0f)
                {
                    Destroy(record.textElement.gameObject);
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