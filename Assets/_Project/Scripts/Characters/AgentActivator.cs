using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Characters
{
    /// <summary>
    /// Accende in Start il NavMeshAgent, che nel prefab parte spento. In build l'agent nativo si
    /// accende al caricamento della scena, prima che NavMeshSurface carichi i dati del NavMesh nel
    /// suo OnEnable, e il log riporta "Failed to create agent". Start arriva dopo tutti gli OnEnable
    /// della scena. Nell'editor non succede perché i dati sono già caricati in edit mode.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentActivator : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<NavMeshAgent>().enabled = true;
        }
    }
}
