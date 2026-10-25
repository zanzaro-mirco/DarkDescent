using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un unico o un pezzo di set (D3, D4 e D10 della M16), scritto a mano: un nome suo, la base da
    /// cui nasce (modello, icona, danno o Armatura), il livello minimo e le proprietà fisse, con i
    /// valori che crescono con il livello. Con un set è un pezzo, in verde; senza è un unico, in oro.
    /// Gli oggetti lo riferiscono con l'id, come le basi.
    /// </summary>
    [CreateAssetMenu(fileName = "Unique", menuName = "DarkDescent/Items/Unique or Set Piece")]
    public sealed class UniqueDefinition : ScriptableObject
    {
        /// <summary>Il livello a cui le proprietà valgono il loro valore più alto: la zona dell'Abisso VI.</summary>
        public const int TopLevel = 55;

        [Tooltip("Generato alla creazione e mai più cambiato: è quello che salvano gli oggetti. Duplicando l'asset va rigenerato.")]
        [SerializeField] private string _id;

        [Tooltip("La chiave del nome nella tabella delle stringhe, come unique.kings_cleaver.")]
        [SerializeField] private string _nameKey;

        [Tooltip("La base: modello, icona, slot e numeri dell'oggetto.")]
        [SerializeField] private ItemDefinition _base;

        [Tooltip("Il livello dell'oggetto da cui può cadere, e a cui le proprietà valgono il valore basso.")]
        [SerializeField, Range(1, TopLevel)] private int _minLevel = 10;

        [SerializeField] private List<NamedProperty> _properties = new List<NamedProperty>();

        [Tooltip("Il set di cui è un pezzo; vuoto per un unico.")]
        [SerializeField] private SetDefinition _set;

        public string Id => _id;

        public string NameKey => _nameKey;

        public ItemDefinition Base => _base;

        public int MinLevel => _minLevel;

        public IReadOnlyList<NamedProperty> Properties => _properties;

        /// <summary>Il set di cui è un pezzo, o null per un unico.</summary>
        public SetDefinition Set => _set;

        public Rarity Rarity => _set != null ? Rarity.Set : Rarity.Unique;

        /// <summary>Se può cadere da un oggetto di questa base e di questo livello.</summary>
        public bool AllowedOn(ItemDefinition item, int itemLevel)
        {
            return item != null && item == _base && itemLevel >= _minLevel;
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
