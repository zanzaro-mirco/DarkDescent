using System;
using DarkDescent.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkDescent.Player
{
    /// <summary>
    /// Traduce l'input grezzo in intenzioni. Non conosce il mondo di gioco:
    /// espone solo eventi e la posizione del puntatore in coordinate schermo.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputReader : MonoBehaviour
    {
        public event Action MoveCommandStarted;
        public event Action MoveCommandCanceled;

        /// <summary>Tasto dell'inventario (I).</summary>
        public event Action InventoryToggled;

        /// <summary>Tasto del pannello del personaggio (C).</summary>
        public event Action CharacterToggled;

        /// <summary>Il tasto provvisorio delle lingue (F9), finché non c'è il menu delle opzioni.</summary>
        public event Action LanguageCycled;

        public bool IsMoveCommandHeld { get; private set; }

        public Vector2 PointerScreenPosition => _controls.Gameplay.Point.ReadValue<Vector2>();

        private PlayerControls _controls;

        private void Awake()
        {
            // un'istanza per reader: la classe generata crea una copia privata dell'asset,
            // quindi due reader non si disabilitano le azioni a vicenda
            _controls = new PlayerControls();
        }

        private void OnEnable()
        {
            _controls.Gameplay.Move.performed += HandleMovePerformed;
            _controls.Gameplay.Move.canceled += HandleMoveCanceled;
            _controls.Gameplay.ToggleInventory.performed += HandleInventoryPerformed;
            _controls.Gameplay.ToggleCharacter.performed += HandleCharacterPerformed;
            _controls.Gameplay.CycleLanguage.performed += HandleLanguagePerformed;
            _controls.Gameplay.Enable();
        }

        private void OnDisable()
        {
            // prima Disable, poi la disiscrizione: se il tasto è premuto, Disable emette il canceled
            // e chi ascolta viene avvisato del rilascio
            _controls.Gameplay.Disable();
            _controls.Gameplay.Move.performed -= HandleMovePerformed;
            _controls.Gameplay.Move.canceled -= HandleMoveCanceled;
            _controls.Gameplay.ToggleInventory.performed -= HandleInventoryPerformed;
            _controls.Gameplay.ToggleCharacter.performed -= HandleCharacterPerformed;
            _controls.Gameplay.CycleLanguage.performed -= HandleLanguagePerformed;

            // rete di sicurezza: il reader non deve mai ripartire "premuto" alla riattivazione
            IsMoveCommandHeld = false;
        }

        private void OnDestroy()
        {
            // l'asset copiato da new PlayerControls() è un ScriptableObject: senza Dispose resta in memoria
            _controls.Dispose();
        }

        // performed e non started: su un Button started scatta appena il controllo si muove,
        // performed quando supera il press point. Col mouse coincidono, con un grilletto analogico no.
        private void HandleMovePerformed(InputAction.CallbackContext context)
        {
            IsMoveCommandHeld = true;
            MoveCommandStarted?.Invoke();
        }

        private void HandleMoveCanceled(InputAction.CallbackContext context)
        {
            IsMoveCommandHeld = false;
            MoveCommandCanceled?.Invoke();
        }

        private void HandleInventoryPerformed(InputAction.CallbackContext context)
        {
            InventoryToggled?.Invoke();
        }

        private void HandleCharacterPerformed(InputAction.CallbackContext context)
        {
            CharacterToggled?.Invoke();
        }

        private void HandleLanguagePerformed(InputAction.CallbackContext context)
        {
            LanguageCycled?.Invoke();
        }
    }
}
