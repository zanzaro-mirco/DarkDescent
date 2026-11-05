using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il riquadro dei suggerimenti (D12 della M23): un titolo in oro, una o due righe e, in fondo, una barra sottile
    /// che si accorcia con gli 8 secondi che restano. Sta in basso al centro, sopra la cintura, e non ruba il click:
    /// nessun elemento riceve raggi. Lo accende e lo spegne il <see cref="HintDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class HintBox : MonoBehaviour
    {
        [Tooltip("Il riquadro vero e proprio, acceso e spento; questo oggetto resta acceso.")]
        [SerializeField] private GameObject _body;

        [SerializeField] private TMP_Text _title;

        [SerializeField] private TMP_Text _text;

        [Tooltip("La barra sottile dei secondi che restano: si accorcia da destra spostando l'ancora.")]
        [SerializeField] private RectTransform _timer;

        public bool IsShown => _body.activeSelf;

        public string Title => _title.text;

        public string Text => _text.text;

        /// <summary>La parte della barra ancora piena, per i test.</summary>
        public float Progress => _timer.anchorMax.x;

        private void Awake()
        {
            _body.SetActive(false);
        }

        public void Show(string title, string text)
        {
            _title.text = title;
            _text.text = text;
            _body.SetActive(true);
        }

        public void SetProgress(float share)
        {
            _timer.anchorMax = new Vector2(Mathf.Clamp01(share), 1f);
        }

        public void Hide()
        {
            _body.SetActive(false);
        }
    }
}
