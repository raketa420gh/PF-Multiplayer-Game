// node vdata.js <Room> ... — проверка готового data/<Room>.js без конструктора.
const fs = require('fs'), path = require('path'), { validate } = require('./validate');
for (const name of process.argv.slice(2)) {
  const ATLAS = { room: (n, a) => { ATLAS.items = a; } };
  new Function('ATLAS', fs.readFileSync(path.join(__dirname, '..', 'data', `${name}.js`), 'utf8'))(ATLAS);
  const { errors, stats } = validate({ name, items: ATLAS.items, corner: false });
  console.log(name, JSON.stringify(stats)); errors.forEach(e => console.log('  ✗', e));
}
