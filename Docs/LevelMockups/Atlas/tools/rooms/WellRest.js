// Вид. Водяной пост: тёсаный зал с колодцем под навесом, столбы и обломки стен, две сторожевые террасы; сыро у колодца.
// История: сторож Ханс вёл книгу воды. В ночь Первой Осыпи он раздал всю воду без записи (посуда так и брошена у колодца) и закрыл
// проёмы изнутри (скобы засовов на арках, сломанные брусья рядом). Мёртвые сюда не ходят; жильщики держат здесь привал.
// 112 тел «Dressing» блокаута — утварь привала, повёрнутая кватернионом: в атласе их тела скрыты, вид собран заново из частей.
exports.dress = S => {
  const { D } = S, { put, tile, beam, R, J, pick, chance } = D, RAD = Math.PI / 180;
  // Сцена не знает кватернионов: тела «Dressing» убраны из запросов геометрии, чтобы не давали мнимых стен.
  for (const b of S.boxes) if (b.o.name === 'Dressing') { b.y0 = b.foot = -100; b.y1 = -99.9; }
  const damp = (x, y, z) => 0.1 + Math.max(0, 0.45 - Math.hypot(x, z) / 16) + (y > 0.9 ? -0.08 : 0);
  S.theme({ rock: 'stone', wall: 'stone', floor: 'cobble', platform: 'stone', stairs: 'slab', damp, age: 1.1, density: 1.3, mat: o => { if (o.name === 'Dressing') { o.bare = o.hide = true; return 'timber'; } } });
  S.keep(-4.4, 0, 0.5, -3.2, 1.6, 1.9);                                                                      // конторка с книгой воды
  S.auto();

  // ---------- помощники ----------
  const wl = (x, z, k, y = 1.5) => { const w = S.wall(x, z, k, y, 6); if (!w) throw new Error(`WellRest: нет стены от ${x}, ${z} на ${k}`); return w; };
  const log = (name, m, x, y, z, len, dia, yaw) => put(name, m, x, y + dia / 2, z, dia, len, dia, { round: true, pitch: 90, ry: yaw });
  const local = (x, y, z, ry) => { const c = Math.cos(ry * RAD), sn = Math.sin(ry * RAD); return (name, m, lx, ly, lz, pw, ph, pd, o = {}) => put(name, m, x + lx * c + lz * sn, y + ly, z - lx * sn + lz * c, pw, ph, pd, { ...o, ry: ry + (o.ry || 0) }); };
  const sconce = (k, plane, u, y = 2.05) => { tile('Держатель факела', 'iron', k, plane, u, y - 0.2, 0.1, 0.34, 0.3, { sink: 0.02, pitch: -20 }); const [x, z] = D.at(k, plane, u, 0.24); put('Огарок факела', 'soot', x, y - 0.12, z, 0.07, 0.24, 0.07, { round: true }); for (let i = 0; i < 4; i++) D.blot('Старая копоть', 'soot', k, plane, u + J(0.1), y + 0.45 + i * 0.22, R(0.28, 0.45) - i * 0.04, R(0.28, 0.42), 0.012, -99); };
  const chalk = (w, y = 1.55) => { for (const s of [-1, 1]) tile('Меловой знак', 'bone', w[0], w[1], w[2], y, 0.24, 0.025, 0.012, { sink: 0, roll: s * 45 }); };
  /// Посудина на полу: стоит или лежит на боку.
  const vessel = (x, y, z, dia, h, lying = false) => {
    const name = h > 0.42 ? (dia < 0.2 ? 'Бутыль' : 'Ведро') : h > 0.27 ? 'Кувшин' : 'Горшок', m = name === 'Ведро' ? 'wood' : name === 'Бутыль' ? pick(['glass', 'clay']) : 'clay';
    if (lying) return put(`${name} на боку`, m, x, y + dia / 2, z, dia, h, dia, { round: true, pitch: 90, ry: R(0, 360) });
    put(name, m, x, y, z, dia, h, dia, { round: true });
    if (name === 'Ведро') { put('Обруч ведра', 'iron', x, y + h * 0.7, z, dia + 0.02, 0.03, dia + 0.02, { round: true }); put('Дужка ведра', 'iron', x, y + h, z, dia * 0.9, 0.1, 0.02, { ry: R(0, 180) }); }
    else if (name !== 'Бутыль') put('Горло', m, x, y + h, z, dia * 0.55, 0.04, dia * 0.55, { round: true });
  };
  const crate = P => { put('Ящик', 'wood', P.x, P.y, P.z, P.w, P.h - 0.02, P.d, { ry: P.ry }); for (const t of [0.12, 0.74]) put('Обвязка ящика', 'timber', P.x, P.y + P.h * t, P.z, P.w + 0.035, 0.07, P.d + 0.035, { ry: P.ry }); if (chance(0.4)) put(pick(['Свёрток', 'Моток верёвки', 'Миска']), pick(['cloth', 'rope', 'clay']), P.x + J(0.1), P.y + P.h - 0.02, P.z + J(0.1), 0.26, 0.1, 0.2, { ry: R(0, 90), round: true }); };

  // ---------- вещи привала из тел «Dressing» ----------
  const rot = ([x, y, z, w], [a, b, c]) => { const tx = 2 * (y * c - z * b), ty = 2 * (z * a - x * c), tz = 2 * (x * b - y * a); return [a + w * tx + y * tz - z * ty, b + w * ty + z * tx - x * tz, c + w * tz + x * ty - y * tx]; };
  /// Тело в мире: центр низа, размеры по осям (w вдоль местной x, d вдоль второй лежачей оси), высота и курс.
  const pose = o => {
    const q = o.q || [0, Math.sin((o.ry || 0) * RAD / 2), 0, Math.cos((o.ry || 0) * RAD / 2)], ax = [[1, 0, 0], [0, 1, 0], [0, 0, 1]].map(v => rot(q, v));
    const up = [0, 1, 2].reduce((b, i) => Math.abs(ax[i][1]) > Math.abs(ax[b][1]) ? i : b, 0), [i, j] = [0, 1, 2].filter(t => t !== up), h = o.s[up], c = [0, 1, 2].map(t => o.p[t] + ax[1][t] * o.s[1] / 2);
    const y0 = c[1] - h / 2, f = S.floor(c[0], c[2], y0);
    return { x: c[0], y: f !== undefined && Math.abs(f - y0) < 0.4 ? f : y0, z: c[2], w: o.s[i], d: o.s[j], h, ry: Math.atan2(ax[j][0], ax[j][2]) / RAD, key: o.s.map(v => v.toFixed(2)).join('x') };
  };
  const KIND = { '1.45x1.46x6.00': 'skip', '0.28x0.28x0.80': 'skip', '0.15x0.40x0.70': 'sconce', '0.49x0.46x1.40': 'post', '1.86x1.43x2.60': 'well', '2.00x0.68x0.43': 'kerb', '0.40x0.23x2.00': 'lantern', '1.09x0.45x1.80': 'cot', '0.60x1.30x0.70': 'lectern', '0.53x0.51x0.50': 'stool',
    '0.50x0.45x0.50': 'stump', '0.80x0.15x2.00': 'bedroll', '0.32x0.05x0.40': 'plate', '0.63x0.21x0.80': 'shelf', '0.63x0.20x0.80': 'shelf', '0.80x0.35x0.53': 'sack', '0.80x0.60x0.49': 'sack', '0.52x0.35x0.70': 'pack', '0.35x0.28x0.60': 'coffer', '0.56x0.37x0.80': 'coffer', '0.60x0.28x0.20': 'roll', '0.16x0.14x0.50': 'bottle' };
  const make = {
    skip: () => {},
    sconce: P => Math.abs(P.x) > 16 ? sconce(P.x > 0 ? 'W' : 'E', Math.sign(P.x) * 16.8, P.z) : sconce(P.z > 0 ? 'S' : 'N', Math.sign(P.z) * 16.8, P.x),
    shelf: P => D.shelf(P.x > 0 ? 'W' : 'E', Math.sign(P.x) * 16.8, P.z, { y: 1.4, w: 0.8, items: 2 }),
    // Тумба у колодца: каменный столбик с кованым фонарём.
    post: P => { put('Тумба', 'slab', P.x, P.y, P.z, 0.42, 1.05, 0.42, { ry: J(4) }); put('Шапка тумбы', 'slab', P.x, P.y + 1.05, P.z, 0.52, 0.1, 0.52, { ry: J(5) }); put('Фонарь', 'iron', P.x, P.y + 1.15, P.z, 0.22, 0.26, 0.22); put('Огарок', 'bone', P.x, P.y + 1.15, P.z, 0.06, 0.12, 0.06, { round: true }); put('Крышка фонаря', 'iron', P.x, P.y + 1.41, P.z, 0.28, 0.04, 0.28); },
    // Колодец: кольцо камней, стойки, ворот с цепью, двускатный навес; замок Ханса висит открытым.
    well: P => {
      const y = P.y, n = 12;
      for (let i = 0; i < n; i++) { const a = i / n * 2 * Math.PI; put('Камень колодца', 'slab', P.x + Math.cos(a) * 0.58, y, P.z + Math.sin(a) * 0.58, 0.36, R(0.72, 0.84), 0.26, { ry: 90 - a / RAD + J(5), roll: J(3) }); }
      put('Вода в колодце', 'wet', P.x, y + 0.2, P.z, 0.95, 0.02, 0.95, { round: true });
      for (const s of [-1, 1]) { put('Стойка ворота', 'timber', P.x + s * 0.84, y, P.z, 0.14, 2.1, 0.16, { roll: s * -1.5 }); put('Скат навеса', 'wood', P.x, y + 2.08, P.z + s * 0.36, 2.05, 0.05, 0.86, { pitch: s * 28 }); beam('Подкос навеса', 'timber', [P.x + s * 0.84, y + 1.55, P.z], [P.x + s * 0.4, y + 2.08, P.z], 0.08); }
      put('Конёк навеса', 'timber', P.x, y + 2.42, P.z, 2.1, 0.1, 0.12);
      log('Ворот', 'timber', P.x - 0.84, y + 1.4, P.z, 1.68, 0.2, 90); log('Намотанная цепь', 'iron', P.x - 0.3, y + 1.38, P.z, 0.6, 0.24, 90);
      put('Рукоять ворота', 'iron', P.x + 0.95, y + 1.5, P.z, 0.04, 0.04, 0.34); put('Рукоять ворота', 'iron', P.x + 0.95, y + 1.5, P.z + 0.17, 0.2, 0.04, 0.04);
      D.chain(P.x, y + 1.42, P.z + 0.1, 0.5); put('Ведро', 'wood', P.x, y + 0.5, P.z + 0.1, 0.3, 0.32, 0.3, { round: true }); put('Обруч ведра', 'iron', P.x, y + 0.72, P.z + 0.1, 0.32, 0.03, 0.32, { round: true });
      D.chain(P.x + 0.95, y + 1.5, P.z + 0.3, 0.3); put('Открытый замок', 'iron', P.x + 0.95, y + 0.96, P.z + 0.3, 0.14, 0.16, 0.06, { roll: 25 });
      D.patch(P.x, P.z, 1.6, { y, n: 7 }); D.mossWall('N', P.z + 0.72, P.x + 0.2, y + 0.4, 0.5, { base: y });
    },
    kerb: P => { const c = Math.cos(P.ry * RAD), sn = Math.sin(P.ry * RAD); for (let i = 0; i < 4; i++) { const t = (i + 0.5) / 4 * P.w - P.w / 2; if (!chance(0.06)) put('Бордюрный камень', 'slab', P.x + t * c, 0, P.z - t * sn, P.w / 4 - R(0.015, 0.04), R(0.25, 0.33), P.d - R(0, 0.06), { ry: P.ry + J(2.5), roll: J(2) }); } },
    lantern: P => {
      const hung = dressing.some(o => o.s[2] === 0.8 && Math.hypot(o.p[0] - P.x, o.p[2] - P.z) < 0.5);
      D.chain(P.x, 6, P.z, hung ? 2 : 1.5); put('Скоба в своде', 'iron', P.x, 5.94, P.z, 0.16, 0.06, 0.16);
      if (hung) { put('Фонарь', 'iron', P.x, 3.2, P.z, 0.3, 0.7, 0.3); put('Крышка фонаря', 'iron', P.x, 3.84, P.z, 0.38, 0.08, 0.38); put('Огарок', 'bone', P.x, 3.25, P.z, 0.08, 0.14, 0.08, { round: true }); }
      else { put('Упавший фонарь', 'iron', P.x + 0.4, 0.15, P.z + 0.3, 0.3, 0.7, 0.3, { pitch: 90, ry: 40 }); for (let i = 0; i < 7; i++) put('Осколок стекла', 'glass', P.x + 0.6 + J(0.4), 0, P.z + 0.5 + J(0.4), R(0.04, 0.1), 0.012, R(0.03, 0.08), { ry: R(0, 180) }); }
    },
    cot: P => { S.set('Койка', P.x, P.z, 1, 1.8, 0.5, { ry: P.ry }); },
    lectern: P => { S.set('Конторка', P.x, P.z, P.w, P.d, 1.2, { ry: P.ry }); },
    stool: P => { S.set('Табурет', P.x, P.z, 0.42, 0.42, 0.46, { y: P.y }); },
    stump: P => { put('Чурбак', 'wood', P.x, P.y, P.z, 0.48, 0.44, 0.48, { round: true, ry: R(0, 90), roll: J(2) }); put('Кора чурбака', 'timber', P.x, P.y, P.z, 0.52, 0.3, 0.52, { round: true }); },
    bedroll: P => { const L = local(P.x, P.y, P.z, P.ry + J(4)); L('Спальник', pick(['cloth', 'clothBlue', 'clothRed']), 0, 0.012, 0, 0.74, 0.09, 1.9); L('Откинутое одеяло', 'cloth', 0.1, 0.1, -0.35, 0.7, 0.05, 1, { ry: 6 }); L('Скатка под голову', 'leather', 0, 0.1, 0.8, 0.5, 0.14, 0.22); },
    plate: P => { put('Плошка', 'clay', P.x, P.y, P.z, 0.3, 0.05, 0.3, { round: true }); if (chance(0.6)) put('Огрызок', 'bone', P.x + J(0.05), P.y + 0.05, P.z + J(0.05), 0.14, 0.03, 0.04, { ry: R(0, 180) }); },
    sack: P => { put('Мешок', 'cloth', P.x, P.y, P.z, P.w, P.h, P.d, { ry: P.ry, round: true, roll: J(4) }); put('Завязка мешка', 'rope', P.x, P.y + P.h - 0.03, P.z, 0.2, 0.08, 0.16, { round: true }); },
    pack: P => { const L = local(P.x, P.y, P.z, P.ry); L('Заплечный мешок', 'leather', 0, 0, 0, P.w, P.h * 0.85, P.d, { round: true }); L('Клапан мешка', 'leather', 0, P.h * 0.8, 0, P.w * 0.8, 0.1, P.d * 0.9); L('Скатка', 'cloth', 0, P.h * 0.9, 0, P.w + 0.1, 0.16, 0.18); },
    coffer: P => { const L = local(P.x, P.y, P.z, P.ry); L('Сундучок', 'wood', 0, 0, 0, P.w, P.h - 0.07, P.d); L('Крышка сундучка', 'wood', 0, P.h - 0.07, 0, P.w + 0.03, 0.07, P.d + 0.03, { roll: J(2) }); for (const t of [-0.28, 0.28]) L('Оковка сундучка', 'iron', 0, 0, t * P.d, P.w + 0.02, P.h + 0.005, 0.05); },
    roll: P => { const a = (P.ry + 90) * RAD; log('Скатка', pick(['cloth', 'clothBlue']), P.x - Math.sin(a) * P.w / 2, P.y, P.z - Math.cos(a) * P.w / 2, P.w, 0.22, P.ry + 90); },
    bottle: P => P.h < 0.3 ? log('Бутыль на боку', 'glass', P.x, P.y, P.z, 0.3, 0.1, P.ry + R(0, 60)) : put('Бутыль', 'glass', P.x, P.y, P.z, 0.12, P.h, 0.12, { round: true }),
    vessel: P => vessel(P.x, P.y, P.z, Math.max(P.w, P.d), P.h),
    crate
  };
  const dressing = S.base.filter(o => o.name === 'Dressing'), poses = {};
  for (const o of dressing) { const P = pose(o), kind = KIND[P.key] || (Math.max(P.w, P.d) <= 0.36 ? 'vessel' : 'crate'); (poses[kind] = poses[kind] || []).push(P); make[kind](P); }

  // ---------- колодец: книга воды и ночь, когда воду раздали ----------
  const lect = poses.lectern[0], LB = local(lect.x, lect.y, lect.z, lect.ry);
  LB('Книга воды', 'leather', 0, 1.1, 0, 0.5, 0.05, 0.36, { roll: 12 }); for (const s of [-1, 1]) LB('Пустая страница', 'paper', s * 0.12, 1.15, 0, 0.22, 0.012, 0.32, { roll: 12 });
  D.chain(lect.x + 0.2, 1.1, lect.z + 0.32, 0.5, { name: 'Цепь книги' }); put('Оброненное перо', 'bone', lect.x + 0.5, 0, lect.z - 0.5, 0.02, 0.01, 0.22, { ry: 50 }); put('Высохшая чернильница', 'clay', lect.x + 0.45, 0, lect.z + 0.55, 0.08, 0.07, 0.08, { round: true });
  put('Столб мерных ковшей', 'timber', -3.75, 0, 2.15, 0.12, 1.9, 0.12, { roll: 1.5 }); put('Перекладина столба', 'timber', -3.75, 1.65, 2.15, 0.9, 0.07, 0.07);
  for (const [dx, h] of [[-0.36, 0.2], [-0.12, 0.26], [0.14, 0.16], [0.36, 0.3]]) { put('Мерный ковш', 'bronze', -3.75 + dx, 1.65 - h - 0.12, 2.2, 0.14, 0.12, 0.14, { round: true }); put('Ручка ковша', 'bronze', -3.75 + dx, 1.65 - h, 2.2, 0.02, h, 0.02); }
  put('Табличка: мера воды', 'wood', -3.75, 1.75, 2.22, 0.8, 0.22, 0.03, { roll: -3 });
  // Посуда, с которой пришли в ту ночь, так и брошена вокруг помоста; жильщики оставляют у колодца огарки и монеты.
  for (let i = 0; i < 22; i++) { const a = R(0, 360) * RAD, r = R(3.35, 4.7), x = Math.cos(a) * r, z = Math.sin(a) * r; if (Math.hypot(x + 3.9, z - 1.2) < 1.1) continue; vessel(x, 0, z, R(0.16, 0.34), R(0.2, 0.5), chance(0.4)); }
  for (let i = 0; i < 7; i++) { const a = (i * 51 + 20) * RAD; vessel(Math.cos(a) * R(1.3, 2.1), 0.25, Math.sin(a) * R(1.3, 2.1), R(0.18, 0.3), R(0.2, 0.34), chance(0.3)); }
  for (const [x, z] of [[1.5, -2.6], [-0.6, -2.65], [2.62, 0.4]]) { for (let i = 0; i < 3; i++) put('Огарок', 'bone', x + J(0.2), 0.3, z + J(0.05), 0.05, R(0.04, 0.14), 0.05, { round: true }); for (let i = 0; i < 4; i++) put('Монета', 'gold', x + J(0.3), 0.3, z + J(0.08), 0.05, 0.01, 0.05, { round: true }); }
  S.pool(1.9, 1.6, 0.9, 0.6, { y: 0.25 }); S.pool(-3.6, -2.2, 1.1, 0.7); S.pool(2.4, 3.6, 0.8, 0.5);

  // ---------- засовы на проёмах: Ханс закрыл зал изнутри, брусья потом сломали ----------
  for (const [k, plane] of [['S', 16.8], ['N', -16.8], ['W', 16.8], ['E', -16.8]]) for (const t of [-8.4, 8.4]) {
    for (const s of [-1, 1]) tile('Скоба засова', 'iron', k, plane, t + s * 0.82, 1.1, 0.1, 0.22, 0.2, { sink: 0.02 });
    const side = [1, -1].find(s => { const [x, z] = D.at(k, plane, t + s * 2.4, 0.4); return S.floor(x, z, 0) === 0 && !poses.crate.concat(poses.sack || []).some(P => Math.hypot(P.x - x, P.z - z) < 1.3); }) || 1;
    const [x, z] = D.at(k, plane, t + side * 2.4, 0.3), [x2, z2] = D.at(k, plane, t + side * 1.75, 0.12);
    put('Обломок засова', 'timber', x, 0, z, 1.2, 0.14, 0.16, { ry: D.FACE[k].ry + J(14), roll: J(3) }); beam('Обломок засова', 'timber', [x2, 0, z2 + (k === 'S' ? -0.3 : k === 'N' ? 0.3 : 0)], [x2, 1.25, z2], 0.14);
    for (let i = 0; i < 5; i++) put('Щепа', 'wood', x + J(0.7), 0, z + J(0.3), R(0.1, 0.3), 0.02, R(0.03, 0.07), { ry: R(0, 180) });
  }

  // ---------- закуток Ханса ----------
  const cot = poses.cot[0], CB = local(cot.x, 0, cot.z, cot.ry);
  CB('Плащ сторожа', 'clothBlue', 0, 0.5, -0.15, 0.86, 0.07, 1.3, { ry: 3 }); CB('Череп', 'bone', 0, 0.5, 0.62, 0.2, 0.2, 0.24, { ry: 20 }); CB('Кость руки', 'bone', 0.36, 0.57, 0.1, 0.07, 0.06, 0.5, { ry: 12 }); CB('Связка ключей', 'iron', 0.4, 0.57, -0.2, 0.14, 0.03, 0.14, { round: true }); CB('Мерный ковш', 'bronze', -0.36, 0.57, -0.1, 0.14, 0.1, 0.14, { round: true });
  { const n = wl(-5.9, -4.2, 'N'), w = wl(-5.9, -5, 'W');
    for (let i = 0; i < 4; i++) tile('Надпись Ханса', 'soot', n[0], n[1], -5.9 + J(0.04), 1.75 - i * 0.14, R(0.7, 1.1), 0.03, 0.012, { sink: 0, roll: J(1.5) });
    S.pegs(w, { n: 3, w: 1, y: 1.75 }); D.tally(w[0], w[1], w[2] - 0.9, 1.2, 19);
  }
  for (let i = 0; i < 5; i++) { const h = R(0.05, 0.16), x = -4.75 + J(0.12), z = -5.05 + J(0.9); put('Огарок', 'bone', x, 0, z, 0.05, h, 0.05, { round: true }); if (i < 2) put('Огонёк', 'ember', x, h, z, 0.025, 0.05, 0.025, { round: true }); }
  put('Наплыв воска', 'bone', -4.75, 0, -5.05, 0.4, 0.015, 1.7, { round: true }); for (const z of [-5.6, -4.5]) vessel(-4.55, 0, z, 0.2, 0.22);

  // ---------- стоянка жильщиков у костра ----------
  for (let i = 0; i < 3; i++) { const a = (i * 120 + 40) * RAD; beam('Нога треноги', 'iron', [4.5 + Math.cos(a) * 0.8, 0, -4.5 + Math.sin(a) * 0.8], [4.5, 1.6, -4.5], 0.04); }
  D.chain(4.5, 1.6, -4.5, 0.6); put('Котелок', 'iron', 4.5, 0.62, -4.5, 0.36, 0.28, 0.36, { round: true });
  if (poses.stump) { const s = poses.stump; put('Миска', 'clay', s[0].x, s[0].y + 0.44, s[0].z, 0.2, 0.05, 0.2, { round: true }); put('Ложка', 'wood', s[0].x + 0.1, s[0].y + 0.44, s[0].z, 0.03, 0.012, 0.16, { ry: 30 }); put('Нож', 'iron', s[1].x, s[1].y + 0.44, s[1].z, 0.03, 0.012, 0.24, { ry: 70 }); }
  { const w = wl(3.2, -7.5, 'S'); S.board(w, { y: 1.3, w: 1.7, h: 0.95, sheets: 5, name: 'Доска жильщиков' }); D.tally(w[0], w[1], w[2] - 2.2, 1.5, 24); D.tally(w[0], w[1], w[2] - 2.2, 1.25, 17); chalk([w[0], w[1], w[2] + 1.4], 1.6); D.drawing(w[0], w[1], w[2] - 2.9, 0.5); }
  S.set('Стойка с оружием', 0.9, -8.25, 1.8, 0.5, 1.7); S.set('Поленница', 8.4, -8.95, 1.8, 0.6, 0.9); put('Колода', 'wood', 9.7, 0, -8.8, 0.5, 0.45, 0.5, { round: true }); beam('Топор', 'iron', [9.7, 0.45, -8.8], [9.9, 0.95, -8.7], 0.05);
  { const w = wl(9, -8.5, 'S', 2.2); tile('Рейка с крюками', 'timber', w[0], w[1], 7.6, 2.1, 1.4, 0.07, 0.05, { sink: 0 }); for (let i = 0; i < 4; i++) tile(pick(['Связка грибов', 'Вяленая рыба', 'Пучок трав']), pick(['fungus', 'leather', 'moss']), w[0], w[1], 7.1 + i * 0.34, 2.05 - R(0.35, 0.5), 0.1, R(0.35, 0.5), 0.08, { sink: -0.04, roll: J(4) }); }

  // ---------- террасы сторожей ----------
  // Северо-восток, 1 м: пост у воды, доска отметок.
  S.set('Стойка с оружием', 13.4, 16.5, 1.6, 0.5, 1.6, { y: 1 }); S.banner(wl(14.5, 15, 'E', 3), 4.6, { w: 1, h: 1.9, m: 'clothBlue', torn: 0.6 });
  { const w = wl(15, 12.6, 'E', 2.5); S.board(w, { y: 2.3, w: 1.3, h: 0.85, sheets: 3, name: 'Доска отметок воды' }); D.tally(w[0], w[1], w[2] - 0.2, 2, 21); }
  S.rug(13.2, 13, 1.6, 2.2, { m: 'clothRed', y: 1.005 }); put('Сигнальный рог', 'bone', 10.6, 2, 12.4, 0.1, 0.1, 0.4, { ry: 40, roll: 20 });
  // Юго-запад, 2,5 м: гнездо дозорного.
  S.banner(wl(-15, -13.5, 'W', 4.5), 5.9, { w: 1, h: 1.9, torn: 0.8 }); put('Спальник дозорного', 'cloth', -16.2, 2.512, -13.2, 0.74, 0.09, 1.9, { ry: 3 });
  put('Ящик болтов', 'wood', -12.2, 2.5, -11.3, 0.5, 0.3, 0.7, { ry: 80 }); for (let i = 0; i < 7; i++) put('Болт', 'timber', -12.2 + J(0.25), 2.8, -11.3 + J(0.2), 0.02, 0.02, 0.4, { ry: 80 + J(10) });
  put('Арбалет на парапете', 'timber', -13.6, 3.5, -10.6, 0.7, 0.06, 0.1, { ry: 8 }); put('Ложа арбалета', 'timber', -13.6, 3.5, -10.6, 0.08, 0.07, 0.75, { ry: 8 });
  for (let i = 0; i < 3; i++) beam('Нога жаровни', 'iron', [-16 + Math.cos(i * 2.1) * 0.3, 2.5, -15.9 + Math.sin(i * 2.1) * 0.3 + 1.6], [-16 + Math.cos(i * 2.1) * 0.14, 3.16, -15.9 + Math.sin(i * 2.1) * 0.14 + 1.6], 0.045); put('Холодная жаровня', 'iron', -16, 3.12, -14.3, 0.6, 0.2, 0.6, { round: true }); put('Зола', 'ash', -16, 3.28, -14.3, 0.48, 0.05, 0.48, { round: true });

  // ---------- стены и столбы: указ о воде, мера на смену, зарубки смен ----------
  D.plaque(...wl(13.4, 3, 'S').slice(0, 2), 13.4, 1.5);
  { const w = wl(-9, 1.5, 'W'); S.board(w, { y: 1.35, w: 1.5, h: 0.9, sheets: 4, name: 'Мера воды на смену' }); for (const [du, y, n] of [[-2, 1.6, 16], [-2, 1.35, 23], [1.4, 1.5, 12]]) D.tally(w[0], w[1], w[2] + du, y, n); }
  for (const [x, z, k] of [[1.45, 6.3, 'N'], [5.45, 0.3, 'N'], [3.45, 12.6, 'S'], [-9.55, 10.2, 'S']]) { const w = wl(x, z, k); D.tally(w[0], w[1], w[2] - 0.3, 1.4, Math.round(R(6, 14))); if (chance(0.6)) chalk(w, 1.75); }
  S.web([-16.7, 3.4, -9.6], [-15.7, 3.2, -9.6]); S.web([16.7, 3.4, 9.6], [15.7, 3.2, 9.6]);
  // Старое железо поста на столбах и обломках стен: скобы с цепями; у колодца по камню тянутся потёки.
  for (const b of S.boxes.filter(b => b.cat === 'wall' && b.o.name !== 'Dressing')) {
    const fs = S.faces(b).filter(f => f.len >= 1 && S.open(f, 0, 2.6)), f = fs.length && pick(fs);
    if (!f) continue;
    const u = J(f.len / 4), y = R(2.3, 3.1), [x, z] = S.fpos(f, u, 0.3);
    S.ftile(f, 'Кованая скоба', 'iron', u, y, 0.26, 0.26, 0.34, { sink: 0.02 }); D.chain(x, y, z, R(0.5, 1.5));
    if (Math.hypot(b.cx, b.cz) < 11) for (let i = 0; i < 3; i++) { const uu = J(f.len / 2 - 0.2), h = R(0.8, 2.2); if (S.open(f, uu, 1)) S.ftile(f, 'Потёк', 'wet', uu, R(0.2, 1.2), R(0.06, 0.14), h, 0.012, { sink: 0, roll: J(2) }); }
  }
};
