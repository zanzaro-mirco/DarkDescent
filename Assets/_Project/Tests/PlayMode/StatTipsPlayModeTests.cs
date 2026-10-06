using System.Collections;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class StatTipsPlayModeTests : SandboxFixture
    {
        private IEnumerator HoverLine(CharacterPanel panel, int line)
        {
            Move(Mouse.position, (Vector2)panel.LineWorldPosition(line));
            yield return null;
            yield return null;
        }

        [UnityTest, Description("Passando sulle righe del pannello del personaggio un tooltip a destra della finestra spiega la statistica; sulla riga vuota e fuori sparisce, cambia lingua con il gioco, si chiude con il pannello")]
        public IEnumerator Hover_ExplainsEachStat()
        {
            yield return LoadSandbox();
            var panel = Object.FindFirstObjectByType<CharacterPanel>();
            var tooltip = panel.Tooltip;
            panel.Toggle();
            yield return null;

            yield return HoverLine(panel, 0);
            Assert.IsTrue(tooltip.IsShowing, "sulla Forza");
            StringAssert.Contains("Strength", tooltip.Text);
            StringAssert.Contains("1% weapon damage", tooltip.Text);
            var corners = new Vector3[4];
            ((RectTransform)panel.transform.Find("Window")).GetWorldCorners(corners);
            var box = new Vector3[4];
            tooltip.Box.GetWorldCorners(box);
            Assert.Greater(box[0].x, corners[2].x, "a destra della finestra: non copre le righe");

            yield return HoverLine(panel, 6);
            StringAssert.Contains("Armor", tooltip.Text);
            StringAssert.Contains("chance to hit you", tooltip.Text);

            yield return HoverLine(panel, 4);
            Assert.IsFalse(tooltip.IsShowing, "la riga vuota non ha spiegazione");

            yield return HoverLine(panel, 9);
            StringAssert.Contains("critical hit", tooltip.Text, "la riga del critico (D13 della M7)");
            yield return HoverLine(panel, 10);
            StringAssert.Contains("shield", tooltip.Text);
            Localizer.SetLanguage("it");
            yield return null;
            StringAssert.Contains("Blocco", tooltip.Text, "segue la lingua senza muovere il cursore");
            StringAssert.Contains("scudo", tooltip.Text);
            Localizer.SetLanguage("en");

            Move(Mouse.position, new Vector2(Screen.width - 10f, 10f));
            yield return null;
            yield return null;
            Assert.IsFalse(tooltip.IsShowing, "uscito dalle righe");

            yield return HoverLine(panel, 3);
            Assert.IsTrue(tooltip.IsShowing);
            panel.Toggle();
            yield return null;
            Assert.IsFalse(tooltip.IsShowing, "si chiude con il pannello");
        }
    }
}
