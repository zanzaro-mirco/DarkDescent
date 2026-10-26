# M17 — "Una pelle nuova"

**Cosa deve succedere (Definition of Done):** le parti della grafica che stonano di più sono rifatte,
nello stile KayKit e non in uno stile nuovo. Prima si fotografa ogni schermata e ogni tipo di livello,
e con Mirco si sceglie una **lista corta**. Poi i **pannelli** dell'interfaccia, oggi rettangoli a
tinta unita, prendono una cornice e uno sfondo; pavimenti e muri di cripta e caverne prendono
**macchie a terra** (crepe, muschio, sangue, polvere) che rompono la ripetizione; menu, morte e
vittoria prendono uno **sfondo**. Le immagini nascono da **Z-Image-Turbo** in ComfyUI, ognuna con la
sua ricetta nel repo, e niente entra nel gioco senza il sì di Mirco sulla foto di prima e quella di
dopo. Esce con il tag `v1.5.0`.

**Tempo stimato:** 7–10 h (D14).
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
| 17.3 | I pannelli: cornice in nove parti e sfondo, `UiSkin`, il contrasto, le foto di dopo (D4, D13) | 1,5–2 |
| 17.4 | Le macchie a terra: atlante, tiro a parte nel popolatore di cripta e caverne, test e ronda (D5, D13, D16) | 1,5–2 |
| 17.5 | Gli sfondi di menu, morte e vittoria e le voci restanti della lista (D6, D7) | 1–1,5 |
| 17.6 | Misura (dimensione della build, memoria, tempi dei livelli), foto di dopo, build da provare e chiusura: ADR, lezioni, piano, README con le foto, `bundleVersion` 1.5.0, tag `v1.5.0` dopo i test verdi in CI (D12) | 1–1,5 |

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
- [ ] Giro di foto e lista corta approvata
- [ ] Strumenti per le immagini, licenze, ADR e `CREDITS.md`
- [ ] Pannelli
- [ ] Macchie a terra
- [ ] Sfondi e voci restanti della lista
- [ ] Misura, foto di dopo e build di prova
- [ ] ADR, lezioni nel piano, README
- [ ] `bundleVersion` 1.5.0 e tag `v1.5.0` dopo i test verdi in CI
