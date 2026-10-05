using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Una pozione (D14 della scheda M6): si beve dalla cintura con i tasti 1–8, o con il click destro
    /// nell'inventario, e rende una parte della vita massima, subito. Non si indossa e non ha affissi.
    /// </summary>
    [CreateAssetMenu(fileName = "Potion", menuName = "DarkDescent/Items/Potion")]
    public sealed class PotionDefinition : ItemDefinition
    {
        [Tooltip("Quanta vita rende, in frazione della vita massima: 0,5 è metà, come la pozione di cura di Diablo.")]
        [SerializeField, Range(0f, 1f)] private float _healFraction = 0.5f;

        public float HealFraction => _healFraction;

        public override EquipSlot Slot => EquipSlot.None;

        public override AffixTargets AffixTarget => AffixTargets.None;

        /// <summary>La vita che rende a chi ne ha <paramref name="maxLife"/> al massimo: intera e almeno 1, come i danni (ADR-031).</summary>
        public float HealAmount(float maxLife)
        {
            return Math.Max(1f, (float)Math.Round(maxLife * _healFraction));
        }
    }
}
