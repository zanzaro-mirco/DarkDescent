using System.Collections;
using DarkDescent.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class PlayerHitRecoveryTests : SandboxFixture
    {
        private static T Load<T>(string path) where T : Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private MeleeAttack _attack;
        private Health _knight;
        private Health _skeleton;
        private int _staggers;

        private void CountStagger()
        {
            _staggers++;
        }

        private IEnumerator LoadArena()
        {
            yield return LoadSandbox();
            _attack = Player.GetComponent<MeleeAttack>();
            _knight = Player.GetComponent<Health>();
            _skeleton = GameObject.Find("Skeleton").GetComponent<Health>();
            _staggers = 0;
            Player.GetComponent<HitRecovery>().Staggered += CountStagger;
        }

        [UnityTearDown]
        public IEnumerator Unsubscribe()
        {
            if (Player != null)
            {
                Player.GetComponent<HitRecovery>().Staggered -= CountStagger;
            }

            yield return null;
        }

        // il cavaliere comincia un fendente sullo scheletro
        private IEnumerator StartSwing()
        {
            _attack.SetTarget(_skeleton);
            for (float time = 0f; !_attack.IsSwinging; time += Time.deltaTime)
            {
                Assert.Less(time, 6f, "il cavaliere non ha cominciato il colpo");
                _attack.SetTarget(_skeleton);
                yield return null;
            }
        }

        private void Hit(float amount)
        {
            _knight.TakeDamage(new DamageInfo(amount, DamageType.Physical, _skeleton.gameObject));
        }

        [Test, Description("La soglia è un quinto della vita del cavaliere (D7): il colpo forte del bruto la supera sempre, i suoi colpi leggeri e quelli di scheletro e sciame mai")]
        public void Threshold_SeparatesTheBruteFromTheRest()
        {
            var knight = Load<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var recovery = new UnityEditor.SerializedObject(knight.GetComponent<HitRecovery>());
            float threshold = recovery.FindProperty("_threshold").floatValue * 100f;
            Assert.AreEqual(20f, threshold, 0.01f, "con la vita di partenza, 100");
            Assert.AreEqual(0.4f, recovery.FindProperty("_duration").floatValue, 0.001f);

            int Max(string prefab) => Load<GameObject>($"Assets/_Project/Prefabs/{prefab}.prefab").GetComponent<MeleeAttack>().Weapon.MaxDamage;
            int Min(string prefab) => Load<GameObject>($"Assets/_Project/Prefabs/{prefab}.prefab").GetComponent<MeleeAttack>().Weapon.MinDamage;
            Assert.GreaterOrEqual(Min("Brute"), threshold, "il bruto interrompe");
            Assert.Less(Max("Skeleton"), threshold, "lo scheletro no");
            Assert.Less(Max("Swarm"), threshold, "lo sciame no");
            var quick = Load<GameObject>("Assets/_Project/Prefabs/Brute.prefab").GetComponent<MeleeAttack>().QuickWeapon;
            Assert.Less(quick.MaxDamage, threshold, "i colpi leggeri del bruto no");
        }

        [UnityTest, Description("Un colpo debole durante il fendente non lo ferma; uno forte lo annulla, il danno non arriva e il cavaliere reagisce")]
        public IEnumerator StrongHit_CancelsTheSwing()
        {
            yield return LoadArena();
            float skeletonLife = _skeleton.Current;

            yield return StartSwing();
            Hit(6f);
            Assert.IsTrue(_attack.IsSwinging, "un colpo da scheletro non interrompe");
            Assert.AreEqual(0, _staggers);
            yield return new WaitForSeconds(_attack.Weapon.HitDelay + 0.1f);
            Assert.Less(_skeleton.Current, skeletonLife, "il fendente è arrivato");

            // il prossimo fendente, interrotto da un colpo da bruto
            yield return new WaitForSeconds(_attack.Weapon.AttackInterval);
            skeletonLife = _skeleton.Current;
            yield return StartSwing();
            Hit(24f);
            Assert.IsFalse(_attack.IsSwinging, "il colpo forte annulla il fendente");
            Assert.IsTrue(_attack.IsInterrupted);
            Assert.AreEqual(1, _staggers, "il cavaliere reagisce");
            yield return new WaitForSeconds(_attack.Weapon.HitDelay + 0.1f);
            Assert.AreEqual(skeletonLife, _skeleton.Current, "il fendente annullato non arriva");
        }

        [UnityTest, Description("Fermato da un colpo forte, il cavaliere resta fermo per 0,4 s: un click in quel momento parte alla fine")]
        public IEnumerator StrongHit_HoldsTheKnightThenTheClickGoes()
        {
            yield return LoadArena();
            Vector3 start = Player.position;
            Vector3 away = start + Vector3.left * 4f;
            ClickAt(away);
            yield return new WaitForSeconds(0.15f);
            Assert.Greater(FlatDistance(Player.position, start), 0.1f, "cammina");

            Hit(24f);
            Vector3 stopped = Player.position;
            ClickAt(away + Vector3.forward * 2f);
            yield return new WaitForSeconds(0.3f);
            Assert.Less(FlatDistance(Player.position, stopped), 0.05f, "fermo subito, e non cammina nemmeno con un click");

            yield return new WaitForSeconds(0.6f);
            Assert.Greater(FlatDistance(Player.position, stopped), 0.5f, "finito il blocco parte il click fatto in mezzo");
        }
    }
}
