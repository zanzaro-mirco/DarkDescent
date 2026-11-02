# L'albero del Guerriero

Il disegno del passo 20.1 della M20 "La via della spada" (`M20_La_via_della_spada.md`), approvato
da Mirco il 2 novembre 2026. È la fotografia del disegno: dal passo 20.2 la fonte dell'albero è
`Editor/TalentTools.cs`, che costruisce gli asset.

> **Ritarati al passo 20.8** (la misura delle difficoltà con l'albero dentro, D14). Il disegno qui sotto
> resta com'era approvato; nei dati sono cambiati solo questi numeri: Braccio forte +2% (era 3),
> Cotta robusta +2% armatura (5), Maestro d'armi +3% e +3 a colpire (5), Cuore di ferro +2% armatura (5),
> Scudo pesante +2 punti di blocco (3), Armatura del custode +1% (6), Punto debole +5% (10), Occhio del
> boia +0,5 punti di critico (3), Maestria delle armi +3% (4).

## Stili di gioco

- **Il muro** (Difesa). Sta in mezzo allo sciame con lo scudo: blocca, stordisce con il Colpo di scudo, rimanda i colpi con la Rappresaglia, e con il Muro di scudi e l'Ultimo baluardo regge il re. Dalla classe prende Tempra, Cotta robusta, Pelle di pietra e Ultima difesa.
- **Il boia** (Armi). Lo spadone, i critici e l'Esecuzione: abbatte un nemico alla volta, i campioni e il re prima di tutti. Dalla classe prende Braccio forte, Occhio attento e Maestro d'armi.
- **Il turbine** (Armi). Turbine, Vortice, Massacro e Frenesia, fino alla Tempesta d'acciaio: svuota le stanze piene e si paga il mana colpendo. Dalla classe prende Disciplina e Sete di sangue.
- **La carica** (tutte e due). Carica, Slancio, il Grido che stordisce, la Carica furiosa: entra, ferma tutti e sceglie chi colpire. Sta nell'albero della classe, e si mette sopra il muro come sopra il boia.

## Punti

Uno a ogni livello dal 2: dal 2 al 9 nella classe, dal 10 ai pari nella specializzazione e ai dispari nella
classe. Il primo nodo di ogni albero è gratis. Un nodo si apre quando uno di quelli sopra a cui è legato ha
tutti i suoi gradi.

| Livello | Classe | Specializzazione |
|---|---|---|
| 2 | 1 | 0 |
| 5 | 4 | 0 |
| 9 | 8 | 0 |
| 10 | 8 | 1 |
| 15 | 11 | 3 |
| 20 | 13 | 6 |
| 21 | 14 | 6 |
| 30 | 18 | 11 |
| 32 | 19 | 12 |
| 40 | 23 | 16 |
| 50 | 28 | 21 |

Al livello 50: 28 punti per 36 gradi nella classe, 21 per 27 nella specializzazione (i gratis esclusi), circa il 78%.

## Stordimento

Il re e i campioni ne prendono metà; dopo uno stordimento un nemico non si stordisce di nuovo per 3 s; gli
Inarrestabili (M12) non si stordiscono mai.

## Barra e altare

Con *Armi* le abilità attive possono essere otto: la barra ha **otto posti, F1–F8**. All'altare si riparte da
zero per **30 oro × livello**.

## Nodi

### Guerriero — L'albero della classe

20 nodi, 37 gradi. Le righe si aprono così: dalla riga 4, dopo 6 punti; dalla riga 7, dopo 14 punti.

| # | Nodo | Tipo | Gradi | Riga, colonna | Si apre da | Effetto | Perché |
|---|---|---|---|---|---|---|---|
| G01 | **Colpo possente** | attivo | 1 | 1, 4 | — | Un colpo al 160% del danno dell'arma. 4 mana, ricarica 3 s. **Preso gratis al livello 1.** | Il tasto destro fa qualcosa fin dal primo minuto, al posto del Dardo di fuoco. |
| G02 | **Braccio forte** | passivo | 3 | 2, 2 | G01 | +3% danno per grado. | Il primo passo del ramo offensivo, buono per tutti. |
| G03 | **Carica** | attivo | 1 | 2, 4 | G01 | Corre sul nemico sotto il cursore, da 3 a 10 m, lo colpisce al 100% e lo stordisce per 1 s. 10 mana, ricarica 8 s. | L'abilità del Guerriero: entra nella mischia senza prendere i colpi della strada. |
| G04 | **Tempra** | passivo | 3 | 2, 6 | G01 | +4% vita massima per grado. | Il primo passo del ramo difensivo. |
| G05 | **Occhio attento** | passivo | 3 | 3, 1 | G02 | +2 punti di critico per grado. | Per chi punta sui critici, il boia. |
| G06 | **Mano ferma** | passivo | 2 | 3, 3 | G02 | +5 a colpire per grado. | Contro i nemici con tanta armatura delle difficoltà alte. |
| G07 | **Slancio** | passivo | 2 | 3, 4 | G03 | Carica: −2 s di ricarica per grado. | Due gradi portano la Carica a 4 s: si entra e si esce. |
| G08 | **Pelle di pietra** | attivo | 1 | 3, 5 | G04 | +50% armatura per 20 s. 12 mana, ricarica 30 s. È l'incantesimo di oggi, che resta al Guerriero. | La difesa a comando che il gioco ha già. |
| G09 | **Cotta robusta** | passivo | 3 | 3, 7 | G04 | +5% armatura per grado. | Armatura sempre accesa, per chi non vuole premere tasti. |
| G10 | **Disciplina** | passivo | 2 | 4, 2 | G05, G06 | Ogni colpo a segno ridà 1 mana per grado. | Il mana del Guerriero viene dal combattere, come la Furia di WoW, senza una risorsa nuova. |
| G11 | **Grido di battaglia** | attivo | 1 | 4, 4 | G06, G07, G08 | +15% danno per 12 s. 12 mana, ricarica 30 s. | Il momento in cui si sceglie di spingere: prima del re, prima di un campione. |
| G12 | **Seconda pelle** | passivo | 2 | 4, 6 | G08, G09 | Pelle di pietra: +5 s di durata e −2 mana per grado. | Con due gradi dura 30 s su 30 di ricarica: sempre accesa, se si sta attenti. |
| G13 | **Sete di sangue · Ritmo di battaglia** | a scelta | 1 | 5, 2 | G10 | **Sete di sangue:** +3% rubavita. **Ritmo di battaglia:** +8% velocità d'attacco. | Si cura colpendo, o si colpisce più spesso. |
| G14 | **Grido che scuote** | passivo | 2 | 5, 4 | G11 | Il Grido stordisce i nemici entro 5 m per 1 s per grado. | Lega il Grido alla difesa: un respiro in mezzo allo sciame. |
| G15 | **Ultima difesa** | passivo | 1 | 5, 6 | G12 | Sotto il 30% della vita, −30% danno preso. | Il margine per arrivare alla pozione. |
| G16 | **Passo pesante** | passivo | 2 | 6, 3 | G13, G14 | +5% velocità di movimento per grado. | Si attraversano prima i livelli già ripuliti, e si scappa meglio. |
| G17 | **Recupero** | passivo | 2 | 6, 5 | G14, G15 | Le pozioni curano il 15% in più per grado. | La cintura conta di più per chi resta in mezzo. |
| G18 | **Maestro d'armi** | passivo | 2 | 7, 2 | G16 | +5% danno e +5 a colpire per grado. | Il traguardo dell'attacco. |
| G19 | **Carica furiosa · Grido dei padri** | a scelta | 1 | 7, 4 | G16, G17 | **Carica furiosa:** la Carica colpisce tutti i nemici entro 3 m dall'arrivo. **Grido dei padri:** il Grido cura il 15% della vita. | Il traguardo della carica: in mezzo a fare danno, o in mezzo a resistere. |
| G20 | **Cuore di ferro** | passivo | 2 | 7, 6 | G17 | +5% vita e +5% armatura per grado. | Il traguardo della difesa. |

### Difesa — Specializzazione, con lo scudo

15 nodi, 28 gradi. Le righe si aprono così: dalla riga 4, dopo 5 punti; dalla riga 6, dopo 12 punti.

| # | Nodo | Tipo | Gradi | Riga, colonna | Si apre da | Effetto | Perché |
|---|---|---|---|---|---|---|---|
| D01 | **Colpo di scudo** | attivo | 1 | 1, 4 | — | Con lo scudo: un colpo al 100% che stordisce per 1,5 s. 8 mana, ricarica 6 s. **Preso gratis scegliendo Difesa.** | Il segno della specializzazione: lo scudo serve anche ad attaccare. |
| D02 | **Scudo pesante** | passivo | 3 | 2, 2 | D01 | +3 punti di blocco per grado (il tetto resta 75%). | Più blocchi vuol dire più Contraccolpo e più Rappresaglia, più in basso. |
| D03 | **Muro di scudi** | attivo | 1 | 2, 4 | D01 | Con lo scudo: per 5 s blocca tutti i colpi che si possono bloccare, ma si cammina a metà velocità. 15 mana, ricarica 40 s. | Il bottone del pericolo: il re, un campione Furioso, una trappola di nemici. |
| D04 | **Pelle spessa** | passivo | 3 | 2, 6 | D01 | −1 danno per ogni colpo preso, per grado. | Contro lo sciame di colpi piccoli. |
| D05 | **Contraccolpo** | passivo | 2 | 3, 1 | D02 | Ogni colpo bloccato ridà 2 mana per grado. | Il mana della Difesa viene dai blocchi. |
| D06 | **Spalla d'urto** | passivo | 2 | 3, 3 | D02, D03 | Colpo di scudo: −2 s di ricarica per grado. | Con due gradi si colpisce ogni 2 s; lo stordimento segue la sua regola, quindi il re non resta fermo. |
| D07 | **Bastione** | passivo | 2 | 3, 5 | D03, D04 | Muro di scudi: +1,5 s di durata per grado. | Per chi conta sul Muro contro il re. |
| D08 | **Armatura del custode** | passivo | 3 | 3, 7 | D04 | Con lo scudo, +6% armatura per grado. | Lo scudo in mano vale di più. |
| D09 | **Rappresaglia** | passivo | 2 | 4, 2 | D05, D06 | Un colpo bloccato rimanda al nemico il 25% del suo danno per grado. | La Difesa fa danno anche stando ferma. |
| D10 | **Scudo tonante · Scudo che acceca** | a scelta | 1 | 4, 4 | D06, D07 | **Scudo tonante:** il Colpo di scudo colpisce anche i nemici entro 2,5 m. **Scudo che acceca:** il Colpo di scudo stordisce 1,5 s in più. | Tanti nemici fermi poco, o uno fermo a lungo. |
| D11 | **Saldo** | passivo | 2 | 4, 6 | D07, D08 | +5% vita massima per grado. | Più vita per arrivare in fondo all'albero. |
| D12 | **Vendicatore** | passivo | 2 | 5, 3 | D09, D10 | Dopo un blocco, il colpo successivo fa +15% danno per grado. | Blocca, poi rispondi: il ritmo della Difesa. |
| D13 | **Riparo** | passivo | 2 | 5, 5 | D10, D11 | −4% danno preso per grado. | Vale contro tutto, anche contro i colpi che non si bloccano. |
| D14 | **Ultimo baluardo** | attivo | 1 | 6, 4 | D12, D13 | Per 10 s +30% vita massima, e cura quanto ne aggiunge. 20 mana, ricarica 90 s. | Il traguardo: quando la pozione non basta. |
| D15 | **Fortezza** | passivo | 1 | 6, 6 | D13 | Con lo scudo, i colpi imparabili (il modificatore della M12) che lo scudo avrebbe fermato fanno metà danno. | La risposta della Difesa ai campioni che passano lo scudo. |

### Armi — Specializzazione, con le armi a due mani

15 nodi, 28 gradi. Le righe si aprono così: dalla riga 4, dopo 5 punti; dalla riga 6, dopo 12 punti.

| # | Nodo | Tipo | Gradi | Riga, colonna | Si apre da | Effetto | Perché |
|---|---|---|---|---|---|---|---|
| A01 | **Turbine** | attivo | 1 | 1, 4 | — | Colpisce tutti i nemici entro 3 m al 90% del danno dell'arma; 3,5 m con un'arma a due mani. 12 mana, ricarica 4 s. **Preso gratis scegliendo Armi.** | Il segno della specializzazione: l'arma grande contro la folla. |
| A02 | **Presa a due mani** | passivo | 3 | 2, 2 | A01 | Con un'arma a due mani, +5% danno per grado. | La ragione per prendere lo spadone. |
| A03 | **Filo affilato** | passivo | 2 | 2, 4 | A01 | Turbine: +15% danno per grado. | Per chi gioca di Turbine. |
| A04 | **Punto debole** | passivo | 3 | 2, 6 | A01 | I critici fanno il 10% di danno in più per grado (da ×2 a ×2,3). | I critici del boia pesano di più. |
| A05 | **Movimento circolare** | passivo | 2 | 3, 1 | A02 | +5% velocità d'attacco per grado (il tetto resta 50%). | Lo spadone è lento: qui si recupera. |
| A06 | **Esecuzione** | attivo | 1 | 3, 3 | A02, A03 | Un colpo al 250% contro un nemico sotto il 25% della vita, al 100% sugli altri. 10 mana, ricarica 6 s. | Il colpo che chiude: lo stile del boia. |
| A07 | **Vortice** | passivo | 2 | 3, 5 | A03, A04 | Turbine: +0,5 m di raggio per grado. | Con due gradi e lo spadone, 4,5 m: mezza stanza. |
| A08 | **Occhio del boia** | passivo | 2 | 3, 7 | A04 | +3 punti di critico per grado. | Con Occhio attento della classe, oltre +10 punti. |
| A10 | **Massacro** | passivo | 2 | 4, 2 | A05, A06 | Uccidere un nemico ridà 3 mana per grado. | In mezzo alla folla il Turbine si paga da solo. |
| A09 | **Salto** | attivo | 1 | 4, 4 | A06, A07 | Salta fino a 8 m sul punto indicato, colpisce al 120% i nemici entro 3 m e li stordisce 1 s. 14 mana, ricarica 12 s. | La Carica delle Armi: si atterra in mezzo. |
| A11 | **Sangue sulla lama** | passivo | 2 | 4, 6 | A07, A08 | +2% rubavita per grado (il tetto resta 10%). | Senza scudo la vita torna colpendo. |
| A12 | **Boia · Turbine continuo** | a scelta | 1 | 5, 3 | A09, A10 | **Boia:** l'Esecuzione vale sotto il 35% della vita. **Turbine continuo:** il Turbine costa 4 mana in meno. | Si sceglie lo stile: uno alla volta, o tutti insieme. |
| A13 | **Frenesia** | passivo | 2 | 5, 5 | A09, A11 | Dopo un'uccisione, +5% velocità d'attacco per 5 s, per grado. | Una stanza piena si svuota sempre più in fretta. |
| A15 | **Maestria delle armi** | passivo | 3 | 6, 2 | A12 | +4% danno per grado. | Il traguardo del boia. |
| A14 | **Tempesta d'acciaio** | attivo | 1 | 6, 4 | A12, A13 | Per 3 s gira su sé stesso camminando al 70%: ogni 0,5 s colpisce al 60% tutti i nemici entro 3,5 m. 25 mana, ricarica 45 s. | Il traguardo: il Turbine che non si ferma. |
