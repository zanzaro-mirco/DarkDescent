namespace DarkDescent.Combat
{
    public static class DamageableExtensions
    {
        /// <summary>
        /// Vivo e non distrutto. Su un'interfaccia "== null" è il confronto di C#, che non vede
        /// gli oggetti Unity distrutti (null "finto"): il cast a UnityEngine.Object usa quello di Unity.
        /// </summary>
        public static bool IsAlive(this IDamageable damageable)
        {
            if (damageable is UnityEngine.Object unityObject)
            {
                return unityObject != null && !damageable.IsDead;
            }

            return damageable != null && !damageable.IsDead;
        }
    }
}
