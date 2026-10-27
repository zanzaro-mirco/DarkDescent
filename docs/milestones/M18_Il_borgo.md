# M18 — "Il borgo"

**Cosa deve succedere (Definition of Done):** il gioco comincia in un **borgo all'aperto**, invece
che nella sala sotto terra, e la sua luce **cambia con il tempo di gioco**: alba, giorno, tramonto,
notte. È la disposizione scelta da Mirco:
- al centro una **piazza** con il fuoco, il banco del mercante e la forgia del **fabbro**;
- la casa della **guaritrice**;
- attorno le case dalle finestre buie, alcune sfondate;
- una **cappella** con l'altare delle difficoltà;
- a nord il **cimitero** e, nel fianco della collina dietro, la **bocca di una grotta** che porta
  alla profondità 1, al posto della scala.

Alla profondità 1 si arriva davanti allo sbocco di un **cunicolo** di roccia. Il borgo è compatto:
dal portale alla grotta meno di 10 s a piedi. Altare, portali, salvataggio e automappa funzionano
come oggi; il negozio si divide tra mercante, fabbro e guaritrice. Alla chiusura si misurano in
build i tempi di costruzione di tutti i livelli. Il borgo è anche il set dei filmati della M19. Esce
con il tag `v1.6.0`.

**Tempo stimato:** 15–21 h (D15): 8–12 per il borgo, più fabbro e guaritrice, il giorno che passa e
la misura dei livelli, aggiunti da Mirco il 27 ott 2026.
**Prerequisito:** M17 chiusa (tag `v1.5.0`, 27 ott 2026).

**Come si lavora:** come alle M13–M17. Faccio io:
- il codice, la mappa, i modelli in Blender e i passaggi nell'editor;
- le prove in batchmode, a Unity chiuso.

A Mirco restano:
- le decisioni qui sotto;
- il sì sui download (D8);
- il sì sulle foto della disposizione e del borgo finito;
- la prova in build.

---

## Stato verificato il 27 ottobre 2026

| Cosa | Stato |
|---|---|
| Accampamento | Una mappa di testo, `Data/Levels/Town.txt`, di 13 × 10 celle da 4 m: una sala con il fuoco, due tavoli, scaffali, botti, giacigli, il banco del mercante (`M`), l'altare (`A`), l'arrivo dei portali (`P`) e la scala verso la cripta (`>`, `@exit Level_Crypt Start 1`). La costruisce a runtime `MapLevel` con lo stesso `LevelBuilder` dei livelli generati, che ne cuoce anche il NavMesh. La profondità 0 è la città (`LevelContext.IsTown`). Il nome in gioco è `level.town`, "Accampamento" / "Camp" |
| Tileset | `LevelTileset` dà al builder un pavimento e un muro con le loro varianti, il muro basso, la torcia a muro, la scala, il colore della luce ambiente, il profilo di post-processing, il fondo sonoro, i passi e i prefab dei simboli (`MarkerPrefab`). È pensato per sale chiuse: ogni `#` diventa un muro |
| Luce | La luce è quella ambiente del tileset, più torce e fuochi. La nebbia è spenta (`RenderSettings.fog = false`), il fondo della camera è quasi nero. Il renderer del PC è **Forward+**, che regge molte luci; le ombre arrivano a 25 m, con una mappa da 2048 |
| Ingresso della profondità 1 | Il generatore mette l'ingresso (`<`) al centro della prima stanza: è solo un punto, **non c'è nessun modello**. Né una scala in su né altro |
| Modelli | Nessun pacchetto scaricato ha case, alberi, lapidi, recinti o rocce da esterno. `Tools/Blender` fa modelli nello stile KayKit (ADR-094). I personaggi `Mage`, `Ranger` e `Rogue` sono nello zip di *Adventurers 2.0 FREE*, non importati. Le animazioni da seduti e del saluto (`Sit_Floor_*`, `Waving`) sono nello zip delle animazioni, non importate |
| Negozio | Un mercante solo. `Shop` (logica pura in `Core`) tiene le provviste, sempre lì, e gli oggetti di questo ritorno in città; compra al prezzo pieno e vende al 25%. `ShopPanel` è la sua finestra. **Gli oggetti non si consumano**: non c'è niente da riparare |
| Tempi dei livelli | La D4 della M6 vuole generazione, costruzione e NavMesh sotto i 50 ms. L'ultima misura in build è della M11, prima di scorte e forzieri (M12), barili, pozzi, santuari e trappole (M13) e delle macchie (M17); la ronda tiene un limite di 150 ms |
| Salvataggio | Il formato 7 ricorda se si è in città (`InTown`), la profondità del portale e il punto del portale nel dungeon. **Non ricorda la posizione in città**: si rientra da `<` |
| Test | `TownMapTests` (EditMode) legge `Town.txt` e il `TownTileset`; `TownTests`, `PortalTests` e `AutomapTests` (PlayMode) passano dalla città |

---

## Decisioni

D2, D5, D7, D18 e D19 le ha scelte Mirco il 27 ottobre 2026, in chat; le altre le ha confermate lo
stesso giorno come proposte ("pusha e procedi con il 18.1"), con il nome "Il borgo" della D11.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M18 "Il borgo"**, tag **`v1.6.0`**. Dentro: il borgo giocabile con il giorno che passa, la grotta, il cunicolo alla profondità 1, gli abitanti, fabbro e guaritrice, i suoni, la misura dei livelli. Le regole del combattimento non cambiano | Si vede dalla prima schermata di ogni partita: una versione minore |
| D2 | **La disposizione** (scelta da Mirco) | **Piazza e grotta**, come Tristram. Nella piazza il fuoco, il banco del mercante, la forgia del fabbro e l'arrivo dei portali. Attorno sei o sette case, due sfondate; una è della guaritrice. A est la cappella con l'altare, a nord il cimitero e dietro la collina con la bocca della grotta. Circa **18 × 14 celle** camminabili, con le strade tra le case | Compatto: ogni ritorno dal dungeon passa di qui, e una camminata lunga stanca alla decima volta |
| D3 | **Ancora una mappa di testo** | Il borgo resta una mappa di testo costruita da `MapLevel`, con un **tileset nuovo** per il fuori, `VillageTileset`: terra battuta e erba al posto del pavimento, e i `#` del bordo diventano rocce, recinti e alberi fitti invece di muri. Le case sono pezzi da più celle, con un simbolo e un verso. La chiave resta `level.town`, la profondità resta 0 | Il builder, il NavMesh, l'automappa e i test ci sono già. Ritoccare il borgo resta cambiare un testo |
| D4 | **Oltre il bordo** | Attorno alla parte camminabile, **3–4 celle di paesaggio** senza collider: colline, alberi, le case più lontane. Più in là la **nebbia** e il fondo della camera, dello stesso colore del cielo nel momento del giorno. Ruotando la camera non si vede mai il vuoto | Sotto terra il nero va bene, fuori no: un borgo che finisce nel nero sembra un modellino. La nebbia costa poco e chiude l'orizzonte |
| D5 | **Il giorno che passa** (scelto da Mirco, al posto del tramonto fisso) | Quattro momenti, **alba, giorno coperto, tramonto, notte**, con passaggi graduali di un minuto. L'ora viene dal **tempo di gioco della partita** (`RunStats.PlayTime`, già nel salvataggio): un giro intero ogni **20 minuti**, e tornando dal dungeon si trova un'altra ora. Una classe pura in `Core`, `DayCycle`, dà il momento e la miscela tra due momenti; il borgo ne legge sole, luce ambiente, nebbia e post-processing. Torce, fuoco e finestre si accendono dal tramonto all'alba. Di notte la luce ha un **minimo**: strade, banchi e grotta restano leggibili. Il dungeon non cambia. Le foto si fanno nei quattro momenti, con il filtro retrò spento e acceso | Un borgo che cambia luce sembra vivo, e legarlo al tempo di gioco non vuole un formato nuovo del salvataggio. Il minimo di notte tiene il gioco leggibile. I filmati della M19 usano gli stessi momenti |
| D6 | **La grotta** | La bocca nel fianco della collina, a nord dietro il cimitero: un arco di roccia con le radici che pendono e due torce, fatto in Blender. Si comporta come la scala di oggi: si illumina al passaggio, un click e si scende. Il simbolo `>` e `@exit Level_Crypt Start 1` non cambiano | Dal cimitero sono usciti gli scheletri, e la grotta sotto il cimitero porta alle cripte del re: il posto racconta già la storia |
| D7 | **Il cunicolo alla profondità 1** (scelto da Mirco) | Sulla cella d'ingresso della profondità 1 della cripta, uno **sbocco di cunicolo**: un modello di roccia a sé, che sta in piedi da solo in mezzo alla stanza. Il cavaliere compare davanti, rivolto verso la stanza. Si mette **dopo** il generatore e **senza tiri del seme**: mappe, arredi e loot di tutti i semi restano uguali. Solo alla profondità 1 della cripta; le caverne restano come sono | L'ingresso oggi non ha nessun modello, quindi non c'è un muro dove scavare. Un modello a sé non tocca il generatore, la ronda e i test con i semi fissi. Ed è il punto da cui la M22 farà risalire |
| D8 | **I modelli** | Pacchetti **CC0 di KayKit**, nello stesso stile del gioco: *Medieval Hexagon* per case e alberi (senza le basi esagonali), *Halloween Bits* per lapidi, recinti e lanterne, *Forest Nature* per rocce e cespugli. Licenza, contenuto e dimensioni si leggono sulla pagina **prima di scaricare**, con il sì di Mirco. In Blender faccio la grotta, il cunicolo, la cappella se manca e le case sfondate. La riserva, se un pacchetto non va, è tutto in Blender, con più ore | Lo stesso autore tiene lo stile del resto del gioco. Un borgo intero solo in Blender costerebbe metà della milestone |
| D9 | **Gli abitanti** | Quattro–sei sopravvissuti senza intelligenza né dialoghi: seduti attorno al fuoco (`Sit_Floor_Idle`), uno che saluta quando si arriva (`Waving`). Sono i modelli di *Adventurers*, senza armi e con le tinte cambiate. Sono ostacoli per il NavMesh. Il mercante resta al suo banco; fabbro e guaritrice sono nella D18 | Un borgo vuoto non sembra un rifugio. Seduti e fermi costano poco, e sono le stesse comparse dei filmati della M19 |
| D10 | **Il suono** | La musica dell'accampamento (*The Bard's Tale*) resta: è già una musica da borgo. Il fondo diventa il fuoco più il vento e qualche corvo lontano, generati con Stable Audio come i suoni della M13 | Fuori il solo crepitio suona vuoto. I suoni generati hanno già le ricette e la pagina d'ascolto |
| D11 | **Il nome** | In gioco **"Il borgo"** / *"The village"*, al posto di "Accampamento", con la stessa chiave `level.town`. Se Mirco vuole un nome proprio, come Tristram, si sceglie qui | Il nome che si legge entrando deve dire quello che si vede |
| D12 | **Salvataggi** | Il formato non cambia: la città è sempre la profondità 0, e si rientra da `<`. Un salvataggio fatto nell'accampamento si carica nel borgo, e un test lo prova con una fixture | Chi ha una partita salvata non deve perderla |
| D13 | **Si controlla** | EditMode: `DayCycle` (stesso tempo, stesso momento; i quattro momenti in fila; la miscela continua), i tre banchi (ognuno solo i suoi tipi, provviste giuste, prezzi di sempre), la guaritrice che riempie vita e mana; il borgo ha i simboli del suo tileset, un ingresso, la grotta che porta alla profondità 1, un cammino dal portale al mercante, all'altare e alla grotta, e il cammino più lungo sotto i 10 s. Il cunicolo non cambia mappe né loot dei semi fissi. PlayMode: una partita nuova comincia nel borgo, la grotta porta alla profondità 1 davanti al cunicolo, il portale riporta nel borgo, l'automappa del borgo si disegna, fabbro e guaritrice aprono i loro banchi, il borgo cambia luce con il tempo di gioco. Il giro di foto della M17 aggiunge il borgo. La ronda non cambia | Il borgo è il posto in cui si passa più spesso: un cammino rotto lì rompe tutto il gioco |
| D14 | **Il sì di Mirco** | Due giri di foto: prima la **disposizione** vista dall'alto, con i pezzi grezzi, poi il **borgo finito** nei quattro momenti del giorno, con e senza filtro. Poi la prova in build | Spostare una casa sulla mappa costa un minuto; dopo luci e dettagli, molto di più |
| D15 | **Stima e linee di taglio** | **15–21 h**. Se servirà tagliare: prima il paesaggio oltre il bordo (resta la nebbia), poi gli abitanti seduti, poi le case sfondate. Il borgo camminabile, la grotta, il cunicolo, il giorno che passa e la misura non si tagliano. Se la milestone passa le 21 h, fabbro e guaritrice diventano una milestone a sé subito dopo, prima dei filmati | Meglio deciderlo adesso che a metà. Con le aggiunte è una milestone lunga: la riserva è già scritta |
| D16 | **Dimensioni e prestazioni** | Il limite è **+20 MB** sulla build. Nel borgo **60 fps** in build sul PC di sviluppo, e il borgo si costruisce in meno di 300 ms. Forward+ regge molte luci, ma ognuna costa: si misura alla chiusura | Il borgo è il livello più grande e più illuminato del gioco, e lo si vede a ogni ritorno |
| D17 | **Fuori da questa milestone** | Abitanti che camminano o parlano; missioni; la riparazione e il consumo degli oggetti; il meteo; i filmati (M19); la risalita e le stanze più grandi (M22) | Ognuna è un lavoro a sé, e il consumo degli oggetti cambierebbe le regole del gioco |
| D18 | **Fabbro e guaritrice** (scelti da Mirco) | Come a Tristram. Il **fabbro**, alla forgia nella piazza (il modello `Barbarian`, con `Hammering` dalle animazioni `Tools`), vende e compra **armi, armature e scudi**. La **guaritrice**, nella sua casa (`Mage` con le tinte cambiate), **riempie vita e mana gratis** quando la si raggiunge e vende le **pozioni**. Il **mercante** tiene anelli, amuleti, le pergamene e le altre provviste. È la stessa `Shop` con un **filtro per tipo** e la sua tabella, e la stessa `ShopPanel`: tre banchi, un codice solo. Gli oggetti di questo ritorno si tirano come oggi e si dividono tra i banchi. Niente riparazione, niente dialoghi | Tre banchi con un mestiere ciascuno riempiono il borgo e rendono chiaro dove cercare cosa. La cura gratis è quella di Pepin: tornare in città rimette in sesto. Un filtro sulla `Shop` che c'è non vuole un sistema nuovo |
| D19 | **La misura dei livelli** (scelta da Mirco) | In build, generazione, costruzione e NavMesh della cripta e delle caverne alle profondità 1, 4 e 8, più il borgo, con il tempo medio e il peggiore su 20 semi, contro i 50 ms della D4 della M6 e i 150 ms della ronda. I numeri vanno nella scheda e nel piano. Se un livello passa il limite, si apre una voce per accorciarlo; in questa milestone si corregge solo il borgo, se passa i suoi 300 ms | L'ultima misura è di sei milestone fa, e da allora ogni milestone ha messo qualcosa nei livelli. Misurare costa mezz'ora; ottimizzare va deciso sui numeri |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 18.1 | I modelli: i pacchetti del borgo (con il sì), le animazioni e i personaggi dagli zip, la grotta, il cunicolo e le case sfondate in Blender, le foto dei pezzi (D6, D7, D8) | 2–3 |
| 18.2 | Il tileset del borgo e la mappa: case, piazza, cappella, cimitero, bordo, paesaggio oltre il bordo; la foto della disposizione per Mirco (D2, D3, D4, D14) | 2–3 |
| 18.3 | Il giorno che passa: `DayCycle`, il sole, la luce ambiente, la nebbia e il post-processing dei quattro momenti, luci che si accendono, il filtro retrò; le foto per Mirco (D5, D14) | 4–5 |
| 18.4 | La grotta e il cunicolo: l'uscita verso la profondità 1, il modello sulla cella d'ingresso, i test sui semi fissi (D6, D7, D13) | 1–1,5 |
| 18.5 | Gli abitanti, il suono, il nome e il salvataggio vecchio (D9, D10, D11, D12) | 1–1,5 |
| 18.6 | Fabbro e guaritrice: la forgia e la casa, i tre banchi con il filtro per tipo, la cura, i test (D18) | 3–5 |
| 18.7 | Misura della build e dei tempi di tutti i livelli, foto, build da provare e chiusura: ADR, lezioni, piano, README, `bundleVersion` 1.6.0, tag `v1.6.0` dopo i test verdi in CI (D16, D19) | 1,5–2 |

---

## Passo 18.1 — I modelli

**Com'è andata (27 ott 2026).** I tre pacchetti della D8 li ho scaricati con il sì di Mirco, dalle
pagine di itch.io, nella versione gratuita: *Medieval Hexagon Pack 1.0* (33 MB), *Halloween Bits 1.0*
(5,3 MB) e *Forest Nature Pack 1.0* (6,1 MB), tutti CC0 come dice il `License.txt` di ognuno. Nel
progetto è entrata solo una scelta, in tre cartelle di `Art/Models/KayKit/`:

- `Village/` (87 modelli): case A e B, fucina, chiesa, taverna, pozzo e mercato nei quattro colori, la
  casa distrutta, l'impalcatura, recinti e mura, pini, colline, montagne, sassi, oggetti di scena e la
  tenda. Le basi di pietra con i gradini restano: fanno da soglia.
- `Graveyard/` (57): tutto Halloween Bits tranne le zucche. Il tempietto con le candele è l'altare
  dell'ultima candela; le tombe distrutte sono quelle da cui escono gli scheletri dei filmati.
- `Nature/` (73): i sassi, i cespugli e l'erba di Forest Nature. Gli alberi tondi no.

Più `Mage`, `Ranger` e `Rogue` di *Adventurers* per gli abitanti, e `Rig_Medium_Simulation`,
`Rig_Medium_Special` e `Rig_Medium_Tools` dalle animazioni complete, per gli abitanti e per i filmati
della M19.

**La scala.** Misurati in Blender accanto al cavaliere (alto 2,54 m), Halloween Bits e Forest Nature
sono già nella scala del gioco: il recinto è lungo 4 m come il muro della cripta. Medieval Hexagon è
fatto per una mappa a esagoni, in miniatura: la casa A è alta 0,93. **Scalato ×6** la porta è alta
quanto il cavaliere, la casa 5,6 m e la chiesa 9,9.

**Le scelte di Mirco** sulle foto (`docs/media/m18/01_pezzi.jpg`): tetti **misti** nei quattro
colori, **pini e alberi secchi** (gli alberi tondi di Forest Nature restano fuori), scala **×6**, e la
strada della grotta.

**La grotta e il cunicolo.** La prima prova, un arco di massi fatti con le scatole di `meshlib`,
sembrava un mucchio di macerie. Meglio i sassi di Forest Nature attorno a un buio: `Tools/Blender/models/cave.py`
fa solo la **galleria**, `CaveHole` (3,6 × 3,8 m, con le radici che pendono dalla volta) e `TunnelHole`
(2,8 × 3 m), con la volta ad arco, nera dentro, che entra di qualche metro; i massi che coprono il bordo
li metterà il prefab, al 18.4. Le prove sono in `docs/media/m18/02_grotta.jpg` e `03_cunicolo.jpg`.
Mirco ha chiesto se si possa generare con ComfyUI e convertire in Blender, come la statua della M13: si
può, con il rischio di uno stile diverso dai KayKit a tinte piatte. Al 18.4 si mettono le due grotte una
accanto all'altra nel borgo, e sceglie lui.

**Import.** `KayKitImporter` dà le impostazioni a tutto senza passare dall'Inspector: i pezzi come i
KayKit della cripta (niente materiali né animazioni), i personaggi con il loro scheletro, le animazioni
con lo scheletro del cavaliere e il loop solo per quelle che durano (seduti, il saluto, l'esultanza, il
martello, il lavoro; non alzarsi, risvegliarsi o morire). `VillageTools.Setup` fa i materiali
`M_Village`, `M_Graveyard`, `M_Nature`, `M_KayKitMage` e `M_KayKitRanger`, copiati da quelli che ci
sono, e scrive cosa c'è. `KayKitImportTests` (5 test) lo controlla; EditMode intera 491/491.

**Una cosa da sapere.** Il browser integrato scarica con un nome temporaneo e lo rinomina solo alla
fine: un file `.tmp` in `Downloads` è un download a metà, non un errore.

---

## Checklist di chiusura

- [x] Decisioni D1–D19 confermate (D2, D5, D7, D18 e D19 scelte da Mirco)
- [x] I download approvati: i pacchetti del borgo
- [ ] La disposizione e il borgo finito approvati da Mirco in foto
- [ ] Test EditMode e PlayMode verdi in locale e in CI
- [ ] La build misurata dentro il limite della D16, i tempi dei livelli misurati (D19)
- [ ] ADR, lezioni nel piano, README, `CREDITS.md` e `Data/Credits.txt`
- [ ] `bundleVersion` 1.6.0 e tag `v1.6.0` dopo i test verdi in CI
