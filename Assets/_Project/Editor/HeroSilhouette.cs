using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DarkDescent.Editor
{
    /// <summary>
    /// La sagoma del cavaliere dietro case, alberi e colonne (M18, 18.2): con la camera a 30° una casa
    /// sul lato vicino lo copre. Il cavaliere sta sul layer <see cref="LayerName"/>, che i renderer non
    /// disegnano con gli altri opachi: lo disegnano due passi dopo, prima la sagoma dove qualcosa lo
    /// copre (prova di profondità "più lontano", senza scrivere la profondità), poi lui come sempre.
    /// Così la sagoma non compare sul cavaliere stesso. Si rifà con un comando, anche in batchmode:
    /// <c>-executeMethod DarkDescent.Editor.HeroSilhouette.Setup</c>.
    /// </summary>
    public static class HeroSilhouette
    {
        public const string LayerName = "Hero";
        public const int Layer = 10;
        public const string HiddenFeature = "HeroHidden";
        public const string HeroFeature = "Hero";
        public const string MaterialPath = "Assets/_Project/Art/Materials/M_HeroSilhouette.mat";
        public const string PlayerPath = "Assets/_Project/Prefabs/Player.prefab";

        public static readonly string[] Renderers = { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" };

        // un ocra spento: si legge sul legno, sulla pietra e sull'erba, e non sembra un effetto magico
        private static readonly Color SilhouetteColor = new Color(0.78f, 0.6f, 0.32f);

        [MenuItem("DarkDescent/Borgo/Sagoma del cavaliere")]
        public static void Setup()
        {
            AddLayer();
            var material = SilhouetteMaterial();
            foreach (string path in Renderers)
            {
                ConfigureRenderer(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path), material);
            }

            MovePlayerModel();
            AssetDatabase.SaveAssets();
            Debug.Log("[DarkDescent] sagoma del cavaliere: layer, materiale, renderer e prefab pronti");
        }

        private static void AddLayer()
        {
            var tags = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            var layers = tags.FindProperty("layers");
            var slot = layers.GetArrayElementAtIndex(Layer);
            if (slot.stringValue != LayerName)
            {
                Debug.Assert(string.IsNullOrEmpty(slot.stringValue), $"il layer {Layer} è già {slot.stringValue}");
                slot.stringValue = LayerName;
                tags.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Material SilhouetteMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "M_HeroSilhouette" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.SetColor("_BaseColor", SilhouetteColor);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureRenderer(UniversalRendererData data, Material material)
        {
            int mask = 1 << Layer;
            data.opaqueLayerMask &= ~mask;

            foreach (var old in data.rendererFeatures.Where(f => f != null && (f.name == HiddenFeature || f.name == HeroFeature)).ToList())
            {
                data.rendererFeatures.Remove(old);
                AssetDatabase.RemoveObjectFromAsset(old);
                Object.DestroyImmediate(old, true);
            }

            var hidden = ScriptableObject.CreateInstance<RenderObjects>();
            hidden.name = HiddenFeature;
            hidden.settings.Event = RenderPassEvent.AfterRenderingOpaques;
            hidden.settings.filterSettings.RenderQueueType = RenderQueueType.Opaque;
            hidden.settings.filterSettings.LayerMask = mask;
            hidden.settings.overrideMode = RenderObjects.RenderObjectsSettings.OverrideMaterialMode.Material;
            hidden.settings.overrideMaterial = material;
            hidden.settings.overrideDepthState = true;
            hidden.settings.depthCompareFunction = CompareFunction.Greater;
            hidden.settings.enableWrite = false;

            var hero = ScriptableObject.CreateInstance<RenderObjects>();
            hero.name = HeroFeature;
            hero.settings.Event = RenderPassEvent.AfterRenderingOpaques;
            hero.settings.filterSettings.RenderQueueType = RenderQueueType.Opaque;
            hero.settings.filterSettings.LayerMask = mask;
            hero.settings.overrideMode = RenderObjects.RenderObjectsSettings.OverrideMaterialMode.None;

            // la sagoma prima: quando la disegna, il cavaliere non è ancora nella profondità
            foreach (var feature in new ScriptableRendererFeature[] { hidden, hero })
            {
                AssetDatabase.AddObjectToAsset(feature, data);
                data.rendererFeatures.Add(feature);
            }

            // la mappa degli id dei sotto-asset: senza, l'editor scarta le feature nuove alla riapertura
            typeof(ScriptableRendererData).GetMethod("ValidateRendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(data, null);
            data.SetDirty();
            EditorUtility.SetDirty(data);
        }

        // Solo i pezzi del modello sotto l'Animator: il corpo e quello che ha in mano. Luci, cerchio del
        // bersaglio e collider restano dove sono.
        private static void MovePlayerModel()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                foreach (var renderer in root.GetComponentInChildren<Animator>().GetComponentsInChildren<Renderer>(true))
                {
                    renderer.gameObject.layer = Layer;
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
