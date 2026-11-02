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
| D9 | **La barra** | **Cambiata il 2 novembre, dopo il 20.4 (D18):** quattro caselle alla Diablo 3. *Prima:* otto posti, **F1–F8** (confermato con il disegno, al posto di sei): il tasto sceglie, il destro lancia, come oggi. Un'abilità attiva presa va nel primo posto libero, e dal pannello si trascina su un altro posto. Il portale resta sul tasto T | È il modo di Diablo 2 e quello di oggi. Con *Armi* le abilità attive possono essere otto, quattro della classe e quattro sue; F4–F8 sono liberi |
| D10 | **Il pannello** | Tasto **N**, a tutto schermo come l'immagine di WoW, nella pelle della M17. In alto i punti di ogni albero. Il nodo preso ha il bordo d'oro, quello disponibile è a colori, quello chiuso è grigio. Il numero dei gradi sta sull'icona; il nodo a scelta è un ottagono che, cliccato, mostra le due abilità. Il **tooltip** dice cosa fa, il grado e cosa serve per aprirlo. Sotto ci sono **Applica modifiche** e **Annulla**. Le due lingue. Un avviso nell'HUD per i punti da spendere, come quello dei punti attributo | Decisioni di Mirco del 19 ottobre 2026 (piano) |
| D11 | **Cambiare idea** | Prima di *Applica* si toglie e si rimette gratis. Dopo, si riparte da zero **all'altare della cappella**, con una seconda voce accanto alla difficoltà. Costa **30 oro × livello** (dal disegno): 300 al 10, 600 al 20, 1500 al 50. La specializzazione si cambia solo così | L'altare c'è già, è di tutte le classi e sta nel borgo, come chiede il piano |
| D12 | **Le icone** | Una per ogni abilità attiva e una per ogni **tipo** di passivo (armatura, blocco, critico…): circa **30–40**. Ci sono tre strade: **Z-Image-Turbo** in ComfyUI (proposta), con le ricette in `Tools/ComfyUI/images/` e la pagina dei semi per Mirco, come alla M17; un **pacchetto CC0** (va trovato, e il download si fa con il sì di Mirco); **render da Blender**, come i modelli della M13 | Z-Image c'è già, è sotto Apache 2.0 e si è vista alla M17. Le icone di WoW sono dipinte, e un render 3D le somiglia meno. Un pacchetto CC0 avrebbe uno stile suo |
| D13 | **Nel codice** | Un nodo è un dato (`TalentNode`: id, posto nella griglia, collegamenti, gradi, effetto, alternativa) e un albero è un asset (`TalentTree`), uno per la classe e uno per specializzazione. La logica (cosa si può prendere, punti, righe, *Applica*, riassegnazione) sta in `Core`, in `TalentBuild`, con i suoi test EditMode. Un passivo è un modificatore della `StatSheet` con l'albero come sorgente, come gli affissi; le `StatType` nuove vanno in fondo. Le abilità attive allargano gli incantesimi di oggi con i tipi nuovi (colpo, carica, grido, turbine, colpo di scudo, muro) | Lo stesso meccanismo degli affissi e degli incantesimi: niente controlli a ogni frame, niente struttura parallela |
| D14 | **Bilancio e ronda** | La misura delle difficoltà prende il cavaliere con un **albero di riferimento** (*Armi*, e *Difesa* come confronto), che `HeroSetup` spende come oggi spende gli attributi. Se le bande della M14 escono dalla tolleranza del 5%, si ritarano i **numeri dell'albero**, non le difficoltà. Nella misura entrano solo i passivi; le abilità attive si provano a mano | Un albero che rende il gioco facile rovina le difficoltà della M14. I passivi sono numeri, e la misura li sa contare |
| D15 | **Si controlla** | **EditMode:** le regole di `TalentBuild` (collegamenti, righe, gradi, nodi a scelta, punti per albero, *Applica*, *Annulla*, riassegnazione); gli alberi del Guerriero (ogni nodo raggiungibile, posti senza sovrapposizioni, ogni testo nelle due lingue, ogni icona c'è); la migrazione 7 → 8; nessuna pergamena nel loot. **PlayMode:** N apre e chiude il pannello; un punto speso e applicato cambia la statistica; un'abilità attiva presa va nella barra e il destro la lancia; l'altare riporta i punti e prende l'oro; *Carica* e *Colpo di scudo* stordiscono; un salvataggio vecchio si apre con i punti | Le regole dell'albero sono logica pura e si provano bene in EditMode; il resto tocca comandi, HUD e salvataggio |
| D16 | **Stima e linee di taglio** | **22,5–30,5 h** dopo il disegno (erano 20–27), di cui 3–4 per le armi a due mani; con le quattro caselle (D18, 6–9 h) **28,5–39,5 h**. Se servirà tagliare, in quest'ordine: Tempesta d'acciaio (diventa un passivo del Turbine); Rappresaglia e Fortezza; Frenesia e Vendicatore; il trascinamento nella barra (le abilità restano nel primo posto libero); un'icona per ogni passivo (resta quella del tipo); i nodi a scelta oltre due; Salto; le armi a due mani (*Armi* con le armi a una mano). Il disegno approvato, il pannello, *Applica* e il salvataggio non si tagliano | Meglio deciderlo adesso che a metà |
| D18 | **Quattro caselle, come Diablo 3** | Richiesta di Mirco del 2 novembre, dopo il 20.4, con le sue scelte: niente più F1–F8 né abilità scelta. **Quattro caselle**, ognuna un tasto: il **sinistro** (sul pavimento cammina, su un nemico usa l'abilità principale), il **destro** (la secondaria), **Q** ed **E**. La camera, che girava con Q ed E, gira con **Z e X**. Nella casella del sinistro vanno solo abilità **senza ricarica e senza mana**: per ora una sola, **l'Attacco**, il colpo dell'arma di sempre. Un **clic su una casella** apre l'elenco delle abilità disponibili per lei; **K** apre le quattro caselle insieme. Una abilità nuova dell'albero va nella prima casella libera tra destro, Q ed E, le altre restano nell'elenco. La barra delle pozioni e quella delle abilità prendono una **grafica alla Diablo 3**, fatta con Z-Image come le icone (D12). Il salvataggio 8 tiene la barra posto per posto, ora quattro | La barra a otto posti di Diablo 2 con un tasto che sceglie era scomoda; quattro tasti sempre pronti sono il modo di Diablo 3 |
| D17 | **Fuori da questa milestone** | Il Mago e la creazione del personaggio (M21); le altre classi (M27, M28); bastoni, bacchette e archi; più configurazioni salvate dell'albero; riassegnare gratis; abilità che fanno scappare i nemici (M24); l'equipaggiamento che si vede addosso (M26) | Ognuna è un lavoro a sé, già nel calendario |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 20.1 | **Il disegno**: la pagina con l'albero della classe e quelli di *Difesa* e *Armi*, cioè rami, stili di gioco, nodi con effetto, gradi e numeri, righe e prezzo dell'altare. Si va avanti **solo con il sì di Mirco** (D6, D7) | 2–3 |
| 20.2 | **Il sistema in `Core`**: `TalentNode`, `TalentTree`, `TalentBuild`, i punti da `CharacterProgress`, i passivi sulla `StatSheet`; il salvataggio 8 con la migrazione e il campione; i test (D4, D6, D13, D15) | 3,5–4,5 |
| 20.3 | **Via le pergamene**: loot e mercante; il Dardo e l'Anello tolti al Guerriero, con i test degli incantesimi su un libro di prova; la barra F1–F8 (D3, D4, D9) | 1,5–2 |
| 20.4 | **Le abilità attive**: le undici del disegno con le animazioni, la corsa, il salto e il turbine che dura, lo stordimento con la sua regola, le statistiche e le condizioni nuove, gli effetti a tempo, gli effetti e i suoni da script come alla M13 (D8) | 6–8 |
| 20.4b | **Le quattro caselle** (D18): sinistro, destro, Q ed E al posto di F1–F8; la camera su Z e X; l'Attacco come abilità del sinistro; l'elenco che si apre dalla casella e con K; la grafica alla Diablo 3 della barra delle pozioni e di quella delle abilità; i test e il salvataggio | 6–9 |
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

## Passo 20.3 — Via le pergamene, la barra a otto posti

**Com'è andata (2 nov 2026).** La barra è diventata **otto posti, F1–F8** (D9). `SpellBook` non ha
più un posto fisso per incantesimo: un'abilità imparata va nel primo posto libero, si sposta con
`Move` (scambiandosi con quella che c'era, e la scelta la segue), si toglie con `Forget`. La
riempie l'albero: `PlayerMagic` ascolta *Applica* e mette le abilità nuove nei primi posti liberi,
toglie quelle che non ci sono più e lascia dove sono le altre. All'inizio c'è solo la radice, il
**Colpo possente** in F1; con *Armi* arriva il Turbine, con *Difesa* il Colpo di scudo. L'altare
lascia solo la radice. La **ricarica è dell'abilità** e non del posto (`SpellCaster`): spostata, se
la porta dietro, e il velo della barra la segue.

**Dardo e Anello fuori dal Guerriero** (D3). `PlayerMagic` non ha più la lista degli incantesimi; il
Dardo, l'Anello e la Pelle di pietra restano nel gioco con il loro comportamento, e i test li danno
con `PlayerMagic.Learn`, il **libro di prova**. Sparisce anche "noto dall'inizio" degli incantesimi.
La Pelle di pietra torna come nodo dell'albero.

**Via le pergamene** (D4). Niente più pergamena del livello (`PlaceScroll`, il tiro con il suo
seme e la tabella `ScrollTable`): gli altri tiri non cambiano, il dominio del seme era a parte. Una
pergamena non si legge più, e il tooltip non lo dice. Le due pergamene restano nel database per i
salvataggi vecchi: al caricamento quelle nello zaino o sul cursore **diventano oro**, al prezzo del
mercante (`Inventory.SellWhere`). Quelle rimaste a terra in un livello ricordato restano oggetti da
vendere: il mercante le compra ancora. Il mercante non ne vendeva già prima.

**Salvataggio.** `SaveGame` scrive la barra posto per posto in `_bar`; caricando, l'albero rimette le
sue abilità e `SpellBook.Arrange` le riporta ai loro posti, ignorando gli id che non ci sono. Prima
di rimetterli `SaveGame` chiede a `PlayerMagic` di allineare la barra all'albero: da spenta, come in
pausa, la magia non sente *Applica*. Gli
incantesimi "noti" di un salvataggio di prima non contano più. Il campione nuovo è `save_v8.json`
(il borgo al livello 24 con *Armi*, il Turbine in F1 e il Colpo possente in F2).

**La scena.** `TalentTools.InstallBar` (menu *Talenti → Barra a otto posti in Core*, e dentro
*Costruisci il Guerriero*) copia l'ultimo posto fino a otto, 48 px con 6 di spazio come la cintura:
la barra va da 233 a 659 px dal centro, e a 1920 × 1080 lascia 93 px prima della sfera del mana. Su
uno schermo 4:3 la tocca: si guarda con le foto del 20.6. I tasti F4–F8 sono nell'asset dei
comandi e nella classe generata.

**Le abilità del Guerriero non fanno ancora niente**: lanciate spendono il mana, fanno il gesto e
partono la ricarica, ma il loro effetto arriva nel 20.4.

**Test.** Riscritti i test del libro (`ManaTests`: primo posto libero, barra piena, spostare e
togliere, la ricarica che segue l'abilità, la barra dal salvataggio), `MagicHudTests` (otto posti,
il Colpo possente in F1, F8 che sceglie l'Anello spostato), `SaveLoadTests` (la barra posto per
posto, all'avvio l'albero e la barra), `ScrollTests` (le pergamene solo nel database, il tooltip
senza "leggi"). Nuovi: `FormatEight_LoadsAsItIs` e, in `TalentPlayModeTests`, la barra che segue
l'albero, il campione 8 caricato nel gioco e la pergamena di un salvataggio 4 che diventa oro; il
salvataggio 2 converte la sua pergamena anche in EditMode. Tolti i due test del tiro della
pergamena e quello della pergamena che insegna. EditMode 553/553, PlayMode senza `Nightly` 291/293 (i
due di sempre saltati); dopo il caricamento a magia spenta, le tre classi toccate 13/13 ed EditMode
di nuovo 553/553.

## Passo 20.4 — Le abilità attive

**Com'è andata (2 nov 2026).** Le undici abilità fanno quello che dice il disegno. Le fa
`WarriorAbilities` sul cavaliere, che ascolta il lancio come `PlayerSpells` per gli incantesimi
(che le lascia stare). I numeri stanno negli asset (danno in percentuale dell'arma, stordimento,
soglia, gittata, velocità mentre dura, battito della Tempesta), scritti da `TalentTools`, e i nodi li
cambiano con `TalentBuild.AbilityValue`: l'asset più i bonus, mai sotto zero. Costo e ricarica del
lanciatore (`SpellCaster.SetNumbers`), la barra che si scurisce e la durata della Pelle di pietra
leggono gli stessi numeri.

**Come si mira.** I colpi (Colpo possente, Esecuzione, Colpo di scudo) vanno al nemico sotto il
cursore, o al più vicino al punto, o al più vicino a portata: l'arma più un passo, senza inseguire.
La **Carica** vuole un nemico tra 3 e 10 m con la strada dritta e libera, e ci corre a tre volte la
velocità. Il **Salto** va sul punto, al più a 8 m, e si ferma a un muro. Turbine, Tempesta, Grido,
Muro e Ultimo baluardo partono dal cavaliere. **Senza niente da colpire il lancio non parte e non
spende** (`CastResult.NoTarget`, e `NoShield` per i due dello scudo): il controllo arriva prima del
mana, con `IAbilityTargeting`.

**Il colpo d'abilità** (`MeleeAttack.Strike`) è sempre a segno e senza blocco, con il danno dell'arma
in percentuale, Forza, livello, il bonus del danno, il critico e il rubavita, e annuncia il colpo a
segno come un colpo normale (fermo del tempo, suoni, Disciplina). Tira da un **flusso suo**, così i
tiri dei colpi normali, dei blocchi e dei critici restano dove sono. Le aree colpiscono ognuno una
volta, ognuno con il suo tiro.

**Lo stordimento** è in `EnemyAI.Stun`, con la regola in `StunTimer` (logica pura): metà per il re, i
campioni **e gli unici**, mai per gli Inamovibili, niente di nuovo mentre dura e per 3 s dopo.
Stordito, il nemico annulla il colpo e il lancio e sta fermo; il re ferma anche anello ed evocazioni.
Sopra la testa le stelle, con il loro tintinnio.

**I passivi.** Valgono le statistiche nuove: il blocco in più (`ShieldBlock`, tetto 75%), il danno
preso in meno dopo i punti tolti (tetto 75%), il critico fino a ×2,3, la velocità di movimento (in
`MovementSlow`, che resta l'unico a cambiarla, insieme alle abilità che fanno camminare piano o
correre), le pozioni che curano di più, la Fortezza contro i colpi imparabili. Le **condizioni**
valgono finché reggono: lo scudo in mano e la vita sotto il 30%, ricontrollate solo quando cambiano;
**le due mani valgono "no" fino al 20.5**, e con loro la Presa a due mani e il Turbine a 3,5 m. Gli
**effetti che scattano** stanno in `PlayerTalents`: mana al colpo (Disciplina), al blocco
(Contraccolpo) e all'uccisione (Massacro); la Rappresaglia rimanda una parte di un colpo medio del
nemico, perché un colpo bloccato non tira il danno; il Vendicatore vale per tutti i colpi del frame
dopo il blocco, così un Turbine lo usa tutto; la Frenesia si rinnova senza sommarsi. Il Muro di scudi
blocca tutto senza tirare. L'Ultimo baluardo non cura a parte: la vita sale con il massimo.

**Animazioni, effetti, suoni.** Sei stati nuovi nel controller del cavaliere con le clip di KayKit
(`1H_Attack_Chop`, `2H_Attack_Spinning`, `Block_Attack`, `Blocking`, `1H_Attack_Jump_Chop`,
`Cheering`), messi da `TalentTools.InstallAbilityStates`. Gli effetti sono copie ricolorate di quelli
che il gioco ha: il cerchio d'acciaio del Turbine e del Salto e l'onda d'oro del Grido dall'Anello di
fuoco, le stelle dalle scintille dei campioni. Quattro suoni nuovi da script in
`Tools/Audio/sounds/warrior_abilities.py` (la lama che fischia, lo scudo, l'atterraggio, lo
stordimento); il Grido è una voce, quindi un campione (`grunt_04`).

**Test.** Nuovi `WarriorAbilityNumberTests` in EditMode (i numeri del disegno negli asset, i nodi che
li cambiano, la regola dello stordimento, critico e danno in meno, il lanciatore che legge l'albero)
e `WarriorAbilityTests` in PlayMode (il Colpo possente a portata e senza bersaglio, la Carica e il
Colpo di scudo che stordiscono, la regola per grado, il Turbine, il Grido e il Muro, il Salto,
l'Ultimo baluardo, Disciplina e le condizioni). Il primo test della barra (`MagicHudTests`) ora prova
che il destro su un pavimento vuoto non lancia il Colpo possente. EditMode 558/558, PlayMode !Nightly 301/303 (i due saltati di sempre).

## Passo 20.4b — Le quattro caselle (D18)

**Com'è andata (2 nov 2026).** Niente più F1–F8 né abilità scelta: `SpellBook` ha **quattro caselle**,
sinistro, destro, `Q` ed `E`, e tiene a parte l'elenco delle abilità note (`Known`). Il destro lancia
quella del destro, `Q` ed `E` le loro (`PlayerInputReader.SkillPressed`); il sinistro del mouse resta
il click del controller. La camera gira con `Z` e `X`, `K` apre il menu.

**Il sinistro e l'Attacco.** `SpellBook.IsPrimary` vuole mana 0 e ricarica 0, come ha scritto Mirco:
con gli asset di oggi lo soddisfa solo il nuovo asset **Attacco** (`SpellKind.Attack`, fatto da
`TalentTools`, icona della spada), che `PlayerMagic` impara all'avvio. L'Attacco **non passa dal
lanciatore**: il colpo è sempre quello di `PlayerController` e `MeleeAttack`, la casella lo mostra e
lo assegna soltanto (`TryCastAt` su quella casella dà `NotKnown`). Le altre tre caselle non accettano
l'Attacco, e nel sinistro non va nient'altro.

**Note ma fuori dalle caselle.** Un'abilità nuova va nella prima casella libera che la accetta; se
sono piene resta tra le note e il menu la mette dove si vuole. Assegnare scambia con l'abilità che
c'era, se ci può stare nell'altra casella. L'altare e l'*Applica* tolgono e mettono dall'elenco
(`PlayerMagic.SyncWithTree`).

**Salvataggio.** Il formato resta l'8: `_bar` ora ha quattro posti e `_selectedSpell` si scrive a 0 e
non conta più. Una barra a otto posti (il campione `save_v8`, del 20.3) si rimette per ordine nelle
prime caselle libere, con l'Attacco a sinistra. Al caricamento prima l'albero rimette le abilità
(`SyncWithTree`), poi `Arrange` le mette dove stavano.

**Il menu** (`SkillMenu`, `SkillChoiceView`): un click su una casella apre la sua riga, con le abilità
note che ci vanno e quella di adesso accesa; sceglierne una chiude, un secondo click sulla stessa
casella pure. `K` apre le quattro righe e le lascia aperte; `Esc` le chiude prima di inventario e
pausa. La casella aperta si accende nella barra. Finché le icone non ci sono (20.7) le abilità si
leggono dal nome.

**La grafica alla Diablo 3.** Quattro immagini con Z-Image-Turbo (`Tools/ComfyUI/images/hud.py`, semi
2042, 2052, 2063, 2071): la pietra che si ripete, la cornice a nove parti, la casella d'oro delle
abilità e quella di ferro delle pozioni. La console è 958×132, centrata in basso, con le sfere
attaccate ai lati; da sinistra il portale, le otto pozioni e le quattro caselle con il tasto scritto
sotto (SX, DX, Q, E; in inglese LMB, RMB). L'esperienza sta sopra la console. Lo monta
`HudTools.BuildConsole` (si rilancia), le impostazioni di import sono in `GeneratedUiImporter`. Il
menu prende la pelle delle finestre come le altre.

**Vita e mana in provette (3 nov 2026).** La sfera tonda era tagliata dalla console, e Mirco ha chiesto una
forma rettangolare arrotondata, uguale per vita e mana, nello stile della console. Dopo quattro
alternative di layout (serbatoi, riquadri, barre, provette) e quattro famiglie di cornici, ha scelto le
**provette** con la cornice del **teschio con le corna** (ricetta `gauge_skull` di `images/hud.py`, seme
2102). `Tools/ComfyUI/gauge_parts.py` ne ricava i quattro pezzi (cornice col vetro svuotato, vetro,
liquido rosso e blu) e `HudTools` li monta: vetro, liquido che sale e cornice sopra, 76×250, attaccate
ai lati della console. I numeri («357 / 357») stanno **sopra la corona**, e compaiono con il mouse
sopra tutta la provetta o sempre con l'opzione. Foto `hud_bar_*` e `hud_numbers_*`.

**Test.** `ManaTests` (le caselle, la regola del sinistro, assegnare, `Arrange` anche dal vecchio),
`SaveMigrationTests` (la barra a otto posti nelle quattro caselle), `MagicHudTests` (il destro, `Q` ed
`E`, le caselle, il menu, `K` ed `Esc`), e i test della magia, dei salvataggi, del Guerriero e della
camera messi sui nuovi tasti. Foto a 16:9 e a 4:3 in `Recordings/Photos/hud_*`. EditMode 560/560;
PlayMode !Nightly 302/307 al primo giro (più i 2 saltati di sempre): i tre falliti erano test da
aggiornare (il Grido e il Baluardo sui tasti giusti, un click ora sotto la console), e le loro classi
sono tornate verdi. La PlayMode intera non è stata rilanciata dopo le correzioni.

## Passo 20.5 — Le armi a due mani (D5)

**Com'è andata (3 nov 2026).** Lo **spadone** (14–22, Forza 40, 1,5 s a colpo, 1×4 celle, 160 oro) e
l'**ascia a due mani** (18–28, Forza 50, 1,75 s, 2×4, 210 oro) sono due `WeaponDefinition` con un campo
nuovo, `TwoHanded`, e i modelli `sword_2handed` e `axe_2handed` dello zip di *Adventurers*, che entrano nei
crediti. Li fa `TwoHandedTools` (si rilancia): asset, database, icone dai modelli, voci nelle tabelle dei
forzieri, dei bruti, del re e del mercante (il fabbro li vende, gli altri banchi no).

**La regola dello slot.** `Equipment.Blocking` dice chi sta in mezzo: un'arma a due mani non ha posto
con lo scudo, né uno scudo con lei, e `TryEquip` rifiuta il caso. `Inventory` toglie chi sta in mezzo e lo
rimette nella borsa, sia dal click destro sull'oggetto sia dal click sullo slot; **senza posto per lo scudo
non cambia niente**. L'oggetto che c'era nello slot torna dov'era l'arma nuova.

**Cosa cambia con le due mani.** `MeleeAttack.WeaponChanged` fa ricontrollare i passivi: la condizione
`TwoHanded` ora vale (*Presa a due mani*: +5% danno per grado), e il **Turbine arriva a 3,5 m** invece di 3
(`WarriorAbilities.TwoHandedWhirlBonus`; *Vortice* sale da lì). L'animazione d'attacco è il gesto
`2H_Attack_Chop`, uno stato `Attack2H` del controller messo da `TalentTools` e scelto da
`CharacterAnimatorDriver` quando l'arma lo è, con il suo momento del colpo (0,9 s) per far cadere la lama
insieme al danno. L'attesa è ancora quella a una mano.

**Fuori dalla misura.** `HeroSetup` non prende le armi a due mani: la misura delle difficoltà resta quella di
una mano e scudo (il bilancio non si muove). Il tooltip dice «A due mani».

**Test.** `TwoHandedTests` in EditMode (i numeri, la regola dello slot, il click destro e il click sullo
slot con la borsa che si fa posto o è piena, loot e mercante, la misura), il Turbine a due mani in
`WarriorAbilityTests` (3,6 m: lo spadone sì, la spada no, con la condizione e il gesto), e il conteggio delle
basi in `ItemDatabaseTests`. Foto in cripta del cavaliere con lo spadone e l'ascia, a riposo e a metà colpo,
e della borsa (`PhotoTour.TwoHanded`).

## Passo 20.6 — Il pannello e l'altare (D10, D11)

**Com'è andata (3 nov 2026).** Il tasto **N** apre `TalentPanel`: un velo scuro e una finestra a tutto schermo
nella pelle della M17, con a sinistra l'albero della classe e a destra quello della specializzazione, ognuno
in una griglia 7 × 7. Un nodo è **tondo** se passivo, **quadrato** se abilità attiva, **ottagonale** se a
scelta; chiuso è grigio, aperto ha l'anello azzurro, preso d'oro, e i gradi stanno sull'angolo (`2/3`). Le
linee tra i nodi diventano dorate sulla strada presa, e le **linee tratteggiate** dei punti hanno a sinistra
il numero che chiedono. Le placche le disegna `TalentPanelTools` (`Art/UI/talent_*`): dentro, finché non ci
sono le icone del 20.7, c'è il nome.

- **Click.** Il sinistro prende un grado, il destro lo toglie (solo se preso dopo l'ultimo *Applica*); il nodo
  chiuso non risponde. Il nodo a scelta apre una finestrella con le due opzioni e cosa fanno; scelta una, il
  nodo è preso con quella.
- **Tooltip.** Nome, tipo, gradi, cosa fa e, se ce n'è un altro, cosa fa al grado dopo; per un'abilità attiva
  la descrizione e «mana · ricarica», per un nodo chiuso cosa serve (*prendi prima un talento collegato*,
  *spendi N punti nelle righe sopra*, *nessun punto rimasto*). Le frasi non sono scritte a mano: le fa
  `TalentDescriber` dai numeri degli effetti, così un valore ritarato cambia anche il testo.
- **Specializzazione.** Finché non c'è, due schede (*Difesa*, *Armi*) fanno guardare i due alberi, chiusi;
  prima del livello 10 una riga dice «Disponibile dal livello 10», dal 10 il pulsante *Scegli* la prende, in
  attesa come i nodi. *Applica modifiche* e *Annulla* stanno sotto, accesi solo con qualcosa da applicare, e
  «Non ancora applicato» sta a destra.
- **L'avviso.** `TalentPointsButton`, accanto alla sfera del mana (come quello degli attributi sta accanto a
  quella della vita): «Abilità +N», o «Scegli la specializzazione» dal 10. Sparisce con il pannello aperto.
- **L'altare.** Una seconda voce sotto *Rifai il mondo*: «Riassegna le abilità · 30 oro × livello» (300 al
  10, 1500 al 50), accesa solo se c'è qualcosa di applicato; chiede conferma, prende l'oro (con poco, dice
  «Oro insufficiente» e non cambia niente), riporta tutti i punti e la specializzazione e lo scrive.
- **Il resto.** `Esc` chiude il pannello prima della pausa; aprirlo chiude inventario, personaggio, negozio e
  menu delle caselle. I testi sono nella tabella nelle due lingue: i nomi dei nodi, le descrizioni delle undici
  abilità e le frasi degli effetti.

**Errori trovati strada facendo.** Le copie di un pulsante (la voce dell'altare, l'avviso) si portano dietro il
`LocalizedText` che riscrive il vecchio testo: lo toglie lo strumento. Il velo stava fuori dalla finestra e
restava acceso a pannello chiuso, oscurando tutto: ora velo e finestra sono nel contenitore `Dialog` che si
spegne, e `HudSceneTests` controlla che a riposo niente di `TalentsUI` sia acceso. Le ombre dentro la finestra
vanno in fondo all'ordine dei figli, non in cima.

**Strumento.** `TalentPanelTools.Build` (menu *DarkDescent / UI / Pannello delle abilità*, si rilancia):
placche, pannello, avviso, voce dell'altare e collegamenti del `CompositionRoot`; poi rifà la pelle. Gli aiuti
di `HudTools` (`Node`, `Text`, `Place`, `Stretch`, `FindHud`) sono ora `internal`.

**Test.** EditMode: `TalentTextTests` (ogni nodo, opzione e frase nelle due lingue; le frasi seguono i numeri;
la traduzione), `TalentBuildTests` (punti da spendere, scelta della specializzazione, linee, azzeramento,
costo dell'altare), `HudSceneTests` (il pannello, l'avviso e la voce dell'altare sono collegati e spenti a
riposo) e `InputBindingsTests` (N). PlayMode: `TalentPanelTests` (N e Esc, il disegno dei due alberi, i click
con *Applica* e *Annulla*, il nodo a scelta, il tooltip nelle due lingue, la specializzazione dalle schede,
l'avviso, l'altare con l'oro). Foto in `Recordings/Photos/pannello` (`PhotoTour.Talents`): vuoto al 1, l'avviso
al 5, la scelta al 12, gli alberi a metà al 30 con il tooltip e la finestrella, l'altare, a 16:9 e a 4:3.

## Passo 20.7 — Le icone (D12)

**Com'è andata (3 nov 2026).** Quarantaquattro icone Z-Image-Turbo, 256 × 256, nello stile di Diablo III:
**undici abilità attive** (una per ognuna) e **trentatré tipi di passivo** (danno, vita, critico, armatura,
blocco…); i tipi servono a più talenti (il danno vale per *Braccio forte*, *Filo affilato* e *Maestria*, la
vita per *Tempra* e *Saldo*…). Le ricette sono in `Tools/ComfyUI/images/talents.py` (un'immagine per nome,
`make.py talents`), con tre semi a testa provati e fogli di confronto per Mirco; Mirco ha lasciato la scelta a
Claude (3 nov), che ha scartato le prove con la cornice bianca o lo sfondo grigio (Grido 2213, Danno 2301,
Furto di vita 2332) e quella dove la torre del Muro di scudi aveva una scritta (2220). I semi scelti sono nelle
ricette; i file stanno in `Art/Icons/Talents`.

- **Dove vanno.** `TalentIconTools.Apply` (menu *DarkDescent / Talenti / Assegna le icone*, si rilancia)
  importa i PNG come sprite e li dà alle undici `SpellDefinition` e alle opzioni dei talenti; `TalentTools.BuildWarrior`
  lo richiama alla fine, così un albero rifatto non perde le icone. Le caselle della barra, il menu delle
  abilità e i nodi del pannello leggono già `Icon`: non è cambiato nient'altro.
- **I nodi.** La placca tonda o quadrata ora ritaglia l'icona (`Mask`): l'icona è figlia della placca e sta
  a tutto tondo. I nodi a scelta (ottagoni) hanno ancora il nome: l'icona è dell'opzione presa, e finché non
  si sceglie non c'è.
- **Test.** `TalentIconTests`: la tabella copre ogni talento e niente di più, ogni abilità e opzione ha la sua
  icona dalla cartella `Talents`, le icone sono quadrate (≥ 128) e nella cartella ci sono tutte e solo quelle
  usate. EditMode 577/577, PlayMode 302 verdi e 2 saltati. Foto in `Recordings/Photos/icone_pannello` e
  `icone_barra`.

## Passo 20.8 — Bilancio e chiusura (D14)

**Com'è andata (3 nov 2026).** La misura delle difficoltà ora prende il cavaliere con l'**albero di
riferimento**: `HeroSetup.SpendTalents` sceglie la specializzazione (*Armi*, e *Difesa* come confronto) e
spende tutti i punti del livello sempre allo stesso modo, dall'alto, riga per riga e da sinistra, prima la
classe e poi la specializzazione; `DifficultyCensus.Reference` dice quale (di partenza *Armi*, `null` per il
cavaliere senza albero). Contano i passivi della scheda con lo scudo in mano; le abilità attive e i colpi
rimandati no.

- **La misura non vedeva il critico.** `PowerCensus` contava i punti di critico ma non il danno del critico
  (Punto debole): `CensusFighter.CritDamage` ora ci entra, con `CritMultiplierFor` come nel gioco.
- **I numeri dell'albero, non le difficoltà.** Con i numeri del disegno il cavaliere con *Armi* usciva dalle
  fasce: il Crepuscolo alla profondità 8 cadeva in 3,79 colpi (fascia 4–14), l'Abisso II cominciava il 6%
  più facile dell'Abisso I, e a fine gioco il bruto lo colpiva il 3–4% delle volte (almeno 5%). Si sono
  ritarati i numeri di nove nodi (elenco in `M20_albero_Guerriero.md`): meno danno, meno critico e molta meno
  armatura in percentuale. Con l'albero il bruto cade in al più il 20% di colpi
  in meno che senza (la Tenebra alla profondità 1: 8,83 contro 10,98) e il cavaliere regge al più il 15% di colpi in più.
- **La tolleranza.** Le fasce dei colpi per uccidere perdono il 5% sul lato corto (`CensusChecks.SwingsFloor`,
  la stessa tolleranza dei gradini): il Crepuscolo alla profondità 8 cade in 3,90 colpi e l'Ombra alla
  profondità 1 in 8,97. Le fasce del colpo subito (almeno il 5% dei colpi arriva) reggono senza tolleranza
  con *Armi* (5,9% al minimo) e con *Difesa* (5,1%).
- **Un margine sottile.** Il gradino Abisso I → Abisso II alla profondità 1 è al −4,0% (limite −5%; senza
  albero era −3,4%). La ronda con i semi della notte oscilla del 3%: se un giorno cade lì, è il posto dove
  guardare.
- **La Difesa e il colpo subito.** A fine gioco il cavaliere senza albero è già vicino al minimo del colpo
  subito (7%), quindi blocco e armatura in più lo portano sotto il 5% con poco: per questo l'Armatura del
  custode vale +1% per grado e lo Scudo pesante +2 punti di blocco. Se un giorno si vuole una *Difesa* più
  forte, prima si alza il minimo della fascia (una decisione di Mirco, non un numero dell'albero).

**Test.** `HeroSetup_SpendsTheReferenceTree` (spende tutti i punti, sempre gli stessi, niente al livello 1),
`ReferenceTree_Defense_StaysInTheBands` (le stesse fasce con la *Difesa*) e
`ReferenceTree_StrengthensTheKnightALittle` (contro il cavaliere senza albero, al più il 22% in meno di colpi
per uccidere e il 30% in più di colpi retti, in tutte e due le specializzazioni) in `DifficultyCensusTests`;
le fasce di `CensusChecks` valgono anche per la ronda. Frasi di Braccio forte e Armatura del custode
aggiornate in `TalentTextTests` e `TalentPanelTests`.

**Chiusura.** ADR-128…133 in `DECISIONS.md`, le lezioni e la riga v2.71 nel piano, la v1.8 nel README,
`bundleVersion` 1.8.0, le prove della M20 in `docs/Da_provare.md`.

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
- [x] Niente pergamene nel loot né dal mercante; Dardo e Anello fuori dal Guerriero (20.3)
- [x] Abilità attive con animazioni, stordimento, effetti e suoni (20.4)
- [x] Le quattro caselle, il menu delle abilità e la grafica alla Diablo 3 (20.4b)
- [x] Spadone e ascia a due mani nel loot e dal mercante (20.5)
- [x] Pannello N con *Applica*, l'avviso dei punti, l'altare che riassegna; foto approvate (20.6)
- [x] Icone approvate (20.7)
- [x] Misura delle difficoltà con l'albero di riferimento dentro la tolleranza (20.8); ronda verde: da leggere dopo la notte
- [ ] EditMode e PlayMode verdi in locale e in CI
- [x] ADR, lezioni nel piano, README, prove da fare, `bundleVersion` 1.8.0
- [ ] Tag `v1.8.0` dopo i Tests verdi, build verde, peso dell'artefatto
