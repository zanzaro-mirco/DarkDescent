using DarkDescent.Player;
using UnityEngine;

namespace DarkDescent.Interaction
{
    /// <summary>
    /// Il santuario (D6 della M13): una stele con un cristallo del suo colore. Dà al cavaliere il bonus
    /// del suo tipo per un minuto, al posto di quello che aveva; poi il cristallo si spegne.
    /// </summary>
    [DisallowMultipleComponent]
    public class Shrine : OneUseFeature
    {
        [SerializeField] private ShrineDefinition _definition;

        public ShrineDefinition Definition => _definition;

        protected override string LabelKey => _definition != null ? _definition.LabelKey : string.Empty;

        protected override float UsePitch => _definition != null ? _definition.ChimePitch : 1f;

        protected override bool TryApply(GameObject user)
        {
            if (_definition == null || !user.TryGetComponent(out ShrineBonus bonus))
            {
                return false;
            }

            bonus.Apply(_definition);
            return bonus.Current == _definition;
        }
    }
}
