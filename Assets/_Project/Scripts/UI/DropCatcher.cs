using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkDescent.UI
{
    /// <summary>
    /// Fondo trasparente a tutto schermo, acceso solo con un oggetto sul cursore: un click fuori
    /// dalle finestre lo lascia a terra. Così il click non arriva mai al mondo, e il cavaliere non
    /// parte verso il punto cliccato con un oggetto in mano.
    /// </summary>
    [DisallowMultipleComponent]
    public class DropCatcher : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private InventoryPanel _panel;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _panel.ClickOutside();
            }
        }
    }
}
