"""Test della finitura delle immagini (M17, D8 e D13): python -m unittest Tools/ComfyUI/test_imagelib.py"""
import math
import os
import random
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image  # noqa: E402

import imagelib  # noqa: E402


def periodic(size):
    """Un'immagine che si ripete: onde con un numero intero di periodi su ogni lato."""
    image = Image.new("RGB", (size, size))
    px = image.load()
    for y in range(size):
        for x in range(size):
            v = 128 + 60 * math.sin(2 * math.pi * 3 * x / size) + 50 * math.cos(2 * math.pi * 2 * y / size)
            px[x, y] = (int(v), int(v * 0.8), int(v * 0.6))
    return image


def gradient(size):
    """Un'immagine che non si ripete: una rampa, con un salto netto tra i bordi opposti."""
    image = Image.new("RGB", (size, size))
    px = image.load()
    rnd = random.Random(7)
    for y in range(size):
        for x in range(size):
            v = int(255 * x / size) // 2 + int(255 * y / size) // 2 + rnd.randint(-3, 3)
            px[x, y] = (max(0, min(255, v)),) * 3
    return image


class SeamTests(unittest.TestCase):
    def test_periodic_image_has_no_seams(self):
        self.assertLess(imagelib.seam_score(periodic(128)), imagelib.SEAM_LIMIT)

    def test_ramp_has_seams(self):
        self.assertGreater(imagelib.seam_score(gradient(128)), imagelib.SEAM_LIMIT * 3)

    def test_a_seam_moved_to_the_middle_still_counts(self):
        self.assertGreater(imagelib.seam_score(imagelib.offset_half(gradient(128))), imagelib.SEAM_LIMIT * 3)

    def test_offset_moves_the_seam_inside(self):
        # spostata di mezzo lato, la rampa si ripete ai bordi; il salto è finito al centro
        shifted = imagelib.offset_half(periodic(128))
        self.assertEqual(shifted.size, (128, 128))
        self.assertEqual(shifted.getpixel((64, 64)), periodic(128).getpixel((0, 0)))

    def test_blend_makes_a_noisy_texture_repeat(self):
        rnd = random.Random(3)
        noise = Image.new("RGB", (128, 128))
        for y in range(128):
            for x in range(128):
                v = 90 + rnd.randint(-12, 12) + x // 4
                noise.putpixel((x, y), (v, v, v))
        self.assertGreater(imagelib.seam_score(noise), imagelib.SEAM_LIMIT)
        self.assertLess(imagelib.seam_score(imagelib.blend_seams(noise, 32, 12)), imagelib.SEAM_LIMIT)

    def test_cross_mask_covers_the_center_only(self):
        mask = imagelib.cross_mask((128, 128), 16, 0)
        self.assertEqual(mask.getpixel((64, 10)), 255)
        self.assertEqual(mask.getpixel((10, 64)), 255)
        self.assertEqual(mask.getpixel((10, 10)), 0)


class FinishTests(unittest.TestCase):
    def test_key_out_removes_the_background_and_its_halo(self):
        image = Image.new("RGB", (32, 32), (0, 255, 0))
        for x in range(8, 24):
            for y in range(8, 24):
                image.putpixel((x, y), (200, 40, 40))
        # un bordo mescolato a metà con il fondo
        image.putpixel((7, 16), (100, 148, 20))
        out = imagelib.key_out(image, "#00ff00")
        self.assertEqual(out.getpixel((0, 0))[3], 0)
        self.assertEqual(out.getpixel((16, 16))[3], 255)
        r, g, b, a = out.getpixel((7, 16))
        self.assertTrue(0 < a < 255)
        self.assertLessEqual(g, max(r, b), "il verde del fondo è tolto dal bordo")

    def test_auto_key_reads_the_background_from_the_border(self):
        image = Image.new("RGB", (40, 40), (120, 0, 70))
        for x in range(10, 30):
            for y in range(10, 30):
                image.putpixel((x, y), (40, 40, 40))
        self.assertEqual(imagelib.border_color(image), (120, 0, 70))
        out = imagelib.key_out(image, "auto")
        self.assertEqual(out.getpixel((2, 2))[3], 0)
        self.assertEqual(out.getpixel((20, 20)), (40, 40, 40, 255))

    def test_key_out_keeps_dark_greys_opaque(self):
        # il ferro scuro su un fondo magenta scuro: resta pieno e grigio
        image = Image.new("RGB", (8, 8), (110, 0, 90))
        image.putpixel((4, 4), (30, 32, 30))
        out = imagelib.key_out(image, "auto")
        self.assertEqual(out.getpixel((4, 4))[3], 255)
        self.assertEqual(out.getpixel((0, 0))[3], 0)

    def test_grey_background_is_refused(self):
        with self.assertRaises(ValueError):
            imagelib.key_out(Image.new("RGB", (4, 4), (90, 90, 90)), "auto")

    def test_flatten_removes_the_light_in_the_middle(self):
        image = Image.new("RGB", (64, 64))
        for y in range(64):
            for x in range(64):
                v = 60 + int(60 * (1 - ((x - 32) ** 2 + (y - 32) ** 2) / 2048))
                image.putpixel((x, y), (v, v, v))
        self.assertGreater(imagelib.seam_score(image), imagelib.SEAM_LIMIT)
        flat = imagelib.flatten(image)
        center, corner = flat.getpixel((32, 32))[0], flat.getpixel((2, 2))[0]
        self.assertLess(abs(center - corner), 20)

    def test_trim_and_resize(self):
        image = Image.new("RGBA", (100, 60), (0, 0, 0, 0))
        for x in range(40, 60):
            for y in range(20, 40):
                image.putpixel((x, y), (255, 255, 255, 255))
        out = imagelib.finish(image, [("trim", 2), ("resize", 48)])
        self.assertEqual(out.size, (48, 48))

    def test_nine_slice_keeps_the_corners_and_drops_the_middle(self):
        image = Image.new("RGBA", (200, 200), (0, 0, 0, 0))
        image.putpixel((3, 3), (255, 0, 0, 255))        # nell'angolo
        image.putpixel((100, 2), (0, 255, 0, 255))      # la decorazione a metà lato
        out = imagelib.nine_slice(image, 40, 16)
        self.assertEqual(out.size, (96, 96))
        self.assertEqual(out.getpixel((3, 3)), (255, 0, 0, 255))
        self.assertNotIn((0, 255, 0, 255), list(out.getdata()))

    def test_button_plate_has_the_border_lines(self):
        plate = imagelib.button_plate(Image.new("RGB", (64, 64), (40, 40, 44)), 32, 4)
        self.assertEqual(plate.size, (32, 32))
        self.assertEqual(plate.getpixel((1, 16))[:3], (150, 122, 70))

    def test_fade_clears_the_border_and_keeps_the_middle(self):
        faded = imagelib.fade(Image.new("RGBA", (64, 64), (90, 80, 70, 255)), 0.5)
        self.assertEqual(255, faded.getpixel((32, 32))[3])
        self.assertEqual(0, faded.getpixel((0, 0))[3], "gli angoli spariscono")
        self.assertLess(faded.getpixel((63, 32))[3], 20, "il bordo quasi")
        self.assertEqual((90, 80, 70), faded.getpixel((0, 0))[:3], "il colore resta, cambia solo la trasparenza")

    def test_opacity_scales_the_alpha_only(self):
        faint = imagelib.opacity(Image.new("RGBA", (4, 4), (10, 20, 30, 200)), 0.5)
        self.assertEqual((10, 20, 30, 100), faint.getpixel((1, 1)))

    def test_fit_centers_on_a_square(self):
        fitted = imagelib.fit(Image.new("RGBA", (100, 50), (200, 0, 0, 255)), 32)
        self.assertEqual((32, 32), fitted.size)
        self.assertEqual(0, fitted.getpixel((16, 2))[3], "sopra e sotto vuoto")
        self.assertEqual(255, fitted.getpixel((16, 16))[3])

    def test_atlas_places_the_tiles_row_by_row(self):
        tiles = [Image.new("RGBA", (40, 40), (i * 40, 0, 0, 255)) for i in range(5)]
        sheet = imagelib.atlas(tiles, 4, 16)
        self.assertEqual((64, 32), sheet.size)
        self.assertEqual(120, sheet.getpixel((3 * 16 + 8, 8))[0], "la quarta nella prima riga")
        self.assertEqual(160, sheet.getpixel((8, 16 + 8))[0], "la quinta all'inizio della seconda")
        self.assertEqual(0, sheet.getpixel((16 + 8, 16 + 8))[3], "le caselle in più restano vuote")

    def test_unknown_step_fails(self):
        with self.assertRaises(ValueError):
            imagelib.finish(Image.new("RGB", (4, 4)), [("blur", 2)])


if __name__ == "__main__":
    unittest.main()
