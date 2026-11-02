using System;
using System.Collections.Generic;
using DarkDescent.Magic;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.Talents;
using DarkDescent.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Costruisce l'albero del Guerriero dal disegno approvato da Mirco (passo 20.1 della M20,
    /// <c>docs/milestones/M20_albero_Guerriero.md</c>): le abilità nuove in <c>Data/Spells</c>, i tre
    /// alberi in <c>Data/Talents</c> e il componente sul cavaliere. Da qui in poi la fonte dell'albero
    /// è questa tabella: il documento resta la fotografia del disegno approvato. Si rifà con un comando,
    /// come il borgo dalla sua mappa; gli id degli asset che ci sono già non cambiano.
    /// </summary>
    public static class TalentTools
    {
        public const string SpellsFolder = "Assets/_Project/Data/Spells";
        public const string TalentsFolder = "Assets/_Project/Data/Talents";
        public const string PlayerPrefab = "Assets/_Project/Prefabs/Player.prefab";
        public const string CoreScene = "Assets/_Project/Scenes/Core.unity";

        // i posti della barra: 48 di lato, 6 di spazio (come la cintura)
        private const float SlotSize = 48f;
        private const float SlotSpacing = 6f;

        // le abilità del Guerriero (D8 e disegno): nome dell'asset, chiave, tipo, mana, ricarica,
        // raggio, durata. Danno, stordimento e il resto arrivano con il loro comportamento (20.4)
        private static readonly (string asset, string key, SpellKind kind, float mana, float cooldown, float radius, float duration)[] Abilities =
        {
            ("PowerStrike", "spell.power_strike", SpellKind.Strike, 4f, 3f, 0f, 0f),
            ("Charge", "spell.charge", SpellKind.Charge, 10f, 8f, 0f, 0f),
            ("BattleShout", "spell.battle_shout", SpellKind.Shout, 12f, 30f, 5f, 12f),
            ("ShieldBash", "spell.shield_bash", SpellKind.ShieldBash, 8f, 6f, 0f, 0f),
            ("ShieldWall", "spell.shield_wall", SpellKind.ShieldWall, 15f, 40f, 0f, 5f),
            ("LastStand", "spell.last_stand", SpellKind.LastStand, 20f, 90f, 0f, 10f),
            ("Whirlwind", "spell.whirlwind", SpellKind.Whirlwind, 12f, 4f, 3f, 0f),
            ("Execute", "spell.execute", SpellKind.Execute, 10f, 6f, 0f, 0f),
            ("Leap", "spell.leap", SpellKind.Leap, 14f, 12f, 3f, 0f),
            ("Bladestorm", "spell.bladestorm", SpellKind.Bladestorm, 25f, 45f, 3.5f, 3f),
        };

        [MenuItem("DarkDescent/Talenti/Costruisci il Guerriero")]
        public static void BuildWarrior()
        {
            if (!AssetDatabase.IsValidFolder(TalentsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Talents");
            }

            var spells = new Dictionary<string, SpellDefinition>();
            foreach (var ability in Abilities)
            {
                spells[ability.asset] = SaveAbility(ability);
            }

            spells["StoneSkin"] = AssetDatabase.LoadAssetAtPath<SpellDefinition>($"{SpellsFolder}/StoneSkin.asset");
            var warrior = SaveTree("Warrior", "warrior", false, ClassNodes(spells), new[] { new TalentGate(3, 6), new TalentGate(6, 14) });
            var defense = SaveTree("WarriorDefense", "warrior.defense", true, DefenseNodes(spells), new[] { new TalentGate(3, 5), new TalentGate(5, 12) });
            var arms = SaveTree("WarriorArms", "warrior.arms", true, ArmsNodes(spells), new[] { new TalentGate(3, 5), new TalentGate(5, 12) });
            AssetDatabase.SaveAssets();
            InstallOnPlayer(warrior, defense, arms);
            InstallBar();
            Debug.Log($"[TalentTools] Guerriero: {warrior.Nodes.Count} nodi, Difesa {defense.Nodes.Count}, Armi {arms.Nodes.Count}");
        }

        // l'albero della classe: il boia a sinistra, la carica in mezzo, il muro a destra
        private static TalentNode[] ClassNodes(Dictionary<string, SpellDefinition> s) => new[]
        {
            Active("power_strike", 0, 3, s["PowerStrike"]),
            Passive("strong_arm", 1, 1, 3, Links("power_strike"), Stat(StatType.DamageBonus, 3f)),
            Active("charge", 1, 3, s["Charge"], Links("power_strike")),
            Passive("toughness", 1, 5, 3, Links("power_strike"), Stat(StatType.MaxLifePercent, 4f)),
            Passive("keen_eye", 2, 0, 3, Links("strong_arm"), Stat(StatType.CritChance, 2f)),
            Passive("steady_hand", 2, 2, 2, Links("strong_arm"), Stat(StatType.ToHit, 5f)),
            Passive("momentum", 2, 3, 2, Links("charge"), TalentEffect.ForAbility(s["Charge"], AbilityParam.Cooldown, -2f)),
            Active("stone_skin", 2, 4, s["StoneSkin"], Links("toughness")),
            Passive("sturdy_mail", 2, 6, 3, Links("toughness"), Stat(StatType.Armor, 5f, ModifierKind.Percent)),
            Passive("discipline", 3, 1, 2, Links("keen_eye", "steady_hand"), TalentEffect.OnTrigger(TalentTrigger.Hit, TalentOutcome.Mana, 1f)),
            Active("battle_shout", 3, 3, s["BattleShout"], Links("steady_hand", "momentum", "stone_skin")),
            Passive("second_skin", 3, 5, 2, Links("stone_skin", "sturdy_mail"),
                TalentEffect.ForAbility(s["StoneSkin"], AbilityParam.Duration, 5f), TalentEffect.ForAbility(s["StoneSkin"], AbilityParam.ManaCost, -2f)),
            Choice("battle_instinct", 4, 1, Links("discipline"),
                Option("bloodlust", Stat(StatType.LifeSteal, 3f)),
                Option("battle_rhythm", Stat(StatType.AttackSpeed, 8f))),
            Passive("rousing_shout", 4, 3, 2, Links("battle_shout"), TalentEffect.ForAbility(s["BattleShout"], AbilityParam.Stun, 1f)),
            Passive("last_defense", 4, 5, 1, Links("second_skin"), Stat(StatType.DamageResist, 30f, condition: TalentCondition.LowLife)),
            Passive("heavy_step", 5, 2, 2, Links("battle_instinct", "rousing_shout"), Stat(StatType.MoveSpeed, 5f)),
            Passive("recovery", 5, 4, 2, Links("rousing_shout", "last_defense"), Stat(StatType.PotionBonus, 15f)),
            Passive("weapon_master", 6, 1, 2, Links("heavy_step"), Stat(StatType.DamageBonus, 5f), Stat(StatType.ToHit, 5f)),
            Choice("warlord", 6, 3, Links("heavy_step", "recovery"),
                Option("furious_charge", TalentEffect.ForAbility(s["Charge"], AbilityParam.Radius, 3f)),
                Option("fathers_shout", TalentEffect.ForAbility(s["BattleShout"], AbilityParam.Heal, 15f))),
            Passive("iron_heart", 6, 5, 2, Links("recovery"), Stat(StatType.MaxLifePercent, 5f), Stat(StatType.Armor, 5f, ModifierKind.Percent)),
        };

        // Difesa: lo scudo, i blocchi e gli stordimenti
        private static TalentNode[] DefenseNodes(Dictionary<string, SpellDefinition> s) => new[]
        {
            Active("shield_bash", 0, 3, s["ShieldBash"]),
            Passive("heavy_shield", 1, 1, 3, Links("shield_bash"), Stat(StatType.BlockChance, 3f)),
            Active("shield_wall", 1, 3, s["ShieldWall"], Links("shield_bash")),
            Passive("thick_skin", 1, 5, 3, Links("shield_bash"), Stat(StatType.DamageReduction, 1f)),
            Passive("recoil", 2, 0, 2, Links("heavy_shield"), TalentEffect.OnTrigger(TalentTrigger.Block, TalentOutcome.Mana, 2f)),
            Passive("shoulder_charge", 2, 2, 2, Links("heavy_shield", "shield_wall"), TalentEffect.ForAbility(s["ShieldBash"], AbilityParam.Cooldown, -2f)),
            Passive("bulwark", 2, 4, 2, Links("shield_wall", "thick_skin"), TalentEffect.ForAbility(s["ShieldWall"], AbilityParam.Duration, 1.5f)),
            Passive("guardian_armor", 2, 6, 3, Links("thick_skin"), Stat(StatType.Armor, 6f, ModifierKind.Percent, TalentCondition.Shield)),
            Passive("retaliation", 3, 1, 2, Links("recoil", "shoulder_charge"), TalentEffect.OnTrigger(TalentTrigger.Block, TalentOutcome.Reflect, 25f)),
            Choice("shield_mastery", 3, 3, Links("shoulder_charge", "bulwark"),
                Option("thunder_shield", TalentEffect.ForAbility(s["ShieldBash"], AbilityParam.Radius, 2.5f)),
                Option("blinding_shield", TalentEffect.ForAbility(s["ShieldBash"], AbilityParam.Stun, 1.5f))),
            Passive("steadfast", 3, 5, 2, Links("bulwark", "guardian_armor"), Stat(StatType.MaxLifePercent, 5f)),
            Passive("avenger", 4, 2, 2, Links("retaliation", "shield_mastery"), TalentEffect.OnTrigger(TalentTrigger.Block, TalentOutcome.NextHit, 15f)),
            Passive("shelter", 4, 4, 2, Links("shield_mastery", "steadfast"), Stat(StatType.DamageResist, 4f)),
            Active("last_stand", 5, 3, s["LastStand"], Links("avenger", "shelter")),
            Passive("fortress", 5, 5, 1, Links("shelter"), Stat(StatType.PiercedBlock, 50f, condition: TalentCondition.Shield)),
        };

        // Armi: le due mani, i critici, il turbine
        private static TalentNode[] ArmsNodes(Dictionary<string, SpellDefinition> s) => new[]
        {
            Active("whirlwind", 0, 3, s["Whirlwind"]),
            Passive("two_handed_grip", 1, 1, 3, Links("whirlwind"), Stat(StatType.DamageBonus, 5f, condition: TalentCondition.TwoHanded)),
            Passive("keen_edge", 1, 3, 2, Links("whirlwind"), TalentEffect.ForAbility(s["Whirlwind"], AbilityParam.Damage, 15f)),
            Passive("weak_spot", 1, 5, 3, Links("whirlwind"), Stat(StatType.CritDamage, 10f)),
            Passive("circular_motion", 2, 0, 2, Links("two_handed_grip"), Stat(StatType.AttackSpeed, 5f)),
            Active("execute", 2, 2, s["Execute"], Links("two_handed_grip", "keen_edge")),
            Passive("vortex", 2, 4, 2, Links("keen_edge", "weak_spot"), TalentEffect.ForAbility(s["Whirlwind"], AbilityParam.Radius, 0.5f)),
            Passive("executioners_eye", 2, 6, 2, Links("weak_spot"), Stat(StatType.CritChance, 3f)),
            Passive("slaughter", 3, 1, 2, Links("circular_motion", "execute"), TalentEffect.OnTrigger(TalentTrigger.Kill, TalentOutcome.Mana, 3f)),
            Active("leap", 3, 3, s["Leap"], Links("execute", "vortex")),
            Passive("blood_on_the_blade", 3, 5, 2, Links("vortex", "executioners_eye"), Stat(StatType.LifeSteal, 2f)),
            Choice("fighting_style", 4, 2, Links("leap", "slaughter"),
                Option("headsman", TalentEffect.ForAbility(s["Execute"], AbilityParam.Threshold, 10f)),
                Option("endless_whirlwind", TalentEffect.ForAbility(s["Whirlwind"], AbilityParam.ManaCost, -4f))),
            Passive("frenzy", 4, 4, 2, Links("leap", "blood_on_the_blade"),
                TalentEffect.OnTrigger(TalentTrigger.Kill, TalentOutcome.Buff, 5f, StatType.AttackSpeed, 5f)),
            Passive("weapon_mastery", 5, 1, 3, Links("fighting_style"), Stat(StatType.DamageBonus, 4f)),
            Active("bladestorm", 5, 3, s["Bladestorm"], Links("fighting_style", "frenzy")),
        };

        private static string[] Links(params string[] ids) => ids;

        private static TalentEffect Stat(StatType stat, float perRank, ModifierKind modifier = ModifierKind.Flat, TalentCondition condition = TalentCondition.None) =>
            TalentEffect.ForStat(stat, perRank, modifier, condition);

        private static TalentOption Option(string id, params TalentEffect[] effects) => new TalentOption("talent." + id, null, effects);

        private static TalentNode Active(string id, int row, int column, SpellDefinition ability, string[] links = null) =>
            new TalentNode(id, row, column, 1, links, new TalentOption("talent." + id, ability ?? throw new ArgumentNullException(nameof(ability), id)));

        private static TalentNode Passive(string id, int row, int column, int ranks, string[] links, params TalentEffect[] effects) =>
            new TalentNode(id, row, column, ranks, links, Option(id, effects));

        private static TalentNode Choice(string id, int row, int column, string[] links, TalentOption first, TalentOption second) =>
            new TalentNode(id, row, column, 1, links, first, second);

        private static SpellDefinition SaveAbility((string asset, string key, SpellKind kind, float mana, float cooldown, float radius, float duration) ability)
        {
            string path = $"{SpellsFolder}/{ability.asset}.asset";
            var spell = AssetDatabase.LoadAssetAtPath<SpellDefinition>(path);
            if (spell == null)
            {
                spell = ScriptableObject.CreateInstance<SpellDefinition>();
                AssetDatabase.CreateAsset(spell, path);
            }

            var so = new SerializedObject(spell);
            var id = so.FindProperty("_id");
            if (string.IsNullOrEmpty(id.stringValue))
            {
                id.stringValue = Guid.NewGuid().ToString("N");
            }

            so.FindProperty("_nameKey").stringValue = ability.key;
            so.FindProperty("_kind").intValue = (int)ability.kind;
            so.FindProperty("_manaCost").floatValue = ability.mana;
            so.FindProperty("_cooldown").floatValue = ability.cooldown;
            so.FindProperty("_radius").floatValue = ability.radius;
            so.FindProperty("_duration").floatValue = ability.duration;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spell);
            return spell;
        }

        private static TalentTree SaveTree(string asset, string id, bool specialization, TalentNode[] nodes, TalentGate[] gates)
        {
            string path = $"{TalentsFolder}/{asset}.asset";
            var tree = AssetDatabase.LoadAssetAtPath<TalentTree>(path);
            if (tree == null)
            {
                tree = ScriptableObject.CreateInstance<TalentTree>();
                AssetDatabase.CreateAsset(tree, path);
            }

            tree.Define(id, "talent.tree." + id, specialization, nodes, gates);
            var errors = tree.Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"{asset}: " + string.Join("; ", errors));
            }

            EditorUtility.SetDirty(tree);
            return tree;
        }

        /// <summary>
        /// La barra di otto posti, F1–F8 (D9 della M20): i posti che mancano sono copie dell'ultimo, uno
        /// accanto all'altro, ognuno con il suo numero, il suo tasto e il riferimento della barra.
        /// </summary>
        [MenuItem("DarkDescent/Talenti/Barra a otto posti in Core")]
        public static void InstallBar()
        {
            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var bar = UnityEngine.Object.FindFirstObjectByType<SpellBarView>(FindObjectsInactive.Include);
            var so = new SerializedObject(bar);
            var slots = so.FindProperty("_slots");
            var last = (SpellSlotView)slots.GetArrayElementAtIndex(slots.arraySize - 1).objectReferenceValue;
            for (int index = slots.arraySize; index < SpellBook.Slots; index++)
            {
                var copy = UnityEngine.Object.Instantiate(last.gameObject, last.transform.parent);
                copy.name = $"Spell_{index + 1}";
                var view = copy.GetComponent<SpellSlotView>();
                var slot = new SerializedObject(view);
                slot.FindProperty("_slot").intValue = index;
                slot.FindProperty("_bar").objectReferenceValue = bar;
                slot.ApplyModifiedPropertiesWithoutUndo();
                ((RectTransform)copy.transform).anchoredPosition = new Vector2(index * (SlotSize + SlotSpacing), 0f);
                copy.transform.Find("Key").GetComponent<TMP_Text>().text = $"F{index + 1}";
                slots.arraySize = index + 1;
                slots.GetArrayElementAtIndex(index).objectReferenceValue = view;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            var rect = (RectTransform)bar.transform;
            rect.sizeDelta = new Vector2(SpellBook.Slots * SlotSize + (SpellBook.Slots - 1) * SlotSpacing, SlotSize);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[TalentTools] barra: {slots.arraySize} posti in {CoreScene}");
        }

        private static void InstallOnPlayer(TalentTree warrior, TalentTree defense, TalentTree arms)
        {
            var player = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var talents = player.GetComponent<PlayerTalents>();
                if (talents == null)
                {
                    talents = player.AddComponent<PlayerTalents>();
                }

                var so = new SerializedObject(talents);
                so.FindProperty("_classTree").objectReferenceValue = warrior;
                var specializations = so.FindProperty("_specializations");
                specializations.arraySize = 2;
                specializations.GetArrayElementAtIndex(0).objectReferenceValue = defense;
                specializations.GetArrayElementAtIndex(1).objectReferenceValue = arms;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }
    }
}
