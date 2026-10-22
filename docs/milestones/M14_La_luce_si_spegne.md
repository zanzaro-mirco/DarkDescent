# M14 — "La luce si spegne"

**Cosa deve succedere (Definition of Done):** battere il re non chiude il gioco. Si apre la
difficoltà successiva: **Crepuscolo**, **Ombra**, **Tenebra**, poi **Abisso I–VI**. Ognuna ha nemici
più forti e loot migliore. Si sceglie da un **altare all'accampamento**, tra quelle già completate e
quella subito dopo. Dallo stesso altare si può **rifare il mondo**: un dungeon nuovo, con il re vivo,
mentre personaggio, oggetti e oro restano. Il cavaliere sale fino al **livello 50**. Gli affissi
hanno **gradi**: più si scende di difficoltà, più alti i valori che si trovano. **Morire costa** una
parte dell'oro e dell'esperienza, che cresce con la difficoltà e non fa mai scendere di livello.
Tutti i numeri sono **misurati** da uno strumento, come il loot della M13. Esce con il tag `v1.3.0`.

**Tempo stimato:** 12–16 h (D12).
**Prerequisito:** M13 chiusa (tag `v1.2.0`, `v1.2.1` il 21 ott 2026).

**Come si lavora:** come alla M13. Il codice, i dati e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. Il modello dell'altare passa da una foto, e un suono nuovo da una pagina
di ascolto. A Mirco restano le decisioni qui sotto, i nomi dei gradi (passo 14.1), la foto, l'ascolto
e la prova in build.

---

## Stato verificato il 21 ottobre 2026

| Cosa | Stato |
|---|---|
| Affissi | 14 asset in `Data/Affixes`, 12 gruppi; un gruppo esclude gli altri affissi dello stesso gruppo sullo stesso oggetto. Due gruppi hanno già due gradi: **danno** (Affilato 20–40%, Feroce 41–65% dal livello 2) e **vita** (dell'Orso 5–10, del Grifone 11–20 dal livello 2). Il generatore sceglie **uniforme tra gli affissi ammessi**, quindi dal livello 2 danno e vita escono il doppio degli altri. Magico 1–2 affissi, raro 3–4. Un oggetto salva l'id dell'affisso e il valore |
| Livello dell'oggetto | È la **profondità** (1–8), non il livello della zona (`LootRoller`). Decide gli affissi ammessi e il prezzo (× (1 + livello/10)) |
| Livello della zona | `ProgressionSettings.ZoneLevel` = profondità × 1,25 (1–10). Serve solo a ridurre l'esperienza di un cavaliere più forte della zona |
| Personaggio | Livello massimo **20**, soglia 100 × L^1,6. Per arrivare al 20 servono 86.890 punti di esperienza; con la stessa curva il 50 ne vuole 979.430 (il 30: 254.990; il 40: 544.690). Una discesa intera porta circa al livello 9–10 (simulazione della M8) |
| Nemici | Numeri **fissi per tipo**: la vita sta nel prefab, il danno nell'attacco. Con la profondità cresce solo l'esperienza (+15% a livello). Campioni e unici moltiplicano vita, danno ed esperienza (`RankNumbers`, applicati da `SpecialEnemy` con `Health.MultiplyMax` e `SetDamageMultiplier`) |
| Colpire | 75 + Destrezza/2 − Armatura del bersaglio + bonus a colpire, tra 5 e 95 (come Diablo 1). Con l'armatura dei gradi alti i nemici arriverebbero al 5% e il cavaliere diventerebbe intoccabile |
| Rarità | `RarityTable.Default` 65/28/7, spostata per campioni, unici e forzieri (`LootBonus`, ADR-092) |
| Re | `BossController`, tre fasi, nessun modificatore dei campioni. Cinque modificatori in `Data/Enemies/Modifiers`: Furioso, Rapido, Gelido, Rigenerante, Inamovibile |
| Vittoria | Portale della vittoria → accampamento → `VictoryScreen` → menu. `RunStats.BossDefeated` è salvato. Dopo la vittoria i livelli restano ripuliti (ADR-079) e il tesoro del re si apre subito. **Non c'è modo** di rifare il mondo né di cambiare difficoltà |
| Morte | ADR-048: non si perde niente. Si torna all'ingresso del livello e i nemici tornano a casa a vita piena (`DeathScreen`, pulsante Continua) |
| Salvataggio | **Formato 5** (ricordi dei livelli, ADR-079). Le fixture congelate arrivano a `save_v4.json`: la v5 va congelata prima del formato 6 |
| Mercante | Merce dal seme (`SeedMixer.ForShop`), alla profondità più bassa esplorata (`ExplorationMemory.DeepestDepth`) |
| Accampamento | `Data/Levels/Town.txt`, discesa `>` in alto al centro. L'altare della cappella (`Altar.fbx`, M13) e il cristallo (`Crystal.fbx`) ci sono già |
| Test | 375 EditMode e 227 PlayMode verdi |

---

## Decisioni

Confermate da Mirco il 21 ottobre 2026, tutte come proposte ("procedi con il 14.1"). Le D2, D3, D5 e D9 riprendono quello che ha già deciso il 18 ottobre 2026
(piano v2.39 e v2.40); qui sono messe in numeri.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M14 "La luce si spegne"**, tag **`v1.3.0`**. Dentro: difficoltà, gradi degli affissi, livello 50, punizione alla morte. Unici e set vengono dopo, in una milestone loro | Il nome viene dalle difficoltà, la luce che si spegne scendendo (v2.40). Le quattro parti si toccano: i gradi servono solo se le difficoltà alzano il livello degli oggetti, e la morte costa secondo la difficoltà |
| D2 | **Le difficoltà** | **Crepuscolo** (*Dusk*), **Ombra** (*Shadow*), **Tenebra** (*Darkness*), **Abisso I–VI** (*Abyss I–VI*): nove gradini in una `DifficultyTable` (un asset, una riga per gradino). Una partita nuova parte dal Crepuscolo, che è il gioco di oggi | Decise da Mirco (v2.39, v2.40). Quanti gradini dell'Abisso ci sono è un dato: se ne aggiungono senza codice |
| D3 | **L'altare all'accampamento** | Accanto alla discesa nella cripta, l'**altare della cappella** (M13) con un cristallo grande, del colore della difficoltà (oro, viola, blu notte, rosso per l'Abisso). Un click apre un pannello: le difficoltà in colonna, quelle aperte cliccabili, quella attuale segnata, le chiuse in grigio con il lucchetto. Si possono scegliere **quelle completate e quella subito dopo**. Uccidere il re a una difficoltà apre la successiva, e si può sempre tornare a una più bassa. Sotto c'è il pulsante **Rifai il mondo**. Scegliere un'altra difficoltà o rifare il mondo chiede una conferma ("Il dungeon cambia e il re rinasce. Personaggio, oggetti e oro restano") | Come in Diablo 3 (v2.39). L'altare c'è già, e un'interazione all'accampamento non chiede un menu nuovo |
| D4 | **Cosa fa "rifare il mondo"** | Un **seme nuovo** per il dungeon (`RunSeed`). I ricordi dei livelli (ADR-079) e la mappa scoperta ripartono, il re rinasce e il suo tesoro torna sigillato. Restano personaggio, equipaggiamento, zaino, oro, libro degli incantesimi, cintura e statistiche della partita (tempo, uccisioni, morti). Il nuovo seme viene dal vecchio, con un dominio suo del `SeedMixer` e un contatore dei mondi: niente orologio, così un test lo rifà uguale | È il pilastro del § 2: il gioco non finisce. Un seme derivato tiene la partita riproducibile, come tutto il resto |
| D5 | **Nemici più forti** | Ogni riga della tabella ha: **livello della zona in più** (Crepuscolo +0, Ombra +10, Tenebra +20, Abisso I +30, poi +3 a gradino fino a +45), moltiplicatori di **vita** e **danno**, punti in più **a colpire** e di **armatura** dei nemici, moltiplicatori di **esperienza** e **oro**. Si applicano a tutti i nemici, re compreso, con la stessa strada dei campioni (`Health.MultiplyMax`, `SetDamageMultiplier`); le due cose si moltiplicano. Il **re** prende anche i modificatori dei campioni: uno all'Ombra, due alla Tenebra, tre all'Abisso I, quattro all'Abisso II e i cinque dall'Abisso III. I numeri indicativi sono nella tabella sotto e **escono dalla misura** (D10) | Il piano diceva "con le formule che ci sono già", ma per vita e danno dei nemici formule non ce ne sono: hanno numeri fissi per tipo. Un moltiplicatore per gradino è un dato, si tara senza codice. Il colpire in più serve per la formula di Diablo 1: senza, l'armatura dei gradi alti renderebbe il cavaliere intoccabile |
| D6 | **Livello 50 e curva** | `MaxLevel` **50**, stessa curva 100 × L^1,6. L'esperienza in più delle difficoltà (D5) porta il cavaliere, indicativamente, al **livello 20** alla fine dell'Ombra, al **30** alla fine della Tenebra, al **50** tra l'Abisso III e il VI. Lo verifica la simulazione della D10. Il livello della zona (D5) entra già nella riduzione dell'esperienza di oggi: all'Ombra un cavaliere di livello 10 non è "troppo forte" | Decisa da Mirco (v2.39). Tenere la curva e alzare l'esperienza dei nemici evita di cambiare i livelli 1–20 che sono già provati |
| D7 | **Gradi degli affissi** | Il **livello dell'oggetto** diventa il **livello della zona** (profondità × 1,25, più quello della difficoltà): da 1 a 55. Ogni gruppo ha **sei gradi**, dal livello dell'oggetto **1, 6, 15, 25, 35 e 45**. Un oggetto può avere solo i **due gradi più alti** che il suo livello ammette, così un oggetto dell'Abisso non esce con +3 Forza. Il generatore sceglie prima il **gruppo**, tutti con lo stesso peso, poi il grado tra i due ammessi: così un gruppo con più gradi non esce più spesso degli altri (oggi danno e vita escono il doppio). I gradi sono **asset**, uno per grado, con l'id loro: gli oggetti salvati non cambiano. Gli affissi di oggi restano i gradi I e II del loro gruppo, con gli stessi id. Esempi: Forza 2–5 / 6–10 / 11–16 / 17–24 / 25–34 / 35–45; danno 20–40% / 41–65% / 66–90% / 91–120% / 121–150% / 151–190%. Valori di tutti i gruppi nel passo 14.1 | Come nel piano (v2.39): è la parte che dà senso a salire. Con due gradi alla volta i valori di un oggetto si possono confrontare, e un grado alto si sente quando esce |
| D8 | **I nomi dei gradi** | **Un nome per grado** che cresce, come in Diablo 2: per esempio Affilato → Feroce → Crudele → Spietato → Brutale → Sanguinario; della Forza → del Gigante → del Titano… Nelle due lingue, con le forme per genere dei prefissi. La lista completa (72 nomi) la propongo nel passo 14.1 e la approva Mirco prima che entri nel gioco. Il tooltip dice anche il grado in piccolo, dopo il valore (II, III…) | Dal nome si capisce subito se un oggetto è buono, come in Diablo. Il numero del grado aiuta chi non ricorda la scala dei nomi |
| D9 | **Morire costa** | Alla morte si perde una parte dell'**oro** che si ha addosso e dell'**esperienza** del livello in corso: Crepuscolo **0% / 0%**, Ombra **10% / 5%**, Tenebra **15% / 10%**, Abisso I **20% / 15%**, poi +2% a gradino fino a **30% / 25%**. L'esperienza persa si ferma all'**inizio del livello attuale**: non si scende mai di livello. Il `DeathScreen` dice cosa si è perso ("−120 oro, −450 esperienza"). Il resto di ADR-048 resta: si torna all'ingresso e i nemici tornano a casa a vita piena | Decisa da Mirco il 18 ott 2026 (piano, "punizione alla morte"). Al Crepuscolo resta il gioco di oggi, che i tester conoscono |
| D10 | **Si misura** | Una simulazione in `Scripts/Core`, come il `LootCensus` della M13: per ogni gradino, un cavaliere al livello e con l'equipaggiamento attesi (oggetti dei gradi di quella difficoltà, punti spesi in Forza e Vitalità). Calcola i **colpi per uccidere** uno scheletro, un bruto e il re, i **colpi presi prima di morire** e l'**esperienza per discesa**. Obiettivo: le stesse fasce del Crepuscolo alla profondità corrispondente (per esempio scheletro 2–4 colpi, bruto 8–14), con il re che a ogni gradino dura un po' di più. Un test EditMode tiene le fasce. Lo strumento dell'editor scrive la tabella in un CSV nello scratchpad | Nove gradini a mano non si tarano provando: con la misura i numeri della D5 sono scelti, non indovinati. La prova in build controlla la sensazione |
| D11 | **Rarità e loot** | Le rarità si spostano a ogni gradino: Crepuscolo 65/28/7, Ombra 55/34/11, Tenebra 45/40/15, Abisso I 40/42/18, poi +1 raro a gradino (dal normale), fino a 35/42/23 all'Abisso VI. La **quantità** di oggetti resta quella misurata nella M13 (5 a livello): migliorano, non aumentano. L'oro segue il moltiplicatore della D5. Il mercante vende al livello della zona della profondità più bassa esplorata, nella difficoltà attuale | Come nel piano. Più oggetti a terra rifarebbero il problema che la M13 ha appena chiuso |
| D12 | **Stima** | **12–16 h**. Se servirà tagliare: prima i nomi per grado (D8, si tiene il nome di oggi con il numero del grado), poi i gradini dell'Abisso oltre il III. La misura (D10) non si taglia | Meglio deciderlo adesso che a metà |
| D13 | **Per provare** | Due argomenti della riga di comando, come `-seed`: **`-difficulty <n>`** (0 = Crepuscolo) e **`-herolevel <n>`**. Con una partita nuova mettono la difficoltà e il livello, con i punti già spesi in modo bilanciato e un equipaggiamento dei gradi giusti, generato dal seme. Così l'Abisso si prova senza battere il re otto volte. In una partita salvata non fanno niente | Senza, per provare una difficoltà alta servirebbero ore. Anche i test PlayMode li usano |
| D14 | **Salvataggio** | **Formato 6**: la difficoltà attuale, la più alta completata (−1 se nessuna) e il contatore dei mondi (D4). Prima si congela una fixture vera del formato 5. Un salvataggio vecchio parte dal **Crepuscolo**; se il re era già battuto, il Crepuscolo risulta completato e l'Ombra è aperta. Gli oggetti vecchi tengono il loro livello e i loro affissi. Il menu mostra la difficoltà nei tre posti | Come nel piano (v2.39). Con i gradi come asset nuovi gli oggetti non cambiano formato |
| D15 | **La vittoria** | Il `VictoryScreen` compare solo la **prima volta** che si completa una difficoltà, con una riga in più ("Si apre: Ombra"). Le volte dopo, il re lascia il portale della vittoria e l'HUD scrive "Il re è caduto". Il titolo del livello porta la difficoltà ("Cripta 3 · Ombra"), il Crepuscolo no | Rivedere la schermata e tornare al menu a ogni re ucciso stancherebbe. Il nome della difficoltà nel titolo dice sempre dove si è |
| D16 | **Fuori da questa milestone** | **Unici e set**: la prossima milestone, che li fa cadere dall'Ombra in su. **Nemici e oggetti che crescono con il personaggio**: restano nel piano, da valutare dopo la prova delle difficoltà. **Basi migliori** delle armi e delle armature (versioni "eccezionali", come in Diablo 2): nel piano, da decidere con unici e set. **Nemici nuovi** per le difficoltà alte | Ognuna è un sistema a sé. Questa milestone resta sul gioco che continua |

**Numeri indicativi della D5, D9 e D11** (escono dalla misura del passo 14.6):

| Difficoltà | Zona + | Vita × | Danno × | Colpire / Armatura + | Esperienza × | Oro × | Rarità N/M/R | Morte oro / esp. | Modificatori del re |
|---|---|---|---|---|---|---|---|---|---|
| Crepuscolo | 0 | 1 | 1 | 0 | 1 | 1 | 65/28/7 | 0% / 0% | 0 |
| Ombra | 10 | 3 | 2 | 15 | 4 | 2 | 55/34/11 | 10% / 5% | 1 |
| Tenebra | 20 | 8 | 3,5 | 30 | 10 | 3 | 45/40/15 | 15% / 10% | 2 |
| Abisso I | 30 | 18 | 5,5 | 45 | 20 | 4 | 40/42/18 | 20% / 15% | 3 |
| Abisso II–VI | +3 a gradino | ×1,4 a gradino | ×1,25 a gradino | +5 a gradino | ×1,3 a gradino | +0,5 a gradino | +1 raro a gradino | +2% a gradino, fino a 30% / 25% | 4, poi 5 |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 14.1 | Gradi degli affissi: il livello dell'oggetto dalla zona, i sei gradi come asset, il generatore che sceglie prima il gruppo, i nomi nelle due lingue (lista da approvare), il grado nel tooltip, i test (D7, D8) | 2–3 |
| 14.2 | Difficoltà in Core: `DifficultyTable` e le sue righe, le regole di apertura, rifare il mondo con il seme derivato, il livello 50, la punizione alla morte come logica pura, i test (D2, D4, D6, D9) | 2 |
| 14.3 | In gioco: nemici e re con i moltiplicatori e i modificatori, loot, rarità, oro, mercante, esperienza, morte nel `DeathScreen`, argomenti `-difficulty` e `-herolevel` (D5, D9, D11, D13) | 2–3 |
| 14.4 | Salvataggio: fixture del formato 5, formato 6, migrazione, difficoltà nei posti del menu (D14) | 1–1,5 |
| 14.5 | L'altare e il pannello all'accampamento, la conferma, la vittoria per difficoltà, il titolo del livello, le stringhe nelle due lingue; foto dell'altare con il cristallo (D3, D15) | 2–3 |
| 14.6 | Misura e taratura: la simulazione, i numeri nella tabella, il test delle fasce, il CSV per Mirco (D10) | 1,5–2 |
| 14.7 | Build da provare (Abisso con `-difficulty`) e correzioni | 1–2 |
| 14.8 | Chiusura: ADR, lezioni, piano, README, `bundleVersion` 1.3.0, tag `v1.3.0` dopo i test verdi in CI | 1 |

## Passo 14.1 — Gradi degli affissi

`LootRoller` passa al generatore il livello della zona invece della profondità; il `LootCensus`
della M13 lo segue. `AffixDefinition` resta com'è, con il livello minimo che c'è già; il "solo i due
gradi più alti" lo decide il generatore, che raggruppa il pool per gruppo una volta sola, senza
allocare a ogni oggetto. Gli asset nuovi si creano da uno script di editor temporaneo, con gli id
generati una volta, e il database degli affissi si aggiorna con il menu di sempre. Prima degli asset
c'è la tabella dei 72 gradi (valori e nomi, inglese e italiano con le forme per genere), che approva
Mirco.

**La tabella dei gradi (da approvare).** Il grado I e II di ogni gruppo sono gli affissi di oggi, con
gli stessi id: cambiano solo Feroce e del Grifone, che passano dal livello 2 al 6. I prefissi hanno
in italiano anche le forme femminile e plurale (Affilata, Affilati), come oggi. I valori di
Armatura, a colpire e blocco si ritoccano nella misura del passo 14.6, i nomi no.

| Gruppo | I (liv. 1) | II (liv. 6) | III (liv. 15) | IV (liv. 25) | V (liv. 35) | VI (liv. 45) |
|---|---|---|---|---|---|---|
| **% danno** · prefisso · armi | Affilato (*Sharp*) 20–40 | Feroce (*Savage*) 41–65 | Crudele (*Cruel*) 66–90 | Spietato (*Merciless*) 91–120 | Brutale (*Brutal*) 121–150 | Sanguinario (*Bloodthirsty*) 151–190 |
| **% a colpire** · prefisso · armi, gioielli | Preciso (*Accurate*) 5–10 | Sicuro (*Steady*) 11–15 | Infallibile (*Unerring*) 16–22 | Magistrale (*Masterful*) 23–30 | Impareggiabile (*Peerless*) 31–40 | Perfetto (*Flawless*) 41–50 |
| **Armatura** · prefisso · scudi, armature | Robusto (*Sturdy*) 2–4 | Solido (*Solid*) 5–7 | Rinforzato (*Reinforced*) 8–11 | Temprato (*Tempered*) 12–15 | Fortificato (*Fortified*) 16–20 | Indistruttibile (*Unbreakable*) 21–26 |
| **% Armatura dell'oggetto** · prefisso · scudi, armature · grado I dal liv. 2 | Massiccio (*Massive*) 25–50 | Ferrato (*Ironclad*) 51–70 | Corazzato (*Armored*) 71–90 | Colossale (*Colossal*) 91–110 | Impenetrabile (*Impenetrable*) 111–130 | Adamantino (*Adamantine*) 131–150 |
| **% critico** · prefisso · gioielli · grado I dal liv. 2 | Letale (*Deadly*) 2–5 | Micidiale (*Lethal*) 6–8 | Assassino (*Murderous*) 9–11 | Implacabile (*Ruthless*) 12–14 | Fatale (*Fatal*) 15–17 | Funesto (*Baleful*) 18–20 |
| **mana** · prefisso · gioielli, elmi | Arcano (*Arcane*) 5–15 | Mistico (*Mystic*) 16–25 | Occulto (*Occult*) 26–40 | Esoterico (*Esoteric*) 41–55 | Astrale (*Astral*) 56–75 | Eterno (*Eternal*) 76–100 |
| **% blocco** · suffisso · scudi | della Parata (*of Blocking*) 5–10 | della Guardia (*of Warding*) 11–14 | del Baluardo (*of the Bulwark*) 15–18 | del Bastione (*of the Rampart*) 19–22 | della Cittadella (*of the Citadel*) 23–26 | della Fortezza (*of the Fortress*) 27–30 |
| **Forza** · suffisso · tutto | della Forza (*of Strength*) 2–5 | del Vigore (*of Might*) 6–10 | del Bue (*of the Ox*) 11–16 | del Gigante (*of the Giant*) 17–24 | del Titano (*of the Titan*) 25–34 | del Colosso (*of the Colossus*) 35–45 |
| **Destrezza** · suffisso · tutto | della Destrezza (*of Dexterity*) 2–5 | dell'Agilità (*of Agility*) 6–10 | della Volpe (*of the Fox*) 11–16 | della Pantera (*of the Panther*) 17–24 | del Falco (*of the Falcon*) 25–34 | del Vento (*of the Wind*) 35–45 |
| **Vitalità** · suffisso · tutto | della Vitalità (*of Vitality*) 2–5 | della Salute (*of Health*) 6–10 | della Resistenza (*of Endurance*) 11–16 | della Quercia (*of the Oak*) 17–24 | della Montagna (*of the Mountain*) 25–34 | dell'Immortalità (*of Immortality*) 35–45 |
| **vita** · suffisso · tutto | dell'Orso (*of the Bear*) 5–10 | del Grifone (*of the Griffin*) 11–20 | del Troll (*of the Troll*) 21–35 | del Mammut (*of the Mammoth*) 36–55 | dell'Idra (*of the Hydra*) 56–80 | del Drago (*of the Dragon*) 81–110 |
| **Magia** · suffisso · gioielli, elmi · grado I dal liv. 2 | della Stregoneria (*of Sorcery*) 2–5 | dell'Incanto (*of Enchantment*) 6–10 | del Mago (*of the Magus*) 11–16 | dell'Arcimago (*of the Archmage*) 17–24 | delle Stelle (*of the Stars*) 25–34 | del Cosmo (*of the Cosmos*) 35–45 |

Test: gradi ammessi per livello (mai più di due, mai uno sopra il livello), gruppo scelto con peso
uguale su 10.000 oggetti, oggetti vecchi letti con i loro affissi, nomi presenti nelle due lingue
per ogni grado e genere.

**Com'è andata (21 ott 2026).** Mirco ha approvato i nomi. `AffixDefinition` ha il campo `_grade`
(1–6, `MaxGrade`); il generatore raggruppa il pool per gruppo nel costruttore, sceglie il gruppo
con lo stesso peso e poi il grado tra i due più alti ammessi (`OpenGrades`), senza allocare a ogni
oggetto. `LootRoller` riceve il livello degli oggetti di una profondità (`ItemLevel`); nel gioco è
`ProgressionSettings.ZoneLevel`, e il passo 14.2 ci aggiunge la difficoltà. Il tooltip scrive il
grado in numeri romani, piccolo e grigio, dopo il valore (`ItemDescription.RomanGrade`). I 58 asset
nuovi e le 116 righe di stringhe vengono da uno script Python che parte dalla tabella sopra; Feroce e
del Grifone passano al grado II dal livello 6. Test nuovi in `AffixGradeTests`: due gradi aperti, gruppi
con lo stesso peso su 10.000 oggetti, gradi dello stesso gruppo che si escludono, stesso seme stesso
oggetto, gradi che crescono negli asset, 12 gruppi da 6 tutti nel database, id unici, numeri romani.
`LootSeedTests.Level2_DropsZoneLevelItems`: alla profondità 2 gli oggetti sono di livello 3. 383
EditMode e 227 PlayMode verdi.

## Passo 14.2 — Difficoltà in Core

In `Scripts/Core/Progression`: `DifficultyTable` (ScriptableObject immutabile, in
`Data/Progression/Difficulties.asset`) con le righe `DifficultyTier`, e `DifficultyProgress`
(attuale, più alta completata, contatore dei mondi; `CanSelect`, `Select`, `Complete`, `NewWorld`,
con eventi). `SeedMixer.ForWorld(runSeed, world)`. `ProgressionSettings.ZoneLevel` riceve il livello
in più della difficoltà. `DeathPenalty` calcola oro ed esperienza persi; `CharacterProgress` impara a
togliere esperienza senza scendere sotto l'inizio del livello.

Test: apertura solo della successiva, ritorno a una più bassa, seme del mondo stabile e diverso dal
precedente, curva fino al 50, penalità ai bordi (oro 0, esperienza all'inizio del livello).

**Com'è andata (22 ott 2026).** `DifficultyTable` in `Data/Progression/Difficulties.asset` con nove
`DifficultyTier`: i numeri indicativi della tabella sopra, con i gradini dell'Abisso arrotondati
(vita 18/25/35/49/69/97, danno 5,5/6,9/8,6/10,7/13,4/16,8, esperienza 20/26/34/44/57/74). Colpire e
Armatura in più dei nemici sono lo stesso numero in due campi, da separare nella misura se serve.
Nomi `difficulty.*` nelle due lingue. `DifficultyProgress` sceglie tra le completate e la successiva;
cambiare difficoltà o rifare il mondo avanza il numero del mondo (`WorldChanged`), e
`CompleteCurrent` dice se è la prima volta (per il `VictoryScreen` della D15). `SeedMixer.ForWorld`
ricava il seme nuovo da quello del mondo prima e dal suo numero. `ProgressionSettings`: `MaxLevel`
50, `ZoneLevel(profondità, bonus)` ed `ExperienceForKill` con il bonus della zona e il moltiplicatore
della difficoltà. `DeathPenalty` arrotonda al più vicino (0,19f è 0,18999…); l'esperienza persa è
la parte di quella che serve per il livello, ferma a quella già fatta, zero al livello massimo.
`CharacterProgress.Remove` non fa mai scendere di livello. Test nuovi in `DifficultyTests` (14);
`ExperienceTests` conta 86.890 fino al 20 e 979.430 fino al 50. 397 EditMode e 227 PlayMode verdi.

## Passo 14.3 — In gioco

Il `CompositionRoot` legge la riga della difficoltà attuale quando lega nemici, casse e barili. I
moltiplicatori dei nemici passano dalla stessa strada dei campioni: un campione all'Ombra ha i due
moltiplicatori insieme. Il re prende i modificatori dal seme del mondo, con un dominio suo. Il
colpire e l'armatura in più sono modificatori della `StatSheet` dei nemici, con la difficoltà come
sorgente.

Test PlayMode: uno scheletro all'Ombra ha la vita moltiplicata, il re dell'Abisso I ha tre
modificatori, alla morte all'Ombra l'oro cala del 10%, `-difficulty` e `-herolevel` preparano la
partita.

**Com'è andata (22 ott 2026).** Il `CompositionRoot` tiene la `DifficultyTable` (campo nuovo nella
scena Core) e un `DifficultyProgress`. Nemici: `ApplyDifficulty` in `BindEnemy` moltiplica la vita
(`Health.MultiplyMax`, che si compone con quella del grado), dà il danno della difficoltà con
`SetDifficultyMultiplier` su `MeleeAttack` e `RangedAttack`, e aggiunge colpire e Armatura come
modificatori della scheda con il gradino come sorgente. Il danno di grado e difficoltà si moltiplica
in un punto solo, `DamageMultiplier`: il Furioso riscrive solo il suo. Il re: `PromoteBoss` gli
aggiunge un `SpecialEnemy` di grado nuovo `EnemyRank.Boss` (niente numeri in più, tinta, anello,
loot né punto sull'automappa), con i modificatori scelti da `BossModifiers.Pick` dal seme del mondo
(`SeedMixer.ForBoss`), legato prima del `BossController` perché la sua velocità di partenza comprenda
il Rapido. Loot: `LootRoller.UseDifficulty` cambia rarità e oro (gli stessi tiri, la stessa base),
e il livello degli oggetti, mercante compreso, legge il bonus della zona. Esperienza:
`PlayerProgress.AwardKill` con il gradino. Morte: `HandlePlayerDied` toglie oro ed esperienza con
`DeathPenalty` e il `DeathScreen` ha una riga nuova (`death.loss`, spenta quando non costa niente),
messa nella scena da uno script di editor temporaneo. `-difficulty` e `-herolevel` valgono solo per
una partita nuova; i test li passano con la variabile `DARKDESCENT_ARGS`. Il cavaliere di prova
(`HeroSetup`) spende due punti in Forza, due in Vitalità e uno in Destrezza a livello e porta un raro
del suo livello per slot, la base di valore più alto del mercante che può portare. Le trappole non
cambiano: tolgono già una parte della vita massima. Test nuovi: 3 EditMode (re, loot, punti) e 7
PlayMode (scheletro e campione all'Ombra, esperienza, morte all'Ombra e al Crepuscolo, riga di
comando, re dell'Abisso I). 400 EditMode e 234 PlayMode verdi.

## Passo 14.4 — Salvataggio

Prima una fixture vera del formato 5 (`save_v5.json`), poi il formato 6 con la migrazione 5 → 6.
Come nella migrazione 2 → 3, i valori nuovi si mettono a mano: `JsonUtility` non usa gli
inizializzatori.

Test: la fixture del formato 5 con il re battuto apre l'Ombra; quella senza vittoria resta al
Crepuscolo; il formato 6 rilegge difficoltà, più alta completata e mondo.

**Com'è andata (22 ott 2026).** `save_v5.json` è un salvataggio vero, scritto da un test PlayMode
temporaneo con il codice del formato 5 (cripta 1, due nemici uccisi, 731 d'oro, livello 14, re
battuto) e committato da solo prima del formato 6. `SaveData` ha `_difficulty`, `_highestDifficulty`
(−1 di partenza) e `_world`; la migrazione 5 → 6 li mette a mano: Crepuscolo, mondo 0, e il
Crepuscolo completato se il re era battuto. `SaveGame.Bind` riceve il `DifficultyProgress`, che
`Capture` scrive e `Apply` rimette prima che il livello si colleghi. Il menu scrive la difficoltà di
ogni posto dopo il luogo ("Posto 3 – livello 3 · Cripta – Livello 3 · Crepuscolo · data"). Test:
formato 5 con il re battuto (Ombra aperta), formato 4 senza vittoria (Crepuscolo), formato 6 che va
e torna, il salvataggio in gioco che porta e rimette la difficoltà, il riepilogo del menu. 402
EditMode e 235 PlayMode verdi.

## Passo 14.5 — L'altare

Un simbolo nuovo nella mappa dell'accampamento, accanto alla discesa. Il prefab è l'altare della M13
con il cristallo grande, colorato con un `MaterialPropertyBlock` e una luce. Il pannello è in uGUI come
quelli che ci sono già (`ShopPanel`), con la conferma. Stringhe nelle due lingue. La foto
dell'altare accanto al fuoco dell'accampamento passa da Mirco. Se serve un suono per il cambio di
difficoltà, viene da `Tools/Audio` con la sua pagina di ascolto.

Test PlayMode: il pannello apre solo le difficoltà permesse; dopo la conferma il livello 1 è nuovo e
il re è vivo; la vittoria apre la successiva e mostra il `VictoryScreen` solo la prima volta.

**Com'è andata (22 ott 2026).** L'altare è il simbolo `A` di `Town.txt`, contro la parete nord
subito a sinistra della discesa: nel primo posto provato, in basso a sinistra, la parete lo copriva
alla camera. Il prefab `Prefabs/Town/DifficultyAltar` nasce dal santuario del Vento: modello
`Altar.fbx`, cristallo una volta e mezza più grande, luce più forte, `DifficultyAltar` che tinge
cristallo e luce con il colore del gradino (`MaterialPropertyBlock`). Il `DifficultyPanel` nasce da
una copia della pausa: le righe si creano una volta da un modello, i nomi aperti vanno un po' verso
il bianco (il blu notte sul fondo scuro non si leggeva), e la conferma è quella del menu. Cambiare
difficoltà o rifare il mondo passa da `HandleWorldChanged`: seme da `SeedMixer.ForWorld`, ricordi dei
livelli e mappa svuotati, portale dimenticato (`TownPortals.Forget`), re vivo, mercante riassortito,
titolo e altare ricolorati, salvataggio. `CompleteCurrent` decide la vittoria: la prima volta
`VictoryScreen` con "Si apre: …", le volte dopo "Il re è caduto" e il portale senza schermata. Il
titolo porta la difficoltà dopo il nome (`level.with_difficulty`), il Crepuscolo no. Nessun suono
nuovo. Test: 5 PlayMode in `AltarPlayModeTests`. 402 EditMode e 240 PlayMode verdi; foto dell'altare
(oro, viola, rosso) e del pannello per Mirco.

## Passo 14.6 — Misura e taratura

`PowerCensus` in `Scripts/Core` (logica pura, valori attesi come il `LootCensus`) e uno strumento
dell'editor che scrive il CSV. I numeri della tabella si aggiustano finché le fasce della D10
tengono. Il test EditMode ricalcola tutto con gli asset veri.

## Passo 14.7 — Build da provare

Build dalla CI su richiesta di Mirco, da provare al Crepuscolo con un salvataggio vecchio, all'Ombra
battendo il re, e all'Abisso con `-difficulty 3 -herolevel 32`. Le correzioni vanno in questo passo.

## Passo 14.8 — Chiusura

ADR (gradi come asset e scelta per gruppo; difficoltà come tabella di moltiplicatori; mondo nuovo da
seme derivato; morte che costa), lezioni nel piano, README con una foto dell'altare, `bundleVersion`
1.3.0, tag `v1.3.0` solo con la *conclusion* del run dei test a `success`.

---

## Trappole note

- **Il livello dell'oggetto cambia per tutti.** Da profondità a livello della zona: alla profondità 2
  il livello passa da 2 a 3. Prezzi e affissi ammessi dei semi già provati si spostano, e i test che
  si aspettano un affisso preciso da un seme fisso vanno guardati uno per uno.
- **Il generatore cambia ordine dei tiri.** Scegliere prima il gruppo consuma i tiri in un altro
  modo: lo stesso seme fa un oggetto diverso. Gli oggetti salvati non cambiano (salvano gli affissi,
  non il seme), ma i test con i valori scritti sì.
- **Id degli affissi.** Gli asset nuovi vanno creati con un id nuovo ciascuno. Duplicando un asset
  si copia anche l'id (`OnValidate` lo rigenera solo se è vuoto), e due gradi con lo stesso id
  rovinano i salvataggi.
- **Due moltiplicatori.** Campione e difficoltà si moltiplicano: `SetDamageMultiplier` oggi è
  scritto dal solo `SpecialEnemy`, e chi lo chiama per secondo cancella l'altro. Il valore va
  composto in un punto solo.
- **Rifare il mondo con livelli caricati.** Il cambio avviene all'accampamento, ma i ricordi dei
  livelli, l'automappa, il portale salvato (`portalDepth`) e il tesoro del re vanno azzerati tutti
  insieme, o un portale porta in un livello del mondo vecchio.
- **L'esperienza al livello 50.** `ExperienceToNext` vale 0 al massimo: la barra e la penalità alla
  morte non devono dividere per zero.
- **Il colpire.** La formula di Diablo 1 si satura in fretta (5–95). La misura deve guardare anche
  la probabilità dei nemici di colpire il cavaliere, non solo vita e danno.

## Test

- EditMode: test dei gradi e del generatore, `DifficultyTable`, `DifficultyProgress`, penalità alla
  morte, migrazione 5 → 6, misura delle fasce.
- PlayMode: nemici e re per difficoltà, morte, altare e pannello, vittoria per difficoltà,
  argomenti della riga di comando.

---

## Checklist di chiusura

- [x] Decisioni D1–D16 confermate
- [x] Gradi degli affissi e nomi approvati
- [x] Difficoltà in Core, livello 50, morte che costa
- [x] In gioco: nemici, re, loot, mercante
- [x] Formato 6 del salvataggio
- [x] Altare, pannello e vittoria per difficoltà
- [ ] Misura e taratura
- [ ] Build provata da Mirco
- [ ] ADR, lezioni nel piano, README
- [ ] Tag `v1.3.0` dopo i test verdi in CI: build Windows verde
