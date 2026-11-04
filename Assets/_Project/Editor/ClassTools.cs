using System.Collections.Generic;
using System.IO;
using DarkDescent.Classes;
using DarkDescent.Items;
using DarkDescent.Magic;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.Talents;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Le classi di personaggio (D2 della M21) come asset: il Guerriero e il Mago in <c>Data/Classes</c>, il
    /// loro elenco, e il <see cref="PlayerClass"/> sul prefab del giocatore. Il Guerriero prende gli
    /// attributi dal prefab, così il cavaliere di sempre resta quello di sempre; il Mago prende i suoi
    /// alberi quando ci sono (<c>Data/Talents/Mage.asset</c>, dal 21.5). Si rilancia.
    /// </summary>
    public static class ClassTools
    {
        public const string Folder = "Assets/_Project/Data/Classes";
        public const string CatalogPath = Folder + "/ClassCatalog.asset";

        private const string PlayerPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string TalentsFolder = "Assets/_Project/Data/Talents";
        private const string AttackPath = "Assets/_Project/Data/Spells/Attack.asset";
        private const string ArcanePath = "Assets/_Project/Data/Spells/ArcaneBolt.asset";
        private const string ItemsFolder = "Assets/_Project/Data/Items";
        private const string IconsFolder = "Assets/_Project/Art/Icons/Classes";

        // come HeroSetup di sempre: due punti in Forza, due in Vitalità e uno in Destrezza
        private static readonly StatType[] WarriorPattern = { StatType.Strength, StatType.Vitality, StatType.Strength, StatType.Vitality, StatType.Dexterity };

        // il Mago: Magia e Vitalità, e un punto in Destrezza
        private static readonly StatType[] MagePattern = { StatType.Magic, StatType.Vitality, StatType.Magic, StatType.Vitality, StatType.Dexterity };

        [MenuItem("DarkDescent/Classi/Costruisci le classi")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            var attack = AssetDatabase.LoadAssetAtPath<SpellDefinition>(AttackPath);
            // il Dardo arcano è del sinistro del Mago (21.4)
            var arcane = AssetDatabase.LoadAssetAtPath<SpellDefinition>(ArcanePath) ?? attack;

            // gli attributi del cavaliere sono quelli del prefab
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            var stats = new SerializedObject(player.GetComponent<CharacterStats>());
            float strength = stats.FindProperty("_strength").floatValue;
            float dexterity = stats.FindProperty("_dexterity").floatValue;
            float magic = stats.FindProperty("_magic").floatValue;
            float vitality = stats.FindProperty("_vitality").floatValue;

            var warrior = Asset<CharacterClass>("Warrior");
            warrior.Define(CharacterClass.WarriorId, "class.warrior", "knight", Tree("Warrior"), new[] { Tree("WarriorDefense"), Tree("WarriorArms") },
                strength, dexterity, magic, vitality, WarriorPattern, 1f, attack);
            EditorUtility.SetDirty(warrior);

            // il Mago non ha ancora l'albero (21.5): finché manca non si sceglie nella creazione
            var mage = Asset<CharacterClass>("Mage");
            mage.Define("mage", "class.mage", "mage", Tree("Mage"), new[] { Tree("MageFire"), Tree("MageFrost") },
                8f, 17f, 35f, 20f, MagePattern, 1.5f, arcane);
            // la bacchetta e il tomo: il Mago non comincia con la spada del cavaliere (21.6)
            mage.DefineKit(AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemsFolder + "/Wand.asset"), AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemsFolder + "/Tome.asset"));
            EditorUtility.SetDirty(mage);

            // gli emblemi di Z-Image, se ci sono già (21.7)
            foreach (var characterClass in new[] { warrior, mage })
            {
                string iconPath = $"{IconsFolder}/class_{characterClass.Id}.png";
                TalentIconTools.ImportSprite(iconPath);
                characterClass.DefineIcon(AssetDatabase.LoadAssetAtPath<Sprite>(iconPath));
                EditorUtility.SetDirty(characterClass);
            }

            var catalog = Asset<CharacterClassCatalog>("ClassCatalog");
            catalog.Define(new[] { warrior, mage });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            // il componente sul prefab del giocatore, con l'elenco
            var contents = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var component = contents.GetComponent<PlayerClass>() ?? contents.AddComponent<PlayerClass>();
                var so = new SerializedObject(component);
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            Debug.Log($"[ClassTools] classi costruite: Guerriero ({strength}/{dexterity}/{magic}/{vitality}) e Mago");
        }

        private static TalentTree Tree(string name) => AssetDatabase.LoadAssetAtPath<TalentTree>($"{TalentsFolder}/{name}.asset");

        private static T Asset<T>(string name) where T : ScriptableObject
        {
            string path = $"{Folder}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }
    }
}
