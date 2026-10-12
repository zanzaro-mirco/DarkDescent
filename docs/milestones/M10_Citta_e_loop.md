# M10 — "Città e loop completo"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI**, il gioco parte
da un **menu iniziale** con *Continua*, *Nuova partita* e **tre salvataggi**. Dalla **città** si
compra dal **mercante** con l'**oro** raccolto, si scende nel dungeon, si torna in città con una
**pergamena del ritorno** a vendere e ricomprare, e si riscende dove si era. Alla profondità 8 il
cavaliere affronta il **boss**, lo uccide e vede la **schermata di vittoria** con i
riconoscimenti. Una partita completa, dall'avvio alla vittoria, senza passare dall'editor. La
build della CI parte su un PC che non è quello di Mirco. Test verdi in CI. La pubblicazione su
itch.io è passata alla fine della M11 (D12).

**Tempo stimato:** 19–26 h (D13, piano v2.30).
**Prerequisito:** M9 chiusa (tag `m9`).

**Come si lavora:** come alla M9, il codice e i passaggi nell'editor li faccio io, in batchmode
a Unity chiuso. A Mirco restano le decisioni qui sotto, le prove in build e la revisione degli ADR.
Dopo la prima build da provare c'è un passo per le correzioni (lezione della M7, della M8 e della M9).

---

## Stato verificato l'11 ottobre 2026

| Cosa | Stato |
|---|---|
| Avvio | Il gioco parte dentro la partita: la scena `Core` carica il livello del salvataggio o la cripta alla profondità 1. Non c'è un menu, né una pausa, e `Esc` non è assegnato. `-newgame` ignora il salvataggio, `-seed` sceglie il seme |
| Scene | `Core` sempre caricata, `Level_Crypt` (profondità 1–4, BSP) e `Level_Caves` (5–8, random walk) additive, `Sandbox_Combat`, `Level_01`/`Level_02` fatti a mano dalla M3. Non c'è una città |
| Discesa | Alla profondità 8 la scala non c'è (`CaveSettings` non ha una scena dopo): il gioco non finisce |
| Oro e prezzi | Non ci sono: `ItemDefinition` non ha un valore. Il pacchetto KayKit *Dungeon* già scaricato ha `coin`, `coin_stack_small/medium/large` e `chest_gold` |
| Salvataggio | Formato 3 (ADR-068), un solo `save.json`. Migratore 1 → 2 → 3, file veri dei formati 1 e 2 congelati nei test |
| Morte | Si torna all'ingresso del livello con tutto (ADR-048) |
| Lingua | `F9` cambia lingua, la scelta resta tra le preferenze (provvisorio dalla M5) |
| Audio | Mixer `Main` con i gruppi musica, effetti e ambiente (M7): un volume per gruppo si può già esporre |
| Nemici | Scheletro, sciame, bruto (colpo telegrafato con il settore rosso), scheletro mago. Nessun boss |
| Modelli | KayKit *Adventurers* (Barbarian, Knight, Mage, Ranger, Rogue, Rogue_Hooded) e *Skeletons* (Warrior, Minion, Rogue, Mage, armi e scudi grandi) già scaricati; *Dungeon* con tavoli, scaffali, barili, botti, letti, bauli, candele, monete |
| CI | Test a ogni push, build Windows sui tag `m*`/`v*`, Web ad avvio manuale. Niente butler né itch.io |
| Test | 242 EditMode e 166 PlayMode verdi |

---

## Decisioni

Confermate da Mirco l'11 ottobre 2026: tutte come proposte, tranne la D12 (itch.io passa alla M11).

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Rifiniture chieste nella prova della M9** | Per prime, come descritte nel piano v2.28: **click destro** su un oggetto della borsa per **indossarlo**; **pulsante dorato** dei punti da spendere sopra la sfera della vita; **caratteristiche colorate** nel pannello (azzurro più alte della base, rosso più basse); **nomi degli oggetti a terra con `Alt`**, piccoli, impilati senza sovrapporsi, cliccabili | Sono piccole, si provano subito nella prima build, e il menu delle opzioni (D10) ne usa una |
| D2 | **Menu iniziale e tre salvataggi** | Come nel piano v2.28: *Continua* (l'ultimo giocato), *Nuova partita* (sceglie il posto, conferma se è occupato), *Carica* (tre posti con livello, profondità, città o dungeon, data; cancellabili con conferma), *Opzioni*, *Esci*. File `save_1.json`…`save_3.json`; il vecchio `save.json` diventa il posto 1. **`-newgame` e `-seed` saltano il menu** e giocano senza salvare, come oggi nell'editor | La riga di comando serve a provare e a rigiocare un seme: lì il menu è solo un passaggio in più, e scrivere in un posto rischierebbe di coprire una partita vera |
| D3 | **La città** | Un **accampamento sotterraneo all'ingresso della cripta**: una sala fatta a mano con la mappa di testo della M3, illuminata più calda della cripta, con tavoli, botti, scaffali, bauli e candele del pacchetto *Dungeon*. Dentro: il **mercante**, la **scala** che scende alla profondità 1 e il **portale** del ritorno (D6). Niente nemici, una musica sua. Si comincia lì | Il pacchetto ha arredi da taverna ma non esterni: una città all'aperto vorrebbe un pacchetto nuovo. Sotto terra resta coerente con la cripta, e la mappa di testo è già pronta (M3) |
| D4 | **Oro** | Cade da nemici (spesso, poco) e casse (sempre, di più), crescente con la profondità; a terra come una pila di monete con il nome "N oro", si raccoglie con un click come gli oggetti e **non occupa la borsa**. Il totale nell'inventario. Ogni oggetto ha un **prezzo** calcolato: valore base del tipo × rarità (normale 1, magico 3, raro 6) × (1 + livello dell'oggetto / 10). Il mercante **vende** al prezzo e **compra** al 25% | Come Diablo 1: l'oro a terra è il riscontro più frequente, e un prezzo calcolato evita di scriverne uno per ogni combinazione di affissi. Il 25% rende vendere utile ma non un modo per arricchirsi |
| D5 | **Il mercante** | Un personaggio KayKit *Adventurers* (il Mage o il Rogue incappucciato) dietro un tavolo. Click sopra: si apre la sua finestra accanto all'inventario. **Vende** pozioni di cura e di mana, pergamene del ritorno (D6) e **otto oggetti** del livello della profondità più bassa raggiunta, tirati dal seme della partita e cambiati a ogni ritorno in città. **Compra** tutto. Click su un suo oggetto per comprarlo, click destro su un oggetto della borsa per venderlo; il tooltip dice il prezzo | Un solo mercante, come nel piano. L'assortimento dal seme è ripetibile senza salvarlo |
| D6 | **Tornare in città** | Una **pergamena del ritorno** (consumabile, 1 × 2 celle come quelle degli incantesimi): letta con il click destro apre un **portale** davanti al cavaliere. Il portale porta in città; in città un portale riporta **all'ingresso della profondità** da cui si era partiti (il livello si rigenera dal seme, ADR-061). Se ne comincia con una; il mercante le vende a poco e cadono raramente | È il *Town Portal* di Diablo 1. Le scale in su vorrebbero rifare otto livelli a ritroso; un portale fisso a ogni livello toglierebbe la scelta di quando tornare |
| D7 | **Morte** | Resta come oggi: si torna all'ingresso del livello con tutto (ADR-048). La punizione alla morte resta tra i lavori dopo la v1.0, da valutare con le prove a cinque persone | Il piano la rimanda già; cambiarla adesso cambierebbe anche il bilanciamento dell'oro |
| D8 | **Il boss** | **Il Re delle ossa**: lo `Skeleton_Warrior` a scala 1,8 con l'ascia grande, una luce rossa e un nome suo. Alla profondità 8 le caverne hanno una **sala del boss** nel punto più lontano dall'ingresso; entrando, la sala si chiude e la barra in alto mostra la sua vita per tutto lo scontro. **Tre fasi** per vita: sopra il 60% mischia e il **colpo caricato** del bruto; tra il 60 e il 30% **evoca un gruppo dello sciame** ogni 12 s; sotto il 30% è più veloce e lancia un **anello di fuoco** telegrafato da un cerchio a terra. Vita circa 600, da tarare. Lascia **almeno un oggetto raro**, molto oro e una pergamena del ritorno | Il piano lo vuole "a fasi, colpi telegrafati, evoca uno sciame": tutto riusa pezzi che ci sono già (stati dell'IA, telegrafo del bruto, sciame, anello di fuoco). Il modello è già scaricato |
| D9 | **Vittoria** | Morto il boss, si apre un portale; attraversandolo, la **schermata di vittoria**: tempo di gioco, livello, nemici uccisi, morti, oro raccolto. Poi i **riconoscimenti** che scorrono (da `CREDITS.md`) e il ritorno al menu. Il salvataggio resta: con *Continua* si torna in città e si può riscendere | Chiude la partita senza cancellarla: chi vuole continuare a cercare oggetti può |
| D10 | **Pausa e opzioni** | **`Esc`** apre la pausa: *Riprendi*, *Opzioni*, *Salva ed esci al menu*; il gioco si ferma. **Opzioni** (dal menu e dalla pausa): **lingua** (sostituisce `F9`), **volume** generale, musica, effetti e ambiente, **nomi a terra sempre visibili** (D1). Si salvano tra le preferenze | Il piano chiede la lingua nelle opzioni; i volumi sono gratis con il mixer della M7. `Esc` oggi non fa niente: con una finestra aperta (inventario, pannello, mercante) la chiude, altrimenti apre la pausa |
| D11 | **Formato 4 del salvataggio** | Il salvataggio aggiunge l'**oro**, se si è **in città**, la profondità a cui riporta il portale, e i contatori della vittoria (tempo, uccisioni, morti, oro raccolto). `SaveMigrator` porta il 3 al 4 con oro 0 e contatori a zero. Prima si congela un file vero del formato 3 | Come per la M8 e la M9 (ADR-062): il test della migrazione usa un file vero |
| D12 | **Pubblicazione** | **Niente itch.io nella M10** (decisione di Mirco, 11 ott 2026): la prima pubblicazione passa alla **fine della M11**, con la v1.0. Per la prova a cinque persone e su un altro PC basta lo zip della build Windows della CI | Mirco non vuole pubblicare finché il gioco non è rifinito |
| D13 | **Stima** | **19–26 h** contro le 17–22 del piano, per il formato 4, la pausa e i passi di prova e correzione; senza itch.io (D12) un'ora in meno. Si tengono tutte e il piano si aggiorna (v2.30). Se servirà tagliare, la **linea 3** del piano (la città come sola schermata del mercante) toglie circa 2 h, e i nomi con `Alt` possono passare a dopo la v1.0 (circa 1,5 h) | Meglio deciderlo adesso che a metà milestone |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 10.0 | Rifiniture della prova della M9: click destro per indossare, pulsante dei punti, caratteristiche colorate, nomi con `Alt` (D1) | 3–4 |
| 10.1 | Oro in `Core`: drop, pile a terra, raccolta, prezzi degli oggetti (D4), con i test | 1,5–2 |
| 10.2 | Formato 4 del salvataggio e migrazione, con il file vero del formato 3 congelato prima (D11) | 0,5–1 |
| 10.3 | Menu iniziale, tre salvataggi, pausa e opzioni (D2, D10) | 3–4 |
| 10.4 | La città: sala a mano, arredi, luci, musica, scala e portale; si comincia lì (D3) | 2–3 |
| 10.5 | Il mercante: modello, finestra, comprare e vendere, assortimento dal seme (D5) | 2–3 |
| 10.6 | Pergamena del ritorno e portali (D6) | 1–1,5 |
| 10.7 | Il boss alla profondità 8: sala, tre fasi, barra, loot (D8) | 3–4 |
| 10.8 | Vittoria, statistiche e riconoscimenti (D9) | 1 |
| 10.9 | Build da provare e correzioni | 1–2 |
| 10.10 | Chiusura: prova su un altro PC, GIF, ADR, lezioni, tag `m10`, scheda della M11 | 0,5–1 |

## Passo 10.0 — Rifiniture della prova della M9

**Com'è andata (12 ott 2026).**

- **Click destro per indossare.** `Inventory.EquipFromGrid` mette l'oggetto della borsa nel suo
  slot (per un anello quello libero, o il primo) e rimette quello di prima nel posto lasciato
  libero, o nel primo libero. Prima di toccare qualcosa controlla che ci sia posto per l'oggetto
  che torna (`InventoryGrid.CanAutoPlace`), così un'ascia che non entra da nessuna parte lascia
  tutto com'era. Lampo e suono come equipaggiando con il click sinistro. Nel tooltip di un oggetto
  nella borsa, in fondo, "Click destro per indossare".
- **Pulsante dei punti.** `PointsButton`, un riquadro scuro con il bordo e il "+N" dorati sopra la
  sfera della vita. Ascolta la crescita e il nuovo evento `OpenChanged` del pannello del
  personaggio: si nasconde a pannello aperto e a punti finiti; un click apre il pannello.
- **Caratteristiche colorate.** `CharacterPanel.TryGetLine` dà valore e base di ogni riga che si
  colora: i quattro attributi (con e senza modificatori), Vita, Armatura, critico e Mana rifatti con
  i valori di base. Azzurro (il colore degli affissi) se più alto, rosso se più basso; il tooltip
  aggiunge in grigio "Base 30, oggetti ed effetti +5". Danno, colpire e blocco restano bianchi.
- **Nomi con `Alt`.** Un'azione nuova, `ShowLabels`, su tutti e due gli `Alt`; il reader annuncia
  `LabelsHeldChanged`. `GroundLabels`, primo figlio dell'HUD (sotto finestre e tooltip), mentre
  `Alt` è premuto cerca gli oggetti a terra con un `OverlapSphereNonAlloc` sul livello Interactable,
  tiene quelli sullo schermo, li ordina dal più vicino e mette ogni nome sopra il suo oggetto,
  salendo di una riga finché non tocca un nome già messo (al massimo sei). Testo al 72% del nome
  sotto il cursore, fondo scuro, colore della rarità. Etichette da un pool, legate all'oggetto
  finché resta in vista: il testo si scrive solo quando un oggetto compare. Un click su un nome
  manda il cavaliere a raccoglierlo (`PlayerController.Use`). `SetAlwaysVisible` è pronto per
  l'opzione della D10.
- Il primo screenshot metteva tutti i nomi nello stesso punto: la camera della UI era letta
  all'avvio, e il test degli screenshot cambia il canvas dopo. Ora si legge a ogni giro.

*Test.* `EquipFromBagTests` (5, EditMode): elmo scambiato, Forza che non basta, anelli nel posto
libero e poi al posto del primo, niente posto per l'oggetto che torna, la riga del tooltip.
`RefinementTests` (3, PlayMode): il pulsante segue punti e pannello; colori e nota del tooltip con
un modificatore positivo e uno negativo; sei oggetti nello stesso punto mostrano sei nomi che non
si coprono, più piccoli di quello sotto il cursore, e un click su un nome raccoglie l'oggetto.
`EquipmentPlayModeTests`: la vita alzata dallo scudo è azzurra. 247 EditMode e 169 PlayMode verdi.

## Passo 10.1 — Oro e prezzi

**Com'è andata (12 ott 2026).**

- **In `Core`.** `GoldDefinition` è la pila d'oro: una definizione che non sta nel database degli
  oggetti, con tre modelli per la quantità (piccola sotto 20, media, grande da 60). Ogni
  `LootTable` ha pila, probabilità, intervallo alla profondità 1 e crescita a livello (+35%):
  scheletro 50% 3–10, sciame 30% 1–5, bruto 80% 8–20, mago 60% 5–12, cassa sempre 15–35.
  `LootRoller.RollGold` tira con un seme suo (`SeedMixer.ForGold`, dominio "GOLD"), quindi oggetto,
  pozione e pergamena dei semi provati non cambiano. `Inventory` ha il portafoglio: `Gold`,
  `AddGold`, `TrySpendGold`, `RestoreGold` e l'evento `GoldChanged`.
- **Prezzi.** `ItemDefinition` ha un valore base (dal pugnale a 20 alla corazza scura a 200, le
  pozioni 15, le pergamene 300); `ItemPricing.Price` fa valore × rarità (1, 3, 6) × (1 + livello /
  10), e `SellPrice` un quarto, almeno 1. Li userà il mercante (10.5).
- **A terra.** La pila è un `GroundItem` con la quantità: `SpawnGold` la fa cadere un passo davanti
  all'oggetto, da nemici e casse. Ha il nome "18 oro" in oro, una luce dello stesso colore e un
  tintinnio di monete quando tocca terra (tre suoni sintetizzati, `coins_*.wav`). Un click la
  raccoglie nel portafoglio, sempre, senza occupare la borsa; con `Alt` il nome si vede come gli
  altri. Modelli `coin_stack_small/medium/large` dal pacchetto KayKit *Dungeon*, con il materiale
  del dungeon.
- **Nell'inventario.** "Oro: 18" in oro sopra la griglia, a destra; si aggiorna con
  `GoldChanged` e con la lingua. Il primo posto, sotto la griglia, finiva sul bordo della finestra.
- I test che contavano gli oggetti caduti da un nemico ora saltano le pile d'oro.
- **Suoni di puadlung (ADR-070).** Dopo il passo, su scelta di Mirco, quattro suoni gratuiti di
  *Dark Fantasy Audio – Free*, con un'eccezione all'ADR-004 (non si possono ridistribuire da soli):
  le monete alla raccolta dell'oro (in `PlayerInventory.PickUpGold`), il sorso di ogni pozione, anche
  di mana, che prima non suonava (il vetro della cura è tolto, per non sentirne due), l'apertura della
  cassa, e il colpo sulle ossa aggiunto ai colpi dei quattro scheletri.

*Test.* `GoldTests` (5, EditMode): probabilità, intervallo e crescita con la profondità; seme
dell'oro separato; prezzi per rarità e livello e vendita a un quarto; ogni oggetto ha un valore; il
portafoglio. `GoldPlayModeTests` (1): aperta una cassa, la pila prevista dal seme ha il nome giusto
e raccolta finisce nel portafoglio e nell'inventario, non nella borsa. 252 EditMode e 170 PlayMode
verdi.

## Passo 10.2 — Formato 4 del salvataggio

**Com'è andata (12 ott 2026).**

- **Prima il file vero.** Con il codice ancora del formato 3, uno script dell'editor ha scritto un
  salvataggio nelle caverne alla profondità 6: livello 9, ascia magica Affilata, elmo, anello
  Arcano, pergamena e pugnale nello zaino, pozione rossa e blu nella cintura, l'Anello di fuoco
  imparato e scelto, mana 23,5 e una mappa scoperta. È `Tests/EditMode/Fixtures/save_v3.json`,
  committato da solo prima di toccare il formato.
- **Formato 4 (D11).** `SaveData` aggiunge l'oro, se si è in città, la profondità a cui riporta il
  portale (0: nessuna) e i contatori della partita: tempo di gioco, uccisioni, morti, oro raccolto e
  boss sconfitto. `SaveMigrator` porta il 3 al 4 con tutto a zero, mettendo i valori a mano come al
  9.7. Città e portale si riempiono ai passi 10.4 e 10.6; il boss al 10.7.
- **I contatori.** `RunStats` in `Core`, tenuto dal CompositionRoot: il tempo cresce nel suo
  `Update` (con il tempo scalato, così la pausa lo fermerà), le uccisioni con l'esperienza di ogni
  nemico, le morti con `Died` del cavaliere, l'oro con il nuovo evento `GoldAdded` dell'inventario
  (che non scatta spendendo né caricando). `SaveGame` li cattura e li rimette con l'oro.
- Il primo giro di test ne ha fatti cadere 104: la disiscrizione in `OnDisable` chiedeva l'inventario
  a un cavaliere già distrutto allo scarico della scena. Ora l'inventario si prende in `Awake`.

*Test.* `SaveMigrationTests`: il file vero del formato 3 si carica con il codice del formato 4
(niente oro, nel dungeon, contatori a zero; seme, profondità, incantesimi, mana, mappa e oggetti
com'erano); il formato 4 va e torna dal file; i file dei formati 1 e 2 arrivano al 4.
`SaveLoadTests`: oro e contatori passano dal salvataggio. 254 EditMode e 170 PlayMode verdi.

## Passo 10.3 — Menu iniziale, tre salvataggi, pausa e opzioni

**Com'è andata (12 ott 2026).**

- **Il menu sta nella scena Core**, in una tela sua sopra l'HUD, e non in una scena a parte:
  così il posto scelto non deve passare da una scena all'altra senza un singleton. Se il menu
  serve, il CompositionRoot ferma il primo caricamento (`LevelManager.HoldStart`), spegne i
  tasti del cavaliere e mostra il menu; scelta la partita, decide seme e livello, rimette il
  salvataggio sul cavaliere prima che il livello si colleghi e chiama `Begin`.
- **Quando c'è il menu.** Nelle build sempre; nell'editor solo con `DARKDESCENT_MENU=1`, che usano
  i test. `-newgame` e `-seed` lo saltano e giocano senza salvare (D2); un file scelto con
  `DARKDESCENT_SAVE_FILE` si riprende direttamente, come prima. `SaveGame` ora è acceso solo con un
  file: il posto del menu, quello di un test o la variabile.
- **I tre posti.** `SaveSlots` in `Core`: `save_1.json`…`save_3.json` nella cartella dei dati
  (`DARKDESCENT_SAVE_DIR` per i test), il riepilogo letto dal salvataggio, l'ultimo giocato dalla
  data del file, la cancellazione. Il `save.json` di prima diventa il posto 1 con una rinomina,
  solo se il posto 1 è libero. Nel menu ogni posto dice livello, luogo e data ("Posto 2 – livello 9
  · Caverne – Livello 6 · …"); sovrascrivere o cancellare chiede conferma (`ConfirmDialog`).
- **La pausa con `Esc`** (azione nuova `Pause`): prima chiude inventario e pannello del
  personaggio, poi apre la pausa. Il tempo si ferma (`timeScale` 0) e anche i suoni; un hit stop in
  corso finisce prima (`HitStop.Cancel`), altrimenti alla sua fine rimetterebbe il tempo che scorre
  (trappola 6). In pausa cavaliere e incantesimi non ascoltano i click. *Salva ed esci* salva e
  ricarica Core: nelle build si torna al menu.
- **Le opzioni** (`OptionsPanel`), dal menu e dalla pausa: la lingua, che ha preso il posto di F9
  (azione e tasto tolti); i volumi generale, della musica e degli effetti, sui tre parametri esposti
  del mixer `Main` (il mixer non ha un gruppo per l'ambiente: passa da musica ed effetti); i nomi a
  terra sempre visibili. Tutto nelle preferenze, i volumi applicati in `Start` perché il mixer non
  li accetta in `Awake`.
- La tela è costruita da uno script dell'editor, con Cinzel, i fondi scuri e i bordi dorati
  dell'HUD. Il primo screenshot mostrava la casella dei nomi quasi invisibile: ora è più chiara e
  bordata.

*Test.* `SaveSlotsTests` (4, EditMode): percorsi, ultimo giocato, file rovinato, `save.json` spostato
senza perdere niente. `MenuTests` (7, PlayMode): senza salvataggi Continua manca e il livello non
parte, una nuova partita parte e salva nel suo posto; Continua riprende il posto più recente con
profondità, seme e oro; conferma prima di sovrascrivere e di cancellare; il file vecchio diventa il
posto 1; `Esc` chiude l'inventario e poi mette in pausa fermando tempo, suoni, hit stop e cavaliere;
Salva ed esci torna al menu con la partita da continuare; volume nel mixer in decibel e nomi sempre
visibili. `LanguageSwitchTests` cambia lingua dalle opzioni invece che con F9. 258 EditMode e 177
PlayMode verdi.

## Passo 10.4 — La città

**Com'è andata (12 ott 2026).**

- **Un accampamento sotterraneo**, scritto come mappa di testo (`Data/Levels/Town.txt`, 13 × 10
  celle): una sala con due tavoli apparecchiati, il fuoco al centro, scaffali e botti di birra
  contro il muro nord, giacigli e bauli ai lati, il banco del mercante a ovest, e a nord una nicchia
  con la scala che scende. Il nome in gioco è **Accampamento** (*Camp*), più fedele di "Città" per
  una sala sotto terra; la chiave resta `level.town`.
- **Costruita a runtime dalla sua mappa**, non salvata come scena dall'editor come i livelli della
  M3: la scena `Town` ha solo un `MapLevel`, che entrando costruisce la sala con lo stesso
  `LevelBuilder` della cripta e ne cuoce il NavMesh. Così c'è l'automappa (che le scene salvate non
  hanno) e ritoccare la città vuol dire cambiare il testo. `MapLevel` e `DungeonLevel` hanno ora
  una base comune, `BuiltLevel`, che il `LevelManager` cerca nella scena.
- **Profondità 0.** `LevelContext.IsTown` è la profondità 0; il titolo mostra solo il nome, senza
  numero; il salvataggio scrive `inTown` (il campo del formato 4) e il menu mostra "Accampamento"
  nel riepilogo del posto.
- **Si comincia lì**: ogni partita nuova, dal menu o con `-newgame`/`-seed`. Nell'editor e nei test,
  senza menu né riga di comando, Core parte ancora dalla cripta: i test cercano nemici e casse lì.
  La scala porta all'ingresso `Start` della profondità 1, la stessa cripta di sempre.
- **I mobili** sono nove modelli nuovi del pacchetto *Dungeon* (tavolo lungo apparecchiato,
  sgabelli, scaffali, botte decorata, giaciglio, baule, banco con la tovaglia, cassetta) in sette
  prefab di `Prefabs/Town/`, con il collider del loro ingombro sul layer degli ostacoli; il
  `TownTileset` li lega ai simboli della mappa. Il fuoco è fatto di due mezzi cumuli di macerie
  schiacciati e quattro torce accese inclinate a capanna, con una luce forte che tremola.
- **Più calda della cripta**: luce ambiente arancione (0,24 · 0,165 · 0,11 contro lo 0,03 quasi nero
  della cripta), torce su ogni parete, luci sui tavoli, sul banco e sul fuoco, nessuna ombra oltre a
  quella del cavaliere. Il primo screenshot era troppo buio al centro e sul lato dei giacigli: ho
  aggiunto il fuoco e le torce sui muri est e ovest. La luce ambiente si legge dalla scena salvata:
  cambiandola nel tileset va ricostruita la scena (*DarkDescent → Ricostruisci la scena della città*).
- **Il suono**: un crepitio di fuoco sintetizzato in loop come fondo, senza versi nel buio, e la
  musica *Medieval: The Bard's Tale* di RandomMind (CC0, OpenGameArt; liuto e flauto, loop di 58 s, al
  70%), scaricata con il permesso di Mirco: nessuna di quelle che avevamo era adatta (il loop *Blizzard*
  del pacchetto RKN è vento gelido). Tenuta in `.wav` su LFS, senza un convertitore a portata: Unity la
  comprime in Vorbis all'import, in streaming come le altre musiche.
- **Il portale non c'è ancora**: lo spiazzo accanto all'ingresso è libero per il passo 10.6, che
  aggiungerà anche l'ingresso d'arrivo del portale.

*Test.* `TownMapTests` (3, EditMode): profondità 0, ingresso e scala verso la profondità 1 della cripta;
ogni simbolo ha un mobile nel tileset e nessuno è un nemico; dall'ingresso si raggiunge ogni cella.
`TownTests` (3, PlayMode): una partita nuova comincia in città (scena attiva, niente nemici né casse,
titolo senza numero, automappa, luce calda, una sola luce con le ombre); la scala porta alla
profondità 1 della cripta; salvata in città, la partita lo dice e Continua riporta lì. `MenuTests` ora
si aspetta la città. 261 EditMode e 180 PlayMode verdi.

## Passo 10.5 — Il mercante

**Com'è andata (12 ott 2026).**

- **Il mercante** è il *Rogue* incappucciato di KayKit Adventurers, dietro il banco con la tovaglia
  sul lato ovest dell'accampamento. Banco e personaggio sono un prefab solo (`MerchantStall`, il
  simbolo `M` della mappa): il personaggio sta tra il banco e il muro, con l'animazione di riposo del
  cavaliere e le armi spente. Il nome sopra è *Mercante*; un click e il cavaliere va davanti al banco.
- **Il banco** si apre nella metà sinistra, dove sta il pannello del personaggio (uno chiude
  l'altro), con l'inventario aperto accanto. Dodici posti: prima le **provviste**, pozione di cura e
  di mana, sempre lì; poi gli **otto oggetti**. Sotto ogni posto il prezzo, rosso se l'oro non basta;
  il tooltip dice prezzo e tasto. **Click per comprare**: la pozione va nella cintura se c'è posto,
  il resto nella borsa; senza oro o senza posto lo dice sotto il banco e non tocca niente.
- **Vendere**: con il banco aperto, il **click destro** su un oggetto della borsa lo vende al 25% del
  prezzo invece di indossarlo o berlo, e il suo tooltip dice a quanto. L'oro della vendita non conta
  tra l'oro raccolto della vittoria (`Inventory.ReceiveGold` invece di `AddGold`).
- **L'assortimento** viene da `MerchantTable` (le provviste e le basi pesate, oggi quelle delle casse)
  e da un flusso suo del seme della partita (`SeedMixer.ForShop`): oggetti del livello della
  **profondità più bassa raggiunta** (la presa dalle mappe esplorate, che sono già nel salvataggio),
  da un seme di profondità e numero di uccisioni. Così cambia a ogni ritorno in città dopo aver
  combattuto e resta uguale per lo stesso stato, **senza salvarlo né cambiare formato**. Il prezzo
  della scelta: un oggetto comprato ricompare se si esce e si rientra senza aver ucciso niente (lo si
  ricompra, non si duplica niente).
- **Si chiude** con la sua X, chiudendo l'inventario, con `Esc` o aprendo il pannello del personaggio.
- La logica è in `Core` (`Shop`, `MerchantTable`, `TradeResult`, `LootRoller.RollStock`), la finestra è
  `ShopPanel` con dodici `ShopSlotView`, costruita da uno script dell'editor con lo stile delle altre.
- *Trappola ritrovata*: lo script dell'editor creava la tabella e poi apriva la scena Core, e il
  riferimento della tabella nel composition root restava vuoto (trappola già vista con la cripta:
  aprire una scena scarica gli oggetti caricati prima). Ricollegata dopo l'apertura.
- La pergamena del ritorno si aggiunge alle provviste al passo 10.6; i prezzi e le quantità d'oro si
  tarano con la build del 10.9.

*Test.* `ShopTests` (8, EditMode): la tabella; l'assortimento uguale per seme, diverso per ritorno e
profondità, con un flusso suo; comprare una provvista (resta) e un oggetto (sparisce); senza oro o
senza posto non cambia niente; vendere paga un quarto e non è oro raccolto; la profondità più bassa
senza la città; le righe del prezzo in oro e in rosso. `MerchantTests` (4, PlayMode): raggiunto il
mercante si apre il banco con dieci cose e i prezzi; comprare vuole l'oro; il click destro vende
dalla borsa; il banco si chiude in quattro modi e chiuso la borsa non vende più. 269
EditMode e 184 PlayMode verdi.

## Passo 10.6 — Pergamena del ritorno e portali

**Com'è andata (12 ott 2026).**

- **La pergamena del ritorno** è una definizione sua (`ReturnScrollDefinition`), 1 × 2 come le altre,
  con il modello della pergamena di Quaternius e un sigillo blu. Si legge con il click destro
  nell'inventario: nel dungeon apre un portale e sparisce, in città non fa niente e resta. Se ne
  comincia con una nell'ultima colonna dello zaino; il mercante la vende tra le provviste a 28 oro;
  cade raramente con un tiro suo (`SeedMixer.ForReturnScroll`, dominio "RETURN"), quindi il loot dei
  semi provati non cambia: 6% dalle casse, 3% da bruti e maghi, 2% dagli scheletri, 1% dallo sciame.
  A terra cade in diagonale, tra la pergamena dell'incantesimo e l'oro.
- **Il portale** è un ovale azzurro che gira, con una luce, scintille che salgono e un ronzio in loop;
  cresce in 0,6 s con il suono dell'apertura e guarda sempre la camera, anche quando la camera gira.
  Si apre 2,5 m davanti al cavaliere, o prima del muro, o di lato se davanti non c'è spazio; uno solo
  per volta: una seconda pergamena lo sposta. Sopra c'è scritto *Portale per l'accampamento*.
- **In città** si arriva sul nuovo ingresso `Portal`, il simbolo `P` della mappa, tre celle a ovest
  dell'ingresso iniziale, e il portale del ritorno aspetta due metri dietro il cavaliere: *Portale per
  il livello 3*. Riattraversato, riporta all'ingresso di quella profondità (rigenerata dal seme,
  ADR-061; la scena dalla cripta o dalle caverne secondo la profondità) e si chiude.
- **Il salvataggio** scrive finalmente la profondità del portale (`portalDepth`, già nel formato 4):
  chiusa la partita in città con il portale aperto, *Continua* lo ritrova. Il portale aperto nel
  dungeon non si salva: si riprende comunque dall'ingresso del livello.
- Tutto in `TownPortals`, accanto al gestore dei livelli in Core, che l'inventario conosce solo come
  `IPortalOpener`; il portale è `Portal`, con l'`Interactable` di sempre. Tre suoni sintetizzati in
  Python (apertura, ronzio, viaggio) e la texture del vortice generata dall'editor.

*Test.* `ReturnScrollTests` (4, EditMode): la pergamena (dimensioni, database, provviste, prezzo,
tooltip nelle due lingue); cade raramente, più dalle casse, con un seme suo; le profondità e le loro
scene e ingressi; l'arrivo `P` accanto all'ingresso della città. `PortalTests` (3, PlayMode): la
pergamena di partenza non si legge in città e nel dungeon apre un portale sul NavMesh, uno solo per
volta; il portale porta in città, dove aspetta quello del ritorno e il salvataggio lo ricorda, e
riattraversato riporta alla profondità 1 e si chiude; con il portale aperto, *Continua* lo ritrova.
I test del mercante contano una provvista in più. 273 EditMode e 187 PlayMode verdi.

## Passo 10.7 — Il Re delle ossa

**Com'è andata (12 ott 2026).**

- **La sala.** Alla profondità del boss (`CaveSettings.BossDepth`, la 8) il generatore delle caverne
  scava una sala di 5 × 5 celle nella zona aperta più lontana dall'ingresso e la chiude con un anello
  di roccia, lasciando una porta sola: la più vicina all'ingresso a piedi tra quelle che lasciano la
  sala raggiungibile. Quello che l'anello taglia fuori torna roccia. Dentro ci sono solo il re al
  centro (simbolo `K`) e una candela per angolo; sulla porta il simbolo `G`. Gli altri livelli non
  cambiano: la sala nasce solo a quella profondità.
- **Il re** è il bruto più grande: lo `Skeleton_Warrior` a scala 1,8 con l'ascia, una luce rossa, 600 di
  vita, armi sue (colpo caricato 30–42 con un arco di 140°, colpi leggeri 12–18) e il nome *Il Re delle
  ossa*. Quando il cavaliere arriva a 8 m dal centro (la porta è a 12), la porta si chiude: tre
  colonne di roccia salgono dal pavimento con il rumore di una porta pesante, un collider e un
  ostacolo del NavMesh la tagliano. Il re ruggisce (il ruggito del pacchetto di puadlung, ADR-070) e
  la sua vita resta nella barra in alto per tutto lo scontro (`EnemyBar.Pin`).
- **Tre fasi per vita** (`BossPhases`): sopra il 60% mischia e colpo caricato; fino al 30% evoca tre
  dello sciame ogni 12 s, mai più di sei vivi, su punti del NavMesh della sala; sotto è più veloce del
  40% e ogni 7 s si ferma, un cerchio rosso di 5 m si riempie a terra per 1,4 s e parte l'anello di
  fuoco, con le fiamme dell'incantesimo del cavaliere: chi è ancora dentro prende 28–38 di fuoco.
  Gli evocati non danno esperienza né loot.
- **Morto il re**, gli evocati cadono con lui, la porta riscende e la vittoria va subito nel
  salvataggio (`RunStats.BossDefeated`, già nel formato 4): tornando alla profondità 8, rigenerata, il
  re non c'è più (trappola 4). Lascia sempre un oggetto almeno raro del livello (la tabella ha ora una
  rarità minima), una pozione, molto oro e la pergamena del ritorno.
- **Morto il cavaliere** durante lo scontro, tornando in vita all'ingresso la porta si riapre, gli
  evocati spariscono e il re ricomincia a vita piena dalla prima fase.
- **La profondità 8 si alleggerisce** come la 7 (due gruppi di sciame, due bruti, uno o due maghi):
  con la sala del boss lo spazio per tutti non c'era, e il re con i suoi evocati prende il posto degli
  altri. Il test delle medie della M7 ora lo dice.

*Test.* `BossTests` (EditMode): le fasi per vita; su otto semi la sala alla profondità 8, piena, con
il solo re dentro e una porta sola, senza scala, e nessuna sala alla 7; il bottino del re; il prefab e
il tileset. `BossPlayModeTests` (3): entrando la porta si chiude e il NavMesh non porta fuori, la barra
tiene il re; evoca lo sciame, in furia corre e telegrafa l'anello; morto, la porta si apre, la vittoria
è nel salvataggio e restano un oggetto raro e la pergamena; morto il cavaliere lo scontro ricomincia;
sconfitto, il re non rinasce. `CaveContentTests` contano il boss a parte. 284 EditMode e 190 PlayMode
verdi (`PlayerAttackTests.ClickOnSkeleton_ApproachesAndHitsOnce` è caduto una volta nel giro completo e
passa da solo: un test che ogni tanto balla, da tenere d'occhio).

## Passo 10.8 — Vittoria e riconoscimenti

**Com'è andata (12 ott 2026).**

- **Il portale della vittoria.** Morto il re, una cella a nord del centro della sua sala si apre un
  portale, *Lascia le profondità*: lo stesso dei portali della pergamena, tenuto a parte da
  `TownPortals`, così una pergamena letta lì non lo chiude. Non si salva: c'è ogni volta che si torna
  alla profondità 8 dopo la vittoria, al posto del re che non rinasce. Chi esce prima di attraversarlo
  lo ritrova.
- **La schermata.** Attraversato, si arriva all'accampamento, dove la partita si salva come sempre
  entrando in un livello (in città, boss sconfitto); poi il gioco si ferma e il riquadro della pausa,
  ingrandito, dice *Vittoria*, *Il Re delle ossa è caduto* e i contatori della D11: tempo di gioco
  (`RunStats.FormatTime`, "12:05" o "1:02:05"), livello, nemici uccisi, morti, oro raccolto.
- **I riconoscimenti** salgono dal basso in tempo reale, a gioco fermo, sopra l'accampamento: il nome
  del gioco e dell'autore, poi opera e autore di ogni asset di terzi letti dalla tabella di
  `CREDITS.md` (`CreditsTable`, senza link né markdown), una riga per quello fatto per il gioco e
  *Grazie per aver giocato*. Finiti, o con *Torna al menu*, si salva e si torna al menu iniziale, da
  cui *Continua* riprende all'accampamento.
- `CREDITS.md` sta nella radice del repo e non entra nella build: il gioco legge una copia,
  `Data/Credits.txt`, che si aggiorna con *DarkDescent → Aggiorna i riconoscimenti*. Un test fallisce
  se le due non sono uguali.

*Test.* `VictoryTests` (3, EditMode): il formato del tempo; la tabella dei riconoscimenti, con tutte le
righe di terzi e nessuna di quelle fatte per il progetto, senza markdown; la copia uguale
all'originale. `VictoryPlayModeTests` (1): morto il re si apre il portale, attraversato si arriva in
città con il gioco fermo sulle statistiche e il salvataggio in città con la vittoria; i riconoscimenti
salgono, il pulsante riporta al menu e *Continua* all'accampamento. `BossPlayModeTests`: tornando alla
profondità 8 dopo la vittoria c'è il portale. 287 EditMode e 191 PlayMode verdi.

---

## Passo 10.9 — Build da provare e correzioni

Build della CI del 12 ott 2026 (run 38219581157), provata da Mirco. Correzioni:

- **L'oro nell'inventario** sta al centro della finestra, sotto gli anelli, invece che allineato a
  destra.
- **Il portale della città riporta dove si era aperto**, come in Diablo, e non più all'ingresso della
  profondità. `TownPortals.PortalSpot` ricorda il punto del portale del dungeon e
  `LevelManager.LoadLevelAt` ci porta il cavaliere, sul NavMesh della profondità rigenerata dal seme.
  L'ingresso del livello resta quello di sempre: Continua e la ripartenza dopo la morte usano lui. Il
  punto va nel salvataggio (`_hasPortalSpot`, `_portalSpot`) senza cambiare formato: un salvataggio
  del formato 4 scritto prima non ha i campi e il portale riporta all'ingresso.

---

## Trappole note

1. **Il menu cambia l'avvio di tutti i test:** oggi i test caricano `Core` e trovano la partita
   già avviata. Il menu va in una scena sua (`Menu`) prima di `Core`, e i test continuano a
   caricare `Core` direttamente, come `-newgame`.
2. **Tre salvataggi e un file vecchio:** il primo avvio con il codice nuovo deve trovare
   `save.json`, spostarlo nel posto 1 e non cancellarlo mai, nemmeno se lo spostamento fallisce a
   metà.
3. **L'oro è un tiro nuovo nel loot** (trappola 10 della M7, ADR-067): va in fondo, con un flusso
   suo, o cambia gli oggetti di tutti i semi provati.
4. **Il portale riporta a un livello rigenerato:** nemici e casse tornano (ADR-061). Se si torna
   alla profondità 8 dopo aver ucciso il boss, il boss non deve rinascere: la vittoria va nel
   salvataggio.
5. **La sala del boss non deve chiudersi con il cavaliere fuori o con lo sciame dentro il muro:**
   la chiusura parte quando il cavaliere è ben dentro, e lo sciame evocato nasce su punti
   campionati sul NavMesh della sala.
6. **La pausa ferma `Time.timeScale`:** hit stop, ricariche, rigenerazione del mana e coroutine
   con `WaitForSeconds` si fermano con lei; quello che deve andare in pausa (la UI) usa il tempo
   non scalato, e il fade delle scene anche.

---

## Test

| Test | Cosa controlla |
|---|---|
| `GoldTests` (EditMode) | Oro di nemici e casse per profondità, prezzi da tipo, rarità e livello, vendita al 25% |
| `SaveSlotsTests` (EditMode) | Tre posti, riepilogo letto dal file, ultimo giocato, `save.json` spostato nel posto 1 |
| `SaveMigrationTests` (EditMode, ampliato) | Il file vero del formato 3 si carica con il codice del formato 4 |
| `MenuTests` (PlayMode) | *Continua* manca senza salvataggi; *Nuova partita* chiede conferma sopra un posto occupato; la pausa ferma il gioco e lo riprende |
| `TownTests` (PlayMode) | Si comincia in città; la scala porta alla profondità 1; il portale riporta alla profondità salvata |
| `MerchantTests` (PlayMode) | Comprare toglie l'oro e mette l'oggetto in borsa; senza oro o senza posto non compra; vendere dà il 25%; l'assortimento dipende dal seme e dalla visita |
| `BossTests` (EditMode e PlayMode) | Le tre fasi per vita; l'evocazione dello sciame; la sala si chiude e si riapre; il loot garantito; la vittoria nel salvataggio |
| Rifiniture (PlayMode) | Click destro indossa e scambia; il pulsante dei punti compare e sparisce; i colori del pannello; i nomi con `Alt` non si sovrappongono |

---

## Checklist di chiusura

- [x] Decisioni D1–D13 confermate
- [x] Rifiniture della prova della M9
- [x] Oro, prezzi e formato 4
- [x] Menu, tre salvataggi, pausa e opzioni
- [x] Città, mercante, pergamena del ritorno
- [x] Boss e vittoria
- [ ] Build della CI provata su un altro PC
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m10`
- [ ] Scheda della M11 scritta prima di cominciarla
