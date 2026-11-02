using System.Collections;
using System.Linq;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Magic;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le abilità del Guerriero nel gioco (passo 20.4 della M20): il Colpo possente colpisce chi è a
    /// portata e senza nessuno non spende il mana; la Carica e il Colpo di scudo stordiscono; lo
    /// stordimento segue la sua regola; il Turbine prende tutti; il Grido alza il danno; i passivi
    /// che scattano al colpo e alle condizioni.
    /// </summary>
    public class WarriorAbilityTests : SandboxFixture
    {
        private static SpellDefinition Spell(string name) => AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

        private static ChampionSettings Settings => AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");

        private PlayerMagic Magic => Player.GetComponent<PlayerMagic>();

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        // il cavaliere a tanti metri dal nemico, con la strada libera
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

        // il nemico fermo, che non cammina via durante il test
        private static EnemyAI Still(EnemyAI enemy)
        {
            enemy.GetComponent<NavMeshAgent>().isStopped = true;
            return enemy;
        }

        [UnityTest, Description("Il Colpo possente: lontano dai nemici non parte e non spende niente; a portata spende 4 di mana, fa il gesto e dopo la lama il nemico perde vita, con un colpo a segno")]
        public IEnumerator PowerStrike_HitsTheEnemyInReach()
        {
            yield return LoadSandbox();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            var health = skeleton.GetComponent<Health>();
            Assert.AreSame(Spell("PowerStrike"), Magic.Book[SpellBook.Secondary]);

            StandAt(skeleton.transform, 9f);
            yield return null;
            Assert.AreEqual(CastResult.NoTarget, Magic.TryCastAt(Spell("PowerStrike"), Player.position + Player.forward * 2f));
            Assert.AreEqual(30f, Magic.Mana.Current, 0.01f, "senza bersaglio niente mana");
            Assert.AreEqual(CastResult.Ready, Magic.Caster.Check(1), "e niente ricarica");

            StandAt(skeleton.transform, 1.6f);
            yield return null;
            int landed = 0;
            Player.GetComponent<MeleeAttack>().HitLanded += _ => landed++;
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("PowerStrike"), skeleton.transform.position));
            Assert.AreEqual(26f, Magic.Mana.Current, 0.2f);
            Assert.AreEqual(CastResult.Cooldown, Magic.Caster.Check(1));
            yield return Wait(0.5f);
            Assert.Less(health.Current, health.Max, "il colpo è arrivato");
            Assert.GreaterOrEqual(landed, 1);
        }

        [UnityTest, Description("La Carica corre sul nemico a 6 m, lo colpisce e lo stordisce per 1 s; finito lo stordimento, per 3 s non se ne prende un altro")]
        public IEnumerator Charge_RunsAndStuns()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            var health = skeleton.GetComponent<Health>();
            StandAt(skeleton.transform, 6f);
            yield return null;
            Assert.IsTrue(Magic.Learn(Spell("Charge")));
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Charge"), skeleton.transform.position));
            for (float t = 0f; !skeleton.IsStunned; t += Time.deltaTime)
            {
                Assert.Less(t, 2.5f, "la Carica non è arrivata");
                yield return null;
            }

            Assert.Less(FlatDistance(Player.position, skeleton.transform.position), 2.5f, "è arrivato addosso");
            Assert.Less(health.Current, health.Max);
            Assert.That(skeleton.StunRemaining, Is.InRange(0.8f, 1f), "uno scheletro normale, 1 s intero");
            Assert.AreEqual(1f, Player.GetComponent<MovementSlow>().AbilityFactor, "finita la corsa, la velocità di sempre");

            yield return Wait(1.1f);
            Assert.IsFalse(skeleton.IsStunned);
            Assert.IsFalse(skeleton.Stun(1f), "3 s di tregua");
        }

        [UnityTest, Description("Il Colpo di scudo vuole lo scudo: senza non parte e non spende; con lo scudo colpisce e stordisce per 1,5 s")]
        public IEnumerator ShieldBash_NeedsTheShieldAndStuns()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            var block = Player.GetComponent<ShieldBlock>();
            StandAt(skeleton.transform, 1.6f);
            yield return null;
            Assert.IsTrue(Magic.Learn(Spell("ShieldBash")));
            block.SetShield(false, 0);
            Assert.AreEqual(CastResult.NoShield, Magic.TryCastAt(Spell("ShieldBash"), skeleton.transform.position));
            Assert.AreEqual(30f, Magic.Mana.Current, 0.01f);

            block.SetShield(true, 10);
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("ShieldBash"), skeleton.transform.position));
            yield return Wait(0.5f);
            Assert.IsTrue(skeleton.IsStunned);
            Assert.That(skeleton.StunRemaining, Is.InRange(1.1f, 1.5f));
        }

        [UnityTest, Description("La regola dello stordimento nel gioco: uno scheletro prende tutti i secondi, un campione metà, un Inamovibile niente; stordito non colpisce")]
        public IEnumerator StunRule_ByRank()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Where(e => e.isActiveAndEnabled).Take(3).ToArray();
            Assert.AreEqual(3, enemies.Length);

            Assert.IsTrue(enemies[0].Stun(2f));
            Assert.AreEqual(2f, enemies[0].StunRemaining, 0.001f);
            Assert.IsTrue(enemies[0].GetComponent<MeleeAttack>().IsInterrupted, "stordito non colpisce");

            var champion = enemies[1].gameObject.AddComponent<SpecialEnemy>();
            champion.Promote(EnemyRank.Champion, new string[0]);
            champion.Bind(Settings, Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());
            Assert.IsTrue(enemies[1].Stun(2f));
            Assert.AreEqual(1f, enemies[1].StunRemaining, 0.001f, "il campione a metà");

            var unstoppable = enemies[2].gameObject.AddComponent<SpecialEnemy>();
            unstoppable.Promote(EnemyRank.Champion, new[] { "unstoppable" });
            unstoppable.Bind(Settings, Player.GetComponent<Health>(), Player.GetComponent<MovementSlow>());
            Assert.IsFalse(enemies[2].Stun(2f), "l'Inamovibile mai");
            Assert.IsFalse(enemies[2].IsStunned);
        }

        [UnityTest, Description("Il Turbine colpisce tutti i nemici entro 3 m, una volta ciascuno, e nessuno più lontano")]
        public IEnumerator Whirlwind_HitsEveryoneAround()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Where(e => e.isActiveAndEnabled).Take(3).Select(Still).ToArray();
            Assert.AreEqual(3, enemies.Length);
            StandAt(enemies[0].transform, 1.5f);
            yield return null;
            enemies[1].GetComponent<NavMeshAgent>().Warp(Around(1.8f, 120f));
            enemies[2].GetComponent<NavMeshAgent>().Warp(Around(7f, 240f));
            // i collider spostati li vede la fisica al suo passo
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(Magic.Learn(Spell("Whirlwind")));
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Whirlwind"), Player.position));
            yield return null;
            var healths = enemies.Select(e => e.GetComponent<Health>()).ToArray();
            Assert.Less(healths[0].Current, healths[0].Max);
            Assert.Less(healths[1].Current, healths[1].Max);
            Assert.AreEqual(healths[2].Max, healths[2].Current, "a 7 m no");
        }

        [UnityTest, Description("Le due mani (D5): lo spadone in mano fa valere la condizione dei passivi, il Turbine arriva mezzo metro più in là, e l'animazione d'attacco è quella a due mani; con l'arma a una mano tutto torna com'era")]
        public IEnumerator TwoHanded_WidensTheWhirlwindAndTurnsOnTheCondition()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemy = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Where(e => e.isActiveAndEnabled).Select(Still).First();
            var inventory = Player.GetComponent<PlayerInventory>();
            var talents = Player.GetComponent<PlayerTalents>();
            var health = enemy.GetComponent<Health>();
            health.MultiplyMax(50f); // vita alta: il Turbine non deve ucciderlo, perché un morto non si cura
            health.Heal(100000f);
            Assert.IsFalse(talents.Holds(TalentCondition.TwoHanded));

            // lo scudo va via, lo spadone entra; la Forza non manca
            Player.GetComponent<CharacterStats>().Sheet.SetBase(StatType.Strength, 60f);
            inventory.Equipment.Unequip(EquipSlot.Offhand);
            var sword = new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Greatsword.asset"));
            Assert.IsTrue(inventory.Equipment.TryEquip(sword, out _));
            yield return null;
            Assert.IsTrue(Player.GetComponent<MeleeAttack>().Weapon.TwoHanded);
            Assert.IsTrue(talents.Holds(TalentCondition.TwoHanded));
            Assert.IsTrue(Player.GetComponentInChildren<Animator>().HasState(0, Animator.StringToHash(CharacterAnimatorDriver.TwoHandedAttackState)), "il gesto a due mani è nel controller");
            Assert.IsNull(Player.GetComponent<EquipmentVisuals>().OffHandModel);
            StringAssert.Contains("sword_2handed", Player.GetComponent<EquipmentVisuals>().MainHandModel.name);

            // il nemico a 3,6 m dal cavaliere (il raggio conta dal bordo del suo collider): fuori dal Turbine a una mano, dentro quello a due mani
            StandAt(enemy.transform, 3.6f);
            Assert.That(FlatDistance(Player.position, enemy.transform.position), Is.InRange(3.5f, 3.7f));
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(Magic.Learn(Spell("Whirlwind")));
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Whirlwind"), Player.position));
            yield return null;
            Assert.Less(health.Current, health.Max, "a 3,6 m con lo spadone sì");

            // con la spada a una mano, lo stesso nemico fuori portata
            health.Heal(10000f);
            Magic.Caster.Advance(10f);
            Magic.Mana.Fill();
            inventory.Equipment.TryEquip(new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/ShortSword.asset")), out _);
            yield return null;
            Assert.IsFalse(talents.Holds(TalentCondition.TwoHanded));
            StandAt(enemy.transform, 3.6f);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Whirlwind"), Player.position));
            yield return null;
            Assert.AreEqual(health.Max, health.Current, "a 3,6 m con la spada no");
        }

        private Vector3 Around(float distance, float angle)
        {
            for (int i = 0; i < 24; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, angle + i * 15f, 0f) * Vector3.forward * distance;
                if (NavMesh.SamplePosition(Player.position + offset, out var hit, 0.3f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            Assert.Fail($"nessun punto a {distance} m");
            return Vector3.zero;
        }

        [UnityTest, Description("Il Grido di battaglia: +15% danno sulla scheda per 12 s; il Muro di scudi blocca tutto e fa camminare a metà, finché dura")]
        public IEnumerator ShoutAndWall_LastTheirTime()
        {
            yield return LoadSandbox();
            var stats = Player.GetComponent<CharacterStats>();
            var abilities = Player.GetComponent<WarriorAbilities>();
            float before = stats.Sheet.Get(StatType.DamageBonus);
            Assert.IsTrue(Magic.Learn(Spell("BattleShout")));
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("BattleShout"), Player.position));
            Assert.AreEqual(before + 15f, stats.Sheet.Get(StatType.DamageBonus), 0.001f);
            Assert.That(abilities.Remaining(SpellKind.Shout), Is.InRange(11.5f, 12f));

            var block = Player.GetComponent<ShieldBlock>();
            block.SetShield(true, 10);
            Magic.Mana.Fill();
            Assert.IsTrue(Magic.Learn(Spell("ShieldWall")));
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("ShieldWall"), Player.position));
            Assert.IsTrue(block.BlocksAll);
            Assert.AreEqual(0.5f, Player.GetComponent<MovementSlow>().AbilityFactor);
            Assert.IsTrue(block.TryBlock(new DamageInfo(5f, DamageType.Physical, null), new FixedRandomSource(0.99)), "anche con il tiro peggiore");

            yield return Wait(5.2f);
            Assert.IsFalse(block.BlocksAll);
            Assert.AreEqual(1f, Player.GetComponent<MovementSlow>().AbilityFactor);
        }

        [UnityTest, Description("Il Salto vola sul punto a 5 m, atterra sul NavMesh e stordisce il nemico che c'è vicino; oltre gli 8 m si ferma a 8")]
        public IEnumerator Leap_LandsAndStuns()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            StandAt(skeleton.transform, 5f);
            yield return null;
            Assert.IsTrue(Magic.Learn(Spell("Leap")));
            var abilities = Player.GetComponent<WarriorAbilities>();
            Vector3 point = Vector3.MoveTowards(Player.position, skeleton.transform.position, 4f);
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Leap"), point));
            Assert.IsTrue(abilities.IsBusy, "in aria");
            yield return Wait(0.8f);
            Assert.IsFalse(abilities.IsBusy);
            Assert.IsTrue(PlayerAgent.enabled && PlayerAgent.isOnNavMesh, "di nuovo a terra");
            Assert.Less(FlatDistance(Player.position, point), 0.6f);
            Assert.IsTrue(skeleton.IsStunned);
            Assert.Less(skeleton.GetComponent<Health>().Current, skeleton.GetComponent<Health>().Max);
        }

        [UnityTest, Description("L'Ultimo baluardo: +30% di vita massima per 10 s e cura quanto ne aggiunge; finito, la vita massima torna quella")]
        public IEnumerator LastStand_RaisesAndHeals()
        {
            yield return LoadSandbox();
            var health = Player.GetComponent<Health>();
            float max = health.Max;
            health.TakeDamage(new DamageInfo(max * 0.5f, DamageType.Physical, null));
            float wounded = health.Current;
            Assert.IsTrue(Magic.Learn(Spell("LastStand")));
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("LastStand"), Player.position));
            Assert.AreEqual(max * 1.3f, health.Max, 0.01f);
            Assert.AreEqual(wounded + max * 0.3f, health.Current, 0.01f, "curato quanto ha aggiunto");
            Assert.That(Player.GetComponent<WarriorAbilities>().Remaining(SpellKind.LastStand), Is.InRange(9.5f, 10f));
        }

        [UnityTest, Description("I passivi che scattano: con Disciplina ogni colpo a segno ridà mana; la vita bassa si accende sotto il 30% e lo scudo in mano vale come condizione")]
        public IEnumerator Passives_DisciplineAndConditions()
        {
            yield return LoadSandbox();
            var talents = Player.GetComponent<PlayerTalents>();
            var build = talents.Build;
            Player.GetComponent<PlayerProgress>().Progress.Restore(30, 0, 0);
            var warrior = build.ClassTree;
            foreach (var (node, ranks) in new[] { ("strong_arm", 3), ("keen_eye", 1), ("steady_hand", 2), ("discipline", 2) })
            {
                for (int i = 0; i < ranks; i++)
                {
                    Assert.IsTrue(build.TryAdd(warrior, warrior.Find(node)), node);
                }
            }

            build.Apply();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            Magic.Mana.SetCurrent(10f);
            Player.GetComponent<MeleeAttack>().Strike(skeleton.GetComponent<Health>(), 100f, new FixedRandomSource(0.0));
            Assert.AreEqual(12f, Magic.Mana.Current, 0.01f, "due gradi di Disciplina: 2 di mana");

            var health = Player.GetComponent<Health>();
            Assert.IsFalse(talents.IsLowLife);
            health.TakeDamage(new DamageInfo(health.Max * 0.75f, DamageType.Physical, null));
            Assert.IsTrue(talents.IsLowLife);
            health.Heal(health.Max);
            Assert.IsFalse(talents.IsLowLife);

            var block = Player.GetComponent<ShieldBlock>();
            block.SetShield(false, 0);
            Assert.IsFalse(talents.Holds(Talents.TalentCondition.Shield));
            block.SetShield(true, 10);
            Assert.IsTrue(talents.Holds(Talents.TalentCondition.Shield));
        }
    }
}
