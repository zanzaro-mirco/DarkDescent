using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using DarkDescent.Magic;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Pozzi e santuari nella cripta (D6, D7 e D10 della M13).</summary>
    public class FeaturePlayModeTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentFeatures_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static ShrineDefinition Shrine(string name) => AssetDatabase.LoadAssetAtPath<ShrineDefinition>($"Assets/_Project/Data/Shrines/Shrine_{name}.asset");

        private ShrineBonus Bonus => Player.GetComponent<ShrineBonus>();

        private static IEnumerator Travel(string scene, string entrance, int depth)
        {
            var before = Manager.CurrentLevel;
            Manager.LoadLevel(scene, entrance, depth);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == before; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, scene + " non arriva");
                yield return null;
            }

            yield return null;
            yield return null;
        }

        // la cripta del primo seme, alla profondità 1 o 2, con il punto d'interesse cercato
        private IEnumerator GoToCryptWith(Func<OneUseFeature, bool> wanted)
        {
            yield return LoadGeneratedCore();
            for (ulong seed = 1; seed <= 40; seed++)
            {
                Manager.RunSeed = seed;
                yield return Travel("Level_Crypt", "Start", 1 + (int)(seed % 2));
                if (Manager.CurrentLevel.Features.Any(wanted))
                {
                    foreach (var enemy in Manager.CurrentLevel.Enemies)
                    {
                        if (enemy != null)
                        {
                            enemy.gameObject.SetActive(false);
                        }
                    }

                    yield break;
                }
            }

            Assert.Fail("nessun seme da 1 a 40 ha il punto d'interesse cercato");
        }

        private void StandBefore(OneUseFeature feature)
        {
            Vector3 approach = feature.GetComponent<Interactable>().ApproachPoint;
            Assert.IsTrue(NavMesh.SamplePosition(approach + feature.transform.forward * 1.5f, out var hit, 1.5f, NavMesh.AllAreas), "davanti non c'è posto");
            PlayerAgent.Warp(hit.position);
        }

        [UnityTest, Description("Un click sul pozzo della vita: il cavaliere ci va e la vita torna piena; il liquido scende, il cristallo e la luce si spengono e il nome dice vuoto; una seconda volta non fa niente")]
        public IEnumerator LifeWell_FillsLifeOnce()
        {
            yield return GoToCryptWith(f => f is Well w && !w.IsMana);
            var well = (Well)Manager.CurrentLevel.Features.First(f => f is Well w && !w.IsMana);
            var health = Player.GetComponent<Health>();
            health.SetCurrent(health.Max * 0.3f);
            float full = well.Liquid.localPosition.y;
            var label = well.GetComponent<Interactable>();
            Assert.AreEqual("Life Well", label.GetLabel(Localizer));

            StandBefore(well);
            yield return new WaitForSeconds(0.5f);
            ClickAt(well.transform.position + Vector3.up * 0.8f);
            for (float t = 0f; !well.IsSpent; t += Time.deltaTime)
            {
                Assert.Less(t, 8f, "il cavaliere non ha usato il pozzo");
                yield return null;
            }

            Assert.AreEqual(health.Max, health.Current, 0.01f, "vita piena");
            Assert.AreEqual("Life Well (empty)", label.GetLabel(Localizer));
            Assert.IsFalse(well.GetComponentInChildren<Light>().enabled, "la luce si spegne");
            Assert.AreEqual("M_CrystalSpent", well.transform.Find("Crystal").GetComponentInChildren<Renderer>().sharedMaterial.name);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(full - 0.3f, well.Liquid.localPosition.y, 0.01f, "il liquido è sceso");

            health.SetCurrent(health.Max * 0.3f);
            label.Use(Player.gameObject);
            Assert.AreEqual(health.Max * 0.3f, health.Current, 0.01f, "vuoto non dà più niente");
        }

        [UnityTest, Description("Il pozzo del mana riempie tutto il mana")]
        public IEnumerator ManaWell_FillsMana()
        {
            yield return GoToCryptWith(f => f is Well w && w.IsMana);
            var well = Manager.CurrentLevel.Features.First(f => f is Well w && w.IsMana);
            var mana = Player.GetComponent<PlayerMagic>().Mana;
            mana.TrySpend(mana.Current);
            Assert.AreEqual(0f, mana.Current, 0.01f);
            Assert.AreEqual("Mana Well", well.GetComponent<Interactable>().GetLabel(Localizer));
            well.GetComponent<Interactable>().Use(Player.gameObject);
            Assert.IsTrue(well.IsSpent);
            Assert.AreEqual(mana.Max, mana.Current, 0.01f);
        }

        [UnityTest, Description("Ogni santuario dà il suo bonus: danno, velocità, armatura, esperienza; uno nuovo prende il posto del vecchio; finito il tempo tutto torna com'era. L'icona accanto alla cintura ha il colore e i secondi, e un messaggio dice il bonus")]
        public IEnumerator Shrines_GiveTheirBonus()
        {
            yield return LoadGeneratedCore();
            var bonus = Bonus;
            var stats = Player.GetComponent<CharacterStats>();
            var progress = Player.GetComponent<PlayerProgress>();
            var view = UnityEngine.Object.FindFirstObjectByType<ShrineBonusView>();
            var message = UnityEngine.Object.FindFirstObjectByType<HudMessage>();
            float speed = PlayerAgent.speed;
            float armor = stats.Armor;
            Assert.IsFalse(view.IsShowing);

            bonus.Apply(Shrine("Fury"));
            yield return null;
            Assert.AreEqual(25f, CharacterStats.ValueOf(stats, StatType.DamageBonus));
            Assert.IsTrue(view.IsShowing);
            Assert.AreEqual("60", view.SecondsText);
            Assert.AreEqual(Shrine("Fury").Color, view.IconColor);
            Assert.AreEqual("Fury: +25% damage for 60 seconds", message.Text);

            int before = progress.AwardKill(20, 1);
            bonus.Apply(Shrine("Wisdom"));
            Assert.AreEqual(0f, CharacterStats.ValueOf(stats, StatType.DamageBonus), "la Furia se n'è andata");
            int with = progress.AwardKill(20, 1);
            Assert.AreEqual(Math.Round(before * 1.5, MidpointRounding.AwayFromZero), with, "esperienza +50%");

            bonus.Apply(Shrine("Stone"));
            Assert.AreEqual(armor * 1.5f, stats.Armor, 0.01f, "armatura +50%");

            bonus.Apply(Shrine("Wind"), 0.5f);
            Assert.AreEqual(armor, stats.Armor, 0.01f);
            Assert.AreEqual(speed * 1.25f, PlayerAgent.speed, 0.01f, "velocità +25%");
            Assert.AreEqual("1", view.SecondsText);
            yield return new WaitForSeconds(0.8f);
            Assert.IsNull(bonus.Current, "finito il tempo");
            Assert.AreEqual(speed, PlayerAgent.speed, 0.01f);
            Assert.IsFalse(view.IsShowing, "l'icona sparisce");
        }

        [UnityTest, Description("Usato, il santuario dà il suo bonus; tornando dalla città resta spento e vuoto, e non lo dà più")]
        public IEnumerator Shrine_StaysSpentAfterLeaving()
        {
            yield return GoToCryptWith(f => f is Shrine);
            var level = Manager.CurrentLevel;
            int index = level.Features.ToList().FindIndex(f => f is Shrine);
            var shrine = (Shrine)level.Features[index];
            int depth = level.Depth;
            ulong seed = Manager.RunSeed;
            shrine.GetComponent<Interactable>().Use(Player.gameObject);
            Assert.AreSame(shrine.Definition, Bonus.Current);
            Assert.IsTrue(shrine.IsSpent);

            yield return Travel("Town", "Start", 0);
            Manager.RunSeed = seed;
            yield return Travel("Level_Crypt", "Start", depth);
            var again = (Shrine)Manager.CurrentLevel.Features[index];
            Assert.AreSame(shrine.Definition, again.Definition, "lo stesso santuario dal seme");
            Assert.IsTrue(again.IsSpent, "resta usato");
            StringAssert.EndsWith("(empty)", again.GetComponent<Interactable>().GetLabel(Localizer));
            Assert.IsFalse(again.GetComponentInChildren<Light>().enabled);
            Assert.AreEqual(Manager.CurrentLevel.Features.Count(f => !f.IsSpent), Manager.CurrentLevel.Features.Count - 1, "gli altri restano pieni");

            Bonus.Apply(Shrine("Fury"));
            again.GetComponent<Interactable>().Use(Player.gameObject);
            Assert.AreSame(Shrine("Fury"), Bonus.Current, "vuoto non cambia il bonus");
        }
    }
}
