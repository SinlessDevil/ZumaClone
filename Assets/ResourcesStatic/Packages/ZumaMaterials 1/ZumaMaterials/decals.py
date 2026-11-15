"""Generate standalone 512 px engraving masks for the temple boards."""

from math import cos, pi, sin

from PIL import Image, ImageDraw

from generate import ROOT


SIZE = 1024
CENTER = SIZE // 2


def polar(radius, angle):
    return (CENTER + radius * cos(angle), CENTER + radius * sin(angle))


def make_masks(folder):
    folder.mkdir(parents=True, exist_ok=True)
    names = ("Spiral", "Rosette", "Fracture", "StoneTrim", "Glyphs", "Corner")
    for name in names:
        mask = Image.new("L", (SIZE, SIZE))
        draw = ImageDraw.Draw(mask)
        if name == "Spiral":
            draw.line([polar(26 + t * 1.02, t * 0.035) for t in range(364)],
                      fill=255, width=16, joint="curve")
            draw.arc((72, 72, 952, 952), 40, 328, 215, 10)
        elif name == "Rosette":
            for radius, width in ((368, 12), (320, 8), (103, 13)):
                draw.ellipse((CENTER-radius, CENTER-radius, CENTER+radius, CENTER+radius),
                             outline=255, width=width)
            for i in range(12):
                a = i * 2 * pi / 12
                draw.line([polar(129, a), polar(220, a - 0.16), polar(295, a),
                           polar(220, a + 0.16), polar(129, a)], fill=245, width=14)
        elif name == "Fracture":
            for points, width in (
                ([(85, 35), (260, 240), (388, 254), (423, 410), (597, 490),
                  (680, 650), (778, 701), (905, 969)], 13),
                ([(423, 410), (305, 463), (254, 626), (109, 707)], 9),
                ([(680, 650), (738, 515), (859, 462), (921, 333)], 7),
                ([(260, 240), (316, 142), (458, 59)], 5)):
                draw.line(points, fill=255, width=width, joint="curve")
        elif name == "StoneTrim":
            for y in (231, 263, 758, 790):
                draw.line([(0, y), (SIZE, y)], fill=255, width=14)
            for x in range(0, SIZE, 128):
                draw.line([(x, 369), (x+101, 369), (x+101, 666), (x+23, 666),
                           (x+23, 446), (x+74, 446), (x+74, 580), (x+47, 580)],
                          fill=255, width=12, joint="curve")
        elif name == "Glyphs":
            for x, y, angle in ((257, 257, 0), (769, 257, pi / 2),
                                (257, 769, pi), (769, 769, 3 * pi / 2)):
                outline = [(0, -131), (101, -30), (101, 94), (0, 138),
                           (-101, 94), (-101, -30), (0, -131)]
                points = [(x + a*cos(angle) - b*sin(angle), y + a*sin(angle) + b*cos(angle))
                          for a, b in outline]
                draw.line(points, fill=255, width=14, joint="curve")
                draw.ellipse((x-42, y-42, x+42, y+42), outline=246, width=13)
        else:
            for i in range(4):
                start = 69 + i * 56
                stop = 945 - i * 103
                draw.line([(start, stop), (start, start + 141), (start + 141, start),
                           (stop, start), (stop, start + 44)], fill=255, width=15,
                          joint="curve")
        mask.resize((512, 512), Image.Resampling.LANCZOS).save(folder / f"{name}.png")


if __name__ == "__main__":
    make_masks(ROOT / "Decorations")
