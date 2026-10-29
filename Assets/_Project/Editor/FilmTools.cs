using System.Linq;
using DarkDescent.Core;
using DarkDescent.Films;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Gli attrezzi dei filmati della M19 (D2). Mettono il lettore nella scena <c>Core</c>, con il
    /// carattere delle didascalie, e costruiscono dal codice il set di ogni filmato: la Timeline dalle
    /// inquadrature di <see cref="FilmLibrary"/>, il prefab con il regista e il set, legato al composition
    /// root. Si rifanno con un comando, anche in batchmode:
    /// <c>-executeMethod DarkDescent.Editor.FilmTools.InstallPlayer</c> e
    /// <c>-executeMethod DarkDescent.Editor.FilmTools.BuildOpening</c>.
    /// </summary>
    public static class FilmTools
    {
        private const string CoreScene = "Assets/_Project/Scenes/Core.unity";
        private const string FontPath = "Assets/_Project/Art/Fonts/Cinzel SDF.asset";
        private const string PlayerName = "Films";
        private const string TimelineFolder = "Assets/_Project/Films/";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Films/";
        private const string Prefabs = "Assets/_Project/Prefabs/";
        private const string Models = "Assets/_Project/Art/Models/KayKit/";
        private const string Materials = "Assets/_Project/Art/Materials/";
        private const string Animations = "Assets/_Project/Art/Animations/KayKit/";

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

        /// <summary>
        /// L'apertura (D7): la Timeline, le candele dell'altare, il prefab del set con le comparse e le
        /// animazioni, e il legame con il composition root.
        /// </summary>
        [MenuItem("DarkDescent/Filmati/Costruisci l'apertura")]
        public static void BuildOpening()
        {
            var script = FilmLibrary.Opening;
            var problems = script.Problems();
            if (problems.Count > 0)
            {
                Debug.LogError("[DarkDescent] filmati: l'apertura non si monta\n" + string.Join("\n", problems));
                return;
            }

            Folder("Assets/_Project", "Films");
            Folder("Assets/_Project/Prefabs", "Films");
            var timeline = SaveTimeline(script);
            var candle = SaveModel("Candle", Models + "Graveyard/candle_thin.fbx", "M_Graveyard");
            var candleLit = SaveModel("CandleLit", Models + "Dungeon/candle_thin_lit.fbx", "M_Dungeon");

            var root = new GameObject(script.Id);
            var director = root.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            var set = root.AddComponent<OpeningSet>();
            var so = new SerializedObject(set);
            var skeleton = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Skeleton.prefab");
            var mage = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "SkeletonMage.prefab");
            SetArray(so.FindProperty("_skeletons"), skeleton, mage, skeleton);
            so.FindProperty("_lantern").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Village/HandLantern.prefab");
            so.FindProperty("_graveDug").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Village/GraveDug.prefab");
            so.FindProperty("_candle").objectReferenceValue = candle;
            so.FindProperty("_candleLit").objectReferenceValue = candleLit;
            so.FindProperty("_idle").objectReferenceValue = Clip("General", "Idle_A");
            so.FindProperty("_walk").objectReferenceValue = Clip("MovementBasic", "Walking_A");
            so.FindProperty("_run").objectReferenceValue = Clip("MovementBasic", "Running_A");
            so.FindProperty("_hold").objectReferenceValue = Clip("Tools", "Holding_A");
            so.FindProperty("_interact").objectReferenceValue = Clip("General", "Interact");
            so.FindProperty("_wave").objectReferenceValue = Clip("Simulation", "Waving");
            so.FindProperty("_awaken").objectReferenceValue = Clip("Special", "Skeletons_Awaken_Floor");
            so.FindProperty("_skeletonWalk").objectReferenceValue = Clip("Special", "Skeletons_Walking");
            so.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + script.Id + ".prefab");
            Object.DestroyImmediate(root);

            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var link = new SerializedObject(Object.FindFirstObjectByType<CompositionRoot>());
            link.FindProperty("_opening").objectReferenceValue = prefab.GetComponent<OpeningSet>();
            link.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DarkDescent] filmati: {script.Id}, {script.Shots.Count} inquadrature in {script.Duration:0.#} s");
        }

        // la Timeline del filmato, rifatta da capo: la traccia del set con una clip lunga quanto lui
        private static TimelineAsset SaveTimeline(FilmScript script)
        {
            string path = TimelineFolder + script.Id + ".playable";
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timeline, path);
            }

            foreach (var track in timeline.GetRootTracks().ToList())
            {
                timeline.DeleteTrack(track);
            }

            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = script.Duration;
            var setTrack = timeline.CreateTrack<FilmSetTrack>(null, "Set");
            var clip = setTrack.CreateClip<FilmSetClip>();
            clip.start = 0d;
            clip.duration = script.Duration;
            clip.displayName = script.Id;
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            return timeline;
        }

        // un modello KayKit con il materiale del suo atlante: i modelli si importano senza materiali
        private static GameObject SaveModel(string name, string model, string material)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model));
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = name;
            var shared = AssetDatabase.LoadAssetAtPath<Material>(Materials + material + ".mat");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = shared;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static AnimationClip Clip(string file, string name)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(Animations + "Rig_Medium_" + file + ".fbx").OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == name);
            if (clip == null)
            {
                Debug.LogError($"[DarkDescent] filmati: manca la clip {name} in {file}");
            }

            return clip;
        }

        private static void SetArray(SerializedProperty property, params Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void Folder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
