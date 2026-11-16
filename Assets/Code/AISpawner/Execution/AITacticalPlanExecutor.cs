
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Experimental ordered purchasing plan (two-unit forecast retained).
/// AILevelHarness alone buys and spawns.
/// Optional AIStrategicDirector selects the units for each new plan.
/// </summary>
[DisallowMultipleComponent]
public sealed class AITacticalPlanExecutor : MonoBehaviour
{
    public enum PlanStage
    {
        Inactive,
        WaitingForRoster,
        AwaitingFrontline,
        AwaitingFollowup,
        Completed,
        Cancelled,
        PreparingPush
    }

    [Header("EXPERIMENTAL: planned purchases")]
    [SerializeField] bool enablePlanExecution = false;
    [SerializeField] ScriptableStats frontline;
    [SerializeField] ScriptableStats followup;

    [Header("Income-aware coordinated launch (opt-in)")]
    [Tooltip("OFF: sequential purchasing. ON: prepare resources for both purchases within the deployment window.")]
    [SerializeField] bool useIncomeAwareLaunch = false;
    [SerializeField, Min(0f)] float maxDeploymentGapSeconds = 2f;
    [SerializeField, Min(0f)] float incomeSafetySeconds = 0.5f;

    [Header("Forecast robustness (Milestone 4.1)")]
    [Tooltip("Conservative post-purchase activity assumption. 0 means no forecast future virtual-budget income.")]
    [SerializeField, Min(0f)]
    float assumedFollowupActivityMultiplier = 0f;

    [Header("Plan lifecycle")]
    [SerializeField] bool automaticallyRearm = true;
    [SerializeField, Min(0f)] float rearmDelaySeconds = 5f;
    [SerializeField, Min(0f)] float maxPlanDurationSeconds = 120f;
    [SerializeField] bool cancelOnLevelPhaseChange = true;
    [SerializeField] bool logTransitions = true;

    public bool Enabled => enablePlanExecution;
    public bool IncomeAwareLaunchEnabled => useIncomeAwareLaunch;
    public float MaxDeploymentGapSeconds => maxDeploymentGapSeconds;
    public PlanStage Stage { get; private set; } = PlanStage.Inactive;
    public string Explanation { get; private set; } =
        "Plan execution disabled.";

    public int PlanNumber { get; private set; }
    public int OriginPhaseIndex { get; private set; } = -1;
    public int SuccessfulSteps { get; private set; }
    public float PlanStartedAt { get; private set; }
    public float FirstSpawnAt { get; private set; } = -1f;
    public float LastDeploymentGapSeconds { get; private set; } = -1f;
    public float RequiredActualLaunchMoney { get; private set; }
    public float RequiredVirtualLaunchBudget { get; private set; }
    public float ProjectedActualAtFollowup { get; private set; }
    public float ProjectedVirtualAtFollowup { get; private set; }
    public bool ProjectedLaunchFeasible { get; private set; }
    public float ForecastActivityMultiplier { get; private set; }
    public float ForecastHorizonSeconds { get; private set; }
    public float ActualMoneyAtLaunch { get; private set; } = -1f;
    public float VirtualBudgetAtLaunch { get; private set; } = -1f;
    public float ActualMoneyAfterFirst { get; private set; } = -1f;
    public float VirtualBudgetAfterFirst { get; private set; } = -1f;
    public float ActualMoneyAtFollowup { get; private set; } = -1f;
    public float VirtualBudgetAtFollowup { get; private set; } = -1f;
    public int CompletedWithinWindowCount { get; private set; }
    public int CompletedOutsideWindowCount { get; private set; }
    public IReadOnlyList<PlanRecord> PlanRecords => planRecords;

    [System.Serializable]
    public sealed class PlanRecord
    {
        public int number;
        public int phase;
        public string firstUnit;
        public string secondUnit;
        public string orderedUnits;
        public int plannedCount;
        public int spawnedCount;
        public string spawnTimes;
        public bool incomeAware;
        public float targetGap;
        public float startTime;
        public float firstTime = -1f;
        public float secondTime = -1f;
        public float forecastHorizon;
        public float currentActivityAtClear;
        public float assumedActivityAtClear;
        public float actualAtClear = -1f;
        public float virtualAtClear = -1f;
        public float requiredActualAtClear;
        public float requiredVirtualAtClear;
        public float predictedActualAfterFirst;
        public float predictedVirtualAfterFirst;
        public float actualAfterFirst = -1f;
        public float virtualAfterFirst = -1f;
        public float actualAtFollowup = -1f;
        public float virtualAtFollowup = -1f;
        public string result = "Active";
        public string explanation = "";

        public float deploymentGap =>
            firstTime >= 0f && secondTime >= 0f
                ? secondTime - firstTime
                : -1f;
    }

    public ScriptableStats Frontline => frontline;
    public ScriptableStats Followup => followup;
    public IReadOnlyList<string> RecentEvents => recentEvents;

    public bool FinishedCurrentPlan =>
        Stage == PlanStage.Completed ||
        Stage == PlanStage.Cancelled;

    public ScriptableStats CurrentTarget
    {
        get
        {
            if (Stage == PlanStage.AwaitingFrontline)
                return activeFrontline;
            if (Stage == PlanStage.AwaitingFollowup &&
                SuccessfulSteps < activeUnits.Count)
                return activeUnits[SuccessfulSteps];
            return null;
        }
    }

    const int MaxEvents = 12;
    const int MaxPlanRecords = 500;

    readonly List<string> recentEvents =
        new List<string>(MaxEvents);

    readonly List<PlanRecord> planRecords =
        new List<PlanRecord>();

    PlanRecord currentRecord;
    AIStrategicDirector strategicDirector;
    ScriptableStats activeFrontline;
    ScriptableStats activeFollowup;
    readonly List<ScriptableStats> activeUnits = new List<ScriptableStats>();
    readonly List<float> activeSpawnTimes = new List<float>();
    public int PlannedUnitCount => activeUnits.Count;
    public IReadOnlyList<ScriptableStats> ActiveUnits => activeUnits.AsReadOnly();

    float nextRearmTime;
    bool lastEnabled;
    bool missedDeadlineReported;
    string activeFallbackReason;

    // A plan can fall back to sequential mode without changing the
    // designer's global income-aware launch setting for future plans.
    bool ActiveIncomeAware => currentRecord != null && currentRecord.incomeAware;

    void OnEnable()
    {
        ResetPlan();
    }

    /// <summary>
    /// True means the plan owns the decision.
    /// A null target means hold purchases intentionally.
    /// False delegates to legacy candidate scoring.
    /// </summary>
    public bool TryGetTarget(
        AILevelHarness harness,
        int phaseIndex,
        ScriptableStats[] currentRoster,
        out ScriptableStats target)
    {
        target = null;

        if (!enablePlanExecution)
        {
            if (lastEnabled)
                ResetPlan();

            lastEnabled = false;
            return false;
        }

        lastEnabled = true;

        if (strategicDirector == null)
        {
            strategicDirector =
                GetComponent<AIStrategicDirector>();
        }

        bool usingDirector =
            strategicDirector != null &&
            strategicDirector.AutoCompositionEnabled;

        bool manualRosterValid =
            currentRoster != null &&
            RosterContains(currentRoster, frontline) &&
            RosterContains(currentRoster, followup);

        bool rosterValid =
            harness != null &&
            currentRoster != null &&
            (usingDirector
                ? strategicDirector.HasEligibleRoster(currentRoster)
                : manualRosterValid);

        if (!rosterValid)
        {
            if (IsActiveStage())
            {
                EndPlan(
                    PlanStage.Cancelled,
                    "Configured combination unavailable in the current phase.");
            }

            Stage = PlanStage.WaitingForRoster;
            Explanation =
                "No eligible plan units in current level-phase roster; legacy purchases resume.";

            return false;
        }

        if (Stage == PlanStage.WaitingForRoster ||
            Stage == PlanStage.Inactive)
        {
            StartPlan(
                harness,
                phaseIndex,
                currentRoster);

            if (Stage == PlanStage.WaitingForRoster)
                return true;
        }

        if (IsActiveStage() &&
            cancelOnLevelPhaseChange &&
            OriginPhaseIndex != phaseIndex)
        {
            EndPlan(
                PlanStage.Cancelled,
                "Level phase changed. Existing plan cancelled.");
        }

        if (IsActiveStage() &&
            maxPlanDurationSeconds > 0f &&
            Time.time - PlanStartedAt >= maxPlanDurationSeconds)
        {
            if (Stage == PlanStage.PreparingPush && ActiveIncomeAware)
            {
                SwitchToSequential(
                    "Coordinated launch preparation timed out; " +
                    "retain the selected pair and purchase sequentially.");
            }
            else
            {
                EndPlan(PlanStage.Cancelled, "Plan timed out.");
            }
        }

        if (FinishedCurrentPlan)
        {
            if (automaticallyRearm &&
                Time.time >= nextRearmTime)
            {
                StartPlan(
                    harness,
                    phaseIndex,
                    currentRoster);
            }
            else
            {
                return true;
            }
        }

        if (Stage == PlanStage.PreparingPush)
        {
            // Live Inspector changes take effect without leaving the plan
            // indefinitely trapped in its previously selected mode.
            if (!useIncomeAwareLaunch)
            {
                SwitchToSequential(
                    "Income-aware launch disabled; continue the existing plan sequentially.");
            }
            else
            {
                string impossibleReason;
                if (CoordinatedLaunchImpossible(out impossibleReason))
                {
                    SwitchToSequential(impossibleReason);
                }
                else
                {
                    if (!CheckLaunchFeasibility(harness))
                        return true;

                    Stage = PlanStage.AwaitingFrontline;
                    Explanation =
                        "Push funded within conservative forecast window. Deploy first unit.";

                    CaptureLaunchClear(harness);

                    AddEvent(
                        "Launch cleared: " +
                        activeFrontline.name +
                        " -> " +
                        activeFollowup.name);
                }
            }
        }

        if (Stage == PlanStage.AwaitingFollowup &&
            ActiveIncomeAware &&
            FirstSpawnAt >= 0f &&
            !missedDeadlineReported &&
            Time.time - FirstSpawnAt > maxDeploymentGapSeconds)
        {
            missedDeadlineReported = true;

            AddEvent(
                "WARNING: follow-up deployment deadline exceeded; still attempting purchase.");
        }

        target = CurrentTarget;
        return target != null;
    }

    /// <summary>
    /// Detects a necessary funding/cooldown condition that cannot be met even
    /// with a full wallet and the MOST generous income permitted by the
    /// current conservative forecast. This never classifies merely low
    /// current funds as a permanent failure.
    /// </summary>
    bool CoordinatedLaunchImpossible(out string reason)
    {
        reason = null;
        Money money = Money.AIInstance;
        if (money == null || activeFrontline == null || activeFollowup == null)
            return false;

        float firstCost = activeFrontline._spawnCost;
        float secondCost = activeFollowup._spawnCost;
        if (!IsValidCost(firstCost) || !IsValidCost(secondCost))
            return false;

        float capacity = Mathf.Max(0f, money.CurrentMoneyCapacity);
        float horizon = Mathf.Max(0f, maxDeploymentGapSeconds - incomeSafetySeconds);
        // Deliberately do not require MoneyIsActive here: income could be
        // enabled later. Use an optimistic upper bound to avoid false failures.
        float rate = Mathf.Max(0f, money.MoneyGainPerSec);
        float maxActualGain = Mathf.Floor(rate * horizon);
        float maxVirtualGain = rate *
            Mathf.Max(0f, assumedFollowupActivityMultiplier) * horizon;
        float maxSharedGain = Mathf.Min(maxActualGain, maxVirtualGain);
        float minimumLaunchBudget = firstCost +
            Mathf.Max(0f, secondCost - maxSharedGain);

        if (activeFrontline == activeFollowup &&
            activeFrontline._spawnCooldown > maxDeploymentGapSeconds)
        {
            reason = string.Format(
                "Coordinated launch impossible: repeated unit cooldown {0:0.##}s " +
                "exceeds the {1:0.##}s deployment window; switching to sequential purchases.",
                activeFrontline._spawnCooldown, maxDeploymentGapSeconds);
            return true;
        }

        if (firstCost > capacity || secondCost > capacity ||
            minimumLaunchBudget > capacity + 0.0001f)
        {
            reason = string.Format(
                "Coordinated launch impossible at wallet capacity {0:0.##}: " +
                "{1} costs {2:0.##}, {3} costs {4:0.##}; " +
                "minimum launch budget {5:0.##} after at most {6:0.##} " +
                "forecast follow-up income. Switching to sequential purchases.",
                capacity, activeFrontline.name, firstCost,
                activeFollowup.name, secondCost,
                minimumLaunchBudget, maxSharedGain);
            return true;
        }

        return false;
    }

    void SwitchToSequential(string reason)
    {
        if (Stage != PlanStage.PreparingPush)
            return;

        Stage = PlanStage.AwaitingFrontline;
        if (currentRecord != null)
        {
            currentRecord.incomeAware = false;
            currentRecord.explanation = reason;
        }

        activeFallbackReason = reason;
        PlanStartedAt = Time.time; // allow a fresh, normal saving interval
        Explanation = reason;
        AddEvent("Sequential fallback: " + reason);
    }

    bool CheckLaunchFeasibility(AILevelHarness harness)
    {
        Money money = Money.AIInstance;

        if (money == null ||
            activeFrontline == null ||
            activeFollowup == null)
        {
            Explanation =
                "Waiting for money and valid unit assets.";
            return false;
        }

        float firstCost = activeFrontline._spawnCost;
        float secondCost = activeFollowup._spawnCost;

        if (!IsValidCost(firstCost) ||
            !IsValidCost(secondCost))
        {
            Explanation =
                "Invalid planned unit cost; waiting for corrected assets.";
            return false;
        }

        float horizon = Mathf.Max(
            0f,
            maxDeploymentGapSeconds - incomeSafetySeconds);

        float actual =
            Mathf.Max(0f, money.CurrentMoney);

        float virtualBudget =
            Mathf.Max(0f, harness.VirtualBudget);

        float rate = money.MoneyIsActive
            ? Mathf.Max(0f, money.MoneyGainPerSec)
            : 0f;

        float assumedActivity = Mathf.Min(
            Mathf.Max(0f, harness.CurrentActivityBudgetPercent),
            Mathf.Max(0f, assumedFollowupActivityMultiplier));

        float virtualRate = rate * assumedActivity;

        ForecastActivityMultiplier = assumedActivity;
        ForecastHorizonSeconds = horizon;

        float futureActualGain = Mathf.Floor(
            Mathf.Max(0f, rate * horizon));

        float futureVirtualGain = Mathf.Max(
            0f,
            virtualRate * horizon);

        int walletCapacity =
            money.CurrentMoneyCapacity;

        float afterFirstActual =
            actual - firstCost;

        float afterFirstVirtual =
            virtualBudget - firstCost;

        ProjectedActualAtFollowup = Mathf.Min(
            walletCapacity,
            Mathf.Max(0f, afterFirstActual) +
            futureActualGain);

        ProjectedVirtualAtFollowup = Mathf.Min(
            ProjectedActualAtFollowup,
            Mathf.Max(0f, afterFirstVirtual) +
            futureVirtualGain);

        RequiredActualLaunchMoney =
            firstCost +
            Mathf.Max(0f, secondCost - futureActualGain);

        RequiredVirtualLaunchBudget =
            firstCost +
            Mathf.Max(0f, secondCost - futureVirtualGain);

        float cdRemaining =
            harness.CooldownRemainingForPlan(activeFollowup);

        bool cooldownFeasible =
            cdRemaining <= maxDeploymentGapSeconds;

        if (activeFrontline == activeFollowup)
        {
            cooldownFeasible =
                activeFrontline._spawnCooldown <=
                maxDeploymentGapSeconds;
        }

        bool feasible =
            actual >= firstCost &&
            virtualBudget >= firstCost &&
            ProjectedActualAtFollowup >= secondCost &&
            ProjectedVirtualAtFollowup >= secondCost &&
            cooldownFeasible;

        ProjectedLaunchFeasible = feasible;

        if (!feasible)
        {
            Explanation = string.Format(
                "Preparing: actual {0:0.##}/{1:0.##}, virtual {2:0.##}/{3:0.##}; projected after first purchase at +{4:0.##}s: actual {5:0.##}/{6:0.##}, virtual {7:0.##}/{6:0.##}. Follow-up cooldown ready: {8}.",
                actual,
                RequiredActualLaunchMoney,
                virtualBudget,
                RequiredVirtualLaunchBudget,
                horizon,
                ProjectedActualAtFollowup,
                secondCost,
                ProjectedVirtualAtFollowup,
                cooldownFeasible ? "yes" : "no");
        }

        return feasible;
    }

    void CaptureLaunchClear(AILevelHarness harness)
    {
        if (currentRecord == null)
            return;

        Money money = Money.AIInstance;

        currentRecord.actualAtClear =
            money != null ? money.CurrentMoney : -1f;

        currentRecord.virtualAtClear =
            harness.VirtualBudget;

        currentRecord.currentActivityAtClear =
            harness.CurrentActivityBudgetPercent;

        currentRecord.assumedActivityAtClear =
            ForecastActivityMultiplier;

        currentRecord.forecastHorizon =
            ForecastHorizonSeconds;

        currentRecord.requiredActualAtClear =
            RequiredActualLaunchMoney;

        currentRecord.requiredVirtualAtClear =
            RequiredVirtualLaunchBudget;

        currentRecord.predictedActualAfterFirst =
            ProjectedActualAtFollowup;

        currentRecord.predictedVirtualAfterFirst =
            ProjectedVirtualAtFollowup;

        ActualMoneyAtLaunch =
            currentRecord.actualAtClear;

        VirtualBudgetAtLaunch =
            currentRecord.virtualAtClear;
    }

    static bool IsValidCost(float cost)
    {
        return !float.IsNaN(cost) &&
               !float.IsInfinity(cost) &&
               cost >= 0f;
    }

    static bool RosterContains(
        ScriptableStats[] roster,
        ScriptableStats required)
    {
        if (required == null)
            return false;

        for (int i = 0; i < roster.Length; i++)
        {
            if (roster[i] == required)
                return true;
        }

        return false;
    }

    bool IsActiveStage()
    {
        return
            Stage == PlanStage.PreparingPush ||
            Stage == PlanStage.AwaitingFrontline ||
            Stage == PlanStage.AwaitingFollowup;
    }

    /// <summary>
    /// Called after a real spawn succeeds.
    /// </summary>
    public void NotifySuccessfulPurchase(ScriptableStats purchased)
    {
        if (!enablePlanExecution || purchased == null ||
            (Stage != PlanStage.AwaitingFrontline &&
             Stage != PlanStage.AwaitingFollowup) ||
            SuccessfulSteps >= activeUnits.Count ||
            activeUnits[SuccessfulSteps] != purchased)
            return;

        int index = SuccessfulSteps;
        SuccessfulSteps++;
        AIDecisionTrace.AIDecisionRecorder.RecordPlanEvent(this, "Purchase",
            "Purchased step " + (index + 1) + "/" + activeUnits.Count +
            ": " + purchased.name);
        activeSpawnTimes.Add(Time.time);
        Money money = Money.AIInstance;
        AILevelHarness harness = AILevelHarness.Instance;
        float actual = money != null ? money.CurrentMoney : -1f;
        // The harness decrements virtual budget AFTER notifying the executor.
        float virtualAfter = harness != null
            ? Mathf.Max(0f, harness.VirtualBudget - purchased._spawnCost) : -1f;

        if (currentRecord != null)
        {
            currentRecord.spawnedCount = SuccessfulSteps;
            currentRecord.spawnTimes = string.Join(";", activeSpawnTimes.ConvertAll(
                t => t.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (index == 0)
        {
            FirstSpawnAt = Time.time;
            ActualMoneyAfterFirst = actual;
            VirtualBudgetAfterFirst = virtualAfter;
            if (currentRecord != null)
            {
                currentRecord.firstTime = Time.time;
                currentRecord.actualAfterFirst = actual;
                currentRecord.virtualAfterFirst = virtualAfter;
            }
        }
        else if (index == 1)
        {
            LastDeploymentGapSeconds = Time.time - FirstSpawnAt;
            ActualMoneyAtFollowup = actual;
            VirtualBudgetAtFollowup = virtualAfter;
            if (currentRecord != null)
            {
                currentRecord.secondTime = Time.time;
                currentRecord.actualAtFollowup = actual;
                currentRecord.virtualAtFollowup = virtualAfter;
            }
        }

        if (SuccessfulSteps < activeUnits.Count)
        {
            Stage = PlanStage.AwaitingFollowup;
            missedDeadlineReported = false;
            Explanation = "Deployed " + SuccessfulSteps + "/" + activeUnits.Count +
                "; saving/waiting for " + activeUnits[SuccessfulSteps].name + ".";
            AddEvent(Explanation);
            return;
        }

        if (ActiveIncomeAware)
        {
            if (LastDeploymentGapSeconds <= maxDeploymentGapSeconds)
                CompletedWithinWindowCount++;
            else
                CompletedOutsideWindowCount++;
        }

        string result = "All " + SuccessfulSteps + " units deployed.";
        if (SuccessfulSteps >= 2)
            result += " First-to-second gap: " +
                LastDeploymentGapSeconds.ToString("F2") + "s.";
        if (ActiveIncomeAware &&
            LastDeploymentGapSeconds > maxDeploymentGapSeconds)
            result += " WARNING: exceeded coordinated window.";
        EndPlan(PlanStage.Completed, result);
    }

    public void NotifySpawnFailure(
        ScriptableStats attempted)
    {
        if (!enablePlanExecution)
            return;

        Explanation =
            "Spawn failed for " +
            (attempted != null
                ? attempted.name
                : "planned unit") +
            "; retrying until timeout.";

        AddEvent(Explanation);
    }

    [ContextMenu("Reset Tactical Plan")]
    public void ResetPlan()
    {
        Stage = PlanStage.Inactive;

        Explanation = enablePlanExecution
            ? "Waiting to start plan."
            : "Plan execution disabled.";

        OriginPhaseIndex = -1;
        SuccessfulSteps = 0;
        PlanStartedAt = 0f;
        FirstSpawnAt = -1f;
        LastDeploymentGapSeconds = -1f;
        RequiredActualLaunchMoney = 0f;
        RequiredVirtualLaunchBudget = 0f;
        ProjectedActualAtFollowup = 0f;
        ProjectedVirtualAtFollowup = 0f;
        ProjectedLaunchFeasible = false;

        activeFrontline = null;
        activeFollowup = null;
        activeUnits.Clear();
        activeSpawnTimes.Clear();
        nextRearmTime = 0f;
        missedDeadlineReported = false;
        activeFallbackReason = null;
        currentRecord = null;

        recentEvents.Clear();

        // Preserve completed plan records during Play mode.
    }

    void StartPlan(
        AILevelHarness harness,
        int phaseIndex,
        ScriptableStats[] roster)
    {
        // The director supplies an immutable plan description. The executor
        // never reads the director's personality, scoring, or diagnostics.
        AITacticalPlan selectedPlan = null;
        if (strategicDirector != null &&
            strategicDirector.AutoCompositionEnabled)
        {
            if (!strategicDirector.TryCreatePlan(
                harness, roster, useIncomeAwareLaunch,
                maxDeploymentGapSeconds, out selectedPlan))
            {
                Stage = PlanStage.WaitingForRoster;
                Explanation = "Strategic director could not produce a valid plan.";
                return;
            }
        }
        else if (frontline != null && followup != null)
        {
            selectedPlan = new AITacticalPlan(
                new[] { frontline, followup }, "Manual two-unit plan");
        }

        if (selectedPlan == null || selectedPlan.Units.Count < 2)
        {
            Stage = PlanStage.WaitingForRoster;
            Explanation = "Executor requires at least two planned units.";
            return;
        }

        activeUnits.Clear();
        activeSpawnTimes.Clear();
        foreach (ScriptableStats unit in selectedPlan.Units)
            activeUnits.Add(unit);
        activeFrontline = activeUnits[0];
        activeFollowup = activeUnits[1];

        OriginPhaseIndex = phaseIndex;
        PlanStartedAt = Time.time;
        FirstSpawnAt = -1f;
        LastDeploymentGapSeconds = -1f;
        SuccessfulSteps = 0;
        ProjectedLaunchFeasible = false;
        missedDeadlineReported = false;
        activeFallbackReason = null;

        PlanNumber++;

        currentRecord = new PlanRecord
        {
            number = PlanNumber,
            phase = phaseIndex,
            firstUnit = activeFrontline.name,
            secondUnit = activeFollowup.name,
            orderedUnits = string.Join(" -> ", activeUnits.ConvertAll(u => u.name)),
            plannedCount = activeUnits.Count,
            spawnedCount = 0,
            incomeAware = useIncomeAwareLaunch && activeUnits.Count == 2,
            targetGap = maxDeploymentGapSeconds,
            startTime = Time.time
        };

        planRecords.Add(currentRecord);

        if (planRecords.Count > MaxPlanRecords)
            planRecords.RemoveAt(0);

        // The existing funding forecast covers exactly two purchases. Larger
        // plans explicitly use sequential saving until an N-step forecast exists.
        bool coordinated = currentRecord.incomeAware;
        Stage = coordinated ? PlanStage.PreparingPush : PlanStage.AwaitingFrontline;
        AIDecisionTrace.AIDecisionRecorder.RecordPlanStart(
            this, harness != null ? harness.aiClan : null,
            strategicDirector, selectedPlan.ObjectiveLabel, currentRecord.orderedUnits,
            strategicDirector != null && strategicDirector.AutoCompositionEnabled
                ? "Automatic" : "Manual");
        if (useIncomeAwareLaunch && !coordinated)
            activeFallbackReason = "Plan contains " + activeUnits.Count +
                " units; the two-unit launch forecast is not applicable. " +
                "Using intentional sequential purchasing.";

        AddEvent("Plan objective: " + selectedPlan.ObjectiveLabel);

        Explanation = coordinated
            ? "Preparing funded two-unit push."
            : (activeFallbackReason ?? "Saving for first planned unit.");

        AddEvent(
            "Started plan #" +
            PlanNumber +
            " (phase " + phaseIndex + "): " +
            currentRecord.orderedUnits);
    }

    void EndPlan(
        PlanStage result,
        string reason)
    {
        if (!string.IsNullOrEmpty(activeFallbackReason))
            reason += " [" + activeFallbackReason + "]";

        Stage = result;
        Explanation = reason;

        if (currentRecord != null)
        {
            currentRecord.result =
                result.ToString();

            currentRecord.explanation =
                reason;
        }

        nextRearmTime =
            Time.time +
            Mathf.Max(0f, rearmDelaySeconds);

        AddEvent(
            result + ": " + reason);
        AIDecisionTrace.AIDecisionRecorder.RecordPlanEvent(this, "PlanEnd", reason);
    }

    void AddEvent(string message)
    {
        string entry =
            Time.time.ToString("F1") +
            "s: " + message;

        recentEvents.Add(entry);

        if (recentEvents.Count > MaxEvents)
            recentEvents.RemoveAt(0);

        AIDecisionTrace.AIDecisionRecorder.RecordPlanEvent(this, "Transition", message);

        if (logTransitions)
        {
            Debug.Log(
                "[AI Plan Executor] " + entry,
                this);
        }
    }
}
