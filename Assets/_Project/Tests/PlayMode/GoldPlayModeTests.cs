using System.Collections;
using System.Linq;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>L'oro nel gioco (D4 della M10): cade dalle casse, si raccoglie con un click, finisce nel portafoglio.</summary>
    public class GoldPlayModeTests : SandboxFixture
    {
        [UnityTest, Description("Aperta una cassa cade la pila d'oro prevista dal seme, con il nome \"N gold\"; raccolta, l'oro va nel portafoglio e nell'inventario, non nella borsa")]
        public IEnumerator ChestGold_IsPickedUpIntoTheWallet()
        {
            yield return LoadGeneratedCore();
            ClearEnemies();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            var chest = manager.CurrentLevel.Chests[0];
            int expected = chest.PreviewGold();
            Assert.Greater(expected, 0, "le casse lasciano sempre oro");

            PlayerAgent.Warp(chest.GetComponent<Interactable>().ApproachPoint + chest.transform.forward * 1.5f);
            yield return new WaitForSeconds(0.3f);
            var controller = Player.GetComponent<PlayerController>();
            controller.Use(chest.GetComponent<Interactable>());
            for (float t = 0f; !chest.IsOpen; t += Time.deltaTime)
            {
                Assert.Less(t, 10f, "il cavaliere non ha aperto la cassa");
                yield return null;
            }

            yield return new WaitForSeconds(0.8f);
            var pile = Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Single(g => g.Gold > 0);
            Assert.AreEqual(expected, pile.Gold);
            StringAssert.Contains($"{expected} gold", pile.GetLabel(Localizer));

            var inventory = Player.GetComponent<PlayerInventory>().Inventory;
            int items = inventory.Grid.Placements.Count;
            controller.Use(pile.GetComponent<Interactable>());
            for (float t = 0f; inventory.Gold == 0; t += Time.deltaTime)
            {
                Assert.Less(t, 10f, "il cavaliere non ha raccolto l'oro");
                yield return null;
            }

            Assert.AreEqual(expected, inventory.Gold);
            Assert.AreEqual(items, inventory.Grid.Placements.Count, "l'oro non occupa la borsa");
            Assert.AreEqual($"Gold: {expected}", Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include).GoldText);
        }

        private static void ClearEnemies()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemies.EnemyAI>(FindObjectsSortMode.None))
            {
                enemy.gameObject.SetActive(false);
            }
        }
    }
}
