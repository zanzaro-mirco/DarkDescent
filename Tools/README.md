# Tools

Script fuori da Unity che producono asset del gioco. Ogni asset si rifà dallo script.

| Cartella | Cosa fa | Come si lancia |
|---|---|---|
| `Blender/` | Modelli 3D nello stile KayKit, esportati in FBX in `Assets/_Project/Art/Models/DarkDescent/` (ADR-094) | `blender --background --factory-startup --python Tools/Blender/make.py -- [nomi] [--photo DIR]` (Blender 5.2 LTS) |
| `Audio/` | Effetti sonori sintetizzati, in WAV in `Assets/_Project/Audio/SFX/Generated/` (ADR-095) | `python Tools/Audio/make.py [nomi] [--out DIR --page]` (Python 3.13, `pip install -r Tools/Audio/requirements.txt`) |
| `ComfyUI/` | Suoni (Stable Audio 3 Small SFX), modelli (TRELLIS.2) e immagini (Z-Image-Turbo, M17) generati da ComfyUI attraverso l'API locale (D15 della M13, ADR-114) | `python Tools/ComfyUI/make.py [nomi] [--out DIR --page --takes N]`, `--check` per i modelli scaricati (con l'app aperta; `pip install -r Tools/ComfyUI/requirements.txt`); poi `uiparts.py` (placca dei pulsanti) e `decalatlas.py` (atlante delle macchie a terra, M17) |

Un modello nuovo è un modulo in `Blender/models/` con `MODELS`, `COMPARE` e `PHOTO`; un suono nuovo è
un modulo in `Audio/sounds/` con `SOUNDS`, `TITLE`, `DESCRIPTION`, `COMPARE` e `MAX_DUR`.
Prima di entrare nei prefab, un modello passa da una foto accanto a un KayKit (`--photo`) e un suono da
una pagina di ascolto (`--page`).

In `ComfyUI/` i workflow sono in formato API in `workflows/` e il client cambia gli ingressi per titolo
del nodo; le ricette (`sounds/`, `models/`, `images/`) tengono prompt e seme di ogni asset. Le immagini
passano dalla finitura di `imagelib.py` (ripetizione, trasparenza, dimensione; i test sono in
`test_imagelib.py`) e da una pagina di confronto con `--page`; si lanciano con il Python di ComfyUI,
che ha Pillow, ed escono in `ComfyUI/out/images/`. Un modello generato
esce in GLB in `ComfyUI/out/`, fuori dal repo, e `ComfyUI/views.py` lo fotografa in Blender accanto
alla versione da script.

ComfyUI si può avviare anche senza l'app, con il Python dell'installazione e i modelli della
cartella condivisa, e poi si passa `--host 127.0.0.1:8199` a `make.py`:

```
%LOCALAPPDATA%\Comfy-Desktop\ComfyUI-Installs\DarkDescent\ComfyUI\.venv\Scripts\python.exe main.py --port 8199
    --disable-auto-launch --extra-model-paths-config "%APPDATA%\Comfy Desktop\instance-model-paths\<istanza>.yaml"
```
