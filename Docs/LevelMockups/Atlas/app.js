// Экраны атласа: галерея модулей, макет модуля, генерация подземелья. Адрес: #room/Crypt, #dungeon/12345.
const $ = id => document.getElementById(id);
const M = ATLAS.manifest;
const ENTRIES = M.rooms;
const GAME_POOL = ENTRIES.filter(r => r.pool && r.name !== M.key).map(r => r.name);
const OBJECT_NAMES = {
  Floor: 'Пол', Wall: 'Стена', 'Wall Column': 'Колонна', 'Wall Upper': 'Верх стены', Ceiling: 'Потолок', Platform: 'Платформа', Stairs: 'Лестница', 'Pit Wall': 'Стенка ямы', Bars: 'Решётка',
  Sword: 'Мечник', Archer: 'Лучник', Crossbow: 'Арбалетчик', Warrior: 'Воин', Mage: 'Маг', Skull: 'Череп', Barrel: 'Бочка', Coffin: 'Гроб', Crate: 'Ящик', GoldenChest: 'Золотой сундук',
  LargeOakChest: 'Большой сундук', SmallOakChest: 'Малый сундук', Urn: 'Урна', GoldPile: 'Куча золота', Spike: 'Шипы', Blade: 'Маятник', Vent: 'Пыльный продув', Collapse: 'Обрушение',
  ArrowGallery: 'Стрельчатая галерея', HallMachinery: 'Механизм зала', AcidMoat: 'Кислотный ров', BridgeLever: 'Рычаг моста', SecretLever: 'Рычаг тайника', SecretDoor: 'Потайная плита', Start: 'Старт', Spot: 'Место старта', Torch: 'Факел', Candle: 'Свечи',
  Brazier: 'Жаровня', Fire: 'Костёр', Acid: 'Кислотный свет', Shaft: 'Луч из пролома'
};
const VERDICTS = { ok: '✔ Готов', rework: '✎ Правка', no: '✖ Нет' };
const LEVEL_MIN_AREA = 25;
const CLEAR_HEIGHT = 2.4;
const DEFAULT_CUT = 2;
const SEED_LIMIT = 2147483647;

const title = name => (ATLAS.titles[name] || {}).ru || name;
const fmt = v => (+v).toFixed(2).replace(/\.?0+$/, '') || '0';
const hidden = new Set(['ceiling', 'upper', 'door']);
const state = { screen: 'rooms', room: null, view: 'plan', cut: DEFAULT_CUT, filter: 'all', find: '', selected: null, seed: 1, loops: 0.25, pool: new Set(GAME_POOL), plan: null, placed: null, items: [], cell: null, pan: null };
let notes = {};
try { notes = JSON.parse(localStorage.getItem('atlas-notes') || '{}'); } catch (e) { notes = {}; }
const saveNotes = () => { try { localStorage.setItem('atlas-notes', JSON.stringify(notes)); } catch (e) { /* без хранилища живём до перезагрузки */ } };
const noteOf = name => notes[name] || (notes[name] = { status: '', note: '', objs: {} });
let stage = null;

// --- Загрузка данных комнат ---
function loadRooms() {
  return Promise.all(ENTRIES.map(r => new Promise((done, fail) => {
    const s = document.createElement('script'); s.src = `data/${r.name}.js`; s.onload = done; s.onerror = () => fail(new Error(r.name)); document.head.appendChild(s);
  })));
}

const sideObjs = doorways => M.sides[doorways].map(o => ({ ...o, cat: 'shell' }));
const postObjs = () => M.post.map(o => ({ ...o, cat: 'shell' }));

// Один модуль как в игре: комната + её стороны и столбы. Обычная комната — со всеми проёмами, угловая — с глухими югом и западом.
function moduleItems(name) {
  const entry = ENTRIES.find(r => r.name === name), half = M.pitch / 2, items = [{ objs: ATLAS.rooms[name], x: 0, z: 0, yaw: 0 }];
  if (name === M.key) return items;
  const blind = entry.corner ? 0 : 3;
  items.push({ objs: sideObjs(3), x: half, z: 0, yaw: -90 }, { objs: sideObjs(3), x: 0, z: half, yaw: 0 }, { objs: sideObjs(blind), x: -half, z: 0, yaw: -90 }, { objs: sideObjs(blind), x: 0, z: -half, yaw: 0 });
  [[1, 1], [1, -1], [-1, 1], [-1, -1]].forEach(([x, z]) => items.push({ objs: postObjs(), x: x * half, z: z * half, yaw: 0 }));
  return items;
}

function boundsOf(items) {
  const b = { x0: 1e9, x1: -1e9, z0: 1e9, z1: -1e9, y0: 1e9, y1: -1e9 };
  items.forEach(item => item.objs.forEach(o => {
    if (o.cat !== 'ceiling') { b.y0 = Math.min(b.y0, o.p[1]); b.y1 = Math.max(b.y1, o.p[1] + (o.s ? o.s[1] : 0)); }
    if (!o.s) return;
    planCorners(o, item).forEach(([x, z]) => { b.x0 = Math.min(b.x0, x); b.x1 = Math.max(b.x1, x); b.z0 = Math.min(b.z0, z); b.z1 = Math.max(b.z1, z); });
  }));
  return b;
}

function counts(name) {
  const c = { monster: 0, chest: 0, trap: 0, light: 0 };
  ATLAS.rooms[name].forEach(o => { if (o.cat in c) c[o.cat]++; });
  return c;
}

// --- Галерея ---
function renderFilters() {
  const list = [['all', 'Все'], ['live', 'В игре'], ['corner', 'Угловые'], ['rework', 'На правку'], ['ok', 'Готовы'], ['none', 'Без оценки']];
  $('filters').innerHTML = `<input id="find" type="search" placeholder="Поиск модуля" value="${state.find}" aria-label="Поиск модуля">` + list.map(([k, l]) => `<button data-f="${k}" aria-pressed="${state.filter === k}">${l}</button>`).join('') + `<span class="n" id="shown"></span>`;
  $('find').oninput = e => { state.find = e.target.value; renderCards(); };
  document.querySelectorAll('[data-f]').forEach(b => { b.onclick = () => { state.filter = b.dataset.f; renderFilters(); renderCards(); }; });
}

function matches(r) {
  const status = noteOf(r.name).status, q = state.find.trim().toLowerCase();
  if (q && !`${title(r.name)} ${r.title}`.toLowerCase().includes(q)) return false;
  return state.filter === 'all' || (state.filter === 'live' && r.pool) || (state.filter === 'corner' && r.corner) || (state.filter === 'none' && !status) || state.filter === status;
}

function renderCards() {
  const box = $('cards'); box.innerHTML = '';
  const shown = ENTRIES.filter(matches);
  $('shown').textContent = `${shown.length} из ${ENTRIES.length}`;
  shown.forEach(r => {
    const n = noteOf(r.name), c = counts(r.name), b = document.createElement('button'); b.className = 'card';
    const hasNote = n.note || Object.keys(n.objs).length;
    const tags = [r.pool ? '<span class="tag live">в игре</span>' : '', r.name === M.key ? '<span class="tag">ключевая · 1×2</span>' : '', r.corner ? '<span class="tag">угловая</span>' : '',
      n.status ? `<span class="tag ${n.status}">${VERDICTS[n.status]}</span>` : '', hasNote ? '<span class="tag note">заметки</span>' : ''].join('');
    b.innerHTML = `<canvas width="472" height="472"></canvas><span class="body"><b>${title(r.name)}</b><span class="en">${r.name}</span><span class="tags">${tags}</span><span class="stats">☠ ${c.monster}  ▣ ${c.chest}  ▲ ${c.trap}  ☼ ${c.light}</span></span>`;
    b.onclick = () => go(`room/${r.name}`);
    box.appendChild(b);
    drawThumb(b.querySelector('canvas'), r.name);
  });
}

function drawThumb(canvas, name) {
  const items = moduleItems(name), b = boundsOf(items), size = Math.max(b.x1 - b.x0, b.z1 - b.z0) + 1, m = canvas.width / size;
  const cx = (b.x0 + b.x1) / 2, cz = (b.z0 + b.z1) / 2, ctx = canvas.getContext('2d');
  drawPlan(ctx, items, { cut: DEFAULT_CUT, hidden: new Set(['ceiling', 'upper', 'door']), m, toScreen: (x, z) => [(x - cx) * m + canvas.width / 2, (cz - z) * m + canvas.height / 2] });
}

// --- Сцена: общее для модуля и подземелья ---
function ensureStage() {
  if (stage) return;
  stage = new Stage($('view'));
  stage.onPick = hit => {
    if (state.screen === 'room') select(hit && ATLAS.rooms[state.room].includes(hit.o) ? hit.o : null);
    else if (hit) selectCell(hit.place.bucket);
  };
  stage.onWalk = p => { $('cur').innerHTML = `<b>x ${p.x.toFixed(1)}  z ${(-p.z).toFixed(1)}  ноги ${(p.y - 1.7).toFixed(2)} м</b>`; };
  stage.onFrame = placeLabels;
  stage.setExposure(1);
}

function renderLayers() {
  const present = {};
  state.items.forEach(i => i.objs.forEach(o => { present[o.cat] = (present[o.cat] || 0) + 1; }));
  $('layers').innerHTML = '';
  Object.entries(CATS).forEach(([k, c]) => {
    if (!present[k]) return;
    const l = document.createElement('label');
    l.innerHTML = `<input type="checkbox" ${hidden.has(k) ? '' : 'checked'}><span class="sw" style="background:${c.color}"></span><span>${c.label}</span><span class="n">${present[k]}</span>`;
    l.querySelector('input').onchange = e => { if (e.target.checked) hidden.delete(k); else hidden.add(k); applyLayers(); };
    $('layers').appendChild(l);
  });
}

function applyLayers() { stage.setLayers(hidden); if (state.view === 'plan') drawMainPlan(); }

function setupCut(items) {
  const b = boundsOf(items), area = {};
  items.forEach(item => item.objs.forEach(o => {
    if ((o.cat === 'floor' || o.cat === 'platform') && o.s) { const top = (o.p[1] + o.s[1]).toFixed(1); area[top] = (area[top] || 0) + o.s[0] * o.s[2]; }
  }));
  const tops = Object.keys(area).filter(t => area[t] >= LEVEL_MIN_AREA).map(Number).sort((x, y) => x - y).slice(0, 7);
  state.cutMax = Math.ceil(b.y1); state.cutMin = Math.floor(b.y0);
  $('cut').min = state.cutMin; $('cut').max = state.cutMax;
  $('levels').innerHTML = '';
  tops.forEach(t => { const u = document.createElement('button'); u.textContent = `${fmt(t)} м`; u.onclick = () => setCut(t + DEFAULT_CUT); $('levels').appendChild(u); });
  const all = document.createElement('button'); all.textContent = 'всё'; all.onclick = () => setCut(state.cutMax); $('levels').appendChild(all);
}

function setCut(v) {
  state.cut = +v; $('cut').value = v; $('cutV').textContent = +v >= state.cutMax ? 'всё' : `${(+v).toFixed(1)} м`;
  stage.setCut(+v);
  if (state.view === 'plan') drawMainPlan();
}

function setView(v) {
  state.view = v;
  const isPlan = v === 'plan';
  $('plan').hidden = !isPlan; $('view').hidden = isPlan; $('lightBox').hidden = isPlan; $('labels').hidden = isPlan;
  document.querySelectorAll('#views button').forEach(b => b.setAttribute('aria-pressed', b.dataset.v === v));
  stage.resize();
  setCut(isPlan ? Math.min(DEFAULT_CUT, state.cutMax) : state.cutMax);
  const b = state.bounds, center = [(b.x0 + b.x1) / 2, (b.z0 + b.z1) / 2], size = Math.max(b.x1 - b.x0, b.z1 - b.z0);
  if (isPlan) { state.pan = null; drawMainPlan(); }
  else if (v === '3d') stage.frame(center, size);
  else if (v === 'top') stage.top(center, size);
  else if (v === 'walk') walk();
  if (v !== 'walk') $('cur').textContent = '';
  $('hint').textContent = isPlan ? 'Колесо — зум · перетаскивание — сдвиг · клик — выбрать' : v === 'walk' ? 'Мышь с зажатой кнопкой — обзор · WASD — идти · Shift — бег' : 'ЛКМ — выбрать · правая — орбита · средняя/колесо — зум';
}

function walk() {
  const r = state.screen === 'dungeon' ? cellRoom(state.cell ?? state.plan.keyCells[0]) : null;
  const item = r ? { objs: ATLAS.rooms[r.isKey ? M.key : r.name], x: r.x, z: r.z, yaw: r.yaw } : state.items[0];
  const start = item.objs.find(o => o.cat === 'start' && o.name === 'Start');
  if (start) {
    const c = planCorners({ ...start, s: [0, 0, 0] }, item)[0];
    stage.walk(c[0], start.p[1], c[1], 0);
  } else stage.walk(item.x, 8, item.z, 0);
}

function buildViews() {
  $('views').innerHTML = [['plan', 'План'], ['3d', '3D'], ['top', 'Сверху'], ['walk', 'Ходить']].map(([k, l]) => `<button data-v="${k}">${l}</button>`).join('');
  $('views').className = 'seg';
  document.querySelectorAll('#views button').forEach(b => { b.onclick = () => setView(b.dataset.v); });
}

// --- 2D-план на весь экран ---
function fitPlan() {
  const c = $('plan'), b = state.bounds, w = c.clientWidth, h = c.clientHeight;
  const left = innerWidth > 760 ? 280 : 0, right = innerWidth > 760 ? 316 : 0;
  const m = Math.min((w - left - right - 24) / (b.x1 - b.x0), (h - 60) / (b.z1 - b.z0));
  return { m, ox: left + (w - left - right) / 2 - (b.x0 + b.x1) / 2 * m, oy: (h - 28) / 2 + (b.z0 + b.z1) / 2 * m };
}

function drawMainPlan() {
  const c = $('plan');
  if (c.hidden) return;
  const ratio = Math.min(devicePixelRatio, 2), w = c.clientWidth, h = c.clientHeight;
  c.width = w * ratio; c.height = h * ratio;
  const ctx = c.getContext('2d'); ctx.scale(ratio, ratio);
  state.pan = state.pan || fitPlan();
  const { m, ox, oy } = state.pan, toScreen = (x, z) => [x * m + ox, oy - z * m];
  ctx.fillStyle = '#0a0d0c'; ctx.fillRect(0, 0, w, h);
  drawPlan(ctx, state.items, { cut: state.cut, hidden, toScreen, m });
  if (state.screen === 'dungeon') drawCells(ctx, toScreen, m);
  else if (state.selected) {
    const [a, b, c2, d] = planCorners(state.selected, state.items[0]).map(p => toScreen(p[0], p[1]));
    ctx.strokeStyle = '#7dff8f'; ctx.lineWidth = 2; ctx.beginPath(); ctx.moveTo(...a); ctx.lineTo(...b); ctx.lineTo(...c2); ctx.lineTo(...d); ctx.closePath(); ctx.stroke();
  }
}

function cellRoom(cell) { return state.placed.rooms.find(r => r.cell === cell) || state.placed.rooms.find(r => r.isKey); }

function cellCenter(cell) {
  const grid = state.plan.grid, off = (grid - 1) / 2;
  return [(cell % grid - off) * M.pitch, (Math.floor(cell / grid) - off) * M.pitch];
}

// Подпись клетки, рамка выбранной и зелёная метка клеток, где может начаться забег.
function drawCells(ctx, toScreen, m) {
  const half = M.pitch / 2;
  ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
  state.placed.rooms.forEach(r => {
    const [x, y] = toScreen(r.x, r.z), name = r.isKey ? M.key : r.name;
    ctx.font = `600 ${Math.max(11, Math.min(15, m * 2.2))}px "Barlow Condensed", sans-serif`;
    const text = title(name), w = ctx.measureText(text).width + 8;
    ctx.fillStyle = '#0d1110cc'; ctx.fillRect(x - w / 2, y - 9, w, 18);
    ctx.fillStyle = r.isKey ? '#ffd76b' : '#d9e2dc'; ctx.fillText(text, x, y);
    if (!r.isKey && r.start) { ctx.strokeStyle = '#4dd0ff55'; ctx.lineWidth = 2; ctx.strokeRect(x - half * m + 3, y - half * m + 3, M.pitch * m - 6, M.pitch * m - 6); }
  });
  if (state.cell !== null) {
    const cells = state.plan.keyCells.includes(state.cell) ? state.plan.keyCells : [state.cell];
    ctx.strokeStyle = '#7dff8f'; ctx.lineWidth = 2;
    cells.forEach(cell => { const [cx, cz] = cellCenter(cell), [x, y] = toScreen(cx, cz); ctx.strokeRect(x - half * m, y - half * m, M.pitch * m, M.pitch * m); });
  }
}

function bindPlan() {
  const c = $('plan');
  let drag = null;
  c.addEventListener('wheel', e => {
    e.preventDefault();
    const k = e.deltaY < 0 ? 1.15 : 0.87, p = state.pan, r = c.getBoundingClientRect(), x = e.clientX - r.left, y = e.clientY - r.top;
    p.ox = x - (x - p.ox) * k; p.oy = y - (y - p.oy) * k; p.m *= k; drawMainPlan();
  }, { passive: false });
  c.addEventListener('pointerdown', e => { drag = { x: e.clientX, y: e.clientY, moved: 0 }; c.setPointerCapture(e.pointerId); });
  c.addEventListener('pointermove', e => {
    if (!drag) return;
    const dx = e.clientX - drag.x, dy = e.clientY - drag.y;
    drag.moved += Math.abs(dx) + Math.abs(dy); drag.x = e.clientX; drag.y = e.clientY;
    state.pan.ox += dx; state.pan.oy += dy; drawMainPlan();
  });
  c.addEventListener('pointerup', e => {
    const wasClick = drag && drag.moved < 5; drag = null;
    if (!wasClick) return;
    const r = c.getBoundingClientRect(), { m, ox, oy } = state.pan, x = (e.clientX - r.left - ox) / m, z = (oy - (e.clientY - r.top)) / m;
    if (state.screen === 'dungeon') planClickCell(x, z); else planClickObject(x, z);
  });
}

function planClickCell(x, z) {
  const grid = state.plan.grid, cx = Math.floor(x / M.pitch + grid / 2), cz = Math.floor(z / M.pitch + grid / 2);
  if (cx >= 0 && cx < grid && cz >= 0 && cz < grid) selectCell(cz * grid + cx);
}

// Верхний объект под кликом ниже среза: маркеры важнее пола.
function planClickObject(x, z) {
  const inside = o => {
    if (!o.s) return Math.hypot(o.p[0] - x, o.p[2] - z) < 0.5;
    const a = -(o.ry || 0) * DEG, c = Math.cos(a), s = Math.sin(a), dx = x - o.p[0], dz = z - o.p[2];
    return Math.abs(dx * c + dz * s) <= Math.max(o.s[0], 0.6) / 2 && Math.abs(-dx * s + dz * c) <= Math.max(o.s[2], 0.6) / 2;
  };
  const list = ATLAS.rooms[state.room].filter(o => !hidden.has(o.cat) && o.cat !== 'ceiling' && o.p[1] <= state.cut && inside(o));
  list.sort((a, b) => (CATS[b.cat].ghost ? 1 : 0) - (CATS[a.cat].ghost ? 1 : 0) || (b.p[1] + (b.s ? b.s[1] : 0)) - (a.p[1] + (a.s ? a.s[1] : 0)));
  select(list[0] || null);
}

// --- Экран модуля ---
function openRoom(name) {
  ensureStage();
  state.screen = 'room'; state.room = name; state.selected = null; state.pan = null;
  state.items = moduleItems(name); state.bounds = boundsOf(state.items);
  stage.load(state.items); stage.setLayers(hidden);
  ['genBox', 'statBox'].forEach(id => { $(id).hidden = true; });
  ['roomBox', 'objBox', 'labelsBox'].forEach(id => { $(id).hidden = false; });
  const e = ENTRIES.find(r => r.name === name), n = noteOf(name), c = counts(name);
  $('roomTitle').textContent = title(name);
  $('roomSub').textContent = `${e.pool ? 'в игре' : 'префаб не в пуле'}${e.corner ? ' · угловая' : ''}${name === M.key ? ' · ключевая' : ` · вес ${e.weight}${e.limit ? `, до ${e.limit} шт.` : ''}`} · ☠ ${c.monster} ▣ ${c.chest} ▲ ${c.trap}`;
  $('verdict').innerHTML = Object.entries(VERDICTS).map(([k, l]) => `<button data-s="${k}" aria-pressed="${n.status === k}">${l}</button>`).join('');
  document.querySelectorAll('#verdict button').forEach(b => { b.onclick = () => { n.status = n.status === b.dataset.s ? '' : b.dataset.s; saveNotes(); openVerdict(n); }; });
  $('roomNote').value = n.note;
  $('roomNote').oninput = e2 => { n.note = e2.target.value; saveNotes(); };
  $('crumb').textContent = name;
  setupCut(state.items); renderLayers(); renderInfo(); renderList();
  $('gallery').hidden = true; $('stage').hidden = false; $('prev').hidden = $('next').hidden = false;
  tabs(); stage.resize();
  buildLabels(); setView(state.view);
}

function openVerdict(n) { document.querySelectorAll('#verdict button').forEach(b => b.setAttribute('aria-pressed', n.status === b.dataset.s)); }

function select(o) {
  state.selected = o;
  stage.select(o, state.items[0]);
  renderInfo(); renderList();
  if (state.view === 'plan') drawMainPlan();
}

function renderInfo() {
  const o = state.selected, info = $('info');
  if (state.screen !== 'room') return;
  if (!o) { info.innerHTML = `<h2>Описание</h2><div class="sub" style="margin:0">${(ATLAS.titles[state.room] || {}).desc || 'Кликни объект, чтобы увидеть размеры и написать правку.'}</div>`; return; }
  const n = noteOf(state.room), s = o.s;
  info.innerHTML = `<h2>${CATS[o.cat].label} · ${o.id}</h2><div class="big">${OBJECT_NAMES[o.name] || o.name}</div><div class="sub"></div><dl><dt>Позиция</dt><dd>${o.p.map(fmt).join(', ')}</dd>${s ? `<dt>Размер</dt><dd>${s.map(fmt).join(' × ')}</dd><dt>Верх</dt><dd>${fmt(o.p[1] + s[1])} м</dd>` : `<dt>Цвет</dt><dd>${o.color}</dd><dt>Сила</dt><dd>${o.i}</dd><dt>Радиус</dt><dd>${o.r} м</dd>`}${o.ry ? `<dt>Поворот</dt><dd>${fmt(o.ry)}°</dd>` : ''}</dl><textarea id="objNote" placeholder="Что поменять в этом объекте"></textarea>`;
  const t = $('objNote'); t.value = n.objs[o.id] || '';
  t.oninput = () => { if (t.value.trim()) n.objs[o.id] = t.value; else delete n.objs[o.id]; saveNotes(); renderList(); };
}

function renderList() {
  if (state.screen !== 'room') return;
  const q = $('objFind').value.trim().toLowerCase(), n = noteOf(state.room), order = Object.keys(CATS);
  const list = ATLAS.rooms[state.room].filter(o => o.cat !== 'start' || o.name !== 'Spot').slice().sort((a, b) => order.indexOf(b.cat) - order.indexOf(a.cat));
  const show = list.filter(o => !q || `${o.id} ${OBJECT_NAMES[o.name] || o.name}`.toLowerCase().includes(q));
  $('list').innerHTML = '';
  show.forEach(o => {
    const b = document.createElement('button'); b.setAttribute('aria-pressed', state.selected === o);
    b.innerHTML = `<span class="id">${o.id}</span><span>${OBJECT_NAMES[o.name] || o.name}</span>${n.objs[o.id] ? '<span class="dot"></span>' : ''}`;
    b.onclick = () => { select(o); if (state.view !== 'plan') stage.focus(o, state.items[0]); };
    $('list').appendChild(b);
  });
}

// Подписи маркеров поверх 3D.
let labelEls = [];
function buildLabels() {
  $('labels').innerHTML = ''; labelEls = [];
  ATLAS.rooms[state.room].forEach(o => {
    if (!['trap', 'chest', 'monster'].includes(o.cat) && !(o.cat === 'start' && o.name === 'Start')) return;
    const d = document.createElement('div'); d.className = `lbl ${o.cat}`; d.textContent = OBJECT_NAMES[o.name] || o.name;
    $('labels').appendChild(d);
    const y = o.p[1] + (o.s ? o.s[1] : 0) + 0.3;
    labelEls.push({ d, o, v: new THREE.Vector3(o.p[0], y, -o.p[2]) });
  });
}

const projected = new THREE.Vector3();
function placeLabels() {
  const on = state.screen === 'room' && $('showLabels').checked && state.view !== 'plan', w = $('stage').clientWidth, h = $('stage').clientHeight;
  labelEls.forEach(({ d, o, v }) => {
    projected.copy(v).project(stage.camera);
    const seen = on && !hidden.has(o.cat) && o.p[1] <= state.cut && projected.z < 1;
    d.style.display = seen ? 'block' : 'none';
    if (seen) d.style.transform = `translate(calc(-50% + ${(projected.x + 1) / 2 * w}px), calc(-100% + ${(1 - projected.y) / 2 * h}px))`;
  });
}

// --- Экран подземелья ---
function kindsOf() {
  return ENTRIES.filter(r => r.name !== M.key && state.pool.has(r.name)).map(r => ({ name: r.name, weight: r.pool ? 1 : r.weight, limit: r.pool ? 0 : r.limit, corner: r.corner }));
}

function generate() {
  const kinds = kindsOf();
  if (!kinds.length) { $('warn').hidden = false; $('warn').textContent = 'Пул пуст: отметь хотя бы одну комнату.'; return; }
  const plan = state.plan = Generator.lay(state.seed, { loops: state.loops, kinds });
  const placed = state.placed = Generator.place(plan, M);
  placed.rooms.forEach(r => { r.start = !r.isKey && Generator.isNearKey(plan, r, M.pitch) && ATLAS.rooms[r.name].some(o => o.cat === 'start'); });
  const reach = M.pitch * 1.3, items = [];
  placed.rooms.forEach(r => items.push({ objs: ATLAS.rooms[r.isKey ? M.key : r.name], x: r.x, z: r.z, yaw: r.yaw, bucket: `c${r.cell}`, reach: r.isKey ? M.pitch * 2.6 : reach }));
  placed.sides.forEach(s => items.push({ objs: sideObjs(s.doorways), x: s.x, z: s.z, yaw: s.yaw, bucket: `c${s.cell}`, reach }));
  placed.posts.forEach(p => items.push({ objs: postObjs(), x: p.x, z: p.z, yaw: 0, bucket: `c${p.cell}`, reach }));
  placed.doors.forEach(d => items.push({ objs: [{ id: 'Do', cat: 'door', name: 'Дверь', p: [0, 0, 0], ry: 0, s: [M.doorWidth, M.doorHeight, 0.12] }], x: d.x, z: d.z, yaw: d.yaw, bucket: `c${d.cell}`, reach }));
  state.items = items; state.bounds = boundsOf(items); state.cell = null;
  stage.load(items); stage.setLayers(hidden);
  setupCut(items); renderLayers(); renderStats();
  const hasCorners = kinds.some(k => k.corner);
  $('warn').hidden = hasCorners;
  $('warn').textContent = 'В пуле нет угловых комнат: как и в игре, углы занимают обычные.';
  history.replaceState(null, '', `#dungeon/${state.seed}`);
  $('crumb').textContent = `сид ${state.seed}`;
}

function openDungeon(seed) {
  ensureStage();
  state.screen = 'dungeon'; state.room = null; state.selected = null; state.pan = null;
  if (seed !== undefined) state.seed = seed;
  ['roomBox', 'objBox', 'labelsBox'].forEach(id => { $(id).hidden = true; });
  ['genBox', 'statBox'].forEach(id => { $(id).hidden = false; });
  $('gallery').hidden = true; $('stage').hidden = false; $('prev').hidden = $('next').hidden = true;
  $('seed').value = state.seed; $('loops').value = state.loops; $('loopsV').textContent = state.loops.toFixed(2);
  renderPool(); tabs(); stage.resize(); generate();
  $('labels').innerHTML = ''; labelEls = [];
  renderCell(); setView(state.view);
}

function renderPool() {
  $('pool').innerHTML = '';
  ENTRIES.filter(r => r.name !== M.key).forEach(r => {
    const l = document.createElement('label');
    l.innerHTML = `<input type="checkbox" ${state.pool.has(r.name) ? 'checked' : ''}><span>${title(r.name)}</span><span class="n">${r.corner ? 'угл' : r.limit ? `≤${r.limit}` : ''}</span>`;
    l.querySelector('input').onchange = e => { if (e.target.checked) state.pool.add(r.name); else state.pool.delete(r.name); poolChanged(); };
    $('pool').appendChild(l);
  });
  const isGame = state.pool.size === GAME_POOL.length && GAME_POOL.every(n => state.pool.has(n));
  $('poolGame').setAttribute('aria-pressed', isGame);
  $('poolAll').setAttribute('aria-pressed', state.pool.size === ENTRIES.length - 1);
}

function poolChanged() { renderPool(); generate(); renderCell(); setView(state.view); }

function renderStats() {
  const sum = { monster: 0, chest: 0, trap: 0, light: 0 }, kinds = {};
  state.placed.rooms.forEach(r => {
    const name = r.isKey ? M.key : r.name;
    ATLAS.rooms[name].forEach(o => { if (o.cat in sum) sum[o.cat]++; });
    kinds[name] = (kinds[name] || 0) + 1;
  });
  const starts = state.placed.rooms.filter(r => r.start).length + 1;
  const rows = Object.entries(kinds).sort((a, b) => b[1] - a[1]).map(([k, v]) => `<div><span>${title(k)}</span><span>${v}</span></div>`).join('');
  $('statBox').innerHTML = `<h2>Что получилось</h2><dl><dt>Клетки</dt><dd>${state.plan.grid} × ${state.plan.grid}, ${fmt(M.pitch * state.plan.grid)} м</dd><dt>Проёмы</dt><dd>${state.placed.doors.length}</dd><dt>Монстры</dt><dd>${sum.monster} точек</dd><dt>Контейнеры</dt><dd>${sum.chest}</dd><dt>Ловушки</dt><dd>${sum.trap}</dd><dt>Свет</dt><dd>${sum.light}</dd><dt>Старты</dt><dd>${starts} комнат</dd></dl><div class="bars">${rows}</div>`;
}

function selectCell(bucket) {
  const cell = typeof bucket === 'string' ? +bucket.slice(1) : bucket;
  state.cell = cell; renderCell();
  if (state.view === 'plan') drawMainPlan();
}

function renderCell() {
  const info = $('info');
  if (state.cell === null) { info.innerHTML = '<h2>Клетка</h2><div class="sub" style="margin:0">Кликни клетку на плане или в 3D.</div>'; return; }
  const isKey = state.plan.keyCells.includes(state.cell), r = cellRoom(state.cell), name = isKey ? M.key : r.name;
  const x = state.cell % state.plan.grid, z = Math.floor(state.cell / state.plan.grid), turn = isKey ? state.plan.keyTurns : state.plan.turns[state.cell];
  info.innerHTML = `<h2>Клетка ${x}, ${z}</h2><div class="big">${title(name)}</div><div class="sub">поворот ${turn * 90}°${r.start ? ' · здесь могут стартовать команды' : ''}</div><div class="seg"><button id="openCell">Открыть макет</button><button id="walkCell">Пройти</button></div>`;
  $('openCell').onclick = () => go(`room/${name}`);
  $('walkCell').onclick = () => setView('walk');
}

// --- Навигация и правки ---
function tabs() {
  $('tabRooms').setAttribute('aria-pressed', state.screen !== 'dungeon');
  $('tabDungeon').setAttribute('aria-pressed', state.screen === 'dungeon');
}

function go(hash) { if (location.hash === `#${hash}`) route(); else location.hash = hash; }

function route() {
  const [what, arg] = location.hash.slice(1).split('/');
  if (what === 'room' && ATLAS.rooms[arg]) return openRoom(arg);
  if (what === 'dungeon') return openDungeon(arg ? +arg : undefined);
  state.screen = 'rooms'; state.room = null;
  $('stage').hidden = true; $('gallery').hidden = false; $('prev').hidden = $('next').hidden = true; $('crumb').textContent = '';
  tabs(); renderFilters(); renderCards();
}

function step(delta) {
  const i = ENTRIES.findIndex(r => r.name === state.room), n = ENTRIES.length;
  go(`room/${ENTRIES[(i + delta + n) % n].name}`);
}

function notesText() {
  const lines = [];
  ENTRIES.forEach(r => {
    const n = notes[r.name];
    if (!n || (!n.status && !n.note && !Object.keys(n.objs).length)) return;
    lines.push(`${r.name} (${title(r.name)})${n.status ? ` [${VERDICTS[n.status]}]` : ''}${n.note ? `: ${n.note.trim()}` : ''}`);
    Object.entries(n.objs).forEach(([id, text]) => {
      const o = ATLAS.rooms[r.name].find(x => x.id === id);
      lines.push(`  ${id} (${o ? `${OBJECT_NAMES[o.name] || o.name} @ ${o.p.map(fmt).join(', ')}` : '?'}): ${text.trim()}`);
    });
  });
  return lines.join('\n') || 'Правок нет';
}

async function copyNotes() {
  const text = notesText();
  try { await navigator.clipboard.writeText(text); } catch (e) {
    const t = document.createElement('textarea'); t.value = text; document.body.appendChild(t); t.select(); document.execCommand('copy'); t.remove();
  }
  const b = $('copyAll'), label = b.textContent; b.textContent = 'Скопировано'; setTimeout(() => { b.textContent = label; }, 1400);
}

function bindUi() {
  $('tabRooms').onclick = () => go('rooms');
  $('tabDungeon').onclick = () => go(`dungeon/${state.seed}`);
  $('prev').onclick = () => step(-1); $('next').onclick = () => step(1);
  $('copyAll').onclick = copyNotes;
  $('objFind').oninput = renderList;
  $('cut').oninput = e => setCut(e.target.value);
  $('expo').oninput = e => { stage.setExposure(+e.target.value); $('expoV').textContent = (+e.target.value).toFixed(2); };
  $('ranges').onchange = e => stage.setRings(e.target.checked);
  const light = on => { stage.setGameLight(on); $('lGame').setAttribute('aria-pressed', on); $('lPlan').setAttribute('aria-pressed', !on); };
  $('lGame').onclick = () => light(true); $('lPlan').onclick = () => light(false);
  $('lPlan').setAttribute('aria-pressed', true);
  $('seed').onchange = e => { state.seed = Math.trunc(+e.target.value) || 1; poolChanged(); };
  $('roll').onclick = () => { state.seed = 1 + Math.floor(Math.random() * (SEED_LIMIT - 1)); $('seed').value = state.seed; poolChanged(); };
  $('loops').oninput = e => { state.loops = +e.target.value; $('loopsV').textContent = state.loops.toFixed(2); poolChanged(); };
  $('poolGame').onclick = () => { state.pool = new Set(GAME_POOL); poolChanged(); };
  $('poolAll').onclick = () => { state.pool = new Set(ENTRIES.filter(r => r.name !== M.key).map(r => r.name)); poolChanged(); };
  addEventListener('hashchange', route);
  addEventListener('resize', () => { if (state.view === 'plan' && !$('stage').hidden) { state.pan = null; drawMainPlan(); } });
  buildViews(); bindPlan();
}

bindUi();
loadRooms().then(route).catch(e => { document.body.textContent = `Не загрузился файл данных: ${e.message}`; });
