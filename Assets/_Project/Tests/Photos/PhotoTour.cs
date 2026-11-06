using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Characters;
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
using DarkDescent.Talents;
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
            MenuSteps.StartNew(menu, 1);
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
            MenuSteps.StartNew(menu, 1);
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
            MenuSteps.StartNew(menu, 1);
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
                var readouts = Object.FindObjectsByType<OrbReadout>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var readout in readouts)
                {
                    readout.SetAlwaysVisible(true);
                }

                yield return Frames(3);
                yield return Snap("hud_numbers_" + tag, world: false);
                foreach (var readout in readouts)
                {
                    readout.SetAlwaysVisible(false);
                }

                Find<SpellBarView>().ClickSlot(2);
                yield return Frames(3);
                yield return Snap("hud_pick_" + tag, world: false);
                picker.Toggle();
                yield return Frames(3);
                yield return Snap("hud_menu_" + tag, world: false);
            }
        }

        [UnityTest, Timeout(300000), Description("La console del Mago (21.7): le quattro caselle con le sue icone, il menu di una casella e quello di K")]
        public IEnumerator MageHud()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1, "mage", "Merlino");
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);

            var magic = Hero.GetComponent<PlayerMagic>();
            foreach (string name in new[] { "FireRing", "Blink", "ManaShield", "Fireball" })
            {
                magic.Learn(AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset"));
            }

            yield return Settle();
            var picker = Find<SkillMenu>();
            PlayModeWindow.SetCustomRenderingResolution(1920u, 1080u, "Photos");
            yield return Frames(5);
            picker.Close();
            yield return Snap("mage_hud_bar");
            Find<SpellBarView>().ClickSlot(2);
            yield return Frames(3);
            yield return Snap("mage_hud_pick", world: false);
            picker.Toggle();
            yield return Frames(3);
            yield return Snap("mage_hud_menu", world: false);
        }

        [UnityTest, Timeout(300000), Description("Le armi a due mani (D5 della M20): lo spadone e l'ascia in mano al cavaliere, a riposo e a metà colpo, e la borsa con le loro icone")]
        public IEnumerator TwoHanded()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 1);
            yield return Settle();

            var inventory = Hero.GetComponent<PlayerInventory>();
            Hero.GetComponent<CharacterStats>().Sheet.SetBase(StatType.Strength, 80f);
            var driver = Hero.GetComponentInChildren<CharacterAnimatorDriver>();
            foreach (string name in new[] { "Greatsword", "GreatAxe" })
            {
                var item = new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));
                Assert.IsTrue(inventory.Inventory.Grid.TryAutoPlace(item), name);
                Assert.IsTrue(inventory.Inventory.Grid.TryGetPlacement(item, out RectInt area), name);
                Assert.IsTrue(inventory.Inventory.EquipFromGrid(area.position), name);
                yield return Settle();
                yield return Snap("twohanded_" + name.ToLowerInvariant() + "_idle");
                driver.PlayAttack();
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Snap("twohanded_" + name.ToLowerInvariant() + "_swing");
                yield return new WaitForSecondsRealtime(1.5f);
            }

            Find<InventoryPanel>().SetOpen(true);
            yield return Frames(5);
            yield return Snap("twohanded_inventory", world: false);
        }

        [UnityTest, Timeout(300000), Description("Il corpo del Mago sul rig del cavaliere (D2 della M21): a riposo, in corsa, a un colpo e al lancio, accanto al cavaliere")]
        public IEnumerator MageBody()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 1);
            yield return Settle();

            var driver = Hero.GetComponentInChildren<CharacterAnimatorDriver>();
            var playerClass = Hero.GetComponent<PlayerClass>();
            yield return Snap("body_knight_idle");
            foreach (string id in new[] { "mage" })
            {
                Root.ApplyClass(playerClass.Catalog.Find(id), "Prova");
                yield return Settle();
                yield return Snap($"body_{id}_idle");
                driver.PlayAttack();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Snap($"body_{id}_attack");
                yield return new WaitForSecondsRealtime(1.2f);
                driver.PlayCast();
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Snap($"body_{id}_cast");
            }
        }

        [UnityTest, Timeout(300000), Description("Le armi del Mago in mano (D4 della M21): bacchetta e tomo, poi il bastone a due mani, a riposo, al colpo e al lancio, e la borsa con i tre oggetti")]
        public IEnumerator MageWeapons()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 1);
            yield return Settle();

            var playerClass = Hero.GetComponent<PlayerClass>();
            Root.ApplyClass(playerClass.Catalog.Find("mage"), "Prova");
            Hero.GetComponent<CharacterStats>().Sheet.SetBase(StatType.Magic, 60f);
            yield return Settle();

            var inventory = Hero.GetComponent<PlayerInventory>();
            var driver = Hero.GetComponentInChildren<CharacterAnimatorDriver>();
            foreach (var (label, names) in new[] { ("wand_tome", new[] { "Wand", "Tome" }), ("staff", new[] { "Staff" }) })
            {
                foreach (string name in names)
                {
                    var item = new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));
                    Assert.IsTrue(inventory.Inventory.Grid.TryAutoPlace(item), name);
                    Assert.IsTrue(inventory.Inventory.Grid.TryGetPlacement(item, out RectInt area), name);
                    Assert.IsTrue(inventory.Inventory.EquipFromGrid(area.position), name);
                }

                yield return Settle();
                yield return Snap($"mageweapons_{label}_idle");
                driver.PlayAttack();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Snap($"mageweapons_{label}_swing");
                yield return new WaitForSecondsRealtime(1.5f);
                driver.PlayCast();
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Snap($"mageweapons_{label}_cast");
                yield return new WaitForSecondsRealtime(1.2f);
            }

            Find<InventoryPanel>().SetOpen(true);
            yield return Frames(5);
            yield return Snap("mageweapons_inventory", world: false);
        }

        [UnityTest, Timeout(600000), Description("Le abilità del Mago in azione (D3 della M21): il Dardo arcano e le undici, contro scheletri messi davanti, a metà effetto")]
        public IEnumerator MageSpells()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 1);
            yield return Settle();

            var playerClass = Hero.GetComponent<PlayerClass>();
            Root.ApplyClass(playerClass.Catalog.Find("mage"), "Prova");
            Hero.GetComponent<CharacterStats>().Sheet.SetBase(StatType.Magic, 60f);
            yield return Settle();

            var magic = Hero.GetComponent<PlayerMagic>();
            var agent = Hero.GetComponent<NavMeshAgent>();
            var enemies = Manager.CurrentLevel.Enemies.Where(e => e != null).Take(4).ToArray();
            Assert.GreaterOrEqual(enemies.Length, 2, "servono dei nemici");
            foreach (var enemy in enemies)
            {
                // tanta vita: devono reggere tutto il giro, e stare fermi dove li mettiamo
                enemy.GetComponent<Health>().MultiplyMax(100f);
                enemy.GetComponent<NavMeshAgent>().isStopped = true;
            }

            // gli scheletri a 6 m davanti, in fila di lato; il Mago li guarda
            void Line(float forward)
            {
                Vector3 ahead = Hero.transform.forward;
                Vector3 side = Vector3.Cross(Vector3.up, ahead);
                for (int i = 0; i < enemies.Length; i++)
                {
                    Vector3 wanted = Hero.transform.position + ahead * forward + side * ((i - (enemies.Length - 1) * 0.5f) * 1.6f);
                    if (NavMesh.SamplePosition(wanted, out var hit, 2f, NavMesh.AllAreas))
                    {
                        enemies[i].GetComponent<NavMeshAgent>().Warp(hit.position);
                    }
                }
            }

            IEnumerator Cast(string label, string spellName, float forward, float wait, bool atFirst = true)
            {
                Line(forward);
                magic.Mana.Fill();
                var spell = AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{spellName}.asset");
                magic.Learn(spell);
                yield return Frames(3);
                Vector3 target = atFirst ? enemies[enemies.Length / 2].transform.position : Hero.transform.position;
                Assert.AreEqual(CastResult.Ready, magic.TryCastAt(spell, target), spellName);
                yield return new WaitForSecondsRealtime(wait);
                yield return Snap($"magespell_{label}");
                yield return new WaitForSecondsRealtime(1.5f);
            }

            // il Dardo arcano: il click sul nemico, dopo il gesto il dardo è in volo
            Line(6f);
            yield return Frames(3);
            Hero.GetComponent<MeleeAttack>().SetTarget(enemies[1].GetComponent<Health>());
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Snap("magespell_arcane_bolt");
            yield return new WaitForSecondsRealtime(1.5f);

            yield return Cast("firebolt", "Firebolt", 6f, 0.3f);
            yield return Cast("fireball", "Fireball", 6f, 1.2f);
            yield return Cast("ice_bolt", "IceBolt", 6f, 0.9f);
            yield return Cast("fire_ring", "FireRing", 2f, 0.4f, atFirst: false);
            yield return Cast("frost_nova", "FrostNova", 2f, 0.45f, atFirst: false);
            yield return Cast("flame_wall", "FlameWall", 5f, 1.6f);
            yield return Cast("blizzard", "Blizzard", 5f, 1.6f);
            // la Meteora ha 45 s di ricarica: una sola, fotografata in aria e poi allo scoppio
            Line(6f);
            magic.Mana.Fill();
            var meteor = AssetDatabase.LoadAssetAtPath<SpellDefinition>("Assets/_Project/Data/Spells/Meteor.asset");
            magic.Learn(meteor);
            yield return Frames(3);
            Assert.AreEqual(CastResult.Ready, magic.TryCastAt(meteor, enemies[enemies.Length / 2].transform.position));
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Snap("magespell_meteor_falling");
            yield return new WaitForSecondsRealtime(0.55f);
            yield return Snap("magespell_meteor_impact");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Cast("mana_shield", "ManaShield", 6f, 0.6f, atFirst: false);
            yield return Cast("frost_armor", "FrostArmor", 6f, 0.6f, atFirst: false);

            // il Passo di luce: prima e dopo
            Line(10f);
            yield return Snap("magespell_blink_before");
            var blink = AssetDatabase.LoadAssetAtPath<SpellDefinition>("Assets/_Project/Data/Spells/Blink.asset");
            magic.Learn(blink);
            magic.Mana.Fill();
            magic.TryCastAt(blink, Hero.transform.position + Hero.transform.forward * 6f);
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Snap("magespell_blink_after");
        }

        [UnityTest, Timeout(600000), Description("Le barre sui nemici feriti (23.3, D9 e D10 della M23): lo sciame con la barra solo sui feriti, e un campione e un unico nel colore del loro grado, in inglese e in italiano")]
        public IEnumerator EnemyBars()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 1);

            var bars = Find<EnemyHealthBars>();
            bars.SetOn(true);

            // lo sciame: i sette più vicini attorno al cavaliere, quattro feriti di quanto dice il disegno (62, 34, 81 e 18 per cento)
            CallEnemies(7);
            yield return Frames(3);
            var swarm = Manager.CurrentLevel.Enemies.Where(e => e != null && e.gameObject.activeInHierarchy)
                .OrderBy(e => Vector3.Distance(e.transform.position, Hero.transform.position)).Take(7).ToArray();
            float[] lost = { 0.38f, 0f, 0.66f, 0.19f, 0f, 0.82f, 0f };
            for (int i = 0; i < swarm.Length; i++)
            {
                if (lost[i] > 0f)
                {
                    var health = swarm[i].GetComponent<Health>();
                    health.TakeDamage(new DamageInfo(health.Max * lost[i], DamageType.Physical, Hero));
                }
            }

            // il lampo bianco del colpo dura un po': la foto lo aspetta
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Snap("enemybars_swarm");

            // un campione e Morvak, vicini al cavaliere, a metà vita
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");
            ulong seed = 1;
            while (crypt.UniqueDepth(seed) != 3)
            {
                seed++;
            }

            Manager.RunSeed = seed;
            yield return Descend("Level_Crypt", 3);
            bars.SetOn(true);
            var enemies = Manager.CurrentLevel.Enemies.Where(e => e != null).ToArray();
            var normal = enemies.First(e => e.GetComponent<SpecialEnemy>() == null);
            var champion = enemies.First(e => e.GetComponent<SpecialEnemy>()?.Rank == EnemyRank.Champion);
            var morvak = enemies.First(e => e.GetComponent<SpecialEnemy>()?.Rank == EnemyRank.Unique);
            var line = new[] { normal, champion, morvak };
            Vector3 ahead = Hero.transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, ahead);
            for (int i = 0; i < line.Length; i++)
            {
                Vector3 wanted = Hero.transform.position + ahead * 3.5f + side * ((i - 1) * 3.6f);
                if (NavMesh.SamplePosition(wanted, out var hit, 2f, NavMesh.AllAreas))
                {
                    line[i].GetComponent<NavMeshAgent>().Warp(hit.position);
                    line[i].transform.rotation = Quaternion.LookRotation(Flat(Hero.transform.position - hit.position));
                }

                line[i].GetComponent<Health>().SetCurrent(line[i].GetComponent<Health>().Max * (0.7f - 0.15f * i));
            }

            yield return Frames(5);
            yield return Snap("enemybars_unique");
            Localizer.SetLanguage("it");
            yield return Frames(3);
            yield return Snap("enemybars_unique_it");
            Localizer.SetLanguage("en");
        }

        [UnityTest, Timeout(600000), Description("Quanto costano le barre (23.6 della M23): ventiquattro nemici feriti attorno al cavaliere, il tempo del main thread, le allocazioni e i batch con le barre spente e accese; il numero va in costo_barre.txt")]
        public IEnumerator BarsCost()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            MenuSteps.StartNew(Find<MainMenu>(), 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 4);

            var bars = Find<EnemyHealthBars>();
            CallEnemies(24);
            yield return Frames(3);
            var ring = Manager.CurrentLevel.Enemies.Where(e => e != null && e.gameObject.activeInHierarchy)
                .OrderBy(e => Vector3.Distance(e.transform.position, Hero.transform.position)).Take(24).ToArray();
            foreach (var enemy in ring)
            {
                var health = enemy.GetComponent<Health>();
                health.TakeDamage(new DamageInfo(health.Max * 0.4f, DamageType.Physical, Hero));
            }

            yield return new WaitForSecondsRealtime(1.5f);

            int previousRate = Application.targetFrameRate;
            int previousSync = QualitySettings.vSyncCount;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            var mainThread = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "Main Thread");
            var gcAlloc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame");
            var batches = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Batches Count");
            var lines = new List<string> { $"Cripta alla 4, {ring.Length} nemici feriti del 40% attorno al cavaliere, 600 frame per giro, vsync spento (editor)." };
            const int frames = 600;
            foreach (bool on in new[] { false, true, false, true })
            {
                bars.SetOn(on);
                yield return Frames(30);
                var times = new List<float>(frames);
                long gc = 0;
                long batchCount = 0;
                for (int i = 0; i < frames; i++)
                {
                    yield return null;
                    times.Add(mainThread.Valid ? mainThread.LastValue / 1e6f : 0f);
                    gc += gcAlloc.Valid ? gcAlloc.LastValue : 0;
                    batchCount += batches.Valid ? batches.LastValue : 0;
                }

                times.Sort();
                lines.Add($"barre {(on ? "accese" : "spente ")}: main thread {times.Average():F2} ms in media, mediana {times[frames / 2]:F2}, "
                    + $"p95 {times[(int)(frames * 0.95f)]:F2}, peggiore {times[frames - 1]:F2}; allocazioni {gc / (float)frames:F0} B a frame; batch {batchCount / (float)frames:F1} a frame; "
                    + $"barre mostrate {bars.ShownCount}");
            }

            mainThread.Dispose();
            gcAlloc.Dispose();
            batches.Dispose();
            Application.targetFrameRate = previousRate;
            QualitySettings.vSyncCount = previousSync;
            File.WriteAllLines(Path.Combine(_folder, "costo_barre.txt"), lines);
            foreach (string line in lines)
            {
                Debug.Log("BarsCost: " + line);
            }
        }

        [UnityTest, Timeout(600000), Description("Il disegno del Custode (24.1 della M24): la stanza della scala giù della profondità 4 com'è, con un Custode finto (il modello del re, tinto, con la luce) al centro, e la barra in alto di un nemico ferito per vederne lo stile")]
        public IEnumerator BossDesign()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            MenuSteps.StartNew(Find<MainMenu>(), 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 4);

            var level = Manager.CurrentLevel;
            var down = level.DownExits.First();
            Vector3 front = down.GetComponent<Interactable>().ApproachPoint;
            Vector3 Spot(Vector3 wanted) => NavMesh.SamplePosition(wanted, out var hit, 3f, NavMesh.AllAreas) ? hit.position : wanted;

            // i nemici della stanza si spengono: la foto è del posto
            foreach (var enemy in level.Enemies.Where(e => e != null && (e.transform.position - front).sqrMagnitude < 18f * 18f))
            {
                enemy.gameObject.SetActive(false);
            }

            Vector3 centre = Spot(front + new Vector3(0f, 0f, -3.5f));
            var agent = Hero.GetComponent<NavMeshAgent>();
            agent.Warp(Spot(front + new Vector3(0f, 0f, -12f)));
            yield return Settle();
            yield return Snap("boss_room_empty");

            // il Custode: il modello del re, senza i suoi componenti, tinto di viola e con una luce rossa
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/BoneKing.prefab");
            var holder = new GameObject("StandInHolder");
            holder.SetActive(false);
            var standIn = Object.Instantiate(prefab, holder.transform);
            // prima chi ne richiede un altro (RequireComponent), poi chi è richiesto
            var behaviours = standIn.GetComponentsInChildren<MonoBehaviour>(true).ToList();
            bool Requires(MonoBehaviour owner, MonoBehaviour needed)
            {
                return owner.gameObject == needed.gameObject && owner.GetType().GetCustomAttributes(typeof(RequireComponent), true).Cast<RequireComponent>()
                    .Any(r => (r.m_Type0 != null && r.m_Type0.IsAssignableFrom(needed.GetType())) || (r.m_Type1 != null && r.m_Type1.IsAssignableFrom(needed.GetType()))
                        || (r.m_Type2 != null && r.m_Type2.IsAssignableFrom(needed.GetType())));
            }

            while (behaviours.Count > 0)
            {
                var free = behaviours.FirstOrDefault(m => !behaviours.Any(o => o != m && Requires(o, m))) ?? behaviours[0];
                behaviours.Remove(free);
                Object.DestroyImmediate(free);
            }

            foreach (var part in standIn.GetComponentsInChildren<Component>(true).Where(c => c is Collider || c is NavMeshAgent || c is NavMeshObstacle || c is Rigidbody).ToArray())
            {
                if (part != null)
                {
                    Object.DestroyImmediate(part);
                }
            }

            standIn.transform.SetParent(null);
            Object.Destroy(holder);
            standIn.transform.position = centre;
            standIn.transform.rotation = Quaternion.LookRotation(Flat(Hero.transform.position - centre));
            standIn.transform.localScale = Vector3.one * 1.25f;
            foreach (var renderer in standIn.GetComponentsInChildren<Renderer>())
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", material.GetColor("_BaseColor") * new Color(0.62f, 0.45f, 0.85f, 1f));
                    }
                }
            }

            var aura = new GameObject("Aura").AddComponent<Light>();
            aura.transform.SetParent(standIn.transform, false);
            aura.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            aura.type = LightType.Point;
            aura.color = new Color(0.75f, 0.35f, 1f);
            aura.intensity = 6f;
            aura.range = 9f;
            standIn.SetActive(true);
            yield return Frames(5);
            yield return Snap("boss_room_custode");

            agent.Warp(Spot(centre + new Vector3(0f, 0f, -6f)));
            yield return Settle();
            yield return Snap("boss_custode_near");
            Object.Destroy(standIn);

            // la barra in alto, nello stile che ha: un nemico vero, ferito a metà
            CallEnemies(1);
            yield return Frames(3);
            var near = level.Enemies.Where(e => e != null && e.gameObject.activeInHierarchy).OrderBy(e => Vector3.Distance(e.transform.position, Hero.transform.position)).FirstOrDefault();
            if (near != null)
            {
                var health = near.GetComponent<Health>();
                health.TakeDamage(new DamageInfo(health.Max * 0.5f, DamageType.Physical, Hero));
            }

            yield return Frames(4);
            yield return Snap("boss_bar_reference", world: false);
        }

        // l'inizio di una partita nuova dal menu, come BossDesign
        private IEnumerator StartNewFromMenu()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            MenuSteps.StartNew(Find<MainMenu>(), 1);
            yield return WaitForLevel();
        }

        [UnityTest, Timeout(600000), Description("Vorrak il Custode vero (24.6 della M24): la sala della scala giù con lui al centro, la barra in alto, le tre evocazioni e la striscia rossa della carica")]
        public IEnumerator Keeper()
        {
            yield return StartNewFromMenu();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 4);

            var level = Manager.CurrentLevel;
            var keeper = level.Guardian;
            Assert.IsNotNull(keeper, "il Custode alla profondità 4");
            var keeperAi = keeper.GetComponent<EnemyAI>();
            keeperAi.enabled = true;
            var hero = Hero.GetComponent<Health>();
            hero.MultiplyMax(500f);
            hero.Heal(float.MaxValue);
            var agent = Hero.GetComponent<NavMeshAgent>();
            Vector3 Spot(Vector3 wanted) => NavMesh.SamplePosition(wanted, out var hit, 3f, NavMesh.AllAreas) ? hit.position : wanted;

            Vector3 front = level.DownExits.First().GetComponent<Interactable>().ApproachPoint;
            agent.Warp(Spot(front + new Vector3(0f, 0f, -12f)));
            keeperAi.enabled = false;
            yield return Settle();
            yield return Snap("keeper_room");

            // vicino, ma ancora fermo
            Vector3 toHero = Flat(Hero.transform.position - keeper.transform.position);
            agent.Warp(Spot(keeper.transform.position + toHero * 6.5f));
            yield return Frames(30);
            yield return Snap("keeper_near");

            // lo scontro: la prima fase, poi sotto la metà evoca e carica
            keeperAi.enabled = true;
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Snap("keeper_fight", world: false);
            // il cavaliere dove la sala è più libera (otto direzioni, la corsa più lunga sul NavMesh): la striscia si vede per intero
            Vector3 best = toHero;
            float longest = 0f;
            for (int k = 0; k < 8; k++)
            {
                Vector3 way = Quaternion.Euler(0f, 45f * k, 0f) * Vector3.forward;
                float free = NavMesh.Raycast(keeper.transform.position, keeper.transform.position + way * 12f, out var edge, NavMesh.AllAreas) ? edge.distance : 12f;
                if (free > longest)
                {
                    longest = free;
                    best = way;
                }
            }

            agent.Warp(Spot(keeper.transform.position + best * Mathf.Min(5f, longest * 0.4f)));
            var health = keeper.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(health.Max * 0.55f, DamageType.Physical, Hero));
            for (float t = 0f; !keeper.HasSummoned && t < 10f; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1f);
            yield return Snap("keeper_summons", world: false);
            for (float t = 0f; keeper.Stage != GuardianController.ChargeStage.WindUp && t < 15f; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(GuardianController.ChargeStage.WindUp, keeper.Stage, "la carica comincia");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Snap("keeper_charge", world: false);
        }

        [UnityTest, Timeout(600000), Description("Uno scheletro che scappa (24.6 della M24): ferito sotto il 25% durante lo scontro, scatta all'indietro; due foto a mezzo secondo e a un secondo e mezzo")]
        public IEnumerator Flee()
        {
            yield return StartNewFromMenu();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 1);
            EnemyBehaviours.FleeEnabled = true;
            EnemyBehaviours.PatrolEnabled = false;

            var hero = Hero.GetComponent<Health>();
            hero.MultiplyMax(500f);
            hero.Heal(float.MaxValue);
            var skeleton = Manager.CurrentLevel.Enemies
                .Where(e => e != null && e.gameObject.activeInHierarchy && e.Archetype.name == "Skeleton")
                .OrderBy(e => Vector3.Distance(e.transform.position, Hero.transform.position))
                .First();

            // davanti al cavaliere, a pochi metri, e vivo
            Vector3 toward = Flat(Hero.transform.forward);
            Vector3 wanted = Hero.transform.position + toward * 3.2f;
            skeleton.GetComponent<NavMeshAgent>().Warp(NavMesh.SamplePosition(wanted, out var spot, 3f, NavMesh.AllAreas) ? spot.position : wanted);
            skeleton.enabled = true;
            yield return new WaitForSecondsRealtime(1.2f);
            var health = skeleton.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(health.Max * 0.8f, DamageType.Physical, Hero));
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Snap("skeleton_flee_1", world: false);
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Snap("skeleton_flee_2", world: false);
        }

        // la prima cassa con il segreto cercato, carica livelli di semi diversi alla profondità data (25.3 della M25)
        private IEnumerator FindChestWith(ChestSecretKind kind, int depth, Action<Chest> found)
        {
            for (ulong seed = 1; seed <= 80; seed++)
            {
                Manager.RunSeed = seed;
                Root.UseLootSeed(seed);
                yield return Descend("Level_Crypt", depth);
                var chest = Manager.CurrentLevel.Chests.FirstOrDefault(c => c != null && c.Secret == kind);
                if (chest != null)
                {
                    found(chest);
                    yield break;
                }
            }

            Assert.Fail($"nessun seme alla profondità {depth} ha una cassa {kind}");
        }

        // il cavaliere a qualche passo davanti alla cassa, girato verso di lei
        private IEnumerator StandBefore(Chest chest, float distance)
        {
            Vector3 wanted = chest.transform.position + chest.transform.forward * distance;
            var agent = Hero.GetComponent<NavMeshAgent>();
            agent.Warp(NavMesh.SamplePosition(wanted, out var hit, 3f, NavMesh.AllAreas) ? hit.position : wanted);
            Vector3 toward = Flat(chest.transform.position - Hero.transform.position);
            if (toward.sqrMagnitude > 0.001f)
            {
                Hero.transform.rotation = Quaternion.LookRotation(toward);
            }

            yield return Settle();
        }

        [UnityTest, Timeout(600000), Description("I segreti delle casse (25.3 della M25): un mimic identico a un forziere, poi in piedi e in salto; il baule maledetto con le catene, poi la sua ondata")]
        public IEnumerator ChestSecrets()
        {
            yield return StartNewFromMenu();
            Root.PrepareHero(HeroLevel);
            ChestSecret.Enabled = true;
            try
            {
                var hero = Hero.GetComponent<Health>();
                hero.MultiplyMax(500f);
                hero.Heal(float.MaxValue);

                Chest mimic = null;
                yield return FindChestWith(ChestSecretKind.Mimic, 3, c => mimic = c);
                yield return StandBefore(mimic, 3.4f);
                yield return Snap("mimic_closed");
                mimic.GetComponent<Interactable>().Use(Hero);
                yield return new WaitForSecondsRealtime(0.7f);
                yield return Snap("mimic_awake");
                yield return new WaitForSecondsRealtime(0.45f);
                yield return Snap("mimic_chasing");

                Chest cursed = null;
                yield return FindChestWith(ChestSecretKind.Cursed, 3, c => cursed = c);
                hero = Hero.GetComponent<Health>();
                hero.MultiplyMax(500f);
                hero.Heal(float.MaxValue);
                yield return StandBefore(cursed, 3.4f);
                yield return Snap("cursed_closed");
                cursed.GetComponent<Interactable>().Use(Hero);
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Snap("cursed_wave");
            }
            finally
            {
                ChestSecret.Enabled = false;
            }
        }

        [UnityTest, Timeout(600000), Description("L'altare con «Il mondo cresce con te» (24.6 della M24): spento, acceso con il suggerimento, e in italiano")]
        public IEnumerator GrowingAltar()
        {
            yield return StartNewFromMenu();
            Root.PrepareHero(HeroLevel);
            yield return Settle();

            Manager.CurrentLevel.Altar.GetComponent<Interactable>().Use(Hero);
            var altar = Find<DifficultyPanel>();
            for (float t = 0f; !altar.IsOpen && t < 10f; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            yield return Frames(5);
            yield return Snap("altar_growing_off", world: false);

            altar.GrowingWorldButton.onClick.Invoke();
            altar.GrowingWorldButton.GetComponent<HoverShow>().Target.SetActive(true);
            yield return Frames(5);
            yield return Snap("altar_growing_on_hint", world: false);

            Root.Localizer.SetLanguage("it");
            altar.GrowingWorldButton.GetComponent<HoverShow>().Target.SetActive(true);
            yield return Frames(5);
            yield return Snap("altar_growing_on_hint_it", world: false);
            Root.Localizer.SetLanguage("en");
        }

        [UnityTest, Timeout(600000), Description("Quanto costano le ronde e la fuga (24.6 della M24): fino a trenta nemici tra i più vicini al cavaliere (in una cripta alla 3 ce ne sono 12-14 attivi), tutti attivi con la fuga e le ronde accese, contro tutti fermi; il tempo del main thread, le allocazioni e i batch vanno in costo_ronde.txt")]
        public IEnumerator BehavioursCost()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            MenuSteps.StartNew(Find<MainMenu>(), 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            // le ronde si legano al caricamento del livello: accese prima di scendere
            EnemyBehaviours.FleeEnabled = true;
            EnemyBehaviours.PatrolEnabled = true;
            yield return Descend("Level_Crypt", 3);

            var hero = Hero.GetComponent<Health>();
            hero.MultiplyMax(500f);
            hero.Heal(float.MaxValue);
            var crowd = Manager.CurrentLevel.Enemies.Where(e => e != null && e.gameObject.activeInHierarchy)
                .OrderBy(e => Vector3.Distance(e.transform.position, Hero.transform.position)).Take(30).ToArray();
            yield return Frames(3);

            int previousRate = Application.targetFrameRate;
            int previousSync = QualitySettings.vSyncCount;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            var mainThread = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "Main Thread");
            var gcAlloc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame");
            var batches = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Batches Count");
            var lines = new List<string> { $"Cripta alla 3, {crowd.Length} nemici più vicini al cavaliere, 600 frame per giro, vsync spento (editor)." };
            const int frames = 600;
            foreach (bool on in new[] { false, true, false, true })
            {
                foreach (var enemy in crowd)
                {
                    if (enemy != null)
                    {
                        enemy.enabled = on;
                    }
                }

                yield return Frames(30);
                var times = new List<float>(frames);
                long gc = 0;
                long batchCount = 0;
                for (int k = 0; k < frames; k++)
                {
                    yield return null;
                    times.Add(mainThread.Valid ? mainThread.LastValue / 1e6f : 0f);
                    gc += gcAlloc.Valid ? gcAlloc.LastValue : 0;
                    batchCount += batches.Valid ? batches.LastValue : 0;
                }

                times.Sort();
                lines.Add($"nemici {(on ? "attivi " : "fermi   ")}: main thread {times.Average():F2} ms in media, mediana {times[frames / 2]:F2}, "
                    + $"p95 {times[(int)(frames * 0.95f)]:F2}, peggiore {times[frames - 1]:F2}; allocazioni {gc / (float)frames:F0} B a frame; batch {batchCount / (float)frames:F1} a frame");
            }

            mainThread.Dispose();
            gcAlloc.Dispose();
            batches.Dispose();
            Application.targetFrameRate = previousRate;
            QualitySettings.vSyncCount = previousSync;
            File.WriteAllLines(Path.Combine(_folder, "costo_ronde.txt"), lines);
            foreach (string line in lines)
            {
                Debug.Log("BehavioursCost: " + line);
            }
        }

        [UnityTest, Timeout(600000), Description("I suggerimenti (23.4, D11 e D12 della M23): il riquadro in basso al centro nelle due lingue, con il testo corto e quello più lungo, e le opzioni con la casella e Rivedi")]
        public IEnumerator Hints()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 1);

            var director = Find<HintDirector>();

            IEnumerator Show(string id, string shot)
            {
                // tutti gli altri già visti: esce solo questo
                director.SetOn(false);
                director.Book.Load(string.Join(",", UI.Hints.All.Where(other => other != id)));
                director.SetOn(true);
                director.Book.Raise(id);
                for (float t = 0f; director.Current != id; t += Time.unscaledDeltaTime)
                {
                    Assert.Less(t, 4f, "il suggerimento " + id + " non compare");
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.4f);
                yield return Snap(shot);
            }

            yield return Show(UI.Hints.Move, "hint_move");
            yield return Show(UI.Hints.Item, "hint_item");
            yield return Show(UI.Hints.Potion, "hint_potion");
            yield return Show(UI.Hints.Skills, "hint_skills");
            yield return Show(UI.Hints.Village, "hint_village");
            Root.Localizer.SetLanguage("it");
            yield return Show(UI.Hints.Skills, "hint_skills_it");
            yield return Show(UI.Hints.Item, "hint_item_it");
            director.SetOn(false);

            // le opzioni, con la casella dei suggerimenti e Rivedi sotto le barre
            var options = Find<OptionsPanel>();
            options.Open();
            yield return Frames(3);
            yield return Snap("hint_options_it", world: false);
            Root.Localizer.SetLanguage("en");
            yield return Frames(3);
            yield return Snap("hint_options", world: false);
            options.Back.onClick.Invoke();
        }

        [UnityTest, Timeout(600000), Description("Il pannello dei comandi (23.5, D14 della M23): il menu di pausa con il pulsante Comandi, il pannello in inglese e in italiano, e quello del Mago")]
        public IEnumerator Help()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Descend("Level_Crypt", 1);

            var pause = Find<PauseMenu>();
            var help = Find<HelpPanel>();
            pause.Open();
            yield return Frames(3);
            yield return Snap("help_pause", world: false);

            pause.HelpButton.onClick.Invoke();
            yield return Frames(3);
            yield return Snap("help_panel", world: false);
            Root.Localizer.SetLanguage("it");
            yield return Frames(3);
            yield return Snap("help_panel_it", world: false);
            Root.Localizer.SetLanguage("en");

            help.Close();
            var playerClass = Hero.GetComponent<PlayerClass>();
            Root.ApplyClass(playerClass.Catalog.Find("mage"), "Prova");
            yield return Frames(3);
            pause.HelpButton.onClick.Invoke();
            yield return Frames(3);
            yield return Snap("help_panel_mage", world: false);
            help.Close();
            pause.Close();
        }

        [UnityTest, Timeout(300000), Description("Fabbro, guaritrice e mercante del borgo, ognuno con il suo banco aperto (D18 della M18)")]
        public IEnumerator VillageShops()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
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
            MenuSteps.StartNew(menu, 1);
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

        [UnityTest, Timeout(600000), Description("La scala in su (22.3): lo sbocco nella cripta e nelle caverne, a riposo e sotto il cursore, l'arrivo davanti alla scala giù risalendo, e l'uscita dal cunicolo nel borgo")]
        public IEnumerator StairsUp()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(HeroLevel);
            yield return Settle();

            // davanti allo sbocco, a qualche metro, fuori dal suo trigger
            IEnumerator Face(string name)
            {
                var up = Manager.CurrentLevel.UpExit;
                Assert.IsNotNull(up, name);
                var agent = Hero.GetComponent<NavMeshAgent>();
                bool placed = false;
                foreach (float distance in new[] { 6f, 5f, 7f, 8f })
                {
                    if (NavMesh.SamplePosition(up.transform.position + up.transform.forward * distance, out var hit, 1.2f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                        placed = true;
                        break;
                    }
                }

                Assert.IsTrue(placed, $"{name}: nessun punto davanti allo sbocco");
                yield return Settle();
                yield return Snap(name);
                var interactable = up.GetComponent<Interactable>();
                interactable.SetHighlighted(true);
                yield return Frames(3);
                yield return Snap(name + "_hover");
                interactable.SetHighlighted(false);

                // l'automappa (D10): la scala in su, azzurra, dove si è passati
                var automap = Find<AutomapView>();
                for (int i = 0; i < 3 && automap.Mode != AutomapMode.Overlay; i++)
                {
                    automap.Cycle();
                }

                yield return Frames(3);
                yield return Snap(name + "_map");
                while (automap.Mode != AutomapMode.Corner)
                {
                    automap.Cycle();
                }
            }

            yield return Descend("Level_Crypt", 1);
            yield return Face("up_crypt_1");
            yield return Descend("Level_Crypt", 3);
            yield return Face("up_crypt_3");
            yield return Descend("Level_Caves", 5);
            yield return Face("up_caves_5");
            yield return Descend("Level_Caves", 7);
            yield return Face("up_caves_7");

            // si risale: davanti alla scala giù del livello di sopra, e poi nel borgo
            var manager = Manager;
            manager.LoadLevel("Level_Crypt", DungeonSettings.EntranceFromBelow, 4);
            for (float t = 0f; manager.IsTransitioning || manager.CurrentLevel == null || manager.CurrentLevel.Depth != 4; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 20f, "la profondità 4 non arriva");
                yield return null;
            }

            foreach (var enemy in manager.CurrentLevel.Enemies.Where(e => e != null))
            {
                enemy.enabled = false;
            }

            yield return Settle();
            yield return Snap("below_arrival_4");

            manager.LoadLevel("Town", "FromCrypt", 0);
            for (float t = 0f; manager.IsTransitioning || manager.CurrentLevel == null || manager.CurrentLevel.Depth != 0; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 20f, "il borgo non arriva");
                yield return null;
            }

            yield return Settle();
            yield return Snap("village_from_crypt");
        }

        [UnityTest, Timeout(300000), Description("Le stanze della cripta di prima e di dopo (22.1, D11 della M22): quattro semi con le impostazioni di oggi e le alternative, disegnati dal generatore vero, e i numeri su 64 semi")]
        public IEnumerator RoomDesign()
        {
            // oggi, poi le alternative misurate nella scheda: nome, griglia, zone (min e max), stanze (min e max)
            var configs = new[]
            {
                ("oggi", 28, 6, 11, 3, 7), ("A", 28, 6, 11, 4, 7), ("B", 32, 7, 12, 4, 8), ("C", 34, 7, 13, 4, 9), ("D", 36, 8, 13, 5, 9),
            };
            const int depth = 3;
            const int cell = 10;
            const int slot = 36 * cell + 12;
            var seeds = new ulong[] { 20261104, 20261105, 20261106, 20261107 };
            var settings = new DungeonSettings[configs.Length];
            for (int i = 0; i < configs.Length; i++)
            {
                settings[i] = Object.Instantiate(AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset"));
                var so = new SerializedObject(settings[i]);
                var (_, grid, minLeaf, maxLeaf, minRoom, maxRoom) = configs[i];
                so.FindProperty("_width").intValue = grid;
                so.FindProperty("_height").intValue = grid;
                so.FindProperty("_minLeaf").intValue = minLeaf;
                so.FindProperty("_maxLeaf").intValue = maxLeaf;
                so.FindProperty("_minRoom").intValue = minRoom;
                so.FindProperty("_maxRoom").intValue = maxRoom;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // una riga per seme, una colonna per impostazione; le stanze con un lato di 3 celle sono rosate
            var sheet = new Texture2D(slot * configs.Length, slot * seeds.Length, TextureFormat.RGBA32, false);
            var background = new Color32(40, 40, 46, 255);
            var fill = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < fill.Length; i++)
            {
                fill[i] = background;
            }

            sheet.SetPixels32(fill);
            for (int row = 0; row < seeds.Length; row++)
            {
                for (int column = 0; column < configs.Length; column++)
                {
                    var layout = settings[column].CreateGenerator().Generate(SeedMixer.ForLevel(seeds[row], depth), depth);
                    var narrow = new bool[layout.Width, layout.Height];
                    foreach (var room in layout.Rooms)
                    {
                        if (Mathf.Min(room.width, room.height) < 4)
                        {
                            for (int y = room.yMin; y < room.yMax; y++)
                            {
                                for (int x = room.xMin; x < room.xMax; x++)
                                {
                                    narrow[x, y] = true;
                                }
                            }
                        }
                    }

                    int originX = column * slot + 6;
                    int originY = (seeds.Length - 1 - row) * slot + 6;
                    for (int y = 0; y < layout.Height; y++)
                    {
                        for (int x = 0; x < layout.Width; x++)
                        {
                            Color32 color = MapPainter.ColorOf(layout[x, y]);
                            if (narrow[x, y])
                            {
                                color = new Color32(190, 110, 110, 255);
                            }

                            for (int py = 0; py < cell - 1; py++)
                            {
                                for (int px = 0; px < cell - 1; px++)
                                {
                                    sheet.SetPixel(originX + x * cell + px, originY + (layout.Height - 1 - y) * cell + py, color);
                                }
                            }
                        }
                    }
                }
            }

            sheet.Apply();
            File.WriteAllBytes(Path.Combine(_folder, "rooms_compare.png"), sheet.EncodeToPNG());
            Object.Destroy(sheet);

            var lines = new List<string> { "impostazione | stanze/livello | area mediana (celle) | stanze <=12 celle | stanze con un lato di 3 | pavimento %" };
            for (int column = 0; column < configs.Length; column++)
            {
                var areas = new List<int>();
                var counts = new List<int>();
                int small = 0, narrowCount = 0;
                double floor = 0;
                for (ulong n = 1; n <= 64; n++)
                {
                    var layout = settings[column].CreateGenerator().Generate(SeedMixer.ForLevel(20261000UL + n, depth), depth);
                    counts.Add(layout.Rooms.Count);
                    int floorCells = 0;
                    for (int y = 0; y < layout.Height; y++)
                    {
                        for (int x = 0; x < layout.Width; x++)
                        {
                            floorCells += layout.IsFloor(x, y) ? 1 : 0;
                        }
                    }

                    floor += 100.0 * floorCells / (layout.Width * layout.Height);
                    foreach (var room in layout.Rooms)
                    {
                        areas.Add(room.width * room.height);
                        small += room.width * room.height <= 12 ? 1 : 0;
                        narrowCount += Mathf.Min(room.width, room.height) < 4 ? 1 : 0;
                    }
                }

                areas.Sort();
                lines.Add($"{configs[column].Item1} | {counts.Average():F1} | {areas[areas.Count / 2]} | {100.0 * small / areas.Count:F0}% | {100.0 * narrowCount / areas.Count:F0}% | {floor / 64:F1}");
                yield return null;
            }

            File.WriteAllLines(Path.Combine(_folder, "rooms_stats.txt"), lines);
            foreach (var s in settings)
            {
                Object.Destroy(s);
            }

            _taken.Add("rooms_compare.png");
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
            MenuSteps.StartNew(menu, 1);
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
            MenuSteps.StartNew(menu, 1);
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
            MenuSteps.StartNew(menu, 1);
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
            MenuSteps.StartNew(menu, 1);
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

        [UnityTest, Timeout(600000), Description("Il pannello delle abilità (D10 e D11 della M20): vuoto al livello 1, l'avviso nell'HUD, la scelta della specializzazione al 12, gli alberi a metà al 30 con il tooltip e la scelta, e l'altare, a 16:9 e a 4:3")]
        public IEnumerator Talents()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(1);
            yield return Settle();

            var build = Hero.GetComponent<PlayerTalents>().Build;
            var progress = Hero.GetComponent<PlayerProgress>().Progress;
            var panel = Find<TalentPanel>();
            var resolutions = new[] { (1920u, 1080u, "16x9"), (1440u, 1080u, "4x3") };

            void Take(TalentTree tree, string id, int ranks)
            {
                for (int i = 0; i < ranks; i++)
                {
                    Assert.IsTrue(build.TryAdd(tree, tree.Find(id)), $"{id} {i + 1}");
                }
            }

            // 1. livello 1: tutto vuoto, la radice presa e la specializzazione chiusa
            panel.Open();
            foreach (var (width, height, tag) in resolutions)
            {
                PlayModeWindow.SetCustomRenderingResolution(width, height, "Photos");
                yield return Frames(5);
                yield return Snap("talents_start_" + tag, world: false);
            }

            // 2. livello 5 con il pannello chiuso: l'avviso accanto alla sfera del mana
            panel.Close();
            progress.Restore(5, 0, 0);
            foreach (var (width, height, tag) in resolutions)
            {
                PlayModeWindow.SetCustomRenderingResolution(width, height, "Photos");
                yield return Frames(5);
                yield return Snap("talents_notice_" + tag);
            }

            // 3. livello 12: le due schede e il pulsante per sceglierla
            progress.Restore(12, 0, 0);
            panel.Open();
            panel.SpecTab(1).onClick.Invoke();
            foreach (var (width, height, tag) in resolutions)
            {
                PlayModeWindow.SetCustomRenderingResolution(width, height, "Photos");
                yield return Frames(5);
                yield return Snap("talents_choose_" + tag, world: false);
            }

            // 4. livello 30: la classe applicata, Armi scelta e a metà, il tooltip su un nodo e la scelta aperta
            panel.Close();
            progress.Restore(30, 0, 0);
            var warrior = build.ClassTree;
            Take(warrior, "strong_arm", 3);
            Take(warrior, "charge", 1);
            Take(warrior, "toughness", 3);
            Take(warrior, "keen_eye", 3);
            Take(warrior, "steady_hand", 2);
            Take(warrior, "discipline", 2);
            Take(warrior, "sturdy_mail", 3);
            Take(warrior, "stone_skin", 1);
            Assert.IsTrue(build.TryChoose(build.Specializations[1]));
            build.Apply();
            var arms = build.Specialization;
            Take(arms, "two_handed_grip", 3);
            Take(arms, "keen_edge", 2);
            Take(arms, "weak_spot", 3);
            Take(arms, "circular_motion", 2);
            panel.Open();
            foreach (var (width, height, tag) in resolutions)
            {
                PlayModeWindow.SetCustomRenderingResolution(width, height, "Photos");
                yield return Frames(5);
                yield return Snap("talents_tree_" + tag, world: false);
                panel.View(arms, "keen_edge").Hover();
                yield return Frames(3);
                yield return Snap("talents_tooltip_" + tag, world: false);
                panel.View(arms, "keen_edge").Unhover();
                panel.View(warrior, "momentum").Hover();
                yield return Frames(3);
                yield return Snap("talents_locked_" + tag, world: false);
                panel.View(warrior, "momentum").Unhover();
            }

            // la scelta: un nodo a scelta aperto, con il suo tooltip
            progress.Restore(50, 0, 0);
            yield return Frames(3);
            foreach (var (width, height, tag) in resolutions)
            {
                PlayModeWindow.SetCustomRenderingResolution(width, height, "Photos");
                yield return Frames(5);
                panel.View(warrior, "battle_instinct").Hover();
                panel.View(warrior, "battle_instinct").Click();
                yield return Frames(3);
                yield return Snap("talents_choice_" + tag, world: false);
                panel.View(warrior, "battle_instinct").Click();
                panel.View(warrior, "battle_instinct").Unhover();
            }

            // 5. l'altare, con la voce nuova
            build.Apply();
            panel.Close();
            Hero.GetComponent<PlayerInventory>().Inventory.AddGold(5000);
            var altar = Find<DifficultyPanel>();
            altar.Open();
            foreach (var (width, height, tag) in resolutions)
            {
                PlayModeWindow.SetCustomRenderingResolution(width, height, "Photos");
                yield return Frames(5);
                yield return Snap("talents_altar_" + tag, world: false);
            }

            altar.Close();
        }

        [UnityTest, Timeout(300000), Description("La creazione del personaggio (21.6): il Guerriero e il Mago con il loro testo e il nome, l'apertura con il Mago, e i posti con nome e classe")]
        public IEnumerator Creation()
        {
            ForgetPreferences();
            PlayerPrefs.SetString(CompositionRoot.LanguagePreference, "it");
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            Environment.SetEnvironmentVariable(CompositionRoot.FilmsVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            PlayModeWindow.SetCustomRenderingResolution(1920u, 1080u, "Photos");
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Snap("creation_1_warrior", world: false);
            menu.Creation.Card("mage").onClick.Invoke();
            yield return new WaitForSecondsRealtime(3f);
            yield return Snap("creation_2_mage", world: false);
            menu.Creation.NameField.text = "Merlino";
            yield return Frames(3);
            yield return Snap("creation_3_name", world: false);

            menu.Creation.StartButton.onClick.Invoke();
            yield return WaitForLevel();
            var films = Root.Films;
            for (float t = 0f; !films.IsPlaying; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 10f, "l'apertura non parte");
                yield return null;
            }

            var director = Root.CurrentFilm.Director;
            Time.timeScale = 0f;
            foreach (var (name, time) in OpeningShots.Where(s => s.Name == "opening_1_out" || s.Name == "opening_4_arrival" || s.Name == "opening_4_altar"))
            {
                director.time = time;
                director.Evaluate();
                yield return new WaitForSecondsRealtime(1.6f);
                yield return Snap("creation_4_" + name, world: false);
            }

            Time.timeScale = 1f;
            films.Press();
            films.Press();
            yield return Frames(3);

            // il Mago è nel posto 1; Carica li mostra con nome e classe
            for (float t = 0f; !File.Exists(Path.Combine(_saves, "save_1.json")); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 10f, "il posto 1 non si scrive");
                yield return null;
            }

            yield return Frames(5);
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            menu = Find<MainMenu>();
            menu.LoadButton.onClick.Invoke();
            yield return Frames(3);
            yield return Snap("creation_5_slots", world: false);
            ForgetPreferences();
        }

        [UnityTest, Timeout(600000), Description("L'albero del Mago (21.5): la scelta tra Fuoco e Gelo al 12, la classe e il Fuoco al 50 con un nodo a scelta aperto e un tooltip, e il Gelo")]
        public IEnumerator MageTalents()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return Frames(10);
            var menu = Find<MainMenu>();
            MenuSteps.StartNew(menu, 1);
            yield return WaitForLevel();
            Root.PrepareHero(1);
            yield return Settle();

            var playerClass = Hero.GetComponent<PlayerClass>();
            playerClass.Apply(playerClass.Catalog.Find("mage"), "Merlino", true);
            var build = Hero.GetComponent<PlayerTalents>().Build;
            var progress = Hero.GetComponent<PlayerProgress>().Progress;
            var panel = Find<TalentPanel>();
            PlayModeWindow.SetCustomRenderingResolution(1920u, 1080u, "Photos");

            void Take(TalentTree tree, string id, int ranks, int option = 0)
            {
                for (int i = 0; i < ranks; i++)
                {
                    Assert.IsTrue(build.TryAdd(tree, tree.Find(id), option), $"{id} {i + 1}");
                }
            }

            // 1. livello 12: le due schede, Fuoco e Gelo
            progress.Restore(12, 0, 0);
            panel.Open();
            panel.SpecTab(1).onClick.Invoke();
            yield return Frames(5);
            yield return Snap("mage_choose", world: false);
            panel.Close();

            // 2. livello 50: la classe e il Fuoco a buon punto
            progress.Restore(50, 0, 0);
            var mage = build.ClassTree;
            foreach (var (id, ranks) in new[]
            {
                ("keen_mind", 3), ("fire_ring", 1), ("mana_reserve", 3), ("swift_bolt", 2), ("wide_flame", 2), ("blink", 1), ("ash_skin", 3),
                ("concentration", 2), ("mana_shield", 1), ("cold_mind", 2), ("fire_craft", 1), ("lasting_shield", 2), ("last_spark", 1),
            })
            {
                Take(mage, id, ranks);
            }

            Assert.IsTrue(build.TryChoose(build.Specializations[0]));
            var fire = build.Specialization;
            foreach (var (id, ranks) in new[]
            {
                ("flame_wall", 1), ("inflamed_mind", 3), ("great_blaze", 3), ("explosion", 2), ("quick_hand", 2), ("lasting_flames", 2),
                ("live_embers", 2), ("burning_magic", 2),
            })
            {
                Take(fire, id, ranks);
            }

            Take(fire, "fire_end", 1, option: 1);
            Take(fire, "meteor", 1);
            build.Apply();
            panel.Open();
            yield return Frames(5);
            yield return Snap("mage_tree", world: false);
            panel.View(mage, "concentration").Hover();
            yield return Frames(3);
            yield return Snap("mage_tooltip", world: false);
            panel.View(mage, "concentration").Unhover();

            // il nodo a scelta aperto, con le due opzioni
            panel.View(mage, "fire_craft").Hover();
            panel.View(mage, "fire_craft").Click();
            yield return Frames(3);
            yield return Snap("mage_choice", world: false);
            panel.View(mage, "fire_craft").Click();
            panel.View(mage, "fire_craft").Unhover();

            // 3. il Gelo al posto del Fuoco
            build.Cancel();
            build.Reset();
            foreach (var (id, ranks) in new[] { ("keen_mind", 3), ("fire_ring", 1), ("mana_reserve", 3), ("blink", 1), ("ash_skin", 3), ("cold_mind", 2), ("mana_shield", 1) })
            {
                Take(mage, id, ranks);
            }

            Assert.IsTrue(build.TryChoose(build.Specializations[1]));
            var frost = build.Specialization;
            foreach (var (id, ranks) in new[]
            {
                ("frost_nova", 1), ("frozen_skin", 3), ("piercing_cold", 3), ("quick_frost", 2), ("deep_frost", 2), ("rime", 2), ("ice_shelter", 2),
                ("cold_blood", 3),
            })
            {
                Take(frost, id, ranks);
            }

            Take(frost, "frost_end", 1, option: 0);
            build.Apply();
            yield return Frames(5);
            yield return Snap("mage_frost", world: false);
            panel.Close();
        }
    }
}
