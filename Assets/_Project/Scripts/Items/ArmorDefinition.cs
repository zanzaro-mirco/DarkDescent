using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un oggetto che dà Armatura. Per ora solo scudi, nello slot della mano sinistra, con la loro
    /// probabilità di blocco; elmi e armature arriveranno con il loro slot.
    /// </summary>
    [CreateAssetMenu(fileName = "Armor", menuName = "DarkDescent/Items/Armor")]
    public sealed class ArmorDefinition : ItemDefinition
    {
        [SerializeField, Min(0)] private int _armor = 5;

        [Tooltip("Probabilità di blocco dello scudo, in punti percentuali: si somma a Destrezza / 2 (D1 della M5).")]
        [SerializeField, Range(0, 75)] private int _blockChance;

        public int Armor => _armor;

        public int BlockChance => _blockChance;

        public override EquipSlot Slot => EquipSlot.Offhand;
    }
}
