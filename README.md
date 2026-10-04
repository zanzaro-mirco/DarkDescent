# DarkDescent

[![Tests](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml)

Action RPG isometrico dark fantasy, ispirato ai classici hack & slash di fine anni '90.
Progetto personale in Unity (URP) / C#.

![Loot: lo scheletro cade e lascia la sua lama, il cavaliere la raccoglie, la equipaggia dall'inventario e il danno sale](docs/media/m4_loot.gif)

## Stato

**M4 — "Raccolgo roba"** chiusa il 4 ottobre 2026 (tag `m4`): attributi alla Diablo
(Forza, Destrezza, Magia, Vitalità) e una formula del colpo con colpi mancati e danno
variabile. Gli scheletri lasciano la loro lama, uno scudo aspetta nella prima stanza: un
click e il cavaliere li raccoglie. Inventario a griglia 10 × 4 con oggetti su più celle,
presi e posati con un click come in Diablo 1, slot di arma e scudo visibili in mano al
cavaliere, tooltip con danno e requisiti, pannello del personaggio che si aggiorna.
Prossima: **M5 — "Loot casuale"**.

Milestone precedenti:

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
- click sinistro su un nemico per colpirlo una volta; tenendo premuto, continua a colpirlo
- click sinistro sulla scala per scendere al livello successivo
- click sinistro su un oggetto a terra per raccoglierlo
- `I` inventario: un click prende un oggetto, un altro lo posa (o lo mette nello slot);
  con un oggetto preso, un click fuori dalle finestre lo lascia a terra
- `C` pannello del personaggio

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
