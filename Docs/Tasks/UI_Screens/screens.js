// Port of DungeonUiBuilder (Assets/Game/Scripts/Editor/Dungeon). Every rect keeps the builder's name, anchor, position and size;
// texts and fills show an example state taken from the views (data.js).
const CELL = 44, EQUIP_SCALE = .83, EQUIP_WIDTH = 607, EQUIP_TOP = 14;
const ICON = n => `../../../Assets/Game/Textures/Icons/${n}.png`;
const ABIL = n => `../../../Assets/Game/Textures/Icons/Painted/${n}.png`;
const ABIL2 = n => `../../../Assets/Game/Textures/Icons/Abilities/${n}.png`;
const MINI = n => `../../../Assets/Game/Textures/Minimap/${n}.png`;
const LINE = [.6, .57, .5, 1];

// ---------- shared pieces ----------
function itemView(parent, x, y, w, h, it, o = {}) {
  const r = rect('ItemView', parent, TOPLEFT, V(x, -y), V(w, h));
  const hidden = it.hidden !== undefined, frame = hidden ? [.35, .31, .25, 1] : D.rarity[it.rarity || 'Common'].frame;
  paint(r, [.09, .08, .07, .95], {});
  const f = rect('Frame', r, CENTER, V(0, 0), V(0, 0), { stretch: 0 }); f.el.style.boxShadow = `inset 0 0 0 1px ${rgba(frame)}`; f.el.style.borderRadius = '3px';
  if (hidden) {} else if (it.icon) img('Icon', r, CENTER, V(0, 0), V(0, 0), COL.white, { stretch: 3, src: ICON(it.icon) });
  else txt('Glyph', r, CENTER, V(0, 0), V(0, 0), Math.min(26, Math.max(10, h * .4)), 'Center', it.glyph || it.name.slice(0, 2), { stretch: 2, bold: true });
  if (it.count) txt('Count', r, BOTTOMRIGHT, V(-3, 2), V(40, 16), 12, 'BottomRight', String(it.count));
  if (it.hidden !== undefined) {
    const hid = rect('Hidden', r, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
    img('RingBack', hid, CENTER, V(0, 0), V(38, 38), [0, 0, 0, .55], { sprite: 'Knob' });
    img('Ring', hid, CENTER, V(0, 0), V(38, 38), [.85, .72, .45, .9], { sprite: 'Knob', fill: it.hidden, radial: true });
    img('RingCore', hid, CENTER, V(0, 0), V(31, 31), [.09, .08, .07, 1], { sprite: 'Knob' });
    img('Eye', hid, CENTER, V(0, 0), V(26, 26), [.8, .76, .66, 1], { sprite: 'Eye' });
  }
  if (o.dim) r.el.style.opacity = .45;
  return r;
}

function grid(parent, name, pos, w, h, items, pouch = 0) {
  const g = rect(name, parent, TOPLEFT, pos, V(w * CELL, h * CELL + (pouch ? CELL + 12 : 0)));
  const cells = rect('Cells', g, TOPLEFT, V(0, 0), V(0, 0)), its = rect('Items', g, TOPLEFT, V(0, 0), V(0, 0));
  const cy = y => y * CELL + (y >= h ? 12 : 0);
  for (let y = 0; y < h + (pouch ? 1 : 0); y++) for (let x = 0; x < (y < h ? w : pouch); x++)
    img('Cell', cells, TOPLEFT, V(x * CELL + 1, -cy(y) - 1), V(CELL - 2, CELL - 2), [.16, .14, .11, .85]);
  for (const it of items) itemView(its, it.x * CELL, cy(it.y), (it.w || 1) * CELL, (it.h || 1) * CELL, it);
  return g;
}

function hexChart(parent, radius, values) {
  const n = rect('Chart', parent, CENTER, V(0, 0), V(2 * radius, 2 * radius));
  const c = i => { const a = i * Math.PI / 3, r = values[i] * radius; return [radius + Math.sin(a) * r, radius - Math.cos(a) * r]; };
  let svg = `<svg width="${2 * radius}" height="${2 * radius}" style="position:absolute;left:0;top:0;overflow:visible">`;
  for (let i = 0; i < 6; i++) {
    const a = c(i), b = c((i + 1) % 6), col = D.stats[D.attributes[i]].color;
    svg += `<polygon points="${radius},${radius} ${a} ${b}" fill="${rgba([...col.slice(0, 3), .42])}"/><line x1="${a[0]}" y1="${a[1]}" x2="${b[0]}" y2="${b[1]}" stroke="${rgba(col)}" stroke-width="2.5"/>`;
  }
  n.el.innerHTML = svg + '</svg>';
}

// BuildStats: hexagram with attribute badges and stat dots on its sides, then the list.
function buildStats(parent, position, width, radius, compact) {
  const chartHeight = 2 * radius + 116;
  const root = rect('StatSheet', parent, TOP, position, V(width, chartHeight));
  const chartRoot = rect('Hexagram', root, TOP, V(0, -chartHeight * .5), V(0, 0), { pivot: CENTER });
  const web = 2 * radius / .84;
  img('Web', chartRoot, CENTER, V(0, 0), V(web, web), COL.white, { sprite: 'Hexagram' });
  hexChart(chartRoot, radius, D.attributes.map(a => D.chart[a] ?? .5));
  const badge = compact ? 50 : 58, dot = compact ? 10 : 12, pitch = compact ? 15 : 18;
  const corner = i => V(Math.sin(i * Math.PI / 3) * radius, Math.cos(i * Math.PI / 3) * radius);
  const statDot = (s, p) => img(s, chartRoot, CENTER, p, V(...Array(2).fill(D.stats[s].edge ? dot * 1.5 : dot)), D.stats[s].color, { sprite: D.stats[s].edge ? 'Circle' : 'Knob' });
  for (let i = 0; i < 6; i++) {
    const next = (i + 1) % 6, a = corner(i), b = corner(next), mid = V((a[0] + b[0]) / 2, (a[1] + b[1]) / 2);
    const len = Math.hypot(b[0] - a[0], b[1] - a[1]), al = V((b[0] - a[0]) / len * pitch, (b[1] - a[1]) / len * pitch);
    const own = D.own[D.attributes[i]] || [], nextOwn = D.own[D.attributes[next]] || [];
    for (let k = Math.floor(own.length / 2); k < own.length; k++) statDot(own[k], V(mid[0] - al[0] * (own.length - k), mid[1] - al[1] * (own.length - k)));
    if (D.edge[i]) statDot(D.edge[i], mid);
    for (let k = 0; k < Math.floor(nextOwn.length / 2); k++) statDot(nextOwn[k], V(mid[0] + al[0] * (k + 1), mid[1] + al[1] * (k + 1)));
  }
  D.attributes.forEach((s, i) => statNode(chartRoot, s, corner(i), badge, compact ? 17 : 19, compact ? 14 : 15));

  const list = [];
  const add = (...ids) => ids.forEach(s => list.push({ s }));
  add(...D.listTop); D.edge.forEach(e => e && add(e));
  list.push({ h: 'Damage' }); add(...D.listDamage); list.push({ h: 'Defense' }); add(...D.listDefense);
  const cols = compact ? 2 : 1, rows = Math.ceil(list.length / cols), rowH = compact ? 17 : 23, ts = compact ? 12 : 15;
  const colW = (width - 40 - (cols - 1) * 16) / cols;
  list.forEach((e, i) => {
    const p = V(-width * .5 + 20 + Math.floor(i / rows) * (colW + 16), -chartHeight - 4 - i % rows * rowH);
    if (e.h) return txt(e.h, root, TOP, p, V(colW, rowH), ts, 'MidlineLeft', e.h.toUpperCase(), { pivot: TOPLEFT, color: COL.gold, bold: true });
    const prev = list[i - 1], indent = prev && prev.s && D.result[prev.s] === e.s ? ts : 0;
    if (D.result[e.s]) statLink(root, e.s, p, V(colW, rowH), ts);
    statRow(root, e.s, V(p[0] + indent, p[1]), V(colW - indent, rowH), ts);
  });
  return root;
}

function statNode(parent, s, position, size, valueSize, nameSize) {
  const st = D.stats[s], node = img(s, parent, CENTER, position, V(size, size), st.color, { sprite: 'Diamond' });
  txt('Value', node, CENTER, V(0, 0), V(size + 16, size), valueSize, 'Center', D.values[s] ?? '10', { bold: true });
  const len = Math.hypot(...position), dir = V(position[0] / len, position[1] / len), atSide = Math.abs(dir[0]) > .8;
  const side = Math.abs(dir[0]) < .1 ? 0 : Math.sign(dir[0]);
  const o = { color: st.color, bold: true };
  if (atSide) txt('Name', node, CENTER, V(side * (size * .5 + 4), 0), V(120, nameSize + 6), nameSize, side > 0 ? 'MidlineLeft' : 'MidlineRight', st.name, { ...o, pivot: V(side > 0 ? 0 : 1, .5) });
  else txt('Name', node, CENTER, V(side * 10, Math.sign(dir[1]) * (size * .5 + nameSize * .5 + 5)), V(120, nameSize + 6), nameSize, 'Center', st.name, o);
}

function statRow(parent, s, position, size, ts) {
  const st = D.stats[s], row = img(s, parent, TOP, position, size, [1, 1, 1, .025], { pivot: TOPLEFT });
  img('Dot', row, LEFT, V(ts * .5, 0), V(ts * .6, ts * .6), st.color, { sprite: st.edge ? 'Circle' : 'Knob' });
  txt('Name', row, LEFT, V(ts * 1.2, 0), V(size[0] - ts * 1.2 - 60, size[1]), ts, 'MidlineLeft', st.name, { pivot: LEFT, color: [.8, .77, .7, 1] });
  txt('Value', row, RIGHT, V(-4, 0), V(70, size[1]), ts, 'MidlineRight', D.values[s] ?? '0', { pivot: RIGHT, bold: true });
}

function statLink(parent, s, position, rowSize, ts) {
  const c = D.stats[s].color;
  const plate = img(s + 'Link', parent, TOP, V(position[0] - 4, position[1] + 1), V(rowSize[0] + 4, rowSize[1] * 2 + 2), [c[0], c[1], c[2], .1], { pivot: TOPLEFT });
  img('Accent', plate, LEFT, V(1, 0), V(2, rowSize[1] * 2), c);
  const x = ts * .5 + 4;
  img('Down', plate, TOP, V(x - (rowSize[0] + 4) * .5, -1 - rowSize[1] * .5), V(2, rowSize[1]), c, { pivot: TOP });
  img('Across', plate, TOP, V(x - (rowSize[0] + 4) * .5, -1 - rowSize[1] * 1.5), V(ts, 2), c, { pivot: LEFT });
}

function abilityIcon(name, parent, anchor, position, sprite, size, a = {}) {
  const r = rect(name, parent, anchor, position, V(104, 104), { pivot: CENTER });
  img('Frame', r, CENTER, V(0, 0), V(size, size), a.frame || [.5, .42, .3, 1], { sprite });
  if (a.icon) img('Icon', r, CENTER, V(0, 0), V(size * (sprite === 'Diamond' ? .56 : .72), size * (sprite === 'Diamond' ? .56 : .72)), COL.white, { src: a.icon });
  if (a.glyph) txt('Glyph', r, CENTER, V(0, 0), V(size, 36), 25, 'Center', a.glyph, { bold: true });
  if (a.badge) txt('Badge', r, CENTER, V(0, sprite === 'Diamond' ? -26 : -size * .5 - 9), V(size, 18), 13, 'Center', a.badge, { bold: true, color: COL.gold });
  if (a.dim) r.el.style.opacity = .4;
  return r;
}

function tooltip(parent, name, anchor, pos, w, h, text) {
  const t = rect(name, parent, anchor, pos, V(w, h), { pivot: TOPLEFT });
  img('Back', t, CENTER, V(0, 0), V(0, 0), [.03, .03, .03, .96], { stretch: 0 });
  img('Frame', t, TOP, V(0, 0), V(w, 3), COL.frame);
  txt('Text', t, CENTER, V(0, 0), V(0, 0), 15, 'TopLeft', text, { stretch: 12, color: COL.text });
  return t;
}

// ---------- loading ----------
function buildLoading(root, title, status, progress) {
  const p = rect('Loading', root, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  img('Back', p, CENTER, V(0, 0), V(0, 0), COL.black, { stretch: 0 });
  img('Vignette', p, CENTER, V(0, 0), V(0, 0), [.45, .3, .12, .35], { stretch: 0, sprite: 'Glow' });
  txt('Title', p, CENTER, V(0, 60), V(1400, 80), 58, 'Center', title, { color: COL.gold, bold: true });
  img('Rule', p, CENTER, V(0, 8), V(420, 2), COL.frame);
  txt('Status', p, CENTER, V(0, -30), V(1000, 34), 22, 'Center', status, { color: COL.text });
  img('BarBack', p, CENTER, V(0, -74), V(424, 12), COL.frame);
  img('BarInner', p, CENTER, V(0, -74), V(420, 8), [.05, .04, .03, 1]);
  img('BarFill', p, CENTER, V(0, -74), V(420, 8), COL.gold, { fill: progress });
  const sp = img('Spinner', p, BOTTOMRIGHT, V(-70, 70), V(54, 54), COL.gold, { pivot: CENTER, sprite: 'Diamond' });
  sp.el.style.animation = 'spin 1.6s linear infinite';
  txt('Tip', p, BOTTOM, V(0, 60), V(1200, 30), 16, 'Center', 'Containers open at once, but their loot has to be searched. Perception makes the search faster.', { color: COL.textDim });
}

// ---------- HUD ----------
function abilitySlot(parent, anchor, position, key, s = {}) {
  const slot = rect('Ability' + key, parent, anchor, position, V(56, 56));
  img('Frame', slot, CENTER, V(0, 0), V(60, 60), COL.frame);
  img('Back', slot, CENTER, V(0, 0), V(56, 56), COL.panelLight);
  if (s.icon) img('Icon', slot, CENTER, V(0, 0), V(50, 50), COL.white, { src: s.icon });
  if (s.glyph) txt('Glyph', slot, CENTER, V(0, 0), V(56, 56), 22, 'Center', s.glyph, { bold: true });
  if (s.cd) img('Cooldown', slot, CENTER, V(0, 0), V(56, 56), [0, 0, 0, .7], { sprite: 'Knob', fill: s.cd, radial: true });
  img('KeyBack', slot, TOP, V(0, 14), V(24, 16), [.1, .08, .06, .9], { pivot: BOTTOM });
  txt('Key', slot, TOP, V(0, 14), V(30, 16), 12, 'Center', key, { pivot: BOTTOM, color: [1, .9, .6, 1] });
  if (s.charges) txt('Charges', slot, BOTTOMRIGHT, V(-4, 2), V(40, 16), 13, 'BottomRight', s.charges, { pivot: BOTTOMRIGHT });
}

function beltSlot(parent, anchor, position, key, s = {}) {
  const slot = rect('Belt' + key, parent, anchor, position, V(44, 44));
  img('Frame', slot, CENTER, V(0, 0), V(48, 48), s.active ? COL.gold : COL.frame);
  img('Back', slot, CENTER, V(0, 0), V(44, 44), COL.panelLight);
  if (s.icon) img('Icon', slot, CENTER, V(0, 0), V(40, 40), COL.white, { src: ICON(s.icon) });
  txt('Key', slot, TOPLEFT, V(3, -1), V(20, 14), 10, 'TopLeft', key, { pivot: TOPLEFT, color: [1, .9, .6, 1] });
  if (s.count) txt('Count', slot, BOTTOMRIGHT, V(-3, 1), V(30, 14), 11, 'BottomRight', s.count, { pivot: BOTTOMRIGHT });
  if (s.fuel !== undefined) { const fb = img('FuelBack', slot, BOTTOM, V(0, 5), V(36, 5), [.08, .06, .04, .9]); img('Fuel', fb, CENTER, V(0, 0), V(34, 3), [1, .6, .2, 1], { fill: s.fuel }); }
}

function buildHud(root, o = {}) {
  const h = D.hud, hud = rect('HUD', root, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  if (o.hurt) { const v = img('Vignette', hud, CENTER, V(0, 0), V(0, 0), [.5, 0, 0, .0], { stretch: 0 }); v.el.style.background = 'radial-gradient(ellipse at center, transparent 55%, rgba(128,0,0,.45))'; }
  // Compass
  const strip = rect('Compass', hud, TOP, V(0, -14), V(560, 30), { pivot: TOP });
  strip.el.style.overflow = 'hidden';
  img('Center', strip, TOP, V(0, 0), V(2, 8), [.95, .85, .6, 1], { pivot: TOP });
  for (const l of h.compass) txt('Label', strip, CENTER, V(l.x, 0), V(40, 24), l.size, 'Center', l.label, { color: [...l.color.slice(0, 3), l.alpha] });

  img('Crosshair', hud, CENTER, V(0, 0), V(4, 4), [1, 1, 1, .75]);
  if (o.prompt) {
    img('PromptRing', hud, CENTER, V(0, 0), V(60, 60), [1, .9, .6, .9], { sprite: 'Knob', fill: .62, radial: true });
    txt('Prompt', hud, CENTER, V(0, -58), V(700, 30), 20, 'Center', h.prompt);
  }
  if (o.cast) {
    const cast = rect('CastBar', hud, BOTTOM, V(0, 190), V(240, 60));
    txt('Name', cast, CENTER, V(0, 20), V(300, 24), 18, 'Center', h.cast, { color: [.95, .9, .85, 1] });
    img('Back', cast, CENTER, V(0, 0), V(240, 14), [0, 0, 0, .7]);
    img('Fill', cast, CENTER, V(0, 0), V(236, 10), [.78, .1, .42, 1], { fill: .55 });
    txt('Cancel', cast, CENTER, V(0, -22), V(300, 20), 14, 'Center', '[F] Cancel', { color: [.8, .8, .8, 1] });
  }
  const fl = img('HealthFlash', hud, BOTTOM, V(0, 40), V(540, 84), [.9, .05, .03, o.hurt ? .5 : 0], { sprite: 'SoftBar' });
  img('HealthFrame', hud, BOTTOM, V(0, 40), V(470, 40), COL.frame);
  img('HealthBack', hud, BOTTOM, V(0, 40), V(462, 32), [.06, .03, .02, .95]);
  img('RecoverableFill', hud, BOTTOM, V(0, 40), V(452, 22), [.75, .1, .08, .35], { fill: h.recoverable });
  img('HealthFill', hud, BOTTOM, V(0, 40), V(452, 22), [.75, .1, .08, 1], { fill: h.health });
  txt('HealthText', hud, BOTTOM, V(0, 40), V(452, 22), 15, 'Center', h.healthText);
  txt('Name', hud, BOTTOM, V(0, 70), V(600, 22), 17, 'Center', h.name, { color: [.92, .88, .78, 1] });
  img('RestRing', hud, BOTTOM, V(-150, 74), V(26, 26), [.6, .6, .6, .9], { sprite: 'Knob', fill: 1, radial: true });
  txt('RestKey', hud, BOTTOM, V(-124, 74), V(30, 20), 12, 'Center', 'G');
  abilitySlot(hud, BOTTOM, V(-272, 44), 'Q', h.q);
  abilitySlot(hud, BOTTOM, V(272, 44), 'E', h.e);
  txt('Readied', hud, BOTTOM, V(372, 44), V(360, 44), 15, 'MidlineLeft', o.readied ? h.readied : '', { pivot: LEFT });
  for (let i = 0; i < 6; i++) beltSlot(hud, BOTTOM, V((i < 3 ? -182 : 78) + i % 3 * 52, 104), String(3 + Math.floor(i / 3)), h.belt[i]);
  const fx = rect('Effects', hud, BOTTOM, V(0, 152), V(h.effects.length * 46, 42));
  h.effects.forEach((e, i) => {
    const c = rect('Effect' + i, fx, LEFT, V(i * 46 + 2, 0), V(42, 42), { pivot: LEFT });
    img('Frame', c, CENTER, V(0, 0), V(42, 42), e.debuff ? [.55, .16, .12, 1] : COL.frame);
    img('Back', c, CENTER, V(0, 0), V(38, 38), COL.panelLight);
    img('Icon', c, CENTER, V(0, 0), V(36, 36), COL.white, { src: e.icon });
    if (e.stacks) txt('Stacks', c, BOTTOMRIGHT, V(-2, 1), V(30, 14), 11, 'BottomRight', String(e.stacks), { pivot: BOTTOMRIGHT });
  });

  const row = rect('WeaponRow', hud, BOTTOMRIGHT, V(-330, 56), V(90, 40));
  h.weaponSets.forEach((set, i) => {
    const on = i === h.weaponSlot, s = rect('Slot' + (i + 1), row, LEFT, V(i * 62, 0), V(28, 28), { pivot: LEFT });
    img('Frame', s, CENTER, V(0, 0), V(32, 32), on ? COL.gold : COL.frame);
    img('Back', s, CENTER, V(0, 0), V(28, 28), COL.panelLight);
    txt('Key', s, CENTER, V(0, 0), V(28, 28), 14, 'Center', String(i + 1), { color: on ? COL.gold : COL.white });
    const w = rect('Weapons', s, CENTER, V(0, 70), V(54, 96));
    img('Frame', w, CENTER, V(0, 0), V(56, 98), on ? COL.gold : COL.frame);
    img('Back', w, CENTER, V(0, 0), V(52, 94), COL.panelLight);
    const a = [1, 1, 1, on ? 1 : .6];
    if (set.main) img('Main', w, CENTER, V(0, 0), V(50, 90), a, { src: ICON(set.main) });
    if (set.off) {
      img('OffBack', w, BOTTOMRIGHT, V(-2, 2), V(24, 24), COL.frame, { pivot: BOTTOMRIGHT });
      img('Off', w, BOTTOMRIGHT, V(-3, 3), V(22, 22), a, { pivot: BOTTOMRIGHT, src: ICON(set.off) });
    }
    if (i === 0) img('Line', row, LEFT, V(30, 0), V(30, 2), [.5, .45, .35, .8], { pivot: LEFT });
  });
  img('WeaponBack', hud, BOTTOMRIGHT, V(-280, 22), V(300, 26), [.05, .04, .03, .85]);
  txt('Weapon', hud, BOTTOMRIGHT, V(-280, 22), V(300, 26), 16, 'Center', h.weapon);

  if (!o.hub) {
    const mm = rect('Minimap', hud, BOTTOMRIGHT, V(-24, 50), V(230, 230), { pivot: BOTTOMRIGHT });
    img('Frame', mm, CENTER, V(0, 0), V(238, 238), COL.frame);
    const map = img('Map', mm, CENTER, V(0, 0), V(230, 230), [.9, .85, .7, 1]);
    map.el.style.overflow = 'hidden';
    const m = rect('Floor', map, CENTER, V(-60, 40), V(3 * 230, 3 * 230), { pivot: CENTER }); floorTiles(m, 3, D.minimap);
    if (o.swarm) img('Swarm', map, CENTER, V(0, 0), V(0, 0), [.75, .3, 1, .95], { stretch: 0 }).el.style.cssText += ';background:none;box-shadow:inset 0 0 0 4px rgba(190,76,255,.9), inset 0 0 40px rgba(190,76,255,.5)';
    const ar = img('Arrow', mm, CENTER, V(0, 0), V(14, 14), [.95, .75, .2, 1], { sprite: 'Knob' });
    img('Tip', ar, CENTER, V(0, 9), V(4, 10), [.95, .75, .2, 1]);
    img('TimerBack', hud, BOTTOMRIGHT, V(-24, 284), V(230, 26), [.05, .04, .03, .9], { pivot: BOTTOMRIGHT });
    txt('Timer', hud, BOTTOMRIGHT, V(-24, 284), V(230, 26), 18, 'Center', h.timer, { pivot: BOTTOMRIGHT });
    txt('Swarm', hud, BOTTOMRIGHT, V(-24, 312), V(360, 22), 13, 'MidlineRight', o.swarm ? h.swarm : '', { pivot: BOTTOMRIGHT });
    img('ModuleBack', hud, BOTTOMRIGHT, V(-24, 22), V(230, 26), [.3, .22, .1, .95], { pivot: BOTTOMRIGHT });
    txt('Module', hud, BOTTOMRIGHT, V(-24, 22), V(230, 26), 15, 'Center', h.module, { pivot: BOTTOMRIGHT, color: COL.gold });
  }
  const tab = rect('TabHint', hud, TOPLEFT, V(24, -24), V(60, 70), { pivot: TOPLEFT });
  img('Back', tab, TOP, V(0, 0), V(52, 52), COL.panelLight, { pivot: TOP });
  txt('Icon', tab, TOP, V(0, -8), V(52, 36), 16, 'Center', 'Bag', { pivot: TOP });
  txt('Key', tab, TOP, V(0, -54), V(60, 16), 12, 'Center', 'Tab', { pivot: TOP });
  if (!o.hub) txt('Floor', hud, TOPRIGHT, V(-30, -24), V(400, 24), 16, 'MidlineRight', h.floor);
  txt('Kills', hud, TOPRIGHT, V(-30, -48), V(400, 22), 14, 'MidlineRight', o.hub ? '' : h.kills);
  if (o.boss) {
    const b = rect('Boss', hud, TOP, V(0, -70), V(520, 40));
    txt('Name', b, CENTER, V(0, 12), V(520, 20), 16, 'Center', h.boss, { color: [1, .6, .5, 1] });
    img('Back', b, CENTER, V(0, -8), V(520, 12), [0, 0, 0, .7]);
    img('Fill', b, CENTER, V(0, -8), V(514, 8), [.8, .15, .1, 1], { fill: .72 });
  }
  if (o.aim) { const a = rect('AimCrosshair', hud, CENTER, V(0, 0), V(0, 0)); img('Left', a, CENTER, V(-10, 0), V(12, 2), [1, 1, 1, .75], { pivot: RIGHT }); img('Right', a, CENTER, V(10, 0), V(12, 2), [1, 1, 1, .75], { pivot: LEFT }); }
  if (o.swing) swingHint(hud);
  return hud;
}

function floorTiles(parent, n, names) {
  const s = parent.w / n;
  names.slice(0, n * n).forEach((m, i) => img('Module', parent, TOPLEFT, V(i % n * s, -Math.floor(i / n) * s), V(s, s), COL.white, { src: MINI(m), cover: true }));
}

function swingHint(hud) {
  const s = D.swing; if (!s) return;
  const r = rect('SwingHint', hud, CENTER, s.pos, s.size);
  if (s.draw) s.draw(r);
}

// ---------- inventory ----------
function equipRect(name, parent, x, y, w, h) {
  return rect(name, parent, TOP, V((x - EQUIP_WIDTH * .5) * EQUIP_SCALE, (-y - EQUIP_TOP) * EQUIP_SCALE), V(w * EQUIP_SCALE, h * EQUIP_SCALE), { pivot: TOPLEFT });
}
function equipHeader(parent, number, x, y, width) {
  const r = equipRect('Header' + number, parent, x, y, width, 20); paint(r, [.16, .13, .1, .95], {});
  txt('Number', r, CENTER, V(0, 0), V(0, 0), 14, 'Center', number, { stretch: 0, color: COL.gold });
}
function equipSlot(parent, slot, label, x, y, w, h, item) {
  const r = equipRect('Slot' + slot, parent, x, y, w, h);
  img('Frame', r, CENTER, V(0, 0), V(r.w + 4, r.h + 4), [.3, .25, .18, .9]);
  paint(r, [0, 0, 0, .6], {}); r.el.style.background = 'none';
  img('Background', r, CENTER, V(0, 0), V(0, 0), [0, 0, 0, .6], { stretch: 0 });
  txt('Label', r, CENTER, V(0, 0), V(0, 0), 12, 'Center', label, { stretch: 2, color: [1, 1, 1, .35] });
  if (item) itemView(r, 0, 0, r.w, r.h, item);
}

const EQUIP_SLOTS = [
  ['Weapon1Main', '', 0, 24, 76, 186], ['Weapon1Off', '', 78, 24, 76, 186], ['Weapon2Main', '', 453, 24, 76, 186], ['Weapon2Off', '', 531, 24, 76, 186],
  ['Head', 'Head', 255, 32, 98, 96], ['Necklace', 'Neck', 365, 76, 52, 52], ['Chest', 'Chest', 238, 144, 132, 176], ['Back', 'Cloak', 384, 225, 96, 96],
  ['Pouch1', 'Bag', 0, 226, 78, 78], ['Pouch2', 'Bag', 529, 226, 78, 78], ['Ring1', 'Ring', 172, 338, 52, 52], ['Legs', 'Legs', 238, 336, 132, 180],
  ['Ring2', 'Ring', 384, 338, 52, 52], ['Hands', 'Hands', 126, 404, 97, 96], ['Feet', 'Feet', 384, 404, 97, 96],
  ['Utility1', '', 0, 334, 78, 74], ['Utility2', '', 0, 410, 78, 74], ['Utility3', '', 0, 486, 78, 74],
  ['Utility4', '', 529, 334, 78, 74], ['Utility5', '', 529, 410, 78, 74], ['Utility6', '', 529, 486, 78, 74],
];

function dollPlaceholder(parent, name, anchor, pos, size, o = {}) {
  const r = img(name, parent, anchor, pos, size, [.05, .04, .035, 1], o);
  r.el.style.background = 'radial-gradient(ellipse at 50% 40%, #2b241c, #0c0a08 70%)';
  const sil = rect('Silhouette', r, CENTER, V(0, -size[1] * .04), V(size[0] * .45, size[1] * .86));
  sil.el.style.cssText += ';background:linear-gradient(#0000,#0000),radial-gradient(circle at 50% 9%, #6d5f4c 0 9%, transparent 9.5%),linear-gradient(#5a4e3e,#3a3229);-webkit-mask:radial-gradient(circle at 50% 9%,#000 9%,transparent 9.5%),linear-gradient(90deg,transparent 22%,#000 22% 78%,transparent 78%) 0 18%/100% 46% no-repeat,linear-gradient(90deg,transparent 30%,#000 30% 47%,transparent 47% 53%,#000 53% 70%,transparent 70%) 0 100%/100% 38% no-repeat;opacity:.55';
  txt('Note', r, BOTTOM, V(0, 6), V(size[0], 18), 12, 'Center', '3D preview (RenderTexture)', { color: [.6, .55, .45, .7], italic: true });
  return r;
}

function buildInventory(root, fullscreen, st) {
  const panel = rect((fullscreen ? 'Inventory' : 'Kit') + 'Panel', root, CENTER, V(0, fullscreen ? 0 : -60), V(1920, 1080));
  if (fullscreen) img('Back', panel, CENTER, V(0, 0), V(0, 0), [.02, .02, .02, .82], { stretch: 0 });
  const R = {};
  if (fullscreen) R.doll = rect('Doll', panel, TOPLEFT, V(40, -40), V(460, 980));
  else R.doll = rect('Doll', panel, TOP, V(-390, -36), V(240, 490), { pivot: TOPLEFT });
  img('Back', R.doll, CENTER, V(0, 0), V(0, 0), COL.panel, { stretch: 0 });
  if (fullscreen) { img('ArchFrame', R.doll, TOP, V(0, -20), V(360, 400), COL.frame); dollPlaceholder(R.doll, 'Preview', TOP, V(0, -24), V(352, 392)); }
  else { img('ArchFrame', R.doll, TOP, V(0, -14), V(224, 420), COL.frame); dollPlaceholder(R.doll, 'Preview', TOP, V(0, -18), V(216, 412)); }
  txt('Title', R.doll, TOP, V(0, fullscreen ? -428 : -442), V(fullscreen ? 400 : 236, 30), 24, 'Center', st.title, { color: COL.gold });
  if (fullscreen) buildStats(R.doll, V(0, -462), 460, 105, true);
  else { const sheet = img('Stats', panel, TOPLEFT, V(20, -36), V(530, 964), COL.white, { sprite: 'Panel' }); buildStats(sheet, V(0, -34), 530, 130, false); }

  const eq = fullscreen ? rect('Equipment', panel, TOPLEFT, V(530, -40), V(640, 500)) : rect('Equipment', panel, TOP, V(-140, -36), V(600, 490), { pivot: TOPLEFT });
  img('Back', eq, CENTER, V(0, 0), V(0, 0), COL.panel, { stretch: 0 });
  [['1', 0, 0], ['2', 453, 0], ['3', 0, 310], ['4', 529, 310]].forEach(([n, x, y]) => equipHeader(eq, n, x, y, n === '1' || n === '2' ? 154 : 78));
  EQUIP_SLOTS.forEach(([s, l, x, y, w, h]) => equipSlot(eq, s, l, x, y, w, h, st.equip[s]));

  const bag = fullscreen ? rect('BagPanel', panel, TOPLEFT, V(530, -560), V(640, 36 + CELL * 6 + 28))
    : rect('BagPanel', panel, TOP, V(-390, -536), V(850, 1000 - 46 - 490), { pivot: TOPLEFT });
  img('Back', bag, CENTER, V(0, 0), V(0, 0), COL.panel, { stretch: 0 });
  txt('BagTitle', bag, TOPLEFT, V(16, -8), V(300, 24), 16, 'MidlineLeft', 'Backpack', { pivot: TOPLEFT, color: COL.gold });
  grid(bag, 'Bag', fullscreen ? V(100, -36) : V(205, -70), 10, 5, st.bag, st.pouch);
  txt('Value', bag, TOPRIGHT, V(-16, -8), V(300, 24), 14, 'MidlineRight', st.value, { pivot: TOPRIGHT });
  btn('Sort', bag, TOPRIGHT, V(-16, -34), V(80, 26), 'Sort', { pivot: TOPRIGHT });
  if (!fullscreen) txt('Hints', bag, BOTTOM, V(0, 16), V(820, 44), 13, 'Center',
    '[Drag] move   [R Click] equip / unequip   [Shift + L Click] to the stash and back   [Ctrl + Drag] split a stack\nA belt pouch opens its cells under the backpack; it comes off only while they are empty', { color: COL.textDim });

  if (st.other) {
    const other = fullscreen ? rect('OtherPanel', panel, TOPLEFT, V(1200, -40), V(680, 440)) : rect('OtherPanel', panel, TOPRIGHT, V(-20, -36), V(470, 964));
    img('Back', other, CENTER, V(0, 0), V(0, 0), COL.panel, { stretch: 0 });
    txt('Title', other, TOPLEFT, V(16, -10), V(400, 30), 20, 'MidlineLeft', st.other.title, { pivot: TOPLEFT, color: COL.gold });
    grid(other, 'Other', fullscreen ? V(16, -50) : V(15, -50), 10, st.other.rows, st.other.items);
    if (fullscreen) btn('TakeAll', other, TOPRIGHT, V(-16, -12), V(140, 32), 'Take all', { pivot: TOPRIGHT });
    else {
      for (let i = 0; i < 3; i++) btn('Page' + (i + 1), other, TOPLEFT, V(15 + i * 150, -504), V(142, 34), `Page ${i + 1}`, { pivot: TOPLEFT, color: i === 0 ? [.3, .24, .14, .95] : COL.panelLight });
      txt('Coins', other, TOPRIGHT, V(-15, -504), V(150, 34), 18, 'MidlineRight', st.other.coins, { pivot: TOPRIGHT, color: COL.gold });
      const sell = img('Sell', other, BOTTOM, V(0, 15), V(440, 140), [.12, .09, .05, .95]);
      img('Frame', sell, TOP, V(0, 0), V(440, 2), COL.frame);
      txt('Text', sell, CENTER, V(0, 0), V(0, 0), 20, 'Center', st.other.sell, { stretch: 8, color: [.85, .8, .7, 1] });
    }
  }
  if (fullscreen) txt('Hints', panel, TOP, V(0, -6), V(1400, 24), 13, 'Center',
    '[Drag] move   [R Click] equip / use / unequip   [Shift + L Click] quick transfer   [Ctrl + Drag] split stack   [Drag outside] drop   ·   eye = not searched yet', { pivot: TOP, color: [.8, .75, .65, 1] });
  if (st.tooltip) {
    const t = rect('Tooltip', panel, TOPLEFT, st.tooltip.pos, V(380, st.tooltip.h || 240));
    img('Back', t, CENTER, V(0, 0), V(0, 0), [.03, .03, .03, .96], { stretch: 0 });
    img('Frame', t, TOP, V(0, 0), V(380, 3), COL.frame);
    txt('Text', t, CENTER, V(0, 0), V(0, 0), 15, 'TopLeft', st.tooltip.text, { stretch: 12 });
  }
  return panel;
}

// ---------- hub pages ----------
function hubPages(root, title, build) {
  const panel = rect('Pages', root, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  img('Back', panel, CENTER, V(0, 0), V(0, 0), COL.black, { stretch: 0 });
  build(panel);
  const bar = rect('TopBar', panel, TOP, V(0, 0), V(0, 82), { xStretch: true });
  img('Back', bar, CENTER, V(0, 0), V(0, 0), [0, 0, 0, .94], { stretch: 0 });
  img('Rule', bar, BOTTOM, V(0, 0), V(0, 2), COL.frame, { xStretch: true });
  txt('Title', bar, LEFT, V(150, 0), V(900, 60), 30, 'MidlineLeft', title, { color: COL.gold });
  txt('Close', bar, RIGHT, V(-30, 0), V(300, 60), 18, 'MidlineRight', '[Esc] Close', { color: COL.textDim });
  const sh = img('Rank', bar, TOPLEFT, V(26, 0), V(88, 110), COL.white, { sprite: 'Shield' });
  txt('Text', sh, CENTER, V(0, 10), V(88, 70), 15, 'Center', D.rank, { color: [.16, .1, .03, 1], bold: true, lineSpacing: -30 });
}

function buildBoard(lobby, mode) {
  const b = D.board, page = rect('Board', lobby, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  const party = rect('Party', page, CENTER, V(0, 0), V(0, 0), { stretch: 0 }), descent = rect('Descent', page, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  dollPlaceholder(page, 'Preview', TOP, V(0, -330), V(480, 620)).el.style.background = 'radial-gradient(ellipse at 50% 45%, #241e17, #000 70%)';
  txt('Name', page, TOP, V(0, -196), V(700, 96), 27, 'Bottom', b.name, { color: COL.text });
  img('NameRule', page, TOP, V(0, -298), V(260, 2), COL.frame);
  txt('PartyTitle', page, TOPRIGHT, V(-56, -128), V(270, 30), 17, 'Center', b.partyTitle, { color: COL.text });
  img('PartyRule', page, TOPRIGHT, V(-56, -164), V(270, 2), COL.frame);
  b.slots.forEach((t, i) => {
    const c = rect('PartySlot' + i, page, TOPRIGHT, V(-56, -180 - i * 98), V(270, 86));
    img('Frame', c, CENTER, V(0, 0), V(0, 0), COL.frame, { stretch: 0 });
    img('Back', c, CENTER, V(0, 0), V(0, 0), [.05, .04, .035, .96], { stretch: 2 });
    txt('Text', c, CENTER, V(0, 0), V(0, 0), 20, 'MidlineLeft', t, { stretch: 14 });
  });
  const pb = -180 - 3 * 98;
  if (mode === 'party') {
    btn(b.inParty ? 'LeaveParty' : 'CreateParty', party, TOPRIGHT, V(-56, pb), V(270, 40), b.inParty ? 'Leave Party' : 'Create Party');
    if (!b.inParty) { input('PartyCode', party, TOPRIGHT, V(-196, pb - 50), V(130, 40), 'Code'); btn('JoinParty', party, TOPRIGHT, V(-56, pb - 50), V(130, 40), 'Join Party'); }
  } else {
    btn('Queue', descent, BOTTOMLEFT, V(24, 356), V(307, 44), b.queue, { color: COL.panel, fs: 20 });
    txt('MapLabel', descent, BOTTOMLEFT, V(24, 322), V(307, 26), 17, 'Center', 'Select Map', { color: COL.textDim });
    const map = btn('Map', descent, BOTTOMLEFT, V(24, 122), V(307, 194), 'Change Map', { color: COL.panel, fs: 20, align: 'Bottom', margin: [0, 0, 0, 6] });
    txt('Title', map, TOP, V(0, -8), V(291, 26), 19, 'Center', b.mapTitle);
    txt('Info', map, TOP, V(0, -34), V(291, 20), 13, 'Center', b.mapInfo, { color: [.45, .7, .95, 1] });
    const pic = rect('Picture', map, TOP, V(0, -60), V(291, 96)); pic.el.style.overflow = 'hidden'; pic.el.style.filter = 'sepia(.3) brightness(.85)';
    floorTiles(rect('Floor', pic, CENTER, V(0, 0), V(291, 291), { pivot: CENTER }), 3, D.minimap.slice(9));
    btn('Start', descent, BOTTOMLEFT, V(24, 24), V(307, 86), 'Start', { color: [.17, .16, .15, .97], fs: 34 });
    [['Characters', 'Change\nCharacter', 'Chr'], ['Wipe', 'Wipe\nSave', 'Wipe'], ['ResetKit', 'Squire\nKit', 'Kit'], ['MaxLevel', 'Dev\nMax Level', 'Lv']].forEach(([n, cap, g], col) => {
      const t = btn(n, descent, BOTTOMRIGHT, V(-24 - col * 86, 24), V(68, 68), g, { sprite: 'Square', color: [.72, .64, .5, 1] });
      txt('Caption', t, TOP, V(0, 4), V(86, 36), 12, 'Bottom', cap, { pivot: BOTTOM, color: COL.text });
    });
  }
  txt('Notice', page, BOTTOM, V(0, 66), V(900, 30), 18, 'Center', b.notice, { color: [1, .78, .45, 1] });
  if (b.result && mode !== 'party') { const r = img('Result', page, BOTTOM, V(0, 24), V(640, 34), [0, 0, 0, .65]); txt('Text', r, CENTER, V(0, 0), V(620, 34), 16, 'Center', b.result, { color: COL.text }); }
}

function buildSkills(lobby, mode) {
  const k = D.skills, page = rect('Skills', lobby, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  const sheet = img('Stats', page, TOPLEFT, V(20, -96), V(530, 964), COL.white, { sprite: 'Panel' });
  buildStats(sheet, V(0, -34), 530, 130, false);
  const bar = rect('Tabs', page, TOP, V(0, -104), V(620, 40));
  ['Perks and Skills', 'Spell Wheels'].forEach((m, i) => {
    const on = (mode === 'wheels') === (i === 1);
    const t = btn('Tab' + m.replace(/ /g, ''), bar, LEFT, V(i * 320, 0), V(300, 40), m, { sprite: 'Glow', color: on ? COL.white : COL.clear, fs: 23, pivot: LEFT, textColor: on ? COL.white : COL.textDim });
  });
  img('TitleRule', page, TOP, V(0, -146), V(620, 2), COL.frame);
  txt('SubclassText', page, TOP, V(0, -160), V(900, 24), 13, 'Center', k.subclassText, { color: COL.textDim });

  if (mode !== 'wheels') {
    const ch = rect('Character', page, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
    dollPlaceholder(ch, 'Doll', TOP, V(0, -205), V(480, 590)).el.style.background = 'radial-gradient(ellipse at 50% 45%, #241e17, #000 70%)';
    img('Arch', ch, TOP, V(0, -190), V(532, 620), LINE, { sprite: 'Arch' });
    img('ArchBase', ch, TOP, V(0, -811), V(440, 2), LINE);
    k.subclasses.forEach((s, i) => {
      const t = btn('Subclass' + i, page, TOP, V((i - 1) * 150, -828), V(60, 60), '', { sprite: 'Knob', color: i === k.subclass ? [.55, .45, .28, 1] : [.12, .1, .08, 1] });
      img('Rim', t, CENTER, V(0, 0), V(60, 60), i === k.subclass ? COL.gold : LINE, { sprite: 'Circle' });
      if (s.icon) img('Icon', t, CENTER, V(0, 0), V(40, 40), COL.white, { src: s.icon });
      txt('Name', t, BOTTOM, V(0, -24), V(150, 22), 14, 'Center', s.name, { color: i === k.subclass ? COL.gold : COL.text });
    });
    k.perkSlots.forEach((p, i) => abilityIcon('PerkSlot' + i, ch, TOP, V((i - 1.5) * 122, -992), 'Diamond', 104, p));
    k.skillSlots.forEach((p, i) => abilityIcon('SkillSlot' + i, ch, TOP, V(i === 0 ? -330 : 330, -984), 'Square', 88, p));
  } else buildSpells(page);

  const pool = img('Pool', page, TOPRIGHT, V(-20, -96), V(530, 964), COL.white, { sprite: 'Panel' });
  const content = rect('Content', pool, TOP, V(0, 0), V(0, 0), { stretch: 0 });
  let y = 9;
  const section = (name, cell, cols, sp, items) => {
    const band = img(name + 'Band', content, TOP, V(0, -y), V(470, 38), [0, 0, 0, .35], { pivot: TOP });
    txt(name + 'Title', band, CENTER, V(0, 0), V(0, 0), 22, 'Center', name, { stretch: 0, color: COL.text });
    y += 48;
    const rows = Math.ceil(items.length / cols), gw = cols * cell + (cols - 1) * sp[0];
    const g = rect(name, content, TOP, V(0, -y), V(470, rows * cell + (rows - 1) * sp[1]), { pivot: TOP });
    items.forEach((a, i) => abilityIcon(a.name || name, g, TOPLEFT, V((470 - gw) / 2 + i % cols * (cell + sp[0]) + cell / 2, -Math.floor(i / cols) * (cell + sp[1]) - cell / 2), a.sprite, a.size, a));
    y += rows * cell + (rows - 1) * sp[1] + 14;
  };
  section('Perks', 104, 4, [18, 14], k.perks);
  section('Skills', 104, 4, [18, 14], k.skills);
  if (k.spells.length) section('Spells', 92, 5, [2, 20], k.spells);
  txt('Hint', pool, TOP, V(0, -918), V(470, 40), 12, 'Center',
    'Drag a perk, a skill or a spell onto a slot  ·  click a perk to equip or remove it  ·  LMB / RMB on a skill binds it to Q / E, RMB on a Q / E slot empties it, LMB / RMB on a spell puts it into wheel I / II', { color: COL.textDim });
  if (k.tooltip && mode !== 'wheels') tooltip(page, 'Tooltip', TOPLEFT, V(1390 - 340 - 20, -300), 340, 112, k.tooltip);
}

function buildSpells(page) {
  const k = D.skills, sp = rect('Spells', page, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  const g = rect('Wheels', sp, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  txt('Info', g, TOP, V(0, -232), V(820, 24), 13, 'Center', `Each wheel holds ${k.wheelSize} spells  ·  wheel I opens with the memory skill put on Q, wheel II with the one on E`, { color: COL.textDim });
  for (let w = 0; w < 2; w++) {
    const ring = rect('Wheel' + (w + 1), g, TOP, V((w * 2 - 1) * 205, -600), V(520, 520), { pivot: CENTER, scale: .78 });
    if (k.wheelInactive[w]) ring.el.style.opacity = .35;
    img('Disc', ring, CENTER, V(0, 0), V(500, 500), [0, 0, 0, .45], { sprite: 'Knob' });
    img('Rim', ring, CENTER, V(0, 0), V(500, 500), [...LINE.slice(0, 3), .5], { sprite: 'Circle' });
    img('Track', ring, CENTER, V(0, 0), V(340, 340), [...LINE.slice(0, 3), .15], { sprite: 'Circle' });
    img('Hub', ring, CENTER, V(0, 0), V(150, 150), [.08, .07, .06, .95], { sprite: 'Knob' });
    img('HubRim', ring, CENTER, V(0, 0), V(150, 150), LINE, { sprite: 'Circle' });
    txt('Numeral', ring, CENTER, V(0, 16), V(140, 50), 38, 'Center', w ? 'II' : 'I', { color: COL.gold, bold: true });
    txt('Title', ring, CENTER, V(0, -26), V(130, 36), 13, 'Center', k.wheelTitles[w], { color: COL.text });
    for (let i = 0; i < k.wheelSize; i++) {
      const a = Math.PI / 2 - i * 2 * Math.PI / k.wheelSize;
      abilityIcon('Slot' + i, ring, CENTER, V(170 * Math.cos(a), 170 * Math.sin(a)), 'Square', 88, k.wheels[w][i] || {});
    }
    if (k.wheelInactive[w]) txt('Inactive' + (w + 1), g, TOP, V((w * 2 - 1) * 205, -600), V(320, 50), 28, 'Center', 'INACTIVE', { pivot: CENTER, color: COL.textDim, bold: true });
  }
  txt('Hint', g, TOP, V(0, -990), V(820, 40), 13, 'Center', 'Drag a spell onto a wheel slot  ·  click a slot or drag it off to remove the spell  ·  LMB / RMB on a spell puts it into wheel I / II', { color: COL.textDim });
}

function buildMerchants(lobby) {
  const m = D.merchant, page = rect('Merchants', lobby, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  const counter = img('Counter', page, TOP, V(-270, -96), V(940, 964), COL.white, { sprite: 'Panel' });
  txt('Title', counter, TOP, V(0, -30), V(460, 36), 26, 'Center', m.title, { color: COL.gold, bold: true });
  txt('Description', counter, TOP, V(0, -68), V(840, 24), 15, 'Center', m.description, { color: COL.textDim });
  ['All', 'Weapons', 'Armor', 'Consumables', 'Utility', 'Treasure'].forEach((f, i) =>
    btn('Filter' + f, counter, TOP, V((i - 2.5) * 140, -104), V(132, 34), f, { color: i === m.filter ? m.filterOn : [.05, .045, .04, .9], textColor: i === m.filter ? COL.gold : COL.white }));
  const vp = rect('Viewport', counter, TOP, V(0, -154), V(80 * 10 + 8 * 9, 774)); vp.el.style.overflow = 'hidden';
  m.wares.forEach((w, i) => {
    const r = img('Ware', vp, TOPLEFT, V(i % 10 * 88, -Math.floor(i / 10) * 88), V(80, 80), w.bg || [.09, .08, .07, .95]);
    r.el.style.boxShadow = `inset 0 0 0 1px ${rgba(D.rarity[w.rarity || 'Common'].frame)}`;
    if (w.hover) r.el.style.boxShadow = `inset 0 0 0 2px ${rgba(COL.gold)}`;
    if (w.icon) img('Icon', r, CENTER, V(0, 0), V(68, 68), COL.white, { src: ICON(w.icon) });
    txt('Price', r, BOTTOMRIGHT, V(-5, 3), V(60, 16), 13, 'BottomRight', String(w.price), { color: [1, .85, .4, 1], bold: true });
    if (w.cant) r.el.style.opacity = .45;
  });
  const purse = img('Purse', page, TOP, V(480, -96), V(520, 964), COL.white, { sprite: 'Panel' });
  txt('Title', purse, TOP, V(0, -34), V(460, 36), 23, 'Center', 'Your Purse', { color: COL.gold });
  img('CoinsIcon', purse, TOP, V(-84, -84), V(64, 64), COL.white, { src: ICON('GoldCoins') });
  txt('Coins', purse, TOP, V(54, -98), V(200, 44), 36, 'MidlineLeft', m.coins);
  txt('Note', purse, TOP, V(0, -160), V(420, 44), 14, 'Top', 'Purchases land in the stash. Coins are taken from the stash first, then from the kit bag.', { color: COL.textDim });
  img('Rule', purse, TOP, V(0, -216), V(420, 2), COL.frame);
  img('DetailsIcon', purse, TOP, V(0, -236), V(150, 150), COL.white, { src: ICON(m.detailsIcon) });
  txt('Details', purse, TOP, V(0, -400), V(420, 420), 16, 'TopLeft', m.details, { color: COL.text });
  txt('Status', purse, BOTTOM, V(0, 44), V(440, 50), 17, 'Center', m.status);
}

// ---------- overlays ----------
function buildWheelHud(root) {
  const w = D.wheel, p = rect('SpellWheel', root, CENTER, V(0, 0), V(500, 500));
  img('Dim', p, CENTER, V(0, 0), V(420, 420), [0, 0, 0, .45], { sprite: 'Knob' });
  txt('Title', p, TOP, V(0, 0), V(400, 30), 20, 'Center', w.title, { pivot: TOP, color: COL.gold });
  img('CenterBack', p, CENTER, V(0, 0), V(100, 100), [.08, .07, .06, .9], { sprite: 'Knob' });
  const slots = rect('Slots', p, CENTER, V(0, 0), V(0, 0));
  w.slots.forEach((s, i) => {
    const a = Math.PI / 2 - i * 2 * Math.PI / w.slots.length, sel = i === w.selected;
    const r = img('Slot', slots, CENTER, V(w.radius * Math.cos(a), w.radius * Math.sin(a)), V(104, 84), sel ? [.6, .45, .15, .95] : [.08, .07, .06, .9], { sprite: 'UISprite' });
    if (sel) r.el.style.boxShadow = `0 0 0 2px ${rgba(COL.gold)}`;
    if (s.icon) img('Icon', r, CENTER, V(0, 20), V(38, 38), COL.white, { src: s.icon });
    txt('Name', r, BOTTOM, V(0, 4), V(100, 44), 12, 'Center', s.name, { pivot: BOTTOM, color: s.charges === 0 ? COL.textDim : COL.white });
    const bar = img('Bar', r, BOTTOM, V(0, -8), V(96, 7), [0, 0, 0, .8]);
    img('Fill', bar, CENTER, V(0, 0), V(94, 5), COL.gold, { sprite: 'UISprite', fill: s.fill });
  });
  img('Cursor', p, CENTER, w.cursor, V(14, 14), COL.gold, { sprite: 'Knob' });
  txt('Center', p, CENTER, V(0, 0), V(200, 40), 16, 'Center', w.center);
}

function buildResult(root, r) {
  const p = rect('Result', root, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  img('Back', p, CENTER, V(0, 0), V(0, 0), [0, 0, 0, .85], { stretch: 0 });
  txt('Title', p, CENTER, V(0, 120), V(1000, 100), 72, 'Center', r.title, { bold: true, color: r.color });
  txt('Details', p, CENTER, V(0, 0), V(900, 140), 24, 'Center', r.details);
  btn('Return', p, CENTER, V(0, -130), V(300, 54), 'Return to the hub');
}

function buildPause(root, settings) {
  const p = rect('Pause', root, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  img('Back', p, CENTER, V(0, 0), V(0, 0), [0, 0, 0, .6], { stretch: 0 });
  if (!settings) {
    const m = img('Main', p, CENTER, V(0, 0), V(360, 220), COL.panel);
    txt('Title', m, TOP, V(0, -36), V(320, 50), 30, 'Center', 'Menu', { color: COL.gold });
    btn('Resume', m, CENTER, V(0, 0), V(260, 48), 'Resume');
    btn('Settings', m, CENTER, V(0, -64), V(260, 48), 'Settings');
  } else {
    const s = img('Settings', p, CENTER, V(0, 0), V(460, 320), COL.panel);
    txt('Title', s, TOP, V(0, -36), V(420, 50), 30, 'Center', 'Settings', { color: COL.gold });
    txt('Volume', s, CENTER, V(0, 66), V(380, 30), 20, 'Left', D.pause.volume, { color: COL.text }); slider('Slider', s, CENTER, V(0, 30), V(380, 24), D.pause.volumeValue);
    txt('Sensitivity', s, CENTER, V(0, -14), V(380, 30), 20, 'Left', D.pause.sensitivity, { color: COL.text }); slider('Slider', s, CENTER, V(0, -50), V(380, 24), D.pause.sensitivityValue);
    btn('Back', s, CENTER, V(0, -118), V(200, 44), 'Back');
  }
}

function buildMap(root) {
  const size = 760, p = rect('Map', root, CENTER, V(0, 0), V(size + 40, size + 90));
  img('Back', p, CENTER, V(0, 0), V(0, 0), COL.panel, { stretch: 0 });
  txt('Title', p, TOP, V(0, -12), V(size, 34), 24, 'Center', D.map.title, { pivot: TOP, color: COL.gold });
  img('Frame', p, CENTER, V(0, -15), V(size + 8, size + 8), COL.frame);
  const map = img('Floor', p, CENTER, V(0, -15), V(size, size), [.9, .85, .7, 1]);
  map.el.style.overflow = 'hidden';
  const tiles = rect('Tiles', map, CENTER, V(0, 0), V(0, 0), { stretch: 0 }); floorTiles(tiles, 7, D.floor); tiles.el.style.filter = 'sepia(.55) brightness(.95)';
  const s = size / 7;
  D.floor.forEach((m, i) => txt('Module' + i, map, TOPLEFT, V(i % 7 * s + 4, -Math.floor(i / 7) * s - s / 2 + 20), V(s - 8, 40), 14, 'Center', D.moduleName(m), { color: [.25, .16, .08, 1], bold: true, pivot: TOPLEFT }));
  const pm = img('PortalMarker', map, TOPLEFT, V(5.5 * s, -1.5 * s), V(24, 24), [.35, .6, 1, 1], { sprite: 'Knob', pivot: CENTER }); pm.el.style.boxShadow = '0 0 0 1px #1a0f08';
  const ar = img('Arrow', map, TOPLEFT, V(3.4 * s, -3.6 * s), V(18, 18), [.95, .75, .2, 1], { sprite: 'Knob', pivot: CENTER });
  img('Tip', ar, CENTER, V(0, 12), V(5, 12), [.95, .75, .2, 1]); ar.el.style.transform = 'rotate(35deg)';
  txt('Hint', p, BOTTOM, V(0, 6), V(size, 20), 13, 'Center', 'M / Esc close', { pivot: BOTTOM, color: COL.textDim });
}

function buildHelp(root) {
  const p = rect('Help', root, TOPLEFT, V(100, -30), V(1100, 150), { pivot: TOPLEFT });
  img('Back', p, CENTER, V(0, 0), V(0, 0), [0, 0, 0, .75], { stretch: 0 });
  txt('Text', p, CENTER, V(0, 0), V(0, 0), 16, 'TopLeft', D.help, { stretch: 12 });
}

// ---------- character select ----------
function characterPage(root, name, title, mainLabel, secondLabel, nameText, o = {}) {
  const page = rect(name, root, CENTER, V(0, 0), V(0, 0), { stretch: 0 });
  if (o.back) img('Background', page, CENTER, V(0, 0), V(0, 0), [.035, .03, .027, 1], { stretch: 0 });
  txt('Title', page, TOPLEFT, V(56, -48), V(600, 60), 34, 'MidlineLeft', title, { color: COL.gold, bold: true });
  img('TitleRule', page, TOPLEFT, V(56, -112), V(380, 2), COL.frame);
  txt('Name', page, TOP, V(0, -150), V(900, 60), 36, 'Center', nameText, { color: COL.text });
  img('NameRule', page, TOP, V(0, -214), V(260, 2), COL.frame);
  btn(mainLabel, page, BOTTOM, V(0, 40), V(360, 86), mainLabel, { color: [.17, .16, .15, .97], fs: 32 });
  btn(secondLabel, page, BOTTOMLEFT, V(56, 40), V(220, 64), secondLabel, { fs: 22 });
  return page;
}
function characterCard(page, name, row, label, on) {
  const c = btn(name, page, TOPLEFT, V(56, -140 - row * 108), V(380, 96), label, { fs: 24, align: 'MidlineLeft', margin: [22, 0, 12, 0], textColor: COL.text, color: on ? [.3, .24, .14, .97] : COL.panelLight });
  if (on) c.el.style.boxShadow = `inset 0 0 0 2px ${rgba(COL.gold)}`;
  return c;
}

// ---------- screen list ----------
const SCREENS = [
  { group: 'Выбор персонажа', id: 'C1', name: 'Загрузка', meta: 'CharacterSelectScene · <code>LoadingView</code> · BuildLoading — чёрный экран с первого кадра',
    build: r => buildLoading(r, D.loadingTitles.select, D.loadingStatus[0], .35) },
  { group: 'Выбор персонажа', id: 'C2', name: 'Выбор персонажа', meta: 'CharacterSelectScene · <code>CharacterSelectView</code> · фон = рендер таверны (3D, здесь заглушка) + Vignette',
    build: r => {
      const bd = img('Backdrop', r, CENTER, V(0, 0), V(0, 0), COL.black, { stretch: 0 }); bd.el.style.background = 'radial-gradient(ellipse at 52% 48%, #5a3b1c 0%, #22160c 35%, #050403 75%)';
      dollPlaceholder(bd, 'Character', CENTER, V(40, -60), V(420, 760)).el.style.background = 'none';
      img('Vignette', r, CENTER, V(0, 0), V(0, 0), COL.white, { stretch: 0, sprite: 'Vignette' }).el.style.background = 'radial-gradient(ellipse at center, transparent 45%, #000 100%)';
      const p = characterPage(r, 'Select', 'Select Character', 'Enter Hub', 'Delete', D.select.name);
      D.select.slots.forEach((t, i) => characterCard(p, 'Slot' + i, i, t, i === D.select.selected));
    } },
  { group: 'Выбор персонажа', id: 'C3', name: 'Создание персонажа', meta: 'CharacterSelectScene · <code>CharacterCreateView</code> · классы слева, гексаграмма класса в центре, описание справа',
    build: r => {
      const p = characterPage(r, 'Create', 'Create Character', 'Create', 'Back', D.create.name, { back: true });
      D.classes.forEach((c, i) => characterCard(p, 'Class' + c.name, i, c.name, i === D.create.selected));
      const sheet = img('Stats', p, TOP, V(0, -236), V(520, 590), COL.white, { sprite: 'Panel' });
      buildStats(sheet, V(0, -24), 520, 115, true);
      const info = rect('Info', p, TOPRIGHT, V(-56, -140), V(520, 300));
      img('Frame', info, CENTER, V(0, 0), V(0, 0), COL.frame, { stretch: 0 });
      img('Back', info, CENTER, V(0, 0), V(0, 0), COL.panel, { stretch: 2 });
      txt('Text', info, CENTER, V(0, 0), V(0, 0), 19, 'TopLeft', D.create.description, { stretch: 22, color: COL.text });
      input('Nickname', p, BOTTOM, V(0, 150), V(360, 56), 'Nickname', { color: COL.panelLight, value: D.create.nickname, fs: 24 });
    } },

  { group: 'Хаб «Устье»', id: 'H1', name: 'HUD в хабе + подсказка станции', world: true, meta: 'HubScene · <code>DungeonHudView</code> без Minimap/Timer/Swarm/Floor · <code>HubUiRoot</code>: StationPrompt, Notice',
    build: r => {
      buildHud(r, { hub: true });
      txt('StationPrompt', r, CENTER, V(0, -110), V(700, 30), 20, 'Center', D.hub.prompt, { color: COL.text });
      txt('Notice', r, TOP, V(0, -120), V(900, 30), 18, 'Center', D.hub.notice, { color: [1, .78, .45, 1] });
    } },
  { group: 'Хаб «Устье»', id: 'H2', name: 'Доска отряда', meta: 'HubScene · <code>HubPagesView</code> → <code>HubBoardView</code> (режим Party)',
    build: r => hubPages(r, D.titles.board, p => buildBoard(p, 'party')) },
  { group: 'Хаб «Устье»', id: 'H3', name: 'Книга спусков', meta: 'HubScene · <code>HubBoardView</code> (режим Descent): карта, очередь, Start, dev-кнопки',
    build: r => hubPages(r, D.titles.descent, p => buildBoard(p, 'descent')) },
  { group: 'Хаб «Устье»', id: 'H4', name: 'Перки и навыки', meta: 'HubScene · <code>SkillsView</code> · лист атрибутов, кукла в арке, пул класса',
    build: r => hubPages(r, D.titles.skills, p => buildSkills(p, 'perks')) },
  { group: 'Хаб «Устье»', id: 'H5', name: 'Колёса заклинаний', meta: 'HubScene · <code>SpellsView</code> — вкладка Spell Wheels страницы навыков (класс с заклинаниями)',
    build: r => hubPages(r, D.titles.skills, p => buildSkills(p, 'wheels')) },
  { group: 'Хаб «Устье»', id: 'H6', name: 'Снаряжение и сундук', meta: 'HubScene · <code>InventoryView</code> (LayoutLobbyStash) + <code>StashPagesView</code>',
    build: r => hubPages(r, D.titles.kit, p => buildInventory(p, false, D.kit)) },
  { group: 'Хаб «Устье»', id: 'H7', name: 'Торговец', meta: 'HubScene · <code>MerchantsView</code> — один торговец на страницу',
    build: r => hubPages(r, D.titles.merchant, p => buildMerchants(p)) },

  { group: 'Подземелье', id: 'D1', name: 'Загрузка', meta: 'DungeonScene · <code>LoadingView</code>',
    build: r => buildLoading(r, D.loadingTitles.dungeon, D.loadingStatus[1], .72) },
  { group: 'Подземелье', id: 'D2', name: 'HUD', world: true, meta: 'DungeonScene · <code>DungeonHudView</code> · компас, здоровье, Q/E, пояс, оружие, миникарта, таймер, рой',
    build: r => buildHud(r, { prompt: true, swarm: true, readied: true }) },
  { group: 'Подземелье', id: 'D3', name: 'HUD — бой', world: true, meta: 'Тот же HUD: каст, полоса босса, прицел дальнего оружия, красная виньетка урона, кулдауны',
    build: r => buildHud(r, { cast: true, boss: true, aim: true, hurt: true, swing: true }) },
  { group: 'Подземелье', id: 'D4', name: 'Инвентарь + контейнер', meta: 'DungeonScene · <code>InventoryView</code> на весь экран (Tab), справа открытый контейнер, тултип предмета',
    build: r => { const w = img('World', r, CENTER, V(0, 0), V(0, 0), COL.black, { stretch: 0 }); w.el.className += ' world'; buildInventory(r, true, D.inv); } },
  { group: 'Подземелье', id: 'D5', name: 'Карта этажа', world: true, meta: 'DungeonScene · <code>MapView</code> (M)',
    build: r => buildMap(r) },
  { group: 'Подземелье', id: 'D6', name: 'Итог — выбрался', world: true, meta: 'DungeonScene · <code>ResultView</code>',
    build: r => buildResult(r, D.result.escaped) },
  { group: 'Подземелье', id: 'D7', name: 'Итог — погиб', world: true, meta: 'DungeonScene · <code>ResultView</code>',
    build: r => buildResult(r, D.result.died) },

  { group: 'Общие оверлеи', id: 'O1', name: 'Колесо заклинаний', world: true, meta: 'Hub + Dungeon · <code>SpellWheelView</code> — удержание Q/E у класса с заклинаниями',
    build: r => { buildHud(r, { hub: true }); buildWheelHud(r); } },
  { group: 'Общие оверлеи', id: 'O2', name: 'Меню паузы', world: true, meta: 'Hub + Dungeon · <code>PauseMenuView</code> (Esc)',
    build: r => buildPause(r, false) },
  { group: 'Общие оверлеи', id: 'O3', name: 'Настройки', world: true, meta: 'Hub + Dungeon · <code>PauseMenuView</code> → Settings',
    build: r => buildPause(r, true) },
  { group: 'Общие оверлеи', id: 'O4', name: 'Подсказка управления', world: true, meta: 'Hub + Dungeon · <code>HelpView</code> (H)',
    build: r => { buildHud(r, {}); buildHelp(r); } },

  { group: 'Служебные (dev)', id: 'X1', name: 'Dungeon Test · F2', world: true, meta: 'Тестовый матч · <code>DungeonTestView</code> (OnGUI, GUILayout.Window Rect(20,120,260,600)) + <code>FpsCounter</code> (только в редакторе)',
    build: r => {
      buildHud(r, {});
      const w = img('Window', r, TOPLEFT, V(20, -120), V(260, 600), [.2, .2, .2, .92], { sprite: 'UISprite', pivot: TOPLEFT });
      txt('Title', w, TOP, V(0, -4), V(260, 20), 13, 'Center', 'Dungeon Test · F2', { pivot: TOP, color: [.85, .85, .85, 1] });
      ['Skeleton Warrior', 'Skeleton Swordsman', 'Skeleton Archer', 'Skeleton Crossbowman', 'Bone Spider', 'Flying Head', 'Iron Juggernaut', 'Clear monsters'].forEach((n, i) =>
        btn(n, w, TOPLEFT, V(8, -30 - i * 24), V(244, 21), n, { pivot: TOPLEFT, color: [.33, .33, .33, 1], fs: 12, textColor: [.9, .9, .9, 1] }));
      txt('Fps', r, TOPLEFT, V(10, -10), V(300, 50), 27, 'TopLeft', '<color=#4f4>72 FPS  13.9 ms</color>', { pivot: TOPLEFT });
    } },
];