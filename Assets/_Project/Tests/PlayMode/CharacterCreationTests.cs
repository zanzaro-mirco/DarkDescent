using System;
using System.Collections;
using System.IO;
using DarkDescent.Classes;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Magic;
using DarkDescent.Progression;
using DarkDescent.Save;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>La creazione del personaggio dopo il posto (D8 della M21): le classi, il modello, il nome, i posti e il kit di partenza.</summary>
    public class CharacterCreationTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentCreation_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static MainMenu Menu => UnityEngine.Object.FindFirstObjectByType<MainMenu>();

        private static GameObject Hero => GameObject.Find("Player");

        private static IEnumerator LoadWithMenu()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
        }

        // un posto di prima della M21: né classe né nome
        private void WriteLegacySlot(int slot)
        {
            var slots = new SaveSlots(_folder);
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            var data = SaveData.Create(4242UL, "Level_Crypt", "FromAbove", 2)
                .WithProgress(3, 10, 0)
                .WithAttributes(30f, 20f, 10f, 25f)
                .WithLife(60f)
                .WithInventory(InventorySnapshot.Capture(inventory));
            SaveFile.Write(slots.PathOf(slot), data);
        }

        private static void OpenCreation(int slot)
        {
            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(slot).Button.onClick.Invoke();
        }

        [UnityTest, Description("Dopo il posto si apre la creazione: le classi giocabili come schede, la prima scelta, con il suo testo, i numeri e il nome di partenza; il modello gira sul palco")]
        public IEnumerator Creation_OffersTheClasses()
        {
            yield return LoadWithMenu();
            OpenCreation(1);
            var creation = Menu.Creation;
            Assert.IsTrue(creation.IsOpen);
            Assert.IsFalse(Menu.IsChoosingSlot);
            Assert.IsNotNull(creation.Card("warrior"));
            Assert.IsNotNull(creation.Card("mage"));
            Assert.AreEqual("warrior", creation.Selected.Id);
            Assert.AreEqual("Aldric", creation.NameField.text);
            StringAssert.Contains("Strength", creation.Attributes.text);
            Assert.IsTrue(creation.Preview.IsBuilt);
            Assert.AreEqual("knight", creation.Preview.ShownBody);

            creation.Card("mage").onClick.Invoke();
            Assert.AreEqual("mage", creation.Selected.Id);
            Assert.AreEqual("mage", creation.Preview.ShownBody, "il modello cambia con la classe");
            Assert.AreEqual("Elowen", creation.NameField.text, "il nome di partenza segue la classe");
            StringAssert.Contains("Magic 35", creation.Attributes.text);
            StringAssert.Contains("fire and frost", creation.Description.text);
            Assert.AreEqual("warrior", Hero.GetComponent<PlayerClass>().Current.Id, "il giocatore non cambia finché non si comincia");
        }

        [UnityTest, Description("Un nome scritto da chi gioca resta anche se si cambia classe; il nome di partenza invece la segue")]
        public IEnumerator Creation_KeepsATypedName()
        {
            yield return LoadWithMenu();
            OpenCreation(1);
            var creation = Menu.Creation;
            creation.NameField.text = "Mirco";
            creation.Card("mage").onClick.Invoke();
            Assert.AreEqual("Mirco", creation.NameField.text);
        }

        [UnityTest, Description("Indietro dalla creazione torna ai posti, e un salvataggio che c'era resta intatto: niente si scrive finché non si comincia")]
        public IEnumerator Back_ReturnsToTheSlotsWithTheSaveIntact()
        {
            WriteLegacySlot(2);
            yield return LoadWithMenu();
            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(2).Button.onClick.Invoke();
            Assert.IsTrue(Menu.Confirm.IsOpen);
            Menu.Confirm.Yes.onClick.Invoke();
            Assert.IsTrue(Menu.IsCreating);

            Menu.Creation.BackButton.onClick.Invoke();
            Assert.IsFalse(Menu.IsCreating);
            Assert.IsTrue(Menu.IsChoosingSlot, "di nuovo i posti");
            Assert.IsTrue(new SaveSlots(_folder).TryRead(2, out SaveData kept), "il posto 2 è com'era");
            Assert.AreEqual(3, kept.Level);
            Assert.IsTrue(Menu.IsShown);
        }

        [UnityTest, Description("Il Mago nuovo: classe e nome nel personaggio, gli attributi del Mago, la bacchetta e il tomo al posto della spada, il Dardo di fuoco in barra e il suo albero nel pannello; il posto lo scrive")]
        public IEnumerator NewMage_HasHisClassNameAndKit()
        {
            yield return LoadWithMenu();
            MenuSteps.StartNew(Menu, 1, "mage", "Merlino");
            yield return WaitForLevel();

            var hero = Hero;
            var playerClass = hero.GetComponent<PlayerClass>();
            Assert.AreEqual(("mage", "Merlino"), (playerClass.Current.Id, playerClass.CharacterName));
            var sheet = hero.GetComponent<CharacterStats>().Sheet;
            // la Magia di partenza è 35, e il tomo ne dà 5
            Assert.AreEqual((8f, 17f, 40f, 20f), (sheet.Get(StatType.Strength), sheet.Get(StatType.Dexterity), sheet.Get(StatType.Magic), sheet.Get(StatType.Vitality)));

            var equipment = hero.GetComponent<PlayerInventory>().Equipment;
            Assert.AreEqual("Wand", equipment.Get(EquipSlot.Weapon).Definition.name);
            Assert.AreEqual("Tome", equipment.Get(EquipSlot.Offhand).Definition.name);

            var magic = hero.GetComponent<PlayerMagic>();
            Assert.IsTrue(magic.Book.Knows(AssetSpell("Firebolt")), "il Dardo di fuoco della radice");
            Assert.AreEqual("mage", hero.GetComponentInChildren<DarkDescent.Characters.PlayerBody>(true).Current);
            Assert.AreEqual("mage", hero.GetComponent<PlayerTalents>().Build.ClassTree.Id);

            string path = Path.Combine(_folder, "save_1.json");
            for (float t = 0f; !File.Exists(path); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "entrando nel livello non ha salvato nel posto 1");
                yield return null;
            }

            yield return null;
            Assert.IsTrue(new SaveSlots(_folder).TryRead(1, out SaveData saved));
            Assert.AreEqual(("mage", "Merlino"), (saved.Class, saved.CharacterName));
        }

        [UnityTest, Description("Il Guerriero nuovo tiene la spada del prefab e nessun tomo; il nome di partenza è Aldric")]
        public IEnumerator NewWarrior_KeepsTheKnightsKit()
        {
            yield return LoadWithMenu();
            MenuSteps.StartNew(Menu, 1);
            yield return WaitForLevel();
            var hero = Hero;
            Assert.AreEqual(("warrior", "Aldric"), (hero.GetComponent<PlayerClass>().Current.Id, hero.GetComponent<PlayerClass>().CharacterName));
            var equipment = hero.GetComponent<PlayerInventory>().Equipment;
            Assert.AreEqual("ShortSword", equipment.Get(EquipSlot.Weapon).Definition.name);
            Assert.IsNull(equipment.Get(EquipSlot.Offhand));
        }

        [UnityTest, Description("I posti dicono nome e classe: «Merlino · Mage» per un personaggio della creazione, «Knight · Warrior» per un salvataggio di prima; un nome con segni di formattazione (< e >) ne esce ripulito")]
        public IEnumerator Slots_ShowNameAndClass()
        {
            WriteLegacySlot(3);
            yield return LoadWithMenu();
            MenuSteps.StartNew(Menu, 1, "mage", "<b>Merlino</b>");
            yield return WaitForLevel();
            string path = Path.Combine(_folder, "save_1.json");
            for (float t = 0f; !File.Exists(path); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "non ha salvato");
                yield return null;
            }

            yield return null;
            Assert.IsTrue(new SaveSlots(_folder).TryRead(1, out SaveData saved));
            Assert.AreEqual("bMerlino/b", saved.CharacterName, "senza < e >, che il testo del posto leggerebbe come formattazione");

            yield return LoadWithMenu();
            Menu.LoadButton.onClick.Invoke();
            StringAssert.StartsWith("Slot 1 – bMerlino/b · Mage · level", Menu.SlotView(1).Text);
            StringAssert.StartsWith("Slot 3 – Knight · Warrior · level 3", Menu.SlotView(3).Text, "un salvataggio di prima è il Cavaliere");
        }

        private static SpellDefinition AssetSpell(string name)
        {
            foreach (var spell in Resources.FindObjectsOfTypeAll<SpellDefinition>())
            {
                if (spell.name == name)
                {
                    return spell;
                }
            }

            return null;
        }
    }
}
