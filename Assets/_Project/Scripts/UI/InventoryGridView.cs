using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkDescent.UI
{
    /// <summary>
    /// Lo sfondo della griglia: trasforma il punto premuto nella cella e la passa al pannello. Un
    /// solo bersaglio dei raggi per tutta la griglia; le immagini degli oggetti non ne hanno.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class InventoryGridView : MonoBehaviour, IPointerDownHandler, IPointerMoveHandler, IPointerExitHandler
    {
        [SerializeField] private InventoryPanel _panel;

        // alla pressione e non al click: il click di uGUI arriva al rilascio, e prendere un oggetto
        // deve essere immediato come in Diablo
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            var rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                _panel.ClickCell(CellAt(local, rect, _panel.CellSize));
            }
        }

        // il movimento è un evento del modulo UI, non un controllo per frame; il pannello agisce
        // solo quando la cella cambia
        public void OnPointerMove(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.enterEventCamera, out Vector2 local))
            {
                _panel.HoverCell(CellAt(local, rect, _panel.CellSize));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _panel.HoverCell(null);
        }

        /// <summary>La cella sotto un punto locale del rettangolo, con la cella (0, 0) in alto a sinistra.</summary>
        public static Vector2Int CellAt(Vector2 local, RectTransform rect, float cellSize)
        {
            // dal punto locale (riferito al pivot) all'angolo in alto a sinistra
            Vector2 fromTopLeft = local - new Vector2(rect.rect.xMin, rect.rect.yMax);
            return new Vector2Int(Mathf.FloorToInt(fromTopLeft.x / cellSize), Mathf.FloorToInt(-fromTopLeft.y / cellSize));
        }
    }
}
