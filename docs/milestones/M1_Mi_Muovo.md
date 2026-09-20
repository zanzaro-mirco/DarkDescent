# M1 — "Mi muovo"

**Cosa deve succedere a schermo (Definition of Done):**
in una **build eseguibile**, una stanza grigia con qualche ostacolo. Clicchi sul
pavimento, il personaggio ci cammina aggirando gli ostacoli, con animazione di
corsa, e la camera isometrica lo segue in modo fluido, senza scatti.

**Tempo stimato:** 2 settimane (12–20 h). **Prerequisito:** M0 chiusa.

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 1.1 | Scena sandbox e camera isometrica | 2–3 |
| 1.2 | Input System: asset delle azioni e reader | 2–3 |
| 1.3 | NavMesh e movimento click-to-move | 3–4 |
| 1.4 | Personaggio Mixamo e Animator | 3–4 |
| 1.5 | Rifinitura, build, GIF, commit | 2 |

---

## Concetti Unity che imparerai qui

Prima di scrivere codice, leggi questi (30–40 minuti, sono i fondamenti su cui
si regge tutto il resto del progetto):

- **Ciclo di vita dei componenti** — `Awake`, `OnEnable`, `Start`, `Update`,
  `LateUpdate`, `FixedUpdate`, `OnDisable`, `OnDestroy`, e soprattutto **in quale
  ordine** vengono chiamati tra oggetti diversi.
  → `docs.unity3d.com/Manual/ExecutionOrder.html`
- **GameObject / Component / Prefab** — la composizione al posto dell'ereditarietà.
  Un `GameObject` è un contenitore vuoto; tutto il comportamento sono `Component`.
- **Layer e LayerMask** — filtri bitmask per fisica e rendering.
  → `docs.unity3d.com/Manual/Layers.html`
- **Raycast** — `Camera.ScreenPointToRay` + `Physics.Raycast`.
- **NavMesh e NavMeshAgent** — pathfinding su superficie navigabile.
  → package *AI Navigation*, `docs.unity3d.com/Packages/com.unity.ai.navigation@latest`
- **Animator, parametri, blend tree** — macchina a stati per le animazioni.
- **Input System** — asset di azioni, binding, callback.
  → `docs.unity3d.com/Packages/com.unity.inputsystem@latest`

---

## Architettura

Quattro script piccoli invece di uno grande. Sembra eccessivo adesso; alla M8,
quando gli input saranno dieci e il movimento sarà interrotto da stun, root e
teletrasporti, sarà l'unica cosa che ti salva.

```
Player (GameObject, prefab)
│
├─ NavMeshAgent                 componente Unity: calcola e percorre il path
│
├─ PlayerInputReader            legge l'Input System, emette eventi.
│                               NON sa cosa sia il movimento né il mondo 3D.
│
├─ PlayerMotor                  riceve un punto nel mondo, lo passa all'agent.
│                               Espone la velocità normalizzata.
│                               NON sa nulla di input né di mouse.
│
├─ PlayerController             la "colla": ascolta l'input, fa il raycast,
│                               decide dove andare, chiama il motor.
│
└─ Model (GameObject figlio)
    ├─ Animator
    └─ PlayerAnimatorDriver     legge la velocità dal motor, guida i parametri.
                                NON contiene logica di gioco.

Main Camera
└─ CameraFollow                 segue un target con offset fisso e smoothing.
```

**Il principio:** ogni classe ha **una** ragione per cambiare. Se devi aggiungere
il supporto al gamepad, tocchi solo `PlayerInputReader`. Se cambi animazioni,
solo `PlayerAnimatorDriver`.

---

## Scheletri

Il corpo dei metodi lo scrivi tu. Questi sono i contratti.

```csharp
namespace <NomeProgetto>.Player
{
    /// Traduce l'input grezzo in intenzioni. Non conosce il mondo di gioco.
    public class PlayerInputReader : MonoBehaviour
    {
        public event Action OnMoveCommandStarted;   // click premuto
        public event Action OnMoveCommandCanceled;  // click rilasciato
        public bool IsMoveCommandHeld { get; private set; }
        public Vector2 PointerScreenPosition { get; }

        private void Awake()   { /* crea/abilita le azioni, iscrivi le callback */ }
        private void OnEnable()  { }
        private void OnDisable() { /* disabilita le azioni e disiscrivi */ }
    }

    /// Esegue il movimento. Non conosce l'input.
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerMotor : MonoBehaviour
    {
        /// 0 = fermo, 1 = velocità massima. Serve all'animator.
        public float NormalizedSpeed { get; }

        public void MoveTo(Vector3 worldPoint) { }
        public void Stop() { }
    }

    /// Collega input e movimento. È qui che vive la conoscenza del mondo.
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private LayerMask _walkableLayers;
        [SerializeField] private float _maxRayDistance = 100f;

        // Se il raggio colpisce una superficie camminabile, restituisce true
        // e il punto colpito. Implementala per prima e testala con Debug.DrawRay.
        private bool TryGetPointUnderCursor(out Vector3 point) { point = default; return false; }
    }
}

namespace <NomeProgetto>.Rendering
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(-10f, 12f, -10f);
        [SerializeField] private float _smoothTime = 0.2f;

        private Vector3 _velocity; // stato interno di SmoothDamp, non toccarlo

        private void LateUpdate() { /* Vector3.SmoothDamp verso _target.position + _offset */ }
    }
}
```

---

## Passo 1.1 — Scena e camera

1. Nuova scena `Assets/_Project/Scenes/Sandbox_Combat.unity`.
2. Un **Plane** scalato a 5 (= 50×50 m) come pavimento. Qualche **Cube** come ostacolo.
3. Crea il layer **`Ground`** (*Layers → Edit Layers*) e assegnalo al Plane.
   Crea anche `Obstacle` e assegnalo ai cubi.
4. `Main Camera`: **Projection → Orthographic**, `Size` 8–10,
   **Rotation `(30, 45, 0)`**, posizione qualsiasi (ci penserà lo script).
5. Scrivi `CameraFollow.cs` e assegna il target.

> **Perché `(30,45,0)` e non `(45,45,0)`.** 45° di inclinazione dà un look
> dall'alto, quasi top-down. 30° si avvicina alla proiezione 2:1 dei classici
> isometrici e mostra di più i lati dei modelli. Prova entrambi **guardando**,
> non leggendo: è una scelta estetica, non tecnica.

**Verifica:** muovi il target a mano in Play Mode (trascinandolo nella Scene view)
e la camera lo segue morbida, senza tremolii.

---

## Passo 1.2 — Input System

1. *Assets → Create → Input Actions*, chiamalo `PlayerControls`, mettilo in
   `Assets/_Project/Settings/`.
2. Aprilo, crea un Action Map `Gameplay` con due azioni:
   - `Move` — tipo **Button**, binding `Mouse/leftButton`
   - `Point` — tipo **Value / Vector2**, binding `Mouse/position`
3. Nell'Inspector dell'asset, spunta **Generate C# Class** e applica.
   Unity genera una classe `PlayerControls` che puoi istanziare dal codice.
4. Scrivi `PlayerInputReader.cs`.

> **Le due strade.** Puoi usare il componente `PlayerInput` (drag & drop, comodo
> ma magico) oppure la classe generata (più codice, controllo totale, testabile).
> **Usa la classe generata**: è quella che troverai nei progetti seri e ti fa
> capire cosa succede davvero.

**Verifica:** un `Debug.Log` nell'evento, click nel gioco, il log appare.

---

## Passo 1.3 — NavMesh e movimento

1. Crea un GameObject vuoto `NavMesh`, aggiungi il componente **`NavMeshSurface`**
   (package AI Navigation), imposta *Collect Objects: All*, premi **Bake**.
   Deve comparire una superficie azzurra sul pavimento, con i buchi degli ostacoli.
2. Crea il GameObject `Player` (per ora una Capsule), aggiungi `NavMeshAgent`.
3. Scrivi `PlayerMotor.cs` e `PlayerController.cs`.

**Parametri del NavMeshAgent da capire, non da copiare:**
`speed`, `angularSpeed`, `acceleration`, `stoppingDistance`, `autoBraking`,
`updateRotation`, `radius`, `height`. Cambiali in Play Mode e guarda cosa succede:
si impara più in dieci minuti così che in un'ora di lettura.

**Verifica:** clicchi sul pavimento, la capsula ci va aggirando gli ostacoli.
Clicchi su un ostacolo e **non** succede niente (grazie alla LayerMask).

---

## Passo 1.4 — Personaggio e animazioni

1. Su **mixamo.com**: scegli un personaggio, scarica **FBX for Unity**.
   Scarica anche le animazioni **Idle** e **Running**, in formato *FBX for Unity*,
   **"Without Skin"**, con **"In Place" spuntato**.
2. Import in `Assets/_Project/Art/`. Nelle *Import Settings* di ogni file:
   tab **Rig → Animation Type: Humanoid**. Per le clip, tab **Animation**,
   spunta **Loop Time**.
3. Crea un **Animator Controller**, aggiungi un parametro float `Speed`,
   e un **Blend Tree 1D** con Idle (0) → Running (1).
4. Metti il modello come **figlio** del `Player`, l'Animator sul modello.
5. Scrivi `PlayerAnimatorDriver.cs`.

> **Un float, non un bool.** `Speed` come float in un blend tree ti dà la
> transizione continua camminata→corsa e ti prepara alla M8. Un bool `isRunning`
> ti costringerebbe a rifare tutto.

---

## Trappole note — le incontrerai, riconoscile

1. **Camera che trema.** Il follow va in `LateUpdate`, non in `Update`.
   In `Update` non sai se il player si è già mosso in questo frame.

2. **`Camera.main` è lento.** Internamente fa una ricerca per tag. Chiamalo una
   volta in `Awake` e salvalo in un campo.

3. **Il raycast colpisce la cosa sbagliata.** Passa la `LayerMask` **come
   parametro** a `Physics.Raycast`, non fare il raycast su tutto e poi
   controllare il layer del risultato: il primo collider colpito potrebbe essere
   un nemico e perderesti il click sul pavimento dietro di lui.

4. **`SetDestination` chiamato ogni frame** costa e fa scattare il path.
   Chiamalo solo quando la destinazione cambia davvero.

5. **`agent.remainingDistance` restituisce `Infinity`** mentre `agent.pathPending`
   è `true`. Controlla sempre `pathPending` prima di usarlo.

6. **`agent.velocity` vs `agent.desiredVelocity`:** il primo è la velocità reale
   (0 nel primo frame dopo un `SetDestination`), il secondo è quella voluta.
   Per l'animazione, `velocity.magnitude` è quello giusto, ma smorzalo.

7. **Il personaggio scivola senza animare** → scala d'import sbagliata o
   "In Place" non spuntato su Mixamo.

8. **Il personaggio ruota due volte o vibra** → sia il `NavMeshAgent`
   (`updateRotation`) sia il tuo codice stanno gestendo la rotazione. Scegline uno.

9. **`SetFloat` senza damping** dà transizioni a scatti. Usa l'overload con
   `dampTime` e `Time.deltaTime`, e `Animator.StringToHash` invece della stringa
   (la stringa fa un lookup e alloca).

10. **Il click passa attraverso la UI.** Non è un problema adesso perché UI non ce
    n'è, ma dalla M2 dovrai controllare `EventSystem.current.IsPointerOverGameObject()`.
    Segnatelo.

11. **Eventi non disiscritti.** Ogni `+=` in `OnEnable` vuole il suo `-=` in
    `OnDisable`. Adesso non se ne accorge nessuno; alla M6, quando i nemici
    vengono creati e distrutti a centinaia, è la differenza tra un gioco che gira
    e uno che ingolfa.

**Strumenti di diagnosi da usare fin da subito:** `Debug.DrawRay` (disegna il
raggio nella Scene view), `Debug.DrawLine`, `OnDrawGizmosSelected` per
visualizzare raggi e distanze, e la **Scene view attiva durante il Play Mode** —
guardare l'agent che calcola il path vale cento `Debug.Log`.

---

## Checklist di chiusura

- [ ] Il personaggio si muove dove clicchi, aggirando gli ostacoli
- [ ] Il click su un ostacolo non fa nulla
- [ ] Animazione idle ↔ corsa fluida, senza scivolamenti
- [ ] Camera fluida, nessun jitter
- [ ] `Player` salvato come **prefab** in `Assets/_Project/Prefabs/`
- [ ] Console pulita, nessun warning giallo lasciato lì
- [ ] **Build eseguibile che parte e funziona**
- [ ] GIF registrata (ShareX) per il devlog
- [ ] Voce in `DEVLOG.md`
- [ ] Commit e push

---

## Esercizi di consolidamento

Falli dopo aver chiuso la checklist. Servono a trasformare "ha funzionato" in
"ho capito".

1. **Movimento tenendo premuto.** Come in Diablo: tenendo giù il tasto il
   personaggio continua a seguire il cursore. Suggerimento: l'hai già previsto
   con `IsMoveCommandHeld`.
2. **Indicatore di destinazione.** Un cerchio o un decal che compare dove hai
   cliccato e svanisce in mezzo secondo.
3. **Ortho vs perspective.** Metti la camera in Perspective con FOV 20–25 e
   confronta. Scrivi in `DECISIONS.md` quale scegli e perché.
4. **Esperimento sul refactoring.** Prova a immaginare — a parole, nel devlog —
   cosa dovresti toccare per aggiungere il supporto a tastiera WASD.
   Se la risposta è "solo `PlayerInputReader`", l'architettura è corretta.
   Se è "tre file", ripensa la divisione delle responsabilità.
