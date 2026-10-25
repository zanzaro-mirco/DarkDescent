# M16 — "Tesori con un nome"

**Cosa deve succedere (Definition of Done):** sopra il raro ci sono due rarità nuove, come in
Diablo 2. Gli **unici** hanno il nome in oro e sono oggetti scritti a mano, con un nome loro ("La
Mannaia del Re") e proprietà fisse. I **pezzi di set** hanno il nome in verde: ognuno ha le sue
proprietà, e indossando **due, tre o tutti** i pezzi dello stesso set si accendono bonus in più. Il
tooltip di un pezzo elenca il set, i pezzi indossati e i bonus accesi. Unici e set cadono **dall'Ombra
in su**, più spesso dai campioni, dagli unici, dai forzieri e dal re; al Crepuscolo solo il tesoro
del re può lasciarne uno. Con loro arrivano **quattro proprietà nuove** che gli oggetti di oggi non
hanno: velocità d'attacco, rubavita, danno ridotto e tutti gli attributi. Quanti ne cadono lo
**misura** uno strumento, come il loot della M13. Esce con il tag `v1.4.0`.

**Tempo stimato:** 10–14 h (D14).
**Prerequisito:** M15 chiusa (tag `v1.3.1`, 25 ott 2026).

**Come si lavora:** come alle M13 e M14. Il codice, i dati e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. A Mirco restano le decisioni qui sotto, la tabella degli unici e dei set
(nomi e proprietà, passo 16.1), l'ascolto del suono nuovo e la prova in build.

---

## Stato verificato il 25 ottobre 2026

| Cosa | Stato |
|---|---|
| Rarità | `Rarity` ha tre valori, **Normal, Magic, Rare**, salvati come numero: se ne aggiungono in fondo senza toccare i salvataggi. Una quarta leggendaria era stata chiesta e tolta alla M5 (ADR-027). Colori in `RarityColors` (testo, cella dell'inventario, luce a terra): bianco, blu `#7F9CFF`, giallo `#FFD84A` |
| Basi | **19 da indossare**: 4 armi (spada corta, pugnale, ascia, lama dello scheletro), 4 scudi, 2 elmi (elmo da cavaliere, copricapo d'orso), 3 armature (cuoio, corazza, corazza nera), 2 guanti, 2 stivali, anello, amuleto. Più pozioni e pergamene |
| Affissi | 12 gruppi da 6 gradi (M14), 72 asset. Gli effetti (`AffixEffect`) sono 12: tre dell'oggetto (% danno, % Armatura, blocco) e nove del personaggio (Armatura, a colpire, Forza, Destrezza, Vitalità, vita, critico, Magia, mana) |
| Istanza | `ItemInstance` salva id della base, rarità, livello, seme e la lista degli affissi (id e valore). Il nome non si salva: lo compone `ItemNamer` |
| Statistiche | `StatSheet` con modificatori per sorgente; `Equipment` mette quelli dell'oggetto con l'oggetto come sorgente e li toglie tutti insieme. `StatType` ha già `DamageBonus` ed `ExperienceBonus` (i santuari della M13), che nessun oggetto usa |
| Generazione | `ItemGenerator`: rarità dalla `RarityTable`, poi gruppi e gradi. `LootRoller` fa cadere gli oggetti con un seme per nemico, cassa e barile (`SeedMixer`), e i tiri in più di campioni, unici, forzieri e tesoro del re (`LootBonus`, ADR-092) |
| Difficoltà | `DifficultyTier` ha, tra gli altri, i pesi delle tre rarità (65/28/7 al Crepuscolo, 35/42/23 all'Abisso VI). Livello degli oggetti = livello della zona, da 1 a 55 |
| Prezzi | `ItemPricing`: valore della base × rarità (1, 3, 6) × (1 + livello / 10). Il mercante vende dalla `MerchantTable` |
| Salvataggio | **Formato 6** (difficoltà, ADR-101…105). Le fixture congelate arrivano a `save_v5.json` |
| Censimento | `Editor/LootCensus.cs` conta gli oggetti per livello e per rarità su 200 livelli per profondità (M13); la ronda notturna lo ripete su semi nuovi (M15) |
| Test | 437 EditMode verdi in locale; PlayMode verde in CI (run 40207035287) |

---

## Decisioni

Confermate da Mirco il 25 ottobre 2026, tutte come proposte ("confermate tutte, procedi con il
16.1"). Le D2, D3, D7 e D10 riprendono quello che Mirco ha chiesto il 12 ottobre 2026 (piano
v2.32) e la D16 della M14 (unici e set dall'Ombra in su); qui sono messe in numeri.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M16 "Tesori con un nome"**, tag **`v1.4.0`**. Dentro: le due rarità, una dozzina di unici, due set, le quattro proprietà nuove, il loot, il tooltip, il salvataggio e la misura | Cambia il gioco e aggiunge cose da cercare: una versione minore, come la M14 |
| D2 | **Due rarità nuove** | `Rarity` prende **Unique** e **Set**, in fondo. **Unico in oro scuro** (`#C9A55C`, come in Diablo 2, distinto dal giallo dei rari), **set in verde** (`#4FD45A`). Lo stesso colore per il nome, la cella dell'inventario e la luce a terra, più forte di quella dei rari | Due gradi sopra il raro, come nel piano. Il colore dice cos'è prima di leggere il nome |
| D3 | **Unici** | **12 unici**, uno per base scelta: le 4 armi, 2 scudi, i 2 elmi, 2 armature, l'anello e l'amuleto. Ognuno ha un **nome suo** nelle due lingue, la **base** da cui nasce (il modello e l'icona restano quelli della base), un **livello minimo** e **3–5 proprietà**: le stesse degli affissi, più forti di quelle di un raro dello stesso livello, e quasi sempre una delle proprietà nuove (D5). Nomi legati al gioco: il Re delle ossa, Morvak il Carceriere, Grumm Spaccaossa, la cripta, la luce che si spegne. Esempi: *La Mannaia del Re* (ascia), *Zanna di Morvak* (pugnale), *Il Guscio di Grumm* (scudo), *L'Ultima Candela* (amuleto). La tabella completa la approva Mirco al passo 16.1 | Una dozzina è quello che il piano stimava, ed è scrivibile a mano bene. Senza arte nuova si resta nelle ore |
| D4 | **Set** | **Due set da quattro pezzi**. *L'armatura del Custode* (elmo da cavaliere, corazza, guanti di ferro, stivali di ferro): difesa, vita, blocco. *Le spoglie del Cacciatore* (copricapo d'orso, armatura di cuoio, guanti di cuoio, stivali di cuoio): Destrezza, critico, velocità d'attacco. Ogni pezzo ha 2–3 proprietà sue; i bonus si accendono con **2, 3 e 4 pezzi** indossati: per esempio il Custode +vita con due, +Armatura con tre, danno ridotto con tutti. Un terzo set di gioielli (anello, anello, amuleto) resta **facoltativo**, dopo la misura | Due set sulle armature si leggono subito e usano le basi che ci sono. Il terzo si aggiunge solo se le ore lo permettono (D14) |
| D5 | **Quattro proprietà nuove** | Solo su unici e set, per ora: **velocità d'attacco** (% in più, accorcia l'intervallo dell'arma), **rubavita** (% del danno dei colpi inflitti che torna in vita), **danno ridotto** (punti tolti a ogni colpo preso, mai sotto 1) e **tutti gli attributi** (+ a Forza, Destrezza, Magia e Vitalità). In più usano **+% danno** ed **+% esperienza**, che le statistiche hanno già dai santuari. Ognuna è un `AffixEffect` nuovo e, dove serve, una `StatType` nuova in fondo | Un unico deve fare qualcosa che un raro non fa. Le statistiche dei santuari ci sono già, le quattro nuove toccano un punto solo ciascuna (attacco, colpo inflitto, colpo preso, attributi) |
| D6 | **Valori che crescono con il livello** | Ogni proprietà di un unico o di un pezzo di set ha un **valore al suo livello minimo** e uno **al livello 55**; l'oggetto prende il valore del suo livello, in linea retta, con un tiro di **±10%**. Così *La Mannaia del Re* trovata all'Ombra è buona all'Ombra, e ritrovata all'Abisso è buona all'Abisso. Il valore uscito si salva, come quello degli affissi | Con le nove difficoltà, un unico a valori fissi diventerebbe inutile due gradini dopo. Il gioco non finisce (§ 2): ritrovare un unico più forte è un motivo per scendere ancora |
| D7 | **Quando cadono** | Un **tiro a parte**, con un **dominio suo** del seme (`SeedMixer.ForNamed`), dopo l'oggetto di sempre: se riesce, l'oggetto diventa l'unico o un pezzo di set della **sua base**, se ne esiste uno al suo livello; altrimenti resta com'è. Probabilità per oggetto, nella `DifficultyTier`: **Crepuscolo 0%**, **Ombra 1% unico e 1% set**, **Tenebra 1,5% e 1,5%**, **Abisso I 2% e 2%**, poi +0,2% a gradino fino a **3% e 3%** all'Abisso VI. Campioni, unici e forzieri moltiplicano la probabilità per **3**, il tesoro del re per **10**. **Il tesoro del re al Crepuscolo** ha un tiro suo del **25%** di lasciare un unico o un pezzo di set, così chi gioca la prima volta ne vede uno. Il mercante **non li vende** mai. I numeri escono dalla misura (D11) | Con il tiro a parte il loot dei semi già provati non cambia: un oggetto può solo diventare un unico, mai cambiare base o sparire. Con circa 40 oggetti a discesa, all'Ombra cade in media un unico o un pezzo di set ogni discesa e un po', di più all'Abisso |
| D8 | **Il nome e il tooltip** | Il nome dell'unico o del pezzo al posto del nome composto, nel suo colore, con sotto la **base** in piccolo ("Ascia"). Le proprietà in blu come gli affissi, quelle nuove con la loro riga. Per un pezzo di set, sotto le proprietà: il **nome del set**, i **pezzi** (indossati in bianco, gli altri in grigio) e i **bonus** (accesi in verde, gli altri in grigio, con "(2 pezzi)", "(3 pezzi)", "(set completo)"). Il confronto con l'oggetto indossato funziona come oggi | Come in Diablo 2 e 3, e come nel piano (v2.32) |
| D9 | **Bonus dei set nel codice** | Un `SetBonuses` in `Scripts/Core/Items`, accanto all'`Equipment`: ascolta il suo `Changed`, conta i pezzi di ogni set indossati e mette i modificatori dei bonus accesi con il **set come sorgente**, togliendo tutti quelli di prima. Due pezzi uguali (lo stesso unico non può esserci due volte, ma i due anelli sì) contano una volta | Lo stesso meccanismo degli affissi, senza controlli a ogni frame. Ricalcolare tutto a ogni cambio è semplice e costa poco: gli slot sono nove |
| D10 | **Le definizioni** | Asset, come le basi e gli affissi: **`UniqueDefinition`** (id, chiave del nome, base, livello minimo, proprietà con effetto e valori ai due livelli) e **`SetDefinition`** (id, chiave del nome, i pezzi, i bonus con il numero di pezzi). Un pezzo è una `UniqueDefinition` con il suo set. Un **`NamedItemDatabase`** li trova per id, come `ItemDatabase`. Gli asset nascono da uno script Python che legge la tabella approvata, come i gradi della M14 | Una definizione sola per unici e pezzi tiene corto il codice: cambia solo il colore e il set |
| D11 | **Si misura** | Il `LootCensus` conta anche unici e set, **per difficoltà**: quanti a discesa e quanti dai nemici speciali, dai forzieri e dal re. Obiettivo: all'**Ombra 1–2 a discesa**, all'**Abisso VI 3–5**. Un test EditMode tiene le fasce, e la ronda notturna le controlla su semi nuovi. Le proprietà si confrontano con un raro dello stesso livello: un unico deve dare **almeno il 30% in più**, misurato con il `PowerCensus` della M14 (colpi per uccidere e colpi presi) su un cavaliere che lo indossa al posto del raro | Senza misura le probabilità della D7 sono indovinate. Il confronto con il raro tiene gli unici sopra senza renderli tutti uguali |
| D12 | **Prezzi** | Unico **×10**, set **×8** nella formula di `ItemPricing`. Il mercante li compra (un quarto del prezzo, come tutto) ma non li vende | Venderli dev'essere una scelta che costa: un unico vale molto più di un raro (×6) |
| D13 | **Salvataggio** | **Formato 7**: l'istanza aggiunge l'id dell'unico o del pezzo e i valori delle sue proprietà, in ordine. Prima si **congela una fixture vera del formato 6**. Gli oggetti vecchi non cambiano. Un unico che il database non conosce più si legge come un raro della sua base senza affissi, con un avviso nel log, come un affisso sconosciuto | Come alla M14: la fixture prima del formato nuovo. Salvare i valori e non solo il seme tiene gli oggetti trovati uguali anche se si ritarano le tabelle |
| D14 | **Stima e linee di taglio** | **10–14 h**. Scrivere gli oggetti è la parte lunga. Se servirà tagliare: prima il terzo set (già facoltativo), poi due unici, poi il rubavita. La misura (D11) non si taglia | Meglio deciderlo adesso che a metà |
| D15 | **Per provare** | Un argomento della riga di comando **`-namedchance <x>`**, che moltiplica le probabilità della D7, come `-difficulty` e `-herolevel` della M14: con `-namedchance 50` all'Ombra quasi ogni oggetto è un unico o un pezzo di set. Un suono suo quando ne cade uno, **da script** come quelli della M13, con la pagina di ascolto. I test PlayMode lo usano per far cadere un pezzo e indossare un set intero | Senza, vedere un set completo in build vorrebbe ore di gioco |
| D16 | **Fuori da questa milestone** | **Modelli e icone propri** per unici e pezzi (restano quelli della base; un colore del materiale si può valutare dopo la prova). **Basi eccezionali** delle armi e delle armature. **Le proprietà nuove sugli affissi** degli oggetti magici e rari. **Ricerca magica** (*magic find*). Un **forziere personale** all'accampamento, che con i set diventa utile: nel piano, da decidere dopo la prova | Ognuna è un sistema o un lavoro d'arte a sé. Il forziere è il primo candidato dopo questa milestone |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 16.1 | La tabella degli unici e dei set: nomi nelle due lingue, base, livello, proprietà con i valori ai due livelli, bonus dei set. La approva Mirco prima del codice (D3, D4, D6) | 1–1,5 |
| 16.2 | Core: le due rarità e i colori, le quattro proprietà nuove come effetti e statistiche, `UniqueDefinition`, `SetDefinition`, `NamedItemDatabase`, l'istanza con l'id e i valori, la generazione con i valori del livello, `SetBonuses`, i prezzi, i test (D2, D5, D6, D9, D10, D12) | 2–3 |
| 16.3 | In gioco: velocità d'attacco, rubavita, danno ridotto e tutti gli attributi nel combattimento; il tiro del loot con il dominio suo, i bonus di campioni, forzieri e re, il tesoro del re al Crepuscolo; la luce a terra e il suono; `-namedchance` (D5, D7, D15) | 2–3 |
| 16.4 | Tooltip e inventario: nome e base, proprietà, set con pezzi e bonus, confronto, celle colorate, stringhe nelle due lingue; gli asset dallo script (D8, D10) | 1,5–2 |
| 16.5 | Salvataggio: fixture del formato 6, formato 7, migrazione, oggetto sconosciuto (D13) | 1 |
| 16.6 | Misura e taratura: il censimento per difficoltà, il confronto con i rari, il test delle fasce, la ronda notturna (D11) | 1–1,5 |
| 16.7 | Build da provare (Ombra con `-difficulty 1 -namedchance 50`) e correzioni | 1–2 |
| 16.8 | Chiusura: ADR, lezioni, piano, README, `bundleVersion` 1.4.0, tag `v1.4.0` dopo i test verdi in CI | 1 |

---

## Passo 16.1 — La tabella degli unici e dei set

**Approvata da Mirco il 25 ottobre 2026** ("approvata, procedi con il 16.2"). Ogni valore è scritto come *al livello 10 → al livello 55*: l'oggetto prende quello
del suo livello in linea retta, con il tiro del ±10% (D6). Il livello minimo è **10 per tutti**, il
livello della zona della profondità 8 al Crepuscolo, così il tesoro del re al Crepuscolo (D7) può
lasciare qualunque unico o pezzo. Per confronto, un raro al livello 10 tira i gradi I–II (Forza fino
a 10, vita fino a 20, danno fino al 65%), al livello 55 i gradi V–VI (Forza fino a 45, vita fino a
110, danno fino al 190%). Le proprietà nuove crescono poco: la velocità d'attacco totale si ferma al
**+50%**, il rubavita al **10%**. I numeri si ritoccano nella misura del passo 16.6, i nomi no.

I nomi degli unici e dei pezzi sono nomi interi, senza le forme per genere degli affissi. Sotto il
nome, nel tooltip, c'è la base.

### Unici (oro)

| Base | Nome | Proprietà (liv. 10 → 55) | L'idea |
|---|---|---|---|
| Ascia | **La Mannaia del Re** (*The King's Cleaver*) | +70 → 200% danno · Forza +10 → 40 · rubavita 3 → 6% · vita +15 → 80 | L'arma del Re delle ossa: colpi lenti e pesanti che curano |
| Pugnale | **Zanna di Morvak** (*Morvak's Fang*) | velocità d'attacco +15 → 25% · critico +6 → 18% · Destrezza +8 → 35 · a colpire +12 → 40% | Il pugnale del Carceriere: tanti colpi, tanti critici |
| Spada corta | **Lama del Crepuscolo** (*Dusk Blade*) | +55 → 170% danno · a colpire +15 → 45% · tutti gli attributi +3 → 12 · mana +15 → 60 | Equilibrata, per chi usa anche la magia |
| Lama dello scheletro | **Falce della Cripta** (*Crypt Reaper*) | +60 → 180% danno · rubavita 4 → 8% · vita +15 → 70 · esperienza +5 → 15% | Si nutre dei morti: vita ed esperienza |
| Scudo chiodato | **Il Guscio di Grumm** (*Grumm's Shell*) | +70 → 160% Armatura · blocco +12 → 28% · danno ridotto 2 → 12 · Vitalità +8 → 35 | Lo scudo di Grumm Spaccaossa: il muro che non cade |
| Scudo quadrato | **Il Sigillo della Cripta** (*The Crypt Seal*) | Armatura +6 → 24 · +60 → 150% Armatura · blocco +14 → 30% · vita +15 → 80 | Il blocco più alto del gioco |
| Elmo da cavaliere | **L'Elmo del Primo Cavaliere** (*Helm of the First Knight*) | +60 → 140% Armatura · Vitalità +8 → 35 · danno ridotto 2 → 10 · a colpire +10 → 30% | Chi è sceso prima del cavaliere |
| Copricapo d'orso | **Le Fauci dell'Orso** (*The Bear's Maw*) | Forza +8 → 35 · vita +20 → 90 · danno +5 → 20% · Armatura +4 → 16 | Forza bruta |
| Corazza nera | **Il Sudario d'Ossa** (*The Bone Shroud*) | +70 → 160% Armatura · vita +20 → 100 · danno ridotto 3 → 15 · Forza +8 → 30 | La corazza più pesante, con la vita più alta |
| Armatura di cuoio | **La Pelle dell'Ombra** (*Shadowhide*) | Destrezza +8 → 35 · critico +4 → 12% · velocità d'attacco +8 → 15% · +50 → 120% Armatura | Leggera e veloce |
| Anello | **L'Anello dell'Eclissi** (*Eclipse Band*) | tutti gli attributi +4 → 15 · a colpire +10 → 35% · rubavita 2 → 5% | Un po' di tutto, su un anello |
| Amuleto | **L'Ultima Candela** (*The Last Candle*) | esperienza +5 → 15% · Magia +8 → 35 · mana +20 → 90 · vita +15 → 70 | La luce che resta: per salire di livello e per la magia |

### L'armatura del Custode (verde) — *The Warden's Armor*

Difesa: vita, Armatura, danno ridotto. Il blocco della D4 non c'è: è un numero dello scudo, non del
personaggio, e un bonus di set non ha uno scudo su cui metterlo; al suo posto il danno ridotto.

| Base | Pezzo | Proprietà (liv. 10 → 55) |
|---|---|---|
| Elmo da cavaliere | **Elmo del Custode** (*Warden's Helm*) | +50 → 120% Armatura · vita +15 → 60 |
| Corazza | **Corazza del Custode** (*Warden's Breastplate*) | +60 → 140% Armatura · Vitalità +8 → 30 · danno ridotto 1 → 6 |
| Guanti di ferro | **Guanti del Custode** (*Warden's Gauntlets*) | Forza +6 → 25 · a colpire +10 → 30% |
| Stivali di ferro | **Stivali del Custode** (*Warden's Boots*) | Armatura +4 → 18 · vita +15 → 60 |

| Pezzi indossati | Bonus (liv. 10 → 55) |
|---|---|
| 2 | vita +25 → 120 |
| 3 | Armatura +10 → 40 |
| 4 (set completo) | danno ridotto 4 → 20 · tutti gli attributi +5 → 20 |

### Le spoglie del Cacciatore (verde) — *The Hunter's Hide*

Attacco: Destrezza, critico, velocità.

| Base | Pezzo | Proprietà (liv. 10 → 55) |
|---|---|---|
| Copricapo d'orso | **Cappuccio del Cacciatore** (*Hunter's Hood*) | Destrezza +6 → 25 · critico +3 → 8% |
| Armatura di cuoio | **Giubba del Cacciatore** (*Hunter's Jerkin*) | Destrezza +8 → 30 · vita +15 → 60 |
| Guanti di cuoio | **Guanti del Cacciatore** (*Hunter's Gloves*) | velocità d'attacco +5 → 10% · a colpire +10 → 30% |
| Stivali di cuoio | **Stivali del Cacciatore** (*Hunter's Boots*) | Destrezza +6 → 25 · Armatura +3 → 12 |

| Pezzi indossati | Bonus (liv. 10 → 55) |
|---|---|
| 2 | a colpire +15 → 40% |
| 3 | critico +4 → 10% |
| 4 (set completo) | velocità d'attacco +10 → 20% · rubavita 3 → 6% |

**Il livello dei bonus.** Un bonus di set non ha un livello suo: prende quello del **pezzo più basso**
tra quelli indossati, senza tiro. Così un set completato con un pezzo vecchio dà i bonus di quel
pezzo, e cambiarlo con uno più alto li fa salire.

**Chi resta senza.** Lo scudo con stemma, lo scudo tondo e la corazza non hanno un unico: un tiro
dell'unico riuscito su queste basi lascia l'oggetto com'è (D7). La corazza ha però il pezzo del
Custode, e quindi il tiro del set.

## Passo 16.2 — Core

**Com'è andata (25 ott 2026).** `Rarity` ha in fondo **Unique** e **Set**; `RarityColors` dà testo,
cella e luce delle due rarità e `LightBoost` (×1,6 per la luce a terra); `ItemPricing` ×10 e ×8. Il
generatore non fa mai unici né set dalla tabella: chiesti da fuori, escono rari. `AffixEffect` ha sei
effetti nuovi in fondo (velocità d'attacco, rubavita, danno ridotto, tutti gli attributi, +% danno,
+% esperienza) e `StatType` tre statistiche nuove (`AttackSpeed`, `LifeSteal`, `DamageReduction`);
`ItemStats.AddCharacterModifiers` mette i modificatori di un effetto, quattro per tutti gli attributi,
e lo usano l'`Equipment` e i set. Le righe del tooltip dei sei effetti sono già nelle due lingue.

Le definizioni: `NamedProperty` (effetto, valore basso e alto, `ValueAt` in linea retta dal livello
minimo al 55), `UniqueDefinition` (id, nome, base, livello minimo, proprietà, set; la rarità viene dal
set), `SetDefinition` (pezzi e `SetBonus` per numero di pezzi) e `NamedItemDatabase` (per id, e i
candidati di una base a un livello). `ItemGenerator.GenerateNamed` tira i valori con il ±10% dal seme.
`ItemInstance` salva l'id e i valori; `Resolve` prende il database degli unici come terzo argomento:
senza resta da risolvere, con un id che non conosce più torna un raro della base con un avviso
(D13). `SetBonuses` ascolta l'`Equipment`, conta i pezzi diversi (lo stesso pezzo nei due anelli
conta una volta) e mette i bonus con il set come sorgente, al livello del pezzo più basso;
`Dispose` toglie tutto. `ItemNamer` dà il nome intero degli unici. In gioco non c'è ancora niente:
il database, i bonus dei set e i salvataggi si collegano nei passi 16.3–16.5.

Test nuovi in `NamedItemTests` (15), con definizioni create nel test sulle basi vere: colori, prezzi,
valori per livello, ±10% e stesso seme, niente unici dalla tabella, proprietà sul cavaliere e via
tutte togliendo l'oggetto, bonus a 2, 3 e 4 pezzi, livello del pezzo più basso, anelli, `Dispose`,
JSON, unico sconosciuto, oggetto di prima della M16, candidati del database, nome. 452 EditMode
verdi; PlayMode di equipaggiamento, inventario, loot e salvataggi verdi (31).

## Passo 16.3 — In gioco

**Com'è andata (25 ott 2026).** Gli asset sono anticipati qui dal 16.4, perché il loot in gioco abbia
da cosa scegliere: uno script Python (`named.py` nello scratchpad, come i gradi della M14) scrive i 12
unici in `Data/Named/Uniques`, i due set e i loro pezzi in `Data/Named/Sets`, il
`NamedItemDatabase` e le 22 righe dei nomi nelle due lingue, dalla tabella approvata. Il menu
*Aggiorna il database* rifà anche quello degli unici (`ItemTools.UpdateNamedDatabase`).

Combattimento: `CombatFormulas` ha `AttackSpeedMultiplier` (fermo al +50%), `LifeStolen` (fermo al
10%) e `ReduceDamage` (mai sotto 1). In `MeleeAttack` la velocità degli oggetti si moltiplica con
quella del Rapido in `SpeedMultiplier`, che usano già carica, intervallo, animazione e settore; il
rubavita cura sul danno davvero tolto alla vita del bersaglio, quindi mai su un barile. `Health`
toglie il danno ridotto a ogni colpo preso. I bonus dei set nascono con l'inventario del cavaliere
(`PlayerInventory`) e si chiudono con lui.

Loot: `LootRoller.Promote` fa il tiro a parte dal seme dell'oggetto (`SeedMixer.ForNamed`), dopo la
rarità minima, nei tiri di nemici, barili e casse e nei tiri in più con il moltiplicatore del
`LootBonus` (`NamedFactor`: 3 per campioni, unici e forzieri, 10 per il tesoro del re). Il tesoro del
re ha il tiro suo del 25% (`NamedGuarantee`, `SeedMixer.ForNamedGuarantee`), che vale a ogni
difficoltà se nessun oggetto lo è già. Le probabilità stanno nella `DifficultyTier`
(`UniqueChance`, `SetChance`), con i numeri della D7. `-namedchance <x>` le moltiplica, anche in una
partita ripresa. Il salvataggio e i ricordi dei livelli ritrovano unici e pezzi
(`SaveGame`, `LevelRecorder`); il formato 7 resta per il 16.5.

A terra la luce di unici e set è più forte (×1,6) e, quando toccano terra, suonano
`unique_drop.wav` e `set_drop.wav`, da `Tools/Audio/sounds/named_drop.py`, sopra il suono di sempre.

Test nuovi: `NamedLootTests` (9: formule, loot di sempre invariato al Crepuscolo e all'Ombra, tiro
dalla base e ripetibile, tesoro del re al Crepuscolo circa uno su quattro, campioni ×3, probabilità
per difficoltà, database uguale alla cartella con i nomi nelle due lingue, set coerenti, valori della
tabella) e `NamedItemPlayModeTests` (5: un unico che cade all'Ombra con `-namedchance`, rubavita,
velocità d'attacco, danno ridotto, bonus di vita del Custode). Una base che ha solo il pezzo di set
(gli stivali di cuoio) resta com'è quando il tiro cade sull'unico, come vuole la D7. 461 EditMode
verdi; 66 PlayMode delle classi toccate verdi, critico compreso.

## Trappole note

- **I valori delle rarità.** `Rarity` si salva come numero e fa da indice in più posti (pesi della
  `RarityTable`, `RarityMultiplier`, colori). I due valori nuovi vanno in fondo, e ogni `switch` e
  ogni array indicizzato per rarità va guardato uno per uno: un `default` che torna bianco nasconderebbe
  un unico.
- **Il tiro a parte non deve spostare gli altri.** Il dominio nuovo del `SeedMixer` non consuma tiri
  del nemico, della cassa o del barile: il loot dei semi già provati e dei test con i valori scritti
  resta uguale. Un test lo controlla, al Crepuscolo e all'Ombra.
- **"Più forte di un raro" con la rarità minima.** `RollFrom` rifà l'oggetto alla rarità minima della
  tabella quando esce sotto; il tiro dell'unico va dopo questo, o un forziere che garantisce un raro
  lo rifarebbe raro.
- **Velocità d'attacco e animazioni.** L'intervallo dell'arma governa anche la velocità della clip
  (come il campione Rapido, v1.1.1): accorciarlo senza accelerare l'animazione farebbe partire il
  colpo prima che il braccio arrivi.
- **Rubavita e nemici immortali.** Il rubavita cura sul danno davvero inflitto, dopo armatura e
  blocco, e mai su un barile o un nemico già morto. Il cavaliere invulnerabile della ronda non deve
  falsare le misure.
- **Danno ridotto e il minimo di 1.** Con il danno ridotto alto i nemici dei primi livelli farebbero
  0: il minimo resta 1, come la formula di oggi.
- **Set e anelli.** Due anelli dello stesso set sono due pezzi diversi (due definizioni); lo stesso
  pezzo indossato due volte conta una volta sola.
- **Id degli asset.** Come alla M14: id nuovi generati una volta, mai duplicando un asset.

## Test

- EditMode: rarità e colori, valori per livello con il ±10%, tiro con il dominio suo (stesso seme,
  stesso oggetto; loot di sempre invariato), bonus dei set accesi e spenti mettendo e togliendo pezzi,
  proprietà nuove sulle statistiche, prezzi, tooltip del set, migrazione 6 → 7, fasce della misura,
  nomi presenti nelle due lingue per ogni unico, pezzo e set.
- PlayMode: un unico che cade con `-namedchance`, il set completo indossato con i bonus nel pannello
  del personaggio, velocità d'attacco e rubavita in combattimento, salvataggio e caricamento di un
  pezzo di set.

---

## Checklist di chiusura

- [x] Decisioni D1–D16 confermate
- [x] Tabella degli unici e dei set approvata
- [x] Core: rarità, definizioni, proprietà nuove, bonus dei set
- [ ] In gioco: combattimento, loot, luce e suono
- [ ] Tooltip, inventario e stringhe
- [ ] Formato 7 del salvataggio
- [ ] Misura e taratura
- [ ] Build di prova
- [ ] ADR, lezioni nel piano, README
- [ ] `bundleVersion` 1.4.0 e tag `v1.4.0` dopo i test verdi in CI
