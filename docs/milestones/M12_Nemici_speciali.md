# M12 — "Nemici speciali"

**Cosa deve succedere (Definition of Done):** scendendo si incontrano nemici più forti dei normali,
che si riconoscono da lontano e lasciano loot migliore. I **campioni** sono nemici normali resi più
forti da un dato, con un nome colorato, uno o due **modificatori** e una **scorta**. Gli **unici**
hanno un nome loro e un aspetto loro, uno per la cripta e uno per le caverne. Accanto a loro c'è un
**forziere**, e nella sala del re il **tesoro del re**, che si apre quando il re muore. La barra in
alto mostra il nome colorato e i modificatori. È il primo dei lavori dopo la v1.0 nell'ordine
consigliato dal piano (v2.39), e prepara le difficoltà: dall'Ombra in su il re prende i modificatori
dei campioni. Esce con il tag `v1.1.0`.

**Tempo stimato:** 9–13 h (D12).
**Prerequisito:** v1.0 uscita (tag `v1.0.0`, poi `v1.0.1` e `v1.0.2`).

**Come si lavora:** come alla M11: il codice e i passaggi nell'editor li faccio io, in batchmode a
Unity chiuso. A Mirco restano le decisioni qui sotto e la prova in build.

---

## Stato verificato il 18 ottobre 2026

| Cosa | Stato |
|---|---|
| Nemici | Cinque archetipi (`EnemyArchetype`): scheletro, mago, sciame, bruto, re. Vita e danno stanno nei prefab (`Health`, l'arma), uguali a ogni profondità: con la profondità cambiano solo quanti e quali (`SpawnTable`), e l'esperienza (`ProgressionSettings`) |
| Mappe | I nemici sono simboli della mappa: `S` scheletro e `m` mago dal `DungeonPopulator`, `w` sciame, `B` bruto, `K` re dal `CavePopulator`; il `LevelBuilder` mette il prefab del simbolo dal tileset |
| Loot | Una `LootTable` per tipo di nemico, una per le casse, una per il re (rarità minima: raro). Il seme di un oggetto viene dalla profondità e dalla cella di partenza (`SeedMixer.ForEnemy`); pozione e oro hanno domini loro |
| Casse | Un solo tipo, il modello `chest` del pacchetto KayKit *Dungeon*. Il pacchetto ha anche `chest_gold` (nello zip già scaricato, da estrarre) e i bauli `trunk_*` |
| Barra in alto | `EnemyBar`: nome dalla tabella delle stringhe e vita del nemico sotto il cursore o dell'ultimo colpito |
| Modelli | I sette scheletri di KayKit sono tutti già usati dai cinque prefab, in combinazioni diverse |
| Ricordi dei livelli | Contano i nemici morti per posto (ADR-079): un nemico che nasce dal seme nello stesso posto non vuole un formato nuovo del salvataggio |
| Test | 310 EditMode e 200 PlayMode verdi |

---

## Decisioni

Confermate da Mirco il 18 ottobre 2026: tutte come proposte.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | Ogni lavoro grande dopo la v1.0 è una milestone come le altre, con la sua scheda; questa è la **M12** e si chiude con il tag **`v1.1.0`** (le correzioni piccole restano `v1.1.x`) | Le schede e gli ADR hanno funzionato per undici milestone; il numero di versione dice ai tester che è arrivato del contenuto nuovo |
| D2 | **Campioni** | Possono esserlo scheletro, mago, sciame e bruto, mai il re. **Vita ×3, danno ×1,5, esperienza ×4**, il modello **un 15% più grande** e tinto di **blu**, il nome in blu con il nome del tipo ("Scheletro campione"). I numeri stanno in un asset `ChampionSettings` | Come i campioni di Diablo: si riconoscono dal colore e dalla misura, non servono modelli nuovi, e i numeri si tarano provando |
| D3 | **Quanti** | Cripta: **un gruppo di campioni a livello dalla profondità 2**, con il 40% di averne un secondo. Caverne: **due**. Mai nella stanza di partenza né nella sala del re | Uno a livello si nota senza diventare la normalità; alla profondità 1 si impara a giocare |
| D4 | **Scorta** | Il campione è un nemico già messo dal popolatore, promosso; la scorta sono **2–4 nemici normali in più dello stesso tipo** (sciame 4–6) nelle celle libere della sua stanza. Promozione e scorta si tirano con un **seme loro**, dopo tutto il resto: i nemici, le casse e gli arredi dei semi già provati restano dove sono | Un campione da solo dura poco; il seme a parte tiene buoni i test e i semi già provati, come per gli arredi della M11 |
| D5 | **Modificatori** | Cinque, in un asset ciascuno: **Rapido** (si muove e colpisce il 30% più in fretta), **Gelido** (il colpo rallenta il cavaliere del 40% per 2 s), **Rigenerante** (2% della vita al secondo, fermo se colpito da meno di 3 s), **Inamovibile** (i colpi non lo fanno barcollare e lo scudo non ne blocca i colpi), **Furioso** (sotto metà vita danno +50%). **Uno** fino alla profondità 4, **due** dalla 5; mai due uguali | Sono i quattro del piano più uno; ognuno cambia il modo di combattere, non solo i numeri. Altri si aggiungono come asset |
| D6 | **Unici** | Uno per tipo di livello, con nome e aspetto suoi: nella **cripta *Morvak il Carceriere***, uno scheletro guerriero più grande con lo scudo chiodato, tinto di ruggine; nelle **caverne *Grumm Spaccaossa***, un bruto più grande con l'ascia, tinto di verde palude. **Vita ×5, danno ×1,8, esperienza ×8**, due modificatori fissi, nome in **oro**, una scorta come i campioni. **Una possibilità su tre per livello**, e mai due volte nello stesso mondo una volta ucciso (si ricorda come i nemici morti) | Dà un nome a qualcuno da cercare; con due soli unici restano speciali. Il modello è un insieme di parti che ci sono già, con tinta e scala: niente modelli nuovi |
| D7 | **Loot** | Campione: **due tiri** della sua tabella invece di uno, con le rarità spostate (magico ×2, raro ×3 sui pesi della `RarityTable`), oro ×2,5. Unico: **tre tiri**, **almeno un raro**, oro ×4. Ogni tiro in più ha un **seme suo**: l'oggetto del primo tiro resta quello di un nemico normale nella stessa cella | Il piano chiede loot migliore e un raro sicuro dagli unici; i semi a parte non cambiano il loot dei semi provati |
| D8 | **Forzieri e tesoro del re** | Tre tipi di cassa, ognuno con la sua `LootTable` e il suo modello. La **cassa** di sempre. Il **forziere** (`chest_gold`) accanto a ogni unico e al 50% dei campioni: due oggetti, almeno uno magico, oro ×3. Il **tesoro del re** (un baule grande, `trunk_large`) chiuso in fondo alla sala del re, che si apre solo quando il re è morto: tre oggetti, almeno un raro, molto oro. Il tipo si tira a parte, così le casse dei semi già provati non cambiano | Sono le *casse di più tipi* dei tester (v2.38), che il piano voleva fare insieme ai nemici speciali; il re oggi lascia già un raro, il tesoro premia la vittoria |
| D9 | **Si vedono da lontano** | Un **anello luminoso sotto i piedi**, blu per i campioni e oro per gli unici, con un leggero bagliore; sull'automappa un punto del loro colore, solo quando sono stati visti | Senza, in una stanza buia un campione si scopre solo quando picchia |
| D10 | **Barra in alto** | Il nome nel suo colore, e sotto, in piccolo, i modificatori ("Rapido · Gelido"). Nelle due lingue | Il giocatore deve sapere perché quel nemico è diverso |
| D11 | **Fuori da questa milestone** | Il **boss a metà discesa** (resta da decidere dopo una prova), i **nemici che crescono con il personaggio** (vanno con le difficoltà), i **nemici che scappano** (un lavoro a parte). Gli oggetti unici e set restano dopo i gradi degli affissi, come nel piano | Tiene la milestone sulla cosa che si prova: incontrare e battere un nemico speciale |
| D12 | **Stima** | **9–13 h**. Se servirà tagliare: prima il Furioso, poi l'automappa dei punti colorati, poi il secondo unico | Meglio deciderlo adesso che a metà |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 12.1 | Logica pura: `ChampionSettings`, modificatori, promozione e scorta dal seme, con i test (D2–D5) | 2–3 |
| 12.2 | Popolatori: campioni e scorta nella cripta e nelle caverne, gli unici (D3, D4, D6) | 1,5–2 |
| 12.3 | In gioco: numeri, tinta, scala, anello e i cinque modificatori (D2, D5, D9) | 2–3 |
| 12.4 | Loot, forzieri e tesoro del re (D7, D8) | 1,5–2 |
| 12.5 | Barra in alto, stringhe, automappa (D9, D10) | 1 |
| 12.6 | Build da provare e correzioni | 1–2 |
| 12.7 | Chiusura: tag `v1.1.0`, ADR, lezioni, README | 1 |

## Passo 12.1 — Logica pura

**Com'è andata (18 ott 2026).** In `Scripts/Core/Enemies`: `EnemyRank` (normale, campione, unico),
`ChampionModifier` con il suo `ChampionModifierKind`, e `ChampionSettings` con i numeri dei due gradi
(`RankNumbers`: vita, danno, esperienza, scala, colore) e la lista dei modificatori. I cinque
modificatori sono asset in `Data/Enemies/Modifiers`, con i nomi nelle due lingue.

In `Scripts/Core/Levels`: le regole per livello (`SpecialEnemyRules`, nelle impostazioni della
cripta e delle caverne: un gruppo nella prima, due nelle seconde) e il pianificatore
(`SpecialEnemyPlanner`). Il pianificatore lavora sul livello già popolato e arredato, con un seme suo
(`SeedMixer.ForSpecials`): promuove nemici che ci sono già, mette l'unico (simbolo `u`) in una cella
libera, e la scorta solo su celle di pavimento libere vicine al capo, nella sua stanza se è nella
cripta. Quello che c'era resta dov'era. I campioni passano al builder con la direttiva
`@champions x,y,id,id` (`ChampionSpawn`).

**Una precisazione sulla D6.** "Una possibilità su tre per livello, e mai due volte nello stesso
mondo" si fa senza salvare niente: per ogni profondità, in ordine, un tiro dal seme della partita
(`SeedMixer.ForUnique`), e la prima che riesce è la sola dove sta l'unico (`UniqueDepth`). Nella
cripta, su tre profondità, manca in circa tre mondi su dieci; nelle caverne non sta mai al livello
del re. Ucciso, resta morto come ogni nemico del suo livello (ADR-079).

Test: `SpecialEnemyPlannerTests` (i livelli dei semi già provati non cambiano, campioni e
modificatori, gruppi, scorte, lontani da ingresso e re, stesso seme stesso risultato, l'unico e la
sua profondità, la direttiva) e `ChampionDataTests`. 324 EditMode verdi.

## Passo 12.2 — Popolatori e builder

**Com'è andata (18 ott 2026).** `DungeonLevel.CreateMap` chiama il pianificatore dopo il generatore,
con le regole e i modificatori delle impostazioni del livello (`DungeonSettings.Champions`), e scrive
i campioni nella direttiva `@champions`; l'unico c'è solo alla profondità che dice il seme. Il
`LevelBuilder` legge la direttiva e dà al nemico messo in quella cella un `SpecialEnemy` con il grado
e i modificatori. Per ora un campione si comporta e si vede come gli altri: numeri, tinta, anello e
modificatori arrivano al passo 12.3.

**Gli unici.** Due varianti di prefab, con il loro archetipo (copia di quello di partenza, con il
nome loro) e un `SpecialEnemy` con grado, modificatori e tinta fissi. **Morvak il Carceriere** viene
dallo scheletro, con lo scudo chiodato nella mano sinistra: Gelido e Inamovibile, tinta ruggine.
**Grumm Spaccaossa** viene dal bruto: Furioso e Rigenerante, tinta verde palude. Il simbolo `u` della
mappa li prende dal tileset: Morvak nella cripta, Grumm nelle caverne. Rispetto alla D6, Morvak parte
dallo scheletro e non dal guerriero: il guerriero è già il bruto, e così Morvak combatte come uno
scheletro, veloce e senza carica, mentre Grumm ha i colpi lenti e telegrafati del bruto.

**I salvataggi di prima.** I ricordi di un livello (ADR-079) valgono se il numero di nemici e di
casse è quello di quando è stato lasciato. Con le scorte un livello visitato prima della M12 ha più
nemici, e al ritorno riparte da capo, nemici e casse compresi. Succede una volta per livello, e solo
ai salvataggi di prima della v1.1.

La finestra del generatore nell'editor conta campioni e unico; l'automappa dell'editor colora l'unico
come gli altri nemici. Test: `SpecialEnemyMapTests` (mappa, prima profondità, unici dei tileset) e
`SpecialEnemyLevelTests` (nel livello costruito i campioni hanno i loro modificatori e Morvak è
l'unico); i test della discesa e delle caverne contano i nemici del popolatore, prima delle
scorte. 327 EditMode e 201 PlayMode verdi.

---

## Trappole note

- **Il seme.** Ogni cosa nuova si tira da un dominio suo del `SeedMixer` e dopo quello che c'è già:
  un campione non deve spostare il loot o le casse di un seme già provato, e i test che contano i
  nemici di un seme fisso vanno guardati uno per uno.
- **La scorta e il percorso tra le porte.** I nemici in più vanno solo nelle celle libere, mai sul
  percorso tra le porte né davanti alle casse, con le regole degli arredi della M11.
- **La tinta.** I modelli KayKit condividono un materiale: la tinta va con un `MaterialPropertyBlock`,
  non cambiando il materiale, o diventano blu tutti gli scheletri.
- **La scala.** Un modello più grande vuole anche il raggio dell'agent e il collider più grandi, o
  un campione si incastra nelle porte.
- **Gelido e la velocità.** La `StatSheet` non ha una velocità di movimento: il rallentamento è un
  componente suo sul cavaliere, l'unico che cambia la velocità dell'agent, con una durata. Un secondo
  colpo rinnova la durata senza sommarsi, e alla fine la velocità torna quella di prima.
