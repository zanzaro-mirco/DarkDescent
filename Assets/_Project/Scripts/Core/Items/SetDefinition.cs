using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un set (D4 e D10 della M16): il nome, i pezzi e i bonus per numero di pezzi indossati. Ogni
    /// pezzo è una <see cref="UniqueDefinition"/> che riferisce questo set. I bonus li accende
    /// <see cref="SetBonuses"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Set", menuName = "DarkDescent/Items/Set")]
    public sealed class SetDefinition : ScriptableObject
    {
        [Tooltip("Generato alla creazione e mai più cambiato. Duplicando l'asset va rigenerato.")]
        [SerializeField] private string _id;

        [Tooltip("La chiave del nome nella tabella delle stringhe, come set.warden.")]
        [SerializeField] private string _nameKey;

        [SerializeField] private List<UniqueDefinition> _pieces = new List<UniqueDefinition>();

        [Tooltip("In ordine di pezzi crescente.")]
        [SerializeField] private List<SetBonus> _bonuses = new List<SetBonus>();

        public string Id => _id;

        public string NameKey => _nameKey;

        public IReadOnlyList<UniqueDefinition> Pieces => _pieces;

        public IReadOnlyList<SetBonus> Bonuses => _bonuses;

        /// <summary>Il livello a cui i bonus valgono il valore basso: il più basso tra quelli dei pezzi.</summary>
        public int MinLevel
        {
            get
            {
                int level = UniqueDefinition.TopLevel;
                foreach (var piece in _pieces)
                {
                    if (piece != null)
                    {
                        level = Math.Min(level, piece.MinLevel);
                    }
                }

                return level;
            }
        }

        private void OnValidate()
        {
            EnsureId();
        }

        private void Reset()
        {
            EnsureId();
        }

        private void EnsureId()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = Guid.NewGuid().ToString("N");
            }
        }
    }
}
