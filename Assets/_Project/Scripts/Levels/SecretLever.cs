using System;
using System.Collections;
using DarkDescent.Interaction;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La leva che apre la porta sigillata (D5 della M25): si clicca, il cavaliere ci va e la tira. Una volta sola. L'asta gira attorno al suo piede,
    /// da indietro (a riposo) in avanti (tirata).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class SecretLever : MonoBehaviour
    {
        [Tooltip("L'asta: gira attorno all'asse X del suo piede.")]
        [SerializeField] private Transform _handle;

        [Tooltip("Il collider del click: si spegne a leva tirata.")]
        [SerializeField] private Collider _clickArea;

        [Tooltip("Di quanto si inclina l'asta in ogni verso, in gradi.")]
        [SerializeField, Min(5f)] private float _angle = 32f;

        [SerializeField, Min(0.05f)] private float _duration = 0.45f;

        private Interactable _interactable;
        private Quaternion _rest;

        public bool IsPulled { get; private set; }

        /// <summary>La leva è stata tirata.</summary>
        public event Action Pulled;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            if (_handle != null)
            {
                _rest = _handle.localRotation;
                _handle.localRotation = _rest * Quaternion.Euler(-_angle, 0f, 0f);
            }
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
        }

        /// <summary>Già tirata in una visita di prima (ADR-079): l'asta in avanti, niente click.</summary>
        public void RestorePulled()
        {
            IsPulled = true;
            Disarm();
            if (_handle != null)
            {
                _handle.localRotation = _rest * Quaternion.Euler(_angle, 0f, 0f);
            }
        }

        private void HandleUsed(GameObject user)
        {
            if (IsPulled)
            {
                return;
            }

            IsPulled = true;
            Disarm();
            if (_handle != null)
            {
                StartCoroutine(Swing());
            }

            Pulled?.Invoke();
        }

        private void Disarm()
        {
            if (_interactable != null)
            {
                _interactable.SetHighlighted(false);
            }

            if (_clickArea != null)
            {
                _clickArea.enabled = false;
            }
        }

        private IEnumerator Swing()
        {
            Quaternion from = _rest * Quaternion.Euler(-_angle, 0f, 0f);
            Quaternion to = _rest * Quaternion.Euler(_angle, 0f, 0f);
            for (float t = 0f; t < _duration; t += Time.deltaTime)
            {
                _handle.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / _duration));
                yield return null;
            }

            _handle.localRotation = to;
        }
    }
}
