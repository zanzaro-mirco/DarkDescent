# Devlog

> Una nota per ogni sessione di lavoro. Dieci minuti, non di più.
> A fine progetto questo file è il materiale per il README, per gli articoli
> tecnici e per raccontare il progetto a un colloquio con dettagli veri.
>
> Formato: **Cosa ho fatto · Cosa si è rotto · Cosa ho capito**

---

## 2026-09-21 — M0: fondamenta

**Fatto:** progetto Unity 6.3 LTS (6000.3.24f1) con template Universal 3D e URP.
Git con LFS su fbx/png/wav/psd, merge driver UnityYAMLMerge, Asset Serialization
su Force Text. Rimossi i package inutili (Visual Scripting, Timeline, Multiplayer
Center, Version Control) e ripulito il template dai file TutorialInfo. Creata la
struttura `Assets/_Project/` e l'assembly definition `DarkDescent`. Repo pubblico
su GitHub, e verifica finale clonando il repo in una cartella vuota: si apre in
Unity senza errori e `Ctrl+B` produce un eseguibile che parte.

**Rotto:** il README era illeggibile su GitHub — salvato con BOM e trattini lunghi
corrotti, perché PowerShell 5.1 scrive UTF-8 con BOM di default. Riscritto in UTF-8
puro. Credevo il working tree pulito, invece `ProjectSettings.asset` aveva una
modifica non committata (Unity aveva svuotato `preloadedAssets` dopo la rimozione
dei package). Mancava del tutto l'assembly definition prevista dal piano.

**Capito:** la build che gira sul mio disco non dimostra niente sul repo, perché
usa `Library/` e tutti i file che `.gitignore` esclude. Solo un clone in una
cartella vuota verifica che il repo sia autosufficiente — è il motivo per cui la
Definition of Done di M0 è scritta così e non "la build funziona".

Sull'LFS ho imparato che `git lfs ls-files` su un repo senza binari non prova
nulla: la verifica vera è confrontare il blob in Git (un pointer di testo
`version https://git-lfs.github.com/spec/v1`) con il file dopo il checkout
(24 KB, magic `FF D8 FF E0`). È il filtro smudge che scarica il contenuto.

Gli assembly definition vanno decisi **prima** di scrivere codice: un assembly
con `.asmdef` non può referenziare `Assembly-CSharp`, solo il contrario.
Aggiungerne uno dopo significa vedersi comparire di colpo decine di errori
"type or namespace not found".

