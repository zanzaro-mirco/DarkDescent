using System;
using System.Text;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// I riconoscimenti, presi dalla copia di <c>CREDITS.md</c>, che scorrono dal basso a tutto schermo.
    /// Dalla M17 si aprono dal menu iniziale (prima solo dopo la vittoria); finiti, o con il pulsante,
    /// si chiudono e il menu torna.
    /// </summary>
    [DisallowMultipleComponent]
    public class CreditsPanel : MonoBehaviour
    {
        // il colore degli autori, quello dei suggerimenti dei tooltip
        private const string AuthorColor = "#A89F8C";

        [Tooltip("Il riquadro dei riconoscimenti, a tutto schermo; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _window;

        [Tooltip("La finestra da cui si vedono i riconoscimenti: taglia il testo che sale.")]
        [SerializeField] private RectTransform _viewport;

        [Tooltip("Il testo che sale, ancorato in alto al centro della finestra.")]
        [SerializeField] private TMP_Text _text;

        [SerializeField] private Button _backButton;

        [Tooltip("La copia di CREDITS.md (DarkDescent → Aggiorna i riconoscimenti).")]
        [SerializeField] private TextAsset _source;

        [Tooltip("Pixel al secondo, in tempo reale: il menu può avere il gioco fermo.")]
        [SerializeField, Min(1f)] private float _scrollSpeed = 45f;

        private RectTransform _textRect;
        private float _height;

        /// <summary>Riconoscimenti finiti o saltati.</summary>
        public event Action Closed;

        public bool IsOpen => _window.activeSelf;

        public string Text => _text.text;

        public float Scroll => _textRect.anchoredPosition.y;

        public Button BackButton => _backButton;

        /// <summary>Compone i riconoscimenti nella lingua attiva e li fa partire da sotto il bordo.</summary>
        public void Open(Localizer localizer)
        {
            _text.text = Compose(localizer);
            _window.SetActive(true);
            _text.ForceMeshUpdate();
            _height = _text.preferredHeight;
            _textRect.sizeDelta = new Vector2(_textRect.sizeDelta.x, _height);
            _textRect.anchoredPosition = new Vector2(0f, -_viewport.rect.height);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _window.SetActive(false);
            Closed?.Invoke();
        }

        private string Compose(Localizer localizer)
        {
            var builder = new StringBuilder();
            builder.Append("<size=160%>DarkDescent</size>\n").Append(localizer.Get(TextKeys.CreditsAuthor)).Append("\n\n\n");
            builder.Append("<size=120%>").Append(localizer.Get(TextKeys.CreditsAssets)).Append("</size>\n\n");
            foreach (var entry in CreditsTable.Parse(_source != null ? _source.text : null))
            {
                builder.Append(entry.Work).Append("\n<size=80%><color=").Append(AuthorColor).Append('>')
                    .Append(entry.Author).Append("</color></size>\n\n");
            }

            builder.Append('\n').Append(localizer.Get(TextKeys.CreditsOwn)).Append("\n\n\n\n");
            builder.Append("<size=130%>").Append(localizer.Get(TextKeys.CreditsThanks)).Append("</size>");
            return builder.ToString();
        }

        private void Awake()
        {
            _textRect = _text.rectTransform;
            _window.SetActive(false);
        }

        private void OnEnable()
        {
            _backButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _backButton.onClick.RemoveListener(Close);
        }

        // sale in tempo reale finché l'ultima riga non è uscita dall'alto
        private void Update()
        {
            if (!_window.activeSelf)
            {
                return;
            }

            var position = _textRect.anchoredPosition;
            position.y += _scrollSpeed * Time.unscaledDeltaTime;
            _textRect.anchoredPosition = position;
            if (position.y > _height)
            {
                Close();
            }
        }
    }
}
