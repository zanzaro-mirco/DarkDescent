using DarkDescent.Combat;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un'arma da mischia: un oggetto con danno, portata e tempi del colpo. Immutabile a runtime:
    /// chi la usa legge, non scrive. I tempi sono in secondi reali e vanno tenuti coerenti con la
    /// clip d'attacco. Anche i colpi dei nemici e i pugni usano questo tipo, ma i loro asset stanno in
    /// <c>Data/Attacks</c> e non nel database degli oggetti: non si raccolgono.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon", menuName = "DarkDescent/Items/Weapon")]
    public sealed class WeaponDefinition : ItemDefinition
    {
        [Tooltip("Danno minimo del tiro, prima del moltiplicatore della Forza.")]
        [SerializeField, Min(0)] private int _minDamage = 6;

        [Tooltip("Danno massimo del tiro, compreso.")]
        [SerializeField, Min(0)] private int _maxDamage = 9;

        [SerializeField] private DamageType _damageType = DamageType.Physical;

        [SerializeField] private WeaponKind _kind = WeaponKind.Sword;

        [Tooltip("Forza necessaria per equipaggiarla.")]
        [SerializeField, Min(0)] private int _requiredStrength;

        [Tooltip("Portata tra i bordi: distanza tra i centri meno il raggio di chi attacca e quello del bersaglio.")]
        [SerializeField, Min(0f)] private float _range = 0.6f;

        [Tooltip("Margine oltre la portata concesso al momento del danno: durante il ritardo il bersaglio può essersi spostato.")]
        [SerializeField, Min(0f)] private float _rangeTolerance = 0.5f;

        [Tooltip("Secondi tra l'inizio di un attacco e l'inizio del successivo.")]
        [SerializeField, Min(0.1f)] private float _attackInterval = 1f;

        [Tooltip("Secondi tra l'inizio dell'animazione e il danno: il momento in cui la lama arriva sul bersaglio.")]
        [SerializeField, Min(0f)] private float _hitDelay = 0.6f;

        public int MinDamage => _minDamage;
        public int MaxDamage => _maxDamage;
        public DamageType DamageType => _damageType;
        public WeaponKind Kind => _kind;
        public int RequiredStrength => _requiredStrength;
        public float Range => _range;
        public float RangeTolerance => _rangeTolerance;
        public float AttackInterval => _attackInterval;
        public float HitDelay => _hitDelay;

        public override EquipSlot Slot => EquipSlot.Weapon;

        public override AffixTargets AffixTarget => AffixTargets.Weapon;

        protected override void OnValidate()
        {
            base.OnValidate();
            // un danno che arriva dopo l'inizio del colpo successivo sovrapporrebbe due fendenti
            _hitDelay = Mathf.Min(_hitDelay, _attackInterval);
            _maxDamage = Mathf.Max(_maxDamage, _minDamage);
        }
    }
}
