# M2 — "Colpisco e muoio" ✅ chiusa il 3 ottobre 2026

**Cosa deve succedere a schermo (Definition of Done):**
in una **build eseguibile**, la stanza della M1 con tre scheletri. Clicchi su uno
scheletro: il cavaliere si avvicina e attacca, lo scheletro reagisce al colpo, ti
insegue e ti colpisce. Una sfera rossa mostra la tua vita. Sopravvivi ai tre
scheletri o muori provandoci, e dalla schermata di morte puoi ricominciare.
**Test verdi.**

**Tempo stimato:** 20–30 h. **Prerequisito:** M1 chiusa (tag `m1`).

**Come si lavora:** il codice lo scrivo io; i passaggi nell'editor li faccio in
batchmode a Unity chiuso (script di editor usa e getta), come alla M1. A Mirco
restano le decisioni, le prove in Play Mode e in build, e gli ADR.

---

## Decisioni da prendere prima di cominciare

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Come si trovano i sistemi** (piano § 4.2) | **Composition root**: un componente `CompositionRoot` nella scena che in `Awake` collega player, nemici, HUD e schermata di morte | Esplicito e si legge dall'alto in basso. Alla M3 si sposta nella scena `Core` senza cambiare forma. Singleton e canali a ScriptableObject restano fuori finché non servono |
| D2 | **Sistema di UI** | **uGUI** (già installato, `com.unity.ugui` 2.0, con TextMeshPro incluso) | La sfera è un'`Image` *Filled* verticale: dieci minuti. UI Toolkit a runtime va bene per menu e inventario, ma mescolare due sistemi di UI nello stesso HUD costa più di quanto rende. Si rivaluta alla M4 con l'inventario |
| D3 | **Test PlayMode permanenti** | **Sì, pochi**: un assembly `DarkDescent.Tests.PlayMode` con `InputTestFixture` per i flussi end-to-end (click-to-move, attacco che uccide uno scheletro) | Alla M1 sono serviti a scoprire tre problemi veri (height mesh, culling, test sbagliato). Richiede `"testables": ["com.unity.inputsystem"]` nel manifest. I test di logica restano EditMode |
| D4 | **Download** | KayKit **Skeletons** FREE 1.1 (7,7 MB, CC0) e un pacchetto audio **Kenney** CC0 (Impact Sounds o RPG Audio) | Gli scheletri KayKit dovrebbero condividere lo scheletro dei personaggi (da verificare all'import). L'animazione d'attacco arriva da `Rig_Medium_CombatMelee.fbx`, già nello zip scaricato alla M1 |

D1 e D2 vanno in ADR alla chiusura, quando si è visto come hanno retto.

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 2.1 | Assembly di test, `HealthModel` e test EditMode | 2–3 |
| 2.2 | Danno: `DamageInfo`, `IDamageable`, `Health` | 2 |
| 2.3 | Scheletro KayKit e animazioni di combattimento | 3–4 |
| 2.4 | Attacco del giocatore: bersaglio, avvicinamento, colpo | 4–5 |
| 2.5 | IA dello scheletro (`enum` + `switch`) | 3–4 |
| 2.6 | HUD: sfera della vita e composition root | 2–3 |
| 2.7 | Morte e ricomincia | 2 |
| 2.8 | Game feel: flash, hit stop, numeri di danno, effetti sonori | 3–4 |
| 2.9 | Tre scheletri, build, GIF, tag `m2` | 2 |

---

## Architettura

Il combattimento è **lo stesso componente** per player e nemici: cambia solo chi
decide il bersaglio (input per il player, IA per lo scheletro).

```
DarkDescent.Combat
├─ DamageInfo (readonly struct)   quanto, tipo, sorgente, critico
├─ IDamageable                    TakeDamage(in DamageInfo), IsDead
├─ HealthModel (C# puro)          vita, danno, morte una volta sola — testato in EditMode
├─ Health (MonoBehaviour)         guscio di HealthModel, implementa IDamageable,
│                                 eventi HealthChanged, Damaged, Died
├─ WeaponDefinition (SO)          danno, portata, intervallo, ritardo del colpo — immutabile
└─ MeleeAttack                    avvicina, si gira, colpisce dopo il ritardo dell'arma,
                                  ricontrolla bersaglio vivo e a portata prima del danno

Player (prefab)                           Skeleton (prefab)
├─ PlayerController  click: terreno →     ├─ EnemyAI  Idle / Chase / Attack / Dead
│                    muovi, nemico →      │           (enum + switch)
│                    attacca              ├─ MeleeAttack, Health, NavMeshAgent
├─ MeleeAttack, Health                    └─ Model: Animator + CharacterAnimatorDriver
└─ Model: Animator + CharacterAnimatorDriver

DarkDescent.UI                            DarkDescent.Core
├─ HealthOrb      iscritta a HealthChanged └─ CompositionRoot  collega tutto in Awake
├─ DeathScreen    iscritta a Died
└─ DamageNumbers  numeri fluttuanti, con pool
```

- `PlayerAnimatorDriver` diventa `CharacterAnimatorDriver` (`DarkDescent.Characters`),
  condiviso: legge la velocità dall'agent, e gli one-shot (`PlayAttack`, `PlayHit`,
  `PlayDeath`) partono dagli eventi di `MeleeAttack` e `Health`. Dopo la morte
  ignora ogni altro comando.
- `PlayerMotor` resta del player; lo scheletro usa il `NavMeshAgent` dall'IA.
- Il danno parte **dopo il ritardo dell'arma**, non da un Animation Event: gli
  eventi si perdono a ogni reimport della clip.

### Contratti

```csharp
namespace DarkDescent.Combat
{
    public enum DamageType { Physical }

    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly GameObject Source;
        public readonly bool IsCritical;
    }

    public interface IDamageable
    {
        void TakeDamage(in DamageInfo info);
        bool IsDead { get; }
    }

    /// Logica pura, nessuna dipendenza da Unity oltre ai tipi base.
    public sealed class HealthModel
    {
        public HealthModel(float max);
        public float Current { get; }
        public float Max { get; }
        public bool IsDead { get; }
        /// Restituisce il danno effettivamente applicato (0 se già morto).
        public float ApplyDamage(float amount);
        public event Action<float, float> Changed; // current, max
        public event Action Died;                 // una volta sola
    }
}
```

---

## Passo 2.1 — Assembly di test e `HealthModel`

1. `Assets/_Project/Tests/EditMode/DarkDescent.Tests.EditMode.asmdef`: solo
   Editor, referenzia `DarkDescent`, NUnit e Test Runner (piano § 4.5).
2. `HealthModel` in `Scripts/Combat/`.
3. Test EditMode:
   - il danno riduce la vita;
   - la vita non scende sotto zero;
   - a zero `Died` scatta **una volta sola**, anche con colpi successivi;
   - dopo la morte `ApplyDamage` restituisce 0 e non emette `Changed`;
   - un danno negativo o nullo non cura e non emette eventi.
4. Se D3 è sì: `Assets/_Project/Tests/PlayMode/` con i test di movimento della M1
   resi permanenti, e i testables nel manifest.

**Verifica:** test eseguiti in batchmode (`-runTests -testPlatform EditMode`), tutti verdi.

---

## Passo 2.2 — Danno

`DamageInfo`, `IDamageable`, `Health` (guscio di `HealthModel`, eventi in C#,
nessun `UnityEvent`). Eventi di `Health`, in quest'ordine:
`HealthChanged(current, max)`, `Damaged(DamageInfo, applied)`, `Died` (una volta).
`Damaged` porta il danno **applicato**, non quello richiesto: è quello che mostrano
i numeri di danno.

`Health` inoltra gli eventi da `TakeDamage` invece di iscriversi a quelli del
model: nessun `+=` da bilanciare, e l'ordine è deciso in un punto solo.

**Verifica:** test PlayMode su `Health` (in EditMode `Awake` non gira): ordine
degli eventi, morte una volta sola, danno nullo silenzioso, accesso via `IDamageable`.

---

## Passo 2.3 — Scheletro e animazioni di combattimento

1. `Skeleton_Minion` e `Skeleton_Blade` in `Art/Models/KayKit/Skeletons/`, rig
   Generic con avatar proprio. **Verificato:** stesse 24 ossa del cavaliere con
   gli stessi percorsi, e le animazioni del pacchetto Skeletons sono identiche
   byte per byte a quelle già importate. Le clip si condividono senza retargeting.
2. `Rig_Medium_CombatMelee.fbx` in `Art/Animations/KayKit/`, avatar copiato dal
   cavaliere. Il cavaliere usa `Melee_1H_Attack_Chop` (1,07 s), lo scheletro
   `Melee_1H_Attack_Slice_Diagonal` (1,00 s). `Hit_A` e `Death_A` da `Rig_Medium_General`.
3. Armi figlie dell'osso `handslot.r` con trasformazione identità: `sword_1handed`
   (variante `fbx(unity)` dello zip Adventurers) per il cavaliere, `Skeleton_Blade`
   per lo scheletro.
4. `Player.controller` rinominato `Character.controller` (stesso GUID), condiviso:
   `Locomotion` (blend tree della M1), `Attack` e `Hit` con ritorno a exit time,
   `Death` senza uscite. `Skeleton.overrideController` sostituisce la corsa con
   `Walking_A` e il fendente.
5. Prefab `Skeleton`: radice con `NavMeshAgent` (velocità 3, priorità 60) e
   `Model` con Animator in `CullUpdateTransforms` e `CharacterAnimatorDriver`.
   Uno in scena a (6, 0, 6).
6. Righe in `CREDITS.md`.

**Verifica:** test PlayMode `CharacterAnimationTests` (scheletro fermo sul NavMesh,
attacco che torna in Locomotion, doppio attacco che riparte, morte definitiva,
colpo subito sullo scheletro con il culling acceso); nessun warning di import.

---

## Passo 2.4 — Attacco del giocatore

1. Layer **`Enemy`** (8). Il raggio del `PlayerController` usa ora tre categorie:
   camminabile, bloccante, **nemico**. Primo collider colpito: pavimento → muovi;
   ostacolo → niente; nemico → `MeleeAttack.SetTarget`.
2. `MeleeAttack`: se il bersaglio è fuori portata si avvicina via agent; a portata
   ferma l'agent, si gira verso il bersaglio, avvia l'animazione e applica il
   danno dopo `WeaponDefinition.HitDelay`, **solo se** il bersaglio è ancora vivo
   e ancora a portata (con un margine).
3. Tenendo premuto su un nemico si continua ad attaccarlo; un click sul terreno
   annulla il bersaglio.
4. `WeaponDefinition` per la spada: danno fisso (in M2 non c'è formula, piano § 2).

**Com'è andata:**

- `Data/Weapons/Sword.asset`: danno 10, portata 0,6 m tra i bordi, margine 0,5 m,
  intervallo 1 s, `HitDelay` 0,6 s. Il ritardo viene dalla clip: la punta della
  lama scende tra 0,57 e 0,63 s di `Melee_1H_Attack_Chop`. Per il fendente dello
  scheletro (`Slice_Diagonal`) l'impatto è a 0,42 s.
- `SetTarget` chiede **un** colpo: avvicinamento, colpo, poi il bersaglio si lascia.
  Per colpire di continuo si richiama a ogni tick: il `PlayerController` lo fa
  finché il tasto è premuto sul nemico cliccato, l'IA lo farà nello stato Attack.
- Dall'inizio del colpo al danno (`IsSwinging`) il colpo non si annulla: un click
  sul terreno in quell'intervallo resta in coda e parte subito dopo.
- Tenendo premuto su un nemico morto il cavaliere resta fermo fino al rilascio.
- Il bersaglio è salvato anche come `Transform`, per il confronto con il null di
  Unity. Sulle interfacce c'è `IDamageable.IsAlive()`, che fa il cast a
  `UnityEngine.Object`.
- Player: `Health` 100 e `MeleeAttack` con la spada. Scheletro: layer `Enemy`,
  `CapsuleCollider` sulla radice, `Health` 30 (tre colpi).
- `CharacterAnimatorDriver` si iscrive da solo a `MeleeAttack.SwingStarted`,
  `Health.Damaged` e `Health.Died` del parent. Il combattimento non conosce le
  animazioni.

**Verifica:** test PlayMode `PlayerAttackTests` — click che avvicina e dà un solo
colpo (lo scheletro passa in Hit); tasto tenuto fino alla morte; click sul terreno
che annulla l'avvicinamento; bersaglio che si allontana durante il fendente e non
prende danno.

**Decisione presa al 2.5:** un colpo subito interrompe il colpo in corso di chi lo
riceve solo sopra una soglia di danno, come l'hit recovery di Diablo 1 (vedi
`HitRecovery` al passo 2.5).

---

## Passo 2.5 — IA dello scheletro

`EnemyAI` con `enum State { Idle, Chase, Attack, Dead }` e uno `switch` in `Update`:

- **Idle:** fermo finché il player non entra nel raggio di aggro (con linea di vista
  libera dagli ostacoli: un raycast sul layer `Obstacle`).
- **Chase:** `SetDestination` verso il player, non a ogni frame (trappola 4 della M1).
- **Attack:** delega a `MeleeAttack`.
- **Dead:** stato terminale, entra su `Health.Died`.

Una state machine a classi è rimandata alla M7, quando i tipi di nemico saranno tre.

**Com'è andata:**

- `Chase` e `Attack` chiedono entrambi il colpo con `MeleeAttack.SetTarget` a ogni
  frame: l'avvicinamento è già lì, con il ricalcolo del percorso ogni 0,1 s. Lo
  stato distingue solo se il bersaglio è a portata (`IsTargetInRange`) o se un
  colpo è partito. Una volta notato il player non si molla, come in Diablo 1.
- `Idle` controlla la vista ogni 0,2 s: raggio di 8 m, poi un `Linecast` all'altezza
  degli occhi (1,5 m) sul layer `Obstacle`.
- `Dead` spegne `MeleeAttack` (annulla anche un fendente in volo), l'agent (il
  corpo non spinge gli altri) e il collider (i click passano al pavimento).
- `Data/Weapons/SkeletonBlade.asset`: danno 5, portata 0,6 m, intervallo 1,6 s,
  `HitDelay` 0,42 s. Il cavaliere corre a 5 m/s, lo scheletro cammina a 3: si può
  sempre scappare.
- **Decisione sull'interruzione:** soglia, come l'hit recovery di Diablo 1.
  `HitRecovery` ascolta `Health.Damaged`. Se un colpo toglie almeno il 20% della
  vita massima, chiama `MeleeAttack.Interrupt(0,5 s)`: il fendente in corso si
  annulla e per mezzo secondo niente inseguimento né colpi. L'animazione Hit parte
  solo in quel caso, da `HitRecovery.Staggered`, non a ogni danno. Contro lo
  scheletro (30 di vita) la spada da 10 interrompe sempre. Il player per ora non
  ha `HitRecovery`: i colpi da 5 non lo fermano.
- `CompositionRoot` anticipato da qui: un riferimento serializzato al `Health` del
  player e, in `Awake`, `EnemyAI.Bind` su tutti gli scheletri della scena. Al 2.6
  si aggiungono HUD e schermata di morte.

**Verifica:** test PlayMode `EnemyAITests` — fermo con il player lontano; insegue
e colpisce quando lo vede; resta fermo con il player nel raggio ma dietro il cubo;
colpo forte che interrompe il fendente, colpo debole che non lo interrompe; da
morto non colpisce più e spegne collider e agent; morto il player torna in Idle.

---

## Passo 2.6 — HUD e composition root

1. Canvas *Screen Space – Overlay*, `EventSystem` con `InputSystemUIInputModule`.
2. `HealthOrb`: `Image` *Filled* verticale.
   **Nessun polling** in `Update`. Si iscrive a `HealthChanged`.
3. `CompositionRoot` in `DarkDescent.Core` (nato al 2.5 con `EnemyAI.Bind(Health)`):
   si aggiungono i riferimenti a HUD e schermata di morte, e in `Awake` passa a
   ognuno ciò che gli serve (`HealthOrb.Bind(Health)`).
4. Nel `PlayerController`, il click sopra la UI non deve muovere il player
   (trappola 10 della M1, e trappola 7 qui sotto).

**Com'è andata:**

- Canvas `HUD` (*Screen Space – Overlay*, *Scale With Screen Size* 1920×1080).
  Sfera in basso a sinistra, 190 px, in tre `Image` sovrapposte: fondo scuro
  (l'unico bersaglio dei raycast della UI), `Fill` rosso *Filled* verticale dal
  basso, anello di cornice. Le due sprite (`Art/UI/orb.png`, `orb_ring.png`) sono
  generate da codice: sfera in scala di grigi con luce e riflesso, colorata
  dall'`Image`. Nessun asset esterno.
- `HealthOrb.Bind` e `OnEnable`: si iscrive chi arriva per secondo. All'iscrizione
  la sfera si allinea alla vita attuale, quindi una sfera rimasta spenta durante
  un danno torna giusta alla riaccensione.
- `Health` crea il suo model al primo accesso e non in `Awake`. L'ordine degli
  `Awake` tra oggetti diversi non è garantito: il composition root poteva leggere
  la vita prima che esistesse.
- `PlayerController`: la pressione arriva dalla callback dell'Input System e viene
  solo annotata. Si esegue nel primo `Update`, dove `IsPointerOverGameObject` non
  genera il warning (trappola 7). Una pressione nata sopra la UI non diventa un
  movimento, nemmeno tenendo premuto e trascinando nel mondo.
- `EventSystem` con `InputSystemUIInputModule` e le azioni di default.

**Verifica:** test PlayMode `HealthOrbTests` — sfera piena all'avvio e allineata
subito al danno, senza aspettare un frame; riallineata alla riaccensione; staccata
dalla vita precedente dopo un nuovo `Bind`; scende quando lo scheletro colpisce; un
click sulla sfera non muove il player (il test controlla anche che dietro ci sia
il pavimento, così non passa per caso).

---

## Passo 2.7 — Morte e ricomincia

- Player morto: input disattivato, agent fermo, animazione di morte, poi la
  schermata con il pulsante **Ricomincia** (ricarica la scena).
- Nemico morto: collider e agent disattivati (niente più click né spinte),
  il corpo resta per qualche secondo e poi sparisce.

**Verifica:** muori, ricominci, tutto riparte pulito: vita piena, nemici vivi,
`Time.timeScale` a 1.

**Com'è andata:**

- `PlayerDeath` sul player, iscritto a `Health.Died`. Spegne il controller prima
  del reader: spegnendo il reader con il tasto premuto parte un `canceled`, e il
  controller non deve più ascoltare. Poi spegne `MeleeAttack` (annulla il fendente
  in volo), ferma il motor e spegne l'agent. L'animazione la fa partire il driver.
- `DeathScreen` sotto l'HUD. Il componente sta su un oggetto sempre attivo e si
  accende solo il pannello. `Bind` e `OnEnable` funzionano come per la sfera. La
  schermata compare 2 s dopo la morte, con `WaitForSecondsRealtime`: con un hit
  stop in corso `WaitForSeconds` non finirebbe mai. Non ricarica niente da sé:
  emette `RestartRequested`.
- Il composition root riceve `RestartRequested`, rimette `Time.timeScale` a 1 e
  ricarica la scena attiva per indice.
- Scheletro morto: oltre a collider, agent e attacco spenti (passo 2.5), il corpo
  si distrugge dopo 5 s. Chi lo teneva come bersaglio lo controlla con il null
  di Unity.
- **TMP Essential Resources** importate con `-importPackage` da riga di comando.
  `AssetDatabase.ImportPackage` dentro `-executeMethod` non basta: con `-quit`
  Unity esce prima che l'import asincrono finisca. Dentro ci sono due asset non CC0.
  Decisione (da riportare nell'ADR-004): la sprite EmojiOne (CC BY 4.0) è tolta,
  perché serve solo ai tag `<sprite>` che non usiamo; il riferimento in
  `TMP Settings` è azzerato. Il font LiberationSans (SIL OFL 1.1) resta, come
  eccezione per i font OFL, con il testo della licenza accanto e la riga nei crediti.

**Test:** `DeathAndRestartTests` — alla morte controller, input e agent spenti,
driver in Death, un click non muove il corpo, schermata solo dopo la caduta;
**Ricomincia** cliccato con il mouse virtuale ricarica una scena pulita (vita piena,
scheletro vivo in Idle, sfera piena, schermata nascosta, `timeScale` rimesso a 1
anche se un test lo aveva lasciato a 0,3); il corpo dello scheletro resta e poi sparisce.

---

## Passo 2.8 — Game feel

- **Flash** bianco sul bersaglio colpito, 0,1 s, su **tutti** i renderer del modello
  (i KayKit ne hanno fino a nove).
- **Hit stop** di 0,05 s quando un colpo del player va a segno.
- **Numeri di danno** fluttuanti in TextMeshPro, da un pool (`UnityEngine.Pool.ObjectPool`).
- **Effetti sonori provvisori:** fendente, impatto, morte.

**Verifica:** a occhio e a orecchio, in build.

**Com'è andata:**

- **Lampo** (`HitFlash`, sul modello): per 0,1 s, contati in tempo reale, tutti i
  renderer del modello, arma compresa, passano a `M_HitFlash` (URP Unlit bianco).
  Gli array di materiali si preparano in `Awake`, così il lampo non alloca. Niente
  emissione: i materiali KayKit stanno dentro l'FBX e nessuno usa `_EMISSION`, quindi
  in build quella variante dello shader verrebbe tolta e il lampo funzionerebbe solo
  nell'editor. Il materiale del lampo è un asset referenziato dai prefab, quindi in
  build c'è.
- **Hit stop** (`HitStop`, in scena): 0,05 s a `timeScale` 0, solo sui colpi del
  player (`MeleeAttack.HitLanded`, collegato dal composition root). Due hit stop
  ravvicinati non si sommano: vale la fine più lontana. La scala da ripristinare si
  legge solo al primo, altrimenti il secondo salverebbe lo 0 del primo. Spento a
  metà, ripristina comunque.
- **Numeri di danno** (`DamageNumbers` sotto l'HUD, `DamageNumber` per ciascuno):
  testi TMP in screen space che seguono un punto del mondo che sale di 1 m in 0,8 s
  e sfuma nella seconda metà. Escono da un `ObjectPool`. Un solo `Update` per tutti
  i numeri attivi. `SetText("{0}", n)` non alloca stringhe. Gialli sui nemici, rossi
  sul player. Il composition root fa il `Track` di player e nemici.
- **Suoni** (`CharacterAudio`, sulla radice): fendente su `SwingStarted`, impatto su
  `Damaged`, morte su `Died`, con una clip a caso tra le varianti e l'intonazione
  variata di ±6%. Ogni personaggio ha i suoi. Audio 2D per ora: con la camera a 20 m
  l'attenuazione 3D renderebbe tutto quasi muto (audio posizionale alla M3). Kenney
  RPG Audio e Impact Sounds (CC0): nel repo solo le clip usate. **Dopo la chiusura,
  3 ott 2026:** i primi impatti (legno per lo scheletro, armatura per il cavaliere)
  suonavano troppo legnosi e metallici; sostituiti con `impactPunch` (medio per lo
  scheletro, pesante per il cavaliere) e, per le morti, con `impactSoft_heavy`.

**Test:** `GameFeelTests` — due hit stop ravvicinati non si sommano e ripristinano
la scala di prima (0,5, non 1 e non 0); il colpo del player ferma il tempo, fa
lampeggiare lo scheletro e mostra "10", poi tutto torna normale; i numeri tornano
al pool e vengono riusati. I suoni si verificano a orecchio.

---

## Passo 2.9 — Chiusura

Tre scheletri in `Sandbox_Combat`, prefab `Skeleton` in `Prefabs/`, build, GIF
per il README, tag `m2`. Punto di controllo del piano (§ 1.3, in settimane di
calendario).

**Com'è andata:**

- Due scheletri in più: `Skeleton_B` in (-8, 0, 7) e `Skeleton_C` in (5, 0, -10),
  oltre a `Skeleton` in (6, 0, 6). Tutti partono fuori dal raggio di aggro e girati
  verso il centro: il cavaliere li va a cercare.
- `SandboxFixture.LoadSandbox` spegne gli scheletri diversi da `Skeleton`, perché
  i test su un nemico non vengano disturbati dagli altri. Con `allSkeletons: true`
  carica la stanza della build. Un test verifica i tre scheletri: sul NavMesh,
  fermi in Idle, fuori dal raggio.
- `DamageNumbers` passa la camera della Canvas alla conversione da schermo a
  Canvas: null in Overlay, la camera in Screen Space Camera. Prima i numeri
  funzionavano solo in Overlay.
- **GIF** (`docs/media/m2_combat.gif`, 647 KB): test PlayMode temporaneo a 20 fps
  fissi (`Time.captureFramerate`), con la camera che scrive sempre in una
  RenderTexture e la Canvas in *Screen Space – Camera*. Così il render include
  l'HUD, e coordinate schermo, input e UI restano coerenti tra loro. Lo scontro
  è con `Skeleton_B`: con lo scheletro principale il cubo in (4, 1, 3) copriva
  la scena.
- **Build** Windows: 0 errori, 0 warning. Avviata headless per 10 s: nel log solo i
  quattro `Failed to create agent` della trappola 12 della M1 (player e tre scheletri).
- **Punto di controllo:** dalla chiusura della M0 (23 settembre) alla chiusura
  della M2 sono 1,5 settimane, contro le circa 8 della stima (M1 + M2 = 50 h a
  6 h a settimana). Rapporto 0,19, sotto la soglia di 1,5: nessuna linea di
  taglio. Le stime in ore sono state scritte quando il codice lo scriveva Mirco,
  quindi da qui in avanti sovrastimano.

---

## Trappole note

1. **Il collider del nemico può stare su un figlio.** Oggi è sulla radice, ma dal
   raycast si risale comunque con `GetComponentInParent<IDamageable>()`, non
   `GetComponent`: un collider per osso o un modello diverso non rompono niente.

2. **Portata d'attacco e `stoppingDistance` sono due cose diverse.** La portata va
   misurata tra i bordi (distanza tra i centri meno i raggi), e a portata l'agent
   va fermato con `ResetPath`. Se lo si lascia fermare da solo con
   `stoppingDistance`, il player si ferma a distanze diverse a seconda dell'angolo.

3. **Gli agent si spingono a vicenda.** Con `avoidancePriority` uguale, tre
   scheletri spostano il cavaliere. Il player deve avere priorità più alta
   (numero più basso).

4. **I trigger dell'Animator si accumulano.** Un `SetTrigger("Attack")` arrivato
   durante una transizione resta armato e fa partire un secondo attacco. Per gli
   one-shot si usa `CrossFadeInFixedTime` sullo stato, non i trigger.

5. **Hit stop con `Time.timeScale = 0`:** le coroutine con `WaitForSeconds` si
   bloccano, serve `WaitForSecondsRealtime`. Due hit stop ravvicinati non devono
   sommarsi, e alla fine va ripristinato il valore precedente, non 1: la
   schermata di morte potrebbe aver messo in pausa.

6. **Ricaricare la scena non azzera lo stato statico.** Eventi `static`,
   singleton e `Time.timeScale` sopravvivono al `LoadScene`. Niente eventi statici;
   `timeScale` rimesso a 1 prima di ricaricare.

7. **`IsPointerOverGameObject` dentro una callback dell'Input System** risponde
   con lo stato della UI del frame precedente, e Unity lo segnala con un warning.
   Il controllo va fatto in `Update` e messo in cache, oppure con un raycast
   esplicito della UI.

8. **Il flash su URP Lit:** portare `_BaseColor` a bianco non schiarisce niente,
   perché il colore base moltiplica la texture. Le strade sono l'emissione (va
   attivata la keyword `_EMISSION` sul materiale) o lo scambio temporaneo con un
   materiale bianco unlit. Un `MaterialPropertyBlock` toglie quel renderer dal
   batching SRP: con pochi nemici non conta, alla M7 con gli sciami sì.

9. **TextMeshPro chiede le "TMP Essential Resources"** al primo uso. In batchmode
   vanno importate esplicitamente dal pacchetto incluso in `com.unity.ugui`.

10. **Il colpo parte dopo un ritardo, e nel frattempo il mondo cambia.** Al momento
    del danno il bersaglio può essere morto, scappato o distrutto: si ricontrolla
    tutto, compreso il null "finto" di Unity (`target == null`, non `is null`).

11. **Gli scheletri hanno il culling dell'Animator acceso** (giusto, a differenza
    del player). In batchmode le loro ossa non si muovono: i test PlayMode
    controllano stati e vita, non le pose.

12. **`HealthOrb.Bind` e `OnEnable` arrivano in ordine qualsiasi.** L'iscrizione va
    fatta da chi arriva secondo: `Bind` si iscrive se il componente è già attivo,
    `OnEnable` si iscrive se il bind è già avvenuto. `OnDisable` si disiscrive sempre.

---

## Test

| Tipo | Cosa |
|---|---|
| EditMode | `HealthModel`: i cinque casi del passo 2.1 |
| EditMode | `MeleeAttack` nella parte di temporizzazione, se estratta in una classe pura: non estratta, la coprono i test PlayMode |
| PlayMode (D3) | click-to-move della M1; click su scheletro → la sua vita scende; scheletro che insegue e colpisce; morte del player → schermata di morte. A chiusura: 36 test in nove classi, più gli 11 EditMode |

---

## Checklist di chiusura

- [x] Click su uno scheletro: il cavaliere si avvicina e lo colpisce
- [x] Lo scheletro insegue, attacca, reagisce ai colpi e muore
- [x] Sfera della vita aggiornata via eventi, nessun polling
- [x] Morte del player → schermata → **Ricomincia** riporta tutto allo stato iniziale
- [x] Flash, hit stop, numeri di danno, effetti sonori
- [x] Prefab `Skeleton` in `Assets/_Project/Prefabs/`
- [x] Test EditMode verdi (e PlayMode, se D3)
- [x] Console pulita
- [x] **Build in cui sopravvivi a tre scheletri o muori provandoci** (provata a mano da Mirco il 3 ott 2026)
- [x] `CREDITS.md` aggiornato
- [x] GIF per il README
- [x] Commit, push e tag `m2`
