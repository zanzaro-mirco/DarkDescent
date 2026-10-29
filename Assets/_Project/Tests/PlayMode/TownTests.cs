using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Rendering;
using DarkDescent.Save;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>La città: si comincia lì e la scala scende alla cripta (D3 della M10).</summary>
    public class TownTests : SandboxFixture
    {
        private const string TownScene = "Town";

        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentTown_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static MainMenu Menu => UnityEngine.Object.FindFirstObjectByType<MainMenu>();

        // dal menu, una partita nuova nel posto 1: come nella build
        private static IEnumerator StartNewGame()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Menu.NewGameButton.onClick.Invoke();
            Menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
        }

        [UnityTest, Description("Una partita nuova comincia in città: profondità 0, senza nemici né casse, scena attiva per luci e Instantiate, il cavaliere sul NavMesh, il nome senza numero, l'automappa, luce calda, la sua musica e il suo fondo")]
        public IEnumerator NewGame_StartsInTown()
        {
            yield return StartNewGame();
            var level = Manager.CurrentLevel;
            Assert.AreEqual(TownScene, level.gameObject.scene.name);
            Assert.AreEqual(TownScene, SceneManager.GetActiveScene().name);
            Assert.AreEqual(0, level.Depth);
            Assert.IsTrue(level.IsTown);
            Assert.AreEqual(0, level.Enemies.Count, "in città non si combatte");
            Assert.AreEqual(0, level.Chests.Count);
            Assert.IsTrue(GameObject.Find("Player").GetComponent<NavMeshAgent>().isOnNavMesh);

            Assert.AreEqual("The village", UnityEngine.Object.FindFirstObjectByType<LevelTitle>().Text, "il borgo non ha un numero (D11 della M18)");
            Assert.IsNotNull(UnityEngine.Object.FindFirstObjectByType<ExplorationTracker>().Exploration, "costruita dalla mappa: l'automappa c'è");

            Assert.AreEqual(1, level.Exits.Count);
            Assert.AreEqual(("Level_Crypt", "Start", 1), (level.Exits[0].TargetScene, level.Exits[0].TargetEntrance, level.Exits[0].TargetDepth));
            Assert.AreEqual("Descend to level 1", level.Exits[0].GetComponent<Interactable>().GetLabel(Localizer));

            // calda e più chiara della cripta; all'aperto fanno ombra il sole del borgo e la luce del cavaliere (M18)
            Assert.Greater(RenderSettings.ambientLight.r, RenderSettings.ambientLight.b);
            Assert.Greater(RenderSettings.ambientLight.r, 0.1f);
            var shadowLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.shadows != LightShadows.None).ToArray();
            Assert.AreEqual(2, shadowLights.Length);
            Assert.AreEqual(1, shadowLights.Count(l => l.type == LightType.Directional), "il sole");

            // il terreno del borgo: erba e sassi sparsi, e la texture del prato stesa nel mondo
            Assert.Greater(level.transform.Find(LevelBuilder.ScatterGroup).childCount, 100);
            var ground = level.GetComponentInChildren<DarkDescent.Rendering.WorldUv>();
            var mesh = ground.GetComponent<MeshFilter>().sharedMesh;
            Vector3 corner = ground.transform.TransformPoint(mesh.vertices[0]);
            Assert.AreEqual(new Vector2(corner.x, corner.z) / ground.MetersPerRepeat, mesh.uv[0], "le coordinate vengono dal mondo");

            // la sua musica, il fuoco con il vento e, ogni tanto, un corvo lontano (D10 della M18)
            var ambience = UnityEngine.Object.FindFirstObjectByType<DarkDescent.Audio.AmbiencePlayer>();
            Assert.AreEqual("town_loop_the_bards_tale", ambience.Music.clip.name);
            Assert.AreEqual("bed_village", ambience.Bed.clip.name);
            Assert.AreEqual(1, ambience.Current.StingerCount);
            Assert.AreEqual("stinger_village_crow", ambience.Current.GetStinger(0).name);
        }

        [UnityTest, Description("La grotta del borgo porta alla profondità 1 della cripta, sul suo ingresso iniziale, davanti allo sbocco del cunicolo nel muro (D6 e D7 della M18)")]
        public IEnumerator Cave_LeadsToTheCryptByTheTunnel()
        {
            yield return StartNewGame();
            var player = GameObject.Find("Player");
            var agent = player.GetComponent<NavMeshAgent>();
            var exit = Manager.CurrentLevel.Exits[0].transform;
            agent.Warp(exit.position + exit.forward * 2.5f);
            agent.SetDestination(exit.position);
            for (float t = 0f; Manager.CurrentSceneName != "Level_Crypt"; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 8f, "la scala non porta alla cripta");
                yield return null;
            }

            yield return WaitForLevel();
            Assert.AreEqual(1, Manager.CurrentLevel.Depth);
            Assert.AreEqual("Start", Manager.CurrentEntrance);
            Assert.AreEqual(5, Manager.CurrentLevel.Enemies.Count, "la stessa cripta di sempre");
            Assert.IsNull(Manager.CurrentLevel.Daylight, "sotto terra il giorno non passa");
            Assert.IsFalse(RenderSettings.fog, "la nebbia del borgo resta nel borgo");

            // lo sbocco del cunicolo sull'ingresso, e il cavaliere che ne esce e cammina nella stanza
            var mouth = Manager.CurrentLevel.transform.Find("Props/" + LevelBuilder.EntranceMouthName);
            Assert.IsNotNull(mouth, "alla profondità 1 si arriva dal cunicolo");
            var entrance = Manager.CurrentLevel.GetComponentsInChildren<LevelEntrance>().Single(e => e.Id == "Start");
            Assert.IsTrue(TunnelSpot.TryRead(Manager.CurrentLevel.Map.GetDirective(LevelBuilder.ArrivalDirective), out var cell, out var wall));
            Vector3 center = LevelMap.CellCenter(cell.x, cell.y);
            Assert.Less(Vector3.Distance(center, mouth.position), 0.01f, "al centro della sua cella");
            Assert.Greater(Vector3.Dot(mouth.forward, -LevelMap.ToWorld(wall)), 0.99f, "l'apertura verso la stanza");
            Assert.Greater(Vector3.Distance(entrance.transform.position, mouth.position), LevelMap.CellSize - 0.01f, "il cavaliere compare fuori dalla sua cella");

            // al posto del muro: su quel bordo né il muro alto né quello basso
            Vector3 edge = center + LevelMap.ToWorld(wall) * (LevelMap.CellSize * 0.5f);
            var walls = Manager.CurrentLevel.transform.Find("Walls").GetComponentsInChildren<Transform>(true)
                .Where(w => w.parent != null && (w.parent.name == "High" || w.parent.name == "Low")).ToArray();
            Assert.That(walls.Length, Is.GreaterThan(0));
            Assert.IsFalse(walls.Any(w => Vector3.Distance(w.position, edge) < 0.1f), "lo sbocco prende il posto del muro");
            Assert.IsTrue(agent.isOnNavMesh);
            var path = new NavMeshPath();
            Vector3 stairs = Manager.CurrentLevel.Exits[0].GetComponent<Interactable>().ApproachPoint;
            Assert.IsTrue(agent.CalculatePath(stairs, path));
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status,
                $"dal cunicolo si arriva alla scala: da {player.transform.position} verso {stairs}, fermo a {path.corners.LastOrDefault()}");
        }

        [UnityTest, Description("Attorno al fuoco del borgo tre abitanti seduti e uno in piedi che saluta chi arriva; sono ostacoli, e dall'ingresso si arriva ancora ai tre banchi e alla grotta (D9 e D18 della M18)")]
        public IEnumerator Villagers_SitByTheFireAndGreet()
        {
            yield return StartNewGame();
            // fabbro e guaritrice tengono la loro posa allo stesso modo, ma stanno ai loro banchi
            var everyone = Manager.CurrentLevel.GetComponentsInChildren<DarkDescent.Town.Villager>();
            var villagers = everyone.Where(v => v.GetComponentInParent<DarkDescent.Town.Merchant>() == null).ToArray();
            Assert.AreEqual(4, villagers.Length);
            CollectionAssert.AreEquivalent(new[] { "Hammering", "Idle_B" }, everyone.Except(villagers).Select(v => v.Pose.name).ToArray(), "il fabbro batte, la guaritrice aspetta");
            var greeter = villagers.Single(v => v.Greets);
            Assert.IsTrue(villagers.Where(v => !v.Greets).All(v => v.Pose.name == "Sit_Floor_Idle"), "gli altri seduti per terra");
            Assert.IsTrue(villagers.All(v => v.GetComponent<NavMeshObstacle>().carving), "ostacoli per il NavMesh");

            // il cavaliere va lontano dal fuoco, a ovest, e torna all'ingresso: chi sta in piedi lo saluta
            var agent = GameObject.Find("Player").GetComponent<NavMeshAgent>();
            Vector3 start = agent.transform.position;
            Assert.IsTrue(NavMesh.SamplePosition(start + Vector3.left * 14f, out var away, 3f, NavMesh.AllAreas));
            Assert.Greater(Vector3.Distance(away.position, greeter.transform.position), 12f);
            agent.Warp(away.position);
            for (float t = 0f; greeter.IsGreeting || t < 0.3f; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 6f, "il saluto dell'arrivo non finisce");
                yield return null;
            }

            agent.Warp(start);
            for (float t = 0f; !greeter.IsGreeting; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 2f, $"nessuno saluta chi arriva, a {Vector3.Distance(greeter.transform.position, agent.transform.position):0.0} m");
                yield return null;
            }

            // i ritagli degli ostacoli arrivano dopo qualche frame
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            // i tre banchi (D18) e la grotta
            Assert.AreEqual(3, Manager.CurrentLevel.Merchants.Count, "mercante, fabbro e guaritrice");
            var targets = Manager.CurrentLevel.Merchants.Select(m => m.GetComponent<Interactable>().ApproachPoint)
                .Append(Manager.CurrentLevel.Exits[0].GetComponent<Interactable>().ApproachPoint);
            foreach (var target in targets)
            {
                var path = new NavMeshPath();
                Assert.IsTrue(agent.CalculatePath(target, path));
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, $"verso {target}");
            }
        }

        [UnityTest, Description("Il borgo cambia luce con il tempo di gioco: di notte sole basso, ambiente buio ma sopra il minimo, lanterne e finestre accese (D5 della M18)")]
        public IEnumerator Village_ChangesLightWithPlayTime()
        {
            yield return StartNewGame();
            var view = Manager.CurrentLevel.Daylight;
            Assert.IsNotNull(view, "il borgo ha il giorno che passa");
            Assert.AreEqual(DayMoment.Day, view.Phase.From, "la partita nuova comincia di giorno");
            var lights = Manager.CurrentLevel.GetComponentsInChildren<NightLight>(true);
            Assert.That(lights.Length, Is.GreaterThan(5));
            Assert.IsTrue(lights.All(l => l.Lit == 0f), "di giorno spente");
            float daySun = view.Sun.intensity;
            float dayAmbient = RenderSettings.ambientLight.grayscale;

            var stats = UnityEngine.Object.FindFirstObjectByType<CompositionRoot>().RunStats;
            stats.Restore(DayCycle.Start(DayMoment.Night) + 10f, stats.Kills, stats.Deaths, stats.GoldCollected, stats.BossDefeated);
            yield return null;
            yield return null;
            Assert.AreEqual(DayMoment.Night, view.Phase.From);
            Assert.Less(view.Sun.intensity, daySun);
            Assert.Less(RenderSettings.ambientLight.grayscale, dayAmbient);
            Assert.GreaterOrEqual(RenderSettings.ambientLight.b, view.Lighting.MinimumAmbient.b, "il minimo della notte");
            Assert.IsTrue(lights.All(l => l.Lit == 1f), "di notte accese");
            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(RenderSettings.fogColor, Camera.main.backgroundColor, "il fondo del colore del cielo");
        }

        [UnityTest, Description("Salvata in città, la partita dice di essere in città e Continua riporta lì")]
        public IEnumerator SavedInTown_ContinuesInTown()
        {
            yield return StartNewGame();
            string path = Path.Combine(_folder, "save_1.json");
            for (float t = 0f; !File.Exists(path); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 5f, "entrando in città non ha salvato");
                yield return null;
            }

            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(path, out SaveData data));
            Assert.IsTrue(data.InTown);
            Assert.AreEqual((TownScene, 0), (data.Scene, data.Depth));

            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Menu.LoadButton.onClick.Invoke();
            StringAssert.Contains("Slot 1 – level 1 · The village", Menu.SlotView(1).Text);
            Menu.ContinueButton.onClick.Invoke();
            yield return WaitForLevel();
            Assert.IsTrue(Manager.CurrentLevel.IsTown);
        }

        [UnityTest, Description("Un salvataggio fatto nell'accampamento della v1.5.0 (formato 7, accampamento di 13 × 10 celle tutto scoperto, portale aperto alla profondità 5) continua nel borgo: livello, oro e portale com'erano, la mappa scoperta del borgo riparte (D12 della M18)")]
        public IEnumerator OldCampSave_ContinuesInTheVillage()
        {
            var fixture = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Tests/EditMode/Fixtures/save_v7_camp.json");
            File.WriteAllText(Path.Combine(_folder, "save_1.json"), fixture.text);
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Menu.LoadButton.onClick.Invoke();
            StringAssert.Contains("Slot 1 – level 24 · The village", Menu.SlotView(1).Text);
            Menu.ContinueButton.onClick.Invoke();
            yield return WaitForLevel();

            var level = Manager.CurrentLevel;
            Assert.AreEqual(TownScene, level.gameObject.scene.name);
            Assert.IsTrue(level.IsTown);
            StringAssert.StartsWith("The village", UnityEngine.Object.FindFirstObjectByType<LevelTitle>().Text, "con la difficoltà del salvataggio");
            var player = GameObject.Find("Player");
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().isOnNavMesh, "sull'ingresso del borgo");
            Assert.AreEqual(24, player.GetComponent<DarkDescent.Progression.PlayerProgress>().Progress.Level);
            Assert.AreEqual(4321, player.GetComponent<DarkDescent.Items.PlayerInventory>().Inventory.Gold);

            var portals = UnityEngine.Object.FindFirstObjectByType<DarkDescent.Town.TownPortals>();
            Assert.AreEqual(5, portals.PortalDepth, "il portale aperto resta");
            Assert.AreEqual(new Vector3(36f, 0f, -20f), portals.PortalSpot);

            // l'accampamento scoperto aveva un'altra misura: il borgo si scopre da capo
            var exploration = UnityEngine.Object.FindFirstObjectByType<ExplorationTracker>().Exploration;
            Assert.AreEqual((level.Map.Width, level.Map.Height), (exploration.Map.Width, exploration.Map.Height));
            Assert.Less(exploration.ExploredCount, level.Map.Width * level.Map.Height / 4);
        }
    }
}
