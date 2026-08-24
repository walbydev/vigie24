#!/usr/bin/env python3
"""Génère le logo Vigie24 : loupe (vigie) entourée de vagues radar — palette charte.
Sorties : favicon-radar-512.png (master), favicon-radar-32.png, favicon-radar.ico (16/32/48), apple-touch-icon.png (180)
"""
import math
from PIL import Image, ImageDraw

SIZE = 512

# Palette charte Vigie24 (mockup) + Mode Radar
PETROLE_DARK = (13, 32, 39)        # #0D2027 fond radar
PETROLE_SURFACE = (19, 46, 56)     # #132E38 surface radar
TEAL = (111, 203, 180)             # #6FCBB4 vert-sarcelle radar
TEAL_DIM = (111, 203, 180, 90)
AMBER = (242, 183, 85)             # #F2B755 ambre radar
CREAM = (242, 247, 246)            # #F2F7F6 texte clair radar

def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))

def rounded_gradient(size, c1, c2, radius):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for y in range(size):
        t = y / size
        d.line([(0, y), (size, y)], fill=lerp(c1, c2, t) + (255,))
    mask = Image.new("L", (size, size), 0)
    md = ImageDraw.Draw(mask)
    md.rounded_rectangle([0, 0, size - 1, size - 1], radius=radius, fill=255)
    img.putalpha(mask)
    return img

def radar_arc(d, center, radius, width, color, start=40, end=320, steps=220):
    """Arc « radar » avec atténuation sur la fin (trace de balayage)."""
    pts = []
    for i in range(steps):
        a = math.radians(start + (end - start) * i / (steps - 1))
        x = center[0] + radius * math.cos(a)
        y = center[1] + radius * math.sin(a)
        pts.append((x, y))
    # short segments with decreasing alpha toward the end of the sweep
    for i in range(len(pts) - 1):
        t = i / (len(pts) - 1)
        alpha = int(255 * (0.35 + 0.65 * (1 - t)))
        d.line([pts[i], pts[i + 1]], fill=color[:3] + (alpha,), width=width)

def main():
    img = rounded_gradient(SIZE, PETROLE_DARK, PETROLE_SURFACE, radius=110)
    d = ImageDraw.Draw(img)

    cx, cy = SIZE // 2, SIZE // 2 - 6

    # --- Radar waves: 3 concentric arcs (teal, decreasing opacity) ---
    for r, w, col in [(132, 10, TEAL_DIM), (172, 9, TEAL_DIM), (212, 8, TEAL_DIM)]:
        radar_arc(d, (cx, cy), r, w, col)

    # Ligne de balayage (pointe) ambre
    sweep = math.radians(50)
    sx = cx + 212 * math.cos(sweep)
    sy = cy + 212 * math.sin(sweep)
    d.line([(cx, cy), (sx, sy)], fill=AMBER + (200,), width=6)

    # --- Magnifying glass (vigie): ring + handle, off-white ---
    lens_r = 78
    ring_w = 20
    # handle (thick stroke tilted 45° toward bottom-right)
    handle_w = 26
    hx1, hy1 = cx + lens_r + 8, cy + lens_r + 8
    hx2, hy2 = hx1 + 92, hy1 + 92
    d.line([(hx1, hy1), (hx2, hy2)], fill=CREAM + (255,), width=handle_w)

    # anneau de la loupe
    d.ellipse([cx - lens_r, cy - lens_r, cx + lens_r, cy + lens_r],
              outline=CREAM + (255,), width=ring_w)

    # slightly tinted lens
    glass_r = lens_r - ring_w + 4
    d.ellipse([cx - glass_r, cy - glass_r, cx + glass_r, cy + glass_r],
              fill=(111, 203, 180, 26))

    # --- Œil de vigie : point ambre au centre de la loupe ---
    eye_r = 20
    d.ellipse([cx - eye_r, cy - eye_r, cx + eye_r, cy + eye_r], fill=AMBER + (255,))
    d.ellipse([cx - eye_r + 6, cy - eye_r + 6, cx + eye_r - 6, cy + eye_r - 6],
              fill=(255, 244, 214, 220))

    out = "/opt/data/vigie24-repo/src/Vigie24.Web/wwwroot"
    img.save(f"{out}/favicon-radar-512.png")
    img.resize((32, 32), Image.LANCZOS).save(f"{out}/favicon-radar-32.png")
    img.resize((180, 180), Image.LANCZOS).save(f"{out}/apple-touch-icon.png")
    img.save(f"{out}/favicon-radar.ico", sizes=[(16, 16), (32, 32), (48, 48)])
    print("OK : favicon-radar.ico, favicon-radar-32.png, favicon-radar-512.png, apple-touch-icon.png")

if __name__ == "__main__":
    main()
