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

**Da confermare da Mirco.** Le quattro scelte vere sono **D2** (che cosa entra: sei segreti o meno), **D5** (la leva e la stanza sigillata), **D9** (i testi: quanti e chi li scrive) e **D13** (il salvataggio).

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
| D13 | **Dove si ricorda** | **Nei ricordi dei livelli** (ADR-079), **senza un formato nuovo del salvataggio**: ogni livello ricorda per scena e profondità le **casse e i bauli aperti, le porte aperte, i prigionieri liberati, i testi letti**. **Da verificare** nel 25.1: se il ricordo (oggi solo nemici morti e casse aperte) vuole un campo in più, serve il **formato 12** con la sua fixture e la migrazione (come la M24 per l'interruttore) | Meglio un campo nei ricordi che un formato nuovo; si guarda il codice prima di decidere |
| D14 | **Cosa NON cambia** | La **generazione delle mappe e dei loot dei semi provati** (un flusso suo del seme per i segreti, solo ciò che nasce nuovo cambia); le **difficoltà**, la **misura** a margine, la macchina a stati dei nemici, il re e il Custode, i comandi, gli alberi; i test di ogni push con semi fissi **non si muovono** (un test lo dice: la stessa mappa con e senza segreti ha le stesse casse e gli stessi nemici) | Come per la M22–M24: tutto ciò che non è il tema resta fermo |
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
