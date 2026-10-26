# Piano di Sviluppo — DarkDescent

**ARPG isometrico dark fantasy ispirato a Diablo 1 · versione 2.65**

**Profilo:** sviluppatore esperto, Unity da zero · 6–10 h/settimana
**Obiettivo doppio:** (1) un gioco giocabile e finito, (2) un progetto che regga come materiale da portfolio — repo curato, ADR, build giocabile (§ 8).

| Versione | Data | Cosa cambia |
|---|---|---|
| v1 | — | Piano originale, organizzato per sistemi |
| v2.0 | 20 set 2026 | Milestone verticali, Definition of Done, M0, controllo dell'ambito |
| v2.1 | 23 set 2026 | Stime in ore con consuntivo · linee di taglio e punti di controllo · design di gioco minimo · architettura trasversale · licenze degli asset nel repo pubblico · M2.5 (CI) · dungeon diviso in M6/M7 · test assegnati a ogni milestone · prima build pubblica anticipata a M3 · rituale di chiusura |
| v2.2 | 23 set 2026 | Code review del piano: quota LFS da verificare invece che asserita · ADR-004 con opzione (a) consigliata al posto della (b) · Blender spostato fuori dalla stima di M11 · licenza Mixamo qualificata come lettura prudente · § 4.2 decisa a inizio M2 · segnalata l'incoerenza tra M1 e la sua scheda |
| v2.3 | 30 set 2026 | Rimosso il metodo "io spiego, tu scrivi": Claude scrive il codice di gameplay. Allineati § 8, intestazione e appendice; `CLAUDE.md` aggiornato nel repo |
| v2.4 | 30 set 2026 | Rimossi il devlog e le parti didattiche (esercizi, letture, esperimenti): ore reali solo nella tabella del § 5; § 6 e § 8 allineati, `CLAUDE.md` e scheda M1 aggiornati |
| v2.5 | 1 ott 2026 | Piano spostato nel repo, `docs/Piano_Sviluppo_ARPG.md`, quindi versionato · tolte le ore reali (colonna, rituale) e la retrospettiva · punti di controllo misurati in settimane di calendario · M1 chiusa, scheda M2 scritta |
| v2.6 | 3 ott 2026 | M2 chiusa, con le lezioni · punto di controllo di M2 superato (rapporto 0,19, nessun taglio) · ADR-004: eccezione per il font OFL di TextMesh Pro, EmojiOne tolta |
| v2.8 | 3 ott 2026 | M2.5 chiusa (tag `m2.5`): test e build in CI con GameCI · ADR-011…013 |
| v2.7 | 3 ott 2026 | Stime ricalcolate: il codice lo scrive Claude, le ore sono quelle di sessione di Mirco (totale da 240–350 a circa 95–140 h) · punti di controllo riformulati sulle nuove stime · audio posizionale anticipato a M3 · Cinemachine a M3 con Impulse e zona morta · reazione al colpo del cavaliere a M7 · pavimento della sandbox a M3 · ADR-004…010 scritti |
| v2.9 | 3 ott 2026 | Prima build pubblica su itch.io spostata da M3 a M10, quando il gioco è finibile: decisione di Mirco. A M3 resta la prova della build Web dall'artifact della CI |
| v2.10 | 3 ott 2026 | M3 chiusa (tag `m3`), con le lezioni · § 4.3 decisa (ADR-014) · build Web provata e tenuta (ADR-019) · ADR-014…019 |
| v2.11 | 4 ott 2026 | M4 chiusa (tag `m4`), con le lezioni · attributi e formula del colpo confermati (§ 2, ADR-020) · inventario a click-e-click invece del drag & drop (ADR-023) · ADR-020…024 |
| v2.15 | 5 ott 2026 | M6 chiusa (tag `m6`), con le lezioni · D4 chiusa con i numeri della build: NavMesh a runtime (ADR-035) · asmdef divisa a strati (§ 4.5, ADR-032) · ADR-032…038 |
| v2.14 | 5 ott 2026 | Dopo la prova della cripta generata, su richiesta di Mirco: **pozioni e cintura**, **automappa** (anticipata dalla M7, sovrapposta o nell'angolo), **ripartenza dall'ingresso del livello** dopo la morte fino alla M8, **tooltip delle statistiche** · M6 da 10–13 a 16–19 h |
| v2.13 | 5 ott 2026 | M5 chiusa (tag `m5`), con le lezioni · punto di controllo della M5 superato (rapporto 0,04, nessun taglio) · danno intero (§ 2, ADR-031) · ADR-025…031 |
| v2.65 | 26 ott 2026 | Scheda della **M17 — "Una pelle nuova"** (`docs/milestones/M17_Una_pelle_nuova.md`, D1–D16 da confermare): il ritocco della grafica con Z-Image-Turbo; un giro di foto e una lista corta scelta con Mirco, pannelli con cornice e sfondo al posto dei rettangoli a tinta unita, macchie a terra con un tiro a parte in cripta e caverne, sfondi di menu, morte e vittoria; l'atlante KayKit resta com'è; ricette nel repo e il sì di Mirco su ogni immagine; tag `v1.5.0`, 7–10 h |
| v2.64 | 26 ott 2026 | **M16 chiusa** (tag `v1.4.0`), con le lezioni · unici in oro e set in verde sopra il raro, dodici unici e due set, quattro proprietà nuove, valori che crescono con il livello, un tiro a parte dall'Ombra in su, formato 7 del salvataggio · la misura ha alzato le probabilità degli Abissi (fino al 4%) e i valori di otto unici e dei due set · la build non è stata provata, le prove restano in `docs/Da_provare.md` · ADR-110…113 |
| v2.63 | 25 ott 2026 | **Z-Image-Turbo provato** (immagini da testo in ComfyUI, circa 5 s l'una): la voce dei modelli generati lo dà per pronto, e una voce nuova, il **ritocco della grafica**, lo usa per texture, pannelli e sfondi da rifare, scelti da un giro di foto con Mirco; proposta come M17 · M16: D1–D16 confermate, passi 16.2–16.4 fatti |
| v2.62 | 25 ott 2026 | Scheda della **M16 — "Tesori con un nome"** (`docs/milestones/M16_Tesori_con_un_nome.md`, D1–D16 da confermare): unici in oro e set in verde sopra il raro, una dozzina di unici e due set da quattro pezzi, quattro proprietà nuove (velocità d'attacco, rubavita, danno ridotto, tutti gli attributi), valori che crescono con il livello dell'oggetto, un tiro a parte dall'Ombra in su, misura per difficoltà, formato 7 del salvataggio; tag `v1.4.0`, 10–14 h |
| v2.61 | 24 ott 2026 | **M15 chiusa** (tag `v1.3.1`), con le lezioni · la ronda notturna ogni notte: 40000 mappe, 1600 livelli costruiti, una partita da sola per difficoltà, la partita lunga, le misure, il rapporto e l'issue `ronda` sui fallimenti · corretti il re che mancava all'8, i pavimenti uniti in diagonale attorno alla sala del re, l'Abisso I più facile della Tenebra, NavMesh e mesh mai distrutti · ADR-106…109 |
| v2.60 | 23 ott 2026 | Scheda della **M15 — "La ronda notturna"** (`docs/milestones/M15_La_ronda_notturna.md`, D1–D12 da confermare): i test notturni della v2.58 come milestone, senza versione del gioco; di notte solo i test della categoria `Nightly` (la suite normale la fa già ogni push), 2000 mappe e 200 livelli costruiti con semi nuovi, una partita da sola dalla città al re e oltre, misure, tempi, rapporto nel run e issue `ronda` solo sui fallimenti; 5–8 h |
| v2.59 | 23 ott 2026 | **M14 chiusa** (tag `v1.3.0`), con le lezioni · difficoltà Crepuscolo, Ombra, Tenebra e Abisso I–VI dall'altare, sei gradi per gruppo di affissi, livello 50, morte che costa · la build non è stata provata: al suo posto la rilettura del codice, e le prove restano in `docs/Da_provare.md` · ADR-101…105 |
| v2.58 | 22 ott 2026 | Su richiesta di Mirco, tra i lavori dopo la v1.0: i **test notturni**, un workflow che ogni notte prova centinaia di semi nuovi, una partita automatica, le due misure, i tempi e i salvataggi vecchi, con un report nel run e un'issue solo quando qualcosa fallisce. Subito dopo la chiusura della M14 |
| v2.57 | 22 ott 2026 | Rilettura del piano e delle schede, su richiesta di Mirco: tornano tra i lavori dopo la v1.0 quattro punti rimasti per strada, il **boss a metà discesa** (M12), **pozzi e santuari nelle caverne** e **due avanzi dei barili** (M13), i **tempi di costruzione dei livelli in build** (M11). I muri trasparenti vicino al cavaliere (D5 della M3) si chiudono: bastano i muri bassi che seguono la camera. Spuntate le caselle già fatte delle schede M2.5, M4 e M9 |
| v2.56 | 22 ott 2026 | Su richiesta di Mirco, tra i lavori dopo la v1.0: **l'equipaggiamento si vede addosso al cavaliere** (elmo, armatura, guanti e stivali con le parti dei personaggi KayKit, oltre ad arma e scudo). La M8 l'aveva rimandato alla M11, che non l'ha ripreso |
| v2.55 | 21 ott 2026 | Scheda della **M14 — "La luce si spegne"** (`docs/milestones/M14_La_luce_si_spegne.md`, D1–D16 da confermare): difficoltà Crepuscolo, Ombra, Tenebra, Abisso I–VI scelte da un altare all'accampamento, rifare il mondo, livello 50, sei gradi per gruppo di affissi dal livello della zona, morte che costa oro ed esperienza, numeri misurati da una simulazione; tag `v1.3.0`, 12–16 h |
| v2.54 | 21 ott 2026 | **v1.2.1**: il portale aperto in un angolo stretto, chiuso da ogni parte da muri e barili, finiva dove il cavaliere non arrivava; ora va sul punto libero più lontano. L'ha trovato un test dei portali fallito in CI dopo il tag `v1.2.0` |
| v2.53 | 21 ott 2026 | **M13 chiusa** (v1.2): arredi con un senso e la cappella, barili da rompere, pozzi, santuari, spuntoni e barili esplosivi, modelli da Blender e suoni da script e da Stable Audio, loot misurato a 5,0 oggetti a livello. Come per la M12 la build di prova è saltata su scelta di Mirco, e al suo posto la rilettura del codice · ADR-097…100 |
| v2.52 | 20 ott 2026 | Passo 13.4 fatto: i suoni della M13 generati con Stable Audio 3 piacciono a Mirco ed entrano nel gioco (ADR-096). TRELLIS.2 da una foto da Blender rifà lo stesso modello: l'immagine di partenza da testo con Z-Image-Turbo va negli **sviluppi dopo la v1.0** |
| v2.51 | 20 ott 2026 | Nella M13, D16: TRELLIS.2 parte da una **foto da Blender** di una bozza dell'oggetto, nello **stile KayKit**; un modello testo → immagine (Z-Image-Turbo, Apache 2.0) resta facoltativo, da decidere dopo la prova; il GLB diventa FBX dopo la foto e il sì di Mirco. Lo stile Diablo IV proposto da una pipeline trovata in rete è scartato · passo 13.4 da 1,5–2 a 2–3 h, M13 da 21–27 a 22–28 h |
| v2.50 | 20 ott 2026 | Su richiesta di Mirco la **configurazione di ComfyUI** entra nella M13 come passo 13.4 (D15): Stable Audio 3.0 Small SFX e TRELLIS.2 in int8, guidati da Claude con l'API locale, per i suoni e i modelli organici che lo script fa peggio · M13 da 19–25 a 21–27 h |
| v2.49 | 19 ott 2026 | Su richiesta di Mirco i **suoni** nuovi della M13 si fanno **da script** (D14), con Python, numpy e scipy in `Tools/Audio/`: la prova (santuario, trappola, barile, pozzo) è approvata, e il barile ha due varianti, sintetizzata e ibrida, scelte a caso · M13 da 18–24 a 19–25 h |
| v2.48 | 19 ott 2026 | Su richiesta di Mirco il **loot** si ritara nella M13 (D13): con i mostri e gli oggetti aggiunti cadono circa 10 oggetti a livello; l'obiettivo è 4–5, con la stessa parte di magici e rari, misurato con uno strumento che genera 200 livelli per profondità · M13 da 16–22 a 18–24 h |
| v2.47 | 19 ott 2026 | **v1.1.1** dalla prova di Mirco della v1.1.0: i **modificatori dei campioni si vedono e si sentono** (numeri più marcati, animazioni alla velocità del Rapido, polvere, brina, bagliore e numeri verdi, scintille e "Imparabile!", aura rossa e ruggito del Furioso; ADR-093), il **negozio si chiude allontanandosi** dal mercante, il **confronto con l'oggetto indossato** anche dal banco · ADR-001…093 |
| v2.46 | 19 ott 2026 | Decisione di Mirco sull'**albero delle abilità**: stile **World of Warcraft** (albero della classe e albero della specializzazione affiancati, nodi collegati, a più gradi e a scelta), abilità e magie imparate **salendo di livello** e non più dalle pergamene, **distinte per classe**, un albero sensato per ogni classe disegnato e approvato prima del codice; dopo la M14, con le classi nuove |
| v2.45 | 19 ott 2026 | Su richiesta di Mirco la **M13 — "Stanze vive"**: via il colonnato, arredi con un senso (mai doppioni, girati a caso, legati al tema), un punto d'interesse al centro delle stanze grandi, **barili da rompere**, **pozzi**, **santuari** e **trappole**, e una sezione di **modelli fatti in Blender** da script nel repo (Blender 5.2 LTS installato il 19 ott). Scheda `docs/milestones/M13_Stanze_vive.md`, tag `v1.2.0`; gradi degli affissi e difficoltà passano alla M14 · dalla lista di Mirco restano per dopo mimic, bauli maledetti, porte e leve, lore, prigionieri, statue che si animano |
| v2.44 | 19 ott 2026 | Su richiesta di Mirco il progetto non è più descritto come didattico: tolti la nota sul metodo «io spiego, tu scrivi» del § 8 e l'«obiettivo di imparare» dell'appendice; la descrizione del repo su GitHub diventa «Progetto personale» |
| v2.43 | 19 ott 2026 | **M12 chiusa** (v1.1): campioni con cinque modificatori e scorta, Morvak e Grumm, loot migliore, forzieri e tesoro del re, nomi colorati con i modificatori nella barra in alto e punti sull'automappa. La build di prova è saltata su scelta di Mirco: si prova quella del tag `v1.1.0` |
| v2.42 | 18 ott 2026 | I lavori dopo la v1.0 diventano milestone come le altre: la **M12 — "Nemici speciali"** (campioni con modificatori e scorta, due unici, forzieri e tesoro del re), scheda `docs/milestones/M12_Nemici_speciali.md`, confermata da Mirco, tag `v1.1.0` |
| v2.41 | 18 ott 2026 | **v1.0.2** dalla prova di Mirco della v1.0.1: le spunte delle opzioni si vedono, e nel confronto i due riquadri tornano uguali, con "Equipaggiato" piccolo e grigio chiaro sopra il nome |
| v2.40 | 18 ott 2026 | Le difficoltà prendono nomi nostri, scelti da Mirco al posto di quelli di Diablo: **Crepuscolo, Ombra, Tenebra, Abisso I–VI** |
| v2.39 | 18 ott 2026 | Decisioni di Mirco sulle **difficoltà**: da Normale ai Tormenti, scelte liberamente all'accampamento fino all'ultima completata; livello massimo 50; **punizione alla morte** con oro ed esperienza, senza mai scendere di livello · un pilastro del design: **il gioco non finisce** (§ 2) |
| v2.38 | 18 ott 2026 | Dai suggerimenti dei tester della v1.0, dopo la v1.0: **v1.0.1** con i numeri delle sfere sempre visibili e l'oggetto indossato evidente nel confronto · **suggerimenti al primo uso e aiuto** · **nemici che scappano** · **casse di più tipi** · **ronde dei nemici** · la **schermata di creazione del personaggio** con le classi nuove · le **difficoltà dopo la vittoria** escono dal fuori ambito, da progettare · le quest restano fuori |
| v2.37 | 17 ott 2026 | M11 chiusa (tag `v1.0.0`), con le lezioni · ADR-080…088 · la v1.0 è pronta; la pubblicazione su itch.io è il primo lavoro dopo la v1.0 |
| v2.36 | 17 ott 2026 | Su decisione di Mirco la **pubblicazione su itch.io** esce dalla M11 e passa alla fase dopo la v1.0: la M11 si chiude con il tag `v1.0.0` e la build della CI |
| v2.35 | 17 ott 2026 | Dalla seconda prova della build del passo 11.7, su richiesta di Mirco, dopo la v1.0: **tornare al livello di sopra** con una scala in su · i **barili in mezzo alle stanze** confermati |
| v2.34 | 14 ott 2026 | Dalla prova della build del passo 11.7, su richiesta di Mirco: un **albero delle abilità** dopo la v1.0, con lo stile da scegliere tra Diablo 2, Sacred e World of Warcraft · i **barili da rompere** anche in mezzo alle stanze · la pergamena del ritorno lascia il posto alla **magia del portale** (scheda della M11) |
| v2.33 | 12 ott 2026 | M10 chiusa (tag `m10`), con le lezioni · ADR-071…079 · la M10 resta nelle 19–26 h: le correzioni dopo la prova erano già in tabella |
| v2.32 | 12 ott 2026 | Su richiesta di Mirco, dopo la v1.0: **barili da rompere**, più numerosi, con poco loot · **oggetti unici e set** con i bonus per numero di pezzi, che escono dal fuori ambito del § 1.1 |
| v2.31 | 12 ott 2026 | Scheda della M11 scritta e decisioni confermate da Mirco: filtro retrò con opzione, post-processing per livello, musica del boss, profiling in una build di sviluppo, licenza MIT, itch.io solo Windows · M11 a 20–30 h · dalla prova della M10 i livelli lasciati restano com'erano (ADR-079) |
| v2.30 | 11 ott 2026 | Decisioni della M10 confermate da Mirco · la **prima pubblicazione su itch.io** passa dalla M10 alla **fine della M11** · M10 a 19–26 h, M11 a 18–29 h |
| v2.29 | 11 ott 2026 | M9 chiusa (tag `m9`), con le lezioni · ADR-064…069 · scheda della M10 |
| v2.28 | 11 ott 2026 | Su richiesta di Mirco: un **menu iniziale** con Continua e Nuova partita e **tre salvataggi** nella M10 · **click destro su un oggetto nella borsa per indossarlo** nella M10 · **avviso dei punti attributo da spendere** nell'HUD, nella M10 · **caratteristiche colorate** nel pannello quando oggetti o effetti le cambiano, nella M10 · **nomi degli oggetti a terra con `Alt`**, nella M10 · M10 da 10–14 a 17–22 h |
| v2.27 | 11 ott 2026 | M8 chiusa (tag `m8`), con le lezioni · la M8 sale a 16–22 h con i quattro giri di correzioni dopo le prove in build · la M9 a 10–13 h con le due correzioni chieste da Mirco nell'ultima prova (confronto con due anelli, nomi lunghi nei tooltip) · punto di controllo della M8 (rapporto 0,08, nessun taglio) · ADR-052…063 |
| v2.26 | 11 ott 2026 | Su richiesta di Mirco: la **cintura centrata sotto la barra dell'esperienza** nella M9, insieme alla hotbar · un **dungeon più ricco di oggetti di scena** nella M11 (+2–3 h) · **nemici e oggetti che crescono con il livello del personaggio** dopo la v1.0 |
| v2.25 | 9 ott 2026 | Su richiesta di Mirco: il **movimento con WASD** tra i lavori dopo la v1.0, accanto al click |
| v2.24 | 8 ott 2026 | Su richiesta di Mirco: una **grafica dei personaggi nello stile di Diablo 2** tra i lavori dopo la v1.0, con i vincoli di licenza e di tempo |
| v2.23 | 8 ott 2026 | M7 chiusa (tag `m7`), con le lezioni · la M7 sale a 18–25 h con i tre passi di correzione dopo le prove in build · totale ricalcolato dalla tabella del § 5 · ADR-040…051 |
| v2.22 | 8 ott 2026 | Su richiesta di Mirco: **la vita del nemico che si sta combattendo** resta in alto anche senza il cursore sopra, nella M9 · barre sopra i nemici feriti come opzione, dopo la v1.0 · **nemici speciali** più forti con loot migliore, dopo la v1.0 · il boss della M10 lascia loot migliore |
| v2.21 | 7 ott 2026 | Su richiesta di Mirco: una **punizione alla morte** tra i lavori dopo la v1.0, da valutare prima se le prove della M10 la chiedono |
| v2.20 | 7 ott 2026 | Dopo la stessa prova, su richiesta di Mirco: **alla morte non si perde niente** e il livello non si ricarica (ADR-048, regola che il piano lasciava alla M8) · sciame più leggero, blocco che non ferma, mira sui nemici (ADR-049) · nella M7 il passo 7.11 |
| v2.19 | 7 ott 2026 | Dopo la prova della build M7, su richiesta di Mirco: **elmo, armatura, guanti, stivali, due anelli e amuleto** nella M8, prima del salvataggio, così il formato dei salvataggi nasce con tutti gli slot · M8 da 8–12 a 12–18 h · nella M7 il passo 7.10 di correzioni (D14 della scheda) |
| v2.18 | 6 ott 2026 | Su richiesta di Mirco: **zoom e rotazione della visuale** attorno al cavaliere, nella M8 (proposta: zoom con la rotella, rotazione a scatti di 90° con i muri bassi che seguono la camera; da confermare nella scheda della M8, poi un ADR che aggiorna ADR-017) · M8 da 6–9 a 8–12 h |
| v2.17 | 6 ott 2026 | Su richiesta di Mirco: **colpi critici** del cavaliere, con un verso proprio per ogni tipo di nemico colpito, nella M7 (passo 7.7, D13 confermata) · formula nel § 2 · M7 da 12–18 a 13–20 h |
| v2.16 | 6 ott 2026 | Dopo la prova della M6, su richiesta di Mirco: **musica e rumori d'ambiente** anticipati dalla M11 alla M7 (passo 7.0): un profilo per tipo di livello, tracce CC0 · M7 da 10–15 a 12–18 h · ADR-039 |
| v2.12 | 4 ott 2026 | Decisioni di Mirco sulla M5: **lingue** entrano nella v1.0 (inglese di default, italiano, predisposizione per altre), **blocco** con lo scudo · M5 da 6–9 a 9–12 h · menu delle opzioni con la lingua alla M10 · nuova sezione "Dopo la v1.0" con le armi delle classi future |

---

## Stato del progetto — aggiornato al 23 ottobre 2026

| | |
|---|---|
| **Nome** | DarkDescent |
| **Repo** | `github.com/zanzaro-mirco/DarkDescent` (pubblico) |
| **Cartella locale** | `game_projects/DarkDescent` |
| **Engine** | Unity 6.3 LTS — **6000.3.24f1**, bloccata per tutto il progetto (ADR-001) |
| **Render pipeline** | URP 17.3.0 |
| **Package** | Input System 1.20.0 · AI Navigation 2.0.14 · Cinemachine 3.1.7 · Test Framework 1.6.0 · uGUI 2.0 con TextMeshPro |
| **Assembly** | `DarkDescent.Core` in `Assets/_Project/Scripts/Core/` (logica e dati) e `DarkDescent` in `Assets/_Project/Scripts/` (componenti) (ADR-003, ADR-032) · test in `DarkDescent.Tests.EditMode` e `DarkDescent.Tests.PlayMode` |
| **Milestone chiuse** | M0 — Fondamenta (23 set 2026) · M1 — "Mi muovo" (1 ott 2026, tag `m1`) · M2 — "Colpisco e muoio" (3 ott 2026, tag `m2`) · M2.5 — Pipeline automatica (3 ott 2026, tag `m2.5`) · M3 — "Un dungeon fatto a mano" (3 ott 2026, tag `m3`) · M4 — "Raccolgo roba" (4 ott 2026, tag `m4`) · M5 — "Loot casuale" (5 ott 2026, tag `m5`) · M6 — "Dungeon infinito" (5 ott 2026, tag `m6`) · M7 — "Le profondità" (8 ott 2026, tag `m7`) · M8 — "Progressione e persistenza" (11 ott 2026, tag `m8`) · M9 — "Magia" (11 ott 2026, tag `m9`) · M10 — "Città e loop completo" (12 ott 2026, tag `m10`) · M11 — "Look, feel e release" (17 ott 2026, tag `v1.0.0`) · M12 — "Nemici speciali" (19 ott 2026, tag `v1.1.0`) · M13 — "Stanze vive" (21 ott 2026, tag `v1.2.0`) · M14 — "La luce si spegne" (23 ott 2026, tag `v1.3.0`) · M15 — "La ronda notturna" (24 ott 2026, tag `v1.3.1`) · M16 — "Tesori con un nome" (26 ott 2026, tag `v1.4.0`) |
| **Milestone corrente** | M17 — "Una pelle nuova": il ritocco della grafica con Z-Image-Turbo (scheda `docs/milestones/M17_Una_pelle_nuova.md`, D1–D16 confermate il 26 ott 2026, lista corta approvata: pannelli, menu, opzioni, pausa, vittoria, macchie a terra). Dopo: la risalita e stanze più grandi, proposte come M18 (§ 5) |
| **CI** | GitHub Actions + GameCI, account Unity Personal dedicato: test EditMode e PlayMode a ogni push e PR, build Windows sui tag `m*`/`v*`, build Web ad avvio manuale, la ronda notturna dei test `Nightly` con l'issue `ronda` sui fallimenti (ADR-011…013, ADR-019, ADR-106, ADR-109) |
| **ADR-004** | **Decisa il 1 ott 2026: opzione (a), solo asset CC0** (§ 1.4), scritta in `DECISIONS.md` con gli ADR-005…010 della M1 e della M2. Personaggi e animazioni da KayKit (Adventurers, Skeletons, Character Animations, rig `Rig_Medium`), suoni da Kenney, versi dei nemici da *80 CC0 creature SFX* di rubberduck, musica e rumori d'ambiente da OpenGameArt (dalla M7). Eccezioni: il font LiberationSans di TextMesh Pro (3 ott 2026) e, dalla M8, il font Cinzel dell'interfaccia (ADR-054), tutti e due SIL OFL 1.1 con il testo della licenza nel repo; la sprite EmojiOne (CC BY 4.0) è tolta. Dalla M10, con l'ADR-070, quattro suoni gratuiti di puadlung che non si possono ridistribuire da soli |
| **Documenti vivi** | questo piano (`docs/Piano_Sviluppo_ARPG.md`) · `docs/Da_provare.md` (le prove in build non ancora fatte) · `DECISIONS.md` (ADR-001…114) · `CONVENTIONS.md` · `ICEBOX.md` · `CREDITS.md` · `CLAUDE.md` |

Questa tabella si aggiorna a ogni chiusura di milestone (§ 6). Il dettaglio del passo corrente sta nella scheda della milestone, non qui: il piano dice *cosa* e *perché*, le schede dicono *come*.

---

## 1. Ambito

### 1.1 Il "Minimum Playable Diablo"

**Dentro (v1.0):**

- 1 classe giocante, il Guerriero
- Combattimento corpo a corpo + 3 incantesimi
- 8 livelli di dungeon generati proceduralmente: 1–4 cripta, 5–8 caverne
- 4 archetipi di nemico + 1 boss finale
- Loot casuale con prefissi e suffissi, ~20 oggetti base
- Inventario a griglia, equipaggiamento, pozioni
- Città hub con 1 mercante
- Salvataggio e caricamento
- Oscurità con raggio di luce attorno al giocatore, automappa
- Musica ambientale ed effetti sonori
- Interfaccia e nomi degli oggetti in inglese (default) e italiano, predisposti per altre lingue (da M5, decisione di Mirco del 4 ott 2026)

**Fuori, esplicitamente:** multiplayer · quest system · dialoghi e NPC multipli · crafting · altre classi (con le loro armi: vedi "Dopo la v1.0" nel § 5) · set e oggetti unici (dopo la v1.0, v2.32: vedi «Dopo la v1.0» nel § 5) · cinematiche · gamepad · achievement. I livelli di difficoltà, fuori ambito fino alla v2.37, sono tra i lavori dopo la v1.0 (v2.38): una partita finita deve poter continuare. La localizzazione, fuori fino alla v2.11, è entrata nella v1.0 con la M5.

Un'idea fuori ambito va in `ICEBOX.md`, non nel codice.

### 1.2 Il nucleo intoccabile e le linee di taglio

Un progetto solo raramente muore perché è troppo ambizioso: muore al 70%, quando il tempo finisce e non c'è niente di pubblicabile. Per questo si decide **adesso**, a mente fredda, cosa si taglia in caso di ritardo — e cosa non si taglia mai.

**Nucleo intoccabile**, senza cui non è più un gioco alla Diablo:
click-to-move con un combattimento che "si sente" · loot con affissi · inventario a griglia · dungeon procedurale · discesa verso un boss · salvataggio.

**Linee di taglio**, da applicare una alla volta e in quest'ordine:

1. **Magia (M9):** da 3 incantesimi a 1, il proiettile. Il nemico a distanza resta, perché usa lo stesso sistema di proiettili.
2. **Caverne (M7):** niente secondo algoritmo; i livelli 5–8 sono generati con BSP e si distinguono per luce e palette.
3. **Città (M10):** l'hub 3D diventa una schermata del mercante tra un livello e l'altro.
4. **Profondità:** da 8 livelli a 6.
5. **Arte (M11):** già il percorso base — nessun taglio disponibile qui, semmai si rinuncia all'estensione Blender.

### 1.3 Stime e punti di controllo

Le stime sono in **ore di sessione**: il tempo in cui Mirco lavora con Claude, che scrive il codice, mentre lui decide, prova nell'editor e in build, e rivede. Servono a dimensionare le milestone. Le ore reali non si registrano (decisione del 1 ott 2026).

**Ricalcolo del 3 ott 2026 (v2.7).** Le stime della v2.1 (240–350 h) presupponevano che il codice lo scrivesse Mirco. Con Claude che scrive il codice, M1 e M2 (stimate 32–50 h) si sono chiuse in quattro giorni di calendario. Il nuovo fattore non è uniforme:

- **circa un terzo** della stima originale per le milestone fatte soprattutto di codice e test (M2.5, M4, M5, M6, M8, M9);
- **circa metà** per quelle in cui pesano il giudizio di Mirco e il tempo passato a provare: atmosfera e luci (M3), nuovi nemici da tarare (M7), città e bilanciamento del gioco completo (M10), arte, audio e release (M11).

**Totale stimato:** circa 159–227 ore, comprese M1 e M2: è la somma delle stime della tabella del § 5. Fino alla v2.22 il totale si teneva a mano, aggiungendo le variazioni di ogni versione, e diceva 109–157 ore: non teneva più conto di alcune variazioni, tra cui la crescita della M8 nella v2.19. Alla chiusura della M7 (v2.23) si è ricalcolato dalla tabella, che da ora è l'unica fonte; la M7 stessa è salita a 18–25 h con i tre passi di correzione dopo le prove in build. Alla chiusura della M8 (v2.27) la M8 è salita a 16–22 h, per i quattro giri di correzioni dopo le prove in build, e la M9 a 10–13 h. Nella v2.28 la M10 è salita a 15–20 h con il menu iniziale, i tre salvataggi, il click destro per indossare, l'avviso dei punti da spendere, le caratteristiche colorate e i nomi degli oggetti con `Alt`. Nella v2.30 la scheda della M10 l'ha portata a 19–26 h, e itch.io è passato alla M11 (18–29 h); nella v2.31 la scheda della M11 l'ha portata a 20–30 h. Per la sola M11 ancora aperta, al 12 ott 2026, restano 20–30 ore: a 6–10 h a settimana sono 2–5 settimane di lavoro effettivo; con pause e settimane saltate, **1–1,5 mesi di calendario**.

**Punti di controllo, alla chiusura di M2 e di M5:** confronta le **settimane di calendario** dal punto di controllo precedente con la stima massima delle milestone chiuse nel frattempo, convertita a 6 h a settimana. Per M5 sono M2.5–M5, cioè 42 h, circa 7 settimane dalla chiusura della M2. Se il rapporto supera **1,5**, applica la prossima linea di taglio e ristima il resto. È una regola meccanica di proposito: la decisione di tagliare, presa da stanchi e in ritardo, non arriva mai. Le date di inizio e chiusura stanno già nella storia git e nei tag. Il punto di controllo di M2 (rapporto 0,19) è stato misurato con le stime della v2.1; quello di M5 (rapporto 0,04: 2 giorni contro circa 7 settimane) con quelle della v2.12. Quello della M8 confronta i 6 giorni dalla chiusura della M5 (5 ott) a quella della M8 (11 ott) con la stima massima di M6–M8, 66 h, cioè 11 settimane: rapporto 0,08, nessun taglio. Il prossimo, se serve, alla chiusura della M10, quando il gioco è finibile.

### 1.4 Proprietà intellettuale e licenze degli asset

**Diablo.** Ispirazione sì, riproduzione no: nessun asset, nome, testo, musica o logo originale.

**Asset di terzi in un repo pubblico.** È un vincolo che il repo pubblico rende concreto, e che arriva al passo **1.4** con il primo personaggio:

| Fonte | Nel gioco | Nel repo pubblico |
|---|---|---|
| Kenney, Quaternius, KayKit (CC0) | Sì | Sì — con credito in `CREDITS.md` |
| Freesound | Dipende dal file | Solo CC0 o CC-BY (con credito); mai licenze NC se il gioco potrebbe diventare commerciale |
| **Mixamo** | Sì, royalty free | **No** — lettura prudente, non certezza legale. Le FAQ di Adobe vietano la ridistribuzione dei file grezzi di personaggi e animazioni; se un repo pubblico ricada in quel divieto è discutibile, ma in caso di dubbio si sceglie l'interpretazione cauta |
| **Unity Asset Store** | Sì | **No.** La licenza ne vieta la redistribuzione |

Le opzioni, da fissare in **ADR-004 prima del passo 1.4**:

- **(a) Solo asset CC0**, anche per personaggi e animazioni (Quaternius ne pubblica di animati). Nessuna complessità, repo pubblico integro e clonabile da tutti. *Opzione consigliata.* Prima di decidere, mezz'ora di verifica: le animazioni CC0 disponibili coprono corsa, attacco con arma, colpo subito e morte? Se sì, questa opzione non ha svantaggi.
- **(b) Submodule Git privato:** un repo privato `DarkDescent-Licensed` montato in `Assets/_Licensed/`. Il repo pubblico mostra tutto il codice, tu e la CI clonate il progetto completo. *Sconsigliata, salvo che Mixamo risulti indispensabile:* il repo pubblico smette di essere clonabile e compilabile da chiunque altro — chi valuta il portfolio legge il codice ma non può far partire il progetto, e metà del valore di un repo pubblico è proprio quello. In più complica la CI di M2.5 (token per il checkout del submodule, PR da fork che non buildano).
- **(c) Repo privato fino al rilascio.** Semplice, ma perdi la visibilità da portfolio, che è metà dell'obiettivo.

Da quel momento ogni asset esterno entra con una riga in `CREDITS.md`: fonte, autore, licenza, link.

---

## 2. Design di gioco minimo — bozza

La v2.0 era tutta tecnica. Ma già M2 deve sapere come si calcola un colpo, e M4 quali statistiche esistono: senza una pagina di design, queste decisioni si prendono di fretta dentro il codice. Quella che segue è una **bozza**, e ogni punto indica entro quale milestone va confermato.

**Core loop.** Città → scendi → esplori e combatti → raccogli → torni in città → vendi, compri, equipaggi → scendi più in basso. *Come* si risale da un livello profondo (scale, pergamena, portale) si decide in M10.

**Il gioco non finisce** (pilastro del design, Mirco, 18 ott 2026). Battere il re non chiude la partita: con lo stesso personaggio si continua a giocare, a salire di livello e a cercare oggetti. Si può **rifare il mondo** (livelli nuovi da un seme nuovo, re compreso) quando si vuole ricominciarlo, e **cambiare difficoltà** tra quelle già raggiunte. Ogni sistema nuovo va pensato per una partita senza fine: niente che si esaurisca dopo la prima vittoria, niente che renda inutile un salvataggio. Le difficoltà dopo la v1.0 (§ 5) sono il primo passo.

**Personaggio:** quattro attributi, confermati alla **M4** (ADR-020): il cavaliere parte da 30 / 20 / 10 / 25, e la vita è 50 + 2 × Vitalità.

| Attributo | Influenza |
|---|---|
| Forza | danno fisico, requisiti delle armi |
| Destrezza | probabilità di colpire, blocco |
| Magia | mana, danno degli incantesimi |
| Vitalità | punti vita |

**Formula del colpo**, confermata alla M4 (ADR-020): probabilità di colpire = 75 + Destrezza / 2 − Armatura del bersaglio, limitata tra il 5% e il 95%; il danno è un tiro tra minimo e massimo dell'arma, moltiplicato per (1 + Forza/100) e arrotondato all'intero, almeno 1 (dalla M5: con i decimali l'ultimo colpo poteva mostrare 0).

**Colpo critico**, confermato il 6 ott 2026 (v2.17, D13 della M7). Lo fa solo il cavaliere, e solo con un colpo a segno. La probabilità è 5% + Destrezza / 10, al massimo il 50%: con la Destrezza 20 del cavaliere è il 7%. Il danno è doppio. Il nemico colpito di critico emette un verso suo, diverso per archetipo, e il numero del danno è più grande e di un altro colore. I nemici non fanno critici, come in Diablo 1.

**Archetipi di nemico**, definiti per *comportamento*, perché è il comportamento che costa lavoro, non il modello:

| Archetipo | Comportamento | Cosa insegna | Milestone |
|---|---|---|---|
| Melee base (scheletro) | insegue e colpisce | state machine, NavMeshAgent | M2 |
| Sciame | veloce, fragile, in gruppo | evitamento reciproco, spawn di gruppo | M7 |
| Bruto | lento, resistente, colpo caricato e telegrafato | telegraphing, finestre di reazione | M7 |
| A distanza | tiene le distanze e tira proiettili | posizionamento, pooling | M9 |
| **Boss** | a fasi, colpi telegrafati, evoca uno sciame | composizione dei pattern precedenti | M10 |

**Dungeon.** Livelli 1–4, *cripta*: stanze e corridoi, generati con BSP. Livelli 5–8, *caverne*: spazi organici, random walk. Boss al livello 8.

---

## 3. Stack tecnologico

| Strumento | Scelta | Perché |
|---|---|---|
| Engine | **Unity 6.3 LTS — 6000.3.24f1** | Mercato del lavoro più grande di Godot, C# che già conosci, documentazione ovunque. Versione bloccata, niente upgrade a metà (ADR-001) |
| Render pipeline | **URP 17.3.0** | Built-in è di fatto deprecato, HDRP è sovradimensionato. URP dà Shader Graph e Render Graph per il look retro |
| Input | **Input System 1.20** | Il vecchio Input Manager farebbe risparmiare mezza giornata ora e costerebbe una riscrittura dopo (ADR-002) |
| Navigazione | **AI Navigation 2.0** (`NavMeshSurface`) | Permette il bake a runtime che servirà al dungeon procedurale |
| Camera | **Cinemachine 3.1**, da M3 | Prima la camera scritta a mano in M1, per sapere cosa fa sotto |
| Test | **Unity Test Framework 1.6** | Test EditMode sulla logica pura, da M2 |
| IDE | **Visual Studio Community** (installato), Rider opzionale | Rider ha analisi statica specifica per Unity: da valutare dopo M1 |
| Version control | **Git + LFS**, repo pubblico su GitHub | Quota LFS **verificata il 3 ott 2026** in *Settings → Billing and licensing*: 10 GB di spazio e 10 GB di banda al mese inclusi, entrambi a 0 GB usati. Il repo ha 10,4 MB in LFS (24 file): un checkout completo in CI costa circa 10 MB di banda, quindi la quota regge centinaia di esecuzioni al mese. La cache LFS in CI resta comunque, perché la quota cresce con gli asset (vedi M2.5) |
| Modellazione | **Blender**, estensione opzionale di M11 | Solo se il tempo avanza: vedi la strategia asset e la stima di M11 |
| Animazioni | CC0 KayKit (ADR-004), rig Generic | Humanoid scarta la rotazione dell'osso `chest` di KayKit: si usa Generic |

### Strategia asset: grey-box prima, arte dopo

Tutto nasce con cubi e capsule grigie; l'arte arriva quando il gameplay funziona. Nella fase intermedia: Kenney, Quaternius, KayKit (CC0). Blender entra in M11, quando sai esattamente quali pezzi servono e con quali misure: modellare prima significa modellare cose che butterai.

### Trappola ricorrente: tutorial scritti per versioni vecchie

Gran parte del materiale online precede Unity 6. Quando un tutorial non torna, spesso il problema è la versione, non il tuo codice.

| Nei tutorial vecchi | Nella tua versione |
|---|---|
| `CinemachineVirtualCamera`, namespace `Cinemachine` | `CinemachineCamera`, namespace `Unity.Cinemachine` (Cinemachine 3) |
| Finestra *Navigation* con bake della scena | Componente `NavMeshSurface` (AI Navigation 2) |
| `Input.GetKey`, `Input.GetMouseButton` | Input System: azioni e callback |
| `FindObjectOfType<T>()` | Deprecato in favore di `FindFirstObjectByType` / `FindAnyObjectByType` — e comunque da evitare |
| `rigidbody.velocity` | `rigidbody.linearVelocity` |
| *File → Build Settings* | *File → Build Profiles* |

La fonte di verità è la documentazione ufficiale **per la tua versione** — su docs.unity3d.com c'è il selettore di versione — e quella del singolo package.

---

## 4. Architettura trasversale

Le milestone descrivono sistemi; questa sezione raccoglie le decisioni che li attraversano tutti. Ognuna ha il suo momento: presa prima è over-engineering, presa dopo costa un refactoring.

### 4.1 Logica pura separata dai MonoBehaviour — da M2

La logica di gioco (danno, statistiche, affissi, generazione del dungeon, salvataggio) vive in **classi C# semplici**; i `MonoBehaviour` sono un guscio sottile che la collega a Unity. Per esempio, `HealthModel` sa ricevere danno e sapere se è morto; il componente `Health` la contiene, pubblica gli eventi e parla con l'Animator.

La ragione è pratica: una classe semplice si testa in EditMode in millisecondi, senza scene e senza Play Mode. È il pattern *humble object*, e da solo rende possibile tutta la strategia di test di questo piano.

### 4.2 Come si trovano i sistemi — decisione in M2

In M2 l'HUD deve conoscere la `Health` del giocatore. Finché c'è una scena sola basta un riferimento trascinato nell'Inspector; da M3 il giocatore attraversa le scene, da M6 i livelli nascono a runtime, e i riferimenti trascinati a mano smettono di bastare. La scorciatoia che prendono tutti è un `Find` o un singleton per ogni cosa. Le opzioni vere:

- **Composition root:** un oggetto nella scena persistente (§ 4.3) che all'avvio crea e collega i sistemi. Esplicito, si legge dall'alto in basso, si debugga. *Punto di partenza consigliato.*
- **Singleton statici:** rapidi da scrivere, ma accoppiano tutto e complicano i test. Accettabili solo per servizi davvero globali e senza stato di gioco, come l'audio.
- **ScriptableObject come canali di eventi:** disaccoppiamento forte tra scene; elegante ma indiretto, e rende più difficile seguire il flusso a chi comincia. Solo dove serve davvero.

La scelta si prende **all'inizio di M2** — l'HUD non si può scrivere senza — e si documenta in un ADR alla chiusura, quando si è visto come ha retto.

### 4.3 Struttura delle scene — decisione in M3

Proposta: una scena **`Core`** sempre caricata (giocatore, camera, HUD, EventSystem, manager) e i livelli caricati **in modo additivo** sopra (`Level_Crypt_01`, poi `Town`, poi la scena vuota in cui il generatore costruisce il dungeon). Giocatore e interfaccia sopravvivono al cambio di livello senza `DontDestroyOnLoad`, e in M6 un livello procedurale è semplicemente un'altra scena da caricare. Una scena `Bootstrap` minima fa da punto d'ingresso della build.

La scelta va in un ADR prima di scrivere il `LevelManager` di M3.

**Decisa il 3 ott 2026 (ADR-014):** `Core` più livelli additivi, che diventano la scena attiva. Niente `Bootstrap` per ora: arriverà con il menu.

### 4.4 Dati: definizioni con ID stabili — da M4

La regola è già nota: **ScriptableObject = definizione immutabile, classe C# = istanza runtime.** Si aggiunge un vincolo che conviene rispettare da subito: ogni definizione ha un **ID stabile** — una stringa generata una volta e mai più cambiata, non il nome dell'asset — e le istanze runtime riferiscono le definizioni attraverso quell'ID. Un registro (`ItemDatabase`) risolve ID → definizione.

Senza questo, in M8 si scopre che un riferimento a uno ScriptableObject non sopravvive a un salvataggio su file (`JsonUtility` scrive un identificativo che cambia a ogni avvio), e l'inventario va riscritto per poter salvare la partita.

**Applicata dalla M4 (ADR-022):** `ItemDefinition` con GUID, `ItemInstance` con il solo ID, `ItemDatabase` in `Data/`.

### 4.5 Assembly definition

Oggi c'è un solo `DarkDescent.asmdef` (ADR-003). Da M2 si aggiunge `DarkDescent.Tests.EditMode`, che referenzia `DarkDescent` e il Test Framework — possibile proprio perché il codice non sta in `Assembly-CSharp`. La divisione per area (Player, Combat, Items…) si valuta in M6, sulle dipendenze reali.

**Decisa alla M6 (ADR-032):** non per area, perché tra le aree ci sono cicli veri (Combat e Items, Levels e UI), ma a strati. `DarkDescent.Core` ha logica e dati senza MonoBehaviour, `DarkDescent` i componenti; un test controlla che `Core` resti senza componenti.

---

## 5. Roadmap

Ogni milestone si chiude con una **build eseguibile** e con il rituale del § 6. Se non parte in build, non è finita.

| # | Milestone | Risultato | Ore stimate (v2.7; M1 e M2 con le stime originali) | Stato |
|---|---|---|---|---|
| M0 | Fondamenta | repo e build vuota da clone pulito | — | ✅ 23 set |
| M1 | "Mi muovo" | cammini in una stanza | 12–20 | ✅ 1 ott |
| M2 | "Colpisco e muoio" | primo gameplay loop | 20–30 | ✅ 3 ott |
| M2.5 | Pipeline automatica | test e build in CI | 2–4 | ✅ 3 ott |
| M3 | "Un dungeon fatto a mano" | due livelli, atmosfera, build Web provata | 9–14 | ✅ 3 ott |
| M4 | "Raccolgo roba" | drop, inventario, equipaggiamento | 8–12 | ✅ 4 ott |
| M5 | "Loot casuale" | affissi e rarità, blocco, lingue | 9–12 | ✅ 5 ott |
| M6 | "Dungeon infinito" | cripta procedurale, pozioni, automappa | 16–19 | ✅ 5 ott |
| M7 | "Le profondità" | caverne, nuovi nemici, colpi critici, automappa delle caverne, musica e rumori d'ambiente | 18–25 | ✅ 8 ott |
| M8 | "Progressione e persistenza" | livelli, attributi, equipaggiamento completo, salvataggio, zoom e rotazione della visuale | 16–22 | ✅ 11 ott |
| M9 | "Magia" | mana, incantesimi, nemico a distanza, vita del nemico combattuto, cintura al centro | 10–13 | ✅ 11 ott |
| M10 | "Città e loop completo" | il gioco è finibile, menu iniziale con tre salvataggi, livelli che restano com'erano | 19–26 | ✅ 12 ott |
| M11 | "Look, feel e release" | arte, audio, shader, v1.0 (itch.io dopo la v1.0, dalla v2.36) | 20–30 | ✅ 17 ott |
| M12 | "Nemici speciali" | campioni, unici, forzieri e tesoro del re; v1.1 | 9–13 | ✅ 19 ott |
| M13 | "Stanze vive" | arredi con un senso, barili da rompere, pozzi, santuari, trappole, modelli fatti in Blender, suoni da script, ComfyUI, loot ritarato; v1.2 | 22–28 | ✅ 21 ott |
| M14 | "La luce si spegne" | nove difficoltà dall'altare, mondi nuovi, gradi degli affissi, livello 50, morte che costa, numeri misurati; v1.3 | 12–16 | ✅ 23 ott |

---

### M0 — Fondamenta ✅

Chiusa il 23 settembre 2026, con la Definition of Done verificata da un clone pulito. Lezioni emerse (il devlog della M0 resta nella storia git del repo):

- **Verificare LFS su un repo senza binari non prova nulla:** si confronta il pointer nel blob con il file dopo il checkout. Il primo binario vero arriva al passo 1.4.
- **L'asmdef va creato prima del codice:** un assembly con `.asmdef` non può referenziare `Assembly-CSharp`. Per questo la classe generata dall'Input System va generata sotto `Scripts/` (ADR-003).
- **"Line Endings For New Scripts" su Unix non basta:** serve la regola `eol=lf` in `.gitattributes` per ogni nuovo tipo di file testuale (aggiunta per `*.asmdef` e `*.inputactions`).
- **Residuo del template:** `Assets/InputSystem_Actions.inputactions`, registrato come *Project-wide Actions*. Si stacca da *Project Settings → Input System Package* e si cancella al passo 1.2.

---

### M1 — "Mi muovo" ✅

Chiusa il 1 ottobre 2026: build Windows provata a mano, tag `m1`. Lezioni emerse:

- **I passi nell'editor si possono fare in batchmode.** Script di editor usa e getta lanciati con `-executeMethod`, verifiche con test PlayMode temporanei su `InputTestFixture` (mouse virtuale), poi rimossi. Trappola: in batchmode un errore di compilazione blocca l'import degli asset, quindi non si può usare una classe generata prima di averla generata.
- **KayKit va importato Generic, non Humanoid:** Humanoid non mappa l'osso `chest` e ne scarta la rotazione in ogni clip. Tutto KayKit condivide `Rig_Medium`, quindi il retargeting non serve.
- **NavMesh:** il bake va limitato ai layer dell'ambiente (altrimenti il player si ritaglia un buco), serve la height mesh (altrimenti si galleggia di 8 cm), e gli ostacoli vanno marcati *Not Walkable* per non creare isole sulle loro facce superiori.
- **Il raggio del click deve fermarsi anche sugli ostacoli,** non solo sul pavimento: altrimenti un click su un cubo porta il player alle sue spalle.
- **LFS verificato davvero** con i primi binari: nel blob c'è il pointer, su disco il file.

---

### M2 — "Colpisco e muoio" ✅

Chiusa il 3 ottobre 2026: 11 test EditMode e 36 PlayMode verdi, build Windows senza warning. Lezioni emerse:

- **Il combattimento è un componente solo, `MeleeAttack`, per player e nemici:** cambia chi sceglie il bersaglio. `SetTarget` chiede un colpo; per colpire di continuo lo si richiama. Il danno parte dopo il ritardo dell'arma, misurato campionando la traiettoria della lama nella clip, e al momento del colpo si ricontrolla tutto.
- **Presentazione e logica in un verso solo:** driver delle animazioni, lampo, suoni e numeri si iscrivono agli eventi di `Health`, `MeleeAttack` e `HitRecovery`. Il combattimento non sa niente di come viene mostrato.
- **L'ordine degli `Awake` tra oggetti non è garantito:** `Health` crea il suo model al primo accesso, e i componenti con `Bind` si iscrivono quando arrivano per secondi tra `Bind` e `OnEnable`.
- **Il null "finto" di Unity non si vede attraverso un'interfaccia:** `IDamageable.IsAlive()` fa il cast a `UnityEngine.Object` prima del confronto.
- **Quello che funziona nell'editor può sparire in build:** l'emissione non usata da nessun materiale viene tolta dallo shader, per questo il lampo scambia il materiale. E `timeScale` sopravvive al caricamento della scena.
- **Batchmode:** Unity cancella `Temp/` all'uscita; un `RequireComponent` aggiunge componenti già al caricamento del prefab; `AssetDatabase.ImportPackage` non finisce prima di `-quit`, serve `-importPackage`; gli script di editor vedono TextMeshPro, gli asmdef del gioco no.
- **Punto di controllo superato:** 1,5 settimane contro 8 stimate. Le stime in ore precedono la scelta di far scrivere il codice a Claude e da qui sovrastimano.

---

### M2.5 — Pipeline automatica ✅

Chiusa il 3 ottobre 2026: test e build Windows in CI, tag `m2.5` con la sua build. Lezioni emerse:

- **La licenza Personal in CI passa da email e password,** non più dal file `.ulf`. Un account Unity dedicato alla CI tiene fuori da GitHub la password dell'account principale (ADR-011).
- **Le action di GameCI scaricano l'ultima CLI anche quando sono fissate:** va fissata anche `cliVersion`. Il primo run è morto in 4 secondi per un'incompatibilità tra action e CLI (`--no-coverageEnabled`): la copertura si spegne con `GAME_CI_COVERAGE_ENABLED` (ADR-012).
- **Un gruppo di `concurrency` solo per tutto il repo,** senza annullare i run in corso: i posti della licenza sono pochi e un job ucciso a metà rischia di non restituirlo.
- **I PlayMode girano su Linux in Docker senza modifiche,** e le cache di LFS e `Library/` portano un run da circa 6 a circa 4 minuti; la build Windows riparte dalla `Library/` dei test.
- **Sui run rossi la CLI non crea il riepilogo dei risultati:** il nome del test fallito si legge nel log o nell'XML tra gli artifact.

---

### M3 — "Un dungeon fatto a mano" ✅

Chiusa il 3 ottobre 2026: 18 test EditMode e 58 PlayMode verdi, build Windows del tag `m3` dalla CI, build Web provata nel browser. Lezioni emerse:

- **La scena attiva decide luci e ambiente:** dopo il caricamento additivo il livello va reso attivo, altrimenti prende il cielo di `Core`. Il colore ambiente si scrive in gamma e diventa lineare: 0,12 vale 0,013, cioè nero pieno (ADR-014, ADR-016).
- **Una luce ripida illumina il cimiero e non il resto:** la luce del cavaliere sta alta e spostata verso la camera, e una seconda luce senza ombre illumina solo il rendering layer `Player`. Così il cavaliere si vede di fronte senza schiarire la stanza.
- **Mappe di testo e uno strumento che resta nel repo** (ADR-015): i livelli si rivedono in un diff, e alla M6 il generatore dovrà solo produrre la stessa griglia. Le misure del tileset si prendono all'import: in KayKit `wall_half` è un muro corto, non basso, e il muro basso è `barrier`.
- **Trigger e click hanno bisogni diversi:** un player mosso dall'agent fa scattare i trigger solo se c'è un `Rigidbody` cinematico, e il raggio del click ignora i trigger. Per questo la scala ha un'area cliccabile separata.
- **Gli shader si compilano al primo uso:** nell'editor la prima evidenziazione esce azzurra (segnaposto), in batch il primo fotogramma emissivo esce magenta; in build non succede. URP toglie `_EMISSION` da un materiale i cui flag di illuminazione globale non sono emissivi.
- **Unity non ha un'API pubblica per creare un `AudioMixer`:** lo script di editor è passato per la reflection.
- **Il Web regge il dungeon** (ADR-019) se usa la stessa qualità *PC*: il livello *Mobile*, assegnato di default al Web, toglieva ombre e Forward+. La pubblicazione su itch.io è passata alla M10 (ADR-018).

### M4 — "Raccolgo roba" ✅

Chiusa il 4 ottobre 2026: 62 test EditMode e 77 PlayMode verdi, build Windows della CI provata da Mirco sullo scenario della Definition of Done. Lezioni emerse:

- **La casualità si inietta prima di introdurla** (ADR-021): con una sorgente fissa i test che contano i colpi sono rimasti com'erano. Allentarli avrebbe tolto proprio quello che verificano.
- **Una statistica si toglie per sorgente, non per valore** (ADR-020): ogni modificatore ricorda l'oggetto che l'ha messo, e togliere l'oggetto riporta le statistiche esattamente a prima. È il test che il piano chiedeva, e regge anche per gli affissi della M5.
- **I modelli KayKit hanno il perno sull'impugnatura:** a terra vanno centrati sull'oggetto, altrimenti click, etichetta e punto d'arrivo cadono fuori dal modello. In mano ogni oggetto ha una posizione sua: lo scudo, agganciato all'osso della mano, stava dalla parte sbagliata e lasciava vedere la mano finché non è stato spostato verso l'esterno (riscontro di Mirco).
- **La classe generata dall'Input System va rigenerata prima di usare le azioni nuove:** con un errore di compilazione l'import non parte, quindi in batch servono due passaggi (trappola della M1).
- **Nella UI conta quando arriva l'evento:** il click di uGUI arriva al rilascio, quindi prendere un oggetto usa la pressione. Una finestra spenta non riceve l'uscita del cursore, e chi la chiude deve dimenticare cella e slot sotto il cursore. Un fondo trasparente acceso solo con un oggetto preso impedisce al click di arrivare al mondo.
- **Le icone si fanno dai modelli** (ADR-024): un oggetto nuovo ha l'icona con un click, ma ogni modello ha il suo verso, e lo scudo fotografato da davanti mostrava il retro.
- **`git mv` di un asset tiene il GUID ma non il nome interno:** `m_Name` va corretto, altrimenti l'Inspector e i log mostrano il nome vecchio.

### M5 — "Loot casuale" ✅

Chiusa il 5 ottobre 2026: 98 test EditMode e 89 PlayMode verdi, build Windows della CI provata da Mirco con `-seed 4711`. Lezioni emerse:

- **Le lingue vanno per prime** (ADR-025): ogni testo nuovo della milestone è nato con la sua chiave. I test che confrontavano frasi italiane sono passati all'inglese, con la preferenza della lingua cancellata prima di ogni scena; un test di copertura trova le chiavi che mancano prima che lo faccia un giocatore.
- **Una prova statistica va dimensionata sulla varianza:** su 10.000 estrazioni il 65% dei normali è uscito 63,9%, a 2,4 deviazioni standard, e la tolleranza dell'1% non reggeva. Con 100.000 la deviazione scende a 0,15 punti.
- **Il seme va legato a qualcosa che non cambia** (ADR-029): la cella del nemico, presa in `Awake`, rende il drop indipendente dall'ordine delle uccisioni. Il `Preview` del drop permette ai test di cercare il seme che fa cadere l'oggetto che serve, invece di scriverlo a mano.
- **Il danno con la virgola si vede solo in build** (ADR-031): nei test i tiri fissi davano valori tondi, e il "0" all'ultimo colpo l'ha trovato Mirco giocando.
- **In color space lineare la trasparenza della UI schiarisce molto:** uno sfondo al 30% sembra quasi pieno, e i colori delle celle vanno scelti scuri e controllati in foto.
- **Due componenti uguali nella scena rendono ambiguo `FindFirstObjectByType`:** con il secondo tooltip del confronto, il pannello espone i suoi due riferimenti ai test.
- **Le asserzioni sui testi colorati** cercano il testo tra i tag (`>Lama dello scheletro<`) o la costante del colore, non la frase intera.

### M6 — "Dungeon infinito" ✅

Chiusa il 5 ottobre 2026: 132 test EditMode e 103 PlayMode verdi, build Windows della CI provata da Mirco. Lezioni emerse:

- **Un refactor che sposta codice si verifica con una descrizione, non a occhio** (ADR-033): prima di portare il builder a runtime, uno script ha descritto le due scene a mano oggetto per oggetto; ricostruite, la descrizione era identica riga per riga.
- **Il bake dalle mesh renderizzate funziona nell'editor e viene vuoto in build** (ADR-035): i modelli senza Read/Write non si leggono dalla CPU. L'ha segnalato Unity al primo test del builder, prima di arrivare in build. Il bake dai collider costa 8–16 ms a livello.
- **I messaggi delle asserzioni si compongono anche quando passano:** il test sui 500 semi impiegava cinque secondi e mezzo per una mappa stampata a ogni controllo. Composto solo quando serve, mezzo secondo.
- **Un seme per dominio rende gratuite le novità casuali** (ADR-037): le pozioni hanno un tiro e un seme loro, e gli oggetti del seme 4711, già provati in build, sono rimasti identici.
- **Lo stato che deve sopravvivere a un cambio di scena si evita, non si salva** (ADR-036): "Ricomincia" non ricarica più `Core` ma solo il livello, e una richiamata a schermo nero rimette vita e inventario. Niente singleton né `DontDestroyOnLoad`.
- **Il grafo delle dipendenze si ricava dai tipi usati, non dagli `using`** (ADR-032): per cartelle era un unico ciclo, ma il codice di logica non usava mai componenti di scena. La divisione giusta era per strati, non per aree.
- **L'ordine nella gerarchia della HUD decide chi prende il click:** la cintura sotto il fondo trasparente dell'inventario perdeva i click con un oggetto sul cursore.
- **Una GIF con la camera che segue costa il triplo:** a ogni fotogramma cambia quasi tutto lo schermo. Quella della M6 è a 10 fotogrammi al secondo e 640 × 360, con il tragitto verso la scala accelerato.

### M7 — "Le profondità" ✅

Chiusa l'8 ottobre 2026: 181 test EditMode e 137 PlayMode verdi, tre build della CI provate da Mirco. Lezioni emerse:

- **Le prove in build hanno cambiato la milestone più dei test:** tre giri di correzioni (D14–D16 della scheda), tutti su cose che nessun test poteva chiedere: lo sciame che bloccava, la morte che toglieva gli oggetti, i passi troppo svelti, le armi tutte uguali. La stima è salita da 13–20 a 18–25 h per questo. Nelle prossime schede un passo di correzioni dopo la prima build va messo in conto da subito.
- **Una regola giusta da sola diventa una trappola alla frequenza sbagliata** (ADR-049): il blocco che interrompe il fendente (ADR-026) era innocuo contro uno scheletro; contro sei dello sciame scattava una volta al secondo e il cavaliere non colpiva più. Il conto si fa con il numero di nemici che ci saranno, non con uno.
- **Un test che dipende dal seme della partita passa per caso:** `CaveDescentTests` contava ancora i gruppi di sciame vecchi e in locale è passato, perché il seme cambia a ogni esecuzione; in CI è caduto. I test sui livelli generati o fissano il seme o controllano tutti i casi possibili.
- **L'IA a stati ha ripagato alla seconda modifica** (ADR-042): il ritorno a casa (ADR-050) è stato una classe nuova e quattro membri dell'interfaccia del corpo, provato prima in EditMode con il corpo finto.
- **Un tiro nuovo in un flusso condiviso sposta tutti quelli dopo** (trappola 12): i colpi leggeri del bruto aggiungono un tiro all'inizio del colpo, e i test con i tiri fissi hanno cambiato esito. Un dominio nuovo vuole un flusso suo, come per le pozioni (ADR-037).
- **Il suono va misurato sull'animazione, non stimato:** i passi a distanza fissa andavano più svelti dei piedi. Campionando l'altezza dei piedi nella clip si sono trovati i due appoggi (12% e 62% del ciclo), e i passi seguono l'animator.
- **Cambiare l'intonazione di un `AudioSource` cambia anche i suoni già partiti:** versi e passi hanno sorgenti loro, separate da quella dei colpi.
- **Quando un totale si tiene a mano, prima o poi diverge:** le stime del § 1.3 sommavano le variazioni versione per versione e erano rimaste indietro di 30–50 ore. Ora il totale si ricalcola dalla tabella.

### M8 — "Progressione e persistenza" ✅

Chiusa l'11 ottobre 2026: 217 test EditMode e 148 PlayMode verdi, tre build della CI provate da Mirco. Lezioni emerse:

- **La lezione della M7 non era entrata nella scheda, e le correzioni sono arrivate lo stesso:** quattro giri di note dopo le prove in build (ascia al contrario, font, danno che non cresceva, gioielli senza niente, nessun riscontro equipaggiando, armature squadrate, "Colpire (scheletro)", troppo loot). La M8 è salita da 12–18 a 16–22 h. La scheda della M9 ha un passo di correzioni dopo la prima build già nella tabella.
- **Simulare prima di tarare:** una simulazione della discesa con i nemici veri di ogni profondità ha mostrato che l'esperienza proposta portava al livello 4 invece del 9, prima di qualsiasi prova in build (ADR-059).
- **Un salvataggio vero, congelato prima di cambiare formato,** è l'unico test di migrazione che vale: uno scritto a mano somiglia al formato nuovo più di quello vecchio (ADR-062).
- **Un font nuovo rompe le scritte dove nessun test guarda:** Cinzel, più largo, ha mandato a capo "Mancato" e i nomi a terra. Un test temporaneo che apre ogni schermata nelle due lingue e controlla ogni testo (parole spezzate, testo fuori dal riquadro, caratteri mancanti) li ha trovati tutti in un giro, su circa 28.000 testi.
- **Lo stile viene dallo stesso pacchetto:** armature e stivali presi dai pezzi dei personaggi KayKit stanno bene accanto al cavaliere; quelli di un pacchetto diverso, anche CC0 e low-poly, sembravano squadrati (ADR-056). I personaggi KayKit guardano verso +Z: le icone li vogliono girati di 180°.
- **Un test che cerca un seme deve dire tutto quello che vuole:** abbassando la pozione, il primo seme con un oggetto la faceva cadere insieme; con meno armi, un altro dava un anello che entrava nell'inventario "pieno". E una probabilità salvata come float (0,3) cade dalla parte sbagliata contro lo stesso numero in double.
- **In batchmode `WaitForEndOfFrame` non arriva:** i test temporanei per gli screenshot usano `yield return null`, o si bloccano.

### M9 — "Magia" ✅

Chiusa l'11 ottobre 2026: 242 test EditMode e 166 PlayMode verdi, una build della CI provata da Mirco. Lezioni emerse:

- **Il passo di correzioni in tabella ha retto:** dopo la prova, quattro correzioni (mago e sciame che morivano al primo colpo, il Dardo a raffica, le scritte troppo grandi) in un giro, senza far salire la stima. Le richieste che erano funzioni nuove sono andate nel piano (M10, v2.28) invece di gonfiare la milestone in corso.
- **La vita di un nemico va ricontrollata quando cresce il danno del cavaliere:** lo sciame, tarato alla M7, con il danno da Forza e livello della M8 moriva al primo colpo. Per ogni nemico si conta quanti colpi servono alla profondità dove compare, con l'equipaggiamento che il cavaliere ha lì.
- **Un oggetto in mano a un modello si guarda da vicino, ben illuminato:** il bastone del mago è stato sbagliato tre volte (la radice dei modelli KayKit ha scala 100, poi il verso, poi il teschio dietro). Le misure erano vecchie perché l'animator dei nemici usa `CullUpdateTransforms`: nei test per gli screenshot va `AlwaysAnimate`.
- **I collider non bastano per i muri che la camera spegne:** un raggio sul NavMesh, cotto con tutti i muri accesi, li vede tutti (ADR-065).
- **`Math.Round` arrotonda al pari:** il Dardo con la Magia 10 faceva 8–14 invece di 9–15. Dove il valore è mostrato al giocatore si arrotonda lontano dallo zero.
- **Un test che conta lo stato del mondo dopo un click dipende da dove cadono le cose:** la cassa "aperta due volte" contava gli oggetti a terra, e il secondo click a volte ne raccoglieva uno. Si conta l'evento che interessa, non quello che resta sul pavimento.
- **In CI Unity si può fermare all'avvio senza errori:** la build va seguita con un limite di tempo e annullata, invece di aspettare i 60 minuti del timeout.


### M10 — "Città e loop completo" ✅

Chiusa il 12 ottobre 2026: 291 test EditMode e 192 PlayMode verdi, due build della CI provate da Mirco. Lezioni emerse:

- **Le prove in build trovano anche le scelte sbagliate, non solo gli errori:** il portale che riportava all'ingresso e i livelli che rinascevano erano scritti così apposta (D6, ADR-061). Solo giocando il giro città–dungeon–città si è visto che toglievano senso al ritorno e permettevano di rifare l'oro all'infinito. La correzione ha ribaltato un ADR della M8 (ADR-079) e ha portato un formato del salvataggio in più. Il passo di correzioni in tabella ha retto anche questo.
- **Un dominio del seme per ogni tiro nuovo** (oro, pergamena del ritorno, assortimento del mercante) ha tenuto validi per tutta la milestone i test che cercano un seme preciso: nessun test di loot da riscrivere.
- **Un test che parte da una partita nuova eredita un seme casuale:** il menu non lo fissa, e un test del portale che chiedeva un punto a 12–20 m dall'ingresso è passato in locale e caduto in CI. Un test che genera un livello deve valere per qualunque livello, o fissare il seme.
- **L'ordine degli ascoltatori di un evento conta:** il salvataggio fatto entrando in un livello può arrivare prima che il livello sia rimesso com'era, e avrebbe sovrascritto il ricordo vero. Chi dipende dall'ordine va chiamato in modo esplicito dal composition root, non messo in fila sullo stesso evento.
- **Gli operatori `??` e `?.` di C# non vedono gli oggetti distrutti di Unity:** un componente distrutto non è `null` per loro. Con gli oggetti di Unity si confronta con `== null`, o si usa un metodo che non restituisce un oggetto distrutto.
- **Il menu nella scena Core, senza una scena a parte, ha reso semplici i test:** un test apre il menu, sceglie un posto e gioca, tutto nella stessa scena, con una cartella dei salvataggi temporanea da una variabile d'ambiente.


### M11 — "Look, feel e release" ✅

Chiusa il 17 ottobre 2026 con il tag `v1.0.0`: 310 test EditMode e 199 PlayMode verdi, due build della CI provate da Mirco. La pubblicazione su itch.io è passata a dopo la v1.0 (v2.36). Lezioni emerse:

- **Un effetto di gusto va messo dietro un'opzione:** il filtro retrò, deciso sulla carta e acceso di partenza, nella prova è piaciuto meno della luce senza filtro. Essendo un'opzione, è bastato cambiare la scelta di partenza (ADR-081).
- **Prima di usare un'opzione di uno strumento esterno se ne legge il codice:** `versioning: Tag` di GameCI sembrava fatta apposta, e con due tag sullo stesso commit avrebbe dato una versione rotta proprio alla release (ADR-087).
- **Il renderer software della CI non è una scheda vera:** un test che legge i pixel di un effetto a tutto schermo passava ovunque tranne che su llvmpipe. Il test che si ignora dice nel motivo quello che serve a capire il perché, e ha fatto capire il resto al giro dopo (ADR-083).
- **Un ostacolo nuovo va controllato contro tutto quello che si usa dal davanti:** gli arredi lasciavano liberi passaggi e scala, ma non la cella davanti alle casse, e un test dell'oro è caduto in CI con un seme casuale. Le colonne avevano già la regola: andava data a tutti gli ostacoli insieme.
- **Un flusso del seme per gli arredi ha tenuto fermo il resto:** tre giri di arredi (cripta, caverne, colonnati) senza cambiare nemici, casse e mappe dei semi già provati, e i test lo controllano togliendo gli arredi.
- **Le misure dei modelli si prendono dai contorni, non dal nome:** macerie lunghe 8 m, casse impilate da 3,5 m, modelli di Quaternius di pochi millimetri con una scala di 100. Ogni composizione si è costruita misurando, con una foto di prova renderizzata dall'editor.
- **Un benchmark che gioca da solo trova anche quello che nessuno cercava:** il vSync spento, con la GPU al massimo per 700 fps, è uscito dalla prima misura in build.
- **Un test che aspetta un tempo fisso cade quando la macchina è carica:** la reazione al colpo, guardata dopo 0,15 s esatti, è caduta una volta nel giro completo. Si aspetta l'evento a ogni frame, con un limite.
- **Un gruppo di concorrenza della CI tiene un solo run in attesa:** due build lanciate insieme ne lasciano una sola. Si lanciano una dopo l'altra.

---

### M12 — "Nemici speciali" ✅

Chiusa il 19 ottobre 2026 con il tag `v1.1.0`: 340 test EditMode e 212 PlayMode verdi. La build di prova è saltata su scelta di Mirco. Lezioni emerse:

- **Promuovere quello che c'è costa meno che aggiungere:** i campioni sono nemici già messi, e la mappa di un seme già provato è cambiata solo delle scorte. Lo stesso per il loot: il primo tiro resta, il resto ha semi suoi (ADR-089, ADR-092).
- **Una tinta per renderer resta anche quando il materiale cambia:** il lampo del colpo prendeva il colore di Morvak. Ogni scambio di materiale al volo va guardato contro chi tinge (ADR-090).
- **Una rilettura vale una prova, quando la prova salta:** senza la build di prova, rileggere tutto il codice della milestone ha trovato il lampo colorato, che nei test non si vedeva.
- **Le stanze piccole hanno sempre meno posto di quanto si pensi:** l'unico in una stanzetta della cripta piena di arredi e di scorta non trovava dove mettere il forziere. La ricerca si allarga, e un test conta quante volte manca.
- **Un modello si guarda prima di sceglierlo per un ruolo:** `trunk_large` non ha un coperchio da aprire, e `chest_gold` chiuso sembra la cassa di sempre. Le foto di prova l'hanno detto prima di Mirco.
- **Le foto della UI vanno costruite apposta:** la tela in overlay non finisce nella texture della camera, e il lampo dura meno di due frame in batchmode.

---

### M13 — "Stanze vive" ✅

Chiusa il 21 ottobre 2026 con il tag `v1.2.0`: 375 test EditMode e 226 PlayMode verdi. La build di prova è saltata su scelta di Mirco, e al suo posto la rilettura del codice. Lezioni emerse:

- **Ogni cosa nuova nel livello prende un dominio suo del seme:** barili, pozzi, santuari e trappole sono entrati senza spostare un nemico o una cassa dei semi già provati; quello che si sceglie per cella viene da un hash, non da un tiro (ADR-097).
- **Un tiro conta quando diventa una cosa:** il pozzo tirato dalla stanza di partenza, che non lo prende, lasciava il livello senza pozzo. I limiti si contano su quello che è stato messo.
- **Uno stato a metà va ricordato per com'è:** il barile con la miccia accesa era già "rotto" per il ricordo, senza esplosione né loot (ADR-098).
- **Misurare prima di tagliare:** la stima a mano del loot era sbagliata di un terzo; con i valori attesi sui livelli veri Mirco ha scelto i tagli guardando i numeri (ADR-100).
- **Un bersaglio che non è un nemico tocca più codice del previsto:** cerchio, raggio dell'attacco, click tenuto premuto, ricordi e magie andavano guardati uno per uno; la piastrella del pavimento nascondeva il cerchio, che è salito di 5 cm.
- **Modelli e suoni da script passano dal giudizio di Mirco prima di Unity:** la foto accanto a un modello KayKit e la pagina di ascolto hanno fermato le prove venute male prima dei prefab, e i semi di Stable Audio si scelgono misurando inviluppo e attacco.
- **Un test che salta due volte va riprodotto, non chiamato instabile:** il portale "lento" del passo 13.5 era un portale irraggiungibile in un angolo tra i barili; il seme della partita nel log della CI l'ha rifatto uguale in locale. E il tag parte solo dopo aver letto l'esito dei test, non solo la fine del run (v1.2.1).
- **Il NavMesh ritagliato sposta i warp dei test:** un nemico messo accanto a un barile può finire 3 m più in là; i test cercano un punto vicino che il NavMesh accetta davvero.

---

### M14 — "La luce si spegne" ✅

Chiusa il 23 ottobre 2026 con il tag `v1.3.0`: 411 test EditMode e 242 PlayMode verdi. Mirco non ha potuto provare la build: al suo posto la rilettura del codice, e le prove restano in `docs/Da_provare.md` per la prossima build. Lezioni emerse:

- **Misurare prima di tarare, anche il combattimento:** i numeri indicativi facevano arrivare il cavaliere dieci livelli sopra la zona e il re durava il doppio del previsto. Con i valori attesi delle formule del gioco, nove gradini si tarano guardando le fasce (ADR-105).
- **La formula di Diablo 1 si satura:** colpire e Armatura vanno tarati sul colpire grezzo, senza gli affissi del cavaliere, che cambiano troppo da un seme all'altro; e il tetto del blocco al 75% decide da solo quanto colpiscono i nemici delle difficoltà alte.
- **Un moltiplicatore in un punto solo, e ogni fonte di danno passa da lì:** campione e difficoltà si compongono in `DamageMultiplier`, ma l'anello di fuoco del re aveva un danno suo e restava uguale all'Abisso (ADR-102).
- **Gli strumenti di prova si provano dalla strada della build:** `-difficulty` e `-herolevel` funzionavano nei test, che partono con `-newgame`, ma nelle build c'è sempre il menu, e il menu non le leggeva. L'ha trovato la rilettura, non un test.
- **L'ordine di due eventi è un contratto:** annunciare il mondo nuovo prima del cambio di difficoltà faceva rifornire il mercante con le rarità di prima. Un test ora tiene l'ordine.
- **Asset con id propri tengono fermi i salvataggi:** i gradi nuovi sono asset nuovi e quelli di prima tengono gli id, così nessun oggetto salvato è cambiato, anche se lo stesso seme ora fa un oggetto diverso (ADR-101).
- **Prima della migrazione, una fixture vera del formato vecchio:** il salvataggio del formato 5 è stato congelato da una partita vera prima di toccare il codice, e la migrazione al 6 si prova su quello.
- **Una lista delle prove saltate:** tre milestone di fila senza build provata lasciavano le prove sparse tra schede e memoria. Ora stanno in `docs/Da_provare.md`, che si svuota alla prima build provata.

---

### M15 — "La ronda notturna" ✅

Chiusa il 24 ottobre 2026 con il tag `v1.3.1`: 430 test EditMode e 244 PlayMode verdi a ogni push, più 5 EditMode e 12 PlayMode della ronda, verde la prima notte pianificata. Lezioni emerse:

- **I semi fissi vedono sempre gli stessi livelli:** la prima ronda ha trovato all'8 un mondo senza re e pavimenti uniti solo in diagonale attorno alla sala; con 20000 mappe a notte, le sale del re senza porta. Nessuno di questi era nei semi dei test di ogni push (ADR-106).
- **Una partita lunga trova quello che i test brevi non vedono:** NavMesh e mesh si accumulavano a ogni livello, senza un errore e quasi senza memoria in più. L'ha trovato una partita di sei mondi che conta gli oggetti per tipo (ADR-108).
- **Un controllo che non trova niente da provare passa sempre:** il riassunto della ronda conta i pezzi davvero provati (barili, portali, casse), così una parte vuota si vede.
- **Una correzione si prova su più notti:** il primo rimedio alla sala del re senza porta, riempire come attorno all'anello, chiudeva la sala o staccava mezza caverna su mappe di altre notti; scavare no.
- **Le soglie dei controlli si tarano sulle mappe vere:** su 120000 mappe una aveva il 24,85% di pavimento e una la scala a 22 passi su 46. Si allarga la soglia e la mappa resta come test, invece di cambiare il generatore senza un difetto.
- **Più campioni prima di decidere:** con 32 equipaggiamenti la misura delle difficoltà cadeva ogni notte in un punto diverso; con 256 restava una cosa sola, vera (ADR-107).
- **La coda delle Actions tiene un solo run in attesa:** una ronda lanciata a mano dietro un push ha annullato i test di quel push. E il cron di GitHub può partire ore dopo: la prima notte pianificata è partita alle 7:35 italiane invece che alle 3.
- **Nel container di GameCI le variabili d'ambiente non arrivano:** il seme e il percorso del rapporto passano sulla riga di comando di Unity.

---

### M16 — "Tesori con un nome" ✅

Chiusa il 26 ottobre 2026 con il tag `v1.4.0`: 466 test EditMode e 252 PlayMode verdi a ogni push, più 6 EditMode e 12 PlayMode della ronda. La build è fatta dalla CI ma non è stata provata: le prove restano in `docs/Da_provare.md`. Lezioni emerse:

- **Una tabella scritta a occhio va misurata prima di uscire:** approvata nel 16.1, aveva otto unici su dodici e i due set sotto il raro della stessa base, quasi tutti ai livelli alti, dove i rari salgono con i gradi degli affissi. Le probabilità della D7 davano 2,8 a discesa all'ultimo Abisso, sotto la fascia (ADR-113).
- **Anche la misura va controllata:** anelli e amuleti "normali" nascono magici, e il confronto partiva da un amuleto con i suoi affissi; l'Ultima Candela risultava sotto zero. Il punto di partenza ora è l'oggetto nudo.
- **Pochi campioni ingannano anche qui:** con 8 equipaggiamenti il Sigillo della Cripta sembrava a 1,5 volte il raro, con 64 era a 1,26. Come per la M15, prima di tarare si allarga il campione.
- **Salvare i valori, non solo il seme:** la taratura ha cambiato le tabelle a metà milestone senza toccare un salvataggio (ADR-111).
- **Il tiro a parte tiene fermo il resto:** con un dominio suo del seme il loot dei semi già provati non è cambiato, e i test e la ronda non hanno dovuto riscrivere niente (ADR-110).
- **Una copia nel gioco va tenuta da un test:** la riga nuova di `CREDITS.md` non era passata in `Data/Credits.txt`, e la CI del 16.3 è caduta lì.
- **Un raggio libero non è un punto raggiungibile:** la ronda della prima notte con la M16 ha trovato un portale aperto oltre una cassa, perché `NavMesh.Raycast` attraversava il buco ritagliato dalla cassa. Il posto del portale ora vuole il NavMesh sotto e un cammino completo.

---

### M1 — "Mi muovo" · 12–20 h

**A schermo:** una stanza grigia con ostacoli; clicchi, il personaggio ci arriva animato aggirando gli ostacoli, la camera isometrica lo segue morbida.

**Contenuto:** scena sandbox e camera scritta a mano · Input System con classe C# generata · NavMesh e click-to-move · personaggio animato con blend tree · architettura in quattro componenti (`PlayerInputReader`, `PlayerMotor`, `PlayerController`, `PlayerAnimatorDriver`) più `CameraFollow`. Dettagli, scheletri e trappole: `docs/milestones/M1_Mi_Muovo.md`.

**ADR-004 decisa il 1 ott 2026: opzione (a), solo CC0.** Personaggio e animazioni da KayKit, rig Generic; la scheda M1 è allineata.

**Definition of Done:** in build, clicchi ovunque nella stanza e il personaggio ci arriva aggirando gli ostacoli, animato, senza scatti della camera.

**Per il CV:** ciclo di vita dei componenti, Input System, NavMesh, Animator.

---

### M2 — "Colpisco e muoio" · 20–30 h

**A schermo:** clicchi su uno scheletro, il personaggio si avvicina e attacca; lo scheletro reagisce, ti insegue, ti colpisce. Uno dei due muore. Una sfera rossa mostra la vita.

Chiude il **primo gameplay loop**: da qui in poi migliori un gioco, non ne costruisci uno.

**Contenuto:**

- `DamageInfo` come struct e interfaccia `IDamageable`. Una struct, non `float + GameObject`: quando arriveranno critici, elementi e knockback nessuna firma dovrà cambiare.
  ```csharp
  public readonly struct DamageInfo
  {
      public readonly float Amount;
      public readonly DamageType Type;
      public readonly GameObject Source;
      public readonly bool IsCritical;
  }

  public interface IDamageable
  {
      void TakeDamage(in DamageInfo info);
      bool IsDead { get; }
  }
  ```
- `HealthModel` in C# puro e componente `Health` che la avvolge (§ 4.1), con gli eventi `OnHealthChanged` e `OnDied`
- Targeting: click su un nemico → avvicinamento → attacco
- IA nemica con `enum` e `switch` (`Idle`, `Chase`, `Attack`, `Dead`). Con un nemico solo, una state machine a classi è over-engineering: il refactoring arriva in M7, quando i tipi di nemico saranno tre
- Danno applicato dopo un ritardo configurato sull'arma, non con un Animation Event, che si perde a ogni reimport della clip
- HUD: la sfera della vita, iscritta agli eventi; quella del mana arriva con la magia in M9
- Schermata di morte con possibilità di ricominciare
- **Game feel:** flash bianco sul bersaglio colpito (0,1 s), hit stop di 0,05 s, numeri di danno fluttuanti ed **effetti sonori provvisori** (fendente, impatto, morte). L'audio fa metà della sensazione d'impatto: non va rimandato alla fine

**Decisione da prendere:** come si trovano i sistemi (§ 4.2).

**Scheda operativa:** `docs/milestones/M2_Colpisco_e_muoio.md`, con le decisioni D1–D4 da prendere prima di cominciare.

**Test:** primo assembly di test (§ 4.5). Su `HealthModel`: il danno riduce la vita, a zero la morte scatta una volta sola, la vita non va sotto zero, dopo la morte non si subisce altro danno.

**Definition of Done:** in build, sopravvivi a tre scheletri o muori provandoci, e dalla schermata di morte puoi ricominciare. Test verdi.

**Punto di controllo** (§ 1.3).

**Per il CV:** interfacce ed eventi in C#, state machine, testabilità, game feel.

---

### M2.5 — Pipeline automatica · 2–4 h

Una milestone piccola, ma con un posto preciso: nella v2.0 la CI stava "verso M3–M4", cioè da nessuna parte. Ora esistono i primi test, e la CI ha qualcosa da verificare.

**Contenuto:**

- GitHub Actions con **GameCI**, con la licenza Unity Personal attivata tramite i secrets del repo (email e password: dal 2026 per la Personal non esiste più il file `.ulf`)
- Job di **test EditMode e PlayMode** su ogni push e pull request verso `main` (i PlayMode sono la maggior parte dei test utili, D2 della scheda)
- Job di **build Windows** su tag (`m2`, `m3`… e `v*` per le release) o ad avvio manuale, con la build scaricabile come artifact
- **Cache** della cartella `Library/` e degli oggetti LFS. Il repo è pubblico, quindi i minuti di Actions sui runner standard non si pagano; ogni checkout che scarica file LFS però consuma la banda LFS gratuita, che resta il vincolo da tenere d'occhio anche se oggi è largo (10 GB al mese contro 10 MB per checkout, § 3). È per questo che la build gira sui tag e non a ogni push
- Badge di stato nel README

**Definition of Done:** un push con un test volutamente rotto fa diventare rosso il job — la prova che la CI intercetta davvero i problemi — e un tag produce una build scaricabile che parte.

**Per il CV:** CI/CD per un progetto Unity, una riga che da sola distingue un portfolio.

---

### M3 — "Un dungeon fatto a mano" · 9–14 h

**A schermo:** stanze collegate, costruite a mano con un tileset modulare grezzo, buie, illuminate da torce; il personaggio porta con sé un raggio di luce. In fondo, una scala scende al secondo livello, e vita e stato del personaggio lo seguono.

**Perché prima del procedurale:** un generatore va scritto sapendo cosa deve produrre. Costruisci a mano il livello che vorresti, misura i moduli, capisci quali pezzi servono: poi automatizzi.

**Contenuto:**

- Griglia di moduli a dimensione fissa (4×4 m come punto di partenza): pavimento, muro, angolo, porta, scala
- Due livelli fatti a mano, con la **struttura a scene additive** (§ 4.3) e un `LevelManager`
- Illuminazione URP con ombre; oscurità e raggio di luce del giocatore
- Cinemachine 3 al posto del `CameraFollow` scritto a mano (attenzione ai tutorial per la 2.x, § 3). Cosa porta in più: **Impulse**, cioè uno scuotimento della camera misurato sui colpi pesanti subiti e sulle morti, che completa il game feel della M2; una **zona morta** in cui il cavaliere si muove senza che la camera lo insegua; smorzamento configurabile senza codice. I test della M1 sulla camera vanno riportati sulla nuova camera
- **Audio posizionale.** Oggi i suoni sono 2D, perché con l'`AudioListener` sulla camera, a 20 m, l'attenuazione 3D renderebbe tutto quasi muto. Le modifiche: l'`AudioListener` passa sul giocatore, o su un oggetto che lo segue all'altezza della testa; le `AudioSource` dei personaggi diventano 3D (`spatialBlend` 1) con attenuazione logaritmica tra circa 4 e 25 m; un `AudioMixer` con i gruppi SFX, UI e, più avanti, Musica. In un dungeon buio, un nemico fuori schermo si deve sentire prima di vederlo
- La sandbox `Sandbox_Combat` resta la scena dei test: pavimento più grande, o sfondo in tinta con il pavimento, perché camera e GIF non mostrino il bordo nero oltre il piano
- Da valutare in un ADR: una build **Web** oltre a quella Windows. Si gioca nel browser senza scaricare nulla, e per chi valuta un portfolio fa differenza; richiede però il modulo *Web Build Support*, e URP sul web ha limiti che potrebbero pesare sugli shader di M11

**Decisione da prendere:** struttura delle scene (§ 4.3).

**Definition of Done:** in build, scendi dal livello 1 al livello 2 mantenendo vita e stato. La pubblicazione su itch.io, prevista qui fino alla v2.8, è spostata a M10.

**Per il CV:** level design modulare, gestione delle scene, illuminazione in URP, pubblicazione.

---

### M4 — "Raccolgo roba" · 8–12 h

**A schermo:** un nemico lascia cadere una spada; la raccogli, apri l'inventario a griglia, la metti nello slot dell'arma e il danno nel pannello del personaggio sale. La togli, e torna com'era.

**Contenuto:**

- `ItemDefinition` (ScriptableObject) con **ID stabile**, `ItemInstance` (classe semplice e serializzabile), registro `ItemDatabase` (§ 4.4)
- Oggetti a terra e raccolta
- Inventario a griglia con oggetti su più celle in **uGUI** (per l'HUD di un gioco è più pratico di UI Toolkit, che tornerà utile per i tool dell'editor): click-e-click come in Diablo 1 invece del drag & drop previsto fino alla v2.10 (ADR-023)
- Slot di equipaggiamento
- `StatSystem`: i quattro attributi del § 2, modificatori fissi e percentuali con un **ordine di applicazione definito e documentato**, formula del colpo del § 2
- Tooltip

**Decisione da prendere:** conferma di attributi e formula del colpo (§ 2).

**Test:** ordine dei modificatori (fissi prima dei percentuali); equipaggiare e poi togliere un oggetto riporta le statistiche esattamente ai valori di partenza.

**Definition of Done:** lo scenario descritto sopra, in build. Test verdi.

**Per il CV:** architettura data-driven, sistema di statistiche, UI complessa.

---

### M5 — "Loot casuale" · 9–12 h

**A schermo:** cade una "Savage Short Sword of the Griffin" (in italiano "Spada corta Feroce del Grifone"), rara, con tre affissi; il tooltip li mostra e li confronta con l'arma equipaggiata. Con lo scudo, una parte dei colpi nemici viene bloccata.

È il cuore del genere, e merita una milestone sua.

**Contenuto:** `AffixDefinition` per prefissi e suffissi · pool filtrati per *item level* e tipo di oggetto · rarità normale, magica e rara · generazione del nome, composto secondo la lingua · loot table per nemico · confronto nel tooltip · **blocco** con lo scudo, legato alla Destrezza (§ 2) · **lingue**: inglese di default e italiano, con una tabella delle stringhe a cui una lingua nuova aggiunge solo una colonna. Dettagli e decisioni: `docs/milestones/M5_Loot_casuale.md`.

**Concetti:** estrazione pesata; `System.Random` con **seed riproducibile** — "rigenera il drop 4711" invece di "riprova finché non ricapita".

**Test:** stesso seed, stesso oggetto, sempre; gli affissi rispettano l'item level; su 100.000 estrazioni le rarità restano entro una tolleranza dalle probabilità previste.

**Definition of Done:** in build, con il seed fissato lo stesso nemico lascia sempre lo stesso oggetto; senza seed, in poche partite compaiono normali, magici e rari (le probabilità le verifica un test su 100.000 estrazioni: otto scheletri per partita sono pochi). Lo scudo blocca. Il gioco parte in inglese e passa all'italiano. Test verdi.

**Punto di controllo** (§ 1.3).

**Per il CV:** sistemi procedurali, bilanciamento data-driven, test su logica casuale.

---

### M6 — "Dungeon infinito" · 16–19 h

**A schermo:** i livelli 1–4 sono cripte generate a ogni partita, sempre percorribili dall'ingresso all'uscita; nemici e casse aumentano con la profondità.

**Contenuto:**

- Generazione **BSP**, verifica della connettività (flood fill), istanziazione dei moduli di M3
- **NavMesh a runtime** con `NavMeshSurface.BuildNavMesh()`
- Nemici e casse distribuiti in base alla profondità
- Seed riproducibile per ogni livello
- **Tool dell'editor:** una finestra che genera e disegna il dungeon senza entrare in Play Mode. Circa 150 righe, ore di iterazione risparmiate, e "ho scritto tool per l'editor" pesa a un colloquio
- Aggiunti dopo la prova della build (v2.14): **pozioni di cura** con la cintura a 8 posti, **automappa** che si scopre esplorando (sovrapposta o nell'angolo), **ripartenza dall'ingresso del livello** dopo la morte con l'inventario di quando ci si era entrati, **tooltip** sulle statistiche del personaggio

**Decisioni da prendere:** NavMesh o A* su griglia, con dati reali alla mano (tempi di bake, comportamento nei corridoi stretti) — un A* scritto da te dà pieno controllo su costi e occupazione delle celle, e insegna il pathfinding sul serio; divisione dell'asmdef per area (§ 4.5).

**Test:** su 500 seed, ogni livello è connesso e nessuna stanza si sovrappone a un'altra.

**Definition of Done:** in build, attraversi di fila quattro livelli generati; con lo stesso seed, ottieni lo stesso dungeon. Test verdi.

**Per il CV:** generazione procedurale, algoritmi su grafi, tooling.

---

### M7 — "Le profondità" · 18–25 h

**A schermo:** dal livello 5 il dungeon cambia: caverne organiche e nemici nuovi — uno sciame veloce, un bruto che carica colpi telegrafati. Un'automappa mostra ciò che hai esplorato.

Nella v2.0 tutto questo stava dentro M6, che sarebbe diventata una milestone di sei settimane senza un risultato intermedio da mostrare. Divise, ognuna ha il suo.

**Contenuto:**

- Generazione **random walk** per le caverne
- Archetipi **sciame** e **bruto** (§ 2)
- **Reazione al colpo del cavaliere:** `HitRecovery` anche sul player, con una soglia tarata sui colpi del bruto. Il bruto deve poter interrompere un attacco, lo sciame no (ADR-010)
- **Colpi critici** del cavaliere (v2.17, § 2): danno doppio, numero più grande, e un verso proprio per ogni tipo di nemico colpito
- **Limite di voci audio** per lo sciame: con dieci nemici che colpiscono insieme, una sola clip d'impatto per frame e per tipo, e una priorità più bassa per i nemici lontani
- **Refactoring dell'IA** da `enum` e `switch` a classi di stato: ora i tipi sono tre e il bisogno è reale. Documentato in un ADR, è materiale da portfolio
- **Automappa** estesa alle caverne (l'automappa della cripta è arrivata alla M6, v2.14)
- **Musica e rumori d'ambiente** (anticipati dalla M11, v2.16): una traccia cupa e un fondo di vento e gocce per tipo di livello, e ogni tanto un verso lontano nel buio attorno al cavaliere
- Tabelle di spawn per profondità, con il giusto mix di archetipi

**Test:** gli stessi test di connettività di M6, sul nuovo algoritmo.

**Definition of Done:** in build, attraversi i livelli 5–8 con i nuovi nemici, e l'automappa si riempie mentre esplori. Test verdi.

**Per il CV:** design di IA, refactoring guidato da un bisogno reale, due algoritmi procedurali a confronto.

---

### M8 — "Progressione e persistenza" · 16–22 h

**A schermo:** uccidi, sali di livello, distribuisci punti negli attributi. Trovi un elmo, un anello, un amuleto e li indossi. Chiudi il gioco, lo riapri, e sei dove eri, con lo stesso equipaggiamento. La visuale si avvicina e si allontana con la rotella, e gira attorno al cavaliere.

**Contenuto:** esperienza e curva di livello · punti attributo · **equipaggiamento completo** (v2.19, richiesta di Mirco), descritto sotto · **salvataggio e caricamento** in JSON, con un **formato versionato fin dal primo salvataggio** · regole della morte: decise alla M7, non si perde niente (ADR-048) · **zoom e rotazione della visuale** (v2.18, richiesta di Mirco), descritti sotto.

**Equipaggiamento completo** (proposta, da confermare nella scheda della M8):

- **Slot nuovi** accanto ad arma e scudo: elmo, armatura, guanti, stivali, due anelli, amuleto. Diablo 1 ne ha meno (niente guanti e stivali): qui si tengono perché danno al pannello più varietà con poco lavoro. `EquipSlot` e l'`Equipment` di oggi crescono di valori, non di struttura.
- **Pezzi d'armatura** con l'Armatura e la Forza richiesta, come lo scudo; **anelli e amuleto** senza difesa, fatti dei loro affissi (vita, attributi, colpire, critico). Un affisso nuovo cambia il loot dei semi provati (trappola 10 della M7): i test che fissano i drop si aggiornano insieme.
- **Modelli e icone.** Elmi e pezzi d'armatura dai pacchetti KayKit già scaricati, se ci sono; per anelli e amuleti un pacchetto CC0 da scaricare con il permesso di Mirco, o icone piatte. Si decide nella scheda. L'armatura indossata non cambia il modello del cavaliere: resta per la M11 (non ripreso nella M11: dalla v2.56 è tra i lavori dopo la v1.0).
- **Prima del salvataggio**, così il formato 1 contiene già tutti gli slot.
- Circa 4–6 h in più.

**Visuale: zoom e rotazione** (proposta, da confermare nella scheda della M8):

- **Zoom** con la rotella, tra circa il 60% e il 140% della distanza di oggi, con uno smorzamento breve. Si salva tra le preferenze, come la lingua. Il limite in avvicinamento tiene il cavaliere e i nemici vicini nell'inquadratura; quello in allontanamento non deve mostrare troppo oltre il raggio della sua luce.
- **Rotazione a scatti di 90°** con due tasti (Q ed E, o la rotella premuta), animata in circa 0,4 s. A scatti e non libera, perché i muri bassi (ADR-017) stanno sui lati verso la camera: a ogni scatto ogni lato del livello sceglie di nuovo tra muro alto e muro basso. Il builder mette su ogni lato i due pezzi, e uno solo è acceso; il cambio avviene a metà rotazione, su un evento della camera, senza controlli a ogni frame. Con la rotazione libera servirebbero muri sempre alti che si dissolvono tra la camera e il cavaliere: uno shader apposta, che il piano lascia alla M11.
- **Cosa deve seguire la camera.** Le torce e gli stendardi appesi a un lato che diventa basso. L'automappa, che è ruotata come la camera (ADR-038). La parte sepolta della scala, che oggi si nasconde dietro il muro nord. Il listener e la luce del cavaliere seguono già l'orientamento della camera.
- **Alla fine** un ADR che aggiorna ADR-017. Test: zoom nei limiti; dopo ogni scatto, tra la camera e il cavaliere nessun muro alto.

**Test:** salvare e ricaricare restituisce uno stato identico; un salvataggio nel formato 1 si carica con il codice del formato 2 (migrazione).

**Definition of Done:** lo scenario descritto sopra, in build. Test verdi.

**Per il CV:** serializzazione, persistenza versionata, migrazione dei dati.

---

### M9 — "Magia" · 10–13 h

**A schermo:** la sfera blu del mana si svuota mentre lanci dalla hotbar un proiettile, un incantesimo ad area e un potenziamento; un nemico tiene le distanze e ti bersaglia.

**Contenuto:** mana · 3 incantesimi (proiettile, area, potenziamento) · proiettili gestiti con **`UnityEngine.Pool.ObjectPool<T>`** (esiste già, non serve scriverne uno) · hotbar e tempi di ricarica · effetti con il sistema particellare · pergamene che insegnano gli incantesimi · archetipo **a distanza** (§ 2), che riusa i proiettili · **la vita del nemico che si sta combattendo** (v2.22, richiesta di Mirco), descritta sotto.

**La vita del nemico che si sta combattendo** (proposta, da confermare nella scheda della M9). Dalla M7 la barra in alto mostra il nome e la vita del nemico sotto il cursore (ADR-049): appena il cursore si sposta, sparisce, anche se il cavaliere sta ancora colpendo lo stesso nemico. La barra mostra il nemico sotto il cursore e, quando il cursore non ne ha uno, quello che il cavaliere sta attaccando o ha colpito per ultimo, per circa 3 secondi dopo l'ultimo colpo. Il cerchio rosso a terra resta sul nemico sotto il cursore, così distingue il prossimo bersaglio da quello di prima. Sta nella M9 perché lì il bersaglio cambia significato: un proiettile o un incantesimo ad area colpiscono nemici che il cursore non ha mai toccato, e la regola dell'"ultimo colpito" deve valere anche per loro. Si è scartata la barra sopra ogni nemico: nello sciame sarebbero dieci barre una sull'altra, e Diablo 1 non le ha. Resta un'opzione per dopo la v1.0 (sotto). Circa 1 h.

**Correzioni dalla prova della M8** (v2.27, richieste di Mirco). Con due anelli indossati, il tooltip di un anello nello zaino si confronta con uno solo dei due: deve mostrarli tutti e due, come per ogni slot doppio. I nomi lunghi dei magici e dei rari allargano il tooltip quasi a metà schermo: vanno su due righe, divisi a metà. Circa 1 h, nel primo passo della M9.

**La cintura sotto l'esperienza** (v2.26, richiesta di Mirco). Oggi la cintura delle pozioni sta in basso a sinistra, accanto alla sfera della vita, e la barra dell'esperienza è in basso al centro. La cintura va al centro, sotto la barra. Sta nella M9 perché la hotbar degli incantesimi occupa la stessa fascia in basso: le due file e le due sfere si dispongono insieme, una volta sola. Circa 30 min.

**Definition of Done:** in build, un combattimento misto di mischia e magia contro nemici a distanza; lanciando proiettili a raffica, il Profiler non mostra allocazioni a ogni frame.

**Per il CV:** object pooling, sistemi di abilità estendibili, profiling mirato.

---

### M10 — "Città e loop completo" · 19–26 h

**A schermo:** dal menu arrivi in città, compri dal mercante, scendi, risali a vendere, affronti il boss al livello 8, vedi la schermata di vittoria.

**Contenuto:** città hub · mercante · flusso di gioco completo (menu → città → dungeon → morte o vittoria) costruito sulla struttura a scene di M3 · menu delle opzioni, con la scelta della lingua che sostituisce il tasto provvisorio della M5 · ritorno in città dai livelli profondi · **boss** (§ 2), che lascia loot migliore dei nemici normali (almeno un oggetto raro; v2.22) · vittoria e riconoscimenti. La prima pubblicazione su itch.io, prevista qui fino alla v2.29, passa alla fine della M11 (v2.30, decisione di Mirco).

**Menu iniziale e tre salvataggi** (v2.28, richiesta di Mirco). Il gioco non parte più dentro la partita: parte da un menu.

- **Le voci.** *Continua* riprende l'ultima partita giocata, e manca se non ci sono salvataggi. *Nuova partita* chiede in quale dei tre posti salvarla; se il posto è occupato chiede conferma prima di sovrascriverlo. *Carica* mostra i tre posti. Poi *Opzioni* (la lingua, già prevista qui) ed *Esci*.
- **I tre posti.** Ognuno mostra livello del cavaliere, profondità, città o dungeon, e data dell'ultimo salvataggio, oppure "vuoto". Un posto si può cancellare dal menu, con conferma.
- **Sul disco.** Un file per posto (`save_1.json`, `save_2.json`, `save_3.json`) nella stessa cartella di oggi. Il riepilogo si legge dal salvataggio stesso, quindi il formato non cambia per questo. L'ultimo giocato si riconosce dalla data del file. Il `save.json` di chi ha già giocato diventa il posto 1 al primo avvio: si sposta, non si cancella.
- **Durante la partita** si salva nel posto scelto, come oggi. Con `Esc` si apre un menu di pausa (oggi non c'è) da cui si torna al menu iniziale salvando.
- **Da decidere nella scheda:** cosa fa `-newgame` (oggi ignora il salvataggio) e se il menu va saltato nei test e con `-seed`. La scena del menu è il `Bootstrap` rimandato dall'ADR-014.

**Click destro per indossare** (v2.28, richiesta di Mirco). Oggi il click destro su una cella della borsa beve una pozione o legge una pergamena, e su tutto il resto non fa niente. Diventa: su un oggetto che si indossa, lo indossa nel suo slot.

- **Scambio.** L'oggetto che c'era nello slot torna nella borsa, nel posto lasciato libero se ci sta, altrimenti nel primo posto libero. Se non c'è posto da nessuna parte, non succede niente.
- **Slot doppi.** Un anello va nel posto libero; se sono occupati tutti e due, sostituisce il primo.
- **Requisiti.** Se la Forza non basta, l'oggetto resta nella borsa, come quando lo si porta nello slot con il click sinistro.
- **Tooltip.** Una riga in fondo dice "Click destro: indossa", come per pozioni e pergamene.
- Circa 0,5–1 h: la logica di scambio c'è già in `Equipment.TryEquip`, manca solo il ritorno nella griglia.

**Avviso dei punti da spendere** (v2.28, richiesta di Mirco). Oggi i punti attributo non spesi si vedono solo aprendo il pannello del personaggio (`C`): chiuso il pannello, ci si dimentica di averli.

- **Cosa compare.** Un pulsante dorato con un **+** e il numero dei punti, sopra la sfera della vita, come il pulsante *Level Up* di Diablo 1. Compare salendo di livello e sparisce quando i punti finiscono.
- **Cosa fa.** Un click apre il pannello del personaggio, come `C`. Mentre il pannello è aperto il pulsante si nasconde, perché i **+** sono già lì.
- **Senza controlli a ogni frame.** Si aggiorna sull'evento `Changed` di `CharacterProgress`, e all'avvio da un salvataggio con punti non spesi è già visibile.
- Circa 0,5 h.

**Caratteristiche colorate** (v2.28, richiesta di Mirco). Oggi il pannello del personaggio scrive tutti i valori in bianco, e non si capisce quanto viene dagli oggetti. Come in Diablo 1, un valore cambiato da oggetti o effetti si colora: **azzurro** se è più alto del valore di base, **rosso** se è più basso. Il rosso oggi non compare mai, perché nessun affisso e nessun effetto toglie: servirà con le maledizioni o gli oggetti con un difetto, se arriveranno.

- **Forza, Destrezza, Magia, Vitalità.** Il valore con i bonus (`Get`) confrontato con quello dei punti spesi (`GetBase`).
- **Vita, Armatura, Mana e colpo critico.** Colorati quando c'è un modificatore di oggetti o effetti sulla loro statistica, per esempio la Pelle di pietra sull'Armatura finché dura. Il danno e la probabilità di colpire restano bianchi: dipendono già dall'arma, e colorarli sempre non direbbe niente.
- **Tooltip.** La riga del tooltip aggiunge "base X, oggetti +Y", così il colore ha una spiegazione.
- Si aggiorna con gli eventi che già ridisegnano il pannello, senza controlli a ogni frame. Circa 1 h.

**Nomi degli oggetti a terra con `Alt`** (v2.28, richiesta di Mirco). Oggi il nome di un oggetto a terra si vede solo con il cursore sopra, uno alla volta. Tenendo premuto `Alt`, come in Diablo 2, compaiono i nomi di tutti gli oggetti a terra visibili sullo schermo.

- **Piccoli.** Un corpo più piccolo dell'etichetta sotto il cursore di oggi, su un fondo scuro semitrasparente, nel colore della rarità.
- **Senza sovrapporsi.** Le etichette si dispongono dall'oggetto più vicino al cavaliere al più lontano: ognuna parte sopra il suo oggetto e, se tocca un'etichetta già messa, sale di una riga finché trova posto. Un trattino sottile la collega all'oggetto quando si è spostata. Se una pila di oggetti ne fa più di un certo numero, le ultime non si mostrano.
- **Cliccabili.** Un click sul nome raccoglie l'oggetto, come un click sull'oggetto, così si sceglie bene anche in un mucchio.
- **Opzioni.** Nel menu delle opzioni si potrà scegliere "sempre visibili" invece di tenere premuto `Alt`.
- **Senza allocazioni a ogni frame.** Etichette prese da un pool; la disposizione si ricalcola a ogni frame solo mentre `Alt` è premuto, perché la camera si muove, con buffer fissi.
- Circa 1,5–2 h.

**A fine M10 il gioco è finibile.** È il momento di farlo provare a cinque persone e di prendere appunti senza difendersi: basta lo zip della build Windows della CI.

**Definition of Done:** una partita completa, dall'avvio alla vittoria, in build, senza passare dall'editor; la build della CI parte su un PC che non è quello di Mirco.

**Per il CV:** gestione dello stato di gioco, design di un boss.

---

### M11 — "Look, feel e release" · 20–30 h (percorso base)

**Contenuto (percorso base):** asset CC0 resi coerenti dallo **shader retro** in Shader Graph (dithering ordinato, palette limitata, eventuale riduzione della risoluzione con una Renderer Feature di URP) · post-processing (vignetta, grana, bloom misurato) · rifinitura dell'audio (musica, rumori d'ambiente, audio posizionale e mixer a gruppi ci sono già: M3 e M7) · **profiling** completo (Profiler, Frame Debugger, Memory Profiler) · **dungeon più ricco** (v2.26), descritto sotto · release **v1.0**. La prima pubblicazione su itch.io viene dopo, nella fase che segue la v1.0 (v2.36).

**Un dungeon meno spoglio** (v2.26, richiesta di Mirco). Oggi le stanze hanno pavimento, muri, torce, qualche colonna o cassa e gli stendardi. Mirco le trova monotone. Il pacchetto KayKit Dungeon ne ha già molte altre: per ora ne usiamo 42 su 211, e si possono aggiungere barili, tavoli, scaffali, libri, ossa, ragnatele, catene, candele, sacchi e detriti. Il popolatore della M6 le mette con poche regole: lungo i muri e negli angoli, mai sul percorso tra le porte, mai sopra le celle delle scale, e più fitte nelle stanze grandi. Un "tema" per stanza (deposito, biblioteca, ossario) le tiene coerenti, e il seme del livello le rende ripetibili come il resto. Tutto quello che blocca il passaggio entra nel NavMesh, e il resto è solo decorazione senza collider. Le caverne hanno una lista loro, con funghi, cristalli e rocce. Si misura in build: la decorazione non deve alzare il tempo di costruzione del livello oltre i 50 ms del punto di controllo D4 della M6. Circa 2–3 h, e per questo la stima sale.

**Estensione opzionale — Blender.** Sostituire gli asset CC0 con modelli propri è **fuori** dalle 20–30 h: la sola modellazione, partendo da zero con Blender, vale facilmente 80–100 h, e lì il lavoro resta tutto di Mirco. Si valuta a M11 chiusa, con la v1.0 già pubblicata, e semmai diventa una v1.1. Non è un taglio: è il percorso base che non la prevede.

**Definition of Done:** v1.0 con il tag `v1.0.0` e la build della CI, `CREDITS.md` completo, README con GIF e sezione sull'architettura.

**Per il CV:** Shader Graph, ottimizzazione, pipeline di release.

---

### Dopo la v1.0

Lavori decisi ma rimandati, da riprendere quando arriva il momento indicato. Non sono nella stima del § 1.3.

- **Nuove classi e le loro armi** (chiesto da Mirco il 4 ott 2026). Quando si aggiungono classi oltre al Guerriero: armi **a due mani** (spadone, ascia a due mani; occupano anche lo slot dello scudo), **bacchette** e **bastoni** per gli incantatori, **archi** e **balestre** con le frecce, e gli altri tipi che serviranno. Dalla M5 il tipo d'arma è un dato della definizione (`WeaponKind`): un tipo nuovo vuole un valore dell'enum, la regola sugli slot, le animazioni e gli affissi che lo ammettono, non una struttura nuova. I modelli di KayKit *Adventurers* ci sono già: `sword_2handed`, `axe_2handed`, `staff`, `wand`, `bow`, `crossbow_1handed`, `crossbow_2handed`.
  - **Creazione del personaggio** (dai tester, 18 ott 2026). Con più classi, *Nuova partita* passa da una schermata che fa scegliere la classe, con il modello che gira e una riga sul suo stile, e il nome del personaggio, che va nel salvataggio e nei tre posti del menu. Con il solo Guerriero resterebbe solo il nome: si fa con le classi. Circa 2–3 h.
- **Una punizione alla morte** (chiesta da Mirco il 7 ott 2026). Dalla M7 morire non costa niente: si torna all'ingresso del livello con tutto (ADR-048). Quando il gioco sarà finibile andrà pensata una perdita che dia peso alla morte senza far ricominciare da capo. Idee da valutare: una parte dell'oro (che arriva con il mercante della M10), l'oggetto in mano lasciato dove si è morti da andare a riprendere come in Diablo 1, una perdita di esperienza (M8), o i nemici uccisi che in parte rinascono. Se le prove della M10 con cinque persone dicono che la morte non pesa, si anticipa.
  - **Decisa da Mirco il 18 ott 2026:** si perdono **una parte dell'oro e una parte dell'esperienza**, crescenti con la difficoltà (per esempio al Crepuscolo poco o niente, di più dall'Ombra, di più nell'Abisso; i numeri sono dati da provare). **L'esperienza persa non fa mai scendere di livello**: si ferma all'inizio del livello attuale (`CharacterProgress`). Si fa con le difficoltà.
- **Barre della vita sopra i nemici** (chiesto da Mirco l'8 ott 2026, come alternativa alla barra in alto della M9). Una barra piccola sopra la testa, solo per i nemici feriti, così nello sciame non se ne vedono dieci intatte. È un'opzione del menu della M10, spenta di default. Le barre stanno in un solo canvas in world space, prese da un pool e non una per nemico, e si aggiornano su `HealthChanged` senza controlli a ogni frame.
- **Nemici speciali** (chiesto da Mirco l'8 ott 2026; è la **M12**, con le casse di più tipi, dalla v2.42). Oltre al boss finale della M10, nemici più forti dei normali che lasciano loot migliore, come i *nemici unici* e i *campioni* di Diablo:
  - **Campioni:** un nemico normale reso più forte da un dato, non da un archetipo nuovo. Ha più vita e più danno, un nome colorato e una tinta del modello, è un po' più grande e ha uno o due modificatori presi da una lista (più veloce, colpi che rallentano, rigenera la vita, resiste al blocco). Arriva con una scorta di nemici normali.
  - **Unici:** un nemico con un nome suo e un modello suo, uno per tipo di livello, che lo spawn mette con una certa probabilità. Per esempio un capo degli scheletri nella cripta e un bruto più grande nelle caverne.
  - **Loot:** più oggetti, e una tabella delle rarità spostata verso magico e raro (una moltiplicazione dei pesi della `RarityTable`, da M5). Gli unici lasciano sempre almeno un oggetto raro.
  - Nel codice entra come un dato in più dell'`EnemyArchetype` e della `SpawnTable` (ADR-047): un moltiplicatore della vita, del danno e del loot, e la lista dei modificatori. La barra in alto mostra il nome colorato e i modificatori.
  - Un boss a metà discesa, alla fine della cripta, è una variante da decidere insieme. Se le prove della M10 dicono che la discesa è monotona, si anticipa. Fuori dalla M12 (D11): ha una voce sua più sotto (v2.57).
- **Personaggi nello stile di Diablo 2** (chiesto da Mirco l'8 ott 2026, con un'immagine di *Diablo II: Resurrected* come riferimento). Oggi i personaggi sono KayKit: proporzioni da chibi, testa grande, colori piatti. Lo stile richiesto ha proporzioni realistiche, armature con usura e dettagli, texture con materiali (metallo, cuoio, stoffa) e luci più contrastate. Cosa comporta:
  - **Modelli.** Personaggi CC0 realistici quasi non esistono. Le strade sono tre: modellarli in Blender, che è l'estensione opzionale della M11 (80–100 h di lavoro di Mirco); comprare un pacchetto, che con il repo pubblico vuole il submodule privato scartato nell'ADR-004 o un nuovo ADR; oppure un pacchetto CC0 semi-realistico, da cercare e valutare quando si comincia.
  - **Animazioni.** Le clip di oggi sono per lo scheletro `Rig_Medium` di KayKit (ADR-005): un personaggio con un altro rig vuole clip sue, per il cavaliere e per ogni nemico.
  - **Coerenza.** Personaggi realistici in un dungeon KayKit si vedono fuori posto: cripta, caverne, oggetti e icone vanno cambiati insieme, o almeno avvicinati con lo shader retro della M11.
  - **Codice.** Quasi niente: modelli, animator e icone sono dati. Restano da rifare i punti di aggancio delle armi (`handslot`), i raggi degli agent e i tempi dei colpi misurati sulle clip.
  - Si valuta a v1.0 pubblicata, insieme all'estensione Blender della M11, di cui è la versione completa.
- **L'equipaggiamento si vede addosso al cavaliere** (chiesto da Mirco il 22 ott 2026). Oggi il modello del cavaliere cambia solo con l'arma e lo scudo, agganciati alle mani (`handslot`). Elmo, armatura, guanti e stivali restano quelli del Knight di KayKit, qualunque cosa si indossi. La M8 l'aveva rimandato alla M11 (D6 della scheda della M8), e la M11 non l'ha ripreso. Diventa: ogni pezzo indossato si vede.
  - **Quali slot.** Elmo, armatura, guanti e stivali cambiano il modello; arma e scudo come oggi. Anelli e amuleto no: alla distanza della camera non si vedrebbero.
  - **Da dove vengono i pezzi.** I personaggi KayKit (Knight, Rogue_Hooded, Barbarian) sono divisi in parti: testa, busto, braccio sinistro e destro, gamba sinistra e destra, elmo o copricapo, e il mantello del Knight. Sono gli stessi personaggi da cui sono nati gli oggetti (ADR-056), tutti sullo scheletro `Rig_Medium` (ADR-005). Una parte di un altro personaggio si lega alle ossa del cavaliere e segue le sue animazioni senza clip nuove. L'elmo cambia l'elmo, l'armatura il busto (e il mantello, che va con la corazza), i guanti le due braccia, gli stivali le due gambe: in KayKit mani e piedi non sono parti a sé.
  - **Le varianti di colore**, come la corazza nera, sono la stessa parte con il loro materiale, come l'oggetto a terra e l'icona.
  - **Slot vuoto.** KayKit non ha un corpo senza armatura. Senza elmo si vede la testa, che è già una parte a sé. Per busto, braccia e gambe senza niente si sceglie nella scheda tra le parti più semplici dei tre personaggi, o una loro copia con la texture schiarita. Si guardano in foto prima di scegliere.
  - **Nel codice.** Ogni definizione di un oggetto da indossare dice quali parti mette (un dato in più, come il modello a terra). Un componente sul cavaliere ascolta il `Changed` dell'`Equipment` e cambia le mesh delle parti: niente controlli a ogni frame, nessun oggetto creato a ogni cambio. Il salvataggio non cambia: l'aspetto si ricava dall'equipaggiamento.
  - **Le prove.** Una foto del cavaliere con ogni pezzo, accanto a quello di oggi, prima dei prefab; un test PlayMode che indossa e toglie ogni pezzo e controlla la mesh della sua parte.
  - Circa 3–5 h, più le foto. Si può fare in qualsiasi momento, anche prima delle *Nuove classi*: ogni classe nuova riusa lo stesso componente con le sue parti di partenza.
- **Movimento con WASD** (chiesto da Mirco il 9 ott 2026). Oggi il cavaliere si muove con il click (ADR-006). Con WASD si muove anche da tastiera, e il mouse resta per attaccare, raccogliere e interagire. Cosa comporta:
  - **Direzioni relative alla camera.** W va verso l'alto dello schermo, non verso nord: con la rotazione della M8 (ADR-052) le direzioni del mondo cambiano a ogni scatto. Le diagonali vengono da due tasti insieme.
  - **NavMesh.** Il `NavMeshAgent` resta: ogni frame riceve una destinazione poco avanti nella direzione dei tasti, o si sposta con `NavMeshAgent.Move`, così i muri e gli ostacoli fermano il cavaliere come oggi e i nemici continuano a trovarlo.
  - **Convivenza con il click.** Un tasto premuto annulla il percorso del click; un click a tasti fermi riprende come oggi. Il click su un nemico, durante il WASD, attacca senza spostarsi verso di lui se è già a portata, come lo Shift di Diablo.
  - **Tasti già usati.** Q ed E girano la camera e restano lì, lontane da WASD; I, C, M e i numeri della cintura non si toccano. Il menu delle opzioni della M10 potrà poi permettere di cambiarli.
  - **Animazioni e passi** seguono già la velocità dell'agent: non cambiano.
  - Nel codice è un'azione `Move2D` in più nel `PlayerControls` e un ramo nel `PlayerController`. Il gamepad, fuori dall'ambito (§ 1), riuserebbe la stessa azione.
- **Nemici e oggetti che crescono con il personaggio** (chiesto da Mirco l'11 ott 2026). Dalla M8 il livello dei nemici e degli oggetti dipende solo dalla profondità: il livello della zona. Così un personaggio forte che torna ai primi livelli trova nemici facili e loot povero, come in Diablo 1. Con questa regola nemici e oggetti seguono anche il livello del personaggio. Cosa comporta:
  - **Regola.** Il livello di un nemico o di un oggetto è il più alto tra quello della zona e il livello del personaggio meno un margine (per esempio 2). In profondità comanda la zona come oggi, e nei primi livelli comanda il personaggio.
  - **Nemici.** Vita, danno, armatura ed esperienza crescono con il livello, con una curva nella `ProgressionSettings` della M8. Oggi i nemici hanno numeri fissi per tipo.
  - **Oggetti.** Il livello dell'oggetto c'è dalla M5 (`ItemInstance.ItemLevel`) e decide già quali affissi può avere: basta passargli il livello nuovo.
  - **Il rischio.** Se tutto cresce con il personaggio, salire di livello non si sente più. Il margine e la curva vanno provati in build, e la regola può essere un'opzione della difficoltà invece di valere sempre.
  - **Salvataggio.** Non cambia: i ricordi dei livelli (ADR-079) dicono solo quali nemici sono morti, non i loro numeri, e gli oggetti hanno già il loro livello.
  - Si valuta insieme ai nemici speciali, che toccano gli stessi numeri.
- **Barili da rompere** (chiesto da Mirco il 12 ott 2026; fatti nella **M13**, v1.2 del 21 ott 2026, con i barili esplosivi). Come in Diablo 1: un barile si rompe con un colpo, cliccandolo come un nemico, e a volte lascia qualcosa. Cosa comporta:
  - **Più barili.** Il popolatore li mette in gruppi da due a quattro, lungo i muri e negli angoli, più fitti nelle stanze a tema deposito degli arredi della M11 e mai sul percorso tra le porte. Indicativamente 4–8 per livello nella cripta e 2–5 nelle caverne, da provare in build.
  - **Cosa lasciano.** Meno di uno scheletro, perché sono tanti e non fanno male. Per barile, con tiri separati: **oro 15%**, la metà della quantità di uno scheletro alla stessa profondità; **un oggetto 5%**, con la tabella delle rarità di sempre; **una pozione 5%**. Circa tre barili su quattro non lasciano niente. Con 6 barili a livello fanno in media un oggetto ogni tre livelli e un po' d'oro: un premio per chi esplora, non una fonte che cambia il bilanciamento della M10. I numeri sono una `LootTable` come le altre, quindi si tarano senza toccare il codice.
  - **Il seme.** Un dominio suo, come l'oro (ADR-071), così il loot dei nemici e delle casse dei semi già provati non cambia.
  - **Nel codice.** Un componente `Breakable` con una `Health` di un colpo, il modello rotto (il pacchetto KayKit *Dungeon* ha i barili e i loro pezzi), il suono del legno, il collider che sparisce e l'ostacolo del NavMesh che si toglie. Il click del cavaliere lo tratta come un bersaglio, senza esperienza e senza barra in alto. Rotto, entra nei ricordi del livello come una cassa aperta (ADR-079).
  - **Anche in mezzo alle stanze** (chiesto da Mirco il 14 ott 2026, nella prova del passo 11.7). Gli arredi della M11 stanno contro i muri perché non si spostano e non devono chiudere un passaggio; un barile si rompe, e quindi qualche gruppo può stare in mezzo a una stanza grande, come in Diablo 1. Ribadito da Mirco il 17 ott 2026: quando i barili si rompono, non vanno solo vicino ai muri. Resta la regola del percorso tra le porte: un gruppo in mezzo non lo taglia, così nemici e cavaliere non devono romperlo per passare.
  - **Variante da decidere insieme:** i barili esplosivi delle profondità più basse, che feriscono chi è vicino, cavaliere compreso.
  - Circa 2–3 h. Se si fa subito dopo la v1.0, riusa le regole degli arredi della M11.
- **Oggetti unici e set** (chiesto da Mirco il 12 ott 2026; **fatti nella M16**, v1.4.0, 26 ott 2026, scheda `docs/milestones/M16_Tesori_con_un_nome.md`, v2.62). Oggi le rarità sono tre, normale, magico e raro, e una quarta leggendaria era stata chiesta e poi tolta alla M5 (ADR-027). Due gradi nuovi sopra il raro, come in Diablo 2:
  - **Unico** (nome in oro): un oggetto scritto a mano, con un nome suo ("La Mannaia del Re") e affissi fissi, o con un intervallo stretto, più forti di quelli di un raro. Uno per tipo di base all'inizio, una decina in tutto. Cade molto raramente, dalle profondità più basse, e il Re delle ossa può lasciarne uno.
  - **Set** (nome in verde): pezzi con un nome ciascuno che appartengono a un set ("L'armatura del Custode": elmo, corazza, guanti, stivali). Ogni pezzo ha i suoi affissi; indossandone **due, tre o tutti** si accendono bonus in più, come in Diablo 2 e 3: per esempio +Vita con due pezzi, +Armatura con tre, un effetto speciale con tutti. Il tooltip elenca i pezzi del set, quelli indossati in bianco e gli altri in grigio, e i bonus accesi.
  - **Nel codice.** Unici e set sono asset (`UniqueDefinition`, `SetDefinition` con i pezzi e i bonus per numero di pezzi), come le definizioni degli oggetti. I bonus del set sono modificatori della `StatSheet` con l'equipaggiamento come sorgente, ricalcolati quando cambia uno slot: lo stesso meccanismo degli affissi, senza controlli a ogni frame. La `RarityTable` della M5 aggiunge i due gradi con pesi molto bassi.
  - **Salvataggio.** Un formato nuovo: l'istanza dice se è un unico o un pezzo di set e quale.
  - **Con i nemici speciali.** Campioni e unici (sopra) sono il posto naturale da cui fanno cadere questi oggetti: conviene farli insieme.
  - È stato fuori ambito dalla v1 del piano (§ 1.1, `ICEBOX.md`): passa qui, dopo la v1.0. Circa 5–8 h con una decina di unici e due o tre set; scrivere oggetti a mano è la parte lunga.
- **Albero delle abilità nello stile di World of Warcraft** (chiesto da Mirco il 14 ott 2026; stile e regole decisi da Mirco il 19 ott 2026, con un'immagine del pannello dei talenti di WoW come riferimento). Oggi il cavaliere cresce con i punti attributo (M8) e impara gli incantesimi dalle pergamene (M9): a Mirco non piace. Per lui è uno dei lavori che rendono il gioco più accattivante, e la parte importante è un **albero sensato per ogni classe**. Cosa comporta:
  - **Niente più pergamene degli incantesimi.** Abilità e magie si imparano **salendo di livello**, spendendo punti nell'albero. Le pergamene escono dal loot e dal mercante; un salvataggio con incantesimi già imparati restituisce i punti e l'albero si rifà. La magia del portale (M11) resta a tutti, fuori dall'albero.
  - **Distinte per classe.** Ogni classe ha il suo albero e le sue abilità. Le magie di oggi si riassegnano: il Dardo e l'Anello di fuoco vanno a una classe che usa la magia, quando arriva (*Nuove classi*, sopra); il Guerriero prende abilità da guerriero (per esempio un colpo potente, una carica, un grido di battaglia, un turbine, un colpo di scudo) e la Pelle di pietra può restargli come difesa.
  - **Due alberi affiancati, come nell'immagine.** A sinistra l'**albero della classe**, comune a tutti i personaggi di quella classe; a destra l'**albero della specializzazione**, scelta una volta dal livello 10 tra due o tre (per il Guerriero, per esempio, *Difesa* con lo scudo e *Armi* a due mani). Sopra ogni albero i punti disponibili.
  - **I nodi.** Collegati da linee: un nodo si apre quando è preso uno di quelli sopra a cui è legato. Ci sono nodi **attivi** (un'abilità nuova, da mettere nella hotbar), **passivi** (una statistica, un effetto), **a più gradi** (1/2, 1/3, con il numero sull'icona) e **a scelta** (due abilità sullo stesso nodo, se ne prende una, come i nodi a forma di ottagono di WoW). Più in basso, righe che si aprono solo dopo un certo numero di punti spesi nello stesso albero. Preso, il nodo ha il bordo d'oro; disponibile, colorato; chiuso, grigio.
  - **I punti.** Uno a ogni livello dal 2, alternati tra l'albero della classe e quello della specializzazione. Al livello massimo (50, con le difficoltà della M14) gli alberi **non si riempiono**: le scelte restano vere, e due personaggi della stessa classe possono giocare in modo diverso.
  - **Cambiare idea.** Le modifiche si provano e si confermano con *Applica modifiche*, come in WoW. Riassegnare tutti i punti si fa all'accampamento, per un po' d'oro.
  - **Il pannello.** Un tasto suo (**N**, libero; come in WoW), a tutto schermo come l'immagine, nelle due lingue, con il tooltip di ogni nodo (cosa fa, grado, cosa serve per aprirlo). Le abilità attive prese vanno nella hotbar degli incantesimi (F1–F3 di oggi), che si allunga se servono più posti.
  - **Prima del codice, il disegno.** Per ogni classe uno schema dell'albero su una pagina: i rami, gli **stili di gioco** che permette (tre o quattro per classe), ogni nodo con il suo effetto e perché sta lì. Mirco lo approva prima che si scriva una riga. Per il Guerriero, indicativamente 20–25 nodi nell'albero della classe e 15–20 per specializzazione.
  - **Le icone.** Ne servono decine, una per nodo, nello stile dell'immagine: è la parte di arte più grossa. La fonte è da decidere con l'ADR-004 (solo CC0): un pacchetto CC0, o icone nostre fatte in Blender come i modelli della M13.
  - **Nel codice.** Un nodo è un dato (`TalentNode`: id, posto nella griglia, collegamenti, gradi, effetto, alternativa del nodo a scelta), un albero è un asset per classe e per specializzazione. L'effetto è un modificatore della `StatSheet` (lo stesso meccanismo degli affissi), un grado in più di un'abilità o un'abilità nuova. La logica (cosa si può prendere, punti, riassegnazione) sta in `Scripts/Core/` con i suoi test; i punti vengono dal `CharacterProgress` della M8; un formato nuovo del salvataggio con la specializzazione e i gradi per id.
  - **Quando.** Dopo la M14 (gradi degli affissi e difficoltà), insieme alle *Nuove classi* e alla *Creazione del personaggio* (sopra), che decidono quali alberi esistono. Circa 15–25 h per il sistema, il pannello e l'albero del Guerriero; ogni classe in più 6–10 h di contenuti, icone escluse.
- **Tornare al livello di sopra** (chiesto da Mirco il 17 ott 2026, nella seconda prova del passo 11.7). Oggi la cripta si scende soltanto: l'ingresso di ogni livello è un punto di arrivo, e per risalire c'è il portale per l'accampamento (ADR-076), che ha scartato le scale in su perché avrebbero fatto rifare otto livelli a ritroso. Dal passo 10.9 però i livelli lasciati restano com'erano (ADR-079), e quindi risalire porta in un posto già ripulito, non in un livello nuovo da rifare. Cosa comporta:
  - **La scala in su.** L'ingresso `<` del generatore diventa una scala che sale, con il suo modello accanto al punto di arrivo; il click la usa come la scala che scende. Porta al livello di sopra, davanti alla sua scala `>`: un ingresso nuovo `FromBelow` accanto a quello di sempre. Dalla profondità 1 porta all'accampamento, accanto alla discesa nella cripta.
  - **Il livello di sopra** torna com'era, con i nemici uccisi e le casse aperte (ADR-079); il seme e la profondità bastano a rifarlo.
  - **Nel codice.** `LevelExit` accetta una profondità di arrivo più bassa di quella attuale (oggi lo zero vuol dire "quella sotto"); `LevelManager` sceglie l'ingresso dal verso del viaggio. Le caverne hanno lo stesso ingresso e la stessa regola. L'automappa segna la scala in su con il suo simbolo.
  - **Il re.** La scala in su della profondità 8 sta nella stanza di partenza, lontana dalla sala: non serve a scappare dallo scontro, come il portale non può (passo 11.7).
  - **Salvataggio.** Probabilmente nessun formato nuovo: si salvano già la profondità e i ricordi dei livelli, e *Continua* riparte dall'ingresso di sempre.
  - Circa 2–3 h, con la prova in build: una scala in su non deve chiudere il percorso tra le porte della stanza di partenza.
- **v1.0.1, prima della beta** (dai tester, 18 ott 2026). Due ritocchi piccoli, che chi prova nota per primi:
  - **Vita e mana sempre visibili.** Un'opzione *Numeri sulle sfere sempre visibili*, come quella dei nomi degli oggetti a terra; senza, restano al passaggio del mouse (passo 11.7).
  - **L'oggetto indossato si riconosce nel confronto.** Oggi il tooltip di confronto ha sopra il nome una scritta "Equipaggiato" piccola e grigia. Diventa più grande e colorata, con una cornice diversa per l'oggetto indossato, così con due anelli si capisce subito quale tooltip è quale.
  - Circa 1–1,5 h, con la build da provare.
  - **Fatta il 18 ott 2026** (`bundleVersion` 1.0.1): l'opzione *Numeri di vita e mana sempre visibili* sotto quella dei nomi a terra, ricordata tra le preferenze (`OrbReadout.SetAlwaysVisible`); nel confronto la scritta "Equipaggiato" è in oro e grassetto, e il riquadro dell'oggetto addosso ha il bordo d'oro più spesso (`ItemTooltip.IsMarkedEquipped`). Test: `MenuTests.Options_KeepOrbNumbersVisible`, il confronto dello scudo in `InventoryUITests` controlla i due bordi. 310 EditMode e 200 PlayMode verdi. Esce con il tag `v1.0.1`.
  - **v1.0.2, dalla prova di Mirco (18 ott 2026).** Le spunte delle opzioni non si vedevano: lo sprite *Checkmark* di Unity è già grigio scuro e, tinto d'oro, sul marrone del riquadro spariva. Al suo posto un segno bianco nostro (`Art/UI/check.png`), un po' più grande, che prende davvero l'oro. Nel confronto l'oro e il grassetto facevano sembrare raro l'oggetto addosso, e il bordo spesso dorava tutto il riquadro (l'`Outline` di Unity ricopia l'intera immagine): i due riquadri tornano uguali, e "Equipaggiato" resta sopra il nome al 70% (`ItemDescription.HeaderSize`), in un grigio chiaro (`HeaderColor`). Test: il test delle opzioni controlla la spunta. Esce con il tag `v1.0.2`.
- **Suggerimenti al primo uso e pannello di aiuto** (dai tester, 18 ott 2026). Chi prova il gioco non conosce i comandi, che oggi stanno solo nel README.
  - **Suggerimenti**, non un livello di tutorial a parte: un riquadro breve la prima volta che serve (il primo nemico, il primo oggetto a terra, la prima pozione nella cintura, la prima scala, il primo punto attributo, la prima pergamena, il portale). Ognuno compare una volta sola e si ricorda tra le preferenze; un'opzione li spegne tutti o li fa ripartire.
  - **Pannello di aiuto** con i comandi, da F1 o dal menu di pausa, nelle due lingue.
  - Nel codice: i momenti sono eventi che esistono già (nemico visto, oggetto a terra, salita di livello); un componente li ascolta e mostra il testo dalla tabella delle stringhe. Circa 4–5 h.
- **Nemici che scappano** (dai tester, 18 ott 2026). Oggi scappa solo il mago, per tenere la distanza (M9). Uno stato di **fuga** nella macchina a stati della M7 (ADR-042): scatta con poca vita, dopo un certo numero di colpi presi o quando muore un compagno vicino; il nemico corre lontano dal cavaliere sul NavMesh per qualche secondo, poi torna a combattere o chiama i compagni. Per lo sciame e gli scheletri semplici, non per il bruto né per il re. Le soglie sono dati dell'archetipo. Circa 2–3 h.
- **Casse di più tipi** (dai tester, 18 ott 2026, come in *Titan Quest*). Oggi le casse sono tutte uguali. Tre tipi, ognuno con la sua `LootTable` e un modello suo del pacchetto KayKit: la **cassa** di sempre; il **forziere**, più raro, accanto alla scorta di un campione o di un unico, con più oggetti e rarità spostate verso magico e raro; il **tesoro del re** nella sala del boss, che si apre quando il re muore. Si fa insieme ai nemici speciali, che danno il posto ai forzieri. Il seme ha già il suo dominio per le casse: il tipo si tira a parte, così il contenuto delle casse dei semi già provati non cambia. Circa 2–3 h.
- **Difficoltà dopo la vittoria** (dai tester, 18 ott 2026; decisioni di Mirco del 18 ott 2026, come in Diablo 3). **Fatta nella M14** (v1.3.0, 23 ott 2026) (scheda `docs/milestones/M14_La_luce_si_spegne.md`, v2.55), con i gradi degli affissi, il livello 50 e la punizione alla morte. Oggi, sconfitto il re, la partita resta salvata ma i livelli sono ripuliti e non resta niente da trovare: contro il pilastro del § 2. Cosa comporta:
  - **Le difficoltà**, con nomi nostri (scelti da Mirco, v2.40): la luce che si spegne scendendo. **Crepuscolo** (*Dusk*), **Ombra** (*Shadow*), **Tenebra** (*Darkness*), poi l'**Abisso** (*Abyss*) in numeri romani: da I a VI per cominciare, quanti siano è un dato. Si parte dal Crepuscolo.
  - **Come si sceglie.** All'accampamento, da un altare accanto alla discesa nella cripta (o dal portale per la cripta), liberamente **tra quelle già completate e quella subito dopo**: uccidere il re a una difficoltà apre la successiva. Si può sempre tornare a una più bassa.
  - **Cambiare difficoltà rifà il mondo**: un seme nuovo per il dungeon, i ricordi dei livelli (ADR-079) ripartono, il re rinasce. Personaggio, equipaggiamento, zaino, oro e livello restano. Lo stesso altare permette di **rifare il mondo** senza cambiare difficoltà, per ricominciarlo con un dungeon nuovo, dopo una conferma.
  - **Nemici.** Il livello della zona (M8) sale con la difficoltà, per esempio +10 per gradino: vita, danno ed esperienza crescono con le formule che ci sono già. In più un moltiplicatore di vita e danno per difficoltà, un dato in una tabella. Dalle difficoltà alte il re prende anche i modificatori dei campioni (nemici speciali, sopra), uno in più per gradino, così lo scontro cambia e non è solo più lungo.
  - **Loot.** Il livello degli oggetti segue la zona, e quindi sale. Oggi però i 14 affissi chiedono al massimo un oggetto di livello 2: servono **gradi nuovi degli affissi** che si aprono più in alto (per esempio Forza +2–5 dal livello 1, +6–10 dal 12, +11–18 dal 25), con lo stesso nome o un nome più forte. È la parte che dà senso a salire. Le rarità si spostano verso magico e raro con la difficoltà (pesi della `RarityTable`); unici e set (sopra) cadono dall'Ombra in su.
  - **Il personaggio** sale fino al livello **50** (oggi 20), con la curva dell'esperienza della `ProgressionSettings` da riguardare.
  - **La morte** costa di più salendo (la *punizione alla morte*, sopra): una parte dell'oro e dell'esperienza, crescente con la difficoltà.
  - **Salvataggio.** Formato 6: difficoltà corrente, la più alta completata, il seme del mondo corrente; un salvataggio vecchio parte dal Crepuscolo, con la vittoria già fatta se c'era.
  - **Ordine consigliato:** nemici speciali (campioni, unici, forzieri: M12), poi le stanze della M13 (v2.45), poi i gradi degli affissi e le difficoltà (M14), poi unici e set. Ognuno si prova da solo, e le difficoltà arrivano quando c'è già un loot da cercare.
  - Circa 5–7 h per le difficoltà, più 2–3 h per i gradi degli affissi e la curva fino al 50.
- **Ronde dei nemici** (dai tester, 18 ott 2026). Oggi i nemici aspettano fermi finché non vedono il cavaliere. Alcuni camminano tra due o tre punti della loro stanza o del corridoio vicino, scelti dal seme, con una pausa a ogni punto; vedendo il cavaliere passano all'inseguimento come gli altri, e oltre il limite tornano alla ronda invece che a casa (ADR-050). I ricordi dei livelli (ADR-079) contano già i nemici per posto: un nemico vivo riparte dal suo punto di partenza. Circa 2–3 h.
- **Pubblicazione su itch.io** (spostata dalla M10 alla fine della M11 l'11 ott 2026, e dalla M11 a dopo la v1.0 il 17 ott 2026: decisioni di Mirco). Il primo passo della fase dopo la v1.0, con le scelte già prese nella D9 della M11:
  - **Pagina e chiave.** Mirco crea la pagina del gioco e una chiave API di butler, e la mette tra i secrets del repo (`BUTLER_API_KEY`): le credenziali le mette lui.
  - **La CI.** Sui tag `v*`, dopo la build Windows, un job la carica con butler sul canale `windows`. Il workflow non stampa mai la chiave, e i job delle pull request da fork non la ricevono.
  - **Prima in beta.** La pagina parte in modalità ristretta, con una chiave di download per ogni tester scelto da Mirco, revocabile; diventa pubblica quando Mirco lo decide, senza toccare la CI. La beta è anche la prova a più persone su altri PC che il piano chiedeva dalla fine della M10.
  - **Niente canale Web** alla prima uscita: salvataggio nel browser e prestazioni andrebbero riprovati da capo (ADR-019).
  - Circa 1 h, più la prova del primo caricamento.
- **Dalla lista di Mirco del 19 ott 2026**, fuori dalla M13 (D11): **mimic** (mostri travestiti da forziere) e **bauli maledetti** (un'ondata di nemici prima di aprirsi), **porte, leve e stanze sigillate**, **lore** (leggii, diari, pergamene da leggere), **prigionieri** da liberare, **statue che si animano**, pozze di acido e lava, statue che sputano fuoco o fulmini. I modelli che servono si fanno in Blender, come nella M13.
- **Boss a metà discesa** (dal piano dell'8 ott 2026 e dalla D11 della M12, dove era "da decidere dopo una prova"; ripreso il 22 ott 2026). Oggi c'è un solo boss, il re alla profondità 8, in fondo alle caverne. Un secondo boss alla fine della cripta, alla profondità 4, darebbe alla discesa una tappa a metà. Da decidere con Mirco: se serve, dopo le prove delle difficoltà; che nemico è (un unico più grande, con una sala sua e un tesoro suo, sul modello del re); se si batte una volta per mondo, come il re, e se la scala per le caverne si apre solo dopo. Circa 3–5 h, con il modello e la sala.
- **Pozzi e santuari anche nelle caverne** (dalla M13, ripreso il 22 ott 2026). Nella M13 nascono dai punti d'interesse delle stanze, e le caverne non hanno stanze: restano senza. Vanno negli slarghi dei cunicoli, con le probabilità e il limite della cripta (al più un pozzo e un santuario per livello). Quale pozzo e quale santuario lo dice un hash della cella e della profondità, come nella cripta, così gli arredi dei semi già provati non cambiano. Circa 1–2 h, con una foto in una caverna.
- **Due avanzi dei barili** (dalla M13, ripresi il 22 ott 2026).
  - **Gli incantesimi non li rompono.** Il Dardo e l'Anello di fuoco colpiscono solo chi ha una `Health`, e passano sui barili, anche su quelli esplosivi. Come in Diablo, li rompono anche gli incantesimi.
  - **Il barile piccolo del crollo** `CaveDecor_RockfallC` resta in piedi, mezzo tra le rocce, e non si rompe: si toglie dal crollo, o diventa un barile vero.
  - Circa 1 h.
- **I tempi di costruzione dei livelli in build** (dalla M11, ripresi il 22 ott 2026). La D4 della M6 vuole generazione, costruzione e NavMesh di un livello sotto i 50 ms. L'ultima misura in build è del passo 11.6 (14–30 ms). Al passo 11.7, con gli arredi, la cripta faceva 22–25 ms in editor, "da rimisurare in build alla prossima prova", e non si è più rimisurato. Poi sono arrivati scorte e forzieri (M12), barili, pozzi, santuari e trappole (M13). Si rimisura con il benchmark dell'11.6 su una build, nella cripta e nelle caverne; se si supera il limite, si guarda prima il NavMesh. Circa 1 h, alla prossima build.
- **Test notturni** (chiesti da Mirco il 22 ott 2026). **Fatti nella M15** (v1.3.1, 24 ott 2026) (scheda `docs/milestones/M15_La_ronda_notturna.md`, v2.60). A ogni push la CI rifà tutta la suite, ma con semi fissi: vede sempre gli stessi livelli. Tre errori veri sono usciti solo per caso, da un seme diverso: il portale del passo 10.9, la composizione davanti a una cassa dell'11.7, il portale chiuso tra i barili che ha fatto cadere il primo tag della v1.2.0. Di notte si provano i semi che i test di ogni push non vedono. Cosa comporta:
  - **Workflow.** `.github/workflows/nightly.yml` con `schedule` alle 3 e l'avvio a mano, nel gruppo di concorrenza `unity`. I test lunghi stanno nel progetto con la categoria `Nightly`, che i test di ogni push escludono.
  - **Cosa prova.** (a) **Generazione su molti semi**: 200–500 livelli per profondità, da un seme di partenza diverso ogni notte; uscita raggiungibile, casse, pozzi e santuari sul NavMesh, posto per il portale, niente sovrapposti. (b) **Partita automatica**: un PlayMode che scende tutte le profondità con il cavaliere invulnerabile e prova portali, salvataggio, ricarica e cambio di difficoltà, senza errori nel log. (c) **Le due misure**, loot e difficoltà, con più semi dei test. (d) **Tempi**: costruzione dei livelli contro i 50 ms della D4 della M6 e durata della suite. (e) **Salvataggi**: le fixture dei formati vecchi caricate e risalvate.
  - **Report.** Un riassunto in Markdown nella pagina del run e un file allegato. Ogni errore porta il **seme** che lo rifà. **Un'issue su GitHub solo quando qualcosa fallisce** (Mirco, 22 ott 2026), con il riassunto e il link al run; nessuna issue nelle notti verdi.
  - **Regole.** Di notte la suite di ogni push non si rifà: la esegue già ogni push (D3 della M15); girano solo i test della categoria `Nightly`, ogni notte, perché i semi cambiano. Ogni errore trovato diventa un test con il seme fisso nella suite normale, poi si corregge. Con il repo pubblico i minuti delle Actions non costano.
  - **Quando.** Subito dopo la chiusura della M14, prima della milestone successiva. Circa 4–6 h.
- **Lingue nuove.** Dalla M5 una lingua è una colonna della tabella delle stringhe, più lo schema dei nomi e il genere delle basi. Una lingua con un altro alfabeto vuole anche un font di riserva per TextMesh Pro, con la sua licenza (ADR-004).
- **Modelli generati con l'immagine di partenza da testo** (Mirco, 20 ott 2026, dopo la prova del passo 13.4). TRELLIS.2 rifà l'immagine che riceve: dalla foto della statua da script è uscita la stessa statua, più morbida e con quasi cinque volte le facce. Per i pezzi organici che gli script non fanno (una statua con un volto, radici, una fontana scolpita, resti di creature) l'immagine deve avere già il dettaglio. Cosa comporta:
  - **Immagine.** Z-Image-Turbo in nvfp4 (Apache 2.0; `z_image_turbo_nvfp4` 4,51 GB, `qwen_3_4b_fp4_mixed` 3,48 GB, `ae` 0,34 GB, scaricati da Mirco il 25 ott 2026 nella cartella dei modelli condivisa di ComfyUI Desktop), un terzo workflow in `Tools/ComfyUI` con un prompt nello stile KayKit e prompt e seme nella ricetta.
  - **Prova del 25 ott 2026.** Funziona: 1024×1024 in circa 5 s sulla RTX 5070 Ti, con 8 passi, `res_multistep`, cfg 1, shift 3 e l'encoder caricato come `lumina2`. Una statua di cavaliere su fondo neutro è uscita già adatta a TRELLIS.2, nello stile piatto di KayKit. Il server si avvia dall'installazione `DarkDescent` di ComfyUI Desktop sulla porta 8199, con il file dei percorsi dei modelli dell'app: l'app da sola non apre la porta dell'API.
  - **Conversione.** Un comando di `views.py` porta il GLB in FBX: assi di Unity, scala della cella, base a terra, davanti verso la stanza. Solo dopo la foto e il sì di Mirco.
  - **Materiale.** Ogni modello generato ha una texture sua, non l'atlante KayKit: un materiale per modello in Unity. Va bene per pochi pezzi.
  - **Limiti.** Mai per i nemici, che vogliono uno scheletro e le animazioni. Prima del primo modello nel gioco si legge la licenza di DINOv3 e si aggiunge all'ADR-096.
  - Circa 1–1,5 h, più le foto.
- **Ritocco della grafica con Z-Image-Turbo** (chiesto da Mirco il 25 ott 2026, dopo la prova del modello). Alcune parti della grafica sono rimaste come sono nate nelle prime milestone: texture, pannelli, sfondi e icone che oggi stonano accanto al resto. Si rifanno quelle che si notano di più, con immagini generate nello stile KayKit e non con uno stile nuovo. Cosa comporta:
  - **Prima la lista.** Un giro di foto di ogni schermata e di ogni tipo di livello: menu, città, cripta, caverne, inventario, pannello del personaggio, mercante, mappa, morte e vittoria. Mirco segna quello che trova brutto e Claude aggiunge i suoi candidati; si sceglie una lista corta, e ogni voce ha la foto di prima e quella di dopo. Da guardare per primi: pavimenti e muri di cripta e caverne visti dall'alto, dove la ripetizione dell'atlante può notarsi; sfondi e cornici dei pannelli; lo sfondo del menu principale e delle schermate di morte e vittoria; le icone di anelli e amuleti; macchie a terra (crepe, muschio, sangue) che rompono la ripetizione.
  - **Texture.** Devono ripetersi senza cuciture, e il modello da solo non lo garantisce: nella prova il pavimento si ripeteva, il muro no. Si usa una delle due tecniche: la generazione avvolta su sé stessa (un nodo che rende ripetibile il campionamento) o lo spostamento dell'immagine di mezzo lato con il ridisegno della sola croce delle cuciture. Lo script controlla le cuciture con un numero, come la prova del 25 ott. La palette viene dall'atlante KayKit.
  - **Pannelli.** Cornici e sfondi in nove parti, generati e ripuliti; i colori delle rarità e la leggibilità del testo non cambiano. Ogni pannello rifatto passa dalle foto della UI della M11.
  - **Ricette.** Ogni immagine ha accanto la sua ricetta (prompt, seme, modello, passi), come i suoni della M13, e si rigenera con un comando di `Tools/ComfyUI`. Niente entra negli asset senza il sì di Mirco sulla foto.
  - **Licenze.** Z-Image-Turbo è Apache 2.0 e le immagini generate sono nostre: l'ADR-096 dei suoni generati si estende alle immagini, con una riga in `CREDITS.md`.
  - **Limiti.** Lo stile resta KayKit: i *Personaggi nello stile di Diablo 2* restano una voce a parte, come modelli e animazioni. Le texture nuove vanno compresse o in atlante, senza raddoppiare la memoria delle build.
  - **Quando.** **È la M17** (scheda `docs/milestones/M17_Una_pelle_nuova.md`, v2.65), 7–10 h. Dalla verifica dello stato: l'atlante KayKit è a tinte senza dettagli, quindi pavimenti e muri non si ritexturizzano ma prendono macchie a terra; i pannelli sono 185 rettangoli a tinta unita.

- **La risalita e stanze più grandi** (chiesti da Mirco il 26 ott 2026, nel giro di foto della M17). Proposta come **M18**, dopo la M17. Cosa comporta:
  - **La risalita.** All'ingresso di ogni livello di cripta e caverne una scala in su che funziona e riporta alla profondità di sopra, all'uscita di quella; dalla profondità 1 all'accampamento. Come in Diablo 1. Tocca il generatore (la scala va nella stanza d'ingresso, raggiungibile, mai in mezzo agli arredi), il `LevelManager` (l'ingresso della profondità di sopra è la sua uscita in giù), i ricordi dei livelli e la ronda, che deve provarla.
  - **Stanze più grandi.** Oggi alcune stanze della cripta sono troppo piccole: si alza la dimensione minima e si guarda quanto spazio resta per arredi, punti d'interesse e combattimenti.
  - **Perché a parte.** Tutte e due cambiano i livelli di ogni seme: i test con i semi fissi, le soglie delle mappe e la ronda vanno ritarati, come alla M15. Circa 5–8 h.

---

## 6. Rituale di chiusura milestone

Dieci minuti di disciplina che tengono il progetto leggibile anche dopo un mese di pausa.

1. Definition of Done verificata **in build**, non nell'editor
2. Test verdi (da M2) e CI verde (da M2.5)
3. GIF per il README
4. `git tag m<N>` e push del tag (da M2.5 produce anche la build)
5. Build aggiornata su itch.io (da quando si pubblica, dopo la v1.0)
6. Tabella "Stato del progetto" aggiornata
7. Scheda della milestone successiva in `docs/milestones/`, scritta **prima** di cominciarla

Le schede si scrivono una alla volta, all'inizio di ogni milestone, con quello che si è imparato nella precedente. Scriverle tutte adesso vorrebbe dire riscriverle tutte dopo.

**Abitudini di ogni sessione:** un commit a fine sessione, sempre; un ADR per ogni scelta tecnica non ovvia; uno sguardo al Profiler appena qualcosa sembra lento, non solo in M11.

---

## 7. Competenze per il CV

| Competenza | Dove |
|---|---|
| C# applicato: interfacce, eventi, struct, generics | M2, M4, M5 |
| Unity: ciclo di vita, prefab, ScriptableObject, Animator | M1–M4 |
| Input System, NavMesh, Cinemachine | M1, M3 |
| Architettura: composition root, scene additive, logica testabile | M2, M3 |
| Test automatici (Unity Test Framework) | da M2, in ogni milestone |
| CI/CD (GitHub Actions + GameCI) | M2.5 |
| Architettura data-driven, sistemi di statistiche | M4, M5 |
| Generazione procedurale (BSP, random walk, verifica su grafo) | M6, M7 |
| Design di IA e refactoring guidato dal bisogno | M2, M7 |
| Tool custom per l'editor | M6 |
| Serializzazione e migrazione dei dati | M8 |
| Ottimizzazione: pooling e profiling | M9, M11 |
| URP, Shader Graph, illuminazione | M3, M11 |
| Git, LFS, licenze degli asset in un repo pubblico | M0, M1 |
| Blender (estensione opzionale) | dopo M11 |
| Pubblicazione e release | M11 |

**Come presentarlo:** repo pubblico con un README curato (GIF, schema dell'architettura, link agli ADR), build giocabile su itch.io dopo la v1.0, e 3–4 articoli tecnici sulle parti più interessanti: il generatore di dungeon, il sistema di affissi, la pipeline di CI, lo shader retro.

---

## 8. Protocollo di lavoro

La versione operativa è **`CLAUDE.md`** nel repo, che Claude Code legge a ogni sessione. In breve:

- **Claude scrive il codice, gameplay compreso:** file completi e funzionanti, non firme da riempire. Tu li rivedi, li provi, li integri.
- **Resta richiesto, in forma breve:** il perché delle scelte architetturali non ovvie (ti serve per gli ADR), le trappole Unity rilevanti, le conseguenze a distanza di una scelta, e una code review vera quando il codice lo scrivi o lo modifichi tu.
- **Niente parti didattiche:** né esercizi, né codice da scrivere o modificare a mano per imparare, né letture o esperimenti. A mano restano i passaggi nell'editor e le decisioni.
- **Niente devlog** dal 30 set 2026: `DEVLOG.md` cancellato dal repo, la voce della M0 resta nella storia git.
- **Rituale di chiusura** a ogni milestone (§ 6).

---

## 9. Rischi

| Rischio | Segnale d'allarme | Mitigazione |
|---|---|---|
| Calo di motivazione | Due settimane senza commit | Milestone piccole con un risultato visibile; build pubblica da M3; la tabella di stato rende facile ripartire |
| Stime sballate | Settimane di calendario oltre 1,5 volte la stima (§ 1.3) | Punti di controllo a M2 e M5, poi linee di taglio (§ 1.2) |
| Feature creep | Codice per cose fuori ambito | `ICEBOX.md`, ambito scritto (§ 1.1) |
| Collo di bottiglia sull'arte | Ore in Blender prima di M11 | Grey-box e CC0; lo shader retro uniforma asset diversi |
| Licenze degli asset | Un FBX di Mixamo o dell'Asset Store in un commit pubblico | ADR-004 prima del passo 1.4; `CREDITS.md` |
| Quote di GitHub | Avvisi di consumo su LFS | Build in CI solo sui tag, cache LFS, controllo periodico in *Settings → Billing* |
| Documentazione obsoleta | API che "non esistono" | Documentazione per la versione 6000.3; tabella del § 3 |

---

## Appendice — Da v1 a v2

Il piano v1 era tecnicamente corretto ma organizzato per sistemi, senza criteri di completamento, senza la parte che non è gameplay (Git, build, test, CI), senza un ambito definito, e si chiudeva con un prompt che chiedeva tutti gli script in una volta. La v2 lo ha sostituito con milestone verticali, Definition of Done osservabili, una M0 dedicata all'infrastruttura, un ambito scritto e un protocollo di lavoro esplicito (§ 8).

Le correzioni tecniche puntuali della v2.0 — Input System nuovo, `DamageInfo` come struct, camera in `LateUpdate` con rotazione `(30, 45, 0)`, `ObjectPool<T>` di Unity, ScriptableObject immutabili, disiscrizione dagli eventi in `OnDisable`, state machine a `enum` finché basta — sono ora assorbite dove servono: nelle milestone di questo documento, in `CONVENTIONS.md`, in `CLAUDE.md` e nelle schede.
