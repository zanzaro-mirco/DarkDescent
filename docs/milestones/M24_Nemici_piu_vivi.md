# M24 — "Nemici più vivi"

**Cosa deve succedere (Definition of Done):** i nemici smettono di aspettare fermi di essere ammazzati. Gli **scheletri e lo
sciame** a vita bassa **scappano** e tornano, e chiamano i compagni; **alcuni nemici camminano** tra due o tre punti della
loro stanza e si accorgono del cavaliere che passa; **alla fine della cripta** (profondità 4) c'è un **secondo boss**, con la
sua sala e il suo tesoro, una tappa a metà discesa; e chi lo vuole può accendere all'altare **«Il mondo cresce con te»**: un
cavaliere forte che torna ai primi livelli trova nemici e oggetti del suo livello, non della zona. Italiano e inglese, il
salvataggio passa al formato 11 solo per l'interruttore, e **con l'interruttore spento i numeri della misura non si muovono**.

- La **fuga** è uno stato nuovo nella macchina a stati (ADR-042): scatta a vita bassa o quando muoiono compagni vicini; corre via
  dal cavaliere sul NavMesh per qualche secondo, poi torna a combattere, una sola volta per nemico. Per gli scheletri e lo
  sciame, non per il bruto, il mago, i campioni, gli unici e i re. Le soglie sono dati dell'archetipo.
- Le **ronde** sono uno stato nuovo anch'esso: un nemico su quattro (scelto da un hash della cella, **senza tiri del seme**)
  gira tra due o tre punti entro 8 m da casa, con una pausa a ogni punto. Se vede il cavaliere insegue come gli altri; oltre
  il limite (ADR-050) torna alla ronda, non da fermo. I ricordi dei livelli (ADR-079) non cambiano.
- Il **boss della profondità 4** ha un'unica sala (quella della scala giù), una barra in alto come il re, due fasi, e un tesoro
  quando cade. Si batte una volta per mondo (lo dicono già i ricordi del livello), e la scala resta aperta a chi non lo vuole.
- **Il mondo che cresce** cambia il livello di nemici e oggetti, non la profondità: il livello della zona diventa il più alto tra
  quello di sempre e il livello del cavaliere meno un margine.
- Tag **`v1.12.0`**.

**Fuori da questa milestone:** altri boss (uno per profondità), nemici che si curano o evocano a metà strada, nemici volanti o
che si nascondono, un livello di tutorial, i barili esplosivi, le casse nuove (mimic, bauli maledetti: lista di Mirco del 19 ott),
la seconda classe nemica del Mago, cambiare le difficoltà (M14): restano come sono.

---

## Stato verificato il 5 novembre 2026

Verificato dopo la chiusura della M23 (tag `v1.11.0`, build 42027880526 verde). Il piano del 18 ottobre ha **tre cose da correggere**,
corrette qui: i nemici **non hanno un livello** (hanno vita, arma e armatura fissi per tipo, più la difficoltà: il «livello della zona» serve agli
oggetti e all'esperienza, non a loro), e la cripta **non ha una sala del boss** (la sala del re la generano le caverne).

| Cosa | Com'è oggi |
|---|---|
| La macchina a stati | `EnemyBrain` esegue un `EnemyStateBase` alla volta (`Core/Enemies`): `Idle, Chase, Attack, WindUp, Recover, Return, Dead, Ranged, Retreat`. L'archetipo (`EnemyArchetype`, un asset per tipo) sceglie gli stati in `CreateStates()`: scheletro e sciame `Idle, Chase, Attack, Return, Dead`; il bruto aggiunge `WindUp` e `Recover` (`_recoverTime` 0,6); il mago `Idle, Ranged, Retreat, Return, Dead`. Il corpo (`IEnemyBody`) è un'interfaccia: gli stati si provano in EditMode con un corpo finto |
| Cosa chiede lo stato al corpo | `IsTargetAlive`, `IsTargetInRange`, `IsSwinging`, `CanSeeTarget()` (un raggio, ogni 0,2 s da fermo), `Engage()`, `Disengage()`, `IsBeyondLeash`, `IsHome`, `GoHome()`, `ArriveHome()`. **Non ha** né la vita né un modo di andare in un punto qualunque: la fuga e la ronda vogliono due cose nuove sul corpo |
| Tornare a casa | `ReturnState` lascia il bersaglio, va dov'è nato (`GoHome`) e **riprende la vita piena** (ADR-050): `_leashRange` 20 m, 30 per lo sciame. Una fuga che oltre il limite portasse a `Return` farebbe curare il nemico: va evitata |
| Il branco | `EnemyBrain.Alert()` sveglia un nemico fermo; `EnemyPack` avvisa i compagni dello stesso tipo entro `_packRadius`, ma **solo per i tipi che ce l'hanno** (12 m lo sciame, 0 gli scheletri e il bruto): per «chiamare i compagni» gli scheletri vogliono un raggio loro |
| Il mago che si allontana | `RetreatState` è già una fuga breve (2 s al massimo) per tenere la distanza: la fuga nuova è un'altra cosa (via dal combattimento, non via dal colpo) |
| I numeri dei nemici | Vita, arma, destrezza e armatura stanno nel prefab di ogni tipo; la difficoltà (`DifficultyTier`) li moltiplica (`Life` 1 → 40,5, `Damage` 1 → 12,3, colpire e Armatura in più, `ApplyDifficulty` in `CompositionRoot`). **La profondità non li cambia**: la misura (`DifficultyCensus.Foe`) costruisce il nemico solo da questi numeri e dalla difficoltà |
| Il livello della zona | `ProgressionSettings.ZoneLevel(depth, bonus)` = `depth × 1,25` arrotondato al più vicino (mezzo in su) + il bonus della difficoltà (0, 10, 20, 30…45). Serve a **due cose**: il livello degli oggetti (`LootRoller`) e il calo dell'esperienza (`ExperienceForKill`: −10% per livello che il cavaliere ha oltre la zona). Il livello massimo del cavaliere è 50 |
| Il re | `BossController` (profondità 8, caverne): sala sua dal generatore (`BossCell`, `GateCell`), porta che si chiude (`BossGate`), tre fasi per vita (`BossPhases`: mischia, sopra il 60%; evoca, sotto; furia, sotto il 30%), barra in alto, tesoro quando cade, portale della vittoria. Da `CompositionRoot.BindBoss`: già sconfitto non rinasce (`_runStats.BossDefeated`). **La cripta non ha niente di simile**: stanze e corridoi, con la scala giù nella stanza più lontana |
| La scala giù della cripta | Alla profondità 4 porta alle caverne (scena `Level_Caves`, profondità 5). Sta nella **stanza più lontana dall'ingresso** (`FarthestRoom`), sulla riga più a nord, con roccia a nord e pavimento a sud; stanze da 4 a 8 celle per lato (M22) |
| I nemici speciali | `SpecialEnemy` (campioni, unici Morvak e Grumm, il re con i modificatori): hanno la loro barra e il loro grado; nessuno di loro scappa né gira |
| I ricordi dei livelli | `LevelRecorder` (ADR-079): ricorda per scena e profondità **quali nemici sono morti** e le casse aperte; un nemico vivo riparte da casa. Un boss battuto resta battuto finché il mondo non si rifà |
| Il salvataggio | Formato **10**. L'altare delle difficoltà (M14) è l'unico posto dove si cambia il mondo (difficoltà, rifare il mondo) |
| La misura | `DifficultyCensus` (fasce: scheletro in 1–5 colpi, bruto in 4–14, il cavaliere regge almeno 3 colpi del bruto…) misura il cavaliere e il Mago a ogni difficoltà e profondità, con gli oggetti **della zona**; la ronda la rifà ogni notte su semi nuovi con un margine del 3% (issue 7) |
| I test di ogni push | Molti test PlayMode **contano sul fatto che i nemici stiano fermi** finché non vedono il cavaliere (percorsi, scale, portali, barre). Una ronda accesa di default li romperebbe: serve un interruttore spento nei test (come i suggerimenti della M23) |

---

## Decisioni

**Confermate da Mirco il 5 novembre 2026:** tutte, D1–D18, **come proposte**, senza tagli. Le quattro scelte vere vanno quindi così: **D8** il Custode a due fasi
(non un re più corto), **D9** la sala della scala giù senza porta con la scala aperta, **D12** l'interruttore all'altare, spento di default (non sempre acceso, non solo gli
oggetti), **D15** nel salvataggio, formato 11. Il disegno (24.1) resta da approvare prima del codice, e il nome del boss («Vorrak il Custode») è ancora una proposta.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M24 "Nemici più vivi"**, tag **`v1.12.0`**. Dentro: la fuga, le ronde, il boss della 4, il mondo che cresce | Cambia come si combatte e come si gioca dopo la vittoria, non il mondo: versione minore |
| D2 | **Chi scappa** | **Scheletri e sciame normali** (grado `Normal`). **Non** scappano il bruto (è già telegrafato e ha la sua pausa), il mago (tiene la distanza), i campioni, gli unici e i re | Il piano del 18 ott: «lo sciame e gli scheletri semplici». Un campione che fugge e torna cura la sfida invece di cambiarla |
| D3 | **Quando scatta** | Con **vita sotto il 25%** (scheletro) o **il 30%** (sciame, che ha poca vita), **oppure** quando **due compagni dello stesso tipo muoiono entro 3 s e 8 m** (un'osservazione nuova sulle morti, che `EnemyPack` non fa). **Una sola volta per vita** (un nemico guarito a casa può rifarlo). Le soglie sono **dati dell'archetipo** (`FleeBelow`, `FleeOnLosses`) | Una sola fuga lo rende un momento, non un modo di non morire mai; il lutto dei compagni dà allo sciame la sua paura |
| D4 | **Come scappa** | Per **4 s** (`FleeDuration`) corre a **×1,25** la sua velocità verso un punto sul NavMesh **lontano almeno 10 m dal cavaliere** e **entro 15 m da casa**, scelto dalla direzione opposta; non prende le scale né esce dalla stanza del livello. Non si cura. Se non trova un punto che lo allontani, **combatte** (l'angolo non è una fuga) | Non deve poter fuggire oltre il limite dell'ADR-050 (si curerebbe), e deve potersi ancora colpire: 4 s a ×1,25 il cavaliere lo raggiunge con gli incantesimi o correndo |
| D5 | **Quando torna** | Finiti i 4 s, **torna all'inseguimento** (non fermo), e **chiama i compagni** dello stesso tipo entro `FleeCallRadius` (dato dell'archetipo: il raggio del branco per lo sciame, 10 m per gli scheletri, che oggi non hanno un branco) con `Alert`: uno scheletro che scappa e torna con altri due è la scena voluta. Se il cavaliere è sparito (morto o lontano oltre il limite) torna a casa come sempre | Il piano dice «torna a combattere o chiama i compagni»: le due cose insieme sono più utili da vedere che una sola |
| D6 | **Cosa si vede e si sente** | La corsa è l'animazione di inseguimento; una **riga** di verso diverso all'inizio della fuga (un suono dello stesso archetipo, a tono alto); niente barra né testo | Il giocatore capisce da solo, e non si aggiunge un asset |
| D7 | **Ronde** | **Un nemico normale su quattro** (`PatrolShare` 0,25 sui dati degli archetipi, scheletro e bruto; **non** sciame, mago, speciali, re) gira tra **2 o 3 punti entro 8 m da casa**, scelti sul NavMesh dall'**hash della cella e della profondità** (**nessun tiro del seme**: i livelli, gli arredi e il loot dei semi provati non cambiano), a **60%** della velocità di inseguimento, con **una pausa di 2–4 s** a ogni punto. Vedendo il cavaliere insegue come gli altri; oltre il limite o a inseguimento finito **torna alla ronda**, non fermo | I tester (18 ott): «aspettano fermi». Lo sciame ha già il suo comportamento di branco; gli speciali stanno vicino alla loro scorta |
| D8 | **Il boss della 4: chi è** | Un **unico più grande della cripta**, con un nome suo: proposta **Vorrak il Custode** (il nome lo decide Mirco), il modello di scheletro del re **con un'armatura diversa e un'aura** (stesso trucco di Morvak e Grumm: nessun modello nuovo). **Due fasi**: mischia con un **colpo caricato** (settore a terra, come il bruto, più largo e più forte) sopra il 50%, e **sotto il 50% evoca tre scheletri** e **carica in linea retta** contro il cavaliere. Barra in alto come il re, con il nome. **Da scegliere:** o questo, o un re più corto in tutto (stessa regia del re, solo con numeri minori) | Il piano lo propone «un unico più grande, con una sala sua e un tesoro suo, sul modello del re»: due fasi bastano per una tappa, il re ne ha tre per la fine |
| D9 | **La sua sala** | La **stanza della scala giù** della profondità 4 (la più lontana dall'ingresso, `FarthestRoom`: il generatore la sceglie sempre): il boss nasce al centro, **senza porta che si chiude**; la **scala resta aperta**: chi vuole lo salta (e il boss torna a casa e si cura, ADR-050). La scala è davanti a lui, quindi saltarlo vuol dire passargli accanto. **Da scegliere:** o così, o la porta chiusa come il re (la sala deve avere **una sola entrata**: servirebbe un tipo di stanza nuovo del generatore) | Una porta vuole un tipo di stanza nuovo e cambierebbe le mappe di tutti i semi: la sala che c'è non cambia niente. Il rischio è che si salti il boss: lo si prova |
| D10 | **I suoi numeri** | Vita e danno **da tabella nell'asset** del boss (`Data/Enemies/…`), come il re: partono a **8× la vita di uno scheletro** e **il danno del bruto ×1,3**, poi si tarano con la misura del 24.4 contro il cavaliere e il Mago di riferimento **a ogni difficoltà** (la difficoltà li moltiplica come per gli altri). Il re della 8 **non cambia** | Il boss della metà deve essere uno scontro duro ma non peggio del re: la misura lo dirà con i colpi per ucciderlo |
| D11 | **Il tesoro** | Quando cade: il **tesoro del Custode**, una cassa (`LootTable` sua, tra la cassa e il tesoro del re: 3 oggetti, rarità spostate verso magico e raro, **un unico** possibile dalla Tenebra in su), nella sua sala. Niente portale: la scala è già lì | Il re ha il portale della vittoria; la tappa della metà ha il suo premio, e il loot non cambia per i semi provati (un dominio del seme suo) |
| D12 | **«Il mondo cresce con te»** | **Un interruttore all'altare** delle difficoltà (spento di default), **per mondo**: acceso, il **livello efficace** di una profondità è `max(livello della zona, livello del cavaliere − 3)`, **e** i nemici ne seguono la distanza: **vita e danno ×(1 + 0,04 per livello oltre la zona)** (`OverLevelScaling` in `ProgressionSettings`, come il `_overLevelPenalty` dell'esperienza, che resta). Gli **oggetti** prendono lo stesso livello efficace (`ItemLevel`), l'**esperienza** usa il livello efficace al posto della zona (il calo oltre la zona resta, ma parte dal margine di 3 livelli). **Da scegliere:** (a) così, con l'interruttore; (b) sempre acceso (come Diablo 3, e la misura va rifatta); (c) solo gli oggetti, i nemici fermi | Il piano chiede la regola e avverte che, sempre accesa, «salire di livello non si sente più». Spenta, i numeri della misura non si muovono: è il solo modo di non rifare tutta la M14 per una regola che a qualcuno non piace |
| D13 | **Il margine e la curva** | **Margine 3 livelli** (un cavaliere di livello 20 alla 1 trova oggetti da 17 e nemici ×1,64 al massimo), **curva lineare 0,04 per livello**, con un **tetto ×3** per i valori che i dati futuri potessero spingere oltre (il massimo di oggi è un cavaliere al 50 alla profondità 1: livello 47 contro la zona 1, ×(1+0,04×46) = 2,84, già sotto il tetto). Tutto in `ProgressionSettings`, quindi tarabile senza codice | Una curva lineare è la più facile da leggere nelle fasce, e il tetto tiene il caso peggiore sotto il ×3 |
| D14 | **Che cosa non cresce** | **Boss e re** (hanno la loro taratura), **i campioni e gli unici** crescono come i nemici normali (il grado si applica sopra), **l'oro** no, **la difficoltà** resta com'è e si moltiplica sopra | Il re alla 8 è già il tetto della cripta: farlo crescere con il cavaliere sposta tutta la M14 |
| D15 | **Dove si ricorda** | **Nel salvataggio, formato 11**: l'interruttore è del **mondo** (cambia le difficoltà e il loot, come la difficoltà e il seme), non del computer. **Migrazione:** da 10 a 11 con l'interruttore **spento**. **Da scegliere:** o così, o una preferenza del computer (`PlayerPrefs`: nessun formato nuovo, ma vale per tutti i personaggi e i salvataggi) | La difficoltà e il seme del mondo sono già nel salvataggio; un interruttore del mondo vi appartiene. Il costo è una fixture `save_v10.json` con il suo test, come per il 10 |
| D16 | **Il boss e i ricordi** | Il Custode **non rinasce** una volta battuto: lo dicono i ricordi del livello (ADR-079) e `RunStats` (**da verificare**: se il ricordo non basta per un livello rigenerato dal cavaliere, serve un campo nel salvataggio 11, in più dell'interruttore). Cambiare il mondo (difficoltà o «rifai il mondo») lo fa rinascere | Il re fa lo stesso (`BossDefeated`); lo stesso comportamento per il Custode dà una regola sola |
| D17 | **Cosa NON cambia** | La macchina a stati dei tipi che non scappano né girano (bruto, mago, speciali, re); le **mappe e i semi** (le ronde usano un hash, il boss è un posto della stanza che c'è, niente tiri nuovi: nessun test fisso si muove); le difficoltà e la misura del cavaliere e del Mago **con l'interruttore spento**; la camera, i comandi, gli alberi, i filmati, la vittoria dal re | Come per la M22 e la M23: tutto ciò che non è il tema resta fermo, e un test lo dice |
| D18 | **Si controlla** | **EditMode:** `FleeState` e `PatrolState` con un corpo finto (scatta alla soglia, una sola volta, il lutto dei compagni, torna all'inseguimento e chiama il branco, l'angolo non è una fuga, la ronda segue i punti e alla vista passa all'inseguimento, torna alla ronda), i punti dell'hash (stessa cella e profondità, stessi punti), `BossPhases` della cripta, il livello efficace e la curva (soglie e tetto), la migrazione 10 → 11, **la misura con l'interruttore spento uguale a prima** (`WarriorReference_KeepsItsNumbers`) e con l'interruttore acceso su qualche livello del cavaliere (fasce nuove). **PlayMode:** uno scheletro ferito scappa e torna, un nemico di ronda gira e vede il cavaliere, il Custode combatte e cade (le due fasi, il tesoro), la scala resta aperta, l'interruttore all'altare cambia il loot e i colpi per ucciderlo. **Ronda:** il Custode e il suo tesoro si raggiungono (profondità 4, ogni notte), i punti di ronda stanno sul NavMesh. **Foto:** il boss e la sua sala, uno scheletro in fuga, l'altare con l'interruttore | Come per le altre milestone |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 24.1 | **Il disegno e le soglie**: una pagina con il **Custode** (modello con aura, sala, barra, le due fasi, il tesoro), l'**interruttore** sull'altare (le due lingue), una tabella con le **soglie di fuga e le ronde per tipo** (D3, D4, D7), e una tabella della **curva** del mondo che cresce a quattro livelli (1, 10, 25, 50) e quattro profondità; da approvare **prima del codice** | 1–2 |
| 24.2 | **La fuga**: il corpo guadagna `Health` e `MoveTo(punto)`/`IsAtDestination` (`IEnemyBody`), `FleeState`, i dati dell'archetipo (`FleeBelow`, `FleeOnLosses`, `FleeDuration`), il lutto dei compagni (un'osservazione di `EnemyPack`), il verso, i test in EditMode con il corpo finto e in PlayMode (D2–D6) | 3–4 |
| 24.3 | **Le ronde**: `PatrolState`, i punti dall'hash (`PatrolPoints`), `PatrolShare`, la velocità, l'interruttore spento nei test (`PatrolEnabled`), `Return` che torna alla ronda; la ronda notturna controlla i punti sul NavMesh (D7, D17) | 3–4 |
| 24.4 | **Il Custode**: l'asset del boss, il modello con aura, la sala della scala giù (il posto del boss e del tesoro nella cripta, senza cambiare le mappe), la barra, le due fasi con la carica (`BossController` si riusa dove regge, altrimenti si estrae), il tesoro e la sua `LootTable`, il ricordo; la misura contro il cavaliere e il Mago di riferimento a ogni difficoltà; la ronda controlla che si arrivi alla sala e al tesoro (D8–D11, D16) | 6–9 |
| 24.5 | **Il mondo che cresce**: `ProgressionSettings` (margine, curva, tetto), il livello efficace nel `LootRoller` e nell'esperienza, la vita e il danno dei nemici (`ApplyDifficulty`), il salvataggio **11** con la sua fixture e la migrazione, l'interruttore all'altare con le sue stringhe, la misura nuova con l'interruttore acceso (D12–D15) | 6–8 |
| 24.6 | **Misura e chiusura**: le ADR (146…), le lezioni, il piano, il README (i *Comandi* non cambiano; lo *Stato* sì), `bundleVersion` 1.12.0, *Da provare*, il benchmark dei livelli e di una scena con 30 nemici di ronda, il tag `v1.12.0` dopo i Tests verdi | 2–3 |

**Stima: 21–30 h**, con il disegno e la chiusura. Se serve tagliare, in quest'ordine: il verso della fuga (D6, resta l'animazione); la carica del Custode (resta il colpo caricato, due fasi con le sole evocazioni); l'interruttore all'altare e il suo formato 11 (si rimanda la crescita del mondo a una milestone sua, come la M20 ha fatto con l'albero: è la parte più grossa e quella che vuole più prove); la fuga per il sciame (restano gli scheletri).

---

## Rischi da tenere d'occhio

- **I test che contano sui nemici fermi.** Percorsi, scale, portali, barre e la ronda notturna camminano per livelli con nemici fermi. Le ronde sono accese **solo nel gioco**: `SandboxFixture` le spegne come i suggerimenti e le barre (D17), e i test delle ronde le accendono. Si controlla in 24.3 con la PlayMode intera.
- **Una fuga che cura.** `Return` riporta la vita piena a casa (ADR-050): una fuga che oltrepassasse il limite lo farebbe. Il punto di fuga sta **entro 15 m da casa**, e un test lo prova con un nemico a 18 m (D4).
- **Le ronde e il portale.** Un nemico che gira può trovarsi sul punto del portale o davanti a una cassa quando la ronda notturna li prova: le prove di raggiungibilità spengono già i nemici (`SetActive(false)`), e la misura del portale non cambia.
- **Il Custode sulla scala.** La sala è quella della scala giù; il boss nasce al centro, la scala è sul bordo. Se il generatore mette la scala in una stanza molto piccola (4 × 4), il boss e la sua scorta stanno addosso alla scala: **un controllo di mappa** (la ronda del 24.4) prova la distanza e il raggio del settore, e una stanza piccola sposta il boss al posto più lontano dalla scala.
- **Il livello del Custode nelle misure.** La misura del cavaliere a profondità 4 con il Custode vivo non è quella di sempre: la nuova riga della misura (un giro contro il boss per difficoltà) ha le sue fasce, e le vecchie non si toccano.
- **Il mondo che cresce rompe le fasce.** Con l'interruttore acceso un cavaliere di livello 50 alla 1 incontra nemici ×2,84: la misura lo deve dire con una fascia per l'interruttore acceso (D18), e il tetto tiene il caso peggiore sotto il ×3; l'interruttore spento non deve cambiare un numero (`WarriorReference_KeepsItsNumbers`).
- **Il salvataggio 11.** Una fixture `save_v10.json` si congela **prima** della migrazione (lezione della M22): il test che la legge la legge senza migrare e poi migrata.
- **Le ore.** Quattro cose in una milestone sono molte: se il 24.4 o il 24.5 si allunga, si taglia come scritto sopra, non si comprime il resto.
- **Il rumore.** Una fuga si sente: lo stesso verso da dieci scheletri contemporaneamente è un coro; `_sfxLimiter` (già in `CharacterAudio`) lo tiene in ordine, e si guarda nella foto-video della fuga.

---

## Passo 24.1 — Il disegno

**Com'è andata (5 nov 2026).** Il foglio `Recordings/Photos/m24_disegno/disegno.html` (fuori da git, una pagina sola con le immagini dentro) è montato su foto vere
del gioco: la stanza della scala giù della profondità 4 (`PhotoTour.BossDesign`, che mette un **Custode finto**: il modello del re senza i suoi componenti, tinto di
viola e con una luce) e la schermata delle difficoltà dell'altare (`05_altar`). La barra, le note e l'altare con la riga nuova sono disegnati sopra. Cosa mostra:

- **Il Custode (D8–D11).** La stanza com'è e con lui al centro, la barra in alto con il nome in oro e il segno bianco a metà vita (dove cambia fase), le due fasi (colpo
  caricato; evocazione di tre scheletri e carica in linea retta), i numeri di partenza e il tesoro. Il modello del re **galleggia**, e il Custode con lui: è la sua natura,
  e non serve un modello nuovo.
- **L'interruttore (D12, D15).** La riga **«Il mondo cresce con te»** tra l'elenco delle difficoltà e *Rifai il mondo*, con un pulsante *No/Sì* e il suggerimento al passaggio,
  in inglese (spento) e in italiano (acceso). Il riquadro cresce di una riga.
- **Fuga e ronde (D2–D7).** Una tabella per tipo (chi scappa, a che soglia, chi gira) e due sequenze: la fuga (combatte, scatta una sola volta, scappa 4 s, torna e chiama
  i compagni) e la ronda (2–3 punti da un hash, pausa 2–4 s, vede il cavaliere, oltre il limite torna alla ronda).
- **La curva (D13).** Una tabella 4 × 4 (cavaliere di livello 1, 10, 25, 50; profondità 1, 3, 5, 8) con il moltiplicatore di vita e danno e il livello degli oggetti, al
  Crepuscolo: da ×1,00 (livello 1) a ×2,84 (livello 50 alla profondità 1, oggetti di livello 47). È una curva su carta, da misurare in 24.5.

**Da approvare:** il Custode come nella foto, il nome, la riga all'altare, le soglie (25% scheletro, 30% sciame, 4 s, un nemico su quattro di ronda) e la curva (margine 3,
4% per livello, tetto ×3).

**Approvato da Mirco il 5 novembre 2026:** tutti e cinque i punti, compreso il nome **Vorrak il Custode**.

## Passo 24.2 — La fuga

**Fatto, in locale.** Gli scheletri normali e lo sciame scappano una sola volta per vita (D2–D6), e lo stato è nel nucleo, quindi si prova senza scene.

- **Lo stato.** `FleeState` e l'interfaccia `IFleeBody` (`WantsToFlee`, `BeginFlee`, `IsAtDestination`, `EndFlee`) accanto a `IEnemyBody`: un corpo che non la implementa non scappa mai.
  `Chase` e `Attack` passano a `Flee` se il corpo lo vuole (in `Attack` prima di chiedere un altro colpo). Dopo i secondi della fuga torna a `Chase`; se il cavaliere è morto, a `Idle`.
- **Il corpo.** `EnemyAI` sceglie il punto: dritto all'indietro dal cavaliere, poi sempre più di lato (0, ±30, ±60, ±90 gradi, +0/2/4 m), a almeno 10 m dal cavaliere, al più
  15 m da casa e raggiungibile in linea retta sulla NavMesh. Senza un punto non è una fuga: combatte e non riprova per un secondo. Va a ×1,25 e a fine fuga torna alla velocità
  di prima, anche se nel frattempo era rallentato. Non si cura: il limite di 15 m sta sotto la catena (20/30), quindi non scatta mai il ritorno a casa.
- **Il lutto.** `FleeWatch` (uno per livello, come `EnemyPack`) ricorda le morti: due compagni dello stesso tipo morti entro 8 m e 3 s fanno scappare anche chi sta bene. A fine fuga
  chiama i compagni dello stesso tipo entro 10 m (12 m lo sciame), che accorrono.
- **Il grido.** `CharacterAudio.PlayFleeCry`: il verso di dolore del tipo a ×1,35, senza un suono nuovo.
- **I dati.** Scheletro e sciame hanno i campi nuovi nell'archetipo (soglia 25% e 30%, 2 perdite, 4 s, ×1,25, 10 m, 15 m); bruto, mago, campioni, unici e re non scappano.
- **I test.** `EnemyBehaviours.FleeEnabled` è spento in `SandboxFixture.ForgetPreferences` (D17): i test che contano su nemici fermi non cambiano. Solo i sei test della fuga lo accendono.
  EditMode 685 (11 nuovi: lo stato con un corpo finto, i dati, i numeri), PlayMode 390 su 392 verdi (2 saltati come prima; 6 nuovi: scappa sotto il 25% e si allontana, torna e non
  scappa due volte, interruttore spento, due compagni morti, uno solo non basta, la chiamata a fine fuga).

## Passo 24.3 — Le ronde

**Fatto, in locale.** Un nemico normale su quattro (scheletro e bruto) gira tra due o tre punti vicino a casa invece di aspettare fermo (D7).

- **Il piano.** `PatrolPlan.For(profondità, cella x, cella z, quota, raggio)` ricava da un hash della cella e della profondità (`SeedMixer.ForPatrol`, un dominio suo, senza il seme del
  mondo) se il nemico gira, quanti punti ha (2 o 3), dove stanno rispetto a casa (angolo e distanza, 3–8 m) e quanto sta fermo a ciascuno (2–4 s). Nessun tiro del seme: le mappe, gli
  arredi e il loot dei semi provati non si spostano, e la stessa cella ha sempre la stessa ronda. I numeri si prendono sempre tutti e nello stesso ordine, quindi cambiare la quota non
  sposta i punti di chi già girava.
- **Lo stato.** `PatrolState` e `IPatrolBody` nel nucleo (come la fuga): sta fermo la pausa, va al punto dopo a 60% del passo, e intanto guarda attorno come chi è fermo; se vede il
  cavaliere insegue. Un punto che non si raggiunge entro 12 s (un compagno sulla strada) vale come raggiunto. `IdleState` manda subito alla ronda chi la fa: così **dopo l'inseguimento o
  il ritorno a casa riprende la ronda, non resta fermo**. `EnemyBrain.Alert` sveglia anche chi è di ronda. Novità dell'`EnemyStateBase`: un `Exit` chiamato a ogni cambio di stato
  (anche la morte e il riposo), che qui rimette il passo.
- **Il corpo.** `EnemyAI` trasforma il piano in punti sul NavMesh la prima volta che servono (il grado di campione o di unico arriva dopo `Awake`): ognuno entro 8 m, raggiungibile in
  linea retta da casa, o a metà strada; con meno di due punti non gira. Campioni, unici, re, sciame e mago non girano, né un evocato (`CompositionRoot` lega la profondità solo ai nemici
  del livello, con `BindPatrol`).
- **Una correzione che viene dalla fuga.** Fuga e ronda cambiano il passo dell'agente; se è rallentato dal Mago il rallentamento teneva la vecchia base e si rallentava due volte alla
  fine. Ora c'è un solo `ScaleSpeed` che muove insieme passo, base e velocità attesa. Un test lo prova di ronda.
- **I test.** `EnemyBehaviours.PatrolEnabled` è spento in `SandboxFixture.ForgetPreferences` (D17). EditMode 699 (14 nuovi: lo stato con un corpo finto, il piano, i dati), PlayMode 397 su 399 verdi (2 saltati come prima): 6 test
  (gira entro 8 m a 60%, vede il cavaliere e insegue a passo pieno, senza interruttore né profondità resta fermo, una cella senza ronda resta ferma, dopo il ritorno a casa riprende,
  rallentato non due volte) più uno nei livelli veri (crypt e caverne, sei profondità): **14 nemici su 57 candidati girano (24,6%)**, tutti con punti sul NavMesh e entro 8 m.

## Passo 24.4 — Vorrak il Custode

**Fatto, in locale.** Il boss della profondità 4, come approvato in 24.1 (D8–D11, D16), con quello che il codice ha insegnato.

- **Il posto.** `DungeonPopulator.PlaceGuardian`, per ultimo e con un flusso suo del seme (`SeedMixer.ForGuardian`): nessun tiro del flusso principale, quindi nemici, casse e barili dei semi provati
  non si spostano; alla profondità 4 la cripta ha in più due segni, **`K`** (il Custode, lo stesso del re delle caverne) e **`k`** (il suo tesoro; la `t` del tesoro del re nella cripta c'era già).
  Il Custode sta sulla cella libera più vicina al centro della **sala della scala giù**, il tesoro contro un muro della stessa sala. Le stanze della cripta vanno da 3 a 7 celle per lato (la sala della
  scala è 3×3 una volta su otto, e le celle sono di 4 m, quindi anche quella è un'arena di 12 m), e ci sono già arredi e nemici: in una sala piena il Custode prende il posto di un arredo o di
  un mobile di legno, mai di un nemico, di un barile o di una cassa (i conteggi della misura sono esatti), e il tesoro prende una cella contro un muro anche accanto a una porta, purché ogni
  cella resti raggiungibile e davanti alla cassa si arrivi. **In 397 semi su 400 sta nella sala della scala**; nei tre con una sala di 3×3 piena sta nella stanza più vicina alla scala che lo regge
  (il test fissa il 97%). Le mappe dei semi provati cambiano solo per quei due segni alla profondità 4.
- **Il controllo.** `GuardianController` accanto al `BossController` del re, che non si tocca: niente porta, anello né portale della vittoria. Sopra la metà della vita combatte con il colpo caricato
  del bruto (largo e forte); sotto, **una volta sola** evoca tre scheletri (legati al cavaliere senza esperienza, come quelli del re) e comincia a **caricare**: striscia rossa a terra per 1,1 s
  nella direzione in cui è il cavaliere in quel momento (`TelegraphLine`), corsa a 13 m/s fino a 12 m o a un muro o al cavaliere, colpo se il cavaliere è sulla linea, poi 1,6 s fermo (la
  finestra per colpirlo); ogni 8 s. Lo stordimento e il gelo lo fermano e annullano la carica. Il cavaliere che cade e torna in vita lo fa ricominciare dalla prima fase, con le evocazioni da rifare.
- **Il tesoro.** `Chest` ha un secondo sigillo, `OpensWithGuardian`: chiuso finché lui è vivo, si apre alla sua morte. Con il Custode già caduto (il ricordo del livello lo toglie prima di legare
  i nemici) si apre subito, e il tesoro del re non si apre più per sbaglio quando il re è già caduto: ha il suo sigillo. Tre oggetti (due tiri in più), magici ×1,8 e rari ×2 (il re ×2 e ×3),
  almeno un magico, oro ×1,8, unici e set ×4 con un tiro a parte del 15%. Gli unici hanno il loro livello minimo: per questo "dalla Tenebra in su" non è una regola scritta ma quello che il livello
  degli oggetti permette.
- **La barra e il nome.** In alto per tutto lo scontro, il nome **Vorrak il Custode** (*Vorrak the Keeper*) in oro e un segno a metà, dove comincia la seconda fase; niente barra sopra la testa, come il re.
- **I dati.** `Tools → Custode`: `GuardianTools` crea in modo ripetibile i colpi (`GuardianStrike` 26–36 e `GuardianQuick` 10–16, il bruto per 1,3), l'archetipo, il prefab (il modello del re tinto di
  viola, scalato 1,25, con l'aura al posto della luce del re e senza i suoi componenti), il tesoro e i due segni nel set della cripta. Vita **240** (otto scheletri).
- **Fuori dalle misure tarate.** Il Custode e il suo tesoro non entrano in `LootCensus` né nell'esperienza di `DifficultyCensus`: sono un premio in più che il cavaliere può saltare, e le bande della M14
  e della M16 restano com'erano (D17). La colonna nuova della tabella (`colpi_custode`, `regge_custode`) li misura a parte.

**Misura (D10), alla profondità 4, ogni difficoltà.** Il Custode contro il Guerriero di riferimento: 14 colpi per ucciderlo al Crepuscolo (bruto 5,3, re 36), fino a 29 all'Abisso IV (bruto 10,9, re 73);
il cavaliere regge 4,5 suoi colpi al Crepuscolo e 3,1 all'Abisso IV (bruto 5,8 e 3,9, re 3,8 e 2,6). Il Mago di Fuoco: 16–27 lanci (re 40–68), regge 3,5–4,0 colpi (re 2,4–3,4). **È un terzo del re
per durata (0,33–0,40 a ogni difficoltà, per tutte e due le classi) e meno letale del re ovunque**: la ragione di tenere vita 240 e danno del bruto ×1,3. Se il collaudo di Mirco lo dice troppo morbido
o troppo duro, i numeri sono nei due asset di colpo e nella vita del prefab, e si tarano senza codice (poi `GuardianTools` li riscrive).

**Test.** EditMode (12 nuovi: le fasi, il posto su 400 semi, la scala raggiungibile, i dati, il prefab, il tesoro, il set), PlayMode (7: aspetta alla scala, la barra, le tre evocazioni una volta sola,
la carica in linea retta con la striscia e il colpo, la carica mancata, la morte con il tesoro e il ricordo, il cavaliere che cade). **Non ancora provato dal vivo:** come si vede e come si combatte
(il modello con l'aura, la striscia a terra) — foto e prova di Mirco in 24.6.

## Passo 24.5 — Il mondo cresce con te

**Fatto, in locale.** L'interruttore dell'altare e quello che fa (D12–D15), spento di default.

- **Il nucleo.** `ProgressionSettings` ha il margine (3), la curva (0,04 per livello) e il tetto (×3), tarabili nell'asset. `EffectiveLevel(profondità, livello, bonus, acceso)` è `max(zona, livello − 3)`,
  `EnemyGrowth` è `min(tetto, 1 + 0,04 × (livello efficace − zona))`; spento, uno è la zona e l'altro 1. `ExperienceForKill` ha un parametro in più: acceso, il calo oltre la zona parte dal livello
  efficace, cioè dal margine. `DifficultyProgress.GrowingWorld` tiene l'interruttore.
- **Il gioco.** `CompositionRoot` dà al `LootRoller` il livello efficace (la funzione legge il livello del cavaliere e l'interruttore a ogni tiro), moltiplica vita e danno di ogni nemico che si lega
  (`GrowthOf`, sopra la difficoltà e il grado) tranne **il re e il Custode**, e passa l'interruttore all'esperienza. Un nemico si lega una volta: l'interruttore vale **dal prossimo livello che si carica**,
  non cambia i nemici di un livello già aperto, e non fa un mondo nuovo (non chiede conferma).
- **Il salvataggio 11.** `_growingWorld` in `SaveData`; migrazione **10 → 11**: interruttore spento, **e i ricordi dei livelli e il punto del portale si azzerano** (il livello 4 ha il Custode, e i ricordi
  leggono nemici e casse nell'ordine in cui il livello li costruisce: come il 9 → 10 della M22). Fixture `save_v11.json` (con l'interruttore acceso) accanto a `save_v10.json`; difficoltà, classe,
  oggetti e mappa scoperta restano.
- **L'altare.** `GrowingWorldTools` (menu *UI → Interruttore del mondo che cresce*, idempotente) aggiunge a `DifficultyPanel` un pulsante tra l'elenco e «Rifai il mondo» (la finestra cresce di una riga), con
  «Il mondo cresce con te: No/Sì» e un suggerimento (`HoverShow`) che compare sotto la finestra al passaggio. Stringhe `altar.growing_off`, `altar.growing_on`, `altar.growing_hint`.

**Misura con l'interruttore acceso (D13, D18).** `DifficultyCensus.MeasureGrowing`: la griglia 4 × 4 della scheda (cavaliere di livello 1, 10, 25, 50; profondità 1, 3, 5, 8) a ogni difficoltà, oggetti al livello
efficace e bruto e scheletro con la crescita; il re non cresce. Guerriero (Armi) e Mago di Fuoco. Numeri al Crepuscolo, Guerriero, colpi al bruto da spento ad acceso (e colpi del bruto retti):

| cavaliere | profondità 1 | profondità 8 |
|---|---|---|
| livello 10 (oggetti 7) | 4,2 → 5,2 (regge 7,5 → 6,0), ×1,24 | 4,2 → 4,2, ×1,00 (zona 10) |
| livello 25 (oggetti 22) | 1,9 → 3,1 (regge 11,7 → 6,4), ×1,84 | 1,8 → 2,5 (regge 11,6 → 8,0), ×1,48 |
| livello 50 (oggetti 47) | 0,8 → 1,7 (regge 18,7 → 7,0), ×2,84 | 0,7 → 1,5 (regge 18,6 → 8,0), ×2,48 |

L'esperienza di un cavaliere di livello 50 alla 1 sale da 60 a 270 per livello (il calo parte dal margine). Il Mago dà numeri quasi uguali (livello 50 alla 1: 0,8 → 1,9).

**Che cosa dicono, senza addolcirlo.** La curva fa quello che la scheda scrive (×1,64 a livello 20, ×2,84 al massimo, sotto il tetto), ma **non basta a tenere un cavaliere molto più forte della zona dentro le
fasce di un cavaliere in pari**: a livello 25 il bruto muore ancora in 3 colpi, a livello 50 in 1,7. Quello che l'interruttore fa è dimezzare la distanza: il cavaliere non vince più in uno o due colpi senza
rischio (regge 6–8 colpi del bruto invece di 12–19) e ogni livello in più si sente ancora (5,2 → 3,1 → 1,7). Dall'Abisso I gli oggetti da 47 tengono il passo con la crescita e l'interruttore è quasi neutro
(a livello 50 alla 1 dell'Abisso I: 6,0 → 6,8 colpi; mai più facile del 15%). **Se Mirco vuole un mondo più duro**, è la pendenza (0,04 → 0,06) o il tetto (×3 → ×4) in `Progression.asset`, senza codice; il
tetto di ×3 basta fino a 0,043 per livello al 50.

**Test.** EditMode (13 nuovi tra cui: spento non cambia niente per ogni profondità, livello e bonus; la curva della scheda; il tetto; l'esperienza dal margine; le migrazioni 10 → 11 e il campione
dell'11; la misura spenta uguale a prima; le fasce nuove dell'interruttore acceso per Guerriero e Mago nelle celle in cui un cavaliere sta davvero; salire di livello si sente ancora). PlayMode (4 nuovi:
l'interruttore all'altare, il livello degli oggetti, la vita dei nemici ×1,64 e il Custode che non cresce, l'esperienza più alta). Corretti i test che chiedevano il formato 10.

## Passo 24.6 — Misura e chiusura

**Fatto.** Le foto, il costo, la chiusura. La scheda e il piano dicono com'è finita, con quello che le foto hanno trovato.

- **Le foto** (`PhotoTour.Keeper`, `Flee`, `GrowingAltar`, in `Recordings/Photos`): il Custode vero nella sala della scala (aura viola, barra con il nome in oro e il segno a metà, il tesoro contro il muro), le tre evocazioni, la carica con la striscia rossa; uno scheletro ferito che si allontana; l'altare con l'interruttore, spento e acceso, con il suggerimento in inglese e in italiano.
- **Che cosa hanno trovato** (nessun test lo diceva):
  - la **striscia della carica attraversava il muro** e andava per 12 m anche dove lo scatto si ferma al muro: ora la lunghezza è la corsa fino al primo bordo del NavMesh;
  - il **pannello dell'altare**, con la riga nuova, arriva al bordo dello schermo a 1080p: il suggerimento non ha posto sotto, e sta a **destra** della finestra, all'altezza del pulsante;
  - la voce «Riassegna le abilità» dell'altare **non cambiava lingua** insieme alle altre (si riscriveva solo all'apertura): ora si riscrive a ogni cambio di lingua.
- **Il costo** (`PhotoTour.BehavioursCost`, in editor): i nemici più vicini al cavaliere in una cripta alla 3, **12–14 invece dei 30 della scheda** (quanti ne ha attivi un livello), tutti attivi con la fuga e le ronde accese contro tutti fermi: main thread 2,72–2,75 ms contro 2,72–2,73, stesse allocazioni (9535 B a frame) e stessi batch (422). Dentro il rumore; la prova vera è la build di Mirco (*Da provare*).
- **I test.** EditMode 724 (+50 dalla M23), PlayMode 410, 408 verdi e 2 saltati come prima (+26). Corretti il test delle scale (apriva la prima cassa, che alla 4 è quella sigillata del Custode) e quelli del formato 10.
- **La chiusura.** ADR-146…148, lezioni e piano v2.79, il README (*Stato*), `bundleVersion` 1.12.0, le prove in build in `docs/Da_provare.md`.

**Chiusa il 6 novembre 2026.** Tag `v1.12.0` su e941830: Tests 42214251426 e build 42216532781 verdi, artefatto `DarkDescent-Windows-v1.12.0` di 61,36 MB (+0,01 sulla v1.11.0).
