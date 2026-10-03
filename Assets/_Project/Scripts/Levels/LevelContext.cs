using System.Collections.Generic;
using DarkDescent.Enemies;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Il "chi c'è" di un livello: ingressi e nemici della sua scena. Sta su un oggetto alla radice
    /// della scena del livello, dove il LevelManager lo cerca dopo il caricamento.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelContext : MonoBehaviour
    {
        private readonly List<LevelEntrance> _entrances = new List<LevelEntrance>();
        private readonly List<EnemyAI> _enemies = new List<EnemyAI>();

        /// <summary>I nemici presenti al caricamento. Quelli morti e spariti restano nella lista come null di Unity.</summary>
        public IReadOnlyList<EnemyAI> Enemies => _enemies;

        public int EntranceCount => _entrances.Count;

        private void Awake()
        {
            // tutta la scena, non solo i figli: nemici e ingressi non devono stare sotto questo oggetto
            var entrances = new List<LevelEntrance>();
            var enemies = new List<EnemyAI>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                root.GetComponentsInChildren(true, entrances);
                _entrances.AddRange(entrances);
                root.GetComponentsInChildren(true, enemies);
                _enemies.AddRange(enemies);
            }
        }

        public bool HasEntrance(string id)
        {
            return FindEntrance(id) != null;
        }

        /// <summary>
        /// L'ingresso con quell'id; se non c'è, il primo del livello. Succede nell'editor quando si
        /// preme Play con un livello aperto che non ha l'ingresso iniziale.
        /// </summary>
        public Transform GetEntrance(string id)
        {
            var entrance = FindEntrance(id);
            if (entrance != null)
            {
                return entrance.transform;
            }

            if (_entrances.Count == 0)
            {
                Debug.LogError($"Il livello {gameObject.scene.name} non ha ingressi.", this);
                return transform;
            }

            Debug.LogWarning($"Ingresso \"{id}\" assente in {gameObject.scene.name}: uso \"{_entrances[0].Id}\".", this);
            return _entrances[0].transform;
        }

        private LevelEntrance FindEntrance(string id)
        {
            foreach (var entrance in _entrances)
            {
                if (entrance.Id == id)
                {
                    return entrance;
                }
            }

            return null;
        }
    }
}
