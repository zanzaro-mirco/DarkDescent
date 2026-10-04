using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Tutti gli affissi, per ID: il pool del generatore e il modo di ritrovare quelli di un oggetto
    /// caricato da file. L'elenco lo tiene aggiornato il menu DarkDescent → Oggetti → Aggiorna il database.
    /// </summary>
    [CreateAssetMenu(fileName = "AffixDatabase", menuName = "DarkDescent/Items/Affix Database")]
    public sealed class AffixDatabase : ScriptableObject
    {
        [SerializeField] private List<AffixDefinition> _affixes = new List<AffixDefinition>();

        // indice ricavato dall'elenco, non stato di gioco: si ricostruisce da solo se manca
        [NonSerialized] private Dictionary<string, AffixDefinition> _byId;

        public IReadOnlyList<AffixDefinition> Affixes => _affixes;

        public bool TryGet(string id, out AffixDefinition affix)
        {
            if (string.IsNullOrEmpty(id))
            {
                affix = null;
                return false;
            }

            if (_byId == null)
            {
                _byId = new Dictionary<string, AffixDefinition>(_affixes.Count);
                foreach (var entry in _affixes)
                {
                    if (entry != null)
                    {
                        _byId[entry.Id] = entry;
                    }
                }
            }

            return _byId.TryGetValue(id, out affix);
        }

        private void OnValidate()
        {
            _byId = null;
        }
    }
}
