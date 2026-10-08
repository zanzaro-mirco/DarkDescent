using System.IO;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Save;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Il formato 2 e la migrazione (D9 della M8): un salvataggio vero del formato 1, congelato in
    /// <c>Fixtures/save_v1.json</c> quando il formato 1 era quello corrente, si carica con il codice
    /// di oggi; la mappa scoperta di ogni profondità resta tornandoci e passa dal salvataggio.
    /// </summary>
    public class SaveMigrationTests
    {
        private const string Sample =
            "@depth 1\n@entrance Start\n" +
            "#########\n" +
            "#...#...#\n" +
            "#.<.#.c.#\n" +
            "#...#...#\n" +
            "##.######\n" +
            "##.######\n" +
            "#..>....#\n" +
            "#########\n";

        private const string Other =
            "@depth 2\n@entrance Start\n" +
            "#####\n" +
            "#.<.#\n" +
            "#...#\n" +
            "#####\n";

        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), "DarkDescentMigration_" + System.Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(_path);
        }

        [Test, Description("Il salvataggio del formato 1 si carica con il codice del formato 2: migrato, senza mappe, con tutto il resto com'era")]
        public void FormatOne_LoadsWithFormatTwoCode()
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v1.json");
            StringAssert.Contains("\"_version\": 1", fixture.text, "il file congelato è davvero del formato 1");
            File.WriteAllText(_path, fixture.text);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var data));
            Assert.AreEqual(2, data.Version, "portato al formato corrente");
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.IsNotNull(data.Explored);
            Assert.AreEqual(0, data.Explored.Count, "un salvataggio vecchio non ha mappe");
            Assert.AreEqual(639270066672624430UL, data.RunSeed);
            Assert.AreEqual(("Level_Caves", "FromAbove", 6), (data.Scene, data.Entrance, data.Depth));
            Assert.AreEqual((8, 1234, 5), (data.Level, data.Experience, data.UnspentPoints));
            Assert.AreEqual(40f, data.Strength);
            Assert.AreEqual(77f, data.Life);

            // l'inventario torna intero, con il secondo anello Letale nel secondo slot
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, data.Strength);
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(sheet));
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
            Assert.IsTrue(data.Inventory.Restore(inventory, items, affixes));
            Assert.AreEqual("KnightHelm", inventory.Equipment.Get(EquipSlot.Helm).Definition.name);
            Assert.AreEqual("ShortSword", inventory.Equipment.Get(EquipSlot.Weapon).Definition.name);
            var ring = inventory.Equipment.Get(EquipSlot.Ring2);
            Assert.AreEqual("Deadly", ring.Affixes[0].Definition.name);
            Assert.AreEqual(4, ring.Affixes[0].Value);
            Assert.AreEqual(4f, sheet.Get(StatType.CritChance));
            Assert.AreEqual("Dagger", inventory.Grid.ItemAt(new Vector2Int(2, 1)).Definition.name);
            Assert.AreEqual(1, inventory.Belt.Count);

            // e riscritto, è del formato 2
            SaveFile.Write(_path, data);
            StringAssert.Contains("\"_version\": 2", File.ReadAllText(_path));
        }

        [Test, Description("Tornando a una profondità già visitata, la mappa scoperta è quella di prima; ogni profondità ha la sua")]
        public void Memory_KeepsEachDepth()
        {
            var memory = new ExplorationMemory();
            var first = memory.Open(1, LevelMap.Parse(Sample));
            first.Reveal(new Vector2Int(2, 2), 3);
            int seen = first.ExploredCount;
            Assert.Greater(seen, 0);

            var second = memory.Open(2, LevelMap.Parse(Other));
            Assert.AreEqual(0, second.ExploredCount, "una profondità nuova parte coperta");

            var again = memory.Open(1, LevelMap.Parse(Sample));
            Assert.AreEqual(seen, again.ExploredCount, "il livello rigenerato riprende le celle viste");
            Assert.IsTrue(again.IsExplored(2, 2));
        }

        [Test, Description("Le mappe passano dal salvataggio: esportate, scritte, rilette e importate, riaprendo il livello le celle viste ci sono; una mappa di un'altra misura si ignora")]
        public void Memory_GoesThroughTheSave()
        {
            var memory = new ExplorationMemory();
            var level = memory.Open(1, LevelMap.Parse(Sample));
            level.Reveal(new Vector2Int(2, 2), 3);
            int seen = level.ExploredCount;
            memory.Open(2, LevelMap.Parse(Other)).Reveal(new Vector2Int(2, 1), 1);

            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            var data = SaveData.Create(1UL, "Level_Crypt", "Start", 1)
                .WithInventory(InventorySnapshot.Capture(inventory))
                .WithExplored(memory.Export());
            SaveFile.Write(_path, data);
            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var read));
            Assert.AreEqual(2, read.Explored.Count);

            var restored = new ExplorationMemory();
            restored.Import(read.Explored);
            var reopened = restored.Open(1, LevelMap.Parse(Sample));
            Assert.AreEqual(seen, reopened.ExploredCount);
            Assert.IsTrue(reopened.IsExplored(2, 2));
            Assert.IsFalse(reopened.IsExplored(6, 2), "la stanza dietro il muro resta coperta");

            // la profondità 2 rigenerata con un'altra forma: la mappa salvata non vale più
            var changed = restored.Open(2, LevelMap.Parse(Sample));
            Assert.AreEqual(0, changed.ExploredCount);
        }
    }
}
