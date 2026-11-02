// Порт CatacombGenerator.Lay / PlaceSides / CollectDoorways: тот же сид и тот же пул дают ту же раскладку, что в игре.
// Сверяется с Unity скриптом: один в один на эталонных сидах. Координаты Unity: x восток, z север, yaw по часовой.
(function (root) {
  const MBIG = 2147483647, MSEED = 161803398;

  // System.Random: алгоритм .NET с сидом.
  class DotNetRandom {
    constructor(seed) {
      const sa = this.sa = new Int32Array(56);
      const sub = seed === -2147483648 ? MBIG : Math.abs(seed);
      let mj = MSEED - sub, mk = 1;
      sa[55] = mj;
      for (let i = 1; i < 55; i++) {
        const ii = (21 * i) % 55;
        sa[ii] = mk; mk = mj - mk;
        if (mk < 0) mk += MBIG;
        mj = sa[ii];
      }
      for (let k = 1; k < 5; k++)
        for (let i = 1; i < 56; i++) {
          sa[i] -= sa[1 + (i + 30) % 55];
          if (sa[i] < 0) sa[i] += MBIG;
        }
      this.a = 0; this.b = 21;
    }
    sample() {
      if (++this.a >= 56) this.a = 1;
      if (++this.b >= 56) this.b = 1;
      let r = this.sa[this.a] - this.sa[this.b];
      if (r === MBIG) r--;
      if (r < 0) r += MBIG;
      this.sa[this.a] = r;
      return r * (1 / MBIG);
    }
    next(max) { return Math.trunc(this.sample() * max); }
    nextDouble() { return this.sample(); }
  }

  const STEPS = [[1, 0], [0, 1], [-1, 0], [0, -1]];
  const BOTH = 3;
  const float = Math.fround;

  // kinds: [{ name, weight, corner }] в порядке _kinds сцены.
  function lay(seed, { grid = 7, loops = 0.25, isLong = true, kinds }) {
    const random = new DotNetRandom(seed), count = grid * grid, key = Math.floor(count / 2);
    const neighbour = (cell, step) => {
      const x = cell % grid + step[0], z = Math.floor(cell / grid) + step[1];
      return x >= 0 && x < grid && z >= 0 && z < grid ? z * grid + x : -1;
    };
    const plan = { grid, rooms: new Array(count).fill(null), turns: new Array(count).fill(0), east: new Array(count).fill(0), north: new Array(count).fill(0) };
    const open = (a, b, doorways) => { const low = Math.min(a, b); if (Math.abs(a - b) === 1) plan.east[low] |= doorways; else plan.north[low] |= doorways; };

    plan.keyTurns = isLong ? random.next(4) : 0;
    plan.keyCells = isLong ? [key, neighbour(key, STEPS[(5 - plan.keyTurns) % 4])] : [key];
    const isKey = cell => cell >= 0 && plan.keyCells.includes(cell);
    const visited = new Array(count).fill(false), stack = [];
    plan.keyCells.forEach(cell => { visited[cell] = true; stack.push(cell); });

    while (stack.length) {
      const cell = stack[stack.length - 1];
      const next = STEPS.map(step => neighbour(cell, step)).filter(n => n >= 0 && !visited[n]);
      if (!next.length) { stack.pop(); continue; }
      const chosen = next[random.next(next.length)];
      const one = 1 << random.next(2);
      open(cell, chosen, one | (random.nextDouble() < loops ? BOTH : 0));
      visited[chosen] = true;
      stack.push(chosen);
    }

    for (let cell = 0; cell < count; cell++) {
      if (cell % grid < grid - 1 && random.nextDouble() < loops) plan.east[cell] |= 1 << random.next(2);
      if (Math.floor(cell / grid) < grid - 1 && random.nextDouble() < loops) plan.north[cell] |= 1 << random.next(2);
    }

    plan.keyCells.forEach(cell => STEPS.forEach(step => {
      const n = neighbour(cell, step);
      if (!isKey(n)) open(cell, n, BOTH);
      else if (n === cell + 1) plan.east[cell] = 0;
      else if (n === cell + grid) plan.north[cell] = 0;
    }));

    const used = new Array(kinds.length).fill(0);
    const hasShape = isCorner => kinds.some(k => k.corner === isCorner);
    const fits = (i, isCorner) => kinds[i].corner === isCorner || !hasShape(isCorner);
    const roundOf = isCorner => Math.min(...kinds.map((k, i) => fits(i, isCorner) ? used[i] : Infinity));
    const isFree = (i, isCorner, round) => fits(i, isCorner) && used[i] === round;
    const pick = isCorner => {
      const round = roundOf(isCorner);
      let total = 0;
      kinds.forEach((k, i) => { if (isFree(i, isCorner, round)) total = float(total + float(k.weight)); });
      let roll = float(float(random.nextDouble()) * total), kind = 0;
      for (let i = 0; i < kinds.length; i++) {
        if (!isFree(i, isCorner, round)) continue;
        kind = i;
        roll = float(roll - float(kinds[i].weight));
        if (roll <= 0) break;
      }
      used[kind]++;
      return kind;
    };
    const cornerTurns = cell => {
      const last = grid - 1, x = cell % grid, z = Math.floor(cell / grid);
      if ((x !== 0 && x !== last) || (z !== 0 && z !== last)) return -1;
      return x === 0 ? (z === 0 ? 0 : 1) : (z === 0 ? 3 : 2);
    };

    for (let cell = 0; cell < count; cell++) {
      const corner = cornerTurns(cell);
      plan.rooms[cell] = isKey(cell) ? null : kinds[pick(corner >= 0)].name;
      plan.turns[cell] = corner >= 0 ? corner : random.next(4);
    }
    return plan;
  }

  // Unity: Quaternion.Euler(0, yaw, 0) * (x, 0, z).
  const yawPoint = (x, z, yaw) => {
    const a = yaw * Math.PI / 180, c = Math.cos(a), s = Math.sin(a);
    return [x * c + z * s, -x * s + z * c];
  };

  // Где что стоит: комнаты, стороны, столбы и двери плана.
  function place(plan, { pitch, doorOffset }) {
    const { grid } = plan, half = pitch / 2, offset = (grid - 1) / 2;
    const isKey = cell => plan.keyCells.includes(cell);
    const center = cell => [(cell % grid - offset) * pitch, (Math.floor(cell / grid) - offset) * pitch];
    const filled = (x, z) => x >= 0 && x < grid && z >= 0 && z < grid && (isKey(z * grid + x) || plan.rooms[z * grid + x] !== null);
    const out = { rooms: [], sides: [], posts: [], doors: [] };

    plan.rooms.forEach((name, cell) => {
      if (name === null) return;
      const [cx, cz] = center(cell), x = cell % grid, z = Math.floor(cell / grid);
      out.rooms.push({ name, cell, x: cx, z: cz, yaw: plan.turns[cell] * 90 });
      const side = (sx, sz, yaw, doorways) => out.sides.push({ cell, x: sx, z: sz, yaw, doorways });
      if (!(x < grid - 1 && isKey(cell + 1))) side(cx + half, cz, -90, x < grid - 1 ? plan.east[cell] : 0);
      if (!isKey(cell + grid)) side(cx, cz + half, 0, z < grid - 1 ? plan.north[cell] : 0);
      if (!filled(x - 1, z)) side(cx - half, cz, -90, 0);
      if (!filled(x, z - 1)) side(cx, cz - half, 0, 0);
    });

    const [kx, kz] = center(plan.keyCells[0]);
    out.rooms.push({ name: null, isKey: true, cell: plan.keyCells[0], x: kx, z: kz, yaw: plan.keyTurns * 90 });

    for (let z = 0; z <= grid; z++)
      for (let x = 0; x <= grid; x++)
        if (filled(x - 1, z - 1) || filled(x, z - 1) || filled(x - 1, z) || filled(x, z))
          out.posts.push({ cell: Math.min(z, grid - 1) * grid + Math.min(x, grid - 1), x: (x - grid / 2) * pitch, z: (z - grid / 2) * pitch });

    const addDoors = (cell, sx, sz, yaw, doorways) => {
      for (let i = 0; i < 2; i++)
        if (doorways & (1 << i)) {
          const [ox, oz] = yawPoint((i * 2 - 1) * doorOffset, 0, yaw);
          out.doors.push({ cell, x: sx + ox, z: sz + oz, yaw });
        }
    };
    plan.rooms.forEach((_, cell) => {
      const [cx, cz] = center(cell);
      if (cell % grid < grid - 1) addDoors(cell, cx + half, cz, -90, plan.east[cell]);
      if (Math.floor(cell / grid) < grid - 1) addDoors(cell, cx, cz + half, 0, plan.north[cell]);
    });
    return out;
  }

  // Рядом с ключевой комнатой: DungeonDirector.Starts берёт только такие.
  function isNearKey(plan, room, pitch) {
    const offset = (plan.grid - 1) / 2;
    return plan.keyCells.some(cell => {
      const x = (cell % plan.grid - offset) * pitch, z = (Math.floor(cell / plan.grid) - offset) * pitch;
      return Math.hypot(room.x - x, room.z - z) < pitch * 1.2;
    });
  }

  const api = { DotNetRandom, lay, place, yawPoint, isNearKey };
  if (typeof module !== 'undefined') module.exports = api;
  root.Generator = api;
})(typeof window !== 'undefined' ? window : globalThis);
