using System.Collections.Generic;
using UnityEngine;

namespace AIDecisionTrace
{
    /// <summary>
    /// Immutable-by-convention copy of one unit asset's stats, captured the FIRST time the AI
    /// evaluates that unit while recording (once per unit per recording session, never per tick).
    ///
    /// Two groups of derived values are kept side by side because the codebase uses two different
    /// approximations:
    ///   ai*     - PowerMath.GetProfile / GetDPS, i.e. exactly what ExpectedOutcomeCalculator feeds the AI.
    ///   balsim* - BalanceSimulationRunner.GetEffectiveCombatCycleSeconds / GetTrueStoppingRange.
    /// Nothing here is a new combat model; every value comes from an existing function.
    /// </summary>
    public sealed class AIUnitSnapshot
    {
        // provenance
        public string unitName;
        public UnityEngine.Object unitAsset;   // used by the Editor to resolve the GUID / ping
        public int firstSeenTick;
        public float firstSeenTime;

        // identity (ScriptableStats fields, may be empty strings)
        public string theme;
        public string ods;
        public string rpsType;

        // economy (raw asset fields). calculatedPower / valueDiscrepancy are whatever the balance tools
        // last wrote into the asset; they are NOT recomputed here and can be stale.
        public float spawnCost;
        public float spawnCooldown;
        public float calculatedPower;
        public float valueDiscrepancy;

        // raw asset stats
        public float maxHealth;
        public float attackDamage;
        public float knockbackDamage;
        public float knockbackMaxHealth;       // "knockback durability"
        public float moveSpeed;
        public float horizontalRange;
        public float verticalRange;
        public float extraEndlagSec;
        public float actionCooldown;
        public float animStartupMs;
        public float animRecoveryMs;
        public int combatActionCount;          // non-null actions

        // AI model (PowerMath) - what the decision actually used
        public float aiAtk;                    // damage per attack after action scaling
        public float aiCycleSec;               // PowerMath "Cooldown": base cooldown * castCooldown + anim overhead
        public float aiDps;                    // PowerMath.GetDPS(profile) = Atk / cycle
        public float aiRange;                  // profile.Range (stopping action range, x1.5 if extendsForward)
        public float aiMoveSpeed;
        public float aiHealth;
        public float aiKnockbackDamage;
        public float aiKnockbackDurability;
        public int aiMaxTargets;               // int.MaxValue = unlimited, 0 = deals no enemy damage
        public bool aiIsAoe;
        public float utilityPower;             // PowerMath.GetUtilityPower (support / status effects)

        // balance simulator approximations (clearly separate columns; NOT equal to ai* in general)
        public float balsimCycleSec;
        public float balsimDps;                // profile.Atk * (1 / balsimCycleSec), as in BalanceSimulationRunner
        public float balsimStoppingRange;
        public float balsimKbdps;

        // primary offensive action (PowerMath.GetPrimaryOffensiveAction); gate on hasPrimaryAction
        public bool hasPrimaryAction;
        public string primaryKind;             // derived label: EnemyDamage / Support / Other / None
        public bool primaryTargetFriendly;
        public float primaryHealthChangePercent;
        public bool primaryScalesWithUserStats;
        public int primaryMaxTargetsRaw;
        public int primaryMaxTargetsNormalized;
        public bool primaryUseProjectile;
        public bool primaryHasZoneData;
        public float primaryPriority;
        public float primaryCastCooldown;
        public float primaryKnockbackPercent;
        public int primaryEffectsOnHitCount;

        // stopping-range action (PowerMath.GetStoppingRangeAction ?? primary); gate on hasStoppingAction
        public bool hasStoppingAction;
        public bool stoppingSameAsPrimary;
        public bool stoppingDefinesStoppingRange;
        public float stoppingRangePercent;
        public bool stoppingExtendsForward;

        public static AIUnitSnapshot Capture(ScriptableStats s, int tick, float time)
        {
            if (s == null) return null;

            AIUnitSnapshot u = new AIUnitSnapshot
            {
                unitName = s.name,
                unitAsset = s,
                firstSeenTick = tick,
                firstSeenTime = time,

                theme = s.Theme,
                ods = s.ODS,
                rpsType = s.RPS_Type,

                spawnCost = s._spawnCost,
                spawnCooldown = s._spawnCooldown,
                calculatedPower = s._CalculatedPower,
                valueDiscrepancy = s._ValueDiscrepancy,

                maxHealth = s._MaxHealth,
                attackDamage = s._AttackDamage,
                knockbackDamage = s._KnockBackDamage,
                knockbackMaxHealth = s._KnockBackMaxHealth,
                moveSpeed = s._MoveSpeed,
                horizontalRange = s._HorizontalRange,
                verticalRange = s._VerticalRange,
                extraEndlagSec = s._ExtraEndlag,
                actionCooldown = s._ActionCooldown,
                animStartupMs = s._AnimationStartupTime,
                animRecoveryMs = s._AnimationRecoveryTime
            };

            int actionCount = 0;
            if (s.combatActions != null)
                for (int i = 0; i < s.combatActions.Count; i++)
                    if (s.combatActions[i] != null) actionCount++;
            u.combatActionCount = actionCount;

            // ---- AI model: the same calls ExpectedOutcomeCalculator.BuildCandidateProfile makes ----
            PowerMath.UnitProfile p = PowerMath.GetProfile(s);
            u.aiAtk = p.Atk;
            u.aiCycleSec = p.Cooldown;
            u.aiDps = PowerMath.GetDPS(p);
            u.aiRange = p.Range;
            u.aiMoveSpeed = p.Move;
            u.aiHealth = p.HP;
            u.aiKnockbackDamage = p.KBD;
            u.aiKnockbackDurability = p.KBH;
            u.aiMaxTargets = p.MaxTargets;
            u.aiIsAoe = PowerMath.IsAOE(s.combatActions);
            u.utilityPower = PowerMath.GetUtilityPower(s);

            // ---- balance simulator approximations (existing public static helpers) ----
            u.balsimCycleSec = BalanceSimulationRunner.GetEffectiveCombatCycleSeconds(s);
            float freq = 1f / Mathf.Max(u.balsimCycleSec, 0.05f);
            u.balsimDps = p.Atk * freq;
            u.balsimKbdps = p.KBD * freq;
            u.balsimStoppingRange = BalanceSimulationRunner.GetTrueStoppingRange(s);

            // ---- action properties (only members already referenced by PowerMath) ----
            CombatAction primary = PowerMath.GetPrimaryOffensiveAction(s.combatActions);
            u.hasPrimaryAction = primary != null;
            if (primary != null)
            {
                bool dealsEnemyDamage = !primary.targetFriendly && primary.healthChangePercent < 0f;
                u.primaryKind = dealsEnemyDamage ? "EnemyDamage" : (PowerMath.IsUtilityAction(primary) ? "Support" : "Other");
                u.primaryTargetFriendly = primary.targetFriendly;
                u.primaryHealthChangePercent = primary.healthChangePercent;
                u.primaryScalesWithUserStats = primary.ScaleEffectsWithUsersStats;
                u.primaryMaxTargetsRaw = primary.maxTargets;
                u.primaryMaxTargetsNormalized = PowerMath.NormalizeMaxTargets(primary);
                u.primaryUseProjectile = primary.UseProjectile;
                u.primaryHasZoneData = primary.zoneData != null;
                u.primaryPriority = (float)primary.priority;
                u.primaryCastCooldown = primary.castCooldown;
                u.primaryKnockbackPercent = primary.knockbackPercent;
                u.primaryEffectsOnHitCount = primary.effectsOnHit != null ? primary.effectsOnHit.Count : 0;
            }
            else
            {
                u.primaryKind = "None";
            }

            CombatAction stopping = PowerMath.GetStoppingRangeAction(s.combatActions) ?? primary;
            u.hasStoppingAction = stopping != null;
            if (stopping != null)
            {
                u.stoppingSameAsPrimary = ReferenceEquals(stopping, primary);
                u.stoppingDefinesStoppingRange = stopping.definesStoppingRange;
                u.stoppingRangePercent = stopping.rangePercent;
                u.stoppingExtendsForward = stopping.extendsForward;
            }

            return u;
        }

        /// <summary>True if any key stat differs (used to flag assets edited after first sight).</summary>
        public bool KeyValuesDifferFrom(AIUnitSnapshot o)
        {
            if (o == null) return false;
            return Diff(spawnCost, o.spawnCost) || Diff(spawnCooldown, o.spawnCooldown)
                || Diff(maxHealth, o.maxHealth) || Diff(attackDamage, o.attackDamage)
                || Diff(knockbackDamage, o.knockbackDamage) || Diff(knockbackMaxHealth, o.knockbackMaxHealth)
                || Diff(moveSpeed, o.moveSpeed) || Diff(horizontalRange, o.horizontalRange)
                || Diff(extraEndlagSec, o.extraEndlagSec) || Diff(actionCooldown, o.actionCooldown)
                || Diff(aiDps, o.aiDps) || Diff(aiRange, o.aiRange) || aiMaxTargets != o.aiMaxTargets;
        }

        static bool Diff(float a, float b)
        {
            bool an = float.IsNaN(a), bn = float.IsNaN(b);
            if (an || bn) return an != bn;
            return Mathf.Abs(a - b) > 1e-4f;
        }
    }

    public sealed class AISessionPhase
    {
        public int index;
        public string name;
        public readonly List<string> unitNames = new List<string>();
    }

    /// <summary>Settings and context captured once, on the first recorded tick of a session.</summary>
    public sealed class AISessionMetadata
    {
        public string sceneName;
        public string aiClanName;
        public string aiSpawnZoneName;
        public string unityVersion;
        public string startedUtc;
        public float startGameTime;
        public int startFrame;
        public float timeScale;

        public float tickInterval;
        public float maxThreatLevel;
        public float outcomeUtilityScale;
        public int maxSpawnsPerTick;
        public float swarmEvaluationWindow;
        public int maxSwarmCopies;

        public readonly List<AISessionPhase> phases = new List<AISessionPhase>();
    }
}
