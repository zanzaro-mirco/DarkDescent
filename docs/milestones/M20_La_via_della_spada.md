# M20 — "La via della spada"

**Cosa deve succedere (Definition of Done):** il Guerriero cresce con un **albero delle abilità nello
stile di World of Warcraft**, e le pergamene degli incantesimi escono dal gioco.

- Il pannello si apre con **N**. A sinistra c'è l'**albero della classe**, a destra quello della
  **specializzazione**, scelta dal livello 10: *Difesa* o *Armi* (D5).
- I nodi sono collegati da linee e sono di quattro tipi: **attivi**, **passivi**, **a più gradi** e
  **a scelta**. Alcune righe si aprono solo dopo un certo numero di punti spesi nello stesso albero.
- Si guadagna **un punto a ogni livello dal 2**. Le modifiche si provano e si confermano con
  *Applica modifiche*. Per ripartire da zero si va all'altare, pagando in oro.
- Le abilità attive prese vanno nella barra **F1–F8**, e il tasto destro lancia quella scelta.
- Il Dardo e l'Anello di fuoco aspettano il Mago (M21).
- Un salvataggio vecchio si apre con i punti da spendere.

Prima del codice c'è il **disegno dell'albero**, su una pagina che Mirco approva (passo 20.1). Esce
con il tag `v1.8.0`.

**Tempo stimato:** 22,5–30,5 h con le armi a due mani (D5, D16), dopo il disegno approvato; la prima stima era 20–27 h.
**Prerequisito:** M19 chiusa (tag `v1.7.0`).

**Come si lavora:** come alle M13–M19. Faccio io:
- il codice, i dati, le icone e i passaggi nell'editor;
- le prove in batchmode, a Unity chiuso;
- l'avvio di ComfyUI sulla porta 8199.

A Mirco restano:
- le decisioni qui sotto;
- il sì sul **disegno dell'albero** prima del codice;
- il sì sulle icone e sulle foto del pannello;
- la prova in build.

---

## Stato verificato il 2 novembre 2026

Verificato dopo la chiusura della M19 (tag `v1.7.0`, ronda del 2 novembre verde).

| Cosa | Stato |
|---|---|
| Crescita | `CharacterProgress` (M8): livello fino a 50 (D6 della M14), curva 100 × L^1,6, **5 punti attributo a livello** su Forza, Destrezza, Magia e Vitalità, più 2 di vita. Il cavaliere arriva indicativamente al livello 20 alla fine dell'Ombra e al 30 alla fine della Tenebra. Gli oggetti chiedono Forza e Destrezza per essere portati |
| Incantesimi | `SpellBook` con tre posti, F1–F3, e il tasto destro che lancia quello scelto. Sono il **Dardo di fuoco** (proiettile, noto dall'inizio), l'**Anello di fuoco** (area) e la **Pelle di pietra** (+50% di armatura per 20 s). Il **portale** (M11) sta fuori dai posti, sul tasto T. Le tre `SpellKind` sono `Projectile`, `Area` e `Buff`, più `Portal` |
| Pergamene | `ScrollTable` nel loot: una pergamena per livello, al più, portata da un nemico o in una cassa (`PlaceScroll`, con il suo dominio del seme `SeedMixer.ForScroll`). Le vende anche il mercante (`ShopGoods.Scrolls`) |
| Mana | 10 + 2 × Magia + 1 per livello, più gli affissi; torna del 2% al secondo. Con la Magia di partenza sono 30 |
| Combattimento | Colpo d'arma con la Forza e il livello, critico, blocco con lo scudo (`ShieldBlock`: blocco dello scudo + Destrezza / 2, al più 75%), recupero dal colpo (`HitRecovery`). **Nessuno stordimento** né scatto verso il nemico |
| Armi | `WeaponKind`: `Unarmed`, `Dagger`, `Sword`, `Axe`, tutte a una mano. I modelli `sword_2handed` e `axe_2handed` di *Adventurers* sono nello zip, ma non importati |
| Animazioni | `Rig_Medium_CombatMelee` è importato intero. Dentro ci sono anche `Melee_2H_Attack_Chop/Slice/Spin/Spinning/Stab`, `Melee_2H_Idle`, `Melee_1H_Attack_Jump_Chop`, `Melee_Block_Attack`, `Melee_Blocking` e `Melee_Unarmed_Attack_Kick`, che oggi il cavaliere non usa. `Cheering` viene dalla M18 |
| Comandi | Liberi: **N**, F4–F12, i tasti delle lettere non usati. Occupati: 1–8 (cintura), C, I, M, T, Q/E, Alt, Esc, F1–F3 |
| Misura delle difficoltà | `PowerCensus` e `CensusFighter` (D10 della M14) simulano **solo i colpi d'arma**, con attributi, affissi, blocco e velocità: gli incantesimi non entrano. `HeroSetup.RaiseTo` spende i 5 punti con lo schema Forza, Vitalità, Forza, Vitalità, Destrezza |
| Altare | `DifficultyAltar` nella cappella del borgo sceglie la difficoltà (M14) |
| Icone | Le 23 degli oggetti sono renderizzate dai modelli (`ItemTools.RenderIcons`), più 6 immagini per incantesimi, cornice e santuari. Dalla M17 le immagini si possono generare con **Z-Image-Turbo** in ComfyUI (estensione dell'ADR-096), con ricetta e seme nel repo |
| Salvataggio | Formato **7**. Salva i punti non spesi, gli incantesimi noti e quello scelto. L'ultimo campione è `save_v7_camp.json` |
| Test | 538 EditMode e 274 PlayMode senza la categoria `Nightly`. Usano gli incantesimi 12 classi di test, tra cui `SpellTests`, `ManaTests`, `MagicHudTests`, `SaveMigrationTests` e `NightlyGameTests` |

---

## Decisioni

Confermate da Mirco il 2 novembre 2026: prima D2, D3 e D5, che spostano di più il lavoro, poi tutte le altre.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M20 "La via della spada"**, tag **`v1.8.0`**. Dentro: l'albero del Guerriero con il suo pannello, le abilità attive, la barra F1–F8, l'altare che ridà i punti e il salvataggio nuovo. Escono le pergamene | Cambia come si cresce: una versione minore |
| D2 | **I punti attributo** | **Restano**: 5 a livello, come in Diablo. L'albero si aggiunge e non li sostituisce. Le alternative sono ridurli (3 a livello) o toglierli e mettere gli attributi nei nodi | I requisiti di Forza e Destrezza degli oggetti, la misura delle difficoltà, `HeroSetup` e le tabelle della M14 si reggono su quei punti. Toglierli vorrebbe dire ritarare la M14 e la ronda da capo |
| D3 | **Il Dardo e l'Anello di fuoco fino alla M21** | **Tolti al Guerriero**: tornano con il Mago, in M21. Codice e asset restano nel gioco e i test degli incantesimi li usano con un libro di prova. La **Pelle di pietra** diventa un nodo dell'albero della classe. Le alternative sono lasciarli come oggi fino alla M21, oppure farli nodi provvisori dell'albero | Un guerriero che lancia palle di fuoco stona con l'albero. La misura delle difficoltà simula solo i colpi d'arma, quindi toglierli **non sposta il bilancio** |
| D4 | **Via le pergamene** | Le pergamene escono dal loot (`PlaceScroll`) e dal mercante (`ShopGoods.Scrolls`). Il loro dominio del seme è a parte, quindi gli altri tiri non cambiano. Il **salvataggio 8** trasforma quello vecchio così: gli incantesimi noti si ignorano; i punti dell'albero sono quelli del livello, tutti da spendere; le pergamene nella borsa diventano **oro**, al prezzo a cui le comprerebbe il mercante. Un messaggio dice quanti punti ci sono da spendere. Un campione nuovo è `save_v8`, con un test di migrazione da `save_v7_camp.json` | È la richiesta di Mirco (piano, v2.46). Dare tutti i punti a chi ha già un livello è quello che fa WoW quando cambia gli alberi |
| D5 | **Le due specializzazioni e le armi a due mani** | ***Difesa***, con lo scudo: blocco, armatura, vita, colpo di scudo, muro di scudi. ***Armi***, con le **armi a due mani**: danno, critico, turbine, salto. Nella M20 entrano lo **spadone** e l'**ascia a due mani**, con i modelli di *Adventurers*, le clip `Melee_2H_*`, la regola dello slot (occupano anche quello dello scudo), gli affissi, il loot e il mercante (+3–4 h). L'alternativa è *Armi* con le armi a una mano, e le due mani con le classi nuove | È l'esempio di Mirco nel piano. Senza le due mani, *Armi* è "Difesa senza scudo": la scelta della specializzazione perde senso. I modelli e le animazioni ci sono già |
| D6 | **I punti dell'albero** | Uno a ogni livello dal 2. Dal **2 al 9** vanno nell'albero della classe. Dal **10**, quando si sceglie la specializzazione, ai livelli pari vanno nella specializzazione e ai dispari nella classe. Al livello 50 fanno **28 nella classe e 21 nella specializzazione**. Gli alberi hanno circa **36 gradi** nella classe e **28** per specializzazione, quindi si riempiono per circa tre quarti. Prima del 10 l'albero della specializzazione si vede, ma è chiuso. Il primo nodo di ogni albero è **gratis**: Colpo possente al livello 1, Colpo di scudo o Turbine quando si sceglie la specializzazione (dal disegno) | Così le scelte restano vere anche al livello massimo (piano). Prima del 10 non si accumulano punti di una specializzazione non ancora scelta |
| D7 | **Prima il disegno** | Il passo 20.1 è una **pagina** con i due alberi disegnati. Contiene i rami e **tre o quattro stili di gioco**, per esempio *muro* (Difesa), *colpo pesante* e *turbine* (Armi) e *carica* (classe). Per ogni nodo dice effetto, gradi, posto, collegamenti e perché sta lì, più i numeri. Il codice comincia **solo dopo il sì di Mirco** sulla pagina | È la regola del piano: l'albero sensato è la parte importante |
| D8 | **Le abilità attive** | Le abilità attive costano **mana**, come gli incantesimi di oggi. Le candidate, con le animazioni che ci sono già: *Colpo possente* (`1H/2H_Attack_Chop`), *Carica* (corsa e `Jump_Chop`, con uno stordimento breve), *Grido di battaglia* (`Cheering`, danno in più per un po'), *Turbine* (`2H_Attack_Spinning`), *Colpo di scudo* (`Block_Attack`, stordisce), *Muro di scudi* (`Blocking`, blocco pieno per pochi secondi), *Pelle di pietra*. Il disegno ne aggiunge quattro: *Esecuzione*, *Salto*, *Ultimo baluardo* e *Tempesta d'acciaio*, undici in tutto, sette o otto per personaggio. *Colpo possente* è il primo nodo dell'albero della classe, **preso gratis** al livello 1, così il tasto destro fa qualcosa fin dall'inizio. Lo stordimento è nuovo: il nemico si ferma come nel recupero dal colpo, ma più a lungo. Il re e i campioni ne prendono metà, un nemico appena stordito non si stordisce di nuovo per 3 s, e gli Inarrestabili mai (dal disegno). L'alternativa al mana è una **Furia** che si carica colpendo, come in WoW | Il mana c'è già, con la sfera, le pozioni e gli affissi. La Furia renderebbe inutili pozioni e affissi di mana per il Guerriero, e vorrebbe una sfera e un bilancio nuovi |
| D9 | **La barra** | Otto posti, **F1–F8** (confermato con il disegno, al posto di sei): il tasto sceglie, il destro lancia, come oggi. Un'abilità attiva presa va nel primo posto libero, e dal pannello si trascina su un altro posto. Il portale resta sul tasto T | È il modo di Diablo 2 e quello di oggi. Con *Armi* le abilità attive possono essere otto, quattro della classe e quattro sue; F4–F8 sono liberi |
| D10 | **Il pannello** | Tasto **N**, a tutto schermo come l'immagine di WoW, nella pelle della M17. In alto i punti di ogni albero. Il nodo preso ha il bordo d'oro, quello disponibile è a colori, quello chiuso è grigio. Il numero dei gradi sta sull'icona; il nodo a scelta è un ottagono che, cliccato, mostra le due abilità. Il **tooltip** dice cosa fa, il grado e cosa serve per aprirlo. Sotto ci sono **Applica modifiche** e **Annulla**. Le due lingue. Un avviso nell'HUD per i punti da spendere, come quello dei punti attributo | Decisioni di Mirco del 19 ottobre 2026 (piano) |
| D11 | **Cambiare idea** | Prima di *Applica* si toglie e si rimette gratis. Dopo, si riparte da zero **all'altare della cappella**, con una seconda voce accanto alla difficoltà. Costa **30 oro × livello** (dal disegno): 300 al 10, 600 al 20, 1500 al 50. La specializzazione si cambia solo così | L'altare c'è già, è di tutte le classi e sta nel borgo, come chiede il piano |
| D12 | **Le icone** | Una per ogni abilità attiva e una per ogni **tipo** di passivo (armatura, blocco, critico…): circa **30–40**. Ci sono tre strade: **Z-Image-Turbo** in ComfyUI (proposta), con le ricette in `Tools/ComfyUI/images/` e la pagina dei semi per Mirco, come alla M17; un **pacchetto CC0** (va trovato, e il download si fa con il sì di Mirco); **render da Blender**, come i modelli della M13 | Z-Image c'è già, è sotto Apache 2.0 e si è vista alla M17. Le icone di WoW sono dipinte, e un render 3D le somiglia meno. Un pacchetto CC0 avrebbe uno stile suo |
| D13 | **Nel codice** | Un nodo è un dato (`TalentNode`: id, posto nella griglia, collegamenti, gradi, effetto, alternativa) e un albero è un asset (`TalentTree`), uno per la classe e uno per specializzazione. La logica (cosa si può prendere, punti, righe, *Applica*, riassegnazione) sta in `Core`, in `TalentBuild`, con i suoi test EditMode. Un passivo è un modificatore della `StatSheet` con l'albero come sorgente, come gli affissi; le `StatType` nuove vanno in fondo. Le abilità attive allargano gli incantesimi di oggi con i tipi nuovi (colpo, carica, grido, turbine, colpo di scudo, muro) | Lo stesso meccanismo degli affissi e degli incantesimi: niente controlli a ogni frame, niente struttura parallela |
| D14 | **Bilancio e ronda** | La misura delle difficoltà prende il cavaliere con un **albero di riferimento** (*Armi*, e *Difesa* come confronto), che `HeroSetup` spende come oggi spende gli attributi. Se le bande della M14 escono dalla tolleranza del 5%, si ritarano i **numeri dell'albero**, non le difficoltà. Nella misura entrano solo i passivi; le abilità attive si provano a mano | Un albero che rende il gioco facile rovina le difficoltà della M14. I passivi sono numeri, e la misura li sa contare |
| D15 | **Si controlla** | **EditMode:** le regole di `TalentBuild` (collegamenti, righe, gradi, nodi a scelta, punti per albero, *Applica*, *Annulla*, riassegnazione); gli alberi del Guerriero (ogni nodo raggiungibile, posti senza sovrapposizioni, ogni testo nelle due lingue, ogni icona c'è); la migrazione 7 → 8; nessuna pergamena nel loot. **PlayMode:** N apre e chiude il pannello; un punto speso e applicato cambia la statistica; un'abilità attiva presa va nella barra e il destro la lancia; l'altare riporta i punti e prende l'oro; *Carica* e *Colpo di scudo* stordiscono; un salvataggio vecchio si apre con i punti | Le regole dell'albero sono logica pura e si provano bene in EditMode; il resto tocca comandi, HUD e salvataggio |
| D16 | **Stima e linee di taglio** | **22,5–30,5 h** dopo il disegno (erano 20–27), di cui 3–4 per le armi a due mani. Se servirà tagliare, in quest'ordine: Tempesta d'acciaio (diventa un passivo del Turbine); Rappresaglia e Fortezza; Frenesia e Vendicatore; il trascinamento nella barra (le abilità restano nel primo posto libero); un'icona per ogni passivo (resta quella del tipo); i nodi a scelta oltre due; Salto; le armi a due mani (*Armi* con le armi a una mano). Il disegno approvato, il pannello, *Applica* e il salvataggio non si tagliano | Meglio deciderlo adesso che a metà |
| D17 | **Fuori da questa milestone** | Il Mago e la creazione del personaggio (M21); le altre classi (M27, M28); bastoni, bacchette e archi; più configurazioni salvate dell'albero; riassegnare gratis; abilità che fanno scappare i nemici (M24); l'equipaggiamento che si vede addosso (M26) | Ognuna è un lavoro a sé, già nel calendario |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 20.1 | **Il disegno**: la pagina con l'albero della classe e quelli di *Difesa* e *Armi*, cioè rami, stili di gioco, nodi con effetto, gradi e numeri, righe e prezzo dell'altare. Si va avanti **solo con il sì di Mirco** (D6, D7) | 2–3 |
| 20.2 | **Il sistema in `Core`**: `TalentNode`, `TalentTree`, `TalentBuild`, i punti da `CharacterProgress`, i passivi sulla `StatSheet`; il salvataggio 8 con la migrazione e il campione; i test (D4, D6, D13, D15) | 3,5–4,5 |
| 20.3 | **Via le pergamene**: loot e mercante; il Dardo e l'Anello tolti al Guerriero, con i test degli incantesimi su un libro di prova; la barra F1–F8 (D3, D4, D9) | 1,5–2 |
| 20.4 | **Le abilità attive**: le undici del disegno con le animazioni, la corsa, il salto e il turbine che dura, lo stordimento con la sua regola, le statistiche e le condizioni nuove, gli effetti a tempo, gli effetti e i suoni da script come alla M13 (D8) | 6–8 |
| 20.5 | **Le armi a due mani**: spadone e ascia, la regola dello slot, le clip `Melee_2H_*`, gli affissi, il loot, il mercante e le icone dai modelli (D5) | 3–4 |
| 20.6 | **Il pannello** N: i due alberi, le linee, gli stati, il tooltip, *Applica* e *Annulla*, l'avviso dei punti, l'altare. Le foto per Mirco (D10, D11) | 3,5–4,5 |
| 20.7 | **Le icone**: le ricette Z-Image e la pagina dei semi per Mirco; entrano solo con il suo sì (D12) | 2–3 |
| 20.8 | **Bilancio e chiusura**: l'albero di riferimento nella misura delle difficoltà, la ronda, la build da provare, le ADR, le lezioni, il piano, `bundleVersion` 1.8.0, il tag `v1.8.0` dopo i test verdi in CI (D14) | 1,5–2 |

---

## Passo 20.1 — Il disegno

**Com'è andata (2 nov 2026).** Il disegno sta in `M20_albero_Guerriero.md`, generato con la pagina per
Mirco dagli stessi dati: l'albero della classe ha 20 nodi e 37 gradi, *Difesa* e *Armi* 15 nodi e 28
gradi, con quattro stili di gioco (il muro, il boia, il turbine, la carica). Una verifica dei dati
controlla che ogni nodo stia una riga sotto quelli a cui è legato, che i posti non si sovrappongano e
che ogni albero abbia una radice sola. Al livello 50 i punti coprono il 78% di ogni albero.

Dal disegno, rispetto alla scheda: **undici abilità attive** e quindi la barra **F1–F8**; il primo
nodo della specializzazione gratis; l'altare a 30 oro × livello; una **regola dello stordimento**
(re e campioni a metà, 3 s prima di uno stordimento nuovo, Inarrestabili mai), perché con lo scudo
non si tenga fermo il re per tutto lo scontro; **Colpo possente con 3 s di ricarica**, perché con
Disciplina non diventi un colpo al 160% gratis che la misura delle difficoltà non vede. Le ore salgono
a 22,5–30,5. Mirco ha approvato il disegno e gli otto posti il 2 novembre 2026, senza tagli.

---

## Passo 20.2 — Il sistema in `Core`

**Com'è andata (2 nov 2026).** L'albero è un asset per albero, `TalentTree`, in `Data/Talents`:
`Warrior`, `WarriorDefense` e `WarriorArms`. Ogni nodo (`TalentNode`) ha il posto, i gradi, i legami
con la riga sopra e una o due opzioni (`TalentOption`): un'abilità oppure degli effetti
(`TalentEffect`). Gli effetti sono di tre tipi: un modificatore della scheda, anche con una
condizione (scudo, due mani, vita bassa); un numero di un'abilità (`AbilityParam`); qualcosa che
scatta a un colpo, a un'uccisione o a un blocco. Le regole stanno in `TalentBuild`, logica pura:
- i punti vengono dal livello (`TalentPoints`) e non si salvano;
- un nodo si apre quando un legame ha tutti i gradi, e le linee contano i punti spesi nelle righe
  sopra, così togliere un grado non lascia appeso un nodo più in basso;
- le modifiche stanno in uno stato in attesa e diventano vere con *Applica*; il gioco legge solo
  quello applicato;
- la specializzazione si sceglie dal livello 10, anche lei in attesa fino ad *Applica*;
- `Restore` non lancia mai: id sconosciuti, gradi oltre il massimo, legami mancanti e punti finiti
  si lasciano, riga per riga, così un albero ritarato non rompe i salvataggi.

`PlayerTalents`, sul cavaliere, passa il livello all'albero e mette sulla scheda i passivi
applicati con sé come sorgente. Il **salvataggio 8** ha la specializzazione, i nodi applicati e,
già adesso, la barra (posto per posto), che userà il 20.3; un salvataggio 7 si apre senza nodi, con
tutti i punti da spendere. `SaveGame` rimette l'albero **dopo il livello**, che decide i punti, e
prima della vita, che la vita in più dell'albero alza.

Le statistiche nuove vanno in fondo a `StatType`: vita massima in percentuale (letta già dalla vita e
dalla misura delle difficoltà), blocco, danno preso in percentuale, danno dei critici, velocità di
movimento, cura delle pozioni e il colpo imparabile fermato a metà; le altre si leggono nel 20.4.
Anche i dieci tipi delle abilità nuove vanno in fondo a `SpellKind`, con gli asset in `Data/Spells`
(mana, ricarica, raggio e durata del disegno).

**La fonte dell'albero** adesso è `TalentTools.BuildWarrior`, che costruisce abilità, alberi e
componente sul cavaliere e controlla la forma di ogni albero; `M20_albero_Guerriero.md` resta la
fotografia del disegno approvato.

**Test.** Nuovi: `TalentBuildTests` (le regole su due alberi piccoli: punti, legami, linee, nodi a
scelta, *Applica* e *Annulla*, specializzazione, altare, salvataggio tollerante), `WarriorTreeTests`
(i numeri del disegno, le linee ai livelli 7, 21, 18 e 32, ogni nodo raggiungibile, 28 e 21 punti
spendibili al livello 50), il formato 7 → 8 e, in PlayMode, `TalentPlayModeTests` (tre gradi di
Tempra danno +12% di vita solo dopo *Applica*; un livello 30 con *Armi* salvato e ricaricato torna
uguale). Un test della M16 chiedeva il formato 7 scritto: ora chiede il formato corrente. EditMode
553/553, PlayMode senza `Nightly` 289/291 (i due di sempre saltati).

## Fuori dai passi — I numeri del danno solo sui critici

Richiesta di Mirco del 2 novembre, prima del 20.3: il numero del danno non esce più a ogni colpo, solo
sui **colpi critici** (arancio, più grande, con il punto esclamativo, come prima). Restano le scritte
*Mancato*, *Bloccato* e *Imparabile* e il più verde della vita ripresa da un nemico. In
`DamageNumbers` è un interruttore, `_criticalsOnly`, acceso; i colpi normali li dicono già il lampo
bianco, il fermo del tempo e la barra della vita. `GameFeelTests` ora chiede che un colpo normale non
faccia il numero e che il pool riusi i numeri dei critici.

---

## Checklist di chiusura

- [x] Disegno dell'albero approvato da Mirco (20.1)
- [x] `TalentBuild` in `Core` con i test, salvataggio 8 con la migrazione (20.2)
- [ ] Niente pergamene nel loot né dal mercante; Dardo e Anello fuori dal Guerriero (20.3)
- [ ] Abilità attive con animazioni, stordimento, effetti e suoni (20.4)
- [ ] Spadone e ascia a due mani nel loot e dal mercante (20.5)
- [ ] Pannello N con *Applica*, l'avviso dei punti, l'altare che riassegna; foto approvate (20.6)
- [ ] Icone approvate (20.7)
- [ ] Misura delle difficoltà con l'albero di riferimento dentro la tolleranza; ronda verde (20.8)
- [ ] EditMode e PlayMode verdi in locale e in CI
- [ ] ADR, lezioni nel piano, README, prove da fare, `bundleVersion` 1.8.0
- [ ] Tag `v1.8.0` dopo i Tests verdi, build verde, peso dell'artefatto
