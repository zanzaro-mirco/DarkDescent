using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Magic;
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
    /// L'albero del Mago nel gioco (passo 21.5 della M21): le abilità che entrano nella barra, i passivi sulla scheda (danno
    /// degli incantesimi, mana massimo, rigenerazione), il mana del Dardo arcano e dei congelamenti, la Scintilla, il fuoco che
    /// resta, la Meteora e la Nova doppie, le fiamme a terra e il gelo che resta.
    /// </summary>
    public class MageTalentPlayModeTests : SandboxFixture
    {
        private static SpellDefinition Spell(string name) => AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

        private PlayerMagic Magic => Player.GetComponent<PlayerMagic>();

        private PlayerSpells Spells => Player.GetComponent<PlayerSpells>();

        private TalentBuild Build => Player.GetComponent<PlayerTalents>().Build;

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        // i nodi del Mago a livello 50 (28 punti nella classe, 21 nella specializzazione), nell'ordine dato: "id", "id*gradi", "id/opzione"
        private void BecomeMage(int specialization, params string[] picks)
        {
            var playerClass = Player.GetComponent<PlayerClass>();
            playerClass.Apply(playerClass.Catalog.Find("mage"), "Merlino", true);
            Player.GetComponent<PlayerProgress>().Progress.Restore(50, 0, 0);
            var build = Build;
            Assert.IsTrue(build.TryChoose(build.Specializations[specialization]));
            foreach (string pick in picks)
            {
                string[] option = pick.Split('/');
                string[] ranks = option[0].Split('*');
                string id = ranks[0];
                var tree = build.ClassTree.Find(id) != null ? build.ClassTree : build.Specialization;
                var node = tree.Find(id);
                Assert.IsNotNull(node, id);
                for (int i = 0; i < (ranks.Length > 1 ? int.Parse(ranks[1]) : 1); i++)
                {
                    Assert.IsTrue(build.TryAdd(tree, node, option.Length > 1 ? int.Parse(option[1]) : 0), $"{id}, grado {i + 1}");
                }
            }

            build.Apply();
            Magic.ManaRegenPerSecond = 0f;
        }

        // le strade più corte ai nodi, con i punti e le linee che chiedono
        private static string[] CraftPath(int option) => new[] { "keen_mind*3", "fire_ring", "swift_bolt*2", "concentration*2", $"fire_craft/{option}" };

        private static string[] FinalePath(int option) =>
            new[] { "keen_mind*3", "fire_ring", "mana_reserve*3", "swift_bolt*2", "concentration*2", "fire_craft", "quick_step*2", $"fire_finale/{option}" };

        private static string[] FirePath(int option) =>
            new[] { "flame_wall", "inflamed_mind*3", "great_blaze", "explosion*2", "quick_hand*2", "live_embers*2", $"fire_end/{option}", "meteor" };

        private static string[] FrostPath(int option) => new[] { "frost_nova", "frozen_skin*3", "quick_frost*2", "rime*2", $"frost_end/{option}", "ice_mind*2" };

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

        private static EnemyAI Still(EnemyAI enemy)
        {
            enemy.GetComponent<NavMeshAgent>().isStopped = true;
            return enemy;
        }

        private static EnemyAI[] Skeletons(int count) =>
            Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Where(e => e.isActiveAndEnabled).Take(count).Select(Still).ToArray();

        [UnityTest, Description("Il Mago con la strada fino alla Meteora: le abilità prese entrano nel libro, quelle del Gelo no; l'altare le toglie e resta la radice della classe")]
        public IEnumerator Tree_FillsTheBook()
        {
            yield return LoadSandbox();
            BecomeMage(0, FirePath(0).Append("fire_ring").ToArray());
            var book = Magic.Book;
            foreach (string name in new[] { "Firebolt", "FireRing", "Fireball", "FlameWall", "Meteor" })
            {
                Assert.IsTrue(book.Knows(Spell(name)), name);
            }

            Assert.IsFalse(book.Knows(Spell("IceBolt")), "il Gelo non è stato scelto");
            Assert.IsTrue(book.Knows(Spell("ArcaneBolt")), "il Dardo arcano resta nel sinistro");

            Build.Reset();
            Assert.IsFalse(book.Knows(Spell("Fireball")));
            Assert.IsTrue(book.Knows(Spell("Firebolt")), "la radice della classe è gratis");
        }

        [UnityTest, Description("I passivi della scheda: il danno degli incantesimi, il mana massimo, la rigenerazione e la Magia si sommano, e il mana massimo cresce di quel che dicono")]
        public IEnumerator StatNodes_ChangeTheSheet()
        {
            yield return LoadSandbox();
            var playerClass = Player.GetComponent<PlayerClass>();
            playerClass.Apply(playerClass.Catalog.Find("mage"), "Merlino", true);
            var sheet = Player.GetComponent<CharacterStats>().Sheet;
            float magic = sheet.Get(StatType.Magic);

            BecomeMage(0, "keen_mind*3", "fire_ring", "mana_reserve*3", "blink", "cold_mind*2", "inflamed_mind*3", "great_blaze*3", "burning_magic*2");
            Assert.AreEqual(9f + 12f, sheet.Get(StatType.SpellDamage), 0.001f, "Mente acuta 3×3, Mente infuocata 3×4");
            Assert.AreEqual(15f, sheet.Get(StatType.MaxManaPercent), 0.001f, "Riserva di mana 3×5");
            Assert.AreEqual(100f, sheet.Get(StatType.ManaRegenPercent), 0.001f, "Mente fredda 2×50");
            Assert.AreEqual(magic + 6f, sheet.Get(StatType.Magic), 0.001f, "Magia ardente 2×3");
            float expected = CombatFormulas.MaxMana(sheet.Get(StatType.Magic), sheet.Get(StatType.Level), sheet.Get(StatType.Mana), 15f);
            Assert.AreEqual(expected, Magic.Mana.Max, 0.01f);
        }

        [UnityTest, Description("La rigenerazione del mana: +100% la raddoppia (3% del massimo al secondo del Mago, 6%)")]
        public IEnumerator ManaRegenPercent_DoublesTheRegeneration()
        {
            yield return LoadSandbox();
            var playerClass = Player.GetComponent<PlayerClass>();
            playerClass.Apply(playerClass.Catalog.Find("mage"), "Merlino", true);
            Magic.Mana.TrySpend(Magic.Mana.Current);
            yield return null;
            float before = Magic.Mana.Current;
            yield return Wait(1f);
            float plain = Magic.Mana.Current - before;
            Assert.AreEqual(Magic.Mana.Max * 0.03f, plain, Magic.Mana.Max * 0.006f, "il 3% al secondo");

            Player.GetComponent<CharacterStats>().Sheet.AddModifier(new StatModifier(StatType.ManaRegenPercent, ModifierKind.Flat, 100f, this));
            Magic.Mana.TrySpend(Magic.Mana.Current);
            yield return null;
            before = Magic.Mana.Current;
            yield return Wait(1f);
            Assert.AreEqual(plain * 2f, Magic.Mana.Current - before, plain * 0.25f, "il doppio");
        }

        [UnityTest, Description("Il danno degli incantesimi raddoppia il tiro di un incantesimo con +100%")]
        public IEnumerator SpellDamage_ScalesTheRoll()
        {
            yield return LoadSandbox();
            var playerClass = Player.GetComponent<PlayerClass>();
            playerClass.Apply(playerClass.Catalog.Find("mage"), "Merlino", true);
            var firebolt = Spell("Firebolt");
            float Average() => Enumerable.Range(0, 300).Average(_ => Spells.RollDamage(firebolt));
            float plain = Average();
            Player.GetComponent<CharacterStats>().Sheet.AddModifier(new StatModifier(StatType.SpellDamage, ModifierKind.Flat, 100f, this));
            Assert.AreEqual(plain * 2f, Average(), plain * 0.2f);
        }

        [UnityTest, Description("Concentrazione: ogni Dardo arcano a segno ridà 1 mana per grado (2 al massimo); un'altra abilità che colpisce non ne dà")]
        public IEnumerator Concentration_GivesManaOnBoltHit()
        {
            yield return LoadSandbox();
            BecomeMage(0, CraftPath(1));
            var health = Object.FindFirstObjectByType<EnemyAI>().GetComponent<Health>();
            Magic.Mana.TrySpend(Magic.Mana.Current - 10f);
            Spells.ReportStruck(Spell("Fireball"), health);
            Assert.AreEqual(10f, Magic.Mana.Current, 0.001f, "una palla di fuoco non conta");
            Spells.ReportStruck(Spells.Bolt, health);
            Assert.AreEqual(12f, Magic.Mana.Current, 0.001f, "2 gradi, 2 mana");
        }

        [UnityTest, Description("Scintilla: ogni Dardo arcano a segno ha il 10% di lanciare gratis un Dardo di fuoco, senza spendere mana; con Fiammata non c'è")]
        public IEnumerator Spark_CastsFireboltsForFree()
        {
            yield return LoadSandbox();
            BecomeMage(0, CraftPath(0));
            var health = Still(Object.FindFirstObjectByType<EnemyAI>()).GetComponent<Health>();
            StandAt(health.transform, 6f);
            yield return null;
            Magic.Mana.TrySpend(Magic.Mana.Current - 50f);
            for (int i = 0; i < 400; i++)
            {
                Spells.ReportStruck(Spells.Bolt, health);
            }

            Assert.That(Spells.FreeCasts, Is.InRange(15, 70), "il 10% di 400");

            BecomeMage(0, CraftPath(1));
            int before = Spells.FreeCasts;
            for (int i = 0; i < 200; i++)
            {
                Spells.ReportStruck(Spells.Bolt, health);
            }

            Assert.AreEqual(before, Spells.FreeCasts, "Fiammata al posto della Scintilla");
        }

        [UnityTest, Description("Un lancio gratis colpisce il nemico, senza mana né ricarica")]
        public IEnumerator CastFree_HitsWithoutCost()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            var health = skeleton.GetComponent<Health>();
            StandAt(skeleton.transform, 6f);
            yield return null;
            var playerClass = Player.GetComponent<PlayerClass>();
            playerClass.Apply(playerClass.Catalog.Find("mage"), "Merlino", true);
            float mana = Magic.Mana.Current;
            Spells.CastFree(Spell("Firebolt"), health);
            Assert.AreEqual(1, Spells.FreeCasts);
            yield return Wait(1.2f);
            Assert.Less(health.Current, health.Max);
            Assert.AreEqual(mana, Magic.Mana.Current, 0.5f);
            Spells.CastFree(Spell("FireRing"), health);
            Assert.AreEqual(1, Spells.FreeCasts, "solo i proiettili");
        }

        [UnityTest, Description("Fiammata: il nemico colpito brucia per 3 s e perde il 60% del colpo in tutto, a battiti di 0,5 s; a fine tempo smette; chi non ha il nodo non brucia")]
        public IEnumerator Blaze_BurnsTheEnemy()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            var health = skeleton.GetComponent<Health>();
            var playerClass = Player.GetComponent<PlayerClass>();
            playerClass.Apply(playerClass.Catalog.Find("mage"), "Merlino", true);
            Spells.ApplyBurn(Spell("Firebolt"), health, 20f);
            Assert.AreEqual(0, Spells.Burning, "senza il nodo non brucia");

            BecomeMage(0, CraftPath(1));
            float life = health.Current;
            Spells.ApplyBurn(Spell("Firebolt"), health, 20f);
            Assert.AreEqual(1, Spells.Burning);
            yield return Wait(1.1f);
            Assert.Less(health.Current, life, "brucia già");
            yield return Wait(2.6f);
            Assert.AreEqual(0, Spells.Burning, "finito");
            Assert.AreEqual(life - 12f, health.Current, 1.1f, "il 60% di 20");
        }

        [UnityTest, Description("Pioggia di scintille: il Dardo di fuoco colpisce anche chi sta entro 2 m dall'impatto")]
        public IEnumerator SparkRain_HitsNeighbours()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Skeletons(2);
            StandAt(enemies[0].transform, 6f);
            yield return null;
            // di fianco al primo, di traverso rispetto al tiro: lo scoppio prende il vicino anche se il tiro arriva da un lato
            Vector3 side = Vector3.Cross(Vector3.up, (enemies[0].transform.position - Player.position).normalized);
            PlaceNear(enemies[1], enemies[0].transform.position + side * 0.5f, 0.5f);
            yield return new WaitForFixedUpdate(); // il collider del vicino raggiunge il nuovo posto solo al passo di fisica
            Physics.SyncTransforms();
            BecomeMage(0, FinalePath(0));
            Magic.Mana.Fill();
            // si conta il colpo e non la vita: un nemico fuori dal suo posto torna a casa e si cura, anche entro il secondo di attesa
            bool targetHit = false, neighbourHit = false;
            enemies[0].GetComponent<Health>().Damaged += (info, applied) => targetHit = true;
            enemies[1].GetComponent<Health>().Damaged += (info, applied) => neighbourHit = true;
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Firebolt"), enemies[0].transform.position));
            yield return Wait(1.2f);
            Assert.IsTrue(targetHit, "il bersaglio");
            Assert.IsTrue(neighbourHit, "il vicino");
        }

        [UnityTest, Description("Meteora doppia: la Meteora cade due volte sullo stesso punto, a un secondo di distanza; senza il nodo una sola")]
        public IEnumerator DoubleMeteor_FallsTwice()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            StandAt(skeleton.transform, 6f);
            yield return null;
            BecomeMage(0, FirePath(0));
            Assert.IsTrue(Magic.Book.Assign(3, Spell("Meteor")), "la quinta abilità non ha una casella");
            Magic.Mana.Fill();
            var abilities = Player.GetComponent<MageAbilities>();
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Meteor"), skeleton.transform.position));
            yield return null;
            Assert.AreEqual(2, abilities.PendingMeteors, "due meteore");
            yield return Wait(1.7f);
            Assert.AreEqual(1, abilities.PendingMeteors, "la prima è caduta, la seconda no");
            yield return Wait(1.2f);
            Assert.AreEqual(0, abilities.PendingMeteors);
        }

        [UnityTest, Description("Marea di fuoco: la Palla di fuoco lascia le fiamme a terra per 4 s, che poi si spengono")]
        public IEnumerator FireTide_LeavesFlames()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            StandAt(skeleton.transform, 6f);
            yield return null;
            BecomeMage(0, FirePath(1));
            Magic.Mana.Fill();
            var abilities = Player.GetComponent<MageAbilities>();
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("Fireball"), skeleton.transform.position));
            yield return Wait(1.5f);
            Assert.AreEqual(1, abilities.ActiveZones, "le fiamme a terra");
            yield return Wait(4f);
            Assert.AreEqual(0, abilities.ActiveZones, "spente");
        }

        [UnityTest, Description("Nova doppia: la Nova scatta una seconda volta un secondo dopo, e congela chi nel frattempo è arrivato")]
        public IEnumerator DoubleNova_StrikesTwice()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Skeletons(2);
            StandAt(enemies[0].transform, 2f);
            yield return null;
            PlaceNear(enemies[1], Player.position, 8f);
            yield return null;
            BecomeMage(1, FrostPath(1));
            Magic.Mana.Fill();
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("FrostNova"), Player.position));
            yield return Wait(0.4f);
            Assert.IsTrue(enemies[0].IsFrozen, "il primo scatto");
            Assert.IsFalse(enemies[1].IsFrozen);
            PlaceNear(enemies[1], Player.position, 2f);
            yield return Wait(1f);
            Assert.IsTrue(enemies[1].IsFrozen, "il secondo scatto prende chi è arrivato");
        }

        [UnityTest, Description("Gelo che resta: i congelati dalla Nova restano rallentati del 40% dopo il gelo; senza il nodo non c'è rallentamento")]
        public IEnumerator LingeringFrost_SlowsAfterTheFreeze()
        {
            yield return LoadSandbox();
            var skeleton = Still(Object.FindFirstObjectByType<EnemyAI>());
            StandAt(skeleton.transform, 2f);
            yield return null;
            BecomeMage(1, FrostPath(0));
            Magic.Mana.Fill();
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("FrostNova"), Player.position));
            yield return Wait(0.4f);
            Assert.IsTrue(skeleton.IsFrozen);
            Assert.IsTrue(skeleton.IsSlowed);
            Assert.AreEqual(0.4f, skeleton.SlowFraction, 0.001f);
            Assert.Greater(skeleton.SlowRemaining, 3.5f, "il gelo e poi i suoi 3 s");
        }

        [UnityTest, Description("Mente di ghiaccio: ogni nemico congelato dà 2 mana per grado (4 al massimo)")]
        public IEnumerator IceMind_GivesManaPerFrozenEnemy()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Skeletons(2);
            StandAt(enemies[0].transform, 2f);
            yield return null;
            PlaceNear(enemies[1], Player.position, 2.5f);
            yield return null;
            BecomeMage(1, FrostPath(0));
            Magic.Mana.Fill();
            Assert.AreEqual(CastResult.Ready, Magic.TryCastAt(Spell("FrostNova"), Player.position));
            float afterCast = Magic.Mana.Current;
            yield return Wait(0.4f);
            Assert.IsTrue(enemies[0].IsFrozen && enemies[1].IsFrozen);
            Assert.AreEqual(Mathf.Min(afterCast + 8f, Magic.Mana.Max), Magic.Mana.Current, 0.5f, "4 mana per ognuno dei due");
        }
    }
}
