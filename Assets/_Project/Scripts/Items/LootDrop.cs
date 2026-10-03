using DarkDescent.Combat;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Alla morte lascia un oggetto a terra, davanti al corpo. Nella M4 l'oggetto è sempre lo stesso
    /// (lo scheletro lascia la sua lama, D8): le tabelle di loot casuali sono il lavoro della M5.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class LootDrop : MonoBehaviour
    {
        [SerializeField] private ItemDefinition _item;

        [SerializeField] private GroundItem _groundItemPrefab;

        [Tooltip("Quanto davanti al corpo cade l'oggetto: sotto il corpo non si vedrebbe e non si cliccherebbe.")]
        [SerializeField, Min(0f)] private float _forwardOffset = 0.9f;

        private Health _health;

        public ItemDefinition Item => _item;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            _health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            if (_item != null && _groundItemPrefab != null)
            {
                GroundItem.Spawn(_groundItemPrefab, new ItemInstance(_item), transform.position + transform.forward * _forwardOffset);
            }
        }
    }
}
