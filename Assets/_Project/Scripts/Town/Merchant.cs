using System;
using DarkDescent.Interaction;
using UnityEngine;

namespace DarkDescent.Town
{
    /// <summary>
    /// Il mercante dietro il banco della città (D5 della M10). Raggiunto con un click, annuncia che
    /// vuole aprire il negozio: banco e finestra li gestisce il composition root. Quando il cliente
    /// si allontana dal banco annuncia che se n'è andato, e il negozio si chiude.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class Merchant : MonoBehaviour
    {
        [Tooltip("Oltre questa distanza dal punto di arrivo il cliente se n'è andato e il negozio si chiude.")]
        [SerializeField, Min(1f)] private float _leaveDistance = 3f;

        private Interactable _interactable;
        private Transform _customer;

        /// <summary>Il cavaliere l'ha raggiunto: si apre il negozio.</summary>
        public event Action Opened;

        /// <summary>Il cavaliere si è allontanato dal banco: il negozio si chiude.</summary>
        public event Action Left;

        public float LeaveDistance => _leaveDistance;

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
            _customer = null;
        }

        // solo mentre c'è un cliente: un confronto di distanze, senza allocare
        private void Update()
        {
            if (_customer == null)
            {
                return;
            }

            if ((_customer.position - _interactable.ApproachPoint).sqrMagnitude > _leaveDistance * _leaveDistance)
            {
                _customer = null;
                Left?.Invoke();
            }
        }

        private void HandleUsed(GameObject user)
        {
            _customer = user != null ? user.transform : null;
            Opened?.Invoke();
        }
    }
}
