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
| D1 | **Correzioni dalla prova della M10** | Per prime: quello che esce dalla build provata da Mirco e dalla prova a cinque persone (piano, M10), comprese le tarature rimaste aperte (oro e prezzi, vita e danno del boss, misura del portale). Le pergamene del ritorno impilabili restano nell'`ICEBOX` | Sono le note su un gioco già finibile: vengono prima dell'aspetto |
| D2 | **Shader retro** | Un passo a tutto schermo con la *Full Screen Pass Renderer Feature* di URP e un materiale Shader Graph: **palette limitata** da una texture di 32 colori scuri, caldi per fuochi e oro, freddi per l'ombra; **dithering ordinato** 4 × 4 tra un colore e l'altro. Senza abbassare la risoluzione di default. Un'**opzione** *Filtro retrò* (spento, leggero, pieno) nel menu delle opzioni, su *leggero* all'avvio | Dà a KayKit, Quaternius e alle caverne una pelle comune, come chiede il piano. La pixelatura sull'interfaccia rende illeggibili i testi in Cinzel, ed è per questo che resta fuori di default |
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
6. **butler non deve mai stampare la chiave**, e i job delle pull request da fork non la ricevono.

---

## Test

Da scrivere passo per passo.

---

## Checklist di chiusura

- [x] Decisioni D1–D11 confermate
- [ ] Correzioni dalla prova della M10
- [ ] Shader retro e post-processing
- [ ] Cripta e caverne arredate
- [ ] Audio rifinito
- [ ] Profiling in build
- [ ] Test verdi in CI
- [ ] v1.0 su itch.io dalla CI, in beta ristretta con le chiavi
- [ ] README con GIF e architettura, `CREDITS.md` completo
- [ ] ADR, lezioni nel piano, tag `v1.0.0`
