using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// I riconoscimenti che scorrono dopo la vittoria (D9 della M10) leggono una copia di
    /// <c>CREDITS.md</c> tra gli asset, perché il file nella radice del repo non entra nella build.
    /// Un test controlla che la copia sia uguale all'originale.
    /// </summary>
    public static class CreditsTools
    {
        public const string SourcePath = "CREDITS.md";
        public const string CopyPath = "Assets/_Project/Data/Credits.txt";

        [MenuItem("DarkDescent/Aggiorna i riconoscimenti")]
        public static void UpdateCopy()
        {
            File.Copy(SourcePath, CopyPath, true);
            AssetDatabase.ImportAsset(CopyPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[CreditsTools] {CopyPath} copiato da {SourcePath}");
        }
    }
}
