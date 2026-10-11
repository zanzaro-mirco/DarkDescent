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

Tutte confermate da Mirco l'11 ottobre 2026, con le proposte.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Correzioni dalla prova della M8** | Con due anelli indossati, il tooltip di un anello nello zaino mostra **tutti e due** gli anelli indossati, uno a sinistra e uno a destra, con *Equipaggiato – anello 1* e *anello 2*. I **nomi lunghi** vanno su due righe divise allo spazio più vicino al centro, quando sono più larghi di 480 px di riferimento; il tooltip non supera quella larghezza | Le ha chieste Mirco nell'ultima prova. In Diablo 2 i nomi rari stanno su due righe: qui la divisione a metà tiene le righe simili |
| D2 | **Mana** | Mana massimo **10 + Magia × 2** (con la Magia di partenza 10 fa 30), **+1** a livello. Si rigenera del **2% al secondo**, anche in combattimento. Pozione di mana blu, come quella di cura, nelle stesse tabelle del loot e nella cintura | Il guerriero non è un mago: con 30 di mana lancia cinque o sei dardi. Chi alza la Magia cambia stile di gioco, ed è il motivo per farlo |
| D3 | **Tre incantesimi** | **Dardo di fuoco:** un proiettile verso il cursore, 6 di mana, danno 6–12 + Magia / 4. **Anello di fuoco:** colpisce tutti i nemici entro 4 m, 15 di mana, danno 8–14 + Magia / 3, ricarica 3 s. **Pelle di pietra:** +50% di Armatura per 20 s, 12 di mana, ricarica 30 s. Il danno degli incantesimi non usa la Forza e non può essere bloccato | Uno per tipo, come nel piano. I numeri partono dal danno della spada corta e si tarano provando |
| D4 | **Come si lanciano** | Il **tasto destro** lancia l'incantesimo scelto verso il cursore, come in Diablo 1. **F1–F3** scelgono l'incantesimo, o un click sui tre posti della barra accanto alla sfera del mana. Senza mana, o in ricarica, il posto si scurisce e il lancio non parte, con un suono sordo | Il tasto destro è libero e tiene la mano sul mouse. F1–F3 sono lontani da cintura e rotazione; F9 resta la lingua |
| D5 | **Pergamene** | Si comincia con il **Dardo di fuoco**. Anello di fuoco e Pelle di pietra si imparano dalle **pergamene**: un oggetto nuovo da 1 × 2 celle che cade raramente (circa 1 ogni 3 livelli dalla profondità 2), si legge con il click destro e sparisce. Una pergamena di un incantesimo già noto non cade più | Dà un motivo per esplorare e per raccogliere. Gli incantesimi non hanno livelli: il danno cresce con la Magia |
| D6 | **Nemico a distanza** | **Scheletro mago** (`Skeleton_Mage` con `Skeleton_Staff`): resta tra 6 e 10 m, lancia un dardo magico ogni 2 s (danno 4–8), arretra se il cavaliere si avvicina sotto i 4 m. Poca vita (12; 28 dopo la prova in build, 9.8). Nella cripta dalla profondità 3 e nelle caverne, 1–2 per livello nella `SpawnTable`. Esperienza 60 | Riusa i proiettili del cavaliere, come dice il piano. La balestra resta per un nemico futuro |
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

## Passo 9.0 — Correzioni dalla prova della M8

**Com'è andata (11 ott 2026).**

- **Due anelli.** L'inventario ha un secondo tooltip di confronto, copia del primo nella scena.
  Passando su un anello della griglia con due anelli addosso, si vedono *Equipaggiato – anello 1*
  e *anello 2* in fila dallo stesso lato: oggetto, primo anello, secondo anello. La prima prova li
  metteva uno per lato, ma l'inventario sta sul bordo destro e a destra non c'era posto: finivano
  uno sopra l'altro (lo ha mostrato lo screenshot, non il test). Con un anello solo il confronto
  non c'è, perché il nuovo andrebbe nello slot libero. `ShowBeside` sceglie il lato preferito e
  ripiega sull'altro se non c'è spazio.
- **Nomi lunghi.** Il tooltip misura il nome in grassetto con il suo font: oltre 480 px di
  riferimento lo fa dividere da `ItemDescription.SplitAtMiddle`, allo spazio più vicino al centro
  (*Lama dello scheletro* / *Precisa della Vitalità*). Con 420 si divideva anche *Anello Preciso
  della Vitalità*, che sta bene su una riga.

*Test.* `ItemDescriptionTests.LongName_SplitsAtMiddle` (EditMode): la divisione a metà in inglese e
in italiano, il nome senza spazi, il nome su una riga di base. In `InventoryUITests` (PlayMode):
due anelli confrontati in fila senza sovrapporsi e dentro lo schermo, il confronto che sparisce
con un anello solo; il nome lungo su due righe e il tooltip non più largo della soglia.

## Passo 9.1 — Mana e magia in Core

**Com'è andata (11 ott 2026).** Tutto in `Core/Magic`, logica pura:

- `ManaPool`: massimo, attuale, spesa, pozione, rigenerazione e riempimento. Annuncia il cambio
  solo quando cambia il valore intero o il massimo (trappola 7): sessanta frame di rigenerazione
  fanno un evento ogni punto di mana, non sessanta.
- `SpellDefinition`, un asset per incantesimo in `Data/Spells` con id, nome, tipo, costo,
  ricarica, danno (tiro più Magia / divisore) e i numeri del suo tipo: velocità e portata del
  proiettile, raggio dell'area, Armatura in più e durata del potenziamento. Dardo di fuoco,
  Anello di fuoco e Pelle di pietra con i numeri della D3; solo il Dardo è noto dall'inizio.
- `SpellBook`: gli incantesimi in ordine di posto (F1–F3), quelli noti e quello scelto; impara da
  una pergamena una volta sola, e si ripristina dagli id di un salvataggio.
- `SpellCaster`: un lancio parte se l'incantesimo è noto, la ricarica è finita e il mana basta;
  allora spende e avvia la ricarica. Altrimenti dice perché (`CastResult`), per scurire il posto.
- `CombatFormulas.MaxMana` (10 + 2 × Magia + 1 per livello oltre il primo + affissi) e
  `SpellDamage`, arrotondato a metà per eccesso: con l'arrotondamento al pari il Dardo con la
  Magia 10 faceva 8–14 invece di 9–15. `StatType.Mana` per il mana degli affissi, in fondo
  all'enum. Rigenerazione: il 2% del massimo al secondo.

*Test.* `ManaTests` (8, EditMode): mana massimo da Magia, livello e affissi; spesa, pozione e
rigenerazione; il massimo che cambia; un evento per punto intero; i numeri dei tre incantesimi;
libro che impara e sceglie; ripristino dal salvataggio; lancio rifiutato per incantesimo non
noto, ricarica o mana.

## Passo 9.2 — HUD e comandi

**Com'è andata (11 ott 2026).**

- **Comandi.** Azioni `Cast` (tasto destro) e `SelectSpell` (F1–F3) nell'asset dei comandi; il
  wrapper rigenerato con un avvio batch prima di scrivere il codice che le usa (trappola della
  M8). Il reader annuncia `CastPressed` e `SpellSelected`.
- **`PlayerMagic`** sul cavaliere, con una sorgente audio sua (figlio `Magic`, gruppo SFX): crea
  mana, libro e lancio, aggiorna il mana massimo quando cambia la scheda delle statistiche (Magia,
  livello, affissi), lo riempie salendo di livello e tornando in vita, rigenera e fa scorrere le
  ricariche in `Update`. Il tasto destro, come il click del movimento, si esegue nel primo
  `Update`, fuori dalla UI; il punto lo dà `PlayerController.TryGetCursorPoint`, con lo stesso
  raggio e la stessa tolleranza sui nemici del click. Un lancio annuncia `Cast(incantesimo, punto)`;
  uno rifiutato suona sordo (Kenney `impactSoft_heavy`) e annuncia `CastFailed`. Che cosa succede
  nel mondo arriva con i passi 9.3 e 9.4.
- **HUD.** La cintura è al centro in basso, con sopra la barra dell'esperienza; la sfera della vita
  è scesa nell'angolo; la sfera del mana è la sua gemella blu a destra (`ManaOrb`). A destra
  della cintura la barra dei tre incantesimi (`SpellBarView`, `SpellSlotView`): icona se noto,
  cornice d'oro se scelto, icona scura se il mana non basta, velo radiale che si ritira durante la
  ricarica, nome del tasto in alto. Un click su un posto lo sceglie. Si ridisegna quando si impara
  o si sceglie un incantesimo e quando il mana cambia di un punto; il velo segue la ricarica solo
  mentre c'è.
- **Icone** disegnate da uno script, pixel per pixel: palla di fuoco con la scia, anello di
  fiamme, scudo di pietra, e la cornice del posto scelto. Il primo screenshot ha mostrato posti
  invisibili nel buio (mancava il bordo `Outline` dei posti della cintura), icone piccole su un
  fondo scuro e un velo troppo nero: posti con lo stesso bordo, icone senza fondo e più grandi,
  velo al 55%.

*Test.* `MagicHudTests` (3, PlayMode): all'avvio sfera piena e solo il Dardo, scelto; il tasto
destro del mouse virtuale sul pavimento lancia verso il punto e spende 6 di mana; F2 non sceglie un
incantesimo non noto, imparato l'Anello lo sceglie, il lancio avvia il velo che sparisce dopo 3 s,
senza mana i posti si scuriscono e il lancio è rifiutato; salendo di livello il mana massimo sale
di 1 e si riempie, un punto di Magia lo alza di 2 senza riempire. 226 EditMode e 153 PlayMode verdi.

## Passo 9.3 — Proiettili e Dardo di fuoco

**Com'è andata (11 ott 2026).**

- **`Projectile`** vola dritto all'altezza del petto, senza rigidbody: a ogni frame un raggio a
  sfera (`SphereCastNonAlloc` con un buffer condiviso) cerca il primo bersaglio vivo sui livelli dei
  nemici, saltando chi l'ha lanciato; il danno è di tipo `Fire`, nuovo in `DamageType`. I muri li
  dice un `NavMesh.Raycast` a terra e non i collider, perché dalla M8 i muri alti dalla parte della
  camera sono spenti con i loro collider (trappola 6 corretta). A fine corsa o all'impatto la scia
  smette di emettere, il nucleo sparisce, parte lo scoppio con il suo suono e la luce si spegne in
  0,6 s; poi il proiettile torna al pool.
- **`ProjectilePools`** nella scena Core: un `ObjectPool` per prefab, al massimo 32, con la
  funzione di ritorno creata una volta per pool. Un proiettile ripreso pulisce scia, nucleo e
  scoppio e riaccende la luce (trappola 1).
- **`PlayerSpells`** sul cavaliere: al lancio si ferma, si gira verso il punto, fa il gesto
  (stato `Cast` nell'Animator, la clip Throw a velocità 2) e soffia; il dardo parte dopo 0,33 s,
  quando la mano arriva avanti, misurato su una striscia di fotogrammi del gesto. Il danno usa un
  flusso di numeri casuali suo.
- **Il Dardo**: prefab con nucleo, scia e scoppio di particelle additive su una texture a punto
  morbido, luce puntiforme arancione che illumina il pavimento al passaggio; suoni del lancio e
  dell'impatto sintetizzati da uno script.
- **Nemici colpiti da lontano**: uno scheletro fermo colpito si sveglia e insegue (`EnemyAI`
  ascolta `Damaged`). Prima un nemico svegliato solo dalla vista sarebbe rimasto a farsi bersagliare.
- Il primo controllo del danno dava 8–14 invece di 9–15: l'arrotondamento del banchiere di
  `Math.Round`; ora `SpellDamage` arrotonda lontano dallo zero.

*Test.* `ProjectileTests` (3, PlayMode): il dardo aspetta il gesto, parte, colpisce lo scheletro
con 9–15 di danno, lo sveglia e torna al pool; tirato contro un muro della cripta generata esplode
entro 0,6 m dal muro; due raffiche da dieci creano dieci proiettili, e trenta lanci dal pool caldo
non allocano un byte. Screenshot nella cripta: il dardo in volo illumina il pavimento, lo scheletro
lampeggia e mostra il 9. 226 EditMode e 156 PlayMode verdi.

## Passo 9.4 — Anello di fuoco, Pelle di pietra, pergamene e pozione di mana

**Com'è andata (11 ott 2026).**

- **`PlayerSpells`** lancia ora tutti e tre i tipi, sempre dopo il gesto. L'**Anello di fuoco**
  colpisce con `OverlapSphereNonAlloc` tutti i nemici vivi entro 4 m, una volta sola anche con più
  collider, ognuno con il suo tiro; il cavaliere non si gira. La **Pelle di pietra** mette sulla
  scheda un +50% di Armatura con l'incantesimo come sorgente e lo toglie alla fine dei 20 s, alla
  morte o spegnendo il cavaliere. Il tasto destro fuori dal pavimento lancia comunque anello e
  pelle, che partono dal cavaliere.
- **Effetti.** `AreaEffect`: un cerchio di fiamme che si allarga in 0,3 s; mentre si allarga le
  fiamme vivono poco e lasciano un'onda, poi una raffica sul bordo resta accesa 0,8 s, con un lampo
  di luce. `AuraEffect`: polvere grigio-azzurra che gira attorno al cavaliere, con una luce tenue,
  finché dura. Il primo screenshot ha mostrato un anello rado e un alone invisibile nel buio
  (particelle grigie in trasparenza): fiamme più fitte e più grandi, alone additivo come il fuoco,
  con particelle più grandi.
- **Pergamene.** `ScrollDefinition` (1 × 2, il rotolo di Quaternius già scaricato, con sigillo
  rosso o grigio) si legge con il click destro nell'inventario: insegna l'incantesimo, lo sceglie,
  suona e sparisce; una già nota resta nello zaino. `ScrollTable` tira **una pergamena per
  livello** dalla profondità 2, con probabilità 0,34 e un seme suo (`SeedMixer.ForScroll`); non
  cadono gli incantesimi già noti né quelli di cui c'è già una pergamena nello zaino. La porta uno
  dei nemici o delle casse del livello (`Carry`), che la lascia dall'altra parte della pozione.
- **Pozione di mana.** `PotionDefinition` ha un tipo, vita o mana; la blu è la bottiglia rossa con
  la texture ricolorata di blu. Rende metà del mana massimo, va nella cintura, a mana pieno non si
  beve. Nel loot sta **nello stesso tiro** della pozione di cura, nel tratto dopo: la pozione di
  cura cade dove cadeva (trappola 4). Scheletro 5%, sciame 2%, bruto 10%, cassa 15%.
- Suoni sintetizzati: vampata dell'anello, macina di pietra, fruscio e tre note della pergamena.

*Test.* `ScrollTests` (5, EditMode): pergamene della scheda nel database; niente alla profondità 1
e circa un livello su tre dalla 2, mai di un incantesimo noto; tiro stabile per seme; la pozione blu
nel tratto dopo quella rossa, circa il 5% degli scheletri; tooltip. `SpellTests` (5, PlayMode):
l'anello colpisce il nemico a 3 m con 11–18 e non quello a 6 m; la pelle porta l'Armatura da 20 a 30
e torna a 20 alla fine e alla morte; la pergamena insegna e sparisce, la seconda resta; la pozione
blu rende 15 di mana e a mana pieno resta; nella cripta alla profondità 2 uno solo porta la
pergamena e ucciso la lascia a terra. Quattro test vecchi contano ora solo la pozione di cura.
231 EditMode e 161 PlayMode verdi.

## Passo 9.5 — Scheletro mago

**Com'è andata (11 ott 2026).**

- **IA in `Core`.** Due stati nuovi: `RangedState` (troppo lontano, o con un muro in mezzo, si
  avvicina; entro 10 m con il tiro libero si ferma e lancia appena la ricarica lo permette; sotto
  i 4 m si allontana) e `RetreatState` (va verso un punto lontano dal cavaliere, poi torna a
  lanciare). Il tiro libero si ricontrolla ogni 0,25 s. Appena entrato nel combattimento a
  distanza il mago non scappa per 1,5 s: chiuso in un angolo resta e lancia invece di riprovare a
  ogni frame (trappola 5). Gli stati chiedono al corpo un'interfaccia in più, `IRangedBody`, così
  il corpo finto dei test della mischia non cambia. `IdleState` e `EnemyBrain.Alert` mandano il
  mago al combattimento a distanza invece che all'inseguimento. L'archetipo lo dice con
  `_castRange` maggiore di zero.
- **Il corpo.** `EnemyAI` fa anche da corpo a distanza: il tiro libero e i muri li dice il NavMesh,
  come per i proiettili; la fuga prova dritto all'indietro e poi sempre più di lato, a 7 m dal
  cavaliere, su un punto raggiungibile in linea retta e dentro il guinzaglio da casa.
  `RangedAttack` fa il gesto (lo stato `Cast` dell'animator del cavaliere, che gli scheletri
  condividono), si gira verso il cavaliere e dopo 0,4 s lancia il dardo verso dove il cavaliere è in
  quel momento: 9 m/s, più lento del Dardo di fuoco, per poterlo schivare. Un lancio ogni 2 s, danno
  4–8 di tipo `Magic`.
- **Dardi bloccabili.** `Projectile` riceve un generatore per il blocco: con quello, un bersaglio con
  lo scudo può parare come contro la mischia (stesso `ShieldBlock.TryBlock`, stesso evento). I dardi
  del cavaliere non lo passano e non si parano. I dardi del mago colpiscono solo il layer del
  cavaliere e attraversano gli altri nemici.
- **Modello.** `Skeleton_Mage` e `Skeleton_Staff` dal pacchetto Skeletons già scaricato. Gli
  screenshot hanno mostrato prima un bastone di 2 cm (copiavo la scala della lama, ma il bastone ha
  la scala 100 dell'FBX come l'ascia), poi orizzontale e infine con la testa in basso: rotazione
  dell'FBX ribaltata, testa trovata sui vertici. Una luce viola sulla testa del bastone fa
  riconoscere il mago nel buio. Dardo viola: il Dardo di fuoco ricolorato.
  *Correzione dopo il 9.6:* Mirco ha visto dallo screenshot che il mago teneva il bastone al
  contrario. Avevo giudicato l'orientamento dalla luce, piazzata male: il teschio stava dalla parte
  dell'impugnatura. Ora il bastone è verificato da vicino, da fermo, di lato e nel gesto: la
  rotazione è misurata nella posa di riposo perché il teschio stia in alto, un poco in avanti, con
  la luce sul teschio. Poi Mirco ha notato che il teschio guardava indietro: mezzo giro attorno al
  bastone. Trappola dei primi piani: l'animator dei nemici è in `CullUpdateTransforms`, e fuori
  dalla camera principale le ossa lette da un test sono vecchie; per misurare va messo
  `AlwaysAnimate`.
- **Numeri e spawn.** Vita 12, esperienza 60, vista a 10 m, `MageLoot` con più mana (oggetto 35%,
  cura 5%, mana 15%). Nella cripta 1–2 maghi dalla profondità 3 (`DungeonSettings.MagesAt`), nelle
  caverne dalla `SpawnTable` (1, 1–2, 1–2, 2 dalla 5 alla 8). In tutti e due i popolatori i maghi
  si piazzano **per ultimi**: i livelli e i semi di prima non cambiano (trappola 10 della M7). Il
  mago è anche tra i bersagli della riga Colpire del pannello.

*Test.* `MageStateTests` (7, EditMode): archetipo della scheda; visto il cavaliere si ferma e lancia,
e il gesto si finisce; lontano o dietro un muro si avvicina; troppo vicino scappa dopo la pausa; in
un angolo resta e lancia senza riprovare a ogni frame; avvisato passa al combattimento a distanza;
quanti maghi per profondità su 20 semi. `MageTests` (4, PlayMode): a 7 m resta e colpisce con un
dardo magico; a 2,5 m scappa oltre i 4 m; con lo scudo e il tiro basso il dardo è parato; nella
cripta alla 3 ci sono 1–2 maghi. Tre test vecchi contano ora anche i maghi. 238 EditMode e 165
PlayMode verdi.

## Passo 9.6 — Barra del nemico combattuto, Magia e Mana nel pannello, affissi nuovi

**Com'è andata (11 ott 2026).**

- **Barra del nemico combattuto (D9).** `EnemyBar` segue i nemici del livello come i numeri di
  danno (`Track` e `Untrack` dal CompositionRoot). Un colpo o uno schivato **del cavaliere** (la
  sorgente del danno è lui, di mischia o con un incantesimo) rende quel nemico il combattuto per
  3 s, rinnovati a ogni colpo; i colpi di altri non contano. Il cursore su un nemico vince sul
  combattuto; morto, sparisce. Il tempo lo conta una coroutine riavviata a ogni colpo, non un
  controllo a ogni frame.
- **Pannello (D12).** La riga **Mana** sotto Blocco mostra il mana massimo, con la sua spiegazione
  (formula, rigenerazione, pozioni blu). La spiegazione della **Magia** dice ora a cosa serve: 2 di
  mana a punto e il danno degli incantesimi. Due testi con le virgole sono tra virgolette nel CSV,
  che altrimenti li spezza in colonne.
- **Affissi.** `AffixEffect.Magic` e `Mana`, sulla scheda come `StatType.Magic` e `Mana`. Due
  affissi: **Arcano** (prefisso, +5–15 mana) e **della Stregoneria** (suffisso, +2–5 Magia, dal
  livello 2). Vanno solo su gioielli ed elmi: `AffixTargets.Helm` è nuovo, e gli elmi sono
  `Armor | Helm`, così gli affissi delle armature ci vanno ancora.
- Un bug di passaggio: una stringa C# scritta da uno script Python dentro un heredoc di Bash ha
  perso il `\n`, che è diventato un a capo vero; il compilatore l'ha fermato. Le stringhe con
  `\n` si scrivono con lo strumento di modifica, non con un heredoc.

*Test.* `MagicAffixTests` (2, EditMode): i due affissi solo su gioielli ed elmi, e un anello Arcano
della Stregoneria alza Magia e mana con il suo tooltip in inglese e in italiano.
`AimTests.LastHitEnemy_StaysOnTheBar` (PlayMode): senza cursore la barra mostra il nemico colpito dal
cavaliere, non quello colpito da altri, resta 3 s dall'ultimo colpo e sparisce alla morte. Tre test
vecchi aggiornati (fine del pannello, bersagli degli elmi, numero degli affissi). Screenshot del
pannello con i tooltip di Magia e Mana. 240 EditMode e 166 PlayMode verdi.

## Passo 9.7 — Formato 3 del salvataggio e migrazione

**Com'è andata (11 ott 2026).**

- **Prima il file vero.** Con il codice ancora del formato 2, uno script dell'editor ha scritto un
  salvataggio di una partita alla profondità 3 con gli oggetti della M9 (pergamena nello zaino,
  pozione blu nella cintura, elmo della Stregoneria, anello Arcano) e una mappa scoperta: è
  `Tests/EditMode/Fixtures/save_v2.json`, committato da solo prima di toccare il formato.
- **Formato 3 (D11).** `SaveData` aggiunge gli id degli incantesimi noti, il posto scelto e il
  mana; `FullMana` (−1) vuol dire «pieno». `SaveMigrator` porta il 2 al 3 con nessun incantesimo
  imparato (il Dardo è noto dall'inizio), il primo posto e il mana pieno; un file del formato 1
  passa dal 2 e poi dal 3. La migrazione mette i valori a mano: `JsonUtility` lascia ai campi che
  mancano nel file il default del tipo, non quello dell'inizializzatore.
- **Nel gioco.** `SaveGame` cattura libro e mana (da morto il mana si salva pieno, come la vita) e
  li rimette dopo attributi, livello e oggetti, così il mana massimo è già quello giusto.
  `SpellBook.Restore` annuncia `Learned` per i posti che diventano noti, e la barra degli
  incantesimi li mostra come dopo una pergamena.
- **Trappola 8.** All'avvio il salvataggio si applica nello `Start` del CompositionRoot, mentre il
  livello si carica in modo asincrono e si collega dopo: il libro è già quello salvato quando si
  tira la pergamena del livello, e non cade quella di un incantesimo già noto.

*Test.* `SaveMigrationTests` (EditMode): il file vero del formato 2 si carica con il codice del
formato 3 (solo il Dardo, primo posto, mana pieno; Magia, mappa, pergamena, pozione blu e affissi
della magia com'erano); il file del formato 1 arriva al 3; il formato 3 va e torna dal file.
`SaveLoadTests` (PlayMode, ampliati): la Pelle di pietra imparata e scelta e metà del mana spesa
tornano dopo il caricamento; all'avvio da un salvataggio con l'Anello di fuoco la barra lo mostra
scelto e il mana è quello salvato. 242 EditMode e 166 PlayMode verdi.

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
6. **Il raggio dei proiettili e i muri spenti:** dalla M8 i muri alti dalla parte della camera
   sono spenti con i loro collider; un proiettile non deve passarli. I muri li dice un raggio sul
   NavMesh, cotto con tutti i muri accesi (corretta al passo 9.3: i collider non bastano).
7. **La rigenerazione a ogni frame non deve allocare né annunciare:** il mana cambia di poco
   ogni frame; la sfera si aggiorna con un evento al massimo qualche volta al secondo, o
   quando il valore intero cambia.
8. **La pergamena del livello si tira collegando il livello:** se a quel punto il salvataggio non
   ha ancora rimesso gli incantesimi noti (passo 9.7), nel primo livello di una partita ripresa può
   cadere la pergamena di un incantesimo già noto. Il formato 3 deve rimettere il libro prima.
   Risolta al 9.7: il salvataggio si applica nello `Start`, prima che il livello asincrono si colleghi.

---

## Test

| Test | Cosa controlla |
|---|---|
| `TooltipCompareTests` (PlayMode) | Con due anelli, due tooltip di confronto; il nome lungo su due righe sotto la larghezza massima |
| `ManaTests` (EditMode) | Mana massimo da Magia e livello, rigenerazione, costo, ricarica, lancio rifiutato senza mana |
| `ProjectileTests` (PlayMode) | Il dardo colpisce il primo nemico sulla linea e torna al pool; si ferma su un muro; nessuna allocazione su trenta lanci dal pool caldo |
| `SpellTests` (PlayMode) | Anello di fuoco colpisce tutti entro 4 m e nessuno oltre; Pelle di pietra alza l'Armatura per 20 s; la pergamena insegna e sparisce; la pozione blu rende il mana; la pergamena del livello cade dal suo portatore |
| `MageStateTests` (EditMode), `MageTests` (PlayMode) | Il mago tiene la distanza, lancia, arretra, in un angolo resta; il dardo parato dallo scudo non fa danno; maghi per profondità |
| `ScrollTests` (EditMode) | Pergamene nel database, una ogni tre livelli circa dalla profondità 2, mai di un incantesimo noto; pozione di mana dopo quella di cura |
| `SaveMigrationTests` (EditMode, ampliato) | Il file vero del formato 2 si carica con il codice del formato 3 |
| `AimTests` (PlayMode, ampliato), `MagicAffixTests` (EditMode) | Senza cursore, la barra resta sull'ultimo colpito per 3 s; gli affissi della magia solo su gioielli ed elmi |

---

## Checklist di chiusura

- [x] Decisioni D1–D12 confermate
- [x] Correzioni dalla prova della M8
- [x] Mana, incantesimi e pergamene
- [x] Scheletro mago
- [x] Barra del nemico combattuto e HUD in basso
- [x] Formato 3 e migrazione
- [ ] Profiler: nessuna allocazione a ogni frame lanciando a raffica
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m9`
- [ ] Scheda della M10 scritta prima di cominciarla
