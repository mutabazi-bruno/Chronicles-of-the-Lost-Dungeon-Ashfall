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


def reorder_children(text, parent_path, order):
    """Reorder a parent's children by name.

    Sibling order is draw order on a Canvas: later children paint over earlier
    ones. Names listed in `order` are moved to the front in the order given, and
    anything not listed keeps its relative position after them.
    """
    obj = index(text)
    name, comps, children, owner = hierarchy(obj)
    parent = find_transform(obj, parent_path) if parent_path else next(
        f for f in owner if "m_Father: {fileID: 0}" in obj[f][1])

    current = [c for c in children[parent] if c in owner]
    by_name = {}
    for c in current:
        by_name.setdefault(name[owner[c]], []).append(c)

    front = []
    for n in order:
        if n not in by_name:
            raise KeyError("no child named %r" % n)
        front.extend(by_name[n])

    rest = [c for c in current if c not in front]
    new_order = front + rest

    _, block = obj[parent]
    original = block
    rendered = "".join("\n  - {fileID: %s}" % c for c in new_order)
    block = re.sub(
        r"m_Children:\s*(?:\[\]|(?:\n\s*- \{fileID: \d+\})+)",
        "m_Children:" + rendered,
        block, count=1)

    if block == original:
        raise RuntimeError("child list unchanged")
    return text.replace(original, block, 1)


IMAGE_SCRIPT_GUID = "fe87c0e1cc204ed48ad3b37840f39efc"


def _new_id(text, salt):
    """A stable fileID that is not already used in this file."""
    import hashlib
    n = 0
    while True:
        h = hashlib.md5(("%s#%d" % (salt, n)).encode()).hexdigest()
        candidate = str(int(h[:15], 16))
        if ("&" + candidate) not in text:
            return candidate
        n += 1


def add_image_child(text, parent_path, child_name, sprite_guid,
                    size=(40, 40), pos=(0, 0), colour=(1, 1, 1, 1),
                    anchor=(0.5, 0.5), active=True):
    """Append a child holding a single UI Image, and return (text, gameObjectId).

    Written out by hand because there is no way to add a GameObject to a scene
    from outside the editor. The block layout follows what Unity itself writes.
    """
    obj = index(text)
    parent_tr = find_transform(obj, parent_path)

    go = _new_id(text, parent_path + child_name + "go")
    rt = _new_id(text, parent_path + child_name + "rt")
    cr = _new_id(text, parent_path + child_name + "cr")
    im = _new_id(text, parent_path + child_name + "im")

    block = """--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {rt}}}
  - component: {{fileID: {cr}}}
  - component: {{fileID: {im}}}
  m_Layer: 5
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {active}
--- !u!224 &{rt}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {parent}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {ax}, y: {ay}}}
  m_AnchorMax: {{x: {ax}, y: {ay}}}
  m_AnchoredPosition: {{x: {px}, y: {py}}}
  m_SizeDelta: {{x: {sw}, y: {sh}}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!222 &{cr}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_CullTransparentMesh: 1
--- !u!114 &{im}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {imgguid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {{r: {cr_}, g: {cg}, b: {cb}, a: {ca}}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 21300000, guid: {spriteguid}, type: 3}}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
""".format(go=go, rt=rt, cr=cr, im=im, name=child_name,
           active=1 if active else 0, parent=parent_tr,
           ax=anchor[0], ay=anchor[1], px=pos[0], py=pos[1],
           sw=size[0], sh=size[1],
           cr_=colour[0], cg=colour[1], cb=colour[2], ca=colour[3],
           imgguid=IMAGE_SCRIPT_GUID, spriteguid=sprite_guid)

    text = text.rstrip("\n") + "\n" + block

    # hook it into the parent's child list
    obj = index(text)
    _, pblock = obj[parent_tr]
    original = pblock
    if re.search(r"m_Children: \[\]", pblock):
        pblock = pblock.replace("m_Children: []",
                                "m_Children:\n  - {fileID: %s}" % rt, 1)
    else:
        pblock = re.sub(r"(m_Children:(?:\n\s*- \{fileID: \d+\})+)",
                        r"\1\n  - {fileID: %s}" % rt, pblock, count=1)
    text = text.replace(original, pblock, 1)
    return text, go


def set_field(text, path, class_suffix, field, value):
    """Point a serialized reference field at something, e.g. lockIcon."""
    obj = index(text)
    tr = find_transform(obj, path)
    fid = component_of(obj, tr, class_suffix)
    _, block = obj[fid]
    original = block
    block = re.sub(r"\n  %s: .*" % re.escape(field), "\n  %s: %s" % (field, value),
                   block, count=1)
    if block == original:
        raise RuntimeError("field %r not found on %s" % (field, path))
    return text.replace(original, block, 1)


def reparent(text, child_path, new_parent_path, pos=(0, 0), size=None):
    """Move a child under a new parent, centred on it.

    Draw order on a Canvas follows the hierarchy, and a child always paints over
    its parent. Nesting a label inside its own backdrop is therefore stable in a
    way that shuffling siblings is not.
    """
    obj = index(text)
    child = find_transform(obj, child_path)
    new_parent = find_transform(obj, new_parent_path)

    old_father = re.search(r"m_Father: \{fileID: (\d+)\}", obj[child][1]).group(1)

    # drop it from the old parent's list
    _, oldblock = obj[old_father]
    replacement = re.sub(r"\n\s*- \{fileID: %s\}" % child, "", oldblock, count=1)
    if re.search(r"m_Children:\s*\n\s*m_Father", replacement):
        replacement = re.sub(r"m_Children:\s*\n(\s*m_Father)", r"m_Children: []\n\1", replacement)
    text = text.replace(oldblock, replacement, 1)

    # add it to the new parent's list
    obj = index(text)
    _, newblock = obj[new_parent]
    if re.search(r"m_Children: \[\]", newblock):
        updated = newblock.replace("m_Children: []",
                                   "m_Children:\n  - {fileID: %s}" % child, 1)
    else:
        updated = re.sub(r"(m_Children:(?:\n\s*- \{fileID: \d+\})+)",
                         r"\1\n  - {fileID: %s}" % child, newblock, count=1)
    text = text.replace(newblock, updated, 1)

    # point the child at its new parent and centre it
    obj = index(text)
    _, cblock = obj[child]
    fixed = cblock.replace("m_Father: {fileID: %s}" % old_father,
                           "m_Father: {fileID: %s}" % new_parent, 1)
    text = text.replace(cblock, fixed, 1)

    text = set_rect(text, new_parent_path + "/" + child_path.split("/")[-1],
                    pos=pos, size=size,
                    anchor_min=(0.5, 0.5), anchor_max=(0.5, 0.5), pivot=(0.5, 0.5))
    return text


def add_script_component(text, path, script_guid, class_identifier, fields=""):
    """Attach a MonoBehaviour to an existing object and return the new text."""
    obj = index(text)
    tr = find_transform(obj, path)
    go = hierarchy(obj)[3][tr]

    comp = _new_id(text, path + class_identifier + "component")

    block = """--- !u!114 &{comp}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: {cls}
{fields}""".format(comp=comp, go=go, guid=script_guid,
                   cls=class_identifier, fields=fields)

    text = text.rstrip("\n") + "\n" + block

    obj = index(text)
    _, gblock = obj[go]
    original = gblock
    gblock = re.sub(r"(m_Component:(?:\n\s*- component: \{fileID: \d+\})+)",
                    r"\1\n  - component: {fileID: %s}" % comp, gblock, count=1)
    if gblock == original:
        raise RuntimeError("could not extend the component list on %s" % path)
    return text.replace(original, gblock, 1)
