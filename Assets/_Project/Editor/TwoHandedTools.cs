using System;
using DarkDescent.Characters;
using DarkDescent.Items;
using DarkDescent.Magic;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Lo spadone e l'ascia a due mani (D5 della M20): i due asset in <c>Data/Items</c> con i modelli di
    /// Adventurers, il database, le icone, le tabelle del loot e del mercante, e il tempo del colpo del
    /// gesto a due mani sul cavaliere. Si rilancia: gli asset che ci sono già si aggiornano, e le tabelle
    /// non prendono una voce due volte.
    /// </summary>
    public static class TwoHandedTools
    {
        private const string ItemsFolder = "Assets/_Project/Data/Items";
        private const string LootFolder = "Assets/_Project/Data/Loot";
        private const string ModelsFolder = "Assets/_Project/Art/Models/KayKit";

        // il momento del colpo nella clip 2H_Attack_Chop, che dura 1,633 s
        private const float ClipHitTime = 0.9f;

        // le tabelle che li hanno: i forzieri e i bruti, il re e i banchi del borgo (gli altri nemici no)
        private static readonly string[] Tables = { "ChestLoot", "GoldChestLoot", "BruteLoot", "BossLoot", "MerchantTable" };

        // nome dell'asset, chiave, modello, celle, valore, danno, Forza, portata, intervallo, colpo, tipo, copia della posa in mano
        private static readonly Row[] Rows =
        {
            new Row("Greatsword", "item.greatsword", "sword_2handed", new Vector2Int(1, 4), 160, 14, 22, 40, 0.75f, 1.5f, 0.9f, WeaponKind.Sword, "ShortSword"),
            new Row("GreatAxe", "item.great_axe", "axe_2handed", new Vector2Int(2, 4), 210, 18, 28, 50, 0.7f, 1.75f, 1.05f, WeaponKind.Axe, "Axe"),
        };

        [MenuItem("DarkDescent/Oggetti/Armi a due mani")]
        public static void Build()
        {
            var made = new ItemDefinition[Rows.Length];
            for (int i = 0; i < Rows.Length; i++)
            {
                made[i] = Save(Rows[i]);
            }

            AssetDatabase.SaveAssets();
            ItemTools.UpdateDatabase();
            foreach (var item in made)
            {
                ItemTools.RenderIcon(item);
            }

            foreach (string table in Tables)
            {
                AddToTable(table, made);
            }

            InstallClipTime();
            AssetDatabase.SaveAssets();
            Debug.Log($"[TwoHandedTools] {made.Length} armi a due mani, in {Tables.Length} tabelle");
        }

        private static ItemDefinition Save(Row row)
        {
            string path = $"{ItemsFolder}/{row.Asset}.asset";
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(weapon, path);
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{row.Model}.fbx");
            var like = AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{ItemsFolder}/{row.HeldLike}.asset");
            var held = new SerializedObject(like);
            var so = new SerializedObject(weapon);
            var id = so.FindProperty("_id");
            if (string.IsNullOrEmpty(id.stringValue))
            {
                id.stringValue = Guid.NewGuid().ToString("N");
            }

            so.FindProperty("_nameKey").stringValue = row.Key;
            so.FindProperty("_model").objectReferenceValue = model;
            so.FindProperty("_size").vector2IntValue = row.Size;
            so.FindProperty("_value").intValue = row.Value;
            so.FindProperty("_heldPosition").vector3Value = held.FindProperty("_heldPosition").vector3Value;
            so.FindProperty("_heldRotation").vector3Value = held.FindProperty("_heldRotation").vector3Value;
            so.FindProperty("_iconRotation").vector3Value = held.FindProperty("_iconRotation").vector3Value;
            so.FindProperty("_minDamage").intValue = row.Min;
            so.FindProperty("_maxDamage").intValue = row.Max;
            so.FindProperty("_kind").intValue = (int)row.Kind;
            so.FindProperty("_requiredStrength").intValue = row.Strength;
            so.FindProperty("_range").floatValue = row.Range;
            so.FindProperty("_rangeTolerance").floatValue = 0.5f;
            so.FindProperty("_attackInterval").floatValue = row.Interval;
            so.FindProperty("_hitDelay").floatValue = row.HitDelay;
            so.FindProperty("_arc").floatValue = 360f;
            so.FindProperty("_twoHanded").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        internal static void AddToTable(string name, ItemDefinition[] items)
        {
            string path = $"{LootFolder}/{name}.asset";
            var table = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            var so = new SerializedObject(table);
            var list = so.FindProperty(name == "MerchantTable" ? "_stock" : "_entries");
            foreach (var item in items)
            {
                bool there = false;
                for (int i = 0; i < list.arraySize; i++)
                {
                    there |= list.GetArrayElementAtIndex(i).FindPropertyRelative("_item").objectReferenceValue == item;
                }

                if (there)
                {
                    continue;
                }

                list.arraySize++;
                var entry = list.GetArrayElementAtIndex(list.arraySize - 1);
                entry.FindPropertyRelative("_item").objectReferenceValue = item;
                entry.FindPropertyRelative("_weight").intValue = 1;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
        }

        // il tempo del colpo nella clip a due mani, sul driver del modello del cavaliere
        private static void InstallClipTime()
        {
            var player = PrefabUtility.LoadPrefabContents(TalentTools.PlayerPrefab);
            try
            {
                var driver = player.GetComponentInChildren<CharacterAnimatorDriver>(true);
                var so = new SerializedObject(driver);
                so.FindProperty("_twoHandedClipHitTime").floatValue = ClipHitTime;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, TalentTools.PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        private sealed class Row
        {
            public Row(string asset, string key, string model, Vector2Int size, int value, int min, int max, int strength,
                float range, float interval, float hitDelay, WeaponKind kind, string heldLike)
            {
                Asset = asset;
                Key = key;
                Model = model;
                Size = size;
                Value = value;
                Min = min;
                Max = max;
                Strength = strength;
                Range = range;
                Interval = interval;
                HitDelay = hitDelay;
                Kind = kind;
                HeldLike = heldLike;
            }

            public string Asset { get; }
            public string Key { get; }
            public string Model { get; }
            public Vector2Int Size { get; }
            public int Value { get; }
            public int Min { get; }
            public int Max { get; }
            public int Strength { get; }
            public float Range { get; }
            public float Interval { get; }
            public float HitDelay { get; }
            public WeaponKind Kind { get; }
            public string HeldLike { get; }
        }
    }
}
