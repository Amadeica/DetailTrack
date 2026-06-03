using DetailTrack.Helpers;
using DetailTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DetailTrack.Programmers.Windows
{
    public partial class ProgrammerRequestDetailsWindow : Window
    {
        private readonly Request _request;
        private readonly bool _isArchiveView;
        private ApplicationDbContext _context;
        private string? _selectedProgramPath;

        public ProgrammerRequestDetailsWindow(Request request, bool isArchiveView = false)
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
                .AsNoTracking()
                .Include(r => r.Constructor)
                .Include(r => r.Technologist)
                .Include(r => r.MachineType)
                .Include(r => r.MachineModel)
                  .Include(r => r.Workshop)
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
            LblWorkShop.Text = req.Workshop?.Name ?? "—"; ; 
            LblMachine.Text = req.MachineModel != null
                ? $"{req.MachineType?.Name}: {req.MachineModel.Name}"
                : "Не указан";
            LblComment.Text = string.IsNullOrWhiteSpace(req.Comment) ? "—" : req.Comment;

            UpdateButtonsVisibility(req.Status);

            var techProcess = req.RequestFiles.FirstOrDefault(f => f.FileType == "TechProcess");
            if (techProcess != null)
            {
                IcTechProcess.ItemsSource = new[] { techProcess };
                LblNoTechProcess.Visibility = Visibility.Collapsed;
            }
            else
            {
                IcTechProcess.ItemsSource = null;
                LblNoTechProcess.Visibility = Visibility.Visible;
            }

            var program = req.RequestFiles.FirstOrDefault(f => f.FileType == "Program");
            if (program != null)
            {
                PanelProgramLoaded.Visibility = Visibility.Visible;
                PanelProgramUpload.Visibility = Visibility.Collapsed;
                LblProgramName.Text = program.FileName;
                LblProgramDate.Text = $"Загружена: {program.UploadedAt:dd.MM.yyyy HH:mm}";
            }
            else
            {
                PanelProgramLoaded.Visibility = Visibility.Collapsed;
                PanelProgramUpload.Visibility = Visibility.Visible;
            }

            var toolRequests = req.ToolRequests.OrderByDescending(t => t.CreatedAt).ToList();
            IcToolRequests.ItemsSource = toolRequests;
            LblNoToolRequests.Visibility = toolRequests.Any() ? Visibility.Collapsed : Visibility.Visible;

            IcHistory.ItemsSource = req.RequestHistories
            .OrderBy(h => h.ChangedAt)
            .ToList();

            if (_isArchiveView || req.IsRemoved)
                ApplyArchiveView();
        }

        private void ApplyArchiveView()
        {
            RequestArchiveHelper.ApplyArchiveBanner(this, LblStatus, LblStatus.Text);
            RequestArchiveUi.HideActions(BtnAccept, BtnForward);
            RequestArchiveUi.SetReadOnly(PanelProgramUpload);
        }


        private void UpdateButtonsVisibility(string status)
        {
            // Принять в работу: только если статус "В работе у программиста" и программа ещё не загружена
            bool canAccept = status == "Передано программисту" &&
                            !_context.RequestFiles.Any(f => f.RequestId == _request.Id && f.FileType == "Program");

            BtnAccept.IsEnabled = canAccept;
            BtnAccept.Visibility = canAccept ? Visibility.Visible : Visibility.Collapsed;

            // Передать наладчику: только если есть УП и статус позволяет
            bool hasProgram = _context.RequestFiles.Any(f => f.RequestId == _request.Id && f.FileType == "Program");
            bool canForward = hasProgram &&
                (status == "В работе у программиста" || status == "Программа готова");

            BtnForward.IsEnabled = canForward;
            BtnForward.Visibility = (status != "В производстве") ? Visibility.Visible : Visibility.Collapsed;

            // Подсказки
            if (!BtnForward.IsEnabled && status != "В производстве")
            {
                BtnForward.ToolTip = hasProgram ? null : "• Сначала загрузите управляющую программу";
            }
            else
            {
                BtnForward.ToolTip = null;
            }
        }

        // === Скачивание техпроцесса ===
        private void BtnDownloadTechProcess_Click(object sender, RoutedEventArgs e)
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

        // === Загрузка управляющей программы ===
        private void BtnSelectProgram_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Title = "Выберите управляющую программу",
                Filter = "Файлы ЧПУ|*.nc;*.cnc;*.mpf;*.spf;*.txt|Все файлы|*.*",
                CheckFileExists = true
            };

            if (openDlg.ShowDialog() == true)
            {
                _selectedProgramPath = openDlg.FileName;
                LblSelectedProgram.Text = System.IO.Path.GetFileName(_selectedProgramPath);
            }
        }

        private void BtnSaveProgram_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedProgramPath))
            {
                MessageBox.Show("Выберите файл управляющей программы", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Сохраняем файл и получаем относительный путь
                string relativePath = FileHelper.SaveFileAndGetRelativePath(
                    _selectedProgramPath, _request.Id, "Program");

                // Добавляем запись в БД
                _context.RequestFiles.Add(new RequestFile
                {
                    RequestId = _request.Id,
                    FileName = System.IO.Path.GetFileName(_selectedProgramPath),
                    FilePath = relativePath,
                    FileType = "Program",
                    UploadedById = Session.CurrentUser.Id,
                    UploadedAt = DateTime.Now
                });

                // Фиксируем дату загрузки программы в заявке
                var req = _context.Requests.Find(_request.Id);
                if (req != null)
                {
                    req.ProgramUploadedAt = DateTime.Now;
                    req.Status = "Программа готова";

                    AddHistoryEntry(req, "В работе у программиста", "Программа готова", "Программист загрузил управляющую программу");

                }

                _context.SaveChanges();

                MessageBox.Show("✅ Управляющая программа успешно загружена", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                LoadDetails();
                _selectedProgramPath = null;
                LblSelectedProgram.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Принятие заявки в работу ===
        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            var req = _context.Requests.Find(_request.Id);
            if (req == null || req.Status != "Передано программисту") return;

            // Фиксируем программиста, если ещё не назначен
            if (req.ProgrammerId == null)
                req.ProgrammerId = Session.CurrentUser.Id;

            AddHistoryEntry(req, req.Status, "В работе у программиста", "Программист принял заявку в работу");
            req.Status = "В работе у программиста";

            _context.SaveChanges();

            MessageBox.Show("✅ Заявка принята в работу", "Успех",
                          MessageBoxButton.OK, MessageBoxImage.Information);

            LoadDetails();
        }

        // === Передача наладчику ===
        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            var req = _context.Requests.Find(_request.Id);
            if (req == null) return;

            // Проверка: есть ли управляющая программа
            bool hasProgram = _context.RequestFiles
                .Any(f => f.RequestId == req.Id && f.FileType == "Program");

            if (!hasProgram)
            {
                MessageBox.Show("Сначала загрузите управляющую программу", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var setupTechs = _context.Users
                .Where(u => u.Role.Name == "Наладчик" && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToList();

            if (!setupTechs.Any())
            {
                MessageBox.Show("В системе нет активных наладчиков", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 🔹 Диалог выбора наладчика
            var selectDlg = new Window
            {
                Title = "Выберите наладчика",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var comboBox = new ComboBox { Margin = new Thickness(15), Width = 340, DisplayMemberPath = "FullName" };
            comboBox.ItemsSource = setupTechs;
            comboBox.SelectedIndex = 0;

            var btnOk = new Button
            {
                Content = "Передать",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 15, 10),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(155, 89, 182)),
                Foreground = System.Windows.Media.Brushes.White
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Наладчик:", Margin = new Thickness(15, 10, 0, 5) });
            panel.Children.Add(comboBox);
            panel.Children.Add(btnOk);
            selectDlg.Content = panel;

            btnOk.Click += (s, args) =>
            {
                if (comboBox.SelectedItem is User setupTech)
                {
                    try
                    {
              
                        req.SetupTechnicianId = setupTech.Id;
                        req.Status = "Передано наладчику"; 

                        AddHistoryEntry(req, "В работе у программиста", "Передано наладчику",
                            $"Передано наладчику {setupTech.FullName}");

                        _context.SaveChanges();

                        MessageBox.Show(
                            $"✅ Заявка передана наладчику {setupTech.FullName}\n" +
                            $"Статус изменён на «Программа готова»",
                            "Успех",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        selectDlg.DialogResult = true;
                        selectDlg.Close();
                        LoadDetails();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"❌ Ошибка при передаче: {ex.Message}", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };

            selectDlg.ShowDialog();
        }

        // === Скачивание управляющей программы ===
        private void BtnDownloadProgram_Click(object sender, RoutedEventArgs e)
        {
            var program = _context.RequestFiles
                .FirstOrDefault(f => f.RequestId == _request.Id && f.FileType == "Program");

            if (program != null)
                DownloadFile(program);
        }

        // === Вспомогательные методы ===
        private void AddHistoryEntry(Request req, string? oldStatus, string? newStatus, string comment)
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
