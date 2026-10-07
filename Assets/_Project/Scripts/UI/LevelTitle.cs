using System.Collections;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Dove si trova il cavaliere (prova della M7): entrando in un livello "Cripta – Livello 3" compare
    /// grande in alto e sfuma; piccolo, resta sotto la minimappa. Lo chiama il composition root a ogni
    /// ingresso; la lingua la riscrive quando cambia.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelTitle : MonoBehaviour
    {
        [Tooltip("La scritta grande, che compare entrando e sfuma.")]
        [SerializeField] private TMP_Text _banner;

        [SerializeField] private CanvasGroup _bannerGroup;

        [Tooltip("La scritta piccola sotto la minimappa, che resta.")]
        [SerializeField] private TMP_Text _corner;

        [Tooltip("Secondi a piena luce prima di sfumare.")]
        [SerializeField, Min(0f)] private float _hold = 2.5f;

        [SerializeField, Min(0.05f)] private float _fade = 1.5f;

        private Localizer _localizer;
        private bool _subscribed;
        private string _nameKey;
        private int _depth;
        private Coroutine _fading;

        /// <summary>Il testo mostrato, per i test; vuoto senza livello.</summary>
        public string Text => _corner.text;

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

        /// <summary>
        /// Il livello in cui si è entrati. Senza nome (una scena di prova) le scritte si spengono.
        /// </summary>
        public void Show(string nameKey, int depth)
        {
            _nameKey = nameKey;
            _depth = depth;
            Refresh();

            if (_fading != null)
            {
                StopCoroutine(_fading);
                _fading = null;
            }

            bool named = !string.IsNullOrEmpty(nameKey);
            _bannerGroup.alpha = named ? 1f : 0f;
            if (named && isActiveAndEnabled)
            {
                _fading = StartCoroutine(FadeBanner());
            }
        }

        private void Awake()
        {
            _bannerGroup.alpha = 0f;
            _banner.text = string.Empty;
            _corner.text = string.Empty;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            _fading = null;
            _bannerGroup.alpha = 0f;
        }

        // tempo reale: durante l'hit stop la scritta continua a sfumare
        private IEnumerator FadeBanner()
        {
            yield return new WaitForSecondsRealtime(_hold);
            for (float time = 0f; time < _fade; time += Time.unscaledDeltaTime)
            {
                _bannerGroup.alpha = 1f - time / _fade;
                yield return null;
            }

            _bannerGroup.alpha = 0f;
            _fading = null;
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
            string text = _localizer == null || string.IsNullOrEmpty(_nameKey)
                ? string.Empty
                : _localizer.Format(TextKeys.LevelTitle, _localizer.Get(_nameKey), _depth);
            _banner.text = text;
            _corner.text = text;
        }
    }
}
