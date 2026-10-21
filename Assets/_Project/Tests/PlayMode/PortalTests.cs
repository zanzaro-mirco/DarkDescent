using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Magic;
using DarkDescent.Player;
using DarkDescent.Save;
using DarkDescent.Town;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>La magia del portale e i portali tra il dungeon e la città (D6 della M10, passo 11.7 della M11).</summary>
    public class PortalTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentPortal_" + Guid.NewGuid().ToString("N"));
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

        private static TownPortals Portals => UnityEngine.Object.FindFirstObjectByType<TownPortals>();

        private static MainMenu Menu => UnityEngine.Object.FindFirstObjectByType<MainMenu>();

        private static GameObject PlayerObject => GameObject.Find("Player");

        private static PlayerMagic Magic => PlayerObject.GetComponent<PlayerMagic>();

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

        private static IEnumerator WaitUntilIn(Func<LevelContext, bool> arrived, string what)
        {
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || !arrived(Manager.CurrentLevel); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 12f, what);
                yield return null;
            }

            yield return null;
        }

        // dove si è aperto l'ultimo portale del dungeon
        private static Vector3 _dungeonPortal;

        // una partita nuova, giù alla profondità 1, lontano dall'ingresso, la magia lanciata e il
        // portale attraversato
        private static IEnumerator GoToTownThroughPortal()
        {
            yield return StartNewGame();
            Manager.LoadLevel("Level_Crypt", "Start", 1);
            yield return WaitUntilIn(level => level.Depth == 1, "la cripta non arriva");
            // senza nemici: il punto lontano può stare in una stanza piena, e qui si provano i portali
            foreach (var enemy in Manager.CurrentLevel.Enemies)
            {
                enemy.gameObject.SetActive(false);
            }

            PlayerObject.GetComponent<NavMeshAgent>().Warp(FarFromEntrance());
            yield return null;
            Assert.AreEqual(PortalResult.Opened, Magic.TryCastPortal());
            var portal = Portals.OpenPortal;
            _dungeonPortal = portal.transform.position;
            PlayerObject.GetComponent<PlayerController>().Use(portal.Interactable);
            yield return WaitUntilIn(level => level.IsTown, "il portale non porta in città");
        }

        // il vertice del NavMesh più lontano dall'ingresso, ad almeno 4 m da ogni uscita: la cripta
        // cambia con il seme della partita nuova, e così un punto lontano c'è sempre
        private static Vector3 FarFromEntrance()
        {
            var level = Manager.CurrentLevel;
            Vector3 start = level.GetEntrance("Start").position;
            Vector3 best = start;
            foreach (var vertex in NavMesh.CalculateTriangulation().vertices)
            {
                if (Vector3.Distance(vertex, start) <= Vector3.Distance(best, start) || level.Exits.Any(exit => Vector3.Distance(exit.transform.position, vertex) < 4f))
                {
                    continue;
                }

                if (NavMesh.SamplePosition(vertex, out var hit, 0.5f, NavMesh.AllAreas))
                {
                    best = hit.position;
                }
            }

            Assert.Greater(Vector3.Distance(best, start), 8f, "un punto lontano dall'ingresso");
            return best;
        }

        [UnityTest, Description("Chiuso da ogni parte a meno di un metro e mezzo, come in un angolo tra muri e barili, il cavaliere apre il portale su un punto del NavMesh che raggiunge, e ci entra")]
        public IEnumerator Spell_InATightCorner_OpensAReachablePortal()
        {
            yield return StartNewGame();
            Manager.LoadLevel("Level_Crypt", "Start", 1);
            yield return WaitUntilIn(level => level.Depth == 1, "la cripta non arriva");
            foreach (var enemy in Manager.CurrentLevel.Enemies)
            {
                enemy.gameObject.SetActive(false);
            }

            var agent = PlayerObject.GetComponent<NavMeshAgent>();
            agent.Warp(FarFromEntrance());
            yield return null;
            Vector3 center = PlayerObject.transform.position;
            var walls = new GameObject("ZzStrettoia");
            SceneManager.MoveGameObjectToScene(walls, Manager.CurrentLevel.gameObject.scene);
            foreach (var side in new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
            {
                var wall = new GameObject("Muro");
                wall.transform.SetParent(walls.transform);
                wall.transform.position = center + side * 1.1f;
                wall.transform.rotation = Quaternion.LookRotation(side);
                var obstacle = wall.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = new Vector3(2.6f, 1f, 0.3f);
                obstacle.carving = true;
                obstacle.carveOnlyStationary = false;
            }

            yield return null;
            yield return null;
            foreach (var side in new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
            {
                Assert.IsTrue(NavMesh.Raycast(center, center + side * 2.5f, out var hit, NavMesh.AllAreas) && hit.distance < 1.2f, "chiuso da ogni parte");
            }

            Assert.AreEqual(PortalResult.Opened, Magic.TryCastPortal());
            var portal = Portals.OpenPortal;
            Assert.IsTrue(NavMesh.SamplePosition(portal.transform.position, out _, 0.1f, NavMesh.AllAreas), "il portale sta sul NavMesh");
            Assert.Less(Vector3.Distance(portal.transform.position, center), 1.2f, "dentro la strettoia");
            PlayerObject.GetComponent<PlayerController>().Use(portal.Interactable);
            yield return WaitUntilIn(level => level.IsTown, "il portale non porta in città");
        }

        [UnityTest, Description("Nessuna pergamena all'inizio; in città la magia del portale non parte e l'HUD lo dice. Nel dungeon T apre un portale davanti al cavaliere, sul NavMesh e nella scena del livello, e spende il mana; un secondo lancio lo sposta; senza mana non parte e il posto è scuro")]
        public IEnumerator Spell_OpensAPortalInTheDungeon()
        {
            yield return StartNewGame();
            var grid = PlayerObject.GetComponent<PlayerInventory>().Inventory.Grid;
            Assert.AreEqual(0, grid.Placements.Count, "lo zaino comincia vuoto");
            var slot = UnityEngine.Object.FindFirstObjectByType<PortalSlotView>();
            Assert.IsTrue(slot.HasIcon);
            Assert.AreEqual(PortalResult.InTown, Magic.TryCastPortal(), "in città non serve");
            Assert.AreEqual("You are already at the camp", UnityEngine.Object.FindFirstObjectByType<HudMessage>().Text);
            Assert.IsNull(Portals.OpenPortal);

            Manager.LoadLevel("Level_Crypt", "Start", 1);
            yield return WaitUntilIn(level => level.Depth == 1, "la cripta non arriva");
            Magic.Mana.Fill();
            float full = Magic.Mana.Current;
            Assert.GreaterOrEqual(full, Magic.PortalSpell.ManaCost, "si lancia già al primo livello");
            PlayerObject.GetComponent<PlayerInputReader>().PressPortal();
            yield return null;
            Assert.AreEqual(full - Magic.PortalSpell.ManaCost, Magic.Mana.Current, 0.5f, "il mana è speso");

            var portal = Portals.OpenPortal;
            Assert.IsNotNull(portal, "T apre il portale");
            Assert.IsTrue(portal.IsOpening, "cresce con il suono dell'apertura");
            Assert.AreEqual("Portal to the Camp", portal.Interactable.GetLabel(Localizer));
            Assert.AreEqual(Manager.CurrentLevel.gameObject.scene, portal.gameObject.scene, "si scarica con il livello");
            float distance = Vector3.Distance(PlayerObject.transform.position, portal.transform.position);
            Assert.That(distance, Is.InRange(1f, 2.6f), "davanti al cavaliere");
            Assert.IsTrue(NavMesh.SamplePosition(portal.transform.position, out _, 0.5f, NavMesh.AllAreas), "sul NavMesh, raggiungibile");

            Magic.Mana.Fill();
            Assert.AreEqual(PortalResult.Opened, Magic.TryCastPortal());
            yield return null;
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<Portal>(FindObjectsSortMode.None).Length, "uno solo per volta");

            Magic.Mana.SetCurrent(Magic.PortalSpell.ManaCost - 1f);
            Assert.IsFalse(slot.IsAffordable, "senza mana il posto è scuro");
            Assert.AreEqual(PortalResult.NoMana, Magic.TryCastPortal());
            Magic.Mana.Fill();
            Assert.IsTrue(slot.IsAffordable);
        }

        [UnityTest, Description("Il portale porta in città, dove un portale aspetta accanto all'ingresso e il salvataggio lo ricorda; riattraversato, riporta nel punto della profondità in cui si era aperto e si chiude")]
        public IEnumerator Portal_GoesToTownAndBack()
        {
            yield return GoToTownThroughPortal();
            Assert.AreEqual(TownPortals.PortalEntrance, Manager.CurrentEntrance);
            Assert.IsTrue(PlayerObject.GetComponent<NavMeshAgent>().isOnNavMesh);
            Assert.AreEqual(1, Portals.PortalDepth);

            var portal = Portals.OpenPortal;
            Assert.IsNotNull(portal, "il portale del ritorno aspetta in città");
            Assert.AreEqual("Portal to level 1", portal.Interactable.GetLabel(Localizer));
            Assert.Less(Vector3.Distance(PlayerObject.transform.position, portal.transform.position), 3f);

            var data = UnityEngine.Object.FindFirstObjectByType<SaveGame>().Capture();
            Assert.IsTrue(data.InTown);
            Assert.AreEqual(1, data.PortalDepth);
            Assert.AreEqual(_dungeonPortal, data.PortalSpot);

            PlayerObject.GetComponent<PlayerController>().Use(portal.Interactable);
            yield return WaitUntilIn(level => level.Depth == 1, "il portale della città non riporta giù");
            Assert.AreEqual("Level_Crypt", Manager.CurrentSceneName);
            Assert.AreEqual("Start", Manager.CurrentEntrance, "l'ingresso resta quello del livello, per il salvataggio");
            Assert.Less(Vector3.Distance(PlayerObject.transform.position, _dungeonPortal), 1.5f, "si torna dove si era aperto il portale");
            Assert.IsTrue(PlayerObject.GetComponent<NavMeshAgent>().isOnNavMesh);
            Assert.AreEqual(0, Portals.PortalDepth, "riattraversato si chiude");
            Assert.IsNull(Portals.PortalSpot);
            Assert.IsNull(Portals.OpenPortal);
        }

        [UnityTest, Description("Chiusa la partita in città con il portale aperto, Continua lo ritrova")]
        public IEnumerator SavedPortal_SurvivesContinue()
        {
            yield return GoToTownThroughPortal();
            string path = Path.Combine(_folder, "save_1.json");
            Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(path, out SaveData data));
            Assert.AreEqual((true, 1), (data.InTown, data.PortalDepth), "salvato entrando in città");

            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Menu.ContinueButton.onClick.Invoke();
            yield return WaitUntilIn(level => level.IsTown, "Continua non riporta in città");
            Assert.AreEqual(1, Portals.PortalDepth);
            Assert.AreEqual(_dungeonPortal, Portals.PortalSpot, "anche il punto del ritorno");
            Assert.IsNotNull(Portals.OpenPortal);
        }
    }
}
