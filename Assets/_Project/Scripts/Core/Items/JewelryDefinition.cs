using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un anello o un amuleto (D5 della M8): niente di base, solo i suoi affissi. Gli anelli vanno
    /// in uno dei due slot degli anelli, l'amuleto nel suo. Come in Diablo 1 nasce almeno magico:
    /// normale non avrebbe niente.
    /// </summary>
    [CreateAssetMenu(fileName = "Jewelry", menuName = "DarkDescent/Items/Jewelry")]
    public sealed class JewelryDefinition : ItemDefinition
    {
        [Tooltip("Anello (uno dei due slot degli anelli) o amuleto.")]
        [SerializeField] private EquipSlot _slot = EquipSlot.Ring;

        public override EquipSlot Slot => _slot;

        public override AffixTargets AffixTarget => AffixTargets.Jewelry;

        public override Rarity MinRarity => Rarity.Magic;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (_slot != EquipSlot.Ring && _slot != EquipSlot.Amulet)
            {
                _slot = EquipSlot.Ring;
            }
        }
    }
}
