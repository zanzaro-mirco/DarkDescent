# DarkDescent

Action RPG isometrico dark fantasy, ispirato ai classici hack & slash di fine anni '90.
Progetto personale in Unity (URP) / C#.

![Combattimento: il cavaliere attacca uno scheletro, con lampo, numeri di danno e sfera della vita](docs/media/m2_combat.gif)

## Stato

**M2 — "Colpisco e muoio"** chiusa il 3 ottobre 2026 (tag `m2`): il primo gameplay
loop. Nella stanza di prova ci sono tre scheletri. Clicchi su uno scheletro e il
cavaliere si avvicina e lo colpisce; lo scheletro, quando ti vede, ti insegue e
risponde. La sfera rossa mostra la tua vita, e dalla schermata di morte ricominci.
Colpi con lampo bianco, hit stop, numeri di danno ed effetti sonori.
Prossima: **M2.5 — Pipeline automatica** (test e build in CI).

Milestone precedenti: **M1 — "Mi muovo"** (tag `m1`), click-to-move con NavMesh e
camera isometrica ([GIF](docs/media/m1_click_to_move.gif)).

**Comandi:**

- click sinistro sul pavimento per muoversi; tenendo premuto, il personaggio segue il cursore
- click sinistro su un nemico per colpirlo una volta; tenendo premuto, continua a colpirlo

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
