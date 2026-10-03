using System.Collections;
using DarkDescent.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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

        [UnityTest, Description("La luce di riempimento illumina il cavaliere e nient'altro: niente pavimento, niente scheletri, niente ombre")]
        public IEnumerator FillLight_LightsOnlyThePlayer()
        {
            yield return LoadCore();
            var fill = Player.Find("PlayerFillLight");
            Assert.IsNotNull(fill);
            var light = fill.GetComponent<Light>();
            Assert.AreEqual(LightShadows.None, light.shadows);

            uint playerLayer = RenderingLayerMask.GetMask("Player");
            Assert.AreNotEqual(0u, playerLayer, "manca il rendering layer Player");
            Assert.AreEqual(playerLayer, (uint)fill.GetComponent<UniversalAdditionalLightData>().renderingLayers);

            foreach (var r in Player.GetComponentsInChildren<Renderer>())
            {
                Assert.AreNotEqual(0u, r.renderingLayerMask & playerLayer, $"{r.name} non prende la luce di riempimento");
            }

            foreach (var enemy in Object.FindObjectsByType<Enemies.EnemyAI>(FindObjectsSortMode.None))
            {
                foreach (var r in enemy.GetComponentsInChildren<Renderer>())
                {
                    Assert.AreEqual(0u, r.renderingLayerMask & playerLayer, $"{enemy.name}: {r.name} prende la luce del cavaliere");
                }
            }
        }
    }
}
