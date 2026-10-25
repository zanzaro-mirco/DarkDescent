using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Unici e set nel gioco (passo 16.3): loot, combattimento e bonus dei set sul cavaliere.</summary>
    public class NamedItemPlayModeTests : SandboxFixture
    {
        private PlayerInventory _inventory;
        private MeleeAttack _attack;
        private Health _health;
        private CharacterStats _stats;

        private static UniqueDefinition Named(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<UniqueDefinition>($"Assets/_Project/Data/Named/{path}.asset");
#else
            return null;
#endif
        }

        private IEnumerator LoadKnight(bool allSkeletons = false)
        {
            yield return LoadSandbox(allSkeletons);
            _inventory = Player.GetComponent<PlayerInventory>();
            _attack = Player.GetComponent<MeleeAttack>();
            _health = Player.GetComponent<Health>();
            _stats = Player.GetComponent<CharacterStats>();

            // abbastanza Forza per la lama dello scheletro e i pezzi di ferro del Custode
            _stats.Sheet.SetBase(StatType.Strength, 40f);
        }

        [UnityTest, Description("All'Ombra, con -namedchance, lo scheletro lascia un unico o un pezzo di set: nome e luce nel suo colore, luce più forte")]
        public IEnumerator Shadow_DropsANamedItem()
        {
            yield return LoadKnight(allSkeletons: true);
            var root = Object.FindFirstObjectByType<CompositionRoot>();
            root.Difficulty.Restore(1, 0, 0);
            root.UseNamedChance(100f);
            var skeleton = GameObject.Find("Skeleton");
            var expected = UseLootSeedWithDrop(skeleton.GetComponent<LootDrop>(), item => item.IsNamed);
            skeleton.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            yield return new WaitForSeconds(1f);

            var drop = Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Single(g => g.Item != null && g.Item.IsNamed);
            Assert.AreSame(expected.Named, drop.Item.Named);
            CollectionAssert.AreEqual(expected.NamedValues, drop.Item.NamedValues);
            string label = drop.GetComponent<Interactable>().GetLabel(Localizer);
            StringAssert.Contains(Localizer.Get(expected.Named.NameKey), label);
            StringAssert.Contains(RarityColors.TextHex(expected.Rarity), label);
            Assert.AreEqual(RarityColors.Light(expected.Rarity), drop.GlowColor);
            Assert.Greater(drop.GetComponentInChildren<Light>(true).intensity, 0f);
            root.Difficulty.Restore(0, -1, 0);
        }

        [UnityTest, Description("Con la Falce della Cripta il cavaliere ferito si cura di una parte del danno che fa")]
        public IEnumerator LifeSteal_HealsOnHit()
        {
            yield return LoadKnight();
            var reaper = ItemGenerator.GenerateNamed(Named("Uniques/CryptReaper"), 55, 7UL);
            Assert.IsTrue(_inventory.Equipment.TryEquip(reaper, out _));
            float steal = _stats.Sheet.Get(StatType.LifeSteal);
            Assert.Greater(steal, 0f);

            _health.TakeDamage(new DamageInfo(40f, DamageType.Physical, null));
            float healed = 0f;
            void OnHealed(float amount) => healed += amount;
            _health.Healed += OnHealed;
            UseRandom(0.0);

            var skeleton = GameObject.Find("Skeleton").GetComponent<Health>();
            ClickAt(skeleton.transform.position + Vector3.up);
            for (float waited = 0f; waited < 6f && skeleton.Current >= skeleton.Max; waited += Time.deltaTime)
            {
                yield return null;
            }

            _health.Healed -= OnHealed;
            float dealt = skeleton.Max - skeleton.Current;
            Assert.Greater(dealt, 0f, "il colpo deve arrivare");
            Assert.AreEqual(CombatFormulas.LifeStolen(dealt, steal), healed, 0.01f);
        }

        [UnityTest, Description("Zanna di Morvak: carica e intervallo del colpo si accorciano, e l'animazione la segue")]
        public IEnumerator AttackSpeed_ShortensTheSwing()
        {
            yield return LoadKnight();
            var fang = ItemGenerator.GenerateNamed(Named("Uniques/MorvaksFang"), 30, 3UL);
            Assert.IsTrue(_inventory.Equipment.TryEquip(fang, out _));
            yield return null;

            float speed = fang.NamedValues[0];
            Assert.AreEqual(speed, _stats.Sheet.Get(StatType.AttackSpeed));
            Assert.AreEqual(1f + speed / 100f, _attack.SpeedMultiplier, 1e-4f);
            Assert.AreEqual(_attack.Weapon.HitDelay / (1f + speed / 100f), _attack.SwingHitDelay, 1e-4f);
        }

        [UnityTest, Description("Il Guscio di Grumm toglie punti a ogni colpo preso, mai sotto 1")]
        public IEnumerator DamageReduction_LowersEveryHit()
        {
            yield return LoadKnight();
            var shell = ItemGenerator.GenerateNamed(Named("Uniques/GrummsShell"), 55, 5UL);
            Assert.IsTrue(_inventory.Equipment.TryEquip(shell, out _));
            float reduction = _stats.Sheet.Get(StatType.DamageReduction);
            Assert.Greater(reduction, 5f);

            float before = _health.Current;
            _health.TakeDamage(new DamageInfo(reduction + 4f, DamageType.Physical, null));
            Assert.AreEqual(before - 4f, _health.Current, 0.001f);
            _health.TakeDamage(new DamageInfo(2f, DamageType.Physical, null));
            Assert.AreEqual(before - 5f, _health.Current, 0.001f, "mai sotto 1");
        }

        [UnityTest, Description("Due pezzi del Custode indossati accendono il bonus di vita, e la vita massima del cavaliere sale")]
        public IEnumerator SetPieces_RaiseTheHerosLife()
        {
            yield return LoadKnight();
            float max = _health.Max;

            var boots = ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensBoots"), 10, 1UL);
            var gauntlets = ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensGauntlets"), 10, 2UL);
            Assert.IsTrue(_inventory.Equipment.TryEquip(boots, out _));
            Assert.AreEqual(max + boots.NamedValues[1], _health.Max, 0.001f, "un pezzo: solo la vita degli stivali");
            Assert.IsTrue(_inventory.Equipment.TryEquip(gauntlets, out _));
            Assert.AreEqual(max + boots.NamedValues[1] + 25f, _health.Max, 0.001f, "due pezzi: +25 vita");

            _inventory.Equipment.Unequip(EquipSlot.Gloves);
            Assert.AreEqual(max + boots.NamedValues[1], _health.Max, 0.001f);
            yield return null;
        }
    }
}
