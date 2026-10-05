using System.Text;
using DarkDescent.Items;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il riquadro con la descrizione di un oggetto. Sta sopra l'oggetto, o sotto se sopra non c'è
    /// spazio, oppure accanto a un altro tooltip per il confronto (D9 della M5), e non esce mai
    /// dallo schermo. Lo accende e lo spegne chi lo usa: da solo non guarda niente. Dalla M6 mostra
    /// anche un testo libero, per le righe del pannello del personaggio (D16).
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

        [Tooltip("Distanza tra il riquadro e l'oggetto descritto, o l'altro riquadro.")]
        [SerializeField, Min(0f)] private float _gap = 6f;

        private readonly StringBuilder _builder = new StringBuilder(160);
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _container;

        public bool IsShowing => _box.gameObject.activeSelf;

        public string Text => _text.text;

        /// <summary>Il riquadro, per controllare che resti dentro lo schermo.</summary>
        public RectTransform Box => _box;

        /// <summary>Descrive l'oggetto accanto a <paramref name="target"/>, il rettangolo che lo mostra.</summary>
        public void Show(ItemInstance item, bool meetsRequirements, Localizer localizer, RectTransform target)
        {
            Fill(item, meetsRequirements, localizer, null);
            GetLocalRect(target, out Vector2 min, out Vector2 max);
            PlaceAround(min, max);
        }

        /// <summary>Descrive l'oggetto accanto a un altro tooltip, a sinistra o a destra, con un titolo.</summary>
        public void ShowBeside(ItemInstance item, bool meetsRequirements, Localizer localizer, ItemTooltip anchor, string header)
        {
            Fill(item, meetsRequirements, localizer, header);
            GetLocalRect(anchor.Box, out Vector2 min, out Vector2 max);
            PlaceBeside(min, max);
        }

        /// <summary>
        /// Un testo già pronto, in rich text, a destra di <paramref name="beside"/> e centrato in
        /// altezza su <paramref name="worldPoint"/>: le righe del pannello del personaggio.
        /// </summary>
        public void ShowText(string text, RectTransform beside, Vector3 worldPoint)
        {
            _text.SetText(text);
            Resize();
            GetLocalRect(beside, out _, out Vector2 max);
            Vector2 point = _container.InverseTransformPoint(worldPoint);
            Place(max.x + _gap + _box.sizeDelta.x / 2f, point.y);
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

        private void Fill(ItemInstance item, bool meetsRequirements, Localizer localizer, string header)
        {
            ItemDescription.Write(_builder, item, meetsRequirements, localizer, header);
            _text.SetText(_builder);
            Resize();
        }

        // misurato dal testo: niente layout group da ricostruire
        private void Resize()
        {
            _box.gameObject.SetActive(true);
            _box.sizeDelta = _text.GetPreferredValues() + 2f * _padding;
        }

        // gli angoli di un rettangolo nello spazio del contenitore: vale in Overlay e con una camera
        private void GetLocalRect(RectTransform target, out Vector2 min, out Vector2 max)
        {
            target.GetWorldCorners(_corners);
            min = _container.InverseTransformPoint(_corners[0]);
            max = _container.InverseTransformPoint(_corners[2]);
        }

        private void PlaceAround(Vector2 min, Vector2 max)
        {
            Rect bounds = _container.rect;
            Vector2 half = _box.sizeDelta / 2f;

            // sopra l'oggetto; se sbatte contro il bordo in alto, sotto
            float y = max.y + _gap + half.y;
            if (y + half.y > bounds.yMax)
            {
                y = min.y - _gap - half.y;
            }

            Place((min.x + max.x) / 2f, y);
        }

        private void PlaceBeside(Vector2 min, Vector2 max)
        {
            Rect bounds = _container.rect;
            Vector2 half = _box.sizeDelta / 2f;

            // a sinistra dell'altro, allineato in alto; se a sinistra non c'è spazio, a destra
            float x = min.x - _gap - half.x;
            if (x - half.x < bounds.xMin)
            {
                x = max.x + _gap + half.x;
            }

            Place(x, max.y - half.y);
        }

        private void Place(float x, float y)
        {
            Rect bounds = _container.rect;
            Vector2 half = _box.sizeDelta / 2f;
            x = Mathf.Clamp(x, bounds.xMin + half.x, Mathf.Max(bounds.xMin + half.x, bounds.xMax - half.x));
            y = Mathf.Clamp(y, bounds.yMin + half.y, Mathf.Max(bounds.yMin + half.y, bounds.yMax - half.y));

            // il riquadro è ancorato al centro del contenitore
            _box.anchoredPosition = new Vector2(x, y) - bounds.center;
        }
    }
}
