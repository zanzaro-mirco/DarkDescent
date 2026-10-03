# DarkDescent

[![Tests](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/zanzaro-mirco/DarkDescent/actions/workflows/tests.yml)

Action RPG isometrico dark fantasy, ispirato ai classici hack & slash di fine anni '90.
Progetto personale in Unity (URP) / C#.

![Combattimento: il cavaliere attacca uno scheletro, con lampo, numeri di danno e sfera della vita](docs/media/m2_combat.gif)

## Stato

**M2.5 — Pipeline automatica** chiusa il 3 ottobre 2026 (tag `m2.5`): test e build
Windows in CI con GitHub Actions e GameCI (vedi [CI](#ci)).
Prossima: **M3 — "Un dungeon fatto a mano"**.

**M2 — "Colpisco e muoio"** chiusa il 3 ottobre 2026 (tag `m2`): il primo gameplay
loop. Nella stanza di prova ci sono tre scheletri. Clicchi su uno scheletro e il
cavaliere si avvicina e lo colpisce; lo scheletro, quando ti vede, ti insegue e
risponde. La sfera rossa mostra la tua vita, e dalla schermata di morte ricominci.
Colpi con lampo bianco, hit stop, numeri di danno ed effetti sonori.

Milestone precedenti: **M1 — "Mi muovo"** (tag `m1`), click-to-move con NavMesh e
camera isometrica ([GIF](docs/media/m1_click_to_move.gif)).

**Comandi:**

- click sinistro sul pavimento per muoversi; tenendo premuto, il personaggio segue il cursore
- click sinistro su un nemico per colpirlo una volta; tenendo premuto, continua a colpirlo

## CI

Due workflow di GitHub Actions, con [GameCI](https://game.ci) e la licenza Unity Personal
(ADR-011…013):

- **[Tests](.github/workflows/tests.yml)**: test EditMode e PlayMode a ogni push su
  `main` e a ogni pull request. I risultati XML restano 14 giorni tra gli artifact del run.
- **[Build](.github/workflows/build.yml)**: build Windows sui tag di milestone (`m*`) e di
  versione (`v*`), o ad avvio manuale da *Actions → Build → Run workflow*. Prima rifà i
  test; la build si scarica dagli *Artifacts* del run (`DarkDescent-Windows-<tag>`, 30
  giorni). Eseguibile non firmato: al primo avvio Windows chiede conferma.

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
