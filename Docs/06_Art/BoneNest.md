# Костяное гнездо — промпт концепта

Гнездо маленьких костяных пауков (Поднятые; паук — тело-череп, лапы-рёбра, ниже колена, уже в 3D). Форма — одна огромная грудная клетка, наполовину ушедшая в пол склепа: позвоночник лежит вдоль пола, рёбра дугами поднимаются вверх и почти сходятся над ним (грудины нет). Внутри клетки — тёмная нора в полу, пауки выползают из неё через щели между рёбрами. ~1 м в высоту, 1.5 м в длину, немного голубого оссарита на позвонках.

Под разрушение: каждая часть — отдельный, крупный, чётко отделённый кусок без сращений и мелочи. Позвоночник, 5 пар толстых рёбер с видимым стыком у позвоночника, низкий земляной холм-основание с норой. Разлом: рёбра отлетают по одному, потом рушится позвоночник, остаётся холм с норой.

## GPT-image / Midjourney
```
Concept art of a "Bone Nest" for a grim dark fantasy dungeon crawler: a single oversized human ribcage, about 1 meter tall and 1.5 meters long, half sunk into the stone floor of an old catacomb vault. The spine lies along the floor, five pairs of thick ribs arch up and curve inward, their tips almost meeting above, no sternum, wide clear gaps between the ribs. Inside the ribcage a dark burrow hole goes down into a low mound of earth and rubble; small skull-bodied bone spiders crawl out of the hole through the gaps between the ribs. Every bone is a separate clean piece with a visible joint at the spine, nothing fused or overlapping, modular shapes easy to break apart. A few small pale blue crystal shards (ossarite) on the vertebrae give a faint cold glow. Old yellowed bone, plain stone walls, strong readable silhouette, low detail, no extra props. Three-quarter view at eye level, dim light, no text. Painterly realistic style, muted palette.
```

## DreamShaper XL Turbo (локально)
1024², 7–8 шагов, `dpmpp_2m_sde` karras, cfg 2.
```
(giant human ribcage half sunk into the floor:1.3), (simple modular design, low detail:1.2), spine lying along the floor, five pairs of thick separate ribs arching up and curving inward, tips almost meeting, no sternum, wide gaps between ribs, dark burrow hole inside the ribcage, low mound of earth and rubble, small skull bone spiders crawling out between the ribs, few small pale blue glowing crystal shards on the vertebrae, old yellowed bone, stone catacomb floor, clean simple shapes, readable silhouette, three-quarter view, eye level, dim light, grim dark fantasy concept art, painterly realistic, muted palette
```
Negative:
```
complex, intricate, fused bones, bone pile, many small bones, skull pile, full skeleton, sternum, closed ribcage, cathedral, silk web, cobweb, flesh, gore, blood, props, people, modern, cartoon, text, watermark, bright daylight, blurry, lowres
```

## Добавки
- Без пауков (чистый проп): убрать `small skull-bodied bone spiders crawl out...`, добавить `empty, no creatures`.
- Под Tripo (отдельный проп без склепа): `isolated on plain white background, no walls, no glow, ribs separated by clear gaps`. Рёбра потом режутся по стыкам на отдельные меши для разлома; свечение оссарита — шейдером.
- Вид сверху/сбоку для разреза частей: `orthographic side view, turnaround sheet`.
