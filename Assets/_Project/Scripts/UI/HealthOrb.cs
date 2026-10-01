using DarkDescent.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// La sfera rossa della vita, come in Diablo: un'Image Filled verticale che si svuota dall'alto.
    /// Nessun polling: si aggiorna solo quando Health annuncia un cambiamento.
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthOrb : MonoBehaviour
    {
        [Tooltip("Image di tipo Filled, metodo Vertical, origine Bottom.")]
        [SerializeField] private Image _fill;

        private Health _health;
        private bool _subscribed;

        public float FillAmount => _fill.fillAmount;

        /// <summary>
        /// Collega la sfera a una vita. Bind e OnEnable arrivano in ordine qualsiasi (trappola 12):
        /// si iscrive chi arriva per secondo.
        /// </summary>
        public void Bind(Health health)
        {
            Unsubscribe();
            _health = health;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
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
            if (_subscribed || _health == null)
            {
                return;
            }

            _health.HealthChanged += HandleHealthChanged;
            _subscribed = true;

            // mentre era spenta la vita può essere cambiata: si riparte dal valore attuale
            HandleHealthChanged(_health.Current, _health.Max);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            // _health può essere già distrutta (cambio scena): il -= su un oggetto distrutto è innocuo,
            // ma il riferimento C# serve comunque per toglierlo dalla lista
            _health.HealthChanged -= HandleHealthChanged;
            _subscribed = false;
        }

        private void HandleHealthChanged(float current, float max)
        {
            _fill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }
    }
}
