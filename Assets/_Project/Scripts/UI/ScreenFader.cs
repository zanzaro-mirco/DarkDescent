using System.Collections;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Dissolvenza a nero su tutto lo schermo, per i cambi di livello. In tempo non scalato, come
    /// l'hit stop: un timeScale rimasto basso non deve rallentarla. Non blocca mai i click.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _duration = 0.35f;

        [Tooltip("Nero all'avvio: il primo livello compare con una dissolvenza invece che di colpo.")]
        [SerializeField] private bool _startOpaque = true;

        private CanvasGroup _group;

        public float Alpha => _group.alpha;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = _startOpaque ? 1f : 0f;
        }

        public IEnumerator FadeTo(float alpha)
        {
            float start = _group.alpha;
            if (_duration <= 0f || Mathf.Approximately(start, alpha))
            {
                _group.alpha = alpha;
                yield break;
            }

            for (float t = 0f; t < _duration; t += Time.unscaledDeltaTime)
            {
                _group.alpha = Mathf.Lerp(start, alpha, t / _duration);
                yield return null;
            }

            _group.alpha = alpha;
        }
    }
}
