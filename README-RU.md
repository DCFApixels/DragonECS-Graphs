<p align="center">
<img width="400" src="https://github.com/user-attachments/assets/4c1aaeea-7283-4980-b447-a3bc7e54aeb7">
</p>

<p align="center">
<img alt="Version" src="https://img.shields.io/github/package-json/v/DCFApixels/DragonECS-Graphs?color=%23ff4e85&style=for-the-badge">
<img alt="License" src="https://img.shields.io/github/license/DCFApixels/DragonECS-Graphs?color=ff4e85&style=for-the-badge">
<a href="https://discord.gg/kqmJjExuCf"><img alt="Discord" src="https://img.shields.io/badge/Discord-JOIN-00b269?logo=discord&logoColor=%23ffffff&style=for-the-badge"></a>
<a href="http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=IbDcH43vhfArb30luGMP1TMXB3GCHzxm&authKey=s%2FJfqvv46PswFq68irnGhkLrMR6y9tf%2FUn2mogYizSOGiS%2BmB%2B8Ar9I%2Fnr%2Bs4oS%2B&noverify=0&group_code=949562781"><img alt="QQ" src="https://img.shields.io/badge/QQ-JOIN-00b269?logo=tencentqq&logoColor=%23ffffff&style=for-the-badge"></a>
</p>

# Графы сущностей для [DragonECS](https://github.com/DCFApixels/DragonECS)
 
<table>
  <tr></tr>
  <tr>
    <td colspan="3">Readme Languages:</td>
  </tr>
  <tr></tr>
  <tr>
    <td nowrap width="100">
      <a href="https://github.com/DCFApixels/DragonECS-Graphs/blob/main/README-RU.md">
        <img src="https://github.com/user-attachments/assets/3c699094-f8e6-471d-a7c1-6d2e9530e721"></br>
        <span>Русский</span>
      </a>  
    </td>
    <td nowrap width="100">
      <a href="https://github.com/DCFApixels/DragonECS-Graphs">
        <img src="https://github.com/user-attachments/assets/30528cb5-f38e-49f0-b23e-d001844ae930"></br>
        <span>English(WIP)</span>
      </a>  
    </td>
  </tr>
</table>

</br>

Реализация отношений сущностей в виде графа. Связывающие ребра графа представлены в виде сущностей, что позволяет создавать отношения вида многие ко многим, а с помощью компонентной композиции можно настраивать вид этих отношений.

> [!WARNING]
> Проект в стадии разработки. API может меняться.

# Оглавление
- [Установка](#установка)
- [Инициализация](#инициализация)

</br>

# Установка
Семантика версионирования - [Открыть](https://gist.github.com/DCFApixels/e53281d4628b19fe5278f3e77a7da9e8#file-dcfapixels_versioning_ru-md)
## Окружение
Обязательные требования:
+ Зависимость: [DragonECS](https://github.com/DCFApixels/DragonECS)
+ Минимальная версия C# 7.3;

Опционально:
+ Игровые движки с C#: Unity, Godot, MonoGame и т.д.

Протестировано:
+ **Unity:** Минимальная версия 2020.1.0;

## Установка для Unity
* ### Unity-модуль
Поддерживается установка в виде Unity-модуля в  при помощи добавления git-URL [в PackageManager](https://docs.unity3d.com/2023.2/Documentation/Manual/upm-ui-giturl.html) или ручного добавления в `Packages/manifest.json`: 
```
https://github.com/DCFApixels/DragonECS-Graphs.git
```
* ### В виде исходников
Пакет так же может быть добавлен в проект в виде исходников.

</br>

# Граф
Ключевой класс в котором хранится информация об отношениях. Графу требуется 2 мира: обычный мир и мир для сущностей-связей. Пример создания `EntityGraph`:
```c#
// Обычный мир.
_world = new EcsDefaultWorld();
// EcsGraphWorld специальный тип мира для сущностей-связей,
// но может использоваться любой другой тип мира.
_graphWorld = new EcsGraphWorld();
// Создание EntityGraph связывающий эти два мира.
EntityGraph graph = _world.CreateGraph(_graphWorld);

_pipeline = EcsPipeline.New()
    // ...
    // Далее миры и граф можно внедрить в системы.
    .Inject(_world, _graphWorld, graph)
    // ...
    .Build()
```
Для обычных сущностей и для сущностей-связей может использовать один общий мир:
```c#
_world = new EcsDefaultWorld();
// Создание EntityGraph завязанный на одном мире.
EntityGraph graph = _world.CreateGraph();
```

# Сущность-связь

Как и обычная сущность, но регистрируется и создается в `EntityGraph`. Предназначена для данных об отношении двух сущностей.

> Отношения имеют направление, поэтому чтобы разделять сущности, далее будет использованы понятия: начальная сущность(`Start Entity`) от нее исходит сущность-связъ(`Relation Entity`) к конечной сущности(`End Entity`). Начальная и конечная сущность это сущности-узлы(`Node Entity`).
> 
> (Start Entity) ── (Relation Entity) ─》(End Entity)

Пример работы с связями:
```c#
// Получаем или создаем новую сущность-связь от узлов `startE` к `endE`.
// Сущность создается в мире _graph.GraphWorld и регистрируется в графе.
var relE = _graph.GetOrNewRelation(startE, endE);

// Кроме создания и удаления, в остальном сущности-связи - это обычные сущности.
ref var someCmp = ref _somePool.Add(relE);

// Вернет true если была создана через EntityGraph.GetOrNewRelation(startE, endE)
// и false если через EcsWorld.NewEntity().
bool isRelation = _graph.IsRelation(relE);

// Получить начальную и конечную сущность.
(startE, endE) = _graph.GetRelationStartEnd(relE);

// Взять сущность-связь для отношения в обратном направлении, от `endE` к `startE`.
_graph.GetOrNewInverseRelation(relE);

// Удаляем сущность-связь.
_graph.DelRelation(relE);
```

# Запрос Join

Сопоставляет сущности-связи с привязанными сущностями. Возвращает структуру `SubGraphMap` которая позволяет итерироваться по сопоставленным сущностями-связям.

```c#
// Запросом Where получем сущности-связи, потом запросом Join сопоставляем их с конечными сущностями.
// Аргумент JoinMode.End указывает что сопоставлять нужно с конечными сущностями.
SubGraphMap map = _graph.GraphWorld.Where(out EventAspect relA).Join(JoinMode.End);
// map.Nodes это список конечных сущностей.
foreach (var endE in map.Nodes.Where(out Aspect a))
{
    // ...
    // Итерация по сопоставленным сущностям-связям.
    foreach (var relE in map.GetRelations(endE))
    {
        // ...
    }
}
```

# Пример кода

Ниже приведен пример как бы могли быть реализованы системы нанесения урона взрывом и система применения урона к здоровью. Этот пример поверхностный реализации, но достаточно нагляден и демонстрирует основные функции расширения.
<details>
<summary>Использованные в примере компоненты</summary>

```c#
public struct Explosion : IEcsComponent
{
    public float damage;
    public float radius;
}
public struct Health : IEcsComponent
{
    public float points;
}
public struct Transform : IEcsComponent
{
    public Vector3 position;
}

// Компоненты для связей.
public struct DamageEvent : IEcsComponent
{
    public float points;
}
public struct KillEvent : IEcsTagComponent { }
```

</details>

```c#
public class SomeExplosionHitSystem : IEcsRun, IEcsInject<EntityGraph>, IEcsInject<SpatialService>
{
    class EventAspect : EcsAspect
    {
        public EcsPool<DamageEvent> damageEvents = Inc;
    }
    class Aspect : EcsAspect
    {
        public EcsPool<Transform> transforms = Inc;
        public EcsPool<Explosion> explosions = Inc;
    }
    EntityGraph _graph;
    SpatialService _spatial;

    public void Run()
    {
        var relA = _graph.GraphWorld.GetAspect<EventAspect>();
        foreach (var e in _graph.World.Where(out Aspect a))
        {
            ref var transform = ref a.transforms.Get(e);
            ref var explosion = ref a.explosions.Get(e);
            // Получаем все сущности рядом со взрывом.
            // Реализация опущена, можно реализовать на основе Quad Tree, Spatial hashing или при помощи методов физики движка.
            EcsSpan targetEs = _spatial.GetEntitiesInRadius(transform.position, explosion.radius);

            foreach (var targetE in targetEs)
            {
                // Получаем сущность-связь от `e` к `targetE`.
                var relE = _graph.GetOrNewRelation(e, targetE);
                // Создаем событие нанесения урона.
                ref var damageEvent = ref relA.damageEvents.TryAddOrGet(relE);
                damageEvent.points = explosion.damage;
            }
        }
    }
    public void Inject(EntityGraph obj) => _graph = obj;
    public void Inject(SpatialService obj) => _spatial = obj;
}
```
```c#
public class SomeApplyDamageSystem : IEcsRun, IEcsInject<EntityGraph>
{
    class EventAspect : EcsAspect
    {
        public EcsPool<DamageEvent> damageEvents = Inc;
        public EcsTagPool<KillEvent> killEvents = Opt;
    }
    class Aspect : EcsAspect
    {
        public EcsPool<Health> healths = Inc;
    }
    EntityGraph _graph;
    
    public void Run()
    {
        // Запрос сущностей с DamageEvent и запрос Join для них.
        SubGraphMap map = _graph.GraphWorld.Where(out EventAspect relA).Join(JoinMode.End);
        // Фильтруем конечные сущности наа наличие Health и итерируемся по ним.
        foreach (var endE in map.Nodes.Where(out Aspect a))
        {
            ref var health = ref a.healths.Get(endE);
            bool isAlive = health.points > 0;
            foreach (var relE in map.GetRelations(endE))
            {
                ref var damage = ref relA.damageEvents.Get(relE);
                health.points -= damage.points;
                if (isAlive && health.points <= 0)
                {
                    // Добавляем в сущность связь тег сигнализирующий что
                    // источник урона так же убил сущность.
                    relA.killEvents.TryAdd(relE);
                }
            }
        }
    }
    public void Inject(EntityGraph obj) => _graph = obj;
}
```