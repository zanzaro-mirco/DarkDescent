"""Genera suoni e modelli con ComfyUI (M13, D15), con l'app aperta sul PC.

    python Tools/ComfyUI/make.py --check                     controlla ComfyUI e i modelli scaricati
    python Tools/ComfyUI/make.py well_drink_sa --out DIR --page
    python Tools/ComfyUI/make.py well_drink_sa --takes 4     quattro semi di fila, per scegliere
    python Tools/ComfyUI/make.py Statue_Gen --out DIR

Ogni modulo in sounds/ dichiara SOUNDS (nome del WAV -> ricetta con prompt, seconds e seed), TITLE,
DESCRIPTION, COMPARE e MAX_DUR, come quelli di Tools/Audio; i WAV passano dalla stessa finitura e dalla
stessa pagina di ascolto. Ogni modulo in models/ dichiara MODELS (nome del GLB -> ricetta con image,
seed, faces e texture). I modelli vanno sempre in --out (di base Tools/ComfyUI/out, fuori dal repo):
nel gioco entrano dopo la foto e la rifinitura in Blender.
"""
import argparse
import importlib.util
import io
import os
import pkgutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "..", "Audio"))

import comfy  # noqa: E402

OUT = os.path.join(HERE, "out")
SOUND_WORKFLOW = "stable_audio_sfx"
MODEL_WORKFLOW = "trellis2_image_to_model"


def recipes(folder):
    """I moduli di una cartella, caricati per percorso: sounds/ e models/ non si scontrano con le
    cartelle omonime degli altri strumenti."""
    path = os.path.join(HERE, folder)
    for info in sorted(pkgutil.iter_modules([path]), key=lambda m: m.name):
        spec = importlib.util.spec_from_file_location(f"comfy_{folder}_{info.name}",
                                                      os.path.join(path, info.name + ".py"))
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        yield module


def seeds(recipe, takes):
    return [recipe["seed"] + k for k in range(takes)]


def take_name(name, seed, takes):
    return name if takes == 1 else f"{name}_s{seed}"


def make_sound(client, name, recipe, seed, max_dur, out_dir):
    import numpy as np
    import soundfile
    from scipy import signal

    import sfxlib

    workflow = comfy.with_inputs(comfy.load_workflow(SOUND_WORKFLOW), {
        ("prompt", "text"): recipe["prompt"],
        ("negative", "text"): recipe.get("negative", ""),
        ("length", "seconds"): float(recipe["seconds"]),
        ("sampler", "seed"): seed,
    })
    item = comfy.output_files(client.run(workflow), "audio")[0]
    data, rate = soundfile.read(io.BytesIO(client.fetch(item)), dtype="float64", always_2d=True)
    x = data.mean(axis=1)
    if rate != sfxlib.SR:
        x = signal.resample_poly(x, sfxlib.SR, rate)
    x = sfxlib.finish(np.asarray(x), max_dur=max_dur)
    path = os.path.join(out_dir, name + ".wav")
    sfxlib.write_wav(path, x)
    print(f"[comfy] {name}.wav {len(x) / sfxlib.SR:.2f} s (seme {seed})")
    return path


def make_model(client, module, name, recipe, seed, out_dir):
    image = client.upload_image(os.path.join(os.path.dirname(module.__file__), recipe["image"]))
    workflow = comfy.with_inputs(comfy.load_workflow(MODEL_WORKFLOW), {
        ("image", "image"): image,
        ("structure", "seed"): seed,
        ("shape", "seed"): seed,
        ("texture", "seed"): seed,
        ("faces", "target_face_count"): recipe.get("faces", 8000),
        ("unwrap", "resolution"): recipe.get("texture", 1024),
        ("bake", "texture_size"): recipe.get("texture", 1024),
    })
    item = comfy.output_files(client.run(workflow), "3d")[0]
    path = os.path.join(out_dir, name + ".glb")
    with open(path, "wb") as f:
        f.write(client.fetch(item))
    print(f"[comfy] {name}.glb (seme {seed})")
    return path


def check(client):
    print(f"[comfy] ComfyUI {client.version()} su {client.base}")
    ok = True
    for workflow in (SOUND_WORKFLOW, MODEL_WORKFLOW):
        missing = client.missing_models(comfy.load_workflow(workflow))
        print(f"[comfy] {workflow}: " + ("pronto" if not missing else "mancano " + ", ".join(missing)))
        ok = ok and not missing
    return ok


def main():
    parser = argparse.ArgumentParser(prog="make.py", description="Genera suoni e modelli con ComfyUI.")
    parser.add_argument("names", nargs="*", help="nomi dei suoni o dei modelli")
    parser.add_argument("--out", help="cartella di uscita (suoni: Audio/SFX/Generated, modelli: Tools/ComfyUI/out)")
    parser.add_argument("--page", action="store_true", help="scrive anche la pagina di ascolto dei suoni")
    parser.add_argument("--takes", type=int, default=1, help="quanti semi provare, a partire da quello della ricetta")
    parser.add_argument("--host", default=comfy.DEFAULT_HOST, help="indirizzo di ComfyUI")
    parser.add_argument("--check", action="store_true", help="controlla solo ComfyUI e i modelli")
    args = parser.parse_args()

    client = comfy.Comfy(args.host)
    try:
        if args.check:
            sys.exit(0 if check(client) else 1)
        if not args.names:
            parser.error("dai almeno un nome, o --check")
        sound_modules = list(recipes("sounds"))
        model_modules = list(recipes("models"))
        known = {n for m in sound_modules for n in m.SOUNDS} | {n for m in model_modules for n in m.MODELS}
        unknown = set(args.names) - known
        if unknown:
            sys.exit("nomi sconosciuti: " + ", ".join(sorted(unknown)))

        made = []
        for module in sound_modules:
            files = []
            for name, recipe in module.SOUNDS.items():
                if name not in args.names:
                    continue
                import sfxlib
                out_dir = args.out or sfxlib.GENERATED
                os.makedirs(out_dir, exist_ok=True)
                for seed in seeds(recipe, args.takes):
                    files.append(make_sound(client, take_name(name, seed, args.takes), recipe, seed,
                                            module.MAX_DUR, out_dir))
            if files:
                made.append((module, files))
        for module in model_modules:
            for name, recipe in module.MODELS.items():
                if name not in args.names:
                    continue
                out_dir = args.out or OUT
                os.makedirs(out_dir, exist_ok=True)
                for seed in seeds(recipe, args.takes):
                    make_model(client, module, take_name(name, seed, args.takes), recipe, seed, out_dir)
        if args.page and made:
            import preview
            print(f"[comfy] pagina di ascolto: {preview.write_page(os.path.dirname(made[0][1][0]), made)}")
    except comfy.ComfyError as e:
        sys.exit(f"[comfy] {e}")


if __name__ == "__main__":
    main()
