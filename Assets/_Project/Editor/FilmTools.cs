using DarkDescent.Core;
using DarkDescent.Films;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Gli attrezzi dei filmati della M19 (D2). Per ora mette il lettore nella scena <c>Core</c>, con il
    /// carattere delle didascalie, e lo lega al composition root; si rifà con un comando, anche in
    /// batchmode: <c>-executeMethod DarkDescent.Editor.FilmTools.InstallPlayer</c>.
    /// </summary>
    public static class FilmTools
    {
        private const string CoreScene = "Assets/_Project/Scenes/Core.unity";
        private const string FontPath = "Assets/_Project/Art/Fonts/Cinzel SDF.asset";
        private const string PlayerName = "Films";

        [MenuItem("DarkDescent/Filmati/Metti il lettore in Core")]
        public static void InstallPlayer()
        {
            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var root = Object.FindFirstObjectByType<CompositionRoot>();
            var player = Object.FindFirstObjectByType<FilmPlayer>(FindObjectsInactive.Include);
            if (player == null)
            {
                var go = new GameObject(PlayerName);
                SceneManager.MoveGameObjectToScene(go, scene);
                player = go.AddComponent<FilmPlayer>();
            }

            var so = new SerializedObject(player);
            so.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            var link = new SerializedObject(root);
            link.FindProperty("_films").objectReferenceValue = player;
            link.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DarkDescent] filmati: il lettore {player.name} in {CoreScene}");
        }
    }
}
