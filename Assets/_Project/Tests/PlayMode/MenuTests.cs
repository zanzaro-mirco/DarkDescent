using System;
using System.Collections;
using System.IO;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Save;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Il menu iniziale, i tre posti, la pausa e le opzioni (D2 e D10 della M10).</summary>
    public class MenuTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentMenu_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, null);
            PlayerPrefs.DeleteKey(OptionsPanel.MasterPreference);
            PlayerPrefs.DeleteKey(OptionsPanel.LabelsPreference);
            PlayerPrefs.DeleteKey(OptionsPanel.OrbNumbersPreference);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static MainMenu Menu => UnityEngine.Object.FindFirstObjectByType<MainMenu>();

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        // Core da sola con il menu acceso, come nella build
        private static IEnumerator LoadWithMenu()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
        }

        private void WriteSlot(int slot, int depth, DateTime written)
        {
            var slots = new SaveSlots(_folder);
            var inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(new StatSheet()));
            var data = SaveData.Create(4242UL, "Level_Crypt", "FromAbove", depth)
                .WithProgress(3, 10, 0)
                .WithAttributes(30f, 20f, 10f, 25f)
                .WithLife(60f)
                .WithInventory(InventorySnapshot.Capture(inventory))
                .WithGold(77);
            SaveFile.Write(slots.PathOf(slot), data);
            File.SetLastWriteTimeUtc(slots.PathOf(slot), written);
        }

        [UnityTest, Description("Il menu mostra la versione del gioco in piccolo, quella della build (D7 della M11)")]
        public IEnumerator Menu_ShowsTheVersion()
        {
            yield return LoadWithMenu();
            Assert.IsTrue(Menu.VersionText.gameObject.activeInHierarchy);
            Assert.AreEqual("v" + Application.version, Menu.VersionText.text);
            StringAssert.StartsWith("1.", Application.version, "la v1.0");
        }

        [UnityTest, Description("Nelle build il menu c'è sempre: -difficulty 2 -herolevel 20 valgono per una partita nuova scelta dal menu, che le salva; riprendendola non contano più (passo 14.7)")]
        public IEnumerator NewGame_TakesTheTestOptions()
        {
            WriteSlot(2, 3, DateTime.UtcNow);
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, "-difficulty 2 -herolevel 20");
            yield return LoadWithMenu();
            var root = UnityEngine.Object.FindFirstObjectByType<CompositionRoot>();
            Assert.AreEqual(0, root.Difficulty.Current, "sotto il menu non c'è ancora una partita");

            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            Assert.AreEqual((2, 1), (root.Difficulty.Current, root.Difficulty.HighestCompleted));
            Assert.AreEqual(20, GameObject.Find("Player").GetComponent<PlayerProgress>().Progress.Level);
            Assert.IsNotNull(GameObject.Find("Player").GetComponent<PlayerInventory>().Equipment.Get(EquipSlot.Weapon), "l'equipaggiamento del livello");

            string path = Path.Combine(_folder, "save_1.json");
            for (float t = 0f; !File.Exists(path); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "entrando nel livello non ha salvato nel posto 1");
                yield return null;
            }

            yield return null;
            Assert.IsTrue(new SaveSlots(_folder).TryRead(1, out SaveData saved));
            Assert.AreEqual((2, 20), (saved.Difficulty, saved.Level));

            // il posto 2 è una partita da riprendere: le opzioni non la toccano
            yield return LoadWithMenu();
            root = UnityEngine.Object.FindFirstObjectByType<CompositionRoot>();
            Menu.LoadButton.onClick.Invoke();
            Menu.SlotView(2).Button.onClick.Invoke();
            yield return WaitForLevel();
            Assert.AreEqual(0, root.Difficulty.Current, "ripresa, resta la difficoltà salvata");
            Assert.AreEqual(3, GameObject.Find("Player").GetComponent<PlayerProgress>().Progress.Level);
        }

        [UnityTest, Description("Senza salvataggi: il menu copre tutto, Continua manca, il livello non parte; Nuova partita nel posto 1 parte dalla città e salva lì entrando")]
        public IEnumerator NewGame_StartsAndSavesInTheSlot()
        {
            yield return LoadWithMenu();
            Assert.IsTrue(Menu.IsShown);
            Assert.IsFalse(Menu.ContinueButton.gameObject.activeSelf, "niente da continuare");
            Assert.IsNull(Manager.CurrentLevel, "sotto il menu il gioco non parte");

            Menu.NewGameButton.onClick.Invoke();
            Assert.IsTrue(Menu.IsChoosingSlot);
            StringAssert.Contains("empty", Menu.SlotView(1).Text);
            Menu.SlotView(1).Button.onClick.Invoke();
            Assert.IsFalse(Menu.IsShown);
            yield return WaitForLevel();
            Assert.IsTrue(Manager.CurrentLevel.IsTown, "si comincia in città (D3)");

            string path = Path.Combine(_folder, "save_1.json");
            for (float t = 0f; !File.Exists(path); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "entrando nel livello non ha salvato nel posto 1");
                yield return null;
            }
        }

        [UnityTest, Description("Continua riprende l'ultimo giocato, con profondità e oro; Carica mostra i tre posti con il riepilogo")]
        public IEnumerator Continue_ResumesTheNewestSlot()
        {
            WriteSlot(1, 2, new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc));
            WriteSlot(3, 3, new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc));
            yield return LoadWithMenu();
            Assert.IsTrue(Menu.ContinueButton.gameObject.activeSelf);

            Menu.LoadButton.onClick.Invoke();
            StringAssert.Contains("Slot 3 – level 3 · Crypt – Level 3 · Dusk", Menu.SlotView(3).Text);
            Assert.IsFalse(Menu.SlotView(2).Button.interactable, "un posto vuoto non si carica");
            Assert.IsFalse(Menu.SlotView(2).DeleteButton.gameObject.activeSelf);

            Menu.ContinueButton.onClick.Invoke();
            yield return WaitForLevel();
            Assert.AreEqual(3, Manager.CurrentLevel.Depth, "l'ultimo giocato è il posto 3");
            Assert.AreEqual(4242UL, Manager.RunSeed);
            Assert.AreEqual(77, GameObject.Find("Player").GetComponent<PlayerInventory>().Inventory.Gold);
        }

        [UnityTest, Description("Una nuova partita sopra un posto occupato chiede conferma; cancellare un posto anche")]
        public IEnumerator OccupiedSlot_AsksBeforeOverwritingOrDeleting()
        {
            WriteSlot(2, 4, new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc));
            yield return LoadWithMenu();
            Menu.LoadButton.onClick.Invoke();
            Menu.SlotView(2).DeleteButton.onClick.Invoke();
            Assert.IsTrue(Menu.Confirm.IsOpen);
            StringAssert.Contains("Delete slot 2", Menu.Confirm.Message);
            Menu.Confirm.No.onClick.Invoke();
            Assert.IsTrue(File.Exists(Path.Combine(_folder, "save_2.json")), "No non cancella");

            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(2).Button.onClick.Invoke();
            Assert.IsTrue(Menu.Confirm.IsOpen);
            StringAssert.Contains("Overwrite slot 2", Menu.Confirm.Message);
            Assert.IsTrue(Menu.IsShown, "senza conferma non parte");
            Menu.Confirm.Yes.onClick.Invoke();
            Assert.IsFalse(Menu.IsShown);
            yield return WaitForLevel();
            Assert.IsTrue(Manager.CurrentLevel.IsTown, "una partita nuova riparte dalla città");
        }

        [UnityTest, Description("Il save.json di prima diventa il posto 1 e si può continuare")]
        public IEnumerator LegacySave_BecomesSlotOne()
        {
            WriteSlot(1, 2, DateTime.UtcNow);
            File.Move(Path.Combine(_folder, "save_1.json"), Path.Combine(_folder, SaveSlots.LegacyFile));
            yield return LoadWithMenu();
            Assert.IsTrue(File.Exists(Path.Combine(_folder, "save_1.json")));
            Assert.IsTrue(Menu.ContinueButton.gameObject.activeSelf);
        }

        [UnityTest, Description("Esc prima chiude l'inventario, poi mette in pausa (tempo e suoni fermi, cavaliere fermo); di nuovo Esc riprende")]
        public IEnumerator Escape_ClosesWindowsThenPauses()
        {
            yield return LoadCore();
            var reader = Player.GetComponent<PlayerInputReader>();
            var pause = UnityEngine.Object.FindFirstObjectByType<PauseMenu>();
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryPanel>();
            inventory.SetOpen(true);

            reader.PressPause();
            Assert.IsFalse(inventory.IsOpen);
            Assert.IsFalse(pause.IsOpen, "la prima volta chiude la finestra");

            UnityEngine.Object.FindFirstObjectByType<HitStop>().Trigger(5f);
            reader.PressPause();
            Assert.IsTrue(pause.IsOpen);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
            Assert.IsFalse(Player.GetComponent<PlayerController>().enabled);
            Assert.IsFalse(UnityEngine.Object.FindFirstObjectByType<HitStop>().IsActive, "l'hit stop finisce prima della pausa");

            reader.PressPause();
            Assert.IsFalse(pause.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
            Assert.IsTrue(Player.GetComponent<PlayerController>().enabled);
        }

        [UnityTest, Description("Salva ed esci torna al menu iniziale, e la partita si può continuare")]
        public IEnumerator SaveAndQuit_ReturnsToTheMenu()
        {
            yield return LoadWithMenu();
            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            var reader = GameObject.Find("Player").GetComponent<PlayerInputReader>();
            reader.PressPause();
            var pause = UnityEngine.Object.FindFirstObjectByType<PauseMenu>();
            pause.QuitButton.onClick.Invoke();
            yield return null;
            yield return null;
            Assert.IsTrue(Menu.IsShown, "di nuovo il menu iniziale");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(Menu.ContinueButton.gameObject.activeSelf, "la partita è salvata");
        }

        [UnityTest, Description("Le opzioni: il volume generale va nel mixer in decibel e resta; i nomi sempre visibili accendono le etichette senza Alt")]
        public IEnumerator Options_ApplyVolumeAndLabels()
        {
            yield return LoadCore();
            var options = UnityEngine.Object.FindFirstObjectByType<OptionsPanel>();
            options.Open();
            options.Master.value = 0.5f;
            var mixer = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Project/Audio/Main.mixer");
            Assert.IsTrue(mixer.GetFloat(OptionsPanel.MasterParameter, out float db), "il parametro è esposto");
            Assert.AreEqual(OptionsPanel.ToDecibels(0.5f), db, 0.01f);
            Assert.AreEqual(0.5f, PlayerPrefs.GetFloat(OptionsPanel.MasterPreference), 0.001f);

            var labels = UnityEngine.Object.FindFirstObjectByType<GroundLabels>();
            Assert.IsFalse(labels.IsActive);
            options.Labels.isOn = true;
            Assert.IsTrue(labels.IsActive);
            options.Labels.isOn = false;
            options.Back.onClick.Invoke();
            Assert.IsFalse(options.IsOpen);
            mixer.SetFloat(OptionsPanel.MasterParameter, 0f);
        }

        [UnityTest, Description("L'opzione dei numeri sulle sfere (v1.0.1): spenta di partenza, i numeri compaiono solo con il mouse sopra; accesa, vita e mana restano scritti e la scelta resta anche riaprendo il gioco")]
        public IEnumerator Options_KeepOrbNumbersVisible()
        {
            yield return LoadCore();
            var options = UnityEngine.Object.FindFirstObjectByType<OptionsPanel>();
            var life = UnityEngine.Object.FindFirstObjectByType<HealthOrb>().Readout;
            var mana = UnityEngine.Object.FindFirstObjectByType<ManaOrb>().Readout;
            Assert.IsFalse(options.OrbNumbers.isOn, "spenta di partenza");
            // la spunta è il nostro segno bianco tinto d'oro: quello di Unity è già grigio scuro e sul
            // marrone del riquadro non si vedeva (prova della v1.0.1)
            foreach (var toggle in new[] { options.Labels, options.OrbNumbers })
            {
                var check = (UnityEngine.UI.Image)toggle.graphic;
                Assert.AreEqual("check", check.sprite.texture.name, toggle.name);
            }

            Assert.IsFalse(life.IsShowing);
            Assert.IsFalse(mana.IsShowing);

            options.Open();
            options.OrbNumbers.isOn = true;
            Assert.IsTrue(life.IsShowing, "la vita resta scritta");
            Assert.IsTrue(mana.IsShowing, "il mana resta scritto");
            StringAssert.Contains(" / ", life.Text);
            life.Show(false);
            Assert.IsTrue(life.IsShowing, "il mouse che esce non la spegne");
            Assert.AreEqual(1, PlayerPrefs.GetInt(OptionsPanel.OrbNumbersPreference));
            options.Back.onClick.Invoke();

            // riaperto il gioco, la scelta resta: come LoadCore, senza dimenticare le preferenze
            SceneManager.LoadScene("Core");
            SceneManager.LoadScene("Level_01", LoadSceneMode.Additive);
            yield return WaitForLevel();
            yield return null;
            options = UnityEngine.Object.FindFirstObjectByType<OptionsPanel>();
            life = UnityEngine.Object.FindFirstObjectByType<HealthOrb>().Readout;
            Assert.IsTrue(options.OrbNumbers.isOn);
            Assert.IsTrue(life.IsShowing);

            options.Open();
            options.OrbNumbers.isOn = false;
            Assert.IsFalse(life.IsShowing, "spenta, di nuovo solo con il mouse");
            life.Show(true);
            Assert.IsTrue(life.IsShowing);
            options.Back.onClick.Invoke();
        }
    }
}
