using System;
using System.Collections.Generic;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Stats;

namespace DarkDescent.Progression
{
    /// <summary>
    /// Il cavaliere di <c>-herolevel</c> (D13 della M14): al livello chiesto, con i punti spesi in modo
    /// bilanciato e un oggetto raro per slot, generato dal seme. Serve a provare una difficoltà alta
    /// senza batterne otto. Logica pura.
    /// </summary>
    public static class HeroSetup
    {
        // cinque punti a livello: due in Forza, due in Vitalità, uno in Destrezza (D10 della M14)
        private static readonly StatType[] Pattern =
        {
            StatType.Strength, StatType.Vitality, StatType.Strength, StatType.Vitality, StatType.Dexterity,
        };

        private static readonly EquipSlot[] Slots =
        {
            EquipSlot.Weapon, EquipSlot.Offhand, EquipSlot.Helm, EquipSlot.Body, EquipSlot.Gloves,
            EquipSlot.Boots, EquipSlot.Amulet, EquipSlot.Ring, EquipSlot.Ring2,
        };

        /// <summary>Porta il cavaliere al livello <paramref name="level"/> e ne spende tutti i punti.</summary>
        public static void RaiseTo(CharacterProgress progress, int level)
        {
            progress.Restore(level, 0, (Math.Max(1, level) - 1) * 5);
            for (int i = 0; progress.UnspentPoints > 0; i++)
            {
                progress.TrySpend(Pattern[i % Pattern.Length]);
            }
        }

        /// <summary>
        /// Per ogni slot, la base di valore più alto tra <paramref name="bases"/> che il cavaliere può
        /// portare, rara e del livello dato. Restituisce quanti oggetti ha messo.
        /// </summary>
        public static int Equip(Equipment equipment, IReadOnlyList<LootEntry> bases, ItemGenerator generator, int itemLevel, ulong runSeed)
        {
            int equipped = 0;
            foreach (var slot in Slots)
            {
                ItemDefinition best = null;
                foreach (var entry in bases)
                {
                    var item = entry.Item;
                    if (Equipment.Fits(item, slot) && equipment.MeetsRequirements(item) && (best == null || item.Value > best.Value))
                    {
                        best = item;
                    }
                }

                if (best != null && equipment.TryEquip(generator.Generate(best, Math.Max(1, itemLevel), SeedMixer.ForHero(runSeed, (int)slot), Rarity.Rare), slot, out _))
                {
                    equipped++;
                }
            }

            return equipped;
        }
    }
}
