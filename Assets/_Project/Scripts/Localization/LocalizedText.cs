using TMPro;
using UnityEngine;

namespace DarkDescent.Localization
{
    /// <summary>
    /// Un'etichetta fissa della scena ("Inventory", "Weapon"…): mostra il testo della sua chiave
    /// nella lingua attiva e lo riscrive quando la lingua cambia. La lega il CompositionRoot.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("La chiave nella tabella delle stringhe (Data/Localization/Strings.csv).")]
        [SerializeField] private string _key;

        private TMP_Text _text;
        private Localizer _localizer;
        private bool _subscribed;

        public string Key => _key;

        /// <summary>Come i pannelli: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(Localizer localizer)
        {
            Unsubscribe();
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _localizer == null)
            {
                return;
            }

            _localizer.LanguageChanged += Refresh;
            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _localizer.LanguageChanged -= Refresh;
            _subscribed = false;
        }

        private void Refresh()
        {
            _text.text = _localizer.Get(_key);
        }
    }
}
