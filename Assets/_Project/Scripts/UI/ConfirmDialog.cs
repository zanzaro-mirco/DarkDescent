using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// La domanda prima di un'azione che non si annulla (D2 della M10): sovrascrivere o cancellare un
    /// salvataggio. Sì esegue, No chiude e basta.
    /// </summary>
    [DisallowMultipleComponent]
    public class ConfirmDialog : MonoBehaviour
    {
        [Tooltip("Il riquadro da mostrare; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _root;

        [SerializeField] private TMP_Text _message;

        [SerializeField] private Button _yes;

        [SerializeField] private Button _no;

        private Action _onYes;

        public bool IsOpen => _root.activeSelf;

        public string Message => _message.text;

        public Button Yes => _yes;

        public Button No => _no;

        public void Ask(string message, Action onYes)
        {
            _message.text = message;
            _onYes = onYes;
            _root.SetActive(true);
        }

        public void Close()
        {
            _onYes = null;
            _root.SetActive(false);
        }

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            _yes.onClick.AddListener(HandleYes);
            _no.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _yes.onClick.RemoveListener(HandleYes);
            _no.onClick.RemoveListener(Close);
        }

        private void HandleYes()
        {
            Action action = _onYes;
            Close();
            action?.Invoke();
        }
    }
}
