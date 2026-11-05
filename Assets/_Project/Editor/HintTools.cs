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
    /// I suggerimenti del primo gioco (D11–D13 della M23) nella scena <c>Core</c>: il riquadro in basso al centro
    /// sopra la cintura (la posizione A approvata il 4 nov), il <see cref="HintDirector"/> collegato al
    /// <see cref="CompositionRoot"/> e, nelle opzioni, la casella «Mostra i suggerimenti» con il pulsante «Rivedi i
    /// suggerimenti». Si rifà senza doppioni; in batchmode: <c>-executeMethod DarkDescent.Editor.HintTools.InstallHints</c>.
    /// </summary>
    public static class HintTools
    {
        private const string CoreScene = "Assets/_Project/Scenes/Core.unity";
        private const string BoxName = "HintBox";
        private const string ToggleName = "Hints";
        private const string ReviewName = "HintsReview";
        private const string DirectorName = "HintDirector";

        private static readonly Color Gold = new Color(0.9f, 0.77f, 0.45f, 1f);
        private static readonly Color Border = new Color(0.42f, 0.35f, 0.2f, 1f);
        private static readonly Color Paper = new Color(0.13f, 0.11f, 0.09f, 0.96f);
        private static readonly Color Ink = new Color(0.91f, 0.89f, 0.83f, 1f);

        [MenuItem("DarkDescent/UI/Metti i suggerimenti")]
        public static void InstallHints()
        {
            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var hud = HudTools.FindHud(scene);

            var enemyBar = Object.FindFirstObjectByType<EnemyBar>(FindObjectsInactive.Include);
            var font = ((TMP_Text)new SerializedObject(enemyBar).FindProperty("_name").objectReferenceValue).font;

            var box = BuildBox(hud, font);
            var director = Object.FindFirstObjectByType<HintDirector>(FindObjectsInactive.Include);
            if (director == null)
            {
                director = new GameObject(DirectorName).AddComponent<HintDirector>();
            }

            var directorSo = new SerializedObject(director);
            directorSo.FindProperty("_box").objectReferenceValue = box;
            directorSo.ApplyModifiedPropertiesWithoutUndo();

            var root = Object.FindFirstObjectByType<CompositionRoot>(FindObjectsInactive.Include);
            var rootSo = new SerializedObject(root);
            rootSo.FindProperty("_hints").objectReferenceValue = director;
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            InstallOptions();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("HintTools: suggerimenti installati nella scena Core.");
        }

        // il riquadro: bordo, fondo, titolo in oro, testo, e la barra sottile dei secondi; nessun elemento riceve raggi
        private static HintBox BuildBox(Transform hud, TMP_FontAsset font)
        {
            var old = hud.Find(BoxName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var root = HudTools.Node(hud, BoxName);
            // in basso al centro, sopra la cintura: 640 × 132, con il fondo a 196 dal bordo dello schermo
            HudTools.Place(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 196f), new Vector2(640f, 132f));

            // sotto la schermata di morte, la dissolvenza e il titolo del livello: la morte copre tutto l'HUD
            var death = hud.Find("DeathScreen");
            if (death != null)
            {
                root.SetSiblingIndex(death.GetSiblingIndex());
            }

            var body = HudTools.Node(root, "Body", typeof(Image));
            HudTools.Stretch(body, Vector2.zero, Vector2.zero);
            var edge = body.GetComponent<Image>();
            edge.color = Border;
            edge.raycastTarget = false;

            var paper = HudTools.Node(body, "Paper", typeof(Image));
            HudTools.Stretch(paper, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            var paperImage = paper.GetComponent<Image>();
            paperImage.color = Paper;
            paperImage.raycastTarget = false;

            var title = HudTools.Text(body, "Title", font, 20f, Gold, TextAlignmentOptions.TopLeft, 28f);
            Strip(title.gameObject);
            HudTools.Place((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -12f), new Vector2(600f, 28f));
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.characterSpacing = 2f;

            var text = HudTools.Text(body, "Text", font, 22f, Ink, TextAlignmentOptions.TopLeft, 66f);
            Strip(text.gameObject);
            HudTools.Place((RectTransform)text.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -44f), new Vector2(600f, 70f));

            var track = HudTools.Node(body, "Track", typeof(Image));
            HudTools.Place(track, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 7f), new Vector2(620f, 3f));
            var trackImage = track.GetComponent<Image>();
            trackImage.color = new Color(0f, 0f, 0f, 0.4f);
            trackImage.raycastTarget = false;

            var timer = HudTools.Node(track, "Timer", typeof(Image));
            HudTools.Stretch(timer, Vector2.zero, Vector2.zero);
            var timerImage = timer.GetComponent<Image>();
            timerImage.color = new Color(0.61f, 0.52f, 0.27f, 1f);
            timerImage.raycastTarget = false;

            var view = root.gameObject.AddComponent<HintBox>();
            var so = new SerializedObject(view);
            so.FindProperty("_body").objectReferenceValue = body.gameObject;
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_text").objectReferenceValue = text;
            so.FindProperty("_timer").objectReferenceValue = timer;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // niente LayoutElement: il riquadro non è in un layout
        private static void Strip(GameObject go)
        {
            var layout = go.GetComponent<LayoutElement>();
            if (layout != null)
            {
                Object.DestroyImmediate(layout);
            }
        }

        // nelle opzioni: la casella e il pulsante sotto le barre, e il riquadro più alto per farceli stare
        private static void InstallOptions()
        {
            var options = Object.FindFirstObjectByType<OptionsPanel>(FindObjectsInactive.Include);
            var optionsSo = new SerializedObject(options);
            var bars = (Toggle)optionsSo.FindProperty("_enemyBars").objectReferenceValue;
            var parent = bars.transform.parent;
            var box = (RectTransform)parent;

            // il riquadro passa da 800 a 920 e le righe salgono di 45, così i margini restano pari; da 900 (la prima prova) basta il resto
            float lift = Mathf.Approximately(box.sizeDelta.y, 800f) ? 45f : Mathf.Approximately(box.sizeDelta.y, 900f) ? 10f : 0f;
            if (lift > 0f)
            {
                box.sizeDelta = new Vector2(box.sizeDelta.x, 920f);
                foreach (RectTransform child in parent)
                {
                    if (child.anchorMin == child.anchorMax)
                    {
                        child.anchoredPosition += new Vector2(0f, lift);
                    }
                }
            }

            var barsRect = (RectTransform)bars.transform;
            foreach (string name in new[] { ToggleName, ReviewName })
            {
                var old = parent.Find(name);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }
            }

            var toggleCopy = Object.Instantiate(bars.gameObject, parent);
            toggleCopy.name = ToggleName;
            ((RectTransform)toggleCopy.transform).anchoredPosition = barsRect.anchoredPosition + new Vector2(0f, -44f);
            var toggle = toggleCopy.GetComponent<Toggle>();
            toggle.SetIsOnWithoutNotify(true);
            SetKey(toggleCopy, "options.hints");

            var back = parent.Find("Back");
            var reviewCopy = Object.Instantiate(back.gameObject, parent);
            reviewCopy.name = ReviewName;
            var reviewRect = (RectTransform)reviewCopy.transform;
            reviewRect.anchoredPosition = barsRect.anchoredPosition + new Vector2(0f, -90f);
            reviewRect.sizeDelta = new Vector2(360f, 42f);
            SetKey(reviewCopy, "options.hints_review");
            foreach (var label in reviewCopy.GetComponentsInChildren<TMP_Text>(true))
            {
                label.fontSize = Mathf.Min(label.fontSize, 22f);
            }

            ((RectTransform)back).anchoredPosition = new Vector2(((RectTransform)back).anchoredPosition.x, barsRect.anchoredPosition.y - 145f);

            optionsSo.FindProperty("_hintsToggle").objectReferenceValue = toggle;
            optionsSo.FindProperty("_hintsReview").objectReferenceValue = reviewCopy.GetComponent<Button>();
            optionsSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetKey(GameObject go, string key)
        {
            var label = go.GetComponentInChildren<LocalizedText>(true);
            var so = new SerializedObject(label);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
            label.GetComponent<TMP_Text>().text = key;
        }
    }
}
