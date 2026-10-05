# M7 — "Le profondità"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI**, dalla
scala del livello 4 si scende nelle **caverne**: i livelli 5–8 sono spazi organici generati
a ogni partita con un **random walk**, sempre percorribili dall'ingresso alla scala, con un
aspetto loro (terra, roccia, macerie). Ci vivono due nemici nuovi: lo **sciame**, veloce e
fragile, che arriva in gruppo, e il **bruto**, lento e resistente, che carica un colpo
**telegrafato** da cui ci si può spostare in tempo. Un colpo del bruto **interrompe**
l'attacco del cavaliere, uno dello sciame no. Lo scheletro si comporta come prima, ma la sua
IA è fatta di **classi di stato**. Con dieci nemici che colpiscono insieme l'audio non si
satura. L'automappa si riempie anche nelle caverne. Con `-seed 4711` le caverne tornano
uguali. Test verdi in CI.

**Tempo stimato:** 10–15 h (piano v2.14). **Prerequisito:** M6 chiusa (tag `m6`).

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

Da confermare con Mirco prima di cominciare.

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
| D11 | **Loot dei nemici nuovi** | Una `LootTable` per tipo: sciame **20%** di lasciare un oggetto e 10% una pozione; bruto **sempre** un oggetto e 50% una pozione. Basi e rarità come lo scheletro, livello dell'oggetto dalla profondità | Lo sciame è tanti nemici piccoli: con la probabilità dello scheletro il pavimento si riempirebbe. Il bruto è un premio |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 7.1 | Generatore delle caverne (logica pura) e test sui 500 semi | 1,5–2 |
| 7.2 | Caverne nel gioco: tileset, scena `Level_Caves`, scala dal livello 4, finestra dell'editor | 1,5–2 |
| 7.3 | IA a classi di stato, scheletro invariato | 1,5–2 |
| 7.4 | Sciame | 1–1,5 |
| 7.5 | Bruto e colpo telegrafato | 1,5–2 |
| 7.6 | Reazione al colpo del cavaliere | 0,5–1 |
| 7.7 | Limite di voci audio | 0,5–1 |
| 7.8 | Nemici e loot per profondità | 1 |
| 7.9 | Chiusura: build da provare, GIF, ADR, tag `m7` | 0,5 |

---

## Passo 7.1 — Generatore delle caverne

1. `CaveGenerator` (logica pura, solo interi): camminatori dal centro, smussatura, regione
   connessa più grande, ingresso e scala con una visita in ampiezza.
2. `CaveSettings` con area, percentuale di pavimento, camminatori, passate di smussatura.
3. Test su **500 semi**: tutto il pavimento raggiungibile dall'ingresso, pavimento tra il 35 e
   il 45%, niente celle di roccia isolate in mezzo al pavimento, scala ad almeno metà della
   distanza massima, mappa dentro i bordi, stesso seme stessa mappa. Gli stessi controlli della
   cripta, quindi le funzioni di verifica si condividono.

## Passo 7.2 — Caverne nel gioco

1. Tileset delle caverne (D4): modelli estratti dallo zip del *Dungeon Pack*, prefab con i
   collider per il NavMesh (ADR-035).
2. Scena `Level_Caves` costruita dallo strumento dell'editor, con atmosfera e luce sue.
3. Scala del livello 4 verso la profondità 5; il livello 8 senza scala.
4. Finestra dell'editor: scelta tra cripta e caverne.
5. Test: discesa dal 4 al 5 e fino all'8, nemici sul NavMesh, stesso seme stessa caverna;
   l'automappa disegna le caverne.

## Passo 7.3 — IA a classi di stato

1. `IEnemyBody` e gli stati in `Core/Enemies/`, `EnemyAI` che li esegue.
2. `EnemyArchetype` con gli stati e i numeri; quello dello scheletro riproduce i valori di oggi.
3. I test di `EnemyAITests` restano verdi senza cambiare; test EditMode degli stati con un corpo
   finto (vede, insegue, attacca a distanza, perde il bersaglio, muore).

## Passo 7.4 — Sciame

1. Prefab dello sciame, archetipo, attivazione di gruppo.
2. Gruppi nel popolatore delle caverne.
3. Test: un gruppo si attiva insieme, nessuno resta incastrato in un corridoio largo una cella.

## Passo 7.5 — Bruto

1. Prefab del bruto, stati `WindUp` e `Recover`, settore a terra.
2. Test: fuori dal settore alla fine della carica il colpo non arriva, dentro sì; dopo il
   colpo il bruto resta fermo.

## Passo 7.6 — Reazione al colpo del cavaliere

1. `HitRecovery` sul cavaliere con la soglia di D7; animazione e interruzione del click.
2. Test: il colpo del bruto interrompe l'attacco, quello dello scheletro no.

## Passo 7.7 — Limite di voci audio

1. `SfxLimiter` e la priorità dalla distanza; `CharacterAudio` gli chiede il permesso.
2. Test: dieci impatti nello stesso fotogramma suonano una volta; oltre il limite cade il più
   lontano.

## Passo 7.8 — Nemici e loot per profondità

1. `SpawnTable` in `CaveSettings`, loot table di sciame e bruto.
2. Test su 200 semi per profondità: numeri della tabella, nessun nemico nella zona
   d'ingresso; probabilità dei drop su 10.000 tiri.

## Passo 7.9 — Chiusura

1. Build della CI da provare: discesa fino all'8, sciame e bruto, colpo schivato e colpo
   preso, stesso seme stessa caverna.
2. GIF del README: un bruto che carica e il cavaliere che si sposta, uno sciame in caverna.
3. ADR: caverne, generatori per tipo di livello, IA a stati, telegrafare, limite di voci.
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

---

## Test

| Classe | Cosa verifica |
|---|---|
| `CaveGeneratorTests` (EditMode) | 500 semi: connessa, pavimento tra 35 e 45%, niente pilastri isolati, scala lontana, dentro i bordi, stesso seme stessa mappa |
| `EnemyStateTests` (EditMode) | Gli stati con un corpo finto: percezione, inseguimento, attacco, carica e recupero, morte |
| `CaveDescentTests` (PlayMode) | Dal livello 4 al 5 e all'8, nemici sul NavMesh, stesso seme stessa caverna |
| `SwarmTests`, `BruteTests` (PlayMode) | Attivazione di gruppo; colpo telegrafato schivato e preso; interruzione dell'attacco del cavaliere |
| `SfxLimiterTests` (PlayMode) | Un suono per tipo e per fotogramma, limite di voci |

---

## Checklist di chiusura

- [ ] Decisioni D1–D11 confermate
- [ ] Generatore delle caverne con i test sui 500 semi
- [ ] Caverne nel gioco
- [ ] IA a classi di stato, scheletro invariato
- [ ] Sciame
- [ ] Bruto e colpo telegrafato
- [ ] Reazione al colpo del cavaliere
- [ ] Limite di voci audio
- [ ] Nemici e loot per profondità
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m7`
- [ ] Scheda della M8 scritta prima di cominciarla
