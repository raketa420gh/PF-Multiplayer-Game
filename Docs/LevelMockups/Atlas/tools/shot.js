// node shot.js <Room> <файл.png> ["cam=x,y,z;at=x,y,z;light=game;cut=6;show=ceiling;hide=monster,trap;expo=1.2;fov=60"] [ширина] [высота]
// Снимок макета модуля без интерфейса через Edge без окна. Координаты Unity (x восток, y вверх, z север); без cam — общий вид.
const { spawnSync } = require('child_process'), path = require('path'), fs = require('fs'), os = require('os');
const [room, out, args = '', w = 1280, h = 800] = process.argv.slice(2);
if (!room || !out) { console.log('node shot.js <Room> <out.png> ["cam=…;at=…"] [w] [h]'); process.exit(1); }
const edge = ['C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Microsoft/Edge/Application/msedge.exe'].find(p => fs.existsSync(p));
const url = `file:///${path.join(__dirname, '..', 'index.html').split(path.sep).join('/')}#room/${room}/shot/${args}`;
const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'atlas-shot-'));
const r = spawnSync(edge, ['--headless=new', `--screenshot=${path.resolve(out)}`, `--window-size=${w},${h}`, '--virtual-time-budget=12000', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--hide-scrollbars', '--allow-file-access-from-files', `--user-data-dir=${profile}`, url], { encoding: 'utf8', timeout: 180000 });
try { fs.rmSync(profile, { recursive: true, force: true }); } catch (e) { /* профиль ещё занят: уберёт система */ }
console.log(fs.existsSync(path.resolve(out)) ? `снимок: ${path.resolve(out)}` : `снимка нет: ${r.stderr || r.error}`);
