# Piano di Sviluppo — DarkDescent

**ARPG isometrico dark fantasy ispirato a Diablo 1 · versione 2.5**

**Profilo:** sviluppatore esperto, Unity da zero · 6–10 h/settimana
**Obiettivo doppio:** (1) un gioco giocabile e finito, (2) un progetto che regga come materiale da portfolio — repo curato, ADR, build giocabile (§ 8).

| Versione | Data | Cosa cambia |
|---|---|---|
| v1 | — | Piano originale, organizzato per sistemi |
| v2.0 | 20 set 2026 | Milestone verticali, Definition of Done, M0, controllo dell'ambito |
| v2.1 | 23 set 2026 | Stime in ore con consuntivo · linee di taglio e punti di controllo · design di gioco minimo · architettura trasversale · licenze degli asset nel repo pubblico · M2.5 (CI) · dungeon diviso in M6/M7 · test assegnati a ogni milestone · prima build pubblica anticipata a M3 · rituale di chiusura |
| v2.2 | 23 set 2026 | Code review del piano: quota LFS da verificare invece che asserita · ADR-004 con opzione (a) consigliata al posto della (b) · Blender spostato fuori dalla stima di M11 · licenza Mixamo qualificata come lettura prudente · § 4.2 decisa a inizio M2 · segnalata l'incoerenza tra M1 e la sua scheda |
| v2.3 | 30 set 2026 | Rimosso il metodo "io spiego, tu scrivi": Claude scrive il codice di gameplay. Allineati § 8, intestazione e appendice; `CLAUDE.md` aggiornato nel repo |
| v2.4 | 30 set 2026 | Rimossi il devlog e le parti didattiche (esercizi, letture, esperimenti): ore reali solo nella tabella del § 5; § 6 e § 8 allineati, `CLAUDE.md` e scheda M1 aggiornati |
| v2.5 | 1 ott 2026 | Piano spostato nel repo, `docs/Piano_Sviluppo_ARPG.md`, quindi versionato · tolte le ore reali (colonna, rituale) e la retrospettiva · punti di controllo misurati in settimane di calendario · M1 chiusa, scheda M2 scritta |

---

## Stato del progetto — aggiornato al 1 ottobre 2026

| | |
|---|---|
| **Nome** | DarkDescent |
| **Repo** | `github.com/zanzaro-mirco/DarkDescent` (pubblico) |
| **Cartella locale** | `game_projects/DarkDescent` |
| **Engine** | Unity 6.3 LTS — **6000.3.24f1**, bloccata per tutto il progetto (ADR-001) |
| **Render pipeline** | URP 17.3.0 |
| **Package** | Input System 1.20.0 · AI Navigation 2.0.14 · Cinemachine 3.1.7 · Test Framework 1.6.0 |
| **Assembly** | `DarkDescent.asmdef` in `Assets/_Project/Scripts/` (ADR-003) |
| **Milestone chiuse** | M0 — Fondamenta (23 set 2026) · M1 — "Mi muovo" (1 ott 2026, tag `m1`) |
| **Milestone corrente** | **M2 — "Colpisco e muoio"** → `docs/milestones/M2_Colpisco_e_muoio.md` |
| **ADR-004** | **Decisa il 1 ott 2026: opzione (a), solo asset CC0** (§ 1.4). Personaggio e animazioni da KayKit (Adventurers + Character Animations, rig `Rig_Medium`). Voce in `DECISIONS.md` da scrivere |
| **Documenti vivi** | questo piano (`docs/Piano_Sviluppo_ARPG.md`) · `DECISIONS.md` (ADR-001…003) · `CONVENTIONS.md` · `ICEBOX.md` · `CREDITS.md` · `CLAUDE.md` |

Questa tabella si aggiorna a ogni chiusura di milestone (§ 6). Il dettaglio del passo corrente sta nella scheda della milestone, non qui: il piano dice *cosa* e *perché*, le schede dicono *come*.

---

## 1. Ambito

### 1.1 Il "Minimum Playable Diablo"

**Dentro (v1.0):**

- 1 classe giocante, il Guerriero
- Combattimento corpo a corpo + 3 incantesimi
- 8 livelli di dungeon generati proceduralmente: 1–4 cripta, 5–8 caverne
- 4 archetipi di nemico + 1 boss finale
- Loot casuale con prefissi e suffissi, ~20 oggetti base
- Inventario a griglia, equipaggiamento, pozioni
- Città hub con 1 mercante
- Salvataggio e caricamento
- Oscurità con raggio di luce attorno al giocatore, automappa
- Musica ambientale ed effetti sonori

**Fuori, esplicitamente:** multiplayer · quest system · dialoghi e NPC multipli · crafting · altre classi · set e oggetti unici complessi · cinematiche · localizzazione · gamepad · achievement · livelli di difficoltà.

Un'idea fuori ambito va in `ICEBOX.md`, non nel codice.

### 1.2 Il nucleo intoccabile e le linee di taglio

Un progetto solo raramente muore perché è troppo ambizioso: muore al 70%, quando il tempo finisce e non c'è niente di pubblicabile. Per questo si decide **adesso**, a mente fredda, cosa si taglia in caso di ritardo — e cosa non si taglia mai.

**Nucleo intoccabile**, senza cui non è più un gioco alla Diablo:
click-to-move con un combattimento che "si sente" · loot con affissi · inventario a griglia · dungeon procedurale · discesa verso un boss · salvataggio.

**Linee di taglio**, da applicare una alla volta e in quest'ordine:

1. **Magia (M9):** da 3 incantesimi a 1, il proiettile. Il nemico a distanza resta, perché usa lo stesso sistema di proiettili.
2. **Caverne (M7):** niente secondo algoritmo; i livelli 5–8 sono generati con BSP e si distinguono per luce e palette.
3. **Città (M10):** l'hub 3D diventa una schermata del mercante tra un livello e l'altro.
4. **Profondità:** da 8 livelli a 6.
5. **Arte (M11):** già il percorso base — nessun taglio disponibile qui, semmai si rinuncia all'estensione Blender.

### 1.3 Stime e punti di controllo

Le stime sono in **ore di lavoro**: servono a dimensionare le milestone. Le ore reali non si registrano (decisione del 1 ott 2026).

**Totale stimato:** 240–350 ore. A 8 h a settimana sono 30–44 settimane di lavoro effettivo; con pause, vacanze e settimane saltate, **8–12 mesi di calendario**.

**Punti di controllo, alla chiusura di M2 e di M5:** confronta le **settimane di calendario** impiegate con la stima massima delle milestone chiuse, convertita a 6 h a settimana (M1 + M2 = 50 h → circa 8 settimane). Se il rapporto supera **1,5**, applica la prossima linea di taglio e ristima il resto. È una regola meccanica di proposito: la decisione di tagliare, presa da stanchi e in ritardo, non arriva mai. Le date di inizio e chiusura stanno già nella storia git e nei tag.

### 1.4 Proprietà intellettuale e licenze degli asset

**Diablo.** Ispirazione sì, riproduzione no: nessun asset, nome, testo, musica o logo originale.

**Asset di terzi in un repo pubblico.** È un vincolo che il repo pubblico rende concreto, e che arriva al passo **1.4** con il primo personaggio:

| Fonte | Nel gioco | Nel repo pubblico |
|---|---|---|
| Kenney, Quaternius, KayKit (CC0) | Sì | Sì — con credito in `CREDITS.md` |
| Freesound | Dipende dal file | Solo CC0 o CC-BY (con credito); mai licenze NC se il gioco potrebbe diventare commerciale |
| **Mixamo** | Sì, royalty free | **No** — lettura prudente, non certezza legale. Le FAQ di Adobe vietano la ridistribuzione dei file grezzi di personaggi e animazioni; se un repo pubblico ricada in quel divieto è discutibile, ma in caso di dubbio si sceglie l'interpretazione cauta |
| **Unity Asset Store** | Sì | **No.** La licenza ne vieta la redistribuzione |

Le opzioni, da fissare in **ADR-004 prima del passo 1.4**:

- **(a) Solo asset CC0**, anche per personaggi e animazioni (Quaternius ne pubblica di animati). Nessuna complessità, repo pubblico integro e clonabile da tutti. *Opzione consigliata.* Prima di decidere, mezz'ora di verifica: le animazioni CC0 disponibili coprono corsa, attacco con arma, colpo subito e morte? Se sì, questa opzione non ha svantaggi.
- **(b) Submodule Git privato:** un repo privato `DarkDescent-Licensed` montato in `Assets/_Licensed/`. Il repo pubblico mostra tutto il codice, tu e la CI clonate il progetto completo. *Sconsigliata, salvo che Mixamo risulti indispensabile:* il repo pubblico smette di essere clonabile e compilabile da chiunque altro — chi valuta il portfolio legge il codice ma non può far partire il progetto, e metà del valore di un repo pubblico è proprio quello. In più complica la CI di M2.5 (token per il checkout del submodule, PR da fork che non buildano).
- **(c) Repo privato fino al rilascio.** Semplice, ma perdi la visibilità da portfolio, che è metà dell'obiettivo.

Da quel momento ogni asset esterno entra con una riga in `CREDITS.md`: fonte, autore, licenza, link.

---

## 2. Design di gioco minimo — bozza

La v2.0 era tutta tecnica. Ma già M2 deve sapere come si calcola un colpo, e M4 quali statistiche esistono: senza una pagina di design, queste decisioni si prendono di fretta dentro il codice. Quella che segue è una **bozza**, e ogni punto indica entro quale milestone va confermato.

**Core loop.** Città → scendi → esplori e combatti → raccogli → torni in città → vendi, compri, equipaggi → scendi più in basso. *Come* si risale da un livello profondo (scale, pergamena, portale) si decide in M10.

**Personaggio:** quattro attributi, da confermare entro **M4**, quando nasce lo `StatSystem`.

| Attributo | Influenza |
|---|---|
| Forza | danno fisico, requisiti delle armi |
| Destrezza | probabilità di colpire, blocco |
| Magia | mana, danno degli incantesimi |
| Vitalità | punti vita |

**Formula del colpo**, bozza per M4 (in M2 basta un danno fisso): la probabilità di colpire dipende dalla Destrezza di chi attacca e dall'Armatura del bersaglio, limitata tra il 5% e il 95%; il danno è un tiro tra minimo e massimo dell'arma, moltiplicato per (1 + Forza/100).

**Archetipi di nemico**, definiti per *comportamento*, perché è il comportamento che costa lavoro, non il modello:

| Archetipo | Comportamento | Cosa insegna | Milestone |
|---|---|---|---|
| Melee base (scheletro) | insegue e colpisce | state machine, NavMeshAgent | M2 |
| Sciame | veloce, fragile, in gruppo | evitamento reciproco, spawn di gruppo | M7 |
| Bruto | lento, resistente, colpo caricato e telegrafato | telegraphing, finestre di reazione | M7 |
| A distanza | tiene le distanze e tira proiettili | posizionamento, pooling | M9 |
| **Boss** | a fasi, colpi telegrafati, evoca uno sciame | composizione dei pattern precedenti | M10 |

**Dungeon.** Livelli 1–4, *cripta*: stanze e corridoi, generati con BSP. Livelli 5–8, *caverne*: spazi organici, random walk. Boss al livello 8.

---

## 3. Stack tecnologico

| Strumento | Scelta | Perché |
|---|---|---|
| Engine | **Unity 6.3 LTS — 6000.3.24f1** | Mercato del lavoro più grande di Godot, C# che già conosci, documentazione ovunque. Versione bloccata, niente upgrade a metà (ADR-001) |
| Render pipeline | **URP 17.3.0** | Built-in è di fatto deprecato, HDRP è sovradimensionato. URP dà Shader Graph e Render Graph per il look retro |
| Input | **Input System 1.20** | Il vecchio Input Manager farebbe risparmiare mezza giornata ora e costerebbe una riscrittura dopo (ADR-002) |
| Navigazione | **AI Navigation 2.0** (`NavMeshSurface`) | Permette il bake a runtime che servirà al dungeon procedurale |
| Camera | **Cinemachine 3.1**, da M3 | Prima la camera scritta a mano in M1, per sapere cosa fa sotto |
| Test | **Unity Test Framework 1.6** | Test EditMode sulla logica pura, da M2 |
| IDE | **Visual Studio Community** (installato), Rider opzionale | Rider ha analisi statica specifica per Unity: da valutare dopo M1 |
| Version control | **Git + LFS**, repo pubblico su GitHub | La quota LFS gratuita di GitHub è **da verificare in *Settings → Billing and licensing*** prima di progettare la CI: risulta 1 GiB di spazio e 1 GiB di banda al mese sul piano Free, non i 10 GiB scritti in una versione precedente di questo piano. In ogni caso i download della CI consumano banda (vedi M2.5) |
| Modellazione | **Blender**, estensione opzionale di M11 | Solo se il tempo avanza: vedi la strategia asset e la stima di M11 |
| Animazioni | CC0 KayKit (ADR-004), rig Generic | Humanoid scarta la rotazione dell'osso `chest` di KayKit: si usa Generic |

### Strategia asset: grey-box prima, arte dopo

Tutto nasce con cubi e capsule grigie; l'arte arriva quando il gameplay funziona. Nella fase intermedia: Kenney, Quaternius, KayKit (CC0). Blender entra in M11, quando sai esattamente quali pezzi servono e con quali misure: modellare prima significa modellare cose che butterai.

### Trappola ricorrente: tutorial scritti per versioni vecchie

Gran parte del materiale online precede Unity 6. Quando un tutorial non torna, spesso il problema è la versione, non il tuo codice.

| Nei tutorial vecchi | Nella tua versione |
|---|---|
| `CinemachineVirtualCamera`, namespace `Cinemachine` | `CinemachineCamera`, namespace `Unity.Cinemachine` (Cinemachine 3) |
| Finestra *Navigation* con bake della scena | Componente `NavMeshSurface` (AI Navigation 2) |
| `Input.GetKey`, `Input.GetMouseButton` | Input System: azioni e callback |
| `FindObjectOfType<T>()` | Deprecato in favore di `FindFirstObjectByType` / `FindAnyObjectByType` — e comunque da evitare |
| `rigidbody.velocity` | `rigidbody.linearVelocity` |
| *File → Build Settings* | *File → Build Profiles* |

La fonte di verità è la documentazione ufficiale **per la tua versione** — su docs.unity3d.com c'è il selettore di versione — e quella del singolo package.

---

## 4. Architettura trasversale

Le milestone descrivono sistemi; questa sezione raccoglie le decisioni che li attraversano tutti. Ognuna ha il suo momento: presa prima è over-engineering, presa dopo costa un refactoring.

### 4.1 Logica pura separata dai MonoBehaviour — da M2

La logica di gioco (danno, statistiche, affissi, generazione del dungeon, salvataggio) vive in **classi C# semplici**; i `MonoBehaviour` sono un guscio sottile che la collega a Unity. Per esempio, `HealthModel` sa ricevere danno e sapere se è morto; il componente `Health` la contiene, pubblica gli eventi e parla con l'Animator.

La ragione è pratica: una classe semplice si testa in EditMode in millisecondi, senza scene e senza Play Mode. È il pattern *humble object*, e da solo rende possibile tutta la strategia di test di questo piano.

### 4.2 Come si trovano i sistemi — decisione in M2

In M2 l'HUD deve conoscere la `Health` del giocatore. Finché c'è una scena sola basta un riferimento trascinato nell'Inspector; da M3 il giocatore attraversa le scene, da M6 i livelli nascono a runtime, e i riferimenti trascinati a mano smettono di bastare. La scorciatoia che prendono tutti è un `Find` o un singleton per ogni cosa. Le opzioni vere:

- **Composition root:** un oggetto nella scena persistente (§ 4.3) che all'avvio crea e collega i sistemi. Esplicito, si legge dall'alto in basso, si debugga. *Punto di partenza consigliato.*
- **Singleton statici:** rapidi da scrivere, ma accoppiano tutto e complicano i test. Accettabili solo per servizi davvero globali e senza stato di gioco, come l'audio.
- **ScriptableObject come canali di eventi:** disaccoppiamento forte tra scene; elegante ma indiretto, e rende più difficile seguire il flusso a chi comincia. Solo dove serve davvero.

La scelta si prende **all'inizio di M2** — l'HUD non si può scrivere senza — e si documenta in un ADR alla chiusura, quando si è visto come ha retto.

### 4.3 Struttura delle scene — decisione in M3

Proposta: una scena **`Core`** sempre caricata (giocatore, camera, HUD, EventSystem, manager) e i livelli caricati **in modo additivo** sopra (`Level_Crypt_01`, poi `Town`, poi la scena vuota in cui il generatore costruisce il dungeon). Giocatore e interfaccia sopravvivono al cambio di livello senza `DontDestroyOnLoad`, e in M6 un livello procedurale è semplicemente un'altra scena da caricare. Una scena `Bootstrap` minima fa da punto d'ingresso della build.

La scelta va in un ADR prima di scrivere il `LevelManager` di M3.

### 4.4 Dati: definizioni con ID stabili — da M4

La regola è già nota: **ScriptableObject = definizione immutabile, classe C# = istanza runtime.** Si aggiunge un vincolo che conviene rispettare da subito: ogni definizione ha un **ID stabile** — una stringa generata una volta e mai più cambiata, non il nome dell'asset — e le istanze runtime riferiscono le definizioni attraverso quell'ID. Un registro (`ItemDatabase`) risolve ID → definizione.

Senza questo, in M8 si scopre che un riferimento a uno ScriptableObject non sopravvive a un salvataggio su file (`JsonUtility` scrive un identificativo che cambia a ogni avvio), e l'inventario va riscritto per poter salvare la partita.

### 4.5 Assembly definition

Oggi c'è un solo `DarkDescent.asmdef` (ADR-003). Da M2 si aggiunge `DarkDescent.Tests.EditMode`, che referenzia `DarkDescent` e il Test Framework — possibile proprio perché il codice non sta in `Assembly-CSharp`. La divisione per area (Player, Combat, Items…) si valuta in M6, sulle dipendenze reali.

---

## 5. Roadmap

Ogni milestone si chiude con una **build eseguibile** e con il rituale del § 6. Se non parte in build, non è finita.

| # | Milestone | Risultato | Ore stimate | Stato |
|---|---|---|---|---|
| M0 | Fondamenta | repo e build vuota da clone pulito | — | ✅ 23 set |
| M1 | "Mi muovo" | cammini in una stanza | 12–20 | ✅ 1 ott |
| M2 | "Colpisco e muoio" | primo gameplay loop | 20–30 | |
| M2.5 | Pipeline automatica | test e build in CI | 5–10 | |
| M3 | "Un dungeon fatto a mano" | due livelli, atmosfera, **prima build pubblica** | 20–28 | |
| M4 | "Raccolgo roba" | drop, inventario, equipaggiamento | 24–36 | |
| M5 | "Loot casuale" | affissi e rarità | 16–24 | |
| M6 | "Dungeon infinito" | cripta procedurale | 28–36 | |
| M7 | "Le profondità" | caverne, nuovi nemici, automappa | 24–32 | |
| M8 | "Progressione e persistenza" | livelli, attributi, salvataggio | 16–24 | |
| M9 | "Magia" | mana, incantesimi, nemico a distanza | 24–32 | |
| M10 | "Città e loop completo" | il gioco è finibile | 20–28 | |
| M11 | "Look, feel e release" | arte, audio, shader, v1.0 | 30–50 | |

---

### M0 — Fondamenta ✅

Chiusa il 23 settembre 2026, con la Definition of Done verificata da un clone pulito. Lezioni emerse (il devlog della M0 resta nella storia git del repo):

- **Verificare LFS su un repo senza binari non prova nulla:** si confronta il pointer nel blob con il file dopo il checkout. Il primo binario vero arriva al passo 1.4.
- **L'asmdef va creato prima del codice:** un assembly con `.asmdef` non può referenziare `Assembly-CSharp`. Per questo la classe generata dall'Input System va generata sotto `Scripts/` (ADR-003).
- **"Line Endings For New Scripts" su Unix non basta:** serve la regola `eol=lf` in `.gitattributes` per ogni nuovo tipo di file testuale (aggiunta per `*.asmdef` e `*.inputactions`).
- **Residuo del template:** `Assets/InputSystem_Actions.inputactions`, registrato come *Project-wide Actions*. Si stacca da *Project Settings → Input System Package* e si cancella al passo 1.2.

---

### M1 — "Mi muovo" ✅

Chiusa il 1 ottobre 2026: build Windows provata a mano, tag `m1`. Lezioni emerse:

- **I passi nell'editor si possono fare in batchmode.** Script di editor usa e getta lanciati con `-executeMethod`, verifiche con test PlayMode temporanei su `InputTestFixture` (mouse virtuale), poi rimossi. Trappola: in batchmode un errore di compilazione blocca l'import degli asset, quindi non si può usare una classe generata prima di averla generata.
- **KayKit va importato Generic, non Humanoid:** Humanoid non mappa l'osso `chest` e ne scarta la rotazione in ogni clip. Tutto KayKit condivide `Rig_Medium`, quindi il retargeting non serve.
- **NavMesh:** il bake va limitato ai layer dell'ambiente (altrimenti il player si ritaglia un buco), serve la height mesh (altrimenti si galleggia di 8 cm), e gli ostacoli vanno marcati *Not Walkable* per non creare isole sulle loro facce superiori.
- **Il raggio del click deve fermarsi anche sugli ostacoli,** non solo sul pavimento: altrimenti un click su un cubo porta il player alle sue spalle.
- **LFS verificato davvero** con i primi binari: nel blob c'è il pointer, su disco il file.

---

### M1 — "Mi muovo" · 12–20 h

**A schermo:** una stanza grigia con ostacoli; clicchi, il personaggio ci arriva animato aggirando gli ostacoli, la camera isometrica lo segue morbida.

**Contenuto:** scena sandbox e camera scritta a mano · Input System con classe C# generata · NavMesh e click-to-move · personaggio animato con blend tree · architettura in quattro componenti (`PlayerInputReader`, `PlayerMotor`, `PlayerController`, `PlayerAnimatorDriver`) più `CameraFollow`. Dettagli, scheletri e trappole: `docs/milestones/M1_Mi_Muovo.md`.

**ADR-004 decisa il 1 ott 2026: opzione (a), solo CC0.** Personaggio e animazioni da KayKit, rig Generic; la scheda M1 è allineata.

**Definition of Done:** in build, clicchi ovunque nella stanza e il personaggio ci arriva aggirando gli ostacoli, animato, senza scatti della camera.

**Per il CV:** ciclo di vita dei componenti, Input System, NavMesh, Animator.

---

### M2 — "Colpisco e muoio" · 20–30 h

**A schermo:** clicchi su uno scheletro, il personaggio si avvicina e attacca; lo scheletro reagisce, ti insegue, ti colpisce. Uno dei due muore. Una sfera rossa mostra la vita.

Chiude il **primo gameplay loop**: da qui in poi migliori un gioco, non ne costruisci uno.

**Contenuto:**

- `DamageInfo` come struct e interfaccia `IDamageable`. Una struct, non `float + GameObject`: quando arriveranno critici, elementi e knockback nessuna firma dovrà cambiare.
  ```csharp
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
  ```
- `HealthModel` in C# puro e componente `Health` che la avvolge (§ 4.1), con gli eventi `OnHealthChanged` e `OnDied`
- Targeting: click su un nemico → avvicinamento → attacco
- IA nemica con `enum` e `switch` (`Idle`, `Chase`, `Attack`, `Dead`). Con un nemico solo, una state machine a classi è over-engineering: il refactoring arriva in M7, quando i tipi di nemico saranno tre
- Danno applicato dopo un ritardo configurato sull'arma, non con un Animation Event, che si perde a ogni reimport della clip
- HUD: la sfera della vita, iscritta agli eventi; quella del mana arriva con la magia in M9
- Schermata di morte con possibilità di ricominciare
- **Game feel:** flash bianco sul bersaglio colpito (0,1 s), hit stop di 0,05 s, numeri di danno fluttuanti ed **effetti sonori provvisori** (fendente, impatto, morte). L'audio fa metà della sensazione d'impatto: non va rimandato alla fine

**Decisione da prendere:** come si trovano i sistemi (§ 4.2).

**Scheda operativa:** `docs/milestones/M2_Colpisco_e_muoio.md`, con le decisioni D1–D4 da prendere prima di cominciare.

**Test:** primo assembly di test (§ 4.5). Su `HealthModel`: il danno riduce la vita, a zero la morte scatta una volta sola, la vita non va sotto zero, dopo la morte non si subisce altro danno.

**Definition of Done:** in build, sopravvivi a tre scheletri o muori provandoci, e dalla schermata di morte puoi ricominciare. Test verdi.

**Punto di controllo** (§ 1.3).

**Per il CV:** interfacce ed eventi in C#, state machine, testabilità, game feel.

---

### M2.5 — Pipeline automatica · 5–10 h

Una milestone piccola, ma con un posto preciso: nella v2.0 la CI stava "verso M3–M4", cioè da nessuna parte. Ora esistono i primi test, e la CI ha qualcosa da verificare.

**Contenuto:**

- GitHub Actions con **GameCI**, con la licenza Unity Personal attivata tramite i secrets del repo
- Job di **test EditMode** su ogni push e pull request verso `main`
- Job di **build Windows** su tag (`m2`, `m3`… e `v*` per le release) o ad avvio manuale, con la build scaricabile come artifact
- **Cache** della cartella `Library/` e degli oggetti LFS. Il repo è pubblico, quindi i minuti di Actions sui runner standard non si pagano; ogni checkout che scarica file LFS però consuma la banda LFS gratuita, che è il vincolo stretto (verifica la quota effettiva, § 3). È per questo che la build gira sui tag e non a ogni push
- Badge di stato nel README
- Con il submodule privato (ADR-004, opzione b): un token di accesso per il checkout

**Definition of Done:** un push con un test volutamente rotto fa diventare rosso il job — la prova che la CI intercetta davvero i problemi — e un tag produce una build scaricabile che parte.

**Per il CV:** CI/CD per un progetto Unity, una riga che da sola distingue un portfolio.

---

### M3 — "Un dungeon fatto a mano" · 20–28 h

**A schermo:** stanze collegate, costruite a mano con un tileset modulare grezzo, buie, illuminate da torce; il personaggio porta con sé un raggio di luce. In fondo, una scala scende al secondo livello, e vita e stato del personaggio lo seguono.

**Perché prima del procedurale:** un generatore va scritto sapendo cosa deve produrre. Costruisci a mano il livello che vorresti, misura i moduli, capisci quali pezzi servono: poi automatizzi.

**Contenuto:**

- Griglia di moduli a dimensione fissa (4×4 m come punto di partenza): pavimento, muro, angolo, porta, scala
- Due livelli fatti a mano, con la **struttura a scene additive** (§ 4.3) e un `LevelManager`
- Illuminazione URP con ombre; oscurità e raggio di luce del giocatore
- Cinemachine 3 al posto del `CameraFollow` scritto a mano (attenzione ai tutorial per la 2.x, § 3)
- **Prima build pubblica su itch.io**, con pagina "in sviluppo", aggiornata a ogni milestone. Un link giocabile a M3 invece che a M11 sono mesi di portfolio in più, e chi lo prova ti dà riscontri veri
- Da valutare in un ADR: una build **Web** oltre a quella Windows. Si gioca nel browser senza scaricare nulla, e per chi valuta un portfolio fa differenza; richiede però il modulo *Web Build Support*, e URP sul web ha limiti che potrebbero pesare sugli shader di M11

**Decisione da prendere:** struttura delle scene (§ 4.3).

**Definition of Done:** in build, scendi dal livello 1 al livello 2 mantenendo vita e stato; la build si scarica da itch.io e parte su un PC che non è il tuo.

**Per il CV:** level design modulare, gestione delle scene, illuminazione in URP, pubblicazione.

---

### M4 — "Raccolgo roba" · 24–36 h

**A schermo:** un nemico lascia cadere una spada; la raccogli, apri l'inventario a griglia, la trascini nello slot dell'arma e il danno nel pannello del personaggio sale. La togli, e torna com'era.

**Contenuto:**

- `ItemDefinition` (ScriptableObject) con **ID stabile**, `ItemInstance` (classe semplice e serializzabile), registro `ItemDatabase` (§ 4.4)
- Oggetti a terra e raccolta
- Inventario a griglia con oggetti su più celle, drag & drop in **uGUI** (per l'HUD di un gioco è più pratico di UI Toolkit, che tornerà utile per i tool dell'editor)
- Slot di equipaggiamento
- `StatSystem`: i quattro attributi del § 2, modificatori fissi e percentuali con un **ordine di applicazione definito e documentato**, formula del colpo del § 2
- Tooltip

**Decisione da prendere:** conferma di attributi e formula del colpo (§ 2).

**Test:** ordine dei modificatori (fissi prima dei percentuali); equipaggiare e poi togliere un oggetto riporta le statistiche esattamente ai valori di partenza.

**Definition of Done:** lo scenario descritto sopra, in build. Test verdi.

**Per il CV:** architettura data-driven, sistema di statistiche, UI complessa.

---

### M5 — "Loot casuale" · 16–24 h

**A schermo:** cade una "Spada Lunga Feroce del Grifone", rara, con tre affissi; il tooltip li mostra e li confronta con l'arma equipaggiata.

È il cuore del genere, e merita una milestone sua.

**Contenuto:** `AffixDefinition` per prefissi e suffissi · pool filtrati per *item level* e tipo di oggetto · livelli di rarità con le rispettive probabilità · generazione del nome · loot table per nemico · confronto nel tooltip.

**Concetti:** estrazione pesata; `System.Random` con **seed riproducibile** — "rigenera il drop 4711" invece di "riprova finché non ricapita".

**Test:** stesso seed, stesso oggetto, sempre; gli affissi rispettano l'item level; su 10.000 estrazioni le rarità restano entro una tolleranza dalle probabilità previste.

**Definition of Done:** in build, con il seed fissato lo stesso nemico lascia sempre lo stesso oggetto; senza seed, cinquanta uccisioni mostrano tutte le rarità. Test verdi.

**Punto di controllo** (§ 1.3).

**Per il CV:** sistemi procedurali, bilanciamento data-driven, test su logica casuale.

---

### M6 — "Dungeon infinito" · 28–36 h

**A schermo:** i livelli 1–4 sono cripte generate a ogni partita, sempre percorribili dall'ingresso all'uscita; nemici e casse aumentano con la profondità.

**Contenuto:**

- Generazione **BSP**, verifica della connettività (flood fill), istanziazione dei moduli di M3
- **NavMesh a runtime** con `NavMeshSurface.BuildNavMesh()`
- Nemici e casse distribuiti in base alla profondità
- Seed riproducibile per ogni livello
- **Tool dell'editor:** una finestra che genera e disegna il dungeon senza entrare in Play Mode. Circa 150 righe, ore di iterazione risparmiate, e "ho scritto tool per l'editor" pesa a un colloquio

**Decisioni da prendere:** NavMesh o A* su griglia, con dati reali alla mano (tempi di bake, comportamento nei corridoi stretti) — un A* scritto da te dà pieno controllo su costi e occupazione delle celle, e insegna il pathfinding sul serio; divisione dell'asmdef per area (§ 4.5).

**Test:** su 500 seed, ogni livello è connesso e nessuna stanza si sovrappone a un'altra.

**Definition of Done:** in build, attraversi di fila quattro livelli generati; con lo stesso seed, ottieni lo stesso dungeon. Test verdi.

**Per il CV:** generazione procedurale, algoritmi su grafi, tooling.

---

### M7 — "Le profondità" · 24–32 h

**A schermo:** dal livello 5 il dungeon cambia: caverne organiche e nemici nuovi — uno sciame veloce, un bruto che carica colpi telegrafati. Un'automappa mostra ciò che hai esplorato.

Nella v2.0 tutto questo stava dentro M6, che sarebbe diventata una milestone di sei settimane senza un risultato intermedio da mostrare. Divise, ognuna ha il suo.

**Contenuto:**

- Generazione **random walk** per le caverne
- Archetipi **sciame** e **bruto** (§ 2)
- **Refactoring dell'IA** da `enum` e `switch` a classi di stato: ora i tipi sono tre e il bisogno è reale. Documentato in un ADR, è materiale da portfolio
- Stato di esplorazione delle celle e **automappa**
- Tabelle di spawn per profondità, con il giusto mix di archetipi

**Test:** gli stessi test di connettività di M6, sul nuovo algoritmo.

**Definition of Done:** in build, attraversi i livelli 5–8 con i nuovi nemici, e l'automappa si riempie mentre esplori. Test verdi.

**Per il CV:** design di IA, refactoring guidato da un bisogno reale, due algoritmi procedurali a confronto.

---

### M8 — "Progressione e persistenza" · 16–24 h

**A schermo:** uccidi, sali di livello, distribuisci punti negli attributi. Chiudi il gioco, lo riapri, e sei dove eri, con lo stesso equipaggiamento.

**Contenuto:** esperienza e curva di livello · punti attributo · **salvataggio e caricamento** in JSON, con un **formato versionato fin dal primo salvataggio** · regole della morte (cosa si perde).

**Test:** salvare e ricaricare restituisce uno stato identico; un salvataggio nel formato 1 si carica con il codice del formato 2 (migrazione).

**Definition of Done:** lo scenario descritto sopra, in build. Test verdi.

**Per il CV:** serializzazione, persistenza versionata, migrazione dei dati.

---

### M9 — "Magia" · 24–32 h

**A schermo:** la sfera blu del mana si svuota mentre lanci dalla hotbar un proiettile, un incantesimo ad area e un potenziamento; un nemico tiene le distanze e ti bersaglia.

**Contenuto:** mana · 3 incantesimi (proiettile, area, potenziamento) · proiettili gestiti con **`UnityEngine.Pool.ObjectPool<T>`** (esiste già, non serve scriverne uno) · hotbar e tempi di ricarica · effetti con il sistema particellare · pergamene che insegnano gli incantesimi · archetipo **a distanza** (§ 2), che riusa i proiettili.

**Definition of Done:** in build, un combattimento misto di mischia e magia contro nemici a distanza; lanciando proiettili a raffica, il Profiler non mostra allocazioni a ogni frame.

**Per il CV:** object pooling, sistemi di abilità estendibili, profiling mirato.

---

### M10 — "Città e loop completo" · 20–28 h

**A schermo:** dal menu arrivi in città, compri dal mercante, scendi, risali a vendere, affronti il boss al livello 8, vedi la schermata di vittoria.

**Contenuto:** città hub · mercante · flusso di gioco completo (menu → città → dungeon → morte o vittoria) costruito sulla struttura a scene di M3 · ritorno in città dai livelli profondi · **boss** (§ 2) · vittoria e riconoscimenti.

**A fine M10 il gioco è finibile.** È il momento di farlo provare a cinque persone e di prendere appunti senza difendersi.

**Definition of Done:** una partita completa, dall'avvio alla vittoria, in build, senza passare dall'editor.

**Per il CV:** gestione dello stato di gioco, design di un boss.

---

### M11 — "Look, feel e release" · 30–50 h (percorso base)

**Contenuto (percorso base):** asset CC0 resi coerenti dallo **shader retro** in Shader Graph (dithering ordinato, palette limitata, eventuale riduzione della risoluzione con una Renderer Feature di URP) · post-processing (vignetta, grana, bloom misurato) · musica ambientale e audio posizionale, con un mixer a gruppi · **profiling** completo (Profiler, Frame Debugger, Memory Profiler) · pagina itch.io definitiva e release **v1.0**.

**Estensione opzionale — Blender.** Sostituire gli asset CC0 con modelli propri è **fuori** dalle 30–50 h: la sola modellazione, partendo da zero con Blender, vale facilmente 80–100 h. Si valuta a M11 chiusa, con la v1.0 già pubblicata, e semmai diventa una v1.1. Non è un taglio: è il percorso base che non la prevede.

**Definition of Done:** v1.0 pubblicata, `CREDITS.md` completo, README con GIF e sezione sull'architettura.

**Per il CV:** Shader Graph, ottimizzazione, pipeline di release.

---

## 6. Rituale di chiusura milestone

Dieci minuti di disciplina che tengono il progetto leggibile anche dopo un mese di pausa.

1. Definition of Done verificata **in build**, non nell'editor
2. Test verdi (da M2) e CI verde (da M2.5)
3. GIF per il README
4. `git tag m<N>` e push del tag (da M2.5 produce anche la build)
5. Build aggiornata su itch.io (da M3)
6. Tabella "Stato del progetto" aggiornata
7. Scheda della milestone successiva in `docs/milestones/`, scritta **prima** di cominciarla

Le schede si scrivono una alla volta, all'inizio di ogni milestone, con quello che si è imparato nella precedente. Scriverle tutte adesso vorrebbe dire riscriverle tutte dopo.

**Abitudini di ogni sessione:** un commit a fine sessione, sempre; un ADR per ogni scelta tecnica non ovvia; uno sguardo al Profiler appena qualcosa sembra lento, non solo in M11.

---

## 7. Competenze per il CV

| Competenza | Dove |
|---|---|
| C# applicato: interfacce, eventi, struct, generics | M2, M4, M5 |
| Unity: ciclo di vita, prefab, ScriptableObject, Animator | M1–M4 |
| Input System, NavMesh, Cinemachine | M1, M3 |
| Architettura: composition root, scene additive, logica testabile | M2, M3 |
| Test automatici (Unity Test Framework) | da M2, in ogni milestone |
| CI/CD (GitHub Actions + GameCI) | M2.5 |
| Architettura data-driven, sistemi di statistiche | M4, M5 |
| Generazione procedurale (BSP, random walk, verifica su grafo) | M6, M7 |
| Design di IA e refactoring guidato dal bisogno | M2, M7 |
| Tool custom per l'editor | M6 |
| Serializzazione e migrazione dei dati | M8 |
| Ottimizzazione: pooling e profiling | M9, M11 |
| URP, Shader Graph, illuminazione | M3, M11 |
| Git, LFS, licenze degli asset in un repo pubblico | M0, M1 |
| Blender (estensione opzionale) | dopo M11 |
| Pubblicazione e release | M3, M11 |

**Come presentarlo:** repo pubblico con un README curato (GIF, schema dell'architettura, link agli ADR), build giocabile su itch.io da M3, e 3–4 articoli tecnici sulle parti più interessanti: il generatore di dungeon, il sistema di affissi, la pipeline di CI, lo shader retro.

---

## 8. Protocollo di lavoro

La versione operativa è **`CLAUDE.md`** nel repo, che Claude Code legge a ogni sessione. In breve:

- **Claude scrive il codice, gameplay compreso:** file completi e funzionanti, non firme da riempire. Tu li rivedi, li provi, li integri.
- **Resta richiesto, in forma breve:** il perché delle scelte architetturali non ovvie (ti serve per gli ADR), le trappole Unity rilevanti, le conseguenze a distanza di una scelta, e una code review vera quando il codice lo scrivi o lo modifichi tu.
- **Niente parti didattiche:** né esercizi, né codice da scrivere o modificare a mano per imparare, né letture o esperimenti. A mano restano i passaggi nell'editor e le decisioni.
- **Niente devlog** dal 30 set 2026: `DEVLOG.md` cancellato dal repo, la voce della M0 resta nella storia git.
- **Rituale di chiusura** a ogni milestone (§ 6).

> Fino al 30 settembre 2026 valeva il metodo **"io spiego, tu scrivi"**: Claude si fermava a concetti, architettura e sole firme, e il corpo dei metodi lo scrivevi tu. Rimosso su tua richiesta a M1 iniziata. Conseguenza sull'obiettivo doppio: la competenza tecnica non si acquisisce più scrivendo, quindi il valore da portfolio si sposta tutto su ciò che resta tuo — le decisioni negli ADR, le scelte di design e di ambito.

---

## 9. Rischi

| Rischio | Segnale d'allarme | Mitigazione |
|---|---|---|
| Calo di motivazione | Due settimane senza commit | Milestone piccole con un risultato visibile; build pubblica da M3; la tabella di stato rende facile ripartire |
| Stime sballate | Settimane di calendario oltre 1,5 volte la stima (§ 1.3) | Punti di controllo a M2 e M5, poi linee di taglio (§ 1.2) |
| Feature creep | Codice per cose fuori ambito | `ICEBOX.md`, ambito scritto (§ 1.1) |
| Collo di bottiglia sull'arte | Ore in Blender prima di M11 | Grey-box e CC0; lo shader retro uniforma asset diversi |
| Licenze degli asset | Un FBX di Mixamo o dell'Asset Store in un commit pubblico | ADR-004 prima del passo 1.4; `CREDITS.md` |
| Quote di GitHub | Avvisi di consumo su LFS | Build in CI solo sui tag, cache LFS, controllo periodico in *Settings → Billing* |
| Documentazione obsoleta | API che "non esistono" | Documentazione per la versione 6000.3; tabella del § 3 |

---

## Appendice — Da v1 a v2

Il piano v1 era tecnicamente corretto ma organizzato per sistemi, senza criteri di completamento, senza la parte che non è gameplay (Git, build, test, CI), senza un ambito definito, e si chiudeva con un prompt che chiedeva script completi — l'opposto dell'obiettivo di imparare. La v2 lo ha sostituito con milestone verticali, Definition of Done osservabili, una M0 dedicata all'infrastruttura, un ambito scritto e un protocollo di lavoro esplicito (§ 8).

Le correzioni tecniche puntuali della v2.0 — Input System nuovo, `DamageInfo` come struct, camera in `LateUpdate` con rotazione `(30, 45, 0)`, `ObjectPool<T>` di Unity, ScriptableObject immutabili, disiscrizione dagli eventi in `OnDisable`, state machine a `enum` finché basta — sono ora assorbite dove servono: nelle milestone di questo documento, in `CONVENTIONS.md`, in `CLAUDE.md` e nelle schede.
