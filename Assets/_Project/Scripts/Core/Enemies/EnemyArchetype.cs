using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Un tipo di nemico come combinazione di stati e numeri (D8 della M7). Lo scheletro ha fermo,
    /// inseguimento, attacco e morte; sciame e bruto aggiungono numeri e stati loro, e ognuno ha i suoi
    /// versi: quando viene colpito e quando il colpo è critico. Dati immutabili.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Enemy Archetype", fileName = "EnemyArchetype")]
    public class EnemyArchetype : ScriptableObject
    {
        [Tooltip("Il nome nella tabella delle stringhe: lo mostra l'HUD quando il nemico è sotto il cursore.")]
        [SerializeField] private string _nameKey;

        [Tooltip("Distanza entro cui il bersaglio viene notato, se non c'è un ostacolo in mezzo.")]
        [SerializeField, Min(0f)] private float _aggroRange = 8f;

        [Tooltip("Inseguendo, oltre questa distanza da dove l'ha messo il livello lascia il cavaliere e torna a casa, dove guarisce (seconda prova della build M7): 20 m, lo sciame che caccia in branco 30. 0: insegue ovunque.")]
        [SerializeField, Min(0f)] private float _leashRange = 20f;

        [Tooltip("Altezza degli occhi per il controllo della vista, dal pivot ai piedi.")]
        [SerializeField, Min(0f)] private float _eyeHeight = 1.5f;

        [Tooltip("Ogni quanti secondi, da fermo, si controlla se il bersaglio è visibile.")]
        [SerializeField, Min(0.02f)] private float _perceptionInterval = 0.2f;

        [Tooltip("Secondi in cui il corpo resta a terra prima di sparire.")]
        [SerializeField, Min(0f)] private float _corpseLifetime = 5f;

        [Header("Branco (D5 della M7)")]
        [Tooltip("Chi vede il bersaglio avvisa i compagni dello stesso tipo entro questi metri, che lo inseguono anche senza vederlo. Zero: ognuno per conto suo.")]
        [SerializeField, Min(0f)] private float _packRadius;

        [Tooltip("La priorità di evitamento dell'agent varia di tanto in più o in meno da un nemico all'altro: in un corridoio uno cede il passo invece di spingere (trappola 5).")]
        [SerializeField, Range(0, 49)] private int _avoidanceSpread;

        [Header("Colpo telegrafato (D6 della M7)")]
        [Tooltip("Secondi fermo dopo un colpo, la finestra per colpirlo. Zero: nessuna carica né recupero, come lo scheletro. La carica dura quanto il ritardo dell'arma.")]
        [SerializeField, Min(0f)] private float _recoverTime;

        [Header("Versi del critico (D13 della M7)")]
        [Tooltip("Il verso di questo tipo di nemico quando il cavaliere lo colpisce di critico, al posto dell'impatto.")]
        [SerializeField] private AudioClip[] _criticalVoices;

        [Tooltip("Intonazione del verso: bassa per il bruto, acuta per lo sciame.")]
        [SerializeField] private Vector2 _criticalPitch = Vector2.one;

        [Header("Versi quando viene colpito (prova della M7)")]
        [Tooltip("Il verso di questo tipo di nemico a ogni colpo che non sia critico, insieme all'impatto.")]
        [SerializeField] private AudioClip[] _hurtVoices;

        [SerializeField] private Vector2 _hurtPitch = Vector2.one;

        [Tooltip("Più piano del critico: il critico deve restare il verso che si nota.")]
        [SerializeField, Range(0f, 1f)] private float _hurtVolume = 0.6f;

        public string NameKey => _nameKey;

        public int HurtVoiceCount => _hurtVoices != null ? _hurtVoices.Length : 0;

        public Vector2 HurtPitch => _hurtPitch;

        public float HurtVolume => _hurtVolume;

        public AudioClip GetHurtVoice(int index)
        {
            return _hurtVoices[index];
        }

        public int CriticalVoiceCount => _criticalVoices != null ? _criticalVoices.Length : 0;

        public Vector2 CriticalPitch => _criticalPitch;

        public AudioClip GetCriticalVoice(int index)
        {
            return _criticalVoices[index];
        }

        public float PackRadius => _packRadius;

        public int AvoidanceSpread => _avoidanceSpread;

        public float RecoverTime => _recoverTime;

        public bool IsTelegraphed => _recoverTime > 0f;

        public float AggroRange => _aggroRange;

        public float LeashRange => _leashRange;

        public float EyeHeight => _eyeHeight;

        public float PerceptionInterval => _perceptionInterval;

        public float CorpseLifetime => _corpseLifetime;

        /// <summary>Gli stati di un nemico di questo tipo: nuovi a ogni chiamata, perché ogni nemico ha i suoi.</summary>
        public IEnumerable<EnemyStateBase> CreateStates()
        {
            if (!IsTelegraphed)
            {
                return new EnemyStateBase[] { new IdleState(_perceptionInterval), new ChaseState(), new AttackState(), new ReturnState(), new DeadState() };
            }

            return new EnemyStateBase[]
            {
                new IdleState(_perceptionInterval), new ChaseState(), new AttackState(telegraphed: true),
                new WindUpState(), new RecoverState(_recoverTime), new ReturnState(), new DeadState(),
            };
        }
    }
}
