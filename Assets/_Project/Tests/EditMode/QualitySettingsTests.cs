using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>Le impostazioni di qualità delle build (D6 della M11).</summary>
    public class QualitySettingsTests
    {
        [Test, Description("La qualità PC tiene il vSync acceso: senza, il gioco gira a centinaia di fps e scalda la GPU per niente")]
        public void PcQuality_HasVSync()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var levels = settings.FindProperty("m_QualitySettings");
            for (int i = 0; i < levels.arraySize; i++)
            {
                var level = levels.GetArrayElementAtIndex(i);
                if (level.FindPropertyRelative("name").stringValue == "PC")
                {
                    Assert.AreEqual(1, level.FindPropertyRelative("vSyncCount").intValue);
                    return;
                }
            }

            Assert.Fail("manca la qualità PC");
        }
    }
}
