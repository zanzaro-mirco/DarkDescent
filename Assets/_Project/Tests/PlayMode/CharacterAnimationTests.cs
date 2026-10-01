using System.Collections;
using DarkDescent.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class CharacterAnimationTests : SandboxFixture
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeathHash = Animator.StringToHash("Death");

        private static AnimatorStateInfo State(Animator animator)
        {
            return animator.GetCurrentAnimatorStateInfo(0);
        }

        private static IEnumerator WaitForState(Animator animator, int stateHash, float timeout)
        {
            float elapsed = 0f;
            while (elapsed < timeout && State(animator).shortNameHash != stateHash)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        [UnityTest, Description("Lo scheletro sta sul NavMesh, fermo in Locomotion, con il controller di override")]
        public IEnumerator Skeleton_StandsIdleOnNavMesh()
        {
            yield return LoadSandbox();
            var skeleton = GameObject.Find("Skeleton");
            Assert.IsNotNull(skeleton, "scheletro non trovato nella scena");
            Assert.IsTrue(skeleton.GetComponent<NavMeshAgent>().isOnNavMesh, "lo scheletro deve stare sul NavMesh");

            var animator = skeleton.GetComponentInChildren<Animator>();
            Assert.IsInstanceOf<AnimatorOverrideController>(animator.runtimeAnimatorController);

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(LocomotionHash, State(animator).shortNameHash, "da fermo deve stare in Locomotion");
            Assert.Less(animator.GetFloat(SpeedHash), 0.05f);
        }

        [UnityTest, Description("Un attacco parte subito, dura la clip e poi torna in Locomotion")]
        public IEnumerator PlayAttack_PlaysOnceThenReturnsToLocomotion()
        {
            yield return LoadSandbox();
            var driver = Player.GetComponentInChildren<CharacterAnimatorDriver>();
            var animator = driver.GetComponent<Animator>();

            driver.PlayAttack();
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(AttackHash, State(animator).shortNameHash, "l'attacco deve partire subito");

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(AttackHash, State(animator).shortNameHash, "a metà clip deve essere ancora in attacco");

            yield return WaitForState(animator, LocomotionHash, 2f);
            Assert.AreEqual(LocomotionHash, State(animator).shortNameHash, "finita la clip deve tornare in Locomotion");
        }

        [UnityTest, Description("Un secondo attacco a metà fendente fa ripartire la clip dall'inizio")]
        public IEnumerator PlayAttack_Twice_RestartsSwing()
        {
            yield return LoadSandbox();
            var driver = Player.GetComponentInChildren<CharacterAnimatorDriver>();
            var animator = driver.GetComponent<Animator>();

            driver.PlayAttack();
            yield return new WaitForSeconds(0.5f);
            float before = State(animator).normalizedTime;

            driver.PlayAttack();
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(AttackHash, State(animator).shortNameHash);
            Assert.Less(State(animator).normalizedTime, before, "il secondo attacco deve ripartire dall'inizio");
        }

        [UnityTest, Description("Dopo la morte colpi e attacchi vengono ignorati e il corpo resta in Death")]
        public IEnumerator PlayDeath_IgnoresLaterCommands()
        {
            yield return LoadSandbox();
            var driver = Player.GetComponentInChildren<CharacterAnimatorDriver>();
            var animator = driver.GetComponent<Animator>();

            driver.PlayDeath();
            yield return new WaitForSeconds(0.2f);
            driver.PlayHit();
            driver.PlayAttack();
            yield return new WaitForSeconds(2.5f);

            Assert.IsTrue(driver.IsDead);
            Assert.AreEqual(DeathHash, State(animator).shortNameHash, "la morte non ha uscite");
        }

        [UnityTest, Description("Con il culling acceso lo scheletro cambia comunque stato: Hit e ritorno in Locomotion")]
        public IEnumerator Skeleton_PlayHit_ReturnsToLocomotion()
        {
            yield return LoadSandbox();
            var driver = GameObject.Find("Skeleton").GetComponentInChildren<CharacterAnimatorDriver>();
            var animator = driver.GetComponent<Animator>();

            driver.PlayHit();
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(HitHash, State(animator).shortNameHash, "il colpo subito deve partire subito");

            yield return WaitForState(animator, LocomotionHash, 2f);
            Assert.AreEqual(LocomotionHash, State(animator).shortNameHash, "dopo il colpo deve tornare in Locomotion");
        }
    }
}
