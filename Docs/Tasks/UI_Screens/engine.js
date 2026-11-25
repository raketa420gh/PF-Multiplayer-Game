// Mini uGUI: the same anchor / pivot / anchoredPosition / sizeDelta maths as DungeonUiBuilder, so its code ports line by line.
// Stage = the canvas at the reference resolution 1920 x 1080 (CanvasScaler, match height).
const V = (x, y) => [x, y];
const CENTER = V(.5, .5), TOP = V(.5, 1), BOTTOM = V(.5, 0), LEFT = V(0, .5), RIGHT = V(1, .5),
  TOPLEFT = V(0, 1), TOPRIGHT = V(1, 1), BOTTOMLEFT = V(0, 0), BOTTOMRIGHT = V(1, 0);
const COL = {
  panel: [.04, .035, .03, .92], panelLight: [.12, .1, .08, .95], frame: [.45, .36, .22, 1], gold: [.85, .72, .45, 1],
  text: [.95, .93, .88, 1], textDim: [.62, .58, .5, 1], black: [0, 0, 0, 1], white: [1, 1, 1, 1], clear: [0, 0, 0, 0],
};
const rgba = c => `rgba(${Math.round(Math.min(c[0], 1) * 255)},${Math.round(Math.min(c[1], 1) * 255)},${Math.round(Math.min(c[2], 1) * 255)},${c[3] ?? 1})`;
const hex = h => { h = h.replace('#', ''); if (h.length <= 4) h = [...h].map(c => c + c).join(''); const n = parseInt(h, 16), l = h.length; return l === 8 ? [(n >>> 24 & 255) / 255, (n >> 16 & 255) / 255, (n >> 8 & 255) / 255, (n & 255) / 255] : [(n >> 16 & 255) / 255, (n >> 8 & 255) / 255, (n & 255) / 255, 1]; };
// Sprites drawn in their own colours (the builder tints them white); the rest are white masks tinted by the image colour.
const OWN_COLOURS = new Set(['Panel', 'Hexagram', 'Shield']);

class Node {
  constructor(name, parent, o) {
    this.name = name; this.parent = parent; this.children = [];
    this.el = document.createElement('div');
    this.el.className = "n"; this.el.dataset.name = name; this.el.__node = this;
    if (parent) { parent.children.push(this); o.first ? parent.el.prepend(this.el) : parent.el.append(this.el); }
    this.anchor = o.anchor || CENTER; this.pivot = o.pivot || this.anchor; this.pos = o.pos || V(0, 0); this.size = o.size || V(0, 0);
    this.stretchPad = o.stretch; this.xStretch = o.xStretch; this.scale = o.scale || 1;
    this.layout();
    if (o.hidden) this.el.style.display = 'none';
  }
  layout() {
    const P = this.parent ? [this.parent.w, this.parent.h] : [1920, 1080];
    let x, y;
    if (this.stretchPad !== undefined) {
      const p = this.stretchPad; this.w = P[0] - 2 * p; this.h = P[1] - 2 * p; x = p; y = p;
    } else {
      this.w = this.xStretch ? P[0] + this.size[0] : this.size[0]; this.h = this.size[1];
      const ax = this.xStretch ? .5 : this.anchor[0];
      x = ax * P[0] + this.pos[0] - this.pivot[0] * this.w;
      y = (1 - this.anchor[1]) * P[1] - this.pos[1] - (1 - this.pivot[1]) * this.h;
    }
    Object.assign(this.el.style, { left: x + 'px', top: y + 'px', width: this.w + 'px', height: this.h + 'px' });
    if (this.scale !== 1) { this.el.style.transform = `scale(${this.scale})`; this.el.style.transformOrigin = `${this.pivot[0] * 100}% ${(1 - this.pivot[1]) * 100}%`; }
    return this;
  }
  hide() { this.el.style.display = 'none'; return this; }
  get path() { return (this.parent && this.parent.parent ? this.parent.path + '/' : '') + this.name; }
}

function rect(name, parent, anchor, pos, size, o = {}) { return new Node(name, parent, { ...o, anchor, pos, size }); }

function paint(n, color, o) {
  let s = n.el.style;
  const c = color || COL.white;
  if (o.sprite === 'Knob') s.borderRadius = '50%';
  if (o.sprite === 'UISprite') s.borderRadius = '4px';
  if (o.sprite && SPR[o.sprite]) {
    if (OWN_COLOURS.has(o.sprite)) { s.backgroundImage = `url(${SPR[o.sprite]})`; s.backgroundSize = '100% 100%'; s.opacity = c[3]; }
    else {
      // A mask would clip the children too, so the tinted sprite gets a layer of its own.
      const layer = document.createElement('div'); layer.className = 'spr'; n.el.prepend(layer); s = layer.style;
      s.background = rgba(c); s.webkitMaskImage = s.maskImage = `url(${SPR[o.sprite]})`; s.webkitMaskSize = s.maskSize = '100% 100%';
    }
  } else if (o.src) {
    s.backgroundImage = `url(${o.src})`; s.backgroundSize = o.cover ? 'cover' : 'contain'; s.backgroundRepeat = 'no-repeat'; s.backgroundPosition = 'center';
    if (color) s.filter = `opacity(${c[3]})`;
  } else s.background = rgba(c);
  if (o.outline) s.boxShadow = 'inset 0 0 0 1px ' + o.outline;
  if (o.fill !== undefined) {
    const f = Math.max(0, Math.min(1, o.fill));
    if (o.radial) { const m = `conic-gradient(#000 ${f * 360}deg, transparent 0)`; s.webkitMaskImage = s.maskImage = (s.maskImage && s.maskImage !== 'none' ? s.maskImage + ',' : '') + m; s.borderRadius = '50%'; }
    else s.clipPath = `inset(0 ${(1 - f) * 100}% 0 0)`;
  }
}

function img(name, parent, anchor, pos, size, color, o = {}) { const n = rect(name, parent, anchor, pos, size, o); paint(n, color, o); n.el.classList.add('img'); return n; }

// TMP rich text → HTML: <color>, <b>, <i>, <size>, <sprite> is dropped, \n → <br>.
function rich(t) {
  return String(t).replace(/&/g, '&amp;').replace(/<(?!\/?(color|b|i|size|u|s|sup|sub|alpha)\b)/g, '&lt;')
    .replace(/<color=("?)(#[0-9a-fA-F]{3,8}|\w+)\1>/g, (_, q, c) => `<span style="color:${c[0] === '#' ? rgba(hex(c)) : c}">`).replace(/<\/color>/g, '</span>')
    .replace(/<size=(\d+)%?>/g, (_, v) => `<span style="font-size:${v}${_.includes('%') ? '%' : 'px'}">`).replace(/<\/size>/g, '</span>')
    .replace(/<alpha=#([0-9a-fA-F]{2})>/g, (_, a) => `<span style="opacity:${parseInt(a, 16) / 255}">`).replace(/<\/alpha>/g, '</span>')
    .replace(/\n/g, '<br>');
}

const ALIGN = {
  Center: ['center', 'center'], MidlineLeft: ['left', 'center'], MidlineRight: ['right', 'center'], TopLeft: ['left', 'start'], Left: ['left', 'start'],
  Top: ['center', 'start'], Bottom: ['center', 'end'], BottomRight: ['right', 'end'], TopRight: ['right', 'start'], BottomLeft: ['left', 'end'],
};
function txt(name, parent, anchor, pos, size, fontSize, align, text = '', o = {}) {
  const n = rect(name, parent, anchor, pos, size, o), s = n.el.style, [h, v] = ALIGN[align];
  n.el.classList.add('txt');
  Object.assign(s, { fontSize: fontSize + 'px', textAlign: h, alignItems: v, justifyContent: h === 'left' ? 'flex-start' : h === 'right' ? 'flex-end' : 'center', color: rgba(o.color || COL.white) });
  if (o.bold) s.fontWeight = 'bold';
  if (o.italic) s.fontStyle = 'italic';
  if (o.margin) s.padding = `${o.margin[1]}px ${o.margin[2]}px ${o.margin[3]}px ${o.margin[0]}px`;
  if (o.lineSpacing) s.lineHeight = (1.17 + o.lineSpacing / 100) + '';
  n.span = document.createElement('span'); n.el.append(n.span);
  n.set = t => { n.span.innerHTML = rich(t); return n; };
  return n.set(text);
}

function btn(name, parent, anchor, pos, size, label, o = {}) {
  const n = img(name, parent, anchor, pos, size, o.color || COL.panelLight, { sprite: o.sprite || 'UISprite', ...o });
  n.el.classList.add('btn');
  n.label = txt('Label', n, CENTER, V(0, 0), V(0, 0), o.fs || 16, o.align || 'Center', label, { stretch: 2, color: o.textColor, bold: o.bold, margin: o.margin });
  return n;
}

function input(name, parent, anchor, pos, size, placeholder, o = {}) {
  const n = img(name, parent, anchor, pos, size, o.color || [.05, .04, .035, .96], o);
  const area = rect('Text Area', n, CENTER, V(0, 0), V(0, 0), { stretch: 6 });
  o.value ? txt('Text', area, CENTER, V(0, 0), V(0, 0), o.fs || 20, 'Center', o.value, { stretch: 0, color: COL.text })
    : txt('Placeholder', area, CENTER, V(0, 0), V(0, 0), 18, 'Center', placeholder, { stretch: 0, color: COL.textDim, italic: true });
  return n;
}

// Built-in DefaultControls.CreateSlider look: background bar, gold fill, round knob.
function slider(name, parent, anchor, pos, size, value) {
  const n = rect(name, parent, anchor, pos, size);
  img('Background', n, CENTER, V(0, 0), V(size[0], size[1] * .25), [.85, .85, .85, 1], { sprite: 'UISprite' });
  img('Fill', n, LEFT, V(5, 0), V((size[0] - 20) * value + 5, size[1] * .25), COL.gold, { sprite: 'UISprite', pivot: LEFT });
  img('Handle', n, LEFT, V(5 + (size[0] - 20) * value, 0), V(20, 20), COL.white, { sprite: 'Knob' });
  return n;
}

function stage(host) { const n = new Node('[UI]', null, { size: V(1920, 1080), anchor: BOTTOMLEFT }); n.el.classList.add('stage'); host.append(n.el); return n; }
