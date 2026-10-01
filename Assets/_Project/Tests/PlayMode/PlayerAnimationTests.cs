using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class PlayerAnimationTests : SandboxFixture
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [UnityTest, Description("Speed sale in corsa e torna a zero all'arrivo; le ossa si muovono davvero")]
        public IEnumerator RunAndStop_DrivesAnimatorAndBones()
        {
            yield return LoadSandbox();
            var animator = Player.GetComponentInChildren<Animator>();
            var leg = animator.transform.Find("Rig_Medium/root/hips/upperleg.l");
            Assert.IsNotNull(leg, "osso upperleg.l non trovato");

            yield return new WaitForSeconds(0.5f);
            Assert.Less(animator.GetFloat(SpeedHash), 0.05f, "da fermo Speed deve essere circa 0");

            var target = new Vector3(-10f, 0f, -9f);
            ClickAt(target);
            yield return new WaitForSeconds(0.6f);
            var before = leg.localRotation;
            yield return new WaitForSeconds(0.15f);

            Assert.Greater(animator.GetFloat(SpeedHash), 0.6f, "in corsa Speed deve avvicinarsi a 1");
            // il player ha Always Animate: con il culling, in batchmode le ossa resterebbero ferme
            Assert.Greater(Quaternion.Angle(before, leg.localRotation), 2f, "la gamba deve muoversi");

            float elapsed = 0f;
            while (elapsed < 10f && FlatDistance(Player.position, target) > 0.3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(1f);

            Assert.Less(animator.GetFloat(SpeedHash), 0.05f, "all'arrivo Speed deve tornare circa a 0");
        }
    }
}
