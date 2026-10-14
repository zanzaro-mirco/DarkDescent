using System.Linq;
using DarkDescent.Localization;
using DarkDescent.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarkDescent.Tests
{
    /// <summary>Il filtro retrò (D2 della M11): palette, scelte delle opzioni, passo del renderer.</summary>
    public class RetroFilterTests
    {
        private static RetroPalette Palette => AssetDatabase.LoadAssetAtPath<RetroPalette>("Assets/_Project/Data/RetroPalette.asset");

        [Test, Description("La palette ha 32 colori diversi, con il nero; il colore più vicino a uno della palette è lui stesso, e a un grigio scuro è un grigio scuro")]
        public void Palette_Has32DistinctColors()
        {
            var palette = Palette;
            Assert.AreEqual(RetroPalette.Size, palette.Count);
            var colors = Enumerable.Range(0, palette.Count).Select(i => palette[i]).ToList();
            Assert.AreEqual(RetroPalette.Size, colors.Distinct().Count(), "nessun doppione");
            Assert.Contains(Color.black, colors);
            foreach (var color in colors)
            {
                Assert.AreEqual(color, palette.Nearest(color));
            }

            Color dark = palette.Nearest(new Color(0.12f, 0.12f, 0.13f));
            Assert.Less(Mathf.Max(dark.r, dark.g, dark.b), 0.2f);
            Assert.Less(Mathf.Max(dark.r, dark.g, dark.b) - Mathf.Min(dark.r, dark.g, dark.b), 0.05f, "un grigio va su un grigio");
        }

        [Test, Description("Le scelte girano spento, leggero, pieno; dalle preferenze un valore sbagliato vale spento, la scelta di partenza; ogni scelta ha un nome suo nelle due lingue")]
        public void Modes_CycleAndHaveNames()
        {
            Assert.AreEqual(RetroMode.Light, RetroModes.Next(RetroMode.Off));
            Assert.AreEqual(RetroMode.Full, RetroModes.Next(RetroMode.Light));
            Assert.AreEqual(RetroMode.Off, RetroModes.Next(RetroMode.Full));
            Assert.AreEqual(RetroMode.Off, RetroModes.Default);
            Assert.AreEqual(RetroMode.Full, RetroModes.FromPreference(2));
            Assert.AreEqual(RetroMode.Off, RetroModes.FromPreference(7));
            Assert.AreEqual(RetroMode.Off, RetroModes.FromPreference(-1));

            var table = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text;
            var keys = new[] { RetroMode.Off, RetroMode.Light, RetroMode.Full }.Select(RetroModes.NameKey).ToList();
            Assert.AreEqual(3, keys.Distinct().Count());
            foreach (var key in keys.Append(TextKeys.OptionsRetro))
            {
                StringAssert.Contains("\n" + key + ",", table.Replace("\r\n", "\n"), key);
            }
        }

        [Test, Description("I due renderer hanno il passo a tutto schermo del filtro, acceso, con il suo materiale, dopo il post-processing, che legge l'immagine")]
        public void Renderers_HaveTheRetroPass()
        {
            foreach (var path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                var feature = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().SingleOrDefault(f => f.name == "RetroFilter");
                Assert.IsNotNull(feature, path);
                Assert.IsTrue(feature.isActive, path);
                Assert.AreEqual("DarkDescent/Retro", feature.passMaterial.shader.name);
                Assert.AreEqual(FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing, feature.injectionPoint);
                Assert.IsTrue(feature.fetchColorBuffer);
            }
        }
    }
}
