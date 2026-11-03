using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Magic;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le abilità del Mago nel gioco (passo 21.4 della M21): il Dardo arcano del sinistro da lontano, il gelo e il
    /// rallentamento dei nemici con le loro regole, il Passo di luce, lo Scudo di mana, la Palla di fuoco, la Meteora, le
    /// aree che restano, l'Armatura di ghiaccio.
    /// </summary>
    public class MageSpellPlayModeTests : SandboxFixture
    {
        private static SpellDefinition Spell(string name) => AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

        private PlayerMagic Magic => Player.GetComponent<PlayerMagic>();

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        // il giocatore a tanti metri dal nemico, con la strada libera
        private void StandAt(Transform enemy, float distance)
        {
            for (int i = 0; i < 16; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward * distance;
                if (NavMesh.SamplePosition(enemy.position + offset, out var hit, 0.5f, NavMesh.AllAreas)
                    && !NavMesh.Raycast(hit.position, enemy.position, out _, NavMesh.AllAreas))
                {
                    PlayerAgent.Warp(hit.position);
                    return;
                }
            }

            Assert.Fail($"nessun punto a {distance} m dal nemico");
        }

        private static EnemyAI Still(EnemyAI enemy)
        {
            enemy.GetComponent<NavMeshAgent>().isStopped = true;
            return enemy;
        }

        private static EnemyAI[] Skeletons(int count) =>
            Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Where(e => e.isActiveAndEnabled).Take(count).Select(Still).ToArray();

        private void FullMana()
        {
            Magic.Mana.Fill();
        }

        // un nemico messo a tanti metri da un punto, sul NavMesh
        private static void PlaceNear(EnemyAI enemy, Vector3 point, float distance)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * distance;
                if (NavMesh.SamplePosition(point + offset, out var hit, 0.5f, NavMesh.AllAreas))
                {
                    enemy.GetComponent<NavMeshAgent>().Warp(hit.position);
                    return;
                }
            }

            Assert.Fail("nessun punto libero vicino");
        }

        [UnityTest, Description("Il Dardo arcano: il Mago ha il Dardo nel sinistro, il colpo parte da 6 m (un colpo di spada non ci arriverebbe), non spende mana e fa danno; tornato Guerriero, il colpo è di nuovo l'Attacco in mischia")]
        public IEnumerator ArcaneBolt_HitsFromAfar()
        {
            yield return LoadSandbox();
            var playerClass = Player.GetComponent<PlayerClass>();
            var attack = Player.GetComponent<MeleeAttack>();
            Assert.IsFalse(attack.IsBolt);
            Assert.AreEqual("Attack", Magic.Book[SpellBook.Primary].name);

            playerClass.Apply(playerClass.Catalog.Find("mage"), "Merlino", true);
            Assert.IsTrue(attack.IsBolt);
            Assert.AreSame(Spell("ArcaneBolt"), Magic.Book[SpellBook.Primary]);
            Assert.AreSame(Spell("ArcaneBolt"), Magic.AttackSpell);
            Assert.IsFalse(Magic.Book.Knows(Spell("Attack")), "l'Attacco è del Guerriero");

            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            var health = skeleton.GetComponent<Health>();
            StandAt(skeleton.transform, 6f);
            yield return null;
            float mana = Magic.Mana.Current;
            int swings = 0;
            attack.SwingStarted += () => swings++;
            attack.SetTarget(health);
            yield return Wait(1.2f);
            Assert.GreaterOrEqual(swings, 1, "il gesto del lancio è partito");
            Assert.Less(health.Current, health.Max, "il dardo è arrivato");
            Assert.AreEqual(mana, Magic.Mana.Current, 0.001f, "senza mana");
            Assert.Greater(FlatDistance(Player.position, skeleton.transform.position), 5f, "da fermo, senza avvicinarsi");

            playerClass.Apply(playerClass.Catalog.Warrior, "", true);
            Assert.IsFalse(attack.IsBolt);
            Assert.AreEqual("Attack", Magic.Book[SpellBook.Primary].name);
            Assert.IsFalse(Magic.Book.Knows(Spell("ArcaneBolt")));
        }

        [UnityTest, Description("Il rallentamento nel gioco: uno scheletro perde il 40% della velocità per i secondi dati, un campione solo il 20%, e alla fine la velocità torna; un secondo colpo più debole non cambia niente; non ha tempo di pausa")]
        public IEnumerator Slow_ByRankAndExpires()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Skeletons(2);
            var agent = enemies[0].GetComponent<NavMeshAgent>();
            float speed = agent.speed;

            Assert.IsTrue(enemies[0].Slow(0.4f, 1f));
            Assert.IsTrue(enemies[0].IsSlowed);
            Assert.AreEqual(speed * 0.6f, agent.speed, 0.001f);
            Assert.IsNotNull(enemies[0].GetComponent<EnemyFrostView>(), "la tinta fredda nasce da sola");
            Assert.IsTrue(enemies[0].Slow(0.2f, 0.5f));
            Assert.AreEqual(speed * 0.6f, agent.speed, 0.001f, "il più debole non cambia niente");

            yield return Wait(1.2f);
            Assert.IsFalse(enemies[0].IsSlowed);
            Assert.AreEqual(speed, agent.speed, 0.001f, "la velocità di prima");
            Assert.IsTrue(enemies[0].Slow(0.4f, 1f), "nessun tempo di pausa");

            var champion = enemies[1].gameObject.AddComponent<SpecialEnemy>();
            champion.Promote(EnemyRank.Champion, new string[0]);
            champion.Bind(AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset"),
                Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());
            Assert.IsTrue(enemies[1].Slow(0.4f, 1f));
            Assert.AreEqual(0.2f, enemies[1].SlowFraction, 0.0001f, "il campione a metà");
        }

        [UnityTest, Description("Il congelamento nel gioco: fermo, senza colpi e con l'animazione ferma; uno scheletro prende tutto, un campione metà, un Inamovibile niente; dopo, 3 s senza congelarsi; separato dallo stordimento")]
        public IEnumerator Freeze_FollowsTheStunRule()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Skeletons(3);

            Assert.IsTrue(enemies[0].Freeze(1f));
            Assert.IsTrue(enemies[0].IsFrozen);
            Assert.IsTrue(enemies[0].IsImmobilized);
            Assert.IsFalse(enemies[0].IsStunned, "non è uno stordimento");
            Assert.IsTrue(enemies[0].GetComponent<MeleeAttack>().IsInterrupted, "congelato non colpisce");
            var view = enemies[0].GetComponent<EnemyFrostView>();
            Assert.IsNotNull(view);
            Assert.IsTrue(view.IsPaused, "l'animazione è ferma");
            Assert.IsFalse(enemies[0].Freeze(1f), "già congelato");
            Assert.IsTrue(enemies[0].Stun(1f), "lo stordimento ha la sua regola a parte");

            yield return Wait(1.2f);
            Assert.IsFalse(enemies[0].IsFrozen);
            Assert.IsFalse(view.IsPaused, "l'animazione riparte");
            Assert.IsFalse(enemies[0].Freeze(1f), "3 s di tregua");

            var champion = enemies[1].gameObject.AddComponent<SpecialEnemy>();
            champion.Promote(EnemyRank.Champion, new string[0]);
            champion.Bind(AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset"),
                Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());
            Assert.IsTrue(enemies[1].Freeze(2f));
            Assert.AreEqual(1f, enemies[1].FreezeRemaining, 0.001f, "il campione a metà");

            var unstoppable = enemies[2].gameObject.AddComponent<SpecialEnemy>();
            unstoppable.Promote(EnemyRank.Champion, new[] { "unstoppable" });
            unstoppable.Bind(AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset"),
                Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());
            Assert.IsFalse(enemies[2].Freeze(2f), "l'Inamovibile mai");
        }

        [UnityTest, Description("Il Dardo di ghiaccio colpisce e rallenta del 40% per 3 s; costa 8 di mana")]
        public IEnumerator IceBolt_SlowsTheTarget()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            var health = skeleton.GetComponent<Health>();
            StandAt(skeleton.transform, 6f);
            yield return null;
            FullMana();
            Assert.IsTrue(Magic.Learn(Spell("IceBolt")));
            float mana = Magic.Mana.Current;
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("IceBolt"), skeleton.transform.position));
            Assert.AreEqual(mana - 8f, Magic.Mana.Current, 0.1f);
            yield return Wait(1.2f);
            Assert.Less(health.Current, health.Max);
            Assert.IsTrue(skeleton.IsSlowed);
            Assert.AreEqual(0.4f, skeleton.SlowFraction, 0.001f);
            Assert.That(skeleton.SlowRemaining, Is.InRange(1.5f, 3f));
        }

        [UnityTest, Description("La Nova di gelo congela entro 3,5 m e non oltre, senza far danno; costa 12 di mana")]
        public IEnumerator FrostNova_FreezesAround()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Skeletons(2);
            StandAt(enemies[0].transform, 2f);
            yield return null;
            PlaceNear(enemies[1], Player.position, 6f);
            yield return null;
            FullMana();
            Assert.IsTrue(Magic.Learn(Spell("FrostNova")));
            float life = enemies[0].GetComponent<Health>().Current;
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("FrostNova"), Player.position));
            yield return Wait(0.4f);
            Assert.IsTrue(enemies[0].IsFrozen, "il vicino");
            Assert.IsFalse(enemies[1].IsFrozen, "il lontano");
            Assert.AreEqual(life, enemies[0].GetComponent<Health>().Current, 0.001f, "senza danno");
            Assert.That(enemies[0].FreezeRemaining, Is.InRange(1f, 1.5f), "1,5 s, meno il tempo passato dallo scoppio");
        }

        [UnityTest, Description("La Palla di fuoco scoppia dove tocca e colpisce tutti entro 3 m, anche chi non ha toccato; chi è oltre no")]
        public IEnumerator Fireball_BurstsOnEveryoneAround()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Skeletons(3);
            StandAt(enemies[0].transform, 6f);
            yield return null;
            PlaceNear(enemies[1], enemies[0].transform.position, 1.5f);
            PlaceNear(enemies[2], enemies[0].transform.position, 8f);
            yield return null;
            FullMana();
            Assert.IsTrue(Magic.Learn(Spell("Fireball")));
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Fireball"), enemies[0].transform.position));
            yield return Wait(1.8f);
            Assert.Less(enemies[0].GetComponent<Health>().Current, enemies[0].GetComponent<Health>().Max, "il bersaglio");
            Assert.Less(enemies[1].GetComponent<Health>().Current, enemies[1].GetComponent<Health>().Max, "chi gli sta accanto");
            Assert.AreEqual(enemies[2].GetComponent<Health>().Max, enemies[2].GetComponent<Health>().Current, "chi è lontano no");
        }

        [UnityTest, Description("Il Passo di luce porta il Mago sul punto indicato fino a 8 m, spende 8 di mana e lo mette di nuovo sul NavMesh; più in là si ferma a 8 m")]
        public IEnumerator Blink_MovesToThePoint()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            StandAt(skeleton.transform, 12f);
            yield return null;
            FullMana();
            Assert.IsTrue(Magic.Learn(Spell("Blink")));
            Vector3 from = Player.position;
            float mana = Magic.Mana.Current;

            // il punto verso il nemico, lontano: a 8 m al massimo
            Vector3 toward = (skeleton.transform.position - from).normalized;
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Blink"), from + toward * 20f));
            yield return null;
            Assert.AreEqual(mana - 8f, Magic.Mana.Current, 0.1f);
            Assert.That(FlatDistance(from, Player.position), Is.InRange(0.5f, 8.2f), "al massimo la gittata");
            Assert.IsTrue(PlayerAgent.isOnNavMesh);
            Assert.Less(FlatDistance(Player.position, skeleton.transform.position), FlatDistance(from, skeleton.transform.position), "verso il nemico");
        }

        [UnityTest, Description("Lo Scudo di mana: il danno preso scala 2 di mana per punto e la vita non si muove; a mana quasi finito ne passa una parte alla vita; finito il tempo, il danno torna alla vita")]
        public IEnumerator ManaShield_PaysWithMana()
        {
            yield return LoadSandbox();
            FullMana();
            Assert.IsTrue(Magic.Learn(Spell("ManaShield")));
            var health = Player.GetComponent<Health>();
            var abilities = Player.GetComponent<MageAbilities>();
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("ManaShield"), Player.position));
            yield return null;
            Assert.That(abilities.ShieldRemaining, Is.InRange(11f, 12f));
            Assert.IsTrue(Player.GetComponentsInChildren<AuraEffect>().Any(a => a.IsPlaying), "l'alone si accende");

            float life = health.Current;
            float mana = Magic.Mana.Current;
            health.TakeDamage(new DamageInfo(5f, DamageType.Physical, null));
            Assert.AreEqual(life, health.Current, 0.001f, "la vita non si muove");
            Assert.AreEqual(mana - 10f, Magic.Mana.Current, 0.05f, "2 di mana per punto");

            // il mana che resta copre solo una parte: 3 di mana coprono 1,5 punti, gli altri 3,5 vanno alla vita
            Magic.Mana.TrySpend(Magic.Mana.Current - 3f);
            life = health.Current;
            health.TakeDamage(new DamageInfo(5f, DamageType.Physical, null));
            Assert.AreEqual(life - 3.5f, health.Current, 0.05f);
            Assert.AreEqual(0f, Magic.Mana.Current, 0.05f);

            yield return Wait(12.5f);
            Assert.AreEqual(0f, abilities.ShieldRemaining);
            Magic.Mana.Fill();
            life = health.Current;
            health.TakeDamage(new DamageInfo(5f, DamageType.Physical, null));
            Assert.Less(health.Current, life, "senza lo scudo il danno è della vita");
        }

        [UnityTest, Description("La Meteora cade dopo 1,2 s: prima niente, poi colpisce tutti entro 4 m dal punto, e chi sta fuori no; costa 30 di mana")]
        public IEnumerator Meteor_FallsAfterAMoment()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Skeletons(2);
            StandAt(enemies[0].transform, 8f);
            yield return null;
            PlaceNear(enemies[1], enemies[0].transform.position, 9f);
            yield return null;
            FullMana();
            Assert.IsTrue(Magic.Learn(Spell("Meteor")));
            var abilities = Player.GetComponent<MageAbilities>();
            float mana = Magic.Mana.Current;
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Meteor"), enemies[0].transform.position));
            Assert.AreEqual(mana - 30f, Magic.Mana.Current, 0.1f);
            Assert.AreEqual(1, abilities.PendingMeteors);
            yield return Wait(0.6f);
            Assert.AreEqual(enemies[0].GetComponent<Health>().Max, enemies[0].GetComponent<Health>().Current, "ancora in aria");
            yield return Wait(1f);
            Assert.AreEqual(0, abilities.PendingMeteors);
            Assert.Less(enemies[0].GetComponent<Health>().Current, enemies[0].GetComponent<Health>().Max, "è caduta");
            Assert.AreEqual(enemies[1].GetComponent<Health>().Max, enemies[1].GetComponent<Health>().Current, "chi è fuori no");
        }

        [UnityTest, Description("Il Muro di fiamme resta 6 s e brucia a ogni battito chi sta nella riga; poi si spegne e il danno si ferma")]
        public IEnumerator FlameWall_BurnsWhoStandsInIt()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            var health = skeleton.GetComponent<Health>();
            StandAt(skeleton.transform, 6f);
            yield return null;
            FullMana();
            Assert.IsTrue(Magic.Learn(Spell("FlameWall")));
            var abilities = Player.GetComponent<MageAbilities>();
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("FlameWall"), skeleton.transform.position));
            Assert.AreEqual(1, abilities.ActiveZones);
            yield return Wait(1.6f);
            float after = health.Current;
            Assert.Less(after, health.Max, "brucia");

            yield return Wait(1f);
            Assert.Less(health.Current, after, "e continua a ogni battito");
            yield return Wait(5f);
            Assert.AreEqual(0, abilities.ActiveZones, "spento dopo 6 s");
            float end = health.Current;
            yield return Wait(1f);
            Assert.AreEqual(end, health.Current, 0.001f, "e il danno si ferma");
        }

        [UnityTest, Description("La Tempesta di neve rallenta del 50% chi sta nell'area e lo colpisce; chi esce torna alla velocità di prima entro il secondo di rallentamento")]
        public IEnumerator Blizzard_SlowsAndStrikes()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            var health = skeleton.GetComponent<Health>();
            StandAt(skeleton.transform, 6f);
            yield return null;
            FullMana();
            Assert.IsTrue(Magic.Learn(Spell("Blizzard")));
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Blizzard"), skeleton.transform.position));
            yield return Wait(1.2f);
            Assert.IsTrue(skeleton.IsSlowed);
            Assert.AreEqual(0.5f, skeleton.SlowFraction, 0.001f);
            Assert.Less(health.Current, health.Max);
        }

        [UnityTest, Description("L'Armatura di ghiaccio alza l'armatura del 40% e rallenta del 30% chi colpisce di mischia mentre dura; finita, l'armatura torna")]
        public IEnumerator FrostArmor_SlowsMeleeAttackers()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            StandAt(skeleton.transform, 1.5f);
            yield return null;
            FullMana();
            Assert.IsTrue(Magic.Learn(Spell("FrostArmor")));
            var sheet = Player.GetComponent<CharacterStats>().Sheet;

            // il 40% di un'armatura che c'è: senza, non c'è niente da aumentare
            sheet.SetBase(StatType.Armor, 20f);
            float armor = sheet.Get(StatType.Armor);
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("FrostArmor"), Player.position));
            yield return Wait(0.5f);
            Assert.Greater(sheet.Get(StatType.Armor), armor, "armatura in più");

            // un colpo di mischia dallo scheletro accanto
            Player.GetComponent<Health>().TakeDamage(new DamageInfo(1f, DamageType.Physical, skeleton.gameObject));
            Assert.IsTrue(skeleton.IsSlowed);
            Assert.AreEqual(0.3f, skeleton.SlowFraction, 0.001f);
        }
    }
}
