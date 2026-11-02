using System.Collections.Generic;
using System.Linq;
using DarkDescent.Characters;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// I corpi del giocatore (D2 della M21): i pezzi del Mago, legati alle stesse ossa del cavaliere, dentro
    /// la <c>Model</c> del prefab, spenti; e il <see cref="PlayerBody"/> che li accende secondo la classe. I
    /// modelli KayKit hanno tutti lo stesso scheletro, quindi le clip, le mani per le armi e il lampo del
    /// colpo restano quelli di prima. Si rilancia: rifà i pezzi del Mago da capo.
    /// </summary>
    public static class PlayerBodyTools
    {
        private const string PlayerPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string MagePath = "Assets/_Project/Art/Models/KayKit/Mage.fbx";
        private const string MageMaterial = "Assets/_Project/Art/Materials/M_KayKitMage.mat";

        [MenuItem("DarkDescent/Classi/Corpo del Mago sul rig")]
        public static void Bake()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(MagePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MageMaterial);
            var contents = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var animator = contents.GetComponentInChildren<Animator>(true);
                var model = animator.transform;

                // via i pezzi del Mago di un giro prima
                foreach (var old in model.Cast<Transform>().Where(c => c.name.StartsWith("Mage_")).ToList())
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var knightParts = model.Cast<Transform>().Where(c => c.name.StartsWith("Knight_")).Select(c => c.gameObject).ToArray();
                var reference = knightParts.Select(p => p.GetComponent<SkinnedMeshRenderer>()).First(r => r != null);

                // le ossa del cavaliere, per nome: sono le stesse del Mago
                var bones = new Dictionary<string, Transform>();
                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                {
                    if (t.GetComponent<SkinnedMeshRenderer>() == null && !bones.ContainsKey(t.name))
                    {
                        bones[t.name] = t;
                    }
                }

                var magePieces = new List<GameObject>();
                foreach (var from in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var go = new GameObject(from.name);
                    go.transform.SetParent(model, false);
                    go.layer = reference.gameObject.layer;
                    var to = go.AddComponent<SkinnedMeshRenderer>();
                    to.sharedMesh = from.sharedMesh;
                    to.sharedMaterials = Enumerable.Repeat(material, from.sharedMaterials.Length).ToArray();
                    to.bones = from.bones.Select(b => bones[b.name]).ToArray();
                    to.rootBone = bones[from.rootBone.name];
                    to.localBounds = from.localBounds;
                    to.shadowCastingMode = reference.shadowCastingMode;
                    to.receiveShadows = reference.receiveShadows;
                    to.renderingLayerMask = reference.renderingLayerMask;
                    to.lightProbeUsage = reference.lightProbeUsage;
                    to.motionVectorGenerationMode = reference.motionVectorGenerationMode;
                    to.updateWhenOffscreen = reference.updateWhenOffscreen;
                    go.SetActive(false);
                    magePieces.Add(go);
                }

                var body = contents.GetComponentInChildren<PlayerBody>(true) ?? model.gameObject.AddComponent<PlayerBody>();
                body.Define(new[]
                {
                    new PlayerBody.BodySet { Id = "knight", Parts = knightParts },
                    new PlayerBody.BodySet { Id = "mage", Parts = magePieces.ToArray() },
                }, model.GetComponent<HitFlash>());
                EditorUtility.SetDirty(body);

                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPath);
                Debug.Log($"[PlayerBodyTools] corpi sul rig: Cavaliere ({knightParts.Length} pezzi) e Mago ({magePieces.Count}), ossa {bones.Count}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
