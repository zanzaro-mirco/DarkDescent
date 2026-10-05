using System.Collections;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DarkDescent.Tests
{
    public class AutomapTests : SandboxFixture
    {
        private static AutomapView View => Object.FindFirstObjectByType<AutomapView>();
        private static ExplorationTracker Tracker => Object.FindFirstObjectByType<ExplorationTracker>();

        private static Vector2Int Find(LevelMap map, char symbol)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.GetSymbol(x, y) == symbol)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            Assert.Fail($"nessun {symbol} nella mappa");
            return default;
        }

        [UnityTest, Description("Nella cripta la minimappa parte accesa nell'angolo con i dintorni dell'ingresso; camminando si scopre il resto, e la texture si ridisegna solo allora")]
        public IEnumerator Crypt_RevealsWhileWalking()
        {
            yield return LoadGeneratedCore();
            var view = View;
            var exploration = Tracker.Exploration;

            Assert.IsNotNull(exploration, "la cripta ha la sua mappa");
            Assert.IsTrue(view.Corner.IsShowing);
            Assert.IsFalse(view.Overlay.IsShowing);
            Vector2Int start = Exploration.CellAt(Player.position);
            Assert.IsTrue(exploration.IsExplored(start.x, start.y));
            Vector2Int stairs = Find(exploration.Map, DungeonGenerator.StairsSymbol);
            Assert.IsFalse(exploration.IsExplored(stairs.x, stairs.y), "la scala è nella stanza più lontana");

            int explored = exploration.ExploredCount;
            int repaints = view.Repaints;
            // due celle a sud della scala, fuori dal trigger che fa scendere; una sola se lì c'è un ostacolo
            var map = exploration.Map;
            int y = map.IsFloor(stairs.x, stairs.y + 2) && !DungeonPopulator.IsObstacle(map.GetSymbol(stairs.x, stairs.y + 2))
                && map.GetSymbol(stairs.x, stairs.y + 2) != DungeonPopulator.ChestSymbol ? stairs.y + 2 : stairs.y + 1;
            PlayerAgent.Warp(LevelMap.CellCenter(stairs.x, y));
            yield return null;
            yield return null;

            Assert.IsTrue(exploration.IsExplored(stairs.x, stairs.y));
            Assert.Greater(exploration.ExploredCount, explored);
            Assert.Greater(view.Repaints, repaints);

            repaints = view.Repaints;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Assert.AreEqual(repaints, view.Repaints, "fermo, la mappa non si ridisegna");
            foreach (var graphic in view.GetComponentsInChildren<Graphic>(true))
            {
                Assert.IsFalse(graphic.raycastTarget, $"{graphic.name} si prenderebbe i click destinati al mondo");
            }
        }

        [UnityTest, Description("Tab nasconde e mostra, F passa alla vista sovrapposta, dove il cavaliere resta al centro e la mappa scorre")]
        public IEnumerator Keys_ToggleAndSwitchViews()
        {
            yield return LoadGeneratedCore();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var view = View;

            PressAndRelease(keyboard.tabKey);
            yield return null;
            Assert.IsFalse(view.Corner.IsShowing || view.Overlay.IsShowing, "Tab la nasconde");

            PressAndRelease(keyboard.tabKey);
            yield return null;
            Assert.IsTrue(view.Corner.IsShowing, "e la rimostra nell'angolo");

            PressAndRelease(keyboard.fKey);
            yield return null;
            Assert.IsTrue(view.Overlay.IsShowing);
            Assert.IsFalse(view.Corner.IsShowing);
            Assert.AreEqual(-view.Overlay.Dot.anchoredPosition, view.Overlay.MapImage.rectTransform.anchoredPosition, "il punto del cavaliere cade al centro");

            PlayerAgent.Warp(Player.position + new Vector3(2f, 0f, 0f));
            yield return null;
            Assert.AreEqual(-view.Overlay.Dot.anchoredPosition, view.Overlay.MapImage.rectTransform.anchoredPosition, "anche dopo un passo");

            PressAndRelease(keyboard.tabKey);
            yield return null;
            PressAndRelease(keyboard.tabKey);
            yield return null;
            Assert.IsTrue(view.Overlay.IsShowing, "riaccesa, resta nella vista scelta");

            PressAndRelease(keyboard.tabKey);
            yield return null;
            PressAndRelease(keyboard.fKey);
            yield return null;
            Assert.IsTrue(view.Corner.IsShowing, "F da spenta la riaccende nell'altra vista");
        }

        [UnityTest, Description("Un livello fatto a mano non ha la mappa a runtime: l'automappa resta spenta e Tab non rompe niente")]
        public IEnumerator HandmadeLevel_HasNoAutomap()
        {
            yield return LoadCore();
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Assert.IsNull(Tracker.Exploration);
            Assert.IsFalse(View.Corner.IsShowing || View.Overlay.IsShowing);
            PressAndRelease(keyboard.tabKey);
            PressAndRelease(keyboard.fKey);
            yield return null;
            Assert.IsFalse(View.Corner.IsShowing || View.Overlay.IsShowing);
        }
    }
}
