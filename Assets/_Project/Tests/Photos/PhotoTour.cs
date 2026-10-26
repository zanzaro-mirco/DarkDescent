using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Rendering;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Il giro di foto della M17 (D2): le schermate del gioco con i semi fissi, in 1920×1080, con il
    /// filtro retrò spento e acceso. Sta in un assembly suo, che i workflow non elencano: né i push né
    /// la ronda lo eseguono. Si lancia a mano con <c>-assemblyNames DarkDescent.Tests.Photos</c> e la
    /// cartella con <see cref="FolderOption"/>.
    /// </summary>
    [Category(Category)]
    public class PhotoTour : SandboxFixture
    {
        public const string Category = "Photos";

        /// <summary>La cartella delle foto sulla riga di comando di Unity; senza, <c>Recordings/Photos</c> del progetto (fuori da git; <c>Temp</c> no, Unity lo svuota uscendo).</summary>
        public const string FolderOption = "-photos-out";

        private const int Width = 1920;
        private const int Height = 1080;
        private const int Quality = 88;

        // il mondo delle foto: dungeon e loot uguali a ogni giro
        private const ulong PhotoSeed = 20261026UL;

        private const int HeroLevel = 20;

        private const string GroundItemPath = "Assets/_Project/Prefabs/GroundItem.prefab";

        private string _saves;
        private string _folder;
        private Mouse _mouse;
        private readonly List<string> _taken = new List<string>();

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static CompositionRoot Root => Object.FindFirstObjectByType<CompositionRoot>();

        private static GameObject Hero => GameObject.Find("Player");

        private static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        [SetUp]
        public void UseTemporarySaves()
        {
            _saves = Path.Combine(Path.GetTempPath(), "DarkDescentPhotos_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saves);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _saves);
            _folder = CommandLine.TryGetValue(Environment.GetCommandLineArgs(), FolderOption, out string folder)
                ? folder
                : Path.Combine(Application.dataPath, "..", "Recordings", "Photos");
            Directory.CreateDirectory(_folder);
            PlayModeWindow.SetCustomRenderingResolution(Width, Height, "Photos");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            PlayerPrefs.DeleteKey(RetroFilter.Preference);
            yield return null;
            Directory.Delete(_saves, true);
        }

        [UnityTest, Timeout(900000), Description("Le schermate del gioco, dal menu alla vittoria, salvate come foto per il prima e il dopo della M17")]
        public IEnumerator Tour()
        {
            // il menu e le opzioni
            ForgetPreferences();
            _mouse = InputSystem.AddDevice<Mouse>();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            yield return Snap("01_menu", world: false);
            var menu = Find<MainMenu>();
            menu.OptionsButton.onClick.Invoke();
            yield return Frames(2);
            yield return Snap("02_options", world: false);
            Find<OptionsPanel>().Close();
            yield return Frames(2);

            // i riconoscimenti partono da sotto il bordo: si aspetta che arrivino a metà schermo
            menu.CreditsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(14f);
            yield return Snap("16_credits", world: false);
            Find<CreditsPanel>().BackButton.onClick.Invoke();
            yield return Frames(2);

            // l'accampamento, con un cavaliere del livello 20 e un raro per slot
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            // il seme del mondo e quello del loot: lo stesso equipaggiamento a ogni giro (il banco del
            // mercante viene dalla partita nuova e può cambiare)
            Manager.RunSeed = PhotoSeed;
            Root.UseLootSeed(PhotoSeed);
            Root.PrepareHero(HeroLevel);
            yield return Settle();
            yield return Snap("03_camp");

            var merchant = Manager.CurrentLevel.Merchant;
            Hero.GetComponent<PlayerController>().Use(merchant.GetComponent<Interactable>());
            var shop = Find<ShopPanel>();
            for (float t = 0f; !shop.IsOpen && t < 10f; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            yield return Frames(5);
            yield return Snap("04_merchant");
            shop.Close();
            yield return Frames(2);

            Manager.CurrentLevel.Altar.GetComponent<Interactable>().Use(Hero);
            var altar = Find<DifficultyPanel>();
            for (float t = 0f; !altar.IsOpen && t < 10f; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            yield return Frames(5);
            yield return Snap("05_altar");
            altar.Close();
            yield return Frames(2);

            // l'inventario con un pezzo di set addosso e il tooltip di un altro, e la scheda
            var inventory = Hero.GetComponent<PlayerInventory>();
            Hero.GetComponent<CharacterStats>().Sheet.SetBase(StatType.Strength, 80f);
            inventory.Equipment.TryEquip(ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensGauntlets"), HeroLevel, 1UL), out _);
            inventory.TryPickUp(ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensHelm"), HeroLevel, 2UL));
            inventory.TryPickUp(ItemGenerator.GenerateNamed(Named("Uniques/KingsCleaver"), HeroLevel, 3UL));
            var panel = Find<InventoryPanel>();
            panel.SetOpen(true);
            yield return Frames(2);
            Move(_mouse.position, CellOnScreen(inventory, new Vector2Int(0, 0)));
            yield return Frames(3);
            yield return Snap("06_inventory");
            Move(_mouse.position, new Vector2(Width * 0.2f, Height * 0.5f));
            panel.SetOpen(false);
            var character = Find<CharacterPanel>();
            character.Toggle();
            yield return Frames(3);
            yield return Snap("07_character");
            character.Toggle();

            // la cripta: l'ingresso, una stanza con i nemici, il loot a terra, la mappa
            yield return Descend("Level_Crypt", 2);
            yield return Snap("08_crypt");
            yield return GoToRoom();
            CallEnemies(4);
            yield return Frames(5);
            yield return Snap("09_crypt_room");
            DropLoot();
            Find<GroundLabels>().SetAlwaysVisible(true);
            yield return Frames(30);
            yield return Snap("10_loot");
            Find<GroundLabels>().SetAlwaysVisible(false);
            var automap = Find<AutomapView>();
            for (int i = 0; i < 3 && automap.Mode != AutomapMode.Overlay; i++)
            {
                automap.Cycle();
            }

            yield return Frames(3);
            yield return Snap("11_automap");
            while (automap.Mode != AutomapMode.Corner)
            {
                automap.Cycle();
            }

            // le caverne
            yield return Descend("Level_Caves", 6);
            yield return Snap("12_caves");
            yield return GoToRoom();
            CallEnemies(4);
            yield return Frames(5);
            yield return Snap("13_caves_room");

            var pause = Find<PauseMenu>();
            pause.Open();
            yield return Frames(3);
            yield return Snap("14_pause");
            pause.Close();
            yield return Frames(3);

            var victory = Find<VictoryScreen>();
            victory.Show(new RunStats(), HeroLevel);
            yield return Frames(5);
            yield return Snap("15_victory");

            // la morte, in una partita sua
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            SceneManager.LoadScene("Core");
            yield return WaitForLevel();
            yield return Settle();
            Hero.GetComponent<Health>().TakeDamage(new DamageInfo(100000f, DamageType.Physical, null));
            var death = Find<DeathScreen>();
            for (float t = 0f; !death.IsShown && t < 10f; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1.5f);
            yield return Snap("17_death");

            Debug.Log($"[DarkDescent] giro di foto: {_taken.Count} foto in {Path.GetFullPath(_folder)}\n" + string.Join("\n", _taken));
            Assert.GreaterOrEqual(_taken.Count, 17);
        }

        private static UniqueDefinition Named(string path)
        {
            return AssetDatabase.LoadAssetAtPath<UniqueDefinition>($"Assets/_Project/Data/Named/{path}.asset");
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        // dopo un livello nuovo: il titolo e la dissolvenza passano, la camera arriva
        private static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(4f);
        }

        private IEnumerator Descend(string scene, int depth)
        {
            var previous = Manager.CurrentLevel;
            Manager.LoadLevel(scene, DungeonLevel.EntranceAt(depth), depth);
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == previous
                || Manager.CurrentLevel.Depth != depth; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, $"la profondità {depth} non arriva");
                yield return null;
            }

            // nemici fermi: le foto non devono finire in un combattimento
            foreach (var enemy in Manager.CurrentLevel.Enemies.Where(e => e != null))
            {
                enemy.enabled = false;
            }

            Hero.GetComponent<Health>().Heal(100000f);
            yield return Settle();
        }

        // la stanza della prima cassa, davanti alla cassa
        private static IEnumerator GoToRoom()
        {
            var chest = Manager.CurrentLevel.Chests.FirstOrDefault();
            if (chest != null)
            {
                Hero.GetComponent<NavMeshAgent>().Warp(chest.GetComponent<Interactable>().ApproachPoint);
            }

            yield return new WaitForSecondsRealtime(1.5f);
        }

        // i nemici più vicini attorno al cavaliere, a qualche metro, fermi
        private static void CallEnemies(int count)
        {
            Vector3 center = Hero.transform.position;
            var near = Manager.CurrentLevel.Enemies.Where(e => e != null && e.gameObject.activeInHierarchy)
                .OrderBy(e => Vector3.Distance(e.transform.position, center)).Take(count).ToList();
            for (int i = 0; i < near.Count; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / near.Count;
                Vector3 spot = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 3.5f;
                if (NavMesh.SamplePosition(spot, out var hit, 2f, NavMesh.AllAreas))
                {
                    near[i].GetComponent<NavMeshAgent>().Warp(hit.position);
                    near[i].transform.rotation = Quaternion.LookRotation(Flat(center - hit.position));
                }
            }
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        // un raro, un unico, un pezzo di set e una pila d'oro attorno al cavaliere
        private static void DropLoot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GroundItem>(GroundItemPath);
            var bases = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset").Affixes;
            var generator = new ItemGenerator(affixes, RarityTable.Default);
            Vector3 center = Hero.transform.position;
            var items = new[]
            {
                generator.Generate(bases.Items.First(d => d.name == "ShortSword"), HeroLevel, 11UL, Rarity.Rare),
                ItemGenerator.GenerateNamed(Named("Uniques/MorvaksFang"), HeroLevel, 12UL),
                ItemGenerator.GenerateNamed(Named("Sets/Hunter/HuntersHood"), HeroLevel, 13UL),
                generator.Generate(bases.Items.First(d => d.name == "Ring"), HeroLevel, 14UL, Rarity.Magic),
            };
            for (int i = 0; i < items.Length; i++)
            {
                GroundItem.Spawn(prefab, items[i], center + Quaternion.Euler(0f, 60f * i - 90f, 0f) * Vector3.forward * 2f);
            }
        }

        private Vector2 CellOnScreen(PlayerInventory inventory, Vector2Int cell)
        {
            var grid = (RectTransform)Object.FindFirstObjectByType<InventoryGridView>().transform;
            var corners = new Vector3[4];
            grid.GetWorldCorners(corners);
            Vector2 topLeft = corners[1];
            float cellSize = (corners[2].x - corners[1].x) / inventory.Inventory.Grid.Width;
            return topLeft + new Vector2((cell.x + 0.5f) * cellSize, -(cell.y + 0.5f) * cellSize);
        }

        // la foto, con il filtro retrò spento e, se c'è il mondo dietro, acceso al massimo
        private IEnumerator Snap(string name, bool world = true)
        {
            var filter = Object.FindFirstObjectByType<RetroFilter>();
            var modes = world ? new[] { RetroMode.Off, RetroMode.Full } : new[] { RetroMode.Off };
            foreach (var mode in modes)
            {
                filter.SetMode(mode);
                yield return Frames(3);
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                string file = Path.Combine(_folder, mode == RetroMode.Off ? $"{name}.jpg" : $"{name}_retro.jpg");
                File.WriteAllBytes(file, shot.EncodeToJPG(Quality));
                Object.Destroy(shot);
                _taken.Add(Path.GetFileName(file));
            }

            filter.SetMode(RetroMode.Off);
        }
    }
}
