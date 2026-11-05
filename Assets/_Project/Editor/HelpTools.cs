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
    /// Il pannello dei comandi (D14 della M23) nella scena <c>Core</c>: il <see cref="HelpPanel"/> nell'HUD, fatto con la
    /// stessa cornice delle opzioni (una copia, ripulita), due colonne per i gruppi e un pulsante Chiudi, e il pulsante
    /// <i>Comandi</i> nel menu di pausa, tra Opzioni e Salva ed esci. Si rifà senza doppioni; in batchmode:
    /// <c>-executeMethod DarkDescent.Editor.HelpTools.InstallHelp</c>.
    /// </summary>
    public static class HelpTools
    {
        private const string CoreScene = "Assets/_Project/Scenes/Core.unity";
        private const string PanelName = "HelpPanel";
        private const string PauseButtonName = "Help";

        [MenuItem("DarkDescent/UI/Metti il pannello dei comandi")]
        public static void InstallHelp()
        {
            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var hud = HudTools.FindHud(scene);

            var enemyBar = Object.FindFirstObjectByType<EnemyBar>(FindObjectsInactive.Include);
            var font = ((TMP_Text)new SerializedObject(enemyBar).FindProperty("_name").objectReferenceValue).font;

            var panel = BuildPanel(hud, font);
            var root = Object.FindFirstObjectByType<CompositionRoot>(FindObjectsInactive.Include);
            var rootSo = new SerializedObject(root);
            rootSo.FindProperty("_helpPanel").objectReferenceValue = panel;
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            InstallPauseButton(panel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("HelpTools: pannello dei comandi installato nella scena Core.");
        }

        private static HelpPanel BuildPanel(Transform hud, TMP_FontAsset font)
        {
            var old = hud.Find(PanelName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            // la cornice delle opzioni, copiata: lo sfondo scuro a tutto schermo e il riquadro con la sua pelle
            var options = Object.FindFirstObjectByType<OptionsPanel>(FindObjectsInactive.Include);
            var optionsRoot = (GameObject)new SerializedObject(options).FindProperty("_root").objectReferenceValue;

            var holder = HudTools.Node(hud, PanelName);
            HudTools.Stretch(holder, Vector2.zero, Vector2.zero);
            // sotto la schermata di morte, la dissolvenza e il titolo del livello, sopra il resto dell'HUD
            var death = hud.Find("DeathScreen");
            if (death != null)
            {
                holder.SetSiblingIndex(death.GetSiblingIndex());
            }

            var copy = Object.Instantiate(optionsRoot, holder);
            copy.name = "Root";
            copy.SetActive(true);
            var copyRect = (RectTransform)copy.transform;
            HudTools.Stretch(copyRect, Vector2.zero, Vector2.zero);

            var box = (RectTransform)copy.transform.Find("Box");
            // tengono la pelle, il titolo e Indietro (che diventa Chiudi); il resto va via
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in box)
            {
                children.Add(child);
            }

            foreach (var child in children)
            {
                if (child.name != "SkinFrame" && child.name != "Title" && child.name != "Back")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            box.sizeDelta = new Vector2(1340f, 780f);
            var title = (RectTransform)box.Find("Title");
            title.anchoredPosition = new Vector2(0f, 332f);
            title.sizeDelta = new Vector2(900f, 56f);
            var titleText = title.GetComponent<TMP_Text>();
            DestroyKey(title.gameObject);

            var close = (RectTransform)box.Find("Back");
            close.name = "Close";
            close.anchoredPosition = new Vector2(0f, -342f);
            close.sizeDelta = new Vector2(320f, 50f);
            var closeText = close.GetComponentInChildren<TMP_Text>(true);
            DestroyKey(closeText.gameObject);

            var left = Column(box, "Left", new Vector2(-610f, 270f));
            var right = Column(box, "Right", new Vector2(30f, 270f));

            var view = holder.gameObject.AddComponent<HelpPanel>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = copy;
            so.FindProperty("_title").objectReferenceValue = titleText;
            so.FindProperty("_left").objectReferenceValue = left;
            so.FindProperty("_right").objectReferenceValue = right;
            so.FindProperty("_close").objectReferenceValue = close.GetComponent<Button>();
            so.FindProperty("_closeLabel").objectReferenceValue = closeText;
            so.FindProperty("_font").objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // una colonna di righe, dall'alto in basso: il gruppo e le sue righe le mette il pannello
        private static RectTransform Column(RectTransform box, string name, Vector2 topLeft)
        {
            var node = HudTools.Node(box, name, typeof(VerticalLayoutGroup));
            HudTools.Place(node, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), topLeft, new Vector2(580f, 600f));
            var layout = node.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            return node;
        }

        // la scritta la riscrive il pannello (con i tasti veri): niente LocalizedText che la riscriva sopra
        private static void DestroyKey(GameObject go)
        {
            var key = go.GetComponent<LocalizedText>();
            if (key != null)
            {
                Object.DestroyImmediate(key);
            }
        }

        // «Comandi» tra Opzioni e Salva ed esci; il riquadro cresce di una riga
        private static void InstallPauseButton(HelpPanel panel)
        {
            var pause = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            var pauseSo = new SerializedObject(pause);
            var options = (Button)pauseSo.FindProperty("_options").objectReferenceValue;
            var quit = (Button)pauseSo.FindProperty("_quit").objectReferenceValue;
            var box = (RectTransform)options.transform.parent;

            if (Mathf.Approximately(box.sizeDelta.y, 400f))
            {
                box.sizeDelta = new Vector2(box.sizeDelta.x, 466f);
                foreach (RectTransform child in box)
                {
                    if (child.anchorMin == child.anchorMax)
                    {
                        child.anchoredPosition += new Vector2(0f, 33f);
                    }
                }
            }

            var old = box.Find(PauseButtonName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var optionsRect = (RectTransform)options.transform;
            var copy = Object.Instantiate(options.gameObject, box);
            copy.name = PauseButtonName;
            var copyRect = (RectTransform)copy.transform;
            copyRect.anchoredPosition = optionsRect.anchoredPosition + new Vector2(0f, -66f);
            var key = copy.GetComponentInChildren<LocalizedText>(true);
            var keySo = new SerializedObject(key);
            keySo.FindProperty("_key").stringValue = "pause.help";
            keySo.ApplyModifiedPropertiesWithoutUndo();
            key.GetComponent<TMP_Text>().text = "pause.help";

            var quitRect = (RectTransform)quit.transform;
            quitRect.anchoredPosition = copyRect.anchoredPosition + new Vector2(0f, -66f);

            pauseSo.FindProperty("_help").objectReferenceValue = copy.GetComponent<Button>();
            pauseSo.FindProperty("_helpPanel").objectReferenceValue = panel;
            pauseSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
