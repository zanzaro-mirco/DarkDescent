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
| D13 | **La ronda più fitta** | Aggiunta dopo la prima ronda in CI (Mirco, 23 ott 2026: «troppo veloce», 4 minuti e mezzo di test su un obiettivo di 60). **Più casi**: 20000 mappe per tipo, 200 partite costruite, misure su 2000 partite per profondità e su 400 con 1024 equipaggiamenti. **Una partita da sola per ogni difficoltà**, con il mondo nuovo intero fino al suo re. **La partita lunga**: sei mondi di fila alla stessa difficoltà, senza tornare al menu; dopo ogni re, in città, conta oggetti in scena, oggetti di Unity per tipo, gestori degli eventi e memoria, ed è un errore una misura che cresce a ogni mondo. Fuori, per ora: lo stesso seme rifatto due volte, la partita mortale, l'inventario a caso, i salvataggi rovinati | Il tempo della notte c'era. La partita lunga cerca quello che nessun test vede: il gioco non chiama mai `UnloadUnusedAssets`, e un `+=` senza il suo `-=` non dà errori |

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
| 15.6b | La ronda più fitta (D13): più casi, una partita da sola per ogni difficoltà, la partita lunga | 1 |
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

### 15.2 Mappe su semi nuovi

**Com'è andata (23 ott 2026).** I controlli delle mappe dei test di ogni push sono in funzioni
comuni, che restituiscono il primo errore o null: `MapChecks` (raggiungibilità, ostacoli che non
chiudono passaggi, ingresso e scala, bordo), `CryptChecks` (stanze, scheletri e casse, torce,
ostacoli contro i muri), `CaveChecks` (forma, pilastri e diagonali, ingresso aperto, nemici della
tabella, cunicoli, candele, sala del re). `DungeonGeneratorTests`, `CaveGeneratorTests`,
`DungeonContentTests`, `CaveContentTests` e `BossTests` le chiamano sui semi di sempre, con
`MapChecks.Pass` che fa cadere il test al primo errore; `MapChecksTests` prova che i controlli
trovano gli errori su mappe scritte a mano. La ronda (`NightlyEditModeTests`) prova 2000 partite
nuove per la cripta e 2000 per le caverne, dalla prima all'ultima profondità a turno, con gli asset
veri delle impostazioni: tutti i controlli sul livello generato, poi raggiungibilità e ostacoli
sulla mappa che il gioco costruisce (nemici speciali, forzieri, tesoro del re, scala tolta all'8) e
la sala del re. Gli errori si raccolgono e il test cade alla fine con i primi dieci, ognuno con il
seme della partita e la profondità. Circa 8 s per le caverne e 9 s per la cripta.

La prima notte provata (seme 4711) ha trovato 27 livelli con errori, tutti all'8, che i test di
ogni push sulla forma non provavano mai (solo la profondità 5):

- **Un errore vero, grave: la profondità 8 senza il re** (partita 455840980255010401, 1 livello su
  500 all'8). La zona aperta più lontana a piedi era a due passi dall'ingresso in linea d'aria, in
  una caverna a U: `TryCarveBossRoom` rinunciava alla sala, e senza sala il popolatore non metteva
  il re. Senza re non si apre il portale della vittoria e il mondo non si finisce. Ora
  `TryPlaceBossRoom` prova le zone aperte dalla più lontana in giù e prende la prima abbastanza
  lontana; dove la prima andava, la sala è quella di prima.
- **Un errore vero, piccolo: pavimenti uniti solo in diagonale attorno a un angolo dell'anello**
  (2 su 500), che il NavMesh non collega. `CloseRingDiagonals` riempie il pavimento più lontano
  dalla sala, o l'altro se quello chiudeva la strada per la porta, e toglie quello che resta
  tagliato fuori.
- **Previsti dal progetto, corretti nei controlli**: il pavimento all'8 fuori dal 35–45% (l'anello
  taglia fuori parte della caverna, fino al 30%; la sala scavata nella roccia aggiunge fino al 4%,
  il 46% con il seme 98765): con la sala la fascia è 25–50%. La sala senza porta (4 su 500), che è
  il ripiego scritto in `TryCarveBossRoom` e che il gioco regge (`BossController` controlla la
  porta nulla): il controllo chiede solo che al re si arrivi, e le candele agli angoli della sala
  non devono stare contro la roccia.

I tre livelli sbagliati sono test con il seme fisso in `BossTests` (`Depth8_AlwaysHasTheKing`,
`BossRing_LeavesNoDiagonals`), visti cadere prima della correzione. Dopo: 420 EditMode e 242
PlayMode verdi, e la ronda pulita con sei semi della notte (4711, 20261023, 98765, 1, 31337,
20261024), 24.000 mappe. Le due correzioni cambiano il gioco: escono con la v1.3.1 (D1), con il tag a fine M15 (Mirco, 23 ott
2026), insieme a quello che troveranno le notti dopo.

### 15.3 Livelli costruiti

**Com'è andata (23 ott 2026).** `NightlyPlayModeTests.BuiltLevels_AreWhole` gioca 25 partite nuove
dalla profondità 1 all'8, caricate con il `LevelManager` come nel gioco (cripta fino al 4, caverne
dal 5), e svuota la memoria dei livelli a ogni caricamento, perché i ricordi sono per scena e
profondità e non per seme. Su ogni livello, dall'ingresso e a piedi sul NavMesh: la scala, ogni
cassa e forziere (il tesoro del re compreso), pozzi e santuari, il re; i nemici sul NavMesh; nessun
pezzo sopra un altro; il portale aperto all'ingresso e davanti a ogni cassa, pozzo e santuario,
girati verso il pezzo, su un punto che si raggiunge (tranne nella sala del re, dove non si apre);
i tempi, con l'errore sopra i 150 ms. I barili si provano come li rompe chi gioca: se da un punto
raggiungibile l'arma più corta (0,5 m dal bordo) arriva al barile, lo si rompe spegnendo il suo
ostacolo e si riprova con gli altri, così quello in fondo a un gruppo contro il muro si apre dopo
quelli davanti. Il riassunto dice quanti pezzi ha provato (circa 750 raggiunti, 1100 barili, 720
portali a notte): un controllo che non trova niente da provare passa sempre. 200 livelli in 81 s;
i tempi sul mio PC sono 26–30 ms di media e 36 al massimo, per ogni profondità.

Due falsi allarmi, corretti nel controllo e non nel gioco: i barili in fondo ai gruppi "fuori dal
NavMesh" (prima il controllo cercava il punto più vicino entro 2 m, ignorando che i vicini si
rompono) e un portale "irraggiungibile" che lo era: con un raggio di 0,3 m `SamplePosition` non
trovava il NavMesh sotto il punto, con 0,6 sì e nello stesso punto (partita 3019584370815887483,
profondità 4, riprodotta con un test temporaneo). Ora si cerca entro 1 m e si vuole il punto entro
30 cm. Nessun errore del gioco su cinque semi (4711, 20261023, 98765, 1, 31337), 1000 livelli.

### 15.4 La partita da sola

**Com'è andata (23 ott 2026).** `NightlyGameTests.Game_FromTownToTheKingAndBeyond` gioca una
partita nuova dal menu, nel posto 1 di una cartella temporanea, alla difficoltà che viene dal seme
della notte (`-difficulty`, che vale dal menu dal passo 14.7), con il seme della partita dato con
`UseLootSeed` e `RunSeed` appena arrivati in città. Il cavaliere è invulnerabile: ogni colpo gli
rimette la vita piena, e una morte si conta come errore. Scende dalla città all'8 per le scale; a
ogni livello uccide i nemici, rompe i barili, apre le casse, usa pozzi e santuari e raccoglie
quello che c'è a terra; alla profondità 3 torna in città con il portale e riparte da lì. A ogni
livello va al menu dalla pausa (che salva), legge il file, preme Continua e confronta lo stato
ripreso con il file campo per campo, con il tempo di gioco e il mana del file come in
`SaveLoadTests`. All'8 uccide il re, prende il portale della vittoria, chiude la schermata (che
torna al menu) e continua; all'altare sceglie la difficoltà dopo, o rifà il mondo all'ultima, e
scende due livelli del mondo nuovo, sempre salvando e riprendendo. Ogni passo ha 20 s per
arrivare. Circa 30 s a partita; tutta la ronda PlayMode 122 s.

Un falso allarme, nel confronto: un oggetto a terra torna a x = 99,54547119 da 99,54547882, il
rumore dei float nel passare la posizione (semi della notte 20261023 e 98765). Il confronto ora
vuole il testo uguale e i numeri entro un millimetro; quei due falsi allarmi dicono anche che il
confronto si accorge di una differenza. Nessun errore del gioco su sei notti (4711, 20261023,
98765, 1, 31337, 20261039), con le difficoltà 3, 4, 6, 7 e 8; con l'8 l'altare rifà il mondo.

### 15.5 Le misure di notte

**Com'è andata (23 ott 2026).** `LootCensus.Range` e `DifficultyCensus` prendono i semi da fuori
(i costruttori di sempre usano gli stessi semi di prima, `LootCensus.FixedSeeds` e i semi degli
equipaggiamenti); le fasce dei loro test sono in `CensusChecks`, comuni ai test di ogni push e alla
ronda, e il log scrive i numeri misurati accanto alle fasce. La ronda misura il loot su 250
partite nuove per profondità (2000 livelli: 5,0 oggetti a livello, rari 16,7%, magici 30,1%,
sempre dentro) e le difficoltà con l'esperienza di 100 partite per profondità e 256
equipaggiamenti, in circa 3 s.

La misura delle difficoltà ha trovato una cosa vera. Con 32 equipaggiamenti cadeva su punti
diversi ogni notte (rumore); con 256 restava una cosa sola, su tutte le notti: **l'Abisso I
comincia più facile della Tenebra**, dal 2 al 5,5% (bruto alla profondità 1, a volte il re),
perché dall'Abisso I l'equipaggiamento diventa raro. I test di ogni push non lo vedevano: i loro 8
equipaggiamenti fissi stanno per caso dalla parte buona. Alla profondità 1 Tenebra, Abisso I e II
sono quasi pari, e da una notte all'altra la misura oscilla del 3% (il livello del cavaliere, un
intero, viene dall'esperienza delle mappe della notte). Decisione di Mirco, dopo tre prove:

- la vita dei nemici dell'**Abisso I passa da ×13,5 a ×14,2** (+5%): da solo spostava l'inversione
  sull'Abisso II;
- le due regole d'ordine (ogni discesa e il re non più facili della difficoltà prima) hanno una
  **tolleranza del 5%** (`CensusChecks.StepTolerance`), a ogni push e di notte: da sola, a gioco
  invariato, due notti su sei cadevano ancora.

Insieme, ogni inversione resta entro il 3,4%: ronda verde su otto notti (4711, 20261023, 98765, 1,
31337, 20261039, 20261024, 777), 420 EditMode verdi. Il ritocco esce con la v1.3.1, ed è nelle
prove della build.

### 15.6 Il rapporto

**Com'è andata (23 ott 2026).** `NightlyReport` (in `Scripts/Core`, per EditMode e PlayMode) tiene
il percorso del rapporto, che arriva sulla riga di comando con `-nightly-report` (o
`DARKDESCENT_NIGHTLY_REPORT` in locale), e scrive le parti in Markdown: titolo, spunta o croce con
i numeri, i dettagli e i primi dieci errori in un blocco che si apre, con il testo com'è. Ogni
parte aggiunge la sua: mappe della cripta e delle caverne, misura del loot, misura delle difficoltà
(il valore più basso e il più alto di ogni misura, accanto alla fascia), livelli costruiti (con la
tabella dei tempi per profondità) e partita da sola. Le due parti PlayMode lo scrivono anche se un
passo si ferma a metà (`try`/`finally`), con il passo a cui si sono fermate. `nightly.yml` passa
`-nightly-report artifacts/rapporto.md` (relativo al progetto, nella cartella dei risultati), e il
riassunto della pagina del run aggiunge il file sotto seme, commit ed esito; l'artefatto
`rapporto-notturno` lo porta con i risultati XML. `NightlyReportTests` (4) prova il percorso, una
parte verde, una con errori e l'aggiunta al file. In locale: 424 EditMode verdi, la ronda intera
con il rapporto in un file solo.

In CI, avvio a mano (Nightly #2, seme 20261023): verde in 11 minuti, di cui circa 4 e mezzo di
test; il file arriva ai test con `-nightly-report` e finisce nell'artefatto `rapporto-notturno`.
Lanciata subito dopo un push, la ronda ha preso il posto in attesa nel gruppo `unity` e ha fatto
annullare i test di quel push, rilanciati quando la ronda è partita (verdi): l'ordine giusto è
push, test del push in corso, poi l'avvio a mano.

### 15.6b La ronda più fitta

**Com'è andata (23 ott 2026).** Dopo la prima ronda in CI, D13: più casi nelle stesse parti (20000
mappe per tipo, 200 partite costruite, loot su 2000 partite per profondità, difficoltà su 400 con
1024 equipaggiamenti), una partita da sola per ogni difficoltà (`ValueSource` sulla tabella, 9
partite, con il mondo nuovo intero fino al suo re) e la partita lunga
(`NightlyGameTests.LongGame_LeavesNothingBehind`): sei mondi di fila alla stessa difficoltà, rifatti
all'altare, senza tornare al menu dopo la prima vittoria; dopo ogni re, in città, conta gli oggetti
in scena, gli oggetti di Unity per tipo, i gestori attaccati agli eventi dei componenti del gioco e
la memoria, ed è un errore una misura che cresce a ogni mondo. Il rapporto ne scrive la tabella.

La partita lunga ha trovato una perdita vera, la prima notte (seme 20261024): **11 `NavMeshData` e
10–20 `Mesh` in più a ogni mondo**. `BuildNavMesh` crea un NavMesh nuovo a ogni livello e i
telegrafi del bruto e del re una mesh a testa in `Awake`; la scena che si scarica non distrugge gli
oggetti fatti a runtime, e il gioco non chiama mai `UnloadUnusedAssets` (lo fa solo il ritorno al
menu, che ricarica Core). Ora `BuiltLevel` distrugge il suo NavMesh e i telegrafi la loro mesh in
`OnDestroy`; `LeftBehindTests` (2, nella suite normale) scende tre livelli con il seme di quella
partita e crea e distrugge un bruto e il re, rossi prima della correzione. Dopo: NavMesh e mesh
fermi da un mondo all'altro, la memoria di Unity ferma a 263 MB. La correzione esce con la v1.3.1.

In locale la ronda intera dura circa 24 minuti di test (EditMode 3 min e 49 s, PlayMode 20 min:
livelli costruiti 647 s, partite da sole 46 s l'una, partita lunga 129 s); 424 EditMode e 244
PlayMode verdi.

In CI (Nightly #3, seme 20261023, dopo i Tests verdi del push): **35 minuti**, di cui circa 30 di
test. PlayMode 21 minuti (livelli costruiti 711 s, partite da sole 49 s l'una, partita lunga
131 s, niente che cresce); EditMode circa 9 (mappe della cripta 275 s, delle caverne 223 s: sul
runner le mappe vanno a metà della velocità del mio PC). Rossa per **una mappa delle caverne su
20000**: alla profondità 8 della partita 16557588204245213777 la sala del re resta senza porta (il
caso di riserva) e un suo angolo tocca la caverna solo in diagonale: nel caso di riserva l'anello
tornava pavimento e nessuno chiudeva le diagonali. Chiuderle come attorno all'anello, riempiendo,
non va: sulle mappe di altre notti staccava la sala (la cella fuori era l'unica strada) o mezza
caverna. Senza un anello da tenere chiuso si può **scavare**: `DigDiagonals` apre la roccia più
lontana dalla sala tra i due pavimenti, che non stacca niente. Sei notti di caverne in locale
(120000 mappe) hanno dato altri due semi dello stesso errore, già presente prima (8230148923540967550,
7000665500740811585), e un controllo troppo stretto: con la porta, l'anello lascia il **24,85%** di
pavimento (9793415644043241166), e il minimo con la sala del re scende dal 25 al **22%**
(`CaveChecks.LeastBossFloor`). I quattro semi sono casi di `BossTests.BossRing_LeavesNoDiagonals`,
che ora controlla anche la forma; 429 EditMode verdi.

La notte 31337 ha dato un altro controllo troppo stretto, alla profondità 7: la scala nella zona
aperta più lontana, a 22 passi, e un corridoio lungo in fondo alla caverna, a 46. La regola voleva
la scala ad almeno metà del punto più lontano; ora ad almeno il **45%** (`MapChecks.LeastStairsShare`,
decisione di Mirco), e il seme è `CaveGeneratorTests.Stairs_BeforeALongCorridor_AreFarEnough`. Mappe
delle caverne di notte verdi su sei notti (20261023, 4711, 98765, 20261024, 31337, 1); 430 EditMode
verdi. Da qui la ronda gira solo di notte (Mirco): le correzioni si provano in locale e l'esito in
CI si legge la mattina dopo.

La prima notte pianificata (24 ott 2026, seme 20261024, su `cd86fdb`): **verde in 32 minuti e
mezzo**, tutte le parti senza errori (mappe 208 + 172 s, livelli costruiti 684 s, nove partite da
sole, partita lunga 130 s con niente che cresce). Il cron dell'1 UTC è partito alle **5:35 UTC**,
quattro ore e mezza dopo: il ritardo delle pianificazioni di GitHub nelle ore piene, che la scheda
dava in decine di minuti, può essere di ore. Alle 7:35 italiane la ronda tiene il gruppo `unity`
fino alle 8:10: un push del mattino aspetta.

### 15.7 L'issue sui fallimenti

**Com'è andata (24 ott 2026).** `.github/scripts/ronda-issue.sh` scrive l'issue: il titolo della D9
con la data e il numero di errori, sommati dai blocchi del rapporto ("Ronda del 24 ott 2026: 13
errori"; "non è arrivata alla fine" se il rapporto non c'è o non ha errori), e nel corpo il link al
run, il seme, il commit, come rifarla in locale e **solo le parti rosse** del rapporto. Se un'issue
`ronda` è aperta la commenta; l'etichetta si crea la prima volta (`--force`). Il passo `Issue` di
`nightly.yml` parte quando un passo qualsiasi fallisce, i test o prima (la licenza, LFS), con il
permesso `issues: write`. Lo script si prova a secco con `DRY_RUN=1`: rapporto con due parti rosse,
verde e mancante.

Con la ronda solo di notte, la prova è un avvio a mano con **`issue_test`** (decisione di Mirco):
niente LFS, cache e Unity, un rapporto con una parte rossa finta, poi il passo `Issue`. Il primo
avvio ha aperto [l'issue #2](https://github.com/zanzaro-mirco/DarkDescent/issues/2), il secondo
(15 s) l'ha commentata; chiusa a mano. Lo script con `set -o pipefail` cadeva su un rapporto senza
errori (un `grep` che non trova niente): ora il conto è un `awk` solo.

### 15.8 Chiusura

**Com'è andata (24 ott 2026).** ADR-106 (la ronda), ADR-107 (l'Abisso I e la tolleranza), ADR-108
(chi crea un oggetto di Unity lo distrugge) e ADR-109 (l'issue); le lezioni nel piano (v2.61); nel
README la ronda nella sezione della CI e la v1.3.1 nello stato; `bundleVersion` 1.3.1. Il tag
`v1.3.1` porta le correzioni trovate dalla ronda: il re all'8, le diagonali attorno alla sua sala,
l'Abisso I, NavMesh e mesh distrutti.

**La notte del 25 ott 2026**, prima del tag: la ronda pianificata è caduta con 6 errori e ha aperto da
sola [l'issue #3](https://github.com/zanzaro-mirco/DarkDescent/issues/3), il passo 15.7 al lavoro.
Rifatti in locale con `-nightly-seed 20261025`, tre errori del gioco e uno della ronda:

- **Il tesoro del re chiudeva un cunicolo.** Nella sala senza porta va al centro della parete sud,
  e lì l'anello aperto può lasciare l'unica entrata di un cunicolo. Ora sta dove dietro c'è la roccia:
  prima la parete di fronte alla porta, poi le altre (`PlaceKingsTreasure`).
- **Una cassa chiusa dalla scenografia.** Roccia a nord e a ovest, una candela a est e, messo dopo,
  un oggetto di scena sul davanti: il controllo del cammino non lo vedeva, la cassa stessa è un
  ostacolo. Ora `IsConnected`, nella cripta e nelle caverne, vuole raggiungibile anche il davanti di
  ogni cassa, e i controlli delle mappe lo provano (`ObstaclesLeavePath`). Un unico su 600 nei test
  resta senza forziere in più: la soglia scende al 97,5%.
- **La ripresa spostava gli oggetti a terra.** Rimessi con `SamplePosition` mentre il ritaglio dei
  barili rotti è ancora nel NavMesh, quelli caduti dove stava un barile finivano 70 cm più in là.
  Ora il ricordo li rimette dove stavano (`GroundItem.Restore`): sul NavMesh c'erano già.
- **Il barile che "non si raggiungeva" si raggiungeva.** Dove un ostacolo ha ritagliato il NavMesh,
  `SamplePosition` con 25 cm non lo trova nemmeno sotto il sondaggio; con 60 cm sì, nel punto esatto.
  Il controllo dei barili della ronda cerca a 60 cm.

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
- **Il gioco non scarica gli asset.** Le scene dei livelli si scaricano, ma quello che il codice
  crea con `new` (mesh, NavMesh, materiali) resta finché qualcuno non lo distrugge: chi lo crea lo
  distrugge in `OnDestroy`. La partita lunga lo controlla ogni notte.
- **I semi fissi restano fissi.** Le funzioni comuni prendono i semi da fuori: i test di ogni push
  continuano a provare 1–500, la notte i suoi.
- **Il permesso delle issue.** Senza `issues: write` il token non le apre; l'etichetta `ronda` va
  creata la prima volta.
- **Un'issue da un errore voluto.** La prova del passo 15.7 apre un'issue vera sul repo pubblico:
  si fa una volta, con l'ok di Mirco, e si chiude subito.

## Test

- EditMode `Nightly`: mappe su semi nuovi, misure.
- PlayMode `Nightly`: livelli costruiti, una partita da sola per ogni difficoltà con i salvataggi, la
  partita lunga.
- Nella suite normale: le funzioni comuni dei controlli (sui semi di sempre) e la scrittura del
  rapporto.

---

## Checklist di chiusura

- [x] Decisioni D1–D13 confermate
- [x] Categoria `Nightly` esclusa dai test di ogni push, verificato in CI
- [x] `nightly.yml` pianificato e provato a mano
- [x] Mappe (passo 15.2), livelli costruiti e tempi (15.3), partita da sola con i salvataggi (15.4), misure (15.5)
- [x] Rapporto nella pagina del run e artefatto
- [x] La ronda più fitta e la partita lunga (passo 15.6b)
- [x] Issue sui fallimenti provata una volta (#2, aperta, commentata e chiusa)
- [x] Prima notte pianificata verde (24 ott 2026)
- [x] ADR, lezioni nel piano, README
- [x] `bundleVersion` 1.3.1 e tag `v1.3.1` dopo i Tests verdi (le correzioni trovate dalla ronda)
