using UnityEngine;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Chi controlla, prima che il mana si spenda, se un'abilità del Guerriero ha qualcosa da fare
    /// (M20): un nemico da colpire, alla distanza giusta. Senza, il lancio non parte e dice perché.
    /// </summary>
    public interface IAbilityTargeting
    {
        CastResult CanUse(SpellDefinition ability, Vector3 point);
    }
}
