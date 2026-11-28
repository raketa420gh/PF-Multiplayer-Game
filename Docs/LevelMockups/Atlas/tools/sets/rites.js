// Утварь усыпальниц и святилищ Собора: свечи, кадила, таблички, приношения, останки, стоянки жильщиков, знаки Спящих.
// Общая для Crypt, Mausoleum, LordsTomb, BurialNiches, Chapel, CornerShrine: const A = require('../sets/rites')(S).
const K = require('../kits'), DEG = Math.PI / 180;

module.exports = S => {
  const { D } = S, { put, tile, beam, blot, R, J, pick, chance } = D, A = {};
  /// Пол под точкой рядом с высотой near; пола нет — сама near.
  const F = A.F = (x, z, near = 0) => { const y = S.floor(x, z, near); return y === undefined || Math.abs(y - near) > 1.3 ? near : y; };
  const fr = A.fr = (x, y, z, ry = 0) => K.frame(S, x, y, z, 1, 1, ry);
  /// Оси пропа или сундука: x вдоль длинной стороны, низ на его подошве.
  A.box = o => K.frame(S, o.p[0], o.p[1], o.p[2], o.s[0], o.s[2], o.ry || 0, true);
  A.props = name => S.base.filter(o => o.cat === 'prop' && o.name === name);
  A.chests = name => S.base.filter(o => o.cat === 'chest' && (!name || o.name === name));
  /// Диск на стене без перекоса: центр (u, y), ширина d, толщина th, высота h.
  const disc = A.disc = (name, m, k, p, u, y, d, th, h = d) => { const [x, z] = D.at(k, p, u); return put(name, m, x, y, z, d, th, h, { round: true, pitch: 90, ry: D.FACE[k].ry }); };
  const flame = (x, y, z) => put('Огонёк', 'ember', x, y, z, 0.025, 0.05, 0.025, { round: true });

  /// Огарки кучкой и наплыв воска.
  A.candles = (x, z, n = 4, { y, spread = 0.18, lit = false, wax = true } = {}) => {
    const by = y === undefined ? F(x, z) : y;
    for (let i = 0; i < n; i++) { const h = R(0.04, 0.22), cx = x + J(spread), cz = z + J(spread); put(h < 0.09 ? 'Огарок' : 'Свеча', 'bone', cx, by, cz, 0.05, h, 0.05, { round: true }); if (lit) flame(cx, by + h, cz); }
    if (wax) put('Наплыв воска', 'bone', x, by, z, spread * 2.3, 0.015, spread * 2, { round: true, ry: R(0, 180) });
  };
  /// Настенный подсвечник: кронштейн, чашка, огарок, потёки воска, копоть.
  A.sconce = (wall, y = 1.6, { lit = false } = {}) => {
    if (!wall) return;
    const [k, p, u] = wall, [x, z] = D.at(k, p, u, 0.17), h = R(0.05, 0.16);
    tile('Розетка подсвечника', 'iron', k, p, u, y - 0.14, 0.1, 0.2, 0.02, { sink: 0 }); tile('Кронштейн подсвечника', 'iron', k, p, u, y - 0.04, 0.035, 0.035, 0.2, { sink: 0.01 });
    put('Чашка подсвечника', 'bronze', x, y, z, 0.13, 0.025, 0.13, { round: true }); put('Огарок', 'bone', x, y + 0.025, z, 0.05, h, 0.05, { round: true });
    if (lit) flame(x, y + 0.025 + h, z);
    for (let i = 0; i < 3; i++) { const len = R(0.06, 0.3); put('Потёк воска', 'bone', x + J(0.05), y - len + 0.01, z + J(0.05), 0.02, len, 0.02, { round: true }); }
    disc('Копоть', 'soot', k, p, u, y + 0.5, 0.14, 0.012, 0.3);
  };
  /// Лампада на кованом кронштейне, y — низ чаши.
  A.lamp = (wall, y = 1.75, { m = 'bronze', lit = false } = {}) => {
    if (!wall) return;
    const [k, p, u] = wall, [x, z] = D.at(k, p, u, 0.34), [wx, wz] = D.at(k, p, u, 0);
    beam('Кронштейн лампады', 'iron', [wx, y + 0.56, wz], [x, y + 0.5, z], 0.03); beam('Подкос кронштейна', 'iron', [wx, y + 0.28, wz], [x, y + 0.5, z], 0.02);
    beam('Цепочка лампады', 'iron', [x, y + 0.1, z], [x, y + 0.5, z], 0.015); put('Лампада', m, x, y, z, 0.17, 0.12, 0.17, { round: true }); put('Масло лампады', 'soot', x, y + 0.11, z, 0.12, 0.015, 0.12, { round: true });
    if (lit) flame(x, y + 0.12, z);
  };
  /// Кадило на цепи от потолка или перекрытия над точкой; bottom — низ чаши. false — над точкой нет опоры.
  A.censer = (x, z, bottom, { max = 12, m = 'bronze' } = {}) => {
    let top = bottom + 0.5; while (top < bottom + max && S.air(x, top, z)) top += 0.1;
    if (top >= bottom + max) return false;
    beam('Цепь кадила', 'iron', [x, bottom + 0.36, z], [x, top + 0.04, z], 0.025);
    for (let i = 0; i < 3; i++) { const a = (i * 120 + 30) * DEG; beam('Цепочка кадила', 'iron', [x + Math.cos(a) * 0.1, bottom + 0.1, z + Math.sin(a) * 0.1], [x, bottom + 0.38, z], 0.012); }
    put('Чаша кадила', m, x, bottom, z, 0.24, 0.12, 0.24, { round: true }); put('Крышка кадила', m, x, bottom + 0.12, z, 0.15, 0.09, 0.15, { round: true }); put('Зола кадила', 'ash', x, bottom + 0.105, z, 0.2, 0.02, 0.2, { round: true });
    return true;
  };
  /// Табличка с именем: плита, резные строки, розетки по углам.
  A.plaque = (wall, y = 1.4, { w = 0.6, h = 0.4, m = 'marble', lines = 3, studs = 'bronze', roll, name = 'Табличка с именем' } = {}) => {
    if (!wall) return;
    const [k, p, u] = wall, tilt = roll === undefined ? J(1.5) : roll;
    tile(name, m, k, p, u, y, w, h, 0.05, { sink: 0.01, roll: tilt });
    for (let i = 0; i < lines; i++) tile('Резная строка', 'soot', k, p, u + J(0.02), y + h - 0.1 - i * (h - 0.14) / Math.max(1, lines), w * R(0.45, 0.8), 0.022, 0.012, { sink: -0.035, roll: tilt });
    if (studs) for (const sx of [-1, 1]) for (const sy of [0.03, h - 0.07]) tile('Розетка таблички', studs, k, p, u + sx * (w / 2 - 0.05), y + sy, 0.04, 0.04, 0.02, { sink: -0.035 });
  };
  /// Киот: тёмная ниша с полкой и сводом. Возвращает точку на полке [x, y, z] для приношений.
  A.niche = (wall, y = 1.1, { w = 0.7, h = 0.8, m = 'stone' } = {}) => {
    if (!wall) return null;
    const [k, p, u] = wall, [x, z] = D.at(k, p, u, 0.16);
    tile('Ниша киота', 'soot', k, p, u, y, w, h, 0.03, { sink: 0 }); tile('Полка киота', m, k, p, u, y - 0.1, w + 0.24, 0.1, 0.34, { sink: 0.02 });
    for (const s of [-1, 1]) tile('Столбик киота', m, k, p, u + s * (w / 2 + 0.06), y, 0.12, h, 0.14, { sink: 0.02 });
    tile('Свод киота', m, k, p, u, y + h, w + 0.3, 0.16, 0.18, { sink: 0.02, roll: J(1.5) });
    return [x, y, z];
  };
  /// Приношения родни: чаша с монетами, засохшие цветы, плошки, поминальный хлебец, кубок.
  A.offer = (x, z, { y, n = 4, spread = 0.25, coins = true } = {}) => {
    const by = y === undefined ? F(x, z) : y;
    put('Чаша для приношений', 'bronze', x, by, z, 0.22, 0.07, 0.22, { round: true });
    if (coins) for (let i = 0; i < 3; i++) put('Монета', 'gold', x + J(0.06), by + 0.07, z + J(0.06), 0.035, 0.008, 0.035, { round: true });
    for (let i = 0; i < n; i++) {
      const px = x + J(spread), pz = z + J(spread), kind = D.rnd(), a = R(0, 360);
      if (kind < 0.35) { for (let j = 0; j < 3; j++) put('Засохший цветок', 'straw', px, by + 0.012, pz, 0.015, R(0.18, 0.3), 0.015, { pitch: 90, ry: a + J(14) }); put('Перевязь букета', 'clothRed', px + Math.sin(a * DEG) * 0.04, by, pz + Math.cos(a * DEG) * 0.04, 0.05, 0.03, 0.03, { ry: a }); }
      else if (kind < 0.6) put('Плошка', 'clay', px, by, pz, 0.12, 0.05, 0.12, { round: true });
      else if (kind < 0.8) put('Поминальный хлебец', 'straw', px, by, pz, 0.14, 0.05, 0.1, { ry: a });
      else put('Кубок', 'bronze', px, by, pz, 0.07, 0.13, 0.07, { round: true });
    }
  };
  /// Паутина в углу, от стены к стене: x, z — точка у угла (до 2,5 м от обеих стен), y — её верх.
  A.web = (x, z, y, { r = 0.9, n = 5 } = {}) => {
    const near = (a, b, v) => { const wa = S.wall(x, z, a, y, 2.5), wb = S.wall(x, z, b, y, 2.5); return !wb || (wa && Math.abs(wa[1] - v) <= Math.abs(wb[1] - v)) ? wa : wb; }, wx = near('E', 'W', x), wz = near('N', 'S', z);
    if (!wx || !wz) return;
    const sx = Math.sign(wx[1] - x) || 1, sz = Math.sign(wz[1] - z) || 1;
    S.web([wx[1] + sx * 0.03, y, wz[1] - sz * r], [wx[1] - sx * r, y, wz[1] + sz * 0.03], { n });
  };
  /// Рассыпанные монеты.
  A.coins = (x, y, z, n = 6, spread = 0.2, m = 'gold') => { for (let i = 0; i < n; i++) put('Монета', m, x + J(spread), y, z + J(spread), 0.04, 0.008, 0.04, { round: true }); };
  /// Засохший венок на стене.
  A.wreath = (wall, y = 1.5, { d = 0.42 } = {}) => {
    if (!wall) return;
    const [k, p, u] = wall;
    disc('Засохший венок', 'straw', k, p, u, y, d, 0.05); disc('Середина венка', 'soot', k, p, u, y, d * 0.55, 0.06);
    tile('Лента венка', 'clothRed', k, p, u + J(0.04), y - d / 2 - 0.2, 0.05, 0.26, 0.015, { sink: -0.02, roll: J(8) });
  };
  /// Покров: полотнище на крышке и свисающие края с каймой. P — оси (A.box), y — верх над подошвой.
  A.pall = (P, y, len, wid, { m = 'clothRed', drop = 0.4, slip = 0, trim = 'gold' } = {}) => {
    P.part('Покров', m, slip, y, 0, len, 0.015, wid + 0.06, { ry: J(1.5) });
    for (const s of [-1, 1]) { const dr = drop * R(0.55, 1); P.part('Край покрова', m, slip, y - dr, s * (wid / 2 + 0.035), len, dr + 0.012, 0.015); if (trim) P.part('Кайма покрова', trim, slip, y - dr, s * (wid / 2 + 0.04), len, 0.03, 0.012); }
  };
  /// Надгробная фигура на крышке: голова к +x местной оси, y — верх крышки.
  A.effigy = (P, y, { m = 'marble', len = 1.7, sword = true, headless = false } = {}) => {
    P.part('Подушка фигуры', m, len / 2 - 0.16, y, 0, 0.34, 0.07, 0.4);
    if (!headless) P.part('Голова фигуры', m, len / 2 - 0.16, y + 0.07, 0, 0.2, 0.2, 0.2, { round: true });
    P.part('Торс фигуры', m, len / 2 - 0.62, y, 0, 0.62, 0.22, 0.46); P.part('Сложенные руки', m, len / 2 - 0.55, y + 0.22, 0, 0.2, 0.06, 0.3);
    P.part('Ноги фигуры', m, -0.22, y, 0, 0.9, 0.16, 0.36); P.part('Стопы фигуры', m, -len / 2 + 0.1, y, 0, 0.14, 0.26, 0.32);
    if (sword) { P.part('Каменный меч', m, -0.05, y + 0.22, 0, 1, 0.03, 0.06); P.part('Гарда меча', m, 0.32, y + 0.22, 0, 0.05, 0.035, 0.24); }
  };
  /// Скелет на полу: голова к +x местной оси, scatter — насколько кости разошлись.
  A.skeleton = (x, z, { y, ry = 0, scatter = 0.04 } = {}) => {
    const by = y === undefined ? F(x, z) : y, P = fr(x, by, z, ry), j = () => J(scatter);
    P.part('Череп', 'bone', 0.78 + j(), 0, j(), 0.2, 0.19, 0.22, { round: true, ry: J(30) });
    for (let i = 0; i < 5; i++) P.part('Ребро', 'bone', 0.58 - i * 0.07 + j(), 0, j(), 0.03, 0.12 - i * 0.008, 0.3 - i * 0.012, { ry: J(6) });
    P.part('Хребет', 'bone', 0.4 + j(), 0, j(), 0.5, 0.04, 0.045, { ry: J(4) }); P.part('Таз', 'bone', 0.1 + j(), 0, j(), 0.14, 0.09, 0.3, { ry: J(8) });
    for (const s of [-1, 1]) { P.part('Плечевая кость', 'bone', 0.45 + j(), 0, s * 0.22 + j(), 0.32, 0.04, 0.04, { ry: s * 8 + J(10) }); P.part('Бедренная кость', 'bone', -0.15 + j(), 0, s * 0.1 + j(), 0.44, 0.05, 0.05, { ry: s * 4 + J(8) }); P.part('Берцовая кость', 'bone', -0.6 + j(), 0, s * 0.11 + j(), 0.42, 0.04, 0.04, { ry: J(10) }); }
  };
  /// Скелет, сидящий у стены: спина к стене, ноги в комнату.
  A.sitter = (wall, { y } = {}) => {
    if (!wall) return;
    const [k, p, u] = wall, [x, z] = D.at(k, p, u, 0.14), by = y === undefined ? F(x, z) : y, P = fr(x, by, z, D.FACE[k].ry);
    P.part('Таз', 'bone', 0, 0, 0.04, 0.3, 0.09, 0.16); P.part('Хребет', 'bone', 0, 0.08, -0.05, 0.045, 0.46, 0.045);
    for (let i = 0; i < 4; i++) P.part('Ребро', 'bone', 0, 0.2 + i * 0.08, -0.02, 0.3 - i * 0.02, 0.03, 0.14);
    P.part('Череп', 'bone', J(0.03), 0.53, 0, 0.2, 0.19, 0.22, { round: true, ry: J(25) });
    for (const s of [-1, 1]) { P.part('Бедренная кость', 'bone', s * 0.1, 0, 0.32, 0.05, 0.05, 0.44, { ry: s * 6 }); P.part('Берцовая кость', 'bone', s * 0.14, 0, 0.74, 0.04, 0.04, 0.42, { ry: s * 9 }); P.part('Кость руки', 'bone', s * 0.24, 0, 0.16, 0.04, 0.04, 0.5, { ry: s * 18 }); }
    return P;
  };
  /// Тело в саване: свёрток с перевязями, голова к +x местной оси.
  A.shroud = (x, y, z, { ry = 0, len = 1.7, m = 'cloth' } = {}) => {
    const P = fr(x, y, z, ry);
    P.part('Тело в саване', m, -0.1, 0, 0, len - 0.36, 0.2, 0.42, { ry: J(2) }); P.part('Голова в саване', m, len / 2 - 0.14, 0, 0, 0.24, 0.22, 0.24, { round: true }); P.part('Стопы в саване', m, -len / 2 + 0.08, 0, 0, 0.14, 0.26, 0.3);
    for (const t of [-0.55, -0.1, 0.35]) P.part('Перевязь савана', 'rope', t, 0, 0, 0.035, 0.21, 0.44);
  };
  /// Урна: целая или разбитая, с просыпанным прахом.
  A.urn = (x, z, { y, h = 0.5, m = 'clay', broken = false } = {}) => {
    const by = y === undefined ? F(x, z) : y;
    if (broken) { put('Донце урны', m, x, by, z, h * 0.5, h * 0.3, h * 0.5, { round: true }); for (let i = 0; i < 4; i++) put('Черепок', m, x + J(0.28), by, z + J(0.28), R(0.08, 0.16), 0.02, R(0.06, 0.12), { ry: R(0, 180), roll: J(18) }); put('Просыпанный прах', 'ash', x + J(0.08), by, z + J(0.08), 0.5, 0.012, 0.36, { round: true, ry: R(0, 180) }); return; }
    put('Ножка урны', m, x, by, z, h * 0.3, h * 0.1, h * 0.3, { round: true }); put('Урна', m, x, by + h * 0.1, z, h * 0.56, h * 0.6, h * 0.56, { round: true });
    put('Горло урны', m, x, by + h * 0.7, z, h * 0.34, h * 0.2, h * 0.34, { round: true }); put('Крышка урны', m, x, by + h * 0.9, z, h * 0.42, h * 0.1, h * 0.42, { round: true });
  };
  /// Погасший костёр жильщиков.
  A.ashes = (x, z, { y, r = 0.38 } = {}) => {
    const by = y === undefined ? F(x, z) : y;
    for (let i = 0; i < 7; i++) { const a = i / 7 * 2 * Math.PI + J(0.2); put('Камень кострища', 'rock', x + Math.cos(a) * r, by, z + Math.sin(a) * r, R(0.14, 0.24), R(0.08, 0.15), R(0.12, 0.2), { ry: R(0, 180) }); }
    put('Зола', 'ash', x, by, z, r * 1.6, 0.03, r * 1.6, { round: true }); D.patch(x, z, r * 1.5, { m: 'soot', name: 'Гарь', y: by, n: 3 });
    for (let i = 0; i < 3; i++) put('Головня', 'soot', x + J(r * 0.5), by + 0.05, z + J(r * 0.5), 0.05, R(0.2, 0.34), 0.05, { round: true, pitch: 90, ry: R(0, 360) });
  };
  /// Стоянка жильщиков: спальник, скатка, мешок, кружка, огарок, погасший костёр. Только в углах и тупиках.
  A.camp = (x, z, { y, ry = 0, fire = true } = {}) => {
    const by = y === undefined ? F(x, z) : y, P = fr(x, by, z, ry);
    P.part('Спальник', 'cloth', 0, 0, 0, 0.7, 0.07, 1.8, { ry: J(6) }); P.part('Скатка', 'clothBlue', J(0.05), 0.07, 0.72, 0.5, 0.14, 0.22);
    P.part('Заплечный мешок', 'leather', 0.62, 0, 0.62, 0.36, 0.42, 0.3, { ry: J(20), round: true }); P.part('Клапан мешка', 'cloth', 0.62, 0.42, 0.62, 0.3, 0.04, 0.26, { round: true });
    P.part('Кружка', 'iron', 0.5, 0, -0.1, 0.08, 0.09, 0.08, { round: true }); P.part('Огарок', 'bone', 0.48, 0, 0.18, 0.05, 0.07, 0.05, { round: true }); P.part('Фляга', 'leather', 0.56, 0, -0.42, 0.14, 0.07, 0.24, { ry: J(30), round: true });
    if (fire) { const [fx, fz] = P.P(1.25, -0.3); A.ashes(fx, fz, { y: by }); }
  };
  /// Мел жильщиков на стене: стрелка вдоль стены (arrow ±1 по её координате), крест (cross — пусто, вскрыто), круг (ring — не тронуто).
  A.chalk = (wall, y = 1.5, { arrow, cross, ring } = {}) => {
    if (!wall) return;
    const [k, p, u] = wall, sg = { N: 1, S: -1, E: -1, W: 1 }[k], st = (du, dy, w, roll = 0) => tile('Метка мелом', 'bone', k, p, u + du, y + dy, w, 0.03, 0.012, { sink: 0, roll: roll * sg });
    if (arrow) { st(0, 0, 0.44); st(arrow * 0.16, 0.05, 0.16, -arrow * 38); st(arrow * 0.16, -0.05, 0.16, arrow * 38); }
    if (cross) { st(0, 0, 0.3, 45); st(0, 0, 0.3, -45); }
    if (ring) { disc('Круг мелом', 'bone', k, p, u, y, 0.26, 0.01); disc('Середина круга', 'soot', k, p, u, y, 0.19, 0.02); }
  };
  /// Инструмент взлома на полу: лом, зубило, молоток.
  A.pry = (x, z, { y, ry = 0 } = {}) => {
    const by = y === undefined ? F(x, z) : y, P = fr(x, by, z, ry);
    P.part('Лом', 'iron', 0, 0, 0, 0.035, 0.035, 0.95, { ry: J(15) }); P.part('Зубило', 'iron', 0.2, 0, 0.1, 0.03, 0.03, 0.22, { ry: R(0, 90) });
    P.part('Боёк молотка', 'iron', -0.25, 0, 0.36, 0.1, 0.06, 0.06, { ry: 30 }); P.part('Рукоять молотка', 'timber', -0.25, 0.012, 0.2, 0.03, 0.03, 0.34, { ry: 30 });
  };
  /// Знак Спящих: круг свечей, руна на полу, череп и кости-приношения. Только в тупиках.
  A.sleepers = (x, z, { y, r = 0.6, n = 7, lit = true } = {}) => {
    const by = y === undefined ? F(x, z) : y;
    for (let i = 0; i < n; i++) { const a = i / n * 2 * Math.PI, h = R(0.05, 0.2), cx = x + Math.cos(a) * r, cz = z + Math.sin(a) * r; put('Свеча Спящих', 'bone', cx, by, cz, 0.05, h, 0.05, { round: true }); if (lit) flame(cx, by + h, cz); put('Наплыв воска', 'bone', cx, by, cz, 0.14, 0.012, 0.12, { round: true }); }
    for (let i = 0; i < 3; i++) put('Черта руны', 'rune', x, by + 0.004, z, r * 1.5, 0.012, 0.035, { ry: i * 60 + J(6) });
    put('Череп', 'bone', x, by + 0.016, z, 0.2, 0.19, 0.22, { round: true, ry: R(0, 360) });
    for (let i = 0; i < 6; i++) put('Кость-приношение', 'bone', x + J(0.28), by + 0.012, z + J(0.28), R(0.2, 0.4), 0.05, 0.05, { ry: R(0, 180) });
  };
  /// Руны Спящих на стене.
  A.runes = (wall, y = 1.4, n = 4) => {
    if (!wall) return;
    const [k, p, u] = wall;
    for (let i = 0; i < n; i++) { const du = (i - (n - 1) / 2) * 0.22; tile('Руна', 'rune', k, p, u + du, y, 0.025, R(0.16, 0.26), 0.012, { sink: 0, roll: J(8) }); tile('Руна', 'rune', k, p, u + du, y + R(0.05, 0.15), R(0.08, 0.14), 0.025, 0.012, { sink: 0, roll: pick([-35, 0, 35]) }); }
  };
  /// Аналой с книгой: читающий стоит со стороны +z местной оси.
  A.lectern = (x, z, { y, ry = 0, book = true } = {}) => {
    const by = y === undefined ? F(x, z) : y, P = fr(x, by, z, ry);
    P.part('Основание аналоя', 'timber', 0, 0, 0, 0.5, 0.06, 0.45); P.part('Стойка аналоя', 'timber', 0, 0.06, 0, 0.12, 1.02, 0.12); P.part('Доска аналоя', 'wood', 0, 1.06, 0, 0.6, 0.04, 0.46, { pitch: 20 });
    if (book) { P.part('Переплёт книги', 'leather', 0, 1.11, 0, 0.46, 0.03, 0.34, { pitch: 20 }); P.part('Страницы', 'paper', 0, 1.14, 0, 0.42, 0.02, 0.3, { pitch: 20 }); P.part('Закладка', 'clothRed', 0.08, 1.14, 0.2, 0.03, 0.01, 0.2, { pitch: 20 }); }
    return P;
  };
  /// Раскрытая или закрытая книга на поверхности.
  A.book = (x, y, z, { ry = 0, open = true, m = 'leather' } = {}) => {
    const P = fr(x, y, z, ry);
    if (!open) { P.part('Книга', m, 0, 0, 0, 0.26, 0.07, 0.34); return; }
    P.part('Переплёт книги', m, 0, 0, 0, 0.5, 0.02, 0.34); for (const s of [-1, 1]) P.part('Страницы', 'paper', s * 0.12, 0.02, 0, 0.22, 0.03, 0.3); P.part('Закладка', 'clothRed', 0.02, 0.05, -0.1, 0.03, 0.006, 0.3);
  };
  /// Скамеечка для коленопреклонения с подушкой.
  A.kneeler = (x, z, { y, ry = 0 } = {}) => {
    const by = y === undefined ? F(x, z) : y, P = fr(x, by, z, ry);
    P.part('Скамеечка', 'wood', 0, 0.12, 0, 0.6, 0.04, 0.28); for (const s of [-1, 1]) P.part('Ножка скамеечки', 'timber', s * 0.25, 0, 0, 0.04, 0.12, 0.26); P.part('Подушка', 'clothRed', 0, 0.16, 0, 0.5, 0.05, 0.22, { ry: J(5) });
  };
  /// Напольный шандал: стойка, чаша, свечи; fallen — лежит, ry — куда упал.
  A.stand = (x, z, { y, h = 1.4, m = 'bronze', arms = 3, lit = false, fallen = false, ry = 0 } = {}) => {
    const by = y === undefined ? F(x, z) : y;
    if (fallen) { put('Упавший шандал', m, x, by + 0.03, z, 0.05, h, 0.05, { round: true, pitch: 90, ry }); put('Основание шандала', m, x, by + 0.15, z, 0.3, 0.04, 0.3, { round: true, pitch: 90, ry }); put('Чаша шандала', m, x + Math.sin(ry * DEG) * h, by + 0.14, z + Math.cos(ry * DEG) * h, 0.28, 0.03, 0.28, { round: true, pitch: 90, ry }); return; }
    put('Основание шандала', m, x, by, z, 0.32, 0.05, 0.32, { round: true }); put('Стойка шандала', m, x, by + 0.05, z, 0.05, h - 0.05, 0.05, { round: true }); put('Узел шандала', m, x, by + h * 0.5, z, 0.1, 0.08, 0.1, { round: true }); put('Чаша шандала', m, x, by + h, z, 0.3, 0.03, 0.3, { round: true });
    for (let i = 0; i < arms; i++) { const a = (i / arms * 360 + 20) * DEG, cx = x + (arms > 1 ? Math.cos(a) * 0.1 : 0), cz = z + (arms > 1 ? Math.sin(a) * 0.1 : 0), ch = R(0.05, 0.2); put('Свеча', 'bone', cx, by + h + 0.03, cz, 0.045, ch, 0.045, { round: true }); if (lit) flame(cx, by + h + 0.03 + ch, cz); }
    for (let i = 0; i < 2; i++) { const len = R(0.1, 0.32), a = R(0, 6.28); put('Потёк воска', 'bone', x + Math.cos(a) * 0.14, by + h - len + 0.02, z + Math.sin(a) * 0.14, 0.02, len, 0.02, { round: true }); }
  };
  /// Обломки крышки и каменная крошка у вскрытого.
  A.shards = (x, z, { y, m = 'stone', n = 3, size = 0.55 } = {}) => {
    const by = y === undefined ? F(x, z) : y;
    for (let i = 0; i < n; i++) put('Обломок крышки', m, x + J(0.4), by, z + J(0.4), size * R(0.5, 1), 0.12, size * R(0.4, 0.8), { ry: R(0, 180), roll: chance(0.4) ? J(12) : 0 });
    D.rubble(x, z, 0.5, 6, { m, y: by, pile: 0.1, name: 'Каменная крошка' });
  };
  /// Погребальные носилки у стены; body — на них тело в саване.
  A.bier = (x, z, { y, ry = 0, body = false } = {}) => {
    const by = y === undefined ? F(x, z) : y, P = fr(x, by, z, ry);
    for (const s of [-1, 1]) { P.part('Жердь носилок', 'timber', 0, 0.3, s * 0.3, 2.3, 0.06, 0.06); for (const t of [-0.8, 0.8]) P.part('Ножка носилок', 'timber', t, 0, s * 0.3, 0.06, 0.3, 0.06); }
    for (let i = 0; i < 6; i++) P.part('Планка носилок', 'wood', -0.75 + i * 0.3, 0.36, 0, 0.2, 0.025, 0.66, { ry: J(3) });
    if (body) A.shroud(x, by + 0.385, z, { ry });
  };
  /// Пилястра на стене от y0 на высоту h: база, тело, капитель.
  A.pilaster = (wall, y0, h, { m = 'stone', w = 0.5 } = {}) => {
    if (!wall) return;
    const [k, p, u] = wall;
    tile('База пилястры', m, k, p, u, y0, w + 0.16, 0.3, 0.2, { sink: 0.02 }); tile('Пилястра', m, k, p, u, y0 + 0.3, w, h - 0.62, 0.12, { sink: 0.02, roll: J(0.3) }); tile('Капитель пилястры', m, k, p, u, y0 + h - 0.32, w + 0.2, 0.32, 0.22, { sink: 0.02 });
  };
  /// Герб на стене: щит и девизная лента.
  A.crest = (wall, y = 2.2, { m = 'bronze', s = 0.6 } = {}) => {
    if (!wall) return;
    const [k, p, u] = wall;
    tile('Щит герба', m, k, p, u, y, s, s * 0.8, 0.05, { sink: 0.01 }); disc('Низ герба', m, k, p, u, y, s, 0.04, s * 0.9); disc('Знак герба', 'soot', k, p, u, y + s * 0.3, s * 0.4, 0.05);
    tile('Девизная лента', 'marble', k, p, u, y - s * 0.75, s * 1.3, 0.12, 0.03, { sink: 0.005, roll: J(2) });
  };
  /// Оковка гроба-сундука (его тело остаётся видимым): обручи, крышка, знак, ручки; ajar 0…1 — крышка сдвинута.
  A.coffin = (o, { m = 'wood', ajar = 0, sign = 'bronze', bands = 'iron' } = {}) => {
    const P = A.box(o), w = P.wid, h = o.s[1], d = P.len;
    for (const t of [-0.34, 0, 0.34]) P.part('Обруч гроба', bands, t * d, 0, 0, 0.06, h + 0.015, w + 0.04);
    P.part('Крышка гроба', m, ajar * 0.25, h, ajar * 0.24, d + 0.05, 0.07, w + 0.05, { ry: ajar * 9 });
    if (sign) P.part('Знак на крышке', sign, ajar * 0.25 + d * 0.22, h + 0.07, ajar * 0.24, 0.3, 0.012, 0.3, { round: true });
    for (const s of [-1, 1]) P.part('Ручка гроба', bands, 0, h * 0.5, s * (w / 2 + 0.02), 0.3, 0.04, 0.03);
    return P;
  };
  /// Приметы саркофага поверх его набора: табличка в головах, огарки, венок, урна у ног, метка жильщиков. rich — бронза и приношения.
  A.grave = (o, { rich = false, mark = 0.25, extras = 1 } = {}) => {
    const y = o.p[1], h = o.s[1], P = A.box(o), len = P.len, wid = P.wid;
    P.part(rich ? 'Бронзовая табличка' : 'Высеченный номер', rich ? 'bronze' : 'soot', len / 2 - 0.062, h * 0.4, 0, 0.012, rich ? 0.2 : 0.11, rich ? 0.44 : 0.28);
    if (chance(0.4 * extras)) for (let i = 0, n = Math.round(R(1, 3)); i < n; i++) { const ch = R(0.03, 0.14), lx = J(len / 2 - 0.4), lz = J(0.14); P.part('Огарок', 'bone', lx, h, lz, 0.045, ch, 0.045, { round: true }); P.part('Наплыв воска', 'bone', lx, h, lz, 0.13, 0.012, 0.11, { round: true }); }
    if (chance(0.22 * extras)) { const lx = J(len / 2 - 0.5); P.part('Засохший венок', 'straw', lx, h, 0, 0.36, 0.04, 0.36, { round: true }); P.part('Середина венка', 'soot', lx, h + 0.006, 0, 0.2, 0.04, 0.2, { round: true }); }
    if (chance(0.25 * extras)) { const [ux, uz] = P.P(-len / 2 - 0.26, J(0.3)); if (S.floor(ux, uz, y) !== undefined && Math.abs(S.floor(ux, uz, y) - y) < 0.1) A.urn(ux, uz, { y, h: R(0.3, 0.42), m: rich ? 'bronze' : 'clay', broken: chance(0.35) }); }
    if (chance(mark)) for (const a of [45, -45]) P.part('Метка мелом', 'bone', J(0.02) - len * 0.2, h * 0.34, wid / 2 - 0.04, 0.3, 0.03, 0.012, { roll: a });
    if (chance(0.3)) P.part('Пыль', 'dirt', J(len / 4), h, 0, R(0.4, 0.8), 0.012, wid * 0.36, { round: true });
    return P;
  };
  /// Вскрытый саркофаг вместо набора: пустое тело, крышка прислонена к боку (side ±1 по местной z). bones — внутри разворошённые кости.
  A.openTomb = (o, { m = 'stone', side = 1, bones = false } = {}) => {
    const h = o.s[1], P = A.box(o), len = P.len, wid = P.wid;
    P.part('Цоколь саркофага', m, 0, 0, 0, len, 0.16, wid);
    for (const s of [-1, 1]) { P.part('Стенка саркофага', m, 0, 0.16, s * (wid / 2 - 0.13), len - 0.14, h - 0.36, 0.12); P.part('Торец саркофага', m, s * (len / 2 - 0.13), 0.16, 0, 0.12, h - 0.36, wid - 0.38); }
    P.part('Тьма внутри', 'soot', 0, 0.16, 0, len - 0.38, 0.03, wid - 0.38);
    if (bones) for (let i = 0; i < 7; i++) P.part(i ? 'Кость' : 'Череп', 'bone', J(len / 2 - 0.4), 0.19, J(wid / 2 - 0.3), i ? R(0.2, 0.4) : 0.2, i ? 0.05 : 0.19, i ? 0.05 : 0.22, { ry: R(0, 180), round: !i });
    P.part('Сброшенная крышка', m, J(0.15), 0.5, side * (wid / 2 + 0.2), len - 0.03, 0.13, wid - 0.06, { pitch: side * 70 });
    return P;
  };
  return A;
};
