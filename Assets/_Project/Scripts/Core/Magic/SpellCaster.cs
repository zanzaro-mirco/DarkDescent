using System;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Decide se un incantesimo parte (D3, D4 della M9): deve essere noto, il mana deve bastare e
    /// la ricarica essere finita. Se parte spende il mana e fa ripartire la ricarica. Che cosa
    /// succede nel mondo lo decide chi lo usa.
    /// </summary>
    public sealed class SpellCaster
    {
        private readonly float[] _cooldowns;

        public SpellCaster(SpellBook book, ManaPool mana)
        {
            Book = book ?? throw new ArgumentNullException(nameof(book));
            Mana = mana ?? throw new ArgumentNullException(nameof(mana));
            _cooldowns = new float[book.Count];
        }

        public SpellBook Book { get; }

        public ManaPool Mana { get; }

        /// <summary>Secondi di ricarica che restano a un posto.</summary>
        public float CooldownRemaining(int slot) => _cooldowns[slot];

        /// <summary>La ricarica che resta, da 1 appena lanciato a 0 pronto: per scurire il posto.</summary>
        public float CooldownFraction(int slot)
        {
            float total = Book[slot].Cooldown;
            return total > 0f ? _cooldowns[slot] / total : 0f;
        }

        public CastResult Check(int slot)
        {
            if (!Book.Knows(slot))
            {
                return CastResult.NotKnown;
            }

            if (_cooldowns[slot] > 0f)
            {
                return CastResult.Cooldown;
            }

            return Mana.Current >= Book[slot].ManaCost ? CastResult.Ready : CastResult.NoMana;
        }

        /// <summary>Lancia il posto se può: spende il mana e avvia la ricarica. Restituisce il motivo se non parte.</summary>
        public CastResult TryCast(int slot)
        {
            var result = Check(slot);
            if (result != CastResult.Ready)
            {
                return result;
            }

            Mana.TrySpend(Book[slot].ManaCost);
            _cooldowns[slot] = Book[slot].Cooldown;
            return CastResult.Ready;
        }

        /// <summary>Fa passare il tempo delle ricariche; nessun evento, chi disegna lo legge.</summary>
        public void Advance(float deltaTime)
        {
            for (int i = 0; i < _cooldowns.Length; i++)
            {
                _cooldowns[i] = Math.Max(0f, _cooldowns[i] - deltaTime);
            }
        }
    }
}
