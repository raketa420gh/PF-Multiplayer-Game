// Шоты аниматика «Договор». Каждый кадр — чистая функция времени: draw(t) ставит площадку, болванки, свет и камеру по секунде ролика,
// поэтому перемотка и раскадровка (#sheet) дают тот же кадр, что и проигрывание. Таймкоды — из Docs/03_World/Intro_Cinematic.md.
'use strict';
const DUR = 60, S = {}, state = { fade: 0, flash: 0, logo: 0, shot: 0 };
let renderer;

const PN = 400, pgeo = new THREE.BufferGeometry(); pgeo.setAttribute('position', new THREE.BufferAttribute(new Float32Array(PN * 3), 3));
const pmat = new THREE.PointsMaterial({ size: .05, transparent: true, depthWrite: false }), pts = new THREE.Points(pgeo, pmat); pts.frustumCulled = false; W.add(pts);
function dust(n, fn, color, size, opacity) {
  const a = pgeo.attributes.position.array;
  for (let i = 0; i < n; i++) { const p = fn(i); a[i * 3] = p[0]; a[i * 3 + 1] = p[1]; a[i * 3 + 2] = p[2]; }
  pgeo.setDrawRange(0, n); pgeo.attributes.position.needsUpdate = true; pmat.color.set(color); pmat.size = size; pmat.opacity = opacity; pts.visible = true;
}
// Разлёт от точки: искры и крошка после удара.
const burst = (n, o, dt, speed, color, size) => dust(n, i => { const a = rnd(i) * 6.283, b = rnd(i, 1) * 2 - 1, r = Math.sqrt(1 - b * b), v = speed * (.3 + rnd(i, 2)) * dt; return [o[0] + Math.cos(a) * r * v, o[1] + b * v - 4 * dt * dt, o[2] + Math.sin(a) * r * v]; }, color, size, Math.max(0, 1 - dt * 1.6));

function use(name) { for (const k in S) S[k].visible = k === name; return S[name]; }
function clip(near, far) { camera.near = near; camera.far = far; }
function env(bg, near, far, sky, ground, power, sunColor = 0xffffff, sunPower = 0, dir = [0, 1, 0]) {
  scene.background.set(bg); scene.fog.color.set(bg); scene.fog.near = near; scene.fog.far = far;
  hemi.color.set(sky); hemi.groundColor.set(ground); hemi.intensity = power; sun.color.set(sunColor); sun.intensity = sunPower; sun.position.set(dir[0], dir[1], -dir[2]);
}
function pl(i, p, color, power, reach) { const l = PL[i]; l.position.set(p[0], p[1], p[2]); l.color.set(color); l.intensity = power; l.distance = reach; }
function look(p, t, fov = 40, up) { camera.position.set(p[0], p[1], -p[2]); camera.up.set(up ? up[0] : 0, up ? up[1] : 1, up ? -up[2] : 0); camera.lookAt(t[0], t[1], -t[2]); camera.fov = fov; }
function shake(a, t) { camera.position.x += a * wob(t * 23, 1); camera.position.y += a * wob(t * 27, 2); camera.rotateZ(a * .25 * wob(t * 19, 3)); }
const flick = (t, k = 0) => 1 + .16 * wob(t * 9, k);
const flames = (s, floor) => s.flames.forEach(f => { f.visible = f.userData.y > floor; });
function ins(parts) { const s = use('insert'); clip(.02, 30); for (const k of ['niche', 'ribs', 'skull', 'bag', 'shroud', 'drop', 'hand']) s[k].visible = parts.includes(k); return s; }

function shaftBase(part, hemiPower) {
  const s = use('shaft'); clip(.1, 220); s.proc.visible = part === 'proc'; s.chain.visible = part === 'chain'; s.fall.visible = part === 'fall'; flames(s, -9);
  env(0x050506, 8, 90, 0x54493c, 0x0a0908, hemiPower);

  return s;
}
const CREW_Z = [10.85, 10.3, 10.65];
function nichesBase() {
  const s = use('niches'); clip(.1, 120); flames(s, -9); s.bones.visible = false; s.slab.position.z = 14.6; s.portal.visible = false;
  env(0x030304, 4, 34, 0x3a352e, 0x070606, .3);

  return s;
}
// Трое в коридоре: ведущий на xL, остальные через gap позади; факел у замыкающего.
function crew(s, t, xL, gap, ph, amp, lean) { s.crew.forEach((d, i) => actor(d, xL - i * gap, 0, CREW_Z[i], 90, ph + i * 2, amp, lean)); torchLight(s, t, 1); }
function torchLight(s, t, k) { pl(0, torchAt(s.crew[2]), 0xffb060, 30 * flick(t) * k, 13); }

// Пролёт над «Устьем»: время, точка камеры, точка взгляда. Последний ключ — стартовый кадр шота с логотипом.
const FLY = [
  [46, [45.6, -.9, -99.5], [44, -2, -90]], [48.2, [45.9, -.2, -93], [44, -1, -83]], [49.4, [46, 6.5, -84], [43.5, 5, -66]], [50.3, [44.2, 13, -70], [44.5, 3, -50]],
  [51.3, [44.5, 11.5, -55], [41, 2, -36]], [52.3, [42.5, 8.5, -41], [40, 3, -26]], [53.2, [42.6, 5.6, -29], [40.5, 4, -12]], [54.2, [40.8, 3.4, -21], [40.5, 4.2, -9]], [55, [40.5, 2.9, -18.5], [40.5, 4.3, -9]]
];
const flyP = new THREE.CatmullRomCurve3(FLY.map(k => new THREE.Vector3(...k[1])), false, 'centripetal'), flyT = new THREE.CatmullRomCurve3(FLY.map(k => new THREE.Vector3(...k[2])), false, 'centripetal');
function hubBase(t) {
  const s = use('hub'); clip(.3, 900); env(0x8793a6, 40, 420, 0xaab6c8, 0x4a4540, 1.45, 0xdfe6f2, .9, [-.4, .8, -.5]);
  pl(0, [45.8, 1.4, -50.2], 0xff9a50, 60 * flick(t), 20); pl(1, [28.8, 1.6, -41.2], 0xff8a40, 55 * flick(t, 2), 16); pl(2, [35.6, 1.5, -7.6], 0xffa860, 30, 14); pl(3, [45.4, 1.5, -7.6], 0xffa860, 30, 14); pl(4, [40.5, 3, 1.5], 0x6fb8ff, 14, 18);
  const z = -93 + 3.2 * (t - 46), y = Math.min(0, (z + 66) * .118);
  s.cart.position.set(44.2, y, z); s.cart.rotation.x = y < 0 ? -Math.atan(.118) : 0; s.wheels.forEach(w => { w.rotation.x = t * 5.8; });
  dust(30, i => [30.2 + rnd(i) * .8, 1.1 + ((rnd(i, 1) + t * (.8 + rnd(i, 2))) % 1) * 2.4, -40.4 + rnd(i, 3) * .8], 0xffb060, .22, .9);

  return s;
}

const SHOTS = [
  { t0: 0, id: '01', name: 'Весы', pic: 'Макро из черноты: рука в перчатке кладёт на латунную чашу фалангу, в сколе кости — серебристый кристалл. Чаша со столбиком монет взлетает.', snd: 'Скрип цепи.',
    draw(t) {
      const s = use('scale'), land = 1.7; clip(.02, 30); env(0x000000, 3, 12, 0x5a4a3a, 0x000000, .22); pl(0, [-.5, 1.1, -.6], 0xffc890, 2.6, 5); pl(1, [.9, .6, .7], 0x6f86a8, .7, 4);
      const a = lerp(-.2, .25, sm(seg(t, land, land + .45))) + (t > land ? .09 * Math.exp(-2.5 * (t - land)) * Math.sin((t - land) * 9) : 0), ex = .32 * Math.cos(a), ey = .32 * Math.sin(a);
      s.beam.rotation.z = a; s.panL.position.set(-ex, .55 - ey, 0); s.panR.position.set(ex, .55 + ey, 0);
      const rest = [-ex, .55 - ey - .265, 0], above = [rest[0] - .01, rest[1] + .04, rest[2] - .06];
      const hp = t < land + .15 ? mix([-.8, .72, -.5], above, sm(seg(t, .4, land))) : mix(above, [-.95, .8, -.6], sm(seg(t, land + .15, land + 1.3)));
      s.hand.position.set(...hp); s.hand.rotation.set(.25, .5, 0);
      s.bone.position.set(...(t < land ? [hp[0] + .01, hp[1] - .04, hp[2] + .06] : rest)); s.bone.rotation.y = .5;
      look(mix([-.05, .52, -1.55], [.08, .47, -1.2], sm(seg(t, 0, 5))), mix([-.1, .36, 0], [.04, .4, 0], sm(seg(t, 1.2, 4.6))), 30);
      state.fade = 1 - seg(t, 0, .7);
    } },
  { t0: 5, id: '02', name: 'Хребет', pic: 'Хребет на рассвете, гряда читается как позвоночник. Наезд на чёрный зев у подножия.', snd: 'Вступает низкий дрон.',
    draw(t) {
      use('ridge'); clip(2, 5000); const u = seg(t, 5, 9), k = sm(u);
      env(new THREE.Color(0x1c2030).lerp(new THREE.Color(0x8a6258), k * .8), 400, 3200, 0x4a5070, 0x0c0c10, .55 + .3 * k, 0xff9a5a, 1.6 + k, [1, .22, .45]);
      look(mix([-520, 95, 230], [0, 6.5, 141], u * u * (.35 + .65 * u)), mix([20, 70, 600], [0, 7, 150], k), 36);
    } },
  { t0: 9, id: '03', name: 'Морнхейм', pic: 'Шахта Великого зала сверху вниз, ярусы ниш, процессия с факелами несёт тела в саванах. Костевед ставит метку в книге.', snd: 'Дрон, шаги.',
    draw(t) {
      const s = shaftBase('proc', .6), z0 = -2 + (t - 9);
      s.litters.forEach((l, j) => { const z = z0 - 4.5 * j; l.position.set(8, 0, z); actor(s.bearers[j * 2], 8, 0, z + 1.5, 0, t * 5 + j, .3); actor(s.bearers[j * 2 + 1], 8, 0, z - 1.5, 0, t * 5 + j + 3, .3); });
      actor(s.torchmen[0], 6.7, 0, z0 + 2.2, 0, t * 5, .3); actor(s.torchmen[1], 9.3, 0, z0 - 6.8, 0, t * 5 + 2, .3);
      actor(s.reader, 6.2, 0, 3, 90); s.reader.userData.armR.rotation.x = -1.05 + .07 * Math.sin(t * 10);
      pl(0, torchAt(s.torchmen[0]), 0xffb060, 30 * flick(t), 13); pl(1, torchAt(s.torchmen[1]), 0xffb060, 30 * flick(t, 1), 13);
      pl(2, [0, 2.4, -6], 0xff9940, 26, 12); pl(3, [0, 2.4, 7], 0xff9940, 26, 12); pl(4, [15.2, 3.2, 12], 0xffc280, 22, 12); pl(5, [0, 3.2, -15.2], 0xffc280, 22, 12);
      if (t < 13.4) { const u = seg(t, 9, 13.4); look(mix([4.5, 17.2, 4.5], [6.5, 12.8, 2.5], sm(u)), mix([7, 0, 1.5], [8, 0, 1], u), 50, [Math.sin(u * .5), 0, Math.cos(u * .5)]); }
      else look(mix([5.5, 1.6, 5.2], [5.7, 1.58, 4.9], seg(t, 13.4, 15)), [7, 1.15, 2.6], 34);
      state.fade = 1 - seg(t, 9, 9.5);
    } },
  { t0: 15, id: '04', name: 'Кандалы', pic: 'По тем же ступеням вниз идут ноги в кандалах.', snd: 'Звенья цепи в ритм.',
    draw(t) {
      const s = shaftBase('chain', .45); pl(0, [-14.4, 3, -7.4], 0xffb060, 16, 10);
      s.shackled.forEach((d, i) => { const z = -1 - (t - 15) + i * 1.2; actor(d, -15.6, cl((z + 6) / 7.5) * 6, z, 180, t * 5.2 + i * 1.7, .36); });
      look(mix([-15.5, 1, -9.2], [-15.5, .9, -8.8], seg(t, 15, 18)), [-15.6, 2.3, -3.5], 34);
    } },
  { t0: 18, id: '05', name: 'Первая Осыпь', pic: 'С потолка сыплется пыль, кладка едет вбок и закрывает проём; под саваном сжимается кисть голой кости. Огни шахты гаснут волной снизу вверх.', snd: 'Музыка обрывается, гул камня.',
    draw(t) {
      if (t < 20.2) {
        const s = shaftBase('fall', .5), run = t < 19.5; pl(0, [8.4, 3, -12.5], 0xffb060, 22, 11);
        actor(s.runner, 8.4, 0, lerp(-8, -14.9, seg(t, 18, 19.5)), 180, t * 13, run ? .8 : 0, run ? .2 : -.1);
        s.slab.position.x = lerp(12.6, 8.4, Math.pow(seg(t, 18.5, 19.45), 2));
        dust(260, i => [8 + (rnd(i) - .5) * 9, 6.5 - ((rnd(i, 1) * 6.5 + (t - 17) * (1.2 + rnd(i, 2) * 1.6)) % 6.5), -13 + (rnd(i, 3) - .5) * 8], 0xb8b0a0, .07, .5);
        look(mix([5, 1.55, -9], [5.5, 1.45, -10.4], seg(t, 18, 20.2)), [8.2, 1.7, -16.5], 38); shake(.03 + .05 * seg(t, 18, 19.4) + (t > 19.45 ? .12 * Math.exp(-6 * (t - 19.45)) : 0), t);
      } else if (t < 21.8) {
        const s = ins(['shroud']), k = sm(seg(t, 20.7, 21.35)); env(0x040404, 3, 14, 0x3a342c, 0x000000, .3); pl(0, [.9, 1.2, -.8], 0xffb060, 3.2 * flick(t), 5);
        s.claw.fingers.forEach((f, i) => { f.rotation.x = k * (1.25 + i * .08); });
        dust(60, i => [(rnd(i) - .5) * 2, 1.6 - ((rnd(i, 1) * 1.6 + t * (.5 + rnd(i, 2) * .6)) % 1.6), (rnd(i, 3) - .5) * 1.6], 0xb8b0a0, .012, .5);
        look(mix([1.25, .75, -.55], [1.15, .68, -.45], seg(t, 20.2, 21.8)), [.6, .27, .2], 30); shake(.012, t);
      } else {
        // Тот же вид сверху, что у процессии: носилки брошены, огни гаснут снизу вверх — к камере.
        const out = lerp(-1, 18, seg(t, 22, 23.5)), s = shaftBase('proc', .55 * (1 - seg(t, 22, 23.6))); flames(s, out);
        s.litters.forEach((l, j) => l.position.set(8, -.7, 4 - 4.5 * j)); [...s.bearers, ...s.torchmen, s.reader].forEach(d => { d.visible = false; });
        [[0, 2.4, -6], [0, 2.4, 7], [15.2, 3.2, 12], [15.2, 9.2, -12], [-15.2, 9.2, -12], [15.2, 15.2, 3]].forEach((p, i) => pl(i, p, 0xffc280, p[1] > out ? 26 : 0, 14));
        dust(240, i => [6 + (rnd(i) - .5) * 18, 16 - ((rnd(i, 1) * 16 + t * (2 + rnd(i, 2) * 2)) % 16), 3 + (rnd(i, 3) - .5) * 18], 0xb8b0a0, .09, .5);
        look(mix([5.5, 16.5, 3.5], [5, 17.2, 4], seg(t, 21.8, 24)), [7.5, 0, 1], 52, [.2, 0, 1]); shake(.04, t); state.fade = seg(t, 23.5, 24);
      }
    } },
  { t0: 24, id: '06', name: 'Печать', pic: 'Запечатанные ворота, таймлапс: снег, мох, века. Кувалда с клеймом весов раскалывает свинцовую печать.', snd: 'Ветер, удар по свинцу.',
    draw(t) {
      const s = use('seal'), lapse = t < 26.6, c = seg(t, 24, 26.6), day = lapse ? .35 + .65 * (.5 + .5 * Math.sin(c * 25)) : 1, hit = 27.25; clip(.1, 300);
      env(new THREE.Color(0x8792a0).multiplyScalar(day), 14, 120, 0xaab4c4, 0x3a3632, 1.3 * day, 0xfff0d8, 1.2 * day, lapse ? [Math.cos(c * 25), .7, -.6 + Math.sin(c * 25) * .4] : [.5, .8, -.6]);
      const snow = lapse ? Math.pow(Math.max(0, Math.sin(c * Math.PI * 3)), .6) : 0, moss = sm(seg(t, 24.6, 26.6)), k = seg(t, hit, 27.95);
      s.snow.forEach((m, i) => { const h = [.45, .5, .9][i] * snow + .001; m.scale.y = h; m.position.y = [0, 8.6, 0][i] + h / 2; m.visible = snow > .01; });
      s.moss.forEach((m, i) => { const h = (i < 2 ? 2.6 : 1.6) * moss + .001; m.scale.y = h; m.position.y = h / 2; });
      s.hammer.visible = !lapse; s.hammer.rotation.y = lerp(-.9, .66, Math.pow(seg(t, 26.85, hit), 2.2));
      s.halves.forEach((h, i) => { const side = i ? 1 : -1; h.position.set(side * .55 * k, -2.2 * k * k, -.3 * k); h.rotation.z = -side * .9 * k; });
      if (lapse) look(mix([0, 2.4, -10], [0, 2.5, -7], c), [0, 3.4, .7], 40);
      else { look(mix([-.9, 2.5, -3.4], [-.8, 2.5, -3.1], seg(t, 26.6, 28)), [.15, 2.6, .7], 36); if (t > hit) { shake(.1 * Math.exp(-7 * (t - hit)), t); burst(90, [.1, 2.6, .5], t - hit, 3, 0xb8b0a0, .05); state.flash = .5 * Math.exp(-14 * (t - hit)); } }
    } },
  { t0: 28, id: '07', name: 'Жильщики', pic: 'Сегодня. Трое жильщиков со спины, с факелом в катакомбах. Рука выламывает из грудной клетки ребро с серебряной жилкой и бросает в мешок. Ещё. Ещё.', snd: 'Перкуссия-пульс.',
    draw(t) {
      if (t < 30.6) { const s = nichesBase(), xL = -10 + 1.5 * (t - 28); crew(s, t, xL, 1.4, t * 5.4, .38, 0); look([xL - 6.4, 2, 10.3], [xL + 1.5, 1.25, 10.7], 42); state.fade = 1 - seg(t, 28, 28.3); return; }
      const s = ins(['niche', 'ribs', 'bag', 'hand']), rest = [.5, .9, -.55], bag = [-.3, 1, -.35]; let hp = rest;
      env(0x030304, 3, 12, 0x3a352e, 0x000000, .25); pl(0, [-.7, 1.7, -1.1], 0xffb060, 5 * flick(t), 6); s.bag.position.set(-.3, .82, -.35);
      s.torn.forEach((r, j) => {
        const q = (t - 30.75 - j * .75) / .75, G = [r.home.x, r.home.y + .27, r.home.z - .17];
        if (q < .3) { r.position.copy(r.home); r.rotation.set(0, 0, 0); if (q >= 0) hp = mix(rest, G, sm(q / .3)); }
        else if (q < .85) { const y = seg(q, .3, .45), k = sm(seg(q, .45, .85)), h = mix([G[0], G[1], G[2] - .1 * y], bag, k); hp = h; r.position.set(h[0], h[1] - .27, h[2] + .17); r.rotation.set(-.6 * y, 0, 1.2 * k); }
        else { r.position.set(-.38 + j * .07, .74, -.35); r.rotation.set(0, 0, 1.35); if (q < 1) hp = mix(bag, rest, sm(seg(q, .85, 1))); }
      });
      s.hand.position.set(...hp); s.hand.rotation.set(-.3, -.2, 0);
      look(mix([.12, 1.3, -1.25], [.1, 1.27, -1.1], seg(t, 30.6, 33)), [0, .98, .3], 46);
    } },
  { t0: 33, id: '08', name: 'Пыль', pic: 'Струйка пыли падает на плечо. Жильщик поднимает голову. Череп в нише поворачивается.', snd: 'Пульс тише, шорох.',
    draw(t) {
      if (t >= 35.2) {
        const s = ins(['niche', 'skull']), k = sm(seg(t, 35.3, 35.75)); env(0x030304, 3, 12, 0x3a352e, 0x000000, .22); pl(0, [-.8, 1.6, -1.2], 0xffb060, 4 * flick(t), 6);
        s.skull.rotation.y = lerp(1.25, 0, k); s.eyes.forEach(e => { e.visible = t > 35.72; });
        look(mix([.05, 1.12, -1], [.03, 1.1, -.72], seg(t, 35.2, 36)), [0, 1.03, .34], 32);

        return;
      }
      const s = nichesBase(), xL = -6.1, x = xL - 2.8, z = CREW_Z[2], up = sm(seg(t, 34.3, 34.75)), d = s.crew[2]; crew(s, t, xL, 1.4, 0, 0, 0);
      d.userData.hips.rotation.x = -.22 * up; d.userData.head.position.set(0, .84, -.07 * up);
      dust(70, i => [x + (rnd(i) - .5) * .07, 4.6 - ((rnd(i, 1) * 3 + t * (1.4 + rnd(i, 2))) % 3), z + .3 + (rnd(i, 3) - .5) * .07], 0xe8e0cc, .05, .95);
      if (t < 34.2) look([x + .95, 1.62, z + .9], [x, 1.55, z + .28], 34); else look([x + 1.5, 1, z + .75], [x, 1.75, z + .1], 34);
    } },
  { t0: 36, id: '09', name: 'Бегство', pic: 'Монтаж на удары: стена отрезает коридор, клинок бьёт в щит, бег, впереди синий портал. Двое влетают.', snd: 'Кульминация музыки.',
    draw(t) {
      const s = nichesBase();
      if (t < 37.5) {
        crew(s, t, -6.5 + 6 * (t - 36), 1.5, t * 14, .85, .22); s.slab.position.z = lerp(14.6, 10.9, Math.pow(seg(t, 36.25, 37.1), 2));
        look([-13.6, 1.6, 10.7], [0, 1.35, 10.7], 40); shake(.02 + (t > 37.1 ? .14 * Math.exp(-7 * (t - 37.1)) : 0), t);
      } else if (t < 38.6) {
        const hit = 38, dt = t - hit;
        actor(s.crew[0], .5 + 3 * (t - 37.5), 0, 10.4, 90, t * 14, .8, .2); actor(s.crew[1], -3.5, 0, 10.7, 90, 0, 0, .1); actor(s.crew[2], -6.4, 0, 10.4, 90); torchLight(s, t, 1);
        actor(s.bones, -2.25, 0, 10.7, 270, 0, 0, .15); s.bones.userData.armR.rotation.x = lerp(-2.7, -1.15, Math.pow(seg(t, 37.7, hit), 2));
        s.crew[1].userData.armL.rotation.x = -1.25 + (dt > 0 ? .3 * Math.exp(-8 * dt) : 0);
        look(mix([-5.4, 1.6, 11.3], [-5.2, 1.55, 11.25], seg(t, 37.5, 38.6)), [-2.8, 1.45, 10.8], 36);
        if (dt > 0) { burst(50, [-2.95, 1.5, 10.95], dt, 2.4, 0xffd890, .04); shake(.1 * Math.exp(-9 * dt), t); state.flash = .35 * Math.exp(-16 * dt); }
      } else if (t < 40.4) {
        const xL = -4 + 6.5 * (t - 38.6); crew(s, t, xL, 1.6, t * 15, .9, .25);
        actor(s.bones, xL - 7.5, 0, 10.7, 90, t * 12, .7, .2); s.bones.userData.armR.rotation.x = -1.6;
        look([xL + 4.4, 1.35, 11], [xL - 1.2, 1.3, 10.6], 40); shake(.05, t);
      } else {
        const x1 = 5 + 6.5 * (t - 40.4), x3 = -1.5 + 5 * (t - 40.4);
        s.portal.visible = true; s.portal.scale.setScalar(1 + .04 * Math.sin(t * 20)); pl(1, [12, 1.3, 10.7], 0x6fb8ff, 38, 15);
        actor(s.crew[0], x1, 0, CREW_Z[0], 90, t * 15, .9, .25); actor(s.crew[1], x1 - 2.1, 0, CREW_Z[1], 90, t * 15 + 2, .9, .25); actor(s.crew[2], x3, 0, CREW_Z[2], 90, t * 15 + 4, .9, .25);
        s.crew[0].visible = x1 < 12.1; s.crew[1].visible = x1 - 2.1 < 12.1; torchLight(s, t, 1);
        look([lerp(.5, 5.5, seg(t, 40.4, 42)), 1.75, 10.25], [12.4, 1.2, 10.7], 40); shake(.04, t);
        state.flash = .25 * Math.max(Math.exp(-14 * Math.abs(t - 41.49)), Math.exp(-14 * Math.abs(t - 41.82)));
      }
    } },
  { t0: 42, id: '10', name: 'Не вышел', pic: 'Третий в трёх шагах — портал схлопывается. Темнота, факел гаснет, в раскрытом мешке блестит оссарит.', snd: 'Полная тишина.',
    draw(t) {
      if (t < 43.7) {
        const s = nichesBase(), k = 1 - seg(t, 42.63, 42.81), x = Math.min(11.7, 6.5 + 5 * (t - 42)), run = x < 11.7;
        s.portal.visible = k > 0; s.portal.scale.setScalar(Math.max(.001, k)); pl(1, [12, 1.3, 10.7], 0x6fb8ff, 38 * k, 15);
        s.crew[0].visible = s.crew[1].visible = false; actor(s.crew[2], x, 0, CREW_Z[2], 90, t * 15 + 4, run ? .9 : 0, run ? .25 : -.05); torchLight(s, t, 1 - .5 * seg(t, 43, 43.7));
        look(mix([5.6, 1.6, 9.95], [6.4, 1.55, 10], seg(t, 42, 43.7)), [12.2, 1.25, 10.7], 38); if (run) shake(.03, t);

        return;
      }
      const s = ins(['drop']), die = seg(t, 43.7, 45.3); env(0x000000, 3, 12, 0x2a2622, 0x000000, .16 * (1 - die));
      pl(0, [-.27, .25, -.35], 0xff9a50, 2.4 * (1 - die) * (.7 + .3 * wob(t * 11, 4)), 4); s.ember.scale.setScalar(.16 * Math.max(.05, 1 - die)); s.ember.visible = die < 1;
      s.shards.forEach((m, i) => m.scale.setScalar((.03 + rnd(i, 3) * .03) * (.75 + .25 * Math.sin(t * 5 + i * 2.1))));
      look(mix([.55, .62, -1.25], [.45, .5, -1], sm(seg(t, 43.7, 46))), [.15, .1, .02], 34);
    } },
  { t0: 46, id: '11', name: 'Устье', pic: 'Жёсткая склейка: ветер, холодный свет, тракт в горах, повозка с новичками со спины. Камера обгоняет её, взмывает над воротами G1 и несётся над двором: Большие весы, костёр, искры кузни, башня часовни, вагонетка. Тормозит перед фасадом, вросшим в скалу.', snd: 'Ветер, колокол.',
    draw(t) {
      hubBase(t); let i = 0; while (i < FLY.length - 2 && t > FLY[i + 1][0]) i++;
      const u = (i + seg(t, FLY[i][0], FLY[i + 1][0])) / (FLY.length - 1); look(flyP.getPoint(u).toArray(), flyT.getPoint(u).toArray(), lerp(46, 40, seg(t, 49, 55))); if (t < 49) shake(.015, t);
    } },
  { t0: 55, id: '12', name: 'Логотип', pic: 'Камера замирает на тёмной арке. Удар колокола — логотип MARROWDEEP. Затемнение.', snd: 'Колокол, из темноты — голос.',
    draw(t) { hubBase(t); look(mix([40.5, 2.9, -18.5], [40.5, 2.95, -17.6], sm(seg(t, 55, 60))), [40.5, 4.3, -9], 40); state.logo = seg(t, 55.8, 56.8); state.fade = seg(t, 58.4, 59.6); } }
];
SHOTS.forEach((s, i) => { s.t1 = i + 1 < SHOTS.length ? SHOTS[i + 1].t0 : DUR; });

// Диктор — Кассиан Вейт, оценщик Дома Весов: начало, конец, строка.
const VO = [
  [.6, 4.7, 'Оссарит. Растёт в кости, что легла под Хребтом. Унция — дороже золота.'], [9.4, 14.6, 'Морнхейм тысячу лет хоронил в жиле своих мёртвых. И собирал урожай.'], [15.5, 17.8, 'Потом мёртвых стало мало.'],
  [20.3, 23.6, 'В одну ночь жила вернула всех. Империи не стало.'], [24.4, 26.3, 'Вход запечатали.'], [27.3, 28.5, 'Мы — вскрыли.'], [28.7, 32.3, 'Дом Весов платит за каждую унцию.'],
  [33.2, 35.6, 'Четверть часа — и приходит Осыпь.'], [43.9, 45.8, 'За тех, кто не вышел, Дом не платит.'], [52.4, 54.9, 'Тракт кончается здесь. Устье.'], [58.6, 60, 'Имя, жильщик?']
];
// Кадры раскадровки (#sheet): по одному на каждую склейку внутри шота.
const SHEET = [1, 2.4, 4.2, 5.4, 7.6, 8.8, 10, 12.8, 14.3, 16.5, 19.2, 21.3, 22.9, 25.2, 27.2, 27.5, 29.6, 31.4, 33.7, 34.9, 35.9, 36.9, 38.05, 39.5, 41.3, 42.4, 44.4, 47.2, 49.6, 50.5, 51.5, 52.5, 53.4, 54.6, 57.2];

function init(canvas) {
  renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  Object.assign(S, { scale: scaleSet(), ridge: ridgeSet(), shaft: shaftSet(), seal: sealSet(), niches: nichesSet(), insert: insertSet(), hub: hubSet() });
}
const shotAt = t => { let i = 0; while (i < SHOTS.length - 1 && t >= SHOTS[i].t1) i++; return i; };
function frame(t) {
  t = Math.min(Math.max(t, 0), DUR - .001); state.fade = state.flash = state.logo = 0; state.shot = shotAt(t); PL.forEach(l => { l.intensity = 0; }); pts.visible = false;
  SHOTS[state.shot].draw(t); camera.updateProjectionMatrix(); renderer.render(scene, camera);

  return state;
}

// Черновая звуковая дорожка на WebAudio: дрон, цепь, гул, удары, тишина, ветер, колокол — только чтобы проверить ритм монтажа.
const Snd = {
  ctx: null, bus: null,
  stop() { if (this.bus) { this.bus.disconnect(); this.bus = null; } },
  play(t0, rate) {
    this.stop(); const C = this.ctx || (this.ctx = new (window.AudioContext || window.webkitAudioContext)()); C.resume();
    const bus = this.bus = C.createGain(), now = C.currentTime + .06, at = t => now + (t - t0) / rate; bus.gain.value = .7; bus.connect(C.destination);
    if (!this.hiss) { this.hiss = C.createBuffer(1, C.sampleRate * 2, C.sampleRate); const d = this.hiss.getChannelData(0); for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1; }
    const voice = (src, end, a, b, peak, att, rel) => {
      const g = C.createGain(), s = Math.max(at(a), now), e = Math.max(at(b), s + .02), top = Math.min(s + att, e);
      g.gain.setValueAtTime(0, s); g.gain.linearRampToValueAtTime(peak, top); g.gain.setValueAtTime(peak, Math.max(top, e - rel)); g.gain.linearRampToValueAtTime(0, e); end.connect(g); g.connect(bus); src.start(s); src.stop(e + .05);
    };
    const tone = (a, b, f, peak, type = 'sine', att = .03, f1 = 0, rel = Math.min(.4, (b - a) / 2)) => { if (b <= t0) return; const o = C.createOscillator(); o.type = type; o.frequency.setValueAtTime(f, now); if (f1) { o.frequency.setValueAtTime(f, Math.max(at(a), now)); o.frequency.linearRampToValueAtTime(f1, at(b)); } voice(o, o, a, b, peak, att, rel); };
    const noise = (a, b, peak, type, f, q, att = .3) => { if (b <= t0) return; const n = C.createBufferSource(), fl = C.createBiquadFilter(); n.buffer = this.hiss; n.loop = true; fl.type = type; fl.frequency.value = f; fl.Q.value = q; n.connect(fl); voice(n, fl, a, b, peak, att, Math.min(.4, (b - a) / 2)); };
    const hit = (a, peak) => { tone(a, a + .45, 120, peak, 'sine', .005, 36); noise(a, a + .1, peak * .5, 'lowpass', 1200, .5, .003); };
    const click = (a, peak, f) => noise(a, a + .06, peak, 'bandpass', f, 4, .002);
    const bell = (a, peak) => [[1, 4.2], [2.01, 2.8], [2.76, 2], [4.1, 1.3], [5.43, .9]].forEach(([m, d], i) => tone(a, a + d, 196 * m, peak / (i + 1.2), 'sine', .004, 0, d - .01));
    click(.25, .25, 900); click(.9, .2, 700); hit(1.72, .25); tone(1.72, 2.7, 1320, .05, 'sine', .004, 0, .9); [2, 2.12, 2.3].forEach(t => click(t, .12, 5200));
    tone(5, 18, 55, .22, 'sine', 3); tone(5, 18, 82.4, .1, 'sine', 4); tone(9, 18, 110, .07, 'triangle', 5); noise(5, 9, .05, 'bandpass', 500, .6);
    for (let t = 15; t < 18; t += .375) click(t, .22, 2600);
    noise(18, 24, .5, 'lowpass', 110, .7); hit(19.45, .7); noise(22, 24, .2, 'lowpass', 300, .5);
    noise(24, 26.6, .12, 'bandpass', 600, .5); hit(27.25, 1); tone(27.25, 28.4, 196, .12, 'triangle', .004, 0, 1); tone(27.25, 28.2, 293, .06, 'sine', .004, 0, .9);
    for (let t = 28; t < 36; t += .75) hit(t, .22 + .03 * (t - 28)); [30.95, 31.7, 32.45].forEach(t => click(t, .35, 1800)); noise(33, 36, .07, 'highpass', 3000, .5);
    for (let t = 36, i = 0; t < 42; t += .375, i++) hit(t, i % 2 ? .3 : .6); tone(36, 42, 110, .09, 'sawtooth', .5, 220); hit(37.1, 1); click(38, .7, 3200); tone(38, 38.5, 880, .1, 'sine', .004, 0, .45); tone(40.4, 42.72, 330, .05, 'sine', .4, 392, .02);
    noise(46, 55.5, .22, 'bandpass', 420, .4); noise(46, 60, .08, 'bandpass', 900, .8); bell(50.3, .15); bell(55.6, .5);
  }
};
