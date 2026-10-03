using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un oggetto che dà Armatura. Per ora solo scudi, nello slot della mano sinistra; con la M5
    /// lo slot si sceglierà nell'asset (elmo, armatura).
    /// </summary>
    [CreateAssetMenu(fileName = "Armor", menuName = "DarkDescent/Items/Armor")]
    public sealed class ArmorDefinition : ItemDefinition
    {
        [SerializeField, Min(0)] private int _armor = 5;

        public int Armor => _armor;

        public override EquipSlot Slot => EquipSlot.Offhand;
    }
}
