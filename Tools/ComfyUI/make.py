"""Genera suoni, modelli e immagini con ComfyUI (M13, D15; M17, D9), con ComfyUI aperto sul PC.

    python Tools/ComfyUI/make.py --check                     controlla ComfyUI e i modelli scaricati
    python Tools/ComfyUI/make.py well_drink_sa --out DIR --page
    python Tools/ComfyUI/make.py well_drink_sa --takes 4     quattro semi di fila, per scegliere
    python Tools/ComfyUI/make.py Statue_Gen --out DIR
    python Tools/ComfyUI/make.py panel_back --takes 4 --page immagini, con la pagina di confronto

Ogni modulo in sounds/ dichiara SOUNDS (nome del WAV -> ricetta con prompt, seconds e seed), TITLE,
DESCRIPTION, COMPARE e MAX_DUR, come quelli di Tools/Audio; i WAV passano dalla stessa finitura e dalla
stessa pagina di ascolto. Ogni modulo in models/ dichiara MODELS (nome del GLB -> ricetta con image,
seed, faces e texture). I modelli vanno sempre in --out (di base Tools/ComfyUI/out, fuori dal repo):
nel gioco entrano dopo la foto e la rifinitura in Blender. Ogni modulo in images/ dichiara IMAGES
(nome del PNG -> ricetta con prompt, seed, size, flatten, tile e finish), DESCRIPTION e COMPARE (la
foto di prima, dalla radice del repo): le immagini escono da Z-Image-Turbo; con tile diventano
ripetibili, sfumando l'originale e la copia spostata di mezzo lato (tile True, per i materiali
uniformi) o ridisegnando con il modello la croce delle cuciture (tile "redraw", per i disegni grossi);
poi la finitura di imagelib.py. Le immagini vogliono Pillow:
si lanciano con il Python di ComfyUI, che ce l'ha. Vanno in --out (di base Tools/ComfyUI/out/images)
e negli asset solo dopo il sì sulla pagina.
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
IMAGE_WORKFLOW = "z_image_turbo"
SEAM_WORKFLOW = "z_image_seam"


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


def _image_from(client, outputs):
    from PIL import Image

    item = comfy.output_files(outputs, "images")[0]
    return Image.open(io.BytesIO(client.fetch(item))).convert("RGB")


def _redraw_seams(client, name, recipe, seed, image, work_dir):
    """Sposta l'immagine di mezzo lato e ridisegna solo la croce delle cuciture, finché si ripete:
    fino a tre semi, poi tiene il ridisegno migliore. L'immagine spostata e basta non vale: ha i bordi
    buoni ma la cucitura in mezzo."""
    import imagelib

    shifted = imagelib.offset_half(image)
    band = recipe.get("seam_band", max(32, image.width // 8))
    mask = imagelib.cross_mask(shifted.size, band, band // 2)
    shifted_path = os.path.join(work_dir, f"{name}_{seed}_shifted.png")
    mask_path = os.path.join(work_dir, f"{name}_{seed}_mask.png")
    shifted.save(shifted_path)
    mask.save(mask_path)
    best, best_score = None, None
    for attempt in range(3):
        workflow = comfy.with_inputs(comfy.load_workflow(SEAM_WORKFLOW), {
            ("prompt", "text"): recipe["prompt"],
            ("image", "image"): client.upload_image(shifted_path),
            ("mask", "image"): client.upload_image(mask_path),
            ("sampler", "seed"): seed + 1000 * (attempt + 1),
            ("sampler", "denoise"): recipe.get("seam_denoise", 0.45),
        })
        redrawn = _image_from(client, client.run(workflow))
        score = imagelib.seam_score(redrawn)
        if best_score is None or score < best_score:
            best, best_score = redrawn, score
        if best_score <= imagelib.SEAM_LIMIT:
            break
    return best, best_score


def make_image(client, name, recipe, seed, out_dir):
    import imagelib

    width, height = recipe.get("size", (1024, 1024))
    workflow = comfy.with_inputs(comfy.load_workflow(IMAGE_WORKFLOW), {
        ("prompt", "text"): recipe["prompt"],
        ("canvas", "width"): width,
        ("canvas", "height"): height,
        ("sampler", "seed"): seed,
        ("sampler", "steps"): recipe.get("steps", 8),
    })
    image = _image_from(client, client.run(workflow))
    score = None
    if recipe.get("flatten"):
        image = imagelib.flatten(image)
    if recipe.get("tile") == "redraw":
        work_dir = os.path.join(out_dir, "_work")
        os.makedirs(work_dir, exist_ok=True)
        image, score = _redraw_seams(client, name, recipe, seed, image, work_dir)
    elif recipe.get("tile"):
        band = recipe.get("seam_band", image.width // 4)
        image = imagelib.blend_seams(image, band, band // 2)
        score = imagelib.seam_score(image)
    image = imagelib.finish(image, recipe.get("finish", []))
    path = os.path.join(out_dir, name + ".png")
    image.save(path, optimize=True)
    seam = "" if score is None else f", cuciture {score:.2f}"
    print(f"[comfy] {name}.png {image.width}x{image.height} (seme {seed}{seam})")
    return path, score


def check(client):
    print(f"[comfy] ComfyUI {client.version()} su {client.base}")
    ok = True
    for workflow in (SOUND_WORKFLOW, MODEL_WORKFLOW, IMAGE_WORKFLOW, SEAM_WORKFLOW):
        missing = client.missing_models(comfy.load_workflow(workflow))
        print(f"[comfy] {workflow}: " + ("pronto" if not missing else "mancano " + ", ".join(missing)))
        ok = ok and not missing
    return ok


def main():
    parser = argparse.ArgumentParser(prog="make.py", description="Genera suoni, modelli e immagini con ComfyUI.")
    parser.add_argument("names", nargs="*", help="nomi dei suoni, dei modelli o delle immagini")
    parser.add_argument("--out", help="cartella di uscita (suoni: Audio/SFX/Generated, modelli: Tools/ComfyUI/out, "
                                      "immagini: Tools/ComfyUI/out/images)")
    parser.add_argument("--page", action="store_true", help="scrive anche la pagina di ascolto o di confronto")
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
        image_modules = list(recipes("images"))
        known = ({n for m in sound_modules for n in m.SOUNDS} | {n for m in model_modules for n in m.MODELS}
                 | {n for m in image_modules for n in m.IMAGES})
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
        images = []
        for module in image_modules:
            for name, recipe in module.IMAGES.items():
                if name not in args.names:
                    continue
                out_dir = args.out or os.path.join(OUT, "images")
                os.makedirs(out_dir, exist_ok=True)
                takes = []
                for seed in seeds(recipe, args.takes):
                    path, score = make_image(client, take_name(name, seed, args.takes), recipe, seed, out_dir)
                    takes.append((path, seed, score))
                images.append((module, name, recipe, takes))
        if args.page and made:
            import preview
            print(f"[comfy] pagina di ascolto: {preview.write_page(os.path.dirname(made[0][1][0]), made)}")
        if args.page and images:
            import imagelib
            import imagepage
            out_dir = os.path.dirname(images[0][3][0][0])
            print(f"[comfy] pagina di confronto: {imagepage.write_page(out_dir, images, imagelib.SEAM_LIMIT)}")
    except comfy.ComfyError as e:
        sys.exit(f"[comfy] {e}")


if __name__ == "__main__":
    main()
