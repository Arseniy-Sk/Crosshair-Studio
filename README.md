# Crosshair Studio

<div align="center">

![Crosshair Studio](https://img.shields.io/badge/Crosshair%20Studio-Точное%20прицеливание-blue?style=for-the-badge)
![WPF](https://img.shields.io/badge/WPF-.NET%20Framework-blue?style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-Профессиональный%20уровень-green?style=for-the-badge)

**Профессиональный инструмент для настройки и отображения прицелов для игр и приложений**

[Возможности](#возможности) • [Установка](#установка) • [Использование](#использование) • [Документация](#документация) • [Разработка](#разработка)

</div>

##  Обзор

Crosshair Studio - это современное WPF приложение для создания, настройки и отображения точных прицелов для игр, стриминга и профессиональных приложений. Построенное на современных принципах UI и надежной архитектуре, оно предлагает широкую кастомизацию и производительность.

##  Возможности

###  Типы прицелов
- **Крест** - Классический крестообразный прицел
- **Круг** - Круговой прицел
- **Точка** - Простая точка
- **Квадрат** - Квадратный прицел
- **Треугольник** - Треугольный прицел
- **Кастомный** - Расширенный настраиваемый прицел

###  Опции настройки
| Функция | Описание | Диапазон |
|---------|-------------|-------|
| **Размер** | Размеры прицела | 5-100px |
| **Толщина** | Толщина линий | 1-10px |
| **Прозрачность** | Уровень прозрачности | 10%-100% |
| **Цвет** | Полноценный выбор цвета | 16M+ цветов |
| **Обводка** | Граница с настраиваемым цветом/толщиной | 1-5px |

###  Расширенные возможности
-  **Поддержка нескольких мониторов** - Отображение на любом экране
-  **Превью в реальном времени** - Мгновенное обновление при настройке
-  **Сохранение/Загрузка профилей** - Постоянные конфигурации прицелов
-  **Импорт/Экспорт** - Обмен конфигурациями прицелов
-  **Режим сквозного клика** - Прицел не мешает геймплею
-  **Поверх всех окон** - Всегда виден поверх других приложений

##  Архитектура и паттерны проектирования

### Реализация паттерна MVVM
```csharp
// Модель
public class Crosshair : INotifyPropertyChanged
{
    public string Name { get; set; }
    public CrosshairType Type { get; set; }
    public Color Color { get; set; }
    // ... другие свойства с уведомлениями об изменениях
}

// ViewModel
public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Crosshair> Crosshairs { get; }
    public Crosshair CurrentCrosshair { get; set; }
    public ICommand ShowCrosshairCommand { get; }
    // ... команды и бизнес-логика
}

// Представление
<Window x:Class="MainWindow">
    <Canvas x:Name="MainPreviewCanvas"/>
    <!-- Привязка данных к ViewModel -->
</Window>
```

### Ключевые паттерны проектирования

#### 1. **Паттерн Команда (Command Pattern)**
```csharp
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    
    // Инкапсулирует выполнение методов с поддержкой параметров
}
```

#### 2. **Паттерн Наблюдатель (Observer Pattern)**
```csharp
public class Crosshair : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

#### 3. **Паттерн Стратегия (Strategy Pattern)**
```csharp
switch (crosshair.Type)
{
    case CrosshairType.Cross:
        DrawPreviewCross(centerX, centerY, crosshair, scale);
        break;
    case CrosshairType.Circle:
        DrawPreviewCircle(centerX, centerY, crosshair, scale);
        break;
    // ... разные стратегии отрисовки для каждого типа
}
```

#### 4. **Паттерн Фабрика (Factory Pattern)**
```csharp
private Crosshair CreateDefaultCrosshair()
{
    return new Crosshair
    {
        Name = "Default Crosshair",
        Type = CrosshairType.Cross,
        Color = Colors.Red,
        // ... стандартные настройки
    };
}
```

##  Структура проекта

```
Simple_Customized_Crosshair_SCC/
├── Models/
│   ├── Crosshair.cs          # Основная модель прицела
│   ├── CrosshairType.cs      # Перечисление типов прицелов
│   └── DisplaySettings.cs    # Настройки отображения
├── ViewModels/
│   ├── MainViewModel.cs      # Основная логика приложения
│   └── RelayCommand.cs       # Реализация команд
├── Views/
│   ├── MainWindow.xaml       # Основной UI
│   ├── MainWindow.xaml.cs    # Код основного окна
│   ├── CrosshairWindow.xaml  # Окно отображения прицела
│   └── RenameDialog.xaml     # Диалог переименования
├── Services/
│   ├── CrosshairManager.cs   # Управление сохранением прицелов
│   ├── MonitorService.cs     # Поддержка нескольких мониторов
│   └── ScreenInfo.cs         # Информация об экранах
├── Converters/
│   ├── ColorToBrushConverter.cs
│   ├── EnumToCollectionConverter.cs
│   ├── CrosshairStatusConverter.cs
│   └── BoolToVisibilityConverter.cs
└── App.xaml                  # Точка входа приложения
```

##  Документация основных компонентов

### Модель прицела (`Models/Crosshair.cs`)
```csharp
public class Crosshair : INotifyPropertyChanged
{
    // Основные свойства
    public string Name { get; set; }
    public CrosshairType Type { get; set; }
    public Color Color { get; set; }
    public double Size { get; set; }
    public double Thickness { get; set; }
    public double Opacity { get; set; }
    
    // Конфигурация центральной точки
    public bool ShowCenterDot { get; set; }
    public Color DotColor { get; set; }
    public double DotSize { get; set; }
    
    // Конфигурация линий прицела
    public bool ShowCrosshairLines { get; set; }
    public Color LineColor { get; set; }
    public double LineLength { get; set; }
    public double LineThickness { get; set; }
    public double Gap { get; set; }
    
    // Индивидуальное управление линиями
    public bool ShowTopLine { get; set; }
    public bool ShowBottomLine { get; set; }
    public bool ShowLeftLine { get; set; }
    public bool ShowRightLine { get; set; }
    
    // Конфигурация обводки
    public bool ShowOutline { get; set; }
    public Color OutlineColor { get; set; }
    public double OutlineThickness { get; set; }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public Crosshair Clone(); // Реализация глубокого копирования
}
```

### Основная ViewModel (`ViewModels/MainViewModel.cs`)
```csharp
public class MainViewModel : INotifyPropertyChanged
{
    // Коллекции данных
    public ObservableCollection<Crosshair> Crosshairs { get; }
    public List<ScreenInfo> Screens { get; }
    public DisplaySettings DisplaySettings { get; }
    
    // Текущее состояние
    public Crosshair CurrentCrosshair { get; set; }
    public bool IsCrosshairVisible { get; set; }
    
    // Команды
    public ICommand ShowCrosshairCommand { get; }
    public ICommand HideCrosshairCommand { get; }
    public ICommand SaveCrosshairCommand { get; }
    public ICommand NewCrosshairCommand { get; }
    public ICommand DeleteCrosshairCommand { get; }
    public ICommand ExportCrosshairCommand { get; }
    public ICommand ImportCrosshairCommand { get; }
    public ICommand DuplicateCrosshairCommand { get; }
    public ICommand RenameCrosshairCommand { get; }
    
    // Методы управления прицелами
    private void ShowCrosshair();
    private void HideCrosshair();
    private void SaveCrosshair();
    private void CreateNewCrosshair();
    private void DeleteCurrentCrosshair();
    // ... другие методы управления
}
```

### Менеджер прицелов (`Services/CrosshairManager.cs`)
```csharp
public class CrosshairManager
{
    private readonly string _crosshairsFolder;
    
    // Основные операции
    public void SaveCrosshair(Crosshair crosshair);
    public ObservableCollection<Crosshair> LoadCrosshairs();
    public void DeleteCrosshair(Crosshair crosshair);
    public void ExportCrosshair(Crosshair crosshair, string filePath);
    public Crosshair? ImportCrosshair(string filePath);
    
    // Пути хранения: %AppData%\SimpleCustomizedCrosshair\Crosshairs\
}
```

##  Система отрисовки

### Алгоритм отрисовки прицела
```csharp
private void DrawPreviewCrosshair()
{
    var crosshair = _viewModel.CurrentCrosshair;
    var centerX = MainPreviewCanvas.ActualWidth / 2;
    var centerY = MainPreviewCanvas.ActualHeight / 2;
    
    // Масштабирование для превью
    var scale = Math.Min(MainPreviewCanvas.ActualWidth, MainPreviewCanvas.ActualHeight) / 200;
    scale = Math.Max(0.5, Math.Min(2.0, scale));
    
    // Стратегическая отрисовка по типам
    switch (crosshair.Type)
    {
        case CrosshairType.Cross:
            DrawPreviewCross(centerX, centerY, crosshair, scale);
            break;
        case CrosshairType.Circle:
            DrawPreviewCircle(centerX, centerY, crosshair, scale);
            break;
        // ... обработка всех типов
    }
}
```

### Окно отображения прицела (`Views/CrosshairWindow.xaml.cs`)
```csharp
public partial class CrosshairWindow : Window
{
    // Настройка прозрачного клик-сквозь окна
    private void SetClickThrough()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            extendedStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
    }
    
    // Обновление прицела в реальном времени
    public void UpdateCrosshair(Crosshair crosshair)
    {
        Dispatcher.Invoke(() =>
        {
            CrosshairCanvas.Children.Clear();
            DrawCrosshair();
        });
    }
}
```

##  Инструкция по использованию

###  Быстрый старт

1. **Запустите приложение**
2. **Выберите тип прицела** из выпадающего списка
3. **Настройте базовые параметры**:
   - Цвет
   - Размер
   - Толщину
   - Прозрачность
4. **Нажмите "Show Crosshair"** для отображения

###  Расширенная настройка

#### Кастомный прицел
1. Выберите "Custom" в типе прицела
2. Включите нужные компоненты:
   - **Центральная точка** - точечный маркер в центре
   - **Линии прицела** - перекрестие с настраиваемыми линиями
   - **Обводка** - контур для лучшей видимости

#### Настройка линий
- **Длина линии** - регулирует длину лучей
- **Зазор** - расстояние от центра до начала линий
- **Толщина линии** - толщина каждого луча
- **Индивидуальное управление** - включение/выключение отдельных линий

###  Настройки отображения

#### Выбор монитора
- Выберите целевой монитор из списка
- Прицел будет отображаться поверх всех окон на выбранном экране

#### Режимы отображения
- **Always on Top** - прицел всегда поверх других окон
- **Click Through** - прицел не перехватывает клики мыши

###  Управление профилями

#### Сохранение прицела
1. Настройте прицел
2. Нажмите "Save" для сохранения в папку AppData
3. Прицел будет доступен при следующем запуске

#### Импорт/Экспорт
- **Экспорт** - сохраняет прицел в JSON файл
- **Импорт** - загружает прицел из JSON файла
- **Дублирование** - создает копию текущего прицела

###  Горячие клавиши и управление

- **Двойной клик** по заголовку - развернуть/восстановить окно
- **Перетаскивание** за заголовок - перемещение окна
- **Кнопки управления** в правом верхнем углу:
  - `─` - Свернуть
  - `□` - Развернуть/Восстановить
  - `×` - Закрыть

##  Технические особенности

### Производительность
- **Оптимизированная отрисовка** с использованием WPF Canvas
- **Минимальное потребление ресурсов** в фоновом режиме
- **Эффективное обновление** только изменяемых элементов

### Безопасность
- **Локальное хранение** данных в папке AppData
- **Валидация данных** при импорте/экспорте
- **Обработка исключений** на всех уровнях приложения

### Совместимость
- **Windows 7+** с .NET Framework 4.7.2+
- **Поддержка всех разрешений** экранов
- **Многомониторные конфигурации**

##  Устранение неполадок

### Распространенные проблемы

#### Прицел не отображается
1. Проверьте настройки "Always on Top"
2. Убедитесь, что выбран правильный монитор
3. Проверьте прозрачность прицела

#### Низкая производительность
1. Уменьшите сложность кастомного прицела
2. Закройте ненужные компоненты (обводку, линии)
3. Убедитесь, что драйвера видеокарты обновлены

#### Проблемы с сохранением
1. Проверьте права доступа к папке AppData
2. Убедитесь, что имя прицела не содержит специальных символов

##  Планы развития

- [ ] **Пресеты популярных игр**
- [ ] **Анимации прицела**
- [ ] **Цветовые схемы**
- [ ] **Горячие клавиши для быстрого переключения**
- [ ] **Статистика использования**
- [ ] **Плагины и расширения**

##  Разработка

### Требования для сборки
- **Visual Studio 2019+**
- **.NET Framework 4.7.2**
- **Windows SDK**

### Структура решения
```bash
# Клонирование и сборка
git clone <repository-url>
cd Crosshair-Studio
# Открыть решение в Visual Studio и собрать
```

### Вклад в проект
Мы приветствуем вклады в развитие проекта! Пожалуйста:
1. Форкните репозиторий
2. Создайте feature ветку
3. Внесите изменения
4. Создайте Pull Request

##  Лицензия

Этот проект распространяется под лицензией MIT. Подробнее см. в файле LICENSE.

##  Поддержка

Если у вас возникли вопросы или проблемы:
1. Проверьте раздел "Устранение неполадок"
2. Создайте Issue в репозитории
3. Опишите проблему максимально подробно

---

<div align="center">

**Сделано с ❤️**

[⬆ Наверх](#crosshair-studio-)

</div>