using System.Collections;
using DarkDescent.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class PlayerLightTests : SandboxFixture
    {
        [UnityTest, Description("La luce del cavaliere lo segue, alta e spostata verso la camera, comunque lui si giri")]
        public IEnumerator PlayerLight_FollowsAboveTowardCamera()
        {
            yield return LoadCore();
            var rig = Player.GetComponentInChildren<PlayerLightRig>();
            Assert.IsNotNull(rig);

            Player.rotation = Quaternion.Euler(0f, 137f, 0f);
            yield return null;

            Vector3 light = rig.transform.position;
            Assert.AreEqual(Player.position.y + rig.Height, light.y, 0.01f, "all'altezza fissata");
            Assert.AreEqual(rig.TowardCamera, FlatDistance(light, Player.position), 0.01f);

            // verso la camera: in orizzontale la luce sta dal lato da cui la camera guarda
            Vector3 toLight = light - Player.position;
            Vector3 cameraForward = Camera.transform.forward;
            Assert.Less(toLight.x * cameraForward.x + toLight.z * cameraForward.z, 0f);
        }
    }
}
