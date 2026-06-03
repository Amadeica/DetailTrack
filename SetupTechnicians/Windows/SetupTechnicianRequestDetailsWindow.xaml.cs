using DetailTrack.Helpers;
using DetailTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DetailTrack.SetupTechnicians.Windows
{
    public partial class SetupTechnicianRequestDetailsWindow : Window
    {
        private readonly Request _request;
        private readonly bool _isArchiveView;
        private ApplicationDbContext _context;

        public SetupTechnicianRequestDetailsWindow(Request request, bool isArchiveView = false)
        {
            _request = request;
            _isArchiveView = isArchiveView;
            _context = new ApplicationDbContext();

            InitializeComponent();
            Closed += (s, e) => _context?.Dispose();

            LoadDetails();
        }

        private void LoadDetails()
        {
            _context.Entry(_request).Reload();
            var req = _context.Requests
                .Include(r => r.Constructor)
                .Include(r => r.Technologist)
                .Include(r => r.Programmer)
                .Include(r => r.Workshop)
                .Include(r => r.MachineType)
                .Include(r => r.MachineModel)
                .Include(r => r.RequestFiles)
                .Include(r => r.ToolRequests)
                .Include(r => r.RequestHistories)
                    .ThenInclude(h => h.ChangedBy)
                .FirstOrDefault(r => r.Id == _request.Id);

            if (req == null) { Close(); return; }

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

            LblMachineType.Text = req.MachineType?.Name ?? "—";
            LblMachineModel.Text = req.MachineModel?.Name ?? "—";

            UpdateButtonsVisibility(req.Status);

            var techProcess = req.RequestFiles.FirstOrDefault(f => f.FileType == "TechProcess");
            IcTechProcess.ItemsSource = techProcess != null ? new[] { techProcess } : null;

            var program = req.RequestFiles.FirstOrDefault(f => f.FileType == "Program");
            IcProgram.ItemsSource = program != null ? new[] { program } : null;

            var toolRequests = req.ToolRequests.OrderByDescending(t => t.CreatedAt).ToList();
            IcToolRequests.ItemsSource = toolRequests;
            LblNoToolRequests.Visibility = toolRequests.Any() ? Visibility.Collapsed : Visibility.Visible;

            GrpImplementation.Visibility = (req.Status == "В разработке у наладчика")
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (req.Status == "В разработке у наладчика" && req.ImplementationDate.HasValue)
            {
                DpImplementationDate.SelectedDate = req.ImplementationDate;
            }
            else if (req.Status == "В производстве" && req.ImplementationDate.HasValue)
            {
                DpImplementationDate.SelectedDate = req.ImplementationDate;
                DpImplementationDate.IsEnabled = false;
                TxtImplementationNote.IsEnabled = false;
            }

            IcHistory.ItemsSource = req.RequestHistories
                .OrderBy(h => h.ChangedAt)
                .ToList();

            if (_isArchiveView || req.IsRemoved)
                ApplyArchiveView();
        }

        private void ApplyArchiveView()
        {
            RequestArchiveHelper.ApplyArchiveBanner(this, LblStatus, LblStatus.Text);
            RequestArchiveUi.HideActions(BtnAccept, BtnComplete);
            RequestArchiveUi.SetReadOnly(GrpImplementation, DpImplementationDate, TxtImplementationNote);
        }

        private void UpdateButtonsVisibility(string status)
        {
            BtnAccept.IsEnabled = (status == "Передано наладчику");
            BtnAccept.Visibility = (status == "Передано наладчику") ? Visibility.Visible : Visibility.Collapsed;

            BtnComplete.IsEnabled = (status == "В разработке у наладчика");
            BtnComplete.Visibility = (status == "В разработке у наладчика" || status == "В производстве")
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (status == "В производстве")
            {
                BtnComplete.Content = "✅ Завершено";
                BtnComplete.IsEnabled = false;
                BtnComplete.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(46, 204, 113));
            }
        }

        private void BtnDownloadFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RequestFile file)
            {
                DownloadFile(file);
            }
        }

        private void DownloadFile(RequestFile file)
        {
            string sourcePath = FileHelper.GetAbsolutePath(file.FilePath);
            if (!File.Exists(sourcePath))
            {
                MessageBox.Show($"Файл не найден: {file.FilePath}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDlg = new SaveFileDialog
            {
                FileName = file.FileName,
                Filter = "Все файлы|*.*",
                Title = $"Сохранить: {file.FileName}",
                OverwritePrompt = true
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    File.Copy(sourcePath, saveDlg.FileName, true);
                    MessageBox.Show("✅ Файл сохранён", "Готово",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            var req = _context.Requests.Find(_request.Id);
            if (req == null || req.Status != "Передано наладчику") return;

            if (req.SetupTechnicianId == null)
                req.SetupTechnicianId = Session.CurrentUser.Id;

            req.Status = "В разработке у наладчика";

            AddHistoryEntry(req, "Передано наладчику", "В разработке у наладчика",
                "Наладчик принял заявку в работу");

            _context.SaveChanges();

            MessageBox.Show("✅ Заявка принята в работу", "Успех",
                          MessageBoxButton.OK, MessageBoxImage.Information);

            LoadDetails();
        }

        private void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            var req = _context.Requests.Find(_request.Id);
            if (req == null || req.Status != "В разработке у наладчика") return;

            if (!DpImplementationDate.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "❌ Укажите дату внедрения детали",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                req.ImplementationDate = DpImplementationDate.SelectedDate.Value;
                req.Status = "В производстве";

                string note = string.IsNullOrWhiteSpace(TxtImplementationNote.Text)
                    ? "Деталь внедрена в производство"
                    : $"Внедрено: {TxtImplementationNote.Text.Trim()}";

                AddHistoryEntry(req, "В разработке у наладчика", "В производстве", note);

                _context.SaveChanges();

                MessageBox.Show(
                    $"✅ Деталь успешно внедрена в производство!\n" +
                    $"Дата: {req.ImplementationDate:dd.MM.yyyy}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка при внедрении: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddHistoryEntry(Request req, string oldStatus, string newStatus, string comment)
        {
            _context.RequestHistories.Add(new RequestHistory
            {
                RequestId = req.Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Comment = comment,
                ChangedById = Session.CurrentUser.Id,
                ChangedAt = DateTime.Now
            });
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
