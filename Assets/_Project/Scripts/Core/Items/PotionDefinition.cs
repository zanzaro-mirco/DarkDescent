using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace DarkDescent.Items
{
    /// <summary>
    /// Una pozione (D14 della scheda M6): si beve dalla cintura con i tasti 1–8, o con il click destro
    /// nell'inventario, e rende subito una parte della vita massima, o del mana massimo quella blu
    /// (D2 della M9). Non si indossa e non ha affissi.
    /// </summary>
    [CreateAssetMenu(fileName = "Potion", menuName = "DarkDescent/Items/Potion")]
    public sealed class PotionDefinition : ItemDefinition
    {
        [Tooltip("Che cosa rende: la vita o il mana.")]
        [SerializeField] private PotionKind _kind = PotionKind.Life;

        [Tooltip("Quanto rende, in frazione del massimo: 0,5 è metà, come le pozioni di Diablo.")]
        [FormerlySerializedAs("_healFraction")]
        [SerializeField, Range(0f, 1f)] private float _restoreFraction = 0.5f;

        public PotionKind Kind => _kind;

        public float RestoreFraction => _restoreFraction;

        public override EquipSlot Slot => EquipSlot.None;

        public override AffixTargets AffixTarget => AffixTargets.None;

        /// <summary>Quanto rende a chi ne ha <paramref name="max"/> al massimo: intero e almeno 1, come i danni (ADR-031).</summary>
        public float RestoreAmount(float max)
        {
            return Math.Max(1f, (float)Math.Round(max * _restoreFraction));
        }
    }
}
