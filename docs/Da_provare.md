# Da provare nella prossima build

Le prove in build che Mirco non ha ancora fatto. Si aggiunge una riga quando una milestone salta la
prova, e si toglie quando la prova è fatta (con le note nella scheda della milestone). I test
automatici non bastano per queste: sono sensazioni, cose a schermo o suoni.

Ultimo aggiornamento: 6 nov 2026, build della v1.12.0 da provare (l'artefatto della build del tag).
Le prove della M18, della M19 e della M23 restano valide.

## Dalla M24 — "Nemici più vivi" (v1.12.0)

- **Vorrak il Custode** (profondità 4, nella sala della scala giù): lo trovi senza cercarlo? Con 240 di vita, sono 8 scheletri:
  è troppo morbido o troppo duro? La carica si schiva guardando la striscia rossa? Sotto la metà evoca tre scheletri e
  carica: ti sembra giusto, o troppo tutto insieme? La scala giù resta aperta (chi non vuole combattere scende).
  Il tesoro si apre solo alla sua morte (tre oggetti, almeno un magico).
- **La fuga**: uno scheletro ferito scappa una volta sola e torna con i compagni; uno sciame con i compagni morti scappa?
  Si capisce che sta scappando, o sembra un errore del percorso?
- **Le ronde**: un nemico su quattro cammina tra due o tre punti; quando ti vede, ti insegue; tornato a casa riprende. Si
  nota subito la differenza con i livelli di prima? Un nemico di ronda può coprire una porta o un corridoio?
- **«Il mondo cresce con te»** (altare): il pulsante sta a schermo a 1080p e a risoluzioni più basse? Il suggerimento
  compare a destra della finestra. Acceso, con un cavaliere di livello 20 alla profondità 1 i nemici hanno più vita
  (×1,64) e gli oggetti sono da livello 17: si sente? È troppo poco (a livello 25 il bruto muore ancora in 3 colpi)?
  La pendenza e il tetto stanno in `Progression.asset`.
- **Il salvataggio 11**: una partita salvata con la v1.11 si carica; i livelli si rigenerano (nessun livello ricordato) e
  il portale riporta all'ingresso della profondità, ma classe, oggetti, oro e livello restano.
- **Il costo** (build, `-benchmark-levels`): con le ronde e la fuga accese il tempo dei livelli e i fotogrammi in
  una cripta alla 6 sono quelli della v1.11? (In editor la misura non vede differenze.)

## Dalla M23 — "A portata di mano" (v1.11.0)

- **WASD e le frecce**: camminare con la tastiera, anche dopo aver girato la visuale con `Z` e `X` (`W` deve andare
  sempre verso l'alto dello schermo). Muri, angoli e il corridoio stretto della cripta: si striscia o si vibra? La
  diagonale è più veloce o uguale? Rilasciando i tasti ci si ferma subito?
- **Colpire e lanciare camminando**: con `W` premuto, un click su uno scheletro a portata lo colpisce da fermo; il tasto
  destro, `Q` ed `E` lanciano verso il cursore anche in movimento (col Mago il dardo arcano). Ti sembra naturale, o ti
  aspettavi di inseguire il nemico?
- **Click e tasti insieme**: premendo un tasto mentre il cavaliere cammina verso un click, vince il tasto; a tasti
  rilasciati e sinistro ancora premuto, riprende a seguire il cursore senza uno scatto.
- **Le barre della vita** (opzioni, spente di partenza): accese, una barra sopra ogni nemico ferito; guarito o morto la
  perde. Si leggono contro lo sciame? Il campione è blu e l'unico oro, con il nome: ti torna, o li vuoi come nel disegno
  (dorato e arancione)? Dietro un muro la barra sparisce con il nemico. Con tanti nemici il gioco rallenta? (In editor la
  misura non vede differenze, sta nel costo di un canvas piccolo.)
- **I suggerimenti**: a una partita nuova (o con *Rivedi* nelle opzioni) compaiono uno alla volta, in basso al centro:
  muoversi, il primo nemico, il primo oggetto a terra, la pozione, la scala, i punti da spendere... Compaiono al momento
  giusto? Il riquadro copre qualcosa che serve? Un tasto lo chiude dopo mezzo secondo, scade da solo in 8 secondi. La
  cintura si legge «da 1 a 8» (in Cinzel il «1» somiglia a una «I», e il trattino non aiutava).
- **Il pannello dei comandi**: `F1` e il pulsante *Comandi* nella pausa. Le righe sono tutte vere? Il Mago legge «dardo
  arcano». Da pausa, `F1` passa dal menu e ne esce senza chiudere la pausa.
- **Le opzioni**: il riquadro è più alto (920 px) con le due caselle nuove e *Rivedi i suggerimenti*; stanno tutte a
  schermo anche a risoluzione bassa?

## Dalla M22 — "La strada del ritorno" (v1.10.0)

- **La scala in su**: in ogni livello, nella stanza dove si arriva (o attorno all'ingresso, nelle caverne), c'è lo sbocco del
  cunicolo con le sue luci. Passarci accanto non fa risalire: si clicca e il cavaliere ci va. Il nome sotto il cursore dice
  «Sali al livello N» (e «Torna al borgo» alla 1). Risalendo si arriva davanti alla scala giù del livello di sopra, rivolti
  via da lei, senza riscendere subito. Ti sembra il posto giusto, o lo vorresti più lontano dalla strada principale?
- **Il livello di sopra com'era**: uccidi, apri una cassa, lascia un oggetto a terra, scendi, risali. Tutto come l'hai
  lasciato, la mappa scoperta compresa. Funziona anche tra le caverne (5) e la cripta (4).
- **Dal borgo alla cripta e ritorno**: alla 1 la scala in su porta fuori dalla grotta, nella piazzola a sud dell'arco, e la
  grotta riporta alla 1.
- **L'automappa**: la scala in su è una cella azzurra (la scala giù è gialla, l'ingresso verde). Si legge bene, o la vuoi più
  evidente?
- **Le stanze della cripta**: più grandi (da 4 a 8 celle per lato). Se i livelli ti sembrano vuoti, si alza il numero dei
  gruppi, non la forza (D12 della scheda).
- **Un vecchio salvataggio**: *Continua* da una cripta rimette nello stesso posto, in un livello nuovo dello stesso seme: i
  nemici uccisi e le casse aperte prima non si ricordano più (formato 10).
- **Il re**: durante lo scontro la porta si chiude e la scala in su dell'8, che sta nella stanza d'ingresso, non si raggiunge.

## Dalla M21 — "La via dell'arcano" (v1.9.0)

- **La creazione**: *Nuova partita*, un posto, la schermata. Il modello gira piano, le due schede cambiano
  testo e numeri, il nome di partenza segue la classe finché non scrivi il tuo, *Indietro* torna ai posti.
  Il campo del nome è un rettangolo nero semplice: va bene o lo vuoi nella pelle del gioco? Gli emblemi
  sulle schede sono piccoli.
- **Il Mago a mano**: il Dardo arcano al sinistro (da 9 m, senza mana), il destro con il Dardo di fuoco, `Q`
  con l'Anello e `E` con il Passo di luce o lo Scudo di mana. I gesti, i suoni e gli effetti si leggono?
  Il ghiaccio è verdastro, il Muro di fiamme è una fila di anelli, la bruciatura non mostra la fiamma
  sul nemico.
- **Il Mago che cresce**: `DarkDescent.exe -class mage -herolevel 20`, poi `-herolevel 50`. A fine gioco
  il Mago deve essere forte ma non invincibile: la crescita degli incantesimi è ripida (Magia 200 fa cinque
  volte e mezza i numeri di base) e l'Armatura di classe è 22 + 1,3 a livello. Se è troppo, si abbassa la
  pendenza e si rivedono le fasce.
- **Gli alberi**: la scelta tra *Fuoco* e *Gelo* al livello 10, i nodi a scelta (Scintilla o Fiammata, Pioggia di
  scintille o Anello infuocato, Meteora doppia o Marea di fuoco, Gelo che resta o Nova doppia). La Nova
  doppia è debole (la seconda ondata congela solo chi è arrivato nel frattempo).
- **I posti**: «Nome · Classe · livello · luogo»; un salvataggio vecchio dice «Cavaliere · Guerriero».
- **Il Mago nell'apertura e nella vittoria**: il modello e le didascalie con «l'eroe».

## Dalla M20 — "La via della spada" (v1.8.0)

- **I numeri del danno**: compaiono solo sui colpi critici, arancio e con il punto esclamativo. I colpi
  normali si capiscono lo stesso dal lampo e dalla barra della vita? *Mancato*, *Bloccato* e
  *Imparabile* ci sono ancora: vanno bene o li vuoi togliere anche loro?
- **Le abilità del Guerriero** (passo 20.4): il Colpo possente vicino a un nemico, la Carica da
  lontano, il Turbine in mezzo a un gruppo, il Grido, e con un personaggio avanzato il Salto, il Colpo
  di scudo, il Muro di scudi, l'Esecuzione e la Tempesta d'acciaio. I gesti si leggono? Gli stordimenti
  (le stelle sopra la testa) durano il giusto? I suoni nuovi (la lama, lo scudo, l'atterraggio, i
  tintinnii) e il grido vanno bene?

- **Le quattro caselle** (passo 20.4b): la console in basso con le sfere attaccate ai lati, le pozioni
  a sinistra e le quattro caselle a destra. Il sinistro su un nemico colpisce, il destro, `Q` ed `E`
  lanciano le abilità delle loro caselle; la camera gira con `Z` e `X`. Un click su una casella apre
  l'elenco, `K` le quattro righe, `Esc` le chiude. La grafica va bene a 16:9 e a 4:3? Le icone sulle
  caselle (20.7) si riconoscono a colpo d'occhio? Con una partita vecchia (a otto posti) le
  abilità tornano in ordine nelle caselle?

- **Le armi a due mani** (passo 20.5): dal fabbro o da un forziere, uno spadone e un'ascia a due mani. Indossandoli lo scudo va nella borsa (e senza posto non succede niente); rimettendo lo scudo l'arma torna in borsa. Il gesto d'attacco si legge, e la lama arriva insieme al danno? Il Turbine con l'arma grande arriva un po' più in là?

- **Il pannello delle abilità** (passo 20.6): `N` apre l'albero a tutto schermo: click sinistro prende un grado, destro lo toglie, *Applica modifiche* li rende veri e *Annulla* li butta. Dal livello 10 si sceglie la specializzazione dalle schede. Il tooltip si legge e dice cosa manca per aprire un nodo? Il nodo a scelta mostra le due opzioni? L'avviso «Abilità +N» accanto alla sfera del mana compare e sparisce quando serve? All'altare la voce «Riassegna le abilità» prende 30 oro per livello e riporta tutti i punti? Le icone dei nodi (20.7) dicono cosa fa il talento, o ce n'è qualcuna da cambiare? I nodi a scelta mostrano ancora il nome.

- **Il bilancio con l'albero** (passo 20.8): la misura dice che con l'albero *Armi* il cavaliere è più
  forte fino al 20% e che le difficoltà restano nelle fasce, ma la sensazione si prova giocando. Un
  personaggio al 12, al 29 e al 40 con le abilità prese: le discese più alte sono ancora una sfida come
  nella v1.7, o sono troppo facili (o troppo dure)? Con la *Difesa*, il blocco e l'armatura contano o
  si sentono appena? I numeri di nove nodi sono stati abbassati rispetto al disegno approvato (elenco in
  `docs/milestones/M20_albero_Guerriero.md`): c'è un talento che non si sente?

## Dalla M19 — "L'ultima candela" (v1.7.0)

- **L'apertura**: *Nuova partita* dal menu, scegli un posto. Parte il filmato di circa 50 s, con la
  sua musica. Le cinque scene si capiscono? Il ritmo va bene, o qualche scena è troppo lunga o
  troppo corta?
- **La notte**: le candele si spengono una a una, ognuna con il suo soffio? Il cimitero, con la luna,
  si legge o è troppo buio sul tuo schermo? La terra si rompe sopra ogni scheletro che esce?
- **Le comparse**: i paesani che scappano con le lanterne, gli scheletri, il mercante che accende la
  candela e indica la grotta. Qualcosa scatta, scivola o compenetra?
- **Le didascalie e il titolo**: si leggono, in italiano e in inglese (cambia lingua dalle opzioni e
  rifai una partita nuova)? Le bande nere sopra e sotto stanno bene?
- **Il salto**: Esc o un click mostrano in alto «Esc o click per saltare», il secondo salta, e la
  musica sfuma in fretta. Si torna nel borgo di giorno, con il cavaliere libero e la musica del borgo?
- **La fine**: la camera scende sul cavaliere e diventa quella del gioco senza uno strappo?
- **Quando non deve partire**: *Continua* su un posto già usato non mostra il filmato.
- **La vittoria**: batti il Re delle ossa (da una partita avanzata). Il re cade al rallentatore e resta
  a terra, poi si gioca e il tesoro si apre. Entrando nel portale il cavaliere guarda le ossa ed
  entra; nel borgo all'alba esce dalla grotta, scende la strada, passa sotto l'arco del cimitero e si
  ferma accanto al fuoco, mentre i sopravvissuti esultano e le candele si riaccendono. La musica parte
  al portale e continua nel viaggio? Poi la finestra, e con *Continua* si gioca da lì.
- **I filmati nel menu**: *Filmati*, sotto *Riconoscimenti*. *L'ultima candela* c'è sempre, *La
  vittoria* solo dopo averla vista. Rivisti, tornano al menu senza toccare i posti salvati?
- **Il volume**: la musica delle opzioni abbassa anche quella dei filmati.
- **La strada del borgo**: i sampietrini si leggono, da vicino e da lontano, di giorno e di notte?
  Nessuna interruzione dalla gola alla piazza? Le carriole cariche sul bordo non danno fastidio
  camminando?
- **Il saluto**: l'abitante in piedi accanto al fuoco saluta quando arrivi dall'ingresso o dal portale?
- **Il fluido**: i filmati girano lisci (D17 chiede 60 fps), anche nella notte con tutte le finestre
  accese?

## Dalla M18 — "Il borgo" (v1.6.0)

- **Il borgo**: si cammina bene tra le case, o qualche pezzo blocca la strada? La sagoma ocra si
  vede quando una casa o un albero coprono il cavaliere?
- **Il giorno che passa**: un giro dura 20 minuti. Alba, tramonto e notte sono leggibili? Le
  finestre e le lanterne si accendono al buio?
- **La grotta**: si trova, si clicca bene e si accende sotto il cursore? Alla profondità 1 lo
  sbocco del cunicolo sta nel muro, e si capisce che si è arrivati da lì?
- **Gli abitanti**: quello in piedi saluta quando si arriva dall'ingresso o dal portale?
- **Il suono del borgo**: fuoco e vento sotto la musica, il corvo ogni tanto. Il giro di 15 s si
  sente?
- **I tre banchi**: il fabbro alla forgia, il mercante, la guaritrice sulla soglia. Ognuno ha la sua
  merce e il suo nome sulla finestra? Un oggetto offerto al banco sbagliato dice "Non compra questo
  oggetto"? La guaritrice riempie vita e mana?
- **Un salvataggio della v1.5** fatto nell'accampamento: continua nel borgo, con oro, oggetti e
  portale aperto?

## Dalla M17 — "Una pelle nuova" (v1.5.0)

- **Il menu iniziale**: lo sfondo della cripta, i pulsanti in colonna, *Riconoscimenti* che scorrono e
  tornano al menu. A una risoluzione diversa da 16:9 lo sfondo copre lo schermo senza deformarsi?
- **Le finestre**: inventario, scheda, mercante, altare e tooltip con la cornice nuova. Il testo si
  legge bene, anche i colori delle rarità e il rosso del prezzo troppo alto?
- **I pulsanti**: si scaldano al passaggio del mouse e scuriscono al click.
- **Le opzioni** per gruppi, in inglese e in italiano.
- **Le macchie a terra** in cripta e caverne: si notano senza disturbare? Troppe, poche, troppo
  accese (il muschio)? Con il filtro retrò reggono?
- **La morte e la vittoria** con gli sfondi (la vittoria è la sala del re presa dal gioco, in attesa
  del filmato della M19); *Continua* dalla vittoria lascia giocare in città e porta all'altare.

## Dalla M16 — "Tesori con un nome" (v1.4.0)

Si lancia `DarkDescent.exe -difficulty 1 -herolevel 12 -namedchance 50` e si sceglie *Nuova partita*
in un posto libero: all'Ombra quasi ogni oggetto è un unico o un pezzo di set.

- **A terra**: il nome in oro (unico) o in verde (set), la luce più forte del colore suo e il suono
  di quando cade. Si distingue da un raro anche da lontano?
- **Il tooltip**: il nome, la base sotto, le proprietà in blu. Di un pezzo di set i pezzi del set
  (bianchi se indossati, grigi se no) e i bonus, verdi quando sono attivi, in inglese e in italiano.
- **Un set intero**: indossando i quattro pezzi i bonus si accendono uno alla volta nel tooltip, e
  i valori della scheda del cavaliere salgono con loro.
- **Le proprietà nuove**: la velocità d'attacco si vede nei colpi, il rubavita nella vita che torna,
  il danno ridotto nei colpi del bruto. L'Ultima Candela ora dà Vitalità invece del mana.
- **Salvare e riprendere**: unici e set restano con nome e valori, indossati e nello zaino.
- **Senza `-namedchance`**: quanti ne cadono davvero. La misura dice circa uno a discesa all'Ombra e
  quasi quattro all'ultimo Abisso; al Crepuscolo solo, a volte, dal tesoro del re.

## Dalla M13 — "Stanze vive" (v1.2.x, build saltata il 21 ott 2026)

- **Barili**: si rompono con l'arma e a volte lasciano oro, una pozione o un oggetto.
- **Barili esplosivi** dalla profondità 4: cerchiati di rosso, miccia di 1 s, l'esplosione ferisce
  anche i nemici.
- **Spuntoni** dalla profondità 3: si attraversano a tempo, con l'avviso e lo scatto.
- **Pozzi e santuari** (solo nella cripta, al più uno per tipo a livello): il bonus del santuario
  dura 60 s, con l'icona accanto alla cintura e il suono di fine.
- **Arredi**: niente doppioni nella stessa stanza, oggetti girati, mucchietti per tema, la cappella.
- **Suoni nuovi** nel gioco: escono con tiri diversi da quelli della pagina di ascolto.
- **Loot**: circa 5 oggetti a livello; troppo o troppo poco?

## Dalla M15 — "La ronda notturna" (v1.3.1)

- **Il re c'è sempre**: `DarkDescent.exe -newgame -seed 455840980255010401`, scendere fino all'8:
  c'è la sala del re (prima della correzione mancava, e il mondo non si finiva). Lunga: si fa se
  capita, i test la coprono.
- **Abisso I un po' più duro**: la vita dei nemici è ×14,2 invece di ×13,5. Con
  `DarkDescent.exe -difficulty 3 -herolevel 32` e *Nuova partita*: il bruto della profondità 1 non
  deve sembrare più facile di quello della Tenebra (`-difficulty 2 -herolevel 22`, il livello della
  sua zona).

## Dalla M14 — "La luce si spegne" (v1.3.0)

- **La riga della morte**: quanto oro ed esperienza si perde, nella schermata della morte. Mai vista
  a schermo.
- **L'altare all'accampamento**: pannello, conferma, mondo nuovo. Manca l'ok sulle foto del passo 14.5.
- **I gradi degli affissi**: il numero romano nel tooltip e i nomi nelle due lingue.
- **Crepuscolo con un salvataggio vecchio**: si carica, si resta al Crepuscolo, l'equipaggiamento
  è intatto.
- **Ombra fino al re**: il bruto regge una decina di colpi già alla profondità 1; battuto il re,
  "Si apre" e la Tenebra si sblocca.
- **Abisso**: lanciare `DarkDescent.exe -difficulty 3 -herolevel 32` e scegliere *Nuova partita* in un
  posto libero (dal passo 14.7 le due opzioni valgono anche dal menu). Il cavaliere regge almeno 3
  colpi del bruto e l'anello di fuoco del re fa male. Col blocco al 75% i nemici colpiscono
  poco: se è troppo facile, il primo numero da alzare è il loro danno.
