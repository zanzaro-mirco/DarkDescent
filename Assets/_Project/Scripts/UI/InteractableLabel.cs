using DarkDescent.Interaction;
using DarkDescent.Player;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il nome di ciò che sta sotto il cursore, in alto al centro dello schermo. Nessun polling: si
    /// aggiorna quando il controller annuncia che l'oggetto sotto il cursore è cambiato.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractableLabel : MonoBehaviour
    {
        [Tooltip("Il pannello da mostrare; spento all'avvio. Il componente sta sul padre, sempre attivo.")]
        [SerializeField] private GameObject _panel;

        [SerializeField] private TMP_Text _text;

        private PlayerController _controller;
        private bool _subscribed;

        public bool IsShown => _panel.activeSelf;

        public string Text => _text.text;

        /// <summary>Come la sfera: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerController controller)
        {
            Unsubscribe();
            _controller = controller;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Awake()
        {
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _controller == null)
            {
                return;
            }

            _controller.HoveredChanged += Show;
            _subscribed = true;
            Show(_controller.Hovered);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _controller.HoveredChanged -= Show;
            _subscribed = false;
        }

        private void Show(Interactable hovered)
        {
            bool visible = hovered != null && !string.IsNullOrEmpty(hovered.Label);
            if (visible)
            {
                _text.text = hovered.Label;
            }

            _panel.SetActive(visible);
        }
    }
}
