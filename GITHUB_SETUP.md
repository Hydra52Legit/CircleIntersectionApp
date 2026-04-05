# Инструкции по загрузке проекта на GitHub

## Шаг 1: Создайте репозиторий на GitHub
1. Зайдите на https://github.com
2. Нажмите "+" в правом верхнем углу → "New repository"
3. Укажите название: `CircleIntersectionApp`
4. Описание: `Курсовая работа: Приложение для анализа пересечения окружностей на Avalonia UI`
5. Выберите видимость: `Public` (или `Private` если хотите)
6. НЕ добавляйте README, .gitignore, лицензию (они уже есть)
7. Нажмите "Create repository"

## Шаг 2: Свяжите локальный репозиторий с GitHub
После создания репозитория выполните в терминале:

```bash
cd CourseWork/CircleIntersectionApp

# Замените YOUR_USERNAME на ваше имя пользователя GitHub
git remote add origin https://github.com/YOUR_USERNAME/CircleIntersectionApp.git

# Отправьте код на GitHub
git push -u origin main
```

## Шаг 3: Проверка
После выполнения команд ваш проект будет доступен на:
`https://github.com/YOUR_USERNAME/CircleIntersectionApp`

## Дополнительные команды Git (опционально)

```bash
# Просмотр статуса
git status

# Просмотр истории коммитов
git log --oneline

# Создание новой ветки для разработки
git checkout -b feature/new-feature

# Слияние ветки с main
git checkout main
git merge feature/new-feature
```

## Структура репозитория на GitHub
- **README.md** - Полное описание проекта
- **POYASNITELNA_ZAPISKA.md** - Пояснительная записка (30+ страниц)
- Исходный код на C# с Avalonia UI
- **.gitignore** - Исключает временные файлы
- Тестовые данные в `sample_data.txt`