using System;
using System.Collections.Generic;
using DarkDescent.Stats;

namespace DarkDescent.Items
{
    /// <summary>
    /// I bonus dei set indossati (D9 della M16). Ascolta il <see cref="Equipment.Changed"/>, conta i
    /// pezzi diversi di ogni set e mette i modificatori dei bonus accesi con il set come sorgente,
    /// dopo aver tolto tutti quelli di prima: gli slot sono pochi, e rifare tutto a ogni cambio è
    /// semplice. Un bonus vale al livello del pezzo più basso tra quelli indossati, senza tiro.
    /// </summary>
    public sealed class SetBonuses : IDisposable
    {
        private static readonly int SlotCount = Enum.GetValues(typeof(EquipSlot)).Length;

        private readonly Equipment _equipment;
        private readonly StatSheet _stats;

        // i set che hanno modificatori sulla scheda, per toglierli al prossimo cambio
        private readonly List<SetDefinition> _active = new List<SetDefinition>();

        // riusata a ogni ricalcolo
        private readonly List<SetDefinition> _sets = new List<SetDefinition>();

        public SetBonuses(Equipment equipment, StatSheet stats)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _equipment.Changed += HandleChanged;
            Refresh();
        }

        /// <summary>
        /// Quanti pezzi diversi del set sono indossati; in <paramref name="lowestLevel"/> il livello del più
        /// basso, 0 se nessuno.
        /// </summary>
        public static int CountWorn(Equipment equipment, SetDefinition set, out int lowestLevel)
        {
            lowestLevel = 0;
            int count = 0;
            if (equipment == null || set == null)
            {
                return count;
            }

            foreach (var piece in set.Pieces)
            {
                int level = WornLevel(equipment, piece);
                if (level > 0)
                {
                    count++;
                    lowestLevel = lowestLevel == 0 ? level : Math.Min(lowestLevel, level);
                }
            }

            return count;
        }

        /// <summary>Se questo pezzo è indossato: lo stesso pezzo in due slot conta una volta.</summary>
        public static bool IsWorn(Equipment equipment, UniqueDefinition piece)
        {
            return equipment != null && WornLevel(equipment, piece) > 0;
        }

        public void Dispose()
        {
            _equipment.Changed -= HandleChanged;
            Clear();
        }

        private void HandleChanged(EquipSlot slot)
        {
            Refresh();
        }

        private void Refresh()
        {
            Clear();
            CollectWornSets();
            foreach (var set in _sets)
            {
                int count = CountWorn(_equipment, set, out int level);
                bool any = false;
                foreach (var bonus in set.Bonuses)
                {
                    if (bonus.Pieces > count)
                    {
                        continue;
                    }

                    foreach (var property in bonus.Properties)
                    {
                        any |= ItemStats.AddCharacterModifiers(_stats, property.Effect, property.ValueAt(level, set.MinLevel), set);
                    }
                }

                if (any)
                {
                    _active.Add(set);
                }
            }
        }

        private void Clear()
        {
            foreach (var set in _active)
            {
                _stats.RemoveModifiersFrom(set);
            }

            _active.Clear();
        }

        private void CollectWornSets()
        {
            _sets.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                var set = _equipment.Get((EquipSlot)i)?.Named?.Set;
                if (set != null && !_sets.Contains(set))
                {
                    _sets.Add(set);
                }
            }
        }

        // il livello più alto del pezzo tra gli slot che lo indossano, 0 se non c'è
        private static int WornLevel(Equipment equipment, UniqueDefinition piece)
        {
            int level = 0;
            if (piece == null)
            {
                return level;
            }

            for (int i = 0; i < SlotCount; i++)
            {
                var item = equipment.Get((EquipSlot)i);
                if (item != null && item.Named == piece)
                {
                    level = Math.Max(level, Math.Max(1, item.ItemLevel));
                }
            }

            return level;
        }
    }
}
