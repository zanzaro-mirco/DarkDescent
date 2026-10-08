# M8 — "Progressione e persistenza"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI**, uccidendo
nemici il cavaliere guadagna **esperienza**, sale di **livello** e distribuisce i **punti
attributo** nel pannello del personaggio. Trova e indossa un **elmo**, un'**armatura**,
**guanti**, **stivali**, due **anelli** e un **amuleto**; ognuno cambia le statistiche e
compare nel pannello. **Chiude il gioco, lo riapre, e si ritrova dov'era**: stesso livello,
stessa profondità, stesso equipaggiamento, inventario, cintura, esperienza e mappa scoperta.
Un salvataggio del **formato 1** si carica con il codice del **formato 2**. La visuale si
**avvicina e si allontana** con la rotella e **gira a scatti** attorno al cavaliere, con i
muri bassi sempre dalla parte della camera. Test verdi in CI.

**Tempo stimato:** 12–18 h (piano v2.19). **Prerequisito:** M7 chiusa (tag `m7`).

**Come si lavora:** come alla M7, il codice e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. A Mirco restano le decisioni qui sotto, le prove in build e la
revisione degli ADR.

**Punto di controllo:** alla chiusura di questa milestone (piano § 1.3).

---

## Stato verificato l'8 ottobre 2026

| Cosa | Stato |
|---|---|
| Attributi | `CharacterStats` con Forza 30, Destrezza 20, Magia 10, Vitalità 25 sul cavaliere; vita da Vitalità (`CombatFormulas.MaxLife`). Nessuna esperienza né livello |
| Equipaggiamento | `EquipSlot` ha solo `Weapon` e `Offhand`. `Equipment` applica i modificatori degli affissi allo `StatSheet`; lo scudo dà Armatura e blocco (`ArmorDefinition`) |
| Oggetti | Otto basi (quattro armi, quattro scudi) e la pozione. Icone renderizzate dai modelli KayKit (`Art/Icons`). Gli oggetti a terra mostrano il modello |
| Modelli disponibili | Nel `Knight.fbx` ci sono le mesh `Knight_Helmet` e `Knight_HelmetVisor`; gli altri personaggi di *Adventurers* hanno cappelli e cappucci (`Rogue_Hooded`). Per armature, guanti, stivali, anelli e amuleti nei pacchetti scaricati **non c'è niente**. Il *Dungeon Pack* ha monete e chiavi |
| Persistenza | `InventorySnapshot` (M6) cattura e ripristina griglia, equipaggiamento, cintura e oggetto sul cursore con `JsonUtility`. Nessun file su disco. `PlayerPrefs` tiene solo la lingua |
| Livelli | Generati dal seme della partita e dalla profondità (`SeedMixer.ForLevel`, ADR-034). Alla morte il livello resta com'è (ADR-048); cambiando livello quello vecchio si scarica e, tornando, si rigenera |
| Esplorazione | `Exploration` per livello, in `Core`; si perde cambiando livello |
| Camera | Cinemachine con angolo fisso; muri bassi sui lati verso la camera (ADR-017) |
| Test | 181 EditMode e 137 PlayMode verdi |

---

## Decisioni

Tutte confermate da Mirco l'8 ottobre 2026, con le proposte.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Curva dell'esperienza** | Livello massimo **20** nella v1.0. Esperienza per passare dal livello L al successivo: **100 × L^1,6**, arrotondata a 10 (100 dal 1 al 2, circa 3.360 dal 9 al 10, 11.120 dal 19 al 20). Si arriva verso il 10 in fondo alle caverne | Otto profondità non bastano per i 50 livelli di Diablo: con 20 si sale spesso all'inizio e meno dopo. La formula sta in `Core`, i numeri in un asset |
| D2 | **Esperienza dei nemici** | Un valore per archetipo: scheletro **70**, sciame **30**, bruto **230** (proposti 12, 5 e 40: tarati al passo 8.1, vedi sotto), moltiplicato per **1 + 0,15 × (profondità − 1)**. Si divide per **1 + 0,1 × (livello del cavaliere − livello della zona)** quando il cavaliere è più forte della zona, come in Diablo, per non salire uccidendo sciami del livello 1 | Dà circa un livello per profondità nella cripta e uno ogni due nelle caverne. I numeri si tarano provando |
| D3 | **Salire di livello** | **5 punti attributo** a livello, da spendere con un **+** accanto a ogni attributo del pannello. **+2 di vita** a livello oltre alla Vitalità. La vita si **riempie** salendo. Un suono e una luce dorata attorno al cavaliere | 5 punti come in Diablo 1. Riempire la vita premia il momento; senza, salire di livello in un combattimento non si sente |
| D4 | **Slot nuovi** | **Elmo, armatura, guanti, stivali, due anelli, amuleto**, accanto ad arma e scudo (piano v2.19). Il pannello dell'inventario diventa una sagoma come in Diablo: elmo in alto, amuleto a destra dell'elmo, armatura al centro, anelli sotto le mani, guanti e stivali in basso | Già deciso nel piano; qui si decide solo la disposizione |
| D5 | **Cosa danno** | Elmo, armatura, guanti e stivali: **Armatura** (come lo scudo, con la Forza richiesta per l'armatura). Anelli e amuleto: niente di base, solo **affissi**. Basi nuove: 2 elmi, 3 armature, 2 guanti, 2 stivali, 1 anello, 1 amuleto. Affissi nuovi ammessi sui gioielli: vita, i quattro attributi, colpire, probabilità di critico | Pochi oggetti, ma ogni slot ha già un motivo di cambio. Un affisso nuovo sposta il loot dei semi provati (trappola 10 della M7): i test con i drop fissi si aggiornano |
| D6 | **Modelli e icone dei pezzi nuovi** | Elmi dalle mesh dei personaggi KayKit (`Knight_Helmet`, il cappuccio di `Rogue_Hooded`). Per armature, guanti, stivali, anelli e amuleti **cerco un pacchetto CC0** di oggetti 3D e te lo propongo con nome, fonte e dimensione prima di scaricarlo. Se non c'è niente di adatto: icone piatte disegnate da me in SVG e, a terra, un sacchetto. L'armatura indossata **non cambia** il modello del cavaliere (resta alla M11) | Nei pacchetti scaricati quei modelli non ci sono. Le icone renderizzate dai modelli, come oggi, restano coerenti con il resto |
| D7 | **Dove e quando si salva** | Un **file JSON** in `Application.persistentDataPath/save.json`, scritto in un file temporaneo e poi rinominato, così un crash a metà non rovina il salvataggio. Si salva **entrando in un livello**, **ogni 60 s** fuori dal combattimento e **chiudendo il gioco**. Un solo salvataggio | Un salvataggio solo finché non c'è il menu della M10. La scrittura con il rinomina è la difesa standard contro i file a metà |
| D8 | **Cosa si salva** | Seme della partita, profondità e ingresso, livello, esperienza, attributi e punti da spendere, vita, inventario completo (`InventorySnapshot`), e la **mappa scoperta di ogni profondità visitata**. **Non** si salva lo stato dei livelli: nemici uccisi, casse aperte e oggetti a terra tornano come li genera il seme | Lo stato dei livelli vorrebbe un'identità stabile per ogni nemico, cassa e oggetto, e un formato molto più grande. Diablo 1 lo salva, ma qui un livello si attraversa in pochi minuti. Oggi cambiare livello lo rigenera già |
| D9 | **Formato versionato e migrazione** | Il file ha un campo `version`. Il **formato 1** salva tutto tranne la mappa scoperta; il **formato 2** aggiunge la mappa. Un `SaveMigrator` in `Core` porta un formato 1 al 2 (mappa vuota) prima di leggerlo. Un salvataggio di una versione più nuova del gioco non si carica e lo dice | La migrazione si prova davvero solo con due formati; questo è un cambio reale e piccolo. Il test è nel piano |
| D10 | **Avvio senza menu** | All'avvio, se c'è un salvataggio, si **riprende da lì**; altrimenti partita nuova. `-newgame` da riga di comando ignora il salvataggio, e un nuovo salvataggio lo sostituisce. Il menu con *Continua* e *Nuova partita* arriva con la M10 | Senza menu non c'è dove chiederlo; la riga di comando c'è già per `-seed` e `-lang` |
| D11 | **Zoom** | Rotella tra il **60% e il 140%** della distanza di oggi, smorzato in 0,15 s. Si salva tra le preferenze, come la lingua | Proposta del piano v2.18 |
| D12 | **Rotazione** | A **scatti di 90°** con **Q** ed **E**, animata in 0,4 s. Il builder mette su ogni lato del livello sia il muro alto sia quello basso; a metà rotazione si accende quello giusto per il nuovo lato verso la camera. Torce, stendardi e la parte sepolta della scala seguono; l'automappa gira con la camera | Proposta del piano v2.18. A scatti perché i muri bassi (ADR-017) stanno sui lati verso la camera; la rotazione libera vorrebbe uno shader che dissolve i muri, lasciato alla M11 |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 8.1 | Esperienza e livelli in `Core`: curva, esperienza dei nemici, punti attributo (D1–D3), con i test | 1,5–2 |
| 8.2 | Nel gioco: esperienza alla morte dei nemici, barra dell'esperienza nell'HUD, salita di livello con suono e luce, punti da spendere nel pannello | 1,5–2 |
| 8.3 | Slot nuovi: `EquipSlot`, `Equipment`, armatura dai pezzi, pannello a sagoma (D4, D5) | 1,5–2 |
| 8.4 | Basi nuove, affissi dei gioielli, modelli e icone (D5, D6), loot dei nemici e delle casse | 2–3 |
| 8.5 | Salvataggio formato 1: `SaveData`, scrittura sicura, caricamento all'avvio, `-newgame` (D7, D8, D10) | 2–3 |
| 8.6 | Formato 2 con la mappa scoperta e migrazione (D9) | 1 |
| 8.7 | Zoom (D11) | 0,5–1 |
| 8.8 | Rotazione a scatti e muri che seguono la camera (D12) | 2–3 |
| 8.9 | Chiusura: build da provare, GIF, ADR, lezioni, tag `m8` | 0,5 |

---

## Passo 8.1 — Esperienza e livelli

**Com'è andata (8 ott 2026).** Tutto in `Core/Progression`, logica pura:

- `ProgressionSettings` è un asset (`Data/Progression/Progression.asset`) con la curva (D1), i
  punti e la vita a livello (D3) e le regole dell'esperienza dei nemici (D2). Il livello della
  zona è la profondità × 1,25, arrotondato per eccesso a metà: la profondità 8 vale il livello 10.
- `CharacterProgress` tiene livello, esperienza nel livello e punti da spendere. Salire scrive la
  vita dei livelli come valore base di `Life` sulla scheda; spendere un punto alza di uno il valore
  base dell'attributo. Più livelli in un colpo solo si annunciano uno per uno. `Restore` è pronto
  per il salvataggio.
- `EnemyArchetype` ha l'esperienza del nemico.

*I numeri della D2 non bastavano.* Una simulazione della discesa, con i nemici che la cripta e
le caverne mettono a ogni profondità (cripta: 3 + 2 per profondità; caverne: le medie della
`SpawnTable`), uccidendoli tutti, portava il cavaliere solo al **livello 4** in fondo alle caverne:
con 12, 5 e 40 i nemici sono troppo pochi per la curva della D1. Con **70, 30 e 230**, le stesse
proporzioni circa sei volte tanto, si sale più o meno di un livello a profondità e si arriva al
**9** all'ottava, come voleva la D1. La curva resta quella confermata.

| Profondità | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| Livello in uscita | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |

*Test.* `ExperienceTests` (7, EditMode): la curva con i numeri della scheda, l'esperienza che cresce
con la profondità e cala sopra il livello della zona, i valori dei tre nemici, la salita con punti
e vita, il livello massimo, i punti spesi, lo stato ripristinato. 188 EditMode verdi.

## Passo 8.2 — Esperienza e livelli nel gioco

**Com'è andata (8 ott 2026).**

- `PlayerProgress`, sul cavaliere, crea `CharacterProgress` e riceve l'esperienza dei nemici.
  `EnemyAI` annuncia la sua morte (`Killed`); il composition root lo ascolta per ogni nemico
  legato e dà l'esperienza alla profondità del livello. Ci si stacca alla morte e quando il
  livello se ne va. Da morto il cavaliere non guadagna niente.
- Salendo di livello la vita si riempie, con i 2 punti in più già contati: `CharacterProgress`
  scrive la vita dei livelli prima di annunciarli e `Health` la rilegge dalla scheda. Suona un
  arpeggio di campana e una luce dorata si accende attorno al cavaliere per 1,4 s. Più livelli in
  un colpo solo fanno un suono e una luce. Il suono è sintetizzato da uno script Python, quindi
  nostro (`Audio/SFX/Generated/level_up.wav`, nei crediti). Sorgente audio e luce stanno su un
  figlio del cavaliere, `LevelUp`.
- `ExperienceBar` in basso al centro: una barra dorata verso il livello successivo e sopra
  *Livello 2 – 60 / 300*; al livello massimo è piena.
- Il pannello del personaggio ha una testata sotto il titolo, *Livello 2 – Esperienza 60 / 300*
  e, in oro, *5 punti da spendere*. Con punti da spendere compare un **+** accanto a Forza,
  Destrezza, Magia e Vitalità; si posiziona sulla riga misurandola all'apertura del pannello.
  Le colonne sono scese di 50 px per fare posto alla testata.

*Test.* `LevelUpTests` (2, PlayMode): lo scheletro ucciso dà la sua esperienza una volta sola e
la barra la mostra; salendo, vita piena con i 2 punti in più, la luce, la testata con i punti, il
+ della Forza cliccato con il mouse che compra un punto senza far camminare il cavaliere, i +
che spariscono a punti finiti. 188 EditMode e 139 PlayMode verdi.

## Passo 8.3 — Slot nuovi e sagoma

**Com'è andata (8 ott 2026).**

- `EquipSlot` ha i valori nuovi in fondo, così i numeri di quelli vecchi non cambiano: elmo,
  armatura, guanti, stivali, amuleto, anello e `Ring2`, il secondo anello. Nessun oggetto chiede
  `Ring2`: un anello dice `Ring`, ed è `Equipment` a scegliere (trappola 5). Senza indicazioni va
  nel primo slot libero, o al posto del primo; con un click su uno slot va lì
  (`TryEquip(item, slot, ...)`, `Equipment.Fits`).
- `ArmorDefinition` ha uno slot (scudo, elmo, armatura, guanti, stivali) e la Forza richiesta; il
  blocco vale solo per gli scudi. `JewelryDefinition`, nuova, per anelli e amuleti: niente di base,
  solo affissi. La Forza richiesta è diventata una proprietà di ogni oggetto (`RequiredStrength`,
  0 di base), e `MeetsRequirements` la legge per tutti. Il tooltip la mostra anche per
  l'armatura, senza la riga del blocco.
- `AffixTargets` ha `Armor` e `Jewelry`: quali affissi ci vanno si decide al passo 8.4.
- `InventorySnapshot` salva anche lo slot di ogni oggetto indossato: con due anelli conta quale.
  Le istantanee senza slot si ripristinano come prima.
- La finestra dell'inventario è una sagoma come in Diablo: elmo in alto con l'amuleto accanto,
  arma e scudo ai lati, armatura al centro, anelli sotto le mani, guanti e stivali in basso, con
  il nome sotto ogni slot. È alta 720 invece di 600 e scesa di 40 px, per non coprire la scritta
  del livello sotto la minimappa. `InventoryPanel` ha un array di slot al posto dei due campi.

*Test.* `EquipmentSlotsTests` (6, EditMode), con definizioni create nel test perché le basi
arrivano all'8.4: l'Armatura dei pezzi che si somma, la Forza richiesta, i due anelli, quali slot
accettano cosa, il click che mette l'anello nel secondo slot, il tooltip senza blocco. 194 EditMode
e 139 PlayMode verdi.

## Passo 8.4 — Basi nuove, affissi e loot

**Com'è andata (8 ott 2026).**

*Modelli (D6).* Scaricato con il permesso di Mirco l'*Ultimate RPG Items Pack* di Quaternius
(CC0, 44,6 MB, OpenGameArt): ha cinque corazze, un guanto, sette anelli e tre collane, ma niente
elmi né stivali. Quindi:

- **Elmi** dalle mesh dei personaggi KayKit, cotte nella posa del modello (`BakeMesh`) e salvate
  come asset: l'elmo del cavaliere con la visiera, e il copricapo d'orso del barbaro
  (`Barbarian.fbx`, copiato dallo zip di *Adventurers* già scaricato).
- **Armature, guanti, anello e amuleto** da Quaternius. I suoi FBX hanno la radice girata di −90°
  su X, che icona e oggetto a terra sovrascrivono: stanno in un prefab con la radice dritta, come
  i modelli KayKit. I guanti di ferro sono lo stesso guanto con un materiale di ferro.
- **Stivali** fatti in casa: un paio a blocchi (gambale, piede e risvolto) combinati in una mesh,
  di cuoio e di ferro. Si intonano con il low-poly degli altri.
- Le icone le fa `ItemTools` come per gli altri oggetti; stivali e guanti hanno una rotazione
  d'icona loro.

*Le basi (D5).*

| Base | Slot | Armatura | Forza |
|---|---|---|---|
| Copricapo d'orso | elmo | 3 | — |
| Elmo da cavaliere | elmo | 6 | 25 |
| Armatura di cuoio | armatura | 8 | — |
| Corazza | armatura | 14 | 35 |
| Corazza nera | armatura | 20 | 50 |
| Guanti di cuoio | guanti | 2 | — |
| Guanti di ferro | guanti | 4 | 25 |
| Stivali di cuoio | stivali | 2 | — |
| Stivali di ferro | stivali | 5 | 25 |
| Anello | anello | — | — |
| Amuleto | amuleto | — | — |

Le undici entrano in tutte e quattro le tabelle del loot (scheletro, sciame, bruto, cassa) con
peso 1: la lama dello scheletro, a 3, resta la più frequente.

*Affissi.* Armatura e "+% Armatura" anche sui pezzi d'armatura; Forza, Destrezza, Vitalità e
vita su tutto; "a colpire" su armi e gioielli. Nuovo il prefisso **Letale** (+2–5% di critico, dal
livello d'oggetto 2), solo sui gioielli: `AffixEffect.CritChance` e `StatType.CritChance`, che
`CombatFormulas.CritChance` somma alla Destrezza, sempre con il massimo a 50. Il pannello del
personaggio lo conta.

*Nomi.* Guanti e stivali sono plurali: il genere della base può essere `mp`, e il prefisso usa la
sua forma `.mp` (*Guanti di ferro Robusti*, *Stivali di cuoio Massicci della Forza*).

*Test.* `M8ItemsTests` (4, EditMode): le undici basi con slot, Armatura, Forza, modello e icona;
dove vanno gli affissi; l'anello Letale che alza il critico; i nomi al plurale. Aggiornati i test
che contano basi e affissi (`ItemDatabaseTests`, `ItemGeneratorTests`, `LootTests`,
`LocalizationCoverageTests`). Nessun test con i drop fissi a un seme è cambiato: le basi nuove sono
in fondo alle tabelle. 198 EditMode e 139 PlayMode verdi.

## Passo 8.5 — Salvataggio, formato 1

**Com'è andata (8 ott 2026).**

- `SaveData`, in `Core/Save`: il campo `_version` (1), il seme della partita in testo (un `ulong`
  in JSON come numero si perde in altri lettori), scena, ingresso e profondità, livello,
  esperienza e punti, i valori base dei quattro attributi, la vita e l'`InventorySnapshot` della
  M6, che dal passo 8.3 salva anche lo slot di ogni oggetto indossato. Lo stato dei livelli non
  c'è (D8).
- `SaveFile` scrive in `save.json.tmp` e poi lo mette al posto del vecchio con `File.Replace`: un
  crash a metà lascia il salvataggio di prima. In lettura distingue un file che manca, uno che non
  si legge (vuoto, troncato, non nostro) e uno di una versione più nuova del gioco, che non si
  carica.
- `SaveGame`, un componente nella scena `Core`, salva entrando in un livello, ogni 60 s se da
  5 s non ci sono colpi dati o presi, e chiudendo il gioco. Da morto salva come dopo *Continua*,
  a vita piena. Nell'editor è spento, così le prove e i test non toccano il salvataggio vero: i
  test lo accendono con un file loro, da codice o con la variabile d'ambiente
  `DARKDESCENT_SAVE_FILE`.
- All'avvio il composition root, nel suo `Awake`, legge il salvataggio: se c'è prende il seme e
  dice al gestore dei livelli da dove partire (`SetStartLevel`). Nel suo `Start`, quando vita,
  pannelli ed equipaggiamento ascoltano già, lo rimette sul cavaliere: attributi, crescita,
  inventario (che si svuota prima, così arma e pozioni di partenza spariscono) e per ultima la
  vita, con `Health.SetCurrent`, che non è né un colpo né una cura. `-newgame` e `-seed`
  ignorano il salvataggio, e il primo salvataggio della partita nuova lo sostituisce.
- Il percorso del file e l'accensione si calcolano al primo uso, non in `Awake`: il composition
  root legge il salvataggio dal suo `Awake`, che può arrivare prima di quello di `SaveGame`.

*Test.* `SaveDataTests` (5, EditMode): scrittura e lettura identiche, il seme a 64 bit, la
sostituzione senza file temporanei rimasti, i file rovinati, la versione più nuova, `-newgame`.
`SaveLoadTests` (3, PlayMode): entrando al livello 2 si salva e, rovinato tutto, il cavaliere
torna identico campo per campo, con il secondo anello nel secondo slot; all'avvio con un
salvataggio scritto a mano si riparte dalla profondità 3, con seme, crescita, attributi, vita,
elmo indossato e senza le pozioni di partenza; da morto si salva a vita piena. 203 EditMode e
142 PlayMode verdi.

## Passo 8.6 — Formato 2 e migrazione

**Com'è andata (8 ott 2026).**

- Prima di toccare il formato, uno script in batch ha scritto con il codice del formato 1 un
  salvataggio vero (caverne, profondità 6, livello 8, elmo, due anelli di cui uno Letale, un
  pugnale, una pozione) in `Tests/EditMode/Fixtures/save_v1.json`. Resta lì congelato: il test
  della migrazione carica quel file, non uno scritto a mano che potrebbe somigliare al formato
  di oggi più del vero.
- La mappa scoperta ora resta per ogni profondità: `ExplorationMemory`, in `Core/Levels`, tiene
  un'esplorazione per profondità, e il livello rigenerato dal seme riprende le celle viste.
  `ExplorationTracker.SetLevel` vuole la profondità; il vecchio `keepExplored` non serve più.
- **Formato 2:** `SaveData` ha in più la lista delle mappe (`ExploredLevel`: profondità, misura
  e una cella per bit in base64). Una mappa salvata di un'altra misura si ignora. Le mappe di un
  salvataggio aspettano che il livello di quella profondità venga generato.
- `SaveMigrator` porta un formato vecchio al corrente un passo alla volta; dal 1 al 2 dichiara il
  formato nuovo con le mappe vuote. `SaveFile.TryRead` migra dopo aver letto, e il codice del
  gioco vede solo il formato corrente. Un formato più nuovo resta rifiutato.

*Test.* `SaveMigrationTests` (3, EditMode): il file del formato 1 si carica con il codice del
formato 2, migrato e senza mappe, con seme, profondità, crescita, vita e inventario intero (il
secondo anello Letale nel secondo slot, il critico a +4), e riscritto è del formato 2; la memoria
che tiene ogni profondità; le mappe che passano dal salvataggio e quella di un'altra misura
ignorata. `SaveLoadTests` controlla anche le due mappe nel file. 206 EditMode e 142 PlayMode verdi.

---

## Trappole note

1. **Un affisso nuovo cambia il loot dei semi provati** (trappola 10 della M7): i gioielli
   aggiungono affissi e basi, e i test che fissano un drop vanno aggiornati insieme.
2. **`JsonUtility` non serializza dizionari né riferimenti ad asset:** gli oggetti si salvano
   con l'id della definizione e degli affissi, come fa già `InventorySnapshot`. La mappa
   scoperta va in una lista di profondità con un array di bit per livello.
3. **Il salvataggio all'uscita non sempre arriva:** `OnApplicationQuit` in build sì, un crash
   no. Per questo si salva anche entrando nei livelli e ogni minuto.
4. **Caricare non è come entrare in un livello:** il cavaliere va messo sull'ingresso salvato
   *dopo* che il livello è generato, e i modificatori degli oggetti indossati vanno applicati
   prima di riempire la vita, o la vita massima sarebbe quella senza oggetti.
5. **Due anelli nello stesso tipo di slot:** `Equipment` oggi ha uno slot per tipo. Gli anelli
   vogliono due posti con la stessa regola: o due valori dell'enum, o uno slot con un indice.
6. **I muri doppi raddoppiano i collider:** il muro spento deve spegnere anche il suo collider,
   o il NavMesh, cotto dai collider (ADR-035), cambierebbe con la camera. Si cuoce con i muri
   alti, che stanno sempre sulla griglia.
7. **La rotazione cambia la direzione dei click:** il raggio dal cursore usa la camera, quindi
   segue da solo; ma le scorciatoie che assumono la camera a nord-est (la luce di riempimento
   del cavaliere, l'automappa) vanno riguardate.

---

## Test

| Test | Cosa controlla |
|---|---|
| `ExperienceTests` (EditMode) | Curva, esperienza per nemico e profondità, riduzione per livello alto, punti a livello |
| `SaveDataTests` (EditMode) | Salvare e ricaricare restituisce uno stato identico; formato 1 migrato al 2; versione più nuova rifiutata; file a metà ignorato |
| `EquipmentTests` (EditMode, ampliato) | Gli slot nuovi, i due anelli, l'Armatura dei pezzi |
| `LevelUpTests` (PlayMode) | Uccidere dà esperienza, si sale, la vita si riempie, i punti si spendono |
| `SaveLoadTests` (PlayMode) | Dopo un caricamento il cavaliere è sulla profondità e sull'ingresso salvati, con tutto addosso |
| `CameraRotationTests` (PlayMode) | Zoom nei limiti; dopo ogni scatto, tra la camera e il cavaliere nessun muro alto |

---

## Checklist di chiusura

- [x] Decisioni D1–D12 confermate
- [ ] Esperienza e livelli
- [ ] Equipaggiamento completo
- [ ] Salvataggio e migrazione
- [ ] Zoom e rotazione
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m8`
- [ ] Punto di controllo (piano § 1.3)
- [ ] Scheda della M9 scritta prima di cominciarla
