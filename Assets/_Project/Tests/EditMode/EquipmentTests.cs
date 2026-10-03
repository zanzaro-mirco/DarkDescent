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
    }
}
