using System.Collections.Generic;
using UnityEngine;

namespace AIDecisionTrace
{
    /// <summary>
    /// Bounded ring buffer of AI tick snapshots.
    ///
    /// The AI harness asks for a snapshot with BeginTick(). When recording is disabled this returns
    /// null and the harness skips every capture (no allocations, just a few null checks).
    /// The Editor window flips Enabled; nothing in here ever feeds back into AI decisions.
    /// </summary>
    public static class AIDecisionRecorder
    {
        public const int MinCapacity = 10;
        public const int MaxCapacity = 5000;
        public const int DefaultCapacity = 200;

        static AITickSnapshot[] buffer = new AITickSnapshot[DefaultCapacity];
        static int start;          // index of the oldest entry
        static int count;
        static int tickCounter;
        static AITickSnapshot lastPushed;
        const int MaxPlanHistory = 500;
        static readonly List<AIPlanSnapshot> planHistory = new List<AIPlanSnapshot>();
        static AITacticalPlanExecutor observedExecutor;
        public static IReadOnlyList<AIPlanSnapshot> Plans => planHistory;
        public static AIPlanSnapshot GetPlan(int number)
        {
            for (int i = planHistory.Count - 1; i >= 0; i--)
                if (planHistory[i].number == number) return planHistory[i];
            return null;
        }

        public static void RecordPlanStart(AITacticalPlanExecutor executor,
            AIClan clan, AIStrategicDirector director, string objective,
            string orderedUnits, string source)
        {
            if (!Enabled || executor == null) return;
            var plan = new AIPlanSnapshot
            {
                number = executor.PlanNumber, phase = executor.OriginPhaseIndex,
                startFrame = Time.frameCount, startedAt = Time.time,
                plannedCount = executor.PlannedUnitCount,
                initialMoney = Money.AIInstance != null ? Money.AIInstance.CurrentMoney : -1f,
                initialBudget = AILevelHarness.Instance != null ? AILevelHarness.Instance.VirtualBudget : -1f,
                clan = clan != null ? clan.name : "", objective = objective,
                orderedUnits = orderedUnits, selectionSource = source,
                initiative = clan != null ? clan.initiative : float.NaN,
                economicPatience = clan != null ? clan.economicPatience : float.NaN,
                waveConcentration = clan != null ? clan.waveConcentration : float.NaN,
                rankingReport = director != null && source == "Automatic" ? director.LastRankingReport : "Manual plan: no director ranking.",
                directorReason = director != null && source == "Automatic" ? director.LastReason : "",
                economicExplanation = director != null && source == "Automatic" ? director.LastEconomicExplanation : "",
                initiativeExplanation = director != null && source == "Automatic" ? director.LastInitiativeExplanation : "",
                counterplayStatus = director != null && source == "Automatic"
                    ? (director.LastCounterplayActive ? "Active" : "Unavailable / doctrine fallback") : "Manual"
            };
            if (director != null && source == "Automatic")
                foreach (AIPlanCandidateSnapshot c in director.LastPairSnapshots)
                    plan.candidates.Add(new AIPlanCandidateSnapshot
                    {
                        first = c.first, second = c.second, cost = c.cost,
                        doctrine = c.doctrine, counterGain = c.counterGain,
                        counterNormalized = c.counterNormalized, blended = c.blended,
                        finalScore = c.finalScore, doctrineRank = c.doctrineRank,
                        counterRank = c.counterRank, finalRank = c.finalRank,
                        selected = c.selected
                    });
            planHistory.Add(plan);
            if (planHistory.Count > MaxPlanHistory) planHistory.RemoveAt(0);
            Version++;
        }

        public static void RecordPlanEvent(AITacticalPlanExecutor executor, string kind, string message)
        {
            if (!Enabled || executor == null) return;
            AIPlanSnapshot plan = GetPlan(executor.PlanNumber);
            if (plan == null) return;
            AILevelHarness h = AILevelHarness.Instance;
            Money m = Money.AIInstance;
            plan.events.Add(new AIPlanEventSnapshot
            {
                planNumber = plan.number, frame = Time.frameCount, time = Time.time,
                stage = executor.Stage.ToString(), kind = kind, message = message,
                target = executor.CurrentTarget != null ? executor.CurrentTarget.name : "",
                completed = executor.SuccessfulSteps, planned = executor.PlannedUnitCount,
                actualMoney = m != null ? m.CurrentMoney : -1,
                virtualBudget = h != null ? h.VirtualBudget : -1
            });
            plan.completed = executor.SuccessfulSteps;
            if (kind == "Purchase") plan.lastSpawnAt = Time.time;
            if (executor.FinishedCurrentPlan)
            {
                plan.endedAt = Time.time;
                plan.result = executor.Stage.ToString();
                plan.explanation = executor.Explanation;
            }
            Version++;
        }

        static void ObservePlanTick(AITickSnapshot tick)
        {
            if (!Enabled || tick == null) return;
            if (observedExecutor == null)
                observedExecutor = UnityEngine.Object.FindFirstObjectByType<AITacticalPlanExecutor>();
            var executor = observedExecutor;
            if (executor == null || !executor.Enabled) return;
            var plan = GetPlan(executor.PlanNumber);
            if (plan == null) return;
            Money money = Money.AIInstance;
            AILevelHarness harness = AILevelHarness.Instance;
            ScriptableStats next = executor.CurrentTarget;
            float cost = next != null ? next._spawnCost : -1f;
            float actual = money != null ? money.CurrentMoney : -1f;
            float budget = harness != null ? harness.VirtualBudget : -1f;
            string reason = executor.Explanation ?? "";
            string category = "None";
            float cooldown = -1f; // Not observable from the public plan API.
            if (executor.Stage == AITacticalPlanExecutor.PlanStage.PreparingPush) category = "CoordinatedLaunchForecast";
            else if (executor.FinishedCurrentPlan) category = "RearmOrFinished";
            else if (executor.Stage == AITacticalPlanExecutor.PlanStage.WaitingForRoster) category = "RosterUnavailable";
            else if (next != null && actual >= 0 && cost > actual) category = "ActualMoneyShortfall";
            else if (next != null && budget >= 0 && cost > budget) category = "VirtualBudgetShortfall";
            else if (next != null) category = "PurchasePending_CooldownOrOther";
            // Exact cooldown reason is captured if legacy pass diagnostics expose one.
            var pass = tick.LastPass;
            if (pass != null && pass.outcome == AIPassOutcome.WaitingOnCooldown)
            {
                category = "Cooldown";
                cooldown = pass.cooldownRemainingAtDecision;
            }
            tick.planNumber = plan.number;
            tick.planStage = executor.Stage.ToString();
            tick.planWaitCategory = category;
            tick.planWaitReason = reason;
            tick.planNextUnit = next != null ? next.name : "";
            tick.planStep = executor.SuccessfulSteps;
            tick.planLength = executor.PlannedUnitCount;
            plan.ticks.Add(new AIPlanTickSnapshot
            {
                tick = tick.tickNumber, time = tick.gameTime, frame = tick.frame,
                planNumber = plan.number, phase = tick.phaseIndex,
                completed = executor.SuccessfulSteps, planned = executor.PlannedUnitCount,
                stage = executor.Stage.ToString(), reason = reason, waitCategory = category,
                nextUnit = next != null ? next.name : "", nextCost = cost,
                money = actual, virtualBudget = budget,
                remainingActual = cost >= 0 && actual >= 0 ? Mathf.Max(0, cost - actual) : -1,
                remainingVirtual = cost >= 0 && budget >= 0 ? Mathf.Max(0, cost - budget) : -1,
                cooldownLeft = cooldown, incomeRate = tick.moneyGainPerSec,
                activity = tick.activityBudgetPercent,
                forecastRequiredActual = executor.RequiredActualLaunchMoney,
                forecastRequiredVirtual = executor.RequiredVirtualLaunchBudget,
                forecastProjectedActual = executor.ProjectedActualAtFollowup,
                forecastProjectedVirtual = executor.ProjectedVirtualAtFollowup,
                feasible = executor.ProjectedLaunchFeasible,
                incomeAware = executor.IncomeAwareLaunchEnabled
            });
        }

        // Stage 1.5: first-sight unit stats and one-off session metadata.
        static readonly Dictionary<ScriptableStats, AIUnitSnapshot> units = new Dictionary<ScriptableStats, AIUnitSnapshot>();

        /// <summary>When false (default) the AI harness captures nothing.</summary>
        public static bool Enabled { get; set; }

        /// <summary>Incremented whenever the stored history changes. Cheap change detection for UIs.</summary>
        public static int Version { get; private set; }

        public static int Count { get { return count; } }

        public static int Capacity
        {
            get { return buffer.Length; }
            set { SetCapacity(value); }
        }

        /// <summary>Settings/context captured on the first recorded tick of a session (null until then).</summary>
        public static AISessionMetadata Session { get; set; }

        public static int UnitCount { get { return units.Count; } }

        public static IEnumerable<AIUnitSnapshot> Units { get { return units.Values; } }

        /// <summary>
        /// Returns the first-sight stats for a unit, capturing them on first request. Only called while
        /// recording, and at most once per unit per session (no per-tick cost, no combat evaluation).
        /// </summary>
        public static AIUnitSnapshot GetOrCreateUnit(ScriptableStats asset)
        {
            if (asset == null) return null;
            AIUnitSnapshot u;
            if (units.TryGetValue(asset, out u)) return u;
            u = AIUnitSnapshot.Capture(asset, tickCounter, Time.time);
            units[asset] = u;
            return u;
        }

        public static AITickSnapshot Latest
        {
            get { return count == 0 ? null : GetFromOldest(count - 1); }
        }

        public static AITickSnapshot GetFromOldest(int index)
        {
            if (index < 0 || index >= count) return null;
            return buffer[(start + index) % buffer.Length];
        }

        public static AITickSnapshot GetFromNewest(int index)
        {
            return GetFromOldest(count - 1 - index);
        }

        /// <summary>Returns a new snapshot to fill, or null when recording is off.</summary>
        public static AITickSnapshot BeginTick()
        {
            if (!Enabled) return null;
            return new AITickSnapshot
            {
                tickNumber = ++tickCounter,
                gameTime = Time.time,
                frame = Time.frameCount,
                isRunning = true
            };
        }

        /// <summary>Stores a finished snapshot. Safe to call with null.</summary>
        public static void EndTick(AITickSnapshot snapshot)
        {
            if (snapshot == null) return;

            ComputeStall(snapshot, lastPushed);
            ObservePlanTick(snapshot);

            if (count == buffer.Length)
            {
                buffer[start] = snapshot;
                start = (start + 1) % buffer.Length;
            }
            else
            {
                buffer[(start + count) % buffer.Length] = snapshot;
                count++;
            }

            lastPushed = snapshot;
            Version++;
        }

        public static void Clear()
        {
            System.Array.Clear(buffer, 0, buffer.Length);
            start = 0;
            count = 0;
            lastPushed = null;
            units.Clear();
            planHistory.Clear();
            observedExecutor = null;
            Session = null;
            Version++;
        }

        static void SetCapacity(int value)
        {
            value = Mathf.Clamp(value, MinCapacity, MaxCapacity);
            if (value == buffer.Length) return;

            AITickSnapshot[] next = new AITickSnapshot[value];
            int keep = Mathf.Min(count, value);
            int skip = count - keep; // drop the oldest if shrinking
            for (int i = 0; i < keep; i++)
                next[i] = buffer[(start + skip + i) % buffer.Length];

            buffer = next;
            start = 0;
            count = keep;
            Version++;
        }

        static void ComputeStall(AITickSnapshot t, AITickSnapshot prev)
        {
            AIPassSnapshot last = t.LastPass;
            bool stalled = last != null
                && t.SpawnCount == 0
                && (last.outcome == AIPassOutcome.SavingOverBudget || last.outcome == AIPassOutcome.WaitingOnCooldown);

            if (!stalled)
            {
                t.stallStreakTicks = 0;
                return;
            }

            AICandidateSnapshot sel = last.Selected;
            t.stallOutcome = last.outcome;
            t.stallUnitName = sel != null ? sel.unitName : null;

            bool continuing = prev != null
                && prev.stallStreakTicks > 0
                && prev.stallOutcome == t.stallOutcome
                && prev.stallUnitName == t.stallUnitName;

            t.stallStreakTicks = continuing ? prev.stallStreakTicks + 1 : 1;
        }

        // Start each Play session with an empty history (also covers "Enter Play Mode Options"
        // with domain reload disabled). Enabled is deliberately NOT touched here; the Editor
        // window owns that flag.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlaySession()
        {
            Clear();
            tickCounter = 0;
        }
    }
}