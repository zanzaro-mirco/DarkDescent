using DarkDescent.Magic;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// La sfera blu del mana, in basso a destra come in Diablo (D10 della M9): la gemella della sfera
    /// della vita. Si aggiorna solo quando il mana annuncia un cambio, cioè a ogni punto intero.
    /// </summary>
    [DisallowMultipleComponent]
    public class ManaOrb : MonoBehaviour
    {
        [Tooltip("Image di tipo Filled, metodo Vertical, origine Bottom.")]
        [SerializeField] private Image _fill;

        [Tooltip("Il numero che compare con il mouse sopra (passo 11.7 della M11).")]
        [SerializeField] private OrbReadout _readout;

        private ManaPool _mana;
        private bool _subscribed;

        public float FillAmount => _fill.fillAmount;

        public OrbReadout Readout => _readout;

        /// <summary>Collega la sfera al mana. Bind e OnEnable arrivano in ordine qualsiasi: si iscrive chi arriva per secondo.</summary>
        public void Bind(ManaPool mana)
        {
            Unsubscribe();
            _mana = mana;
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
            if (_subscribed || _mana == null)
            {
                return;
            }

            _mana.Changed += HandleManaChanged;
            _subscribed = true;
            HandleManaChanged(_mana.Current, _mana.Max);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _mana.Changed -= HandleManaChanged;
            _subscribed = false;
        }

        private void HandleManaChanged(float current, float max)
        {
            _fill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (_readout != null)
            {
                _readout.SetValues(current, max);
            }
        }
    }
}
