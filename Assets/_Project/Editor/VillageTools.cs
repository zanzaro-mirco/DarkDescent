using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// I materiali dei pezzi del borgo (M18, 18.1): uno per atlante, copiati da <c>M_Dungeon</c> perché
    /// prendano la luce come il resto del mondo, e quelli dei personaggi nuovi, copiati da quello del
    /// ladro. Scrive anche quante animazioni nuove ci sono e quali girano in loop, per controllarle senza
    /// aprire l'editor. Si rifà con un comando, anche in batchmode:
    /// <c>-executeMethod DarkDescent.Editor.VillageTools.Setup</c>.
    /// </summary>
    public static class VillageTools
    {
        public const string MaterialFolder = "Assets/_Project/Art/Materials/";
        public const string DungeonMaterial = MaterialFolder + "M_Dungeon.mat";
        public const string RogueMaterial = MaterialFolder + "M_KayKitRogue.mat";

        /// <summary>Materiale, atlante e materiale di partenza di ogni pacchetto e personaggio nuovo.</summary>
        public static readonly (string Material, string Texture, string Source)[] Materials =
        {
            ("M_Village", KayKitImporter.ModelFolder + "Village/hexagons_medieval.png", DungeonMaterial),
            ("M_Graveyard", KayKitImporter.ModelFolder + "Graveyard/halloweenbits_texture.png", DungeonMaterial),
            ("M_Nature", KayKitImporter.ModelFolder + "Nature/forest_texture.png", DungeonMaterial),
            ("M_KayKitMage", KayKitImporter.ModelFolder + "mage_texture.png", RogueMaterial),
            ("M_KayKitRanger", KayKitImporter.ModelFolder + "ranger_texture.png", RogueMaterial),
        };

        [MenuItem("DarkDescent/Borgo/Materiali dei pezzi")]
        public static void Setup()
        {
            foreach (var (name, texturePath, sourcePath) in Materials)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                var source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                if (texture == null || source == null)
                {
                    throw new FileNotFoundException($"manca {(texture == null ? texturePath : sourcePath)}");
                }

                string path = MaterialFolder + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(source) { name = name };
                    AssetDatabase.CreateAsset(material, path);
                }
                else
                {
                    material.CopyPropertiesFromMaterial(source);
                }

                material.SetTexture("_BaseMap", texture);
                material.SetTexture("_MainTex", texture);
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssets();
            Report();
        }

        /// <summary>Quanti pezzi per cartella e le animazioni nuove, con quelle in loop.</summary>
        public static void Report()
        {
            foreach (string folder in new[] { "Village", "Graveyard", "Nature" })
            {
                int count = AssetDatabase.FindAssets("t:Model", new[] { KayKitImporter.ModelFolder + folder }).Length;
                Debug.Log($"[DarkDescent] borgo: {count} modelli in {folder}");
            }

            foreach (string file in new[] { "Rig_Medium_Simulation.fbx", "Rig_Medium_Special.fbx", "Rig_Medium_Tools.fbx" })
            {
                var clips = AssetDatabase.LoadAllAssetsAtPath(KayKitImporter.AnimationFolder + file)
                    .OfType<AnimationClip>()
                    .Where(clip => !clip.name.StartsWith("__preview__"))
                    .Select(clip => clip.isLooping ? clip.name + " (loop)" : clip.name);
                Debug.Log($"[DarkDescent] borgo: {file}: {string.Join(", ", clips)}");
            }
        }
    }
}
