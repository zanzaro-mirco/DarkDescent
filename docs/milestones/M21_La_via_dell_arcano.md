# M21 — "La via dell'arcano"

**Cosa deve succedere (Definition of Done):** *Nuova partita* fa **creare il personaggio**: si sceglie la
classe, il **Guerriero** di oggi o il **Mago**, e si scrive il nome. Il Mago combatte con gli incantesimi,
ha il suo **albero delle abilità** (la classe e due specializzazioni), le sue armi (**bastone** e
**bacchetta**) e il suo modello. Il salvataggio tiene classe e nome, e i tre posti del menu li mostrano.

- Il pannello è lo stesso del Guerriero (tasto **N**), con gli alberi del Mago.
- Il **Dardo di fuoco** e l'**Anello di fuoco**, tolti al Guerriero nella M20, tornano al Mago.
- Il bilancio non si sposta: la misura delle difficoltà prende anche il Mago, nelle stesse fasce (D11).
- Tag **`v1.9.0`**.

**Fuori da questa milestone:** il Ranger e il Ladro (M27, M28); l'equipaggiamento che si vede addosso
(M26); più personaggi nello stesso posto; cambiare classe a partita iniziata; le armi a distanza con le
frecce.

---

## Stato verificato il 2 novembre 2026

Verificato dopo la chiusura della M20 (tag `v1.8.0`, build 41529821395 verde).

| Cosa | Com'è oggi |
|---|---|
| La classe | Non esiste: il cavaliere è il prefab `Player`, con il modello `Knight`. `PlayerTalents` ha due campi, l'albero della classe e le specializzazioni, assegnati nel prefab |
| Gli incantesimi | `SpellDefinition` (in `Core/Magic`) ha i tipi `SpellKind` del Dardo, dell'Anello e della Pelle di pietra, più quelli del Guerriero (`IsWarriorAbility`). Il Dardo di fuoco e l'Anello (`Data/Spells/Firebolt`, `FireRing`) ci sono ancora, senza un libro che li dia: i test li usano con un libro di prova |
| Il danno magico | `CombatFormulas.SpellDamage(roll, magia, divisore)`; il mana è 50 + 2 × Magia + il livello. Il mana torna al 2% del massimo al secondo (`ManaRegenPerSecond`), per tutti, e dal Guerriero anche colpendo (Disciplina) |
| Le armi | `WeaponKind` ha Unarmed, Dagger, Sword, Axe; le due mani sono la bandiera `TwoHanded` (M20). Bastone e bacchetta non ci sono. Nel progetto sono importati il `Knight`, il `Mage`, il `Ranger`, il `Rogue` e il `Barbarian`; di armi `axe_1handed`, `axe_2handed` e `dagger` (le altre sono nello zip di *Adventurers*) |
| Il menu | *Nuova partita* sceglie il posto e parte l'apertura (M19). Il salvataggio è alla versione 8; non ha né classe né nome |
| La misura | `PowerCensus`, `HeroSetup` e `DifficultyCensus` simulano i colpi d'arma di un cavaliere con scudo; `HeroSetup.SpendTalents` spende l'albero di riferimento (M20). Gli incantesimi non entrano |
| Le animazioni | Il controller del cavaliere ha il gesto del lancio (`Spellcast`) del Dardo e dell'Anello, e i gesti del Guerriero (M20). Il `Mage` di KayKit ha lo stesso scheletro |

---

## Decisioni

**Confermate da Mirco il 2 novembre 2026:** prima D3 (il Dardo arcano gratis), D5 (*Fuoco* e *Gelo*) e D10 («l'eroe»), poi tutte le altre insieme al disegno dell'albero (21.1), senza tagli.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M21 "La via dell'arcano"**, tag **`v1.9.0`**. Dentro: la creazione del personaggio, il Mago con il suo albero, le sue armi e il salvataggio 9 | Una classe nuova e una schermata nuova: una versione minore |
| D2 | **La classe è un dato** | Un asset `CharacterClass`: nome, descrizione, modello, albero della classe e specializzazioni, abilità di partenza, attributi iniziali, schema dei punti (Guerriero: Forza, Vitalità, Forza, Vitalità, Destrezza), arma iniziale. Un solo prefab `Player`: alla creazione si applica la classe e il modello cambia. L'alternativa è un prefab per classe, da tenere uguali a ogni modifica | La M27 e la M28 aggiungono una riga di dati, non un prefab. `PlayerTalents`, `HeroSetup` e la misura leggono la classe |
| D3 | **Il Mago combatte con gli incantesimi** | Il sinistro del Mago è il **Dardo arcano**: parte dalla bacchetta o dal bastone, a distanza, **senza mana e senza ricarica**, con il danno dell'arma più la Magia. Il Dardo di fuoco diventa un'abilità del destro e costa mana. Alternative: il bastone in mischia (poco da Mago), o il sinistro che costa mana | È la regola del sinistro (M20): niente mana e niente ricarica. Il Mago ha poca vita e armatura, quindi attacca da lontano |
| D4 | **Le armi** | **Bastone** (due mani: danno alto, Magia richiesta) e **bacchetta** (una mano, con il **tomo** nell'altra mano al posto dello scudo: + Magia e + mana, senza blocco). Tre valori nuovi, `WeaponKind.Staff`, `WeaponKind.Wand` e un tomo come armatura dello slot della mano libera. Modelli di *Adventurers* (`staff`, `wand`, il libro di incantesimi: da importare dallo zip, con il sì di Mirco). Il Guerriero non le usa per i requisiti (Magia), e il Mago non usa lo scudo | Come per le armi a due mani (M20), un tipo d'arma nuovo vuole regola dello slot, gesto, affissi e loot, non una struttura nuova |
| D5 | **Le due specializzazioni** | ***Fuoco***: il danno e le aree (Palla di fuoco, Meteora, fiamme che restano). ***Gelo***: il controllo e la difesa (rallentare, congelare, armatura di ghiaccio). Alternativa: *Fuoco* e *Arcano* (teletrasporto, mana), più da giocatore esperto | Come *Armi* e *Difesa*: una che fa danno e una che tiene a bada i nemici |
| D6 | **Le abilità attive** | Undici, come il Guerriero, ognuna con il suo gesto, effetto e suono. **Classe**: Dardo di fuoco, Anello di fuoco, Scudo di mana (il danno preso scala il mana), Passo di luce (teletrasporto breve). **Fuoco**: Palla di fuoco, Muro di fiamme, Meteora. **Gelo**: Dardo di ghiaccio (rallenta), Nova di gelo (congela attorno), Armatura di ghiaccio, Tempesta di neve. I nomi e i numeri si decidono nel disegno (21.1). Servono tipi nuovi (`SpellKind`): teletrasporto, scudo di mana, area che resta, rallentamento e congelamento dei nemici | Il Guerriero ne ha undici; un Mago con meno sarebbe povero accanto a lui |
| D7 | **Il mana del Mago** | Torna **più in fretta**: 3% del massimo al secondo, contro il 2% che tutti hanno oggi (`CombatFormulas.ManaRegenPerSecond`), con un fattore 1,5 nel dato della classe (il Guerriero 1). Per il resto come oggi: pozioni e mana dall'albero. *Corretto il 2 novembre, al 21.2: la proposta diceva che il mana del Guerriero non si rigenera, ma torna già al 2% al secondo, e un Mago «lento» sarebbe stato peggio di lui* | Con il Dardo arcano gratis (D3) il mana serve alle sole abilità forti; il Mago ne spende di più |
| D8 | **La creazione del personaggio** | *Nuova partita* → il posto → una schermata: **le due classi** con il modello che gira, una riga sul loro stile e sui numeri di partenza, e il **nome** (fino a 16 lettere, uno di partenza per classe). Poi l'apertura. Il nome e la classe vanno nel salvataggio e nei tre posti del menu («Mirco · Mago · livello 12»). I salvataggi vecchi si aprono come Guerriero, con il nome «Cavaliere». Il nome non si cambia dopo, come in Diablo | Dalla richiesta dei tester del 18 ottobre |
| D9 | **Il salvataggio 9** | Aggiunge `class` e `name`; la migrazione dall'8 mette Guerriero e «Cavaliere». Un campione nuovo è `save_v9`, con un test di migrazione da `save_v8`. La barra, l'albero e le abilità si salvano come oggi, per classe | Lo schema del salvataggio 8 non cambia altro |
| D10 | **I filmati e le parole** | L'apertura e la vittoria usano il **modello della classe scelta** (oggi il cavaliere). Il testo dice «il cavaliere» in più posti: dove parla del personaggio scelto diventa **«l'eroe»** (*the hero*), nei filmati e nei messaggi; «cavaliere» resta solo nel nome della classe. Alternativa: due versioni delle didascalie | Una sola versione dei testi, e il Mago non è un cavaliere |
| D11 | **Bilancio e ronda** | La misura delle difficoltà prende anche il **Mago** di riferimento (*Fuoco*, e *Gelo* come confronto), con l'attacco a distanza (arma + Magia) e senza scudo, nelle **stesse fasce** della M14, con la tolleranza del 5% sul lato corto dei colpi per uccidere. Se escono, si ritarano i **numeri della classe** (vita, armatura, danno del Dardo arcano), non le difficoltà. La ronda misura le due classi | Come per l'albero del Guerriero (D14 della M20): la M14 non si ritara |
| D12 | **Il loot e il mercante** | Bastoni, bacchette e tomi entrano nel loot e dal **mercante** (il fabbro resta delle armi e armature da guerriero); gli affissi del Mago (danno degli incantesimi, mana, ricarica) si ammettono sui tipi nuovi, e quelli del Guerriero (blocco, rubavita) non vi cadono. I numeri tengono i loot della M13 nelle fasce: la ronda li conta | Senza un loot per il Mago, il Mago è un Guerriero che non può equipaggiarsi |
| D13 | **Le icone** | Una per abilità attiva e per **tipo** di passivo: circa 40, **Z-Image-Turbo**, nello stile di Diablo III, come alla M20 (tre semi, scelti da Claude, foto per Mirco). Una decina si riusano (vita, armatura, critico…). Il Mago ha anche la sua icona di classe per la schermata (D8) | Si ripete la strada della M20, che ha funzionato |
| D14 | **Il Guerriero non cambia** | Con la classe come dato (D2) il Guerriero è una riga di dati. I suoi numeri, il suo albero e le sue 44 icone restano com'erano: la misura del 20.8 vale ancora | Un Guerriero che cambia romperebbe la misura |
| D15 | **Si controlla** | **EditMode:** `CharacterClass` (ogni classe ha modello, albero e schema), il salvataggio 9 e la migrazione, gli alberi del Mago (ogni nodo raggiungibile, testi nelle due lingue, icone), le armi nuove (slot, requisiti, affissi), la misura con il Mago. **PlayMode:** la creazione (le due classi, il nome, i posti), il Dardo arcano, ogni abilità del Mago, il pannello con i suoi alberi, la partita di ognuna delle due classi che attraversa il borgo e la cripta. **Foto** per Mirco: la schermata, il Mago in gioco, il pannello, le abilità | La scheda M20 e le sue prove sono il modello |
| D16 | **Stima e linee di taglio** | **30–42 h**, circa un quarto più della M20 per la creazione e per i tipi di incantesimo nuovi. Se serve tagliare, in quest'ordine: Tempesta di neve (diventa un passivo); la Meteora; l'Armatura di ghiaccio; il fattore del mana (D7); la scelta di due specializzazioni (una sola, il *Fuoco*); le icone per tipo (restano quelle della classe). La creazione, il Dardo arcano, l'albero della classe e il salvataggio no | Stima da rivedere dopo il disegno |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 21.1 | **Il disegno**: la pagina con l'albero della classe del Mago e quelli di *Fuoco* e *Gelo*, nodi con effetto, gradi, numeri, posti, e le undici abilità (D5, D6). Si va avanti **solo con il sì di Mirco** | 2–3 |
| 21.2 | **La classe come dato**: `CharacterClass`, il modello e gli attributi di partenza applicati alla creazione, `PlayerTalents` e `HeroSetup` che la leggono, il salvataggio 9 con la migrazione e il campione, il fattore del mana (D2, D7, D9) | 4–5 |
| 21.3 | **Le armi del Mago**: `Staff`, `Wand`, il tomo, i modelli, la regola dello slot, il gesto, gli affissi, il loot e il mercante (D4, D12) | 3–4 |
| 21.4 | **Le abilità del Mago**: i tipi nuovi di `SpellKind`, il Dardo arcano come sinistro, le undici con i loro gesti, effetti e suoni; il rallentamento e il congelamento dei nemici (D3, D6) | 8–11 |
| 21.5 | **L'albero del Mago**: gli asset (`TalentTools.BuildMage`), i passivi, i testi nelle due lingue, il pannello e l'altare per entrambe le classi (D5) | 3–4 |
| 21.6 | **La creazione del personaggio**: la schermata con il modello che gira, il nome, i posti del menu con classe e nome, l'apertura con il modello scelto (D8, D10) | 4–5 |
| 21.7 | **Le icone**: le ricette Z-Image, i fogli per Mirco, l'import (D13) | 2–3 |
| 21.8 | **Bilancio e chiusura**: il Mago nella misura e nella ronda, i numeri ritarati, le ADR, le lezioni, il piano, il README, `bundleVersion` 1.9.0, il tag `v1.9.0` dopo i Tests verdi in CI (D11) | 4–5 |

---

## Passo 21.1 — Il disegno

**Com'è andata (2 nov 2026).** Il disegno sta in `M21_albero_Mago.md`, generato con la pagina per Mirco dagli
stessi dati: l'albero della classe ha 19 nodi e 34 gradi, *Fuoco* 15 nodi e 29 gradi, *Gelo* 15 nodi e 27, con
quattro stili di gioco (il rogo, il gelo, lo scudo, il passo) e **undici abilità attive**: quattro nella
classe (Dardo di fuoco, Anello di fuoco, Passo di luce, Scudo di mana), tre nel Fuoco (Palla di fuoco, Muro di
fiamme, Meteora) e quattro nel Gelo (Dardo di ghiaccio, Nova di gelo, Armatura di ghiaccio, Tempesta di neve).
Una verifica dei dati controlla che ogni nodo stia una riga sotto quelli a cui è legato, che i posti non si
sovrappongano e che ogni albero abbia una radice sola, attiva. Al livello 50 i punti coprono l'85% della
classe e il 75–81% delle specializzazioni. Mirco ha approvato il disegno il 2 novembre 2026, senza tagli.

## Passo 21.2 — La classe come dato (D2, D7, D9)

**Com'è andata (2 nov 2026).** `CharacterClass` (in `Core/Classes`) è un asset: id, nome, corpo, albero della
classe e specializzazioni, attributi di partenza, schema dei punti, fattore del mana e abilità del sinistro. Il
Guerriero (`Data/Classes/Warrior`) prende gli attributi dal prefab, quindi il cavaliere di sempre resta lo
stesso; il Mago (`Mage`) ha Forza 8, Destrezza 17, Magia 35 e Vitalità 20, spende i punti Magia, Vitalità,
Magia, Vitalità, Destrezza e riprende il mana a 1,5 volte quello di tutti, cioè il 3% al secondo (D7). Gli
alberi del Mago (`Mage`, `MageFire`, `MageFrost`) arrivano al 21.5: finché mancano il Mago ha
`IsPlayable` falso e la creazione non lo mostrerà. L'elenco è `ClassCatalog`; un id che non c'è torna il Guerriero.

- **Un prefab solo, due corpi.** I modelli KayKit hanno tutti lo stesso scheletro. `PlayerBodyTools` aggiunge alla
  `Model` del giocatore i pezzi del Mago (cappello, mantello, tunica, gambe), legati per nome alle ossa del
  cavaliere e spenti; `PlayerBody` ne accende uno e spegne l'altro secondo la classe. L'Animator, le mani per le
  armi e il lampo del colpo restano quelli di prima, e le foto lo mostrano: il Mago a riposo, al colpo e al
  lancio con le clip del cavaliere (`Recordings/Photos/mago_corpo`). In mano ha ancora la spada e lo scudo, finché
  non ci sono bastone e bacchetta (21.3).
- **`PlayerClass`.** `Apply(classe, nome, attributiNuovi)` dà gli alberi (`PlayerTalents.SetClass`), il corpo, il
  mana e, a un personaggio nuovo, gli attributi con vita e mana pieni. Gli alberi si cambiano **nello stesso
  `TalentBuild`** (`SetTrees`): il pannello, l'altare e il salvataggio che l'hanno già collegato lo ascoltano
  ancora, e il suo stato riparte da zero. Una classe senza albero non si applica agli alberi.
- **Il salvataggio 9.** Aggiunge `class` e `name`; la migrazione dall'8 mette Guerriero e nome vuoto, che la
  schermata chiamerà con il nome di partenza della classe. `SaveGame` scrive la classe e la rimette **per prima**,
  prima di attributi, livello e albero, perché cambiare gli alberi azzera lo stato. Il campione `save_v8` si
  carica e passa al 9.
- **Da riga di comando.** `-class mage` e `-heroname Nome` (con `-newgame`) scelgono la classe di una partita
  nuova, e `-herolevel` spende i punti con lo schema della classe (`HeroSetup.RaiseTo` con lo schema); il
  composition root la applica in `Start`, dopo gli Awake. `CompositionRoot.ApplyClass` è quello che userà la
  schermata della creazione.
- **Il Guerriero non si muove (D14).** Un test congela i colpi al bruto della misura del 20.8 a un centesimo
  (`WarriorReference_KeepsItsNumbers`), così un rifacimento della misura o una classe nuova non lo sposta in
  silenzio dentro le fasce. `HeroSetup.RaiseTo` ha ora una variante con lo schema, e quella di sempre la usa.
- **Corretta la D7.** La proposta diceva che il mana del Guerriero non si rigenera: torna già al 2% al secondo
  (`ManaRegenPerSecond`). Il Mago non è «lento», è più veloce: 3%, con il fattore 1,5 nel dato della classe.

**Test.** EditMode 587 e PlayMode 309 (2 saltati) verdi. EditMode: `CharacterClassTests` (l'elenco, il Guerriero uguale al prefab e a `HeroSetup`, il Mago, il
cambio d'alberi nello stesso `TalentBuild`, i due corpi sulle stesse ossa), `SaveMigrationTests` (il formato 9, il
nome e la classe, la migrazione dal 8), `WarriorReference_KeepsItsNumbers`. PlayMode: `ClassPlayModeTests` (di
partenza il Guerriero; il Mago al posto del cavaliere e ritorno; gli alberi nello stesso oggetto; il salvataggio
che rimette la classe; `-class` e `-heroname`).

---

## Passo 21.3 — Le armi del Mago (D4, D12)

Fatto il 3 novembre 2026.

- **Tre oggetti nuovi** in `Data/Items`, con i modelli di *Adventurers* (`staff`, `wand`, `spellbook_closed`, presi
  dallo zip già scaricato) e le icone dal modello: il **Bastone** (due mani, 12–18, ogni 1,5 s, Magia 40), la
  **Bacchetta** (una mano, 4–7, ogni 0,8 s, Magia 20) e il **Tomo** (mano libera, Magia 30, dà sempre +5 Magia e
  +20 mana). `WeaponKind.Staff` e `Wand` in fondo all'elenco.
- **Il requisito di Magia.** `ItemDefinition.RequiredMagic` (virtuale, 0 per tutti gli altri); `Equipment.MeetsRequirements`
  controlla Forza e Magia, e il tooltip scrive «Magia richiesta» in rosso se non basta. Il Guerriero (Magia 10) non
  porta niente di questo, il Mago che parte (Magia 35) porta bacchetta e tomo e il bastone dal 40.
- **Il tomo non è uno scudo.** Sta nello slot della mano sinistra ma `ArmorDefinition.IsShield` è falso: non blocca
  (senza questa regola avrebbe dato il blocco di Destrezza/2 al Mago), non ripara, e l'arma a due mani lo toglie come
  fa con lo scudo. `PlayerInventory` e `PowerCensus` guardano `IsShield`, non più «è un'armatura».
- **Gli affissi (D12).** Nuovo `AffixTargets.Caster`: bastone e bacchetta sono `Weapon | Caster`, il tomo solo
  `Caster`. Gli affissi della Magia e del mana (12 asset) ammettono anche `Caster`; blocco e Armatura dello scudo non
  cadono sul tomo, e sulle armi del Mago non cadono quelli degli scudi. **Rimandati al 21.4** il danno degli
  incantesimi e la ricarica: servono le loro statistiche, che nascono con le abilità.
- **Il mercante.** `ShopGoods.Arcane` (32): `ItemGoods.Of` dà «Arcane» a bastone, bacchetta e tomo, che il fabbro non
  tratta più; il banco del mercante vende gioielli, pergamene e merce arcana, e tira sei oggetti invece di quattro.
  Le tre voci sono anche nelle tabelle dei forzieri, dei bruti e del re.
- **Lo strumento** `MageWeaponTools` (menu *Oggetti → Armi del Mago*) rifà tutto e si rilancia senza duplicare voci.
  Trappola trovata: `ItemTools.UpdateDatabase` rifà anche il database degli affissi **in un altro ordine**, e l'ordine
  sposta le scelte a seme dei Guerriero: la misura delle difficoltà è saltata di colpo (6,95 → 7,06). Lo strumento ora
  rifà il database solo se un oggetto manca.
- **La posa in mano** è stata ritoccata con la foto: il bastone e la bacchetta partivano rovesciati (sfera in giù), ora
  girati di 180° sulla X; l'icona del tomo era di taglio, ora a tre quarti con le fibbie.
- **Da fare più avanti.** Il Mago nato dalla schermata prende ancora il kit di partenza del Guerriero (ascia e scudo in
  borsa): lo deciderà il 21.6, con la creazione.

**Test.** EditMode 593 verdi (`MageWeaponTests`: i numeri degli asset, il requisito di Magia, il tomo con i suoi
bonus e la regola delle mani, gli affissi ammessi, il loot e i banchi, il tooltip; adeguati `ItemDatabaseTests`,
`ShopTests`); PlayMode mirato (banchi, equipaggiamento, borsa, classe, loot) verde; foto in
`Recordings/Photos/mago_armi` (bacchetta e tomo, bastone, a riposo, al colpo, al lancio, la borsa).

---

## Passo 21.4 — Le abilità del Mago (D3, D6)

Fatto il 3 novembre 2026.

**Il gelo dei nemici (D6).** `EnemyAI.Slow(parte, secondi)` e `EnemyAI.Freeze(secondi)`. Il **congelamento** ha la regola
dello stordimento (un `StunTimer` suo: metà per il re, i campioni e gli unici, mai per un Inamovibile, 3 s di tregua
dopo), ma a parte: un nemico stordito si può congelare e viceversa. Fermo, senza colpi né lanci, con l'animazione ferma.
`EnemyAI.IsImmobilized` vale per tutti e due, e il re guarda quello. Il **rallentamento** (`SlowTimer`, logica pura) toglie
una parte della velocità dell'agent: la metà per il re e i campioni, niente tempo di pausa, un secondo colpo tiene il più
forte e il più lungo; se il Rapido o il re furioso cambiano la velocità mentre dura, la nuova è la base. La tinta fredda
la dà `EnemyFrostView`, che nasce da solo al primo gelo (niente da mettere nei prefab) e rimette la tinta di prima, quella
dei campioni compresa.

**Il Dardo arcano (D3).** Il sinistro del Mago passa dal colpo di `MeleeAttack`: stessa scelta del bersaglio, stesso
inseguimento e stesso ritmo dell'arma (bacchetta 0,8 s, bastone 1,5 s), ma `SetBolt` lo fa partire da **9 m** (con la linea
libera sul NavMesh: dietro uno spigolo ci si avvicina), con il gesto del lancio dopo 0,25 s. Il danno è il **tiro
dell'arma** più Magia / 6 (con il +% dei suoi affissi e il bonus del danno), senza tiro per colpire: un incantesimo non
manca. Costa niente e non ricarica, quindi sta nella casella del sinistro (`IsPrimary`). Il proiettile lo lancia
`PlayerSpells.Shoot` (`IBoltShooter`). `PlayerMagic.SetAttackSpell` mette il dardo al posto dell'Attacco quando
`PlayerClass.Apply` dà la classe, e rimette l'Attacco al Guerriero.

**Le dodici abilità** (`MageSpellTools`, menu *Classi → Abilità del Mago*): sei sono tipi di sempre con i numeri nuovi,
sei hanno un tipo loro (`SpellKind.ArcaneBolt`, `Teleport`, `ManaShield`, `Meteor`, `Zone`).

| Abilità | Tipo | Mana | Ricarica | Numeri |
|---|---|---|---|---|
| Dardo di fuoco | proiettile | 6 | 2 s | 6–12 + Magia/4 |
| Anello di fuoco | area | 12 | 8 s | 7–14 + Magia/3,3, raggio 3 m |
| Passo di luce | `Teleport` | 8 | 12 s | fino a 8 m, sopra i nemici, non sui muri |
| Scudo di mana | `ManaShield` | 10 | 40 s | 12 s, 2 mana per punto di danno |
| Palla di fuoco | proiettile che scoppia | 14 | 6 s | 12–24 + Magia/2, scoppio 3 m |
| Muro di fiamme | `Zone` (riga) | 16 | 20 s | 5 m, 6 s, ogni 0,5 s 2–5 + Magia/10 |
| Meteora | `Meteor` | 30 | 45 s | dopo 1,2 s, 36–72 + Magia/0,7, 4 m |
| Dardo di ghiaccio | proiettile | 8 | 3 s | 6–12 + Magia/4, rallenta 40% per 3 s |
| Nova di gelo | area | 12 | 14 s | 3,5 m, congela 1,5 s, nessun danno |
| Armatura di ghiaccio | potenziamento | 12 | 35 s | +40% armatura 15 s, rallenta 30% chi colpisce di mischia |
| Tempesta di neve | `Zone` (cerchio) | 28 | 60 s | 5 m, 6 s, ogni 0,5 s 2–5 + Magia/10, rallenta 50% |

Le proporzioni del disegno si leggono nei dati (un test le controlla: la Palla 200% del Dardo di fuoco, la Meteora 300%
della Palla, il Dardo di ghiaccio 100%, l'Anello 120%, il Muro 40% a battito). Gli effetti sono copie tinte di quelli
del Dardo e dell'Anello di fuoco (`Prefabs/Magic`); le icone sono provvisorie (quelle di sempre) fino al 21.7.

**Cambiati i numeri di due abilità che c'erano già** (D14 non li tocca: il Guerriero non le usa dall'M20): il Dardo di
fuoco passa da 0,8 a 2 s di ricarica; l'Anello da 15 mana e 3 s a 12 mana e 8 s, raggio da 4 a 3 m.

**Come i nodi dell'albero le cambieranno** (21.5): `AbilityParam` ha ora `Slow` e `Freeze`, e `Radius` legge lo scoppio per
un proiettile e il raggio per le aree (`SpellDefinition.EffectRadius`); costo, ricarica, durata e danno (`Damage`, in
punti percentuali) passano già dai nodi. `PlayerSpells.Struck` dice quando un'abilità ha colpito un nemico, per la
Concentrazione e la Pira.

**Il resto.** `Health.Absorber` (un filtro sul danno, dopo le riduzioni) è come lo Scudo di mana fa pagare il mana;
`DamageType.Frost` è nuovo; `MageAbilities` sta sul prefab del giocatore.

**La sagoma del personaggio (richiesta di Mirco, 3 novembre).** Dietro nemici e persone del borgo la sagoma gialla del
personaggio non deve comparire: va bene dietro case, alberi e muri, perché senza non si saprebbe dov'è, ma non dietro un
nemico. Nemici e persone (11 prefab con un Animator, i filmati esclusi) stanno ora sul layer **Characters** (11) e si
disegnano a parte come il personaggio, **scrivendo lo stencil 1**; la sagoma lo rispetta (*NotEqual 1*) e non passa dove
c'è uno di loro. L'ordine dei renderer è: chi copre, la sagoma, il personaggio. `HeroSilhouette.Setup` rifà tutto
(layer, due renderer, prefab). Due test lo controllano; foto in `Recordings/Photos/mago_abilita`.

**Foto degli effetti** (`PhotoTour.MageSpells`, in `Recordings/Photos/mago_abilita`): il Dardo arcano viola, il Dardo
di ghiaccio, la Nova, il Muro, la Tempesta, la Meteora in aria e allo scoppio, lo Scudo di mana, l'Armatura di ghiaccio,
il Passo di luce. Trovato dalla foto: lo Scudo di mana non aveva l'alone (l'alone si accende con `Play()`, non
attivando l'oggetto): corretto, con un controllo nel test. **Da rivedere con Mirco:** gli effetti del ghiaccio sono
copie dell'Anello di fuoco tinte con il colore di avvio delle particelle e vengono verdastri (la texture è arancione e il
colore la moltiplica); il Muro di fiamme è una fila di cerchi, non un muro. Sono provvisori come le icone.

---

## Passo 21.5 — L'albero del Mago (D5)

Fatto il 3 novembre 2026.

**I dati.** `MageTalentTools.BuildMage` (menu *Talenti → Costruisci il Mago*) rifà gli alberi dal disegno: `Mage` (19 nodi,
34 gradi), `MageFire` (15, 29) e `MageFrost` (15, 27) in `Data/Talents`, con le stesse linee del Guerriero (la classe a 6 e
14 punti, le specializzazioni a 5 e 12), e li dà alla classe `Mage`, che da qui ha `IsPlayable` vero. Righe e colonne del
disegno partono da 1, nel codice da 0. Il pannello e l'altare non sapevano nulla del Guerriero (leggono gli alberi del
`TalentBuild`, che la classe cambia con `SetTrees`): per il Mago servono solo i dati e i testi, e l'altare azzera i suoi
punti come quelli del Guerriero (un test lo guarda con `Build.Reset`).

**Gli effetti nuovi.** Quasi tutto il disegno stava già nei tre tipi di effetto della M20 (il numero di un'abilità, un
modificatore della scheda, un innesco). Per il resto:

| Nuovo | Dove | A cosa serve |
|---|---|---|
| `StatType.SpellDamage` | `RollDamage` degli incantesimi e il Dardo arcano (`MeleeAttack.ResolveBolt`) | Mente acuta, Maestro del fuoco, Mente infuocata, Maestria del fuoco |
| `StatType.MaxManaPercent` | `CombatFormulas.MaxMana(…, percent)`, `PlayerMagic`, scheda del personaggio | Riserva di mana, Cuore arcano |
| `StatType.ManaRegenPercent` | `PlayerMagic.Update`: il mana torna a `ManaRegenPerSecond × (1 + %)` | Mente fredda (due gradi: il 6% al secondo) |
| `TalentTrigger.BoltHit`, `Freeze` | `PlayerTalents` ascolta `PlayerSpells.Struck` (solo per il Dardo arcano) e `Froze` | Concentrazione, Scintilla, Mente di ghiaccio |
| `TalentOutcome.Cast` | `PlayerSpells.CastFree` e `Chance` (col flusso casuale del gioco) | Scintilla: il 10% di un Dardo di fuoco gratis |
| `AbilityParam.Burn` | `PlayerSpells.ApplyBurn`: brucia il 60% del colpo in tanti secondi, a battiti di 0,5 s, e l'ultimo porta il resto | Fiammata, Anello infuocato |
| `AbilityParam.Echo` | la Nova si ripete dopo 1 s (`PlayerSpells`), la Meteora cade ancora (`MageAbilities`) | Nova doppia, Meteora doppia |
| `AbilityParam.Ground` | `MageAbilities.OpenGround`: la Palla lascia un cerchio che brucia, il 10% del suo danno a ogni battito | Marea di fuoco |

La Pioggia di scintille è il raggio dello scoppio (`AbilityParam.Radius`, che per un proiettile è lo scoppio) del Dardo di
fuoco, quindi non ha codice suo. Il rallentamento della Nova c'è nell'asset (40%) ma dura 0 s: Gelo che resta gli dà 3 s,
che cominciano a gelo finito (la durata è il gelo più i suoi secondi).

**Le frasi** (`TalentDescriber`): ogni effetto si scrive dai suoi numeri, come per il Guerriero; il lancio gratis nomina
l'abilità. Nomi, tre alberi, descrizioni delle due specializzazioni e nuove frasi sono nelle due lingue.

**Da rivedere con Mirco.**
- **Nova doppia** è debole: un nemico congelato non si congela di nuovo per 3 s (la regola dello stordimento, D6), quindi la
  seconda Nova prende solo chi è arrivato nel secondo che passa. Si può cambiarla (per esempio in una Nova più larga, o che
  non ha il tempo di pausa) se non piace.
- I passivi non hanno ancora l'icona (21.7): nel pannello c'è il nome nel cerchio, e i più lunghi (*Concentrazione*)
  escono dal bordo.
- La Fiammata e l'Anello infuocato non hanno ancora un segno sul nemico che brucia: il danno c'è, la fiamma no.

**Test.** EditMode 610 verdi (`MageTreeTests`: forma, abilità, linee, nodi raggiungibili, classe, i nodi a scelta e i
passivi nuovi sulle righe giuste; `TalentTextTests` con i tre alberi del Mago e le frasi nuove). PlayMode: `MageTalentPlayModeTests`
(14): le abilità nel libro e l'altare, i passivi sulla scheda, la rigenerazione, il danno degli incantesimi, Concentrazione,
Scintilla e il lancio gratis, Fiammata, Pioggia di scintille, Meteora doppia, Marea di fuoco, Nova doppia, Gelo che resta, Mente di
ghiaccio. Foto in `Recordings/Photos/mago_albero` (la scelta tra Fuoco e Gelo, la classe e il Fuoco a buon punto, un
tooltip, un nodo a scelta aperto, il Gelo).

---

## Checklist di chiusura

- [x] Disegno dell'albero del Mago approvato da Mirco (21.1)
- [x] `CharacterClass`, salvataggio 9 e migrazione, fattore del mana (21.2)
- [x] Bastone, bacchetta e tomo nel loot e dal mercante (21.3)
- [x] Dardo arcano e le undici abilità del Mago (21.4)
- [x] Albero del Mago nel pannello; altare per entrambe le classi (21.5)
- [ ] Creazione del personaggio con nome; i tre posti; foto approvate (21.6)
- [ ] Icone approvate (21.7)
- [ ] Misura delle difficoltà con il Mago dentro la tolleranza; ronda verde (21.8)
- [ ] EditMode e PlayMode verdi in locale e in CI
- [ ] ADR, lezioni nel piano, README, prove da fare, `bundleVersion` 1.9.0
- [ ] Tag `v1.9.0` dopo i Tests verdi, build verde, peso dell'artefatto
