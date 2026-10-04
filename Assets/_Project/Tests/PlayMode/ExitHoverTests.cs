using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class ExitHoverTests : SandboxFixture
    {
        private LevelExit _exit;
        private Interactable _interactable;
        private InteractableHighlight _highlight;
        private InteractableLabel _label;

        // il cavaliere va vicino alla scala, perché la camera la inquadri; poi un secondo di attesa
        // perché la camera arrivi
        private IEnumerator LoadNearStairs()
        {
            yield return LoadCore();
            _exit = Object.FindFirstObjectByType<LevelManager>().CurrentLevel.Exits[0];
            _interactable = _exit.GetComponent<Interactable>();
            _highlight = _exit.GetComponent<InteractableHighlight>();
            _label = Object.FindFirstObjectByType<InteractableLabel>();
            PlayerAgent.Warp(_interactable.ApproachPoint + _exit.transform.forward * 3f);
            yield return new WaitForSeconds(1f);
        }

        [UnityTest, Description("Il cursore sulla scala la accende e mostra dove porta; spostato altrove, tutto si spegne")]
        public IEnumerator HoverOnStairs_HighlightsAndShowsLabel()
        {
            yield return LoadNearStairs();
            Assert.IsFalse(_label.IsShown, "all'inizio l'etichetta è nascosta");

            PointAt(_exit.transform.position);
            yield return null;
            yield return null;

            Assert.IsTrue(_interactable.IsHighlighted);
            Assert.IsTrue(_highlight.IsShowing, "la scala si accende");
            Assert.IsTrue(_label.IsShown);
            Assert.AreEqual("Descend to level 2", _label.Text);

            PointAt(_interactable.ApproachPoint + _exit.transform.forward * 3f);
            yield return null;
            yield return null;

            Assert.IsFalse(_interactable.IsHighlighted);
            Assert.IsFalse(_highlight.IsShowing);
            Assert.IsFalse(_label.IsShown);
        }

        [UnityTest, Description("Se il cavaliere muore con il cursore sulla scala, l'evidenziazione si spegne")]
        public IEnumerator PlayerDies_ClearsHover()
        {
            yield return LoadNearStairs();
            PointAt(_exit.transform.position);
            yield return null;
            yield return null;
            Assert.IsTrue(_interactable.IsHighlighted);

            Player.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            yield return null;

            Assert.IsFalse(_interactable.IsHighlighted);
            Assert.IsFalse(_highlight.IsShowing);
            Assert.IsFalse(_label.IsShown);
        }
    }
}
