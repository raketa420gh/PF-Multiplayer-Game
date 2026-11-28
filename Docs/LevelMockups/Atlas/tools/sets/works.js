// Общие мелочи сцен производств (PumpWorks, Mint, Aqueduct, FloodedCistern, ColossusWorks): прислонённый инструмент, крюк на цепи, копоть,
// метки и лёжка жильщиков, приношение Спящих, останки, кучи: const G = require('../sets/works')(S).
module.exports = S => {
  const { D } = S, { put, tile, beam, R, J, pick, chance } = D, DEG = Math.PI / 180, G = {};
  /// Местные оси: x вдоль, z вперёд (у стены — наружу от неё).
  G.frame = (x, y, z, ry) => { const c = Math.cos(ry * DEG), s = Math.sin(ry * DEG); return (name, m, lx, ly, lz, w, h, d, o = {}) => put(name, m, x + lx * c + lz * s, y + ly, z - lx * s + lz * c, w, h, d, { ...o, ry: ry + (o.ry || 0) }); };
  /// Грань стены свободна и за ней тело: настенное не повиснет в проёме.
  G.backed = (k, p, u, y) => { const [ax, az] = D.at(k, p, u, 0.12), [bx, bz] = D.at(k, p, u, -0.15); return S.air(ax, y, az) && !!S.solid(bx, y, bz); };
  /// Инструмент, прислонённый к стене (w — результат S.wall, y — пол): head [имя, материал, w, h, d] наверху или, с low, у пола; wide — ширина доски вдоль стены.
  G.lean = (w, y, name, len, { m = 'timber', th = 0.04, wide = th, out = 0.4, du = 0, head, low } = {}) => {
    if (!w) return;
    const [k, p, u] = w, a = D.at(k, p, u + du, out), b = D.at(k, p, u + du + (wide > th ? 0 : J(0.08)), 0.03), top = y + Math.sqrt(len * len - out * out), ns = k === 'N' || k === 'S';
    beam(name, m, [a[0], y, a[1]], [b[0], top, b[1]], ns ? wide : th, ns ? th : wide);
    if (head) low ? put(head[0], head[1], a[0], y, a[1], head[2], head[3], head[4], { ry: D.FACE[k].ry + J(12) }) : tile(head[0], head[1], k, p, u + du, top - head[3] * 0.4, head[2], head[3], head[4], { sink: -0.01, roll: J(6) });
  };
  /// Кронштейн в стене, цепь и крюк.
  G.hook = (w, y, len = 0.8, out = 0.5) => { if (!w) return; const [k, p, u] = w, [x, z] = D.at(k, p, u, out); tile('Кронштейн', 'iron', k, p, u, y, 0.06, 0.06, out + 0.05, { sink: 0.02 }); D.chain(x, y, z, len); put('Крюк', 'iron', x, y - len - 0.14, z, 0.07, 0.14, 0.05); };
  /// Доска с инструментом на стене на уровне глаз: y — пол.
  G.rack = (w, names, n = 5, wd = 1.6, y = 0, m = 'iron') => { if (!w) return; const [k, p, u] = w; tile('Доска с инструментом', 'wood', k, p, u, y + 1.72, wd, 0.2, 0.04, { sink: 0 }); for (let i = 0; i < n; i++) { const len = R(0.4, 0.75); tile(pick(names), m, k, p, u - wd / 2 + (i + 0.5) * wd / n, y + 1.76 - len, R(0.05, 0.1), len, 0.04, { sink: -0.04, roll: J(4) }); } };
  /// Язык копоти по стене от y вверх.
  G.soot = (w, y, size = 1, n = 4) => { if (!w) return; const [k, p, u] = w; for (let i = 0; i < n; i++) D.blot('Копоть', 'soot', k, p, u + J(0.25 * size), y + i * 0.3 * size, R(0.5, 0.9) * size, R(0.4, 0.7) * size, 0.012, -99); };
  /// Потёк по стене от y вниз: сырость, ржавчина, известь.
  G.streak = (w, y, len = 1.4, m = 'wet', name = 'Потёк') => { if (!w) return; const [k, p, u] = w; for (let i = 0, n = Math.round(R(2, 4)); i < n; i++) { const l = len * R(0.5, 1); tile(name, m, k, p, u + J(0.25), y - l, R(0.03, 0.07), l, 0.012, { sink: 0, roll: J(2) }); } };
  /// Меловая метка жильщиков: стрелка.
  G.chalk = (w, y = 1.5, dir = 1) => { if (!w) return; const [k, p, u] = w; tile('Меловая стрелка', 'paper', k, p, u, y, 0.4, 0.025, 0.012, { sink: 0 }); for (const s of [-1, 1]) tile('Меловая стрелка', 'paper', k, p, u + dir * 0.13, y + s * 0.05, 0.17, 0.025, 0.012, { sink: 0, roll: -s * dir * 35 }); };
  /// Лёжка жильщиков: спальник, котомка, прогоревший костёр, котелок. Костёр сбоку по местной x.
  G.camp = (x, z, y = 0, ry = 0) => {
    const p = G.frame(x, y, z, ry);
    p('Спальник', 'cloth', 0, 0.01, 0, 0.7, 0.08, 1.8, { ry: J(6) }); p('Скатка', 'clothBlue', 0, 0.09, 0.7, 0.5, 0.14, 0.24, { ry: J(10) }); p('Котомка', 'leather', -0.6, 0, 0.5, 0.36, 0.3, 0.3, { ry: R(0, 60) });
    for (let i = 0; i < 7; i++) { const a = i / 7 * 2 * Math.PI; p('Камень кострища', 'rock', 1.15 + Math.cos(a) * 0.32, 0, 0.1 + Math.sin(a) * 0.32, R(0.16, 0.24), R(0.1, 0.16), R(0.14, 0.2), { ry: R(0, 180) }); }
    p('Зола костра', 'ash', 1.15, 0, 0.1, 0.5, 0.04, 0.5, { round: true }); for (let i = 0; i < 3; i++) p('Головешка', 'soot', 1.15 + J(0.12), 0.04, 0.1 + J(0.12), 0.07, 0.06, R(0.25, 0.4), { ry: R(0, 180) });
    p('Котелок', 'iron', 1, 0, 0.66, 0.22, 0.16, 0.22, { round: true }); p('Огарок', 'bone', -0.45, 0, -0.2, 0.05, 0.07, 0.05, { round: true }); p('Фляга', 'leather', 0.45, 0, 0.55, 0.12, 0.2, 0.08, { ry: 30 });
  };
  /// Приношение Спящих: руна на полу, огарки полукругом, кости.
  G.offering = (x, z, y = 0, n = 6) => {
    put('Руна Спящих', 'rune', x, y + 0.004, z, 0.16, 0.012, 0.16, { round: true }); for (const a of [20, 80, 140]) put('Руна Спящих', 'rune', x, y + 0.005, z, 0.62, 0.012, 0.04, { ry: a });
    for (let i = 0; i < n; i++) { const a = (i / n * 1.4 - 0.2) * Math.PI, h = R(0.06, 0.2), cx = x + Math.cos(a) * 0.48, cz = z + Math.sin(a) * 0.48; put('Свеча Спящих', 'bone', cx, y, cz, 0.05, h, 0.05, { round: true }); put('Наплыв воска', 'bone', cx, y, cz, 0.14, 0.012, 0.12, { round: true }); }
    D.bones(x, z, 4, { y, spread: 0.22 });
  };
  /// Останки: лежат головой по ry.
  G.body = (x, z, y = 0, ry = 0, m = 'bone') => { const p = G.frame(x, y, z, ry); p('Череп', m, J(0.05), 0, 0.82, 0.2, 0.2, 0.24, { ry: J(40), round: true }); p('Рёбра', m, 0, 0, 0.42, 0.36, 0.16, 0.42, { round: true }); p('Таз', m, 0, 0, 0.1, 0.3, 0.1, 0.2); for (const sx of [-1, 1]) { p('Кость руки', m, sx * 0.3, 0, 0.42, 0.06, 0.05, 0.62, { ry: sx * R(5, 40) }); p('Кость ноги', m, sx * 0.13, 0, -0.42, 0.07, 0.06, 0.86, { ry: sx * R(0, 16) }); } };
  /// Бухта цепи или троса.
  G.coil = (x, y, z, m = 'iron', dia = 0.5) => { for (let i = 0; i < 3; i++) put(m === 'rope' ? 'Бухта троса' : 'Бухта цепи', m, x + J(0.03), y + i * 0.05, z + J(0.03), dia - i * 0.06, 0.06, dia - i * 0.06, { round: true }); };
  G.bucket = (x, y, z) => { put('Ведро', 'wood', x, y, z, 0.3, 0.3, 0.3, { round: true }); put('Обруч ведра', 'iron', x, y + 0.2, z, 0.32, 0.03, 0.32, { round: true }); };
  /// Куча сыпучего у стены: пологий холм, комья на нём и россыпь вокруг.
  G.heap = (x, z, r, m, name, { y = 0, h = 0.5, n = 14 } = {}) => {
    put(name, m, x, y, z, 2 * r, h * 0.35, 2 * r * R(0.8, 0.95), { round: true, ry: R(0, 180) }); put(name, m, x + J(r * 0.2), y + h * 0.3, z + J(r * 0.2), r * 1.15, h * 0.35, r * R(0.9, 1.1), { round: true, ry: R(0, 180) });
    for (let i = 0; i < n; i++) { const a = R(0, 6.28), t = Math.sqrt(D.rnd()), s = R(0.12, 0.26); put(name, m, x + Math.cos(a) * r * t, y + h * (1 - t) * 0.8, z + Math.sin(a) * r * t, s, s * R(0.6, 0.9), s * R(0.7, 1.2), { ry: R(0, 180), roll: J(25), pitch: J(25) }); }
    D.rubble(x, z, r * 1.3, Math.round(n / 2), { m, name, y, pile: 0.1, big: 0 });
  };
  /// Подсказки у ловушек: гарь и обгоревшие кости у огня, старая кровь и кости у шипов, борозды под маятником.
  G.traps = () => {
    for (const o of S.room.items.filter(o => o.cat === 'trap')) {
      const [x, y, z] = o.p, flat = (px, pz) => { const f = S.floor(px, pz, y); return f !== undefined && Math.abs(f - y) < 0.05; };
      if (o.name === 'Vent') { D.patch(x, z, 0.9, { m: 'soot', name: 'Гарь', y, n: 7 }); if (chance(0.6) && flat(x, z)) D.bones(x + J(0.5), z + J(0.5), 2, { y, spread: 0.3 }); }
      if (o.name === 'Spike' && flat(x, z)) { S.pool(x + J(0.3), z + J(0.3), R(0.9, 1.3), R(0.6, 0.9), { m: 'blood', y, name: 'Старая кровь' }); D.bones(x + J(0.5), z + J(0.4), 3, { y, spread: 0.35 }); }
      if (o.name === 'Blade') { for (let i = 0; i < 4; i++) put('Борозда маятника', 'soot', x + J(0.1), y + 0.004, z + J(0.1), 1.5, 0.012, 0.03, { ry: (o.ry || 0) + 90 + J(4) }); put('Срезанная кость', 'bone', x + J(0.6), y, z + J(0.6), 0.3, 0.06, 0.07, { ry: R(0, 180) }); S.pool(x + J(0.4), z + J(0.4), 0.8, 0.5, { m: 'blood', y, name: 'Старая кровь' }); }
    }
  };
  return G;
};
