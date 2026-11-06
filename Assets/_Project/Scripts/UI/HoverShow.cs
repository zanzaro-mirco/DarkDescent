using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkDescent.UI
{
    /// <summary>
    /// Mostra un oggetto finché il puntatore sta sopra questo: il suggerimento di un pulsante (l'interruttore dell'altare, M24).
    /// All'accensione e allo spegnimento il suggerimento torna nascosto.
    /// </summary>
    [DisallowMultipleComponent]
    public class HoverShow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject _target;

        public GameObject Target => _target;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Show(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Show(false);
        }

        private void OnEnable()
        {
            Show(false);
        }

        private void OnDisable()
        {
            Show(false);
        }

        private void Show(bool on)
        {
            if (_target != null)
            {
                _target.SetActive(on);
            }
        }
    }
}
