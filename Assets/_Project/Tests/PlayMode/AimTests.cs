using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Rendering;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Dalla prova della build M7: nello sciame non si capiva quale nemico si stava per colpire, i click
    /// finivano a terra e il cavaliere veniva spinto dagli altri agent.
    /// </summary>
    public class AimTests : SandboxFixture
    {
        private static T Load<T>(string path) where T : Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private IEnumerator StillSkeleton()
        {
            yield return LoadSandbox();
            Object.FindFirstObjectByType<EnemyAI>().enabled = false;
        }

        [UnityTest, Description("Con il cursore su un nemico: in alto il suo nome e la sua vita, a terra il cerchio rosso; colpito, la barra cala; morto, spariscono")]
        public IEnumerator HoveredEnemy_ShowsNameLifeAndRing()
        {
            yield return StillSkeleton();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>().GetComponent<Health>();
            var bar = Object.FindFirstObjectByType<EnemyBar>();
            var marker = Object.FindFirstObjectByType<TargetMarker>();
            Assert.IsNull(bar.Shown);
            Assert.IsNull(marker.Target);

            PointAt(skeleton.transform.position + Vector3.up);
            yield return null;
            yield return null;
            Assert.AreSame(skeleton, bar.Shown);
            Assert.AreEqual("Skeleton", bar.NameText);
            Assert.AreEqual(1f, bar.FillAmount, 0.001f);
            Assert.AreSame(skeleton, marker.Target);
            Assert.Less(Vector3.Distance(Flat(marker.transform.position), Flat(skeleton.transform.position)), 0.01f, "il cerchio sotto i suoi piedi");

            skeleton.TakeDamage(new DamageInfo(skeleton.Max * 0.5f, DamageType.Physical, null));
            Assert.AreEqual(0.5f, bar.FillAmount, 0.001f, "metà vita, metà barra");

            skeleton.TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            Assert.IsNull(bar.Shown, "morto, la barra sparisce");
            Assert.IsNull(marker.Target);
        }

        [UnityTest, Description("Senza cursore sopra, la barra mostra il nemico colpito dal cavaliere per 3 s dopo l'ultimo colpo; i colpi di altri non contano; morto, sparisce")]
        public IEnumerator LastHitEnemy_StaysOnTheBar()
        {
            yield return StillSkeleton();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>().GetComponent<Health>();
            var bar = Object.FindFirstObjectByType<EnemyBar>();
            PointAt(Player.position + Vector3.right * 2f);
            yield return null;
            Assert.IsNull(bar.Shown);

            skeleton.TakeDamage(new DamageInfo(1f, DamageType.Physical, null));
            Assert.IsNull(bar.Shown, "un colpo che non è del cavaliere");

            skeleton.TakeDamage(new DamageInfo(1f, DamageType.Fire, Player.gameObject));
            Assert.AreSame(skeleton, bar.Shown, "colpito dal cavaliere, anche senza cursore");
            Assert.AreEqual("Skeleton", bar.NameText);
            yield return Wait(2f);

            // un altro colpo rinnova i 3 s
            skeleton.TakeDamage(new DamageInfo(1f, DamageType.Physical, Player.gameObject));
            yield return Wait(2f);
            Assert.AreSame(skeleton, bar.Shown, "2 s dopo l'ultimo colpo resta");
            yield return Wait(1.5f);
            Assert.IsNull(bar.Shown, "3 s dopo l'ultimo colpo sparisce");

            skeleton.TakeDamage(new DamageInfo(1f, DamageType.Physical, Player.gameObject));
            Assert.AreSame(skeleton, bar.Shown);
            skeleton.TakeDamage(new DamageInfo(1000f, DamageType.Physical, Player.gameObject));
            Assert.IsNull(bar.Shown, "morto, sparisce");
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        [UnityTest, Description("Un click sul pavimento accanto a un nemico, entro 0,6 m, lo colpisce invece di camminare")]
        public IEnumerator ClickNextToEnemy_Attacks()
        {
            yield return StillSkeleton();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();

            // nella sandbox lo scheletro sta dietro un cubo, visto dalla camera: lo si porta allo scoperto
            skeleton.GetComponent<NavMeshAgent>().Warp(new Vector3(6f, 0f, 1f));
            Physics.SyncTransforms();

            // appena fuori dal corpo, dal primo lato in cui sotto il cursore c'è il pavimento e non il
            // nemico o un ostacolo della sandbox
            int mask = LayerMask.GetMask("Ground", "Obstacle", "Enemy", "Interactable");
            Vector3 beside = Vector3.zero;
            bool found = false;
            for (int i = 0; i < 8 && !found; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                beside = skeleton.transform.position + direction * 0.55f;
                var ray = Camera.ScreenPointToRay(Camera.WorldToScreenPoint(beside));
                found = Physics.Raycast(ray, out var hit, 100f, mask, QueryTriggerInteraction.Ignore) && hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground");
            }

            Assert.IsTrue(found, "un punto del pavimento accanto al nemico");

            ClickAt(beside);
            yield return null;
            yield return null;
            Assert.IsTrue(Player.GetComponent<MeleeAttack>().HasTarget, "il click accanto al nemico lo colpisce");
        }

        [Test, Description("Il cavaliere ha la precedenza sugli agent di tutti i nemici: nello sciame non viene spinto")]
        public void Knight_IsNotPushedByEnemies()
        {
            int knight = Load<GameObject>("Assets/_Project/Prefabs/Player.prefab").GetComponent<NavMeshAgent>().avoidancePriority;
            foreach (var name in new[] { "Skeleton", "Swarm", "Brute" })
            {
                var prefab = Load<GameObject>($"Assets/_Project/Prefabs/{name}.prefab");
                int lowest = prefab.GetComponent<NavMeshAgent>().avoidancePriority - prefab.GetComponent<EnemyAI>().Archetype.AvoidanceSpread;
                Assert.Less(knight, lowest, $"{name}: il più importante dei nemici cede comunque il passo al cavaliere");
            }
        }

        [Test, Description("Ogni tipo di nemico ha un nome nella tabella delle stringhe")]
        public void EveryEnemy_HasAName()
        {
            foreach (var name in new[] { "Skeleton", "Swarm", "Brute" })
            {
                var archetype = Load<EnemyArchetype>($"Assets/_Project/Data/Enemies/{name}.asset");
                Assert.IsFalse(string.IsNullOrEmpty(archetype.NameKey), name);
                var table = DarkDescent.Localization.StringTable.Parse(Load<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text);
                Assert.IsTrue(table.Contains(archetype.NameKey), $"{name}: {archetype.NameKey} nella tabella");
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
