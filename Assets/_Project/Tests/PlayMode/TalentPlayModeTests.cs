using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Progression;
using DarkDescent.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// L'albero delle abilità nel gioco (passo 20.2 della M20): i punti seguono il livello del
    /// cavaliere, un nodo applicato cambia la scheda, l'altare la riporta com'era, e il salvataggio
    /// rimette l'albero dopo il livello, senza perderne niente.
    /// </summary>
    public class TalentPlayModeTests : SandboxFixture
    {
        [UnityTest, Description("Un punto speso nell'albero cambia la vita solo dopo Applica: tre gradi di Tempra, +12%; ripartire da zero la riporta com'era")]
        public IEnumerator AppliedPoint_ChangesTheStat()
        {
            yield return LoadGeneratedCore();
            var progress = Player.GetComponent<PlayerProgress>().Progress;
            var build = Player.GetComponent<PlayerTalents>().Build;
            var health = Player.GetComponent<Health>();
            progress.Restore(5, 0, 0);
            Assert.AreEqual(5, build.Level, "i punti seguono il livello");
            Assert.AreEqual(4, build.Available(build.ClassTree));

            var toughness = build.ClassTree.Find("toughness");
            float before = health.Max;
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(build.TryAdd(build.ClassTree, toughness));
            }

            Assert.AreEqual(before, health.Max, "in attesa non cambia niente");
            build.Apply();
            Assert.AreEqual(before * 1.12f, health.Max, 0.01f);

            build.Reset();
            Assert.AreEqual(before, health.Max, 0.01f);
        }

        [UnityTest, Description("Un Guerriero di livello 30 con Armi e i suoi nodi, salvato e ricaricato, torna con lo stesso albero e la stessa vita: l'albero si rimette dopo il livello")]
        public IEnumerator SaveAndLoad_KeepsTheBuild()
        {
            yield return LoadGeneratedCore();
            var save = Object.FindFirstObjectByType<SaveGame>();
            var progress = Player.GetComponent<PlayerProgress>().Progress;
            var build = Player.GetComponent<PlayerTalents>().Build;
            var health = Player.GetComponent<Health>();
            progress.Restore(30, 0, 0);

            var warrior = build.ClassTree;
            var arms = build.Specializations[1];
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(build.TryAdd(warrior, warrior.Find("toughness")));
                Assert.IsTrue(build.TryAdd(warrior, warrior.Find("strong_arm")));
            }

            Assert.IsTrue(build.TryChoose(arms));
            Assert.IsTrue(build.TryAdd(arms, arms.Find("keen_edge")));
            Assert.IsTrue(build.TryAdd(arms, arms.Find("keen_edge")));
            build.Apply();
            var records = build.Export();
            float max = health.Max;
            string json = save.Capture().ToJson();

            // un altro cavaliere
            progress.Restore(1, 0, 0);
            build.Reset();
            Assert.Less(health.Max, max);

            var data = SaveData.FromJson(json);
            Assert.AreEqual("warrior.arms", data.Specialization);
            save.Apply(data);
            Assert.AreSame(arms, build.AppliedSpecialization);
            CollectionAssert.AreEquivalent(records, build.Export());
            Assert.AreEqual(max, health.Max, 0.01f);
            Assert.AreEqual((30, 18 - 6, 11 - 2), (build.Level, build.Available(warrior), build.Available(arms)));
        }
    }
}
