# M7 — "Le profondità"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI**, dalla
scala del livello 4 si scende nelle **caverne**: i livelli 5–8 sono spazi organici generati
a ogni partita con un **random walk**, sempre percorribili dall'ingresso alla scala, con un
aspetto loro (terra, roccia, macerie). Ci vivono due nemici nuovi: lo **sciame**, veloce e
fragile, che arriva in gruppo, e il **bruto**, lento e resistente, che carica un colpo
**telegrafato** da cui ci si può spostare in tempo. Un colpo del bruto **interrompe**
l'attacco del cavaliere, uno dello sciame no. Un **colpo critico** del cavaliere fa il
doppio del danno, si riconosce dal numero e fa urlare il nemico con un **verso suo**, diverso
per tipo. Lo scheletro si comporta come prima, ma la sua IA è fatta di **classi di stato**.
Con dieci nemici che colpiscono insieme l'audio non si satura. L'automappa si riempie anche
nelle caverne. Nel buio suonano una musica cupa e un fondo di vento e gocce, diversi tra
cripta e caverne, e ogni tanto un verso lontano. Con `-seed 4711` le caverne tornano uguali.
Test verdi in CI.

**Tempo stimato:** 13–20 h (piano v2.17: 2–3 h in più per il passo 7.0, 1–1,5 h per il
7.7). **Prerequisito:** M6 chiusa (tag `m6`).

**Come si lavora:** come alla M6, il codice e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. A Mirco restano le decisioni qui sotto, le prove in Play Mode e
in build e la revisione degli ADR.

**Punto di controllo:** alla chiusura della M8 (piano § 1.3), non di questa.

---

## Stato verificato il 5 ottobre 2026

| Cosa | Stato |
|---|---|
| Livelli | Cripta generata (ADR-033, ADR-034): `Level_Crypt` con `DungeonLevel`, che genera con `DungeonGenerator` e `DungeonPopulator` dai numeri di `CryptSettings` e costruisce con `LevelBuilder`. `LastDepth` è 4: il livello 4 non ha la scala |
| Griglia | Celle da 4 m, `LevelMap` con i marcatori; `DungeonLayout` accetta marcatori su celle libere. Builder, NavMesh, automappa, esplorazione e finestra dell'editor lavorano sulla griglia, non sul generatore |
| NavMesh | Cotto a runtime dai collider, 8–16 ms a livello in build (ADR-035) |
| Nemici | Uno solo: lo scheletro (`Skeleton.prefab`, modello `Skeleton_Minion` con `Skeleton_Blade`). `EnemyAI` è un `enum` con uno `switch` (Idle, Chase, Attack, Dead), 178 righe, con aggro a 8 m e vista bloccata dai muri. `HitRecovery` (soglia 20% della vita massima, 0,5 s) c'è solo sui nemici |
| Modelli disponibili | Nello zip *KayKit Skeletons 1.1* già scaricato: `Skeleton_Rogue`, `Skeleton_Warrior`, `Skeleton_Mage`, armi (`Skeleton_Axe`, scudi grandi e piccoli). Nel *Dungeon Pack*: pavimenti di terra (`floor_dirt_large`, `floor_dirt_large_rocky`, `floor_dirt_small_*`), muri rotti e crepati, macerie, colonne |
| Audio | `CharacterAudio` per personaggio: un `AudioSource` 3D sul mixer SFX, clip a caso tra quelle del suo evento. Nessun limite di voci |
| Loot | `LootTable` per tipo di nemico con la probabilità della pozione (ADR-030, ADR-037); seme dalla cella del nemico |
| Assembly | `DarkDescent.Core` per logica e dati, `DarkDescent` per i componenti (ADR-032) |
| Test | 132 EditMode e 103 PlayMode verdi |

---

## Decisioni

Tutte confermate da Mirco il 5 ottobre 2026, con le proposte consigliate: celle da 4 m, settore rosso a terra, stati in `Core`.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Algoritmo delle caverne** | **Random walk** sulla stessa griglia da 4 m: alcuni camminatori partono dal centro e scavano finché il pavimento è circa il **40%** dell'area (28 × 28 celle, come la cripta). Poi una passata di **smussatura** (automa cellulare: si riempie la roccia isolata e si toglie il pavimento a spillo) e si tiene la **regione connessa più grande**. Ingresso dove il primo camminatore è partito, scala nella cella più lontana a piedi, come nella cripta | Sulla stessa griglia funziona tutto quello che c'è: builder, NavMesh, automappa, ripartenza, finestra dell'editor. Il random walk dà corridoi larghi e irregolari, l'opposto delle stanze del BSP, ed è l'algoritmo del piano |
| D2 | **Un generatore per tipo di livello** | `CaveGenerator` accanto a `DungeonGenerator`, tutti e due in `Core`, che producono lo stesso `DungeonLayout`. Le impostazioni dicono quale si usa: `CaveSettings` è un `DungeonSettings` con i numeri del random walk. `DungeonLevel` non cambia, se non per chiedere il generatore alle impostazioni | Il gioco, la finestra dell'editor e i test non devono sapere quale algoritmo ha fatto la mappa. Un terzo tipo di livello (la città della M10) sarà un'altra coppia |
| D3 | **Come si arriva alle caverne** | Il livello 4 della cripta riceve la scala, verso una scena `Level_Caves` alla profondità 5. Le caverne vanno dal 5 all'8, e l'8 per ora non ha la scala (il boss è alla M10) | Stesso meccanismo della cripta (D9 della M6): una scena con atmosfera e luci sue, che il `LevelManager` carica con una profondità |
| D4 | **Aspetto delle caverne** | Pavimenti di terra del *Dungeon Pack* (`floor_dirt_large`, variante con i sassi a caso), muri `wall_broken` e `wall_cracked` sui lati alti e macerie (`rubble_half`) su quelli bassi (ADR-017), qualche colonna rotta e mucchio di macerie come scenografia. Niente torce sui muri: qualche **candela** a terra, e luce ambiente un po' più fredda. Un `LevelTileset` per le caverne accanto a quello della cripta | Con i modelli già nel pacchetto CC0 non serve niente di nuovo. Senza torce le caverne sono più buie della cripta, e il cavaliere conta sulla sua luce |
| D5 | **Sciame** | Modello `Skeleton_Rogue` a scala 0,85, con un pugnale. **Velocità 1,6 volte** lo scheletro, **vita un terzo**, danno basso, attacco rapido. Arriva in **gruppi da 4 a 6** nella stessa zona, e si attiva tutto insieme quando uno vede il cavaliere. Evitamento tra agent con priorità diverse, così non si incastrano in un corridoio | È il comportamento dell'archetipo nel § 2: tanti colpi piccoli, che il blocco e l'Armatura assorbono bene. L'attivazione di gruppo evita di trovarli a uno a uno |
| D6 | **Bruto** | Modello `Skeleton_Warrior` a scala 1,3, con `Skeleton_Axe`. **Velocità 0,6**, **vita 3 volte**, danno alto. Il colpo si **carica per 0,9 s**: il bruto si ferma, alza l'ascia, e a terra compare un **settore rosso** davanti a lui. Il colpo arriva solo a chi è ancora dentro al settore alla fine (il controllo al momento del colpo c'è già dalla M2). Dopo il colpo resta fermo 0,6 s, la finestra per colpirlo | Il telegrafare è quello che l'archetipo insegna: la finestra di reazione si vede e si prova. Il settore a terra si legge anche nel buio delle caverne, l'animazione da sola no |
| D7 | **Reazione al colpo del cavaliere** | `HitRecovery` anche sul cavaliere, con una soglia del **20% della vita massima**: il bruto la supera, lo sciame e lo scheletro no. Interrompe l'attacco in corso e il movimento per 0,4 s (ADR-010) | La soglia c'è già sui nemici e vale per lo stesso motivo: i colpi piccoli non devono bloccare il giocatore, il colpo del bruto sì, ed è il motivo per spostarsi dal settore |
| D8 | **IA a classi di stato** | `EnemyAI` diventa un componente sottile che esegue uno **stato** alla volta. Gli stati (`Idle`, `Chase`, `Attack`, `WindUp`, `Recover`, `Dead`) sono classi di logica in `Core` che parlano con il nemico attraverso un'interfaccia (`IEnemyBody`: dove sta, dove va, colpisce, vede il bersaglio). Scheletro, sciame e bruto sono **combinazioni di stati e numeri** in uno ScriptableObject `EnemyArchetype`. Prima il refactor con lo scheletro identico, verificato dai test che ci sono, poi i nemici nuovi | Con tre comportamenti lo `switch` cresce in tutti i rami; con le classi un comportamento nuovo è uno stato nuovo. In `Core` gli stati si provano in EditMode con un corpo finto, senza scene. È il refactor "guidato da un bisogno reale" del piano |
| D9 | **Limite di voci audio** | Un `SfxLimiter` nella scena `Core`: al più **una clip per tipo di suono per fotogramma** (impatto, morte, colpo a vuoto), e oltre **12 voci** in tutto si scartano le più lontane. Priorità dell'`AudioSource` dalla distanza dal cavaliere | Dieci colpi dello sciame nello stesso fotogramma suonano come uno più forte, e con il limite Unity non toglie a caso quelli vicini |
| D10 | **Nemici per profondità** | Una `SpawnTable` nelle impostazioni del livello: per ogni profondità quanti **gruppi** e di che tipo, con un peso. Caverne: profondità 5 circa 5 scheletri, uno sciame e un bruto; profondità 8 circa 4 scheletri, tre sciami e tre bruti. Numeri in `CaveSettings`, ritarati alla M10 | Il mix cambia scendendo, e il bruto arriva quando il cavaliere ha già trovato qualche oggetto. I numeri fuori dal codice si cambiano provando la build |
| D12 | **Musica e rumori d'ambiente** (aggiunta il 6 ott, su richiesta di Mirco dopo la prova della M6; anticipati dalla M11) | Un profilo per tipo di livello nel tileset: musica cupa e fondo di vento e gocce in loop, versi singoli ogni 20–50 s da un punto a caso a 10–18 m dal cavaliere. Tracce CC0 da OpenGameArt, ispirate a Diablo ma niente musica originale (piano § 1.1). Dissolvenza tra profili diversi, la musica continua tra livelli con lo stesso profilo | Il buio fa paura se si sente qualcosa che non si vede. In 3D e oltre la luce del cavaliere, il verso arriva da una direzione dove non c'è niente da guardare |
| D13 | **Colpi critici e versi dei nemici** (aggiunta e confermata da Mirco il 6 ott) | Solo il cavaliere, solo sui colpi a segno: probabilità **5% + Destrezza / 10**, al massimo 50% (7% con la Destrezza 20 di partenza), **danno doppio**. Il tiro va dopo quello del danno, e il critico esce dalla parte **alta** dell'intervallo (trappola 9). `DamageInfo.IsCritical` c'è dalla M2 e non è mai stato usato. A schermo: numero più grande, giallo-arancio, con un punto esclamativo, e un hit stop un po' più lungo. A orecchio: ogni `EnemyArchetype` (D8) ha i suoi **versi del critico**, scelti a caso senza ripetere il precedente. Scheletro: un verso secco, intonazione 1,1–1,2. Sciame: uno stridio, 1,4–1,6. Bruto: un ruggito basso, 0,6–0,7. Per cominciare sono ritagliati dai versi delle tracce CC0 già nel progetto. Se non convincono, si cerca un pacchetto CC0 di versi di mostri, da scaricare con il permesso di Mirco. Niente affissi sul critico per ora: un affisso nuovo cambia il loot dei semi provati (trappola 10) | Il critico dà un picco nel ritmo del combattimento, e il verso dice subito *chi* l'ha preso anche nel buio. La Destrezza oggi conta per colpire e bloccare: con il critico pesa anche sul danno. Danno doppio come il guerriero di Diablo 1 |
| D14 | **Correzioni dalla prova della build** (7 ott, chieste da Mirco dopo aver provato la build della M7) | Prima del tag: l'etichetta "(inventario pieno)" sparisce quando si libera un posto; il cursore attraversa colonne, barili e muri bassi per arrivare a un nemico o a una cosa da usare (i muri alti no); il bruto alterna il colpo forte con 1–3 colpi leggeri da 8–12, senza settore; una scritta dice tipo e profondità del livello; passi del cavaliere con il suono del pavimento; un verso a ogni colpo subito dai nemici, da un pacchetto CC0 scaricato con il permesso di Mirco. Elmo, armatura, guanti, stivali, anelli e amuleto vanno nella M8, prima del salvataggio (piano v2.19). Zoom e rotazione della visuale restano nella M8 (v2.18) | Sono quello che manca giocando: due bug di interfaccia, un bruto prevedibile, nessun riferimento su dove si è, un cavaliere muto |
| D15 | **Sciame e morte** (7 ott, dopo la stessa prova) | Lo sciame era troppo forte: ogni colpo bloccato fermava il cavaliere 0,45 s e annullava il suo fendente, e in mezzo al gruppo succedeva circa una volta al secondo. Il blocco non ferma più (ADR-026 cambiato). Mira più facile: nome e vita del nemico sotto il cursore in alto, un cerchio rosso ai suoi piedi, un click a meno di 0,6 m da un nemico lo colpisce, il cavaliere non viene spinto dagli agent dei nemici. Sciame a gruppi da 3 a 5 e un colpo ogni 1,2 s. Morte: il livello non si ricarica; il cavaliere torna in vita all'ingresso, con l'inventario, i nemici uccisi, le casse aperte e la mappa di prima; i nemici vivi tornano fermi dove li ha messi il livello, con la vita che hanno (sostituisce D13 della M6) | Scelte di Mirco. Il limite di tre attaccanti insieme, proposto, non l'ha voluto |
| D11 | **Loot dei nemici nuovi** | Una `LootTable` per tipo: sciame **20%** di lasciare un oggetto e 10% una pozione; bruto **sempre** un oggetto e 50% una pozione. Basi e rarità come lo scheletro, livello dell'oggetto dalla profondità | Lo sciame è tanti nemici piccoli: con la probabilità dello scheletro il pavimento si riempirebbe. Il bruto è un premio |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 7.0 | Musica e rumori d'ambiente (D12) | 2–3 |
| 7.1 | Generatore delle caverne (logica pura) e test sui 500 semi | 1,5–2 |
| 7.2 | Caverne nel gioco: tileset, scena `Level_Caves`, scala dal livello 4, finestra dell'editor | 1,5–2 |
| 7.3 | IA a classi di stato, scheletro invariato | 1,5–2 |
| 7.4 | Sciame | 1–1,5 |
| 7.5 | Bruto e colpo telegrafato | 1,5–2 |
| 7.6 | Reazione al colpo del cavaliere | 0,5–1 |
| 7.7 | Colpi critici e versi dei nemici (D13) | 1–1,5 |
| 7.8 | Limite di voci audio | 0,5–1 |
| 7.9 | Nemici e loot per profondità | 1 |
| 7.10 | Correzioni dalla prova della build (D14) | 2–3 |
| 7.11 | Sciame e morte (D15) | 2 |
| 7.12 | Chiusura: build da provare, GIF, ADR, tag `m7` | 0,5 |

---

## Passo 7.0 — Musica e rumori nel buio

1. Tracce CC0 da OpenGameArt, con l'autorizzazione di Mirco al download: una musica per la
   cripta, una per le caverne, un fondo in loop. Righe in `CREDITS.md`.
2. Versi singoli ritagliati dalle tracce dove il segnale esce dal fondo.
3. `AmbienceProfile` (dati) e `StingerSchedule` (logica pura) in `Core`, `AmbiencePlayer` in
   `Core.unity`, profilo nel `LevelTileset` e nel `LevelContext`.
4. Test: profili completi, musica e fondo nella cripta, musica che continua scendendo,
   dissolvenza tra profili, versi in 3D alla distanza giusta e mai uguali di fila.

**Com'è andata (6 ott 2026).** Tre tracce, 4,5 MB. Per la cripta *Dungeon Ambience* di yd:
206 s, molto bassa (mediana −43 dB, picco −19 dB), con le sue dissolvenze in testa e in coda.
Per le caverne la versione in loop di *Dark Cavern Ambient* di Paul Wortmann: 120 s, forte
(mediana −17 dB), con dentro versi di mostri. Sotto a tutte e due il fondo di *Loopable
Dungeon Ambience* di JaggedStone (vento basso e gocce, mediana −30 dB), che copre anche il
silenzio quando la musica della cripta ricomincia. Un'analisi in batch dell'inviluppo, a
finestre di 50 ms contro la mediana mobile, ha trovato dieci picchi; ne sono rimasti otto:
uno si sovrapponeva a un altro, uno usciva dal fondo di soli 9 dB. Sono ritagliati in WAV
mono, da 1,25 a 5,85 s, con dissolvenze di 0,15 e 0,6 s e normalizzati a −3 dB. Volumi: il
gruppo `Music` del mixer è a +14 dB, la musica della cripta a 1, quella delle caverne a 0,08,
il fondo a 0,15; così le due musiche hanno la mediana tra −29 e −26 dB in uscita. I versi
nel buio partono ogni 20–50 s nella cripta e ogni 30–70 s nelle caverne, dove la musica ne ha
già. Ogni verso nasce a 10–18 m dal cavaliere, oltre la sua luce, nel gruppo `SFX`, con un
passa-basso a 2,2 kHz che lo fa suonare dietro la roccia e l'intonazione tra 0,75 e 1,05.
La cripta ha il suo profilo dal `DungeonTileset`; `AmbienceCaves` è pronto per il tileset
delle caverne del passo 7.2. I livelli fatti a mano e la sandbox usano il profilo predefinito
dell'`AmbiencePlayer`, quello della cripta. `StingerSchedule.NextIndex` all'inizio non poteva
scegliere l'ultimo verso: un test sui bordi l'ha trovato prima del commit. ADR-039. Test:
`StingerScheduleTests` (4) e `AmbienceTests` (5). In batchmode le sorgenti suonano davvero,
e il test verifica che la musica vada avanti tra il livello 1 e il 2. 146 EditMode e 109
PlayMode verdi.

## Passo 7.1 — Generatore delle caverne

1. `CaveGenerator` (logica pura, solo interi): camminatori dal centro, smussatura, regione
   connessa più grande, ingresso e scala con una visita in ampiezza.
2. `CaveSettings` con area, percentuale di pavimento, camminatori, passate di smussatura.
3. Test su **500 semi**: tutto il pavimento raggiungibile dall'ingresso, pavimento tra il 35 e
   il 45%, niente celle di roccia isolate in mezzo al pavimento, scala ad almeno metà della
   distanza massima, mappa dentro i bordi, stesso seme stessa mappa. Gli stessi controlli della
   cripta, quindi le funzioni di verifica si condividono.

**Com'è andata (6 ott 2026).** `CaveGenerator` (logica pura, `Core/Levels`) produce lo stesso
`DungeonLayout` della cripta, senza stanze. Le impostazioni scelgono l'algoritmo:
`DungeonSettings.CreateGenerator()` dà il BSP, `CaveSettings` (che ne eredita misure e contenuto)
lo sostituisce con il random walk, attraverso l'interfaccia `ILevelGenerator`; `DungeonLevel` non sa
più quale generatore usa. La visita in ampiezza della cripta è passata in `LevelGrid`, comune ai
due, e le mappe della cripta non sono cambiate. **Algoritmo:** quattro camminatori partono dal
centro e scavano a turno finché il pavimento è il 37% dell'area dentro il bordo; ogni tanto uno
riparte da una cella già scavata, ogni tanto scava un quadrato di 2 × 2. La prima versione, a passi
del tutto casuali, faceva una macchia unica, più una sala che una caverna: ora ogni camminatore
tiene la direzione due volte su tre, e scava cunicoli che si allargano dove si incrociano. Poi due
passate di smussatura (la roccia con sei vicini di pavimento su otto si riempie, le punte si
tolgono), le celle unite solo in diagonale si collegano (trappola 2), i pilastri di una cella si
riempiono (trappola 3) e resta la regione connessa più grande. L'ingresso è la cella aperta (otto
vicini di pavimento) più lontana dal centro, la scala la cella più lontana dall'ingresso con la
roccia a nord e il pavimento a sud, come nella cripta. Su 500 semi il pavimento va dal 36,4 al
42,9%, e la scala è ad almeno 24 passi. Il seme 4711:

```
####>.........#####.......##
####.####.....#####.......##
####.####.....##########..##
####.####.....##########..##
####.####.....##########..##
###............#########..##
####..###......#########..##
####..####.....####.......##
####..####.............##..#
####..####.......##...###..#
####.######......##..####.##
####.#####...........####..#
####.................####..#
###.................#####..#
###..................#######
#######...###........#######
#######..#####......########
#######..#####......########
#######..#####....<.########
#######..####.......########
#######..###....############
#######..####..#############
#######..####..#############
#######...###..#############
#######..###################
```

(senza le righe di sola roccia). Test: `CaveGeneratorTests`, sei test, quelli su 500 semi in 0,6 s
in tutto (impostazioni che scelgono l'algoritmo, tutto raggiungibile, pavimento tra 35 e 45%,
niente pilastri né contatti solo in diagonale, ingresso aperto e scala lontana verso sud, stesso
seme stessa caverna). Le visite dei test sono in `MapChecks`, comune con quelli della cripta. 142
EditMode e 104 PlayMode verdi.

## Passo 7.2 — Caverne nel gioco

1. Tileset delle caverne (D4): modelli estratti dallo zip del *Dungeon Pack*, prefab con i
   collider per il NavMesh (ADR-035).
2. Scena `Level_Caves` costruita dallo strumento dell'editor, con atmosfera e luce sue.
3. Scala del livello 4 verso la profondità 5; il livello 8 senza scala.
4. Finestra dell'editor: scelta tra cripta e caverne.
5. Il tileset delle caverne riceve `AmbienceCaves` (passo 7.0).
6. Test: discesa dal 4 al 5 e fino all'8, nemici sul NavMesh, stesso seme stessa caverna;
   l'automappa disegna le caverne.

**Com'è andata (6 ott 2026).**

*Concatenazione.* Le profondità si concatenano dalle impostazioni. `DungeonSettings` sa la sua
scena, la prima e l'ultima profondità e la scena dopo: la cripta va dall'1 al 4 e poi a
`Level_Caves`, le caverne dal 5 all'8 e poi a nessuna. `DungeonLevel.CreateMap` non riceve
più il nome della scena: prima dell'ultima profondità la scala riporta nella stessa scena,
all'ultima porta alla scena dopo, e senza una scena dopo la scala sparisce.

*Contenuto.* Lo mette `CavePopulator` (trappola 7). Ragiona su passi e celle, perché le
caverne non hanno stanze:
- candele (`l`) contro la roccia, una ogni 14 celle e ad almeno 3 celle l'una dall'altra:
  da 16 in su per caverna su 800 livelli provati;
- casse e scenografia contro la roccia, mai in un cunicolo largo una cella e mai dove chiudono
  il cammino;
- scheletri a gruppi da 2 a 4 attorno a centri distanti almeno 4 celle tra loro;
- casse e scheletri ad almeno 6 passi dall'ingresso.

Gli scheletri sono 2 × profondità (10 al 5, 16 all'8), le casse 1 al 5 e 2 dal 6. I numeri
veri, con sciame e bruto, arrivano al 7.9. I simboli di barile, casse e pilastro sono quelli
della cripta: il tileset delle caverne ci mette mucchi di sassi, un tavolo rotto e una colonna
rotta. Mappa, builder, automappa e finestra non cambiano.

*Tileset.* `CaveTileset` è una copia di quello della cripta con:
- terra (`floor_dirt_large`, e la variante con i sassi sul 30% delle celle);
- muri alti crepati (`wall_cracked`, variante `wall_broken`);
- macerie basse;
- ambiente un po' più freddo (0,20/0,22/0,30);
- il profilo sonoro `AmbienceCaves`.

Le varianti le sceglie il builder con una mescola della posizione, senza generatore di numeri:
lo stesso livello ha gli stessi pezzi anche costruito dall'editor. La candela è la torcia da
muro con `candle_triple` al posto del modello, 1,3 m verso la roccia, con una luce da 5 e
raggio 7 senza ombre.

*Correzione a D4.* `rubble_half` è un mucchio alto 3,5 m, non un muro basso. Schiacciato a
1,1 m lungo il lato fa le macerie basse; dimezzato fa il mucchio di sassi. Dallo zip sono
entrati tre modelli: i due pavimenti di terra e la candela tripla. Gli altri erano già nel
progetto dalla M3.

*Scena e finestra.* `Level_Caves` è la copia di `Level_Crypt` con impostazioni e tileset delle
caverne, nella build dopo la cripta. La finestra del generatore ha due pulsanti, *Cripta (1–4)*
e *Caverne (5–8)*, e limita la profondità a quelle del tipo scelto.

*Tempi.* Nei test la discesa dall'1 all'8 costa per livello 1–2 ms di generazione, 18–21 di
costruzione e 35–40 di NavMesh, poco più della cripta.

*Due intoppi, scoperti con le foto in batch.* Il primo (trappola 11): il pavimento non si
vedeva, e il cavaliere galleggiava nel nero. I tre FBX nuovi hanno la radice ruotata di −90°
su X e scalata 100 volte, quelli della M3 no; lo script li aveva forzati a rotazione zero e
scala 1, cioè un quadrato di 4 cm in piedi. Tolte le due sostituzioni dai prefab, e un test
controlla ora la misura vera di ogni pezzo del tileset. Il secondo: i riferimenti della scena
nuova si sono salvati vuoti. Gli asset creati poco prima nello stesso script erano stati
reimportati da `SaveAssets`, e i riferimenti in memoria non valevano più; scritti a mano nel
file della scena.

*Test.* `CaveContentTests` (6): concatenazione delle profondità; scheletri e casse quanti
dicono i numeri e lontani dall'ingresso; nessun ostacolo che tagli la caverna; candele contro
la roccia e distanti tra loro; misure del tileset; stesso seme stesso contenuto, senza cambiare
la forma. Gli ultimi quattro girano su 200 semi × 4 profondità. `CaveDescentTests` (2):
aspetto, luce, suono, automappa, nemici sul NavMesh e uscita verso il 6; stesso seme stessa
caverna. Aggiornati la discesa della cripta, che ora arriva all'8 attraverso le caverne, e il
test della finestra. 152 EditMode e 111 PlayMode verdi.

## Passo 7.3 — IA a classi di stato

1. `IEnemyBody` e gli stati in `Core/Enemies/`, `EnemyAI` che li esegue.
2. `EnemyArchetype` con gli stati e i numeri; quello dello scheletro riproduce i valori di oggi.
3. I test di `EnemyAITests` restano verdi senza cambiare; test EditMode degli stati con un corpo
   finto (vede, insegue, attacca a distanza, perde il bersaglio, muore).

**Com'è andata (6 ott 2026).** In `Core/Enemies/`:
- `IEnemyBody` con sei domande e comandi: il bersaglio è vivo, a portata, un colpo è in volo,
  si vede il bersaglio, chiedi il colpo, lascia il bersaglio;
- `EnemyStateBase`, con `Enter` e un `Tick` che restituisce lo stato successivo;
- `IdleState`, `ChaseState`, `AttackState` e `DeadState`;
- `EnemyBrain`, che tiene uno stato per ogni `EnemyState` e avvisa con `StateChanged`;
- `EnemyArchetype`.

L'enum `EnemyState` resta come nome dello stato, e i test e il resto del gioco continuano a
leggere `EnemyAI.State`.

`EnemyAI` è sceso a un corpo: implementa `IEnemyBody` (vista con un raggio, colpo chiesto a
`MeleeAttack`), passa il tempo al cervello e tiene quello che succede al corpo da morto. I
numeri (aggro 8 m, occhi a 1,5 m, uno sguardo ogni 0,2 s, corpo a terra 5 s) sono passati dal
prefab a `Data/Enemies/Skeleton.asset`; i layer che bloccano la vista restano sul componente,
perché sono fisica della scena e non del tipo di nemico.

L'ordine delle decisioni è quello dello `switch` (trappola 4): il cambio di stato vale dal
frame dopo, e il conto della percezione resta tra un'entrata e l'altra in `Idle`. Unica
differenza: morendo il nemico lascia anche il bersaglio, cosa che prima faceva solo spegnendo
`MeleeAttack`; non cambia niente a schermo.

Test: `EnemyStateTests` (7 casi, EditMode, con un corpo finto che conta sguardi e richieste).
Uno sguardo ogni 0,2 s e non a ogni frame; vede, insegue, attacca, torna a inseguire; un colpo
in volo porta all'attacco; bersaglio morto, sia inseguendo sia attaccando; morte da qualsiasi
stato e una volta sola; stati separati per ogni nemico. I test PlayMode dello scheletro sono
tutti verdi senza toccarli. 159 EditMode e 111 PlayMode verdi.

## Passo 7.4 — Sciame

1. Prefab dello sciame, archetipo, attivazione di gruppo.
2. Gruppi nel popolatore delle caverne.
3. Test: un gruppo si attiva insieme, nessuno resta incastrato in un corridoio largo una cella.

**Com'è andata (6 ott 2026).**

*Il prefab.* `Swarm` è lo scheletro con un altro modello, ricavato da uno script come i pezzi
delle caverne:
- `Skeleton_Rogue` (cappuccio rosso e mantello) a scala 0,85, con la lama dello scheletro
  accorciata a pugnale nella stessa mano;
- vita 10 contro 30;
- velocità 4,8 contro 3 e accelerazione 30;
- agent e capsula di raggio 0,35 invece di 0,45;
- colpo `SwarmStrike` da 1–3 ogni 0,9 s (lo scheletro 4–6 ogni 1,6 s);
- Destrezza 15, Armatura 5.

Il rig del Rogue ha le stesse ossa del Minion (trappola 8), quindi controller e animazioni
sono quelli dello scheletro. La sagoma rossa si distingue dagli scheletri anche nel buio.

*L'archetipo `Swarm`* aggiunge due numeri a `EnemyArchetype`:
- il raggio del branco, 12 m (allo scheletro 0);
- la variazione della priorità di evitamento, ±15 attorno a 50, scelta dall'istanza così che
  in un corridoio uno ceda il passo (trappola 5).

*Attivazione di gruppo.* `EnemyAI` avvisa con `Spotted` solo quando vede il cavaliere con i suoi
occhi. `EnemyPack`, creato dal composition root a ogni livello e rilasciato all'uscita, sveglia
i compagni dello stesso tipo entro il raggio con `EnemyBrain.Alert`, che vale solo da fermi e
con il bersaglio vivo. Chi è avvisato non avvisa a sua volta: si sveglia il gruppo, non la
caverna.

*Popolatore.* `CavePopulator` mette prima i gruppi di sciame (`w`), interi e da 4 a 6, attorno
a centri con abbastanza celle libere: il primo tentativo li metteva anche in un cunicolo, e un
test su 200 semi ha trovato un gruppo di 6 invece di 8–12. I gruppi crescono da 1 al 5 a 3
all'8 (1, 2, 2, 3); gli scheletri sono 5 a ogni profondità finché la tabella del 7.9 non dà i
numeri di D10.

*Bottino.* `SwarmLoot` c'è già con i numeri di D11 (20% un oggetto, 10% una pozione), perché il
prefab ne vuole uno; il 7.9 lo ricontrolla con le probabilità su 10.000 tiri.

*Test.*
- `SwarmTests` (PlayMode) usa una mappa di prova costruita nel test: sei dello sciame, un
  cunicolo largo una cella, la stanza del cavaliere e altri tre chiusi nella roccia. Il gruppo
  si sveglia tutto anche se a vederlo sono stati uno o due; quello chiuso resta fermo; i sei
  passano il cunicolo e raggiungono il cavaliere in 6,5 s. Un terzo test confronta i numeri
  con lo scheletro.
- `EnemyStateTests` ha un caso in più, l'avviso.
- `CaveContentTests` controlla i gruppi di sciame su 200 semi × 4 profondità e la loro crescita
  con la profondità.

161 EditMode e 115 PlayMode verdi.

## Passo 7.5 — Bruto

1. Prefab del bruto, stati `WindUp` e `Recover`, settore a terra.
2. Test: fuori dal settore alla fine della carica il colpo non arriva, dentro sì; dopo il
   colpo il bruto resta fermo.

**Com'è andata (6 ott 2026).**

*Il colpo telegrafato* è il colpo di sempre, con due aggiunte:
- `WeaponDefinition` ha un arco in gradi (360 per tutte le armi di prima, quindi niente cambia);
- `MeleeAttack`, al momento del danno, ricontrolla anche l'angolo e avvisa con `SwingEnded` quando
  un colpo finisce, a vuoto o annullato.

Durante la carica `MeleeAttack` già non insegue e non gira, quindi chi passa di lato o dietro
ne esce davvero. Gli stati nuovi sono `WindUpState`, che aspetta la fine del colpo, e
`RecoverState`, fermo per il tempo dell'archetipo e senza chiedere colpi. `AttackState` del
bruto passa alla carica appena il colpo parte. Un archetipo con un tempo di recupero è
telegrafato e prende questi stati; lo scheletro e lo sciame no.

*Il bruto.* `Brute` è lo scheletro con `Skeleton_Warrior` (stesse ossa, trappola 8) a scala 1,3
e `Skeleton_Axe` in mano:
- velocità 1,8, cioè 0,6 volte lo scheletro (D6), accelerazione 12, rotazione lenta (300°/s);
- vita 90, tre volte lo scheletro;
- `BruteStrike`: 12–18 danni, portata 1 m + 0,4, arco 100°, carica 0,9 s, un colpo ogni 2,4 s;
- recupero 0,6 s;
- Destrezza 5, Armatura 20;
- `BruteLoot` già con i numeri di D11: sempre un oggetto, metà delle volte una pozione.

L'animazione d'attacco del controller condiviso prende la velocità dal parametro nuovo
`AttackSpeed`, che vale 1 per tutti; il bruto la porta a 0,47, così l'ascia si alza piano e
cade insieme al danno.

*Il settore.* `TelegraphSector` costruisce in `Awake` un ventaglio dell'arco dell'arma, di
raggio pari alla portata vera (raggio del bruto + portata + margine + corpo del cavaliere,
2,45 m). Il settore appare con `SwingStarted`, si riempie dal centro durante la carica e
sparisce con `SwingEnded`. I due materiali, `M_Telegraph` e `M_TelegraphFill`, sono URP Unlit
trasparenti: si leggono nel buio delle caverne, e la foto in batch lo conferma (trappola 6).

*Popolatore.* I bruti sono da soli, ognuno con il suo centro, da 1 al 5 a 3 all'8 come in D10
(1, 2, 2, 3).

*Test.*
- `BruteTests` (PlayMode), su una mappa di prova con una stanza:
  - numeri contro lo scheletro;
  - il cavaliere passa dietro il bruto durante la carica: dentro la portata ma fuori dall'arco,
    il colpo va a vuoto, il settore sparisce, e il bruto resta fermo e girato per tutto il
    recupero;
  - chi resta nel settore prende almeno 12.
- `EnemyStateTests`: carica e recupero, colpo annullato, lo scheletro che non carica.
- `CaveContentTests`: i bruti su 200 semi × 4 profondità.

164 EditMode e 117 PlayMode verdi.

## Passo 7.6 — Reazione al colpo del cavaliere

1. `HitRecovery` sul cavaliere con la soglia di D7; animazione e interruzione del click.
2. Test: il colpo del bruto interrompe l'attacco, quello dello scheletro no.

**Com'è andata (6 ott 2026).** Il cavaliere ha ora `HitRecovery`, con soglia 20% della vita
massima e blocco di 0,4 s (D7). A partire da 100 di vita la soglia è 20.

*Il danno del bruto.* Con i 12–18 messi al 7.5 il bruto non l'avrebbe mai superata, e D7 dice
che il bruto la supera: `BruteStrike` passa a **20–28**. Così un colpo del bruto toglie un
quarto della vita, il prezzo di non essersi spostati dal settore. Scheletro (4–6) e sciame
(1–3) restano sotto. Un test lo controlla sui dati, così un ritocco ai numeri che rompe la
regola si vede subito.

*Cosa succede quando il colpo supera la soglia.* Il fendente in corso viene annullato e il suo
danno non arriva. L'animazione è quella del colpo subito, già agganciata a `Staggered`. Il
cavaliere si ferma.

*Cosa cambia nel codice.* `PlayerController` mette in coda il click di movimento anche durante
il blocco, non solo durante un fendente, e lo fa partire alla fine. In più
`MeleeAttack.Interrupt` azzera la velocità dell'agent: il primo test ha misurato 31 cm di
scivolata per la frenata, ora il cavaliere si ferma sul posto. Vale anche per i nemici fermati
da un colpo forte.

*Test.* `PlayerHitRecoveryTests` (PlayMode, 3):
- la soglia separa il bruto dagli altri, sui dati dei prefab;
- un colpo da 6 durante il fendente non lo ferma, uno da 24 lo annulla e il danno non arriva;
- fermato mentre cammina, il cavaliere resta fermo entro 5 cm per 0,3 s anche con un click, e
  finito il blocco parte verso il punto cliccato.

164 EditMode e 120 PlayMode verdi.

## Passo 7.7 — Colpi critici e versi dei nemici

1. `CombatFormulas.CritChance` e `RollCrit` in `Core`; `MeleeAttack` del cavaliere tira il
   critico dopo il danno, raddoppia e riempie `DamageInfo.IsCritical`.
2. Numero del danno grande e colorato, hit stop un po' più lungo.
3. Versi del critico nell'`EnemyArchetype` di scheletro, sciame e bruto; `CharacterAudio` li
   suona al posto dell'impatto quando il colpo è critico.
4. Pannello del personaggio: la probabilità di critico tra le statistiche, con il suo tooltip
   (stile del passo 6.10), in inglese e in italiano.
5. Test: la formula ai bordi, critico solo dalla parte alta del tiro, danno doppio, verso
   giusto per tipo di nemico, i test dei colpi esistenti invariati.

**Com'è andata (6 ott 2026).**

*Tiro e danno.* `CombatFormulas.CritChance` dà 5% + Destrezza / 10, al massimo 50%;
`RollCrit` esce dalla parte alta del tiro (trappola 9). `MeleeAttack` ha `_canCrit`, acceso
solo sul prefab del cavaliere: dopo il tiro del danno tira il critico, raddoppia e riempie
`DamageInfo.IsCritical`, il campo che aspettava dalla M2. Tutti i test dei colpi già scritti
sono passati senza cambiare un numero.

*A schermo.* Il numero del critico è arancio, una volta e mezza più grande, con il punto
esclamativo. Il numero torna dal pool, quindi la scala si rimette a ogni uso. L'hit stop del
critico dura 0,12 s invece di 0,05.

*I versi.* Sono nell'`EnemyArchetype`. Per ora sono i versi più brevi tra quelli ritagliati
al 7.0 dalle tracce CC0, senza altri download, con l'intonazione del tipo:
- scheletro: 1,1–1,2;
- sciame: 1,4–1,6, uno stridio;
- bruto: 0,6–0,7, un ruggito lungo.

`CharacterAudio` li suona al posto dell'impatto, mai lo stesso due volte di fila. Se a
orecchio non convincono, il passo dopo è un pacchetto CC0 di versi, con il permesso di Mirco.

*Il pannello del personaggio.* Una riga in più, *Critical chance* / *Critico*, tra la
probabilità di colpire e il blocco, con il suo tooltip; anche il tooltip della Destrezza
spiega il critico. La foto in batch mostra che la finestra la contiene.

*Test.*
- `CombatFormulasTests`: probabilità ai bordi e tiro dalla parte alta.
- `CriticalHitTests` (PlayMode): solo il cavaliere fa critici, ogni nemico ha i suoi versi
  con le intonazioni in ordine. Un critico vero sullo scheletro: danno doppio, uno dei suoi
  versi con l'intonazione giusta, il numero con il punto esclamativo e più grande, l'hit stop
  ancora attivo dopo 0,08 s, il 7% nel pannello.
- Il test dei tooltip passa da 10 a 11 righe.

166 EditMode e 122 PlayMode verdi.

## Passo 7.8 — Limite di voci audio

1. `SfxLimiter` e la priorità dalla distanza; `CharacterAudio` gli chiede il permesso. Il
   verso del critico passa prima dell'impatto dello stesso nemico.
2. Test: dieci impatti nello stesso fotogramma suonano una volta; oltre il limite cade il più
   lontano.

**Com'è andata (6 ott 2026).** La regola sta in `SfxBudget`, in `Core`: logica pura, con
fotogramma, tempo e distanza passati da fuori, e nessuna allocazione dopo la costruzione.
- Al più un suono per tipo (`SfxKind`) per fotogramma.
- Al più 12 voci insieme. Oltre, un suono nuovo passa solo se è più vicino del più lontano tra
  quelli in corso, e ne prende il posto; un suono finito libera il suo.
- La priorità di Unity va da 64 a un passo dal cavaliere a 255 lontano: se Unity deve togliere
  una voce, toglie quella lontana.

`SfxLimiter`, in `Core.unity`, misura la distanza dall'`AudioListener` sulla testa del
cavaliere e conta il tempo reale, perché durante l'hit stop il gioco è fermo ma i suoni no.
`CharacterAudio` gli chiede il permesso per fendente, impatto, morte, blocco, pozione e verso
del critico; il composition root lo collega al cavaliere e ai nemici di ogni livello. Senza
limite collegato, in una scena di prova, si suona come prima. Il verso del critico è un tipo a
sé: passa anche nel fotogramma in cui un altro nemico fa un impatto. Musica, fondo e versi nel
buio hanno sorgenti loro e non contano tra le 12 voci (ADR-039).

Test: `SfxBudgetTests` (EditMode, 4) e `SfxLimiterTests` (PlayMode, 2). Il primo PlayMode
mostra dieci dello sciame colpiti nello stesso fotogramma: dieci richieste e un impatto solo,
e il fotogramma dopo l'impatto passa. Il secondo mostra la sorgente vicina con priorità più
alta di quella lontana, e il cavaliere collegato al limite. 170 EditMode e 124 PlayMode verdi.

## Passo 7.9 — Nemici e loot per profondità

1. `SpawnTable` in `CaveSettings`, loot table di sciame e bruto.
2. Test su 200 semi per profondità: numeri della tabella, nessun nemico nella zona
   d'ingresso; probabilità dei drop su 10.000 tiri.

**Com'è andata (6 ott 2026).** `SpawnTable` è in `CaveSettings`. Ogni riga (`SpawnRow`) vale
dalla sua profondità in giù e dice da quanti a quanti gruppi di scheletri e di sciame, e quanti
bruti.

*Differenza da D10.* Intervalli invece dei pesi: con i pesi un livello 5 poteva uscire senza
bruto o con tre, mentre il primo bruto deve arrivare al 5.

*Le righe:*

| Profondità | Gruppi di scheletri | Gruppi di sciame | Bruti |
|---|---|---|---|
| 5 | 2 | 1 | 1 |
| 6 | 2 | 1–2 | 1–2 |
| 7 | 1–2 | 2 | 2 |
| 8 | 1–2 | 3 | 3 |

I gruppi di scheletri delle caverne sono da 2 a 3. Il popolatore tira gli intervalli e mette
prima lo sciame, poi i bruti, poi gli scheletri; il conto per profondità della 7.4 è tolto.
Medie su 200 semi:
- al 5: 4,9 scheletri, 5,0 dello sciame (un gruppo), 1 bruto;
- all'8: 3,7 scheletri, 15,1 dello sciame (tre gruppi), 3 bruti.

Sono i numeri di D10.

*Bottino.* Le tabelle di sciame e bruto erano già nate con i loro prefab (7.4 e 7.5); su 10.000
tiri danno:
- sciame: 20% un oggetto, 10% una pozione;
- bruto: sempre un oggetto, metà delle volte una pozione.

Il livello dell'oggetto è la profondità.

*Test.*
- `CaveContentTests`: ogni tipo negli intervalli della sua riga su 200 semi × 4 profondità, le
  medie di D10, la riga giusta per ogni profondità.
- `LootTests`: le probabilità di D11 su 10.000 tiri.
- `CaveDescentTests`: il livello 5 accetta da 4 a 6 scheletri.

172 EditMode e 124 PlayMode verdi.

## Passo 7.10 — Correzioni dalla prova della build

1. L'etichetta "(inventario pieno)" torna il solo nome quando l'inventario cambia.
2. Il cursore attraversa gli ostacoli bassi per arrivare a nemici e cose da usare.
3. Colpi leggeri del bruto, alternati a quello forte.
4. Scritta del livello: grande entrando, piccola sotto la minimappa.
5. Passi del cavaliere, con il suono del pavimento del livello.
6. Versi dei nemici a ogni colpo subito.

**Com'è andata (7 ott 2026).**

*Inventario pieno.* L'oggetto a terra ricordava di non essere entrato finché qualcuno non lo
raccoglieva. Ora, quando non entra, ascolta griglia e cintura dell'inventario che l'ha rifiutato:
appena cambiano, l'etichetta torna il solo nome. Si stacca quando l'ascolto non serve più, o
quando l'oggetto sparisce.

*Il cursore.* Il raggio si fermava sul primo collider, e la colonna davanti alla cassa la
nascondeva. Ora il raggio raccoglie fino a 16 collider, li ordina per distanza e, se il primo è un
ostacolo basso, guarda dietro. Se trova un nemico o una cosa da usare, vince quello. Un muro alto
o il pavimento fermano la ricerca, così non si clicca in un'altra stanza. I muri alti si
riconoscono dal tag `Wall`, messo sui tre prefab dei muri. Un click sulla sola colonna, come
prima, non fa nulla.

*Il bruto.* `MeleeAttack` ha un secondo colpo, `_quickWeapon` (`BruteQuick`: 8–12, ogni 1,5 s,
0,42 s di ritardo, stesso arco e portata). `SwingPattern`, in `Core`, decide: il primo colpo è
forte, poi da 1 a 3 leggeri, poi di nuovo forte. In media un colpo forte ogni tre, mai due di
fila. Durante un colpo leggero niente settore, niente carica né recupero, e l'animazione va a
velocità piena. Gli 8–12 restano sotto la soglia che ferma il cavaliere (20). Il tiro di quanti
colpi leggeri seguono consuma un numero all'inizio del colpo forte (trappola 12).

*La scritta del livello.* `LevelTitle` nell'HUD. Entrando, "Cripta – Livello 3" compare grande in
alto al centro e sfuma dopo 2,5 s; la stessa scritta, piccola, resta sotto la minimappa
d'angolo. Il nome del tipo di livello sta nel tileset (`level.crypt`, `level.caves`), e la
scritta cambia con la lingua. `LevelContext` ora tiene il tileset da cui è nato, invece del solo
profilo sonoro.

*I passi.* `Footsteps`, su un figlio del cavaliere con una sorgente sua: un passo ogni 1,6 m
percorsi, quindi da fermo tacciono e a 5 m/s sono tre al secondo. I suoni vengono dal tileset del
livello: pietra nella cripta (`footstep_concrete` di Kenney), terra nelle caverne
(`footstep_snow`, più sordo). Erano già nello zip della M5.

*I versi.* Da *80 CC0 creature SFX* di rubberduck (OpenGameArt, 1,9 MB, scaricato con il permesso
di Mirco), solo i 15 file che servono:
- scheletro: cinque lamenti, intonazione 0,95–1,05;
- sciame: quattro versi da insetto, 1,1–1,3;
- bruto: quattro grugniti e due versi da troll, 0,8–0,9.

Sono tutti sotto 0,7 s, e suonano più piano del critico (0,6). Il colpo che uccide fa solo il
suono della morte. I versi dei nemici, critico compreso, hanno ora una sorgente loro: il pitch di
una sorgente vale anche per i suoni già partiti, e il verso basso del bruto rallentava l'impatto
che suonava insieme.

*Test.* `CursorThroughObstaclesTests` (2): scheletro dietro una colonna cliccato, oggetto dietro
una colonna evidenziato e dietro un muro alto no. `FootstepsAndTitleTests` (2): passi con la
distanza e il pavimento, scritta nelle due lingue che sfuma e resta. `SwingPatternTests` (3,
EditMode). In `BruteTests` i colpi leggeri dopo il forte; in `EnemyStateTests` il colpo leggero
che non carica; in `CriticalHitTests` i versi dei tre nemici e il verso a un colpo normale; in
`LootAndPickupTests` l'etichetta che torna normale; in `PlayerHitRecoveryTests` i colpi leggeri
sotto la soglia. `SfxLimiterTests` conta anche il verso. `SwarmTests` ora mette il cavaliere lontano
prima di svegliare i nemici: restava dove l'aveva lasciato la cripta generata, che cambia a ogni
avvio, e ogni tanto finiva in vista del gruppo. 176 EditMode e 131 PlayMode verdi.

## Passo 7.11 — Sciame e morte

1. Il blocco non ferma più il cavaliere.
2. Mira: nemico sotto il cursore con nome, vita e cerchio a terra; click vicino a un nemico; il
   cavaliere non viene spinto.
3. Sciame più leggero.
4. Morte senza ricaricare il livello.

**Com'è andata (7 ott 2026).**

*Perché il cavaliere non riusciva a fare niente.* Non erano i colpi dello sciame: era lo scudo.
Ogni colpo bloccato chiamava `MeleeAttack.Interrupt(0,45 s)`. Con lo scudo e la Destrezza di
partenza il blocco è al 20–25%, e sei dello sciame attaccavano quasi una volta al secondo
ciascuno: in mezzo al gruppo arrivava un blocco al secondo, e ognuno annullava il fendente. Ora
`ShieldBlock` non ferma niente: niente danno, scritta, suono, e l'animazione del blocco solo se
il cavaliere non sta colpendo. ADR-026 cambia.

*Mira.* `PlayerController` segue anche il nemico vivo sotto il cursore (`HoveredEnemy`, con il
suo evento):
- `EnemyBar`, in alto al centro, mostra il nome (dall'archetipo: *Scheletro*, *Scheletro
  predone*, *Bruto scheletrico*) e una barra rossa della vita. Ascolta la vita del nemico,
  sparisce quando il cursore lo lascia o quando muore.
- `TargetMarker` disegna ai suoi piedi un cerchio rosso, un `LineRenderer` steso a terra con il
  materiale del settore del bruto. Sta in Core e segue il nemico; non gli diventa figlio, così
  non sparisce con il livello.
- Un click sul pavimento a meno di 0,6 m da un nemico vivo colpisce lui. Vale alla pressione,
  non tenendo premuto: chi cammina tenendo il tasto non si ferma passando vicino a un nemico.
- Il cavaliere ha la priorità di evitamento 10, sotto quella di ogni nemico (lo sciame va da
  35 a 65): gli agent dei nemici non lo spingono più.

*Lo sciame.* Gruppi da 3 a 5 invece di 4–6, un colpo ogni 1,2 s invece di 0,9. Medie su 200
semi: 4 dello sciame al livello 5, 12 all'8.

*La morte.* `LevelManager.ReturnToEntrance` sostituisce `RestartLevel`: a schermo nero il
cavaliere torna in vita e viene rimesso sull'ingresso da cui è entrato, senza scaricare la scena.
Inventario, pozioni bevute, casse aperte, oggetti a terra, nemici uccisi e mappa scoperta restano
come erano. I nemici vivi tornano fermi dove li aveva messi il livello (`EnemyAI.ReturnHome`, con
`EnemyBrain.Rest`) e tengono le ferite. L'istantanea dell'inventario all'ingresso non serve più al
gioco; `InventorySnapshot` resta, servirà al salvataggio della M8. Il pulsante ora dice
*Continua*.

*Test.* `AimTests` (4): nome, vita e cerchio del nemico sotto il cursore, che calano e spariscono;
il click accanto al nemico che lo colpisce; il cavaliere che ha la precedenza su tutti; un nome
per ogni nemico. `DeathAndRestartTests` riscritto per la regola nuova: stesso livello, inventario
e pozione bevuta com'erano, cassa aperta, nemico ucciso che non rinasce, ferito che torna a casa
fermo e ferito. `ShieldBlockTests`: il blocco non ferma più il fendente. `CaveContentTests`: le
medie nuove dello sciame. 176 EditMode e 135 PlayMode verdi.

## Passo 7.12 — Chiusura

1. Build della CI da provare: discesa fino all'8, sciame e bruto, colpo schivato e colpo
   preso, critici con i versi dei tre nemici, stesso seme stessa caverna.
2. GIF del README: un bruto che carica e il cavaliere che si sposta, uno sciame in caverna.
3. ADR: caverne, generatori per tipo di livello, IA a stati, telegrafare, critici, limite di voci.
   Lezioni nel piano, tabella dello stato, tag `m7`.

---

## Trappole note

1. **Il random walk può fare un livello piccolo o a forma di serpente:** si ferma a una
   percentuale di pavimento, non a un numero di passi, e i camminatori ripartono da celle già
   scavate a caso, non solo dal centro.
2. **Celle di pavimento collegate solo in diagonale:** per il NavMesh e per l'esplorazione
   (che non taglia gli spigoli) non sono collegate. La smussatura e la regione connessa vanno
   calcolate con i quattro vicini.
3. **Muri a scacchiera:** con il random walk compaiono pilastri di roccia di una cella, che
   con i muri alti a nord ed est coprono il cavaliere. La smussatura li toglie; il test li cerca.
4. **Il refactor dell'IA cambia i tempi:** l'ordine tra percezione e decisione nello `switch`
   va tenuto identico, o i test sul primo colpo dello scheletro cambiano di un fotogramma.
5. **Lo sciame e i corridoi:** sei agent nello stesso corridoio largo una cella si spingono.
   Priorità diverse di evitamento e un raggio un po' più piccolo.
6. **Il settore del bruto nel buio:** una decalcomania non illuminata, o il post-processing la
   fa sparire. Va guardata in una foto in batch.
7. **Un ritocco al popolatore cambia tutte le cripte:** il popolatore delle caverne è un'altra
   classe, o i semi provati della cripta cambiano.
8. **I modelli nuovi devono stare su `Rig_Medium`:** se un modello dello zip avesse un rig
   diverso, il controller condiviso (ADR-009) non basterebbe. Si controlla all'import.
9. **Il tiro del critico e i test con i tiri fissi:** `SandboxFixture` usa `FixedRandomSource(0.0)`,
   cioè ogni colpo a segno con il danno minimo. Se il critico uscisse con un tiro basso, ogni
   colpo dei test diventerebbe critico e il danno atteso raddoppierebbe. Il critico esce dalla
   parte alta (tiro ≥ 1 − probabilità) e si tira per ultimo: con 0,0 non c'è mai, e i tiri di
   colpire e del danno restano dove sono. I test con più valori in ciclo vanno ricontati.
10. **Un affisso nuovo cambia il loot dei semi provati:** il generatore sceglie tra tutti gli
    affissi del database, quindi un "+% critico" sposterebbe gli oggetti del seme 4711.
    Rimandato; se arriva, con un test che fissi i drop del seme.
12. **I colpi leggeri del bruto spostano i tiri fissi:** all'inizio di ogni colpo forte si tira
    quanti colpi leggeri seguono. Un test del bruto con una sequenza di tiri fissi deve contarne
    uno in più in testa, prima di quelli di colpire, bloccare e danno.
11. **Gli FBX del pacchetto non sono tutti esportati allo stesso modo:** quelli usati dalla M3
    hanno la radice a rotazione zero e scala 1, i pavimenti di terra e la candela tripla a −90°
    su X e scala 100. Uno script che mette un modello in un prefab non deve forzare rotazione e
    scala della radice; il test sulle misure del tileset lo controlla.

---

## Test

| Classe | Cosa verifica |
|---|---|
| `StingerScheduleTests` (EditMode) | Attese, intonazione e distanze negli intervalli, versi da tutte le direzioni, mai lo stesso di fila, bordi |
| `AmbienceTests` (PlayMode) | Profili completi, musica e fondo della cripta, musica che continua scendendo, dissolvenza, versi in 3D attorno al cavaliere |
| `CaveContentTests` (EditMode) | Profondità concatenate, contenuto su 200 semi × 4 profondità (numeri, distanza dall'ingresso, cammino libero, candele), misure del tileset |
| `CaveGeneratorTests` (EditMode) | 500 semi: connessa, pavimento tra 35 e 45%, niente pilastri isolati, scala lontana, dentro i bordi, stesso seme stessa mappa |
| `EnemyStateTests` (EditMode) | Gli stati con un corpo finto: percezione, inseguimento, attacco, carica e recupero, morte |
| `CaveDescentTests` (PlayMode) | Aspetto, luce e suono delle caverne, nemici sul NavMesh, stesso seme stessa caverna; la discesa dall'1 all'8 è in `DungeonDescentTests` |
| `SwarmTests` (PlayMode) | Numeri dello sciame contro lo scheletro, attivazione di gruppo su una mappa di prova, sei dello sciame da un cunicolo largo una cella |
| `BruteTests` (PlayMode) | Colpo telegrafato schivato e preso; interruzione dell'attacco del cavaliere |
| `CombatFormulasTests` (EditMode) | Probabilità di critico ai bordi (0, 20, 450 di Destrezza), critico solo dalla parte alta del tiro |
| `CriticalHitTests` (PlayMode) | Danno doppio e `IsCritical`, numero del critico, verso giusto per scheletro, sciame e bruto, nessun critico con i tiri fissi a 0 |
| `SfxBudgetTests` (EditMode) | Un suono per tipo e per fotogramma, oltre il limite cade il più lontano, i suoni finiti liberano il posto, priorità dalla distanza |
| `SfxLimiterTests` (PlayMode) | Dieci impatti nello stesso fotogramma, priorità delle sorgenti, cavaliere collegato |
| `SwingPatternTests` (EditMode) | Colpo forte, poi da 1 a 3 leggeri; uno forte ogni tre in media, mai due di fila |
| `CursorThroughObstaclesTests` (PlayMode) | Nemico e oggetto dietro una colonna cliccabili, dietro un muro alto no |
| `FootstepsAndTitleTests` (PlayMode) | Passi con la distanza e il pavimento del livello; scritta del livello nelle due lingue |
| `AimTests` (PlayMode) | Nemico sotto il cursore con nome, vita e cerchio; click accanto al nemico; cavaliere che non viene spinto |

---

## Checklist di chiusura

- [x] Decisioni D1–D11 confermate, D12 aggiunta il 6 ott
- [x] D13 (colpi critici) confermata il 6 ott
- [x] Musica e rumori d'ambiente (7.0)
- [x] Generatore delle caverne con i test sui 500 semi
- [x] Caverne nel gioco
- [x] IA a classi di stato, scheletro invariato
- [x] Sciame
- [x] Bruto e colpo telegrafato
- [x] Reazione al colpo del cavaliere
- [x] Colpi critici e versi dei nemici
- [x] Limite di voci audio
- [x] Nemici e loot per profondità
- [x] Correzioni dalla prova della build (D14)
- [x] Sciame e morte (D15)
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m7`
- [ ] Scheda della M8 scritta prima di cominciarla
