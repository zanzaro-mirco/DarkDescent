using System;
using DarkDescent.Stats;

namespace DarkDescent.Items
{
    /// <summary>
    /// Gli oggetti indossati, senza dipendenze dalla scena. Ogni oggetto equipaggiato mette i suoi
    /// modificatori sullo StatSheet con sé stesso come sorgente, e li toglie tutti quando esce:
    /// equipaggiare e togliere riporta le statistiche esattamente a prima.
    /// </summary>
    public sealed class Equipment
    {
        private static readonly int SlotCount = Enum.GetValues(typeof(EquipSlot)).Length;

        private readonly StatSheet _stats;
        private readonly ItemInstance[] _slots = new ItemInstance[SlotCount];

        public Equipment(StatSheet stats)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        /// <summary>Uno slot ha cambiato oggetto: modelli in mano, arma usata, pannelli.</summary>
        public event Action<EquipSlot> Changed;

        /// <summary>L'oggetto nello slot, o null se è vuoto.</summary>
        public ItemInstance Get(EquipSlot slot)
        {
            return _slots[(int)slot];
        }

        /// <summary>Se i requisiti dell'oggetto sono soddisfatti dalle statistiche attuali.</summary>
        public bool MeetsRequirements(ItemDefinition definition)
        {
            return !(definition is WeaponDefinition weapon) || _stats.Get(StatType.Strength) >= weapon.RequiredStrength;
        }

        public bool CanEquip(ItemInstance item)
        {
            return item?.Definition != null && item.Definition.Slot != EquipSlot.None && MeetsRequirements(item.Definition);
        }

        /// <summary>
        /// Mette l'oggetto nel suo slot. Restituisce false, senza toccare niente, se non si può;
        /// altrimenti in <paramref name="previous"/> c'è l'oggetto che occupava lo slot, o null.
        /// </summary>
        public bool TryEquip(ItemInstance item, out ItemInstance previous)
        {
            previous = null;
            if (!CanEquip(item))
            {
                return false;
            }

            EquipSlot slot = item.Definition.Slot;
            previous = Remove(slot);
            _slots[(int)slot] = item;
            AddModifiers(item);
            Changed?.Invoke(slot);
            return true;
        }

        /// <summary>Svuota lo slot e restituisce l'oggetto che c'era, o null.</summary>
        public ItemInstance Unequip(EquipSlot slot)
        {
            ItemInstance item = Remove(slot);
            if (item != null)
            {
                Changed?.Invoke(slot);
            }

            return item;
        }

        private ItemInstance Remove(EquipSlot slot)
        {
            ItemInstance item = _slots[(int)slot];
            if (item != null)
            {
                _slots[(int)slot] = null;
                _stats.RemoveModifiersFrom(item);
            }

            return item;
        }

        private void AddModifiers(ItemInstance item)
        {
            // le armi non toccano le statistiche: il loro danno lo legge MeleeAttack. Con la M5 gli
            // affissi aggiungeranno modificatori a qualsiasi oggetto.
            if (item.Definition is ArmorDefinition armor && armor.Armor != 0)
            {
                _stats.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Flat, armor.Armor, item));
            }
        }
    }
}
