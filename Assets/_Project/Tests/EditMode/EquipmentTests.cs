using System;
using System.Linq;
using DarkDescent.Items;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    public class EquipmentTests
    {
        private static readonly StatType[] AllStats = (StatType[])Enum.GetValues(typeof(StatType));

        private StatSheet _stats;
        private Equipment _equipment;

        private static T Load<T>(string name) where T : ItemDefinition
        {
            return AssetDatabase.LoadAssetAtPath<T>($"Assets/_Project/Data/Items/{name}.asset");
        }

        private static ItemInstance Sword => new ItemInstance(Load<WeaponDefinition>("ShortSword"));
        private static ItemInstance Blade => new ItemInstance(Load<WeaponDefinition>("SkeletonBlade"));
        private static ItemInstance Shield => new ItemInstance(Load<ArmorDefinition>("BadgeShield"));

        private static AffixDefinition Affix(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AffixDefinition>($"Assets/_Project/Data/Affixes/{name}.asset");
        }

        // uno scudo raro scritto a mano, con un affisso per tipo: dell'oggetto e del personaggio
        private static ItemInstance RareShield => new ItemInstance(Load<ArmorDefinition>("BadgeShield"), Rarity.Rare, 2, 1UL, new[]
        {
            new ItemAffix(Affix("Massive"), 50),
            new ItemAffix(Affix("Sturdy"), 3),
            new ItemAffix(Affix("OfStrength"), 4),
            new ItemAffix(Affix("OfTheBear"), 8),
        });

        private float[] Snapshot()
        {
            return AllStats.Select(stat => _stats.Get(stat)).ToArray();
        }

        [SetUp]
        public void SetUp()
        {
            // il cavaliere di D1
            _stats = new StatSheet();
            _stats.SetBase(StatType.Strength, 30f);
            _stats.SetBase(StatType.Dexterity, 20f);
            _stats.SetBase(StatType.Magic, 10f);
            _stats.SetBase(StatType.Vitality, 25f);
            _equipment = new Equipment(_stats);
        }

        [Test, Description("Equipaggiare e poi togliere riporta tutte le statistiche esattamente ai valori di partenza")]
        public void EquipThenUnequip_RestoresExactStats()
        {
            float[] before = Snapshot();

            Assert.IsTrue(_equipment.TryEquip(Shield, out _));
            Assert.IsTrue(_equipment.TryEquip(Blade, out _));
            Assert.AreEqual(5f, _stats.Get(StatType.Armor));

            _equipment.Unequip(EquipSlot.Offhand);
            _equipment.Unequip(EquipSlot.Weapon);

            CollectionAssert.AreEqual(before, Snapshot());
        }

        [Test, Description("Ogni oggetto va nel suo slot: la spada nell'arma, lo scudo nella mano sinistra")]
        public void Items_GoToTheirSlot()
        {
            var sword = Sword;
            var shield = Shield;

            _equipment.TryEquip(sword, out _);
            _equipment.TryEquip(shield, out _);

            Assert.AreSame(sword, _equipment.Get(EquipSlot.Weapon));
            Assert.AreSame(shield, _equipment.Get(EquipSlot.Offhand));
        }

        [Test, Description("Un'arma al posto di un'altra: la vecchia esce, con i suoi modificatori")]
        public void Equip_ReplacesAndReturnsPrevious()
        {
            var first = Shield;
            var second = Shield;
            _equipment.TryEquip(first, out _);

            Assert.IsTrue(_equipment.TryEquip(second, out var previous));

            Assert.AreSame(first, previous);
            Assert.AreSame(second, _equipment.Get(EquipSlot.Offhand));
            Assert.AreEqual(5f, _stats.Get(StatType.Armor), "l'armatura del primo scudo non resta");
        }

        [Test, Description("Senza la Forza richiesta l'arma viene rifiutata e niente cambia")]
        public void WeaponWithoutStrength_IsRefused()
        {
            _stats.SetBase(StatType.Strength, 20f);
            var sword = Sword;
            _equipment.TryEquip(sword, out _);
            int changes = 0;
            _equipment.Changed += slot => changes++;

            Assert.IsFalse(_equipment.TryEquip(Blade, out var previous));

            Assert.IsNull(previous);
            Assert.AreSame(sword, _equipment.Get(EquipSlot.Weapon));
            Assert.AreEqual(0, changes);
        }

        [Test, Description("Con la Forza giusta la stessa arma si equipaggia")]
        public void WeaponWithStrength_IsEquipped()
        {
            Assert.IsTrue(_equipment.MeetsRequirements(Blade.Definition));
            Assert.IsTrue(_equipment.TryEquip(Blade, out _));
        }

        [Test, Description("Changed dice quale slot è cambiato; svuotare uno slot vuoto non lo fa scattare")]
        public void Changed_ReportsSlot()
        {
            var changed = new System.Collections.Generic.List<EquipSlot>();
            _equipment.Changed += changed.Add;

            _equipment.TryEquip(Shield, out _);
            _equipment.Unequip(EquipSlot.Offhand);
            Assert.IsNull(_equipment.Unequip(EquipSlot.Weapon));

            CollectionAssert.AreEqual(new[] { EquipSlot.Offhand, EquipSlot.Offhand }, changed);
        }
    
        [Test, Description("Uno scudo con quattro affissi: Armatura 5 + 50% = 7, più 3; Forza +4; vita +8. Tolto, ogni statistica torna a prima")]
        public void AffixedItem_AddsAndRemovesEveryModifier()
        {
            float[] before = Snapshot();

            Assert.IsTrue(_equipment.TryEquip(RareShield, out _));

            Assert.AreEqual(10f, _stats.Get(StatType.Armor), "7 dello scudo con +50%, più 3 di Robusto");
            Assert.AreEqual(34f, _stats.Get(StatType.Strength));
            Assert.AreEqual(8f, _stats.Get(StatType.Life));

            _equipment.Unequip(EquipSlot.Offhand);
            CollectionAssert.AreEqual(before, Snapshot());
        }

        [Test, Description("Gli affissi dell'oggetto cambiano i suoi numeri: spada +40% 8–12, scudo +10 di blocco 20")]
        public void ItemAffixes_ChangeItemNumbers()
        {
            var sword = new ItemInstance(Load<WeaponDefinition>("ShortSword"), Rarity.Magic, 1, 1UL, new[] { new ItemAffix(Affix("Sharp"), 40) });
            var shield = new ItemInstance(Load<ArmorDefinition>("BadgeShield"), Rarity.Magic, 1, 1UL, new[] { new ItemAffix(Affix("OfBlocking"), 10) });

            Assert.AreEqual((8, 12), ItemStats.WeaponDamage(sword));
            Assert.AreEqual(20, ItemStats.ShieldBlock(shield));
            Assert.AreEqual(5, ItemStats.ItemArmor(shield));
            Assert.AreEqual((6, 9), ItemStats.WeaponDamage(Sword), "senza affissi");

            Assert.IsTrue(_equipment.TryEquip(sword, out _));
            Assert.AreEqual(0f, _stats.Get(StatType.Armor), "il danno dell'arma non passa dal personaggio");
        }
    }
}
