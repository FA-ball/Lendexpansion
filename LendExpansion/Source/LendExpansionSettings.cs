using System;
using System.Reflection;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace LendExpansion
{
    /// <summary>
    /// Persistent player-configurable settings for LendExpansion.
    /// </summary>
    public class LendExpansionSettings : ModSettings
    {
        // Global quest availability and relative root-selection weight.
        public bool enableLendExpansionQuest = true;
        public float questSelectionWeight = 5.0f;

        public bool enableReturnPregnancy = true;
        public float returnPregnancyChance = 0.15f;

        // Optional anomaly-story override. The commissioner is still Royal or Local;
        // this switch only enables/disables the later anomaly probability roll.
        public bool enableAnomalyEvents = true;
        public float anomalyStoryChance = 0.20f;

        // Master switch for the young-colonist storyline. Age eligibility and
        // probability are ignored when this is disabled.
        public bool enableYoungStory = true;

        // Probability that an age-eligible pawn actually enters the young storyline.
        // Age below the category threshold is only eligibility; the branch is rolled once at boarding.
        public float youngStoryTriggerChance = 0.30f;

        // Independent pregnancy settings for the young-story branch.
        public bool enableYoungColonistPregnancy = true;
        public float youngColonistPregnancyChance = 0.10f;

        // Player-configurable quest parameters.
        public float taskDurationDays = 2.0f;
        public float taskRewardMultiplier = 1.0f;

        // Independent randomization switches for quest duration and reward.
        public bool randomTaskDuration = false;
        public bool randomTaskReward = false;

        // Pawn PlayLog controls. The fixed count is retained while random mode is enabled.
        public bool enablePawnPlayLog = true;
        public int pawnPlayLogEntryCount = 8;
        public bool randomPawnPlayLogEntryCount = false;

        // Return mood controls. Multiplier affects the mood memory only.
        public bool enableReturnMoodEffect = true;
        public float returnMoodEffectMultiplier = 1.0f;
        public bool randomReturnMoodEffectMultiplier = false;

        // Return health-state controls. Negative scaling affects pain and movement speed.
        public bool enableReturnHealthEffect = true;
        public bool enableNegativeReturnHealthEffects = true;
        public float negativeReturnHealthEffectMultiplier = 1.0f;
        public bool randomNegativeReturnHealthEffectMultiplier = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(
                ref enableLendExpansionQuest,
                "enableLendExpansionQuest",
                true);

            Scribe_Values.Look(
                ref questSelectionWeight,
                "questSelectionWeight",
                5.0f);

            Scribe_Values.Look(
                ref enableReturnPregnancy,
                "enableReturnPregnancy",
                true);

            Scribe_Values.Look(
                ref returnPregnancyChance,
                "returnPregnancyChance",
                0.15f);

            Scribe_Values.Look(
                ref enableAnomalyEvents,
                "enableAnomalyEvents",
                true);

            Scribe_Values.Look(
                ref anomalyStoryChance,
                "anomalyStoryChance",
                0.20f);

            Scribe_Values.Look(
                ref enableYoungStory,
                "enableYoungStory",
                true);

            Scribe_Values.Look(
                ref youngStoryTriggerChance,
                "youngStoryTriggerChance",
                0.30f);

            Scribe_Values.Look(
                ref enableYoungColonistPregnancy,
                "enableYoungColonistPregnancy",
                true);

            Scribe_Values.Look(
                ref youngColonistPregnancyChance,
                "youngColonistPregnancyChance",
                0.10f);

            Scribe_Values.Look(
                ref taskDurationDays,
                "taskDurationDays",
                2f);

            Scribe_Values.Look(
                ref taskRewardMultiplier,
                "taskRewardMultiplier",
                1.0f);

            Scribe_Values.Look(
                ref randomTaskDuration,
                "randomTaskDuration",
                false);

            Scribe_Values.Look(
                ref randomTaskReward,
                "randomTaskReward",
                false);

            Scribe_Values.Look(
                ref enablePawnPlayLog,
                "enablePawnPlayLog",
                true);

            Scribe_Values.Look(
                ref pawnPlayLogEntryCount,
                "pawnPlayLogEntryCount",
                8);

            Scribe_Values.Look(
                ref randomPawnPlayLogEntryCount,
                "randomPawnPlayLogEntryCount",
                false);

            Scribe_Values.Look(
                ref enableReturnMoodEffect,
                "enableReturnMoodEffect",
                true);

            Scribe_Values.Look(
                ref returnMoodEffectMultiplier,
                "returnMoodEffectMultiplier",
                1.0f);

            Scribe_Values.Look(
                ref randomReturnMoodEffectMultiplier,
                "randomReturnMoodEffectMultiplier",
                false);

            Scribe_Values.Look(
                ref enableReturnHealthEffect,
                "enableReturnHealthEffect",
                true);

            Scribe_Values.Look(
                ref enableNegativeReturnHealthEffects,
                "enableNegativeReturnHealthEffects",
                true);

            Scribe_Values.Look(
                ref negativeReturnHealthEffectMultiplier,
                "negativeReturnHealthEffectMultiplier",
                1.0f);

            Scribe_Values.Look(
                ref randomNegativeReturnHealthEffectMultiplier,
                "randomNegativeReturnHealthEffectMultiplier",
                false);

            if (questSelectionWeight < 0f)
                questSelectionWeight = 0f;
            else if (questSelectionWeight > 30f)
                questSelectionWeight = 30f;

            // Keep quest weight on 0.1 increments.
            questSelectionWeight =
                Mathf.Round(questSelectionWeight * 10f) / 10f;

            if (returnPregnancyChance < 0f)
                returnPregnancyChance = 0f;
            else if (returnPregnancyChance > 1f)
                returnPregnancyChance = 1f;

            if (anomalyStoryChance < 0f)
                anomalyStoryChance = 0f;
            else if (anomalyStoryChance > 1f)
                anomalyStoryChance = 1f;

            if (youngStoryTriggerChance < 0f)
                youngStoryTriggerChance = 0f;
            else if (youngStoryTriggerChance > 1f)
                youngStoryTriggerChance = 1f;

            if (youngColonistPregnancyChance < 0f)
                youngColonistPregnancyChance = 0f;
            else if (youngColonistPregnancyChance > 1f)
                youngColonistPregnancyChance = 1f;

            if (taskDurationDays < 0f)
                taskDurationDays = 0f;
            else if (taskDurationDays > 5f)
                taskDurationDays = 5f;

            // Keep duration on 0.1-day increments.
            taskDurationDays =
                Mathf.Round(taskDurationDays * 10f) / 10f;

            if (taskRewardMultiplier < 0f)
                taskRewardMultiplier = 0f;
            else if (taskRewardMultiplier > 10f)
                taskRewardMultiplier = 10f;

            // Keep reward multiplier on 1% increments.
            taskRewardMultiplier =
                Mathf.Round(taskRewardMultiplier * 100f) / 100f;

            if (pawnPlayLogEntryCount < 0)
                pawnPlayLogEntryCount = 0;
            else if (pawnPlayLogEntryCount > 8)
                pawnPlayLogEntryCount = 8;

            if (returnMoodEffectMultiplier < 0f)
                returnMoodEffectMultiplier = 0f;
            else if (returnMoodEffectMultiplier > 5f)
                returnMoodEffectMultiplier = 5f;
            returnMoodEffectMultiplier =
                Mathf.Round(returnMoodEffectMultiplier * 100f) / 100f;

            if (negativeReturnHealthEffectMultiplier < 0f)
                negativeReturnHealthEffectMultiplier = 0f;
            else if (negativeReturnHealthEffectMultiplier > 5f)
                negativeReturnHealthEffectMultiplier = 5f;
            negativeReturnHealthEffectMultiplier =
                Mathf.Round(negativeReturnHealthEffectMultiplier * 100f) / 100f;

            base.ExposeData();
        }
    }

    /// <summary>
    /// Applies the Mod Settings value to this quest's QuestScriptDef at runtime.
    /// Disabling the quest is implemented as an effective root-selection weight of 0.
    /// </summary>
    public static class LendExpansionQuestSelectionController
    {
        private const string QuestDefName = "FA_BALL_LendColonist_test";
        private static FieldInfo weightField;
        private static PropertyInfo weightProperty;
        private static bool weightMemberResolved;
        private static bool missingMemberWarningShown;

        public static void ApplyCurrentSettings()
        {
            QuestScriptDef questDef =
                DefDatabase<QuestScriptDef>.GetNamedSilentFail(QuestDefName);

            if (questDef == null)
                return;

            ResolveWeightMember(questDef.GetType());

            float effectiveWeight =
                LendExpansionMod.LendExpansionQuestEnabled
                    ? LendExpansionMod.LendExpansionQuestSelectionWeight
                    : 0f;

            try
            {
                if (weightField != null)
                {
                    weightField.SetValue(questDef, effectiveWeight);
                    return;
                }

                if (weightProperty != null)
                {
                    weightProperty.SetValue(questDef, effectiveWeight, null);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[LendExpansion] Could not apply quest selection weight: " +
                    ex.Message);
                return;
            }

            if (!missingMemberWarningShown)
            {
                missingMemberWarningShown = true;
                Log.Warning(
                    "[LendExpansion] QuestScriptDef rootSelectionWeight member was not found; " +
                    "the quest enable/weight setting cannot be applied.");
            }
        }

        private static void ResolveWeightMember(Type startType)
        {
            if (weightMemberResolved)
                return;

            weightMemberResolved = true;
            Type type = startType;
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            while (type != null && weightField == null)
            {
                FieldInfo candidate = type.GetField("rootSelectionWeight", flags);
                if (candidate != null && candidate.FieldType == typeof(float))
                    weightField = candidate;

                type = type.BaseType;
            }

            if (weightField != null)
                return;

            type = startType;
            while (type != null && weightProperty == null)
            {
                PropertyInfo candidate = type.GetProperty("RootSelectionWeight", flags);
                if (candidate == null)
                    candidate = type.GetProperty("rootSelectionWeight", flags);

                if (candidate != null &&
                    candidate.PropertyType == typeof(float) &&
                    candidate.CanWrite)
                {
                    weightProperty = candidate;
                }

                type = type.BaseType;
            }
        }
    }

    /// <summary>
    /// RimWorld Mod entry point and native Mod Settings UI.
    /// </summary>
    public class LendExpansionMod : Mod
    {
        public static LendExpansionSettings Settings;

        // Scroll position for the native RimWorld mod-settings window.
        // The settings list is taller than the visible window, so keep it
        // inside a scroll view instead of allowing lower controls to be clipped.
        private Vector2 settingsScrollPosition = Vector2.zero;

        // Estimated content height for the full settings page.
        // Keep this comfortably larger than the current Listing_Standard content.
        private const float SettingsContentHeight = 2700f;

        public LendExpansionMod(ModContentPack content)
            : base(content)
        {
            Settings = GetSettings<LendExpansionSettings>();
            LongEventHandler.ExecuteWhenFinished(
                LendExpansionQuestSelectionController.ApplyCurrentSettings);
        }

        public static bool LendExpansionQuestEnabled
        {
            get
            {
                return Settings == null || Settings.enableLendExpansionQuest;
            }
        }

        public static float LendExpansionQuestSelectionWeight
        {
            get
            {
                float value = Settings == null
                    ? 5.0f
                    : Settings.questSelectionWeight;
                value = Mathf.Clamp(value, 0f, 30f);
                return Mathf.Round(value * 10f) / 10f;
            }
        }

        public static bool PregnancyEnabled
        {
            get
            {
                return Settings == null || Settings.enableReturnPregnancy;
            }
        }

        public static float PregnancyChance
        {
            get
            {
                float value =
                    Settings == null
                        ? 0.15f
                        : Settings.returnPregnancyChance;

                if (value < 0f)
                    return 0f;
                if (value > 1f)
                    return 1f;

                return value;
            }
        }

        public static bool AnomalyEventsEnabled
        {
            get
            {
                return Settings == null || Settings.enableAnomalyEvents;
            }
        }

        public static float AnomalyStoryChance
        {
            get
            {
                float value = Settings == null
                    ? 0.20f
                    : Settings.anomalyStoryChance;

                if (value < 0f)
                    return 0f;
                if (value > 1f)
                    return 1f;

                return value;
            }
        }

        public static bool YoungStoryEnabled
        {
            get
            {
                return Settings == null || Settings.enableYoungStory;
            }
        }

        public static float YoungStoryTriggerChance
        {
            get
            {
                float value =
                    Settings == null
                        ? 0.30f
                        : Settings.youngStoryTriggerChance;

                if (value < 0f)
                    return 0f;
                if (value > 1f)
                    return 1f;

                return value;
            }
        }

        public static bool YoungColonistPregnancyEnabled
        {
            get
            {
                return Settings == null ||
                    Settings.enableYoungColonistPregnancy;
            }
        }

        public static float YoungColonistPregnancyChance
        {
            get
            {
                float value =
                    Settings == null
                        ? 0.10f
                        : Settings.youngColonistPregnancyChance;

                if (value < 0f)
                    return 0f;
                if (value > 1f)
                    return 1f;

                return value;
            }
        }

        public static bool PawnPlayLogEnabled
        {
            get
            {
                return Settings == null || Settings.enablePawnPlayLog;
            }
        }

        public static int PawnPlayLogEntryCount
        {
            get
            {
                int value = Settings == null ? 8 : Settings.pawnPlayLogEntryCount;
                if (value < 0)
                    return 0;
                if (value > 8)
                    return 8;
                return value;
            }
        }

        public static bool RandomPawnPlayLogEntryCountEnabled
        {
            get
            {
                return Settings != null && Settings.randomPawnPlayLogEntryCount;
            }
        }

        public static int ResolvePawnPlayLogEntryCount()
        {
            if (!PawnPlayLogEnabled)
                return 0;

            return RandomPawnPlayLogEntryCountEnabled
                ? Rand.RangeInclusive(3, 5)
                : PawnPlayLogEntryCount;
        }

        public static bool ReturnMoodEffectEnabled
        {
            get
            {
                return Settings == null || Settings.enableReturnMoodEffect;
            }
        }

        public static float ReturnMoodEffectMultiplier
        {
            get
            {
                float value = Settings == null
                    ? 1.0f
                    : Settings.returnMoodEffectMultiplier;
                return Mathf.Clamp(value, 0f, 5f);
            }
        }

        public static bool RandomReturnMoodEffectMultiplierEnabled
        {
            get
            {
                return Settings != null && Settings.randomReturnMoodEffectMultiplier;
            }
        }

        public static float ResolveReturnMoodEffectMultiplier()
        {
            return RandomReturnMoodEffectMultiplierEnabled
                ? Rand.RangeInclusive(50, 150) / 100f
                : ReturnMoodEffectMultiplier;
        }

        public static bool ReturnHealthEffectEnabled
        {
            get
            {
                return Settings == null || Settings.enableReturnHealthEffect;
            }
        }

        public static bool NegativeReturnHealthEffectsEnabled
        {
            get
            {
                return Settings == null || Settings.enableNegativeReturnHealthEffects;
            }
        }

        public static float NegativeReturnHealthEffectMultiplier
        {
            get
            {
                float value = Settings == null
                    ? 1.0f
                    : Settings.negativeReturnHealthEffectMultiplier;
                return Mathf.Clamp(value, 0f, 5f);
            }
        }

        public static bool RandomNegativeReturnHealthEffectMultiplierEnabled
        {
            get
            {
                return Settings != null && Settings.randomNegativeReturnHealthEffectMultiplier;
            }
        }

        public static float ResolveNegativeReturnHealthEffectMultiplier()
        {
            return RandomNegativeReturnHealthEffectMultiplierEnabled
                ? Rand.RangeInclusive(50, 150) / 100f
                : NegativeReturnHealthEffectMultiplier;
        }

        public static float TaskDurationDays
        {
            get
            {
                float value = Settings == null ? 2.0f : Settings.taskDurationDays;

                if (value < 0f)
                    value = 0f;
                else if (value > 5f)
                    value = 5f;

                return Mathf.Round(value * 10f) / 10f;
            }
        }

        public static bool RandomTaskDurationEnabled
        {
            get
            {
                return Settings != null && Settings.randomTaskDuration;
            }
        }

        public static int TaskDurationTicks
        {
            get
            {
                return TaskDurationDaysToTicks(TaskDurationDays);
            }
        }

        public static int TaskDurationDaysToTicks(float durationDays)
        {
            // RimWorld uses 60,000 ticks per in-game day.
            int ticks = Mathf.RoundToInt(durationDays * 60000f);

            // Keep a tiny safety delay at 0.0 days so the departure letter
            // (sent after 200 ticks) still appears before the return letter.
            return ticks <= 0 ? 300 : ticks;
        }

        public static float ResolveTaskDurationDaysForQuest()
        {
            if (!RandomTaskDurationEnabled)
                return TaskDurationDays;

            // Uniform discrete random value: 1.0, 1.1, ... 3.0 days.
            return Rand.RangeInclusive(10, 30) / 10f;
        }

        public static float TaskRewardMultiplier
        {
            get
            {
                float value = Settings == null
                    ? 1.0f
                    : Settings.taskRewardMultiplier;

                if (value < 0f)
                    return 0f;
                if (value > 10f)
                    return 10f;

                return Mathf.Round(value * 100f) / 100f;
            }
        }

        public static bool RandomTaskRewardEnabled
        {
            get
            {
                return Settings != null && Settings.randomTaskReward;
            }
        }

        public static float CalculateBaseTaskRewardValue(float durationDays)
        {
            float safeDays = Mathf.Max(0f, durationDays);
            return 500f + safeDays * 300f;
        }

        public static float ResolveTaskRewardValueForQuest(float durationDays)
        {
            float baseReward = CalculateBaseTaskRewardValue(durationDays);

            float multiplier = RandomTaskRewardEnabled
                ? Rand.RangeInclusive(50, 200) / 100f
                : TaskRewardMultiplier;

            return Mathf.Round(baseReward * multiplier);
        }

        public override string SettingsCategory()
        {
            return "LendExpansion";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings == null)
                Settings = GetSettings<LendExpansionSettings>();

            Rect outRect = inRect;
            Rect viewRect = new Rect(
                0f,
                0f,
                inRect.width - 16f,
                SettingsContentHeight);

            Widgets.BeginScrollView(
                outRect,
                ref settingsScrollPosition,
                viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            listing.Label("LendExpansion 设置");
            Text.Font = GameFont.Small;

            listing.GapLine();

            Text.Font = GameFont.Medium;
            listing.Label("任务参数");
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled(
                "开启出借拓展任务",
                ref Settings.enableLendExpansionQuest,
                "默认开启。关闭后将本任务的有效生成权重设为0，不再随机生成新的出借拓展任务；已经生成的任务不受影响。");

            listing.Gap();

            listing.Label(
                "出借拓展任务触发权重：" +
                Settings.questSelectionWeight.ToString("0.0"));
            listing.Label(
                Settings.enableLendExpansionQuest
                    ? "有效权重为上方数值，范围0～30，默认5。权重越高，与其他可生成任务相比时被选中的机会越高。"
                    : "出借拓展任务已关闭；滑条数值会保留，但当前有效权重为0。");

            float questWeightRaw = listing.Slider(
                Settings.questSelectionWeight,
                0f,
                30f);
            Settings.questSelectionWeight =
                Mathf.Round(questWeightRaw * 10f) / 10f;

            // Apply immediately so the next quest-selection pass uses the new value.
            LendExpansionQuestSelectionController.ApplyCurrentSettings();

            listing.Gap();

            if (listing.ButtonText("恢复默认任务权重"))
            {
                Settings.enableLendExpansionQuest = true;
                Settings.questSelectionWeight = 5.0f;

                // Apply the restored quest availability/weight immediately.
                LendExpansionQuestSelectionController.ApplyCurrentSettings();
            }

            listing.GapLine();

            listing.CheckboxLabeled(
                "随机任务时间",
                ref Settings.randomTaskDuration,
                "开启后，每次生成任务时会在1.0～3.0天之间随机任务持续时间。关闭后使用下方固定值。");

            listing.Gap();

            float taskDays = Settings.taskDurationDays;
            listing.Label(
                "固定任务持续时间：" + taskDays.ToString("0.0") + " 天");
            listing.Label(
                Settings.randomTaskDuration
                    ? "当前使用随机任务时间：每次生成任务时在1.0～3.0天之间随机。影响殖民者返回时间，中途信件送达时间和基础奖励。"
                    : "当前使用固定任务时间。范围0.0～5.0天，步进0.1天；影响殖民者返回时间，中途信件送达时间和基础奖励。");

            float taskDaysRaw = listing.Slider(
                Settings.taskDurationDays,
                0f,
                5f);

            // Snap to 0.1-day increments.
            Settings.taskDurationDays =
                Mathf.Round(taskDaysRaw * 10f) / 10f;

            listing.Gap();

            if (listing.ButtonText("恢复默认任务时间"))
            {
                Settings.taskDurationDays = 2.0f;
                Settings.randomTaskDuration = false;
            }

            listing.GapLine();

            listing.CheckboxLabeled(
                "随机任务奖励",
                ref Settings.randomTaskReward,
                "开启后，每次生成任务时会在50%～200%之间随机奖励倍率。关闭后使用下方固定倍率。");

            listing.Gap();

            int rewardMultiplierPercent = Mathf.RoundToInt(
                Settings.taskRewardMultiplier * 100f);
            listing.Label(
                "固定任务奖励倍率：" + rewardMultiplierPercent + "%");

            int fixedBaseReward = Mathf.RoundToInt(
                CalculateBaseTaskRewardValue(Settings.taskDurationDays));
            listing.Label(
                "基础奖励：500 + 任务天数 × 300（当前固定任务时间对应基础奖励："
                + fixedBaseReward + "）");

            listing.Label(
                Settings.randomTaskReward
                    ? "当前使用随机任务奖励：每次生成任务时随机50%～200%倍率；最终奖励 = 基础奖励 × 随机倍率。固定倍率会保留但不参与本次判定。"
                    : "当前使用固定任务奖励倍率。范围0%～1000%，步进1%；最终奖励 = 基础奖励 × 固定倍率。");

            float rewardMultiplierRaw = listing.Slider(
                Settings.taskRewardMultiplier,
                0f,
                10f);

            // Snap reward multiplier to 1% increments.
            Settings.taskRewardMultiplier =
                Mathf.Round(rewardMultiplierRaw * 100f) / 100f;

            listing.Gap();

            if (listing.ButtonText("恢复默认任务奖励"))
            {
                Settings.taskRewardMultiplier = 1.0f;
                Settings.randomTaskReward = false;
            }

            listing.GapLine();

            Text.Font = GameFont.Medium;
            listing.Label("角色日志参数");
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled(
                "显示角色日志",
                ref Settings.enablePawnPlayLog,
                "默认开启。关闭后，本模组不再向角色日志页添加出借任务记录；已经写入的历史日志不会被删除。");

            listing.Gap();

            listing.CheckboxLabeled(
                "随机角色日志数量（3～5）",
                ref Settings.randomPawnPlayLogEntryCount,
                "默认关闭。开启后，每次任务从对应剧情日志池中随机抽取3～5条；关闭后使用下方固定数量。");

            listing.Gap();

            listing.Label(
                "固定中途剧情日志数量：" + Settings.pawnPlayLogEntryCount);
            listing.Label(
                Settings.randomPawnPlayLogEntryCount
                    ? "当前使用随机数量3～5条；固定数量会保留但不参与本次抽取。"
                    : "当前使用固定数量。范围0～8条；0表示只保留任务开始/返回日志，不抽取中途剧情日志。");

            float playLogCountRaw = listing.Slider(
                Settings.pawnPlayLogEntryCount,
                0f,
                8f);
            Settings.pawnPlayLogEntryCount = Mathf.RoundToInt(playLogCountRaw);

            listing.Gap();

            if (listing.ButtonText("恢复默认角色日志参数"))
            {
                Settings.enablePawnPlayLog = true;
                Settings.pawnPlayLogEntryCount = 8;
                Settings.randomPawnPlayLogEntryCount = false;
            }

            listing.GapLine();

            Text.Font = GameFont.Medium;
            listing.Label("返回效果参数");
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled(
                "出借结束后获得对应心情",
                ref Settings.enableReturnMoodEffect,
                "默认开启。关闭后不再获得对应的心情记忆；对委托人的社交记忆不受该开关影响。");

            listing.Gap();

            listing.CheckboxLabeled(
                "随机心情加成倍率（50%～150%）",
                ref Settings.randomReturnMoodEffectMultiplier,
                "默认关闭。开启后每次返回随机一次倍率；关闭后使用下方固定倍率。");

            int moodMultiplierPercent = Mathf.RoundToInt(
                Settings.returnMoodEffectMultiplier * 100f);
            listing.Label("心情加成倍率：" + moodMultiplierPercent + "%");
            listing.Label(
                Settings.randomReturnMoodEffectMultiplier
                    ? "当前使用随机50%～150%倍率；固定倍率会保留但不参与本次判定。"
                    : "当前使用固定倍率。范围0%～500%，步进1%。");

            float moodMultiplierRaw = listing.Slider(
                Settings.returnMoodEffectMultiplier,
                0f,
                5f);
            Settings.returnMoodEffectMultiplier =
                Mathf.Round(moodMultiplierRaw * 100f) / 100f;

            listing.Gap();

            if (listing.ButtonText("恢复默认心情加成设置"))
            {
                Settings.enableReturnMoodEffect = true;
                Settings.returnMoodEffectMultiplier = 1.0f;
                Settings.randomReturnMoodEffectMultiplier = false;
            }

            listing.GapLine();

            listing.CheckboxLabeled(
                "出借结束后获得对应健康状态",
                ref Settings.enableReturnHealthEffect,
                "默认开启。关闭后不再获得剧情对应的临时健康状态。");

            listing.Gap();

            listing.CheckboxLabeled(
                "启用负面健康状态",
                ref Settings.enableNegativeReturnHealthEffects,
                "默认开启。关闭后仍可获得对应健康状态，但该状态不再造成疼痛和移动速度降低。");

            listing.Gap();

            listing.CheckboxLabeled(
                "随机负面健康效果倍率（50%～150%）",
                ref Settings.randomNegativeReturnHealthEffectMultiplier,
                "默认关闭。开启后每次返回随机一次倍率，同时缩放疼痛和移动速度降低；关闭后使用下方固定倍率。");

            int negativeHealthPercent = Mathf.RoundToInt(
                Settings.negativeReturnHealthEffectMultiplier * 100f);
            listing.Label("负面健康效果倍率：" + negativeHealthPercent + "%");
            listing.Label(
                Settings.randomNegativeReturnHealthEffectMultiplier
                    ? "当前使用随机50%～150%倍率；固定倍率会保留但不参与本次判定。"
                    : "当前使用固定倍率。范围0%～500%，步进1%。");

            float negativeHealthRaw = listing.Slider(
                Settings.negativeReturnHealthEffectMultiplier,
                0f,
                5f);
            Settings.negativeReturnHealthEffectMultiplier =
                Mathf.Round(negativeHealthRaw * 100f) / 100f;

            listing.Gap();

            if (listing.ButtonText("恢复默认健康状态设置"))
            {
                Settings.enableReturnHealthEffect = true;
                Settings.enableNegativeReturnHealthEffects = true;
                Settings.negativeReturnHealthEffectMultiplier = 1.0f;
                Settings.randomNegativeReturnHealthEffectMultiplier = false;
            }

            listing.GapLine();

            Text.Font = GameFont.Medium;
            listing.Label("剧情分支参数");
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled(
                "开启异象类事件（触手相关）",
                ref Settings.enableAnomalyEvents,
                "默认开启。开启后，每次任务在皇家/地方委托人确定后，按下方概率判定是否切换为对应势力的异象剧情；异象剧情不会触发年轻殖民者剧情。关闭后异象剧情不会出现。");

            listing.Gap();

            int anomalyPercent = Mathf.RoundToInt(
                Settings.anomalyStoryChance * 100f);

            listing.Label(
                "异象剧情触发概率：" + anomalyPercent + "%");

            listing.Label(
                "仅在异象类事件开关开启时生效。0%表示永不触发，100%表示每次任务都进入对应势力的异象剧情。");

            float anomalyRawValue = listing.Slider(
                Settings.anomalyStoryChance,
                0f,
                1f);

            Settings.anomalyStoryChance =
                Mathf.Round(anomalyRawValue * 100f) / 100f;

            listing.Gap();

            if (listing.ButtonText("异象剧情恢复默认概率（20%）"))
                Settings.anomalyStoryChance = 0.20f;

            listing.Gap();

            listing.CheckboxLabeled(
                "开启年轻殖民者剧情",
                ref Settings.enableYoungStory,
                "默认开启。关闭后，即使登机者符合年轻剧情年龄条件，也始终进入正常剧情；异象剧情优先级仍然最高。");

            listing.Gap();

            int youngStoryPercent = Mathf.RoundToInt(
                Settings.youngStoryTriggerChance * 100f);

            listing.Label(
                "年轻剧情触发概率：" + youngStoryPercent + "%");

            listing.Label(
                Settings.enableYoungStory
                    ? "仅在登机者符合年轻剧情年龄条件、且本次任务没有进入异象剧情时判定一次。默认30%。"
                    : "年轻殖民者剧情当前已关闭；下方概率会保留，但不会参与判定。");

            float youngStoryRaw = listing.Slider(
                Settings.youngStoryTriggerChance,
                0f,
                1f);

            Settings.youngStoryTriggerChance =
                Mathf.Round(youngStoryRaw * 100f) / 100f;

            listing.Gap();

            if (listing.ButtonText("年轻剧情恢复默认概率（30%）"))
                Settings.youngStoryTriggerChance = 0.30f;

            listing.GapLine();

            listing.CheckboxLabeled(
                "正常殖民者怀孕开关",
                ref Settings.enableReturnPregnancy,
                "仅控制最终进入正常剧情分支的登机者。与年轻剧情分支设置互不影响。");

            listing.Gap();

            int percent = Mathf.RoundToInt(
                Settings.returnPregnancyChance * 100f);

            listing.Label(
                "正常殖民者怀孕概率：" + percent + "%");

            listing.Label(
                "仅用于最终进入正常剧情分支的登机者；返回时只判定一次。0%表示永不触发，100%表示必定触发。");

            float rawValue = listing.Slider(
                Settings.returnPregnancyChance,
                0f,
                1f);

            Settings.returnPregnancyChance =
                Mathf.Round(rawValue * 100f) / 100f;

            listing.Gap();

            if (listing.ButtonText("正常殖民者恢复默认概率（15%）"))
                Settings.returnPregnancyChance = 0.15f;

            listing.GapLine();

            listing.CheckboxLabeled(
                "年轻殖民者怀孕开关",
                ref Settings.enableYoungColonistPregnancy,
                "仅控制最终进入年轻剧情分支的登机者。与正常剧情分支设置互不影响。");

            listing.Gap();

            int youngPercent = Mathf.RoundToInt(
                Settings.youngColonistPregnancyChance * 100f);

            listing.Label(
                "年轻殖民者怀孕概率：" + youngPercent + "%");

            listing.Label(
                "仅用于最终进入年轻剧情分支的登机者；返回时只判定一次。0%表示永不触发，100%表示必定触发。");

            float youngRawValue = listing.Slider(
                Settings.youngColonistPregnancyChance,
                0f,
                1f);

            Settings.youngColonistPregnancyChance =
                Mathf.Round(youngRawValue * 100f) / 100f;

            listing.Gap();

            if (listing.ButtonText("年轻殖民者恢复默认概率（10%）"))
                Settings.youngColonistPregnancyChance = 0.10f;

            listing.GapLine();

            string stateText = Settings.enableReturnPregnancy
                ? "正常殖民者：已启用，怀孕概率 "
                  + Mathf.RoundToInt(
                      Settings.returnPregnancyChance * 100f)
                  + "%"
                : "正常殖民者：已关闭怀孕机制";

            listing.Label(stateText);

            string youngStateText =
                Settings.enableYoungColonistPregnancy
                    ? "年轻殖民者：已启用，怀孕概率 "
                      + Mathf.RoundToInt(
                          Settings.youngColonistPregnancyChance * 100f)
                      + "%"
                    : "年轻殖民者：已关闭怀孕机制";

            listing.Label(youngStateText);

            listing.GapLine();
            listing.Gap();

            if (listing.ButtonText("一键恢复所有默认设置"))
            {
                Settings.enableLendExpansionQuest = true;
                Settings.questSelectionWeight = 5.0f;

                Settings.taskDurationDays = 2.0f;
                Settings.randomTaskDuration = false;
                Settings.taskRewardMultiplier = 1.0f;
                Settings.randomTaskReward = false;

                Settings.enablePawnPlayLog = true;
                Settings.pawnPlayLogEntryCount = 8;
                Settings.randomPawnPlayLogEntryCount = false;

                Settings.enableReturnMoodEffect = true;
                Settings.returnMoodEffectMultiplier = 1.0f;
                Settings.randomReturnMoodEffectMultiplier = false;

                Settings.enableReturnHealthEffect = true;
                Settings.enableNegativeReturnHealthEffects = true;
                Settings.negativeReturnHealthEffectMultiplier = 1.0f;
                Settings.randomNegativeReturnHealthEffectMultiplier = false;

                Settings.enableAnomalyEvents = true;
                Settings.anomalyStoryChance = 0.20f;
                Settings.enableYoungStory = true;
                Settings.youngStoryTriggerChance = 0.30f;

                Settings.enableReturnPregnancy = true;
                Settings.returnPregnancyChance = 0.15f;
                Settings.enableYoungColonistPregnancy = true;
                Settings.youngColonistPregnancyChance = 0.10f;

                // Quest enable/weight is runtime-backed, so refresh it immediately.
                LendExpansionQuestSelectionController.ApplyCurrentSettings();
            }

            listing.End();
            Widgets.EndScrollView();
        }
    }
}