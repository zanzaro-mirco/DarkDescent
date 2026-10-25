using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Una proprietà di un unico, di un pezzo di set o di un bonus di set (D6 della M16): l'effetto,
    /// come quello di un affisso, con un valore al livello minimo e uno al livello
    /// <see cref="UniqueDefinition.TopLevel"/>. In mezzo il valore va in linea retta, così lo stesso
    /// unico trovato più in basso è più forte.
    /// </summary>
    [Serializable]
    public sealed class NamedProperty
    {
        [SerializeField] private AffixEffect _effect;

        [Tooltip("Il valore al livello minimo dell'oggetto.")]
        [SerializeField] private int _low;

        [Tooltip("Il valore al livello 55.")]
        [SerializeField] private int _high;

        public NamedProperty()
        {
        }

        public NamedProperty(AffixEffect effect, int low, int high)
        {
            _effect = effect;
            _low = low;
            _high = high;
        }

        public AffixEffect Effect => _effect;

        public int Low => _low;

        public int High => _high;

        /// <summary>
        /// Il valore a questo livello, tra il minimo e <see cref="UniqueDefinition.TopLevel"/>, moltiplicato
        /// per <paramref name="factor"/> (il tiro del ±10%, 1 per i bonus dei set) e arrotondato. Un valore
        /// positivo resta almeno 1.
        /// </summary>
        public int ValueAt(int level, int minLevel, double factor = 1.0)
        {
            int top = UniqueDefinition.TopLevel;
            double t = top > minLevel ? (Math.Min(Math.Max(level, minLevel), top) - minLevel) / (double)(top - minLevel) : 1.0;
            int value = (int)Math.Round((_low + (_high - _low) * t) * factor, MidpointRounding.AwayFromZero);
            return _low > 0 ? Math.Max(1, value) : value;
        }
    }
}
