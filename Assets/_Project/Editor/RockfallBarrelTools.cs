using System;
using System.Linq;
using DarkDescent.Items;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Il barile del crollo (D11 della M25): nella composizione <c>CaveDecor_RockfallC</c> il barile piccolo era un modello con un collider fermo. Qui diventa un barile vero,
    /// un'istanza di <c>Barrel_Small</c> con la sua rottura, i suoi pezzi e il suo suono, ma senza contenuto (nessuna tabella di loot e niente oggetto a terra): il livello e i
    /// suoi premi restano quelli di prima. Poggia dove stava il vecchio, a terra. Si rilancia: il barile si rifà e niente si duplica. In batchmode:
    /// <c>-executeMethod DarkDescent.Editor.RockfallBarrelTools.Build</c>.
    /// </summary>
    public static class RockfallBarrelTools
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Caves/Decor/CaveDecor_RockfallC.prefab";
        private const string BarrelPath = "Assets/_Project/Prefabs/Dungeon/Barrel_Small.prefab";
        private const string OldBarrel = "barrel_small_2";
        private const string OldCollider = "Collider_barrel_small";
        private const string NewBarrel = "RockfallBarrel";

        [MenuItem("DarkDescent/Segreti/Barile del crollo: crea o aggiorna")]
        public static void Build()
        {
            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BarrelPath);
            if (barrelPrefab == null)
            {
                throw new InvalidOperationException("Manca " + BarrelPath);
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                // dove stava il vecchio: il centro del suo ingombro in pianta e il suo piede
                var old = root.transform.Find(OldBarrel);
                Vector3 spot;
                if (old != null)
                {
                    var bounds = old.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                    spot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                }
                else
                {
                    var existing = root.transform.Find(NewBarrel);
                    spot = existing != null ? existing.position : throw new InvalidOperationException("Il barile del crollo non c'è né vecchio né nuovo");
                }

                var oldCollider = root.transform.Find(OldCollider);
                if (oldCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldCollider.gameObject);
                }

                var previous = root.transform.Find(NewBarrel);
                if (previous != null)
                {
                    UnityEngine.Object.DestroyImmediate(previous.gameObject);
                }

                var barrel = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, root.transform);
                barrel.name = NewBarrel;
                barrel.transform.position = Vector3.zero;
                float foot = barrel.GetComponentsInChildren<Renderer>().Where(r => r.gameObject.name == "Intact").Select(r => r.bounds.min.y).DefaultIfEmpty(0f).Min();
                barrel.transform.position = new Vector3(spot.x, spot.y - foot, spot.z);

                // il barile del crollo è vuoto: la stessa rottura degli altri, niente tabella e niente oggetto a terra
                var breakable = barrel.GetComponent<Breakable>();
                var so = new SerializedObject(breakable);
                so.FindProperty("_lootTable").objectReferenceValue = null;
                so.FindProperty("_groundItemPrefab").objectReferenceValue = null;
                so.FindProperty("_canExplode").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.Refresh();
            Debug.Log("[DarkDescent] Barile del crollo: ora è un barile vero");
        }
    }
}
