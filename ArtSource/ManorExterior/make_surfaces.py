"""Generate tileable brick and slate textures for the exterior mesh UVs."""
from pathlib import Path
from random import Random

from PIL import Image


output = Path(__file__).resolve().parents[2] / 'Assets/Props/ManorExterior'
output.mkdir(parents=True, exist_ok=True)
size = 512
rng = Random(1887)

for filename, columns, courses, base, joint in (
    ('Weathered brick', 8, 20, (119, 78, 61), (79, 72, 66)),
    ('Weathered slate', 8, 10, (72, 81, 88), (43, 48, 53)),
):
    tones = [[rng.uniform(.79, 1.21) for _ in range(columns)] for _ in range(courses)]
    image = Image.new('RGB', (size, size))
    pixels = image.load()
    for y in range(size):
        row = (y * courses) // size
        v = (y * courses) % size
        for x in range(size):
            shift = x * columns + (size // 2 if row % 2 else 0)
            cell, u = (shift // size) % columns, shift % size
            seam = v < 33 or u < 16
            bevel = -.14 if v < 67 else .06 if v > 473 else 0
            scale = 1 if seam else tones[row][cell] + bevel
            rgb = joint if seam else base
            grain = rng.randrange(-10, 11)
            pixels[x, y] = tuple(max(0, min(255, round(channel * scale) + grain)) for channel in rgb)
    image.save(output / (filename + '.png'))
