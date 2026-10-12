using System;
using System.Collections;
using System.IO;
using DarkDescent.Core;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Il click dei menu (D5 della M11).</summary>
    public class UiClickSoundTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentClick_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            AudioListener.pause = false;
            yield return null;
            Directory.Delete(_folder, true);
        }

        [UnityTest, Description("I pulsanti dei menu fanno click, quelli spenti all'avvio compresi; il click è una sorgente 2D sul gruppo UI")]
        public IEnumerator MenuButtons_Click()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            var sound = UnityEngine.Object.FindFirstObjectByType<UiClickSound>();
            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenu>();
            var pause = UnityEngine.Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            var source = sound.GetComponent<AudioSource>();
            Assert.AreEqual(0f, source.spatialBlend, "2D");
            Assert.AreEqual("UI", source.outputAudioMixerGroup.name);
            Assert.Greater(sound.Count, 10, "tutti i pulsanti dei menu");

            int played = 0;
            sound.Played += () => played++;
            menu.LoadButton.onClick.Invoke();
            Assert.AreEqual(1, played, "il menu principale");

            // anche quelli del menu di pausa, spento all'avvio
            pause.ResumeButton.onClick.Invoke();
            Assert.AreEqual(2, played, "il menu di pausa");
        }

        [UnityTest, Description("Con il gioco in pausa l'audio del mondo si ferma, il click no")]
        public IEnumerator Click_PlaysDuringPause()
        {
            yield return LoadGeneratedCore();
            var sound = UnityEngine.Object.FindFirstObjectByType<UiClickSound>();
            var pause = UnityEngine.Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            pause.Open();
            Assert.IsTrue(AudioListener.pause, "la pausa ferma l'audio");
            Assert.IsTrue(sound.GetComponent<AudioSource>().ignoreListenerPause, "il click passa lo stesso");

            int played = 0;
            sound.Played += () => played++;
            pause.ResumeButton.onClick.Invoke();
            Assert.AreEqual(1, played);
            Assert.IsFalse(pause.IsOpen);
        }
    }
}
