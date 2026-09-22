# Convenzioni di progetto

## Naming

| Elemento | Convenzione | Esempio |
|---|---|---|
| Classi, metodi, proprietà, eventi | `PascalCase` | `PlayerMotor`, `TakeDamage`, `IsDead` |
| Campi privati | `_camelCase` | `_currentHealth` |
| Campi privati serializzati | `_camelCase` + `[SerializeField]` | `[SerializeField] private float _moveSpeed;` |
| Parametri e variabili locali | `camelCase` | `damageAmount` |
| Costanti | `PascalCase` | `MaxInventorySlots` |
| Interfacce | `I` + PascalCase | `IDamageable` |
| ScriptableObject (tipo) | suffisso `Definition` | `ItemDefinition`, `AffixDefinition` |
| Prefab | `PascalCase` | `Player`, `Skeleton_Basic` |
| Scene | `PascalCase` | `Sandbox_Combat`, `Town` |

## Regole di codice

1. **Mai campi `public`** su un MonoBehaviour. Usa `[SerializeField] private` per l'esposizione in Inspector e una proprietà `public` in sola lettura se serve dall'esterno.
2. **Niente `GameObject.Find` / `FindObjectOfType`** fuori da `Awake`, e comunque da evitare: preferisci riferimenti serializzati o injection esplicita.
3. **Ogni `+=` su un evento ha il suo `-=`.** Iscrizione in `OnEnable`, disiscrizione in `OnDisable`. Non negoziabile: è la causa numero uno di memory leak in Unity.
4. **La UI non fa polling.** Si iscrive a eventi, non legge dati in `Update`.
5. **`Update` vuoto va cancellato.** Un `Update()` vuoto viene comunque chiamato dall'engine e costa.
6. **Niente allocazioni per-frame** in `Update`: no `new`, no LINQ, no concatenazione di stringhe nei loop caldi.
7. **`Camera` e logica di inseguimento in `LateUpdate`**, mai in `Update`.
8. **ScriptableObject = dati immutabili.** Mai stato runtime dentro un SO: in editor sembra funzionare, in build si rompe.
9. **Un file, una classe.** Il nome del file coincide con il nome della classe.
10. **Namespace** `DarkDescent.<Area>` — es. `DarkDescent.Combat`, `DarkDescent.Items`.

## Commenti

Commenta il **perché**, non il **cosa**. `// incrementa la salute` è rumore.
`// il danno si applica qui e non nell'Animation Event perché il reimport da Mixamo li perde` è informazione.

## Commit

Formato: `tipo: descrizione all'imperativo`

Tipi: `feat`, `fix`, `refactor`, `art`, `docs`, `chore`, `test`

Esempi:
```
feat: movimento click-to-move con NavMeshAgent
fix: jitter della camera spostando il follow in LateUpdate
refactor: EnemyAI da enum switch a classi di stato
```

Un commit = un cambiamento coerente. Committa a fine di ogni sessione, minimo.
