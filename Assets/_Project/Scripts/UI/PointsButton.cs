using DarkDescent.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il pulsante dorato dei punti da spendere sopra la sfera della vita (D1 della M10), come il
    /// "Level Up" di Diablo 1: compare con punti non spesi e sparisce quando finiscono o mentre il
    /// pannello del personaggio è aperto, perché lì ci sono già i "+". Un click apre il pannello.
    /// Ascolta la crescita e il pannello, mai in Update.
    /// </summary>
    [DisallowMultipleComponent]
    public class PointsButton : MonoBehaviour
    {
        [Tooltip("Il pulsante vero e proprio, acceso e spento; questo oggetto resta acceso per ascoltare.")]
        [SerializeField] private GameObject _body;

        [SerializeField] private Button _button;

        [SerializeField] private TMP_Text _label;

        private CharacterProgress _progress;
        private CharacterPanel _panel;
        private bool _subscribed;

        public bool IsShown => _body.activeSelf;

        public string LabelText => _label.text;

        public Button Button => _button;

        /// <summary>Come i pannelli: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(CharacterProgress progress, CharacterPanel panel)
        {
            Unsubscribe();
            _progress = progress;
            _panel = panel;
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
            if (_subscribed || _progress == null)
            {
                return;
            }

            _progress.Changed += Refresh;
            _panel.OpenChanged += HandleOpenChanged;
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

            _progress.Changed -= Refresh;
            _panel.OpenChanged -= HandleOpenChanged;
            _button.onClick.RemoveListener(HandleClick);
            _subscribed = false;
        }

        private void HandleOpenChanged(bool open)
        {
            Refresh();
        }

        private void HandleClick()
        {
            if (!_panel.IsOpen)
            {
                _panel.Toggle();
            }
        }

        private void Refresh()
        {
            int points = _progress.UnspentPoints;
            bool show = points > 0 && !_panel.IsOpen;
            _body.SetActive(show);
            if (show)
            {
                // SetText con il formato non alloca stringhe
                _label.SetText("+{0}", points);
            }
        }
    }
}
