using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un oggetto che dà Armatura: lo scudo nella mano sinistra, con la sua probabilità di blocco,
    /// e dalla M8 elmi, armature, guanti e stivali (D5), con la Forza richiesta come le armi.
    /// </summary>
    [CreateAssetMenu(fileName = "Armor", menuName = "DarkDescent/Items/Armor")]
    public sealed class ArmorDefinition : ItemDefinition
    {
        [Tooltip("Scudo (mano sinistra), elmo, armatura, guanti o stivali.")]
        [SerializeField] private EquipSlot _slot = EquipSlot.Offhand;

        [SerializeField, Min(0)] private int _armor = 5;

        [Tooltip("Probabilità di blocco dello scudo, in punti percentuali: si somma a Destrezza / 2 (D1 della M5). Solo per gli scudi.")]
        [SerializeField, Range(0, 75)] private int _blockChance;

        [Tooltip("Forza necessaria per indossarlo.")]
        [SerializeField, Min(0)] private int _requiredStrength;

        public int Armor => _armor;

        public int BlockChance => _slot == EquipSlot.Offhand ? _blockChance : 0;

        public bool IsShield => _slot == EquipSlot.Offhand;

        public override EquipSlot Slot => _slot;

        public override int RequiredStrength => _requiredStrength;

        public override AffixTargets AffixTarget => IsShield ? AffixTargets.Shield : AffixTargets.Armor;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (_slot != EquipSlot.Offhand && _slot != EquipSlot.Helm && _slot != EquipSlot.Body && _slot != EquipSlot.Gloves && _slot != EquipSlot.Boots)
            {
                _slot = EquipSlot.Offhand;
            }
        }
    }
}
