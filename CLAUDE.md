# Istruzioni di progetto per Claude

> Questo file viene letto automaticamente da Claude Code all'apertura del progetto.
> Definisce **come** si lavora qui. Vale più delle abitudini di default.

---

## Contesto

ARPG isometrico dark fantasy ispirato a Diablo 1. Unity LTS + URP + C#.
Progetto personale di **Mirco**: sviluppatore esperto in altri ambiti, **Unity da zero**.
Disponibilità: 6–10 h/settimana.

Il piano completo è in `Piano_Sviluppo_ARPG_v2.md` (cartella padre, `game_projects/`).
La milestone corrente e le schede operative sono in `docs/milestones/`.

## Obiettivo doppio

1. Finire un gioco giocabile.
2. **Un progetto che regga come materiale da portfolio.** Repo pubblico curato,
   ADR che spiegano le scelte, devlog, build giocabile.

---

## Come si lavora

**Claude scrive il codice, compreso quello di gameplay.** File completi e
funzionanti, non firme da riempire. Mirco li rivede, li prova e li integra.

Resta comunque richiesto, in forma breve e senza lezioni:

- **Perché** di una scelta architetturale, quando non è ovvia — serve a Mirco
  per scrivere l'ADR e il devlog.
- **Trappole Unity** rilevanti per quel codice, segnalate mentre le si aggira.
- **Conseguenze a distanza:** se una scelta si paga in una milestone successiva,
  dirlo subito.
- **Code review vera** quando Mirco scrive o modifica del codice: cosa è corretto,
  cosa rifaresti, cosa si romperà tra tre milestone. Niente compiacenza.

*(Fino al 30 set 2026 valeva il metodo "io spiego, tu scrivi", con Claude limitato
a concetti, architettura e sole firme. Rimosso su richiesta di Mirco.)*

---

## Regole di codice

Le convenzioni complete sono in `CONVENTIONS.md`. Le non negoziabili:

- **Mai campi `public`** su MonoBehaviour: `[SerializeField] private` + proprietà read-only.
- **Ogni `+=` su un evento ha il suo `-=`**: iscrizione in `OnEnable`, disiscrizione in `OnDisable`.
- **La UI non fa polling**: si iscrive a eventi, non legge in `Update`.
- **ScriptableObject = dati immutabili.** Lo stato runtime va in classi C# semplici.
- **Camera e logica di inseguimento in `LateUpdate`**, mai in `Update`.
- **Niente `GameObject.Find` / `FindObjectOfType`** fuori da `Awake`.
- **Niente allocazioni per-frame** in `Update` (no `new`, no LINQ, no stringhe concatenate).
- **Input System nuovo**, mai `Input.GetKey` / `Input.GetMouseButton` legacy.
- Namespace `DarkDescent.<Area>`.

## Ambito

Il gioco v1.0 è definito nella sezione 1 del piano. Se salta fuori un'idea
fuori scope, **va in `ICEBOX.md`**, non nel codice. Segnalalo esplicitamente
quando succede: il feature creep è il rischio numero uno del progetto.

## Documentazione viva

A ogni sessione significativa, ricordagli di aggiornare:
- `DEVLOG.md` — cosa fatto, cosa rotto, cosa capito
- `DECISIONS.md` — una ADR per ogni scelta tecnica non ovvia

Non scrivere tu queste voci al posto suo, ma aiutalo a formularle se te lo chiede.

## Cosa NON puoi fare da terminale

Claude Code non può cliccare nell'editor Unity. I passaggi che richiedono la GUI
(creare scene, configurare l'Animator, bake del NavMesh, impostare l'Inspector,
Build Settings) vanno descritti **passo per passo**, non tentati.
