using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// La pergamena del ritorno (D6 della M10): letta con il click destro nell'inventario apre un
    /// portale per l'accampamento davanti al cavaliere, e sparisce. In città non si legge. Non si
    /// indossa e non ha affissi.
    /// </summary>
    [CreateAssetMenu(fileName = "ReturnScroll", menuName = "DarkDescent/Items/Return Scroll")]
    public sealed class ReturnScrollDefinition : ItemDefinition
    {
        public override EquipSlot Slot => EquipSlot.None;

        public override AffixTargets AffixTarget => AffixTargets.None;
    }
}
