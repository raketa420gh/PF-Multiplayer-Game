// Набор аниматика: сцена, свет, материалы, болванки и площадки. Всё содержимое лежит в группе W в координатах Unity
// (x — восток, y — вверх, z — север): у W масштаб z = −1, поэтому числа из hub.json и данных атласа ставятся как есть.
'use strict';
const DEG = Math.PI / 180;
const lerp = (a, b, u) => a + (b - a) * u, cl = u => Math.min(1, Math.max(0, u)), seg = (t, a, b) => cl((t - a) / (b - a)), sm = u => u * u * (3 - 2 * u);
const mix = (a, b, u) => a.map((v, i) => lerp(v, b[i], u));
const rnd = (i, k = 0) => { const s = Math.sin(i * 127.1 + k * 311.7) * 43758.5453; return s - Math.floor(s); };
const wob = (t, k = 0) => { const i = Math.floor(t), f = t - i; return lerp(rnd(i, k), rnd(i + 1, k), sm(f)) * 2 - 1; };

const scene = new THREE.Scene(); scene.background = new THREE.Color(0); scene.fog = new THREE.Fog(0, 10, 100);
const W = new THREE.Group(); W.scale.z = -1; scene.add(W);
const camera = new THREE.PerspectiveCamera(40, 16 / 9, 0.1, 600);
const hemi = new THREE.HemisphereLight(0xffffff, 0x000000, 1), sun = new THREE.DirectionalLight(0xffffff, 0); scene.add(hemi, sun, sun.target);
// Точечных огней всегда шесть: шот двигает их и гасит лишние, число не меняется — шейдеры не пересобираются на склейках.
const PL = [...Array(6)].map(() => { const l = new THREE.PointLight(0xffffff, 0, 10, 1.5); W.add(l); return l; });

const BOX = new THREE.BoxGeometry(1, 1, 1), CYL = new THREE.CylinderGeometry(.5, .5, 1, 16), CYL2 = new THREE.CylinderGeometry(.5, .5, 2, 16), SPH = new THREE.SphereGeometry(.5, 16, 12),
  OCT = new THREE.OctahedronGeometry(.5), HALF = new THREE.CylinderGeometry(.5, .5, 1, 16, 1, false, 0, Math.PI), RING = new THREE.TorusGeometry(1, .07, 8, 40), DISC = new THREE.CircleGeometry(1, 40);
const lit = {}, glow = {};
const L = c => lit[c] || (lit[c] = new THREE.MeshLambertMaterial({ color: c, side: THREE.DoubleSide }));
const E = c => glow[c] || (glow[c] = new THREE.MeshBasicMaterial({ color: c, side: THREE.DoubleSide }));

function grp(parent, x = 0, y = 0, z = 0) { const g = new THREE.Group(); g.position.set(x, y, z); parent.add(g); return g; }
function mesh(parent, geo, mat, x, y, z, sx, sy, sz) { const o = new THREE.Mesh(geo, mat); o.position.set(x, y, z); o.scale.set(sx, sy ?? sx, sz ?? sx); parent.add(o); return o; }
const box = (p, m, x, y, z, w, h, d) => mesh(p, BOX, m, x, y, z, w, h, d);

// ---- статика одним InstancedMesh на пару «геометрия + материал»
const _p = new THREE.Vector3(), _q = new THREE.Quaternion(), _s = new THREE.Vector3(), _e = new THREE.Euler();
const mx = (x, y, z, w, h, d, ry = 0) => new THREE.Matrix4().compose(_p.set(x, y, z), _q.setFromEuler(_e.set(0, ry * DEG, 0)), _s.set(w, h, d));
class Bake {
  constructor() { this.map = new Map(); }
  add(geo, mat, m) { const k = geo.uuid + mat.uuid; if (!this.map.has(k)) this.map.set(k, { geo, mat, list: [] }); this.map.get(k).list.push(m); }
  box(mat, x, y, z, w, h, d, ry) { this.add(BOX, mat, mx(x, y, z, w, h, d, ry)); }
  into(g) {
    for (const { geo, mat, list } of this.map.values()) { const im = new THREE.InstancedMesh(geo, mat, list.length); list.forEach((m, i) => im.setMatrixAt(i, m)); im.frustumCulled = false; g.add(im); }

    return g;
  }
}

// ---- комната из атласа (Docs/LevelMockups/Atlas/data): pivot снизу по центру, yaw по часовой, лестница поднимается вдоль локального +z
const ROOM_COLORS = { floor: 0x4a4e4c, platform: 0x5a5f5c, stairs: 0x6c716c, wall: 0x5c605e, shell: 0x525654, column: 0x767a7c, pit: 0x4a443e, bars: 0x30323a, prop: 0x7a6248, chest: 0x6a5236, machine: 0x3c3e46, upper: 0x5c605e, ceiling: 0x3a3d3c };
function room(name) {
  const g = grp(W), b = new Bake(), top = new Bake(), flames = [];
  for (const o of ROOMS[name]) {
    if (o.cat === 'light') { const f = mesh(g, SPH, E(0xffb060), o.p[0], o.p[1], o.p[2], .34); f.userData.y = o.p[1]; flames.push(f); continue; }
    if (o.cat === 'trap' && !/^Door|^Lamp/.test(o.name)) continue;
    const color = /Кост|Череп/.test(o.name) ? 0xd8d0bc : ROOM_COLORS[o.cat === 'trap' ? 'prop' : o.cat];
    if (color === undefined || !o.s) continue;
    const [w, h, d] = o.s, [x, y, z] = o.p, m = L(color), to = o.cat === 'ceiling' ? top : b;
    if (o.cat === 'stairs') {
      const n = Math.max(2, Math.round(h / .25)), run = d / n, P = mx(x, y, z, 1, 1, 1, o.ry);
      for (let i = 0; i < n; i++) { const up = h * (i + 1) / n; to.add(BOX, m, P.clone().multiply(mx(0, up / 2, run * (i + .5) - d / 2, w, up, run))); }
    } else to.add(o.round ? CYL : BOX, m, mx(x, y + h / 2, z, w, Math.max(h, .02), d, o.ry || 0));
  }
  b.into(g); g.flames = flames; g.ceiling = top.into(grp(g)); g.visible = false;

  return g;
}

// ---- болванки
// Человек: начало в стопах, смотрит вдоль +z. Конечности — рычаги от бедра и плеча; отрицательный поворот по x поднимает их вперёд.
function dummy(parent, color = 0x8c8f96, k = 1) {
  const g = grp(parent), m = L(color), hips = grp(g, 0, .95, 0);
  box(hips, m, 0, .32, 0, .44 * k, .64, .25 * k);
  const head = mesh(hips, SPH, m, 0, .84, 0, .26 * (k < 1 ? .92 : 1));
  const limb = (x, y, len, w) => { const p = grp(hips, x, y, 0); box(p, m, 0, -len / 2, 0, w, len, w); return p; };
  Object.assign(g.userData, { hips, head, legL: limb(-.12, 0, .95, .17 * k), legR: limb(.12, 0, .95, .17 * k), armL: limb(-.3 * k, .6, .64, .13 * k), armR: limb(.3 * k, .6, .64, .13 * k) });

  return g;
}
// Поза и место. ph — фаза шага, amp — размах, lean — наклон корпуса вперёд. Рука с предметом (lockL/lockR) в шаге не машет.
function actor(d, x, y, z, yaw, ph = 0, amp = 0, lean = 0) {
  const u = d.userData, s = Math.sin(ph);
  d.visible = true; d.position.set(x, y, z); d.rotation.y = yaw * DEG; d.scale.setScalar(1);
  u.legL.rotation.x = s * amp; u.legR.rotation.x = -s * amp;
  if (!u.lockL) u.armL.rotation.x = -s * amp * .7;
  if (!u.lockR) u.armR.rotation.x = s * amp * .7;
  u.hips.rotation.x = lean; u.hips.position.y = .95 - Math.abs(s) * amp * .04; u.head.position.set(0, .84, 0);
}
function hold(d, side, angle) { const u = d.userData; u['lock' + side] = true; u['arm' + side].rotation.x = angle; return u['arm' + side]; }
function torch(d) { const arm = hold(d, 'R', -1.25), g = grp(arm, 0, -.62, 0); g.rotation.x = 1.25; box(g, L(0x4a3622), 0, .05, 0, .05, .5, .05); d.userData.flame = mesh(g, SPH, E(0xffb050), 0, .38, 0, .17); }
// Где горит факел болванки (для точечного огня): чуть впереди и выше плеча.
const torchAt = d => { const a = d.rotation.y; return [d.position.x + Math.sin(a) * .55, d.position.y + 2, d.position.z + Math.cos(a) * .55]; };

// Кисть: ладонь, четыре пальца вдоль +z, большой палец и манжета к −z.
function hand(parent, color) {
  const g = grp(parent), m = L(color); g.fingers = [];
  box(g, m, 0, 0, 0, .09, .035, .1); box(g, m, 0, .004, -.12, .1, .05, .16); box(g, m, .06, 0, .01, .022, .022, .07);
  for (let i = 0; i < 4; i++) { const p = grp(g, -.034 + i * .0225, 0, .05); box(p, m, 0, 0, .035, .019, .022, .07); g.fingers.push(p); }

  return g;
}

// ---- площадки
// 1. Весы оценщика: коромысло на стойке, две чаши, столбик монет справа, фаланга с оссаритом и рука в перчатке.
function scaleSet() {
  const g = grp(W), brass = L(0xb08a3a), dark = L(0x2a2622), bm = L(0xd8d0bc);
  box(g, L(0x2b221b), 0, -.02, 0, 2.4, .04, 1.4);
  mesh(g, CYL, brass, 0, .03, 0, .16, .06, .16); mesh(g, CYL, brass, 0, .3, 0, .03, .5, .03);
  const beam = grp(g, 0, .55, 0); box(beam, brass, 0, 0, 0, .66, .018, .018);
  const pan = () => { const p = grp(g); box(p, dark, 0, -.14, 0, .004, .28, .004); mesh(p, CYL, brass, 0, -.285, 0, .2, .012, .2); return p; };
  const panL = pan(), panR = pan(), bone = grp(g);
  for (let i = 0; i < 8; i++) mesh(panR, CYL, L(0xd9b34a), rnd(i) * .006, -.275 + i * .0075, rnd(i, 2) * .006, .036, .006, .036);
  mesh(bone, CYL, bm, 0, 0, 0, .016, .05, .016).rotation.z = Math.PI / 2; mesh(bone, SPH, bm, -.027, 0, 0, .024); mesh(bone, SPH, bm, .027, 0, 0, .021);
  g.crystal = mesh(bone, OCT, E(0xdfe9f2), .004, .009, -.002, .014, .02, .012);
  Object.assign(g, { beam, panL, panR, bone, hand: hand(g, 0x3a2c20) }); g.visible = false;

  return g;
}

// 2. Хребет: гряда из «позвонков» — тело, остистый отросток, два боковых склона — и чёрный зев у подножия ближнего.
function ridgeSet() {
  const g = grp(W), b = new Bake(), rock = L(0x23232a), spur = L(0x2c2b33);
  b.box(L(0x15151a), 0, -1, 600, 5000, 2, 5000);
  for (let i = 0; i < 17; i++) {
    const z = 170 + i * 52, x = Math.sin(i * .55) * 70 * (i / 16), s = 1 + (i % 3) * .12, h = 70 + rnd(i) * 26;
    b.box(rock, x, h / 2, z, 95 * s, h, 40, rnd(i, 1) * 10 - 5); b.box(spur, x, h + 26, z, 16, 60, 22, 0);
    b.box(rock, x - 62 * s, h * .33, z + 4, 50, h * .66, 30, 12); b.box(rock, x + 62 * s, h * .33, z + 4, 50, h * .66, 30, -12);
  }
  for (let i = 0; i < 26; i++) { const side = i % 2 ? 1 : -1, h = 6 + rnd(i, 3) * 22; b.box(rock, side * (60 + rnd(i, 4) * 260), h / 2, (side < 0 ? 290 : 20) + rnd(i, 5) * 480, 30 + rnd(i, 6) * 40, h, 30 + rnd(i, 7) * 40, rnd(i, 8) * 90); }
  b.box(E(0x000000), 0, 7, 149.2, 12, 14, 2); b.box(L(0x3a3940), -7.4, 8, 148.6, 2.8, 16, 3); b.box(L(0x3a3940), 7.4, 8, 148.6, 2.8, 16, 3); b.box(L(0x3a3940), 0, 15.4, 148.6, 17.6, 2.8, 3);
  b.into(g); g.sun = mesh(g, SPH, E(0xffb070), 520, 40, 1500, 190); g.visible = false;

  return g;
}

// 3–5. Ствол Морнхейма: процессия с носилками, костевед у конторки, колонна в кандалах на лестнице, плита на дверной проём.
function shaftSet() {
  const g = room('GreatHallShaft'), robe = 0x6b6258, shroud = L(0xcfc8b8);
  g.proc = grp(g); g.bearers = []; g.litters = []; g.torchmen = [];
  for (let j = 0; j < 3; j++) {
    const l = grp(g.proc); box(l, L(0x4a3622), -.33, .95, 0, .05, .05, 2.9); box(l, L(0x4a3622), .33, .95, 0, .05, .05, 2.9); box(l, shroud, 0, 1.1, 0, .56, .24, 1.85); g.litters.push(l);
    for (let k = 0; k < 2; k++) { const d = dummy(g.proc, robe); hold(d, 'L', -.5); hold(d, 'R', -.5); g.bearers.push(d); }
  }
  for (let j = 0; j < 2; j++) { const d = dummy(g.proc, 0x7a6f60); torch(d); g.torchmen.push(d); }
  g.reader = dummy(g.proc, 0x8a8478); hold(g.reader, 'R', -1.05);
  box(g.proc, L(0x4a3622), 6.85, .55, 3, .5, 1.1, .6); box(g.proc, L(0xd9d2c0), 6.85, 1.13, 3, .36, .05, .5);
  g.chain = grp(g); g.shackled = [];
  for (let i = 0; i < 6; i++) { const d = dummy(g.chain, 0x77706a); box(d, L(0x26262a), 0, .14, 0, .34, .035, .035); box(d, L(0x26262a), 0, .5, -.55, .03, .03, 1.1).rotation.x = .5; g.shackled.push(d); }
  g.fall = grp(g); g.slab = box(g.fall, L(0x6a6e6c), 12.6, 2.3, -16.55, 3.2, 4.6, .5); g.runner = dummy(g.fall, 0x8a8478);

  return g;
}

// 6. Печать: портал в скале, створки, свинцовый диск из двух половин с клеймом весов, снег и мох для хода веков, кувалда.
function sealSet() {
  const g = grp(W), st = L(0x55565c), lead = L(0x7b7f86), brass = L(0xb08a3a);
  box(g, L(0x34343a), 0, 10, 3.6, 70, 26, 5); box(g, L(0x3d3b38), 0, -.5, -10, 70, 1, 30);
  box(g, st, -3.4, 3.6, .7, 1.6, 7.2, 1.6); box(g, st, 3.4, 3.6, .7, 1.6, 7.2, 1.6); box(g, st, 0, 7.9, .7, 9.6, 1.4, 1.9);
  box(g, L(0x2b2a2c), -1.3, 3.6, 1, 2.58, 7.2, .4); box(g, L(0x2b2a2c), 1.3, 3.6, 1, 2.58, 7.2, .4);
  const seal = grp(g, 0, 2.6, .7); g.halves = [-1, 1].map(side => {
    const h = grp(seal); h.scale.x = side; mesh(h, HALF, lead, 0, 0, 0, 1.5, .16, 1.5).rotation.x = Math.PI / 2;
    box(h, brass, .19, .2, -.1, .38, .05, .03); box(h, brass, .34, .06, -.1, .02, .26, .02); box(h, brass, .34, -.08, -.1, .24, .035, .03); box(h, brass, .015, .02, -.1, .03, .5, .03);

    return h;
  });
  g.snow = [box(g, L(0xe8ecf0), 0, 0, -8, 70, 1, 26), box(g, L(0xe8ecf0), 0, 8.6, .5, 9.8, 1, 2.2), box(g, L(0xe8ecf0), 0, 0, .45, 5.2, 1, .7)];
  g.moss = [box(g, L(0x4a6638), -3.4, 0, -.15, 1.7, 1, .12), box(g, L(0x4a6638), 3.4, 0, -.15, 1.7, 1, .12), box(g, L(0x4a6638), -1.3, 0, .78, 2.5, 1, .06), box(g, L(0x4a6638), 1.3, 0, .78, 2.5, 1, .06)];
  g.hammer = grp(g, 1.25, 2.45, -.35); box(g.hammer, L(0x5a4028), -.7, 0, 0, 1.4, .055, .055); box(g.hammer, L(0x3a3c42), -1.4, 0, 0, .24, .32, .32); box(g.hammer, brass, -1.4, 0, .165, .14, .14, .01);
  g.visible = false;

  return g;
}

// 7–10. Погребальные ниши: трое жильщиков (замыкающий — с факелом и мешком), костяной мечник, плита поперёк коридора, портал.
function nichesSet() {
  const g = room('BurialNiches');
  g.crew = [0x8c8f96, 0x7d8a80, 0x94887a].map(c => dummy(g, c));
  const shield = hold(g.crew[1], 'L', -1.2); box(shield, L(0x5a4028), -.05, -.45, .12, .52, .72, .06); box(shield, L(0x8a8c92), -.05, -.45, .16, .14, .14, .03);
  torch(g.crew[2]); box(g.crew[2].userData.hips, L(0x5a4028), 0, .3, -.22, .36, .44, .2);
  g.bones = dummy(g, 0xd8d0bc, .72); box(hold(g.bones, 'R', -2.6), L(0x9aa0a8), 0, -.66, .42, .04, .05, .9);
  g.slab = box(g, L(0x6a6e6c), -10.5, 2.3, 14.6, 1, 4.6, 3.4);
  g.portal = grp(g, 12.4, 1.25, 10.7); g.portal.rotation.y = 90 * DEG; mesh(g.portal, RING, E(0x8fd0ff), 0, 0, 0, 1.05); mesh(g.portal, DISC, new THREE.MeshBasicMaterial({ color: 0x4f9fe8, transparent: true, opacity: .55, side: THREE.DoubleSide }), 0, 0, 0, 1);

  return g;
}

// Вставки крупным планом: ниша с лежащим костяком (рёбра вырывают по одному), череп, саван с костяной кистью, мешок и факел на полу.
function insertSet() {
  const g = grp(W), st = L(0x4c4e50), bone = L(0xd8d0bc), leather = L(0x5a4028);
  g.niche = grp(g); box(g.niche, st, -2.4, 1.5, .35, 3.3, 3, .7); box(g.niche, st, 2.4, 1.5, .35, 3.3, 3, .7); box(g.niche, st, 0, .375, .35, 1.5, .75, .7); box(g.niche, st, 0, 2.275, .35, 1.5, 1.45, .7);
  box(g.niche, L(0x1c1c1e), 0, 1.15, .75, 1.5, .8, .1); box(g.niche, L(0x3a3b3d), 0, -.05, -1, 8, .1, 3);
  g.ribs = grp(g); g.torn = []; box(g.ribs, bone, .06, .8, .38, .64, .04, .04); mesh(g.ribs, SPH, bone, -.4, .86, .38, .2); box(g.ribs, L(0x8a8478), .5, .78, .4, .4, .03, .3);
  for (let i = 0; i < 7; i++) for (const side of [-1, 1]) {
    const r = grp(g.ribs, -.2 + i * .085, .8, .38); box(r, bone, 0, .03, side * .08, .02, .02, .17); box(r, bone, 0, .15, side * .16, .02, .26, .02);
    if (side < 0 && i > 1 && i < 5) { box(r, E(0xdfe9f2), 0, .15, -.172, .008, .2, .006); r.home = r.position.clone(); g.torn.push(r); }
  }
  g.hand = hand(g, 0x3a2c20);
  g.bag = grp(g, -.42, .66, -.5); box(g.bag, leather, 0, -.14, 0, .3, .02, .24); box(g.bag, leather, -.15, 0, 0, .02, .3, .24); box(g.bag, leather, .15, 0, 0, .02, .3, .24); box(g.bag, leather, 0, 0, -.12, .3, .3, .02); box(g.bag, leather, 0, 0, .12, .3, .3, .02);
  g.skull = grp(g, 0, 1.02, .34); mesh(g.skull, SPH, bone, 0, 0, 0, .24, .26, .28); box(g.skull, bone, 0, -.12, -.05, .15, .08, .15);
  g.eyes = [-1, 1].map(side => { box(g.skull, L(0x0a0a0c), side * .056, .012, -.128, .064, .056, .03); return box(g.skull, E(0x8fd0ff), side * .056, .012, -.14, .024, .024, .01); });
  g.shroud = grp(g); box(g.shroud, L(0x3a3b3d), 0, -.05, 0, 8, .1, 8); box(g.shroud, L(0x4a3622), 0, .12, 0, 1, .24, 2.3); box(g.shroud, L(0xcfc8b8), 0, .36, 0, .62, .22, 1.9); box(g.shroud, bone, .42, .3, .2, .26, .03, .03);
  g.claw = hand(g.shroud, 0xd8d0bc); g.claw.position.set(.66, .3, .2); g.claw.rotation.y = 90 * DEG;
  g.drop = grp(g); box(g.drop, L(0x3a3b3d), 0, -.05, 0, 8, .1, 8); box(g.drop, leather, -.1, .13, .1, .5, .26, .34); box(g.drop, L(0x2a1c12), .16, .13, .1, .02, .22, .3);
  g.shards = [...Array(9)].map((_, i) => mesh(g.drop, OCT, E(0x9fb4c8), .22 + rnd(i) * .34, .03 + rnd(i, 1) * .02, -.12 + rnd(i, 2) * .4, .03 + rnd(i, 3) * .03));
  box(g.drop, L(0x4a3622), -.55, .03, -.35, .5, .05, .05); g.ember = mesh(g.drop, SPH, E(0xffb050), -.27, .06, -.35, .16);
  g.visible = false;

  return g;
}

// 11–12. «Устье»: блокаут хаба из выгрузки макета, тракт идёт по рельефу южнее ворот; повозка с новичками.
function hubSet() {
  const g = grp(W), b = new Bake(), H = window.HUB, mats = {};
  for (const m of H.mats) {
    const color = new THREE.Color().setRGB(m.c[0], m.c[1], m.c[2], THREE.SRGBColorSpace);
    mats[m.n] = m.glow ? new THREE.MeshBasicMaterial({ color, side: THREE.DoubleSide }) : new THREE.MeshLambertMaterial({ color, side: THREE.DoubleSide, transparent: m.a < 1, opacity: m.a, depthWrite: m.a >= 1 });
  }
  const geos = H.geos.map(q => { let bg = new THREE.BufferGeometry(); bg.setAttribute('position', new THREE.Float32BufferAttribute(q.v, 3)); bg.setIndex(q.i); if (q.flat) bg = bg.toNonIndexed(); bg.computeVertexNormals(); return bg; });
  for (const e of H.meshes) b.add(e.k === 'box' ? BOX : e.k === 'cyl' ? CYL2 : e.k === 'sph' ? SPH : geos[e.g], mats[e.m], new THREE.Matrix4().compose(_p.set(...e.p), _q.set(...e.q), _s.set(...e.s)));
  b.into(g);
  const cart = g.cart = grp(g), wood = L(0x5a4630); g.wheels = [];
  box(cart, wood, 0, .75, 0, 1.5, .12, 2.8); box(cart, wood, -.75, 1.05, 0, .06, .5, 2.8); box(cart, wood, .75, 1.05, 0, .06, .5, 2.8); box(cart, wood, 0, 1.05, -1.4, 1.5, .5, .06); box(cart, wood, 0, .7, 2.4, .08, .08, 2);
  for (const side of [-1, 1]) { const w = mesh(cart, CYL, L(0x3a2c20), side * .85, .55, -.3, 1.1, .1, 1.1); w.rotation.z = Math.PI / 2; g.wheels.push(w); }
  box(cart, L(0x4a4038), 0, 1.15, 3.6, .55, .7, 1.7); box(cart, L(0x4a4038), 0, 1.75, 4.5, .3, .7, .4); for (const [x, z] of [[-.2, 3], [.2, 3], [-.2, 4.2], [.2, 4.2]]) box(cart, L(0x4a4038), x, .4, z, .14, .8, .14);
  [[-.4, .5], [.4, .5], [-.4, -.6], [.4, -.6]].forEach(([x, z], i) => { const d = dummy(cart, [0x8c8f96, 0x7d8a80, 0x94887a, 0x86808c][i]); actor(d, x, 0, z, 0); d.userData.legL.rotation.x = d.userData.legR.rotation.x = -1.4; });
  g.visible = false;

  return g;
}
