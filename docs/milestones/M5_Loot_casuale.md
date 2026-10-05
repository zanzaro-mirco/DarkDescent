# M5 — "Loot casuale"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI** avviata con
`-seed 4711`, il primo scheletro del livello 1 lascia sempre lo stesso oggetto: stesso nome,
stessi affissi, in due avvii diversi. Senza seme, in poche partite compaiono oggetti
normali, magici e rari, ognuno con il nome e la luce del suo colore. Il tooltip di un oggetto magico ne
mostra gli affissi e lo mette accanto a quello equipaggiato. Equipaggiato, gli affissi
cambiano il pannello del personaggio; tolto, tutto torna com'era. Con uno scudo, una parte
dei colpi degli scheletri viene **bloccata**, con animazione e suono. Il gioco parte in
**inglese**; avviato con `-lang it` (o cambiando lingua con il tasto provvisorio) è tutto in
italiano, nomi degli oggetti compresi. Test verdi in CI.

**Tempo stimato:** 9–12 h (piano v2.12), blocco e lingue compresi. **Prerequisito:** M4
chiusa (tag `m4`).

**Come si lavora:** come alla M4, il codice e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. A Mirco restano le decisioni qui sotto, il via ai download, le
prove in Play Mode e in build e la revisione degli ADR.

**Punto di controllo** (piano § 1.3) alla chiusura: settimane di calendario dalla chiusura
della M2 (3 ott 2026) contro le 42 h di M2.5–M5 a 6 h a settimana.

---

## Stato verificato il 4 ottobre 2026

| Cosa | Stato |
|---|---|
| Oggetti | Tre definizioni con ID stabile: Spada corta (6–9), Lama dello scheletro (8–12, Forza 25), Scudo con stemma (Armatura 5). `ItemInstance` porta solo l'ID della definizione |
| Drop | Ogni scheletro lascia sempre la sua lama (`LootDrop` con un oggetto fisso); lo scudo è messo dalla mappa del livello 1 |
| Statistiche | `StatSheet` con Forza, Destrezza, Magia, Vitalità e Armatura, modificatori fissi e percentuali per sorgente (ADR-020). `HealthModel.Max` è fissa dalla nascita (trappola 8 della M4) |
| Casualità | `IRandomSource` iniettata dal `CompositionRoot` per il combattimento (ADR-021); `SystemRandomSource` usa `System.Random` |
| Scudo | Armatura 5: lo scheletro passa dall'80% al 75% di colpi a segno, e non si sente. Mirco ha chiesto il blocco per questa milestone |
| Testi | Tutto in italiano e scritto a mano: etichette nelle scene (`Inventario`, `Arma`, `Forza`…), stringhe nel codice (`Mancato`, `inventario pieno`, le righe del tooltip), nomi degli oggetti nel campo `_displayName`. Nessun sistema di lingue: la localizzazione era fuori scope (piano § 1) fino alla v2.11 |
| Font | LiberationSans di TextMesh Pro (ADR-004): copre l'alfabeto latino, accenti compresi |
| Livelli | Due, costruiti da mappe di testo; `LevelContext` non sa a che profondità si trova |
| Modelli | Nel progetto `sword_1handed`, `shield_badge`, la lama dello scheletro. Nello zip di *Adventurers* 2.0 (in `Downloads`) ci sono anche `dagger`, `axe_1handed`, `shield_round`, `shield_square`, `shield_spikes`, e per dopo `sword_2handed`, `axe_2handed`, `staff`, `wand`, `bow`, `crossbow_*` |
| Animazioni | Nel `Rig_Medium_CombatMelee` già importato ci sono `Melee_Block`, `Melee_Blocking` e `Melee_Block_Hit` |
| Suoni | Importati solo `impactPunch`, `impactSoft` e `knifeSlice` di Kenney. Il pacchetto *Impact Sounds* ha anche `impactMetal`, ma lo zip non è più in `Downloads` |
| Test | 62 EditMode e 77 PlayMode verdi. Una ventina di asserzioni confrontano testi italiani ("Lama dello scheletro", "Danno: 8–12", "inventario pieno") |

---

## Decisioni

Tutte confermate da Mirco il 4 ottobre 2026. D4 e D7 sono riscritte secondo le sue
indicazioni; D12 e D13 sono nate dalla richiesta delle lingue. Lo zip di D11 lo scarico io.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Blocco** (richiesto da Mirco) | Solo con uno scudo. Probabilità = blocco dello scudo + Destrezza / 2, al massimo 75%. Scudo con stemma 10, quindi **20%** per il cavaliere. Si tira **dopo** il colpo a segno: il colpo bloccato non fa danno, sopra il cavaliere compare "Bloccato" e parte `Melee_Block_Hit` con un suono metallico. Il blocco interrompe il colpo che il cavaliere stava dando, come in Diablo 1. Gli scheletri non bloccano | Con lo scudo il cavaliere subisce il 60% dei colpi invece dell'80%: si sente. La Destrezza ottiene un secondo uso, come dice il § 2. L'interruzione è il prezzo del blocco in Diablo e lo rende una scelta, non un bonus gratis |
| D2 | **Rarità** | Tre. **Normale** (bianco, nessun affisso). **Magico** (blu, 1–2 affissi: al più un prefisso e un suffisso). **Raro** (giallo, 3–4 affissi, al più due prefissi e due suffissi). Al drop: **65% / 28% / 7%**. Se il pool di un oggetto non ha abbastanza affissi, ne prende quanti ce ne sono. Gli unici restano fuori | Le probabilità sono generose perché nei due livelli fatti a mano ci sono otto scheletri: un raro ogni due o tre partite. Si ritarano alla M10, con il gioco completo. Gli unici vogliono oggetti scritti a mano uno per uno, che oggi non ci sono |
| D3 | **Affissi** | Undici, ciascuno con un intervallo di valori interi, un livello minimo, i tipi di oggetto ammessi e un **gruppo**: due affissi dello stesso gruppo non stanno sullo stesso oggetto ("Affilato" e "Feroce"). Il danno e l'Armatura in percentuale sono **dell'oggetto** e cambiano i numeri dell'arma o dello scudo; gli altri sono **del personaggio** e passano dallo `StatSheet` | Come in Diablo: "+40% danno" cambia il danno della spada, non quello dei pugni. Valori interi: niente virgole nel tooltip e nessun problema di cultura (lezione della M4) |
| D4 | **Nomi degli oggetti in più lingue** | Il nome si compone con uno **schema per lingua**: in inglese "Savage Short Sword of the Griffin" (prefisso prima, suffisso dopo), in italiano "Spada corta Feroce del Grifone" (prefisso dopo il nome). Ogni base ha un **genere per lingua** (in inglese nessuno, in italiano m/f; altre lingue potranno usare anche il neutro); i prefissi hanno una forma per genere, con la forma senza genere come riserva. Il raro si chiama come il magico, con il primo prefisso e il primo suffisso, in giallo; il tooltip mostra tutti gli affissi | L'ordine delle parole e l'accordo cambiano da lingua a lingua: un nome incollato con il `+` funziona solo in inglese. Con schema e genere nella tabella delle stringhe, una lingua nuova è una colonna in più, senza codice |
| D5 | **Seme** | Un seme di partita: da riga di comando (`-seed 4711`), altrimenti casuale e scritto nel log. Ogni nemico riceve un seme suo, ricavato da seme di partita, livello e posizione nella mappa, quindi lo stesso nemico lascia lo stesso oggetto **in qualunque ordine** si uccidano. Il generatore usa un **PRNG nostro** (SplitMix64) dietro `IRandomSource`, separato da quello del combattimento | Con una sorgente sola, un colpo mancato in più cambierebbe il loot. `System.Random` non promette lo stesso algoritmo tra versioni di .NET e `string.GetHashCode` cambia tra runtime: il seme deve dare lo stesso oggetto anche in build Web e dopo un aggiornamento |
| D6 | **Cosa salva un oggetto** | `ItemInstance` aggiunge rarità, livello dell'oggetto, seme e la lista degli affissi con il **valore tirato** (ID dell'affisso + numero). **Mai il nome**: si compone nella lingua attiva ogni volta che serve | Il seme basta a rigenerare l'oggetto, ma se alla M10 si ritarano gli intervalli, gli oggetti già salvati cambierebbero. Con i valori salvati restano quelli trovati. Un nome salvato resterebbe nella lingua in cui è caduto l'oggetto |
| D7 | **Basi di questa milestone** | Otto oggetti a una mano. Armi: **Pugnale** (`dagger`, 1×2, 3–6), **Spada corta** (1×3), **Ascia** (`axe_1handed`, 2×3, 7–11, Forza 30), **Lama dello scheletro** (1×3). Scudi: **con stemma** (2×2, Armatura 5, blocco 10), **tondo** (`shield_round`, 2×2, 4, 15), **quadrato** (`shield_square`, 2×3, 8, 10), **chiodato** (`shield_spikes`, 2×2, 6, 5). Armi a due mani, bacchette, bastoni, archi e balestre arrivano **con le classi nuove**: sono un compito scritto nel piano (§ 5, "Dopo la v1.0"), come chiesto da Mirco. Da qui il codice tiene il **tipo di arma** come dato della definizione (enum `WeaponKind`), così un tipo nuovo non cambia la struttura | Scelte diverse sul serio: l'ascia chiede Forza, lo scudo tondo blocca e quello quadrato para. Con il tipo d'arma come dato, affissi e loot table già filtrano per tipo, e le armi a due mani della classe nuova dovranno aggiungere solo la regola sullo slot dello scudo |
| D8 | **Loot table e livello dell'oggetto** | Uno ScriptableObject `LootTable` per nemico: probabilità di lasciare qualcosa (scheletro **70%**) e basi pesate. Livello dell'oggetto = profondità del livello (1 o 2), scritta nella mappa con `@depth`. Lo scudo del livello 1 resta messo dalla mappa, normale. Gli oggetti a terra prendono il colore della rarità nel nome e nella luce | Il drop fisso della M4 serviva a tenere tutto deterministico; ora il determinismo lo dà il seme. La luce colorata a terra è il segnale più forte del genere: un bagliore giallo si riconosce da lontano |
| D9 | **Confronto nel tooltip** | Passando su un oggetto della griglia che va in uno slot occupato, accanto al suo tooltip ne compare un secondo con l'oggetto equipaggiato, intitolato "Equipaggiato" | Due riquadri si leggono senza fare conti, e riusano `ItemTooltip`. Le differenze in verde e rosso su ogni riga diventano illeggibili con 3–4 affissi |
| D10 | **Vita massima che cambia** (trappola 8 della M4) | Quando la vita massima sale, la vita attuale sale della stessa quantità; quando scende, scende della stessa quantità ma mai sotto 1 | È il comportamento di Diablo 1: togliere un oggetto della Vitalità non uccide mai, e indossarlo non è una cura gratis |
| D11 | **Download** | Lo zip di Kenney *Impact Sounds* (CC0, da `kenney.nl`, circa 1 MB: misura esatta prima di scaricarlo) per `impactMetal`. Lo riscarica Mirco, o lo scarico io con il suo via | Il blocco vuole un suono metallico; quelli importati sono pugni e colpi sordi |
| D12 | **Sistema delle lingue** (nuova) | **Un sistema nostro, piccolo.** Una tabella CSV nel repo (`Data/Localization/Strings.csv`): una riga per chiave, **una colonna per lingua** (`key,en,it`). `Localizer` (logica pura) la legge, risponde con la lingua attiva e, se manca una traduzione, ricade sull'inglese. Le etichette delle scene hanno un componente `LocalizedText` con la chiave; il codice chiede le stringhe per chiave. Cambiare lingua manda un evento: chi mostra testo si ridisegna, nessuno controlla a ogni frame. Le definizioni (oggetti, affissi) tengono una **chiave**, non più un nome. Un test controlla che ogni chiave usata esista e che ogni lingua abbia tutte le righe | Il pacchetto *Unity Localization* è lo standard e farebbe bella figura nel CV, ma porta con sé Addressables, carica le tabelle in modo asincrono (in build Web non si può aspettarle in modo sincrono) e aggiunge un passaggio di build alla CI. Per due lingue e qualche centinaio di righe, una tabella CSV si rivede in un diff, si passa a un traduttore così com'è e una lingua nuova è una colonna. Se un giorno servisse il pacchetto, le chiavi restano le stesse |
| D13 | **Come si sceglie la lingua** (nuova) | Inglese di default. Si cambia con `-lang it` da riga di comando, oppure con un **tasto provvisorio** (`F9`, passa alla lingua successiva) finché non c'è il menu delle opzioni della M10, dove andrà la scelta vera. La scelta resta salvata tra un avvio e l'altro (`PlayerPrefs`) | Senza un menu, un tasto è il modo più rapido per provare le due lingue anche in build Web, dove la riga di comando non c'è. `PlayerPrefs` è il posto giusto per una preferenza: non fa parte del salvataggio della partita della M8 |

### Affissi proposti (D3)

| Tipo | Inglese | Italiano (m/f) | Effetto | Su | Livello minimo | Gruppo |
|---|---|---|---|---|---|---|
| Prefisso | Sharp | Affilato / Affilata | +20–40% danno dell'arma | armi | 1 | danno |
| Prefisso | Savage | Feroce | +41–65% danno dell'arma | armi | 2 | danno |
| Prefisso | Accurate | Preciso / Precisa | +5–10 a colpire | armi | 1 | colpire |
| Prefisso | Sturdy | Robusto / Robusta | +2–4 Armatura | scudi | 1 | armatura |
| Prefisso | Massive | Massiccio / Massiccia | +25–50% Armatura dello scudo | scudi | 2 | armatura % |
| Suffisso | of Strength | della Forza | +2–5 Forza | tutti | 1 | forza |
| Suffisso | of Dexterity | della Destrezza | +2–5 Destrezza | tutti | 1 | destrezza |
| Suffisso | of Vitality | della Vitalità | +2–5 Vitalità | tutti | 1 | vitalità |
| Suffisso | of the Bear | dell'Orso | +5–10 vita | tutti | 1 | vita |
| Suffisso | of the Griffin | del Grifone | +11–20 vita | tutti | 2 | vita |
| Suffisso | of Blocking | della Parata | +5–10% blocco | scudi | 1 | blocco |

Servono due statistiche nuove del personaggio: **a colpire** (si somma alla probabilità di
colpire) e **vita** (si somma alla vita massima). Il blocco dello scudo è dell'oggetto.

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 5.1 | Lingue: tabella, `Localizer`, testi esistenti in inglese e italiano | 1,5–2 |
| 5.2 | Blocco con lo scudo | 1 |
| 5.3 | Affissi, rarità e generatore | 1,5–2 |
| 5.4 | Statistiche nuove e vita massima variabile | 0,5–1 |
| 5.5 | Basi nuove: modelli, definizioni e icone | 1 |
| 5.6 | Loot table, profondità e seme | 1–1,5 |
| 5.7 | Nomi, colori e tooltip con confronto | 1–1,5 |
| 5.8 | Chiusura: GIF, ADR, punto di controllo, tag `m5` | 0,5 |

Le lingue vanno per prime: ogni testo nuovo della milestone nasce già con la sua chiave.

---

## Architettura

```
Core
├─ CompositionRoot      seme di partita (-seed), lingua (-lang), sorgenti del combattimento e del loot
├─ LanguageSwitch       tasto provvisorio, preferenza in PlayerPrefs
├─ Player
│  ├─ CharacterStats    + a colpire, + vita
│  ├─ PlayerInventory   oggetti con affissi: modificatori per sorgente come alla M4
│  └─ ShieldBlock       tiro del blocco, animazione, suono
└─ HUD
   ├─ LocalizedText     su ogni etichetta fissa
   └─ ItemTooltip × 2   l'oggetto sotto il cursore e quello equipaggiato

Logica pura
├─ Localization
│  ├─ StringTable       CSV → chiave × lingua, con ricaduta sull'inglese
│  ├─ Localizer         lingua attiva, Get(chiave), evento LanguageChanged
│  └─ ItemNamer         schema della lingua + genere della base + forme del prefisso
├─ Items / Loot
│  ├─ AffixDefinition   ScriptableObject: ID, chiave del nome, effetto, intervallo, livello, tipi, gruppo
│  ├─ ItemGenerator     (base, livello, seme) → ItemInstance, sempre lo stesso
│  ├─ RarityTable       probabilità delle rarità, estrazione pesata
│  ├─ LootTable         ScriptableObject per nemico: probabilità di drop, basi pesate
│  ├─ SplitMix64Source  IRandomSource riproducibile su ogni piattaforma
│  └─ SeedMixer         seme di partita + livello + posizione → seme del nemico

Livelli
└─ Skeleton             LootDrop legge la LootTable e il seme del nemico
```

### Contratti

- **`Localizer`**: `Language` (codice ISO: `en`, `it`), `SetLanguage(string)`,
  `Get(string key)`, `Format(string key, params …)` senza allocare quando non ci sono
  argomenti, evento `LanguageChanged`. Una chiave mancante restituisce `#chiave`, che si vede
  subito a schermo e nei test.
- **`ItemNamer.Name(ItemInstance, Localizer)`**: schema `item.name.pattern` della lingua,
  genere `item.<base>.gender`, prefisso `affix.<id>.<genere>` o `affix.<id>`.
- **`ItemGenerator.Generate(ItemDefinition base, int itemLevel, ulong seed)`**: stesso
  ingresso, stesso oggetto. Sceglie la rarità, poi gli affissi ammessi (tipo, livello,
  gruppo libero), poi i valori.
- **`ItemInstance`**: in più `Rarity`, `ItemLevel`, `Seed`, `Affixes` (lista di ID e valori);
  resta serializzabile con `JsonUtility`.
- **`Equipment`**: per ogni affisso del personaggio un modificatore con l'oggetto come
  sorgente, come l'Armatura alla M4. Gli affissi dell'oggetto cambiano danno, Armatura e
  blocco letti da `MeleeAttack`, dallo scudo e dal tooltip.
- **`HealthModel.SetMax(float)`**: cambia la vita massima con la regola di D10 e manda
  `Changed`.
- **`ShieldBlock`**: `Health` gli chiede se il colpo è bloccato prima di applicare il danno;
  usa la sorgente del combattimento, così i test restano sulla sorgente fissa.

---

## Passo 5.1 — Lingue

1. `StringTable`, `Localizer`, `LocalizedText`, `LanguageSwitch`; il `CompositionRoot` crea il
   `Localizer` dalla tabella, legge `-lang` e la preferenza salvata.
2. Tutti i testi di oggi diventano chiavi: etichette delle scene (con uno script di editor
   che mette `LocalizedText` sulle etichette dell'HUD), stringhe nel codice, nomi dei tre
   oggetti (da `_displayName` a una chiave). Traduzione inglese di tutto.
3. I pannelli e i numeri di danno si ridisegnano su `LanguageChanged`.
4. I test esistenti girano in inglese, la lingua di default; le asserzioni sui testi si
   aggiornano.

**Test:** ricaduta sull'inglese per una traduzione mancante; `#chiave` per una chiave che
non c'è; ogni chiave usata da scene, codice e definizioni esiste; ogni lingua ha tutte le
righe; cambiare lingua ridisegna pannello del personaggio, tooltip ed etichetta a terra; `F9`
passa da inglese a italiano e la scelta resta dopo un nuovo avvio di `Core`.

**Com'è andata (4 ott 2026).** `StringTable` legge `Data/Localization/Strings.csv`
(intestazione `key,en,it`, virgolette, virgolette raddoppiate, `
`, commenti con `#`) e
rifiuta chiavi ripetute e virgolette non chiuse. `Localizer` (logica pura) tiene la lingua
attiva, ricade sull'inglese, restituisce `#chiave` per una chiave che non c'è e manda
`LanguageChanged`; `Format` usa sempre le cifre invarianti. Le chiavi del codice stanno in
`TextKeys`. Nessun singleton (ADR-007): il `CompositionRoot` crea il `Localizer` dalla
tabella, legge `-lang` (`CommandLine`, che servirà anche per `-seed`) o la preferenza in
`PlayerPrefs`, e lo passa a etichette, numeri di danno, etichetta sotto il cursore e
inventario. **Etichette fisse:** uno script di editor ha messo `LocalizedText` sulle otto
etichette dell'HUD (titoli, slot, nomi delle statistiche, schermata di morte). **Oggetti:**
`_displayName` è diventato `_nameKey` (`item.short_sword`…), armi dei nemici comprese.
**Nomi sotto il cursore:** `Interactable` tiene una chiave e un argomento (le scale:
`exit.descend` con 2, livelli ricostruiti dal builder), oppure una sorgente che compone il
nome da sé (`ILabelSource`): l'oggetto a terra, con "(inventory full)". Il nome si compone
quando serve, nella lingua del momento. **F9:** azione `CycleLanguage`, import della
classe generata in due tempi come alla M4; la scelta va in `PlayerPrefs`. I test girano in
inglese (la fixture cancella la preferenza) e le asserzioni sui testi sono passate
all'inglese. Test: 72 EditMode (`LocalizationTests`, `LocalizationCoverageTests`: chiavi del
codice, degli oggetti, delle scene e dei prefab, lingue complete; `ItemDescriptionTests`
anche in italiano) e 79 PlayMode (`LanguageSwitchTests`: F9, finestra chiusa al cambio,
preferenza al riavvio di `Core`, tooltip e nome a terra riscritti) verdi.

## Passo 5.2 — Blocco con lo scudo

1. Probabilità di blocco: blocco dello scudo + Destrezza / 2, al massimo 75 (logica pura in
   `CombatFormulas`).
2. `ShieldBlock` sul cavaliere: con uno scudo equipaggiato, dopo un colpo a segno tira il
   blocco. Bloccato: niente danno, evento `Blocked`, "Blocked"/"Bloccato" sopra il cavaliere,
   `Melee_Block_Hit`, suono metallico (`impactMetal`), colpo in corso interrotto.
3. Il pannello del personaggio mostra la probabilità di blocco.

**Test:** probabilità limitata a 75; senza scudo mai un blocco; con la sorgente fissa il colpo
bloccato non toglie vita e interrompe l'attacco del cavaliere. I test che contano i colpi
ricevuti restano verdi perché il cavaliere parte senza scudo: con la sorgente fissa a 0,0
ogni tiro di blocco riuscirebbe.

**Com'è andata (4 ott 2026).** `CombatFormulas.BlockChance` (scudo + Destrezza / 2, tra 0
e 75) e `RollBlock`. `ArmorDefinition` ha il suo `_blockChance` (scudo con stemma 10, quindi
20% per il cavaliere). `ShieldBlock` sul cavaliere: lo scudo glielo dà `PlayerInventory`
quando cambia lo slot, come l'arma a `MeleeAttack`. `MeleeAttack` tira nell'ordine colpito,
bloccato, danno, con la sorgente del combattimento; il colpo bloccato non tira il danno.
Bloccato: `Interrupt` del colpo del cavaliere (0,45 s), evento `Blocked`, a cui ascoltano
`CharacterAnimatorDriver` (stato `Block` nuovo nel controller, clip `Melee_Block_Hit` da
1,07 s, uscite come `Hit`), `CharacterAudio` (tre `impactMetal_light` da *Impact Sounds*,
riga in `CREDITS.md`) e `DamageNumbers` ("Blocked"/"Bloccato" in azzurro). Il pannello del
personaggio mostra il blocco su una riga nuova e si aggiorna con `ShieldChanged`: con
l'evento dell'equipaggiamento, l'ordine tra il suo handler e quello dell'inventario non è
garantito. Test: 73 EditMode (`BlockChance_ClampedTo75`) e 83 PlayMode (`ShieldBlockTests`:
blocco 0 e 20% anche nel pannello, colpi dello scheletro bloccati senza danno con scritta e
animazione, nessun blocco senza scudo, colpo del cavaliere annullato) verdi.

## Passo 5.3 — Affissi, rarità e generatore

1. `AffixDefinition`, `Rarity`, `RarityTable`, `ItemGenerator`, `SplitMix64Source`, tutto
   logica pura con ScriptableObject immutabili.
2. Gli undici affissi come asset in `Data/Affixes/`, con ID stabile come gli oggetti e un
   loro database; i nomi nella tabella delle stringhe.
3. `ItemInstance` con rarità, livello, seme e affissi, e il ritorno da JSON.

**Test:** stesso seme, stesso oggetto, sempre; un oggetto di livello 1 non ha mai "Savage" né
"of the Griffin"; mai due affissi dello stesso gruppo; su 100.000 estrazioni le rarità restano
entro l'1% da 65/28/7; il magico ha al più un prefisso e un suffisso; JSON e ritorno.

**Com'è andata (4 ott 2026).** `SplitMix64Source` (in `Core`, dietro `IRandomSource`, con
`NextUInt64` e i 53 bit alti per `NextDouble`; il test confronta i primi tre valori di
riferimento del seme 0). `Rarity`, `AffixKind`, `AffixEffect` (danno e Armatura in
percentuale e blocco sono dell'oggetto; Armatura, a colpire, attributi e vita del
personaggio), `AffixTargets` (flag: armi, scudi; le classi future aggiungeranno valori).
`ItemDefinition` dichiara il suo `AffixTarget`. `AffixDefinition`: ID stabile, chiave del
nome, tipo, effetto, intervallo intero, livello minimo, oggetti ammessi, gruppo.
`AffixDatabase` come quello degli oggetti, riempito dallo stesso menu *Aggiorna il
database*. `RarityTable` con i pesi 65/28/7. `ItemGenerator`: rarità, numero di affissi
(magico 1–2, raro 3–4), poi a ogni giro gli affissi ammessi con il tipo ancora libero e il
gruppo non usato, scelta e valore; liste riusate, un drop non crea liste nuove. Se il pool
non basta, l'oggetto ha gli affissi che ci sono. Un secondo `Generate` riceve la rarità da
fuori, per i test e per gli oggetti messi a mano. `ItemInstance` aggiunge rarità, livello,
seme e `ItemAffix` (ID e valore); `Resolve` con i due database. Undici affissi in
`Data/Affixes/` (creati da uno script di editor) e i loro nomi nella tabella, con la forma
femminile dei prefissi in `.f`. **Il test delle rarità** chiedeva l'1% su 10.000
estrazioni, ma su 10.000 lo scarto normale sul 65% è mezzo punto: con il seme scelto usciva
63,9%, un caso da due sigma e mezzo, non un difetto (lo stesso conto in Python dà gli
stessi numeri). Il test ora estrae 100.000 volte, con l'1% che vale quasi sette sigma.
Test: 81 EditMode (`ItemGeneratorTests`: valori di riferimento di SplitMix64, stesso seme
stesso oggetto su 300 semi, rarità su 100.000 estrazioni, livello minimo, limiti per
rarità, gruppi e oggetti ammessi su 1000 semi, JSON con affissi e seme, database completo;
nomi degli affissi nella copertura delle lingue) e 83 PlayMode verdi.

## Passo 5.4 — Statistiche nuove e vita massima variabile

1. `StatType` aggiunge *a colpire* e *vita*; `CombatFormulas.HitChance` somma il bonus.
2. `HealthModel.SetMax` con la regola di D10; `Health` ricalcola il massimo quando lo
   `StatSheet` cambia.
3. La sfera della vita e il pannello del personaggio si aggiornano dagli eventi.

**Test:** "+10 vita" alza massimo e attuale di 10; tolto, li abbassa ma mai sotto 1; "of
Vitality" passa dalla formula 50 + 2 × Vitalità; equipaggia e togli un oggetto con quattro
affissi riporta ogni statistica a prima.

**Com'è andata (4 ott 2026).** `StatType` aggiunge `ToHit` e `Life` in fondo (i valori
fanno da indice). `CombatFormulas.HitChance` somma il bonus a colpire, sempre tra 5 e 95;
`ApplyPercent` arrotonda per difetto le percentuali degli oggetti (spada corta +40%: 8–12).
`ItemStats` (logica pura) fa i conti di un oggetto con i suoi affissi: danno dell'arma,
Armatura e blocco dello scudo, e quali effetti vanno sul personaggio. `Equipment` mette sullo
`StatSheet` l'Armatura dello scudo già con il suo "+%" e un modificatore per ogni affisso
del personaggio, sempre con l'oggetto come sorgente. `MeleeAttack.SetWeapon` riceve il "+%
danno" e tiene il danno effettivo (`MinDamage`, `MaxDamage`); `ShieldBlock` riceve il
blocco con l'affisso. **Vita massima:** `HealthModel.SetMax` con la regola di D10 (sale e
scende della stessa quantità, mai sotto 1, da morto cambia solo il massimo); `Health`
ascolta lo `StatSheet` e ricalcola 50 + 2 × Vitalità + vita, e la sfera si aggiorna dal suo
evento. Il pannello del personaggio calcola il danno dall'oggetto equipaggiato e non da
`MeleeAttack`: i due ascoltano lo stesso evento e l'ordine non è garantito (lo stesso
motivo di `ShieldChanged` nel 5.2). Test: 86 EditMode (`SetMax` in salita, in discesa e da
morto; bonus a colpire e percentuali; scudo con quattro affissi che toglie tutto;
`ItemStats`) e 85 PlayMode (scudo della Vitalità e dell'Orso: vita da 100 a 120 e ritorno,
con 30 di danno in mezzo; spada Affilata: `MeleeAttack` 8–12 e pannello 10–16) verdi.

## Passo 5.5 — Basi nuove: modelli, definizioni e icone

1. Dallo zip di *Adventurers*: `dagger`, `axe_1handed`, `shield_round`, `shield_square`,
   `shield_spikes` in `Art/Models/KayKit/`, riga in `CREDITS.md`.
2. Definizioni con `WeaponKind`, chiave del nome, genere nelle due lingue, livello minimo,
   blocco per gli scudi; posizione in mano provata con le foto, come lo scudo della M4.
3. Icone con *Rigenera le icone*, database aggiornato.

**Test:** il test della M4 sugli ID unici copre i nuovi oggetti; ogni base ha icona, modello,
nome in ogni lingua e genere in italiano; ogni scudo ha un blocco tra 0 e 75.

**Com'è andata (4 ott 2026).** Dallo zip di *Adventurers* (cartella `fbx(unity)`):
`dagger`, `axe_1handed`, `shield_round`, `shield_square`, `shield_spikes`, con le texture
che usano (`rogue_texture` per il pugnale, `barbarian_texture` per l'ascia; gli scudi
usano `knight_texture`, già nel progetto). Le impostazioni d'importazione sono quelle di
`shield_badge`, con GUID nuovi; riga di `CREDITS.md` aggiornata. `WeaponKind` (pugni,
pugnale, spada, ascia) è un dato di `WeaponDefinition`; i colpi dello scheletro sono una
spada, i pugni `Unarmed`. Uno script di editor ha creato le cinque definizioni con i valori
di D7, copiando dalla spada corta portata e tempi del colpo (legati alla clip d'attacco) e
dallo scudo con stemma rotazione dell'icona e posizione in mano; poi *Rigenera le icone*
e il database. Le icone mostrano tutte il davanti; le foto del cavaliere da vicino, di
fronte e dal lato sinistro, mostrano gli scudi fuori dal braccio con il davanti verso
l'esterno, pugnale e ascia nella mano destra come la spada. Nomi e **generi** delle basi
nella tabella (`item.<base>.gender`: `n` in inglese, `m` o `f` in italiano). Un livello
minimo per le basi non serve ancora: quale base cade lo decidono i pesi della loot table
(5.6). Test: 88 EditMode (valori delle otto basi e tipi d'arma, generi in ogni lingua) e
85 PlayMode verdi.

## Passo 5.6 — Loot table, profondità e seme

1. `@depth` nelle mappe, letta da `LevelContext`.
2. `LootTable` per lo scheletro; `LootDrop` la usa con il seme del nemico (`SeedMixer`).
3. `-seed N` letto all'avvio dal `CompositionRoot`, altrimenti un seme casuale; il seme va nel
   log.
4. `GroundItem`: nome e luce del colore della rarità; il nome cambia con la lingua.

**Test:** stesso seme di partita, gli stessi scheletri lasciano gli stessi oggetti anche
uccisi in ordine inverso; seme diverso, oggetti diversi; nel livello 2 gli oggetti hanno
livello 2; la luce dell'oggetto raro è gialla.

**Com'è andata (4 ott 2026).** `@depth` nelle due mappe, letta dal builder in
`LevelContext.Depth` (livelli ricostruiti). `LootTable` (ScriptableObject): probabilità di
drop e righe `LootEntry` con base e peso; quella dello scheletro (`Data/Loot/SkeletonLoot`)
ha il 70% e le otto basi, con la lama più pesante. `SeedMixer` mescola seme della partita,
profondità e cella di partenza (in decimetri, presa in `Awake`: il nemico si muove, il suo
seme no) con il finale di SplitMix64. `LootRoller` (logica pura) tira drop e base con il
seme del nemico e passa al generatore un seme nuovo per l'oggetto; il livello dell'oggetto è
la profondità. `LootDrop` ora ha la loot table al posto dell'oggetto fisso, e un `Preview`
che dice cosa lascerà senza cambiare niente. Il `CompositionRoot` legge `-seed`, altrimenti
usa l'orologio, scrive il seme nel log ("per rigiocarla: -seed …"), crea generatore e
`LootRoller` con il database degli affissi e collega i `LootDrop` di ogni livello;
`UseLootSeed` lo cambia per i test. **A terra:** nome nel colore della rarità (bianco, blu,
giallo) e luce dello stesso colore, più satura (`RarityColors`). Nei test un aiuto della
fixture cerca il primo seme con cui un nemico lascia l'oggetto voluto, e i test del loot
della M4 confrontano il drop con la previsione invece della lama fissa. Test: 92 EditMode
(`LootTests`: seme del nemico stabile e sparso, stessi drop in ordine inverso e diversi con
un altro seme, 70% di drop e lama più frequente su 10.000 nemici, la loot table al limite)
e 88 PlayMode (`LootSeedTests`: seme 4711 nel livello 1 con gli scheletri uccisi in ordine
inverso e un nuovo avvio di `Core` uguale, raro con nome e luce gialli, profondità 2 e
oggetti di livello 2) verdi.

## Passo 5.7 — Nomi, colori e tooltip con confronto

1. `ItemNamer` con schema e genere; colore della rarità nel tooltip, nell'etichetta a terra e
   nel bordo della cella.
2. Tooltip con le righe degli affissi, il danno e l'Armatura già modificati, la probabilità
   di blocco degli scudi.
3. Secondo tooltip "Equipped"/"Equipaggiato" accanto al primo, dentro lo schermo tutti e due.

**Test:** "Sturdy Axe" e "Ascia Robusta", "Sturdy Badge Shield" e "Scudo con stemma Robusto";
il tooltip di una spada corta "Sharp" (+40%) mostra 8–12 invece di 6–9; passando su uno scudo
con lo scudo equipaggiato compaiono due riquadri che non si sovrappongono e non escono dallo
schermo; cambiando lingua il nome di un oggetto già nell'inventario cambia.

**Com'è andata (4 ott 2026).** `ItemNamer` compone il nome dallo schema della lingua
(`item.name.pattern`: "{prefix} {base} {suffix}" in inglese, "{base} {prefix} {suffix}"
in italiano), con il primo prefisso nella forma del genere della base (`.f` per le basi
femminili) e il primo suffisso; un pezzo che manca non lascia spazi doppi. Lo usano
l'etichetta a terra e il tooltip. `ItemDescription` ora descrive un `ItemInstance`: nome
nel colore della rarità, danno, Armatura e **blocco** già con gli affissi, Forza richiesta,
poi una riga blu per affisso ("+40% damage", "+9 vita"), con le righe degli effetti nella
tabella (una chiave per effetto, un test lo controlla). **Confronto:** un secondo
`ItemTooltip` (`CompareTooltip`, duplicato del primo nell'HUD da uno script di editor)
compare quando dalla griglia si passa su un oggetto il cui slot è occupato, intitolato
"Equipped"/"Equipaggiato" in piccolo; si mette a sinistra del primo, allineato in alto, o a
destra se a sinistra non c'è spazio, sempre dentro lo schermo. Il pannello espone i due
tooltip ai test: con due nella scena, "il primo che si trova" non vuol dire più niente.
**Celle:** ogni oggetto della griglia ha dietro uno sfondo nel colore della rarità, creato
insieme alla sua immagine (quindi dietro). La prima foto li mostrava accesi come pieni:
in color space lineare la trasparenza della UI si mescola in lineare e un colore al 30%
schiarisce molto. Ora sono scuri apposta. Test: 97 EditMode (`ItemNamerTests`: accordo,
schema, pezzi mancanti, raro; `ItemDescriptionTests` su istanze, con affissi, blocco e
italiano) e 89 PlayMode (confronto con lo scudo equipaggiato: due riquadri dentro lo
schermo che non si sovrappongono, nessun confronto senza scudo, chiusura) verdi.

## Passo 5.8 — Chiusura

1. GIF del README: una serie di uccisioni con oggetti di più colori, un raro raccolto e
   confrontato con l'arma in mano, un colpo bloccato.
2. ADR: lingue (D12, D13), blocco (D1), rarità e affissi (D2, D3), nomi composti per lingua
   (D4), seme e PRNG nostro (D5, D6), loot table (D8).
3. Punto di controllo, lezioni nel piano, tabella dello stato, tag `m5`.

**Prova in build (5 ott 2026).** Mirco ha provato la build di `b9814e8` con `-seed 4711`:
stessi oggetti rigiocando, rarità, confronto, blocco e lingua a posto. Ogni tanto però un
numero di danno mostrava **0**. Il danno era un numero con la virgola (la lama 8–12 per la
Forza 25 fa 10, 11,25, 12,5, 13,75 o 15), quindi la vita dello scheletro restava
frazionaria: se all'ultimo colpo gli restava 0,25, il numero mostrava il danno assorbito
arrotondato, cioè 0. Ora `RollDamage` arrotonda il danno come già faceva il pannello del
personaggio, e il risultato è sempre un intero, almeno 1. Così la vita resta intera e
l'ultimo colpo mostra quanto toglie davvero. Test: 98 EditMode (danno sempre intero con la
Forza, almeno 1) e 89 PlayMode verdi.

**Com'è andata (5 ott 2026).** Test verdi anche in CI sul commit della correzione. GIF del
README (`docs/media/m5_loot.gif`, 800 × 450, 2,1 MB in LFS) registrata da un test usa e
getta nel livello 1: due scheletri, uno magico e uno raro, una parata, poi nell'inventario
il tooltip di una spada magica e di un pugnale raro accanto alla spada equipaggiata. La
palette della GIF tiene da parte i colori delle rarità: scelta solo per frequenza, perdeva
blu e oro, cioè proprio quello che la GIF deve mostrare. ADR-025…031 in `DECISIONS.md`
(ADR-020 rimanda al danno intero). **Punto di controllo:** dalla chiusura della M2 (3 ott)
alla chiusura della M5 (5 ott) sono passati 2 giorni di calendario contro le circa 7
settimane delle 42 h stimate per M2.5–M5: rapporto 0,04, nessun taglio. Lezioni nel piano
(v2.13), tabella dello stato aggiornata. Tag `m5` pushato il 5 ottobre 2026: build del tag
verde (test e Windows).

---

## Trappole note

1. **`string.GetHashCode` cambia tra runtime e tra avvii** in .NET moderno: per mescolare i
   semi serve una funzione nostra.
2. **`System.Random` con seme non promette lo stesso algoritmo tra versioni di .NET**, e la
   build Web passa da IL2CPP: il loot usa un PRNG scritto da noi.
3. **Un tiro in più cambia tutti gli oggetti dopo:** aggiungere un affisso o un passaggio al
   generatore cambia il risultato di ogni seme. Gli oggetti salvati tengono i valori (D6); i
   test "stesso seme, stesso oggetto" confrontano due generazioni, non un oggetto scritto a
   mano.
4. **Un'unica sorgente per combattimento e loot** renderebbe il loot dipendente dai colpi
   mancati: sono due sorgenti separate.
5. **Il requisito di Forza e gli affissi:** togliere uno scudo "della Forza" può lasciare
   un'arma equipaggiata senza Forza sufficiente. In questa milestone l'arma resta in mano e
   il suo tooltip mostra il requisito in rosso; con anelli e amuleti (M8) si decide se
   smette di funzionare, come in Diablo.
6. **La vita massima e la sfera:** `HealthModel.Max` oggi è in sola lettura e la sfera la
   legge una volta; cambiarla senza evento lascia la sfera sbagliata.
7. **Il blocco interrompe un colpo a metà:** il colpo programmato con il ritardo dell'arma
   va annullato, altrimenti il danno arriva dopo l'animazione del blocco.
8. **Accordi e apostrofi:** "della Abilità" si scrive "dell'Abilità". I suffissi si scrivono
   interi nella tabella, non composti da "della" + nome.
9. **Le icone hanno ognuna il suo verso** (lezione della M4): ogni scudo nuovo va
   fotografato e controllato.
10. **Le probabilità basse si verificano male a occhio:** il 7% dei rari, con il 70% di drop
    e otto scheletri per partita, vuol dire un raro ogni due o tre partite. La verifica vera è
    il test sulle 100.000 estrazioni; in build si guarda che compaiano tutte e tre le rarità.
11. **Testi più lunghi in un'altra lingua:** l'italiano è più lungo dell'inglese, e lingue
    future come il tedesco lo sono ancora di più. Le etichette dei pannelli vanno provate in
    tutte e due le lingue, con lo spazio per il 30% in più.
12. **Il font copre solo l'alfabeto latino:** una lingua futura con un altro alfabeto
    (cirillico, greco, cinese) vorrà un font in più come *fallback* di TextMesh Pro, con la
    sua licenza (ADR-004). Va scritto nell'ADR delle lingue.
13. **Il CSV e le virgole:** le stringhe con virgole vanno tra virgolette, e le virgolette
    dentro una stringa raddoppiate. Il lettore va provato proprio su questi casi; il file si
    salva in UTF-8, altrimenti gli accenti si rompono.
14. **Una stringa composta a ogni frame alloca:** le stringhe si chiedono quando cambia
    qualcosa (CONVENTIONS), e `Get` senza argomenti restituisce la stringa della tabella,
    senza copie.

---

## Test

| Classe | Cosa verifica |
|---|---|
| `StringTableTests` (EditMode) | Lettura del CSV con virgole, virgolette e accenti; ricaduta sull'inglese; `#chiave` |
| `LocalizationCoverageTests` (EditMode) | Ogni chiave usata da scene, codice e definizioni esiste; ogni lingua ha tutte le righe |
| `ItemNamerTests` (EditMode) | Schema inglese e italiano, accordo di genere, forma di riserva |
| `ItemGeneratorTests` (EditMode) | Stesso seme stesso oggetto, livello minimo, gruppi, limiti di prefissi e suffissi per rarità |
| `RarityTableTests` (EditMode) | 100.000 estrazioni entro l'1% dalle probabilità |
| `SeedTests` (EditMode) | SplitMix64 con valori noti, semi dei nemici diversi e stabili |
| `ItemInstanceTests` (EditMode) | JSON e ritorno con affissi e valori |
| `StatSheetTests`, `HealthModelTests` (EditMode) | A colpire e vita, vita massima che cambia con la regola di D10 |
| `LanguageSwitchTests` (PlayMode) | `F9`, ridisegno dei pannelli, preferenza salvata |
| `ShieldBlockTests` (PlayMode) | Blocco con lo scudo, mai senza, attacco interrotto, "Blocked" |
| `LootTableTests` (PlayMode) | Stesso seme stessi drop in ordine qualsiasi, livello dell'oggetto dalla profondità |
| `InventoryUITests` (PlayMode) | Tooltip con affissi e confronto con l'equipaggiato, dentro lo schermo |

---

## Checklist di chiusura

- [x] Decisioni D1–D13 confermate
- [x] Lingue: inglese di default, italiano con `-lang it` e `F9`, testi di oggi tradotti
- [x] Blocco con lo scudo, con animazione e suono
- [x] Affissi, rarità e generatore riproducibile
- [x] Statistiche nuove e vita massima variabile
- [x] Basi nuove con modelli, icone e riga in `CREDITS.md`
- [x] Loot table, profondità e seme da riga di comando
- [x] Nomi composti per lingua, colori, tooltip con confronto
- [x] Scenario della Definition of Done provato in build
- [x] Test verdi in CI
- [x] Punto di controllo misurato
- [x] GIF, ADR, lezioni nel piano, tag `m5`
- [x] Scheda della M6 scritta prima di cominciarla
