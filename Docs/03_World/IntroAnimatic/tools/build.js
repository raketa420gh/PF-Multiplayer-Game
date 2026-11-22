// Данные хаба для аниматика: export/hub.json макета «Устья» → data/hub.js. Скриптом, а не JSON, чтобы страница открывалась
// с диска без сервера. Перезапускать после каждой выгрузки макета: node Docs/03_World/IntroAnimatic/tools/build.js
const fs = require('fs'), path = require('path');
const src = path.join(__dirname, '../../../02_Gameplay/HubMockup/export/hub.json'), out = path.join(__dirname, '../data/hub.js');
const { meshes, geos, mats } = JSON.parse(fs.readFileSync(src, 'utf8'));
const round = v => Math.round(v * 100) / 100;
geos.forEach(g => { g.v = g.v.map(round); });
fs.mkdirSync(path.dirname(out), { recursive: true });
fs.writeFileSync(out, 'window.HUB=' + JSON.stringify({ meshes: meshes.map(({ m, p, q, k, g, s }) => ({ m, p, q, k, g, s })), geos, mats }) + ';\n');
console.log(`hub.js: ${(fs.statSync(out).size / 1024).toFixed(0)} KB, meshes ${meshes.length}, raw ${geos.length}`);
