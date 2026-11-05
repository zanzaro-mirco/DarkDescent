using System;
using System.IO;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Vorrak il Custode (D8–D11 della M24): i colpi, l'archetipo, il prefab (il modello del re tinto di viola, con un'aura, senza i suoi
    /// componenti di re e con il controllo del Custode), il prefab del suo tesoro e i due segni nel set della cripta. Si rilancia: gli
    /// asset che ci sono già si aggiornano, e il set non prende un segno due volte.
    /// </summary>
    public static class GuardianTools
    {
        private const string Root = "Assets/_Project";
        private const string AttacksFolder = Root + "/Data/Attacks";
        private const string EnemiesFolder = Root + "/Data/Enemies";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string MaterialsFolder = Root + "/Prefabs/GuardianMaterials";

        // la vita è otto scheletri (30 ciascuno); i colpi sono quelli del bruto per 1,3
        private const float Health = 240f;
        private static readonly Color Tint = new Color(0.62f, 0.45f, 0.85f, 1f);
        private static readonly Color AuraColor = new Color(0.75f, 0.35f, 1f);

        [MenuItem("DarkDescent/Custode/Crea o aggiorna")]
        public static void Build()
        {
            var strike = Weapon("GuardianStrike", "BoneKingStrike", 26f, 36f, 1.4f, 2.4f, 0.9f, 130f);
            var quick = Weapon("GuardianQuick", "BoneKingQuick", 10f, 16f, 1.4f, 1.5f, 0.42f, 110f);
            var archetype = Archetype();
            var prefab = GuardianPrefab(strike, quick, archetype);
            var treasure = TreasurePrefab();
            Tileset(prefab, treasure);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DarkDescent] Custode: colpi, archetipo, prefab, tesoro e segni del set pronti");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Manca " + path);
            }

            return asset;
        }

        private static T CopyOf<T>(string from, string to) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(to);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.CopyAsset(from, to))
            {
                throw new InvalidOperationException("Copia fallita: " + from + " -> " + to);
            }

            return Load<T>(to);
        }

        // un colpo come quello del re, con i numeri del Custode: il bruto per 1,3
        private static WeaponDefinition Weapon(string name, string model, float min, float max, float range, float interval, float hitDelay, float arc)
        {
            var weapon = CopyOf<WeaponDefinition>($"{AttacksFolder}/{model}.asset", $"{AttacksFolder}/{name}.asset");
            var so = new SerializedObject(weapon);
            so.FindProperty("_id").stringValue = Guid.NewGuid().ToString("N");
            so.FindProperty("_minDamage").intValue = Mathf.RoundToInt(min);
            so.FindProperty("_maxDamage").intValue = Mathf.RoundToInt(max);
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_attackInterval").floatValue = interval;
            so.FindProperty("_hitDelay").floatValue = hitDelay;
            so.FindProperty("_arc").floatValue = arc;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        // il tipo del Custode: l'archetipo del re con il suo nome, senza il portale né l'anello (sono del controllo), e una taglia meno da re
        private static EnemyArchetype Archetype()
        {
            var archetype = CopyOf<EnemyArchetype>($"{EnemiesFolder}/BoneKing.asset", $"{EnemiesFolder}/Guardian.asset");
            var so = new SerializedObject(archetype);
            so.FindProperty("_nameKey").stringValue = "enemy.guardian";
            so.FindProperty("_aggroRange").floatValue = 7f;
            so.FindProperty("_leashRange").floatValue = 18f;
            so.FindProperty("_experience").intValue = 700;
            so.FindProperty("_recoverTime").floatValue = 0.6f;
            so.FindProperty("_corpseLifetime").floatValue = 20f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(archetype);
            return archetype;
        }

        private static GameObject GuardianPrefab(WeaponDefinition strike, WeaponDefinition quick, EnemyArchetype archetype)
        {
            string path = $"{PrefabsFolder}/Guardian.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            if (!AssetDatabase.CopyAsset($"{PrefabsFolder}/BoneKing.prefab", path))
            {
                throw new InvalidOperationException("Copia del prefab del re fallita");
            }

            Directory.CreateDirectory(MaterialsFolder);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                contents.name = "Guardian";
                var king = contents.GetComponent<BossController>();
                var circle = contents.GetComponentInChildren<TelegraphCircle>(true);
                var kingSo = new SerializedObject(king);
                var audio = kingSo.FindProperty("_audio").objectReferenceValue as AudioSource;
                var roar = kingSo.FindProperty("_roarClip").objectReferenceValue as AudioClip;
                var circleSo = new SerializedObject(circle);
                var fieldMaterial = circleSo.FindProperty("_material").objectReferenceValue as Material;
                var fillMaterial = circleSo.FindProperty("_fillMaterial").objectReferenceValue as Material;

                // via quello che è del re: l'anello, l'evocazione a intervalli, la porta
                UnityEngine.Object.DestroyImmediate(king);
                UnityEngine.Object.DestroyImmediate(circle);

                var guardian = contents.AddComponent<GuardianController>();
                var line = contents.AddComponent<TelegraphLine>();
                var lineSo = new SerializedObject(line);
                lineSo.FindProperty("_material").objectReferenceValue = fieldMaterial;
                lineSo.FindProperty("_fillMaterial").objectReferenceValue = fillMaterial;
                lineSo.ApplyModifiedPropertiesWithoutUndo();

                var guardianSo = new SerializedObject(guardian);
                guardianSo.FindProperty("_audio").objectReferenceValue = audio;
                guardianSo.FindProperty("_roarClip").objectReferenceValue = roar;
                guardianSo.FindProperty("_chargeTelegraph").objectReferenceValue = line;
                guardianSo.FindProperty("_summonPrefab").objectReferenceValue = Load<GameObject>($"{PrefabsFolder}/Skeleton.prefab").GetComponent<EnemyAI>();
                guardianSo.ApplyModifiedPropertiesWithoutUndo();

                var ai = contents.GetComponent<EnemyAI>();
                var aiSo = new SerializedObject(ai);
                aiSo.FindProperty("_archetype").objectReferenceValue = archetype;
                aiSo.ApplyModifiedPropertiesWithoutUndo();

                var healthSo = new SerializedObject(contents.GetComponent<Health>());
                healthSo.FindProperty("_maxHealth").floatValue = Health;
                healthSo.ApplyModifiedPropertiesWithoutUndo();

                var attackSo = new SerializedObject(contents.GetComponent<MeleeAttack>());
                attackSo.FindProperty("_weapon").objectReferenceValue = strike;
                attackSo.FindProperty("_quickWeapon").objectReferenceValue = quick;
                attackSo.ApplyModifiedPropertiesWithoutUndo();

                // quello che lascia da morto: un bruto grosso; il premio vero è il suo tesoro
                var loot = contents.GetComponent<LootDrop>();
                if (loot != null)
                {
                    var lootSo = new SerializedObject(loot);
                    lootSo.FindProperty("_lootTable").objectReferenceValue = Load<LootTable>(Root + "/Data/Loot/BruteLoot.asset");
                    lootSo.ApplyModifiedPropertiesWithoutUndo();
                }

                contents.transform.localScale = Vector3.one * 1.25f;
                TintRenderers(contents);

                // l'aura: la luce del re, in viola
                foreach (var light in contents.GetComponentsInChildren<Light>(true))
                {
                    light.color = AuraColor;
                    light.intensity = 6f;
                    light.range = 9f;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return Load<GameObject>(path);
        }

        // ogni materiale del modello, copiato e tinto: il modello del re resta com'è
        private static void TintRenderers(GameObject contents)
        {
            foreach (var renderer in contents.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null || !source.HasProperty("_BaseColor"))
                    {
                        continue;
                    }

                    string copyPath = $"{MaterialsFolder}/Guardian_{source.name}.mat";
                    var copy = AssetDatabase.LoadAssetAtPath<Material>(copyPath);
                    if (copy == null)
                    {
                        copy = new Material(source) { name = "Guardian_" + source.name };
                        AssetDatabase.CreateAsset(copy, copyPath);
                    }

                    copy.SetColor("_BaseColor", source.GetColor("_BaseColor") * Tint);
                    EditorUtility.SetDirty(copy);
                    materials[i] = copy;
                }

                renderer.sharedMaterials = materials;
            }
        }

        // il tesoro del re, chiuso finché vive il Custode, con meno rari: tre oggetti, magici e rari spostati, un unico possibile
        private static GameObject TreasurePrefab()
        {
            string path = $"{PrefabsFolder}/Dungeon/GuardianTreasure.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            if (!AssetDatabase.CopyAsset($"{PrefabsFolder}/Dungeon/KingsTreasure.prefab", path))
            {
                throw new InvalidOperationException("Copia del tesoro del re fallita");
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                contents.name = "GuardianTreasure";
                var chest = contents.GetComponentInChildren<Chest>(true);
                var so = new SerializedObject(chest);
                so.FindProperty("_sealedUntilBossDies").boolValue = false;
                so.FindProperty("_sealedUntilGuardianDies").boolValue = true;
                var bonus = so.FindProperty("_bonus");
                bonus.FindPropertyRelative("_extraRolls").intValue = 2;
                bonus.FindPropertyRelative("_magicFactor").floatValue = 1.8f;
                bonus.FindPropertyRelative("_rareFactor").floatValue = 2f;
                bonus.FindPropertyRelative("_guaranteed").enumValueIndex = (int)Rarity.Magic;
                bonus.FindPropertyRelative("_goldFactor").floatValue = 1.8f;
                bonus.FindPropertyRelative("_namedFactor").floatValue = 4f;
                bonus.FindPropertyRelative("_namedGuarantee").floatValue = 0.15f;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return Load<GameObject>(path);
        }

        // 'K' come il boss delle caverne, e 'k' per il suo tesoro (la 't' del tesoro del re la cripta ce l'ha già)
        private static void Tileset(GameObject guardian, GameObject treasure)
        {
            var tileset = Load<LevelTileset>(Root + "/Data/Levels/DungeonTileset.asset");
            var so = new SerializedObject(tileset);
            var markers = so.FindProperty("_markers");
            Set(markers, DungeonPopulator.GuardianSymbol, guardian);
            Set(markers, DungeonPopulator.GuardianTreasureSymbol, treasure);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tileset);
        }

        private static void Set(SerializedProperty markers, char symbol, GameObject prefab)
        {
            for (int i = 0; i < markers.arraySize; i++)
            {
                var entry = markers.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("_symbol").intValue == symbol)
                {
                    entry.FindPropertyRelative("_prefab").objectReferenceValue = prefab;
                    return;
                }
            }

            markers.arraySize++;
            var added = markers.GetArrayElementAtIndex(markers.arraySize - 1);
            added.FindPropertyRelative("_symbol").intValue = symbol;
            added.FindPropertyRelative("_prefab").objectReferenceValue = prefab;
            added.FindPropertyRelative("_replacesFloor").boolValue = false;
            added.FindPropertyRelative("_keepsRotation").boolValue = false;
        }
    }
}
