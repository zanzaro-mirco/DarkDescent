using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Un tipo di nemico come combinazione di stati e numeri (D8 della M7). Lo scheletro ha fermo,
    /// inseguimento, attacco e morte; sciame e bruto aggiungeranno numeri e stati loro (passi 7.4 e
    /// 7.5), i versi del critico arriveranno al 7.7. Dati immutabili.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Enemy Archetype", fileName = "EnemyArchetype")]
    public class EnemyArchetype : ScriptableObject
    {
        [Tooltip("Distanza entro cui il bersaglio viene notato, se non c'è un ostacolo in mezzo.")]
        [SerializeField, Min(0f)] private float _aggroRange = 8f;

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

        public float PackRadius => _packRadius;

        public int AvoidanceSpread => _avoidanceSpread;

        public float RecoverTime => _recoverTime;

        public bool IsTelegraphed => _recoverTime > 0f;

        public float AggroRange => _aggroRange;

        public float EyeHeight => _eyeHeight;

        public float PerceptionInterval => _perceptionInterval;

        public float CorpseLifetime => _corpseLifetime;

        /// <summary>Gli stati di un nemico di questo tipo: nuovi a ogni chiamata, perché ogni nemico ha i suoi.</summary>
        public IEnumerable<EnemyStateBase> CreateStates()
        {
            if (!IsTelegraphed)
            {
                return new EnemyStateBase[] { new IdleState(_perceptionInterval), new ChaseState(), new AttackState(), new DeadState() };
            }

            return new EnemyStateBase[]
            {
                new IdleState(_perceptionInterval), new ChaseState(), new AttackState(telegraphed: true),
                new WindUpState(), new RecoverState(_recoverTime), new DeadState(),
            };
        }
    }
}
