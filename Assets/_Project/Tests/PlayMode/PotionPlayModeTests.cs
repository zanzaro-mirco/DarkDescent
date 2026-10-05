using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class PotionPlayModeTests : SandboxFixture
    {
        private PlayerInventory _inventory;
        private InventoryPanel _panel;
        private BeltView _belt;
        private Health _health;

        private static ItemInstance Potion()
        {
#if UNITY_EDITOR
            return new ItemInstance(UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/HealingPotion.asset"));
#else
            return null;
#endif
        }

        // lo scheletro della sandbox resta fermo: i conti della vita non devono subire colpi
        private void FindParts(bool keepSkeleton = false)
        {
            if (!keepSkeleton)
            {
                GameObject.Find("Skeleton").SetActive(false);
            }

            _inventory = Player.GetComponent<PlayerInventory>();
            _health = Player.GetComponent<Health>();
            _panel = Object.FindFirstObjectByType<InventoryPanel>();
            _belt = Object.FindFirstObjectByType<BeltView>();
        }

        // ferito al 30%: abbastanza perché una pozione non arrivi al massimo
        private void Hurt()
        {
            _health.TakeDamage(new DamageInfo(Mathf.Round(_health.Max * 0.7f), DamageType.Physical, null));
        }

        private float HealOf(ItemInstance potion)
        {
            return ((PotionDefinition)potion.Definition).HealAmount(_health.Max);
        }

        // In Screen Space - Overlay le coordinate "mondo" di un RectTransform sono pixel dello schermo
        private static Vector2 CenterOnScreen(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) / 2f;
        }

        private Vector2 CellOnScreen(Vector2Int cell)
        {
            var grid = (RectTransform)Object.FindFirstObjectByType<InventoryGridView>().transform;
            var corners = new Vector3[4];
            grid.GetWorldCorners(corners);
            Vector2 topLeft = corners[1];
            float cellSize = (corners[2].x - corners[1].x) / _inventory.Inventory.Grid.Width;
            return topLeft + new Vector2((cell.x + 0.5f) * cellSize, -(cell.y + 0.5f) * cellSize);
        }

        private IEnumerator ClickScreen(Vector2 position, ButtonControl button)
        {
            Move(Mouse.position, position);
            yield return null;
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        [UnityTest, Description("Il cavaliere parte con 2 pozioni nella cintura; i tasti 1–8 bevono il loro posto, ma a vita piena non sprecano niente")]
        public IEnumerator StartingBelt_KeysDrinkOnlyWhenHurt()
        {
            yield return LoadSandbox();
            FindParts();
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Assert.AreEqual(2, _inventory.Belt.Count);
            Assert.IsTrue(_belt.Slot(0).HasIcon && _belt.Slot(1).HasIcon);
            Assert.IsFalse(_belt.Slot(2).HasIcon);

            PressAndRelease(keyboard.digit1Key);
            yield return null;
            Assert.AreEqual(2, _inventory.Belt.Count, "a vita piena la pozione resta");

            Hurt();
            float before = _health.Current;
            float heal = HealOf(_inventory.Belt.Get(1));
            PressAndRelease(keyboard.digit2Key);
            yield return null;

            Assert.AreEqual(before + heal, _health.Current, "il tasto 2 beve il secondo posto");
            Assert.IsNotNull(_inventory.Belt.Get(0));
            Assert.IsNull(_inventory.Belt.Get(1));
            Assert.IsFalse(_belt.Slot(1).HasIcon, "la cintura a schermo si è aggiornata");
            Assert.AreEqual(_health.Current / _health.Max, Object.FindFirstObjectByType<HealthOrb>().FillAmount, 0.001f);

            PressAndRelease(keyboard.digit8Key);
            yield return null;
            Assert.AreEqual(before + heal, _health.Current, "il posto 8 è vuoto");
        }

        [UnityTest, Description("Click destro beve dalla griglia e dalla cintura; con l'inventario aperto il sinistro sposta le pozioni nella cintura, da chiuso non fa niente")]
        public IEnumerator Mouse_DrinksAndMovesPotions()
        {
            yield return LoadSandbox();
            FindParts();
            var inGrid = Potion();
            _inventory.Inventory.Grid.TryPlace(inGrid, new Vector2Int(2, 1));
            _panel.SetOpen(true);
            yield return null;
            Vector3 start = Player.position;

            Hurt();
            float before = _health.Current;
            yield return ClickScreen(CellOnScreen(new Vector2Int(2, 1)), Mouse.rightButton);
            Assert.IsNull(_inventory.Inventory.Grid.ItemAt(new Vector2Int(2, 1)), "bevuta dalla griglia");
            Assert.AreEqual(before + HealOf(inGrid), _health.Current);

            Hurt();
            before = _health.Current;
            yield return ClickScreen(CenterOnScreen((RectTransform)_belt.Slot(0).transform), Mouse.rightButton);
            Assert.IsNull(_inventory.Belt.Get(0), "bevuta dalla cintura");
            Assert.Greater(_health.Current, before);

            var second = _inventory.Belt.Get(1);
            yield return ClickScreen(CenterOnScreen((RectTransform)_belt.Slot(1).transform), Mouse.leftButton);
            Assert.AreSame(second, _inventory.Inventory.Held, "presa sul cursore");
            yield return ClickScreen(CenterOnScreen((RectTransform)_belt.Slot(5).transform), Mouse.leftButton);
            Assert.AreSame(second, _inventory.Belt.Get(5), "posata nel sesto posto");
            Assert.IsNull(_inventory.Inventory.Held);

            _panel.SetOpen(false);
            yield return ClickScreen(CenterOnScreen((RectTransform)_belt.Slot(5).transform), Mouse.leftButton);
            Assert.IsNull(_inventory.Inventory.Held, "a inventario chiuso il sinistro non prende");
            Assert.AreSame(second, _inventory.Belt.Get(5));

            yield return new WaitForSeconds(0.3f);
            Assert.Less(FlatDistance(start, Player.position), 0.05f, "i click sulla cintura non arrivano al mondo");
        }

        [UnityTest, Description("Uno scheletro con la pozione nel seme la lascia accanto all'oggetto; raccolta, va nella cintura")]
        public IEnumerator SkeletonDrop_PotionGoesToTheBelt()
        {
            yield return LoadSandbox();
            FindParts(keepSkeleton: true);
            var skeleton = GameObject.Find("Skeleton");
            var drop = skeleton.GetComponent<LootDrop>();
            var root = Object.FindFirstObjectByType<CompositionRoot>();
            ulong seed = 1;
            while (drop.PreviewPotion() == null && seed < 1000)
            {
                root.UseLootSeed(++seed);
            }

            Assert.IsNotNull(drop.PreviewPotion(), "un seme con la pozione");
            skeleton.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            yield return null;

            var potions = Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Where(g => g.Item?.Definition is PotionDefinition).ToList();
            Assert.AreEqual(1, potions.Count);
            Assert.Less(FlatDistance(potions[0].transform.position, skeleton.transform.position), 2.5f, "accanto al corpo");

            potions[0].GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Assert.AreEqual(3, _inventory.Belt.Count, "nel terzo posto, dopo le due di partenza");
            Assert.IsTrue(_belt.Slot(2).HasIcon);
        }

        [UnityTest, Description("Una cassa con la pozione nel seme la lascia accanto all'oggetto")]
        public IEnumerator ChestDrop_IncludesThePotion()
        {
            yield return LoadGeneratedCore();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            foreach (var enemy in manager.CurrentLevel.Enemies)
            {
                enemy.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            }

            yield return null;
            var potionsBefore = Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Count(g => g.Item?.Definition is PotionDefinition);
            var chest = manager.CurrentLevel.Chests[0];
            var root = Object.FindFirstObjectByType<CompositionRoot>();
            ulong first = root.LootSeed, seed = first;
            while (chest.PreviewPotion() == null && seed < first + 1000)
            {
                root.UseLootSeed(++seed);
            }

            Assert.IsNotNull(chest.PreviewPotion(), "un seme con la pozione");
            chest.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;

            var potions = Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Where(g => g.Item?.Definition is PotionDefinition).ToList();
            Assert.AreEqual(potionsBefore + 1, potions.Count);
            Assert.IsTrue(potions.Any(p => FlatDistance(p.transform.position, chest.transform.position) < 3f), "davanti alla cassa");
        }
    }
}
