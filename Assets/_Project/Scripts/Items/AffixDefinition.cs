using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un prefisso o un suffisso (D3 della M5), immutabile: effetto, intervallo di valori interi,
    /// livello minimo dell'oggetto, oggetti ammessi e gruppo. Due affissi dello stesso gruppo non
    /// stanno sullo stesso oggetto. Il nome è una chiave della tabella delle stringhe; i prefissi
    /// hanno una forma per genere (chiave + ".f"), che compone il nome nelle lingue che lo vogliono.
    /// </summary>
    [CreateAssetMenu(fileName = "Affix", menuName = "DarkDescent/Items/Affix")]
    public sealed class AffixDefinition : ScriptableObject
    {
        [Tooltip("Generato alla creazione e mai più cambiato: è quello che salvano gli oggetti. Duplicando l'asset va rigenerato.")]
        [SerializeField] private string _id;

        [Tooltip("La chiave del nome nella tabella delle stringhe, come affix.sharp.")]
        [SerializeField] private string _nameKey;

        [SerializeField] private AffixKind _kind;

        [SerializeField] private AffixEffect _effect;

        [Tooltip("Valore minimo e massimo, estremi compresi.")]
        [SerializeField] private int _min = 1;

        [SerializeField] private int _max = 1;

        [Tooltip("Il livello dell'oggetto da cui l'affisso può comparire.")]
        [SerializeField, Min(1)] private int _minItemLevel = 1;

        [SerializeField] private AffixTargets _targets = AffixTargets.All;

        [Tooltip("Affissi dello stesso gruppo si escludono a vicenda sullo stesso oggetto (Affilato e Feroce).")]
        [SerializeField] private string _group;

        public string Id => _id;
        public string NameKey => _nameKey;
        public AffixKind Kind => _kind;
        public AffixEffect Effect => _effect;
        public int Min => _min;
        public int Max => _max;
        public int MinItemLevel => _minItemLevel;
        public AffixTargets Targets => _targets;
        public string Group => _group;

        /// <summary>Se può comparire su questo oggetto, a questo livello.</summary>
        public bool AllowedOn(ItemDefinition item, int itemLevel)
        {
            return item != null && (_targets & item.AffixTarget) != 0 && itemLevel >= _minItemLevel;
        }

        private void OnValidate()
        {
            EnsureId();
            _max = Math.Max(_max, _min);
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
