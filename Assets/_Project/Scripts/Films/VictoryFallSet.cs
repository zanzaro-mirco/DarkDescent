using DarkDescent.Levels;
using Unity.Cinemachine;
using UnityEngine;

namespace DarkDescent.Films
{
    /// <summary>
    /// Il primo tempo della vittoria (D8 della M19), nella sala, appena muore il re: il tempo rallenta a
    /// un quarto sul colpo finale, la camera si stringe sul re che si sfalda, e si torna a giocare. Il re
    /// che cade è una copia del suo modello; il vero si nasconde e sparisce con il suo corpo. La copia
    /// resta nella sala come il mucchio d'ossa, che il secondo tempo riprende.
    /// </summary>
    public class VictoryFallSet : FilmSet
    {
        /// <summary>Il nome del mucchio d'ossa del re, che resta nella sala dopo il filmato.</summary>
        public const string RemainsName = "KingRemains";

        // un quarto della velocità per mezzo secondo del filmato, due secondi veri; poi il tempo riparte
        private const float SlowScale = 0.25f;
        private const float SlowSeconds = 0.5f;
        private const float RampSeconds = 0.6f;

        [Header("Animazioni")]
        [SerializeField] private AnimationClip _death;
        [Tooltip("La posa a terra, che il mucchio d'ossa tiene dopo il filmato.")]
        [SerializeField] private AnimationClip _deathPose;

        private FilmActor _king;
        private Transform _level;
        private Vector3 _target;
        private float _yaw;

        public override FilmScript Script => FilmLibrary.VictoryFall;

        protected override void Stage(LevelContext level, GameObject hero, CinemachineCamera gameplay)
        {
            _level = level.transform;
            var boss = level.Boss;
            Transform from = boss != null ? boss.transform : hero.transform;
            if (boss != null)
            {
                _king = FilmActor.Clone(boss.gameObject, transform, RemainsName, _death, _deathPose);
                _king.Place(from.position, from.forward);
                foreach (var part in boss.GetComponentsInChildren<Renderer>())
                {
                    part.enabled = false;
                }
            }

            // di tre quarti, dal lato del cavaliere: il re cade all'indietro, e il centro va un po' oltre
            // di lui; chi ha dato il colpo resta sul bordo
            Vector3 toKing = from.position - hero.transform.position;
            toKing.y = 0f;
            _target = from.position + toKing.normalized * 0.8f + Vector3.up * 0.8f;
            _yaw = toKing.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toKing).eulerAngles.y + 35f : gameplay.transform.eulerAngles.y;
        }

        protected override void Pose(float time)
        {
            time = Mathf.Clamp(time, 0f, Script.Duration);
            float k = time / Script.Duration;

            // fermo resta fermo: il filmato fotografato a un istante, con il tempo a 0
            if (Time.timeScale > 0f)
            {
                Time.timeScale = Mathf.Lerp(SlowScale, 1f, Mathf.Clamp01((time - SlowSeconds) / RampSeconds));
            }

            if (_king != null)
            {
                _king.Pose(_death, time);
            }

            Frame(_target, _yaw, Mathf.Lerp(32f, 26f, Ease(k)), Mathf.Lerp(5.2f, 4.2f, Ease(k)));
        }

        // il mucchio d'ossa resta nella sala, a terra, finché c'è il livello
        protected override void Strike()
        {
            Time.timeScale = 1f;
            if (_king == null || _level == null)
            {
                return;
            }

            _king.transform.SetParent(_level, true);
            _king.Pose(_deathPose, 0f);
            _king = null;
        }
    }
}
