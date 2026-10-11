using System.Collections;
using DarkDescent.Magic;
using DarkDescent.Progression;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class MagicHudTests : SandboxFixture
    {
        private static SpellDefinition Spell(string name) => UnityEditor.AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

        private IEnumerator Click(ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        [UnityTest, Description("All'avvio: sfera del mana piena, il Dardo nel primo posto e scelto, gli altri posti vuoti; il tasto destro sul pavimento lancia verso il cursore, spende 6 di mana e la sfera scende")]
        public IEnumerator RightClick_CastsSelectedSpell()
        {
            yield return LoadSandbox();
            var magic = Player.GetComponent<PlayerMagic>();
            var orb = Object.FindFirstObjectByType<ManaOrb>();
            var bar = Object.FindFirstObjectByType<SpellBarView>();
            Assert.AreEqual(30f, magic.Mana.Max, "Magia 10 al livello 1");
            Assert.AreEqual(1f, orb.FillAmount);
            Assert.IsTrue(bar.Slot(0).HasIcon && bar.Slot(0).IsSelected);
            Assert.IsFalse(bar.Slot(1).HasIcon || bar.Slot(2).HasIcon, "Anello e Pelle non si conoscono");

            SpellDefinition cast = null;
            Vector3 target = Vector3.zero;
            magic.Cast += (spell, point) => { cast = spell; target = point; };
            Vector3 floor = Player.position + new Vector3(3f, 0f, 0f);
            PointAt(floor);
            yield return null;
            yield return Click(Mouse.rightButton);

            Assert.AreEqual(Spell("Firebolt"), cast);
            Assert.AreEqual(floor.x, target.x, 0.3f, "verso il punto sotto il cursore");
            Assert.AreEqual(floor.z, target.z, 0.3f);
            Assert.AreEqual(24f, magic.Mana.Current, 0.5f, "6 di mana spesi, più un soffio di rigenerazione");
            Assert.Less(orb.FillAmount, 0.85f);
        }

        [UnityTest, Description("F2 non sceglie un incantesimo non noto; imparato l'Anello compare nel secondo posto, F2 lo sceglie, lanciarlo avvia il velo della ricarica; senza mana il posto si scurisce e il lancio non parte")]
        public IEnumerator Keys_SelectAndCooldownAndMana()
        {
            yield return LoadSandbox();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var magic = Player.GetComponent<PlayerMagic>();
            var bar = Object.FindFirstObjectByType<SpellBarView>();

            yield return Click(keyboard.f2Key);
            Assert.AreEqual(0, magic.Book.Selected, "l'Anello non si conosce: resta il Dardo");

            Assert.IsTrue(magic.Book.Learn(Spell("FireRing")));
            yield return null;
            Assert.IsTrue(bar.Slot(1).HasIcon);
            yield return Click(keyboard.f2Key);
            Assert.AreEqual(1, magic.Book.Selected);
            Assert.IsTrue(bar.Slot(1).IsSelected && !bar.Slot(0).IsSelected);

            Assert.AreEqual(CastResult.Ready, magic.TryCastAt(Player.position));
            yield return null;
            Assert.Greater(bar.Slot(1).CooldownFill, 0.8f, "il velo appena lanciato");
            Assert.AreEqual(CastResult.Cooldown, magic.TryCastAt(Player.position));
            yield return new WaitForSeconds(3.2f);
            Assert.AreEqual(0f, bar.Slot(1).CooldownFill, "finita la ricarica il velo sparisce");

            // 15 + 15 spesi: ne restano pochi, l'Anello costa 15
            magic.Mana.TrySpend(magic.Mana.Current - 5f);
            yield return null;
            Assert.IsFalse(bar.Slot(1).IsAffordable, "il mana non basta: il posto è scuro");
            Assert.IsTrue(bar.Slot(0).HasIcon && !bar.Slot(0).IsAffordable, "anche il Dardo, che ne costa 6");
            CastResult failed = CastResult.Ready;
            magic.CastFailed += result => failed = result;
            Assert.AreEqual(CastResult.NoMana, magic.TryCastAt(Player.position));
            Assert.AreEqual(CastResult.NoMana, failed);
        }

        [UnityTest, Description("Il mana massimo segue Magia e livello; salendo di livello il mana si riempie, come la vita")]
        public IEnumerator LevelUp_RaisesAndFillsMana()
        {
            yield return LoadSandbox();
            var magic = Player.GetComponent<PlayerMagic>();
            var progress = Player.GetComponent<PlayerProgress>().Progress;
            magic.Mana.TrySpend(20f);
            progress.Add(progress.ExperienceToNext);
            yield return null;
            Assert.AreEqual(2, progress.Level);
            Assert.AreEqual(31f, magic.Mana.Max, "+1 a livello");
            Assert.AreEqual(31f, magic.Mana.Current, "pieno");

            Assert.IsTrue(progress.TrySpend(Stats.StatType.Magic));
            Assert.AreEqual(33f, magic.Mana.Max, "+2 per punto di Magia");
            Assert.AreEqual(31f, magic.Mana.Current, "alzare il massimo non riempie");
        }
    }
}
