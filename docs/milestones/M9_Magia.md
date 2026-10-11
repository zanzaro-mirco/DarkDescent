# M9 — "Magia"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI**, la **sfera
blu del mana** sta in basso a destra e si svuota mentre il cavaliere lancia dalla barra degli
incantesimi un **proiettile**, un incantesimo **ad area** e un **potenziamento**. Gli
incantesimi oltre al primo si imparano dalle **pergamene** trovate nel dungeon. Uno
**scheletro mago** tiene le distanze e lancia dardi; il cavaliere li evita spostandosi o li
para con lo scudo. La barra in alto resta sul nemico che si sta combattendo anche senza il
cursore sopra. Lanciando proiettili a raffica, il Profiler non mostra allocazioni a ogni
frame. Il salvataggio passa al **formato 3** (incantesimi e mana) e un salvataggio del
formato 2 si carica. Test verdi in CI.

**Tempo stimato:** 10–13 h (piano v2.27). **Prerequisito:** M8 chiusa (tag `m8`).

**Come si lavora:** come alla M8, il codice e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. A Mirco restano le decisioni qui sotto, le prove in build e la
revisione degli ADR. Dopo la prima build da provare c'è già un passo per le correzioni
(lezione della M7 e della M8).

---

## Stato verificato l'11 ottobre 2026

| Cosa | Stato |
|---|---|
| Statistiche | `StatType.Magic` c'è dalla M4 e si alza con i punti (M8), ma non serve a niente: il tooltip dice "servirà agli incantesimi" |
| HUD | Sfera della vita in basso a sinistra, cintura delle pozioni sotto di lei, barra dell'esperienza in basso al centro. A destra in basso non c'è niente |
| Input | Tasto sinistro per muoversi e attaccare; `1`–`8` cintura; `I`, `C`, `M`, `F9`; rotella zoom; `Q`/`E` rotazione. Il **tasto destro** nel mondo è libero (nell'inventario beve una pozione) |
| Combattimento | `MeleeAttack` con colpo a portata, `Health`, `DamageInfo` con `DamageType.Physical`. Blocco con lo scudo (`ShieldBlock`) contro i colpi di mischia. Hit stop, numeri di danno, critici |
| Nemici | Scheletro, sciame e bruto, IA a stati in `Core` (ADR-042), archetipi come asset. Nessun nemico a distanza |
| Modelli | Nel pacchetto KayKit *Skeletons* già scaricato ci sono `Skeleton_Mage` e `Skeleton_Staff`, e anche `Skeleton_Crossbow` con le frecce. Non sono ancora nel progetto |
| Pool | Nessun pool di oggetti di gioco: i numeri di danno usano un pool loro |
| Salvataggio | Formato 2 (ADR-062), con il migratore 1 → 2 e il file del formato 1 congelato nei test |
| Barra del nemico | Mostra solo il nemico sotto il cursore (ADR-049) |
| Test | 217 EditMode e 148 PlayMode verdi |

---

## Decisioni

Da confermare con Mirco prima di cominciare.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Correzioni dalla prova della M8** | Con due anelli indossati, il tooltip di un anello nello zaino mostra **tutti e due** gli anelli indossati, uno a sinistra e uno a destra, con *Equipaggiato – anello 1* e *anello 2*. I **nomi lunghi** vanno su due righe divise allo spazio più vicino al centro, quando sono più larghi di circa 420 px; il tooltip non supera quella larghezza | Le ha chieste Mirco nell'ultima prova. In Diablo 2 i nomi rari stanno su due righe: qui la divisione a metà tiene le righe simili |
| D2 | **Mana** | Mana massimo **10 + Magia × 2** (con la Magia di partenza 10 fa 30), **+1** a livello. Si rigenera del **2% al secondo**, anche in combattimento. Pozione di mana blu, come quella di cura, nelle stesse tabelle del loot e nella cintura | Il guerriero non è un mago: con 30 di mana lancia cinque o sei dardi. Chi alza la Magia cambia stile di gioco, ed è il motivo per farlo |
| D3 | **Tre incantesimi** | **Dardo di fuoco:** un proiettile verso il cursore, 6 di mana, danno 6–12 + Magia / 4. **Anello di fuoco:** colpisce tutti i nemici entro 4 m, 15 di mana, danno 8–14 + Magia / 3, ricarica 3 s. **Pelle di pietra:** +50% di Armatura per 20 s, 12 di mana, ricarica 30 s. Il danno degli incantesimi non usa la Forza e non può essere bloccato | Uno per tipo, come nel piano. I numeri partono dal danno della spada corta e si tarano provando |
| D4 | **Come si lanciano** | Il **tasto destro** lancia l'incantesimo scelto verso il cursore, come in Diablo 1. **F1–F3** scelgono l'incantesimo, o un click sui tre posti della barra accanto alla sfera del mana. Senza mana, o in ricarica, il posto si scurisce e il lancio non parte, con un suono sordo | Il tasto destro è libero e tiene la mano sul mouse. F1–F3 sono lontani da cintura e rotazione; F9 resta la lingua |
| D5 | **Pergamene** | Si comincia con il **Dardo di fuoco**. Anello di fuoco e Pelle di pietra si imparano dalle **pergamene**: un oggetto nuovo da 1 × 2 celle che cade raramente (circa 1 ogni 3 livelli dalla profondità 2), si legge con il click destro e sparisce. Una pergamena di un incantesimo già noto non cade più | Dà un motivo per esplorare e per raccogliere. Gli incantesimi non hanno livelli: il danno cresce con la Magia |
| D6 | **Nemico a distanza** | **Scheletro mago** (`Skeleton_Mage` con `Skeleton_Staff`): resta tra 6 e 10 m, lancia un dardo magico ogni 2 s (danno 4–8), arretra se il cavaliere si avvicina sotto i 4 m. Poca vita (12). Nella cripta dalla profondità 3 e nelle caverne, 1–2 per livello nella `SpawnTable`. Esperienza 60 | Riusa i proiettili del cavaliere, come dice il piano. La balestra resta per un nemico futuro |
| D7 | **Proiettili** | Un `Projectile` con velocità, portata e danno, presi da un **`UnityEngine.Pool.ObjectPool<T>`** per tipo. Volano dritti all'altezza del petto e si fermano sul primo nemico o muro (raggio di sfera, non fisica). I dardi dei maghi **si possono bloccare** con lo scudo, come i colpi di mischia; quelli del cavaliere no | Il pool è nel piano; il raggio evita i rigidbody e le collisioni perse ad alta velocità |
| D8 | **Effetti** | Sistema particellare di Unity con texture fatte da uno script (punto morbido, scintilla), una luce che segue il proiettile e una piccola esplosione all'impatto. Anello di fuoco: un cerchio di fiamme che si allarga in 0,3 s. Pelle di pietra: un alone grigio attorno al cavaliere finché dura. Suoni Kenney già scaricati, o sintetizzati | Niente pacchetti nuovi da scaricare; le luci colorate nel buio della cripta bastano a leggere gli incantesimi |
| D9 | **La barra del nemico combattuto** | Come nel piano v2.22: senza nemico sotto il cursore, la barra in alto mostra quello colpito per ultimo, da mischia o da incantesimo, per **3 s** dopo l'ultimo colpo | Con i proiettili si colpisce senza il cursore sopra |
| D10 | **HUD in basso** | **Sfera del mana** in basso a destra, simmetrica a quella della vita. Al centro, dal basso: la **cintura**, sopra la **barra dell'esperienza**, e a destra della cintura i **tre posti degli incantesimi** | La cintura al centro sotto l'esperienza l'ha chiesta Mirco (v2.26); la sistemo una volta sola, insieme agli incantesimi |
| D11 | **Formato 3 del salvataggio** | Il salvataggio aggiunge gli incantesimi conosciuti, quello scelto e il mana attuale. `SaveMigrator` porta il 2 al 3 con il solo Dardo di fuoco e il mana pieno. Prima di cambiarlo si congela un file vero del formato 2 accanto a quello del formato 1 | Come per la M8 (ADR-062): il test della migrazione usa un file vero |
| D12 | **Magia nel pannello** | La riga **Magia** spiega mana e danno degli incantesimi. Sotto **Blocco** una riga **Mana**. Gli affissi possono dare **+Magia** e **+mana** (nuovi, su gioielli ed elmi) | Oggi alzare la Magia non serve a niente: il pannello deve dire perché farlo |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 9.0 | Correzioni dalla prova della M8: confronto con due anelli, nomi lunghi su due righe (D1) | 1 |
| 9.1 | Mana e magia in `Core`: mana massimo, rigenerazione, costi, ricariche, incantesimi conosciuti (D2, D3, D5), con i test | 1 |
| 9.2 | HUD: sfera del mana, cintura al centro, barra degli incantesimi, F1–F3 e tasto destro (D4, D10) | 1–1,5 |
| 9.3 | Proiettili con il pool, Dardo di fuoco, effetti (D7, D8) | 1,5–2 |
| 9.4 | Anello di fuoco e Pelle di pietra, pergamene e pozione di mana nel loot (D3, D5, D2) | 1,5–2 |
| 9.5 | Scheletro mago: modello, IA a distanza, dardi bloccabili, spawn (D6) | 1,5–2 |
| 9.6 | Barra del nemico combattuto, Magia e mana nel pannello, affissi nuovi (D9, D12) | 0,5–1 |
| 9.7 | Formato 3 del salvataggio e migrazione (D11) | 0,5–1 |
| 9.8 | Build da provare e correzioni | 1 |
| 9.9 | Chiusura: Profiler, GIF, ADR, lezioni, tag `m9`, scheda della M10 | 0,5 |

---

## Trappole note

1. **Un pool restituisce oggetti sporchi:** un proiettile ripreso dal pool ha ancora la scia e
   la luce di prima. `OnGet` deve azzerare particelle, luce e tempo di volo; `OnRelease` deve
   spegnere tutto, o il primo fotogramma mostra il proiettile dove è morto.
2. **Il tasto destro c'è già nell'inventario:** sopra una pozione beve. Il lancio deve ignorare
   i click sopra la UI, come fa già il movimento con il sinistro.
3. **I tiri nuovi spostano quelli dopo** (trappola 12 della M7): danno e critico degli
   incantesimi vogliono un flusso di numeri casuali loro, non quello della mischia.
4. **Un oggetto nuovo nel loot cambia i semi provati** (trappola 10 della M7): pergamene e
   pozione di mana vanno in fondo alle tabelle, e i test che cercano un seme dicono cosa vogliono.
5. **Il mago che scappa può uscire dal NavMesh** o finire in un angolo: la fuga sceglie un punto
   campionato sul NavMesh lontano dal cavaliere, e se non c'è resta e lancia.
6. **Il raggio dei proiettili e i muri bassi:** dalla M8 i muri bassi sono accesi e spenti con la
   camera; un proiettile non deve passare un muro alto spento. Il raggio guarda i collider dei
   muri alti, che restano sempre (il NavMesh è cotto con quelli).
7. **La rigenerazione a ogni frame non deve allocare né annunciare:** il mana cambia di poco
   ogni frame; la sfera si aggiorna con un evento al massimo qualche volta al secondo, o
   quando il valore intero cambia.

---

## Test

| Test | Cosa controlla |
|---|---|
| `TooltipCompareTests` (PlayMode) | Con due anelli, due tooltip di confronto; il nome lungo su due righe sotto la larghezza massima |
| `ManaTests` (EditMode) | Mana massimo da Magia e livello, rigenerazione, costo, ricarica, lancio rifiutato senza mana |
| `ProjectileTests` (PlayMode) | Il dardo colpisce il primo nemico sulla linea e torna al pool; si ferma su un muro; nessuna allocazione su cento lanci |
| `SpellTests` (PlayMode) | Anello di fuoco colpisce tutti entro 4 m e nessuno oltre; Pelle di pietra alza l'Armatura per 20 s |
| `SkeletonMageTests` (PlayMode) | Il mago tiene la distanza, lancia, arretra; il dardo parato dallo scudo non fa danno |
| `ScrollTests` (EditMode e PlayMode) | La pergamena insegna l'incantesimo e sparisce; una già nota non cade più |
| `SaveMigrationTests` (EditMode, ampliato) | Il file vero del formato 2 si carica con il codice del formato 3 |
| `EnemyBarTests` (PlayMode, ampliato) | Senza cursore, la barra resta sull'ultimo colpito per 3 s |

---

## Checklist di chiusura

- [ ] Decisioni D1–D12 confermate
- [ ] Correzioni dalla prova della M8
- [ ] Mana, incantesimi e pergamene
- [ ] Scheletro mago
- [ ] Barra del nemico combattuto e HUD in basso
- [ ] Formato 3 e migrazione
- [ ] Profiler: nessuna allocazione a ogni frame lanciando a raffica
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m9`
- [ ] Scheda della M10 scritta prima di cominciarla
