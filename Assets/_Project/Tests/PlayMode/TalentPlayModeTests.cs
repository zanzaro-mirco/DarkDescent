using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Magic;
using DarkDescent.Progression;
using DarkDescent.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// L'albero delle abilità nel gioco (passo 20.2 della M20): i punti seguono il livello del
    /// cavaliere, un nodo applicato cambia la scheda, l'altare la riporta com'era, e il salvataggio
    /// rimette l'albero dopo il livello, senza perderne niente. La barra la riempie l'albero (20.3).
    /// </summary>
    public class TalentPlayModeTests : SandboxFixture
    {
        private static SpellDefinition Spell(string name) => AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

        private static string Fixture(string name) => AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/_Project/Tests/EditMode/Fixtures/{name}.json").text;

        [UnityTest, Description("Le caselle seguono l'albero (D9 e D18 della M20): l'Attacco nel sinistro e la radice nel destro; con Armi applicata il Turbine entra nella prima casella libera; spostato, ci resta a ogni Applica; l'altare lo toglie e lascia il Colpo possente")]
        public IEnumerator Bar_FollowsTheTree()
        {
            yield return LoadGeneratedCore();
            var progress = Player.GetComponent<PlayerProgress>().Progress;
            var build = Player.GetComponent<PlayerTalents>().Build;
            var book = Player.GetComponent<PlayerMagic>().Book;
            CollectionAssert.AreEqual(new[] { Spell("Attack").Id, Spell("PowerStrike").Id }, book.KnownIds(), "l'Attacco e la radice: niente Dardo, niente Anello");
            Assert.AreEqual((Spell("Attack"), Spell("PowerStrike")), (book[0], book[1]));

            progress.Restore(12, 0, 0);
            Assert.IsTrue(build.TryChoose(build.Specializations[1]));
            Assert.IsFalse(book.Knows(Spell("Whirlwind")), "in attesa non cambia niente");
            build.Apply();
            Assert.AreEqual(2, book.SlotOf(Spell("Whirlwind")));

            Assert.IsTrue(book.Assign(3, Spell("Whirlwind")));
            Assert.IsTrue(build.TryAdd(build.ClassTree, build.ClassTree.Find("toughness")));
            build.Apply();
            Assert.AreEqual(3, book.SlotOf(Spell("Whirlwind")), "un altro Applica non lo sposta");

            build.Reset();
            Assert.IsFalse(book.Knows(Spell("Whirlwind")));
            Assert.AreEqual(1, book.SlotOf(Spell("PowerStrike")));
            Assert.AreEqual(2, book.KnownIds().Count, "l'Attacco resta");
        }

        [UnityTest, Description("Il salvataggio del formato 8 (il campione save_v8) rimette l'albero con Armi e la barra a otto posti messa nelle quattro caselle: l'Attacco a sinistra, il Turbine nel destro, il Colpo possente in Q")]
        public IEnumerator SaveEight_RestoresTreeAndBar()
        {
            yield return LoadGeneratedCore();
            var save = Object.FindFirstObjectByType<SaveGame>();
            var build = Player.GetComponent<PlayerTalents>().Build;
            var book = Player.GetComponent<PlayerMagic>().Book;
            var data = SaveMigrator.Migrate(SaveData.FromJson(Fixture("save_v8")));
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            save.Apply(data);

            Assert.AreEqual("warrior.arms", build.AppliedSpecialization.Id);
            Assert.AreEqual(3, build.AppliedRank(build.ClassTree, build.ClassTree.Find("toughness")));
            Assert.AreEqual((Spell("Attack"), Spell("Whirlwind"), Spell("PowerStrike")), (book[0], book[1], book[2]));
            Assert.AreEqual(4, book.Export().Count, "ricatturata, la barra è a quattro posti");
        }

        [UnityTest, Description("Il salvataggio rimette la barra anche con la magia spenta, come in pausa: riaccesa, i posti restano quelli salvati")]
        public IEnumerator SaveEight_WithMagicOff_KeepsTheBar()
        {
            yield return LoadGeneratedCore();
            var save = Object.FindFirstObjectByType<SaveGame>();
            var magic = Player.GetComponent<PlayerMagic>();
            magic.enabled = false;
            save.Apply(SaveMigrator.Migrate(SaveData.FromJson(Fixture("save_v8"))));
            magic.enabled = true;
            yield return null;

            Assert.AreEqual((Spell("Attack"), Spell("Whirlwind"), Spell("PowerStrike")), (magic.Book[0], magic.Book[1], magic.Book[2]));
        }

        [UnityTest, Description("Una pergamena nello zaino di un salvataggio vecchio (formato 4) diventa oro al caricamento, al prezzo del mercante, e non insegna niente (D4 della M20)")]
        public IEnumerator OldSave_SellsTheScroll()
        {
            yield return LoadGeneratedCore();
            var save = Object.FindFirstObjectByType<SaveGame>();
            var inventory = Player.GetComponent<PlayerInventory>().Inventory;
            string path = Path.Combine(Application.temporaryCachePath, "save_v4_scroll.json");
            File.WriteAllText(path, Fixture("save_v4"));
            try
            {
                Assert.AreEqual(SaveReadResult.Ok, SaveFile.TryRead(path, out var data));
                save.Apply(data);
                Assert.IsFalse(inventory.Grid.Placements.Keys.Any(item => item.Definition is ScrollDefinition), "niente pergamene nello zaino");
                Assert.Greater(inventory.Gold, data.Gold, "venduta");
                Assert.IsFalse(Player.GetComponent<PlayerMagic>().Book.Knows(Spell("StoneSkin")));
            }
            finally
            {
                File.Delete(path);
            }
        }

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
