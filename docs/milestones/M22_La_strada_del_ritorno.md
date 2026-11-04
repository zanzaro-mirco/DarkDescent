# M22 — "La strada del ritorno"

**Cosa deve succedere (Definition of Done):** in ogni livello della cripta e delle caverne c'è una **scala
in su** che funziona: porta al livello di sopra, davanti alla sua scala che scende, e quel livello è **com'era**
(nemici uccisi, casse aperte, oggetti a terra). Dalla profondità 1 si risale al **borgo**, dal cunicolo della
M18. Le **stanze della cripta** sono più grandi: nessuna più stretta di 4 celle per lato (16 m). Le caverne
  non hanno stanze e restano com'è la loro forma.

- La scala in su sta nella stanza d'ingresso, in un muro, mai in mezzo agli arredi e mai a chiudere il
  percorso tra le porte; si clicca o ci si cammina contro, come la scala che scende.
- Il portale per il borgo resta com'è: la scala in su non lo sostituisce, lo affianca.
- La sala del re non diventa una via di fuga: finché il re tiene il giocatore, la scala non serve (e dalla sala
  è comunque lontana).
- I livelli di ogni seme cambiano (le stanze più grandi): le mappe di prova, le soglie e la ronda si ritarano,
  come alla M15.
- Tag **`v1.10.0`**.

**Fuori da questa milestone:** i nemici che inseguono sulle scale; scorciatoie tra livelli lontani; più di
otto livelli; cambiare il portale; stanze di forme nuove (a L, rotonde); la sala del re più grande (resta
com'è).

---

## Stato verificato il 4 novembre 2026

Verificato dopo la chiusura della M21 (tag `v1.9.0`, build 41835856766 verde). Le cifre delle stanze sono una
**stima**: ho riportato il taglio della cripta (`DungeonGenerator.Split`/`AddRoom`) in uno script e l'ho
girato su 5000 semi; la misura vera, con il generatore, è il passo 22.2.

| Cosa | Com'è oggi |
|---|---|
| Come si scende | Ogni livello generato ha la scala `>` (`DungeonGenerator.StairsSymbol`, nella stanza più lontana a piedi, riga nord): `LevelBuilder.PlaceStairsDown` ci mette il modello e un `LevelExit` (trigger di 4 m + 1,5 m davanti, `ExitTriggerReach`) con la scena, l'ingresso `FromAbove` e la profondità `depth + 1` dalla direttiva `@exit`. Alla profondità 4 la scena dopo è `Level_Caves`; all'8 non c'è scala (`layout.Remove`) |
| Come si entra | L'ingresso è la cella `<` al **centro della stanza** di partenza, scelta dal seme, **senza modello**: `LevelEntrance` con id `Start` (profondità 1) o `FromAbove` (le altre, `DungeonLevel.EntranceAt`). Il livello non ha niente che risalga |
| Il cunicolo | Alla profondità 1 `TunnelSpot.TryChoose` sceglie un muro della stanza d'ingresso (nord, poi ovest, est, sud) e il builder ci mette lo **sbocco** (`TunnelMouth`, campo `EntranceMouth` del tileset: solo `DungeonTileset`, non `CaveTileset` né `VillageTileset`). È solo una scena: **non ha un'uscita**. Le regole di `TunnelSpot` (due celle di lato della stanza, roccia dietro, mai un angolo né l'imbocco di un corridoio) vengono dalle ronde del 31 ott e dell'1 nov |
| Il borgo | `Town.txt`: `@exit Level_Crypt Start 1` e la grotta `>` a nord; un solo ingresso `<` (`Start`) in basso più `P` per i portali. Non c'è un ingresso davanti alla grotta |
| `LevelExit` | `_targetDepth`: **0 vuol dire «quella sotto»** (`LevelManager.HandleExitRequested`: `exit.TargetDepth > 0 ? … : Depth + 1`). Il borgo ha profondità 0, quindi un'uscita verso il borgo non si può scrivere così. Il builder passa sempre una profondità esplicita; lo zero serve solo ai livelli fatti a mano |
| Il livello lasciato | `LevelRecorder` (ADR-079): prima di scaricare un livello ne scrive il ricordo (nemici uccisi, casse aperte, oggetti a terra, barili, pozzi), per **scena e profondità**; rientrando, il livello si rigenera dal seme e `Restore` lo rimette com'era. I ricordi sono nel salvataggio. `Record.Matches` guarda quanti nemici e casse c'erano: se il livello generato non corrisponde, ripartono nuovi |
| Il salvataggio | Versione 9 (M21): scena, **id dell'ingresso**, profondità, seme, ricordi. `Continua` fa `SetStartLevel(scena, ingresso, profondità)`: qualunque id esista nel livello va bene |
| Il portale | ADR-076 e ADR-086: dal borgo riporta nel punto dove si era aperto (`LoadLevelAt`), non sull'ingresso; nella sala del re vivo non si apre (`BossController.HoldsPlayerAt`) |
| La morte | `ReturnToEntrance` rimette il giocatore all'ingresso da cui è entrato nel livello (`CurrentEntrance`), senza ricaricare niente |
| L'automappa | `MapPainter` colora la scala `>` e il suo stato esplorato; l'ingresso non ha un simbolo suo |
| Le caverne | **Non hanno stanze**: `CaveGenerator` è un cammino casuale (quattro camminatori scavano fino al 37% di pavimento, poi la smussatura); `layout.Rooms` è vuoto e `StartRoom` è −1. L'ingresso è la **cella aperta più lontana dal centro** (`FarthestOpenCell`), la scala il punto più lontano da lui. `TunnelSpot`, che parte dalla stanza d'ingresso, qui non si applica così com'è; `CaveSettings` non ha dimensioni di stanza, solo la griglia (28×28) e la frazione di pavimento |
| Le stanze della cripta | `CryptSettings`: griglia 28×28 celle da 4 m, zone da 6 a 11 celle, **stanze da 3 a 7 celle per lato**. Il taglio dà in media **11,5 stanze** per livello (da 9 a 16), di area mediana **16 celle** (256 m²). Il **35%** ha al massimo 12 celle (3×3 o 3×4, cioè 12×12 m o 12×16 m) e il **56%** ha un lato di 3 celle. Con la sala del re e le stanze con punti d'interesse (M13) che chiedono spazio, le piccole sono quelle che restano spoglie o strette |
| I test | 28 file di test chiamano il generatore o `CreateMap`; i semi fissi e le soglie delle mappe (pavimento minimo 22%, scala al 45%, M15) e la ronda (`NightlyEditModeTests`) leggono la disposizione |

---

## Decisioni

**Confermate da Mirco il 4 novembre 2026:** tutte, D1–D17, senza tagli. Il disegno (22.1) resta da approvare prima del codice.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M22 "La strada del ritorno"**, tag **`v1.10.0`**. Dentro: la scala in su, il ritorno al borgo, le stanze più grandi, il salvataggio 10 | Cambia come si gioca (si risale) e com'è fatto ogni livello: una versione minore |
| D2 | **Dove si risale** | In **ogni livello generato**, cripta (1–4) e caverne (5–8), compreso il primo. Dalla profondità *N* si arriva alla *N−1*; dalla 1 al borgo. All'8 c'è, nella stanza di partenza come negli altri | «Come in Diablo 1» (richiesta del 17 e del 26 ott): scendere e salire senza il portale |
| D3 | **Il modello** | La scala in su è lo **sbocco del cunicolo** della M18 (`TunnelMouth`: fronte di massi e galleria nella roccia), che già c'è alla profondità 1 e lì diventa un'uscita vera. Si mette **in tutti i livelli**, anche nelle caverne (nuovo campo `EntranceMouth` nel `CaveTileset`); nelle caverne, dove non c'è una stanza d'ingresso, la regola di `TunnelSpot` si applica alle celle attorno all'ingresso, partendo dalla più vicina (22.3). Alternativa: un pezzo di scala nuovo dai pezzi KayKit, da costruire e provare. Le foto del disegno (22.1) mostrano le due | Zero asset nuovi, ed è la stessa porta da cui si arriva alla profondità 1: si risale per dove si è passati. Una scala vera in su, in una cella di roccia, va fatta con una galleria comunque |
| D4 | **Dove sta** | La regola di `TunnelSpot` (muro nord, poi ovest, est, sud della **stanza d'ingresso**; due celle di lato della stanza, roccia dietro, mai un angolo né l'imbocco di un corridoio) diventa quella della scala in su, a tutte le profondità. **Nessun tiro del seme**: nemici, casse e loot dei livelli non cambiano per la scala | La regola è già provata sulle ronde (31 ott, 1 nov) e dipende solo dalla disposizione. Si evita lo stesso bug del 31 ott: un fronte che stringe l'imbocco di un corridoio chiude il NavMesh |
| D5 | **Dove si arriva** | Un ingresso nuovo **`FromBelow`**, nella cella davanti alla scala `>` del livello di sopra (la cella da cui ci si arriva, non la scala), a più di 2 m dal bordo del trigger: arrivando non si riscende. Il giocatore guarda lontano dalla scala | Il trigger della scala sporge 1,5 m nella cella davanti; arrivare nel suo centro darebbe 0,5 m di margine e rimbalzerebbe |
| D6 | **Il livello di sopra torna com'era** | Nessun codice nuovo nel ricordo: `LevelRecorder` lavora già per scena e profondità (ADR-079). Si **prova** che dopo una salita il livello ha nemici uccisi, casse aperte, oggetti a terra e l'automappa esplorata | È la ragione per cui il portale era stato preferito alla scala (ADR-076): il rifare non c'è più, ma va provato |
| D7 | **Scene e profondità** | `DungeonSettings` ha già `NextScene`; si aggiunge **`PreviousScene`** (le caverne → `Level_Crypt`, la cripta → il borgo), e `LevelExit` smette di usare lo **0 per «quella sotto»**: la profondità di arrivo è sempre scritta (il borgo è 0, un livello fatto a mano la sua). `LevelManager.HandleExitRequested` la usa com'è | Senza questo, un'uscita verso la profondità 0 si leggerebbe «la prossima». Il builder la passa già sempre esplicita: cambia solo il modo di dire «non so» |
| D8 | **Il borgo** | Un ingresso **`FromCrypt`** in `Town.txt`, nella piazzola davanti alla grotta (a sud dell'arco `>`), con il giocatore rivolto verso il borgo. Ogni salita dal livello 1 ci porta lì; il borgo ritrova la sua ora del giorno (M18) e le sue luci, e *Nuova partita* parte ancora da `Start` | Chi risale dalla grotta esce dalla grotta, non dalla piazza |
| D9 | **Il re e il portale** | Come oggi: la scala in su della profondità 8 sta nella stanza d'ingresso, la sala del re ha il cancello (`BossGate`) che si chiude: durante lo scontro la scala non si raggiunge, e il portale non si apre. Si **prova**, non si aggiunge una regola. Il portale resta com'è (ADR-076, ADR-086) | Non si deve fuggire dal re, ma non serve un divieto in più: il posto lo fa già |
| D10 | **L'automappa** | Un simbolo e un colore per la scala in su (azzurro tenue, come l'ingresso), esplorato come `>`; non cambia `ExplorationMemory`: la scala è nella disposizione | Si vede dove si è entrati e dove si può tornare |
| D11 | **Le stanze più grandi** | **Griglia 32×32, zone da 7 a 12 celle, stanze da 4 a 8 celle per lato**, per la **sola cripta** (le caverne non hanno stanze: se la griglia delle caverne va allargata, si guarda nelle foto e si decide a parte, non qui). Stima: stanze di area mediana **25 celle** (400 m²) invece di 16, nessuna sotto le 16 celle (16×16 m), circa 11 stanze per livello come oggi. Alternative misurate nella stima: *solo* il minimo a 4 (griglia 28×28: mediana 20, ma le stanze grandi non crescono); griglia 34 e stanze 4–9 (mediana 25, 12,7 stanze: livelli più lunghi da attraversare); griglia 36 con stanze 5–9 (mediana 36, ma 10,8 stanze e 144 m di lato) | Il minimo da solo risolve le stanze strette, non quelle poco ariose; la 36 farebbe lunghe camminate vuote. La 32 con minimo 4 alza tutto senza allungare il giro |
| D12 | **Cosa cresce con le stanze** | Il numero dei nemici e delle casse resta quello per profondità (`EnemyCount`, `ChestCount`): sono legati alla misura delle difficoltà (M14, M20, M21) e a quella del loot (M13), non alla superficie. Lo spazio in più va a **arredi, punti d'interesse** (la soglia delle «stanze grandi» della M13 si rimisura) e a **combattere** (gruppi che si aprono). Se i livelli sembrano vuoti, si alza il numero di gruppi, non la forza | I numeri dei nemici sono già tarati; cambiarli rompe la misura |
| D13 | **I livelli dei salvataggi in corso** | **Salvataggio 10**: la migrazione dalla 9 azzera i **ricordi dei livelli** e il punto del portale, e lascia il resto (scena, profondità, ingresso). Chi ha una partita in una cripta la riprende in un livello nuovo dello stesso seme, con le stanze nuove. Nessun salvataggio è uscito dal PC di Mirco, come per l'ADR-079. Si aggiunge `save_v10.json` e `save_v9.json` congelato | I ricordi leggono i posti di nemici e casse nell'ordine in cui il livello li costruisce: con un generatore diverso rimetterebbero oggetti a terra a casaccio |
| D14 | **La misura dei livelli** | Si rifanno **le soglie delle mappe** (pavimento minimo, scala, ingresso, raggiungibilità) su più semi, **a mano e di ronda**; il nuovo limite è provato con il generatore vero, non con la stima. La ronda prova inoltre, su 64 semi e 8 profondità, che **la scala in su c'è, è raggiungibile dall'ingresso e dalla scala giù, non chiude un corridoio e che il NavMesh passa davanti**. Le soglie della M15 si **ritarano** una volta sola (come al 15.5), non si allargano per far passare | È la lezione della M15: le stanze cambiano i livelli di ogni seme |
| D15 | **Cosa NON cambia** | La misura delle difficoltà (nemici e danni non dipendono dalla mappa), il loot, gli alberi, i filmati, la sala del re, il borgo (salvo il nuovo ingresso), il portale | I numeri non si ritarano per una scala |
| D16 | **Si controlla** | **EditMode:** il generatore (stanze ≥ 4 celle, livelli connessi, scala in su valida a ogni seme e profondità), `PreviousScene`, `LevelExit` senza lo zero, il salvataggio 10 e la migrazione, `FromBelow` davanti alla scala giù, la mappa del borgo con `FromCrypt`. **PlayMode:** si scende e si risale di quattro livelli (cripta), si passa dalla cripta alle caverne e si torna, si risale al borgo e si riscende; il livello di sopra è com'era; l'arrivo non rimbalza; la scala del livello 8 e la sala del re. **Foto** per Mirco: le stanze di prima e di dopo, la scala in su nella cripta e nelle caverne, il borgo all'uscita | Come alle milestone precedenti |
| D17 | **Stima e linee di taglio** | **12–16 h**, più delle 5–8 h previste nel piano: la risalita ha il borgo, il salvataggio e i ricordi da provare, e le stanze rifanno le soglie di ogni seme. Se serve tagliare, in quest'ordine: le caverne senza scala in su (solo la cripta); l'ingresso nuovo del borgo (si esce dal borgo da `Start`); il simbolo dell'automappa. Le stanze più grandi, la scala in su della cripta e il ritorno al livello com'era no | Stima da rivedere dopo il disegno |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 22.1 | **Il disegno**: una pagina con le **stanze di prima e di dopo** (quattro semi, la stessa disposizione affiancata, le tre alternative di D11) e la scala in su nelle due versioni di D3 (lo sbocco, o il pezzo di scala), con le foto del generatore. Si va avanti **solo con il sì di Mirco** | 1–2 |
| 22.2 | **Le stanze più grandi** (cripta): le impostazioni, la misura vera con il generatore, le soglie delle mappe e i test con i semi fissi rifatti, la ronda con le nuove soglie (D11, D12, D14) | 3–4 |
| 22.3 | **La scala in su**: `TunnelSpot` generalizzato alla scala, il builder che mette lo sbocco e l'uscita a ogni profondità, `PreviousScene`, `LevelExit` senza lo zero, gli ingressi `FromBelow` e `FromCrypt`, il `CaveTileset` con lo sbocco (D2–D8) | 4–5 |
| 22.4 | **Automappa, salvataggio e prova di partita**: il simbolo, il salvataggio 10 con migrazione e campione, il livello di sopra com'era, il re e il portale, le prove PlayMode di discesa e salita (D6, D9, D10, D13, D16) | 2–3 |
| 22.5 | **Misura e chiusura**: la ronda sui 64 semi, le ADR, le lezioni, il piano, il README, `bundleVersion` 1.10.0, il tag `v1.10.0` dopo i Tests verdi in CI (D14) | 2–3 |

---

## Rischi da tenere d'occhio

- **Rimbalzo sulle scale.** Il trigger della scala sporge 1,5 m: arrivare da sotto nella cella davanti, o dall'alto
  vicino alla scala in su di una stanza da 4 celle, può far ripartire subito il viaggio. Un test PlayMode per
  ogni verso (D5, D16).
- **Lo zero di `LevelExit`.** Oggi vuol dire «quella sotto»; il borgo è lo zero. Va tolto in un colpo solo, con i
  test dei livelli fatti a mano (`Level_01`, `Level_02`).
- **Il cunicolo e i corridoi.** Le due ronde che hanno trovato lo sbocco di lato a un corridoio (31 ott, 1 nov)
  valgono ora per ogni profondità: più livelli, più casi. La ronda sui 64 semi è il controllo.
- **Le soglie delle mappe.** Con stanze più grandi il pavimento sale e la scala può cadere in punti diversi: le
  soglie si ritarano **una volta**, con la misura vera, non una per una.
- **La camera.** La camera guarda da sud-ovest: i muri nord e ovest li vede alti (e lo sbocco ci si vede
  bene), est e sud bassi. `TunnelSpot` li prova in quell'ordine per questo; con la scala in su su ogni
  profondità, le foto del disegno (22.1) mostrano anche il caso dei muri bassi.
- **NavMesh e tempi.** I livelli crescono di circa il 30% di pavimento: si rimisurano i tempi di generazione e
  cottura (`-benchmark-levels`, M18) e il peso dell'artefatto.
