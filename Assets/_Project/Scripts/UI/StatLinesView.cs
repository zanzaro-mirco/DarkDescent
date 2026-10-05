using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkDescent.UI
{
    /// <summary>
    /// Una zona invisibile sopra le righe del pannello del personaggio (D16 della scheda M6): trasforma
    /// il punto del cursore nella riga della colonna dei nomi e la passa al pannello. Un solo
    /// bersaglio dei raggi per tutte le righe, come la griglia dell'inventario per le celle.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class StatLinesView : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler
    {
        [SerializeField] private CharacterPanel _panel;

        [Tooltip("La colonna dei nomi: le sue righe decidono quale statistica c'è sotto il cursore.")]
        [SerializeField] private TMP_Text _names;

        // il movimento è un evento del modulo UI, non un controllo per frame; il pannello agisce
        // solo quando la riga cambia
        public void OnPointerMove(PointerEventData eventData)
        {
            _panel.HoverLine(TMP_TextUtilities.FindIntersectingLine(_names, eventData.position, eventData.enterEventCamera));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _panel.HoverLine(-1);
        }
    }
}
