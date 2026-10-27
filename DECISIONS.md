# Decisioni architetturali (ADR)

> Una voce per ogni scelta tecnica non ovvia. Cinque righe.
> Quando tra sei mesi ti chiederai "perché avevo fatto così", la risposta è qui.
> In un colloquio, questo file vale più di mille righe di codice.

Formato:

```
## ADR-NNN — Titolo
- **Data:**
- **Contesto:** qual era il problema
- **Decisione:** cosa ho scelto
- **Alternative scartate:** cosa altro ho valutato e perché no
- **Conseguenze:** cosa diventa più facile, cosa più difficile
```

---

## ADR-001 — Unity + URP come engine e render pipeline
- **Data:** 2026-09-20
- **Contesto:** serve un engine 3D per un ARPG isometrico, con doppio obiettivo: finire il gioco e acquisire competenze spendibili sul mercato.
- **Decisione:** Unity 6.3 LTS (6000.3.24f1) con Universal Render Pipeline 17.3.0, C#.
- **Alternative scartate:** Godot (C# supportato ma ecosistema e mercato del lavoro più piccoli); Unreal (C++/Blueprint, curva più ripida, sovradimensionato per un progetto solo); Built-in RP (di fatto deprecato); HDRP (troppo pesante, pensato per alta fedeltà).
- **Conseguenze:** accesso a Shader Graph e al grosso della documentazione e delle risposte online. Vincolo: restare sulla stessa LTS per tutto il progetto, niente upgrade a metà strada.

## ADR-002 — Input System nuovo invece del legacy Input Manager
- **Data:** 2026-09-20
- **Contesto:** il vecchio `Input.GetMouseButtonDown` è più rapido da usare ma è legacy.
- **Decisione:** package *Input System* fin dalla milestone M1.
- **Alternative scartate:** Input Manager legacy — avrebbe richiesto di riscrivere tutta la gestione input più avanti, quando gli input diventano una decina (hotbar, inventario, incantesimi).
- **Conseguenze:** mezza giornata di setup in più all'inizio; nessuna riscrittura dopo. È anche ciò che si trova nei progetti professionali.

## ADR-003 — Un assembly definition per il codice di progetto
- **Data:** 2026-09-21
- **Contesto:** senza `.asmdef` tutti gli script finiscono in `Assembly-CSharp`, un unico assembly che ricompila per intero a ogni modifica e in cui ogni script può usare qualsiasi cosa. Le dipendenze restano implicite e invisibili.
- **Decisione:** un singolo `DarkDescent.asmdef` in `Assets/_Project/Scripts/`, con `rootNamespace: DarkDescent` e le sole reference effettivamente usate (per ora `Unity.InputSystem`). Nessuna suddivisione per area finché le dipendenze reali non la giustificano.
- **Alternative scartate:** nessun asmdef — ricompilazioni più lente e nessun controllo sulle dipendenze, e aggiungerlo dopo costa un giro di errori "type or namespace not found". Un asmdef per area (Player, Combat, Items...) — over-engineering adesso: si introduce alla M6, quando le dipendenze fra aree esistono davvero e le si può disegnare sui fatti.
- **Conseguenze:** compilazioni incrementali più rapide e dipendenze esplicite. In cambio, ogni package nuovo va dichiarato a mano nelle reference dell'asmdef. Vincolo da ricordare: un assembly con `.asmdef` **non può** referenziare `Assembly-CSharp`, solo il contrario — quindi il codice generato dall'Input System deve stare sotto l'asmdef, non in `Assets/_Project/Settings/`.

## ADR-004 — Solo asset CC0 nel repo pubblico, con un'eccezione per i font OFL
- **Data:** 2026-10-01 (eccezione per i font: 2026-10-03)
- **Contesto:** il repo è pubblico e deve restare clonabile e compilabile da chiunque. Mixamo e Asset Store vietano di ridistribuire i file grezzi; servono comunque personaggi animati, armi e suoni.
- **Decisione:** opzione (a) del piano (§ 1.4): solo asset CC0. Personaggi, armi e animazioni da KayKit (Adventurers, Skeletons, Character Animations, rig `Rig_Medium` comune a tutti), suoni da Kenney (RPG Audio, Impact Sounds). Ogni asset ha una riga in `CREDITS.md`. Eccezione: il font LiberationSans incluso nelle TMP Essential Resources (SIL OFL 1.1), che resta con il testo della licenza accanto. La sprite EmojiOne (CC BY 4.0) delle stesse risorse è stata tolta: serve solo ai tag `<sprite>`, che non usiamo.
- **Alternative scartate:** (b) submodule privato con gli asset Mixamo, che rende il repo non compilabile per chi lo clona e complica la CI; (c) repo privato fino al rilascio, che toglie la visibilità da portfolio. Per il font, un font CC0 di Kenney: un download in più per un font che cambierà comunque con lo stile grafico definitivo.
- **Conseguenze:** nessun problema di licenze e CI semplice. In cambio lo stile è vincolato a quello che offrono gli autori CC0: per KayKit è low poly e colorato, da scurire con illuminazione e post-processing. Ogni font futuro deve essere CC0 oppure OFL con la licenza nel repo.

## ADR-005 — Personaggi KayKit importati con rig Generic, non Humanoid
- **Data:** 2026-10-01
- **Contesto:** il rig Humanoid di Unity permette il retargeting tra scheletri diversi, ma importando il cavaliere KayKit come Humanoid compaiono warning e l'osso `chest` non viene mappato: la sua rotazione viene scartata in ogni clip e il busto resta rigido.
- **Decisione:** rig Generic, con avatar creato dal modello. Le clip di animazione copiano l'avatar del cavaliere. Tutti i personaggi KayKit condividono `Rig_Medium`: lo scheletro ha le stesse 24 ossa con gli stessi percorsi, e le stesse clip vanno bene per tutti.
- **Alternative scartate:** Humanoid con mappatura manuale del `chest`, che resta comunque fuori dalle ossa che Humanoid sa gestire; retargeting con tool esterni, che serve solo con scheletri diversi.
- **Conseguenze:** animazioni fedeli e nessun warning. Il retargeting automatico verso scheletri diversi da `Rig_Medium` non è disponibile: un personaggio con un altro rig avrà bisogno delle sue clip. Con l'ADR-004 non è un limite pratico.

## ADR-006 — Il raggio del click si ferma anche sugli ostacoli
- **Data:** 2026-09-30
- **Contesto:** il click-to-move fa un raycast dalla camera al punto sotto il cursore. Se il raggio considera solo il pavimento, attraversa i cubi e colpisce il pavimento dietro: un click su un ostacolo manda il player alle sue spalle.
- **Decisione:** il raggio usa camminabile + bloccante + nemico, e decide in base al primo collider colpito. Pavimento: si cammina. Ostacolo: non succede niente. Nemico: lo si attacca. Il punto cliccato viene poi riportato sul NavMesh con `SamplePosition` (raggio 1 m).
- **Alternative scartate:** solo il pavimento, con lo spostamento a sorpresa descritto sopra; spostare il player verso il bordo dell'ostacolo cliccato, comportamento poco prevedibile e diverso da Diablo.
- **Conseguenze:** il click fa quello che il giocatore vede sotto il cursore. Ogni nuova categoria cliccabile (porte, oggetti a terra, PNG) diventa un layer e un ramo nella stessa decisione, che resta in un solo posto: `PlayerController`.

## ADR-007 — Composition root per collegare i sistemi della scena
- **Data:** 2026-10-03 (scelta a inizio M2, confermata a chiusura)
- **Contesto:** HUD, schermata di morte, numeri di danno, hit stop e IA nemica devono conoscere il player e la sua `Health`. Con una scena sola basterebbero i riferimenti nell'Inspector, ma dalla M3 il player attraversa le scene e dalla M6 i livelli nascono a runtime (§ 4.2).
- **Decisione:** un componente `CompositionRoot` nella scena. In `Awake` collega tutto: `HealthOrb.Bind`, `DeathScreen.Bind`, `DamageNumbers.Track`, `EnemyAI.Bind` su ogni nemico (una ricerca sola, all'avvio). Si iscrive a `HitLanded` del player per l'hit stop e a `RestartRequested` per ricaricare la scena. I componenti espongono `Bind` e non cercano niente da soli.
- **Alternative scartate:** singleton statici, che accoppiano tutto, complicano i test e sopravvivono al ricaricamento della scena; ScriptableObject come canali di eventi, eleganti ma indiretti e da usare solo dove servono davvero.
- **Conseguenze:** il flusso si legge dall'alto in basso in un solo file, e i test possono ricollegare i componenti a mano (`Bind` su un'altra `Health`). Le trappole: l'ordine degli `Awake` tra oggetti non è garantito, quindi `Health` crea il suo model al primo accesso, e ogni `Bind` si iscrive solo se arriva dopo `OnEnable`. Alla M3 il root si sposta nella scena persistente; alla M6 i nemici generati a runtime li collegherà lo spawner.

## ADR-008 — uGUI per l'HUD
- **Data:** 2026-10-03 (scelta a inizio M2, confermata a chiusura)
- **Contesto:** la M2 richiede sfera della vita, numeri di danno e schermata di morte. Unity ha due sistemi di UI a runtime: uGUI (GameObject e Canvas) e UI Toolkit (documenti UXML e USS).
- **Decisione:** uGUI 2.0, già installato, con TextMeshPro per i testi. La sfera è un'`Image` *Filled* verticale; i numeri sono testi TMP in un pool; la Canvas è *Screen Space – Overlay* con *Scale With Screen Size* 1920×1080. Nessun polling: ogni elemento si aggiorna dagli eventi.
- **Alternative scartate:** UI Toolkit, adatto a menu e inventario, ma meno diretto per elementi che seguono il mondo 3D (i numeri sopra i nemici). Mescolare i due sistemi nello stesso HUD costerebbe più di quanto rende.
- **Conseguenze:** HUD semplice da costruire e da testare. I numeri di danno passano la camera della Canvas alla conversione delle coordinate, così funzionano in ogni modalità della Canvas. La scelta va rivista alla M4 con l'inventario, dove UI Toolkit potrebbe convenire per i soli menu.

## ADR-009 — Un Animator Controller condiviso, con override per personaggio
- **Data:** 2026-10-01
- **Contesto:** cavaliere e scheletro hanno lo stesso rig e gli stessi stati (locomozione, attacco, colpo subito, morte), ma clip diverse per la camminata e per il fendente.
- **Decisione:** un `Character.controller` con gli stati `Locomotion` (blend tree), `Attack`, `Hit` e `Death`, più un `AnimatorOverrideController` per lo scheletro che sostituisce solo le clip. Gli one-shot partono con `CrossFadeInFixedTime` sullo stato, non con i trigger. Un unico `CharacterAnimatorDriver` traduce in animazioni gli eventi di combattimento.
- **Alternative scartate:** un controller per personaggio, cioè la stessa macchina a stati duplicata e da tenere allineata a mano; i trigger, che restano armati se arrivano durante una transizione e fanno partire un secondo attacco.
- **Conseguenze:** un nuovo nemico con `Rig_Medium` costa un override con le sue clip. Un nemico con stati diversi (un incantatore, un boss) avrà bisogno di un controller suo, oppure di layer aggiuntivi.

## ADR-010 — Un colpo subito interrompe l'attacco solo sopra una soglia
- **Data:** 2026-10-01
- **Contesto:** quando un personaggio viene colpito mentre sta tirando un fendente, il suo colpo deve partire comunque? Se sì, i colpi subiti non hanno peso; se viene sempre interrotto, chi colpisce per primo blocca l'altro all'infinito.
- **Decisione:** `HitRecovery`, come l'hit recovery di Diablo 1. Se un colpo toglie almeno una frazione della vita massima (20% per lo scheletro), il fendente in corso viene annullato e il personaggio resta bloccato per 0,5 s, con l'animazione Hit. I colpi più deboli non interrompono e non fanno partire l'animazione.
- **Alternative scartate:** nessuna interruzione, che rende l'animazione di colpo subito incoerente con il danno che arriva lo stesso; interruzione sempre, che dà vantaggio decisivo a chi colpisce per primo e rende inutili le armi lente.
- **Conseguenze:** la spada del cavaliere (10 contro 30 di vita) interrompe sempre lo scheletro. La soglia è un parametro per personaggio, pronto per le statistiche della M8. Il cavaliere per ora non ha `HitRecovery`; va aggiunto quando arriveranno nemici con colpi abbastanza forti.

## ADR-011 — CI con GameCI e un account Unity Personal dedicato
- **Data:** 2026-10-03
- **Contesto:** test e build devono girare su GitHub Actions, e Unity, anche in batchmode, parte solo con una licenza attivata. Dal 2026 le licenze Personal non producono più il file `.ulf` da copiare nei secrets: l'attivazione passa dal Licensing Client di Unity, con login all'account. Il repo è pubblico.
- **Decisione:** GameCI (`unity-test-runner`, `unity-builder`) con i secrets `UNITY_EMAIL` e `UNITY_PASSWORD` di un **account Unity creato solo per la CI**, senza verifica in due passaggi. La CLI di GameCI attiva un posto all'inizio del job e lo restituisce alla fine. Un run alla volta in tutto il repo: un solo gruppo di `concurrency` (`unity`), senza `cancel-in-progress`.
- **Alternative scartate:** l'account principale di Mirco, che metterebbe la sua password nei secrets e richiederebbe di spegnere la 2FA; un runner self-hosted sul PC di Mirco, che userebbe la licenza già attiva ma funzionerebbe solo a PC acceso; un gruppo di `concurrency` per branch, che con un push su `main` e una PR in parallelo occuperebbe due posti.
- **Conseguenze:** se i secrets trapelano, è esposto solo un account senza niente sopra. I run si mettono in coda, e in coda ne resta uno solo: un push durante l'attesa della build di un tag la annulla. Un runner ucciso di colpo può lasciare un posto occupato, che si libera dalla pagina dell'account Unity.

## ADR-012 — Versioni della CI fissate, CLI di GameCI compresa
- **Data:** 2026-10-03
- **Contesto:** le action di GameCI sono involucri della `game-ci/cli`, che a settembre e ottobre 2026 riceveva correzioni sulle licenze quasi ogni giorno. Le action scaricano di default l'ultima versione della CLI anche quando l'action è fissata. Il primo run è fallito proprio per un'incompatibilità tra action e CLI (`--no-coverageEnabled` rifiutato).
- **Decisione:** tag esatti per GameCI (`unity-test-runner@v4.4.0`, `unity-builder@v6.0.0`) e per la CLI (`cliVersion: v0.1.71`); major per le action di GitHub (`checkout@v7`, `cache@v6`, `upload-artifact@v7`, tutte su Node 24); runner `ubuntu-24.04` invece di `ubuntu-latest`. La copertura del codice si spegne con `GAME_CI_COVERAGE_ENABLED`, non con l'input dell'action.
- **Alternative scartate:** tag mobili (`@v4`, CLI `latest`), che porterebbero dentro da soli i cambi mentre la gestione delle licenze si assesta: un run rosso potrebbe dipendere da un aggiornamento e non dal nostro codice.
- **Conseguenze:** i run sono riproducibili. Gli aggiornamenti si fanno a mano, insieme, con un run di prova; a quel punto si ricontrolla se l'input `coverageEnabled` è stato sistemato.

## ADR-013 — Test EditMode e PlayMode a ogni push, build solo sui tag
- **Data:** 2026-10-03
- **Contesto:** il piano prevedeva in CI solo i test EditMode, ma 36 test su 47 sono PlayMode e sono quelli che hanno trovato i problemi veri. Una build Windows richiede più minuti e produce un archivio da circa 40 MB.
- **Decisione:** il workflow dei test fa girare EditMode e PlayMode (solo i nostri assembly) a ogni push su `main` e a ogni pull request, con la cache degli oggetti LFS e di `Library/`. La build parte sui tag `m*` e `v*` e ad avvio manuale, e richiama il workflow dei test come workflow riusabile: un test rosso blocca la build.
- **Alternative scartate:** solo EditMode, che lascerebbe fuori quasi tutto quello che conta; build a ogni push, che consuma minuti e banda LFS per archivi che nessuno scarica.
- **Conseguenze:** ogni push riceve un verdetto in circa 5 minuti, e ogni tag di milestone produce la sua build da scaricare. I test non devono leggere pixel da una camera: in CI Unity gira senza grafica. Sui run rossi la CLI non crea il riepilogo dei risultati, quindi il nome del test fallito si legge nel log o nell'XML tra gli artifact.

## ADR-014 — Una scena `Core` sempre caricata, livelli additivi sopra
- **Data:** 2026-10-03
- **Contesto:** alla M2 tutto stava in una scena sola, e il restart la ricaricava. Con due livelli, vita e stato del cavaliere devono sopravvivere al cambio, e alla M6 un livello procedurale dovrà entrare nello stesso schema.
- **Decisione:** `Core` (indice 0 della build) contiene player, camera, HUD, EventSystem, `HitStop`, `CompositionRoot` e `LevelManager`. I livelli si caricano in modo additivo, uno alla volta, e diventano la **scena attiva**, così le loro impostazioni di luce valgono. Il `LevelManager` sfuma al nero, scarica il vecchio, carica il nuovo, fa `Warp` del player sull'ingresso e riaccende l'input. Il restart ricarica `Core` in modalità singola e riparte dal livello 1.
- **Alternative scartate:** `DontDestroyOnLoad` sul player e sull'HUD, che porta oggetti fuori da ogni scena, difficili da ripulire al restart e nei test; una scena `Bootstrap` prima di `Core`, che oggi non avrebbe niente da fare (resta l'idea per menu e caricamento).
- **Conseguenze:** i riferimenti a oggetti del livello (bersaglio, numeri di danno, hover) vanno puliti prima di scaricarlo. Se nell'editor un livello è già aperto, il `LevelManager` lo usa invece di caricarne una copia. I test partono da `Core` come la build.

## ADR-015 — Livelli scritti come mappe di testo e costruiti da uno strumento di editor
- **Data:** 2026-10-03
- **Contesto:** i livelli li compone Claude, che non può trascinare moduli nell'editor, e una scena Unity in un diff è illeggibile. Alla M6 un generatore dovrà produrre livelli dello stesso tipo.
- **Decisione:** ogni livello è una griglia di caratteri su celle di 4 m (`#` roccia, `.` pavimento, `T` torcia, `S` scheletro, `<` ingresso, `>` scala) con direttive `@` per le uscite. `LevelMap` la legge (logica pura, test EditMode); `LevelTileset`, uno ScriptableObject, dice quale prefab va dove e porta anche l'atmosfera; `LevelMapBuilder`, nell'assembly solo editor, rifà le scene con NavMesh cotto e tiene allineati i *Build Profiles*. Lo strumento resta nel repo.
- **Alternative scartate:** livelli composti a mano nell'editor, che solo Mirco potrebbe fare e che nessuno rivede in un diff; script usa e getta per ogni livello, da riscrivere a ogni modifica.
- **Conseguenze:** cambiare un livello vuol dire cambiare il testo e ricostruire. Un ritocco fatto a mano nella scena si perde alla ricostruzione successiva, quindi i ritocchi vanno nel builder o nei prefab. Alla M6 il generatore dovrà solo produrre la stessa griglia.

## ADR-016 — Solo luci in tempo reale, una sola con le ombre
- **Data:** 2026-10-03
- **Contesto:** il dungeon deve essere buio, con torce e un raggio di luce attorno al cavaliere. Alla M6 i livelli nasceranno a runtime, quindi le lightmap non si potranno cuocere. Ogni luce puntiforme con ombre occupa sei porzioni della mappa delle ombre.
- **Decisione:** niente lightmap né luce direzionale; ambiente a colore unico quasi nero e post-processing (ACES, bloom, vignetta) dal tileset. La luce del cavaliere è l'unica con le ombre: `PlayerLightRig` la tiene a 6 m d'altezza e 2,5 m verso la camera, indipendente da dove guarda lui. Una seconda luce senza ombre, all'altezza del petto, illumina solo il rendering layer `Player`. Le torce sono puntiformi senza ombre, con `TorchFlicker` (rumore di Perlin, fase presa dalla posizione).
- **Alternative scartate:** lightmap, impossibili sui livelli procedurali; luce del cavaliere proprio sopra la testa, che bruciava l'elmo e lasciava al buio il resto; abbassarla per illuminare il davanti, che restringeva il cerchio a terra; ombre anche sulle torce, troppo costose.
- **Conseguenze:** un livello qualsiasi, anche generato, ha la stessa luce senza passaggi di cottura. Il cavaliere si legge bene anche al buio, gli scheletri no. Un test controlla che in ogni livello ci sia una sola luce con le ombre e nessuna direzionale.

## ADR-017 — Muri bassi sui lati rivolti verso la camera
- **Data:** 2026-10-03
- **Contesto:** con la camera isometrica, un muro alto tra la camera e il cavaliere lo nasconde del tutto. In più il raggio del click si ferma sugli ostacoli (ADR-006), quindi dietro un muro alto non si potrebbe cliccare.
- **Decisione:** muri alti (4 m) sui lati nord ed est di ogni cella, balaustre in pietra di 1,1 m (`barrier` di KayKit) su sud e ovest, come nella visuale di Diablo. Torce e scale stanno contro un muro alto: la scala scende verso nord, sotto il muro che la nasconde.
- **Alternative scartate:** muri tutti alti con trasparenza vicino al cavaliere, che vuole uno shader apposta (si rivaluta alla M11); muri tutti bassi, che tolgono la sensazione di chiuso; `wall_half` di KayKit, che è un muro corto, non basso.
- **Conseguenze:** il cavaliere resta sempre visibile e cliccabile. Nelle mappe torce e scale vanno contro un muro nord o est. La rotazione della camera resta fissa: girandola, i lati bassi sarebbero sbagliati.
- **Aggiornamento (M8):** la camera ora gira a scatti di 90° e i lati alti e bassi la seguono (ADR-052). La regola resta: alti i due lati lontani dalla camera, bassi i due vicini.

## ADR-018 — Pubblicazione su itch.io rimandata alla M10
- **Data:** 2026-10-03
- **Contesto:** il piano anticipava la prima build pubblica alla M3, per avere presto un link giocabile e i primi riscontri. Pubblicare però vuol dire una pagina pubblica a nome di Mirco, un account in più e una chiave nei secrets.
- **Decisione:** su scelta di Mirco, il gioco va su itch.io quando è quasi completo: alla M10, quando è finibile e va fatto provare a cinque persone. Pagina creata da Mirco, caricamento dalla CI con butler sui tag (canale `windows`, versione uguale al tag, cartella di debug di Burst esclusa), chiave in un secret `BUTLER_API_KEY`.
- **Alternative scartate:** itch.io dalla M3, come diceva il piano; GitHub Releases, che non chiede account ma non dà la pagina del gioco né la build giocabile nel browser.
- **Conseguenze:** fino alla M10 la build si scarica solo dagli artifact della CI, quindi chi non ha GitHub non può provarla. Il passo con butler era già scritto: alla M10 si rifà in poco. La prova su un PC che non è quello di Mirco passa anch'essa alla M10.

## ADR-019 — Build Web dalla CI, pubblicata accanto a quella Windows
- **Data:** 2026-10-03
- **Contesto:** per chi guarda un portfolio, "gioca nel browser" vale più di "scarica". Il punto debole di URP sul web sono proprio molte luci e ombre, quindi andava giudicato sul dungeon vero.
- **Decisione:** la build Web si fa in CI, ad avvio manuale di `build.yml` con `targetPlatform: WebGL`. Usa compressione Gzip con *decompression fallback*, così parte anche da server che non mandano gli header di compressione, come itch.io o un server locale. La qualità resta *PC*, la stessa di Windows. Provata da Mirco il 3 ottobre 2026: luci e fluidità reggono. Alla M10 va su itch.io nel canale `web`, accanto a Windows.
- **Alternative scartate:** solo Windows, che rinuncia al link giocabile; qualità *Mobile* per il Web, che toglieva le ombre del cavaliere e il Forward+; Brotli, che con il *fallback* si decomprime più lentamente nel browser.
- **Conseguenze:** non serve il modulo *Web Build Support* sul PC di Mirco. Ogni effetto della M11 (shader retro, post-processing) va riprovato anche nel browser. Sul web `Application.Quit` non fa niente, e l'audio parte solo dopo il primo click.

## ADR-020 — Attributi di Diablo, formula del colpo e modificatori sommati
- **Data:** 2026-10-04
- **Contesto:** fino alla M3 ogni colpo andava a segno con un danno fisso. Con gli oggetti servono statistiche che gli oggetti possano cambiare, e alla M5 gli affissi aggiungeranno modificatori fissi e percentuali sulla stessa statistica.
- **Decisione:** quattro attributi più l'Armatura in uno `StatSheet` (logica pura); il cavaliere parte da 30 / 20 / 10 / 25 come il guerriero di Diablo. Vita = 50 + 2 × Vitalità. Colpire = 75 + Destrezza / 2 − Armatura del bersaglio, limitata tra 5 e 95. Danno = tiro intero tra minimo e massimo dell'arma × (1 + Forza / 100). Valore di una statistica = (base + fissi) × (1 + somma delle percentuali / 100). Ogni modificatore ricorda la sua sorgente (l'oggetto), e `RemoveModifiersFrom` li toglie tutti insieme. Gli scheletri hanno un `CharacterStats` con la sola Destrezza 10 e Armatura 10.
- **Alternative scartate:** percentuali moltiplicate tra loro, che con gli affissi crescono in modo esponenziale e nel tooltip non si leggono; modificatori tolti uno per uno, dove basta dimenticarne uno perché togliere un oggetto non riporti le statistiche a prima; attributi completi anche per i nemici, che oggi nessuno userebbe.
- **Conseguenze:** il bilanciamento della M2 resta: con la spada corta (6–9) il cavaliere fa 7,8–11,7 a colpo e colpisce lo scheletro 3 volte su 4. La vita massima è ancora fissa all'avvio: quando alla M5 un affisso cambierà la Vitalità, va deciso cosa succede alla vita corrente. **Aggiornato dalla M5:** il danno si arrotonda all'intero (ADR-031); la vita massima che cambia segue D10 della scheda M5.

## ADR-021 — Tiri del combattimento da una sorgente iniettata
- **Data:** 2026-10-04
- **Contesto:** con colpi mancati e danno variabile, i test che contano i colpi (lo scheletro muore al terzo) diventano casuali. La M5 vorrà un seme riproducibile per il loot.
- **Decisione:** i tiri passano da `IRandomSource`. Il `CompositionRoot` crea una `SystemRandomSource` con un seme qualsiasi e la passa al cavaliere e, a ogni livello caricato, agli attacchi dei nemici. I test usano `FixedRandomSource`, che restituisce in ciclo i valori dati: con 0,0 ogni colpo va a segno con il danno minimo. Un attacco senza sorgente lancia un'eccezione invece di tirare a caso.
- **Alternative scartate:** `UnityEngine.Random`, globale e condiviso con suoni e animazioni, quindi un seme fissato nei test non basterebbe; allentare le asserzioni dei test, che smetterebbero di verificare il numero di colpi.
- **Conseguenze:** i test di combattimento restano deterministici senza cambiare le asserzioni. Un attaccante nuovo deve ricevere la sorgente dal `CompositionRoot`, altrimenti il primo colpo lo dice subito. Il loot della M5 avrà una sorgente sua, con il seme salvato.

## ADR-022 — Oggetti con ID stabile, definizione immutabile e istanza separata
- **Data:** 2026-10-04
- **Contesto:** un inventario va salvato su file alla M8, e `JsonUtility` non sa salvare un riferimento a uno ScriptableObject: scrive un identificativo che cambia a ogni avvio (piano § 4.4). Alla M4 l'arma era già una `WeaponDefinition`, letta da `MeleeAttack`.
- **Decisione:** `ItemDefinition` è uno ScriptableObject astratto e immutabile: ID generato una volta (GUID), nome, icona, celle, modello, slot, posizione in mano. `WeaponDefinition` lo estende, assorbendo quella di prima (spostata con `git mv`, stesso GUID dell'asset), e `ArmorDefinition` aggiunge l'Armatura. `ItemInstance` è una classe serializzabile con il solo ID; `ItemDatabase` risolve l'ID nella definizione e raccoglie gli oggetti di `Data/Items` da un menu dell'editor. I colpi dei nemici e i pugni restano `WeaponDefinition` in `Data/Attacks`, fuori dal database.
- **Alternative scartate:** riferimenti diretti alle definizioni nell'inventario, da riscrivere alla M8; il nome dell'asset come ID, che cambia a ogni rinomina; due asset per la stessa spada (oggetto e arma).
- **Conseguenze:** l'inventario si potrà salvare così com'è. Duplicare un asset copia anche l'ID: un test controlla che gli ID siano unici e che ogni definizione sia nel database. Con `git mv` il nome interno dell'asset resta quello vecchio e va corretto a mano.

## ADR-023 — Inventario a click-e-click, come in Diablo 1
- **Data:** 2026-10-04
- **Contesto:** il piano prevedeva il drag & drop. Il gioco di riferimento usa un'altra regola, e lo scambio tra due oggetti con il trascinamento è scomodo.
- **Decisione:** una pressione prende l'oggetto sul cursore, un'altra lo posa centrato sulla cella e spostato dentro i bordi. Su un oggetto solo li scambia, su due non fa niente. Lo slot equipaggia se l'oggetto è giusto e i requisiti bastano. Con un oggetto preso, un fondo trasparente dietro le finestre prende la pressione fuori dai pannelli e lascia l'oggetto a terra ai piedi del cavaliere; chiudendo l'inventario l'oggetto torna nella griglia. Si usa `OnPointerDown`, non il click, così prendere è immediato. La logica sta in `Inventory`, pura; i pannelli inoltrano e si ridisegnano sugli eventi.
- **Alternative scartate:** drag & drop, che obbliga a tenere premuto e rende lo scambio poco naturale; un click destro per equipaggiare, che Diablo 1 non ha.
- **Conseguenze:** nei test sono due pressioni invece di un trascinamento. L'immagine sul cursore non deve essere bersaglio dei raggi, i pannelli sì. Il tooltip non si mostra mentre si tiene un oggetto.

## ADR-024 — Icone fatte dai modelli 3D con uno strumento di editor
- **Data:** 2026-10-04
- **Contesto:** ogni oggetto ha bisogno di un'icona per la griglia. KayKit fornisce modelli, non icone, e un pacchetto di icone a parte vorrebbe dire un altro stile e un'altra licenza da verificare (ADR-004).
- **Decisione:** il menu *DarkDescent → Oggetti → Rigenera le icone* fotografa ogni modello in una scena di anteprima: camera ortografica, sfondo trasparente, 128 pixel per cella, rotazione propria di ogni oggetto (lo scudo va girato di 180° per mostrare lo stemma). Le PNG finiscono in `Art/Icons/` come sprite e si committano.
- **Alternative scartate:** icone disegnate o prese da un pacchetto, con stile e licenza diversi; icone renderizzate a runtime, che costano memoria e tempo all'avvio per immagini che non cambiano mai.
- **Conseguenze:** un oggetto nuovo della M5 ha l'icona con un click, nello stesso stile degli oggetti a terra. Se cambia un modello, l'icona va rigenerata.

## ADR-025 — Lingue con una tabella CSV e un `Localizer` nostro
- **Data:** 2026-10-05
- **Contesto:** alla M5 Mirco ha chiesto il gioco in più lingue: inglese di default, italiano, e altre in futuro (D12 e D13 della scheda M5). Fino alla M4 ogni testo era in italiano, scritto a mano in scene, codice e definizioni degli oggetti.
- **Decisione:** una tabella `Data/Localization/Strings.csv` con una riga per chiave e una colonna per lingua (`key,en,it`), commenti con `#` e `\n` per andare a capo. `StringTable` la legge; `Localizer`, logica pura, risponde nella lingua attiva, ricade sull'inglese se manca una traduzione e restituisce `#chiave` se manca la chiave. Le etichette delle scene hanno un `LocalizedText` con la chiave, il codice chiede per chiave (costanti in `TextKeys`), le definizioni tengono una chiave al posto del nome. Cambiare lingua manda `LanguageChanged`, e chi mostra testo si ridisegna. La lingua viene da `-lang` sulla riga di comando, altrimenti dalla preferenza salvata (`PlayerPrefs`); `F9` passa alla successiva finché alla M10 non c'è il menu delle opzioni.
- **Alternative scartate:** il pacchetto *Unity Localization*, che porta con sé Addressables, carica le tabelle in modo asincrono (in build Web non si può aspettarle) e aggiunge un passaggio di build alla CI; stringhe in un ScriptableObject per lingua, che non si rivedono in un diff e non si passano a un traduttore.
- **Conseguenze:** una lingua nuova è una colonna, più lo schema dei nomi e il genere delle basi (ADR-028). Un test controlla che ogni chiave usata da scene, codice e definizioni esista e che ogni lingua abbia tutte le righe. I test girano in inglese, con la preferenza cancellata prima di ogni scena. Una lingua con un altro alfabeto vorrà un font di riserva per TextMesh Pro, con la sua licenza (ADR-004).

## ADR-026 — Blocco con lo scudo, tirato dopo il colpo a segno
> Cambiato il 7 ottobre 2026 (ADR-049): il blocco non interrompe più il colpo del cavaliere.

- **Data:** 2026-10-05
- **Contesto:** alla M4 lo scudo dava solo Armatura, che abbassa la probabilità di essere colpiti di qualche punto: provandolo, Mirco non ha sentito differenza e ha chiesto il blocco (D1 della scheda M5).
- **Decisione:** `ShieldBlock` sul cavaliere: probabilità = blocco dello scudo + Destrezza / 2, tra 0 e 75, e solo con uno scudo in mano. L'ordine dei tiri è colpito, poi bloccato, poi danno: un colpo bloccato non tira il danno. Il blocco mostra "Blocked" sopra il cavaliere, suona `impactMetal_light` di Kenney, fa partire `Melee_Block_Hit` e interrompe per 0,45 s il colpo che il cavaliere stava dando, come in Diablo 1. Gli scheletri non bloccano.
- **Alternative scartate:** più Armatura sugli scudi, che resta un numero che non si vede; un blocco che riduce il danno invece di annullarlo, poco leggibile; un blocco senza interruzione, che sarebbe solo un bonus.
- **Conseguenze:** con lo scudo con stemma e la Destrezza 20 del cavaliere il blocco è al 20%, e lo scheletro lo ferisce 3 volte su 5 invece di 4. La Destrezza ha un secondo uso. Gli scudi si distinguono per blocco e Armatura, e un affisso ("della Parata") alza il blocco.

## ADR-027 — Rarità e affissi: gruppi esclusivi, valori interi, effetti dell'oggetto o del personaggio
- **Data:** 2026-10-05
- **Contesto:** il loot casuale è il cuore del genere. Servono oggetti che si distinguano e numeri che si leggano nel tooltip (D2 e D3 della scheda M5).
- **Decisione:** tre rarità tirate da `RarityTable`: normale (65%, nessun affisso), magico (28%, 1–2 affissi, al più un prefisso e un suffisso), raro (7%, 3–4 affissi, al più due prefissi e due suffissi). Undici `AffixDefinition` immutabili, ciascuna con un intervallo di valori **interi**, un livello minimo, i tipi di oggetto ammessi (`AffixTargets`) e un **gruppo**: due affissi dello stesso gruppo non stanno sullo stesso oggetto. Gli effetti sul danno e sull'Armatura in percentuale, sull'Armatura fissa e sul blocco sono **dell'oggetto** e cambiano i suoi numeri; Forza, Destrezza, Vitalità, a colpire e vita sono **del personaggio** e passano dallo `StatSheet` con l'oggetto come sorgente (ADR-020). `ItemGenerator` tira rarità e affissi; se il pool non basta, l'oggetto ne prende quanti ce ne sono.
- **Alternative scartate:** una quarta rarità leggendaria, chiesta e poi tolta da Mirco; gli unici, che vogliono oggetti scritti a mano uno per uno; valori con la virgola, che nel tooltip dipendono dalla cultura (lezione della M4); percentuali di danno applicate al personaggio, per cui "+40% danno" cambierebbe anche i pugni.
- **Conseguenze:** con otto scheletri nei due livelli fatti a mano esce un raro ogni due o tre partite; le probabilità si ritarano alla M10. Le percentuali di un oggetto arrotondano per difetto (la spada corta +40% fa 8–12). Un test su 100.000 estrazioni verifica le rarità entro l'1%: con 10.000 la tolleranza era più stretta della varianza.

## ADR-028 — Nomi degli oggetti composti per lingua, con schema e genere
- **Data:** 2026-10-05
- **Contesto:** "Savage Short Sword of the Griffin" in italiano è "Spada corta Feroce del Grifone": cambiano l'ordine delle parole e l'accordo dell'aggettivo. Un nome incollato con il `+` funziona solo in inglese (D4 della scheda M5).
- **Decisione:** `ItemNamer` compone il nome con lo schema della lingua (`item.name.pattern`: `{prefix} {base} {suffix}` in inglese, `{base} {prefix} {suffix}` in italiano). Ogni base ha un genere per lingua (`item.<base>.gender`: `n` in inglese, `m`/`f` in italiano); i prefissi hanno la forma femminile in una riga `.f`, e la forma senza suffisso fa da riserva. Il raro si chiama con il primo prefisso e il primo suffisso, come il magico. Il nome non si salva mai: si compone nella lingua attiva ogni volta che serve, a terra e nel tooltip.
- **Alternative scartate:** nomi composti in codice per ogni lingua, che vogliono codice nuovo a ogni lingua; nomi salvati con l'oggetto, che resterebbero nella lingua in cui è caduto; nomi inventati per i rari, alla Diablo, che vogliono liste per lingua e si possono aggiungere dopo.
- **Conseguenze:** una lingua con un altro ordine o un altro genere (il neutro tedesco) aggiunge righe, non codice. Un test controlla che ogni base abbia il genere in ogni lingua e che ogni prefisso abbia le sue forme. Cambiando lingua, cambia anche il nome di un oggetto già nell'inventario.

## ADR-029 — Seme della partita, PRNG nostro e seme per nemico
- **Data:** 2026-10-05
- **Contesto:** "rigenera il drop 4711" serve a provare e a correggere il loot. Con una sorgente sola per combattimento e loot, un colpo mancato in più cambierebbe tutti gli oggetti dopo. `System.Random` non promette lo stesso algoritmo tra versioni di .NET, e `string.GetHashCode` cambia tra runtime (D5 e D6 della scheda M5).
- **Decisione:** un seme di partita da `-seed N`, altrimenti dall'orologio, scritto nel log con il comando per rigiocarlo. Il loot usa `SplitMix64Source`, scritto da noi e verificato con valori noti, separato dalla sorgente del combattimento (ADR-021). Ogni nemico ha un seme suo, `SeedMixer.ForEnemy(seme, profondità, cella)`, con la cella in decimetri presa in `Awake`: lo stesso nemico lascia lo stesso oggetto in qualunque ordine si uccidano. `ItemInstance` salva rarità, livello, seme e gli affissi con il **valore tirato**.
- **Alternative scartate:** `UnityEngine.Random` o `System.Random` con seme, che non garantiscono la stessa sequenza in build Web e dopo un aggiornamento; una sola sequenza per tutta la partita, che dipende dall'ordine delle uccisioni; salvare solo il seme, che rigenerebbe oggetti diversi dopo una ritaratura.
- **Conseguenze:** Mirco ha verificato in build che con `-seed 4711` gli stessi scheletri lasciano gli stessi oggetti. Aggiungere un affisso o un tiro al generatore cambia il risultato di ogni seme: i test confrontano due generazioni, non un oggetto scritto a mano. Alla M6 il seme del livello generato verrà dallo stesso seme di partita.

## ADR-030 — Loot table per nemico e livello dell'oggetto dalla profondità
- **Data:** 2026-10-05
- **Contesto:** alla M4 ogni scheletro lasciava sempre la sua lama, per tenere i test deterministici. Ora il determinismo lo dà il seme, e i drop possono variare (D8 della scheda M5).
- **Decisione:** `LootTable`, uno ScriptableObject per tipo di nemico: probabilità di lasciare qualcosa (scheletro 70%) e basi pesate. `LootRoller` tira, nell'ordine, se cade qualcosa, la base, poi rarità e affissi. Il livello dell'oggetto è la profondità del livello, scritta nella mappa con `@depth`. A terra nome e luce prendono il colore della rarità. Lo scudo messo dalla mappa del livello 1 resta normale.
- **Alternative scartate:** una tabella sola per tutto il gioco, che non distingue i nemici della M7; livello dell'oggetto dal nemico, quando oggi c'è un nemico solo.
- **Conseguenze:** i nemici nuovi della M7 avranno una tabella loro, e le casse della M6 potranno usarne una. Gli affissi di livello 2 (Feroce, Massiccio, del Grifone) escono solo dal secondo livello in giù.

## ADR-031 — Danno intero
- **Data:** 2026-10-05
- **Contesto:** con la formula di ADR-020 il danno aveva i decimali (la lama 8–12 per la Forza 25 fa 13,75), quindi la vita restava frazionaria. Provando la build della M5, Mirco ha visto ogni tanto un "0": all'ultimo colpo restava 0,25 di vita, e il numero mostrava il danno assorbito arrotondato.
- **Decisione:** `RollDamage` arrotonda all'intero, come già faceva il pannello del personaggio, e non scende mai sotto 1.
- **Alternative scartate:** mostrare il danno tirato invece di quello assorbito, che lascia la vita frazionaria nascosta nella sfera; arrotondare solo il numero mostrato, che continuerebbe a mostrare 0.
- **Conseguenze:** la vita resta intera finché lo sono la vita massima e le cure. Il danno medio non cambia. Aggiorna ADR-020.

## ADR-032 — Due assembly: logica e dati in `DarkDescent.Core`, componenti in `DarkDescent`
- **Data:** 2026-10-05
- **Contesto:** ADR-003 rimandava la divisione dell'asmdef alla M6, sulle dipendenze reali; D11 della scheda M6 la limitava alla logica pura, e solo se non c'erano cicli da rompere. Il grafo delle cartelle di `Scripts/`, ricavato dai tipi usati e non solo dagli `using`, è un unico groviglio: tutte le aree tranne Audio, Input, Rendering e Stats stanno in un ciclo. Gran parte passa dal CompositionRoot, che collega tutto, ma restano cicli veri tra aree: Combat e Items (`MeleeAttack` usa `WeaponDefinition`, l'inventario usa `Health` e `ShieldBlock`), Levels e UI (`LevelManager` usa `ScreenFader`, l'automappa usa `Exploration`). Il codice senza MonoBehaviour invece non usa mai componenti di scena: l'unica eccezione è `LevelBuilder`, che non è un componente ma monta le scene.
- **Decisione:** due assembly, a strati. `DarkDescent.Core` (`Scripts/Core/`) contiene logica e dati: semi e generatori di numeri, formule, vita, statistiche, griglia, inventario, cintura, istantanea, generatori di oggetti e di dungeon, mappe, esplorazione, lingue, descrizioni degli oggetti e gli ScriptableObject delle definizioni. Dipende solo da UnityEngine (tipi valore, ScriptableObject, Texture2D). `DarkDescent` (`Scripts/<Area>/`) contiene i MonoBehaviour, `LevelBuilder`, `LevelTileset` (usa `VolumeProfile` della render pipeline) e la classe generata dall'Input System, e referenzia `DarkDescent.Core`. Dentro `Core` le sottocartelle seguono le aree, e i namespace restano quelli dell'area (`DarkDescent.Items` sta sia in `Core/Items/` sia in `Items/`). CompositionRoot e HitStop, gli unici componenti della vecchia cartella `Core`, passano in `Composition/` e tengono il namespace `DarkDescent.Core`. I 56 file si sono spostati con `git mv`: i GUID dei `.meta` non cambiano, e scene e asset trovano gli script come prima.
- **Alternative scartate:** un assembly per area, che chiede di rompere i cicli veri con interfacce che servono solo alla divisione. Un assembly della logica senza UnityEngine (`noEngineReferences`): ci starebbero una ventina di file (formule, vita, statistiche, semi, lingue), perché griglia, inventario e generatori usano `Vector2Int`, `RectInt` e le definizioni ScriptableObject; la garanzia in più non vale una terza cartella. Restare a un assembly: compila in pochi secondi, ma niente impedisce a una classe di logica di prendere un componente di scena.
- **Conseguenze:** la regola "la logica pura non conosce la scena" (piano § 4.1) ora la controlla il compilatore: una classe in `Core` che usa un MonoBehaviour non compila, e un test controlla che `Core` non ne contenga. Un file nuovo va nell'assembly giusto: in `Core/<Area>/` se non è un componente e non usa pacchetti, altrimenti in `<Area>/`. Gli assembly dell'editor e dei test referenziano tutti e due. Modificare un componente non ricompila più la logica.

## ADR-033 — Un builder solo, a runtime, per i livelli a mano e per quelli generati
- **Data:** 2026-10-05
- **Contesto:** fino alla M5 `LevelMapBuilder` (ADR-015) costruiva le scene nell'editor con `PrefabUtility` e `SerializedObject`: non poteva girare in build. Il generatore della M6 deve costruire una cripta a ogni partita (D3 e D9 della scheda M6).
- **Decisione:** la costruzione esce dall'editor e diventa `LevelBuilder`, nell'assembly del gioco. Prende una `LevelMap` e tre funzioni: come istanziare un prefab (l'editor tiene il legame con il prefab, il gioco usa `Instantiate`), come trovare un oggetto di `@items`, chi avvisare quando cambia un'istanza. I campi che l'editor scriveva con `SerializedObject` passano da `Configure` sui componenti. Tutto nasce sotto la radice del livello spenta, accesa alla fine, così gli `Awake` trovano il livello finito. Le cripte stanno in una scena `Level_Crypt` con le sole impostazioni di luce e un `DungeonLevel` che genera e costruisce quando il `LevelManager` la carica con una profondità.
- **Alternative scartate:** un secondo builder per il runtime accanto a quello dell'editor, che divergerebbe alla prima correzione; una scena salvata per ogni livello generato, che non è un dungeon generato.
- **Conseguenze:** un livello a mano e uno generato sono identici per muri, luci, click e NavMesh. Verificato descrivendo le due scene a mano oggetto per oggetto prima e dopo il cambio: descrizione identica. `LevelMapBuilder` è sceso da 460 a un centinaio di righe. Le mappe a mano restano come livelli di prova per i test. L'assembly del gioco referenzia `Unity.AI.Navigation`.

## ADR-034 — Cripte con BSP su celle intere, seme per livello
- **Data:** 2026-10-05
- **Contesto:** quattro livelli generati a ogni partita, sempre percorribili, e lo stesso dungeon con lo stesso seme anche in build Web (D2 e D8 della scheda M6, trappola 8).
- **Decisione:** `DungeonGenerator`, logica pura e solo interi: BSP su una griglia di 28 × 28 celle da 4 m, divisione finché una zona supera 11 celle e mai sotto 6, una stanza da 3 a 7 celle per lato in ogni foglia con almeno una cella di roccia attorno, corridoi a L tra le stanze più vicine dei due rami di ogni divisione, risalendo l'albero. Scala nella stanza più lontana a piedi, contata con una visita in ampiezza. Il contenuto (`DungeonPopulator`, numeri in `DungeonSettings`) usa la stessa sorgente subito dopo la pianta e scrive gli stessi marcatori delle mappe a mano. Il seme è `SeedMixer.ForLevel(seme della partita, profondità)` su `SplitMix64Source` (ADR-029).
- **Alternative scartate:** automi cellulari o random walk, che fanno caverne e non stanze (si valutano per le caverne della M7); collegare stanze a caso e poi aggiustare quelle isolate, quando collegando i rami il livello è connesso per costruzione; `System.Random` e `float`, che non garantiscono la stessa sequenza tra piattaforme.
- **Conseguenze:** i test su 500 semi (connessione, stanze separate, scala lontana, bordi, stesso seme stessa mappa) girano in meno di mezzo secondo. Il seme di un livello non coincide mai con quello di un nemico, quindi la mappa non cambia il loot. Un tiro in più nel generatore cambia tutte le cripte: i test confrontano due generazioni, non una mappa scritta.

## ADR-035 — NavMesh cotto a runtime, dai collider
- **Data:** 2026-10-05
- **Contesto:** il piano chiedeva di scegliere tra NavMesh e un A\* nostro con i dati (D4 della scheda M6). Movimento, inseguimento ed evitamento si basano sul `NavMeshAgent` dalla M1. I modelli KayKit sono importati senza Read/Write.
- **Decisione:** `NavMeshSurface.BuildNavMesh()` dopo la costruzione, con la geometria presa dai **collider** (`PhysicsColliders`), a schermo già nero. I nemici nascono spenti e si accendono dopo il bake, così si agganciano al NavMesh appena cotto. La soglia era 1 s: in build il primo livello costa 44 ms (generazione 10, costruzione 18, NavMesh 16), i successivi 12–15 ms.
- **Alternative scartate:** A\* su griglia, che vorrebbe anche movimento ed evitamento nostri, senza un problema da risolvere; il bake dalle mesh renderizzate, che nell'editor funziona e in build viene vuoto (Unity lo segnala); abilitare Read/Write su tutti i modelli, che ne raddoppia la memoria.
- **Conseguenze:** un livello costa al più il 4,4% del tempo concesso. I collider devono descrivere bene il calpestabile: un oggetto di scena senza collider non buca il NavMesh. Le caverne della M7 useranno lo stesso bake.

## ADR-036 — Ripartenza dall'ingresso del livello, con l'istantanea dell'inventario
> Sostituito da ADR-048 il 7 ottobre 2026: il livello non si ricarica più.

- **Data:** 2026-10-05
- **Contesto:** dopo la prova della build Mirco ha chiesto che morendo si ripartisse dal livello in cui si è morti, con quello che si aveva entrando (D13 della scheda M6). Prima "Ricomincia" ricaricava `Core`, quindi cavaliere e HUD nuovi: l'istantanea avrebbe dovuto sopravvivere in un oggetto `DontDestroyOnLoad` o in un campo statico, lo stato globale che ADR-007 esclude.
- **Decisione:** `Core` resta caricata e si ricarica solo il livello, come per le scale: stessa scena, stessa profondità, stesso seme, quindi la stessa cripta con nemici e casse rimessi. `LevelManager.RestartLevel` riceve una richiamata che gira a schermo nero, tra lo scarico del livello vecchio e il carico del nuovo: lì il cavaliere torna in vita (`Health.Revive`) e l'inventario torna com'era. L'istantanea (`InventorySnapshot`) è JSON con le istanze come sono (ADR-022), presa un frame dopo ogni ingresso. La mappa scoperta resta, su richiesta di Mirco.
- **Alternative scartate:** ricaricare `Core` con lo stato in un singleton; ripartire dal livello 1 con un seme nuovo e senza niente, il comportamento di prima, che toglie la voglia di scendere; ripartire senza ripristinare l'inventario, che premia chi muore apposta per riaprire una cassa.
- **Conseguenze:** nessuno scheletro può colpire il cavaliere mentre si rialza. I componenti che reagiscono alla morte devono reagire anche a `Revived`. L'istantanea è già il formato del salvataggio della M8, dove le regole della morte diventano definitive.

## ADR-037 — Pozioni con un tiro e un seme a parte, cintura dentro l'inventario
- **Data:** 2026-10-05
- **Contesto:** senza cure quattro livelli non si finiscono (D14 della scheda M6). Il loot del seme 4711 era già verificato in build: aggiungere un tiro alla sequenza di un nemico avrebbe cambiato tutti gli oggetti.
- **Decisione:** `PotionDefinition` (1 × 1, 50% della vita massima, arrotondato all'intero come il danno di ADR-031). La probabilità di una pozione sta nella `LootTable` (scheletri 25%, casse 50%) e si tira con `SeedMixer.ForPotion`, che mescola il seme del nemico con un dominio suo. La `Belt` da 8 posti è logica pura dentro `Inventory`: la raccolta prova prima la cintura, poi la griglia. I tasti 1–8 sono un'azione sola con otto binding. A vita piena la pozione non si beve, diversamente da Diablo 1.
- **Alternative scartate:** tiro della pozione nella stessa sequenza dell'oggetto, che cambia il loot già legato al seme; una cintura separata dall'inventario, che raccolta, istantanea e ritorno dal cursore avrebbero dovuto conoscere a parte; vita che si rigenera, che toglie peso alla scelta di bere.
- **Conseguenze:** gli oggetti del seme 4711 sono rimasti identici, e un test lo controlla. L'istantanea della ripartenza copia anche la cintura. Mana e mercante, alle M9 e M10, aggiungono tipi di pozione come altre `PotionDefinition`.

## ADR-038 — Automappa da una texture disegnata a celle, ruotata come la camera
- **Data:** 2026-10-05
- **Contesto:** nelle cripte generate ci si perde (D15 della scheda M6). La griglia della cripta c'è già, e la finestra dell'editor la disegnava in una texture.
- **Decisione:** `Exploration` (logica pura) scopre le celle con una visita sul pavimento a 3 passi dal cavaliere, anche in diagonale ma senza tagliare gli spigoli: la stanza dietro un muro resta nascosta. Il tracker lavora solo quando il cavaliere cambia cella, e `MapPainter.PaintExplored` ridisegna una texture da 20 pixel per cella con un buffer riusato. Le due viste sono la stessa texture in un genitore ruotato di 45° e schiacciato a metà, come la camera da 45° e 30°, così il nord cade dove cade sullo schermo. Il tasto `M` scorre angolo, sovrapposta e spenta. Nessuna immagine prende i raggi.
- **Alternative scartate:** una seconda camera dall'alto, che disegna la scena una volta in più a ogni frame e mostra anche quello che non si è scoperto; scoperta a raggio, che vede attraverso i muri; mappa con il nord in alto, che non corrisponde a quello che si vede.
- **Conseguenze:** la mappa costa un ridisegno per cella attraversata, mai per frame. Le scene salvate a mano non hanno la mappa del livello, quindi lì l'automappa resta spenta. Alla M7 basta che le caverne producano la stessa griglia.

## ADR-039 — Ambiente sonoro per tipo di livello, dal tileset
- **Data:** 2026-10-06
- **Contesto:** dopo la prova della M6 Mirco ha chiesto una musica di sottofondo nello stile di Diablo e rumori d'ambiente che mettano paura. Il piano la metteva alla M11; il mixer aveva già un gruppo `Music`, vuoto. Le tracce CC0 trovate hanno volumi molto diversi: la musica della cripta ha la mediana a −43 dB, il loop delle caverne a −17 dB.
- **Decisione:** un `AmbienceProfile` (ScriptableObject in `Core`) per tipo di livello, appeso al `LevelTileset`, con musica, fondo in loop e una serie di versi singoli. Il `LevelContext` lo riceve dal builder. Un `AmbiencePlayer` in `Core` lo riceve dal composition root a ogni ingresso. Se il profilo è lo stesso, la musica continua; altrimenti sfuma nell'altro in 2,5 s, con due coppie di sorgenti. I versi nel buio partono ogni 20–50 s da un punto in piano a 10–18 m dal cavaliere, in 3D, con un passa-basso a 2,2 kHz. Ogni volta hanno un'intonazione diversa, e mai lo stesso verso due volte di fila. La logica di quando, dove e quale sta in `StingerSchedule`, pura e con la sorgente di numeri passata da fuori. Musica e fondo vanno nel gruppo `Music`, a +14 dB; i volumi delle tracce si pareggiano nel profilo. I versi vanno nel gruppo `SFX`. I versi sono ritagliati dalle stesse tracce CC0 dove il segnale supera il fondo di 13–22 dB, con dissolvenze e normalizzati a −3 dB.
- **Alternative scartate:** una musica sola per tutto il gioco, che toglie alle caverne un carattere loro; una `AudioSource` nella scena di ogni livello, che ricomincia a ogni scala; i versi come suoni 2D, che non si capisce da dove vengano; tracce rinormalizzate in WAV, che avrebbero fatto pesare il repository 36 MB in più.
- **Conseguenze:** un nuovo tipo di livello (la città della M10) è un profilo in più. Il menu delle opzioni della M10 regola musica ed effetti attraverso i due gruppi del mixer. Il limite di voci del passo 7.8 deve lasciare passare i versi, che sono al più due per volta.

## ADR-040 — Caverne con il random walk sulla griglia da 4 m
- **Data:** 2026-10-06
- **Contesto:** la M7 porta un secondo tipo di livello, le caverne, dalla profondità 5 all'8. La cripta usa il BSP su celle da 4 m (ADR-034), e su quella griglia lavorano builder, NavMesh, automappa, ripartenza e finestra dell'editor. Le caverne devono sembrare l'opposto delle stanze: corridoi larghi e irregolari.
- **Decisione:** un `CaveGenerator` in `Core/Levels`, logica pura su interi, che produce lo stesso `DungeonLayout` della cripta. Quattro camminatori partono dal centro e scavano a turno finché il pavimento è il 37% dell'area dentro il bordo. Ogni camminatore tiene la direzione due volte su tre; ogni tanto riparte da una cella già scavata, ogni tanto scava un quadrato di 2 × 2. Poi due passate di smussatura, le celle unite solo in diagonale si collegano, i pilastri di una cella si riempiono e resta la regione connessa più grande, tutto con i quattro vicini. L'ingresso è la cella aperta più lontana dal centro, la scala la più lontana a piedi dall'ingresso. Il contenuto lo mette un `CavePopulator` a parte, che ragiona su passi e celle invece che su stanze, con gli stessi simboli della cripta. L'aspetto è un `LevelTileset` delle caverne con i modelli CC0 del *Dungeon Pack*: terra, roccia crepata, macerie basse, candele a terra al posto delle torce, ambiente più freddo. Le varianti dei pezzi le sceglie il builder con una mescola della posizione. La scena è `Level_Caves`, dopo la cripta.
- **Alternative scartate:** l'automa cellulare puro, che dà caverne a macchie con molte regioni da ricucire; passi del tutto casuali, provati per primi, che facevano una sala unica; una griglia più fine solo per le caverne, che avrebbe cambiato builder, NavMesh e automappa; aggiungere le caverne al popolatore della cripta, che avrebbe cambiato i livelli dei semi già provati.
- **Conseguenze:** su 500 semi il pavimento va dal 36 al 43% e la scala è ad almeno 24 passi; la generazione costa 1–2 ms. I nuovi FBX del pacchetto hanno la radice a −90° su X e scala 100: uno script che li mette in un prefab non deve forzarne rotazione e scala, e un test controlla la misura vera di ogni pezzo del tileset.

## ADR-041 — Un generatore per tipo di livello, scelto dalle impostazioni, e profondità concatenate
- **Data:** 2026-10-06
- **Contesto:** con due algoritmi, `DungeonLevel`, la finestra dell'editor e i test non devono sapere quale ha fatto la mappa. La cripta finiva al 4 con una scala che portava nella stessa scena, e il nome della scena veniva passato a mano.
- **Decisione:** un'interfaccia `ILevelGenerator`. `DungeonSettings.CreateGenerator()` dà il BSP, e `CaveSettings`, che eredita misure e contenuto, lo sostituisce con il random walk. Le impostazioni sanno anche la loro scena, la prima e l'ultima profondità e la scena dopo: la cripta va dall'1 al 4 e poi a `Level_Caves`, le caverne dal 5 all'8 e poi a nessuna. Prima dell'ultima profondità la scala riporta nella stessa scena, all'ultima porta alla scena dopo, senza scena dopo sparisce. La visita in ampiezza comune ai due generatori sta in `LevelGrid`.
- **Alternative scartate:** uno `switch` sul tipo di livello in `DungeonLevel`; un generatore unico con parametri per tutti e due i tipi; l'elenco delle scene in un posto centrale, da tenere allineato a mano con le impostazioni.
- **Conseguenze:** un terzo tipo di livello (la città della M10) è una coppia impostazioni e generatore in più. Le mappe della cripta non sono cambiate. La finestra dell'editor sceglie il tipo e limita la profondità a quelle del tipo.

## ADR-042 — IA dei nemici a classi di stato in `Core`
- **Data:** 2026-10-06
- **Contesto:** dalla M2 `EnemyAI` era uno `switch` con quattro stati. Con sciame e bruto ogni ramo sarebbe cresciuto con i casi dei tre nemici, e l'IA si provava solo in PlayMode.
- **Decisione:** gli stati (`Idle`, `Chase`, `Attack`, `WindUp`, `Recover`, `Dead`) sono classi di logica in `Core/Enemies`, con `Enter` e un `Tick` che restituisce lo stato successivo. Parlano con il nemico attraverso `IEnemyBody`: il bersaglio è vivo, è a portata, c'è un colpo in volo, lo si vede; chiedi il colpo, lascia il bersaglio. `EnemyBrain` tiene uno stato per tipo e avvisa con `StateChanged`. `EnemyAI` è un corpo sottile che implementa `IEnemyBody`. Scheletro, sciame e bruto sono combinazioni di stati e numeri in un ScriptableObject `EnemyArchetype`: aggro, occhi, percezione, corpo a terra, raggio del branco, evitamento, recupero, versi del critico. Il refactor è venuto prima dei nemici nuovi, con l'ordine delle decisioni identico a quello dello `switch`.
- **Alternative scartate:** lo `switch` con più rami; un albero di comportamento, troppo per sei stati; una macchina a stati nell'Animator, che lega la logica alla scena e non si prova in EditMode.
- **Conseguenze:** gli stati si provano in EditMode con un corpo finto. I test PlayMode dello scheletro sono passati senza toccarli. Un comportamento nuovo è uno stato nuovo, un nemico nuovo un archetipo. I layer che bloccano la vista restano sul componente, perché sono fisica della scena.

## ADR-043 — Sciame a gruppi, svegliato dal branco
- **Data:** 2026-10-06
- **Contesto:** l'archetipo dello sciame (piano § 2) è fatto di tanti nemici piccoli e veloci. Trovati a uno a uno non sono uno sciame, e sei agent nello stesso cunicolo largo una cella si spingono.
- **Decisione:** `Swarm` è lo scheletro con `Skeleton_Rogue` a scala 0,85: vita 10, velocità 4,8, raggio 0,35, colpo da 1–3 ogni 0,9 s. Arriva in gruppi interi da 4 a 6 attorno a un centro con abbastanza celle libere. `EnemyAI` avvisa con `Spotted` solo quando vede il cavaliere con i suoi occhi. `EnemyPack`, creato dal composition root a ogni livello e rilasciato all'uscita, sveglia i compagni dello stesso tipo entro 12 m con `EnemyBrain.Alert`, che vale solo da fermi. Chi è avvisato non avvisa a sua volta. La priorità di evitamento varia di ±15 attorno a 50, scelta dall'istanza.
- **Alternative scartate:** svegliare tutto il livello, che porta l'intera caverna addosso al cavaliere; avvisi a catena, con lo stesso risultato; un gestore del gruppo con un capo, che aggiunge un oggetto da tenere in vita per ogni gruppo.
- **Conseguenze:** si sveglia il gruppo, non la caverna. In un test sei dello sciame passano un cunicolo largo una cella e raggiungono il cavaliere in 6,5 s. Il raggio del branco è un numero dell'archetipo: lo scheletro ha 0 e resta solitario.

## ADR-044 — Colpo telegrafato con un settore a terra, e reazione del cavaliere ai colpi forti
- **Data:** 2026-10-06
- **Contesto:** il bruto deve insegnare a leggere un colpo e a spostarsi. Nel buio delle caverne l'animazione da sola non si legge. Il cavaliere finora non si fermava mai quando veniva colpito.
- **Decisione:** `WeaponDefinition` ha un arco in gradi, 360 per le armi di prima e 100 per l'ascia del bruto. `MeleeAttack` controlla portata e angolo al momento del danno e avvisa con `SwingEnded` quando un colpo finisce. Il bruto carica per 0,9 s fermo e girato: `WindUpState` aspetta la fine del colpo, `RecoverState` lo tiene fermo 0,6 s. Un archetipo con un tempo di recupero è telegrafato. `TelegraphSector` disegna a terra un ventaglio rosso con il raggio vero del colpo, che si riempie durante la carica, con materiali URP Unlit trasparenti. Il cavaliere ha `HitRecovery` con la soglia del 20% della vita massima: il colpo del bruto (20–28) la supera, scheletro e sciame no. Quando la supera, il fendente in corso si annulla e il cavaliere si ferma 0,4 s; un click durante il blocco parte alla fine. `MeleeAttack.Interrupt` azzera la velocità dell'agent.
- **Alternative scartate:** una decalcomania illuminata, che il buio e il post-processing fanno sparire; solo l'animazione rallentata; un colpo che segue il bersaglio durante la carica, che non si può schivare; fermare il cavaliere a ogni colpo, che con lo sciame lo bloccherebbe sempre.
- **Conseguenze:** chi esce dal settore durante la carica non prende il colpo, ed è dimostrato da un test. Un altro nemico telegrafato (il boss della M10) è un archetipo con il recupero e un'arma con il suo arco. Il danno del bruto è legato alla soglia, e un test lo controlla sui dati.

## ADR-045 — Colpi critici del cavaliere, con i versi del nemico
- **Data:** 2026-10-06
- **Contesto:** Mirco ha chiesto i colpi critici, con un verso diverso per ogni tipo di nemico colpito. `DamageInfo.IsCritical` esisteva dalla M2 e non era mai stato usato. I test usano tiri fissi a 0,0.
- **Decisione:** solo il cavaliere, solo sui colpi a segno. La probabilità è 5% + Destrezza / 10, al massimo 50%, e il danno è doppio. Il tiro va dopo quello del danno, e il critico esce dalla parte alta dell'intervallo (tiro ≥ 1 − probabilità): con i tiri fissi a 0,0 non esce mai. A schermo il numero è arancio, una volta e mezza più grande e con il punto esclamativo, e l'hit stop dura 0,12 s invece di 0,05. A orecchio ogni `EnemyArchetype` ha i suoi versi, scelti a caso senza ripetere il precedente, con un'intonazione per tipo: scheletro 1,1–1,2, sciame 1,4–1,6, bruto 0,6–0,7. Per ora sono ritagliati dalle tracce CC0 dell'ADR-039. Il pannello del personaggio ha la riga del critico con il suo tooltip.
- **Alternative scartate:** critico anche dei nemici, che nelle caverne con lo sciame diventa rumore; danno ×1,5, poco visibile con i numeri piccoli di oggi; critico dalla parte bassa del tiro, che avrebbe reso critico ogni colpo dei test; un affisso "+% critico", che sposterebbe il loot dei semi provati.
- **Conseguenze:** la Destrezza pesa anche sul danno. I test dei colpi già scritti sono passati senza cambiare un numero. Se i versi non convincono a orecchio, si cerca un pacchetto CC0 di versi di mostri, da scaricare con il permesso di Mirco. Un affisso sul critico arriverà con un test che fissi i drop del seme.

## ADR-046 — Limite di voci per i suoni dei personaggi
- **Data:** 2026-10-06
- **Contesto:** dieci colpi dello sciame nello stesso fotogramma suonavano come un rumore forte. Oltre il numero di voci reali, Unity toglieva sorgenti senza guardare quali.
- **Decisione:** la regola sta in `SfxBudget`, logica pura in `Core/Audio` senza allocazioni dopo la costruzione. Al più un suono per tipo (`SfxKind`) per fotogramma. Al più 12 voci insieme: oltre, un suono nuovo passa solo se è più vicino del più lontano in corso, e ne prende il posto. La priorità della sorgente va da 64 vicino al cavaliere a 255 lontano. `SfxLimiter`, in `Core.unity`, misura la distanza dall'`AudioListener` e conta il tempo reale, perché l'hit stop ferma il gioco ma non i suoni. Il composition root lo collega a `CharacterAudio` del cavaliere e dei nemici. Il verso del critico è un tipo a sé.
- **Alternative scartate:** solo il numero di voci reali del progetto, che taglia a caso; un mixer con ducking, che abbassa tutto invece di scegliere; un limite per nemico, che con dieci nemici lascia passare dieci impatti.
- **Conseguenze:** dieci impatti nello stesso fotogramma ne suonano uno. Musica, fondo e versi nel buio hanno sorgenti loro e restano fuori dal limite. Senza limite collegato, in una scena di prova, i suoni vanno come prima.

## ADR-047 — Nemici per profondità da una tabella a intervalli
- **Data:** 2026-10-06
- **Contesto:** scendendo nelle caverne il mix di nemici deve cambiare, e il bruto deve arrivare quando il cavaliere ha già trovato qualche oggetto. D10 proponeva gruppi con un peso.
- **Decisione:** una `SpawnTable` in `CaveSettings`, con una `SpawnRow` per profondità che vale dalla sua in giù. Ogni riga dà da quanti a quanti gruppi di scheletri e di sciame, e quanti bruti: al 5 due gruppi di scheletri, uno di sciame e un bruto; all'8 da uno a due gruppi di scheletri, tre di sciame e tre bruti. Il popolatore tira gli intervalli e mette prima lo sciame, poi i bruti, poi gli scheletri. Sciame e bruto hanno le loro `LootTable`: lo sciame lascia un oggetto il 20% delle volte e una pozione il 10%, il bruto sempre un oggetto e una pozione la metà delle volte. Il livello dell'oggetto è la profondità.
- **Alternative scartate:** pesi per tipo di gruppo, con cui un livello 5 poteva uscire senza bruto o con tre; numeri calcolati dalla profondità nel codice, da ricompilare a ogni ritocco; la tabella dello scheletro per lo sciame, che riempie il pavimento di oggetti.
- **Conseguenze:** su 200 semi le medie sono quelle di D10: al 5 circa 5 scheletri, 5 dello sciame e un bruto; all'8 circa 4 scheletri, 15 dello sciame e 3 bruti. I numeri si ritoccano nell'asset provando la build, e la M10 li ritara con le classi nuove. La cripta tiene il suo conto e i suoi semi.

## ADR-048 — Morire non ricarica il livello
- **Data:** 2026-10-07
- **Contesto:** dalla M6 *Ricomincia* ricaricava il livello dallo stesso seme, con nemici e casse rimessi e l'inventario com'era entrando (ADR-036). Provando la build della M7, Mirco ha chiesto il contrario: alla morte si tiene tutto quello che c'era prima.
- **Decisione:** `LevelManager.ReturnToEntrance` sostituisce `RestartLevel`. A schermo nero il cavaliere torna in vita a vita piena sull'ingresso da cui è entrato nel livello corrente, senza scaricare la scena. Restano inventario, pozioni bevute, casse aperte, oggetti a terra, nemici uccisi e mappa scoperta. I nemici vivi tornano fermi dove li aveva messi il livello (`EnemyAI.ReturnHome`, `EnemyBrain.Rest`) a vita piena (all'inizio tenevano le ferite; Mirco ha chiesto la vita piena l'8 ottobre). Il pulsante dice *Continua*.
- **Alternative scartate:** tornare in vita dove si è morti, con i nemici ancora addosso; tornare all'inizio della cripta; la ricarica della M6, che toglieva oggetti trovati e faceva rinascere i nemici uccisi.
- **Conseguenze:** morire costa solo la strada fino al punto di prima, e un livello si può finire morendo più volte. L'istantanea dell'inventario all'ingresso non serve più al gioco; `InventorySnapshot` resta per il salvataggio della M8. Le regole della morte del piano per la M8 ("cosa si perde") sono decise: niente.

## ADR-049 — Mira sui nemici: cosa c'è sotto il cursore, e un blocco che non ferma
- **Data:** 2026-10-07
- **Contesto:** nella prova della build M7, in mezzo allo sciame Mirco non riusciva a fare quasi niente e non capiva se lo fermavano i colpi o se non riusciva a selezionare il nemico. Erano tutte e due le cose: ogni blocco interrompeva il fendente per 0,45 s (ADR-026), i nemici piccoli e fitti erano difficili da cliccare, niente diceva quale era sotto il cursore, e gli agent dei nemici spingevano il cavaliere. Una colonna davanti a una cassa o a uno scheletro ne nascondeva il click.
- **Decisione:** il blocco annulla il danno ma non ferma più il cavaliere. `PlayerController` tiene il nemico vivo sotto il cursore e lo annuncia: l'HUD ne mostra nome e vita in alto al centro (`EnemyBar`), e un cerchio rosso a terra lo segna (`TargetMarker`). Un click sul pavimento a meno di 0,6 m da un nemico vivo colpisce lui, solo alla pressione. Il raggio del cursore attraversa gli ostacoli bassi (colonne, barili, muri bassi) quando dietro c'è un nemico o una cosa da usare; i muri alti, con il tag `Wall`, lo fermano. Il cavaliere ha la priorità di evitamento più alta di tutti gli agent.
- **Alternative scartate:** al più tre nemici dello sciame che attaccano insieme, proposto e non voluto da Mirco; un contorno sul modello del nemico, che litiga con il lampo bianco dei colpi (anche lui cambia i materiali); un raggio con uno spessore (SphereCast), che a ogni frame sceglierebbe nemici anche dove si vuole camminare.
- **Conseguenze:** lo scudo resta utile ma non è più una trappola nei gruppi. Il nome dei nemici è un dato dell'archetipo, che la città e il boss della M10 useranno. Il cerchio e la barra seguono un nemico solo: con più bersagli (gli incantesimi della M9) andranno ripensati.

## ADR-050 — Il nemico torna a casa oltre un limite, e guarisce
- **Data:** 2026-10-08
- **Contesto:** nella seconda prova della build M7 i nemici seguivano il cavaliere per tutto il livello. Mirco ha chiesto che, superata una distanza dal punto di partenza, tornino indietro.
- **Decisione:** uno stato nuovo, `ReturnState`. Solo l'inseguimento guarda la distanza in linea d'aria da casa: oltre `EnemyArchetype.LeashRange` (20 m; lo sciame 30, perché caccia in branco nelle caverne larghe) il nemico lascia il bersaglio e cammina fino al punto in cui l'ha messo il livello. Tornando non guarda e non risponde agli avvisi del branco; arrivato riprende la vita piena e torna fermo, pronto a rivedere il cavaliere. Un percorso che non arriva vale come arrivato. L'attacco ignora la distanza: un duello iniziato si finisce.
- **Alternative scartate:** perdere il cavaliere quando esce dalla vista, che nei corridoi della cripta succede di continuo e fa sembrare i nemici distratti; un limite di tempo dall'ultimo colpo; tornare senza guarire, che permette di colpire, scappare oltre il limite e ricominciare.
- **Conseguenze:** i nemici restano nella loro zona e un livello si può attraversare a pezzi. La casa è anche il punto a cui tornano quando il cavaliere muore (ADR-048): una sola idea. I campioni e gli unici del dopo v1.0 potranno avere un limite loro.

## ADR-051 — La velocità d'attacco è dell'arma, e l'animazione la segue
- **Data:** 2026-10-08
- **Contesto:** dalla M2 ogni arma ha intervallo e ritardo del colpo, ma erano tutti uguali (1 s e 0,6 s). Nella seconda prova della build M7 Mirco ha chiesto che un'arma piccola come il pugnale colpisca più in fretta.
- **Decisione:** ogni arma ha tempi suoi: pugnale 0,7 s, spada corta 1 s, lama dello scheletro 1,1 s, ascia 1,25 s, con il danno dell'ascia portato a 10–15 perché resti la più forte. Il ritardo del colpo è sempre il 60% dell'intervallo, dove la lama arriva nella clip del cavaliere; `CharacterAnimatorDriver` accelera o rallenta la clip di 0,6 / ritardo dell'arma, così l'animazione e il danno restano insieme. Il tooltip dice la velocità a parole: veloce, normale, lenta.
- **Alternative scartate:** una velocità in più nella definizione dell'arma accanto ai tempi, che avrebbe potuto contraddirli; clip diverse per arma, che KayKit non ha; la velocità dalla Destrezza, che è un'altra decisione (M8, se serve).
- **Conseguenze:** un'arma nuova sceglie solo il suo intervallo e tiene il ritardo al 60% (un test lo controlla). I nemici restano con le loro velocità fisse nell'animator (il bruto, ADR-044). Gli affissi di velocità d'attacco, se arriveranno, cambieranno l'intervallo dell'istanza e la stessa formula reggerà.

## ADR-052 — La visuale gira a scatti di 90°, e i muri alti la seguono
- **Data:** 2026-10-09
- **Contesto:** dalla M3 la camera guarda sempre a nord-est e i muri bassi stanno a sud e a ovest (ADR-017). Nella prova della build M7 Mirco ha chiesto di poter girare la visuale; la decisione D12 della M8 l'ha voluta a scatti.
- **Decisione:** Q ed E girano la camera di 90° attorno al cavaliere in 0,4 s. Il builder mette su ogni lato del livello sia il muro alto sia la balaustra, in due gruppi per lato; `WallView` accende quelli giusti a metà di ogni scatto. Il NavMesh si cuoce con tutti i muri alti, così non cambia girando. Una torcia sta sul muro della sua cella e ne ha una gemella sul muro di fronte della stessa stanza, accesa quando la prima finisce su un lato basso; se di fronte non c'è un muro della stanza, scende sulla balaustra. Lo stendardo della scala si vede solo quando il suo muro è alto. L'automappa gira con la camera; la luce del cavaliere e le orecchie la seguivano già.
- **Alternative scartate:** rotazione libera, che vuole muri che si dissolvono vicino al cavaliere (uno shader, M11); spostare i muri da un lato all'altro a ogni scatto, che rifà centinaia di oggetti a metà animazione; torce solo sui muri alti, che lasciano metà delle stanze al buio a ogni giro.
- **Conseguenze:** i livelli hanno il doppio dei pezzi di muro, ma metà sono spenti. Le scene costruite a mano vanno ricostruite dalle mappe per avere i lati doppi. La rotazione non si salva: ogni avvio parte da nord-est.

## ADR-053 — Il danno cresce con la Forza e con il livello, come in Diablo 1
- **Data:** 2026-10-09
- **Contesto:** dalla M4 il danno è il tiro dell'arma per 1 + Forza / 100. Nella prova della build M8 Mirco ha alzato di molto la Forza senza vedere il danno cambiare. I conti gli danno ragione: 30 punti in più danno circa il 20% sulla spada corta (da 8–12 a 10–14) e i nemici hanno vita fissa, quindi lo scheletro muore sempre in 3 colpi.
- **Decisione:** al danno di oggi si aggiunge Forza × livello / 100, il bonus del guerriero di Diablo 1. Il livello è sulla scheda del personaggio come statistica (`StatType.Level`), scritta dalla crescita insieme alla vita dei livelli: l'attacco lo legge come la Forza, e i nemici, che non ce l'hanno, restano com'erano. `CombatFormulas.Damage` è l'unica formula, usata dal colpo e dal pannello.
- **Alternative scartate:** raddoppiare il peso della Forza (1 + Forza / 50), che alza anche la partenza e costringe a rifare i danni delle armi, e non dà niente al livello; un bonus fisso per punto di Forza, con lo stesso problema alla partenza.
- **Conseguenze:** al livello 1 non cambia niente (+0,3). Con la spada corta, livello 7 e Forza 60 il danno medio passa da 12 a 16, livello 10 e Forza 75 da 13 a 21: lo scheletro muore in 2 colpi. Più avanti i nemici possono diventare facili; se le prove lo dicono, la vita dei nemici crescerà con la profondità (la zona ha già un livello, dalla M8).

## ADR-054 — Cinzel come font dell'interfaccia
- **Data:** 2026-10-09
- **Contesto:** dalla M3 tutti i testi sono in LiberationSans, il font di TextMesh Pro. Nella prova della build M8 Mirco ha chiesto un font più nello stile di Diablo.
- **Decisione:** Cinzel (SIL OFL 1.1, Google Fonts): maiuscole romane da iscrizione, con le minuscole disegnate come maiuscolette. È un asset TextMesh Pro statico con un atlante 1024 × 1024, i caratteri ASCII, le lettere accentate e i caratteri della tabella delle stringhe; LiberationSans resta come riserva per quelli che mancano (le frecce). È il font predefinito delle impostazioni di TextMesh Pro, quindi anche i testi creati dal codice lo usano. Le etichette piccole degli slot sono in maiuscolo pieno: a 18 punti la "g" maiuscoletta si leggeva come una "o".
- **Alternative scartate:** un font gotico (MedievalSharp, UnifrakturCook), poco leggibile nei numeri e nei tooltip; Exocet, il font di Diablo, che non ha una licenza libera.
- **Conseguenze:** Cinzel è più largo di LiberationSans: le colonne del pannello del personaggio sono scese a 24 punti e non vanno più a capo, come le etichette degli slot. Una lingua con un altro alfabeto vuole un font di riserva suo (piano § 5, "Lingue nuove"). La licenza sta accanto al font (ADR-004).

## ADR-055 — Anelli e amuleti nascono almeno magici
- **Data:** 2026-10-09
- **Contesto:** dalla M8 i gioielli non hanno caratteristiche di base, solo affissi (D6). Con la tabella delle rarità della M5 (normale 65%, magico 28%, raro 7%) un gioiello su due nasceva normale e non faceva niente. Mirco l'ha notato con l'amuleto nella prova della build.
- **Decisione:** ogni base ha una rarità minima, normale di default; anelli e amuleti l'hanno magica, come in Diablo 1. Il generatore tira la rarità come prima e la alza se è sotto il minimo: il seme e i tiri degli affissi non cambiano.
- **Alternative scartate:** dare ai gioielli una caratteristica di base (un po' di vita o di un attributo), che li rende tutti uguali a parità di livello; togliere la rarità normale dalla tabella per i gioielli con una tabella loro, più dati per lo stesso effetto.
- **Conseguenze:** i gioielli valgono sempre la pena di essere raccolti, e sono un po' più comuni tra i magici. Una base futura senza caratteristiche, come un talismano, userà la stessa regola. I salvataggi con un gioiello normale lo tengono com'era.

## ADR-056 — Armature e stivali dai pezzi dei personaggi KayKit
- **Data:** 2026-10-11
- **Contesto:** dalla M8 (8.4) le armature erano i busti di Quaternius e gli stivali due blocchi fatti a mano. Nella prova della build Mirco li ha trovati squadrati: busti schiacciati come scatole, stivali come mattoni. Gli altri oggetti (armi, scudi, elmi, guanti, gioielli, pozione) reggono.
- **Decisione:** i modelli vengono dai personaggi di KayKit Adventurers, lo stesso pacchetto CC0 del cavaliere, come gli elmi dell'8.4. Uno script temporaneo cuoce le mesh nella posa di riposo e le salva in `Art/Models/Generated/`: la corazza è il busto del Knight; la corazza nera è lo stesso busto con una copia della texture con l'acciaio brunito; l'armatura di cuoio è il busto del Rogue con una copia della texture con la tunica verde portata al marrone; gli stivali sono le gambe del Knight e del Rogue tagliate a due terzi dell'altezza, in coppia. I buchi rimasti aperti (collo, fianchi, bocca degli stivali) sono chiusi con un ventaglio di triangoli del colore più scuro della texture. I busti sono allungati del 20% in verticale, per leggersi come corazze in una cella 2 × 3. A terra armature e stivali stanno in piedi (`ItemDefinition.StandsOnGround`): distesi, come le armi, si vedevano vuoti come tubi.
- **Alternative scartate:** un pacchetto CC0 nuovo da scaricare, con uno stile diverso da quello del cavaliere; modellarli a mano, che è l'estensione Blender della M11; tenere le corazze di Quaternius con un'altra angolazione dell'icona, che resta squadrata.
- **Conseguenze:** armature, elmi e cavaliere hanno lo stesso stile. La corazza e la corazza nera hanno la stessa forma e cambiano colore, come le varianti di Diablo. I tre busti e gli stivali di Quaternius e il materiale `M_ItemLeather` sono usciti dal repo. Le texture KayKit ricolorate stanno accanto alle originali, nei crediti.

## ADR-057 — La probabilità di colpire contro ogni nemico
- **Data:** 2026-10-11
- **Contesto:** dalla M4 il pannello del personaggio mostra la probabilità di colpire contro l'Armatura dello scheletro (10), con la riga "Colpire (scheletro)". Dalla M7 ci sono anche lo sciame (Armatura 5) e il bruto (20), e il numero valeva per un nemico solo. Mirco ha chiesto di togliere "scheletro" e di trovare un modo migliore.
- **Decisione:** la riga si chiama "Colpire" e mostra l'intervallo su tutti i tipi di nemico, dal più corazzato al meno: per esempio 65–80% con la Destrezza di partenza. Il tooltip della riga, dopo la formula, elenca la probabilità contro ogni nemico con il suo nome nella lingua del gioco. Il pannello riceve un prefab per tipo di nemico e ne legge l'Armatura e il nome dell'archetipo: un nemico nuovo è un elemento in più della lista, senza codice.
- **Alternative scartate:** la precisione "pura" (75% + Destrezza / 2, prima dell'Armatura del nemico), come il "To hit" di Diablo 1, che può superare il 100% e non dice quanto si colpisce davvero; la probabilità contro il nemico sotto il cursore nella barra in alto, utile ma lontana dal pannello: resta un'idea per la barra della M9.
- **Conseguenze:** con i nemici speciali o un'Armatura che cresce con la profondità (piano § 5, dopo la v1.0) l'intervallo si allarga da sé. Se i tipi diventano molti, il tooltip li raggrupperà per livello.

## ADR-058 — Meno oggetti e meno pozioni dai nemici
- **Data:** 2026-10-11
- **Contesto:** nella prova della build M8 Mirco ha trovato troppe armi e troppe pozioni. Con le tabelle della M5 e della M7 la media era di 4,5 oggetti e 1,8 pozioni alla cripta 1, 10,7 e 4,2 alla cripta 4, 10,6 e 4,9 alle caverne 8. Le armi pesavano più delle altre basi (la lama 3, spada e pugnale 2), e con gli scudi erano più di metà dei drop.
- **Decisione:** scheletro dal 70% al 30% di oggetti e dal 25% al 10% di pozioni; sciame dal 20% al 7% e dal 10% al 3%; bruto dal 100% al 60% e dal 50% al 20%; le casse lasciano sempre un oggetto, ma la pozione scende dal 50% al 25%. Tutte le basi hanno peso 1: le quattro armi sono circa 4 oggetti su 19. Il numero delle casse non cambia, perché cambierebbe i livelli generati di ogni seme.
- **Alternative scartate:** meno nemici per livello, che toglie combattimento invece di loot; meno casse, che sposta tutti i semi provati nei test di generazione.
- **Conseguenze:** in media 2,5 oggetti e 0,8 pozioni alla cripta 1, 6,3 e 1,9 alla cripta 4, 6,0 e 1,9 alle caverne 8, circa la metà degli oggetti e meno della metà delle pozioni; le armi scendono da 3,6 a 1,3 per livello alla cripta 4. Le pozioni diventano una risorsa da tenere da conto. I test che fissano un drop cercano il seme da soli (trappola 10 della M7); con la pozione più rara il primo seme buono la faceva cadere insieme all'oggetto, e ora la ricerca vuole un seme senza pozione. Cambiano anche i test sulle frequenze.

## ADR-059 — Esperienza e livelli: curva in un asset, logica in Core
- **Data:** 2026-10-08 (scritto alla chiusura della M8, l'11 ott)
- **Contesto:** la M8 porta l'esperienza e i livelli (D1–D3 della scheda). La curva e i valori dei nemici andavano tarati provando, senza toccare il codice.
- **Decisione:** livello massimo 20; per passare dal livello L al successivo servono 100 × L^1,6 punti, arrotondati a 10. Ogni nemico ha l'esperienza nel suo archetipo (scheletro 70, sciame 30, bruto 230), moltiplicata per 1 + 0,15 × (profondità − 1) e divisa per 1 + 0,1 × (livello del cavaliere − livello della zona) quando il cavaliere è più forte della zona; il livello della zona è la profondità × 1,25. A ogni livello 5 punti attributo, 2 di vita in più e la vita piena. I numeri stanno in `ProgressionSettings`, un asset; la logica in `CharacterProgress`, in `Core`, che scrive livello e vita dei livelli come valori base della scheda delle statistiche.
- **Alternative scartate:** i valori proposti nella scheda (12, 5 e 40), che una simulazione della discesa ha mostrato troppo bassi (livello 4 in fondo alle caverne invece di 9); la curva di Diablo 1 fino al 50, troppo lunga per otto profondità.
- **Conseguenze:** si sale circa di un livello per profondità e si arriva al 9 all'ottava. Il livello sulla scheda serve anche al danno (ADR-053). Una classe nuova o una difficoltà cambiano l'asset, non il codice.

## ADR-060 — Dieci slot, due anelli con due valori dell'enum
- **Data:** 2026-10-08 (scritto alla chiusura della M8, l'11 ott)
- **Contesto:** l'equipaggiamento completo (piano v2.19) aggiunge elmo, armatura, guanti, stivali, amuleto e due anelli ad arma e scudo. `Equipment` aveva un posto per valore di `EquipSlot`, e due anelli vogliono due posti con la stessa regola.
- **Decisione:** `EquipSlot` cresce di valori in fondo all'enum, così i numeri salvati di quelli vecchi non cambiano, e il secondo anello è un valore suo, `Ring2`. Nessuna base chiede `Ring2`: un anello dice `Ring` ed è `Equipment` a scegliere il posto (il primo libero, o lo slot cliccato). Pezzi d'armatura con Armatura e Forza richiesta (`ArmorDefinition` con uno slot); anelli e amuleti senza niente di base, solo affissi (`JewelryDefinition`). L'inventario è una sagoma come in Diablo.
- **Alternative scartate:** uno slot "anello" con un indice, che cambiava la chiave dell'equipaggiamento e il formato dell'istantanea per un caso solo.
- **Conseguenze:** l'istantanea salva lo slot di ogni oggetto indossato, perché con due anelli conta quale. L'armatura indossata non cambia il modello del cavaliere (piano § 5, M11). I gioielli nascono almeno magici (ADR-055).

## ADR-061 — Un salvataggio JSON, scritto per sostituzione, senza lo stato dei livelli
- **Data:** 2026-10-08 (scritto alla chiusura della M8, l'11 ott)
- **Contesto:** la M8 porta il salvataggio (D7, D8, D10 della scheda). Non c'è ancora il menu (M10), e un crash a metà scrittura non deve perdere la partita.
- **Decisione:** un solo file, `save.json` in `Application.persistentDataPath`, con `JsonUtility`. Si scrive in `save.json.tmp` e lo si mette al posto del vecchio con `File.Replace`. Si salva entrando in un livello, ogni 60 s se da 5 s non ci sono colpi, e chiudendo il gioco; da morto si salva a vita piena, come dopo *Continua*. Dentro ci sono seme (in testo), scena, ingresso, profondità, crescita, attributi base, vita, inventario completo e mappe scoperte; non lo stato dei livelli, che il seme rigenera. All'avvio si riprende dal salvataggio; `-newgame` e `-seed` lo ignorano. Nell'editor è spento: i test lo accendono con un file loro.
- **Alternative scartate:** `PlayerPrefs`, che non regge un inventario e su Windows finisce nel registro; salvare nemici uccisi, casse aperte e oggetti a terra, che vorrebbe un'identità stabile per ognuno e un formato molto più grande, per livelli che si attraversano in pochi minuti; più slot di salvataggio prima del menu.
- **Conseguenze:** tornando in un livello, nemici e casse tornano come li genera il seme. Il menu della M10 avrà *Continua* e *Nuova partita* sullo stesso file.
- **Aggiornamento (passo 10.9 della M10):** i livelli lasciati ora restano com'erano, anche nel salvataggio (ADR-079); i tre posti del menu sono nell'ADR-073.

## ADR-062 — Formato versionato dal primo salvataggio, migrazione un passo alla volta
- **Data:** 2026-10-08 (scritto alla chiusura della M8, l'11 ott)
- **Contesto:** un salvataggio scritto da una versione del gioco deve caricarsi con le successive. La M8 ne ha dato un caso vero: il formato 2 aggiunge le mappe scoperte (D9).
- **Decisione:** `SaveData` ha un campo `_version` fin dal formato 1. `SaveMigrator` porta un formato vecchio al corrente un passo alla volta (1 → 2: mappe vuote), dentro `SaveFile.TryRead`, così il resto del gioco vede solo il formato corrente. Un formato più nuovo del gioco non si carica e lo dice. Il test della migrazione carica un salvataggio vero del formato 1, scritto dal codice di allora e congelato in `Tests/EditMode/Fixtures/save_v1.json`.
- **Alternative scartate:** campi facoltativi senza versione, che funzionano finché un campo non cambia significato; un file di prova scritto a mano, che somiglia al formato di oggi più di quello vero.
- **Conseguenze:** ogni formato nuovo è un passo del migratore e un file congelato in più. La M9 ne avrà uno (incantesimi conosciuti e mana).

## ADR-063 — Lo zoom cambia la dimensione della vista ortografica
- **Data:** 2026-10-09 (scritto alla chiusura della M8, l'11 ott)
- **Contesto:** lo zoom con la rotella (D11 della scheda, piano v2.18). La camera è ortografica: avvicinarla non ingrandisce niente.
- **Decisione:** lo zoom cambia la dimensione della vista di Cinemachine (9 di base), a scatti interi del 10% tra il 60% e il 140%, in 0,15 s con un'uscita cubica e con il tempo non scalato. La logica sta in `ZoomLevel`, in `Core`; `CameraZoom` la applica e ricorda lo scatto tra le preferenze, come la lingua. Il reader manda solo il verso della rotella.
- **Alternative scartate:** spostare la camera, che in ortografica non ha effetto; uno zoom continuo, che accumula somme di 0,1 lontane dai valori tondi.
- **Conseguenze:** luce del cavaliere, muri bassi e automappa non cambiano con lo zoom. Il limite in allontanamento tiene la vista dentro il raggio della luce.

## ADR-064 — Incantesimi come asset, logica in Core, effetti in ascolto
- **Data:** 2026-10-11 (scritto alla chiusura della M9)
- **Contesto:** la M9 porta il mana e tre incantesimi di tipo diverso (D2, D3, D5 della scheda): un proiettile, un'area e un potenziamento. Il piano chiede un sistema di abilità estendibile.
- **Decisione:** ogni incantesimo è un `SpellDefinition` in `Data/Spells`, con id stabile, tipo, costo, ricarica, danno (tiro più Magia / divisore) e i numeri del suo tipo. In `Core/Magic` stanno `ManaPool`, `SpellBook` (noti e scelto, ripristinabile dagli id) e `SpellCaster`, che decide se un lancio parte e dice perché no (`CastResult`). `PlayerMagic` traduce il tasto destro in un lancio e annuncia `Cast`; che cosa succede nel mondo lo decide `PlayerSpells`, che ascolta e sceglie per tipo: proiettile dal pool, area con un `OverlapSphereNonAlloc`, potenziamento come modificatore dell'Armatura con l'incantesimo come sorgente.
- **Alternative scartate:** un `MonoBehaviour` per incantesimo, che sparge regole di costo e ricarica in tre posti; incantesimi con livelli, che il gioco non chiede (il danno cresce con la Magia).
- **Conseguenze:** un incantesimo nuovo di un tipo esistente è un asset; un tipo nuovo è un ramo in `PlayerSpells`. Il danno degli incantesimi non usa la Forza e non si blocca, e ha un flusso di numeri casuali suo (trappola 3).

## ADR-065 — Proiettili da un pool, con un raggio a sfera e i muri dal NavMesh
- **Data:** 2026-10-11 (scritto alla chiusura della M9)
- **Contesto:** il Dardo di fuoco e i dardi dei maghi (D7). Lanciando a raffica non si deve allocare a ogni frame; i muri alti dalla parte della camera sono spenti con i loro collider dalla M8.
- **Decisione:** `ProjectilePools` nella scena Core tiene un `UnityEngine.Pool.ObjectPool<Projectile>` per prefab, al massimo 32, con la funzione di ritorno creata una volta. Il proiettile vola dritto senza rigidbody: a ogni frame un `SphereCastNonAlloc` con un buffer condiviso cerca il primo bersaglio vivo, e un `NavMesh.Raycast` a terra dice se ha incontrato un muro. Ripreso dal pool azzera scia, nucleo, scoppio e luce. I dardi dei nemici passano da `ShieldBlock` come i colpi di mischia.
- **Alternative scartate:** rigidbody e collisioni, che a 14 m/s perdono i contatti e vogliono i collider dei muri accesi; `Instantiate` e `Destroy` a ogni lancio; un pool scritto a mano, quando Unity ne ha uno.
- **Conseguenze:** lanciando un Dardo ogni due frame con 99 proiettili in volo il thread principale alloca 0 byte (9.9). Un muro che non è nel NavMesh non ferma i proiettili.

## ADR-066 — Il nemico a distanza: due stati nuovi e un corpo che sa tirare
- **Data:** 2026-10-11 (scritto alla chiusura della M9)
- **Contesto:** lo scheletro mago (D6) tiene la distanza, lancia e arretra. L'IA dei nemici è una macchina a stati in `Core` con un corpo astratto (ADR-042).
- **Decisione:** `IRangedBody` estende `IEnemyBody` con distanza dal bersaglio, lancio, tiro libero, avvicinamento, attesa e fuga. Due stati nuovi in fondo all'enum: `RangedState` (si avvicina fino alla portata, si ferma e lancia se il tiro è libero, arretra sotto i 4 m dopo una pausa) e `RetreatState` (fugge verso un punto campionato sul NavMesh, con un limite di tempo). `EnemyArchetype` sceglie gli stati a distanza quando ha una portata; `EnemyBrain.Alert` entra in `Ranged` invece di `Chase`. `EnemyAI` implementa la fuga provando angoli di 0°, ±30°, ±60° e ±90° dalla direzione opposta al cavaliere, e il tiro libero con un `NavMesh.Raycast`.
- **Alternative scartate:** un componente di IA separato per i maghi, che duplicherebbe percezione, ritorno a casa e morte; la fuga in linea retta, che finisce negli angoli (trappola 5).
- **Conseguenze:** il comportamento si prova in EditMode con il corpo finto (`MageStateTests`). Un altro nemico a distanza (la balestra) è un archetipo e un prefab.

## ADR-067 — Una pergamena per livello, tirata una volta e portata da un nemico o da una cassa
- **Data:** 2026-10-11 (scritto alla chiusura della M9)
- **Contesto:** gli incantesimi oltre al primo si imparano dalle pergamene (D5), circa una ogni tre livelli dalla profondità 2, mai di un incantesimo già noto. Il loot è deterministico dal seme (ADR-029, ADR-037) e un tiro nuovo sposta quelli dopo (trappola 10 della M7).
- **Decisione:** `ScrollTable` tira una volta per livello, collegando il livello, con un flusso suo (`SeedMixer.ForScroll`): se esce una pergamena, sceglie tra quelle volute (incantesimi non noti) e un portatore tra nemici e casse, che la lascia accanto al suo drop (`Carry`). La pozione di mana divide il tiro della pozione di cura (`LootTable.PotionFor`) invece di aggiungerne uno.
- **Alternative scartate:** pergamene nelle tabelle di ogni nemico, che renderebbero la frequenza dipendente dal numero di nemici e cambierebbero i drop dei semi provati; un tiro alla morte di ogni nemico, che potrebbe dare due pergamene uguali nello stesso livello.
- **Conseguenze:** la frequenza è per livello, non per nemico. Il libro va rimesso dal salvataggio prima che il livello si colleghi (trappola 8, ADR-068).

## ADR-068 — Formato 3: incantesimi e mana nel salvataggio
- **Data:** 2026-10-11 (scritto alla chiusura della M9)
- **Contesto:** con la M9 la partita ha incantesimi imparati, uno scelto e il mana (D11). ADR-062 vuole un passo del migratore e un file vero congelato per ogni formato.
- **Decisione:** `SaveData` aggiunge gli id degli incantesimi noti, il posto scelto e il mana; −1 vuol dire «pieno», e da morto si salva pieno come la vita. `SaveMigrator` porta il 2 al 3 con nessun incantesimo imparato, il primo posto e il mana pieno, mettendo i valori a mano: `JsonUtility` lascia ai campi mancanti nel file il default del tipo, non quello dell'inizializzatore. Prima di cambiare il formato si è congelato `Tests/EditMode/Fixtures/save_v2.json`, scritto dal codice del formato 2. All'avvio il salvataggio si applica nello `Start` del CompositionRoot, prima che il livello caricato in modo asincrono tiri la sua pergamena.
- **Alternative scartate:** salvare i nomi degli incantesimi invece degli id, che cambiano con la lingua o con un rinomina; ricalcolare il mana pieno a ogni caricamento, che regala mana a chi salva a secco.
- **Conseguenze:** un file del formato 1 passa dal 2 e poi dal 3. Il menu della M10 con tre salvataggi non cambia il formato: il riepilogo si legge dal file.

## ADR-069 — La barra in alto resta sull'ultimo nemico colpito
- **Data:** 2026-10-11 (scritto alla chiusura della M9)
- **Contesto:** dalla M7 la barra in alto mostra solo il nemico sotto il cursore (ADR-049). Con i proiettili e l'Anello di fuoco si colpiscono nemici che il cursore non ha mai toccato (D9, piano v2.22).
- **Decisione:** `EnemyBar` segue i nemici del livello (`Track`/`Untrack`, chiamati dal CompositionRoot accanto ai numeri di danno) e ricorda l'ultimo colpito o mancato dal cavaliere per 3 s. Mostra il nemico sotto il cursore se c'è, altrimenti l'ultimo colpito. Il cerchio rosso a terra resta solo sul nemico sotto il cursore. Aggiorna ADR-049.
- **Alternative scartate:** una barra sopra ogni nemico, che nello sciame fa dieci barre una sull'altra (resta un'opzione per dopo la v1.0); tenere l'ultimo colpito senza scadenza, che lascia sulla barra un nemico già lontano.
- **Conseguenze:** colpi ed evasioni del cavaliere aggiornano la barra con un evento, senza controlli a ogni frame; il tempo di 3 s è un `WaitForSeconds` creato una volta.

## ADR-070 — Eccezione all'ADR-004: i suoni gratuiti di puadlung, senza ridistribuzione da soli
- **Data:** 2026-10-12
- **Contesto:** l'oro della M10 non aveva un suono di raccolta, e i pacchetti CC0 già usati non ne hanno uno. Mirco ha trovato *Dark Fantasy Audio – Free* (Lite Audio Pack) di puadlung, con monete, cassa, pozione, ossa e un ruggito per il boss. La licenza della pagina: uso gratuito anche commerciale, senza diritti né credito obbligatorio, ma «Do NOT resell or redistribute these audio files as standalone assets». Non è CC0, e il repo pubblico rende scaricabili i singoli file: una zona grigia rispetto al divieto.
- **Decisione:** Mirco ha scelto di usarli con un'eccezione documentata (12 ott 2026). Entrano solo i file che il gioco usa, nella cartella `Assets/_Project/Audio/SFX/Puadlung/`, con una riga in `CREDITS.md` che riporta la licenza e il link alla pagina. Oggi sono quattro: monete alla raccolta dell'oro, sorso di pozione (di cura e di mana), apertura della cassa, colpo sulle ossa degli scheletri. Il ruggito entrerà con il boss. Il pacchetto intero non entra nel repo.
- **Alternative scartate:** chiedere prima il permesso all'autore, che toglieva la zona grigia ma rimandava i suoni; restare sul CC0 (Kenney RPG Audio per le monete, un ruggito sintetizzato), più sicuro ma con suoni meno adatti.
- **Aggiornamento (passo 10.7):** con il boss entrano il ruggito, come previsto, e la porta pesante, per le colonne che chiudono la sua sala: sei file in tutto.
- **Conseguenze:** se l'autore chiedesse di toglierli, si sostituiscono i quattro file e il loro riferimento nei prefab, senza toccare il codice. Ogni altro asset con una licenza diversa da CC0 o OFL vuole un ADR come questo.

## ADR-071 — L'oro in Core, a terra come un oggetto e fuori dalla borsa; prezzi calcolati
- **Data:** 2026-10-12 (scritto durante la chiusura della M10)
- **Contesto:** la M10 porta il mercante, e con lui l'oro e i prezzi (D4 della scheda). Scrivere un prezzo per ogni combinazione di base, rarità e affissi non regge.
- **Decisione:** `GoldDefinition` è una definizione fuori dal database degli oggetti, con tre modelli per la quantità. Ogni `LootTable` ha probabilità, intervallo e crescita con la profondità; `LootRoller.RollGold` tira con un seme suo (`SeedMixer.ForGold`), così il loot dei semi già provati non cambia. A terra la pila è un `GroundItem` con la quantità e si raccoglie nel portafoglio di `Inventory`, senza occupare la borsa. Il prezzo è valore base × rarità (1, 3, 6) × (1 + livello / 10) (`ItemPricing`); si vende a un quarto. L'oro delle vendite non conta tra l'oro raccolto della vittoria (`ReceiveGold` invece di `AddGold`).
- **Alternative scartate:** l'oro come oggetto nella borsa, che in Diablo 1 occupa celle e qui avrebbe solo riempito la griglia; un prezzo scritto in ogni definizione e per ogni affisso.
- **Conseguenze:** ogni flusso nuovo del loot (pergamena del ritorno, assortimento del mercante) ha il suo dominio del seme. Quantità e prezzi si tarano dalle prove in build senza toccare il codice.

## ADR-072 — Formato 4: oro, città, portale e contatori della partita
- **Data:** 2026-10-12 (scritto durante la chiusura della M10)
- **Contesto:** la M10 aggiunge alla partita l'oro, la città, il portale del ritorno e la vittoria con le sue statistiche (D11). L'ADR-062 vuole un passo del migratore e un file vero congelato per ogni formato.
- **Decisione:** `SaveData` aggiunge l'oro, se si è in città, la profondità a cui riporta il portale e i contatori di `RunStats` (tempo, uccisioni, morti, oro raccolto, boss sconfitto). Il migratore porta il 3 al 4 con tutto a zero, mettendo i valori a mano. Prima del cambio si è congelato `save_v3.json`, scritto dal codice del formato 3.
- **Alternative scartate:** i contatori in un file a parte, che si sarebbe perso o disallineato dal salvataggio della partita.
- **Conseguenze:** la vittoria sta nel salvataggio appena il boss muore: tornando alla profondità 8 il re non rinasce. Il formato 5 (ADR-079) è arrivato con le correzioni del 10.9.

## ADR-073 — Il menu nella scena Core, tre posti e la pausa che ferma il tempo
- **Data:** 2026-10-12 (scritto durante la chiusura della M10)
- **Contesto:** fino alla M9 il gioco partiva dentro la partita, con un solo `save.json` (ADR-061). La M10 vuole un menu con *Continua*, *Nuova partita* e tre salvataggi, una pausa e le opzioni (D2, D10). L'ADR-014 aveva rimandato una scena `Bootstrap`.
- **Decisione:** il menu sta in una tela della scena Core, sopra l'HUD, non in una scena sua: così il posto scelto non deve passare tra due scene senza un singleton. Se serve il menu, il CompositionRoot ferma il primo caricamento (`LevelManager.HoldStart`) e, scelta la partita, rimette il salvataggio sul cavaliere e chiama `Begin`. Nelle build il menu c'è sempre; nell'editor solo con `DARKDESCENT_MENU=1`; `-newgame` e `-seed` lo saltano e giocano senza salvare. `SaveSlots` gestisce `save_1.json`…`save_3.json`, legge il riepilogo dal salvataggio stesso e trasforma il vecchio `save.json` nel posto 1. La pausa (`Esc`) mette `Time.timeScale` a 0 e la UI usa il tempo non scalato; *Salva ed esci* ricarica Core. Le opzioni (lingua al posto di F9, volumi del mixer, nomi a terra sempre visibili) stanno nelle preferenze.
- **Alternative scartate:** una scena `Bootstrap` con il menu, che vuole uno stato condiviso tra scene; tre partite in un solo file, che rende rischioso cancellarne una.
- **Conseguenze:** l'ADR-014 resta senza la scena `Bootstrap`. Ogni sistema che si muove nel tempo deve sapere se va in pausa: un hit stop in corso si annulla aprendo la pausa, altrimenti alla sua fine rimetterebbe il tempo che scorre.

## ADR-074 — La città come mappa di testo costruita a runtime, alla profondità 0
- **Data:** 2026-10-12 (scritto durante la chiusura della M10)
- **Contesto:** la città (D3) è un accampamento sotterraneo fatto a mano. I livelli fatti a mano della M3 sono scene salvate dall'editor con `LevelMapBuilder`, senza automappa.
- **Decisione:** la città è un testo (`Data/Levels/Town.txt`) che un `MapLevel` costruisce entrando, con lo stesso `LevelBuilder` della cripta, e di cui cuoce il NavMesh. `MapLevel` e `DungeonLevel` hanno una base comune, `BuiltLevel`, che il `LevelManager` cerca nella scena. La profondità 0 è la città (`LevelContext.IsTown`): niente numero nel titolo e `inTown` nel salvataggio. Ogni partita nuova comincia lì.
- **Alternative scartate:** una scena salvata come quelle della M3, che non ha l'automappa e si ritocca solo nell'editor; una sola schermata del mercante, la linea di taglio 3 del piano.
- **Conseguenze:** ritoccare la città vuol dire cambiare il testo. La luce ambiente si legge dalla scena salvata, quindi cambiandola nel tileset va ricostruita la scena.

## ADR-075 — L'assortimento del mercante dal seme, senza salvarlo
- **Data:** 2026-10-12 (scritto durante la chiusura della M10)
- **Contesto:** il mercante (D5) vende otto oggetti del livello della profondità più bassa raggiunta, che cambiano a ogni ritorno in città. Salvarli vorrebbe un campo nuovo nel salvataggio.
- **Decisione:** `LootRoller.RollStock` tira l'assortimento da un flusso suo (`SeedMixer.ForShop`) mescolato con la profondità più bassa raggiunta (dalle mappe esplorate, già salvate) e con il numero di uccisioni (da `RunStats`, già salvato). Le provviste (pozioni e pergamena del ritorno) ci sono sempre.
- **Alternative scartate:** salvare l'assortimento; cambiarlo a ogni ingresso in città, anche senza combattere, che lo farebbe ritirare uscendo e rientrando.
- **Conseguenze:** l'assortimento cambia solo dopo aver ucciso qualcosa. Un oggetto comprato ricompare se si esce e si rientra senza uccidere: lo si ricompra, non si duplica niente.

## ADR-076 — La pergamena del ritorno e i portali, con il ritorno dove si era aperto
- **Data:** 2026-10-12 (scritto durante la chiusura della M10)
- **Contesto:** dal dungeon si deve poter tornare in città e riscendere (D6), come con il *Town Portal* di Diablo 1. La scheda proponeva il ritorno all'ingresso della profondità; nella prova della build è sembrato un errore.
- **Decisione:** `ReturnScrollDefinition` si legge con il click destro e chiede a un `IPortalOpener` (`TownPortals`, in Core) di aprire un portale davanti al cavaliere, uno solo per volta. Il portale porta all'ingresso `Portal` della città; lì un portale riporta alla profondità di partenza e si chiude dopo l'uso. Dal 10.9 riporta nel punto in cui si era aperto: `TownPortals.PortalSpot` lo ricorda e `LevelManager.LoadLevelAt` ci porta il cavaliere, sul NavMesh del livello rigenerato. L'ingresso del livello resta quello di sempre, per *Continua* e per la ripartenza dopo la morte. Profondità e punto vanno nel salvataggio (formati 4 e 5). Il portale della vittoria è un secondo portale, tenuto a parte.
- **Alternative scartate:** scale in su, che vorrebbero rifare otto livelli a ritroso; un portale fisso a ogni livello, che toglie la scelta di quando tornare; ripartire dal punto del portale dopo la morte, che alla profondità 8 può far rinascere dentro la sala del boss.
- **Conseguenze:** il ritorno nel punto ha senso solo perché il livello torna com'era (ADR-079).
- **Aggiornamento (passo 11.7 della M11):** la pergamena non c'è più: il portale lo apre una magia, con il tasto T, chiusa nella sala del re vivo (ADR-086). Portali, ritorno nel punto e salvataggio restano questi.

## ADR-077 — La sala del boss scavata dal generatore, il boss a fasi dai pezzi che c'erano
- **Data:** 2026-10-12 (scritto durante la chiusura della M10)
- **Contesto:** il boss (D8) sta alla profondità 8, in una sala che si chiude entrando. Le caverne sono generate dal seme.
- **Decisione:** alla `CaveSettings.BossDepth` il `CaveGenerator` scava una sala di 5 × 5 celle nella zona aperta più lontana dall'ingresso, la chiude con un anello di roccia e lascia una porta sola, la più vicina all'ingresso tra quelle che tengono la sala raggiungibile; quello che l'anello isola torna roccia. Il popolatore mette il re al centro e la porta (`BossGate`, colonne con un `NavMeshObstacle` che taglia il NavMesh solo da chiusa) e non mette altro dentro. `BossController` comanda lo scontro sopra l'`EnemyAI` di sempre: fasi per vita in `Core` (`BossPhases`), evocazioni dello sciame, anello di fuoco telegrafato da un cerchio. Gli evocati non danno esperienza né loot. La `LootTable` ha una rarità minima.
- **Alternative scartate:** una sala fatta a mano e incollata nella caverna, che vuole un innesto tra mappa di testo e generatore; una macchina a stati del boss separata dall'IA dei nemici.
- **Conseguenze:** gli altri livelli non cambiano. Morto il cavaliere, lo scontro ricomincia da capo: porta aperta, evocati spariti, re a vita piena.

## ADR-078 — I riconoscimenti da una copia di CREDITS.md tra gli asset
- **Data:** 2026-10-12 (scritto durante la chiusura della M10)
- **Contesto:** dopo la vittoria scorrono i riconoscimenti (D9), che devono dire le stesse cose di `CREDITS.md`. Il file sta nella radice del repo e non entra nella build.
- **Decisione:** il gioco legge `Data/Credits.txt`, una copia aggiornata dal menu *DarkDescent → Aggiorna i riconoscimenti*; un test EditMode fallisce se la copia è diversa dall'originale. `CreditsTable` legge la tabella, toglie link e markdown e salta le righe degli asset fatti per il progetto.
- **Alternative scartate:** un testo scritto a mano per la schermata, che ci si dimentica di aggiornare; spostare `CREDITS.md` sotto `Assets/`, lontano da dove chi legge il repo lo cerca.
- **Conseguenze:** cambiando `CREDITS.md` va rifatta la copia, e il test lo ricorda.

## ADR-079 — I livelli lasciati restano com'erano: formato 5
- **Data:** 2026-10-12 (passo 10.9, decisione di Mirco)
- **Contesto:** dall'ADR-061 un livello, rientrandoci, si rigenerava nuovo dal seme. Con la città e i portali della M10 si torna spesso nella stessa profondità, e nella prova della build i nemici rinascevano e le casse ridavano gli stessi oggetti: si poteva fare l'oro all'infinito. Mirco ha scelto di ricordare tutto, come Diablo 1, anche nel salvataggio.
- **Decisione:** `LevelRecorder` (sul LevelManager di Core), prima di scaricare un livello, ne scrive un `LevelRecord`: nemici uccisi e casse aperte per posto nell'ordine in cui il livello li costruisce, oggetti e oro a terra con il punto (`GroundItemRecord`), anche quelli messi dalla mappa. Rientrando, il livello si rigenera dal seme e `Restore` lo rimette com'era prima che il CompositionRoot colleghi i nemici: gli uccisi si spengono subito e si distruggono a fine frame, le casse aperte restano aperte e vuote, gli oggetti tornano a terra. I ricordi sono per scena e profondità (`LevelMemory`) e dicono anche quanti nemici e casse c'erano: se il livello generato non corrisponde, nemici e casse ripartono nuovi. Il formato 5 aggiunge i ricordi e il punto del portale (ADR-076); un salvataggio del formato 4 si migra senza ricordi, e `save_v4.json` è congelato.
- **Alternative scartate:** ricordare solo finché il gioco è aperto, che lascia rifarmare le casse chiudendo e riaprendo; ricordare nemici e casse ma non gli oggetti a terra, che li perde uscendo dal livello; un'identità stabile salvata in ogni nemico, superflua finché il seme dà lo stesso livello e i posti nell'ordine di costruzione restano gli stessi.
- **Conseguenze:** aggiorna l'ADR-061. Un livello appena caricato non si ricorda finché non è stato rimesso, perché il salvataggio fatto entrando non sovrascriva il ricordo vero con il livello nuovo del seme. I nemici feriti tornano a vita piena. Una punizione alla morte che faccia rinascere parte dei nemici (piano, «Dopo la v1.0») partirebbe da questi ricordi.

## ADR-080 — Il post-processing per livello dal tileset, senza un Volume globale in Core
- **Data:** 2026-10-17 (passo 11.1, scritto durante la chiusura della M11)
- **Contesto:** la D3 della M11 chiedeva un Volume globale in `Core` con un profilo nostro, più un profilo per il colore in ogni `LevelTileset`. Al passo 11.1 si è visto che ogni livello aveva già un Volume, creato dal `LevelBuilder` con il profilo del tileset dalla M6, e che cripta e caverne lo condividevano.
- **Decisione:** nessun Volume in `Core`. Ogni tileset porta un profilo completo: tonemapping ACES, bloom con soglia 0,9 (solo fuochi, candele, portali), vignetta, grana leggera e un colore suo (cripta fredda e spenta, caverne verdastre con un profilo loro, accampamento caldo). Entrando in un livello cambia tutto insieme alla luce ambiente, che sta già nel tileset.
- **Alternative scartate:** un Volume globale più uno locale per il colore, che vuole priorità e pesi da tenere allineati per avere lo stesso risultato; un profilo solo per tutto il gioco, che non distingue i luoghi.
- **Conseguenze:** un livello nuovo ha l'aspetto giusto con il suo tileset e basta. Un effetto da cambiare ovunque va cambiato nei tre profili; un test controlla che tutti abbiano gli stessi effetti di base.

## ADR-081 — Il filtro retrò in HLSL a tutto schermo, con una palette e il dithering legati allo schermo, spento di partenza
- **Data:** 2026-10-17 (passi 11.2 e 11.7, scritto durante la chiusura della M11)
- **Contesto:** gli asset di KayKit, Quaternius e delle caverne vengono da pacchetti diversi, e il piano chiedeva una pelle comune (D2): una palette limitata con il dithering ordinato, in Shader Graph.
- **Decisione:** una *Full Screen Pass Renderer Feature* di URP sui due renderer, dopo il post-processing e prima dell'interfaccia in overlay, che resta nitida. Lo shader (`Retro.shader`) è scritto a mano in HLSL: un ciclo sui 32 colori di `RetroPalette` con una distanza pesata, in sRGB, spostato dalla soglia di Bayer 4 × 4 del pixel *sullo schermo*, così il motivo non scorre quando la camera si muove o gira. `RetroFilter` in Core lo regola con proprietà globali (palette, forza, dithering), senza toccare asset. Tre scelte nelle opzioni, *spento*, *leggero* e *pieno*, ricordate tra le preferenze. Dalla prova del passo 11.7 si parte da *spento*: con il filtro la luce delle torce si spezza in bande, e a Mirco non piace.
- **Alternative scartate:** Shader Graph, dove il ciclo sarebbe finito comunque in un nodo *Custom Function* con lo stesso codice, e un grafo non si scrive bene da uno script; la pixelatura a bassa risoluzione, che rende illeggibili i testi in Cinzel; un filtro sempre acceso.
- **Conseguenze:** il filtro è un'opzione e non l'aspetto del gioco: chi lo vuole lo accende. Il dithering resta fermo sullo schermo durante gli scatti della visuale. Il passo a tutto schermo non agisce su llvmpipe, il renderer software della CI (ADR-083).

## ADR-082 — Arredi a composizioni contro i muri, con un seme loro e fuori dai ricordi; colonnati e mucchietti in mezzo
- **Data:** 2026-10-17 (passi 11.3, 11.4 e 11.7, scritto durante la chiusura della M11)
- **Contesto:** le stanze erano spoglie (D4). Una cella di 4 m con un barile solo sembra vuota; ogni tiro in più nel popolatore cambia i livelli di tutti i semi (trappola 10 della M7); i ricordi dei livelli (ADR-079) contano nemici e casse per posto; un arredo con un collider cambia il NavMesh e può chiudere un passaggio.
- **Decisione:** un arredo è una **composizione** di due–quattro modelli in un prefab, che occupa una cella: il retro sul muro, il davanti verso la stanza, i pezzi grossi con un collider sul layer degli ostacoli e quelli piccoli senza. Un **tema** per stanza nella cripta (deposito, studio, cella, rovina, cinque composizioni ciascuno) e per zona di 7 × 7 celle nelle caverne (cristalli, ossa, frane). Il popolatore le mette per ultime, con un flusso suo del seme (`SeedMixer.ForDecor`), mai accanto a un passaggio, alla scala o davanti a una cassa, e toglie quella che chiude il cammino. Dalla prova del passo 11.7: nelle stanze di almeno 5 × 5 celle un **colonnato** in mezzo, due file simmetriche a una cella dai muri, tutto o niente per gruppi speculari; e fino a due **mucchietti** a terra (ossa, cianfrusaglie, armi rotte), senza collider, di preferenza lontano dai muri. Gli arredi non entrano nei ricordi dei livelli: il seme li rimette uguali.
- **Alternative scartate:** oggetti sparsi uno per cella, che riempiono poco e chiudono più passaggi; arredi nello stesso flusso del seme di nemici e casse, che avrebbe cambiato i livelli e i test di tutti i semi già provati; arredi qualunque in mezzo alle stanze, che non si spostano né si rompono e chiuderebbero i passaggi.
- **Conseguenze:** nemici, casse e mappa dei semi già provati sono rimasti uguali, e i test lo controllano togliendo gli arredi. La costruzione resta sotto i 50 ms della D4 della M6. I barili da rompere, dopo la v1.0, potranno stare anche in mezzo alle stanze con le stesse regole.

## ADR-083 — Il controllo dei pixel del filtro si ignora sul renderer software della CI
- **Data:** 2026-10-17 (passo 11.4, scritto durante la chiusura della M11)
- **Contesto:** il test che controlla che con il filtro pieno ogni pixel sia un colore della palette passava in locale, anche con OpenGL forzato, e falliva in CI con quasi tutti i pixel fuori palette. La CI disegna con **llvmpipe** (OpenGL in software, Mesa): lo shader è supportato e la forza arriva, ma il passo a tutto schermo non compare nell'immagine.
- **Decisione:** sul renderer llvmpipe la parte dei pixel del test si ignora (`Assert.Ignore`), con nel motivo quello che serve a capire il perché (shader supportato, forza globale); il resto del test vale ovunque. Il controllo vero dei pixel resta sulle schede vere, D3D11 e OpenGL, e il gioco esce per Windows.
- **Alternative scartate:** togliere il controllo dei pixel, che sulle schede vere ha senso; una scheda video in CI, che con GameCI e i runner gratuiti non c'è; ignorare tutto il test in CI.
- **Conseguenze:** la CI è tornata verde senza perdere il test in locale. Un test che legge i pixel di un effetto a tutto schermo va provato anche con `-force-glcore` (trappola 6 della M11).

## ADR-084 — La musica del boss come profilo d'ambiente
- **Data:** 2026-10-17 (passo 11.5, scritto durante la chiusura della M11)
- **Contesto:** lo scontro con il Re delle ossa suonava come un livello qualunque (D5). L'`AmbiencePlayer` della M7 sfuma già musica, fondo e versi da un profilo all'altro tra i livelli.
- **Decisione:** la musica dello scontro è un profilo d'ambiente in più (`AmbienceBoss`): il fondo della caverna, nessun verso nel buio, il brano a 0,4. Il composition root lo passa al lettore su `FightStarted`, quando la porta si chiude, e rimette quello del livello su `Defeated` e su `FightReset`: le due musiche sfumano l'una nell'altra in 2,5 s.
- **Alternative scartate:** una sorgente a parte per la musica del boss, con la sua sfumatura da scrivere e da coordinare con quella del livello; uno stinger corto all'inizio dello scontro, che non accompagna un combattimento a tre fasi.
- **Conseguenze:** nessun codice nuovo nel lettore. Un altro momento con la sua musica, per esempio un nemico unico dopo la v1.0, è un profilo e due righe nel composition root.

## ADR-085 — Le prestazioni misurate da un benchmark che gioca da solo in build, e il vSync acceso
- **Data:** 2026-10-17 (passo 11.6, scritto durante la chiusura della M11)
- **Contesto:** le misure della M9 erano nell'editor; il piano chiedeva il profiling in build (D6), su un PC dove Claude non può giocare né guardare il Profiler.
- **Decisione:** `-benchmark risultati.txt` con `-seed` fa camminare e combattere il cavaliere da solo, 20 s per tratto, in città, nella cripta, nelle caverne e contro il re, senza che muoia; scrive per ogni tratto i frame medi, p95, p99 e massimi, il main thread, le allocazioni (nella build di sviluppo), batch, SetPass e triangoli. I conti stanno in `FrameStats`, in Core, con i test. La build di sviluppo esce dalla CI con un'opzione dell'avvio manuale (`-Development`). La misura spegne il vSync solo per sé; il gioco lo tiene acceso.
- **Alternative scartate:** una sessione del Profiler collegato, che vuole Mirco davanti all'editor e non si ripete uguale; contatori nell'HUD, che misurano anche sé stessi e non scrivono un resoconto.
- **Conseguenze:** la misura si ripete con lo stesso seme dopo ogni cambiamento. Ha trovato la sola correzione del passo: con il vSync spento il gioco girava a 600–700 fps con la GPU al massimo per niente. Su un PC più modesto i numeri restano da prendere.

## ADR-086 — La magia del portale al posto della pergamena del ritorno, chiusa nella sala del re
- **Data:** 2026-10-17 (passo 11.7, decisione di Mirco)
- **Contesto:** dalla M10 il ritorno in città era una pergamena consumabile (ADR-076), da trovare o comprare. Nella prova del passo 11.7 Mirco ha chiesto una magia fissa accanto alla cintura, e che il portale non servisse a fuggire dal re.
- **Decisione:** `SpellKind.Portal`, sempre nota e fuori dai posti F1–F3, in un posto suo a sinistra della cintura, con il tasto **T** o un click: 10 di mana, nessuna ricarica. `PlayerMagic.TryCastPortal` chiede prima all'`IPortalOpener` (`TownPortals`) se lì si può, e il risultato (`PortalResult`) dice perché no: in città, nella sala del re vivo (`BossController.HoldsPlayerAt`: durante lo scontro ovunque, prima dentro il quadrato della sala), senza mana. L'HUD lo scrive (`HudMessage`). Portali, ritorno nel punto e salvataggio restano quelli dell'ADR-076; la pergamena è sparita da oggetti, drop, casse, mercante e stringhe.
- **Alternative scartate:** pergamene impilabili (era nell'`ICEBOX`), che restano un oggetto da gestire nello zaino; un portale gratuito, che toglie al mana un uso; il blocco solo durante lo scontro, che lascerebbe aprire il portale nella sala appena prima di svegliare il re.
- **Conseguenze:** aggiorna l'ADR-076. Il mercante ha un posto in più per gli oggetti. Un salvataggio con una pergamena nello zaino la perde al caricamento, come ogni oggetto che il database non conosce più: nessun salvataggio era uscito dal PC di Mirco.

## ADR-087 — La versione del gioco dal tag `v*`, ricavata dal workflow
- **Data:** 2026-10-17 (passo 11.8, scritto durante la chiusura della M11)
- **Contesto:** la D7 chiedeva il `bundleVersion` 1.0.0 e la versione presa dal tag con `versioning: Tag` di GameCI. Nel codice della CLI di GameCI (v0.1.71) quella strategia usa `git tag --points-at HEAD` per intero e toglie solo una `v` iniziale: con due tag sullo stesso commit la versione diventa due righe, senza tag è vuota.
- **Decisione:** un passo del workflow `Build` la ricava da sé. Sui tag `v*` è il nome del tag senza la `v`, passato con `versioning: Custom`; sui tag di milestone e da avvio manuale `None`, cioè il `bundleVersion` del progetto. Se tag e progetto non coincidono la build esce lo stesso, con un avviso nel run. Il menu iniziale mostra la versione in piccolo (`Application.version`).
- **Alternative scartate:** `versioning: Tag`, per i motivi sopra; `Semantic`, che conta i commit dall'ultimo tag e darebbe 1.0.3 a una build fatta tre commit dopo; solo il `bundleVersion`, che si può dimenticare di aggiornare.
- **Conseguenze:** una release è il `bundleVersion` aggiornato e il suo tag. L'avviso ricorda di allineare il progetto se il tag arriva prima.

## ADR-088 — Codice con tutti i diritti riservati, asset fatti per il progetto CC0
- **Data:** 2026-10-17 (D8 della M11, decisione di Mirco del 12 ott 2026)
- **Contesto:** il repo è pubblico perché fa da portfolio. Senza un file di licenza chi legge non sa cosa può fare; Mirco non vuole che il codice o il gioco si riusino. `CREDITS.md` dichiara CC0, da prima, i suoni, le icone e i modelli generati per il progetto.
- **Decisione:** un `LICENSE` in italiano e in inglese: copyright di Mirco Zanzaro, tutti i diritti riservati; il repo si legge, ma il codice e i contenuti non si copiano, non si modificano e non si ridistribuiscono senza permesso. Eccezione: gli asset di `CREDITS.md`, quelli di terzi con le loro licenze e quelli fatti per il progetto CC0 come già dichiarato. Il README ha una sezione che lo dice.
- **Alternative scartate:** la MIT proposta nella scheda, che permette il riuso; nessuna licenza, che per legge vuol dire lo stesso ma non lo dice a chi legge; riprendere indietro il CC0 degli asset già dichiarati.
- **Conseguenze:** chi legge il repo sa cosa può fare. Un contributo esterno andrebbe concordato a parte. Un asset nuovo fatto per il progetto va dichiarato in `CREDITS.md` con la sua licenza.

## ADR-089 — I nemici speciali come promozione dopo il popolatore, l'unico dal seme della partita
- **Data:** 2026-10-18 (passi 12.1 e 12.2)
- **Contesto:** la M12 aggiunge campioni con modificatori e scorta e un unico per mondo. Il popolatore delle cripte e delle caverne decide già nemici, casse e arredi, e i semi già provati non devono cambiare (trappola 10 della M7).
- **Decisione:** `SpecialEnemyPlanner`, logica pura, gira dopo il generatore con un flusso suo del seme (`SeedMixer.ForSpecials`). Un campione è un nemico già messo, promosso: la mappa lo dice con la direttiva `@champions x,y,id,id`, e il builder aggiunge il `SpecialEnemy` al nemico di quella cella. La scorta va nelle celle libere attorno. L'unico ha un simbolo suo (`u`), e il tileset dice quale (Morvak nella cripta, Grumm nelle caverne). La profondità dell'unico la dice il seme della partita (`ForUnique`): una probabilità su tre a ogni profondità, e la prima che riesce è l'unica. Non si salva niente.
- **Alternative scartate:** campioni con un simbolo loro, che sposterebbero i nemici di tutti i semi; l'unico salvato nel salvataggio, che vorrebbe un formato nuovo per una cosa che il seme sa già.
- **Conseguenze:** un livello di un seme già provato ha gli stessi nemici, più le scorte. I ricordi dei livelli (ADR-079) di un salvataggio di prima non combaciano più e quei livelli ripartono da capo una volta.

## ADR-090 — La tinta dei nemici con un MaterialPropertyBlock, e il lampo del colpo che non la legge
- **Data:** 2026-10-19 (passi 12.3 e 12.6)
- **Contesto:** campioni e unici si riconoscono anche dal colore del modello, e il cavaliere rallentato dal Gelido diventa più freddo. I modelli KayKit condividono un materiale: cambiarne il colore tingerebbe tutti gli scheletri. Il lampo del colpo (`HitFlash`) scambia i materiali con uno bianco.
- **Decisione:** la tinta è un `MaterialPropertyBlock` su `_BaseColor`, per renderer. Il materiale del lampo usa `Unlit/Color`, che legge `_Color`: il blocco resta sul renderer anche con il materiale scambiato, ma non lo colora.
- **Alternative scartate:** copie dei materiali per ogni nemico speciale, che rompono il batching e vanno distrutte; il lampo che salva e toglie il blocco per poi rimetterlo, che si scontra con chi cambia la tinta nello stesso colpo (il Gelido rallenta proprio quando il cavaliere viene colpito).
- **Conseguenze:** chi aggiunge una tinta usa il blocco; chi aggiunge un materiale scambiato al volo controlla che non legga `_BaseColor`. Un test EditMode lo controlla per il lampo.

## ADR-091 — Il rallentamento del Gelido come componente del cavaliere
- **Data:** 2026-10-18 (passo 12.3)
- **Contesto:** il modificatore Gelido rallenta il cavaliere. La `StatSheet` non ha una velocità di movimento, e la velocità dell'agent la fissa il cavaliere all'avvio.
- **Decisione:** `MovementSlow` sul prefab del cavaliere è l'unico che tocca la velocità dell'agent: `Apply(frazione, durata)` la abbassa e rinnova la durata, senza sommare due rallentamenti, e alla fine la rimette com'era. Il `SpecialEnemy` Gelido ascolta i colpi che arrivano al cavaliere e lo chiama quando la sorgente è lui, dardi compresi.
- **Alternative scartate:** una statistica di velocità nella `StatSheet`, che vorrebbe affissi e interfaccia per un solo uso; il rallentamento dentro il controller del cavaliere, che mescola input e stati.
- **Conseguenze:** un altro effetto sulla velocità (una pozione, una trappola) passa da `MovementSlow` o da un componente fatto come lui, mai direttamente dall'agent.

## ADR-092 — Il loot in più come bonus, con un seme per tiro, per nemici e casse
- **Data:** 2026-10-19 (passo 12.4)
- **Contesto:** campioni e unici devono lasciare loot migliore, e il piano voleva casse di più tipi: un forziere accanto ai capi e un tesoro del re. Il loot di un seme già provato non deve cambiare.
- **Decisione:** `LootBonus` (tiri in più, fattori di magici e rari, rarità garantita, fattore dell'oro) sta nei numeri del grado e sulle casse. Il primo tiro resta quello di sempre; ogni tiro in più ha un seme suo (`ForExtraLoot`), cade sempre e usa la tabella delle rarità spostata. Se la rarità garantita manca, la prende l'ultimo tiro, rifatto con la stessa base e lo stesso seme. Forziere e tesoro sono `Chest` con un bonus e una loot table loro; il tesoro è sigillato finché il re è vivo, e lo stato si ricava dalla vittoria salvata, senza un campo nuovo.
- **Alternative scartate:** loot table diverse per i campioni, che cambierebbero il primo oggetto dei nemici promossi; tiri in più con la probabilità della tabella, che lascerebbero campioni senza niente in più; un tipo di cassa nuovo, quando la cassa sapeva già fare tutto tranne il bonus.
- **Conseguenze:** un nemico o una cassa nuovi con loot migliore sono un bonus nei dati. Il tesoro può portare la pergamena del livello, e allora la si prende dopo la vittoria.

## ADR-093 — Ogni modificatore dei campioni si vede e si sente
- **Data:** 2026-10-19 (v1.1.1)
- **Contesto:** nella prova della v1.1.0 Mirco non ha notato differenze tra un campione con i modificatori e un nemico normale. Il codice funzionava, ma i numeri erano timidi (Rapido +30%, Rigenerante fermo per 3 s dopo ogni colpo, quindi quasi mai attivo in combattimento) e solo il Gelido aveva un segno, una tinta leggera sul cavaliere.
- **Decisione:** ogni modificatore ha numeri più marcati e un segno suo. Rapido: +50%, con le animazioni alla stessa velocità (`Animator.speed`) e la polvere. Gelido: 50% per 3 s, brina attorno al campione, tinta più fredda e cristalli sul cavaliere. Rigenerante: 3% al secondo dopo 1 s, con un bagliore verde e il numero verde. Inamovibile: scintille e suono di metallo quando lo si colpisce, e "Imparabile!" quando lo scudo l'avrebbe fermato (`ShieldBlock.RollPierced`). Furioso: anche +25% di velocità, più grande, rosso, con un'aura e un ruggito. Gli effetti sono prefab di particelle in `Prefabs/VFX`, presi dalle `ChampionSettings`, figli del nemico; la tinta non tocca né le particelle né l'anello.
- **Alternative scartate:** solo numeri più alti, che renderebbero i campioni più duri senza dire perché; scritte sopra la testa per ogni modificatore, che nello sciame si sovrappongono.
- **Conseguenze:** un modificatore nuovo vuole anche il suo segno. Il tiro in più dello scudo contro un colpo imparabile consuma la sorgente casuale del combattimento solo per gli Inamovibili.

## ADR-094 — I modelli nostri fatti in Blender da script, con Blender 5.2 LTS bloccato
- **Data:** 2026-10-19 (passo 13.1, D9 della M13)
- **Contesto:** i pacchetti KayKit e Quaternius non hanno pozzi, santuari, sarcofagi, statue, anfore, gabbie e altari, che servono alle stanze della M13. Cercarli in altri pacchetti CC0 vorrebbe dire stili diversi e download da approvare uno per uno. Claude non disegna a mano: può solo scrivere codice.
- **Decisione:** i modelli si costruiscono con script Python dentro **Blender 5.2 LTS**, la versione bloccata come quella di Unity. Gli script stanno in `Tools/Blender/`: una libreria comune (`meshlib.py`) con i colori presi dall'atlante KayKit (`dungeon_texture.png`, la v segue l'altezza come nei KayKit) e uno script per gruppo di modelli in `models/`. `make.py` esporta gli FBX in `Art/Models/DarkDescent/` con la trasformazione applicata e gli assi di Unity; in Unity `GeneratedModelImporter` dà le impostazioni dei KayKit, e il prefab usa `M_Dungeon`. Ogni modello passa da una foto accanto a un KayKit prima di entrare nei prefab.
- **Alternative scartate:** modelli fatti a mano in Blender, che Mirco dovrebbe disegnare; altri pacchetti CC0, con stili diversi; un generatore dentro Unity (`Mesh` da codice), che non ha smusso, foto né export e lega il modello all'editor.
- **Conseguenze:** ogni modello si rifà e si modifica dallo script, e la licenza è nostra (CC0 come gli altri asset fatti per il progetto). Le forme geometriche vengono bene, quelle organiche (personaggi, elmi) no: la prova del 19 ott lo ha mostrato con l'elmo, e restano ai KayKit. Aggiornare Blender vuol dire rifare le foto e controllare gli FBX con `GeneratedModelTests`.

## ADR-095 — I suoni nostri fatti da script, con numpy e scipy, un seme per suono
- **Data:** 2026-10-19 (passo 13.1, D14 della M13)
- **Contesto:** i suoni sintetizzati delle milestone di prima (monete, portale, incantesimi, salita di livello) venivano da script Python rimasti fuori dal repo: non si potevano rifare né ritoccare. La M13 ha bisogno di suoni nuovi per santuari, pozzi, trappole e barili.
- **Decisione:** gli script stanno in `Tools/Audio/`, con numpy e scipy alle versioni di `requirements.txt`. Una libreria comune (`sfxlib.py`: inviluppi, filtri, risonatori, campane, legno, bolle, riverbero a convoluzione, sample del progetto ricampionati) e uno script per suono in `sounds/`. Il seme di ogni suono viene dal suo nome. `make.py` scrive i WAV in `Audio/SFX/Generated`; con `--page` scrive anche una pagina di ascolto con gli spettrogrammi e il suono più vicino che il gioco ha già, da far sentire a Mirco prima dei prefab.
- **Alternative scartate:** suoni da altri pacchetti CC0, da scaricare e approvare uno per uno; un programma per la sintesi come SuperCollider o Csound, più potente del necessario e da installare; un seme solo per tutti i suoni, che farebbe cambiare tutti i WAV quando se ne aggiunge uno.
- **Conseguenze:** i suoni "geometrici" (campane, meccanismi, acqua, magie) si fanno qui; il legno e le esplosioni vengono meglio partendo da un sample elaborato, e allora il WAV prende la licenza del sample; voci e creature restano ai sample. I suoni delle milestone di prima restano come sono finché non serve ritoccarli.

## ADR-096 — Suoni generati con Stable Audio 3 attraverso ComfyUI, con prompt e seme nel repo
- **Data:** 2026-10-20 (passo 13.4, D15 e D16 della M13)
- **Contesto:** la sintesi di `Tools/Audio` regge campane e meccanismi, meno il legno, l'acqua e i colpi. Mirco ha installato ComfyUI Desktop e scaricato Stable Audio 3.0 Small SFX e TRELLIS.2, per generare suoni e modelli in locale.
- **Decisione:** `Tools/ComfyUI/` guida ComfyUI attraverso l'API locale: un client con la sola libreria standard, i workflow in formato API in `workflows/` (gli ingressi si cambiano per titolo del nodo) e una ricetta per asset con prompt, negativo, durata e seme. I WAV passano dalla finitura e dalla pagina di ascolto di `Tools/Audio` e finiscono in `Audio/SFX/Generated` con il suffisso `_sa`; rigenerati da ricetta escono identici a quelli ascoltati. Licenze: Stable Audio 3 ha la *Stability AI Community License*, per cui gli output sono nostri e non contano come opere derivate. Se il gioco viene venduto serve la registrazione gratuita presso Stability, valida sotto 1 milione di dollari di ricavi annui. L'encoder T5Gemma ha i *Gemma Terms of Use*, e Google non rivendica gli output. TRELLIS.2 e BiRefNet sono MIT; DINOv3 ha la licenza di Meta, da leggere prima che un modello generato entri nel gioco. TRELLIS parte da una foto da Blender (D16).
- **Alternative scartate:** solo sintesi, che fa peggio legno e acqua; pacchetti di suoni da scaricare uno per uno; servizi online, con termini diversi per ognuno e risultati che non si rifanno; Stable Audio 3 Medium (9,2 GB, per musica e suoni lunghi); Pixal3D (segnalato come bloccato nell'UE) e Hunyuan3D (la licenza esclude l'UE).
- **Conseguenze:** i suoni di legno, acqua e colpi si chiedono prima a Stable Audio, ognuno con la sua ricetta e una pagina di ascolto; i WAV da script restano come riserva. Nella prova TRELLIS ha rifatto la statua da script quasi uguale, solo più morbida e con quasi cinque volte le facce: da una foto senza dettaglio non ne aggiunge. Per ora nessun modello generato entra nel gioco, e l'immagine di partenza da testo è negli sviluppi dopo la v1.0. ComfyUI si può avviare anche senza l'app, con i modelli della cartella condivisa (`Tools/README.md`).
## ADR-097 — Barili, pozzi, santuari e trappole nel flusso degli arredi, con i ricordi contati a parte
- **Data:** 2026-10-21 (passi 13.2 e 13.5–13.9)
- **Contesto:** la M13 mette nei livelli barili da rompere, pozzi, santuari e spuntoni. Nemici e casse dei semi già provati non devono cambiare, e i ricordi dei livelli (ADR-079) di un salvataggio di prima non conoscono questi oggetti.
- **Decisione:** tutto entra nel flusso degli arredi (`Decorate`), che gira dopo nemici e casse. Il tiro che decideva il colonnato, tolto, sceglie il punto d'interesse della stanza; quale pozzo e quale santuario, la rotazione e lo scarto degli oggetti in mezzo vengono da un hash della cella, senza consumare tiri. Il resto ha domini suoi del seme: il loot del barile (`ForBarrel`), il barile esplosivo (`ForExplosive`), il compenso della cassa vuota (`ForComfort`). Ogni oggetto ha un simbolo suo nella mappa; gli spuntoni non sono un ostacolo. `LevelRecord` conta barili e punti d'interesse a parte (`MatchesBreakables`, `MatchesFeatures`): un ricordo di prima della M13 li fa ripartire nuovi e lascia com'erano nemici e casse. Pozzi e santuari, al più uno per livello, contano quando sono messi, non quando sono tirati.
- **Alternative scartate:** il flusso principale del seme, che avrebbe spostato nemici e casse di ogni seme; un solo conteggio nel ricordo, che avrebbe fatto ripartire nemici e casse dei salvataggi di prima; tipo e rotazione dal flusso degli arredi, che li avrebbe fatti scorrere tutti a ogni oggetto aggiunto.
- **Conseguenze:** un livello di un seme già provato ha gli stessi nemici e le stesse casse; cambiano gli arredi. Un altro oggetto da ricordare prende un conteggio suo, e un'altra scelta per cella un hash o un dominio suo.

## ADR-098 — Il barile da rompere è un bersaglio senza vita
- **Data:** 2026-10-21 (passi 13.5 e 13.7)
- **Contesto:** il barile si clicca e si colpisce come un nemico, ma non dà esperienza, non ha la barra in alto, non è sull'automappa e non conta tra i nemici del livello.
- **Decisione:** `Breakable` è un `IDamageable` senza `Health`, sul layer dei nemici. Qualsiasi colpo lo rompe, anche quello mancato. Il controller lo cerca dopo i nemici e senza la tolleranza del pavimento, il cerchio rosso lo segna come un nemico e l'attacco si ferma sul raggio del suo ostacolo. Il barile esplosivo accende la miccia, poi ferisce una volta ogni `Health` nel raggio e rompe i barili vicini; finché la miccia è accesa il ricordo del livello lo tiene intero (`IsFusing`).
- **Alternative scartate:** una `Health` da un punto, che avrebbe portato esperienza, barra, automappa e conteggi dei nemici da spegnere uno per uno; un `Interactable` come la cassa, che non si colpisce e che il cavaliere non attacca tenendo premuto.
- **Conseguenze:** chi cerca i nemici per `Health` non vede i barili. Le magie ad area oggi cercano le `Health`, quindi non rompono i barili: se serve, cercano anche i `Breakable`.

## ADR-099 — Il bonus del santuario come modificatori della scheda, non salvato
- **Data:** 2026-10-21 (passo 13.6)
- **Contesto:** i santuari danno per 60 s più danno, più velocità, più armatura o più esperienza. La `StatSheet` non aveva bonus a tempo né una statistica per il danno in più e per l'esperienza; la velocità la cambia solo `MovementSlow` (ADR-091).
- **Decisione:** `ShrineBonus` sul cavaliere tiene un bonus alla volta. Danno, armatura ed esperienza sono modificatori della scheda con il componente come sorgente, tolti con `RemoveModifiersFrom`: due statistiche nuove, `DamageBonus` (applicata con `CombatFormulas.WithBonus` alla mischia e alle magie) ed `ExperienceBonus` (dopo la riduzione dell'esperienza per livello). La velocità passa da `MovementSlow.SetHaste`, che si somma al rallentamento del Gelido. Gli eventi arrivano a ogni secondo intero, per l'icona accanto alla cintura e per il messaggio. Il bonus finisce alla morte e non si salva.
- **Alternative scartate:** un riferimento al santuario in ogni attacco, da moltiplicare a mano; salvare il bonus, con un formato nuovo per un minuto di gioco.
- **Conseguenze:** un altro effetto a tempo (una pozione, una maledizione) usa lo stesso schema: modificatori con sorgente, velocità da `MovementSlow`, eventi per secondo.

## ADR-100 — Il loot si misura con i valori attesi sui livelli generati
- **Data:** 2026-10-21 (passo 13.8)
- **Contesto:** dopo la M12 il loot era troppo: la stima per la cripta alla profondità 3 era di circa 10 oggetti a livello, e l'obiettivo di Mirco era 4–5. I numeri della D13 erano stimati a mano.
- **Decisione:** `LootCensus`, nell'editor, genera i livelli come il gioco (`DungeonLevel.CreateMap`, nemici speciali e scorte compresi) e somma per ogni fonte gli oggetti attesi, divisi per rarità: valori attesi, non tiri, quindi senza rumore. Un test EditMode tiene la media tra 4 e 5,2 su 40 livelli per profondità. `LootBonus` ha `ReplacesRoll` (solo i tiri in più) ed `ExtraChance` (la probabilità di ogni tiro in più); le scorte, scritte nella direttiva `escorts` della mappa, tirano lo stesso numero con la soglia dimezzata; la cassa senza oggetto dà una pozione o il doppio dell'oro.
- **Alternative scartate:** simulare i tiri, più lento e con un rumore da tollerare nel test; stimare a mano, che con i numeri della D13 dava 4–5 e la misura 6,7.
- **Conseguenze:** ogni fonte di loot nuova va nel conteggio, e il test dice quando la media esce dalla fascia. I tagli si decidono con i numeri davanti: la D13 è passata da 6,7 a 5,0 con tre tagli scelti da Mirco.

## ADR-101 — I gradi degli affissi sono asset, e il generatore sceglie prima il gruppo
- **Data:** 2026-10-22 (passo 14.1)
- **Contesto:** con le difficoltà il livello degli oggetti arriva a 55, e gli affissi di prima (due per gruppo) non bastavano. Il generatore pescava tra tutti gli affissi ammessi: un gruppo con più voci usciva più spesso, e danno e vita uscivano il doppio degli altri.
- **Decisione:** ogni gruppo ha sei gradi, dal livello dell'oggetto 1, 6, 15, 25, 35 e 45; ogni grado è un `AffixDefinition` con il suo id e il campo `_grade`. Il livello dell'oggetto è il livello della zona, con quello della difficoltà. Il generatore sceglie prima il gruppo, con lo stesso peso per tutti quelli ammessi, poi il grado tra i due più alti che il livello consente (`OpenGrades`), poi il valore. Gli affissi di prima restano i gradi I e II con gli stessi id; il tooltip scrive il grado in piccolo dopo il valore.
- **Alternative scartate:** un affisso per gruppo con il valore che cresce con il livello, che avrebbe cambiato gli oggetti salvati e tolto i nomi per grado; pescare tra tutti i gradi ammessi, che dà a un oggetto dell'Abisso un +3 Forza.
- **Conseguenze:** gli oggetti salvati non cambiano, perché salvano gli id. Lo stesso seme fa un oggetto diverso da prima: i test con valori scritti sono stati rivisti uno per uno. Un grado nuovo è un asset con un id nuovo: duplicando un asset si copia anche l'id.

## ADR-102 — La difficoltà è una tabella di moltiplicatori sopra i numeri dei nemici
- **Data:** 2026-10-22 (passi 14.2 e 14.3)
- **Contesto:** vita e danno dei nemici sono numeri fissi per tipo nei prefab, senza una formula da far crescere. Servivano nove gradini, dal Crepuscolo all'Abisso VI, tarabili senza codice.
- **Decisione:** una `DifficultyTable`, un asset con una riga per gradino (`DifficultyTier`): livello della zona in più, moltiplicatori di vita, danno, esperienza e oro, punti a colpire e di Armatura in più dei nemici, quanti modificatori dei campioni prende il re, i pesi delle rarità e quanto costa morire. Il composition root li applica a ogni nemico che lega, re ed evocati compresi. Il danno del grado e quello della difficoltà sono due campi distinti di `MeleeAttack` e `RangedAttack`, composti in `DamageMultiplier` e solo lì; l'anello di fuoco del re lo usa come i suoi colpi. `DifficultyProgress`, in Core, tiene la difficoltà attuale, la più alta completata e il numero del mondo.
- **Alternative scartate:** prefab o livelli diversi per difficoltà, che moltiplicano gli asset; nemici che crescono con il livello del cavaliere, che tolgono il senso di salire (resta nel piano da valutare).
- **Conseguenze:** un gradino in più è una riga in più. Ogni fonte di danno nuova deve passare da `DamageMultiplier`, o resta uguale a ogni difficoltà come l'anello del re fino al passo 14.6. Cambiando difficoltà, `Changed` arriva prima di `WorldChanged`: chi rifà il mondo legge già i numeri nuovi (passo 14.7).

## ADR-103 — Un mondo nuovo viene da un seme ricavato dal vecchio
- **Data:** 2026-10-22 (passo 14.5)
- **Contesto:** cambiare difficoltà o rifare il mondo deve dare un dungeon nuovo, con il re vivo e il suo tesoro sigillato, senza toccare personaggio, oggetti e oro. Un seme dall'orologio renderebbe la partita non riproducibile.
- **Decisione:** `SeedMixer.ForWorld(seme di prima, numero del mondo)`, con un dominio suo. Il cambio avviene all'accampamento: si svuotano insieme i ricordi dei livelli, la mappa scoperta e il portale salvato, il re torna vivo, il mercante si rifornisce e si salva. Il salvataggio (formato 6) porta il seme del mondo attuale, la difficoltà, la più alta completata e il numero del mondo.
- **Alternative scartate:** un seme nuovo dall'orologio; tenere i ricordi dei livelli, che rimetterebbero casse aperte e nemici uccisi in un livello diverso.
- **Conseguenze:** la stessa partita rifà gli stessi mondi, e un test li rifà uguali. Ogni stato nuovo legato al mondo va azzerato nello stesso punto, `HandleWorldChanged`.

## ADR-104 — Morire costa oro ed esperienza, mai un livello
- **Data:** 2026-10-22 (passi 14.2 e 14.3)
- **Contesto:** dalla M7 morire non costava niente (ADR-048). Mirco ha deciso il 18 ottobre 2026 una perdita di oro ed esperienza che cresce con la difficoltà.
- **Decisione:** `DeathPenalty`, in Core: una parte dell'oro addosso e una parte dell'esperienza che serve per il livello successivo, mai più di quella già fatta nel livello (`CharacterProgress.Remove`). Al livello massimo non si perde esperienza. Le parti stanno nella riga della difficoltà: niente al Crepuscolo, dal 10% e 5% dell'Ombra fino al 30% e 25% dell'Abisso VI. La schermata della morte scrive quanto si è perso. Il resto di ADR-048 resta.
- **Alternative scartate:** lasciare l'oggetto in mano dove si è morti, come in Diablo 1, che chiede un recupero e un salvataggio dell'oggetto a terra; scendere di livello, che rimette in discussione i punti spesi.
- **Conseguenze:** al Crepuscolo il gioco resta quello che i tester conoscono. La penalità si regola come gli altri numeri della tabella.

## ADR-105 — Le difficoltà si misurano con le formule del gioco
- **Data:** 2026-10-22 (passo 14.6)
- **Contesto:** nove gradini non si tarano provando. I numeri indicativi della scheda facevano salire il cavaliere troppo in fretta, lasciavano il colpire della formula di Diablo 1 schiacciato ai limiti e allungavano troppo il re.
- **Decisione:** `PowerCensus`, in Core, calcola con le formule del combattimento quanti colpi servono per uccidere scheletro, bruto e re e quanti colpi arrivati regge il cavaliere: valori attesi, come `LootCensus` (ADR-100). `DifficultyCensus`, nell'editor, fa una discesa per difficoltà, una dopo l'altra: a ogni profondità il cavaliere ha il livello portato dall'esperienza dei livelli generati e la base migliore del mercante in ogni slot, al livello della zona e della rarità del gradino. Un test EditMode tiene le fasce a ogni profondità e l'inizio di ogni discesa.
- **Alternative scartate:** simulare i combattimenti, con un rumore da tollerare; tarare a mano provando in build.
- **Conseguenze:** i numeri della tabella sono scelti guardando le fasce. Il tetto del blocco al 75% decide quanto i nemici colpiscono dall'Abisso: se in prova è troppo facile, il primo numero da alzare è il danno.

## ADR-106 — La ronda notturna prova di notte quello che i push non vedono
- **Data:** 2026-10-24 (M15)
- **Contesto:** a ogni push la CI rifà tutta la suite, ma con semi fissi: vede sempre gli stessi livelli. Tre errori veri erano usciti solo per caso, da un seme diverso.
- **Decisione:** `nightly.yml`, solo con la pianificazione dell'1 UTC, esegue i test della categoria `Nightly`, che i test di ogni push escludono. Il seme della notte viene dalla data (o da `-nightly-seed`), e ogni parte ricava i suoi con `SeedMixer.ForNightly`. I controlli stanno in funzioni comuni (`MapChecks`, `CryptChecks`, `CaveChecks`, `CensusChecks`): i test di ogni push le usano sui semi fissi, la notte sui suoi. Ogni notte: 20000 mappe per tipo, 200 partite costruite con il NavMesh, una partita da sola per ogni difficoltà con salvataggio e ripresa a ogni livello, la partita lunga, le misure di loot e difficoltà. Il rapporto in Markdown va nella pagina del run e in un artefatto. Ogni errore trovato diventa un test con il seme fisso nella suite normale, poi si corregge.
- **Alternative scartate:** rifare di notte la suite di ogni push, che non trova niente di nuovo; semi casuali nei test di ogni push, rossi a caso; avvii a mano per provare le correzioni (Mirco, 23 ott 2026): 35 minuti nel gruppo `unity`, che fermano i push.
- **Conseguenze:** 30–35 minuti a notte. Il cron di GitHub può partire ore dopo e tenere il gruppo `unity` al mattino. I controlli con una soglia (il pavimento, la distanza della scala) si allargano quando una mappa vera al limite li fa cadere, e quella mappa resta come test.

## ADR-107 — L'Abisso I più duro, e le regole d'ordine delle difficoltà con il 5% di tolleranza
- **Data:** 2026-10-23 (passo 15.5)
- **Contesto:** con 256 equipaggiamenti per notte la misura delle difficoltà (ADR-105) trova l'Abisso I più facile della Tenebra dal 2 al 5,5%, perché da lì l'equipaggiamento diventa raro; da una notte all'altra la misura oscilla del 3%. Gli 8 equipaggiamenti fissi dei test di ogni push stavano per caso dalla parte buona.
- **Decisione (Mirco):** la vita dei nemici dell'Abisso I passa da ×13,5 a ×14,2, e le due regole d'ordine (ogni discesa e il re non più facili della difficoltà prima) hanno una tolleranza del 5% (`CensusChecks.StepTolerance`), a ogni push e di notte.
- **Alternative scartate:** solo il ritocco, che spostava l'inversione sull'Abisso II; solo la tolleranza, con cui a gioco invariato due notti su sei cadevano ancora; ritoccare tutte le difficoltà alte.
- **Conseguenze:** ogni inversione resta entro il 3,4%. Il ritocco esce con la v1.3.1.

## ADR-108 — Chi crea un oggetto di Unity a runtime lo distrugge
- **Data:** 2026-10-23 (passo 15.6b)
- **Contesto:** la partita lunga della ronda, sei mondi senza tornare al menu, ha trovato 11 `NavMeshData` e 10–20 mesh in più a ogni mondo. Scaricare una scena non distrugge gli oggetti fatti con `new` o da `BuildNavMesh`, e il gioco non chiama mai `UnloadUnusedAssets`: lo fa solo il ritorno al menu, che ricarica Core.
- **Decisione:** chi crea l'oggetto lo distrugge in `OnDestroy`: `BuiltLevel` il NavMesh che cuoce, `TelegraphSector` e `TelegraphCircle` la loro mesh. `LeftBehindTests` lo prova a ogni push; di notte la partita lunga segnala ogni misura che cresce a ogni mondo (oggetti di Unity per tipo, oggetti in scena, gestori degli eventi, memoria).
- **Alternative scartate:** `UnloadUnusedAssets` a ogni cambio di livello, che costa decine di millisecondi a ogni scala e nasconde chi perde; una mesh sola per tutti i telegrafi, che vorrebbe uno stato statico (ADR-007).
- **Conseguenze:** una regola per il codice nuovo: un `new Mesh`, un `new Material` o un NavMesh cotto hanno il loro `Destroy`. La memoria di Unity resta ferma da un mondo all'altro.

## ADR-109 — L'issue della ronda solo quando fallisce, un commento se è ancora aperta
- **Data:** 2026-10-24 (passo 15.7)
- **Contesto:** una ronda rossa deve farsi vedere senza aprire la pagina delle Actions ogni mattina, e una ronda verde non deve fare rumore (D9 della M15).
- **Decisione:** il passo `Issue` di `nightly.yml` parte quando un passo qualsiasi fallisce, i test o prima. `.github/scripts/ronda-issue.sh` scrive il titolo con la data e il numero di errori, e nel corpo il link al run, il seme, il commit, come rifarla in locale e solo le parti rosse del rapporto. Se un'issue `ronda` è aperta la commenta; si chiude a mano quando l'errore è corretto. L'avvio a mano con `issue_test` prova il passo senza Unity, con un rapporto finto.
- **Alternative scartate:** un'issue nuova per ogni notte rossa, che per lo stesso errore diventa rumore; la sola mail di GitHub sul run fallito, che non resta come lavoro da fare; provarla con una notte rossa voluta, che ne chiede due e un commit da togliere.
- **Conseguenze:** il workflow ha il permesso `issues: write`. La prova veloce resta, per riprovare il passo quando cambia.

## ADR-110 — Unici e set con un tiro a parte, dopo l'oggetto di sempre
- **Data:** 2026-10-25 (M16, D7)
- **Contesto:** unici e pezzi di set dovevano cadere dall'Ombra in su, più spesso da campioni, forzieri e re, senza cambiare il loot dei semi che i test e la ronda già conoscono.
- **Decisione:** `LootRoller.Promote` tira dopo la rarità minima, con un dominio suo del seme (`SeedMixer.ForNamed`), che non consuma tiri del nemico, della cassa o del barile: se riesce, l'oggetto diventa un unico o un pezzo della sua base, se ce n'è uno al suo livello, altrimenti resta com'è. Le probabilità stanno nella `DifficultyTier`; il `LootBonus` le moltiplica (`NamedFactor`) e il tesoro del re ha un tiro suo (`NamedGuarantee`), così anche al Crepuscolo ne può lasciare uno. Il mercante non li vende.
- **Alternative scartate:** unici e set come due righe in più della `RarityTable`, che sposta ogni tiro dopo e cambia tutto il loot provato; una tabella di loot a parte per gli unici, che può far cadere una base che il nemico non lascia.
- **Conseguenze:** al Crepuscolo e all'Ombra il loot di sempre è identico, e un test lo tiene. Una base senza unico né pezzo al suo livello non ne dà mai: la parte delle basi che li ha entra nella misura (ADR-113).

## ADR-111 — Una definizione sola per unici e pezzi, con i valori ai due livelli e i valori salvati
- **Data:** 2026-10-25 (M16, D6, D10, D13)
- **Contesto:** con nove difficoltà un unico a valori fissi diventa inutile due gradini dopo, e ritarare una tabella non deve cambiare gli oggetti già trovati.
- **Decisione:** `UniqueDefinition` descrive un unico e un pezzo di set, che ha in più il suo `SetDefinition`; ogni proprietà ha un valore al livello minimo e uno al 55, e l'oggetto prende quello del suo livello in linea retta, con un tiro di ±10%. `ItemInstance` salva l'id e i valori usciti (formato 7). Un id che il database non conosce più torna un raro della sua base senza affissi, con un avviso.
- **Alternative scartate:** due classi per unici e pezzi; salvare solo il seme e ricalcolare i valori, che con la taratura del passo 16.6 avrebbe cambiato gli oggetti nelle borse.
- **Conseguenze:** la taratura del 16.6 ha cambiato le tabelle senza migrazioni. Gli asset nascono da uno script che legge la tabella approvata, come i gradi della M14.

## ADR-112 — I bonus dei set con il set come sorgente, ricalcolati a ogni cambio
- **Data:** 2026-10-25 (M16, D9)
- **Contesto:** i bonus si accendono con due, tre e quattro pezzi indossati, e devono spegnersi togliendone uno, anche caricando una partita.
- **Decisione:** `SetBonuses`, in Core accanto all'`Equipment`, ascolta il suo `Changed`, conta i pezzi diversi di ogni set e rimette i modificatori dei bonus accesi con il set come sorgente, togliendo prima tutti quelli di prima, al livello del pezzo indossato più basso. Lo stesso pezzo nei due anelli conta una volta.
- **Alternative scartate:** controlli a ogni frame; bonus come modificatori degli oggetti, che restano accesi togliendo un altro pezzo.
- **Conseguenze:** nove slot, un ricalcolo che costa poco. Il tooltip scrive i bonus con la stessa regola, così quello che si legge è quello che si ha.

## ADR-113 — Unici e set misurati: quanti cadono e quanto rendono più di un raro
- **Data:** 2026-10-25 (passo 16.6, D11)
- **Contesto:** le probabilità della D7 e i valori della tabella del 16.1 erano scritti a occhio; un unico deve rendere almeno il 30% più di un raro della stessa base.
- **Decisione:** il `LootCensus` conta unici e pezzi come valore atteso per difficoltà, con la parte delle basi di ogni tabella che ne ha uno e i tiri di campioni, forzieri e re; un test lo confronta con i tiri veri. `DifficultyCensus.CompareNamed` e `CompareSet` mettono un cavaliere del livello dell'oggetto, con un raro per slot, contro il bruto della difficoltà di quel livello, e ne misurano la forza (`PowerCensus.Power`: colpi retti, tempo per uccidere, con velocità, danno ridotto, danno in più e rubavita) con la base nuda, un raro e l'unico, su 64 equipaggiamenti, ai livelli 20, 35 e 55. Le fasce: all'Ombra 1–2 a discesa, all'ultimo Abisso 3–5, ogni unico e set intero almeno 1,3 volte il raro. Un test a ogni push e la ronda su semi nuovi.
- **Alternative scartate:** simulare i combattimenti; tarare provando in build, dove un set intero vorrebbe ore.
- **Conseguenze:** la prima misura ha alzato gli Abissi a +0,4% a gradino fino al 4% e i valori di otto unici e dei due set. Il confronto conta solo il corpo a corpo: magia ed esperienza non danno forza, e un oggetto fatto di quelle va tenuto sopra con altro.

## ADR-114 — Immagini generate con Z-Image-Turbo, con la ricetta nel repo e la finitura da script
- **Data:** 2026-10-26 (passo 17.2, D8–D11 della M17)
- **Contesto:** la M17 rifà pannelli, sfondi e macchie a terra con immagini generate in locale, come i suoni dell'ADR-096. Il modello non dà la trasparenza né immagini che si ripetono, e il colore chiesto per il fondo non esce mai preciso.
- **Decisione:** l'ADR-096 si estende alle immagini. `Tools/ComfyUI/make.py` le genera con i workflow `z_image_turbo` e `z_image_seam`, da una ricetta per immagine in `images/` (prompt, seme, dimensione, finitura); `imagelib.py` fa la finitura con Pillow: la luce spianata, la ripetizione (sfumando l'originale e la copia spostata di mezzo lato, o ridisegnando con il modello la croce delle cuciture), la misura delle cuciture sui bordi e sulla linea di mezzo, la trasparenza sulla tinta del fondo letta dai bordi con il colore della tinta tolto dai bordi, il ritaglio e la dimensione. Una pagina mostra i semi provati accanto alla foto di prima; niente entra negli asset senza il sì di Mirco. Licenze: Z-Image-Turbo e l'encoder Qwen3 sono Apache 2.0; il VAE è quello di FLUX.1 (16 canali, scala 0,3611, spostamento 0,1159), nel repo di Z-Image sotto Apache 2.0 e in FLUX.1-schnell sotto Apache 2.0. Le immagini generate sono nostre.
- **Alternative scartate:** chiedere al modello "seamless tileable", che disegna lui una griglia di lastre con una linea al centro; ridisegnare sempre la croce con il modello, che sulle superfici lisce aggiunge una piega scura; la trasparenza dalla distanza dal colore del fondo, che rende semitrasparente un ferro scuro su un magenta scuro; nodi di ComfyUI da scaricare per la ripetizione.
- **Conseguenze:** un'immagine si rifà identica dalla sua ricetta. Le immagini si generano con il Python di ComfyUI, che ha Pillow. La riga in `CREDITS.md` arriva con la prima immagine che entra nel gioco.

## ADR-115 — La pelle dell'interfaccia e gli sfondi messi da uno strumento, non a mano
- **Data:** 2026-10-26 (passi 17.3 e 17.6, D4, D6 e D12 della M17)
- **Contesto:** l'interfaccia aveva 185 rettangoli a tinta unita e nessuna immagine di sfondo. Rivestirli a mano nell'editor vuol dire rifarlo a ogni finestra nuova, e un'immagine rigenerata non deve chiedere di ripassare la scena.
- **Decisione:** una `UiSkin` (ScriptableObject solo editor) tiene le immagini e i colori: lo sfondo che si ripete, la cornice in nove parti, la placca dei pulsanti e i tre sfondi a tutto schermo. `UiSkinTools.ApplyToCore`, anche da `-executeMethod`, riconosce il ruolo di ogni immagine dalla scena (le finestre dell'HUD, le scatole dei menu, i tooltip, i pulsanti) e la riveste: sfondo ripetuto e un figlio `SkinFrame` con la cornice, fuori dai layout; placca e colori del passaggio del mouse per i pulsanti; un figlio `Backdrop` con un `AspectRatioFitter` che copre lo schermo per menu, morte e vittoria. Menu e morte sono illustrazioni di Z-Image-Turbo; la vittoria è un render del gioco (`PhotoTour.VictoryBackdrop` e `Tools/ComfyUI/renders.py`), la sala del re appena abbattuto, perché l'accampamento generato sembrava di un altro gioco. Le impostazioni di import le scrive `GeneratedUiImporter`. Un test misura il contrasto di ogni testo sullo sfondo medio, almeno 4,5:1.
- **Alternative scartate:** una lista a mano delle finestre da rivestire, che dimentica le nuove; uno sprite in nove parti con lo sfondo dentro, che allunga la trama invece di ripeterla; sfondi stirati sullo schermo, che a 16:10 deformano il disegno.
- **Conseguenze:** una finestra nuova si riveste da sola con un comando. Lo sfondo opaco della morte ha mostrato che l'ordine dell'HUD conta: la morte ora sta dopo tutti i pezzi, sotto solo la dissolvenza e il nome del livello, e un test lo tiene.

## ADR-116 — Le macchie a terra con un seme a parte, in una direttiva e in una mesh sola
- **Data:** 2026-10-27 (passo 17.5, D5, D12 e D16 della M17)
- **Contesto:** l'atlante KayKit è fatto di sfumature senza dettagli: pavimenti e muri non si possono ritexturizzare senza cambiare lo stile, e lo stesso motivo si ripete in ogni stanza. Le macchie non devono cambiare i livelli che test e ronda conoscono.
- **Decisione:** `DecalPlanner`, logica pura, sceglie le macchie in `DungeonLevel.CreateMap` dopo tutto il resto, con un dominio suo del seme (`SeedMixer.ForDecals`): da 1 a 3 per stanza o per zona di 7×7 celle, qualcuna nei corridoi, il tema della stanza che sceglie le caselle, solo su pavimento libero e lontano da scala, ingresso, cancello e punti d'interesse. Le macchie viaggiano nella direttiva `@decals` (`DecalSpot`, sette interi), così la griglia non cambia e il salvataggio non le ricorda. `LevelBuilder` le mette in una mesh sola (`DecalLayer`) con un materiale solo, Lit trasparente nella coda 2990, sul layer di base fuori dal NavMesh, 2 cm sopra la cima della piastrella letta dalla sua mesh. Otto macchie in un atlante da 1024×512, generate con Z-Image-Turbo.
- **Alternative scartate:** i *Decal Projector* di URP, che vogliono il renderer delle decal e costano una proiezione per macchia; un quad per macchia, una chiamata di disegno l'una; le macchie come simboli della griglia, che occupano celle e spostano gli arredi; il tiro con il seme degli arredi, che cambia ogni livello già provato.
- **Conseguenze:** i livelli di ogni seme hanno la stessa griglia, gli stessi nemici e lo stesso loot di prima, e i tempi dei livelli non cambiano. Una macchia non si salva e non si raccoglie: è solo un disegno.

## ADR-117 — I riconoscimenti nel menu iniziale, la vittoria con Continua e Menu principale
- **Data:** 2026-10-26 (passo 17.4, lista corta del giro di foto)
- **Contesto:** i riconoscimenti si vedevano solo dopo aver battuto il re, e la vittoria portava sempre al menu. Mirco li ha voluti nel menu iniziale, e dalla vittoria la scelta di restare a giocare.
- **Decisione:** `CreditsPanel` diventa un pannello del menu, aperto dal pulsante *Riconoscimenti* al posto dei pulsanti, con lo stesso testo da `CREDITS.md`. `VictoryScreen` ha due esiti: *Continua* (`Continued`) chiude e si resta in città, *Menu principale* (`Finished`) salva e torna al menu come prima. Il menu iniziale ha i pulsanti in una colonna a sinistra con un layout verticale, e le opzioni sono per gruppi.
- **Alternative scartate:** lasciare i riconoscimenti anche nella vittoria, che allunga la schermata; un pulsante dei riconoscimenti nella pausa, che a metà partita non serve.
- **Conseguenze:** dopo la vittoria si può tornare all'altare e scegliere la difficoltà dopo senza passare dal menu.
