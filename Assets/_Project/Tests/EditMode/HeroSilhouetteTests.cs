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

        [Test, Description("Nemici e persone del borgo (richiesta di Mirco, M21) stanno sul loro layer, non su quello del cavaliere: lo coprono senza lasciare la sagoma. I loro collider restano dove sono")]
        public void Characters_AreOnTheirOwnLayer()
        {
            Assert.AreEqual(HeroSilhouette.CharacterLayer, LayerMask.NameToLayer(HeroSilhouette.CharacterLayerName));
            foreach (string name in new[] { "Skeleton", "SkeletonMage", "Brute", "Swarm", "BoneKing", "Grumm", "Morvak", "Town/Blacksmith", "Town/Healer", "Town/VillageFire" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/{name}.prefab");
                Assert.IsNotNull(prefab, name);
                var meshes = prefab.GetComponentsInChildren<Animator>(true).SelectMany(a => a.GetComponentsInChildren<Renderer>(true))
                    .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToList();
                Assert.That(meshes.Count, Is.GreaterThan(0), name);
                Assert.IsTrue(meshes.All(r => r.gameObject.layer == HeroSilhouette.CharacterLayer), $"{name}: tutto il modello");
                Assert.IsTrue(prefab.GetComponentsInChildren<Collider>(true).All(c => c.gameObject.layer != HeroSilhouette.CharacterLayer), $"{name}: i collider no");
            }
        }

        [Test, Description("I renderer disegnano prima chi copre e scrive lo stencil, poi la sagoma che lo rispetta, poi il cavaliere: dove c'è un nemico la sagoma non passa")]
        public void Renderers_StencilKeepsTheSilhouetteOffCharacters()
        {
            int people = 1 << HeroSilhouette.CharacterLayer;
            foreach (string path in HeroSilhouette.Renderers)
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                Assert.AreEqual(0, data.opaqueLayerMask & people, $"{path}: non con gli altri opachi");
                var features = data.rendererFeatures.OfType<RenderObjects>().ToList();
                int characters = features.FindIndex(f => f.name == HeroSilhouette.CharacterFeature);
                int hidden = features.FindIndex(f => f.name == HeroSilhouette.HiddenFeature);
                Assert.That(characters, Is.GreaterThanOrEqualTo(0), path);
                Assert.Less(characters, hidden, $"{path}: chi copre prima della sagoma");

                var write = features[characters].settings;
                Assert.AreEqual(people, write.filterSettings.LayerMask.value, path);
                Assert.IsTrue(write.stencilSettings.overrideStencilState);
                Assert.AreEqual((HeroSilhouette.CharacterStencil, StencilOp.Replace), (write.stencilSettings.stencilReference, write.stencilSettings.passOperation), path);
                Assert.AreEqual(RenderObjects.RenderObjectsSettings.OverrideMaterialMode.None, write.overrideMode, $"{path}: con i loro materiali");

                var read = features[hidden].settings.stencilSettings;
                Assert.IsTrue(read.overrideStencilState, path);
                Assert.AreEqual((HeroSilhouette.CharacterStencil, CompareFunction.NotEqual), (read.stencilReference, read.stencilCompareFunction), $"{path}: la sagoma non dove c'è lo stencil");
            }
        }
    }
}
