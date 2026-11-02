using System;
using System.IO;
using DarkDescent.Core;
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
    /// Il pannello delle abilità della scena Core (D10 e D11 della M20): la finestra a tutto schermo con i due
    /// alberi, il tooltip e la finestrella delle scelte, l'avviso dei punti accanto alla sfera del mana e
    /// la voce dell'altare che ricomincia l'albero. Si rilancia: quello che c'è lo riusa. Le placche dei nodi
    /// (tonda, quadrata, ottagonale) le disegna da sé in <c>Art/UI</c>, finché le icone di Z-Image (20.7) non
    /// le coprono.
    /// </summary>
    public static class TalentPanelTools
    {
        public const string ArtFolder = "Assets/_Project/Art/UI";

        private const float CellWidth = 88f;
        private const float CellHeight = 92f;
        private const float NodeSize = 72f;
        private const float TreeWidth = 7f * CellWidth;
        private const float TreeHeight = 7f * CellHeight;
        private const float AltarStep = 80f;

        private static readonly Color Gold = new Color(0.9f, 0.77f, 0.45f, 1f);
        private static readonly Color Dim = new Color(0.6f, 0.58f, 0.54f, 1f);
        private static readonly string[] Shapes = { "round", "square", "octagon" };

        [MenuItem("DarkDescent/UI/Pannello delle abilità")]
        public static void Build()
        {
            var plates = new Sprite[Shapes.Length];
            var rings = new Sprite[Shapes.Length];
            for (int i = 0; i < Shapes.Length; i++)
            {
                plates[i] = SaveShape(Shapes[i], false);
                rings[i] = SaveShape(Shapes[i], true);
            }

            var dash = SaveDash();

            var scene = EditorSceneManager.OpenScene(TalentTools.CoreScene, OpenSceneMode.Single);
            var hud = HudTools.FindHud(scene);
            var font = hud.Find("PortalSlot/Key").GetComponent<TMP_Text>().font;

            var root = BuildPanel(hud, font, plates, rings, dash, out var panel);
            var button = BuildPointsButton(hud, font);
            BuildAltarEntry(font);

            var composition = UnityEngine.Object.FindFirstObjectByType<CompositionRoot>(FindObjectsInactive.Include);
            var serialized = new SerializedObject(composition);
            serialized.FindProperty("_talentPanel").objectReferenceValue = panel;
            serialized.FindProperty("_talentPointsButton").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[TalentPanelTools] pannello {root.name} e avviso costruiti");
            UiSkinTools.ApplyToCore();
        }

        private static RectTransform BuildPanel(Transform hud, TMP_FontAsset font, Sprite[] plates, Sprite[] rings, Sprite dash, out TalentPanel panel)
        {
            var root = HudTools.Node(hud, "TalentsUI", typeof(TalentPanel));
            HudTools.Stretch(root, Vector2.zero, Vector2.zero);

            // sotto la morte e la dissolvenza, che coprono tutto l'HUD
            var death = hud.Find("DeathScreen");
            root.SetSiblingIndex(root.GetSiblingIndex() < death.GetSiblingIndex() ? death.GetSiblingIndex() - 1 : death.GetSiblingIndex());
            panel = root.GetComponent<TalentPanel>();

            // il contenitore che si accende e si spegne: il velo e la finestra; il resto (i modelli) resta spento da sé
            var dialog = HudTools.Node(root, "Dialog");
            HudTools.Stretch(dialog, Vector2.zero, Vector2.zero);
            foreach (string moved in new[] { "Window", "Veil" })
            {
                var found = root.Find(moved);
                if (found != null)
                {
                    found.SetParent(dialog, false);
                }
            }

            var window = HudTools.Node(dialog, "Window", typeof(Image));
            window.gameObject.SetActive(true);
            HudTools.Stretch(window, new Vector2(110f, 24f), new Vector2(-110f, -24f));
            window.GetComponent<Image>().raycastTarget = true;

            // il velo che oscura il gioco e prende i click, e un'ombra dentro la cornice che rende opaco il fondo
            var veil = HudTools.Node(dialog, "Veil", typeof(Image));
            HudTools.Stretch(veil, Vector2.zero, Vector2.zero);
            veil.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            veil.SetAsFirstSibling();
            var shade = HudTools.Node(window, "Shade", typeof(Image));
            shade.SetAsFirstSibling();
            HudTools.Stretch(shade, new Vector2(40f, 40f), new Vector2(-40f, -40f));
            shade.GetComponent<Image>().color = new Color(0.02f, 0.018f, 0.015f, 0.72f);
            shade.GetComponent<Image>().raycastTarget = false;

            var title = HudTools.Text(window, "Title", font, 32f, Gold, TextAlignmentOptions.Center, 44f);
            title.fontStyle = FontStyles.Bold;
            HudTools.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(700f, 44f));
            var close = MakeButton(window, "Close", font, 150f, 44f, 22f);
            HudTools.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-64f, -34f), new Vector2(150f, 44f));

            var classArea = BuildTree(window, "ClassTree", 0.27f, font);
            var specArea = BuildTree(window, "SpecTree", 0.73f, font);

            // la specializzazione da scegliere: due schede, il pulsante e una riga che dice com'è
            // la prima versione li metteva sotto la finestra: via quelli rimasti
            foreach (string stale in new[] { "Tab_1", "Tab_2", "Choose", "Note" })
            {
                var old = window.Find(stale);
                if (old != null)
                {
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                }
            }

            var specBlock = specArea.Area.parent;
            float below = -(44f + TreeHeight + 18f);
            var tabs = new Button[2];
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i] = MakeButton(specBlock, $"Tab_{i + 1}", font, 160f, 42f, 22f);
                HudTools.Place((RectTransform)tabs[i].transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * 172f, below), new Vector2(160f, 42f));
            }

            var choose = MakeButton(specBlock, "Choose", font, 250f, 42f, 20f);
            HudTools.Place((RectTransform)choose.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, below), new Vector2(250f, 42f));
            var note = HudTools.Text(specBlock, "Note", font, 20f, Dim, TextAlignmentOptions.TopLeft, 50f);
            HudTools.Place(note.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, below - 52f), new Vector2(TreeWidth, 56f));

            // sotto: Applica e Annulla al centro, la guida a sinistra e "non ancora applicato" a destra
            var apply = MakeButton(window, "Apply", font, 270f, 54f, 24f);
            HudTools.Place((RectTransform)apply.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 34f), new Vector2(270f, 54f));
            var cancel = MakeButton(window, "CancelChanges", font, 220f, 54f, 24f);
            HudTools.Place((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150f, 34f), new Vector2(220f, 54f));
            var hint = HudTools.Text(window, "Hint", font, 18f, Dim, TextAlignmentOptions.MidlineLeft, 40f);
            HudTools.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(44f, 41f), new Vector2(520f, 40f));
            var pending = HudTools.Text(window, "Pending", font, 20f, new Color(0.88f, 0.54f, 0.42f, 1f), TextAlignmentOptions.MidlineRight, 40f);
            HudTools.Place(pending.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 41f), new Vector2(520f, 40f));

            // i modelli: spenti, se ne fanno copie
            var nodeTemplate = BuildNodeTemplate(root, font);
            var line = HudTools.Node(root, "LineTemplate", typeof(Image));
            line.GetComponent<Image>().raycastTarget = false;
            HudTools.Place(line, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 5f));
            var gate = HudTools.Node(root, "GateTemplate", typeof(Image));
            var gateImage = gate.GetComponent<Image>();
            gateImage.sprite = dash;
            gateImage.type = Image.Type.Tiled;
            gateImage.raycastTarget = false;
            HudTools.Place(gate, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(TreeWidth, 4f));
            var gateLabel = HudTools.Text(gate, "Label", font, 17f, Dim, TextAlignmentOptions.MidlineLeft, 24f);
            HudTools.Place(gateLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(2f, 2f), new Vector2(60f, 22f));

            // il velo sotto la finestrella delle scelte, e la finestrella; l'ultimo il tooltip
            var blocker = HudTools.Node(window, "ChoiceBlocker", typeof(Image), typeof(Button));
            HudTools.Stretch(blocker, Vector2.zero, Vector2.zero);
            blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            blocker.GetComponent<Button>().transition = Selectable.Transition.None;
            var popup = BuildFloating(window, "ChoiceTooltip", font, 360f, 0);
            var choices = new Button[2];
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i] = MakeButton(popup, $"Option_{i + 1}", font, 340f, 96f, 19f);
                var label = choices[i].GetComponentInChildren<TMP_Text>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                choices[i].GetComponent<LayoutElement>().preferredHeight = 96f;
            }

            var tooltip = BuildFloating(window, "TalentTooltip", font, 400f, 20f);
            var tooltipText = tooltip.Find("Text").GetComponent<TMP_Text>();

            var so = new SerializedObject(panel);
            so.FindProperty("_window").objectReferenceValue = dialog.gameObject;
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_close").objectReferenceValue = close;
            WriteArea(so.FindProperty("_classArea"), classArea);
            WriteArea(so.FindProperty("_specArea"), specArea);
            so.FindProperty("_cell").vector2Value = new Vector2(CellWidth, CellHeight);
            so.FindProperty("_nodeTemplate").objectReferenceValue = nodeTemplate;
            so.FindProperty("_lineTemplate").objectReferenceValue = line.GetComponent<Image>();
            so.FindProperty("_gateTemplate").objectReferenceValue = gate;
            for (int i = 0; i < Shapes.Length; i++)
            {
                so.FindProperty("_plates").GetArrayElementAtIndex(i).objectReferenceValue = plates[i];
                so.FindProperty("_rings").GetArrayElementAtIndex(i).objectReferenceValue = rings[i];
            }

            for (int i = 0; i < tabs.Length; i++)
            {
                so.FindProperty("_specTabs").GetArrayElementAtIndex(i).objectReferenceValue = tabs[i];
            }

            so.FindProperty("_chooseButton").objectReferenceValue = choose;
            so.FindProperty("_specNote").objectReferenceValue = note;
            so.FindProperty("_apply").objectReferenceValue = apply;
            so.FindProperty("_cancel").objectReferenceValue = cancel;
            so.FindProperty("_pendingNote").objectReferenceValue = pending;
            so.FindProperty("_hint").objectReferenceValue = hint;
            so.FindProperty("_tooltip").objectReferenceValue = tooltip;
            so.FindProperty("_tooltipText").objectReferenceValue = tooltipText;
            so.FindProperty("_choicePopup").objectReferenceValue = popup;
            for (int i = 0; i < choices.Length; i++)
            {
                so.FindProperty("_choiceButtons").GetArrayElementAtIndex(i).objectReferenceValue = choices[i];
            }

            so.FindProperty("_choiceBlocker").objectReferenceValue = blocker.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            dialog.gameObject.SetActive(false);
            nodeTemplate.gameObject.SetActive(false);
            line.gameObject.SetActive(false);
            gate.gameObject.SetActive(false);
            return root;
        }

        // un albero: l'intestazione con il nome e i punti, e l'area con le linee dietro i nodi
        private static TalentPanel.TreeArea BuildTree(Transform window, string name, float anchorX, TMP_FontAsset font)
        {
            var block = HudTools.Node(window, name);
            HudTools.Place(block, new Vector2(anchorX, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(TreeWidth, TreeHeight + 44f));
            var label = HudTools.Text(block, "Name", font, 28f, Gold, TextAlignmentOptions.MidlineLeft, 38f);
            label.fontStyle = FontStyles.Bold;
            HudTools.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(360f, 38f));
            var points = HudTools.Text(block, "Points", font, 22f, Dim, TextAlignmentOptions.MidlineRight, 38f);
            HudTools.Place(points.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(300f, 38f));

            var area = HudTools.Node(block, "Area");
            HudTools.Place(area, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -44f), new Vector2(TreeWidth, TreeHeight));
            var lines = HudTools.Node(area, "Lines");
            HudTools.Place(lines, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            var nodes = HudTools.Node(area, "Nodes");
            HudTools.Place(nodes, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            return new TalentPanel.TreeArea { Area = area, Lines = lines, Nodes = nodes, Name = label, Points = points };
        }

        private static void WriteArea(SerializedProperty property, TalentPanel.TreeArea area)
        {
            property.FindPropertyRelative("Area").objectReferenceValue = area.Area;
            property.FindPropertyRelative("Lines").objectReferenceValue = area.Lines;
            property.FindPropertyRelative("Nodes").objectReferenceValue = area.Nodes;
            property.FindPropertyRelative("Name").objectReferenceValue = area.Name;
            property.FindPropertyRelative("Points").objectReferenceValue = area.Points;
        }

        private static TalentNodeView BuildNodeTemplate(Transform root, TMP_FontAsset font)
        {
            var node = HudTools.Node(root, "NodeTemplate", typeof(TalentNodeView));
            HudTools.Place(node, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(NodeSize, NodeSize));
            // la placca ritaglia l'icona: la maschera ha la sua forma, tonda, quadrata o ottagonale
            var plate = HudTools.Node(node, "Plate", typeof(Image), typeof(Mask));
            HudTools.Stretch(plate, Vector2.zero, Vector2.zero);
            plate.GetComponent<Mask>().showMaskGraphic = true;
            var movedIcon = node.Find("Icon");
            if (movedIcon != null)
            {
                movedIcon.SetParent(plate, false);
            }

            var icon = HudTools.Node(plate, "Icon", typeof(Image));
            HudTools.Stretch(icon, Vector2.zero, Vector2.zero);
            icon.GetComponent<Image>().raycastTarget = false;
            icon.GetComponent<Image>().preserveAspect = false;
            var ring = HudTools.Node(node, "Ring", typeof(Image));
            HudTools.Stretch(ring, Vector2.zero, Vector2.zero);
            ring.GetComponent<Image>().raycastTarget = false;
            var label = HudTools.Text(node, "Label", font, 13f, Color.white, TextAlignmentOptions.Center, 52f);
            HudTools.Stretch(label.rectTransform, new Vector2(2f, 7f), new Vector2(-2f, -7f));
            label.enableAutoSizing = true;
            label.fontSizeMin = 7f;
            label.fontSizeMax = 12f;
            label.overflowMode = TextOverflowModes.Truncate;
            var rank = HudTools.Text(node, "Rank", font, 17f, Color.white, TextAlignmentOptions.BottomRight, 22f);
            rank.fontStyle = FontStyles.Bold;
            HudTools.Place(rank.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(7f, -9f), new Vector2(48f, 22f));

            var view = node.GetComponent<TalentNodeView>();
            var so = new SerializedObject(view);
            so.FindProperty("_plate").objectReferenceValue = plate.GetComponent<Image>();
            so.FindProperty("_ring").objectReferenceValue = ring.GetComponent<Image>();
            so.FindProperty("_icon").objectReferenceValue = icon.GetComponent<Image>();
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_rank").objectReferenceValue = rank;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // una finestrella fluttuante nella pelle dei tooltip: il "Box" sotto un padre "...Tooltip"; con un
        // testo dentro se <paramref name="fontSize"/> è positivo, altrimenti a chi ci mette dentro i suoi figli
        private static RectTransform BuildFloating(Transform parent, string name, TMP_FontAsset font, float width, float fontSize)
        {
            var floating = HudTools.Node(parent, name, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            floating.sizeDelta = new Vector2(width, 40f);
            var layout = floating.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 16, 16);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = floating.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var box = HudTools.Node(floating, "Box", typeof(Image), typeof(LayoutElement));
            box.GetComponent<LayoutElement>().ignoreLayout = true;
            HudTools.Stretch(box, Vector2.zero, Vector2.zero);
            box.GetComponent<Image>().raycastTarget = false;
            var shade = HudTools.Node(floating, "Shade", typeof(Image), typeof(LayoutElement));
            shade.SetSiblingIndex(box.GetSiblingIndex() + 1);
            shade.GetComponent<LayoutElement>().ignoreLayout = true;
            HudTools.Stretch(shade, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            shade.GetComponent<Image>().color = new Color(0.02f, 0.018f, 0.015f, 0.8f);
            shade.GetComponent<Image>().raycastTarget = false;

            if (fontSize > 0f)
            {
                var text = HudTools.Text(floating, "Text", font, fontSize, Color.white, TextAlignmentOptions.TopLeft, 40f);
                text.richText = true;
                text.GetComponent<LayoutElement>().preferredHeight = -1f;
            }

            return floating;
        }

        private static Button MakeButton(Transform parent, string name, TMP_FontAsset font, float width, float height, float fontSize)
        {
            var node = HudTools.Node(parent, name, typeof(Image), typeof(Button), typeof(LayoutElement));
            node.sizeDelta = new Vector2(width, height);
            var image = node.GetComponent<Image>();
            image.color = Color.white;
            node.GetComponent<Button>().targetGraphic = image;
            node.GetComponent<LayoutElement>().preferredHeight = height;
            var label = HudTools.Text(node, "Label", font, fontSize, Color.white, TextAlignmentOptions.Center, height);
            HudTools.Stretch(label.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            label.GetComponent<LayoutElement>().ignoreLayout = true;
            return node.GetComponent<Button>();
        }

        // l'avviso accanto alla sfera del mana: una copia di quello degli attributi, specchiata
        private static TalentPointsButton BuildPointsButton(Transform hud, TMP_FontAsset font)
        {
            var existing = hud.Find("TalentPointsButton");
            if (existing == null)
            {
                var source = hud.Find("PointsButton");
                existing = UnityEngine.Object.Instantiate(source.gameObject, hud).transform;
                existing.name = "TalentPointsButton";
                var old = existing.GetComponent<PointsButton>();
                var oldSo = new SerializedObject(old);
                var body = oldSo.FindProperty("_body").objectReferenceValue;
                var button = oldSo.FindProperty("_button").objectReferenceValue;
                var label = oldSo.FindProperty("_label").objectReferenceValue;
                UnityEngine.Object.DestroyImmediate(old);
                var added = existing.gameObject.AddComponent<TalentPointsButton>();
                var so = new SerializedObject(added);
                so.FindProperty("_body").objectReferenceValue = body;
                so.FindProperty("_button").objectReferenceValue = button;
                so.FindProperty("_label").objectReferenceValue = label;
                so.ApplyModifiedPropertiesWithoutUndo();

                // specchiato attorno al centro dello schermo, e più largo per "Scegli la specializzazione"
                var rect = (RectTransform)existing;
                rect.anchorMin = new Vector2(1f - rect.anchorMin.x, rect.anchorMin.y);
                rect.anchorMax = new Vector2(1f - rect.anchorMax.x, rect.anchorMax.y);
                rect.pivot = new Vector2(1f - rect.pivot.x, rect.pivot.y);
                rect.anchoredPosition = new Vector2(-rect.anchoredPosition.x, rect.anchoredPosition.y);
                var bodyRect = (RectTransform)((GameObject)body).transform;
                bodyRect.sizeDelta = new Vector2(Mathf.Max(bodyRect.sizeDelta.x, 250f), bodyRect.sizeDelta.y);
                var text = (TMP_Text)label;
                text.enableAutoSizing = true;
                text.fontSizeMax = text.fontSize;
                text.fontSizeMin = 12f;
            }

            // accanto a quello degli attributi, sotto la morte e la dissolvenza che coprono tutto l'HUD
            var points = hud.Find("PointsButton");
            existing.SetSiblingIndex(points.GetSiblingIndex() + (existing.GetSiblingIndex() > points.GetSiblingIndex() ? 1 : 0));

            StripLocalized(existing.gameObject);
            return existing.GetComponent<TalentPointsButton>();
        }

        // la voce dell'altare "Riassegna le abilità": una copia di "Rifai il mondo" sotto di lei, e Chiudi più giù
        private static void BuildAltarEntry(TMP_FontAsset font)
        {
            var panel = UnityEngine.Object.FindFirstObjectByType<DifficultyPanel>(FindObjectsInactive.Include);
            var so = new SerializedObject(panel);
            var newWorld = (Button)so.FindProperty("_newWorld").objectReferenceValue;
            var source = (RectTransform)newWorld.transform;

            // la finestra ha un passo in più di altezza: il padre dei pulsanti, con le righe dal centro in giù
            var window = (RectTransform)source.parent;
            window.sizeDelta = new Vector2(window.sizeDelta.x, Mathf.Max(window.sizeDelta.y, 860f + AltarStep));

            if (so.FindProperty("_resetTalents").objectReferenceValue is Button already)
            {
                StripLocalized(already.gameObject);
                return;
            }

            var close = (Button)so.FindProperty("_close").objectReferenceValue;
            var copy = UnityEngine.Object.Instantiate(newWorld.gameObject, source.parent);
            copy.name = "ResetTalents";
            StripLocalized(copy);
            ((RectTransform)copy.transform).anchoredPosition = source.anchoredPosition + Vector2.down * AltarStep;
            ((RectTransform)close.transform).anchoredPosition += Vector2.down * AltarStep;
            so.FindProperty("_resetTalents").objectReferenceValue = copy.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // una copia di un pulsante porta con sé il LocalizedText del suo testo, che riscriverebbe quello nuovo
        private static void StripLocalized(GameObject copy)
        {
            foreach (var localized in copy.GetComponentsInChildren<LocalizedText>(true))
            {
                UnityEngine.Object.DestroyImmediate(localized);
            }
        }

        // la placca (o l'anello) di una forma: bianca con il bordo morbido, si tinge a runtime
        private static Sprite SaveShape(string shape, bool ring)
        {
            const int size = 128;
            const float half = size * 0.5f;
            const float radius = 60f;
            const float thickness = 7f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - half;
                    float py = y + 0.5f - half;
                    float d = shape switch
                    {
                        "round" => Mathf.Sqrt(px * px + py * py) - radius,
                        "square" => RoundBox(px, py, 54f, 16f),
                        _ => Octagon(px, py, radius),
                    };
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (ring)
                    {
                        alpha *= Mathf.Clamp01(0.5f + d + thickness);
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            return SaveTexture($"talent_{shape}_{(ring ? "ring" : "plate")}", size, size, pixels);
        }

        private static float RoundBox(float x, float y, float half, float corner)
        {
            float qx = Mathf.Abs(x) - (half - corner);
            float qy = Mathf.Abs(y) - (half - corner);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - corner;
        }

        // la distanza dal bordo di un ottagono regolare con l'apotema dato
        private static float Octagon(float x, float y, float apothem)
        {
            float best = float.MinValue;
            for (int k = 0; k < 8; k++)
            {
                float angle = (k * 45f + 22.5f) * Mathf.Deg2Rad;
                best = Mathf.Max(best, x * Mathf.Cos(angle) + y * Mathf.Sin(angle));
            }

            return best - apothem;
        }

        // il tratteggio delle linee dei punti: 8 pixel pieni e 8 vuoti, ripetuto
        private static Sprite SaveDash()
        {
            const int width = 16;
            const int height = 4;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(x < 8 ? 255 : 0));
                }
            }

            return SaveTexture("talent_dash", width, height, pixels, FilterMode.Point);
        }

        private static Sprite SaveTexture(string name, int width, int height, Color32[] pixels, FilterMode filter = FilterMode.Bilinear)
        {
            Directory.CreateDirectory(ArtFolder);
            string path = $"{ArtFolder}/{name}.png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = filter;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
