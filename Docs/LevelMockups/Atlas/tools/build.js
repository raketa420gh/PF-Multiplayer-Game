// node build.js [Room ...] — собирает комнаты из tools/rooms в data/<Room>.js, проверяет и без аргументов ещё обновляет описания в data/titles.js.
const fs = require('fs'), path = require('path');
const { Room } = require('./roomkit');
const { validate } = require('./validate');
const roomsDir = path.join(__dirname, 'rooms'), dataDir = path.join(__dirname, '..', 'data');
const names = process.argv.slice(2).length ? process.argv.slice(2) : fs.readdirSync(roomsDir).filter(f => f.endsWith('.js')).map(f => f.slice(0, -3));
const titlesPath = path.join(dataDir, 'titles.js');
const ATLAS = {};
new Function('ATLAS', fs.readFileSync(titlesPath, 'utf8'))(ATLAS);
let failed = 0;

for (const name of names) {
  const spec = require(path.join(roomsDir, `${name}.js`));
  const room = new Room(name, spec.options);
  spec.build(room);
  room.finish();
  const { errors, warnings, stats } = validate(room);
  console.log(`${errors.length ? 'FAIL' : 'OK  '} ${name} ${JSON.stringify(stats)}`);
  errors.forEach(e => console.log(`   ✗ ${e}`));
  warnings.forEach(e => console.log(`   ! ${e}`));
  failed += errors.length ? 1 : 0;
  fs.writeFileSync(path.join(dataDir, `${name}.js`), room.toFile());
  if (room.text && ATLAS.titles[name] && !process.argv[2]) ATLAS.titles[name].desc = room.text;
}

if (!process.argv[2]) fs.writeFileSync(titlesPath, `ATLAS.titles={${Object.keys(ATLAS.titles).map(k => `${JSON.stringify(k)}:${JSON.stringify(ATLAS.titles[k])}`).join(',\n')}};\n`);
process.exit(failed ? 1 : 0);
