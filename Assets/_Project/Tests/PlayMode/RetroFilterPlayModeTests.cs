using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DarkDescent.Core;
using DarkDescent.Rendering;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Il filtro retrò sull'immagine vera e nelle opzioni (D2 della M11).</summary>
    public class RetroFilterPlayModeTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentRetro_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            PlayerPrefs.DeleteKey(RetroFilter.Preference);
            yield return null;
            Directory.Delete(_folder, true);
        }

        // l'immagine della camera senza interfaccia, in sRGB come la vede il giocatore
        private static Color32[] Render()
        {
            var camera = Camera.main;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            var target = new RenderTexture(320, 180, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(320, 180, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
            texture.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            var pixels = texture.GetPixels32();
            UnityEngine.Object.Destroy(texture);
            target.Release();
            return pixels;
        }

        [UnityTest, Description("Pieno, ogni pixel della cripta è un colore della palette; spento, l'immagine ne ha molti di più; il leggero è la scelta di partenza")]
        public IEnumerator Full_UsesOnlyThePalette()
        {
            yield return LoadGeneratedCore();
            var filter = UnityEngine.Object.FindFirstObjectByType<RetroFilter>();
            Assert.AreEqual(RetroMode.Light, filter.Mode, "leggero all'avvio");
            yield return new WaitForSeconds(0.5f);

            var palette = AssetDatabase.LoadAssetAtPath<RetroPalette>("Assets/_Project/Data/RetroPalette.asset");
            var allowed = new List<Color32>();
            for (int i = 0; i < palette.Count; i++)
            {
                allowed.Add(palette[i]);
            }

            filter.SetMode(RetroMode.Full);
            int outside = 0;
            var used = new HashSet<Color32>();
            foreach (var pixel in Render())
            {
                used.Add(new Color32(pixel.r, pixel.g, pixel.b, 255));
                if (!allowed.Exists(c => Math.Abs(c.r - pixel.r) <= 2 && Math.Abs(c.g - pixel.g) <= 2 && Math.Abs(c.b - pixel.b) <= 2))
                {
                    outside++;
                }
            }

            Assert.AreEqual(0, outside, "pixel fuori dalla palette");
            Assert.LessOrEqual(used.Count, RetroPalette.Size);

            filter.SetMode(RetroMode.Off);
            var free = new HashSet<Color32>(Render());
            Assert.Greater(free.Count, RetroPalette.Size * 3, "spento, l'immagine è quella vera");
        }

        [UnityTest, Description("Il pulsante delle opzioni gira le scelte, dice quella di adesso e la ricorda tra le preferenze")]
        public IEnumerator OptionsButton_CyclesAndRemembers()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            var options = UnityEngine.Object.FindFirstObjectByType<OptionsPanel>(FindObjectsInactive.Include);
            var filter = UnityEngine.Object.FindFirstObjectByType<RetroFilter>();
            options.Open();
            Assert.AreEqual("Retro filter: light", options.RetroText);

            options.RetroButton.onClick.Invoke();
            Assert.AreEqual(RetroMode.Full, filter.Mode);
            Assert.AreEqual("Retro filter: full", options.RetroText);
            Assert.AreEqual((int)RetroMode.Full, PlayerPrefs.GetInt(RetroFilter.Preference, -1));

            options.RetroButton.onClick.Invoke();
            Assert.AreEqual(RetroMode.Off, filter.Mode);
            Assert.AreEqual("Retro filter: off", options.RetroText);

            // riaperto il gioco, la scelta resta
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            Assert.AreEqual(RetroMode.Off, UnityEngine.Object.FindFirstObjectByType<RetroFilter>().Mode);
        }
    }
}
