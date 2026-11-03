using System.Linq;
using DarkDescent.Classes;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Editor;
using DarkDescent.Localization;
using DarkDescent.Magic;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le abilità del Mago (D3 della M21, passo 21.4): le dodici con i numeri del disegno, il colpo del sinistro che è
    /// il Dardo arcano, il rallentamento che si somma e si dimezza, il congelamento con la regola dello stordimento,
    /// le stringhe nelle due lingue.
    /// </summary>
    public class MageSpellTests
    {
        private static readonly string[] MageAssets =
        {
            "ArcaneBolt", "Firebolt", "FireRing", "Blink", "ManaShield", "Fireball", "FlameWall", "Meteor", "IceBolt", "FrostNova", "FrostArmor", "Blizzard",
        };

        private static SpellDefinition Spell(string name) => AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

        [Test, Description("I numeri del disegno: costo e ricarica delle undici abilità, e le percentuali rispetto al Dardo di fuoco (Palla 200%, Meteora 600% con la Palla a 300%, Dardo di ghiaccio 100%)")]
        public void Spells_HaveTheNumbersOfTheDesign()
        {
            Assert.AreEqual((6f, 2f), (Spell("Firebolt").ManaCost, Spell("Firebolt").Cooldown));
            Assert.AreEqual((12f, 8f, 3f), (Spell("FireRing").ManaCost, Spell("FireRing").Cooldown, Spell("FireRing").Radius));
            Assert.AreEqual((8f, 12f, 8f), (Spell("Blink").ManaCost, Spell("Blink").Cooldown, Spell("Blink").Range));
            Assert.AreEqual((10f, 40f, 12f, 2f), (Spell("ManaShield").ManaCost, Spell("ManaShield").Cooldown, Spell("ManaShield").Duration, Spell("ManaShield").ManaPerDamage));
            Assert.AreEqual((14f, 6f, 3f), (Spell("Fireball").ManaCost, Spell("Fireball").Cooldown, Spell("Fireball").BlastRadius));
            Assert.AreEqual((16f, 20f, 5f, 6f, 0.5f), (Spell("FlameWall").ManaCost, Spell("FlameWall").Cooldown, Spell("FlameWall").Length, Spell("FlameWall").Duration, Spell("FlameWall").Tick));
            Assert.AreEqual((30f, 45f, 1.2f, 4f), (Spell("Meteor").ManaCost, Spell("Meteor").Cooldown, Spell("Meteor").Delay, Spell("Meteor").Radius));
            Assert.AreEqual((8f, 3f, 40f, 3f), (Spell("IceBolt").ManaCost, Spell("IceBolt").Cooldown, Spell("IceBolt").SlowPercent, Spell("IceBolt").SlowSeconds));
            Assert.AreEqual((12f, 14f, 3.5f, 1.5f), (Spell("FrostNova").ManaCost, Spell("FrostNova").Cooldown, Spell("FrostNova").Radius, Spell("FrostNova").Freeze));
            Assert.AreEqual((12f, 35f, 40, 15f, 30f), (Spell("FrostArmor").ManaCost, Spell("FrostArmor").Cooldown, Spell("FrostArmor").ArmorPercent, Spell("FrostArmor").Duration, Spell("FrostArmor").SlowPercent));
            Assert.AreEqual((28f, 60f, 5f, 6f, 50f), (Spell("Blizzard").ManaCost, Spell("Blizzard").Cooldown, Spell("Blizzard").Radius, Spell("Blizzard").Duration, Spell("Blizzard").SlowPercent));

            // a Magia 0 il danno medio sta nelle proporzioni del disegno, rispetto al Dardo di fuoco
            float Average(string name) => (Spell(name).MinDamage + Spell(name).MaxDamage) / 2f;
            float fire = Average("Firebolt");
            Assert.AreEqual(2f, Average("Fireball") / fire, 0.05f, "la Palla: 200% del Dardo di fuoco");
            Assert.AreEqual(1f, Average("IceBolt") / fire, 0.05f, "il Dardo di ghiaccio: 100%");
            Assert.AreEqual(3f, Average("Meteor") / Average("Fireball"), 0.05f, "la Meteora: 300% della Palla");
            Assert.AreEqual(0.4f, Average("FlameWall") / fire, 0.1f, "il Muro: il 40% a ogni battito");
            Assert.AreEqual(1.2f, Average("FireRing") / fire, 0.05f, "l'Anello: il 120%");
            Assert.AreEqual(0, Spell("FrostNova").MaxDamage, "la Nova non fa danno, solo gelo");
        }

        [Test, Description("I tipi: nessuna delle dodici è del Guerriero; solo il Dardo arcano e il Passo di luce, lo Scudo, la Meteora e le aree che restano sono tipi del Mago, e quelle che vogliono un punto lo dicono")]
        public void Spells_AreOfTheMage()
        {
            foreach (string name in MageAssets)
            {
                Assert.IsFalse(Spell(name).IsWarriorAbility, name);
                Assert.IsFalse(string.IsNullOrEmpty(Spell(name).Id), name);
                Assert.IsNotNull(Spell(name).Icon, $"{name}: un'icona, anche provvisoria");
            }

            Assert.AreEqual(12, MageAssets.Select(n => Spell(n).Id).Distinct().Count());
            foreach (string name in new[] { "ArcaneBolt", "Blink", "ManaShield", "Meteor", "FlameWall", "Blizzard" })
            {
                Assert.IsTrue(Spell(name).IsMageAbility, name);
            }

            foreach (string name in new[] { "Firebolt", "FireRing", "Fireball", "IceBolt", "FrostNova", "FrostArmor" })
            {
                Assert.IsFalse(Spell(name).IsMageAbility, $"{name}: un tipo di sempre, con i numeri nuovi");
            }

            foreach (string name in new[] { "Firebolt", "Fireball", "IceBolt", "Blink", "Meteor", "FlameWall", "Blizzard" })
            {
                Assert.IsTrue(Spell(name).NeedsPoint, name);
            }

            foreach (string name in new[] { "FireRing", "FrostNova", "FrostArmor", "ManaShield" })
            {
                Assert.IsFalse(Spell(name).NeedsPoint, name);
            }

            Assert.IsFalse(Spell("Attack").IsMageAbility);
            Assert.IsTrue(Spell("Attack").IsWarriorAbility, "il Guerriero resta com'era");
        }

        [Test, Description("Il sinistro: il Dardo arcano non costa e non ricarica, quindi sta nella casella del sinistro; il Guerriero ha l'Attacco, il Mago il Dardo")]
        public void LeftSlot_IsTheArcaneBolt()
        {
            var bolt = Spell("ArcaneBolt");
            Assert.AreEqual(SpellKind.ArcaneBolt, bolt.Kind);
            Assert.IsTrue(SpellBook.IsPrimary(bolt));
            Assert.IsTrue(SpellBook.Fits(SpellBook.Primary, bolt));
            Assert.IsFalse(SpellBook.Fits(SpellBook.Secondary, bolt));
            Assert.AreEqual((0, 0), (bolt.MinDamage, bolt.MaxDamage), "il danno è quello dell'arma più la Magia");

            var catalog = AssetDatabase.LoadAssetAtPath<CharacterClassCatalog>(ClassTools.CatalogPath);
            Assert.AreEqual(bolt, catalog.Find("mage").AttackSpell);
            Assert.AreEqual(Spell("Attack"), catalog.Warrior.AttackSpell);

            var book = new SpellBook();
            book.Learn(Spell("Attack"));
            book.Learn(Spell("Firebolt"));
            book.Forget(Spell("Attack"));
            book.Learn(bolt);
            Assert.AreSame(bolt, book[SpellBook.Primary]);
            Assert.AreSame(Spell("Firebolt"), book[SpellBook.Secondary], "le altre caselle restano");
        }

        [Test, Description("I numeri che l'albero cambia: il rallentamento e il congelamento nei loro parametri, il raggio dello scoppio per un proiettile e dell'area per le altre")]
        public void AbilityParams_ReadTheNewNumbers()
        {
            Assert.AreEqual(3f, TalentBuild.BaseValue(Spell("IceBolt"), AbilityParam.Slow));
            Assert.AreEqual(1.5f, TalentBuild.BaseValue(Spell("FrostNova"), AbilityParam.Freeze));
            Assert.AreEqual(3f, TalentBuild.BaseValue(Spell("Fireball"), AbilityParam.Radius), "lo scoppio");
            Assert.AreEqual(3.5f, TalentBuild.BaseValue(Spell("FrostNova"), AbilityParam.Radius), "l'area");
            Assert.AreEqual(0f, TalentBuild.BaseValue(Spell("Firebolt"), AbilityParam.Radius), "il Dardo di fuoco non scoppia");
            Assert.AreEqual(12f, TalentBuild.BaseValue(Spell("ManaShield"), AbilityParam.Duration));
        }

        [Test, Description("Il rallentamento: la parte e il tempo; già rallentato tiene la più forte e il più lungo e non si somma; il re e i campioni ne prendono metà; niente tempo di pausa; a fine tempo avvisa")]
        public void SlowTimer_KeepsTheStrongestAndLongest()
        {
            var slow = new SlowTimer();
            int ended = 0;
            slow.Ended += () => ended++;
            Assert.IsFalse(slow.IsSlowed);
            Assert.IsFalse(slow.Apply(0f, 3f));
            Assert.IsFalse(slow.Apply(0.4f, 0f));

            Assert.IsTrue(slow.Apply(0.4f, 3f));
            Assert.AreEqual((0.4f, 3f), (slow.Fraction, slow.Remaining));
            slow.Advance(1f);
            Assert.IsTrue(slow.Apply(0.3f, 1f));
            Assert.AreEqual((0.4f, 2f), (slow.Fraction, slow.Remaining), "né più debole né più corto");
            Assert.IsTrue(slow.Apply(0.5f, 5f));
            Assert.AreEqual((0.5f, 5f), (slow.Fraction, slow.Remaining), "più forte e più lungo");

            slow.Advance(5f);
            Assert.IsFalse(slow.IsSlowed);
            Assert.AreEqual((0f, 1), (slow.Fraction, ended));
            Assert.IsTrue(slow.Apply(0.4f, 1f), "nessun tempo di pausa: subito di nuovo");

            Assert.AreEqual(0.2f, SlowTimer.Scaled(0.4f, true), 0.0001f, "il re e i campioni: metà");
            Assert.AreEqual(0.4f, SlowTimer.Scaled(0.4f, false), 0.0001f);
            Assert.AreEqual(1f, SlowTimer.Scaled(3f, false), "mai più del tutto");
            slow.Clear();
            Assert.IsFalse(slow.IsSlowed);
        }

        [Test, Description("Ogni nome e ogni descrizione delle dodici abilità c'è in inglese e in italiano")]
        public void Strings_ExistInBothLanguages()
        {
            var table = StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text);
            var localizer = new Localizer(table);
            foreach (string name in MageAssets)
            {
                string key = Spell(name).NameKey;
                foreach (string language in new[] { "en", "it" })
                {
                    localizer.SetLanguage(language);
                    Assert.AreNotEqual(key, localizer.Get(key), $"{key} in {language}");
                    Assert.AreNotEqual(key + ".desc", localizer.Get(key + ".desc"), $"{key}.desc in {language}");
                }
            }

            localizer.SetLanguage("it");
            Assert.AreEqual("Dardo arcano", localizer.Get(Spell("ArcaneBolt").NameKey));
            Assert.AreEqual("Passo di luce", localizer.Get(Spell("Blink").NameKey));
        }

        [Test, Description("Il prefab del giocatore ha le abilità del Mago collegate: i nemici, l'altoparlante, i sei effetti e i tre suoni")]
        public void PlayerPrefab_HasTheMageAbilities()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var abilities = player.GetComponent<MageAbilities>();
            Assert.IsNotNull(abilities);
            var so = new SerializedObject(abilities);
            Assert.AreNotEqual(0, so.FindProperty("_targetLayers").intValue);
            foreach (string field in new[] { "_audio", "_blinkEffect", "_shieldEffect", "_meteorEffect", "_meteorImpact", "_wallEffect", "_snowEffect", "_blinkClip", "_shieldClip", "_meteorClip" })
            {
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);
            }
        }
    }
}
