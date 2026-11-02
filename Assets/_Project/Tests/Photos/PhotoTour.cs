using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Films;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Magic;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Rendering;
using DarkDescent.Stats;
using DarkDescent.Town;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
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
            Environment.SetEnvironmentVariable(CompositionRoot.FilmsVariable, null);
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

        /// <summary>Il file del render della vittoria, nella cartella delle foto.</summary>
        public const string VictoryRender = "victory_render.png";

        [UnityTest, Timeout(600000), Description("Lo sfondo della vittoria dal gioco (D6, riserva scelta da Mirco il 27 ott 2026): la sala del re appena abbattuto, il cavaliere davanti ai resti, senza interfaccia, in PNG a 1920x1080")]
        public IEnumerator VictoryBackdrop()
        {
            yield return LoadGeneratedCore();
            for (ulong seed = 1; seed <= 10; seed++)
            {
                Manager.RunSeed = seed;
                yield return Descend("Level_Caves", 8);
                if (Manager.CurrentLevel.BossGate != null)
                {
                    break;
                }
            }

            var level = Manager.CurrentLevel;
            Assert.IsNotNull(level.Boss, "un seme con la sala del re");
            Vector3 home = level.Boss.GetComponent<EnemyAI>().HomePosition;
            Vector3 gate = level.BossGate.transform.position;

            // il cavaliere tra la porta e il re, girato verso di lui; poi il re cade
            Vector3 spot = home + Flat(gate - home) * 3.5f;
            Assert.IsTrue(NavMesh.SamplePosition(spot, out var hit, 2f, NavMesh.AllAreas), "un posto davanti al re");
            Hero.GetComponent<NavMeshAgent>().Warp(hit.position);
            Hero.transform.rotation = Quaternion.LookRotation(Flat(home - hit.position));
            level.Boss.Health.TakeDamage(new DamageInfo(level.Boss.Health.Max * 10f, DamageType.Physical, Hero));
            yield return new WaitForSecondsRealtime(4f);

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            Object.FindFirstObjectByType<RetroFilter>().SetMode(RetroMode.Off);
            yield return Frames(3);
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(_folder, VictoryRender), shot.EncodeToPNG());
            Object.Destroy(shot);
        }

        private static UniqueDefinition Named(string path)
        {
            return AssetDatabase.LoadAssetAtPath<UniqueDefinition>($"Assets/_Project/Data/Named/{path}.asset");
        }

        [UnityTest, Timeout(300000), Description("Il borgo nei quattro momenti del giorno, con il filtro retrò spento e acceso (D5 della M18)")]
        public IEnumerator VillageDay()
        {
            // dal menu, come la partita nuova: senza menu si comincia nella cripta
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);

            // davanti al banco del mercante, con il fuoco e la forgia nell'inquadratura
            Hero.GetComponent<NavMeshAgent>().Warp(Manager.CurrentLevel.Merchant.GetComponent<Interactable>().ApproachPoint);
            yield return Settle();

            var stats = Root.RunStats;
            foreach (var moment in new[] { DayMoment.Dawn, DayMoment.Day, DayMoment.Dusk, DayMoment.Night })
            {
                stats.Restore(DayCycle.Start(moment) + 20f, stats.Kills, stats.Deaths, stats.GoldCollected, stats.BossDefeated);
                yield return Frames(5);
                yield return Snap("village_" + moment.ToString().ToLowerInvariant());
            }

            // di notte tra le case, accanto alla finestra accesa più vicina alla piazza
            Vector3 square = Hero.transform.position;
            var lamp = Object.FindObjectsByType<NightLight>(FindObjectsSortMode.None).OrderBy(l => Vector3.Distance(l.transform.position, square)).First();
            if (NavMesh.SamplePosition(lamp.transform.position, out var near, 4f, NavMesh.AllAreas))
            {
                Hero.GetComponent<NavMeshAgent>().Warp(near.position);
            }

            yield return new WaitForSecondsRealtime(1.5f);
            yield return Snap("village_night_lights");
        }

        [UnityTest, Timeout(300000), Description("La console in basso (D18 della M20): le quattro caselle piene, il menu di una casella e quello di K, a 16:9 e a 4:3")]
        public IEnumerator Hud()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);

            var magic = Hero.GetComponent<PlayerMagic>();
            foreach (string name in new[] { "BattleShout", "Whirlwind", "ShieldWall" })
            {
                magic.Learn(AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset"));
            }

            yield return Settle();
            var picker = Find<SkillMenu>();
            foreach (var (width, height, tag) in new[] { (1920u, 1080u, "16x9"), (1440u, 1080u, "4x3") })
            {
                PlayModeWindow.SetCustomRenderingResolution(width, height, "Photos");
                yield return Frames(5);
                picker.Close();
                yield return Snap("hud_bar_" + tag);
                Find<SpellBarView>().ClickSlot(2);
                yield return Frames(3);
                yield return Snap("hud_pick_" + tag, world: false);
                picker.Toggle();
                yield return Frames(3);
                yield return Snap("hud_menu_" + tag, world: false);
            }
        }

        [UnityTest, Timeout(300000), Description("Fabbro, guaritrice e mercante del borgo, ognuno con il suo banco aperto (D18 della M18)")]
        public IEnumerator VillageShops()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);

            var level = Manager.CurrentLevel;
            var agent = Hero.GetComponent<NavMeshAgent>();
            foreach (var (goods, name) in new[] { (ShopGoods.Weapons, "smith"), (ShopGoods.Potions, "healer"), (ShopGoods.Jewelry, "merchant") })
            {
                var interactable = level.Trading(goods).GetComponent<Interactable>();
                agent.Warp(interactable.ApproachPoint);
                yield return Settle();
                yield return Snap("village_" + name);
                interactable.Use(Hero);
                yield return Frames(5);
                yield return Snap("village_" + name + "_shop", false);
                Find<ShopPanel>().Close();
                Find<InventoryPanel>().SetOpen(false);
            }
        }

        [UnityTest, Timeout(300000), Description("La bocca della grotta nel borgo, di giorno, di notte e sotto il cursore, e lo sbocco del cunicolo alla profondità 1 (D6 e D7 della M18)")]
        public IEnumerator CaveAndTunnel()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);

            // davanti alla grotta, nella gola
            var exit = Manager.CurrentLevel.Exits[0];
            var agent = Hero.GetComponent<NavMeshAgent>();
            agent.Warp(exit.transform.position + exit.transform.forward * 5f);
            yield return Settle();
            yield return Snap("cave_day");

            var interactable = exit.GetComponent<Interactable>();
            interactable.SetHighlighted(true);
            yield return Frames(3);
            yield return Snap("cave_hover");
            interactable.SetHighlighted(false);

            var stats = Root.RunStats;
            stats.Restore(DayCycle.Start(DayMoment.Night) + 20f, stats.Kills, stats.Deaths, stats.GoldCollected, stats.BossDefeated);
            yield return Frames(5);
            yield return Snap("cave_night");

            // si entra nella grotta e si arriva davanti al cunicolo
            agent.SetDestination(exit.transform.position);
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != 1; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 20f, "la grotta non porta alla profondità 1");
                yield return null;
            }

            foreach (var enemy in Manager.CurrentLevel.Enemies.Where(e => e != null))
            {
                enemy.enabled = false;
            }

            yield return Settle();
            yield return Snap("crypt_tunnel");

            // il cunicolo vuoto e senza interfaccia: la prima immagine dello stacco della vittoria (D8 e D11 della M19)
            HideHeroAndHud();
            yield return Frames(3);
            yield return Snap("crypt_tunnel_empty");
        }

        [UnityTest, Timeout(300000), Description("Il borgo dei filmati (19.3): la notte, metà delle candele spente, il cimitero con le tombe smosse e la lanterna, l'alba con il fuoco spento")]
        public IEnumerator FilmSet()
        {
            ForgetPreferences();
            // lo zoom più largo: le case attorno alla piazza nell'inquadratura
            PlayerPrefs.SetFloat(CameraZoom.Preference, ZoomLevel.Max);
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            HideHeroAndHud();
            var view = Manager.CurrentLevel.Daylight;
            var agent = Hero.GetComponent<NavMeshAgent>();

            // la piazza di notte, con il fuoco e le case attorno
            view.Hold(DayCycle.Start(DayMoment.Night) + 20f);
            agent.Warp(Manager.CurrentLevel.Merchant.GetComponent<Interactable>().ApproachPoint);
            yield return Settle();
            yield return Snap("set_night");

            // le candele che si spengono una a una: una su due già spenta
            var lights = Object.FindObjectsByType<NightLight>(FindObjectsSortMode.None)
                .OrderBy(l => Vector3.Distance(l.transform.position, Hero.transform.position)).ToArray();
            for (int i = 0; i < lights.Length; i += 2)
            {
                lights[i].Snuff(0.5f);
            }

            yield return new WaitForSecondsRealtime(1f);
            yield return Snap("set_night_snuffed");
            foreach (var light in lights)
            {
                light.Relight(0f);
            }

            // il cimitero: le tre tombe più vicine alla cripta smosse, e una lanterna posata a terra
            var crypt = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "Crypt" || t.name == "Crypt(Clone)").position;
            var graves = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "Grave" || t.name == "Grave(Clone)")
                .OrderBy(t => Vector3.Distance(t.position, crypt)).Take(3).ToArray();
            var dug = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Village/GraveDug.prefab");
            foreach (var grave in graves)
            {
                Object.Instantiate(dug, grave.position, grave.rotation, grave.parent);
                grave.gameObject.SetActive(false);
            }

            var lantern = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Village/HandLantern.prefab"));
            lantern.transform.position = graves[0].position - graves[0].forward * 1.3f + graves[0].right * 1.1f;
            float bottom = lantern.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.y);
            lantern.transform.position += Vector3.up * (graves[0].position.y - bottom);
            // la camera segue il cavaliere nascosto: lo si posa sulla tomba, anche fuori dal NavMesh
            agent.enabled = false;
            Hero.transform.position = graves[0].position;
            Debug.Log($"[DarkDescent] foto: cripta {crypt}, tombe smosse {string.Join(", ", graves.Select(g => g.position))}");
            yield return Settle();
            yield return Snap("set_graves");
            view.Hold(DayCycle.Start(DayMoment.Day) + 20f);
            yield return Frames(5);
            yield return Snap("set_graves_day");

            // l'alba nella piazza, con il fuoco spento
            view.Hold(DayCycle.Start(DayMoment.Dawn) + 20f);
            Object.FindFirstObjectByType<Hearth>().SetBurning(false);
            agent.enabled = true;
            agent.Warp(Manager.CurrentLevel.Merchant.GetComponent<Interactable>().ApproachPoint);
            yield return Settle();
            yield return Snap("set_dawn");
        }

        [UnityTest, Timeout(300000), Description("L'apertura (19.4): una foto per scena, con le bande e le didascalie, dalla partita nuova del menu")]
        public IEnumerator Opening()
        {
            ForgetPreferences();
            PlayerPrefs.SetString(CompositionRoot.LanguagePreference, "it");
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            Environment.SetEnvironmentVariable(CompositionRoot.FilmsVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            var films = Root.Films;
            for (float t = 0f; !films.IsPlaying; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 10f, "l'apertura non parte");
                yield return null;
            }

            // il filmato fermo all'istante di ogni foto: il tempo del gioco a 0, la Timeline portata lì
            var director = Root.CurrentFilm.Director;
            Time.timeScale = 0f;
            foreach (var (name, time) in OpeningShots)
            {
                director.time = time;
                director.Evaluate();
                yield return new WaitForSecondsRealtime(1.6f);
                yield return Snap(name, world: false);
            }

            Time.timeScale = 1f;
            films.Press();
            films.Press();
            yield return Frames(3);
            Assert.IsFalse(films.IsPlaying);
            Assert.IsNull(Root.CurrentFilm, "il set smontato");
            ForgetPreferences();
        }

        [UnityTest, Timeout(120000), Description("Il pulsante Filmati del menu iniziale e il suo elenco, con la vittoria già vista (D18 della M19)")]
        public IEnumerator FilmsMenu()
        {
            ForgetPreferences();
            PlayerPrefs.SetString(CompositionRoot.LanguagePreference, "it");
            FilmsPanel.MarkVictorySeen();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            yield return Snap("films_1_menu", world: false);
            Find<MainMenu>().FilmsButton.onClick.Invoke();
            yield return Frames(3);
            yield return Snap("films_2_list", world: false);
            ForgetPreferences();
        }

        [UnityTest, Timeout(300000), Description("Il video dell'apertura (D12): MP4 1920x1080 a 30 fotogrammi, dall'inizio fino alla camera del gioco")]
        public IEnumerator OpeningVideo()
        {
            ForgetPreferences();
            PlayerPrefs.SetString(CompositionRoot.LanguagePreference, "it");
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            Environment.SetEnvironmentVariable(CompositionRoot.FilmsVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            var films = Root.Films;
            for (float t = 0f; !films.IsPlaying; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 10f, "l'apertura non parte");
                yield return null;
            }

            var recorder = StartMovie("opening");
            Root.CurrentFilm.Director.time = 0d;
            while (films.IsPlaying)
            {
                yield return null;
            }

            // la sfumatura nella camera del gioco
            yield return new WaitForSeconds(2.5f);
            yield return StopMovie(recorder, "opening");
            ForgetPreferences();
        }

        [UnityTest, Timeout(600000), Description("Il video della vittoria (D12): MP4 1920x1080 a 30 fotogrammi, dal colpo al re fino alla finestra, con il gioco tra un pezzo e l'altro")]
        public IEnumerator VictoryVideo()
        {
            yield return ToTheKing();
            var recorder = StartMovie("victory");
            yield return new WaitForSeconds(0.5f);
            var level = Manager.CurrentLevel;
            level.Boss.Health.TakeDamage(new DamageInfo(level.Boss.Health.Max * 10f, DamageType.Physical, Hero));
            yield return WaitForFilmEnd(FilmLibrary.VictoryFallId);

            // un secondo di gioco, poi il cavaliere va nel portale
            yield return new WaitForSeconds(1f);
            EnterVictoryPortal();
            yield return WaitForFilmEnd(FilmLibrary.VictoryLeaveId);
            yield return WaitForFilmEnd(FilmLibrary.VictoryReturnId);
            var victory = Find<VictoryScreen>();
            for (float t = 0f; !victory.IsOpen; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 10f, "la finestra della vittoria non si apre");
                yield return null;
            }

            yield return new WaitForSecondsRealtime(2.5f);
            yield return StopMovie(recorder, "victory");
            ForgetPreferences();
        }

        // aspetta che il filmato parta e arrivi in fondo da solo
        private static IEnumerator WaitForFilmEnd(string id)
        {
            var films = Root.Films;
            for (float t = 0f; !films.IsPlaying || films.Current.Id != id; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 30f, $"il filmato {id} non parte");
                yield return null;
            }

            while (films.IsPlaying && films.Current.Id == id)
            {
                yield return null;
            }
        }

        // A tempo fisso: un fotogramma del video è un trentesimo di secondo del gioco, qualunque cosa
        // costi disegnarlo. Il rallentatore del re resta rallentato anche nel video.
        private RecorderController StartMovie(string name)
        {
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = name;
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            };
            // con la musica e i suoni dei filmati (19.6)
            movie.CaptureAudio = true;
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = Width, OutputHeight = Height };
            movie.OutputFile = Path.Combine(_folder, name);
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            settings.FrameRate = 30f;
            settings.CapFrameRate = true;
            var recorder = new RecorderController(settings);
            recorder.PrepareRecording();
            Assert.IsTrue(recorder.StartRecording(), "Recorder non parte");
            return recorder;
        }

        private IEnumerator StopMovie(RecorderController recorder, string name)
        {
            recorder.StopRecording();
            yield return Frames(5);
            Assert.IsTrue(File.Exists(Path.Combine(_folder, name + ".mp4")));
        }

        [UnityTest, Timeout(600000), Description("La vittoria (19.5): il re che cade, il cavaliere davanti alle sue ossa, il ritorno nel borgo all'alba e la finestra, con le bande e le didascalie")]
        public IEnumerator Victory()
        {
            yield return ToTheKing();
            var level = Manager.CurrentLevel;
            level.Boss.Health.TakeDamage(new DamageInfo(level.Boss.Health.Max * 10f, DamageType.Physical, Hero));
            yield return Frames(2);
            yield return SnapFilm(VictoryFallShots, "il re che cade");

            EnterVictoryPortal();
            yield return SnapFilm(VictoryLeaveShots, "il cavaliere davanti alle ossa");

            for (float t = 0f; !Root.Films.IsPlaying || Root.Films.Current.Id != FilmLibrary.VictoryReturnId; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 20f, "il ritorno nel borgo non parte");
                yield return null;
            }

            yield return SnapFilm(VictoryReturnShots, "il ritorno nel borgo");
            var victory = Find<VictoryScreen>();
            for (float t = 0f; !victory.IsOpen; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 10f, "la finestra della vittoria non si apre");
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1f);
            yield return Snap("victory_4_window", world: false);
            ForgetPreferences();
        }

        // Una partita nuova dal menu, in italiano, con i filmati: l'apertura parte e si salta. Poi la
        // sala del re di un seme che ce l'ha, con il cavaliere davanti al re
        private IEnumerator ToTheKing()
        {
            ForgetPreferences();
            PlayerPrefs.SetString(CompositionRoot.LanguagePreference, "it");
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            Environment.SetEnvironmentVariable(CompositionRoot.FilmsVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
            yield return Frames(3);
            Root.Films.Press();
            Root.Films.Press();
            yield return Frames(3);
            for (ulong seed = 1; seed <= 10; seed++)
            {
                Manager.RunSeed = seed;
                yield return Descend("Level_Caves", 8);
                if (Manager.CurrentLevel.BossGate != null)
                {
                    break;
                }
            }

            var level = Manager.CurrentLevel;
            Assert.IsNotNull(level.Boss, "un seme con la sala del re");
            _kingHome = level.Boss.GetComponent<EnemyAI>().HomePosition;
            Vector3 gate = level.BossGate.transform.position;
            Vector3 spot = _kingHome + Flat(gate - _kingHome) * 3.5f;
            Assert.IsTrue(NavMesh.SamplePosition(spot, out var hit, 2f, NavMesh.AllAreas), "un posto davanti al re");
            Hero.GetComponent<NavMeshAgent>().Warp(hit.position);
            Hero.transform.rotation = Quaternion.LookRotation(Flat(_kingHome - hit.position));
            yield return Frames(3);
        }

        // il cavaliere entra nel portale della vittoria, da un metro e mezzo dalla parte del re
        private void EnterVictoryPortal()
        {
            var portal = Find<TownPortals>().VictoryPortal;
            Assert.IsNotNull(portal, "il portale della vittoria");
            Assert.IsTrue(NavMesh.SamplePosition(portal.transform.position + Flat(_kingHome - portal.transform.position) * 1.5f, out var hit, 2f, NavMesh.AllAreas));
            Hero.GetComponent<NavMeshAgent>().Warp(hit.position);
            Hero.GetComponent<PlayerController>().Use(portal.Interactable);
        }

        // aspetta il filmato, lo ferma a ogni istante per la foto, poi lo porta in fondo
        private IEnumerator SnapFilm((string Name, float Time)[] shots, string what)
        {
            var films = Root.Films;
            for (float t = 0f; !films.IsPlaying || Root.CurrentFilm == null; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, $"non parte: {what}");
                yield return null;
            }

            var set = Root.CurrentFilm;
            var director = set.Director;
            Time.timeScale = 0f;
            foreach (var (name, time) in shots)
            {
                director.time = time;
                director.Evaluate();
                yield return new WaitForSecondsRealtime(1.6f);
                yield return Snap(name, world: false);
            }

            Time.timeScale = 1f;
            director.time = set.Script.Duration - 0.05f;
            // il set smontato è distrutto, e per Unity uguale a null: si confrontano i riferimenti
            for (float t = 0f; ReferenceEquals(Root.CurrentFilm, set); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 10f, $"non finisce: {what}");
                yield return null;
            }
        }

        // dove stava il re, per mettere il cavaliere davanti a lui e poi davanti al portale
        private Vector3 _kingHome;

        private static readonly (string Name, float Time)[] VictoryFallShots =
        {
            ("victory_1_blow", 0.15f),
            ("victory_1_fall", 1.6f),
            ("victory_1_bones", 4.6f),
        };

        private static readonly (string Name, float Time)[] VictoryLeaveShots =
        {
            ("victory_2_look", 1.4f),
            ("victory_2_walk", 3.5f),
        };

        private static readonly (string Name, float Time)[] VictoryReturnShots =
        {
            ("victory_3_tunnel", 3f),
            ("victory_3_road", 7f),
            ("victory_3_cheer", 11f),
            ("victory_3_candles", 13.5f),
            ("victory_3_home", 16.7f),
        };

        private static readonly (string Name, float Time)[] OpeningShots =
        {
            ("opening_1_night", 1.5f),
            ("opening_1_out", 8f),
            ("opening_2_rise", 15f),
            ("opening_2_flee", 18.5f),
            ("opening_3_dawn", 26f),
            ("opening_4_arrival", 33f),
            ("opening_4_altar", 40f),
            ("opening_5_title", 44.5f),
            ("opening_5_descent", 49.5f),
        };

        // per le scene senza il cavaliere: il cavaliere, la sua luce e l'interfaccia spariscono
        private static void HideHeroAndHud()
        {
            foreach (var part in Hero.GetComponentsInChildren<Renderer>())
            {
                part.enabled = false;
            }

            foreach (var light in Hero.GetComponentsInChildren<Light>())
            {
                light.enabled = false;
            }

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace))
            {
                canvas.enabled = false;
            }
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
