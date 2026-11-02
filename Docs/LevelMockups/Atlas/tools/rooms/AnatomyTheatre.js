// Новая: крутая восьмиугольная воронка зрительских рядов вокруг стола, дно только снизу через служебную галерею −5.
const BAY = 3.6, BAY_W = 1.6, BOT = -5, ROW = -2, FLOOR_R = 4.4, RIM = 10.4, SHAFT = 7, GAL1 = 7.6, GAL2 = 10.6, H = 8, BOX = 4.5;
// Границы рядов от центра: нижний ряд шире, на нём ограждение.
const RINGS = [FLOOR_R, 6.1, 7, 7.9, 8.8, RIM];
const T = Math.tan(Math.PI / 8), RT = Math.SQRT1_2, QUAD = [[1, 1, 45], [-1, 1, 315], [1, -1, 315], [-1, -1, 45]], AISLE = 0.8;

exports.options = {};
exports.build = r => {
  r.desc('Восьмиугольная воронка диаметром около 21 м: пять рядов-уступов высотой 1 м спускаются от кромки на 0 к нижнему ряду −4, за его ограждением метровый обрыв на дно −5 со столом-саркофагом. Ряды режут четыре узких радиальных марша: три выходят на балконы, выдвинутые из нижнего ряда к столу, южный кончается на крыше портала. На дно с рядов не попасть: вход один, из кольцевой служебной галереи −5 под рядами через портал с дверью, к галерее спускаются две лестницы из передних, на запад и на восток, вокруг неё комнаты препараторов. На 0 вокруг воронки четыре стороны залов, разделённые скалой на углах: у каждой центральная передняя с дверью-вомиторием на кромку и боковые холлы с проёмами. Из северной передней марш ведёт на лекторскую ложу +4,5 над рядами.');
  const low = r.level(BOT, 2.4), mid = r.level(ROW, 1.4), g = r.level(0, H);
  const seg = (dir, a, b, rIn, rOut, top, y0) => {
    const [x1, z1, x2, z2] = { N: [a, rIn, b, rOut], S: [a, -rOut, b, -rIn], E: [rIn, a, rOut, b], W: [-rOut, a, -rIn, b] }[dir];
    r.platform(x1, z1, x2, z2, top, y0);
  };
  // Диагональная грань из двух половин: проверка считает повёрнутый блок по его описанному прямоугольнику, половины его сужают.
  const diag = (rIn, rOut, top, y0) => {
    const total = top - y0, n = Math.ceil(total / 1.5 - 1e-6), t = total / n, rm = (rIn + rOut) / 2, c = rm * RT, L = rOut * T;
    for (const [sx, sz, ry] of QUAD) for (const u of [-0.5, 0.5]) for (let i = 0; i < n; i++)
      r.add('platform', 'Platform', [sx * c + sx * sz * u * L * RT, y0 + i * t, sz * c - sx * sz * u * L * RT], [L, t, rOut - rIn], { ry });
  };

  // Яма, галерея, кромка.
  low.carve(-SHAFT, -SHAFT, SHAFT, SHAFT);
  for (const [a, b, c, d] of [[-GAL2, GAL1, GAL2, GAL2], [-GAL2, -GAL2, GAL2, -GAL1], [GAL1, -GAL1, GAL2, GAL1], [-GAL2, -GAL1, -GAL1, GAL1]]) low.carve(a, b, c, d);
  mid.carve(-RIM, -RIM, RIM, RIM); mid.hole(-SHAFT, -SHAFT, SHAFT, SHAFT);
  g.carve(-RIM, -RIM, RIM, RIM); g.hole(-RIM, -RIM, RIM, RIM);

  // Ряды: −4 и −3 стоят на дне, −2 это пол яруса, −1 и 0 стоят на нём. Каждый ряд прорезан маршем.
  const rows = [[-4, BOT], [-3, BOT], [-2, null], [-1, ROW], [0, ROW]];
  rows.forEach(([top, y0], k) => {
    const rIn = RINGS[k], rOut = RINGS[k + 1];
    if (y0 === null) return;
    const half = (rIn + rOut) / 2 * T + 0.5;
    for (const dir of ['N', 'E', 'S', 'W']) {
      const gap = dir === 'S' && top < ROW ? 1.6 : AISLE;
      seg(dir, -half, -gap, rIn, rOut, top, y0); seg(dir, gap, half, rIn, rOut, top, y0);
    }
    diag(rIn, rOut, top, y0);
  });
  // У подножия трёх маршей балкон: нижний ряд выдвинут к столу, марш выходит на площадку, а не в обрыв.
  for (const dir of ['N', 'E', 'W']) { seg(dir, -AISLE, AISLE, FLOOR_R, 5.15, -4, BOT); seg(dir, -BAY_W, BAY_W, BAY, FLOOR_R, -4, BOT); }
  // Перемычка верхнего ряда за концами маршей и закрытие углов шахты под рядом −2.
  for (const dir of ['N', 'E', 'S', 'W']) seg(dir, -AISLE, AISLE, 9.9, RIM, 0, ROW);
  diag(SHAFT, 9.9, ROW, ROW - 0.6);
  // Внешняя грань кромки на углах и ограждение нижнего ряда.
  for (const [sx, sz, ry] of QUAD) r.add('wall', 'Wall', [sx * 10.7 * RT, -0.6, sz * 10.7 * RT], [2 * 10.7 * T, H + 0.6, 0.6], { ry });
  const lip = (x, z, w, d, ry = 0) => r.prop('Парапет', x, z, w, d, 1.1, { y: -4, ry });
  lip(0, BAY + 0.08, 2 * BAY_W, 0.16); lip(BAY + 0.08, 0, 0.16, 2 * BAY_W); lip(-BAY - 0.08, 0, 0.16, 2 * BAY_W);
  for (const s of [-1, 1]) {
    const m = (BAY + FLOOR_R) / 2 + 0.08, d = FLOOR_R - BAY, e = BAY_W + 0.08, c = FLOOR_R + 0.08;
    lip(s * e, m, 0.16, d); lip(e, s * m, d, 0.16); lip(-e, s * m, d, 0.16); lip(s * 1.72, c, 0.16, 0.16); lip(c, s * 1.72, 0.16, 0.16); lip(-c, s * 1.72, 0.16, 0.16);
  }
  for (const [sx, sz, ry] of QUAD) for (const u of [-0.5, 0.5]) lip((FLOOR_R + 0.08) * RT * sx + sx * sz * u * 1.9 * RT, (FLOOR_R + 0.08) * RT * sz - sx * sz * u * 1.9 * RT, 1.9, 0.16, ry);

  // Радиальные марши.
  // Два марша один за другим: нижний стоит на дне шахты, верхний на полу −2, над галереей.
  for (const y of [-4, ROW]) {
    const c = y === ROW ? 8.75 : 6.35;
    r.stairs(0, c, 1.6, 2.4, 2, 0, y); r.stairs(c, 0, 1.6, 2.4, 2, 90, y); r.stairs(-c, 0, 1.6, 2.4, 2, 270, y);
  }
  mid.hole(-AISLE, SHAFT, AISLE, 7.6); mid.hole(SHAFT, -AISLE, 7.6, AISLE); mid.hole(-7.6, -AISLE, -SHAFT, AISLE);
  r.stairs(0, -8.55, 1.6, 2.7, 2, 180, ROW);

  // Портал: коридор сквозь нижние ряды с юга, на его крыше кончается южный марш.
  low.carve(-1, -GAL1, 1, -SHAFT);
  r.door(0, -7.3, 0, { y: BOT });
  r.wall(-1.3, -SHAFT, -1.3, -FLOOR_R + 0.2, { y: BOT, h: 3 - 0.6 });
  r.wall(1.3, -SHAFT, 1.3, -FLOOR_R + 0.2, { y: BOT, h: 3 - 0.6 });
  r.slab(-1.6, -SHAFT, 1.6, -FLOOR_R + 0.2, ROW);
  for (const [x, z, w, d] of [[-1.5, -5.2, 0.2, 2], [1.5, -5.2, 0.2, 2], [0, -4.3, 3.2, 0.2]]) r.prop('Парапет', x, z, w, d, 1, { y: ROW });

  // Дно.
  r.prop('Саркофаг', 0, 0, 1.2, 2.4, 1, { y: BOT });
  r.chest('GoldenChest', 0, 2.2, { y: BOT, ry: 180 });
  r.chest('Urn', -3, -0.4, { y: BOT });
  r.prop('Кости', 2.4, 1.4, 1.2, 1, 0.2, { y: BOT });

  // Передние на 0: скала на углах рвёт обход, внутри стороны холл, передняя, холл.
  g.carve(-GAL2, 11, GAL2, 16.8); g.carve(-GAL2, -16.8, GAL2, -11); g.carve(11, -GAL2, 16.8, GAL2); g.carve(-16.8, -GAL2, -11, GAL2);
  r.doorWall(-3.6, 11, -3.6, 16.8, [12.6], { h: H }); r.doorWall(4.4, 11, 4.4, 16.8, [12.6], { h: H });
  r.doorWall(-3, -16.8, -3, -11, [-13], { h: H }); r.doorWall(3, -16.8, 3, -11, [-15], { h: H });
  r.doorWall(11, 2.4, 16.8, 2.4, [15.4], { h: H }); r.doorWall(11, -2.4, 16.8, -2.4, [13], { h: H });
  r.doorWall(-16.8, 2.4, -11, 2.4, [-13], { h: H }); r.doorWall(-16.8, -2.4, -11, -2.4, [-15.4], { h: H });
  const vom = (x, z, alongX) => {
    alongX ? g.carve(x - 1, z - 0.3, x + 1, z + 0.3) : g.carve(x - 0.3, z - 1, x + 0.3, z + 1);
    r.door(x, z, alongX ? 0 : 90);
  };
  const lintel = (x, z, alongX, y, h) => r.block(x, z, alongX ? 2 : 0.6, alongX ? 0.6 : 2, { y, h });
  vom(0, 10.7, true); vom(0, -10.7, true); vom(10.7, 0, false); vom(-10.7, 0, false);
  lintel(0, -10.7, true, 2.4, H - 2.4); lintel(10.7, 0, false, 2.4, H - 2.4); lintel(-10.7, 0, false, 2.4, H - 2.4);
  // Над северным вомиторием дверь на ложу.
  lintel(0, 10.7, true, 2.4, BOX - 2.4);
  r.door(0, 10.7, 0, { y: BOX });
  lintel(0, 10.7, true, BOX + 2.4, H - BOX - 2.4);

  // Лекторская ложа и марш к ней.
  r.stairs(0.3, 15.8, 2, 5.4, BOX, 270);
  r.slab(-3.3, 11, 1.5, 14.8, BOX);
  r.slab(-3.3, 14.8, -2.4, 16.8, BOX);
  r.slab(-1, 10.4, 1, 11, BOX);
  r.prop('Парапет', 0.05, 14.65, 2.9, 0.3, 1, { y: BOX });
  r.prop('Парапет', 1.35, 12.9, 0.3, 3.8, 1, { y: BOX });
  r.slab(-3.8, 6.6, 3.8, RIM, BOX);
  for (const [x, z, w, d] of [[-3.65, 8.5, 0.3, 3.8], [3.65, 8.5, 0.3, 3.8], [0, 6.75, 7.6, 0.3]]) r.prop('Парапет', x, z, w, d, 1, { y: BOX });
  r.prop('Стол', 1.6, 8.8, 1.8, 0.8, 1, { y: BOX, ry: 0 });
  r.chest('SmallOakChest', -2.4, 9.6, { y: BOX, ry: 180 });

  // Лестницы к галерее: восток и запад, у верхнего конца ограждение пролёта.
  const shaft = (x1, z1, x2, z2, cx, zc, ry) => {
    low.carve(x1, z1, x2, z2); mid.carve(x1, z1, x2, z2); mid.hole(x1, z1, x2, z2); g.hole(x1, z1, x2, z2);
    r.stairs(cx, zc, x2 - x1, z2 - z1, 5, ry, BOT);
  };
  shaft(11.2, 4.4, 13.6, 10.4, 12.4, 7.4, 180); low.carve(11.2, 10.4, 13.6, 12.2); low.carve(8.2, 10.6, 11.2, 12.2); mid.carve(11.2, 10.4, 13.6, 11.2); mid.hole(11.2, 10.4, 13.6, 11.2);
  shaft(-13.6, -10.4, -11.2, -4.4, -12.4, -7.4, 0); low.carve(-13.6, -12.2, -11.2, -10.4); low.carve(-11.2, -12.2, -8.2, -10.6); mid.carve(-13.6, -11.2, -11.2, -10.4); mid.hole(-13.6, -11.2, -11.2, -10.4);
  r.prop('Парапет', 13.75, 7.4, 0.3, 6, 1); r.prop('Парапет', -13.75, -7.4, 0.3, 6, 1);

  // Комнаты препараторов.
  const prep = (x1, z1, x2, z2, nx, nz, alongX) => {
    low.carve(x1, z1, x2, z2);
    alongX ? low.carve(nx - 1, nz - 0.3, nx + 1, nz + 0.3) : low.carve(nx - 0.3, nz - 1, nx + 0.3, nz + 1);
    r.door(nx, nz, alongX ? 0 : 90, { y: BOT });
  };
  prep(-8, 11.2, -1, 16, -4, 10.9, true);
  prep(2, -16, 9, -11.2, 6, -10.9, true);
  prep(-16, 2, -11.2, 10, -10.9, 6, false);
  prep(11.2, -10, 16, -2, 10.9, -6, false);
  const y = BOT;
  r.prop('Чан', -2.4, 14.6, 1.6, 1.6, 1, { y, round: true }); r.prop('Полки с припасами', -6.4, 15.6, 2.6, 0.6, 2, { y });
  r.prop('Стол', -5.6, 13, 2, 1, 0.85, { y }); r.chest('LargeOakChest', -7.4, 12, { y, ry: 90 });
  r.prop('Чан', 8, -15, 1.6, 1.6, 1, { y, round: true }); r.prop('Полки с припасами', 4, -15.6, 2.6, 0.6, 2, { y });
  r.prop('Стол', -14.6, 3.4, 1, 2, 0.85, { y }); r.prop('Полки с припасами', -15.6, 8, 0.6, 2.6, 2, { y }); r.chest('SmallOakChest', -14.8, 9, { y, ry: 90 });
  r.prop('Чан', 14.4, -3.8, 1.6, 1.6, 1, { y, round: true }); r.prop('Гроб', 15.2, -8, 0.8, 2, 0.6, { y }); r.chest('Coffin', 12.6, -9, { y, ry: 0 });
  r.chest('Urn', 3.4, -11.8, { y });
  r.trap('Spike', 9, 2, { y });
  r.trap('Spike', -2, -9, { y });

  // Залы на 0.
  r.prop('Лавка', -9.8, 13, 0.5, 2.4, 0.45); r.prop('Лавка', 9.8, 14.6, 0.5, 2.4, 0.45);
  r.prop('Лавка', -6, -11.5, 2.4, 0.5, 0.45); r.prop('Лавка', 6, -11.6, 2.4, 0.5, 0.45);
  r.prop('Скамья', 15.8, -5, 0.6, 2.4, 0.5); r.prop('Скамья', -15.8, 5, 0.6, 2.4, 0.5);
  r.chest('Crate', -5, 15.9); r.chest('Urn', 15.9, 1.4); r.chest('Crate', -15.9, -9.6);

  // Свет: над столом, в передних и в галерее.
  r.add('light', 'Brazier', [0, 3.2, 0], null, { color: '#FFD9A6', i: 9, r: 14 });
  r.brazier(2.6, -1.6, BOT);
  r.brazier(0, 13);
  r.torch(0, -16.6, 0);
  r.torch(16.6, 0, 270);
  r.torch(-16.6, 0, 90);
  r.candle(9, -9, BOT);
  r.candle(-9, 9, BOT);

  r.monster('Archer', 3, 9.8, { ry: 180 });
  r.monster('Archer', -9.8, 3, { ry: 90 });
  r.monster('Crossbow', -1.6, 8, { y: BOX, ry: 180 });
  r.monster('Sword', 9, -9, { y: BOT, ry: 90 });
  r.monster('Warrior', -2.4, -2, { y: BOT, ry: 90 });
  r.monster('Skull', 6, -14, { ry: 0 });
  r.start(8.4, 15.4, 180);
};
