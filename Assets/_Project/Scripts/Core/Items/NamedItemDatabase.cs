using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Tutti gli unici e i pezzi di set, per ID (D10 della M16): da qui il loot sceglie quello della
    /// base caduta, e un oggetto caricato da file ritrova il suo. L'elenco lo tiene aggiornato il menu
    /// DarkDescent → Oggetti → Aggiorna il database, come gli altri.
    /// </summary>
    [CreateAssetMenu(fileName = "NamedItemDatabase", menuName = "DarkDescent/Items/Named Item Database")]
    public sealed class NamedItemDatabase : ScriptableObject
    {
        [SerializeField] private List<UniqueDefinition> _items = new List<UniqueDefinition>();

        // indice ricavato dall'elenco, non stato di gioco: si ricostruisce da solo se manca
        [NonSerialized] private Dictionary<string, UniqueDefinition> _byId;

        public IReadOnlyList<UniqueDefinition> Items => _items;

        public bool TryGet(string id, out UniqueDefinition named)
        {
            if (string.IsNullOrEmpty(id))
            {
                named = null;
                return false;
            }

            if (_byId == null)
            {
                _byId = new Dictionary<string, UniqueDefinition>(_items.Count);
                foreach (var item in _items)
                {
                    if (item != null)
                    {
                        _byId[item.Id] = item;
                    }
                }
            }

            return _byId.TryGetValue(id, out named);
        }

        /// <summary>
        /// Gli unici (o i pezzi di set) che possono nascere da questa base a questo livello, in
        /// <paramref name="into"/>, svuotata prima, nell'ordine dell'elenco.
        /// </summary>
        public void Candidates(ItemDefinition item, int itemLevel, Rarity rarity, List<UniqueDefinition> into)
        {
            into.Clear();
            foreach (var named in _items)
            {
                if (named != null && named.Rarity == rarity && named.AllowedOn(item, itemLevel))
                {
                    into.Add(named);
                }
            }
        }

        private void OnValidate()
        {
            _byId = null;
        }
    }
}
