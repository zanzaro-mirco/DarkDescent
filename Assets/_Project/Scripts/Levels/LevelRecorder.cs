using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Items;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Ricorda com'erano i livelli lasciati e li rimette così rientrandoci (10.9 della M10), come in
    /// Diablo 1: il seme rigenera il livello, poi i nemici uccisi spariscono, le casse aperte restano
    /// aperte, i barili rotti (D5 della M13) restano a pezzi, pozzi e santuari usati restano vuoti, e
    /// gli oggetti a terra tornano dov'erano, anche quelli messi dalla mappa. Prima del 10.9 ogni
    /// livello ripartiva nuovo, e le casse ridavano gli stessi oggetti. Lo chiama il composition
    /// root: <see cref="Restore"/> appena caricato il livello, prima di collegare i nemici, e
    /// <see cref="Remember"/> prima di scaricarlo. I ricordi vanno nel salvataggio.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelRecorder : MonoBehaviour
    {
        [SerializeField] private GroundItem _groundItemPrefab;

        [Tooltip("La definizione dell'oro: le pile tornano a terra con questa.")]
        [SerializeField] private GoldDefinition _gold;

        [SerializeField] private ItemDatabase _items;

        [SerializeField] private AffixDatabase _affixes;

        private readonly List<GroundItem> _found = new List<GroundItem>();

        // il livello rimesso com'era: solo lui si può ricordare. Uno appena caricato e non ancora
        // rimesso è quello nuovo del seme, e ricordarlo cancellerebbe il ricordo vero
        private LevelContext _current;

        public LevelMemory Memory { get; } = new LevelMemory();

        /// <summary>Rimette il livello appena caricato com'era l'ultima volta; niente se è la prima.</summary>
        public void Restore(LevelContext level)
        {
            _current = level;
            if (!Memory.TryGet(level.gameObject.scene.name, level.Depth, out LevelRecord record))
            {
                return;
            }

            // gli oggetti messi dalla mappa ci sono già: li rimette il ricordo, con quelli caduti
            foreach (var ground in GroundItems(level))
            {
                ground.gameObject.SetActive(false);
                Destroy(ground.gameObject);
            }

            // spenti subito e distrutti a fine frame: fino ad allora contano ancora, e la pergamena del
            // livello sceglie lo stesso portatore della prima visita (D5 della M9)
            if (record.Matches(level.Enemies.Count, level.Chests.Count))
            {
                foreach (int index in record.Killed)
                {
                    var enemy = level.Enemies[index];
                    if (enemy != null)
                    {
                        enemy.gameObject.SetActive(false);
                        Destroy(enemy.gameObject);
                    }
                }

                foreach (int index in record.Opened)
                {
                    level.Chests[index].RestoreOpen();
                }
            }
            else
            {
                Debug.LogWarning($"[DarkDescent] il ricordo di {record.Scene} a profondità {record.Depth} non corrisponde al livello generato: nemici e casse ripartono nuovi.", this);
            }

            // contati a parte: un ricordo di prima della M13 non ha barili, e questi ripartono interi
            if (record.MatchesBreakables(level.Breakables.Count))
            {
                foreach (int index in record.Broken)
                {
                    level.Breakables[index].RestoreBroken();
                }
            }

            if (record.MatchesFeatures(level.Features.Count))
            {
                foreach (int index in record.Spent)
                {
                    level.Features[index].RestoreSpent();
                }
            }

            foreach (var item in record.Items)
            {
                if (item.Gold > 0)
                {
                    GroundItem.RestoreGold(_groundItemPrefab, _gold, item.Gold, item.Position);
                }
                else if (item.Item != null && item.Item.Resolve(_items, _affixes))
                {
                    GroundItem.Restore(_groundItemPrefab, item.Item, item.Position);
                }
            }
        }

        /// <summary>Ricorda il livello che si sta per lasciare.</summary>
        public void Remember(LevelContext level)
        {
            if (level != null && level == _current)
            {
                Memory.Remember(Record(level));
            }

            _current = null;
        }

        /// <summary>I ricordi per il salvataggio, con quello del livello di adesso se è già rimesso.</summary>
        public List<LevelRecord> Export(LevelContext current)
        {
            if (current != null && current == _current)
            {
                Memory.Remember(Record(current));
            }

            return Memory.Export();
        }

        private LevelRecord Record(LevelContext level)
        {
            var record = new LevelRecord(level.gameObject.scene.name, level.Depth, level.Enemies.Count, level.Chests.Count, level.Breakables.Count,
                level.Features.Count);
            for (int i = 0; i < level.Enemies.Count; i++)
            {
                // un nemico morto da un po' è già sparito
                var enemy = level.Enemies[i];
                if (enemy == null || !enemy.gameObject.activeSelf || enemy.GetComponent<Health>().IsDead)
                {
                    record.AddKilled(i);
                }
            }

            for (int i = 0; i < level.Chests.Count; i++)
            {
                if (level.Chests[i] != null && level.Chests[i].IsOpen)
                {
                    record.AddOpened(i);
                }
            }

            for (int i = 0; i < level.Breakables.Count; i++)
            {
                // con la miccia ancora accesa non è esploso: al ritorno è intero
                if (level.Breakables[i] != null && level.Breakables[i].IsBroken && !level.Breakables[i].IsFusing)
                {
                    record.AddBroken(i);
                }
            }

            for (int i = 0; i < level.Features.Count; i++)
            {
                if (level.Features[i] != null && level.Features[i].IsSpent)
                {
                    record.AddSpent(i);
                }
            }

            foreach (var ground in GroundItems(level))
            {
                // raccolto in questo frame, o tolto da Restore: sta per sparire
                var item = ground.Remembered;
                if (item == null || !ground.gameObject.activeSelf)
                {
                    continue;
                }

                Vector3 position = ground.transform.position;
                record.AddItem(ground.Gold > 0 ? new GroundItemRecord(ground.Gold, position) : new GroundItemRecord(item, position));
            }

            return record;
        }

        // nella scena del livello: quelli caduti stanno alla radice, quelli della mappa sotto il livello
        private List<GroundItem> GroundItems(LevelContext level)
        {
            var all = new List<GroundItem>();
            foreach (var root in level.gameObject.scene.GetRootGameObjects())
            {
                root.GetComponentsInChildren(true, _found);
                all.AddRange(_found);
            }

            return all;
        }
    }
}
