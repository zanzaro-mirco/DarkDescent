# M19 — "L'ultima candela"

**Cosa deve succedere (Definition of Done):** il gioco ha due **filmati**, fatti nel motore con i
modelli, le luci e le animazioni del gioco e girati nel **borgo della M18**.

L'**apertura**, "L'ultima candela", parte con *Nuova partita*. In 45–60 s racconta:
- il borgo che si spegne;
- gli scheletri che escono dalla terra del cimitero;
- i sopravvissuti nella piazza all'alba;
- il mercante che accende l'ultima candela e indica la grotta.

Finisce nel borgo, con il cavaliere in mano a chi gioca.

La **vittoria** parte la prima volta che si batte il Re delle ossa a ogni difficoltà:
- il colpo che rallenta e il re che si sfalda;
- l'uscita dalla grotta all'alba;
- i sopravvissuti che esultano;
- poi la finestra delle statistiche.

Tutti e due si saltano con Esc o con un click, hanno le didascalie nelle due lingue e la loro musica.
Un pulsante **Filmati** nel menu principale li fa rivedere. Ogni inquadratura passa dal sì di
Mirco, prima in foto e poi in un video. Esce con il tag `v1.7.0`.

**Tempo stimato:** 13–18 h (D15), con il pulsante per rivederli aggiunto da Mirco il 27 ott 2026.
**Prerequisito:** M18 chiusa (tag `v1.6.0`): il borgo, la grotta, gli abitanti e le loro animazioni.

**Come si lavora:** come alle M13–M18. Faccio io:
- il codice, le scene, i modelli in Blender, gli script e i passaggi nell'editor;
- le prove in batchmode, a Unity chiuso;
- l'avvio di ComfyUI sulla porta 8199.

A Mirco restano:
- le decisioni qui sotto;
- il sì sui download (D2, D10, D11, D12);
- il sì sulle foto delle inquadrature e sui video dei due filmati;
- la prova in build.

---

## Stato verificato il 29 ottobre 2026

Riverificato dopo la chiusura della M18 (tag `v1.6.0`).

| Cosa | Stato |
|---|---|
| Pacchetti | **Cinemachine 3.1.7** c'è già e muove la camera del gioco: Position Composer, rotazione e zoom. **Timeline** e **Recorder** non ci sono. Il modulo `video` di Unity è attivo, ma nessuna scena usa un `VideoPlayer`. ffmpeg non è installato |
| Animazioni | Del rig medio di *KayKit Character Animations 1.1* sono importati 6 file su 8: `CombatMelee`, `General`, `MovementBasic` e, dalla M18, `Simulation` (seduti, `Sit_Floor_StandUp`, `Waving`, `Cheering`), `Special` (`Skeletons_Awaken_Floor`, `Skeletons_Death`, `Skeletons_Taunt`, `Skeletons_Walking`) e `Tools` (`Holding_A/B/C`, `Hammering`), con il loop solo dove serve. Manca solo `MovementAdvanced` (`Crouching`, `Sneaking`), che i filmati non usano |
| Personaggi | `Knight`, `Barbarian` (con l'avatar, dalla M18), `Rogue_Hooded`, `Mage`, `Ranger`, `Rogue` e i sette scheletri. Il Re delle ossa usa i modelli degli scheletri, più grande, con l'`overrideController` degli scheletri |
| Mondo | Il **borgo** della M18: piazza con il fuoco, case, taverna, cappella con l'altare, cimitero con la cripta, la grotta in fondo alla gola, il bosco attorno. Il giorno che passa viene da `DayCycle` e `DayLighting`, e **finestre e lanterne si accendono già di notte** (`NightLight`, luci vere). Attorno al fuoco quattro abitanti (`Villager`, con un grafo di Playables e senza Animator Controller); il mercante al suo banco (`Rogue_Hooded`), il fabbro alla forgia, la guaritrice sulla soglia. Alla profondità 1 si arriva dallo sbocco del cunicolo, nel muro. **Il trono non c'è**: la sala del re è una stanza del generatore |
| Vittoria oggi | Morto il re, `CompositionRoot.HandleBossDefeated` salva, apre il portale della vittoria nella sala e sblocca il tesoro. La prima volta a ogni difficoltà (`_firstVictory`) la finestra si apre arrivando nel borgo dal portale (`HandleVictoryReached`); le volte dopo c'è solo la scritta "Il re è caduto" |
| Nuova partita | Menu, scelta dello slot, borgo. Con `-newgame` e `-seed` dalla riga di comando, e nei test, non passa dal menu |
| Musica | Quattro brani CC0 da OpenGameArt (cripta, caverne, boss, città) e il fondo del borgo (fuoco e vento da Stable Audio, M18). Nessun brano per i filmati |
| ComfyUI | Nella cartella condivisa ci sono Z-Image-Turbo, TRELLIS.2 e Stable Audio 3 small SFX: **nessun modello di video né di musica**. La scheda è una RTX 5070 Ti Laptop con 12 GB; sul disco C ci sono 693 GB liberi |
| Test | 517 EditMode e 260 PlayMode a ogni push, più quelli della ronda. `PhotoTour` (assembly `DarkDescent.Tests.Photos`) fa le foto fuori dalla CI, anche del borgo |

---|---|
| Pacchetti | **Cinemachine 3.1.7** c'è già e muove la camera del gioco: Position Composer, rotazione e zoom. **Timeline** e **Recorder** non ci sono. Il modulo `video` di Unity è attivo, ma nessuna scena usa un `VideoPlayer`. ffmpeg non è installato |
| Animazioni | Dal pacchetto completo *KayKit Character Animations 1.1* (CC0, lo zip è in `Downloads`) sono importati 3 file su 8 del rig medio: `CombatMelee`, `General`, `MovementBasic`. La M18 aggiunge `Simulation`, con le pose da seduti e il saluto. Restano `Special` (`Skeletons_Awaken_Floor`, `Skeletons_Awaken_Floor_Long`, `Skeletons_Death`, `Skeletons_Taunt`, `Skeletons_Walking`), `Tools` (`Holding_A/B/C`) e `MovementAdvanced` (`Crouching`, `Sneaking`). Sono tutti per il rig medio, lo stesso di cavaliere e scheletri |
| Personaggi | Importati `Knight`, `Barbarian`, `Rogue_Hooded` e i sette scheletri; la M18 aggiunge `Mage`, `Ranger` e `Rogue` per gli abitanti. Il Re delle ossa usa i modelli degli scheletri, più grande, con l'`overrideController` degli scheletri |
| Mondo | Dopo la M18 c'è il **borgo all'aperto**, con il giorno che passa (alba, giorno, tramonto, notte, da `DayCycle`): piazza, case, cappella con l'altare, cimitero, la grotta verso la cripta e il cunicolo alla profondità 1. **Il trono non c'è**: la sala del re è una stanza del generatore |
| Vittoria oggi | Morto il re, `CompositionRoot.HandleBossDefeated` salva, apre il portale della vittoria nella sala e sblocca il tesoro. La prima volta a ogni difficoltà (`_firstVictory`) la finestra si apre arrivando in città dal portale (`HandleVictoryReached`); le volte dopo c'è solo la scritta "Il re è caduto" |
| Nuova partita | Menu, scelta dello slot, città. Con `-newgame` e `-seed` dalla riga di comando, e nei test, non passa dal menu |
| Musica | Quattro brani CC0 da OpenGameArt (cripta, caverne, boss, città) e il fondo della città. Nessun brano per i filmati |
| ComfyUI | Nella cartella condivisa ci sono Z-Image-Turbo, TRELLIS.2 e Stable Audio 3 small SFX: **nessun modello di video né di musica**. La scheda è una RTX 5070 Ti Laptop con 12 GB; sul disco C ci sono 686 GB liberi |
| Test | 486 EditMode e i PlayMode a ogni push, più quelli della ronda. `PhotoTour` (assembly `DarkDescent.Tests.Photos`) fa le foto fuori dalla CI |

---

## Decisioni

Confermate da Mirco il 29 ottobre 2026, con D5 e D6 aggiornate al borgo della M18 (D18 l'aveva
scelta lui il 27). Le scene sono quelle approvate il 27 ottobre 2026, portate nel borgo.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M19 "L'ultima candela"**, tag **`v1.7.0`**. Dentro: i due filmati, le luci di notte e d'alba del borgo, le animazioni nuove, la musica, il modo di saltarli e le didascalie. Il gioco non cambia regole | Due filmati si vedono da chi gioca: una versione minore. Il nome è quello dell'apertura, che racconta la storia di tutto il gioco |
| D2 | **Il montaggio** | **Unity Timeline** (pacchetto ufficiale, gratuito, dal registro di Unity) con le camere di **Cinemachine** che ci sono già. Ogni filmato è una scena con il suo `PlayableDirector`. Lo **costruisce uno strumento dell'editor dal codice**, `FilmTools`, come la città dalla sua mappa: si rifà con un comando e si rilegge nel diff. La lista delle inquadrature (inizio, durata, camera, didascalia) è una classe pura in `Core`, `FilmScript`, che si prova in EditMode. Download con il sì di Mirco | Timeline mette in fila camere, animazioni, luci, suoni e didascalie con un tempo solo, che è il lavoro di un filmato. Scritta a mano con le coroutine sarebbe più codice e niente anteprima. Costruirla dal codice serve a me, che lavoro a Unity chiuso |
| D3 | **Il set** | Il set è **il borgo della M18**, costruito dalla stessa mappa e dallo stesso tileset. Le luci sono quelle del **giorno che passa** della M18, fermate sul momento che serve: la **notte**, con le candele alle finestre, e l'**alba**. Il cimitero, la cappella e la grotta sono quelli in cui si gioca | Chi gioca riconosce nel filmato il posto in cui si trova. Un set solo, già fatto, costa una frazione di un set nuovo |
| D4 | **Dove finiscono** | L'apertura finisce **nel borgo**, dove comincia la partita. La vittoria finisce **fuori dalla grotta**, nel borgo, e con *Continua* si è già lì, all'ora del tempo di gioco | I filmati portano esattamente dove si gioca, senza stacchi tra la storia e il gioco |
| D5 | **Cosa manca al set** | Le finestre accese di notte ci sono già (`NightLight`): nel filmato si **spengono una a una**, con la luce e il bagliore del vetro. Mancano le **lanterne** in mano ai paesani e la **terra smossa** sulle tombe da cui escono gli scheletri. Niente da scaricare per il mondo: il borgo c'è già | È tutto quello che la storia mostra e il borgo giocabile non ha |
| D6 | **Le comparse** | I paesani sono gli abitanti della M18, con una lanterna in mano: `Holding_A` sulla parte alta del corpo e la corsa sotto, con una maschera nel grafo di Playables di `Villager` (un `AnimationLayerMixerPlayable` con un `AvatarMask`). Il cavaliere è il modello del gioco; nella vittoria porta l'**equipaggiamento di chi gioca**. Il mercante è il suo. Al più 8 comparse in una inquadratura | Le stesse facce del borgo legano il filmato al gioco. Il cavaliere della vittoria deve essere il tuo, non uno qualunque |
| D7 | **L'apertura** | Cinque scene in circa 50 s. Si vede **a ogni *Nuova partita***, non quando si carica; mai con `-newgame` né nei test. Il dettaglio delle scene è nell'elenco sotto la tabella | È la storia scelta da Mirco, portata nel borgo. Chi comincia una partita nuova la vede, chi la sa la salta |
| D8 | **La vittoria in due tempi** | **Primo tempo, nella sala** (circa 5 s), appena muore il re. **Secondo tempo**, attraversando il portale della vittoria, dove oggi si apre la finestra. Tutti e due **la prima volta a ogni difficoltà**, come la finestra di oggi. Il dettaglio dei due tempi è nell'elenco sotto la tabella | Il tesoro e il bottino del re restano a chi gioca: un filmato che li apre al posto suo li toglierebbe. Il trono non c'è, e aggiungerlo cambierebbe la sala del re, i suoi semi e la ronda; il posto vuoto del re è il mucchio d'ossa che resta |
| D9 | **Saltare e guardare** | Esc o un click: il primo mostra in basso «Esc o click per saltare», il secondo salta. Saltata l'apertura si è nel borgo, saltata la vittoria si va alla finestra. Durante un filmato il gioco non legge comandi e non salva. **Bande nere** sopra e sotto (2,35:1), le didascalie in Cinzel con una dissolvenza. Nessuna voce | Due tasti per saltare evitano di perdere un filmato per sbaglio, come in Diablo. Le bande dicono "questo è un filmato" senza parole |
| D10 | **La musica** | Prima si prova **ACE-Step** in ComfyUI (musica da testo), come Z-Image alla M16: un brano per l'apertura e uno per la vittoria, ognuno con la sua ricetta in `Tools/ComfyUI/music/`, e una pagina di ascolto per Mirco. Se non convince, un **brano CC0** di OpenGameArt come gli altri quattro. I suoni (la candela che si spegne, la terra che si rompe, la folla) con Stable Audio, che c'è già. Licenza e dimensione dei pesi si leggono prima di scaricare, con il sì di Mirco | Una musica fatta per il filmato segue le scene, un brano trovato va tagliato. La prova costa poco e c'è una riserva sicura |
| D11 | **I modelli di video** | Si provano **Wan 2.2 TI2V 5B** e **LTX-Video** su un render del borgo, in **1 h al massimo**, solo per gli stacchi che il motore fa male: fumo, polvere, la luce che entra dalla grotta. **Mai per i personaggi**, che cambiano forma da un fotogramma all'altro. Una clip entra solo con il sì di Mirco, come `VideoPlayer` dietro la scena; altrimenti tutto resta nel motore, e la prova finisce nelle lezioni. Licenze e pesi si leggono prima di scaricare | Chiesto da Mirco. Un filmato nel motore si vede alla risoluzione dello schermo e pesa poco; una clip generata pesa megabyte e può stonare. Un tempo fisso tiene la prova nelle ore |
| D12 | **Il video da mostrare** | **Unity Recorder** (pacchetto ufficiale, dal registro di Unity) registra i due filmati in MP4 1920×1080 da un test della categoria `Photos`, come il giro di foto della M17. I video vanno nelle release, e nel README come GIF corte; nel repo non entrano interi. Download con il sì di Mirco | Senza ffmpeg, Recorder è la strada più corta per un MP4. Il video serve a Mirco per il sì e a chi guarda il repo |
| D13 | **Il sì di Mirco** | Tre giri: prima le **foto delle inquadrature** (un fotogramma per scena, da `PhotoTour`), prima di montare; poi il **video** di ogni filmato; poi la prova in build. A ogni giro si cambia quello che non convince | Una foto si corregge in minuti, un filmato montato in ore. Come alla M17, niente entra senza il sì |
| D14 | **Si controlla** | EditMode: `FilmScript` (inquadrature in fila senza buchi, durata dentro i limiti, ogni didascalia nelle due lingue) e quando parte la vittoria (la prima volta sì, la seconda no). PlayMode: vedi l'elenco sotto la tabella. La ronda non cambia | I filmati toccano il tempo, la camera e i comandi del gioco: se uno di questi non torna com'era, il gioco è rotto. I test lo dicono prima di Mirco |
| D15 | **Stima e linee di taglio** | **13–18 h**, con il pulsante per rivederli; meno delle 14–20 della prima stima perché il set c'è già. Se servirà tagliare, in quest'ordine: le clip generate (D11); la scena 2 dell'apertura, con meno comparse; la musica generata (resta un brano CC0); il video per il README. I due filmati interi, il salto e le didascalie non si tagliano | Meglio deciderlo adesso che a metà |
| D16 | **Fuori da questa milestone** | Filmati per la morte o per ogni difficoltà; voci e labiale; filmati prerenderizzati come quelli di Blizzard; l'albero delle abilità (M20) e il Mago (M21); la risalita e le stanze più grandi (M22) | Ognuna è un lavoro a sé |
| D18 | **Rivederli** (scelto da Mirco) | Un pulsante **Filmati** nel menu principale, sotto *Riconoscimenti*, apre l'elenco: *L'ultima candela* sempre, *La vittoria* solo dopo averla vista una volta in una partita qualsiasi. La vittoria rivista usa il cavaliere con l'equipaggiamento base. Che cosa è stato visto lo ricordano le impostazioni, non il salvataggio | Chi ha finito il gioco lo vuole rivedere senza rifare tutto. Mostrare la vittoria prima di averla vista la rovinerebbe |
| D17 | **Dimensioni e prestazioni** | Il limite è **+30 MB** sulla build: le animazioni, due brani e, se entrano, le clip. I filmati girano a **60 fps** sul PC di sviluppo in build. Forward+ regge molte luci, ma ognuna costa: le candele del borgo di notte sono soprattutto materiali emissivi. Si misura alla chiusura come alla M17 | Il borgo di notte con le finestre accese è la scena con più luci del gioco |

**D7, le cinque scene dell'apertura:**
1. Il borgo di notte, le candele alle finestre che si spengono una a una.
2. Il cimitero: gli scheletri escono dalla terra (`Skeletons_Awaken_Floor`) e i paesani scappano con
   le lanterne.
3. L'alba nella piazza, i sopravvissuti seduti attorno al fuoco spento.
4. Arriva il cavaliere. Il mercante accende l'ultima candela sull'altare della cappella e indica la
   grotta. La didascalia: il Re delle ossa si è svegliato.
5. Il titolo sopra la bocca della grotta. Poi la camera scende sul cavaliere nella piazza, e si gioca.

**D8, i due tempi della vittoria:**
- **Primo tempo.** Il tempo rallenta a un quarto sul colpo finale. Una camera si stringe sul re che si
  sfalda (`Skeletons_Death`) e la musica tace. Poi si torna a giocare.
- **Secondo tempo.**
  1. Il cavaliere guarda il mucchio d'ossa dov'era il re e si avvia.
  2. Stacco sul cunicolo, con la luce che entra da fuori.
  3. Il cavaliere esce dalla grotta all'alba e i sopravvissuti si alzano ed esultano (`Cheering`).
  4. La finestra con le statistiche, *Continua* e *Menu principale*.

**D14, cosa provano i test PlayMode:**
- *Nuova partita* dal menu avvia l'apertura, che finisce nel borgo con il cavaliere libero;
- Esc più Esc salta il filmato in meno di un secondo;
- il re battuto la prima volta fa il primo tempo, il portale il secondo e poi la finestra;
- la seconda volta non parte nessun filmato;
- dopo un filmato il tempo torna a 1 e la camera torna al cavaliere.

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 19.1 | Gli attrezzi: Timeline e Recorder (con il sì), le animazioni nuove importate, `FilmScript` in `Core`, il lettore dei filmati (salto, bande, didascalie, comandi e tempo fermi), i test (D2, D9, D12, D14) | 2–2,5 |
| 19.2 | La prova dei modelli in ComfyUI: ACE-Step per la musica, Wan 2.2 e LTX-Video per gli stacchi, con le pagine per Mirco; si decide cosa entra (D10, D11) | 1,5–2 |
| 19.3 | Il borgo di notte e all'alba: le luci, le candele alle finestre, le lanterne, la terra smossa; le foto per Mirco (D3, D5, D17) | 1,5–2 |
| 19.4 | L'apertura: le cinque scene, le comparse, le luci che si spengono, le didascalie, il collegamento a *Nuova partita*. Prima le foto per Mirco, poi il video (D4, D6, D7, D13) | 3–4 |
| 19.5 | La vittoria: il primo tempo nella sala, il secondo dal portale, il cavaliere con l'equipaggiamento di chi gioca, la finestra alla fine. Prima le foto per Mirco, poi il video (D4, D8, D13) | 2,5–3,5 |
| 19.6 | Musica e suoni dei due filmati, il volume con le opzioni, i crediti; il pulsante **Filmati** nel menu (D10, D18) | 2–2,5 |
| 19.7 | Misura, video e GIF per il README, build da provare e chiusura: ADR, lezioni, piano, `bundleVersion` 1.7.0, tag `v1.7.0` dopo i test verdi in CI (D17) | 1–1,5 |

---

## Passo 19.1 — Gli attrezzi

**Com'è andata (29 ott 2026).**
- **I pacchetti**, con il sì di Mirco, dal registro di Unity:
  - **Timeline 1.8.13** (23 MB scaricati, nella build solo il runtime);
  - **Recorder 5.1.7** (8 MB) con **OpenImageIO 1.0.2** (24 MB), che serve a Recorder. Sono solo
    per l'editor e non entrano nella build.

  Le animazioni dei filmati erano già entrate con la M18: `Simulation`, `Special` e `Tools`.
- **`FilmScript`** e **`FilmShot`** (logica pura in `Core/Films`):
  - un filmato è una lista di inquadrature, ognuna con inizio, durata, camera e didascalia;
  - `Problems()` dice cosa non torna: buchi, inquadrature sovrapposte o vuote, camera mancante,
    durata fuori dai limiti del filmato;
  - `ShotAt` e `CaptionAt` danno l'inquadratura e la didascalia di un istante.
- **`SkipPrompt`** (logica pura): il primo Esc o click mostra l'invito a saltare per 3 s, il secondo
  mentre si vede salta.
- **`FilmPlayer`**, il lettore, fa partire la Timeline di un filmato. Mentre gira:
  - spegne `PlayerInputReader` e le finestre del gioco, e le riaccende alla fine;
  - mette le **bande nere** a 2,35:1 e la **didascalia** in Cinzel, che appare e sparisce in 0,6 s;
  - in alto mostra l'invito a saltare;
  - legge Esc e il click direttamente da tastiera e mouse.

  Le bande e le scritte stanno in un canvas suo, costruito dal codice, sopra tutti gli altri. Alla
  fine, o al salto, il tempo torna a 1 e parte l'evento `Finished`. Il gioco salva quando si carica
  un livello, prima che parta il filmato.
- **`FilmTools.InstallPlayer`** mette il lettore nella scena `Core` con il carattere e lo lega al
  composition root; si rifà con un comando.

**I test.**
- **`FilmScriptTests` (4) e `SkipPromptTests` (2), EditMode:** le inquadrature in fila, i buchi e le
  sovrapposizioni, i limiti della durata, l'istante che trova la sua inquadratura; il doppio tasto e
  l'invito che sparisce.
- **`FilmPlayerTests` (2), PlayMode, con una Timeline vuota di 2,5 s:**
  - il filmato spegne comandi e finestre, mette le bande e la didascalia, e alla fine riaccende
    tutto con il tempo a 1;
  - il secondo tasto salta nello stesso frame.
- **In locale:** EditMode intera 523/523; PlayMode del lettore, del borgo e del menu verdi.

---

## Passo 19.2 — La prova dei modelli

**I download (29 ott 2026).** Con il sì di Mirco, scaricati da lui a mano in
`ComfyUI-Installs/DarkDescent/ComfyUI/models`; peso e hash SHA-256 uguali a quelli di Hugging Face:

| Modello | File | Peso | Licenza |
|---|---|---|---|
| ACE-Step 1.5 turbo | `ace_step_1.5_turbo_aio` (tutto in uno) | 10,0 GB | MIT; Apache 2.0 il pacchetto di ComfyUI |
| Wan 2.2 TI2V 5B | il modello, `umt5_xxl_fp8_e4m3fn_scaled`, `wan2.2_vae` | 18,2 GB | Apache 2.0 |
| LTX-Video 2B 0.9.8 distilled fp8 | il modello e `t5xxl_fp8_e4m3fn` | 9,4 GB | LTXV Open Weights (gratuita sotto 10 milioni di fatturato; nessun diritto sugli output) |

**La musica (D10).** ACE-Step 1.5 c'è:
- il workflow `ace_step_music.json` carica il file tutto in uno; le ricette stanno in
  `Tools/ComfyUI/music/` (descrizione, tempo, tonalità, durata, seme) e `make.py` fa i brani in OGG
  stereo a 48 kHz con la loro pagina di ascolto;
- un brano di 60 s esce in **circa 9 s** sulla 5070 Ti e pesa 0,8 MB;
- libsndfile va in stack overflow se riceve un OGG lungo in una volta sola: `make.py` lo scrive a pezzi.

Quattro semi per brano. Mirco ha scelto il **1904** per l'apertura (60 s, re minore, l'unico pieno
fino in fondo) e il **1905** per la vittoria (30 s, re maggiore, che finisce da solo verso i 25 s).
Sono in `Audio/Music/` come `film_opening.ogg` e `film_victory.ogg`, in 2D, e nei crediti. La riserva
CC0 non serve.

**I video (D11): non entra niente, tutto resta nel motore.** Circa un'ora di prove, in tre giri:
- **le foto:** il fuoco del borgo con i paesani, la bocca della grotta di giorno e il cunicolo nella
  cripta, prima con il cavaliere e poi vuoto, senza interfaccia. Lo scatto `crypt_tunnel_empty` è entrato
  in `PhotoTour.CaveAndTunnel` e servirà allo stacco della vittoria;
- **LTX-Video 2B 0.9.8:** una clip di 4–5 s in 18–20 s. Su quindici tentativi, anche con i passi del
  modello distilled, cambia scena, muove la camera, inventa sottotitoli e fari;
- **Wan 2.2 TI2V 5B:** 9 minuti a clip, con 9,5 GB di VRAM su 12. Tiene ferma la scena, ma mette gli
  effetti nel posto sbagliato: il fumo è una nuvola piatta che esce dalla testa del cavaliere, la luce
  cade dall'alto invece di uscire dal cunicolo, e il cavaliere diventa una sagoma bianca.

**Le lezioni.**
- Questi modelli sono addestrati per fare cinema: vogliono muovere la camera e cambiare inquadratura.
  Un effetto piccolo, in un punto preciso di una scena ferma e stilizzata, è il caso in cui riescono
  peggio. Le ricette ufficiali di LTX mettono perfino "video game, cartoon" nella frase negativa.
- I modelli che ho scelto erano i più piccoli e i più vecchi della loro famiglia. ComfyUI ha già le
  ricette di Wan 2.2 I2V 14B e di LTX-2.3 e LTX-2.5 (22B, luglio 2026), ma su questo PC non ci stanno:
  12 GB di VRAM e 31 GB di RAM, contro i 14,3 GB per ognuna delle due parti di Wan 14B e i 19–21 GB
  di LTX-2.5 più il codificatore del testo. **Prima di proporre un modello si fanno i conti con la VRAM
  e la RAM del PC, non solo con il disco.**
- I file di Wan 5B e di LTX 2B (25,6 GB) sono nel Cestino, su richiesta di Mirco; ACE-Step resta.

La luce che entra dal cunicolo, il fumo e la polvere si fanno nel motore, con luci e particelle, al
19.4 e al 19.5.

---

## Passo 19.3 — Il borgo di notte e all'alba

**Com'è andata (29 ott 2026).** Il borgo della M18 aveva già le luci della notte: una luce davanti a
ogni casa e sotto ogni lanterna, accese dal tramonto all'alba. Mancavano il vetro che brilla, il modo
di spegnerle una a una, l'ora ferma, il fuoco spento, le lanterne a mano e la terra smossa.
- **Il vetro.** Nelle case KayKit il vetro delle finestre è un riquadro dell'atlante, riga 1 e colonna
  6, e solo il vetro lo usa: lo si è visto in Blender, colorando quel riquadro in tutte e sette le case.
  Nelle lanterne del cimitero il lume è la riga 3, colonna 2.
  - `VillageTools` mette ai materiali `M_Village` e `M_Graveyard` una **maschera di 8×4 pixel**, uno
    per riquadro, bianco solo sul vetro, come mappa dell'emissione. Il filtro è a punti, così la
    maschera non sbava sui riquadri accanto.
  - Il materiale resta senza emissione: la accende `NightLight`, modello per modello, con un
    `MaterialPropertyBlock`.
  - Niente mesh nuove e nessuna luce in più (D17).
- **`NightLight`** accende il vetro insieme alla luce, con il suo colore in HDR (finestre ×2,5,
  lanterne ×2). `Snuff(secondi)` e `Relight(secondi)` spengono e riaccendono la candela con una
  dissolvenza, e il buio non la riaccende: è quello che serve all'apertura.
- **`DayCycleView.Hold(tempo)`** ferma il borgo a un'ora, di notte o all'alba, qualunque cosa dica
  l'orologio della partita; `Release()` lo ridà all'orologio.
- **`Hearth`**, sul fuoco della piazza: `SetBurning(false)` spegne la luce e toglie le torce accese.
  Restano i sassi, attorno a cui siedono i sopravvissuti dell'alba.
- **`HandLantern`**, la lanterna a mano dei paesani: quella appesa del cimitero ×0,4, con il gancio
  nell'origine dove la prende la mano, la sua luce e il lume sempre acceso (`M_HandLantern`).
- **`GraveDug`**, la tomba da cui esce uno scheletro: la lapide rotta KayKit e davanti, dal lato della
  scritta, il cumulo di zolle **`DugEarth`** di `Tools/Blender/models/earth.py`, alto 50 cm. Ha la
  terra dei sentieri del borgo (`M_VillageDirt`), perché l'atlante KayKit non ha un marrone terra.
- **Le foto** vengono da `PhotoTour.FilmSet`, senza cavaliere né interfaccia e con lo zoom più largo:
  - la notte nella piazza;
  - le candele, una su due spenta;
  - il cimitero con tre tombe smosse e una lanterna, di notte e di giorno;
  - l'alba con il fuoco spento.

**Le lezioni.**
- **URP tiene l'emissione solo con il flag giusto.** La parola chiave `_EMISSION` resta accesa solo se
  il materiale ha l'emissione in tempo reale (o baked) tra i flag. Con `None`, URP la spegne ogni volta
  che rivede il materiale, anche durante i test: alla chiusura di Unity il file veniva risalvato senza.
  Il test la trovava ancora accesa, e il danno si vedeva solo nel file su disco. Ora i materiali del
  vetro hanno `RealtimeEmissive`, e un test lo controlla.
- **Il davanti delle lapidi KayKit è −Z.** La prima volta la terra era finita dietro la pietra.
- **Il cumulo va più alto del vero.** Con la camera dall'alto un cumulo di 30 cm si schiaccia sul
  prato; con 50 cm si legge.
- **Il 19.2 aveva rotto la CI.** Ho aggiunto la musica a `CREDITS.md` senza rifare la copia del gioco
  (`Data/Credits.txt`), e il run Tests del 19.2 è rosso. La copia è rifatta in questo passo.

**I test.**
- **`NightSetTests` (5), EditMode:**
  - la candela spenta spegne luce e vetro anche di notte, e si riaccende;
  - la maschera ha un riquadro solo, quello del vetro, e i materiali tengono l'emissione;
  - ogni casa e lanterna ha il vetro che brilla;
  - l'ora ferma resta ferma e torna all'orologio;
  - il fuoco si spegne e si riaccende;
  - la lanterna a mano e la tomba smossa sono pronte.
- **In locale:** EditMode intera 534/534. PlayMode intera senza la ronda 269/272, con un rosso,
  `PortalTests.Spell_InATightCorner_OpensAReachablePortal` (il portale in un angolo stretto della
  cripta, in un punto a caso). Rilanciati da soli, i test dei portali sono verdi 5/5: è instabile e
  non tocca il borgo. Se cade di nuovo si riproduce.

---

## Checklist di chiusura

- [x] Decisioni D1–D18 confermate (D18 scelta da Mirco)
- [ ] I download approvati: Timeline e Recorder (sì il 29 ott), i modelli di ComfyUI (sì il 29 ott)
- [ ] L'apertura e la vittoria approvate da Mirco in video
- [ ] Test EditMode e PlayMode verdi in locale e in CI
- [ ] La build misurata dentro il limite della D17
- [ ] ADR, lezioni nel piano, README, `CREDITS.md` e `Data/Credits.txt`
- [ ] `bundleVersion` 1.7.0 e tag `v1.7.0` dopo i test verdi in CI
