using System.Linq;
using DarkDescent.Core;
using DarkDescent.Films;
using DarkDescent.Localization;
using DarkDescent.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Gli attrezzi dei filmati della M19 (D2). Mettono il lettore nella scena <c>Core</c>, con il
    /// carattere delle didascalie, e costruiscono dal codice il set di ogni filmato: la Timeline dalle
    /// inquadrature di <see cref="FilmLibrary"/>, il prefab con il regista e il set, legato al composition
    /// root. Si rifanno con un comando, anche in batchmode:
    /// <c>-executeMethod DarkDescent.Editor.FilmTools.InstallPlayer</c>,
    /// <c>-executeMethod DarkDescent.Editor.FilmTools.BuildOpening</c>,
    /// <c>-executeMethod DarkDescent.Editor.FilmTools.BuildVictory</c> e
    /// <c>-executeMethod DarkDescent.Editor.FilmTools.InstallMenu</c>, che mette il pulsante Filmati nel
    /// menu iniziale (D18).
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
        private const string Mixer = "Assets/_Project/Audio/Main.mixer";
        private const string Music = "Assets/_Project/Audio/Music/";
        private const string Sounds = "Assets/_Project/Audio/SFX/Generated/";
        // la musica della vittoria riparte da qui nel borgo se il portale è stato saltato: il pezzo del
        // portale e il viaggio
        private const float ReturnMusicFrom = 7f;

        // il menu iniziale: un pulsante alto 54 con 12 di spazio, il primo in alto a 127 con sei pulsanti
        private const float MenuButtonHeight = 54f;
        private const float MenuButtonSpacing = 12f;
        private const float MenuCenterY = 127f - (6f * MenuButtonHeight + 5f * MenuButtonSpacing + 12f) / 2f;

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
            so.FindProperty("_musicGroup").objectReferenceValue = Group("Music");
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
            so.FindProperty("_snuffSound").objectReferenceValue = Sound("candle_snuff_sa.wav");
            so.FindProperty("_earthSound").objectReferenceValue = Sound("earth_break_sa.wav");
            SetMusic(so, "film_opening.ogg", 0f, runsOut: false);
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

        /// <summary>
        /// La vittoria (D8), in tre set: il re che cade nella sala, il cavaliere che entra nel portale, il
        /// ritorno nel borgo all'alba. Per ognuno la Timeline e il prefab, legati al composition root.
        /// </summary>
        [MenuItem("DarkDescent/Filmati/Costruisci la vittoria")]
        public static void BuildVictory()
        {
            foreach (var script in new[] { FilmLibrary.VictoryFall, FilmLibrary.VictoryLeave, FilmLibrary.VictoryReturn })
            {
                var problems = script.Problems();
                if (problems.Count > 0)
                {
                    Debug.LogError($"[DarkDescent] filmati: {script.Id} non si monta\n" + string.Join("\n", problems));
                    return;
                }
            }

            Folder("Assets/_Project", "Films");
            Folder("Assets/_Project/Prefabs", "Films");
            // il re cade nel silenzio: la musica del livello tace e quella della vittoria non è ancora partita
            SaveSet<VictoryFallSet>(FilmLibrary.VictoryFall, so =>
            {
                so.FindProperty("_death").objectReferenceValue = Clip("Special", "Skeletons_Death");
                so.FindProperty("_deathPose").objectReferenceValue = Clip("Special", "Skeletons_Death_Pose");
                SetMusic(so, null, 0f, runsOut: false);
            });
            // la musica della vittoria parte al portale e va avanti nel viaggio e nel borgo
            SaveSet<VictoryLeaveSet>(FilmLibrary.VictoryLeave, so =>
            {
                so.FindProperty("_idle").objectReferenceValue = Clip("General", "Idle_A");
                so.FindProperty("_walk").objectReferenceValue = Clip("MovementBasic", "Walking_A");
                SetMusic(so, "film_victory.ogg", 0f, runsOut: true);
            });
            SaveSet<VictoryReturnSet>(FilmLibrary.VictoryReturn, so =>
            {
                so.FindProperty("_idle").objectReferenceValue = Clip("General", "Idle_A");
                so.FindProperty("_walk").objectReferenceValue = Clip("MovementBasic", "Walking_A");
                so.FindProperty("_standUp").objectReferenceValue = Clip("Simulation", "Sit_Floor_StandUp");
                so.FindProperty("_cheer").objectReferenceValue = Clip("Simulation", "Cheering");
                so.FindProperty("_cheerSound").objectReferenceValue = Sound("crowd_cheer_sa.wav");
                SetMusic(so, "film_victory.ogg", ReturnMusicFrom, runsOut: true);
            });

            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var link = new SerializedObject(Object.FindFirstObjectByType<CompositionRoot>());
            // i prefab si rileggono dopo aver aperto la scena, che scarica quello che non usa
            link.FindProperty("_victoryFall").objectReferenceValue = LoadSet<VictoryFallSet>(FilmLibrary.VictoryFall);
            link.FindProperty("_victoryLeave").objectReferenceValue = LoadSet<VictoryLeaveSet>(FilmLibrary.VictoryLeave);
            link.FindProperty("_victoryReturn").objectReferenceValue = LoadSet<VictoryReturnSet>(FilmLibrary.VictoryReturn);
            link.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[DarkDescent] filmati: la vittoria in tre set");
        }

        /// <summary>
        /// Il pulsante Filmati nel menu iniziale, sotto Riconoscimenti, e l'elenco che apre (D18): una copia
        /// del pannello dei posti, con il titolo, i due filmati e Indietro. Si rifà senza doppioni.
        /// </summary>
        [MenuItem("DarkDescent/Filmati/Metti i filmati nel menu")]
        public static void InstallMenu()
        {
            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var menu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
            var link = new SerializedObject(menu);
            var credits = (Button)link.FindProperty("_credits").objectReferenceValue;
            var slots = (GameObject)link.FindProperty("_slotsPanel").objectReferenceValue;
            var buttons = (RectTransform)credits.transform.parent;

            // il pulsante, copiato da Riconoscimenti e messo sotto di lui
            var old = buttons.Find(TextKeys.MenuFilms);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var films = Object.Instantiate(credits.gameObject, buttons);
            films.name = TextKeys.MenuFilms;
            films.transform.SetSiblingIndex(credits.transform.GetSiblingIndex() + 1);
            Label(films, TextKeys.MenuFilms, "Filmati");
            int count = buttons.childCount;
            float height = count * MenuButtonHeight + (count - 1) * MenuButtonSpacing + 12f;
            buttons.sizeDelta = new Vector2(buttons.sizeDelta.x, height);
            buttons.anchoredPosition = new Vector2(buttons.anchoredPosition.x, MenuCenterY + height / 2f);

            // l'elenco: il pannello dei posti senza i posti, con i due filmati al loro posto
            var oldPanel = slots.transform.parent.Find("Films");
            if (oldPanel != null)
            {
                Object.DestroyImmediate(oldPanel.gameObject);
            }

            var panel = Object.Instantiate(slots, slots.transform.parent);
            panel.name = "Films";
            panel.transform.SetSiblingIndex(slots.transform.GetSiblingIndex() + 1);
            foreach (var slot in panel.GetComponentsInChildren<SaveSlotView>(true))
            {
                Object.DestroyImmediate(slot.gameObject);
            }

            var back = panel.transform.Find("Back").gameObject;
            var opening = FilmButton(back, "Opening", FilmLibrary.Opening.TitleKey, "L'ultima candela", 40f);
            var victory = FilmButton(back, "Victory", FilmLibrary.VictoryFall.TitleKey, "La vittoria", -30f);
            panel.SetActive(false);

            var filmsPanel = menu.GetComponent<FilmsPanel>();
            if (filmsPanel == null)
            {
                filmsPanel = menu.gameObject.AddComponent<FilmsPanel>();
            }

            var so = new SerializedObject(filmsPanel);
            so.FindProperty("_window").objectReferenceValue = panel;
            so.FindProperty("_title").objectReferenceValue = panel.transform.Find("Title").GetComponent<TMP_Text>();
            so.FindProperty("_opening").objectReferenceValue = opening;
            so.FindProperty("_victory").objectReferenceValue = victory;
            so.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            link.FindProperty("_films").objectReferenceValue = films.GetComponent<Button>();
            link.FindProperty("_filmsPanel").objectReferenceValue = filmsPanel;
            link.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DarkDescent] filmati: il pulsante nel menu, {count} pulsanti");
        }

        // un filmato dell'elenco: una copia di Indietro, larga come i posti
        private static Button FilmButton(GameObject back, string name, string key, string text, float y)
        {
            var copy = Object.Instantiate(back, back.transform.parent);
            copy.name = name;
            copy.transform.SetSiblingIndex(back.transform.GetSiblingIndex());
            var rect = (RectTransform)copy.transform;
            rect.sizeDelta = new Vector2(520f, 58f);
            rect.anchoredPosition = new Vector2(0f, y);
            Label(copy, key, text);
            return copy.GetComponent<Button>();
        }

        // la scritta di un pulsante copiato: la sua chiave, e il testo di prima della lingua
        private static void Label(GameObject button, string key, string text)
        {
            var localized = button.GetComponentInChildren<LocalizedText>(true);
            var so = new SerializedObject(localized);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
            localized.GetComponent<TMP_Text>().text = text;
        }

        // la musica che il set dice al lettore
        private static void SetMusic(SerializedObject so, string clip, float from, bool runsOut)
        {
            so.FindProperty("_music").objectReferenceValue = clip != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(Music + clip) : null;
            so.FindProperty("_musicFrom").floatValue = from;
            so.FindProperty("_musicRunsOut").boolValue = runsOut;
            so.FindProperty("_soundGroup").objectReferenceValue = Group("SFX");
        }

        private static AudioClip Sound(string file)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Sounds + file);
            if (clip == null)
            {
                Debug.LogError($"[DarkDescent] filmati: manca il suono {file}");
            }

            return clip;
        }

        private static AudioMixerGroup Group(string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath(Mixer).OfType<AudioMixerGroup>().First(g => g.name == name);
        }

        private static T LoadSet<T>(FilmScript script) where T : FilmSet
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + script.Id + ".prefab").GetComponent<T>();
        }

        // il prefab di un set: il regista con la sua Timeline e il set, con le animazioni che gli dà configure
        private static void SaveSet<T>(FilmScript script, System.Action<SerializedObject> configure) where T : FilmSet
        {
            var root = new GameObject(script.Id);
            var director = root.AddComponent<PlayableDirector>();
            director.playableAsset = SaveTimeline(script);
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            var set = root.AddComponent<T>();
            var so = new SerializedObject(set);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + script.Id + ".prefab");
            Object.DestroyImmediate(root);
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
