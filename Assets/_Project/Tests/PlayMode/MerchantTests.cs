using System;
using System.Collections;
using System.IO;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Il mercante della città: banco, comprare, vendere (D5 della M10).</summary>
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

        // una partita nuova dal menu, in città, poi un click sul mercante: il cavaliere ci va e apre il banco
        private static IEnumerator OpenTheShop()
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

            var merchant = UnityEngine.Object.FindFirstObjectByType<LevelManager>().CurrentLevel.Merchant;
            Assert.IsNotNull(merchant, "in città c'è il mercante");
            GameObject.Find("Player").GetComponent<PlayerController>().Use(merchant.GetComponent<Interactable>());
            for (float t = 0f; !Shop.IsOpen; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 10f, "il cavaliere non arriva al banco");
                yield return null;
            }
        }

        [UnityTest, Description("Raggiunto il mercante si apre il banco con l'inventario accanto: due pozioni, la pergamena del ritorno e otto oggetti del livello 1, con il prezzo; il nome sopra è Mercante")]
        public IEnumerator Merchant_OpensTheShop()
        {
            yield return OpenTheShop();
            Assert.IsTrue(Bag.IsOpen);
            Assert.IsTrue(Bag.IsTrading);
            Assert.AreEqual("Merchant", UnityEngine.Object.FindFirstObjectByType<LevelManager>().CurrentLevel.Merchant.GetComponent<Interactable>().GetLabel(Localizer));
            for (int i = 0; i < 11; i++)
            {
                Assert.IsTrue(Shop.Slot(i).HasItem, $"posto {i} vuoto");
            }

            Assert.IsFalse(Shop.Slot(11).HasItem);
            Assert.AreEqual("17", Shop.Slot(0).PriceText, "la pozione di cura");
        }

        [UnityTest, Description("Comprare: senza oro lo dice, con l'oro la pozione va nella cintura e le monete suonano; il tooltip dice prezzo e tasto")]
        public IEnumerator Buy_NeedsGold()
        {
            yield return OpenTheShop();
            var inventory = PlayerBag.Inventory;
            int belt = inventory.Belt.Count;
            Shop.Buy(0);
            Assert.AreEqual("Not enough gold", Shop.MessageText);
            Assert.AreEqual(belt, inventory.Belt.Count);

            inventory.AddGold(100);
            Shop.Hover(2);
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
