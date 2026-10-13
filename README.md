# DarkDescent

[![Tests](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml)

Action RPG isometrico dark fantasy, ispirato ai classici hack & slash di fine anni '90.
Progetto personale in Unity (URP) / C#.

![Città e loop completo: dal menu iniziale si comincia nell'accampamento sotterraneo, il cavaliere va dal mercante e compra una pozione; poi, alla profondità 8 delle caverne, entra nella sala del Re delle ossa, la porta si chiude, il re evoca lo sciame e in furia telegrafa l'anello di fuoco con un cerchio rosso; morto il re si apre il portale della vittoria, che porta alla schermata con le statistiche della partita](docs/media/m10_camp_boss.gif)

## Stato

**M10 — "Città e loop completo"** chiusa il 12 ottobre 2026 (tag `m10`): il gioco è finibile.
Parte da un menu con *Continua*, *Nuova partita* e tre salvataggi, una pausa con `Esc` e le
opzioni (lingua, volumi). Si comincia in un accampamento sotterraneo con il mercante, che vende
pozioni, pergamene del ritorno e oggetti tirati dal seme, e compra quello che si trova. L'oro cade
da nemici e casse; la pergamena del ritorno apre un portale verso l'accampamento, e il portale
riporta dove lo si era aperto. I livelli lasciati restano com'erano: nemici uccisi, casse aperte,
oggetti a terra. Alla profondità 8 il Re delle ossa aspetta in una sala che si chiude, con tre
fasi; morto lui, la schermata di vittoria e i riconoscimenti. Salvataggio al formato 5. Prossima:
**M11 — "Look, feel e release"**, con la v1.0.

Milestone precedenti:

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
e dove il **mercante** vende pozioni, oggetti e la **pergamena del ritorno** (letta nel dungeon apre
un portale per l'accampamento, e lì un portale riporta giù) e compra quelli della borsa (click destro);
e *Carica* usano tre posti di salvataggio (`save_1.json`…`save_3.json` nella cartella dei dati
dell'applicazione), che si possono anche cancellare. Nelle **opzioni** ci sono la lingua
(inglese, italiano), i volumi generale, della musica e degli effetti, e i nomi degli oggetti
a terra sempre visibili; tutto resta per gli avvii successivi. In fondo alle caverne, alla
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
  giorni). Eseguibile non firmato: al primo avvio Windows chiede conferma. La build Web
  va servita da un server, anche locale (`python -m http.server` nella cartella di
  `index.html`): aperta con un doppio click non parte.

Un run alla volta in tutto il repo, perché la licenza ha pochi posti di attivazione.

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
