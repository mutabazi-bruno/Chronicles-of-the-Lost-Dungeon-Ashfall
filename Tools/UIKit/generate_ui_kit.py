"""
Generates the Ashfall UI kit.

The project already ships a button pack drawn as matte black plates with a rough,
chalky hand-drawn border. Nothing in the project draws panels, frames or slots in
that style, so the objectives box and the level complete card fall back to default
flat rectangles that clash with everything else.

This script draws the missing pieces in the same language: black fill, jittered
chalk border, one accent colour. Everything is exported as a 9-slice friendly PNG
so a single sprite stretches to any size without distorting its corners.

Re-run it to change the palette or the roughness. Output goes to Assets/UI/Generated.
"""

import math
import os
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = os.environ.get("UI_KIT_OUT", "out")

# Sampled from the existing Menu Buttons pack so the new art sits beside it.
BONE = (232, 228, 220, 255)
CRIMSON = (200, 35, 43, 255)
GOLD = (206, 160, 74, 255)
PLATE = (10, 8, 9, 235)
PLATE_SOLID = (10, 8, 9, 255)

SEED = 20260906


def chalk_edge(size, inset, thickness, jitter, seed, closed=True, corner=0):
    """A rough outline that wobbles like a drawn line.

    corner rounds the turns so the stroke agrees with the rounded plate underneath.
    Left at 0 it draws a hard rectangle, which is what the bars and divider want.
    """
    rng = random.Random(seed)
    w, h = size
    layer = Image.new("L", (w * 2, h * 2), 0)
    draw = ImageDraw.Draw(layer)

    x0, y0 = inset * 2, inset * 2
    x1, y1 = (w - inset) * 2, (h - inset) * 2
    r = corner * 2

    def wobble_line(ax, ay, bx, by):
        span = math.hypot(bx - ax, by - ay)
        steps = max(int(span / 6), 8)
        pts = []
        for i in range(steps + 1):
            t = i / steps
            px = ax + (bx - ax) * t
            py = ay + (by - ay) * t
            # push the point sideways a little, more in the middle than the ends
            falloff = math.sin(math.pi * t)
            px += rng.uniform(-jitter, jitter) * 2 * falloff
            py += rng.uniform(-jitter, jitter) * 2 * falloff
            pts.append((px, py))
        for i in range(len(pts) - 1):
            wob = thickness * 2 * rng.uniform(0.72, 1.28)
            draw.line([pts[i], pts[i + 1]], fill=255, width=max(int(wob), 2))

    def wobble_arc(cx, cy, start_deg, end_deg):
        steps = max(int(r / 3), 6)
        pts = []
        for i in range(steps + 1):
            a = math.radians(start_deg + (end_deg - start_deg) * i / steps)
            pts.append(
                (
                    cx + math.cos(a) * (r + rng.uniform(-jitter, jitter)),
                    cy + math.sin(a) * (r + rng.uniform(-jitter, jitter)),
                )
            )
        for i in range(len(pts) - 1):
            wob = thickness * 2 * rng.uniform(0.72, 1.28)
            draw.line([pts[i], pts[i + 1]], fill=255, width=max(int(wob), 2))

    wobble_line(x0 + r, y0, x1 - r, y0)
    wobble_line(x1, y0 + r, x1, y1 - r)
    if r:
        wobble_arc(x1 - r, y0 + r, -90, 0)
    if closed:
        wobble_line(x1 - r, y1, x0 + r, y1)
        wobble_line(x0, y1 - r, x0, y0 + r)
        if r:
            wobble_arc(x1 - r, y1 - r, 0, 90)
            wobble_arc(x0 + r, y1 - r, 90, 180)
            wobble_arc(x0 + r, y0 + r, 180, 270)

    layer = layer.resize((w, h), Image.LANCZOS)
    return layer


def erode(mask, seed, amount=0.30):
    """Bite holes out of a stroke so it reads as chalk rather than vector."""
    rng = np.random.default_rng(seed)
    arr = np.asarray(mask).astype(np.float32) / 255.0
    noise = rng.random(arr.shape)
    noise = np.asarray(
        Image.fromarray((noise * 255).astype(np.uint8)).filter(
            ImageFilter.GaussianBlur(1.1)
        )
    ).astype(np.float32) / 255.0
    keep = np.clip((noise - amount) / max(1e-5, 1 - amount), 0, 1)
    out = np.clip(arr * (0.55 + 0.75 * keep), 0, 1)
    return Image.fromarray((out * 255).astype(np.uint8))


def tint(mask, colour):
    img = Image.new("RGBA", mask.size, colour[:3] + (0,))
    img.putalpha(mask)
    return img


def panel(size, accent, corner=18, border=7, fill=PLATE, seed=SEED, double=True):
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))

    body = Image.new("L", size, 0)
    ImageDraw.Draw(body).rounded_rectangle(
        [border // 2, border // 2, w - border // 2 - 1, h - border // 2 - 1],
        radius=corner,
        fill=255,
    )
    plate = Image.new("RGBA", size, fill)
    plate.putalpha(body)
    img = Image.alpha_composite(img, plate)

    outer = erode(chalk_edge(size, border // 2, border, 1.7, seed, corner=corner), seed)
    img = Image.alpha_composite(img, tint(outer, accent))

    if double:
        inner = erode(
            chalk_edge(size, border + 7, max(border - 4, 2), 1.2, seed + 91, corner=max(corner - 6, 0)),
            seed + 91,
            amount=0.45,
        )
        faded = tint(inner, accent)
        faded.putalpha(faded.getchannel("A").point(lambda v: int(v * 0.45)))
        img = Image.alpha_composite(img, faded)

    return img


def bar_frame(size, accent, seed=SEED):
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    body = Image.new("L", size, 0)
    ImageDraw.Draw(body).rectangle([3, 3, w - 4, h - 4], fill=255)
    plate = Image.new("RGBA", size, (6, 5, 6, 245))
    plate.putalpha(body)
    img = Image.alpha_composite(img, plate)
    edge = erode(chalk_edge(size, 3, 5, 1.0, seed), seed, amount=0.22)
    return Image.alpha_composite(img, tint(edge, accent))


def bar_fill(size, top, bottom):
    w, h = size
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    for y in range(h):
        t = y / max(h - 1, 1)
        # a soft sheen through the middle so the bar does not read as flat colour
        sheen = 1.0 - abs(t - 0.38) * 0.85
        for c in range(3):
            arr[y, :, c] = int(np.clip(top[c] * sheen + bottom[c] * (1 - sheen), 0, 255))
        arr[y, :, 3] = 255
    return Image.fromarray(arr, "RGBA")


def slot(size, accent, seed=SEED):
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    body = Image.new("L", size, 0)
    ImageDraw.Draw(body).rounded_rectangle([4, 4, w - 5, h - 5], radius=10, fill=255)
    plate = Image.new("RGBA", size, (12, 10, 11, 225))
    plate.putalpha(body)
    img = Image.alpha_composite(img, plate)
    edge = erode(chalk_edge(size, 4, 6, 1.3, seed, corner=9), seed, amount=0.28)
    return Image.alpha_composite(img, tint(edge, accent))


def divider(size, accent, seed=SEED):
    w, h = size
    line = erode(chalk_edge((w, h), 2, 4, 1.4, seed, closed=False), seed, amount=0.34)
    # keep only the top run, the wobble line already drew it
    return tint(line, accent)


def icon_lock(size=(96, 96), accent=BONE, seed=SEED + 41):
    """A padlock, drawn with the same wobble as the panels so it belongs to them."""
    w, h = size
    layer = Image.new("L", (w * 2, h * 2), 0)
    d = ImageDraw.Draw(layer)
    rng = random.Random(seed)

    # shackle
    cx, cy, r = w, int(h * 0.78), int(w * 0.40)
    pts = []
    for i in range(41):
        a = math.radians(180 + 180 * i / 40)
        pts.append((cx + math.cos(a) * (r + rng.uniform(-2, 2)),
                    cy + math.sin(a) * (r + rng.uniform(-2, 2))))
    for i in range(len(pts) - 1):
        d.line([pts[i], pts[i + 1]], fill=255, width=int(w * 0.20))

    # body
    bw, bh = int(w * 1.20), int(h * 0.86)
    bx, by = w - bw // 2, int(h * 0.92)
    d.rounded_rectangle([bx, by, bx + bw, by + bh], radius=int(w * 0.16), fill=255)

    layer = layer.resize(size, Image.LANCZOS)
    body = erode(layer, seed, amount=0.14)

    img = tint(body, accent)

    # keyhole punched back out so it reads at small sizes
    hole = Image.new("L", (w * 2, h * 2), 0)
    hd = ImageDraw.Draw(hole)
    kx, ky = w, int(h * 1.30)
    kr = int(w * 0.15)
    hd.ellipse([kx - kr, ky - kr, kx + kr, ky + kr], fill=255)
    hd.polygon([(kx - kr // 2, ky), (kx + kr // 2, ky), (kx + kr // 3, ky + kr * 2),
                (kx - kr // 3, ky + kr * 2)], fill=255)
    hole = hole.resize(size, Image.LANCZOS)

    alpha = np.asarray(img.getchannel("A")).astype(np.int16)
    alpha = np.clip(alpha - np.asarray(hole).astype(np.int16), 0, 255).astype(np.uint8)
    img.putalpha(Image.fromarray(alpha))
    return img


def icon_check(size=(96, 96), accent=(150, 200, 120, 255), seed=SEED + 47):
    """A tick. The objective list currently asks the font for a Unicode box that
    is not in the atlas, so it renders as nothing."""
    w, h = size
    layer = Image.new("L", (w * 2, h * 2), 0)
    d = ImageDraw.Draw(layer)
    rng = random.Random(seed)

    stroke = int(w * 0.22)
    path = [(w * 0.36, h * 1.05), (w * 0.85, h * 1.50), (w * 1.64, h * 0.55)]
    dense = []
    for i in range(len(path) - 1):
        ax, ay = path[i]; bx, by = path[i + 1]
        for t in range(21):
            f = t / 20
            dense.append((ax + (bx - ax) * f + rng.uniform(-2, 2),
                          ay + (by - ay) * f + rng.uniform(-2, 2)))
    for i in range(len(dense) - 1):
        d.line([dense[i], dense[i + 1]], fill=255, width=stroke)

    layer = layer.resize(size, Image.LANCZOS)
    return tint(erode(layer, seed, amount=0.16), accent)


def save(img, name):
    path = os.path.join(OUT, name + ".png")
    img.save(path)
    print("wrote", path, img.size)


def main():
    os.makedirs(OUT, exist_ok=True)

    save(panel((160, 160), BONE), "Panel_Bone")
    save(panel((160, 160), CRIMSON, seed=SEED + 5), "Panel_Crimson")
    save(panel((160, 160), GOLD, seed=SEED + 11), "Panel_Gold")
    save(
        panel((160, 160), BONE, corner=10, border=5, fill=(8, 7, 8, 200), seed=SEED + 21, double=False),
        "Panel_Subtle",
    )

    save(bar_frame((120, 30), BONE), "Bar_Frame")
    save(bar_fill((64, 24), (214, 62, 58), (120, 24, 26)), "Bar_Fill_Health")
    save(bar_fill((64, 24), (226, 174, 68), (140, 92, 22)), "Bar_Fill_Stamina")
    save(bar_fill((64, 24), (110, 190, 226), (32, 92, 140)), "Bar_Fill_Mana")

    save(slot((96, 96), BONE), "Slot_Empty")
    save(slot((96, 96), GOLD, seed=SEED + 31), "Slot_Filled")

    save(divider((240, 12), BONE), "Divider")

    save(icon_lock(), "Icon_Lock")
    save(icon_check(), "Icon_Check")


if __name__ == "__main__":
    main()
