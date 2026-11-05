using System;
using DarkDescent.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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


        /// <summary>Un tasto da 1 a 8: il posto della cintura da bere, da 0 (D14 della M6).</summary>
        public event Action<int> BeltSlotUsed;

        /// <summary>M: l'automappa passa alla modalità dopo, tra angolo, sovrapposta e spenta (D15 della M6).</summary>
        public event Action MapCycled;

        /// <summary>La rotella: +1 per uno scatto in avanti, che avvicina, -1 indietro (D11 della M8).</summary>
        public event Action<int> ZoomScrolled;

        /// <summary>Z e X: -1 gira la visuale a sinistra, +1 a destra, di 90° (D12 della M8; Q ed E fino alla M20).</summary>
        public event Action<int> ViewRotated;

        /// <summary>
        /// Il tasto destro, Q o E: usa l'abilità della casella verso il cursore, da 1 a 3 (D18 della
        /// M20). La casella 0, del sinistro, la usa il click su un nemico.
        /// </summary>
        public event Action<int> SkillPressed;

        /// <summary>K: apre e chiude il menu delle abilità, con le quattro caselle (D18 della M20).</summary>
        public event Action SkillsToggled;

        /// <summary>N: apre e chiude il pannello delle abilità del personaggio (D10 della M20).</summary>
        public event Action TalentsToggled;

        /// <summary>T: la magia del portale per l'accampamento (passo 11.7 della M11).</summary>
        public event Action PortalPressed;

        /// <summary>`Esc`: chiude le finestre aperte, o apre e chiude la pausa (D10 della M10).</summary>
        public event Action PausePressed;

        /// <summary>`Alt` premuto o rilasciato: i nomi di tutti gli oggetti a terra (D1 della M10).</summary>
        public event Action<bool> LabelsHeldChanged;

        public bool IsMoveCommandHeld { get; private set; }

        public bool IsLabelsHeld { get; private set; }

        public Vector2 PointerScreenPosition => _controls.Gameplay.Point.ReadValue<Vector2>();

        /// <summary>W A S D e le frecce (D2 della M23): x a destra, y in alto sullo schermo, lunghezza al più 1. Zero con le azioni spente.</summary>
        public Vector2 MoveKeys => _controls.Gameplay.Move2D.ReadValue<Vector2>();

        /// <summary>
        /// Il nome del tasto di un'azione come lo vede il giocatore («I», «Alt», «1»), letto dal binding: se un giorno i
        /// tasti cambiano, cambiano anche i suggerimenti (D12 della M23). Il primo tasto, o l'ultimo con <paramref name="last"/>;
        /// vuoto se l'azione non c'è o non ha tasti.
        /// </summary>
        public string KeyText(string action, bool last = false)
        {
            var found = _controls.asset.FindAction("Gameplay/" + action);
            if (found == null)
            {
                return string.Empty;
            }

            string text = string.Empty;
            foreach (var binding in found.bindings)
            {
                if (binding.isComposite || binding.isPartOfComposite)
                {
                    continue;
                }

                text = Describe(binding);
                if (!last)
                {
                    break;
                }
            }

            return text;
        }

        /// <summary>I quattro tasti di W A S D, in quest'ordine (su, sinistra, giù, destra), letti dal primo composto di <c>Move2D</c>.</summary>
        public string MoveKeysText()
        {
            var found = _controls.asset.FindAction("Gameplay/Move2D");
            if (found == null)
            {
                return string.Empty;
            }

            string up = string.Empty, down = string.Empty, left = string.Empty, right = string.Empty;
            bool inFirst = false;
            foreach (var binding in found.bindings)
            {
                if (binding.isComposite)
                {
                    if (inFirst)
                    {
                        break;
                    }

                    inFirst = true;
                    continue;
                }

                if (!inFirst || !binding.isPartOfComposite)
                {
                    continue;
                }

                switch (binding.name)
                {
                    case "up": up = Describe(binding); break;
                    case "down": down = Describe(binding); break;
                    case "left": left = Describe(binding); break;
                    case "right": right = Describe(binding); break;
                }
            }

            return up + " " + left + " " + down + " " + right;
        }

        // «Left Alt» diventa «Alt»: il tasto di sinistra e quello di destra fanno la stessa cosa
        private static string Describe(InputBinding binding)
        {
            string text = InputControlPath.ToHumanReadableString(binding.effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
            if (text.StartsWith("Left ", StringComparison.Ordinal))
            {
                text = text.Substring(5);
            }

            return text;
        }

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
            _controls.Gameplay.UseBelt.performed += HandleBeltPerformed;
            _controls.Gameplay.CycleMap.performed += HandleMapPerformed;
            _controls.Gameplay.Zoom.performed += HandleZoomPerformed;
            _controls.Gameplay.Rotate.performed += HandleRotatePerformed;
            _controls.Gameplay.Cast.performed += HandleCastPerformed;
            _controls.Gameplay.UseSkill.performed += HandleUseSkillPerformed;
            _controls.Gameplay.ToggleSkills.performed += HandleToggleSkillsPerformed;
            _controls.Gameplay.ToggleTalents.performed += HandleToggleTalentsPerformed;
            _controls.Gameplay.ShowLabels.performed += HandleLabelsPerformed;
            _controls.Gameplay.ShowLabels.canceled += HandleLabelsCanceled;
            _controls.Gameplay.Pause.performed += HandlePausePerformed;
            _controls.Gameplay.TownPortal.performed += HandlePortalPerformed;
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
            _controls.Gameplay.UseBelt.performed -= HandleBeltPerformed;
            _controls.Gameplay.CycleMap.performed -= HandleMapPerformed;
            _controls.Gameplay.Zoom.performed -= HandleZoomPerformed;
            _controls.Gameplay.Rotate.performed -= HandleRotatePerformed;
            _controls.Gameplay.Cast.performed -= HandleCastPerformed;
            _controls.Gameplay.UseSkill.performed -= HandleUseSkillPerformed;
            _controls.Gameplay.ToggleSkills.performed -= HandleToggleSkillsPerformed;
            _controls.Gameplay.ToggleTalents.performed -= HandleToggleTalentsPerformed;
            _controls.Gameplay.ShowLabels.performed -= HandleLabelsPerformed;
            _controls.Gameplay.ShowLabels.canceled -= HandleLabelsCanceled;
            _controls.Gameplay.Pause.performed -= HandlePausePerformed;
            _controls.Gameplay.TownPortal.performed -= HandlePortalPerformed;

            // rete di sicurezza: il reader non deve mai ripartire "premuto" alla riattivazione
            IsMoveCommandHeld = false;
            IsLabelsHeld = false;
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

        private void HandlePausePerformed(InputAction.CallbackContext context)
        {
            PressPause();
        }

        /// <summary>Come se si premesse `Esc`: per i test.</summary>
        public void PressPause()
        {
            PausePressed?.Invoke();
        }

        private void HandleLabelsPerformed(InputAction.CallbackContext context)
        {
            SetLabelsHeld(true);
        }

        private void HandleLabelsCanceled(InputAction.CallbackContext context)
        {
            SetLabelsHeld(false);
        }

        /// <summary>Come se `Alt` fosse premuto o rilasciato: per i test.</summary>
        public void SetLabelsHeld(bool held)
        {
            if (held == IsLabelsHeld)
            {
                return;
            }

            IsLabelsHeld = held;
            LabelsHeldChanged?.Invoke(held);
        }

        private void HandleInventoryPerformed(InputAction.CallbackContext context)
        {
            InventoryToggled?.Invoke();
        }

        private void HandleCharacterPerformed(InputAction.CallbackContext context)
        {
            CharacterToggled?.Invoke();
        }

        // un'azione sola con otto tasti: il posto si ricava dal tasto premuto, da 1 a 8 consecutivi nell'enum
        private void HandleBeltPerformed(InputAction.CallbackContext context)
        {
            if (context.control is KeyControl key)
            {
                UseBeltSlot(key.keyCode - Key.Digit1);
            }
        }

        private void HandleMapPerformed(InputAction.CallbackContext context)
        {
            MapCycled?.Invoke();
        }

        // la rotella dà 120 a scatto su Windows e 1 altrove: conta solo il verso
        private void HandleZoomPerformed(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();
            if (value != 0f)
            {
                ZoomScrolled?.Invoke(value > 0f ? 1 : -1);
            }
        }

        // Z e X sono i due capi di un asse: -1 e +1. Con tutti e due premuti l'asse torna a 0 e non gira
        private void HandleRotatePerformed(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();
            if (value != 0f)
            {
                ViewRotated?.Invoke(value > 0f ? 1 : -1);
            }
        }

        private void HandleCastPerformed(InputAction.CallbackContext context)
        {
            PressSkill(1);
        }

        // un'azione sola con due tasti: Q è la casella 2, E la 3
        private void HandleUseSkillPerformed(InputAction.CallbackContext context)
        {
            if (context.control is KeyControl key)
            {
                PressSkill(key.keyCode == Key.Q ? 2 : 3);
            }
        }

        private void HandleToggleSkillsPerformed(InputAction.CallbackContext context)
        {
            SkillsToggled?.Invoke();
        }

        private void HandleToggleTalentsPerformed(InputAction.CallbackContext context)
        {
            TalentsToggled?.Invoke();
        }

        /// <summary>Come premere il tasto di una casella (1 il destro, 2 Q, 3 E): per i test.</summary>
        public void PressSkill(int slot)
        {
            SkillPressed?.Invoke(slot);
        }

        private void HandlePortalPerformed(InputAction.CallbackContext context)
        {
            PressPortal();
        }

        /// <summary>Come premere T: per i test.</summary>
        public void PressPortal()
        {
            PortalPressed?.Invoke();
        }

        /// <summary>Come premere il tasto del posto (da 0): per i test, che non hanno una tastiera.</summary>
        public void UseBeltSlot(int slot)
        {
            BeltSlotUsed?.Invoke(slot);
        }
    }
}
