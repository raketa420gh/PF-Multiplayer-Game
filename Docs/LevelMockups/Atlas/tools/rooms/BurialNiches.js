// Правка: комната сложнее и детальнее, ниши — настоящие ниши, а не прямоугольники, места у дверей прикрыть холлами.
const NICHE_W = 2, NICHE_D = 1.2, LINTEL_Y = 2.4, TOP = 5, UP_H = 4.4;
const SIDE = { N: (u, v) => [u, 16.8 - v], S: (u, v) => [u, -16.8 + v], E: (u, v) => [16.8 - v, u], W: (u, v) => [-16.8 + v, u] };
const N = [0, 1], S = [0, -1], E = [1, 0], W = [-1, 0];

exports.options = {};
exports.build = r => {
  r.desc('Высеченный в скале некрополь: кольцевая галерея вокруг центрального склепа, стены которой сплошь в нишах глубиной 1,2 м с перемычками, полками и гробами в два ряда. Каждый проём ведёт сначала в тесный холл-тамбур с изломом, из него в галерею, поэтому от дверей ничего не простреливается. В склеп ведут дверь с юга и арка с востока, кольцо и склеп дают две петли. Два марша поднимают на верхний ярус ниш над северной половиной кольца: оттуда балкон смотрит в склеп, а за дверью на севере стоит родовая усыпальница.');
  const g = r.level(0), up = r.level(TOP, UP_H);

  // Кольцо и центральный склеп.
  g.carve(-12, 9, 12, 12); g.carve(-12, -12, 12, -9); g.carve(-12, -9, -9, 9); g.carve(9, -9, 12, 9);
  g.carve(-5, -5, 5, 5);
  g.carve(-4.2, -9, -2.2, -5);
  r.door(-3.2, -7, 0);
  r.block(-3.2, -7, 2, 4, { y: 2.4, h: 2 });
  g.carve(5, 1, 9, 3);
  r.block(7, 2, 4, 2, { y: 2.4, h: 2 });

  // Холлы-тамбуры: проём → поперечный холл → выход в кольцо со сдвигом.
  const hall = (side, a, b, e0, e1) => {
    const [x1, z1] = SIDE[side](a, 1.8), [x2, z2] = SIDE[side](b, 4), [x3, z3] = SIDE[side](e0, 4), [x4, z4] = SIDE[side](e1, 4.8);
    g.carve(x1, z1, x2, z2); g.carve(x3, z3, x4, z4);
    const [lx, lz] = SIDE[side]((e0 + e1) / 2, 4.4), alongX = side === 'N' || side === 'S';
    r.block(lx, lz, alongX ? Math.abs(e1 - e0) : 0.8, alongX ? 0.8 : Math.abs(e1 - e0), { y: 2.4, h: 2 });
  };
  hall('N', -9.6, -4.2, -6.2, -4.2);
  hall('N', 7.2, 11.6, 9.6, 11.6);
  hall('E', 4, 9.6, 4, 6);
  hall('E', -11.6, -7.2, -11.6, -9.6);
  hall('S', 3.4, 9.6, 3.4, 5.4);
  hall('S', -11.6, -7.2, -11.6, -9.6);
  hall('W', 7.2, 11.6, 9.6, 11.6);
  hall('W', -9.6, -4, -6, -4);

  // Ниша: выемка в скале, перемычка спереди; kind: 0 два ряда гробов на полке, 1 один гроб, 2 урны, 3 обвал; loot — контейнер вместо нижнего гроба.
  const niche = (lv, x, z, [nx, nz], kind, loot) => {
    const alongX = nz !== 0, y = lv.y, h = lv === g ? 4.4 : UP_H, hw = NICHE_W / 2, px = v => x + nx * v, pz = v => z + nz * v;
    const wd = d => (alongX ? [NICHE_W, d] : [d, NICHE_W]);
    alongX ? lv.carve(x - hw, z, x + hw, pz(NICHE_D)) : lv.carve(x, z - hw, px(NICHE_D), z + hw);
    r.block(px(0.2), pz(0.2), ...wd(0.4), { y: y + LINTEL_Y, h: h - LINTEL_Y });
    const ry = alongX ? 0 : 90, cx = px(0.75), cz = pz(0.75);
    if (loot) r.chest(loot, cx, cz, { y, ry });
    else if (kind === 2) r.chest('Urn', cx, cz, { y, ry });
    else if (kind === 3) r.prop('Обломки', px(0.6), pz(0.6), ...wd(1), 0.9, { y });
    else r.prop('Гроб', cx, cz, ...wd(0.8), 0.7, { y });
    if (kind === 0) {
      r.block(px(0.6), pz(0.6), ...wd(NICHE_D), { y: y + 1.2, h: 0.15 });
      r.prop('Гроб', cx, cz, ...wd(0.8), 0.7, { y: y + 1.35 });
    }
  };
  // Наружные стены кольца.
  [[-2.2, 0], [1, 1], [4.2, 0]].forEach(([x, k]) => niche(g, x, 12, N, k));
  [[-5, 1], [-1.8, 0], [1.4, 3]].forEach(([x, k]) => niche(g, x, -12, S, k));
  niche(g, 10.8, -12, S, 0);
  niche(g, 12, 10.8, E, 2);
  niche(g, -12, 5.8, W, 1); niche(g, -12, -10.8, W, 0);
  // Стены склепа со стороны кольца.
  [[-6.4, 0], [-3.2, 0], [0, 2], [3.2, 0], [6.4, 1]].forEach(([x, k], i) => niche(g, x, 9, S, k, i === 2 ? 'Coffin' : undefined));
  [[-6.4, 1], [0, 0], [3.2, 3], [6.4, 0]].forEach(([x, k]) => niche(g, x, -9, N, k));
  [[-6.4, 0], [-3.2, 1], [0, 0], [3.2, 2], [6.4, 0]].forEach(([z, k]) => niche(g, -9, z, E, k));
  [[-6.4, 0], [-3.2, 1], [-0.4, 0], [5, 0], [7.4, 3]].forEach(([z, k]) => niche(g, 9, z, W, k));
  // Ниши в самом склепе.
  [[-3.2, 0], [0, 0], [3.2, 0]].forEach(([x, k]) => niche(g, x, 5, N, k));
  [[1.4, 1], [-1.6, 0]].forEach(([z, k]) => niche(g, -5, z, W, k));

  // Склеп: возвышение с саркофагом, колонны под балконом.
  r.platform(-2.4, -3.2, 2.4, 0.6, 0.25);
  r.prop('Саркофаг', 0, -1.3, 2.4, 1.2, 1, { y: 0.25 });
  r.chest('LargeOakChest', 3.8, -3.8, { ry: 270 });
  r.column(-2.4, 3, { s: 0.8, h: 4.4 });
  r.column(2.4, 3, { s: 0.8, h: 4.4 });

  // Марши на верхний ярус в карманах кольца.
  g.carve(-14.4, -3.6, -12, 4.4);
  r.stairs(-13.2, 1.2, 2.4, 6.4, TOP, 0, 0);
  up.carve(-14.4, -2, -12, 4.4); up.hole(-14.4, -2, -12, 4.4);
  up.carve(-14.4, 4.4, -12, 7);
  g.carve(12, -5, 14.4, 3.2);
  r.stairs(13.2, 0, 2.4, 6.4, TOP, 0, 0);
  up.carve(12, -3.2, 14.4, 3.2); up.hole(12, -3.2, 14.4, 3.2);
  up.carve(12, 3.2, 14.4, 5.6);

  // Верхний ярус: северная половина кольца.
  up.carve(-12, 9, 12, 12); up.carve(-12, 4.4, -9, 9); up.carve(9, 3.2, 12, 9);
  [[-8.2, 0], [-5, 2], [5, 0], [8.2, 1]].forEach(([x, k]) => niche(up, x, 12, N, k));
  [[-6.4, 1], [-3.2, 0], [3.2, 0], [6.4, 2]].forEach(([x, k]) => niche(up, x, 9, S, k));
  niche(up, -9, 6.4, E, 0); niche(up, 9, 6.4, W, 0, 'SmallOakChest'); niche(up, 12, 8, E, 0); niche(up, -12, 9.2, W, 3);

  // Балкон над склепом и родовая усыпальница.
  up.carve(-5, -5, 5, 5);
  up.hole(-5, -5, 5, 3); up.hole(-5, 3, -2.4, 5); up.hole(2.4, 3, 5, 5);
  up.carve(-1.2, 5, 1.2, 9);
  for (const [x, z, w, d] of [[-2.25, 4, 0.3, 2], [2.25, 4, 0.3, 2], [0, 3.15, 4.8, 0.3]]) r.prop('Парапет', x, z, w, d, 1, { y: TOP });
  up.carve(-1, 12, 1, 12.6);
  up.carve(-3.2, 12.6, 3.2, 16.2);
  r.door(0, 12.3, 0, { y: TOP });
  r.block(0, 12.3, 2, 0.6, { y: TOP + 2.4, h: UP_H - 2.4 });
  r.chest('GoldenChest', 0, 15.4, { y: TOP, ry: 180 });
  r.prop('Саркофаг', -2.2, 14.6, 1.2, 2.4, 1, { y: TOP });
  r.prop('Саркофаг', 2.2, 14.6, 1.2, 2.4, 1, { y: TOP });

  // Укрытия в кольце и обвал на углу.
  r.prop('Обломки', -11.2, -8.2, 1.2, 2.2, 1.1);
  r.prop('Обломки', -7, -11.2, 2.2, 1.2, 1.1);
  r.prop('Кости', -7.4, 14.2, 1.6, 1, 0.2);
  r.prop('Кости', 14, -9.8, 1, 1.6, 0.2);
  r.trap('Spike', 0.6, 10.5);
  r.trap('Spike', 10.5, -1.4);

  // Свет.
  r.torch(-0.6, 11.8, 180);
  r.torch(-11.8, -11, 90);
  r.torch(11.8, -6.8, 270);
  r.brazier(-3.6, 3.6);
  r.candle(1.4, -3.6, 0.25);
  r.torch(0, 11.8, 180, TOP);
  r.torch(-9.2, 10.5, 90, TOP);
  r.candle(0, 13.6, TOP);

  r.monster('Sword', -10.5, 2, { ry: 0 });
  r.monster('Sword', 3, -10.5, { ry: 90 });
  r.monster('Skull', 3.4, 2, { ry: 270 });
  r.monster('Archer', 0, 4.2, { y: TOP, ry: 180 });
  r.monster('Warrior', 10.5, 6, { y: TOP, ry: 180 });
  r.start(-8.4, 14, 180);
};

// Вид. Некрополь безымянных высечен в сырой скале: на нишах бирки с номером смены, по косякам остатки глиняной заделки.
// История: вдова Хельга выцарапала мужу отметку на сырой глине — такие отметки разошлись по всем простенкам; на юге писари
// замазали их известью, к северу мазки редеют, в склепе стоят пустые бочки: известь кончилась.
exports.dress = S => {
  const { D } = S, { put, tile, beam, R, J, pick, chance } = D, K = require('../kits'), A = require('../sets/rites')(S), W = (x, z, k, base = 0) => S.wall(x, z, k, base + 1.5);
  S.theme({
    rock: 'rock', wall: 'slab', floor: 'slab', slab: 'slab', column: 'stone', platform: 'stone', arch: 'slab', age: 1.2, density: 0.8, damp: (x, y, z) => 0.3 + (z > 8 ? 0.15 : 0) + (y > 4 ? -0.12 : 0),
    mat: o => {
      if (o.p[1] >= TOP && o.p[2] > 12.4 && Math.abs(o.p[0]) < 4) return o.name === 'Саркофаг' ? 'marble' : o.cat === 'wall' ? 'stone' : undefined;   // родовая усыпальница: у этих были деньги на имя
      if (o.name === 'Саркофаг' || o.name === 'Парапет') return 'stone';
    }
  });
  S.kit['Саркофаг'] = (S, x, y, z, w, d, h, ry, o) => { K['Саркофаг'](S, x, y, z, w, d, h, ry, o); const P = A.grave(o, { rich: y >= TOP, mark: 0 }); if (y >= TOP) A.pall(P, h, P.len * 0.5, P.wid * 0.46, { m: 'clothBlue', drop: 0.1, trim: null }); };
  S.auto();

  // ================= Ниши: бирка смены, глиняная заделка, отметки на простенках =================
  const lintels = S.base.filter(o => o.cat === 'wall' && (o.p[1] === 2.4 || o.p[1] === TOP + 2.4) && Math.min(o.s[0], o.s[2]) === 0.4 && Math.max(o.s[0], o.s[2]) === 2);
  const sign = (wl, y, size = 1, name = 'Отметка ногтем') => { for (const [du, dy, h, a] of [[0, 0, 0.2, 0], [-0.05, 0.1, 0.12, 40], [0.05, 0.1, 0.12, -40], [0, -0.03, 0.02, 0]]) tile(name, 'soot', wl[0], wl[1], wl[2] + du * size, y + dy * size, a || h > 0.05 ? 0.018 * size : 0.14 * size, h * size, 0.012, { sink: -0.035, roll: a }); };
  let helga;
  for (const o of lintels) {
    const alongX = o.s[0] > o.s[2], y = o.p[1] - 2.4, [x, , z] = o.p, n = alongX ? [0, S.solid(x, y + 1, z + 1.3) ? 1 : -1] : [S.solid(x + 1.3, y + 1, z) ? 1 : -1, 0];
    const k = alongX ? (n[1] > 0 ? 'S' : 'N') : (n[0] > 0 ? 'W' : 'E'), into = { N: 'S', S: 'N', E: 'W', W: 'E' }[k], plane = (alongX ? z : x) - (n[0] + n[1]) * 0.2, u = alongX ? x : z, at = (du, out) => D.at(k, plane, u + du, out);
    const washed = y === 0 && (alongX ? z : u) < -2, thin = y === 0 && !washed && (alongX ? z : u) < 4.5, own = y === 0 && plane === -12 && u === 5.8;
    tile('Бирка с номером смены', 'iron', k, plane, u, y + 2.52, 0.34, 0.2, 0.02, { sink: 0, roll: J(3) });
    for (let i = 0, c = 2 + Math.floor(R(0, 5)); i < c; i++) tile('Насечка номера', 'soot', k, plane, u - 0.12 + i * 0.045, y + 2.56, 0.015, 0.12, 0.012, { sink: -0.02 });
    for (const s of [-1, 1]) if (chance(0.75)) { const [cx, cz] = at(s * 0.86, -0.08); put('Остаток глиняной заделки', 'clay', cx, y, cz, alongX ? 0.28 : 0.14, R(0.3, 1.9), alongX ? 0.14 : 0.28, { ry: J(3) }); }
    if (chance(0.6)) { const [cx, cz] = at(J(0.3), -0.08), len = R(0.7, 1.4); put('Порог заделки', 'clay', cx, y, cz, alongX ? len : 0.13, R(0.05, 0.1), alongX ? 0.13 : len, { ry: J(2) }); }
    for (let i = 0; i < 3; i++) { const [cx, cz] = at(J(0.8), R(0.1, 0.4)); put('Обломок заделки', 'clay', cx, y, cz, R(0.14, 0.26), 0.05, R(0.1, 0.18), { ry: R(0, 180), roll: J(10) }); }
    // Простенки: глина с отметкой; на юге она под известью, посередине мазки редеют, дальше отметки открыты и множатся.
    for (const s of [-1, 1]) {
      const [px, pz] = at(s * 1.42, 0.4), wl = S.wall(px, pz, into, y + 1.5, 0.7);
      if (!wl || Math.abs(wl[1] - plane) > 0.15 || (own && s < 0)) continue;
      const my = y + R(1.15, 1.45);
      tile('Глиняная обмазка', 'clay', wl[0], wl[1], wl[2], my - 0.14, R(0.34, 0.44), R(0.4, 0.5), 0.03, { sink: 0.005, roll: J(4) });
      if (washed) { for (let i = 0; i < 4; i++) A.disc('Побелка известью', 'bone', wl[0], wl[1], wl[2] + J(0.16), my + 0.1 + J(0.16), R(0.24, 0.42), 0.04, R(0.2, 0.4)); for (let i = 0; i < 2; i++) tile('Потёк извести', 'bone', wl[0], wl[1], wl[2] + J(0.2), my - R(0.5, 0.8), 0.03, R(0.3, 0.6), 0.012, { sink: 0 }); continue; }
      sign(wl, my);
      if (thin) { for (let i = 0; i < 2; i++) tile('Жидкий мазок извести', 'bone', wl[0], wl[1], wl[2] + J(0.1), my + R(-0.05, 0.2), R(0.24, 0.4), 0.05, 0.012, { sink: -0.04, roll: J(14) }); continue; }
      for (let i = 0, c = Math.round(R(0, 2.4)); i < c; i++) sign([wl[0], wl[1], wl[2] + pick([-0.24, 0.24]) + J(0.04)], y + R(0.8, 1.8), R(0.6, 0.9), 'Отметка на камне');
    }
    if (chance(0.3)) { const [cx, cz] = at(J(0.6), -0.2); A.candles(cx, cz, 2, { y, spread: 0.08, wax: false }); }
    if (chance(0.2)) { const [cx, cz] = at(J(0.5), -0.22); for (let j = 0; j < 3; j++) put('Засохший цветок', 'straw', cx, y + 0.012, cz, 0.015, R(0.18, 0.3), 0.015, { pitch: 90, ry: D.FACE[k].ry + 90 + J(20) }); }
    if (chance(0.25)) { const [ax, az] = at(-1.02, -0.25), [bx, bz] = at(-0.2, -0.25); S.web([ax, y + 2.35, az], [bx, y + 2.35, bz], { n: 3 }); }
    if (own) helga = { k, plane, u, at, y };
  }
  // --- Ниша мужа Хельги: первая отметка, глубокая; зубило и киянка каменотёса, огарки и цветы тех, кто пришёл после ---
  if (helga) {
    const { k, plane, u, at } = helga, wl = [k, plane, u + 1.42];
    tile('Глиняная обмазка', 'clay', k, plane, u + 1.42, 1.0, 0.6, 0.7, 0.04, { sink: 0.005, roll: 2 }); sign(wl, 1.3, 1.8, 'Отметка Хельги');
    const [tx, tz] = at(0.5, -0.2), [fx, fz] = at(-0.55, -0.18), [cx, cz] = at(1.42, 0.3);
    put('Зубило каменотёса', 'iron', tx, 0, tz, 0.03, 0.03, 0.24, { ry: 30 }); put('Киянка каменотёса', 'timber', tx + 0.1, 0, tz + 0.22, 0.12, 0.1, 0.18, { ry: 60 }); put('Рукоять киянки', 'timber', tx + 0.2, 0.03, tz + 0.3, 0.03, 0.03, 0.3, { ry: 60 });
    A.offer(fx, fz, { n: 5, spread: 0.25, coins: false }); A.candles(cx, cz, 7, { spread: 0.22, lit: true }); A.wreath([k, plane, u - 1.42], 1.5);
  }
  // ================= Склеп посередине: счётная писарей и последние бочки извести =================
  S.board(W(1.4, -4, 'S'), { y: 1.3, w: 1.6, h: 0.9, sheets: 3, name: 'Счётная доска смен' }); for (const y of [1.45, 1.7, 1.95]) D.tally(...W(2.9, -4, 'S').slice(0, 2), 2.7, y, Math.round(R(8, 15)));
  D.shelf(...W(-0.6, -4, 'S'), { y: 1.5, w: 1.2, items: 2 }); S.books(-0.6, 1.54, -4.82, 3, { spread: 0.2 });
  for (const [z, lying] of [[-1.5, 0], [-0.7, 0], [0.2, 1]]) lying ? put('Опрокинутая бочка извести', 'wood', 4.1, 0.28, z, 0.56, 0.75, 0.56, { round: true, pitch: 90, ry: 250 }) : D.barrel(4.6, 0, z, 0.56, 0.78);
  for (const z of [-1.5, -0.7]) put('Присохшая известь на дне', 'bone', 4.6, 0.62, z, 0.42, 0.02, 0.42, { round: true }); D.patch(3.9, -0.4, 0.8, { m: 'bone', name: 'Известковая корка', n: 6 });
  beam('Мазилка на шесте', 'timber', [4.75, 0, -2.2], [4.9, 2.1, -2.3], 0.04); put('Мочало мазилки', 'bone', 4.9, 2.05, -2.3, 0.14, 0.26, 0.1); put('Ведро с известью', 'wood', 4.2, 0, -2.4, 0.3, 0.3, 0.3, { round: true }); put('Засохшая известь', 'bone', 4.2, 0.26, -2.4, 0.24, 0.03, 0.24, { round: true });
  A.plaque(W(0, -4, 'S'), 2.3, { w: 1.2, h: 0.5, lines: 3, m: 'stone', name: 'Плита приёмщика смен' }); A.sconce(W(-1.9, -4, 'S'), 1.7); A.sconce(W(3.6, 4, 'E'), 1.7, { lit: true });
  A.stand(-2.1, 0.3, { y: 0.25, h: 1.4, arms: 1 }); A.stand(2.1, -2.9, { y: 0.25, h: 1.4, fallen: true, ry: 200 }); put('Подставка под свечу', 'stone', 1.4, 0, -3.6, 0.3, 0.25, 0.3);
  put('Брошенная мазилка', 'timber', -10.9, 0.02, -0.4, 0.04, 1.6, 0.04, { pitch: 90, ry: 10 }); put('Мочало мазилки', 'bone', -10.65, 0, 1.15, 0.14, 0.08, 0.24, { ry: 10 }); D.patch(-10.6, 1.3, 0.5, { m: 'bone', name: 'Последние капли извести', n: 4 });
  // ================= Кольцо: шипы, обвал, стоянка жильщиков, тамбуры =================
  for (const [x, z] of [[0.6, 10.5], [10.5, -1.4]]) { S.pool(x + 0.3, z - 0.2, 1.5, 1, { m: 'blood', name: 'Старое пятно' }); D.bones(x - 0.9, z + 0.6, 5, { spread: 0.5 }); for (let i = 0; i < 6; i++) put('Царапины от шипов', 'soot', x + J(0.8), 0.004, z + J(0.8), 0.02, 0.012, R(0.2, 0.5), { ry: R(0, 180) }); put('Сношенный башмак', 'leather', x + 1, 0, z - 0.8, 0.11, 0.08, 0.26, { ry: R(0, 180) }); }
  A.camp(-11.45, -10.9, { ry: 0, fire: false }); A.ashes(-10.3, -11.4, { r: 0.32 }); A.pry(-9.6, -11.6, { ry: 80 }); S.set('Мешки', -11.5, -9.7, 0.7, 0.6, 0.45); A.chalk(W(-11, -11, 'S'), 1.5, { arrow: 1 }); D.tally(...W(-11, -11, 'W').slice(0, 2), -11.2, 1.4, 8);
  for (const [x, z] of [[-11.2, -8.2], [-7, -11.2]]) { D.rubble(x, z, 1.6, 18, { m: 'rock', pile: 0.3, big: 0.3, name: 'Камень обвала' }); D.patch(x, z, 1.4, { m: 'dirt', name: 'Каменная пыль', n: 5 }); }
  S.roots(-11.4, 4.35, -9, { n: 6, len: 1.5 }); S.roots(11, 4.35, 10.5, { n: 5, len: 1.3 }); S.pool(-10.4, 10.6, 1.4, 0.9); S.pool(4.5, 11.2, 0.9, 0.6); D.fungus(-11.6, 11.6, 6); D.fungus(11.7, 11.7, 5); D.fungus(6, 11.7, 4);
  for (const [x, z, k] of [[-8.4, 14, 'S'], [8.4, 14, 'S'], [14, 8.4, 'W'], [14, -8.4, 'W'], [8.4, -14, 'N'], [-8.4, -14, 'N'], [-14, 8.4, 'E'], [-14, -8.4, 'E']]) {
    const wl = S.wall(x, z, k, 1.5, 2.5);
    A.plaque(wl, 1.5, { w: 0.8, h: 0.4, lines: 2, m: 'stone', studs: 'iron', name: 'Указатель смен' }); if (wl) tile('Кованая стрелка', 'iron', wl[0], wl[1], wl[2], 1.98, 0.5, 0.06, 0.02, { sink: 0, roll: J(3) });
    if (chance(0.6)) A.chalk(wl && [wl[0], wl[1], wl[2] + 0.9], 1.4, { [pick(['cross', 'ring', 'cross'])]: true });
  }
  // ================= Верхний ярус =================
  // Сюда известь не дошла вовсе; в северо-западном углу Спящие, на балконе колчан стрелка.
  A.sleepers(-11.2, 11.2, { y: TOP, r: 0.45, n: 6 }); A.runes(W(-11, 11, 'N', TOP), TOP + 1.5, 4); D.rubble(-11.3, 9.2, 1, 14, { m: 'rock', y: TOP, pile: 0.4, big: 0.3, name: 'Камень обвала' }); S.roots(-11.5, TOP + 4.35, 8.6, { n: 5, len: 1.2 });
  put('Колчан', 'leather', -1.8, TOP, 4.6, 0.12, 0.5, 0.12, { round: true, roll: 12 }); for (let i = 0; i < 4; i++) put('Стрела', 'timber', -1.8 + J(0.03), TOP + 0.4, 4.6 + J(0.03), 0.015, 0.36, 0.015, { roll: 12 + J(5) }); A.candles(1.9, 4.6, 3, { y: TOP });
  S.pool(5, 10.4, 1.2, 0.8, { y: TOP }); D.fungus(11.6, 11.6, 5, { y: TOP }); D.bones(10.6, 4, 4, { y: TOP, spread: 0.3 }); A.web(11.3, 3.4, TOP + 4.2);
  // Родовая усыпальница: мрамор, имена на табличках, знамя — всё, чего нет у ниш внизу.
  for (const s of [-1, 1]) { A.plaque(S.wall(s * 2.2, 14.6, s > 0 ? 'E' : 'W', TOP + 1.9, 2), TOP + 1.6, { w: 0.9, h: 0.5, lines: 4 }); A.sconce(S.wall(s * 2.2, 13.2, s > 0 ? 'E' : 'W', TOP + 1.9, 2), TOP + 1.7, { lit: s > 0 }); A.crest(S.wall(s * 2.2, 15.6, s > 0 ? 'E' : 'W', TOP + 2.4, 2), TOP + 2.5, { s: 0.5 }); }
  S.banner(W(0, 15.4, 'N', TOP), TOP + 3.9, { w: 1.2, h: 2, m: 'clothRed', torn: 0.3, sign: 'gold' }); A.offer(0.9, 13.4, { y: TOP, n: 3 }); A.plaque(S.wall(1.8, 11.6, 'N', TOP + 1.5, 2), TOP + 1.5, { w: 0.7, h: 0.4, lines: 3, name: 'Табличка с именем рода' });
};
