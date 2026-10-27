# M17 — "Una pelle nuova"

**Cosa deve succedere (Definition of Done):** le parti della grafica che stonano di più sono rifatte,
nello stile KayKit e non in uno stile nuovo. Prima si fotografa ogni schermata e ogni tipo di livello,
e con Mirco si sceglie una **lista corta**. Poi i **pannelli** dell'interfaccia, oggi rettangoli a
tinta unita, prendono una cornice e uno sfondo; pavimenti e muri di cripta e caverne prendono
**macchie a terra** (crepe, muschio, sangue, polvere) che rompono la ripetizione; menu, morte e
vittoria prendono uno **sfondo**. Le immagini nascono da **Z-Image-Turbo** in ComfyUI, ognuna con la
sua ricetta nel repo, e niente entra nel gioco senza il sì di Mirco sulla foto di prima e quella di
dopo. Esce con il tag `v1.5.0`.

**Tempo stimato:** 7–10 h (D14); con i menu della lista del 17.1, **9–12 h**.
**Prerequisito:** M16 chiusa (tag `v1.4.0`, 26 ott 2026).

**Come si lavora:** come alle M13–M16. Il codice, gli script, le immagini e i passaggi nell'editor li
faccio io, in batchmode a Unity chiuso, e ComfyUI lo avvio io sulla porta 8199. A Mirco restano le
decisioni qui sotto, la lista corta del giro di foto (passo 17.1), il sì su ogni immagine prima che
entri e la prova in build.

---

## Stato verificato il 26 ottobre 2026

| Cosa | Stato |
|---|---|
| Strumenti | `Tools/ComfyUI/` guida ComfyUI dall'API locale (ADR-096): `comfy.py` con la sola libreria standard, `make.py` per suoni e modelli da ricetta, i workflow `stable_audio_sfx.json` e `trellis2_image_to_model.json`. **Nessun workflow per le immagini**: la prova di Z-Image del 25 ott è uno script dello scratchpad. ComfyUI si avvia senza l'app (`Tools/README.md`); Pillow c'è solo nel suo ambiente Python |
| Z-Image-Turbo | Nella cartella condivisa dei modelli: `z_image_turbo_nvfp4` (Apache 2.0), l'encoder `qwen_3_4b_fp4_mixed` (Qwen3, Apache 2.0), il VAE `ae.safetensors`. 1024×1024 in circa 5 s: 8 passi, `res_multistep`, cfg 1, shift 3. Nella prova il pavimento si ripeteva, il muro aveva le cuciture, la statua andava bene come partenza per TRELLIS |
| Mondo | Pavimenti, muri e arredi KayKit prendono il colore da un **atlante di 32 sfumature** (`dungeon_texture.png`, 1024×1024): non hanno dettagli disegnati, solo tinte. Il filtro retrò della M11, se acceso, porta ogni pixel del mondo a una palette di 32 colori **prima** dell'interfaccia |
| Interfaccia | Due canvas in sovrimpressione in `Core`: il filtro retrò non li tocca. **185 componenti grafici senza immagine**, cioè pannelli, cornici e celle a tinta unita; le immagini sono poche (le sfere di vita e mana, la spunta, le icone). Il font è Cinzel (ADR-054). I colori delle rarità stanno in `RarityColors` |
| Icone | 23 icone degli oggetti di 128×128, **renderizzate dai modelli** da `ItemTools.RenderIcons`: corrispondono a quello che sta a terra. Unici e pezzi di set usano quella della base (D16 della M16). Più 6 immagini degli incantesimi, della cornice di selezione e del cristallo dei santuari |
| Sfondi | **Nessuna immagine di sfondo** in tutta l'interfaccia: menu, morte e vittoria sono veli e pannelli a tinta unita. Le sole immagini sono le sfere, la spunta, la cornice e il cristallo degli incantesimi e le icone |
| Foto | Nessuno strumento nel repo: le foto della M14 sono uscite da un test temporaneo. In `docs/media` ci sono le GIF e le foto del README |
| Test | 466 EditMode e 252 PlayMode a ogni push, più 6 EditMode e 12 PlayMode della ronda |

---

## Decisioni

Confermate da Mirco il 26 ottobre 2026, tutte come proposte ("confermate tutte, procedi con il
17.1").

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M17 "Una pelle nuova"**, tag **`v1.5.0`**. Dentro: il giro di foto, i pannelli, le macchie a terra, gli sfondi, le voci della lista corta che restano nelle ore, gli strumenti per le immagini. Il gioco non cambia: solo come si vede | Si nota in ogni schermata, anche se le regole restano quelle della v1.4: una versione minore. In alternativa `v1.4.1`, se Mirco la vede come un ritocco |
| D2 | **Il giro di foto** | Uno strumento PlayMode, `PhotoTour`, nella categoria **`Photos`**, che né i push né la ronda eseguono: costruisce le schermate con i semi fissi e le salva in 1920×1080, **con il filtro retrò spento e acceso**. Le schermate: menu, accampamento, cripta e caverne viste dall'alto con e senza nemici, inventario con il tooltip di un unico, scheda del cavaliere, mercante, altare, mappa, cintura e incantesimi, morte e vittoria, circa 16. Le foto di prima vanno in `docs/media/m17/prima`, quelle di dopo in `dopo`, con gli stessi nomi | Rifare le foto a ogni passo con lo stesso comando permette di confrontarle una a una. Le foto restano nel repo: sono il prima e il dopo della milestone |
| D3 | **La lista corta** | Con le foto, una pagina HTML locale con le schermate in griglia. **Mirco segna** quello che trova brutto, io aggiungo i miei candidati, e si sceglie **al più 8 voci**, in ordine. Le voci certe sono le tre di D4, D5 e D6; le altre vengono dalle foto | Una lista corta finisce nelle ore, e ogni voce ha il suo prima e dopo. Le voci fuori dalla lista restano nel piano |
| D4 | **Pannelli** | Una famiglia sola per tutti i pannelli: una **cornice in nove parti** (angoli e bordi, scalabile senza deformarsi) e uno **sfondo che si ripete** (pietra scura o cuoio, scelti con le foto), più una variante per tooltip e celle. Li applica uno strumento dell'editor, `UiSkin`, che mette le immagini sui componenti per nome: si rifà con un comando, non a mano. I colori delle rarità, le sfere e le icone non cambiano | 185 rettangoli a tinta unita sono la cosa che si nota di più accanto ai modelli. Uno strumento che li riveste tiene la scena uguale per il resto, e torna indietro togliendo le immagini |
| D5 | **Macchie a terra** | Immagini con la trasparenza (crepe, muschio, sangue secco, polvere e ossa sparse; 6–10 in un atlante solo) su quad appoggiati al pavimento. Le mette il popolatore di cripta e caverne con un **tiro a parte**, con un dominio suo del seme (`SeedMixer.ForDecals`), 0–3 per stanza e qualcuna nei corridoi, il tema della stanza sceglie quali. **Niente collider né NavMesh**, nessun ricordo nel salvataggio (rinascono dal seme), mai sopra scale, trappole, pozzi, santuari e casse | L'atlante KayKit è a tinte: una texture con dettagli sui modelli cambierebbe lo stile. Le macchie aggiungono dettaglio sopra, si spengono togliendole, e il tiro a parte lascia uguali arredi, loot e livelli già provati, come l'ADR-110 per gli unici |
| D6 | **Sfondi** | Menu principale, morte e vittoria prendono un'**illustrazione** a tutto schermo, scura sotto i testi: l'ingresso della cripta, una tomba aperta, l'alba sull'accampamento. In stile diorama low poly, come i modelli KayKit. Se una non convince, al suo posto un **render del gioco** con la camera del menu | L'immagine di partenza conta più di tutte le altre: è la prima cosa che si vede. Il render del gioco è la riserva sicura, perché è già nello stile |
| D7 | **Icone** | Restano renderizzate dai modelli. Se le foto le segnano (anelli e amuleti sono le candidate), si migliora il **render** (luce, angolo, contorno) e si aggiunge uno **sfondo della cella** generato, non un'icona disegnata | Un'icona disegnata non somiglierebbe più all'oggetto a terra |
| D8 | **Texture che si ripetono** | Per sfondi dei pannelli e macchie: l'immagine si **sposta di mezzo lato** e si ridisegna solo la croce delle cuciture, con una maschera sul latente. Non servono nodi nuovi da scaricare. Lo script misura le cuciture con un numero (la differenza tra i bordi opposti, contro quella tra due righe vicine all'interno) e scarta i semi sopra la soglia | Il modello da solo non garantisce la ripetizione, come ha mostrato il muro della prova. Un numero decide senza guardare cento immagini |
| D9 | **Ricette e strumenti** | Il workflow `z_image_turbo.json` in formato API, e una ricetta per immagine in `Tools/ComfyUI/images/` con prompt, negativo, seme, dimensione e finitura (ritaglio, trasparenza, ripetizione, nove parti, riduzione). `make.py` le genera come i suoni, con `--takes` per provare più semi. La finitura usa Pillow, nei requirements di `Tools/ComfyUI`. Le immagini finite vanno in `Art/Generated/`, ognuna rifatta identica dalla sua ricetta | Come i suoni della M13: un'immagine che non si può rifare non si può nemmeno correggere |
| D10 | **Il sì di Mirco** | Ogni immagine passa da una **pagina di confronto**, accanto alla foto di prima, con 3–4 semi. Mirco sceglie, o chiede un altro giro. **Niente entra negli asset senza il sì**; dopo, la foto di dopo nel gioco vero | Le immagini sono gusto, non numeri. Come la pagina di ascolto dei suoni |
| D11 | **Licenze** | Z-Image-Turbo e l'encoder Qwen3 sono Apache 2.0, e le immagini generate sono nostre. Il VAE `ae.safetensors` è quello di FLUX.1: **prima del primo asset** si verifica da quale modello viene e con che licenza. L'ADR-096 si estende alle immagini con un ADR nuovo, e `CREDITS.md` ha una riga per le immagini generate | Come per i suoni: la licenza si scrive prima che un file entri, non dopo |
| D12 | **Memoria e dimensioni** | Le immagini entrano compresse, con una dimensione massima per tipo: pannelli 512, macchie in un atlante da 1024, sfondi 1920×1080. Le immagini dell'interfaccia senza mipmap, le macchie con. Si misura la **dimensione della build** e la memoria delle texture prima e dopo; il limite è **+25 MB** sulla build | Ritoccare la grafica non deve raddoppiare la build. Un limite scritto si controlla |
| D13 | **Si controlla** | Test EditMode: le macchie dal seme (stesso seme, stesse macchie; livelli di sempre uguali), mai sopra i pezzi, nell'atlante; ogni pannello rivestito; il contrasto tra testo e sfondo dei pannelli almeno **4,5:1**, misurato sulla media dello sfondo. Test PlayMode: un livello con le macchie senza collider né ostacoli, i pannelli nelle due lingue dentro lo schermo. La ronda conta le macchie e tiene il limite dei 150 ms | Le macchie toccano il popolatore, che la ronda prova ogni notte; il contrasto è la cosa che uno sfondo con texture rompe per prima |
| D14 | **Stima e linee di taglio** | **7–10 h**. Se servirà tagliare: prima le voci della lista oltre le tre certe, poi le icone, poi gli sfondi di morte e vittoria (resta quello del menu). Il giro di foto, i pannelli e le macchie non si tagliano | Meglio deciderlo adesso che a metà |
| D15 | **Fuori da questa milestone** | Modelli nuovi e modelli da TRELLIS; i **personaggi nello stile di Diablo 2**; luci, ombre e shader nuovi; animazioni; font; icone proprie di unici e set; il forziere personale | Ognuna è un lavoro a sé, già nel piano. Qui si riveste quello che c'è |
| D16 | **Il filtro retrò** | Le macchie passano dal filtro come il resto del mondo; le foto si fanno con il filtro spento e acceso, e un'immagine deve reggere tutte e due. Interfaccia e sfondi non passano dal filtro | Con la palette a 32 colori i dettagli fini spariscono o fanno rumore: si vede solo provando |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 17.1 | Il giro di foto: `PhotoTour` nella categoria `Photos`, le foto di prima, la pagina per Mirco, la lista corta approvata (D2, D3) | 1 |
| 17.2 | Le immagini in `Tools/ComfyUI`: workflow di Z-Image, ricette, `make.py` per le immagini, la ripetizione con la misura delle cuciture, la trasparenza, la pagina di confronto; licenza del VAE, ADR e `CREDITS.md` (D8, D9, D10, D11) | 1,5–2 |
| 17.3 | I pannelli nello stile scuro e pulito: cornice in nove parti, sfondo, pulsanti che si illuminano al passaggio, `UiSkin`, il contrasto (D4, D13) | 1,5–2 |
| 17.4 | I menu: principale con il pulsante *Riconoscimenti*, opzioni riordinate per gruppi, pausa, vittoria senza riconoscimenti con *Continua* e *Menu principale*, morte; stringhe nelle due lingue e test (lista del 17.1) | 1,5–2 |
| 17.5 | Le macchie a terra: atlante, tiro a parte nel popolatore di cripta e caverne, test e ronda (D5, D13, D16) | 1,5–2 |
| 17.6 | Gli sfondi di menu, morte e vittoria (D6) | 1 |
| 17.7 | Misura (dimensione della build, memoria, tempi dei livelli), foto di dopo, build da provare e chiusura: ADR, lezioni, piano, README con le foto, `bundleVersion` 1.5.0, tag `v1.5.0` dopo i test verdi in CI (D12) | 1–1,5 |

---

## Passo 17.1 — Il giro di foto

**Com'è andata (26 ott 2026).** `PhotoTour` sta in un assembly suo, `DarkDescent.Tests.Photos`, con la
categoria `Photos`: i workflow eseguono solo gli assembly che elencano (`-assemblyNames` EditMode e
PlayMode), quindi né i push né la ronda lo vedono, senza toccare i workflow. Si lancia con

```
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -assemblyNames DarkDescent.Tests.Photos -photos-out <cartella>
```

e scrive 17 schermate in JPEG 1920×1080 (qualità 88): menu, opzioni, accampamento, mercante,
altare, inventario con il tooltip di un pezzo del Custode, scheda, cripta (ingresso, stanza con
quattro scheletri fermi, loot a terra con i nomi, mappa sovrapposta), caverne (ingresso e stanza),
pausa, vittoria, riconoscimenti e morte. Quelle con il mondo dietro anche con il filtro retrò pieno
(`_retro`), 32 file e 7,5 MB in `docs/media/m17/prima`. Il seme del mondo è fisso (20261026), il
cavaliere è di livello 20 con un raro per slot (`PrepareHero`), i nemici sono fermi. La risoluzione
la impone `PlayModeWindow.SetCustomRenderingResolution`; la foto è `ScreenCapture`, che prende anche
l'interfaccia in sovrimpressione.

La pagina per scegliere è `docs/media/m17/giro_di_foto.html`: le foto in griglia con il filtro da
accendere, «Brutta» e una nota per ognuna, i miei candidati da spuntare e «Copia la lista». I
candidati, dalle foto: i pannelli e i pulsanti a tinta unita, il menu e le opzioni su nero, i
pavimenti senza macchie (le tre certe); vittoria e morte, la barra in basso senza cornice, celle e
slot spogli (anelli e amuleto senza cella), l'icona dell'anello (un cerchio sottile), la cornice
della minimappa.

### La lista corta

**Approvata da Mirco il 26 ottobre 2026**, dalla pagina e con quattro risposte in chat. Dentro la M17,
in ordine:

1. **Pannelli e pulsanti** (D4), nello stile **scuro e pulito** scelto da Mirco, come i Diablo recenti:
   sfondi quasi neri, cornici sottili in metallo scuro, molto spazio, testo grande, pochi elementi,
   pulsanti che si illuminano al passaggio. Prende il posto della «pietra scura o cuoio» della D4.
2. **Menu principale** (D6), "troppo old stile": lo sfondo, una disposizione nuova e un pulsante
   **Riconoscimenti**, che oggi si vedono solo dopo la vittoria.
3. **Opzioni**: riordinate per gruppi (lingua, audio, grafica, interfaccia) e nello stile nuovo.
4. **Pausa**: nello stile nuovo.
5. **Vittoria**: nello stile nuovo, **senza i riconoscimenti**; due pulsanti, *Continua* (si resta
   nella partita, all'accampamento) e *Menu principale* (salva e torna al menu).
6. **Pavimenti di cripta e caverne** (D5): le macchie a terra.

La morte non è segnata, ma prende lo stesso stile e lo sfondo della D6 per non restare l'unica
schermata vecchia. Gli altri candidati (barra in basso, celle e slot, icona dell'anello, cornice
della minimappa) restano fuori: i pannelli nuovi li toccano in parte, e si riguardano nelle foto di
dopo.

**Fuori dalla M17, nella M19** (era la M18, prima della cinematica). Mirco ha segnato anche due cose che non sono grafica: all'ingresso di
cripta e caverne **manca la risalita**, una scala che funziona e riporta alla profondità di sopra
(dalla 1 all'accampamento), come in Diablo 1; e **alcune stanze sono troppo piccole**, va alzata la
dimensione minima. Tutte e due cambiano il generatore, quindi i livelli di tutti i semi, i test con
i semi fissi e le soglie della ronda: decise da Mirco il 26 ott come milestone a sé, dopo la M17.

## Passo 17.2 — Le immagini in `Tools/ComfyUI`

**Com'è andata (26 ott 2026).** Due workflow in formato API, `z_image_turbo` (da testo) e
`z_image_seam` (la croce ridisegnata con una maschera sul latente), le ricette in `images/` e
`make.py` che le genera come i suoni, con `--takes` e `--page`. La finitura sta in `imagelib.py`,
con 13 test (`test_imagelib.py`). Le prime due ricette, `panel_back` e `panel_frame` (`images/ui.py`),
hanno fatto da prova, e la prova ha cambiato tre cose:

- **"Seamless tileable" nel prompt fa danni.** Il modello disegna lui una griglia di lastre, con una
  linea proprio al centro: spostata di mezzo lato finisce sul bordo. Si chiede una superficie unica
  che riempie l'immagine, e alla ripetizione pensa la finitura.
- **La croce ridisegnata dal modello fa pieghe** sulle superfici lisce, anche a denoise 0,45. Per i
  materiali uniformi la ripetizione si fa senza il modello: l'originale lungo la croce e la copia
  spostata altrove, sfumando (`blend_seams`). Il ridisegno resta per i disegni grossi
  (`tile: "redraw"`). Prima, la luce più chiara al centro che il modello aggiunge si spiana
  (`flatten`), o ripetuta fa una scacchiera.
- **La trasparenza va presa sulla tinta, non sulla distanza.** Il magenta chiesto esce bordeaux o
  viola, e un ferro scuro su un fondo scuro diventava semitrasparente. Ora il colore del fondo si
  legge dai bordi, un pixel si misura lungo la tinta del fondo (un grigio resta pieno anche se
  scurissimo) e dai bordi si toglie solo quella tinta.

La misura delle cuciture guarda i bordi e la linea di mezzo, dove un'immagine spostata nasconde la
sua; la soglia è 1,8 (le buone stanno tra 1,0 e 1,3, quelle con le cuciture visibili oltre 2,5). Con
le correzioni lo sfondo dei pannelli si ripete a 1,15–1,4 su quattro semi, e la cornice in ferro
nero esce intera, con le quattro decorazioni agli angoli, pronta per le nove parti del 17.3.

**Licenze (D11).** Il VAE `ae.safetensors` è quello di FLUX.1 (16 canali latenti, scala 0,3611,
spostamento 0,1159): sta nel repo ufficiale di Z-Image, sotto Apache 2.0, ed è lo stesso di
FLUX.1-schnell, anch'esso Apache 2.0. ADR-114. La riga in `CREDITS.md`, con la copia in
`Data/Credits.txt`, arriva con la prima immagine che entra nel gioco.

## Passo 17.3 — I pannelli

**Com'è andata (26 ott 2026).** Le immagini sono quelle della prova del 17.2: lo sfondo `panel_back`
(seme 1703) e la cornice `panel_frame` (seme 1712), rigenerati identici dalla ricetta in
`Art/Generated/UI`. La cornice intera aveva una decorazione dorata a metà di ogni lato, che nelle
nove parti si sarebbe allungata in una striscia: `nine_slice` tiene i quattro angoli (76 pixel) e un
tratto di lato pulito, e toglie il centro. La placca dei pulsanti (`button_plate`) la ricava
`uiparts.py` dallo sfondo, con un filo scuro e uno d'oro spento: è grigia, e il colore lo mette il
pulsante.

`GeneratedUiImporter` scrive le impostazioni di import (sprite senza mipmap, compressi, al più 512,
ripetizione per lo sfondo, bordi delle nove parti). `UiSkin` (`Data/UI/UiSkin.asset`) tiene immagini e
colori; `UiSkinTools.ApplyToCore`, dal menu o in batchmode, riveste la scena Core riconoscendo il
ruolo dalla scena: **9 finestre** (inventario, scheda, mercante, conferma, difficoltà, opzioni, pausa,
vittoria, riconoscimenti) e **4 tooltip** prendono lo sfondo ripetuto e un figlio `SkinFrame` con la
cornice, primo figlio e fuori dai layout; **31 pulsanti** la placca e i colori, scuri a riposo e
caldi al passaggio del mouse. Restano com'erano i veli, le celle, gli slot, le etichette a terra e il
pulsante dei punti, come vuole la lista.

Le foto hanno deciso due ritocchi: la cornice a 2,5 spariva sul fondo scuro, ora è a 1,6 nelle
finestre (lati di circa 19 pixel a 1080p, angoli di 47) e a 2,6 nei tooltip; le finestre sono più
scure (0,54). Il test del contrasto ha trovato il rosso di «oro insufficiente» del mercante a 4,3 su
4,5: il rosso su un fondo quasi nero non sale scurendo il fondo, quindi è schiarito, nella scena e in
`ShopPanel.UnaffordableColor` (anche i prezzi).

`PhotoTour` scriveva per difetto in `Temp`, che Unity svuota uscendo: ora in `Recordings/Photos`,
fuori da git. E fissa anche il seme del loot, così l'equipaggiamento del cavaliere è lo stesso a ogni
giro (il banco del mercante viene dalla partita nuova e può cambiare). `CREDITS.md` ha la riga delle
immagini generate, ricopiata in `Data/Credits.txt`.

Test: `UiSkinTests` (3: import, scena rivestita, contrasto almeno 4,5:1 di ogni testo sullo sfondo
medio), e in `test_imagelib.py` le nove parti e la placca (15).

## Passo 17.4 — I menu

**Com'è andata (27 ott 2026).** I riconoscimenti escono dalla vittoria e diventano un pannello a sé,
`CreditsPanel` (`MenuUI/Credits`), con lo stesso testo da `CREDITS.md` che sale dal basso; li apre il
pulsante **Riconoscimenti** del menu iniziale, al posto dei pulsanti, e Indietro riporta il menu.
La vittoria ha due pulsanti: **Continua** chiude la schermata e si gioca ancora in città
(`VictoryScreen.Continued`), **Menu principale** salva e torna al menu come prima (`Finished`).

La disposizione nuova, nello stile scelto da Mirco:

- **menu iniziale**: titolo e pulsanti in una colonna a sinistra, a 140 pixel dal bordo, con una linea
  sotto il titolo e il testo dei pulsanti allineato a sinistra; i pulsanti stanno in un layout
  verticale, così Continua, nascosto senza salvataggi, non lascia il buco; lo spazio a destra è per lo sfondo
  del 17.6. Il titolo sta con i pulsanti, così si nasconde con loro sotto le opzioni e i
  riconoscimenti;
- **opzioni** per gruppi, ognuno con un titolo piccolo in oro spento e una linea: Lingua, Audio
  (i tre volumi), Grafica (il filtro retrò), Interfaccia (nomi e numeri sempre visibili);
- **pausa** e **vittoria** più ariose, con una linea sotto il titolo.

La morte non cambia disposizione: ha già la placca nuova sul pulsante. I pulsanti rivestiti sono ora
33. La scena è stata ridisposta da uno script dell'editor lanciato una volta in batchmode e poi tolto;
la pelle si riapplica con `UiSkinTools`. Stringhe nuove nelle due lingue: `menu.credits`,
`victory.continue`, `victory.menu` e i quattro titoli dei gruppi delle opzioni; `victory.credits` è
tolta.

Test: `MenuTests.Credits_OpenFromTheMenu` (si aprono, ci sono gli autori e Z-Image-Turbo, salgono,
Indietro riporta il menu), `VictoryPlayModeTests.Victory_FromBossToTheMenu` (Menu principale torna al
menu e Continua del menu riporta in città) e, in `AltarPlayModeTests.FirstVictory_OpensTheNext`,
Continua che lascia in città con il tempo e i comandi. `PhotoTour` fotografa i riconoscimenti dal
menu.

## Passo 17.5 — Le macchie a terra

**Com'è andata (27 ott 2026).** Otto macchie generate con Z-Image-Turbo (`Tools/ComfyUI/images/decals.py`):
crepe, polvere, ossa sparse, sangue secco, muschio, pozza, sassi e fuliggine, viste dall'alto su un
fondo uniforme, ritagliate con la stessa finitura dei pannelli e sfumate verso il bordo (`fade`, nuovo
in `imagelib.py` con `fit`, `opacity` e `atlas`). `decalatlas.py` le mette in un atlante da 1024×512,
quattro colonne per due righe, in `Art/Generated/Decals/`; `GeneratedDecalImporter` lo importa con i
mipmap, compresso. Le prove sono servite: il sangue su magenta spariva con il fondo (va su verde), "crepe
nella pietra" disegna le lastre intere e "polvere" un blocco di pietra; il teschio intero era grande
come il cavaliere.

Nel gioco:

- `DecalPlanner` (logica pura) sceglie le macchie in `DungeonLevel.CreateMap`, dopo il tesoro del re e
  con un seme suo (`SeedMixer.ForDecals`): da 1 a 3 per stanza della cripta e qualcuna nei corridoi
  (una ogni 8 celle al più), da 1 a 3 per zona di 7×7 celle nelle caverne. Ogni stanza o zona tira
  un tema (cripta: deposito, battaglia, umido, bruciato; caverne: umido, tana, frana) e il tema
  sceglie le caselle. Solo su celle di pavimento libere, non accanto a scala, ingresso, cancello e
  punti d'interesse, una per cella e con la parte visibile dentro la cella. Rispetto alla D5 il
  minimo è 1 e non 0: con 0 le stanze senza macchie erano troppe;
- le macchie viaggiano nella direttiva `@decals` (`DecalSpot`, sette interi): la griglia, i nemici, le
  casse e gli arredi restano quelli di prima, e il salvataggio non le ricorda;
- `LevelBuilder` le mette in una mesh sola (`DecalLayer`), con un materiale solo (`M_Decals`, Lit
  trasparente, coda 2990 sotto i cerchi dei nemici speciali e gli avvisi), sul layer di base: niente
  collider, e il NavMesh, che si cuoce dai layer Ground e Obstacle, non le vede. Stanno 2 cm sopra
  la cima della piastrella di base, letta dalla mesh: quella della cripta è a 5 cm, quella delle
  caverne a 11;
- `DecalTools.Setup` (`-executeMethod`) crea il materiale e lo mette nei tileset di cripta e caverne;
  la città non ne ha.

Test: `DecalTests` (stesso seme stesse macchie, il livello non cambia, mai sopra un marcatore né
accanto ai pezzi da guardare, temi, al più tre per stanza, la parola della direttiva, il quadrato,
il seme a parte, import e materiale), `DecalPlayModeTests` (cripta e caverne con le macchie della
mappa in una mesh sola, sopra il pavimento, senza collider e fuori dai layer del NavMesh); la ronda
conta le macchie nel riassunto dei livelli costruiti e segnala una mesh che non ha quelle della mappa.

## Passo 17.6 — Gli sfondi

**Com'è andata (27 ott 2026).** Tre illustrazioni a tutto schermo da Z-Image-Turbo
(`Tools/ComfyUI/images/backgrounds.py`), tutte e tre buone, senza il render del gioco di riserva:

- **menu**: l'ingresso della cripta nella roccia, di notte, con due torce e la porta socchiusa; la
  cripta sta a destra, la sinistra resta scura per la colonna dei pulsanti (`shade` su quel lato);
- **morte**: una tomba aperta su una base di pietra, con l'elmo e la spada del cavaliere sotto una
  luce fredda dall'alto;
- **vittoria**: l'accampamento all'alba, due tende, il fuoco che si spegne e il sole dietro i monti.

Il primo giro di morte e vittoria era un'illustrazione piatta, diversa dal menu: con "3D render of a
low poly isometric diorama" in testa al prompt escono diorami come i modelli KayKit. Le immagini nascono a
1920×1088 (multiplo di 16) e la finitura le porta a 1920×1080, le scurisce dove stanno i testi
(`shade`, nuovo in `imagelib.py`) e un poco in tutto.

Nel gioco: `GeneratedUiImporter` le importa da `Art/Generated/Backgrounds/` come sprite senza
mipmap, al più 2048. `UiSkin` ha i tre sfondi e `UiSkinTools.ApplyToCore` li mette come primo figlio
(`Backdrop`) del fondo del menu e dei veli di morte e vittoria, con un `AspectRatioFitter` che copre lo
schermo senza deformarli a ogni proporzione e senza fermare i click. Lo sfondo della morte è opaco: la
sfera del mana e la barra dell'esperienza, che venivano dopo nella gerarchia, gli restavano sopra, così
`DeathScreen` ora sta dopo tutto l'HUD, sotto solo la dissolvenza e il nome del livello.

Test: `UiSkinTests.Screens_HaveTheirBackdrops` (import, primo figlio, 1920×1080, copre senza
deformare) e `UiSkinTests.DeathScreen_CoversTheHud`.

## Passo 17.7 — Misura e chiusura

**Com'è andata (27 ott 2026).**

| Misura | v1.4.0 | v1.5.0 |
|---|---|---|
| Build Windows della CI (artefatto) | 53,05 MB | 54,91 MB (+1,86, limite +25) |
| Texture nuove nella build | — | circa 4,1 MB: tre sfondi DXT1 1920×1080 da 1,0 MB, l'atlante DXT5 con i mipmap 0,7 MB, i pannelli 0,16 MB |
| Tempi di un livello (generazione, costruzione, NavMesh, a regime) | 25–30 ms | 25–30 ms |

**La vittoria dal gioco.** Guardando le foto Mirco ha scartato l'accampamento all'alba: bello, ma
sembrava di un altro gioco. Al suo posto la riserva della D6: `PhotoTour.VictoryBackdrop` scende alla
sala del re, lo abbatte, mette il cavaliere davanti ai resti e al tesoro, spegne l'interfaccia e salva
un PNG a 1920×1080; `Tools/ComfyUI/renders.py` lo rifinisce con la ricetta di `RENDERS` in
`images/backgrounds.py`. Mirco vuole al suo posto una **cinematica** di chiusura, come i filmati di
Diablo: il re che crolla, il cavaliere che apre il tesoro ed esce, l'accampamento all'alba, poi le
statistiche; e un filmato d'apertura per la nuova partita, "L'ultima candela". Sono proposti insieme
come M18 (piano v2.66), e risalita e stanze più grandi passano alla M19.

Le foto di dopo sono in `docs/media/m17/dopo` (31: i riconoscimenti si fotografano dal menu, senza
il filtro), e `docs/media/m17/prima_e_dopo.html` le mette accanto a quelle di prima. La build da
provare è il run 40490509049; le prove sono in `docs/Da_provare.md`. ADR-115…117, le lezioni nel
piano (v2.66), il README e `bundleVersion` 1.5.0.

## Trappole note

- **Lo stile KayKit è a tinte.** Un'immagine con dettagli fotografici accanto ai modelli stona: nei
  prompt colori piatti, low poly, dipinto a mano, e la palette presa dall'atlante. Si giudica sulle
  foto nel gioco, non sull'immagine da sola.
- **Z-Image non fa la trasparenza.** Le macchie nascono su un fondo uniforme e si ritagliano: attorno
  ai bordi resta un alone del colore del fondo, da togliere nella finitura.
- **Nove parti e scala dell'interfaccia.** I margini della cornice sono in pixel dell'immagine:
  vanno legati alla risoluzione di riferimento del `CanvasScaler` e ai pixel per unità, o a 4K la
  cornice si assottiglia e a 720p si mangia il testo.
- **Il contrasto.** Uno sfondo con una texture abbassa la leggibilità del testo grigio e dei colori
  delle rarità, soprattutto il blu dei magici: il test lo misura, le foto lo confermano.
- **Il tiro a parte delle macchie.** Un dominio suo del seme, che non consuma i tiri degli arredi: se
  le macchie spostano anche una sola cassa, cambiano i livelli che la ronda e i test conoscono.
- **Le macchie e il pavimento.** Un quad appoggiato sul pavimento sfarfalla: va alzato di un paio di
  centimetri e disegnato dopo il pavimento. Non deve nascondere il segnale degli spuntoni né il
  cerchio dei nemici speciali.
- **Draw call.** Tante macchie con materiali diversi costano: un atlante solo, un materiale solo, e
  l'instancing della GPU.
- **Import delle immagini.** Le impostazioni (compressione, dimensione massima, mipmap, tipo sprite e
  bordi delle nove parti) si scrivono da script nell'importer, non a mano nell'editor. I `.meta` li
  genera Unity, mai copiati.
- **ComfyUI e Pillow.** ComfyUI va avviato a mano sulla porta 8199, e Pillow c'è solo nel suo ambiente
  Python: gli script delle immagini girano con quello, o Pillow va nei requirements.
- **Le foto in batchmode.** Unity in batchmode senza `-nographics` disegna: le foto si fanno lì, con la
  stessa risoluzione e le stesse impostazioni di qualità della build. Lo schermo parte a 640×480 e
  `WaitForEndOfFrame` in batchmode fa cadere il test: la risoluzione si imposta con
  `PlayModeWindow.SetCustomRenderingResolution` e la foto si prende dopo qualche frame.

## Test

- EditMode: macchie dal seme (stesso seme, stesse macchie), livelli di sempre uguali con le macchie,
  nessuna macchia sopra scale, trappole, pozzi, santuari e casse; ogni pannello rivestito da
  `UiSkin`; contrasto almeno 4,5:1; ricette con l'immagine che esiste e le impostazioni di import;
  misura delle cuciture su un'immagine che si ripete e su una che non si ripete.
- PlayMode: un livello di cripta e uno di caverne con le macchie senza collider e senza ostacoli del
  NavMesh; i pannelli nelle due lingue dentro lo schermo; menu, morte e vittoria con lo sfondo.
- Ronda: le macchie contate nel riassunto dei livelli costruiti, dentro i 150 ms.

---

## Checklist di chiusura

- [x] Decisioni D1–D16 confermate
- [x] Giro di foto e lista corta approvata
- [x] Strumenti per le immagini, licenze e ADR (`CREDITS.md` con la prima immagine nel gioco)
- [x] Pannelli
- [x] Menu: principale, opzioni, pausa, vittoria e morte
- [x] Macchie a terra
- [x] Sfondi di menu, morte e vittoria
- [x] Misura, foto di dopo e build di prova
- [x] ADR, lezioni nel piano, README
- [x] `bundleVersion` 1.5.0 e tag `v1.5.0` dopo i test verdi in CI
