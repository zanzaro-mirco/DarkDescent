using System.Linq;
using DarkDescent.Classes;
using DarkDescent.Editor;
using DarkDescent.Magic;
using DarkDescent.Stats;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Gli alberi del Mago come li ha approvati Mirco (passo 21.1 della M21): forma, numeri di nodi e gradi, le linee ai livelli
    /// del disegno, ogni nodo raggiungibile e al livello 50 i punti spendibili, la classe che li ha e le sue abilità attive.
    /// Li costruisce <c>MageTalentTools.BuildMage</c>.
    /// </summary>
    public class MageTreeTests
    {
        private static TalentTree Tree(string name) => AssetDatabase.LoadAssetAtPath<TalentTree>($"Assets/_Project/Data/Talents/{name}.asset");

        private static TalentTree Mage => Tree("Mage");
        private static TalentTree Fire => Tree("MageFire");
        private static TalentTree Frost => Tree("MageFrost");

        private static SpellDefinition Spell(string name) => AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

        [Test, Description("I tre alberi hanno la forma giusta e i numeri del disegno: 19 nodi e 34 gradi nella classe, 15 e 29 nel Fuoco, 15 e 27 nel Gelo; la radice è un'abilità")]
        public void Trees_MatchTheDesign()
        {
            foreach (var (tree, id, specialization, nodes, ranks, actives) in new[]
            {
                (Mage, "mage", false, 19, 34, 4),
                (Fire, "mage.fire", true, 15, 29, 3),
                (Frost, "mage.frost", true, 15, 27, 4),
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

        [Test, Description("Le undici abilità del 21.4 stanno ognuna nel suo albero: quattro nella classe, tre nel Fuoco, quattro nel Gelo; il Dardo arcano è del sinistro e non è in nessun albero")]
        public void Abilities_SitWhereTheDesignPutsThem()
        {
            string[] Actives(TalentTree tree) => tree.Nodes.Where(n => n.Kind == TalentNodeKind.Active).Select(n => n.Options[0].Ability.name).OrderBy(n => n, System.StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(new[] { "Blink", "FireRing", "Firebolt", "ManaShield" }, Actives(Mage));
            CollectionAssert.AreEqual(new[] { "Fireball", "FlameWall", "Meteor" }, Actives(Fire));
            CollectionAssert.AreEqual(new[] { "Blizzard", "FrostArmor", "FrostNova", "IceBolt" }, Actives(Frost));
            Assert.AreEqual("Firebolt", Mage.Root.Options[0].Ability.name, "gratis al livello 1");
            Assert.AreEqual("Fireball", Fire.Root.Options[0].Ability.name);
            Assert.AreEqual("IceBolt", Frost.Root.Options[0].Ability.name);
        }

        [Test, Description("Le linee si passano ai livelli del disegno, come per il Guerriero: la classe dopo 6 e 14 punti, le specializzazioni dopo 5 e 12")]
        public void Gates_OpenAtTheDesignLevels()
        {
            Assert.AreEqual((6, 14), (Mage.PointsFor(3), Mage.PointsFor(6)));
            foreach (var spec in new[] { Fire, Frost })
            {
                Assert.AreEqual((5, 12), (spec.PointsFor(3), spec.PointsFor(5)), spec.Id);
            }
        }

        [Test, Description("Ogni nodo si può prendere con tutti i suoi gradi, e al livello 50 si spendono tutti i 28 e 21 punti, in tutte e due le specializzazioni")]
        public void EveryNode_IsReachable()
        {
            foreach (var spec in new[] { Fire, Frost })
            {
                var build = new TalentBuild(Mage, new[] { Fire, Frost }, 200);
                Assert.IsTrue(build.TryChoose(spec));
                Fill(build, Mage);
                Fill(build, spec);
                foreach (var tree in new[] { Mage, spec })
                {
                    foreach (var node in tree.Nodes)
                    {
                        Assert.AreEqual(node.Ranks, build.Rank(tree, node), $"{tree.Id}/{node.Id}");
                    }
                }

                var capped = new TalentBuild(Mage, new[] { Fire, Frost }, 50);
                Assert.IsTrue(capped.TryChoose(spec));
                Fill(capped, Mage);
                Fill(capped, spec);
                Assert.AreEqual((28, 21), (capped.Spent(Mage), capped.Spent(spec)), spec.Id);
                Assert.Less(capped.Spent(Mage), Mage.TotalRanks - 1, "la classe non si riempie");
                Assert.Less(capped.Spent(spec), spec.TotalRanks - 1, "la specializzazione non si riempie");
            }
        }

        [Test, Description("Il Mago ha i suoi alberi: la classe e le due specializzazioni, ed è una classe che si può scegliere; il Guerriero non è cambiato")]
        public void MageClass_HasItsTrees()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterClassCatalog>(ClassTools.CatalogPath);
            var mage = catalog.Find("mage");
            Assert.AreSame(Mage, mage.ClassTree);
            CollectionAssert.AreEqual(new[] { Fire, Frost }, mage.Specializations);
            Assert.IsTrue(mage.IsPlayable);
            Assert.AreEqual("warrior", catalog.Warrior.ClassTree.Id);
        }

        [Test, Description("I nodi a scelta del disegno: Scintilla o Fiammata, Pioggia di scintille o Anello infuocato, Meteora doppia o Marea di fuoco, Gelo che resta o Nova doppia; ognuno dà quello che dice")]
        public void Choices_DoWhatTheyDescribe()
        {
            var spark = Mage.Find("fire_craft").Options[0].Effects.Single();
            Assert.AreEqual((TalentEffectKind.Trigger, TalentTrigger.BoltHit, TalentOutcome.Cast, 10f), (spark.Kind, spark.Trigger, spark.Outcome, spark.PerRank));
            Assert.AreSame(Spell("Firebolt"), spark.Ability);

            var blaze = Mage.Find("fire_craft").Options[1].Effects.Single();
            Assert.AreEqual((Spell("Firebolt"), AbilityParam.Burn, 3f), (blaze.Ability, blaze.Param, blaze.PerRank));
            var rain = Mage.Find("fire_finale").Options[0].Effects.Single();
            Assert.AreEqual((Spell("Firebolt"), AbilityParam.Radius, 2f), (rain.Ability, rain.Param, rain.PerRank));
            var ring = Mage.Find("fire_finale").Options[1].Effects.Single();
            Assert.AreEqual((Spell("FireRing"), AbilityParam.Burn, 3f), (ring.Ability, ring.Param, ring.PerRank));

            var meteor = Fire.Find("fire_end").Options[0].Effects.Single();
            Assert.AreEqual((Spell("Meteor"), AbilityParam.Echo, 1f), (meteor.Ability, meteor.Param, meteor.PerRank));
            var tide = Fire.Find("fire_end").Options[1].Effects.Single();
            Assert.AreEqual((Spell("Fireball"), AbilityParam.Ground, 4f), (tide.Ability, tide.Param, tide.PerRank));

            var linger = Frost.Find("frost_end").Options[0].Effects.Single();
            Assert.AreEqual((Spell("FrostNova"), AbilityParam.Slow, 3f), (linger.Ability, linger.Param, linger.PerRank));
            var twice = Frost.Find("frost_end").Options[1].Effects.Single();
            Assert.AreEqual((Spell("FrostNova"), AbilityParam.Echo, 1f), (twice.Ability, twice.Param, twice.PerRank));
        }

        [Test, Description("I passivi nuovi della scheda: danno degli incantesimi, mana massimo e rigenerazione del mana, sulle righe del disegno")]
        public void NewStats_AreOnTheDesignedNodes()
        {
            float Per(TalentTree tree, string id, StatType stat) =>
                tree.Find(id).Options[0].Effects.Where(e => e.Kind == TalentEffectKind.Stat && e.Stat == stat).Sum(e => e.PerRank);
            Assert.AreEqual(3f, Per(Mage, "keen_mind", StatType.SpellDamage));
            Assert.AreEqual(4f, Per(Mage, "fire_master", StatType.SpellDamage));
            Assert.AreEqual(5f, Per(Mage, "mana_reserve", StatType.MaxManaPercent));
            Assert.AreEqual(5f, Per(Mage, "arcane_heart", StatType.MaxManaPercent));
            Assert.AreEqual(50f, Per(Mage, "cold_mind", StatType.ManaRegenPercent));
            Assert.AreEqual(4f, Per(Fire, "inflamed_mind", StatType.SpellDamage));
            Assert.AreEqual(3f, Per(Fire, "fire_mastery", StatType.SpellDamage));
            Assert.AreEqual(3f, Per(Fire, "burning_magic", StatType.Magic));
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
