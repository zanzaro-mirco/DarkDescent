using UnityEngine;

namespace DarkDescent.Core
{
    /// <summary>
    /// Ferma il tempo per un istante quando un colpo va a segno: il colpo "pesa".
    /// Conta in tempo reale, perché con timeScale a 0 il tempo di gioco non scorre.
    /// </summary>
    [DisallowMultipleComponent]
    public class HitStop : MonoBehaviour
    {
        [Tooltip("Durata di default, in secondi reali.")]
        [SerializeField, Min(0f)] private float _duration = 0.05f;

        [Tooltip("Durata per un colpo critico del cavaliere (D13 della M7): il colpo pesa di più.")]
        [SerializeField, Min(0f)] private float _criticalDuration = 0.12f;

        private float _remaining;
        private float _previousTimeScale = 1f;

        public bool IsActive => _remaining > 0f;

        public void Trigger()
        {
            Trigger(_duration);
        }

        public void TriggerCritical()
        {
            Trigger(_criticalDuration);
        }

        /// <summary>
        /// Due hit stop ravvicinati non si sommano: vale la fine più lontana. E il valore da
        /// ripristinare si legge solo al primo, altrimenti il secondo salverebbe lo 0 del primo (trappola 5).
        /// </summary>
        public void Trigger(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            if (!IsActive)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            _remaining = Mathf.Max(_remaining, duration);
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f)
            {
                End();
            }
        }

        private void OnDisable()
        {
            // spento a metà (cambio scena): il tempo non deve restare fermo
            if (IsActive)
            {
                End();
            }
        }

        private void End()
        {
            _remaining = 0f;
            // il valore di prima, non 1: una pausa iniziata prima dell'hit stop deve restare
            Time.timeScale = _previousTimeScale;
        }
    }
}
