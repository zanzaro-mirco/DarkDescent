using System.IO;
using System.Linq;
using DarkDescent.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>I modelli fatti in Blender da script arrivano in Unity dritti e in metri (D9 della M13).</summary>
    public class GeneratedModelTests
    {
        private static string[] Models => Directory.GetFiles(GeneratedModelImporter.Folder, "*.fbx")
            .Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();

        private static Bounds BoundsOf(GameObject model)
        {
            Bounds? total = null;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                Bounds local = filter.sharedMesh.bounds;
                Matrix4x4 m = filter.transform.localToWorldMatrix;
                foreach (var corner in new[] { -1, 1 }.SelectMany(x => new[] { -1, 1 }.SelectMany(y => new[] { -1, 1 }
                             .Select(z => local.center + Vector3.Scale(local.extents, new Vector3(x, y, z))))))
                {
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (total == null)
                    {
                        total = new Bounds(p, Vector3.zero);
                    }
                    else
                    {
                        Bounds b = total.Value;
                        b.Encapsulate(p);
                        total = b;
                    }
                }
            }

            Assert.IsNotNull(total, "il modello ha almeno una mesh");
            return total.Value;
        }

        [Test, Description("Ogni FBX della cartella ha le impostazioni dei KayKit: niente materiali né animazioni")]
        public void Models_ImportLikeKayKit()
        {
            Assert.IsNotEmpty(Models, "c'è almeno un modello");
            foreach (string path in Models)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(ModelImporterMaterialImportMode.None, importer.materialImportMode, path);
                Assert.IsFalse(importer.importAnimation, path);
                Assert.AreEqual(ModelImporterAnimationType.None, importer.animationType, path);
            }
        }

        [Test, Description("Ogni modello arriva senza rotazione e in scala 1, appoggiato a terra, di una misura da stanza")]
        public void Models_AreUprightAndInMeters()
        {
            foreach (string path in Models)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                {
                    Assert.Less(Quaternion.Angle(Quaternion.identity, t.localRotation), 0.01f, $"{path}: {t.name} è girato");
                    Assert.Less((t.localScale - Vector3.one).magnitude, 1e-4f, $"{path}: {t.name} è scalato");
                }

                Bounds b = BoundsOf(model);
                Assert.AreEqual(0f, b.min.y, 0.01f, $"{path}: la base sta a terra");
                Assert.That(b.size.x, Is.InRange(0.1f, 8f), $"{path}: larghezza");
                Assert.That(b.size.y, Is.InRange(0.1f, 8f), $"{path}: altezza");
                Assert.That(b.size.z, Is.InRange(0.1f, 8f), $"{path}: profondità");
            }
        }

        [Test, Description("Il pozzo è largo 2,38 m e alto 1,07 come in Blender: gli assi non sono scambiati")]
        public void Well_KeepsTheBlenderSize()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedModelImporter.Folder + "Well.fbx");
            Assert.IsNotNull(model);
            Bounds b = BoundsOf(model);
            Assert.AreEqual(2.38f, b.size.x, 0.02f, "larghezza");
            Assert.AreEqual(1.07f, b.size.y, 0.02f, "altezza");
            Assert.AreEqual(2.38f, b.size.z, 0.02f, "profondità");
            Assert.AreEqual(0f, b.center.x, 0.02f, "centrato");
            Assert.AreEqual(0f, b.center.z, 0.02f, "centrato");
        }
    }
}
