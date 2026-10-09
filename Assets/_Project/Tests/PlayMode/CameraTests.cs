using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Levels;
using DarkDescent.Rendering;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [UnityTest, Description("La rotella avvicina e allontana la vista a scatti del 10%, tra il 60% e il 140%, e la scelta resta tra le preferenze")]
        public IEnumerator Wheel_ZoomsWithinLimits_AndRemembers()
        {
            yield return LoadSandbox();
            var zoom = Object.FindFirstObjectByType<CameraZoom>();
            var lens = zoom.GetComponent<CinemachineCamera>();
            float baseSize = zoom.BaseSize;
            Assert.AreEqual(9f, baseSize, "senza preferenza, la vista della scena");
            Assert.AreEqual(baseSize, Camera.orthographicSize, 1e-3f);

            // uno scatto in avanti: la vista arriva al 90% in 0,15 s
            yield return Scroll(1);
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.AreEqual(baseSize * 0.9f, lens.Lens.OrthographicSize, 1e-3f);
            Assert.AreEqual(baseSize * 0.9f, Camera.orthographicSize, 1e-3f, "la camera vera segue");
            Assert.AreEqual(0.9f, PlayerPrefs.GetFloat(CameraZoom.Preference), 1e-5f);

            for (int i = 0; i < 6; i++)
            {
                yield return Scroll(1);
            }

            yield return new WaitForSecondsRealtime(0.25f);
            Assert.AreEqual(baseSize * 0.6f, lens.Lens.OrthographicSize, 1e-3f, "non si avvicina oltre il 60%");

            for (int i = 0; i < 12; i++)
            {
                yield return Scroll(-1);
            }

            yield return new WaitForSecondsRealtime(0.25f);
            Assert.AreEqual(baseSize * 1.4f, lens.Lens.OrthographicSize, 1e-3f, "non si allontana oltre il 140%");

            // ricaricato il gioco, lo zoom è quello scelto, senza animazione
            SceneManager.LoadScene("Core");
            yield return WaitForLevel();
            zoom = Object.FindFirstObjectByType<CameraZoom>();
            Assert.AreEqual(baseSize * 1.4f, zoom.GetComponent<CinemachineCamera>().Lens.OrthographicSize, 1e-3f);
        }

        // uno scatto della rotella del mouse virtuale: 120 come su Windows, e due frame perché passi
        private IEnumerator Scroll(int notches)
        {
            Set(Mouse.scroll.y, notches * 120f);
            yield return null;
            yield return null;
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
