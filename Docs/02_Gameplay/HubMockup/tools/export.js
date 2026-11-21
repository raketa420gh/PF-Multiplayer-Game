// Выгрузка макета хаба в игру: открывает index.html#export в headless Edge и пишет export/hub.json, который читает HubBlockoutBuilder
// (Tools/Game/Dungeon/Hub/Apply Mockup). Запуск: node Docs/02_Gameplay/HubMockup/tools/export.js
const { execFileSync } = require('child_process'), fs = require('fs'), os = require('os'), path = require('path');
const root = path.join(__dirname, '..'), edge = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', profile = fs.mkdtempSync(path.join(os.tmpdir(), 'hub-export-'));
const dom = execFileSync(edge, ['--headless=new', '--dump-dom', '--virtual-time-budget=30000', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--window-size=1280,720',
  `--user-data-dir=${profile}`, `file:///${path.join(root, 'index.html').replace(/\\/g, '/')}#export`], { maxBuffer: 1 << 28, encoding: 'utf8' });
const match = dom.match(/<pre id="export">([\s\S]*?)<\/pre>/);

if (!match)
  throw new Error('В DOM нет <pre id="export">: three.js не загрузился или в 3D-блоке ошибка');

const json = match[1].replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&'), data = JSON.parse(json);
fs.mkdirSync(path.join(root, 'export'), { recursive: true });
fs.writeFileSync(path.join(root, 'export', 'hub.json'), json);
console.log(`hub.json: ${(json.length / 1024).toFixed(0)} KB, meshes ${data.meshes.length}, raw ${data.geos.length}, materials ${data.mats.length}, lights ${data.lights.length}, markers ${data.markers.length}`);
