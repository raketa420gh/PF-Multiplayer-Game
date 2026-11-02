// node plan.js <Room> <y> [cut=1.5] — текстовый план на срезе y + cut: # стена, : проп, S лестница, . пол (цифры — высота пола, если не y), D дверь, L рычаг, P плита, X секретная плита, T ловушка, c сундук, m монстр, ~ вода.
const fs = require('fs'), path = require('path');
const [name, ys, cuts] = process.argv.slice(2), y0 = +ys, cut = y0 + (cuts ? +cuts : 1.5), STEP = 0.6, EXT = 17.4;
const ATLAS = { room: (n, a) => { ATLAS.items = a; } };
new Function('ATLAS', fs.readFileSync(path.join(__dirname, '..', 'data', `${name}.js`), 'utf8'))(ATLAS);
const fp = o => { const y = (o.ry || 0) * Math.PI / 180, c = Math.abs(Math.cos(y)), s = Math.abs(Math.sin(y)), w = o.s[0] * c + o.s[2] * s, d = o.s[0] * s + o.s[2] * c; return [o.p[0] - w / 2, o.p[2] - d / 2, o.p[0] + w / 2, o.p[2] + d / 2]; };
const hits = (o, x, z) => { if (!o.s) return false; const f = fp(o), m = Math.max(0.3, 0); return x + m > f[0] && x - m < f[2] && z + m > f[1] && z - m < f[3]; };
const inCut = o => o.p[1] <= cut && o.p[1] + o.s[1] >= cut;
const marks = { Door: 'D', DoorHalf: 'D', SecretLever: 'L', FloorPlate: 'P', SecretDoor: 'X', Lift: 'E', MonsterDrop: 'V' };
const lines = [];
for (let z = EXT - 0.3; z > -EXT; z -= STEP) {
  let line = '';
  for (let x = -EXT + 0.3; x < EXT; x += STEP) {
    let ch = ' ';
    const at = ATLAS.items.filter(o => hits(o, x, z));
    const floor = at.filter(o => (o.cat === 'floor' || o.cat === 'platform') && o.p[1] + o.s[1] <= cut + 0.01).map(o => o.p[1] + o.s[1]);
    if (floor.length) { const t = Math.max(...floor); ch = Math.abs(t - y0) < 0.3 ? '.' : String(Math.min(9, Math.abs(Math.round(t - y0)))); }
    if (at.some(o => o.cat === 'water' && Math.abs(o.p[1] - y0) < 1)) ch = '~';
    if (at.some(o => o.cat === 'stairs' && o.p[1] <= cut + 1 && o.p[1] + o.s[1] >= y0 - 1)) ch = 'S';
    if (at.some(o => o.cat === 'prop' && inCut({ ...o, s: [o.s[0], Math.max(o.s[1], 1.6), o.s[2]] }))) ch = ':';
    if (at.some(o => ['wall', 'column', 'bars'].includes(o.cat) && inCut(o))) ch = '#';
    const pt = ATLAS.items.find(o => o.s && ['trap', 'chest', 'monster'].includes(o.cat) && Math.abs(o.p[1] - y0) < 3 && (hits(o, x, z)));
    if (pt) ch = marks[pt.name] || (pt.cat === 'trap' ? 'T' : pt.cat === 'chest' ? 'c' : 'm');
    line += ch;
  }
  lines.push(line);
}
console.log(lines.join('\n'));
