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
    /// I formati 2 e 3 e la migrazione (D9 della M8, D11 della M9): i salvataggi veri dei formati 1
    /// e 2, congelati in <c>Fixtures/save_v1.json</c> e <c>save_v2.json</c> quando erano quelli
    /// correnti, si caricano con il codice di oggi; la mappa scoperta di ogni profondità resta
    /// tornandoci e passa dal salvataggio.
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

        [Test, Description("Il salvataggio del formato 1 si carica con il codice di oggi: migrato un passo alla volta, senza mappe, con tutto il resto com'era")]
        public void FormatOne_LoadsWithTodaysCode()
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v1.json");
            StringAssert.Contains("\"_version\": 1", fixture.text, "il file congelato è davvero del formato 1");
            File.WriteAllText(_path, fixture.text);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var data));
            Assert.AreEqual(4, data.Version, "portato al formato corrente");
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

            // passando anche per il formato 3: niente incantesimi imparati e il mana pieno
            Assert.AreEqual(0, data.KnownSpells.Count);
            Assert.AreEqual(SaveData.FullMana, data.Mana);

            // e riscritto, è del formato corrente
            SaveFile.Write(_path, data);
            StringAssert.Contains("\"_version\": 4", File.ReadAllText(_path));
        }

        [Test, Description("Il salvataggio vero del formato 2 si carica con il codice di oggi: il solo Dardo di fuoco, il primo posto e il mana pieno; mappa, Magia e oggetti della M9 (pergamena, pozione blu, affissi della magia) com'erano")]
        public void FormatTwo_LoadsWithFormatThreeCode()
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v2.json");
            StringAssert.Contains("\"_version\": 2", fixture.text, "il file congelato è davvero del formato 2");
            StringAssert.DoesNotContain("_knownSpells", fixture.text);
            File.WriteAllText(_path, fixture.text);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var data));
            Assert.AreEqual(4, data.Version, "passa anche dal 3 al 4");
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.AreEqual(0, data.KnownSpells.Count, "nessun incantesimo imparato: il Dardo è noto dall'inizio");
            Assert.AreEqual(0, data.SelectedSpell);
            Assert.AreEqual(SaveData.FullMana, data.Mana, "mana pieno");
            Assert.AreEqual(424242424242UL, data.RunSeed);
            Assert.AreEqual(("Level_Crypt", "FromAbove", 3), (data.Scene, data.Entrance, data.Depth));
            Assert.AreEqual((6, 2100, 2), (data.Level, data.Experience, data.UnspentPoints));
            Assert.AreEqual(18f, data.Magic);
            Assert.AreEqual(1, data.Explored.Count, "la mappa della profondità 3");

            var spells = new[] { "Firebolt", "FireRing", "StoneSkin" };
            var book = new Magic.SpellBook(System.Array.ConvertAll(spells, n => AssetDatabase.LoadAssetAtPath<Magic.SpellDefinition>($"Assets/_Project/Data/Spells/{n}.asset")));
            book.Restore(data.KnownSpells, data.SelectedSpell);
            Assert.IsTrue(book.Knows(0));
            Assert.IsFalse(book.Knows(1) || book.Knows(2));
            Assert.AreEqual(0, book.Selected);

            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, data.Strength);
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(sheet));
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
            Assert.IsTrue(data.Inventory.Restore(inventory, items, affixes));
            Assert.AreEqual(3f, sheet.Get(StatType.Magic), "l'elmo della Stregoneria");
            Assert.AreEqual(9f, sheet.Get(StatType.Mana), "l'anello Arcano");
            Assert.IsTrue(inventory.Grid.Placements.Keys.GetEnumerator().MoveNext(), "la pergamena nello zaino");
            Assert.AreEqual(2, inventory.Belt.Count, "pozione rossa e blu");
        }

        [Test, Description("Il salvataggio vero del formato 3 si carica con il codice del formato 4: niente oro, nel dungeon, contatori a zero; incantesimi, mana, mappa e oggetti com'erano")]
        public void FormatThree_LoadsWithFormatFourCode()
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v3.json");
            StringAssert.Contains("\"_version\": 3", fixture.text, "il file congelato è davvero del formato 3");
            StringAssert.DoesNotContain("_gold", fixture.text);
            File.WriteAllText(_path, fixture.text);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var data));
            Assert.AreEqual(4, data.Version);
            Assert.AreEqual(0, data.Gold);
            Assert.IsFalse(data.InTown);
            Assert.AreEqual(0, data.PortalDepth);
            Assert.AreEqual((0f, 0, 0, 0, false), (data.PlayTime, data.Kills, data.Deaths, data.GoldCollected, data.BossDefeated));
            Assert.AreEqual(5150505050505050505UL, data.RunSeed);
            Assert.AreEqual(("Level_Caves", "FromAbove", 6), (data.Scene, data.Entrance, data.Depth));
            Assert.AreEqual((9, 3400, 0), (data.Level, data.Experience, data.UnspentPoints));
            Assert.AreEqual(1, data.KnownSpells.Count);
            Assert.AreEqual(1, data.SelectedSpell);
            Assert.AreEqual(23.5f, data.Mana);
            Assert.AreEqual(1, data.Explored.Count);

            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, data.Strength);
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(sheet));
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
            Assert.IsTrue(data.Inventory.Restore(inventory, items, affixes));
            Assert.AreEqual("Axe", inventory.Equipment.Get(EquipSlot.Weapon).Definition.name);
            Assert.AreEqual(12f, sheet.Get(StatType.Mana), "l'anello Arcano");
            Assert.AreEqual(2, inventory.Belt.Count);
        }

        [Test, Description("Il formato 4 va e torna: oro, città, portale e contatori passano dal file")]
        public void FormatFour_RoundTrips()
        {
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            var data = SaveData.Create(9UL, "Level_Town", "FromPortal", 0)
                .WithInventory(InventorySnapshot.Capture(inventory))
                .WithGold(1234)
                .WithTown(true, 5)
                .WithStats(3600.5f, 210, 3, 4321, true);
            SaveFile.Write(_path, data);
            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var read));
            Assert.AreEqual(1234, read.Gold);
            Assert.AreEqual((true, 5), (read.InTown, read.PortalDepth));
            Assert.AreEqual((3600.5f, 210, 3, 4321, true), (read.PlayTime, read.Kills, read.Deaths, read.GoldCollected, read.BossDefeated));
        }

        [Test, Description("Il formato 3 va e torna: incantesimi noti, posto scelto e mana passano dal file")]
        public void FormatThree_RoundTrips()
        {
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            var data = SaveData.Create(9UL, "Level_Crypt", "FromAbove", 2)
                .WithInventory(InventorySnapshot.Capture(inventory))
                .WithMagic(new System.Collections.Generic.List<string> { "abc", "def" }, 2, 17.5f);
            SaveFile.Write(_path, data);
            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var read));
            CollectionAssert.AreEqual(new[] { "abc", "def" }, read.KnownSpells);
            Assert.AreEqual(2, read.SelectedSpell);
            Assert.AreEqual(17.5f, read.Mana);
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
