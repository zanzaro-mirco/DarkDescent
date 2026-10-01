using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class PlayerMovementTests : SandboxFixture
    {
        [UnityTest, Description("Click dietro il muro: il player ci arriva aggirandolo, con i piedi sul pavimento")]
        public IEnumerator ClickBehindWall_ReachesPointGoingAround()
        {
            yield return LoadSandbox();
            var target = new Vector3(0f, 0f, -9f);
            ClickAt(target);

            float elapsed = 0f;
            while (elapsed < 8f && FlatDistance(Player.position, target) > 0.3f)
            {
                var p = Player.position;
                // muro in (0, 1, -6), scala (8, 2, 1): il player non deve mai attraversarlo
                Assert.IsFalse(Mathf.Abs(p.x) < 4f && Mathf.Abs(p.z + 6f) < 0.5f, $"player dentro il muro in {p}");
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(FlatDistance(Player.position, target), 0.3f, $"non arrivato: {Player.position} dopo {elapsed:F1}s");
            Assert.AreEqual(0f, Player.position.y, 0.05f, "il pivot ai piedi deve stare sul pavimento");
        }

        [UnityTest, Description("Click su un ostacolo: il player non si muove")]
        public IEnumerator ClickOnObstacle_DoesNothing()
        {
            yield return LoadSandbox();
            var start = Player.position;

            ClickAt(new Vector3(4f, 2f, 3f));
            yield return new WaitForSeconds(1f);

            Assert.IsFalse(PlayerAgent.hasPath);
            Assert.Less(Vector3.Distance(start, Player.position), 0.01f);
        }

        [UnityTest, Description("Tenendo premuto, la destinazione segue il cursore")]
        public IEnumerator HoldingButton_DestinationFollowsCursor()
        {
            yield return LoadSandbox();
            var first = new Vector3(8f, 0f, -2f);
            PointAt(first);
            Press(Mouse.leftButton);
            yield return null;
            Assert.Less(FlatDistance(PlayerAgent.destination, first), 0.5f);

            // punto scelto perché il raggio dalla camera non passi sopra un ostacolo
            var second = new Vector3(-8f, 0f, 10f);
            PointAt(second);
            yield return new WaitForSeconds(0.2f);

            Assert.Less(FlatDistance(PlayerAgent.destination, second), 1f, $"destinazione {PlayerAgent.destination}");
            Release(Mouse.leftButton);
        }
    }
}
