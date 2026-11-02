// Проверка макета: растр 0,2 м, поверхности для тела с запасом до стен 0,6 и высотой 1,8, подъём за шаг 0,3 м. Два прохода:
// закрытый (секреты заперты, лифт стоит) и открытый (плиты сдвинуты, лифт соединяет остановки).
const { DOOR_OFFSET, MIN_Y, MAX_Y } = require('./roomkit');
const STEP = 0.2, EXT = 17.4, N = Math.round(2 * EXT / STEP), R_CELLS = 3, BODY = 1.8, CLIMB = 0.31, MIN_FLIGHT_W = 1.6;
const toI = v => Math.floor((v + EXT) / STEP), toV = i => -EXT + (i + 0.5) * STEP;
const idx = (i, j) => i * N + j;
const footprint = o => {
  const yaw = (o.ry || 0) * Math.PI / 180, c = Math.abs(Math.cos(yaw)), s = Math.abs(Math.sin(yaw));
  const w = o.s[0] * c + o.s[2] * s, d = o.s[0] * s + o.s[2] * c;
  return [o.p[0] - w / 2, o.p[2] - d / 2, o.p[0] + w / 2, o.p[2] + d / 2];
};
const SOLID = new Set(['wall', 'column', 'floor', 'platform', 'pit', 'ceiling', 'upper', 'bars']);

function buildColumns(items, isOpen) {
  const solids = Array.from({ length: N * N }, () => []), tops = Array.from({ length: N * N }, () => []), ramps = new Set();
  const each = (o, fn) => {
    const [a, b, c, d] = footprint(o);
    for (let i = Math.max(0, toI(a + 1e-3)); i <= Math.min(N - 1, toI(c - 1e-3)); i++)
      for (let j = Math.max(0, toI(b + 1e-3)); j <= Math.min(N - 1, toI(d - 1e-3)); j++)
        if (toV(i) >= a && toV(i) <= c && toV(j) >= b && toV(j) <= d) fn(i, j);
  };
  for (const o of items) {
    if (!o.s) continue;
    const y0 = o.p[1], y1 = y0 + o.s[1];
    if (o.cat === 'stairs') {
      const yaw = (o.ry || 0) * Math.PI / 180, sn = Math.sin(yaw), cs = Math.cos(yaw);
      each(o, (i, j) => {
        const t = ((toV(i) - o.p[0]) * sn + (toV(j) - o.p[2]) * cs + o.s[2] / 2) / o.s[2], y = y0 + o.s[1] * Math.max(0, Math.min(1, t));
        tops[idx(i, j)].push(y); ramps.add(`${idx(i, j)}|${Math.round(y * 100)}`); solids[idx(i, j)].push([y0 - 0.6, y - 0.02]);
      });
    } else if (SOLID.has(o.cat)) {
      const isWalk = o.cat === 'floor' || o.cat === 'platform';
      each(o, (i, j) => { solids[idx(i, j)].push([y0, y1 - (isWalk ? 0.001 : 0)]); if (isWalk) tops[idx(i, j)].push(y1); });
    } else if (o.cat === 'prop' && o.s[1] > 0.3) each(o, (i, j) => solids[idx(i, j)].push([y0, y1]));
    else if (o.cat === 'trap' && o.name === 'SecretDoor' && !isOpen) each(o, (i, j) => solids[idx(i, j)].push([y0, y1]));
    else if (o.cat === 'trap' && o.name === 'Lift') {
      for (const y of [y0, y0 + (isOpen ? o.travel : 0)]) each(o, (i, j) => tops[idx(i, j)].push(y));
      if (!isOpen) each(o, (i, j) => { for (let k = 0; k < tops[idx(i, j)].length; k++) if (Math.abs(tops[idx(i, j)][k] - y0) < 1e-6) tops[idx(i, j)][k] = y0; });
    }
  }
  return { solids, tops, ramps };
}

function nodes({ solids, tops, ramps }) {
  // Высота, на которой тело умещается в столбце; затем эрозия по радиусу.
  // Настоящая поверхность марша идёт до 0,35 м выше линии «низ → верх» (призма в CatacombKit).
  const free = (c, y, body, margin) => !solids[c].some(([a, b]) => b > y + margin && a < y + body);
  const ok = new Map();
  for (let i = R_CELLS; i < N - R_CELLS; i++) for (let j = R_CELLS; j < N - R_CELLS; j++) {
    for (const y of tops[idx(i, j)]) {
      let good = true;
      const body = BODY + (ramps.has(`${idx(i, j)}|${Math.round(y * 100)}`) ? 0.4 : 0);
      for (let a = -R_CELLS; a <= R_CELLS && good; a++) for (let b = -R_CELLS; b <= R_CELLS && good; b++)
        if (a * a + b * b <= R_CELLS * R_CELLS + 1) good = free(idx(i + a, j + b), y, body, a || b ? 0.55 : 0.31);
      if (good) { const c = idx(i, j); if (!ok.has(c)) ok.set(c, []); ok.get(c).push(y); }
    }
  }
  return ok;
}

function flood(ok, seeds, extra = []) {
  const seen = new Set(), q = [...seeds], key = (c, y) => `${c}|${Math.round(y * 100)}`;
  seeds.forEach(([c, y]) => seen.add(key(c, y)));
  while (q.length) {
    const [c, y] = q.pop();
    for (const e of extra) if (e.from.has(c) && Math.abs(e.y0 - y) < 0.05) for (const n of e.to) if (!seen.has(key(n[0], n[1]))) { seen.add(key(n[0], n[1])); q.push(n); }
    for (const d of [N, -N, 1, -1]) {
      const n = c + d; const list = ok.get(n); if (!list) continue;
      for (const y2 of list) if (Math.abs(y2 - y) <= CLIMB && !seen.has(key(n, y2))) { seen.add(key(n, y2)); q.push([n, y2]); }
    }
  }
  return seen;
}

const near = (ok, seen, x, y, z, reach, up = 1.8) => {
  const ci = toI(x), cj = toI(z), r = Math.ceil(reach / STEP);
  for (let a = -r; a <= r; a++) for (let b = -r; b <= r; b++) {
    const c = idx(ci + a, cj + b); const list = ok.get(c); if (!list) continue;
    for (const yy of list) if (Math.abs(yy - y) <= up && seen.has(`${c}|${Math.round(yy * 100)}`)) return true;
  }
  return false;
};

function validate(room) {
  const items = room.items, errors = [], warnings = [];
  for (const o of items) {
    if (o.p[1] < MIN_Y - 0.61 || o.p[1] + (o.s ? o.s[1] : 0) > MAX_Y + 0.61) errors.push(`${o.id} ${o.name}: y вне ${MIN_Y}…${MAX_Y}`);
    if (o.s && o.cat !== 'upper' && o.cat !== 'ceiling') { const f = footprint(o); if (Math.min(...f) < -17.11 || Math.max(...f) > 17.11) errors.push(`${o.id} ${o.name}: вне клетки`); }
    if (o.cat === 'stairs') {
      if (o.s[0] < MIN_FLIGHT_W) errors.push(`${o.id}: марш уже ${MIN_FLIGHT_W} м`);
      if (o.s[1] / o.s[2] > 0.85) errors.push(`${o.id}: марш круче 40° (${o.s[1]}/${o.s[2]})`);
      if ((o.ry || 0) % 90) errors.push(`${o.id}: марш не по осям`);
    }
    if (o.cat === 'prop' && o.s[1] > 0.3 && o.s[1] < 0.3) warnings.push(o.id);
  }
  const closedCols = buildColumns(items, false), openCols = buildColumns(items, true);
  const closed = nodes(closedCols), open = nodes(openCols);
  const doors = [];
  for (const s of [-1, 1]) {
    doors.push([s * DOOR_OFFSET, 16.4], [16.4, s * DOOR_OFFSET]);
    if (!room.corner) doors.push([s * DOOR_OFFSET, -16.4], [-16.4, s * DOOR_OFFSET]);
  }
  const seedOf = (ok, x, z) => { const c = idx(toI(x), toI(z)), l = ok.get(c); return l && l.some(y => Math.abs(y) < 0.3) ? [[c, l.find(y => Math.abs(y) < 0.3)]] : []; };
  const seeds = doors.flatMap(([x, z]) => seedOf(closed, x, z));
  if (seeds.length < doors.length) errors.push(`свободных проёмов ${seeds.length} из ${doors.length}`);
  const seen = flood(closed, seeds.slice(0, 1));
  doors.forEach(([x, z]) => { const s = seedOf(closed, x, z)[0]; if (s && !seen.has(`${s[0]}|${Math.round(s[1] * 100)}`)) errors.push(`проём (${x}, ${z}) не связан с остальными`); });

  const lifts = items.filter(o => o.name === 'Lift'), extra = [];
  for (const l of lifts) {
    const cells = y => { const out = []; const [a, b, c, d] = footprint(l); for (let i = toI(a); i <= toI(c); i++) for (let j = toI(b); j <= toI(d); j++) { const list = open.get(idx(i, j)); if (list && list.some(v => Math.abs(v - y) < 0.05)) out.push([idx(i, j), y]); } return out; };
    const top = cells(l.p[1]), bottom = cells(l.p[1] + l.travel);
    if (!top.length || !bottom.length) errors.push(`${l.id}: кабине негде стоять (верх ${top.length}, низ ${bottom.length})`);
    extra.push({ from: new Set(top.map(n => n[0])), y0: l.p[1], to: bottom }, { from: new Set(bottom.map(n => n[0])), y0: l.p[1] + l.travel, to: top });
  }
  const openSeeds = doors.flatMap(([x, z]) => seedOf(open, x, z)).slice(0, 1);
  const seenOpen = flood(open, openSeeds, extra);
  const secretDoors = items.filter(o => o.name === 'SecretDoor').length;
  if ((secretDoors || lifts.length) && seenOpen.size <= seen.size + 20) errors.push(`секрет ничего не открывает: узлов закрыто ${seen.size}, открыто ${seenOpen.size}`);
  const closedY = new Set([...seen].map(k => +k.split('|')[1] / 100));
  const unreachable = [];
  let secretOnly = 0;
  for (const o of items) {
    if (!['chest', 'monster', 'start'].includes(o.cat) && o.name !== 'SecretLever' && o.name !== 'FloorPlate') continue;
    const reach = o.name === 'SecretLever' ? 1.5 : 1.3;
    const inClosed = near(closed, seen, o.p[0], o.p[1], o.p[2], reach), inOpen = near(open, seenOpen, o.p[0], o.p[1], o.p[2], reach);
    if (!inOpen) unreachable.push(`${o.id} ${o.name} (${o.p})`);
    else if (!inClosed) { secretOnly++; if ((o.name === 'SecretLever' && o.action !== 'lift') || o.name === 'FloorPlate') errors.push(`${o.id}: ${o.name} доступен только внутри секрета`); }
  }
  if (unreachable.length) errors.push(`недоступны: ${unreachable.slice(0, 8).join('; ')}${unreachable.length > 8 ? ` … всего ${unreachable.length}` : ''}`);

  // Прямая видимость между проёмами на уровне глаз, сквозь двери не видно.
  const eye = 1.7, blocks = items.filter(o => o.name === 'Door' || o.name === 'DoorHalf' || o.name === 'SecretDoor');
  const blocked = (x, z) => {
    const c = idx(toI(x), toI(z));
    return closedCols.solids[c].some(([a, b]) => a <= eye && b >= eye) || blocks.some(o => { if (o.p[1] > eye || o.p[1] + o.s[1] < eye) return false; const f = footprint(o); return x >= f[0] && x <= f[2] && z >= f[1] && z <= f[3]; });
  };
  let sight = 0, pairs = 0;
  for (let a = 0; a < doors.length; a++) for (let b = a + 1; b < doors.length; b++) {
    pairs++;
    const [x1, z1] = doors[a], [x2, z2] = doors[b], steps = Math.ceil(Math.hypot(x2 - x1, z2 - z1) / 0.1);
    let clear = true;
    for (let t = 0; t <= steps && clear; t++) clear = !blocked(x1 + (x2 - x1) * t / steps, z1 + (z2 - z1) * t / steps);
    sight += clear ? 1 : 0;
  }
  const floors = [...new Set([...seenOpen].map(k => Math.round(+k.split('|')[1] / 50) / 2))].length;
  const levels = [...closedY].filter(y => Number.isInteger(y * 4)).sort((a, b) => a - b);
  const cat = c => items.filter(o => o.cat === c).length;
  return { errors, warnings, stats: { items: items.length, walkClosed: Math.round(seen.size * STEP * STEP), walkOpen: Math.round(seenOpen.size * STEP * STEP), walkHeights: levels.length, floors, sightPairs: `${sight}/${pairs}`, secretOnly, doors: items.filter(o => o.name === 'Door' || o.name === 'DoorHalf').length, stairs: cat('stairs'), lights: cat('light') } };
}

module.exports = { validate };
