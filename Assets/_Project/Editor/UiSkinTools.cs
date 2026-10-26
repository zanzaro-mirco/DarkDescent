using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkDescent.Editor
{
    /// <summary>Che cosa è un'immagine dell'interfaccia per la pelle.</summary>
    public enum UiRole
    {
        None,
        Window,
        Tooltip,
        Button,
    }

    /// <summary>
    /// Riveste l'interfaccia della scena Core con la <see cref="UiSkin"/> (M17, D4): le finestre e i
    /// tooltip prendono lo sfondo ripetuto e una cornice in nove parti (un figlio <see cref="FrameName"/>,
    /// fuori dai layout), i pulsanti la placca e i colori del passaggio del mouse. Il ruolo si
    /// riconosce dalla scena, non da una lista a mano, così una finestra nuova si riveste da sola.
    /// Si rifà con un comando, anche in batchmode:
    /// <c>-executeMethod DarkDescent.Editor.UiSkinTools.ApplyToCore</c>.
    /// </summary>
    public static class UiSkinTools
    {
        public const string SkinPath = "Assets/_Project/Data/UI/UiSkin.asset";
        public const string CoreScene = "Assets/_Project/Scenes/Core.unity";
        public const string FrameName = "SkinFrame";

        // pulsanti che restano come sono: le etichette a terra e il pulsante dei punti hanno uno stile loro
        private static readonly string[] OwnStyle = { "GroundLabels", "PointsButton" };

        [MenuItem("DarkDescent/UI/Riveste l'interfaccia")]
        public static void ApplyToCore()
        {
            var skin = LoadOrCreateSkin();
            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var counts = Apply(scene, skin);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DarkDescent] interfaccia rivestita: {counts[UiRole.Window]} finestre, {counts[UiRole.Tooltip]} tooltip, "
                + $"{counts[UiRole.Button]} pulsanti");
        }

        /// <summary>La pelle, creata con le tre immagini importate se non c'è ancora.</summary>
        public static UiSkin LoadOrCreateSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            if (skin != null)
            {
                return skin;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SkinPath));
            skin = ScriptableObject.CreateInstance<UiSkin>();
            skin.SetSprites(Sprite(GeneratedUiImporter.PanelBack), Sprite(GeneratedUiImporter.PanelFrame), Sprite(GeneratedUiImporter.ButtonPlate));
            AssetDatabase.CreateAsset(skin, SkinPath);
            AssetDatabase.SaveAssets();
            return skin;
        }

        private static Sprite Sprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedUiImporter.Folder}{name}.png");
            if (sprite == null)
            {
                throw new FileNotFoundException($"manca l'immagine {name}.png in {GeneratedUiImporter.Folder}: va generata con Tools/ComfyUI");
            }

            return sprite;
        }

        /// <summary>Il ruolo di un'immagine, dalla sua posizione nella scena.</summary>
        public static UiRole RoleOf(Image image)
        {
            if (image.name == FrameName)
            {
                return UiRole.None;
            }

            string path = PathOf(image.transform);
            if (image.TryGetComponent<Button>(out var button) && button.targetGraphic == image)
            {
                foreach (string own in OwnStyle)
                {
                    if (path.Contains(own))
                    {
                        return UiRole.None;
                    }
                }

                return UiRole.Button;
            }

            var parent = image.transform.parent;
            if (image.name == "Box" && parent != null && parent.name.Contains("Tooltip"))
            {
                return UiRole.Tooltip;
            }

            // le finestre dell'HUD (inventario, scheda, mercante) e le scatole dei menu
            if (image.name == "Window" && path.StartsWith("HUD/"))
            {
                return UiRole.Window;
            }

            if (image.name == "Box" && path.StartsWith("MenuUI/"))
            {
                return UiRole.Window;
            }

            return path == "MenuUI/VictoryUI/Credits" ? UiRole.Window : UiRole.None;
        }

        /// <summary>Il percorso nella scena, dalla radice: "HUD/InventoryUI/Window".</summary>
        public static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (var t = transform; t != null; t = t.parent)
            {
                parts.Add(t.name);
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>Riveste ogni immagine della scena secondo il suo ruolo; restituisce quante per ruolo.</summary>
        public static Dictionary<UiRole, int> Apply(Scene scene, UiSkin skin)
        {
            var counts = new Dictionary<UiRole, int> { [UiRole.None] = 0, [UiRole.Window] = 0, [UiRole.Tooltip] = 0, [UiRole.Button] = 0 };
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var image in root.GetComponentsInChildren<Image>(true))
                {
                    var role = RoleOf(image);
                    counts[role]++;
                    switch (role)
                    {
                        case UiRole.Window:
                            Dress(image, skin, skin.WindowTint, skin.WindowFrameScale);
                            break;
                        case UiRole.Tooltip:
                            Dress(image, skin, skin.TooltipTint, skin.TooltipFrameScale);
                            break;
                        case UiRole.Button:
                            DressButton(image, skin);
                            break;
                    }
                }
            }

            return counts;
        }

        private static void Dress(Image image, UiSkin skin, Color tint, float frameScale)
        {
            image.sprite = skin.PanelBack;
            image.type = Image.Type.Tiled;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = tint;
            EditorUtility.SetDirty(image);

            var frameTransform = image.transform.Find(FrameName);
            if (frameTransform == null)
            {
                var go = new GameObject(FrameName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                go.layer = image.gameObject.layer;
                frameTransform = go.transform;
                frameTransform.SetParent(image.transform, false);
            }

            // primo figlio: sopra lo sfondo, sotto il contenuto
            frameTransform.SetAsFirstSibling();
            var rect = (RectTransform)frameTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            frameTransform.GetComponent<LayoutElement>().ignoreLayout = true;
            var frame = frameTransform.GetComponent<Image>();
            frame.sprite = skin.PanelFrame;
            frame.type = Image.Type.Sliced;
            frame.fillCenter = false;
            frame.pixelsPerUnitMultiplier = frameScale;
            frame.color = Color.white;
            frame.raycastTarget = false;
            EditorUtility.SetDirty(frame);
        }

        private static void DressButton(Image image, UiSkin skin)
        {
            image.sprite = skin.ButtonPlate;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = skin.ButtonScale;
            image.color = Color.white;
            EditorUtility.SetDirty(image);

            var button = image.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = skin.ButtonNormal;
            colors.highlightedColor = skin.ButtonHighlighted;
            colors.selectedColor = skin.ButtonNormal;
            colors.pressedColor = skin.ButtonPressed;
            colors.disabledColor = skin.ButtonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            EditorUtility.SetDirty(button);
        }
    }
}
