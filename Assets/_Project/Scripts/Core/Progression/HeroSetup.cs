using System;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Stats;
using DarkDescent.Talents;

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
            RaiseTo(progress, level, Pattern);
        }

        /// <summary>Lo stesso con lo schema di una classe (D2 della M21): per il Mago, Magia e Vitalità.</summary>
        public static void RaiseTo(CharacterProgress progress, int level, IReadOnlyList<StatType> pattern)
        {
            if (pattern == null || pattern.Count == 0)
            {
                pattern = Pattern;
            }

            progress.Restore(level, 0, (Math.Max(1, level) - 1) * 5);
            for (int i = 0; progress.UnspentPoints > 0; i++)
            {
                progress.TrySpend(pattern[i % pattern.Count]);
            }
        }

        /// <summary>
        /// L'albero di riferimento (D14 della M20): spende tutti i punti del livello di <paramref name="build"/>
        /// come <see cref="RaiseTo"/> spende gli attributi, sempre allo stesso modo. Sceglie la specializzazione,
        /// poi riempie dall'alto, riga per riga e da sinistra, prima la classe e poi quella: ogni nodo al grado
        /// massimo (e la prima opzione, per quelli a scelta) prima del successivo, finché i punti o i nodi aperti
        /// finiscono. Alla fine applica. Senza specializzazione spende solo la classe.
        /// </summary>
        public static void SpendTalents(TalentBuild build, TalentTree specialization)
        {
            if (specialization != null)
            {
                build.TryChoose(specialization);
            }

            foreach (var tree in new[] { build.ClassTree, build.Specialization }.Where(t => t != null))
            {
                var order = tree.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Column).ToList();
                bool added = true;
                while (added)
                {
                    added = false;
                    foreach (var node in order)
                    {
                        while (build.TryAdd(tree, node))
                        {
                            added = true;
                        }
                    }
                }
            }

            build.Apply();
        }

        /// <summary>
        /// Per ogni slot, la base di valore più alto tra <paramref name="bases"/> che il cavaliere può
        /// portare, rara (o della rarità data, per la misura delle difficoltà) e del livello dato.
        /// Restituisce quanti oggetti ha messo.
        /// </summary>
        public static int Equip(Equipment equipment, IReadOnlyList<LootEntry> bases, ItemGenerator generator, int itemLevel, ulong runSeed,
            Rarity rarity = Rarity.Rare)
        {
            int equipped = 0;
            foreach (var slot in Slots)
            {
                ItemDefinition best = null;
                foreach (var entry in bases)
                {
                    var item = entry.Item;
                    // le armi a due mani no: la misura delle difficoltà è quella di una mano e scudo (D5 della M20)
                    if (item is WeaponDefinition { TwoHanded: true })
                    {
                        continue;
                    }

                    if (Equipment.Fits(item, slot) && equipment.MeetsRequirements(item) && (best == null || item.Value > best.Value))
                    {
                        best = item;
                    }
                }

                if (best != null && equipment.TryEquip(generator.Generate(best, Math.Max(1, itemLevel), SeedMixer.ForHero(runSeed, (int)slot), rarity), slot, out _))
                {
                    equipped++;
                }
            }

            return equipped;
        }
    }
}
