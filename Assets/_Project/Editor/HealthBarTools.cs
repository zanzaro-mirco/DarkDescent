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
    /// Le barre sopra i nemici feriti (D9 della M23) nella scena <c>Core</c>: l'oggetto <see cref="EnemyHealthBars"/>
    /// con il font della barra in alto, il collegamento al <see cref="CompositionRoot"/> e, nelle opzioni, la casella
    /// «Barre della vita sui nemici feriti» sotto i numeri delle sfere. Si rifà senza doppioni; in batchmode:
    /// <c>-executeMethod DarkDescent.Editor.HealthBarTools.InstallBars</c>.
    /// </summary>
    public static class HealthBarTools
    {
        private const string CoreScene = "Assets/_Project/Scenes/Core.unity";
        private const string ToggleName = "EnemyBars";
        private const string ObjectName = "EnemyHealthBars";

        [MenuItem("DarkDescent/UI/Metti le barre sui nemici")]
        public static void InstallBars()
        {
            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);

            InstallObject();
            InstallToggle();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("HealthBarTools: barre sui nemici installate nella scena Core.");
        }

        private static void InstallObject()
        {
            var bars = Object.FindFirstObjectByType<EnemyHealthBars>(FindObjectsInactive.Include);
            if (bars == null)
            {
                var go = new GameObject(ObjectName);
                bars = go.AddComponent<EnemyHealthBars>();
            }

            // il font del nome nella barra in alto: le stesse lettere in tutto il gioco
            var enemyBar = Object.FindFirstObjectByType<EnemyBar>(FindObjectsInactive.Include);
            var nameText = (TMP_Text)new SerializedObject(enemyBar).FindProperty("_name").objectReferenceValue;
            var barsSo = new SerializedObject(bars);
            barsSo.FindProperty("_font").objectReferenceValue = nameText.font;
            barsSo.ApplyModifiedPropertiesWithoutUndo();

            var root = Object.FindFirstObjectByType<CompositionRoot>(FindObjectsInactive.Include);
            var rootSo = new SerializedObject(root);
            rootSo.FindProperty("_healthBars").objectReferenceValue = bars;
            rootSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InstallToggle()
        {
            var options = Object.FindFirstObjectByType<OptionsPanel>(FindObjectsInactive.Include);
            var optionsSo = new SerializedObject(options);
            var source = (Toggle)optionsSo.FindProperty("_orbNumbers").objectReferenceValue;
            var parent = source.transform.parent;

            var old = parent.Find(ToggleName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            // una copia dei numeri delle sfere, una riga sotto, con la sua chiave
            var copy = Object.Instantiate(source.gameObject, parent);
            copy.name = ToggleName;
            var rect = (RectTransform)copy.transform;
            var sourceRect = (RectTransform)source.transform;
            rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -44f);
            var toggle = copy.GetComponent<Toggle>();
            toggle.SetIsOnWithoutNotify(false);

            var label = copy.GetComponentInChildren<LocalizedText>(true);
            var labelSo = new SerializedObject(label);
            labelSo.FindProperty("_key").stringValue = "options.enemy_bars";
            labelSo.ApplyModifiedPropertiesWithoutUndo();
            label.GetComponent<TMP_Text>().text = "options.enemy_bars";

            // Indietro scende di una riga, dentro il riquadro (620 × 800)
            var back = (RectTransform)parent.Find("Back");
            back.anchoredPosition = new Vector2(back.anchoredPosition.x, -355f);

            optionsSo.FindProperty("_enemyBars").objectReferenceValue = toggle;
            optionsSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
