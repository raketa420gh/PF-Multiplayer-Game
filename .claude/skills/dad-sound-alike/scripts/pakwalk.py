"""Finds Wwise banks and media in a UE5 .pak without its (AES-encrypted) index.

Every entry sits behind an inline header. A bank ('BKHD') or media ('RIFF') shows up as a literal near the start of
its entry even when the entry is Oodle-compressed, so each literal hit is traced back to the header that owns it.
"""
import re, struct


def header(m, q):
    """Parsed entry header at q, or None when q is not a plausible header."""
    if q < 0 or q + 53 > len(m) or m[q:q + 8] != b'\0' * 8:
        return None
    size, usize, meth = struct.unpack_from('<QQI', m, q + 8)
    if not 0 < size < len(m) or not 0 < usize < 1 << 34 or meth > 4:
        return None
    if meth == 0:
        if size != usize or m[q + 48] > 1:
            return None
        return dict(q=q, usize=usize, meth=0, blocks=[(q + 53, q + 53 + size)], enc=m[q + 48] & 1, bs=0)
    nb, = struct.unpack_from('<I', m, q + 48)
    if not 0 < nb < 100000:
        return None
    hl = 52 + 16 * nb + 5
    bl = struct.unpack_from('<%dQ' % (2 * nb), m, q + 52)
    flags, bs = struct.unpack_from('<BI', m, q + hl - 5)
    if bl[0] != hl or flags > 1 or bs == 0 or nb != -(-usize // bs):
        return None
    return dict(q=q, usize=usize, meth=meth, blocks=[(q + bl[2 * i], q + bl[2 * i + 1]) for i in range(nb)], enc=flags & 1, bs=bs)


def owner(m, hit):
    """Header of the entry whose data holds the literal at hit."""
    h = header(m, hit - 53)
    if h:
        return h
    lo, q = max(0, hit - 70000), hit - 73
    while q >= lo:
        q = m.rfind(b'\0' * 8, lo, q + 8)
        if q < 0:
            break
        h = header(m, q)
        if h and h['meth'] and h['blocks'][0][0] <= hit < h['blocks'][0][1]:
            return h
        q -= 1
    return None


def scan(m, tag):
    """Owning entry headers of every literal tag ('BKHD' / 'RIFF'), one per entry."""
    seen = {}
    for x in re.finditer(re.escape(tag), m):
        if any(h['blocks'][0][0] <= x.start() < h['blocks'][0][1] for h in list(seen.values())[-1:]):
            continue
        h = owner(m, x.start())
        if h and not h['enc']:
            seen[h['q']] = h
    return list(seen.values())


def read(m, h, dec, limit=None):
    """Entry bytes, decompressed; with limit only as many blocks as needed for that many bytes."""
    if h['meth'] == 0:
        a, b = h['blocks'][0]
        return bytes(m[a:b if limit is None else min(b, a + limit)])
    out = bytearray()
    for i, (a, b) in enumerate(h['blocks']):
        out += dec(bytes(m[a:b]), min(h['bs'], h['usize'] - i * h['bs']))
        if limit is not None and len(out) >= limit:
            break
    return bytes(out)
