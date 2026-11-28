// Вид пропсов из частей для одевания (dress.js): K[имя](S, x, y, z, w, d, h, ry, o). Проп остаётся телом для ходьбы и Unity,
// в атласе вместо него видны части; набор, вернувший true, оставляет тело видимым. Наборы мебели посёлка лежат в detail.js (D.kit).
// Свои наборы комнаты задаются в её dress: S.kit['Имя'] = (S, x, y, z, w, d, h, ry, o) => { … }.
const DEG = Math.PI / 180;
/// Оси пропа: части ставятся от его низа, x вдоль ширины, z вдоль глубины; long: true — x вдоль длинной стороны.
const frame = (S, x, y, z, w, d, ry, long) => {
  const turn = long && d > w, a = (ry + (turn ? 90 : 0)) * DEG, c = Math.cos(a), sn = Math.sin(a), P = (lx, lz) => [x + lx * c + lz * sn, z - lx * sn + lz * c];
  return { P, len: turn ? d : w, wid: turn ? w : d, ry: ry + (turn ? 90 : 0), part: (name, m, lx, ly, lz, pw, ph, pd, o = {}) => { const [px, pz] = P(lx, lz); return S.D.put(name, m, px, y + ly, pz, pw, ph, pd, { ...o, ry: ry + (turn ? 90 : 0) + (o.ry || 0) }); } };
};
const stoneOf = m => ({ rock: 'rock', stone: 'stone', marble: 'marble', brick: 'brick', slab: 'slab' })[m] || 'stone';
const K = module.exports = {};
K.frame = frame;

K['Парапет'] = (S, x, y, z, w, d, h, ry, o) => {
  const { R, J, chance } = S.D, L = frame(S, x, y, z, w, d, ry, true), m = stoneOf(o.m), age = S.T.age, rows = Math.max(1, Math.round((h - 0.12) / 0.42)), rh = (h - 0.12) / rows;
  for (let r = 0; r < rows; r++) for (let u = -L.len / 2 + (r % 2 ? 0 : 0.001); u < L.len / 2 - 0.12;) {
    const bl = Math.min(R(0.5, 0.95) * (r % 2 && u === -L.len / 2 ? 0.6 : 1), L.len / 2 - u);
    if (!(r === rows - 1 && chance(0.1 * age))) L.part('Блок парапета', m, u + bl / 2, r * rh, J(0.012), bl - 0.02, rh - 0.012, L.wid * R(0.92, 1.03), { ry: J(1.2) });
    u += bl;
  }
  for (let u = -L.len / 2; u < L.len / 2 - 0.15;) {
    const bl = Math.min(R(0.6, 1.1), L.len / 2 - u);
    if (!chance(0.14 * age)) L.part('Плита парапета', m, u + bl / 2, h - 0.12, 0, bl - 0.02, 0.12, L.wid + 0.08, { ry: J(1.5), roll: chance(0.15 * age) ? J(4) : 0 });
    else if (S.floor(...L.P(u + bl / 2, L.wid / 2 + 0.4), y) !== undefined) { const [px, pz] = L.P(u + bl / 2, L.wid / 2 + R(0.3, 0.5)); S.D.put('Сбитая плита', m, px, S.floor(px, pz, y), pz, bl * 0.8, 0.11, L.wid, { ry: R(0, 180), roll: J(6) }); }
    u += bl;
  }
  if (chance(S.damp(x, y, z))) L.part('Мох', 'moss', J(L.len / 4), h - 0.01, 0, R(0.4, 0.9), 0.04, L.wid + 0.1, { round: true });
};

K['Обломки'] = (S, x, y, z, w, d, h, ry, o) => {
  const { R, J, chance } = S.D, L = frame(S, x, y, z, w, d, ry), m = o.m && o.m !== 'wood' ? o.m : 'rock', n = Math.max(4, Math.round(w * d * 2.4));
  for (let i = 0; i < n; i++) {
    const lx = J(Math.max(0.05, w / 2 - 0.3)), lz = J(Math.max(0.05, d / 2 - 0.3)), r = Math.min(1, Math.hypot(lx / (w / 2), lz / (d / 2)));
    L.part('Глыба', m, lx, 0, lz, R(0.4, Math.max(0.5, Math.min(1.2, w * 0.6))), h * (1 - 0.5 * r) * R(0.65, 1), R(0.35, Math.max(0.45, Math.min(0.95, d * 0.6))), { ry: R(0, 90), roll: J(9), pitch: J(9) });
  }
  S.D.rubble(x, z, Math.max(w, d) / 2 + 0.35, Math.round(w * d * 5), { m: stoneOf(m), y, pile: 0.3, big: 0.2, name: 'Обломок' });
  if (chance(S.damp(x, y, z))) S.D.patch(x, z, Math.min(w, d) / 2, { y: y + h * 0.55, n: 4 });
};

K['Кости'] = (S, x, y, z, w, d, h, ry) => {
  const { R, J, chance, put } = S.D, L = frame(S, x, y, z, w, d, ry), n = Math.round(w * d * 16) + 6;
  for (let i = 0; i < n; i++) {
    const lx = J(w / 2 - 0.12), lz = J(d / 2 - 0.12), r = Math.min(1, Math.hypot(lx / (w / 2), lz / (d / 2))), ly = h * 0.75 * (1 - r) * R(0, 1), [px, pz] = L.P(lx, lz);
    if (chance(0.14)) put('Череп', 'bone', px, y + ly, pz, 0.2, 0.2, 0.24, { ry: R(0, 360), roll: J(25) });
    else if (chance(0.15)) put('Рёбра', 'bone', px, y + ly, pz, 0.34, 0.2, 0.26, { ry: R(0, 360), roll: J(30), round: true });
    else put('Кость', 'bone', px, y + ly, pz, R(0.25, 0.55), 0.06, 0.07, { ry: R(0, 180), roll: J(30), pitch: J(30) });
  }
};
K['Черепа'] = (S, x, y, z, w, d, h, ry) => {
  const { R, J, put } = S.D, L = frame(S, x, y, z, w, d, ry), s = 0.21;
  for (let t = 0, ly = 0; ly < h - 0.05; t++, ly += s * 0.82) {
    const k = 1 - ly / (h + 0.3) * 0.9;
    for (let lx = -w / 2 * k + s / 2; lx < w / 2 * k; lx += s) for (let lz = -d / 2 * k + s / 2; lz < d / 2 * k; lz += s) { const [px, pz] = L.P(lx + J(0.03), lz + J(0.03)); put('Череп', 'bone', px, y + ly, pz, s - 0.02, s - 0.02, s + 0.02, { ry: ry + R(-40, 40) + (lz > 0 ? 180 : 0), roll: J(8), round: true }); }
  }
};

K['Саркофаг'] = (S, x, y, z, w, d, h, ry, o) => {
  const { R, J, chance } = S.D, L = frame(S, x, y, z, w, d, ry, true), m = stoneOf(o.m), ajar = chance(0.22 * S.T.age), dx = ajar ? J(0.12) : J(0.015), dz = ajar ? R(0.1, 0.24) * (chance(0.5) ? 1 : -1) : J(0.012), turn = ajar ? J(7) : J(0.8);
  L.part('Цоколь саркофага', m, 0, 0, 0, L.len, 0.16, L.wid);
  L.part('Тело саркофага', m, 0, 0.16, 0, L.len - 0.14, h - 0.36, L.wid - 0.14);
  for (const s of [-1, 1]) { L.part('Пояс саркофага', m, 0, h - 0.34, s * (L.wid / 2 - 0.06), L.len - 0.1, 0.1, 0.05); for (let i = 0, n = Math.max(1, Math.round(L.len / 0.8)); i < n; i++) L.part('Филёнка', m, (i + 0.5) / n * (L.len - 0.3) - (L.len - 0.3) / 2, 0.3, s * (L.wid / 2 - 0.065), (L.len - 0.3) / n - 0.12, h - 0.75, 0.03); }
  L.part('Крышка саркофага', m, dx, h - 0.2, dz, L.len - 0.03, 0.13, L.wid - 0.03, { ry: turn });
  L.part('Гребень крышки', m, dx, h - 0.07, dz, L.len - 0.5, 0.07, L.wid * 0.42, { ry: turn });
  if (chance(0.3 * S.T.age)) L.part('Скол', 'soot', pickSide(S) * (L.len / 2 - 0.15), h - 0.19, pickSide(S) * (L.wid / 2 - 0.005), 0.26, 0.1, 0.012);
  if (chance(S.damp(x, y, z))) L.part('Мох', 'moss', J(L.len / 3), h, J(0.1), R(0.3, 0.7), 0.03, L.wid * 0.5, { round: true });
};
const pickSide = S => S.D.chance(0.5) ? 1 : -1;

K['Гроб'] = (S, x, y, z, w, d, h, ry) => {
  const { R, J, chance } = S.D, L = frame(S, x, y, z, w, d, ry, true), ajar = chance(0.25);
  L.part('Дно гроба', 'wood', 0, 0, 0, L.len, 0.05, L.wid);
  for (const s of [-1, 1]) { L.part('Борт гроба', 'wood', 0, 0.05, s * (L.wid / 2 - 0.02), L.len, h - 0.1, 0.04, { roll: J(0.6) }); L.part('Торец гроба', 'wood', s * (L.len / 2 - 0.02), 0.05, 0, 0.04, h - 0.1, L.wid - 0.08); }
  for (let i = 0, n = 3; i < n; i++) L.part('Доска крышки', 'wood', ajar ? J(0.04) : 0, h - 0.05, (i + 0.5) / n * L.wid - L.wid / 2 + (ajar ? 0.14 : 0), L.len + J(0.03), 0.04, L.wid / n - 0.012, { ry: ajar ? 5 : J(0.6) });
  for (const t of [-0.3, 0.3]) L.part('Обруч гроба', 'iron', t * L.len, 0.02, 0, 0.05, h - 0.04, L.wid + 0.03);
};

K['Алтарь'] = (S, x, y, z, w, d, h, ry, o) => {
  const { R, J, put } = S.D, L = frame(S, x, y, z, w, d, ry, true), m = stoneOf(o.m);
  L.part('Ступень алтаря', m, 0, 0, 0, L.len + 0.3, 0.14, L.wid + 0.3);
  L.part('Тело алтаря', m, 0, 0.14, 0, L.len - 0.12, h - 0.28, L.wid - 0.12);
  L.part('Плита алтаря', m, 0, h - 0.14, 0, L.len + 0.08, 0.14, L.wid + 0.08, { ry: J(0.6) });
  L.part('Покров', 'clothRed', 0, h, 0, L.len * 0.42, 0.015, L.wid + 0.1);
  for (const s of [-1, 1]) L.part('Край покрова', 'clothRed', 0, h - 0.42, s * (L.wid / 2 + 0.05), L.len * 0.42, 0.43, 0.015, { pitch: s * 2 });
  for (const s of [-1, 1]) { const [cx, cz] = L.P(s * (L.len / 2 - 0.2), J(0.05)), ch = R(0.12, 0.26); put('Подсвечник', 'bronze', cx, y + h, cz, 0.12, 0.05, 0.12, { round: true }); put('Свеча', 'bone', cx, y + h + 0.05, cz, 0.05, ch, 0.05, { round: true }); }
  const [bx, bz] = L.P(J(0.1), J(0.04)); put('Чаша', 'bronze', bx, y + h + 0.015, bz, 0.26, 0.1, 0.26, { round: true });
};

K['Стойка с оружием'] = (S, x, y, z, w, d, h, ry) => {
  const { R, J, chance } = S.D, L = frame(S, x, y, z, w, d, ry, true);
  for (const s of [-1, 1]) { L.part('Стойка', 'timber', s * (L.len / 2 - 0.05), 0, 0, 0.09, h, 0.09, { roll: J(0.8) }); L.part('Лапа стойки', 'timber', s * (L.len / 2 - 0.05), 0, 0, 0.1, 0.08, Math.max(L.wid, 0.5)); }
  for (const ly of [0.35, h - 0.25]) L.part('Перекладина', 'timber', 0, ly, 0, L.len, 0.08, 0.06);
  for (let lx = -L.len / 2 + 0.22; lx < L.len / 2 - 0.15; lx += R(0.22, 0.36)) {
    const kind = S.D.rnd();
    if (kind < 0.2) continue;                                                                       // пустое гнездо
    if (kind < 0.55) { L.part('Древко', 'timber', lx, 0.03, 0.1, 0.04, h + R(0.1, 0.4), 0.04, { pitch: -5, roll: J(2) }); L.part('Наконечник', 'iron', lx, h + 0.2, 0.07, 0.06, 0.24, 0.03, { pitch: -5 }); }
    else if (kind < 0.85) { L.part('Клинок', 'iron', lx, 0.42, 0.08, 0.06, R(0.75, 0.95), 0.02, { pitch: -4, roll: J(2) }); L.part('Гарда', 'iron', lx, 0.36, 0.08, 0.2, 0.04, 0.04, { pitch: -4 }); L.part('Рукоять', 'leather', lx, 0.16, 0.09, 0.04, 0.2, 0.04, { pitch: -4 }); }
    else { L.part('Топорище', 'timber', lx, 0.03, 0.1, 0.04, h * 0.7, 0.04, { pitch: -5 }); L.part('Лезвие топора', 'iron', lx + 0.09, h * 0.58, 0.07, 0.2, 0.18, 0.025, { pitch: -5 }); }
  }
  if (chance(0.4)) { const [px, pz] = L.P(J(L.len / 3), L.wid / 2 + 0.25), fy = S.floor(px, pz, y); if (fy !== undefined) S.D.put('Упавший клинок', 'iron', px, fy, pz, 0.06, 0.02, 0.9, { ry: R(0, 180) }); }
};

K['Стеллаж'] = (S, ...a) => S.D.kit['Полки с припасами'](...a);

K['Очаг'] = (S, x, y, z, w, d, h, ry, o) => {
  const { R, J, put } = S.D, L = frame(S, x, y, z, w, d, ry), m = stoneOf(o.m);
  for (const s of [-1, 1]) { for (let u = -w / 2; u < w / 2 - 0.1;) { const bl = Math.min(R(0.3, 0.55), w / 2 - u); L.part('Камень очага', m, u + bl / 2, 0, s * (d / 2 - 0.12), bl - 0.02, h * R(0.85, 1), 0.24, { ry: J(3) }); u += bl; } for (let v = -d / 2 + 0.24; v < d / 2 - 0.3;) { const bl = Math.min(R(0.3, 0.55), d / 2 - 0.24 - v); L.part('Камень очага', m, s * (w / 2 - 0.12), 0, v + bl / 2, 0.24, h * R(0.85, 1), bl - 0.02, { ry: J(3) }); v += bl; } }
  L.part('Зола', 'ash', 0, 0, 0, w - 0.4, h * 0.45, d - 0.4); L.part('Угли', 'ember', 0, h * 0.45, 0, (w - 0.5) * 0.8, 0.06, (d - 0.5) * 0.8, { round: true });
  for (let i = 0; i < 3; i++) { const [ax, az] = L.P(J(w / 4), J(d / 4)), [bx, bz] = L.P(J(w / 4), J(d / 4)); S.D.beam('Полено', 'soot', [ax, y + h * 0.5, az], [bx, y + h * R(0.6, 0.9), bz], 0.1); }
  S.D.patch(x, z, Math.max(w, d) / 2 + 0.4, { m: 'soot', name: 'Сажа', y, n: 5 });
};

K['Котёл'] = (S, x, y, z, w, d, h) => {
  const { put, beam } = S.D, dia = Math.min(w, d);
  for (let i = 0; i < 3; i++) { const a = (i * 120 + 15) * DEG; beam('Нога котла', 'iron', [x + Math.cos(a) * dia * 0.42, y, z + Math.sin(a) * dia * 0.42], [x + Math.cos(a) * dia * 0.3, y + h * 0.3, z + Math.sin(a) * dia * 0.3], 0.05); }
  put('Котёл', 'iron', x, y + h * 0.22, z, dia * 0.9, h * 0.7, dia * 0.9, { round: true }); put('Обод котла', 'iron', x, y + h * 0.9, z, dia, h * 0.1, dia, { round: true });
  put('Варево', S.T.brew || 'soot', x, y + h * 0.93, z, dia * 0.82, h * 0.08, dia * 0.82, { round: true });
};

K['Чан'] = (S, x, y, z, w, d, h, ry) => {
  const { J, put } = S.D, dia = Math.min(w, d), n = Math.max(10, Math.round(dia * 9));
  for (let i = 0; i < n; i++) { const a = i / n * 2 * Math.PI; put('Клёпка чана', 'wood', x + Math.cos(a) * (dia / 2 - 0.03), y, z + Math.sin(a) * (dia / 2 - 0.03), dia * Math.PI / n - 0.01, h + J(0.02), 0.05, { ry: 90 - a / DEG }); }
  for (const t of [0.15, 0.55, 0.9]) put('Обруч чана', 'iron', x, y + h * t, z, dia + 0.03, 0.05, dia + 0.03, { round: true });
  put('Содержимое чана', S.T.brew || 'soot', x, y + h * 0.82, z, dia - 0.12, 0.03, dia - 0.12, { round: true });
};

K['Ящик'] = (S, x, y, z, w, d, h, ry) => {
  const L = frame(S, x, y, z, w, d, ry);
  L.part('Ящик', 'wood', 0, 0, 0, w, h - 0.02, d);
  for (const t of [0.1, 0.75]) L.part('Обвязка ящика', 'timber', 0, h * t, 0, w + 0.035, 0.07, d + 0.035);
};

K['Гриб'] = (S, x, y, z, w, d, h) => {
  const { R, J, put } = S.D, dia = Math.min(w, d);
  put('Ножка гриба', 'fungus', x, y, z, dia * 0.32, h * 0.82, dia * 0.32, { round: true, roll: J(4), pitch: J(4) });
  put('Шляпка гриба', 'fungus', x, y + h * 0.72, z, dia * 1.15, h * 0.2, dia * 1.15, { round: true, roll: J(5) }); put('Маковка шляпки', 'fungus', x, y + h * 0.9, z, dia * 0.7, h * 0.1, dia * 0.7, { round: true });
  S.D.fungus(x, z, 7, { y, spread: dia * 0.7 });
};

K['Золото'] = (S, x, y, z, w, d, h) => {
  const { R, J, put } = S.D;
  for (let t = 0, n = 4; t < n; t++) put('Груда монет', 'gold', x + J(0.04), y + h * t / n * 0.8, z + J(0.04), w * (1 - t / n * 0.75), h / n, d * (1 - t / n * 0.75), { round: true });
  for (let i = 0; i < 9; i++) put('Монета', 'gold', x + J(w / 2 + 0.25), y, z + J(d / 2 + 0.25), 0.06, 0.012, 0.06, { round: true });
  for (let i = 0; i < 3; i++) put('Слиток', 'gold', x + J(w / 3), y + h * 0.4, z + J(d / 3), 0.22, 0.07, 0.1, { ry: R(0, 180), roll: J(14) });
};

// Упавшая колонна: барабаны лежат вдоль длинной стороны, разошлись и повернулись.
K['Упавшая колонна'] = (S, x, y, z, w, d, h, ry, o) => {
  const { R, J, put } = S.D, L = frame(S, x, y, z, w, d, ry, true), m = stoneOf(o.m), dia = Math.min(L.wid, h);
  for (let u = -L.len / 2; u < L.len / 2 - 0.3;) {
    const bl = Math.min(R(0.8, 1.5), L.len / 2 - u), [px, pz] = L.P(u + 0.03, J(0.08));
    put('Барабан колонны', m, px, y + dia / 2, pz, dia, bl - 0.08, dia, { round: true, pitch: 90, ry: L.ry + 90 + J(5) });
    u += bl;
  }
  S.D.rubble(x, z, Math.max(w, d) / 2, Math.round(L.len * 4), { m, y, pile: 0.2, big: 0.2, name: 'Обломок' });
};
K['Барабан колонны'] = (S, x, y, z, w, d, h, ry, o) => { S.D.put('Барабан колонны', stoneOf(o.m), x, y, z, w, h, d, { round: true, ry: ry + S.D.J(4), roll: S.D.J(2) }); S.D.rubble(x, z, Math.max(w, d) / 2 + 0.3, 6, { m: stoneOf(o.m), y, pile: 0.2, name: 'Обломок' }); };

K['Статуя'] = (S, x, y, z, w, d, h, ry, o) => {
  const L = frame(S, x, y, z, w, d, ry), m = stoneOf(o.m), s = Math.min(w, d);
  L.part('Постамент', m, 0, 0, 0, w, h * 0.22, d); L.part('Карниз постамента', m, 0, h * 0.22, 0, w + 0.08, 0.08, d + 0.08);
  for (const sx of [-1, 1]) L.part('Нога статуи', m, sx * s * 0.14, h * 0.24, 0, s * 0.2, h * 0.3, s * 0.24);
  L.part('Торс статуи', m, 0, h * 0.54, 0, s * 0.55, h * 0.28, s * 0.34); L.part('Плечи статуи', m, 0, h * 0.76, 0, s * 0.72, h * 0.07, s * 0.36);
  L.part('Голова статуи', m, 0, h * 0.84, 0, s * 0.24, h * 0.14, s * 0.26, { round: true });
};

K['Колодец'] = (S, x, y, z, w, d, h) => {
  const { R, J, put } = S.D, r = Math.min(w, d) / 2, n = Math.max(10, Math.round(r * 11));
  for (let i = 0; i < n; i++) { const a = i / n * 2 * Math.PI; put('Камень колодца', 'slab', x + Math.cos(a) * (r - 0.12), y, z + Math.sin(a) * (r - 0.12), 2 * Math.PI * r / n * 0.95, h * R(0.9, 1.05), R(0.22, 0.3), { ry: 90 - a / DEG + J(5), roll: J(3) }); }
  put('Вода в колодце', 'soot', x, y + h * 0.3, z, 2 * r - 0.5, 0.02, 2 * r - 0.5, { round: true });
};

K['Мешки'] = (S, ...a) => S.D.kit['Мешки'](...a);

K['Манекен'] = (S, x, y, z, w, d, h, ry) => {
  const L = frame(S, x, y, z, w, d, ry);
  L.part('Крестовина', 'timber', 0, 0, 0, 0.7, 0.08, 0.1); L.part('Крестовина', 'timber', 0, 0, 0, 0.1, 0.08, 0.7);
  L.part('Столб манекена', 'timber', 0, 0, 0, 0.1, h * 0.8, 0.1); L.part('Плечи манекена', 'timber', 0, h * 0.7, 0, Math.max(w, 0.9), 0.08, 0.08);
  L.part('Соломенный торс', 'straw', 0, h * 0.4, 0, 0.42, h * 0.36, 0.3, { round: true }); L.part('Голова манекена', 'straw', 0, h * 0.8, 0, 0.24, 0.26, 0.24, { round: true });
  for (let i = 0; i < 3; i++) L.part('Зарубка', 'soot', S.D.J(0.12), h * (0.45 + i * 0.1), 0.151, 0.2, 0.02, 0.01, { roll: S.D.J(40) });
};
