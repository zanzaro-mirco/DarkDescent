using System;
using DarkDescent.Interaction;
using UnityEngine;

namespace DarkDescent.Town
{
    /// <summary>
    /// Il mercante dietro il banco della città (D5 della M10). Raggiunto con un click, annuncia che
    /// vuole aprire il negozio: banco e finestra li gestisce il composition root.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class Merchant : MonoBehaviour
    {
        private Interactable _interactable;

        /// <summary>Il cavaliere l'ha raggiunto: si apre il negozio.</summary>
        public event Action Opened;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
        }

        private void HandleUsed(GameObject user)
        {
            Opened?.Invoke();
        }
    }
}
