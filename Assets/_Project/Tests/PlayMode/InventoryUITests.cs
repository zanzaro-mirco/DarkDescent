using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class InventoryUITests : SandboxFixture
    {
        private PlayerInventory _inventory;
        private InventoryPanel _panel;
        private CharacterPanel _character;
        private ItemCursor _cursor;

        private static ItemInstance Item(string name)
        {
#if UNITY_EDITOR
            return new ItemInstance(UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));
#else
            return null;
#endif
        }

        private IEnumerator LoadUI()
        {
            yield return LoadSandbox();
            _inventory = Player.GetComponent<PlayerInventory>();
            _panel = Object.FindFirstObjectByType<InventoryPanel>();
            _character = Object.FindFirstObjectByType<CharacterPanel>();
            _cursor = Object.FindFirstObjectByType<ItemCursor>();
        }

        // In Screen Space - Overlay le coordinate "mondo" di un RectTransform sono pixel dello schermo
        private Vector2 CellOnScreen(Vector2Int cell)
        {
            var grid = (RectTransform)Object.FindFirstObjectByType<InventoryGridView>().transform;
            var corners = new Vector3[4];
            grid.GetWorldCorners(corners);
            Vector2 topLeft = corners[1];
            float cellSize = (corners[2].x - corners[1].x) / _inventory.Inventory.Grid.Width;
            return topLeft + new Vector2((cell.x + 0.5f) * cellSize, -(cell.y + 0.5f) * cellSize);
        }

        private static Vector2 CenterOnScreen(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) / 2f;
        }

        private IEnumerator ClickScreen(Vector2 position)
        {
            Move(Mouse.position, position);
            yield return null;
            Press(Mouse.leftButton);
            yield return null;
            Release(Mouse.leftButton);
            yield return null;
        }

        private IEnumerator HoverScreen(Vector2 position)
        {
            Move(Mouse.position, position);
            yield return null;
            yield return null;
        }

        // In Overlay gli angoli "mondo" del riquadro sono pixel dello schermo
        private static void AssertInsideScreen(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Assert.GreaterOrEqual(corners[0].x, -0.5f, "esce a sinistra");
            Assert.GreaterOrEqual(corners[0].y, -0.5f, "esce in basso");
            Assert.LessOrEqual(corners[2].x, Screen.width + 0.5f, "esce a destra");
            Assert.LessOrEqual(corners[2].y, Screen.height + 0.5f, "esce in alto");
        }

        private EquipmentSlotView SlotView(EquipSlot slot)
        {
            foreach (var view in Object.FindObjectsByType<EquipmentSlotView>(FindObjectsSortMode.None))
            {
                if (view.Slot == slot)
                {
                    return view;
                }
            }

            return null;
        }

        [UnityTest, Description("I apre e chiude l'inventario, C il pannello del personaggio")]
        public IEnumerator Keys_TogglePanels()
        {
            yield return LoadUI();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            // un frame perché le azioni vedano la tastiera nuova
            yield return null;
            Assert.IsFalse(_panel.IsOpen);
            Assert.IsFalse(_character.IsOpen);

            Press(keyboard.iKey);
            yield return null;
            Release(keyboard.iKey);
            yield return null;
            Assert.IsTrue(_panel.IsOpen);

            Press(keyboard.cKey);
            yield return null;
            Release(keyboard.cKey);
            yield return null;
            Assert.IsTrue(_character.IsOpen);

            Press(keyboard.iKey);
            yield return null;
            Release(keyboard.iKey);
            yield return null;
            Assert.IsFalse(_panel.IsOpen);
        }

        [UnityTest, Description("Un click su una cella prende l'oggetto, un altro lo posa; il cavaliere non si muove")]
        public IEnumerator ClickCells_PickAndPlace_PlayerStays()
        {
            yield return LoadUI();
            var shield = Item("BadgeShield");
            _inventory.TryPickUp(shield);
            _panel.SetOpen(true);
            yield return null;
            Vector3 start = Player.position;
            Assert.AreEqual(1, _panel.VisibleItemCount);

            yield return ClickScreen(CellOnScreen(new Vector2Int(1, 1)));
            Assert.AreSame(shield, _inventory.Inventory.Held, "preso sul cursore");
            Assert.IsTrue(_cursor.IsShowing);
            Assert.AreEqual(0, _panel.VisibleItemCount);

            yield return ClickScreen(CellOnScreen(new Vector2Int(6, 2)));
            Assert.IsNull(_inventory.Inventory.Held, "posato");
            Assert.IsFalse(_cursor.IsShowing);
            Assert.AreEqual(new Vector2Int(6, 2), _inventory.Inventory.Grid.Placements[shield].position);

            yield return new WaitForSeconds(0.3f);
            Assert.Less(FlatDistance(start, Player.position), 0.05f, "i click sulla finestra non arrivano al mondo");
            Assert.IsFalse(PlayerAgent.hasPath);
        }

        [UnityTest, Description("Dalla griglia allo slot dell'arma: il cavaliere la impugna e il pannello mostra il danno nuovo")]
        public IEnumerator EquipFromGrid_UpdatesWeaponAndCharacterPanel()
        {
            yield return LoadUI();
            var blade = Item("SkeletonBlade");
            _inventory.TryPickUp(blade);
            _panel.SetOpen(true);
            _character.Toggle();
            yield return null;
            StringAssert.Contains("8–12", _character.ValuesText, "spada corta: 6–9 per 1,3");

            yield return ClickScreen(CellOnScreen(new Vector2Int(0, 1)));
            yield return ClickScreen(CenterOnScreen((RectTransform)SlotView(EquipSlot.Weapon).transform));

            Assert.AreSame(blade, _inventory.Equipment.Get(EquipSlot.Weapon));
            Assert.AreSame(blade.Definition, Player.GetComponent<MeleeAttack>().Weapon);
            Assert.AreEqual("ShortSword", _inventory.Inventory.Held.Definition.name, "la spada corta sale sul cursore");
            Assert.IsTrue(SlotView(EquipSlot.Weapon).HasIcon);
            StringAssert.Contains("10–16", _character.ValuesText, "lama: 8–12 per 1,3");
        }

        [UnityTest, Description("Con un oggetto sul cursore, un click fuori dalle finestre lo lascia a terra ai piedi del cavaliere")]
        public IEnumerator ClickOutside_DropsHeldItem()
        {
            yield return LoadUI();
            var shield = Item("BadgeShield");
            _inventory.TryPickUp(shield);
            _panel.SetOpen(true);
            yield return null;
            Vector3 start = Player.position;

            yield return ClickScreen(CellOnScreen(new Vector2Int(0, 0)));
            Assert.AreSame(shield, _inventory.Inventory.Held);
            yield return ClickScreen(new Vector2(Screen.width * 0.3f, Screen.height * 0.5f));

            Assert.IsNull(_inventory.Inventory.Held);
            var dropped = Object.FindFirstObjectByType<GroundItem>();
            Assert.IsNotNull(dropped, "lo scudo è a terra");
            Assert.AreSame(shield, dropped.Item);
            Assert.Less(FlatDistance(dropped.transform.position, Player.position), 1.5f);
            yield return new WaitForSeconds(0.3f);
            Assert.Less(FlatDistance(start, Player.position), 0.05f, "il cavaliere non parte verso il punto cliccato");
        }

        [UnityTest, Description("Il tooltip della lama mostra danno e Forza richiesta; sparisce quando il cursore esce dalla cella")]
        public IEnumerator Tooltip_ShowsBlade_HidesWhenCursorLeaves()
        {
            yield return LoadUI();
            var tooltip = Object.FindFirstObjectByType<ItemTooltip>();
            _inventory.TryPickUp(Item("SkeletonBlade"));
            _panel.SetOpen(true);
            yield return null;
            Assert.IsFalse(tooltip.IsShowing);

            yield return HoverScreen(CellOnScreen(new Vector2Int(0, 1)));
            Assert.IsTrue(tooltip.IsShowing, "sopra la lama");
            StringAssert.Contains("Lama dello scheletro", tooltip.Text);
            StringAssert.Contains("Danno: 8–12", tooltip.Text);
            StringAssert.Contains("Forza richiesta: 25", tooltip.Text);
            StringAssert.DoesNotContain("<color", tooltip.Text, "Forza 30: il requisito non è in rosso");
            AssertInsideScreen(tooltip.Box);

            yield return HoverScreen(CellOnScreen(new Vector2Int(5, 2)));
            Assert.IsFalse(tooltip.IsShowing, "su una cella vuota");

            yield return HoverScreen(CellOnScreen(new Vector2Int(0, 2)));
            Assert.IsTrue(tooltip.IsShowing);
            yield return HoverScreen(new Vector2(Screen.width * 0.3f, Screen.height * 0.5f));
            Assert.IsFalse(tooltip.IsShowing, "fuori dalla griglia");

            // con un oggetto sul cursore il tooltip coprirebbe la cella dove posarlo
            yield return ClickScreen(CellOnScreen(new Vector2Int(0, 0)));
            Assert.IsNotNull(_inventory.Inventory.Held);
            yield return HoverScreen(CellOnScreen(new Vector2Int(0, 1)));
            Assert.IsFalse(tooltip.IsShowing, "non mentre si tiene un oggetto");
        }

        [UnityTest, Description("Sopra lo slot dell'arma il tooltip descrive la spada impugnata; chiudendo l'inventario sparisce")]
        public IEnumerator Tooltip_OnWeaponSlot_HidesOnClose()
        {
            yield return LoadUI();
            var tooltip = Object.FindFirstObjectByType<ItemTooltip>();
            _panel.SetOpen(true);
            yield return null;

            yield return HoverScreen(CenterOnScreen((RectTransform)SlotView(EquipSlot.Weapon).transform));
            Assert.IsTrue(tooltip.IsShowing);
            StringAssert.Contains("Spada corta", tooltip.Text);
            StringAssert.Contains("Danno: 6–9", tooltip.Text);
            AssertInsideScreen(tooltip.Box);

            _panel.SetOpen(false);
            Assert.IsFalse(tooltip.IsShowing);
        }

        [UnityTest, Description("Un oggetto in un angolo dello schermo: il tooltip resta dentro, sotto o di lato")]
        public IEnumerator Tooltip_NearScreenEdges_StaysInside()
        {
            yield return LoadUI();
            var tooltip = Object.FindFirstObjectByType<ItemTooltip>();
            var definition = Item("SkeletonBlade").Definition;

            // un bersaglio finto grande quanto una cella, in ognuno dei quattro angoli
            var target = new GameObject("Target", typeof(RectTransform)).GetComponent<RectTransform>();
            target.SetParent(tooltip.transform.parent, false);
            target.sizeDelta = new Vector2(56f, 56f);
            var corners = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            foreach (var corner in corners)
            {
                target.anchorMin = target.anchorMax = target.pivot = corner;
                target.anchoredPosition = Vector2.zero;
                tooltip.Show(definition, true, target);
                AssertInsideScreen(tooltip.Box);
            }

            Object.Destroy(target.gameObject);
        }

        [UnityTest, Description("Chiudendo l'inventario con un oggetto sul cursore, l'oggetto torna nella griglia")]
        public IEnumerator Close_PutsHeldItemBack()
        {
            yield return LoadUI();
            var shield = Item("BadgeShield");
            _inventory.TryPickUp(shield);
            _panel.SetOpen(true);
            yield return null;
            yield return ClickScreen(CellOnScreen(new Vector2Int(0, 0)));

            _panel.SetOpen(false);

            Assert.IsNull(_inventory.Inventory.Held);
            Assert.IsTrue(_inventory.Inventory.Grid.TryGetPlacement(shield, out _));
        }
    }
}
