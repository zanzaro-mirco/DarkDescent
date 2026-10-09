using System;
using DarkDescent.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Gira la camera attorno al cavaliere a scatti di 90° con Q ed E (D12 della M8). Cambia solo
    /// l'orientamento: il Position Composer di Cinemachine porta la camera attorno al punto seguito
    /// senza smorzamento. Chi dipende dai lati lontani (muri, automappa) ascolta gli eventi.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    public class CameraRotation : MonoBehaviour
    {
        private readonly ViewRotation _rotation = new ViewRotation();
        private PlayerInputReader _reader;
        private float _pitch;
        private bool _subscribed;

        /// <summary>A metà di uno scatto, quando i lati lontani diventano altri: il valore è il lato da cui si guarda.</summary>
        public event Action<int> FacingChanged;

        /// <summary>A ogni frame della rotazione, con l'imbardata in gradi.</summary>
        public event Action<float> YawChanged;

        public int Facing => _rotation.Facing;

        public float Yaw => _rotation.Yaw;

        public bool IsTurning => _rotation.IsTurning;

        /// <summary>Come l'automappa: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerInputReader reader)
        {
            Unsubscribe();
            _reader = reader;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Uno scatto: +1 a destra (E), -1 a sinistra (Q). Anche per i test.</summary>
        public void Turn(int direction)
        {
            _rotation.Turn(direction);
        }

        private void Awake()
        {
            // l'inclinazione è quella della scena; l'imbardata parte da nord-est, come prima della M8
            _pitch = transform.eulerAngles.x;
            Apply();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // prima del LateUpdate di Cinemachine, che legge l'orientamento dal transform; senza tempo
        // scalato, come lo zoom
        private void Update()
        {
            int facing = _rotation.Facing;
            if (!_rotation.Advance(Time.unscaledDeltaTime))
            {
                return;
            }

            Apply();
            YawChanged?.Invoke(_rotation.Yaw);
            if (_rotation.Facing != facing)
            {
                FacingChanged?.Invoke(_rotation.Facing);
            }
        }

        private void Subscribe()
        {
            if (_subscribed || _reader == null)
            {
                return;
            }

            _reader.ViewRotated += Turn;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _reader.ViewRotated -= Turn;
            _subscribed = false;
        }

        private void Apply()
        {
            transform.rotation = Quaternion.Euler(_pitch, _rotation.Yaw, 0f);
        }
    }
}
