using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Levels;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class CameraTests : SandboxFixture
    {
        // metà della zona morta del Position Composer (0,06 × 0,08 dello schermo), più un margine
        private const float DeadZoneHalfWidth = 0.03f + 0.02f;
        private const float DeadZoneHalfHeight = 0.04f + 0.02f;

        private Vector3 PlayerViewport => Camera.WorldToViewportPoint(Player.position);

        [UnityTest, Description("Dopo un cambio di livello la camera è subito sul cavaliere, senza attraversare la mappa")]
        public IEnumerator Camera_SnapsToPlayerAfterLevelLoad()
        {
            yield return LoadSandbox();

            // lontano dall'ingresso: con il solo smorzamento la camera ci metterebbe secondi a tornare
            PlayerAgent.Warp(new Vector3(30f, 0f, 30f));
            yield return new WaitForSeconds(1.5f);

            Object.FindFirstObjectByType<LevelManager>().LoadLevel(SandboxScene, "Start", 1);
            yield return WaitForLevel();
            yield return null;

            Vector3 viewport = PlayerViewport;
            Assert.AreEqual(0.5f, viewport.x, 0.02f, "il cavaliere deve essere al centro");
            Assert.AreEqual(0.5f, viewport.y, 0.02f, "il cavaliere deve essere al centro");
        }

        [UnityTest, Description("La camera segue il cavaliere e lo tiene dentro la zona morta")]
        public IEnumerator Camera_FollowsPlayer()
        {
            yield return LoadSandbox();

            PlayerAgent.Warp(new Vector3(8f, 0f, -6f));
            yield return new WaitForSeconds(1.5f);

            Vector3 viewport = PlayerViewport;
            Assert.AreEqual(0.5f, viewport.x, DeadZoneHalfWidth);
            Assert.AreEqual(0.5f, viewport.y, DeadZoneHalfHeight);
        }

        [UnityTest, Description("Un colpo subito dal cavaliere fa partire una scossa della camera")]
        public IEnumerator PlayerHit_GeneratesImpulse()
        {
            yield return LoadSandbox();
            var listenerPosition = Camera.transform.position;

            Player.GetComponent<Health>().TakeDamage(new DamageInfo(20f, DamageType.Physical, null));

            bool shaking = false;
            float elapsed = 0f;
            while (!shaking && elapsed < 0.2f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                shaking = CinemachineImpulseManager.Instance.GetImpulseAt(listenerPosition, false, 1, out _, out _);
            }

            Assert.IsTrue(shaking, "la scossa deve partire");
        }
    }
}
