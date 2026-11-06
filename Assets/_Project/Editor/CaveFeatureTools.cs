using System;
using DarkDescent.Levels;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Pozzi e santuari nelle caverne (D10 della M25): il tileset delle caverne ottiene i sei simboli della cripta (due pozzi e quattro santuari) con gli stessi prefab,
    /// che non dipendono dal set. Si rilancia: i simboli già presenti si aggiornano e niente si duplica. In batchmode:
    /// <c>-executeMethod DarkDescent.Editor.CaveFeatureTools.Install</c>.
    /// </summary>
    public static class CaveFeatureTools
    {
        private const string Root = "Assets/_Project/Data/Levels/";

        [MenuItem("DarkDescent/Segreti/Caverne: pozzi e santuari nel tileset")]
        public static void Install()
        {
            var crypt = Load("DungeonTileset.asset");
            var cave = Load("CaveTileset.asset");
            int copied = 0;
            foreach (char symbol in DungeonPopulator.WellSymbols + DungeonPopulator.ShrineSymbols)
            {
                Copy(crypt, cave, symbol);
                copied++;
            }

            EditorUtility.SetDirty(cave);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DarkDescent] Caverne: {copied} simboli di pozzi e santuari nel tileset");
        }

        private static LevelTileset Load(string file)
        {
            var asset = AssetDatabase.LoadAssetAtPath<LevelTileset>(Root + file);
            if (asset == null)
            {
                throw new InvalidOperationException("Manca " + Root + file);
            }

            return asset;
        }

        private static void Copy(LevelTileset from, LevelTileset to, char symbol)
        {
            var source = Find(new SerializedObject(from).FindProperty("_markers"), symbol);
            if (source == null)
            {
                throw new InvalidOperationException($"Il set della cripta non ha il simbolo '{symbol}'");
            }

            var so = new SerializedObject(to);
            var markers = so.FindProperty("_markers");
            var target = Find(markers, symbol);
            if (target == null)
            {
                markers.arraySize++;
                target = markers.GetArrayElementAtIndex(markers.arraySize - 1);
                target.FindPropertyRelative("_symbol").intValue = symbol;
            }

            target.FindPropertyRelative("_prefab").objectReferenceValue = source.FindPropertyRelative("_prefab").objectReferenceValue;
            target.FindPropertyRelative("_replacesFloor").boolValue = source.FindPropertyRelative("_replacesFloor").boolValue;
            target.FindPropertyRelative("_keepsRotation").boolValue = source.FindPropertyRelative("_keepsRotation").boolValue;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Find(SerializedProperty markers, char symbol)
        {
            for (int i = 0; i < markers.arraySize; i++)
            {
                var element = markers.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("_symbol").intValue == symbol)
                {
                    return element;
                }
            }

            return null;
        }
    }
}
