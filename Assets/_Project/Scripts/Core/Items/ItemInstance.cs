using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un oggetto preciso: quello in inventario, a terra o in mano. Classe semplice e serializzabile
    /// che riferisce la definizione con l'ID, così alla M8 si salva in JSON così com'è. Dalla M5
    /// porta anche rarità, livello, seme e affissi con i loro valori (D6). Il nome non si salva: si
    /// compone nella lingua attiva.
    /// </summary>
    [Serializable]
    public sealed class ItemInstance
    {
        [SerializeField] private string _definitionId;
        [SerializeField] private Rarity _rarity;
        [SerializeField] private int _itemLevel = 1;
        [SerializeField] private ulong _seed;
        [SerializeField] private List<ItemAffix> _affixes = new List<ItemAffix>();

        // non serializzata: dopo un caricamento si ritrova con Resolve
        [NonSerialized] private ItemDefinition _definition;

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

        public string DefinitionId => _definitionId;

        /// <summary>La definizione; null per un'istanza appena letta da file e non ancora risolta.</summary>
        public ItemDefinition Definition => _definition;

        public Rarity Rarity => _rarity;

        public int ItemLevel => _itemLevel;

        /// <summary>Il seme che l'ha generato: con lo stesso seme, base e livello, il generatore rifà lo stesso oggetto.</summary>
        public ulong Seed => _seed;

        public IReadOnlyList<ItemAffix> Affixes => _affixes;

        /// <summary>Ritrova la definizione dall'ID; false se il database non la conosce.</summary>
        public bool Resolve(ItemDatabase database)
        {
            return database.TryGet(_definitionId, out _definition);
        }

        /// <summary>Ritrova definizione e affissi; false se manca qualcosa.</summary>
        public bool Resolve(ItemDatabase items, AffixDatabase affixes)
        {
            bool resolved = Resolve(items);
            foreach (var affix in _affixes)
            {
                resolved &= affix.Resolve(affixes);
            }

            return resolved;
        }
    }
}
