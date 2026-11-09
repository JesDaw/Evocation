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
