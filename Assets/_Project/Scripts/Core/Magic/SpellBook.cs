using System;
using System.Collections.Generic;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Gli incantesimi del gioco in ordine di posto (F1, F2, F3), quelli che il cavaliere conosce e
    /// quello scelto (D4, D5 della M9). Si comincia con quelli segnati come noti dall'inizio; gli
    /// altri arrivano dalle pergamene.
    /// </summary>
    public sealed class SpellBook
    {
        private readonly IReadOnlyList<SpellDefinition> _spells;
        private readonly bool[] _known;

        /// <summary>Un incantesimo imparato, con il suo posto.</summary>
        public event Action<int> Learned;

        /// <summary>Il posto scelto è cambiato.</summary>
        public event Action<int> SelectionChanged;

        public SpellBook(IReadOnlyList<SpellDefinition> spells)
        {
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
            _known = new bool[spells.Count];
            Selected = -1;
            for (int i = 0; i < spells.Count; i++)
            {
                _known[i] = spells[i].KnownFromStart;
                if (_known[i] && Selected < 0)
                {
                    Selected = i;
                }
            }
        }

        public int Count => _spells.Count;

        /// <summary>Il posto scelto, o -1 se non se ne conosce nessuno.</summary>
        public int Selected { get; private set; }

        public SpellDefinition SelectedSpell => Selected >= 0 ? _spells[Selected] : null;

        public SpellDefinition this[int slot] => _spells[slot];

        public bool Knows(int slot) => slot >= 0 && slot < _known.Length && _known[slot];

        public bool Knows(SpellDefinition spell) => Knows(SlotOf(spell));

        public int SlotOf(SpellDefinition spell)
        {
            for (int i = 0; i < _spells.Count; i++)
            {
                if (_spells[i] == spell)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Impara un incantesimo (una pergamena); false se è già noto o non è del gioco.</summary>
        public bool Learn(SpellDefinition spell)
        {
            int slot = SlotOf(spell);
            if (slot < 0 || _known[slot])
            {
                return false;
            }

            _known[slot] = true;
            Learned?.Invoke(slot);
            if (Selected < 0)
            {
                TrySelect(slot);
            }

            return true;
        }

        /// <summary>Sceglie il posto da lanciare con il tasto destro; false se l'incantesimo non è noto.</summary>
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

        /// <summary>Gli id degli incantesimi noti, per il salvataggio.</summary>
        public List<string> KnownIds()
        {
            var ids = new List<string>();
            for (int i = 0; i < _spells.Count; i++)
            {
                if (_known[i])
                {
                    ids.Add(_spells[i].Id);
                }
            }

            return ids;
        }

        /// <summary>
        /// Rimette gli incantesimi di un salvataggio: quelli noti dall'inizio restano noti, gli id
        /// sconosciuti si ignorano, e se il posto scelto non è noto si sceglie il primo noto.
        /// </summary>
        public void Restore(IEnumerable<string> knownIds, int selected)
        {
            var ids = new HashSet<string>(knownIds ?? Array.Empty<string>());
            for (int i = 0; i < _spells.Count; i++)
            {
                bool wasKnown = _known[i];
                _known[i] = _spells[i].KnownFromStart || ids.Contains(_spells[i].Id);

                // la barra degli incantesimi mostra i posti nuovi come dopo una pergamena
                if (_known[i] && !wasKnown)
                {
                    Learned?.Invoke(i);
                }
            }

            Selected = -1;
            if (!TrySelect(selected))
            {
                for (int i = 0; i < _known.Length && Selected < 0; i++)
                {
                    TrySelect(i);
                }
            }
        }
    }
}
