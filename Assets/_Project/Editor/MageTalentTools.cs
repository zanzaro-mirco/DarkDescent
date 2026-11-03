using System;
using System.Collections.Generic;
using DarkDescent.Classes;
using DarkDescent.Magic;
using DarkDescent.Stats;
using DarkDescent.Talents;
using UnityEditor;
using UnityEngine;
using static DarkDescent.Editor.TalentTools;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Costruisce gli alberi del Mago dal disegno approvato da Mirco (passo 21.1 della M21,
    /// <c>docs/milestones/M21_albero_Mago.md</c>): la classe, <c>Fuoco</c> e <c>Gelo</c>, in <c>Data/Talents</c>, e li
    /// dà alla classe <c>Mage</c>, che da qui si può scegliere. Le abilità sono quelle del 21.4. Da qui in poi la fonte
    /// dell'albero è questa tabella; il documento resta la fotografia del disegno. Si rifà con un comando, come quello
    /// del Guerriero: gli id degli asset che ci sono già non cambiano. Righe e colonne del disegno partono da 1, qui da 0.
    /// </summary>
    public static class MageTalentTools
    {
        public const string ClassPath = "Assets/_Project/Data/Classes/Mage.asset";

        [MenuItem("DarkDescent/Talenti/Costruisci il Mago")]
        public static void BuildMage()
        {
            var s = new Dictionary<string, SpellDefinition>();
            foreach (string name in new[] { "Firebolt", "FireRing", "Blink", "ManaShield", "Fireball", "FlameWall", "Meteor", "IceBolt", "FrostNova", "FrostArmor", "Blizzard" })
            {
                s[name] = AssetDatabase.LoadAssetAtPath<SpellDefinition>($"{SpellsFolder}/{name}.asset")
                    ?? throw new InvalidOperationException($"manca l'abilità {name}: prima il menu Classi → Abilità del Mago");
            }

            // il rallentamento della Nova c'è da subito (il 40%), ma dura zero secondi: lo allunga Gelo che resta
            var nova = new SerializedObject(s["FrostNova"]);
            nova.FindProperty("_slowPercent").floatValue = 40f;
            nova.FindProperty("_slowSeconds").floatValue = 0f;
            nova.ApplyModifiedPropertiesWithoutUndo();

            var mage = SaveTree("Mage", "mage", false, ClassNodes(s), new[] { new TalentGate(3, 6), new TalentGate(6, 14) });
            var fire = SaveTree("MageFire", "mage.fire", true, FireNodes(s), new[] { new TalentGate(3, 5), new TalentGate(5, 12) });
            var frost = SaveTree("MageFrost", "mage.frost", true, FrostNodes(s), new[] { new TalentGate(3, 5), new TalentGate(5, 12) });
            AssetDatabase.SaveAssets();
            GiveToClass(mage, fire, frost);
            Debug.Log($"[MageTalentTools] Mago: {mage.Nodes.Count} nodi, Fuoco {fire.Nodes.Count}, Gelo {frost.Nodes.Count}");
        }

        // l'albero della classe: il rogo a sinistra, il passo e lo scudo in mezzo, la riserva a destra
        private static TalentNode[] ClassNodes(Dictionary<string, SpellDefinition> s) => new[]
        {
            Active("firebolt", 0, 3, s["Firebolt"]),
            Passive("keen_mind", 1, 1, 3, Links("firebolt"), Stat(StatType.SpellDamage, 3f)),
            Active("fire_ring", 1, 3, s["FireRing"], Links("firebolt")),
            Passive("mana_reserve", 1, 5, 3, Links("firebolt"), Stat(StatType.MaxManaPercent, 5f)),
            Passive("swift_bolt", 2, 0, 2, Links("keen_mind"), TalentEffect.ForAbility(s["Firebolt"], AbilityParam.Cooldown, -0.5f)),
            Passive("wide_flame", 2, 2, 2, Links("keen_mind", "fire_ring"), TalentEffect.ForAbility(s["FireRing"], AbilityParam.Radius, 0.5f)),
            Active("blink", 2, 4, s["Blink"], Links("fire_ring", "mana_reserve")),
            Passive("ash_skin", 2, 6, 3, Links("mana_reserve"), Stat(StatType.MaxLifePercent, 4f)),
            Passive("concentration", 3, 1, 2, Links("swift_bolt", "wide_flame"), TalentEffect.OnTrigger(TalentTrigger.BoltHit, TalentOutcome.Mana, 1f)),
            Active("mana_shield", 3, 3, s["ManaShield"], Links("wide_flame", "blink")),
            Passive("cold_mind", 3, 5, 2, Links("blink", "ash_skin"), Stat(StatType.ManaRegenPercent, 50f)),
            Choice("fire_craft", 4, 1, Links("concentration"),
                Option("spark", TalentEffect.OnTrigger(TalentTrigger.BoltHit, TalentOutcome.Cast, 10f, ability: s["Firebolt"])),
                Option("blaze", TalentEffect.ForAbility(s["Firebolt"], AbilityParam.Burn, 3f))),
            Passive("lasting_shield", 4, 3, 2, Links("mana_shield"), TalentEffect.ForAbility(s["ManaShield"], AbilityParam.Duration, 3f)),
            Passive("last_spark", 4, 5, 1, Links("cold_mind"), Stat(StatType.DamageResist, 25f, condition: TalentCondition.LowLife)),
            Passive("quick_step", 5, 2, 2, Links("fire_craft", "lasting_shield"), TalentEffect.ForAbility(s["Blink"], AbilityParam.Cooldown, -3f)),
            Passive("restoration", 5, 4, 2, Links("lasting_shield", "last_spark"), Stat(StatType.PotionBonus, 15f)),
            Passive("fire_master", 6, 1, 2, Links("quick_step"), Stat(StatType.SpellDamage, 4f)),
            Choice("fire_finale", 6, 3, Links("quick_step", "restoration"),
                Option("spark_rain", TalentEffect.ForAbility(s["Firebolt"], AbilityParam.Radius, 2f)),
                Option("burning_ring", TalentEffect.ForAbility(s["FireRing"], AbilityParam.Burn, 3f))),
            Passive("arcane_heart", 6, 5, 2, Links("restoration"), Stat(StatType.MaxManaPercent, 5f), Stat(StatType.MaxLifePercent, 3f)),
        };

        // Fuoco: il danno e le aree
        private static TalentNode[] FireNodes(Dictionary<string, SpellDefinition> s) => new[]
        {
            Active("fireball", 0, 3, s["Fireball"]),
            Passive("inflamed_mind", 1, 1, 3, Links("fireball"), Stat(StatType.SpellDamage, 4f)),
            Active("flame_wall", 1, 3, s["FlameWall"], Links("fireball")),
            Passive("great_blaze", 1, 5, 3, Links("fireball"), TalentEffect.ForAbility(s["Fireball"], AbilityParam.Radius, 0.5f)),
            Passive("explosion", 2, 0, 2, Links("inflamed_mind"), TalentEffect.ForAbility(s["Fireball"], AbilityParam.Damage, 15f)),
            Passive("quick_hand", 2, 2, 2, Links("inflamed_mind", "flame_wall"), TalentEffect.ForAbility(s["Fireball"], AbilityParam.Cooldown, -1f)),
            Passive("lasting_flames", 2, 4, 2, Links("flame_wall", "great_blaze"), TalentEffect.ForAbility(s["FlameWall"], AbilityParam.Duration, 2f)),
            Passive("burning_magic", 2, 6, 2, Links("great_blaze"), Stat(StatType.Magic, 3f)),
            Passive("pyre", 3, 1, 2, Links("explosion", "quick_hand"), TalentEffect.OnTrigger(TalentTrigger.Kill, TalentOutcome.Mana, 3f)),
            Passive("live_embers", 3, 3, 2, Links("quick_hand", "lasting_flames"), TalentEffect.ForAbility(s["FlameWall"], AbilityParam.Damage, 20f)),
            Passive("ardor", 3, 5, 2, Links("lasting_flames", "burning_magic"),
                TalentEffect.OnTrigger(TalentTrigger.Kill, TalentOutcome.Buff, 5f, StatType.AttackSpeed, 5f)),
            Choice("fire_end", 4, 2, Links("pyre", "live_embers"),
                Option("double_meteor", TalentEffect.ForAbility(s["Meteor"], AbilityParam.Echo, 1f)),
                Option("fire_tide", TalentEffect.ForAbility(s["Fireball"], AbilityParam.Ground, 4f))),
            Passive("searing_bolt", 4, 4, 2, Links("live_embers", "ardor"), TalentEffect.ForAbility(s["Firebolt"], AbilityParam.Damage, 15f)),
            Passive("fire_mastery", 5, 1, 3, Links("fire_end"), Stat(StatType.SpellDamage, 3f)),
            Active("meteor", 5, 3, s["Meteor"], Links("fire_end", "searing_bolt")),
        };

        // Gelo: il controllo e la difesa
        private static TalentNode[] FrostNodes(Dictionary<string, SpellDefinition> s) => new[]
        {
            Active("ice_bolt", 0, 3, s["IceBolt"]),
            Passive("frozen_skin", 1, 1, 3, Links("ice_bolt"), Stat(StatType.Armor, 2f, ModifierKind.Percent)),
            Active("frost_nova", 1, 3, s["FrostNova"], Links("ice_bolt")),
            Passive("piercing_cold", 1, 5, 3, Links("ice_bolt"), TalentEffect.ForAbility(s["IceBolt"], AbilityParam.Slow, 1f)),
            Passive("shards", 2, 0, 2, Links("frozen_skin"), TalentEffect.ForAbility(s["IceBolt"], AbilityParam.Damage, 15f)),
            Passive("quick_frost", 2, 2, 2, Links("frozen_skin", "frost_nova"), TalentEffect.ForAbility(s["FrostNova"], AbilityParam.Cooldown, -2f)),
            Passive("deep_frost", 2, 4, 2, Links("frost_nova", "piercing_cold"), TalentEffect.ForAbility(s["FrostNova"], AbilityParam.Freeze, 0.5f)),
            Passive("cold_blood", 2, 6, 3, Links("piercing_cold"), Stat(StatType.MaxLifePercent, 4f)),
            Active("frost_armor", 3, 1, s["FrostArmor"], Links("shards", "quick_frost")),
            Passive("rime", 3, 3, 2, Links("quick_frost", "deep_frost"), TalentEffect.ForAbility(s["FrostNova"], AbilityParam.Radius, 0.5f)),
            Passive("ice_shelter", 3, 5, 2, Links("deep_frost", "cold_blood"), Stat(StatType.DamageResist, 4f)),
            Choice("frost_end", 4, 2, Links("frost_armor", "rime"),
                Option("lingering_frost", TalentEffect.ForAbility(s["FrostNova"], AbilityParam.Slow, 3f)),
                Option("double_nova", TalentEffect.ForAbility(s["FrostNova"], AbilityParam.Echo, 1f))),
            Passive("ice_mind", 4, 4, 2, Links("rime", "ice_shelter"), TalentEffect.OnTrigger(TalentTrigger.Freeze, TalentOutcome.Mana, 2f)),
            Active("blizzard", 5, 3, s["Blizzard"], Links("frost_end", "ice_mind")),
            Passive("ice_fortress", 5, 5, 1, Links("ice_mind"), TalentEffect.ForAbility(s["FrostArmor"], AbilityParam.Duration, 10f)),
        };

        // la classe prende i suoi alberi: da qui si sceglie nella creazione del personaggio
        private static void GiveToClass(TalentTree mage, TalentTree fire, TalentTree frost)
        {
            var characterClass = AssetDatabase.LoadAssetAtPath<CharacterClass>(ClassPath) ?? throw new InvalidOperationException($"manca {ClassPath}");
            var so = new SerializedObject(characterClass);
            so.FindProperty("_classTree").objectReferenceValue = mage;
            var specializations = so.FindProperty("_specializations");
            specializations.arraySize = 2;
            specializations.GetArrayElementAtIndex(0).objectReferenceValue = fire;
            specializations.GetArrayElementAtIndex(1).objectReferenceValue = frost;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(characterClass);
            AssetDatabase.SaveAssets();
        }
    }
}
