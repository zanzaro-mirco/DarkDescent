# DarkDescent

Action RPG isometrico dark fantasy, ispirato ai classici hack & slash di fine anni '90.
Progetto personale in Unity (URP) / C#.

![Click-to-move con NavMesh e camera isometrica](docs/media/m1_click_to_move.gif)

## Stato

**M1 — "Mi muovo"** chiusa il 1 ottobre 2026 (tag `m1`): una stanza di prova in cui
clicchi sul pavimento e il cavaliere ci corre aggirando gli ostacoli, con la camera
isometrica che lo segue. Prossima: **M2 — "Colpisco e muoio"**, il primo gameplay loop.

**Comandi:** click sinistro per muoversi; tenendo premuto, il personaggio segue il cursore.

## Documenti

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
