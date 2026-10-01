using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Un numero di danno: segue un punto del mondo che sale e sfuma. Lo muove DamageNumbers,
    /// con un solo Update per tutti i numeri attivi.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DamageNumber : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private RectTransform _rect;
        private Vector3 _worldPosition;
        private Color _color;
        private float _age;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _rect = (RectTransform)transform;
        }

        public void Show(Vector3 worldPosition, float amount, Color color)
        {
            _worldPosition = worldPosition;
            _color = color;
            _age = 0f;
            // SetText con il formato non alloca stringhe, a differenza di ToString()
            _text.SetText("{0}", Mathf.RoundToInt(amount));
            _text.color = color;
        }

        /// <summary>Avanza il numero; false quando ha finito e va restituito al pool.</summary>
        public bool Tick(float deltaTime, float lifetime, float rise, Camera camera, RectTransform container)
        {
            _age += deltaTime;
            float t = _age / lifetime;
            if (t >= 1f)
            {
                return false;
            }

            Vector3 world = _worldPosition + Vector3.up * (rise * t);
            Vector2 screen = camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, null, out Vector2 local);
            _rect.anchoredPosition = local;

            // pieno per metà vita, poi sfuma
            _color.a = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            _text.color = _color;
            return true;
        }
    }
}
