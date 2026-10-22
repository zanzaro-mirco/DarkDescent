using System;
using System.Text;
using DarkDescent.Localization;
using DarkDescent.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// La vittoria (D9 della M10): arrivati in città dal portale della vittoria, il gioco si ferma e
    /// mostra tempo di gioco, livello, nemici uccisi, morti e oro raccolto. Poi i riconoscimenti, presi
    /// dalla copia di <c>CREDITS.md</c>, scorrono dal basso; finiti, o con il pulsante, si torna al menu.
    /// Il salvataggio resta: con Continua si riprende in città.
    /// </summary>
    [DisallowMultipleComponent]
    public class VictoryScreen : MonoBehaviour
    {
        // il colore degli autori, quello dei suggerimenti dei tooltip
        private const string AuthorColor = "#A89F8C";

        [Tooltip("Il riquadro con le statistiche; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _window;

        [SerializeField] private TMP_Text _stats;

        [SerializeField] private Button _creditsButton;

        [Tooltip("Il riquadro dei riconoscimenti, a tutto schermo.")]
        [SerializeField] private GameObject _creditsWindow;

        [Tooltip("La finestra da cui si vedono i riconoscimenti: taglia il testo che sale.")]
        [SerializeField] private RectTransform _creditsViewport;

        [Tooltip("Il testo che sale, ancorato in alto al centro della finestra.")]
        [SerializeField] private TMP_Text _creditsText;

        [SerializeField] private Button _backButton;

        [Tooltip("La copia di CREDITS.md (DarkDescent → Aggiorna i riconoscimenti).")]
        [SerializeField] private TextAsset _creditsSource;

        [Tooltip("Pixel al secondo, in tempo reale: il gioco è fermo.")]
        [SerializeField, Min(1f)] private float _scrollSpeed = 45f;

        private Localizer _localizer;
        private RectTransform _creditsRect;
        private float _creditsHeight;

        /// <summary>Riconoscimenti finiti o saltati: chi tiene la partita torna al menu.</summary>
        public event Action Finished;

        public bool IsOpen => _window.activeSelf || _creditsWindow.activeSelf;

        public bool IsShowingCredits => _creditsWindow.activeSelf;

        public string StatsText => _stats.text;

        public string CreditsText => _creditsText.text;

        public Button CreditsButton => _creditsButton;

        public Button BackButton => _backButton;

        public void Bind(Localizer localizer)
        {
            _localizer = localizer;
        }

        /// <summary>Ferma il gioco e mostra i contatori della partita vinta.</summary>
        /// <param name="unlockedKey">Il nome della difficoltà che si apre (D15 della M14); null se nessuna.</param>
        public void Show(RunStats stats, int level, string unlockedKey = null)
        {
            var builder = new StringBuilder();
            builder.AppendLine(_localizer.Format(TextKeys.VictoryTime, RunStats.FormatTime(stats.PlayTime)));
            builder.AppendLine(_localizer.Format(TextKeys.VictoryLevel, level));
            builder.AppendLine(_localizer.Format(TextKeys.VictoryKills, stats.Kills));
            builder.AppendLine(_localizer.Format(TextKeys.VictoryDeaths, stats.Deaths));
            builder.Append(_localizer.Format(TextKeys.VictoryGold, stats.GoldCollected));
            if (!string.IsNullOrEmpty(unlockedKey))
            {
                builder.AppendLine();
                builder.AppendLine();
                builder.Append(_localizer.Format(TextKeys.VictoryUnlocked, _localizer.Get(unlockedKey)));
            }
            _stats.text = builder.ToString();
            Time.timeScale = 0f;
            _creditsWindow.SetActive(false);
            _window.SetActive(true);
        }

        /// <summary>Dalle statistiche ai riconoscimenti, che partono da sotto il bordo.</summary>
        public void ShowCredits()
        {
            _creditsText.text = ComposeCredits();
            _window.SetActive(false);
            _creditsWindow.SetActive(true);
            _creditsText.ForceMeshUpdate();
            _creditsHeight = _creditsText.preferredHeight;
            _creditsRect.sizeDelta = new Vector2(_creditsRect.sizeDelta.x, _creditsHeight);
            _creditsRect.anchoredPosition = new Vector2(0f, -_creditsViewport.rect.height);
        }

        /// <summary>Chiude tutto e rimette il tempo: il gioco torna al menu.</summary>
        public void Finish()
        {
            if (!IsOpen)
            {
                return;
            }

            _window.SetActive(false);
            _creditsWindow.SetActive(false);
            Time.timeScale = 1f;
            Finished?.Invoke();
        }

        private string ComposeCredits()
        {
            var builder = new StringBuilder();
            builder.Append("<size=160%>DarkDescent</size>\n").Append(_localizer.Get(TextKeys.CreditsAuthor)).Append("\n\n\n");
            builder.Append("<size=120%>").Append(_localizer.Get(TextKeys.CreditsAssets)).Append("</size>\n\n");
            foreach (var entry in CreditsTable.Parse(_creditsSource != null ? _creditsSource.text : null))
            {
                builder.Append(entry.Work).Append("\n<size=80%><color=").Append(AuthorColor).Append('>')
                    .Append(entry.Author).Append("</color></size>\n\n");
            }

            builder.Append('\n').Append(_localizer.Get(TextKeys.CreditsOwn)).Append("\n\n\n\n");
            builder.Append("<size=130%>").Append(_localizer.Get(TextKeys.CreditsThanks)).Append("</size>");
            return builder.ToString();
        }

        private void Awake()
        {
            _creditsRect = _creditsText.rectTransform;
            _window.SetActive(false);
            _creditsWindow.SetActive(false);
        }

        private void OnEnable()
        {
            _creditsButton.onClick.AddListener(ShowCredits);
            _backButton.onClick.AddListener(Finish);
        }

        private void OnDisable()
        {
            _creditsButton.onClick.RemoveListener(ShowCredits);
            _backButton.onClick.RemoveListener(Finish);

            // spenta a metà (cambio scena): il tempo non deve restare fermo
            if (IsOpen)
            {
                Time.timeScale = 1f;
            }
        }

        // sale in tempo reale finché l'ultima riga non è uscita dall'alto
        private void Update()
        {
            if (!_creditsWindow.activeSelf)
            {
                return;
            }

            var position = _creditsRect.anchoredPosition;
            position.y += _scrollSpeed * Time.unscaledDeltaTime;
            _creditsRect.anchoredPosition = position;
            if (position.y > _creditsHeight)
            {
                Finish();
            }
        }
    }
}
