# M2 — "Colpisco e muoio"

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
  condiviso: legge la velocità dall'agent e riceve i comandi one-shot
  (`PlayAttack`, `PlayHit`, `PlayDeath`). Dopo la morte ignora ogni altro comando.
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

**Verifica:** test PlayMode — click su uno scheletro fermo, il cavaliere si
avvicina, attacca, la vita dello scheletro scende.

---

## Passo 2.5 — IA dello scheletro

`EnemyAI` con `enum State { Idle, Chase, Attack, Dead }` e uno `switch` in `Update`:

- **Idle:** fermo finché il player non entra nel raggio di aggro (con linea di vista
  libera dagli ostacoli: un raycast sul layer `Obstacle`).
- **Chase:** `SetDestination` verso il player, non a ogni frame (trappola 4 della M1).
- **Attack:** delega a `MeleeAttack`.
- **Dead:** stato terminale, entra su `Health.Died`.

Una state machine a classi è rimandata alla M7, quando i tipi di nemico saranno tre.

**Verifica:** test PlayMode — il player si avvicina, lo scheletro lo insegue e
gli toglie vita.

---

## Passo 2.6 — HUD e composition root

1. Canvas *Screen Space – Overlay*, `EventSystem` con `InputSystemUIInputModule`.
2. `HealthOrb`: `Image` *Filled* verticale.
   **Nessun polling** in `Update`. Si iscrive a `HealthChanged`.
3. `CompositionRoot` in `DarkDescent.Core`: riferimenti serializzati a player,
   nemici, HUD e schermata di morte; in `Awake` passa a ognuno ciò che gli serve
   (`HealthOrb.Bind(Health)`, `EnemyAI.Init(Transform player)`).
4. Nel `PlayerController`, il click sopra la UI non deve muovere il player
   (trappola 10 della M1, e trappola 7 qui sotto).

**Verifica:** la sfera scende quando lo scheletro colpisce.

---

## Passo 2.7 — Morte e ricomincia

- Player morto: input disattivato, agent fermo, animazione di morte, poi la
  schermata con il pulsante **Ricomincia** (ricarica la scena).
- Nemico morto: collider e agent disattivati (niente più click né spinte),
  il corpo resta per qualche secondo e poi sparisce.

**Verifica:** muori, ricominci, tutto riparte pulito: vita piena, nemici vivi,
`Time.timeScale` a 1.

---

## Passo 2.8 — Game feel

- **Flash** bianco sul bersaglio colpito, 0,1 s, su **tutti** i renderer del modello
  (i KayKit ne hanno fino a nove).
- **Hit stop** di 0,05 s quando un colpo del player va a segno.
- **Numeri di danno** fluttuanti in TextMeshPro, da un pool (`UnityEngine.Pool.ObjectPool`).
- **Effetti sonori provvisori:** fendente, impatto, morte.

**Verifica:** a occhio e a orecchio, in build.

---

## Passo 2.9 — Chiusura

Tre scheletri in `Sandbox_Combat`, prefab `Skeleton` in `Prefabs/`, build, GIF
per il README, tag `m2`. Punto di controllo del piano (§ 1.3, in settimane di
calendario).

---

## Trappole note

1. **Il collider del nemico sta sul modello, non sulla radice.** Dal raycast si
   risale con `GetComponentInParent<IDamageable>()`, non `GetComponent`.

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
| EditMode | `MeleeAttack` nella parte di temporizzazione, se estratta in una classe pura (intervallo e ritardo del colpo) |
| PlayMode (D3) | click-to-move della M1; click su scheletro → la sua vita scende; scheletro che insegue e colpisce; morte del player → schermata di morte |

---

## Checklist di chiusura

- [ ] Click su uno scheletro: il cavaliere si avvicina e lo colpisce
- [ ] Lo scheletro insegue, attacca, reagisce ai colpi e muore
- [ ] Sfera della vita aggiornata via eventi, nessun polling
- [ ] Morte del player → schermata → **Ricomincia** riporta tutto allo stato iniziale
- [ ] Flash, hit stop, numeri di danno, effetti sonori
- [ ] Prefab `Skeleton` in `Assets/_Project/Prefabs/`
- [ ] Test EditMode verdi (e PlayMode, se D3)
- [ ] Console pulita
- [ ] **Build in cui sopravvivi a tre scheletri o muori provandoci**
- [ ] `CREDITS.md` aggiornato
- [ ] GIF per il README
- [ ] Commit, push e tag `m2`
