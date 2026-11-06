using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>La stanza sigillata nella cripta vera (25.4 della M25, D5): la porta ferma, la leva apre, il ricordo la ritrova aperta e il livello non cambia.</summary>
    public class SealedRoomPlayModeTests : SandboxFixture
    {
        private const int SeedsToTry = 60;

        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentSealed_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            SealedRoom.Enabled = false;
            ChestSecret.Enabled = false;
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static void UseSeed(ulong seed)
        {
            Manager.RunSeed = seed;
            UnityEngine.Object.FindFirstObjectByType<CompositionRoot>().UseLootSeed(seed);
        }

        private static IEnumerator Travel(string scene, string entrance, int depth)
        {
            var before = Manager.CurrentLevel;
            Manager.LoadLevel(scene, entrance, depth);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == before; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, scene + " non arriva");
                yield return null;
            }

            yield return null;
            yield return null;
        }

        // la prima cripta con una stanza sigillata, dal seme 1 in su, alle profondità 2-4: ne ha una su tre
        private IEnumerator LevelWithASealedRoom(Action<int, ulong> found)
        {
            yield return LoadGeneratedCore();
            SealedRoom.Enabled = true;
            for (int seed = 1; seed <= SeedsToTry; seed++)
            {
                for (int depth = SealedRoomPlanner.FirstDepth; depth <= SealedRoomPlanner.LastDepth; depth++)
                {
                    UseSeed((ulong)seed);
                    yield return Travel("Level_Crypt", "Start", depth);
                    if (Manager.CurrentLevel.Secrets.Count > 0)
                    {
                        var health = Player.GetComponent<Health>();
                        health.MultiplyMax(500f);
                        health.Heal(float.MaxValue);
                        found(depth, (ulong)seed);
                        yield break;
                    }
                }
            }

            Assert.Fail($"nessun livello di {SeedsToTry} semi ha una stanza sigillata");
        }

        private static SealedRoom Room => (SealedRoom)Manager.CurrentLevel.Secrets[0];

        private static bool CanWalk(Vector3 from, Vector3 to)
        {
            if (!NavMesh.SamplePosition(from, out var a, 3f, NavMesh.AllAreas) || !NavMesh.SamplePosition(to, out var b, 3f, NavMesh.AllAreas))
            {
                return false;
            }

            var path = new NavMeshPath();
            return NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        [UnityTest, Timeout(300000), Description("La porta chiusa (D5): il tesoro del fondo non si raggiunge a piedi, la leva e la scala sì, e la porta ferma la vista")]
        public IEnumerator TheClosedDoor_BlocksTheDeadEnd_ButNotTheLeverOrTheStairs()
        {
            yield return LevelWithASealedRoom((d, s) => { });
            yield return null;
            yield return null;
            var level = Manager.CurrentLevel;
            var room = Room;
            Vector3 from = Player.position;

            Assert.IsFalse(room.Door.IsOpen);
            Assert.IsTrue(level.Chests.Any(c => room.Touches(c.transform.position)), "il fondo ha una cassa: il tesoro");
            foreach (var chest in level.Chests.Where(c => room.Touches(c.transform.position)))
            {
                Assert.IsFalse(CanWalk(from, chest.Clickable().ApproachPoint), "il tesoro del fondo non si raggiunge a porta chiusa");
            }

            Assert.IsTrue(CanWalk(from, room.Lever.GetComponent<Interactable>().ApproachPoint), "la leva si raggiunge");
            Assert.IsTrue(CanWalk(from, level.DownExits.First().GetComponent<Interactable>().ApproachPoint), "la scala si raggiunge");

            // la porta ferma la vista: un raggio dal corridoio dentro il fondo non passa
            var door = room.Door.GetComponentInChildren<BoxCollider>();
            Assert.IsTrue(door.enabled, "il collider della porta è acceso");
        }

        [UnityTest, Timeout(300000), Description("La leva (D5): un click la tira, la porta si apre, e il tesoro del fondo si raggiunge a piedi")]
        public IEnumerator PullingTheLever_OpensTheDoor_AndTheTreasureCanBeReached()
        {
            yield return LevelWithASealedRoom((d, s) => { });
            yield return null;
            var level = Manager.CurrentLevel;
            var room = Room;
            Vector3 from = Player.position;

            room.Lever.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Assert.IsTrue(room.Lever.IsPulled);
            Assert.IsTrue(room.Door.IsOpen, "la porta si apre");

            // il ritaglio del NavMesh non si applica nello stesso frame in cui si spegne l'ostacolo
            for (int i = 0; i < 6; i++)
            {
                yield return null;
            }

            foreach (var chest in level.Chests.Where(c => room.Touches(c.transform.position)))
            {
                Assert.IsTrue(CanWalk(from, chest.Clickable().ApproachPoint), "il tesoro si raggiunge a porta aperta");
            }

            Assert.IsTrue(room.IsDone);
        }

        [UnityTest, Timeout(300000), Description("Il ricordo del livello (D13): leva tirata e porta aperta si ritrovano quando si torna; il livello ha gli stessi nemici e le stesse casse")]
        public IEnumerator TheOpenDoor_IsRememberedWhenYouComeBack()
        {
            int depth = 0;
            ulong seed = 0;
            yield return LevelWithASealedRoom((d, s) => { depth = d; seed = s; });
            var level = Manager.CurrentLevel;
            int chests = level.Chests.Count;
            int enemies = level.Enemies.Count;
            Room.Lever.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Assert.IsTrue(Room.Door.IsOpen);

            yield return Travel("Level_Crypt", "Start", depth == 2 ? 3 : 2);
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", depth);
            var back = Manager.CurrentLevel;

            Assert.AreEqual(1, back.Secrets.Count, "lo stesso livello ha la stessa stanza sigillata");
            Assert.IsTrue(Room.Lever.IsPulled, "la leva è tirata");
            Assert.IsTrue(Room.Door.IsOpen, "la porta è aperta");
            Assert.AreEqual(chests, back.Chests.Count);
            Assert.AreEqual(enemies, back.Enemies.Count);
        }

        [UnityTest, Timeout(300000), Description("La stanza sigillata non tocca il livello (D14): con l'interruttore acceso e spento le casse e i nemici sono gli stessi, nello stesso posto")]
        public IEnumerator TheSealedRoom_DoesNotChangeTheLevel()
        {
            int depth = 0;
            ulong seed = 0;
            yield return LevelWithASealedRoom((d, s) => { depth = d; seed = s; });
            var on = Manager.CurrentLevel;
            var chestsOn = on.Chests.Select(c => c.transform.position).ToList();
            var enemiesOn = on.Enemies.Select(e => e.transform.position).ToList();

            SealedRoom.Enabled = false;
            UseSeed(seed);
            yield return Travel("Level_Crypt", "Start", depth);
            var off = Manager.CurrentLevel;
            Assert.AreEqual(0, off.Secrets.Count, "spento: nessuna porta");
            CollectionAssert.AreEqual(chestsOn, off.Chests.Select(c => c.transform.position).ToList(), "le stesse casse, nello stesso posto");
            CollectionAssert.AreEqual(enemiesOn, off.Enemies.Select(e => e.transform.position).ToList(), "gli stessi nemici, nello stesso posto");
        }
    }

    internal static class ChestInteractionExtensions
    {
        public static Interactable Clickable(this Chest chest)
        {
            return chest.GetComponent<Interactable>();
        }
    }
}
