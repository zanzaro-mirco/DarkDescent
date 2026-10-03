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

---

## Passo 4.3 — Equipaggiamento

1. `Equipment` con i due slot e i modificatori sullo `StatSheet`.
2. `EquipmentVisuals`: il modello in mano cambia con l'oggetto, su `handslot.r` e
   `handslot.l`.
3. Il cavaliere parte con la spada corta equipaggiata.

**Test:** equipaggiare e poi togliere un oggetto riporta tutte le statistiche
**esattamente** ai valori di partenza; un'arma senza la Forza richiesta viene rifiutata;
cambiando arma, `MeleeAttack` usa quella nuova.

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

- [ ] Decisioni D1–D8 confermate
- [ ] Statistiche e formula del colpo, test esistenti verdi sulla sorgente fissa
- [ ] Tre oggetti con ID stabile, nel database, con le icone
- [ ] Modelli nuovi importati, riga in `CREDITS.md`
- [ ] Equipaggiamento con arma e scudo visibili sul cavaliere
- [ ] Drop, oggetti a terra e raccolta
- [ ] Inventario, pannello del personaggio e tooltip
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m4`
- [ ] Scheda della M5 scritta prima di cominciarla
