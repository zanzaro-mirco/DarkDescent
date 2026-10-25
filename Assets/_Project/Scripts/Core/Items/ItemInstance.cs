using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un oggetto preciso: quello in inventario, a terra o in mano. Classe semplice e serializzabile
    /// che riferisce la definizione con l'ID, così alla M8 si salva in JSON così com'è. Dalla M5
    /// porta anche rarità, livello, seme e affissi con i loro valori (D6). Un unico o un pezzo di set
    /// (M16) porta invece l'id della sua definizione e i valori delle sue proprietà, in ordine. Il
    /// nome non si salva: si compone nella lingua attiva.
    /// </summary>
    [Serializable]
    public sealed class ItemInstance
    {
        [SerializeField] private string _definitionId;
        [SerializeField] private Rarity _rarity;
        [SerializeField] private int _itemLevel = 1;
        [SerializeField] private ulong _seed;
        [SerializeField] private List<ItemAffix> _affixes = new List<ItemAffix>();
        [SerializeField] private string _namedId;
        [SerializeField] private List<int> _namedValues = new List<int>();

        // non serializzate: dopo un caricamento si ritrovano con Resolve
        [NonSerialized] private ItemDefinition _definition;
        [NonSerialized] private UniqueDefinition _named;

        /// <summary>Un oggetto normale, senza affissi: quelli messi dalla mappa e l'arma di partenza.</summary>
        public ItemInstance(ItemDefinition definition)
            : this(definition, Rarity.Normal, 1, 0UL, null)
        {
        }

        public ItemInstance(ItemDefinition definition, Rarity rarity, int itemLevel, ulong seed, IReadOnlyList<ItemAffix> affixes)
        {
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            _definitionId = definition.Id;
            _rarity = rarity;
            _itemLevel = itemLevel;
            _seed = seed;
            if (affixes != null)
            {
                _affixes.AddRange(affixes);
            }
        }

        /// <summary>Un unico o un pezzo di set, con un valore per proprietà nell'ordine della definizione.</summary>
        public ItemInstance(UniqueDefinition named, int itemLevel, ulong seed, IReadOnlyList<int> values)
            : this(named != null ? named.Base : throw new ArgumentNullException(nameof(named)), named.Rarity, itemLevel, seed, null)
        {
            if (values == null || values.Count != named.Properties.Count)
            {
                throw new ArgumentException("Serve un valore per ogni proprietà", nameof(values));
            }

            _named = named;
            _namedId = named.Id;
            _namedValues.AddRange(values);
        }

        public string DefinitionId => _definitionId;

        /// <summary>La definizione; null per un'istanza appena letta da file e non ancora risolta.</summary>
        public ItemDefinition Definition => _definition;

        public Rarity Rarity => _rarity;

        public int ItemLevel => _itemLevel;

        /// <summary>Il seme che l'ha generato: con lo stesso seme, base e livello, il generatore rifà lo stesso oggetto.</summary>
        public ulong Seed => _seed;

        public IReadOnlyList<ItemAffix> Affixes => _affixes;

        /// <summary>L'id dell'unico o del pezzo di set; vuoto per gli altri oggetti.</summary>
        public string NamedId => _namedId;

        /// <summary>L'unico o il pezzo di set; null per gli altri e per uno appena letto da file e non ancora risolto.</summary>
        public UniqueDefinition Named => _named;

        /// <summary>I valori delle proprietà dell'unico o del pezzo, nell'ordine della definizione.</summary>
        public IReadOnlyList<int> NamedValues => _namedValues ??= new List<int>();

        public bool IsNamed => !string.IsNullOrEmpty(_namedId);

        /// <summary>Ritrova la definizione dall'ID; false se il database non la conosce.</summary>
        public bool Resolve(ItemDatabase database)
        {
            return database.TryGet(_definitionId, out _definition);
        }

        /// <summary>
        /// Ritrova definizione, affissi e, per un unico o un pezzo di set, la sua definizione; false se
        /// manca qualcosa. Un unico che <paramref name="named"/> non conosce più (D13 della M16) diventa un
        /// raro della sua base senza affissi, con un avviso; senza il database resta da risolvere.
        /// </summary>
        public bool Resolve(ItemDatabase items, AffixDatabase affixes, NamedItemDatabase named = null)
        {
            bool resolved = Resolve(items);
            foreach (var affix in _affixes)
            {
                resolved &= affix.Resolve(affixes);
            }

            if (!IsNamed)
            {
                return resolved;
            }

            if (named == null)
            {
                return false;
            }

            // un salvataggio di prima della M16 non ha la lista: JsonUtility la lascia vuota o nulla
            _namedValues ??= new List<int>();
            if (named.TryGet(_namedId, out _named) && _named.Properties.Count == _namedValues.Count)
            {
                return resolved;
            }

            Debug.LogWarning($"[DarkDescent] l'oggetto con nome {_namedId} non c'è più: torna un raro della sua base");
            _named = null;
            _namedId = string.Empty;
            _namedValues.Clear();
            _rarity = Rarity.Rare;
            return resolved;
        }
    }
}
