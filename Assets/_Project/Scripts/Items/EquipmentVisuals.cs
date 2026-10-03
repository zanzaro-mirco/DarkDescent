using DarkDescent.Characters;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Mostra in mano al cavaliere quello che ha equipaggiato: il modello dell'oggetto sulle ossa
    /// handslot del rig KayKit, fatte apposta per armi e scudi. Si crea un modello solo quando
    /// cambia l'equipaggiamento, mai per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public class EquipmentVisuals : MonoBehaviour
    {
        [SerializeField] private PlayerInventory _inventory;

        [Tooltip("Osso della mano destra (handslot.r): l'arma.")]
        [SerializeField] private Transform _mainHand;

        [Tooltip("Osso della mano sinistra (handslot.l): lo scudo.")]
        [SerializeField] private Transform _offHand;

        [Tooltip("Renderer del corpo: i modelli in mano ne prendono i rendering layer, così la luce di riempimento illumina anche loro (ADR-016).")]
        [SerializeField] private Renderer _layerSource;

        [Tooltip("Il lampo del colpo subito deve includere anche i modelli in mano.")]
        [SerializeField] private HitFlash _hitFlash;

        private GameObject _mainHandModel;
        private GameObject _offHandModel;

        public GameObject MainHandModel => _mainHandModel;

        public GameObject OffHandModel => _offHandModel;

        private void OnEnable()
        {
            _inventory.Equipment.Changed += Refresh;
            // l'equipaggiamento può essere cambiato mentre il componente era spento
            Refresh(EquipSlot.Weapon);
            Refresh(EquipSlot.Offhand);
        }

        private void OnDisable()
        {
            _inventory.Equipment.Changed -= Refresh;
        }

        private void Refresh(EquipSlot slot)
        {
            if (slot == EquipSlot.Weapon)
            {
                _mainHandModel = Replace(_mainHandModel, slot, _mainHand);
            }
            else if (slot == EquipSlot.Offhand)
            {
                _offHandModel = Replace(_offHandModel, slot, _offHand);
            }

            if (_hitFlash != null)
            {
                _hitFlash.RefreshRenderers();
            }
        }

        private GameObject Replace(GameObject current, EquipSlot slot, Transform hand)
        {
            ItemDefinition definition = _inventory.Equipment.Get(slot)?.Definition;
            GameObject model = definition != null ? definition.Model : null;

            // stesso modello di prima: niente da rifare
            if (current != null && model != null && current.name == model.name)
            {
                return current;
            }

            if (current != null)
            {
                // subito, non a fine frame: il lampo raccoglie i renderer appena dopo
                DestroyImmediate(current);
            }

            if (model == null)
            {
                return null;
            }

            var instance = Instantiate(model, hand, false);
            instance.name = model.name;
            // ogni oggetto sa come si tiene: lo scudo, per esempio, sta fuori dal braccio e non dentro
            instance.transform.SetLocalPositionAndRotation(definition.HeldPosition, Quaternion.Euler(definition.HeldRotation));
            if (_layerSource != null)
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    renderer.renderingLayerMask = _layerSource.renderingLayerMask;
                }
            }

            return instance;
        }
    }
}
