using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Lo zoom della camera (D11 della M8): un fattore sulla distanza di partenza, dal 60% al 140%, a
    /// scatti del 10% con la rotella. Il valore mostrato raggiunge quello scelto in 0,15 s, rallentando
    /// verso la fine; uno scatto a metà riparte da dove si è. Logica pura.
    /// </summary>
    public sealed class ZoomLevel
    {
        public const float Step = 0.1f;

        public const int MinSteps = -4;

        public const int MaxSteps = 4;

        public const float Duration = 0.15f;

        public static float Min => 1f + MinSteps * Step;

        public static float Max => 1f + MaxSteps * Step;

        // in scatti dalla distanza di partenza: niente somme di 0,1 che si allontanano dai valori tondi
        private int _steps;
        private float _from = 1f;
        private float _elapsed = Duration;

        /// <summary>Il fattore scelto, dove lo zoom si fermerà.</summary>
        public float Target => 1f + _steps * Step;

        /// <summary>Il fattore di adesso, durante lo smorzamento tra il precedente e quello scelto.</summary>
        public float Current { get; private set; } = 1f;

        public bool IsSettled => _elapsed >= Duration;

        /// <summary>
        /// Scatti della rotella: positivi avvicinano, negativi allontanano. Fuori dai limiti si ferma.
        /// False se il fattore scelto non cambia.
        /// </summary>
        public bool Scroll(int notches)
        {
            int steps = Mathf.Clamp(_steps - notches, MinSteps, MaxSteps);
            if (steps == _steps)
            {
                return false;
            }

            _steps = steps;
            _from = Current;
            _elapsed = 0f;
            return true;
        }

        /// <summary>Il fattore salvato tra le preferenze, subito e senza smorzamento: portato allo scatto più vicino dentro i limiti.</summary>
        public void Set(float factor)
        {
            _steps = Mathf.Clamp(Mathf.FloorToInt((factor - 1f) / Step + 0.5f), MinSteps, MaxSteps);
            Current = Target;
            _from = Current;
            _elapsed = Duration;
        }

        /// <summary>Fa passare il tempo dello smorzamento. False se lo zoom era già fermo e non è cambiato niente.</summary>
        public bool Advance(float deltaTime)
        {
            if (IsSettled)
            {
                return false;
            }

            _elapsed = Mathf.Min(_elapsed + deltaTime, Duration);
            float t = 1f - _elapsed / Duration;
            Current = IsSettled ? Target : Mathf.Lerp(_from, Target, 1f - t * t * t);
            return true;
        }
    }
}
