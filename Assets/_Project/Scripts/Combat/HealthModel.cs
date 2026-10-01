using System;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Vita di un'entità, senza dipendenze da Unity: si testa in EditMode.
    /// Il componente Health la avvolge e la collega alla scena (piano § 4.1).
    /// </summary>
    public sealed class HealthModel
    {
        /// <summary>Vita corrente e massima, dopo ogni danno applicato.</summary>
        public event Action<float, float> Changed;

        /// <summary>Emesso una sola volta, quando la vita arriva a zero.</summary>
        public event Action Died;

        public float Current { get; private set; }
        public float Max { get; }
        public bool IsDead { get; private set; }

        public HealthModel(float max)
        {
            // NaN non supera il confronto, quindi va escluso a parte
            if (float.IsNaN(max) || max <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "La vita massima deve essere positiva");
            }

            Max = max;
            Current = max;
        }

        /// <summary>Applica il danno e restituisce quanto ne è stato assorbito davvero (0 se già morto).</summary>
        public float ApplyDamage(float amount)
        {
            // un danno nullo, negativo o NaN non deve né curare né emettere eventi
            if (IsDead || float.IsNaN(amount) || amount <= 0f)
            {
                return 0f;
            }

            float applied = Math.Min(amount, Current);
            Current -= applied;
            Changed?.Invoke(Current, Max);

            if (Current <= 0f)
            {
                Current = 0f;
                IsDead = true;
                Died?.Invoke();
            }

            return applied;
        }
    }
}
