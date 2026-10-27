using System.Linq;
using DarkDescent.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DarkDescent.Tests
{
    /// <summary>La sagoma del cavaliere dietro case e alberi (M18, 18.2).</summary>
    public class HeroSilhouetteTests
    {
        [Test, Description("Il layer del cavaliere c'è, e il corpo del cavaliere ci sta sopra; i suoi collider no")]
        public void PlayerModel_IsOnTheHeroLayer()
        {
            Assert.AreEqual(HeroSilhouette.Layer, LayerMask.NameToLayer(HeroSilhouette.LayerName));
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(HeroSilhouette.PlayerPath);
            var body = player.GetComponentInChildren<Animator>().GetComponentsInChildren<Renderer>(true);
            Assert.That(body.Length, Is.GreaterThan(0));
            Assert.IsTrue(body.All(r => r.gameObject.layer == HeroSilhouette.Layer), "tutto il modello");
            Assert.IsTrue(player.GetComponentsInChildren<Collider>(true).All(c => c.gameObject.layer != HeroSilhouette.Layer), "i click e i colpi non cambiano");
        }

        [Test, Description("I renderer disegnano il cavaliere a parte: prima la sagoma dove qualcosa lo copre, poi lui")]
        public void Renderers_DrawTheSilhouetteFirst()
        {
            int mask = 1 << HeroSilhouette.Layer;
            foreach (string path in HeroSilhouette.Renderers)
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                Assert.AreEqual(0, data.opaqueLayerMask & mask, $"{path}: non con gli altri opachi");

                var features = data.rendererFeatures.OfType<RenderObjects>().ToList();
                int hidden = features.FindIndex(f => f.name == HeroSilhouette.HiddenFeature);
                int hero = features.FindIndex(f => f.name == HeroSilhouette.HeroFeature);
                Assert.That(hidden, Is.GreaterThanOrEqualTo(0), path);
                Assert.Greater(hero, hidden, $"{path}: la sagoma prima del cavaliere");

                var silhouette = features[hidden].settings;
                Assert.AreEqual(mask, silhouette.filterSettings.LayerMask.value, path);
                Assert.IsTrue(silhouette.overrideDepthState);
                Assert.AreEqual(CompareFunction.Greater, silhouette.depthCompareFunction, $"{path}: solo dove è coperto");
                Assert.IsFalse(silhouette.enableWrite);
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Material>(HeroSilhouette.MaterialPath), silhouette.overrideMaterial);
                Assert.AreEqual(mask, features[hero].settings.filterSettings.LayerMask.value, path);
                Assert.AreEqual(RenderObjects.RenderObjectsSettings.OverrideMaterialMode.None, features[hero].settings.overrideMode, $"{path}: il cavaliere con i suoi materiali");
            }
        }
    }
}
