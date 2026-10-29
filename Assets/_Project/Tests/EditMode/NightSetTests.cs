using System.Linq;
using DarkDescent.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Il borgo di notte e all'alba per i filmati (D3 e D5 della M19): candele, vetro, fuoco, ora ferma.</summary>
    public class NightSetTests
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/Village/";
        private const string LightingPath = "Assets/_Project/Data/Levels/VillageDayLighting.asset";
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private GameObject _root;

        [TearDown]
        public void Clean()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [Test, Description("Il vetro brilla con la luce; la candela spenta spegne tutti e due, anche se il buio dice di accenderla, e si riaccende")]
        public void Candle_SnuffsLightAndGlass()
        {
            _root = (GameObject)PrefabUtility.InstantiatePrefab(Load("HouseA_Red"));
            var night = _root.GetComponentInChildren<NightLight>();
            var light = night.GetComponent<Light>();
            var glass = _root.GetComponentsInChildren<Renderer>().First();

            night.SetLit(1f);
            Assert.IsTrue(light.enabled);
            Assert.Greater(Glow(glass), 1f, "il vetro acceso, in HDR");

            night.Snuff(0f);
            night.SetLit(1f);
            Assert.AreEqual(0f, night.Level);
            Assert.IsFalse(light.enabled, "la candela spenta resta spenta di notte");
            Assert.AreEqual(0f, Glow(glass), 1e-4f);

            night.Relight(0f);
            Assert.AreEqual(1f, night.Level);
            Assert.IsTrue(light.enabled);

            night.SetLit(0f);
            Assert.AreEqual(0f, Glow(glass), 1e-4f, "di giorno il vetro è quello del modello");
        }

        [Test, Description("Il vetro brilla solo dove la maschera dell'atlante dice vetro: le finestre delle case e il lume delle lanterne")]
        public void Glass_IsTheAtlasWindowCell()
        {
            foreach (var (material, cell) in new[] { ("M_Village", new Vector2Int(6, 1)), ("M_Graveyard", new Vector2Int(2, 3)) })
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Art/Materials/{material}.mat");
                Assert.IsTrue(mat.enabledKeywords.Any(k => k.name == "_EMISSION"), material);
                Assert.AreEqual(Color.black, mat.GetColor(EmissionColor), $"{material}: spento finché una luce non lo accende");
                Assert.AreNotEqual(0, (int)(mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive), $"{material}: URP tiene l'emissione");
                var mask = (Texture2D)mat.GetTexture("_EmissionMap");
                Assert.AreEqual((8, 4), (mask.width, mask.height), material);
                int lit = Enumerable.Range(0, 32).Count(i => mask.GetPixel(i % 8, i / 8).r > 0.5f);
                Assert.AreEqual(1, lit, $"{material}: un riquadro solo");
                Assert.Greater(mask.GetPixel(cell.x, 3 - cell.y).r, 0.5f, $"{material}: il riquadro del vetro");
            }

            foreach (string house in new[] { "HouseA_Red", "HouseA_Blue", "HouseB_Yellow", "HealerHouse", "Tavern", "Forge", "Chapel", "LanternPost" })
            {
                var night = Load(house).GetComponentInChildren<NightLight>();
                Assert.IsNotNull(night, house);
                Assert.Greater(night.GlowCount, 0, $"{house}: il vetro che brilla");
            }
        }

        [Test, Description("Il borgo fermo di notte resta notte anche se l'orologio dice giorno, e torna all'orologio quando lo si lascia")]
        public void Hold_StopsTheClock()
        {
            _root = new GameObject("Day");
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(_root.transform);
            var view = _root.AddComponent<DayCycleView>();
            var so = new SerializedObject(view);
            so.FindProperty("_lighting").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DayLighting>(LightingPath);
            so.FindProperty("_sun").objectReferenceValue = sun;
            so.ApplyModifiedPropertiesWithoutUndo();

            view.Bind(() => DayCycle.Start(DayMoment.Day) + 10f, null);
            Assert.AreEqual(DayMoment.Day, view.Phase.From);

            view.Hold(DayCycle.Start(DayMoment.Night) + 10f);
            Assert.IsTrue(view.IsHeld);
            Assert.AreEqual(DayMoment.Night, view.Phase.From);

            view.Release();
            Assert.IsFalse(view.IsHeld);
            Assert.AreEqual(DayMoment.Day, view.Phase.From);
        }

        [Test, Description("Il fuoco della piazza si spegne: niente luce e niente fiamme, restano i sassi; e si riaccende")]
        public void Hearth_GoesOut()
        {
            _root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Town/VillageFire.prefab"));
            var hearth = _root.GetComponent<Hearth>();
            var light = _root.GetComponentInChildren<Light>();
            int shown = _root.GetComponentsInChildren<Renderer>().Count(r => r.enabled);

            hearth.SetBurning(false);
            Assert.IsFalse(hearth.IsBurning);
            Assert.IsFalse(light.enabled);
            int left = _root.GetComponentsInChildren<Renderer>().Count(r => r.enabled);
            Assert.Less(left, shown, "le fiamme spariscono");
            Assert.Greater(left, 0, "i sassi e i paesani restano");

            hearth.SetBurning(true);
            Assert.IsTrue(light.enabled);
            Assert.AreEqual(shown, _root.GetComponentsInChildren<Renderer>().Count(r => r.enabled));
        }

        [Test, Description("La lanterna a mano brilla da sola, con la sua luce sotto il gancio; la tomba smossa ha la lapide rotta e la terra")]
        public void Props_AreReady()
        {
            var lantern = Load("HandLantern");
            var light = lantern.GetComponentInChildren<Light>();
            Assert.IsNotNull(light);
            Assert.Less(light.transform.localPosition.y, 0f, "la luce sotto il gancio");
            var lamp = lantern.GetComponentInChildren<Renderer>().sharedMaterial;
            Assert.Greater(lamp.GetColor(EmissionColor).maxColorComponent, 1f, "il lume acceso sempre");

            var grave = Load("GraveDug");
            var renderers = grave.GetComponentsInChildren<Renderer>();
            Assert.IsTrue(renderers.Any(r => r.sharedMaterial.name == "M_VillageDirt"), "la terra dei sentieri");
            Assert.IsTrue(renderers.Any(r => r.sharedMaterial.name == "M_Graveyard"), "la lapide del cimitero");
        }

        private static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + name + ".prefab");

        private static float Glow(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetColor(EmissionColor).maxColorComponent;
        }
    }
}
