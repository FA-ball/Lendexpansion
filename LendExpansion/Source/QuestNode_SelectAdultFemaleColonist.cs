using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace LendExpansion
{
    /// <summary>
    /// 年龄相关参数统一配置区。
    /// 以后调整年龄规则时，优先只修改这里，不需要在整个文件中搜索数字。
    /// </summary>
    public static class LendExpansionAgeSettings
    {
        // [年龄参数 1]
        // 任务生成时的最小年龄筛选参数。
        // 该值会写入 Quest Slate 的 "minAge"，
        // 供任务生成筛选、任务文本和穿梭机 XML 参数共同读取。
        public const int MinAge = 14;

        // [年龄参数 2]
        // 区分“年轻殖民者”与“正常殖民者”的分界阈值。
        // 低于该阈值的合格登机者归类为“年轻殖民者”；
        // 达到或超过该阈值的合格登机者归类为“正常殖民者”。
        public const int NormalColonistCategoryThreshold = 18;

        // [年龄参数 3]
        // 穿梭机登机时的硬编码最低年龄门槛。
        // 这是 C# Harmony 层的兜底限制：
        // 即使 XML/Slate 的 minAge 被误改，低于此值的 Pawn 仍会被拒绝登机。
        public const int ShuttleBoardingMinimumAge = 14;
    }

    /// <summary>
    /// Story-routing constants that are intentionally not player sliders.
    /// </summary>
    /// <summary>
    /// Selects the base commissioner channel. Only Royal and Local exist here.
    /// Anomaly is no longer a third commissioner type; it is rolled afterwards
    /// as an optional story override.
    ///
    /// questDescriptionVariant:
    ///   0/1 = Royal panel A/B
    ///   2/3 = Local panel A/B
    /// </summary>
    public class QuestNode_SelectStoryFaction : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return slate != null;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (slate != null)
                ResolveFactionAndPanel(slate);
        }

        private static void ResolveFactionAndPanel(Slate slate)
        {
            int factionType = Rand.RangeInclusive(0, 1);
            string factionKey = factionType == 0 ? "Royal" : "Local";

            int panelWithinFaction = Rand.RangeInclusive(0, 1);
            int questDescriptionVariant =
                factionType == 0
                    ? panelWithinFaction
                    : 2 + panelWithinFaction;

            slate.Set("storyFactionKey", factionKey);
            slate.Set("storyFactionType", factionType);
            slate.Set("questDescriptionVariant", questDescriptionVariant);
        }
    }

    /// <summary>
    /// After the commissioner has been selected, optionally overrides the quest
    /// with an anomaly storyline. The base Royal/Local identity is preserved and
    /// determines which anomaly line is used.
    /// </summary>
    public class QuestNode_RollAnomalyStory : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            if (slate == null)
                return false;

            ResolveAnomaly(slate);
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (slate != null)
                ResolveAnomaly(slate);
        }

        private static void ResolveAnomaly(Slate slate)
        {
            bool active =
                LendExpansionMod.AnomalyEventsEnabled &&
                Rand.Value < LendExpansionMod.AnomalyStoryChance;

            slate.Set("anomalyStoryActive", active ? 1 : 0);
        }
    }

    public class QuestNode_InitializeAgeSettings : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            if (slate == null)
                return false;

            slate.Set("minAge", LendExpansionAgeSettings.MinAge);
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (slate != null)
                slate.Set("minAge", LendExpansionAgeSettings.MinAge);
        }
    }

    /// <summary>
    /// 将 Mod 设置中的任务持续时间和奖励价值写入 Quest Slate。
    /// XML 后续统一读取 $lendForDays、$returnLentColonistsInTicks 和 $rewardValue。
    /// </summary>
    public class QuestNode_InitializeTaskSettings : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return slate != null;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (slate != null)
                ApplySettingsToSlate(slate);
        }

        private static void ApplySettingsToSlate(Slate slate)
        {
            // Resolve one final duration value for this generation pass, then
            // derive both the displayed days and return ticks from that same value.
            // This prevents the description and return signal from using different rolls.
            float durationDays =
                LendExpansionMod.ResolveTaskDurationDaysForQuest();
            int durationTicks =
                LendExpansionMod.TaskDurationDaysToTicks(durationDays);

            // The mid-trip communication uses the SAME resolved duration as the
            // actual return timer. This keeps the letter proportional to the
            // task length instead of using a fixed XML delay.
            int midCommunicationTicks =
                CalculateMidCommunicationTicks(durationTicks);

            // Reward is based on the resolved task duration. Random reward mode
            // randomizes only the multiplier, independently of duration randomization.
            float rewardValue =
                LendExpansionMod.ResolveTaskRewardValueForQuest(durationDays);

            slate.Set("lendForDays", durationDays);
            slate.Set("returnLentColonistsInTicks", durationTicks);
            slate.Set("midCommunicationTicks", midCommunicationTicks);
            slate.Set("rewardValue", rewardValue);
        }

        private static int CalculateMidCommunicationTicks(int returnTicks)
        {
            // Departure letter is scheduled 200 ticks after boarding.
            // Keep the mid-trip communication at least 50 ticks after that.
            const int earliestMidCommunicationTicks = 250;

            // Also keep the communication at least 50 ticks before return.
            const int returnSafetyGapTicks = 50;

            int latestMidCommunicationTicks =
                returnTicks - returnSafetyGapTicks;

            if (latestMidCommunicationTicks <
                earliestMidCommunicationTicks)
            {
                latestMidCommunicationTicks =
                    earliestMidCommunicationTicks;
            }

            // Default position: halfway through the actual task duration.
            int midCommunicationTicks = returnTicks / 2;

            if (midCommunicationTicks <
                earliestMidCommunicationTicks)
            {
                midCommunicationTicks =
                    earliestMidCommunicationTicks;
            }
            else if (midCommunicationTicks >
                     latestMidCommunicationTicks)
            {
                midCommunicationTicks =
                    latestMidCommunicationTicks;
            }

            return midCommunicationTicks;
        }
    }

    /// <summary>
    /// Quest-generation guard.
    /// The quest is valid only if the selected map has at least one free,
    /// adult female colonist. It does not lock the quest to a specific pawn.
    /// </summary>
    public class QuestNode_SelectAdultFemaleColonist : QuestNode
    {
        public SlateRef<Map> map;
        public SlateRef<int> minAge;

        protected override bool TestRunInt(Slate slate)
        {
            return HasEligiblePawn(slate);
        }

        protected override void RunInt()
        {
            // Validation only. Any eligible adult female may board later.
        }

        private bool HasEligiblePawn(Slate slate)
        {
            Map targetMap = map.GetValue(slate);
            if (targetMap == null)
                return false;

            // minAge comes from Quest Slate and is initialized by
            // QuestNode_InitializeAgeSettings from LendExpansionAgeSettings.MinAge.
            int minimumAge = minAge.GetValue(slate);

            foreach (Pawn pawn in targetMap.mapPawns.FreeColonists)
            {
                if (pawn == null)
                    continue;
                if (pawn.gender != Gender.Female)
                    continue;
                if (!pawn.IsColonist || pawn.IsPrisoner || pawn.IsSlave)
                    continue;
                if (pawn.ageTracker == null)
                    continue;
                if (pawn.ageTracker.AgeBiologicalYears < minimumAge)
                    continue;

                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Marks only the shuttle created by this quest as female-only.
    /// </summary>
    public class QuestNode_MarkFemaleOnlyShuttle : QuestNode
    {
        public SlateRef<Thing> shuttle;

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Thing thing = shuttle.GetValue(slate);

            if (thing == null)
            {
                Log.Warning("[LendExpansion] Could not mark female-only shuttle: shuttle was null.");
                return;
            }

            if (thing.questTags == null)
                thing.questTags = new List<string>();

            if (!thing.questTags.Contains(LendExpansionRuntimePatches.ShuttleTag))
                thing.questTags.Add(LendExpansionRuntimePatches.ShuttleTag);
        }
    }

    /// <summary>
    /// Adds a runtime QuestPart that waits for the shuttle's SentSatisfied signal.
    /// At that point the actual boarded pawn is known.
    /// </summary>
    public class QuestNode_BindBoardedPawnText : QuestNode
    {
        public SlateRef<Thing> shuttle;
        public SlateRef<string> inSignal;

        // v3.8: data needed for return-time gameplay effects.
        public SlateRef<string> returnSignal;
        public SlateRef<int> normalStoryVariant;
        public SlateRef<int> youngStoryVariant;
        public SlateRef<int> anomalyStoryVariant;
        public SlateRef<int> anomalyStoryActive;
        public SlateRef<string> storyFactionKey;
        public SlateRef<Pawn> asker;

        // Runtime age routing. Only one branch is emitted after boarding,
        // and only the matching return branch is emitted on ColonistsReturned.
        public SlateRef<string> normalStoryStartSignal;
        public SlateRef<string> youngStoryStartSignal;
        public SlateRef<string> anomalyStoryStartSignal;
        public SlateRef<string> normalStoryReturnSignal;
        public SlateRef<string> youngStoryReturnSignal;
        public SlateRef<string> anomalyStoryReturnSignal;

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Thing shuttleThing = shuttle.GetValue(slate);
            string signal = inSignal.GetValue(slate);

            if (shuttleThing == null || signal.NullOrEmpty())
            {
                Log.Warning("[LendExpansion] Could not create boarded-pawn binder: missing shuttle or signal.");
                return;
            }

            string returnedSignal = returnSignal.GetValue(slate);

            QuestPart_BindBoardedPawnText part = new QuestPart_BindBoardedPawnText
            {
                shuttle = shuttleThing,
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(signal),
                returnSignal = returnedSignal.NullOrEmpty()
                    ? null
                    : QuestGenUtility.HardcodedSignalWithQuestID(returnedSignal),
                normalStoryVariant = normalStoryVariant.GetValue(slate),
                youngStoryVariant = youngStoryVariant.GetValue(slate),
                anomalyStoryVariant = anomalyStoryVariant.GetValue(slate),
                anomalyStoryActive = anomalyStoryActive.GetValue(slate),
                storyFactionKey = storyFactionKey.GetValue(slate),
                asker = asker.GetValue(slate),
                normalStoryStartSignal = HardcodeOptionalSignal(
                    normalStoryStartSignal.GetValue(slate)),
                youngStoryStartSignal = HardcodeOptionalSignal(
                    youngStoryStartSignal.GetValue(slate)),
                anomalyStoryStartSignal = HardcodeOptionalSignal(
                    anomalyStoryStartSignal.GetValue(slate)),
                normalStoryReturnSignal = HardcodeOptionalSignal(
                    normalStoryReturnSignal.GetValue(slate)),
                youngStoryReturnSignal = HardcodeOptionalSignal(
                    youngStoryReturnSignal.GetValue(slate)),
                anomalyStoryReturnSignal = HardcodeOptionalSignal(
                    anomalyStoryReturnSignal.GetValue(slate))
            };

            QuestGen.quest.AddPart(part);
        }

        private static string HardcodeOptionalSignal(string signal)
        {
            return signal.NullOrEmpty()
                ? null
                : QuestGenUtility.HardcodedSignalWithQuestID(signal);
        }
    }

    /// <summary>
    /// Captures the real pawn in the shuttle when SentSatisfied fires.
    /// Instead of mutating generated QuestPart_Letter data, this registers the
    /// pawn name for the quest's letter parts. Harmony then substitutes {pawn}
    /// at the exact moment SignalArgs.GetFormattedText formats a letter.
    /// </summary>
    public class QuestPart_BindBoardedPawnText : QuestPart
    {
        public Thing shuttle;
        public string inSignal;

        // v3.8: return-time effect context.
        public string returnSignal;
        public int normalStoryVariant;
        public int youngStoryVariant;
        public int anomalyStoryVariant;
        public int anomalyStoryActive;
        public string storyFactionKey;

        // Kept only to recover faction identity from quests saved before storyFactionKey existed.
        private int legacyIsRoyal;
        public Pawn asker;

        public string normalStoryStartSignal;
        public string youngStoryStartSignal;
        public string anomalyStoryStartSignal;
        public string normalStoryReturnSignal;
        public string youngStoryReturnSignal;
        public string anomalyStoryReturnSignal;

        private Pawn capturedPawn;
        private bool captured;
        private bool returnEffectsApplied;
        private int boardingTick = -1;

        // Play-log state. Story letters are counted only when their own signal fires.
        // The second fired story letter is the mid-task communication.
        private int triggeredStoryLettersAfterBoarding;
        private bool departurePlayLogAdded;
        private bool midPlayLogsAdded;
        private bool returnPlayLogAdded;

        // Biological age is locked at boarding. Being below the category threshold
        // only makes the pawn eligible for the young storyline; the actual storyline
        // is rolled once and persisted.
        private int boardedBiologicalAge = -1;
        private bool youngStoryBranchResolved;
        private bool youngStoryBranchTriggered;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            // First stage: remember the actual pawn that boarded.
            if (!captured && signal.tag == inSignal)
            {
                Pawn pawn = GetBoardedPawn();
                if (pawn == null)
                {
                    Log.Warning(
                        "[LendExpansion] SentSatisfied received, but no boarded pawn " +
                        "was found in the shuttle.");
                    return;
                }

                capturedPawn = pawn;
                boardingTick = Find.TickManager == null
                    ? -1
                    : Find.TickManager.TicksGame;
                boardedBiologicalAge =
                    pawn.ageTracker == null
                        ? -1
                        : pawn.ageTracker.AgeBiologicalYears;
                captured = true;

                ResolveYoungStoryBranchAtBoarding();

                RegisterRuntimeBindings();
                AddDeparturePlayLog();

                // Runtime fork is now probability-based for age-eligible pawns.
                // If the young roll does not trigger, the pawn continues through
                // the normal storyline even though her boarding age was below the threshold.
                SendAgeStorySignal(
                    AnomalyStoryActive
                        ? anomalyStoryStartSignal
                        : (YoungColonistBranchAtBoarding
                            ? youngStoryStartSignal
                            : normalStoryStartSignal),
                    "start");

                return;
            }

            // Second stage: route the matching return story and then apply gameplay effects.
            if (captured &&
                !returnEffectsApplied &&
                !returnSignal.NullOrEmpty() &&
                signal.tag == returnSignal)
            {
                SendAgeStorySignal(
                    AnomalyStoryActive
                        ? anomalyStoryReturnSignal
                        : (YoungColonistBranchAtBoarding
                            ? youngStoryReturnSignal
                            : normalStoryReturnSignal),
                    "return");

                // Story-log entries are written at return with synthetic creation
                // times distributed evenly across the actual elapsed task duration.
                AddMidStoryPlayLogs();
                AddReturnPlayLog();
                ApplyReturnEffects();
            }
        }

        private void ResolveYoungStoryBranchAtBoarding()
        {
            // Anomaly overrides every age branch. It is decided at quest generation
            // after the Royal/Local commissioner has already been selected.
            if (AnomalyStoryActive)
            {
                youngStoryBranchTriggered = false;
                youngStoryBranchResolved = true;
                return;
            }

            if (!LendExpansionMod.YoungStoryEnabled)
            {
                youngStoryBranchTriggered = false;
                youngStoryBranchResolved = true;
                return;
            }

            bool ageEligible =
                boardedBiologicalAge >= 0 &&
                boardedBiologicalAge <
                    LendExpansionAgeSettings.NormalColonistCategoryThreshold;

            float chance = LendExpansionMod.YoungStoryTriggerChance;

            if (ageEligible && chance > 0f)
            {
                youngStoryBranchTriggered =
                    chance >= 1f || Rand.Value < chance;
            }
            else
            {
                youngStoryBranchTriggered = false;
            }

            youngStoryBranchResolved = true;
        }

        private void SendAgeStorySignal(string outSignal, string stage)
        {
            if (outSignal.NullOrEmpty())
            {
                Log.Warning(
                    "[LendExpansion] Age story route skipped: missing " +
                    stage + " signal.");
                return;
            }

            try
            {
                Find.SignalManager.SendSignal(
                    new Signal(outSignal, default(SignalArgs)));

            }
            catch (Exception ex)
            {
                Log.Error(
                    "[LendExpansion] Failed to emit age story " +
                    stage + " signal: " + ex);
            }
        }

        private void RegisterRuntimeBindings()
        {
            if (capturedPawn == null)
                return;

            Quest owningQuest = FindOwningQuest();
            if (owningQuest == null)
                return;

            PawnTokenRuntime.RegisterQuestLetters(
                owningQuest,
                capturedPawn,
                YoungColonistBranchAtBoarding,
                this);

        }

        public void NotifyMappedLetterTriggered()
        {
            if (!captured || capturedPawn == null || returnEffectsApplied)
                return;

            triggeredStoryLettersAfterBoarding++;

            // Stage order for the selected branch is: departure, mid-task, return.
            // Story PlayLog entries are deferred until return so their displayed
            // timestamps can be spread across the full actual task duration.
        }

        private string CurrentStoryProfileId
        {
            get
            {
                int variant = AnomalyStoryActive
                    ? anomalyStoryVariant
                    : (YoungColonistBranchAtBoarding
                        ? youngStoryVariant
                        : normalStoryVariant);

                string category = AnomalyStoryActive
                    ? "Anomaly"
                    : (YoungColonistBranchAtBoarding ? "Young" : "Normal");

                return category + "_" + ResolvedStoryFactionKey + "_" + variant;
            }
        }

        private void AddDeparturePlayLog()
        {
            if (departurePlayLogAdded || capturedPawn == null)
                return;

            string factionName = asker != null && asker.Faction != null
                ? asker.Faction.Name
                : ResolvedStoryFactionKey;

            LendExpansionPlayLog.Add(
                capturedPawn,
                "{pawn}接受了来自" + factionName + "的出借任务，并离开了殖民地。");

            departurePlayLogAdded = true;
        }

        private void AddMidStoryPlayLogs()
        {
            if (midPlayLogsAdded || capturedPawn == null)
                return;

            if (!LendExpansionMod.PawnPlayLogEnabled)
            {
                midPlayLogsAdded = true;
                return;
            }

            // Adult mid-story log pools are only surfaced for adult pawns.
            if (capturedPawn.ageTracker != null &&
                capturedPawn.ageTracker.AgeBiologicalYears < 18)
            {
                midPlayLogsAdded = true;
                return;
            }

            string[] pool = LendExpansionStoryLogPools.GetPool(CurrentStoryProfileId);
            if (pool == null || pool.Length == 0)
            {
                midPlayLogsAdded = true;
                return;
            }

            int wanted = LendExpansionMod.ResolvePawnPlayLogEntryCount();
            wanted = Math.Min(wanted, pool.Length);

            if (wanted <= 0)
            {
                midPlayLogsAdded = true;
                return;
            }

            List<int> indices = new List<int>();
            for (int i = 0; i < pool.Length; i++)
                indices.Add(i);

            // Shuffle only the prefix we need, preserving distinct selection.
            for (int i = 0; i < wanted; i++)
            {
                int pick = Rand.RangeInclusive(i, indices.Count - 1);
                int tmp = indices[i];
                indices[i] = indices[pick];
                indices[pick] = tmp;
            }

            // The random draw decides WHICH pool entries are used, but their
            // narrative order always follows their original position in the pool.
            // Example: drawing 2, 5, 6, 7 is displayed as 2 -> 5 -> 6 -> 7.
            indices.Sort(0, wanted, Comparer<int>.Default);

            int nowTick = Find.TickManager == null
                ? -1
                : Find.TickManager.TicksGame;

            int elapsedTicks = 0;
            if (boardingTick >= 0 && nowTick >= boardingTick)
                elapsedTicks = nowTick - boardingTick;

            // Compatibility fallback for quests created before boardingTick existed.
            if (elapsedTicks <= 0)
                elapsedTicks = Math.Max(1, LendExpansionMod.TaskDurationTicks);

            // Evenly distribute N story events between departure and return.
            // Example for four logs: 20%, 40%, 60%, 80% through the task.
            for (int i = 0; i < wanted; i++)
            {
                long eventOffset =
                    (long)elapsedTicks * (i + 1) / (wanted + 1);
                int ageTicks = elapsedTicks - (int)eventOffset;

                LendExpansionPlayLog.Add(
                    capturedPawn,
                    pool[indices[i]],
                    ageTicks);
            }

            midPlayLogsAdded = true;
        }

        private void AddReturnPlayLog()
        {
            if (returnPlayLogAdded || capturedPawn == null)
                return;

            LendExpansionPlayLog.Add(
                capturedPawn,
                "{pawn}完成了出借任务并返回殖民地。");

            returnPlayLogAdded = true;
        }

        private static void DebugReturnLog(string message)
        {
            // Return-effect diagnostics intentionally remain warnings/errors only.
            // Routine white Log.Message output is disabled.
            Log.Warning("[LendExpansion v3.10] " + message);
        }

        private static int? TryGetOpinion(Pawn pawn, Pawn otherPawn)
        {
            if (pawn == null || otherPawn == null || pawn.relations == null)
                return null;

            try
            {
                return pawn.relations.OpinionOf(otherPawn);
            }
            catch (Exception ex)
            {
                DebugReturnLog(
                    "Could not read OpinionOf(" +
                    otherPawn.LabelShortCap.ToString() + "): " +
                    ex.Message);
                return null;
            }
        }

        private static int CountSocialMemories(
            Pawn pawn,
            ThoughtDef thoughtDef,
            Pawn otherPawn)
        {
            if (pawn == null ||
                thoughtDef == null ||
                otherPawn == null ||
                pawn.needs == null ||
                pawn.needs.mood == null ||
                pawn.needs.mood.thoughts == null ||
                pawn.needs.mood.thoughts.memories == null)
            {
                return 0;
            }

            try
            {
                object handler = pawn.needs.mood.thoughts.memories;

                PropertyInfo memoriesProperty = handler.GetType().GetProperty(
                    "Memories",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (memoriesProperty == null)
                    return -1;

                System.Collections.IEnumerable memories =
                    memoriesProperty.GetValue(handler, null)
                    as System.Collections.IEnumerable;

                if (memories == null)
                    return -1;

                int count = 0;

                foreach (object obj in memories)
                {
                    Thought_Memory memory = obj as Thought_Memory;
                    if (memory == null || memory.def != thoughtDef)
                        continue;

                    Pawn memoryOtherPawn = null;

                    FieldInfo otherPawnField = memory.GetType().GetField(
                        "otherPawn",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                    if (otherPawnField == null)
                    {
                        Type baseType = memory.GetType().BaseType;
                        while (baseType != null && otherPawnField == null)
                        {
                            otherPawnField = baseType.GetField(
                                "otherPawn",
                                BindingFlags.Instance |
                                BindingFlags.Public |
                                BindingFlags.NonPublic);
                            baseType = baseType.BaseType;
                        }
                    }

                    if (otherPawnField != null)
                        memoryOtherPawn = otherPawnField.GetValue(memory) as Pawn;

                    PropertyInfo otherPawnProperty = memory.GetType().GetProperty(
                        "OtherPawn",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                    if (memoryOtherPawn == null && otherPawnProperty != null)
                        memoryOtherPawn =
                            otherPawnProperty.GetValue(memory, null) as Pawn;

                    if (memoryOtherPawn == otherPawn)
                        count++;
                }

                return count;
            }
            catch (Exception ex)
            {
                DebugReturnLog(
                    "Could not inspect social memories: " + ex.Message);
                return -1;
            }
        }

        private void ApplyReturnEffects()
        {
            Pawn pawn = capturedPawn;

            if (pawn == null || pawn.Dead)
            {
                DebugReturnLog(
                    "Return effects skipped because captured pawn was null or dead.");
                return;
            }

            int effectiveStoryVariant =
                AnomalyStoryActive
                    ? anomalyStoryVariant
                    : (YoungColonistBranchAtBoarding
                        ? youngStoryVariant
                        : normalStoryVariant);

            string storyCategoryToken =
                AnomalyStoryActive
                    ? "Anomaly"
                    : (YoungColonistBranchAtBoarding ? "Young" : "Normal");
            string factionToken = ResolvedStoryFactionKey;
            string effectProfileId =
                storyCategoryToken + "_" +
                factionToken + "_" +
                effectiveStoryVariant;

            // Return effects use a naming convention instead of a hard-coded
            // switch. Both age categories are peers: either side can add variant
            // 3/4/5/etc. simply by adding matching XML story branches and Defs.
            string moodDefName =
                "LE_LendReturnMood_" + effectProfileId;
            string socialDefName =
                "LE_LendReturnSocial_" + effectProfileId;
            string hediffDefName =
                "LE_LendReturnHediff_" + effectProfileId;

            DebugReturnLog(
                "Return effect profile=" + effectProfileId +
                ", mood=" + moodDefName +
                ", social=" + socialDefName +
                ", hediff=" + hediffDefName + ".");

            if (LendExpansionMod.ReturnMoodEffectEnabled)
            {
                float moodMultiplier =
                    LendExpansionMod.ResolveReturnMoodEffectMultiplier();
                TryGiveMemory(pawn, moodDefName, null);
                TrySetMoodMemoryMultiplier(pawn, moodDefName, moodMultiplier);
                DebugReturnLog(
                    "Mood memory requested: " + moodDefName +
                    ", multiplier=" + moodMultiplier.ToString("0.00") + ".");
            }
            else
            {
                DebugReturnLog("Mood memory skipped by mod setting.");
            }

            if (LendExpansionMod.ReturnHealthEffectEnabled)
            {
                float negativeHealthMultiplier =
                    LendExpansionMod.ResolveNegativeReturnHealthEffectMultiplier();
                TryAddHediff(
                    pawn,
                    hediffDefName,
                    negativeHealthMultiplier);
                DebugReturnLog(
                    "Hediff requested: " + hediffDefName +
                    ", negativeMultiplier=" +
                    negativeHealthMultiplier.ToString("0.00") + ".");
            }
            else
            {
                DebugReturnLog("Return health state skipped by mod setting.");
            }

            if (asker != null && asker != pawn)
            {
                ThoughtDef socialDef =
                    DefDatabase<ThoughtDef>.GetNamedSilentFail(socialDefName);

                int? opinionBefore = TryGetOpinion(pawn, asker);
                int memoriesBefore =
                    CountSocialMemories(pawn, socialDef, asker);

                DebugReturnLog(
                    "Social effect before: def=" + socialDefName +
                    ", opinion=" +
                    (opinionBefore.HasValue
                        ? opinionBefore.Value.ToString()
                        : "<unavailable>") +
                    ", matchingMemories=" + memoriesBefore);

                TryGiveMemory(pawn, socialDefName, asker);

                int? opinionAfter = TryGetOpinion(pawn, asker);
                int memoriesAfter =
                    CountSocialMemories(pawn, socialDef, asker);

                DebugReturnLog(
                    "Social effect after: pawn=" +
                    pawn.LabelShortCap.ToString() +
                    ", asker=" +
                    asker.LabelShortCap.ToString() +
                    ", opinion=" +
                    (opinionAfter.HasValue
                        ? opinionAfter.Value.ToString()
                        : "<unavailable>") +
                    ", delta=" +
                    (opinionBefore.HasValue && opinionAfter.HasValue
                        ? (opinionAfter.Value - opinionBefore.Value).ToString()
                        : "<unavailable>") +
                    ", matchingMemories=" + memoriesAfter);

                if (opinionBefore.HasValue &&
                    opinionAfter.HasValue &&
                    opinionAfter.Value == opinionBefore.Value)
                {
                    DebugReturnLog(
                        "WARNING: social memory was requested but OpinionOf did " +
                        "not change. The log above will show whether the memory " +
                        "was actually stored.");
                }
            }
            else
            {
                DebugReturnLog(
                    "Social effect skipped because asker is " +
                    (asker == null ? "<null>" : "the same pawn") + ".");
            }


            // v3.10: roll once for a real RimWorld pregnancy on return.
            TryApplyReturnPregnancy(pawn);

            returnEffectsApplied = true;

            DebugReturnLog(
                "Return effects completed for " +
                pawn.LabelShortCap.ToString() + ".");
        }

        private void TryApplyReturnPregnancy(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                DebugReturnLog(
                    "Pregnancy skipped because pawn was null, dead, or had no health tracker.");
                return;
            }

            if (pawn.gender != Gender.Female)
            {
                DebugReturnLog(
                    "Pregnancy skipped because returned pawn is not female: " +
                    pawn.LabelShortCap.ToString());
                return;
            }

            int ageForPregnancyBranch = boardedBiologicalAge;

            // Pregnancy settings follow the storyline that was locked at boarding.
            // This keeps the two age channels internally consistent when an
            // age-eligible pawn fails the young-story probability roll.
            bool useYoungColonistSettings = YoungColonistBranchAtBoarding;

            bool pregnancyEnabled = useYoungColonistSettings
                ? LendExpansionMod.YoungColonistPregnancyEnabled
                : LendExpansionMod.PregnancyEnabled;

            float pregnancyChance = useYoungColonistSettings
                ? LendExpansionMod.YoungColonistPregnancyChance
                : LendExpansionMod.PregnancyChance;

            if (!pregnancyEnabled)
            {
                DebugReturnLog(
                    "Pregnancy roll skipped because the " +
                    (useYoungColonistSettings
                        ? "young-colonist"
                        : "normal-colonist") +
                    " pregnancy feature is disabled in mod settings.");
                return;
            }

            float roll = Rand.Value;
            bool passed = roll < pregnancyChance;

            DebugReturnLog(
                "Pregnancy roll: pawn=" + pawn.LabelShortCap.ToString() +
                ", boardedAge=" + ageForPregnancyBranch +
                ", settingsBranch=" +
                (useYoungColonistSettings ? "young" : "normal") +
                ", chance=" + (pregnancyChance * 100f).ToString("0") + "%" +
                ", roll=" + roll.ToString("0.0000") +
                ", passed=" + passed + ".");

            if (!passed)
            {
                DebugReturnLog("Pregnancy roll failed; no pregnancy added.");
                return;
            }

            HediffDef pregnancyDef = FindHumanPregnancyDef();
            if (pregnancyDef == null)
            {
                DebugReturnLog(
                    "Pregnancy roll passed, but no loaded human pregnancy HediffDef " +
                    "could be found. Biotech/pregnancy content may be unavailable.");
                return;
            }

            DebugReturnLog(
                "Pregnancy HediffDef selected: " + pregnancyDef.defName + ".");

            try
            {
                if (pawn.health.hediffSet.HasHediff(pregnancyDef))
                {
                    DebugReturnLog(
                        pawn.LabelShortCap.ToString() +
                        " is already pregnant; no duplicate pregnancy was added.");
                    return;
                }

                // Use RimWorld's actual loaded pregnancy HediffDef.
                pawn.health.AddHediff(pregnancyDef);

                Hediff pregnancy = FindPregnancyHediffOnPawn(pawn, pregnancyDef);
                if (pregnancy == null)
                {
                    DebugReturnLog(
                        "Pregnancy HediffDef was added, but the new pregnancy Hediff " +
                        "could not be located afterwards.");
                    return;
                }

                // The story does not identify a biological father. Keep father null
                // rather than incorrectly assigning the quest giver. RimWorld supports
                // pregnancies whose father is not known/available.
                bool parentsInitialized =
                    TryInitializePregnancyParents(pregnancy, pawn, null);

                // Show a player-facing notification only after the pregnancy Hediff
                // has been successfully created and verified on the returned pawn.
                TryShowPregnancyLetter(pawn);

                DebugReturnLog(
                    "Pregnancy added successfully: pawn=" +
                    pawn.LabelShortCap.ToString() +
                    ", hediff=" + pregnancy.def.defName +
                    ", father=<unknown>" +
                    ", SetParentsInvoked=" + parentsInitialized + ".");
            }
            catch (Exception ex)
            {
                DebugReturnLog(
                    "Failed to add return pregnancy: " + ex);
            }
        }

        private static void TryShowPregnancyLetter(Pawn pawn)
        {
            if (pawn == null)
                return;

            try
            {
                string pawnName = pawn.LabelShortCap.ToString();

                TaggedString label =
                    pawnName + "已经怀孕";

                // Three equally weighted letter bodies.
                // The random choice is made only once when the letter is created.
                TaggedString letterText;
                int letterVariant = Rand.RangeInclusive(0, 2);

                if (letterVariant == 0)
                {
                    letterText =
                        "在经历了那次任务之后，少女小腹的弧度不但没有因为体内精液的排出而恢复，反而是一天比一天更加高耸，终于有一天，即使是最宽松的衣物也再也无法遮掩住她的腹部，殖民者最终被强行拉去做了检查。结果也是不出所料，这位少女成功地怀上了一个新的生命。\n\n" +
                        "由于这次任务期间没有留下能够确认生父身份的可靠记录，" +
                        "目前无法确定孩子的生父。\n\n" +
                        "她已经被告知检查结果。你可以选中她，并在健康面板中查看当前的妊娠状态。";
                }
                else if (letterVariant == 1)
                {
                    letterText =
                        "在任务结束之后，生活垃圾里面不知为何反复地出现了好几根验孕棒，正当殖民地里面的其他人还在互相猜测的时候，殖民者颤抖着举着一根带有两根红线的验孕棒，敲响了你的房门...\n\n" +
                        "从现有记录来看，无法确认孩子生父的身份。" +
                        "这次意外的结果或许会给她之后的生活带来一些新的变化。\n\n" +
                        "你可以选中她，并在健康面板中查看当前的妊娠状态。";
                }
                else
                {
                    letterText =
                        "结束了那次任务之后，殖民者发现自己的月经不知何时已经停止了，而且肚子也膨胀得越来越大。在犹豫了好久之后，眼瞅着事情已经隐瞒不下去的她最终还是选择向殖民地的其他人公开了这个消息...\n\n" +
                        "由于任务期间的相关记录并不完整，孩子的生父目前仍然无法确认。" +
                        "这个消息似乎也让她一时不知道该如何反应。\n\n" +
                        "你可以选中她，并在健康面板中查看当前的妊娠状态。";
                }

                Find.LetterStack.ReceiveLetter(
                    label,
                    letterText,
                    LetterDefOf.PositiveEvent,
                    new LookTargets(pawn));

                DebugReturnLog(
                    "Pregnancy letter shown for " +
                    pawnName +
                    ", variant=" + letterVariant + ".");
            }
            catch (Exception ex)
            {
                // A letter failure must never undo or interfere with the
                // pregnancy that was already applied successfully.
                DebugReturnLog(
                    "Failed to show pregnancy letter: " + ex);
            }
        }

        private static HediffDef FindHumanPregnancyDef()
        {
            try
            {
                Type pregnantType = typeof(Hediff).Assembly.GetType(
                    "Verse.Hediff_Pregnant",
                    false);

                if (pregnantType == null)
                {
                    DebugReturnLog(
                        "Verse.Hediff_Pregnant type was not found.");
                    return null;
                }

                List<HediffDef> candidates =
                    DefDatabase<HediffDef>.AllDefsListForReading
                        .Where(def =>
                            def != null &&
                            def.hediffClass != null &&
                            pregnantType.IsAssignableFrom(def.hediffClass))
                        .ToList();

                if (candidates.Count == 0)
                    return null;

                // Prefer a def that explicitly looks human-specific.
                HediffDef preferred = candidates.FirstOrDefault(def =>
                    def.defName != null &&
                    (def.defName.IndexOf(
                        "Human",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                     def.defName.IndexOf(
                        "Pregnan",
                        StringComparison.OrdinalIgnoreCase) >= 0));

                if (preferred != null)
                    return preferred;

                return candidates[0];
            }
            catch (Exception ex)
            {
                DebugReturnLog(
                    "Failed while locating pregnancy HediffDef: " + ex);
                return null;
            }
        }

        private static Hediff FindPregnancyHediffOnPawn(
            Pawn pawn,
            HediffDef pregnancyDef)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.hediffSet == null ||
                pregnancyDef == null)
            {
                return null;
            }

            try
            {
                // Avoid relying on a public hediff-list member. Ask the HediffSet
                // for its first matching Hediff through reflection so minor 1.6 API
                // accessibility differences do not break compilation.
                MethodInfo method = pawn.health.hediffSet.GetType().GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "GetFirstHediffOfDef")
                            return false;

                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length >= 1 &&
                            parameters[0].ParameterType == typeof(HediffDef);
                    });

                if (method != null)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    object[] args = new object[parameters.Length];
                    args[0] = pregnancyDef;

                    for (int i = 1; i < args.Length; i++)
                    {
                        Type t = parameters[i].ParameterType;
                        args[i] = t.IsValueType
                            ? Activator.CreateInstance(t)
                            : null;
                    }

                    return method.Invoke(
                        pawn.health.hediffSet,
                        args) as Hediff;
                }
            }
            catch (Exception ex)
            {
                DebugReturnLog(
                    "Could not retrieve pregnancy Hediff after adding it: " +
                    ex.Message);
            }

            return null;
        }

        private static bool TryInitializePregnancyParents(
            Hediff pregnancy,
            Pawn mother,
            Pawn father)
        {
            if (pregnancy == null || mother == null)
                return false;

            try
            {
                MethodInfo[] methods = pregnancy.GetType().GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "SetParents")
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0)
                        continue;

                    object[] args = new object[parameters.Length];
                    int unnamedPawnIndex = 0;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo parameter = parameters[i];
                        Type parameterType = parameter.ParameterType;
                        string name = parameter.Name ?? "";

                        if (typeof(Pawn).IsAssignableFrom(parameterType))
                        {
                            if (name.IndexOf(
                                    "mother",
                                    StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                args[i] = mother;
                            }
                            else if (name.IndexOf(
                                         "father",
                                         StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                args[i] = father;
                            }
                            else
                            {
                                args[i] = unnamedPawnIndex++ == 0
                                    ? mother
                                    : father;
                            }

                            continue;
                        }

                        // GeneSet, XenotypeDef and other reference-type context can
                        // safely remain unknown for this test pregnancy. Value types
                        // receive their default value.
                        args[i] = parameterType.IsValueType
                            ? Activator.CreateInstance(parameterType)
                            : null;
                    }

                    method.Invoke(pregnancy, args);
                    return true;
                }

                DebugReturnLog(
                    "Pregnancy Hediff has no SetParents method; pregnancy remains " +
                    "without explicit parent initialization.");
            }
            catch (Exception ex)
            {
                DebugReturnLog(
                    "SetParents initialization failed: " + ex.Message);
            }

            return false;
        }

        private static void TryGiveMemory(
            Pawn pawn,
            string thoughtDefName,
            Pawn otherPawn)
        {
            if (pawn == null ||
                pawn.needs == null ||
                pawn.needs.mood == null ||
                pawn.needs.mood.thoughts == null ||
                pawn.needs.mood.thoughts.memories == null)
            {
                DebugReturnLog(
                    "Memory skipped because pawn mood-memory handler is unavailable: " +
                    thoughtDefName);
                return;
            }

            ThoughtDef thoughtDef =
                DefDatabase<ThoughtDef>.GetNamedSilentFail(thoughtDefName);

            if (thoughtDef == null)
            {
                DebugReturnLog(
                    "ThoughtDef not found: " + thoughtDefName);
                return;
            }

            try
            {
                if (otherPawn == null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                }
                else
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(
                        thoughtDef,
                        otherPawn);
                }

                DebugReturnLog(
                    "TryGainMemory completed: " + thoughtDefName +
                    (otherPawn == null
                        ? ""
                        : ", otherPawn=" +
                          otherPawn.LabelShortCap.ToString()));
            }
            catch (Exception ex)
            {
                DebugReturnLog(
                    "Failed to give memory " +
                    thoughtDefName + ": " + ex);
            }
        }

        private static void TrySetMoodMemoryMultiplier(
            Pawn pawn,
            string thoughtDefName,
            float multiplier)
        {
            if (pawn == null ||
                pawn.needs == null ||
                pawn.needs.mood == null ||
                pawn.needs.mood.thoughts == null ||
                pawn.needs.mood.thoughts.memories == null)
            {
                return;
            }

            ThoughtDef thoughtDef =
                DefDatabase<ThoughtDef>.GetNamedSilentFail(thoughtDefName);
            if (thoughtDef == null)
                return;

            multiplier = Math.Max(0f, Math.Min(5f, multiplier));

            try
            {
                object handler = pawn.needs.mood.thoughts.memories;
                PropertyInfo memoriesProperty = handler.GetType().GetProperty(
                    "Memories",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                System.Collections.IEnumerable memories =
                    memoriesProperty == null
                        ? null
                        : memoriesProperty.GetValue(handler, null)
                            as System.Collections.IEnumerable;

                if (memories == null)
                    return;

                foreach (object obj in memories)
                {
                    Thought_Memory memory = obj as Thought_Memory;
                    if (memory == null || memory.def != thoughtDef)
                        continue;

                    FieldInfo factorField = null;
                    Type type = memory.GetType();
                    while (type != null && factorField == null)
                    {
                        factorField = type.GetField(
                            "moodPowerFactor",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);
                        type = type.BaseType;
                    }

                    if (factorField != null && factorField.FieldType == typeof(float))
                    {
                        factorField.SetValue(memory, multiplier);
                        return;
                    }

                    PropertyInfo factorProperty = memory.GetType().GetProperty(
                        "MoodPowerFactor",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                    if (factorProperty != null &&
                        factorProperty.CanWrite &&
                        factorProperty.PropertyType == typeof(float))
                    {
                        factorProperty.SetValue(memory, multiplier, null);
                        return;
                    }
                }

                DebugReturnLog(
                    "Mood memory multiplier field was not found for " +
                    thoughtDefName + ".");
            }
            catch (Exception ex)
            {
                DebugReturnLog(
                    "Failed to scale mood memory " +
                    thoughtDefName + ": " + ex.Message);
            }
        }

        private static void TryAddHediff(
            Pawn pawn,
            string hediffDefName,
            float negativeEffectMultiplier)
        {
            if (pawn == null || pawn.health == null)
                return;

            HediffDef hediffDef =
                DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);

            if (hediffDef == null)
            {
                DebugReturnLog(
                    "HediffDef not found: " + hediffDefName);
                return;
            }

            negativeEffectMultiplier =
                Math.Max(0f, Math.Min(5f, negativeEffectMultiplier));

            try
            {
                Hediff hediff = LendExpansionReturnEffectScaling
                    .FindReturnHediff(pawn, hediffDef);

                if (hediff == null)
                {
                    pawn.health.AddHediff(hediffDef);
                    hediff = LendExpansionReturnEffectScaling
                        .FindReturnHediff(pawn, hediffDef);
                }

                if (hediff != null)
                {
                    // Severity 1..6 encodes a 0..5x negative-effect multiplier.
                    // Old saves with severity below 1 are interpreted as legacy 1x.
                    hediff.Severity = 1f + negativeEffectMultiplier;
                }
            }
            catch (Exception ex)
            {
                DebugReturnLog(
                    "Failed to add hediff " +
                    hediffDefName + ": " + ex);
            }
        }

        public bool AnomalyStoryActive
        {
            get
            {
                if (anomalyStoryActive == 1)
                    return true;

                // Compatibility with the previous development build where
                // Anomaly was stored directly as storyFactionKey.
                return string.Equals(
                    storyFactionKey,
                    "Anomaly",
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool YoungColonistBranchAtBoarding
        {
            get
            {
                if (youngStoryBranchResolved)
                    return youngStoryBranchTriggered;

                // Compatibility fallback for quests saved before the probability
                // branch fields existed: preserve the old deterministic age behavior.
                int age = boardedBiologicalAge;

                if (age < 0 &&
                    capturedPawn != null &&
                    capturedPawn.ageTracker != null)
                {
                    age = capturedPawn.ageTracker.AgeBiologicalYears;
                }

                return age >= 0 &&
                    age < LendExpansionAgeSettings.NormalColonistCategoryThreshold;
            }
        }

        private string ResolvedStoryFactionKey
        {
            get
            {
                if (!storyFactionKey.NullOrEmpty())
                {
                    if (string.Equals(
                            storyFactionKey,
                            "Anomaly",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return legacyIsRoyal == 1 ? "Royal" : "Local";
                    }

                    return storyFactionKey;
                }

                // Old-save compatibility only. New quests always use storyFactionKey.
                return legacyIsRoyal == 1 ? "Royal" : "Local";
            }
        }

        public Pawn CapturedPawn
        {
            get
            {
                if (!captured)
                    return null;

                return capturedPawn;
            }
        }

        public string CapturedPawnName
        {
            get
            {
                Pawn pawn = CapturedPawn;
                return pawn == null ? null : pawn.LabelShortCap.ToString();
            }
        }

        private Pawn GetBoardedPawn()
        {
            if (shuttle == null)
                return null;

            CompShuttle comp = shuttle.TryGetComp<CompShuttle>();
            if (comp == null)
                return null;

            try
            {
                PropertyInfo property = typeof(CompShuttle).GetProperty(
                    "ContainedPawns",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (property != null)
                {
                    Pawn pawn = FirstPawn(property.GetValue(comp, null));
                    if (pawn != null)
                        return pawn;
                }

                MethodInfo getter = typeof(CompShuttle).GetMethod(
                    "get_ContainedPawns",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (getter != null)
                {
                    Pawn pawn = FirstPawn(getter.Invoke(comp, null));
                    if (pawn != null)
                        return pawn;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[LendExpansion] Failed to read boarded pawn from CompShuttle: " + ex);
            }

            return null;
        }

        private static Pawn FirstPawn(object value)
        {
            IEnumerable<Pawn> pawnEnumerable = value as IEnumerable<Pawn>;
            if (pawnEnumerable != null)
                return pawnEnumerable.FirstOrDefault();

            System.Collections.IEnumerable enumerable =
                value as System.Collections.IEnumerable;

            if (enumerable != null)
            {
                foreach (object obj in enumerable)
                {
                    Pawn pawn = obj as Pawn;
                    if (pawn != null)
                        return pawn;
                }
            }

            return null;
        }

        private Quest FindOwningQuest()
        {
            if (Find.QuestManager == null)
                return null;

            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (quest == null || quest.PartsListForReading == null)
                    continue;

                if (quest.PartsListForReading.Contains(this))
                    return quest;
            }

            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref returnSignal, "returnSignal");
            Scribe_Values.Look(ref normalStoryVariant, "storyVariant", 0);
            Scribe_Values.Look(
                ref youngStoryVariant,
                "youngStoryVariant",
                0);
            Scribe_Values.Look(
                ref anomalyStoryVariant,
                "anomalyStoryVariant",
                0);
            Scribe_Values.Look(
                ref anomalyStoryActive,
                "anomalyStoryActive",
                0);
            Scribe_Values.Look(ref storyFactionKey, "storyFactionKey");
            Scribe_Values.Look(ref legacyIsRoyal, "isRoyal", 0);
            Scribe_References.Look(ref asker, "asker");

            Scribe_Values.Look(
                ref normalStoryStartSignal,
                "normalStoryStartSignal");
            Scribe_Values.Look(
                ref youngStoryStartSignal,
                "youngStoryStartSignal");
            Scribe_Values.Look(
                ref anomalyStoryStartSignal,
                "anomalyStoryStartSignal");
            Scribe_Values.Look(
                ref normalStoryReturnSignal,
                "normalStoryReturnSignal");
            Scribe_Values.Look(
                ref youngStoryReturnSignal,
                "youngStoryReturnSignal");
            Scribe_Values.Look(
                ref anomalyStoryReturnSignal,
                "anomalyStoryReturnSignal");

            Scribe_References.Look(ref capturedPawn, "capturedPawn");
            Scribe_Values.Look(
                ref boardedBiologicalAge,
                "boardedBiologicalAge",
                -1);
            Scribe_Values.Look(
                ref youngStoryBranchResolved,
                "youngStoryBranchResolved",
                false);
            Scribe_Values.Look(
                ref youngStoryBranchTriggered,
                "youngStoryBranchTriggered",
                false);
            Scribe_Values.Look(ref captured, "captured", false);
            Scribe_Values.Look(ref boardingTick, "boardingTick", -1);
            Scribe_Values.Look(
                ref returnEffectsApplied,
                "returnEffectsApplied",
                false);
            Scribe_Values.Look(
                ref triggeredStoryLettersAfterBoarding,
                "triggeredStoryLettersAfterBoarding",
                0);
            Scribe_Values.Look(
                ref departurePlayLogAdded,
                "departurePlayLogAdded",
                false);
            Scribe_Values.Look(
                ref midPlayLogsAdded,
                "midPlayLogsAdded",
                false);
            Scribe_Values.Look(
                ref returnPlayLogAdded,
                "returnPlayLogAdded",
                false);
        }
    }

    /// <summary>
    /// A compact custom PlayLog entry associated with one pawn.
    /// It is saved by RimWorld together with the normal play log.
    /// </summary>
    public class LendExpansionPlayLogEntry : LogEntry
    {
        private Pawn pawn;
        private string text;

        public LendExpansionPlayLogEntry()
        {
        }

        public LendExpansionPlayLogEntry(Pawn pawn, string text)
        {
            this.pawn = pawn;
            this.text = text;
        }

        public override bool Concerns(Thing t)
        {
            return t == pawn;
        }

        public override IEnumerable<Thing> GetConcerns()
        {
            if (pawn != null)
                yield return pawn;
        }

        private const string CategoryLabel = "出借任务";

        protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog = false)
        {
            return text ?? string.Empty;
        }

        public override string GetTipString()
        {
            return CategoryLabel + "\n" + base.GetTipString();
        }

        private static bool iconSearched;
        private static Texture2D cachedIcon;

        public override Texture2D IconFromPOV(Thing pov)
        {
            if (!iconSearched)
            {
                iconSearched = true;
                cachedIcon = ResolveVanillaIcon();
            }

            return cachedIcon;
        }

        private static Texture2D ResolveVanillaIcon()
        {
            // First try several transport/travel command textures from Core.
            // Unlike the previous guessed PlayLog paths, these are normal vanilla
            // command textures and therefore can be reused directly as the log icon.
            string[] candidatePaths =
            {
                "UI/Commands/LoadTransporter",
                "UI/Commands/LaunchShip",
                "UI/Commands/FormCaravan",
                "UI/Commands/Trade"
            };

            foreach (string path in candidatePaths)
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(path, false);
                if (texture != null)
                    return texture;
            }

            // Build-compatible fallback: discover RimWorld/Verse's already-loaded
            // UI texture holders by name, then reuse a transport/travel-like icon.
            // This avoids compile-time dependency on a specific TexCommand/TexButton
            // namespace or field name between RimWorld builds.
            List<Type> holders = new List<Type>();
            string[] holderTypeNames =
            {
                "Verse.TexCommand",
                "Verse.TexButton",
                "Verse.TexUI",
                "RimWorld.TexCommand",
                "RimWorld.TexButton",
                "RimWorld.TexUI"
            };

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (string typeName in holderTypeNames)
                {
                    Type holder = assembly.GetType(typeName, false);
                    if (holder != null && !holders.Contains(holder))
                        holders.Add(holder);
                }
            }

            string[] preferredNameParts =
            {
                "Transport",
                "Launch",
                "Caravan",
                "Trade",
                "Load",
                "Info"
            };

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            foreach (string preferred in preferredNameParts)
            {
                Texture2D texture = FindTextureOnHolders(holders, flags, preferred);
                if (texture != null)
                    return texture;
            }

            // Absolute fallback: pick the first non-null vanilla UI texture so the
            // PlayLog row always has an icon rather than silently rendering no icon.
            Texture2D fallback = FindTextureOnHolders(holders, flags, null);
            return fallback ?? BaseContent.WhiteTex;
        }

        private static Texture2D FindTextureOnHolders(
            List<Type> holders,
            BindingFlags flags,
            string preferredNamePart)
        {
            foreach (Type holder in holders)
            {
                foreach (FieldInfo field in holder.GetFields(flags))
                {
                    if (!typeof(Texture2D).IsAssignableFrom(field.FieldType))
                        continue;

                    if (!preferredNamePart.NullOrEmpty() &&
                        field.Name.IndexOf(
                            preferredNamePart,
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    try
                    {
                        Texture2D texture = field.GetValue(null) as Texture2D;
                        if (texture != null)
                            return texture;
                    }
                    catch
                    {
                    }
                }

                foreach (PropertyInfo property in holder.GetProperties(flags))
                {
                    if (!typeof(Texture2D).IsAssignableFrom(property.PropertyType) ||
                        property.GetIndexParameters().Length != 0)
                    {
                        continue;
                    }

                    if (!preferredNamePart.NullOrEmpty() &&
                        property.Name.IndexOf(
                            preferredNamePart,
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    try
                    {
                        Texture2D texture = property.GetValue(null, null) as Texture2D;
                        if (texture != null)
                            return texture;
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref text, "text");
        }
    }

    public static class LendExpansionPlayLog
    {
        private static bool tickMemberSearched;
        private static FieldInfo creationTickField;
        private static PropertyInfo creationTickProperty;

        public static void Add(Pawn pawn, string template, int ageTicks = 0)
        {
            if (!LendExpansionMod.PawnPlayLogEnabled ||
                pawn == null ||
                template.NullOrEmpty() ||
                Find.PlayLog == null)
            {
                return;
            }

            // PlayLog does not automatically apply Named() pawn coloring to custom
            // pre-resolved strings, so inject the same native pawn-name color tag
            // used by RimWorld text rendering before storing the entry.
            string coloredPawnName = pawn.LabelShortCap.ToString()
                .Colorize(ColoredText.NameColor);
            string text = template.Replace("{pawn}", coloredPawnName);

            LendExpansionPlayLogEntry entry =
                new LendExpansionPlayLogEntry(pawn, text);

            BackdateEntry(entry, Math.Max(0, ageTicks));
            Find.PlayLog.Add(entry);
        }

        private static void BackdateEntry(LogEntry entry, int ageTicks)
        {
            if (entry == null || ageTicks <= 0)
                return;

            EnsureCreationTickMember();

            try
            {
                if (creationTickField != null)
                {
                    int current = (int)creationTickField.GetValue(entry);
                    creationTickField.SetValue(
                        entry,
                        Math.Max(0, current - ageTicks));
                    return;
                }

                if (creationTickProperty != null)
                {
                    int current = (int)creationTickProperty.GetValue(entry, null);
                    creationTickProperty.SetValue(
                        entry,
                        Math.Max(0, current - ageTicks),
                        null);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[LendExpansion] Could not backdate PlayLog entry: " +
                    ex.Message);
            }
        }

        private static void EnsureCreationTickMember()
        {
            if (tickMemberSearched)
                return;

            tickMemberSearched = true;

            string[] preferredNames =
            {
                "creationTick",
                "createdTick",
                "ticksGame",
                "tick"
            };

            Type type = typeof(LogEntry);
            while (type != null && creationTickField == null)
            {
                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                foreach (string preferred in preferredNames)
                {
                    creationTickField = fields.FirstOrDefault(f =>
                        f.FieldType == typeof(int) &&
                        string.Equals(
                            f.Name,
                            preferred,
                            StringComparison.OrdinalIgnoreCase));
                    if (creationTickField != null)
                        break;
                }

                if (creationTickField == null)
                {
                    creationTickField = fields.FirstOrDefault(f =>
                        f.FieldType == typeof(int) &&
                        f.Name.IndexOf(
                            "tick",
                            StringComparison.OrdinalIgnoreCase) >= 0);
                }

                type = type.BaseType;
            }

            if (creationTickField != null)
                return;

            type = typeof(LogEntry);
            while (type != null && creationTickProperty == null)
            {
                PropertyInfo[] properties = type.GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                creationTickProperty = properties.FirstOrDefault(prop =>
                    prop.PropertyType == typeof(int) &&
                    prop.CanRead &&
                    prop.CanWrite &&
                    prop.Name.IndexOf(
                        "tick",
                        StringComparison.OrdinalIgnoreCase) >= 0);

                type = type.BaseType;
            }
        }
    }

    /// <summary>
    /// Mid-task PlayLog pools. Each story profile can contain any number of templates.
    /// To expand a storyline later, append more strings to the matching array; the
    /// selection code uses the configured fixed count (0-8) or random 3-5 count.
    /// </summary>
    public static class LendExpansionStoryLogPools
    {
        private static readonly Dictionary<string, string[]> Pools =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Normal_Royal_0", new[] {
                    "{pawn}被侍从们围在中间，换上了一件近乎透明的轻纱礼服。",
                    "{pawn}踩着透明玻璃高跟鞋，鞋子里面被射入了大量温热黏滑的精液。",
                    "{pawn}站在皇家侍从队列中拍照，裙摆后被一只黝黑大手探入，五指揉捏臀肉。",
                    "{pawn}被引导进入贵宾隔间，门锁落下后，里面传来衣物撕裂声。",
                    "{pawn}跪在软垫上，面前站着一圈仆从，手指捏住她的下巴使其张开嘴，然后肉棒插入了小嘴之中。",
                    "{pawn}被仆从按住腰肢抽送，肉棒从后方挤入她的小穴，腹部可见轻微凸起。",
                    "{pawn}的大腿根处不断流下白浊液体，顺着美腿流进高跟鞋内。",
                    "{pawn}离开隔间时裙摆湿透，鞋中白色黏液不断外溢，在地面留下脚印。"
                } },
                { "Normal_Royal_1", new[] {
                    "{pawn}登机后直接被带进豪华隔间，面朝玻璃窗被按压。",
                    "{pawn}身后的肥腻身影贴紧少女翘臀，大手从衣领伸入揉捏双乳。",
                    "{pawn}绯红的脸颊被按在玻璃上，樱桃小嘴微微张开，唾液拉丝滴落。",
                    "{pawn}被一根粗大肉棒从后方插入她的小穴，每次撞击使腹部顶出轮廓。",
                    "{pawn}被要求含住男人的肉棒，喉咙被巨根顶的鼓起，吞咽声清晰可闻。",
                    "{pawn}吐出一根沾满黏液的肉棒，嘴角挂着刚刚射进去的白浊。",
                    "{pawn}的翘臀之上满是被抽打的巴掌印记，内裤完全湿透。",
                    "{pawn}被灌满精液的小腹微胀，走动时腿间滴落数滴精液。"
                } },
                { "Normal_Royal_2", new[] {
                    "{pawn}接过管家递来的珠串，走进卫生间，再出来时珠串已不见。",
                    "{pawn}坐在座椅上，臀下隐隐有异物顶入后穴，珠粒逐一挤进肠道。",
                    "{pawn}行走时体内珠串滚动，每步都让她腰肢微颤。",
                    "{pawn}被要求寄出了一条自己的黑色裤袜，裆部撕破，沾有干涸白色精液斑痕。",
                    "{pawn}换上了一条崭新黑丝，然后带上了狗项圈，被要求扮演母狗",
                    "{pawn}的膝盖因为爬行红肿，被男人拽着链子从后面抽插。",
                    "{pawn}挪步时后穴内的珠串被反复牵动，每次摩擦都挤出肠液。",
                    "{pawn}尝试用力拽出整串肛珠，后穴之中喷出了大量精液。"
                } },
                { "Normal_Royal_3", new[] {
                    "{pawn}被年轻贵族搂住腰肢，手掌滑至臀沟按压。",
                    "{pawn}在机舱内被按倒在沙发上，双腿被分开，内裤被扯下。",
                    "{pawn}被粗长的肉棒插入她的小穴深处，同时被粗壮手指抠挖后庭。",
                    "{pawn}被要求跪下，双手扶住座椅，臀部抬起，露出红肿的双穴。",
                    "{pawn}的嘴巴被塞入肉棒，前后小穴同时被肉棒贯穿。",
                    "{pawn}高潮时小穴收缩，挤压出大量蜜液，喷在座椅皮面上。",
                    "{pawn}返回时下体裸露，阴唇外翻，穴口不断滴落晶莹汁液。",
                    "{pawn}的臀部和屁眼在返程路上被年轻贵族玩弄，手指拔出时还带出少许粘液。"
                } },
                { "Normal_Royal_4", new[] {
                    "{pawn}在机舱门口被戴上黑色眼罩，双手被绑在身后。",
                    "{pawn}被两名侍从架住，裙摆被掀起，一根振动棒塞入小穴。",
                    "{pawn}坐在椅子上，双腿被分开，更多玩具被依次填入前后穴。",
                    "{pawn}小穴之中塞满了跳蛋，控制器绑在大腿腿环之上，震动声持续不断。",
                    "{pawn}被以壁尻姿势卡在墙洞中，屁股露出，穴中塞满震动棒，身后男人排队使用她的后庭。",
                    "{pawn}的臀肉被使用壁尻便器的男人马克笔写满正字和淫乱涂鸦。",
                    "{pawn}被肏得双腿无法合拢，走路呈外八字，穴口呈圆形张开。",
                    "{pawn}尝试清理笔记，但是难以去除，还被机组人员发现后又肏了一顿。"
                } },

                { "Normal_Local_0", new[] {
                    "{pawn}步入穿梭机，臀上立刻挨了一巴掌，淫靡的肉浪荡开。",
                    "{pawn}被驾驶员拉坐到身旁，大手覆上大腿内侧揉捏。",
                    "{pawn}在机舱内被多人围住，衣物被逐件剥落，双乳被抓捏，小穴被指尖抠弄。",
                    "{pawn}被按在座椅上，一根肉棒从正面插入小穴，另一人从后方顶入后庭。",
                    "{pawn}身体被前后夹击，双乳被两只手同时抓握揉掐。",
                    "{pawn}被射精数次，腹股沟流满白浊，座椅垫浸湿。",
                    "{pawn}为了防止露馅，双腿紧紧夹住，小手捂住小腹，但仍有精液从指缝渗出。",
                    "{pawn}每走一步都滴落数滴浓白，形成一条断续的痕迹。"
                } },
                { "Normal_Local_1", new[] {
                    "{pawn}坐进了改装被座椅，座垫下方探出一根硅胶肉棒，强行顶入小穴中抽插。",
                    "{pawn}被安全带固定，震动装置开始高速抽送，腹部随之起伏。",
                    "{pawn}另一根炮机肉棒探入后穴，两根同时往复运动，液压杆发出噗嗤声。",
                    "{pawn}在测试过程中被迫连续高潮，小穴喷水溅湿仪表盘。",
                    "{pawn}全身痉挛，穴口被扩张至极限，机械臂准确记录了每次插入的深度。",
                    "{pawn}结束后瘫软在椅子之上，两个红肿肉洞完全无法合拢，从中不断流出混合液体。",
                    "{pawn}被打入催乳针之后，被吸奶器强行榨乳。",
                    "{pawn}被要求一边被炮机抽插，一边还要向科研人员提交反馈时。"
                } },
                { "Normal_Local_2", new[] {
                    "{pawn}换上兔女郎装，黑丝渔网裹住双腿，臀后塞入了伪装成兔尾巴的肛塞。",
                    "{pawn}端着托盘穿梭于酒吧，被客人伸手拍打她的臀部调戏。",
                    "{pawn}被两位壮汉夹坐，大腿上各放一只大手，粗壮的手指探入网眼抠弄小穴和屁眼。",
                    "{pawn}举杯陪酒时，被客人的大手从桌底伸入衣服内，指尖拨弄阴蒂。",
                    "{pawn}被抱上吧台，双腿架在男人肩上，粗长的肉棒被一口气整根没入。",
                    "{pawn}的翘臀被撞击得红肿不堪，乳头在衣料下凸起，不断摩擦着面料。",
                    "{pawn}在收集色情服务的小费时，纸币被客人卷起塞进小穴之中，导致部分钞票沾有精液。",
                    "{pawn}返回时醉意朦胧，小穴红肿流出客人的精液，腿环内侧夹着一捆厚厚的钞票。"
                } },
                { "Normal_Local_3", new[] {
                    "{pawn}被艺术家抚摸全身，手指滑过乳尖、臀缝和大腿根。",
                    "{pawn}在AV片场更衣室换上白色纱裙，内裤被直接剪开。",
                    "{pawn}躺上沙发，五名健壮的AV男演员围拢过来，粗壮的肉棒插入小穴和屁眼。",
                    "{pawn}轮流承受AV男演员的中出，每射完一人便换下一人，全程无套。",
                    "{pawn}的小穴内被连续灌注五份精液，腹部逐渐鼓起。",
                    "{pawn}高潮时全身抽搐，穴口溢出白浊淫汁，将纱裙下摆浸湿。",
                    "{pawn}被男人轮奸的画面被记录下来，变成了黑市畅销的AV作品。",
                    "{pawn}行走时不断有白色液体沿大腿流淌，在地上留下断断续续的水渍。"
                } },
                { "Normal_Local_4", new[] {
                    "{pawn}被雇佣兵包围，裤裆中勃起的肉棒不断摩擦着她娇嫩的皮肤。",
                    "{pawn}的衣物被撕裂，胸罩脱落，双乳被两只大手粗暴抓握。",
                    "{pawn}被夹在两壮汉之间，前后两穴同时插入两根粗长肉棒。",
                    "{pawn}双手各握一根肉棒，嘴里含一根肉棒，浑身上下共五根肉棒。",
                    "{pawn}被连续射精，精液从嘴角、穴口、后庭同时溢出。",
                    "{pawn}被从后面抱着M腿叉开，小穴被掰开展示其中精液。",
                    "{pawn}的腹部明显鼓起，每步都能听到其中的液体晃动声。",
                    "{pawn}走过之处留下断续白渍，穴口源源不断淌出浓精。"
                } },

                { "Young_Royal_0", new[] {
                    "{pawn}被皇家侍从要求出示证件，看后表示要改换任务。",
                    "{pawn}被半推半就带上穿梭机，机舱内已有几位胖嘉宾等待。",
                    "{pawn}换上白色连衣裙和白丝，站在欢迎队列中递出花束。",
                    "{pawn}被引导到软包沙发旁，趴下，臀抬起，白丝裤袜被撕开等待插入。",
                    "{pawn}被三位胖男人用种付的姿势插入，肉棒轮流插入她的小穴。",
                    "{pawn}体内的粗壮肉棒每次抽出都带出大量精液，沙发垫被浸透。",
                    "{pawn}返回途中全程坐在胖大叔腿上，肉棒始终留在她体内不断抽插射精。",
                    "{pawn}下机时尽量抬高臀部，让肉棒从小穴滑出，穴口涌出白浊精液。"
                } },
                { "Young_Royal_1", new[] {
                    "{pawn}被皇家人员拦下，改登另一艘穿梭机，驶向不明地点。",
                    "{pawn}在卫生间内，后穴塞着一根粉红假阳具，不断震动。",
                    "{pawn}坐在马桶上用力拔出假阳具，肛门中随之喷出大量精液。",
                    "{pawn}发现门外传来敲门声，一个壮硕身影挤入隔间，肉棒顶入她口中。",
                    "{pawn}被按在墙上，肉棒插入后穴，白丝小脚在半空晃动。",
                    "{pawn}挨肏的时候，摄像头被踢落，画面只余白丝脚踝和抽插声。",
                    "{pawn}被允许离开时肛门之中塞着一根滑腻的硅胶肉棒，每走几步便需要她伸手推回。",
                    "{pawn}后穴口被假阳具撑到了极限，黏滑的肠液不断渗出，使这根硅胶肉棒滑移不定。"
                } },
                { "Young_Local_0", new[] {
                    "{pawn}被机务人员拦下，说任务取消，改去别处。",
                    "{pawn}被带入机舱后半部分，里面已有健壮的男人等候肏她。",
                    "{pawn}被脱去黑丝，趴在床上，肉棒从后方插入小穴。",
                    "{pawn}被抽插时小腹被顶出凸起，每一次都带出蜜液雾气。",
                    "{pawn}身后的男人低吼后压住她，精液射入子宫，持续数十秒。",
                    "{pawn}的后穴中被灌满白浊精液，缓缓流出，弄湿床单。",
                    "{pawn}在返程途中被男人塞了一个发光肛塞，导致臀缝中隐约发光，肛塞露出尾端。",
                    "{pawn}用手捂住被塞了肛塞的臀部，强忍着羞耻感跑下了穿梭机。"
                } },
                { "Young_Local_1", new[] {
                    "{pawn}被机务人员调换任务，带进机舱后半部调教了一番。",
                    "{pawn}以鸭子坐跪在男人面前，双手握住肉棒，张嘴含入。",
                    "{pawn}的头部被按压，粗大肉棒深喉，喉咙鼓起又缩回。",
                    "{pawn}被男人抱起悬空抽插，白丝双腿紧紧盘在男人背上。",
                    "{pawn}体内的肉棒在她小穴内快速进出，水声不断。",
                    "{pawn}临近高潮时被射入子宫，直播信号恰好中断。",
                    "{pawn}发现常有看过她淫乱直播的人打招呼，她只能红脸点头。",
                    "{pawn}口中残留精液味，小穴内仍夹着温热精液。"
                } },

                { "Anomaly_Royal_0", new[] {
                    "{pawn}被带进货舱，黑布揭开，粉红触手缠绕脚踝。",
                    "{pawn}被拖入铁笼，衣物碎片被触手撕下吐出。",
                    "{pawn}被触手钻入小穴和口，黏液涂满全身。",
                    "{pawn}被一根粗大的触手插入后庭，另一根插入前穴，同时抽插。",
                    "{pawn}的腹部被顶出蠕动轮廓，触手尖端在子宫内肆意搅动。",
                    "{pawn}被触手射入大量粘液，肚子鼓胀如怀孕一样。",
                    "{pawn}回来是宽松衣服下腹部仍不断起伏，一根触手仍在内部活动。",
                    "{pawn}快走时捂住腹部，双穴内触手持续蠕动抽插。"
                } },
                { "Anomaly_Local_0", new[] {
                    "{pawn}登上覆盖活性组织的穿梭机，舱壁伸出触手缠绕四肢。",
                    "{pawn}被触手固定住后，一根粗壮的触手探入小穴，另一根插入后穴。",
                    "{pawn}被两根触手同步抽插，肉瘤摩擦内壁，发出咕叽水声。",
                    "{pawn}体内的触手顶端射出卵状物，连续注入子宫和直肠。",
                    "{pawn}全身被催情黏液和精液包裹，白丝破裂，触手在体内产卵。",
                    "{pawn}高潮时翻出白眼，樱桃小嘴中喷出一股浓郁的精液。",
                    "{pawn}被触手扔在草地上，腹部明显隆起，衣物破碎。",
                    "{pawn}肚内的触手卵仍然在不断蠕动，她必须找个地方将其排出。"
                } }
            };

        public static string[] GetPool(string profileId)
        {
            if (profileId.NullOrEmpty())
                return null;

            string[] pool;
            return Pools.TryGetValue(profileId, out pool) ? pool : null;
        }
    }

    /// <summary>
    /// Encodes per-instance return-health negative-effect strength in Hediff.Severity.
    /// Severity 1..6 represents 0..5x. Legacy instances below severity 1 remain 1x.
    /// The health-state text/disappearance timer remain present even when negatives are disabled.
    /// </summary>
    public static class LendExpansionReturnEffectScaling
    {
        private const string ReturnHediffPrefix = "LE_LendReturnHediff_";

        public static bool IsReturnHediff(Hediff hediff)
        {
            return hediff != null &&
                hediff.def != null &&
                !hediff.def.defName.NullOrEmpty() &&
                hediff.def.defName.StartsWith(
                    ReturnHediffPrefix,
                    StringComparison.Ordinal);
        }

        public static float GetNegativeEffectScale(Hediff hediff)
        {
            if (!IsReturnHediff(hediff))
                return 1f;

            if (!LendExpansionMod.NegativeReturnHealthEffectsEnabled)
                return 0f;

            float severity = hediff.Severity;

            // Compatibility with older saves, whose return Hediffs used normal
            // low/default severity rather than the encoded 1..6 range.
            if (severity < 1f)
                return 1f;

            float scale = severity - 1f;
            if (scale < 0f)
                return 0f;
            if (scale > 5f)
                return 5f;
            return scale;
        }

        public static Hediff FindReturnHediff(Pawn pawn, HediffDef def)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.hediffSet == null ||
                def == null)
            {
                return null;
            }

            foreach (Hediff hediff in GetHediffs(pawn.health.hediffSet))
            {
                if (hediff != null && hediff.def == def)
                    return hediff;
            }

            return null;
        }

        public static IEnumerable<Hediff> GetHediffs(HediffSet hediffSet)
        {
            if (hediffSet == null)
                yield break;

            object raw = null;
            Type type = hediffSet.GetType();

            while (type != null && raw == null)
            {
                FieldInfo field = type.GetField(
                    "hediffs",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                if (field != null)
                    raw = field.GetValue(hediffSet);

                type = type.BaseType;
            }

            if (raw == null)
            {
                PropertyInfo property = hediffSet.GetType().GetProperty(
                    "Hediffs",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (property != null)
                    raw = property.GetValue(hediffSet, null);
            }

            System.Collections.IEnumerable enumerable =
                raw as System.Collections.IEnumerable;
            if (enumerable == null)
                yield break;

            foreach (object obj in enumerable)
            {
                Hediff hediff = obj as Hediff;
                if (hediff != null)
                    yield return hediff;
            }
        }

        public static float GetStageStatOffset(Hediff hediff, StatDef stat)
        {
            if (hediff == null || stat == null)
                return 0f;

            try
            {
                object stage = null;
                PropertyInfo curStageProperty = hediff.GetType().GetProperty(
                    "CurStage",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (curStageProperty != null)
                    stage = curStageProperty.GetValue(hediff, null);

                if (stage == null)
                    return 0f;

                object rawOffsets = null;
                FieldInfo offsetsField = stage.GetType().GetField(
                    "statOffsets",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
                if (offsetsField != null)
                    rawOffsets = offsetsField.GetValue(stage);

                if (rawOffsets == null)
                {
                    PropertyInfo offsetsProperty = stage.GetType().GetProperty(
                        "statOffsets",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);
                    if (offsetsProperty != null)
                        rawOffsets = offsetsProperty.GetValue(stage, null);
                }

                System.Collections.IEnumerable offsets =
                    rawOffsets as System.Collections.IEnumerable;
                if (offsets == null)
                    return 0f;

                float total = 0f;
                foreach (object modifier in offsets)
                {
                    if (modifier == null)
                        continue;

                    Type modifierType = modifier.GetType();
                    FieldInfo statField = modifierType.GetField(
                        "stat",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);
                    FieldInfo valueField = modifierType.GetField(
                        "value",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                    StatDef modifierStat = statField == null
                        ? null
                        : statField.GetValue(modifier) as StatDef;

                    if (modifierStat != stat ||
                        valueField == null ||
                        valueField.FieldType != typeof(float))
                    {
                        continue;
                    }

                    total += (float)valueField.GetValue(modifier);
                }

                return total;
            }
            catch
            {
                return 0f;
            }
        }
    }

    /// <summary>
    /// Runtime map from generated QuestPart_Letter instances to the real pawn name.
    /// A thread-static current value is set only while a mapped letter is formatting.
    /// </summary>
    public static class PawnTokenRuntime
    {
        private static readonly Dictionary<QuestPart_Letter, Pawn> PawnByLetter =
            new Dictionary<QuestPart_Letter, Pawn>();

        private static readonly Dictionary<QuestPart_Letter, bool>
            YoungColonistBranchByLetter =
                new Dictionary<QuestPart_Letter, bool>();

        private static readonly Dictionary<QuestPart_Letter, QuestPart_BindBoardedPawnText>
            BinderByLetter =
                new Dictionary<QuestPart_Letter, QuestPart_BindBoardedPawnText>();

        [ThreadStatic]
        private static Pawn currentPawn;

        [ThreadStatic]
        private static bool currentYoungColonistBranch;

        public static Pawn CurrentPawn
        {
            get { return currentPawn; }
        }

        public static bool CurrentYoungColonistBranch
        {
            get { return currentYoungColonistBranch; }
        }

        public static int RegisterQuestLetters(
            Quest quest,
            Pawn pawn,
            bool youngColonistBranch,
            QuestPart_BindBoardedPawnText binder)
        {
            if (quest == null || quest.PartsListForReading == null || pawn == null)
                return 0;

            int count = 0;

            foreach (QuestPart part in quest.PartsListForReading)
            {
                QuestPart_Letter letter = part as QuestPart_Letter;
                if (letter == null)
                    continue;

                PawnByLetter[letter] = pawn;
                YoungColonistBranchByLetter[letter] = youngColonistBranch;
                if (binder != null)
                    BinderByLetter[letter] = binder;
                count++;
            }

            return count;
        }

        public static void EnterLetter(QuestPart_Letter letter)
        {
            currentPawn = null;
            currentYoungColonistBranch = false;

            if (letter == null)
                return;

            Pawn pawn;
            bool youngColonistBranch;

            if (PawnByLetter.TryGetValue(letter, out pawn) && pawn != null)
            {
                currentPawn = pawn;

                if (YoungColonistBranchByLetter.TryGetValue(
                    letter,
                    out youngColonistBranch))
                {
                    currentYoungColonistBranch = youngColonistBranch;
                }

                return;
            }

            /*
             * Static dictionaries are not saved by RimWorld. After loading a
             * save, recover both the Pawn and its locked story category from
             * the QuestPart that scribed the boarding context.
             */
            pawn = RecoverPawnFromOwningQuest(
                letter,
                out youngColonistBranch);

            if (pawn != null)
            {
                currentPawn = pawn;
                currentYoungColonistBranch = youngColonistBranch;
            }
        }

        private static Pawn RecoverPawnFromOwningQuest(
            QuestPart_Letter letter,
            out bool youngColonistBranch)
        {
            youngColonistBranch = false;

            if (Find.QuestManager == null)
                return null;

            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (quest == null || quest.PartsListForReading == null)
                    continue;

                if (!quest.PartsListForReading.Contains(letter))
                    continue;

                foreach (QuestPart part in quest.PartsListForReading)
                {
                    QuestPart_BindBoardedPawnText binder =
                        part as QuestPart_BindBoardedPawnText;

                    if (binder == null)
                        continue;

                    Pawn pawn = binder.CapturedPawn;

                    if (pawn == null)
                        continue;

                    youngColonistBranch =
                        binder.YoungColonistBranchAtBoarding;

                    // Rebuild both caches for every letter in this quest so
                    // this scan is normally needed only once after loading.
                    RegisterQuestLetters(
                        quest,
                        pawn,
                        youngColonistBranch,
                        binder);


                    return pawn;
                }

                return null;
            }

            return null;
        }

        public static void NotifyLetterSignal(QuestPart_Letter letter, Signal signal)
        {
            if (letter == null)
                return;

            string expectedSignal = GetLetterInSignal(letter);
            if (expectedSignal.NullOrEmpty() || expectedSignal != signal.tag)
                return;

            QuestPart_BindBoardedPawnText binder;
            if (!BinderByLetter.TryGetValue(letter, out binder) || binder == null)
            {
                bool ignoredYoung;
                RecoverPawnFromOwningQuest(letter, out ignoredYoung);
                BinderByLetter.TryGetValue(letter, out binder);
            }

            if (binder != null)
                binder.NotifyMappedLetterTriggered();
        }

        private static string GetLetterInSignal(QuestPart_Letter letter)
        {
            Type type = letter.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(
                    "inSignal",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (field != null)
                    return field.GetValue(letter) as string;

                type = type.BaseType;
            }

            return null;
        }

        public static void ExitLetter()
        {
            currentPawn = null;
            currentYoungColonistBranch = false;
        }
    }

    /// <summary>
    /// Runtime Harmony patches:
    /// 1) Female-only filtering for only this quest's shuttle.
    /// 2) {pawn} replacement exactly while QuestPart_Letter is formatting text.
    /// Age storyline routing itself is handled by QuestPart_BindBoardedPawnText.
    ///
    /// Harmony is loaded dynamically through reflection, so this project does not
    /// require a compile-time reference to 0Harmony.dll.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class LendExpansionRuntimePatches
    {
        public const string ShuttleTag = "LendExpansion_FemaleOnlyShuttle";
        private const string HarmonyId = "FAironball.LendExpansion.FemaleOnly";

        private static Type harmonyType;
        private static Type harmonyMethodType;
        private static object harmony;

        static LendExpansionRuntimePatches()
        {
            try
            {
                harmonyType = FindType("HarmonyLib.Harmony");
                harmonyMethodType = FindType("HarmonyLib.HarmonyMethod");

                if (harmonyType == null || harmonyMethodType == null)
                {
                    Log.Error(
                        "[LendExpansion] Harmony was not found. " +
                        "Female-only shuttle filtering and {pawn} formatting cannot be enabled. " +
                        "Please enable the Harmony mod.");
                    return;
                }

                harmony = Activator.CreateInstance(harmonyType, HarmonyId);

                // Shuttle gender filtering.
                PatchBySignature(
                    typeof(CompShuttle),
                    "IsAllowed",
                    new[] { typeof(Thing) },
                    nameof(ShuttleAllowedPrefix),
                    null);

                PatchBySignature(
                    typeof(CompShuttle),
                    "IsAllowedNow",
                    new[] { typeof(Thing) },
                    nameof(ShuttleAllowedPrefix),
                    null);

                // Establish the correct pawn context while a quest letter is handled.
                PatchBySignature(
                    typeof(QuestPart_Letter),
                    "Notify_QuestSignalReceived",
                    new[] { typeof(Signal) },
                    nameof(LetterNotifyPrefix),
                    nameof(LetterNotifyPostfix));

                // Replace {pawn} immediately before RimWorld resolves SignalArgs symbols.
                PatchBySignature(
                    typeof(SignalArgs),
                    "GetFormattedText",
                    new[] { typeof(TaggedString) },
                    nameof(GetFormattedTextPrefix),
                    null);

                // Optional return-health scaling patches. These are isolated so a
                // minor API-name change cannot disable the core shuttle/text patches.
                TryPatchReturnHealthScaling();

            }
            catch (Exception ex)
            {
                Log.Error("[LendExpansion] Failed to initialize runtime patches:\n" + ex);
            }
        }

        private static void PatchBySignature(
            Type targetType,
            string methodName,
            Type[] argumentTypes,
            string prefixName,
            string postfixName)
        {
            MethodInfo original = targetType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic,
                null,
                argumentTypes,
                null);

            if (original == null)
                throw new MissingMethodException(targetType.FullName, methodName);

            object prefix = CreateHarmonyMethod(prefixName);
            object postfix = CreateHarmonyMethod(postfixName);

            PatchOriginal(original, prefix, postfix);
        }

        private static object CreateHarmonyMethod(string patchMethodName)
        {
            if (patchMethodName.NullOrEmpty())
                return null;

            MethodInfo patch = typeof(LendExpansionRuntimePatches).GetMethod(
                patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patch == null)
                throw new MissingMethodException(
                    typeof(LendExpansionRuntimePatches).FullName,
                    patchMethodName);

            return Activator.CreateInstance(harmonyMethodType, patch);
        }

        private static void PatchOriginal(
            MethodInfo original,
            object prefix,
            object postfix)
        {
            MethodInfo patchMethod = harmonyType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name == "Patch")
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] p = m.GetParameters();
                    return p.Length >= 2
                        && typeof(MethodBase).IsAssignableFrom(p[0].ParameterType)
                        && p[1].ParameterType == harmonyMethodType;
                });

            if (patchMethod == null)
                throw new MissingMethodException("HarmonyLib.Harmony.Patch");

            ParameterInfo[] parameters = patchMethod.GetParameters();
            object[] args = new object[parameters.Length];

            args[0] = original;

            // Harmony.Patch(original, prefix, postfix, transpiler, finalizer, ...)
            if (args.Length > 1)
                args[1] = prefix;
            if (args.Length > 2)
                args[2] = postfix;

            for (int i = 3; i < args.Length; i++)
                args[i] = null;

            patchMethod.Invoke(harmony, args);
        }

        private static void TryPatchReturnHealthScaling()
        {
            try
            {
                PropertyInfo painProperty = typeof(Hediff).GetProperty(
                    "PainOffset",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                MethodInfo painGetter = painProperty == null
                    ? null
                    : painProperty.GetGetMethod(true);

                if (painGetter != null)
                {
                    PatchOriginal(
                        painGetter,
                        null,
                        CreateHarmonyMethod(nameof(HediffPainOffsetPostfix)));
                }
                else
                {
                    Log.Warning(
                        "[LendExpansion] Hediff.PainOffset getter was not found; " +
                        "pain scaling is unavailable.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[LendExpansion] Could not patch return-health pain scaling: " +
                    ex.Message);
            }

            try
            {
                MethodInfo statOffsetMethod = typeof(HediffSet)
                    .GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .FirstOrDefault(method =>
                    {
                        if (method.Name != "GetStatOffset" ||
                            method.ReturnType != typeof(float))
                        {
                            return false;
                        }

                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length == 1 &&
                            parameters[0].ParameterType == typeof(StatDef);
                    });

                if (statOffsetMethod != null)
                {
                    PatchOriginal(
                        statOffsetMethod,
                        null,
                        CreateHarmonyMethod(
                            nameof(HediffSetStatOffsetPostfix)));
                }
                else
                {
                    Log.Warning(
                        "[LendExpansion] HediffSet.GetStatOffset(StatDef) was not " +
                        "found; movement-speed scaling is unavailable.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[LendExpansion] Could not patch return-health move-speed scaling: " +
                    ex.Message);
            }
        }

        private static void HediffPainOffsetPostfix(
            Hediff __instance,
            ref float __result)
        {
            if (!LendExpansionReturnEffectScaling.IsReturnHediff(__instance))
                return;

            __result *= LendExpansionReturnEffectScaling
                .GetNegativeEffectScale(__instance);
        }

        private static void HediffSetStatOffsetPostfix(
            HediffSet __instance,
            StatDef __0,
            ref float __result)
        {
            if (__instance == null ||
                __0 == null ||
                !string.Equals(
                    __0.defName,
                    "MoveSpeed",
                    StringComparison.Ordinal))
            {
                return;
            }

            foreach (Hediff hediff in
                LendExpansionReturnEffectScaling.GetHediffs(__instance))
            {
                if (!LendExpansionReturnEffectScaling.IsReturnHediff(hediff))
                    continue;

                float baseOffset = LendExpansionReturnEffectScaling
                    .GetStageStatOffset(hediff, __0);
                if (Math.Abs(baseOffset) < 0.0001f)
                    continue;

                float scale = LendExpansionReturnEffectScaling
                    .GetNegativeEffectScale(hediff);

                // Vanilla already added baseOffset once. Add only the delta needed
                // to reach baseOffset * scale. scale=0 therefore cancels it.
                __result += baseOffset * (scale - 1f);
            }
        }

        private static bool ShuttleAllowedPrefix(
            CompShuttle __instance,
            Thing __0,
            ref bool __result)
        {
            if (__instance == null || __instance.parent == null)
                return true;

            List<string> tags = __instance.parent.questTags;
            if (tags == null || !tags.Contains(ShuttleTag))
                return true;

            Pawn pawn = __0 as Pawn;
            if (pawn != null)
            {
                if (pawn.gender != Gender.Female)
                {
                    __result = false;
                    return false;
                }

                if (pawn.ageTracker != null &&
                    pawn.ageTracker.AgeBiologicalYears < LendExpansionAgeSettings.ShuttleBoardingMinimumAge)
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }

        private static void LetterNotifyPrefix(QuestPart_Letter __instance)
        {
            PawnTokenRuntime.EnterLetter(__instance);
        }

        private static void LetterNotifyPostfix(
            QuestPart_Letter __instance,
            Signal __0)
        {
            PawnTokenRuntime.NotifyLetterSignal(__instance, __0);
            PawnTokenRuntime.ExitLetter();
        }

        private static void GetFormattedTextPrefix(ref TaggedString __0)
        {
            string raw = __0.RawText;
            if (raw.NullOrEmpty())
                return;

            Pawn pawn = PawnTokenRuntime.CurrentPawn;

            if (pawn == null ||
                raw.IndexOf("{pawn}", StringComparison.Ordinal) < 0)
            {
                return;
            }

            /*
             * Do NOT flatten the pawn name to System.String here.
             * Passing the actual Pawn through Named("pawn") preserves
             * RimWorld's native rich-text/name styling.
             */
            try
            {
                __0 = __0.Formatted(pawn.Named("pawn"));
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[LendExpansion] Native {pawn} formatting failed; " +
                    "falling back to plain name. " + ex.Message);

                __0 = raw.Replace(
                    "{pawn}",
                    pawn.LabelShortCap.ToString());
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}