using System.Collections;
using System.IO;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Progression;
using DarkDescent.Save;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Il salvataggio nel gioco (D7–D10 della M8): entrando in un livello si salva; ricaricato, il
    /// cavaliere torna identico, con crescita, attributi, vita e inventario.
    /// </summary>
    public class SaveLoadTests : SandboxFixture
    {
        private string _path;

        [UnityTearDown]
        public IEnumerator DeleteSave()
        {
            System.Environment.SetEnvironmentVariable(SaveGame.FileVariable, null);
            if (_path != null && File.Exists(_path))
            {
                File.Delete(_path);
            }

            yield return null;
        }

        private static ItemInstance Item(string name)
        {
            return new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));
        }

        [UnityTest, Description("Entrando al livello 2 si salva; rovinato tutto, il salvataggio rimette il cavaliere com'era, fino all'ultimo oggetto")]
        public IEnumerator SaveThenLoad_RestoresTheKnight()
        {
            yield return LoadGeneratedCore();
            var save = Object.FindFirstObjectByType<SaveGame>();
            Assert.IsFalse(save.IsActive, "nell'editor il salvataggio è spento");
            _path = Path.Combine(Application.temporaryCachePath, "save_test.json");
            File.Delete(_path);
            save.UseFile(_path);

            // un cavaliere con una storia: livelli, punti spesi, oggetti indossati e in borsa, una pozione bevuta, ferite
            var progress = Player.GetComponent<PlayerProgress>().Progress;
            var inventory = Player.GetComponent<PlayerInventory>();
            var stats = Player.GetComponent<CharacterStats>();
            var health = Player.GetComponent<Health>();
            progress.Add(450);
            Assert.IsTrue(progress.TrySpend(StatType.Strength));
            Assert.IsTrue(progress.TrySpend(StatType.Strength));
            Assert.IsTrue(inventory.Equipment.TryEquip(Item("Ring"), out _));
            Assert.IsTrue(inventory.Equipment.TryEquip(Item("Ring"), out _));
            Assert.IsTrue(inventory.Equipment.TryEquip(Item("LeatherArmor"), out _));
            Assert.IsTrue(inventory.TryPickUp(Item("Dagger")));
            health.TakeDamage(new DamageInfo(60f, DamageType.Physical, null));
            Assert.IsTrue(inventory.DrinkFromBelt(0), "una pozione bevuta: nella cintura ne resta una");

            SaveData saved = null;
            save.Saved += data => saved = data;
            var manager = Object.FindFirstObjectByType<LevelManager>();
            manager.LoadLevel("Level_Crypt", "FromAbove", 2);
            for (float time = 0f; saved == null; time += Time.unscaledDeltaTime)
            {
                Assert.Less(time, 15f, "entrando nel livello non ha salvato");
                yield return null;
            }

            Assert.IsTrue(File.Exists(_path));
            Assert.AreEqual(2, saved.Depth);
            Assert.AreEqual("Level_Crypt", saved.Scene);
            Assert.AreEqual(manager.RunSeed, saved.RunSeed);
            string before = save.Capture().ToJson();
            float life = health.Current;

            // tutto rovinato: un altro cavaliere
            progress.Restore(1, 0, 0);
            stats.Sheet.SetBase(StatType.Strength, 99f);
            inventory.Inventory.Clear();
            health.Heal(1000f);

            Assert.AreEqual(SaveReadResult.Ok, save.TryLoad(out SaveData loaded));
            Assert.IsTrue(save.Apply(loaded), "ogni oggetto torna al suo posto");
            Assert.AreEqual(before, save.Capture().ToJson(), "lo stesso stato, campo per campo");
            Assert.AreEqual(life, health.Current, 0.001f);
            Assert.AreEqual(3, progress.Level);
            Assert.AreEqual(32f, stats.Sheet.GetBase(StatType.Strength), "30 di partenza e i due punti spesi");
            Assert.IsNotNull(inventory.Equipment.Get(EquipSlot.Ring2), "il secondo anello nel secondo slot");
        }

        [UnityTest, Description("All'avvio, con un salvataggio, si riparte da lì: profondità e ingresso, crescita, attributi, vita, inventario e seme")]
        public IEnumerator Startup_ResumesFromTheSave()
        {
            // un salvataggio scritto a mano, come l'avrebbe lasciato una partita al livello 3
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, 35f);
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(sheet));
            Assert.IsTrue(inventory.Equipment.TryEquip(Item("KnightHelm"), out _));
            Assert.IsTrue(inventory.Equipment.TryEquip(Item("ShortSword"), out _));
            Assert.IsTrue(inventory.Grid.TryPlace(Item("Amulet"), new Vector2Int(3, 1)));
            var data = SaveData.Create(4711UL, "Level_Crypt", "FromAbove", 3)
                .WithProgress(4, 120, 5)
                .WithAttributes(35f, 21f, 10f, 27f)
                .WithLife(50f)
                .WithInventory(InventorySnapshot.Capture(inventory));
            _path = Path.Combine(Application.temporaryCachePath, "save_startup_test.json");
            SaveFile.Write(_path, data);
            System.Environment.SetEnvironmentVariable(SaveGame.FileVariable, _path);

            yield return LoadGeneratedCore();
            yield return null;

            var manager = Object.FindFirstObjectByType<LevelManager>();
            Assert.AreEqual(3, manager.CurrentLevel.Depth, "si riparte dalla profondità salvata");
            Assert.AreEqual(4711UL, manager.RunSeed, "con il seme della partita");
            var progress = Player.GetComponent<PlayerProgress>().Progress;
            Assert.AreEqual((4, 120, 5), (progress.Level, progress.Experience, progress.UnspentPoints));
            var stats = Player.GetComponent<CharacterStats>();
            Assert.AreEqual(35f, stats.Sheet.GetBase(StatType.Strength));
            Assert.AreEqual(50f, Player.GetComponent<Health>().Current, 0.001f);
            var equipment = Player.GetComponent<PlayerInventory>().Equipment;
            Assert.AreEqual("KnightHelm", equipment.Get(EquipSlot.Helm).Definition.name, "l'elmo, che vuole Forza 25, indossato");
            Assert.AreEqual("ShortSword", equipment.Get(EquipSlot.Weapon).Definition.name);
            Assert.AreEqual("Amulet", Player.GetComponent<PlayerInventory>().Inventory.Grid.ItemAt(new Vector2Int(3, 1)).Definition.name);
            Assert.AreEqual(0, Player.GetComponent<PlayerInventory>().Belt.Count, "le pozioni di partenza non tornano: il salvataggio non le aveva");
        }

        [UnityTest, Description("Da morto si salva come dopo Continua: a vita piena, all'ingresso del livello")]
        public IEnumerator Dead_SavesAsIfContinued()
        {
            yield return LoadGeneratedCore();
            var save = Object.FindFirstObjectByType<SaveGame>();
            _path = Path.Combine(Application.temporaryCachePath, "save_test.json");
            save.UseFile(_path);
            var health = Player.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(10000f, DamageType.Physical, null));
            Assert.IsTrue(health.IsDead);

            var data = save.Capture();
            Assert.AreEqual(health.Max, data.Life);
            Assert.IsTrue(save.Save());
        }
    }
}
