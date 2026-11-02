using System;
using System.Collections;
using System.Linq;
using DarkDescent.Characters;
using DarkDescent.Classes;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Magic;
using DarkDescent.Progression;
using DarkDescent.Save;
using DarkDescent.Stats;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// La classe nel gioco (D2 della M21, passo 21.2): la classe si applica al giocatore che c'è, con il
    /// suo corpo sul rig, gli attributi di partenza, il mana e gli alberi; il salvataggio la scrive e la
    /// rimette prima di tutto il resto; <c>-class</c> e <c>-heroname</c> la scelgono da riga di comando.
    /// </summary>
    public class ClassPlayModeTests : SandboxFixture
    {
        [TearDown]
        public void ForgetArguments()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, null);
        }

        private PlayerClass Class => Player.GetComponent<PlayerClass>();

        private CharacterClass Mage => Class.Catalog.Find("mage");

        private CharacterClass Warrior => Class.Catalog.Warrior;

        // i pezzi accesi di un corpo: Knight_ o Mage_
        private int Showing(string prefix)
        {
            return Player.GetComponentsInChildren<SkinnedMeshRenderer>(false).Count(r => r.name.StartsWith(prefix));
        }

        [UnityTest, Description("Senza opzioni il personaggio è il Guerriero, con il suo corpo, i suoi attributi e il mana di tutti")]
        public IEnumerator DefaultIsTheWarrior()
        {
            yield return LoadSandbox();
            Assert.AreEqual("warrior", Class.Current.Id);
            Assert.AreEqual("", Class.CharacterName);
            Assert.AreEqual(0, Showing("Mage_"));
            Assert.AreEqual(9, Showing("Knight_"), "il cavaliere com'era");
            Assert.AreEqual(Combat.CombatFormulas.ManaRegenPerSecond, Player.GetComponent<PlayerMagic>().ManaRegenPerSecond);
        }

        [UnityTest, Description("Il Mago applicato a un personaggio nuovo: il suo corpo sul rig al posto del cavaliere, i suoi attributi, vita e mana pieni, il mana al 3% al secondo, e il Guerriero rimette tutto com'era")]
        public IEnumerator Mage_ReplacesTheKnight_AndBack()
        {
            yield return LoadSandbox();
            var sheet = Player.GetComponent<CharacterStats>().Sheet;
            var health = Player.GetComponent<Health>();
            var magic = Player.GetComponent<PlayerMagic>();
            int changes = 0;
            Class.Changed += () => changes++;

            Class.Apply(Mage, "Merlino", true);
            yield return null;
            Assert.AreEqual(("mage", "Merlino"), (Class.Current.Id, Class.CharacterName));
            Assert.AreEqual(8, Showing("Mage_"), "i pezzi del Mago");
            Assert.AreEqual(0, Showing("Knight_"), "e niente cavaliere");
            Assert.AreEqual(("mage", 8f, 17f, 35f, 20f), (Player.GetComponentInChildren<PlayerBody>(true).Current,
                sheet.GetBase(StatType.Strength), sheet.GetBase(StatType.Dexterity), sheet.GetBase(StatType.Magic), sheet.GetBase(StatType.Vitality)));
            Assert.AreEqual(health.Max, health.Current, "vita piena");
            Assert.AreEqual(magic.Mana.Max, magic.Mana.Current, "mana pieno");
            Assert.AreEqual(0.03f, magic.ManaRegenPerSecond, 1e-6f);
            Assert.Less(health.Max, 100f, "poca vita: 50 + 2 × Vitalità");

            Class.Apply(Warrior, "", true);
            yield return null;
            Assert.AreEqual(9, Showing("Knight_"));
            Assert.AreEqual(0, Showing("Mage_"));
            Assert.AreEqual((30f, 20f, 10f, 25f), (sheet.GetBase(StatType.Strength), sheet.GetBase(StatType.Dexterity), sheet.GetBase(StatType.Magic), sheet.GetBase(StatType.Vitality)));
            Assert.AreEqual(Combat.CombatFormulas.ManaRegenPerSecond, magic.ManaRegenPerSecond);
            Assert.AreEqual(2, changes);
        }

        [UnityTest, Description("Una classe con i suoi alberi li mette nello stesso TalentBuild già collegato, e riparte da zero: il pannello e il salvataggio non perdono l'oggetto")]
        public IEnumerator ClassWithTrees_ReplacesTheBuildTreesInPlace()
        {
            yield return LoadSandbox();
            var talents = Player.GetComponent<PlayerTalents>();
            var build = talents.Build;
            var arms = AssetDatabase.LoadAssetAtPath<TalentTree>("Assets/_Project/Data/Talents/WarriorArms.asset");
            int changed = 0;
            build.Changed += () => changed++;
            var other = CharacterClass.Create("test", "knight", arms, new TalentTree[0], 10f, 10f, 10f, 10f, new[] { StatType.Magic });

            Class.Apply(other, "Prova", false);
            Assert.AreSame(build, talents.Build, "lo stesso oggetto");
            Assert.AreEqual(arms, build.ClassTree);
            Assert.Greater(changed, 0, "chi ascolta se ne accorge");
            Assert.AreEqual(0, build.Specializations.Count);

            Assert.Throws<ArgumentException>(() => talents.SetClass(CharacterClass.Create("senza", "knight", null, null, 1f, 1f, 1f, 1f, null)), "una classe senza albero non si applica");
            UnityEngine.Object.DestroyImmediate(other);
        }

        [UnityTest, Description("Il salvataggio scrive la classe e il nome e li rimette per primi: un Mago salvato torna Mago, con il corpo, gli attributi salvati e il suo mana")]
        public IEnumerator Save_KeepsTheClass()
        {
            yield return LoadSandbox();
            var save = UnityEngine.Object.FindFirstObjectByType<SaveGame>();
            var sheet = Player.GetComponent<CharacterStats>().Sheet;

            Class.Apply(Mage, "Merlino", true);
            sheet.SetBase(StatType.Magic, 41f);
            var data = save.Capture();
            Assert.AreEqual(("mage", "Merlino"), (data.Class, data.CharacterName));
            Assert.AreEqual(41f, data.Magic);

            Class.Apply(Warrior, "", true);
            yield return null;
            Assert.AreEqual(0, Showing("Mage_"));

            save.Apply(data);
            yield return null;
            Assert.AreEqual(("mage", "Merlino"), (Class.Current.Id, Class.CharacterName));
            Assert.AreEqual(8, Showing("Mage_"));
            Assert.AreEqual(41f, sheet.GetBase(StatType.Magic), "gli attributi sono quelli salvati, non quelli di partenza");
            Assert.AreEqual(0.03f, Player.GetComponent<PlayerMagic>().ManaRegenPerSecond, 1e-6f);
        }

        [UnityTest, Description("-class e -heroname scelgono la classe e il nome di una partita nuova, senza la schermata della creazione: -class mage dà il corpo e gli attributi del Mago, e -herolevel spende i punti con il suo schema")]
        public IEnumerator CommandLine_ChoosesTheClass()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, "-newgame -class mage -heroname Merlino -herolevel 6");
            yield return LoadSandbox();
            var sheet = Player.GetComponent<CharacterStats>().Sheet;
            Assert.AreEqual(("mage", "Merlino"), (Class.Current.Id, Class.CharacterName));
            Assert.AreEqual(8, Showing("Mage_"));
            // livello 6: cinque livelli, venticinque punti, dieci in Magia e dieci in Vitalità
            Assert.AreEqual((8f, 17f + 5f, 35f + 10f, 20f + 10f), (sheet.GetBase(StatType.Strength), sheet.GetBase(StatType.Dexterity), sheet.GetBase(StatType.Magic), sheet.GetBase(StatType.Vitality)));
        }
    }
}
