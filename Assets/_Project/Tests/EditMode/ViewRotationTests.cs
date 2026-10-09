using DarkDescent.Levels;
using DarkDescent.Rendering;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>La rotazione della visuale (D12 della M8): scatti di 90° in 0,4 s, i lati lontani che cambiano a metà.</summary>
    public class ViewRotationTests
    {
        [Test, Description("Si parte guardando a nord-est: lontani nord ed est; ogni scatto a destra sposta la coppia di un lato")]
        public void IsFar_FollowsFacing()
        {
            Assert.IsTrue(ViewRotation.IsFar(MapDirection.North, 0));
            Assert.IsTrue(ViewRotation.IsFar(MapDirection.East, 0));
            Assert.IsFalse(ViewRotation.IsFar(MapDirection.South, 0));
            Assert.IsFalse(ViewRotation.IsFar(MapDirection.West, 0));

            // la camera a nord-ovest guarda a sud-est: lontani est e sud
            Assert.IsTrue(ViewRotation.IsFar(MapDirection.East, 1));
            Assert.IsTrue(ViewRotation.IsFar(MapDirection.South, 1));
            Assert.IsFalse(ViewRotation.IsFar(MapDirection.North, 1));

            Assert.IsTrue(ViewRotation.IsFar(MapDirection.South, 2));
            Assert.IsTrue(ViewRotation.IsFar(MapDirection.West, 2));
            Assert.IsTrue(ViewRotation.IsFar(MapDirection.West, 3));
            Assert.IsTrue(ViewRotation.IsFar(MapDirection.North, 3));
            Assert.IsFalse(ViewRotation.IsFar(MapDirection.East, 3));

            // per ogni lato della camera, due lati lontani e due vicini
            for (int facing = 0; facing < 4; facing++)
            {
                int far = 0;
                for (int side = 0; side < 4; side++)
                {
                    far += ViewRotation.IsFar((MapDirection)side, facing) ? 1 : 0;
                }

                Assert.AreEqual(2, far);
            }
        }

        [Test, Description("Uno scatto a destra porta l'imbardata da 45° a 135° in 0,4 s; il lato cambia a metà, non prima")]
        public void Turn_ReachesNextQuarterInDuration()
        {
            var rotation = new ViewRotation();
            Assert.AreEqual(45f, rotation.Yaw);
            Assert.AreEqual(0, rotation.Facing);

            rotation.Turn(1);
            Assert.IsTrue(rotation.IsTurning);
            Assert.AreEqual(1, rotation.TargetFacing);
            rotation.Advance(0.19f);
            Assert.Less(rotation.Yaw, 90f);
            Assert.AreEqual(0, rotation.Facing, "prima di metà i muri restano quelli di prima");
            rotation.Advance(0.02f);
            Assert.Greater(rotation.Yaw, 90f);
            Assert.AreEqual(1, rotation.Facing, "passata la metà, il lato nuovo");

            rotation.Advance(0.2f);
            Assert.AreEqual(135f, rotation.Yaw, 1e-4f);
            Assert.IsFalse(rotation.IsTurning);
            Assert.IsFalse(rotation.Advance(0.1f), "ferma, non cambia più");
        }

        [Test, Description("Quattro scatti a sinistra tornano al lato di partenza, senza salti di 360° nell'imbardata")]
        public void FourTurns_WrapAround()
        {
            var rotation = new ViewRotation();
            for (int i = 0; i < 4; i++)
            {
                rotation.Turn(-1);
                float previous = rotation.Yaw;
                while (rotation.Advance(0.05f))
                {
                    Assert.LessOrEqual(rotation.Yaw, previous + 1e-4f, "gira sempre dalla stessa parte");
                    Assert.Greater(rotation.Yaw, previous - 50f, "nessun salto");
                    previous = rotation.Yaw;
                }
            }

            Assert.AreEqual(0, rotation.Facing);
            Assert.AreEqual(45f - 360f, rotation.Yaw, 1e-3f);
            rotation.Turn(-1);
            rotation.Advance(1f);
            Assert.AreEqual(3, rotation.Facing);
        }

        [Test, Description("Uno scatto durante la rotazione riparte da dove si è e arriva un quarto più in là")]
        public void TurnWhileTurning_StartsFromCurrent()
        {
            var rotation = new ViewRotation();
            rotation.Turn(1);
            rotation.Advance(0.1f);
            float midway = rotation.Yaw;
            rotation.Turn(1);
            Assert.AreEqual(midway, rotation.Yaw, "nessun salto allo scatto");
            rotation.Advance(ViewRotation.Duration);
            Assert.AreEqual(225f, rotation.Yaw, 1e-4f);
            Assert.AreEqual(2, rotation.Facing);
        }
    }
}
