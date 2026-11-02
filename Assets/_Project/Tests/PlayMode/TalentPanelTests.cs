using System.Collections;
using System.Linq;
using DarkDescent.Items;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Talents;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Il pannello delle abilità sul tasto N (D10 e D11 della M20): si apre e si chiude, disegna i due
    /// alberi, i click prendono e tolgono gradi, il nodo a scelta chiede quale, il tooltip dice cosa fa,
    /// la specializzazione si sceglie dalle schede, l'avviso dei punti compare e l'altare ricomincia l'albero.
    /// </summary>
    public class TalentPanelTests : SandboxFixture
    {
        private static TalentPanel Panel => Object.FindFirstObjectByType<TalentPanel>();

        private static TalentPointsButton Notice => Object.FindFirstObjectByType<TalentPointsButton>();

        private TalentBuild Build => Player.GetComponent<PlayerTalents>().Build;

        private CharacterProgress Progress => Player.GetComponent<PlayerProgress>().Progress;

        private IEnumerator Click(ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        private TalentNodeView ClassNode(string id) => Panel.View(Build.ClassTree, id);

        // i nodi della classe, riga per riga, fino a quella del nodo dato (escluso): così il nodo si apre
        private void TakeUntilOpen(string id)
        {
            var tree = Build.ClassTree;
            var target = tree.Find(id);
            foreach (var node in tree.Nodes.OrderBy(n => n.Row))
            {
                if (node.Row >= target.Row)
                {
                    break;
                }

                while (Build.Rank(tree, node) < node.Ranks && Build.TryAdd(tree, node))
                {
                }
            }

            Assert.IsTrue(Build.IsOpen(tree, target), $"{id} si apre");
        }

        [UnityTest, Description("N apre il pannello delle abilità e lo richiude; Esc lo chiude senza aprire la pausa; la finestra copre il resto e il cavaliere resta fermo")]
        public IEnumerator N_OpensAndClosesThePanel()
        {
            yield return LoadSandbox();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            Assert.IsFalse(Panel.IsOpen);

            yield return Click(keyboard.nKey);
            Assert.IsTrue(Panel.IsOpen);
            yield return Click(keyboard.nKey);
            Assert.IsFalse(Panel.IsOpen);

            yield return Click(keyboard.nKey);
            Assert.IsTrue(Panel.IsOpen);
            yield return Click(keyboard.escapeKey);
            Assert.IsFalse(Panel.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, "Esc ha chiuso solo il pannello, non ha aperto la pausa");
        }

        [UnityTest, Description("Aperto al livello 1: tutti i nodi della classe con le linee e le due righe di punti, la radice presa, i nodi sotto di lei aperti e il resto chiuso; a destra le due specializzazioni da guardare, chiuse, con il perché")]
        public IEnumerator Panel_DrawsBothTrees()
        {
            yield return LoadSandbox();
            Panel.Open();
            var tree = Build.ClassTree;
            Assert.AreEqual("Warrior", Panel.AreaName(false));
            Assert.AreEqual("Points to spend: 0", Panel.AreaPoints(false));
            Assert.AreEqual(tree.Nodes.Count, tree.Nodes.Count(n => Panel.View(tree, n.Id) != null), "ogni nodo ha la sua vista");
            Assert.AreEqual((tree.Nodes.Sum(n => n.Links.Length), tree.Gates.Count), Panel.Drawn(false));

            Assert.AreEqual(TalentNodeState.Full, ClassNode("power_strike").State, "la radice è gratis");
            Assert.AreEqual(TalentNodeState.Available, ClassNode("strong_arm").State);
            Assert.AreEqual(TalentNodeState.Locked, ClassNode("keen_eye").State);
            Assert.AreEqual("0/3", ClassNode("strong_arm").RankText);
            Assert.AreEqual("Strong Arm", ClassNode("strong_arm").LabelText);
            Assert.AreEqual("Power Strike", ClassNode("power_strike").LabelText);
            Assert.AreNotEqual(ClassNode("strong_arm").RingColor, ClassNode("keen_eye").RingColor, "aperto e chiuso si vedono");
            Assert.AreNotEqual(ClassNode("strong_arm").RingColor, ClassNode("power_strike").RingColor, "aperto e preso si vedono");

            var defense = Build.Specializations[0];
            Assert.AreEqual("Specialization", Panel.AreaName(true));
            Assert.AreEqual("Available from level 10", Panel.SpecNote);
            Assert.IsFalse(Panel.ChooseButton.interactable);
            Assert.IsTrue(Panel.SpecTab(0).gameObject.activeSelf && Panel.SpecTab(1).gameObject.activeSelf);
            Assert.AreEqual(TalentNodeState.Locked, Panel.View(defense, "shield_bash").State, "la specializzazione si vede, ma è chiusa");
            Assert.AreEqual((defense.Nodes.Sum(n => n.Links.Length), defense.Gates.Count), Panel.Drawn(true));
        }

        [UnityTest, Description("Il click sinistro prende un grado e il destro lo toglie, finché non si applica; il nodo chiuso non si prende; Applica rende veri i gradi e li blocca, Annulla li butta; i punti scendono e risalgono")]
        public IEnumerator Clicks_TakeRanksThenApplyOrCancel()
        {
            yield return LoadSandbox();
            Progress.Restore(8, 0, 0);
            Panel.Open();
            Assert.AreEqual("Points to spend: 7", Panel.AreaPoints(false));
            Assert.IsFalse(Panel.ApplyButton.interactable, "niente da applicare");

            for (int i = 0; i < 3; i++)
            {
                ClassNode("strong_arm").Click();
            }

            Assert.AreEqual("3/3", ClassNode("strong_arm").RankText);
            Assert.AreEqual(TalentNodeState.Full, ClassNode("strong_arm").State);
            Assert.AreEqual("Points to spend: 4", Panel.AreaPoints(false));
            Assert.AreEqual(0, Build.AppliedRank(Build.ClassTree, Build.ClassTree.Find("strong_arm")), "non ancora applicato");
            Assert.IsTrue(Panel.ApplyButton.interactable && Panel.CancelButton.interactable);
            Assert.AreEqual("Not applied yet", Panel.PendingText);
            Assert.AreEqual(TalentNodeState.Available, ClassNode("keen_eye").State, "il nodo sotto si è aperto");

            ClassNode("strong_arm").Click(right: true);
            Assert.AreEqual("2/3", ClassNode("strong_arm").RankText);
            Assert.AreEqual("Points to spend: 5", Panel.AreaPoints(false));
            ClassNode("strong_arm").Click();
            ClassNode("momentum").Click();
            Assert.AreEqual(0, Build.Rank(Build.ClassTree, Build.ClassTree.Find("momentum")), "chiuso: il click non fa niente");

            Panel.ApplyButton.onClick.Invoke();
            Assert.AreEqual(3, Build.AppliedRank(Build.ClassTree, Build.ClassTree.Find("strong_arm")));
            Assert.IsFalse(Panel.ApplyButton.interactable);
            Assert.AreEqual(string.Empty, Panel.PendingText);
            ClassNode("strong_arm").Click(right: true);
            Assert.AreEqual("3/3", ClassNode("strong_arm").RankText, "un grado applicato non si toglie");

            ClassNode("charge").Click();
            Assert.AreEqual(TalentNodeState.Full, ClassNode("charge").State);
            Panel.CancelButton.onClick.Invoke();
            Assert.AreEqual(TalentNodeState.Available, ClassNode("charge").State, "Annulla butta quello che non era applicato");
            Assert.AreEqual("Points to spend: 4", Panel.AreaPoints(false));
        }

        [UnityTest, Description("Il nodo a scelta è chiuso finché non si apre il suo legame; cliccato mostra le due opzioni con cosa fanno; scegliere la seconda prende il nodo con quell'opzione e chiude la finestrella; il destro lo toglie")]
        public IEnumerator ChoiceNode_AsksWhichOne()
        {
            yield return LoadSandbox();
            Progress.Restore(50, 0, 0);
            TakeUntilOpen("battle_instinct");
            Panel.Open();
            var view = ClassNode("battle_instinct");
            Assert.AreEqual(TalentNodeState.Available, view.State);
            Assert.AreEqual("Battle Instinct", view.LabelText, "finché non è presa si legge il nome del nodo");

            view.Click();
            Assert.IsTrue(Panel.IsChoiceOpen);
            StringAssert.Contains("Bloodlust", Panel.ChoiceButton(0).GetComponentInChildren<TMPro.TMP_Text>().text);
            StringAssert.Contains("3% of the damage dealt returns as life", Panel.ChoiceButton(0).GetComponentInChildren<TMPro.TMP_Text>().text);
            StringAssert.Contains("Battle Rhythm", Panel.ChoiceButton(1).GetComponentInChildren<TMPro.TMP_Text>().text);

            Panel.ChoiceButton(1).onClick.Invoke();
            Assert.IsFalse(Panel.IsChoiceOpen);
            Assert.AreEqual(1, Build.OptionOf(Build.ClassTree, Build.ClassTree.Find("battle_instinct")));
            Assert.AreEqual("Battle Rhythm", ClassNode("battle_instinct").LabelText);
            Assert.AreEqual(TalentNodeState.Full, ClassNode("battle_instinct").State);

            ClassNode("battle_instinct").Click();
            Assert.IsFalse(Panel.IsChoiceOpen, "già presa: niente scelta");
            ClassNode("battle_instinct").Click(right: true);
            Assert.AreEqual(-1, Build.OptionOf(Build.ClassTree, Build.ClassTree.Find("battle_instinct")));

            ClassNode("battle_instinct").Click();
            Assert.IsTrue(Panel.IsChoiceOpen);
            ClassNode("battle_instinct").Click();
            Assert.IsFalse(Panel.IsChoiceOpen, "un secondo click la richiude");
        }

        [UnityTest, Description("Il tooltip dice nome, tipo, gradi e cosa fa (con il grado successivo), per un'abilità attiva la descrizione e il mana, per un nodo chiuso cosa serve; segue la lingua e sparisce quando il mouse esce")]
        public IEnumerator Tooltip_ExplainsTheNode()
        {
            yield return LoadSandbox();
            Progress.Restore(5, 0, 0);
            Panel.Open();
            Assert.IsFalse(Panel.IsTooltipShown);

            ClassNode("strong_arm").Hover();
            Assert.IsTrue(Panel.IsTooltipShown);
            foreach (string part in new[] { "Strong Arm", "Passive", "Rank 0/3", "+2% damage" })
            {
                StringAssert.Contains(part, Panel.TooltipText);
            }

            ClassNode("strong_arm").Click();
            StringAssert.Contains("Rank 1/3", Panel.TooltipText, "il tooltip si aggiorna col click");
            StringAssert.Contains("Next rank:", Panel.TooltipText);
            StringAssert.Contains("+4% damage", Panel.TooltipText);

            ClassNode("power_strike").Hover();
            foreach (string part in new[] { "Power Strike", "Active ability", "A heavy blow with your weapon.", "4 mana" })
            {
                StringAssert.Contains(part, Panel.TooltipText);
            }

            ClassNode("keen_eye").Hover();
            StringAssert.Contains("Keen Eye", Panel.TooltipText);
            StringAssert.Contains("Take a connected talent above first", Panel.TooltipText, "dice cosa serve per aprirlo");

            Localizer.SetLanguage("it");
            try
            {
                ClassNode("strong_arm").Hover();
                StringAssert.Contains("Braccio forte", Panel.TooltipText);
                StringAssert.Contains("+2% di danno", Panel.TooltipText);
                Assert.AreEqual("Punti da spendere: 3", Panel.AreaPoints(false));
            }
            finally
            {
                Localizer.SetLanguage("en");
            }

            ClassNode("strong_arm").Unhover();
            Assert.IsFalse(Panel.IsTooltipShown);
        }

        [UnityTest, Description("Dal livello 10 la specializzazione si sceglie: le schede fanno guardare i due alberi, il pulsante sceglie quello che si guarda, le schede spariscono e la radice è presa; Annulla riporta la scelta")]
        public IEnumerator Specialization_IsChosenFromTheTabs()
        {
            yield return LoadSandbox();
            Progress.Restore(12, 0, 0);
            Panel.Open();
            var arms = Build.Specializations[1];
            Assert.IsTrue(Panel.ChooseButton.interactable);
            Assert.AreEqual("Shield and endurance: block and absorb", Panel.SpecNote);

            Panel.SpecTab(1).onClick.Invoke();
            Assert.AreEqual("Two-handed weapons: damage and whirlwind", Panel.SpecNote);
            Assert.AreEqual(TalentNodeState.Locked, Panel.View(arms, "whirlwind").State);
            Assert.IsNull(Build.Specialization, "guardare non sceglie");

            Panel.ChooseButton.onClick.Invoke();
            Assert.AreSame(arms, Build.Specialization);
            Assert.IsFalse(Panel.SpecTab(0).gameObject.activeSelf || Panel.SpecTab(1).gameObject.activeSelf || Panel.ChooseButton.gameObject.activeSelf);
            Assert.AreEqual("Arms", Panel.AreaName(true));
            Assert.AreEqual("Points to spend: 2", Panel.AreaPoints(true));
            Assert.AreEqual(TalentNodeState.Full, Panel.View(arms, "whirlwind").State);
            Assert.AreEqual(TalentNodeState.Available, Panel.View(arms, "two_handed_grip").State);
            Assert.IsTrue(Panel.ApplyButton.interactable, "una specializzazione scelta è una modifica da applicare");

            Panel.View(arms, "two_handed_grip").Click();
            Assert.AreEqual("Points to spend: 1", Panel.AreaPoints(true));
            Panel.CancelButton.onClick.Invoke();
            Assert.IsNull(Build.Specialization);
            Assert.IsTrue(Panel.SpecTab(0).gameObject.activeSelf, "le schede tornano");
        }

        [UnityTest, Description("L'avviso dei punti: spento al livello 1, \"Skills +N\" con punti da spendere, \"Choose a specialization\" dal livello 10, spento mentre il pannello è aperto e di nuovo acceso alla chiusura; il click lo apre")]
        public IEnumerator Notice_AnnouncesTheTreePoints()
        {
            yield return LoadSandbox();
            Assert.IsFalse(Notice.IsShown);

            Progress.Restore(5, 0, 0);
            Assert.IsTrue(Notice.IsShown);
            Assert.AreEqual("Skills +4", Notice.LabelText);

            Notice.Button.onClick.Invoke();
            Assert.IsTrue(Panel.IsOpen);
            Assert.IsFalse(Notice.IsShown, "aperto il pannello, l'avviso non serve");
            Panel.Close();
            Assert.IsTrue(Notice.IsShown);

            Progress.Restore(10, 0, 0);
            Assert.AreEqual("Choose a specialization", Notice.LabelText);
            Assert.IsTrue(Build.TryChoose(Build.Specializations[0]));
            Build.Apply();
            Assert.AreEqual("Skills +9", Notice.LabelText);

            Assert.IsTrue(Build.TryAdd(Build.ClassTree, Build.ClassTree.Find("strong_arm")));
            Assert.AreEqual("Skills +8", Notice.LabelText, "spesi, i punti scendono");
            Panel.Open();
            Panel.Close();
            Assert.IsTrue(Notice.IsShown, "restano punti");
        }

        [UnityTest, Description("L'altare (D11): la voce dice 30 oro per livello e si accende solo con qualcosa di applicato; confermata, prende l'oro, riporta tutti i punti e la specializzazione e lo dice; con poco oro non cambia niente")]
        public IEnumerator Altar_ReassignsTheSkillsForGold()
        {
            yield return LoadGeneratedCore();
            var altar = Object.FindFirstObjectByType<DifficultyPanel>();
            var confirm = altar.transform.parent.Find("Confirm").GetComponent<ConfirmDialog>();
            var inventory = Player.GetComponent<PlayerInventory>().Inventory;
            var message = Object.FindFirstObjectByType<HudMessage>();

            Progress.Restore(20, 0, 0);
            altar.Open();
            Assert.AreEqual("Reassign skills · 600 gold", altar.ResetTalentsText);
            Assert.IsFalse(altar.ResetTalentsButton.interactable, "niente di applicato");
            altar.Close();

            Assert.IsTrue(Build.TryChoose(Build.Specializations[1]));
            Assert.IsTrue(Build.TryAdd(Build.ClassTree, Build.ClassTree.Find("strong_arm")));
            Build.Apply();
            Assert.IsTrue(Build.HasApplied);

            altar.Open();
            Assert.IsTrue(altar.ResetTalentsButton.interactable);
            inventory.TrySpendGold(inventory.Gold);
            inventory.AddGold(100);
            altar.ResetTalentsButton.onClick.Invoke();
            Assert.IsTrue(confirm.IsOpen);
            StringAssert.Contains("600 gold", confirm.Message);
            confirm.Yes.onClick.Invoke();
            Assert.AreEqual(100, inventory.Gold, "con poco oro non si paga");
            Assert.IsTrue(Build.HasApplied);
            Assert.AreEqual("Not enough gold", message.Text);

            altar.Open();
            inventory.AddGold(900);
            altar.ResetTalentsButton.onClick.Invoke();
            confirm.Yes.onClick.Invoke();
            Assert.AreEqual(400, inventory.Gold, "600 oro pagati");
            Assert.IsFalse(Build.HasApplied);
            Assert.IsNull(Build.AppliedSpecialization);
            Assert.AreEqual(0, Build.AppliedRank(Build.ClassTree, Build.ClassTree.Find("strong_arm")));
            Assert.AreEqual(Build.Budget(Build.ClassTree), Build.Available(Build.ClassTree), "tutti i punti sono tornati");
            StringAssert.Contains("Skills reset", message.Text);
            Assert.IsFalse(altar.IsOpen);
        }
    }
}
