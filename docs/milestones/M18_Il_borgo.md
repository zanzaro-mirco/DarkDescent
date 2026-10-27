# M18 — "Il borgo"

**Cosa deve succedere (Definition of Done):** il gioco comincia in un **borgo all'aperto**, al
tramonto, invece che nella sala sotto terra. È la disposizione scelta da Mirco:
- al centro una **piazza** con il fuoco e il banco del mercante;
- attorno le case dalle finestre buie, alcune sfondate;
- una **cappella** con l'altare delle difficoltà;
- a nord il **cimitero** e, nel fianco della collina dietro, la **bocca di una grotta** che porta
  alla profondità 1, al posto della scala.

Alla profondità 1 si arriva davanti allo sbocco di un **cunicolo** di roccia. Il borgo è compatto:
dal portale alla grotta meno di 10 s a piedi. Mercante, altare, portali, salvataggio e automappa
funzionano come oggi. Il borgo è anche il set dei filmati della M19. Esce con il tag `v1.6.0`.

**Tempo stimato:** 8–12 h (D15).
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
| Salvataggio | Il formato 7 ricorda se si è in città (`InTown`), la profondità del portale e il punto del portale nel dungeon. **Non ricorda la posizione in città**: si rientra da `<` |
| Test | `TownMapTests` (EditMode) legge `Town.txt` e il `TownTileset`; `TownTests`, `PortalTests` e `AutomapTests` (PlayMode) passano dalla città |

---

## Decisioni

D2, D5 e D7 le ha scelte Mirco il 27 ottobre 2026, in chat. Le altre sono da confermare.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M18 "Il borgo"**, tag **`v1.6.0`**. Dentro: il borgo giocabile al tramonto, la grotta, il cunicolo alla profondità 1, gli abitanti, i suoni. Le regole del gioco non cambiano | Si vede dalla prima schermata di ogni partita: una versione minore |
| D2 | **La disposizione** (scelta da Mirco) | **Piazza e grotta**, come Tristram. Nella piazza il fuoco, il banco del mercante e l'arrivo dei portali. Attorno sei o sette case, due sfondate. A est la cappella con l'altare, a nord il cimitero e dietro la collina con la bocca della grotta. Circa **18 × 14 celle** camminabili, con le strade tra le case | Compatto: ogni ritorno dal dungeon passa di qui, e una camminata lunga stanca alla decima volta |
| D3 | **Ancora una mappa di testo** | Il borgo resta una mappa di testo costruita da `MapLevel`, con un **tileset nuovo** per il fuori, `VillageTileset`: terra battuta e erba al posto del pavimento, e i `#` del bordo diventano rocce, recinti e alberi fitti invece di muri. Le case sono pezzi da più celle, con un simbolo e un verso. La chiave resta `level.town`, la profondità resta 0 | Il builder, il NavMesh, l'automappa e i test ci sono già. Ritoccare il borgo resta cambiare un testo |
| D4 | **Oltre il bordo** | Attorno alla parte camminabile, **3–4 celle di paesaggio** senza collider: colline, alberi, le case più lontane. Più in là la **nebbia** del colore del tramonto e il fondo della camera dello stesso colore. Ruotando la camera non si vede mai il vuoto | Sotto terra il nero va bene, fuori no: un borgo che finisce nel nero sembra un modellino. La nebbia costa poco e chiude l'orizzonte |
| D5 | **Il tramonto** (scelto da Mirco) | **Tramonto fisso**: una luce del sole bassa e arancione, con le ombre; la luce ambiente viola; le torce, il fuoco della piazza e la candela della cappella accesi. Il profilo di post-processing del borgo si rifà. Le foto si fanno con il filtro retrò spento e acceso, e il borgo deve reggere tutte e due | Scuro ma leggibile, e sempre uguale: si gioca, non si aspetta la luce. È anche la luce che il filmato d'apertura cambia in notte e in alba |
| D6 | **La grotta** | La bocca nel fianco della collina, a nord dietro il cimitero: un arco di roccia con le radici che pendono e due torce, fatto in Blender. Si comporta come la scala di oggi: si illumina al passaggio, un click e si scende. Il simbolo `>` e `@exit Level_Crypt Start 1` non cambiano | Dal cimitero sono usciti gli scheletri, e la grotta sotto il cimitero porta alle cripte del re: il posto racconta già la storia |
| D7 | **Il cunicolo alla profondità 1** (scelto da Mirco) | Sulla cella d'ingresso della profondità 1 della cripta, uno **sbocco di cunicolo**: un modello di roccia a sé, che sta in piedi da solo in mezzo alla stanza. Il cavaliere compare davanti, rivolto verso la stanza. Si mette **dopo** il generatore e **senza tiri del seme**: mappe, arredi e loot di tutti i semi restano uguali. Solo alla profondità 1 della cripta; le caverne restano come sono | L'ingresso oggi non ha nessun modello, quindi non c'è un muro dove scavare. Un modello a sé non tocca il generatore, la ronda e i test con i semi fissi. Ed è il punto da cui la M20 farà risalire |
| D8 | **I modelli** | Pacchetti **CC0 di KayKit**, nello stesso stile del gioco: *Medieval Hexagon* per case e alberi (senza le basi esagonali), *Halloween Bits* per lapidi, recinti e lanterne, *Forest Nature* per rocce e cespugli. Licenza, contenuto e dimensioni si leggono sulla pagina **prima di scaricare**, con il sì di Mirco. In Blender faccio la grotta, il cunicolo, la cappella se manca e le case sfondate. La riserva, se un pacchetto non va, è tutto in Blender, con più ore | Lo stesso autore tiene lo stile del resto del gioco. Un borgo intero solo in Blender costerebbe metà della milestone |
| D9 | **Gli abitanti** | Quattro–sei sopravvissuti senza intelligenza né dialoghi: seduti attorno al fuoco (`Sit_Floor_Idle`), uno che saluta quando si arriva (`Waving`). Sono i modelli di *Adventurers*, senza armi e con le tinte cambiate. Sono ostacoli per il NavMesh. Il mercante resta al suo banco | Un borgo vuoto non sembra un rifugio. Seduti e fermi costano poco, e sono le stesse comparse dei filmati della M19 |
| D10 | **Il suono** | La musica dell'accampamento (*The Bard's Tale*) resta: è già una musica da borgo. Il fondo diventa il fuoco più il vento e qualche corvo lontano, generati con Stable Audio come i suoni della M13 | Fuori il solo crepitio suona vuoto. I suoni generati hanno già le ricette e la pagina d'ascolto |
| D11 | **Il nome** | In gioco **"Il borgo"** / *"The village"*, al posto di "Accampamento", con la stessa chiave `level.town`. Se Mirco vuole un nome proprio, come Tristram, si sceglie qui | Il nome che si legge entrando deve dire quello che si vede |
| D12 | **Salvataggi** | Il formato non cambia: la città è sempre la profondità 0, e si rientra da `<`. Un salvataggio fatto nell'accampamento si carica nel borgo, e un test lo prova con una fixture | Chi ha una partita salvata non deve perderla |
| D13 | **Si controlla** | EditMode: il borgo ha i simboli del suo tileset, un ingresso, la grotta che porta alla profondità 1, un cammino dal portale al mercante, all'altare e alla grotta, e il cammino più lungo sotto i 10 s. Il cunicolo non cambia mappe né loot dei semi fissi. PlayMode: una partita nuova comincia nel borgo, la grotta porta alla profondità 1 davanti al cunicolo, il portale riporta nel borgo, l'automappa del borgo si disegna. Il giro di foto della M17 aggiunge il borgo. La ronda non cambia | Il borgo è il posto in cui si passa più spesso: un cammino rotto lì rompe tutto il gioco |
| D14 | **Il sì di Mirco** | Due giri di foto: prima la **disposizione** vista dall'alto, con i pezzi grezzi, poi il **borgo finito** al tramonto, con e senza filtro. Poi la prova in build | Spostare una casa sulla mappa costa un minuto; dopo luci e dettagli, molto di più |
| D15 | **Stima e linee di taglio** | **8–12 h**. Se servirà tagliare: prima il paesaggio oltre il bordo (resta la nebbia), poi gli abitanti, poi le case sfondate. Il borgo camminabile, la grotta, il cunicolo e il tramonto non si tagliano | Meglio deciderlo adesso che a metà |
| D16 | **Dimensioni e prestazioni** | Il limite è **+20 MB** sulla build. Nel borgo **60 fps** in build sul PC di sviluppo, e il borgo si costruisce in meno di 300 ms. Forward+ regge molte luci, ma ognuna costa: si misura alla chiusura | Il borgo è il livello più grande e più illuminato del gioco, e lo si vede a ogni ritorno |
| D17 | **Fuori da questa milestone** | Il giorno e la notte che cambiano; abitanti che camminano o parlano; missioni; altri mercanti (fabbro, guaritrice); i filmati (M19); la risalita e le stanze più grandi (M20) | Ognuna è un lavoro a sé. Altri mercanti, come in Diablo, sarebbero una voce da mettere nel piano |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 18.1 | I modelli: i pacchetti del borgo (con il sì), le animazioni e i personaggi dagli zip, la grotta, il cunicolo e le case sfondate in Blender, le foto dei pezzi (D6, D7, D8) | 2–3 |
| 18.2 | Il tileset del borgo e la mappa: case, piazza, cappella, cimitero, bordo, paesaggio oltre il bordo; la foto della disposizione per Mirco (D2, D3, D4, D14) | 2–3 |
| 18.3 | Il tramonto: il sole, la luce ambiente, la nebbia, il post-processing, il filtro retrò; le foto per Mirco (D5, D14) | 1–1,5 |
| 18.4 | La grotta e il cunicolo: l'uscita verso la profondità 1, il modello sulla cella d'ingresso, i test sui semi fissi (D6, D7, D13) | 1–1,5 |
| 18.5 | Gli abitanti, il suono, il nome e il salvataggio vecchio (D9, D10, D11, D12) | 1–1,5 |
| 18.6 | Misura, foto, build da provare e chiusura: ADR, lezioni, piano, README, `bundleVersion` 1.6.0, tag `v1.6.0` dopo i test verdi in CI (D16) | 1–1,5 |

---

## Checklist di chiusura

- [ ] Decisioni D1–D17 confermate (D2, D5 e D7 scelte da Mirco)
- [ ] I download approvati: i pacchetti del borgo
- [ ] La disposizione e il borgo finito approvati da Mirco in foto
- [ ] Test EditMode e PlayMode verdi in locale e in CI
- [ ] La build misurata dentro il limite della D16
- [ ] ADR, lezioni nel piano, README, `CREDITS.md` e `Data/Credits.txt`
- [ ] `bundleVersion` 1.6.0 e tag `v1.6.0` dopo i test verdi in CI
