using System.Collections;
using DarkDescent.Combat;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class HealthOrbTests : SandboxFixture
    {
        private HealthOrb _orb;
        private Health _playerHealth;

        private IEnumerator LoadHud()
        {
            yield return LoadSandbox();
            _orb = Object.FindFirstObjectByType<HealthOrb>();
            Assert.IsNotNull(_orb, "sfera della vita non trovata nella scena");
            _playerHealth = Player.GetComponent<Health>();
        }

        private static DamageInfo Damage(float amount)
        {
            return new DamageInfo(amount, DamageType.Physical, null);
        }

        [UnityTest, Description("All'avvio la sfera è piena e scende subito a ogni danno, senza polling")]
        public IEnumerator PlayerDamaged_OrbFollowsHealth()
        {
            yield return LoadHud();
            Assert.AreEqual(1f, _orb.FillAmount, 0.001f, "all'avvio la sfera deve essere piena");

            _playerHealth.TakeDamage(Damage(25f));
            // nessun frame d'attesa: l'aggiornamento parte dall'evento, non da un Update
            Assert.AreEqual(0.75f, _orb.FillAmount, 0.001f);
        }

        [UnityTest, Description("Spenta durante un danno, alla riaccensione la sfera riparte dal valore attuale")]
        public IEnumerator OrbDisabledWhileDamaged_RefreshesOnEnable()
        {
            yield return LoadHud();

            _orb.gameObject.SetActive(false);
            _playerHealth.TakeDamage(Damage(40f));
            _orb.gameObject.SetActive(true);

            Assert.AreEqual(0.6f, _orb.FillAmount, 0.001f);
        }

        [UnityTest, Description("Un nuovo Bind stacca la sfera dalla vita precedente")]
        public IEnumerator Rebind_DetachesFromPreviousHealth()
        {
            yield return LoadHud();
            var skeletonHealth = GameObject.Find("Skeleton").GetComponent<Health>();

            _orb.Bind(skeletonHealth);
            _playerHealth.TakeDamage(Damage(50f));
            Assert.AreEqual(1f, _orb.FillAmount, 0.001f, "i danni al player non devono più arrivare");

            skeletonHealth.TakeDamage(Damage(15f));
            Assert.AreEqual(0.5f, _orb.FillAmount, 0.001f);
        }

        [UnityTest, Description("Il colpo dello scheletro fa scendere la sfera")]
        public IEnumerator SkeletonHitsPlayer_OrbDrops()
        {
            yield return LoadHud();

            ClickAt(new Vector3(6f, 0f, 1f));
            float elapsed = 0f;
            while (elapsed < 8f && _playerHealth.Current >= _playerHealth.Max)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(_orb.FillAmount, 1f, "la sfera deve scendere");
            Assert.AreEqual(_playerHealth.Current / _playerHealth.Max, _orb.FillAmount, 0.001f);
        }

        [UnityTest, Description("Un click sulla sfera non muove il player, anche se dietro c'è il pavimento")]
        public IEnumerator ClickOnOrb_DoesNotMovePlayer()
        {
            yield return LoadHud();
            var rect = (RectTransform)_orb.transform;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));

            // senza la UI quel click camminerebbe: il test non deve passare per caso
            Assert.IsTrue(Physics.Raycast(Camera.ScreenPointToRay(screen), 100f, LayerMask.GetMask("Ground")), "dietro la sfera deve esserci il pavimento");

            // il cursore arriva sopra la sfera prima del click, come con un mouse vero
            Move(Mouse.position, screen);
            yield return null;
            yield return null;
            Press(Mouse.leftButton);
            Release(Mouse.leftButton);
            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(PlayerAgent.hasPath, "il click sulla UI non deve muovere il player");
        }
    }
}
