using System.Linq;
using System.Text;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le armi del Mago (D4 e D12 della M21, passo 21.3): bastone, bacchetta e tomo chiedono Magia, il
    /// tomo sta nella mano libera ma non è uno scudo, gli affissi della magia ci cadono e quelli del blocco no,
    /// e li vende il mercante, non il fabbro.
    /// </summary>
    public class MageWeaponTests
    {
        private static ItemInstance Item(string name)
        {
            return new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));
        }

        private static StatSheet Sheet(float strength, float magic)
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, strength);
            sheet.SetBase(StatType.Magic, magic);
            return sheet;
        }

        private static string Describe(ItemInstance item, Localizer localizer, bool meetsRequirements)
        {
            var builder = new StringBuilder();
            ItemDescription.Write(builder, item, meetsRequirements, localizer);
            return builder.ToString();
        }

        private static AffixDefinition[] Affixes()
        {
            return AssetDatabase.FindAssets("t:AffixDefinition", new[] { "Assets/_Project/Data/Affixes" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<AffixDefinition>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
        }

        [Test, Description("I numeri dei tre asset: il bastone a due mani con più danno, la bacchetta a una mano, il tomo nella mano libera che non ripara e non blocca; tutti chiedono Magia e non Forza, con modello, icona e posto nel database")]
        public void Assets_AreWhatTheDesignSays()
        {
            var staff = Item("Staff").Definition as WeaponDefinition;
            var wand = Item("Wand").Definition as WeaponDefinition;
            var tome = Item("Tome").Definition as ArmorDefinition;
            Assert.AreEqual((WeaponKind.Staff, true, 40, 0), (staff.Kind, staff.TwoHanded, staff.RequiredMagic, staff.RequiredStrength));
            Assert.AreEqual((WeaponKind.Wand, false, 20, 0), (wand.Kind, wand.TwoHanded, wand.RequiredMagic, wand.RequiredStrength));
            Assert.Greater(staff.MaxDamage, wand.MaxDamage);
            Assert.Greater(staff.AttackInterval, wand.AttackInterval);
            Assert.IsTrue(staff.IsCaster && wand.IsCaster);

            Assert.AreEqual(EquipSlot.Offhand, tome.Slot);
            Assert.IsTrue(tome.IsTome);
            Assert.IsFalse(tome.IsShield);
            Assert.AreEqual((0, 0, 30), (tome.Armor, tome.BlockChance, tome.RequiredMagic));
            Assert.AreEqual((5, 20), (tome.BonusMagic, tome.BonusMana));

            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            foreach (ItemDefinition item in new ItemDefinition[] { staff, wand, tome })
            {
                Assert.IsNotNull(item.Model, item.name);
                Assert.IsNotNull(item.Icon, item.name);
                Assert.IsTrue(database.TryGet(item.Id, out _), item.name);
            }

            foreach (var weapon in new[] { staff, wand })
            {
                Assert.LessOrEqual(weapon.HitDelay, weapon.AttackInterval, weapon.name);
            }
        }

        [Test, Description("Il requisito di Magia: il Guerriero (Magia 10) non porta niente, il Mago che parte (Magia 35) porta la bacchetta e il tomo ma non il bastone, finché la Magia non arriva a 40; i vecchi requisiti di Forza restano")]
        public void Equipment_ChecksMagic()
        {
            var warrior = new Equipment(Sheet(30f, 10f));
            var mage = new Equipment(Sheet(8f, 35f));
            foreach (string name in new[] { "Staff", "Wand", "Tome" })
            {
                Assert.IsFalse(warrior.MeetsRequirements(Item(name).Definition), $"Guerriero: {name}");
            }

            Assert.IsTrue(mage.MeetsRequirements(Item("Wand").Definition));
            Assert.IsTrue(mage.MeetsRequirements(Item("Tome").Definition));
            Assert.IsFalse(mage.MeetsRequirements(Item("Staff").Definition));
            Assert.IsFalse(mage.TryEquip(Item("Staff"), out _));

            var senior = new Equipment(Sheet(8f, 40f));
            Assert.IsTrue(senior.TryEquip(Item("Staff"), out _));
            Assert.IsTrue(senior.IsTwoHanded);

            // la Forza conta ancora: il Mago non porta lo spadone
            Assert.IsFalse(mage.MeetsRequirements(Item("Greatsword").Definition));
            Assert.IsTrue(warrior.MeetsRequirements(Item("ShortSword").Definition));
        }

        [Test, Description("Il tomo dà Magia e mana da sé e li toglie quando esce; il bastone a due mani non sta con il tomo, la bacchetta sì")]
        public void Tome_GivesMagicAndManaAndFollowsTheHandRule()
        {
            var sheet = Sheet(8f, 35f);
            var equipment = new Equipment(sheet);
            var tome = Item("Tome");
            Assert.IsTrue(equipment.TryEquip(tome, out _));
            Assert.AreEqual(40f, sheet.Get(StatType.Magic));
            Assert.AreEqual(20f, sheet.Get(StatType.Mana));
            Assert.AreEqual(0f, sheet.Get(StatType.Armor), "non ripara");

            Assert.IsTrue(equipment.TryEquip(Item("Wand"), out _), "bacchetta e tomo insieme");

            // con Magia 40 il bastone ci sarebbe, ma ha le due mani: prima il tomo esce
            Assert.IsNotNull(equipment.Blocking(Item("Staff").Definition, EquipSlot.Weapon));
            equipment.Unequip(EquipSlot.Weapon);
            Assert.IsFalse(equipment.TryEquip(Item("Staff"), out _), "il tomo è in mano");
            Assert.AreSame(tome, equipment.Blocking(Item("Staff").Definition, EquipSlot.Weapon));

            equipment.Unequip(EquipSlot.Offhand);
            Assert.AreEqual(35f, sheet.Get(StatType.Magic));
            Assert.AreEqual(0f, sheet.Get(StatType.Mana));
        }

        [Test, Description("Gli affissi: Magia e mana cadono su bastone, bacchetta e tomo; blocco e Armatura dello scudo sul tomo no, e sulle armi del Mago ci cadono quelli delle armi ma non quelli degli scudi")]
        public void Affixes_FitCasterItems()
        {
            var staff = Item("Staff").Definition;
            var wand = Item("Wand").Definition;
            var tome = Item("Tome").Definition;
            var affixes = Affixes();
            foreach (var item in new[] { staff, wand, tome })
            {
                Assert.IsTrue(affixes.Any(a => a.Effect == AffixEffect.Magic && a.AllowedOn(item, 40)), $"{item.name}: Magia");
                Assert.IsTrue(affixes.Any(a => a.Effect == AffixEffect.Mana && a.AllowedOn(item, 40)), $"{item.name}: mana");
            }

            Assert.IsFalse(affixes.Any(a => (a.Effect == AffixEffect.BlockChance || a.Effect == AffixEffect.ArmorPercent) && a.AllowedOn(tome, 40)), "il tomo non è uno scudo");
            Assert.IsFalse(affixes.Any(a => a.Effect == AffixEffect.WeaponDamagePercent && a.AllowedOn(tome, 40)), "e non è un'arma");
            Assert.IsTrue(affixes.Any(a => a.Effect == AffixEffect.WeaponDamagePercent && a.AllowedOn(staff, 40)), "il bastone è un'arma");
            Assert.IsFalse(affixes.Any(a => a.Effect == AffixEffect.BlockChance && a.AllowedOn(staff, 40)));

            // le armi del Guerriero non cambiano: i suoi affissi di prima, senza Magia
            var sword = Item("Greatsword").Definition;
            Assert.IsFalse(affixes.Any(a => a.Effect == AffixEffect.Magic && a.AllowedOn(sword, 40)), "gli affissi della Magia non sullo spadone");
        }

        [Test, Description("Bastone, bacchetta e tomo entrano nelle tabelle dei forzieri, dei bruti, del re e del mercante; li tratta il mercante e non il fabbro, e nessun banco li tratta due volte")]
        public void LootAndShops_HaveTheMageItems()
        {
            var items = new[] { "Staff", "Wand", "Tome" }.Select(n => Item(n).Definition).ToArray();
            foreach (string name in new[] { "ChestLoot", "GoldChestLoot", "BruteLoot", "BossLoot" })
            {
                var table = AssetDatabase.LoadAssetAtPath<LootTable>($"Assets/_Project/Data/Loot/{name}.asset");
                foreach (var item in items)
                {
                    Assert.IsTrue(table.Entries.Any(entry => entry.Item == item), $"{name}: {item.name}");
                }
            }

            var merchant = AssetDatabase.LoadAssetAtPath<MerchantTable>("Assets/_Project/Data/Loot/MerchantTable.asset");
            foreach (var item in items)
            {
                Assert.IsTrue(merchant.Stock.Any(entry => entry.Item == item), item.name);
                Assert.AreEqual(ShopGoods.Arcane, ItemGoods.Of(item), item.name);
                Assert.IsTrue((ShopGoods.Jewelry | ShopGoods.Scrolls | ShopGoods.Arcane).Trades(item), item.name);
                Assert.IsFalse((ShopGoods.Weapons | ShopGoods.Armor).Trades(item), $"{item.name}: il fabbro no");
            }

            // il resto resta com'era: l'asta del fabbro è sua
            Assert.AreEqual(ShopGoods.Weapons, ItemGoods.Of(Item("Greatsword").Definition));
            Assert.AreEqual(ShopGoods.Armor, ItemGoods.Of(Item("RoundShield").Definition));
        }

        [Test, Description("Il tooltip del tomo dice Magia e mana e il requisito di Magia, senza Armatura né blocco; chi non ha la Magia lo vede in rosso; il bastone dice 'Magia richiesta'")]
        public void Tooltip_ShowsMagicRequirement()
        {
            var localizer = new Localizer(StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text));
            string tome = Describe(Item("Tome"), localizer, true);
            StringAssert.Contains("+5 Magic", tome);
            StringAssert.Contains("+20 mana", tome);
            StringAssert.Contains("Required Magic: 30", tome);
            StringAssert.DoesNotContain("Armor:", tome);
            StringAssert.DoesNotContain("Block", tome);

            string staff = Describe(Item("Staff"), localizer, false);
            StringAssert.Contains(ItemDescription.UnmetColor, staff);
            StringAssert.Contains("Required Magic: 40", staff);
            StringAssert.Contains("Two-handed", staff);
            StringAssert.DoesNotContain("Required Strength", staff);

            localizer.SetLanguage("it");
            StringAssert.Contains("Magia richiesta: 20", Describe(Item("Wand"), localizer, true));
        }
    }
}
