# Da provare nella prossima build

Le prove in build che Mirco non ha ancora fatto. Si aggiunge una riga quando una milestone salta la
prova, e si toglie quando la prova è fatta (con le note nella scheda della milestone). I test
automatici non bastano per queste: sono sensazioni, cose a schermo o suoni.

Ultimo aggiornamento: 29 ott 2026, build della M18 da provare (passo 18.7, la build del tag `v1.6.0`).

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
