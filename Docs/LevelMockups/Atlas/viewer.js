// 3D-сцена и 2D-план. Данные в координатах Unity: x восток, z север, pivot снизу по центру, yaw по часовой.
// В three они переводятся как (x, y, -z). Геометрия сливается в один меш на клетку и категорию, поэтому 3D целого
// подземелья не тяжелее одной комнаты; выбор объекта идёт по карте треугольник → объект.
const CATS = {
  floor: { label: 'Пол', color: '#4a5450' },
  platform: { label: 'Платформы', color: '#6f7f76' },
  stairs: { label: 'Лестницы', color: '#8fa188' },
  wall: { label: 'Стены', color: '#77837c' },
  shell: { label: 'Стороны клетки', color: '#5d6a64' },
  column: { label: 'Колонны', color: '#93a09a' },
  pit: { label: 'Стенки ям', color: '#5a4f45' },
  bars: { label: 'Решётки', color: '#a0a8b0' },
  prop: { label: 'Пропсы', color: '#b08a5a' },
  machine: { label: 'Механизмы', color: '#c77dff', ghost: true },
  water: { label: 'Вода', color: '#2f6fb0', alpha: 0.6, ghost: true },
  acid: { label: 'Кислота', color: '#5ff07a', alpha: 0.7, ghost: true },
  fx: { label: 'Эффекты', color: '#7cc8ff', ghost: true },
  door: { label: 'Двери', color: '#8a5a33', ghost: true },
  light: { label: 'Свет', color: '#ffd08a', ghost: true, glow: true },
  monster: { label: 'Монстры', color: '#ff5a4d', ghost: true },
  chest: { label: 'Контейнеры', color: '#ffd76b', ghost: true },
  trap: { label: 'Ловушки', color: '#ff8a3d', ghost: true },
  start: { label: 'Старты', color: '#4dd0ff', ghost: true },
  upper: { label: 'Верх стен', color: '#77837c', alpha: 0.3 },
  ceiling: { label: 'Потолок', color: '#3c4744', alpha: 0.3, ghost: true }
};
const LIGHT_POWER = { Torch: 2.5, Candle: 3, Acid: 4 };
const LIGHT_POOL = 16;
const HAND_TORCH_RANGE = 5;
const EYE = 1.7;
const DEG = Math.PI / 180;

// Точка объекта (локальная) → three. Поворот объекта, затем поворот и сдвиг его размещения.
function transformOf(o, place) {
  const a = (o.ry || 0) * DEG, c = Math.cos(a), s = Math.sin(a);
  const b = (place.yaw || 0) * DEG, pc = Math.cos(b), ps = Math.sin(b);
  const q = o.q, [px, py, pz] = o.p;
  return (x, y, z) => {
    let X, Y = y, Z;
    if (q) {
      const [qx, qy, qz, qw] = q;
      const tx = 2 * (qy * z - qz * y), ty = 2 * (qz * x - qx * z), tz = 2 * (qx * y - qy * x);
      X = x + qw * tx + qy * tz - qz * ty; Y = y + qw * ty + qz * tx - qx * tz; Z = z + qw * tz + qx * ty - qy * tx;
    } else { X = x * c + z * s; Z = -x * s + z * c; }
    X += px; Y += py; Z += pz;
    return [X * pc + Z * ps + (place.x || 0), Y, -(-X * ps + Z * pc + (place.z || 0))];
  };
}

class Batch {
  constructor(cat, place) { this.cat = cat; this.place = place; this.pos = []; this.nor = []; this.line = []; this.faces = []; }
  tri(a, b, c, center, o) {
    const ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2], vx = c[0] - a[0], vy = c[1] - a[1], vz = c[2] - a[2];
    let nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
    const len = Math.hypot(nx, ny, nz) || 1; nx /= len; ny /= len; nz /= len;
    const mid = [(a[0] + b[0] + c[0]) / 3 - center[0], (a[1] + b[1] + c[1]) / 3 - center[1], (a[2] + b[2] + c[2]) / 3 - center[2]];
    if (nx * mid[0] + ny * mid[1] + nz * mid[2] < 0) { [b, c] = [c, b]; nx = -nx; ny = -ny; nz = -nz; }
    this.pos.push(...a, ...b, ...c);
    for (let i = 0; i < 3; i++) this.nor.push(nx, ny, nz);
    this.faces.push(o);
  }
  quad(a, b, c, d, center, o) { this.tri(a, b, c, center, o); this.tri(a, c, d, center, o); }
  edge(a, b) { this.line.push(...a, ...b); }
  box(T, cx, cy, cz, w, h, d, o) {
    const p = [];
    for (let i = 0; i < 8; i++) p.push(T(cx + (i & 1 ? w : -w) / 2, cy + (i & 2 ? h : -h) / 2, cz + (i & 4 ? d : -d) / 2));
    const center = T(cx, cy, cz);
    [[0, 1, 3, 2], [4, 5, 7, 6], [0, 1, 5, 4], [2, 3, 7, 6], [0, 2, 6, 4], [1, 3, 7, 5]].forEach(f => this.quad(p[f[0]], p[f[1]], p[f[2]], p[f[3]], center, o));
    [[0, 1], [1, 3], [3, 2], [2, 0], [4, 5], [5, 7], [7, 6], [6, 4], [0, 4], [1, 5], [2, 6], [3, 7]].forEach(e => this.edge(p[e[0]], p[e[1]]));
  }
  cylinder(T, w, h, d, o) {
    const n = 12, center = T(0, h / 2, 0), top = T(0, h, 0), bottom = T(0, 0, 0), ring = i => [Math.cos(i / n * 2 * Math.PI) * w / 2, Math.sin(i / n * 2 * Math.PI) * d / 2];
    for (let i = 0; i < n; i++) {
      const [ax, az] = ring(i), [bx, bz] = ring(i + 1);
      const a0 = T(ax, 0, az), b0 = T(bx, 0, bz), a1 = T(ax, h, az), b1 = T(bx, h, bz);
      this.quad(a0, b0, b1, a1, center, o); this.tri(top, a1, b1, center, o); this.tri(bottom, a0, b0, center, o);
      this.edge(a0, b0); this.edge(a1, b1);
    }
  }
  add(o, place) {
    const T = transformOf(o, place), [w, h, d] = o.s || [0.25, 0.25, 0.25];
    if (o.cat === 'light') return this.box(T, 0, 0, 0, 0.25, 0.25, 0.25, o);
    if (o.cat === 'stairs') {
      const steps = Math.max(2, Math.round(h / 0.25)), run = d / steps;
      for (let i = 0; i < steps; i++) { const top = h * (i + 1) / steps; this.box(T, 0, top / 2, run * (i + 0.5) - d / 2, w, top, run, o); }
      return;
    }
    if (o.round) this.cylinder(T, w, h, d, o); else this.box(T, 0, h / 2, 0, w, Math.max(h, 0.01), d, o);
    if (o.cat === 'monster') this.box(T, 0, h * 0.75, d / 2 + 0.2, 0.2, 0.2, 0.4, o);
  }
}

// Пример размещения: { objs, x, z, yaw, bucket }. bucket — клетка, в чей меш лягут объекты.
class Stage {
  constructor(el) {
    this.el = el;
    const r = this.renderer = new THREE.WebGLRenderer({ antialias: true, preserveDrawingBuffer: true });
    r.setPixelRatio(Math.min(devicePixelRatio, 2));
    r.outputEncoding = THREE.sRGBEncoding;
    r.toneMapping = THREE.ACESFilmicToneMapping;
    r.physicallyCorrectLights = true;
    r.localClippingEnabled = true;
    el.appendChild(r.domElement);
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color('#0d1110');
    this.camera = new THREE.PerspectiveCamera(50, 1, 0.05, 900);
    this.orbit = new THREE.OrbitControls(this.camera, r.domElement);
    this.orbit.enableDamping = true;
    this.orbit.dampingFactor = 0.12;
    this.orbit.mouseButtons = { LEFT: null, MIDDLE: THREE.MOUSE.DOLLY, RIGHT: THREE.MOUSE.ROTATE };
    this.clip = new THREE.Plane(new THREE.Vector3(0, -1, 0), 100);
    this.hemi = new THREE.HemisphereLight('#e8efe9', '#3a3530', 2.2);
    this.sun = new THREE.DirectionalLight('#ffffff', 1.6); this.sun.position.set(18, 40, 10);
    this.ambient = new THREE.AmbientLight('#8fa3ff', 0.03);
    this.scene.add(this.hemi, this.sun, this.ambient);
    this.pool = Array.from({ length: LIGHT_POOL }, () => { const l = new THREE.PointLight('#fff', 0, 10, 2); l.visible = false; this.scene.add(l); return l; });
    // Факел в руке: без него катакомбы в игре тоже чёрные.
    this.handTorch = new THREE.PointLight('#ffb070', 1.5, HAND_TORCH_RANGE, 2); this.handTorch.visible = false; this.scene.add(this.handTorch);
    this.mats = {}; this.lineMats = {};
    Object.entries(CATS).forEach(([k, c]) => {
      this.mats[k] = c.glow ? new THREE.MeshBasicMaterial({ color: c.color, clippingPlanes: [this.clip] })
        : new THREE.MeshStandardMaterial({ color: c.color, roughness: 0.9, metalness: 0, transparent: !!c.alpha, opacity: c.alpha || 1, depthWrite: !c.alpha, clippingPlanes: [this.clip] });
      this.lineMats[k] = new THREE.LineBasicMaterial({ color: '#0b0f0e', transparent: true, opacity: 0.55, clippingPlanes: [this.clip] });
    });
    this.groups = {};
    Object.keys(CATS).forEach(k => { this.groups[k] = new THREE.Group(); this.scene.add(this.groups[k]); });
    this.selection = new THREE.LineSegments(new THREE.BufferGeometry(), new THREE.LineBasicMaterial({ color: '#7dff8f', depthTest: false }));
    this.selection.visible = false; this.selection.renderOrder = 10; this.scene.add(this.selection);
    this.rings = new THREE.Group(); this.rings.visible = false; this.scene.add(this.rings);
    this.isFp = false; this.isGameLight = false; this.keys = {}; this.yaw = 0; this.pitch = 0; this.lights = []; this.meshes = []; this.floorY = -1;
    this.ray = new THREE.Raycaster(); this.clock = new THREE.Clock(); this.lightTimer = 0;
    addEventListener('resize', () => this.resize());
    addEventListener('keydown', e => { if (!/INPUT|TEXTAREA|SELECT/.test(e.target.tagName)) this.keys[e.code] = true; });
    addEventListener('keyup', e => { this.keys[e.code] = false; });
    this.bindLook();
    this.loop();
  }

  bindLook() {
    const dom = this.renderer.domElement;
    let drag = null;
    dom.addEventListener('contextmenu', e => e.preventDefault());
    dom.addEventListener('pointerdown', e => { drag = { x: e.clientX, y: e.clientY, moved: 0 }; });
    addEventListener('pointerup', e => {
      if (drag && drag.moved < 5 && e.target === dom && !this.isFp && e.button === 0 && this.onPick) this.onPick(this.pick(e));
      drag = null;
    });
    addEventListener('pointermove', e => {
      if (!drag) return;
      const dx = e.clientX - drag.x, dy = e.clientY - drag.y;
      drag.moved += Math.abs(dx) + Math.abs(dy); drag.x = e.clientX; drag.y = e.clientY;
      if (this.isFp) { this.yaw -= dx * 0.004; this.pitch = Math.max(-1.5, Math.min(1.5, this.pitch - dy * 0.004)); }
    });
  }

  resize() {
    const w = this.el.clientWidth || innerWidth, h = this.el.clientHeight || innerHeight;
    this.renderer.setSize(w, h); this.camera.aspect = w / h; this.camera.updateProjectionMatrix();
  }

  load(items) {
    this.meshes.forEach(m => { m.parent.remove(m); m.geometry.dispose(); });
    this.meshes = []; this.lights = []; this.rings.clear(); this.selection.visible = false;
    const batches = new Map();
    items.forEach(item => item.objs.forEach(o => {
      const key = `${item.bucket || 0}|${o.cat}`;
      if (!batches.has(key)) batches.set(key, new Batch(o.cat, item));
      batches.get(key).add(o, item);
      if (o.cat === 'light') {
        const T = transformOf(o, item), [x, y, z] = T(0, 0, 0);
        this.lights.push({ pos: new THREE.Vector3(x, y, z), color: o.color, power: o.i * (LIGHT_POWER[o.name] || 3), r: o.r });
        const ring = new THREE.Mesh(new THREE.SphereGeometry(o.r, 18, 10), new THREE.MeshBasicMaterial({ color: o.color, wireframe: true, transparent: true, opacity: 0.1 }));
        ring.position.set(x, y, z); ring.raycast = () => {}; this.rings.add(ring);
      }
    }));
    batches.forEach((b, key) => {
      const g = new THREE.BufferGeometry();
      g.setAttribute('position', new THREE.Float32BufferAttribute(b.pos, 3));
      g.setAttribute('normal', new THREE.Float32BufferAttribute(b.nor, 3));
      const m = new THREE.Mesh(g, this.mats[b.cat]); m.userData = { faces: b.faces, cat: b.cat, place: b.place };
      const eg = new THREE.BufferGeometry(); eg.setAttribute('position', new THREE.Float32BufferAttribute(b.line, 3));
      const e = new THREE.LineSegments(eg, this.lineMats[b.cat]); e.raycast = () => {};
      m.add(e); this.groups[b.cat].add(m); this.meshes.push(m);
      if (key.endsWith('floor')) m.geometry.computeBoundingBox();
    });
  }

  setLayers(hidden) { Object.keys(this.groups).forEach(k => { this.groups[k].visible = !hidden.has(k); }); }
  setCut(y) { this.clip.constant = y; }
  setExposure(v) { this.renderer.toneMappingExposure = v; }
  setRings(on) { this.rings.visible = on; }
  setGameLight(on) {
    this.isGameLight = on; this.hemi.visible = this.sun.visible = !on; this.ambient.visible = on;
    this.scene.background.set(on ? '#000000' : '#0d1110');
    if (!on) this.pool.forEach(l => { l.visible = false; });
    this.lightTimer = 0;
  }

  frame(center, size) {
    this.isFp = false; this.orbit.enabled = true; this.orbit.enableRotate = true;
    this.orbit.target.set(center[0], 1, -center[1]);
    this.camera.position.set(center[0] + size * 0.75, size * 0.95, -center[1] + size * 0.75);
    this.camera.near = 0.3; this.camera.updateProjectionMatrix();
    this.orbit.update();
  }
  top(center, size) {
    this.isFp = false; this.orbit.enabled = true; this.orbit.enableRotate = false;
    this.orbit.target.set(center[0], 0, -center[1]);
    this.camera.position.set(center[0], size * 1.25, -center[1] + 0.01);
    this.orbit.update();
  }
  walk(x, y, z, yaw) {
    this.isFp = true; this.orbit.enabled = false; this.yaw = yaw || 0; this.pitch = 0; this.vy = 0;
    this.camera.near = 0.05; this.camera.updateProjectionMatrix();
    this.camera.position.set(x, y + EYE, -z);
  }

  solids(position) {
    return this.meshes.filter(m => {
      if (CATS[m.userData.cat].ghost || !this.groups[m.userData.cat].visible) return false;
      const p = m.userData.place;
      return p.reach === undefined || Math.hypot((p.x || 0) - position.x, (p.z || 0) + position.z) < p.reach;
    });
  }
  stepWalk(dt) {
    const speed = (this.keys.ShiftLeft || this.keys.ShiftRight ? 6.7 : 3.36) * dt, cam = this.camera;
    const f = new THREE.Vector3(-Math.sin(this.yaw), 0, -Math.cos(this.yaw)), r = new THREE.Vector3(-f.z, 0, f.x), mv = new THREE.Vector3();
    if (this.keys.KeyW) mv.add(f); if (this.keys.KeyS) mv.sub(f); if (this.keys.KeyD) mv.add(r); if (this.keys.KeyA) mv.sub(r);
    const S = this.solids(cam.position);
    if (mv.lengthSq()) {
      mv.normalize();
      const blocked = [0.5, 1.2].some(h => {
        this.ray.set(new THREE.Vector3(cam.position.x, cam.position.y - EYE + h, cam.position.z), mv); this.ray.far = speed + 0.35;
        return this.ray.intersectObjects(S, false).length > 0;
      });
      if (!blocked) cam.position.addScaledVector(mv, speed);
    }
    this.ray.set(new THREE.Vector3(cam.position.x, cam.position.y, cam.position.z), new THREE.Vector3(0, -1, 0)); this.ray.far = 30;
    const feet = cam.position.y - EYE, hit = this.ray.intersectObjects(S, false).find(h => h.point.y < feet + 0.5);
    if (hit) {
      if (hit.point.y >= feet - 0.05) { cam.position.y = hit.point.y + EYE; this.vy = 0; } else { this.vy = (this.vy || 0) - 18 * dt; cam.position.y = Math.max(hit.point.y + EYE, cam.position.y + this.vy * dt); }
    }
    cam.rotation.set(this.pitch, this.yaw, 0, 'YXZ');
    if (this.onWalk) this.onWalk(cam.position);
  }

  // Самые близкие источники: на подземелье их сотни, а шейдер держит только пул.
  updateLights(dt) {
    if (!this.isGameLight || (this.lightTimer -= dt) > 0) return;
    this.lightTimer = 0.15;
    const at = this.isFp ? this.camera.position : this.orbit.target;
    const near = this.lights.map(l => ({ l, d: l.pos.distanceToSquared(at) })).sort((a, b) => a.d - b.d).slice(0, LIGHT_POOL);
    this.pool.forEach((p, i) => {
      const n = near[i]; p.visible = !!n;
      if (n) { p.position.copy(n.l.pos); p.color.set(n.l.color); p.intensity = n.l.power; p.distance = n.l.r; }
    });
  }

  pick(e) {
    const r = this.renderer.domElement.getBoundingClientRect();
    const mouse = new THREE.Vector2((e.clientX - r.left) / r.width * 2 - 1, -(e.clientY - r.top) / r.height * 2 + 1);
    this.ray.setFromCamera(mouse, this.camera); this.ray.far = Infinity;
    const list = this.meshes.filter(m => this.groups[m.userData.cat].visible);
    const hit = this.ray.intersectObjects(list, false).find(h => h.point.y <= this.clip.constant);
    if (!hit) return null;
    return { o: hit.object.userData.faces[hit.faceIndex], place: hit.object.userData.place };
  }

  select(o, place) {
    if (!o) { this.selection.visible = false; return; }
    const b = new Batch(o.cat, place); b.add(o, place);
    this.selection.geometry.dispose();
    this.selection.geometry = new THREE.BufferGeometry().setAttribute('position', new THREE.Float32BufferAttribute(b.line, 3));
    this.selection.visible = true;
  }
  focus(o, place) {
    const [x, y, z] = transformOf(o, place)(0, (o.s ? o.s[1] : 0) / 2, 0), p = new THREE.Vector3(x, y, z);
    const d = this.camera.position.clone().sub(this.orbit.target);
    this.orbit.target.copy(p); this.camera.position.copy(p).add(d.setLength(Math.min(d.length(), 18)));
  }

  loop() {
    requestAnimationFrame(() => this.loop());
    if (!this.el.offsetParent) return;
    const dt = Math.min(this.clock.getDelta(), 0.05);
    if (this.isFp) this.stepWalk(dt); else this.orbit.update();
    this.updateLights(dt);
    this.handTorch.visible = this.isFp && this.isGameLight;
    this.handTorch.position.copy(this.camera.position);
    this.renderer.render(this.scene, this.camera);
    if (this.onFrame) this.onFrame();
  }
}

// --- 2D-план ---
const PLAN_DOT = { monster: 0.55, chest: 0.5, trap: 0.5, start: 0.6, light: 0.3 };

function planCorners(o, place) {
  const a = (o.ry || 0) * DEG, c = Math.cos(a), s = Math.sin(a), b = (place.yaw || 0) * DEG, pc = Math.cos(b), ps = Math.sin(b);
  const [w, , d] = o.s || [0, 0, 0], [px, , pz] = o.p;
  return [[-w, -d], [w, -d], [w, d], [-w, d]].map(([x, z]) => {
    const X = x / 2 * c + z / 2 * s + px, Z = -x / 2 * s + z / 2 * c + pz;
    return [X * pc + Z * ps + (place.x || 0), -X * ps + Z * pc + (place.z || 0)];
  });
}

// Разрез на высоте cut: всё, что начинается ниже него, снизу вверх. toScreen: (x, z) → [px, py], m — пикселей на метр.
function drawPlan(ctx, items, { cut, hidden, toScreen, m }) {
  const list = [];
  items.forEach(item => item.objs.forEach(o => {
    if (hidden.has(o.cat) || o.cat === 'ceiling' || o.p[1] > cut) return;
    const top = o.p[1] + (o.s ? o.s[1] : 0);
    list.push({ o, item, top: PLAN_DOT[o.cat] ? 1e4 : top });
  }));
  list.sort((a, b) => a.top - b.top);
  list.forEach(({ o, item }) => {
    const dot = PLAN_DOT[o.cat];
    ctx.fillStyle = CATS[o.cat].color;
    if (dot) {
      if (o.cat === 'start' && o.name === 'Spot') return;
      const [c] = [planCorners({ ...o, s: [0, 0, 0] }, item)[0]], [x, y] = toScreen(c[0], c[1]);
      ctx.beginPath(); ctx.arc(x, y, Math.max(1.5, dot * m), 0, 7); ctx.fill();
      return;
    }
    if (o.round) {
      const c = planCorners({ ...o, s: [0, 0, 0] }, item)[0], [x, y] = toScreen(c[0], c[1]);
      ctx.beginPath(); ctx.ellipse(x, y, o.s[0] / 2 * m, o.s[2] / 2 * m, 0, 0, 7); ctx.fill();
      return;
    }
    ctx.beginPath();
    planCorners(o, item).forEach((c, i) => { const [x, y] = toScreen(c[0], c[1]); if (i) ctx.lineTo(x, y); else ctx.moveTo(x, y); });
    ctx.closePath(); ctx.fill();
  });
}
