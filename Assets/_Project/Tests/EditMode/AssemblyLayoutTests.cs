using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class AssemblyLayoutTests
    {
        [Test, Description("DarkDescent.Core ha logica e dati, nessun componente di scena, e non dipende dall'assembly del gioco (ADR-032)")]
        public void Core_HasNoMonoBehaviours()
        {
            var core = typeof(LevelMap).Assembly;
            Assert.AreEqual("DarkDescent.Core", core.GetName().Name);
            Assert.AreSame(core, typeof(CombatFormulas).Assembly);

            var components = core.GetTypes().Where(t => typeof(MonoBehaviour).IsAssignableFrom(t)).Select(t => t.FullName).ToList();
            CollectionAssert.IsEmpty(components, "un MonoBehaviour va nell'assembly DarkDescent");
            CollectionAssert.DoesNotContain(core.GetReferencedAssemblies().Select(a => a.Name).ToList(), "DarkDescent");
        }
    }
}
