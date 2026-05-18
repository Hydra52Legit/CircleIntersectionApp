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
    private string _statusMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private CircleData _currentCircleData = new CircleData();
    private bool _isValidData;

    public MainWindowViewModel()
    {
        InputData = "0 0 5\n8 0 5";
        OutputResult = "Результаты появятся после ввода данных";
        StatusMessage = "Ожидание ввода данных";

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

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _errorMessage, value);
            this.RaisePropertyChanged(nameof(HasErrorMessage));
        }
    }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

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
                await using var stream = await files[0].OpenReadAsync();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                InputData = content;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки файла: {ex.Message}";
            OutputResult = "";
            StatusMessage = "Ошибка загрузки";
            IsValidData = false;
        }
    }

    private void TryParseInputData(string data)
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(data))
        {
            OutputResult = "Введите данные в две строки: x1 y1 r1 и x2 y2 r2";
            StatusMessage = "Ожидание ввода данных";
            IsValidData = false;
            return;
        }

        var lines = data.Replace("\r", string.Empty)
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            OutputResult = "Ошибка: требуется две строки с тремя числами в каждой.";
            ErrorMessage = "Недостаточно данных. Введите две строки по три числа.";
            StatusMessage = "Неверный ввод";
            IsValidData = false;
            return;
        }

        try
        {
            var firstLine = SplitLine(lines[0]);
            var secondLine = SplitLine(lines[1]);

            if (firstLine.Length != 3 || secondLine.Length != 3)
                throw new FormatException("Каждая строка должна содержать ровно три числа.");

            CurrentCircleData.X1 = ParseDouble(firstLine[0]);
            CurrentCircleData.Y1 = ParseDouble(firstLine[1]);
            CurrentCircleData.R1 = ParseDouble(firstLine[2]);
            CurrentCircleData.X2 = ParseDouble(secondLine[0]);
            CurrentCircleData.Y2 = ParseDouble(secondLine[1]);
            CurrentCircleData.R2 = ParseDouble(secondLine[2]);

            if (CurrentCircleData.R1 < 0 || CurrentCircleData.R2 < 0)
            {
                string negativeRadius = CurrentCircleData.R1 < 0 && CurrentCircleData.R2 < 0
                    ? "Радиусы r1 и r2 отрицательные."
                    : CurrentCircleData.R1 < 0
                        ? "Радиус r1 отрицательный."
                        : "Радиус r2 отрицательный.";

                throw new ArgumentOutOfRangeException("r", negativeRadius);
            }

            if (CurrentCircleData.R1 == 0 || CurrentCircleData.R2 == 0)
            {
                throw new ArgumentOutOfRangeException("r", "Радиусы должны быть положительными числами.");
            }

            ValidateAndProcessData();
        }
        catch (FormatException ex)
        {
            OutputResult = "Ошибка: неверный формат данных.";
            ErrorMessage = $"Ошибка парсинга: {ex.Message}";
            StatusMessage = "Неверный ввод";
            IsValidData = false;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            OutputResult = "Ошибка: радиусы должны быть положительными числами.";
            ErrorMessage = ex.Message;
            StatusMessage = "Неверный ввод";
            IsValidData = false;
        }
        catch (Exception ex)
        {
            OutputResult = "Ошибка при обработке данных.";
            ErrorMessage = ex.Message;
            StatusMessage = "Ошибка";
            IsValidData = false;
        }
    }

    private string[] SplitLine(string line)
    {
        return line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private double ParseDouble(string value)
    {
        if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result))
            throw new FormatException($"Не удалось прочитать число: '{value}'. Используйте десятичную точку.");

        return result;
    }

    private void ValidateAndProcessData()
    {
        double dx = CurrentCircleData.X2 - CurrentCircleData.X1;
        double dy = CurrentCircleData.Y2 - CurrentCircleData.Y1;
        double centerDistance = Math.Sqrt(dx * dx + dy * dy);

        const double epsilon = 1e-6;
        double sumRadii = CurrentCircleData.R1 + CurrentCircleData.R2;
        double diffRadii = Math.Abs(CurrentCircleData.R1 - CurrentCircleData.R2);
        var points = CurrentCircleData.GetIntersectionPoints();

        if (centerDistance < epsilon && Math.Abs(CurrentCircleData.R1 - CurrentCircleData.R2) < epsilon)
        {
            OutputResult = "Окружности совпадают."
                           + $"\n\nРадиус: r1 = {CurrentCircleData.R1:F3}, r2 = {CurrentCircleData.R2:F3}";
            StatusMessage = "Окружности совпадают";
            ErrorMessage = string.Empty;
            IsValidData = true;
            return;
        }

        if (points.HasValue)
        {
            bool singlePoint = Math.Abs(points.Value.x1 - points.Value.x2) < epsilon &&
                               Math.Abs(points.Value.y1 - points.Value.y2) < epsilon;
            string intersectionInfo;

            if (singlePoint)
            {
                intersectionInfo = $"Точка пересечения: ({points.Value.x1:F3}, {points.Value.y1:F3})";
                StatusMessage = "Окружности касаются в одной точке";
            }
            else
            {
                intersectionInfo = $"Точка A: ({points.Value.x1:F3}, {points.Value.y1:F3})\n" +
                                   $"Точка B: ({points.Value.x2:F3}, {points.Value.y2:F3})";
                StatusMessage = "Окружности пересекаются";
            }

            OutputResult = (singlePoint ? "Окружности касаются в одной точке!\n\n" : "Окружности пересекаются!\n\n") +
                           $"Первая: центр ({CurrentCircleData.X1:F2}, {CurrentCircleData.Y1:F2}), r = {CurrentCircleData.R1:F2}\n" +
                           $"Вторая: центр ({CurrentCircleData.X2:F2}, {CurrentCircleData.Y2:F2}), r = {CurrentCircleData.R2:F2}\n\n" +
                           intersectionInfo + "\n\n" +
                           $"d = {centerDistance:F3}\n" +
                           $"r1 + r2 = {sumRadii:F3}\n" +
                           $"|r1 - r2| = {diffRadii:F3}";
            ErrorMessage = string.Empty;
            IsValidData = true;
            return;
        }

        string reason;
        if (centerDistance > sumRadii)
            reason = "Окружности удалены друг от друга.";
        else if (centerDistance < diffRadii)
            reason = "Одна окружность находится внутри другой без пересечения.";
        else if (Math.Abs(centerDistance - sumRadii) < epsilon)
            reason = "Окружности касаются внешне в одной точке.";
        else if (Math.Abs(centerDistance - diffRadii) < epsilon)
            reason = "Окружности касаются внутренне в одной точке.";
        else
            reason = "Окружности не пересекаются в одной точке.";

        OutputResult = "Окружности не пересекаются.\n\n" +
                       reason + "\n\n" +
                       $"d = {centerDistance:F3}\n" +
                       $"r1 + r2 = {sumRadii:F3}\n" +
                       $"|r1 - r2| = {diffRadii:F3}";
        StatusMessage = "Окружности не пересекаются";
        ErrorMessage = string.Empty;
        IsValidData = true;
    }

    private void ResetData()
    {
        InputData = "0 0 5\n8 0 5";
        OutputResult = "Результаты появятся после ввода данных";
        StatusMessage = "Ожидание ввода данных";
        ErrorMessage = string.Empty;
        IsValidData = false;
        CurrentCircleData = new CircleData();
    }

    private async System.Threading.Tasks.Task SaveResult(Window window)
    {
        if (!IsValidData)
        {
            ErrorMessage = "Невозможно сохранить результат: данные невалидны.";
            return;
        }

        try
        {
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить результат",
                DefaultExtension = "txt",
                SuggestedFileName = $"result_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            });

            if (file is not null)
            {
                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(OutputResult);
                await writer.FlushAsync();
                StatusMessage = $"Результат сохранён в файл {file.Name}";
                ErrorMessage = string.Empty;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
            StatusMessage = "Ошибка сохранения";
        }
    }
}
