using System;
using System.Collections.Generic;

namespace DarkDescent.Magic
{
    /// <summary>
    /// La barra delle abilità, otto posti da F1 a F8 (D9 della M20), e quello scelto. Un'abilità
    /// imparata va nel primo posto libero e si può spostare su un altro; quelle dell'albero le
    /// mette e le toglie l'albero stesso, a ogni *Applica*. Prima della M20 i posti erano fissi, uno
    /// per incantesimo, e gli incantesimi arrivavano dalle pergamene.
    /// </summary>
    public sealed class SpellBook
    {
        public const int Slots = 8;

        private readonly SpellDefinition[] _bar = new SpellDefinition[Slots];

        /// <summary>Un'abilità messa nella barra, con il suo posto.</summary>
        public event Action<int> Learned;

        /// <summary>I posti sono cambiati: un'abilità tolta o spostata.</summary>
        public event Action Rearranged;

        /// <summary>Il posto scelto è cambiato; -1 se la barra si è svuotata.</summary>
        public event Action<int> SelectionChanged;

        public SpellBook()
        {
            Selected = -1;
        }

        /// <summary>Un libro con queste abilità già nei primi posti: il libro di prova dei test.</summary>
        public SpellBook(IEnumerable<SpellDefinition> spells)
            : this()
        {
            foreach (var spell in spells ?? Array.Empty<SpellDefinition>())
            {
                Learn(spell);
            }
        }

        public int Count => Slots;

        /// <summary>Il posto scelto, o -1 se la barra è vuota.</summary>
        public int Selected { get; private set; }

        public SpellDefinition SelectedSpell => Selected >= 0 ? _bar[Selected] : null;

        /// <summary>L'abilità di un posto, o null se è vuoto.</summary>
        public SpellDefinition this[int slot] => slot >= 0 && slot < Slots ? _bar[slot] : null;

        public bool Knows(int slot) => this[slot] != null;

        public bool Knows(SpellDefinition spell) => SlotOf(spell) >= 0;

        public int SlotOf(SpellDefinition spell) => spell != null ? Array.IndexOf(_bar, spell) : -1;

        /// <summary>
        /// Mette un'abilità nel primo posto libero; false se c'è già o se la barra è piena. Se non
        /// c'era niente di scelto, la sceglie.
        /// </summary>
        public bool Learn(SpellDefinition spell)
        {
            int slot = spell != null && !Knows(spell) ? Array.IndexOf(_bar, null) : -1;
            if (slot < 0)
            {
                return false;
            }

            _bar[slot] = spell;
            Learned?.Invoke(slot);
            if (Selected < 0)
            {
                TrySelect(slot);
            }

            return true;
        }

        /// <summary>Toglie un'abilità dalla barra; se era la scelta, si sceglie la prima che resta.</summary>
        public bool Forget(SpellDefinition spell)
        {
            int slot = SlotOf(spell);
            if (slot < 0)
            {
                return false;
            }

            _bar[slot] = null;
            Rearranged?.Invoke();
            if (slot == Selected)
            {
                Selected = -1;
                SelectFirst();
            }

            return true;
        }

        /// <summary>
        /// Sposta l'abilità di un posto su un altro, scambiandola con quella che c'era; la scelta
        /// segue l'abilità. False se il primo posto è vuoto.
        /// </summary>
        public bool Move(int from, int to)
        {
            if (!Knows(from) || to < 0 || to >= Slots || from == to)
            {
                return false;
            }

            (_bar[from], _bar[to]) = (_bar[to], _bar[from]);
            Rearranged?.Invoke();
            if (Selected == from || Selected == to)
            {
                Selected = Selected == from ? to : from;
                SelectionChanged?.Invoke(Selected);
            }

            return true;
        }

        /// <summary>Sceglie il posto da lanciare con il tasto destro; false se è vuoto.</summary>
        public bool TrySelect(int slot)
        {
            if (!Knows(slot))
            {
                return false;
            }

            if (slot != Selected)
            {
                Selected = slot;
                SelectionChanged?.Invoke(slot);
            }

            return true;
        }

        /// <summary>Gli id delle abilità nella barra, senza i posti vuoti.</summary>
        public List<string> KnownIds()
        {
            var ids = new List<string>();
            foreach (var spell in _bar)
            {
                if (spell != null)
                {
                    ids.Add(spell.Id);
                }
            }

            return ids;
        }

        /// <summary>La barra posto per posto, per il salvataggio: l'id, o vuoto.</summary>
        public List<string> Export()
        {
            var ids = new List<string>(Slots);
            foreach (var spell in _bar)
            {
                ids.Add(spell != null ? spell.Id : "");
            }

            return ids;
        }

        /// <summary>
        /// Rimette i posti di un salvataggio sulle abilità che la barra ha già: ognuna torna al suo
        /// posto, gli id che non ci sono si ignorano. Se il posto scelto è vuoto si sceglie il primo.
        /// </summary>
        public void Arrange(IReadOnlyList<string> saved, int selected)
        {
            for (int slot = 0; saved != null && slot < Math.Min(saved.Count, Slots); slot++)
            {
                string id = saved[slot];
                if (string.IsNullOrEmpty(id) || _bar[slot]?.Id == id)
                {
                    continue;
                }

                // un posto prima di questo già rimesso ha un altro id: lo scambio non lo tocca
                int from = Array.FindIndex(_bar, spell => spell != null && spell.Id == id);
                if (from >= 0)
                {
                    (_bar[from], _bar[slot]) = (_bar[slot], _bar[from]);
                }
            }

            Rearranged?.Invoke();
            Selected = -1;
            if (!TrySelect(selected))
            {
                SelectFirst();
            }
        }

        private void SelectFirst()
        {
            for (int slot = 0; slot < Slots && Selected < 0; slot++)
            {
                TrySelect(slot);
            }

            if (Selected < 0)
            {
                SelectionChanged?.Invoke(-1);
            }
        }
    }
}
