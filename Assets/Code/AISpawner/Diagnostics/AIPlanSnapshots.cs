using System.Collections.Generic;
using UnityEngine;

namespace AIDecisionTrace
{
    // Values are copied at commitment time. No live ScriptableObject lookups are needed to
    // inspect a historical selection. Pair rankings are for the FOUNDATION pair only.
    public sealed class AIPlanCandidateSnapshot
    {
        public string first, second;
        public float cost, doctrine, counterGain, counterNormalized, blended, finalScore;
        public int doctrineRank, counterRank, finalRank;
        public bool selected;
    }

    public sealed class AIPlanEventSnapshot
    {
        public int planNumber, frame;
        public float time, actualMoney, virtualBudget;
        public string kind, stage, message, target;
        public int completed, planned;
    }

    public sealed class AIPlanTickSnapshot
    {
        public int tick, planNumber, frame, phase, completed, planned;
        public float time, money, virtualBudget, incomeRate, activity;
        public float nextCost, remainingActual, remainingVirtual, cooldownLeft;
        public float forecastRequiredActual, forecastRequiredVirtual, forecastProjectedActual, forecastProjectedVirtual;
        public bool feasible, incomeAware;
        public string stage, reason, waitCategory, nextUnit;
    }

    public sealed class AIPlanSnapshot
    {
        public int number, phase, startFrame, plannedCount;
        public float startedAt, endedAt = -1f, initialMoney, initialBudget;
        public float initiative, economicPatience, waveConcentration;
        public string clan, objective, orderedUnits, selectionSource, result = "Active";
        public string directorReason, economicExplanation, initiativeExplanation, rankingReport;
        public string counterplayStatus, explanation;
        public readonly List<AIPlanCandidateSnapshot> candidates = new List<AIPlanCandidateSnapshot>();
        public readonly List<AIPlanEventSnapshot> events = new List<AIPlanEventSnapshot>();
        public readonly List<AIPlanTickSnapshot> ticks = new List<AIPlanTickSnapshot>();
        public int completed;
        public float lastSpawnAt = -1f;
    }
}
