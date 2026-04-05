using System;
using System.IO;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using CircleIntersectionApp.Models;

namespace CircleIntersectionApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _inputData = string.Empty;
    private string _outputResult = string.Empty;
    private CircleData _currentCircleData = new CircleData();
    private bool _isValidData;

    public MainWindowViewModel()
    {
        InputData = "Введите данные в формате:\nx1 y1 r1\nx2 y2 r2\n\nили загрузите из файла";
        OutputResult = "Результаты появятся здесь";

        LoadFileCommand = ReactiveCommand.CreateFromTask<Window>(LoadFile);
        SaveFileCommand = ReactiveCommand.CreateFromTask<Window>(SaveResult);
        ResetCommand = ReactiveCommand.Create(ResetData);
    }

    public string InputData
    {
        get => _inputData;
        set
        {
            this.RaiseAndSetIfChanged(ref _inputData, value);
            TryParseInputData(value);
        }
    }

    public string OutputResult
    {
        get => _outputResult;
        set => this.RaiseAndSetIfChanged(ref _outputResult, value);
    }

    public CircleData CurrentCircleData
    {
        get => _currentCircleData;
        set => this.RaiseAndSetIfChanged(ref _currentCircleData, value);
    }

    public bool IsValidData
    {
        get => _isValidData;
        set => this.RaiseAndSetIfChanged(ref _isValidData, value);
    }

    public ReactiveCommand<Window, Unit> LoadFileCommand { get; }
    public ReactiveCommand<Window, Unit> SaveFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }

    private async System.Threading.Tasks.Task LoadFile(Window window)
    {
        try
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите файл с исходными данными",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                var file = files[0];
                using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                InputData = content;
                ParseInputData(content);
            }
        }
        catch (Exception ex)
        {
            OutputResult = $"Ошибка загрузки файла: {ex.Message}";
        }
    }

    private void ParseInputData(string data)
    {
        try
        {
            var lines = data.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                throw new Exception("Недостаточно данных");

            var firstLine = lines[0].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var secondLine = lines[1].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (firstLine.Length < 3 || secondLine.Length < 3)
                throw new Exception("Некорректный формат данных");

            CurrentCircleData.X1 = double.Parse(firstLine[0]);
            CurrentCircleData.Y1 = double.Parse(firstLine[1]);
            CurrentCircleData.R1 = double.Parse(firstLine[2]);
            CurrentCircleData.X2 = double.Parse(secondLine[0]);
            CurrentCircleData.Y2 = double.Parse(secondLine[1]);
            CurrentCircleData.R2 = double.Parse(secondLine[2]);
        }
        catch (Exception ex)
        {
            OutputResult = $"Ошибка парсинга данных: {ex.Message}";
            IsValidData = false;
        }
    }

    private void TryParseInputData(string data)
    {
        try
        {
            var lines = data.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                IsValidData = false;
                return;
            }

            var firstLine = lines[0].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var secondLine = lines[1].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (firstLine.Length < 3 || secondLine.Length < 3)
            {
                IsValidData = false;
                return;
            }

            CurrentCircleData.X1 = double.Parse(firstLine[0]);
            CurrentCircleData.Y1 = double.Parse(firstLine[1]);
            CurrentCircleData.R1 = double.Parse(firstLine[2]);
            CurrentCircleData.X2 = double.Parse(secondLine[0]);
            CurrentCircleData.Y2 = double.Parse(secondLine[1]);
            CurrentCircleData.R2 = double.Parse(secondLine[2]);

            if (CurrentCircleData.R1 > 0 && CurrentCircleData.R2 > 0)
            {
                ValidateAndProcessData();
            }
            else
            {
                IsValidData = false;
            }
        }
        catch
        {
            IsValidData = false;
        }
    }

    private void ValidateAndProcessData()
    {
        try
        {
            ParseInputData(InputData);

            if (CurrentCircleData.R1 <= 0 || CurrentCircleData.R2 <= 0)
            {
                OutputResult = "Ошибка: Радиусы должны быть положительными числами";
                IsValidData = false;
                return;
            }

            double dx = CurrentCircleData.X2 - CurrentCircleData.X1;
            double dy = CurrentCircleData.Y2 - CurrentCircleData.Y1;
            double centerDistance = Math.Sqrt(dx * dx + dy * dy);

            if (CurrentCircleData.CirclesIntersect())
            {
                var points = CurrentCircleData.GetIntersectionPoints();
                if (points.HasValue)
                {
                    OutputResult = "✓ Окружности пересекаются!\n\n" +
                                   $"Первая окружность: центр ({CurrentCircleData.X1:F2}, {CurrentCircleData.Y1:F2}), радиус = {CurrentCircleData.R1:F2}\n" +
                                   $"Вторая окружность: центр ({CurrentCircleData.X2:F2}, {CurrentCircleData.Y2:F2}), радиус = {CurrentCircleData.R2:F2}\n\n" +
                                   "Точки пересечения:\n" +
                                   $"Точка A: ({points.Value.x1:F3}, {points.Value.y1:F3})\n" +
                                   $"Точка B: ({points.Value.x2:F3}, {points.Value.y2:F3})\n\n" +
                                   $"Расстояние между центрами: {centerDistance:F3}";
                    IsValidData = true;
                }
                else
                {
                    OutputResult = "Ошибка: вычислить точки пересечения не удалось.";
                    IsValidData = false;
                }
            }
            else
            {
                OutputResult = "✗ Окружности не пересекаются!\n\n" +
                               "Проверьте условия:\n" +
                               "- Расстояние между центрами должно быть меньше суммы радиусов\n" +
                               "- Расстояние между центров должно быть больше разности радиусов\n\n" +
                               $"Расстояние между центрами: {centerDistance:F3}\n" +
                               $"Сумма радиусов: {CurrentCircleData.R1 + CurrentCircleData.R2:F3}\n" +
                               $"Разность радиусов: {Math.Abs(CurrentCircleData.R1 - CurrentCircleData.R2):F3}";
                IsValidData = false;
            }
        }
        catch (Exception ex)
        {
            OutputResult = $"Ошибка валидации: {ex.Message}";
            IsValidData = false;
        }
    }

    private void ResetData()
    {
        InputData = "Введите данные в формате:\nx1 y1 r1\nx2 y2 r2\n\nили загрузите из файла";
        OutputResult = "Результаты появятся здесь";
        IsValidData = false;
        CurrentCircleData = new CircleData();
    }

    private async System.Threading.Tasks.Task SaveResult(Window window)
    {
        if (!IsValidData)
        {
            OutputResult = "Невозможно сохранить результат: данные невалидны";
            return;
        }

        try
        {
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить результат",
                DefaultExtension = "txt",
                SuggestedFileName = $"intersection_result_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            });

            if (file is not null)
            {
                using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(OutputResult);
                await writer.FlushAsync();
                OutputResult += $"\n\n✓ Результат сохранен в файл: {file.Name}";
            }
        }
        catch (Exception ex)
        {
            OutputResult = $"Ошибка сохранения: {ex.Message}";
        }
    }
}
