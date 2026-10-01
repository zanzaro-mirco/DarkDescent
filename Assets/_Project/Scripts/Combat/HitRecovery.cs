using System;
using UnityEngine;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Reazione al colpo, come l'hit recovery di Diablo: solo un colpo abbastanza forte interrompe
    /// l'attacco in corso e blocca il personaggio per un attimo. I colpi deboli non fermano niente.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class HitRecovery : MonoBehaviour
    {
        [Tooltip("Frazione della vita massima che un singolo colpo deve togliere per interrompere.")]
        [SerializeField, Range(0f, 1f)] private float _threshold = 0.2f;

        [Tooltip("Secondi di blocco dopo un colpo che interrompe. Conviene vicino alla durata della clip Hit.")]
        [SerializeField, Min(0f)] private float _duration = 0.5f;

        private Health _health;
        private MeleeAttack _attack;

        /// <summary>Colpo abbastanza forte da interrompere: l'animazione di colpo subito si aggancia qui.</summary>
        public event Action Staggered;

        private void Awake()
        {
            _health = GetComponent<Health>();
            // facoltativo: un barile prende colpi ma non attacca
            _attack = GetComponent<MeleeAttack>();
        }

        private void OnEnable()
        {
            _health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            _health.Damaged -= HandleDamaged;
        }

        private void HandleDamaged(DamageInfo info, float applied)
        {
            // il colpo che uccide passa alla morte: niente interruzione in mezzo
            if (_health.IsDead || applied < _threshold * _health.Max)
            {
                return;
            }

            if (_attack != null)
            {
                _attack.Interrupt(_duration);
            }

            Staggered?.Invoke();
        }
    }
}
