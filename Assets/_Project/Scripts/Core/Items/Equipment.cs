using System;
using DarkDescent.Stats;

namespace DarkDescent.Items
{
    /// <summary>
    /// Gli oggetti indossati, senza dipendenze dalla scena. Ogni oggetto equipaggiato mette i suoi
    /// modificatori sullo StatSheet con sé stesso come sorgente, e li toglie tutti quando esce:
    /// equipaggiare e togliere riporta le statistiche esattamente a prima. Gli anelli hanno due
    /// slot (D4 della M8): un anello va nel primo libero, o al posto del primo.
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

        /// <summary>Se l'oggetto entra in quello slot: il suo, o il secondo degli anelli per un anello.</summary>
        public static bool Fits(ItemDefinition definition, EquipSlot slot)
        {
            if (definition == null || slot == EquipSlot.None)
            {
                return false;
            }

            return definition.Slot == slot || (definition.Slot == EquipSlot.Ring && slot == EquipSlot.Ring2);
        }

        /// <summary>Se i requisiti dell'oggetto sono soddisfatti dalle statistiche attuali.</summary>
        public bool MeetsRequirements(ItemDefinition definition)
        {
            return _stats.Get(StatType.Strength) >= definition.RequiredStrength;
        }

        public bool CanEquip(ItemInstance item)
        {
            return item?.Definition != null && item.Definition.Slot != EquipSlot.None && MeetsRequirements(item.Definition);
        }

        /// <summary>
        /// Lo slot in cui andrebbe l'oggetto equipaggiandolo senza sceglierne uno: il suo, e per un
        /// anello il primo dei due libero, o il primo se sono pieni.
        /// </summary>
        public EquipSlot TargetSlot(ItemDefinition definition)
        {
            EquipSlot slot = definition != null ? definition.Slot : EquipSlot.None;
            if (slot == EquipSlot.Ring && Get(EquipSlot.Ring) != null && Get(EquipSlot.Ring2) == null)
            {
                return EquipSlot.Ring2;
            }

            return slot;
        }

        /// <summary>
        /// Mette l'oggetto nel suo slot. Restituisce false, senza toccare niente, se non si può;
        /// altrimenti in <paramref name="previous"/> c'è l'oggetto che occupava lo slot, o null.
        /// </summary>
        public bool TryEquip(ItemInstance item, out ItemInstance previous)
        {
            return TryEquip(item, TargetSlot(item?.Definition), out previous);
        }

        /// <summary>Come l'altro, ma nello slot scelto (il click su uno slot): false se l'oggetto non ci entra.</summary>
        public bool TryEquip(ItemInstance item, EquipSlot slot, out ItemInstance previous)
        {
            previous = null;
            if (!CanEquip(item) || !Fits(item.Definition, slot))
            {
                return false;
            }

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
            // il danno dell'arma e il blocco dello scudo li leggono MeleeAttack e ShieldBlock; qui
            // l'Armatura di scudi e pezzi d'armatura, con il suo "+%", e gli affissi del personaggio.
            // Tutto con l'oggetto come sorgente: toglierlo toglie tutto insieme.
            if (item.Definition is ArmorDefinition)
            {
                int armor = ItemStats.ItemArmor(item);
                if (armor != 0)
                {
                    _stats.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Flat, armor, item));
                }
            }

            foreach (var affix in item.Affixes)
            {
                if (affix.Definition != null && ItemStats.TryGetCharacterStat(affix.Definition.Effect, out StatType stat))
                {
                    _stats.AddModifier(new StatModifier(stat, ModifierKind.Flat, affix.Value, item));
                }
            }
        }
    }
}
