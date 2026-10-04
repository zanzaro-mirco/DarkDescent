using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class EquipmentPlayModeTests : SandboxFixture
    {
        private PlayerInventory _inventory;
        private EquipmentVisuals _visuals;
        private MeleeAttack _attack;
        private CharacterStats _stats;

        private static T Load<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }

        private static ItemInstance Item<T>(string name) where T : ItemDefinition
        {
            return new ItemInstance(Load<T>($"Assets/_Project/Data/Items/{name}.asset"));
        }

        private IEnumerator LoadKnight()
        {
            yield return LoadSandbox();
            _inventory = Player.GetComponent<PlayerInventory>();
            _visuals = Player.GetComponent<EquipmentVisuals>();
            _attack = Player.GetComponent<MeleeAttack>();
            _stats = Player.GetComponent<CharacterStats>();
        }

        [UnityTest, Description("Il cavaliere parte con la spada corta: equipaggiata, usata da MeleeAttack e in mano destra")]
        public IEnumerator Knight_StartsWithShortSword()
        {
            yield return LoadKnight();

            var weapon = _inventory.Equipment.Get(EquipSlot.Weapon);
            Assert.IsNotNull(weapon);
            Assert.AreEqual("ShortSword", weapon.Definition.name);
            Assert.AreSame(weapon.Definition, _attack.Weapon);
            Assert.IsNotNull(_visuals.MainHandModel);
            Assert.AreEqual("handslot.r", _visuals.MainHandModel.transform.parent.name);
            Assert.AreEqual("sword_1handed", _visuals.MainHandModel.name);
        }

        [UnityTest, Description("Cambiando arma, MeleeAttack usa quella nuova e in mano c'è il suo modello, nel rendering layer del cavaliere")]
        public IEnumerator EquipBlade_ChangesWeaponAndModel()
        {
            yield return LoadKnight();
            var blade = Item<WeaponDefinition>("SkeletonBlade");

            Assert.IsTrue(_inventory.Equipment.TryEquip(blade, out var previous));
            yield return null;

            Assert.AreEqual("ShortSword", previous.Definition.name);
            Assert.AreSame(blade.Definition, _attack.Weapon);
            Assert.AreEqual("Skeleton_Blade", _visuals.MainHandModel.name);
            Assert.AreEqual(1, _visuals.MainHandModel.transform.parent.childCount, "il modello della spada vecchia è sparito");

            uint playerLayer = RenderingLayerMask.GetMask("Player");
            foreach (var renderer in _visuals.MainHandModel.GetComponentsInChildren<Renderer>())
            {
                Assert.AreNotEqual(0u, renderer.renderingLayerMask & playerLayer, "la luce di riempimento deve illuminare anche l'arma");
            }
        }

        [UnityTest, Description("Lo scudo dà Armatura e compare in mano sinistra; tolto, tutto torna com'era")]
        public IEnumerator EquipShield_AddsArmorAndModel()
        {
            yield return LoadKnight();

            Assert.IsTrue(_inventory.Equipment.TryEquip(Item<ArmorDefinition>("BadgeShield"), out _));
            yield return null;
            Assert.AreEqual(5f, _stats.Armor);
            Assert.IsNotNull(_visuals.OffHandModel);
            Transform hand = _visuals.OffHandModel.transform.parent;
            Assert.AreEqual("handslot.l", hand.name);

            // lo scudo sta fuori dal braccio, non tra il braccio e il corpo: il suo centro è più a
            // sinistra della mano (la sinistra del cavaliere), di almeno 10 cm
            Bounds shieldBounds = _visuals.OffHandModel.GetComponentInChildren<Renderer>().bounds;
            float outward = Vector3.Dot(shieldBounds.center - hand.position, -Player.right);
            Assert.Greater(outward, 0.1f, $"lo scudo deve coprire la mano da fuori, è a {outward:F2} m");

            _inventory.Equipment.Unequip(EquipSlot.Offhand);
            yield return null;
            Assert.AreEqual(0f, _stats.Armor);
            Assert.IsNull(_visuals.OffHandModel);
        }

        [UnityTest, Description("Senza arma il cavaliere combatte con i pugni, e la mano destra è vuota")]
        public IEnumerator Unequip_Weapon_UsesFists()
        {
            yield return LoadKnight();

            _inventory.Equipment.Unequip(EquipSlot.Weapon);
            yield return null;

            Assert.AreSame(_inventory.Unarmed, _attack.Weapon);
            Assert.AreEqual((1, 3), (_attack.Weapon.MinDamage, _attack.Weapon.MaxDamage));
            Assert.IsNull(_visuals.MainHandModel);
        }
    
        private static AffixDefinition Affix(string name)
        {
            return Load<AffixDefinition>($"Assets/_Project/Data/Affixes/{name}.asset");
        }

        [UnityTest, Description("Uno scudo della Vitalità e dell'Orso: vita da 100 a 120, sfera e pannello compresi; tolto, torna 100")]
        public IEnumerator LifeAffixes_ChangeMaxLife_AndBack()
        {
            yield return LoadKnight();
            var health = Player.GetComponent<Health>();
            var character = Object.FindFirstObjectByType<CharacterPanel>();
            var shield = new ItemInstance(Load<ArmorDefinition>("Assets/_Project/Data/Items/BadgeShield.asset"), Rarity.Magic, 1, 1UL, new[]
            {
                new ItemAffix(Affix("OfVitality"), 5),
                new ItemAffix(Affix("OfTheBear"), 10),
            });

            Assert.IsTrue(_inventory.Equipment.TryEquip(shield, out _));
            yield return null;

            Assert.AreEqual(120f, health.Max, "50 + 2 × 30 + 10");
            Assert.AreEqual(120f, health.Current, "indossarlo non è una cura, ma la vita sale con il massimo");
            StringAssert.Contains("\n120\n", character.ValuesText);

            health.TakeDamage(new DamageInfo(30f, DamageType.Physical, null));
            _inventory.Equipment.Unequip(EquipSlot.Offhand);
            yield return null;

            Assert.AreEqual(100f, health.Max);
            Assert.AreEqual(70f, health.Current, "scende della stessa quantità");
        }

        [UnityTest, Description("Una spada corta Affilata +40%: MeleeAttack tira 8–12, e il pannello mostra 10–16 con la Forza")]
        public IEnumerator SharpSword_RaisesAttackDamage()
        {
            yield return LoadKnight();
            var character = Object.FindFirstObjectByType<CharacterPanel>();
            var sword = new ItemInstance(Load<WeaponDefinition>("Assets/_Project/Data/Items/ShortSword.asset"), Rarity.Magic, 1, 1UL, new[]
            {
                new ItemAffix(Affix("Sharp"), 40),
            });

            Assert.IsTrue(_inventory.Equipment.TryEquip(sword, out _));
            yield return null;

            Assert.AreEqual((8, 12), (_attack.MinDamage, _attack.MaxDamage));
            Assert.AreEqual((10, 16), character.DamageRange());
        }
    }
}
