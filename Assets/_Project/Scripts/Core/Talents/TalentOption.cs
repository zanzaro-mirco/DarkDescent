using System;
using DarkDescent.Magic;
using UnityEngine;

namespace DarkDescent.Talents
{
    /// <summary>
    /// Quello che si prende con un nodo (M20): un'abilità da mettere nella barra, o dei passivi. Un
    /// nodo normale ne ha una; un nodo a scelta due, e se ne prende una sola.
    /// </summary>
    [Serializable]
    public sealed class TalentOption
    {
        [Tooltip("La chiave del nome nella tabella delle stringhe, come talent.strong_arm; la descrizione aggiunge .desc.")]
        [SerializeField] private string _nameKey;

        [SerializeField] private Sprite _icon;

        [Tooltip("L'abilità che il nodo insegna; vuoto per un passivo.")]
        [SerializeField] private SpellDefinition _ability;

        [SerializeField] private TalentEffect[] _effects = Array.Empty<TalentEffect>();

        /// <summary>Per il serializzatore di Unity.</summary>
        public TalentOption()
        {
        }

        public TalentOption(string nameKey, SpellDefinition ability, params TalentEffect[] effects)
        {
            _nameKey = nameKey;
            _ability = ability;
            _effects = effects ?? Array.Empty<TalentEffect>();
        }

        public string NameKey => _nameKey;
        public Sprite Icon => _icon;
        public SpellDefinition Ability => _ability;
        public TalentEffect[] Effects => _effects ?? Array.Empty<TalentEffect>();
    }
}
