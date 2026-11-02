# M21 — "La via dell'arcano"

**Cosa deve succedere (Definition of Done):** *Nuova partita* fa **creare il personaggio**: si sceglie la
classe, il **Guerriero** di oggi o il **Mago**, e si scrive il nome. Il Mago combatte con gli incantesimi,
ha il suo **albero delle abilità** (la classe e due specializzazioni), le sue armi (**bastone** e
**bacchetta**) e il suo modello. Il salvataggio tiene classe e nome, e i tre posti del menu li mostrano.

- Il pannello è lo stesso del Guerriero (tasto **N**), con gli alberi del Mago.
- Il **Dardo di fuoco** e l'**Anello di fuoco**, tolti al Guerriero nella M20, tornano al Mago.
- Il bilancio non si sposta: la misura delle difficoltà prende anche il Mago, nelle stesse fasce (D11).
- Tag **`v1.9.0`**.

**Fuori da questa milestone:** il Ranger e il Ladro (M27, M28); l'equipaggiamento che si vede addosso
(M26); più personaggi nello stesso posto; cambiare classe a partita iniziata; le armi a distanza con le
frecce.

---

## Stato verificato il 3 novembre 2026

Verificato dopo la chiusura della M20 (tag `v1.8.0`, build 41529821395 verde).

| Cosa | Com'è oggi |
|---|---|
| La classe | Non esiste: il cavaliere è il prefab `Player`, con il modello `Knight`. `PlayerTalents` ha due campi, l'albero della classe e le specializzazioni, assegnati nel prefab |
| Gli incantesimi | `SpellDefinition` (in `Core/Magic`) ha i tipi `SpellKind` del Dardo, dell'Anello e della Pelle di pietra, più quelli del Guerriero (`IsWarriorAbility`). Il Dardo di fuoco e l'Anello (`Data/Spells/Firebolt`, `FireRing`) ci sono ancora, senza un libro che li dia: i test li usano con un libro di prova |
| Il danno magico | `CombatFormulas.SpellDamage(roll, magia, divisore)`; il mana è 50 + 2 × Magia + il livello. Il mana del Guerriero viene da colpire (Disciplina), non si rigenera |
| Le armi | `WeaponKind` ha Unarmed, Dagger, Sword, Axe; le due mani sono la bandiera `TwoHanded` (M20). Bastone e bacchetta non ci sono. Nel progetto sono importati il `Knight`, il `Mage`, il `Ranger`, il `Rogue` e il `Barbarian`; di armi `axe_1handed`, `axe_2handed` e `dagger` (le altre sono nello zip di *Adventurers*) |
| Il menu | *Nuova partita* sceglie il posto e parte l'apertura (M19). Il salvataggio è alla versione 8; non ha né classe né nome |
| La misura | `PowerCensus`, `HeroSetup` e `DifficultyCensus` simulano i colpi d'arma di un cavaliere con scudo; `HeroSetup.SpendTalents` spende l'albero di riferimento (M20). Gli incantesimi non entrano |
| Le animazioni | Il controller del cavaliere ha il gesto del lancio (`Spellcast`) del Dardo e dell'Anello, e i gesti del Guerriero (M20). Il `Mage` di KayKit ha lo stesso scheletro |

---

## Decisioni

**Da confermare da Mirco.** Ognuna ha una proposta e le alternative scartate.

| # | Decisione | Proposta | Perché |
|---|---|---|---|
| D1 | **Versione e nome** | **M21 "La via dell'arcano"**, tag **`v1.9.0`**. Dentro: la creazione del personaggio, il Mago con il suo albero, le sue armi e il salvataggio 9 | Una classe nuova e una schermata nuova: una versione minore |
| D2 | **La classe è un dato** | Un asset `CharacterClass`: nome, descrizione, modello, albero della classe e specializzazioni, abilità di partenza, attributi iniziali, schema dei punti (Guerriero: Forza, Vitalità, Forza, Vitalità, Destrezza), arma iniziale. Un solo prefab `Player`: alla creazione si applica la classe e il modello cambia. L'alternativa è un prefab per classe, da tenere uguali a ogni modifica | La M27 e la M28 aggiungono una riga di dati, non un prefab. `PlayerTalents`, `HeroSetup` e la misura leggono la classe |
| D3 | **Il Mago combatte con gli incantesimi** | Il sinistro del Mago è il **Dardo arcano**: parte dalla bacchetta o dal bastone, a distanza, **senza mana e senza ricarica**, con il danno dell'arma più la Magia. Il Dardo di fuoco diventa un'abilità del destro e costa mana. Alternative: il bastone in mischia (poco da Mago), o il sinistro che costa mana | È la regola del sinistro (M20): niente mana e niente ricarica. Il Mago ha poca vita e armatura, quindi attacca da lontano |
| D4 | **Le armi** | **Bastone** (due mani: danno alto, Magia richiesta) e **bacchetta** (una mano, con il **tomo** nell'altra mano al posto dello scudo: + Magia e + mana, senza blocco). Tre valori nuovi, `WeaponKind.Staff`, `WeaponKind.Wand` e un tomo come armatura dello slot della mano libera. Modelli di *Adventurers* (`staff`, `wand`, il libro di incantesimi: da importare dallo zip, con il sì di Mirco). Il Guerriero non le usa per i requisiti (Magia), e il Mago non usa lo scudo | Come per le armi a due mani (M20), un tipo d'arma nuovo vuole regola dello slot, gesto, affissi e loot, non una struttura nuova |
| D5 | **Le due specializzazioni** | ***Fuoco***: il danno e le aree (Palla di fuoco, Meteora, fiamme che restano). ***Gelo***: il controllo e la difesa (rallentare, congelare, armatura di ghiaccio). Alternativa: *Fuoco* e *Arcano* (teletrasporto, mana), più da giocatore esperto | Come *Armi* e *Difesa*: una che fa danno e una che tiene a bada i nemici |
| D6 | **Le abilità attive** | Undici, come il Guerriero, ognuna con il suo gesto, effetto e suono. **Classe**: Dardo di fuoco, Anello di fuoco, Scudo di mana (il danno preso scala il mana), Passo di luce (teletrasporto breve). **Fuoco**: Palla di fuoco, Muro di fiamme, Meteora. **Gelo**: Dardo di ghiaccio (rallenta), Nova di gelo (congela attorno), Armatura di ghiaccio, Tempesta di neve. I nomi e i numeri si decidono nel disegno (21.1). Servono tipi nuovi (`SpellKind`): teletrasporto, scudo di mana, area che resta, rallentamento e congelamento dei nemici | Il Guerriero ne ha undici; un Mago con meno sarebbe povero accanto a lui |
| D7 | **Il mana del Mago** | Si **rigenera piano** (1% al secondo, fuori dal combattimento di più) oltre a tornare con le pozioni: un Mago che lancia senza fine finisce a secco in mezzo a una stanza. Il Guerriero resta com'è (il mana viene dal colpire) | Senza rigenerazione il Mago vive di pozioni; con il Dardo arcano gratis (D3) il mana serve alle sole abilità forti |
| D8 | **La creazione del personaggio** | *Nuova partita* → il posto → una schermata: **le due classi** con il modello che gira, una riga sul loro stile e sui numeri di partenza, e il **nome** (fino a 16 lettere, uno di partenza per classe). Poi l'apertura. Il nome e la classe vanno nel salvataggio e nei tre posti del menu («Mirco · Mago · livello 12»). I salvataggi vecchi si aprono come Guerriero, con il nome «Cavaliere». Il nome non si cambia dopo, come in Diablo | Dalla richiesta dei tester del 18 ottobre |
| D9 | **Il salvataggio 9** | Aggiunge `class` e `name`; la migrazione dall'8 mette Guerriero e «Cavaliere». Un campione nuovo è `save_v9`, con un test di migrazione da `save_v8`. La barra, l'albero e le abilità si salvano come oggi, per classe | Lo schema del salvataggio 8 non cambia altro |
| D10 | **I filmati e le parole** | L'apertura e la vittoria usano il **modello della classe scelta** (oggi il cavaliere). Il testo dice «il cavaliere» in più posti: dove parla del personaggio scelto diventa **«l'eroe»** (*the hero*), nei filmati e nei messaggi; «cavaliere» resta solo nel nome della classe. Alternativa: due versioni delle didascalie | Una sola versione dei testi, e il Mago non è un cavaliere |
| D11 | **Bilancio e ronda** | La misura delle difficoltà prende anche il **Mago** di riferimento (*Fuoco*, e *Gelo* come confronto), con l'attacco a distanza (arma + Magia) e senza scudo, nelle **stesse fasce** della M14, con la tolleranza del 5% sul lato corto dei colpi per uccidere. Se escono, si ritarano i **numeri della classe** (vita, armatura, danno del Dardo arcano), non le difficoltà. La ronda misura le due classi | Come per l'albero del Guerriero (D14 della M20): la M14 non si ritara |
| D12 | **Il loot e il mercante** | Bastoni, bacchette e tomi entrano nel loot e dal **mercante** (il fabbro resta delle armi e armature da guerriero); gli affissi del Mago (danno degli incantesimi, mana, ricarica) si ammettono sui tipi nuovi, e quelli del Guerriero (blocco, rubavita) non vi cadono. I numeri tengono i loot della M13 nelle fasce: la ronda li conta | Senza un loot per il Mago, il Mago è un Guerriero che non può equipaggiarsi |
| D13 | **Le icone** | Una per abilità attiva e per **tipo** di passivo: circa 40, **Z-Image-Turbo**, nello stile di Diablo III, come alla M20 (tre semi, scelti da Claude, foto per Mirco). Una decina si riusano (vita, armatura, critico…). Il Mago ha anche la sua icona di classe per la schermata (D8) | Si ripete la strada della M20, che ha funzionato |
| D14 | **Il Guerriero non cambia** | Con la classe come dato (D2) il Guerriero è una riga di dati. I suoi numeri, il suo albero e le sue 44 icone restano com'erano: la misura del 20.8 vale ancora | Un Guerriero che cambia romperebbe la misura |
| D15 | **Si controlla** | **EditMode:** `CharacterClass` (ogni classe ha modello, albero e schema), il salvataggio 9 e la migrazione, gli alberi del Mago (ogni nodo raggiungibile, testi nelle due lingue, icone), le armi nuove (slot, requisiti, affissi), la misura con il Mago. **PlayMode:** la creazione (le due classi, il nome, i posti), il Dardo arcano, ogni abilità del Mago, il pannello con i suoi alberi, la partita di ognuna delle due classi che attraversa il borgo e la cripta. **Foto** per Mirco: la schermata, il Mago in gioco, il pannello, le abilità | La scheda M20 e le sue prove sono il modello |
| D16 | **Stima e linee di taglio** | **30–42 h**, circa un quarto più della M20 per la creazione e per i tipi di incantesimo nuovi. Se serve tagliare, in quest'ordine: Tempesta di neve (diventa un passivo); la Meteora; l'Armatura di ghiaccio; la rigenerazione del mana (D7, il mana torna con le pozioni); la scelta di due specializzazioni (una sola, il *Fuoco*); le icone per tipo (restano quelle della classe). La creazione, il Dardo arcano, l'albero della classe e il salvataggio no | Stima da rivedere dopo il disegno |

---

## Passi

| # | Passo | Ore |
|---|---|---|
| 21.1 | **Il disegno**: la pagina con l'albero della classe del Mago e quelli di *Fuoco* e *Gelo*, nodi con effetto, gradi, numeri, posti, e le undici abilità (D5, D6). Si va avanti **solo con il sì di Mirco** | 2–3 |
| 21.2 | **La classe come dato**: `CharacterClass`, il modello e gli attributi di partenza applicati alla creazione, `PlayerTalents` e `HeroSetup` che la leggono, il salvataggio 9 con la migrazione e il campione, la rigenerazione del mana (D2, D7, D9) | 4–5 |
| 21.3 | **Le armi del Mago**: `Staff`, `Wand`, il tomo, i modelli, la regola dello slot, il gesto, gli affissi, il loot e il mercante (D4, D12) | 3–4 |
| 21.4 | **Le abilità del Mago**: i tipi nuovi di `SpellKind`, il Dardo arcano come sinistro, le undici con i loro gesti, effetti e suoni; il rallentamento e il congelamento dei nemici (D3, D6) | 8–11 |
| 21.5 | **L'albero del Mago**: gli asset (`TalentTools.BuildMage`), i passivi, i testi nelle due lingue, il pannello e l'altare per entrambe le classi (D5) | 3–4 |
| 21.6 | **La creazione del personaggio**: la schermata con il modello che gira, il nome, i posti del menu con classe e nome, l'apertura con il modello scelto (D8, D10) | 4–5 |
| 21.7 | **Le icone**: le ricette Z-Image, i fogli per Mirco, l'import (D13) | 2–3 |
| 21.8 | **Bilancio e chiusura**: il Mago nella misura e nella ronda, i numeri ritarati, le ADR, le lezioni, il piano, il README, `bundleVersion` 1.9.0, il tag `v1.9.0` dopo i Tests verdi in CI (D11) | 4–5 |

---

## Checklist di chiusura

- [ ] Disegno dell'albero del Mago approvato da Mirco (21.1)
- [ ] `CharacterClass`, salvataggio 9 e migrazione, rigenerazione del mana (21.2)
- [ ] Bastone, bacchetta e tomo nel loot e dal mercante (21.3)
- [ ] Dardo arcano e le undici abilità del Mago (21.4)
- [ ] Albero del Mago nel pannello; altare per entrambe le classi (21.5)
- [ ] Creazione del personaggio con nome; i tre posti; foto approvate (21.6)
- [ ] Icone approvate (21.7)
- [ ] Misura delle difficoltà con il Mago dentro la tolleranza; ronda verde (21.8)
- [ ] EditMode e PlayMode verdi in locale e in CI
- [ ] ADR, lezioni nel piano, README, prove da fare, `bundleVersion` 1.9.0
- [ ] Tag `v1.9.0` dopo i Tests verdi, build verde, peso dell'artefatto
