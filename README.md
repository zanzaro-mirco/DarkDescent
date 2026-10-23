# DarkDescent

[![Tests](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml)

Action RPG isometrico dark fantasy, ispirato ai classici hack & slash di fine anni '90.
Progetto personale in Unity (URP) / C#.

![La v1.0: il menu iniziale con la versione in basso a destra; l'accampamento; nella cripta, accanto a un colonnato tra barili e casse, il cavaliere affronta uno scheletro, e con il mouse sopra la sfera della vita si legge quanta ne resta; poi la magia del portale apre un portale azzurro che riporta all'accampamento](docs/media/v1_release.gif)

## Stato

**v1.3 — M14 "La luce si spegne"**, chiusa il 23 ottobre 2026 (tag `v1.3.0`). Rispetto alla v1.2:

- **nove difficoltà**: Crepuscolo, Ombra, Tenebra e Abisso I–VI, scelte da un **altare**
  all'accampamento. Battere il re apre la successiva, e si può tornare indietro o **rifare il mondo**:
  un dungeon nuovo con il re vivo, mentre personaggio, oggetti e oro restano;
- nemici con più vita, più danno, più colpire e più Armatura, e un **re** che prende i modificatori
  dei campioni; più esperienza, più oro e oggetti più rari;
- **sei gradi** per ogni gruppo di affissi, dal livello della zona: un nome per grado e il grado in
  piccolo nel tooltip;
- **livello 50**;
- **morire costa** una parte dell'oro e dell'esperienza del livello, dall'Ombra in su, senza mai
  scendere di livello;
- i numeri delle difficoltà sono **misurati** con le formule del gioco, non indovinati.

![Il pannello dell'altare all'accampamento: il Crepuscolo completato, l'Ombra attuale, la Tenebra aperta, i sei Abissi chiusi in grigio; sotto, Rifai il mondo e Indietro](docs/media/m14_altar.png)

**v1.2 — M13 "Stanze vive"**, chiusa il 21 ottobre 2026 (tag `v1.2.0`). Rispetto alla v1.1:

- **arredi con un senso**: un tema per stanza, mai due composizioni uguali, oggetti in mezzo girati
  a caso, e un quinto tema, la **cappella**; il colonnato non c'è più;
- al centro delle stanze grandi un **punto d'interesse**: **barili da rompere** (anche contro i muri),
  un **pozzo** della vita o del mana, un **santuario** che dà un bonus per un minuto (Furia, Vento,
  Pietra, Sapienza), o gli **spuntoni**;
- **trappole**: spuntoni nei corridoi e nei cunicoli, e dalla profondità 4 **barili esplosivi** che
  feriscono anche i nemici;
- **loot ritarato**: circa 5 oggetti a livello invece di quasi 7, con la stessa parte di magici e rari;
- modelli nuovi fatti **in Blender da script** e suoni nuovi fatti da script o con **Stable Audio 3**
  attraverso ComfyUI, tutti nel repo e rifacibili.

**v1.2.1** (tag `v1.2.1`): il portale aperto in un angolo stretto, tra muri e barili, va sul punto
libero più lontano, dove il cavaliere arriva.

![Una cappella della cripta: il cavaliere accanto al pozzo del mana, con il cristallo e la luce blu; intorno candele, un tavolo con un libro e una statua](docs/media/m13_well.png)

**v1.1 — M12 "Nemici speciali"**, chiusa il 19 ottobre 2026 (tag `v1.1.0`). Rispetto alla v1.0:

- i **campioni**: nemici più forti, più grandi e con un anello blu sotto i piedi, con uno o due
  modificatori (Rapido, Gelido, Rigenerante, Inamovibile, Furioso) e una scorta;
- due **unici**, uno per mondo: **Morvak il Carceriere** nella cripta e **Grumm Spaccaossa** nelle
  caverne, con l'anello d'oro;
- **loot migliore** da campioni e unici, un **forziere** accanto ai capi e il **tesoro del re**,
  sigillato in fondo alla sua sala finché il re è vivo;
- nella barra in alto il nome nel colore del grado, con i modificatori sotto, e i punti dei nemici
  speciali sull'automappa.

**v1.1.1** (tag `v1.1.1`): i modificatori si vedono e si sentono (polvere, brina, bagliore verde,
scintille, aura rossa), il negozio si chiude allontanandosi dal mercante, e il confronto con
l'oggetto indossato c'è anche per gli oggetti in vendita.

![Morvak il Carceriere con l'anello d'oro e la sua scorta di scheletri; in alto il suo nome in oro con i modificatori Frozen e Unstoppable](docs/media/m12_morvak.png)

**v1.0 — M11 "Look, feel e release"**, chiusa il 17 ottobre 2026 (tag `v1.0.0`). Rispetto alla M10:

- un **filtro retrò** a tutto schermo (palette scura e dithering), da attivare nelle opzioni,
  e un post-processing diverso per cripta, caverne e accampamento;
- **cripta e caverne arredate**: un tema per stanza (deposito, biblioteca, cella, rovine), colonnati
  in mezzo alle sale grandi, ossa e cianfrusaglie a terra; cristalli, ossa e frane nelle caverne;
- la **magia del portale** al posto della pergamena del ritorno, e i numeri di vita e mana sulle sfere;
- una **musica per lo scontro con il re**, i suoni mancanti e un giro sui volumi;
- prestazioni misurate in build con il Profiler e con un **benchmark** che gioca da solo;
- la versione del gioco nel menu iniziale, presa dal tag dalla CI.

La pubblicazione su itch.io viene dopo la v1.0: prima in **beta ristretta**, con una chiave per
ogni tester, e poi pubblica.

Milestone precedenti:

- **M10 — "Città e loop completo"** (tag `m10`): il gioco è finibile. Menu con tre salvataggi,
  pausa e opzioni; l'accampamento con il mercante; oro, portali per tornare in città, livelli che
  restano com'erano; il Re delle ossa alla profondità 8, con tre fasi, la vittoria e i
  riconoscimenti ([GIF](docs/media/m10_camp_boss.gif)).

- **M9 — "Magia"** (tag `m9`): mana, Dardo di fuoco, Anello di fuoco e Pelle di pietra,
  pergamene, lo scheletro mago che tira da lontano ([GIF](docs/media/m9_magic.gif)).

- **M8 — "Progressione e persistenza"** (tag `m8`): esperienza e livelli, punti attributo,
  equipaggiamento completo, salvataggio versionato, zoom e rotazione della visuale
  ([GIF](docs/media/m8_progression.gif)).

- **M7 — "Le profondità"** (tag `m7`): caverne generate con un random walk, lo sciame e il
  bruto con il colpo telegrafato, IA a stati, colpi critici, musica e rumori d'ambiente
  ([GIF](docs/media/m7_caves.gif)).

- **M6 — "Dungeon infinito"** (tag `m6`): cripte generate con un BSP, casse, pozioni nella
  cintura, automappa, finestra dell'editor per generare i livelli
  ([GIF](docs/media/m6_descent.gif)).

- **M5 — "Loot casuale"** (tag `m5`): oggetti magici e rari con affissi, nomi composti
  secondo la lingua, blocco con lo scudo, inglese e italiano, loot legato al seme
  ([GIF](docs/media/m5_loot.gif)).

- **M4 — "Raccolgo roba"** (tag `m4`): attributi alla Diablo e formula del colpo, oggetti
  a terra, inventario a griglia a click-e-click, arma e scudo in mano, pannello del
  personaggio ([GIF](docs/media/m4_loot.gif)).

- **M3 — "Un dungeon fatto a mano"** (tag `m3`): due livelli di una cripta buia da mappe
  di testo, torce, scala per scendere, camera Cinemachine, audio posizionale, build Web
  ([GIF](docs/media/m3_descent.gif)).

- **M2.5 — Pipeline automatica** (tag `m2.5`): test e build in CI con GitHub Actions e
  GameCI (vedi [CI](#ci)).
- **M2 — "Colpisco e muoio"** (tag `m2`): il primo gameplay loop, scheletri che
  inseguono e rispondono, sfera della vita, schermata di morte
  ([GIF](docs/media/m2_combat.gif)).
- **M1 — "Mi muovo"** (tag `m1`): click-to-move con NavMesh e camera isometrica
  ([GIF](docs/media/m1_click_to_move.gif)).

**Comandi:**

- click sinistro sul pavimento per muoversi; tenendo premuto, il personaggio segue il cursore
- click sinistro su un nemico per colpirlo una volta; tenendo premuto, continua a colpirlo.
  Il nemico sotto il cursore ha un cerchio rosso a terra, e in alto il suo nome e la sua vita
- click sinistro sulla scala per scendere al livello successivo
- click sinistro su un oggetto a terra per raccoglierlo, su una cassa per aprirla
- `1`–`8` bevono la pozione in quel posto della cintura; click destro su una pozione,
  nella cintura o nell'inventario, per berla, o su una pergamena per imparare l'incantesimo
- `T`, o un click sul posto a sinistra della cintura, lancia la **magia del portale** (10 di
  mana): apre un portale per l'accampamento. Non serve in città, e nella sala del re non funziona
  finché il re è vivo
- con il mouse sopra la sfera della vita o del mana si legge quanto resta; un'opzione li lascia
  sempre scritti
- click destro nel mondo per lanciare l'incantesimo scelto verso il cursore; `F1`–`F3`, o un
  click sui tre posti accanto alla sfera del mana, scelgono l'incantesimo
- `I` inventario: un click prende un oggetto, un altro lo posa (o lo mette nello slot);
  con un oggetto preso, un click fuori dalle finestre lo lascia a terra. Click destro su un
  oggetto della borsa per indossarlo
- `C` pannello del personaggio; passando su una statistica, un tooltip dice a cosa serve. I
  valori cambiati da oggetti o effetti sono azzurri se più alti, rossi se più bassi. Con punti
  da spendere, un `+` accanto a ogni attributo ne compra uno, e sopra la sfera della vita un
  pulsante dorato apre il pannello
- `Alt` tenuto premuto mostra i nomi di tutti gli oggetti a terra; un click su un nome lo raccoglie
- rotella del mouse per avvicinare e allontanare la visuale; `Q` ed `E` la girano di 90°
- `M` automappa: nell'angolo, sovrapposta al gioco, spenta
- `Esc` chiude le finestre aperte, poi mette in pausa: da lì si riprende, si aprono le
  opzioni o si salva per tornare al menu iniziale

Il gioco parte da un **menu iniziale**: *Continua* riprende l'ultima partita, *Nuova partita*
comincia nell'**accampamento** all'ingresso della cripta, da cui la scala scende al primo livello
e dove il **mercante** vende pozioni e oggetti e compra quelli della borsa (click destro); nel dungeon
la magia del portale apre un portale per l'accampamento, e lì un portale riporta dove lo si era aperto.
e *Carica* usano tre posti di salvataggio (`save_1.json`…`save_3.json` nella cartella dei dati
dell'applicazione), che si possono anche cancellare. Nelle **opzioni** ci sono la lingua
(inglese, italiano), i volumi generale, della musica e degli effetti, i nomi degli oggetti
a terra sempre visibili, i numeri di vita e mana sempre visibili e il filtro retrò (spento,
leggero, pieno); tutto resta per gli avvii successivi. In fondo alle caverne, alla
profondità 8, aspetta nella sua sala il **Re delle ossa**: entrati, la porta si chiude finché
uno dei due non cade. Morto il re, il portale della vittoria porta all'accampamento, alle statistiche
della partita e ai riconoscimenti; la partita resta salvata e si può riscendere.

**Opzioni da riga di comando:**

- `-lang it` avvia in italiano (`en` per l'inglese)
- `-seed 4711` usa quel seme per dungeon e loot; senza, il seme è casuale e si legge nel
  log (`Player.log`), con il comando per rigiocarlo
- `-newgame` salta il menu e comincia una partita nuova che non si salva (anche `-seed`)
- `-benchmark risultati.txt`, con `-seed`: il cavaliere gira da solo per città, cripta, caverne e
  scontro con il re misurando i frame; i risultati vanno nel log e nel file, poi il gioco si chiude.
  `-benchmark-seconds 5` accorcia i tratti, `-benchmark-fps 30` mette un tetto agli fps

## CI

Due workflow di GitHub Actions, con [GameCI](https://game.ci) e la licenza Unity Personal
(ADR-011…013):

- **[Tests](.github/workflows/tests.yml)**: test EditMode e PlayMode a ogni push su
  `main` e a ogni pull request. I risultati XML restano 14 giorni tra gli artifact del run.
- **[Build](.github/workflows/build.yml)**: build Windows sui tag di milestone (`m*`) e di
  versione (`v*`), o ad avvio manuale da *Actions → Build → Run workflow*, dove si può
  scegliere anche la build Web (`WebGL`). Prima rifà i test; la build si scarica dagli
  *Artifacts* del run (`DarkDescent-Windows-<tag>` o `DarkDescent-Web-<ramo>`, 30
  giorni). Sui tag di versione (`v1.0.0`) la versione del gioco è quella del tag; altrimenti
  quella del progetto. Eseguibile non firmato: al primo avvio Windows chiede conferma. La build Web
  va servita da un server, anche locale (`python -m http.server` nella cartella di
  `index.html`): aperta con un doppio click non parte.

Un run alla volta in tutto il repo, perché la licenza ha pochi posti di attivazione.

## Architettura

- **Scene.** `Core` resta sempre caricata, con cavaliere, camera, HUD, menu e `LevelManager`; i
  livelli si caricano sopra, uno alla volta (ADR-014).
  Accampamento, cripta e caverne li monta a runtime un solo `LevelBuilder`, da mappe di testo o
  dai generatori (BSP per la cripta, random walk per le caverne), con il NavMesh cotto dai
  collider (ADR-033, ADR-035, ADR-074).
- **Logica pura in `Core`.** Due assembly: `DarkDescent.Core` (`Scripts/Core/`) ha logica e dati
  senza componenti di scena (formule, statistiche, inventario, generatori di livelli e di loot,
  IA dei nemici a stati, salvataggio) e si prova con test EditMode veloci; `DarkDescent`
  (`Scripts/<Area>/`) ha i MonoBehaviour che la usano. Il confine lo controlla il compilatore
  (ADR-032, ADR-042).
- **Composition root.** Un `CompositionRoot` nella scena `Core` collega i sistemi all'avvio con dei
  `Bind`: niente singleton statici, e i componenti non cercano niente da soli (ADR-007).
- **Eventi.** I componenti si parlano con eventi C#, e la UI si aggiorna quando qualcosa cambia,
  senza controllare a ogni frame. Ogni iscrizione ha la sua disiscrizione.
- **Seme.** Un seme per partita, con un PRNG nostro, mescolato per livello e per dominio (loot,
  oro, pozioni, mercante, arredi): la stessa partita si rigioca uguale con `-seed`, e un sistema
  nuovo non cambia i livelli dei semi già provati (ADR-029, ADR-034, ADR-071).
- **Dati.** Oggetti, incantesimi, tabelle del loot e impostazioni dei generatori sono
  ScriptableObject immutabili; un oggetto nel salvataggio è solo il suo ID (ADR-022, ADR-064).
- **Salvataggio versionato.** JSON scritto per sostituzione in tre posti, con il numero del formato
  e una migrazione un passo alla volta; oggi è al formato 5, con i ricordi dei livelli lasciati
  (ADR-061, ADR-062, ADR-079).

Le decisioni, con il contesto e le alternative scartate, sono in [DECISIONS.md](DECISIONS.md).

## Licenza

Copyright © 2026 Mirco Zanzaro, tutti i diritti riservati ([LICENSE](LICENSE)). Il repository è
pubblico perché si possa leggere, ma il codice e il gioco non si copiano, non si riusano e non si
ridistribuiscono senza permesso. Gli asset di terzi restano con le loro licenze, elencate in
[CREDITS.md](CREDITS.md).

## Documenti

- [Piano di sviluppo](docs/Piano_Sviluppo_ARPG.md) — ambito, milestone, architettura
- [Milestone](docs/milestones/) — schede operative
- [DECISIONS.md](DECISIONS.md) — decisioni architetturali (ADR)
- [CONVENTIONS.md](CONVENTIONS.md) — convenzioni di codice
- [ICEBOX.md](ICEBOX.md) — idee fuori scope
- [CREDITS.md](CREDITS.md) — asset di terzi e licenze

## Setup

Richiede Unity 6.3 LTS **6000.3.24f1** (vedi `ProjectSettings/ProjectVersion.txt`) e Git LFS.

```
git clone <url>
git lfs pull
```
