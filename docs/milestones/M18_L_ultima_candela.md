# M18 — "L'ultima candela"

**Cosa deve succedere (Definition of Done):** il gioco ha due **filmati**, fatti nel motore con i
modelli, le luci e le animazioni del gioco. L'**apertura**, "L'ultima candela", parte con *Nuova
partita* e racconta in 45–60 s il borgo che si spegne, gli scheletri che escono dalla terra, i
sopravvissuti all'ingresso della cripta e il cavaliere che scende; finisce nell'accampamento, con il
cavaliere in mano a chi gioca. La **vittoria** parte la prima volta che si batte il Re delle ossa a
ogni difficoltà: il colpo che rallenta, il re che si sfalda, l'uscita verso la luce, i sopravvissuti
all'alba, poi la finestra delle statistiche. Tutti e due si saltano con Esc o con un click, hanno le
didascalie nelle due lingue e la loro musica. Ogni inquadratura passa dal sì di Mirco, prima in foto
e poi in un video. Esce con il tag `v1.6.0`.

**Tempo stimato:** 14–20 h (D15).
**Prerequisito:** M17 chiusa (tag `v1.5.0`, 27 ott 2026).

**Come si lavora:** come alle M13–M17. Il codice, le scene, i modelli in Blender, gli script e i
passaggi nell'editor li faccio io, in batchmode a Unity chiuso, e ComfyUI lo avvio io sulla porta
8199. A Mirco restano le decisioni qui sotto, il sì sui download (D2, D5, D10), sulle foto delle
inquadrature e sui video dei due filmati, e la prova in build.

---

## Stato verificato il 27 ottobre 2026

| Cosa | Stato |
|---|---|
| Pacchetti | **Cinemachine 3.1.7** c'è già e muove la camera del gioco (Position Composer, rotazione e zoom). **Timeline** e **Recorder** non ci sono. Il modulo `video` di Unity è attivo, ma nessuna scena usa un `VideoPlayer`. ffmpeg non è installato |
| Animazioni | Dal pacchetto completo *KayKit Character Animations 1.1* (CC0, lo zip è in `Downloads`) sono importati 3 file su 8 del rig medio: `CombatMelee`, `General`, `MovementBasic`. Mancano `Simulation` (`Cheering`, `Waving`, `Sit_Floor_*`, `Lie_*`), `Special` (`Skeletons_Awaken_Floor`, `Skeletons_Awaken_Floor_Long`, `Skeletons_Death`, `Skeletons_Taunt`, `Skeletons_Walking`), `Tools` (`Holding_A/B/C`, `Dig`), `MovementAdvanced` (`Crouching`, `Sneaking`, `Crawling`) e `CombatRanged`. Tutti per il rig medio, lo stesso di cavaliere e scheletri |
| Personaggi | Importati `Knight`, `Barbarian`, `Rogue_Hooded` e i sette scheletri. Nello zip *Adventurers 2.0 FREE* restano `Mage`, `Ranger`, `Rogue`. Il Re delle ossa usa i modelli degli scheletri, più grande, con l'`overrideController` degli scheletri |
| Mondo | Il pacchetto *Dungeon* ha 211 modelli, 87 nel gioco: è tutto interno. **Non ci sono case, alberi, lapidi, recinti, tende né un trono**, in nessun pacchetto scaricato. `Tools/Blender` fa modelli nello stile KayKit (ADR-094): l'altare, le anfore, la gabbia, le catene e gli altri della M13 |
| Accampamento | È una **sala sotto terra** (`Data/Levels/Town.txt`, 13 × 10 celle): il fuoco, due tavoli, il banco del mercante, l'altare delle difficoltà e la scala verso la cripta. **Il gioco non ha un fuori**: l'alba, il borgo e l'ingresso della cripta vanno costruiti |
| Vittoria oggi | Morto il re, `CompositionRoot.HandleBossDefeated` salva, apre il portale della vittoria nella sala e sblocca il tesoro. La prima volta a ogni difficoltà (`_firstVictory`) la finestra si apre arrivando in città dal portale (`HandleVictoryReached`); le volte dopo c'è solo la scritta "Il re è caduto" |
| Nuova partita | Menu, scelta dello slot, accampamento. Con `-newgame` e `-seed` dalla riga di comando, e nei test, non passa dal menu |
| Musica | Quattro brani CC0 da OpenGameArt (cripta, caverne, boss, accampamento) e un fondo di fuoco. Nessun brano per i filmati |
| ComfyUI | Nella cartella condivisa ci sono Z-Image-Turbo, TRELLIS.2 e Stable Audio 3 small SFX: **nessun modello di video né di musica**. La scheda è una RTX 5070 Ti Laptop con 12 GB; sul disco C ci sono 686 GB liberi |
| Test | 486 EditMode e i PlayMode a ogni push, più quelli della ronda. `PhotoTour` (assembly `DarkDescent.Tests.Photos`) fa le foto fuori dalla CI |

---

## Decisioni

Da confermare con Mirco.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M18 "L'ultima candela"**, tag **`v1.6.0`**. Dentro: i due filmati, il set esterno, le animazioni nuove, la musica, il modo di saltarli e le didascalie. Il gioco non cambia regole | Due filmati si vedono da chi gioca: una versione minore. Il nome è quello dell'apertura, che racconta la storia di tutto il gioco |
| D2 | **Il montaggio** | **Unity Timeline** (pacchetto ufficiale, gratuito, dal registro di Unity) con le camere di **Cinemachine** che ci sono già. Ogni filmato è una scena con il suo `PlayableDirector`, e lo **costruisce uno strumento dell'editor dal codice**, `FilmTools`, come la città dalla sua mappa: si rifà con un comando e si rilegge nel diff. La lista delle inquadrature (inizio, durata, camera, didascalia) è una classe pura in `Core`, `FilmScript`, che si prova in EditMode. Download con il sì di Mirco | Timeline mette in fila camere, animazioni, luci, suoni e didascalie con un tempo solo, che è il lavoro di un filmato. Scritta a mano con le coroutine sarebbe più codice e niente anteprima. Costruirla dal codice serve a me, che lavoro a Unity chiuso |
| D3 | **Il fuori** | Una scena sola per i filmati, **`Film_Outside`**: il borgo sulla collina, il cimitero, l'ingresso della cripta con le tende dei sopravvissuti, con **due luci**, la notte e l'alba. La usano tutti e due i filmati. Il fuori **non è giocabile**: niente NavMesh, niente collider, niente salvataggio | Il gioco è tutto sotto terra, e l'alba la vedono solo i filmati. Un set solo per due filmati costa la metà, e tornarci alla fine chiude il giro: si esce da dove si è entrati |
| D4 | **L'accampamento dentro e fuori** | Le tende stanno **fuori**, davanti all'ingresso; l'accampamento del gioco resta **la prima sala sotto l'ingresso**, dove i sopravvissuti hanno portato le loro cose. Nell'apertura il mercante accende l'ultima candela sull'**altare delle difficoltà**, che è dentro: il cavaliere scende i gradini dietro di lui, la torcia si stringe, il titolo, e il gioco comincia lì, nella sala. Nella vittoria si fa la strada al contrario, dalla scala verso la luce | Così la storia porta esattamente dove si comincia a giocare, e l'altare (la luce che si spegne, M14) diventa l'ultima candela della storia. L'altra strada, l'altare fuori, lascerebbe due altari |
| D5 | **Il borgo** | Modelli **CC0 di KayKit**, nello stesso stile: per le case e gli alberi il pacchetto *Medieval Hexagon* (le case senza le basi esagonali), per lapidi, recinti, bare e lanterne *Halloween Bits*, per rocce e cespugli *Forest Nature*. Licenza, contenuto e dimensioni si leggono sulla pagina **prima di scaricare**, con il sì di Mirco. Quello che manca (le tende, la collina, il cancello della cripta) lo faccio in Blender. La riserva, se un pacchetto non va: tutto in Blender, con più ore | Lo stesso autore tiene lo stile uguale al resto del gioco, gratis. Blender da solo per un borgo intero costerebbe le ore di metà milestone |
| D6 | **Le comparse** | I paesani sono `Mage`, `Ranger`, `Rogue` e `Rogue_Hooded` **senza armi**, con le tinte cambiate e una lanterna in mano (`Holding_A` sulla parte alta del corpo, la corsa sotto, con una maschera dell'Animator). Il cavaliere è il modello del gioco; nella vittoria porta l'**equipaggiamento di chi gioca**. Il mercante è il suo. Al più 8 comparse in una inquadratura | Adventurers è l'unico pacchetto di persone nello stile. Il cavaliere della vittoria deve essere il tuo, non uno qualunque |
| D7 | **L'apertura** | Le cinque scene approvate, in circa 50 s: (1) il borgo di notte, le candele alle finestre che si spengono una a una; (2) il cimitero, gli scheletri che escono dalla terra (`Skeletons_Awaken_Floor`) e i paesani che scappano con le lanterne; (3) l'alba, le tende davanti all'ingresso della cripta, i sopravvissuti seduti a terra; (4) arriva il cavaliere, il mercante indica la cripta, e la didascalia: il Re delle ossa si è svegliato; (5) giù per i gradini nella sala, il mercante accende l'ultima candela sull'altare, la torcia si stringe, il titolo. **A ogni *Nuova partita***, non quando si carica; mai con `-newgame` né nei test | È la storia scelta da Mirco, con l'altare spostato dentro per la D4. Chi comincia una partita nuova la vede, chi la sa la salta |
| D8 | **La vittoria in due tempi** | **Primo tempo, nella sala** (circa 5 s), appena muore il re: il tempo rallenta a un quarto sul colpo finale, una camera si stringe sul re che si sfalda (`Skeletons_Death`), la musica tace, poi si torna a giocare. **Secondo tempo**, attraversando il portale della vittoria, dove oggi si apre la finestra: il cavaliere guarda il mucchio d'ossa dov'era il re e si avvia; stacco sull'ultima scala, con la luce che entra da fuori; fuori all'alba, i sopravvissuti che si alzano ed esultano (`Cheering`); poi la finestra con le statistiche, *Continua* e *Menu principale*. Tutti e due **la prima volta a ogni difficoltà**, come la finestra di oggi | Il tesoro e il bottino del re restano a chi gioca: un filmato che li apre al posto suo li toglierebbe. Il trono non c'è, e metterlo cambierebbe la sala del re, i suoi semi e la ronda; il posto vuoto del re è il mucchio d'ossa che resta. È la scena approvata, divisa in due perché in mezzo si gioca |
| D9 | **Saltare e guardare** | Esc o un click: il primo mostra in basso «Esc o click per saltare», il secondo salta. Saltata l'apertura si è nell'accampamento, saltata la vittoria si va alla finestra. Durante un filmato il gioco non legge comandi e non salva. **Bande nere** sopra e sotto (2,35:1), le didascalie in Cinzel con una dissolvenza. Nessuna voce | Due tasti per saltare evitano di perdere un filmato per sbaglio, come in Diablo. Le bande dicono "questo è un filmato" senza parole |
| D10 | **La musica** | Prima si prova **ACE-Step** in ComfyUI (musica da testo), come Z-Image alla M16: un brano per l'apertura e uno per la vittoria, ognuno con la sua ricetta in `Tools/ComfyUI/music/`, e una pagina di ascolto per Mirco. Se non convince, un **brano CC0** di OpenGameArt come gli altri quattro. I suoni (la candela che si spegne, la terra che si rompe, il vento, la folla) con Stable Audio, che c'è già. Licenza e dimensione dei pesi si leggono prima di scaricare, con il sì di Mirco | Una musica fatta per il filmato segue le scene, un brano trovato va tagliato. La prova costa poco e c'è una riserva sicura |
| D11 | **I modelli di video** | Si provano **Wan 2.2 TI2V 5B** e **LTX-Video** su un render del set (D3), in **1 h al massimo**, solo per gli stacchi che il motore fa male: fumo, polvere, la luce che entra dall'uscita. **Mai per i personaggi**, che cambiano forma da un fotogramma all'altro. Una clip entra solo con il sì di Mirco, come `VideoPlayer` dietro la scena; altrimenti tutto resta nel motore, e la prova finisce nelle lezioni. Licenze e pesi si leggono prima di scaricare | Chiesto da Mirco. Un filmato nel motore si vede alla risoluzione dello schermo e pesa poco; una clip generata pesa megabyte e può stonare. Un tempo fisso tiene la prova nelle ore |
| D12 | **Il video da mostrare** | **Unity Recorder** (pacchetto ufficiale, dal registro di Unity) registra i due filmati in MP4 1920×1080 da un test della categoria `Photos`, come il giro di foto della M17. I video vanno nelle release e nel README come GIF corte, non nel repo interi. Download con il sì di Mirco | Senza ffmpeg, Recorder è la strada più corta per un MP4. Il video serve a Mirco per il sì e a chi guarda il repo |
| D13 | **Il sì di Mirco** | Tre giri: le **foto delle inquadrature** (un fotogramma per scena, da `PhotoTour`) prima di montare, poi il **video** di ogni filmato, poi la prova in build. Si cambia quello che non convince a ogni giro | Una foto si corregge in minuti, un filmato montato in ore. Come alla M17, niente entra senza il sì |
| D14 | **Si controlla** | EditMode: `FilmScript` (inquadrature in fila senza buchi, durata dentro i limiti, ogni didascalia nelle due lingue), quando parte la vittoria (prima volta sì, seconda no). PlayMode: *Nuova partita* dal menu avvia l'apertura e finisce nell'accampamento con il cavaliere libero; Esc più Esc salta in meno di un secondo; il re battuto la prima volta fa il primo tempo, il portale il secondo e poi la finestra; la seconda volta nessun filmato; dopo un filmato il tempo torna a 1 e la camera al cavaliere. La ronda non cambia | I filmati toccano il tempo, la camera e i comandi del gioco: se uno di questi non torna com'era, il gioco è rotto. I test lo dicono prima di Mirco |
| D15 | **Stima e linee di taglio** | **14–20 h**. Se servirà tagliare: prima le clip generate (D11), poi la scena 2 dell'apertura con meno comparse, poi la musica generata (resta un brano CC0), poi il video per il README. I due filmati interi, il salto e le didascalie non si tagliano | Meglio deciderlo adesso che a metà |
| D16 | **Fuori da questa milestone** | Filmati per la morte o per ogni difficoltà; voci e labiale; filmati prerenderizzati come quelli di Blizzard; un pulsante del menu per rivederli; il fuori giocabile; la risalita e le stanze più grandi (M19) | Ognuna è un lavoro a sé. Un pulsante per rivederli si aggiunge in poco dopo, se Mirco lo vuole |
| D17 | **Dimensioni e prestazioni** | Il limite è **+40 MB** sulla build: le animazioni, il set, due brani e, se entrano, le clip. I filmati girano a **60 fps** sul PC di sviluppo in build; le candele del borgo sono materiali emissivi con poche luci vere, perché URP ne regge un numero limitato per oggetto. Si misura alla chiusura come alla M17 | Il set esterno è la scena più grande del gioco: le luci sono il primo rischio |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 18.1 | Gli attrezzi: Timeline e Recorder (con il sì), le animazioni nuove importate, `FilmScript` in `Core`, il lettore dei filmati (salto, bande, didascalie, comandi e tempo fermi), i test (D2, D9, D12, D14) | 2–2,5 |
| 18.2 | La prova dei modelli in ComfyUI: ACE-Step per la musica, Wan 2.2 e LTX-Video per gli stacchi, con le pagine per Mirco; si decide cosa entra (D10, D11) | 1,5–2 |
| 18.3 | Il fuori: i pacchetti del borgo (con il sì), le tende, la collina e il cancello in Blender, `Film_Outside` con la notte e l'alba, le foto per Mirco (D3, D5, D17) | 3–4 |
| 18.4 | L'apertura: le cinque scene, le comparse, le luci che si spengono, le didascalie, il collegamento a *Nuova partita*; foto, poi video per Mirco (D4, D6, D7, D13) | 3–4 |
| 18.5 | La vittoria: il primo tempo nella sala, il secondo dal portale, il cavaliere con l'equipaggiamento di chi gioca, la finestra alla fine; foto, poi video per Mirco (D8, D13) | 2,5–3,5 |
| 18.6 | Musica e suoni dei due filmati, il volume con le opzioni, i crediti (D10) | 1–1,5 |
| 18.7 | Misura, video e GIF per il README, build da provare e chiusura: ADR, lezioni, piano, `bundleVersion` 1.6.0, tag `v1.6.0` dopo i test verdi in CI (D17) | 1–1,5 |

---

## Checklist di chiusura

- [ ] Decisioni D1–D17 confermate
- [ ] I download approvati: Timeline, Recorder, i pacchetti del borgo, i modelli di ComfyUI
- [ ] L'apertura e la vittoria approvate da Mirco in video
- [ ] Test EditMode e PlayMode verdi in locale e in CI
- [ ] La build misurata dentro il limite della D17
- [ ] ADR, lezioni nel piano, README, `CREDITS.md` e `Data/Credits.txt`
- [ ] `bundleVersion` 1.6.0 e tag `v1.6.0` dopo i test verdi in CI
