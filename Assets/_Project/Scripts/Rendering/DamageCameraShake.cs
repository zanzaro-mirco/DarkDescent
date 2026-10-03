using DarkDescent.Combat;
using Unity.Cinemachine;
using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Scuote la camera quando il player viene colpito, in proporzione al danno, e più forte alla
    /// morte. Il segnale parte da un CinemachineImpulseSource e lo riceve l'Impulse Listener della
    /// camera: questo componente non sa niente della camera.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineImpulseSource), typeof(Health))]
    public class DamageCameraShake : MonoBehaviour
    {
        [Tooltip("Frazione della vita massima a cui la scossa è piena. Sotto, scala con il danno (la stessa soglia di HitRecovery, ADR-010).")]
        [SerializeField, Range(0.01f, 1f)] private float _fullForceFraction = 0.2f;

        [Tooltip("Forza della scossa alla morte, rispetto a quella piena di un colpo.")]
        [SerializeField, Min(0f)] private float _deathForce = 1.5f;

        private CinemachineImpulseSource _source;
        private Health _health;

        private void Awake()
        {
            _source = GetComponent<CinemachineImpulseSource>();
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.Damaged += HandleDamaged;
            _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= HandleDamaged;
            _health.Died -= HandleDied;
        }

        private void HandleDamaged(DamageInfo info, float applied)
        {
            float force = Mathf.Clamp01(applied / (_health.Max * _fullForceFraction));
            if (force > 0f)
            {
                _source.GenerateImpulseWithForce(force);
            }
        }

        private void HandleDied()
        {
            _source.GenerateImpulseWithForce(_deathForce);
        }
    }
}
