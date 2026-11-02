# Da provare nella prossima build

Le prove in build che Mirco non ha ancora fatto. Si aggiunge una riga quando una milestone salta la
prova, e si toglie quando la prova è fatta (con le note nella scheda della milestone). I test
automatici non bastano per queste: sono sensazioni, cose a schermo o suoni.

Ultimo aggiornamento: 2 nov 2026, build della v1.8.0 da provare (l'artefatto della build del tag).
Le prove della M18 e della M19 restano valide.

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
