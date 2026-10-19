import struct, os

def pad4(b):
    return b + b'\x00' * ((4 - len(b) % 4) % 4)

def checksum(b):
    b = pad4(b)
    return sum(struct.unpack('>%dI' % (len(b)//4), b)) & 0xFFFFFFFF

UPM = 1000

def glyph_contours(contours):
    """contours: list of list of (x, y, oncurve)"""
    if not contours:
        return b''
    xs = [p[0] for c in contours for p in c]
    ys = [p[1] for c in contours for p in c]
    out = struct.pack('>hhhhh', len(contours), min(xs), min(ys), max(xs), max(ys))
    end = -1
    ends = []
    for c in contours:
        end += len(c)
        ends.append(end)
    out += b''.join(struct.pack('>H', e) for e in ends)
    out += struct.pack('>H', 0)  # instruction length
    pts = [p for c in contours for p in c]
    flags = b''.join(struct.pack('>B', 1 if p[2] else 0) for p in pts)
    out += flags
    prev = 0
    xb = b''
    for p in pts:
        xb += struct.pack('>h', p[0] - prev); prev = p[0]
    prev = 0
    yb = b''
    for p in pts:
        yb += struct.pack('>h', p[1] - prev); prev = p[1]
    return out + xb + yb

# Glyphs: 0 = .notdef (empty), 1 = filled square, 2 = triangle, 3 = square with a hole
square = [[(100, 0, True), (700, 0, True), (700, 700, True), (100, 700, True)]]
triangle = [[(100, 0, True), (700, 0, True), (400, 700, True)]]
ring = [
    [(100, 0, True), (700, 0, True), (700, 700, True), (100, 700, True)],
    [(250, 150, True), (250, 550, True), (550, 550, True), (550, 150, True)],  # reverse winding
]
glyphs = [b'', glyph_contours(square), glyph_contours(triangle), glyph_contours(ring)]
glyphs = [pad4(g) for g in glyphs]

loca_offsets = [0]
for g in glyphs:
    loca_offsets.append(loca_offsets[-1] + len(g))
glyf = b''.join(glyphs)
loca = b''.join(struct.pack('>I', o) for o in loca_offsets)  # long format

num_glyphs = len(glyphs)
head = struct.pack('>IIIIHHQQhhhhHHhhh',
    0x00010000, 0x00010000, 0, 0x5F0F3CF5,
    0b11, UPM, 0, 0,
    0, 0, 800, 800,
    0, 8, 2,   # macStyle, lowestRecPPEM, fontDirectionHint
    1,         # indexToLocFormat = 1 (long)
    0)         # glyphDataFormat
assert len(head) == 54, len(head)

hhea = struct.pack('>IhhhHhhhhhhhhhhhH',
    0x00010000, 800, -200, 0,
    900, 0, 0, 800,
    1, 0, 0, 0, 0, 0, 0, 0,
    num_glyphs)
assert len(hhea) == 36, len(hhea)

hmtx = b''.join(struct.pack('>Hh', 800, 100) for _ in range(num_glyphs))
maxp = struct.pack('>IHHHHHHHHHHHHHH', 0x00010000, num_glyphs, 8, 2, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0)

# cmap format 4 mapping 'A'->1, 'B'->2, 'C'->3
segs = [(0x41, 0x43, 1 - 0x41), (0xFFFF, 0xFFFF, 1)]
seg_count = len(segs)
search_range = 2 * (2 ** (seg_count.bit_length() - 1))
sub = struct.pack('>HHHHHHH', 4, 16 + seg_count * 8, 0, seg_count * 2, search_range,
                  seg_count.bit_length() - 1, seg_count * 2 - search_range)
sub += b''.join(struct.pack('>H', s[1]) for s in segs)
sub += struct.pack('>H', 0)
sub += b''.join(struct.pack('>H', s[0]) for s in segs)
sub += b''.join(struct.pack('>h', s[2]) for s in segs)
sub += b''.join(struct.pack('>H', 0) for _ in segs)
cmap = struct.pack('>HHHHI', 0, 1, 3, 1, 12) + sub

def name_table(entries):
    storage = b''
    records = b''
    for (platform, enc, lang, nid, text) in entries:
        data = text.encode('utf-16-be') if platform == 3 else text.encode('ascii')
        records += struct.pack('>HHHHHH', platform, enc, lang, nid, len(data), len(storage))
        storage += data
    return struct.pack('>HHH', 0, len(entries), 6 + len(entries) * 12) + records + storage

name = name_table([
    (3, 1, 0x409, 1, 'Bmage Test'),
    (3, 1, 0x409, 4, 'Bmage Test Regular'),
])

# kern table format 0: pair (1,2) -> -50
kern_pairs = [(1, 2, -50)]
kern_sub = struct.pack('>HHHHHHH', 0, 14 + len(kern_pairs) * 6, 1, len(kern_pairs), 6, 0, 0)
kern_sub += b''.join(struct.pack('>HHh', a, b, v) for a, b, v in kern_pairs)
kern = struct.pack('>HH', 0, 1) + kern_sub

tables = {
    b'cmap': cmap, b'glyf': glyf, b'head': head, b'hhea': hhea,
    b'hmtx': hmtx, b'kern': kern, b'loca': loca, b'maxp': maxp, b'name': name,
}
tags = sorted(tables)
num_tables = len(tags)
entry_selector = (num_tables.bit_length() - 1)
search_range = (2 ** entry_selector) * 16
header = struct.pack('>IHHHH', 0x00010000, num_tables, search_range, entry_selector, num_tables * 16 - search_range)

offset = len(header) + num_tables * 16
records = b''
body = b''
for tag in tags:
    data = tables[tag]
    records += tag + struct.pack('>III', checksum(data), offset, len(data))
    padded = pad4(data)
    body += padded
    offset += len(padded)

font = header + records + body
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'BmageTest.ttf')
with open(out, 'wb') as f:
    f.write(font)
print('wrote', out, len(font), 'bytes')
