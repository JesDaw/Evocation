using System.Collections.Generic;
using UnityEngine;

namespace AIDecisionTrace
{
    // Read-only observations. In V1 "operation" means a ranked hypothetical proposal,
    // NOT an executable order or a prediction of capture/arrival time.
    public sealed class AIOperationCandidate
    {
        public string objective, maneuver, target, unit, caveat;
        public int zoneNumber, enemyCount, playerCount;
        public float baseline, hypothetical, gain, incentive, cost, estimatedWait;
        public float militaryBenefit, objectiveBenefit, economicBenefit, score;
        public bool selected, rankable = true;
    }

    public sealed class AIOperationalObservation
    {
        public int id, frame, planNumber, phase, eligibleCount;
        public float time, money, virtualBudget, incomeRate, playerMoney, playerIncome;
        public float initiative, positionalValuation, economicPatience;
        public string chosen, status;
        public readonly List<AIOperationCandidate> proposals = new List<AIOperationCandidate>();
    }

    /// <summary>
    /// Shadow-only operational evaluator. It never calls purchasing, upgrade, movement,
    /// plan creation, or selection APIs. Attach to the AI harness object (or another
    /// always-active scene object); enable Decision Inspector recording to sample.
    /// All utilities are deliberately provisional and exposed for calibration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AIOperationalObserver : MonoBehaviour
    {
        [Header("Operational Shadow Evaluator (Never Controls AI)")]
        [SerializeField] bool enableObservation = true;
        [SerializeField, Min(0.25f)] float sampleIntervalSeconds = 2f;
        [SerializeField, Range(10, 1000)] int maxObservations = 400;
        [Tooltip("Scale for normalizing ExpectedOutcomeCalculator score improvements; these are NOT capture probabilities.")]
        [SerializeField, Min(1f)] float outcomeGainScale = 100f;
        [Tooltip("Reference currency expenditure for comparing candidate unit commitments.")]
        [SerializeField, Min(1f)] float referenceCost = 50f;
        [Tooltip("Reference funding delay; not a tactical execution prediction.")]
        [SerializeField, Min(1f)] float referenceWaitSeconds = 20f;
        [Tooltip("Player-denial multiplier for resource spawner AIWeight; hypothetical, since AI cannot interact with them yet.")]
        [SerializeField, Range(0f, 2f)] float denialWeight = 0.5f;

        static readonly List<AIOperationalObservation> history = new List<AIOperationalObservation>();
        static int version, nextId;
        static int sessionMarker = -1;
        float nextSample;
        AILevelHarness harness;
        AITacticalPlanExecutor executor;

        public static IReadOnlyList<AIOperationalObservation> History => history;
        public static int Version => version;
        public static void ClearHistory() { history.Clear(); nextId = 0; version++; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlay() { ClearHistory(); sessionMarker = -1; }

        void OnEnable() { nextSample = Time.time; }

        void Update()
        {
            if (!enableObservation || !AIDecisionRecorder.Enabled || !Application.isPlaying || Time.time < nextSample)
                return;
            nextSample = Time.time + sampleIntervalSeconds;
            if (harness == null) harness = AILevelHarness.Instance;
            if (executor == null) executor = FindFirstObjectByType<AITacticalPlanExecutor>();
            if (harness == null || harness.aiClan == null || UnitTracker.Instance == null) return;

            // Reset alongside the existing inspector's Clear operation when tick history is emptied.
            // A new session also resets via SubsystemRegistration above.
            if (sessionMarker > AIDecisionRecorder.Count && AIDecisionRecorder.Count == 0) ClearHistory();
            sessionMarker = AIDecisionRecorder.Count;

            AIOperationalObservation snap = Evaluate(harness, executor);
            history.Add(snap);
            if (history.Count > maxObservations) history.RemoveAt(0);
            version++;
        }

        AIOperationalObservation Evaluate(AILevelHarness h, AITacticalPlanExecutor plan)
        {
            Money ai = Money.AIInstance;
            Money player = Money.Instance;
            var snap = new AIOperationalObservation
            {
                id = ++nextId, time = Time.time, frame = Time.frameCount,
                planNumber = plan != null ? plan.PlanNumber : -1,
                phase = h.CurrentPhaseIndex,
                money = ai != null ? ai.CurrentMoney : -1f,
                virtualBudget = h.VirtualBudget,
                incomeRate = ai != null ? ai.MoneyGainPerSec : -1f,
                playerMoney = player != null ? player.CurrentMoney : -1f,
                playerIncome = player != null ? player.MoneyGainPerSec : -1f,
                initiative = h.aiClan.initiative,
                positionalValuation = h.aiClan.positionalValuation,
                economicPatience = h.aiClan.economicPatience,
                status = "Shadow evaluation only; no operation committed."
            };
            ScriptableStats[] roster = null;
            if (h.aiClan.Phases != null && h.CurrentPhaseIndex >= 0 && h.CurrentPhaseIndex < h.aiClan.Phases.Length)
                roster = h.aiClan.Phases[h.CurrentPhaseIndex].Characters;
            var eligible = new List<ScriptableStats>();
            if (roster != null)
            {
                foreach (ScriptableStats s in roster)
                {
                    if (s == null || eligible.Contains(s) || float.IsNaN(s._spawnCost) ||
                        float.IsInfinity(s._spawnCost) || s._spawnCost < 0f ||
                        float.IsNaN(s._spawnCooldown) || float.IsInfinity(s._spawnCooldown) || s._spawnCooldown < 0f) continue;
                    if (ai != null && s._spawnCost > ai.CurrentMoneyCapacity) continue;
                    eligible.Add(s);
                }
            }
            snap.eligibleCount = eligible.Count;

            UnitTracker tracker = UnitTracker.Instance;
            MapZone[] zones = tracker.zones;
            if (zones != null)
            {
                foreach (MapZone zone in zones)
                {
                    if (zone == null) continue;
                    float baseOutcome = zone.ExpectedOutcome() * 100f;
                    int enemyCount = CountAlive(zone.EnemyUnits);
                    int playerCount = CountAlive(zone.PlayerUnits) + CountAlive(zone.AllyUnits);
                    // MapZone.totalAIIncentive currently sums ResourceSpawner weights only.
                    // These are an unverified opportunity for player-denial, not a guaranteed gain.
                    float objectiveIncentive = Mathf.Max(0f, zone.totalAIIncentive);
                    foreach (ScriptableStats s in eligible)
                    {
                        float with = zone.ExpectedOutcome(s, false, 1) * 100f;
                        float gain = baseOutcome - with; // positive = improvement for AI
                        float combat = SignedNormalize(gain, outcomeGainScale);
                        float playerDenial = Mathf.Clamp01(objectiveIncentive / (1f + objectiveIncentive)) *
                            Mathf.Clamp01(h.aiClan.positionalValuation) * denialWeight;
                        // A denial opportunity counts only if player units are currently present.
                        // Not a claim about future player pickup accessibility.
                        if (playerCount == 0) playerDenial = 0f;
                        float cost = s._spawnCost;
                        float wait = WaitSeconds(h, cost, ai);
                        float economical = 1f / (1f + cost / referenceCost + wait / referenceWaitSeconds);
                        snap.proposals.Add(new AIOperationCandidate
                        {
                            objective = "Zone military dominance", maneuver = "Hypothetical single reinforcement",
                            target = zone.name, zoneNumber = zone.ZoneNumber, unit = s.name,
                            baseline = baseOutcome, hypothetical = with, gain = gain,
                            enemyCount = enemyCount, playerCount = playerCount,
                            incentive = objectiveIncentive, cost = cost, estimatedWait = wait,
                            militaryBenefit = combat, objectiveBenefit = playerDenial,
                            economicBenefit = economical,
                            score = (combat + playerDenial) * economical,
                            caveat = "Zone-local marginal combat score; no route feasibility, capture time, or objective interaction model."
                        });
                    }
                }
            }

            float currentMoney = ai != null ? ai.CurrentMoney : 0f;
            float playerMoney = player != null ? player.CurrentMoney : 0f;
            float economicPosition = (currentMoney - playerMoney) / Mathf.Max(1f, currentMoney + playerMoney);
            float reserveNeed = Mathf.Clamp01((1f - economicPosition) * 0.5f);
            snap.proposals.Add(new AIOperationCandidate
            {
                objective = "Increase liquid reserves", maneuver = "Defer optional spending",
                target = "AI economy", unit = "(none)", cost = 0f, estimatedWait = referenceWaitSeconds,
                economicBenefit = reserveNeed, score = reserveNeed * (0.15f + 0.35f * h.aiClan.economicPatience),
                caveat = "Relative cash balances only; no forecast of enemy spending, cap overflow, or tactical threat."
            });
            if (ai != null && ai.NextMoneyUpgradePrice > 0f)
            {
                float upgradeCost = ai.NextMoneyUpgradePrice;
                float wait = WaitSeconds(h, upgradeCost, ai);
                bool affordable = ai.CanUpGradeMoney();
                // Do not invent next-tier capacity, price or payback; Money doesn't expose them here.
                snap.proposals.Add(new AIOperationCandidate
                {
                    objective = "Expand economy", maneuver = "Potential wallet upgrade",
                    target = "AI economy", unit = "(upgrade)", cost = upgradeCost,
                    estimatedWait = wait, economicBenefit = 0f, score = 0f, rankable = false,
                    caveat = (affordable ? "Upgrade is currently affordable. " : "Upgrade is not currently affordable. ") +
                        "UNRANKED: next-tier income, capacity and match-time payoff are not available in this API."
                });
            }
            snap.proposals.Sort((a,b) =>
            {
                if (a.rankable != b.rankable) return a.rankable ? -1 : 1;
                return b.score.CompareTo(a.score);
            });
            if (snap.proposals.Count > 0 && snap.proposals[0].rankable)
            {
                snap.proposals[0].selected = true;
                snap.chosen = snap.proposals[0].objective + " / " + snap.proposals[0].target + " / " + snap.proposals[0].unit;
            }
            else snap.chosen = "No proposals";
            return snap;
        }

        static int CountAlive(List<GameObject> objects)
        {
            if (objects == null) return 0;
            int count = 0;
            foreach (GameObject obj in objects)
            {
                if (obj == null) continue;
                Stats stats = obj.GetComponent<Stats>();
                if (stats != null && !stats._IsDead) count++;
            }
            return count;
        }

        static float SignedNormalize(float v, float scale)
        {
            return v / (Mathf.Abs(v) + Mathf.Max(1f, scale));
        }

        static float WaitSeconds(AILevelHarness h, float cost, Money m)
        {
            if (m == null) return -1f;
            float available = Mathf.Min(m.CurrentMoney, h.VirtualBudget);
            float rate = Mathf.Max(0f, m.MoneyGainPerSec * Mathf.Max(0f, h.CurrentActivityBudgetPercent));
            float shortfall = Mathf.Max(0f, cost - available);
            if (shortfall <= 0f) return 0f;
            return rate > 0.001f ? shortfall / rate : 600f;
        }
    }
}
