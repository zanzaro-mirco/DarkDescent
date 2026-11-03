using System.Collections;
using System.Linq;
using DarkDescent.Magic;
using DarkDescent.Player;
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

        [UnityTest, Description("All'avvio: sfera del mana piena, quattro caselle, l'Attacco a sinistra con l'icona e il Colpo possente (la radice dell'albero) nel destro, le altre vuote con il loro tasto; il destro sul pavimento senza nemici non lancia e non spende; il Grido, messo nel destro, parte verso il cursore, spende 12 di mana e la sfera scende")]
        public IEnumerator RightClick_CastsTheSecondary()
        {
            yield return LoadSandbox();
            var magic = Player.GetComponent<PlayerMagic>();
            var orb = Object.FindFirstObjectByType<ManaOrb>();
            var bar = Object.FindFirstObjectByType<SpellBarView>();
            Assert.AreEqual(30f, magic.Mana.Max, "Magia 10 al livello 1");
            Assert.AreEqual(1f, orb.FillAmount);
            Assert.AreEqual(4, bar.Count);
            Assert.AreSame(Spell("Attack"), magic.Book[0]);
            Assert.AreSame(Spell("PowerStrike"), magic.Book[1]);
            Assert.IsTrue(bar.Slot(0).HasIcon, "l'Attacco ha l'icona");
            Assert.IsTrue(bar.Slot(1).IsFilled);
            Assert.IsFalse(bar.Slot(2).IsFilled);
            Assert.IsFalse(bar.Slot(3).IsFilled);
            Assert.AreEqual(4, SpellBarView.KeyKeys.Length);

            SpellDefinition cast = null;
            Vector3 target = Vector3.zero;
            CastResult failed = CastResult.Ready;
            magic.Cast += (spell, point) => { cast = spell; target = point; };
            magic.CastFailed += result => failed = result;
            Vector3 floor = Player.position + new Vector3(3f, 0f, 0f);
            PointAt(floor);
            yield return null;
            yield return Click(Mouse.rightButton);
            Assert.IsNull(cast);
            Assert.AreEqual(CastResult.NoTarget, failed, "il Colpo possente vuole un nemico a portata");
            Assert.AreEqual(30f, magic.Mana.Current, 0.5f);

            Assert.IsTrue(magic.Learn(Spell("BattleShout")));
            Assert.AreEqual(2, magic.Book.SlotOf(Spell("BattleShout")), "arriva in Q");
            Assert.IsTrue(magic.Assign(SpellBook.Secondary, Spell("BattleShout")));
            yield return Click(Mouse.rightButton);

            Assert.AreEqual(Spell("BattleShout"), cast);
            Assert.AreEqual(floor.x, target.x, 0.3f, "verso il punto sotto il cursore");
            Assert.AreEqual(floor.z, target.z, 0.3f);
            Assert.AreEqual(18f, magic.Mana.Current, 0.5f, "12 di mana spesi, più un soffio di rigenerazione");
            Assert.Less(orb.FillAmount, 0.7f);
        }

        [UnityTest, Description("Q ed E lanciano le abilità delle loro caselle verso il cursore e spendono il mana; il sinistro non passa dagli incantesimi")]
        public IEnumerator QAndE_CastTheirSlots()
        {
            yield return LoadSandbox();
            var magic = Player.GetComponent<PlayerMagic>();
            var reader = Player.GetComponent<PlayerInputReader>();
            var cast = new System.Collections.Generic.List<SpellDefinition>();
            magic.Cast += (spell, point) => cast.Add(spell);
            Assert.IsTrue(magic.Learn(Spell("BattleShout")));
            Assert.IsTrue(magic.Learn(Spell("LastStand")));
            Assert.AreEqual((2, 3), (magic.Book.SlotOf(Spell("BattleShout")), magic.Book.SlotOf(Spell("LastStand"))));
            PointAt(Player.position + new Vector3(3f, 0f, 0f));
            yield return null;

            reader.PressSkill(2);
            yield return null;
            magic.Mana.Fill();
            reader.PressSkill(3);
            yield return null;
            CollectionAssert.AreEqual(new[] { Spell("BattleShout"), Spell("LastStand") }, cast);

            Assert.AreEqual(CastResult.NotKnown, magic.TryCastAt(SpellBook.Primary, Player.position), "il sinistro è il colpo dell'arma, non un incantesimo");
        }

        [UnityTest, Description("Le caselle: l'Anello, dato dal libro di prova, compare in Q; spostato in E, Q si svuota ed E si riempie; lanciarlo avvia il velo della ricarica; senza mana la casella si scurisce e il lancio non parte")]
        public IEnumerator Slots_FollowTheBookAndCooldownAndMana()
        {
            yield return LoadSandbox();
            var magic = Player.GetComponent<PlayerMagic>();
            var bar = Object.FindFirstObjectByType<SpellBarView>();

            Assert.IsTrue(magic.Book.Learn(Spell("FireRing")));
            yield return null;
            Assert.IsTrue(bar.Slot(2).IsFilled);
            Assert.IsTrue(magic.Book.Assign(3, Spell("FireRing")));
            yield return null;
            Assert.IsFalse(bar.Slot(2).IsFilled);
            Assert.IsTrue(bar.Slot(3).IsFilled);

            Assert.AreEqual(CastResult.Ready, magic.TryCastAt(Spell("FireRing"), Player.position));
            yield return null;
            Assert.Greater(bar.Slot(3).CooldownFill, 0.8f, "il velo appena lanciato");
            Assert.AreEqual(CastResult.Cooldown, magic.TryCastAt(Spell("FireRing"), Player.position));
            yield return new WaitForSeconds(8.2f);
            Assert.AreEqual(0f, bar.Slot(3).CooldownFill, "finita la ricarica il velo sparisce");

            // l'Anello costa 12, il Colpo possente 4: con 3 non basta per nessuno dei due
            magic.Mana.TrySpend(magic.Mana.Current - 3f);
            yield return null;
            Assert.IsFalse(bar.Slot(3).IsAffordable, "il mana non basta: la casella è scura");
            Assert.IsTrue(bar.Slot(1).IsFilled && !bar.Slot(1).IsAffordable, "anche il Colpo possente, che ne costa 4");
            Assert.IsTrue(bar.Slot(0).IsAffordable, "l'Attacco non costa niente");
            CastResult failed = CastResult.Ready;
            magic.CastFailed += result => failed = result;
            Assert.AreEqual(CastResult.NoMana, magic.TryCastAt(Spell("FireRing"), Player.position));
            Assert.AreEqual(CastResult.NoMana, failed);
        }

        [UnityTest, Description("Il menu delle abilità (D18): un click su una casella apre la sua riga con le abilità note che ci vanno, quella di adesso accesa; sceglierne una la mette, scambia con l'altra casella e chiude; il sinistro offre solo l'Attacco")]
        public IEnumerator Picker_AssignsAnAbilityToASlot()
        {
            yield return LoadSandbox();
            var magic = Player.GetComponent<PlayerMagic>();
            var bar = Object.FindFirstObjectByType<SpellBarView>();
            var picker = bar.Menu;
            Assert.IsNotNull(picker);
            Assert.IsTrue(magic.Learn(Spell("BattleShout")));
            Assert.IsTrue(magic.Learn(Spell("ShieldWall")));
            Assert.AreEqual((2, 3), (magic.Book.SlotOf(Spell("BattleShout")), magic.Book.SlotOf(Spell("ShieldWall"))));
            Assert.IsFalse(picker.IsOpen);

            bar.ClickSlot(2);
            Assert.IsTrue(picker.IsOpen);
            Assert.IsTrue(picker.ShowsSlot(2) && !picker.ShowsSlot(3) && !picker.ShowsSlot(0));
            Assert.IsTrue(bar.Slot(2).IsSelected && !bar.Slot(3).IsSelected, "la casella aperta si accende");
            Assert.AreEqual(3, picker.Choices(2).Count(view => view.gameObject.activeSelf), "il Colpo possente, il Grido e il Muro");
            var current = picker.Choices(2).Single(view => view.gameObject.activeSelf && view.IsCurrent);
            Assert.AreSame(Spell("BattleShout"), current.Spell);

            var wall = picker.Choices(2).Single(view => view.gameObject.activeSelf && view.Spell == Spell("ShieldWall"));
            wall.Click();
            Assert.IsFalse(picker.IsOpen, "una scelta chiude la riga");
            Assert.AreEqual((Spell("ShieldWall"), Spell("BattleShout")), (magic.Book[2], magic.Book[3]), "scambiate");

            bar.ClickSlot(0);
            Assert.AreEqual(1, picker.Choices(0).Count(view => view.gameObject.activeSelf), "il sinistro vuole solo l'Attacco");
            Assert.IsTrue(picker.Choices(0)[0].IsCurrent);
            bar.ClickSlot(0);
            Assert.IsFalse(picker.IsOpen, "un secondo click sulla stessa casella chiude");
        }

        [UnityTest, Description("K apre il menu con tutte e quattro le righe e lo richiude; da aperto con K una scelta non lo chiude; Esc lo chiude senza aprire la pausa")]
        public IEnumerator Picker_OpensWithKAndClosesWithEscape()
        {
            yield return LoadSandbox();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var magic = Player.GetComponent<PlayerMagic>();
            var picker = Object.FindFirstObjectByType<SpellBarView>().Menu;
            Assert.IsTrue(magic.Learn(Spell("BattleShout")));

            yield return Click(keyboard.kKey);
            Assert.IsTrue(picker.IsOpen);
            Assert.AreEqual(SkillMenu.AllSlots, picker.OpenSlot);
            for (int slot = 0; slot < SpellBook.Slots; slot++)
            {
                Assert.IsTrue(picker.ShowsSlot(slot), $"riga {slot}");
            }

            picker.Choices(1).Single(view => view.gameObject.activeSelf && view.Spell == Spell("BattleShout")).Click();
            Assert.IsTrue(picker.IsOpen, "aperto con K resta aperto");
            Assert.AreEqual(Spell("BattleShout"), magic.Book[SpellBook.Secondary]);

            yield return Click(keyboard.kKey);
            Assert.IsFalse(picker.IsOpen);

            yield return Click(keyboard.kKey);
            Assert.IsTrue(picker.IsOpen);
            yield return Click(keyboard.escapeKey);
            Assert.IsFalse(picker.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, "Esc ha chiuso solo il menu, non ha aperto la pausa");
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
