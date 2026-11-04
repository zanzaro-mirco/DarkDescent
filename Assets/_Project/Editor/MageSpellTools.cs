using System;
using DarkDescent.Classes;
using DarkDescent.Combat;
using DarkDescent.Magic;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Le abilità del Mago (D3 della M21, passo 21.4): gli effetti nuovi in <c>Prefabs/Magic</c> (copie tinte di quelli
    /// del Dardo e dell'Anello di fuoco), le dodici abilità in <c>Data/Spells</c> (il Dardo arcano e le undici, con il
    /// Dardo e l'Anello di fuoco portati ai numeri del disegno), <see cref="MageAbilities"/> sul giocatore e il Dardo
    /// arcano come colpo del sinistro del Mago. Si rilancia: gli asset che ci sono già si aggiornano.
    /// </summary>
    public static class MageSpellTools
    {
        private const string SpellsFolder = "Assets/_Project/Data/Spells";
        private const string MagicFolder = "Assets/_Project/Prefabs/Magic";
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Player.prefab";
        private const string SoundsFolder = "Assets/_Project/Audio/SFX/Generated";
        private const string IconsFolder = "Assets/_Project/Art/Icons/Spells";

        private static readonly Color Violet = new Color(0.72f, 0.5f, 1f, 1f);
        private static readonly Color Ice = new Color(0.5f, 0.85f, 1f, 1f);
        private static readonly Color Snow = new Color(0.85f, 0.95f, 1f, 1f);
        private static readonly Color Mana = new Color(0.35f, 0.55f, 1f, 1f);

        // nome dell'asset, chiave, tipo, mana, ricarica, effetto, icona provvisoria (le definitive sono del 21.7)
        private static readonly Row[] Rows =
        {
            new Row("ArcaneBolt", "spell.arcane_bolt", SpellKind.ArcaneBolt, 0f, 0f, "ArcaneBolt", "Firebolt") { Divisor = 11f, Speed = 16f, Range = 9f, Delay = 0.25f, Type = DamageType.Magic },
            new Row("Firebolt", "spell.firebolt", SpellKind.Projectile, 6f, 2f, null, null) { Min = 6, Max = 12, Divisor = 4f, Speed = 14f, Range = 14f },
            new Row("FireRing", "spell.fire_ring", SpellKind.Area, 12f, 8f, null, null) { Min = 7, Max = 14, Divisor = 3.3f, Radius = 3f },
            new Row("Blink", "spell.blink", SpellKind.Teleport, 8f, 12f, "Blink", "FireRing") { Range = 8f },
            new Row("ManaShield", "spell.mana_shield", SpellKind.ManaShield, 10f, 40f, null, "StoneSkin") { Duration = 12f, ManaPerDamage = 2f },
            new Row("Fireball", "spell.fireball", SpellKind.Projectile, 14f, 6f, "Fireball", "Firebolt") { Min = 12, Max = 24, Divisor = 2f, Speed = 9f, Range = 12f, Blast = 3f },
            new Row("FlameWall", "spell.flame_wall", SpellKind.Zone, 16f, 20f, null, "FireRing") { Min = 2, Max = 5, Divisor = 10f, Range = 12f, Duration = 6f, Tick = 0.5f, Length = 5f },
            new Row("Meteor", "spell.meteor", SpellKind.Meteor, 30f, 45f, "MeteorFall", "FireRing") { Min = 36, Max = 72, Divisor = 0.7f, Range = 14f, Radius = 4f, Delay = 1.2f },
            new Row("IceBolt", "spell.ice_bolt", SpellKind.Projectile, 8f, 3f, "FrostBolt", "Firebolt") { Min = 6, Max = 12, Divisor = 4f, Speed = 14f, Range = 14f, SlowPercent = 40f, SlowSeconds = 3f, Type = DamageType.Frost },
            new Row("FrostNova", "spell.frost_nova", SpellKind.Area, 12f, 14f, "FrostNova", "FireRing") { Radius = 3.5f, Freeze = 1.5f, SlowPercent = 40f, Type = DamageType.Frost },
            new Row("FrostArmor", "spell.frost_armor", SpellKind.Buff, 12f, 35f, "FrostArmor", "StoneSkin") { ArmorPercent = 40, Duration = 15f, SlowPercent = 30f, SlowSeconds = 3f, Type = DamageType.Frost },
            new Row("Blizzard", "spell.blizzard", SpellKind.Zone, 28f, 60f, null, "FireRing") { Min = 2, Max = 5, Divisor = 10f, Range = 14f, Radius = 5f, Duration = 6f, Tick = 0.5f, SlowPercent = 50f, SlowSeconds = 1f, Type = DamageType.Frost },
        };

        [MenuItem("DarkDescent/Classi/Abilità del Mago")]
        public static void Build()
        {
            var fx = new Effects
            {
                Arcane = Projectile("ArcaneBolt", Violet, 1f, DamageType.Magic),
                FrostBolt = Projectile("FrostBolt", Ice, 1f, DamageType.Frost),
                Fireball = Projectile("Fireball", null, 1.8f, DamageType.Fire),
                MeteorFall = Projectile("MeteorFall", new Color(1f, 0.5f, 0.15f, 1f), 3f, DamageType.Fire),
                Blink = Area("Blink", Violet, keepSound: false),
                FrostNova = Area("FrostNova", Ice, keepSound: true),
                MeteorImpact = Area("MeteorImpact", null, keepSound: true),
                FlameWall = Area("FlameWall", null, keepSound: false),
                Snowfall = Area("Snowfall", Snow, keepSound: false),
                ManaShield = Aura("ManaShield", Mana),
                FrostArmor = Aura("FrostArmor", Ice),
            };

            foreach (var row in Rows)
            {
                Save(row, fx);
            }

            AssetDatabase.SaveAssets();
            InstallOnPlayer(fx);
            SetMageAttack();
            AssetDatabase.SaveAssets();
            Debug.Log($"[MageSpellTools] {Rows.Length} abilità del Mago");
        }

        // ---- Effetti -----------------------------------------------------------------------------------------------

        // un proiettile: copia del Dardo di fuoco, tinta e della dimensione data
        private static GameObject Projectile(string name, Color? color, float scale, DamageType type)
        {
            string path = $"{MagicFolder}/{name}.prefab";
            Copy($"{MagicFolder}/Firebolt.prefab", path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.transform.localScale = Vector3.one * scale;
                if (color.HasValue)
                {
                    Tint(root, color.Value);
                }

                var so = new SerializedObject(root.GetComponent<Projectile>());
                so.FindProperty("_damageType").intValue = (int)type;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // un'area: copia dell'Anello di fuoco; le aree che si ripetono (il Muro, la neve) non rifanno il suono a ogni battito
        private static AreaEffect Area(string name, Color? color, bool keepSound)
        {
            string path = $"{MagicFolder}/{name}.prefab";
            Copy($"{MagicFolder}/FireRing.prefab", path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (color.HasValue)
                {
                    Tint(root, color.Value);
                }

                if (!keepSound)
                {
                    var area = new SerializedObject(root.GetComponent<AreaEffect>());
                    area.FindProperty("_clip").objectReferenceValue = null;
                    area.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<AreaEffect>();
        }

        // un alone: copia della Pelle di pietra, tinto
        private static GameObject Aura(string name, Color color)
        {
            string path = $"{MagicFolder}/{name}.prefab";
            Copy($"{MagicFolder}/StoneSkin.prefab", path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Tint(root, color);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void Copy(string from, string to)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(to) == null && !AssetDatabase.CopyAsset(from, to))
            {
                throw new InvalidOperationException($"non si copia {from} in {to}");
            }
        }

        private static void Tint(GameObject root, Color color)
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
        }

        // ---- Abilità -----------------------------------------------------------------------------------------------

        private static void Save(Row row, Effects fx)
        {
            string path = $"{SpellsFolder}/{row.Asset}.asset";
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

            so.FindProperty("_nameKey").stringValue = row.Key;
            so.FindProperty("_kind").intValue = (int)row.Kind;
            so.FindProperty("_manaCost").floatValue = row.ManaCost;
            so.FindProperty("_cooldown").floatValue = row.Cooldown;
            so.FindProperty("_minDamage").intValue = row.Min;
            so.FindProperty("_maxDamage").intValue = row.Max;
            so.FindProperty("_magicDivisor").floatValue = row.Divisor;
            so.FindProperty("_speed").floatValue = row.Speed;
            so.FindProperty("_range").floatValue = row.Range;
            so.FindProperty("_radius").floatValue = row.Radius;
            so.FindProperty("_armorPercent").intValue = row.ArmorPercent;
            so.FindProperty("_duration").floatValue = row.Duration;
            so.FindProperty("_tick").floatValue = row.Tick;
            so.FindProperty("_weaponPercent").floatValue = 0f;
            so.FindProperty("_damageType").intValue = (int)row.Type;
            so.FindProperty("_blastRadius").floatValue = row.Blast;
            so.FindProperty("_slowPercent").floatValue = row.SlowPercent;
            so.FindProperty("_slowSeconds").floatValue = row.SlowSeconds;
            so.FindProperty("_freeze").floatValue = row.Freeze;
            so.FindProperty("_delay").floatValue = row.Delay;
            so.FindProperty("_length").floatValue = row.Length;
            so.FindProperty("_manaPerDamage").floatValue = row.ManaPerDamage;

            var effect = fx.For(row.Effect);
            if (effect != null)
            {
                so.FindProperty("_effect").objectReferenceValue = effect;
            }

            // l'icona che c'è si tiene; a quelle nuove ne va una provvisoria, finché non arrivano le definitive (21.7)
            if (row.Icon != null && so.FindProperty("_icon").objectReferenceValue == null)
            {
                so.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconsFolder}/{row.Icon}.png");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spell);
        }

        // ---- Giocatore ---------------------------------------------------------------------------------------------

        private static void InstallOnPlayer(Effects fx)
        {
            var player = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var abilities = player.GetComponent<MageAbilities>() ?? player.AddComponent<MageAbilities>();
                var spells = new SerializedObject(player.GetComponent<PlayerSpells>());
                var so = new SerializedObject(abilities);
                so.FindProperty("_targetLayers").intValue = spells.FindProperty("_targetLayers").intValue;
                so.FindProperty("_audio").objectReferenceValue = spells.FindProperty("_audio").objectReferenceValue;
                so.FindProperty("_blinkEffect").objectReferenceValue = fx.Blink;
                so.FindProperty("_shieldEffect").objectReferenceValue = fx.ManaShield;
                so.FindProperty("_meteorEffect").objectReferenceValue = fx.MeteorFall;
                so.FindProperty("_meteorImpact").objectReferenceValue = fx.MeteorImpact;
                so.FindProperty("_wallEffect").objectReferenceValue = fx.FlameWall;
                so.FindProperty("_snowEffect").objectReferenceValue = fx.Snowfall;
                so.FindProperty("_blinkClip").objectReferenceValue = Sound("ability_whirl.wav");
                so.FindProperty("_shieldClip").objectReferenceValue = Sound("ability_shield.wav");
                so.FindProperty("_meteorClip").objectReferenceValue = Sound("ability_land.wav");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        // il Dardo arcano è il colpo del sinistro del Mago, al posto dell'Attacco
        private static void SetMageAttack()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterClassCatalog>(ClassTools.CatalogPath);
            var mage = catalog.Find("mage");
            var so = new SerializedObject(mage);
            so.FindProperty("_attackSpell").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SpellDefinition>($"{SpellsFolder}/ArcaneBolt.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mage);
        }

        private static AudioClip Sound(string file) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundsFolder}/{file}") ?? throw new InvalidOperationException($"manca il suono {file}");

        private sealed class Effects
        {
            public GameObject Arcane;
            public GameObject FrostBolt;
            public GameObject Fireball;
            public GameObject MeteorFall;
            public AreaEffect Blink;
            public AreaEffect FrostNova;
            public AreaEffect MeteorImpact;
            public AreaEffect FlameWall;
            public AreaEffect Snowfall;
            public GameObject ManaShield;
            public GameObject FrostArmor;

            // l'effetto di un'abilità dal suo nome: null per quelle che tengono il loro
            public UnityEngine.Object For(string name)
            {
                switch (name)
                {
                    case "ArcaneBolt": return Arcane;
                    case "FrostBolt": return FrostBolt;
                    case "Fireball": return Fireball;
                    case "MeteorFall": return MeteorFall;
                    case "Blink": return Blink != null ? Blink.gameObject : null;
                    case "FrostNova": return FrostNova != null ? FrostNova.gameObject : null;
                    case "FrostArmor": return FrostArmor;
                    default: return null;
                }
            }
        }

        private sealed class Row
        {
            public Row(string asset, string key, SpellKind kind, float mana, float cooldown, string effect, string icon)
            {
                Asset = asset;
                Key = key;
                Kind = kind;
                ManaCost = mana;
                Cooldown = cooldown;
                Effect = effect;
                Icon = icon;
            }

            public string Asset { get; }
            public string Key { get; }
            public SpellKind Kind { get; }
            public float ManaCost { get; }
            public float Cooldown { get; }
            public string Effect { get; }
            public string Icon { get; }
            public int Min { get; set; }
            public int Max { get; set; }
            public float Divisor { get; set; } = 4f;
            public float Speed { get; set; } = 14f;
            public float Range { get; set; }
            public float Radius { get; set; }
            public int ArmorPercent { get; set; }
            public float Duration { get; set; }
            public float Tick { get; set; }
            public DamageType Type { get; set; } = DamageType.Fire;
            public float Blast { get; set; }
            public float SlowPercent { get; set; }
            public float SlowSeconds { get; set; }
            public float Freeze { get; set; }
            public float Delay { get; set; }
            public float Length { get; set; }
            public float ManaPerDamage { get; set; }
        }
    }
}
