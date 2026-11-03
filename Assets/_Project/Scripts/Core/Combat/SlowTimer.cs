using System;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Il rallentamento di un nemico (M21, dal disegno dell'albero del Mago): di una parte della sua
    /// velocità per qualche secondo. Il re, i campioni e gli unici ne prendono metà; a differenza dello
    /// stordimento non ha un tempo di pausa. Un secondo colpo tiene il più forte e il più lungo. Logica
    /// pura: il tempo lo fa passare chi la usa.
    /// </summary>
    public sealed class SlowTimer
    {
        /// <summary>Di quanto si accorcia il rallentamento di un grado superiore: la metà.</summary>
        public const float ReducedFactor = 0.5f;

        /// <summary>I secondi che restano al rallentamento; 0 se non è rallentato.</summary>
        public float Remaining { get; private set; }

        /// <summary>La parte della velocità che si perde ora, tra 0 e 1; 0 se non è rallentato.</summary>
        public float Fraction { get; private set; }

        public bool IsSlowed => Remaining > 0f;

        /// <summary>Il rallentamento è finito: il nemico riprende la sua velocità.</summary>
        public event Action Ended;

        /// <summary>La parte che si perde davvero: la metà per il re, i campioni e gli unici.</summary>
        public static float Scaled(float fraction, bool reduced)
        {
            float clamped = Math.Min(Math.Max(fraction, 0f), 1f);
            return reduced ? clamped * ReducedFactor : clamped;
        }

        /// <summary>
        /// Rallenta della parte data (già passata per <see cref="Scaled"/>) per tanti secondi; già rallentato,
        /// tiene la parte più forte e il tempo più lungo. False se non c'è niente da applicare.
        /// </summary>
        public bool Apply(float fraction, float seconds)
        {
            if (fraction <= 0f || seconds <= 0f)
            {
                return false;
            }

            Fraction = IsSlowed ? Math.Max(Fraction, fraction) : fraction;
            Remaining = Math.Max(Remaining, seconds);
            return true;
        }

        public void Advance(float deltaTime)
        {
            if (Remaining <= 0f)
            {
                return;
            }

            Remaining -= deltaTime;
            if (Remaining <= 0f)
            {
                Clear();
                Ended?.Invoke();
            }
        }

        /// <summary>Senza rallentamento: il nemico è tornato a casa o è morto.</summary>
        public void Clear()
        {
            Remaining = 0f;
            Fraction = 0f;
        }
    }
}
