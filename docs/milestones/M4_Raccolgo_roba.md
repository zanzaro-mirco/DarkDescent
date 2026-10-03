# M4 — "Raccolgo roba"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI**, il
cavaliere uccide uno scheletro e lo scheletro lascia cadere la sua lama. Un click, e il
cavaliere la raccoglie. Con `I` si apre l'inventario a griglia: la lama occupa più celle e
il tooltip ne mostra danno e requisiti. Messa nello slot dell'arma, il cavaliere la
impugna e il danno nel pannello del personaggio (`C`) sale. Rimessa nell'inventario, tutto
torna esattamente com'era. Lo stesso vale per uno scudo trovato a terra nel livello 1. Test
verdi in CI.

**Tempo stimato:** 8–12 h (piano v2.10). **Prerequisito:** M3 chiusa (tag `m3`).

**Come si lavora:** come alla M3, il codice e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. A Mirco restano le decisioni qui sotto, il download dei modelli
(o il via perché lo faccia io), le prove in Play Mode e in build e la revisione degli ADR.

---

## Stato verificato il 4 ottobre 2026

| Cosa | Stato |
|---|---|
| Armi | `WeaponDefinition` (ScriptableObject) con danno **fisso**, tipo di danno, portata e tempi del colpo; due asset: `Sword` (10 di danno) e `SkeletonBlade` (5). `MeleeAttack` legge l'arma da un campo serializzato |
| Personaggi | Cavaliere 100 di vita, scheletro 30. Ogni colpo va a segno: nel combattimento non c'è niente di casuale. `HealthModel.Max` è in sola lettura |
| Modelli | Nel progetto ci sono `sword_1handed` (cavaliere) e `Skeleton_Blade` (scheletro), più il forziere del Dungeon Pack. Gli zip KayKit **non sono più** in `Downloads`. KayKit *Adventurers* 2.0 gratuito: 12 MB, CC0, "spade, scudi, asce, balestre, bastone, bacchetta e altro"; i nomi esatti dei file si vedono solo all'import. Il rig ha le ossa `handslot.r` e `handslot.l`, fatte per agganciare armi e scudi |
| HUD | uGUI, Canvas *Screen Space – Overlay*, *Canvas Scaler* a 1920×1080 con *match* 0,5, `InputSystemUIInputModule` |
| Input | Mappa `Gameplay` con le sole azioni `Move` (click) e `Point` |
| Test | 18 EditMode e 58 PlayMode verdi. Alcuni contano i colpi (lo scheletro muore al terzo): con colpi mancati e danno variabile diventerebbero casuali |

---

## Decisioni da prendere prima di cominciare

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Attributi e formula del colpo** (piano § 2) | Forza, Destrezza, Magia, Vitalità. Cavaliere: **30 / 20 / 10 / 25**, come il guerriero di Diablo. Vita = 50 + 2 × Vitalità = **100**, come oggi. Danno = tiro tra minimo e massimo dell'arma × (1 + Forza / 100). Probabilità di colpire = **75 + Destrezza / 2 − Armatura** del bersaglio, limitata tra 5 e 95. Gli scheletri non hanno attributi ma un blocco fisso: vita 30, Destrezza 10, Armatura 10 | Gli attributi del § 2 senza cambiare il bilanciamento della M2: con la spada corta (6–9) il cavaliere fa in media 9,75 a colpo, oggi 10. Colpisce lo scheletro 3 volte su 4, lo scheletro lui 4 su 5 (3 su 4 con lo scudo). Attributi per i nemici servirebbero solo alla M7, con i tipi nuovi |
| D2 | **Ordine dei modificatori** | Valore = (base + somma dei fissi) × (1 + somma delle percentuali / 100). Le percentuali **si sommano**, non si moltiplicano tra loro. Ogni modificatore ricorda da dove viene (l'oggetto), così si toglie tutto insieme | È l'ordine di Diablo e il più facile da leggere nel tooltip. Le percentuali moltiplicate tra loro crescono in modo esponenziale con gli affissi della M5 |
| D3 | **Casualità del combattimento** | I tiri (colpito o mancato, danno) passano da un'interfaccia `IRandomSource`, creata dal `CompositionRoot`. In build parte da un seme qualsiasi; nei test si usa una sorgente fissa | I test di oggi restano deterministici, e la M5 (seme riproducibile per il loot) trova la strada già fatta. `UnityEngine.Random` è globale e la condividono anche i suoni |
| D4 | **Oggetti** (piano § 4.4) | `ItemDefinition` (ScriptableObject astratto): ID stabile, nome, icona, celle occupate, modello a terra, slot. Sottotipi `WeaponDefinition`, che **assorbe** quella di oggi e aggiunge danno minimo e massimo, Forza richiesta e modello in mano, e `ArmorDefinition` (Armatura; per ora solo scudi). `ItemInstance` è una classe semplice con l'ID della definizione. `ItemDatabase` risolve ID → definizione | L'ID stabile serve per salvare l'inventario alla M8 (`JsonUtility` non sa salvare un riferimento a uno ScriptableObject). Assorbire `WeaponDefinition` evita due asset per la stessa spada |
| D5 | **Come si spostano gli oggetti nella UI** | **Come in Diablo 1:** un click prende l'oggetto sul cursore, un click lo posa; se lo spazio è occupato da un solo oggetto, i due si scambiano. Con un oggetto sul cursore, un click fuori dai pannelli lo lascia a terra ai piedi del cavaliere | Il piano diceva *drag & drop*. Il click-e-click è quello del gioco di riferimento, non obbliga a tenere premuto, rende naturale lo scambio, e nei test sono due click invece di un trascinamento |
| D6 | **Icone** | Fatte **dai modelli 3D**, con uno strumento di editor che resta nel repo (menu *DarkDescent → Rigenera le icone*): camera ortografica, sfondo trasparente, PNG in `Art/Icons/` | Stesso stile degli oggetti a terra, nessun asset nuovo da cercare e licenziare. Alla M5 e dopo, un oggetto nuovo ha l'icona con un click |
| D7 | **Modelli e oggetti della M4** | Tre oggetti. **Spada corta** (`sword_1handed`, 6–9, equipaggiata all'avvio). **Lama dello scheletro** (`Skeleton_Blade`, 8–12, Forza 25). **Scudo** (uno degli scudi di *Adventurers*, Armatura 5), a terra nel livello 1. Lo zip di *Adventurers* 2.0 (12 MB) lo riscarica Mirco, o lo scarico io con il suo via | Tre oggetti bastano per celle di forme diverse (1×3, 2×2), due slot e un confronto tra armi. Gli altri modelli si importano quando servono (M5) |
| D8 | **Slot, pannelli e drop** | Due slot: **arma** (mano destra) e **scudo** (mano sinistra), visibili sul cavaliere. Inventario **10 × 4** celle, come Diablo 1. `I` apre l'inventario nella metà destra dello schermo, `C` il personaggio in quella sinistra; il gioco **non** si mette in pausa. In questa milestone ogni scheletro lascia sempre la sua lama; lo scudo è messo dalla mappa (`@item`) | Elmo, armatura e anelli arrivano con il loot della M5: oggi non ci sarebbero oggetti per riempirli. Un drop fisso tiene la M4 deterministica; le tabelle di loot casuali sono il lavoro della M5 |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 4.1 | Statistiche, modificatori e formula del colpo | 1,5–2 |
| 4.2 | Definizioni degli oggetti, database e icone | 1–1,5 |
| 4.3 | Equipaggiamento: modello e oggetti in mano | 1–1,5 |
| 4.4 | Oggetti a terra, drop e raccolta | 1–1,5 |
| 4.5 | Inventario e pannello del personaggio | 2–3 |
| 4.6 | Tooltip | 0,5–1 |
| 4.7 | Chiusura: GIF, ADR, tag `m4` | 0,5 |

---

## Architettura

```
Core
├─ CompositionRoot      crea IRandomSource e ItemDatabase, collega pannelli e inventario
├─ Player
│  ├─ CharacterStats    guscio di StatSheet: attributi, modificatori, valori derivati
│  ├─ PlayerInventory   guscio di InventoryGrid + Equipment
│  └─ EquipmentVisuals  modelli dell'arma e dello scudo sulle ossa handslot
└─ HUD
   ├─ InventoryPanel    griglia 10×4, slot arma e scudo
   ├─ CharacterPanel    attributi, danno, probabilità di colpire, armatura, vita
   ├─ ItemCursor        l'oggetto preso, che segue il mouse
   └─ ItemTooltip

Livelli
├─ Skeleton             + LootDrop: alla morte lascia un GroundItem
└─ GroundItem           Interactable con il nome come etichetta e il modello dell'oggetto
```

### Contratti

- **`StatSheet`** (`DarkDescent.Stats`, logica pura): valori base, `AddModifier`,
  `RemoveModifiersFrom(object source)`, `Get(StatType)`, evento `Changed`.
  `StatModifier` è uno struct: statistica, tipo (fisso o percentuale), valore, sorgente.
- **`CombatFormulas`** (statico, puro): `HitChance(dexterity, armor)`,
  `RollDamage(min, max, strength, IRandomSource)`, `MaxLife(vitality)`.
- **`InventoryGrid`** (`DarkDescent.Items`, puro): `CanPlace`, `TryPlace(item, x, y)`,
  `TryAutoPlace`, `Remove`, `ItemAt(x, y)`, evento `Changed`.
- **`Equipment`** (puro): `Equip(item, slot)` restituisce l'oggetto che c'era, `Unequip`;
  aggiunge e toglie i modificatori sullo `StatSheet`. Rifiuta un'arma se la Forza non basta.
- **`MeleeAttack`**: legge l'arma equipaggiata invece del campo serializzato; senza arma,
  pugni (1–3). Gli scheletri continuano a usare la loro definizione.
- **`GroundItem`**: un click lo raggiunge (il ramo `Interactable` di `PlayerController`,
  come le scale) e lo mette nel primo posto libero. Se non c'è posto resta a terra, e
  l'etichetta dice "Inventario pieno".

---

## Passo 4.1 — Statistiche e formula del colpo

1. `StatSheet`, `StatModifier`, `CombatFormulas` e `IRandomSource` in logica pura, con i
   test EditMode.
2. `CharacterStats` sul cavaliere (attributi di D1); blocco fisso sugli scheletri.
3. `MeleeAttack` tira colpito o mancato e il danno. Un colpo mancato mostra "Mancato" al
   posto del numero e non fa scattare lampo, hit stop e interruzione.
4. I test PlayMode di oggi passano alla sorgente fissa, senza cambiare cosa verificano.

**Verifica:** a occhio, il combattimento ha lo stesso ritmo della M3, con qualche colpo
mancato. **Test:** ordine dei modificatori; percentuali sommate; limiti 5–95; tiri del danno
sempre tra minimo e massimo.

**Com'è andata (4 ott 2026).** Decisioni D1–D8 confermate da Mirco così come sono.
**Logica pura:** `StatSheet` in `DarkDescent.Stats` (valori base in un array indicizzato
dall'enum, modificatori in una lista, `Get` senza allocazioni), `StatModifier` con la
sorgente obbligatoria, `CombatFormulas` in `DarkDescent.Combat`, `IRandomSource` con
`SystemRandomSource` (seme) e `FixedRandomSource` (valori in ciclo, per i test) in
`DarkDescent.Core`. **Gusci:** `CharacterStats` sul cavaliere (30/20/10/25, vita dalla
Vitalità) e sullo scheletro (Destrezza 10, Armatura 10); `Health` prende la vita massima
dalla Vitalità se `CharacterStats` lo chiede, altrimenti resta la sua. **Armi:**
`WeaponDefinition` ha danno minimo e massimo interi al posto del danno fisso: spada 6–9,
lama dello scheletro 4–6 (in media 5, come prima). **`MeleeAttack`** tira al momento del
danno, con le statistiche del bersaglio prese quando lo sceglie (niente `GetComponent` a
ogni colpo); senza `IRandomSource` lancia un'eccezione invece di colpire a caso.
**"Mancato":** `IDamageable.Evade` e l'evento `Health.Evaded`, a cui si iscrivono i numeri di
danno con una scritta grigia; un colpo mancato non fa scattare lampo, hit stop e
interruzione, che stanno tutti sul danno. **`CompositionRoot`** crea la sorgente con un
seme preso dall'orologio e la passa al cavaliere e ai nemici di ogni livello;
`UseRandomSource` la cambia per tutti. **Test:** `SandboxFixture` passa a tiri fissi (sempre a
segno, danno minimo); tre test che leggevano il danno fisso ora lo calcolano dalla formula.
34 EditMode (`StatSheetTests`, `CombatFormulasTests`, `RandomSourceTests`) e 61 PlayMode
(`CombatStatsTests`: statistiche di cavaliere e scheletro, colpo mancato senza danno né hit
stop con la scritta, danno tirato) verdi.

---

## Passo 4.2 — Definizioni degli oggetti, database e icone

1. `ItemDefinition`, `WeaponDefinition` (spostata in `DarkDescent.Items`, con gli asset
   `Sword` e `SkeletonBlade` migrati senza perdere i tempi del colpo), `ArmorDefinition`,
   `ItemInstance`, `ItemDatabase`.
2. **Mirco** (o io, con il suo via): zip di KayKit *Adventurers* 2.0. Import dello scudo e
   riga in `CREDITS.md`.
3. ID generato alla creazione dell'asset; il test lo verifica.
4. Strumento delle icone e icone dei tre oggetti.

**Test:** ogni definizione ha un ID non vuoto e unico, ed è nel database; un
`ItemInstance` passato per `JsonUtility` e riletto risolve la stessa definizione.

**Com'è andata (4 ott 2026).** In `DarkDescent.Items`: `ItemDefinition` astratta (ID,
nome, icona, celle, modello, rotazione per l'icona; l'ID nasce in `Reset` e `OnValidate`
se manca), `WeaponDefinition` spostata qui con `git mv` (lo stesso GUID dello script,
quindi gli asset e i prefab non si accorgono di niente) e con la Forza richiesta,
`ArmorDefinition`, `ItemInstance` (ID serializzato, definizione ritrovata con `Resolve`),
`ItemDatabase` con un indice per ID ricavato dall'elenco. **Asset:** in `Data/Items` la
spada corta (l'asset della spada di prima, rinominato), la lama dello scheletro (8–12,
Forza 25) e lo scudo con stemma (`shield_badge`, Armatura 5, 2×2); il colpo dello
scheletro è passato in `Data/Attacks/SkeletonStrike`, fuori dal database: è un'arma, ma
non si raccoglie. **Scudo:** `shield_badge` dalla cartella `fbx(unity)` dello zip, con le
stesse impostazioni d'import della spada; usa la texture del cavaliere, già nel progetto.
Riga in `CREDITS.md` aggiornata. **`ItemTools`** in `DarkDescent.Editor`, menu
*DarkDescent → Oggetti*: *Aggiorna il database* (le definizioni di `Data/Items` in ordine
di nome) e *Rigenera le icone*, che rende ogni modello in una scena di anteprima, così non
tocca le scene aperte: camera ortografica, una luce principale e una di riempimento, 128
pixel per cella, sfondo trasparente, PNG in `Art/Icons` importati come sprite. Lo scudo
usciva di schiena, con la maniglia in vista: ha una rotazione di 180° nell'asset. Test: 40
EditMode (`ItemDatabaseTests`: ID unici anche tra i nemici, database uguale alla cartella,
oggetti completi, ricerca per ID, JSON con l'ID e ritorno, valori di D7) e 61 PlayMode
verdi.

---

## Passo 4.3 — Equipaggiamento

1. `Equipment` con i due slot e i modificatori sullo `StatSheet`.
2. `EquipmentVisuals`: il modello in mano cambia con l'oggetto, su `handslot.r` e
   `handslot.l`.
3. Il cavaliere parte con la spada corta equipaggiata.

**Test:** equipaggiare e poi togliere un oggetto riporta tutte le statistiche
**esattamente** ai valori di partenza; un'arma senza la Forza richiesta viene rifiutata;
cambiando arma, `MeleeAttack` usa quella nuova.

**Com'è andata (4 ott 2026).** `Equipment` (logica pura): uno slot per valore di
`EquipSlot`, `TryEquip` restituisce l'oggetto che c'era, `Unequip`, `MeetsRequirements`
(Forza attuale contro Forza richiesta) ed evento `Changed` con lo slot. Ogni oggetto mette i
suoi modificatori con sé stesso come sorgente: lo scudo dà Armatura fissa, le armi nessun
modificatore, perché il loro danno lo legge `MeleeAttack`. **`PlayerInventory`** sul
cavaliere crea l'equipaggiamento sullo `StatSheet`, equipaggia la spada corta in `Start`
(quando tutti gli iscritti ascoltano già) e tiene allineata l'arma di `MeleeAttack`
(`SetWeapon`); senza arma, i **pugni** (`Data/Attacks/Unarmed`, 1–3). La cartella dei colpi
che non si raccolgono ora si chiama `Data/Attacks`, con dentro anche quello dello scheletro.
**`EquipmentVisuals`** crea il modello dell'oggetto su `handslot.r` o `handslot.l` solo
quando lo slot cambia, gli copia i rendering layer del corpo (la luce di riempimento
illumina anche l'arma) e chiede a `HitFlash` di raccogliere di nuovo i renderer. `HitFlash`
ora salta i renderer distrutti, perché il modello vecchio sparisce prima del nuovo elenco.
Dal prefab è sparita la spada fissa in mano. Una foto da vicino ha confermato lo scudo sul
braccio sinistro con lo stemma verso l'esterno, senza bisogno di spostamenti. Test: 46
EditMode (`EquipmentTests`) e 65 PlayMode (`EquipmentPlayModeTests`: spada all'avvio,
cambio d'arma con modello e layer, scudo con Armatura avanti e indietro, pugni) verdi.

**Ritocco dopo la prova di Mirco.** Dalle foto lo scudo stava tra il braccio e il corpo,
con la mano che gli passava davanti. `ItemDefinition` ora ha posizione e rotazione "in mano",
applicate da `EquipmentVisuals` rispetto all'osso. Provati 0, 10, 20 e 30 cm verso
l'esterno (l'asse +Z di `handslot.l`, che punta alla sinistra del cavaliere): a 10 cm
spuntava ancora il braccio sul bordo, a 30 lo scudo si staccava dal corpo. Scelto 15 cm:
di lato e dalla camera di gioco la mano non si vede più. Il test dello scudo ora controlla
anche che il suo centro stia almeno 10 cm più in fuori della mano. 46 EditMode e 65
PlayMode verdi.

---

## Passo 4.4 — Oggetti a terra, drop e raccolta

1. Prefab `GroundItem`: modello dell'oggetto, `Interactable` con il nome come etichetta,
   evidenziazione al passaggio del mouse come per le scale.
2. `LootDrop` sullo scheletro: alla morte un `GroundItem` sul NavMesh vicino al corpo.
3. Direttiva `@item <colonna> <riga> <id>` nelle mappe; lo scudo nel livello 1.
4. Raccolta: click, il cavaliere lo raggiunge, l'oggetto va nell'inventario.

**Test:** uno scheletro morto lascia la lama; un click la raccoglie e la mette
nell'inventario; con l'inventario pieno resta a terra; cambiando livello gli oggetti a terra
del livello vecchio spariscono con lui.

**Com'è andata (4 ott 2026).** `InventoryGrid` (logica pura): occupazione cella per cella,
`TryPlace`, `TryPlaceOrSwap` (scambio solo con un oggetto sotto), `TryAutoPlace` colonna per
colonna come Diablo, `Remove`, evento `Changed`. **`GroundItem`** (prefab `GroundItem` sul
layer `Interactable`): mostra il modello disteso, centrato sull'oggetto e appoggiato al
pavimento (i modelli KayKit hanno il perno sull'impugnatura, e il click cadeva fuori),
ridimensiona il collider sul modello e ha una piccola luce calda, `Glow`, che triplica al
passaggio del mouse: con il materiale evidenziato della scala avrebbe preso la texture del
dungeon. `Spawn` lo mette sul NavMesh, con una rotazione ricavata dal punto. **Raccolta:**
`Interactable` ha ora `Use` e `Used`, e un'etichetta che può cambiare (`SetLabel`,
`LabelChanged`, che `InteractableLabel` ascolta per l'oggetto sotto il cursore).
`PlayerController` ricorda l'oggetto cliccato e lo usa quando arriva entro 1 m; ogni comando
nuovo lo dimentica. Le scale non cambiano: le porta giù il loro trigger. Con l'inventario
pieno l'etichetta diventa "… (inventario pieno)". **`LootDrop`** sullo scheletro: alla morte
la lama, 0,9 m davanti al corpo. **Mappe:** il marcatore `i` con la direttiva `@items` (gli
oggetti in ordine di lettura); `LevelTileset` porta il prefab, e il builder mette lo scudo
nella prima stanza del livello 1. Nei test gli oggetti lasciati dallo scheletro principale
finivano dietro il cubo della sandbox, come per la GIF della M2: si usa `Skeleton_B`.

---

## Passo 4.5 — Inventario e pannello del personaggio

1. Azioni `ToggleInventory` (`I`) e `ToggleCharacter` (`C`) nella mappa `Gameplay`.
2. `InventoryPanel`: griglia, slot arma e scudo, `ItemCursor` con il click-e-click di D5.
3. `CharacterPanel`: attributi, danno minimo–massimo, probabilità di colpire contro uno
   scheletro, Armatura, vita. Si aggiorna sull'evento `Changed`, mai in `Update`.
4. Un click su un pannello non muove mai il cavaliere.

**Verifica:** in Play Mode e in build, lo scenario della Definition of Done. **Test:** un
click su una cella prende l'oggetto e un altro lo posa; posato su un oggetto solo, li
scambia; un click sul pannello non muove il cavaliere; il pannello del personaggio cambia
quando cambia l'arma.

**Com'è andata (4 ott 2026).** `Inventory` (logica pura) tiene griglia, equipaggiamento e
oggetto sul cursore: `ClickCell` prende o posa centrando l'oggetto sulla cella e
spostandolo dentro i bordi, `ClickSlot` equipaggia (slot giusto e requisiti) o toglie,
`ReleaseHeld`, `TryStoreHeld`. `PlayerInventory` è il suo guscio: la griglia 10 × 4, la
spada di partenza, i pugni, `DropHeld` ai piedi del cavaliere e `PutAwayHeld` alla
chiusura. **Input:** azioni `ToggleInventory` (I) e `ToggleCharacter` (C); la classe
generata va rigenerata da Unity prima di usarle, quindi un import in batch in due tempi
(trappola della M1). **UI** costruita da uno script nell'HUD di `Core`, prima della
schermata di morte: `InventoryPanel` a destra (titolo, slot Arma e Scudo da 2 × 3 celle,
griglia da 56 pixel di riferimento per cella, immagini degli oggetti riusate),
`InventoryGridView` che trasforma il punto premuto nella cella, `EquipmentSlotView`,
`DropCatcher` (fondo trasparente a tutto schermo, acceso solo con un oggetto sul cursore:
il click lascia l'oggetto a terra e non arriva mai al mondo), `ItemCursor` (l'oggetto
segue il mouse, senza bersaglio dei raggi), `CharacterPanel` a sinistra con attributi,
vita, Armatura, danno e probabilità di colpire contro l'Armatura dello scheletro, scritti
con uno `StringBuilder` riusato. Tutto si aggiorna sugli eventi; l'unico lavoro per frame è
l'oggetto che segue il mouse, e solo quando c'è. Le pressioni sono `OnPointerDown`, non click:
prendere un oggetto è immediato come in Diablo. Il `CompositionRoot` collega pannelli e
cursore. Test: 59 EditMode (`InventoryGridTests`, `InventoryTests`) e 74 PlayMode
(`LootAndPickupTests`: drop sul NavMesh, raccolta con un click, inventario pieno, scudo del
livello 1 che resta lì scendendo; `InventoryUITests`: I e C, prendi e posa con il mouse
senza muovere il cavaliere, dalla griglia allo slot con il pannello che passa da 8–12 a
10–16, click fuori che lascia a terra, chiusura che rimette a posto) verdi.

---

## Passo 4.6 — Tooltip

1. Al passaggio del mouse su un oggetto (inventario o slot): nome, danno o Armatura,
   requisiti, con la Forza richiesta in rosso se non basta.
2. Il tooltip resta dentro lo schermo.

**Test:** il tooltip della lama mostra 8–12 e la Forza richiesta; sparisce quando il
cursore esce dalla cella.

---

## Passo 4.7 — Chiusura

1. GIF del README: lo scheletro lascia la lama, il cavaliere la raccoglie, la equipaggia e
   il danno sale.
2. ADR: formula del colpo e attributi (D1, D2), casualità iniettata (D3), oggetti con ID
   stabile (D4), click-e-click nell'inventario (D5), icone dai modelli (D6).
3. Lezioni nel piano, tabella dello stato, tag `m4`.

---

## Trappole note

1. **Un riferimento a uno ScriptableObject non si salva su file:** `JsonUtility` scrive un
   identificativo che cambia a ogni avvio. Le istanze tengono l'ID (piano § 4.4).
2. **Duplicare un asset copia anche l'ID:** due oggetti con lo stesso ID si scambierebbero
   nei salvataggi. Il test sugli ID unici serve proprio a questo.
3. **Mai stato di gioco in uno ScriptableObject:** l'inventario vive in `ItemInstance` e
   `InventoryGrid`, la definizione resta immutabile.
4. **I raycast della UI:** l'immagine dell'oggetto sul cursore deve avere *Raycast Target*
   spento, altrimenti intercetta il click destinato alla cella sotto. I pannelli invece lo
   devono avere acceso, altrimenti il click passa al mondo e il cavaliere si muove.
5. **I test che contano i colpi:** con colpi mancati e danno variabile diventano casuali.
   Vanno sulla sorgente fissa (D3), non si allentano le asserzioni.
6. **`Instantiate` mette gli oggetti nella scena attiva,** cioè nel livello (trappola 1
   della M3): gli oggetti a terra si scaricano con lui. È quello che si vuole, ma un
   oggetto a terra non va mai creato come figlio di qualcosa di `Core`.
7. **Un drop deve finire sul NavMesh:** vicino a un muro, il punto si cerca con
   `NavMesh.SamplePosition`, altrimenti l'oggetto resta dentro la roccia e non si raggiunge.
8. **`HealthModel.Max` è fissa:** in questa milestone nessun oggetto cambia la Vitalità.
   Quando alla M5 arriverà un affisso "+Vitalità", servirà cambiare la vita massima a
   runtime, e va deciso cosa succede alla vita corrente.
9. **Celle e risoluzione:** con il *Canvas Scaler* a 1920×1080 e *match* 0,5, le celle
   vanno provate anche in 16:10 e 4:3, dove l'inventario non deve uscire dallo schermo.
10. **I modelli agganciati alle mani vanno nel rendering layer `Player`** (ADR-016),
    altrimenti la luce di riempimento illumina il cavaliere ma non la sua arma. Il test
    della luce di riempimento lo controlla già su tutti i renderer del cavaliere.

---

## Test

| Classe | Cosa verifica |
|---|---|
| `StatSheetTests` (EditMode) | Fissi prima delle percentuali, percentuali sommate, modificatori tolti per sorgente |
| `CombatFormulasTests` (EditMode) | Probabilità di colpire limitata tra 5 e 95, danno sempre tra minimo e massimo, vita dalla Vitalità |
| `InventoryGridTests` (EditMode) | Posa, sovrapposizione, bordi, rimozione, primo posto libero per oggetti di forme diverse |
| `EquipmentTests` (EditMode) | Equipaggia e togli riporta le statistiche ai valori di partenza; Forza insufficiente |
| `ItemDatabaseTests` (EditMode) | ID non vuoti e unici, ogni definizione nel database, `ItemInstance` in JSON e ritorno |
| `LootAndPickupTests` | Drop della lama, raccolta, inventario pieno, oggetti a terra scaricati con il livello |
| `InventoryUITests` | Click-e-click, scambio, click sul pannello che non muove il cavaliere, pannello del personaggio aggiornato |
| `EquipmentPlayModeTests` | Arma nuova in mano e usata da `MeleeAttack` |

---

## Checklist di chiusura

- [x] Decisioni D1–D8 confermate
- [x] Statistiche e formula del colpo, test esistenti verdi sulla sorgente fissa
- [x] Tre oggetti con ID stabile, nel database, con le icone
- [x] Modelli nuovi importati, riga in `CREDITS.md`
- [x] Equipaggiamento con arma e scudo visibili sul cavaliere
- [x] Drop, oggetti a terra e raccolta
- [ ] Inventario, pannello del personaggio e tooltip (manca il tooltip)
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m4`
- [ ] Scheda della M5 scritta prima di cominciarla
