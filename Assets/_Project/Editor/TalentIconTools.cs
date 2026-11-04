using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Magic;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Le icone dei talenti (D12 della M20 e D13 della M21): le abilità attive e i tipi di
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

        // le dodici abilità del Mago (M21, D13): il Dardo arcano del sinistro e le undici degli alberi
        public static readonly (string Spell, string Icon)[] MageSkills =
        {
            ("ArcaneBolt", "skill_arcane_bolt"),
            ("Firebolt", "skill_firebolt"),
            ("FireRing", "skill_fire_ring"),
            ("Blink", "skill_blink"),
            ("ManaShield", "skill_mana_shield"),
            ("Fireball", "skill_fireball"),
            ("FlameWall", "skill_flame_wall"),
            ("Meteor", "skill_meteor"),
            ("IceBolt", "skill_ice_bolt"),
            ("FrostNova", "skill_frost_nova"),
            ("FrostArmor", "skill_frost_armor"),
            ("Blizzard", "skill_blizzard"),
        };

        // i passivi del Mago: molti tipi sono quelli del Guerriero (cooldown, area, vita, armatura, durata…)
        public static readonly Dictionary<string, string> MagePassives = new Dictionary<string, string>
        {
            // la classe
            ["keen_mind"] = "talent_spell_damage",
            ["mana_reserve"] = "talent_mana_pool",
            ["swift_bolt"] = "talent_cooldown",
            ["wide_flame"] = "talent_area",
            ["ash_skin"] = "talent_life",
            ["concentration"] = "talent_mana_hit",
            ["cold_mind"] = "talent_mana_regen",
            ["lasting_shield"] = "talent_duration",
            ["last_spark"] = "talent_last_defense",
            ["quick_step"] = "talent_cooldown",
            ["restoration"] = "talent_potion",
            ["fire_master"] = "talent_spell_damage",
            ["arcane_heart"] = "talent_mana_pool",
            ["spark"] = "talent_free_cast",
            ["blaze"] = "talent_burn",
            ["spark_rain"] = "talent_spark_rain",
            ["burning_ring"] = "talent_burn",

            // Fuoco
            ["inflamed_mind"] = "talent_spell_damage",
            ["great_blaze"] = "talent_area",
            ["explosion"] = "talent_fire_damage",
            ["quick_hand"] = "talent_cooldown",
            ["lasting_flames"] = "talent_duration",
            ["burning_magic"] = "talent_magic",
            ["pyre"] = "talent_mana_kill",
            ["live_embers"] = "talent_fire_damage",
            ["ardor"] = "talent_cast_haste",
            ["searing_bolt"] = "talent_fire_damage",
            ["fire_mastery"] = "talent_spell_damage",
            ["double_meteor"] = "talent_echo",
            ["fire_tide"] = "talent_fire_ground",

            // Gelo
            ["frozen_skin"] = "talent_armor",
            ["piercing_cold"] = "talent_slow",
            ["shards"] = "talent_frost_damage",
            ["quick_frost"] = "talent_cooldown",
            ["deep_frost"] = "talent_freeze",
            ["cold_blood"] = "talent_life",
            ["rime"] = "talent_area",
            ["ice_shelter"] = "talent_resist",
            ["ice_mind"] = "talent_mana_hit",
            ["ice_fortress"] = "talent_duration",
            ["lingering_frost"] = "talent_slow",
            ["double_nova"] = "talent_echo",
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
            foreach (var (spell, icon) in Skills.Concat(MageSkills))
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
            foreach (string name in new[] { "Warrior", "WarriorDefense", "WarriorArms", "Mage", "MageFire", "MageFrost" })
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
                        if ((Passives.TryGetValue(id, out string icon) || MagePassives.TryGetValue(id, out icon)) && Sprite(icon) != null)
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
        public static void ImportSprite(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            Import(path);
        }

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
