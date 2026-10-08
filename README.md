# DarkDescent

[![Tests](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml)

Action RPG isometrico dark fantasy, ispirato ai classici hack & slash di fine anni '90.
Progetto personale in Unity (URP) / C#.

![Le profondità: nelle caverne del livello 7 il cavaliere affronta un bruto che carica il colpo con il settore rosso a terra, poi un gruppo dello sciame; in alto il nome e la vita del nemico sotto il cursore](docs/media/m7_caves.gif)

## Stato

**M7 — "Le profondità"** chiusa l'8 ottobre 2026 (tag `m7`): sotto la cripta, i livelli
5–8 sono caverne generate con un random walk. Ci vivono due nemici nuovi: lo sciame, veloce
e in branco, e il bruto, che carica un colpo telegrafato da un settore rosso a terra. L'IA è
fatta di classi di stato; i nemici che si allontanano troppo da casa tornano indietro. Colpi
critici con un verso per tipo di nemico, armi più o meno veloci, nome e vita del nemico
sotto il cursore. Musica, rumori d'ambiente, passi e un limite di voci audio. Morendo si
torna all'ingresso del livello con tutto quello che si aveva. Prossima: **M8 —
"Progressione e persistenza"**.

Milestone precedenti:

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
  nella cintura o nell'inventario, per berla
- `I` inventario: un click prende un oggetto, un altro lo posa (o lo mette nello slot);
  con un oggetto preso, un click fuori dalle finestre lo lascia a terra
- `C` pannello del personaggio; passando su una statistica, un tooltip dice a cosa serve
- `M` automappa: nell'angolo, sovrapposta al gioco, spenta
- `F9` cambia lingua (inglese, italiano); la scelta resta per gli avvii successivi

**Opzioni da riga di comando:**

- `-lang it` avvia in italiano (`en` per l'inglese)
- `-seed 4711` usa quel seme per dungeon e loot; senza, il seme è casuale e si legge nel
  log (`Player.log`), con il comando per rigiocarlo

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
