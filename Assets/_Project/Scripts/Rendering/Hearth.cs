using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Il fuoco della piazza del borgo, che nei filmati si può spegnere (D7 della M19): all'alba i
    /// sopravvissuti siedono attorno al fuoco spento. Spento restano i sassi, senza fiamme e senza luce;
    /// nel gioco brucia sempre.
    /// </summary>
    [DisallowMultipleComponent]
    public class Hearth : MonoBehaviour
    {
        [Tooltip("La luce del fuoco.")]
        [SerializeField] private Light _light;

        [Tooltip("I modelli delle fiamme, che spariscono con il fuoco spento.")]
        [SerializeField] private Renderer[] _flames = new Renderer[0];

        public bool IsBurning { get; private set; } = true;

        public void SetBurning(bool burning)
        {
            IsBurning = burning;
            if (_light != null)
            {
                _light.enabled = burning;
            }

            foreach (var flame in _flames)
            {
                if (flame != null)
                {
                    flame.enabled = burning;
                }
            }
        }
    }
}
