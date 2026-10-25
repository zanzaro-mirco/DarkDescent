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

        [Tooltip("Oltre questa larghezza il nome va su due righe, divise a metà (M9, D1).")]
        [SerializeField, Min(100f)] private float _maxNameWidth = 480f;

        private readonly StringBuilder _builder = new StringBuilder(160);
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _container;

        public bool IsShowing => _box.gameObject.activeSelf;

        public string Text => _text.text;

        /// <summary>Il riquadro, per controllare che resti dentro lo schermo.</summary>
        public RectTransform Box => _box;

        /// <summary>Descrive l'oggetto accanto a <paramref name="target"/>, il rettangolo che lo mostra.</summary>
        /// <param name="trade">Le righe del mercante in fondo, con il negozio aperto (D5 della M10).</param>
        /// <param name="equipment">L'equipaggiamento del cavaliere, per i pezzi e i bonus di un set (D8 della M16).</param>
        public void Show(ItemInstance item, bool meetsRequirements, Localizer localizer, RectTransform target, bool equipHint = false, string trade = null, Equipment equipment = null)
        {
            Fill(item, meetsRequirements, localizer, null, equipHint, trade, equipment);
            GetLocalRect(target, out Vector2 min, out Vector2 max);
            PlaceAround(min, max);
        }

        /// <summary>
        /// Descrive l'oggetto accanto a un altro tooltip, con un titolo: a sinistra, o a destra se
        /// a sinistra non c'è spazio; con <paramref name="onRight"/> il contrario, per il secondo anello.
        /// </summary>
        public void ShowBeside(ItemInstance item, bool meetsRequirements, Localizer localizer, ItemTooltip anchor, string header, bool onRight = false, Equipment equipment = null)
        {
            Fill(item, meetsRequirements, localizer, header, false, null, equipment);
            GetLocalRect(anchor.Box, out Vector2 min, out Vector2 max);
            PlaceBeside(min, max, onRight);
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

        private void Fill(ItemInstance item, bool meetsRequirements, Localizer localizer, string header, bool equipHint, string trade, Equipment equipment)
        {
            // un nome più largo del massimo va su due righe: misurato con il font del tooltip, in grassetto come sarà
            bool twoLines = _text.GetPreferredValues("<b>" + ItemNamer.Name(item, localizer) + "</b>").x > _maxNameWidth;
            ItemDescription.Write(_builder, item, meetsRequirements, localizer, header, twoLines, equipHint, trade, equipment);
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

        private void PlaceBeside(Vector2 min, Vector2 max, bool onRight)
        {
            Rect bounds = _container.rect;
            Vector2 half = _box.sizeDelta / 2f;

            // dal lato chiesto, allineato in alto all'altro; se lì non c'è spazio, dall'altro lato
            float left = min.x - _gap - half.x;
            float right = max.x + _gap + half.x;
            bool leftFits = left - half.x >= bounds.xMin;
            bool rightFits = right + half.x <= bounds.xMax;
            float x = onRight ? (rightFits || !leftFits ? right : left) : (leftFits || !rightFits ? left : right);

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
