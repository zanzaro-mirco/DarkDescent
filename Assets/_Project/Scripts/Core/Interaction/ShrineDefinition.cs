using UnityEngine;

namespace DarkDescent.Interaction
{
    /// <summary>
    /// Un tipo di santuario (D6 della M13): il bonus che dà, per quanto, il suo nome e il messaggio
    /// all'uso, il colore dell'icona e il materiale del cristallo. Furia, Vento, Pietra e Sapienza
    /// sono quattro asset in <c>Data/Shrines</c>. Non cambia a runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Shrine", fileName = "Shrine")]
    public class ShrineDefinition : ScriptableObject
    {
        [SerializeField] private ShrineEffect _effect;

        [Tooltip("Il bonus in punti percentuali: 25 vuol dire +25%.")]
        [SerializeField, Min(0f)] private float _percent = 25f;

        [Tooltip("Quanto dura il bonus, in secondi.")]
        [SerializeField, Min(1f)] private float _duration = 60f;

        [Tooltip("Il nome sul santuario; usato, quello con \".spent\" in fondo.")]
        [SerializeField] private string _labelKey;

        [Tooltip("Il messaggio al centro dello schermo quando si usa.")]
        [SerializeField] private string _messageKey;

        [Tooltip("Il colore dell'icona accanto alla cintura e della luce del santuario.")]
        [SerializeField] private Color _color = Color.white;

        [Tooltip("Il materiale del cristallo acceso.")]
        [SerializeField] private Material _crystal;

        [Tooltip("Il tono del rintocco: ogni santuario suona un po' diverso (D14).")]
        [SerializeField, Range(0.5f, 2f)] private float _chimePitch = 1f;

        public ShrineEffect Effect => _effect;

        public float Percent => _percent;

        public float Duration => _duration;

        public string LabelKey => _labelKey;

        public string SpentLabelKey => _labelKey + ".spent";

        public string MessageKey => _messageKey;

        public Color Color => _color;

        public Material Crystal => _crystal;

        public float ChimePitch => _chimePitch;
    }
}
