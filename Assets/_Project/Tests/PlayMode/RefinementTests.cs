using System.Collections;
using System.Linq;
using DarkDescent.Items;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DarkDescent.Tests
{
    /// <summary>Le rifiniture chieste nella prova della M9 (D1 della M10), nella scena.</summary>
    public class RefinementTests : SandboxFixture
    {
        private static ItemInstance Item(string name)
        {
            return new ItemInstance(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset"));
        }

        [UnityTest, Description("Il pulsante dorato compare salendo di livello con i punti, si nasconde a pannello aperto, lo apre con un click e sparisce a punti finiti")]
        public IEnumerator PointsButton_FollowsThePoints()
        {
            yield return LoadSandbox();
            var button = Object.FindFirstObjectByType<PointsButton>();
            var panel = Object.FindFirstObjectByType<CharacterPanel>();
            var progress = Player.GetComponent<PlayerProgress>().Progress;
            Assert.IsFalse(button.IsShown, "senza punti non c'è");

            progress.Add(progress.ExperienceToNext - progress.Experience);
            yield return null;
            Assert.IsTrue(button.IsShown);
            Assert.AreEqual("+5", button.LabelText);

            button.Button.onClick.Invoke();
            Assert.IsTrue(panel.IsOpen, "il click apre il pannello");
            Assert.IsFalse(button.IsShown, "a pannello aperto i + sono già lì");

            panel.Toggle();
            Assert.IsTrue(button.IsShown);
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(progress.TrySpend(StatType.Strength));
            }

            Assert.AreEqual("+1", button.LabelText);
            Assert.IsTrue(progress.TrySpend(StatType.Strength));
            Assert.IsFalse(button.IsShown, "punti finiti");
        }

        [UnityTest, Description("Una caratteristica alzata da un oggetto è azzurra, abbassata da un effetto è rossa; il tooltip dice base e differenza")]
        public IEnumerator Panel_ColorsChangedValues()
        {
            yield return LoadSandbox();
            var panel = Object.FindFirstObjectByType<CharacterPanel>();
            var stats = Player.GetComponent<CharacterStats>();
            panel.Toggle();
            yield return null;
            StringAssert.StartsWith("30\n", panel.ValuesText, "la Forza senza niente è bianca");

            stats.Sheet.AddModifier(new StatModifier(StatType.Strength, ModifierKind.Flat, 5f, this));
            stats.Sheet.AddModifier(new StatModifier(StatType.Dexterity, ModifierKind.Flat, -4f, this));
            yield return null;
            StringAssert.StartsWith("<color=" + CharacterPanel.RaisedColor + ">35</color>\n<color=" + CharacterPanel.LoweredColor + ">", panel.ValuesText);
            Assert.IsTrue(panel.TryGetLine(0, out int value, out int baseValue));
            Assert.AreEqual((35, 30), (value, baseValue));

            panel.HoverLine(0);
            yield return null;
            StringAssert.Contains("Base 30", panel.Tooltip.GetComponentInChildren<TMP_Text>(true).text);
            StringAssert.Contains("+5", panel.Tooltip.GetComponentInChildren<TMP_Text>(true).text);

            stats.Sheet.RemoveModifiersFrom(this);
            yield return null;
            var lines = panel.ValuesText.Split('\n');
            Assert.AreEqual("30", lines[0], "tolto l'effetto torna bianca");
            StringAssert.DoesNotContain("<color", lines[1]);
        }

        [UnityTest, Description("Con Alt premuto sei oggetti nello stesso punto mostrano sei nomi piccoli che non si coprono; un click su un nome lo raccoglie; rilasciato Alt spariscono")]
        public IEnumerator AltLabels_StackAndPickUp()
        {
            yield return LoadSandbox();
            var prefab = AssetDatabase.LoadAssetAtPath<GroundItem>("Assets/_Project/Prefabs/GroundItem.prefab");
            Vector3 spot = Player.position + new Vector3(-2f, 0f, -2f);
            string[] names = { "Dagger", "ShortSword", "Ring", "Amulet", "BearHat", "LeatherBoots" };
            foreach (var name in names)
            {
                GroundItem.Spawn(prefab, Item(name), spot);
            }

            yield return null;
            var labels = Object.FindFirstObjectByType<GroundLabels>();
            Assert.AreEqual(0, labels.Shown.Count(), "senza Alt niente nomi");

            var reader = Player.GetComponent<PlayerInputReader>();
            reader.SetLabelsHeld(true);
            yield return null;
            yield return null;
            var shown = labels.Shown.ToArray();
            Assert.AreEqual(names.Length, shown.Length);
            var rects = shown.Select(WorldRect).ToArray();
            for (int i = 0; i < rects.Length; i++)
            {
                for (int j = i + 1; j < rects.Length; j++)
                {
                    Assert.IsFalse(rects[i].Overlaps(rects[j]), $"{shown[i].Text} e {shown[j].Text} si coprono");
                }
            }

            float hoverSize = Object.FindFirstObjectByType<InteractableLabel>(FindObjectsInactive.Include).GetComponentInChildren<TMP_Text>(true).fontSize;
            Assert.Less(shown[0].GetComponentInChildren<TMP_Text>().fontSize, hoverSize, "più piccoli del nome sotto il cursore");

            var inventory = Player.GetComponent<PlayerInventory>().Inventory;
            int before = inventory.Grid.Placements.Count + inventory.Belt.Count;
            var target = shown.Last();
            target.GetComponent<Button>().onClick.Invoke();
            for (float t = 0f; inventory.Grid.Placements.Count + inventory.Belt.Count == before; t += Time.deltaTime)
            {
                Assert.Less(t, 6f, "il cavaliere non l'ha raccolto");
                yield return null;
            }

            yield return null;
            Assert.AreEqual(names.Length - 1, labels.Shown.Count(), "il nome dell'oggetto raccolto sparisce");

            reader.SetLabelsHeld(false);
            Assert.AreEqual(0, labels.Shown.Count());
        }

        private static Rect WorldRect(GroundLabel label)
        {
            var corners = new Vector3[4];
            label.Rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
