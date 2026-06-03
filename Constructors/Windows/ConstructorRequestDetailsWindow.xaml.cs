using DetailTrack.Helpers;
using DetailTrack.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace DetailTrack.Constructors.Windows
{
    public partial class ConstructorRequestDetailsWindow : Window
    {
        private ApplicationDbContext _context;
        private readonly Request _request;
        private readonly bool _isArchiveView;

        public ConstructorRequestDetailsWindow(Request request, bool isArchiveView = false)
        {
            _context = new ApplicationDbContext();
            _request = request;
            _isArchiveView = isArchiveView;

            InitializeComponent();
            Closed += (s, e) => _context?.Dispose();
            LoadDetails();
        }

        private void LoadDetails()
        {
            var req = _context.Requests
                .Include(r => r.Technologist)
                .Include(r => r.RequestFiles)
                .Include(r => r.Programmer)
                .Include(r => r.Workshop)
                .Include(r => r.MachineType)
                .Include(r => r.MachineModel)
                .Include(r => r.RequestHistories)
                    .ThenInclude(h => h.ChangedBy)
                .FirstOrDefault(r => r.Id == _request.Id);

            if (req == null) return;

            Title = $"Заявка: {req.RequestNumber}";
            LblTitle.Text = $"{req.RequestNumber} — {req.DetailName}";
            LblStatus.Text = $"Статус: {req.Status}";

            LblType.Text = req.Type;
            LblConstructor.Text = req.Constructor?.FullName ?? "—";
            LblTechnologist.Text = req.Technologist?.FullName ?? "—";
            LblProgrammer.Text = req.Programmer?.FullName ?? "—";
            LblMachine.Text = req.MachineModel != null
                ? $"{req.MachineType?.Name}: {req.MachineModel.Name}"
                : "Не указан";
            LblComment.Text = string.IsNullOrWhiteSpace(req.Comment) ? "—" : req.Comment;
            LblWorkshop.Text = req.Workshop?.Name ?? "—";

            IcFiles.ItemsSource = req.RequestFiles
                .Where(f => f.FileType == "Drawing")
                .OrderBy(f => f.UploadedAt)
                .ToList();

            IcHistory.ItemsSource = req.RequestHistories
                .OrderBy(h => h.ChangedAt)
                .ToList();

            if (_isArchiveView || req.IsRemoved)
                RequestArchiveHelper.ApplyArchiveBanner(this, LblStatus, $"Статус: {req.Status}");
        }

        private string GetStatusDisplayName(string status) => status switch
        {
            "Created" => "Создана",
            "InDevelopment" => "В разработке",
            "ProgramReady" => "Программа готова",
            "InProduction" => "В производстве",
            "Completed" => "Завершена",
            _ => status
        };

        private void BtnDownloadFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is RequestFile file)
            {
                string sourcePath = FileHelper.GetAbsolutePath(file.FilePath);

                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show(
                        $"Файл не найден в хранилище:\n{file.FilePath}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var saveDlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = file.FileName, 
                    Filter = "Все файлы|*.*", 
                    Title = $"Сохранить файл: {file.FileName}",
                    AddExtension = true,
                    OverwritePrompt = true,
                    ValidateNames = true
                };

                if (saveDlg.ShowDialog() == true)
                {
                    try
                    {
                        File.Copy(sourcePath, saveDlg.FileName, overwrite: true);

                        MessageBox.Show(
                            $"Файл успешно сохранён:\n{saveDlg.FileName}",
                            "Готово",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show(
                            "Нет прав для записи в выбранную папку.\nПопробуйте выбрать другое место.",
                            "Ошибка доступа",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show(
                            $"Ошибка при сохранении файла:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Неожиданная ошибка:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
