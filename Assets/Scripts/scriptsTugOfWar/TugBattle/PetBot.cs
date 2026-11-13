using System;
using UnityEngine;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// The Pet opponent. Cycle: Normal -> Warn ("Strong Pull!") -> Strong -> Tired -> Recover -> Normal.
    /// Pure logic, no scene references.
    /// </summary>
    public class PetBot
    {
        public PetState State { get; private set; } = PetState.Normal;
        public event Action<PetState> StateChanged;

        private const float RecoverDuration = 1f;
        private const float MinFirstGap = 4.5f;

        private TugLevelConfig cfg;
        private float timer;
        private float nextStrongIn;

        public void Begin(TugLevelConfig config)
        {
            cfg = config;
            State = PetState.Normal;
            timer = 0f;
            nextStrongIn = Mathf.Max(MinFirstGap, RandomGap());
        }

        /// <summary>Multiplier applied to the level's base pull.</summary>
        public float Factor
        {
            get
            {
                switch (State)
                {
                    case PetState.Warn: return 0.85f;
                    case PetState.Strong: return cfg.strongMultiplier;
                    case PetState.Tired: return 0.28f;
                    case PetState.Recover: return 0.6f;
                    default: return 1f;
                }
            }
        }

        public float Pull => cfg.petPull * Factor;

        public void Tick(float dt)
        {
            timer += dt;
            switch (State)
            {
                case PetState.Normal: if (timer >= nextStrongIn) Set(PetState.Warn); break;
                case PetState.Warn: if (timer >= cfg.warnTime) Set(PetState.Strong); break;
                case PetState.Strong: if (timer >= cfg.strongDuration) Set(PetState.Tired); break;
                case PetState.Tired: if (timer >= cfg.tiredDuration) Set(PetState.Recover); break;
                case PetState.Recover:
                    if (timer >= RecoverDuration)
                    {
                        nextStrongIn = RandomGap();
                        Set(PetState.Normal);
                    }
                    break;
            }
        }

        private float RandomGap() => UnityEngine.Random.Range(cfg.strongGap.x, cfg.strongGap.y);

        private void Set(PetState s)
        {
            State = s;
            timer = 0f;
            StateChanged?.Invoke(s);
        }
    }
}
