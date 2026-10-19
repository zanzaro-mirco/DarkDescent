# Tools

Script fuori da Unity che producono asset del gioco. Ogni asset si rifà dallo script.

| Cartella | Cosa fa | Come si lancia |
|---|---|---|
| `Blender/` | Modelli 3D nello stile KayKit, esportati in FBX in `Assets/_Project/Art/Models/DarkDescent/` (ADR-094) | `blender --background --factory-startup --python Tools/Blender/make.py -- [nomi] [--photo DIR]` (Blender 5.2 LTS) |
| `Audio/` | Effetti sonori sintetizzati, in WAV in `Assets/_Project/Audio/SFX/Generated/` (ADR-095) | `python Tools/Audio/make.py [nomi] [--out DIR --page]` (Python 3.13, `pip install -r Tools/Audio/requirements.txt`) |

Un modello nuovo è un modulo in `Blender/models/` con `MODELS`, `COMPARE` e `PHOTO`; un suono nuovo è
un modulo in `Audio/sounds/` con `SOUNDS`, `TITLE`, `DESCRIPTION`, `COMPARE` e `MAX_DUR`.
Prima di entrare nei prefab, un modello passa da una foto accanto a un KayKit (`--photo`) e un suono da
una pagina di ascolto (`--page`).
