// node build.js [Room ...] — собирает комнаты из tools/rooms: блокаут в data/<Room>.js (только у комнат с build), вид в data/detail/<Room>.js
// и data/looks.js, проверяет и без аргументов ещё обновляет описания в data/titles.js.
// Комната без build (только dress) одевается поверх готового data/<Room>.js, сам файл не трогается.
const fs = require('fs'), path = require('path');
const { Room } = require('./roomkit');
const { validate } = require('./validate');
const { dress, floaters } = require('./dress');
const roomsDir = path.join(__dirname, 'rooms'), dataDir = path.join(__dirname, '..', 'data'), detailDir = path.join(dataDir, 'detail');
const names = process.argv.slice(2).length ? process.argv.slice(2) : fs.readdirSync(roomsDir).filter(f => f.endsWith('.js')).map(f => f.slice(0, -3));
const titlesPath = path.join(dataDir, 'titles.js'), looksPath = path.join(dataDir, 'looks.js');
const ATLAS = {};
new Function('ATLAS', fs.readFileSync(titlesPath, 'utf8'))(ATLAS);
fs.mkdirSync(detailDir, { recursive: true });
let failed = 0;

/// Запись целиком: сборки разных комнат могут идти одновременно.
const writeWhole = (file, text) => { const tmp = `${file}.${process.pid}.tmp`; fs.writeFileSync(tmp, text); fs.renameSync(tmp, file); };
/// Детали компактно: таблицы имён и материалов, строка [имя, материал, x, y, z, w, h, d, ry | [кватернион], флаги]: 1 — круглая, 2 — на верхе стен (слой «Верх стен»).
const packDetail = (name, decor) => {
  const names = [], mats = [], ix = (list, v) => { let i = list.indexOf(v); if (i < 0) { i = list.length; list.push(v); } return i; };
  const rows = decor.map(o => JSON.stringify([ix(names, o.name), ix(mats, o.m || ''), ...o.p, ...o.s, o.q || o.ry || 0, ...(o.round || o.up ? [(o.round ? 1 : 0) | (o.up ? 2 : 0)] : [])]));
  return `ATLAS.detail(${JSON.stringify(name)},{"names":${JSON.stringify(names)},"mats":${JSON.stringify(mats)},"items":[\n${rows.join(',\n')}\n]});\n`;
};

for (const name of names) {
  const spec = require(path.join(roomsDir, `${name}.js`));
  const room = new Room(name, spec.options);
  let report = { errors: [], warnings: [], stats: {} };
  if (spec.build) {
    spec.build(room);
    room.finish();
    report = validate(room);
    fs.writeFileSync(path.join(dataDir, `${name}.js`), room.toFile());
  } else {
    const loaded = { room: (n, a) => { room.items = a; } };
    new Function('ATLAS', fs.readFileSync(path.join(dataDir, `${name}.js`), 'utf8'))(loaded);
  }
  // Вид: материалы и скрытые тела, которые назначило одевание, уходят в looks.js, а не в блокаут.
  const before = new Map(room.items.map(o => [o, [o.m, o.hide]]));
  if (spec.dress) dress(room, spec);
  const decor = room.items.filter(o => o.cat === 'decor'), look = { detail: decor.length, side: room.side };
  for (const [o, [m, hide]] of before) {
    // Ключ «категория:id»: в выгрузке из Unity id лестниц и стартов совпадают (St1).
    if (o.m !== m) { look.m = look.m || {}; (look.m[o.m] = look.m[o.m] || []).push(o.cat + ':' + o.id); }
    if (o.hide && !hide) (look.hide = look.hide || []).push(o.cat + ':' + o.id);
  }
  if (decor.length) { writeWhole(path.join(detailDir, `${name}.js`), packDetail(name, decor)); writeWhole(path.join(detailDir, `${name}.look.json`), JSON.stringify(look)); report.stats.decor = decor.length; }
  const loose = spec.dress ? floaters(room) : [];
  if (loose.length) report.warnings.push(`в воздухе ${loose.length}: ${loose.slice(0, 8).join('; ')}`);
  if (spec.dress && room.scene.plain().length) report.warnings.push(`пропсы без набора частей: ${room.scene.plain().join(', ')}`);
  console.log(`${report.errors.length ? 'FAIL' : 'OK  '} ${name} ${JSON.stringify(report.stats)}`);
  report.errors.forEach(e => console.log(`   ✗ ${e}`));
  report.warnings.forEach(e => console.log(`   ! ${e}`));
  failed += report.errors.length ? 1 : 0;
  if (room.text && ATLAS.titles[name] && !process.argv[2]) ATLAS.titles[name].desc = room.text;
}

// looks.js собирается из записей всех одетых комнат.
const looks = fs.readdirSync(detailDir).filter(f => f.endsWith('.look.json')).sort().map(f => `${JSON.stringify(f.slice(0, -10))}:${fs.readFileSync(path.join(detailDir, f), 'utf8')}`);
writeWhole(looksPath, `ATLAS.looks={${looks.join(',\n')}};\n`);
if (!process.argv[2]) fs.writeFileSync(titlesPath, `ATLAS.titles={${Object.keys(ATLAS.titles).map(k => `${JSON.stringify(k)}:${JSON.stringify(ATLAS.titles[k])}`).join(',\n')}};\n`);
process.exit(failed ? 1 : 0);
