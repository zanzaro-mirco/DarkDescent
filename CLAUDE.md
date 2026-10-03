# Istruzioni di progetto per Claude

> Questo file viene letto automaticamente da Claude Code all'apertura del progetto.
> Definisce **come** si lavora qui. Vale più delle abitudini di default.

---

## Contesto

ARPG isometrico dark fantasy ispirato a Diablo 1. Unity LTS + URP + C#.
Progetto personale di **Mirco**: sviluppatore esperto in altri ambiti, **Unity da zero**.
Disponibilità: 6–10 h/settimana.

Il piano completo è in `docs/Piano_Sviluppo_ARPG.md`, versionato nel repo.
La milestone corrente e le schede operative sono in `docs/milestones/`.

## Obiettivo doppio

1. Finire un gioco giocabile.
2. **Un progetto che regga come materiale da portfolio.** Repo pubblico curato,
   ADR che spiegano le scelte, build giocabile.

---

## Come si lavora

**Claude scrive il codice, compreso quello di gameplay.** File completi e
funzionanti, non firme da riempire. Mirco li rivede, li prova e li integra.

Resta comunque richiesto, in forma breve e senza lezioni:

- **Perché** di una scelta architetturale, quando non è ovvia — finisce nell'ADR.
- **Trappole Unity** rilevanti per quel codice, segnalate mentre le si aggira.
- **Conseguenze a distanza:** se una scelta si paga in una milestone successiva,
  dirlo subito.
- **Code review vera** quando Mirco scrive o modifica del codice: cosa è corretto,
  cosa rifaresti, cosa si romperà tra tre milestone. Niente compiacenza.

**Niente parti didattiche.** Le schede e le risposte non assegnano a Mirco
esercizi, codice da scrivere o modificare a mano per imparare, letture di
approfondimento o esperimenti del tipo "cambia il valore e guarda cosa succede".
A mano restano solo i passaggi nell'editor che Claude non può fare (sezione in
fondo) e le decisioni che spettano a lui (design, ambito).

*(Fino al 30 set 2026 valeva il metodo "io spiego, tu scrivi", con Claude limitato
a concetti, architettura e sole firme. Rimosso su richiesta di Mirco, insieme a
esercizi e devlog.)*

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

**Gli ADR li scrive Claude** (dal 3 ott 2026, su richiesta di Mirco): una voce in
`DECISIONS.md` per ogni scelta tecnica non ovvia, al più tardi alla chiusura della
milestone in cui è stata presa. Mirco li rivede. *(Prima li scriveva Mirco, con
Claude che ricordava quali mancavano.)*

Il devlog non esiste più (dal 30 set 2026): non ricordarlo e non proporlo.
`DEVLOG.md` è stato cancellato; la voce della M0 resta nella storia git.

## Cosa NON puoi fare da terminale

Claude Code non può cliccare nell'editor Unity. I passaggi che richiedono la GUI
(creare scene, configurare l'Animator, bake del NavMesh, impostare l'Inspector,
Build Settings) vanno descritti **passo per passo**, non tentati.
