using System.Linq;
using DarkDescent.Localization;
using DarkDescent.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.Editor
{
    /// <summary>
    /// La schermata di creazione del personaggio (D8 della M21) nella scena <c>Core</c>: una copia del pannello dei
    /// posti con il titolo e Indietro, a cui si aggiungono il palco del modello, le schede delle classi, il testo, i
    /// numeri, il nome e Comincia. Si rifà senza doppioni; in batchmode:
    /// <c>-executeMethod DarkDescent.Editor.CreationTools.InstallCreation</c>.
    /// </summary>
    public static class CreationTools
    {
        private const string CoreScene = "Assets/_Project/Scenes/Core.unity";
        private const string PanelName = "Creation";
        private const string Animations = "Assets/_Project/Art/Animations/KayKit/Rig_Medium_General.fbx";

        [MenuItem("DarkDescent/Classi/Metti la creazione nel menu")]
        public static void InstallCreation()
        {
            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var menu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
            var link = new SerializedObject(menu);
            var slots = (GameObject)link.FindProperty("_slotsPanel").objectReferenceValue;
            var parent = slots.transform.parent;

            var old = parent.Find(PanelName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var panel = Object.Instantiate(slots, parent);
            panel.name = PanelName;
            panel.transform.SetSiblingIndex(slots.transform.GetSiblingIndex() + 1);
            foreach (var slot in panel.GetComponentsInChildren<SaveSlotView>(true))
            {
                Object.DestroyImmediate(slot.gameObject);
            }

            var titleSource = panel.transform.Find("Title").gameObject;
            var backSource = panel.transform.Find("Back").gameObject;

            // il titolo in alto, Indietro sotto il modello
            Place(titleSource, new Vector2(0f, 450f), new Vector2(1200f, 70f));
            Strip(titleSource);
            Place(backSource, new Vector2(-520f, -440f), new Vector2(300f, 56f));

            // il modello che gira, a sinistra
            var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(panel.transform, false);
            Place(frame, new Vector2(-520f, 0f), new Vector2(448f, 572f));
            frame.GetComponent<Image>().color = new Color(0.55f, 0.42f, 0.2f, 1f);
            var previewGo = new GameObject("Preview", typeof(RectTransform), typeof(RawImage));
            previewGo.transform.SetParent(panel.transform, false);
            Place(previewGo, new Vector2(-520f, 0f), new Vector2(436f, 560f));
            var preview = previewGo.AddComponent<CharacterPreview>();
            var previewSo = new SerializedObject(preview);
            previewSo.FindProperty("_image").objectReferenceValue = previewGo.GetComponent<RawImage>();
            previewSo.FindProperty("_idle").objectReferenceValue = AssetDatabase.LoadAllAssetsAtPath(Animations).OfType<AnimationClip>().First(c => c.name == "Idle_A");
            previewSo.ApplyModifiedPropertiesWithoutUndo();

            // le schede delle classi, in fila
            var row = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(panel.transform, false);
            Place(row, new Vector2(330f, 270f), new Vector2(800f, 72f));
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            var template = Object.Instantiate(backSource, row.transform);
            template.name = "CardTemplate";
            Strip(template);
            template.GetComponentInChildren<TMP_Text>(true).text = "Class";
            // l'emblema della classe, a sinistra della scritta
            var emblem = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            emblem.transform.SetParent(template.transform, false);
            var emblemRect = (RectTransform)emblem.transform;
            emblemRect.anchorMin = emblemRect.anchorMax = new Vector2(0f, 0.5f);
            emblemRect.pivot = new Vector2(0.5f, 0.5f);
            emblemRect.anchoredPosition = new Vector2(44f, 0f);
            emblemRect.sizeDelta = new Vector2(56f, 56f);
            emblem.GetComponent<Image>().raycastTarget = false;
            // la scheda scelta ha il bordo dorato, acceso dal pannello
            var outline = template.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.82f, 0.38f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.enabled = false;

            // la descrizione, i numeri e il nome
            var description = Text(titleSource, panel.transform, "Description", new Vector2(330f, 130f), new Vector2(800f, 130f), 30f, TextAlignmentOptions.TopLeft);
            var attributes = Text(titleSource, panel.transform, "Attributes", new Vector2(330f, 20f), new Vector2(800f, 44f), 28f, TextAlignmentOptions.Left);
            attributes.color = new Color(1f, 0.82f, 0.38f, 1f);
            var nameLabel = Text(titleSource, panel.transform, "NameLabel", new Vector2(330f, -70f), new Vector2(800f, 40f), 28f, TextAlignmentOptions.Left);
            var field = NameField(backSource, titleSource, panel.transform, out var hint);

            // Comincia, grande, sotto il nome
            var start = Object.Instantiate(backSource, panel.transform);
            start.name = "Start";
            Place(start, new Vector2(330f, -280f), new Vector2(420f, 64f));
            var startLabel = start.GetComponentInChildren<TMP_Text>(true);
            Label(start, TextKeys.CreateStart, "Begin");

            var creation = menu.GetComponent<CharacterCreationPanel>() ?? menu.gameObject.AddComponent<CharacterCreationPanel>();
            var so = new SerializedObject(creation);
            so.FindProperty("_window").objectReferenceValue = panel;
            so.FindProperty("_title").objectReferenceValue = titleSource.GetComponent<TMP_Text>();
            so.FindProperty("_cardTemplate").objectReferenceValue = template.GetComponent<Button>();
            so.FindProperty("_preview").objectReferenceValue = preview;
            so.FindProperty("_description").objectReferenceValue = description;
            so.FindProperty("_attributes").objectReferenceValue = attributes;
            so.FindProperty("_nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("_name").objectReferenceValue = field;
            so.FindProperty("_nameHint").objectReferenceValue = hint;
            so.FindProperty("_start").objectReferenceValue = start.GetComponent<Button>();
            so.FindProperty("_startLabel").objectReferenceValue = startLabel;
            so.FindProperty("_backButton").objectReferenceValue = backSource.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            link.FindProperty("_creation").objectReferenceValue = creation;
            link.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DarkDescent] creazione del personaggio: la schermata in {CoreScene}");
        }

        private static void Place(GameObject go, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // le scritte della schermata le scrive il codice: la chiave della copia non deve riscriverle
        private static void Strip(GameObject go)
        {
            foreach (var localized in go.GetComponentsInChildren<LocalizedText>(true))
            {
                Object.DestroyImmediate(localized);
            }
        }

        private static void Label(GameObject button, string key, string text)
        {
            var localized = button.GetComponentInChildren<LocalizedText>(true);
            var so = new SerializedObject(localized);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
            localized.GetComponent<TMP_Text>().text = text;
        }

        // un testo come il titolo, più piccolo e a sinistra
        private static TMP_Text Text(GameObject source, Transform parent, string name, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var copy = Object.Instantiate(source, parent);
            copy.name = name;
            Strip(copy);
            Place(copy, position, size);
            var text = copy.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.text = name;
            return text;
        }

        // il campo del nome: lo sfondo del pulsante, il testo come il titolo e il suggerimento in chiaro
        private static TMP_InputField NameField(GameObject buttonSource, GameObject textSource, Transform parent, out TMP_Text hint)
        {
            var go = new GameObject("Name", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            Place(go, new Vector2(330f, -150f), new Vector2(520f, 58f));
            var image = go.GetComponent<Image>();
            var buttonImage = buttonSource.GetComponent<Image>();
            image.sprite = buttonImage.sprite;
            image.type = buttonImage.type;
            image.color = new Color(0.12f, 0.1f, 0.09f, 1f);

            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(go.transform, false);
            var areaRect = (RectTransform)area.transform;
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(16f, 6f);
            areaRect.offsetMax = new Vector2(-16f, -6f);

            var text = Text(textSource, area.transform, "Text", Vector2.zero, Vector2.zero, 30f, TextAlignmentOptions.Left);
            text.text = "";
            Stretch(text.rectTransform);
            hint = Text(textSource, area.transform, "Hint", Vector2.zero, Vector2.zero, 30f, TextAlignmentOptions.Left);
            hint.text = "";
            hint.fontStyle = FontStyles.Italic;
            hint.color = new Color(1f, 1f, 1f, 0.4f);
            Stretch(hint.rectTransform);

            var field = go.GetComponent<TMP_InputField>();
            field.textViewport = areaRect;
            field.textComponent = text;
            field.placeholder = hint;
            field.targetGraphic = image;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = 16;
            field.richText = false;
            return field;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
