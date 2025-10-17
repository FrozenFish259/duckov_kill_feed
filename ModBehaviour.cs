using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KillFeed
{
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
    }

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static bool shouldDisplayNonMainPlayerKill = true;
        public static int maxKillFeedRecordsNum = 6;
        public static float fadeInTime = 0.3f;
        public static float displayTime = 25f;
        public static float fadeOutTime = 0.6f;
        public static float slideInTime = 0.4f;
        public static float itemSpacing = 50f;

        private Queue<KillFeedRecord> killFeedQueue = new Queue<KillFeedRecord>();
        private List<KillFeedRecord> activeRecords = new List<KillFeedRecord>();
        private RectTransform killFeedContainer;

        void Awake()
        {
            Debug.Log("KillFeed Mod Loaded!!!");
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

        private void CreateKillFeedUI()
        {
            // 创建容器
            var containerGO = new GameObject("KillFeedContainer");
            killFeedContainer = containerGO.AddComponent<RectTransform>();

            // 设置到屏幕右侧，使用百分比定位
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                killFeedContainer.SetParent(canvas.transform);
            }

            // 设置锚点到右上角
            killFeedContainer.anchorMin = new Vector2(1f, 1f);
            killFeedContainer.anchorMax = new Vector2(1f, 1f);
            killFeedContainer.pivot = new Vector2(1f, 1f);

            // 设置位置（距离右边5%，距离顶部10%）
            killFeedContainer.anchoredPosition = new Vector2(-Screen.width * 0.05f, -Screen.height * 0.1f);
            killFeedContainer.sizeDelta = new Vector2(Screen.width * 0.25f, Screen.height * 0.4f);
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

            if (!shouldDisplayNonMainPlayerKill && !characterKiller.IsMainCharacter)
            {
                return;
            }

            string killerName = GetCharacterName(characterKiller);
            string victimName = GetCharacterName(characterVictim);

            AddKillRecord(killerName, victimName);
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

        private void AddKillRecord(string killer, string victim)
        {
            // 创建新的文本元素
            var textGO = new GameObject($"KillRecord_{Time.time}");
            var textComp = textGO.AddComponent<TextMeshProUGUI>();

            // 复制样式
            var templateText = GameplayDataSettings.UIStyle.TemplateTextUGUI;
            if (templateText != null)
            {
                textComp.font = templateText.font;
                textComp.fontSize = 24;
                textComp.color = Color.white;
                textComp.alignment = TextAlignmentOptions.Right;
            }

            // 设置文本
            textComp.text = $"{killer} killed {victim}";

            // 设置到容器中
            var rectTransform = textGO.GetComponent<RectTransform>();
            rectTransform.SetParent(killFeedContainer);
            rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = new Vector2(280, 35);
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);

            // 计算垂直偏移量（新记录在最底部）
            float verticalOffset = (activeRecords.Count) * itemSpacing;

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
                verticalOffset = verticalOffset
            };

            // 添加到活动记录列表的末尾（新的在最下面）
            activeRecords.Add(record);

            // 如果超过最大数量，移除最老的（第一个）
            if (activeRecords.Count > maxKillFeedRecordsNum)
            {
                RemoveOldestRecord();
            }
        }

        private void UpdateAllRecordsPosition()
        {
            // 更新所有记录的目标位置（从下到上排列）
            for (int i = 0; i < activeRecords.Count; i++)
            {
                var record = activeRecords[i];
                // 最老的记录在顶部（偏移最小），最新的在底部（偏移最大）
                float newOffset = (activeRecords.Count - 1 - i) * itemSpacing;
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

                // 移除后更新其他记录的位置
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
                    continue;
                }

                // 更新目标位置
                record.targetPosition = new Vector2(0f, -record.verticalOffset);

                // 滑动阶段
                if (timeSinceCreation < slideInTime)
                {
                    record.slideProgress = Mathf.Clamp01(timeSinceCreation / slideInTime);
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
                if (timeSinceCreation < fadeInTime)
                {
                    record.currentAlpha = Mathf.Clamp01(timeSinceCreation / fadeInTime);
                }
                // 显示阶段
                else if (timeSinceCreation < fadeInTime + displayTime)
                {
                    record.currentAlpha = 1f;
                }
                // 淡出阶段
                else if (timeSinceCreation < fadeInTime + displayTime + fadeOutTime)
                {
                    float fadeOutProgress = (timeSinceCreation - fadeInTime - displayTime) / fadeOutTime;
                    record.currentAlpha = Mathf.Clamp01(1f - fadeOutProgress);
                    record.isFadingOut = true;
                }
                // 结束，准备移除
                else
                {
                    record.currentAlpha = 0f;
                    record.isFadingOut = true;
                }

                // 应用透明度
                Color color = record.textElement.color;
                color.a = record.currentAlpha;
                record.textElement.color = color;

                // 移除已经完全淡出的记录
                if (record.isFadingOut && record.currentAlpha <= 0f)
                {
                    Destroy(record.textElement.gameObject);
                    activeRecords.RemoveAt(i);

                    // 移除后更新其他记录的位置
                    UpdateAllRecordsPosition();
                }
            }
        }

        private float EaseOutCubic(float x)
        {
            return 1f - Mathf.Pow(1f - x, 3f);
        }

        private void FixedUpdate()
        {
            // 可以留空或移除
        }
    }
}