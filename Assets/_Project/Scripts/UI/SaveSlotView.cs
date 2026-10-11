using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Un posto di salvataggio nel menu (D2 della M10): il riepilogo, cliccabile per sceglierlo, e
    /// accanto il tasto per cancellarlo.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveSlotView : MonoBehaviour
    {
        [SerializeField] private Button _button;

        [SerializeField] private TMP_Text _text;

        [SerializeField] private Button _delete;

        [SerializeField, Range(1, 3)] private int _slot = 1;

        public event Action<int> Clicked;

        public event Action<int> DeleteClicked;

        public int Slot => _slot;

        public string Text => _text.text;

        public Button Button => _button;

        public Button DeleteButton => _delete;

        /// <summary>Il riepilogo; <paramref name="selectable"/> spegne il click (un posto vuoto da caricare), <paramref name="deletable"/> il tasto per cancellare.</summary>
        public void Show(string text, bool selectable, bool deletable)
        {
            _text.text = text;
            _button.interactable = selectable;
            _delete.gameObject.SetActive(deletable);
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);
            _delete.onClick.AddListener(HandleDelete);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);
            _delete.onClick.RemoveListener(HandleDelete);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(_slot);
        }

        private void HandleDelete()
        {
            DeleteClicked?.Invoke(_slot);
        }
    }
}
