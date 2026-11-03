using System.Collections.Generic;
using System.Linq;
using DarkDescent.Localization;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// I testi dei talenti del Guerriero (D10 e D15 della M20): ogni nodo, ogni opzione e ogni frase di un
    /// effetto ha il suo testo nelle due lingue, e le frasi nascono dai numeri degli effetti.
    /// </summary>
    public class TalentTextTests
    {
        private const string TablePath = "Assets/_Project/Data/Localization/Strings.csv";

        private static readonly string[] Names = { "Warrior", "WarriorDefense", "WarriorArms", "Mage", "MageFire", "MageFrost" };

        private static StringTable Table => StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(TablePath).text);

        private static TalentTree Tree(string name) => AssetDatabase.LoadAssetAtPath<TalentTree>($"Assets/_Project/Data/Talents/{name}.asset");

        private static IEnumerable<string> LanguagesOf(StringTable table) => table.Languages;

        private static string Name(TalentOption option) => option.Ability != null ? option.Ability.NameKey : option.NameKey;

        [Test, Description("Per ogni lingua: il nome di ogni albero, di ogni nodo e di ogni opzione, il titolo dei nodi a scelta, la descrizione di ogni abilità attiva e ogni frase degli effetti sono nella tabella, senza segnaposto rimasti")]
        public void EveryNodeHasItsTextInBothLanguages()
        {
            var table = Table;
            var missing = new List<string>();
            foreach (string language in LanguagesOf(table))
            {
                var localizer = new Localizer(table, language);
                void Check(string key)
                {
                    if (localizer.Get(key).StartsWith("#"))
                    {
                        missing.Add($"{language}: {key}");
                    }
                }

                foreach (string name in Names)
                {
                    var tree = Tree(name);
                    Check(tree.NameKey);
                    foreach (var node in tree.Nodes)
                    {
                        if (node.Kind == TalentNodeKind.Choice)
                        {
                            Check("talent." + node.Id);
                        }

                        foreach (var option in node.Options)
                        {
                            Check(Name(option));
                            if (option.Ability != null)
                            {
                                Check(option.Ability.NameKey + ".desc");
                            }

                            foreach (var effect in option.Effects)
                            {
                                foreach (string key in TalentDescriber.KeysOf(effect))
                                {
                                    Check(key);
                                }

                                string line = TalentDescriber.Describe(effect, 1, localizer);
                                Assert.IsFalse(line.Contains("{") || line.Contains("#"), $"{language}, {node.Id}: \"{line}\"");
                            }
                        }
                    }
                }

                foreach (string key in new[] { "talents.title", "talents.apply", "talents.cancel", "talents.spec.warrior.defense", "talents.spec.warrior.arms", "talents.spec.mage.fire", "talents.spec.mage.frost" })
                {
                    Check(key);
                }
            }

            CollectionAssert.IsEmpty(missing, string.Join("\n", missing));
        }

        [Test, Description("Le frasi nascono dai numeri: il grado moltiplica il valore, i segni seguono il numero e la condizione, il buff dice quanto e per quanto, un valore negativo accorcia")]
        public void Phrases_FollowTheNumbers()
        {
            var localizer = new Localizer(Table, "en");
            var warrior = Tree("Warrior");
            var defense = Tree("WarriorDefense");
            var arms = Tree("WarriorArms");

            string Describe(TalentTree tree, string id, int rank, int option = 0) => TalentDescriber.Describe(tree.Find(id).Options[option], rank, localizer);

            Assert.AreEqual("+2% damage", Describe(warrior, "strong_arm", 1));
            Assert.AreEqual("+6% damage", Describe(warrior, "strong_arm", 3));
            Assert.AreEqual("Charge: -2 s recharge", Describe(warrior, "momentum", 1));
            Assert.AreEqual("Charge: -4 s recharge", Describe(warrior, "momentum", 2));
            Assert.AreEqual("-30% damage taken (below 30% life)", Describe(warrior, "last_defense", 1));
            Assert.AreEqual("+1% armor (with a shield)", Describe(defense, "guardian_armor", 1));
            Assert.AreEqual("+15% damage (two-handed weapon)", Describe(arms, "two_handed_grip", 3));
            Assert.AreEqual("Every hit gives 2 mana", Describe(warrior, "discipline", 2));
            Assert.AreEqual("A blocked hit returns 50% of its damage to the attacker", Describe(defense, "retaliation", 2));
            Assert.AreEqual("Every kill: +10% attack speed for 5 s", Describe(arms, "frenzy", 2));
            Assert.AreEqual("Whirlwind: +0.5 m radius", Describe(arms, "vortex", 1));
            Assert.AreEqual("Whirlwind: -4 mana", Describe(arms, "fighting_style", 1, option: 1));
            Assert.AreEqual("-1 damage from every hit taken", Describe(defense, "thick_skin", 1));
            Assert.AreEqual("A shield stops 50% of an unblockable hit (with a shield)", Describe(defense, "fortress", 1));
        }

        [Test, Description("Le frasi del Mago: i passivi della scheda, i nodi che cambiano un'abilità (bruciatura, ripetizione, fiamme a terra), gli inneschi (a segno, congelamento) e il lancio gratis con il nome dell'abilità")]
        public void MagePhrases_FollowTheNumbers()
        {
            var localizer = new Localizer(Table, "en");
            var mage = Tree("Mage");
            var fire = Tree("MageFire");
            var frost = Tree("MageFrost");
            string Describe(TalentTree tree, string id, int rank, int option = 0) => TalentDescriber.Describe(tree.Find(id).Options[option], rank, localizer);

            Assert.AreEqual("+9% spell damage", Describe(mage, "keen_mind", 3));
            Assert.AreEqual("+10% maximum mana", Describe(mage, "mana_reserve", 2));
            Assert.AreEqual("+100% mana regeneration", Describe(mage, "cold_mind", 2));
            Assert.AreEqual("+6 magic", Describe(fire, "burning_magic", 2));
            Assert.AreEqual("Firebolt: -1 s recharge", Describe(mage, "swift_bolt", 2));
            Assert.AreEqual("Every arcane bolt that hits gives 2 mana", Describe(mage, "concentration", 2));
            Assert.AreEqual("Every arcane bolt that hits has a 10% chance to cast Firebolt for free", Describe(mage, "fire_craft", 1, option: 0));
            Assert.AreEqual("Firebolt: +3 s of burning (60% of the damage)", Describe(mage, "fire_craft", 1, option: 1));
            Assert.AreEqual("Meteor: +1 repeat, 1 s later", Describe(fire, "fire_end", 1, option: 0));
            Assert.AreEqual("Fireball: +4 s of flames left on the ground", Describe(fire, "fire_end", 1, option: 1));
            Assert.AreEqual("Frost Nova: +3 s of slowing", Describe(frost, "frost_end", 1, option: 0));
            Assert.AreEqual("Every frozen enemy gives 4 mana", Describe(frost, "ice_mind", 2));
            Assert.AreEqual("-25% damage taken (below 30% life)", Describe(mage, "last_spark", 1));
        }

        [Test, Description("In italiano le stesse frasi: i numeri uguali, il testo tradotto")]
        public void Phrases_AreTranslated()
        {
            var localizer = new Localizer(Table, "it");
            var warrior = Tree("Warrior");
            Assert.AreEqual("+2% di danno", TalentDescriber.Describe(warrior.Find("strong_arm").Options[0], 1, localizer));
            Assert.AreEqual("Carica: -2 s di ricarica", TalentDescriber.Describe(warrior.Find("momentum").Options[0], 1, localizer));
            Assert.AreEqual("Braccio forte", localizer.Get("talent.strong_arm"));
            Assert.AreEqual("Guerriero", localizer.Get(warrior.NameKey));
        }
    }
}
