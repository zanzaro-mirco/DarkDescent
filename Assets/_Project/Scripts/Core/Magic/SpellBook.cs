using System;
using System.Collections.Generic;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Le abilità note e le quattro caselle alla Diablo 3 (D18 della M20): il sinistro, il destro, Q
    /// ed E. Nella casella del sinistro vanno solo abilità senza mana e senza ricarica (per ora
    /// l'Attacco), e nelle altre tre solo quelle che costano. Un'abilità nuova va nella prima casella
    /// libera che la accetta; se non ce n'è, resta nell'elenco e il menu delle abilità la mette dove
    /// si vuole. Fino al 20.4 la barra aveva otto posti da F1 a F8 e un posto scelto, lanciato col
    /// destro; prima della M20 i posti erano fissi, uno per incantesimo.
    /// </summary>
    public sealed class SpellBook
    {
        public const int Slots = 4;

        /// <summary>La casella del sinistro: si usa con un click su un nemico.</summary>
        public const int Primary = 0;

        /// <summary>La casella del destro.</summary>
        public const int Secondary = 1;

        private readonly List<SpellDefinition> _known = new List<SpellDefinition>();
        private readonly SpellDefinition[] _bar = new SpellDefinition[Slots];

        /// <summary>Un'abilità nuova tra le note, con la casella dove è andata (-1 se nessuna).</summary>
        public event Action<int> Learned;

        /// <summary>Le caselle sono cambiate: un'abilità tolta, messa o spostata.</summary>
        public event Action Rearranged;

        public SpellBook()
        {
        }

        /// <summary>Un libro con queste abilità già note e nelle caselle: il libro di prova dei test.</summary>
        public SpellBook(IEnumerable<SpellDefinition> spells)
        {
            foreach (var spell in spells ?? Array.Empty<SpellDefinition>())
            {
                Learn(spell);
            }
        }

        public int Count => Slots;

        /// <summary>Le abilità note, nell'ordine in cui sono arrivate, nelle caselle o no.</summary>
        public IReadOnlyList<SpellDefinition> Known => _known;

        /// <summary>L'abilità di una casella, o null se è vuota.</summary>
        public SpellDefinition this[int slot] => slot >= 0 && slot < Slots ? _bar[slot] : null;

        public bool Knows(int slot) => this[slot] != null;

        public bool Knows(SpellDefinition spell) => spell != null && _known.Contains(spell);

        /// <summary>La casella di un'abilità, o -1 se è nota ma fuori dalle caselle, o non nota.</summary>
        public int SlotOf(SpellDefinition spell) => spell != null ? Array.IndexOf(_bar, spell) : -1;

        /// <summary>
        /// Un'abilità senza mana e senza ricarica: va solo nella casella del sinistro, che vuole solo
        /// queste (D18). I numeri sono quelli dell'asset, non quelli dell'albero: per ora basta.
        /// </summary>
        public static bool IsPrimary(SpellDefinition spell) => spell != null && spell.ManaCost <= 0f && spell.Cooldown <= 0f;

        /// <summary>Se un'abilità può stare in una casella.</summary>
        public static bool Fits(int slot, SpellDefinition spell) =>
            spell != null && slot >= 0 && slot < Slots && (slot == Primary) == IsPrimary(spell);

        /// <summary>Le abilità note che possono andare in una casella, nell'ordine dell'elenco: il menu.</summary>
        public List<SpellDefinition> Choices(int slot)
        {
            return _known.FindAll(spell => Fits(slot, spell));
        }

        /// <summary>
        /// Aggiunge un'abilità alle note e la mette nella prima casella libera che la accetta; false
        /// se era già nota. Una già nota non si rimette: una casella lasciata vuota resta vuota.
        /// </summary>
        public bool Learn(SpellDefinition spell)
        {
            if (spell == null || Knows(spell))
            {
                return false;
            }

            _known.Add(spell);
            int slot = FirstFree(spell);
            if (slot >= 0)
            {
                _bar[slot] = spell;
            }

            Learned?.Invoke(slot);
            return true;
        }

        /// <summary>Toglie un'abilità dalle note e dalla sua casella, che resta vuota.</summary>
        public bool Forget(SpellDefinition spell)
        {
            if (!_known.Remove(spell))
            {
                return false;
            }

            int slot = SlotOf(spell);
            if (slot >= 0)
            {
                _bar[slot] = null;
            }

            Rearranged?.Invoke();
            return true;
        }

        /// <summary>
        /// Mette un'abilità nota in una casella. Se stava in un'altra, le due si scambiano, quando
        /// quella di prima ci può andare; altrimenti l'altra casella si svuota. False se l'abilità non
        /// è nota o non è adatta alla casella.
        /// </summary>
        public bool Assign(int slot, SpellDefinition spell)
        {
            if (!Knows(spell) || !Fits(slot, spell))
            {
                return false;
            }

            if (_bar[slot] == spell)
            {
                return true;
            }

            int from = SlotOf(spell);
            if (from >= 0)
            {
                _bar[from] = Fits(from, _bar[slot]) ? _bar[slot] : null;
            }

            _bar[slot] = spell;
            Rearranged?.Invoke();
            return true;
        }

        /// <summary>Gli id delle abilità note, anche fuori dalle caselle.</summary>
        public List<string> KnownIds()
        {
            return _known.ConvertAll(spell => spell.Id);
        }

        /// <summary>Le caselle una per una, per il salvataggio: l'id, o vuoto.</summary>
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
        /// Rimette le caselle di un salvataggio sulle abilità già note: ognuna torna nella sua, se ci
        /// può stare, e le caselle che il salvataggio aveva vuote restano vuote. Una barra più lunga di
        /// quattro è quella a otto posti del 20.3: lì conta l'ordine, non il posto, e le abilità tornano
        /// nelle prime caselle libere. La casella del sinistro, se resta vuota, riprende la sua abilità.
        /// </summary>
        public void Arrange(IReadOnlyList<string> saved)
        {
            if (saved == null || saved.Count == 0)
            {
                return;
            }

            Array.Clear(_bar, 0, Slots);
            bool legacy = saved.Count > Slots;
            for (int index = 0; index < saved.Count && (legacy || index < Slots); index++)
            {
                var spell = _known.Find(known => known.Id == saved[index]);
                if (spell == null || SlotOf(spell) >= 0)
                {
                    continue;
                }

                int slot = legacy ? FirstFree(spell) : index;
                if (Fits(slot, spell) && _bar[slot] == null)
                {
                    _bar[slot] = spell;
                }
            }

            if (_bar[Primary] == null)
            {
                _bar[Primary] = _known.Find(IsPrimary);
            }

            Rearranged?.Invoke();
        }

        private int FirstFree(SpellDefinition spell)
        {
            for (int slot = 0; slot < Slots; slot++)
            {
                if (_bar[slot] == null && Fits(slot, spell))
                {
                    return slot;
                }
            }

            return -1;
        }
    }
}
