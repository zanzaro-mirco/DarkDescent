# M5 — "Loot casuale"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI** avviata con
`-seed 4711`, il primo scheletro del livello 1 lascia sempre lo stesso oggetto: stesso nome,
stessi affissi, in due avvii diversi. Senza seme, in poche partite compaiono oggetti
normali, magici e rari, ognuno con il nome e la luce del suo colore. Il tooltip di un oggetto
magico ne mostra gli affissi e lo mette accanto a quello equipaggiato. Equipaggiato, gli
affissi cambiano il pannello del personaggio; tolto, tutto torna com'era. Con uno scudo, una
parte dei colpi degli scheletri viene **bloccata**, con animazione e suono. Test verdi in CI.

**Tempo stimato:** 6–9 h (piano v2.11), più circa 1 h per il blocco. **Prerequisito:** M4
chiusa (tag `m4`).

**Come si lavora:** come alla M4, il codice e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. A Mirco restano le decisioni qui sotto, il via ai download, le
prove in Play Mode e in build e la revisione degli ADR.

**Punto di controllo** (piano § 1.3) alla chiusura: settimane di calendario dalla chiusura
della M2 (3 ott 2026) contro le 39 h di M2.5–M5 a 6 h a settimana.

---

## Stato verificato il 4 ottobre 2026

| Cosa | Stato |
|---|---|
| Oggetti | Tre definizioni con ID stabile: Spada corta (6–9), Lama dello scheletro (8–12, Forza 25), Scudo con stemma (Armatura 5). `ItemInstance` porta solo l'ID della definizione |
| Drop | Ogni scheletro lascia sempre la sua lama (`LootDrop` con un oggetto fisso); lo scudo è messo dalla mappa del livello 1 |
| Statistiche | `StatSheet` con Forza, Destrezza, Magia, Vitalità e Armatura, modificatori fissi e percentuali per sorgente (ADR-020). `HealthModel.Max` è fissa dalla nascita (trappola 8 della M4) |
| Casualità | `IRandomSource` iniettata dal `CompositionRoot` per il combattimento (ADR-021); `SystemRandomSource` usa `System.Random` |
| Scudo | Armatura 5: lo scheletro passa dall'80% al 75% di colpi a segno, e non si sente. Mirco ha chiesto il blocco per questa milestone |
| Livelli | Due, costruiti da mappe di testo; `LevelContext` non sa a che profondità si trova |
| Modelli | Nel progetto `sword_1handed`, `shield_badge`, la lama dello scheletro. Nello zip di *Adventurers* 2.0 (in `Downloads`) ci sono anche `dagger`, `axe_1handed`, `sword_2handed`, `shield_round`, `shield_square`, `shield_spikes` |
| Animazioni | Nel `Rig_Medium_CombatMelee` già importato ci sono `Melee_Block`, `Melee_Blocking` e `Melee_Block_Hit` |
| Suoni | Importati solo `impactPunch`, `impactSoft` e `knifeSlice` di Kenney. Il pacchetto *Impact Sounds* ha anche `impactMetal`, ma lo zip non è più in `Downloads` |
| Test | 62 EditMode e 77 PlayMode verdi |

---

## Decisioni da prendere prima di cominciare

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Blocco** (richiesto da Mirco) | Solo con uno scudo. Probabilità = blocco dello scudo + Destrezza / 2, al massimo 75%. Scudo con stemma 10, quindi **20%** per il cavaliere. Si tira **dopo** il colpo a segno: il colpo bloccato non fa danno, sopra il cavaliere compare "Bloccato" e parte `Melee_Block_Hit` con un suono metallico. Il blocco interrompe il colpo che il cavaliere stava dando, come in Diablo 1. Gli scheletri non bloccano | Con lo scudo il cavaliere subisce il 60% dei colpi invece dell'80%: si sente. La Destrezza ottiene un secondo uso, come dice il § 2. L'interruzione è il prezzo del blocco in Diablo e lo rende una scelta, non un bonus gratis |
| D2 | **Rarità** | Tre. **Normale** (bianco, nessun affisso). **Magico** (blu, 1–2 affissi: al più un prefisso e un suffisso). **Raro** (giallo, 3–4 affissi, al più due prefissi e due suffissi). Al drop: **65% / 28% / 7%**. Gli unici restano fuori | Le probabilità sono generose perché nei due livelli fatti a mano ci sono otto scheletri; si ritarano alla M10, con il gioco completo. Gli unici vogliono oggetti scritti a mano uno per uno, che oggi non ci sono |
| D3 | **Affissi** | Undici, ciascuno con un intervallo di valori interi, un livello minimo e i tipi di oggetto ammessi (tabella sotto). Il danno e l'Armatura in percentuale sono **dell'oggetto** e cambiano i numeri dell'arma o dello scudo; gli altri sono **del personaggio** e passano dallo `StatSheet` | Come in Diablo: "+40% danno" cambia il danno della spada, non quello dei pugni. Valori interi: niente virgole nel tooltip e nessun problema di cultura (lezione della M4) |
| D4 | **Nomi in italiano** | *Base + prefisso + suffisso*: "Spada corta Feroce del Grifone". Le basi hanno un **genere**, i prefissi due forme ("Affilato/Affilata"), i suffissi una. Il raro si chiama come il magico con il primo prefisso e il primo suffisso, in giallo, e il tooltip mostra tutti gli affissi | In italiano l'aggettivo si accorda: "Ascia Robusto" si vedrebbe subito. I nomi casuali alla Diablo 2 ("Morso Sinistro") richiedono un vocabolario apposta |
| D5 | **Seme** | Un seme di partita: da riga di comando (`-seed 4711`), altrimenti casuale e scritto nel log. Ogni nemico riceve un seme suo, ricavato da seme di partita, livello e posizione nella mappa, quindi lo stesso nemico lascia lo stesso oggetto **in qualunque ordine** si uccidano. Il generatore usa un **PRNG nostro** (SplitMix64) dietro `IRandomSource`, separato da quello del combattimento | Con una sorgente sola, un colpo mancato in più cambierebbe il loot. `System.Random` non promette lo stesso algoritmo tra versioni di .NET e `string.GetHashCode` cambia tra runtime: il seme deve dare lo stesso oggetto anche in build Web e dopo un aggiornamento |
| D6 | **Cosa salva un oggetto** | `ItemInstance` aggiunge rarità, livello dell'oggetto, seme e la lista degli affissi con il **valore tirato** (ID dell'affisso + numero) | Il seme basta a rigenerare l'oggetto, ma se alla M10 si ritarano gli intervalli, gli oggetti già salvati cambierebbero. Con i valori salvati restano quelli trovati; il seme serve per "rigenera il drop 4711" |
| D7 | **Basi nuove** | Otto oggetti in tutto. Armi: **Pugnale** (`dagger`, 1×2, 3–6), **Spada corta** (1×3), **Ascia** (`axe_1handed`, 2×3, 7–11, Forza 30), **Lama dello scheletro** (1×3). Scudi: **con stemma** (2×2, Armatura 5, blocco 10), **tondo** (`shield_round`, 2×2, 4, 15), **quadrato** (`shield_square`, 2×3, 8, 10), **chiodato** (`shield_spikes`, 2×2, 6, 5). Niente armi a due mani | Scelte diverse sul serio: l'ascia chiede Forza, lo scudo tondo blocca e quello quadrato para. Le due mani vogliono una regola sullo slot dello scudo: si valutano alla M8 o restano nell'`ICEBOX.md` |
| D8 | **Loot table e livello dell'oggetto** | Uno ScriptableObject `LootTable` per nemico: probabilità di lasciare qualcosa (scheletro **70%**) e basi pesate. Livello dell'oggetto = profondità del livello (1 o 2), scritta nella mappa con `@depth`. Lo scudo del livello 1 resta messo dalla mappa, normale. Gli oggetti a terra prendono il colore della rarità nel nome e nella luce | Il drop fisso della M4 serviva a tenere tutto deterministico; ora il determinismo lo dà il seme. La luce colorata a terra è il segnale più forte del genere: un bagliore giallo si riconosce da lontano |
| D9 | **Confronto nel tooltip** | Passando su un oggetto della griglia che va in uno slot occupato, accanto al suo tooltip ne compare un secondo con l'oggetto equipaggiato, intitolato "Equipaggiato" | Due riquadri si leggono senza fare conti, e riusano `ItemTooltip`. Le differenze in verde e rosso su ogni riga diventano illeggibili con 3–4 affissi |
| D10 | **Vita massima che cambia** (trappola 8 della M4) | Quando la vita massima sale, la vita attuale sale della stessa quantità; quando scende, scende della stessa quantità ma mai sotto 1 | È il comportamento di Diablo 1: togliere un anello della Vitalità non uccide mai, e indossarlo non è una cura gratis |
| D11 | **Download** | Lo zip di Kenney *Impact Sounds* (CC0, da `kenney.nl`, circa 1 MB: misura esatta prima di scaricarlo) per `impactMetal`. Lo riscarica Mirco, o lo scarico io con il suo via | Il blocco vuole un suono metallico; quelli importati sono pugni e colpi sordi |

### Affissi proposti (D3)

| Tipo | Nome (m/f) | Effetto | Su | Livello minimo |
|---|---|---|---|---|
| Prefisso | Affilato / Affilata | +20–40% danno dell'arma | armi | 1 |
| Prefisso | Feroce | +41–65% danno dell'arma | armi | 2 |
| Prefisso | Preciso / Precisa | +5–10 a colpire | armi | 1 |
| Prefisso | Robusto / Robusta | +2–4 Armatura | scudi | 1 |
| Prefisso | Massiccio / Massiccia | +25–50% Armatura dello scudo | scudi | 2 |
| Suffisso | della Forza | +2–5 Forza | tutti | 1 |
| Suffisso | della Destrezza | +2–5 Destrezza | tutti | 1 |
| Suffisso | della Vitalità | +2–5 Vitalità | tutti | 1 |
| Suffisso | dell'Orso | +5–10 vita | tutti | 1 |
| Suffisso | del Grifone | +11–20 vita | tutti | 2 |
| Suffisso | della Parata | +5–10% blocco | scudi | 1 |

Servono due statistiche nuove del personaggio: **a colpire** (si somma alla probabilità di
colpire) e **vita** (si somma alla vita massima). Il blocco dello scudo è dell'oggetto.

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 5.1 | Blocco con lo scudo | 1 |
| 5.2 | Affissi, rarità e generatore | 1,5–2 |
| 5.3 | Statistiche nuove e vita massima variabile | 0,5–1 |
| 5.4 | Basi nuove: modelli, definizioni e icone | 1 |
| 5.5 | Loot table, profondità e seme | 1–1,5 |
| 5.6 | Nomi, colori e tooltip con confronto | 1–1,5 |
| 5.7 | Chiusura: GIF, ADR, punto di controllo, tag `m5` | 0,5 |

---

## Architettura

```
Core
├─ CompositionRoot      seme di partita (-seed), sorgente del combattimento e del loot
├─ Player
│  ├─ CharacterStats    + a colpire, + vita
│  ├─ PlayerInventory   oggetti con affissi: modificatori per sorgente come alla M4
│  └─ ShieldBlock       tiro del blocco, animazione, suono
└─ HUD
   └─ ItemTooltip × 2   l'oggetto sotto il cursore e quello equipaggiato

Logica pura (DarkDescent.Items / Loot)
├─ AffixDefinition      ScriptableObject: ID, nomi m/f, effetto, intervallo, livello, tipi
├─ ItemGenerator        (base, livello, seme) → ItemInstance, sempre lo stesso
├─ RarityTable          probabilità delle rarità, estrazione pesata
├─ LootTable            ScriptableObject per nemico: probabilità di drop, basi pesate
├─ SplitMix64Source     IRandomSource riproducibile su ogni piattaforma
└─ SeedMixer            seme di partita + livello + posizione → seme del nemico

Livelli
└─ Skeleton             LootDrop legge la LootTable e il seme del nemico
```

### Contratti

- **`ItemGenerator.Generate(ItemDefinition base, int itemLevel, ulong seed)`**: logica pura,
  stesso ingresso, stesso oggetto. Sceglie la rarità, poi gli affissi ammessi (tipo e livello)
  senza ripetere lo stesso affisso, poi i valori.
- **`ItemInstance`**: in più `Rarity`, `ItemLevel`, `Seed`, `Affixes` (lista di ID e valori);
  resta serializzabile con `JsonUtility`. Un costruttore senza affissi per gli oggetti normali.
- **`Equipment`**: per ogni affisso del personaggio un modificatore con l'oggetto come
  sorgente, come l'Armatura alla M4. Gli affissi dell'oggetto cambiano danno, Armatura e
  blocco letti da `MeleeAttack`, dallo scudo e dal tooltip.
- **`HealthModel.SetMax(float)`**: cambia la vita massima con la regola di D10 e manda
  `Changed`.
- **`ShieldBlock`**: `Health` gli chiede se il colpo è bloccato prima di applicare il danno;
  usa la sorgente del combattimento, così i test restano sulla sorgente fissa.

---

## Passo 5.1 — Blocco con lo scudo

1. Probabilità di blocco: blocco dello scudo + Destrezza / 2, al massimo 75 (logica pura in
   `CombatFormulas`).
2. `ShieldBlock` sul cavaliere: con uno scudo equipaggiato, dopo un colpo a segno tira il
   blocco. Bloccato: niente danno, evento `Blocked`, "Bloccato" sopra il cavaliere,
   `Melee_Block_Hit`, suono metallico (`impactMetal`), colpo in corso interrotto.
3. Il pannello del personaggio mostra la probabilità di blocco.

**Test:** probabilità limitata a 75; senza scudo mai un blocco; con la sorgente fissa il colpo
bloccato non toglie vita e interrompe l'attacco del cavaliere. I test che contano i colpi
ricevuti restano verdi perché il cavaliere parte senza scudo: con la sorgente fissa a 0,0
ogni tiro di blocco riuscirebbe.

## Passo 5.2 — Affissi, rarità e generatore

1. `AffixDefinition`, `Rarity`, `RarityTable`, `ItemGenerator`, `SplitMix64Source`, tutto
   logica pura con ScriptableObject immutabili.
2. Gli undici affissi come asset in `Data/Affixes/`, con ID stabile come gli oggetti e un
   loro database.
3. `ItemInstance` con rarità, livello, seme e affissi, e il ritorno da JSON.

**Test:** stesso seme, stesso oggetto, sempre; un oggetto di livello 1 non ha mai "Feroce" né
"del Grifone"; mai due volte lo stesso affisso; su 10.000 estrazioni le rarità restano entro
l'1% da 65/28/7; il magico ha al più un prefisso e un suffisso; JSON e ritorno.

## Passo 5.3 — Statistiche nuove e vita massima variabile

1. `StatType` aggiunge *a colpire* e *vita*; `CombatFormulas.HitChance` somma il bonus.
2. `HealthModel.SetMax` con la regola di D10; `Health` ricalcola il massimo quando lo
   `StatSheet` cambia.
3. La sfera della vita e il pannello del personaggio si aggiornano dagli eventi.

**Test:** "+10 vita" alza massimo e attuale di 10; tolto, li abbassa ma mai sotto 1; "della
Vitalità" passa dalla formula 50 + 2 × Vitalità; equipaggia e togli un oggetto con tre
affissi riporta ogni statistica a prima.

## Passo 5.4 — Basi nuove: modelli, definizioni e icone

1. Dallo zip di *Adventurers*: `dagger`, `axe_1handed`, `shield_round`, `shield_square`,
   `shield_spikes` in `Art/Models/KayKit/`, riga in `CREDITS.md`.
2. Definizioni con genere, livello minimo, blocco per gli scudi; posizione in mano provata
   con le foto, come lo scudo della M4.
3. Icone con *Rigenera le icone*, database aggiornato.

**Test:** il test della M4 sugli ID unici copre i nuovi oggetti; ogni base ha icona, modello,
genere; ogni scudo ha un blocco tra 0 e 75.

## Passo 5.5 — Loot table, profondità e seme

1. `@depth` nelle mappe, letta da `LevelContext`.
2. `LootTable` per lo scheletro; `LootDrop` la usa con il seme del nemico (`SeedMixer`).
3. `-seed N` letto all'avvio dal `CompositionRoot`, altrimenti un seme casuale; il seme va nel
   log.
4. `GroundItem`: nome e luce del colore della rarità.

**Test:** stesso seme di partita, gli stessi scheletri lasciano gli stessi oggetti anche
uccisi in ordine inverso; seme diverso, oggetti diversi; nel livello 2 gli oggetti hanno
livello 2; la luce dell'oggetto raro è gialla.

## Passo 5.6 — Nomi, colori e tooltip con confronto

1. Nome dall'accordo di genere; colore della rarità nel tooltip, nell'etichetta a terra e
   nel bordo della cella.
2. Tooltip con le righe degli affissi, il danno e l'Armatura già modificati, la probabilità
   di blocco degli scudi.
3. Secondo tooltip "Equipaggiato" accanto al primo, dentro lo schermo tutti e due.

**Test:** "Ascia Robusta" e "Scudo Robusto"; il tooltip di "Spada corta Affilata" (+40%)
mostra 8–12 invece di 6–9; passando su uno scudo con lo scudo equipaggiato compaiono due
riquadri che non si sovrappongono e non escono dallo schermo.

## Passo 5.7 — Chiusura

1. GIF del README: una serie di uccisioni con oggetti di tre colori, un raro raccolto e
   confrontato con l'arma in mano, un colpo bloccato.
2. ADR: blocco (D1), rarità e affissi (D2, D3), seme e PRNG nostro (D5, D6), loot table (D8).
3. Punto di controllo, lezioni nel piano, tabella dello stato, tag `m5`.

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
   interi, non composti da "della" + nome.
9. **Le icone hanno ognuna il suo verso** (lezione della M4): ogni scudo nuovo va
   fotografato e controllato.
10. **Le probabilità basse si verificano male a occhio:** il 7% dei rari con otto scheletri
    per partita vuol dire un raro ogni due partite circa. La verifica vera è il test sulle
    10.000 estrazioni; in build si guarda che compaiano.

---

## Test

| Classe | Cosa verifica |
|---|---|
| `ItemGeneratorTests` (EditMode) | Stesso seme stesso oggetto, livello minimo degli affissi, niente affissi ripetuti, limiti di prefissi e suffissi per rarità |
| `RarityTableTests` (EditMode) | 10.000 estrazioni entro l'1% dalle probabilità |
| `SeedTests` (EditMode) | SplitMix64 con valori noti, semi dei nemici diversi e stabili |
| `AffixNameTests` (EditMode) | Accordo di genere, suffissi, colore della rarità |
| `ItemInstanceTests` (EditMode) | JSON e ritorno con affissi e valori |
| `StatSheetTests`, `HealthModelTests` (EditMode) | A colpire e vita, vita massima che cambia con la regola di D10 |
| `ShieldBlockTests` (PlayMode) | Blocco con lo scudo, mai senza, attacco interrotto, "Bloccato" |
| `LootTableTests` (PlayMode) | Stesso seme stessi drop in ordine qualsiasi, livello dell'oggetto dalla profondità |
| `InventoryUITests` (PlayMode) | Tooltip con affissi e confronto con l'equipaggiato, dentro lo schermo |

---

## Checklist di chiusura

- [ ] Decisioni D1–D11 confermate
- [ ] Blocco con lo scudo, con animazione e suono
- [ ] Affissi, rarità e generatore riproducibile
- [ ] Statistiche nuove e vita massima variabile
- [ ] Basi nuove con modelli, icone e riga in `CREDITS.md`
- [ ] Loot table, profondità e seme da riga di comando
- [ ] Nomi accordati, colori, tooltip con confronto
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] Punto di controllo misurato
- [ ] GIF, ADR, lezioni nel piano, tag `m5`
- [ ] Scheda della M6 scritta prima di cominciarla
