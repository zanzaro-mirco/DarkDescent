using DarkDescent.Magic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Una pergamena (D5 della M9): si legge con il click destro nell'inventario, insegna il suo
    /// incantesimo e sparisce. Non si indossa e non ha affissi.
    /// </summary>
    [CreateAssetMenu(fileName = "Scroll", menuName = "DarkDescent/Items/Scroll")]
    public sealed class ScrollDefinition : ItemDefinition
    {
        [Tooltip("L'incantesimo che insegna.")]
        [SerializeField] private SpellDefinition _spell;

        public SpellDefinition Spell => _spell;

        public override EquipSlot Slot => EquipSlot.None;

        public override AffixTargets AffixTarget => AffixTargets.None;
    }
}
