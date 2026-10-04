using System.Text;
using DarkDescent.Items;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il riquadro con la descrizione dell'oggetto sotto il cursore. Sta sopra l'oggetto, o sotto se
    /// sopra non c'è spazio, e non esce mai dallo schermo. Lo accende e lo spegne l'inventario: da
    /// solo non guarda niente.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ItemTooltip : MonoBehaviour
    {
        [Tooltip("Il riquadro: sfondo senza bersaglio dei raggi, con il testo come figlio.")]
        [SerializeField] private RectTransform _box;

        [SerializeField] private TMP_Text _text;

        [Tooltip("Spazio tra il testo e il bordo del riquadro, in pixel di riferimento.")]
        [SerializeField] private Vector2 _padding = new Vector2(14f, 10f);

        [Tooltip("Distanza tra il riquadro e l'oggetto descritto.")]
        [SerializeField, Min(0f)] private float _gap = 6f;

        private readonly StringBuilder _builder = new StringBuilder(96);
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _container;

        public bool IsShowing => _box.gameObject.activeSelf;

        public string Text => _text.text;

        /// <summary>Il riquadro, per controllare che resti dentro lo schermo.</summary>
        public RectTransform Box => _box;

        /// <summary>Descrive l'oggetto accanto a <paramref name="target"/>, il rettangolo che lo mostra.</summary>
        public void Show(ItemDefinition definition, bool meetsRequirements, Localizer localizer, RectTransform target)
        {
            ItemDescription.Write(_builder, definition, meetsRequirements, localizer);
            _text.SetText(_builder);
            _box.gameObject.SetActive(true);

            // misurato dal testo: niente layout group da ricostruire
            _box.sizeDelta = _text.GetPreferredValues() + 2f * _padding;
            Place(target);
        }

        public void Hide()
        {
            _box.gameObject.SetActive(false);
        }

        private void Awake()
        {
            _container = (RectTransform)transform;
            _box.anchorMin = _box.anchorMax = new Vector2(0.5f, 0.5f);
            _box.pivot = new Vector2(0.5f, 0.5f);
            Hide();
        }

        private void Place(RectTransform target)
        {
            // gli angoli del bersaglio nello spazio del contenitore: vale in Overlay e con una camera
            target.GetWorldCorners(_corners);
            Vector2 min = _container.InverseTransformPoint(_corners[0]);
            Vector2 max = _container.InverseTransformPoint(_corners[2]);

            Rect bounds = _container.rect;
            Vector2 half = _box.sizeDelta / 2f;

            // sopra l'oggetto; se sbatte contro il bordo in alto, sotto
            float y = max.y + _gap + half.y;
            if (y + half.y > bounds.yMax)
            {
                y = min.y - _gap - half.y;
            }

            float x = (min.x + max.x) / 2f;
            x = Mathf.Clamp(x, bounds.xMin + half.x, Mathf.Max(bounds.xMin + half.x, bounds.xMax - half.x));
            y = Mathf.Clamp(y, bounds.yMin + half.y, Mathf.Max(bounds.yMin + half.y, bounds.yMax - half.y));

            // il riquadro è ancorato al centro del contenitore
            _box.anchoredPosition = new Vector2(x, y) - bounds.center;
        }
    }
}
