# M25 — "Stanze con segreti"

**Cosa deve succedere (Definition of Done):** le stanze smettono di essere solo posti da attraversare e ripulire: ci sono cose da
**scoprire** e da **scegliere**. Un forziere può essere un **mimic**, un baule maledetto scatena un'**ondata** prima di aprirsi, una
**stanza sigillata** si apre con una **leva** trovata altrove nel livello, un **prigioniero** da liberare ringrazia con un premio,
un **leggio** o un **diario** racconta un pezzo del mondo, una **statua** si anima quando ci si avvicina al tesoro. Le caverne
hanno finalmente **pozzi e santuari**, e gli incantesimi rompono i **barili**. Italiano e inglese, e **le mappe e i loot dei semi già
provati non cambiano** (tutto ciò che nasce qui ha un flusso suo del seme, come il Custode nella M24).

- I **segreti** sono dati di una stanza, non un tipo di stanza nuovo del generatore: si scelgono **dopo** tutto il resto
  (`PlaceSecrets`, come `PlaceGuardian`), da un hash della cella e della profondità, e si ricordano nei ricordi dei livelli (ADR-079).
- Ogni segreto è un `Interactable` o un componente sul modello dei forzieri e dei santuari (M12, M13): niente macchina nuova.
- I modelli si fanno in Blender da script nel repo, nello stile KayKit (ADR-096), come nella M13; la sola cosa nuova è il **mimic**,
  che riusa il forziere con denti e una lingua.
- Tag **`v1.13.0`**.

**Fuori da questa milestone:** pozze di acido e lava e statue che sputano fuoco o fulmini (sono **pericoli del pavimento**, un
tema loro con la misura del danno: restano nella lista *Da valutare*), un editor di stanze, enigmi con più leve in ordine, chiavi e
inventario di chiavi, un diario con tutte le pagine raccolte, boss nuovi, la risalita (M22 è fatta).

---

## Stato verificato il 6 novembre 2026

Verificato dopo la chiusura della M24 (tag `v1.12.0`, build 42216532781 verde).

| Cosa | Com'è oggi |
|---|---|
| Come nascono le stanze piene | `DungeonPopulator` (`Core/Levels`): `Populate` (torce, casse, oggetti contro i muri, nemici, maghi), poi `Decorate` (arredi con un flusso suo del seme, un punto d'interesse al centro delle stanze grandi: barili, pozzo, santuario, trappole, `RoomFeatureKind`), poi `PlaceGuardian` (M24, flusso suo). Simboli a un carattere nella mappa (`c` cassa, `S` nemico, `(` `)` pozzi, `[ ] { }` santuari, `^` trappola, `K`/`k` il Custode e il suo tesoro) |
| Le casse | `Chest` (`Items`): `Bind`, `Preview*` (il contenuto si decide alla creazione, non all'apertura), `Opened`, `RestoreOpen`, **sigillata** con `OpensWithBoss` e `OpensWithGuardian`, `Unseal()`. I ricordi dei livelli (`LevelRecorder`, ADR-079) sanno quali casse sono aperte |
| Cosa si clicca | `Interactable` (`Interaction`): etichetta localizzata, punto d'arrivo, `Used`. Pozzo (`Well`) e santuario (`Shrine`, `ShrineDefinition`) sono interattivi usabili una volta |
| Le porte | Una sola, `BossGate` (la sala del re): si chiude quando si entra, si apre alla morte del re. Non c'è una porta generica né una leva |
| Pozzi e santuari | Nascono dal punto d'interesse **delle stanze grandi** della cripta. Le caverne (`CavePopulator`) **non hanno stanze**: restano senza (dal piano, «ripreso il 22 ott») |
| I barili | `Health` + `Barrel`, esplosivi inclusi. Il Dardo e l'Anello di fuoco colpiscono solo chi ha una `Health` **e passano sui barili** (avanzo della M13); il barile piccolo del crollo (`CaveDecor_RockfallC`) resta in piedi e non si rompe |
| I nemici | `EnemyBrain` con fuga e ronde (M24); `SpecialEnemy` per campioni e unici; scorte di forzieri (M12) |
| Testi e racconto | Nessun testo di mondo: c'è solo il filmato dell'apertura (M19) e i nomi degli oggetti. Le stringhe sono in tabella IT/EN (`Localizer`) |
| Il salvataggio | Formato **11**. I ricordi dei livelli sono per scena e profondità: nemici morti e casse aperte |

---

## Decisioni

**Confermate da Mirco il 6 novembre 2026:** tutte, D1–D15, **come proposte**, e per le quattro scelte vere le opzioni consigliate: **D2** i sei segreti (non solo i tre del combattimento), **D5** la leva lontana dalla porta (non la pietra da spingere), **D9** dodici testi che raccontano il borgo e Morvak (non solo atmosfera), **D13** nei ricordi dei livelli (verificato in 25.1: **niente formato 12**). Il disegno e i testi (25.1) restano da approvare prima del codice.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M25 "Stanze con segreti"**, tag **`v1.13.0`** | Aggiunge cose da fare nel mondo, non cambia le regole: versione minore |
| D2 | **Che cosa entra** | **Sei segreti**: **mimic**, **baule maledetto**, **leva con stanza sigillata**, **prigioniero**, **lore** (leggio/diario), **statua che si anima**; più le due code di M13 (**pozzi e santuari nelle caverne**, **incantesimi che rompono i barili** e il barile del crollo). **Fuori:** acido, lava e statue che sputano (pericoli del pavimento, un'altra milestone). **Da scegliere:** o così, o i soli tre che cambiano il combattimento (mimic, baule, statua) con il resto più avanti | La lista di Mirco del 19 ott li mette insieme; i pericoli del pavimento vogliono la loro misura del danno e fanno una milestone corta a parte |
| D3 | **Il mimic** | Un forziere come gli altri **fino a che non lo si apre**: il click lo fa saltare in piedi e attaccare (un nemico `Mimic`, un unico del tipo bruto con il modello del forziere, vita e danno da tabella). Uno su **otto** dalla profondità **2**, scelto da un hash della cella: ha il **contenuto di una cassa normale** quando muore. L'etichetta è quella di un forziere, ma la **ronda** (M24) lo prova su semi nuovi: raggiungibile e battibile | Il piano di Mirco: «mostri travestiti da forziere». Un tiro in più sposterebbe i loot dei semi provati: l'hash no |
| D4 | **Il baule maledetto** | Un forziere più scuro, con **etichetta diversa** («Baule maledetto»: il giocatore sa cosa fa): aprirlo chiama **un'ondata di 3–5 nemici** della zona dai lati della stanza, e il **premio è migliore** (una cassa col premio della profondità successiva, rarità spostate). Il baule non si apre di nuovo e l'ondata **si ricorda** (ADR-079). Uno ogni **due livelli** dalla profondità 3 | È la scelta consapevole che manca: ora la sola scelta è se aprire o no una cassa; così è se vale la pena |
| D5 | **Leva e stanza sigillata** | In ogni livello **da 2 in su**, uno su tre ha una **stanza sigillata**: una porta di pietra (nuovo `SealedDoor`) in un corridoio, con dietro un tesoro e un po' di scorta. Una **leva** (un `Interactable`) sta in **un'altra stanza** del livello, a distanza, e apre la porta quando la si usa; la porta si ricorda aperta. Sulla mappa non compare niente: il segreto si scopre andando in giro. **Da scegliere:** o così (la leva è lontana dalla porta), o **un solo passo** (una pietra da spingere accanto alla porta, senza giro per il livello: meno da scoprire) | È il segreto più grosso, quello che fa girare per il livello. Servono posti che il generatore sa dare: la stanza di mezzo di un corridoio, e una stanza lontana per la leva (vicino ai punti d'interesse già in uso, senza toccare le mappe) |
| D6 | **Il prigioniero** | Un **cavaliere in catene** (modello `Barbarian` o `Knight` di Adventurers già nello zip, in posa seduta, con la catena in Blender) in una stanza **con scorta**: liberato (click, dopo che la scorta è morta) **dà una pozione e un po' d'oro e un consiglio** (una riga di testo del mondo), poi se ne va verso la scala su per il livello (si sposta con `MoveTo`, come la fuga). Uno ogni **tre livelli** dalla 2 | Dà una scelta (ripulire prima o no) e un po' di racconto con poche cose: non unisce un compagno, non c'è un compagno nel gioco |
| D7 | **La statua che si anima** | Una **statua di pietra** (arredo che c'è già nel tema) **vicino a una cassa**: quando il cavaliere si avvicina a **2 m** dalla cassa, la statua si **scrolla** e diventa un nemico unico `Statue` (lento, tanta vita e armatura, danno da bruto) con un colpo caricato; quando muore resta in macerie. Una ogni **due livelli** dalla 2, mai nella stessa stanza di un baule maledetto | È la sorpresa classica che si vede nell'ombra: la telegrafia c'è (la polvere, il verso) e la regola è una sola |
| D8 | **Il tesoro dei segreti** | **Nessun premio più grosso di quello di una cassa del livello successivo**, e **un solo unico possibile** (il tetto dei forzieri del Custode, D11 della M24) alla profondità 4 e oltre: i segreti sono un'alternativa più rischiosa alle casse, non una strada più facile | Il tesoro non deve spostare le fasce della misura (M14): si controlla con la misura dei livelli |
| D9 | **La lore** | **Dodici testi brevi** (leggii, diari, un'incisione sul muro; 40–80 parole l'uno) in italiano e in inglese, **scritti da me e approvati da Mirco in blocco** in un file (`Strings` come le altre), letti con un click in un pannello come la finestra dell'altare. Un testo si **ricorda letto** (non si ripete nello stesso mondo), e **si sceglie dall'hash della cella**, senza ripetersi nello stesso livello. **Da scegliere:** o dodici testi che raccontano il borgo e Morvak, o solo flavor da dungeon («un diario di un cavaliere che scese prima»), senza storia | La storia non esiste ancora nel gioco (l'apertura e la vittoria sì): dodici testi la danno senza aprire una storia a parte. Un'unica cosa che Mirco deve leggere, non otto |
| D10 | **Pozzi e santuari nelle caverne** | Negli **slarghi dei cunicoli** (le celle con più di 5 vicini liberi), con le **probabilità e il limite della cripta** (al più un pozzo e un santuario per livello), scelti dall'**hash della cella e della profondità** come in cripta: le caverne dei semi provati non cambiano. Una foto in una caverna | Dalla M13 sono rimasti senza: è un'ora e mezza |
| D11 | **Gli incantesimi rompono i barili** | Il Dardo e l'Anello di fuoco **colpiscono** anche i barili (compresi quelli esplosivi: fanno saltare l'area come con un colpo). Il barile del crollo diventa **un barile vero** (si rompe come gli altri, con lo stesso contenuto vuoto) | Come in Diablo, e un avanzo di una milestone chiusa |
| D12 | **Come si riconosce un segreto** | **Una luce e un suono** (un bagliore tenue dalla porta sigillata, un tintinnio dalla leva quando ci si avvicina a 6 m), e **una riga** quando si scopre qualcosa («Hai sentito scattare qualcosa»); niente icone sulla mappa. Le **etichette** dei forzieri restano quelle di sempre (il mimic mente) | Il piacere è scoprirli; un'icona sulla mappa lo toglie |
| D13 | **Dove si ricorda** | **Nei ricordi dei livelli** (ADR-079), **senza un formato nuovo del salvataggio**: ogni livello ricorda per scena e profondità le **casse e i bauli aperti, le porte aperte, i prigionieri liberati, i testi letti**. **Da verificare** nel 25.1: se il ricordo (oggi solo nemici morti e casse aperte) vuole un campo in più, serve il **formato 12** con la sua fixture e la migrazione (come la M24 per l'interruttore) | Meglio un campo nei ricordi che un formato nuovo. **Verificato in 25.1:** `LevelRecord` ha già liste che un salvataggio di prima non ha (`_broken`, `_spent` della M13, con il loro conteggio): lo stesso modo per porte, leve, prigionieri e testi, quindi **niente formato 12** |
| D14 | **Cosa NON cambia** | La **generazione delle mappe e dei loot dei semi provati** (un flusso suo del seme per i segreti, solo ciò che nasce nuovo cambia); le **difficoltà**, la **misura** a margine, la macchina a stati dei nemici, il re e il Custode, i comandi, gli alberi; i test di ogni push con semi fissi **non si muovono** (un test lo dice: la stessa mappa con e senza segreti ha le stesse casse e gli stessi nemici) | Come per la M22–M24: tutto ciò che non è il tema resta fermo |
| D16 | **Cripta e caverne** (aggiunta in 25.1, da approvare) | **Nella cripta** (profondità 1–4) tutti e sei i segreti. **Nelle caverne** (5–8) solo **mimic, baule maledetto e lore**: non hanno stanze né corridoi dove mettere una porta sigillata, una stanza con scorta o una statua. Tetto: **al più tre segreti per livello**, lore e mimic esclusi | Le caverne sono slarghi e cunicoli: una porta di pietra e una leva lì vogliono un generatore diverso; la stanza sigillata resta della cripta |
| D15 | **Si controlla** | **EditMode:** `PlaceSecrets` (le stesse mappe e gli stessi loot, i segreti solo dove si può: una stanza sigillata ha sempre un percorso alla leva, mai su un passaggio, né sul portale né sulla scala), l'hash, i ricordi, la stringa dei dodici testi (IT/EN, nessuna chiave mancante). **PlayMode:** il mimic salta e muore col suo premio, il baule evoca l'ondata, la leva apre la porta e la stanza si raggiunge, il prigioniero si libera e se ne va, la statua si anima, un testo si legge e non si ripete, un incantesimo rompe un barile. **Ronda:** la stanza sigillata si apre **sempre** con la sua leva (ogni notte, semi nuovi), il percorso alla leva esiste, i segreti non bloccano il portale né la scala. **Foto:** il mimic, il baule con l'ondata, la porta con la leva, il prigioniero, la statua, il leggio, un pozzo in una caverna | Come per le altre milestone |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 25.1 | **Il disegno e i testi**: una pagina con i sei segreti (come si vedono, che cosa fanno, la luce e il suono), la tabella delle probabilità per profondità (D3–D8), **i dodici testi in italiano e inglese** da approvare, e il controllo dei ricordi (D13: serve il formato 12?); da approvare **prima del codice** | 2–3 |
| 25.2 | **Gli arredi e i modelli**: Blender da script per la porta sigillata, la leva, il leggio, il baule maledetto, il prigioniero in catene, la statua animata (stile KayKit, ADR-096); il mimic riusa il forziere | 4–6 |
| 25.3 | **Il mimic e il baule maledetto**: `Mimic`, l'ondata, i premi, `PlaceSecrets` con il suo flusso del seme, i ricordi, i test (D3, D4, D8) | 5–7 |
| 25.4 | **La leva e la stanza sigillata**: `SealedDoor`, la leva, il posto di entrambe nel generatore (senza cambiare le mappe), la ronda che prova il percorso (D5, D12) | 6–9 |
| 25.5 | **Il prigioniero, la statua e la lore**: i tre componenti, le stringhe, il pannello del testo, i ricordi (D6, D7, D9) | 6–8 |
| 25.6 | **Le code della M13**: pozzi e santuari nelle caverne, gli incantesimi che rompono i barili, il barile del crollo (D10, D11) | 2–3 |
| 25.7 | **Misura e chiusura**: la misura dei livelli (i segreti non spostano le fasce, i tempi di costruzione), le foto, le ADR (149…), le lezioni, il piano, il README (*Stato*), `bundleVersion` 1.13.0, *Da provare*, il tag `v1.13.0` dopo i Tests verdi | 3–4 |

**Stima: 28–40 h**, con il disegno e la chiusura. Se serve tagliare, in quest'ordine: la **statua** (resta il baule maledetto per le sorprese); il **prigioniero** (D6); la **lore** (resta il testo del leggio di una riga); la **stanza sigillata** con la leva (D5, il pezzo più grosso: si rimanda a una milestone sua, come la M20 ha fatto con l'albero); le code della M13 non si tagliano, costano due ore.

---

## Rischi da tenere d'occhio

- **Una stanza sigillata che non si apre.** Una porta chiusa su un tesoro, con la leva dietro un nemico o in una stanza che non si raggiunge, è un errore vero: la ronda notturna prova ogni notte che la leva si raggiunga **prima** della porta e che la porta **non chiuda il portale né la scala** (come le lezioni delle M10, M15).
- **I semi provati.** Un segreto che usa il seme delle casse sposterebbe tutti i loot: ha un **flusso suo** (come `PlaceGuardian`), e un test confronta una mappa con e senza segreti.
- **Il mimic che regala.** Un mimic uno su otto che dà un premio come una cassa normale e basta cambia la misura del loot: si guarda con la misura dei livelli (fasce a margine).
- **L'ondata nella stanza sbagliata.** I 3–5 nemici del baule nascono dai lati e **non devono bloccare** il portale e le porte; un percorso minimo verso l'uscita resta libero (si prova).
- **I test che contano sugli arredi.** Percorsi, scale, portali e barre contano sulle stesse casse: i segreti sono **spenti** nei test di `SandboxFixture` come le ronde e le barre, e i test nuovi li accendono.
- **Il testo.** Dodici testi sono poco ma si leggono: un refuso o un'incoerenza con il filmato dell'apertura si vede. Mirco li approva in blocco **prima** del codice (25.1).
- **Le ore.** Sei segreti sono tanti: se il 25.4 si allunga, si taglia come scritto sopra, non si comprime il resto.

---

## Passo 25.1 — Il disegno e i testi

**Com'è andata (6 nov 2026).** Il disegno è questa sezione: i sei segreti, le probabilità e i **dodici testi** in italiano e inglese. Il controllo dei ricordi (D13) è
fatto: niente formato 12. Cosa c'è:

- **I sei segreti e come si riconoscono (D12).** Il mimic è identico a un forziere finché non si apre. Il baule maledetto ha il coperchio scuro e l'etichetta
  «Baule maledetto». La porta sigillata ha una luce tenue sulle giunture; la leva tintinna a 6 m. Il prigioniero è seduto in catene e parla solo a scorta morta.
  La statua sta vicino a una cassa e si scrolla con un velo di polvere e un verso.
- **Le probabilità per profondità (D3–D8, D16).**

| Segreto | Profondità | Quanto |
|---|---|---|
| Lore (leggio, diario, incisione) | 1–8 | **2 livelli su 3**, uno solo per livello |
| Mimic | 2–8 | **1 forziere su 8** (le casse del livello, mai quella sigillata del Custode) |
| Baule maledetto | 3–8 | **1 livello su 2**, uno solo, mai nella stanza di una statua |
| Stanza sigillata con la leva | 2–4 | **1 livello su 3** |
| Prigioniero con scorta | 2–4 | **1 livello su 3** |
| Statua che si anima | 2–4 | **1 livello su 2**, vicino a una cassa |
| Tetto | tutte | **al più tre segreti per livello**, lore e mimic esclusi |

- **I dodici testi (D9).** Sei nella cripta (1–4), quattro nelle caverne (5–8), due ovunque. Parlano del borgo, delle candele, di Morvak, di Grumm e di Vorrak, in
  accordo con il filmato dell'apertura (*L'ultima candela*). Sono i testi qui sotto, **da approvare in blocco**.
- **Che cosa si ricorda (D13).** Quattro liste nuove in `LevelRecord` (porte aperte, leve usate, prigionieri liberati, testi letti), ognuna con il suo conteggio come
  `_spent` della M13: un salvataggio di prima non le ha e i livelli restano com'erano.

### I testi

Ogni testo ha un titolo e 35–70 parole (le incisioni, in maiuscolo, meno). Le chiavi sono `lore.1.title`, `lore.1.text` … `lore.12.text` nella tabella delle stringhe.

| # | Dove | Titolo (IT / EN) | Testo italiano | English text |
|---|---|---|---|---|
| 1 | cripta, leggio | *Il registro delle candele* / *The Register of Candles* | Ogni anima deposta in questa cripta ha una candela nel borgo di sopra. Il sagrestano le conta al tramonto e ne scrive il numero qui. Questa pagina: novecentoquattro accese, nessuna spenta. Chi legge, ricordi il numero. Cento anni così, e non ne è mancata una. | Every soul laid in this crypt has a candle in the village above. The sexton counts them at dusk and writes the number here. This page: nine hundred and four lit, none dark. Whoever reads this, remember the number. A hundred years of it, and not one lost. |
| 2 | cripta, diario | *Il diario del sagrestano* / *The Sexton's Diary* | Le candele si sono spente ieri notte. Non soffiate, non consumate: spente, tutte insieme, come un fiato trattenuto che si lascia andare. La campana non voleva suonare. Ho preso la lanterna e la chiave del cancello. Se le ossa si muovono, qualcuno deve avvisare Morvak. | The candles went out last night. Not blown, not burned down: out, all at once, like a held breath let go. The bell would not ring. I took the lantern and the key to the gate. If the bones are stirring, someone must warn Morvak. |
| 3 | cripta, incisione | *Sul muro* / *On the Wall* | QUI IL CARCERIERE CUSTODISCE I MORTI. I MORTI CUSTODISCONO IL BUIO. NESSUNO PORTI GIÙ UNA CANDELA, E NESSUNO NE PORTI SU. | HERE THE JAILER KEEPS THE DEAD. THE DEAD KEEP THE DARK. LET NO ONE CARRY A CANDLE DOWN, AND LET NO ONE CARRY ONE UP. |
| 4 | cripta, diario | *Il diario di un cavaliere* / *A Knight's Diary* | Terza notte nella cripta. Gli scheletri non combattono come le bestie: combattono come soldati che hanno dimenticato il perché. Oggi uno è scappato, e si è ripresentato con due compagni. Non sottovaluterò il prossimo. La mia candela, in una finestra del borgo, è l'ultima: se qualcuno la tiene ancora accesa. | Third night in the crypt. The skeletons do not fight like beasts; they fight like soldiers who forgot why. Today one ran from me and came back with two friends. I will not underestimate the next. My candle, in a window of the village, is the last: if anyone still keeps it lit. |
| 5 | cripta, leggio | *Grumm Spaccaossa* / *Grumm Bonesplitter* | Grumm tagliò la pietra di questa cripta cento anni fa, e non rivide più il sole. Dicono che continuò a picchiare anche quando gli altri se ne furono andati. Dicono che picchi ancora. Tieni alto lo scudo: non si stanca, e non sceglie. | Grumm cut the stone of this crypt a hundred years ago, and never saw the sun again. They say he kept swinging long after the others had gone. They say he is still swinging. Keep your shield high: he does not tire, and he does not choose. |
| 6 | cripta, incisione | *Il Custode* / *The Keeper* | Vorrak fu il primo sepolto qui, e il primo a custodire la porta. Chiese di guardare la scala, perché nessuno scendesse più in fretta di quanto potesse risalire. Chi lo supera gli deve qualcosa, e i morti sanno tenere i conti. | Vorrak was the first buried here, and the first to keep the door. He asked to watch the stair, so that no one would go down faster than they could climb back. Whoever passes him owes him something, and the dead keep good accounts. |
| 7 | caverne, incisione | *Sotto la scala* / *Below the Stair* | Sotto la scala il muro scorda il muratore. Queste gallerie le hanno scavate i morti, da sotto, verso il borgo. Non scappavano. Contavano le candele, e ne trovavano sempre una in più di quante ne avevano lasciate. | Below the stair the wall forgets the mason. These tunnels were dug by the dead, from below, toward the village. They were not escaping. They were counting candles, and always finding one more than they had left. |
| 8 | caverne, diario | *Un diario bagnato* / *A Wet Diary* | I cunicoli sono umidi e tiepidi. Ho trovato un pozzo, un santuario e un mucchio di moccoli di candela, centinaia, in file. Qualcuno li porta giù. Qualcuno di vivo, credo. Non mi piace aver pensato «credo». Ho smesso di scrivere dopo questa riga e ho ripreso. | The tunnels are damp and warm. I found a well, a shrine, and a heap of candle stubs, hundreds, laid in rows. Someone carries them down. Someone living, I think. I do not like that I wrote "I think". I stopped after this line, then began again. |
| 9 | caverne, lettera | *Una lettera mai spedita* / *An Unsent Letter* | Cara Anya, i minatori dicono che di notte i muri cantano. Il caposquadra ride. Ho scritto il mio nome sul muro della terza galleria, nel caso qualcuno volesse sapere che sono stato qui. Se leggi questo, la miniera ha smesso di sembrare una miniera e io di sembrare io. | Dear Anya, the miners say the walls sing at night. The foreman laughs. I wrote my name on the wall of the third gallery, in case anyone should want to know I was here. If you are reading this, the mine has stopped looking like a mine, and I have stopped looking like myself. |
| 10 | caverne, leggio | *La corte del Re* / *The King's Court* | Il Re delle ossa non siede su un trono. Siede dove arde l'ultima candela, e aspetta che qualcuno la spenga o la porti via. Finché brucia non sa di essere morto. Finché brucia, ricorda di essere atteso. Non so quale delle due cose sia peggio. | The Bone King does not sit on a throne. He sits where the last candle burns, and waits for someone to snuff it or carry it away. While it burns he does not know he is dead. While it burns, he remembers being waited for. I do not know which is worse. |
| 11 | ovunque, biglietto | *Un biglietto del fornaio* / *A Baker's Note* | A chi legge: la candela che hai visto alla finestra del borgo è stata accesa per te. Non so il tuo nome. L'ho accesa lo stesso. Il pane, quando torni, è sul davanzale. — M., il fornaio | To whoever reads this: the candle you saw in the village window was lit for you. I do not know your name. I lit it anyway. The bread, when you return, is on the sill. — M., the baker |
| 12 | ovunque, incisione | *La scala è una promessa* / *The Stair Is a Promise* | SCENDERE È FACILE. LA SCALA È UNA PROMESSA E NON UN CONSIGLIO. | GOING DOWN IS EASY. THE STAIR IS A PROMISE, NOT ADVICE. |

**Da approvare:** i sei segreti come descritti, le probabilità e il tetto di tre per livello, le **caverne con tre soli segreti** (D16), e i dodici testi (con le correzioni che vuoi).

**Approvato da Mirco il 6 novembre 2026:** tutto: i sei segreti, le probabilità e il tetto di tre, la D16 (caverne con mimic, baule maledetto e lore) e i dodici testi.

---

## Passo 25.2 — Gli arredi e i modelli

**Com'è andata (6 nov 2026).** Sette modelli nuovi in `Tools/Blender/models/hidden.py` (stile KayKit, atlante `dungeon_texture.png`, ADR-094/096), esportati in
`Assets/_Project/Art/Models/DarkDescent/`:

| Modello | Cos'è | Misura | Note per Unity |
|---|---|---|---|
| `SealedDoor` | La porta di pietra: stipiti e architrave scuri, due battenti con specchiature e bandelle di ferro, il fermo di mezzo | 4,0 × 0,7 × 4,0 m (una cella) | Davanti -Y. Chiude un passaggio di una cella; si apre abbassandosi nel pavimento o scorrendo di lato |
| `SealedDoor_Runes` | Le rune viola (quattro rombi, il filo sull'architrave, un cristallino): stanno a un pelo dalla porta | 2,0 × 0,6 × 3,7 m | Stesso centro della porta; materiale **emissivo** (come `Well_Liquid` e `Crystal`) acceso a porta chiusa, spento alla leva |
| `Lever` | Il piedistallo con la placca e l'arco graduato | 1,0 × 0,8 × 1,1 m | Il perno dell'asta è a z 1,05 |
| `Lever_Handle` | L'asta con il pomello rosso, l'origine al piede | 0,2 × 0,2 × 1,1 m | Gira attorno a X nel perno del piedistallo, da -30° a +30° |
| `Lectern` | Il leggio con il libro aperto e il segnalibro | 1,0 × 0,9 × 1,55 m | Il piano guarda -Y; per i testi 1, 5, 10 |
| `Tablet` | La lapide con le righe incise e il sole in cima | 1,2 × 0,6 × 1,7 m | Per le incisioni 3, 6, 7, 12 |
| `ChestCurse` | Le catene che cingono il forziere, il teschio con gli occhi viola sul fermo, un cristallo in cima | 1,9 × 1,4 × 1,5 m | Si mette sul forziere `chest.fbx` stesso centro e stessa base; il forziere prende un materiale più scuro |

Non servono modelli per il **prigioniero** (la `Cage` e le `Chains` della M13 ci sono già; il cavaliere è un modello di *Adventurers*), per la **statua** (la `Statue`
del guardiano della M13 diventa il nemico, con lo scheletro e un materiale di pietra) né per **diari, lettere e biglietti** (`Books`, M13). Il **mimic** è il forziere.

Il foglio con tutti i modelli accanto al forziere dei KayKit è in `Recordings/Photos/m25_modelli/foglio.png` (fuori da git). Nessuna foto in gioco ancora: i
modelli entrano nelle scene dal 25.3. I 724 test EditMode restano verdi con i sette FBX importati.

**Modelli approvati da Mirco il 6 novembre 2026.**

---

## Passo 25.3 — Il mimic e il baule maledetto

**Com'è andata (6 nov 2026).** Il mimic e il baule maledetto sono fatti, con 9 test EditMode nuovi (733 in tutto) e 6 PlayMode (13 verdi con le classi delle casse e dei ricordi).

**Una correzione al disegno: i segreti delle casse non stanno nella mappa.** La scheda diceva `PlaceSecrets` nel generatore, con un flusso suo del seme come il Custode. Per le
casse non va: `LevelRecord` ricorda le casse aperte **per posto nell'ordine** in cui il livello le costruisce, e `Matches(enemyCount, chestCount)` scarta il ricordo se il conto
cambia. Il Custode l'ha potuto fare perché la migrazione 10 → 11 azzerava i ricordi; qui non c'è un formato nuovo (D13), e un simbolo in più o una cassa scambiata avrebbe fatto
tornare i nemici uccisi e richiudere le casse in un salvataggio della v1.12, senza un errore da nessuna parte. **Il segreto si decide al caricamento**, sulle casse che il livello
ha già: il livello generato è **identico** (stesso numero e stesso ordine di casse e nemici, `Secrets_DoNotChangeTheLevel`), D14 vale per costruzione, e funziona anche nelle
caverne senza altro codice (D16). La stessa regola vale per il 25.4 e il 25.5: i nemici della scorta del prigioniero e la statua **non** entrano nel conto dei nemici del livello
(nascono a runtime o hanno un conto loro).

**Che cosa c'è:**
- **`SecretRules`** (`Core/Levels`): funzioni pure del seme del mondo, della profondità e della cella (`SeedMixer.ForSecret`, un dominio suo): `IsMimic` (1 su 8 dalla profondità 2),
  `LevelHasCursed` (1 livello su 2 dalla 3), `PickCursed` (la cassa con il punteggio più alto tra **tutte** le casse normali, anche le aperte, e mai un mimic: aprirne una non sposta
  la scelta), `WaveSize` (3–5) e `WaveBrutes` (0 o 1 nella cripta, 2 nelle caverne).
- **`Chest`**: un segreto (`ArmSecret`), l'evento `Disturbed` (un click su una cassa con un segreto non la apre), `Open()` pubblico, `SetBonus` e `Cell`. `ChestSecretKind`: None, Mimic, Cursed.
- **`ChestSecret`** (`Items`, sul prefab della cassa normale): il mimic nasconde il modello del forziere e salta fuori un nemico al suo posto; alla sua morte il forziere torna e **si apre con
  il suo loot di sempre** (`Preview()` prima e dopo è lo stesso oggetto: un test lo controlla); il baule maledetto **chiama da tre a cinque nemici** in un arco di 160° davanti a lui e a ondata
  battuta si apre da solo. I nemici nati qui passano dal CompositionRoot (`HandleSecretSpawned`: difficoltà, mondo che cresce, esperienza, barra), non sono nel ricordo del livello e,
  se si lascia il livello a metà, la cassa riparte chiusa e con lo stesso segreto (`Mimic_LeftHalfway_IsAClosedChestAgain`).
- **Il mimic** (`Mimic.prefab`, `Mimic.asset`): il bruto con il modello del forziere dei KayKit, senza loot suo (il premio è la cassa); un componente, `MimicHop`, fa saltare il modello a ogni passo e
  sbattere il coperchio come una bocca. Il baule maledetto ha **`ChestCurse`** (le catene, il teschio, il cristallo) più un **bagliore viola** (una luce): dalla camera del gioco le catene da sole si perdevano nel buio.
- **Il premio del baule maledetto** (`ChestSecret.CursedBonus`): il loot della profondità **dopo**, un oggetto in più, magici e rari ×2, almeno un magico, oro ×2, unici ×1,5 e nessun unico garantito:
  un gradino sopra il forziere (D8), non il tesoro del re. Il numero vero lo dirà la misura del 25.7 (`LootCensus`).
- **`SecretTools.Build`** (menu *DarkDescent → Segreti*): idempotente, crea l'archetipo e il prefab del mimic e mette il segreto sul prefab della cassa normale. Le casse del forziere dell'unico, del tesoro del re e del Custode hanno
  un prefab loro senza segreto: non sono mai un mimic né un baule maledetto (`SealedChests_NeverHaveASecret`).
- **`ChestSecret.Enabled`**: un interruttore statico, spento da `SandboxFixture` (come quelli della M24): i test che aprono la prima cassa non trovano un mimic. Solo i test dei segreti lo accendono. **Resta da fare** (25.4): la
  ronda notturna gira con l'interruttore spento; per provare i segreti sui semi nuovi la ronda deve accenderlo.
- **Le stringhe**: `chest.cursed` («Baule maledetto») e `enemy.mimic`.

**Cosa non è come in scheda:** il baule non è «più scuro»: `InteractableHighlight` ricorda i materiali di partenza e un cambio a runtime verrebbe cancellato dall'evidenziazione; lo si riconosce dalle catene, dal teschio, dal
bagliore viola e dall'etichetta (D4 chiedeva proprio l'etichetta diversa).

**Le foto** (`PhotoTour.ChestSecrets`, in `Recordings/Photos`): il mimic chiuso (identico a un forziere), sveglio e in salto, il baule maledetto chiuso con il bagliore e la sua ondata di quattro scheletri e un bruto.

**Lezione:** `UseLootSeed` e `LevelManager.RunSeed` sono due semi: nel gioco si cambiano insieme, nei test no. Un test che cerca un livello con un segreto deve impostarli tutti e due (`UseSeed`).

**Tests di c27623e (25.2 e 25.3) verdi in CI.**

---

## Passo 25.4 — La leva e la stanza sigillata

**Com'è andata (6 nov 2026).** Fatto, con 7 test EditMode nuovi (740 in tutto), 4 PlayMode e un test della ronda (38 PlayMode verdi con le classi dei livelli, delle casse e dei ricordi).

**La misura che decideva se si poteva fare** (`SealedRoomMeasureTests`, 200 semi per profondità). I corridoi sono L di una cella tra i centri delle stanze e si incrociano, quindi un fondo cieco
non è scontato. Risultato: **un fondo cieco adatto c'è in 197 livelli su 200** a ogni profondità della cripta, e **con almeno una cassa in 132 su 200 alla 2 e alla 3, 152 alla 4** (a metà: una stanza
di circa 32 celle, una cassa, 1–3 nemici, la leva a 27–30 passi dalla porta). Con «uno su tre» applicato a tutti i livelli ne sarebbe uscito uno su cinque: la regola è quindi **uno su due tra quelli che hanno un
fondo adatto con una cassa**, e in tutto **circa uno su tre della cripta** (nella ronda locale: 63 livelli su 180, 35%).

**Come sta in piedi, senza una stanza nuova.** Come per le casse del 25.3, il generatore non cambia: la stanza sigillata è **un fondo cieco che il livello ha già**, chiuso al caricamento da una porta di pietra
su un corridoio dritto. Le casse e i nemici che ci sono dentro sono il tesoro e la scorta, quindi il numero e l'ordine di casse e nemici **non cambiano** (un test lo controlla), e le mappe dei semi provati
nemmeno.
- **`SealedRoomPlanner`** (`Core/Levels`): funzione pura della mappa e del seme. Prende le celle di pavimento semplice con due lati di cammino opposti e roccia sugli altri due (un corridoio dritto), lontano
  almeno 4 celle dall'ingresso e dalla scala; per ognuna calcola quello che l'ingresso non raggiunge più chiudendola; vale se è una stanza (almeno 8 celle, meno del 60% del livello), senza ingresso, scala
  né segni del Custode, e con almeno una cassa. Tra le candidate vince il punteggio più alto di un dominio del seme. La leva sta su una cella semplice del livello **raggiungibile a porta chiusa**, contro un muro e
  dentro una stanza (non alla bocca di un corridoio), a 12 passi dalla porta almeno (6 se non ce n'è una). Nella cripta, profondità 2–4 (D16).
- **Gli invarianti**, su 450 livelli (150 semi × 3 profondità, `SealedRoomPlannerTests`): con la porta chiusa l'ingresso raggiunge la scala giù, la leva e il suo davanti e **non** il fondo; con la porta aperta raggiunge
  tutto il fondo; il resto del livello resta raggiungibile; la leva ha il muro alle spalle.
- **`SealedDoor`** (sul prefab `SealedDoor`): un ostacolo che **ritaglia il NavMesh**, un collider sul layer degli ostacoli (ferma la vista dei nemici e gli incantesimi), le rune viola (un materiale emissivo
  salvato: niente emissione accesa a runtime) e un bagliore; aperta, il modello **scende nel pavimento** in 1,6 s. **`SecretLever`** (prefab `SecretLever`, un `Interactable` con la sua etichetta «Leva»): l'asta
  da -32° a +32°. **`SealedRoom`** li mette sotto il livello (la faccia a rune dal lato di chi arriva, la leva contro il muro) e li registra come **segreto del livello** (`ILevelSecret`).
- **Il ricordo (D13), senza formato nuovo:** `LevelRecord` ha due campi nuovi con il loro conteggio (`_secretCount`, `_secretsDone`), come i barili e i pozzi della M13. Un ricordo di una versione prima **combacia ancora**
  (nemici morti e casse aperte restano), e non inventa una porta aperta (`LevelRecordSecretsTests`). La porta si crea **prima** che il ricordo venga rimesso, in `BindLevel`, perché lo ritrovi.
- **Chi sta nel fondo non fa la ronda** (M24): i suoi punti di ronda potrebbero stare oltre la porta.
- **L'interruttore** `SealedRoom.Enabled`: spento da `SandboxFixture`, perché una porta chiusa cambia i percorsi che molti test contano di fare. Lo accendono i suoi test e la ronda.
- **`SealedRoomTools`** (menu *DarkDescent → Segreti*): crea i due prefab, il materiale e li mette nel set della cripta; idempotente.

**La ronda** (`NightlyPlayModeTests.SecretRooms_AreSound`, parte 12): 60 partite nuove, profondità 2–4, **con i segreti accesi** (stanza sigillata e casse). A porta chiusa dall'ingresso si raggiungono la leva e la
scala giù e **non** il tesoro del fondo; tirata la leva si raggiunge ogni cassa; ogni cassa con un segreto resta armata. Se in 60 partite nessuna ha la stanza sigillata, il controllo fallisce (un controllo
che non trova niente da provare passa sempre). Prova in locale con il seme 424242: 180 livelli, 63 con la stanza sigillata, 59 mimic e 61 bauli maledetti, nessun errore. *Una trappola trovata:* `LoadGeneratedCore`
spegne gli interruttori come per tutti i test (`ForgetPreferences`): vanno accesi **dopo**, non prima, o il controllo non prova niente e passa.

**Le foto** (`PhotoTour.SealedRoomShots`): la porta chiusa con le rune dal corridoio, la leva contro il muro, la porta che scende e la porta aperta.

**Da fare nei passi dopo:** il messaggio «hai sentito scattare qualcosa» (D12) e il tintinnio della leva a 6 m (D12) non ci sono ancora: servono un posto per un messaggio e un suono, e vanno con la lore del 25.5.

---

## Passo 25.5 — Il prigioniero, la statua e la lore

### 25.5a — La lore (D9, D12, D16)

**Com'è andata (6 nov 2026).** Fatti i testi del mondo, il pannello di lettura, la riga della leva e il suo tintinnio. 7 test EditMode nuovi (747 in tutto), 5 PlayMode (9 verdi con la stanza sigillata) e la ronda estesa.

**Quale testo, dove (senza un formato nuovo).** La scheda diceva che un testo letto «non si ripete nello stesso mondo»: per saperlo serviva una lista dei testi letti nel salvataggio, cioè il formato 12 (D13 voleva evitarlo). Invece **lo decide una permutazione del seme del mondo** (`LoreRules`): i testi della cripta (1–6 più 11 e 12) si ordinano con un dominio del seme e le profondità 1–4 prendono i primi quattro; le caverne (7–10 più i due di ovunque che la cripta non ha usato) fanno lo stesso per 5–8. Così **due profondità dello stesso mondo non danno mai lo stesso testo, per costruzione**, e un testo della cripta non sta nelle caverne (`ATextNeverRepeatsInTheSameWorld`, su 2000 semi). Due livelli su tre hanno un testo (misurato 62–71% a ogni profondità). Il ricordo del livello (ADR-079) sa solo se *quel* testo è già stato letto (il bagliore).
- **`SecretSpots`** (`Core/Levels`): il posto di un arredo dei segreti, per la lore ora e per prigioniero e statua dopo: una cella di pavimento semplice contro un muro, dentro una stanza, davanti libera, senza ostacoli nelle otto celle attorno, lontano da ingresso, scale, porta, leva e dagli altri segreti, fuori dal fondo della stanza sigillata. Funzione della mappa e del seme come la leva. **Misura** (60 semi per profondità): un posto in **240 livelli su 240 nella cripta e 237 su 240 nelle caverne** (98,8%).
- **`LoreSpot`** (`Levels`): un `Interactable` e un segreto del livello (`ILevelSecret`); un testo non letto ha un **bagliore di candela**, letto si spegne; si può rileggere. Tre prefab (`LoreLectern` per 1, 5 e 10; `LoreTablet` per 3, 6, 7 e 12; `LoreBook`, le carte, per gli altri) nel set della cripta **e** in quello delle caverne. I segreti si registrano sempre nello stesso ordine (stanza sigillata, lore, poi quelli dopo), così l'indice nel ricordo è stabile.
- **`LorePanel`**: titolo e testo nella lingua attiva (si rilegge se la lingua cambia), cornice delle opzioni come il pannello dei comandi, pulsante «Chiudi» ed Esc. Sta nel giro di Esc, di `CanShowHints`, del blocco dei tasti e si chiude lasciando il livello. Si installa con `LoreTools.InstallPanel` (menu *DarkDescent → UI*, Unity chiuso); i prefab con `LoreTools.BuildProps`.
- **I dodici testi** sono nella tabella delle stringhe (`lore.N.title` e `lore.N.text`, italiano e inglese), come approvati; un test controlla che ci siano tutti e due, che le due lingue siano diverse e che le virgolette del testo 8 («I think») reggano la tabella.
- **D12:** tirata la leva compare «Hai sentito scattare qualcosa» (`secret.heard`, `HudMessage`); la leva **tintinna a 6 m**, una volta per avvicinamento (si riarma oltre i 9 m), con un colpo di metallo leggero e il controllo quattro volte al secondo.
- **Interruttore** `LoreSpot.Enabled`, spento da `SandboxFixture`; la ronda (`SecretRooms_AreSound`) lo accende dopo l'avvio e ora gira anche sulle **caverne** (20 partite): con il seme 424242, **320 livelli, 237 testi (62 nelle caverne), tutti raggiungibili dall'ingresso anche a porta chiusa**; il controllo fallisce se non ne trova nessuno, né nelle caverne.
- **Foto** (`PhotoTour.LoreShots`): il leggio con il bagliore e il suo pannello, la lapide, le carte, e una lapide nelle caverne.

**Cosa non è come in scheda:** il pannello non si chiude con un click fuori dal foglio, solo con il pulsante ed Esc. Le carte (diari, lettera, biglietto) stanno su un mucchio di libri (`Books`, M13): contro un muro lontano dalla camera si vedono poco, come la leva.

### 25.5b — La statua che si anima (D7, D16)

**Com'è andata (6 nov 2026).** Fatta, con 4 test EditMode nuovi (`StatueRulesTests`), 6 PlayMode (`StatuePlayModeTests`) e la ronda estesa.

- **Quando e dove (`StatueRules`).** Un livello su due della cripta, profondità 2–4 (D16). La cassa che guarda è scelta dal seme tra **le casse normali del livello, tutte, anche le aperte** (come il baule maledetto: aprirne una non sposta la scelta), tranne: un mimic (per hash), il baule maledetto, qualsiasi cassa a meno di **14 m** dal baule maledetto (la «stanza» del baule, D7: una stanza ne misura una dozzina, e il baule chiama già un'ondata), e quelle del fondo sigillato. Il posto è accanto al davanti della cassa, su pavimento semplice della mappa, di lato e poi un poco avanti. Tutto è funzione del seme e delle casse, anche con gli interruttori spenti: il livello è lo stesso e l'indice nel ricordo non si sposta.
- **Il tetto di tre segreti (D16).** I segreti che contano sono quattro (stanza sigillata, prigioniero, statua, baule maledetto; lore e mimic no). **Se ci sono già gli altri tre la statua cede** (`StatueRules.Yields`): è la prima da tagliare nella scheda e non cambia niente di quello che c'era. Misura (60 semi × 3 profondità, con i segreti accesi): 85 statue in 180 livelli, 25 nello stesso livello di un baule maledetto (sempre a più di 14 m), e nessun livello supera i tre segreti.
- **`StatueGuard`** (`Levels`, segreto del livello): la statua di pietra (il modello `Statue` della M13, ingrandito) sta ferma; quando il cavaliere arriva a 3,2 m dalla cassa **si scrolla per un secondo** (tremito che cresce, velo di polvere, grattare di pietra) e poi **prende vita**: la statua sparisce e nasce il nemico `Statue`, con un verso. Il nemico lo lega il CompositionRoot come quelli del mimic (`HandleSecretSpawned`: difficoltà, mondo che cresce, esperienza, barra) e **non entra nel conto dei nemici del livello**. Quando muore restano le **macerie** (`Rubble`) e il ricordo (ADR-079, via `ILevelSecret`) la ritrova caduta, senza animarsi più. **Lasciando il livello a metà torna in piedi**, come il mimic.
- **Il nemico `Statue`** (`StatueTools.Build`, menu *DarkDescent → Segreti*, idempotente): il bruto con 220 di vita (90) e 55 di armatura (20), un po' più lento (1,45 contro 1,8), il doppio dell'esperienza (460), il modello più grosso del 12% e una **pelle di pietra** (le sue texture con una tinta fredda). Il colpo caricato è quello del bruto.
- **Interruttore** `StatueGuard.Enabled`, spento da `SandboxFixture`. **Ronda:** `SecretRooms_AreSound` lo accende; con il seme 424242, **82 statue** in 320 livelli, la loro cassa si raggiunge dall'ingresso anche a porta chiusa, la statua sta su un posto di cammino e il tetto di tre regge; il controllo fallisce se non ne trova nessuna.
- **Foto** (`PhotoTour.StatueShots`): la statua ferma, mentre si scrolla, il nemico di pietra, e le macerie.

### 25.5c — Il prigioniero (D6, D16)

**Com'è andata (6 nov 2026).** Fatto, con 4 test EditMode nuovi (`PrisonerRulesTests`), 5 PlayMode (`PrisonerPlayModeTests`) e la ronda estesa.

- **Dove sta, senza nemici in più (`PrisonerRules`).** Un livello su tre della cripta, profondità 2–4. La scheda diceva «una stanza con scorta»: la scorta **sono i nemici che il livello ha già** attorno a lui (nessun nemico nuovo: il conto e l'ordine restano quelli del livello generato, un test lo controlla con l'interruttore acceso e spento). Il posto è una cella contro un muro con almeno due nemici nelle sette per sette celle attorno (se non c'è, uno), scelta dal seme, fuori dal fondo sigillato e lontana dalla porta, dalla leva e dal leggio. Misura: un posto in **253 livelli su 300** (165 con la stanza sigillata), cioè circa un livello su quattro della cripta lo ha davvero. A runtime la scorta sono i nemici di casa nelle stesse sette per sette celle che ha contato il piano (fuori dal fondo sigillato), fissata prima che il ricordo tolga i morti. Un primo giro con un raggio di 14 m non combaciava con il quadrato: in 3 livelli su 50 la scorta veniva vuota, e la ronda locale lo ha preso.
- **`Prisoner`** (`Levels`, segreto del livello): il barbaro di Adventurers senza il cappello, con la pelle sporca, **seduto** (`Sit_Floor_Idle`) e con le catene (`Chains`, M13); un click con la scorta viva non fa niente e il gioco dice **«Prima la scorta»**; a scorta morta lo **libera**: esulta (`Cheering`), lascia **una pozione e dell'oro** a terra (15 × (profondità + 1) monete: 45–75 oro, meno di una cassa, D8; niente tiri, quindi nessun seme dei loot si muove), apre il pannello di lettura con il suo **consiglio** e, dopo due secondi e mezzo, **sale a piedi** lungo il NavMesh verso la scala in su, dove sparisce. Le catene restano. Il ricordo (ADR-079) lo ritrova andato, con le catene vuote.
- **Il tetto di tre segreti.** Il prigioniero conta come segreto: la statua cede se ci sono già stanza sigillata, prigioniero e baule maledetto (`ArmStatue` lo chiede a `ArmPrisoner`). Se il livello vuole un prigioniero ma non ha un posto adatto, non conta.
- **I quattro consigli** (`prisoner.advice.1…4`, italiano e inglese) sono **nuovi e da approvare** (vedi sotto).
- **Interruttore** `Prisoner.Enabled`, spento da `SandboxFixture`. **Ronda:** `SecretRooms_AreSound` lo accende; ogni prigioniero ha una scorta e si raggiunge dall'ingresso anche a porta chiusa; il tetto di tre include il prigioniero; il controllo fallisce se non trova né statue né prigionieri.
- **Foto** (`PhotoTour.PrisonerShots`): il pannello con il consiglio, e il cavaliere liberato accanto al suo premio mentre se ne va.

**Limite noto, da decidere.** La camera guarda da lontano con quattro rotazioni, e un arredo contro un muro «vicino alla camera» (leggio, lapide, leva, prigioniero) si vede dietro il muro solo dopo aver ruotato la camera o essersi avvicinati. È così anche per i forzieri e le torce di sempre. Se non basta, si può far preferire a `SecretSpots` i muri che guardano la camera di partenza: costa un punteggio in più e sposta i posti (non i ricordi, i segreti non sono ancora in un salvataggio).

### I consigli del prigioniero da approvare

| # | Italiano | English |
|---|---|---|
| 1 | Sei sceso anche tu per le candele, vero? Prendi questo. Una parola prima che vada: i morti non si stancano, quindi non combatterli allo scoperto. Fa' che ti raggiungano da una porta. | You came down for the candles too, did you not? Take this. One word before I go: the dead do not tire, so do not fight them in the open. Make them come to you through a door. |
| 2 | Grazie, straniero. Mi hanno preso la spada e lasciato le catene, e dice molto di ciò che temevano. Prendi questo. Un forziere troppo gentile merita una seconda occhiata: l'ho imparato nel modo lento. | Thank you, stranger. They took my sword and left me the chains, which tells you what they feared. Take this. A chest that looks too kind is worth a second look. I learned that the slow way. |
| 3 | Libero, finalmente. Prendi questo, e ascolta: se nel buio scatta una leva, da qualche parte si è aperta una porta. Cammina lungo i muri. La cripta nasconde ciò che ama. | Free at last. Take this, and listen: if a lever clicks somewhere in the dark, a door has opened somewhere else. Walk the walls. The crypt hides what it loves. |
| 4 | Salirò al borgo a dire che qualcuno sta ancora scendendo. Prendi questo. Non fidarti di una statua che sta troppo vicina a un forziere. | I will go up to the village and tell them someone is still coming down. Take this. Do not trust a statue that stands too close to a chest. |

Il titolo del pannello è «Il cavaliere in catene» / «The Knight in Chains»; l'etichetta è «Prigioniero» / «Prisoner».

## Passo 25.6 — Le code della M13 (D10, D11)

Tre pezzi che la M13 aveva lasciato fuori: i pozzi e i santuari nelle caverne, gli incantesimi che rompono i barili, il barile del crollo.

### Pozzi e santuari nelle caverne (D10)

- **Dove e quanti.** Negli **slarghi** (celle libere con più di cinque vicini liberi su otto), fuori dai posti riservati, dai cunicoli, dall'ingresso, dalla scala e dal davanti delle casse. **Al più un pozzo e un santuario per livello**, a sei celle l'uno dall'altro, come in cripta.
- **Probabilità.** Quelle della cripta (`_wellChance` e `_shrineChance`, 15% per punto d'interesse), contate come i quattro punti d'interesse di una cripta: per livello di caverna **1 − 0,85⁴ ≈ 48%** a tipo. Misura su 400 semi: 199 pozzi, 202 santuari, 110 livelli con tutti e due.
- **Come si sceglie.** La cella e il tipo vengono dall'**hash della cella e della profondità** (`DungeonPopulator.CellPick`, lo stesso della cripta), e i due soli tiri per livello stanno **in fondo al flusso degli arredi**: tutto quello che c'era prima (mappa, nemici, casse, arredi, spuntoni) resta cella per cella com'era. Un test lo controlla confrontando ogni caverna con la stessa senza i due tiri.
- **Il ricordo.** Sono `OneUseFeature` come in cripta: `LevelRecord` li conta a parte (`MatchesFeatures`), e un salvataggio di una caverna di prima non ha quel conto, quindi nemici e casse tornano come erano e i pozzi sono nuovi.
- **Il tileset.** `CaveFeatureTools.Install` aggiunge al `CaveTileset` i sei simboli della cripta con gli stessi prefab (si rilancia).

### Gli incantesimi rompono i barili (D11)

- **Il proiettile** (Dardo di fuoco, Dardo arcano, Palla di fuoco): un barile intero sulla sua strada si rompe e il proiettile scoppia lì, anche se il colpo non fa danno. Prima passava sopra.
- **L'area** (Anello di fuoco, Nova di gelo, scoppio della Palla di fuoco): i barili dentro il raggio si rompono, **anche quelli esplosivi** (accendono la miccia come con un colpo, e l'esplosione rompe i vicini).
- **Il buco del NavMesh.** Il proiettile si ferma sui muri leggendo il NavMesh, e un barile ci scava un buco più largo del suo collider: il dardo lo scambiava per un muro e scoppiava prima di toccarlo. Ora un barile subito oltre il bordo (1,6 m, `Projectile.BarrelCarve`) si colpisce. Lo ha trovato il test.
- **Chi non rompe.** I proiettili dei nemici non cambiano: cercano solo il cavaliere. Nessuna esperienza e nessun segno sul barile, come per il colpo di mischia.

### Il barile del crollo (D11)

`CaveDecor_RockfallC` aveva un barile piccolo come modello con un collider fermo. Ora il posto è un'istanza di `Barrel_Small` (`RockfallBarrelTools.Build`, si rilancia): si rompe come gli altri, con i suoi pezzi, il suo suono e il passaggio che si libera, ma **vuoto** (nessuna tabella di loot e niente oggetto a terra), quindi i premi delle caverne non cambiano. Poggia a terra dove stava il vecchio.

### Controlli

- **EditMode:** `CaveFeaturesTests` (5: al più uno per tipo con le probabilità della cripta, nello slargo e a distanza, la caverna resta raggiungibile, il resto non cambia, l'hash sceglie tipo e posto). 760 verdi in tutto.
- **PlayMode:** `BarrelSpellsPlayModeTests` (4: l'Anello di fuoco rompe il barile vicino e non quello lontano, il Dardo di fuoco rompe il barile sulla strada, un barile esplosivo colpito da un incantesimo accende la miccia ed esplode, il barile del crollo è un barile vero, vuoto e a terra).
- **Foto** (`PhotoTour.CaveFeatureShots`): `cave_well`, `cave_shrine`, `cave_rockfall_barrel` e `cave_rockfall_barrel_broken` in `Recordings/Photos`. Il barile del crollo sta contro un muro e con certe rotazioni della camera si vede appena: è il limite della camera già descritto nel 25.5.

---

## Passo 25.7 — Misura e chiusura

### La misura del loot (D8)

`LootCensus` ora conta anche il baule maledetto (`secrets: true`): prende il posto di una cassa normale scelta da `SecretRules.PickCursed`, con il bonus `ChestSecret.CursedBonus` e il livello degli oggetti della profondità dopo. Il test nuovo `SecretLootTests` misura 320 livelli (40 semi, profondità 1–8):

| | Oggetti a livello | Rari | Magici |
|---|---|---|---|
| Senza segreti | 4,99 | 16,6% | 30,2% |
| Con il baule maledetto | 5,39 | 16,1% | 31,0% |
| Fasce della M14 | 4,0–5,2 | 10–25% | 25–35% |

Il baule maledetto è in 60 livelli della cripta su 240 (uno su due dalla profondità 3, mai alle prime due: uno su quattro in tutto) e vale **una cassa normale più un oggetto sicuro**: +0,40 oggetti a livello. **Rari e magici restano nelle fasce; gli oggetti a livello le superano di 0,19.** Il baule è un premio che il cavaliere può saltare e che costa un'ondata, come Vorrak e il suo tesoro (D17 della M24): la fascia degli oggetti resta misurata **senza** segreti (`LootCensusTests`, 4,99), e i segreti hanno un tetto loro, **mezzo oggetto a livello** (`SecretLootTests`). Se si preferisse tenere il baule dentro la fascia, le strade sono due: il baule **sostituisce** il tiro di sempre invece di aggiungersi (circa +0,26, 5,25: ancora fuori) oppure compare in un livello su tre invece che su due (con la sostituzione, circa 5,16); nessuna delle due è stata fatta, e il numero resta a chi gioca. Gli unici restano al più uno per baule: il fattore degli unici è 1,5, sotto quello dei forzieri (3).

Non sono contati dalla misura, come nella M24: i nemici dell'ondata e dei segreti (lasciano il loot dei nemici normali) e il prigioniero (una pozione e oro).

### I tempi dei livelli (build locale, `-benchmark-levels 20`, seme 424242)

| Livello | Generazione | Costruzione | NavMesh | Totale medio | Il peggiore |
|---|---|---|---|---|---|
| Borgo | 0,0 | 8,5 | 9,8 | 18,2 ms | 19 ms |
| Cripta, profondità 1 | 4,7 | 5,7 | 9,2 | 19,6 ms | 37 ms |
| Cripta, profondità 4 | 4,8 | 6,4 | 10,2 | 21,4 ms | 25 ms |
| Caverne, profondità 8 | 2,5 | 4,7 | 10,1 | 17,2 ms | 25 ms |

Tutti sotto i 50 ms della D4 della M6 (alla M18: 15,7–17,5 ms, ora 17,2–21,4). Il tempo di generazione della cripta è salito da 2,8 a 4,8 ms: non è stato indagato, perché nessun segreto sta nel generatore (si decidono in `BindLevel`, e la mappa è la stessa, lo prova un test); può essere la macchina di oggi, e in ogni caso il totale sta a meno della metà del limite. Non serve aprire voci.

### I numeri di fine milestone

EditMode 762, PlayMode 440 (438 verdi, 2 saltati), e la ronda notturna intera in locale con il seme 424242: 13 test verdi, comprese le partite da sole a ogni difficoltà e la partita lunga di sei mondi.

### La chiusura

- ADR-149…154 in `DECISIONS.md`.
- Le lezioni nel piano (v2.81).
- Il README con la v1.13, *Da provare* con la sezione della M25, `bundleVersion` 1.13.0.
- Il tag `v1.13.0` dopo i Tests verdi (letta la conclusione, non solo la fine).

### Aperti

- I **quattro consigli del prigioniero** non hanno avuto una risposta di Mirco: restano i testi proposti.
- Il **baule maledetto sopra la fascia** (sopra): resta a Mirco la scelta tra tenerlo com'è (0,4 oggetti a livello in più, un premio che si può saltare) e riportarlo nella fascia.
- Il limite della camera isometrica (un prop contro un muro vicino si vede dietro il muro solo ruotando) resta quello dei forzieri e delle torce.
