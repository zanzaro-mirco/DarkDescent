using DarkDescent.Player;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La direzione dei tasti di movimento rispetto alla camera (D2 e D3 della M23): logica pura, senza scena.</summary>
    public class KeyboardMoveMathTests
    {
        // una camera isometrica come quella del gioco: guarda in giù di 40° e gira di 45° + 90° per scatto
        private static (Vector3 forward, Vector3 up, Vector3 right) Camera(float yaw, float pitch = 40f)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            return (rotation * Vector3.forward, rotation * Vector3.up, rotation * Vector3.right);
        }

        private static Vector3 Direction(Vector2 keys, float yaw, float pitch = 40f)
        {
            var (forward, up, right) = Camera(yaw, pitch);
            return KeyboardMove.WorldDirection(keys, forward, up, right);
        }

        private static Vector3 FlatForward(float yaw)
        {
            return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        }

        [Test, Description("Nessun tasto, o due tasti opposti: nessuna direzione")]
        public void NoKeys_NoDirection()
        {
            Assert.AreEqual(Vector3.zero, Direction(Vector2.zero, 45f));
            Assert.AreEqual(Vector3.zero, Direction(new Vector2(0.05f, 0.05f), 45f), "sotto la zona morta");
            Assert.IsFalse(KeyboardMove.IsPressed(Vector2.zero));
            Assert.IsTrue(KeyboardMove.IsPressed(Vector2.up));
        }

        [Test, Description("W va verso l'alto dello schermo, qualunque sia lo scatto della visuale: la direzione è quella in avanti della camera, in piano")]
        public void W_GoesUpTheScreen_AtEveryTurn()
        {
            for (int turn = 0; turn < 4; turn++)
            {
                float yaw = 45f + 90f * turn;
                Vector3 expected = FlatForward(yaw);
                Vector3 actual = Direction(Vector2.up, yaw);
                Assert.AreEqual(0f, Vector3.Distance(expected, actual), 0.0001f, $"scatto {turn}, yaw {yaw}");
                Assert.AreEqual(0f, actual.y, "in piano");
            }
        }

        [Test, Description("S, A e D sono il contrario, la sinistra e la destra dello schermo a ogni scatto")]
        public void SAndAAndD_FollowTheScreen_AtEveryTurn()
        {
            for (int turn = 0; turn < 4; turn++)
            {
                float yaw = 45f + 90f * turn;
                Vector3 forward = FlatForward(yaw);
                Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                Assert.AreEqual(0f, Vector3.Distance(-forward, Direction(Vector2.down, yaw)), 0.0001f, $"S, yaw {yaw}");
                Assert.AreEqual(0f, Vector3.Distance(right, Direction(Vector2.right, yaw)), 0.0001f, $"D, yaw {yaw}");
                Assert.AreEqual(0f, Vector3.Distance(-right, Direction(Vector2.left, yaw)), 0.0001f, $"A, yaw {yaw}");
            }
        }

        [Test, Description("La diagonale ha lunghezza 1 come le altre direzioni, e sta a metà tra i due tasti")]
        public void Diagonal_IsNotFaster()
        {
            for (int turn = 0; turn < 4; turn++)
            {
                float yaw = 45f + 90f * turn;
                foreach (var keys in new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f), new Vector2(0.7071f, 0.7071f) })
                {
                    Vector3 diagonal = Direction(keys, yaw);
                    Assert.AreEqual(1f, diagonal.magnitude, 0.0001f, $"yaw {yaw}, tasti {keys}");
                }

                Vector3 both = Direction(new Vector2(1f, 1f), yaw);
                Vector3 half = (Direction(Vector2.up, yaw) + Direction(Vector2.right, yaw)).normalized;
                Assert.AreEqual(0f, Vector3.Distance(both, half), 0.0001f, "a metà tra W e D");
            }
        }

        [Test, Description("La camera gira a ogni frame: la stessa tastiera dà un'altra direzione dopo uno scatto, e quattro scatti tornano al punto di partenza")]
        public void TurningTheView_TurnsTheDirection()
        {
            Vector3 first = Direction(Vector2.up, 45f);
            Vector3 second = Direction(Vector2.up, 135f);
            Assert.AreEqual(90f, Vector3.Angle(first, second), 0.01f);
            Assert.AreEqual(0f, Vector3.Distance(first, Direction(Vector2.up, 45f + 360f)), 0.0001f);
        }

        [Test, Description("Una camera quasi a picco, il cui avanti in piano sparisce, usa il suo su: W va comunque verso l'alto dello schermo")]
        public void TopDownCamera_UsesItsUp()
        {
            Vector3 direction = Direction(Vector2.up, 135f, 90f);
            Assert.AreEqual(1f, direction.magnitude, 0.0001f);
            Assert.AreEqual(0f, Vector3.Distance(FlatForward(135f), direction), 0.001f);
        }
    }
}
