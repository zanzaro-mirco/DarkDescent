# Piano di Sviluppo — DarkDescent

**ARPG isometrico dark fantasy ispirato a Diablo 1 · versione 2.23**

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
| v2.6 | 3 ott 2026 | M2 chiusa, con le lezioni · punto di controllo di M2 superato (rapporto 0,19, nessun taglio) · ADR-004: eccezione per il font OFL di TextMesh Pro, EmojiOne tolta |
| v2.8 | 3 ott 2026 | M2.5 chiusa (tag `m2.5`): test e build in CI con GameCI · ADR-011…013 |
| v2.7 | 3 ott 2026 | Stime ricalcolate: il codice lo scrive Claude, le ore sono quelle di sessione di Mirco (totale da 240–350 a circa 95–140 h) · punti di controllo riformulati sulle nuove stime · audio posizionale anticipato a M3 · Cinemachine a M3 con Impulse e zona morta · reazione al colpo del cavaliere a M7 · pavimento della sandbox a M3 · ADR-004…010 scritti |
| v2.9 | 3 ott 2026 | Prima build pubblica su itch.io spostata da M3 a M10, quando il gioco è finibile: decisione di Mirco. A M3 resta la prova della build Web dall'artifact della CI |
| v2.10 | 3 ott 2026 | M3 chiusa (tag `m3`), con le lezioni · § 4.3 decisa (ADR-014) · build Web provata e tenuta (ADR-019) · ADR-014…019 |
| v2.11 | 4 ott 2026 | M4 chiusa (tag `m4`), con le lezioni · attributi e formula del colpo confermati (§ 2, ADR-020) · inventario a click-e-click invece del drag & drop (ADR-023) · ADR-020…024 |
| v2.15 | 5 ott 2026 | M6 chiusa (tag `m6`), con le lezioni · D4 chiusa con i numeri della build: NavMesh a runtime (ADR-035) · asmdef divisa a strati (§ 4.5, ADR-032) · ADR-032…038 |
| v2.14 | 5 ott 2026 | Dopo la prova della cripta generata, su richiesta di Mirco: **pozioni e cintura**, **automappa** (anticipata dalla M7, sovrapposta o nell'angolo), **ripartenza dall'ingresso del livello** dopo la morte fino alla M8, **tooltip delle statistiche** · M6 da 10–13 a 16–19 h |
| v2.13 | 5 ott 2026 | M5 chiusa (tag `m5`), con le lezioni · punto di controllo della M5 superato (rapporto 0,04, nessun taglio) · danno intero (§ 2, ADR-031) · ADR-025…031 |
| v2.23 | 8 ott 2026 | M7 chiusa (tag `m7`), con le lezioni · la M7 sale a 18–25 h con i tre passi di correzione dopo le prove in build · totale ricalcolato dalla tabella del § 5 · ADR-040…051 |
| v2.22 | 8 ott 2026 | Su richiesta di Mirco: **la vita del nemico che si sta combattendo** resta in alto anche senza il cursore sopra, nella M9 · barre sopra i nemici feriti come opzione, dopo la v1.0 · **nemici speciali** più forti con loot migliore, dopo la v1.0 · il boss della M10 lascia loot migliore |
| v2.21 | 7 ott 2026 | Su richiesta di Mirco: una **punizione alla morte** tra i lavori dopo la v1.0, da valutare prima se le prove della M10 la chiedono |
| v2.20 | 7 ott 2026 | Dopo la stessa prova, su richiesta di Mirco: **alla morte non si perde niente** e il livello non si ricarica (ADR-048, regola che il piano lasciava alla M8) · sciame più leggero, blocco che non ferma, mira sui nemici (ADR-049) · nella M7 il passo 7.11 |
| v2.19 | 7 ott 2026 | Dopo la prova della build M7, su richiesta di Mirco: **elmo, armatura, guanti, stivali, due anelli e amuleto** nella M8, prima del salvataggio, così il formato dei salvataggi nasce con tutti gli slot · M8 da 8–12 a 12–18 h · nella M7 il passo 7.10 di correzioni (D14 della scheda) |
| v2.18 | 6 ott 2026 | Su richiesta di Mirco: **zoom e rotazione della visuale** attorno al cavaliere, nella M8 (proposta: zoom con la rotella, rotazione a scatti di 90° con i muri bassi che seguono la camera; da confermare nella scheda della M8, poi un ADR che aggiorna ADR-017) · M8 da 6–9 a 8–12 h |
| v2.17 | 6 ott 2026 | Su richiesta di Mirco: **colpi critici** del cavaliere, con un verso proprio per ogni tipo di nemico colpito, nella M7 (passo 7.7, D13 confermata) · formula nel § 2 · M7 da 12–18 a 13–20 h |
| v2.16 | 6 ott 2026 | Dopo la prova della M6, su richiesta di Mirco: **musica e rumori d'ambiente** anticipati dalla M11 alla M7 (passo 7.0): un profilo per tipo di livello, tracce CC0 · M7 da 10–15 a 12–18 h · ADR-039 |
| v2.12 | 4 ott 2026 | Decisioni di Mirco sulla M5: **lingue** entrano nella v1.0 (inglese di default, italiano, predisposizione per altre), **blocco** con lo scudo · M5 da 6–9 a 9–12 h · menu delle opzioni con la lingua alla M10 · nuova sezione "Dopo la v1.0" con le armi delle classi future |

---

## Stato del progetto — aggiornato all'8 ottobre 2026

| | |
|---|---|
| **Nome** | DarkDescent |
| **Repo** | `github.com/zanzaro-mirco/DarkDescent` (pubblico) |
| **Cartella locale** | `game_projects/DarkDescent` |
| **Engine** | Unity 6.3 LTS — **6000.3.24f1**, bloccata per tutto il progetto (ADR-001) |
| **Render pipeline** | URP 17.3.0 |
| **Package** | Input System 1.20.0 · AI Navigation 2.0.14 · Cinemachine 3.1.7 · Test Framework 1.6.0 · uGUI 2.0 con TextMeshPro |
| **Assembly** | `DarkDescent.Core` in `Assets/_Project/Scripts/Core/` (logica e dati) e `DarkDescent` in `Assets/_Project/Scripts/` (componenti) (ADR-003, ADR-032) · test in `DarkDescent.Tests.EditMode` e `DarkDescent.Tests.PlayMode` |
| **Milestone chiuse** | M0 — Fondamenta (23 set 2026) · M1 — "Mi muovo" (1 ott 2026, tag `m1`) · M2 — "Colpisco e muoio" (3 ott 2026, tag `m2`) · M2.5 — Pipeline automatica (3 ott 2026, tag `m2.5`) · M3 — "Un dungeon fatto a mano" (3 ott 2026, tag `m3`) · M4 — "Raccolgo roba" (4 ott 2026, tag `m4`) · M5 — "Loot casuale" (5 ott 2026, tag `m5`) · M6 — "Dungeon infinito" (5 ott 2026, tag `m6`) · M7 — "Le profondità" (8 ott 2026, tag `m7`) |
| **Milestone corrente** | **M8 — "Progressione e persistenza"** → `docs/milestones/M8_Progressione_e_persistenza.md` |
| **CI** | GitHub Actions + GameCI, account Unity Personal dedicato: test EditMode e PlayMode a ogni push e PR, build Windows sui tag `m*`/`v*`, build Web ad avvio manuale (ADR-011…013, ADR-019) |
| **ADR-004** | **Decisa il 1 ott 2026: opzione (a), solo asset CC0** (§ 1.4), scritta in `DECISIONS.md` con gli ADR-005…010 della M1 e della M2. Personaggi e animazioni da KayKit (Adventurers, Skeletons, Character Animations, rig `Rig_Medium`), suoni da Kenney, versi dei nemici da *80 CC0 creature SFX* di rubberduck, musica e rumori d'ambiente da OpenGameArt (dalla M7). Eccezione del 3 ott 2026: il font LiberationSans di TextMesh Pro (SIL OFL 1.1, con il testo della licenza nel repo); la sprite EmojiOne (CC BY 4.0) è tolta |
| **Documenti vivi** | questo piano (`docs/Piano_Sviluppo_ARPG.md`) · `DECISIONS.md` (ADR-001…051) · `CONVENTIONS.md` · `ICEBOX.md` · `CREDITS.md` · `CLAUDE.md` |

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
- Interfaccia e nomi degli oggetti in inglese (default) e italiano, predisposti per altre lingue (da M5, decisione di Mirco del 4 ott 2026)

**Fuori, esplicitamente:** multiplayer · quest system · dialoghi e NPC multipli · crafting · altre classi (con le loro armi: vedi "Dopo la v1.0" nel § 5) · set e oggetti unici complessi · cinematiche · gamepad · achievement · livelli di difficoltà. La localizzazione, fuori fino alla v2.11, è entrata nella v1.0 con la M5.

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

Le stime sono in **ore di sessione**: il tempo in cui Mirco lavora con Claude, che scrive il codice, mentre lui decide, prova nell'editor e in build, e rivede. Servono a dimensionare le milestone. Le ore reali non si registrano (decisione del 1 ott 2026).

**Ricalcolo del 3 ott 2026 (v2.7).** Le stime della v2.1 (240–350 h) presupponevano che il codice lo scrivesse Mirco. Con Claude che scrive il codice, M1 e M2 (stimate 32–50 h) si sono chiuse in quattro giorni di calendario. Il nuovo fattore non è uniforme:

- **circa un terzo** della stima originale per le milestone fatte soprattutto di codice e test (M2.5, M4, M5, M6, M8, M9);
- **circa metà** per quelle in cui pesano il giudizio di Mirco e il tempo passato a provare: atmosfera e luci (M3), nuovi nemici da tarare (M7), città e bilanciamento del gioco completo (M10), arte, audio e release (M11).

**Totale stimato:** circa 140–205 ore, comprese M1 e M2: è la somma delle stime della tabella del § 5. Fino alla v2.22 il totale si teneva a mano, aggiungendo le variazioni di ogni versione, e diceva 109–157 ore: non teneva più conto di alcune variazioni, tra cui la crescita della M8 nella v2.19. Alla chiusura della M7 (v2.23) si è ricalcolato dalla tabella, che da ora è l'unica fonte; la M7 stessa è salita a 18–25 h con i tre passi di correzione dopo le prove in build. Per le milestone ancora aperte, M8–M11 all'8 ott 2026, restano 46–69 ore: a 6–10 h a settimana sono 5–12 settimane di lavoro effettivo; con pause e settimane saltate, **2–3 mesi di calendario**.

**Punti di controllo, alla chiusura di M2 e di M5:** confronta le **settimane di calendario** dal punto di controllo precedente con la stima massima delle milestone chiuse nel frattempo, convertita a 6 h a settimana. Per M5 sono M2.5–M5, cioè 42 h, circa 7 settimane dalla chiusura della M2. Se il rapporto supera **1,5**, applica la prossima linea di taglio e ristima il resto. È una regola meccanica di proposito: la decisione di tagliare, presa da stanchi e in ritardo, non arriva mai. Le date di inizio e chiusura stanno già nella storia git e nei tag. Il punto di controllo di M2 (rapporto 0,19) è stato misurato con le stime della v2.1; quello di M5 (rapporto 0,04: 2 giorni contro circa 7 settimane) con quelle della v2.12. Il prossimo, se serve, si fissa alla chiusura della M8.

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

**Personaggio:** quattro attributi, confermati alla **M4** (ADR-020): il cavaliere parte da 30 / 20 / 10 / 25, e la vita è 50 + 2 × Vitalità.

| Attributo | Influenza |
|---|---|
| Forza | danno fisico, requisiti delle armi |
| Destrezza | probabilità di colpire, blocco |
| Magia | mana, danno degli incantesimi |
| Vitalità | punti vita |

**Formula del colpo**, confermata alla M4 (ADR-020): probabilità di colpire = 75 + Destrezza / 2 − Armatura del bersaglio, limitata tra il 5% e il 95%; il danno è un tiro tra minimo e massimo dell'arma, moltiplicato per (1 + Forza/100) e arrotondato all'intero, almeno 1 (dalla M5: con i decimali l'ultimo colpo poteva mostrare 0).

**Colpo critico**, confermato il 6 ott 2026 (v2.17, D13 della M7). Lo fa solo il cavaliere, e solo con un colpo a segno. La probabilità è 5% + Destrezza / 10, al massimo il 50%: con la Destrezza 20 del cavaliere è il 7%. Il danno è doppio. Il nemico colpito di critico emette un verso suo, diverso per archetipo, e il numero del danno è più grande e di un altro colore. I nemici non fanno critici, come in Diablo 1.

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
| Version control | **Git + LFS**, repo pubblico su GitHub | Quota LFS **verificata il 3 ott 2026** in *Settings → Billing and licensing*: 10 GB di spazio e 10 GB di banda al mese inclusi, entrambi a 0 GB usati. Il repo ha 10,4 MB in LFS (24 file): un checkout completo in CI costa circa 10 MB di banda, quindi la quota regge centinaia di esecuzioni al mese. La cache LFS in CI resta comunque, perché la quota cresce con gli asset (vedi M2.5) |
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

**Decisa il 3 ott 2026 (ADR-014):** `Core` più livelli additivi, che diventano la scena attiva. Niente `Bootstrap` per ora: arriverà con il menu.

### 4.4 Dati: definizioni con ID stabili — da M4

La regola è già nota: **ScriptableObject = definizione immutabile, classe C# = istanza runtime.** Si aggiunge un vincolo che conviene rispettare da subito: ogni definizione ha un **ID stabile** — una stringa generata una volta e mai più cambiata, non il nome dell'asset — e le istanze runtime riferiscono le definizioni attraverso quell'ID. Un registro (`ItemDatabase`) risolve ID → definizione.

Senza questo, in M8 si scopre che un riferimento a uno ScriptableObject non sopravvive a un salvataggio su file (`JsonUtility` scrive un identificativo che cambia a ogni avvio), e l'inventario va riscritto per poter salvare la partita.

**Applicata dalla M4 (ADR-022):** `ItemDefinition` con GUID, `ItemInstance` con il solo ID, `ItemDatabase` in `Data/`.

### 4.5 Assembly definition

Oggi c'è un solo `DarkDescent.asmdef` (ADR-003). Da M2 si aggiunge `DarkDescent.Tests.EditMode`, che referenzia `DarkDescent` e il Test Framework — possibile proprio perché il codice non sta in `Assembly-CSharp`. La divisione per area (Player, Combat, Items…) si valuta in M6, sulle dipendenze reali.

**Decisa alla M6 (ADR-032):** non per area, perché tra le aree ci sono cicli veri (Combat e Items, Levels e UI), ma a strati. `DarkDescent.Core` ha logica e dati senza MonoBehaviour, `DarkDescent` i componenti; un test controlla che `Core` resti senza componenti.

---

## 5. Roadmap

Ogni milestone si chiude con una **build eseguibile** e con il rituale del § 6. Se non parte in build, non è finita.

| # | Milestone | Risultato | Ore stimate (v2.7; M1 e M2 con le stime originali) | Stato |
|---|---|---|---|---|
| M0 | Fondamenta | repo e build vuota da clone pulito | — | ✅ 23 set |
| M1 | "Mi muovo" | cammini in una stanza | 12–20 | ✅ 1 ott |
| M2 | "Colpisco e muoio" | primo gameplay loop | 20–30 | ✅ 3 ott |
| M2.5 | Pipeline automatica | test e build in CI | 2–4 | ✅ 3 ott |
| M3 | "Un dungeon fatto a mano" | due livelli, atmosfera, build Web provata | 9–14 | ✅ 3 ott |
| M4 | "Raccolgo roba" | drop, inventario, equipaggiamento | 8–12 | ✅ 4 ott |
| M5 | "Loot casuale" | affissi e rarità, blocco, lingue | 9–12 | ✅ 5 ott |
| M6 | "Dungeon infinito" | cripta procedurale, pozioni, automappa | 16–19 | ✅ 5 ott |
| M7 | "Le profondità" | caverne, nuovi nemici, colpi critici, automappa delle caverne, musica e rumori d'ambiente | 18–25 | ✅ 8 ott |
| M8 | "Progressione e persistenza" | livelli, attributi, equipaggiamento completo, salvataggio, zoom e rotazione della visuale | 12–18 | |
| M9 | "Magia" | mana, incantesimi, nemico a distanza, vita del nemico combattuto | 9–12 | |
| M10 | "Città e loop completo" | il gioco è finibile, **prima build pubblica** | 10–14 | |
| M11 | "Look, feel e release" | arte, audio, shader, v1.0 | 15–25 | |

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

### M2 — "Colpisco e muoio" ✅

Chiusa il 3 ottobre 2026: 11 test EditMode e 36 PlayMode verdi, build Windows senza warning. Lezioni emerse:

- **Il combattimento è un componente solo, `MeleeAttack`, per player e nemici:** cambia chi sceglie il bersaglio. `SetTarget` chiede un colpo; per colpire di continuo lo si richiama. Il danno parte dopo il ritardo dell'arma, misurato campionando la traiettoria della lama nella clip, e al momento del colpo si ricontrolla tutto.
- **Presentazione e logica in un verso solo:** driver delle animazioni, lampo, suoni e numeri si iscrivono agli eventi di `Health`, `MeleeAttack` e `HitRecovery`. Il combattimento non sa niente di come viene mostrato.
- **L'ordine degli `Awake` tra oggetti non è garantito:** `Health` crea il suo model al primo accesso, e i componenti con `Bind` si iscrivono quando arrivano per secondi tra `Bind` e `OnEnable`.
- **Il null "finto" di Unity non si vede attraverso un'interfaccia:** `IDamageable.IsAlive()` fa il cast a `UnityEngine.Object` prima del confronto.
- **Quello che funziona nell'editor può sparire in build:** l'emissione non usata da nessun materiale viene tolta dallo shader, per questo il lampo scambia il materiale. E `timeScale` sopravvive al caricamento della scena.
- **Batchmode:** Unity cancella `Temp/` all'uscita; un `RequireComponent` aggiunge componenti già al caricamento del prefab; `AssetDatabase.ImportPackage` non finisce prima di `-quit`, serve `-importPackage`; gli script di editor vedono TextMeshPro, gli asmdef del gioco no.
- **Punto di controllo superato:** 1,5 settimane contro 8 stimate. Le stime in ore precedono la scelta di far scrivere il codice a Claude e da qui sovrastimano.

---

### M2.5 — Pipeline automatica ✅

Chiusa il 3 ottobre 2026: test e build Windows in CI, tag `m2.5` con la sua build. Lezioni emerse:

- **La licenza Personal in CI passa da email e password,** non più dal file `.ulf`. Un account Unity dedicato alla CI tiene fuori da GitHub la password dell'account principale (ADR-011).
- **Le action di GameCI scaricano l'ultima CLI anche quando sono fissate:** va fissata anche `cliVersion`. Il primo run è morto in 4 secondi per un'incompatibilità tra action e CLI (`--no-coverageEnabled`): la copertura si spegne con `GAME_CI_COVERAGE_ENABLED` (ADR-012).
- **Un gruppo di `concurrency` solo per tutto il repo,** senza annullare i run in corso: i posti della licenza sono pochi e un job ucciso a metà rischia di non restituirlo.
- **I PlayMode girano su Linux in Docker senza modifiche,** e le cache di LFS e `Library/` portano un run da circa 6 a circa 4 minuti; la build Windows riparte dalla `Library/` dei test.
- **Sui run rossi la CLI non crea il riepilogo dei risultati:** il nome del test fallito si legge nel log o nell'XML tra gli artifact.

---

### M3 — "Un dungeon fatto a mano" ✅

Chiusa il 3 ottobre 2026: 18 test EditMode e 58 PlayMode verdi, build Windows del tag `m3` dalla CI, build Web provata nel browser. Lezioni emerse:

- **La scena attiva decide luci e ambiente:** dopo il caricamento additivo il livello va reso attivo, altrimenti prende il cielo di `Core`. Il colore ambiente si scrive in gamma e diventa lineare: 0,12 vale 0,013, cioè nero pieno (ADR-014, ADR-016).
- **Una luce ripida illumina il cimiero e non il resto:** la luce del cavaliere sta alta e spostata verso la camera, e una seconda luce senza ombre illumina solo il rendering layer `Player`. Così il cavaliere si vede di fronte senza schiarire la stanza.
- **Mappe di testo e uno strumento che resta nel repo** (ADR-015): i livelli si rivedono in un diff, e alla M6 il generatore dovrà solo produrre la stessa griglia. Le misure del tileset si prendono all'import: in KayKit `wall_half` è un muro corto, non basso, e il muro basso è `barrier`.
- **Trigger e click hanno bisogni diversi:** un player mosso dall'agent fa scattare i trigger solo se c'è un `Rigidbody` cinematico, e il raggio del click ignora i trigger. Per questo la scala ha un'area cliccabile separata.
- **Gli shader si compilano al primo uso:** nell'editor la prima evidenziazione esce azzurra (segnaposto), in batch il primo fotogramma emissivo esce magenta; in build non succede. URP toglie `_EMISSION` da un materiale i cui flag di illuminazione globale non sono emissivi.
- **Unity non ha un'API pubblica per creare un `AudioMixer`:** lo script di editor è passato per la reflection.
- **Il Web regge il dungeon** (ADR-019) se usa la stessa qualità *PC*: il livello *Mobile*, assegnato di default al Web, toglieva ombre e Forward+. La pubblicazione su itch.io è passata alla M10 (ADR-018).

### M4 — "Raccolgo roba" ✅

Chiusa il 4 ottobre 2026: 62 test EditMode e 77 PlayMode verdi, build Windows della CI provata da Mirco sullo scenario della Definition of Done. Lezioni emerse:

- **La casualità si inietta prima di introdurla** (ADR-021): con una sorgente fissa i test che contano i colpi sono rimasti com'erano. Allentarli avrebbe tolto proprio quello che verificano.
- **Una statistica si toglie per sorgente, non per valore** (ADR-020): ogni modificatore ricorda l'oggetto che l'ha messo, e togliere l'oggetto riporta le statistiche esattamente a prima. È il test che il piano chiedeva, e regge anche per gli affissi della M5.
- **I modelli KayKit hanno il perno sull'impugnatura:** a terra vanno centrati sull'oggetto, altrimenti click, etichetta e punto d'arrivo cadono fuori dal modello. In mano ogni oggetto ha una posizione sua: lo scudo, agganciato all'osso della mano, stava dalla parte sbagliata e lasciava vedere la mano finché non è stato spostato verso l'esterno (riscontro di Mirco).
- **La classe generata dall'Input System va rigenerata prima di usare le azioni nuove:** con un errore di compilazione l'import non parte, quindi in batch servono due passaggi (trappola della M1).
- **Nella UI conta quando arriva l'evento:** il click di uGUI arriva al rilascio, quindi prendere un oggetto usa la pressione. Una finestra spenta non riceve l'uscita del cursore, e chi la chiude deve dimenticare cella e slot sotto il cursore. Un fondo trasparente acceso solo con un oggetto preso impedisce al click di arrivare al mondo.
- **Le icone si fanno dai modelli** (ADR-024): un oggetto nuovo ha l'icona con un click, ma ogni modello ha il suo verso, e lo scudo fotografato da davanti mostrava il retro.
- **`git mv` di un asset tiene il GUID ma non il nome interno:** `m_Name` va corretto, altrimenti l'Inspector e i log mostrano il nome vecchio.

### M5 — "Loot casuale" ✅

Chiusa il 5 ottobre 2026: 98 test EditMode e 89 PlayMode verdi, build Windows della CI provata da Mirco con `-seed 4711`. Lezioni emerse:

- **Le lingue vanno per prime** (ADR-025): ogni testo nuovo della milestone è nato con la sua chiave. I test che confrontavano frasi italiane sono passati all'inglese, con la preferenza della lingua cancellata prima di ogni scena; un test di copertura trova le chiavi che mancano prima che lo faccia un giocatore.
- **Una prova statistica va dimensionata sulla varianza:** su 10.000 estrazioni il 65% dei normali è uscito 63,9%, a 2,4 deviazioni standard, e la tolleranza dell'1% non reggeva. Con 100.000 la deviazione scende a 0,15 punti.
- **Il seme va legato a qualcosa che non cambia** (ADR-029): la cella del nemico, presa in `Awake`, rende il drop indipendente dall'ordine delle uccisioni. Il `Preview` del drop permette ai test di cercare il seme che fa cadere l'oggetto che serve, invece di scriverlo a mano.
- **Il danno con la virgola si vede solo in build** (ADR-031): nei test i tiri fissi davano valori tondi, e il "0" all'ultimo colpo l'ha trovato Mirco giocando.
- **In color space lineare la trasparenza della UI schiarisce molto:** uno sfondo al 30% sembra quasi pieno, e i colori delle celle vanno scelti scuri e controllati in foto.
- **Due componenti uguali nella scena rendono ambiguo `FindFirstObjectByType`:** con il secondo tooltip del confronto, il pannello espone i suoi due riferimenti ai test.
- **Le asserzioni sui testi colorati** cercano il testo tra i tag (`>Lama dello scheletro<`) o la costante del colore, non la frase intera.

### M6 — "Dungeon infinito" ✅

Chiusa il 5 ottobre 2026: 132 test EditMode e 103 PlayMode verdi, build Windows della CI provata da Mirco. Lezioni emerse:

- **Un refactor che sposta codice si verifica con una descrizione, non a occhio** (ADR-033): prima di portare il builder a runtime, uno script ha descritto le due scene a mano oggetto per oggetto; ricostruite, la descrizione era identica riga per riga.
- **Il bake dalle mesh renderizzate funziona nell'editor e viene vuoto in build** (ADR-035): i modelli senza Read/Write non si leggono dalla CPU. L'ha segnalato Unity al primo test del builder, prima di arrivare in build. Il bake dai collider costa 8–16 ms a livello.
- **I messaggi delle asserzioni si compongono anche quando passano:** il test sui 500 semi impiegava cinque secondi e mezzo per una mappa stampata a ogni controllo. Composto solo quando serve, mezzo secondo.
- **Un seme per dominio rende gratuite le novità casuali** (ADR-037): le pozioni hanno un tiro e un seme loro, e gli oggetti del seme 4711, già provati in build, sono rimasti identici.
- **Lo stato che deve sopravvivere a un cambio di scena si evita, non si salva** (ADR-036): "Ricomincia" non ricarica più `Core` ma solo il livello, e una richiamata a schermo nero rimette vita e inventario. Niente singleton né `DontDestroyOnLoad`.
- **Il grafo delle dipendenze si ricava dai tipi usati, non dagli `using`** (ADR-032): per cartelle era un unico ciclo, ma il codice di logica non usava mai componenti di scena. La divisione giusta era per strati, non per aree.
- **L'ordine nella gerarchia della HUD decide chi prende il click:** la cintura sotto il fondo trasparente dell'inventario perdeva i click con un oggetto sul cursore.
- **Una GIF con la camera che segue costa il triplo:** a ogni fotogramma cambia quasi tutto lo schermo. Quella della M6 è a 10 fotogrammi al secondo e 640 × 360, con il tragitto verso la scala accelerato.

### M7 — "Le profondità" ✅

Chiusa l'8 ottobre 2026: 181 test EditMode e 137 PlayMode verdi, tre build della CI provate da Mirco. Lezioni emerse:

- **Le prove in build hanno cambiato la milestone più dei test:** tre giri di correzioni (D14–D16 della scheda), tutti su cose che nessun test poteva chiedere: lo sciame che bloccava, la morte che toglieva gli oggetti, i passi troppo svelti, le armi tutte uguali. La stima è salita da 13–20 a 18–25 h per questo. Nelle prossime schede un passo di correzioni dopo la prima build va messo in conto da subito.
- **Una regola giusta da sola diventa una trappola alla frequenza sbagliata** (ADR-049): il blocco che interrompe il fendente (ADR-026) era innocuo contro uno scheletro; contro sei dello sciame scattava una volta al secondo e il cavaliere non colpiva più. Il conto si fa con il numero di nemici che ci saranno, non con uno.
- **Un test che dipende dal seme della partita passa per caso:** `CaveDescentTests` contava ancora i gruppi di sciame vecchi e in locale è passato, perché il seme cambia a ogni esecuzione; in CI è caduto. I test sui livelli generati o fissano il seme o controllano tutti i casi possibili.
- **L'IA a stati ha ripagato alla seconda modifica** (ADR-042): il ritorno a casa (ADR-050) è stato una classe nuova e quattro membri dell'interfaccia del corpo, provato prima in EditMode con il corpo finto.
- **Un tiro nuovo in un flusso condiviso sposta tutti quelli dopo** (trappola 12): i colpi leggeri del bruto aggiungono un tiro all'inizio del colpo, e i test con i tiri fissi hanno cambiato esito. Un dominio nuovo vuole un flusso suo, come per le pozioni (ADR-037).
- **Il suono va misurato sull'animazione, non stimato:** i passi a distanza fissa andavano più svelti dei piedi. Campionando l'altezza dei piedi nella clip si sono trovati i due appoggi (12% e 62% del ciclo), e i passi seguono l'animator.
- **Cambiare l'intonazione di un `AudioSource` cambia anche i suoni già partiti:** versi e passi hanno sorgenti loro, separate da quella dei colpi.
- **Quando un totale si tiene a mano, prima o poi diverge:** le stime del § 1.3 sommavano le variazioni versione per versione e erano rimaste indietro di 30–50 ore. Ora il totale si ricalcola dalla tabella.

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

### M2.5 — Pipeline automatica · 2–4 h

Una milestone piccola, ma con un posto preciso: nella v2.0 la CI stava "verso M3–M4", cioè da nessuna parte. Ora esistono i primi test, e la CI ha qualcosa da verificare.

**Contenuto:**

- GitHub Actions con **GameCI**, con la licenza Unity Personal attivata tramite i secrets del repo (email e password: dal 2026 per la Personal non esiste più il file `.ulf`)
- Job di **test EditMode e PlayMode** su ogni push e pull request verso `main` (i PlayMode sono la maggior parte dei test utili, D2 della scheda)
- Job di **build Windows** su tag (`m2`, `m3`… e `v*` per le release) o ad avvio manuale, con la build scaricabile come artifact
- **Cache** della cartella `Library/` e degli oggetti LFS. Il repo è pubblico, quindi i minuti di Actions sui runner standard non si pagano; ogni checkout che scarica file LFS però consuma la banda LFS gratuita, che resta il vincolo da tenere d'occhio anche se oggi è largo (10 GB al mese contro 10 MB per checkout, § 3). È per questo che la build gira sui tag e non a ogni push
- Badge di stato nel README

**Definition of Done:** un push con un test volutamente rotto fa diventare rosso il job — la prova che la CI intercetta davvero i problemi — e un tag produce una build scaricabile che parte.

**Per il CV:** CI/CD per un progetto Unity, una riga che da sola distingue un portfolio.

---

### M3 — "Un dungeon fatto a mano" · 9–14 h

**A schermo:** stanze collegate, costruite a mano con un tileset modulare grezzo, buie, illuminate da torce; il personaggio porta con sé un raggio di luce. In fondo, una scala scende al secondo livello, e vita e stato del personaggio lo seguono.

**Perché prima del procedurale:** un generatore va scritto sapendo cosa deve produrre. Costruisci a mano il livello che vorresti, misura i moduli, capisci quali pezzi servono: poi automatizzi.

**Contenuto:**

- Griglia di moduli a dimensione fissa (4×4 m come punto di partenza): pavimento, muro, angolo, porta, scala
- Due livelli fatti a mano, con la **struttura a scene additive** (§ 4.3) e un `LevelManager`
- Illuminazione URP con ombre; oscurità e raggio di luce del giocatore
- Cinemachine 3 al posto del `CameraFollow` scritto a mano (attenzione ai tutorial per la 2.x, § 3). Cosa porta in più: **Impulse**, cioè uno scuotimento della camera misurato sui colpi pesanti subiti e sulle morti, che completa il game feel della M2; una **zona morta** in cui il cavaliere si muove senza che la camera lo insegua; smorzamento configurabile senza codice. I test della M1 sulla camera vanno riportati sulla nuova camera
- **Audio posizionale.** Oggi i suoni sono 2D, perché con l'`AudioListener` sulla camera, a 20 m, l'attenuazione 3D renderebbe tutto quasi muto. Le modifiche: l'`AudioListener` passa sul giocatore, o su un oggetto che lo segue all'altezza della testa; le `AudioSource` dei personaggi diventano 3D (`spatialBlend` 1) con attenuazione logaritmica tra circa 4 e 25 m; un `AudioMixer` con i gruppi SFX, UI e, più avanti, Musica. In un dungeon buio, un nemico fuori schermo si deve sentire prima di vederlo
- La sandbox `Sandbox_Combat` resta la scena dei test: pavimento più grande, o sfondo in tinta con il pavimento, perché camera e GIF non mostrino il bordo nero oltre il piano
- Da valutare in un ADR: una build **Web** oltre a quella Windows. Si gioca nel browser senza scaricare nulla, e per chi valuta un portfolio fa differenza; richiede però il modulo *Web Build Support*, e URP sul web ha limiti che potrebbero pesare sugli shader di M11

**Decisione da prendere:** struttura delle scene (§ 4.3).

**Definition of Done:** in build, scendi dal livello 1 al livello 2 mantenendo vita e stato. La pubblicazione su itch.io, prevista qui fino alla v2.8, è spostata a M10.

**Per il CV:** level design modulare, gestione delle scene, illuminazione in URP, pubblicazione.

---

### M4 — "Raccolgo roba" · 8–12 h

**A schermo:** un nemico lascia cadere una spada; la raccogli, apri l'inventario a griglia, la metti nello slot dell'arma e il danno nel pannello del personaggio sale. La togli, e torna com'era.

**Contenuto:**

- `ItemDefinition` (ScriptableObject) con **ID stabile**, `ItemInstance` (classe semplice e serializzabile), registro `ItemDatabase` (§ 4.4)
- Oggetti a terra e raccolta
- Inventario a griglia con oggetti su più celle in **uGUI** (per l'HUD di un gioco è più pratico di UI Toolkit, che tornerà utile per i tool dell'editor): click-e-click come in Diablo 1 invece del drag & drop previsto fino alla v2.10 (ADR-023)
- Slot di equipaggiamento
- `StatSystem`: i quattro attributi del § 2, modificatori fissi e percentuali con un **ordine di applicazione definito e documentato**, formula del colpo del § 2
- Tooltip

**Decisione da prendere:** conferma di attributi e formula del colpo (§ 2).

**Test:** ordine dei modificatori (fissi prima dei percentuali); equipaggiare e poi togliere un oggetto riporta le statistiche esattamente ai valori di partenza.

**Definition of Done:** lo scenario descritto sopra, in build. Test verdi.

**Per il CV:** architettura data-driven, sistema di statistiche, UI complessa.

---

### M5 — "Loot casuale" · 9–12 h

**A schermo:** cade una "Savage Short Sword of the Griffin" (in italiano "Spada corta Feroce del Grifone"), rara, con tre affissi; il tooltip li mostra e li confronta con l'arma equipaggiata. Con lo scudo, una parte dei colpi nemici viene bloccata.

È il cuore del genere, e merita una milestone sua.

**Contenuto:** `AffixDefinition` per prefissi e suffissi · pool filtrati per *item level* e tipo di oggetto · rarità normale, magica e rara · generazione del nome, composto secondo la lingua · loot table per nemico · confronto nel tooltip · **blocco** con lo scudo, legato alla Destrezza (§ 2) · **lingue**: inglese di default e italiano, con una tabella delle stringhe a cui una lingua nuova aggiunge solo una colonna. Dettagli e decisioni: `docs/milestones/M5_Loot_casuale.md`.

**Concetti:** estrazione pesata; `System.Random` con **seed riproducibile** — "rigenera il drop 4711" invece di "riprova finché non ricapita".

**Test:** stesso seed, stesso oggetto, sempre; gli affissi rispettano l'item level; su 100.000 estrazioni le rarità restano entro una tolleranza dalle probabilità previste.

**Definition of Done:** in build, con il seed fissato lo stesso nemico lascia sempre lo stesso oggetto; senza seed, in poche partite compaiono normali, magici e rari (le probabilità le verifica un test su 100.000 estrazioni: otto scheletri per partita sono pochi). Lo scudo blocca. Il gioco parte in inglese e passa all'italiano. Test verdi.

**Punto di controllo** (§ 1.3).

**Per il CV:** sistemi procedurali, bilanciamento data-driven, test su logica casuale.

---

### M6 — "Dungeon infinito" · 16–19 h

**A schermo:** i livelli 1–4 sono cripte generate a ogni partita, sempre percorribili dall'ingresso all'uscita; nemici e casse aumentano con la profondità.

**Contenuto:**

- Generazione **BSP**, verifica della connettività (flood fill), istanziazione dei moduli di M3
- **NavMesh a runtime** con `NavMeshSurface.BuildNavMesh()`
- Nemici e casse distribuiti in base alla profondità
- Seed riproducibile per ogni livello
- **Tool dell'editor:** una finestra che genera e disegna il dungeon senza entrare in Play Mode. Circa 150 righe, ore di iterazione risparmiate, e "ho scritto tool per l'editor" pesa a un colloquio
- Aggiunti dopo la prova della build (v2.14): **pozioni di cura** con la cintura a 8 posti, **automappa** che si scopre esplorando (sovrapposta o nell'angolo), **ripartenza dall'ingresso del livello** dopo la morte con l'inventario di quando ci si era entrati, **tooltip** sulle statistiche del personaggio

**Decisioni da prendere:** NavMesh o A* su griglia, con dati reali alla mano (tempi di bake, comportamento nei corridoi stretti) — un A* scritto da te dà pieno controllo su costi e occupazione delle celle, e insegna il pathfinding sul serio; divisione dell'asmdef per area (§ 4.5).

**Test:** su 500 seed, ogni livello è connesso e nessuna stanza si sovrappone a un'altra.

**Definition of Done:** in build, attraversi di fila quattro livelli generati; con lo stesso seed, ottieni lo stesso dungeon. Test verdi.

**Per il CV:** generazione procedurale, algoritmi su grafi, tooling.

---

### M7 — "Le profondità" · 18–25 h

**A schermo:** dal livello 5 il dungeon cambia: caverne organiche e nemici nuovi — uno sciame veloce, un bruto che carica colpi telegrafati. Un'automappa mostra ciò che hai esplorato.

Nella v2.0 tutto questo stava dentro M6, che sarebbe diventata una milestone di sei settimane senza un risultato intermedio da mostrare. Divise, ognuna ha il suo.

**Contenuto:**

- Generazione **random walk** per le caverne
- Archetipi **sciame** e **bruto** (§ 2)
- **Reazione al colpo del cavaliere:** `HitRecovery` anche sul player, con una soglia tarata sui colpi del bruto. Il bruto deve poter interrompere un attacco, lo sciame no (ADR-010)
- **Colpi critici** del cavaliere (v2.17, § 2): danno doppio, numero più grande, e un verso proprio per ogni tipo di nemico colpito
- **Limite di voci audio** per lo sciame: con dieci nemici che colpiscono insieme, una sola clip d'impatto per frame e per tipo, e una priorità più bassa per i nemici lontani
- **Refactoring dell'IA** da `enum` e `switch` a classi di stato: ora i tipi sono tre e il bisogno è reale. Documentato in un ADR, è materiale da portfolio
- **Automappa** estesa alle caverne (l'automappa della cripta è arrivata alla M6, v2.14)
- **Musica e rumori d'ambiente** (anticipati dalla M11, v2.16): una traccia cupa e un fondo di vento e gocce per tipo di livello, e ogni tanto un verso lontano nel buio attorno al cavaliere
- Tabelle di spawn per profondità, con il giusto mix di archetipi

**Test:** gli stessi test di connettività di M6, sul nuovo algoritmo.

**Definition of Done:** in build, attraversi i livelli 5–8 con i nuovi nemici, e l'automappa si riempie mentre esplori. Test verdi.

**Per il CV:** design di IA, refactoring guidato da un bisogno reale, due algoritmi procedurali a confronto.

---

### M8 — "Progressione e persistenza" · 12–18 h

**A schermo:** uccidi, sali di livello, distribuisci punti negli attributi. Trovi un elmo, un anello, un amuleto e li indossi. Chiudi il gioco, lo riapri, e sei dove eri, con lo stesso equipaggiamento. La visuale si avvicina e si allontana con la rotella, e gira attorno al cavaliere.

**Contenuto:** esperienza e curva di livello · punti attributo · **equipaggiamento completo** (v2.19, richiesta di Mirco), descritto sotto · **salvataggio e caricamento** in JSON, con un **formato versionato fin dal primo salvataggio** · regole della morte: decise alla M7, non si perde niente (ADR-048) · **zoom e rotazione della visuale** (v2.18, richiesta di Mirco), descritti sotto.

**Equipaggiamento completo** (proposta, da confermare nella scheda della M8):

- **Slot nuovi** accanto ad arma e scudo: elmo, armatura, guanti, stivali, due anelli, amuleto. Diablo 1 ne ha meno (niente guanti e stivali): qui si tengono perché danno al pannello più varietà con poco lavoro. `EquipSlot` e l'`Equipment` di oggi crescono di valori, non di struttura.
- **Pezzi d'armatura** con l'Armatura e la Forza richiesta, come lo scudo; **anelli e amuleto** senza difesa, fatti dei loro affissi (vita, attributi, colpire, critico). Un affisso nuovo cambia il loot dei semi provati (trappola 10 della M7): i test che fissano i drop si aggiornano insieme.
- **Modelli e icone.** Elmi e pezzi d'armatura dai pacchetti KayKit già scaricati, se ci sono; per anelli e amuleti un pacchetto CC0 da scaricare con il permesso di Mirco, o icone piatte. Si decide nella scheda. L'armatura indossata non cambia il modello del cavaliere: resta per la M11.
- **Prima del salvataggio**, così il formato 1 contiene già tutti gli slot.
- Circa 4–6 h in più.

**Visuale: zoom e rotazione** (proposta, da confermare nella scheda della M8):

- **Zoom** con la rotella, tra circa il 60% e il 140% della distanza di oggi, con uno smorzamento breve. Si salva tra le preferenze, come la lingua. Il limite in avvicinamento tiene il cavaliere e i nemici vicini nell'inquadratura; quello in allontanamento non deve mostrare troppo oltre il raggio della sua luce.
- **Rotazione a scatti di 90°** con due tasti (Q ed E, o la rotella premuta), animata in circa 0,4 s. A scatti e non libera, perché i muri bassi (ADR-017) stanno sui lati verso la camera: a ogni scatto ogni lato del livello sceglie di nuovo tra muro alto e muro basso. Il builder mette su ogni lato i due pezzi, e uno solo è acceso; il cambio avviene a metà rotazione, su un evento della camera, senza controlli a ogni frame. Con la rotazione libera servirebbero muri sempre alti che si dissolvono tra la camera e il cavaliere: uno shader apposta, che il piano lascia alla M11.
- **Cosa deve seguire la camera.** Le torce e gli stendardi appesi a un lato che diventa basso. L'automappa, che è ruotata come la camera (ADR-038). La parte sepolta della scala, che oggi si nasconde dietro il muro nord. Il listener e la luce del cavaliere seguono già l'orientamento della camera.
- **Alla fine** un ADR che aggiorna ADR-017. Test: zoom nei limiti; dopo ogni scatto, tra la camera e il cavaliere nessun muro alto.

**Test:** salvare e ricaricare restituisce uno stato identico; un salvataggio nel formato 1 si carica con il codice del formato 2 (migrazione).

**Definition of Done:** lo scenario descritto sopra, in build. Test verdi.

**Per il CV:** serializzazione, persistenza versionata, migrazione dei dati.

---

### M9 — "Magia" · 9–12 h

**A schermo:** la sfera blu del mana si svuota mentre lanci dalla hotbar un proiettile, un incantesimo ad area e un potenziamento; un nemico tiene le distanze e ti bersaglia.

**Contenuto:** mana · 3 incantesimi (proiettile, area, potenziamento) · proiettili gestiti con **`UnityEngine.Pool.ObjectPool<T>`** (esiste già, non serve scriverne uno) · hotbar e tempi di ricarica · effetti con il sistema particellare · pergamene che insegnano gli incantesimi · archetipo **a distanza** (§ 2), che riusa i proiettili · **la vita del nemico che si sta combattendo** (v2.22, richiesta di Mirco), descritta sotto.

**La vita del nemico che si sta combattendo** (proposta, da confermare nella scheda della M9). Dalla M7 la barra in alto mostra il nome e la vita del nemico sotto il cursore (ADR-049): appena il cursore si sposta, sparisce, anche se il cavaliere sta ancora colpendo lo stesso nemico. La barra mostra il nemico sotto il cursore e, quando il cursore non ne ha uno, quello che il cavaliere sta attaccando o ha colpito per ultimo, per circa 3 secondi dopo l'ultimo colpo. Il cerchio rosso a terra resta sul nemico sotto il cursore, così distingue il prossimo bersaglio da quello di prima. Sta nella M9 perché lì il bersaglio cambia significato: un proiettile o un incantesimo ad area colpiscono nemici che il cursore non ha mai toccato, e la regola dell'"ultimo colpito" deve valere anche per loro. Si è scartata la barra sopra ogni nemico: nello sciame sarebbero dieci barre una sull'altra, e Diablo 1 non le ha. Resta un'opzione per dopo la v1.0 (sotto). Circa 1 h.

**Definition of Done:** in build, un combattimento misto di mischia e magia contro nemici a distanza; lanciando proiettili a raffica, il Profiler non mostra allocazioni a ogni frame.

**Per il CV:** object pooling, sistemi di abilità estendibili, profiling mirato.

---

### M10 — "Città e loop completo" · 10–14 h

**A schermo:** dal menu arrivi in città, compri dal mercante, scendi, risali a vendere, affronti il boss al livello 8, vedi la schermata di vittoria.

**Contenuto:** città hub · mercante · flusso di gioco completo (menu → città → dungeon → morte o vittoria) costruito sulla struttura a scene di M3 · menu delle opzioni, con la scelta della lingua che sostituisce il tasto provvisorio della M5 · ritorno in città dai livelli profondi · **boss** (§ 2), che lascia loot migliore dei nemici normali (almeno un oggetto raro; v2.22) · vittoria e riconoscimenti · **prima build pubblica su itch.io** (spostata da M3): pagina "in sviluppo" creata da Mirco, caricamento dalla CI con butler sui tag, eventuale canale Web se la prova di M3 ha retto.

**A fine M10 il gioco è finibile.** È il momento di farlo provare a cinque persone e di prendere appunti senza difendersi: il link di itch.io è il modo più semplice per dargliela.

**Definition of Done:** una partita completa, dall'avvio alla vittoria, in build, senza passare dall'editor; la build si scarica da itch.io e parte su un PC che non è quello di Mirco.

**Per il CV:** gestione dello stato di gioco, design di un boss.

---

### M11 — "Look, feel e release" · 15–25 h (percorso base)

**Contenuto (percorso base):** asset CC0 resi coerenti dallo **shader retro** in Shader Graph (dithering ordinato, palette limitata, eventuale riduzione della risoluzione con una Renderer Feature di URP) · post-processing (vignetta, grana, bloom misurato) · rifinitura dell'audio (musica, rumori d'ambiente, audio posizionale e mixer a gruppi ci sono già: M3 e M7) · **profiling** completo (Profiler, Frame Debugger, Memory Profiler) · pagina itch.io definitiva e release **v1.0**.

**Estensione opzionale — Blender.** Sostituire gli asset CC0 con modelli propri è **fuori** dalle 15–25 h: la sola modellazione, partendo da zero con Blender, vale facilmente 80–100 h, e lì il lavoro resta tutto di Mirco. Si valuta a M11 chiusa, con la v1.0 già pubblicata, e semmai diventa una v1.1. Non è un taglio: è il percorso base che non la prevede.

**Definition of Done:** v1.0 pubblicata, `CREDITS.md` completo, README con GIF e sezione sull'architettura.

**Per il CV:** Shader Graph, ottimizzazione, pipeline di release.

---

### Dopo la v1.0

Lavori decisi ma rimandati, da riprendere quando arriva il momento indicato. Non sono nella stima del § 1.3.

- **Nuove classi e le loro armi** (chiesto da Mirco il 4 ott 2026). Quando si aggiungono classi oltre al Guerriero: armi **a due mani** (spadone, ascia a due mani; occupano anche lo slot dello scudo), **bacchette** e **bastoni** per gli incantatori, **archi** e **balestre** con le frecce, e gli altri tipi che serviranno. Dalla M5 il tipo d'arma è un dato della definizione (`WeaponKind`): un tipo nuovo vuole un valore dell'enum, la regola sugli slot, le animazioni e gli affissi che lo ammettono, non una struttura nuova. I modelli di KayKit *Adventurers* ci sono già: `sword_2handed`, `axe_2handed`, `staff`, `wand`, `bow`, `crossbow_1handed`, `crossbow_2handed`.
- **Una punizione alla morte** (chiesta da Mirco il 7 ott 2026). Dalla M7 morire non costa niente: si torna all'ingresso del livello con tutto (ADR-048). Quando il gioco sarà finibile andrà pensata una perdita che dia peso alla morte senza far ricominciare da capo. Idee da valutare: una parte dell'oro (che arriva con il mercante della M10), l'oggetto in mano lasciato dove si è morti da andare a riprendere come in Diablo 1, una perdita di esperienza (M8), o i nemici uccisi che in parte rinascono. Se le prove della M10 con cinque persone dicono che la morte non pesa, si anticipa.
- **Barre della vita sopra i nemici** (chiesto da Mirco l'8 ott 2026, come alternativa alla barra in alto della M9). Una barra piccola sopra la testa, solo per i nemici feriti, così nello sciame non se ne vedono dieci intatte. È un'opzione del menu della M10, spenta di default. Le barre stanno in un solo canvas in world space, prese da un pool e non una per nemico, e si aggiornano su `HealthChanged` senza controlli a ogni frame.
- **Nemici speciali** (chiesto da Mirco l'8 ott 2026). Oltre al boss finale della M10, nemici più forti dei normali che lasciano loot migliore, come i *nemici unici* e i *campioni* di Diablo:
  - **Campioni:** un nemico normale reso più forte da un dato, non da un archetipo nuovo. Ha più vita e più danno, un nome colorato e una tinta del modello, è un po' più grande e ha uno o due modificatori presi da una lista (più veloce, colpi che rallentano, rigenera la vita, resiste al blocco). Arriva con una scorta di nemici normali.
  - **Unici:** un nemico con un nome suo e un modello suo, uno per tipo di livello, che lo spawn mette con una certa probabilità. Per esempio un capo degli scheletri nella cripta e un bruto più grande nelle caverne.
  - **Loot:** più oggetti, e una tabella delle rarità spostata verso magico e raro (una moltiplicazione dei pesi della `RarityTable`, da M5). Gli unici lasciano sempre almeno un oggetto raro.
  - Nel codice entra come un dato in più dell'`EnemyArchetype` e della `SpawnTable` (ADR-047): un moltiplicatore della vita, del danno e del loot, e la lista dei modificatori. La barra in alto mostra il nome colorato e i modificatori.
  - Un boss a metà discesa, alla fine della cripta, è una variante da decidere insieme. Se le prove della M10 dicono che la discesa è monotona, si anticipa.
- **Lingue nuove.** Dalla M5 una lingua è una colonna della tabella delle stringhe, più lo schema dei nomi e il genere delle basi. Una lingua con un altro alfabeto vuole anche un font di riserva per TextMesh Pro, con la sua licenza (ADR-004).

---

## 6. Rituale di chiusura milestone

Dieci minuti di disciplina che tengono il progetto leggibile anche dopo un mese di pausa.

1. Definition of Done verificata **in build**, non nell'editor
2. Test verdi (da M2) e CI verde (da M2.5)
3. GIF per il README
4. `git tag m<N>` e push del tag (da M2.5 produce anche la build)
5. Build aggiornata su itch.io (da M10)
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
| Pubblicazione e release | M10, M11 |

**Come presentarlo:** repo pubblico con un README curato (GIF, schema dell'architettura, link agli ADR), build giocabile su itch.io da M10, e 3–4 articoli tecnici sulle parti più interessanti: il generatore di dungeon, il sistema di affissi, la pipeline di CI, lo shader retro.

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
