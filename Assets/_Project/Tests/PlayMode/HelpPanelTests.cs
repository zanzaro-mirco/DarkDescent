using System.Collections;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Il pannello dei comandi (23.5 della M23): F1 e il pulsante nella pausa, i tasti veri, due lingue, il Mago.</summary>
    public class HelpPanelTests : SandboxFixture
    {
        private static HelpPanel Panel => Object.FindFirstObjectByType<HelpPanel>();

        private static PauseMenu Pause => Object.FindFirstObjectByType<PauseMenu>();

        private Keyboard _keyboard;

        [SetUp]
        public void AddKeyboard()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Localizer?.SetLanguage("en");
        }

        private IEnumerator Tap(UnityEngine.InputSystem.Controls.KeyControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        [UnityTest, Description("F1 apre il pannello e F1 lo richiude; Esc lo richiude senza aprire la pausa")]
        public IEnumerator F1_OpensAndClosesThePanel()
        {
            yield return LoadSandbox();
            Assert.IsFalse(Panel.IsOpen);
            yield return Tap(_keyboard.f1Key);
            Assert.IsTrue(Panel.IsOpen);
            Assert.AreEqual(17, Panel.RowCount);
            Assert.AreEqual(4, Panel.GroupCount);
            yield return Tap(_keyboard.f1Key);
            Assert.IsFalse(Panel.IsOpen);

            yield return Tap(_keyboard.f1Key);
            Assert.IsTrue(Panel.IsOpen);
            yield return Tap(_keyboard.escapeKey);
            Assert.IsFalse(Panel.IsOpen, "Esc chiude il pannello");
            Assert.IsFalse(Pause.IsOpen, "e non apre la pausa");
        }

        [UnityTest, Description("Il pannello dice i tasti veri letti dai binding, in inglese, e il titolo e il pulsante Chiudi con F1")]
        public IEnumerator Panel_ShowsTheRealKeys()
        {
            yield return LoadSandbox();
            Panel.Open();
            Assert.AreEqual("Controls", Panel.TitleText);
            Assert.AreEqual("Close (F1)", Panel.CloseText);
            Assert.AreEqual("Movement", Panel.GroupTitleAt(0));
            Assert.AreEqual("Seeing", Panel.GroupTitleAt(1));
            Assert.AreEqual("Combat", Panel.GroupTitleAt(2));
            Assert.AreEqual("Windows", Panel.GroupTitleAt(3));

            var keys = new[]
            {
                "Left click", "W A S D", "Z X", "Wheel", "Alt", "F1",
                "Left click", "Right click", "Q E", "1–8", "T",
                "I", "C", "K", "N", "M", "Esc",
            };
            for (int i = 0; i < keys.Length; i++)
            {
                Assert.AreEqual(keys[i], Panel.KeyAt(i), "tasto della riga " + i);
                Assert.IsFalse(string.IsNullOrWhiteSpace(Panel.TextAt(i)), "testo della riga " + i);
            }

            Assert.AreEqual("On an enemy: strikes, even while walking", Panel.TextAt(6));
        }

        [UnityTest, Description("Cambiando lingua a pannello aperto, i testi si riscrivono: titolo, gruppi, righe e Chiudi")]
        public IEnumerator Panel_FollowsTheLanguage()
        {
            yield return LoadSandbox();
            Panel.Open();
            Localizer.SetLanguage("it");
            Assert.AreEqual("Comandi", Panel.TitleText);
            Assert.AreEqual("Chiudi (F1)", Panel.CloseText);
            Assert.AreEqual("Movimento", Panel.GroupTitleAt(0));
            Assert.AreEqual("Combattimento", Panel.GroupTitleAt(2));
            Assert.AreEqual("Clic sinistro", Panel.KeyAt(0));
            Assert.AreEqual("Cammina; tenuto, segue il cursore", Panel.TextAt(0));
            Assert.AreEqual("Menu delle abilità", Panel.TextAt(13));
        }

        [UnityTest, Description("Per il Mago la riga del tasto sinistro sul nemico dice il dardo arcano; il resto del pannello è uguale")]
        public IEnumerator Panel_SpeaksOfTheBoltToTheMage()
        {
            yield return LoadSandbox();
            Panel.Open();
            Assert.AreEqual("On an enemy: strikes, even while walking", Panel.TextAt(6));
            Panel.Close();

            var classes = Player.GetComponent<PlayerClass>();
            classes.Apply(classes.Catalog.Find("mage"), "Merlino", true);
            yield return null;
            Panel.Open();
            Assert.AreEqual("On an enemy: arcane bolt, even while walking", Panel.TextAt(6));
            Assert.AreEqual("Right-button ability, at the cursor", Panel.TextAt(7), "le altre righe non cambiano");
        }

        [UnityTest, Description("Dal menu di pausa il pulsante Comandi apre il pannello: il menu si nasconde, il gioco resta fermo, e alla chiusura il menu torna")]
        public IEnumerator PauseButton_OpensThePanelAndComesBack()
        {
            yield return LoadSandbox();
            var pause = Pause;
            Assert.IsNotNull(pause.HelpButton);
            pause.Open();
            yield return null;
            Assert.IsTrue(pause.ResumeButton.gameObject.activeInHierarchy, "il menu è aperto");

            pause.HelpButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(Panel.IsOpen);
            Assert.IsFalse(pause.ResumeButton.gameObject.activeInHierarchy, "il menu si nasconde");
            Assert.IsTrue(pause.IsOpen, "ma si è ancora in pausa");
            Assert.AreEqual(0f, Time.timeScale);

            Panel.CloseButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(Panel.IsOpen);
            Assert.IsTrue(pause.ResumeButton.gameObject.activeInHierarchy, "il menu torna");
            Assert.IsTrue(pause.IsOpen);

            // Esc dal pannello torna al menu, non lo chiude del tutto
            pause.HelpButton.onClick.Invoke();
            yield return null;
            yield return Tap(_keyboard.escapeKey);
            Assert.IsFalse(Panel.IsOpen);
            Assert.IsTrue(pause.IsOpen, "Esc chiude solo il pannello");
            Assert.IsTrue(pause.ResumeButton.gameObject.activeInHierarchy);
            pause.Close();
        }

        [UnityTest, Description("F1 in pausa apre il pannello passando dal menu (che si nasconde), e F1 di nuovo riporta il menu")]
        public IEnumerator F1_InPause_GoesThroughTheMenu()
        {
            yield return LoadSandbox();
            Pause.Open();
            yield return null;
            yield return Tap(_keyboard.f1Key);
            Assert.IsTrue(Panel.IsOpen);
            Assert.IsFalse(Pause.ResumeButton.gameObject.activeInHierarchy, "il menu non resta sopra il pannello");
            Assert.IsTrue(Pause.IsOpen);

            yield return Tap(_keyboard.f1Key);
            Assert.IsFalse(Panel.IsOpen);
            Assert.IsTrue(Pause.ResumeButton.gameObject.activeInHierarchy, "il menu torna");
            Pause.Close();
        }

        [UnityTest, Description("Con il pannello aperto i tasti W A S D non muovono il cavaliere")]
        public IEnumerator Panel_StopsTheKeyboardWalk()
        {
            yield return LoadSandbox();
            Panel.Open();
            Vector3 start = Player.position;
            Press(_keyboard.wKey);
            yield return new WaitForSeconds(0.5f);
            Release(_keyboard.wKey);
            Assert.Less(FlatDistance(start, Player.position), 0.2f, "fermo sotto il pannello");

            Panel.Close();
            Press(_keyboard.wKey);
            yield return new WaitForSeconds(0.5f);
            Release(_keyboard.wKey);
            Assert.Greater(FlatDistance(start, Player.position), 0.5f, "chiuso, si cammina");
        }
    }
}
