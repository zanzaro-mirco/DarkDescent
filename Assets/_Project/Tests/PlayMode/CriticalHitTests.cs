using System.Collections;
using System.Linq;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class CriticalHitTests : SandboxFixture
    {
        private static T Load<T>(string path) where T : Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static bool CanCrit(string prefab)
        {
            var attack = Load<GameObject>($"Assets/_Project/Prefabs/{prefab}.prefab").GetComponent<MeleeAttack>();
            return new UnityEditor.SerializedObject(attack).FindProperty("_canCrit").boolValue;
        }

        private static EnemyArchetype Archetype(string name)
        {
            return Load<EnemyArchetype>($"Assets/_Project/Data/Enemies/{name}.asset");
        }

        [Test, Description("Solo il cavaliere fa critici (D13); ogni tipo di nemico ha i suoi versi, bassi per il bruto e acuti per lo sciame")]
        public void OnlyTheKnightCrits_EachEnemyHasItsVoice()
        {
            Assert.IsTrue(CanCrit("Player"));
            foreach (var enemy in new[] { "Skeleton", "Swarm", "Brute" })
            {
                Assert.IsFalse(CanCrit(enemy), $"{enemy} non fa critici");
                var archetype = Archetype(enemy);
                Assert.Greater(archetype.CriticalVoiceCount, 0, $"{enemy}: i versi del critico");
                for (int i = 0; i < archetype.CriticalVoiceCount; i++)
                {
                    Assert.IsNotNull(archetype.GetCriticalVoice(i), $"{enemy}: verso {i}");
                }
            }

            Assert.Less(Archetype("Brute").CriticalPitch.y, Archetype("Skeleton").CriticalPitch.x, "il bruto più basso dello scheletro");
            Assert.Less(Archetype("Skeleton").CriticalPitch.y, Archetype("Swarm").CriticalPitch.x, "lo sciame più acuto");
        }

        [UnityTest, Description("Un critico sullo scheletro: danno doppio, il suo verso al posto dell'impatto, numero grande con il punto esclamativo, hit stop più lungo; il pannello mostra il 7%")]
        public IEnumerator Critical_OnTheSkeleton()
        {
            yield return LoadSandbox();

            // il cavaliere: colpisce, danno minimo, critico (lo scheletro non ha scudo, quindi niente tiro
            // di blocco). Lo scheletro manca sempre, così i suoi tiri non spostano quelli del cavaliere
            UseRandom(0.0, 0.0, 0.99);
            var skeleton = GameObject.Find("Skeleton");
            skeleton.GetComponent<MeleeAttack>().SetRandomSource(new FixedRandomSource(0.99));
            var health = skeleton.GetComponent<Health>();
            var attack = Player.GetComponent<MeleeAttack>();
            float expected = 2f * MinHitDamage(attack);

            DamageInfo landed = default;
            float applied = 0f;
            bool damaged = false;
            void OnDamaged(DamageInfo info, float amount)
            {
                landed = info;
                applied = amount;
                damaged = true;
            }

            AudioClip voice = null;
            void OnVoice(AudioClip clip) => voice = clip;

            var audio = skeleton.GetComponent<CharacterAudio>();
            health.Damaged += OnDamaged;
            audio.CriticalVoicePlayed += OnVoice;
            try
            {
                for (float time = 0f; !damaged; time += Time.unscaledDeltaTime)
                {
                    Assert.Less(time, 6f, "il colpo non è arrivato");
                    attack.SetTarget(health);
                    yield return null;
                }

                Assert.IsTrue(landed.IsCritical);
                Assert.AreEqual(expected, applied, 0.001f, "danno doppio");

                var archetype = skeleton.GetComponent<EnemyAI>().Archetype;
                Assert.IsNotNull(voice, "il verso del critico");
                Assert.IsTrue(Enumerable.Range(0, archetype.CriticalVoiceCount).Any(i => archetype.GetCriticalVoice(i) == voice));
                Assert.That(skeleton.GetComponent<CharacterAudio>().VoiceSource.pitch, Is.InRange(archetype.CriticalPitch.x, archetype.CriticalPitch.y));

                var number = Object.FindObjectsByType<DamageNumber>(FindObjectsSortMode.None)
                    .FirstOrDefault(n => n.GetComponent<TMP_Text>().text == $"{Mathf.RoundToInt(expected)}!");
                Assert.IsNotNull(number, "il numero con il punto esclamativo");
                Assert.Greater(number.transform.localScale.x, 1.2f, "più grande");

                var hitStop = Object.FindFirstObjectByType<HitStop>();
                yield return new WaitForSecondsRealtime(0.08f);
                Assert.IsTrue(hitStop.IsActive, "il critico ferma il tempo più a lungo di un colpo normale (0,05 s)");
            }
            finally
            {
                health.Damaged -= OnDamaged;
                audio.CriticalVoicePlayed -= OnVoice;
            }

            StringAssert.Contains("%\n7%\n", Object.FindFirstObjectByType<CharacterPanel>().ValuesText, "critico 7% con la Destrezza 20");
        }

        [Test, Description("Ogni tipo di nemico ha anche i versi di quando viene colpito (prova della M7): più piano del critico, bassi per il bruto e acuti per lo sciame")]
        public void EachEnemyHasItsHurtVoices()
        {
            foreach (var enemy in new[] { "Skeleton", "Swarm", "Brute" })
            {
                var archetype = Archetype(enemy);
                Assert.Greater(archetype.HurtVoiceCount, 0, $"{enemy}: i versi quando viene colpito");
                for (int i = 0; i < archetype.HurtVoiceCount; i++)
                {
                    Assert.IsNotNull(archetype.GetHurtVoice(i), $"{enemy}: verso {i}");
                    Assert.Less(archetype.GetHurtVoice(i).length, 0.8f, $"{enemy}: verso {i} breve, arriva a ogni colpo");
                }

                Assert.Less(archetype.HurtVolume, 1f, $"{enemy}: più piano del critico");
            }

            Assert.Less(Archetype("Brute").HurtPitch.y, Archetype("Skeleton").HurtPitch.x, "il bruto più basso dello scheletro");
            Assert.Less(Archetype("Skeleton").HurtPitch.y, Archetype("Swarm").HurtPitch.x, "lo sciame più acuto");
        }

        [UnityTest, Description("Un colpo normale sullo scheletro: insieme all'impatto il suo verso, su una sorgente sua; nessun verso del critico")]
        public IEnumerator NormalHit_PlaysTheHurtVoice()
        {
            yield return LoadSandbox();

            // colpisce, danno minimo, niente critico (tiro basso)
            UseRandom(0.0);
            var skeleton = GameObject.Find("Skeleton");
            skeleton.GetComponent<MeleeAttack>().SetRandomSource(new FixedRandomSource(0.99));
            var health = skeleton.GetComponent<Health>();
            var attack = Player.GetComponent<MeleeAttack>();
            var audio = skeleton.GetComponent<CharacterAudio>();

            AudioClip hurt = null;
            AudioClip critical = null;
            void OnHurt(AudioClip clip) => hurt = clip;
            void OnCritical(AudioClip clip) => critical = clip;
            audio.HurtVoicePlayed += OnHurt;
            audio.CriticalVoicePlayed += OnCritical;
            try
            {
                for (float time = 0f; health.Current >= health.Max; time += Time.unscaledDeltaTime)
                {
                    Assert.Less(time, 6f, "il colpo non è arrivato");
                    attack.SetTarget(health);
                    yield return null;
                }

                var archetype = skeleton.GetComponent<EnemyAI>().Archetype;
                Assert.IsNotNull(hurt, "il verso del colpo");
                Assert.IsTrue(Enumerable.Range(0, archetype.HurtVoiceCount).Any(i => archetype.GetHurtVoice(i) == hurt));
                Assert.That(audio.VoiceSource.pitch, Is.InRange(archetype.HurtPitch.x, archetype.HurtPitch.y));
                Assert.AreNotSame(skeleton.GetComponent<AudioSource>(), audio.VoiceSource, "l'intonazione del verso non tocca l'impatto");
                Assert.IsNull(critical, "non è un critico");
            }
            finally
            {
                audio.HurtVoicePlayed -= OnHurt;
                audio.CriticalVoicePlayed -= OnCritical;
            }
        }
    }
}
