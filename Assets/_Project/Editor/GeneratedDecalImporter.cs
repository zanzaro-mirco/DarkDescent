using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Impostazioni di import dell'atlante delle macchie a terra generato con Z-Image-Turbo
    /// (<c>Tools/ComfyUI/decalatlas.py</c>, M17 D12): texture con i mipmap, perché si vede da lontano e
    /// inclinata, compressa, al più 1024 pixel, con la trasparenza. Scritte qui e non nell'Inspector,
    /// così un atlante rifatto si reimporta sempre uguale.
    /// </summary>
    public sealed class GeneratedDecalImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Art/Generated/Decals/";

        public const string AtlasPath = Folder + "decal_atlas.png";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || assetImporter is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
        }
    }
}
