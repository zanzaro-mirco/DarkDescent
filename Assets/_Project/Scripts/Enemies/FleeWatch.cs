using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// I nemici di un livello che si guardano a vicenda (D3 e D5 della M24): chi scappa quando muoiono i compagni vuole sapere
    /// quanti sono morti vicino e da poco, e chi finisce di scappare chiama i compagni dello stesso tipo. Come
    /// <see cref="EnemyPack"/> lo crea il composition root a ogni livello e lo rilascia all'uscita; senza, un nemico scappa
    /// solo per la vita.
    /// </summary>
    public sealed class FleeWatch
    {
        private struct Death
        {
            public float Time;
            public Vector3 Position;
            public EnemyArchetype Archetype;
        }

        private readonly List<EnemyAI> _members = new List<EnemyAI>();
        private readonly List<Death> _deaths = new List<Death>();

        public FleeWatch(IReadOnlyList<EnemyAI> enemies)
        {
            foreach (var enemy in enemies)
            {
                // un nemico già morto e sparito resta nella lista del livello come null di Unity
                if (enemy != null && enemy.Archetype != null && enemy.Archetype.CanFlee)
                {
                    _members.Add(enemy);
                    enemy.Killed += HandleKilled;
                    enemy.BindWatch(this);
                }
            }
        }

        public int Count => _members.Count;

        /// <summary>Si stacca da tutti, anche da quelli già distrutti: il -= su un oggetto distrutto è innocuo.</summary>
        public void Release()
        {
            foreach (var enemy in _members)
            {
                if (enemy != null)
                {
                    enemy.Killed -= HandleKilled;
                    enemy.BindWatch(null);
                }
            }

            _members.Clear();
            _deaths.Clear();
        }

        /// <summary>Quanti compagni dello stesso tipo sono morti, vicino a <paramref name="who"/> e da poco (i numeri sono quelli del suo archetipo).</summary>
        public int RecentLosses(EnemyAI who, float now)
        {
            var archetype = who.Archetype;
            float radiusSquared = archetype.FleeLossRadius * archetype.FleeLossRadius;
            _deaths.RemoveAll(d => now - d.Time > archetype.FleeLossWindow);
            int count = 0;
            foreach (var death in _deaths)
            {
                if (death.Archetype == archetype && (death.Position - who.transform.position).sqrMagnitude <= radiusSquared)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Sveglia i compagni dello stesso tipo, fermi, entro <paramref name="radius"/> metri: accorrono. Chi insegue già non cambia.</summary>
        public int Call(EnemyAI caller, float radius)
        {
            int called = 0;
            float radiusSquared = radius * radius;
            foreach (var other in _members)
            {
                if (other != null && other != caller && other.Archetype == caller.Archetype
                    && (other.transform.position - caller.transform.position).sqrMagnitude <= radiusSquared)
                {
                    other.Alert();
                    called++;
                }
            }

            return called;
        }

        private void HandleKilled(EnemyAI enemy)
        {
            _deaths.Add(new Death { Time = Time.time, Position = enemy.transform.position, Archetype = enemy.Archetype });
        }
    }
}
