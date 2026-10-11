using System;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Il mana del cavaliere (D2 della M9), senza dipendenze da Unity: massimo, valore attuale,
    /// spesa e rigenerazione. Si rigenera un poco a ogni frame, ma annuncia il cambio solo quando
    /// cambia il valore intero o il massimo: la sfera non si ridisegna sessanta volte al secondo
    /// (trappola 7 della scheda).
    /// </summary>
    public sealed class ManaPool
    {
        private int _announced = -1;

        /// <summary>Mana attuale e massimo, quando cambia il valore intero o il massimo.</summary>
        public event Action<float, float> Changed;

        public float Current { get; private set; }

        public float Max { get; private set; }

        public ManaPool(float max)
        {
            if (float.IsNaN(max) || max < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "Il mana massimo non può essere negativo");
            }

            Max = max;
            Current = max;
            _announced = (int)Current;
        }

        /// <summary>
        /// Cambia il massimo (un livello, la Magia, un oggetto): il mana attuale resta, ma non oltre
        /// il nuovo massimo. Alzare il massimo non riempie, come per la vita con gli oggetti.
        /// </summary>
        public void SetMax(float max)
        {
            if (float.IsNaN(max) || max < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "Il mana massimo non può essere negativo");
            }

            if (max == Max)
            {
                return;
            }

            Max = max;
            Current = Math.Min(Current, Max);
            Announce(force: true);
        }

        /// <summary>Spende <paramref name="cost"/> se c'è; false e niente spesa se non basta.</summary>
        public bool TrySpend(float cost)
        {
            if (float.IsNaN(cost) || cost < 0f || cost > Current)
            {
                return false;
            }

            Current -= cost;
            Announce(force: false);
            return true;
        }

        /// <summary>Rende mana fino al massimo e restituisce quanto ne è tornato davvero (la pozione blu).</summary>
        public float Restore(float amount)
        {
            if (float.IsNaN(amount) || amount <= 0f || Current >= Max)
            {
                return 0f;
            }

            float applied = Math.Min(amount, Max - Current);
            Current += applied;
            Announce(force: false);
            return applied;
        }

        /// <summary>Rigenera <paramref name="fractionPerSecond"/> del massimo al secondo, per <paramref name="deltaTime"/> secondi.</summary>
        public void Regenerate(float deltaTime, float fractionPerSecond)
        {
            if (deltaTime <= 0f || Current >= Max)
            {
                return;
            }

            Current = Math.Min(Max, Current + Max * fractionPerSecond * deltaTime);
            Announce(force: false);
        }

        /// <summary>Riempie il mana: salendo di livello, come la vita, o tornando in vita.</summary>
        public void Fill()
        {
            Current = Max;
            Announce(force: true);
        }

        /// <summary>Rimette il mana a un valore salvato, tra 0 e il massimo.</summary>
        public void SetCurrent(float current)
        {
            Current = float.IsNaN(current) ? Max : Math.Min(Max, Math.Max(0f, current));
            Announce(force: true);
        }

        private void Announce(bool force)
        {
            int whole = (int)Current;
            if (!force && whole == _announced)
            {
                return;
            }

            _announced = whole;
            Changed?.Invoke(Current, Max);
        }
    }
}
