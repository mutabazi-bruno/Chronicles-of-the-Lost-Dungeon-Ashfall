"""Small helpers for retargeting UI Image components in a prefab or scene file.

Unity stores prefabs as YAML. Reassigning a sprite by hand means finding the right
Image block among hundreds and rewriting three fields without disturbing anything
else, which is what these functions do. Resolving objects by hierarchy path keeps
it readable, because names like Background and Fill repeat all over a HUD.
"""

import re

SPRITE_FILE_ID = 21300000  # the sprite sub-asset inside a Single-mode texture


def load(path):
    with open(path, encoding="utf-8") as fh:
        return fh.read()


def save(path, text):
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(text)


def index(text):
    blocks = re.split(r"\n(?=--- !u!)", text)
    obj = {}
    for b in blocks:
        m = re.match(r"--- !u!(\d+) &(\d+)", b)
        if m:
            obj[m.group(2)] = (m.group(1), b)
    return obj


def hierarchy(obj):
    name, comps, children, owner = {}, {}, {}, {}
    for fid, (t, b) in obj.items():
        if t == "1":
            n = re.search(r"m_Name: (.*)", b)
            name[fid] = n.group(1).strip() if n else "?"
            comps[fid] = re.findall(r"- component: \{fileID: (\d+)\}", b)
        if t in ("224", "4"):
            g = re.search(r"m_GameObject: \{fileID: (\d+)\}", b)
            if g:
                owner[fid] = g.group(1)
            ch = re.search(r"m_Children:\s*(\[\]|(?:\n\s*- \{fileID: \d+\})+)", b)
            children[fid] = (
                re.findall(r"\{fileID: (\d+)\}", ch.group(1))
                if ch and ch.group(1) != "[]" else []
            )
    return name, comps, children, owner


def find_transform(obj, path):
    """Resolve a slash separated hierarchy path to its transform fileID.

    A prefab has one root so the path can start at its children. A scene has many,
    so start the path with the root object's own name, for example
    "Canvas/SettingsPanel".
    """
    name, comps, children, owner = hierarchy(obj)
    roots = [f for f in owner if "m_Father: {fileID: 0}" in obj[f][1]]

    parts = [p for p in path.split("/") if p]

    if len(roots) == 1:
        current = roots[0]
    else:
        first = parts.pop(0)
        matches = [r for r in roots if name.get(owner[r]) == first]
        if len(matches) != 1:
            raise KeyError("expected exactly one root named %r, found %d"
                           % (first, len(matches)))
        current = matches[0]
    for part in parts:
        nxt = None
        for ch in children.get(current, []):
            if ch in owner and name.get(owner[ch]) == part:
                nxt = ch
                break
        if nxt is None:
            raise KeyError("no child %r under %r" % (part, name.get(owner[current])))
        current = nxt
    return current


def component_of(obj, transform_fid, class_suffix):
    name, comps, children, owner = hierarchy(obj)
    go = owner[transform_fid]
    for c in comps[go]:
        if c not in obj:
            continue
        t, b = obj[c]
        if t != "114":
            continue
        cls = re.search(r"m_EditorClassIdentifier: (.*)", b)
        if cls and cls.group(1).strip().endswith(class_suffix):
            return c
    raise KeyError("no %s on that object" % class_suffix)


def set_image(text, path, guid=None, image_type=None, colour=None):
    """Point an Image at a new sprite, change its draw mode, or retint it."""
    obj = index(text)
    tr = find_transform(obj, path)
    fid = component_of(obj, tr, "UnityEngine.UI.Image")
    _, block = obj[fid]
    original = block

    if guid is not None:
        block = re.sub(
            r"m_Sprite: \{fileID: -?\d+(?:, guid: \w+, type: \d+)?\}",
            "m_Sprite: {fileID: %d, guid: %s, type: 3}" % (SPRITE_FILE_ID, guid),
            block, count=1)

    if image_type is not None:
        block = re.sub(r"\n  m_Type: \d+", "\n  m_Type: %d" % image_type, block, count=1)

    if colour is not None:
        r, g, b, a = colour
        block = re.sub(
            r"m_Color: \{r: [\d.eE+-]+, g: [\d.eE+-]+, b: [\d.eE+-]+, a: [\d.eE+-]+\}",
            "m_Color: {r: %s, g: %s, b: %s, a: %s}" % (r, g, b, a),
            block, count=1)

    if block == original:
        raise RuntimeError("nothing changed for %s" % path)
    return text.replace(original, block, 1)


def set_rect(text, path, pos=None, size=None, anchor_min=None, anchor_max=None, pivot=None):
    """Move or resize a RectTransform."""
    obj = index(text)
    tr = find_transform(obj, path)
    _, block = obj[tr]
    original = block

    def vec2(key, value):
        nonlocal block
        block = re.sub(
            r"%s: \{x: -?[\d.eE+-]+, y: -?[\d.eE+-]+\}" % key,
            "%s: {x: %s, y: %s}" % (key, value[0], value[1]),
            block, count=1)

    if pos is not None: vec2("m_AnchoredPosition", pos)
    if size is not None: vec2("m_SizeDelta", size)
    if anchor_min is not None: vec2("m_AnchorMin", anchor_min)
    if anchor_max is not None: vec2("m_AnchorMax", anchor_max)
    if pivot is not None: vec2("m_Pivot", pivot)

    if block == original:
        raise RuntimeError("nothing changed for %s" % path)
    return text.replace(original, block, 1)
