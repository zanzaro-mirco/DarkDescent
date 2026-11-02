using System;
using System.Collections.Generic;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Decide se un incantesimo parte (D3, D4 della M9): deve essere nella barra, il mana deve
    /// bastare e la ricarica essere finita. Se parte spende il mana e fa ripartire la ricarica. La
    /// ricarica è dell'abilità, non del posto: spostata su un altro posto, se la porta dietro (D9
    /// della M20). Costo e ricarica li dice chi li conosce, con i nodi dell'albero: senza, quelli
    /// dell'asset. Che cosa succede nel mondo lo decide chi lo usa.
    /// </summary>
    public sealed class SpellCaster
    {
        // per ogni abilità in ricarica, quanto resta e quanto era lunga quando è partita
        private readonly Dictionary<SpellDefinition, (float left, float total)> _cooldowns = new Dictionary<SpellDefinition, (float, float)>();
        private readonly List<SpellDefinition> _cooling = new List<SpellDefinition>();
        private Func<SpellDefinition, float> _manaCost = spell => spell.ManaCost;
        private Func<SpellDefinition, float> _cooldown = spell => spell.Cooldown;

        public SpellCaster(SpellBook book, ManaPool mana)
        {
            Book = book ?? throw new ArgumentNullException(nameof(book));
            Mana = mana ?? throw new ArgumentNullException(nameof(mana));
        }

        public SpellBook Book { get; }

        public ManaPool Mana { get; }

        /// <summary>Costo e ricarica con l'albero (M20); null rimette quelli dell'asset.</summary>
        public void SetNumbers(Func<SpellDefinition, float> manaCost, Func<SpellDefinition, float> cooldown)
        {
            _manaCost = manaCost ?? (spell => spell.ManaCost);
            _cooldown = cooldown ?? (spell => spell.Cooldown);
        }

        /// <summary>Il mana che costa adesso.</summary>
        public float ManaCost(SpellDefinition spell) => spell != null ? _manaCost(spell) : 0f;

        /// <summary>I secondi di ricarica che avrebbe lanciata adesso.</summary>
        public float Cooldown(SpellDefinition spell) => spell != null ? _cooldown(spell) : 0f;

        /// <summary>Secondi di ricarica che restano all'abilità di un posto.</summary>
        public float CooldownRemaining(int slot) => Remaining(Book[slot]);

        /// <summary>La ricarica che resta, da 1 appena lanciato a 0 pronto: per scurire il posto.</summary>
        public float CooldownFraction(int slot)
        {
            var spell = Book[slot];
            return spell != null && _cooldowns.TryGetValue(spell, out var cooling) && cooling.total > 0f ? cooling.left / cooling.total : 0f;
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

            return Mana.Current >= ManaCost(spell) ? CastResult.Ready : CastResult.NoMana;
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
            Mana.TrySpend(ManaCost(spell));
            float cooldown = Cooldown(spell);
            if (cooldown > 0f)
            {
                if (!_cooldowns.ContainsKey(spell))
                {
                    _cooling.Add(spell);
                }

                _cooldowns[spell] = (cooldown, cooldown);
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
                var cooling = _cooldowns[spell];
                float left = cooling.left - deltaTime;
                if (left > 0f)
                {
                    _cooldowns[spell] = (left, cooling.total);
                }
                else
                {
                    _cooldowns.Remove(spell);
                    _cooling.RemoveAt(i);
                }
            }
        }

        private float Remaining(SpellDefinition spell) => spell != null && _cooldowns.TryGetValue(spell, out var cooling) ? cooling.left : 0f;
    }
}
