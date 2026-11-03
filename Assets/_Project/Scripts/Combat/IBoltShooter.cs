using UnityEngine;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Chi lancia il Dardo arcano del Mago (M21) quando il suo colpo arriva: <see cref="MeleeAttack"/> sceglie
    /// il bersaglio, aspetta il gesto e tira il danno; questo lo fa volare fino a lui.
    /// </summary>
    public interface IBoltShooter
    {
        /// <summary>Lancia il dardo verso <paramref name="target"/> con questo danno: lo fa arrivare il proiettile.</summary>
        void Shoot(Transform target, float damage);
    }
}
