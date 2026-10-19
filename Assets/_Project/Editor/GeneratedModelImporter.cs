using UnityEditor;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Impostazioni di import dei modelli fatti in Blender da script (<c>Tools/Blender/</c>, M13 D9),
    /// le stesse dei modelli KayKit: niente materiali (il prefab mette <c>M_Dungeon</c>), niente
    /// animazioni, camere o luci, scala del file. Così un FBX rifatto dallo script si reimporta
    /// sempre uguale, senza passare dall'Inspector.
    /// </summary>
    public sealed class GeneratedModelImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Art/Models/DarkDescent/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder) || assetImporter is not ModelImporter importer)
            {
                return;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
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
