# M15 — "La ronda notturna"

**Cosa deve succedere (Definition of Done):** ogni notte la CI prova il gioco dove i test di ogni push
non arrivano. Genera e costruisce **centinaia di livelli con semi nuovi**, controlla che ogni cosa
sia raggiungibile, **gioca una partita da sola** dalla città al re e oltre, rifà le **misure** del
loot e delle difficoltà e prende i **tempi** di costruzione dei livelli. Scrive un **rapporto** nella
pagina del run. Se qualcosa fallisce apre un'**issue** con il seme che rifà l'errore; nelle notti
verdi non apre niente. Ogni errore trovato diventa un test con il seme fisso nella suite normale,
poi si corregge.

**Tempo stimato:** 5–8 h (D12).
**Prerequisito:** M14 chiusa (tag `v1.3.0`, 23 ott 2026). Voce del piano: «Test notturni» (v2.58).

**Come si lavora:** il codice, i test e il workflow li faccio io; i test in batchmode a Unity chiuso.
Il primo run notturno lo lancio a mano, dopo il push. A Mirco restano le decisioni qui sotto e l'ok
alla prova dell'issue (passo 15.7), che ne apre una vera sul repo.

---

## Stato verificato il 23 ottobre 2026

| Cosa | Stato |
|---|---|
| Test | 411 EditMode e 242 PlayMode, tutti a ogni push su `main`, a ogni pull request e prima di ogni build (`tests.yml`, chiamato da `build.yml`). Un run dura circa 12 minuti. Nessun test usa una categoria |
| Semi fissi | Le mappe si provano su semi fissi: cripta e caverne sui semi 1–500 (`DungeonGeneratorTests`, `CaveGeneratorTests`: raggiungibilità, uscite, misure), il contenuto della cripta su 1–200 (`DungeonContentTests`). I livelli **costruiti**, con NavMesh, arredi, barili e portale, si provano su pochi semi scelti, uno per test |
| Errori trovati per caso | Tre errori veri, tutti nei livelli costruiti e non nelle mappe: il portale dipendente dal seme (passo 10.9), un arredo davanti a una cassa (passo 11.7), il portale chiuso in un angolo tra i barili (v1.2.0, corretto nella v1.2.1). Li ha trovati un seme diverso da quelli dei test |
| Tempi dei livelli | `DungeonLevel.Timings` dà generazione, costruzione e NavMesh di ogni livello; `Descent_ReachesTheEighthLevel` li scrive nel log ma non li controlla. La D4 della M6 vuole meno di 50 ms in build; l'ultima misura in build è del passo 11.6 (14–30 ms) |
| Partita da sola | `Diagnostics/Benchmark` (M11) fa girare il cavaliere da solo per città, cripta, caverne e re, ma solo in una build con `-benchmark`, e misura le prestazioni, non la correttezza. I test PlayMode scendono le otto profondità (`Descent_ReachesTheEighthLevel`) ma senza combattere, salvare o cambiare difficoltà |
| Misure | `LootCensus` (M13) e `DifficultyCensus` (M14) nell'editor, ognuna con il suo test EditMode su un numero fisso di livelli generati |
| Salvataggi | Fixture vere dei formati 1–5 in `Tests/EditMode/Fixtures`, migrate e lette a ogni push |
| CI | GameCI `unity-test-runner` v4.4.0 su `ubuntu-24.04`, licenza Personal, gruppo di concorrenza `unity` (un run alla volta in tutto il repo). Permessi del token: `contents: read`, `checks: write`. Il repo è pubblico: i minuti delle Actions non costano |

---

## Decisioni

Confermate da Mirco il 23 ottobre 2026, tutte come proposte ("confermo tutto, pusha e procedi"). La
D3 cambia una riga della voce del piano (v2.58): la suite normale di notte non si rifà mai.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Nome e versione** | **M15 "La ronda notturna"**. Non cambia il gioco: nessuna versione e nessun tag del gioco per la sola ronda. Se le prime notti trovano errori nel gioco, le correzioni escono come **v1.3.1** | È infrastruttura, come la M2.5. Una versione del gioco senza niente di nuovo da provare confonderebbe la lista delle build |
| D2 | **Quando** | Ogni notte alle **3 ora italiana d'estate** (cron `0 1 * * *`, in UTC: d'inverno sono le 2). Più l'**avvio a mano**, con un seme facoltativo per rifare una notte. Il workflow è `.github/workflows/nightly.yml` | Di notte non si scontra con i push nel gruppo `unity`. GitHub esegue le pianificazioni solo dal branch principale e a volte in ritardo: l'ora esatta non conta |
| D3 | **Solo i test lunghi** | La ronda esegue **solo** i test con la categoria **`Nightly`**; la suite normale non la rifà mai, perché ogni push la esegue già. I test di ogni push escludono `Nightly` (`-testCategory "!Nightly"`) | Rifare di notte i test di un commit già provato non trova niente. Così il tempo della notte va tutto in quello che i push non vedono |
| D4 | **Il seme della notte** | Un **seme di partenza** dalla data (UTC) del run, o quello dato a mano. Ogni test ne ricava i suoi semi con `SeedMixer`, in un dominio suo. Il rapporto scrive il seme della notte e, per ogni errore, il **seme del livello o della partita** e come rifarlo in locale (una variabile d'ambiente, `DARKDESCENT_NIGHTLY_SEED`, come `DARKDESCENT_ARGS`) | Con la data la stessa notte si rifà uguale; un errore senza il seme che lo rifà non serve |
| D5 | **Cosa prova** | (a) **Mappe**: gli stessi controlli dei test di ogni push (raggiungibilità, uscite, contenuto), su **2000 semi nuovi** per cripta e caverne; i controlli si spostano in funzioni comuni, così push e notte provano la stessa cosa. (b) **Livelli costruiti**: **25 partite × 8 profondità**, 200 livelli con NavMesh. Dall'ingresso devono essere raggiungibili sul NavMesh l'uscita, ogni cassa e forziere, pozzo, santuario, barile, il tesoro del re; i nemici stanno sul NavMesh; il portale trova posto; niente pezzi nello stesso punto. (c) **Partita da sola** (D6). (d) **Misure**: `LootCensus` e `DifficultyCensus` su livelli nuovi, con le fasce dei loro test. (e) **Salvataggi**: nella partita da sola, a ogni livello, salvataggio su file, lettura e ripresa, con lo stesso stato prima e dopo. (f) **Tempi** (D7) | I tre errori del passato erano tutti nei livelli costruiti: la parte (b) è il cuore. Le fixture dei formati vecchi girano già a ogni push |
| D6 | **La partita da sola** | Una per notte, a una **difficoltà scelta dal seme**. Il cavaliere è invulnerabile e si sposta con `Warp`, come nei test. Dalla città scende fino all'8 per le scale, a ogni livello apre le casse e uccide i nemici vicini, una volta torna in città con il portale e riparte, salva e riprende; all'8 uccide il re, prende il portale della vittoria, all'altare cambia difficoltà (mondo nuovo) e scende ancora due livelli. Fallisce su **ogni errore nel log** (il test framework lo fa già) e su ogni passo che non arriva entro il suo tempo | È il percorso che fa un giocatore, con tutti i sistemi insieme: quello che i test separati non vedono. Il benchmark della M11 fa qualcosa di simile, ma in build e senza controlli |
| D7 | **I tempi** | Generazione, costruzione e NavMesh di ogni livello della parte (b), con media e massimo per profondità nel rapporto, e la durata di ogni parte. Sul runner il limite non è quello della build: il test fallisce solo sopra **150 ms** per un livello. Il limite dei 50 ms resta da misurare in build («I tempi di costruzione dei livelli in build», nel piano) | Il runner della CI non è la macchina di un giocatore: un limite stretto darebbe falsi allarmi. Serve vedere la tendenza, notte dopo notte |
| D8 | **Il rapporto** | Un file Markdown che i test scrivono man mano (un percorso in una variabile d'ambiente): seme della notte, commit, per ogni parte quanti casi e quanti errori, gli errori con il seme e come rifarli, i tempi, i numeri delle misure accanto alle fasce. Va nella **pagina del run** (`$GITHUB_STEP_SUMMARY`) e in un artefatto `rapporto-notturno` con i risultati XML, tenuto 30 giorni | Si legge senza scaricare niente; l'artefatto resta per confrontare le notti |
| D9 | **L'issue** | **Solo quando qualcosa fallisce** (Mirco, 22 ott 2026): titolo "Ronda del 24 ott 2026: 2 errori", etichetta **`ronda`**, nel corpo il riassunto degli errori e il link al run. Se un'issue `ronda` è **ancora aperta**, la notte dopo **aggiunge un commento** invece di aprirne una nuova. Si chiude a mano quando l'errore è corretto. Il workflow ha il permesso `issues: write` | Un'issue al giorno per lo stesso errore diventerebbe rumore. GitHub manda la mail a chi segue il repo |
| D10 | **Durata e posto** | Un job solo, nel gruppo `unity`, con un tetto di **120 minuti**; obiettivo sotto i 60. Se una parte si allunga, si riducono i suoi casi (sono costanti in testa ai test) | Un job lungo tiene la licenza e blocca i push del mattino |
| D11 | **Fuori da questa milestone** | La **build notturna** e il **benchmark in build**: il runner Linux non ha una GPU e non esegue la build Windows, i tempi non direbbero niente. Le prove di **sensazione** (restano in `docs/Da_provare.md`). Click a caso sull'interfaccia | Ognuna chiede un'infrastruttura sua; la ronda resta sui controlli che danno un sì o un no |
| D12 | **Stima** | **5–8 h**. Se servirà tagliare: prima il secondo mondo della partita da sola, poi le misure di notte (restano quelle di ogni push) | La parte (b) e il rapporto con l'issue sono quello che serve davvero |

---

## Passi

| Passo | Cosa | Ore |
|---|---|---|
| 15.1 | La categoria `Nightly`, l'esclusione nei test di ogni push, `nightly.yml` con la pianificazione, l'avvio a mano e il seme; un primo test `Nightly` che passa; il primo run a mano | 1 |
| 15.2 | Mappe su semi nuovi: i controlli dei test di ogni push in funzioni comuni, la parte (a) | 0,5–1 |
| 15.3 | Livelli costruiti: raggiungibilità sul NavMesh, portale, sovrapposizioni, tempi (parti b e f) | 1,5–2 |
| 15.4 | La partita da sola, con i salvataggi (parti c ed e) | 1,5–2 |
| 15.5 | Le misure di notte (parte d) | 0,5 |
| 15.6 | Il rapporto: il file che i test scrivono, la pagina del run, l'artefatto | 0,5–1 |
| 15.7 | L'issue sui fallimenti, provata una volta con un errore voluto dall'avvio a mano (con l'ok di Mirco: apre un'issue vera, che poi si chiude) | 0,5 |
| 15.8 | Chiusura: ADR, lezioni nel piano, README (la sezione della CI) | 0,5 |

### 15.1 La categoria e il primo run

**Com'è andata (23 ott 2026).** `NightlyRun` (in `Scripts/Core/`) tiene il nome della categoria e
trova il seme della notte: prima `-nightly-seed` sulla riga di comando, poi
`DARKDESCENT_NIGHTLY_SEED`, poi la data UTC come aaaammgg. Nel container di GameCI le variabili
d'ambiente non arrivano, quindi `nightly.yml` lo passa come argomento. `SeedMixer.ForNightly(seme,
parte, indice)` dà a ogni caso il suo seme, in un dominio separato da quelli del gioco. `tests.yml`
aggiunge `-testCategory !Nightly`: in locale l'EditMode ha eseguito 414 test senza la ronda e
`Nightly` da sola un test; in CI il run 39931995937 ha eseguito 656 test (uno saltato come prima)
e il comando arrivato a Unity contiene `!Nightly` intatto. `nightly.yml` gira alle 01:00 UTC e a
mano con il seme come input; il primo run a mano (39932016183, seme 4711) ha eseguito solo i due
test `Nightly`, ha scritto "seme della notte 4711" nel log e il riassunto nella pagina del run.
NightlyRunTests (3) resta nella suite normale.

Il primo giro di `!Nightly` in locale ha fatto cadere tre test PlayMode che dipendevano dal seme
dell'orologio, cioè proprio quello che la ronda deve trovare:
`ExplosiveBarrel_HurtsEveryoneNearby` spostava il nemico lontano con `Warp` su un punto fuori dal
NavMesh, che fallisce senza dirlo (ora sceglie un punto del NavMesh tra 5 e 9 m e controlla il
risultato); `Firebolt_StopsOnWall` prendeva per muro il bordo del NavMesh ritagliato da un tronco
alto 1 m, sopra cui il dardo passa (ora il bordo deve avere davanti un collider sotto `Walls`);
`LeftLevel_StaysAsItWas` contava sull'oggetto sempre presente nella cassa, che dal passo 13.8 esce
solo il 35% delle volte (ora uccide e apre finché a terra ci sono due cose). Riprodotti con il seme
del run caduto, poi verdi: 414 EditMode e 242 PlayMode.

---

## Trappole note

- **La negazione delle categorie.** `-testCategory "!Nightly"` deve arrivare intatto a Unity
  attraverso i `customParameters` di GameCI, e il Test Framework 1.6 deve accettarlo. Se non va, la
  suite di ogni push diventa lunga un'ora: va verificato nel passo 15.1, in locale e in CI, prima di
  aggiungere test lunghi.
- **Il cron è in UTC** e GitHub lo esegue solo dal branch principale, a volte con decine di minuti di
  ritardo nelle ore piene. Dopo 60 giorni senza attività nel repo le pianificazioni si sospendono.
- **Test lunghi e timeout.** Un `UnityTest` ha un tempo massimo di partenza: i test della ronda
  dichiarano il loro `[Timeout]`.
- **Duecento livelli in un PlayMode.** Le scene si scaricano una dopo l'altra, ma gli asset no: ogni
  tanto `Resources.UnloadUnusedAssets`, o la memoria del runner cresce fino a cadere.
- **Un errore non deve fermare la notte.** Una parte prova tutti i suoi casi, raccoglie gli errori e
  fallisce alla fine con l'elenco; il rapporto si scrive anche quando un test cade
  (`if: always()`).
- **I semi fissi restano fissi.** Le funzioni comuni prendono i semi da fuori: i test di ogni push
  continuano a provare 1–500, la notte i suoi.
- **Il permesso delle issue.** Senza `issues: write` il token non le apre; l'etichetta `ronda` va
  creata la prima volta.
- **Un'issue da un errore voluto.** La prova del passo 15.7 apre un'issue vera sul repo pubblico:
  si fa una volta, con l'ok di Mirco, e si chiude subito.

## Test

- EditMode `Nightly`: mappe su semi nuovi, misure.
- PlayMode `Nightly`: livelli costruiti, partita da sola con i salvataggi.
- Nella suite normale: le funzioni comuni dei controlli (sui semi di sempre) e la scrittura del
  rapporto.

---

## Checklist di chiusura

- [x] Decisioni D1–D12 confermate
- [x] Categoria `Nightly` esclusa dai test di ogni push, verificato in CI
- [x] `nightly.yml` pianificato e provato a mano
- [ ] Mappe, livelli costruiti, partita da sola, misure, tempi
- [ ] Rapporto nella pagina del run e artefatto
- [ ] Issue sui fallimenti provata una volta
- [ ] Prima notte pianificata verde
- [ ] ADR, lezioni nel piano, README
