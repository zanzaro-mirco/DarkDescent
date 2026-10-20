"""La statua del guardiano rifatta da TRELLIS.2, partendo dalla foto della versione da script
(refs/statue.png, scritta da reference.py): la prova di un modello organico del passo 13.4."""

COMPARE = "DarkDescent/Statue.fbx"
PHOTO = {"target": (0.0, 0.0, 1.6), "ortho": 6.0}
MODELS = {
    "Statue_Gen": {"image": "refs/statue.png", "seed": 1401, "faces": 8000, "texture": 1024},
}
