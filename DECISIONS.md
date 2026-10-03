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
