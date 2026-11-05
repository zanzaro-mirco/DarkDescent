using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// I tasti del gioco (D18 della M20): ogni tasto fa una cosa sola, e quelli che Mirco ha chiesto stanno dove
    /// devono: Q ed E le abilità, Z e X la camera, K il menu, T il portale; F1–F8 non scelgono più niente.
    /// </summary>
    public class InputBindingsTests
    {
        // il file .inputactions è JSON: lo si legge com'è, senza il pacchetto dell'Input System nell'assembly dei test
        [Serializable] private class BindingData { public string path; public string action; public bool isComposite; }

        [Serializable] private class ActionData { public string name; }

        [Serializable] private class MapData { public string name; public ActionData[] actions; public BindingData[] bindings; }

        [Serializable] private class AssetData { public MapData[] maps; }

        private MapData _gameplay;

        [OneTimeSetUp]
        public void Load()
        {
            string json = System.IO.File.ReadAllText("Assets/_Project/Settings/PlayerControls.inputactions");
            _gameplay = JsonUtility.FromJson<AssetData>(json).maps.Single(map => map.name == "Gameplay");
        }

        private IEnumerable<(string Action, string Path)> Bindings() =>
            _gameplay.bindings.Where(b => !b.isComposite && !string.IsNullOrEmpty(b.path)).Select(b => (b.action, b.path));

        [Test, Description("Nessun tasto è legato a due azioni diverse: i tasti di prima non si pestano i piedi con quelli nuovi")]
        public void NoKeyIsBoundToTwoActions()
        {
            var clashes = Bindings().GroupBy(b => b.Path).Where(g => g.Select(b => b.Action).Distinct().Count() > 1)
                .Select(g => $"{g.Key}: {string.Join(", ", g.Select(b => b.Action).Distinct())}").ToList();
            Assert.IsEmpty(clashes, string.Join("; ", clashes));
        }

        [Test, Description("Q ed E usano le abilità, Z e X girano la camera, K apre il menu delle caselle, N il pannello delle abilità, T il portale, il destro lancia, F1 apre il pannello dei comandi (M23); F2–F8 e SelectSpell non ci sono più")]
        public void KeysAreWhereTheyShouldBe()
        {
            string ActionOf(string path) => Bindings().Where(b => b.Path == path).Select(b => b.Action).FirstOrDefault();
            Assert.AreEqual("UseSkill", ActionOf("<Keyboard>/q"));
            Assert.AreEqual("UseSkill", ActionOf("<Keyboard>/e"));
            Assert.AreEqual("Rotate", ActionOf("<Keyboard>/z"));
            Assert.AreEqual("Rotate", ActionOf("<Keyboard>/x"));
            Assert.AreEqual("ToggleSkills", ActionOf("<Keyboard>/k"));
            Assert.AreEqual("ToggleTalents", ActionOf("<Keyboard>/n"));
            Assert.AreEqual("TownPortal", ActionOf("<Keyboard>/t"));
            Assert.AreEqual("Cast", ActionOf("<Mouse>/rightButton"));
            Assert.IsFalse(_gameplay.actions.Any(action => action.name == "SelectSpell"));
            Assert.AreEqual("ToggleHelp", ActionOf("<Keyboard>/f1"));
            for (int key = 2; key <= 8; key++)
            {
                Assert.IsNull(ActionOf($"<Keyboard>/f{key}"), $"F{key}");
            }
        }
    }
}
