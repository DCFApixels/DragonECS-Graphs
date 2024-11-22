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
Для начала нужно создать граф реализованный классом `EntityGraph`. Графу требуется 2 мира: обычный мир и мир для сущностей-связей. Пример создания `EntityGraph`:
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
Можно использовать один мир для обычных сущностей и для сущностей-связей. 
```c#
_world = new EcsDefaultWorld();
// Создание EntityGraph завязанный на одном мире.
EntityGraph graph = _world.CreateGraph();
```

# Сущность-связь

Как и обычная сущность, но управляется и создается в `EntityGraph` и предназначена для данных об отношении двух сущностей.

> Отношения имеют направление, поэтому чтобы разделять сущности, далее будет использованы понятия: начальная сущность(`Start Entity`) от нее исходит сущность-связъ(`Relation Entity`) к конечной сущности(`End Entity`).
```
(Start Entity) ─── (Relation Entity) ──► (End Entity)
```
Пример работы с связями:
```c#
        // Получаем или создаем новую сущность-связь от сущности `startE` к `endE`.
        // Сущность создается в мире _graph.GraphWorld и регистрируется в графе.
        var relE = _graph.GetOrNewRelation(startE, endE);

        // Кроме создания и удаления, в остальном сущности-свящи - это обычные сущности.
        ref var someCmp = ref _somePool.Add(relE);

        // Вернет true если была создана через EntityGraph.GetOrNewRelation(startE, endE)
        // и false если через EcsWorld.NewEntity().
        bool isRelation = _graph.IsRelation(relE);

        // Взять сущность-связь для отношения в обратном направлении, от `endE` к `startE`.
        _graph.GetOrNewInverseRelation(relE);

        // Удаляем сущность связь.
        _graph.DelRelation(relE);
```

# Запрос


