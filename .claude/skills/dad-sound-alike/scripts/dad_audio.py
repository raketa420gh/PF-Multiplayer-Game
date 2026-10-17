"""Dark and Darker Wwise audio extraction.

  py -I dad_audio.py search <substring>          list Wwise event assets whose path contains it
  py -I dad_audio.py extract <EventName> [out]   event -> bank -> .wem -> .wav (default out D:/DaD_Sounds/<EventName>)

Event uassets live in IoStore (.utoc/.ucas, Oodle). Banks and .wem media are entries of the legacy .pak files
(some Oodle-compressed) whose index is AES-encrypted: entries are found from their 'BKHD'/'RIFF' literals
(pakwalk.py), media are matched by the size the bank records, and ties are broken by the pak's lexical path order.
"""
import ctypes, glob, json, mmap, os, re, struct, subprocess, sys, urllib.request, zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pakwalk  # noqa: E402

PAKS = 'D:/Steam/steamapps/common/Dark and Darker/DungeonCrawler/Content/Paks'
TOOLS = 'D:/DaD_Sounds/_tools'
OODLE_URL = 'https://github.com/WorkingRobot/OodleUE/releases/download/2026-06-04-1357/msvc-x64-release.zip'
VGM_URL = 'https://github.com/vgmstream/vgmstream/releases/latest/download/vgmstream-win64.zip'


def fetch(url, sub, probe):
    d = os.path.join(TOOLS, sub)
    hit = glob.glob(os.path.join(d, '**', probe), recursive=True)
    if hit:
        return hit[0]
    os.makedirs(d, exist_ok=True)
    z = os.path.join(d, 'pkg.zip')
    urllib.request.urlretrieve(url, z)
    zipfile.ZipFile(z).extractall(d)
    return glob.glob(os.path.join(d, '**', probe), recursive=True)[0]


def oodle():
    lib = ctypes.CDLL(fetch(OODLE_URL, 'oodle', 'oodle-data-shared.dll'))
    f = lib.OodleLZ_Decompress
    f.restype = ctypes.c_int64

    def dec(raw, usz):
        out = ctypes.create_string_buffer(usz)
        r = f(raw, ctypes.c_int64(len(raw)), out, ctypes.c_int64(usz), 1, 0, 0, None, 0, None, None, None, 0, 3)
        assert r == usz, (r, usz)
        return out.raw
    return dec


def fnv(s):
    h = 2166136261
    for c in s.lower().encode():
        h = (h * 16777619) & 0xFFFFFFFF
        h ^= c
    return h


class Toc:
    """IoStore container table of contents (utoc version 8)."""

    def __init__(self, path):
        self.path = path
        d = open(path, 'rb').read()
        assert d[:16] == b'-==--==--==--==-'
        hsize, n, nblk, _, ncm, cmlen, self.block_size, dirsize, _ = struct.unpack_from('<9I', d, 20)
        flags = d[80]
        nseeds, = struct.unpack_from('<I', d, 84)
        nnohash, = struct.unpack_from('<I', d, 96)
        p = hsize + 12 * n
        self.offlen = [(int.from_bytes(d[p + i * 10:p + i * 10 + 5], 'big'), int.from_bytes(d[p + i * 10 + 5:p + i * 10 + 10], 'big')) for i in range(n)]
        p += 10 * n + 4 * nseeds + 4 * nnohash
        self.blocks = [(int.from_bytes(d[p + i * 12:p + i * 12 + 5], 'little'), int.from_bytes(d[p + i * 12 + 5:p + i * 12 + 8], 'little'),
                        int.from_bytes(d[p + i * 12 + 8:p + i * 12 + 11], 'little'), d[p + i * 12 + 11]) for i in range(nblk)]
        p += 12 * nblk + ncm * cmlen
        if flags & 4:
            hs, = struct.unpack_from('<I', d, p)
            p += 4 + hs * 2 + 20 * nblk
        self.files = {}
        if flags & 8 and dirsize:
            self._index(d[p:p + dirsize])

    def _index(self, d):
        p = 0

        def fstr():
            nonlocal p
            l, = struct.unpack_from('<i', d, p)
            p += 4
            s = d[p:p - l * 2].decode('utf-16-le') if l < 0 else d[p:p + l].decode('utf-8', 'replace')
            p += -l * 2 if l < 0 else l
            return s.rstrip('\0')
        mount = fstr()
        nd, = struct.unpack_from('<I', d, p)
        dirs = [struct.unpack_from('<4I', d, p + 4 + i * 16) for i in range(nd)]
        p += 4 + 16 * nd
        nf, = struct.unpack_from('<I', d, p)
        files = [struct.unpack_from('<3I', d, p + 4 + i * 12) for i in range(nf)]
        p += 4 + 12 * nf
        ns, = struct.unpack_from('<I', d, p)
        p += 4
        strs = [fstr() for _ in range(ns)]
        none = 0xFFFFFFFF

        def walk(di, path):
            name, child, _, fe = dirs[di]
            here = path + (strs[name] + '/' if name != none else '')
            while fe != none:
                fn, fe, toc = files[fe]
                self.files[(mount + here + strs[fn]).replace('../../../', '')] = toc
            while child != none:
                walk(child, here)
                child = dirs[child][2]
        walk(0, '')

    def read(self, idx, dec):
        off, ln = self.offlen[idx]
        bs = self.block_size
        out = bytearray()
        with open(os.path.splitext(self.path)[0] + '.ucas', 'rb') as f:
            for bi in range(off // bs, (off + ln - 1) // bs + 1):
                boff, csz, usz, m = self.blocks[bi]
                f.seek(boff)
                raw = f.read(csz)
                out += raw[:usz] if m == 0 else dec(raw, usz)
        s = off - (off // bs) * bs
        return bytes(out[s:s + ln])


def tocs():
    # Patch containers (_P) override the base ones: read them first.
    fs = sorted(glob.glob(PAKS + '/*.utoc'), key=lambda f: ('_P' not in f, f))
    return [Toc(f) for f in fs]


def find_event(name):
    for t in tocs():
        for k, i in t.files.items():
            if '/WwiseAudio/Events/' in k and os.path.basename(k)[:-7].lower() == name.lower():
                return t, i, k
    sys.exit(f'event {name} not found; try: search {name[:12]}')


def paks():
    fs = [f for f in glob.glob(PAKS + '/*.pak') if os.path.getsize(f) > 100000]
    return sorted(fs, key=lambda f: ('_P' not in f, f))


def open_pak(pak):
    fh = open(pak, 'rb')
    return mmap.mmap(fh.fileno(), 0, access=mmap.ACCESS_READ)


def pak_index(pak, dec):
    """Every bank (by id) and media entry of the pak, cached next to the tools."""
    cache = os.path.join(TOOLS, 'index_' + os.path.basename(pak) + '.json')
    st = os.path.getmtime(pak)
    if os.path.exists(cache):
        c = json.load(open(cache))
        if c['mtime'] == st:
            return c
    m = open_pak(pak)
    banks = {}
    for h in pakwalk.scan(m, b'BKHD'):
        d = pakwalk.read(m, h, dec, 16)
        if d[:4] == b'BKHD':
            banks[str(struct.unpack_from('<I', d, 12)[0])] = h
    c = {'mtime': st, 'banks': banks, 'media': pakwalk.scan(m, b'RIFF')}
    os.makedirs(TOOLS, exist_ok=True)
    json.dump(c, open(cache, 'w'))
    return c


def bank_chunks(d):
    p = 0
    while p + 8 <= len(d) and re.fullmatch(rb'[A-Z]{4}', d[p:p + 4]):
        sz, = struct.unpack_from('<I', d, p + 4)
        yield d[p:p + 4], p + 8, sz
        p += 8 + sz


def find_bank(bank, dec):
    bid = str(fnv(bank))
    for pak in paks():
        h = pak_index(pak, dec)['banks'].get(bid)
        if h:
            return pak, pakwalk.read(open_pak(pak), h, dec)
    return None, None


def hirc_sizes(d, media):
    sizes = {}
    for tag, p, sz in bank_chunks(d):
        if tag == b'HIRC':
            for mid in media:
                k = d.find(struct.pack('<I', mid), p, p + sz)
                if k >= 0:
                    sizes[mid] = struct.unpack_from('<I', d, k + 8)[0]
    return sizes


def sizes_elsewhere(media, dec):
    """Sizes of media the event's own bank does not describe, from any other bank."""
    sizes = {}
    for pak in paks():
        m = open_pak(pak)
        for h in pak_index(pak, dec)['banks'].values():
            sizes.update(hirc_sizes(pakwalk.read(m, h, dec), [x for x in media if x not in sizes]))
            if len(sizes) == len(media):
                return sizes
    return sizes


def resolve_ties(found):
    """Orders same-size candidates best first. Inside a pak media are stored in lexical order of '<id>.wem', so
    '0.<id>' is monotonic in offset; ids are hashes, so it is also roughly linear between resolved neighbours."""
    key = lambda mid: float('0.' + str(mid))
    changed = True
    while changed:
        changed = False
        for mid, c in found.items():
            if len(c) < 2 or c[0][1] is None:
                continue
            pk = c[0][0]
            anchors = [(0.0, 0), (1.0, os.path.getsize(pk))] + [(key(a), x[0][1]) for a, x in found.items()
                                                                if a != mid and len(x) == 1 and x[0][0] == pk and x[0][1] is not None]
            lo = max((a for a in anchors if a[0] < key(mid)), key=lambda a: a[0])
            hi = min((a for a in anchors if a[0] > key(mid)), key=lambda a: a[0])
            expect = lo[1] + (key(mid) - lo[0]) / (hi[0] - lo[0]) * (hi[1] - lo[1])
            inside = [x for x in c if lo[1] < x[1] < hi[1]]
            ranked = sorted(inside or c, key=lambda x: abs(x[1] - expect)) + [x for x in c if inside and x not in inside]
            if len(inside) == 1 and len(c) > 1:
                ranked = inside
                changed = True
            found[mid] = ranked


def extract(event, out):
    dec = oodle()
    t, i, path = find_event(event)
    asset = t.read(i, dec)
    strs = re.findall(rb'[ -~]{4,}', asset)
    bank = next((re.search(rb'([^/]+)\.bnk', s).group(1).decode() for s in strs if b'.bnk' in s), None)
    media = [int(x) for x in re.findall(rb'Media/(\d+)\.wem', asset)]
    print('event', path, '\n bank', bank, '\n media', media)
    os.makedirs(out, exist_ok=True)
    pak, d = find_bank(bank, dec)
    if d is None:
        sys.exit('bank not found in any pak (entry encrypted?)')
    sizes, embedded = hirc_sizes(d, media), {}
    didx = data = None
    for tag, p, sz in bank_chunks(d):
        if tag == b'DIDX':
            didx = [struct.unpack_from('<III', d, p + k * 12) for k in range(sz // 12)]
        elif tag == b'DATA':
            data = p
    if didx and data is not None:
        for mid, off, sz in didx:
            embedded[mid] = d[data + off:data + off + sz]
    missing = [mid for mid in media if mid not in sizes and mid not in embedded]
    if missing:
        sizes.update(sizes_elsewhere(missing, dec))
    print(' bank in', os.path.basename(pak), 'sizes', sizes, 'embedded', list(embedded))
    found = {mid: [(pak, None, b)] for mid, b in embedded.items()}
    for pk in paks():
        idx = pak_index(pk, dec)['media']
        for mid, sz in sizes.items():
            if mid not in found:
                c = [(pk, h['q'], h) for h in idx if h['usize'] == sz]
                if c:
                    found[mid] = c
    resolve_ties(found)
    vgm = fetch(VGM_URL, 'vgmstream', 'vgmstream-cli.exe')
    os.makedirs(os.path.join(out, '_alternates'), exist_ok=True)
    for n, mid in enumerate(media, 1):
        if mid not in found:
            print(' MISSING', mid, '(no bank describes it - stale reference)')
            continue
        for k, (pk, off, blob) in enumerate(found[mid]):
            if off is not None:
                blob = pakwalk.read(open_pak(pk), blob, dec)
            stem = f'{event}_{n}_{mid}' if k == 0 else os.path.join('_alternates', f'{mid}_alt{k}')
            wem = os.path.join(TOOLS, 'tmp.wem')
            open(wem, 'wb').write(blob)
            subprocess.run([vgm, '-o', os.path.join(out, stem) + '.wav', wem], check=True, capture_output=True)
        note = '' if len(found[mid]) == 1 else f'  ({len(found[mid]) - 1} same-size alternates kept)'
        print(' wrote', f'{event}_{n}_{mid}.wav{note}')


if __name__ == '__main__':
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    if sys.argv[1] == 'search':
        pat = sys.argv[2].lower()
        for t in tocs():
            for k in t.files:
                if '/WwiseAudio/Events/' in k and pat in k.lower():
                    print(k.split('/WwiseAudio/Events/')[1][:-7])
    elif sys.argv[1] == 'extract':
        extract(sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else f'D:/DaD_Sounds/{sys.argv[2]}')
