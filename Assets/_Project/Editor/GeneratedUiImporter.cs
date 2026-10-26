using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Impostazioni di import delle immagini dell'interfaccia generate con Z-Image-Turbo
    /// (<c>Tools/ComfyUI/images/</c>, M17 D12): sprite senza mipmap, compressi, al più 512 pixel, con i
    /// bordi delle nove parti scritti qui. Così un'immagine rifatta dalla ricetta si reimporta sempre
    /// uguale, senza passare dall'Inspector.
    /// </summary>
    public sealed class GeneratedUiImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Art/Generated/UI/";

        public const string PanelBack = "panel_back";
        public const string PanelFrame = "panel_frame";
        public const string ButtonPlate = "button_plate";

        /// <summary>Il bordo delle nove parti della cornice: l'angolo tenuto da <c>nine_slice</c> in <c>images/ui.py</c>.</summary>
        public const int FrameBorder = 76;

        /// <summary>Il bordo della placca dei pulsanti, come in <c>uiparts.py</c>.</summary>
        public const int PlateBorder = 6;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || assetImporter is not TextureImporter importer)
            {
                return;
            }

            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.spritePixelsPerUnit = 100f;
            importer.wrapMode = name == PanelBack ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            // nove parti e ripetizione vogliono la mesh piena
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            int border = name == PanelFrame ? FrameBorder : name == ButtonPlate ? PlateBorder : 0;
            importer.spriteBorder = new Vector4(border, border, border, border);
        }
    }
}
