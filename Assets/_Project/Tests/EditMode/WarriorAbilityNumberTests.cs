using DarkDescent.Combat;
using DarkDescent.Magic;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>
    /// I numeri delle abilità del Guerriero (passo 20.4 della M20): quelli del disegno negli asset, i
    /// nodi dell'albero che li cambiano, la regola dello stordimento, il danno dei critici e quello
    /// preso in meno, il costo e la ricarica che il lanciatore legge dall'albero.
    /// </summary>
    public class WarriorAbilityNumberTests
    {
        private static SpellDefinition Spell(string name) => AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

        private static TalentTree Tree(string name) => AssetDatabase.LoadAssetAtPath<TalentTree>($"Assets/_Project/Data/Talents/{name}.asset");

        private static TalentBuild Build(int level) => new TalentBuild(Tree("Warrior"), new[] { Tree("WarriorDefense"), Tree("WarriorArms") }, level);

        [Test, Description("Gli asset hanno i numeri del disegno: il danno in percentuale dell'arma, gli stordimenti, la gittata della Carica e del Salto, l'Esecuzione, il Grido, il Muro, l'Ultimo baluardo e la Tempesta")]
        public void Abilities_HaveTheDesignNumbers()
        {
            Assert.AreEqual(160f, Spell("PowerStrike").WeaponPercent);
            var charge = Spell("Charge");
            Assert.AreEqual((100f, 1f, 3f, 10f), (charge.WeaponPercent, charge.Stun, charge.MinRange, charge.Range));
            var shout = Spell("BattleShout");
            Assert.AreEqual((15f, 12f, 5f), (shout.BonusPercent, shout.Duration, shout.Radius));
            Assert.AreEqual((100f, 1.5f), (Spell("ShieldBash").WeaponPercent, Spell("ShieldBash").Stun));
            Assert.AreEqual((5f, 0.5f), (Spell("ShieldWall").Duration, Spell("ShieldWall").MoveFactor));
            Assert.AreEqual((30f, 10f), (Spell("LastStand").BonusPercent, Spell("LastStand").Duration));
            Assert.AreEqual((90f, 3f), (Spell("Whirlwind").WeaponPercent, Spell("Whirlwind").Radius));
            var execute = Spell("Execute");
            Assert.AreEqual((100f, 250f, 25f), (execute.WeaponPercent, execute.BonusPercent, execute.Threshold));
            var leap = Spell("Leap");
            Assert.AreEqual((120f, 1f, 3f, 8f), (leap.WeaponPercent, leap.Stun, leap.Radius, leap.Range));
            var storm = Spell("Bladestorm");
            Assert.AreEqual((60f, 3.5f, 3f, 0.7f, 0.5f), (storm.WeaponPercent, storm.Radius, storm.Duration, storm.MoveFactor, storm.Tick));
            foreach (var name in new[] { "PowerStrike", "Charge", "BattleShout", "ShieldBash", "ShieldWall", "LastStand", "Whirlwind", "Execute", "Leap", "Bladestorm" })
            {
                Assert.IsTrue(Spell(name).IsWarriorAbility, name);
            }

            Assert.IsFalse(Spell("Firebolt").IsWarriorAbility || Spell("StoneSkin").IsWarriorAbility);
        }

        [Test, Description("I nodi cambiano i numeri delle abilità applicati: Slancio accorcia la Carica a 4 s, Seconda pelle allunga la Pelle di pietra e la fa costare meno, il Boia alza la soglia dell'Esecuzione, il Vortice allarga il Turbine; mai sotto zero")]
        public void AbilityValue_AddsTheAppliedNodes()
        {
            var build = Build(50);
            var warrior = build.ClassTree;
            Assert.AreEqual(8f, build.AbilityValue(Spell("Charge"), AbilityParam.Cooldown), "senza nodi, quello dell'asset");
            Assert.IsTrue(build.TryAdd(warrior, warrior.Find("charge")));
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(build.TryAdd(warrior, warrior.Find("momentum")));
            }

            Assert.AreEqual(8f, build.AbilityValue(Spell("Charge"), AbilityParam.Cooldown), "in attesa non conta");
            build.Apply();
            Assert.AreEqual(4f, build.AbilityValue(Spell("Charge"), AbilityParam.Cooldown));
            Assert.AreEqual(0f, build.AbilityValue(Spell("Charge"), AbilityParam.Radius), "la Carica furiosa non è presa");
            Assert.AreEqual(1f, build.AbilityValue(Spell("Charge"), AbilityParam.Stun));

            var arms = build.Specializations[1];
            Assert.IsTrue(build.TryChoose(arms));
            foreach (var (node, ranks) in new[] { ("keen_edge", 2), ("weak_spot", 1), ("vortex", 2) })
            {
                for (int i = 0; i < ranks; i++)
                {
                    Assert.IsTrue(build.TryAdd(arms, arms.Find(node)), node);
                }
            }

            build.Apply();
            Assert.AreEqual(4f, build.AbilityValue(Spell("Whirlwind"), AbilityParam.Radius), "3 m più due gradi di Vortice");
            Assert.AreEqual(120f, build.AbilityValue(Spell("Whirlwind"), AbilityParam.Damage), "90% più due gradi di Filo affilato");
            Assert.AreEqual(25f, build.AbilityValue(Spell("Execute"), AbilityParam.Threshold));
            Assert.AreEqual(12f, TalentBuild.BaseValue(Spell("Whirlwind"), AbilityParam.ManaCost));
        }

        [Test, Description("La regola dello stordimento: intero per i nemici normali, metà per re, campioni e unici, niente per gli Inamovibili; mentre dura e per 3 s dopo non se ne prende un altro")]
        public void StunTimer_FollowsTheRule()
        {
            Assert.AreEqual(1.5f, StunTimer.Scaled(1.5f, false, false));
            Assert.AreEqual(0.75f, StunTimer.Scaled(1.5f, true, false), "il re e i campioni a metà");
            Assert.AreEqual(0f, StunTimer.Scaled(1.5f, true, true), "gli Inamovibili mai");

            var stun = new StunTimer();
            int ended = 0;
            stun.Ended += () => ended++;
            Assert.IsFalse(stun.TryStun(0f));
            Assert.IsTrue(stun.TryStun(1f));
            Assert.IsFalse(stun.TryStun(2f), "mentre dura non si allunga");
            stun.Advance(0.6f);
            Assert.IsTrue(stun.IsStunned);
            stun.Advance(0.6f);
            Assert.IsFalse(stun.IsStunned);
            Assert.AreEqual(1, ended);
            Assert.AreEqual(StunTimer.Immunity, stun.ImmuneFor);
            Assert.IsFalse(stun.TryStun(1f), "appena finito, 3 s di tregua");
            stun.Advance(2.9f);
            Assert.IsFalse(stun.TryStun(1f));
            stun.Advance(0.2f);
            Assert.IsTrue(stun.TryStun(1f), "passati i 3 s");
            stun.Clear();
            Assert.IsFalse(stun.IsStunned);
            Assert.AreEqual(0f, stun.ImmuneFor);
        }

        [Test, Description("Il Punto debole alza il critico da ×2 a ×2,3 con tre gradi; il danno preso in meno in percentuale si ferma al 75%, dopo i punti tolti")]
        public void Formulas_CritDamageAndResist()
        {
            Assert.AreEqual(2f, CombatFormulas.CritMultiplierFor(0f));
            Assert.AreEqual(2.3f, CombatFormulas.CritMultiplierFor(30f), 0.0001f);
            Assert.AreEqual(20f, CombatFormulas.ResistDamage(20f, 0f));
            Assert.AreEqual(14f, CombatFormulas.ResistDamage(20f, 30f), 0.0001f, "l'Ultima difesa");
            Assert.AreEqual(5f, CombatFormulas.ResistDamage(20f, 90f), 0.0001f, "al più il 75%");
        }

        [Test, Description("Il lanciatore legge costo e ricarica da chi conosce l'albero: spende quello e la ricarica parte da quella; la frazione del velo è di quella partita")]
        public void Caster_UsesTheTreeNumbers()
        {
            var charge = Spell("Charge");
            var book = new SpellBook(new[] { charge });
            var caster = new SpellCaster(book, new ManaPool(30f));
            caster.SetNumbers(spell => spell.ManaCost - 4f, spell => spell.Cooldown - 4f);
            Assert.AreEqual((6f, 4f), (caster.ManaCost(charge), caster.Cooldown(charge)));
            Assert.AreEqual(CastResult.Ready, caster.TryCast(0));
            Assert.AreEqual(24f, caster.Mana.Current);
            caster.Advance(1f);
            Assert.AreEqual(0.75f, caster.CooldownFraction(0), 0.0001f);
            caster.SetNumbers(null, null);
            Assert.AreEqual(0.75f, caster.CooldownFraction(0), 0.0001f, "la ricarica partita resta quella");
            Assert.AreEqual(10f, caster.ManaCost(charge), "di nuovo quello dell'asset");
        }
    }
}
