using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// I branchi di un livello (D5 della M7): quando un nemico vede il bersaglio con i suoi occhi,
    /// avvisa i compagni dello stesso tipo entro il raggio del branco del suo archetipo, che inseguono
    /// anche senza averlo visto. Chi è avvisato non avvisa a sua volta: un gruppo si sveglia, non il
    /// livello intero. Lo crea il composition root a ogni livello e lo rilascia all'uscita.
    /// </summary>
    public sealed class EnemyPack
    {
        private readonly List<EnemyAI> _members = new List<EnemyAI>();

        public EnemyPack(IReadOnlyList<EnemyAI> enemies)
        {
            foreach (var enemy in enemies)
            {
                // un nemico già morto e sparito resta nella lista del livello come null di Unity
                if (enemy != null && enemy.Archetype.PackRadius > 0f)
                {
                    _members.Add(enemy);
                    enemy.Spotted += HandleSpotted;
                }
            }
        }

        public int Count => _members.Count;

        /// <summary>Si stacca da tutti, anche da quelli già distrutti: il -= su un oggetto distrutto è innocuo.</summary>
        public void Release()
        {
            foreach (var enemy in _members)
            {
                enemy.Spotted -= HandleSpotted;
            }

            _members.Clear();
        }

        private void HandleSpotted(EnemyAI spotter)
        {
            float radius = spotter.Archetype.PackRadius;
            Vector3 from = spotter.transform.position;
            foreach (var other in _members)
            {
                if (other != null && other != spotter && other.Archetype == spotter.Archetype
                    && (other.transform.position - from).sqrMagnitude <= radius * radius)
                {
                    other.Alert();
                }
            }
        }
    }
}
