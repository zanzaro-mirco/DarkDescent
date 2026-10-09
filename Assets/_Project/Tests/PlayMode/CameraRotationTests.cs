using System.Collections;
using System.Linq;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Rendering;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// La rotazione della visuale nel gioco (D12 della M8): Q ed E girano la camera di 90°, i muri
    /// alti passano sui lati lontani, ogni torcia resta una e l'automappa gira con la camera.
    /// </summary>
    public class CameraRotationTests : SandboxFixture
    {
        [UnityTest, Description("Dopo ogni scatto, tra la camera e il cavaliere nessun muro alto; i muri alti sono sui due lati lontani; una torcia accesa per ogni torcia della mappa; l'automappa girata come la camera")]
        public IEnumerator EachTurn_KeepsKnightVisible()
        {
            yield return LoadGeneratedCore();
            var rotation = Object.FindFirstObjectByType<CameraRotation>();
            var level = Object.FindFirstObjectByType<LevelManager>().CurrentLevel;
            var walls = level.GetComponent<WallView>();
            Assert.IsNotNull(walls, "il livello generato ha muri per tutti e quattro i lati");
            Assert.AreEqual(0, walls.Facing, "si parte guardando a nord-est");
            int torches = level.Map.Markers.Count(m => m.Symbol == DungeonPopulator.TorchSymbol);
            Assert.Greater(torches, 0);

            var keyboard = InputSystem.AddDevice<Keyboard>();
            var automap = Object.FindFirstObjectByType<AutomapView>();
            for (int turn = 1; turn <= 4; turn++)
            {
                AssertView(rotation, walls, level, torches, automap);

                // il primo scatto con il tasto vero, gli altri dal componente
                if (turn == 1)
                {
                    Press(keyboard.eKey);
                    yield return null;
                    Release(keyboard.eKey);
                }
                else
                {
                    rotation.Turn(1);
                }

                yield return new WaitForSecondsRealtime(0.55f);
                Assert.IsFalse(rotation.IsTurning);
                Assert.AreEqual(turn % 4, rotation.Facing);
                Assert.AreEqual(Mathf.Repeat(45f + 90f * turn, 360f), Mathf.Repeat(Camera.transform.eulerAngles.y, 360f), 0.5f);
                Assert.AreEqual(turn % 4, walls.Facing, "i muri seguono la camera");
            }

            AssertView(rotation, walls, level, torches, automap);

            // Q gira dall'altra parte
            Press(keyboard.qKey);
            yield return null;
            Release(keyboard.qKey);
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.AreEqual(3, rotation.Facing);
            AssertView(rotation, walls, level, torches, automap);
        }

        [UnityTest, Description("Cambiando livello, i muri del livello nuovo nascono già girati come la camera")]
        public IEnumerator NewLevel_FollowsCurrentFacing()
        {
            yield return LoadGeneratedCore();
            var rotation = Object.FindFirstObjectByType<CameraRotation>();
            rotation.Turn(-1);
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.AreEqual(3, rotation.Facing);

            var manager = Object.FindFirstObjectByType<LevelManager>();
            manager.LoadLevel("Level_Crypt", "FromAbove", 2);
            yield return WaitForLevel();
            yield return null;
            var walls = manager.CurrentLevel.GetComponent<WallView>();
            Assert.AreEqual(2, manager.CurrentLevel.Depth);
            Assert.AreEqual(3, walls.Facing);
            AssertNoHighWallBetween(GameObject.Find("Player").transform);
        }

        private void AssertView(CameraRotation rotation, WallView walls, LevelContext level, int torches, AutomapView automap)
        {
            AssertNoHighWallBetween(Player);

            // muri alti accesi solo sui lati lontani, bassi solo su quelli vicini
            var high = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.CompareTag(PlayerController.WallTag) && t.IsChildOf(level.transform))
                .Select(t => t.parent.parent.name).Distinct().ToList();
            var far = Enumerable.Range(0, 4).Select(s => (MapDirection)s).Where(s => ViewRotation.IsFar(s, rotation.Facing)).Select(s => s.ToString()).ToList();
            CollectionAssert.AreEquivalent(far, high, $"lato {rotation.Facing}: muri alti fuori posto");

            int lit = Object.FindObjectsByType<TorchFlicker>(FindObjectsSortMode.None).Count(t => t.transform.IsChildOf(level.transform));
            Assert.AreEqual(torches, lit, $"lato {rotation.Facing}: una torcia per ogni torcia della mappa");

            float mapYaw = automap.Corner.MapImage.rectTransform.parent.localEulerAngles.z;
            Assert.AreEqual(Mathf.Repeat(rotation.Yaw, 360f), Mathf.Repeat(mapYaw, 360f), 0.5f, "l'automappa gira con la camera");
        }

        // dal centro della camera al petto del cavaliere: niente con il tag dei muri alti
        private void AssertNoHighWallBetween(Transform player)
        {
            Vector3 from = Camera.transform.position;
            Vector3 to = player.position + Vector3.up * 1f;
            foreach (var hit in Physics.RaycastAll(from, to - from, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore))
            {
                Assert.IsFalse(hit.collider.CompareTag(PlayerController.WallTag), $"il muro alto {hit.collider.name} copre il cavaliere");
            }
        }
    }
}
