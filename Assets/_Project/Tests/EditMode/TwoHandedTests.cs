using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Progression;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Le armi a due mani (D5 della M20): lo spadone e l'ascia, la regola dello slot con lo scudo, la borsa, il loot e il mercante.</summary>
    public class TwoHandedTests
    {
        private static ItemInstance Item(string name)
        {
            return new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));
        }

        private static Inventory NewInventory(float strength = 60f)
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, strength);
            return new Inventory(new InventoryGrid(10, 4), new Equipment(sheet));
        }

        [Test, Description("I numeri dei due asset: a due mani, più danno e più lenti di quelle a una mano, con la Forza che chiedono; modello, icona e posto nel database")]
        public void Assets_AreTwoHandedAndHeavier()
        {
            var sword = Item("Greatsword").Definition as WeaponDefinition;
            var axe = Item("GreatAxe").Definition as WeaponDefinition;
            var oneHandedAxe = Item("Axe").Definition as WeaponDefinition;
            Assert.IsTrue(sword.TwoHanded && axe.TwoHanded);
            Assert.IsFalse(oneHandedAxe.TwoHanded);
            Assert.AreEqual((14, 22, 40, 1.5f, 0.9f), (sword.MinDamage, sword.MaxDamage, sword.RequiredStrength, sword.AttackInterval, sword.HitDelay));
            Assert.AreEqual((18, 28, 50, 1.75f, 1.05f), (axe.MinDamage, axe.MaxDamage, axe.RequiredStrength, axe.AttackInterval, axe.HitDelay));
            Assert.Greater(sword.MaxDamage, oneHandedAxe.MaxDamage);
            Assert.Greater(sword.AttackInterval, oneHandedAxe.AttackInterval);
            foreach (var weapon in new[] { sword, axe })
            {
                Assert.IsNotNull(weapon.Model, weapon.name);
                Assert.IsNotNull(weapon.Icon, weapon.name);
                Assert.AreEqual(EquipSlot.Weapon, weapon.Slot);
                Assert.LessOrEqual(weapon.HitDelay, weapon.AttackInterval);
            }

            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            Assert.IsTrue(database.TryGet(sword.Id, out _));
            Assert.IsTrue(database.TryGet(axe.Id, out _));
        }

        [Test, Description("La regola dello slot: con lo scudo in mano l'arma a due mani non entra, e con lei lo scudo non entra; tolto chi sta in mezzo, entrano")]
        public void Equipment_RefusesTwoHandedWithAShield()
        {
            var inventory = NewInventory();
            var equipment = inventory.Equipment;
            var shield = Item("BadgeShield");
            var sword = Item("Greatsword");
            Assert.IsTrue(equipment.TryEquip(shield, out _));
            Assert.AreSame(shield, equipment.Blocking(sword.Definition, EquipSlot.Weapon));
            Assert.IsFalse(equipment.TryEquip(sword, out _));
            Assert.IsNull(equipment.Get(EquipSlot.Weapon));
            Assert.IsFalse(equipment.IsTwoHanded);

            equipment.Unequip(EquipSlot.Offhand);
            Assert.IsTrue(equipment.TryEquip(sword, out _));
            Assert.IsTrue(equipment.IsTwoHanded);
            Assert.AreSame(sword, equipment.Blocking(shield.Definition, EquipSlot.Offhand));
            Assert.IsFalse(equipment.TryEquip(shield, out _));

            // un'arma a una mano non toglie lo scudo
            equipment.Unequip(EquipSlot.Weapon);
            Assert.IsTrue(equipment.TryEquip(shield, out _));
            Assert.IsTrue(equipment.TryEquip(Item("ShortSword"), out _));
            Assert.IsNull(equipment.Blocking(Item("Axe").Definition, EquipSlot.Weapon));
        }

        [Test, Description("Click destro sullo spadone con scudo e spada indossati: spadone in mano, la spada torna nel suo posto e lo scudo in borsa; e rimessi, lo scudo toglie lo spadone")]
        public void EquipFromGrid_SendsTheShieldToTheBag()
        {
            var inventory = NewInventory();
            var sword = Item("Greatsword");
            var oldSword = Item("ShortSword");
            var shield = Item("BadgeShield");
            Assert.IsTrue(inventory.Equipment.TryEquip(oldSword, out _));
            Assert.IsTrue(inventory.Equipment.TryEquip(shield, out _));
            Assert.IsTrue(inventory.Grid.TryPlace(sword, new Vector2Int(0, 0)));
            int flashes = 0;
            inventory.Equipped += (slot, item) => flashes++;

            Assert.IsTrue(inventory.EquipFromGrid(new Vector2Int(0, 2)));
            Assert.AreSame(sword, inventory.Equipment.Get(EquipSlot.Weapon));
            Assert.IsNull(inventory.Equipment.Get(EquipSlot.Offhand));
            Assert.IsTrue(inventory.Grid.TryGetPlacement(oldSword, out _), "la spada di prima in borsa");
            Assert.IsTrue(inventory.Grid.TryGetPlacement(shield, out _), "lo scudo in borsa");
            Assert.AreEqual(1, flashes);

            Assert.IsTrue(shield.Definition.Slot == EquipSlot.Offhand);
            Assert.IsTrue(inventory.Grid.TryGetPlacement(shield, out RectInt area));
            Assert.IsTrue(inventory.EquipFromGrid(area.position));
            Assert.AreSame(shield, inventory.Equipment.Get(EquipSlot.Offhand));
            Assert.IsNull(inventory.Equipment.Get(EquipSlot.Weapon), "lo scudo ha tolto lo spadone");
            Assert.IsTrue(inventory.Grid.TryGetPlacement(sword, out _), "lo spadone in borsa");
        }

        [Test, Description("Con la borsa piena non c'è posto per lo scudo: l'arma a due mani non si indossa e non cambia niente")]
        public void EquipFromGrid_WithNoRoom_ChangesNothing()
        {
            var inventory = NewInventory();
            var sword = Item("Greatsword");
            var shield = Item("BadgeShield");
            Assert.IsTrue(inventory.Equipment.TryEquip(shield, out _));
            Assert.IsTrue(inventory.Grid.TryPlace(sword, new Vector2Int(0, 0)));

            // il resto della borsa pieno di scudi quadrati da 2 x 2, fino a non starci più niente
            var filler = Enumerable.Range(0, 40).Select(_ => Item("SquareShield")).ToList();
            foreach (var item in filler)
            {
                inventory.Grid.TryAutoPlace(item);
            }

            Assert.IsFalse(inventory.Grid.CanAutoPlace(shield), "la prova: lo scudo non ci sta");
            Assert.IsFalse(inventory.EquipFromGrid(new Vector2Int(0, 0)));
            Assert.AreSame(shield, inventory.Equipment.Get(EquipSlot.Offhand));
            Assert.IsNull(inventory.Equipment.Get(EquipSlot.Weapon));
            Assert.IsTrue(inventory.Grid.TryGetPlacement(sword, out RectInt area));
            Assert.AreEqual(Vector2Int.zero, area.position);
        }

        [Test, Description("Un click sullo slot dell'arma con lo spadone in mano toglie lo scudo e lo mette in borsa; uno scudo in mano sullo slot dello scudo toglie lo spadone")]
        public void ClickSlot_MakesRoomForTheOtherHand()
        {
            var inventory = NewInventory();
            var sword = Item("Greatsword");
            var shield = Item("BadgeShield");
            Assert.IsTrue(inventory.Equipment.TryEquip(shield, out _));
            Assert.IsTrue(inventory.Grid.TryPlace(sword, new Vector2Int(5, 0)));
            Assert.IsTrue(inventory.ClickCell(new Vector2Int(5, 0)));
            Assert.AreSame(sword, inventory.Held);

            Assert.IsTrue(inventory.ClickSlot(EquipSlot.Weapon));
            Assert.IsNull(inventory.Held);
            Assert.AreSame(sword, inventory.Equipment.Get(EquipSlot.Weapon));
            Assert.IsNull(inventory.Equipment.Get(EquipSlot.Offhand));
            Assert.IsTrue(inventory.Grid.TryGetPlacement(shield, out _));

            Assert.IsTrue(inventory.Grid.TryGetPlacement(shield, out RectInt area));
            Assert.IsTrue(inventory.ClickCell(area.position));
            Assert.AreSame(shield, inventory.Held);
            Assert.IsTrue(inventory.ClickSlot(EquipSlot.Offhand));
            Assert.AreSame(shield, inventory.Equipment.Get(EquipSlot.Offhand));
            Assert.IsNull(inventory.Equipment.Get(EquipSlot.Weapon));
            Assert.IsTrue(inventory.Grid.TryGetPlacement(sword, out _));
        }

        [Test, Description("Le due armi entrano nelle tabelle dei forzieri, dei bruti, del re e dei banchi del borgo, e solo il fabbro le vende")]
        public void LootAndMerchant_HaveTheTwoHandedWeapons()
        {
            var sword = Item("Greatsword").Definition;
            var axe = Item("GreatAxe").Definition;
            foreach (string name in new[] { "ChestLoot", "GoldChestLoot", "BruteLoot", "BossLoot" })
            {
                var table = AssetDatabase.LoadAssetAtPath<LootTable>($"Assets/_Project/Data/Loot/{name}.asset");
                Assert.IsTrue(table.Entries.Any(entry => entry.Item == sword), $"{name}: spadone");
                Assert.IsTrue(table.Entries.Any(entry => entry.Item == axe), $"{name}: ascia");
            }

            var merchant = AssetDatabase.LoadAssetAtPath<MerchantTable>("Assets/_Project/Data/Loot/MerchantTable.asset");
            Assert.IsTrue(merchant.Stock.Any(entry => entry.Item == sword) && merchant.Stock.Any(entry => entry.Item == axe));
            Assert.IsTrue(ShopGoods.Weapons.Trades(sword) && ShopGoods.Weapons.Trades(axe));
            Assert.IsFalse(ShopGoods.Jewelry.Trades(sword));
        }

        [Test, Description("La misura delle difficoltà resta con una mano e lo scudo: HeroSetup non prende le armi a due mani, anche se valgono di più")]
        public void HeroSetup_SkipsTwoHandedWeapons()
        {
            var inventory = NewInventory(80f);
            var bases = new[] { "ShortSword", "Axe", "Greatsword", "GreatAxe", "BadgeShield" }
                .Select(name => new LootEntry(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"), 1)).ToList();
            var generator = new ItemGenerator(new AffixDefinition[0], RarityTable.Default);
            HeroSetup.Equip(inventory.Equipment, bases, generator, 10, 1UL, Rarity.Normal);
            Assert.AreEqual("Axe", inventory.Equipment.Get(EquipSlot.Weapon).Definition.name);
            Assert.IsNotNull(inventory.Equipment.Get(EquipSlot.Offhand), "lo scudo c'è ancora");
        }
    }
}
