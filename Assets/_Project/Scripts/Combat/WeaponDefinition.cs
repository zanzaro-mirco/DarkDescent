using UnityEngine;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Dati di un'arma da mischia. Immutabili a runtime: chi la usa legge, non scrive.
    /// I tempi sono in secondi reali e vanno tenuti coerenti con la clip d'attacco.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon", menuName = "DarkDescent/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _damage = 10f;

        [SerializeField] private DamageType _damageType = DamageType.Physical;

        [Tooltip("Portata tra i bordi: distanza tra i centri meno il raggio di chi attacca e quello del bersaglio.")]
        [SerializeField, Min(0f)] private float _range = 0.6f;

        [Tooltip("Margine oltre la portata concesso al momento del danno: durante il ritardo il bersaglio può essersi spostato.")]
        [SerializeField, Min(0f)] private float _rangeTolerance = 0.5f;

        [Tooltip("Secondi tra l'inizio di un attacco e l'inizio del successivo.")]
        [SerializeField, Min(0.1f)] private float _attackInterval = 1f;

        [Tooltip("Secondi tra l'inizio dell'animazione e il danno: il momento in cui la lama arriva sul bersaglio.")]
        [SerializeField, Min(0f)] private float _hitDelay = 0.6f;

        public float Damage => _damage;
        public DamageType DamageType => _damageType;
        public float Range => _range;
        public float RangeTolerance => _rangeTolerance;
        public float AttackInterval => _attackInterval;
        public float HitDelay => _hitDelay;

        private void OnValidate()
        {
            // un danno che arriva dopo l'inizio del colpo successivo sovrapporrebbe due fendenti
            _hitDelay = Mathf.Min(_hitDelay, _attackInterval);
        }
    }
}
