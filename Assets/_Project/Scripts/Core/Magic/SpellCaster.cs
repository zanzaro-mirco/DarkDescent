using System;
using System.Collections.Generic;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Decide se un incantesimo parte (D3, D4 della M9): deve essere nella barra, il mana deve
    /// bastare e la ricarica essere finita. Se parte spende il mana e fa ripartire la ricarica. La
    /// ricarica è dell'abilità, non del posto: spostata su un altro posto, se la porta dietro (D9
    /// della M20). Che cosa succede nel mondo lo decide chi lo usa.
    /// </summary>
    public sealed class SpellCaster
    {
        private readonly Dictionary<SpellDefinition, float> _cooldowns = new Dictionary<SpellDefinition, float>();
        private readonly List<SpellDefinition> _cooling = new List<SpellDefinition>();

        public SpellCaster(SpellBook book, ManaPool mana)
        {
            Book = book ?? throw new ArgumentNullException(nameof(book));
            Mana = mana ?? throw new ArgumentNullException(nameof(mana));
        }

        public SpellBook Book { get; }

        public ManaPool Mana { get; }

        /// <summary>Secondi di ricarica che restano all'abilità di un posto.</summary>
        public float CooldownRemaining(int slot) => Remaining(Book[slot]);

        /// <summary>La ricarica che resta, da 1 appena lanciato a 0 pronto: per scurire il posto.</summary>
        public float CooldownFraction(int slot)
        {
            var spell = Book[slot];
            return spell != null && spell.Cooldown > 0f ? Remaining(spell) / spell.Cooldown : 0f;
        }

        public CastResult Check(int slot)
        {
            var spell = Book[slot];
            if (spell == null)
            {
                return CastResult.NotKnown;
            }

            if (Remaining(spell) > 0f)
            {
                return CastResult.Cooldown;
            }

            return Mana.Current >= spell.ManaCost ? CastResult.Ready : CastResult.NoMana;
        }

        /// <summary>Lancia il posto se può: spende il mana e avvia la ricarica. Restituisce il motivo se non parte.</summary>
        public CastResult TryCast(int slot)
        {
            var result = Check(slot);
            if (result != CastResult.Ready)
            {
                return result;
            }

            var spell = Book[slot];
            Mana.TrySpend(spell.ManaCost);
            if (spell.Cooldown > 0f)
            {
                if (!_cooldowns.ContainsKey(spell))
                {
                    _cooling.Add(spell);
                }

                _cooldowns[spell] = spell.Cooldown;
            }

            return CastResult.Ready;
        }

        /// <summary>Fa passare il tempo delle ricariche; nessun evento, chi disegna lo legge.</summary>
        public void Advance(float deltaTime)
        {
            // all'indietro: una ricarica finita esce dalla lista mentre la si scorre
            for (int i = _cooling.Count - 1; i >= 0; i--)
            {
                var spell = _cooling[i];
                float left = _cooldowns[spell] - deltaTime;
                if (left > 0f)
                {
                    _cooldowns[spell] = left;
                }
                else
                {
                    _cooldowns.Remove(spell);
                    _cooling.RemoveAt(i);
                }
            }
        }

        private float Remaining(SpellDefinition spell) => spell != null && _cooldowns.TryGetValue(spell, out float left) ? left : 0f;
    }
}
