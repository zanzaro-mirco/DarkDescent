using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkDescent.Tests
{
    /// <summary>La pelle dell'interfaccia della M17 (D4, D12, D13): immagini importate, scena rivestita, testo leggibile.</summary>
    public class UiSkinTests
    {
        // il contrasto minimo tra il testo e lo sfondo medio sotto (D13 della M17)
        private const float MinContrast = 4.5f;

        private Scene _scene;
        private UiSkin _skin;

        [OneTimeSetUp]
        public void OpenCore()
        {
            _skin = AssetDatabase.LoadAssetAtPath<UiSkin>(UiSkinTools.SkinPath);
            _scene = EditorSceneManager.OpenScene(UiSkinTools.CoreScene, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void CloseCore()
        {
            EditorSceneManager.CloseScene(_scene, true);
        }

        private IEnumerable<Image> Images => _scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Image>(true));

        private static TextureImporter Importer(string name)
        {
            return (TextureImporter)AssetImporter.GetAtPath($"{GeneratedUiImporter.Folder}{name}.png");
        }

        // il colore medio di un'immagine, letto dal PNG (le texture importate non sono leggibili)
        private static Color Average(Sprite sprite)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            var pixels = texture.GetPixels();
            Object.DestroyImmediate(texture);
            var opaque = pixels.Where(p => p.a > 0.5f).ToList();
            return new Color(opaque.Average(p => p.r), opaque.Average(p => p.g), opaque.Average(p => p.b), 1f);
        }

        private static float Luminance(Color c)
        {
            static float Channel(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * Channel(c.r) + 0.7152f * Channel(c.g) + 0.0722f * Channel(c.b);
        }

        private static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        [Test, Description("Menu, morte e vittoria hanno lo sfondo (D6): uno sprite di 1920x1080 senza mipmap, primo figlio del fondo o del velo, che copre lo schermo senza deformarsi e non ferma i click")]
        public void Screens_HaveTheirBackdrops()
        {
            foreach (var (path, name) in UiSkinTools.Backdrops)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath($"{GeneratedUiImporter.BackgroundFolder}{name}.png");
                Assert.IsNotNull(importer, name);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, name);
                Assert.IsFalse(importer.mipmapEnabled, name);
                Assert.AreEqual(2048, importer.maxTextureSize, name);

                var parent = UiSkinTools.FindPath(_scene, path);
                Assert.IsNotNull(parent, path);
                var backdrop = parent.GetChild(0);
                Assert.AreEqual(UiSkinTools.BackdropName, backdrop.name, $"{path}: lo sfondo sotto a tutto");
                var image = backdrop.GetComponent<Image>();
                Assert.AreEqual(name, image.sprite.name, path);
                Assert.AreEqual(new Vector2(1920, 1080), image.sprite.rect.size, path);
                Assert.IsFalse(image.raycastTarget, path);
                var fitter = backdrop.GetComponent<AspectRatioFitter>();
                Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, fitter.aspectMode, path);
                Assert.AreEqual(16f / 9f, fitter.aspectRatio, 0.01f, path);
            }
        }

        [Test, Description("La morte, con lo sfondo opaco, copre tutto l'HUD: sopra restano solo la dissolvenza e il nome del livello")]
        public void DeathScreen_CoversTheHud()
        {
            var hud = UiSkinTools.FindPath(_scene, "HUD/DeathScreen").parent;
            int death = hud.Find("DeathScreen").GetSiblingIndex();
            Assert.AreEqual(hud.childCount - 3, death, "terzultimo");
            Assert.Greater(hud.Find("ScreenFader").GetSiblingIndex(), death);
            Assert.Greater(hud.Find("LevelTitle").GetSiblingIndex(), death);
            foreach (string piece in new[] { "HealthOrb", "ManaOrb", "ExperienceBar", "Belt", "SpellBar", "EnemyBar" })
            {
                Assert.Less(hud.Find(piece).GetSiblingIndex(), death, piece);
            }
        }

        [Test, Description("Le tre immagini generate entrano come sprite senza mipmap e al più di 512 pixel; lo sfondo si ripete, la cornice e la placca hanno i bordi delle nove parti")]
        public void Images_AreImportedForTheInterface()
        {
            foreach (string name in new[] { GeneratedUiImporter.PanelBack, GeneratedUiImporter.PanelFrame, GeneratedUiImporter.ButtonPlate })
            {
                var importer = Importer(name);
                Assert.IsNotNull(importer, name);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, name);
                Assert.IsFalse(importer.mipmapEnabled, name);
                Assert.LessOrEqual(importer.maxTextureSize, 512, name);
            }

            Assert.AreEqual(TextureWrapMode.Repeat, Importer(GeneratedUiImporter.PanelBack).wrapMode);
            Assert.AreEqual(Vector4.one * GeneratedUiImporter.FrameBorder, Importer(GeneratedUiImporter.PanelFrame).spriteBorder);
            Assert.AreEqual(Vector4.one * GeneratedUiImporter.PlateBorder, Importer(GeneratedUiImporter.ButtonPlate).spriteBorder);
            Assert.IsNotNull(_skin.PanelBack);
            Assert.IsNotNull(_skin.PanelFrame);
            Assert.IsNotNull(_skin.ButtonPlate);
        }

        [Test, Description("Ogni finestra e ogni tooltip della scena Core ha lo sfondo ripetuto e la cornice in nove parti fuori dai layout; ogni pulsante la placca e il colore al passaggio del mouse")]
        public void Core_IsDressed()
        {
            var counts = new Dictionary<UiRole, int>();
            foreach (var image in Images)
            {
                var role = UiSkinTools.RoleOf(image);
                counts[role] = counts.TryGetValue(role, out int n) ? n + 1 : 1;
                string where = UiSkinTools.PathOf(image.transform);
                if (role == UiRole.Window || role == UiRole.Tooltip)
                {
                    Assert.AreEqual(_skin.PanelBack, image.sprite, where);
                    Assert.AreEqual(Image.Type.Tiled, image.type, where);
                    var frame = image.transform.Find(UiSkinTools.FrameName);
                    Assert.IsNotNull(frame, where);
                    Assert.AreEqual(0, frame.GetSiblingIndex(), $"{where}: la cornice sotto il contenuto");
                    Assert.AreEqual(_skin.PanelFrame, frame.GetComponent<Image>().sprite, where);
                    Assert.IsFalse(frame.GetComponent<Image>().raycastTarget, where);
                    Assert.IsTrue(frame.GetComponent<LayoutElement>().ignoreLayout, where);
                }
                else if (role == UiRole.Button)
                {
                    Assert.AreEqual(_skin.ButtonPlate, image.sprite, where);
                    Assert.AreEqual(_skin.ButtonHighlighted, image.GetComponent<Button>().colors.highlightedColor, where);
                }
            }

            // inventario, scheda, mercante, conferma, difficoltà, opzioni, pausa, vittoria, riconoscimenti
            Assert.GreaterOrEqual(counts.GetValueOrDefault(UiRole.Window), 9);
            Assert.AreEqual(4, counts.GetValueOrDefault(UiRole.Tooltip));
            Assert.GreaterOrEqual(counts.GetValueOrDefault(UiRole.Button), 25);
        }

        [Test, Description("Il testo delle finestre, dei tooltip e dei pulsanti si legge: contrasto almeno 4,5:1 con lo sfondo medio sotto")]
        public void Text_IsReadableOnTheSkin()
        {
            Color window = Average(_skin.PanelBack) * _skin.WindowTint;
            Color tooltip = Average(_skin.PanelBack) * _skin.TooltipTint;
            Color plate = Average(_skin.ButtonPlate) * _skin.ButtonNormal;
            var poor = new List<string>();
            foreach (var image in Images)
            {
                var role = UiSkinTools.RoleOf(image);
                if (role == UiRole.None)
                {
                    continue;
                }

                Color back = role == UiRole.Window ? window : role == UiRole.Tooltip ? tooltip : plate;
                foreach (var text in image.GetComponentsInChildren<TMP_Text>(true))
                {
                    // il testo di un pulsante dentro una finestra sta sulla placca, non sulla finestra
                    if (role != UiRole.Button && text.GetComponentInParent<Button>(true) != null)
                    {
                        continue;
                    }

                    float contrast = Contrast(text.color, back);
                    if (contrast < MinContrast)
                    {
                        poor.Add($"{UiSkinTools.PathOf(text.transform)} {contrast:F1}");
                    }
                }
            }

            Assert.IsEmpty(poor, "testo poco leggibile:\n" + string.Join("\n", poor));
        }
    }
}
