# M6 — "Dungeon infinito"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI** i livelli
1–4 sono cripte **generate a ogni partita**: si attraversano di fila dall'ingresso alla
scala, e nessuna stanza resta irraggiungibile. Più si scende, più scheletri e casse ci
sono; una cassa si apre con un click e lascia un oggetto. Avviata con `-seed 4711`, la
partita genera **lo stesso dungeon** due volte di fila, con gli stessi nemici e gli stessi
oggetti. Nell'editor una finestra genera e disegna un livello per seme e profondità senza
entrare in Play Mode. Test verdi in CI.

**Tempo stimato:** 10–13 h (piano v2.13). **Prerequisito:** M5 chiusa (tag `m5`).

**Come si lavora:** come alla M5, il codice e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. A Mirco restano le decisioni qui sotto, le prove in Play Mode e
in build e la revisione degli ADR.

**Punto di controllo:** nessuno alla chiusura di questa milestone (piano § 1.3: il prossimo
si fissa alla chiusura della M8).

---

## Stato verificato il 5 ottobre 2026

| Cosa | Stato |
|---|---|
| Livelli | Due, fatti a mano: `Level_01` (23 × 13 celle da 4 m, cinque stanze, quattro scheletri) e `Level_02` (15 × 11, due stanze, due scheletri, nessuna uscita). Mappe di testo in `Levels/`, con le direttive `@depth`, `@entrance`, `@exit` e `@items` |
| Costruzione | `LevelMapBuilder` (460 righe, assembly `DarkDescent.Editor`) costruisce una scena per mappa: pavimenti, muri alti a nord ed est e bassi a sud e ovest (ADR-017), marcatori, atmosfera (ADR-016), NavMesh. Usa `PrefabUtility`, `SerializedObject` ed `EditorSceneManager`: **gira solo nell'editor** |
| Griglia | `LevelMap` è logica pura e si legge già dal testo; il commento della classe dice che alla M6 il generatore produrrà la stessa griglia. Marcatori in uso: `<` ingresso, `>` scala, `T` torcia, `S` scheletro, `i` oggetto, `b` barile, `x` casse di legno, `p` pilastro, `c` cassa (solo scenografia) |
| NavMesh | Cotto nell'editor e salvato come asset accanto a ogni scena (`NavMeshSurface`, geometria dalle mesh renderizzate). I modelli KayKit sono importati **senza Read/Write** (`isReadable: 0`) |
| Caricamento | `LevelManager` carica per **nome di scena** in modo additivo, con dissolvenza; l'uscita porta il nome della scena e dell'ingresso di destinazione. `LevelContext` raccoglie ingressi, uscite e nemici della scena nel suo `Awake` |
| Seme | Seme di partita da `-seed` o dall'orologio (ADR-029). Il riavvio dopo la morte ricarica `Core`, quindi senza `-seed` sceglie un seme nuovo. I drop dipendono dalla cella del nemico, presa in `Awake` |
| Profondità | `LevelContext.Depth` dalla direttiva `@depth`: è il livello degli oggetti che cadono |
| Assembly | Uno solo per il gioco (`DarkDescent`, ADR-003), più `DarkDescent.Editor` e i due dei test |
| Test | 98 EditMode e 89 PlayMode verdi. Tredici PlayMode, in sette classi, caricano `Core`, che parte da `Level_01` (discesa, uscita, loot, seme, luci, torce, lingua) |

---

## Decisioni

Tutte confermate da Mirco il 5 ottobre 2026.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Quali livelli si generano** | Tutti e quattro i livelli della cripta. `Level_01` e `Level_02` escono dal percorso del gioco ma **restano nel repo** come livelli di prova: i test che hanno bisogno di una stanza nota continuano a usarli, e il loro builder è lo stesso del generatore (D3) | Un primo livello sempre uguale si impara a memoria alla seconda partita. Tenere le mappe a mano costa poco e dà ai test un terreno che non cambia |
| D2 | **Algoritmo** | **BSP** sulla griglia di celle da 4 m: il rettangolo del livello (circa **28 × 28 celle**) si divide finché i pezzi sono piccoli, in ogni foglia una stanza da 3 × 3 a 7 × 7 celle, poi corridoi larghi una cella tra le stanze sorelle, risalendo l'albero. Ingresso nella stanza di partenza; scala nella stanza **più lontana** dall'ingresso, contando le celle con una visita in ampiezza. Torce sui muri alti, una ogni tante celle di muro | È l'algoritmo del piano. Collegando le sorelle, il livello è connesso per costruzione: il flood fill lo verifica senza doverlo aggiustare. La scala lontana obbliga ad attraversare il livello |
| D3 | **Un builder solo, a runtime** | La costruzione esce da `LevelMapBuilder` e diventa `LevelBuilder`, nell'assembly del gioco: prende una `LevelMap` e istanzia i moduli con `Instantiate`. Lo usano il generatore a runtime e lo strumento dell'editor per le mappe a mano | Due builder divergerebbero alla prima correzione. Con uno solo, un livello generato e uno a mano sono identici per luci, muri, click e NavMesh, e quello che è stato provato alla M3 vale anche qui |
| D4 | **NavMesh o A\*** (il piano chiede di decidere con i dati) | **NavMesh a runtime**, con `NavMeshSurface.BuildNavMesh()` dopo la costruzione. Al passo 6.4 si **misura** il tempo del bake in build: se supera **1 s** dietro la dissolvenza, o se i corridoi larghi una cella bloccano gli scheletri, si torna qui con i numeri | Movimento del cavaliere, inseguimento, evitamento tra scheletri e click sul pavimento si basano tutti sul `NavMeshAgent` dalla M1. Un A\* nostro darebbe controllo sui costi, ma vorrebbe anche un movimento e un evitamento nostri: settimane, senza un problema da risolvere |
| D5 | **Da cosa si cuoce il NavMesh** | Dai **collider** dei moduli (`PhysicsColliders`), non dalle mesh renderizzate. I collider ci sono già, perché il click usa il raggio | In build una mesh senza Read/Write non si può leggere dalla CPU, e il NavMesh verrebbe vuoto: nell'editor funziona, in build no. Abilitare Read/Write su tutti i modelli raddoppia la loro memoria |
| D6 | **Contenuto per profondità** | Scheletri: **3 + 2 × profondità** (5 al livello 1, 11 al livello 4), mai nella stanza d'ingresso, a gruppi di 1–3 per stanza. Casse: **1 + profondità / 2** (1, 2, 2, 3). Barili, casse di legno e pilastri come scenografia, qualcuno per stanza. Numeri in uno ScriptableObject `DungeonSettings`, per ritararli senza codice | Il primo livello resta morbido, il quarto è pieno. Con 5–11 scheletri per livello i rari della M5 escono più spesso: lo si guarda in build e si ritara alla M10 |
| D7 | **Casse** | Una cassa è un'`Interactable`: con un click il cavaliere ci va, la cassa si apre (il coperchio ruota, se il modello lo ha separato, altrimenti cambia modello) con un suono e lascia **un oggetto** da una `LootTable` sua: cade sempre, e la rarità si tira come per i nemici. Seme della cassa ricavato come quello dei nemici, dalla cella | Riusa interazione, loot table e seme della M5. Una cassa che dà sempre qualcosa è il premio di una stanza in fondo al corridoio |
| D8 | **Seme del livello** | `SeedMixer.ForLevel(seme della partita, profondità)`: lo stesso seme dà lo stesso dungeon, livello per livello, anche tornando a giocare dopo una morte con `-seed`. Il generatore usa `SplitMix64Source` (ADR-029) | Lo stesso principio della M5: un seme per cosa, ricavato e non condiviso, così il loot non cambia la mappa e la mappa non cambia il loot |
| D9 | **Come si carica un livello generato** | Una scena `Level_Crypt`, costruita una volta dallo strumento dell'editor, con l'atmosfera, il volume del post-processing e un `DungeonLevel` che genera e costruisce all'avvio. Il `LevelManager` la carica come oggi, con una **profondità**: l'uscita del livello N porta a `Level_Crypt` con profondità N + 1. Il livello 4 per ora **non ha la scala** (le caverne arrivano alla M7) | Il piano § 4.3 prevedeva proprio "la scena vuota in cui il generatore costruisce il dungeon". Atmosfera e luci restano impostazioni di scena, come alla M3 |
| D10 | **Finestra dell'editor** | *DarkDescent → Generatore di dungeon*, in **UI Toolkit**: seme, profondità, pulsanti "genera" e "seme successivo", la mappa disegnata a celle colorate (pavimento, muro, ingresso, scala, nemici, casse) e i numeri del livello (stanze, celle, distanza tra ingresso e scala). Un pulsante salva la mappa generata in `Levels/` come testo | Si vedono cento livelli in un minuto, senza Play Mode. Salvare un livello generato come mappa trasforma un caso strano in un livello di prova per i test |
| D11 | **Divisione dell'asmdef** (piano § 4.5) | **Rimandata.** Al passo 6.7 scrivo il grafo delle dipendenze tra le cartelle di `Scripts/` e, se non ci sono cicli da rompere, divido solo la logica pura (`DarkDescent.Core`: griglia, generatore, semi, formule) da quella con i `MonoBehaviour`. Se ci sono cicli, resta un assembly e l'ADR dice perché | Oggi un assembly compila in pochi secondi. Dividere per area vuol dire rompere le dipendenze incrociate tra le cartelle, se ci sono, con interfacce che servono solo alla divisione. La logica pura è già separata (piano § 4.1): spostarla in un assembly suo costa poco e garantisce che non tocchi Unity |
| D12 | **Riavvio dopo la morte** | Resta com'è: un seme nuovo, quindi un dungeon nuovo, a meno di `-seed` | È il comportamento di Diablo, e con `-seed` si rigioca lo stesso dungeon per provare |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 6.1 | Builder a runtime: `LevelBuilder` nel gioco, le mappe a mano ricostruite uguali | 1,5–2 |
| 6.2 | Generatore BSP (logica pura) e test sui 500 semi | 2–2,5 |
| 6.3 | Contenuto: ingresso, scala, torce, nemici, casse e scenografia per profondità | 1–1,5 |
| 6.4 | Scena `Level_Crypt`, NavMesh a runtime, `LevelManager` con la profondità, misura del bake | 2 |
| 6.5 | Casse che si aprono, con loot table e suono | 1–1,5 |
| 6.6 | Finestra dell'editor | 1–1,5 |
| 6.7 | Dipendenze tra le cartelle e asmdef | 0,5 |
| 6.8 | Chiusura: build da provare, GIF, ADR, tag `m6` | 0,5 |

---

## Passo 6.1 — Builder a runtime

1. `LevelBuilder` (runtime, `DarkDescent.Levels`): pavimenti, muri, marcatori, atmosfera,
   superficie del NavMesh. I campi che oggi lo script di editor scrive con
   `SerializedObject` passano da metodi `Configure` dei componenti (ingresso, uscita,
   contesto).
2. `LevelMapBuilder` resta nell'editor e fa solo il lavoro dell'editor: crea la scena, chiama
   `LevelBuilder`, cuoce il NavMesh, salva.
3. Ricostruiti `Level_01` e `Level_02`: un test confronta il numero di oggetti per tipo con
   quello di prima (pavimenti, muri, torce, nemici), e i PlayMode esistenti restano verdi.

**Com'è andata (5 ott 2026).** `LevelBuilder` sta in `Scripts/Levels/` e riceve tre funzioni:
come istanziare un prefab (l'editor tiene il legame con il prefab, a runtime basta
`Instantiate`), come trovare un oggetto di `@items` e chi avvisare quando cambia un campo di
un'istanza di prefab (l'editor lo registra, o al salvataggio si perde). I campi che lo script
di editor scriveva con `SerializedObject` passano da `Configure` su `LevelContext`,
`LevelEntrance`, `LevelExit`, `Interactable`, `InteractableHighlight` e `GroundItem`. Tutto nasce
sotto la radice del livello spenta, che si accende alla fine: a runtime gli `Awake` partono a
livello finito, e il contesto trova ingressi, uscite e nemici (trappola 1). `LevelMapBuilder`
è sceso da 460 a un centinaio di righe: crea la scena, chiama il builder, cuoce e salva il
NavMesh. L'assembly del gioco ora referenzia `Unity.AI.Navigation`. **Verifica:** prima del
refactor uno script usa e getta ha descritto le due scene oggetto per oggetto (nome, layer,
posizione, rotazione, prefab, ogni campo serializzato); ricostruite con il builder nuovo, la
descrizione è identica riga per riga. Le scene nel repo restano quelle di prima, perché
ricostruirle cambia solo gli identificativi interni. **Test nuovo** (`LevelBuilderTests`): il
livello 1 costruito a runtime ha la stessa firma di quello dell'editor (gruppi, posizioni,
contesto, uscita, ingresso), l'oggetto a terra, la scala che si accende sotto il cursore e un
NavMesh cotto a runtime sotto l'ingresso. Il test ha trovato subito la trappola 2: dalle mesh
renderizzate Unity segnala che in build il NavMesh verrebbe vuoto, quindi cuoce dai collider
come dice D5; il builder passerà ai collider al passo 6.4, con la misura. 98 EditMode e 90
PlayMode verdi.

## Passo 6.2 — Generatore

1. `DungeonGenerator` (logica pura): seme, profondità e `DungeonSettings` in ingresso,
   `LevelMap` in uscita, con i marcatori.
2. BSP con dimensione minima delle foglie, stanze dentro le foglie con un margine di roccia,
   corridoi a L tra i centri delle stanze sorelle.
3. Ingresso e scala con una visita in ampiezza dalla stanza di partenza.
4. Test su **500 semi**: ogni cella di pavimento raggiungibile dall'ingresso, nessuna stanza
   sovrapposta a un'altra né a contatto, scala ad almeno metà della distanza massima, mappa
   dentro i bordi, stesso seme stessa mappa.

**Com'è andata (5 ott 2026).** `DungeonGenerator` (logica pura, solo interi) prende un
`DungeonSettings` e un seme e restituisce un `DungeonLayout`: griglia, stanze, stanza di
partenza e di arrivo, ingresso e scala. Il layout accetta marcatori su celle di pavimento
libere (`TryPlace`, per il passo 6.3) e diventa una `LevelMap` con le direttive, attraverso il
nuovo `LevelMap.FromCells`, che ora usa anche la lettura del testo. Il BSP divide il
rettangolo dentro il bordo di roccia finché una zona supera 11 celle, mai sotto 6; ogni zona
ha una stanza da 3 a 7 celle per lato con almeno una cella di roccia attorno, quindi due
stanze non si toccano mai. Risalendo, i due rami di ogni divisione si collegano con un
corridoio a L tra le loro stanze più vicine. La scala va nella stanza più lontana a piedi,
sulla riga nord, con la roccia oltre (lo stendardo) e il pavimento davanti (da dove si
arriva): tra le celle candidate si scarta quella che taglierebbe fuori un pezzo di livello.
`SeedMixer` è passato da `Items` a `Core`, con `ForLevel`: il seme di un livello non coincide
mai con quello di un nemico. Su 500 semi le cripte hanno da 9 a 16 stanze. Il seme 4711:

```
############################
##......####################
##.............#############
##......##.....######....###
##########.....######....###
############...........<.###
#####################....###
#######################.####
#######################.####
#############....##.......##
#############....##.......##
######.>.####....##.......##
######...####.............##
######...####....##.......##
######...####....##.......##
######...######.############
######...######.############
#######.#######.####....####
#######.#######.####....####
#####......###...###....####
#####...................####
#####......###...###....####
####################....####
############################
```

(senza le righe di sola roccia in alto e in basso). Test: `DungeonGeneratorTests`, cinque
test su 500 semi in meno di mezzo secondo in tutto. Il primo giro ne impiegava cinque e mezzo:
il messaggio di errore con la mappa intera si componeva a ogni asserzione, anche quando
passava (trappola 9). 103 EditMode e 90 PlayMode verdi.

## Passo 6.3 — Contenuto

1. Nemici e casse dai numeri di D6, scelti tra le celle delle stanze con il seme del livello.
2. Scenografia contro i muri, mai nei corridoi larghi una cella (bloccherebbe il passaggio:
   un test lo controlla con il flood fill che considera bloccate le celle occupate).
3. Torce sui muri alti, distanziate.

**Com'è andata (5 ott 2026).** `DungeonPopulator` mette il contenuto come marcatori della
mappa, gli stessi delle mappe a mano (`T`, `S`, `c`, `b`, `x`, `p`), con lo stesso seme del
generatore subito dopo la pianta: `Generate(seme, profondità)` restituisce la cripta già
piena. I numeri sono in `DungeonSettings`: scheletri 3 + 2 × profondità (5, 7, 9, 11) a gruppi
da 1 a 3 in stanze diverse da quella d'ingresso; casse 1 + profondità / 2 (1, 2, 2, 3), una
per stanza; da 0 a 2 tra barili, casse di legno e pilastri per stanza. **Torce:** ogni stanza
ne ha almeno una, sulla riga nord con la roccia oltre (o sul muro est, se a nord passano solo
corridoi), più una ogni 4 celle di larghezza, distribuite. **Ostacoli** (casse e scenografia)
solo sul bordo delle stanze, mai su una cella che dà su un corridoio né accanto; dopo ogni posa
una visita in ampiezza controlla che dall'ingresso si arrivi ancora dappertutto, altrimenti
l'ostacolo si toglie. Restano sempre libere la cella davanti alla scala, le due ai suoi lati
(le balaustre) e le quattro attorno all'ingresso: nelle prime mappe stampate c'erano una
cassa di legno attaccata alla scala e un barile addosso al punto in cui compare il cavaliere.
Test: `DungeonContentTests` su 200 semi per ognuna delle quattro profondità (numeri di D6,
nessuno scheletro nella stanza d'ingresso, nessun passaggio chiuso, una torcia per stanza su
un muro alto, ostacoli contro i muri e lontani da scala e ingresso, stesso seme stesso
contenuto). 108 EditMode e 90 PlayMode verdi.

## Passo 6.4 — Livelli generati nel gioco

1. Scena `Level_Crypt` costruita dallo strumento dell'editor (atmosfera, volume, `DungeonLevel`).
2. `DungeonLevel.Awake`: genera, costruisce, cuoce il NavMesh dai collider, **poi** fa
   raccogliere a `LevelContext` ingressi, uscite e nemici (trappola 1).
3. `LevelManager.LoadLevel` con la profondità; l'uscita del livello N porta al livello N + 1.
4. Misura del tempo di generazione e bake in build, scritta nel log; D4 si chiude con quel
   numero.

## Passo 6.5 — Casse

1. `Chest`: interazione, apertura una volta sola, suono di Kenney (da `Downloads`, se c'è,
   altrimenti chiedo il via al download), `LootTable` delle casse, seme dalla cella.
2. Etichetta "Chest"/"Cassa" nella tabella delle stringhe.
3. Test PlayMode: click sulla cassa, apertura, oggetto a terra, un secondo click non fa
   niente.

## Passo 6.6 — Finestra dell'editor

1. `DungeonGeneratorWindow` in UI Toolkit, nell'assembly dell'editor.
2. Disegno della mappa in una texture, una cella per pixel ingrandita.
3. Salvataggio della mappa in `Levels/`.

## Passo 6.7 — Dipendenze e asmdef

1. Grafo delle dipendenze tra le cartelle di `Scripts/`, dagli `using` e dai tipi usati.
2. Decisione secondo D11, con l'ADR.

## Passo 6.8 — Chiusura

1. Build della CI da provare: quattro livelli di fila, stesso dungeon con `-seed 4711`.
2. GIF del README: la discesa attraverso due livelli generati, con una cassa aperta.
3. ADR: builder unico, BSP, NavMesh a runtime dai collider, contenuto per profondità, seme
   del livello, asmdef. Lezioni nel piano, tabella dello stato, tag `m6`.

---

## Trappole note

1. **`LevelContext` raccoglie nel suo `Awake`:** se il livello viene costruito dopo, trova
   una scena vuota. La generazione deve finire prima, o il contesto deve raccogliere su
   richiesta.
2. **Mesh senza Read/Write:** un NavMesh cotto a runtime dalle mesh renderizzate funziona
   nell'editor e viene vuoto in build. Per questo D5 usa i collider.
3. **Le celle dei nemici in `Awake`** (ADR-029): un nemico istanziato e poi spostato avrebbe
   la cella sbagliata e un drop diverso. Va istanziato già nella sua posizione.
4. **`Instantiate` finisce nella scena attiva:** durante la costruzione la scena attiva deve
   essere quella del livello, altrimenti i moduli restano in `Core` e sopravvivono al cambio
   di livello (trappola 1 della M3).
5. **Corridoi larghi una cella e agent:** il NavMesh lascia mezzo metro dai muri (raggio
   dell'agent), quindi un corridoio di una cella si stringe di un metro buono. Due scheletri che si incrociano
   possono bloccarsi: si guarda nel passo 6.4.
6. **Generare durante la dissolvenza:** il lavoro di un fotogramma lungo blocca anche la
   dissolvenza. Lo schermo deve essere già nero quando parte la generazione.
7. **Molti moduli istanziati:** un livello di 28 × 28 celle sono circa mille oggetti tra
   pavimenti e muri. Il batching statico a runtime vuole mesh leggibili: si misura con il
   Profiler prima di fare qualcosa.
8. **Lo stesso seme in build Web:** il generatore non deve usare niente che dipenda dalla
   piattaforma (`System.Random`, ordine dei `Dictionary`, `float` accumulati). Celle intere e
   `SplitMix64Source`.
9. **Il test sui 500 semi va tenuto veloce:** solo logica pura, niente scene, o l'EditMode
   passa da un secondo a minuti.
10. **I test che partono da `Core` cambiano terreno:** con D1 `Core` parte da un livello
    generato. Quelli che cercano una stanza o uno scheletro precisi devono chiedere
    `Level_01` esplicitamente; gli altri possono restare sul livello generato, con un seme
    fisso.

---

## Test

| Classe | Cosa verifica |
|---|---|
| `DungeonGeneratorTests` (EditMode) | 500 semi: connesso, stanze separate, scala lontana, dentro i bordi, stesso seme stessa mappa |
| `DungeonContentTests` (EditMode) | Nemici e casse per profondità, mai nella stanza d'ingresso, scenografia che non chiude i corridoi |
| `LevelBuilderTests` (PlayMode) | I livelli a mano ricostruiti uguali a prima; un livello generato ha ingresso, scala, NavMesh e nemici sul NavMesh |
| `DungeonDescentTests` (PlayMode) | Dal livello 1 al 4 generati, con lo stesso seme lo stesso livello |
| `ChestTests` (PlayMode) | Apertura, oggetto a terra, una volta sola, stesso seme stesso oggetto |

---

## Checklist di chiusura

- [x] Decisioni D1–D12 confermate
- [x] Builder a runtime, mappe a mano ricostruite uguali
- [x] Generatore con i test sui 500 semi
- [x] Contenuto per profondità
- [ ] Livelli generati nel gioco, NavMesh a runtime misurato
- [ ] Casse
- [ ] Finestra dell'editor
- [ ] Dipendenze e asmdef
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m6`
- [ ] Scheda della M7 scritta prima di cominciarla
