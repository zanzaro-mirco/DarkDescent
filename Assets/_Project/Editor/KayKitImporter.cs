using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Impostazioni di import dei pezzi KayKit arrivati con il borgo (M18): i pacchetti Medieval Hexagon,
    /// Halloween Bits e Forest Nature, i personaggi degli abitanti e le animazioni nuove. I pezzi sono
    /// come i modelli KayKit della cripta: niente materiali (li mette il prefab), niente animazioni. Le
    /// animazioni usano lo scheletro del cavaliere, come quelle importate alla M1, e quelle che durano
    /// girano in loop. Così un file si reimporta sempre uguale, senza passare dall'Inspector.
    /// </summary>
    public sealed class KayKitImporter : AssetPostprocessor
    {
        public const string ModelFolder = "Assets/_Project/Art/Models/KayKit/";
        public const string AnimationFolder = "Assets/_Project/Art/Animations/KayKit/";
        public const string KnightPath = ModelFolder + "Knight.fbx";

        // il fabbro del borgo (D18 della M18): con il suo avatar, per le pose; i materiali importati
        // restano, perché li usa l'elmo d'orso
        public const string BarbarianPath = ModelFolder + "Barbarian.fbx";

        private static readonly string[] PropFolders = { "Village/", "Graveyard/", "Nature/" };
        private static readonly string[] Characters = { "Mage.fbx", "Ranger.fbx", "Rogue.fbx" };
        private static readonly string[] Animations =
        {
            "Rig_Medium_Simulation.fbx", "Rig_Medium_Special.fbx", "Rig_Medium_Tools.fbx",
        };

        // le animazioni che durano finché qualcuno non le cambia: sedersi, salutare, lavorare
        private static readonly HashSet<string> Looping = new HashSet<string>
        {
            "Sit_Floor_Idle", "Sit_Chair_Idle", "Lie_Idle", "Waving", "Cheering", "Push_Ups", "Sit_Ups",
            "Skeletons_Idle", "Skeletons_Walking",
            "Chopping", "Digging", "Hammering", "Lockpicking", "Pickaxing", "Sawing",
            "Holding_A", "Holding_B", "Holding_C", "Working_A", "Working_B", "Working_C",
            "Fishing_Idle", "Fishing_Reeling", "Fishing_Struggling",
        };

        public static bool IsProp(string path)
        {
            foreach (string folder in PropFolders)
            {
                if (path.StartsWith(ModelFolder + folder))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsLooping(string clip)
        {
            return Looping.Contains(clip);
        }

        private static bool IsCharacter(string path)
        {
            return path.StartsWith(ModelFolder) && System.Array.IndexOf(Characters, path.Substring(ModelFolder.Length)) >= 0;
        }

        private static bool IsAnimation(string path)
        {
            return path.StartsWith(AnimationFolder) && System.Array.IndexOf(Animations, path.Substring(AnimationFolder.Length)) >= 0;
        }

        private void OnPreprocessModel()
        {
            if (assetImporter is not ModelImporter importer)
            {
                return;
            }

            if (IsProp(assetPath))
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                Common(importer);
            }
            else if (IsCharacter(assetPath))
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
                Common(importer);
            }
            else if (assetPath == BarbarianPath)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
            }
            else if (IsAnimation(assetPath))
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.animationType = ModelImporterAnimationType.Generic;
                var knight = AssetDatabase.LoadAssetAtPath<Avatar>(KnightPath);
                if (knight != null)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = knight;
                }

                importer.importAnimation = true;
                Common(importer);
            }
        }

        private void OnPreprocessAnimation()
        {
            if (!IsAnimation(assetPath) || assetImporter is not ModelImporter importer)
            {
                return;
            }

            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = IsLooping(clip.name);
            }

            importer.clipAnimations = clips;
        }

        private static void Common(ModelImporter importer)
        {
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.isReadable = false;
            importer.globalScale = 1f;
            importer.useFileScale = true;
        }
    }
}
