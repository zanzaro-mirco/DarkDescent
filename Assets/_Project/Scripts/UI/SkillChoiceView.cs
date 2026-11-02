using System;
using DarkDescent.Magic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Una scelta del menu delle abilità (D18 della M20): l'icona nella cornice d'oro, o il nome finché
    /// le icone non ci sono, il nome sotto, e la cornice accesa se è l'abilità già nella casella.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class SkillChoiceView : MonoBehaviour
    {
        [SerializeField] private Image _icon;

        [Tooltip("Il nome dentro la cornice, quando l'abilità non ha un'icona.")]
        [SerializeField] private TMP_Text _inner;

        [SerializeField] private TMP_Text _name;

        [Tooltip("La cornice accesa sull'abilità già nella casella.")]
        [SerializeField] private Image _current;

        private Action _chosen;

        public SpellDefinition Spell { get; private set; }

        public bool IsCurrent => _current.enabled;

        public string Name => _name.text;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Click);
        }

        public void Show(SpellDefinition spell, string name, bool current, Action chosen)
        {
            Spell = spell;
            _chosen = chosen;
            _icon.sprite = spell.Icon;
            _icon.enabled = spell.Icon != null;
            _inner.text = name;
            _inner.enabled = spell.Icon == null;
            _name.text = name;
            _current.enabled = current;
        }

        /// <summary>Il click sulla scelta: per i test, senza passare dal sistema degli eventi.</summary>
        public void Click()
        {
            _chosen?.Invoke();
        }
    }
}
