using System;
using DarkDescent.Combat;

namespace DarkDescent.UI
{
    /// <summary>
    /// I gestori che <see cref="DamageNumbers"/> iscrive a una vita e al suo scudo, tenuti insieme
    /// per togliere gli stessi delegati che ha messo.
    /// </summary>
    internal sealed class DamageNumberHandlers
    {
        public DamageNumberHandlers(Action<DamageInfo, float> damaged, Action<DamageInfo> evaded, Action<DamageInfo> blocked,
            Action<DamageInfo> pierced, Action<float> healed, ShieldBlock block)
        {
            Damaged = damaged;
            Evaded = evaded;
            Blocked = blocked;
            Pierced = pierced;
            Healed = healed;
            Block = block;
        }

        public Action<DamageInfo, float> Damaged { get; }

        public Action<DamageInfo> Evaded { get; }

        public Action<DamageInfo> Blocked { get; }

        /// <summary>Lo scudo passato da un colpo imparabile.</summary>
        public Action<DamageInfo> Pierced { get; }

        /// <summary>La vita ripresa; null per il cavaliere.</summary>
        public Action<float> Healed { get; }

        /// <summary>Lo scudo della vita seguita, o null.</summary>
        public ShieldBlock Block { get; }
    }
}
