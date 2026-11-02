using System;
using DarkDescent.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.Editor
{
    /// <summary>
    /// La console in basso alla Diablo 3 (D18 della M20): la pietra e la cornice generate con Z-Image
    /// (<c>Tools/ComfyUI/images/hud.py</c>), le sfere attaccate ai lati, il portale e le otto pozioni a
    /// sinistra, le quattro caselle delle abilità a destra con il tasto scritto sotto, e il menu per
    /// assegnarle. Le misure sono quelle dell'anteprima approvata da Mirco. Si rifà con un comando e si
    /// può rilanciare: i nodi che ci sono già si aggiornano.
    /// </summary>
    public static class HudTools
    {
        // la console: 958 x 132, nel centro in basso (come l'anteprima del passo 20.4b)
        private const float ConsoleWidth = 958f;
        private const float ConsoleHeight = 132f;
        private const float ConsoleBottom = 6f;
        private const float Pad = 26f;
        private const float PotionSize = 54f;
        private const float SkillSize = 78f;
        private const float Gap = 6f;

        // la cornice a nove parti: 84 pixel di bordo steso a 63 (84 / 1,333)
        private const float FrameScale = 1.333f;
        private const float BackScale = 1.6f;

        // la sfera sporge sulla console di tanto, ai due lati
        private const float OrbOverlap = 34f;
        private const float OrbSize = 176f;
        private const float OrbBottom = 4f;

        private static readonly Color Gold = new Color(0.9f, 0.77f, 0.45f, 1f);
        private static readonly Color Well = new Color(0.03f, 0.025f, 0.02f, 0.92f);

        [MenuItem("DarkDescent/UI/Console in basso")]
        public static void BuildConsole()
        {
            foreach (string name in new[] { "hud_back", "hud_frame", "skill_frame", "potion_frame" })
            {
                AssetDatabase.ImportAsset($"{GeneratedUiImporter.Folder}{name}.png", ImportAssetOptions.ForceUpdate);
            }

            var scene = EditorSceneManager.OpenScene(TalentTools.CoreScene, OpenSceneMode.Single);
            var hud = FindHud(scene);
            var back = Art("hud_back");
            var frame = Art("hud_frame");
            var skillFrame = Art("skill_frame");
            var potionFrame = Art("potion_frame");
            TMP_FontAsset font = hud.Find("PortalSlot/Key").GetComponent<TMP_Text>().font;

            var healthOrb = (RectTransform)hud.Find("HealthOrb");
            var manaOrb = (RectTransform)hud.Find("ManaOrb");

            // la console sotto le sfere e le caselle
            var console = Node(hud, "Console");
            Place(console, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, ConsoleBottom), new Vector2(ConsoleWidth, ConsoleHeight));
            console.SetSiblingIndex(healthOrb.GetSiblingIndex());
            var consoleBack = Node(console, "ConsoleBack", typeof(Image));
            Stretch(consoleBack, Vector2.zero, Vector2.zero);
            var backImage = consoleBack.GetComponent<Image>();
            backImage.sprite = back;
            backImage.type = Image.Type.Tiled;
            backImage.pixelsPerUnitMultiplier = BackScale;
            backImage.color = new Color(0.82f, 0.8f, 0.78f, 1f);
            backImage.raycastTarget = true;
            var consoleFrame = Node(console, "ConsoleFrame", typeof(Image));
            Stretch(consoleFrame, Vector2.zero, Vector2.zero);
            var frameImage = consoleFrame.GetComponent<Image>();
            frameImage.sprite = frame;
            frameImage.type = Image.Type.Sliced;
            frameImage.fillCenter = false;
            frameImage.pixelsPerUnitMultiplier = FrameScale;
            frameImage.color = Color.white;
            frameImage.raycastTarget = false;
            consoleBack.SetAsFirstSibling();
            consoleFrame.SetSiblingIndex(1);

            // le sfere attaccate ai lati della console
            float edge = ConsoleWidth / 2f - OrbOverlap;
            Place(healthOrb, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-edge, OrbBottom), new Vector2(OrbSize, OrbSize));
            Place(manaOrb, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(edge, OrbBottom), new Vector2(OrbSize, OrbSize));
            var points = (RectTransform)hud.Find("PointsButton");
            Place(points, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-edge - OrbSize / 2f, OrbBottom + OrbSize + 6f), points.sizeDelta);

            // da sinistra: il portale, le otto pozioni, poi le quattro caselle
            float left = -ConsoleWidth / 2f + Pad;
            float potionY = ConsoleBottom + (ConsoleHeight - PotionSize) / 2f - 4f;
            float skillY = ConsoleBottom + (ConsoleHeight - SkillSize) / 2f - 2f;

            var portal = (RectTransform)hud.Find("PortalSlot");
            Place(portal, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(left, potionY), new Vector2(PotionSize, PotionSize));
            DressSlot(portal, potionFrame, font, false);

            float beltLeft = left + PotionSize + 14f;
            var belt = (RectTransform)hud.Find("Belt");
            float beltWidth = 8 * PotionSize + 7 * Gap;
            Place(belt, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(beltLeft, potionY), new Vector2(beltWidth, PotionSize));
            for (int i = 0; i < belt.childCount; i++)
            {
                var slot = (RectTransform)belt.GetChild(i);
                Place(slot, Vector2.zero, Vector2.zero, new Vector2(i * (PotionSize + Gap), 0f), new Vector2(PotionSize, PotionSize));
                DressSlot(slot, potionFrame, font, false);
            }

            float skillsLeft = beltLeft + beltWidth + 34f;
            float skillsWidth = 4 * SkillSize + 3 * Gap;
            var bar = (RectTransform)hud.Find("SpellBar");
            Place(bar, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(skillsLeft, skillY), new Vector2(skillsWidth, SkillSize));
            var view = bar.GetComponent<SpellBarView>();
            var slots = new SpellSlotView[SpellBookSlots];
            for (int i = 0; i < SpellBookSlots; i++)
            {
                var slot = bar.Find($"Spell_{i + 1}") as RectTransform;
                if (slot == null)
                {
                    var template = (RectTransform)bar.Find("Spell_1");
                    slot = (RectTransform)UnityEngine.Object.Instantiate(template.gameObject, bar).transform;
                    slot.name = $"Spell_{i + 1}";
                }

                Place(slot, Vector2.zero, Vector2.zero, new Vector2(i * (SkillSize + Gap), 0f), new Vector2(SkillSize, SkillSize));
                DressSlot(slot, skillFrame, font, true);
                slots[i] = slot.GetComponent<SpellSlotView>();
            }

            for (int i = bar.childCount - 1; i >= 0; i--)
            {
                if (bar.GetChild(i).name.StartsWith("Spell_") && int.Parse(bar.GetChild(i).name.Substring(6)) > SpellBookSlots)
                {
                    UnityEngine.Object.DestroyImmediate(bar.GetChild(i).gameObject);
                }
            }

            // l'esperienza sopra la console, il bonus del santuario sopra il portale
            var experience = (RectTransform)hud.Find("ExperienceBar");
            Place(experience, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, ConsoleBottom + ConsoleHeight + 4f), new Vector2(ConsoleWidth - 40f, 10f));
            var shrine = (RectTransform)hud.Find("ShrineBonus");
            Place(shrine, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(left + PotionSize / 2f, ConsoleBottom + ConsoleHeight + 40f), shrine.sizeDelta);

            // il menu delle abilità
            var menu = BuildMenu(hud, skillFrame, font, out var menuView);
            var barSerialized = new SerializedObject(view);
            var slotsProperty = barSerialized.FindProperty("_slots");
            slotsProperty.arraySize = SpellBookSlots;
            for (int i = 0; i < SpellBookSlots; i++)
            {
                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
                var slotSerialized = new SerializedObject(slots[i]);
                slotSerialized.FindProperty("_bar").objectReferenceValue = view;
                slotSerialized.FindProperty("_slot").intValue = i;
                slotSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            barSerialized.FindProperty("_menu").objectReferenceValue = menuView;
            barSerialized.ApplyModifiedPropertiesWithoutUndo();

            // le finestre prendono la pelle del gioco, anche quella del menu
            UiSkinTools.Apply(scene, UiSkinTools.LoadOrCreateSkin());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[HudTools] console: {slots.Length} caselle, {belt.childCount} pozioni, menu {menu.name} in {TalentTools.CoreScene}");
        }

        private const int SpellBookSlots = 4;

        // ---- la casella: cornice, pozzo scuro sotto l'icona, e quel che serve alle abilità

        private static void DressSlot(RectTransform slot, Sprite frame, TMP_FontAsset font, bool skill)
        {
            var image = slot.GetComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = true;

            float inset = skill ? 9f : 7f;
            var well = Node(slot, "Well", typeof(Image));
            Stretch(well, new Vector2(inset, inset), new Vector2(-inset, -inset));
            var wellImage = well.GetComponent<Image>();
            wellImage.color = Well;
            wellImage.raycastTarget = false;
            well.SetAsFirstSibling();

            var icon = (RectTransform)slot.Find("Icon");
            Stretch(icon, new Vector2(inset + 2f, inset + 2f), new Vector2(-inset - 2f, -inset - 2f));
            icon.GetComponent<Image>().preserveAspect = true;

            var key = (RectTransform)slot.Find("Key");
            var keyText = key.GetComponent<TMP_Text>();
            if (!skill)
            {
                key.anchoredPosition = new Vector2(7f, -5f);
                return;
            }

            // il tasto sotto, sul bordo d'oro, e il nome per le abilità senza icona
            var pill = Node(slot, "KeyBack", typeof(Image));
            Place(pill, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(SkillSize - 22f, 18f));
            pill.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
            pill.GetComponent<Image>().raycastTarget = false;
            key.SetAsLastSibling();
            Place(key, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(SkillSize - 14f, 22f));
            keyText.alignment = TextAlignmentOptions.Center;
            keyText.fontSize = 15f;
            keyText.fontStyle = FontStyles.Bold;
            keyText.color = Gold;
            keyText.raycastTarget = false;

            var cooldown = (RectTransform)slot.Find("Cooldown");
            Stretch(cooldown, new Vector2(inset, inset), new Vector2(-inset, -inset));

            var selected = (RectTransform)slot.Find("Selected");
            Stretch(selected, new Vector2(-6f, -6f), new Vector2(6f, 6f));
            var selectedImage = selected.GetComponent<Image>();
            selectedImage.sprite = frame;
            selectedImage.type = Image.Type.Sliced;
            selectedImage.fillCenter = false;
            selectedImage.pixelsPerUnitMultiplier = 2.4f;
            selectedImage.color = new Color(1f, 0.82f, 0.35f, 1f);
            selectedImage.raycastTarget = false;
            selectedImage.enabled = false;

            var name = Node(slot, "Name", typeof(TextMeshProUGUI));
            Stretch(name, new Vector2(inset + 2f, inset + 16f), new Vector2(-inset - 2f, -inset - 2f));
            var nameText = name.GetComponent<TextMeshProUGUI>();
            nameText.font = font;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 8f;
            nameText.fontSizeMax = 15f;
            nameText.color = Color.white;
            nameText.raycastTarget = false;
            nameText.text = "";
            nameText.enabled = false;

            var serialized = new SerializedObject(slot.GetComponent<SpellSlotView>());
            serialized.FindProperty("_icon").objectReferenceValue = icon.GetComponent<Image>();
            serialized.FindProperty("_name").objectReferenceValue = nameText;
            serialized.FindProperty("_key").objectReferenceValue = keyText;
            serialized.FindProperty("_selectedFrame").objectReferenceValue = selectedImage;
            serialized.FindProperty("_cooldown").objectReferenceValue = cooldown.GetComponent<Image>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- il menu delle abilità: una finestra con una riga per casella

        private static RectTransform BuildMenu(Transform hud, Sprite frame, TMP_FontAsset font, out SkillMenu menuView)
        {
            var root = Node(hud, "SkillsUI", typeof(SkillMenu));
            Stretch(root, Vector2.zero, Vector2.zero);

            // sotto la morte, la dissolvenza e il titolo del livello, che coprono tutto l'HUD
            var death = hud.Find("DeathScreen");
            root.SetSiblingIndex(root.GetSiblingIndex() < death.GetSiblingIndex() ? death.GetSiblingIndex() - 1 : death.GetSiblingIndex());
            menuView = root.GetComponent<SkillMenu>();
            var window = Node(root, "Window", typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Place(window, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(780f, 0f));
            window.GetComponent<Image>().raycastTarget = true;
            var layout = window.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(56, 56, 52, 52);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = window.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = Text(window, "Title", font, 28f, Gold, TextAlignmentOptions.Center, 40f);
            title.fontStyle = FontStyles.Bold;

            var rows = new GameObject[SpellBookSlots];
            var rowTitles = new TMP_Text[SpellBookSlots];
            var rowChoices = new RectTransform[SpellBookSlots];
            var rowEmpty = new TMP_Text[SpellBookSlots];
            for (int i = 0; i < SpellBookSlots; i++)
            {
                var row = Node(window, $"Row_{i + 1}", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 14f;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;
                row.GetComponent<LayoutElement>().minHeight = 100f;

                var rowTitle = Text(row, "Title", font, 22f, Gold, TextAlignmentOptions.MidlineLeft, 100f);
                rowTitle.GetComponent<LayoutElement>().preferredWidth = 150f;

                var choices = Node(row, "Choices", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                var choicesLayout = choices.GetComponent<HorizontalLayoutGroup>();
                choicesLayout.spacing = 8f;
                choicesLayout.childAlignment = TextAnchor.MiddleLeft;
                choicesLayout.childControlWidth = true;
                choicesLayout.childControlHeight = true;
                choicesLayout.childForceExpandWidth = false;
                choicesLayout.childForceExpandHeight = false;
                var choicesElement = choices.GetComponent<LayoutElement>();
                choicesElement.flexibleWidth = 1f;
                choicesElement.minHeight = 100f;

                var empty = Text(choices, "Empty", font, 20f, new Color(0.6f, 0.58f, 0.54f, 1f), TextAlignmentOptions.MidlineLeft, 40f);
                empty.GetComponent<LayoutElement>().preferredWidth = 360f;

                rows[i] = row.gameObject;
                rowTitles[i] = rowTitle;
                rowChoices[i] = choices;
                rowEmpty[i] = empty;
            }

            var template = BuildChoiceTemplate(root, frame, font);
            var serialized = new SerializedObject(menuView);
            serialized.FindProperty("_window").objectReferenceValue = window.gameObject;
            serialized.FindProperty("_title").objectReferenceValue = title;
            serialized.FindProperty("_choiceTemplate").objectReferenceValue = template;
            for (int i = 0; i < SpellBookSlots; i++)
            {
                serialized.FindProperty("_rows").GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
                serialized.FindProperty("_rowTitles").GetArrayElementAtIndex(i).objectReferenceValue = rowTitles[i];
                serialized.FindProperty("_rowChoices").GetArrayElementAtIndex(i).objectReferenceValue = rowChoices[i];
                serialized.FindProperty("_rowEmpty").GetArrayElementAtIndex(i).objectReferenceValue = rowEmpty[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            window.gameObject.SetActive(false);
            return root;
        }

        private static SkillChoiceView BuildChoiceTemplate(Transform root, Sprite frame, TMP_FontAsset font)
        {
            var choice = Node(root, "ChoiceTemplate", typeof(Image), typeof(Button), typeof(SkillChoiceView), typeof(LayoutElement));
            choice.sizeDelta = new Vector2(88f, 106f);
            choice.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var element = choice.GetComponent<LayoutElement>();
            element.preferredWidth = 88f;
            element.preferredHeight = 106f;

            var well = Node(choice, "Well", typeof(Image));
            Place(well, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(54f, 54f));
            well.GetComponent<Image>().color = Well;
            well.GetComponent<Image>().raycastTarget = false;

            var border = Node(choice, "Frame", typeof(Image));
            Place(border, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(72f, 72f));
            var borderImage = border.GetComponent<Image>();
            borderImage.sprite = frame;
            borderImage.raycastTarget = false;

            var icon = Node(choice, "Icon", typeof(Image));
            Place(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(54f, 54f));
            icon.GetComponent<Image>().preserveAspect = true;
            icon.GetComponent<Image>().raycastTarget = false;

            var inner = Text(choice, "Inner", font, 13f, Color.white, TextAlignmentOptions.Center, 0f);
            Destroy(inner.GetComponent<LayoutElement>());
            Place((RectTransform)inner.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(54f, 54f));
            inner.enableAutoSizing = true;
            inner.fontSizeMin = 7f;
            inner.fontSizeMax = 13f;

            var current = Node(choice, "Current", typeof(Image));
            Place(current, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 5f), new Vector2(82f, 82f));
            var currentImage = current.GetComponent<Image>();
            currentImage.sprite = frame;
            currentImage.type = Image.Type.Sliced;
            currentImage.fillCenter = false;
            currentImage.pixelsPerUnitMultiplier = 2.4f;
            currentImage.color = new Color(1f, 0.82f, 0.35f, 1f);
            currentImage.raycastTarget = false;
            currentImage.enabled = false;

            var name = Text(choice, "Name", font, 13f, new Color(0.86f, 0.82f, 0.72f, 1f), TextAlignmentOptions.Top, 0f);
            Destroy(name.GetComponent<LayoutElement>());
            var nameRect = (RectTransform)name.transform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.offsetMin = new Vector2(0f, 0f);
            nameRect.offsetMax = new Vector2(0f, 30f);
            name.enableAutoSizing = true;
            name.fontSizeMin = 8f;
            name.fontSizeMax = 13f;

            var button = choice.GetComponent<Button>();
            button.targetGraphic = borderImage;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.7f, 1f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.5f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var serialized = new SerializedObject(choice.GetComponent<SkillChoiceView>());
            serialized.FindProperty("_icon").objectReferenceValue = icon.GetComponent<Image>();
            serialized.FindProperty("_inner").objectReferenceValue = inner;
            serialized.FindProperty("_name").objectReferenceValue = name;
            serialized.FindProperty("_current").objectReferenceValue = currentImage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            choice.gameObject.SetActive(false);
            return choice.GetComponent<SkillChoiceView>();
        }

        // ---- piccoli aiuti

        private static void Destroy(UnityEngine.Object component)
        {
            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static Transform FindHud(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "HUD")
                {
                    return root.transform;
                }
            }

            throw new InvalidOperationException("HUD non trovato in Core");
        }

        private static Sprite Art(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedUiImporter.Folder}{name}.png");
            if (sprite == null)
            {
                throw new InvalidOperationException($"manca lo sprite {name}");
            }

            return sprite;
        }

        // un nodo figlio con quel nome: se c'è lo riusa, e gli aggiunge i componenti che mancano
        private static RectTransform Node(Transform parent, string name, params Type[] components)
        {
            var found = parent.Find(name) as RectTransform;
            if (found == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                found = (RectTransform)go.transform;
            }

            foreach (var type in components)
            {
                if (found.GetComponent(type) == null)
                {
                    found.gameObject.AddComponent(type);
                }
            }

            return found;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment, float height)
        {
            var node = Node(parent, name, typeof(TextMeshProUGUI), typeof(LayoutElement));
            var text = node.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            node.GetComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
