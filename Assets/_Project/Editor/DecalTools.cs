using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Il materiale delle macchie a terra (M17, D5): Lit di URP trasparente con l'atlante, così le macchie
    /// prendono la luce delle torce come il pavimento; disegnato prima degli altri trasparenti (cerchi dei
    /// nemici speciali, avvisi degli attacchi), che restano sopra. Lo mette nei tileset di cripta e
    /// caverne. Si rifà con un comando, anche in batchmode:
    /// <c>-executeMethod DarkDescent.Editor.DecalTools.Setup</c>.
    /// </summary>
    public static class DecalTools
    {
        public const string MaterialPath = GeneratedDecalImporter.Folder + "M_Decals.mat";

        /// <summary>I tileset che hanno le macchie: la città no.</summary>
        public static readonly string[] Tilesets =
        {
            "Assets/_Project/Data/Levels/DungeonTileset.asset",
            "Assets/_Project/Data/Levels/CaveTileset.asset",
        };

        // prima dei trasparenti della coda di base (3000): i cerchi e gli avvisi si disegnano sopra
        public const int DecalQueue = (int)RenderQueue.Transparent - 10;

        [MenuItem("DarkDescent/Livelli/Materiale delle macchie a terra")]
        public static void Setup()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(GeneratedDecalImporter.AtlasPath);
            if (atlas == null)
            {
                throw new FileNotFoundException($"manca {GeneratedDecalImporter.AtlasPath}: va fatto con Tools/ComfyUI/decalatlas.py");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_Decals" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            Configure(material, atlas);
            EditorUtility.SetDirty(material);

            foreach (string path in Tilesets)
            {
                var tileset = AssetDatabase.LoadMainAssetAtPath(path);
                var so = new SerializedObject(tileset);
                so.FindProperty("_decalMaterial").objectReferenceValue = material;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tileset);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DarkDescent] macchie a terra: {MaterialPath} nei tileset di cripta e caverne");
        }

        /// <summary>Trasparente con la fusione sull'alfa, senza scrivere la profondità né fare ombra, opaca e senza riflessi.</summary>
        public static void Configure(Material material, Texture2D atlas)
        {
            material.SetTexture("_BaseMap", atlas);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.SetFloat("_EnvironmentReflections", 0f);
            material.SetFloat("_ReceiveShadows", 1f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("SHADOWCASTER", false);
            // URP ricalcola la coda dal tipo di superficie e da _QueueOffset: la coda si sposta da lì
            material.SetFloat("_QueueOffset", DecalQueue - (int)RenderQueue.Transparent);
            material.renderQueue = DecalQueue;
            material.enableInstancing = false;
        }
    }
}
