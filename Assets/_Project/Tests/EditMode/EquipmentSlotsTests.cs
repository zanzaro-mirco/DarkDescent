using System.Collections.Generic;
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
    /// Gli slot della M8 (D4, D5): elmo, armatura, guanti, stivali, amuleto e due anelli. Le basi
    /// vere arrivano al passo 8.4: qui definizioni create nel test.
    /// </summary>
    public class EquipmentSlotsTests
    {
        private readonly List<Object> _created = new List<Object>();
        private StatSheet _stats;
        private Inventory _inventory;

        private Equipment Equipment => _inventory.Equipment;

        [SetUp]
        public void SetUp()
        {
            _stats = new StatSheet();
            _stats.SetBase(StatType.Strength, 30f);
            _inventory = new Inventory(new InventoryGrid(10, 4), new Equipment(_stats));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        private ItemInstance Armor(EquipSlot slot, int armor, int strength = 0)
        {
            var definition = ScriptableObject.CreateInstance<ArmorDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("_slot").enumValueIndex = (int)slot;
            so.FindProperty("_armor").intValue = armor;
            so.FindProperty("_blockChance").intValue = 20;
            so.FindProperty("_requiredStrength").intValue = strength;
            so.FindProperty("_nameKey").stringValue = "item.badge_shield";
            so.ApplyModifiedPropertiesWithoutUndo();
            _created.Add(definition);
            return new ItemInstance(definition);
        }

        private ItemInstance Jewel(EquipSlot slot)
        {
            var definition = ScriptableObject.CreateInstance<JewelryDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("_slot").enumValueIndex = (int)slot;
            so.ApplyModifiedPropertiesWithoutUndo();
            _created.Add(definition);
            return new ItemInstance(definition);
        }

        [Test, Description("Elmo, armatura, guanti e stivali danno la loro Armatura, che si somma; tolto un pezzo, torna giusta")]
        public void ArmorPieces_AddTheirArmor()
        {
            var helm = Armor(EquipSlot.Helm, 4);
            Assert.IsTrue(Equipment.TryEquip(helm, out _));
            Assert.IsTrue(Equipment.TryEquip(Armor(EquipSlot.Body, 10), out _));
            Assert.IsTrue(Equipment.TryEquip(Armor(EquipSlot.Gloves, 2), out _));
            Assert.IsTrue(Equipment.TryEquip(Armor(EquipSlot.Boots, 3), out _));
            Assert.AreEqual(19f, _stats.Get(StatType.Armor));
            Assert.AreSame(helm, Equipment.Get(EquipSlot.Helm));

            Equipment.Unequip(EquipSlot.Helm);
            Assert.AreEqual(15f, _stats.Get(StatType.Armor));
        }

        [Test, Description("Un pezzo d'armatura chiede la sua Forza, come le armi")]
        public void ArmorPieces_RequireStrength()
        {
            var plate = Armor(EquipSlot.Body, 20, strength: 40);
            Assert.IsFalse(Equipment.MeetsRequirements(plate.Definition));
            Assert.IsFalse(Equipment.TryEquip(plate, out _));

            _stats.SetBase(StatType.Strength, 40f);
            Assert.IsTrue(Equipment.TryEquip(plate, out _));
        }

        [Test, Description("Due anelli: il primo nel primo slot, il secondo nell'altro; il terzo prende il posto del primo")]
        public void Rings_FillBothSlots()
        {
            var first = Jewel(EquipSlot.Ring);
            var second = Jewel(EquipSlot.Ring);
            var third = Jewel(EquipSlot.Ring);

            Assert.IsTrue(Equipment.TryEquip(first, out _));
            Assert.IsTrue(Equipment.TryEquip(second, out _));
            Assert.AreSame(first, Equipment.Get(EquipSlot.Ring));
            Assert.AreSame(second, Equipment.Get(EquipSlot.Ring2));

            Assert.IsTrue(Equipment.TryEquip(third, out var previous));
            Assert.AreSame(first, previous);
            Assert.AreSame(third, Equipment.Get(EquipSlot.Ring));
        }

        [Test, Description("Ogni oggetto entra solo nel suo slot: un anello in tutti e due quelli degli anelli, l'amuleto e l'elmo solo nel loro")]
        public void Fits_OnlyTheRightSlots()
        {
            var ring = Jewel(EquipSlot.Ring).Definition;
            var amulet = Jewel(EquipSlot.Amulet).Definition;
            var helm = Armor(EquipSlot.Helm, 1).Definition;

            Assert.IsTrue(Equipment.Fits(ring, EquipSlot.Ring));
            Assert.IsTrue(Equipment.Fits(ring, EquipSlot.Ring2));
            Assert.IsFalse(Equipment.Fits(ring, EquipSlot.Amulet));
            Assert.IsTrue(Equipment.Fits(amulet, EquipSlot.Amulet));
            Assert.IsFalse(Equipment.Fits(amulet, EquipSlot.Ring2));
            Assert.IsTrue(Equipment.Fits(helm, EquipSlot.Helm));
            Assert.IsFalse(Equipment.Fits(helm, EquipSlot.Body));
            Assert.AreEqual(AffixTargets.Jewelry, ring.AffixTarget);
            Assert.AreEqual(AffixTargets.Armor, helm.AffixTarget);
        }

        [Test, Description("Con un anello sul cursore, un click sul secondo slot degli anelli lo mette lì; un elmo sullo slot dell'armatura no")]
        public void ClickSlot_PutsTheRingWhereClicked()
        {
            var ring = Jewel(EquipSlot.Ring);
            Assert.IsTrue(_inventory.Grid.TryAutoPlace(ring));
            _inventory.ClickCell(new Vector2Int(0, 0));
            Assert.AreSame(ring, _inventory.Held);

            Assert.IsTrue(_inventory.ClickSlot(EquipSlot.Ring2));
            Assert.AreSame(ring, Equipment.Get(EquipSlot.Ring2));
            Assert.IsNull(Equipment.Get(EquipSlot.Ring));
            Assert.IsNull(_inventory.Held);

            var helm = Armor(EquipSlot.Helm, 2);
            Assert.IsTrue(_inventory.Grid.TryAutoPlace(helm));
            _inventory.ClickCell(new Vector2Int(0, 0));
            Assert.IsFalse(_inventory.ClickSlot(EquipSlot.Body));
            Assert.IsTrue(_inventory.ClickSlot(EquipSlot.Helm));
        }

        [Test, Description("Il tooltip di un pezzo d'armatura ha l'Armatura e la Forza richiesta, ma non il blocco, che è degli scudi")]
        public void ArmorTooltip_HasNoBlock()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv");
            var localizer = new Localizer(StringTable.Parse(csv.text));
            var builder = new StringBuilder();

            ItemDescription.Write(builder, Armor(EquipSlot.Helm, 4, strength: 20), true, localizer);
            string text = builder.ToString();
            StringAssert.Contains("Armor: 4", text);
            StringAssert.Contains("Required Strength: 20", text);
            StringAssert.DoesNotContain("Block", text);

            ItemDescription.Write(builder, Armor(EquipSlot.Offhand, 4), true, localizer);
            StringAssert.Contains("Block chance: 20%", builder.ToString(), "lo scudo sì");
        }
    }
}
