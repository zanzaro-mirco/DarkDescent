using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    /// <summary>Unici e set della M16 (passo 16.2): definizioni scritte qui, sulle basi vere.</summary>
    public class NamedItemTests
    {
        private static readonly StatType[] AllStats = (StatType[])Enum.GetValues(typeof(StatType));

        private readonly List<Object> _created = new List<Object>();

        private static T Load<T>(string name) where T : ItemDefinition
        {
            return AssetDatabase.LoadAssetAtPath<T>($"Assets/_Project/Data/Items/{name}.asset");
        }

        private static NamedProperty Property(AffixEffect effect, int low, int high)
        {
            return new NamedProperty(effect, low, high);
        }

        private UniqueDefinition Unique(string item, SetDefinition set, params NamedProperty[] properties)
        {
            var named = ScriptableObject.CreateInstance<UniqueDefinition>();
            _created.Add(named);
            var serialized = new SerializedObject(named);
            serialized.FindProperty("_id").stringValue = Guid.NewGuid().ToString("N");
            serialized.FindProperty("_nameKey").stringValue = "enemy.morvak";
            serialized.FindProperty("_base").objectReferenceValue = Load<ItemDefinition>(item);
            serialized.FindProperty("_minLevel").intValue = 10;
            serialized.FindProperty("_set").objectReferenceValue = set;
            var list = serialized.FindProperty("_properties");
            list.arraySize = properties.Length;
            for (int i = 0; i < properties.Length; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_effect").enumValueIndex = (int)properties[i].Effect;
                element.FindPropertyRelative("_low").intValue = properties[i].Low;
                element.FindPropertyRelative("_high").intValue = properties[i].High;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return named;
        }

        private SetDefinition Set()
        {
            var set = ScriptableObject.CreateInstance<SetDefinition>();
            _created.Add(set);
            return set;
        }

        // i pezzi e i bonus del set, dopo i pezzi che lo riferiscono
        private static void Fill(SetDefinition set, UniqueDefinition[] pieces, params SetBonus[] bonuses)
        {
            var serialized = new SerializedObject(set);
            serialized.FindProperty("_id").stringValue = Guid.NewGuid().ToString("N");
            var list = serialized.FindProperty("_pieces");
            list.arraySize = pieces.Length;
            for (int i = 0; i < pieces.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
            }

            var bonusList = serialized.FindProperty("_bonuses");
            bonusList.arraySize = bonuses.Length;
            for (int i = 0; i < bonuses.Length; i++)
            {
                var element = bonusList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_pieces").intValue = bonuses[i].Pieces;
                var properties = element.FindPropertyRelative("_properties");
                properties.arraySize = bonuses[i].Properties.Count;
                for (int j = 0; j < bonuses[i].Properties.Count; j++)
                {
                    var property = properties.GetArrayElementAtIndex(j);
                    property.FindPropertyRelative("_effect").enumValueIndex = (int)bonuses[i].Properties[j].Effect;
                    property.FindPropertyRelative("_low").intValue = bonuses[i].Properties[j].Low;
                    property.FindPropertyRelative("_high").intValue = bonuses[i].Properties[j].High;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private NamedItemDatabase Database(params UniqueDefinition[] items)
        {
            var database = ScriptableObject.CreateInstance<NamedItemDatabase>();
            _created.Add(database);
            var serialized = new SerializedObject(database);
            var list = serialized.FindProperty("_items");
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return database;
        }

        // l'armatura del Custode della tabella del 16.1, con i suoi bonus
        private UniqueDefinition[] Warden(out SetDefinition set)
        {
            set = Set();
            var pieces = new[]
            {
                Unique("KnightHelm", set, Property(AffixEffect.ArmorPercent, 50, 120), Property(AffixEffect.Life, 15, 60)),
                Unique("Breastplate", set, Property(AffixEffect.ArmorPercent, 60, 140), Property(AffixEffect.Vitality, 8, 30)),
                Unique("Gauntlets", set, Property(AffixEffect.Strength, 6, 25), Property(AffixEffect.ToHit, 10, 30)),
                Unique("IronBoots", set, Property(AffixEffect.Armor, 4, 18), Property(AffixEffect.Life, 15, 60)),
            };
            Fill(set, pieces,
                new SetBonus(2, new[] { Property(AffixEffect.Life, 25, 120) }),
                new SetBonus(3, new[] { Property(AffixEffect.Armor, 10, 40) }),
                new SetBonus(4, new[] { Property(AffixEffect.DamageReduction, 4, 20), Property(AffixEffect.AllAttributes, 5, 20) }));
            return pieces;
        }

        private static StatSheet Hero()
        {
            var stats = new StatSheet();
            stats.SetBase(StatType.Strength, 40f);
            stats.SetBase(StatType.Dexterity, 20f);
            stats.SetBase(StatType.Magic, 10f);
            stats.SetBase(StatType.Vitality, 25f);
            return stats;
        }

        private static float[] Snapshot(StatSheet stats)
        {
            return AllStats.Select(stats.Get).ToArray();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test, Description("Unici e set hanno un colore loro, diverso dalle altre rarità, e una luce a terra più forte")]
        public void Rarities_HaveTheirOwnColors()
        {
            var rarities = (Rarity[])Enum.GetValues(typeof(Rarity));
            Assert.AreEqual(5, rarities.Length);
            Assert.AreEqual(rarities.Length, rarities.Select(RarityColors.TextHex).Distinct().Count(), "testo");
            Assert.AreEqual(rarities.Length, rarities.Select(RarityColors.Cell).Distinct().Count(), "cella");
            Assert.AreEqual(rarities.Length, rarities.Select(RarityColors.Light).Distinct().Count(), "luce");
            Assert.AreEqual("#C9A55C", RarityColors.TextHex(Rarity.Unique));
            Assert.AreEqual("#4FD45A", RarityColors.TextHex(Rarity.Set));
            Assert.Greater(RarityColors.LightBoost(Rarity.Unique), 1f);
            Assert.Greater(RarityColors.LightBoost(Rarity.Set), 1f);
            Assert.AreEqual(1f, RarityColors.LightBoost(Rarity.Rare));
        }

        [Test, Description("Un unico vale dieci volte un normale, un pezzo di set otto (D12)")]
        public void Prices_OfUniquesAndSets()
        {
            Assert.AreEqual(10, ItemPricing.RarityMultiplier(Rarity.Unique));
            Assert.AreEqual(8, ItemPricing.RarityMultiplier(Rarity.Set));

            var cleaver = Unique("Axe", null, Property(AffixEffect.Strength, 10, 40));
            var item = ItemGenerator.GenerateNamed(cleaver, 10, 1UL);
            Assert.AreEqual((int)Math.Round(90 * 10 * 2.0), ItemPricing.Price(item));
        }

        [Test, Description("Il valore va in linea retta dal livello minimo al 55, fermo fuori; il tiro lo sposta; un valore positivo resta almeno 1")]
        public void Values_GrowWithTheLevel()
        {
            var strength = Property(AffixEffect.Strength, 10, 40);
            Assert.AreEqual(10, strength.ValueAt(10, 10));
            Assert.AreEqual(40, strength.ValueAt(55, 10));
            Assert.AreEqual(25, strength.ValueAt(33, 10), "10 + 30 × 23 / 45");
            Assert.AreEqual(10, strength.ValueAt(3, 10), "sotto il minimo vale il minimo");
            Assert.AreEqual(40, strength.ValueAt(70, 10), "sopra il 55 vale il 55");
            Assert.AreEqual(44, strength.ValueAt(55, 10, 1.1));
            Assert.AreEqual(9, strength.ValueAt(10, 10, 0.9));
            Assert.AreEqual(1, Property(AffixEffect.LifeSteal, 1, 2).ValueAt(10, 10, 0.9), "0,9 arrotonda a 1");
            Assert.AreEqual(1, Property(AffixEffect.LifeSteal, 1, 1).ValueAt(10, 10, 0.4), "mai sotto 1");
        }

        [Test, Description("Stesso seme e livello, stessi valori; ogni valore entro il ±10% del suo livello, e i semi diversi li cambiano")]
        public void GenerateNamed_IsReproducibleAndWithinTheSpread()
        {
            var cleaver = Unique("Axe", null,
                Property(AffixEffect.WeaponDamagePercent, 70, 200), Property(AffixEffect.Strength, 10, 40),
                Property(AffixEffect.LifeSteal, 3, 6), Property(AffixEffect.Life, 15, 80));
            var seen = new HashSet<int>();
            for (ulong seed = 1; seed <= 1000; seed++)
            {
                int level = 10 + (int)(seed % 46);
                var item = ItemGenerator.GenerateNamed(cleaver, level, seed);
                Assert.AreEqual(Rarity.Unique, item.Rarity);
                Assert.AreSame(cleaver.Base, item.Definition);
                Assert.AreSame(cleaver, item.Named);
                Assert.IsTrue(item.IsNamed);
                Assert.IsEmpty(item.Affixes);
                Assert.AreEqual(level, item.ItemLevel);
                CollectionAssert.AreEqual(item.NamedValues, ItemGenerator.GenerateNamed(cleaver, level, seed).NamedValues, $"seme {seed}");
                for (int i = 0; i < cleaver.Properties.Count; i++)
                {
                    var property = cleaver.Properties[i];
                    int low = property.ValueAt(level, 10, 1.0 - ItemGenerator.NamedSpread);
                    int high = property.ValueAt(level, 10, 1.0 + ItemGenerator.NamedSpread);
                    Assert.That(item.NamedValues[i], Is.InRange(low, high), $"seme {seed} proprietà {i}");
                }

                seen.Add(item.NamedValues[0]);
            }

            Assert.Greater(seen.Count, 50, "il danno cambia da un seme all'altro");
        }

        [Test, Description("La tabella delle rarità non fa mai unici né set: chiesti al generatore, escono rari")]
        public void Generator_NeverMakesNamedItemsFromTheTable()
        {
            var generator = new ItemGenerator(AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset").Affixes, RarityTable.Default);
            Assert.AreEqual(Rarity.Rare, generator.Generate(Load<ItemDefinition>("Axe"), 10, 7UL, Rarity.Unique).Rarity);
            Assert.AreEqual(Rarity.Rare, generator.Generate(Load<ItemDefinition>("Axe"), 10, 7UL, Rarity.Set).Rarity);
        }

        [Test, Description("Un unico indossato mette le sue proprietà sul cavaliere, tutti gli attributi su tutti e quattro; toglierlo riporta tutto a prima")]
        public void Unique_PutsItsPropertiesOnTheHero()
        {
            var stats = Hero();
            var equipment = new Equipment(stats);
            float[] before = Snapshot(stats);
            var cleaver = Unique("Axe", null,
                Property(AffixEffect.WeaponDamagePercent, 70, 200), Property(AffixEffect.Strength, 10, 40),
                Property(AffixEffect.AllAttributes, 5, 15), Property(AffixEffect.LifeSteal, 3, 6),
                Property(AffixEffect.AttackSpeed, 15, 25), Property(AffixEffect.DamageReduction, 2, 12),
                Property(AffixEffect.DamageBonus, 5, 20), Property(AffixEffect.ExperienceBonus, 5, 15));
            var item = new ItemInstance(cleaver, 10, 1UL, new[] { 70, 10, 5, 3, 15, 2, 5, 5 });

            Assert.IsTrue(equipment.TryEquip(item, out _));
            Assert.AreEqual(55f, stats.Get(StatType.Strength), "40 + 10 + 5");
            Assert.AreEqual(25f, stats.Get(StatType.Dexterity));
            Assert.AreEqual(15f, stats.Get(StatType.Magic));
            Assert.AreEqual(30f, stats.Get(StatType.Vitality));
            Assert.AreEqual(3f, stats.Get(StatType.LifeSteal));
            Assert.AreEqual(15f, stats.Get(StatType.AttackSpeed));
            Assert.AreEqual(2f, stats.Get(StatType.DamageReduction));
            Assert.AreEqual(5f, stats.Get(StatType.DamageBonus));
            Assert.AreEqual(5f, stats.Get(StatType.ExperienceBonus));
            Assert.AreEqual((17, 25), ItemStats.WeaponDamage(item), "ascia 10–15 con +70%, troncato");

            equipment.Unequip(EquipSlot.Weapon);
            CollectionAssert.AreEqual(before, Snapshot(stats));
        }

        [Test, Description("I bonus del set si accendono con 2, 3 e 4 pezzi e si spengono togliendoli")]
        public void SetBonuses_FollowThePiecesWorn()
        {
            var stats = Hero();
            var equipment = new Equipment(stats);
            var pieces = Warden(out _);
            using var bonuses = new SetBonuses(equipment, stats);
            float[] before = Snapshot(stats);

            Assert.IsTrue(equipment.TryEquip(ItemGenerator.GenerateNamed(pieces[2], 10, 1UL), out _));
            float lifeOne = stats.Get(StatType.Life);
            float armorOne = stats.Get(StatType.Armor);

            Assert.IsTrue(equipment.TryEquip(ItemGenerator.GenerateNamed(pieces[3], 10, 2UL), out _));
            var boots = equipment.Get(EquipSlot.Boots);
            Assert.AreEqual(lifeOne + boots.NamedValues[1] + 25f, stats.Get(StatType.Life), "due pezzi: +25 vita");

            Assert.IsTrue(equipment.TryEquip(ItemGenerator.GenerateNamed(pieces[0], 10, 3UL), out _));
            Assert.AreEqual(armorOne + ItemStats.ItemArmor(equipment.Get(EquipSlot.Helm)) + boots.NamedValues[0] + ItemStats.ItemArmor(boots) + 10f,
                stats.Get(StatType.Armor), "tre pezzi: +10 Armatura");
            Assert.AreEqual(0f, stats.Get(StatType.DamageReduction));

            Assert.IsTrue(equipment.TryEquip(ItemGenerator.GenerateNamed(pieces[1], 10, 4UL), out _));
            Assert.AreEqual(4f, stats.Get(StatType.DamageReduction), "set completo");
            Assert.AreEqual(4, SetBonuses.CountWorn(equipment, pieces[0].Set, out int level));
            Assert.AreEqual(10, level);

            equipment.Unequip(EquipSlot.Body);
            Assert.AreEqual(0f, stats.Get(StatType.DamageReduction), "tolto un pezzo, il bonus del set completo si spegne");
            equipment.Unequip(EquipSlot.Helm);
            equipment.Unequip(EquipSlot.Gloves);
            equipment.Unequip(EquipSlot.Boots);
            CollectionAssert.AreEqual(before, Snapshot(stats));
        }

        [Test, Description("I bonus valgono al livello del pezzo più basso indossato, senza tiro")]
        public void SetBonuses_UseTheLowestPiece()
        {
            var stats = Hero();
            var equipment = new Equipment(stats);
            var pieces = Warden(out var set);
            using var bonuses = new SetBonuses(equipment, stats);

            equipment.TryEquip(ItemGenerator.GenerateNamed(pieces[0], 55, 1UL), out _);
            equipment.TryEquip(ItemGenerator.GenerateNamed(pieces[3], 55, 2UL), out _);
            float high = stats.Get(StatType.Life) - equipment.Get(EquipSlot.Helm).NamedValues[1] - equipment.Get(EquipSlot.Boots).NamedValues[1];
            Assert.AreEqual(120f, high, "tutti e due al 55");

            equipment.TryEquip(ItemGenerator.GenerateNamed(pieces[3], 10, 3UL), out _);
            float low = stats.Get(StatType.Life) - equipment.Get(EquipSlot.Helm).NamedValues[1] - equipment.Get(EquipSlot.Boots).NamedValues[1];
            Assert.AreEqual(25f, low, "gli stivali al 10 portano giù il bonus");
            Assert.AreEqual(2, SetBonuses.CountWorn(equipment, set, out int level));
            Assert.AreEqual(10, level);
        }

        [Test, Description("Lo stesso pezzo nei due anelli conta una volta; due pezzi diversi contano due")]
        public void SetBonuses_CountEachPieceOnce()
        {
            var stats = Hero();
            var equipment = new Equipment(stats);
            var set = Set();
            var left = Unique("Ring", set, Property(AffixEffect.Strength, 4, 15));
            var right = Unique("Ring", set, Property(AffixEffect.Dexterity, 4, 15));
            Fill(set, new[] { left, right }, new SetBonus(2, new[] { Property(AffixEffect.Life, 30, 100) }));
            using var bonuses = new SetBonuses(equipment, stats);

            Assert.IsTrue(equipment.TryEquip(ItemGenerator.GenerateNamed(left, 10, 1UL), EquipSlot.Ring, out _));
            Assert.IsTrue(equipment.TryEquip(ItemGenerator.GenerateNamed(left, 10, 2UL), EquipSlot.Ring2, out _));
            Assert.AreEqual(1, SetBonuses.CountWorn(equipment, set, out _));
            Assert.AreEqual(0f, stats.Get(StatType.Life));

            Assert.IsTrue(equipment.TryEquip(ItemGenerator.GenerateNamed(right, 10, 3UL), EquipSlot.Ring2, out _));
            Assert.AreEqual(2, SetBonuses.CountWorn(equipment, set, out _));
            Assert.AreEqual(30f, stats.Get(StatType.Life));
            Assert.IsTrue(SetBonuses.IsWorn(equipment, left));
            Assert.IsTrue(SetBonuses.IsWorn(equipment, right));
        }

        [Test, Description("Dispose toglie i bonus e smette di ascoltare l'equipaggiamento")]
        public void SetBonuses_DisposeRemovesEverything()
        {
            var stats = Hero();
            var equipment = new Equipment(stats);
            var pieces = Warden(out _);
            var bonuses = new SetBonuses(equipment, stats);
            equipment.TryEquip(ItemGenerator.GenerateNamed(pieces[0], 10, 1UL), out _);
            equipment.TryEquip(ItemGenerator.GenerateNamed(pieces[3], 10, 2UL), out _);
            float with = stats.Get(StatType.Life);

            bonuses.Dispose();
            Assert.AreEqual(with - 25f, stats.Get(StatType.Life));
            float armor = stats.Get(StatType.Armor);
            var gauntlets = ItemGenerator.GenerateNamed(pieces[2], 10, 3UL);
            equipment.TryEquip(gauntlets, out _);
            Assert.AreEqual(armor + ItemStats.ItemArmor(gauntlets), stats.Get(StatType.Armor), "il terzo pezzo non accende più l'Armatura in più");
            Assert.AreEqual(with - 25f, stats.Get(StatType.Life));
        }

        [Test, Description("Unici e pezzi si salvano in JSON con l'id e i valori, e tornano uguali")]
        public void NamedItem_SurvivesJson()
        {
            var cleaver = Unique("Axe", null, Property(AffixEffect.WeaponDamagePercent, 70, 200), Property(AffixEffect.Strength, 10, 40));
            var original = ItemGenerator.GenerateNamed(cleaver, 30, 42UL);
            var loaded = JsonUtility.FromJson<ItemInstance>(JsonUtility.ToJson(original));
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");

            Assert.IsNull(loaded.Named);
            Assert.IsFalse(loaded.Resolve(items, affixes), "senza il database degli unici resta da risolvere");
            Assert.IsTrue(loaded.Resolve(items, affixes, Database(cleaver)));
            Assert.AreSame(cleaver, loaded.Named);
            Assert.AreEqual(Rarity.Unique, loaded.Rarity);
            Assert.AreEqual(30, loaded.ItemLevel);
            CollectionAssert.AreEqual(original.NamedValues, loaded.NamedValues);
        }

        [Test, Description("Un unico che il database non conosce più torna un raro della sua base, senza proprietà, con un avviso (D13)")]
        public void UnknownNamedItem_BecomesRare()
        {
            var cleaver = Unique("Axe", null, Property(AffixEffect.Strength, 10, 40));
            var loaded = JsonUtility.FromJson<ItemInstance>(JsonUtility.ToJson(ItemGenerator.GenerateNamed(cleaver, 20, 5UL)));
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");

            LogAssert.Expect(LogType.Warning, new Regex("non c'è più"));
            Assert.IsTrue(loaded.Resolve(items, affixes, Database()));
            Assert.AreEqual(Rarity.Rare, loaded.Rarity);
            Assert.IsFalse(loaded.IsNamed);
            Assert.IsNull(loaded.Named);
            Assert.IsEmpty(loaded.NamedValues);
            Assert.AreSame(cleaver.Base, loaded.Definition);
        }

        [Test, Description("Un oggetto salvato prima della M16, senza i campi degli unici, si legge come prima")]
        public void OldItem_WithoutNamedFields_StillResolves()
        {
            var sword = new ItemInstance(Load<ItemDefinition>("ShortSword"), Rarity.Magic, 3, 9UL, null);
            string json = Regex.Replace(JsonUtility.ToJson(sword), ",\"_namedId\":\"[^\"]*\",\"_namedValues\":\\[[^\\]]*\\]", string.Empty);
            StringAssert.DoesNotContain("_named", json);
            var loaded = JsonUtility.FromJson<ItemInstance>(json);
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");

            Assert.IsTrue(loaded.Resolve(items, affixes, Database()));
            Assert.IsFalse(loaded.IsNamed);
            Assert.AreEqual(Rarity.Magic, loaded.Rarity);
            Assert.IsEmpty(loaded.NamedValues);
        }

        [Test, Description("Il database trova per id e dà gli unici di una base dal loro livello, separati dai pezzi di set")]
        public void Database_FindsCandidatesByBaseAndLevel()
        {
            var cleaver = Unique("Axe", null, Property(AffixEffect.Strength, 10, 40));
            var set = Set();
            var piece = Unique("Axe", set, Property(AffixEffect.Strength, 5, 20));
            Fill(set, new[] { piece });
            var database = Database(cleaver, piece);
            var into = new List<UniqueDefinition>();

            Assert.IsTrue(database.TryGet(cleaver.Id, out var found));
            Assert.AreSame(cleaver, found);
            Assert.IsFalse(database.TryGet("nessuno", out _));

            database.Candidates(Load<ItemDefinition>("Axe"), 10, Rarity.Unique, into);
            CollectionAssert.AreEqual(new[] { cleaver }, into);
            database.Candidates(Load<ItemDefinition>("Axe"), 10, Rarity.Set, into);
            CollectionAssert.AreEqual(new[] { piece }, into);
            database.Candidates(Load<ItemDefinition>("Axe"), 9, Rarity.Unique, into);
            CollectionAssert.IsEmpty(into, "sotto il livello minimo");
            database.Candidates(Load<ItemDefinition>("Dagger"), 30, Rarity.Unique, into);
            CollectionAssert.IsEmpty(into, "un'altra base");
        }

        [Test, Description("Un unico ha il suo nome intero, senza affissi")]
        public void NamedItem_HasItsOwnName()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv");
            var localizer = new Localizer(StringTable.Parse(csv.text));
            var item = ItemGenerator.GenerateNamed(Unique("Dagger", null, Property(AffixEffect.Dexterity, 8, 35)), 10, 1UL);

            localizer.SetLanguage("it");
            Assert.AreEqual("Morvak il Carceriere", ItemNamer.Name(item, localizer));
            localizer.SetLanguage("en");
            Assert.AreEqual("Morvak the Jailer", ItemNamer.Name(item, localizer));
        }
    }
}
