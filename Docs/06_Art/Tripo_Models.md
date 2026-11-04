# Модели из Tripo AI

Генерация идёт вручную в веб-студии Tripo за кредиты подписки. API (platform.tripo3d.ai) не используется: у него отдельный баланс, подписка его не пополняет (проверено 2026-11-05, `403 code 2010`). Ключ API в MCP for Unity сохранён, автоматическая генерация (`generate_model`) включится, если пополнить баланс API.

## Процесс
1. **Список** — Claude составляет пачку: имя ассета, промпт (английский), режим (text / image), текстура, риг, целевой размер, куда пойдёт.
2. **Генерация** — пользователь запускает задания в студии Tripo, выбирает удачный вариант.
3. **Перенос** — `Tools > Tripo3D Bridge` → `Start Server`, из студии отправить в Unity. Бридж кладёт модель в `Assets/TripoModels/<имя>/` (`<имя>.fbx`, текстуры в `<имя>.fbm/`, материал в `Materials/`).
4. **Приёмка** — в любой 3D-задаче Claude сам замечает модели в `Assets/TripoModels` (и другие файлы не на месте), переносит их через AssetDatabase в категорию (ниже) и даёт смысловое английское PascalCase-имя (`золотой_кубок_3d_модель` → `GoldenGoblet`). Проверяет габариты, треугольники, риг, текстуры. Скрипта-автоматики нет. `Assets/TripoModels` в git не идёт.

Пачки: [WellRest](Tripo_Batch_WellRest.md) — полный набор одного модуля катакомб.

## Шаблон промпта
`<предмет>, <материал и состояние>, dark fantasy medieval, game asset, realistic PBR, clean quad topology, low poly, single object, neutral pose, no base, no background`
- Персонажи и мобы — `T-pose` или `A-pose`, без оружия в руках (оружие — отдельные модели на риге).
- Предметы — один объект без подставки, лицевая сторона вперёд.
- Стиль — как у остального окружения (`Art_Direction.md`): реалистичный, приглушённый, потёртый.

## Перенос в проект
- Путь: `Assets/Game/Models/<Monsters|Environment|Items>/<PascalName>/<PascalName>.fbx`.
- Текстуры: `Textures/<PascalName>_Color.jpg`, `_Normal.png`, `_MetallicSmoothness.png` (металл в R, гладкость в A).
- Материал URP Lit собирается билдером (`DungeonSkeletonBuilder.TripoMaterial`), импортёр — `materialImportMode = None`.
- Масштаб: Tripo отдаёт модель высотой около 1 м — `globalScale` в импортёре под реальный размер.
- Риг Tripo — Generic: для гуманоидов импортёр переводится в `Human` (`DungeonSkeletonBuilder.Import`), тело надевается на риг бойца через `Retarget`.

## В проекте
| Модель | Где лежит | Где используется |
|---|---|---|
| Skeleton | `Assets/Game/Models/Monsters/Skeleton` | тело всех скелетов (`DungeonSkeletonBuilder.Attach`) |
| FlyingSkull | `Assets/Game/Models/Monsters/FlyingSkull` | летающий череп (`DungeonContentBuilder`) |
| TerracottaPot, TerracottaVase | `Assets/Game/Models/Environment/<Name>` | пока не подключены |
| GoldenGoblet, SilverGoblet | `Assets/Game/Models/Items/<Name>` | пока не подключены |
