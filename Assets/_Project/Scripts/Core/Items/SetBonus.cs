using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>Un bonus di set (D4 della M16): le proprietà che si accendono con almeno tanti pezzi indossati.</summary>
    [Serializable]
    public sealed class SetBonus
    {
        [Tooltip("Quanti pezzi diversi del set servono.")]
        [SerializeField, Min(2)] private int _pieces = 2;

        [SerializeField] private List<NamedProperty> _properties = new List<NamedProperty>();

        public SetBonus()
        {
        }

        public SetBonus(int pieces, IEnumerable<NamedProperty> properties)
        {
            _pieces = pieces;
            _properties.AddRange(properties);
        }

        public int Pieces => _pieces;

        public IReadOnlyList<NamedProperty> Properties => _properties;
    }
}
