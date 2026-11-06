using System;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il pannello di lettura (D9 della M25): un titolo e un testo breve del mondo, quelli di un leggio, di una lapide o di un diario, o le parole di un
    /// prigioniero appena liberato. Si chiude con il pulsante o con Esc. Non ferma il gioco da solo; la pausa lo ferma.
    /// Cambiando la lingua con il pannello aperto, si rilegge nella lingua nuova.
    /// </summary>
    [DisallowMultipleComponent]
    public class LorePanel : MonoBehaviour
    {
        [Tooltip("Il riquadro da mostrare; il componente sta sul padre, sempre acceso. Lo sfondo scuro a tutto schermo non lascia passare i click.")]
        [SerializeField] private GameObject _root;

        [SerializeField] private TMP_Text _title;

        [SerializeField] private TMP_Text _body;

        [SerializeField] private Button _close;

        private Localizer _localizer;
        private string _titleKey;
        private string _bodyKey;
        private bool _subscribed;

        /// <summary>Aperto o chiuso, per chi deve sapere se c'è un pannello davanti.</summary>
        public event Action<bool> OpenChanged;

        public bool IsOpen => _root.activeSelf;

        public Button CloseButton => _close;

        public string TitleText => _title.text;

        public string BodyText => _body.text;

        public void Bind(Localizer localizer)
        {
            Unsubscribe();
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Mostra il testo di quelle due chiavi nella lingua attiva.</summary>
        public void Open(string titleKey, string bodyKey)
        {
            if (_localizer == null)
            {
                return;
            }

            _titleKey = titleKey;
            _bodyKey = bodyKey;
            Refresh();
            bool wasOpen = IsOpen;
            _root.SetActive(true);
            if (!wasOpen)
            {
                OpenChanged?.Invoke(true);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _root.SetActive(false);
            OpenChanged?.Invoke(false);
        }

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            _close.onClick.AddListener(Close);
            Subscribe();
        }

        private void OnDisable()
        {
            _close.onClick.RemoveListener(Close);
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
            if (_localizer == null || _titleKey == null)
            {
                return;
            }

            _title.text = _localizer.Get(_titleKey);
            _body.text = _localizer.Get(_bodyKey);
        }
    }
}
