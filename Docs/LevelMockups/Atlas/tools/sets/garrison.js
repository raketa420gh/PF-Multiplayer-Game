// Общие вещи гарнизона (Barbican, Armory, CornerBarracks, CornerWatch, ArcherGalleries, Checkpoint): стена по точке, щиты, стрелы, остовы,
// стоянки жильщиков, подношения Спящих: const G = require('../sets/garrison')(S).
module.exports = S => {
  const { D } = S, { put, tile, beam, R, J, chance } = D, G = { miss: [] }, RAD = Math.PI / 180;
  /// Стена от точки в сторону k на высоте y; если её нет, null и запись в miss.
  /// Плоскость уточняется до самой грани: S.wall округляет её до 0,05 м и может увести тонкую вещь в толщу стены.
  G.W = (x, z, k, y = 1.5, max = 4) => {
    const w = S.wall(x, z, k, y, max); if (!w) { G.miss.push(`${x},${z},${k},${y}`); return null; }
    const [dx, dz] = { N: [0, 1], E: [1, 0], S: [0, -1], W: [-1, 0] }[k], from = dx ? x : z, d0 = Math.max(0, Math.abs(w[1] - from) - 0.12); let t = d0;
    while (t < d0 + 0.3 && S.air(x + dx * t, y, z + dz * t)) t += 0.01;
    w[1] = Math.round((from + (dx || dz) * (t - 0.035)) * 200) / 200; return w;                       // и на 3 см наружу: доски и листы не тонут в выступающих блоках
  };
  /// Знамя на двух крюках, на ладонь от стены: рельеф кладки не протыкает полотнище.
  G.banner = (w, y, o = {}) => { if (!w) return; const n = w[0] === 'N' || w[0] === 'E' ? 1 : -1, bw = o.w || 0.9; for (const e of [-1, 1]) tile('Крюк знамени', 'iron', w[0], w[1], w[2] + e * (bw / 2 + 0.1), y - 0.03, 0.05, 0.12, 0.22, { sink: 0.05 }); S.banner([w[0], w[1] + n * 0.06, w[2]], y, o); };
  G.fy = (x, z, near = 0) => { const f = S.floor(x, z, near); return f === undefined ? near : f; };
  /// Брус на стене между двумя точками (du, y) от точки стены: оружие, метки мелом, тяги.
  G.bar = (name, m, w, u1, y1, u2, y2, th = 0.04, out = 0.05) => { if (!w) return; const a = D.at(w[0], w[1], w[2] + u1, out), b = D.at(w[0], w[1], w[2] + u2, out); beam(name, m, [a[0], y1, a[1]], [b[0], y2, b[1]], th); };
  G.shield = (w, y, { m = 'wood', s = 0.62 } = {}) => { if (!w) return; D.blot('Щит на стене', m, w[0], w[1], w[2], y, s, s, 0.1, -99); D.blot('Умбон', 'iron', w[0], w[1], w[2], y, s * 0.3, s * 0.3, 0.2, -99); };
  G.cross = (w, y, { len = 1.5, name = 'Копьё', m = 'timber' } = {}) => { for (const s of [-1, 1]) { G.bar(name, m, w, -s * len * 0.38, y - len * 0.32, s * len * 0.38, y + len * 0.32); if (w) tile('Наконечник', 'iron', w[0], w[1], w[2] + s * len * 0.38, y + len * 0.3, 0.07, 0.2, 0.03, { sink: -0.04 }); } };
  G.chalk = (w, y = 1.5, kind = 'arrow', dir = 1) => {
    const st = (u1, y1, u2, y2) => G.bar('Меловая метка', 'bone', w, u1, y + y1, u2, y + y2, 0.025, 0);
    if (kind === 'cross') { st(-0.18, -0.18, 0.18, 0.18); st(-0.18, 0.18, 0.18, -0.18); }
    else if (kind === 'arrow') { st(-0.3 * dir, 0, 0.3 * dir, 0); st(0.3 * dir, 0, 0.14 * dir, 0.13); st(0.3 * dir, 0, 0.14 * dir, -0.13); }
    else if (w) for (let i = 0; i < 7; i++) tile('Меловая метка', 'bone', w[0], w[1], w[2] + i * 0.06, y, 0.02, R(0.12, 0.16), 0.012, { sink: 0, roll: J(5) });
  };
  G.sconce = (w, y = 1.9) => { if (!w) return; tile('Пустой держатель', 'iron', w[0], w[1], w[2], y, 0.1, 0.3, 0.26, { sink: 0.02, pitch: -20 }); for (let i = 0; i < 3; i++) D.blot('Старая копоть', 'soot', w[0], w[1], w[2] + J(0.1), y + 0.6 + i * 0.22, 0.4 - i * 0.06, 0.35, 0.012, -99); };
  G.claws = (w, y, n = 6, spread = 0.8) => { if (w) for (let i = 0; i < n; i++) tile('Царапины', 'soot', w[0], w[1], w[2] + J(spread), y + J(0.5), 0.02, R(0.2, 0.45), 0.012, { sink: 0, roll: J(16) }); };
  G.quiver = (x, z, { y = G.fy(x, z), n = 7, name = 'Корзина стрел' } = {}) => { put(name, 'straw', x, y, z, 0.32, 0.42, 0.32, { round: true }); for (let i = 0; i < n; i++) beam('Стрела', 'timber', [x + J(0.08), y + 0.1, z + J(0.08)], [x + J(0.16), y + R(0.7, 0.85), z + J(0.16)], 0.018); };
  G.stuck = (x, z, n, { y, spread = 0.6, lean = [0, 0.2] } = {}) => { for (let i = 0; i < n; i++) { const px = x + J(spread), pz = z + J(spread), py = y === undefined ? G.fy(px, pz) : y; beam('Стрела', 'timber', [px, py, pz], [px + lean[0] + J(0.1), py + R(0.45, 0.65), pz + lean[1] + J(0.1)], 0.018); } };
  G.bedroll = (x, z, ry = 0, { y = G.fy(x, z), m = 'cloth' } = {}) => { const hx = Math.sin(ry * RAD), hz = Math.cos(ry * RAD); put('Скатка жильщика', m, x, y + 0.01, z, 0.7, 0.06, 1.8, { ry }); put('Свёрнутый плащ', 'leather', x + hx * 0.72, y + 0.06, z + hz * 0.72, 0.5, 0.14, 0.24, { ry }); put('Заплечный мешок', 'leather', x + hx * 0.75 + hz * 0.55, y, z + hz * 0.75 - hx * 0.55, 0.36, 0.4, 0.3, { ry: ry + 20, round: true }); };
  G.camp = (x, z, { y = G.fy(x, z) } = {}) => { for (let i = 0; i < 8; i++) { const a = i / 8 * 6.283; put('Камень кострища', 'rock', x + Math.cos(a) * 0.45, y, z + Math.sin(a) * 0.45, R(0.18, 0.28), R(0.1, 0.18), R(0.16, 0.22), { ry: R(0, 180) }); } for (let i = 0; i < 3; i++) put('Головня', 'soot', x + J(0.2), y + 0.02, z + J(0.2), 0.08, 0.07, R(0.3, 0.5), { ry: R(0, 180) }); D.patch(x, z, 0.6, { m: 'ash', name: 'Зола', y, n: 4 }); put('Котелок', 'iron', x + 0.62, y, z + 0.3, 0.24, 0.2, 0.24, { round: true }); };
  G.offering = (x, z, { y = G.fy(x, z), n = 5 } = {}) => { for (let i = 0; i < n; i++) { const a = i / n * 6.283, h = R(0.05, 0.18); put('Огарок', 'bone', x + Math.cos(a) * 0.42, y, z + Math.sin(a) * 0.42, 0.05, h, 0.05, { round: true }); put('Кость подношения', 'bone', x + Math.cos(a + 0.6) * 0.24, y, z + Math.sin(a + 0.6) * 0.24, 0.3, 0.05, 0.06, { ry: -a / RAD }); put('Знак Спящих', 'rune', x + Math.cos(a + 0.3) * 0.6, y + 0.004, z + Math.sin(a + 0.3) * 0.6, 0.22, 0.008, 0.04, { ry: 90 - a / RAD }); } put('Череп подношения', 'bone', x, y, z, 0.2, 0.2, 0.24, { ry: R(0, 360) }); };
  G.dead = (x, z, ry = 0, { y = G.fy(x, z), helm } = {}) => {
    const c = Math.cos(ry * RAD), s = Math.sin(ry * RAD), part = (n, m, lx, lz, w, h, d, o = {}) => put(n, m, x + lx * c + lz * s, y, z - lx * s + lz * c, w, h, d, { ...o, ry: ry + (o.ry || 0) });
    part('Череп', 'bone', J(0.05), 0.8, 0.2, 0.2, 0.24, { ry: J(40) }); part('Рёбра', 'bone', 0, 0.42, 0.36, 0.16, 0.42, { round: true }); part('Таз', 'bone', 0, 0.08, 0.3, 0.1, 0.2);
    for (const e of [-1, 1]) { part('Кость руки', 'bone', e * 0.3, 0.4, 0.06, 0.05, 0.6, { ry: e * R(5, 30) }); part('Кость ноги', 'bone', e * 0.12, -0.45, 0.07, 0.06, 0.85, { ry: e * R(0, 14) }); }
    if (helm) part('Шлем', 'iron', 0.34, 0.95, 0.24, 0.2, 0.26, { round: true });
  };
  G.papers = (x, y, z, n = 4, spread = 0.3) => { for (let i = 0; i < n; i++) put(chance(0.3) ? 'Свиток' : 'Лист', 'paper', x + J(spread), y + i * 0.002, z + J(spread), R(0.18, 0.24), 0.008, R(0.26, 0.32), { ry: R(0, 180) }); };
  G.bucket = (x, z, y = G.fy(x, z)) => put('Ведро', 'wood', x, y, z, 0.3, 0.32, 0.3, { round: true });
  G.coil = (x, z, y = G.fy(x, z)) => { put('Бухта верёвки', 'rope', x, y, z, 0.45, 0.1, 0.45, { round: true }); put('Бухта верёвки', 'rope', x, y + 0.09, z, 0.36, 0.08, 0.36, { round: true }); };
  G.lantern = (x, y, z) => { put('Фонарь', 'iron', x, y, z, 0.14, 0.24, 0.14); put('Стекло фонаря', 'glass', x, y + 0.05, z, 0.16, 0.12, 0.1); };
  /// Холодная жаровня: чаша на трёх ногах, зола.
  G.brazier = (x, z, y = G.fy(x, z)) => { for (let i = 0; i < 3; i++) { const a = (i * 120 + 20) * RAD; beam('Нога жаровни', 'iron', [x + Math.cos(a) * 0.3, y, z + Math.sin(a) * 0.3], [x + Math.cos(a) * 0.14, y + 0.66, z + Math.sin(a) * 0.14], 0.045); } put('Холодная жаровня', 'iron', x, y + 0.62, z, 0.7, 0.22, 0.7, { round: true }); put('Зола', 'ash', x, y + 0.8, z, 0.56, 0.05, 0.56, { round: true }); };
  /// Доска или щит, прислонённые к стене: низ в (x, z), верх уходит на (dx, dz) к стене.
  G.lean = (name, m, x, z, dx, dz, h, w, { y = G.fy(x, z), th = 0.05 } = {}) => beam(name, m, [x, y, z], [x + dx, y + h, z + dz], dx ? th : w, dx ? w : th);
  /// Паутина: концы притянуты к ближайшим стенам, чтобы нити не висели в воздухе.
  G.web = (a, b, o) => { const snap = p => { const q = [...p]; for (const k of 'NESW') { const w = S.wall(p[0], p[2], k, p[1] - 0.05, 0.45); if (w) q[k === 'N' || k === 'S' ? 2 : 0] = w[1] + (k === 'N' || k === 'E' ? -0.02 : 0.02); } return q; }; return S.web(snap(a), snap(b), o); };
  /// Стол без тела в блокауте: набор и проножки, которые связывают царгу с ножками.
  G.table = (x, z, w, d, h, y) => { S.set('Стол', x, z, w, d, h, { y }); const yy = (y === undefined ? G.fy(x, z) : y) + h * 0.35; for (const e of [-1, 1]) w >= d ? put('Проножка', 'timber', x + e * (w - 0.3) / 2, yy, z, 0.05, 0.06, d - 0.2) : put('Проножка', 'timber', x, yy, z + e * (d - 0.3) / 2, w - 0.2, 0.06, 0.05); };
  G.shelf = (w, o) => { if (w) D.shelf(...w, o); };
  return G;
};
