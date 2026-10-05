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
