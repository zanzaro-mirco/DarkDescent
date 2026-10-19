"""Genera i suoni fatti da script (M13, D14).

    python Tools/Audio/make.py                      tutti, in Assets/_Project/Audio/SFX/Generated
    python Tools/Audio/make.py well_drink           solo quelli nominati
    python Tools/Audio/make.py --out DIR --page     in DIR, con la pagina di ascolto ascolto.html

Ogni modulo in sounds/ dichiara SOUNDS (nome del WAV -> funzione che riceve il generatore), TITLE,
DESCRIPTION, COMPARE (etichetta e percorso sotto Audio/SFX del suono più vicino che il gioco ha già)
e MAX_DUR. Il seme di ogni suono viene dal suo nome (sfxlib.rng_for).
"""
import argparse
import importlib
import os
import pkgutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import sfxlib  # noqa: E402
import preview  # noqa: E402


def modules():
    path = os.path.join(HERE, "sounds")
    for info in sorted(pkgutil.iter_modules([path]), key=lambda m: m.name):
        yield importlib.import_module("sounds." + info.name)


def main():
    parser = argparse.ArgumentParser(description="Genera i suoni fatti da script.")
    parser.add_argument("names", nargs="*", help="nomi dei WAV (senza estensione); nessuno = tutti")
    parser.add_argument("--out", default=sfxlib.GENERATED, help="cartella di uscita")
    parser.add_argument("--page", action="store_true", help="scrive anche la pagina di ascolto")
    args = parser.parse_args()

    os.makedirs(args.out, exist_ok=True)
    made = []
    for module in modules():
        files = []
        for name, build in module.SOUNDS.items():
            if args.names and name not in args.names:
                continue
            x = sfxlib.finish(build(sfxlib.rng_for(name)), max_dur=module.MAX_DUR)
            path = os.path.join(args.out, name + ".wav")
            sfxlib.write_wav(path, x)
            print(f"[audio] {name}.wav {len(x) / sfxlib.SR:.2f} s")
            files.append(path)
        if files:
            made.append((module, files))

    unknown = set(args.names) - {n for m in modules() for n in m.SOUNDS}
    if unknown:
        sys.exit("suoni sconosciuti: " + ", ".join(sorted(unknown)))
    if args.page:
        page = preview.write_page(args.out, made)
        print(f"[audio] pagina di ascolto: {page}")


if __name__ == "__main__":
    main()
