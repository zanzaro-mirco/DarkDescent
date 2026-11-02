# L'albero del Mago

Il disegno del passo 21.1 della M21 "La via dell'arcano" (`M21_La_via_dell_arcano.md`). È la fotografia
del disegno: dal passo 21.5 la fonte dell'albero è `Editor/TalentTools.cs`, che costruisce gli asset.

## Il Mago in breve

- Il tasto sinistro è il **Dardo arcano**: dalla bacchetta o dal bastone, a distanza, senza mana e senza ricarica.
- Il mana si rigenera piano (1% al secondo, il doppio fuori dal combattimento); il Guerriero resta com'è.
- Poca vita e poca armatura, tanto mana; i punti a livello vanno Magia, Vitalità, Magia, Vitalità, Destrezza.
- Bacchetta (una mano) col tomo, o bastone (due mani); chiedono Magia.

## Stili di gioco

- **Il rogo** (Fuoco). Il Dardo di fuoco, la Palla, il Muro di fiamme e la Meteora: tutto il danno del Mago, e il mana che torna dai morti. Dalla classe prende Mente acuta, Dardo rapido, Fiamma ampia e Maestro del fuoco.
- **Il gelo** (Gelo). Rallenta, congela e tiene lontano lo sciame: il Dardo di ghiaccio, la Nova, la Tempesta di neve, con l'Armatura di ghiaccio per chi arriva lo stesso. Dalla classe prende il Passo di luce e lo Scudo di mana.
- **Lo scudo** (tutte e due). Più mana e più vita per un Mago che ne ha poca: Riserva di mana, Pelle di cenere, Scudo di mana e Cuore arcano. Sta nell'albero della classe, e si mette sopra il fuoco come sopra il gelo.
- **Il passo** (tutte e due). Passo di luce, Mente fredda e Concentrazione: il Mago che si sposta e ricarica il mana senza fermarsi. Sta nell'albero della classe.

## Punti

Come per il Guerriero: uno a ogni livello dal 2, dal 2 al 9 nella classe, dal 10 ai pari nella specializzazione e ai
dispari nella classe. Il primo nodo di ogni albero è gratis. Un nodo si apre quando uno di quelli sopra a cui è
legato ha tutti i suoi gradi.

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

Al livello 50: 28 punti per 33 gradi nella classe, 21 per 28 in *Fuoco* e 21 per 26 in *Gelo* (i gratis esclusi).

## Congelamento e rallentamento

Per il congelamento vale la regola dello stordimento (M20): il re e i campioni ne prendono metà; dopo un congelamento un
nemico non si congela di nuovo per 3 s; gli Inarrestabili non si congelano mai. Il rallentamento (−40%, per il re e i
campioni −20%) non ha tempo di pausa.

## Nodi

### Mago — L'albero della classe

19 nodi, 34 gradi. Le righe si aprono così: dalla riga 4, dopo 6 punti; dalla riga 7, dopo 14 punti.

| # | Nodo | Tipo | Gradi | Riga, colonna | Si apre da | Effetto | Perché |
|---|---|---|---|---|---|---|---|
| M01 | **Dardo di fuoco** | attivo | 1 | 1, 4 | — | Un dardo che brucia: 160% del danno del Dardo arcano, più la Magia. 6 mana, ricarica 2 s. **Preso gratis al livello 1.** | Il tasto destro fa qualcosa fin dal primo minuto: l'incantesimo che il gioco ha già. |
| M02 | **Mente acuta** | passivo | 3 | 2, 2 | M01 | +3% danno degli incantesimi per grado. | Il primo passo del ramo del danno, buono per tutti. |
| M03 | **Anello di fuoco** | attivo | 1 | 2, 4 | M01 | Un anello di fiamme attorno a chi lancia: colpisce i nemici entro 3 m al 120% del danno del Dardo di fuoco. 12 mana, ricarica 8 s. | L'altro incantesimo di oggi: la difesa quando lo sciame arriva. |
| M04 | **Riserva di mana** | passivo | 3 | 2, 6 | M01 | +5% mana massimo per grado. | Il primo passo del ramo della difesa: più mana, più scudo. |
| M05 | **Dardo rapido** | passivo | 2 | 3, 1 | M02 | Dardo di fuoco: −0,5 s di ricarica per grado. | Con due gradi il Dardo di fuoco parte ogni secondo. |
| M06 | **Fiamma ampia** | passivo | 2 | 3, 3 | M02, M03 | Anello di fuoco: +0,5 m di raggio per grado. | Lo sciame si prende più lontano. |
| M07 | **Passo di luce** | attivo | 1 | 3, 5 | M03, M04 | Si sposta subito al punto indicato, fino a 8 m, passando sopra i nemici. 8 mana, ricarica 12 s. | Il Mago non deve stare dove i nemici lo raggiungono: esce dalla mischia. |
| M08 | **Pelle di cenere** | passivo | 3 | 3, 7 | M04 | +4% vita massima per grado. | Il Mago ha poca vita: questi tre gradi bastano a non morire al primo colpo. |
| M09 | **Concentrazione** | passivo | 2 | 4, 2 | M05, M06 | Ogni Dardo arcano a segno ridà 1 mana per grado. | Il mana del Mago viene anche dal combattere, come la Disciplina del Guerriero. |
| M10 | **Scudo di mana** | attivo | 1 | 4, 4 | M06, M07 | Per 12 s il danno preso scala il mana (2 mana per ogni punto) invece della vita, finché ce n'è. 10 mana, ricarica 40 s. | La difesa a comando: il mana è la seconda vita del Mago. |
| M11 | **Mente fredda** | passivo | 2 | 4, 6 | M07, M08 | La rigenerazione del mana è del 50% più veloce per grado. | Chi lancia senza fermarsi ha bisogno di mana che torna. |
| M12 | **Scintilla · Fiammata** | a scelta | 1 | 5, 2 | M09 | **Scintilla:** ogni Dardo arcano a segno ha il 10% di lanciare un Dardo di fuoco gratis. **Fiammata:** il Dardo di fuoco brucia il nemico per 3 s (il 60% del suo danno nel tempo). | Si sceglie lo stile: tante scintille, o un fuoco che resta. |
| M13 | **Scudo duraturo** | passivo | 2 | 5, 4 | M10 | Scudo di mana: +3 s di durata per grado. | Con due gradi dura 18 s su 40 di ricarica: lo scudo di tutti gli scontri grossi. |
| M14 | **Ultima scintilla** | passivo | 1 | 5, 6 | M11 | Sotto il 30% della vita, −25% danno preso. | Il margine per arrivare alla pozione. |
| M15 | **Passo veloce** | passivo | 2 | 6, 3 | M12, M13 | Passo di luce: −3 s di ricarica per grado. | Con due gradi il Passo di luce è ogni 6 s: si salta dentro e fuori dalle stanze piene. |
| M16 | **Recupero** | passivo | 2 | 6, 5 | M13, M14 | Le pozioni curano il 15% in più per grado. | La cintura conta di più per chi sta in mezzo. |
| M17 | **Maestro del fuoco** | passivo | 2 | 7, 2 | M15 | +4% danno degli incantesimi per grado. | Il traguardo dell'attacco. |
| M18 | **Pioggia di scintille · Anello infuocato** | a scelta | 1 | 7, 4 | M15, M16 | **Pioggia di scintille:** il Dardo di fuoco colpisce anche i nemici entro 2 m dall'impatto. **Anello infuocato:** l'Anello di fuoco brucia i nemici per 3 s (il 60% del suo danno nel tempo). | Il traguardo del fuoco: più nemici colpiti insieme, o più danno nel tempo. |
| M19 | **Cuore arcano** | passivo | 2 | 7, 6 | M16 | +5% mana massimo e +3% vita massima per grado. | Il traguardo della difesa. |

### Fuoco — Specializzazione, il danno e le aree

15 nodi, 29 gradi. Le righe si aprono così: dalla riga 4, dopo 5 punti; dalla riga 6, dopo 12 punti.

| # | Nodo | Tipo | Gradi | Riga, colonna | Si apre da | Effetto | Perché |
|---|---|---|---|---|---|---|---|
| F01 | **Palla di fuoco** | attivo | 1 | 1, 4 | — | Una palla lenta che esplode entro 3 m dall'impatto: 200% del danno del Dardo di fuoco. 14 mana, ricarica 6 s. **Preso gratis scegliendo Fuoco.** | Il segno della specializzazione: il fuoco che prende tutta la stanza. |
| F02 | **Mente infuocata** | passivo | 3 | 2, 2 | F01 | +4% danno degli incantesimi per grado. | Più danno, per chi vuole fare più danno. |
| F03 | **Muro di fiamme** | attivo | 1 | 2, 4 | F01 | Un muro di fiamme di 5 m che resta 6 s: chi lo attraversa brucia (il 40% del Dardo di fuoco ogni 0,5 s). 16 mana, ricarica 20 s. | Il bottone del corridoio: ferma lo sciame, tiene fuori il re. |
| F04 | **Vasto incendio** | passivo | 3 | 2, 6 | F01 | Palla di fuoco: +0,5 m di raggio per grado. | Tre gradi fanno 4,5 m: una stanza intera. |
| F05 | **Esplosione** | passivo | 2 | 3, 1 | F02 | Palla di fuoco: +15% danno per grado. | Il fuoco grosso fa più male. |
| F06 | **Mano rapida** | passivo | 2 | 3, 3 | F02, F03 | Palla di fuoco: −1 s di ricarica per grado. | Con due gradi una palla ogni 4 s. |
| F07 | **Fiamme durature** | passivo | 2 | 3, 5 | F03, F04 | Muro di fiamme: +2 s di durata per grado. | Con due gradi dura 10 s: tutto il tempo di un corridoio. |
| F08 | **Magia ardente** | passivo | 2 | 3, 7 | F04 | +3 Magia per grado. | Più mana e più danno di ogni incantesimo, senza toccare i punti attributo. |
| F09 | **Pira** | passivo | 2 | 4, 2 | F05, F06 | Ogni uccisione ridà 3 mana per grado. | Il mana del Fuoco viene dai morti: una stanza piena paga da sola. |
| F10 | **Brace viva** | passivo | 2 | 4, 4 | F06, F07 | Muro di fiamme: +20% danno per grado. | Il muro che brucia davvero. |
| F11 | **Ardore** | passivo | 2 | 4, 6 | F07, F08 | Dopo un'uccisione, +5% velocità d'attacco per 5 s, per grado. | Una stanza piena si svuota sempre più in fretta. |
| F12 | **Meteora doppia · Marea di fuoco** | a scelta | 1 | 5, 3 | F09, F10 | **Meteora doppia:** la Meteora cade due volte, a 1 s di distanza. **Marea di fuoco:** la Palla di fuoco lascia a terra fiamme che bruciano per 4 s. | Si sceglie come finire: un colpo grosso due volte, o un terreno che brucia. |
| F13 | **Colpo ardente** | passivo | 2 | 5, 5 | F10, F11 | Dardo di fuoco: +15% danno per grado. | Anche l'incantesimo piccolo cresce. |
| F15 | **Maestria del fuoco** | passivo | 3 | 6, 2 | F12 | +3% danno degli incantesimi per grado. | Il traguardo del danno. |
| F14 | **Meteora** | attivo | 1 | 6, 4 | F12, F13 | Dopo 1,2 s una meteora cade sul punto indicato: 300% del danno della Palla di fuoco entro 4 m. 30 mana, ricarica 45 s. | Il traguardo: il colpo che decide lo scontro con il re. |

### Gelo — Specializzazione, il controllo e la difesa

15 nodi, 27 gradi. Le righe si aprono così: dalla riga 4, dopo 5 punti; dalla riga 6, dopo 12 punti.

| # | Nodo | Tipo | Gradi | Riga, colonna | Si apre da | Effetto | Perché |
|---|---|---|---|---|---|---|---|
| I01 | **Dardo di ghiaccio** | attivo | 1 | 1, 4 | — | Un dardo gelato: 100% del danno del Dardo di fuoco, e rallenta il nemico del 40% per 3 s. 8 mana, ricarica 3 s. **Preso gratis scegliendo Gelo.** | Il segno della specializzazione: il Mago che non si fa raggiungere. |
| I02 | **Pelle gelata** | passivo | 3 | 2, 2 | I01 | +2% armatura per grado. | Meno colpi che arrivano a un Mago con poca vita. |
| I03 | **Nova di gelo** | attivo | 1 | 2, 4 | I01 | Un'onda di gelo attorno a chi lancia: congela per 1,5 s i nemici entro 3,5 m. 12 mana, ricarica 14 s. | Il bottone del pericolo: lo sciame è addosso, si ferma tutto. |
| I04 | **Freddo penetrante** | passivo | 3 | 2, 6 | I01 | Dardo di ghiaccio: +1 s di rallentamento per grado. | Tre gradi fanno 6 s: un nemico che non arriva più. |
| I05 | **Schegge** | passivo | 2 | 3, 1 | I02 | Dardo di ghiaccio: +15% danno per grado. | Il ghiaccio fa anche danno. |
| I06 | **Gelo veloce** | passivo | 2 | 3, 3 | I02, I03 | Nova di gelo: −2 s di ricarica per grado. | Con due gradi una nova ogni 10 s: lo stordimento segue la sua regola, quindi il re non resta fermo. |
| I07 | **Gelo profondo** | passivo | 2 | 3, 5 | I03, I04 | Nova di gelo: +0,5 s di congelamento per grado. | Per chi conta sulla nova contro il re. |
| I08 | **Sangue freddo** | passivo | 3 | 3, 7 | I04 | +4% vita massima per grado. | Più vita per un Mago che sta in mezzo. |
| I09 | **Armatura di ghiaccio** | attivo | 1 | 4, 2 | I05, I06 | Per 15 s +40% armatura, e chi colpisce in mischia viene rallentato del 30%. 12 mana, ricarica 35 s. | La difesa a comando, come la Pelle di pietra del Guerriero. |
| I10 | **Brina** | passivo | 2 | 4, 4 | I06, I07 | Nova di gelo: +0,5 m di raggio per grado. | Con due gradi la nova arriva a 4,5 m. |
| I11 | **Riparo di ghiaccio** | passivo | 2 | 4, 6 | I07, I08 | −4% danno preso per grado. | Vale contro tutto. |
| I12 | **Gelo che resta · Nova doppia** | a scelta | 1 | 5, 3 | I09, I10 | **Gelo che resta:** i nemici congelati dalla Nova restano rallentati del 40% per 3 s. **Nova doppia:** la Nova di gelo scatta due volte, a 1 s di distanza. | Si sceglie come tenere fermo lo sciame: più a lungo, o due volte. |
| I13 | **Mente di ghiaccio** | passivo | 2 | 5, 5 | I10, I11 | Ogni nemico congelato ridà 2 mana per grado. | Il mana del Gelo viene dal controllo. |
| I14 | **Tempesta di neve** | attivo | 1 | 6, 4 | I12, I13 | Per 6 s una tempesta su un'area di 5 m rallenta i nemici del 50% e li colpisce ogni 0,5 s (il 40% del Dardo di fuoco). 28 mana, ricarica 60 s. | Il traguardo: un'area dove i nemici non arrivano. |
| I15 | **Fortezza di ghiaccio** | passivo | 1 | 6, 6 | I13 | Armatura di ghiaccio: +10 s di durata. | Il traguardo della difesa: l'armatura di tutto lo scontro. |
