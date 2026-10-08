# M8 — "Progressione e persistenza"

**Cosa deve succedere (Definition of Done):** in una **build prodotta dalla CI**, uccidendo
nemici il cavaliere guadagna **esperienza**, sale di **livello** e distribuisce i **punti
attributo** nel pannello del personaggio. Trova e indossa un **elmo**, un'**armatura**,
**guanti**, **stivali**, due **anelli** e un **amuleto**; ognuno cambia le statistiche e
compare nel pannello. **Chiude il gioco, lo riapre, e si ritrova dov'era**: stesso livello,
stessa profondità, stesso equipaggiamento, inventario, cintura, esperienza e mappa scoperta.
Un salvataggio del **formato 1** si carica con il codice del **formato 2**. La visuale si
**avvicina e si allontana** con la rotella e **gira a scatti** attorno al cavaliere, con i
muri bassi sempre dalla parte della camera. Test verdi in CI.

**Tempo stimato:** 12–18 h (piano v2.19). **Prerequisito:** M7 chiusa (tag `m7`).

**Come si lavora:** come alla M7, il codice e i passaggi nell'editor li faccio io, in
batchmode a Unity chiuso. A Mirco restano le decisioni qui sotto, le prove in build e la
revisione degli ADR.

**Punto di controllo:** alla chiusura di questa milestone (piano § 1.3).

---

## Stato verificato l'8 ottobre 2026

| Cosa | Stato |
|---|---|
| Attributi | `CharacterStats` con Forza 30, Destrezza 20, Magia 10, Vitalità 25 sul cavaliere; vita da Vitalità (`CombatFormulas.MaxLife`). Nessuna esperienza né livello |
| Equipaggiamento | `EquipSlot` ha solo `Weapon` e `Offhand`. `Equipment` applica i modificatori degli affissi allo `StatSheet`; lo scudo dà Armatura e blocco (`ArmorDefinition`) |
| Oggetti | Otto basi (quattro armi, quattro scudi) e la pozione. Icone renderizzate dai modelli KayKit (`Art/Icons`). Gli oggetti a terra mostrano il modello |
| Modelli disponibili | Nel `Knight.fbx` ci sono le mesh `Knight_Helmet` e `Knight_HelmetVisor`; gli altri personaggi di *Adventurers* hanno cappelli e cappucci (`Rogue_Hooded`). Per armature, guanti, stivali, anelli e amuleti nei pacchetti scaricati **non c'è niente**. Il *Dungeon Pack* ha monete e chiavi |
| Persistenza | `InventorySnapshot` (M6) cattura e ripristina griglia, equipaggiamento, cintura e oggetto sul cursore con `JsonUtility`. Nessun file su disco. `PlayerPrefs` tiene solo la lingua |
| Livelli | Generati dal seme della partita e dalla profondità (`SeedMixer.ForLevel`, ADR-034). Alla morte il livello resta com'è (ADR-048); cambiando livello quello vecchio si scarica e, tornando, si rigenera |
| Esplorazione | `Exploration` per livello, in `Core`; si perde cambiando livello |
| Camera | Cinemachine con angolo fisso; muri bassi sui lati verso la camera (ADR-017) |
| Test | 181 EditMode e 137 PlayMode verdi |

---

## Decisioni

Da confermare. Per ognuna c'è una proposta.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Curva dell'esperienza** | Livello massimo **20** nella v1.0. Esperienza per passare dal livello L al successivo: **100 × L^1,6**, arrotondata a 10 (100 dal 1 al 2, circa 3.360 dal 9 al 10, 11.120 dal 19 al 20). Si arriva verso il 10 in fondo alle caverne | Otto profondità non bastano per i 50 livelli di Diablo: con 20 si sale spesso all'inizio e meno dopo. La formula sta in `Core`, i numeri in un asset |
| D2 | **Esperienza dei nemici** | Un valore per archetipo: scheletro **12**, sciame **5**, bruto **40**, moltiplicato per **1 + 0,15 × (profondità − 1)**. Si divide per **1 + 0,1 × (livello del cavaliere − livello della zona)** quando il cavaliere è più forte della zona, come in Diablo, per non salire uccidendo sciami del livello 1 | Dà circa un livello per profondità nella cripta e uno ogni due nelle caverne. I numeri si tarano provando |
| D3 | **Salire di livello** | **5 punti attributo** a livello, da spendere con un **+** accanto a ogni attributo del pannello. **+2 di vita** a livello oltre alla Vitalità. La vita si **riempie** salendo. Un suono e una luce dorata attorno al cavaliere | 5 punti come in Diablo 1. Riempire la vita premia il momento; senza, salire di livello in un combattimento non si sente |
| D4 | **Slot nuovi** | **Elmo, armatura, guanti, stivali, due anelli, amuleto**, accanto ad arma e scudo (piano v2.19). Il pannello dell'inventario diventa una sagoma come in Diablo: elmo in alto, amuleto a destra dell'elmo, armatura al centro, anelli sotto le mani, guanti e stivali in basso | Già deciso nel piano; qui si decide solo la disposizione |
| D5 | **Cosa danno** | Elmo, armatura, guanti e stivali: **Armatura** (come lo scudo, con la Forza richiesta per l'armatura). Anelli e amuleto: niente di base, solo **affissi**. Basi nuove: 2 elmi, 3 armature, 2 guanti, 2 stivali, 1 anello, 1 amuleto. Affissi nuovi ammessi sui gioielli: vita, i quattro attributi, colpire, probabilità di critico | Pochi oggetti, ma ogni slot ha già un motivo di cambio. Un affisso nuovo sposta il loot dei semi provati (trappola 10 della M7): i test con i drop fissi si aggiornano |
| D6 | **Modelli e icone dei pezzi nuovi** | Elmi dalle mesh dei personaggi KayKit (`Knight_Helmet`, il cappuccio di `Rogue_Hooded`). Per armature, guanti, stivali, anelli e amuleti **cerco un pacchetto CC0** di oggetti 3D e te lo propongo con nome, fonte e dimensione prima di scaricarlo. Se non c'è niente di adatto: icone piatte disegnate da me in SVG e, a terra, un sacchetto. L'armatura indossata **non cambia** il modello del cavaliere (resta alla M11) | Nei pacchetti scaricati quei modelli non ci sono. Le icone renderizzate dai modelli, come oggi, restano coerenti con il resto |
| D7 | **Dove e quando si salva** | Un **file JSON** in `Application.persistentDataPath/save.json`, scritto in un file temporaneo e poi rinominato, così un crash a metà non rovina il salvataggio. Si salva **entrando in un livello**, **ogni 60 s** fuori dal combattimento e **chiudendo il gioco**. Un solo salvataggio | Un salvataggio solo finché non c'è il menu della M10. La scrittura con il rinomina è la difesa standard contro i file a metà |
| D8 | **Cosa si salva** | Seme della partita, profondità e ingresso, livello, esperienza, attributi e punti da spendere, vita, inventario completo (`InventorySnapshot`), e la **mappa scoperta di ogni profondità visitata**. **Non** si salva lo stato dei livelli: nemici uccisi, casse aperte e oggetti a terra tornano come li genera il seme | Lo stato dei livelli vorrebbe un'identità stabile per ogni nemico, cassa e oggetto, e un formato molto più grande. Diablo 1 lo salva, ma qui un livello si attraversa in pochi minuti. Oggi cambiare livello lo rigenera già |
| D9 | **Formato versionato e migrazione** | Il file ha un campo `version`. Il **formato 1** salva tutto tranne la mappa scoperta; il **formato 2** aggiunge la mappa. Un `SaveMigrator` in `Core` porta un formato 1 al 2 (mappa vuota) prima di leggerlo. Un salvataggio di una versione più nuova del gioco non si carica e lo dice | La migrazione si prova davvero solo con due formati; questo è un cambio reale e piccolo. Il test è nel piano |
| D10 | **Avvio senza menu** | All'avvio, se c'è un salvataggio, si **riprende da lì**; altrimenti partita nuova. `-newgame` da riga di comando ignora il salvataggio, e un nuovo salvataggio lo sostituisce. Il menu con *Continua* e *Nuova partita* arriva con la M10 | Senza menu non c'è dove chiederlo; la riga di comando c'è già per `-seed` e `-lang` |
| D11 | **Zoom** | Rotella tra il **60% e il 140%** della distanza di oggi, smorzato in 0,15 s. Si salva tra le preferenze, come la lingua | Proposta del piano v2.18 |
| D12 | **Rotazione** | A **scatti di 90°** con **Q** ed **E**, animata in 0,4 s. Il builder mette su ogni lato del livello sia il muro alto sia quello basso; a metà rotazione si accende quello giusto per il nuovo lato verso la camera. Torce, stendardi e la parte sepolta della scala seguono; l'automappa gira con la camera | Proposta del piano v2.18. A scatti perché i muri bassi (ADR-017) stanno sui lati verso la camera; la rotazione libera vorrebbe uno shader che dissolve i muri, lasciato alla M11 |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 8.1 | Esperienza e livelli in `Core`: curva, esperienza dei nemici, punti attributo (D1–D3), con i test | 1,5–2 |
| 8.2 | Nel gioco: esperienza alla morte dei nemici, barra dell'esperienza nell'HUD, salita di livello con suono e luce, punti da spendere nel pannello | 1,5–2 |
| 8.3 | Slot nuovi: `EquipSlot`, `Equipment`, armatura dai pezzi, pannello a sagoma (D4, D5) | 1,5–2 |
| 8.4 | Basi nuove, affissi dei gioielli, modelli e icone (D5, D6), loot dei nemici e delle casse | 2–3 |
| 8.5 | Salvataggio formato 1: `SaveData`, scrittura sicura, caricamento all'avvio, `-newgame` (D7, D8, D10) | 2–3 |
| 8.6 | Formato 2 con la mappa scoperta e migrazione (D9) | 1 |
| 8.7 | Zoom (D11) | 0,5–1 |
| 8.8 | Rotazione a scatti e muri che seguono la camera (D12) | 2–3 |
| 8.9 | Chiusura: build da provare, GIF, ADR, lezioni, tag `m8` | 0,5 |

---

## Trappole note

1. **Un affisso nuovo cambia il loot dei semi provati** (trappola 10 della M7): i gioielli
   aggiungono affissi e basi, e i test che fissano un drop vanno aggiornati insieme.
2. **`JsonUtility` non serializza dizionari né riferimenti ad asset:** gli oggetti si salvano
   con l'id della definizione e degli affissi, come fa già `InventorySnapshot`. La mappa
   scoperta va in una lista di profondità con un array di bit per livello.
3. **Il salvataggio all'uscita non sempre arriva:** `OnApplicationQuit` in build sì, un crash
   no. Per questo si salva anche entrando nei livelli e ogni minuto.
4. **Caricare non è come entrare in un livello:** il cavaliere va messo sull'ingresso salvato
   *dopo* che il livello è generato, e i modificatori degli oggetti indossati vanno applicati
   prima di riempire la vita, o la vita massima sarebbe quella senza oggetti.
5. **Due anelli nello stesso tipo di slot:** `Equipment` oggi ha uno slot per tipo. Gli anelli
   vogliono due posti con la stessa regola: o due valori dell'enum, o uno slot con un indice.
6. **I muri doppi raddoppiano i collider:** il muro spento deve spegnere anche il suo collider,
   o il NavMesh, cotto dai collider (ADR-035), cambierebbe con la camera. Si cuoce con i muri
   alti, che stanno sempre sulla griglia.
7. **La rotazione cambia la direzione dei click:** il raggio dal cursore usa la camera, quindi
   segue da solo; ma le scorciatoie che assumono la camera a nord-est (la luce di riempimento
   del cavaliere, l'automappa) vanno riguardate.

---

## Test

| Test | Cosa controlla |
|---|---|
| `ExperienceTests` (EditMode) | Curva, esperienza per nemico e profondità, riduzione per livello alto, punti a livello |
| `SaveDataTests` (EditMode) | Salvare e ricaricare restituisce uno stato identico; formato 1 migrato al 2; versione più nuova rifiutata; file a metà ignorato |
| `EquipmentTests` (EditMode, ampliato) | Gli slot nuovi, i due anelli, l'Armatura dei pezzi |
| `LevelUpTests` (PlayMode) | Uccidere dà esperienza, si sale, la vita si riempie, i punti si spendono |
| `SaveLoadTests` (PlayMode) | Dopo un caricamento il cavaliere è sulla profondità e sull'ingresso salvati, con tutto addosso |
| `CameraRotationTests` (PlayMode) | Zoom nei limiti; dopo ogni scatto, tra la camera e il cavaliere nessun muro alto |

---

## Checklist di chiusura

- [ ] Decisioni D1–D12 confermate
- [ ] Esperienza e livelli
- [ ] Equipaggiamento completo
- [ ] Salvataggio e migrazione
- [ ] Zoom e rotazione
- [ ] Scenario della Definition of Done provato in build
- [ ] Test verdi in CI
- [ ] GIF, ADR, lezioni nel piano, tag `m8`
- [ ] Punto di controllo (piano § 1.3)
- [ ] Scheda della M9 scritta prima di cominciarla
