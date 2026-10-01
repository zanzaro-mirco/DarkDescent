using UnityEngine;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Descrive un colpo. Una struct e non "float + GameObject": quando arriveranno critici,
    /// elementi e knockback si aggiunge un campo, e nessuna firma di TakeDamage deve cambiare.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly GameObject Source;
        public readonly bool IsCritical;

        public DamageInfo(float amount, DamageType type, GameObject source, bool isCritical = false)
        {
            Amount = amount;
            Type = type;
            Source = source;
            IsCritical = isCritical;
        }
    }
}
