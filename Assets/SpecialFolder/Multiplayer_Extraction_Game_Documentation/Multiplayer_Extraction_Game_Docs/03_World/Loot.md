# Loot

## Categories
- Weapons
- Armor
- Consumables
- Materials
- Quest Items
- Currency
- Keys
- Artifacts
- Rare Items

## Sources
- Containers
- Chests
- Corpses
- NPCs
- Elites
- Mini-bosses
- Bosses
- Secrets
- Quests
- PvP kills

## Zone Loot (этаж 1)
Ценность контейнеров в домах задаёт `HouseSpec.Loot` (`VillageArchitectureBuilder.LootTier`), DD-017:

| Tier | Где | Сундук в доме | Верхний этаж |
|---|---|---|---|
| Poor | Ферма | Ящик или малый дубовый сундук (50/50) | Ящик или малый сундук |
| Normal | Лесные POI, кладбище | Малый (70 %) или большой (30 %) дубовый сундук | Ящик или малый сундук |
| Rich | Деревня | Золотой (15 %), большой (45 %) или малый (40 %) сундук | Так же, как основной |

Плюс золотой сундук на площади деревни, на мельнице — бочки и ящик внизу, малый сундук посередине, большой дубовый сундук наверху. В амбаре фермы вместо большого сундука — малый.

## Enemy Loot
Лут врага соответствует его лору (DD-016): таблица врага = лорная часть + универсальная часть.

**Универсальный лут** (любой враг): монеты, бинты, расходники, экипировка (разная), драгоценности.

| Enemy | Lore loot |
|---|---|
| Skeleton Swordsman | Мечи |
| Skeleton Archer | Луки, стрелы |
| Skeleton Crossbowman | Арбалеты, болты |
| Skeleton Warrior | Мечи, щиты |
| Skeleton Mage | Посохи, книги заклинаний |
| Flying Skull | — (только универсальный) |

## Rarity
Common → Uncommon → Rare → Epic → Legendary → Unique

## Generation
Source → Loot Table → Rarity Roll → Item Roll → Modifier Roll → Final Item
