using System.Collections.Generic;
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
    /// I formati dal 2 al 7 e la migrazione (D9 della M8, D11 della M9, D11 e 10.9 della M10, D14 della
    /// M14, D13 della M16): i salvataggi veri dei formati dall'1 al 6, congelati in <c>Fixtures/save_v1.json</c>…<c>save_v6.json</c>
    /// quando erano quelli correnti, si caricano con il codice di oggi; la mappa scoperta di ogni profondità resta
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
            Assert.AreEqual(7, data.Version, "portato al formato corrente");
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
            StringAssert.Contains($"\"_version\": {SaveData.CurrentVersion}", File.ReadAllText(_path));
        }

        [Test, Description("Il salvataggio vero del formato 2 si carica con il codice di oggi: il solo Dardo di fuoco, il primo posto e il mana pieno; mappa, Magia e oggetti della M9 (pergamena, pozione blu, affissi della magia) com'erano")]
        public void FormatTwo_LoadsWithFormatThreeCode()
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v2.json");
            StringAssert.Contains("\"_version\": 2", fixture.text, "il file congelato è davvero del formato 2");
            StringAssert.DoesNotContain("_knownSpells", fixture.text);
            File.WriteAllText(_path, fixture.text);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var data));
            Assert.AreEqual(7, data.Version, "passa anche dal 3 al 4, dal 4 al 5, dal 5 al 6 e dal 6 al 7");
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

        [Test, Description("Il salvataggio vero del formato 3 si carica con il codice di oggi: niente oro, nel dungeon, contatori a zero, nessun livello ricordato; incantesimi, mana, mappa e oggetti com'erano")]
        public void FormatThree_LoadsWithTodaysCode()
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v3.json");
            StringAssert.Contains("\"_version\": 3", fixture.text, "il file congelato è davvero del formato 3");
            StringAssert.DoesNotContain("_gold", fixture.text);
            File.WriteAllText(_path, fixture.text);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var data));
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.AreEqual(0, data.Gold);
            Assert.IsFalse(data.InTown);
            Assert.AreEqual(0, data.PortalDepth);
            Assert.IsNull(data.PortalSpot);
            Assert.AreEqual(0, data.Levels.Count);
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

        [Test, Description("Il formato 4 va e torna: oro, città, portale con il suo punto e contatori passano dal file")]
        public void FormatFour_RoundTrips()
        {
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            var data = SaveData.Create(9UL, "Level_Town", "FromPortal", 0)
                .WithInventory(InventorySnapshot.Capture(inventory))
                .WithGold(1234)
                .WithTown(true, 5, new Vector3(12.5f, 0.25f, -7f))
                .WithStats(3600.5f, 210, 3, 4321, true);
            SaveFile.Write(_path, data);
            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var read));
            Assert.AreEqual(1234, read.Gold);
            Assert.AreEqual((true, 5), (read.InTown, read.PortalDepth));
            Assert.AreEqual(new Vector3(12.5f, 0.25f, -7f), read.PortalSpot);
            Assert.AreEqual((3600.5f, 210, 3, 4321, true), (read.PlayTime, read.Kills, read.Deaths, read.GoldCollected, read.BossDefeated));
        }

        [Test, Description("Il salvataggio del formato 4 si carica con il codice di oggi: il portale riporta all'ingresso della profondità, nessun livello è ricordato e, senza vittoria, si resta al Crepuscolo; oro, città, portale e contatori com'erano")]
        public void FormatFour_LoadsWithTodaysCode()
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v4.json");
            StringAssert.Contains("\"_version\": 4", fixture.text, "il file congelato è davvero del formato 4");
            StringAssert.DoesNotContain("_levels", fixture.text);
            File.WriteAllText(_path, fixture.text);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var data));
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.AreEqual((0, -1, 0), (data.Difficulty, data.HighestDifficulty, data.World), "il re non era battuto: l'Ombra resta chiusa");
            Assert.AreEqual(6, data.PortalDepth);
            Assert.IsNull(data.PortalSpot, "riporta all'ingresso");
            Assert.AreEqual(0, data.Levels.Count);
            Assert.AreEqual(("Town", "Portal", 0), (data.Scene, data.Entrance, data.Depth));
            Assert.AreEqual((250, true), (data.Gold, data.InTown));
            Assert.AreEqual((1834.5f, 97, 2, 612, false), (data.PlayTime, data.Kills, data.Deaths, data.GoldCollected, data.BossDefeated));
            Assert.AreEqual(1, data.Explored.Count);
        }

        [Test, Description("Il salvataggio vero del formato 5, con il re battuto, si carica con il codice di oggi: al Crepuscolo, completato, nel primo mondo; livelli ricordati, oro, livello e contatori com'erano")]
        public void FormatFive_LoadsWithTodaysCode()
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v5.json");
            StringAssert.Contains("\"_version\": 5", fixture.text, "il file congelato è davvero del formato 5");
            StringAssert.DoesNotContain("_difficulty", fixture.text);
            File.WriteAllText(_path, fixture.text);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var data));
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.AreEqual((0, 0, 0), (data.Difficulty, data.HighestDifficulty, data.World), "il Crepuscolo completato: l'Ombra si può scegliere");
            Assert.IsTrue(data.BossDefeated);
            Assert.AreEqual(("Level_Crypt", 1), (data.Scene, data.Depth));
            Assert.AreEqual((14, 1200, 731), (data.Level, data.Experience, data.Gold));
            Assert.AreEqual(2, data.Kills);
            Assert.AreEqual(1, data.Levels.Count);
            Assert.AreEqual(2, data.Levels[0].Killed.Count);
        }

        private static NamedItemDatabase NamedItems => AssetDatabase.LoadAssetAtPath<NamedItemDatabase>("Assets/_Project/Data/NamedItemDatabase.asset");

        private static UniqueDefinition Named(string path) => AssetDatabase.LoadAssetAtPath<UniqueDefinition>($"Assets/_Project/Data/Named/{path}.asset");

        [Test, Description("Il salvataggio vero del formato 6, alla Tenebra nel quarto mondo, si carica con il codice di oggi: nessun oggetto ha un nome, e rari, magici, livelli ricordati e difficoltà sono com'erano")]
        public void FormatSix_LoadsWithTodaysCode()
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v6.json");
            StringAssert.Contains("\"_version\": 6", fixture.text, "il file congelato è davvero del formato 6");
            StringAssert.DoesNotContain("_named", fixture.text);
            File.WriteAllText(_path, fixture.text);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var data));
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.AreEqual((2, 1, 4), (data.Difficulty, data.HighestDifficulty, data.World));
            Assert.AreEqual(("Level_Caves", "FromAbove", 7), (data.Scene, data.Entrance, data.Depth));
            Assert.AreEqual((24, 4321), (data.Level, data.Gold));

            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, data.Strength);
            sheet.SetBase(StatType.Dexterity, data.Dexterity);
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(sheet));
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
            Assert.IsTrue(data.Inventory.Restore(inventory, items, affixes, NamedItems));
            var axe = inventory.Equipment.Get(EquipSlot.Weapon);
            Assert.AreEqual(("Axe", Rarity.Rare, 28), (axe.Definition.name, axe.Rarity, axe.ItemLevel));
            Assert.IsFalse(axe.IsNamed);
            Assert.Greater(axe.Affixes.Count, 0);
            Assert.AreEqual(Rarity.Magic, inventory.Equipment.Get(EquipSlot.Boots).Rarity);
            Assert.AreEqual(2, inventory.Belt.Count);

            var level = data.Levels[0];
            Assert.AreEqual(2, level.Items.Count);
            Assert.IsTrue(level.Items[0].Item.Resolve(items, affixes, NamedItems));
            Assert.AreEqual(("SpikedShield", Rarity.Rare), (level.Items[0].Item.Definition.name, level.Items[0].Item.Rarity));
            Assert.AreEqual(88, level.Items[1].Gold);
        }

        [Test, Description("Il formato 7 va e torna: un unico addosso, un pezzo di set nella borsa e uno a terra tornano con il loro nome e gli stessi valori, e i bonus del set si riaccendono")]
        public void FormatSeven_RoundTripsNamedItems()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, 60f);
            sheet.SetBase(StatType.Dexterity, 40f);
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(sheet));
            var fang = ItemGenerator.GenerateNamed(Named("Uniques/MorvaksFang"), 30, 11UL);
            var boots = ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensBoots"), 22, 12UL);
            var gauntlets = ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensGauntlets"), 25, 13UL);
            var helm = ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensHelm"), 40, 14UL);
            Assert.IsTrue(inventory.Equipment.TryEquip(fang, out _));
            Assert.IsTrue(inventory.Equipment.TryEquip(boots, out _));
            Assert.IsTrue(inventory.Equipment.TryEquip(gauntlets, out _));
            Assert.IsTrue(inventory.TryPickUp(helm));
            var record = new LevelRecord("Level_Crypt", 3, 10, 2);
            var hood = ItemGenerator.GenerateNamed(Named("Sets/Hunter/HuntersHood"), 18, 15UL);
            record.AddItem(new GroundItemRecord(hood, new Vector3(2f, 0f, 3f)));

            var data = SaveData.Create(9UL, "Level_Crypt", "Start", 3)
                .WithInventory(InventorySnapshot.Capture(inventory))
                .WithLevels(new List<LevelRecord> { record });
            SaveFile.Write(_path, data);
            string json = File.ReadAllText(_path);
            StringAssert.Contains("\"_version\": 7", json);
            StringAssert.Contains(fang.NamedId, json);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var read));
            var restoredSheet = new StatSheet();
            restoredSheet.SetBase(StatType.Strength, 60f);
            restoredSheet.SetBase(StatType.Dexterity, 40f);
            var restored = new Inventory(new InventoryGrid(10, 4), new Equipment(restoredSheet));
            using var bonuses = new SetBonuses(restored.Equipment, restoredSheet);
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
            Assert.IsTrue(read.Inventory.Restore(restored, items, affixes, NamedItems));

            var weapon = restored.Equipment.Get(EquipSlot.Weapon);
            Assert.AreSame(fang.Named, weapon.Named);
            Assert.AreEqual((Rarity.Unique, 30), (weapon.Rarity, weapon.ItemLevel));
            CollectionAssert.AreEqual(fang.NamedValues, weapon.NamedValues);
            CollectionAssert.AreEqual(boots.NamedValues, restored.Equipment.Get(EquipSlot.Boots).NamedValues);
            var bagged = restored.Grid.ItemAt(Vector2Int.zero);
            Assert.AreSame(helm.Named, bagged.Named);
            CollectionAssert.AreEqual(helm.NamedValues, bagged.NamedValues);

            // il bonus da due pezzi del Custode, al livello degli stivali
            var set = boots.Named.Set;
            Assert.IsTrue(SetBonuses.IsWorn(restored.Equipment, gauntlets.Named));
            float life = boots.NamedValues[1] + set.Bonuses[0].Properties[0].ValueAt(22, set.MinLevel);
            Assert.AreEqual(life, restoredSheet.Get(StatType.Life), 0.001f);

            var ground = read.Levels[0].Items[0].Item;
            Assert.IsTrue(ground.Resolve(items, affixes, NamedItems));
            Assert.AreSame(hood.Named, ground.Named);
            Assert.AreEqual(Rarity.Set, ground.Rarity);
            CollectionAssert.AreEqual(hood.NamedValues, ground.NamedValues);
        }

        [Test, Description("Il formato 6 va e torna: difficoltà, la più alta completata e il mondo passano dal file")]
        public void FormatSix_RoundTrips()
        {
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            var data = SaveData.Create(9UL, "Town", "Start", 0)
                .WithInventory(InventorySnapshot.Capture(inventory))
                .WithDifficulty(4, 5, 3);
            SaveFile.Write(_path, data);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var read));
            Assert.AreEqual((4, 5, 3), (read.Difficulty, read.HighestDifficulty, read.World));
            Assert.AreEqual((0, -1, 0), (SaveData.Create(9UL, "Town", "Start", 0).Difficulty, SaveData.Create(9UL, "Town", "Start", 0).HighestDifficulty, 0),
                "una partita nuova è al Crepuscolo, senza niente di completato");
        }

        [Test, Description("Il formato 5 va e torna: il punto del portale e i ricordi dei livelli, con nemici uccisi, casse aperte, oggetti e oro a terra; senza portale il punto non si salva")]
        public void FormatFive_RoundTrips()
        {
            var potion = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/HealingPotion.asset");
            var record = new LevelRecord("Level_Crypt", 2, 14, 3);
            record.AddKilled(0);
            record.AddKilled(7);
            record.AddOpened(2);
            record.AddItem(new GroundItemRecord(new ItemInstance(potion), new Vector3(4f, 0f, 9.5f)));
            record.AddItem(new GroundItemRecord(37, new Vector3(5f, 0f, 9.5f)));
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            var data = SaveData.Create(9UL, "Town", "Portal", 0)
                .WithInventory(InventorySnapshot.Capture(inventory))
                .WithTown(true, 2, new Vector3(1f, 0.5f, 3f))
                .WithLevels(new List<LevelRecord> { record });
            SaveFile.Write(_path, data);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(_path, out var read));
            Assert.AreEqual(new Vector3(1f, 0.5f, 3f), read.PortalSpot);
            Assert.AreEqual(1, read.Levels.Count);
            var level = read.Levels[0];
            Assert.AreEqual(("Level_Crypt", 2), (level.Scene, level.Depth));
            Assert.IsTrue(level.Matches(14, 3));
            Assert.IsFalse(level.Matches(15, 3), "un livello generato diverso");
            CollectionAssert.AreEqual(new[] { 0, 7 }, level.Killed);
            CollectionAssert.AreEqual(new[] { 2 }, level.Opened);
            Assert.AreEqual(2, level.Items.Count);
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
            Assert.IsTrue(level.Items[0].Item.Resolve(items, affixes));
            Assert.AreEqual(potion, level.Items[0].Item.Definition);
            Assert.AreEqual(new Vector3(4f, 0f, 9.5f), level.Items[0].Position);
            Assert.IsNull(level.Items[1].Item, "una pila d'oro");
            Assert.AreEqual(37, level.Items[1].Gold);

            Assert.IsNull(SaveData.Create(9UL, "Town", "Start", 0).WithTown(true, 0, Vector3.one).PortalSpot);
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
