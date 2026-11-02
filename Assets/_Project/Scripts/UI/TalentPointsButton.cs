using DarkDescent.Localization;
using DarkDescent.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// L'avviso dei punti dell'albero da spendere (D10 della M20), accanto alla sfera del mana come quello
    /// degli attributi sta accanto a quella della vita: "Abilità +3", o "Scegli la specializzazione" dal
    /// livello 10. Compare con punti non spesi e sparisce quando finiscono o mentre il pannello è aperto;
    /// un click lo apre. Ascolta l'albero e il pannello, mai in Update.
    /// </summary>
    [DisallowMultipleComponent]
    public class TalentPointsButton : MonoBehaviour
    {
        [Tooltip("Il pulsante vero e proprio, acceso e spento; questo oggetto resta acceso per ascoltare.")]
        [SerializeField] private GameObject _body;

        [SerializeField] private Button _button;

        [SerializeField] private TMP_Text _label;

        private PlayerTalents _talents;
        private TalentPanel _panel;
        private Localizer _localizer;
        private bool _subscribed;

        public bool IsShown => _body.activeSelf;

        public string LabelText => _label.text;

        public Button Button => _button;

        /// <summary>Come i pannelli: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerTalents talents, TalentPanel panel, Localizer localizer)
        {
            Unsubscribe();
            _talents = talents;
            _panel = panel;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
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
            if (_subscribed || _talents == null)
            {
                return;
            }

            _talents.Build.Changed += Refresh;
            _panel.OpenChanged += HandleOpenChanged;
            _localizer.LanguageChanged += Refresh;
            _button.onClick.AddListener(HandleClick);
            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _talents.Build.Changed -= Refresh;
            _panel.OpenChanged -= HandleOpenChanged;
            _localizer.LanguageChanged -= Refresh;
            _button.onClick.RemoveListener(HandleClick);
            _subscribed = false;
        }

        private void HandleOpenChanged(bool open)
        {
            Refresh();
        }

        private void HandleClick()
        {
            _panel.Open();
        }

        private void Refresh()
        {
            var build = _talents.Build;
            bool choose = build.NeedsSpecialization;
            int points = build.UnspentPoints;
            bool show = (choose || points > 0) && !_panel.IsOpen;
            _body.SetActive(show);
            if (show)
            {
                _label.text = choose ? _localizer.Get("talents.spec_button") : _localizer.Format("talents.points_button", points);
            }
        }
    }
}
