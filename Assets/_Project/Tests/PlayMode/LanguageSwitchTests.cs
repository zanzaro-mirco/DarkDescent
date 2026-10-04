using System.Collections;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class LanguageSwitchTests : SandboxFixture
    {
        [TearDown]
        public void ForgetChoice()
        {
            // la scelta salvata non deve passare ai test dopo, né alle prove nell'editor
            PlayerPrefs.DeleteKey(CompositionRoot.LanguagePreference);
        }

        private static string Label(string key)
        {
            foreach (var text in Object.FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.Key == key)
                {
                    return text.GetComponent<TMP_Text>().text;
                }
            }

            Assert.Fail($"nessuna etichetta con la chiave {key}");
            return null;
        }

        [UnityTest, Description("Si parte in inglese; F9 passa all'italiano, anche le finestre chiuse, e la scelta resta al riavvio di Core")]
        public IEnumerator F9_SwitchesToItalian_AndIsRemembered()
        {
            yield return LoadCore();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
            Assert.AreEqual("en", Localizer.Language);
            Assert.AreEqual("Inventory", Label("hud.inventory"));

            Press(keyboard.f9Key);
            yield return null;
            Release(keyboard.f9Key);
            yield return null;

            Assert.AreEqual("it", Localizer.Language);
            var character = Object.FindFirstObjectByType<CharacterPanel>();
            character.Toggle();
            yield return null;
            Assert.AreEqual("Cavaliere", Label("hud.character"), "la finestra era chiusa al cambio");
            StringAssert.StartsWith("Forza\nDestrezza", Label("hud.stat_names"));

            SceneManager.LoadScene("Core");
            yield return WaitForLevel();
            Assert.AreEqual("it", Localizer.Language, "la preferenza salvata vale al nuovo avvio");
            Assert.AreEqual("Inventario", Label("hud.inventory"));
        }

        [UnityTest, Description("Cambiando lingua con il tooltip aperto, il tooltip e il nome dell'oggetto a terra si riscrivono")]
        public IEnumerator LanguageChange_RewritesTooltipAndGroundLabel()
        {
            yield return LoadSandbox();
#if UNITY_EDITOR
            var blade = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/SkeletonBlade.asset");
#else
            ItemDefinition blade = null;
#endif
            var inventory = Player.GetComponent<PlayerInventory>();
            inventory.TryPickUp(new ItemInstance(blade));
            var panel = Object.FindFirstObjectByType<InventoryPanel>();
            var tooltip = Object.FindFirstObjectByType<ItemTooltip>();
            panel.SetOpen(true);
            yield return null;
            panel.HoverCell(new Vector2Int(0, 1));
            StringAssert.Contains("Skeleton Blade", tooltip.Text);

            Localizer.SetLanguage("it");

            StringAssert.Contains("Lama dello scheletro", tooltip.Text);
            StringAssert.Contains("Danno: 8–12", tooltip.Text);

            // a terra: il nome viene composto quando lo si chiede, nella lingua di quel momento
            panel.HoverCell(new Vector2Int(0, 0));
            yield return null;
            panel.ClickCell(new Vector2Int(0, 0));
            var dropped = inventory.DropHeld();
            StringAssert.Contains(">Lama dello scheletro<", dropped.GetComponent<Interactable>().GetLabel(Localizer));
            Localizer.SetLanguage("en");
            StringAssert.Contains(">Skeleton Blade<", dropped.GetComponent<Interactable>().GetLabel(Localizer));
        }
    }
}
