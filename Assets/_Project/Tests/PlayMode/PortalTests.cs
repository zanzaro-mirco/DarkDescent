using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Levels;
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
    /// <summary>La pergamena del ritorno e i portali tra il dungeon e la città (D6 della M10).</summary>
    public class PortalTests : SandboxFixture
    {
        // la pergamena di partenza sta nell'ultima colonna dello zaino
        private static readonly Vector2Int ScrollCell = new Vector2Int(9, 0);

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

        private static PlayerInventory Bag => PlayerObject.GetComponent<PlayerInventory>();

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

        // una partita nuova, giù alla profondità 1, lontano dall'ingresso, la pergamena letta e il
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
            Assert.IsTrue(Bag.UseAt(ScrollCell));
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

        [UnityTest, Description("Si comincia con una pergamena del ritorno; in città non si legge. Letta nel dungeon apre un portale davanti al cavaliere, sul NavMesh e nella scena del livello; una seconda pergamena lo sposta")]
        public IEnumerator Scroll_OpensAPortalInTheDungeon()
        {
            yield return StartNewGame();
            var grid = Bag.Inventory.Grid;
            Assert.IsInstanceOf<ReturnScrollDefinition>(grid.ItemAt(ScrollCell)?.Definition, "la pergamena di partenza");
            Assert.IsFalse(Bag.UseAt(ScrollCell), "in città non si legge");
            Assert.IsNotNull(grid.ItemAt(ScrollCell));
            Assert.IsNull(Portals.OpenPortal);

            Manager.LoadLevel("Level_Crypt", "Start", 1);
            yield return WaitUntilIn(level => level.Depth == 1, "la cripta non arriva");
            var scroll = grid.ItemAt(ScrollCell);
            Assert.IsTrue(Bag.UseAt(ScrollCell));
            Assert.IsNull(grid.ItemAt(ScrollCell), "letta, sparisce");

            var portal = Portals.OpenPortal;
            Assert.IsNotNull(portal);
            Assert.IsTrue(portal.IsOpening, "cresce con il suono dell'apertura");
            Assert.AreEqual("Portal to the Camp", portal.Interactable.GetLabel(Localizer));
            Assert.AreEqual(Manager.CurrentLevel.gameObject.scene, portal.gameObject.scene, "si scarica con il livello");
            float distance = Vector3.Distance(PlayerObject.transform.position, portal.transform.position);
            Assert.That(distance, Is.InRange(1f, 2.6f), "davanti al cavaliere");
            Assert.IsTrue(NavMesh.SamplePosition(portal.transform.position, out _, 0.5f, NavMesh.AllAreas), "sul NavMesh, raggiungibile");

            Assert.IsTrue(grid.TryPlace(scroll, ScrollCell));
            Assert.IsTrue(Bag.UseAt(ScrollCell));
            yield return null;
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<Portal>(FindObjectsSortMode.None).Length, "uno solo per volta");
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
            Assert.IsNull(Bag.Inventory.Grid.ItemAt(ScrollCell), "la pergamena letta non torna");
        }
    }
}
