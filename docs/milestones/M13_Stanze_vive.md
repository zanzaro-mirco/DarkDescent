# M13 — "Stanze vive"

**Cosa deve succedere (Definition of Done):** le stanze della cripta e delle caverne hanno un senso.
Il **colonnato** non c'è più; gli arredi seguono il tema della stanza, non si ripetono e non stanno
tutti girati allo stesso modo. Al centro delle stanze grandi c'è al più un **punto d'interesse**: un
gruppo di **barili da rompere**, un **pozzo** della vita o del mana, un **santuario** che dà un bonus
per un minuto, o una zona di **trappole**. I modelli che i pacchetti non hanno (pozzi, santuari,
sarcofagi, statue, anfore, gabbie e gli altri della D9) li facciamo **in Blender, da script** che
stanno nel repo. Anche i **suoni** nuovi (santuari, pozzi, trappole, barili) li generiamo **da
script**, con Python, numpy e scipy (D14). Il **loot** torna a una quantità giusta, misurata: meno oggetti, ma migliori (D13).
Esce con il tag `v1.2.0`.

**Tempo stimato:** 22–28 h (D12).
**Prerequisito:** M12 chiusa (tag `v1.1.0`), Blender 5.2 LTS installato (19 ott 2026), numpy 2.5.4 e scipy
1.18.1 installati (19 ott 2026).

**Come si lavora:** come alla M12: il codice, gli script di Blender e i passaggi nell'editor li faccio
io, in batchmode a Unity chiuso. Ogni modello nuovo passa prima da una foto accanto a un modello
KayKit, che Mirco approva, e ogni suono nuovo da una pagina di ascolto accanto al suono più vicino
che il gioco ha già. A Mirco restano le decisioni qui sotto, le foto dei modelli, l'ascolto dei suoni
e la prova in build.

---

## Stato verificato il 19 ottobre 2026

| Cosa | Stato |
|---|---|
| Arredi | `DungeonPopulator.Decorate`, con un flusso suo del seme (M11, D4). Quattro temi (deposito, studio, cella, rovina) con cinque composizioni ciascuno contro i muri (`DecorThemes`), fino a 4 per stanza. Il **colonnato** `o` (`ColumnCells`, due file simmetriche a una cella dai muri) nell'85% delle stanze da 5 × 5 in su (`ColumnChance` 0,85 nella cripta e nelle caverne). Mucchietti a terra `v` `y` `z` (ossa, cianfrusaglie, armi rotte), fino a 2 per stanza, di preferenza lontano dai muri |
| Oggetti di scena | `b` barile, `x` casse, `p` pilastro contro i muri, dal flusso principale del seme: si vedono, non si rompono |
| Orientamento | Il `LevelBuilder` gira ogni oggetto lontano dal muro più vicino (`AwayFromWall`); in mezzo alla stanza, senza muri, tutti verso sud: per questo due mucchietti uguali sembrano copie (foto di Mirco del 19 ott) |
| Simboli liberi | Nella cripta e nelle caverne insieme restano liberi `i` `k` `r` `s` tra le minuscole, `T` è la torcia; maiuscole libere nella cripta, ma molte sono già delle caverne (`A` `B` `C` `G` `H` `I` `J` `K` `M` `P` `Q` `X`): i simboli sono unici per tutto il gioco, perché `IsObstacle` li guarda insieme |
| Interazione | `Interactable`: click, punto di arrivo, evidenziazione, nome nell'HUD (`_labelKey`). Il cavaliere attacca chi ha una `Health` sul layer dei nemici (`PlayerController`) |
| Statistiche | `StatSheet` con modificatori per sorgente (`AddModifier`, `RemoveModifiersFrom`): nessun bonus a tempo. La velocità non è una statistica: la cambia solo `MovementSlow` (M12, ADR-091) |
| Ricordi dei livelli | `LevelRecord`: numero di nemici e casse, nemici uccisi, casse aperte, oggetti a terra (ADR-079, formato 5). I campi nuovi si leggono anche dai salvataggi vecchi (`??=`) |
| Modelli | KayKit *Dungeon* (gratuito): niente statue, sarcofagi, urne, gabbie, altari, pozzi. Ha `floor_tile_big_spikes` (spuntoni), `keg`, `barrel_*`, `rubble_*`. Quaternius *Ultimate RPG Items* (zip già scaricato): libri, pergamene, ossa, teschi, cristalli |
| Blender | 5.2.2 LTS in `C:\Program Files\Blender Foundation\Blender 5.2`. Il pozzo di prova (19 ott) si costruisce, si renderizza e si esporta in FBX da script in 4 secondi, con i colori dell'atlante KayKit |
| Suoni | `Audio/SFX/Generated` ha 13 suoni sintetizzati da script Python durante le milestone precedenti (salire di livello, monete, portale, magie, click), ma gli script non sono nel repo. La prova del 19 ott, con numpy e scipy, ha fatto quattro suoni della M13: rintocco del santuario, scatto della trappola, barile che si rompe (tutto sintetizzato e ibrido) e sorso dal pozzo. Mirco li ha approvati tutti |
| ComfyUI | ComfyUI Desktop installato da Mirco il 20 ott 2026: istanza "DarkDescent", ComfyUI v0.42.1 con il Manager, nessun modello. Con l'app aperta risponde su `127.0.0.1:8188`. Stable Audio 3.0 e TRELLIS.2 sono nodi del core, senza nodi aggiuntivi |
| Git LFS | `.fbx`, `.blend` e `.png` vanno già in LFS |
| Loot | Probabilità di un oggetto per nemico: scheletro 30%, mago 35%, bruto 60%, sciame 7%; le scorte dei campioni come i nemici normali. Campione: il tiro di sempre più uno sicuro; unico: tre oggetti, uno raro (`LootBonus`, ADR-092). Cassa: un oggetto sicuro; forziere d'oro: due, accanto all'unico e al 50% dei campioni. Rarità di base 65/28/7. Stima per la cripta alla profondità 3: **circa 10 oggetti a livello**, contro i 6,5 di prima della M12; Mirco, 19 ott: "troppo" |
| Test | 340 EditMode e 212 PlayMode verdi |

---

## Decisioni

Confermate da Mirco il 19 ottobre 2026, tutte come proposte.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M13 "Stanze vive"**, tag **`v1.2.0`**. I gradi degli affissi e le difficoltà passano alla M14 | Le richieste di Mirco del 19 ott (via il colonnato, arredi con un senso, idee dalla sua lista) toccano tutte il popolatore: conviene farle insieme |
| D2 | **Via il colonnato** | Il simbolo `o` non si genera più (`ColumnChance` sparisce). Il tiro che lo decideva resta nel flusso degli arredi e sceglie il **punto d'interesse** (D4), così il resto degli arredi di un seme non scorre. I pilastri `p` contro i muri restano | Mirco non vuole le colonne vicino ai muri; tenere il tiro evita di cambiare gli arredi di ogni seme |
| D3 | **Arredi con un senso** | (a) **Mai due volte** la stessa composizione né lo stesso mucchietto nella stessa stanza. (b) In mezzo alla stanza ogni oggetto ha una **rotazione** e un piccolo **scarto dal centro della cella** presi da un hash della cella, non dal flusso del seme. (c) **Mucchietti per tema**: cella → ossa, teschi, catene; deposito → cocci, sacchi; studio → libri, pergamene, candele; rovina → armi rotte, macerie. (d) **Piatti e bottiglie solo accanto a un tavolo**, su una cella libera vicina a una composizione dello studio o del deposito | Sono le tre cose che si vedono nella foto di Mirco: copie, stesso verso, oggetti senza un motivo per stare lì |
| D4 | **Punto d'interesse** | Nelle stanze da 5 × 5 in su (dove andava il colonnato), mai nella stanza di partenza né nella sala del re: **barili** 35%, **niente** 25%, **pozzo** 15%, **santuario** 15%, **trappole** 10% (dalla profondità 3). Al più **un pozzo e un santuario per livello**: un secondo tiro uguale diventa barili. Sta nel centro della stanza, mai sul percorso tra le porte | Una cosa sola al centro dà un senso alla stanza; lo "niente" tiene le stanze vuote possibili, come in Diablo |
| D5 | **Barili da rompere** | Come nel piano (v2.32, v2.35): si rompono con **un colpo**, cliccandoli come un nemico, senza esperienza e senza barra in alto. Gruppi di **2–4** contro i muri e negli angoli e, come punto d'interesse, in mezzo alla stanza. Per livello **4–8** nella cripta, **2–5** nelle caverne. Loot per barile, a tiri separati: **oro 15%** (metà di uno scheletro), **oggetto 5%**, **pozione 5%**, in una `LootTable` sua; seme da un dominio suo (`SeedMixer.ForBarrel`). Anche il barile `b` di oggi diventa da rompere; nelle composizioni del deposito i barili diventano botti (`keg`), che restano arredo. Rotto, entra nei ricordi del livello | Se alcuni barili si rompono e altri no, il giocatore li clicca tutti: ogni barile singolo si rompe, le botti no |
| D6 | **Santuari** | Una **stele** (modello nostro) con un cristallo colorato. Si clicca e dà un **bonus per 60 s**, uno solo alla volta (uno nuovo prende il posto del vecchio). Quattro tipi: **Furia** (danno +25%, rosso), **Vento** (velocità +25%, verde), **Pietra** (armatura +50%, grigio), **Sapienza** (esperienza +50%, viola). Usato, il cristallo si spegne. Un'icona con il tempo che resta accanto alla cintura, il nome nell'HUD al passaggio del mouse e un messaggio all'uso. Entra nei ricordi del livello | Dalla lista di Mirco ("santuari e stele"): un bonus breve cambia il modo di affrontare la stanza dopo, senza toccare il bilanciamento a lungo termine |
| D7 | **Pozzi** | Il **pozzo della vita** (rosso) riempie tutta la vita, il **pozzo del mana** (blu) tutto il mana. **Una volta sola**: il liquido si abbassa e il cristallo si spegne. Luce colorata e un po' di emissione, come il forziere d'oro. Entra nei ricordi del livello | Il modello di prova c'è già; un pozzo è il premio di una stanza difficile |
| D8 | **Trappole** | (a) **Spuntoni** (`floor_tile_big_spikes`): a riposo 2 s, avviso 0,5 s (le punte affiorano, un suono), fuori 1 s. Feriscono **solo il cavaliere**, per una parte della vita massima che cresce con la profondità. Dalla profondità 3: **0–2** per livello nella cripta, **1–3** nelle caverne, nei corridoi o come punto d'interesse; si attraversano a tempo, quindi possono stare sul percorso. (b) **Barili esplosivi** dalla profondità 4: un barile su cinque nei gruppi, cerchiato di rosso. Rotto, dopo una miccia di 1 s esplode nel raggio di 3 m e ferisce **tutti**, nemici compresi | Dalla lista di Mirco ("trappole meccaniche", "barili esplosivi"); il barile che ferisce anche i nemici diventa un'arma da usare |
| D9 | **Modelli in Blender** | Script Python in **`Tools/Blender/`**: una libreria comune (forme, smusso, colori dall'atlante KayKit, render di prova, export FBX con gli assi di Unity) e uno script per modello. Gli FBX in `Art/Models/DarkDescent/`. **Blender 5.2 LTS** bloccato come Unity (un ADR). Ogni modello passa da una **foto accanto a un modello KayKit** prima di entrare in Unity. Modelli: **pozzo** (vita e mana), **stele del santuario**, **barile esplosivo**, **sacchi**, **anfore** (intera e rotta), **sarcofago**, **statua** del guardiano, **gabbia**, **altare** con candele, **mucchio di libri e pergamene**. Con questi i temi si arricchiscono: la cella prende la gabbia, il deposito anfore e sacchi, lo studio i libri, e un **quinto tema, la cappella** (sarcofago, statua, altare, candele) | Mirco, 19 ott: "una sezione dove creare oggetti nuovi con Blender per arricchire il gioco". Gli script nel repo rendono ogni modello rifacibile e modificabile; la licenza è nostra |
| D10 | **Si capisce cosa si usa** | Pozzi e santuari si evidenziano al passaggio del mouse come le casse, con il nome nell'HUD ("Santuario della Furia", "Pozzo della vita"); usati, il nome dice "(vuoto)". I barili hanno il cerchio dei bersagli dei nemici. Tutto nelle due lingue | Senza, un pozzo è un arredo come gli altri |
| D11 | **Fuori da questa milestone** | Dalla lista di Mirco restano per dopo, nel piano: **mimic** e **bauli maledetti** (un'ondata di nemici prima di aprirsi), **porte, leve e stanze sigillate**, **lore** (leggii, diari), **prigionieri** da liberare, **statue che si animano**, pozze di acido e lava, statue che sputano fuoco | Ognuna è un sistema a sé; questa milestone resta sulle stanze |
| D12 | **Stima** | **22–28 h**. Se servirà tagliare: prima i barili esplosivi, poi il quinto tema, poi gli spuntoni. Il loot (D13) non si taglia | Meglio deciderlo adesso che a metà |
| D14 | **Suoni da script** | Script Python in **`Tools/Audio/`**: una libreria comune (inviluppi, filtri, risonatori, rumore, campane, bolle, riverbero a convoluzione, lettura e cambio di tono dei sample, export WAV 44,1 kHz a 16 bit normalizzato) e uno script per suono, con un **seme fisso**, così ogni WAV si rifà uguale. Versioni bloccate in `Tools/Audio/requirements.txt` (numpy 2.5.4, scipy 1.18.1). I WAV in `Audio/SFX/Generated`, nei crediti come nostri. Ogni suono passa da una **pagina di ascolto** con lo spettrogramma, accanto al suono più vicino che il gioco ha già. Suoni: **santuario** (un rintocco per tipo, con il tono che cambia), **pozzo** (sorso), **barile che si rompe** in **due varianti**, la sintetizzata e l'ibrida, scelte a caso a ogni rottura, **spuntoni** (avviso e scatto), **barile esplosivo** (miccia ed esplosione) e **fine del bonus** del santuario | Mirco, 19 ott 2026: la prova va bene, e si tengono tutti e due i barili. I suoni "geometrici" (campane, meccanismi, acqua, magie) vengono bene da script; il legno e le esplosioni vengono meglio con un sample elaborato, le voci e le creature restano ai sample |
| D15 | **ComfyUI per suoni e modelli generati** | ComfyUI Desktop sul PC di Mirco, guidato da Claude attraverso l'API locale (`127.0.0.1:8188`) con un piccolo client Python in `Tools/ComfyUI/`: workflow in formato API nel repo, prompt e seme di ogni asset salvati accanto, così ogni risultato si rifà. **Audio**: Stable Audio 3.0 **Small SFX** (`stable_audio_3_small_sfx`, 2,27 GB, più l'encoder `t5gemma_b_b_ul2`, 1,19 GB), licenza *stable-audio-community*. **3D**: TRELLIS.2 in **int8** (`trellis_2_int8_convrot`, 5,25 GB, più i due VAE e DINOv3, circa 3,3 GB) a risoluzione 512 per i 12 GB di VRAM, senza il ramo Pixal3D (segnalato come bloccato nell'UE). I modelli li scarica Mirco. Ogni asset generato passa dalla stessa prova di quelli da script (pagina di ascolto o foto accanto ai KayKit), si rifinisce con `Tools/Audio` o `Tools/Blender`, e va in `CREDITS.md` con il modello e la licenza. Le licenze (DINOv3 sui risultati, registrazione Stability per l'uso commerciale) si chiudono in un ADR prima del primo asset nel gioco | Mirco, 20 ott 2026: ComfyUI installato per generare suoni e modelli. La prova del 19 ott ha mostrato che lo script regge le forme geometriche ma non quelle organiche (l'elmo); un modello generato può fare da base per la statua e i pezzi organici, e Stable Audio può dare il legno, l'acqua e i colpi che la sintesi fa peggio |
| D16 | **Immagine di partenza: una foto da Blender, nello stile KayKit** | TRELLIS.2 parte da un'immagine. Quella standard è una **foto da Blender** di una bozza dell'oggetto: un modello di `Tools/Blender` o pezzi KayKit montati insieme (CC0), fotografati di tre quarti su sfondo uniforme con `Tools/ComfyUI/views.py reference`. La bozza può essere grossolana (proporzioni e colori giusti), il dettaglio organico lo aggiunge TRELLIS; lo stile resta KayKit perché forme e colori sono nostri, e la foto non porta licenze nuove. La foto sta nel repo accanto alla ricetta. Un workflow testo → immagine per gli oggetti che non vale la pena abbozzare, con Z-Image-Turbo (Apache 2.0, circa 8,3 GB in nvfp4) e un prompt in stile KayKit, è negli **sviluppi dopo la v1.0** (Mirco, 20 ott 2026, dopo la prova del 13.4). Scartati: Flux.1 dev e SDXL-Turbo (licenze non commerciali), Flux.2 Klein 4B (il VAE dei workflow di ComfyUI viene da un repository con licenza non commerciale), i nodi API a pagamento. Il GLB diventa FBX con un comando di `views.py` (assi di Unity, scala della cella, base a terra, davanti verso la stanza) **solo dopo la foto e il sì di Mirco**: niente va dritto negli asset | Mirco, 20 ott 2026: una pipeline trovata in rete proponeva testo → immagine → TRELLIS → Blender → FBX in stile Diablo IV. Lo stile resta KayKit; della proposta si tiene la conversione in FBX, e per l'immagine Mirco sceglie la foto da Blender, senza scaricare un modello in più |
| D13 | **Loot: meno oggetti, ma migliori** | Obiettivo **4–5 oggetti a livello** in media, con la stessa parte di magici e rari. Probabilità per nemico: scheletro **15%**, mago **25%**, bruto **35%**, sciame **4%**; le **scorte** dei campioni a **metà** della probabilità del loro tipo. **Campione**: un solo oggetto, **sicuro**, con le rarità spostate (al posto del tiro di sempre più uno). **Unico**: **due** oggetti, uno raro. **Cassa**: oggetto al **50%**, e senza oggetto più oro o una pozione. **Forziere d'oro**: accanto all'unico e al **25%** dei campioni. Barili, pozzi e santuari della M13 entrano nel conto. I numeri si **misurano**: uno strumento nell'editor genera 200 livelli per profondità e conta gli oggetti per fonte e per rarità; un test EditMode tiene la media nella fascia | Mirco, 19 ott 2026: con tutti i mostri e gli oggetti aggiunti il loot è troppo. In un ARPG un oggetto a terra deve contare; conviene tarare tutto insieme, dopo che barili, pozzi e santuari ci sono |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 13.1 | Blender e audio: la libreria comune in `Tools/Blender/`, il pozzo rifatto con la libreria, l'import in Unity con gli assi giusti, l'ADR della versione (D9); la libreria in `Tools/Audio/` con i quattro suoni della prova (D14) | 2–3 |
| 13.2 | Arredi con un senso: via il colonnato, mai doppioni, rotazione e scarto, mucchietti per tema, punto d'interesse nella logica pura, con i test (D2–D4) | 2–3 |
| 13.3 | Modelli per gli arredi: sacchi, anfore, sarcofago, statua, gabbia, altare, libri; le composizioni nuove e il tema della cappella (D3, D9) | 3–4 |
| 13.4 | ComfyUI: Mirco scarica i modelli; il client Python in `Tools/ComfyUI/` con i workflow in formato API (Stable Audio 3.0 Small SFX, TRELLIS.2 senza Pixal3D); la prova sui quattro suoni della M13 accanto a quelli da script e su un modello organico accanto alla versione da script; la foto di partenza da Blender (D16); la conversione da GLB a FBX; l'ADR con le licenze (D15, D16) | 2–3 |
| 13.5 | Barili da rompere: `Breakable`, il click, il loot con il suo seme, i ricordi del livello (D5) | 2–3 |
| 13.6 | Pozzi e santuari: i modelli, il bonus a tempo, l'icona, i ricordi del livello (D6, D7, D10) | 3–4 |
| 13.7 | Trappole: spuntoni e barili esplosivi (D8) | 2–3 |
| 13.8 | Loot: lo strumento che conta gli oggetti per livello, i numeri nuovi, il test della fascia (D13) | 1,5–2 |
| 13.9 | Build da provare e correzioni | 1–2 |
| 13.10 | Chiusura: tag `v1.2.0`, ADR, lezioni, README | 1 |

## Passo 13.1 — Blender e audio

**Com'è andata (19 ott 2026).** In `Tools/Blender/` la libreria comune (`meshlib.py`: colori
dall'atlante KayKit, scatole, solidi di rivoluzione, anelli, prismi, cristalli, smusso, foto con la
camera del gioco, export FBX) e `make.py`, che gira dentro Blender e costruisce tutti i modelli di
`models/`, o quelli nominati:

```
blender --background --factory-startup --python Tools/Blender/make.py -- [nomi] [--photo DIR]
```

Gli FBX vanno in `Art/Models/DarkDescent/`; con `--photo` ogni modello ha anche la sua foto accanto
al modello KayKit di confronto. Il primo modello è il **pozzo** della prova, in due FBX
(`Well_Life`, `Well_Mana`: cambiano il liquido e il cristallo), 2,38 × 1,83 m, 646 facce.

In Unity un `AssetPostprocessor` dell'editor (`GeneratedModelImporter`) dà agli FBX della cartella
le impostazioni dei KayKit: niente materiali (il prefab mette `M_Dungeon`, che ha già l'atlante),
niente animazioni, camere o luci. Gli FBX arrivano senza rotazione e in scala 1 al primo import:
la trappola del `chest_gold` della M12 non si ripresenta, perché l'export applica la trasformazione
(`bake_space_transform`). `GeneratedModelTests` lo controlla per ogni FBX della cartella (impostazioni,
rotazione zero, scala 1, base a terra, misure da stanza) e, per il pozzo, che le misure siano quelle
di Blender, cioè che gli assi non siano scambiati.

In `Tools/Audio/` la libreria dei suoni (`sfxlib.py`), `make.py` e `preview.py` per la pagina di
ascolto, con le versioni bloccate in `requirements.txt`:

```
python Tools/Audio/make.py [nomi] [--out DIR --page]
```

Uno script per suono in `sounds/`; il seme di ogni suono viene dal suo nome, così un WAV si rifà
identico byte per byte e cambiare un suono non cambia gli altri. Dentro `Audio/SFX/Generated` ci sono
i quattro della prova: `shrine_chime`, `trap_trigger`, `barrel_break_0` (sintetizzato),
`barrel_break_1` (ibrido) e `well_drink`. Con il seme per nome i dettagli casuali sono un po' diversi
da quelli ascoltati da Mirco (la stessa ricetta, altri tiri): si riascoltano in build. L'ibrido parte
dallo schianto di ossa di Puadlung e ne prende la licenza, scritta nella riga di Puadlung di
`CREDITS.md`; gli altri sono CC0, in una riga loro.

ADR-094 (Blender 5.2 LTS e modelli da script) e ADR-095 (suoni da script). 343 EditMode verdi.

## Passo 13.2 — Arredi con un senso

**Com'è andata (20 ott 2026).** Il **colonnato** non c'è più: via `ColumnChance`, le file di colonne
e il prefab `Decor_Column`. Il tiro che lo decideva ora sceglie il **punto d'interesse** delle stanze
da 5 × 5 in su (`DungeonSettings.FeatureFor`: barili 35%, pozzo 15%, santuario 15%, trappole 10% dalla
profondità 3, niente il resto). Un secondo pozzo o santuario nello stesso livello diventa barili, e
così le trappole prima della profondità 3. Mai nella stanza di partenza. Il punto sta nella cella
libera più vicina al centro, lontano dai muri, dai passaggi, da casse e scala e dai **percorsi più
brevi tra le porte** della stanza (`DoorPaths`), e solo se non chiude il cammino. Il layout ricorda
ogni punto (`DungeonLayout.Features`: stanza, tipo, cella), e la cella resta riservata: pozzi,
santuari e trappole arrivano nei passi 13.6 e 13.7 proprio lì, senza spostare gli altri arredi. Per
ora si vedono solo i **barili**: `Decor_BarrelGroup` (simbolo `0`), tre barili KayKit in diagonale.

Le lettere per i simboli sono finite (tutte le maiuscole e quasi tutte le minuscole sono usate):
le cose nuove della M13 prendono le **cifre**, a partire da `0`.

**Mai doppioni**: nella stessa stanza ogni composizione e ogni mucchietto compare al più una volta.
**Mucchietti per tema** (`ScatterThemes`): ossa nella cella, armi rotte nella rovina, piatti e
bottiglie nel deposito e nello studio, e questi solo **accanto a un mobile** della stanza. Sacchi,
cocci, libri, catene e macerie arrivano con i modelli del passo 13.3. **Rotazione e scarto**: i
mucchietti e i barili in mezzo alla stanza, senza muri accanto, prendono un angolo a passi di 15° e
uno scarto fino a 0,6 m da un hash della cella (`DecorJitter`), non dal flusso del seme; contro un
muro restano girati verso la stanza, per non entrarci dentro.

Test: `DecorTests` (niente colonne, niente doppioni, mucchietti del tema, piatti accanto ai mobili,
punto d'interesse in 400 livelli: dove sta, quanti pozzi e santuari, niente trappole prima della
profondità 3, tutti i tipi presenti; il tiro; la rotazione dalla cella) e `DungeonContentTests`
(in mezzo alle stanze solo i barili del punto d'interesse). `SpecialHudTests` cercava un posto
accanto a Morvak solo a sud, dove ora c'è un gruppo di barili: prova i quattro lati. 348 EditMode
e 215 PlayMode verdi.

## Passo 13.3 — Modelli per gli arredi

**Com'è andata (20 ott 2026).** Dieci modelli nuovi in `Tools/Blender/models/`, uno script per tema:
`storage.py` (sacco rattoppato e legato, anfora, anfora rotta con i cocci curvi e il collo rotolato),
`study.py` (pila di libri con segnalibri e angoli d'ottone, libro aperto, calamaio con la penna,
pergamena), `cell.py` (catene con piastra imbullonata e manette, gabbia con porta, cardini e
lucchetto), `ruin.py` (macerie con rocchio, capitello e asse spezzata), `chapel.py` (sarcofago con il
coperchio appena scostato, statua del guardiano incappucciato con la spada, altare con drappo,
candelabro e libro). Due giri di foto accanto ai KayKit; nel secondo Mirco ha chiesto di raffinarli
ancora. La libreria ha preso `torus`, `rock`, `jagged_lathe`, `beam` e `transform`, i nomi di tutti i
riquadri dell'atlante, e un confronto e un'inquadratura per modello (`COMPARES`, `PHOTOS`); `finish`
appoggia ogni modello a terra (un libro inclinato finiva 4 cm sotto il pavimento).

**Prefab e temi.** Le lettere sono finite: i simboli nuovi sono cifre e segni. Deposito: `7`
(sacchi e anfora), `8` (anfore e sacco). Cella: `9` (la gabbia, con dentro le ossa del mucchietto di
sempre). Il **quinto tema, la cappella**: `+` sarcofago, `&` statua con le candele, `%` altare, `=`
due urne con le candele (prima era un secondo sarcofago: in una stanza sembravano copie). Mucchietti:
`2` libri (studio), `3` macerie (rovina), `4` cocci (deposito), `5` catene (cella), `6` candele
(cappella). Con cinque temi ogni stanza può cambiare tema rispetto a prima, come previsto nelle
trappole note.

**Una correzione al pianificatore dei nemici speciali.** Con più arredi l'unico poteva finire in una
cella stretta, senza posto per la scorta (seme 32 della cripta): ora sceglie tra le celle dove la
scorta più piccola ci sta, se ce ne sono (`SpecialEnemyPlanner.EscortCells`).

Test: `DecorTests.EveryThemeAndPieceShowsUp` (in 100 livelli esce ogni composizione e ogni
mucchietto, cappella compresa); gli altri test degli arredi coprono i simboli nuovi da soli
(tileset, collider, dentro la cella, niente doppioni, mucchietti del tema). `CREDITS.md` ha la riga
dei modelli fatti in Blender, pozzo compreso. 349 EditMode e 215 PlayMode verdi.

## Passo 13.4 — ComfyUI

**Com'è andata (20 ott 2026).** `Tools/ComfyUI/` guida ComfyUI attraverso l'API locale, con la sola
libreria standard. `comfy.py` cambia gli ingressi per titolo del nodo, controlla i modelli che mancano
(`make.py --check`), carica le immagini, mette in coda e scarica i risultati. Ci sono due workflow in
formato API: `stable_audio_sfx` (8 passi, lcm) e `trellis2_image_to_model` (la pipeline a 512 senza
Pixal3D, poi ricostruzione, riduzione a 8000 facce, UV, texture 1024 e GLB). Prima che Mirco scaricasse
i modelli li ho validati su un ComfyUI temporaneo con file finti. I modelli sono nella cartella
condivisa dell'app (`ComfyUI-Shared/models`), e per la prova ComfyUI è girato senza l'app, sulla porta
8199. `soundfile` legge i FLAC che ComfyUI salva.

**Suoni.** Quattro ricette, tre semi l'una, in 17 s; per il pozzo ne sono serviti quattro in più,
con un prompt nuovo che parla solo d'acqua (il primo, con i sorsi, dava colpi cupi). Mirco ha scelto
le versioni generate: barile 1301, santuario 1302, trappola 1303, pozzo 1402. Rigenerati da ricetta,
i WAV escono identici a quelli ascoltati. Nei passi 13.5–13.7 il gioco usa le versioni `_sa`: il
barile generato entra tra quelli scelti a caso, con i due da script (D14); santuario, trappola e pozzo
usano il loro. I WAV da script restano come riserva.

**Modelli.** TRELLIS ha rifatto la statua in 40 s partendo dalla foto della versione da script
(`views.py reference`): 7996 facce contro 1721, quasi uguale, più morbida, con la targa copiata anche
sul retro. Da una foto senza dettaglio non ne aggiunge: la statua resta quella da script. Mirco ha
deciso di non scaricare Z-Image per ora: l'immagine di partenza da testo, la conversione da GLB a FBX
e la lettura della licenza di DINOv3 vanno negli sviluppi dopo la v1.0 del piano.

ADR-096 (suoni generati e licenze). `CREDITS.md` ha la riga dei suoni generati.

## Passo 13.5 — Barili da rompere

**Com'è andata (21 ott 2026).** `Breakable` è un `IDamageable` senza `Health`: sta sul layer dei
nemici, si clicca e si colpisce come loro, ma non dà esperienza e non ha la barra in alto. Si rompe al
primo colpo, anche mancato. Rompendosi spegne il barile intero, il collider e l'ostacolo che ritaglia
il NavMesh (il passaggio si apre), accende i pezzi a terra, fa volare le schegge (`Fx_BarrelSplinters`,
cubetti di legno che rimbalzano sul pavimento) e suona una delle tre varianti (`barrel_break_0`, `_1`
e `_sa`). Il loot cade verso chi l'ha colpito, mai dentro il muro dietro: l'oggetto davanti, la
pozione e l'oro ai lati. Il cavaliere si ferma sul bordo del barile (`MeleeAttack` usa il raggio
dell'ostacolo), e il cerchio rosso a terra segna il barile sotto il cursore, 10 cm sopra la sua base:
il barile parte dal fondo della piastrella, 5 cm sotto il NavMesh dei piedi.

**Loot e semi.** `BarrelLoot`: oggetto 5%, pozione 3% di vita e 2% di mana, oro 15% tra 2 e 5. I
semi vengono da un dominio loro (`SeedMixer.ForBarrel`, e da lì pozione e oro), dalla cella del barile
in decimetri: due barili dello stesso gruppo hanno semi diversi, e il loot di nemici e casse non cambia.

**Ricordi del livello.** I barili rotti si ricordano a parte (`LevelRecord.Broken` e il loro numero):
un salvataggio di prima non ha barili, i suoi nemici e le sue casse restano validi e i barili ripartono
interi. Il formato del salvataggio non cambia.

**Modelli e prefab.** Il barile rotto è fatto in Blender (`Tools/Blender/models/barrel.py`): il fondo
con le doghe spezzate e il primo cerchio, doghe e cerchio di sopra a terra, schegge. Lo usano i tre
barili (`Barrel_Large`, `Barrel_Small`, `Barrel_Decorated`), scalato sul raggio di ognuno. Il barile
`b` della cripta e il gruppo `0` al centro della stanza si rompono; quello `b` della città resta un
arredo. Nelle composizioni del deposito i barili in piedi sono diventati botti (`keg`): `StoreA`,
`StoreC`, `StoreD`. Il barile coricato della rovina e la pila di barilotti restano: non si confondono
con quelli da rompere.

**Gruppi contro i muri.** Due composizioni nuove, `1` (un barile grande e uno piccolo) e `!` (grande,
decorato e piccolo), contro un muro o in un angolo. Arrivano dopo tutti gli arredi, con il loro flusso,
finché il livello non ha i barili tirati tra quelli delle impostazioni (`BarrelsPerLevel`): 4–8 nella
cripta, 2–5 nelle caverne, dove il `b` è roccia e contano solo i gruppi. Nemici, casse e mappa dei semi
già provati non cambiano. I gruppi sono di due e di tre e non di quattro: in una cella di 4 m quattro
barili non ci stanno senza uscire dalla cella. Con i barili singoli e quelli al centro un livello della
cripta può superare gli 8 (il seme 3 alla profondità 1 ne ha 9).

Restano fuori: gli incantesimi non rompono i barili (colpiscono solo `Health`), e il crollo
`CaveDecor_RockfallC` ha ancora un barile piccolo in piedi, mezzo tra le rocce, che non si rompe.

Test: `BarrelDataTests` (semi separati, loot raro, ricordi e salvataggi di prima, prefab, tileset,
nessun barile in piedi che non si rompe negli arredi della cripta, barili per livello in 100 cripte e
100 caverne) e `BarrelPlayModeTests` (cerchio, click, loot dal seme verso il cavaliere, una rottura
sola; barile rotto dopo il ritorno dalla città, senza loot doppio). `DecorTests` e `CaveDecorTests`
conoscono i barili da rompere. 357 EditMode e 217 PlayMode verdi; in un primo giro
`PortalTests.Portal_GoesToTownAndBack` ha sforato i 12 s di 0,2 ms, da solo e nel giro dopo è verde.

---

## Trappole note

- **Il seme.** Il colonnato tolto non deve far scorrere il flusso degli arredi (D2); barili, santuari,
  pozzi e trappole si tirano da domini loro, dopo quello che c'è già. I test che contano arredi o
  ostacoli di un seme fisso vanno guardati uno per uno.
- **Un quinto tema cambia gli arredi di tutti i semi.** La scelta del tema è un tiro tra quattro:
  con cinque, ogni stanza può cambiare tema. Gli arredi non sono nei ricordi dei livelli, quindi i
  salvataggi reggono; i test che fissano un tema vanno aggiornati.
- **Gli oggetti nuovi e i ricordi dei livelli.** Barili, pozzi e santuari nuovi cambiano quello che
  un livello contiene: i ricordi li contano come le casse, e un livello di un salvataggio di prima
  riparte da capo una volta (come nella M12, ADR-079).
- **Gli assi del FBX.** `chest_gold` arrivava in Unity girato di 270° e scalato 100 volte (M12): gli
  FBX di Blender si esportano con la trasformazione applicata, e il primo import si controlla con
  una foto prima di fare i prefab.
- **Un bersaglio che non è un nemico.** Il barile sta sul layer dei bersagli ma non ha un'IA né
  esperienza: tutto quello che oggi dà per scontato che una `Health` sul layer dei nemici sia un
  nemico (barra in alto, esperienza, automappa, conteggi dei test) va guardato.
- **Un bonus a tempo e il salvataggio.** Un bonus attivo non si salva: uscendo si perde. Va detto nel
  messaggio, o va salvato; la proposta è perderlo, come le pozioni bevute.
- **Il percorso tra le porte.** Il punto d'interesse e i barili in mezzo non lo chiudono mai; gli
  spuntoni sì, ma si attraversano a tempo, e un nemico che insegue non ne è ferito (D8).
- **Il loot e i semi.** Cambiare le probabilità cambia quali nemici lasciano qualcosa nei semi già
  provati: i test che aspettano un oggetto da un nemico preciso vanno guardati uno per uno. Il seme
  di ogni tiro non cambia, cambia solo la soglia.
- **Il volume dei suoni nuovi.** Un WAV normalizzato al picco può suonare più forte o più piano
  dei sample Kenney e Puadlung: i suoni nuovi si portano allo stesso volume medio di quelli vicini
  (come i suoni Puadlung alzati alla M11) e si giudicano in build, non solo nella pagina di ascolto.
- **Le due varianti del barile.** Se la scelta tra le due clip venisse dal seme dei livelli, il
  suono cambierebbe gli altri tiri: si sceglie con un `Random` dell'audio, fuori dai semi.
- **Le luci.** Pozzi e santuari hanno una luce colorata: con il forziere d'oro, le torce e le
  scale, una stanza può averne troppe. Si misura in build.

---

## Test

- EditMode: arredi (nessun doppione per stanza, mucchietti del tema, rotazione stabile per cella,
  nessun colonnato, punto d'interesse nelle stanze grandi e al più un pozzo e un santuario per
  livello), loot dei barili, bonus a tempo, media degli oggetti per livello nella fascia di D13.
- PlayMode: rompere un barile, usare un pozzo e un santuario, la trappola che ferisce il cavaliere,
  il barile esplosivo che ferisce i nemici, i ricordi del livello.
- Le foto dei modelli nuovi accanto ai KayKit, prima dei prefab.
- La pagina di ascolto dei suoni nuovi, prima di metterli nei prefab.

---

## Checklist di chiusura

- [x] Decisioni D1–D14 confermate (19 ott 2026); D15 (ComfyUI) e D16 (immagine di partenza) aggiunte da Mirco il 20 ott 2026
- [x] Libreria Blender e primo modello in Unity
- [x] Libreria audio e suoni della prova nel repo
- [x] Arredi con un senso, senza colonnato
- [x] Modelli nuovi e tema della cappella
- [x] ComfyUI configurato e provato: suoni generati nel gioco, TRELLIS provato sulla statua; ADR-096
- [x] Barili da rompere, anche nelle caverne, con i gruppi contro i muri
- [ ] Pozzi e santuari
- [ ] Trappole
- [ ] Loot misurato e ritarato
- [ ] Build di prova
- [ ] ADR, lezioni nel piano, README
- [ ] Tag `v1.2.0`, con il via di Mirco
