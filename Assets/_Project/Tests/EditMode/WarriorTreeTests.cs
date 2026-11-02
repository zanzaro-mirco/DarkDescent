using System.Linq;
using DarkDescent.Progression;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Gli alberi del Guerriero come li ha approvati Mirco (passo 20.1 della M20): forma, numeri di
    /// nodi e gradi, le linee ai livelli del disegno, ogni nodo raggiungibile e al livello 50 tutti i
    /// punti spendibili. Li costruisce <c>TalentTools.BuildWarrior</c>.
    /// </summary>
    public class WarriorTreeTests
    {
        private static TalentTree Tree(string name) => AssetDatabase.LoadAssetAtPath<TalentTree>($"Assets/_Project/Data/Talents/{name}.asset");

        private static TalentTree Warrior => Tree("Warrior");

        private static TalentTree Defense => Tree("WarriorDefense");

        private static TalentTree Arms => Tree("WarriorArms");

        [Test, Description("I tre alberi hanno la forma giusta e i numeri del disegno: 20 nodi e 37 gradi nella classe, 15 e 28 in ogni specializzazione")]
        public void Trees_MatchTheDesign()
        {
            foreach (var (tree, id, specialization, nodes, ranks, actives) in new[]
            {
                (Warrior, "warrior", false, 20, 37, 4),
                (Defense, "warrior.defense", true, 15, 28, 3),
                (Arms, "warrior.arms", true, 15, 28, 4),
            })
            {
                Assert.IsNotNull(tree, id);
                CollectionAssert.IsEmpty(tree.Validate(), id);
                Assert.AreEqual((id, specialization, nodes, ranks), (tree.Id, tree.IsSpecialization, tree.Nodes.Count, tree.TotalRanks), id);
                Assert.AreEqual(actives, tree.Nodes.Count(n => n.Kind == TalentNodeKind.Active), $"{id}: abilità attive");
                Assert.IsTrue(tree.Nodes.Where(n => n.Kind == TalentNodeKind.Active).All(n => n.Options[0].Ability != null));
                Assert.AreEqual(TalentNodeKind.Active, tree.Root.Kind, $"{id}: la radice gratis è un'abilità");
            }
        }

        [Test, Description("Le linee si passano ai livelli del disegno: la classe al 7 e al 21, le specializzazioni al 18 e al 32")]
        public void Gates_OpenAtTheDesignLevels()
        {
            Assert.AreEqual((6, 14), (Warrior.PointsFor(3), Warrior.PointsFor(6)));
            Assert.AreEqual((5, 6), (TalentPoints.ForClass(6), TalentPoints.ForClass(7)));
            Assert.AreEqual((13, 14), (TalentPoints.ForClass(20), TalentPoints.ForClass(21)));
            foreach (var spec in new[] { Defense, Arms })
            {
                Assert.AreEqual((5, 12), (spec.PointsFor(3), spec.PointsFor(5)), spec.Id);
            }

            Assert.AreEqual((4, 5), (TalentPoints.ForSpecialization(16), TalentPoints.ForSpecialization(18)));
            Assert.AreEqual((11, 12), (TalentPoints.ForSpecialization(30), TalentPoints.ForSpecialization(32)));
        }

        [Test, Description("Ogni nodo si può prendere con tutti i suoi gradi, e al livello 50 si spendono tutti i 28 e 21 punti, in tutte e due le specializzazioni")]
        public void EveryNode_IsReachable()
        {
            foreach (var spec in new[] { Defense, Arms })
            {
                // con punti senza fine, tutto l'albero
                var build = new TalentBuild(Warrior, new[] { Defense, Arms }, 200);
                Assert.IsTrue(build.TryChoose(spec));
                Fill(build, Warrior);
                Fill(build, spec);
                foreach (var tree in new[] { Warrior, spec })
                {
                    foreach (var node in tree.Nodes)
                    {
                        Assert.AreEqual(node.Ranks, build.Rank(tree, node), $"{tree.Id}/{node.Id}");
                    }
                }

                // al livello 50, i punti finiscono prima dell'albero
                var capped = new TalentBuild(Warrior, new[] { Defense, Arms }, 50);
                Assert.IsTrue(capped.TryChoose(spec));
                Fill(capped, Warrior);
                Fill(capped, spec);
                Assert.AreEqual((28, 21), (capped.Spent(Warrior), capped.Spent(spec)), spec.Id);
                Assert.Less(capped.Spent(Warrior), Warrior.TotalRanks - 1, "la classe non si riempie");
                Assert.Less(capped.Spent(spec), spec.TotalRanks - 1, "la specializzazione non si riempie");
            }
        }

        [Test, Description("Il cavaliere ha il componente dell'albero, con il Guerriero e le sue due specializzazioni")]
        public void Player_HasTheWarriorTrees()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var talents = player.GetComponent<PlayerTalents>();
            Assert.IsNotNull(talents);
            var so = new SerializedObject(talents);
            Assert.AreSame(Warrior, so.FindProperty("_classTree").objectReferenceValue);
            var specs = so.FindProperty("_specializations");
            Assert.AreEqual(2, specs.arraySize);
            Assert.AreSame(Defense, specs.GetArrayElementAtIndex(0).objectReferenceValue);
            Assert.AreSame(Arms, specs.GetArrayElementAtIndex(1).objectReferenceValue);
        }

        // prende riga per riga ogni grado che si può, finché qualcosa entra
        private static void Fill(TalentBuild build, TalentTree tree)
        {
            for (bool added = true; added;)
            {
                added = false;
                foreach (var node in tree.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Column))
                {
                    while (build.TryAdd(tree, node))
                    {
                        added = true;
                    }
                }
            }
        }
    }
}
