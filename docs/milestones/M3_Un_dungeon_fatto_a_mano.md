# M3 — "Un dungeon fatto a mano"

**Cosa deve succedere (Definition of Done):** in una **build scaricata da itch.io**,
il cavaliere parte nel livello 1: stanze e corridoi di una cripta, buia, illuminata da
torce, con un raggio di luce che il personaggio porta con sé. Si combatte con gli
scheletri come alla M2. In fondo una scala: un click, il cavaliere la raggiunge e
scende al livello 2, **con la vita che aveva**. I nemici si sentono prima di vederli.
La build parte su un PC che **non è quello di Mirco**. Test verdi in CI.

**Tempo stimato:** 9–14 h (piano v2.8). **Prerequisito:** M2.5 chiusa (tag `m2.5`).

**Come si lavora:** il codice e i passaggi nell'editor li faccio io, in batchmode a
Unity chiuso, come alla M2. A Mirco restano le decisioni qui sotto, il download del
tileset, l'account e la pagina su itch.io con il suo secret, le prove in Play Mode e
in build (anche su un altro PC) e la revisione degli ADR.

---

## Stato verificato il 3 ottobre 2026

| Cosa | Stato |
|---|---|
| Tileset | **KayKit Dungeon Pack**, versione gratuita 1.1 (16 lug 2026): oltre 200 modelli tra muri, pavimenti, scale, porte, torce e oggetti di scena, FBX/GLTF/OBJ, **CC0**, 31 MB. Stesso autore e stesso stile dei personaggi già nel progetto. La misura della griglia non è dichiarata: si misura all'import (passo 3.4) |
| Render pipeline | URP con renderer **Forward+** già attivo nel profilo PC (`PC_Renderer`): nessun limite di 4 o 8 luci per oggetto, quindi molte torce non sono un problema. Ombre delle luci aggiuntive già abilitate. Distanza delle ombre 50 m, troppa per una camera ortografica che inquadra circa 18 m |
| Camera | `CameraFollow` scritto a mano (M1), ortografica, `Size` 9. Cinemachine 3.1.7 installato e mai usato. Nessun test verifica la camera in modo diretto: i test PlayMode la usano per convertire i click |
| Audio | Sorgenti 2D, `AudioListener` sulla `Main Camera` |
| Scene | Una sola, `Sandbox_Combat`, che contiene tutto: player, HUD, camera, nemici, `CompositionRoot`. Restart = `LoadScene(buildIndex)` |
| itch.io | Butler, lo strumento ufficiale per caricare le build, è alla versione **15.31.0** |
| Build Web | Esiste l'immagine `unityci/editor:ubuntu-6000.3.24f1-webgl-3`: una build Web si può provare in CI **senza installare niente** sul PC di Mirco |

---

## Decisioni da prendere prima di cominciare

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Struttura delle scene** (piano § 4.3) | Una scena **`Core`**, punto d'ingresso della build, con player, camera, HUD, EventSystem, `CompositionRoot` e `LevelManager`. I livelli (`Level_01`, `Level_02` e la sandbox) si caricano **in modo additivo** sopra. **Niente `Bootstrap`** per ora | Player e HUD sopravvivono al cambio di livello senza `DontDestroyOnLoad`, e alla M6 un livello procedurale sarà solo un'altra scena. La `Bootstrap` del piano serve quando ci sarà qualcosa da fare prima di `Core` (menu principale, caricamento): oggi sarebbe una scena vuota in più |
| D2 | **Come si costruiscono i livelli** | **Mappe di testo**: ogni livello è una griglia di caratteri in un file (`#` muro, `.` pavimento, `D` porta, `T` torcia, `S` scheletro, `<` ingresso, `>` scala). Uno strumento di editor, che **resta nel repo**, piazza i moduli e salva la scena. Ritocchi a mano nell'editor possibili | Io non posso trascinare moduli nell'editor, e una mappa di testo si legge e si corregge in un diff. Il piano dice "costruisci a mano, poi automatizza": la mappa *è* fatta a mano, e alla M6 il generatore dovrà solo produrre la stessa griglia. Per questo lo strumento resta, invece di essere usa e getta come gli altri script di editor |
| D3 | **Tileset** | KayKit **Dungeon Pack Free 1.1** (31 MB, CC0), scaricato da Mirco | CC0 come vuole l'ADR-004, stesso stile di cavaliere e scheletri. Si scarica dalla pagina di itch.io dell'autore con un click nel browser ("No thanks, just take me to the downloads") |
| D4 | **Illuminazione** | **Solo luci in tempo reale**, niente lightmap. Ambiente quasi nero, nessuna luce direzionale. Una luce puntiforme sul cavaliere, l'**unica con le ombre**. Torce come luci puntiformi **senza ombre**, con un leggero tremolio. Post-processing con tonemapping, bloom sulle fiamme e vignetta | Alla M6 i livelli nascono a runtime e non si possono cuocere le lightmap: meglio non prendere un'abitudine da buttare. Ogni luce puntiforme con ombre occupa sei porzioni della mappa delle ombre: con dieci torce sarebbero sessanta |
| D5 | **Muri che coprono il cavaliere** | Sui lati del livello rivolti verso la camera, **muri bassi** (mezza altezza), come nella visuale di Diablo. Un effetto di trasparenza vicino al personaggio si valuta alla M11 con gli shader | Con la camera isometrica un muro alto tra camera e cavaliere lo nasconde del tutto. In più l'ADR-006 ferma il raggio del click sugli ostacoli: dietro un muro alto non si potrebbe cliccare |
| D6 | **Pubblicazione su itch.io** | Pagina creata da **Mirco** (account, titolo, stato "In development", gratuita, Windows). Poi la **CI carica la build** con butler a ogni tag `m*`/`v*`, con un secret `BUTLER_API_KEY` inserito da Mirco | La pagina è una vetrina pubblica a suo nome: la crea e la pubblica lui. Il caricamento invece si ripete a ogni milestone, ed è il lavoro giusto per il workflow della build della M2.5. Il primo caricamento si può fare anche a mano dal sito, se si preferisce vedere il risultato prima di automatizzare |
| D7 | **Build Web** | **Prova in CI** dopo che le luci del dungeon esistono (passo 3.8), con l'immagine `webgl`. Se aspetto e fluidità reggono, va su itch.io accanto a quella Windows, giocabile nel browser. La scelta finisce in un ADR | Chi guarda un portfolio clicca "gioca" più volentieri di "scarica". Ma il punto debole di URP sul web sono proprio molte luci e ombre: va giudicato sul dungeon vero, non in astratto. In CI non serve installare il modulo *Web Build Support* sul PC |
| D8 | **Cambio di livello e restart** | Le scale stanno su un layer nuovo, **`Interactable`**: un click ci porta il cavaliere, e quando le raggiunge lo schermo sfuma al nero, il livello vecchio si scarica e il nuovo si carica. Dopo la morte, **Ricomincia** riparte da `Core` e dal livello 1 | È il ramo in più del click previsto dall'ADR-006, che dopo servirà anche per porte, oggetti a terra e PNG. Ripartire dal livello 1 dopo la morte è il comportamento più semplice finché non c'è un salvataggio (M8) |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 3.1 | Scene: `Core`, `LevelManager`, sandbox come livello, test riportati | 1,5–3 |
| 3.2 | Camera con Cinemachine 3, zona morta e Impulse | 1–1,5 |
| 3.3 | Audio posizionale e `AudioMixer` | 0,5–1 |
| 3.4 | Tileset: import, misure, prefab dei moduli, strumento delle mappe | 1–2 |
| 3.5 | Livelli 1 e 2, scale e cambio di livello | 1,5–2 |
| 3.6 | Buio, torce, luce del cavaliere e post-processing | 1,5–2 |
| 3.7 | itch.io: pagina, butler in CI, prova su un altro PC | 1–1,5 |
| 3.8 | Prova della build Web | 0,5 |
| 3.9 | Chiusura: GIF, ADR, tag `m3` | 0,5 |

---

## Architettura

```
Core (scena d'ingresso, sempre caricata)
├─ CompositionRoot      collega HUD, player e i nemici di ogni livello caricato
├─ LevelManager         carica e scarica i livelli, sposta il player, sfuma lo schermo
├─ Player               NavMeshAgent spento finché un livello non ha un NavMesh
│  ├─ PlayerLight       luce puntiforme con ombre
│  └─ AudioListener     all'altezza della testa
├─ Main Camera          CinemachineBrain
├─ PlayerCamera         CinemachineCamera + Position Composer + Impulse Listener
├─ HUD (Canvas)         sfera, numeri di danno, schermata di morte, pannello di dissolvenza
└─ EventSystem, HitStop

Level_01 / Level_02 / Sandbox_Combat (additive, una alla volta)
└─ Level                LevelContext: ingressi, uscite, NavMeshSurface, Volume, luci, nemici
```

### Contratti

- **`LevelManager`** (`DarkDescent.Levels`): `LoadLevel(string sceneName, string entranceId)`,
  evento `LevelLoaded(LevelContext)`, `IsTransitioning`. Sequenza: input del player
  spento → dissolvenza al nero → `ClearTarget` sul player → scarica il livello
  corrente → carica il nuovo in modo additivo → lo rende **scena attiva** → `Warp`
  del player sull'ingresso → camera avvisata dello spostamento → `LevelLoaded` →
  dissolvenza dal nero → input acceso.
- **`LevelContext`** (sulla radice di ogni livello): ingressi per id, `GetEnemies()`.
  Il `CompositionRoot` collega i nemici a ogni `LevelLoaded` e li scollega quando il
  livello si scarica (`DamageNumbers.Untrack`).
- **`LevelExit`** (sulle scale, layer `Interactable`): livello e ingresso di
  destinazione. Il cavaliere che entra nel suo trigger chiede il cambio.
- **`TorchFlicker`**: varia l'intensità della luce con un rumore di Perlin, senza
  allocare niente per frame.
- **Mappe** (`Assets/_Project/Levels/*.txt`) e **`LevelMapBuilder`**
  (`Assets/_Project/Editor/`, assembly solo editor): dalla mappa alla scena.

---

## Passo 3.1 — Scene

1. Scena `Core` con quello che oggi sta in `Sandbox_Combat` e non appartiene al
   livello: player, camera, HUD, EventSystem, `HitStop`, `CompositionRoot`. Indice 0
   nei *Build Profiles*.
2. `Sandbox_Combat` diventa un livello: pavimento, ostacoli, tre scheletri,
   `LevelContext` con un ingresso, `NavMeshSurface`. Il pavimento si allarga, o lo
   sfondo della camera prende il colore del pavimento, così il bordo nero non si vede
   più (voce rimandata dalla M2).
3. `LevelManager` con la sequenza dei contratti, e il primo livello serializzato.
   Se all'avvio un livello è già aperto nella Hierarchy (succede nell'editor), lo usa
   invece di caricarne un secondo.
4. `CompositionRoot`: `Bind` dei nemici su `LevelLoaded`, non più in `Awake`. Restart:
   `LoadScene("Core")` in modalità singola.
5. **Test:** `SandboxFixture` carica `Core` e chiede la sandbox al posto del primo
   livello. I 47 test esistenti devono restare verdi senza cambiare quello che
   verificano.

**Verifica:** test verdi in locale e in CI; in Play Mode, da `Core`, la sandbox si
gioca come alla M2.

**Com'è andata (3 ott 2026).** `Core` creata da uno script di editor usa e getta che
ci ha spostato `CompositionRoot`, `EventSystem`, `HitStop`, HUD, camera e player, e
ha aggiunto il `LevelManager`. Nella sandbox restano pavimento (ora 100×100 m, NavMesh
ricotto), ostacoli, luce e scheletri, più un oggetto `Level` con `LevelContext` e
l'ingresso `Start`. Dal prefab del player è sparito `AgentActivator`: l'agent lo
accende il `LevelManager` dopo averlo messo sull'ingresso. `DamageNumbers.Untrack` usa
`ReferenceEquals` perché uno scheletro già distrutto va tolto lo stesso dal dizionario.
Test: 11 EditMode e 40 PlayMode verdi (i 36 di prima più `LevelLoadingTests`). Una
trappola nuova nei test: `WaitForLevel` deve aspettare un frame prima di controllare,
altrimenti trova il `LevelManager` della scena precedente, già pronto. Build locale
senza avvisi nel log.

---

## Passo 3.2 — Camera con Cinemachine

1. `CinemachineBrain` sulla `Main Camera`; una `CinemachineCamera` con *Follow* sul
   cavaliere, lente ortografica `Size` 9 e rotazione fissa, senza componente di mira.
2. **Position Composer:** distanza 20, smorzamento morbido, una **zona morta** piccola
   al centro, dentro cui il cavaliere si muove senza trascinare la camera.
3. **Impulse:** una sorgente sul player che scuote la camera quando subisce un colpo
   pesante (almeno il 20% della vita, come la soglia dell'ADR-010) e quando muore. Un
   *Impulse Listener* sulla camera. Scossa breve e piccola: si tara a occhio.
4. `CameraFollow` cancellato.

**Verifica:** a occhio in Play Mode, niente tremolii. **Test:** dopo un `Warp` del
cavaliere la camera è già su di lui al frame successivo, senza attraversare la mappa.

**Com'è andata (3 ott 2026).** `CinemachineBrain` sulla `Main Camera` e una
`PlayerCamera` con la rotazione della camera di prima, lente ortografica `Size` 9,
*Position Composer* (distanza 20, smorzamento 0,3 s, zona morta 6% × 8% dello
schermo) e *Impulse Listener*. Sul player una `CinemachineImpulseSource` (uniforme,
forma *Bump*, 0,2 s) e `DamageCameraShake`: la scossa scala con il danno ed è piena
al 20% della vita, quindi i colpi dello scheletro (5 su 100) danno una scossa leggera,
la morte una forte (1,5). Dopo un cambio di livello il `CompositionRoot` invalida lo
stato della camera (`PreviousStateIsValid = false`) invece di chiamare
`OnTargetObjectWarped`: con lo schermo che sfuma conviene il riposizionamento netto.
`CameraFollow` cancellato. Test: 11 EditMode e 43 PlayMode verdi (`CameraTests`:
camera al centro dopo il cambio di livello, cavaliere dentro la zona morta, scossa
sui colpi).

---

## Passo 3.3 — Audio posizionale

1. `AudioListener` tolto dalla camera e messo su un figlio del player a circa 1,6 m.
2. `AudioSource` dei personaggi in 3D (`spatialBlend` 1), attenuazione logaritmica
   tra 4 e 25 m.
3. `AudioMixer` con i gruppi `SFX` e `UI`, più `Music` vuoto per dopo.

**Verifica:** a orecchio: uno scheletro fuori schermo si sente, da lontano più piano.
**Test:** in `Core` esiste un solo `AudioListener`.

**Com'è andata (3 ott 2026).** Il listener non è un figlio del player, come scritto
sopra, ma un oggetto `AudioListener` in `Core` con `AudioListenerRig`: in
`LateUpdate` si mette a 1,6 m sopra il cavaliere e prende l'imbardata della camera.
Figlio del player avrebbe girato con lui, e destra e sinistra si sarebbero scambiate
a ogni cambio di direzione. Sorgenti di cavaliere e scheletro in 3D, logaritmiche tra
4 e 25 m, effetto Doppler spento (con camera e listener che si muovono darebbe
variazioni di intonazione a caso). `Audio/Main.mixer` con `SFX`, `UI` e `Music` sotto
`Master`: Unity non ha un'API pubblica per creare un mixer, quindi lo script di
editor è passato per la reflection, e ha dovuto creare a mano anche la vista della
finestra del mixer. `*.mixer` aggiunto a `.gitattributes`. Test: 11 EditMode e 46
PlayMode verdi (`AudioTests`).

---

## Passo 3.4 — Tileset e strumento delle mappe

1. **Mirco** scarica KayKit Dungeon Pack Free 1.1 nella cartella `Downloads`.
2. Import dei modelli che servono (pavimento, muro alto e basso, angolo, porta,
   scala, torcia a muro, pochi oggetti di scena) come alla M1: rig nessuno,
   materiale condiviso, collider sui muri.
3. **Misure:** dimensione del modulo, altezza dei muri, punto di origine. Se il passo
   non è 4 m, la griglia si adatta al tileset, non il contrario.
4. Prefab dei moduli in `Prefabs/Dungeon/`, con layer giusti: pavimento `Ground`,
   muri `Obstacle`, scale `Interactable`.
5. `LevelMapBuilder`: legge la mappa, piazza i moduli, aggiunge `LevelContext`,
   `NavMeshSurface` (cotto), luci e nemici, salva la scena e la aggiunge ai *Build
   Profiles*.

**Verifica:** una mappa di prova 3×3 stanze costruita e giocabile da `Core`.

**Com'è andata (3 ott 2026).** Lo zip l'ho scaricato io su richiesta di Mirco; nel
progetto ne sono entrati 38 modelli su 211, 1,2 MB, con un materiale condiviso
`M_Dungeon` sulla texture atlante. **Misure:** la griglia è davvero 4 m
(`floor_tile_large` 4×4, cima a 0,05 m); `wall` è lungo 4, alto 4 e spesso 1, centrato
sul lato; `wall_half` è un muro *corto*, non basso, quindi il muro basso è `barrier`,
una balaustra in pietra alta 1,1 m; `stairs` è larga 5, profonda 4 e scende di 5,1.
**Prefab** in `Prefabs/Dungeon/`: radice con layer e collider, modello come figlio.
Nuovo layer `Interactable` (9) per le scale. **Codice:** `LevelMap` (logica pura, 7 test
EditMode) legge griglia, marcatori e direttive `@`; `LevelTileset` (ScriptableObject in
`Data/Levels/`) dice quale prefab va dove; `LevelMapBuilder` (assembly
`DarkDescent.Editor`, menu *DarkDescent → Ricostruisci i livelli dalle mappe*) rifà le
scene in `Scenes/Levels/` con NavMesh cotto e le aggiunge ai *Build Profiles*. Muri
alti sui lati nord ed est, balaustre su sud e ovest; torce sul muro alto della loro
cella. **Scala:** la prima versione scendeva verso sud e un pezzo spuntava sotto la
balaustra, nel vuoto. Ora si entra preferibilmente da sud e scende verso nord, sotto
il muro alto che la nasconde: nelle mappe il `>` va contro un muro nord. Tra una stanza
e l'altra la roccia larga una cella resta un vuoto nero: con il buio del passo 3.6 non
dovrebbe vedersi, si giudica lì. La porta `D` della legenda non c'è ancora: per ora le
stanze si collegano con passaggi aperti. Verifica con un test temporaneo: cavaliere
sull'ingresso, percorso completo fino alla stanza della scala, a piedi dalla prima
stanza a quella centrale in 6,4 s, cella della scala non calpestabile. Test: 18
EditMode e 46 PlayMode verdi.

---

## Passo 3.5 — Livelli e cambio di livello

1. Due mappe: **livello 1**, quattro o cinque stanze con corridoi, tre o quattro
   scheletri, scala in fondo; **livello 2**, più piccolo, con l'ingresso dalle scale.
   Le mappe le disegno io e Mirco le rivede nel diff, anche solo per dire "questa
   stanza più grande".
2. `LevelExit` sulle scale, layer `Interactable`, e il ramo nuovo in `PlayerController`.
3. Pannello di dissolvenza nell'HUD, in tempo non scalato (come l'hit stop).

**Verifica:** in build, dal livello 1 si scende al 2 con la vita che si aveva.
**Test:** `LevelTransitionTests` — la vita sopravvive al cambio; il livello vecchio è
scaricato; il cavaliere è sull'ingresso e il suo agent è sul NavMesh; i nemici del
livello nuovo sono collegati (numeri di danno, IA); durante la transizione i click
sono ignorati.

**Com'è andata (3 ott 2026).** Mappe composte da uno script che verifica che ogni cella
sia raggiungibile dall'ingresso e che torce e scala stiano contro un muro: **livello 1**
23×13 celle, cinque stanze, quattro scheletri, scala a est (`@exit Level_02 FromAbove`);
**livello 2** 15×11, due stanze e due scheletri, senza uscita. `Level_Test` cancellato;
il builder ora tiene allineati i *Build Profiles* (le scene dei livelli sono esattamente
quelle con una mappa). **Uscita:** il builder mette sulla scala un oggetto `Exit` con
`LevelExit` (trigger più `Rigidbody` cinematico, perché il player si muove con l'agent e
senza un corpo rigido i trigger non scattano), un `ClickArea` separato sul layer
`Interactable` (il raggio del click ignora i trigger) e `Interactable` con il punto
d'arrivo appena dentro il NavMesh. Il `LevelContext` raccoglie le uscite e le riporta al
`LevelManager`, che sta in `Core` e che le uscite non conoscono. `PlayerController` ha
il ramo nuovo previsto dall'ADR-006. **Dissolvenza:** `ScreenFader` sull'HUD, nero
all'avvio, 0,35 s in tempo non scalato, non blocca mai i click (altrimenti i test che
cliccano subito dopo il caricamento sarebbero falliti). Primo livello: `Level_01`; dopo
la morte si riparte da lì. Test: 18 EditMode e 51 PlayMode verdi (`LevelTransitionTests`:
click sulla scala → livello 2 con la stessa vita, dissolvenza, un morto non scende,
controller spento fino al livello pronto; `LevelDataTests`: ogni uscita porta a una
scena della build e a un ingresso che esiste).

---

## Passo 3.6 — Buio e luci

1. In ogni livello: ambiente quasi nero, nessuna luce direzionale, `Volume` con
   tonemapping, bloom e vignetta. Le impostazioni di luce della scena valgono solo
   se è la **scena attiva**: per questo il `LevelManager` la rende attiva.
2. Luce del cavaliere: puntiforme, sopra la testa, con ombre morbide; raggio tarato
   perché si veda una stanza piccola, non tutta.
3. Torce: modello KayKit più luce puntiforme senza ombre, colore caldo, `TorchFlicker`.
   La mappa le mette sui muri (`T`).
4. Distanza delle ombre ridotta a circa 25 m; risoluzione della mappa delle ombre
   per le luci aggiuntive tarata sulla qualità vista in build.
5. Scheletri: un leggero materiale emissivo sugli occhi o niente, a occhio.

**Verifica:** a occhio in build, con Mirco: il buio deve fare paura ma i bordi delle
stanze si devono leggere. **Test:** nessuno di aspetto; un test che ogni livello ha
un `Volume` e nessuna luce direzionale.

---

## Passo 3.7 — itch.io

1. **Mirco:** account itch.io (se non c'è già), progetto nuovo "DarkDescent", tipo
   *Downloadable*, gratuito, stato **In development**, piattaforma Windows, visibilità
   *Draft* finché la prima build non è su.
2. **Mirco:** chiave API di butler (*Settings → API keys*) nel secret
   `BUTLER_API_KEY` del repo. Come per Unity: non passa dalla chat.
3. `build.yml`: dopo l'artifact, sui soli tag, un passo che scarica butler 15.31.0
   dal sito ufficiale e fa `butler push` della cartella sul canale `windows`, con la
   versione uguale al tag.
4. Tag di prova o avvio manuale con il caricamento acceso, poi **Mirco** rende
   pubblica la pagina.
5. **Prova su un altro PC:** Mirco, o qualcuno a cui manda il link, scarica da
   itch.io e gioca.

**Verifica:** la pagina pubblica mostra la build, e su un PC che non è quello di
Mirco il gioco parte.

---

## Passo 3.8 — Prova della build Web

1. Un avvio manuale di `build.yml` con `targetPlatform: WebGL`, compressione con
   *decompression fallback* (itch.io non manda gli header per i file compressi).
2. Mirco la prova nel browser dall'artifact: aspetto delle luci, fluidità, audio
   (nel browser parte solo dopo il primo click).
3. Se regge: canale `web` su itch.io, giocabile nella pagina. Se no: resta Windows e
   si riprova alla M11. In tutti e due i casi, ADR.

---

## Passo 3.9 — Chiusura

1. GIF del README: discesa delle scale, dal buio della cripta al livello 2.
2. ADR: struttura delle scene (D1), mappe di testo (D2), luci in tempo reale (D4),
   muri bassi (D5), itch.io con butler (D6), build Web (D7).
3. Lezioni nel piano, tabella dello stato, tag `m3`: la CI produce la build e la
   carica su itch.io.

---

## Trappole note

1. **La scena attiva decide luci e ambiente,** e anche dove finiscono gli oggetti
   creati con `Instantiate`. Dopo il caricamento additivo va chiamato
   `SceneManager.SetActiveScene` sul livello, altrimenti il dungeon prende il cielo
   di `Core`.
2. **Un `NavMeshAgent` senza NavMesh sotto** dà "Failed to create agent" (già visto
   alla M2) e non si sposta più. In `Core` l'agent del player resta spento finché un
   livello non è caricato; poi `Warp`, mai `transform.position`.
3. **Cinemachine dopo un teletrasporto** fa attraversare la mappa alla camera con lo
   smorzamento. Va chiamato `OnTargetObjectWarped` sulla `CinemachineCamera`.
4. **Tutorial per Cinemachine 2.x:** `CinemachineVirtualCamera` e *Framing
   Transposer* non esistono più; ora sono `CinemachineCamera` e *Position Composer*,
   namespace `Unity.Cinemachine` (piano § 3).
5. **Riferimenti a oggetti del livello scaricato:** il bersaglio tenuto premuto dal
   player, le iscrizioni dei numeri di danno. Vanno puliti **prima** di scaricare,
   altrimenti puntano a oggetti distrutti, il null "finto" di Unity della M2.
6. **Livello già aperto nell'editor:** se `Core` e un livello sono tutti e due nella
   Hierarchy quando si preme Play, il `LevelManager` non deve caricarne una seconda
   copia.
7. **Due `AudioListener`:** Unity avvisa a ogni frame. Quello della camera va tolto.
8. **Le ombre delle luci puntiformi costano sei volte quelle di una spot.** Una sola
   luce con ombre; se la mappa delle ombre è piena, URP le spegne senza avvisare.
9. **I muri alti bloccano anche i click** (ADR-006): un motivo in più per i muri
   bassi verso la camera.
10. **Nomi delle scene come stringhe:** un errore di battitura si scopre solo
    caricando. Un test percorre tutte le uscite e verifica che ogni destinazione sia
    nei *Build Profiles*.
11. **Build Web:** `Application.Quit` non fa niente, e l'audio parte solo dopo
    un'interazione dell'utente. Se manca il *decompression fallback*, la pagina resta
    nera su host che non mandano gli header giusti.

---

## Test

| Classe | Cosa verifica |
|---|---|
| Esistenti (47) | Tutto il combattimento della M2, ora su `Core` + sandbox |
| `LevelTransitionTests` | Vita conservata, livello vecchio scaricato, cavaliere sull'ingresso e sul NavMesh, nemici collegati, click ignorati durante la transizione, restart da `Core` |
| `LevelDataTests` | Ogni uscita porta a una scena nei *Build Profiles* e a un ingresso esistente; ogni livello ha `Volume`, `NavMeshSurface` e nessuna luce direzionale |
| `CameraTests` | Camera sul cavaliere al frame dopo un `Warp` |
| `AudioTests` | Un solo `AudioListener`, sorgenti dei personaggi in 3D |
| EditMode | Lettura delle mappe di testo (caratteri, dimensioni, ingressi) |

---

## Checklist di chiusura

- [ ] Decisioni D1–D8 confermate
- [ ] `Core` + livelli additivi, test esistenti verdi
- [ ] Camera Cinemachine con zona morta e Impulse
- [ ] Audio posizionale
- [ ] Tileset KayKit importato, riga in `CREDITS.md`
- [ ] Livelli 1 e 2 dalle mappe, scale funzionanti, vita conservata
- [ ] Buio, torce e luce del cavaliere, giudicati in build
- [ ] Pagina itch.io pubblica, build caricata dalla CI
- [ ] Build provata su un PC che non è quello di Mirco
- [ ] Build Web provata e decisa in un ADR
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m3`
- [ ] Scheda della M4 scritta prima di cominciarla
