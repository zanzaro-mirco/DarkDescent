using System;
using System.Collections.Generic;
using DarkDescent.Magic;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Talents;
using DarkDescent.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
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
        public const string ControllerPath = "Assets/_Project/Art/Animations/Character.controller";
        public const string EffectsFolder = "Assets/_Project/Prefabs/VFX";
        private const string AnimationsFolder = "Assets/_Project/Art/Animations/KayKit";
        private const string SoundsFolder = "Assets/_Project/Audio/SFX";

        // le abilità del Guerriero (D8 e disegno): nome dell'asset, chiave, tipo, mana, ricarica, e i
        // numeri del loro comportamento (20.4)
        private static readonly AbilityRow[] Abilities =
        {
            new AbilityRow("Attack", "spell.attack", SpellKind.Attack, 0f, 0f) { Icon = "Assets/_Project/Art/Icons/ShortSword.png" },
            new AbilityRow("PowerStrike", "spell.power_strike", SpellKind.Strike, 4f, 3f) { Weapon = 160f },
            new AbilityRow("Charge", "spell.charge", SpellKind.Charge, 10f, 8f) { Weapon = 100f, Stun = 1f, MinRange = 3f, Range = 10f },
            new AbilityRow("BattleShout", "spell.battle_shout", SpellKind.Shout, 12f, 30f) { Radius = 5f, Duration = 12f, Bonus = 15f },
            new AbilityRow("ShieldBash", "spell.shield_bash", SpellKind.ShieldBash, 8f, 6f) { Weapon = 100f, Stun = 1.5f },
            new AbilityRow("ShieldWall", "spell.shield_wall", SpellKind.ShieldWall, 15f, 40f) { Duration = 5f, MoveFactor = 0.5f },
            new AbilityRow("LastStand", "spell.last_stand", SpellKind.LastStand, 20f, 90f) { Duration = 10f, Bonus = 30f },
            new AbilityRow("Whirlwind", "spell.whirlwind", SpellKind.Whirlwind, 12f, 4f) { Weapon = 90f, Radius = 3f },
            new AbilityRow("Execute", "spell.execute", SpellKind.Execute, 10f, 6f) { Weapon = 100f, Bonus = 250f, Threshold = 25f },
            new AbilityRow("Leap", "spell.leap", SpellKind.Leap, 14f, 12f) { Weapon = 120f, Stun = 1f, Radius = 3f, Range = 8f },
            new AbilityRow("Bladestorm", "spell.bladestorm", SpellKind.Bladestorm, 25f, 45f) { Weapon = 60f, Radius = 3.5f, Duration = 3f, MoveFactor = 0.7f, Tick = 0.5f },
        };

        // i gesti delle abilità: lo stato del controller, il file delle animazioni e la fine del nome della clip
        private static readonly (string state, string file, string clip)[] AbilityStates =
        {
            (WarriorAbilities.StrikeState, "Rig_Medium_CombatMelee", "1H_Attack_Chop"),
            (WarriorAbilities.SpinState, "Rig_Medium_CombatMelee", "2H_Attack_Spinning"),
            (WarriorAbilities.BashState, "Rig_Medium_CombatMelee", "Block_Attack"),
            (WarriorAbilities.WallState, "Rig_Medium_CombatMelee", "Melee_Blocking"),
            (WarriorAbilities.LeapState, "Rig_Medium_CombatMelee", "1H_Attack_Jump_Chop"),
            (WarriorAbilities.ShoutState, "Rig_Medium_Simulation", "Cheering"),
            (CharacterAnimatorDriver.TwoHandedAttackState, "Rig_Medium_CombatMelee", "2H_Attack_Chop"),
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
                spells[ability.Asset] = SaveAbility(ability);
            }

            var attack = spells["Attack"];
            spells["StoneSkin"] = AssetDatabase.LoadAssetAtPath<SpellDefinition>($"{SpellsFolder}/StoneSkin.asset");
            var warrior = SaveTree("Warrior", "warrior", false, ClassNodes(spells), new[] { new TalentGate(3, 6), new TalentGate(6, 14) });
            var defense = SaveTree("WarriorDefense", "warrior.defense", true, DefenseNodes(spells), new[] { new TalentGate(3, 5), new TalentGate(5, 12) });
            var arms = SaveTree("WarriorArms", "warrior.arms", true, ArmsNodes(spells), new[] { new TalentGate(3, 5), new TalentGate(5, 12) });
            AssetDatabase.SaveAssets();
            // le icone di Z-Image, se ci sono già (20.7)
            TalentIconTools.Apply();
            InstallAbilityStates();
            var effects = InstallAbilityEffects();
            InstallOnPlayer(warrior, defense, arms, effects, attack);
            Debug.Log($"[TalentTools] Guerriero: {warrior.Nodes.Count} nodi, Difesa {defense.Nodes.Count}, Armi {arms.Nodes.Count}");
        }

        // l'albero della classe: il boia a sinistra, la carica in mezzo, il muro a destra
        private static TalentNode[] ClassNodes(Dictionary<string, SpellDefinition> s) => new[]
        {
            Active("power_strike", 0, 3, s["PowerStrike"]),
            Passive("strong_arm", 1, 1, 3, Links("power_strike"), Stat(StatType.DamageBonus, 2f)),
            Active("charge", 1, 3, s["Charge"], Links("power_strike")),
            Passive("toughness", 1, 5, 3, Links("power_strike"), Stat(StatType.MaxLifePercent, 4f)),
            Passive("keen_eye", 2, 0, 3, Links("strong_arm"), Stat(StatType.CritChance, 2f)),
            Passive("steady_hand", 2, 2, 2, Links("strong_arm"), Stat(StatType.ToHit, 5f)),
            Passive("momentum", 2, 3, 2, Links("charge"), TalentEffect.ForAbility(s["Charge"], AbilityParam.Cooldown, -2f)),
            Active("stone_skin", 2, 4, s["StoneSkin"], Links("toughness")),
            Passive("sturdy_mail", 2, 6, 3, Links("toughness"), Stat(StatType.Armor, 2f, ModifierKind.Percent)),
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
            Passive("weapon_master", 6, 1, 2, Links("heavy_step"), Stat(StatType.DamageBonus, 3f), Stat(StatType.ToHit, 3f)),
            Choice("warlord", 6, 3, Links("heavy_step", "recovery"),
                Option("furious_charge", TalentEffect.ForAbility(s["Charge"], AbilityParam.Radius, 3f)),
                Option("fathers_shout", TalentEffect.ForAbility(s["BattleShout"], AbilityParam.Heal, 15f))),
            Passive("iron_heart", 6, 5, 2, Links("recovery"), Stat(StatType.MaxLifePercent, 5f), Stat(StatType.Armor, 2f, ModifierKind.Percent)),
        };

        // Difesa: lo scudo, i blocchi e gli stordimenti
        private static TalentNode[] DefenseNodes(Dictionary<string, SpellDefinition> s) => new[]
        {
            Active("shield_bash", 0, 3, s["ShieldBash"]),
            Passive("heavy_shield", 1, 1, 3, Links("shield_bash"), Stat(StatType.BlockChance, 2f)),
            Active("shield_wall", 1, 3, s["ShieldWall"], Links("shield_bash")),
            Passive("thick_skin", 1, 5, 3, Links("shield_bash"), Stat(StatType.DamageReduction, 1f)),
            Passive("recoil", 2, 0, 2, Links("heavy_shield"), TalentEffect.OnTrigger(TalentTrigger.Block, TalentOutcome.Mana, 2f)),
            Passive("shoulder_charge", 2, 2, 2, Links("heavy_shield", "shield_wall"), TalentEffect.ForAbility(s["ShieldBash"], AbilityParam.Cooldown, -2f)),
            Passive("bulwark", 2, 4, 2, Links("shield_wall", "thick_skin"), TalentEffect.ForAbility(s["ShieldWall"], AbilityParam.Duration, 1.5f)),
            Passive("guardian_armor", 2, 6, 3, Links("thick_skin"), Stat(StatType.Armor, 1f, ModifierKind.Percent, TalentCondition.Shield)),
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
            Passive("weak_spot", 1, 5, 3, Links("whirlwind"), Stat(StatType.CritDamage, 5f)),
            Passive("circular_motion", 2, 0, 2, Links("two_handed_grip"), Stat(StatType.AttackSpeed, 5f)),
            Active("execute", 2, 2, s["Execute"], Links("two_handed_grip", "keen_edge")),
            Passive("vortex", 2, 4, 2, Links("keen_edge", "weak_spot"), TalentEffect.ForAbility(s["Whirlwind"], AbilityParam.Radius, 0.5f)),
            Passive("executioners_eye", 2, 6, 2, Links("weak_spot"), Stat(StatType.CritChance, 0.5f)),
            Passive("slaughter", 3, 1, 2, Links("circular_motion", "execute"), TalentEffect.OnTrigger(TalentTrigger.Kill, TalentOutcome.Mana, 3f)),
            Active("leap", 3, 3, s["Leap"], Links("execute", "vortex")),
            Passive("blood_on_the_blade", 3, 5, 2, Links("vortex", "executioners_eye"), Stat(StatType.LifeSteal, 2f)),
            Choice("fighting_style", 4, 2, Links("leap", "slaughter"),
                Option("headsman", TalentEffect.ForAbility(s["Execute"], AbilityParam.Threshold, 10f)),
                Option("endless_whirlwind", TalentEffect.ForAbility(s["Whirlwind"], AbilityParam.ManaCost, -4f))),
            Passive("frenzy", 4, 4, 2, Links("leap", "blood_on_the_blade"),
                TalentEffect.OnTrigger(TalentTrigger.Kill, TalentOutcome.Buff, 5f, StatType.AttackSpeed, 5f)),
            Passive("weapon_mastery", 5, 1, 3, Links("fighting_style"), Stat(StatType.DamageBonus, 3f)),
            Active("bladestorm", 5, 3, s["Bladestorm"], Links("fighting_style", "frenzy")),
        };

        private static AudioClip Sound(string path) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundsFolder}/{path}") ?? throw new InvalidOperationException($"manca il suono {path}");

        internal static string[] Links(params string[] ids) => ids;

        internal static TalentEffect Stat(StatType stat, float perRank, ModifierKind modifier = ModifierKind.Flat, TalentCondition condition = TalentCondition.None) =>
            TalentEffect.ForStat(stat, perRank, modifier, condition);

        internal static TalentOption Option(string id, params TalentEffect[] effects) => new TalentOption("talent." + id, null, effects);

        internal static TalentNode Active(string id, int row, int column, SpellDefinition ability, string[] links = null) =>
            new TalentNode(id, row, column, 1, links, new TalentOption("talent." + id, ability ?? throw new ArgumentNullException(nameof(ability), id)));

        internal static TalentNode Passive(string id, int row, int column, int ranks, string[] links, params TalentEffect[] effects) =>
            new TalentNode(id, row, column, ranks, links, Option(id, effects));

        internal static TalentNode Choice(string id, int row, int column, string[] links, TalentOption first, TalentOption second) =>
            new TalentNode(id, row, column, 1, links, first, second);

        private static SpellDefinition SaveAbility(AbilityRow ability)
        {
            string path = $"{SpellsFolder}/{ability.Asset}.asset";
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

            so.FindProperty("_nameKey").stringValue = ability.Key;
            so.FindProperty("_kind").intValue = (int)ability.Kind;
            so.FindProperty("_manaCost").floatValue = ability.Mana;
            so.FindProperty("_cooldown").floatValue = ability.Cooldown;
            so.FindProperty("_radius").floatValue = ability.Radius;
            so.FindProperty("_duration").floatValue = ability.Duration;
            so.FindProperty("_range").floatValue = ability.Range;
            so.FindProperty("_weaponPercent").floatValue = ability.Weapon;
            so.FindProperty("_stun").floatValue = ability.Stun;
            so.FindProperty("_bonusPercent").floatValue = ability.Bonus;
            so.FindProperty("_threshold").floatValue = ability.Threshold;
            so.FindProperty("_minRange").floatValue = ability.MinRange;
            so.FindProperty("_moveFactor").floatValue = ability.MoveFactor;
            so.FindProperty("_tick").floatValue = ability.Tick;
            if (ability.Icon != null)
            {
                so.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(ability.Icon);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spell);
            return spell;
        }

        /// <summary>
        /// I gesti delle abilità nel controller del cavaliere (20.4): uno stato per gesto, con la clip di
        /// KayKit, che torna alla corsa come l'attacco. Gli stati che ci sono già si aggiornano.
        /// </summary>
        [MenuItem("DarkDescent/Talenti/Gesti delle abilità")]
        public static void InstallAbilityStates()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var machine = controller.layers[0].stateMachine;
            AnimatorState locomotion = null;
            foreach (var child in machine.states)
            {
                if (child.state.name == "Locomotion")
                {
                    locomotion = child.state;
                }
            }

            if (locomotion == null)
            {
                throw new InvalidOperationException($"{ControllerPath}: manca lo stato Locomotion");
            }

            foreach (var (stateName, file, clipName) in AbilityStates)
            {
                var clip = FindClip($"{AnimationsFolder}/{file}.fbx", clipName);
                AnimatorState state = null;
                foreach (var child in machine.states)
                {
                    if (child.state.name == stateName)
                    {
                        state = child.state;
                    }
                }

                if (state == null)
                {
                    state = machine.AddState(stateName);
                    var back = state.AddTransition(locomotion);
                    back.hasExitTime = true;
                    back.exitTime = 0.85f;
                    back.hasFixedDuration = true;
                    back.duration = 0.2f;
                }

                state.motion = clip;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static AnimationClip FindClip(string path, string ending)
        {
            var names = new List<string>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    if (clip.name.EndsWith(ending, StringComparison.Ordinal))
                    {
                        return clip;
                    }

                    names.Add(clip.name);
                }
            }

            throw new InvalidOperationException($"{path}: nessuna clip che finisce con {ending}; ci sono {string.Join(", ", names)}");
        }

        /// <summary>
        /// Gli effetti delle abilità (20.4), copie di quelli che il gioco ha già con un altro colore: il
        /// cerchio d'acciaio del Turbine e del Salto e l'onda d'oro del Grido dall'Anello di fuoco, le
        /// stelle dello stordimento dalle scintille dei campioni.
        /// </summary>
        private static (AreaEffect spin, AreaEffect shout, GameObject stun) InstallAbilityEffects()
        {
            var spin = AreaCopy("Fx_Spin", new Color(0.78f, 0.84f, 0.95f, 1f));
            var shout = AreaCopy("Fx_Shout", new Color(1f, 0.78f, 0.3f, 1f));
            string stunPath = $"{EffectsFolder}/Fx_Stun.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(stunPath) == null)
            {
                AssetDatabase.CopyAsset($"{EffectsFolder}/Fx_ChampionSparks.prefab", stunPath);
            }

            var root = PrefabUtility.LoadPrefabContents(stunPath);
            try
            {
                root.transform.localScale = Vector3.one * 0.5f;
                foreach (var particles in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particles.main;
                    main.startColor = new Color(1f, 0.9f, 0.35f, 1f);
                    main.loop = true;
                    main.playOnAwake = true;
                }

                PrefabUtility.SaveAsPrefabAsset(root, stunPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return (spin, shout, AssetDatabase.LoadAssetAtPath<GameObject>(stunPath));
        }

        private static AreaEffect AreaCopy(string name, Color color)
        {
            string path = $"{EffectsFolder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                AssetDatabase.CopyAsset("Assets/_Project/Prefabs/Magic/FireRing.prefab", path);
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var particles in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particles.main;
                    main.startColor = color;
                }

                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    light.color = color;
                }

                // il suono lo fa l'abilità
                var area = new SerializedObject(root.GetComponent<AreaEffect>());
                area.FindProperty("_clip").objectReferenceValue = null;
                area.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<AreaEffect>();
        }

        internal static TalentTree SaveTree(string asset, string id, bool specialization, TalentNode[] nodes, TalentGate[] gates)
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

        private static void InstallOnPlayer(TalentTree warrior, TalentTree defense, TalentTree arms, (AreaEffect spin, AreaEffect shout, GameObject stun) effects, SpellDefinition attack)
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

                // l'Attacco è il colpo dell'arma nella casella del sinistro (D18)
                var magic = new SerializedObject(player.GetComponent<PlayerMagic>());
                magic.FindProperty("_attackSpell").objectReferenceValue = attack;
                magic.ApplyModifiedPropertiesWithoutUndo();

                // le abilità: i nemici e l'altoparlante degli incantesimi, il fendente del cavaliere
                var abilities = player.GetComponent<WarriorAbilities>();
                if (abilities == null)
                {
                    abilities = player.AddComponent<WarriorAbilities>();
                }

                var spells = new SerializedObject(player.GetComponent<PlayerSpells>());
                var swings = new SerializedObject(player.GetComponent<CharacterAudio>()).FindProperty("_swingClips");
                var ability = new SerializedObject(abilities);
                ability.FindProperty("_targetLayers").intValue = spells.FindProperty("_targetLayers").intValue;
                ability.FindProperty("_audio").objectReferenceValue = spells.FindProperty("_audio").objectReferenceValue;
                ability.FindProperty("_swingClip").objectReferenceValue = swings.GetArrayElementAtIndex(0).objectReferenceValue;
                ability.FindProperty("_shoutClip").objectReferenceValue = Sound("Creatures/grunt_04.ogg");
                ability.FindProperty("_whirlClip").objectReferenceValue = Sound("Generated/ability_whirl.wav");
                ability.FindProperty("_stunClip").objectReferenceValue = Sound("Generated/ability_stun.wav");
                ability.FindProperty("_shieldClip").objectReferenceValue = Sound("Generated/ability_shield.wav");
                ability.FindProperty("_landClip").objectReferenceValue = Sound("Generated/ability_land.wav");
                ability.FindProperty("_spinEffect").objectReferenceValue = effects.spin;
                ability.FindProperty("_shoutEffect").objectReferenceValue = effects.shout;
                ability.FindProperty("_stunEffect").objectReferenceValue = effects.stun;
                ability.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        // una riga della tabella delle abilità: i numeri che non servono restano a zero
        private sealed class AbilityRow
        {
            public AbilityRow(string asset, string key, SpellKind kind, float mana, float cooldown)
            {
                Asset = asset;
                Key = key;
                Kind = kind;
                Mana = mana;
                Cooldown = cooldown;
            }

            public string Asset { get; }
            public string Key { get; }
            public SpellKind Kind { get; }
            public float Mana { get; }
            public float Cooldown { get; }
            public float Radius { get; set; }
            public float Duration { get; set; }
            public float Range { get; set; }
            public float Weapon { get; set; }
            public float Stun { get; set; }
            public float Bonus { get; set; }
            public float Threshold { get; set; }
            public float MinRange { get; set; }
            public float MoveFactor { get; set; } = 1f;
            public float Tick { get; set; }
            public string Icon { get; set; }
        }
    }
}
