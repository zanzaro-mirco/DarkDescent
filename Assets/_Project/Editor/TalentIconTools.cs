using System.Collections.Generic;
using System.IO;
using DarkDescent.Magic;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Le icone dei talenti del Guerriero (D12 della M20): le undici abilità attive e i trentatré tipi di
    /// passivo, generate con Z-Image da <c>Tools/ComfyUI/images/talents.py</c> e messe in
    /// <c>Art/Icons/Talents</c> dopo il sì di Mirco. Questo strumento le importa come sprite e le dà alle
    /// abilità e alle opzioni dei nodi; <see cref="TalentTools.BuildWarrior"/> lo richiama alla fine, così
    /// un albero rifatto non le perde. Si rilancia.
    /// </summary>
    public static class TalentIconTools
    {
        public const string Folder = "Assets/_Project/Art/Icons/Talents";

        private const string SpellsFolder = "Assets/_Project/Data/Spells";
        private const string TalentsFolder = "Assets/_Project/Data/Talents";

        // l'asset di ogni abilità attiva e il suo file
        public static readonly (string Spell, string Icon)[] Skills =
        {
            ("PowerStrike", "skill_power_strike"),
            ("Charge", "skill_charge"),
            ("StoneSkin", "skill_stone_skin"),
            ("BattleShout", "skill_battle_shout"),
            ("ShieldBash", "skill_shield_bash"),
            ("ShieldWall", "skill_shield_wall"),
            ("LastStand", "skill_last_stand"),
            ("Whirlwind", "skill_whirlwind"),
            ("Execute", "skill_execute"),
            ("Leap", "skill_leap"),
            ("Bladestorm", "skill_bladestorm"),
        };

        // il tipo di ogni passivo, e di ogni opzione dei nodi a scelta: l'id del talento e il file
        public static readonly Dictionary<string, string> Passives = new Dictionary<string, string>
        {
            // la classe
            ["strong_arm"] = "talent_damage",
            ["toughness"] = "talent_life",
            ["keen_eye"] = "talent_crit",
            ["steady_hand"] = "talent_accuracy",
            ["momentum"] = "talent_cooldown",
            ["sturdy_mail"] = "talent_armor",
            ["discipline"] = "talent_mana_hit",
            ["second_skin"] = "talent_skin",
            ["bloodlust"] = "talent_lifesteal",
            ["battle_rhythm"] = "talent_attack_speed",
            ["rousing_shout"] = "talent_stun",
            ["last_defense"] = "talent_last_defense",
            ["heavy_step"] = "talent_speed",
            ["recovery"] = "talent_potion",
            ["weapon_master"] = "talent_weapon_master",
            ["furious_charge"] = "talent_area",
            ["fathers_shout"] = "talent_heal",
            ["iron_heart"] = "talent_iron_heart",

            // Difesa
            ["heavy_shield"] = "talent_block",
            ["thick_skin"] = "talent_thick_skin",
            ["recoil"] = "talent_recoil",
            ["shoulder_charge"] = "talent_cooldown",
            ["bulwark"] = "talent_duration",
            ["guardian_armor"] = "talent_shield_armor",
            ["retaliation"] = "talent_reflect",
            ["thunder_shield"] = "talent_area",
            ["blinding_shield"] = "talent_stun",
            ["steadfast"] = "talent_life",
            ["avenger"] = "talent_avenger",
            ["shelter"] = "talent_resist",
            ["fortress"] = "talent_fortress",

            // Armi
            ["two_handed_grip"] = "talent_two_handed",
            ["keen_edge"] = "talent_damage",
            ["weak_spot"] = "talent_crit_damage",
            ["circular_motion"] = "talent_attack_speed",
            ["vortex"] = "talent_area",
            ["executioners_eye"] = "talent_crit",
            ["slaughter"] = "talent_mana_kill",
            ["blood_on_the_blade"] = "talent_lifesteal",
            ["headsman"] = "talent_threshold",
            ["endless_whirlwind"] = "talent_mana_cost",
            ["frenzy"] = "talent_frenzy",
            ["weapon_mastery"] = "talent_damage",
        };

        [MenuItem("DarkDescent/Talenti/Assegna le icone")]
        public static void Apply()
        {
            if (!Directory.Exists(Folder))
            {
                Debug.Log($"[TalentIconTools] niente icone in {Folder}: si saltano");
                return;
            }

            foreach (string path in Directory.GetFiles(Folder, "*.png"))
            {
                Import(path.Replace('\\', '/'));
            }

            int spells = 0;
            foreach (var (spell, icon) in Skills)
            {
                var asset = AssetDatabase.LoadAssetAtPath<SpellDefinition>($"{SpellsFolder}/{spell}.asset");
                var sprite = Sprite(icon);
                if (asset == null || sprite == null)
                {
                    continue;
                }

                var so = new SerializedObject(asset);
                so.FindProperty("_icon").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                spells++;
            }

            int options = 0;
            foreach (string name in new[] { "Warrior", "WarriorDefense", "WarriorArms" })
            {
                var tree = AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{TalentsFolder}/{name}.asset");
                var so = new SerializedObject(tree);
                var nodes = so.FindProperty("_nodes");
                for (int i = 0; i < nodes.arraySize; i++)
                {
                    var list = nodes.GetArrayElementAtIndex(i).FindPropertyRelative("_options");
                    for (int j = 0; j < list.arraySize; j++)
                    {
                        var option = list.GetArrayElementAtIndex(j);
                        string id = option.FindPropertyRelative("_nameKey").stringValue.Replace("talent.", "");
                        if (Passives.TryGetValue(id, out string icon) && Sprite(icon) != null)
                        {
                            option.FindPropertyRelative("_icon").objectReferenceValue = Sprite(icon);
                            options++;
                        }
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tree);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TalentIconTools] {spells} abilità e {options} opzioni con la loro icona");
        }

        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");

        // sprite quadrati senza mipmap, come le altre icone
        private static void Import(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer.textureType == TextureImporterType.Sprite && !importer.mipmapEnabled)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }
}
