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
| 1.4 | Personaggio KayKit (CC0) e Animator | 3–4 |
| 1.5 | Rifinitura, build, GIF, commit | 2 |

---

## Architettura

Quattro script piccoli invece di uno grande. Sembra eccessivo adesso; alla M9,
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

Questi sono i contratti. Le implementazioni complete arrivano passo per passo,
nei file sotto `Assets/_Project/Scripts/`: dove differiscono, vale il codice.

```csharp
namespace DarkDescent.Player
{
    /// Traduce l'input grezzo in intenzioni. Non conosce il mondo di gioco.
    public class PlayerInputReader : MonoBehaviour
    {
        public event Action MoveCommandStarted;   // click premuto
        public event Action MoveCommandCanceled;  // click rilasciato
        public bool IsMoveCommandHeld { get; private set; }
        public Vector2 PointerScreenPosition { get; }

        private void Awake()     { /* crea PlayerControls */ }
        private void OnEnable()  { /* iscrivi le callback, abilita la mappa */ }
        private void OnDisable() { /* disabilita la mappa, disiscrivi */ }
        private void OnDestroy() { /* Dispose di PlayerControls */ }
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
        [SerializeField] private LayerMask _walkableLayers;   // Ground
        [SerializeField] private LayerMask _blockingLayers;   // Obstacle: fermano il raggio, non si cammina
        [SerializeField] private float _maxRayDistance = 100f;
        [SerializeField] private float _holdRepathInterval = 0.1f;
        [SerializeField] private Camera _camera;              // vuoto = Camera.main in Awake

        // Se il primo collider colpito è camminabile, restituisce true
        // e il punto colpito.
        private bool TryGetPointUnderCursor(out Vector3 point) { point = default; return false; }
    }
}

namespace DarkDescent.Rendering
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _distance = 20f;     // lungo l'asse di vista
        [SerializeField] private float _smoothTime = 0.15f;

        private Vector3 _velocity; // stato interno di SmoothDamp, non toccarlo

        private void Start()      { /* aggancio immediato, niente volo iniziale */ }
        private void LateUpdate() { /* SmoothDamp verso _target.position - transform.forward * _distance */ }
    }
}
```

---

## Passo 1.1 — Scena e camera

1. Nuova scena `Assets/_Project/Scenes/Sandbox_Combat.unity`.
2. Un **Plane** scalato a 5 (= 50×50 m) come pavimento. Qualche **Cube** come ostacolo.
3. Crea il layer **`Ground`** (*Layers → Edit Layers*) e assegnalo al Plane.
   Crea anche `Obstacle` e assegnalo ai cubi.
4. `Main Camera`: **Projection → Orthographic**, `Size` 9,
   **Rotation `(30, 45, 0)`**, posizione qualsiasi (ci penserà lo script).
5. Aggiungi `CameraFollow` (`Scripts/Rendering/`) alla camera e assegna il target.
   La posizione si ricava dalla rotazione, non da un offset fisso: un offset
   scritto a mano va tenuto allineato con la rotazione, altrimenti il target
   esce dal centro dell'inquadratura.

> **Perché `(30,45,0)` e non `(45,45,0)`.** 45° di inclinazione dà un look
> dall'alto, quasi top-down. 30° si avvicina alla proiezione 2:1 dei classici
> isometrici e mostra di più i lati dei modelli. È una scelta estetica: se
> preferisci l'altra, basta cambiare la rotazione nell'Inspector.

**Verifica:** muovi il target a mano in Play Mode (trascinandolo nella Scene view)
e la camera lo segue morbida, senza tremolii.

---

## Passo 1.2 — Input System

0. **Prima, il residuo del template.** In *Project Settings → Input System Package*
   stacca `InputSystem_Actions` dal campo *Project-wide Actions*, poi cancella
   `Assets/InputSystem_Actions.inputactions` dal Project window.
1. *Assets → Create → Input Actions*, chiamalo `PlayerControls`, mettilo in
   `Assets/_Project/Settings/`.
2. Aprilo, crea un Action Map `Gameplay` con due azioni:
   - `Move` — tipo **Button**, binding `Mouse/leftButton`
   - `Point` — tipo **Value / Vector2**, binding `Mouse/position`
3. Nell'Inspector dell'asset, spunta **Generate C# Class** e imposta
   **C# Class File** a `Assets/_Project/Scripts/Input/PlayerControls.cs`, poi
   applica. Unity genera una classe `PlayerControls` che puoi istanziare dal codice.
   Namespace della classe: `DarkDescent.Input`.
   **Il percorso conta:** la classe deve stare sotto la cartella di
   `DarkDescent.asmdef`, altrimenti finisce in `Assembly-CSharp` e il
   `PlayerInputReader` non la vede (ADR-003). Lasciandola accanto all'asset, in
   `Settings/`, avresti un errore "type or namespace not found".
4. `PlayerInputReader` in `Scripts/Player/`. Va sul `Player` al passo 1.3,
   insieme a `PlayerMotor` e `PlayerController`.

> **Nomi degli eventi senza `On`.** `MoveCommandStarted`, non `OnMoveCommandStarted`:
> in .NET il prefisso `On` è dei metodi che sollevano l'evento, e in Unity
> `OnQualcosa` sembra un messaggio dell'engine come `OnEnable`.

> **Le due strade.** Il componente `PlayerInput` (drag & drop, comodo ma magico)
> oppure la classe generata (più codice, controllo totale, testabile).
> **Si usa la classe generata**: le dipendenze restano esplicite nel codice
> invece che nascoste in callback collegate dall'Inspector.

**Verifica:** fatta con un test PlayMode temporaneo su `InputTestFixture`
(mouse virtuale): pressione, rilascio, disattivazione a tasto premuto e
riattivazione emettono gli eventi giusti. La prova a mano arriva con il 1.3,
quando il click muove la capsula.

---

## Passo 1.3 — NavMesh e movimento

1. GameObject vuoto `NavMesh` con **`NavMeshSurface`**: *Collect Objects: All*,
   *Include Layers* solo `Ground` e `Obstacle`, *Build Height Mesh* attivo.
   L'asset cotto sta in `Scenes/Sandbox_Combat/`.
   - Senza il filtro sui layer, il bake include la capsula del player e le ritaglia
     un buco nel NavMesh proprio dove parte.
   - Senza la height mesh, il NavMesh sta circa 8 cm sopra il pavimento e il player
     galleggia.
2. `NavMeshModifier` su `Obstacles`, area *Not Walkable*, applicato ai figli:
   altrimenti le facce superiori dei cubi diventano isole di NavMesh irraggiungibili.
3. Sul `Player`: `PlayerController`, che porta con sé `PlayerMotor`,
   `PlayerInputReader` e `NavMeshAgent` via `RequireComponent`.
   Agent: *Base Offset* 1 (il pivot della capsula è al centro),
   *Speed* 5, *Angular Speed* 720, *Acceleration* 40, *Stopping Distance* 0.1.
4. Tenendo premuto il tasto il player continua a seguire il cursore; la
   destinazione si aggiorna ogni 0,1 s e il motor scarta quelle quasi uguali.

**Verifica:** clicchi sul pavimento, la capsula ci va aggirando gli ostacoli.
Clicchi su un ostacolo e **non** succede niente. Coperta anche da test PlayMode
temporanei (click dietro il muro, click sull'ostacolo, tasto tenuto premuto).

---

## Passo 1.4 — Personaggio e animazioni

Fonte: **KayKit** di Kay Lousberg, CC0 (ADR-004, opzione a). Crediti in `CREDITS.md`.
Nel repo entra solo ciò che si usa: gli zip completi restano fuori.

1. Da *KayKit Adventurers 2.0* (FREE): `Knight.fbx` e `knight_texture.png`
   in `Art/Models/KayKit/`. Da *KayKit Character Animations 1.1* (FREE):
   `Rig_Medium_General.fbx` e `Rig_Medium_MovementBasic.fbx` in `Art/Animations/KayKit/`.
   Gli attacchi (`Rig_Medium_CombatMelee.fbx`) entrano alla M2.
2. Import, tab **Rig → Animation Type: Generic**, non Humanoid. Il Knight crea
   l'avatar (*Create From This Model*); i due file di animazioni lo copiano
   (*Copy From Other Avatar* → `KnightAvatar`). **Loop Time** su idle, walk e run.
   > **Perché Generic.** Con Humanoid, Unity non mappa l'osso `chest` di KayKit
   > e scarta la rotazione del busto in tutte le clip (più alcune traslazioni):
   > l'import lo segnala con un warning per clip. Personaggi, scheletri nemici e
   > animazioni KayKit condividono lo stesso `Rig_Medium`, quindi il retargeting
   > di Humanoid non serve; Generic riproduce le clip senza perdite e costa meno.
   > Si rinuncia a retargeting su rig diversi e IK dei piedi.
3. `Art/Animations/Player.controller`: parametro float `Speed`, stato di default
   **Blend Tree 1D** `Locomotion` con `Idle_A` (0) → `Running_A` (1).
4. Il Knight è il figlio `Model` del `Player`. Sul `Player` la capsula resta solo
   collider (centro 1.1, altezza 2.2); il pivot va ai piedi, quindi
   **Base Offset 0** sull'agent. Sull'Animator: *Apply Root Motion* spento
   (la posizione la decide il NavMeshAgent), *Culling Mode* **Always Animate**:
   il player è sempre inquadrato, il culling non risparmierebbe nulla.
5. `PlayerAnimatorDriver` sul `Model`: legge `NormalizedSpeed` dal motor e lo
   passa a `Speed` con damping.

**Verifica:** fatta con test PlayMode temporanei: da fermo `Speed` ≈ 0, in corsa
≈ 0,93 con le ossa delle gambe in movimento, all'arrivo di nuovo ≈ 0, piedi sul
pavimento. A mano: click lontano, il cavaliere corre, si ferma e torna in idle
senza scivolare.

> **Un float, non un bool.** `Speed` come float in un blend tree ti dà la
> transizione continua camminata→corsa e ti prepara alla M9. Un bool `isRunning`
> ti costringerebbe a rifare tutto.

---

## Trappole note — le incontrerai, riconoscile

1. **Camera che trema.** Il follow va in `LateUpdate`, non in `Update`.
   In `Update` non sai se il player si è già mosso in questo frame.

2. **`Camera.main` è lento.** Internamente fa una ricerca per tag. Chiamalo una
   volta in `Awake` e salvalo in un campo.

3. **Il raycast colpisce la cosa sbagliata.** Passa la `LayerMask` **come
   parametro** a `Physics.Raycast`, non fare il raycast su tutto e poi
   filtrare. Ma la maschera deve includere anche ciò che **blocca** il click:
   con il solo `Ground` il raggio attraversa i cubi e colpisce il pavimento
   dietro, e un click su un ostacolo porta il player alle sue spalle. Per questo
   il raggio usa camminabili + bloccanti, e poi si controlla il layer del primo
   collider colpito. Alla M2 i nemici entrano come terza categoria (click = attacco).

4. **`SetDestination` chiamato ogni frame** costa e fa scattare il path.
   Chiamalo solo quando la destinazione cambia davvero.

5. **`agent.remainingDistance` restituisce `Infinity`** mentre `agent.pathPending`
   è `true`. Controlla sempre `pathPending` prima di usarlo.

6. **`agent.velocity` vs `agent.desiredVelocity`:** il primo è la velocità reale
   (0 nel primo frame dopo un `SetDestination`), il secondo è quella voluta.
   Per l'animazione, `velocity.magnitude` è quello giusto, ma smorzalo.

7. **Il personaggio scivola senza animare** → le clip non sono collegate allo
   scheletro (percorsi delle ossa diversi tra clip e modello) oppure l'Animator
   è in culling. In batchmode nessuna camera disegna davvero, quindi con
   *Cull Update Transforms* le ossa restano ferme anche se `Speed` cambia.

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

12. **`Failed to create agent because there is no valid NavMesh`** nel log della
    build, non in editor. L'agent nativo prova ad agganciarsi un istante prima che
    `NavMeshSurface` carichi i dati, poi ci riesce da solo: al primo frame utile è
    già sul NavMesh. Innocuo qui; alla M6, con il bake a runtime, l'agent del player
    va attivato solo dopo il bake, e lì sparisce anche questo messaggio.

**Strumenti di diagnosi da usare fin da subito:** `Debug.DrawRay` (disegna il
raggio nella Scene view), `Debug.DrawLine`, `OnDrawGizmosSelected` per
visualizzare raggi e distanze, e la **Scene view attiva durante il Play Mode** —
guardare l'agent che calcola il path vale cento `Debug.Log`.

---

## Checklist di chiusura

- [x] Il personaggio si muove dove clicchi, aggirando gli ostacoli *(test PlayMode)*
- [x] Il click su un ostacolo non fa nulla *(test PlayMode)*
- [ ] Animazione idle ↔ corsa fluida, senza scivolamenti *(test: Speed e ossa ok; manca l'occhio di Mirco)*
- [x] Camera fluida, nessun jitter *(provata da Mirco al passo 1.1)*
- [x] `Player` salvato come **prefab** in `Assets/_Project/Prefabs/`
- [x] Console pulita, nessun warning giallo lasciato lì *(editor; nel log della build resta la riga della trappola 12)*
- [ ] **Build eseguibile che parte e funziona** *(avvio e movimento verificati in headless; manca la prova a mano di Mirco)*
- [x] GIF per il README *(generata dai fotogrammi della camera, `docs/media/`)*
- [ ] Commit e push
