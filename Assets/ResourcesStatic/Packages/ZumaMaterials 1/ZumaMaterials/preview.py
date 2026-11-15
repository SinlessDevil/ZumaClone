"""Render comparisons from the exported PNGs. The board previews use example paths."""

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import map_coordinates
from scipy.spatial import cKDTree

from decals import make_masks
from generate import ROOT, smoothstep, to_byte


def read(folder, name):
    return np.asarray(Image.open(folder / f"{name}.png"), dtype=np.float32) / 255


def shade(base, normal, ao):
    # A movable directional light in the preview. Source PNGs contain no light pass.
    light = np.array([-0.52, 0.49, 0.70], dtype=np.float32)
    diffuse = np.maximum(0, normal @ light)
    intensity = (0.55 + 0.62 * diffuse) * (0.75 + 0.25 * ao)
    return np.clip(base * intensity[..., None], 0, 1)


def tile(folder, size, lit=True):
    base = read(folder, "BaseColor")
    normals = read(folder, "Normal")
    data = shade(base, 2 * normals - 1, read(folder, "AO")) if lit else normals
    return Image.fromarray(to_byte(data)).resize(size, Image.Resampling.LANCZOS)


def sample(data, u, v):
    h, w = data.shape[:2]
    coords = [v * h - 0.5, u * w - 0.5]
    if data.ndim == 2:
        return map_coordinates(data, coords, order=1, mode="grid-wrap")
    return np.stack([map_coordinates(data[..., i], coords, order=1, mode="grid-wrap")
                     for i in range(data.shape[2])], axis=-1)


def example_path(w, h, layout):
    t = np.linspace(0, 1, 3600)
    if layout == "spiral":
        angle = 0.18 * np.pi + 3.94 * np.pi * t
        radius = 1 - 0.79 * t
        return np.column_stack((w * (0.5 + 0.447 * radius * np.cos(angle)),
                                h * (0.5 + 0.455 * radius * np.sin(angle))))
    turns = 1.5 if layout == "wide" else 1.75
    angle = 0.08 * np.pi + 2 * np.pi * turns * t
    taper = 1 - 0.72 * t
    power = 0.68 if layout == "wide" else 0.90
    cos, sin = np.cos(angle), np.sin(angle)
    return np.column_stack((w * (0.5 + 0.46 * taper * np.sign(cos) * abs(cos) ** power),
                            h * (0.5 + 0.46 * taper * np.sign(sin) * abs(sin) ** power)))


def board(folder, w, h, layout, decoration=None):
    xx, yy = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
    ground = folder / "Ground"
    u, v = xx / (h * 0.67), yy / (h * 0.67)
    base = shade(sample(read(ground, "BaseColor"), u, v),
                 2 * sample(read(ground, "Normal"), u, v) - 1,
                 sample(read(ground, "AO"), u, v))
    if decoration is not None:
        span = h * 0.53
        stencil = read(decoration.parent, decoration.stem)
        coords = [(yy - h * 0.46) / span + 0.5, (xx - w * 0.49) / span + 0.5]
        engraving = map_coordinates(stencil, np.asarray(coords) * len(stencil) - 0.5,
                                    order=1, mode="constant")
        bevel = np.roll(engraving, 1, axis=0) - engraving
        base *= (1 - 0.29 * engraving + 0.30 * bevel)[..., None]
    path = example_path(w, h, layout)
    tangent = np.gradient(path, axis=0)
    tangent /= np.linalg.norm(tangent, axis=-1, keepdims=True)
    distance = np.r_[0, np.cumsum(np.linalg.norm(np.diff(path, axis=0), axis=-1))]
    nearest_distance, ndx = cKDTree(path).query(np.stack((xx, yy), axis=-1), workers=2)
    nearest = path[ndx]
    tx, ty = np.moveaxis(tangent[ndx], -1, 0)
    signed = (xx - nearest[..., 0]) * ty - (yy - nearest[..., 1]) * tx
    half_width = h * 0.028
    track_u = np.clip(0.5 + signed / (2 * half_width), 0.002, 0.998)
    track_v = 1 - distance[ndx] / (4.8 * half_width)
    track = folder / "Track"
    normal = 2 * sample(read(track, "Normal"), track_u, track_v) - 1
    nx, ny, nz = np.moveaxis(normal, -1, 0)
    rotated = np.stack((nx * ty + ny * tx, nx * tx - ny * ty, nz), axis=-1)
    track_color = shade(sample(read(track, "BaseColor"), track_u, track_v), rotated,
                        sample(read(track, "AO"), track_u, track_v))
    coverage = 1 - smoothstep(half_width * 0.89, half_width + 0.3, nearest_distance)
    base *= (1 - 0.18 * np.exp(-((nearest_distance - half_width) / 2.6) ** 2))[..., None]
    image = base * (1 - coverage[..., None]) + track_color * coverage[..., None]
    return Image.fromarray(to_byte(image))


def contact_sheet(materials, output, themes, masks):
    width = 1110
    row_h = 258
    image = Image.new("RGB", (width, 116 + row_h * len(themes)), "#16181b")
    draw = ImageDraw.Draw(image)
    title = ImageFont.load_default(30)
    label = ImageFont.load_default(19)
    note = ImageFont.load_default(13)
    draw.text((27, 22), "ZUMA  /  TEMPLE MATERIALS", font=title, fill="#e9dfcb")
    columns = [(27, "GROUND / 2 x 2"), (248, "TRACK / LIT"), (374, "GROUND NORMAL"),
               (526, "TRACK NORMAL"), (644, "BOARD / EXAMPLE PATH")]
    for left, caption in columns:
        draw.text((left, 82), caption, font=note, fill="#bcb2a2")
    for idx, (name, theme) in enumerate(themes.items()):
        top = 108 + row_h * idx
        folder = materials / name
        draw.text((27, top), theme["label"], font=label, fill="#e9dfcb")
        tile_2x2 = Image.new("RGB", (196, 196))
        unit = tile(folder / "Ground", (98, 98))
        for y in (0, 98):
            for x in (0, 98):
                tile_2x2.paste(unit, (x, y))
        image.paste(tile_2x2, (27, top + 32))
        image.paste(tile(folder / "Track", (99, 196)), (248, top + 32))
        image.paste(tile(folder / "Ground", (128, 128), lit=False), (374, top + 32))
        image.paste(tile(folder / "Track", (90, 196), lit=False), (526, top + 32))
        layout = ("wide", "spiral", "round")[idx % 3]
        image.paste(board(folder, 440, 196, layout, masks / "Rosette.png"), (644, top + 32))
    image.save(output / "ContactSheet.png")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--materials", type=Path, default=ROOT / "Materials")
    parser.add_argument("--output", type=Path, default=ROOT / "Previews")
    parser.add_argument("--theme", action="append")
    parser.add_argument("--without-decals", action="store_true")
    args = parser.parse_args()
    all_themes = json.loads((ROOT / "themes.json").read_text(encoding="utf-8"))
    themes = {key: all_themes[key] for key in (args.theme or all_themes)}
    args.output.mkdir(parents=True, exist_ok=True)
    masks = ROOT / "Decorations"
    make_masks(masks)
    if not args.without_decals:
        contact_sheet(args.materials, args.output, themes, masks)
    for i, name in enumerate(themes):
        decoration = None if args.without_decals else masks / "Rosette.png"
        board(args.materials / name, 900, 625, ("wide", "spiral", "round")[i % 3],
              decoration).save(args.output / f"{name}_Board.png")


if __name__ == "__main__":
    main()
