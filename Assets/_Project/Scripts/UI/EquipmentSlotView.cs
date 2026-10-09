using System.Collections;
using DarkDescent.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Uno slot dell'equipaggiamento: mostra l'icona di quello che c'è e inoltra al pannello
    /// pressione e passaggio del cursore. Quando ci si mette un oggetto, lampeggia d'oro: anche al
    /// posto di uno uguale si vede che il click è arrivato.
    /// </summary>
    [DisallowMultipleComponent]
    public class EquipmentSlotView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private InventoryPanel _panel;

        [SerializeField] private EquipSlot _slot;

        [Tooltip("L'icona dell'oggetto, senza bersaglio dei raggi: la pressione la prende lo sfondo dello slot.")]
        [SerializeField] private Image _icon;

        [Tooltip("Il lampo sopra lo slot quando ci si mette un oggetto, spento a riposo e senza bersaglio dei raggi.")]
        [SerializeField] private Image _flash;

        [Tooltip("Quanto dura il lampo, in secondi.")]
        [SerializeField, Min(0.05f)] private float _flashDuration = 0.35f;

        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.6f;

        private Coroutine _flashing;

        public EquipSlot Slot => _slot;

        public bool HasIcon => _icon.enabled;

        /// <summary>Se il lampo è acceso: per i test.</summary>
        public bool IsFlashing => _flash.enabled;

        /// <summary>Il lampo d'oro che si spegne in <see cref="_flashDuration"/>; un lampo nuovo riparte da capo.</summary>
        public void Flash()
        {
            if (_flashing != null)
            {
                StopCoroutine(_flashing);
            }

            _flashing = isActiveAndEnabled ? StartCoroutine(FadeFlash()) : null;
        }

        private void OnDisable()
        {
            _flash.enabled = false;
            _flashing = null;
        }

        // senza tempo scalato: con l'inventario aperto il gioco può essere fermo nell'hit stop
        private IEnumerator FadeFlash()
        {
            _flash.enabled = true;
            Color color = _flash.color;
            for (float time = 0f; time < _flashDuration; time += Time.unscaledDeltaTime)
            {
                float t = time / _flashDuration;
                color.a = _flashAlpha * (1f - t) * (1f - t);
                _flash.color = color;
                yield return null;
            }

            _flash.enabled = false;
            _flashing = null;
        }

        public void Show(ItemInstance item)
        {
            _icon.sprite = item?.Definition.Icon;
            _icon.enabled = item != null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _panel.ClickSlot(_slot);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _panel.HoverSlot(_slot);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _panel.HoverSlot(EquipSlot.None);
        }
    }
}
