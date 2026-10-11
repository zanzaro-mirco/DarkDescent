using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// L'oro a terra (D4 della M10): una pila di monete che si raccoglie con un click e non occupa la
    /// borsa. Non sta nel database degli oggetti e non si equipaggia; la pila cresce con la quantità.
    /// </summary>
    [CreateAssetMenu(fileName = "Gold", menuName = "DarkDescent/Items/Gold")]
    public sealed class GoldDefinition : ItemDefinition
    {
        [Tooltip("La pila media, da questa quantità in su.")]
        [SerializeField] private GameObject _mediumModel;

        [SerializeField, Min(1)] private int _mediumFrom = 20;

        [Tooltip("La pila grande, da questa quantità in su.")]
        [SerializeField] private GameObject _largeModel;

        [SerializeField, Min(1)] private int _largeFrom = 60;

        public override EquipSlot Slot => EquipSlot.None;

        public override AffixTargets AffixTarget => AffixTargets.None;

        /// <summary>Il modello della pila per una quantità: piccola, media o grande.</summary>
        public GameObject ModelFor(int amount)
        {
            if (amount >= _largeFrom && _largeModel != null)
            {
                return _largeModel;
            }

            return amount >= _mediumFrom && _mediumModel != null ? _mediumModel : Model;
        }
    }
}
