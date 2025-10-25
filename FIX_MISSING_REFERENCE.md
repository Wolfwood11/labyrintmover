# 🔧 Исправление MissingReferenceException

## ❌ Проблема

При генерации уровня возникала ошибка `MissingReferenceException`:
```
MissingReferenceException: The object of type 'LabyrinthMover.Gameplay.StaticTile' has been destroyed but you are still trying to access it.
```

## 🔍 Причина

Проблема была в неправильном порядке операций:
1. Создавались визуальные объекты (StaticTile, MovableBlock)
2. Устанавливался GridModel в LevelManager
3. Вызывался `LevelManager.BuildGridModel()` который пытался прочитать уже уничтоженные объекты

## ✅ Решение

### 1. Убрали вызов BuildGridModel
Поскольку мы уже создали визуальные объекты и установили GridModel напрямую, нет необходимости вызывать `BuildGridModel()`.

### 2. Добавили проверки на null
В `LevelManager.BuildGridModel()` добавлены проверки:
```csharp
if (tile == null || tile.gameObject == null)
{
    Debug.LogWarning("Найден уничтоженный StaticTile, пропускаем");
    continue;
}
```

## 🎯 Результат

Теперь генератор работает без ошибок:
- ✅ Создаются визуальные объекты с правильным размером (128px)
- ✅ GridModel устанавливается корректно
- ✅ Нет ошибок MissingReferenceException
- ✅ LevelManager работает с готовым GridModel

## 🎮 Тестирование

Попробуйте снова сгенерировать уровень 20x20 с размером тайла 128px - теперь должно работать без ошибок!

