using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un oggetto che dà Armatura: lo scudo nella mano sinistra, con la sua probabilità di blocco,
    /// e dalla M8 elmi, armature, guanti e stivali (D5), con la Forza richiesta come le armi. Dalla M21
    /// anche il tomo del Mago (D4): sta nello slot dello scudo ma non è uno scudo, non blocca, e dà
    /// Magia e mana.
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

        [Tooltip("Magia necessaria per indossarlo (il tomo, D4 della M21).")]
        [SerializeField, Min(0)] private int _requiredMagic;

        [Tooltip("Il tomo del Mago (M21): nella mano libera ma non è uno scudo, non blocca. Solo per lo slot della mano sinistra.")]
        [SerializeField] private bool _tome;

        [Tooltip("Magia che dà il tomo, sempre, oltre agli affissi.")]
        [SerializeField, Min(0)] private int _bonusMagic;

        [Tooltip("Mana massimo che dà il tomo, sempre, oltre agli affissi.")]
        [SerializeField, Min(0)] private int _bonusMana;

        public int Armor => _armor;

        public int BlockChance => IsShield ? _blockChance : 0;

        public bool IsTome => _slot == EquipSlot.Offhand && _tome;

        public bool IsShield => _slot == EquipSlot.Offhand && !_tome;

        public int BonusMagic => IsTome ? _bonusMagic : 0;

        public int BonusMana => IsTome ? _bonusMana : 0;

        public override EquipSlot Slot => _slot;

        public override int RequiredStrength => _requiredStrength;

        public override int RequiredMagic => _requiredMagic;

        public override AffixTargets AffixTarget => IsTome ? AffixTargets.Caster
            : IsShield ? AffixTargets.Shield
            : Slot == EquipSlot.Helm ? AffixTargets.Armor | AffixTargets.Helm
            : AffixTargets.Armor;

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
