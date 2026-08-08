# Коллизия камеры в Fusion Multi-Peer

## Назначение документа

Этот документ описывает ошибку, из-за которой камера `CinemachineThirdPersonFollow` проходила через стены в основной мультиплеерной сцене, хотя тот же prefab игрока корректно работал в отдельной тестовой сцене.

Здесь зафиксированы наблюдаемые симптомы, фактическая причина, итоговое решение и проверки, необходимые при дальнейших изменениях камеры или сетевой загрузки сцен.

## Контекст

В проекте используются:

- Photon Fusion в режиме Multi-Peer;
- отдельная physics scene для каждого `NetworkRunner`;
- Cinemachine 3 и `CinemachineThirdPersonFollow`;
- камера, находящаяся внутри prefab игрока и назначаемая локальному игроку после spawn;
- authored-объекты окружения, загружаемые до появления сетевого игрока.

В обычной тестовой сцене игрок, камера и препятствия находились в стандартной Unity physics scene. В мультиплеерной сцене Fusion переносил объекты в scene и physics scene конкретного runner.

## Симптомы

Проблема проявлялась очень специфично:

1. В отдельной тестовой сцене тот же prefab камеры корректно реагировал на стены.
2. В Fusion-сцене игрок не мог пройти через стену, то есть физический коллайдер стены работал.
3. Камера при этом проходила через ту же стену.
4. Куб, размещённый в сцене до входа в Play Mode, блокировал игрока, но не камеру.
5. Такой же куб, созданный вручную во время Play Mode, сразу начинал блокировать камеру.
6. LayerMask, `Avoid Obstacles`, Follow/LookAt и назначение локальной камеры были корректными.

Главным диагностическим признаком была разница между authored-объектом и объектом, созданным во время Play Mode.

## Почему обычные исправления не помогали

Во время диагностики проверялись маски слоёв, настройки `CinemachineThirdPersonFollow`, наличие коллайдеров, порядок создания игрока и синхронизация transform. Эти проверки были необходимы, но не объясняли главное наблюдение: один и тот же куб работал или не работал только в зависимости от момента его создания.

Попытки дополнительно копировать коллайдеры, кешировать bounds или опрашивать сразу несколько physics scene усложняли систему, но не устраняли ошибочный фильтр. После нахождения первопричины эти временные обходные решения были удалены.

## Фактическая причина

Проблема состояла из двух связанных особенностей Fusion Multi-Peer.

### 1. Physics query должен выполняться в сцене runner

Статические объекты мультиплеерной сцены находятся не обязательно в `Physics.defaultPhysicsScene`. Обычный `Physics.SphereCast` может опрашивать другую physics scene и не видеть препятствия текущего клиента.

Камера должна получать `PhysicsScene` из scene локального Follow target и выполнять cast непосредственно через этот экземпляр:

```csharp
PhysicsScene targetPhysicsScene = PhysicsSceneQueries.Resolve(targetScene);
targetPhysicsScene.SphereCast(...);
```

### 2. `target.root` не является корнем игрока в Multi-Peer

Изначально камера исключала собственные коллайдеры игрока такой проверкой:

```csharp
Transform targetRoot = target.root;
```

После загрузки Fusion Multi-Peer и игрок, и authored-объекты сцены могут находиться под общим `MultiPeerSceneRoot`. Поэтому `target.root` возвращал не prefab игрока, а общий корень runner-сцены.

Следующая проверка ошибочно исключала все заранее загруженные объекты:

```csharp
collider.transform.IsChildOf(targetRoot)
```

Куб, созданный во время Play Mode, находился вне общего корня и поэтому проходил фильтр. Именно это объясняло разницу между двумя визуально одинаковыми кубами.

## Итоговое решение

### Корректное исключение иерархии игрока

Вместо общего scene root определяется ближайший родительский `NetworkObject`:

```csharp
NetworkObject targetNetworkObject = target.GetComponentInParent<NetworkObject>();
Transform targetRoot = targetNetworkObject != null
    ? targetNetworkObject.transform
    : target.root;
```

Теперь фильтр исключает только коллайдеры локального игрока. Стены, здания и другие authored-объекты остаются доступными camera cast.

### Fusion-aware Cinemachine extension

`FusionPhysicsSceneCameraCollision` подключается к локальной `CinemachineCamera` во время назначения Follow target.

Extension:

- запускается на стадии `CinemachineCore.Stage.Body`;
- использует параметры `AvoidObstacles` активного `CinemachineThirdPersonFollow`;
- получает physics scene локального runner;
- выполняет `SphereCast` только в этой physics scene;
- исключает только иерархию `NetworkObject` игрока;
- игнорирует trigger-коллайдеры и настроенный `IgnoreTag`;
- учитывает `CameraRadius` и небольшой `_surfaceOffset`;
- выполняет отдельную проверку участка rig root -> hand и участка hand -> camera;
- применяет `DampingIntoCollision` и `DampingFromCollision` из prefab камеры.

### Camera blockers для архитектуры сцены

Часть визуальной архитектуры не имела подходящих коллайдеров для камеры или использовала сложную LOD-структуру. `SceneCameraObstacleColliders` создаёт простые `BoxCollider` по bounds основных LOD-мешей зданий, стен, крыш, границ, колонн и других архитектурных объектов.

Blocker-объекты помещаются на слой `CameraObstacle`. Физические столкновения этого слоя отключаются, поэтому blockers влияют на camera query, но не изменяют движение персонажей и сетевую физику.

## Подключение

Основные файлы решения:

- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Player/Network/FusionPhysicsSceneCameraCollision.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Player/Network/SceneCameraObstacleColliders.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Player/Network/PlayerSceneCamera.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Player/Network/NetworkStarterAssetsPlayer.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Player/Network/PlayerSceneContext.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Editor/PortfolioDemo/PortfolioDemoSceneBuilder.cs`
- `ProjectSettings/TagManager.asset`

`PlayerSceneCamera.AssignFollow` и локальная ветка `NetworkStarterAssetsPlayer` гарантируют наличие extension на активной third-person камере. `PlayerSceneContext` при поиске камеры отдаёт приоритет `CinemachineThirdPersonFollow`.

## Настройки камеры

Базовые настройки находятся на `PlayerFollowCamera` в компоненте `CinemachineThirdPersonFollow`:

- `CameraDistance` задаёт обычную дистанцию;
- `AvoidObstacles.CameraRadius` задаёт физический радиус камеры;
- `AvoidObstacles.DampingIntoCollision` сглаживает приближение к препятствию;
- `AvoidObstacles.DampingFromCollision` сглаживает возвращение назад;
- `AvoidObstacles.CollisionFilter` определяет слои препятствий.

В текущем решении нет искусственной минимальной дистанции от игрока. Такое ограничение тестировалось, но было удалено, потому что оно снова позволяло камере оставаться за близкой стеной и нарушало гарантированную коллизию.

## Проверка в Play Mode

Перед проверкой нужно полностью выйти из Play Mode и войти снова. Закрывать Unity или заново открывать проект не требуется.

Проверка считается успешной, если:

1. Камера назначена локальному игроку.
2. В Console присутствует сообщение о подключении `FusionPhysicsSceneCameraCollision`.
3. В диагностике указана physics scene runner и `ignored player hierarchy` содержит имя игрока, а не общий scene root.
4. Куб, сохранённый в сцене до Play Mode, блокирует камеру.
5. Куб, созданный во время Play Mode, ведёт себя так же.
6. Стены и здания блокируют камеру.
7. Коллайдеры игрока не притягивают его собственную камеру.
8. После выхода из-за препятствия камера плавно возвращается на исходную дистанцию.

Ожидаемые диагностические сообщения:

```text
[FusionPhysicsSceneCameraCollision] Attached to 'PlayerFollowCamera' ...
[FusionPhysicsSceneCameraCollision] Querying runner physics scene only ...
[FusionPhysicsSceneCameraCollision] First camera collision: '...' ...
```

## Важные ограничения

- Не заменять `NetworkObject`-корень обратно на `target.root`.
- Не использовать глобальный `Physics.SphereCast` для этой камеры без явной проверки physics scene.
- Не считать успешную компиляцию доказательством runtime-коллизии: поведение обязательно проверяется в Fusion Play Mode.
- Не добавлять копирование всей сцены или bounds fallback без нового подтверждённого сценария, который не покрывается текущим решением.
- При изменении порядка scene loading повторно проверять, что camera blockers созданы в сцене до или во время подготовки physics scene runner.

## Результат

После исправления камера одинаково распознаёт authored-препятствия и объекты, созданные во время Play Mode. Решение не зависит от стандартной Unity physics scene и корректно учитывает структуру Fusion Multi-Peer, не исключая окружение вместе с иерархией игрока.
