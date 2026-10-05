# M6 — "Dungeon infinito"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI** i livelli
1–4 sono cripte **generate a ogni partita**: si attraversano di fila dall'ingresso alla
scala, e nessuna stanza resta irraggiungibile. Più si scende, più scheletri e casse ci
sono; una cassa si apre con un click e lascia un oggetto. Avviata con `-seed 4711`, la
partita genera **lo stesso dungeon** due volte di fila, con gli stessi nemici e gli stessi
oggetti. Le **pozioni** cadono da scheletri e casse, vanno nella cintura e si bevono con i
tasti 1–8. L'**automappa** si scopre camminando; il tasto `M` la passa dall'angolo alla vista
sovrapposta al gioco, poi la spegne, poi di nuovo nell'angolo. Morendo si riparte dall'ingresso del livello, con l'inventario
di quando ci si era entrati. Passando sulle statistiche del pannello del personaggio, un
tooltip dice a cosa servono. Nell'editor una finestra genera e disegna un livello per seme e
profondità senza entrare in Play Mode. Test verdi in CI.

**Tempo stimato:** 16–19 h (piano v2.14: 10–13 h, più pozioni, automappa, ripartenza e
tooltip delle statistiche chiesti da Mirco dopo la prova della build del 5 ottobre). **Prerequisito:** M5 chiusa (tag `m5`).

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
| D4 | **NavMesh o A\*** (il piano chiede di decidere con i dati) | **Chiusa il 5 ott 2026 con i numeri della build: 12–44 ms a livello, NavMesh compreso (passo 6.4).** **NavMesh a runtime**, con `NavMeshSurface.BuildNavMesh()` dopo la costruzione. Al passo 6.4 si **misura** il tempo del bake in build: se supera **1 s** dietro la dissolvenza, o se i corridoi larghi una cella bloccano gli scheletri, si torna qui con i numeri | Movimento del cavaliere, inseguimento, evitamento tra scheletri e click sul pavimento si basano tutti sul `NavMeshAgent` dalla M1. Un A\* nostro darebbe controllo sui costi, ma vorrebbe anche un movimento e un evitamento nostri: settimane, senza un problema da risolvere |
| D5 | **Da cosa si cuoce il NavMesh** | Dai **collider** dei moduli (`PhysicsColliders`), non dalle mesh renderizzate. I collider ci sono già, perché il click usa il raggio | In build una mesh senza Read/Write non si può leggere dalla CPU, e il NavMesh verrebbe vuoto: nell'editor funziona, in build no. Abilitare Read/Write su tutti i modelli raddoppia la loro memoria |
| D6 | **Contenuto per profondità** | Scheletri: **3 + 2 × profondità** (5 al livello 1, 11 al livello 4), mai nella stanza d'ingresso, a gruppi di 1–3 per stanza. Casse: **1 + profondità / 2** (1, 2, 2, 3). Barili, casse di legno e pilastri come scenografia, qualcuno per stanza. Numeri in uno ScriptableObject `DungeonSettings`, per ritararli senza codice | Il primo livello resta morbido, il quarto è pieno. Con 5–11 scheletri per livello i rari della M5 escono più spesso: lo si guarda in build e si ritara alla M10 |
| D7 | **Casse** | Una cassa è un'`Interactable`: con un click il cavaliere ci va, la cassa si apre (il coperchio ruota, se il modello lo ha separato, altrimenti cambia modello) con un suono e lascia **un oggetto** da una `LootTable` sua: cade sempre, e la rarità si tira come per i nemici. Seme della cassa ricavato come quello dei nemici, dalla cella | Riusa interazione, loot table e seme della M5. Una cassa che dà sempre qualcosa è il premio di una stanza in fondo al corridoio |
| D8 | **Seme del livello** | `SeedMixer.ForLevel(seme della partita, profondità)`: lo stesso seme dà lo stesso dungeon, livello per livello, anche tornando a giocare dopo una morte con `-seed`. Il generatore usa `SplitMix64Source` (ADR-029) | Lo stesso principio della M5: un seme per cosa, ricavato e non condiviso, così il loot non cambia la mappa e la mappa non cambia il loot |
| D9 | **Come si carica un livello generato** | Una scena `Level_Crypt`, costruita una volta dallo strumento dell'editor, con l'atmosfera, il volume del post-processing e un `DungeonLevel` che genera e costruisce all'avvio. Il `LevelManager` la carica come oggi, con una **profondità**: l'uscita del livello N porta a `Level_Crypt` con profondità N + 1. Il livello 4 per ora **non ha la scala** (le caverne arrivano alla M7) | Il piano § 4.3 prevedeva proprio "la scena vuota in cui il generatore costruisce il dungeon". Atmosfera e luci restano impostazioni di scena, come alla M3 |
| D10 | **Finestra dell'editor** | *DarkDescent → Generatore di dungeon*, in **UI Toolkit**: seme, profondità, pulsanti "genera" e "seme successivo", la mappa disegnata a celle colorate (pavimento, muro, ingresso, scala, nemici, casse) e i numeri del livello (stanze, celle, distanza tra ingresso e scala). Un pulsante salva la mappa generata in `Levels/` come testo | Si vedono cento livelli in un minuto, senza Play Mode. Salvare un livello generato come mappa trasforma un caso strano in un livello di prova per i test |
| D11 | **Divisione dell'asmdef** (piano § 4.5) | **Decisa al passo 6.11: a strati, `DarkDescent.Core` e `DarkDescent` (ADR-032).** **Rimandata.** Al passo 6.11 scrivo il grafo delle dipendenze tra le cartelle di `Scripts/` e, se non ci sono cicli da rompere, divido solo la logica pura (`DarkDescent.Core`: griglia, generatore, semi, formule) da quella con i `MonoBehaviour`. Se ci sono cicli, resta un assembly e l'ADR dice perché | Oggi un assembly compila in pochi secondi. Dividere per area vuol dire rompere le dipendenze incrociate tra le cartelle, se ci sono, con interfacce che servono solo alla divisione. La logica pura è già separata (piano § 4.1): spostarla in un assembly suo costa poco e garantisce che non tocchi Unity |
| D12 | **Riavvio dopo la morte** | ~~Resta com'è: un seme nuovo, quindi un dungeon nuovo, a meno di `-seed`~~ Sostituita da D13 | ~~È il comportamento di Diablo, e con `-seed` si rigioca lo stesso dungeon per provare~~ Provando la build, Mirco ha chiesto da dove si ricomincia: dal livello 1 di una cripta nuova, senza niente |

### Decisioni nate dalla prova della build (5 ottobre 2026)

Provata la build di `45cfc6d`, Mirco ha chiesto una mappa per orientarsi, le pozioni per
arrivare in fondo e una regola per la morte, poi un tooltip sulle statistiche. Confermate da
Mirco lo stesso giorno; per la mappa ha chiesto entrambe le viste, da cambiare con un tasto.

| # | Decisione | Proposta confermata | Perché |
|---|---|---|---|
| D13 | **Morte, fino alla M8** | "Ricomincia" riporta all'**ingresso del livello in cui si è morti**, nella stessa cripta (stesso seme), con nemici e casse rimessi, la vita piena, l'**inventario com'era entrando** nel livello e la **mappa scoperta** che resta (aggiunta di Mirco al passo 6.9): un'istantanea in JSON (il formato di `ItemInstance`, ADR-022) presa all'ingresso. Alla M8 le regole definitive, con il salvataggio | Ricominciare da capo senza niente toglie la voglia di scendere. L'istantanea evita di morire apposta per riaprire la stessa cassa |
| D14 | **Pozioni e cintura** | **Pozione di cura** da 1 × 1: rende il **50% della vita massima**, subito, e la vita non si rigenera da sola, come in Diablo 1. **Cintura** da 8 posti sotto la sfera della vita, **tasti 1–8**; una pozione raccolta va nella cintura se c'è posto, altrimenti nell'inventario, dove si beve con il **click destro**. Cadono dal **25%** degli scheletri e dal **50%** delle casse, con tiri a parte e un seme loro, senza cambiare gli oggetti già legati al seme. Si parte con **2 pozioni** nella cintura. Mana alla M9, mercante alla M10 | Senza cure quattro livelli non si finiscono. La cintura con i tasti numerici è il gesto di Diablo; il click destro nell'inventario è il suo |
| D15 | **Automappa, anticipata dalla M7** | Le celle si **scoprono** entro qualche metro dal cavaliere. Un tasto solo, **`M`**, scorre le modalità: **minimappa** nell'angolo, vista **sovrapposta** al gioco (trasparente, a linee, come in Diablo), spenta, e da capo. *Aggiornata il 5 ott 2026 su richiesta di Mirco, provato il 6.8: prima erano `Tab` per mostrarla e `F` per cambiare vista.* Muri, scala, casse e il punto del cavaliere. Alla M7 si estende alle caverne | La griglia della cripta c'è già. Le due viste le ha chieste Mirco: la sovrapposta non copre niente quando è chiusa, l'angolo si legge giocando |
| D16 | **Tooltip delle statistiche** | Passando su una riga del pannello del personaggio, un tooltip dice a cosa serve, con la formula in parole: Forza (danno e requisiti), Destrezza (a colpire e blocco), Magia (dalla M9), Vitalità (vita), Vita, Armatura, Danno, A colpire, Blocco. Testi nella tabella delle lingue | Chi gioca deve sapere perché alzare un attributo, senza leggere il piano |

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
| 6.7 | Pozioni e cintura (D14) | 2 |
| 6.8 | Automappa sovrapposta e nell'angolo (D15) | 2–2,5 |
| 6.9 | Ripartenza dall'ingresso del livello (D13) | 1 |
| 6.10 | Tooltip delle statistiche (D16) | 0,5–1 |
| 6.11 | Dipendenze tra le cartelle e asmdef | 0,5 |
| 6.12 | Chiusura: build da provare, GIF, ADR, tag `m6` | 0,5 |

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

**Com'è andata (5 ott 2026).** `Level_Crypt` sta in `Scenes/`, accanto a `Core`: contiene solo
le impostazioni di luce della cripta e un `DungeonLevel` con i numeri (`Data/Levels/CryptSettings.asset`)
e il tileset. La costruisce il menu *DarkDescent → Ricostruisci la scena della cripta*, che la
mette nei Build Profiles prima dei livelli fatti a mano. Primo giro con i riferimenti vuoti:
gli asset caricati prima di aprire la scena nuova vengono scaricati dal cambio di scena, quindi
ora la scena si apre per prima. **`DungeonLevel.Build(seme, profondità)`** rende attiva la sua
scena (trappola 4), genera con `SeedMixer.ForLevel`, aggiunge le direttive (ingresso `Start` al
livello 1, `FromAbove` sotto; uscita verso la stessa scena alla profondità successiva, tolta
all'ultima con la sua scala), costruisce con i nemici spenti, cuoce il NavMesh **dai collider**
(D5) e solo allora accende i nemici, che si agganciano al NavMesh appena cotto. Scrive i tempi
nel log. **`LevelManager`** riceve il seme dal composition root (`RunSeed`), carica con una
profondità, e se la scena caricata non ha un `LevelContext` alla radice ma un `DungeonLevel`, gli
fa costruire il livello: a schermo già nero, perché la dissolvenza viene prima del caricamento
(trappola 6). L'uscita porta la profondità di arrivo (`LevelExit.TargetDepth`; per le mappe a
mano, se manca, la successiva), e l'etichetta "Descend to level N" la usa. `Core` parte ora da
`Level_Crypt`. **Misura (D4):** nei test, nell'editor, un livello costa 30–50 ms in tutto
(generazione 0–7, costruzione 5–17, NavMesh 11–25): molto sotto il secondo. **In build**
(`Player.log` della prova di Mirco, 5 ott 2026, RTX 5070 Ti Laptop): il primo livello della
partita 44 ms (generazione 10, costruzione 18, NavMesh 16), da freddo; i successivi 15 ms
(1 + 3 + 11) e 12 ms (0 + 4 + 8). Un livello costa al più il 4,4% del secondo concesso da D4,
quindi D4 si chiude: NavMesh a runtime. **Test** (trappola 10): `LoadCore` dei test carica
`Core` con `Level_01` nello stesso frame, come faceva la sandbox, e i test che cercano la stanza
dello scudo o il primo scheletro restano com'erano; `LoadGeneratedCore` parte dalla cripta.
Aggiornati i tre test che si aspettavano `Level_01` come primo livello (avvio, riavvio dopo la
morte, seme dopo un nuovo avvio). Nuovo `DungeonDescentTests`: avvio con il livello 1 generato
(buio, una sola luce con le ombre, 5 scheletri sul NavMesh, scala per il livello 2), discesa
fino al 4 che non ha la scala, stesso seme della partita stesso livello con gli stessi drop e
seme diverso livello diverso. `LevelBuilderTests` accende i nemici dopo il NavMesh, come il
gioco: niente più "Failed to create agent" nel log. 108 EditMode e 93 PlayMode verdi.

## Passo 6.5 — Casse

1. `Chest`: interazione, apertura una volta sola, suono di Kenney (da `Downloads`, se c'è,
   altrimenti chiedo il via al download), `LootTable` delle casse, seme dalla cella.
2. Etichetta "Chest"/"Cassa" nella tabella delle stringhe.
3. Test PlayMode: click sulla cassa, apertura, oggetto a terra, un secondo click non fa
   niente.

**Com'è andata (5 ott 2026).** Il modello `chest` di KayKit ha il coperchio separato
(`chest_lid`), con il perno già sulla cerniera dietro: `Chest` lo ruota di 110° attorno a X in
0,35 s, e il bordo davanti sale. Il componente sta sul prefab della cassa, quindi vale anche per
quella del livello 1 fatto a mano: `Interactable` con l'etichetta "Chest"/"Cassa" e il punto
d'arrivo 1,7 m davanti, un collider del click sul layer `Interactable` poco più grande di quello
dell'ostacolo (il raggio prende prima lui), l'evidenziazione con il materiale acceso del
dungeon, una `AudioSource` sul mixer SFX con le stesse impostazioni 3D degli scheletri e tre
`impactPlank_medium` di Kenney, presi dallo zip *Impact Sounds* già scaricato alla M5 (riga in
`CREDITS.md`). Aperta, spegne il collider del click: il cursore non la vede più, niente nome né
evidenziazione. `Data/Loot/ChestLoot.asset` cade sempre (probabilità 1) e pesa allo stesso modo
le otto basi; il seme è quello della cella, come per i nemici, attraverso `LootRoller`. Le casse
le raccoglie `LevelContext.Chests` e le collega il composition root con la profondità. **Verso:**
il builder ora gira gli oggetti di scena con il davanti verso la stanza, lontano dal muro che
hanno accanto, altrimenti una cassa contro il muro sud si sarebbe aperta verso il muro. Una
foto usa e getta ha confermato coperchio e verso. Test: `ChestTests` (le casse della cripta sono
collegate, si raggiungono e si chiamano "Chest"; il click le apre, il coperchio sale, cade
l'oggetto previsto dal seme, un secondo click non fa niente). 108 EditMode e 95 PlayMode verdi.

## Passo 6.6 — Finestra dell'editor

1. `DungeonGeneratorWindow` in UI Toolkit, nell'assembly dell'editor.
2. Disegno della mappa in una texture, una cella per pixel ingrandita.
3. Salvataggio della mappa in `Levels/`.

**Com'è andata (5 ott 2026).** *DarkDescent → Generatore di dungeon* apre la finestra, in UI
Toolkit e senza UXML: i numeri della cripta (`CryptSettings`), il **seme della partita** e la
profondità, i pulsanti per il seme precedente e il successivo, e la mappa a quadretti da 14
pixel per cella, con sotto stanze, celle di pavimento, scheletri, casse e passi a piedi
dall'ingresso alla scala. Il seme è quello di `-seed`, non quello del livello: la finestra usa
`DungeonLevel.CreateMap`, lo stesso metodo del gioco, quindi mostra proprio il livello che si
gioca con quel seme a quella profondità (all'ultima, senza la scala). Il disegno sta in
`MapPainter`, nell'assembly del gioco, perché lo riuserà l'automappa (D15): un pixel per cella, il
nord in alto, un colore per simbolo, e le celle non scoperte trasparenti. "Salva come mappa"
scrive `Levels/Crypt_<seme>_<profondità>.txt` con `LevelMap.ToText` (direttive in ordine e
griglia, lo stesso formato che si legge); la ricostruzione dei livelli ne fa una scena di prova.
`CreateGUI` può arrivare dopo `GetWindow`, quindi la finestra si costruisce alla prima chiamata
che ne ha bisogno. Test (`MapPainterTests`, con il riferimento all'assembly dell'editor): una
mappa scritta e riletta resta uguale, i colori finiscono nelle celle giuste con il nord in alto,
e la finestra con seme 4711 e profondità 2 disegna la stessa mappa del gioco. 111 EditMode e 95
PlayMode verdi.

## Passo 6.7 — Pozioni e cintura

1. `PotionDefinition` (oggetto 1 × 1, quanto cura in percentuale della vita massima) e la
   pozione di cura, con icona e nome nelle due lingue.
2. `Belt`, logica pura: 8 posti, raccolta che preferisce la cintura, uso per posto. Il
   cavaliere parte con 2 pozioni.
3. Barra della cintura sotto la sfera della vita, tasti 1–8 nell'Input System (due passaggi,
   lezione della M4), click destro nell'inventario per bere.
4. Tiri delle pozioni a parte nel loot di scheletri e casse, con un seme loro.

**Com'è andata (5 ott 2026).** La pozione di cura è una `PotionDefinition` da 1 × 1 che rende il
50% della vita massima, arrotondato a un intero come i danni (ADR-031). Il modello è la bottiglia
con l'etichetta del Dungeon Pack: esiste solo verde o marrone, quindi una copia della texture ha il
vetro ricolorato di rosso (`dungeon_texture_red.png`, materiale `M_Potion`). Il prefab la dimezza:
alta 0,89 m, a terra sembrava una damigiana. `HealthModel.Heal` rende vita fino al massimo, non
riporta in vita e a vita piena non fa niente. `Health` la inoltra con `HealthChanged`, così la
sfera si aggiorna da sola, e con `Healed`, su cui `CharacterAudio` suona il vetro
(`impactGlass_light` di Kenney). La `Belt` sta dentro `Inventory`: 8 posti che accettano solo
pozioni. La raccolta, e il ritorno di un oggetto dal cursore alla chiusura dell'inventario,
provano prima la cintura e poi la griglia. `ClickBelt` fa il click-e-click come le celle.
Scostamento da Diablo: **a vita piena la pozione non si beve**, mentre in Diablo 1 si sprecava;
un tasto premuto per sbaglio non deve costare una pozione. I tasti 1–8 sono un'azione sola
`UseBelt` con otto binding: il posto si ricava dal tasto premuto (`Key.Digit1` e seguenti sono
consecutivi). Con il primo passaggio in batch Unity ha rigenerato `PlayerControls`, e solo dopo è
arrivato il codice che la usa (lezione della M1). Li ascolta `PlayerInventory`, che ora dipende
dal `PlayerInputReader` dello stesso oggetto: è una dipendenza nuova da Items a Player, da mettere
nel grafo del passo 6.11. La cintura a schermo (`BeltView`, `BeltSlotView`) è una fila di otto
posti con il bordo del colore della sfera e il numero nell'angolo. La sfera è salita di 52 pixel ed è
passata da 190 a 176, così resta sotto la finestra del personaggio. Il click destro beve dalla cintura
e dalla griglia; il sinistro sposta le pozioni nella cintura solo con l'inventario aperto,
altrimenti l'oggetto preso non avrebbe dove andare. Trappola: la cintura deve stare **dopo**
l'inventario nella gerarchia della HUD, sopra il fondo trasparente che lascia a terra gli oggetti,
altrimenti con un oggetto sul cursore i click della cintura li prende lui. Il loot ha due campi
in più, pozione e probabilità (scheletri 0,25, casse 0,5). `LootRoller.RollPotion` tira con
`SeedMixer.ForPotion`, che mescola il seme del nemico con un dominio suo ("POTION"): gli oggetti
del seme 4711 sono rimasti identici, e il test lo conferma. La pozione cade di lato all'oggetto,
così i due non si coprono. Test: cura e suoi limiti; cintura (riempimento, presa e scambio,
raccolta che la preferisce, ritorno dal cursore, bere solo pozioni); probabilità su 10.000 tiri;
tiro della pozione scollegato da quello dell'oggetto; tooltip nelle due lingue. In gioco: due
pozioni alla partenza, il tasto 2 beve il secondo posto e non spreca a vita piena; click destro
e sinistro veri; la pozione dello scheletro va nella cintura; la cassa la lascia. Il test del
seme 4711 ora conta anche la pozione prevista. 122 EditMode e 99 PlayMode verdi. Per guardare la
HUD in batch, `ScreenCapture` non funziona: la Canvas passa a Screen Space Camera e la camera
disegna in una RenderTexture, come per la prima anteprima della sfera.

## Passo 6.8 — Automappa

1. Stato di esplorazione delle celle (logica pura), aggiornato da dove sta il cavaliere.
2. Disegno della mappa scoperta in una texture: muri, scala, casse, il punto del cavaliere.
3. Vista sovrapposta e minimappa nell'angolo; il tasto nell'Input System (`M`, D15 aggiornata).

**Com'è andata (5 ott 2026).** `Exploration` (logica pura) tiene le celle viste e le scopre con
una visita sul pavimento: 3 passi dalla cella del cavaliere, anche in diagonale, così attorno a
lui si scopre un quadrato e non una croce (la prima versione, a passi ortogonali, lasciava fuori
gli angoli delle stanze). La diagonale non taglia lo spigolo di un muro, e la stanza dietro un
muro resta nascosta finché non ci si arriva: a raggio, una cella di là dal muro si sarebbe vista.
`ExplorationTracker` sta in Core. A ogni frame confronta due celle, e solo quando il cavaliere
cambia cella scopre e avvisa. Il CompositionRoot gli dà la mappa di ogni livello caricato, che
ora `LevelContext` espone. Esiste solo per i livelli costruiti a runtime: nelle scene salvate non
si serializza, e nei livelli fatti a mano l'automappa resta spenta. `MapPainter.PaintExplored`
disegna in una texture da 20 pixel per cella, con un buffer riusato, il pavimento appena
accennato, una linea su ogni lato che confina con la roccia, la scala piena e un quadratino per
casse e ingresso. Nemici e oggetti di scena non compaiono, come in Diablo. Le due viste
(`AutomapFrame`) sono la stessa texture dentro un genitore ruotato di 45° e uno schiacciato a
metà: la camera guarda da 45° e 30°, quindi il nord della mappa cade dove cade sullo schermo.
Tutte e due sono centrate sul cavaliere, e scorre la mappa. La minimappa (240 × 140, 12 pixel per
cella) ha una maschera e il bordo della sfera; la vista sovrapposta (56 pixel per cella, circa un
quinto del mondo) è trasparente all'80%. Le misure vengono da tre giri di screenshot: a 22 pixel
la vista sovrapposta era un francobollo, e la minimappa ferma mostrava solo un angolo. Le linee
sono un decimo di cella, così restano visibili nell'angolo senza diventare strisce al centro.
Si parte con la minimappa accesa. Il tasto `M` scorre le modalità (`AutomapMode`): angolo,
sovrapposta, spenta, e da capo. La prima versione usava `Tab` per mostrarla e `F` per cambiare
vista; Mirco ha chiesto un tasto solo. L'azione `CycleMap` è nata con i due passaggi in batch,
come le pozioni: prima aggiunta e rigenerata, poi il codice e la rimozione delle azioni vecchie. Nessuna immagine dell'automappa prende i raggi: coprirebbe il mondo e
ruberebbe i click; un test lo controlla. Test: visita per passi, stanza dietro il muro, spigoli,
cella di un punto del mondo, disegno (muri, pavimento, scala, cassa, trasparenza del non visto).
In gioco: la cripta parte con l'ingresso scoperto e la minimappa accesa; spostando il cavaliere
si scopre la scala e la texture si ridisegna, e da fermo no; `M` vero per le tre modalità, con
il punto del cavaliere sempre al centro; il livello fatto a mano non ha automappa. 126 EditMode e
102 PlayMode verdi.

## Passo 6.9 — Ripartenza dall'ingresso del livello

1. Istantanea dell'inventario e dell'equipaggiamento all'ingresso di ogni livello.
2. "Ricomincia" ricarica lo stesso livello (stesso seme e profondità), rimette l'istantanea,
   vita piena.

**Com'è andata (5 ott 2026).** Prima "Ricomincia" ricaricava tutta `Core`, e con lei cavaliere e
HUD nuovi: l'istantanea avrebbe dovuto sopravvivere al cambio di scena in un oggetto
`DontDestroyOnLoad` o in un campo statico, cioè proprio lo stato globale che ADR-007 esclude.
Così ora `Core` resta caricata e si ricarica solo il livello, come per le scale. A schermo nero,
con il livello vecchio già scaricato e il nuovo non ancora caricato, il cavaliere torna in vita
e l'inventario torna com'era: nessuno scheletro può colpirlo mentre si rialza. Le parti nuove:
- `LevelManager.RestartLevel` ripete l'ultimo ingresso, con la stessa scena, la stessa
  profondità e lo stesso seme della partita, quindi la stessa cripta con nemici e casse rimessi.
  Riceve una richiamata da eseguire nel buio.
- `HealthModel.Revive` e `Health.Revive` riportano la vita piena; con l'evento `Revived`,
  `PlayerDeath` riaccende input e attacco. Controller e agent li riaccende il LevelManager
  entrando nel livello, come a ogni cambio.
- `CharacterAnimatorDriver` rimette l'animator in piedi con `Rebind`, e `DeathScreen` si chiude.
- `InventorySnapshot` (logica pura) copia griglia con le posizioni, equipaggiamento, cintura e
  l'oggetto sul cursore, e passa per JSON con le istanze così come sono (ADR-022). È una copia
  che niente in gioco può cambiare, e alla M8 è già il formato del salvataggio. `Restore` svuota
  l'inventario (`Inventory.Clear`) e rimette prima l'equipaggiamento, i cui bonus possono servire
  ai requisiti, poi griglia e cintura; quello che non va più al suo posto finisce nella griglia.
- Il CompositionRoot prende l'istantanea un frame dopo ogni ingresso: con un livello già aperto
  (editor, test) l'ingresso arriva nello `Start` del LevelManager, prima che il cavaliere abbia
  equipaggiato la spada. Per ritrovare gli oggetti dall'ID ora conosce anche l'`ItemDatabase`.

L'automappa invece **resta**: nella prima versione ripartiva da capo come nemici e casse, ma
Mirco preferisce tenere la mappa scoperta. Il livello ricaricato è la stessa cripta, quindi
`ExplorationTracker.SetLevel(map, keepExplored)` copia le celle viste dall'esplorazione di prima
(`Exploration.CopyExplored`, solo se le misure coincidono); il CompositionRoot lo chiede tra
"Ricomincia" e il livello ricaricato, e in quel tempo non spegne l'automappa. Test: istantanea
passata per JSON (posizioni, equipaggiamento, cintura, affissi; quello raccolto dopo sparisce;
oggetto sul cursore) e `Revive` del modello. In gioco: morte alla profondità 2 dopo aver bevuto
una pozione, raccolto un pugnale, aperto la cassa e scoperta una cella lontana; "Ricomincia" riporta allo stesso ingresso
della stessa cripta, con la pozione di nuovo nella cintura, niente pugnale, la cassa chiusa, gli
scheletri a vita piena, la mappa scoperta com'era, il cavaliere in piedi e `timeScale` a 1;
poi si può morire di nuovo. Più la copia delle celle viste in EditMode. 130 EditMode e 102
PlayMode verdi.

## Passo 6.10 — Tooltip delle statistiche

1. Una zona del cursore per ogni riga del pannello del personaggio, con il tooltip degli
   oggetti riusato e i testi nella tabella delle lingue.

**Com'è andata (5 ott 2026).** Non una zona per riga: una sola zona invisibile (`StatLinesView`)
sopra le due colonne, che dal punto del cursore ricava la riga della colonna dei nomi con
`TMP_TextUtilities.FindIntersectingLine`, come la griglia dell'inventario ricava la cella; il
pannello agisce solo quando la riga cambia. Il riquadro è lo stesso componente dei tooltip degli
oggetti, con un metodo in più per un testo libero (`ItemTooltip.ShowText`), messo a destra della
finestra e centrato sulla riga, così non copre le altre righe. È una **copia sua** nella HUD
(`StatTooltip`): con quello dell'inventario, un ridisegno dell'inventario aperto l'avrebbe
spento. Titolo e spiegazione vengono tutti e due dalla tabella delle lingue (`hud.stat_names` e
nove chiavi `stat.tip.*`), con le formule in parole (Forza: +1% di danno a punto; Destrezza: ogni
2 punti +1% a colpire e di blocco; Vitalità: 2 punti vita; Armatura: −1% alla probabilità dei
nemici di colpirti; la formula del colpo e quella del blocco). Il titolo non si legge dalla
colonna dei nomi: al cambio di lingua quella si traduce da sé, e poteva arrivare dopo il tooltip.
`CharacterPanel.LineTips` lega le righe alle chiavi, con null per la riga vuota. Test: righe e
spiegazioni coincidono nelle due lingue; in gioco il tooltip compare sulla Forza, a destra della
finestra, cambia sull'Armatura, sparisce sulla riga vuota e fuori, segue la lingua senza muovere
il cursore e si chiude con il pannello. 131 EditMode e 103 PlayMode verdi.

## Passo 6.11 — Dipendenze e asmdef

1. Grafo delle dipendenze tra le cartelle di `Scripts/`, dagli `using` e dai tipi usati.
2. Decisione secondo D11, con l'ADR.

**Com'è andata (5 ott 2026).** Il grafo l'ha ricavato uno script dai tipi dichiarati in ogni
cartella e usati nelle altre, non solo dagli `using`. Per cartelle è un unico groviglio: tutte le
aree tranne Audio, Input, Rendering e Stats stanno in un ciclo. Molto passa dal CompositionRoot,
ma restano cicli veri: Combat e Items, Levels e UI. Qualche falso positivo va scartato a mano: la
costante `TextKeys.Chest` scambiata per la classe `Chest`, il metodo `ItemStats.ShieldBlock`. La
domanda di D11 però è un'altra: il codice senza MonoBehaviour usa mai componenti di scena? No,
con un'eccezione sola, `LevelBuilder`, che non è un componente ma monta le scene. Quindi
**si divide a strati** (ADR-032): `DarkDescent.Core` in `Scripts/Core/<Area>/`, con logica e dati
(56 file spostati con `git mv`, GUID invariati), e `DarkDescent` con i componenti, che lo
referenzia. Il primo tentativo ha portato in `Core` anche `LevelTileset`, che usa `VolumeProfile`
della render pipeline: lo usa solo il runtime, ed è tornato indietro con `MarkerPrefab`, così
`Core` dipende solo da UnityEngine. CompositionRoot e HitStop, gli unici componenti della vecchia
`Core`, sono in `Composition/`. Un assembly senza UnityEngine è scartato: griglia, inventario e
generatori usano `Vector2Int`, `RectInt` e le definizioni ScriptableObject, e ci sarebbero stati
solo una ventina di file. La regola su dove va un file nuovo è nelle convenzioni (11). Test:
`Core` non contiene MonoBehaviour e non referenzia `DarkDescent`. 132 EditMode e 103 PlayMode verdi.

## Passo 6.12 — Chiusura

1. Build della CI da provare: quattro livelli di fila, stesso dungeon con `-seed 4711`,
   pozioni, automappa nelle due viste, ripartenza dopo la morte.
2. GIF del README: la discesa attraverso due livelli generati, con una cassa aperta e la mappa.
3. ADR: builder unico, BSP, NavMesh a runtime dai collider, contenuto per profondità, seme
   del livello, asmdef. Lezioni nel piano, tabella dello stato, tag `m6`.

**Com'è andata (5 ott 2026).** GIF del README (`docs/media/m6_descent.gif`, 640 × 360, 3,7 MB
in LFS) registrata da un test usa e getta nella cripta del seme 1137, scelto tra 1500 perché ha
la cassa vicina all'ingresso e la scala a una ventina di passi: la cassa aperta e l'oggetto
raccolto, la vista sovrapposta dell'automappa che si scopre mentre il cavaliere combatte due
scheletri verso la scala, la discesa al livello 2. A 20 fotogrammi al secondo e 800 × 450 pesava
8,6 MB, perché con la camera che segue cambia quasi tutto lo schermo a ogni fotogramma: ora è a
10 al secondo, con il tragitto verso la scala accelerato. ADR-033…038 in `DECISIONS.md` (builder
unico, BSP e seme del livello, NavMesh dai collider, ripartenza, pozioni, automappa), oltre
all'ADR-032 del passo 6.11. Lezioni nel piano (v2.15), tabella dello stato aggiornata, README con
i comandi nuovi. Scheda della M7 scritta. Restano la prova della build della CI e il tag `m6`.

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
- [x] Livelli generati nel gioco, NavMesh a runtime misurato (in build 12–44 ms a livello: D4 chiusa)
- [x] Casse
- [x] Decisioni D13–D16 confermate (dopo la prova della build)
- [x] Finestra dell'editor
- [x] Pozioni e cintura
- [x] Automappa nelle due viste
- [x] Ripartenza dall'ingresso del livello
- [x] Tooltip delle statistiche
- [x] Dipendenze e asmdef
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m6`
- [x] Scheda della M7 scritta prima di cominciarla
