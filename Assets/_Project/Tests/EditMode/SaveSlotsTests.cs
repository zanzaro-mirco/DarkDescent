using System;
using System.IO;
using DarkDescent.Items;
using DarkDescent.Save;
using DarkDescent.Stats;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>I tre posti di salvataggio (D2 della M10).</summary>
    public class SaveSlotsTests
    {
        private string _folder;
        private SaveSlots _slots;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentSlots_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _slots = new SaveSlots(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_folder, true);
        }

        private void Write(int slot, int depth, DateTime written)
        {
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            SaveFile.Write(_slots.PathOf(slot), SaveData.Create(7UL, "Level_Crypt", "FromAbove", depth).WithInventory(InventorySnapshot.Capture(inventory)));
            File.SetLastWriteTimeUtc(_slots.PathOf(slot), written);
        }

        [Test, Description("Tre posti, save_1…save_3 nella cartella; fuori da 1–3 non ce n'è")]
        public void Paths_AreOnePerSlot()
        {
            Assert.AreEqual(Path.Combine(_folder, "save_2.json"), _slots.PathOf(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => _slots.PathOf(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _slots.PathOf(4));
        }

        [Test, Description("Senza salvataggi niente da continuare e il primo posto è libero; l'ultimo giocato è il file scritto per ultimo")]
        public void LastPlayed_IsTheNewestFile()
        {
            Assert.AreEqual(0, _slots.LastPlayed());
            Assert.AreEqual(1, _slots.FirstFree());

            Write(1, 2, new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc));
            Write(3, 5, new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(3, _slots.LastPlayed());
            Assert.AreEqual(2, _slots.FirstFree());
            Assert.IsTrue(_slots.TryRead(3, out var data));
            Assert.AreEqual(5, data.Depth);
            Assert.IsFalse(_slots.TryRead(2, out _));

            Write(1, 2, new DateTime(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(1, _slots.LastPlayed());
        }

        [Test, Description("Un file rovinato non è l'ultimo giocato, ma il posto resta occupato e si può cancellare")]
        public void CorruptFile_IsNotContinued_ButCanBeDeleted()
        {
            File.WriteAllText(_slots.PathOf(2), "{ rotto");
            Assert.AreEqual(0, _slots.LastPlayed());
            Assert.IsTrue(_slots.IsUsed(2));
            _slots.Delete(2);
            Assert.IsFalse(_slots.IsUsed(2));
        }

        [Test, Description("Il save.json di prima diventa il posto 1, spostato e non copiato; se il posto 1 c'è già, resta dov'è")]
        public void Legacy_MovesToSlotOne()
        {
            Assert.IsFalse(_slots.MigrateLegacy(), "niente da spostare");
            string legacy = Path.Combine(_folder, SaveSlots.LegacyFile);
            File.WriteAllText(legacy, "vecchio");
            Assert.IsTrue(_slots.MigrateLegacy());
            Assert.IsFalse(File.Exists(legacy));
            Assert.AreEqual("vecchio", File.ReadAllText(_slots.PathOf(1)));

            File.WriteAllText(legacy, "un altro");
            Assert.IsFalse(_slots.MigrateLegacy());
            Assert.AreEqual("un altro", File.ReadAllText(legacy), "non si perde");
            Assert.AreEqual("vecchio", File.ReadAllText(_slots.PathOf(1)));
        }
    }
}
