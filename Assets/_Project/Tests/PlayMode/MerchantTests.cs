using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Localization;
using DarkDescent.Magic;
using DarkDescent.Player;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>I banchi del borgo: mercante, fabbro e guaritrice; banco, comprare, vendere (D5 della M10, D18 della M18).</summary>
    public class MerchantTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentShop_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, null);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static ShopPanel Shop => UnityEngine.Object.FindFirstObjectByType<ShopPanel>();

        private static InventoryPanel Bag => UnityEngine.Object.FindFirstObjectByType<InventoryPanel>();

        private static PlayerInventory PlayerBag => GameObject.Find("Player").GetComponent<PlayerInventory>();

        private static LevelContext Town => UnityEngine.Object.FindFirstObjectByType<LevelManager>().CurrentLevel;

        // una partita nuova dal menu, in città
        private static IEnumerator NewGame()
        {
            ForgetPreferences();
            Environment.SetEnvironmentVariable(CompositionRoot.MenuVariable, "1");
            SceneManager.LoadScene("Core");
            yield return null;
            yield return null;
            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenu>();
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(1).Button.onClick.Invoke();
            yield return WaitForLevel();
        }

        // un click sul banco che tratta quella merce: il cavaliere ci va e lo apre
        private static IEnumerator Visit(ShopGoods goods)
        {
            var merchant = Town.Trading(goods);
            Assert.IsNotNull(merchant, $"in città c'è il banco di {goods}");
            GameObject.Find("Player").GetComponent<PlayerController>().Use(merchant.GetComponent<Interactable>());
            for (float t = 0f; !Shop.IsOpen || Shop.TitleKey != merchant.NameKey; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, "il cavaliere non arriva al banco");
                yield return null;
            }
        }

        private static IEnumerator OpenTheShop(ShopGoods goods = ShopGoods.Jewelry)
        {
            yield return NewGame();
            yield return Visit(goods);
        }

        [UnityTest, Description("Raggiunto il mercante si apre il banco con l'inventario accanto: quattro anelli e amuleti del livello 1, con il prezzo; il nome sopra è Mercante")]
        public IEnumerator Merchant_OpensTheShop()
        {
            yield return OpenTheShop();
            Assert.IsTrue(Bag.IsOpen);
            Assert.IsTrue(Bag.IsTrading);
            Assert.AreEqual("Merchant", Town.Merchant.GetComponent<Interactable>().GetLabel(Localizer));
            Assert.AreEqual(TextKeys.MerchantName, Shop.TitleKey);
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(Shop.Slot(i).HasItem, $"posto {i} vuoto");
                Assert.IsInstanceOf<JewelryDefinition>(Shop.ItemAt(i).Definition, $"posto {i}");
                Assert.AreEqual(1, Shop.ItemAt(i).ItemLevel);
            }

            Assert.IsFalse(Shop.Slot(4).HasItem);
            Assert.IsNotEmpty(Shop.Slot(0).PriceText);
        }

        [UnityTest, Description("Il fabbro, alla forgia, vende otto armi, armature e scudi; il nome sopra è Fabbro")]
        public IEnumerator Smith_SellsWeaponsAndArmor()
        {
            yield return OpenTheShop(ShopGoods.Weapons);
            Assert.AreEqual(TextKeys.SmithName, Shop.TitleKey);
            Assert.AreEqual("Blacksmith", Localizer.Get(Shop.TitleKey));
            for (int i = 0; i < 8; i++)
            {
                var item = Shop.ItemAt(i);
                Assert.IsNotNull(item, $"posto {i} vuoto");
                Assert.IsTrue(item.Definition is WeaponDefinition || item.Definition is ArmorDefinition, $"{item.Definition.name} dal fabbro");
            }

            Assert.IsNull(Shop.ItemAt(8));
        }

        [UnityTest, Description("La guaritrice riempie vita e mana a chi arriva, gratis, e vende le due pozioni; il nome sopra è Guaritrice")]
        public IEnumerator Healer_HealsAndSellsPotions()
        {
            yield return NewGame();
            var player = GameObject.Find("Player");
            var health = player.GetComponent<Health>();
            var mana = player.GetComponent<PlayerMagic>().Mana;
            health.SetCurrent(health.Max * 0.3f);
            mana.SetCurrent(0f);
            int gold = PlayerBag.Inventory.Gold;

            yield return Visit(ShopGoods.Potions);
            Assert.AreEqual(health.Max, health.Current, 0.01f, "vita piena");
            Assert.AreEqual(mana.Max, mana.Current, 0.01f, "mana pieno");
            Assert.AreEqual(gold, PlayerBag.Inventory.Gold, "gratis");
            Assert.AreEqual(TextKeys.HealerName, Shop.TitleKey);
            CollectionAssert.AreEqual(new[] { "HealingPotion", "ManaPotion" }, new[] { Shop.ItemAt(0).Definition.name, Shop.ItemAt(1).Definition.name });
            Assert.IsNull(Shop.ItemAt(2));
            Assert.AreEqual("17", Shop.Slot(0).PriceText, "la pozione di cura");
        }

        [UnityTest, Description("Ogni banco compra solo i suoi tipi: il mercante un'ascia no, e il tooltip lo dice; il fabbro sì")]
        public IEnumerator Shops_BuyOnlyTheirGoods()
        {
            yield return OpenTheShop();
            var inventory = PlayerBag.Inventory;
            var axe = new ItemInstance(UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Axe.asset"), Rarity.Magic, 3, 5UL, null);
            Assert.IsTrue(inventory.Grid.TryPlace(axe, new Vector2Int(0, 0)));
            Bag.HoverCell(new Vector2Int(0, 0));
            StringAssert.Contains("Won't buy this item", Bag.Tooltip.Text);
            StringAssert.DoesNotContain("Sells for", Bag.Tooltip.Text);

            int gold = inventory.Gold;
            Bag.UseCell(new Vector2Int(0, 0));
            Assert.AreSame(axe, inventory.Grid.ItemAt(new Vector2Int(0, 0)), "l'ascia resta nella borsa");
            Assert.AreEqual(gold, inventory.Gold);
            Assert.AreEqual("Won't buy this item", Shop.MessageText);

            yield return Visit(ShopGoods.Weapons);
            Assert.AreEqual(TextKeys.SmithName, Shop.TitleKey, "la finestra passa al fabbro");
            Bag.UseCell(new Vector2Int(0, 0));
            Assert.IsNull(inventory.Grid.ItemAt(new Vector2Int(0, 0)), "il fabbro la compra");
            Assert.AreEqual(gold + ItemPricing.SellPrice(axe), inventory.Gold);
        }

        [UnityTest, Description("Allontanandosi dal banco il negozio si chiude da solo, e con lui l'inventario; un passo accanto al banco lo lascia aperto")]
        public IEnumerator WalkingAway_ClosesTheShop()
        {
            yield return OpenTheShop();
            var merchant = UnityEngine.Object.FindFirstObjectByType<LevelManager>().CurrentLevel.Merchant;
            var counter = merchant.GetComponent<Interactable>().ApproachPoint;
            var agent = GameObject.Find("Player").GetComponent<UnityEngine.AI.NavMeshAgent>();

            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(agent.transform.position, out var near, 1f, UnityEngine.AI.NavMesh.AllAreas));
            agent.Warp(near.position);
            yield return null;
            yield return null;
            Assert.IsTrue(Shop.IsOpen, "accanto al banco resta aperto");

            Vector3 far = Vector3.zero;
            bool found = false;
            for (int i = 0; i < 8 && !found; i++)
            {
                var direction = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                if (UnityEngine.AI.NavMesh.SamplePosition(counter + direction * (merchant.LeaveDistance + 3f), out var hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas)
                    && Vector3.Distance(hit.position, counter) > merchant.LeaveDistance + 1f)
                {
                    far = hit.position;
                    found = true;
                }
            }

            Assert.IsTrue(found, "un punto lontano dal banco sul NavMesh");
            agent.Warp(far);
            yield return null;
            yield return null;
            Assert.IsFalse(Shop.IsOpen, "lontano dal banco il negozio si chiude");
            Assert.IsFalse(Bag.IsOpen, "e con lui l'inventario");
        }

        [UnityTest, Description("Passando su un oggetto del banco si vede accanto quello indossato nello stesso posto, come dalla borsa; lasciandolo spariscono tutti e due")]
        public IEnumerator Hover_ComparesWithTheEquipped()
        {
            yield return OpenTheShop(ShopGoods.Weapons);
            var equipment = PlayerBag.Inventory.Equipment;
            int index = -1;
            for (int i = 0; i < 10 && index < 0; i++)
            {
                var item = Shop.ItemAt(i);
                // Il seme cambia a ogni partita: serve un oggetto che il cavaliere possa indossare
                // (forza sufficiente) o uno per uno slot già occupato
                if (item != null && item.Definition.Slot != EquipSlot.None && item.Definition.Slot != EquipSlot.Ring
                    && (equipment.Get(item.Definition.Slot) != null || equipment.MeetsRequirements(item.Definition)))
                {
                    index = i;
                }
            }

            Assert.GreaterOrEqual(index, 0, "il banco vende qualcosa da indossare");
            var forSale = Shop.ItemAt(index);
            var slot = equipment.TargetSlot(forSale.Definition);
            if (equipment.Get(slot) == null)
            {
                Assert.IsTrue(equipment.TryEquip(new ItemInstance(forSale.Definition, Rarity.Normal, 1, 7UL, null), out _));
            }

            Shop.Hover(index);
            yield return null;
            Assert.IsTrue(Bag.Tooltip.IsShowing);
            Assert.IsTrue(Bag.CompareTooltip.IsShowing, "accanto quello indossato");
            StringAssert.Contains("Equipped", Bag.CompareTooltip.Text);
            Assert.IsFalse(Bag.SecondCompareTooltip.IsShowing);
            var a = new Vector3[4];
            var b = new Vector3[4];
            Bag.Tooltip.Box.GetWorldCorners(a);
            Bag.CompareTooltip.Box.GetWorldCorners(b);
            bool overlap = a[0].x < b[2].x && b[0].x < a[2].x && a[0].y < b[2].y && b[0].y < a[2].y;
            Assert.IsFalse(overlap, "i due riquadri non si sovrappongono");
            Assert.GreaterOrEqual(b[0].x, -0.5f, "il confronto esce a sinistra");
            Assert.LessOrEqual(b[2].x, Screen.width + 0.5f, "il confronto esce a destra");

            Shop.Hover(-1);
            yield return null;
            Assert.IsFalse(Bag.Tooltip.IsShowing);
            Assert.IsFalse(Bag.CompareTooltip.IsShowing, "lasciato il banco il confronto sparisce");
        }

        [UnityTest, Description("Comprare: senza oro lo dice, con l'oro la pozione va nella cintura e le monete suonano; il tooltip dice prezzo e tasto")]
        public IEnumerator Buy_NeedsGold()
        {
            yield return OpenTheShop(ShopGoods.Potions);
            var inventory = PlayerBag.Inventory;
            int belt = inventory.Belt.Count;
            Shop.Buy(0);
            Assert.AreEqual("Not enough gold", Shop.MessageText);
            Assert.AreEqual(belt, inventory.Belt.Count);

            inventory.AddGold(100);
            Shop.Hover(1);
            StringAssert.Contains("Price: ", Bag.Tooltip.Text);
            StringAssert.Contains("Click to buy", Bag.Tooltip.Text);
            Shop.Buy(0);
            Assert.AreEqual(string.Empty, Shop.MessageText);
            Assert.AreEqual(83, inventory.Gold);
            Assert.AreEqual(belt + 1, inventory.Belt.Count);
        }

        [UnityTest, Description("Con il banco aperto il click destro su un oggetto della borsa lo vende, e il tooltip della borsa dice a quanto")]
        public IEnumerator RightClick_SellsFromTheBag()
        {
            yield return OpenTheShop();
            var inventory = PlayerBag.Inventory;
            var ring = new ItemInstance(UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Ring.asset"), Rarity.Magic, 3, 5UL, null);
            Assert.IsTrue(inventory.Grid.TryPlace(ring, new Vector2Int(0, 0)));
            Bag.HoverCell(new Vector2Int(0, 0));
            StringAssert.Contains("Sells for " + ItemPricing.SellPrice(ring) + " gold", Bag.Tooltip.Text);
            StringAssert.DoesNotContain("Right-click to equip", Bag.Tooltip.Text);

            int gold = inventory.Gold;
            Bag.UseCell(new Vector2Int(0, 0));
            Assert.AreEqual(gold + ItemPricing.SellPrice(ring), inventory.Gold);
            Assert.IsNull(inventory.Grid.ItemAt(new Vector2Int(0, 0)));
            Assert.AreEqual(0, UnityEngine.Object.FindFirstObjectByType<CompositionRoot>().RunStats.GoldCollected, "vendere non è raccogliere");
        }

        [UnityTest, Description("Il banco si chiude con la sua X, chiudendo l'inventario, con Esc e aprendo il pannello del personaggio; chiuso, la borsa non vende più")]
        public IEnumerator Shop_ClosesWithTheBag()
        {
            yield return OpenTheShop();
            Shop.CloseButton.onClick.Invoke();
            Assert.IsFalse(Shop.IsOpen);
            Assert.IsTrue(Bag.IsOpen, "l'inventario resta");
            Assert.IsFalse(Bag.IsTrading);

            Shop.Open();
            Bag.SetOpen(false);
            Assert.IsFalse(Shop.IsOpen);

            Shop.Open();
            GameObject.Find("Player").GetComponent<PlayerInputReader>().PressPause();
            Assert.IsFalse(Shop.IsOpen);
            Assert.IsFalse(Bag.IsOpen);

            Shop.Open();
            UnityEngine.Object.FindFirstObjectByType<CharacterPanel>().Toggle();
            Assert.IsFalse(Shop.IsOpen, "si alternano nella metà sinistra");
        }
    }
}
