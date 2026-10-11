using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace DarkDescent.Magic
{
    /// <summary>
    /// I proiettili di tutti, presi da un <see cref="ObjectPool{T}"/> per prefab (D7 della M9): un
    /// lancio a raffica non crea né distrugge niente dopo i primi. Sta nella scena Core, che resta,
    /// così un livello che se ne va non porta via i proiettili del pool.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProjectilePools : MonoBehaviour
    {
        [Tooltip("Quanti proiettili per prefab il pool tiene da parte; oltre si distruggono al ritorno.")]
        [SerializeField, Min(1)] private int _maxPerPrefab = 32;

        private readonly Dictionary<GameObject, ObjectPool<Projectile>> _pools = new Dictionary<GameObject, ObjectPool<Projectile>>();

        // la funzione che rimette un proiettile nel suo pool, una per pool: creata a ogni lancio allocherebbe
        private readonly Dictionary<GameObject, System.Action<Projectile>> _releases = new Dictionary<GameObject, System.Action<Projectile>>();
        private readonly HashSet<Projectile> _active = new HashSet<Projectile>();

        /// <summary>I proiettili in volo o in esplosione: per i test.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>Quanti ne sono stati creati in tutto, per prefab: per i test del pool.</summary>
        public int CreatedCount(GameObject prefab) => _pools.TryGetValue(prefab, out var pool) ? pool.CountAll : 0;

        /// <summary>
        /// Lancia un proiettile del <paramref name="prefab"/>; torna nel pool da solo dopo l'esplosione.
        /// Con <paramref name="blockRandom"/> si può parare con lo scudo.
        /// </summary>
        public Projectile Launch(GameObject prefab, Vector3 from, Vector3 direction, float speed, float range, float damage, GameObject source, int targetLayers, IRandomSource blockRandom = null)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<Projectile>(
                    () => Instantiate(prefab, transform).GetComponent<Projectile>(),
                    projectile => projectile.gameObject.SetActive(true),
                    projectile => projectile.gameObject.SetActive(false),
                    projectile => Destroy(projectile.gameObject),
                    collectionCheck: false,
                    defaultCapacity: 8,
                    maxSize: _maxPerPrefab);
                _pools.Add(prefab, pool);
                var owner = pool;
                _releases.Add(prefab, projectile => Release(owner, projectile));
            }

            var launched = pool.Get();
            _active.Add(launched);
            launched.Launch(from, direction, speed, range, damage, source, targetLayers, _releases[prefab], blockRandom);
            return launched;
        }

        private void Release(ObjectPool<Projectile> pool, Projectile projectile)
        {
            if (_active.Remove(projectile))
            {
                pool.Release(projectile);
            }
        }
    }
}
