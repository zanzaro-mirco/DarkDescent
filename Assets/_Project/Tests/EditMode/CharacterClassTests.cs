using System.Linq;
using DarkDescent.Characters;
using DarkDescent.Classes;
using DarkDescent.Editor;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le classi di personaggio (D2 della M21, passo 21.2): il Guerriero è il cavaliere di sempre, il Mago ha i
    /// suoi attributi, il suo schema dei punti e il suo mana; l'albero si cambia senza perdere l'oggetto a cui
    /// pannello e salvataggio sono collegati; il prefab ha i due corpi sullo stesso rig.
    /// </summary>
    public class CharacterClassTests
    {
        private static CharacterClassCatalog Catalog => AssetDatabase.LoadAssetAtPath<CharacterClassCatalog>(ClassTools.CatalogPath);

        private static TalentTree Tree(string name) => AssetDatabase.LoadAssetAtPath<TalentTree>($"Assets/_Project/Data/Talents/{name}.asset");

        private static ProgressionSettings Settings => AssetDatabase.LoadAssetAtPath<ProgressionSettings>("Assets/_Project/Data/Progression/Progression.asset");

        [Test, Description("L'elenco ha il Guerriero e il Mago, nell'ordine; un id che non c'è torna il Guerriero, come un salvataggio di prima")]
        public void Catalog_HasWarriorAndMage()
        {
            CollectionAssert.AreEqual(new[] { "warrior", "mage" }, Catalog.All.Select(c => c.Id).ToArray());
            Assert.AreEqual(Catalog.Warrior, Catalog.Find("warrior"));
            Assert.AreEqual("mage", Catalog.Find("mage").Id);
            Assert.AreEqual(Catalog.Warrior, Catalog.Find("ranger"), "una classe che non c'è");
            Assert.AreEqual(Catalog.Warrior, Catalog.Find(null));
        }

        [Test, Description("Il Guerriero è il cavaliere di sempre: gli attributi sono quelli del prefab, i suoi alberi quelli del Guerriero, lo schema dei punti quello di HeroSetup, il mana quello di tutti")]
        public void Warrior_IsTheKnightOfAlways()
        {
            var warrior = Catalog.Warrior;
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var stats = new SerializedObject(player.GetComponent<CharacterStats>());
            Assert.AreEqual((stats.FindProperty("_strength").floatValue, stats.FindProperty("_dexterity").floatValue, stats.FindProperty("_magic").floatValue, stats.FindProperty("_vitality").floatValue),
                (warrior.Strength, warrior.Dexterity, warrior.Magic, warrior.Vitality));
            Assert.AreEqual(Tree("Warrior"), warrior.ClassTree);
            CollectionAssert.AreEqual(new[] { "warrior.defense", "warrior.arms" }, warrior.Specializations.Select(s => s.Id).ToArray());
            Assert.IsTrue(warrior.IsPlayable);
            Assert.AreEqual(1f, warrior.ManaRegenFactor);
            Assert.AreEqual(Combat.CombatFormulas.ManaRegenPerSecond, warrior.ManaRegenPerSecond);
            Assert.AreEqual("knight", warrior.Body);

            // con lo schema della classe i punti vanno dove andavano
            var a = new StatSheet();
            var b = new StatSheet();
            foreach (var sheet in new[] { a, b })
            {
                sheet.SetBase(StatType.Strength, warrior.Strength);
                sheet.SetBase(StatType.Dexterity, warrior.Dexterity);
                sheet.SetBase(StatType.Magic, warrior.Magic);
                sheet.SetBase(StatType.Vitality, warrior.Vitality);
            }

            HeroSetup.RaiseTo(new CharacterProgress(Settings, a), 32);
            HeroSetup.RaiseTo(new CharacterProgress(Settings, b), 32, warrior.PointPattern);
            foreach (var stat in new[] { StatType.Strength, StatType.Dexterity, StatType.Magic, StatType.Vitality })
            {
                Assert.AreEqual(a.GetBase(stat), b.GetBase(stat), stat.ToString());
            }
        }

        [Test, Description("Il Mago ha poca Forza e tanta Magia, spende i punti in Magia e Vitalità, e il mana gli torna una volta e mezza più in fretta: il 3% del massimo al secondo")]
        public void Mage_FavorsMagic()
        {
            var mage = Catalog.Find("mage");
            Assert.Greater(mage.Magic, mage.Vitality);
            Assert.Greater(mage.Magic, mage.Strength);
            Assert.AreEqual(StatType.Magic, mage.PointPattern[0]);
            Assert.AreEqual(0.03f, mage.ManaRegenPerSecond, 1e-6f);
            Assert.AreEqual("mage", mage.Body);

            var sheet = new StatSheet();
            sheet.SetBase(StatType.Magic, mage.Magic);
            sheet.SetBase(StatType.Vitality, mage.Vitality);
            HeroSetup.RaiseTo(new CharacterProgress(Settings, sheet), 10, mage.PointPattern);
            Assert.AreEqual(mage.Magic + 18f, sheet.GetBase(StatType.Magic), "due punti su cinque in Magia, a ogni livello: 9 livelli, 18 punti");
            Assert.AreEqual(mage.Vitality + 18f, sheet.GetBase(StatType.Vitality));
        }

        [Test, Description("Cambiare gli alberi azzera i nodi presi ma l'oggetto resta lo stesso, e chi lo ascolta lo sa: Applicato e Cambiato scattano")]
        public void SetTrees_KeepsTheObjectAndStartsOver()
        {
            var warrior = Tree("Warrior");
            var arms = Tree("WarriorArms");
            var build = new TalentBuild(warrior, new[] { arms }, 12);
            Assert.IsTrue(build.TryAdd(warrior, warrior.Find("strong_arm")));
            build.Apply();
            Assert.IsTrue(build.HasApplied);

            int applied = 0, changed = 0;
            build.Applied += () => applied++;
            build.Changed += () => changed++;
            build.SetTrees(arms, new TalentTree[0]);
            Assert.AreEqual(arms, build.ClassTree);
            Assert.AreEqual(0, build.Specializations.Count);
            Assert.IsFalse(build.HasApplied);
            Assert.IsFalse(build.HasChanges);
            Assert.AreEqual((1, 1), (applied, changed));
            Assert.Throws<System.ArgumentNullException>(() => build.SetTrees(null, null));
        }

        [Test, Description("Il prefab del giocatore ha i due corpi sullo stesso rig: i pezzi del Cavaliere e quelli del Mago, legati alle ossa della Model, e la classe")]
        public void PlayerPrefab_HasBothBodiesAndTheClass()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            Assert.IsNotNull(player.GetComponent<PlayerClass>());
            Assert.AreEqual(Catalog, player.GetComponent<PlayerClass>().Catalog);
            var body = player.GetComponentInChildren<PlayerBody>(true);
            Assert.IsNotNull(body);
            Assert.AreEqual(2, body.Count);
            var model = body.transform;
            foreach (string prefix in new[] { "Knight_", "Mage_" })
            {
                var pieces = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.name.StartsWith(prefix)).ToList();
                Assert.GreaterOrEqual(pieces.Count, 8, prefix);
                foreach (var piece in pieces)
                {
                    Assert.IsTrue(piece.bones.All(bone => bone != null && bone.IsChildOf(model)), $"{piece.name}: le ossa sono quelle della Model");
                }
            }
        }
    }
}
