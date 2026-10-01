using UnityEngine;

namespace DarkDescent.Player
{
    /// <summary>
    /// Traduce lo stato del motor in parametri dell'Animator. Nessuna logica di gioco:
    /// sta sul modello, figlio del Player, così cambiare modello non tocca il resto.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        // hash calcolato una volta: SetFloat con la stringa fa un lookup a ogni chiamata
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Tooltip("Vuoto = PlayerMotor del parent, risolto in Awake.")]
        [SerializeField] private PlayerMotor _motor;

        [Tooltip("Smorzamento del parametro Speed, in secondi. Evita gli scatti tra idle e corsa.")]
        [SerializeField, Min(0f)] private float _speedDampTime = 0.1f;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_motor == null)
            {
                _motor = GetComponentInParent<PlayerMotor>();
            }
        }

        private void Update()
        {
            _animator.SetFloat(SpeedHash, _motor.NormalizedSpeed, _speedDampTime, Time.deltaTime);
        }
    }
}
