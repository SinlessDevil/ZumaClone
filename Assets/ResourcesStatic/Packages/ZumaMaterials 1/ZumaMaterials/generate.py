"""Procedural Zuma materials. Python 3.10+, numpy, scipy, Pillow."""

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image
from scipy.ndimage import map_coordinates


ROOT = Path(__file__).resolve().parent
SIZES = (256, 512, 1024, 2048)


def smoothstep(low, high, value):
    t = np.clip((value - low) / (high - low), 0, 1)
    return t * t * (3 - 2 * t)


def grid(size):
    center = (np.arange(size, dtype=np.float32) + 0.5) / size
    return np.meshgrid(center, center)


def noise(x, y, frequency, seed):
    rng = np.random.default_rng(seed)
    fy, fx = (frequency, frequency) if isinstance(frequency, int) else frequency
    lattice = rng.uniform(-1, 1, (fy, fx)).astype(np.float32)
    return map_coordinates(lattice, [y * fy, x * fx], order=3, mode="grid-wrap")


def grain(x, y, size, seed):
    coarse = noise(x, y, 67, seed)
    fine = noise(x, y, min(size // 3, 234), seed + 1)
    return 0.67 * coarse + 0.33 * fine


def cells(x, y, frequency, seed):
    """Periodic distances to the two nearest jittered cell centers."""
    rng = np.random.default_rng(seed)
    offsets = rng.uniform(-0.35, 0.35, (2, frequency, frequency)).astype(np.float32)
    px, py = x * frequency, y * frequency
    ix, iy = np.floor(px).astype(np.int32), np.floor(py).astype(np.int32)
    nearest = np.full_like(px, np.inf)
    second = nearest.copy()
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            row, col = (iy + dy) % frequency, (ix + dx) % frequency
            cx = ix + dx + 0.5 + offsets[0, row, col]
            cy = iy + dy + 0.5 + offsets[1, row, col]
            distance = ((px - cx) ** 2 + (py - cy) ** 2).astype(np.float32)
            second = np.minimum(second, np.maximum(nearest, distance))
            nearest = np.minimum(nearest, distance)
    return np.sqrt(nearest), np.sqrt(second) - np.sqrt(nearest)


def colors(values, palette):
    rgb = np.asarray([[int(color[i:i+2], 16) for i in (0, 2, 4)]
                      for color in palette], dtype=np.float32) / 255
    factor = np.clip(values, 0, 1)[..., None]
    lower = rgb[0] + (rgb[1] - rgb[0]) * np.clip(factor * 2, 0, 1)
    return lower + (rgb[2] - rgb[1]) * np.clip(factor * 2 - 1, 0, 1)


def stone(x, y, size, theme, track=False):
    seed = theme["seed"] + (127 if track else 0)
    broad = noise(x, y, 4, seed)
    patch = noise(x, y, 10, seed + 1)
    mid = noise(x, y, 27, seed + 2)
    fine = grain(x, y, size, seed + 3)
    wx = x + 0.037 * noise(x, y, 6, seed + 5) + 0.008 * mid
    wy = y + 0.037 * noise(x, y, 6, seed + 6) + 0.008 * fine
    style = theme["style"]
    frequency = {"sandstone": 11, "volcanic": 15, "slate": 8, "jade": 6, "bronze": 9}[style]
    _, boundaries = cells(wx, wy, frequency, seed + 8)
    crack_mask = smoothstep(-0.4, 0.18, patch + 0.5 * mid + theme["cracks"] - 0.58)
    cracks = np.exp(-boundaries / 0.060) * crack_mask * theme["cracks"]
    pits = smoothstep(0.26, 0.86, -noise(wx, wy, 46, seed + 9)) ** 2
    strata = np.zeros_like(broad)
    local_color = np.zeros_like(broad)
    if style == "slate":
        phase = 2 * np.pi * (16 * wy + 3 * wx + 1.5 * broad + 0.45 * patch)
        strata = 0.52 * np.sin(phase) + 0.20 * np.sin(2 * phase + 1.4 * mid)
        local_color = 0.11 * strata
    elif style == "volcanic":
        pits = np.maximum(pits, smoothstep(0.16, 0.78, -mid) ** 2 * 0.7)
        local_color = 0.08 * broad - 0.07 * pits
    elif style == "jade":
        cracks = np.maximum(cracks, 0.28 * np.exp(-boundaries / 0.11))
        local_color = -0.14 * smoothstep(0.02, 0.70, patch + 0.5 * mid)
    elif style == "bronze":
        strata = 0.34 * np.sin(2 * np.pi * (9 * wy + 6 * wx + broad))
        local_color = 0.11 * patch + 0.07 * strata
    else:
        strata = 0.19 * np.sin(2 * np.pi * (12 * wy + wx + 0.8 * broad))

    detail = theme["detail"] * (0.67 if track else 1.0)
    # Height units are relative to one repeat. Color contains pigment and weathering.
    height = detail * (0.007 * broad + 0.0045 * patch + 0.0018 * mid
                       + 0.00085 * fine + 0.0036 * strata - 0.0048 * cracks - 0.003 * pits)
    value = 0.54 + 0.21 * broad + 0.12 * patch + 0.08 * mid + 0.065 * fine
    value += local_color - 0.18 * cracks - 0.10 * pits
    rough = np.clip(theme["roughness"] + 0.05 * mid + 0.07 * pits, 0, 1)
    ao = np.clip(1.0 - 0.50 * cracks - 0.25 * pits, 0, 1)

    if track:
        across = np.abs(2 * x - 1)
        edge = theme["edge"]
        wall = smoothstep(0.70 - edge, 0.82, across)
        lip = np.exp(-0.5 * ((across - 0.87) / (edge * 0.28)) ** 2)
        outside = smoothstep(0.91, 1.0, across)
        channels = np.exp(-0.5 * ((across - 0.65) / 0.024) ** 2)
        chips = smoothstep(0.20, 0.78, mid + 0.40 * patch) * lip
        accent = theme["ornaments"]
        height += theme["depth"] * (0.67 * wall + 0.24 * lip - 0.13 * outside
                                    - 0.045 * channels * accent - 0.10 * chips)
        value = 0.45 + 0.75 * (value - 0.54) + 0.32 * lip + 0.15 * wall
        value -= 0.09 * channels * accent + 0.11 * chips + 0.17 * outside
        rough = np.clip(rough - 0.12 * (1 - wall) - 0.18 * lip, 0, 1)
        ao = np.clip(ao - 0.25 * np.exp(-0.5 * ((across - 0.69) / 0.11) ** 2), 0, 1)
    return colors(value, theme["track"] if track else theme["palette"]), height, rough, ao


def surface_normal(height, width=1, track=False):
    size = len(height)
    dx = np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)
    if track:
        dx[:, 0] = 2 * (height[:, 1] - height[:, 0])
        dx[:, -1] = 2 * (height[:, -1] - height[:, -2])
    dy = np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)
    # PNG rows run down while tangent-space +V runs up.
    normal = np.stack((-dx * size / (2 * width), dy * size / 2, np.ones_like(height)), axis=-1)
    return normal / np.linalg.norm(normal, axis=-1, keepdims=True)


def to_byte(values):
    return np.rint(np.clip(values, 0, 1) * 255).astype(np.uint8)


def material_maps(size, theme, track):
    x, y = grid(size)
    base, height, rough, ao = stone(x, y, size, theme, track)
    normal = surface_normal(height, theme["width"] if track else 1, track)
    roughness = to_byte(rough)
    packed = np.zeros((size, size, 4), dtype=np.uint8)
    packed[..., 3] = 255 - roughness
    return {
        "BaseColor": to_byte(base),
        "Normal": to_byte(normal * 0.5 + 0.5),
        "Height": np.rint(np.clip(0.40 + 12 * height, 0, 1) * 65535).astype(np.uint16),
        "Roughness": roughness,
        "AO": to_byte(ao),
        "MetallicSmoothness": packed,
    }


def seam_ratio(pixels, axis):
    pixels = pixels.astype(np.float32)
    crossing = np.take(pixels, 0, axis=axis) - np.take(pixels, -1, axis=axis)
    neighbors = np.diff(pixels, axis=axis)
    scale = max(np.sqrt(np.mean(neighbors ** 2)), 0.25)
    return float(np.sqrt(np.mean(crossing ** 2)) / scale)


def verify(maps, track):
    axes = (0,) if track else (0, 1)
    ratios = {name: [round(seam_ratio(data, axis), 3) for axis in axes]
              for name, data in maps.items()}
    if max(max(row) for row in ratios.values()) > 1.75:
        raise ValueError(f"Abnormal edge discontinuity: {ratios}")
    normal = maps["Normal"].astype(np.float32) / 127.5 - 1
    if float(np.max(np.abs(np.linalg.norm(normal, axis=-1) - 1))) > 0.008:
        raise ValueError("Normal map is outside unit-vector quantization tolerance")
    if not np.array_equal(maps["MetallicSmoothness"][..., 3], 255 - maps["Roughness"]):
        raise ValueError("Packed smoothness does not match roughness")
    return ratios


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--themes", type=Path, default=ROOT / "themes.json")
    parser.add_argument("--theme", action="append", help="Theme name, repeat for a subset")
    parser.add_argument("--size", type=int, choices=SIZES, default=1024)
    parser.add_argument("--seed", type=int, help="Override configured seed")
    parser.add_argument("--output", type=Path, default=ROOT / "Materials")
    args = parser.parse_args()
    themes = json.loads(args.themes.read_text(encoding="utf-8"))
    unknown = set(args.theme or ()) - themes.keys()
    if unknown:
        parser.error(f"Unknown theme: {', '.join(sorted(unknown))}")
    report = {}
    for key in args.theme or themes:
        theme = themes[key].copy()
        if args.seed is not None:
            theme["seed"] = args.seed
        report[key] = {}
        for kind in ("Ground", "Track"):
            track = kind == "Track"
            maps = material_maps(args.size, theme, track)
            report[key][kind] = verify(maps, track)
            folder = args.output / key / kind
            folder.mkdir(parents=True, exist_ok=True)
            for name, data in maps.items():
                Image.fromarray(data).save(folder / f"{name}.png", compress_level=4)
            print(f"{key}/{kind} {args.size}x{args.size}", flush=True)
    (args.output / "SeamCheck.json").write_text(
        json.dumps(report, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
