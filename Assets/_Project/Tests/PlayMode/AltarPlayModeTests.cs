using System;
using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Town;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>L'altare dell'accampamento e la vittoria per difficoltà (passo 14.5, D3, D4, D15).</summary>
    public class AltarPlayModeTests : SandboxFixture
    {
        private static CompositionRoot Root => UnityEngine.Object.FindFirstObjectByType<CompositionRoot>();

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static DifficultyPanel Panel => UnityEngine.Object.FindFirstObjectByType<DifficultyPanel>();

        private static ConfirmDialog Confirm => Panel.transform.parent.Find("Confirm").GetComponent<ConfirmDialog>();

        private static VictoryScreen Victory => UnityEngine.Object.FindFirstObjectByType<VictoryScreen>();

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, null);
            yield return null;
        }

        // una partita nuova in città, alla difficoltà data
        private IEnumerator StartInTown(int difficulty)
        {
            Environment.SetEnvironmentVariable(CompositionRoot.ArgumentsVariable, $"-newgame -difficulty {difficulty}");
            yield return LoadGeneratedCore();
            Assert.IsTrue(Manager.CurrentLevel.IsTown);
        }

        private static IEnumerator WaitUntilIn(Func<LevelContext, bool> arrived, string what)
        {
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || !arrived(Manager.CurrentLevel); t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, what);
                yield return null;
            }

            yield return null;
        }

        [UnityTest, Description("L'altare sta in città, del colore della difficoltà; il pannello apre le completate e la successiva, segna l'attuale e chiude le altre (Ombra completata: si arriva alla Tenebra, non all'Abisso)")]
        public IEnumerator Altar_OpensOnlyTheAllowedDifficulties()
        {
            yield return StartInTown(1);
            Root.Difficulty.Restore(1, 1, 0);
            var altar = Manager.CurrentLevel.Altar;
            Assert.IsNotNull(altar, "l'altare in città");
            Assert.AreEqual(Root.CurrentDifficulty.Color, altar.Color);
            Assert.AreEqual("Altar of Darkness", altar.GetComponent<Interaction.Interactable>().GetLabel(Localizer));
            Assert.IsTrue(NavMesh.SamplePosition(altar.GetComponent<Interaction.Interactable>().ApproachPoint, out _, 0.5f, NavMesh.AllAreas), "ci si arriva");

            altar.GetComponent<Interaction.Interactable>().Use(Player.gameObject);
            Assert.IsTrue(Panel.IsOpen);
            Assert.AreEqual("Dusk · completed", Panel.RowText(0));
            Assert.AreEqual("Shadow · current", Panel.RowText(1), "completata e attuale: si dice che è l'attuale");
            Assert.AreEqual("Darkness", Panel.RowText(2));
            Assert.AreEqual("Abyss I · locked", Panel.RowText(3));
            Assert.IsTrue(Panel.Row(0).interactable, "si torna a una più bassa");
            Assert.IsFalse(Panel.Row(1).interactable, "è già quella");
            Assert.IsTrue(Panel.Row(2).interactable, "la successiva");
            Assert.IsFalse(Panel.Row(3).interactable);
            Assert.IsFalse(Panel.Row(8).interactable);

            Panel.Row(3).onClick.Invoke();
            Assert.IsFalse(Confirm.IsOpen, "una chiusa non chiede niente");
            Panel.CloseButton.onClick.Invoke();
            Assert.IsFalse(Panel.IsOpen);
        }

        [UnityTest, Description("Rifai il mondo, dopo la conferma: seme nuovo dal vecchio, re vivo, portale e ricordi dimenticati; personaggio e oro restano")]
        public IEnumerator NewWorld_RemakesTheDungeon()
        {
            yield return StartInTown(1);
            var inventory = Player.GetComponent<PlayerInventory>().Inventory;
            inventory.RestoreGold(500);
            Root.RunStats.MarkBossDefeated();
            ulong before = Manager.RunSeed;

            Manager.CurrentLevel.Altar.GetComponent<Interaction.Interactable>().Use(Player.gameObject);
            Panel.NewWorldButton.onClick.Invoke();
            Assert.IsTrue(Confirm.IsOpen);
            StringAssert.Contains("king is reborn", Confirm.Message);
            Confirm.Yes.onClick.Invoke();
            yield return null;

            Assert.IsFalse(Panel.IsOpen);
            Assert.AreEqual((1, 0, 1), (Root.Difficulty.Current, Root.Difficulty.HighestCompleted, Root.Difficulty.World));
            Assert.AreEqual(SeedMixer.ForWorld(before, 1), Manager.RunSeed);
            Assert.AreEqual(Manager.RunSeed, Root.LootSeed);
            Assert.IsFalse(Root.RunStats.BossDefeated, "il re rinasce");
            Assert.AreEqual(0, UnityEngine.Object.FindFirstObjectByType<TownPortals>().PortalDepth);
            Assert.AreEqual(500, inventory.Gold, "l'oro resta");
            StringAssert.Contains("Shadow", UnityEngine.Object.FindFirstObjectByType<LevelTitle>().Text, "il titolo dice la difficoltà");

            // si scende nel mondo nuovo: il livello 1 viene dal seme nuovo
            Manager.LoadLevel("Level_Crypt", "Start", 1);
            yield return WaitUntilIn(level => level.Depth == 1, "la profondità 1 non arriva");
            StringAssert.Contains("Level 1 · Shadow", UnityEngine.Object.FindFirstObjectByType<LevelTitle>().Text);
        }

        [UnityTest, Description("Scegliere un'altra difficoltà chiede conferma e fa un mondo nuovo a quella difficoltà; il Crepuscolo non si scrive nel titolo")]
        public IEnumerator Select_ChangesTheDifficulty()
        {
            yield return StartInTown(1);
            Manager.CurrentLevel.Altar.GetComponent<Interaction.Interactable>().Use(Player.gameObject);
            Panel.Row(0).onClick.Invoke();
            Assert.IsTrue(Confirm.IsOpen);
            StringAssert.Contains("Dusk", Confirm.Message);
            Confirm.Yes.onClick.Invoke();
            yield return null;

            Assert.AreEqual((0, 0, 1), (Root.Difficulty.Current, Root.Difficulty.HighestCompleted, Root.Difficulty.World));
            Assert.AreEqual(Root.CurrentDifficulty.Color, Manager.CurrentLevel.Altar.Color);
            StringAssert.DoesNotContain("·", UnityEngine.Object.FindFirstObjectByType<LevelTitle>().Text);
        }

        // il re della profondità 8 muore, il cavaliere attraversa il portale della vittoria e arriva in città
        private IEnumerator KillTheKingAndGoHome()
        {
            Manager.LoadLevel("Level_Caves", "FromAbove", 8);
            yield return WaitUntilIn(level => level.Depth == 8, "la profondità 8 non arriva");
            var boss = Manager.CurrentLevel.Boss;
            boss.Health.TakeDamage(new DamageInfo(boss.Health.Max, DamageType.Physical, null));
            yield return null;

            var portal = UnityEngine.Object.FindFirstObjectByType<TownPortals>().VictoryPortal;
            Assert.IsNotNull(portal, "il portale della vittoria");
            var player = GameObject.Find("Player");
            Assert.IsTrue(NavMesh.SamplePosition(portal.transform.position + Vector3.back * 2f, out var hit, 2f, NavMesh.AllAreas));
            player.GetComponent<NavMeshAgent>().Warp(hit.position);
            player.GetComponent<PlayerController>().Use(portal.Interactable);
            yield return WaitUntilIn(level => level.IsTown, "il portale della vittoria non porta in città");
        }

        [UnityTest, Description("Il re battuto per la prima volta all'Ombra apre la Tenebra e la vittoria lo dice")]
        public IEnumerator FirstVictory_OpensTheNext()
        {
            yield return StartInTown(1);
            yield return KillTheKingAndGoHome();

            Assert.AreEqual(1, Root.Difficulty.HighestCompleted);
            Assert.IsTrue(Victory.IsOpen, "la prima volta, la schermata");
            StringAssert.Contains("Unlocked: Darkness", Victory.StatsText);

            // Continua (M17): si resta in città, con il tempo che scorre e i comandi
            Victory.ContinueButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(Victory.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(Manager.CurrentLevel.IsTown);
            Assert.IsTrue(GameObject.Find("Player").GetComponent<PlayerInputReader>().enabled, "si gioca ancora");
        }

        [UnityTest, Description("Il re battuto di nuovo a una difficoltà già completata: una scritta, il portale, e in città niente schermata")]
        public IEnumerator LaterVictory_OnlySaysSo()
        {
            yield return StartInTown(1);
            Root.Difficulty.Restore(1, 1, 0);
            Manager.LoadLevel("Level_Caves", "FromAbove", 8);
            yield return WaitUntilIn(level => level.Depth == 8, "la profondità 8 non arriva");
            var boss = Manager.CurrentLevel.Boss;
            boss.Health.TakeDamage(new DamageInfo(boss.Health.Max, DamageType.Physical, null));
            yield return null;
            Assert.AreEqual("The king has fallen", UnityEngine.Object.FindFirstObjectByType<HudMessage>().Text);

            var portal = UnityEngine.Object.FindFirstObjectByType<TownPortals>().VictoryPortal;
            Assert.IsNotNull(portal, "il portale della vittoria c'è anche le volte dopo");
            var player = GameObject.Find("Player");
            Assert.IsTrue(NavMesh.SamplePosition(portal.transform.position + Vector3.back * 2f, out var hit, 2f, NavMesh.AllAreas));
            player.GetComponent<NavMeshAgent>().Warp(hit.position);
            player.GetComponent<PlayerController>().Use(portal.Interactable);
            yield return WaitUntilIn(level => level.IsTown, "il portale della vittoria non porta in città");

            Assert.IsFalse(Victory.IsOpen, "le volte dopo niente schermata");
            Assert.AreEqual(1f, Time.timeScale);
        }
    }
}
