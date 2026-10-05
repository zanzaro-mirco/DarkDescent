using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class CombatStatsTests : SandboxFixture
    {
        private GameObject _skeleton;
        private Health _skeletonHealth;
        private MeleeAttack _playerAttack;

        private IEnumerator LoadArena()
        {
            yield return LoadSandbox();
            _skeleton = GameObject.Find("Skeleton");
            _skeletonHealth = _skeleton.GetComponent<Health>();
            _playerAttack = Player.GetComponent<MeleeAttack>();
        }

        [UnityTest, Description("Il cavaliere ha gli attributi di D1 e la vita dalla Vitalità; lo scheletro un blocco fisso")]
        public IEnumerator Stats_KnightAndSkeleton()
        {
            yield return LoadArena();

            var knight = Player.GetComponent<CharacterStats>();
            Assert.AreEqual(30f, knight.Strength);
            Assert.AreEqual(20f, knight.Dexterity);
            Assert.AreEqual(10f, knight.Magic);
            Assert.AreEqual(25f, knight.Vitality);
            Assert.AreEqual(0f, knight.Armor);
            Assert.IsTrue(knight.LifeFromVitality);
            Assert.AreEqual(CombatFormulas.MaxLife(25f), Player.GetComponent<Health>().Max);
            Assert.AreEqual(100f, Player.GetComponent<Health>().Max, "la vita della M2 non cambia");

            var skeleton = _skeleton.GetComponent<CharacterStats>();
            Assert.AreEqual(10f, skeleton.Dexterity);
            Assert.AreEqual(10f, skeleton.Armor);
            Assert.IsFalse(skeleton.LifeFromVitality);
            Assert.AreEqual(30f, _skeletonHealth.Max);
        }

        [UnityTest, Description("Un colpo mancato: niente danno, niente hit stop, e sopra lo scheletro compare \"Miss\"")]
        public IEnumerator MissedSwing_NoDamageShowsMiss()
        {
            yield return LoadArena();
            // 99 non è sotto nessuna probabilità di colpire: tutti i colpi vanno a vuoto
            UseRandom(0.99);
            var hitStop = Object.FindFirstObjectByType<HitStop>();
            var numbers = Object.FindFirstObjectByType<DamageNumbers>();
            int misses = 0;
            void CountMiss(DamageInfo info) => misses++;
            _playerAttack.Missed += CountMiss;

            ClickAt(_skeleton.transform.position + Vector3.up);
            float elapsed = 0f;
            while (elapsed < 6f && misses == 0)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _playerAttack.Missed -= CountMiss;
            Assert.AreEqual(1, misses, "il colpo deve partire e andare a vuoto");
            Assert.AreEqual(_skeletonHealth.Max, _skeletonHealth.Current, "un colpo mancato non toglie vita");
            Assert.IsFalse(hitStop.IsActive, "un colpo mancato non ferma il tempo");
            var texts = numbers.GetComponentsInChildren<TextMeshProUGUI>().Select(t => t.text).ToArray();
            CollectionAssert.Contains(texts, "Miss");
        }

        [UnityTest, Description("Un colpo a segno toglie il danno tirato: arma per Forza, arrotondato")]
        public IEnumerator Hit_DealsRolledDamage()
        {
            yield return LoadArena();
            // 0,7: colpito (70 < 75) e tiro 6 + 2 = 8, per 1,3 di Forza = 10,4, cioè 10
            UseRandom(0.7);

            ClickAt(_skeleton.transform.position + Vector3.up);
            float elapsed = 0f;
            while (elapsed < 6f && _skeletonHealth.Current >= _skeletonHealth.Max)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(_skeletonHealth.Max - 10f, _skeletonHealth.Current, 0.001f);
        }
    }
}
