using System.Collections.Generic;
using System.Linq;
using DarkDescent.Magic;
using DarkDescent.Stats;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le regole dell'albero delle abilità (D6, D10, D11 e D13 della M20) su due alberi piccoli fatti
    /// apposta: punti dal livello, legami con tutti i gradi, linee che contano i punti sopra, nodi a
    /// scelta, specializzazione dal livello 10, *Applica* e *Annulla*, l'altare e il salvataggio che
    /// non lancia mai.
    /// </summary>
    public class TalentBuildTests
    {
        private TalentTree _class;
        private TalentTree _spec;
        private SpellDefinition _rootSpell;
        private SpellDefinition _specSpell;

        // la classe:        root
        //            a (2 gradi)   b
        //  e (scudo)       c (dopo 2 punti sopra)
        //                  d (a scelta)
        [SetUp]
        public void SetUp()
        {
            _rootSpell = ScriptableObject.CreateInstance<SpellDefinition>();
            _specSpell = ScriptableObject.CreateInstance<SpellDefinition>();
            _class = ScriptableObject.CreateInstance<TalentTree>();
            _class.Define("test", "talent.tree.test", false, new[]
            {
                new TalentNode("root", 0, 1, 1, null, new TalentOption("talent.root", _rootSpell)),
                new TalentNode("a", 1, 0, 2, new[] { "root" }, new TalentOption("talent.a", null, TalentEffect.ForStat(StatType.DamageBonus, 3f))),
                new TalentNode("b", 1, 2, 1, new[] { "root" }, new TalentOption("talent.b", null, TalentEffect.ForAbility(_rootSpell, AbilityParam.Cooldown, -2f))),
                new TalentNode("c", 2, 1, 1, new[] { "a", "b" }, new TalentOption("talent.c", null, TalentEffect.OnTrigger(TalentTrigger.Hit, TalentOutcome.Mana, 1f))),
                new TalentNode("e", 2, 0, 1, new[] { "a" }, new TalentOption("talent.e", null,
                    TalentEffect.ForStat(StatType.Armor, 6f, ModifierKind.Percent, TalentCondition.Shield))),
                new TalentNode("d", 3, 1, 1, new[] { "c" },
                    new TalentOption("talent.d1", null, TalentEffect.ForStat(StatType.LifeSteal, 3f)),
                    new TalentOption("talent.d2", null, TalentEffect.ForStat(StatType.AttackSpeed, 8f))),
            }, new[] { new TalentGate(2, 2) });

            _spec = ScriptableObject.CreateInstance<TalentTree>();
            _spec.Define("test.spec", "talent.tree.test.spec", true, new[]
            {
                new TalentNode("s0", 0, 1, 1, null, new TalentOption("talent.s0", _specSpell)),
                new TalentNode("s1", 1, 1, 2, new[] { "s0" }, new TalentOption("talent.s1", null, TalentEffect.ForStat(StatType.MaxLifePercent, 5f))),
            }, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_class);
            Object.DestroyImmediate(_spec);
            Object.DestroyImmediate(_rootSpell);
            Object.DestroyImmediate(_specSpell);
        }

        private TalentBuild Build(int level) => new TalentBuild(_class, new[] { _spec }, level);

        private TalentNode Node(string id) => _class.Find(id) ?? _spec.Find(id);

        [Test, Description("I punti vengono dal livello: dal 2 al 9 nella classe, dal 10 i pari nella specializzazione e i dispari nella classe; al 50 sono 28 e 21")]
        public void Points_FollowTheLevel()
        {
            var table = new[] { (1, 0, 0), (2, 1, 0), (9, 8, 0), (10, 8, 1), (11, 9, 1), (20, 13, 6), (21, 14, 6), (30, 18, 11), (32, 19, 12), (50, 28, 21) };
            foreach (var (level, cls, spec) in table)
            {
                Assert.AreEqual((cls, spec), (TalentPoints.ForClass(level), TalentPoints.ForSpecialization(level)), $"livello {level}");
            }
        }

        [Test, Description("Al livello 1 la radice è già presa, gratis, e non si può prendere altro")]
        public void LevelOne_HasOnlyTheFreeRoot()
        {
            var build = Build(1);
            Assert.AreEqual(1, build.Rank(_class, Node("root")));
            Assert.AreEqual(0, build.Available(_class));
            Assert.IsFalse(build.CanAdd(_class, Node("a")));
            CollectionAssert.AreEqual(new[] { _rootSpell }, build.Abilities(), "l'abilità della radice è nella barra dall'inizio");
        }

        [Test, Description("Un nodo si apre quando un legame ha tutti i gradi, e dopo una linea quando sopra ci sono abbastanza punti; i gradi hanno un massimo e i punti finiscono")]
        public void Nodes_OpenWithFullLinksAndGates()
        {
            var build = Build(4);
            Assert.IsTrue(build.TryAdd(_class, Node("b")));
            Assert.IsFalse(build.CanAdd(_class, Node("c")), "b è pieno, ma sopra la linea c'è un punto solo");
            Assert.IsTrue(build.TryAdd(_class, Node("a")));
            Assert.IsFalse(build.CanAdd(_class, Node("e")), "a ha un grado su due");
            Assert.IsTrue(build.CanAdd(_class, Node("c")), "due punti sopra la linea e b pieno");
            Assert.IsTrue(build.TryAdd(_class, Node("a")));
            Assert.IsFalse(build.CanAdd(_class, Node("a")), "a ha già tutti i gradi");
            Assert.AreEqual(0, build.Available(_class), "tre punti al livello 4, tutti spesi");
            Assert.IsFalse(build.CanAdd(_class, Node("c")), "niente punti");
        }

        [Test, Description("Un nodo a scelta prende una sola delle due opzioni, e l'altra resta chiusa")]
        public void ChoiceNode_TakesOneOption()
        {
            var build = Build(10);
            build.TryAdd(_class, Node("a"));
            build.TryAdd(_class, Node("a"));
            build.TryAdd(_class, Node("c"));
            Assert.IsTrue(build.TryAdd(_class, Node("d"), 1));
            Assert.IsFalse(build.CanAdd(_class, Node("d"), 0));
            Assert.AreEqual(1, build.OptionOf(_class, Node("d")));
            build.Apply();
            var modifiers = new List<StatModifier>();
            build.CollectModifiers(modifiers, this, null);
            Assert.IsTrue(modifiers.Any(m => m.Stat == StatType.AttackSpeed && m.Value == 8f));
            Assert.IsFalse(modifiers.Any(m => m.Stat == StatType.LifeSteal));
        }

        [Test, Description("Prima di Applica si toglie gratis, ma non un nodo da cui un altro dipende; dopo Applica non si toglie più, e Annulla torna allo stato applicato")]
        public void PendingChanges_ApplyAndCancel()
        {
            var build = Build(5);
            int applied = 0;
            build.Applied += () => applied++;
            build.TryAdd(_class, Node("a"));
            build.TryAdd(_class, Node("a"));
            build.TryAdd(_class, Node("c"));
            Assert.IsTrue(build.HasChanges);
            Assert.IsFalse(build.CanRemove(_class, Node("a")), "c resterebbe senza legame pieno");
            Assert.IsTrue(build.TryRemove(_class, Node("c")));
            Assert.IsTrue(build.TryAdd(_class, Node("c")));

            build.Apply();
            Assert.AreEqual(1, applied);
            Assert.IsFalse(build.HasChanges);
            Assert.IsFalse(build.CanRemove(_class, Node("c")), "applicato: si riparte solo dall'altare");

            build.TryAdd(_class, Node("b"));
            build.Cancel();
            Assert.AreEqual(0, build.Rank(_class, Node("b")));
            Assert.AreEqual(1, build.Rank(_class, Node("c")));
            Assert.AreEqual(1, applied, "annullare non tocca lo stato applicato");
        }

        [Test, Description("Solo lo stato applicato dà modificatori; quelli a una condizione arrivano solo se la condizione vale")]
        public void Modifiers_ComeFromTheAppliedState()
        {
            var build = Build(4);
            build.TryAdd(_class, Node("a"));
            build.TryAdd(_class, Node("a"));
            build.TryAdd(_class, Node("e"));
            var modifiers = new List<StatModifier>();
            build.CollectModifiers(modifiers, this, null);
            Assert.AreEqual(0, modifiers.Count, "niente prima di Applica");

            build.Apply();
            build.CollectModifiers(modifiers, this, null);
            Assert.AreEqual(1, modifiers.Count, "lo scudo non c'è");
            Assert.AreEqual((StatType.DamageBonus, 6f), (modifiers[0].Stat, modifiers[0].Value), "due gradi da 3");
            Assert.AreSame(this, modifiers[0].Source);

            modifiers.Clear();
            build.CollectModifiers(modifiers, this, condition => condition != TalentCondition.LowLife);
            Assert.IsTrue(modifiers.Any(m => m.Stat == StatType.Armor && m.Kind == ModifierKind.Percent && m.Value == 6f), "con lo scudo");
        }

        [Test, Description("I numeri delle abilità e gli effetti che scattano si sommano per grado")]
        public void AbilityBonusesAndTriggers()
        {
            var build = Build(5);
            build.TryAdd(_class, Node("b"));
            build.TryAdd(_class, Node("a"));
            build.TryAdd(_class, Node("c"));
            build.Apply();
            Assert.AreEqual(-2f, build.AbilityBonus(_rootSpell, AbilityParam.Cooldown));
            Assert.AreEqual(0f, build.AbilityBonus(_rootSpell, AbilityParam.Radius));
            var hits = build.Triggers(TalentTrigger.Hit);
            Assert.AreEqual(1, hits.Count);
            Assert.AreEqual((TalentOutcome.Mana, 1f), (hits[0].effect.Outcome, hits[0].effect.Value(hits[0].rank)));
            Assert.AreEqual(0, build.Triggers(TalentTrigger.Kill).Count);
        }

        [Test, Description("La specializzazione si sceglie dal livello 10, con la sua radice gratis e i suoi punti; non applicata si lascia, e l'altare toglie tutto")]
        public void Specialization_FromLevelTen()
        {
            var build = Build(9);
            Assert.IsFalse(build.CanChoose(_spec));
            build.SetLevel(10);
            Assert.IsTrue(build.TryChoose(_spec));
            Assert.AreEqual(1, build.Rank(_spec, Node("s0")));
            Assert.AreEqual(1, build.Available(_spec));
            Assert.IsTrue(build.TryUnchoose());
            Assert.IsTrue(build.TryChoose(_spec));
            Assert.IsTrue(build.TryAdd(_spec, Node("s1")));
            build.Apply();
            Assert.AreSame(_spec, build.AppliedSpecialization);
            Assert.IsFalse(build.TryUnchoose(), "applicata: si cambia solo all'altare");
            CollectionAssert.AreEqual(new[] { _rootSpell, _specSpell }, build.Abilities());

            build.Reset();
            Assert.IsNull(build.AppliedSpecialization);
            Assert.AreEqual(0, build.Rank(_spec, Node("s1")));
            Assert.AreEqual(1, build.Rank(_class, Node("root")), "la radice della classe resta");
        }

        [Test, Description("Esportato e rimesso, lo stato applicato torna uguale; quello in attesa no")]
        public void ExportAndRestore_RoundTrip()
        {
            var build = Build(12);
            build.TryAdd(_class, Node("a"));
            build.TryAdd(_class, Node("a"));
            build.TryAdd(_class, Node("c"));
            build.TryAdd(_class, Node("d"), 1);
            build.TryChoose(_spec);
            build.TryAdd(_spec, Node("s1"));
            build.Apply();
            build.TryAdd(_class, Node("b"));
            var records = build.Export();

            var again = Build(1);
            again.Restore(12, _spec.Id, records);
            Assert.AreSame(_spec, again.AppliedSpecialization);
            Assert.AreEqual((2, 1, 1, 1), (again.Rank(_class, Node("a")), again.Rank(_class, Node("c")), again.OptionOf(_class, Node("d")), again.Rank(_spec, Node("s1"))));
            Assert.AreEqual(0, again.Rank(_class, Node("b")), "b non era applicato");
            Assert.IsFalse(again.HasChanges);
        }

        [Test, Description("Il salvataggio non lancia mai: id sconosciuti, gradi oltre il massimo, nodi senza legame o oltre i punti si lasciano, e sotto il livello 10 la specializzazione non c'è")]
        public void Restore_IsTolerant()
        {
            var records = new List<TalentRecord>
            {
                new TalentRecord("test", "nope", 1, 0),
                new TalentRecord("other", "a", 1, 0),
                new TalentRecord("test", "a", 9, 0),
                new TalentRecord("test", "c", 1, 0),
                new TalentRecord("test", "e", 1, 0),
                new TalentRecord("test", "d", 1, 7),
                new TalentRecord("test.spec", "s1", 2, 0),
            };
            var build = Build(1);
            build.Restore(4, _spec.Id, records);
            Assert.IsNull(build.AppliedSpecialization, "al livello 4 nessuna specializzazione");
            Assert.AreEqual(2, build.Rank(_class, Node("a")), "al massimo due gradi");
            Assert.AreEqual(1, build.Rank(_class, Node("e")), "riga per riga e da sinistra: e prende il terzo e ultimo punto");
            Assert.AreEqual(0, build.Rank(_class, Node("c")), "per c i punti sono finiti");
            Assert.AreEqual(-1, build.OptionOf(_class, Node("d")), "un'opzione che non c'è");
            Assert.AreEqual(0, build.Available(_class));

            build.Restore(4, "", null);
            Assert.AreEqual(0, build.Rank(_class, Node("a")));
        }
    }
}
