# M23 — "A portata di mano"

**Cosa deve succedere (Definition of Done):** il cavaliere (e il mago) si muove anche con **WASD** (e le frecce),
con il mouse libero per colpire, raccogliere e lanciare; i nemici **feriti** hanno una **barra della vita sopra la
testa**, se il giocatore la vuole (opzione, spenta di default); chi comincia trova **suggerimenti brevi la prima
volta che servono** (il primo nemico, il primo oggetto a terra, la prima pozione...), e un **pannello di aiuto** con
tutti i comandi, da **F1** o dal menu di pausa, in italiano e in inglese.

- WASD segue la camera: **W va verso l'alto dello schermo**, qualunque sia la rotazione (Z e X, ADR-052). Due tasti
  insieme fanno la diagonale. I muri e gli ostacoli fermano il cavaliere come col click; i nemici lo trovano come prima.
- Il click non cambia: chi gioca solo col mouse non nota niente. Un tasto di movimento premuto prende il posto del
  percorso del click; a tasti fermi il click riprende com'è.
- Con i tasti premuti, un click su un nemico a portata lo colpisce **senza spostarsi**; un click sul pavimento non fa
  muovere altrove.
- Le barre stanno in un solo canvas e vengono da un pool; si aggiornano sugli eventi della vita, non a ogni frame.
- I suggerimenti non fermano il gioco, non sono un livello a parte, ognuno compare **una volta sola** e si ricorda tra
  le sessioni. Un'opzione li spegne tutti o li fa ripartire.
- Salvataggio e livelli **non cambiano**: restano il formato 10 e i semi di prima.
- Tag **`v1.11.0`**.

**Fuori da questa milestone:** cambiare i tasti dal menu; il gamepad; un livello di tutorial; la barra sopra il re
(ha la sua); nuovi comandi (corsa, schivata, mira libera); cambiare i tasti già usati (I, C, N, K, M, T, Q, E, Z, X,
1–8, Alt, Esc); cambiare la barra in alto al centro (M7, M9), che resta.

---

## Stato verificato il 4 novembre 2026

Verificato dopo la chiusura della M22 (tag `v1.10.0`, build 41870576525 verde). Il piano scritto il 9 e 18 ottobre
ha **due cose non più vere**, corrette qui: **Q ed E non girano la camera** (sono due caselle di abilità; la camera
gira con **Z e X**, README), e **le pergamene non ci sono più** (le ha tolte la M20), quindi niente suggerimento
«prima pergamena».

| Cosa | Com'è oggi |
|---|---|
| Il movimento | Solo col mouse. `PlayerInputReader` ha l'azione `Move` (tasto sinistro, `IsMoveCommandHeld`) e `Point` (la posizione); `PlayerController` fa il raggio sotto il cursore e chiama `PlayerMotor.MoveTo(punto)`, che usa il `NavMeshAgent` (`SamplePosition` entro 1 m, nuova destinazione solo oltre 0,2 m). Tenendo premuto, il comando si rinnova ogni 0,1 s (`_holdRepathInterval`): il cavaliere segue il cursore. `Walk` cancella il bersaglio d'attacco prima di muoversi |
| Il colpo in corso | `IsBusy` (colpo in corso o interrotto): il movimento chiesto in quel momento va in coda (`_queuedMove`) e parte a colpo finito. Un movimento da tastiera deve rispettare la stessa regola |
| Attaccare | `MeleeAttack.SetTarget(bersaglio)` fa avvicinare e colpire; ha `IsTargetInRange`, `HasTarget`, `Reach` (arma o dardo). Non c'è un «colpisci da fermo»: va aggiunto per il click con i tasti premuti |
| I tasti usati | `Gameplay` ha: `I` inventario, `C` personaggio, `1`–`8` cintura, `M` automappa, rotella zoom, **`Z`/`X` ruotano**, tasto destro lancia, **`Q`/`E` abilità**, `Alt` nomi a terra, `Esc` pausa, `T` portale, `K` abilità, `N` talenti. **Liberi:** `W A S D`, le frecce, `F1`, `H` |
| Camera | Gira a scatti di 90° (`ViewRotated`, ADR-052): la direzione di W si calcola dalla rotazione della camera **ogni frame**, non una volta |
| La barra dei nemici | `EnemyBar` (M7/M9): in alto al centro, per il nemico sotto il cursore o l'ultimo colpito (3 s), con nome, vita, nome nel colore del grado e riga dei modificatori per campioni e unici. Ascolta `Health.Damaged`/`Died` di ogni nemico (`_tracked`). Resta com'è |
| La vita | `Health` ha `HealthChanged(corrente, massima)`, `Damaged`, `Healed`, `Died`, `Revived`, `Current`, `Max`: bastano per sapere quando un nemico è ferito, senza controlli a ogni frame |
| Un precedente di pool | `DamageNumbers` (M9) prende numeri da un pool e li anima: stessa idea per le barre |
| Le preferenze | `PlayerPrefs` (lingua, volumi, nomi sempre visibili, numeri delle sfere, filtro retrò, zoom). `OptionsPanel` ha le sue costanti `*Preference` e dei `Toggle`; una nuova opzione è una costante, un toggle e una chiave di testo |
| Il testo | `TextKeys` e `Data/Localization/Strings.csv` (718 righe, italiano e inglese); un test controlla le chiavi. Nessun suggerimento né pannello di aiuto: i comandi stanno **solo nel README** |
| La pausa | `PauseMenu` ha *Riprendi*, *Opzioni*, *Esci*; `Esc` chiude le finestre aperte e poi mette in pausa |
| Eventi per i suggerimenti | Ci sono: `Health` dei nemici, `Interactable.Used`/`HighlightChanged`, `PlayerProgress.LevelUpShown`, il portale e le scale (`LevelExit`), la cintura. **Da verificare in 23.4** quelli che mancano (nemico «visto», oggetto a terra comparso, punto talento) |
| I test | `PlayerController` e `PlayerInputReader` hanno test PlayMode con l'`InputTestFixture`; le barre in alto e i numeri danno test per il canvas |

---

## Decisioni

**Confermate da Mirco il 4 novembre 2026:** tutte, D1–D16, senza tagli. Il disegno (23.1) resta da approvare prima del codice.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M23 "A portata di mano"**, tag **`v1.11.0`**. Dentro: WASD, le barre sui nemici feriti, i suggerimenti, il pannello di aiuto | Cambia come si gioca (si cammina da tastiera) ma non il mondo: versione minore |
| D2 | **WASD e frecce** | **W A S D** e le **frecce** muovono; tenendoli premuti si cammina, rilasciandoli ci si ferma. Nessun cambio di tasti dal menu (fuori dalla milestone) | Le frecce costano una riga di binding e aiutano chi ha la tastiera in un'altra disposizione |
| D3 | **Direzioni** | Relative alla **camera**, ogni frame: W = in alto sullo schermo, S = in basso, A = a sinistra, D = a destra. Due tasti = diagonale, normalizzata (non più veloce) | Con Z/X il «nord» cambia a ogni scatto; il giocatore ragiona sullo schermo |
| D4 | **Come si muove** | Ogni frame con un tasto premuto, `PlayerMotor` riceve **una destinazione 1,5 m avanti** nella direzione dei tasti (`MoveTo`, con `SamplePosition`): resta il `NavMeshAgent`, muri e ostacoli fermano come col click, animazioni e passi seguono la velocità dell'agent. In alternativa `NavMeshAgent.Move`: si prova in 23.2 e si sceglie con una misura (angoli, animazioni), non a occhio | Non cambia niente di quello che già funziona; la destinazione vicina dà curve morbide |
| D5 | **Convivenza col click** | **Un tasto premuto ha la precedenza**: il percorso del click si cancella e il tasto sinistro tenuto non rinnova il comando finché i tasti sono premuti. A tasti fermi il click riprende come oggi, anche tenendolo premuto. Una pressione nata sopra la UI non diventa mai un movimento (come oggi) | Evita due comandi che litigano |
| D6 | **Colpire camminando** | Con un tasto di movimento premuto, un click su un **nemico a portata** lo colpisce **da fermo** (`IsTargetInRange`), senza avvicinarsi; fuori portata non fa niente. Un barile si tratta come un nemico. Un click su un oggetto da usare (cassa, scala) **funziona come sempre**: cammina e usa | È lo Shift di Diablo; un click su una scala o una cassa è già «voglio andare lì» |
| D7 | **Incantesimi** | Il tasto destro e le abilità (`Q`, `E`) mirano al cursore **anche camminando**; il mago lancia muovendosi, come col click | Per il mago camminare e lanciare insieme è la ragione di WASD |
| D8 | **Quando WASD non muove** | Con la **pausa**, i dialoghi (conferme), i banchi (`ShopPanel`), la creazione del personaggio (si scrive il nome), la morte e i filmati: **no**. Con inventario, personaggio, talenti e abilità aperti: **sì**, come in Diablo. Mai quando il campo di testo ha il fuoco | Una lettera scritta nel nome non deve muovere il cavaliere |
| D9 | **Barre sopra i nemici** | Opzione **«Barre della vita sui nemici»**, **spenta di default** (il piano del 8 ott). Accesa: ogni nemico **ferito** e vivo ha sopra la testa una barra piccola (rossa; dorata per i campioni, arancione per gli unici, nei colori dei gradi della M12); intatti, morti e il **re** (ha la sua barra) non ne hanno. La barra in alto al centro resta | Nello sciame se ne vedono solo quelle che servono |
| D10 | **Come sono fatte** | Un **solo canvas in world space**, barre prese da un **pool** (massimo 24, le più vicine al cavaliere), girate verso la camera. Si aggiornano su `Health.HealthChanged`/`Died`; la posizione segue il nemico solo per quelle mostrate. Niente `Update` per nemico, niente allocazioni | Lo sciame ha dieci nemici fitti: 10 canvas sarebbero 10 chiamate di disegno |
| D11 | **Suggerimenti: quali** | Dieci, ognuno col suo momento: **muoversi** (a inizio partita: click o WASD), **il primo nemico in vista**, **il primo oggetto a terra** (Alt per i nomi, click per raccoglierlo), **la prima pozione in cintura** (1–8 o click destro), **la prima scala**, **il primo punto attributo** (C), **il primo punto talento** (N), **il portale** (T, la prima volta che c'è il mana), **le abilità** (Q/E/destro, la prima volta che c'è una seconda abilità), **il borgo** (fabbro, mercante, guaritrice, la prima volta). Fuori: la pergamena (non c'è più) | I tester del 18 ott non conoscevano i comandi; sono i momenti in cui i comandi servono |
| D12 | **Suggerimenti: come** | Un **riquadro piccolo** in basso al centro, sopra la cintura: un titolo e una o due righe, **8 secondi** o finché si preme un tasto qualsiasi (non ruba il click), uno alla volta in coda, mai durante pausa, filmati, morte o sala del re. **Non ferma il gioco.** Il testo è nella tabella delle stringhe, nelle due lingue, e dice i tasti **veri** (se un giorno cambiano, il testo li legge dal binding, non dalla stringa) | Un riquadro che si chiude da solo non è un tutorial |
| D13 | **Suggerimenti: la memoria** | Quali sono già stati visti sta nelle **preferenze del computer** (`PlayerPrefs`), non nel salvataggio: sono del giocatore, non del cavaliere, e un nuovo personaggio non li ripete. Opzione **«Suggerimenti»** (accesa di default) e un pulsante **«Rivedi i suggerimenti»** che li azzera | Il piano del 18 ott diceva «si ricordano tra le preferenze» |
| D14 | **Il pannello di aiuto** | Si apre con **F1** (e si chiude con F1 o Esc) e dal menu di pausa con un pulsante **«Comandi»**. Quattro gruppi in due colonne: **Movimento** (click, WASD, tenere premuto, Z/X, rotella), **Combattimento** (click, tasto destro, Q/E, 1–8, T), **Finestre** (I, C, K, N, M, Esc) e **Vedere** (Alt, M). Italiano e inglese, dalla stessa tabella dei suggerimenti. Il testo cambia con la classe solo dove serve (il tasto sinistro del mago lancia il dardo) | I comandi oggi stanno solo nel README |
| D15 | **Cosa NON cambia** | I numeri del combattimento e delle difficoltà, la barra in alto al centro (M7/M9), i tasti già usati, le mappe e i semi (nessun test fisso si muove), il salvataggio (resta 10), gli alberi, i filmati, la camera. Il README e il piano si correggono per le due cose non vere (Q/E, pergamene) | I numeri non si ritarano per un tasto |
| D16 | **Si controlla** | **EditMode:** la direzione dei tasti rispetto alla camera (ognuno dei quattro scatti, le diagonali, normalizzata), la memoria dei suggerimenti (una volta sola, azzera, spenti), i testi in tutte e due le lingue per ogni suggerimento e per ogni riga del pannello, il tetto del pool delle barre. **PlayMode:** WASD muove nelle quattro rotazioni, un muro ferma, un tasto prende il posto del click e il click riprende, un click su un nemico a portata colpisce da fermo, fuori portata no, il mago lancia camminando, con la pausa o il campo del nome nessun movimento; le barre compaiono solo sui feriti, spariscono alla morte, restano al massimo 24, l'opzione le spegne; i suggerimenti compaiono una volta, in ordine, mai in pausa; F1 apre e chiude. **Foto:** la barra sullo sciame, il riquadro, il pannello nelle due lingue (`PhotoTour`). **Ronda:** invariata | Le foto sono la prova per chi gioca |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 23.1 | **Il disegno**: una pagina con la **barra sopra un nemico ferito** (sciame, campione, unico: i tre colori), il **riquadro del suggerimento** (due versioni: in basso al centro, o in alto a sinistra) e il **pannello di aiuto** (le due lingue), con le foto del gioco. Si va avanti **solo con il sì di Mirco** | 1–2 |
| 23.2 | **WASD**: l'azione `Move2D` e le frecce nel `PlayerControls`, la lettura nel `PlayerInputReader`, il ramo nel `PlayerController` (direzione dalla camera, precedenza al tasto, click da fermo sul nemico a portata, `IsBusy`, i casi di D8), `MeleeAttack` con l'attacco da fermo; la scelta tra destinazione e `Move` con la misura (D2–D8) | 4–5 |
| 23.3 | **Le barre sui nemici**: il canvas, il pool, i colori dei gradi, l'opzione e la sua chiave, i test e le foto sullo sciame (D9, D10) | 3–4 |
| 23.4 | **I suggerimenti**: il registro dei momenti, la memoria nelle preferenze, il riquadro, i dieci testi nelle due lingue, l'opzione e «Rivedi»; gli eventi che mancano si aggiungono dove servono (D11–D13) | 4–5 |
| 23.5 | **Il pannello di aiuto**: F1 e il pulsante nella pausa, i quattro gruppi dalla tabella dei testi, il testo del mago, i test e le foto nelle due lingue (D14) | 2–3 |
| 23.6 | **Misura e chiusura**: le ADR, le lezioni, il piano, il README (la sezione *Comandi* riscritta), `bundleVersion` 1.11.0, la lista *Da provare*, il tag `v1.11.0` dopo i Tests verdi in CI | 2–3 |

**Stima: 16–22 h**, con il disegno e la chiusura. Se serve tagliare, in quest'ordine: il suggerimento del borgo e quello delle abilità (restano otto); le frecce (solo WASD); la riga del mago nel pannello; il pulsante «Rivedi i suggerimenti» (si azzerano dalle opzioni cancellando la preferenza). **Non si taglia:** la precedenza del tasto sul click, il click da fermo, il fuoco del campo del nome, la memoria dei suggerimenti.

---

## Rischi da tenere d'occhio

- **Le lettere nel campo del nome.** La creazione del personaggio ha un campo di testo: scrivere «Dario» non deve far
  camminare nessuno, né in quel pannello né nelle schermate dopo. Le azioni di `Gameplay` si spengono già fuori dal
  gioco? Si controlla in 23.2 con un test (D8).
- **Gli angoli e i muri.** Con una destinazione a 1,5 m, camminando contro un muro in diagonale il cavaliere può
  strisciare o vibrare; la misura di 23.2 (destinazione contro `Move`) guarda proprio questo, con una scena di
  prova a stanze strette (il corridoio di un metro e mezzo della cripta).
- **Il colpo in corso e la coda.** Col click un movimento chiesto durante un colpo va in coda; con una tastiera
  tenuta premuta la coda si riempie a ogni frame. Va decisa una volta (l'ultima direzione vince) e provata con una
  spada e con il dardo.
- **Il tasto sinistro tenuto.** Oggi il comando si rinnova ogni 0,1 s; con i tasti premuti non deve rinnovarsi, e a
  tasti rilasciati con il tasto ancora premuto deve riprendere senza uno scatto verso il cursore.
- **Il canvas delle barre.** Un canvas in world space con 24 barre cambia di disegno a ogni spostamento: si misura
  in `-benchmark-levels` o nella scena dello sciame (i 531 fps del borgo non devono scendere, nel dungeon si guarda
  l'attuale).
- **I suggerimenti nei test.** Un riquadro che compare in un test PlayMode può coprire un click: i test lo
  spengono con la preferenza (come i filmati con `DARKDESCENT_FILMS`), e solo i test dei suggerimenti lo accendono.
- **Le due lingue.** Ogni testo nuovo è una riga della tabella: il test delle chiavi lo tiene d'occhio, ma i testi
  lunghi nel riquadro e nel pannello si guardano nelle foto, soprattutto in inglese.
- **Il gamepad.** Fuori dall'ambito; l'azione `Move2D` è fatta in modo che lo stick possa riusarla un giorno.

---

## Passo 23.1 — Il disegno

**Com'è andata (4 nov 2026).** Il foglio `Recordings/Photos/m23_disegno/disegno.html` (fuori da git, una pagina sola con
le immagini dentro) è montato su foto vere del gioco (`13_caves_room`, `09_crypt_room`, `14_pause`): niente è ancora nel
gioco, le barre, il riquadro e il pannello sono disegnati sopra. Cosa mostra:

- **Le barre (D9, D10).** Uno sciame di sette nelle caverne con la barra solo sui quattro feriti (62%, 34%, 81%, 18%),
  uno dei quali campione (dorata); la cripta con un unico (arancione, nome sopra la barra) e uno scheletro. Barra di
  74 × 10 px a 1080p, bordo scuro, riempimento che si accorcia da destra.
- **Il riquadro (D11, D12).** Due posti, **A** in basso al centro sopra la cintura e **B** in alto a sinistra (l'automappa
  è a destra), con titolo in oro, una o due righe, i tasti disegnati come tasti e la barra sottile degli 8 secondi; due
  esempi di testo (italiano, inglese) e la tabella dei dieci momenti.
- **Il pannello (D14).** Il menu di pausa con il quarto pulsante *Comandi*, poi il pannello in italiano e in inglese:
  *Movimento*, *Vedere* a sinistra, *Combattimento*, *Finestre* a destra, con la riga del Mago e il pulsante *Chiudi (F1)*.

**Due cose da sapere:** i tasti con il numero 1 in Cinzel somigliano a una I (nel gioco si guarda se il carattere
lo fa davvero), e il pannello è alto 800 px su 1080: con una riga in più nei gruppi non basta, e andrebbe in tre
colonne.

**Approvato da Mirco il 4 novembre 2026:** il riquadro **A** (in basso al centro), le barre e il pannello com'erano
disegnati, con il pulsante *Comandi* nella pausa.

---

## Passo 23.2 — WASD

**Cosa c'è (4 nov 2026).** `Move2D` (Value, Vector2) nel `PlayerControls` con due composti, **WASD** e le **frecce**
(`2DVector` normalizzato: la diagonale non è più veloce); il wrapper si è rigenerato da solo al primo avvio di Unity.
`PlayerInputReader.MoveKeys` li legge (zero con le azioni spente: menu, filmati e morte restano coperti).

- **`KeyboardMove`** (`Core/Player`, logica pura): dai tasti e dalla camera di quel frame la direzione in piano, di
  lunghezza 1; di una camera a picco, che non ha un avanti in piano, usa il suo su. Sei test EditMode: i quattro scatti
  della visuale, le diagonali, la zona morta.
- **`PlayerMotor.Steer`** (D4): una destinazione a 1,5 m, **ritagliata sul bordo del NavMesh** con `NavMesh.Raycast`.
  Contro un bordo storto si scivola lungo il bordo (la direzione proiettata sul bordo), contro uno dritto ci si ferma:
  un punto oltre il muro, preso con `MoveTo`, avrebbe mandato l'agent a fare il giro dall'altra parte. Non ho provato
  `NavMeshAgent.Move`: la destinazione ritagliata passa le prove (muro dritto, muro di sbieco, spiazzo) e non cambia
  animazioni e passi, che seguono la velocità dell'agent.
- **`MeleeAttack.IsInReach`**: il bersaglio è già a portata (colpo o dardo con la linea libera), senza inseguirlo.
- **`PlayerController`** (D5–D8): i tasti hanno la precedenza e prendono tutto il frame (coda del click, pressione,
  tasto tenuto); rilasciati ci si ferma, e il sinistro tenuto riprende a seguire il cursore.
  - Un click su un **nemico a portata** (o un barile) lo colpisce **da fermo**: i tasti non spostano il cavaliere finché il
    bersaglio c'è, il sinistro è tenuto su di lui o il colpo non è finito; tenuto premuto si continua a colpire, fuori
    portata si lascia e i tasti riprendono. Un click su un nemico **fuori portata** e sul **pavimento** non fa niente.
  - Un click su un **oggetto da usare** (cassa, scala) è un comando nuovo: il cavaliere ci cammina e lo usa, e i tasti
    **tacciono finché non si rilasciano tutti** (se no il frame dopo il tasto ripigliava il comando e l'oggetto si usava
    quando capitava di passarci accanto).
  - Un tasto che ricomincia a guidare lascia l'oggetto che si andava a usare e il nemico da inseguire. Durante un colpo
    partito non si cammina e non si accoda niente: la direzione si rilegge a ogni frame.
  - Il bottone cliccato resta selezionato dall'interfaccia, e le sue frecce e WASD ne sposterebbero l'evidenza: a ogni
    frame di tasti la selezione si toglie (tranne un campo di testo).
- **Chi spegne i tasti (D8):** `SetKeyboardBlocker`, dato dalla `CompositionRoot`: il pannello delle abilità (`N`, a
  tutto schermo), l'altare delle difficoltà e un'abilità del Guerriero che porta il cavaliere (`WarriorAbilities.IsBusy`).
  Pausa, morte, filmati, menu e vittoria spengono già il controller o il reader. **Due scostamenti dalla scheda:** il
  **banco** del borgo non blocca i tasti (si chiude da solo allontanandosi, come col click: bloccarli avrebbe lasciato il
  banco aperto a chi cammina con la tastiera), e il campo del nome vive nel menu iniziale, dove il reader è spento.
- **Gli incantesimi (D7)** non cambiano: mirano al cursore, e il `Stop` del lancio dura un frame, poi i tasti riprendono.

**Prove.** EditMode 645 verdi (+6). PlayMode `KeyboardMovementTests`, 13: le quattro direzioni e la diagonale, la
diagonale non più veloce, l'arresto al rilascio, il muro dritto e di sbieco, il tasto sopra il click e il click che
riprende, il click sul pavimento che non cambia strada, il blocco da fuori, il colpo da fermo (Guerriero e Mago, dardo a
4 m), il click fuori portata, la cassa con i tasti giù, W a ogni scatto della visuale nel livello generato.

**Lezioni.** (1) Con più `Set` di fila su una tastiera di prova i tasti si cancellano: ogni evento scrive il byte dei
tasti letto prima che gli altri siano elaborati (W e S stanno nello stesso byte); si manda **un solo stato** con tutti i
tasti (`KeyboardState`). (2) Dopo uno `Warp` il collider segue al passo di fisica: un click nello stesso frame colpisce
il pavimento; serve `Physics.SyncTransforms()`.

---

## Passo 23.3 — Le barre sui nemici

**Cosa c'è (5 nov 2026).** L'opzione **«Barre della vita sui nemici feriti»** nelle opzioni (sotto i numeri delle sfere),
**spenta di default**, chiave `enemybars.show` tra le preferenze. Accesa, ogni nemico **vivo e ferito** ha una barra
sopra la testa; intatti, morti e il **re** non ne hanno (il re ha la sua in alto).

- **`EnemyBarRules`** (`Core/UI`, logica pura): chi ha la barra (`IsWounded`), quanto è piena (`Fill`), le **24 più
  vicine** (`PickNearest`, senza allocare). Tredici test EditMode (con i casi).
- **`EnemyHealthBars`** (`UI`): un solo canvas in **world space**, creato al primo uso (spente non costano niente),
  girato verso la camera a ogni frame; le barre vengono da un **pool** che non supera 24. Si segue la vita sugli eventi
  di `Health` (`HealthChanged`, `Died`, `Revived`): niente `Update` per nemico, solo un `LateUpdate` che sceglie le più
  vicine dentro 24 m e muove le barre mostrate. `Track`/`Untrack` come per i numeri di danno, dal `CompositionRoot`.
  Nessun raycast né collider: le barre non rubano il click.
- **Colori (D9):** rosso per i normali; per campioni e unici **il colore del loro grado** (`RankNumbers.Color`), lo
  stesso del nome nella barra in alto. L'unico ha anche il **nome** sopra la barra, nella lingua attiva.
- **`HealthBarTools.InstallBars`** (Editor, `DarkDescent/UI/Metti le barre sui nemici`): mette nella scena `Core`
  l'oggetto `EnemyHealthBars` e la casella nelle opzioni (copia di quella dei numeri delle sfere, con la spunta d'oro);
  *Indietro* scende di una riga. Si rifà senza doppioni.

**Uno scostamento dalla scheda:** D9 diceva «dorata per i campioni, arancione per gli unici», ma i gradi della M12 sono
**blu per i campioni e oro per gli unici** (nome, anello sotto i piedi e barra in alto). Le barre prendono il colore dal
grado, non da una tabella loro: così le tre cose dicono la stessa cosa.

**Prove.** EditMode 658 verdi (+13). PlayMode `EnemyHealthBarsTests`, 9: spente di default (e il canvas non
nasce), solo sui feriti con la parte piena giusta e sopra la testa, guarito del tutto e morto la tolgono, `Revive`,
trenta feriti → 24 barre e sono le più vicine (uno oltre il raggio no, e quando uno muore il venticinquesimo prende il
posto senza far crescere il pool), spegnere l'opzione le toglie, `Untrack`, la casella (spenta di partenza, spunta
d'oro, preferenza, riaperto il gioco le trova accese), i colori dei gradi e il nome di Morvak nelle due lingue, il re
senza barra. Foto: `Recordings/Photos/m23_barre` (`PhotoTour.EnemyBars`: lo sciame, un normale, un campione e Morvak).

**Da sapere.** Le barre hanno il test di profondità: un muro davanti a un nemico copre anche la sua barra (e il
nome), come copre il nemico.

---

## Passo 23.4 — I suggerimenti

**Cosa c'è (5 nov 2026).** Dieci suggerimenti (D11), un riquadro in basso al centro sopra la cintura (la posizione **A**
approvata il 4 nov), la memoria nelle preferenze del computer e, nelle opzioni, **«Mostra i suggerimenti»** (accesa di
default) e **«Rivedi i suggerimenti»**.

- **`Hints`, `HintBook`** (`Core/UI`, logica pura): gli id nell'ordine di precedenza (muoversi, nemico, oggetto,
  pozione, scala, punto attributo, punto abilità, portale, abilità, borgo); il libro sa quali sono stati visti, quali
  aspettano, se sono accesi; ogni suggerimento esce **una volta sola**, **uno alla volta**, in ordine; spenti, niente
  entra in coda; `Serialize`/`Load` come una riga di id. Dieci test EditMode, con i testi (titolo e testo in tutte e
  due le lingue, stessi segnaposto, nessuno vuoto).
- **`HintDirector`** (`UI`): ogni condizione si controlla **quattro volte al secondo** finché il suggerimento non è
  stato mostrato; la coda esce un riquadro alla volta per **8 secondi** o finché si preme un tasto (dopo mezzo secondo,
  perché il tasto che si stava già premendo non lo cancelli); il click non lo chiude e non lo prende (nessun elemento
  riceve raggi). Tace con `gate` falso e il tempo si ferma dov'è.
- **`HintSenses`** (`Composition`): le dieci condizioni, che leggono quello che c'è già: nemico vivo a 13 m
  (`LevelContext.Enemies`), oggetto a terra a 9 m (nuovo registro `GroundItem.OnTheGround`, niente ricerche nella scena),
  pozione in cintura, scala a 9 m (non nel borgo), punti attributo e abilità da spendere, mana per il portale (non nel
  borgo), almeno due abilità sul destro/Q/E, il borgo. **Scostamento dalla scheda:** D11 diceva «gli eventi che mancano
  si aggiungono dove servono»: non ne ho aggiunti, le condizioni si leggono con il controllo a 4 Hz (costo trascurabile,
  e spento appena il suggerimento è visto), così non ho toccato dodici classi.
- **Quando tace (D12):** menu iniziale, pausa, filmati, passaggio di livello, morte, sala del re, albero delle
  abilità e altare aperti. Il riquadro sta sotto la schermata di morte, la dissolvenza e il titolo del livello.
- **I tasti veri (D12):** `PlayerInputReader.KeyText`/`MoveKeysText` leggono il nome del tasto dal binding («I», «C»,
  «N», «T», «Alt», da «1» a «8», «Q»/«E», «K», «W A S D»); i testi hanno i segnaposto `{0} {1} {2}`.
- **La memoria (D13):** `hints.on` (default 1) e `hints.seen` in `PlayerPrefs`, non nel salvataggio: un nuovo
  personaggio non li ripete. Una **partita già avanti** su un computer nuovo li vede tutti, al loro momento.
  «Rivedi» li azzera e li riaccende.
- **Le opzioni:** il riquadro passa da 800 a 920 px e le righe salgono di 45, per fare posto a casella e pulsante;
  *Indietro* è l'ultima riga.
- **`HintTools.InstallHints`** (Editor): mette riquadro, direttore e righe delle opzioni nella scena `Core`; si rifà
  senza doppioni.

**Nei test** i suggerimenti partono **spenti** (`ForgetPreferences` scrive `hints.on = 0` e cancella i visti): un
riquadro non deve coprire un click. Solo `HintPlayModeTests` li accende.

**Prove.** EditMode 668 verdi (+10). PlayMode `HintPlayModeTests`, 10: spenti nei test; il muoversi compare con
«W A S D» nel testo, i suoi elementi non ricevono raggi, un tasto lo chiude solo dopo mezzo secondo e non torna; scade
da solo con la barra che si accorcia; in pausa sparisce e il tempo si ferma, alla ripresa torna; segue la lingua anche
aperto; i tasti dai binding; i punti attributo e il nemico a portata (da 40 m no); le opzioni (casella, preferenza,
«Rivedi» li riaccende); riaperto il gioco il visto resta. Foto: `Recordings/Photos/m23_hints` (`PhotoTour.Hints`: il
riquadro nelle due lingue con testi corti e lunghi, e le opzioni).

**Da sapere.** Il riquadro è largo 640 (non 600 del disegno): in maiuscoletto le righe italiane e inglesi sono più
larghe di come le avevo disegnate; con testi di due righe sta comodo sopra la cintura. Il `1` in Cinzel somiglia a una I (nella foto della pozione, «PRESS 1–8» si legge «l–8» a colpo d'occhio): si capisce dal contesto, ma è il dubbio che la scheda di 23.1 già segnalava. Se non basta si scrive «da 1 a 8».

---

## Passo 23.5 — Il pannello dei comandi

**Cosa c'è (5 nov 2026).** **F1** apre e chiude il pannello, **Esc** lo chiude, e il menu di pausa ha il pulsante
**Comandi** tra *Opzioni* e *Salva ed esci*. Quattro gruppi in due colonne come nel disegno approvato: *Movimento* e
*Vedere* a sinistra, *Combattimento* e *Finestre* a destra, 17 righe.

- **`HelpTable`** (`Core/UI`, dati): i gruppi, le righe e quale tasto va accanto a ognuna (`HelpKey`); solo la riga del
  tasto sinistro sul nemico ha un testo per il Mago («dardo arcano»), le altre sono uguali per tutti.
- **`HelpPanel`** (`UI`): costruisce le righe alla prima apertura da `HelpTable` e riscrive tasti e testi a ogni
  apertura e a ogni cambio di lingua; i tasti vengono dai binding (`PlayerInputReader.KeyText`, `MoveKeysText`,
  `RotateKeysText`, ora con `CompositeKeysText` per i tasti dei composti). La classe si legge all'apertura.
- **F1:** nuova azione `ToggleHelp` in `PlayerControls` (il wrapper si è rigenerato da solo; per farlo ho dovuto
  spegnere un attimo le due righe del reader che la nominano, o la compilazione non partiva).
- **Pausa:** *Comandi* nasconde il menu e apre il pannello, chiuso il menu torna (come per le opzioni); **F1 in
  pausa passa dal menu**, così il menu non resta sopra al pannello; Esc dal pannello torna al menu, non chiude la pausa.
- **Gating:** F1 non fa niente nel menu iniziale, da morti e nei filmati; con il pannello aperto WASD non muove il
  cavaliere (si aggiunge al blocco di 23.2) e i suggerimenti tacciono.
- **`HelpTools.InstallHelp`** (Editor): il pannello è una copia ripulita della cornice delle opzioni; il menu di
  pausa cresce di una riga (400 → 466 px).

**Prove.** EditMode 673 verdi (+5, e `InputBindingsTests` ora sa che F1 è il pannello: F2–F8 restano libere). PlayMode
`HelpPanelTests`, 7: F1 apre e chiude, Esc chiude senza la pausa; i tasti veri («W A S D», «Z X», «Q E», «1–8», «Esc»…)
e i testi; italiano a pannello aperto; il Mago; il pulsante della pausa e il ritorno al menu (anche con Esc); F1 in
pausa; WASD fermo sotto il pannello. PlayMode completa 383 verdi e 2 saltati (prima dell'ultima correzione, F1 in pausa,
poi 17 su 17 per `HelpPanelTests` e `HintPlayModeTests`). Foto: `Recordings/Photos/m23_help` (pausa, pannello in inglese,
in italiano, del Mago).

**Da sapere.** Il pannello è alto 780 px (non 800): le righe di due testi stanno in una colonna senza passare alle tre
colonne temute in 23.1. Il `1` di «1–8» in Cinzel si legge ancora come una I (come nei suggerimenti): con «Pozione della
cintura» accanto si capisce.

---

## Passo 23.6 — Misura e chiusura

**Fatto il 5 novembre 2026.**

- **Il costo delle barre (D10).** `PhotoTour.BarsCost` (categoria Photos, fuori dai test di ogni push) mette i nemici
  feriti del 40% attorno al cavaliere nella cripta alla 4 e misura 600 frame per giro, a vsync spento, due giri con le
  barre spente e due accese (`Recordings/Photos/m23_costo/costo_barre.txt`). Nel livello c'erano 16 nemici vicini, non
  24 (il tetto di 24 lo prova `EnemyHealthBarsTests`):

  | | main thread (media) | p95 | allocazioni a frame | batch a frame |
  |---|---|---|---|---|
  | barre spente | 4,07 / 4,06 ms | 4,46 / 4,76 | 9552 / 9556 B | 891 |
  | barre accese (16) | 4,15 / 3,92 ms | 4,58 / 4,29 | 9551 / 9551 B | 892 |

  La differenza sta dentro il rumore tra i due giri uguali; le allocazioni non cambiano e c'è **un solo batch in più**
  (il canvas). È una misura da editor: i fps della build (531 nel borgo) si guardano nella build, con le barre accese,
  e sono nella lista *Da provare*.
- **I numeri.** EditMode 673 verdi (+34 dalla M22), PlayMode 384 verdi e 2 saltati (+39). Salvataggio **10** e livelli
  invariati, nessun test fisso spostato.
- **Documenti.** ADR-142…145 (WASD, barre, suggerimenti, pannello), le lezioni e il calendario nel piano (v2.77), il
  README (*Stato* e *Comandi*), `docs/Da_provare.md`, `bundleVersion` 1.11.0.
- **Scostamenti dalla scheda, in un posto solo:** i colori delle barre sono quelli dei gradi (blu e oro, non dorato e
  arancione, 23.3); le condizioni dei suggerimenti si leggono a 4 Hz, senza eventi nuovi (23.4); il banco del borgo non
  blocca i tasti e il campo del nome vive nel menu (23.2); il pannello è alto 780 px (23.5).
- **Resta fuori, come da scheda:** cambiare i tasti dal menu, il gamepad, un livello di tutorial.
