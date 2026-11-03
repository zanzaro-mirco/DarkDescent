using System;
using System.Linq;
using DarkDescent.Items;
using DarkDescent.Town;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Le armi del Mago (D4 e D12 della M21): il bastone a due mani, la bacchetta a una mano e il tomo
    /// nella mano libera, con i modelli di Adventurers; il database, le icone, le tabelle del loot e il
    /// banco del mercante, e gli affissi della magia ammessi anche sui tipi nuovi. Si rilancia: gli asset
    /// che ci sono già si aggiornano, e le tabelle non prendono una voce due volte.
    /// </summary>
    public static class MageWeaponTools
    {
        private const string ItemsFolder = "Assets/_Project/Data/Items";
        private const string AffixesFolder = "Assets/_Project/Data/Affixes";
        private const string ModelsFolder = "Assets/_Project/Art/Models/KayKit";
        private const string MerchantStallPath = "Assets/_Project/Prefabs/Town/MerchantStall.prefab";

        // le tabelle che li hanno: come le armi a due mani (M20)
        private static readonly string[] Tables = { "ChestLoot", "GoldChestLoot", "BruteLoot", "BossLoot", "MerchantTable" };

        [MenuItem("DarkDescent/Oggetti/Armi del Mago")]
        public static void Build()
        {
            var staff = SaveWeapon("Staff", "item.staff", "staff", new Vector2Int(1, 4), 150, 12, 18, magic: 40, range: 0.85f, interval: 1.5f, hitDelay: 0.9f,
                WeaponKind.Staff, twoHanded: true, heldLike: "Greatsword", heldRotation: new Vector3(180f, 0f, 0f));
            var wand = SaveWeapon("Wand", "item.wand", "wand", new Vector2Int(1, 2), 70, 4, 7, magic: 20, range: 0.6f, interval: 0.8f, hitDelay: 0.48f,
                WeaponKind.Wand, twoHanded: false, heldLike: "Dagger", heldRotation: new Vector3(180f, 0f, 0f));
            var tome = SaveTome("Tome", "item.tome", "spellbook_closed", new Vector2Int(2, 2), 100, magic: 30, bonusMagic: 5, bonusMana: 20, heldLike: "RoundShield", iconRotation: new Vector3(20f, -60f, 0f));
            var made = new ItemDefinition[] { staff, wand, tome };

            AssetDatabase.SaveAssets();

            // il database si rifà solo se manca qualcuno: rifarlo cambia l'ordine degli affissi, e con lui le scelte a seme
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            if (made.Any(item => !database.TryGet(item.Id, out _)))
            {
                ItemTools.UpdateDatabase();
            }

            foreach (var item in made)
            {
                ItemTools.RenderIcon(item);
            }

            foreach (string table in Tables)
            {
                TwoHandedTools.AddToTable(table, made);
            }

            AdmitMagicAffixes();
            SetMerchantStall();
            AssetDatabase.SaveAssets();
            Debug.Log($"[MageWeaponTools] {made.Length} oggetti del Mago, in {Tables.Length} tabelle");
        }

        private static WeaponDefinition SaveWeapon(string asset, string key, string model, Vector2Int size, int value, int min, int max, int magic,
            float range, float interval, float hitDelay, WeaponKind kind, bool twoHanded, string heldLike, Vector3? heldRotation = null)
        {
            string path = $"{ItemsFolder}/{asset}.asset";
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(weapon, path);
            }

            var so = new SerializedObject(weapon);
            Common(so, key, model, size, value, heldLike, heldRotation);
            so.FindProperty("_minDamage").intValue = min;
            so.FindProperty("_maxDamage").intValue = max;
            so.FindProperty("_kind").intValue = (int)kind;
            so.FindProperty("_requiredStrength").intValue = 0;
            so.FindProperty("_requiredMagic").intValue = magic;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_rangeTolerance").floatValue = 0.5f;
            so.FindProperty("_attackInterval").floatValue = interval;
            so.FindProperty("_hitDelay").floatValue = hitDelay;
            so.FindProperty("_arc").floatValue = 360f;
            so.FindProperty("_twoHanded").boolValue = twoHanded;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        private static ArmorDefinition SaveTome(string asset, string key, string model, Vector2Int size, int value, int magic, int bonusMagic, int bonusMana, string heldLike, Vector3? iconRotation = null)
        {
            string path = $"{ItemsFolder}/{asset}.asset";
            var tome = AssetDatabase.LoadAssetAtPath<ArmorDefinition>(path);
            if (tome == null)
            {
                tome = ScriptableObject.CreateInstance<ArmorDefinition>();
                AssetDatabase.CreateAsset(tome, path);
            }

            var so = new SerializedObject(tome);
            Common(so, key, model, size, value, heldLike, null, iconRotation);
            so.FindProperty("_slot").intValue = (int)EquipSlot.Offhand;
            so.FindProperty("_armor").intValue = 0;
            so.FindProperty("_blockChance").intValue = 0;
            so.FindProperty("_requiredStrength").intValue = 0;
            so.FindProperty("_requiredMagic").intValue = magic;
            so.FindProperty("_tome").boolValue = true;
            so.FindProperty("_bonusMagic").intValue = bonusMagic;
            so.FindProperty("_bonusMana").intValue = bonusMana;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tome);
            return tome;
        }

        // i campi di ogni oggetto; la posa in mano parte da quella di un oggetto simile (si ritocca con la foto)
        private static void Common(SerializedObject so, string key, string model, Vector2Int size, int value, string heldLike, Vector3? heldRotation = null, Vector3? iconRotation = null)
        {
            var like = new SerializedObject(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemsFolder}/{heldLike}.asset"));
            var id = so.FindProperty("_id");
            if (string.IsNullOrEmpty(id.stringValue))
            {
                id.stringValue = Guid.NewGuid().ToString("N");
            }

            so.FindProperty("_nameKey").stringValue = key;
            so.FindProperty("_model").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{model}.fbx");
            so.FindProperty("_size").vector2IntValue = size;
            so.FindProperty("_value").intValue = value;
            so.FindProperty("_heldPosition").vector3Value = like.FindProperty("_heldPosition").vector3Value;
            so.FindProperty("_heldRotation").vector3Value = heldRotation ?? like.FindProperty("_heldRotation").vector3Value;
            so.FindProperty("_iconRotation").vector3Value = iconRotation ?? like.FindProperty("_iconRotation").vector3Value;
        }

        // gli affissi della Magia e del mana possono cadere anche su bastoni, bacchette e tomi
        private static void AdmitMagicAffixes()
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:AffixDefinition", new[] { AffixesFolder }))
            {
                var affix = AssetDatabase.LoadAssetAtPath<AffixDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (affix.Effect != AffixEffect.Magic && affix.Effect != AffixEffect.Mana)
                {
                    continue;
                }

                var so = new SerializedObject(affix);
                var targets = so.FindProperty("_targets");
                if ((targets.intValue & (int)AffixTargets.Caster) == 0)
                {
                    targets.intValue |= (int)AffixTargets.Caster;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(affix);
                    changed++;
                }
            }

            Debug.Log($"[MageWeaponTools] {changed} affissi della magia ammessi sui tipi nuovi");
        }

        // il banco del mercante tratta anche la merce del Mago, e tira due oggetti in più
        private static void SetMerchantStall()
        {
            var stall = AssetDatabase.LoadAssetAtPath<GameObject>(MerchantStallPath).GetComponent<Merchant>();
            var so = new SerializedObject(stall);
            so.FindProperty("_goods").intValue = (int)(ShopGoods.Jewelry | ShopGoods.Scrolls | ShopGoods.Arcane);
            so.FindProperty("_stockSize").intValue = 6;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stall);
        }
    }
}
