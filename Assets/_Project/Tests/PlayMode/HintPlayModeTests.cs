using System.Collections;
using System.Linq;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DarkDescent.Tests
{
    /// <summary>I suggerimenti del primo gioco (23.4 della M23): compaiono una volta, uno alla volta, tacciono in pausa, si ricordano.</summary>
    public class HintPlayModeTests : SandboxFixture
    {
        private static HintDirector Director => Object.FindFirstObjectByType<HintDirector>();

        private static HintBox Box => Object.FindFirstObjectByType<HintBox>(FindObjectsInactive.Include);

        private Keyboard _keyboard;

        [SetUp]
        public void AddKeyboard()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void Restore()
        {
            PlayerPrefs.DeleteKey(HintDirector.OnPreference);
            PlayerPrefs.DeleteKey(HintDirector.SeenPreference);
            Localizer?.SetLanguage("en");
        }

        // di partenza i suggerimenti sono spenti nei test (non coprano un click): qui si accende solo quello che serve
        private static void Only(params string[] ids)
        {
            Director.Book.Load(string.Join(",", Hints.All.Except(ids)));
            Director.SetOn(true);
        }

        private static IEnumerator WaitFor(string id, float timeout = 4f)
        {
            for (float t = 0f; Director.Current != id; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, timeout, "il suggerimento " + id + " non compare");
                yield return null;
            }

            yield return null;
        }

        private IEnumerator PressAnyKey()
        {
            Press(_keyboard.hKey);
            yield return null;
            Release(_keyboard.hKey);
            yield return null;
        }

        [UnityTest, Description("Nei test i suggerimenti sono spenti di partenza: un riquadro non deve coprire un click")]
        public IEnumerator Hints_AreOffInTests()
        {
            yield return LoadSandbox();
            Assert.IsFalse(Director.IsOn);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsNull(Director.Current);
            Assert.IsFalse(Box.IsShown);
        }

        [UnityTest, Description("Il primo suggerimento è il muoversi, con i tasti veri nel testo; il riquadro non riceve raggi (non ruba il click); un tasto lo chiude dopo mezzo secondo, e non torna")]
        public IEnumerator Move_AppearsOnceWithTheRealKeys()
        {
            yield return LoadSandbox();
            Only(Hints.Move);
            yield return WaitFor(Hints.Move);

            Assert.IsTrue(Box.IsShown);
            Assert.AreEqual("Move", Box.Title);
            Assert.AreEqual("Click the floor, or hold W A S D.", Box.Text);
            foreach (var graphic in Box.GetComponentsInChildren<Graphic>(true))
            {
                Assert.IsFalse(graphic.raycastTarget, graphic.name + " non deve prendere i click");
            }

            // un tasto appena comparso non lo chiude: il tasto che si stava premendo non deve cancellarlo
            yield return PressAnyKey();
            Assert.IsTrue(Box.IsShown, "troppo presto");

            yield return new WaitForSecondsRealtime(0.6f);
            yield return PressAnyKey();
            Assert.IsFalse(Box.IsShown, "un tasto lo chiude");
            Assert.IsNull(Director.Current);
            StringAssert.Contains("move", PlayerPrefs.GetString(HintDirector.SeenPreference), "il visto si ricorda tra le preferenze");

            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsFalse(Box.IsShown, "una volta sola");
            Assert.IsNull(Director.Current);
        }

        [UnityTest, Description("Passato il tempo il riquadro si chiude da solo, e la barra sottile si accorcia")]
        public IEnumerator Hint_ClosesBySelf()
        {
            yield return LoadSandbox();
            Only(Hints.Move);
            Director.Duration = 1.2f;
            yield return WaitFor(Hints.Move);
            float start = Box.Progress;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.Less(Box.Progress, start, "la barra si accorcia");
            Assert.Greater(Box.Progress, 0f);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsFalse(Box.IsShown, "scaduto");
            Assert.IsNull(Director.Current);
        }

        [UnityTest, Description("In pausa il riquadro sparisce e il tempo si ferma; alla ripresa torna lo stesso suggerimento")]
        public IEnumerator Hint_IsQuietInPause()
        {
            yield return LoadSandbox();
            Only(Hints.Move);
            Director.Duration = 6f;
            yield return WaitFor(Hints.Move);

            var pause = Object.FindFirstObjectByType<PauseMenu>();
            pause.Open();
            yield return null;
            yield return null;
            Assert.IsFalse(Box.IsShown, "in pausa niente riquadro");
            Assert.AreEqual(Hints.Move, Director.Current, "ma il suggerimento aspetta");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(Hints.Move, Director.Current, "il tempo si è fermato");

            pause.Close();
            yield return null;
            yield return null;
            Assert.IsTrue(Box.IsShown, "alla ripresa torna");
            Assert.AreEqual("Move", Box.Title);
        }

        [UnityTest, Description("Il testo segue la lingua anche a riquadro aperto")]
        public IEnumerator Hint_FollowsTheLanguage()
        {
            yield return LoadSandbox();
            Only(Hints.Move);
            yield return WaitFor(Hints.Move);
            Localizer.SetLanguage("it");
            Assert.AreEqual("Muoviti", Box.Title);
            Assert.AreEqual("Clicca sul pavimento, oppure tieni premuti W A S D.", Box.Text);
        }

        [UnityTest, Description("Il testo dei tasti viene dai binding: inventario I, personaggio C, alberi N, portale T, nomi Alt, cintura da 1 a 8, abilità Q ed E, K")]
        public IEnumerator KeyTexts_ComeFromTheBindings()
        {
            yield return LoadSandbox();
            var reader = Player.GetComponent<PlayerInputReader>();
            Assert.AreEqual("W A S D", reader.MoveKeysText());
            Assert.AreEqual("I", reader.KeyText("ToggleInventory"));
            Assert.AreEqual("C", reader.KeyText("ToggleCharacter"));
            Assert.AreEqual("N", reader.KeyText("ToggleTalents"));
            Assert.AreEqual("T", reader.KeyText("TownPortal"));
            Assert.AreEqual("K", reader.KeyText("ToggleSkills"));
            Assert.AreEqual("Alt", reader.KeyText("ShowLabels"));
            Assert.AreEqual("1", reader.KeyText("UseBelt"));
            Assert.AreEqual("8", reader.KeyText("UseBelt", last: true));
            Assert.AreEqual("Q", reader.KeyText("UseSkill"));
            Assert.AreEqual("E", reader.KeyText("UseSkill", last: true));
            Assert.AreEqual(string.Empty, reader.KeyText("NoSuchAction"));
        }

        [UnityTest, Description("Con punti attributo da spendere compare il suggerimento del personaggio, col tasto C nel testo")]
        public IEnumerator AttributePoints_RaiseTheirHint()
        {
            yield return LoadSandbox();
            Only(Hints.AttributePoint);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsNull(Director.Current, "senza punti niente suggerimento");

            var progress = Player.GetComponent<PlayerProgress>().Progress;
            progress.Restore(progress.Level, progress.Experience, 3);
            yield return WaitFor(Hints.AttributePoint);
            StringAssert.Contains("Press C", Box.Text);
        }

        [UnityTest, Description("Con un nemico a portata di vista compare il suggerimento del nemico; da lontano no")]
        public IEnumerator Enemy_RaisesItsHint_OnlyWhenNear()
        {
            yield return LoadSandbox();
            Only(Hints.Enemy);
            var skeleton = Object.FindObjectsByType<DarkDescent.Enemies.EnemyAI>(FindObjectsSortMode.None).First(e => e.gameObject.activeSelf);
            skeleton.enabled = false;
            skeleton.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(Player.position + Vector3.right * 40f);
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(0.7f);
            Assert.IsNull(Director.Current, "a quaranta metri non conta");

            skeleton.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(Player.position + Vector3.right * 6f);
            Physics.SyncTransforms();
            yield return WaitFor(Hints.Enemy);
            Assert.AreEqual("An enemy", Box.Title);
        }

        [UnityTest, Description("Le opzioni: «Mostra i suggerimenti» è accesa di partenza (fuori dai test) e la scelta resta tra le preferenze; spenta, niente riquadro; «Rivedi» li azzera e li riaccende")]
        public IEnumerator Options_TurnHintsOffAndReview()
        {
            yield return LoadCore();
            var options = Object.FindFirstObjectByType<OptionsPanel>();
            Assert.IsNotNull(options.Hints);
            Assert.IsNotNull(options.HintsReview);
            var check = (Image)options.Hints.graphic;
            Assert.AreEqual("check", check.sprite.texture.name, "la spunta d'oro come le altre");

            // la casella legge la preferenza: nei test è 0 (spenta), togliendola è accesa
            Assert.IsFalse(options.Hints.isOn);
            options.Open();
            options.Hints.isOn = true;
            Assert.IsTrue(Director.IsOn);
            Assert.AreEqual(1, PlayerPrefs.GetInt(OptionsPanel.HintsPreference));

            Director.Book.Load(string.Join(",", Hints.All.Except(new[] { Hints.Move })));
            yield return WaitFor(Hints.Move);
            Assert.IsTrue(Box.IsShown);

            options.Hints.isOn = false;
            Assert.IsFalse(Director.IsOn);
            Assert.IsFalse(Box.IsShown, "spenti, il riquadro si chiude");
            Assert.AreEqual(0, PlayerPrefs.GetInt(OptionsPanel.HintsPreference, 1));

            // Rivedi: tutto torna nuovo e i suggerimenti si riaccendono
            options.HintsReview.onClick.Invoke();
            Assert.IsTrue(Director.IsOn, "Rivedi li riaccende");
            Assert.IsTrue(options.Hints.isOn);
            Assert.AreEqual(0, Director.Book.SeenCount);
            Assert.AreEqual(string.Empty, PlayerPrefs.GetString(HintDirector.SeenPreference, string.Empty));
            options.Back.onClick.Invoke();
        }

        [UnityTest, Description("Riaperto il gioco, il suggerimento già visto non torna: il visto sta nelle preferenze, non nel salvataggio")]
        public IEnumerator Seen_SurvivesReopeningTheGame()
        {
            yield return LoadSandbox();
            Only(Hints.Move);
            yield return WaitFor(Hints.Move);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return PressAnyKey();
            Assert.IsNull(Director.Current);

            // come il gioco riaperto: le preferenze restano, la scena è nuova
            SceneManager.LoadScene("Core");
            SceneManager.LoadScene(SandboxScene, LoadSceneMode.Additive);
            yield return WaitForLevel();
            yield return null;
            Assert.IsTrue(Director.Book.HasSeen(Hints.Move), "ricordato");
            Director.SetOn(true);
            for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime)
            {
                Assert.AreNotEqual(Hints.Move, Director.Current, "il muoversi non torna");
                yield return null;
            }
        }
    }
}
