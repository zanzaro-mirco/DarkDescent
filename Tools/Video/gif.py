"""Le GIF corte del README dai video dei filmati (M19, D12): qualche scena presa dal video MP4 di
PhotoTour, rimpicciolita, con una tavolozza per ogni scena. Dentro la scena i fotogrammi hanno gli
stessi colori e la GIF li comprime per differenza; tra una scena e l'altra la tavolozza cambia, così
la luce blu del portale e il titolo all'alba non fanno bande.

    python gif.py <video> <gif> <larghezza> <fps> <da-a> [<da-a> ...]

Le scene sono in secondi del video, per esempio 6.5-10.0. GIF_COLORS cambia i colori di ogni scena
(128 senza). Va lanciato con il Python di ComfyUI, che ha Pillow e PyAV:

    python gif.py Recordings/Photos/opening.mp4 docs/media/m19_opening.gif 560 10 6.5-10.0 14.0-17.5 42.5-45.5
    python gif.py Recordings/Photos/victory.mp4 docs/media/m19_victory.gif 560 10 1.0-4.5 10.0-14.0 24.0-28.0
"""
import os
import sys

import av
from PIL import Image


def main():
    src, out, width, fps = sys.argv[1], sys.argv[2], int(sys.argv[3]), float(sys.argv[4])
    spans = [tuple(map(float, s.split('-'))) for s in sys.argv[5:]]
    colors = int(os.environ.get('GIF_COLORS', '128'))

    # i fotogrammi più vicini agli istanti voluti, uno ogni 1/fps secondi dentro ogni scena
    container = av.open(src)
    video = container.streams.video[0]
    want = [a + i / fps for a, b in spans for i in range(int((b - a) * fps))]
    frames, k = [], 0
    for frame in container.decode(video):
        t = float(frame.pts * video.time_base)
        while k < len(want) and t + 1e-6 >= want[k]:
            if t - want[k] < 0.05:
                image = frame.to_image()
                frames.append(image.resize((width, round(width * image.height / image.width)), Image.LANCZOS))
            k += 1
        if k >= len(want):
            break

    paletted, start = [], 0
    for a, b in spans:
        count = int((b - a) * fps)
        scene = frames[start:start + count]
        start += count
        sample = scene[::max(1, len(scene) // 8)]
        mosaic = Image.new('RGB', (width, sample[0].height * len(sample)))
        for i, image in enumerate(sample):
            mosaic.paste(image, (0, i * sample[0].height))
        palette = mosaic.quantize(colors=colors, method=Image.Quantize.MEDIANCUT)
        paletted += [image.quantize(palette=palette, dither=Image.Dither.NONE) for image in scene]

    paletted[0].save(out, save_all=True, append_images=paletted[1:], duration=round(1000 / fps), loop=0, optimize=True)
    print(out, len(paletted), 'fotogrammi', round(os.path.getsize(out) / 1e6, 2), 'MB')


if __name__ == '__main__':
    main()
