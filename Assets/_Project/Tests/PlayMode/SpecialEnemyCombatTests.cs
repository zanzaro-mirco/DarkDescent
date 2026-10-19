using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class SpecialEnemyCombatTests : SandboxFixture
    {
        private static ChampionSettings Settings => AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");

        private static DamageInfo Damage(float amount, GameObject source = null)
        {
            return new DamageInfo(amount, DamageType.Physical, source);
        }

        // lo scheletro della sandbox, fatto campione con questi modificatori e legato come fa il composition root
        private SpecialEnemy MakeChampion(params string[] modifiers)
        {
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            var special = skeleton.gameObject.AddComponent<SpecialEnemy>();
            special.Promote(EnemyRank.Champion, modifiers);
            special.Bind(Settings, Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());
            return special;
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        [UnityTest, Description("Un campione ha il triplo della vita, a vita piena, colpisce più forte, è più grande, ha l'anello blu sotto i piedi e dà quattro volte l'esperienza; morto, l'anello si spegne")]
        public IEnumerator Champion_IsStrongerAndShowsIt()
        {
            yield return LoadSandbox();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            var health = skeleton.GetComponent<Health>();
            float life = health.Max;
            Vector3 scale = skeleton.transform.localScale;
            var progress = Player.GetComponent<PlayerProgress>();
            int depth = Object.FindFirstObjectByType<Levels.LevelManager>().CurrentLevel.Depth;

            var special = MakeChampion();
            Assert.AreEqual(life * 3f, health.Max, 0.01f);
            Assert.AreEqual(health.Max, health.Current, 0.01f, "a vita piena");
            Assert.AreEqual(1.5f, skeleton.GetComponent<MeleeAttack>().DamageMultiplier, 0.001f);
            Assert.AreEqual(scale.x * 1.15f, skeleton.transform.localScale.x, 0.001f);
            Assert.IsNotNull(special.Ring);
            Assert.IsTrue(special.Ring.activeInHierarchy);

            special.Bind(Settings, Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());
            Assert.AreEqual(life * 3f, health.Max, 0.01f, "legato due volte, i numeri non si moltiplicano due volte");

            int expected = progress.Settings.ExperienceForKill(skeleton.Archetype.Experience * 4, depth, 1);
            health.TakeDamage(Damage(10000f));
            yield return null;
            // quattro volte uno scheletro bastano a salire al livello 2: il resto è l'esperienza del livello nuovo
            int toNext = progress.Settings.ExperienceToNext(1);
            Assert.AreEqual(expected >= toNext ? 2 : 1, progress.Progress.Level);
            Assert.AreEqual(expected >= toNext ? expected - toNext : expected, progress.Progress.Experience);
            Assert.IsFalse(special.Ring.activeSelf, "l'anello si spegne con lui");
        }

        [UnityTest, Description("Rapido: si muove e colpisce il 50% più in fretta, con le animazioni alla stessa velocità, e alza la polvere (v1.1.1)")]
        public IEnumerator Fast_MovesAndStrikesQuicker()
        {
            yield return LoadSandbox();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            float speed = skeleton.GetComponent<NavMeshAgent>().speed;
            var attack = skeleton.GetComponent<MeleeAttack>();
            float delay = attack.SwingHitDelay;

            var special = MakeChampion("fast");
            Assert.AreEqual(speed * 1.5f, skeleton.GetComponent<NavMeshAgent>().speed, 0.001f);
            Assert.AreEqual(delay / 1.5f, attack.SwingHitDelay, 0.001f);
            Assert.AreEqual(1.5f, skeleton.GetComponentInChildren<Animator>().speed, 0.001f, "le animazioni vanno con lui");
            Assert.IsNotNull(skeleton.transform.Find("Fx_Dust"), "la polvere");
        }

        [UnityTest, Description("Gelido: un suo colpo rallenta il cavaliere del 50% per 3 secondi con i cristalli di ghiaccio, un secondo colpo rinnova senza sommare, poi la velocità torna e i cristalli si spengono; i colpi di altri no; attorno al campione la brina (v1.1.1)")]
        public IEnumerator Frozen_SlowsTheKnight()
        {
            yield return LoadSandbox();
            var special = MakeChampion("frozen");
            // fermo: i colpi li decide il test, non lo scheletro che si avvicina
            special.GetComponent<EnemyAI>().enabled = false;
            special.GetComponent<MeleeAttack>().enabled = false;
            var playerHealth = Player.GetComponent<Health>();
            var slow = Player.GetComponent<MovementSlow>();
            float speed = PlayerAgent.speed;

            playerHealth.TakeDamage(Damage(1f));
            Assert.IsFalse(slow.IsSlowed, "un colpo che non è suo");

            Assert.IsNotNull(special.transform.Find("Fx_Frost"), "la brina attorno al campione");
            Assert.IsNotNull(slow.Effect);
            Assert.IsFalse(slow.Effect.activeSelf);

            playerHealth.TakeDamage(Damage(1f, special.gameObject));
            Assert.IsTrue(slow.IsSlowed);
            Assert.IsTrue(slow.Effect.activeSelf, "i cristalli sul cavaliere");
            Assert.AreEqual(speed * 0.5f, PlayerAgent.speed, 0.001f);
            playerHealth.TakeDamage(Damage(1f, special.gameObject));
            Assert.AreEqual(speed * 0.5f, PlayerAgent.speed, 0.001f, "non si somma");

            Time.timeScale = 4f;
            yield return new WaitForSeconds(3.2f);
            Assert.IsFalse(slow.IsSlowed);
            Assert.IsFalse(slow.Effect.activeSelf);
            Assert.AreEqual(speed, PlayerAgent.speed, 0.001f);
        }

        [UnityTest, Description("Rigenerante: dopo un secondo senza colpi riprende il 3% della vita al secondo, con il bagliore verde e il numero verde (v1.1.1)")]
        public IEnumerator Regenerating_HealsAfterAPause()
        {
            yield return LoadSandbox();
            var special = MakeChampion("regenerating");
            // fermo: la pausa la decidono i colpi del test
            special.GetComponent<EnemyAI>().enabled = false;
            var health = special.GetComponent<Health>();
            var glow = special.transform.Find("Fx_Regen").GetComponent<ParticleSystem>();
            health.TakeDamage(Damage(health.Max * 0.5f));
            float wounded = health.Current;

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(wounded, health.Current, 0.01f, "ferma subito dopo il colpo");
            // il numero del colpo se n'è andato prima della vita ripresa
            var numbers = Object.FindFirstObjectByType<DamageNumbers>();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(wounded, health.Current, 0.01f);
            Assert.AreEqual(0, numbers.ActiveCount);
            for (float t = 0f; health.Current <= wounded; t += Time.deltaTime)
            {
                Assert.Less(t, 3f, "non rigenera");
                yield return null;
            }

            Assert.AreEqual(wounded + health.Max * 0.03f, health.Current, 0.01f, "il 3% in un colpo");
            Assert.IsTrue(glow.isPlaying, "il bagliore verde");
            Assert.AreEqual(1, numbers.ActiveCount, "il numero verde");
        }

        [UnityTest, Description("Inamovibile: i colpi non lo fanno barcollare e lo scudo del cavaliere non ne ferma i colpi; colpito, fa scintille (v1.1.1)")]
        public IEnumerator Unstoppable_IgnoresStaggerAndBlock()
        {
            yield return LoadSandbox();
            var special = MakeChampion("unstoppable");
            Assert.IsFalse(special.GetComponent<HitRecovery>().enabled);
            Assert.IsTrue(special.GetComponent<MeleeAttack>().IsUnblockable);
            var sparks = special.transform.Find("Fx_Sparks").GetComponent<ParticleSystem>();
            Assert.IsFalse(sparks.isPlaying);
            special.GetComponent<Health>().TakeDamage(Damage(1f));
            Assert.IsTrue(sparks.isPlaying, "le scintille");
        }

        [UnityTest, Description("Lo scudo che avrebbe fermato un colpo imparabile lo dice: la scritta Imparabile! sopra il cavaliere, senza bloccare (v1.1.1)")]
        public IEnumerator Shield_SaysWhenPierced()
        {
            yield return LoadSandbox();
            var block = Player.GetComponent<ShieldBlock>();
            block.SetShield(true, 100);
            int pierced = 0;
            int blocked = 0;
            block.Pierced += _ => pierced++;
            block.Blocked += _ => blocked++;
            int numbers = Object.FindFirstObjectByType<DamageNumbers>().ActiveCount;
            // un seme il cui primo tiro lo scudo lo avrebbe fermato
            ulong seed = 1UL;
            while (new SplitMix64Source(seed).NextDouble() * 100.0 >= block.BlockChance)
            {
                seed++;
            }

            block.RollPierced(Damage(0f), new SplitMix64Source(seed));
            Assert.AreEqual(1, pierced, "lo scudo l'avrebbe fermato");
            Assert.AreEqual(0, blocked, "il colpo non è bloccato");
            Assert.Greater(Object.FindFirstObjectByType<DamageNumbers>().ActiveCount, numbers, "la scritta");
        }

        [UnityTest, Description("Furioso: sotto metà della vita il danno sale di metà e la velocità di un quarto, una volta sola; diventa più grande e rosso, con l'aura (v1.1.1)")]
        public IEnumerator Enraged_HitsHarderWhenWounded()
        {
            yield return LoadSandbox();
            var special = MakeChampion("enraged");
            var health = special.GetComponent<Health>();
            var attack = special.GetComponent<MeleeAttack>();

            health.TakeDamage(Damage(health.Max * 0.3f));
            Assert.IsFalse(special.IsEnraged);
            Assert.AreEqual(1.5f, attack.DamageMultiplier, 0.001f);

            Vector3 scale = special.transform.localScale;
            health.TakeDamage(Damage(health.Max * 0.3f));
            Assert.IsTrue(special.IsEnraged);
            Assert.AreEqual(1.5f * 1.5f, attack.DamageMultiplier, 0.001f);
            Assert.AreEqual(1.25f, special.SpeedFactor, 0.001f);
            Assert.Greater(special.transform.localScale.x, scale.x, "più grande");
            Assert.IsNotNull(special.transform.Find("Fx_Rage"), "l'aura rossa");
            health.TakeDamage(Damage(1f));
            Assert.AreEqual(1.5f * 1.5f, attack.DamageMultiplier, 0.001f, "non sale di nuovo");
        }
    }
}
