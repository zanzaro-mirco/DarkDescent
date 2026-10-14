using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkDescent.UI
{
    /// <summary>
    /// Quanto resta in una sfera, scritto sopra di lei mentre il mouse ci passa (passo 11.7 della
    /// M11): "45 / 80". La sfera gli passa i valori quando cambiano; il testo si riscrive solo se si
    /// vede.
    /// </summary>
    [DisallowMultipleComponent]
    public class OrbReadout : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Il testo al centro della sfera, spento a riposo e senza bersaglio dei raggi.")]
        [SerializeField] private TMP_Text _text;

        private int _current;
        private int _max;

        public bool IsShowing => _text.enabled;

        public string Text => _text.text;

        /// <summary>I valori della sfera: il rimanente arrotondato per eccesso, così chi è vivo non legge mai 0.</summary>
        public void SetValues(float current, float max)
        {
            _current = Mathf.CeilToInt(Mathf.Max(0f, current) - 0.001f);
            _max = Mathf.RoundToInt(max);
            if (_text.enabled)
            {
                Refresh();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Show(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Show(false);
        }

        /// <summary>Come il mouse che entra o esce: per i test.</summary>
        public void Show(bool show)
        {
            _text.enabled = show;
            if (show)
            {
                Refresh();
            }
        }

        private void Awake()
        {
            _text.enabled = false;
        }

        private void OnDisable()
        {
            _text.enabled = false;
        }

        private void Refresh()
        {
            _text.SetText("{0} / {1}", _current, _max);
        }
    }
}
