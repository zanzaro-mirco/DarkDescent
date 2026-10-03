using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Tutti gli oggetti che si possono trovare, per ID (piano § 4.4). L'elenco lo tiene
    /// aggiornato il menu DarkDescent → Oggetti → Aggiorna il database, e un test verifica che
    /// non manchi niente.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "DarkDescent/Items/Item Database")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> _items = new List<ItemDefinition>();

        // indice ricavato dall'elenco, non stato di gioco: si ricostruisce da solo se manca
        [NonSerialized] private Dictionary<string, ItemDefinition> _byId;

        public IReadOnlyList<ItemDefinition> Items => _items;

        public bool TryGet(string id, out ItemDefinition definition)
        {
            if (string.IsNullOrEmpty(id))
            {
                definition = null;
                return false;
            }

            return Index.TryGetValue(id, out definition);
        }

        private Dictionary<string, ItemDefinition> Index
        {
            get
            {
                if (_byId == null)
                {
                    _byId = new Dictionary<string, ItemDefinition>(_items.Count);
                    foreach (var item in _items)
                    {
                        if (item != null)
                        {
                            _byId[item.Id] = item;
                        }
                    }
                }

                return _byId;
            }
        }

        private void OnValidate()
        {
            // l'elenco è cambiato nell'editor: l'indice va rifatto
            _byId = null;
        }
    }
}
