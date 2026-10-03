using System.Collections;
using System.Linq;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class GameFeelTests : SandboxFixture
    {
        [TearDown]
        public void ResetTimeScale()
        {
            // timeScale sopravvive tra un test e l'altro, come tra una scena e l'altra
            Time.timeScale = 1f;
        }

        [UnityTest, Description("Due hit stop ravvicinati non si sommano e alla fine torna la scala di prima, non 1")]
        public IEnumerator HitStop_DoesNotStackAndRestoresPreviousScale()
        {
            yield return LoadSandbox();
            var hitStop = Object.FindFirstObjectByType<HitStop>();
            Time.timeScale = 0.5f;

            hitStop.Trigger(0.3f);
            Assert.AreEqual(0f, Time.timeScale);

            yield return new WaitForSecondsRealtime(0.15f);
            // il secondo arriva a metà: la fine diventa 0,45 s dal primo, non 0,6
            hitStop.Trigger(0.3f);
            Assert.AreEqual(0f, Time.timeScale);

            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsTrue(hitStop.IsActive, "a 0,35 s il secondo hit stop è ancora in corso");

            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsFalse(hitStop.IsActive, "a 0,55 s è finito: due hit stop sommati durerebbero fino a 0,6 s");
            Assert.AreEqual(0.5f, Time.timeScale, "va ripristinata la scala di prima, non 1 e non lo 0 del primo hit stop");
        }

        [UnityTest, Description("Colpo del player a segno: hit stop, lampo bianco e numero di danno, poi tutto torna normale")]
        public IEnumerator PlayerHit_TriggersStopFlashAndNumber()
        {
            yield return LoadSandbox();
            var skeleton = GameObject.Find("Skeleton");
            var skeletonHealth = skeleton.GetComponent<Health>();
            var flash = skeleton.GetComponentInChildren<HitFlash>();
            var renderer = skeleton.GetComponentInChildren<SkinnedMeshRenderer>();
            var originalMaterial = renderer.sharedMaterial;
            var hitStop = Object.FindFirstObjectByType<HitStop>();
            var numbers = Object.FindFirstObjectByType<DamageNumbers>();

            ClickAt(skeleton.transform.position + Vector3.up);
            // tempo reale: durante l'hit stop deltaTime è 0
            float elapsed = 0f;
            while (elapsed < 6f && skeletonHealth.Current >= skeletonHealth.Max)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Less(skeletonHealth.Current, skeletonHealth.Max, "il colpo deve arrivare");

            Assert.IsTrue(hitStop.IsActive, "il colpo del player ferma il tempo");
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(flash.IsFlashing);
            Assert.AreNotSame(originalMaterial, renderer.sharedMaterial, "durante il lampo il materiale è quello bianco");
            var texts = numbers.GetComponentsInChildren<TextMeshProUGUI>().Select(t => t.text).ToArray();
            string expected = Mathf.RoundToInt(MinHitDamage(Player.GetComponent<MeleeAttack>())).ToString();
            CollectionAssert.Contains(texts, expected, "deve comparire il numero del danno della spada");

            yield return new WaitForSecondsRealtime(1.2f);
            Assert.AreEqual(1f, Time.timeScale, "finito l'hit stop il tempo riparte");
            Assert.IsFalse(flash.IsFlashing);
            Assert.AreSame(originalMaterial, renderer.sharedMaterial, "finito il lampo torna il materiale originale");
        }

        [UnityTest, Description("I numeri tornano al pool a fine vita e il pool li riusa")]
        public IEnumerator DamageNumbers_ReturnToPoolAndAreReused()
        {
            yield return LoadSandbox();
            var skeletonHealth = GameObject.Find("Skeleton").GetComponent<Health>();
            var numbers = Object.FindFirstObjectByType<DamageNumbers>();

            skeletonHealth.TakeDamage(new DamageInfo(3f, DamageType.Physical, null));
            Assert.AreEqual(1, numbers.ActiveCount);
            int created = numbers.GetComponentsInChildren<DamageNumber>(true).Length;

            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, numbers.ActiveCount, "finita la vita il numero torna al pool");

            skeletonHealth.TakeDamage(new DamageInfo(3f, DamageType.Physical, null));
            Assert.AreEqual(1, numbers.ActiveCount);
            Assert.AreEqual(created, numbers.GetComponentsInChildren<DamageNumber>(true).Length, "il secondo numero riusa il primo");
        }
    }
}
