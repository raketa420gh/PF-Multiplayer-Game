// Новая: вырытые руками норы в скале на двух ярусах, на −5 круглая камера «Слух» с воронкой, руной и кольцом свечей; только свечи.
const { subtract, rect } = require('../roomkit');
const Y = -5, CX = 1, CZ = -2.6;

exports.options = {};
exports.build = r => {
  r.desc('Вся клетка скала, в ней вырытые руками норы шириной 1,6–2,4 м с потолком 3 м, неровные, с изломами и выпирающими под углом глыбами. Наверху между проёмами пять гнёзд, связанных петлями, и несколько слепых отнорков. Три коротких марша в тесноте уходят на −5, там ходы образуют ещё две петли вокруг круглой камеры Слух диаметром 12,8 м. Посреди неё воронка: уступ на −6,25 и дно на −7,5 с руной, кольцом свечей и лучшим сундуком. Факелов нет, свет дают только свечи в гнёздах.');
  const low = r.level(Y), fun = r.level(-7.5), lid = r.level(-1.4), g = r.level(0, 3);
  let seed = 20261102;
  const rnd = (a, b) => { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return a + (b - a) * (seed / 0x7fffffff); };
  const q = v => Math.round(v * 2.5) / 2.5;
  // Ход: ломаная из рваных кусков.
  const dig = (l, pts, w = 2.1) => {
    for (let i = 0; i + 1 < pts.length; i++) {
      const [x1, z1] = pts[i], [x2, z2] = pts[i + 1], n = Math.max(1, Math.ceil(Math.hypot(x2 - x1, z2 - z1) / 2.2));
      for (let k = 0; k < n; k++) {
        const ax = x1 + (x2 - x1) * k / n, bx = x1 + (x2 - x1) * (k + 1) / n, az = z1 + (z2 - z1) * k / n, bz = z1 + (z2 - z1) * (k + 1) / n, h = w / 2 + rnd(-0.2, 0.2), a = rnd(-0.05, 0.05), b = rnd(-0.05, 0.05);
        l.carve(q(Math.min(ax, bx) - h + a), q(Math.min(az, bz) - h + b), q(Math.max(ax, bx) + h + a), q(Math.max(az, bz) + h + b));
      }
    }
  };
  // Гнездо: три сдвинутых куска вокруг центра.
  const nest = (l, x, z, rx, rz) => {
    l.carve(q(x - rx), q(z - rz * 0.6), q(x + rx * 0.9), q(z + rz * 0.5));
    l.carve(q(x - rx * 0.6), q(z - rz), q(x + rx * 0.5), q(z + rz * 0.9));
    l.carve(q(x - rx * 0.2), q(z - rz * 0.8), q(x + rx), q(z + rz));
  };
  // Круг полосами по общей сетке 0,8 м: уступы воронки совпадают краями.
  const disc = R => { const out = [], n = Math.round(R / 0.8); for (let k = -n; k < n; k++) { const z = k * 0.8, m = Math.max(Math.abs(z), Math.abs(z + 0.8)) - 0.4, h = q(Math.sqrt(R * R - m * m)); out.push(rect(CX - h, CZ + z, CX + h, CZ + z + 0.8)); } return out; };
  // Марш сквозь перекрытие: скала яруса 0 снята с той части, где голова иначе упрётся; пролёт через немую плиту целиком.
  const shaft = (x1, z1, x2, z2, ux1, uz1, ux2, uz2) => { low.carve(x1, z1, x2, z2); lid.carve(x1, z1, x2, z2).hole(x1, z1, x2, z2); g.carve(ux1, uz1, ux2, uz2).hole(ux1, uz1, ux2, uz2); };
  const glyba = (x, z, w, d, ry, y = 0, h = 3) => r.block(x, z, w, d, { y, h, ry });

  // ---------- Верхний ярус ----------
  nest(g, -1, 11, 3.2, 2.4); nest(g, -9, 1.4, 3, 2.6); nest(g, 8, 1, 3, 2.4); nest(g, -8, -5.4, 2.4, 1.8); nest(g, -1, -10, 3, 2.4); nest(g, 6, -8, 2.6, 2);
  dig(g, [[-8.4, 15], [-7, 11.6], [-3.4, 10.4]]);
  dig(g, [[8.4, 15], [7, 12], [4, 10.6], [1.8, 11]]);
  dig(g, [[-15, 8.4], [-12, 8], [-11, 4], [-9.6, 2.6]]);
  dig(g, [[-2.6, 8.6], [-2.4, 7], [-5, 5], [-7.4, 3.2]], 1.8);
  dig(g, [[15, 8.4], [12, 7], [10.4, 3]]);
  dig(g, [[2.6, 9.6], [4, 7.4], [6.6, 4], [7, 2.6]], 1.9);
  dig(g, [[-6.4, 0.6], [-4, -0.6], [1, 0.4], [5.4, 0.2]], 1.8);
  dig(g, [[-15, -8.4], [-11.6, -7.4], [-9, -5.8]]);
  dig(g, [[-9.4, -3.2], [-9.6, -1]], 1.8);
  dig(g, [[-8.4, -15], [-7, -12], [-4, -9.8]]);
  dig(g, [[-3, -8.2], [-5, -6.8], [-6.2, -5.4]], 1.8);
  dig(g, [[8.4, -15], [7, -12.4], [6, -9.6]]);
  dig(g, [[1.6, -10.6], [3.4, -9.6]], 1.8);
  dig(g, [[15, -8.4], [12.4, -7.4], [9, -6.8], [7.4, -7.4]]);
  dig(g, [[5.4, -1], [3.6, -4], [4.8, -6.4]], 1.8);
  // Слепые отнорки.
  dig(g, [[-12, 8.4], [-13, 12.8]], 1.8); nest(g, -14, 13.6, 1.8, 1.6);
  dig(g, [[-11.6, -8], [-13, -11.6]], 1.8); nest(g, -14, -13.2, 1.8, 1.8);
  dig(g, [[7, 12.4], [11, 12.8]], 1.8); nest(g, 13.2, 13.4, 2, 1.8);
  dig(g, [[0, 12.6], [0.4, 15.2]], 1.8);
  dig(g, [[7.4, -11], [11, -11.8], [13, -12.6]], 1.8); nest(g, 13.2, -13, 2, 1.8);
  dig(g, [[-3, -11.6], [-3.4, -14.8]], 1.8);

  // ---------- Марши вниз ----------
  shaft(-13.9, -5, -12.1, 1, -13.9, -3.2, -12.1, 1); r.stairs(-13, -2, 1.8, 6, 5, 0, Y);
  dig(g, [[-13, 2], [-10.8, 2]], 2);
  dig(low, [[-13, -6], [-10.4, -7.4], [-7, -6], [-5.6, -4.6]]);
  shaft(0.1, 3.4, 1.9, 9.4, 0.1, 5.2, 1.9, 9.4); r.stairs(1, 6.4, 1.8, 6, 5, 0, Y);
  shaft(7.4, -3.9, 13.4, -2.1, 9.2, -3.9, 13.4, -2.1); r.stairs(10.4, -3, 1.8, 6, 5, 90, Y);
  dig(g, [[13.4, -3], [14.6, -3], [14.4, -0.6], [12.8, 1.4], [10.4, 1.2]], 2); dig(g, [[14.6, -3.4], [14.6, -5]], 1.8);

  // ---------- Нижний ярус ----------
  for (const d of disc(6.4)) low.carve(d.x1, d.z1, d.x2, d.z2);
  for (const d of disc(6.4)) lid.carve(d.x1, d.z1, d.x2, d.z2).hole(d.x1, d.z1, d.x2, d.z2);
  dig(low, [[-4, 1.8], [-7, 4.4], [-9.6, 8.4], [-7.4, 11]]); nest(low, -6.4, 12, 2.4, 2);
  dig(low, [[6, 1.4], [9, 6], [12.4, 7.4]]); nest(low, 13, 9, 2.4, 2.2);
  dig(low, [[0, -8.8], [-0.6, -10.6]]); nest(low, 1, -12.6, 3, 2);
  dig(low, [[-7, -6.4], [-6, -10.6], [-2.4, -11.4]], 1.8);
  dig(low, [[5.4, -7.2], [8, -8.4], [10.4, -10]]); nest(low, 12.6, -12, 2.4, 2);
  dig(low, [[3.4, -13.4], [7, -13.8], [10.6, -11.6]], 1.8);

  // ---------- Воронка «Слух» ----------
  const inner = disc(3.2), outer = disc(4.8);
  outer.forEach(d => { low.hole(d.x1, d.z1, d.x2, d.z2); fun.carve(d.x1, d.z1, d.x2, d.z2); subtract(d, inner).forEach(p => r.platform(p.x1, p.z1, p.x2, p.z2, -6.25, -7.5)); });
  r.stairs(CX, CZ + 4, 1.8, 1.6, 1.25, 0, -6.25); r.stairs(CX + 4, CZ, 1.8, 1.6, 1.25, 90, -6.25);
  r.stairs(CX, CZ - 2.4, 1.8, 1.6, 1.25, 180, -7.5);
  r.add('fx', 'Rune', [CX, -7.5, CZ], [4.6, 0, 4.6], { ry: 0 });
  r.chest('GoldenChest', CX, CZ, { y: -7.5 });
  for (let i = 0; i < 8; i++) { const a = i * Math.PI / 4 + 0.2, b = 2.3; r.candle(CX + b * Math.cos(a), CZ + b * Math.sin(a), -7.5); }
  r.chest('GoldPile', CX - 1.4, CZ + 0.6, { y: -7.5 }); r.chest('GoldPile', CX + 1.2, CZ + 1, { y: -7.5 });
  r.trap('Spike', CX + 2.8, CZ + 2.8, { y: -6.25 }); r.trap('Vent', CX - 3.9, CZ, { y: -6.25 }); r.trap('Spike', CX, CZ - 1.2, { y: -7.5 });

  // ---------- Глыбы под углом: сбивают прямые линии ----------
  for (const [x, z, w, d, ry] of [[-13.2, 7, 1.6, 0.9, 25], [11.8, 8.6, 1.4, 1, 30], [3.4, 11.8, 1.2, 0.8, 40], [-2.6, 2, 1.6, 1, 20], [10.4, -0.8, 1.2, 0.8, 35], [-10.4, -3.6, 1, 0.8, 30], [-6.6, -9.2, 1.2, 0.8, 25], [10.8, -8.4, 1.4, 0.8, 20], [2.4, -7.4, 1.2, 0.8, 35], [-1.8, 0.8, 0.9, 0.7, 30]]) glyba(x, z, w, d, ry);
  for (const [x, z, w, d, ry] of [[-9.6, 6.4, 1.2, 0.8, 30], [8.8, 3.4, 1.2, 0.8, 25], [-8.6, -7.8, 1.2, 0.8, 35], [-3.8, -11.2, 1, 0.7, 20], [6.6, -9.6, 1.2, 0.8, 30]]) glyba(x, z, w, d, ry, Y);
  // Лежанки и кости: гнёзда обжиты.
  r.prop('Кости', -10, 0.6, 1.4, 1, 0.2); r.prop('Кости', 9, -0.2, 1.4, 1, 0.2); r.prop('Черепа', -2.6, 12.4, 0.8, 0.8, 0.7); r.prop('Обломки', -1, -11.4, 1.4, 1, 0.8, { ry: 30 }); r.prop('Мешки', 6.8, -7, 1.2, 0.9, 0.7, { ry: 15 }); r.prop('Кости', 1.8, -12.8, 1.6, 1, 0.2, { y: Y }); r.prop('Черепа', -6, 12.8, 0.9, 0.9, 0.7, { y: Y });
  r.chest('SmallOakChest', -14.2, 14, { ry: 135 }); r.chest('Urn', -14.4, -13.6); r.chest('Urn', 13.6, 13.8); r.chest('SmallOakChest', 13.8, -13.4, { ry: 225 }); r.chest('Coffin', 0.4, 15.4, { ry: 90 }); r.chest('LargeOakChest', -6.8, 12.8, { y: Y, ry: 135 }); r.chest('LargeOakChest', 13.4, 9.6, { y: Y, ry: 225 }); r.chest('Urn', 13.4, -12.6, { y: Y }); r.chest('Coffin', 1, -13.4, { y: Y, ry: 90 });
  r.trap('Spike', -9.6, 8.4, { y: Y }); r.trap('Spike', 8, -8.4, { y: Y }); r.trap('Spike', -6.4, -8.6, { y: Y }); r.trap('Spike', 9, 6, { y: Y });
  for (const [x, z, y = 0] of [[-1.6, 10.2], [-8.6, 0.4], [7.6, 1.6], [-0.6, -9.2], [5.6, -8.6], [-6, 11.2, Y], [12.6, 8.2, Y], [1.4, -11.8, Y]]) r.candle(x, z, y);
  r.monster('Sword', -1.4, 11.6, { ry: 180 }); r.monster('Skull', 8.4, 0.4, { ry: 270 }); r.monster('Skull', -1, -10.4, { ry: 0 }); r.monster('Sword', 12.6, 9, { y: Y, ry: 270 }); r.monster('Warrior', -7, -6.4, { y: Y, ry: 90 }); r.monster('Mage', CX + 3.9, CZ - 1.6, { y: -6.25, ry: 270 });
  r.start(-8.4, 13.6, 180);
};

// Вид. «Слух»: голая скала и вынутая руками земля, ни доски, ни железа: только камень, кости, тряпьё, свечи и руны.
// История: первую нору вырыла каторжная Сольвейг голыми руками. Её путь от западного гнезда вниз по западному маршу до камеры
// отмечен светящимися отпечатками ладоней, в гнезде висят её кандалы. В камере Слуха лежат те, кто слушал камень и не поднялся.
exports.dress = S => {
  const { D } = S, { put, tile, beam, R, J, pick, chance } = D, DEG = Math.PI / 180;
  S.theme({ rock: 'rock', wall: 'rock', floor: 'dirt', platform: 'rock', stairs: 'rock', arch: 'rock', rings: false, damp: (x, y, z) => y < -6 ? 0.3 : y < -2 ? 0.22 : 0.14, age: 1.2, density: 0.85 });
  S.keep(-14.4, 0.3, 1.3, -13.5, 3.4, 3.2);                                                                       // стена с кандалами Сольвейг: без выступающих камней
  S.auto();


  // ---------- свои помощники ----------
  const on = (x, z, k, y, fn, max = 4.5) => { const w = S.wall(x, z, k, y, max); if (w) fn(w); return w; };
  const loc = (x, z, ry) => { const a = ry * DEG, c = Math.cos(a), s = Math.sin(a); return (lx, lz) => [x + lx * c + lz * s, z - lx * s + lz * c]; };
  const bodies = S.base.filter(o => ['prop', 'chest', 'trap', 'monster'].includes(o.cat) && o.s);
  const busy = (x, z, y, r = 0.9) => bodies.some(o => Math.abs(o.p[1] - y) < 1.5 && Math.abs(o.p[0] - x) < o.s[0] / 2 + r && Math.abs(o.p[2] - z) < o.s[2] / 2 + r);
  const flatAt = (x, z, y) => S.tops(x, z).some(q => Math.abs(q.y - y) < 0.05 && q.b.cat !== 'stairs');
  const ceil = (x, z, y) => { for (let c = y + 1.8; c < y + 5; c += 0.1) if (!S.air(x, c, z)) return c; };
  const candles = (x, y, z, n = 4, sx = 0.18, sz = sx, lit = true) => {
    for (let i = 0; i < n; i++) { const h = R(0.05, 0.2), cx = x + J(sx), cz = z + J(sz); put(lit ? 'Свеча' : 'Огарок', 'bone', cx, y, cz, 0.05, h, 0.05, { round: true }); if (lit) put('Огонёк', 'ember', cx, y + h, cz, 0.025, 0.05, 0.025, { round: true }); }
    put('Наплыв воска', 'bone', x, y, z, sx * 2.2 + 0.1, 0.012, sz * 2.2 + 0.1, { round: true });
  };
  const eye = (w, y, s = 0.5, m = 'rune', crossed) => {
    const [k, plane, u] = w, o = { sink: 0 }, d = 0.1;
    tile('Знак Спящих', m, k, plane, u, y, s * 0.46, 0.05, d, o);
    for (const sg of [-1, 1]) tile('Знак Спящих', m, k, plane, u + sg * s * 0.36, y + s * 0.05, s * 0.3, 0.05, d, { sink: 0, roll: sg * 24 });
    for (const t of [-0.3, 0, 0.3]) tile('Знак Спящих', m, k, plane, u + t * s, y - s * 0.26, 0.05, s * 0.2, d, o);
    if (crossed) for (const rl of [-40, 40]) tile('Знак «здесь тихо»', m, k, plane, u, y - s * 0.45, 0.05, s * 0.9, d + 0.01, { sink: 0, roll: rl });
  };
  const grooves = (w, y0) => { const [k, plane, u] = w, a = J(22), len = R(0.3, 0.6); for (let i = 0; i < 4; i++) tile('Борозда от пальцев', 'soot', k, plane, u + i * 0.045, y0 + J(0.025), 0.02, len * R(0.8, 1), 0.03, { sink: 0, roll: a + J(3) }); };
  const hand = (w, y0) => { const [k, plane, u] = w; tile('Отпечаток ладони', 'rune', k, plane, u, y0, 0.12, 0.13, 0.09, { sink: 0 }); for (let i = 0; i < 4; i++) tile('Отпечаток ладони', 'rune', k, plane, u - 0.048 + i * 0.032, y0 + 0.13, 0.024, i === 1 || i === 2 ? 0.11 : 0.085, 0.09, { sink: 0, roll: (i - 1.5) * -5 }); tile('Отпечаток ладони', 'rune', k, plane, u + 0.085, y0 + 0.04, 0.06, 0.028, 0.09, { sink: 0, roll: 30 }); };
  // Свернувшийся слушатель: лежит на боку, ухо к камню.
  const listener = (x, y, z, ry) => {
    const P = loc(x, z, ry), p = (n, lx, lz, w, h, d, o = {}) => { const [px, pz] = P(lx, lz); put(n, 'bone', px, y, pz, w, h, d, { ...o, ry: ry + (o.ry || 0) }); };
    p('Череп ухом к камню', 0, 0.62, 0.2, 0.16, 0.24, { round: true }); p('Рёбра', 0.04, 0.26, 0.34, 0.14, 0.4, { round: true, ry: 12 }); p('Хребет', -0.08, 0.2, 0.06, 0.05, 0.6, { ry: -12 }); p('Таз', 0.06, -0.14, 0.3, 0.09, 0.2, { ry: 25 });
    p('Кость ноги', 0.28, -0.3, 0.07, 0.06, 0.5, { ry: 65 }); p('Кость ноги', 0.3, -0.52, 0.07, 0.06, 0.5, { ry: -40 }); p('Кость руки', 0.22, 0.5, 0.06, 0.05, 0.44, { ry: 50 }); p('Кисть под головой', 0.02, 0.68, 0.12, 0.03, 0.14);
    const [rx, rz] = P(-0.02, 0.1); put('Истлевшее тряпьё', 'cloth', rx, y, rz, 0.6, 0.02, 1.1, { ry: ry + J(8) });
  };
  const rags = (x, y, z, ry) => { put('Лежанка из тряпья', pick(['cloth', 'leather', 'straw']), x, y, z, 0.72, 0.07, 1.7, { ry: ry + J(5), round: chance(0.3) }); const P = loc(x, z, ry), [hx, hz] = P(0, 0.65); put('Скатка под голову', 'cloth', hx, y + 0.07, hz, 0.46, 0.1, 0.24, { ry }); const [bx, bz] = P(0.5, -0.3); put('Плошка', 'clay', bx, y, bz, 0.18, 0.06, 0.18, { round: true }); };
  const bowls = (x, y, z) => { put('Чаша с осадком', 'clay', x, y, z, 0.26, 0.09, 0.26, { round: true }); put('Светящийся осадок', 'ossarite', x, y + 0.075, z, 0.18, 0.02, 0.18, { round: true }); put('Кувшин', 'clay', x + 0.34, y, z + 0.1, 0.2, 0.3, 0.2, { round: true }); put('Плошка', 'clay', x - 0.2, y, z + 0.3, 0.16, 0.05, 0.16, { round: true }); };
  const heap = (x, y, z) => { put('Отвал вынутой земли', 'dirt', x, y, z, 0.9, 0.3, 0.6, { round: true, ry: R(0, 180) }); put('Отвал вынутой земли', 'dirt', x + J(0.15), y + 0.24, z + J(0.1), 0.5, 0.18, 0.36, { round: true, ry: R(0, 180) }); for (let i = 0; i < 4; i++) put('Камень из отвала', 'rock', x + J(0.5), y, z + J(0.4), R(0.12, 0.26), R(0.08, 0.16), R(0.12, 0.22), { ry: R(0, 180), roll: J(14) }); put('Скребок из кости', 'bone', x + 0.5, y, z + 0.3, 0.05, 0.03, 0.3, { ry: R(0, 180) }); };
  const store = (x, y, z) => { for (let i = 0; i < 4; i++) put(pick(['Горшок', 'Кувшин', 'Корчага']), 'clay', x + (i % 2) * 0.32 - 0.16 + J(0.03), y, z + Math.floor(i / 2) * 0.32 - 0.16, R(0.2, 0.28), R(0.2, 0.36), R(0.2, 0.28), { round: true }); D.fungus(x + 0.5, z, 5, { y, spread: 0.18 }); put('Связка сушёных грибов', 'fungus', x - 0.45, y, z, 0.3, 0.1, 0.2, { ry: R(0, 90) }); };
  const charms = (x, y, z) => { const c = ceil(x, z, y); if (!c || c - y > 4) return; const len = c - y - R(1.75, 2.1); put('Шнур оберега', 'rope', x, c - len, z, 0.015, len + 0.05, 0.015); for (let t = 0.05; t < len - 0.1; t += 0.25) put('Костяной оберег', 'bone', x + J(0.03), c - len + t, z + J(0.03), 0.04, 0.17, 0.04, { roll: J(25) }); };
  const teeth = (x, z, y) => { const w = S.nearWall(x, z, y + 1.4, 3); if (!w) return; for (let i = -1; i <= 1; i++) tile('Знак зубов', 'rune', w[0], w[1], w[2] + i * 0.16, y + 1.3, 0.09, 0.26 - Math.abs(i) * 0.06, 0.1, { sink: 0, roll: i * 8 }); };

  // ================= ходы: следы босых ног, борозды от пальцев, свечные лунки, отвалы, корни, обереги =================
  const UP = [[[-8.4, 15], [-7, 11.6], [-3.4, 10.4]], [[8.4, 15], [7, 12], [4, 10.6], [1.8, 11]], [[-15, 8.4], [-12, 8], [-11, 4], [-9.6, 2.6]], [[-2.6, 8.6], [-2.4, 7], [-5, 5], [-7.4, 3.2]], [[15, 8.4], [12, 7], [10.4, 3]], [[2.6, 9.6], [4, 7.4], [6.6, 4], [7, 2.6]], [[-6.4, 0.6], [-4, -0.6], [1, 0.4], [5.4, 0.2]],
    [[-15, -8.4], [-11.6, -7.4], [-9, -5.8]], [[-9.4, -3.2], [-9.6, -1]], [[-8.4, -15], [-7, -12], [-4, -9.8]], [[-3, -8.2], [-5, -6.8], [-6.2, -5.4]], [[8.4, -15], [7, -12.4], [6, -9.6]], [[1.6, -10.6], [3.4, -9.6]], [[15, -8.4], [12.4, -7.4], [9, -6.8], [7.4, -7.4]], [[5.4, -1], [3.6, -4], [4.8, -6.4]],
    [[-12, 8.4], [-13, 12.8]], [[-11.6, -8], [-13, -11.6]], [[7, 12.4], [11, 12.8]], [[0, 12.6], [0.4, 15.2]], [[7.4, -11], [11, -11.8], [13, -12.6]], [[-3, -11.6], [-3.4, -14.8]], [[-13, 2], [-10.8, 2]], [[13.4, -3], [14.6, -3], [14.4, -0.6], [12.8, 1.4], [10.4, 1.2]]];
  const LOW = [[[-13, -6], [-10.4, -7.4], [-7, -6], [-5.6, -4.6]], [[-4, 1.8], [-7, 4.4], [-9.6, 8.4], [-7.4, 11]], [[6, 1.4], [9, 6], [12.4, 7.4]], [[0, -8.8], [-0.6, -10.6]], [[-7, -6.4], [-6, -10.6], [-2.4, -11.4]], [[5.4, -7.2], [8, -8.4], [10.4, -10]], [[3.4, -13.4], [7, -13.8], [10.6, -11.6]]];
  const walk = (pts, step, fn) => { for (let i = 0; i + 1 < pts.length; i++) { const [x1, z1] = pts[i], [x2, z2] = pts[i + 1], len = Math.hypot(x2 - x1, z2 - z1), n = Math.max(1, Math.round(len / step)); for (let k = 0; k < n; k++) { const t = (k + 0.5) / n; fn(x1 + (x2 - x1) * t, z1 + (z2 - z1) * t, (x2 - x1) / len, (z2 - z1) / len); } } };
  let stepNo = 0;
  for (const [paths, y] of [[UP, 0], [LOW, -5]]) for (const pts of paths) walk(pts, 1.05, (x, z, dx, dz) => {
    if (!flatAt(x, z, y)) return;
    const s = stepNo++ % 2 ? 1 : -1;
    if (!busy(x, z, y, 0.3)) put('След босой ноги', 'soot', x - dz * 0.14 * s, y + 0.004, z + dx * 0.14 * s, 0.1, 0.012, 0.24, { ry: Math.atan2(dx, dz) / DEG + s * 8, round: true });
    for (const k of Math.abs(dx) > Math.abs(dz) ? ['N', 'S'] : ['E', 'W']) {
      const w = S.wall(x, z, k, y + 1.2, 2.4); if (!w) continue;
      const [fx, fz] = D.at(w[0], w[1], w[2], 0.4), roll = D.rnd();
      if (chance(0.55)) grooves(w, y + R(0.5, 1.7)); if (chance(0.2)) grooves([w[0], w[1], w[2] + R(0.2, 0.4)], y + R(0.3, 1.2));
      if (roll < 0.07) { tile('Свечная лунка', 'soot', w[0], w[1], w[2], y + 1.15, 0.3, 0.3, 0.1, { sink: 0.06 }); const [cx, cz] = D.at(w[0], w[1], w[2], 0.07); put('Огарок', 'bone', cx, y + 1.15, cz, 0.05, R(0.04, 0.1), 0.05, { round: true }); tile('Потёк воска', 'bone', w[0], w[1], w[2], y + 0.75, 0.07, 0.4, 0.07, { sink: 0 }); }
      else if (roll < 0.17 && flatAt(fx, fz, y) && !busy(fx, fz, y, 0.5)) { put('Ком вынутой земли', 'dirt', fx, y, fz, R(0.4, 0.6), R(0.12, 0.2), R(0.3, 0.45), { round: true, ry: R(0, 180) }); for (let i = 0; i < 3; i++) put('Камень из отвала', 'rock', fx + J(0.35), y, fz + J(0.35), R(0.1, 0.22), R(0.07, 0.14), R(0.1, 0.2), { ry: R(0, 180), roll: J(14) }); }
      else if (roll < 0.25) { const c = ceil(fx, fz, y); if (c && c - y < 4) S.roots(fx, c, fz, { n: 4, len: R(0.6, 1.1), spread: 0.25 }); }
      else if (roll < 0.29 && !busy(fx, fz, y, 0.3)) charms(fx, y, fz);
      else if (roll < 0.34 && flatAt(fx, fz, y)) D.bones(fx, fz, 2, { y, spread: 0.15 });
    }
  });

  // ================= путь Сольвейг: кандалы в западном гнезде, отпечатки ладоней вниз по западному маршу до камеры =================
  on(-13, 2.25, 'W', 1.4, w => {
    const [k, plane, u] = w, [x, z] = D.at(k, plane, u, 0.08);
    tile('Костяной колышек', 'bone', k, plane, u, 1.75, 0.05, 0.05, 0.16, { sink: 0 }); D.chain(x + 0.06, 1.75, z, 0.75, { name: 'Цепь кандалов Сольвейг' }); for (const dz of [-0.09, 0.09]) put('Кандалы Сольвейг', 'iron', x + 0.06, 0.82, z + dz, 0.15, 0.06, 0.15, { round: true });
    for (const [du, dy] of [[-0.5, 1.3], [0.5, 1.4], [-0.75, 1.9], [0.75, 1.85], [-0.3, 2.2], [0.3, 2.25], [0, 0.55]]) hand([k, plane, u + du], dy); eye([k, plane, u], 2.75, 0.9);
    const [cx, cz] = D.at(k, plane, u, 0.35); candles(cx - 0.1, 0, cz - 0.45, 3, 0.07, 0.1); candles(cx - 0.1, 0, cz + 0.55, 3, 0.07, 0.1); put('Лоскут арестантской робы', 'cloth', cx + 0.15, 0, cz, 0.5, 0.03, 0.6, { ry: 12 });
  });
  for (let i = 0; i < 4; i++) { const x = -10.6 - i * 0.7; on(x, 2, i % 2 ? 'N' : 'S', 1.3, w => hand(w, R(1.1, 1.5)), 2.2); }
  for (let i = 0; i < 8; i++) { const z = 0.6 - i * 0.75, y = -5 + (z + 5) / 6 * 5; on(-13, z, i % 2 ? 'W' : 'E', y + 1.3, w => { hand(w, y + R(1, 1.4)); if (i % 3 === 0) grooves([w[0], w[1], w[2] + 0.3], y + 0.6); }, 1.6); }
  walk(LOW[0], 1.25, (x, z, dx, dz) => { for (const k of Math.abs(dx) > Math.abs(dz) ? [chance(0.5) ? 'N' : 'S'] : [chance(0.5) ? 'E' : 'W']) on(x, z, k, -3.7, w => hand(w, -5 + R(1, 1.45)), 2.4); });
  for (let i = 0; i < 3; i++) put('Отпечаток ладони на полу', 'rune', -5 + i * 0.5, -4.99, -4.3 + i * 0.22, 0.12, 0.014, 0.2, { ry: 70 + J(12) });

  // ================= гнёзда =================
  const furnish = (cx, cz, y, plan) => {
    for (const k of 'NESW') {
      const what = plan[k], w = what && S.wall(cx, cz, k, y + 1.3, 4.6); if (!w) continue;
      const [fx, fz] = D.at(w[0], w[1], w[2], 0.55), ok = flatAt(fx, fz, y) && !busy(fx, fz, y, 0.75);
      if (what === 'eye') { eye(w, y + 1.75, 0.8); if (ok) candles(...[D.at(w[0], w[1], w[2], 0.3)[0], y, D.at(w[0], w[1], w[2], 0.3)[1]], 3, 0.12, 0.12, false); }
      else if (what === 'crossed') { eye(w, y + 1.6, 0.7, 'soot', true); grooves(w, y + 0.8); grooves([w[0], w[1], w[2] + 0.5], y + 1.1); grooves([w[0], w[1], w[2] - 0.5], y + 0.6); }
      else if (!ok) continue;
      else if (what === 'bed') rags(fx, y, fz, k === 'N' || k === 'S' ? 90 : 0);
      else if (what === 'bowls') bowls(fx, y, fz);
      else if (what === 'heap') heap(fx, y, fz);
      else if (what === 'store') store(fx, y, fz);
      else if (what === 'charms') charms(fx, y, fz);
    }
  };
  furnish(-1, 11, 0, { N: 'bed', E: 'eye', S: 'bowls', W: 'bed' }); furnish(-9, 1.4, 0, { N: 'bowls', S: 'bed' }); furnish(8, 1, 0, { N: 'store', E: 'bed', S: 'eye', W: 'charms' }); furnish(-8, -5.4, 0, { W: 'heap', S: 'bed', N: 'charms' });
  furnish(-1, -10, 0, { N: 'eye', E: 'bed', W: 'bowls' }); furnish(6, -8, 0, { N: 'bed', E: 'store', S: 'eye' });
  furnish(-14, 13.6, 0, { W: 'eye', S: 'bed' }); furnish(-14, -13.2, 0, { N: 'bed', W: 'crossed' }); furnish(13.2, 13.4, 0, { E: 'crossed', S: 'bed' }); furnish(13.2, -13, 0, { E: 'eye', N: 'bed' });
  furnish(-6.4, 12, -5, { W: 'bed', E: 'eye', S: 'bowls' }); furnish(13, 9, -5, { E: 'eye', S: 'bed' }); furnish(1, -12.6, -5, { W: 'bed', E: 'bowls', N: 'charms' }); furnish(12.6, -12, -5, { E: 'eye', S: 'bed', N: 'bowls' });
  // Северное гнездо: привратное, лежанки вокруг кучи черепов. Восточное: кладовая. Юго-восточное: мешки с вынутой землёй.
  for (let i = 0; i < 5; i++) put('Череп на камне', 'bone', -2.6 + J(0.6), 0, 12.4 + J(0.6) - 0.7, 0.2, 0.2, 0.24, { ry: R(0, 360), round: true }); heap(7.4, 0, -5.9); for (let i = 0; i < 3; i++) put('Пустой мешок', 'cloth', 5.6 + i * 0.1, i * 0.03, -6.1, 0.5, 0.03, 0.7, { ry: J(15) });
  // Южное гнездо: стража засыпала ходы, но не успевала. Имперская лопата и шлем в завале — единственное железо, кроме кандалов.
  D.rubble(-1, -11.4, 1.3, 16, { m: 'rock', pile: 0.3, big: 0.3, name: 'Камень засыпки' }); beam('Имперская лопата', 'timber', [-0.2, 0.1, -11.2], [0.5, 0.9, -11.8], 0.045); put('Штык лопаты', 'iron', -0.2, 0, -11.15, 0.2, 0.28, 0.03, { ry: 50, pitch: 30 }); put('Смятый шлем стражника', 'iron', -1.9, 0, -10.9, 0.26, 0.16, 0.28, { round: true, roll: 20 }); put('Кость руки', 'bone', -1.7, 0, -11.3, 0.06, 0.05, 0.6, { ry: 60 });
  // Слепые отнорки: на севере Спящий, похороненный в собственной норе; на юге рыли до конца и ничего не услышали.
  on(0.4, 14.4, 'N', 1.6, w => eye(w, 1.9, 0.7), 2.5); candles(-0.3, 0, 14.3, 3, 0.1, 0.1, false); candles(1.1, 0, 14.4, 2, 0.08, 0.08, false);
  on(-3.4, -14, 'S', 1.5, w => { eye(w, 1.5, 0.7, 'soot', true); for (let i = 0; i < 5; i++) grooves([w[0], w[1], w[2] + J(0.6)], R(0.3, 1.9)); }, 2.5); listener(-3.5, 0, -14.5, 80); put('Корзина для земли', 'straw', -2.9, 0, -13.6, 0.44, 0.3, 0.44, { round: true }); put('Земля в корзине', 'dirt', -2.9, 0.25, -13.6, 0.36, 0.08, 0.36, { round: true }); put('Скребок из кости', 'bone', -3.9, 0, -13.7, 0.05, 0.03, 0.3, { ry: 40 });
  // Нижнее северо-восточное гнездо: сырой оссарит сочится из трещины в чашу. Его пьют.
  on(13, 9, 'N', -3.8, w => { const [k, plane, u] = w; for (let i = 0, uu = u - 0.2, y = -2.6; i < 6; i++) { tile('Светящаяся трещина', 'ossarite', k, plane, uu, y - 0.45, 0.05, 0.5, 0.1, { sink: 0.01, roll: J(30) }); uu += J(0.14); y -= 0.42; } const [x, z] = D.at(k, plane, u - 0.1, 0.25); if (flatAt(x, z, -5)) { put('Каменная чаша', 'rock', x, -5, z, 0.5, 0.2, 0.5, { round: true }); put('Сырой оссарит', 'ossarite', x, -4.82, z, 0.38, 0.03, 0.38, { round: true }); put('Черпак из черепа', 'bone', x + 0.4, -5, z + 0.1, 0.18, 0.1, 0.2, { round: true }); } });

  // ================= камера Слуха =================
  const CX = 1, CZ = -2.6, P = (r, a) => [CX + Math.sin(a * DEG) * r, CZ + Math.cos(a * DEG) * r];
  // Слушатели: на кольце и на уступе, вдоль стены, ухом к камню. Рядом слуховые рожки и миски, к которым не вернулись.
  for (const [a, r, y] of [[28, 5.85, -5], [72, 5.8, -5], [112, 5.85, -5], [163, 5.85, -5], [222, 5.85, -5], [283, 5.85, -5], [335, 5.85, -5], [35, 4.3, -6.25], [150, 4.3, -6.25], [215, 4.35, -6.25], [315, 4.3, -6.25]]) {
    const [x, z] = P(r, a); if (!flatAt(x, z, y) || busy(x, z, y, 0.5)) continue;
    listener(x, y, z, a + 90); const [hx, hz] = P(r - 0.45, a + 9); if (flatAt(hx, hz, y)) { put('Слуховой рожок', 'bone', hx, y, hz, 0.2, 0.1, 0.2, { round: true }); put('Слуховой рожок', 'bone', hx, y + 0.1, hz, 0.1, 0.22, 0.1, { round: true }); }
    const [bx, bz] = P(r - 0.3, a - 10); if (flatAt(bx, bz, y)) put('Пустая плошка', 'clay', bx, y, bz, 0.18, 0.06, 0.18, { round: true });
  }
  // Рунные насечки по кругу, лунки от голов, светящиеся трещины в дне: то, что здесь слышно.
  for (const [r, y, n] of [[5.45, -5, 30], [4, -6.25, 22]]) for (let i = 0; i < n; i++) { const a = i * 360 / n + 4, [x, z] = P(r, a); if (flatAt(x, z, y) && !busy(x, z, y, 0.2) && i % 5 !== 4) put('Рунная насечка', 'rune', x, y + 0.002, z, 0.36, 0.014, 0.05, { ry: a + 90 }); }
  for (const a of [50, 95, 138, 190, 250, 300, 350]) { const [x, z] = P(5.3, a); if (flatAt(x, z, -5)) put('Лунка от головы', 'soot', x, -4.995, z, 0.34, 0.012, 0.28, { round: true, ry: a }); }
  for (let i = 0; i < 7; i++) { let a = i * 51 + 12, r = 0.7; for (let j = 0; j < 4; j++) { const [x, z] = P(r, a), len = R(0.4, 0.65); if (flatAt(x, z, -7.5) && !busy(x, z, -7.5, 0.05)) put('Светящаяся трещина', 'ossarite', x, -7.495, z, 0.045, 0.014, len, { ry: a + J(25) }); r += len * 0.85; a += J(14); } }
  for (const [x, z, k] of [[4, 2.2, 'N'], [-2.4, 2.2, 'N'], [6.4, -1, 'E'], [6.3, -5.6, 'E'], [-4.2, -0.6, 'W'], [-4, -5, 'W'], [-2.6, -7.2, 'S'], [3.6, -7.8, 'S']]) { on(x, z, k, -3.3, w => eye(w, -3.2, 1.2), 3); for (const du of [-0.9, 0.9]) on(x + (k === 'N' || k === 'S' ? du : 0), z + (k === 'E' || k === 'W' ? du : 0), k, -3.7, w => hand(w, -3.9 + J(0.2)), 2.2); }
  // Стенки воронки: ладони тех, кто спускался слушать ближе; на дне подношения вокруг сундука.
  for (let a = 20; a < 360; a += 36) { const [x, z] = P(2.6, a), k = a < 45 || a > 315 ? 'N' : a < 135 ? 'E' : a < 225 ? 'S' : 'W'; on(x, z, k, -7, w => hand(w, -7.5 + R(0.5, 0.9)), 1.6); }
  for (const a of [70, 160, 250, 340]) { const [x, z] = P(2.75, a); if (flatAt(x, z, -7.5) && !busy(x, z, -7.5, 0.3)) { put('Чаша с осадком', 'clay', x, -7.5, z, 0.26, 0.09, 0.26, { round: true }); put('Светящийся осадок', 'ossarite', x, -7.425, z, 0.18, 0.02, 0.18, { round: true }); for (let i = 0; i < 4; i++) put('Фаланга-подношение', 'bone', x + J(0.25), -7.5, z + J(0.25), 0.03, 0.03, 0.09, { ry: R(0, 180) }); } }
  for (const a of [10, 100, 200, 280]) { const [x, z] = P(6, a); charms(x, -5, z); }

  // ================= мелочь: комья и щебень по всем ходам, подношения у каждой свечи, скарб по гнёздам =================
  for (const y of [0, -5]) for (let i = 0; i < 620; i++) {
    const x = R(-16, 16), z = R(-16, 16); if (!flatAt(x, z, y) || busy(x, z, y, 0.2)) continue;
    const kind = D.rnd();
    if (kind < 0.45) put('Ком земли', 'dirt', x, y - 0.01, z, R(0.12, 0.3), R(0.05, 0.12), R(0.12, 0.26), { ry: R(0, 180), roll: J(10) });
    else if (kind < 0.8) put('Щебень', 'rock', x, y - 0.01, z, R(0.08, 0.22), R(0.04, 0.1), R(0.08, 0.2), { ry: R(0, 180), roll: J(14) });
    else if (kind < 0.92) put('Обломок кости', 'bone', x, y, z, 0.04, 0.035, R(0.1, 0.3), { ry: R(0, 180) });
    else put('Капля воска', 'bone', x, y, z, R(0.06, 0.14), 0.012, R(0.06, 0.12), { round: true });
  }
  for (const o of S.base.filter(o => o.cat === 'light' && o.name === 'Candle')) {
    const [x, , z] = o.p, y = S.floor(x, z, o.p[1] - 0.3); if (y === undefined) continue;
    for (let i = 0; i < 6; i++) { const a = R(0, 360) * DEG, r = R(0.3, 0.6), px = x + Math.cos(a) * r, pz = z + Math.sin(a) * r; if (!flatAt(px, pz, y)) continue; i < 3 ? put('Огарок', 'bone', px, y, pz, 0.05, R(0.03, 0.1), 0.05, { round: true }) : put('Фаланга-подношение', 'bone', px, y, pz, 0.03, 0.03, 0.09, { ry: R(0, 180) }); }
    put('Наплыв воска', 'bone', x, y, z, 0.8, 0.014, 0.7, { round: true, ry: R(0, 180) });
  }
  for (const [cx, cz, y] of [[-1, 11, 0], [-9, 1.4, 0], [8, 1, 0], [-8, -5.4, 0], [-1, -10, 0], [6, -8, 0], [-14, 13.6, 0], [-14, -13.2, 0], [13.2, 13.4, 0], [13.2, -13, 0], [-6.4, 12, -5], [13, 9, -5], [1, -12.6, -5], [12.6, -12, -5]]) for (let i = 0; i < 9; i++) {
    const x = cx + J(2.6), z = cz + J(2.2), w = flatAt(x, z, y) && !busy(x, z, y, 0.5) && S.nearWall(x, z, y + 0.4, 0.8); if (!w) continue;
    const kind = D.rnd();
    if (kind < 0.25) { put('Узелок', 'cloth', x, y, z, 0.3, 0.2, 0.26, { round: true, ry: R(0, 180) }); put('Завязка узелка', 'rope', x, y + 0.2, z, 0.08, 0.05, 0.08, { round: true }); }
    else if (kind < 0.45) for (let j = 0; j < 7; j++) put('Костяная бусина чёток', 'bone', x + Math.cos(j * 0.9) * 0.14, y, z + Math.sin(j * 0.9) * 0.14, 0.04, 0.03, 0.04, { round: true });
    else if (kind < 0.65) { put('Плошка', 'clay', x, y, z, 0.18, 0.06, 0.18, { round: true }); if (chance(0.5)) put('Светящийся осадок', 'ossarite', x, y + 0.05, z, 0.12, 0.02, 0.12, { round: true }); }
    else if (kind < 0.85) put('Тряпьё', pick(['cloth', 'leather']), x, y, z, R(0.4, 0.7), 0.04, R(0.3, 0.5), { ry: R(0, 180) });
    else put('Скребок из кости', 'bone', x, y, z, 0.05, 0.03, 0.3, { ry: R(0, 180) });
  }

  // ================= ловушки: знак зубов на стене, кровь и кости рядом =================
  for (const o of bodies.filter(o => o.cat === 'trap')) {
    const [x, y, z] = o.p; teeth(x, z, y);
    for (let i = 0; i < 3; i++) { const a = R(0, 360) * DEG, px = x + Math.cos(a) * 0.95, pz = z + Math.sin(a) * 0.95; if (!flatAt(px, pz, y)) continue; if (o.name === 'Vent') put('Сдутая пыль', 'plaster', px, y, pz, 0.14, 0.012, 0.6, { ry: 90 - a / DEG }); else if (i) D.bones(px, pz, 3, { y, spread: 0.2 }); else S.pool(px, pz, 0.7, 0.5, { m: 'blood', y, name: 'Старая кровь' }); }
  }
};
