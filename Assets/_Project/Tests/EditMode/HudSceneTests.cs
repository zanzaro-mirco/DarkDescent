using System.Linq;
using DarkDescent.Editor;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkDescent.Tests
{
    /// <summary>
    /// La scena Core com'è dopo <c>HudTools.BuildConsole</c> (D18 della M20): console, provette, cintura,
    /// quattro caselle e menu delle abilità tutti collegati. I test PlayMode ci passano da dentro; questi
    /// girano in un secondo e dicono subito quale riferimento è saltato.
    /// </summary>
    public class HudSceneTests
    {
        private Scene _scene;
        private Transform _hud;

        [OneTimeSetUp]
        public void OpenCore()
        {
            _scene = EditorSceneManager.OpenScene(TalentTools.CoreScene, OpenSceneMode.Additive);
            _hud = _scene.GetRootGameObjects().Single(root => root.name == "HUD").transform;
        }

        [OneTimeTearDown]
        public void CloseCore()
        {
            EditorSceneManager.CloseScene(_scene, true);
        }

        private int Index(string name) => _hud.Find(name).GetSiblingIndex();

        private static SerializedProperty Property(Object target, string name) => new SerializedObject(target).FindProperty(name);

        [Test, Description("L'ordine nell'HUD: la console sotto le provette e le caselle, il menu delle abilità sotto la schermata della morte che copre tutto")]
        public void Hud_HasTheRightOrder()
        {
            foreach (string above in new[] { "HealthOrb", "ManaOrb", "Belt", "SpellBar", "PortalSlot", "ExperienceBar" })
            {
                Assert.Less(Index("Console"), Index(above), $"la console sotto {above}");
            }

            Assert.Less(Index("SkillsUI"), Index("DeathScreen"), "il menu sotto la morte");
            Assert.Less(Index("SkillsUI"), Index("ScreenFader"));
            Assert.Less(Index("TalentsUI"), Index("DeathScreen"), "il pannello delle abilità sotto la morte");
            Assert.Less(Index("Console"), Index("TalentsUI"), "e sopra la console");
        }

        [Test, Description("La barra ha le quattro caselle in ordine (sinistro, destro, Q, E), ognuna con icona, nome, tasto, cornice accesa, velo e barra; il menu è collegato")]
        public void SpellBar_HasFourWiredSlots()
        {
            var bar = _hud.Find("SpellBar").GetComponent<SpellBarView>();
            var slots = Property(bar, "_slots");
            Assert.AreEqual(4, slots.arraySize);
            Assert.AreEqual(4, _hud.Find("SpellBar").childCount, "niente caselle in più");
            for (int i = 0; i < 4; i++)
            {
                var slot = (SpellSlotView)slots.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsNotNull(slot, $"casella {i}");
                Assert.AreEqual(i, Property(slot, "_slot").intValue);
                foreach (string field in new[] { "_icon", "_name", "_key", "_selectedFrame", "_cooldown", "_bar" })
                {
                    Assert.IsNotNull(Property(slot, field).objectReferenceValue, $"casella {i}, {field}");
                }

                Assert.IsFalse(((Image)Property(slot, "_selectedFrame").objectReferenceValue).enabled, "la cornice accesa a riposo è spenta");
            }

            Assert.IsNotNull(Property(bar, "_menu").objectReferenceValue);
        }

        [Test, Description("Il menu delle abilità: finestra spenta, titolo, quattro righe con titolo, scelte e scritta vuota, e la scelta modello spenta con le sue parti")]
        public void SkillMenu_IsWired()
        {
            var root = _hud.Find("SkillsUI");
            var menu = root.GetComponent<SkillMenu>();
            Assert.IsNotNull(menu);
            var window = (GameObject)Property(menu, "_window").objectReferenceValue;
            Assert.IsNotNull(window);
            Assert.IsFalse(window.activeSelf, "chiuso all'avvio");
            Assert.IsNotNull(Property(menu, "_title").objectReferenceValue);
            foreach (string array in new[] { "_rows", "_rowTitles", "_rowChoices", "_rowEmpty" })
            {
                var list = Property(menu, array);
                Assert.AreEqual(4, list.arraySize, array);
                for (int i = 0; i < 4; i++)
                {
                    Assert.IsNotNull(list.GetArrayElementAtIndex(i).objectReferenceValue, $"{array}[{i}]");
                }
            }

            var template = (SkillChoiceView)Property(menu, "_choiceTemplate").objectReferenceValue;
            Assert.IsNotNull(template);
            Assert.IsFalse(template.gameObject.activeSelf);
            foreach (string field in new[] { "_icon", "_inner", "_name", "_current" })
            {
                Assert.IsNotNull(Property(template, field).objectReferenceValue, field);
            }

            Assert.AreEqual(8, _hud.Find("Belt").childCount, "otto pozioni nella cintura");
        }

        [Test, Description("Le provette di vita e mana: vetro, liquido a riempimento verticale dal basso e cornice con il loro sprite, e due liquidi diversi")]
        public void Gauges_HaveTheirSprites()
        {
            foreach (string orb in new[] { "HealthOrb", "ManaOrb" })
            {
                var fill = _hud.Find(orb + "/Fill").GetComponent<Image>();
                Assert.AreEqual(Image.Type.Filled, fill.type, orb);
                Assert.AreEqual(Image.FillMethod.Vertical, fill.fillMethod, orb);
                Assert.AreEqual((int)Image.OriginVertical.Bottom, fill.fillOrigin, orb);
                Assert.IsNotNull(fill.sprite, orb);
                Assert.IsNotNull(_hud.Find(orb + "/Back").GetComponent<Image>().sprite, orb);
                Assert.IsNotNull(_hud.Find(orb + "/Ring").GetComponent<Image>().sprite, orb);
            }

            Assert.AreNotSame(_hud.Find("HealthOrb/Fill").GetComponent<Image>().sprite, _hud.Find("ManaOrb/Fill").GetComponent<Image>().sprite);
        }

        [Test, Description("Il pannello delle abilità (D10 della M20): finestra spenta con i due alberi, i modelli spenti, le tre placche e i tre anelli, le due schede, Applica e Annulla, il tooltip e la finestrella delle scelte; l'avviso dei punti e la voce dell'altare sono collegati")]
        public void TalentPanel_IsWired()
        {
            var root = _hud.Find("TalentsUI");
            var panel = root.GetComponent<TalentPanel>();
            Assert.IsNotNull(panel);
            var window = (GameObject)Property(panel, "_window").objectReferenceValue;
            Assert.IsFalse(window.activeSelf, "chiuso all'avvio");
            foreach (Transform child in root)
            {
                Assert.IsFalse(child.gameObject.activeSelf, $"{child.name} spento a riposo: un velo acceso oscurerebbe il gioco");
            }
            foreach (string field in new[] { "_title", "_close", "_nodeTemplate", "_lineTemplate", "_gateTemplate", "_chooseButton", "_specNote", "_apply", "_cancel",
                "_pendingNote", "_hint", "_tooltip", "_tooltipText", "_choicePopup", "_choiceBlocker" })
            {
                Assert.IsNotNull(Property(panel, field).objectReferenceValue, field);
            }

            foreach (string area in new[] { "_classArea", "_specArea" })
            {
                foreach (string part in new[] { "Area", "Lines", "Nodes", "Name", "Points" })
                {
                    Assert.IsNotNull(Property(panel, area).FindPropertyRelative(part).objectReferenceValue, $"{area}.{part}");
                }
            }

            foreach (string array in new[] { "_plates", "_rings", "_specTabs", "_choiceButtons" })
            {
                var list = Property(panel, array);
                Assert.Greater(list.arraySize, 1, array);
                for (int i = 0; i < list.arraySize; i++)
                {
                    Assert.IsNotNull(list.GetArrayElementAtIndex(i).objectReferenceValue, $"{array}[{i}]");
                }
            }

            Assert.AreEqual(3, Property(panel, "_plates").arraySize, "tonda, quadrata, ottagonale");
            foreach (string template in new[] { "_nodeTemplate", "_lineTemplate" })
            {
                Assert.IsFalse(((Component)Property(panel, template).objectReferenceValue).gameObject.activeSelf, template);
            }

            var node = (TalentNodeView)Property(panel, "_nodeTemplate").objectReferenceValue;
            foreach (string field in new[] { "_plate", "_ring", "_icon", "_label", "_rank" })
            {
                Assert.IsNotNull(Property(node, field).objectReferenceValue, field);
            }

            var notice = _hud.Find("TalentPointsButton").GetComponent<TalentPointsButton>();
            foreach (string field in new[] { "_body", "_button", "_label" })
            {
                Assert.IsNotNull(Property(notice, field).objectReferenceValue, $"avviso: {field}");
            }

            var composition = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<DarkDescent.Core.CompositionRoot>(true)).Single();
            Assert.AreSame(panel, Property(composition, "_talentPanel").objectReferenceValue);
            Assert.AreSame(notice, Property(composition, "_talentPointsButton").objectReferenceValue);
        }

        [Test, Description("La voce dell'altare che ricomincia l'albero è collegata nel pannello delle difficoltà, sotto \"Rifai il mondo\"")]
        public void Altar_HasTheTalentEntry()
        {
            var altar = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<DifficultyPanel>(true)).Single();
            var reset = (Button)Property(altar, "_resetTalents").objectReferenceValue;
            var newWorld = (Button)Property(altar, "_newWorld").objectReferenceValue;
            Assert.IsNotNull(reset);
            Assert.AreSame(newWorld.transform.parent, reset.transform.parent);
            Assert.Less(((RectTransform)reset.transform).anchoredPosition.y, ((RectTransform)newWorld.transform).anchoredPosition.y, "sotto");
            Assert.IsNotNull(reset.GetComponentInChildren<TMPro.TMP_Text>(true));
        }
    }
}
