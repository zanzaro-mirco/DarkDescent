using System.IO;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Save;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>Il salvataggio su disco (D7–D10 della M8): il formato, la scrittura sicura, i file che non vanno.</summary>
    public class SaveDataTests
    {
        private string _folder;

        private string SavePath => Path.Combine(_folder, "save.json");

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentSaveTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_folder, true);
        }

        private static SaveData Sample()
        {
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            inventory.Grid.TryPlace(new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Dagger.asset")), new UnityEngine.Vector2Int(1, 0));
            return SaveData.Create(18446744073709551000UL, "Level_Caves", "FromAbove", 6)
                .WithProgress(7, 512, 3)
                .WithAttributes(41f, 22f, 10f, 30f)
                .WithLife(87.5f)
                .WithInventory(InventorySnapshot.Capture(inventory));
        }

        [Test, Description("Scritto e riletto, il salvataggio è identico; il seme a 64 bit non perde cifre")]
        public void RoundTrip_KeepsEverything()
        {
            var data = Sample();
            SaveFile.Write(SavePath, data);

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(SavePath, out var read));
            Assert.AreEqual(data.ToJson(), read.ToJson());
            Assert.AreEqual(18446744073709551000UL, read.RunSeed);
            Assert.AreEqual(("Level_Caves", "FromAbove", 6), (read.Scene, read.Entrance, read.Depth));
            Assert.AreEqual((7, 512, 3), (read.Level, read.Experience, read.UnspentPoints));
            Assert.AreEqual(41f, read.Strength);
            Assert.AreEqual(87.5f, read.Life);
            Assert.AreEqual(SaveData.CurrentVersion, read.Version);
        }

        [Test, Description("Un secondo salvataggio sostituisce il primo, e non resta il file temporaneo")]
        public void Write_ReplacesTheOldFile()
        {
            SaveFile.Write(SavePath, Sample());
            SaveFile.Write(SavePath, SaveData.Create(1UL, "Level_Crypt", "Start", 1).WithInventory(Sample().Inventory));

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(SavePath, out var read));
            Assert.AreEqual(1, read.Depth);
            Assert.IsFalse(File.Exists(SavePath + SaveFile.TemporarySuffix));
        }

        [Test, Description("Un file vuoto, troncato o non nostro non si carica; senza file si comincia da capo")]
        public void BadFiles_AreNotLoaded()
        {
            Assert.AreEqual(SaveReadResult.Missing, SaveFile.TryRead(SavePath, out _));

            File.WriteAllText(SavePath, string.Empty);
            Assert.AreEqual(SaveReadResult.Corrupt, SaveFile.TryRead(SavePath, out _));

            string json = Sample().ToJson();
            File.WriteAllText(SavePath, json.Substring(0, json.Length / 2));
            Assert.AreEqual(SaveReadResult.Corrupt, SaveFile.TryRead(SavePath, out _));

            File.WriteAllText(SavePath, "{\"name\": \"un altro gioco\"}");
            Assert.AreEqual(SaveReadResult.Corrupt, SaveFile.TryRead(SavePath, out var data));
            Assert.IsNull(data);
        }

        [Test, Description("Un salvataggio di una versione più nuova del gioco non si carica")]
        public void NewerVersion_IsRefused()
        {
            string json = Sample().ToJson().Replace("\"_version\": 2", "\"_version\": 99");
            File.WriteAllText(SavePath, json);
            Assert.AreEqual(SaveReadResult.TooNew, SaveFile.TryRead(SavePath, out var data));
            Assert.IsNull(data);
        }

        [Test, Description("-newgame si riconosce in qualsiasi punto della riga di comando, maiuscole o no")]
        public void NewGameFlag_IsFound()
        {
            Assert.IsTrue(CommandLine.HasFlag(new[] { "DarkDescent.exe", "-lang", "it", "-NewGame" }, "-newgame"));
            Assert.IsFalse(CommandLine.HasFlag(new[] { "DarkDescent.exe", "-seed", "4711" }, "-newgame"));
            Assert.IsFalse(CommandLine.HasFlag(null, "-newgame"));
        }
    }
}
