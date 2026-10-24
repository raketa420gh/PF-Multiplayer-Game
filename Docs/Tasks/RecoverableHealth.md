# Здоровье как в Dark and Darker (Recoverable Health) — исследование

Источники: darkanddarker.wiki.spellsandguns.com (Health, Healing, Bandage), Steam-обсуждения; имена ассетов из `.utoc` установленной игры
(`Id_Constant_RecoverableHealthRatio`, `Hud_MainHP_Gauge`, `Hud_MainHP_Gauge_OverHealed`, `GE_TreeOfLife_AddRecoverableHealth`, `GA_BandageConsume`, `CV_Resting_Start`).

## Сегменты полосы
- **Current HP** — сплошной красный (зелёный, пока есть Bonus HP). 0 = смерть.
- **Recoverable HP** — полупрозрачный/тёмно-красный кусок сразу после current, внутри недостающей части. От смерти не спасает.
- **Bonus HP** (только перк клирика Over Healing) — жёлтый справа; урон съедает его раньше current.
- Max HP = ширина полосы. При временном изменении max HP сохраняется процент current.

## Правила пула recoverable
`pool = min(pool + rate * amount, maxHp - currentHp)` — обновляется только на уроне и на bandage/rest.
- Урон от игроков/монстров и DoT: **+65%** урона. Зона (Death Swarm), трата HP варлоком: **+100%**.
- Bandage, отдых (G), Tranquility барда: **−100%** — переводят recoverable в current 1:1 и не лечат дальше пула.
- Зелья лечения, заклинания клирика, Shrine of Health, % от max HP: лечат current в любую недостающую часть, пул не трогают
  (лишний пул обрезается до недостающего на следующем уроне/бинте).
- Bandage II–VIII: 15/18/22/26/30/35/40, применяется разом после 4 с (скорость взаимодействия −50%), −10 к скорости бега.
- Отдых: 1 recoverable HP за 2 с × (1 + Health Recovery Bonus).
- Пассивной регенерации нет, затухания пула нет. Смерть/воскрешение — пул = 0. Inferno — пул = 50% недостающего (не проверено).

Пример: 100/100, пул 15, урон 40 → 60/100, пул = min(15 + 26, 40) = 40.

## Не подтверждено
- Отстающая «chip»-полоса при уроне и анимация лечения — источников нет, сверить по видео.
- Влияет ли Campfire Kit на пул; теряется ли пул, если встать с отдыха.

## Модель для реализации
```
float maxHp, currentHp, recoverable; // 0 <= recoverable <= maxHp - currentHp, всё [Networked] на хосте
TakeDamage(d, ratio = 0.65f): currentHp -= d; recoverable = min(recoverable + ratio * d, maxHp - currentHp)
Heal(h):        currentHp = min(currentHp + h, maxHp); recoverable = min(recoverable, maxHp - currentHp)
RecoverHeal(h): x = min(h, recoverable); currentHp += x; recoverable -= x   // bandage / rest
```
UI: три заливки одной полосы — current (красная), current..current+recoverable (полупрозрачная красная), bonus (жёлтая).
