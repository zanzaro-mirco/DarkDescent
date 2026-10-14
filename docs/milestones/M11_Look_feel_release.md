# M11 — "Look, feel e release"

**Cosa deve succedere (Definition of Done):** la **v1.0** è pubblicata su **itch.io** dalla CI. Il
gioco ha un aspetto suo e coerente: uno **shader retro** a tutto schermo e un post-processing
misurato tengono insieme asset di pacchetti diversi. Le stanze della cripta e delle caverne sono
**arredate**, l'audio è rifinito, e una partita completa gira fluida su un PC normale, misurata con il
**Profiler** in build. Il README ha la GIF finale e una sezione sull'architettura; `CREDITS.md` è
completo.

**Tempo stimato:** 20–30 h (D11, piano v2.31).
**Prerequisito:** M10 chiusa (tag `m10`).

**Come si lavora:** come alla M10, il codice e i passaggi nell'editor li faccio io, in batchmode
a Unity chiuso. A Mirco restano le decisioni qui sotto, le prove in build, la pagina di itch.io con
la sua chiave e la revisione degli ADR. Dopo la prima build da provare c'è un passo per le correzioni.

---

## Stato verificato il 12 ottobre 2026

| Cosa | Stato |
|---|---|
| Rendering | URP 17.3, renderer `PC_Renderer` con la sola Renderer Feature del template (SSAO). Nessuno shader né Shader Graph del progetto; il pacchetto Shader Graph 17.3 c'è già, come dipendenza di URP |
| Post-processing | Ogni livello costruito ha un Volume dal suo tileset (`LevelBuilder`), con tonemapping ACES, bloom (soglia 0,9) e vignetta; cripta e caverne condividono lo stesso profilo, l'accampamento ne ha uno uguale a parte. Niente grana né colore per livello. *(Corretto al passo 11.1: la prima stesura diceva che i livelli usavano il profilo del template.)* |
| Arredi | Cripta: colonne, casse, torce e stendardi, 42 modelli del pacchetto KayKit *Dungeon* su 211. Caverne: rocce e colonne. L'accampamento ha già i suoi mobili (M10) |
| Audio | Musica e rumori d'ambiente per cripta, caverne e accampamento; mixer `Main` con musica ed effetti. Lo scontro con il boss non ha una musica sua |
| Prestazioni | Allocazioni a ogni frame misurate a 0 byte nell'editor (M9). Costruzione del livello sotto i 50 ms alla M6. Mai misurato con il Profiler collegato a una build |
| Versione | `bundleVersion` 0.1.0; la CI non legge la versione dal tag. Nessun file `LICENSE` nel repo |
| CI | Test a ogni push, build Windows sui tag `m*`/`v*`, Web ad avvio manuale. Niente butler né itch.io |
| README | Una GIF per milestone fino alla M9; manca quella della M10 e la sezione sull'architettura |
| Test | 291 EditMode e 192 PlayMode verdi |

---

## Decisioni

Confermate da Mirco il 12 ottobre 2026: tutte come proposte, tranne la D8 (tutti i diritti riservati invece della MIT) e la D9 (prima una beta ristretta).

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Correzioni dalla prova della M10** | Per prime: quello che esce dalla build provata da Mirco e dalla prova a cinque persone (piano, M10), comprese le tarature rimaste aperte (oro e prezzi, vita e danno del boss, misura del portale). Le pergamene del ritorno impilabili restano nell'`ICEBOX` (superata al passo 11.7: la pergamena è diventata una magia) | Sono le note su un gioco già finibile: vengono prima dell'aspetto |
| D2 | **Shader retro** | Un passo a tutto schermo con la *Full Screen Pass Renderer Feature* di URP e un materiale Shader Graph: **palette limitata** da una texture di 32 colori scuri, caldi per fuochi e oro, freddi per l'ombra; **dithering ordinato** 4 × 4 tra un colore e l'altro. Senza abbassare la risoluzione di default. Un'**opzione** *Filtro retrò* (spento, leggero, pieno) nel menu delle opzioni, su *leggero* all'avvio (*spento* dal passo 11.7, scelta di Mirco) | Dà a KayKit, Quaternius e alle caverne una pelle comune, come chiede il piano. La pixelatura sull'interfaccia rende illeggibili i testi in Cinzel, ed è per questo che resta fuori di default |
| D3 | **Post-processing** | Un Volume globale in Core con un profilo nostro: tonemapping, vignetta, grana leggera, bloom con una soglia alta (solo fuochi, portali, incantesimi). Ogni `LevelTileset` porta un profilo suo per il colore (cripta fredda, caverne verdastre, accampamento caldo), sostituito entrando nel livello | Oggi è il profilo del template, uguale ovunque. Il profilo nel tileset segue la luce ambiente, che sta già lì |
| D4 | **Dungeon più ricco** | Come nel piano v2.26: barili, tavoli, scaffali, libri, ossa, ragnatele, catene, sacchi e detriti, lungo i muri e negli angoli, mai sul percorso tra le porte né sulle scale. Un **tema per stanza** (deposito, biblioteca, ossario, cella) scelto dal seme. Caverne: funghi, cristalli e rocce del pacchetto, con una lista loro. Quello che blocca il passo entra nel NavMesh; il resto senza collider. Si misura la costruzione: sotto i 50 ms | Lo chiede Mirco; i modelli sono già scaricati, e il popolatore della M6 ha già le regole su porte e scale |
| D5 | **Audio** | Una **musica per lo scontro con il boss** (CC0 da OpenGameArt, scaricata con il permesso di Mirco), che parte con la chiusura della porta e sfuma alla vittoria. Suoni mancanti: click nei menu, compravendita, salita di livello. Un giro di volumi su tutto | Il boss è il culmine della partita e oggi suona come un livello qualunque |
| D6 | **Prestazioni** | Una **build di sviluppo** dalla CI (un parametro dell'avvio manuale) con il Profiler collegato sul PC di Mirco: frame a 1080p nella cripta, nelle caverne piene e nello scontro con il boss, con lo shader acceso. Obiettivo **60 fps**, nessuna allocazione a ogni frame, livello costruito sotto i 50 ms. Quello che non regge si corregge nello stesso passo | Il piano chiede il profiling completo, e le misure della M9 erano nell'editor |
| D7 | **Versione 1.0** | `bundleVersion` **1.0.0**, la CI la prende dal tag `v*` (`versioning: Tag`). Il tag `v1.0.0` è l'ultimo passo e fa partire build e pubblicazione. Il titolo del menu mostra la versione in piccolo | Un numero di versione nel gioco serve per le segnalazioni dei giocatori |
| D8 | **Licenza del codice** | **Tutti i diritti riservati** (scelta di Mirco, 12 ott 2026, al posto della MIT proposta): un file `LICENSE` con «Copyright © 2026 Mirco Zanzaro. Tutti i diritti riservati.», e una sezione del README che lo dice e ricorda che gli asset di terzi restano con le loro licenze, elencate in `CREDITS.md`. Il repo resta pubblico: si legge, ma il codice non si riusa né si ridistribuisce senza permesso | Il repo fa da portfolio e va letto; Mirco non vuole che il codice o il gioco si riusino. Senza una dicitura chi legge non sa cosa può fare |
| D9 | **itch.io, prima in beta** | Mirco crea la pagina e una chiave di butler e la mette tra i secrets (`BUTLER_API_KEY`). La CI, sui tag `v*`, dopo la build Windows la carica con butler sul canale `windows`. La v1.0 esce prima in modalità **ristretta**, con una **chiave di download** per ogni tester scelto da Mirco, revocabile; la pagina diventa pubblica quando Mirco lo decide, senza toccare la CI (scelta di Mirco, 12 ott 2026). Niente canale Web alla v1.0: il salvataggio nel browser e le prestazioni andrebbero riprovati da capo (ADR-019) | Una beta chiusa raccoglie le note di persone scelte prima dell'uscita pubblica; le chiavi dicono chi ha accesso, una password si passa ad altri. Il canale Web può arrivare con una v1.1 |
| D10 | **README e GIF** | La GIF della M10 (città, mercante, portale, boss) e quella finale della v1.0 in testa al README. Una sezione **Architettura** breve: le scene, la logica pura in `Core`, il composition root, gli eventi, il seme, il salvataggio versionato, con i link agli ADR | Il piano lo chiede per la v1.0, e per il portfolio è la pagina che si legge per prima |
| D11 | **Stima** | **20–30 h**, da 18–29: un'ora in più per l'opzione del filtro e per la musica del boss. Se servirà tagliare: il canale Web non c'è già; poi i temi delle stanze (resta la decorazione lungo i muri, circa 1 h in meno), poi il dithering (resta la palette) | Meglio deciderlo adesso che a metà milestone |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 11.0 | Correzioni dalla prova della M10 (D1) | 2–4 |
| 11.1 | Post-processing: Volume in Core, profili per livello (D3) | 1,5–2 |
| 11.2 | Shader retro: palette, dithering, opzione nel menu (D2) | 3–5 |
| 11.3 | Cripta arredata: temi delle stanze, regole del popolatore (D4) | 3–4 |
| 11.4 | Caverne arredate e misura della costruzione (D4) | 1,5–2,5 |
| 11.5 | Audio: musica del boss, suoni mancanti, volumi (D5) | 1,5–2 |
| 11.6 | Profiling in build e correzioni (D6) | 2–4 |
| 11.7 | Build da provare e correzioni | 1,5–2 |
| 11.8 | Versione 1.0, `LICENSE`, README, `CREDITS.md`, GIF (D7, D8, D10) | 1,5–2 |
| 11.9 | itch.io con butler: pagina ristretta e chiave di Mirco, job della CI, chiavi dei tester (D9) | 1 |
| 11.10 | Chiusura: tag `v1.0.0`, pubblicazione, ADR, lezioni | 1–1,5 |


## Passo 11.1 — Post-processing

**Com'è andata (12 ott 2026).** Il Volume per livello c'era già dalla M6: il `LevelBuilder` lo crea
con il profilo del tileset, e così i colori cambiano con il livello senza un Volume in Core. Il passo
è stato più corto del previsto.

- **Le caverne hanno un profilo loro** (`CavePostProcessing`), copiato da quello della cripta.
- **A tutti e tre** si aggiungono la grana (*Thin1*, intensità 0,18) e un colore: la cripta fredda
  e un po' spenta (temperatura −14, saturazione −12), le caverne verdastre (tinta −25, temperatura
  −6), l'accampamento caldo (temperatura +10, saturazione +6). Contrasto +5…+10. Bloom, tonemapping
  e vignetta restano com'erano: il bloom a soglia 0,9 prende solo fuochi, candele e portali.
- I numeri sono da guardare in build: negli screenshot del batch la grana a quella intensità quasi
  non si vede, e il verde delle caverne è tenue apposta.

*Test.* `PostProcessingTests` (1, EditMode): ogni tileset ha tonemapping, bloom sopra 0,9, vignetta,
grana leggera, bilanciamento del bianco e colore; la cripta fredda, le caverne verdastre,
l'accampamento caldo, e le caverne con un profilo loro. `CaveDescentTests`: il Volume delle caverne
usa il loro profilo. 292 EditMode verdi.

## Passo 11.2 — Filtro retrò

**Com'è andata (12 ott 2026).**

- **Il passo a tutto schermo** è una *Full Screen Pass Renderer Feature* di URP sui due renderer
  (PC e Mobile), dopo il post-processing: legge l'immagine e la riscrive. L'interfaccia è disegnata
  dopo, in overlay, e resta nitida.
- **Lo shader** (`Art/Shaders/Retro.shader`) è scritto a mano in HLSL e non in Shader Graph come
  diceva la D2. Il calcolo è un ciclo su 32 colori con una distanza pesata, che in Shader Graph
  sarebbe stato un nodo *Custom Function* con lo stesso codice dentro, e un grafo non si scrive
  bene da uno script. Per ogni pixel: l'immagine passa in sRGB, si sposta della soglia di Bayer 4 × 4
  del suo pixel sullo schermo (il motivo non segue il mondo, quindi resta fermo quando la camera si
  muove o gira) e va al colore più vicino della palette; poi si mescola con l'originale secondo la
  forza.
- **La palette** (`Data/RetroPalette.asset`, `RetroPalette`) ha 32 colori: dieci grigi dal nero al
  bianco caldo, bruni e aranci per fuochi e oro, rosso, blu freddi, verdi delle caverne, due viola. Nel
  primo giro il fuoco dell'accampamento sfumava in un anello rosso scuro: due rossi scuri sono
  diventati bruni caldi, e ora la luce scende per aranci e bruni.
- **`RetroFilter`** in Core regola lo shader con proprietà globali (palette, forza, dithering), così
  nessun asset cambia giocando e fuori dal gioco l'immagine resta com'è. *Spento* lascia passare
  l'immagine; *leggero*, la scelta di partenza, mescola al 60% con metà dithering; *pieno* porta tutto
  alla palette. Senza palette resta spento invece di annerire tutto.
- **Nelle opzioni** un pulsante *Filtro retrò: leggero* gira le tre scelte e le ricorda tra le
  preferenze; il riquadro è cresciuto di una riga.
- *Trappola ritrovata* (la stessa del 10.5): lo script dell'editor creava la palette e poi apriva la
  scena Core, e il riferimento alla palette restava vuoto: il primo screenshot del filtro pieno era tutto
  nero. Ricollegata dopo l'apertura.

*Test.* `RetroFilterTests` (3, EditMode): la palette ha 32 colori diversi con il nero, e il più vicino a
un grigio è un grigio; le scelte girano, un valore sbagliato vale leggero, i nomi ci sono nelle due
lingue; i due renderer hanno il passo acceso, con il materiale, dopo il post-processing.
`RetroFilterPlayModeTests` (2): con il filtro pieno ogni pixel della cripta è un colore della palette,
spento l'immagine ne ha molti di più, e si parte da leggero; il pulsante delle opzioni gira le scelte,
lo dice e le ricorda dopo aver riaperto il gioco. 295 EditMode e 194 PlayMode verdi.

## Passo 11.3 — Cripta arredata

**Com'è andata (12 ott 2026).**

- **Composizioni, non oggetti singoli.** Una cella della cripta è di 4 m: un barile da solo la
  occupa quasi vuota. Ogni arredo è una composizione di due o quattro modelli addossata al muro,
  dentro la sua cella, con il retro sul muro (a 1,45 m dal centro) e il davanti verso la stanza, come
  le casse. Dodici, tre per tema: **deposito** (barili, casse, botti), **studio** (tavolini con candele
  e attrezzi, sedie, sgabello, bottiglie, un baule), **cella** (letti, bauli, sgabello, candela consumata),
  **rovina** (macerie, tavoli rotti, scudo con le spade spezzate). Ogni modello solido ha un collider sul
  layer degli ostacoli, quindi il NavMesh ci gira attorno; candele e bottiglie no.
- **Ventun modelli nuovi** dal pacchetto KayKit *Dungeon* già in Downloads, la versione per Unity,
  importati senza materiali e con `M_Dungeon` come gli altri. Le misure dei modelli erano lontane dal
  previsto: le macerie grandi sono lunghe 8 m, le casse impilate 3,5 m. Si sono scalate (0,35 e 0,55).
  Le mensole e gli scaffali del pacchetto si appendono al muro: con la rotazione della visuale
  (ADR-052) qualunque muro può diventare basso, e resterebbero a mezz'aria. Sono rimasti fuori, e
  la "biblioteca" della D4 è diventata uno studio: il pacchetto gratuito non ha librerie da terra, né
  ossa, ragnatele o catene.
- **Nel popolatore** (`DungeonPopulator.Decorate`): dopo tutto il resto, con un flusso suo del seme
  (`SeedMixer.ForDecor`), ogni stanza prende un tema e una composizione ogni sei celle di lato fino a
  tre (`DungeonSettings.MaxDecorPerRoom`), sul bordo, mai accanto a un passaggio e con il controllo che
  ogni cella resti raggiungibile, come gli oggetti di scena della M6. I simboli sono dodici maiuscole
  libere (`DEF`, `LNO`, `RUV`, `WYZ`) e contano come ostacoli anche per l'automappa. Tra 18 e 29
  composizioni per livello; la costruzione resta sotto i 50 ms (generazione, costruzione e NavMesh
  fanno 22–26 ms, 48 al primo livello con l'avvio a freddo).
- Nemici, casse e mappa dei semi già provati non cambiano: il test lo controlla togliendo gli arredi.
  Non entrano nei ricordi dei livelli (ADR-079), che contano solo nemici e casse.

*Test.* `DecorTests` (3, EditMode): su 400 cripte, tolti gli arredi la cripta è uguale cella per cella
a quella generata senza; ogni stanza ha un tema solo, le composizioni stanno sul bordo e sono almeno
una ogni due stanze; ogni composizione ha il prefab nel tileset, sta nella sua cella e sopra il
pavimento, con i collider sul layer degli ostacoli e il materiale del dungeon. 298 EditMode e 194 PlayMode verdi (due giri completi).

## Passo 11.4 — Caverne arredate

**Com'è andata (12 ott 2026).**

- **Nove composizioni, tre temi**: **cristalli** (gruppi di cristalli azzurri alti fino a 2,5 m, uno
  tra le macerie), **ossa** (teschi e ossa sparse, uno scudo con le spade spezzate accanto a un baule,
  candele consumate), **frane** (macerie grandi e mezze, un barile rovesciato). Come nella cripta, il
  retro contro la roccia e il davanti verso il pavimento, con i collider sul layer degli ostacoli solo
  per i pezzi grossi: ossa, teschi piccoli e candele si scavalcano.
- **I modelli.** Il pacchetto *Dungeon* non ha né funghi né cristalli né ossa: cristalli, teschi e
  ossa vengono dall'*Ultimate RPG Items Pack* di Quaternius (CC0, lo stesso di anello, guanto e
  pergamena), estratto dallo zip già in Downloads. Nove modelli, importati senza materiali; hanno
  l'origine al centro e sono lunghi pochi millimetri con una scala di 100 nel file, quindi le
  composizioni li appoggiano a terra misurandone i contorni. Due materiali nuovi: `M_CaveCrystal`
  (azzurro con un'emissione leggera: i cristalli si vedono anche fuori dalla luce del cavaliere) e
  `M_Bone`. **I funghi della D4 non ci sono**: nessuno dei pacchetti scaricati li ha, e uno nuovo
  vorrebbe un download.
- **Nel popolatore** (`CavePopulator.Decorate`): le caverne non hanno stanze, così il tema va per
  **zone** di 7 × 7 celle, scelto dal seme; una composizione ogni 20 celle di pavimento
  (`CaveSettings.FloorPerDecor`), ad almeno 3 celle l'una dall'altra (`DecorSpacing`), contro la
  roccia, mai in un cunicolo, mai accanto a casse, scala o porta del boss, mai nella sala del boss; se
  una composizione chiude il cammino si toglie. Le celle riservate (scala, ingresso, sala del boss)
  sono passate in `Reserved`, condiviso con `Populate`. Stesso flusso del seme degli arredi della
  cripta (`SeedMixer.ForDecor`), dopo tutto il resto: nemici, casse, candele e boss non cambiano.
- **Simboli**: `ACH` (cristalli), `IJM` (ossa), `PQX` (frane), maiuscole che non usano né la cripta
  né le caverne; contano come ostacoli (`DungeonPopulator.IsObstacle`), anche per l'automappa.
- **Misure.** Tra 10 e 14 composizioni per caverna, 12,6 in media su 400 caverne. Generazione,
  costruzione e NavMesh fanno 25–31 ms per caverna, sotto i 50 ms della D4.

**Fuori passo: la CI rossa dall'11.2.** `RetroFilterPlayModeTests.Full_UsesOnlyThePalette` passava in
locale (anche con OpenGL forzato) e falliva in CI con 53 576 pixel su 57 600 fuori palette. La prima
ipotesi era la scrittura in una texture sRGB senza conversione: il test ora legge da una texture float
lineare e converte da sé (`4dbf005`), ma la CI è rimasta rossa. Il messaggio nuovo ha detto il resto:
la CI disegna con **llvmpipe** (OpenGL in software, Mesa) e i pixel sono quelli grezzi della cripta,
(2, 2, 3) e simili: lì il passo a tutto schermo non si vede proprio nell'immagine. Sulle schede vere,
D3D11 e OpenGL, il controllo passa, e il gioco esce per Windows (D9): su llvmpipe la parte dei pixel
del test si ignora, con nel motivo quello che serve a capire il perché (shader supportato, forza
globale); tutto il resto del test vale ovunque.

*Test.* `CaveDecorTests` (4, EditMode): su 400 caverne (profondità 5–8, sala del boss compresa), tolti
gli arredi la caverna è uguale cella per cella a quella generata senza; almeno 6 composizioni, contro
la roccia, mai in un cunicolo, mai accanto a casse, scala o porta, lontane tra loro e fuori dalla sala
del boss, una zona un tema, e dall'ingresso si arriva a ogni cella e a ogni cassa; ogni composizione
ha il prefab nel tileset delle caverne, dentro la cella e sopra il pavimento, con i materiali delle
caverne; i simboli non sono già usati. 302 EditMode e 194 PlayMode verdi (due giri completi).

## Passo 11.5 — Audio

**Com'è andata (12 ott 2026).**

- **La musica del boss**: *Epic Boss Battle [Seamlessly Looping]* di Juhani Junkala (CC0, da
  OpenGameArt, scaricata con il permesso di Mirco), orchestrale, 123 s in loop. Resta il WAV originale
  (21,8 MB, in LFS): qui non c'è un convertitore, e nella build Unity lo comprime in Vorbis come gli
  altri brani, in streaming. Entra come un **profilo d'ambiente** (`AmbienceBoss`): stesso fondo della
  caverna, nessun verso nel buio, musica a 0,4. Il brano è masterizzato forte (RMS −8,5 dB contro
  −14,6 della musica dell'accampamento), e a 0,4 resta un po' sopra quella. Il composition root lo
  passa all'`AmbiencePlayer` su `FightStarted` (la porta si chiude nello stesso momento) e rimette il
  profilo del livello su `Defeated` e su `FightReset`: le due musiche sfumano l'una nell'altra in 2,5 s,
  come tra due livelli. Nessun codice nuovo nel lettore.
- **Il click dei menu** (`UiClickSound`): un colpo di legno di 90 ms sintetizzato con uno script
  Python, come gli altri suoni nostri. All'avvio prende i pulsanti delle due tele della scena Core
  (HUD e menu), anche quelli dei pannelli spenti, e li sgancia alla distruzione. Le etichette degli
  oggetti a terra restano fuori, e gli slot di inventario, cintura e mercante non sono `Button`:
  hanno già i loro suoni, niente doppioni. La sorgente è 2D sul gruppo UI del mixer e
  **ignora la pausa dell'audio**, che il menu di pausa accende.
- **Compravendita e salita di livello avevano già un suono** (le monete del mercante dalla M10,
  l'arpeggio dalla M8): la D5 li dava per mancanti.
- **Il giro dei volumi**, misurato: uno script dell'editor ha letto picco, RMS e i 100 ms più forti
  di ogni clip breve. I più bassi rispetto agli altri, a parità di volume della sorgente (quasi tutte
  a 1), erano il ruggito del boss, la porta della sala, il sorso di pozione e le monete raccolte:
  alzati di 7, 7, 6 e 4 dB direttamente nei file (WAV a 32 bit float), con il picco sotto −0,8 dB. Il
  resto sta tra −8 e −20 dB nei 100 ms più forti, con i passi in fondo come deve essere. Un verso dei
  nemici (`grunt_02`) è 5–9 dB sopra gli altri: è un OGG, e qui non c'è un modo di riscriverlo; da
  sentire nella prova. L'ascolto vero resta alla build del passo 11.7.

**Dalla CI, sul filtro retrò**: il test ignorato su llvmpipe ha riportato *shader supportato, forza 1*:
lo shader c'è e la forza arriva, è il passo a tutto schermo a non agire su quel renderer.

*Test.* `UiClickSoundTests` (2, PlayMode): i pulsanti dei menu fanno click, anche quelli del menu di
pausa spento all'avvio, con una sorgente 2D sul gruppo UI; con il gioco in pausa l'audio del mondo si
ferma e il click no. `BossPlayModeTests.BossMusic_EntersAndFadesOut`: con la porta chiusa suona il brano
dello scontro, senza versi; vinto, torna la caverna. `VictoryTests`: i crediti con le righe nuove (il titolo del brano ha le parentesi tonde: le quadre i riconoscimenti le leggono come un link). 302 EditMode e 197 PlayMode verdi (due giri completi).

## Passo 11.6 — Profiling in build

**Com'è andata (13 ott 2026).**

- **La build di sviluppo dalla CI**: l'avvio manuale di `Build` ha un'opzione `development`. Lo
  script di build di GameCI accende le `BuildOptions` passate come parametri con lo stesso nome, e
  basta `-Development`; l'artefatto prende il suffisso `-dev`. Sui tag la build resta normale.
- **Una misura che gira da sola** (`Benchmark`, con i conti in `FrameStats` nel Core):
  `DarkDescent.exe -seed 1 -benchmark risultati.txt` fa camminare e combattere il cavaliere per 20 s
  in città, nella cripta alle profondità 2 e 4, nelle caverne alla 6 e contro il re alla 8, senza
  che muoia; la vita del re scende a tempo per passare dalle tre fasi. Per ogni tratto una riga:
  frame medi, p95, p99 e massimi, main thread, allocazioni (solo nella build di sviluppo), batch,
  SetPass e triangoli. Spegne il vSync per misurare il margine, e i collider delle uscite: al primo
  giro il cavaliere era entrato nella cripta camminando a caso per la città. `-benchmark-seconds` e
  `-benchmark-fps` accorciano e rallentano la misura, per una registrazione del Profiler
  (`-profiler-enable -profiler-log-file`) che entri nella storia dell'editor.
- **Le misure**, sul PC di Mirco (Core Ultra 9 275HX, RTX 5070 Ti Laptop, D3D12, 1920×1080, filtro
  retrò acceso), build normale:

  | Tratto | fps medi | p99 | massimo | batch | triangoli |
  |---|---|---|---|---|---|
  | Città | 680 | 2,0 ms | 2,8 ms | 427 | 130k |
  | Cripta, profondità 2 | 759 | 2,0 ms | 8,8 ms | 321 | 90k |
  | Cripta, profondità 4 | 693 | 2,0 ms | 4,7 ms | 430 | 144k |
  | Caverne, profondità 6 | 697 | 2,0 ms | 8,6 ms | 460 | 206k |
  | Re delle ossa | 659 | 2,1 ms | 22,7 ms | 513 | 242k |

  I 60 fps su questa macchina sono lontanissimi; quanto margine resta su un PC più modesto lo
  diranno la prova del passo 11.7 e quella con le cinque persone. I livelli si costruiscono in
  14–30 ms tra generazione, costruzione e NavMesh, sotto i 50. Il massimo di 22 ms contro il re è un
  frame solo: a 60 fps un frame perso, da riguardare con il Profiler se nella prova si sente.
- **Nessuna allocazione a ogni frame.** Nella build di sviluppo allocano da 4 a 27 frame su 11–13
  mila per tratto, 72 contro il re. La registrazione del Profiler, letta con uno script dell'editor,
  dice da dove: caricamenti dei livelli e `Awake`, la prima compilazione JIT di Mono, i testi TMP e
  il pool dei numeri di danno che crescono le prime volte, URP che aggiunge i dati delle luci alle
  torce al primo frame di un livello, i `-=` negli `OnDisable` dei nemici distrutti e circa 450 B a
  colpo negli handler dei colpi. Tutte legate a un evento: nessuna da correggere.
- **La correzione: il vSync.** La qualità PC lo aveva spento, e nessuno metteva un tetto agli fps: il
  gioco girava a 600–700 fps con la GPU al massimo per niente, con ventole e consumi da portatile
  sotto sforzo. Ora è acceso; la misura lo spegne solo per sé e lo scrive nel resoconto.

*Test.* `FrameStatsTests` (5, EditMode): media e percentili al rango più vicino, allocazioni,
contatori mancanti e `Reset`, frame oltre la capacità, e aggiungere un frame non alloca.
`QualitySettingsTests` (1, EditMode): la qualità PC ha il vSync. 308 EditMode e 197 PlayMode verdi.

## Passo 11.7 — Build da provare e correzioni

**Com'è andata (14 ott 2026).** Mirco ha provato la build normale dalla CI (run 38471688991); quella
di sviluppo (run 38474083466) è pronta per il Profiler, se nella prova qualcosa scatta. Le sue note,
e cosa è cambiato:

- **Il filtro retrò parte spento.** Con il filtro la luce delle torce si spezza in bande, e a Mirco
  non piace: l'opzione resta, ma *spento* è la scelta di partenza (D2 corretta). Chi l'aveva già
  cambiato tiene la sua scelta.
- **La pergamena del ritorno diventa una magia.** Il portale per l'accampamento lo apre la *magia del
  portale* (`SpellKind.Portal`), in un posto suo a sinistra della cintura, con il tasto **T** o un
  click: 10 di mana, nessuna ricarica, sempre nota e fuori dai posti F1–F3. Il portale e il ritorno
  sono quelli della M10 (`TownPortals`); la magia chiede prima se lì si può (`IPortalOpener`):
  - **in città** non parte, e l'HUD dice "Sei già all'accampamento";
  - **nella sala del re vivo** non parte, e l'HUD dice "Il Re delle ossa non ti lascia fuggire":
    durante lo scontro ovunque, prima dello scontro dentro il quadrato della sala (`BossController.
    HoldsPlayerAt`, entro 10,5 m dal centro sui due assi: la sala è di 20 m, la porta a 12). Morto il re, si apre;
  - **senza mana** suona sordo e il posto è già scuro, come per gli altri incantesimi.

  La pergamena è sparita del tutto: oggetto, icona, drop di nemici, casse e re, il banco del mercante
  (ora due pozioni e otto oggetti), quella di partenza, la riga del tooltip, il suo dominio del seme.
  Un salvataggio di prima con una pergamena nello zaino la perde al caricamento, come ogni oggetto che
  il database non conosce più: nessun salvataggio è ancora uscito dal PC di Mirco. L'avviso dello
  zaino pieno è diventato `HudMessage`, che dice anche perché il portale non si apre.
- **Vita e mana sulle sfere.** Con il mouse sopra una sfera compare quanto resta, "45 / 80"
  (`OrbReadout`): arrotondato per eccesso, così chi è vivo non legge mai 0; il testo si riscrive sui
  cambi della sfera, solo mentre si vede.
- **Più arredi, e non solo contro i muri.**
  - *Composizioni*: cinque per tema invece di tre. Con colonne e mucchietti entrano quindici modelli
    nuovi del pacchetto KayKit *Dungeon* già in Downloads. Le otto nuove: forzieri impilati e barili, il tavolo con le bottiglie e la
    botte, la tavola apparecchiata con le sedie, il tavolino con il pranzo e le monete, il giaciglio
    a terra, il forziere con le chiavi, lo scudo con le spade appoggiato al muro, la botte rovesciata.
    Una ogni quattro celle di lato invece che sei, fino a quattro per stanza.
  - *Colonnati* (`DungeonPopulator.ColumnCells`): nell'85% delle stanze di almeno 5 × 5 celle, due
    file di colonne decorate a una cella dai muri, lungo il lato lungo, una ogni due celle. Il
    colonnato resta simmetrico: una colonna che non può stare (accanto a un passaggio, davanti a una
    cassa, accanto alla scala) porta via le sue speculari, e con meno di due colonne non c'è.
  - *Mucchietti a terra*: ossa e teschi, cianfrusaglie (piatti, bottiglie rovesciate, chiavi, una
    candela), armi rotte; fino a due per stanza, di preferenza lontano dai muri, senza collider. Le
    monete sono rimaste fuori: a terra sembrano oro da raccogliere.
  - Con il seme di una partita di prova, nelle quattro profondità della cripta: da 26 a 32 arredi
    per livello, colonne comprese, e da 7 a 14 mucchietti; prima erano da 18 a 29 composizioni. Le
    caverne non cambiano. Generazione, costruzione e NavMesh fanno 22–25 ms, e 53 al primo livello
    con l'avvio a freddo dell'editor (erano 48): sopra i 50 della D4 solo lì, e nella build il
    benchmark dell'11.6 misurava 14–30 ms. Da rimisurare in build alla prossima prova.
- **Per dopo la v1.0** (piano v2.34): un albero delle abilità, con lo stile da scegliere tra Diablo 2,
  Sacred e World of Warcraft; i barili da rompere anche in mezzo alle stanze. Il movimento con WASD
  c'era già, dalla v2.25.

*Test.* `PortalSpellTests` (EditMode, al posto di `ReturnScrollTests`): la magia ha tipo, icona, costo
e nome nelle due lingue, il cavaliere la ha fuori da F1–F3; la pergamena non c'è più da nessuna parte.
`PortalTests` (PlayMode): nessuna pergamena all'inizio, in città la magia non parte e l'HUD lo dice, T
apre il portale e spende il mana, un secondo lancio lo sposta, senza mana non parte e il posto è
scuro. `BossPlayModeTests.Portal_IsRefusedInTheBossRoom`: rifiutato nella sala prima e durante lo
scontro, aperto morto il re. `DecorTests`: colonnati solo nelle stanze grandi, simmetrici, mai accanto
a passaggi, casse o scala e frequenti; mucchietti quasi tutti in mezzo e attraversabili; ogni arredo
ha il suo prefab, con i collider giusti. `RetroFilterTests` e `RetroFilterPlayModeTests`: si parte da
spento. 310 EditMode e 198 PlayMode verdi; in un giro `BossMusic_EntersAndFadesOut` è caduto perché con quel seme la sala del re era uscita senza porta (il generatore lo ammette), e rilanciato è passato. Il test dava la porta per certa: ora i test del boss provano i semi da 1 in su e tengono il primo con la porta, e girano sempre sulla stessa sala.

---

## Trappole note

1. **Lo shader retro tocca anche l'interfaccia** se la UI è disegnata dalla camera: la tela dell'HUD è
   in overlay, ma i test degli screenshot la passano alla camera. Il passo a tutto schermo va prima
   della UI, e gli screenshot dei test vanno ricontrollati.
2. **Il dithering sfarfalla quando la camera si muove** se il motivo segue il mondo: va legato ai pixel
   dello schermo, e misurato anche durante gli scatti della rotazione (ADR-052).
3. **Un arredo con un collider cambia il NavMesh** e può chiudere un passaggio che il generatore
   credeva aperto: il test di raggiungibilità di ogni livello va ripetuto con gli arredi, su più semi.
4. **Gli arredi cambiano l'ordine di costruzione**: i ricordi dei livelli (ADR-079) contano nemici e
   casse per posto. Gli arredi non devono passare dalla stessa lista, o un salvataggio della M10
   ritroverebbe nemici sbagliati: il controllo dei conteggi li farebbe ripartire nuovi.
5. **Un tiro in più nel popolatore cambia i livelli di tutti i semi** (trappola 10 della M7): gli arredi
   prendono un flusso suo del seme, come l'oro e il mercante.
6. **Il rendering software della CI non è una scheda vera**: con llvmpipe il passo a tutto schermo
   del filtro retrò non arriva nell'immagine. Un test che legge i pixel va provato anche con
   `-force-glcore`, e se passa ovunque tranne che in CI si ignora lì, dicendo perché.
7. **Una finestra senza focus ferma il player**: `runInBackground` è spento, e un frame misurato
   mentre si guarda altro dura secondi. La misura in build lo accende per sé.
8. **Un solo run in attesa per gruppo di concorrenza**: test e build stanno nel gruppo `unity`, e un
   run nuovo in attesa cancella quello che aspettava già. Due build lanciate insieme (normale e di
   sviluppo) ne lasciano una sola: si lanciano una dopo l'altra.
9. **butler non deve mai stampare la chiave**, e i job delle pull request da fork non la ricevono.

---

## Test

Da scrivere passo per passo.

---

## Checklist di chiusura

- [x] Decisioni D1–D11 confermate
- [ ] Correzioni dalla prova della M10
- [ ] Shader retro e post-processing
- [x] Cripta e caverne arredate
- [x] Audio rifinito
- [x] Profiling in build
- [ ] Test verdi in CI
- [ ] v1.0 su itch.io dalla CI, in beta ristretta con le chiavi
- [ ] README con GIF e architettura, `CREDITS.md` completo
- [ ] ADR, lezioni nel piano, tag `v1.0.0`
