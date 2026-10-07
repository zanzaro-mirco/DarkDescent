using System.Collections;
using System.Collections.Generic;
using DarkDescent.Audio;
using DarkDescent.Characters;
using DarkDescent.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class SfxLimiterTests : SandboxFixture
    {
        private readonly List<(SfxKind kind, bool allowed)> _requests = new List<(SfxKind, bool)>();
        private SfxLimiter _limiter;

        private void Record(SfxKind kind, bool allowed)
        {
            _requests.Add((kind, allowed));
        }

        [UnityTearDown]
        public IEnumerator Unsubscribe()
        {
            if (_limiter != null)
            {
                _limiter.Requested -= Record;
            }

            yield return null;
        }

        // dieci dello sciame attorno al cavaliere, collegati al limite come quelli di un livello
        private List<GameObject> SpawnSwarm(int count)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Swarm.prefab");
            var swarm = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                var position = Player.position + Quaternion.Euler(0f, i * 360f / count, 0f) * Vector3.forward * (1.5f + i * 0.3f);
                var go = Object.Instantiate(prefab, position, Quaternion.identity);
                go.GetComponent<CharacterAudio>().Bind(_limiter);
                swarm.Add(go);
            }

            return swarm;
        }

        private IEnumerator Load()
        {
            yield return LoadSandbox();
            _limiter = Object.FindFirstObjectByType<SfxLimiter>();
            Assert.IsNotNull(_limiter, "il limite di voci è in Core");
            _requests.Clear();
            _limiter.Requested += Record;
        }

        [UnityTest, Description("Dieci colpi dello sciame nello stesso fotogramma: dieci richieste, un impatto solo")]
        public IEnumerator TenHitsInOneFrame_SoundOnce()
        {
            yield return Load();
            var swarm = SpawnSwarm(10);
            yield return null;
            _requests.Clear();

            foreach (var go in swarm)
            {
                go.GetComponent<Health>().TakeDamage(new DamageInfo(1f, DamageType.Physical, Player.gameObject));
            }

            Assert.AreEqual(10, _requests.FindAll(r => r.kind == SfxKind.Hit).Count, "ognuno ha chiesto");
            Assert.AreEqual(1, _requests.FindAll(r => r.kind == SfxKind.Hit && r.allowed).Count, "uno solo suona");

            // il fotogramma dopo un altro impatto passa
            yield return null;
            _requests.Clear();
            swarm[3].GetComponent<Health>().TakeDamage(new DamageInfo(1f, DamageType.Physical, Player.gameObject));
            Assert.IsTrue(_requests.Exists(r => r.kind == SfxKind.Hit && r.allowed));
        }

        [UnityTest, Description("Il cavaliere è collegato al limite; la sorgente di un suono vicino ha la priorità più alta di uno lontano")]
        public IEnumerator Priority_FollowsTheDistance()
        {
            yield return Load();
            var swarm = SpawnSwarm(10);
            yield return null;
            _requests.Clear();

            // impatto e verso da vicino, morte da lontano: tipi diversi, passano tutti e tre; l'impatto
            // del lontano, nello stesso fotogramma, no
            var near = swarm[0];
            var far = swarm[9];
            near.GetComponent<Health>().TakeDamage(new DamageInfo(1f, DamageType.Physical, Player.gameObject));
            far.GetComponent<Health>().TakeDamage(new DamageInfo(100f, DamageType.Physical, Player.gameObject));
            Assert.AreEqual(3, _requests.FindAll(r => r.allowed).Count);
            Assert.Less(near.GetComponent<AudioSource>().priority, far.GetComponent<AudioSource>().priority);

            yield return null;
            _requests.Clear();
            Player.GetComponent<Health>().TakeDamage(new DamageInfo(1f, DamageType.Physical, near));
            Assert.IsTrue(_requests.Exists(r => r.kind == SfxKind.Hit), "anche il cavaliere chiede il permesso");
        }
    }
}
