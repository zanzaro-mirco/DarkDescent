using System.Collections.Generic;
using System.Linq;
using DarkDescent.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class ItemDatabaseTests
    {
        private const string ItemsFolder = "Assets/_Project/Data/Items";
        private const string DatabasePath = "Assets/_Project/Data/ItemDatabase.asset";

        private static List<ItemDefinition> LoadDefinitions(params string[] folders)
        {
            return AssetDatabase.FindAssets("t:ItemDefinition", folders)
                .Select(guid => AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
        }

        private static ItemDatabase Database => AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);

        private static T Item<T>(string name) where T : ItemDefinition
        {
            return AssetDatabase.LoadAssetAtPath<T>($"{ItemsFolder}/{name}.asset");
        }

        [Test, Description("Ogni definizione, anche dei nemici, ha un ID non vuoto e diverso dalle altre: duplicare un asset copierebbe l'ID")]
        public void AllDefinitions_HaveUniqueIds()
        {
            var definitions = LoadDefinitions("Assets/_Project");
            Assert.IsNotEmpty(definitions);

            foreach (var definition in definitions)
            {
                Assert.IsFalse(string.IsNullOrEmpty(definition.Id), $"{definition.name} senza ID");
            }

            var duplicates = definitions.GroupBy(d => d.Id).Where(g => g.Count() > 1).Select(g => string.Join(", ", g.Select(d => d.name)));
            CollectionAssert.IsEmpty(duplicates, "ID ripetuti");
        }

        [Test, Description("Il database contiene esattamente gli oggetti di Data/Items: niente dimenticato, niente colpo di nemico")]
        public void Database_ContainsExactlyTheItemsFolder()
        {
            var expected = LoadDefinitions(ItemsFolder);

            CollectionAssert.AreEquivalent(expected, Database.Items);
            Assert.IsFalse(Database.TryGet(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Enemies/SkeletonStrike.asset").Id, out _),
                "il colpo dello scheletro non si raccoglie");
        }

        [Test, Description("Ogni oggetto ha nome, icona, modello e almeno una cella")]
        public void Items_AreComplete()
        {
            foreach (var item in Database.Items)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.DisplayName), $"{item.name} senza nome");
                Assert.IsNotNull(item.Icon, $"{item.name} senza icona");
                Assert.IsNotNull(item.Model, $"{item.name} senza modello");
                Assert.GreaterOrEqual(item.Size.x, 1);
                Assert.GreaterOrEqual(item.Size.y, 1);
                Assert.AreNotEqual(EquipSlot.None, item.Slot, $"{item.name}: per ora ogni oggetto si equipaggia");
            }
        }

        [Test, Description("Il database ritrova ogni oggetto dal suo ID, e niente da un ID sconosciuto")]
        public void Database_ResolvesById()
        {
            foreach (var item in Database.Items)
            {
                Assert.IsTrue(Database.TryGet(item.Id, out var found));
                Assert.AreSame(item, found);
            }

            Assert.IsFalse(Database.TryGet("non-esiste", out _));
            Assert.IsFalse(Database.TryGet(null, out _));
        }

        [Test, Description("Un'istanza passata per JsonUtility e riletta ritrova la stessa definizione: il JSON contiene l'ID, non un riferimento")]
        public void ItemInstance_SurvivesJson()
        {
            var shield = Item<ArmorDefinition>("BadgeShield");
            var instance = new ItemInstance(shield);

            string json = JsonUtility.ToJson(instance);
            var loaded = JsonUtility.FromJson<ItemInstance>(json);

            StringAssert.Contains(shield.Id, json);
            Assert.IsNull(loaded.Definition, "appena letta non ha ancora la definizione");
            Assert.IsTrue(loaded.Resolve(Database));
            Assert.AreSame(shield, loaded.Definition);
        }

        [Test, Description("I tre oggetti della M4 hanno i valori della scheda (D7)")]
        public void M4Items_MatchTheScheda()
        {
            var sword = Item<WeaponDefinition>("ShortSword");
            Assert.AreEqual((6, 9, 0), (sword.MinDamage, sword.MaxDamage, sword.RequiredStrength));
            Assert.AreEqual(new Vector2Int(1, 3), sword.Size);

            var blade = Item<WeaponDefinition>("SkeletonBlade");
            Assert.AreEqual((8, 12, 25), (blade.MinDamage, blade.MaxDamage, blade.RequiredStrength));
            Assert.AreEqual(new Vector2Int(1, 3), blade.Size);

            var shield = Item<ArmorDefinition>("BadgeShield");
            Assert.AreEqual(5, shield.Armor);
            Assert.AreEqual(new Vector2Int(2, 2), shield.Size);
            Assert.AreEqual(EquipSlot.Offhand, shield.Slot);
        }
    }
}
