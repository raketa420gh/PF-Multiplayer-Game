// Новая: грибные чеки, холм из шести контурных террас с водой по колено, внутри сквозной ход с корневыми погребами.
const { subtract, rect } = require('../roomkit');
const HILL = 5, CEIL = 18, ROCK = 4.4, FULL = rect(-16.8, -16.8, 16.8, 16.8);
const R = (x1, z1, x2, z2) => rect(x1, z1, x2, z2);
// Подошва холма +5: квадрат с рваным краем и скальные углы до стен клетки; на углах кольцо у края рвётся.
const BASE = subtract(FULL, [R(3.4, 9.8, 7, 11), R(-11, -5, -9.8, -1.6), R(-5, -11, -2, -9.8), R(9.8, 1, 11, 5)],
  [R(-11, -11, 11, 11), R(-16.8, 9.8, -9.8, 16.8), R(9.8, 10.4, 16.8, 16.8), R(9.8, -16.8, 16.8, -10), R(-16.8, -16.8, -10.4, -9.8)]);
// Выше холм сдвинут на северо-восток: там склон круче, на юго-западе длиннее.
const UPPER = [
  // Марши на +7 врезаны в террасу: кольцо +5 уже марша, у подножия нужна площадка.
  [7, subtract(FULL, [R(-8.4, 5, -5, 8.6), R(5.6, -8.4, 8.6, -5), R(-8.2, 1.3, -6.9, 3.5), R(6.9, -4.7, 8.2, -2.5), R(-4.1, 6.9, -1.9, 8.2)], [R(-8.2, -8.4, 8.2, 8.2)])],
  [9, subtract(FULL, [R(-3.6, 3.4, -1, 6), R(3.6, -4.6, 6, -2.2)], [R(-3.6, -4.6, 6, 6)])],
  [11, subtract(FULL, [R(0.4, 0.6, 2.4, 2.4)], [R(0.4, 0.6, 6, 6)])],
];
// Стороны холма: координаты u вдоль стены и v от стены внутрь.
const SIDES = {
  N: { p: (u, v) => [u, 16.8 - v], ins: 180, along: 90 },
  E: { p: (u, v) => [16.8 - v, -u], ins: 270, along: 180 },
  S: { p: (u, v) => [-u, -16.8 + v], ins: 0, along: 270 },
  W: { p: (u, v) => [-16.8 + v, u], ins: 90, along: 0 },
};
const FACE = 5.8, EDGE_U = 6.6;
// Низ у стен: отрог +2,5 до стены, полки +1,25 по бокам, марши вразбежку. Марш с отрога стоит в стороне от бокового: тот выходит на отрог, а не в его борт.
const BANDS = [
  { side: 'N', spur: [-2.8, 2.2], vA: 2.2, vB: 3, toA: true, toB: true, linkA: true, linkB: false, up: 0.9 },
  { side: 'E', spur: [-1.4, 3.6], vA: 3, vB: 2.2, toA: false, toB: true, linkA: false, linkB: true, up: 0.2 },
  { side: 'S', spur: [-3.6, 1.4], vA: 2.6, vB: 2.6, toA: true, toB: true, linkA: true, linkB: true, up: -1.1 },
  { side: 'W', spur: [-1, 4], vA: 2, vB: 3.2, toA: true, toB: false, linkA: true, linkB: false, up: null },
];

exports.options = {};
exports.build = r => {
  r.desc('В зале высотой 18 м стоит асимметричный холм: от стен шесть террас +1,25, +2,5, +5, +7, +9 и +11, у юго-западного склона длинные, у северо-восточного крутые. На террасах чеки с водой по колено, между ними сухие валики 0,25, каскад спускается с вершины лентами по одной линии. Лестницы разнесены вразбежку, на +5 и выше подъём приходится обходить вдоль контура. Скальные углы холма доходят до стен, а на середине каждой стороны к стене выходит отрог +2,5, поэтому у края кольца нет, и ни один проём не видит другой. Под холмом на 0 проходит сквозной ход: от каждого проёма дверь в кольцевую штольню, в ней двери, поперёк идёт центральный ход к корневым погребам. На вершине источник и лучший сундук.');
  const g = r.level(0, ROCK), up = r.level(HILL, CEIL - HILL);
  up.carve(-16.8, -16.8, 16.8, 16.8);
  const open = subtract(FULL, BASE);
  for (const a of open) { g.carve(a.x1, a.z1, a.x2, a.z2); up.hole(a.x1, a.z1, a.x2, a.z2); }

  // Чек: терраса с бордюром-валиком, внутри вода; длинный чек делится поперечным валиком.
  const paddies = (a, y) => {
    const w = a.x2 - a.x1, d = a.z2 - a.z1, m = 0.45;
    if (Math.min(w, d) < 1.4) { r.platform(a.x1, a.z1, a.x2, a.z2, y + 0.25, y); return; }
    const inner = rect(a.x1 + m, a.z1 + m, a.x2 - m, a.z2 - m), cuts = [];
    if (Math.max(w, d) > 7) { const c = w > d ? (a.x1 + a.x2) / 2 + 0.8 : (a.z1 + a.z2) / 2 - 0.8; w > d ? cuts.push(rect(c - 0.25, inner.z1, c + 0.25, inner.z2)) : cuts.push(rect(inner.x1, c - 0.25, inner.x2, c + 0.25)); }
    const pools = subtract(inner, cuts);
    for (const p of pools) r.water(p.x1, p.z1, p.x2, p.z2, y);
    for (const b of subtract(a, pools)) r.platform(b.x1, b.z1, b.x2, b.z2, y + 0.25, y);
  };

  // Низ: полки и отроги у стен.
  for (const b of BANDS) {
    const s = SIDES[b.side], box = (u1, v1, u2, v2) => { const [x1, z1] = s.p(u1, v1), [x2, z2] = s.p(u2, v2); return rect(x1, z1, x2, z2); };
    const flight = (u, v, w, len, rise, ry, y0) => { const [x, z] = s.p(u, v); r.stairs(x, z, w, len, rise, ry, y0); };
    const [a, c] = b.spur, sa = box(-EDGE_U, b.vA, a, FACE), sb = box(c, b.vB, EDGE_U, FACE), spur = box(a, 0, c, FACE);
    r.platform(sa.x1, sa.z1, sa.x2, sa.z2, 1.25); r.platform(sb.x1, sb.z1, sb.x2, sb.z2, 1.25); r.platform(spur.x1, spur.z1, spur.x2, spur.z2, 2.5);
    paddies(sa, 1.25); paddies(sb, 1.25); paddies(spur, 2.5);
    if (b.toA) flight((-EDGE_U + a) / 2, b.vA - 0.9, 2.2, 1.8, 1.5, s.ins, 0);
    if (b.toB) flight((c + EDGE_U) / 2, b.vB - 0.9, 2.2, 1.8, 1.5, s.ins, 0);
    if (b.linkA) flight(a - 0.9, 4.4, 2.4, 1.8, 1.5, s.along, 1.25);
    if (b.linkB) flight(c + 0.9, 4.4, 2.4, 1.8, 1.5, (s.along + 180) % 360, 1.25);
    if (b.up !== null) flight(b.up, 4.15, 2.2, 3.3, 2.75, s.ins, 2.5);
  }

  // Террасы холма: кладка, затем чеки на открытом кольце каждой.
  let below = BASE, y = HILL;
  for (const [top, area] of UPPER) {
    for (const a of area) r.platform(a.x1, a.z1, a.x2, a.z2, top, y);
    for (const a of subtract(below === BASE ? R(-11, -11, 11, 11) : FULL, area, below)) paddies(a, y);
    below = area; y = top;
  }
  // Вершина: чаша источника.
  r.water(2.8, 2.8, 5.6, 5.6, 11);
  r.prop('Источник', 4.8, 4.8, 1.4, 1.4, 1, { y: 11, round: true });

  // Марши вразбежку: на каждый уступ свой угол.
  r.stairs(-8.25, 2.4, 2.2, 2.7, 2.25, 90, HILL);
  r.stairs(8.25, -3.6, 2.2, 2.7, 2.25, 270, HILL);
  r.stairs(-3, 8.25, 2.2, 2.7, 2.25, 180, HILL);
  r.stairs(2, -5.95, 2.2, 2.7, 2.25, 0, 7);
  r.stairs(-4.95, 1, 2.2, 2.7, 2.25, 90, 7);
  r.stairs(4.2, -0.6, 2.2, 2.4, 2, 0, 9);
  r.stairs(-0.8, 4.2, 2.2, 2.4, 2, 90, 9);

  // Каскад лентами по одной линии с вершины на юг.
  for (const [z, y1, h] of [[0.5, 9, 2], [-4.7, 7, 2], [-8.5, 5, 2], [-11.05, 2.5, 2.5]]) r.prop('Каскад', 3, z, 1.4, 0.16, h, { y: y1 });

  // Сквозной ход: кольцевая штольня и проколы к каждому проёму.
  for (const [a, b, c, d] of [[-9.4, 7, 9.4, 9.4], [-9.4, -9.4, 9.4, -7], [-9.4, -7, -7, 7], [7, -7, 9.4, 7]]) g.carve(a, b, c, d);
  const gate = (x, z, alongX) => {
    r.door(x, z, alongX ? 0 : 90);
    r.block(x, z, alongX ? 2.4 : 0.8, alongX ? 0.8 : 2.4, { y: 2.4, h: ROCK - 2.4 });
  };
  for (const u of [-8, 8]) {
    g.carve(u - 1.2, 9.4, u + 1.2, 11); gate(u, 10.2, true);
    g.carve(u - 1.2, -11, u + 1.2, -9.4); gate(u, -10.2, true);
    g.carve(9.4, u - 1.2, 11, u + 1.2); gate(10.2, u, false);
    g.carve(-11, u - 1.2, -9.4, u + 1.2); gate(-10.2, u, false);
  }
  // Двери в штольне: кольцо по ней тоже рвётся.
  r.doorWall(3, 7, 3, 9.4, [8.2], { h: ROCK }); r.doorWall(-3, -9.4, -3, -7, [-8.2], { h: ROCK });
  r.doorWall(-9.4, 2, -7, 2, [-8.2], { h: ROCK }); r.doorWall(7, -2, 9.4, -2, [8.2], { h: ROCK });
  // Центральный ход и корневые погреба.
  g.carve(-1.2, -7, 1.2, 7);
  gate(0, 6.6, true); gate(0, -6.6, true);
  g.carve(-6.4, 1.4, -1.6, 6.2); g.carve(-1.6, 2, -1.2, 4.4); r.door(-1.4, 3.2, 90); r.block(-1.4, 3.2, 0.4, 2.4, { y: 2.4, h: 2 });
  g.carve(1.6, -6.2, 6.4, -1.4); g.carve(1.2, -4.4, 1.6, -2); r.door(1.4, -3.2, 90); r.block(1.4, -3.2, 0.4, 2.4, { y: 2.4, h: 2 });
  g.carve(-6.4, -6.4, -1.8, -1.6); g.carve(-7, -5.2, -6.4, -2.8); r.door(-6.7, -4, 90); r.block(-6.7, -4, 0.6, 2.4, { y: 2.4, h: 2 });
  g.carve(1.8, 1.6, 6.4, 6.4); g.carve(6.4, 2.8, 7, 5.2); r.door(6.7, 4, 90); r.block(6.7, 4, 0.6, 2.4, { y: 2.4, h: 2 });
  r.prop('Полки с припасами', -5.8, 5.2, 1, 1.8, 2.2); r.prop('Бочки', -5.2, 2.2, 1.4, 1.4, 1.2); r.chest('LargeOakChest', -2.6, 5.4, { ry: 180 });
  r.prop('Мешки', 5.6, -5.4, 1.2, 1.2, 0.8); r.prop('Гриб', 3.4, -5.2, 1.4, 1.4, 1.2, { round: true }); r.chest('Barrel', 5.6, -2.2);
  r.prop('Штабель ящиков', -5.4, -5.4, 1.4, 1.4, 2); r.chest('SmallOakChest', -2.6, -5.6, { ry: 0 });
  r.prop('Гриб', 5.4, 5.4, 1.2, 1.2, 1, { round: true }); r.chest('Crate', 2.6, 2.4);

  // Добыча и опасность растут к вершине.
  r.chest('GoldenChest', 2.2, 5, { y: 11, ry: 90 });
  r.chest('GoldPile', 4, 2, { y: 11 });
  r.chest('LargeOakChest', -2.4, -3, { y: 9, ry: 45 });
  r.chest('SmallOakChest', -7.4, -7, { y: 7, ry: 45 });
  r.chest('Urn', 15.6, 0.4, { y: 2.5 });
  r.chest('Urn', -9.8, 8.6, { y: 5 });
  r.prop('Гриб', 1, 7.4, 1.6, 1.6, 1.3, { y: 7, round: true });
  r.prop('Гриб', -6.2, -2.6, 1.2, 1.2, 1, { y: 7, round: true });
  r.prop('Гриб', 4.6, -9.6, 1.4, 1.4, 1.2, { y: 5, round: true });
  r.prop('Гриб', 9.6, -8, 1.2, 1.2, 1, { y: 5, round: true });
  r.trap('Spike', -1, -9.4, { y: 5 });
  r.trap('Spike', -6.4, -6, { y: 7 });
  r.trap('Spike', 7.4, 0, { y: 7 });
  r.trap('Vent', 1.6, -3, { y: 9 });
  r.trap('Spike', -1.8, 0.4, { y: 9 });
  r.trap('Spike', 4.2, 1.2, { y: 11 });
  r.trap('Vent', 1.4, 3.4, { y: 11 });
  r.trap('Spike', 3.2, 3, { y: 11 });

  // Прохладный свет.
  const cold = (x, yy, z, i, rr) => r.add('light', 'Brazier', [x, yy, z], null, { color: '#7FE3D2', i, r: rr });
  cold(4.8, 12.6, 4.8, 9, 16);
  cold(-6, 8.4, 0, 7, 12);
  cold(4, 6.2, -9.6, 6, 11);
  cold(-13, 3, 14, 6, 11);
  cold(13, 3, -14, 6, 11);
  r.add('light', 'Candle', [-3.6, 1.4, 3.6], null, { color: '#9FE8D8', i: 1.4, r: 5 });
  r.add('light', 'Candle', [0, 1.4, 0], null, { color: '#9FE8D8', i: 1.4, r: 6 });
  r.add('light', 'Candle', [8.2, 1.4, -8.2], null, { color: '#9FE8D8', i: 1.4, r: 6 });

  r.monster('Sword', 8, 12.6, { ry: 180 });
  r.monster('Skull', 0, 2, { ry: 0 });
  r.monster('Archer', -9.4, 5, { y: 5, ry: 90 });
  r.monster('Crossbow', 6.8, -1, { y: 7, ry: 270 });
  r.monster('Warrior', -1, -2, { y: 9, ry: 45 });
  r.monster('Mage', 3.6, 4, { y: 11, ry: 225 });
  r.start(-8.4, 15, 180);
};
