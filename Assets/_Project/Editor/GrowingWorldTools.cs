using DarkDescent.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.Editor
{
    /// <summary>
    /// L'interruttore «Il mondo cresce con te» (D12 della M24) nel pannello dell'altare: una copia di «Rifai il mondo» tra le
    /// difficoltà e lui, con a destra della finestra il suggerimento che compare al passaggio. Idempotente: una seconda volta non fa niente.
    /// Da riga di comando: <c>-executeMethod DarkDescent.Editor.GrowingWorldTools.Build</c>.
    /// </summary>
    public static class GrowingWorldTools
    {
        private const float Step = 80f;
        private const float HintWidth = 440f;
        private const float HintHeight = 200f;

        [MenuItem("DarkDescent/UI/Interruttore del mondo che cresce")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(TalentTools.CoreScene, OpenSceneMode.Single);
            var panel = Object.FindFirstObjectByType<DifficultyPanel>(FindObjectsInactive.Include);
            var so = new SerializedObject(panel);
            if (so.FindProperty("_growingWorld").objectReferenceValue != null)
            {
                Debug.Log("[DarkDescent] l'interruttore del mondo che cresce c'è già");
                return;
            }

            var newWorld = (Button)so.FindProperty("_newWorld").objectReferenceValue;
            var reset = (Button)so.FindProperty("_resetTalents").objectReferenceValue;
            var close = (Button)so.FindProperty("_close").objectReferenceValue;
            var source = (RectTransform)newWorld.transform;
            var window = (RectTransform)source.parent;

            // la finestra cresce di un passo; Rifai il mondo, le abilità e Chiudi scendono, l'interruttore prende il posto di «Rifai il mondo»
            window.sizeDelta = new Vector2(window.sizeDelta.x, window.sizeDelta.y + Step);
            var copy = Object.Instantiate(newWorld.gameObject, window);
            copy.name = "GrowingWorld";
            foreach (var localized in copy.GetComponentsInChildren<DarkDescent.Localization.LocalizedText>(true))
            {
                Object.DestroyImmediate(localized);
            }

            ((RectTransform)copy.transform).anchoredPosition = source.anchoredPosition;
            foreach (var moved in new[] { newWorld, reset, close })
            {
                ((RectTransform)moved.transform).anchoredPosition += Vector2.down * Step;
            }

            var hint = BuildHint(window, (RectTransform)copy.transform, copy.GetComponentInChildren<TMP_Text>(true));
            var hover = copy.AddComponent<HoverShow>();
            var hoverSo = new SerializedObject(hover);
            hoverSo.FindProperty("_target").objectReferenceValue = hint.transform.parent.gameObject;
            hoverSo.ApplyModifiedPropertiesWithoutUndo();

            so.FindProperty("_growingWorld").objectReferenceValue = copy.GetComponent<Button>();
            so.FindProperty("_growingHint").objectReferenceValue = hint;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DarkDescent] interruttore del mondo che cresce aggiunto al pannello dell'altare");
        }

        // un riquadro scuro a destra della finestra, all'altezza del pulsante (sotto non c'è posto: la finestra arriva al bordo dello
        // schermo), con il testo a capo; nascosto finché il puntatore non è sull'interruttore
        private static TMP_Text BuildHint(RectTransform window, RectTransform button, TMP_Text like)
        {
            var box = new GameObject("GrowingHint", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(button, false);
            var rect = (RectTransform)box.transform;
            var windowCorners = new Vector3[4];
            var buttonCorners = new Vector3[4];
            window.GetWorldCorners(windowCorners);
            button.GetWorldCorners(buttonCorners);
            float gap = button.InverseTransformVector(windowCorners[2] - buttonCorners[2]).x + 16f;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(gap, 0f);
            rect.sizeDelta = new Vector2(HintWidth, HintHeight);
            box.GetComponent<Image>().color = new Color(0.04f, 0.035f, 0.03f, 0.92f);
            box.GetComponent<Image>().raycastTarget = false;

            var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(box.transform, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 12f);
            labelRect.offsetMax = new Vector2(-20f, -12f);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.font = like.font;
            text.fontSize = 22f;
            text.color = new Color(0.9f, 0.86f, 0.78f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            box.SetActive(false);
            return text;
        }
    }
}
