# Labyrinth Mover Prototype

Игровой прототип для головоломки с подвижными блоками.

## Добавление нового уровня

1. Дублируйте сцену `Assets/Scenes/Levels/Level_001.unity` и переименуйте копию. В репозитории уже есть тестовая площадка `Level_TestMechanics.unity`, которую можно использовать как пример насыщенного окружения для отладки механик.
2. Откройте сцену и проверьте объект `GridRoot` с компонентом `GridConfig` — укажите размеры сетки и коэффициенты.
3. Разместите экземпляры `StaticTile.prefab` в контейнере `Statics/` и `MovableBlock.prefab` в `Blocks/`, соблюдая привязку к целым координатам.
4. Установите маркеры `CharacterStart/MarkerStart` и `Goal/MarkerGoal` в нужные клетки.
5. Убедитесь, что на Canvas размещён `HUD.prefab`, и свяжите ссылки `LevelManager`, `MoveExecutor`, `InputRouter` и HUD.
6. Добавьте новую сцену в Build Settings, чтобы меню могло загрузить уровень.
