using DetailTrack.Helpers;
using DetailTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DetailTrack.Technologists.Windows
{
    public partial class TechnologistRequestDetailsWindow : Window
    {
        private readonly Request _request;
        private readonly bool _isArchiveView;
        private ApplicationDbContext _context;
        private string? _selectedTechProcessPath;

        public TechnologistRequestDetailsWindow(Request request, bool isArchiveView = false)
        {
            _request = request;
            _isArchiveView = isArchiveView;
            _context = new ApplicationDbContext();

            InitializeComponent();
            Closed += (s, e) => _context?.Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDetails();
        }

        private void LoadDetails()
        {
            _context.Entry(_request).Reload();

            var requests = _context.Requests
                .AsNoTracking()
                .Include(r => r.Constructor)
                .Include(r => r.Programmer)
                .Include(r => r.Workshop)
                .Include(r => r.MachineType)
                .Include(r => r.MachineModel)
                .Include(r => r.RequestFiles)
                .Include(r => r.ToolRequests)
                .Include(r => r.RequestHistories)
                    .ThenInclude(h => h.ChangedBy)
                .FirstOrDefault(r => r.Id == _request.Id);

            if (requests == null) { Close(); return; }

            Title = $"Заявка: {requests.RequestNumber}";
            LblTitle.Text = $"{requests.RequestNumber} — {requests.DetailName}";
            LblStatus.Text = $"Статус: {requests.Status}";

            LblType.Text = requests.Type == "New" ? "Новая" : "Изменение";
            LblConstructor.Text = requests.Constructor?.FullName ?? "—";
            LblProgrammer.Text = requests.Programmer?.FullName ?? "Не назначен";
            var machineInfo = requests.MachineModel != null
                ? $"{requests.MachineType?.Name}: {requests.MachineModel.Name}"
                : "Не указан";
            var workshopInfo = requests.Workshop?.Name ?? "Не указан";
            LblMachine.Text = $"Цех: {workshopInfo} | Станок: {machineInfo}";
            LblComment.Text = string.IsNullOrWhiteSpace(requests.Comment) ? "—" : requests.Comment;

            UpdateButtonsVisibility(requests.Status);

            var drawings = requests.RequestFiles.Where(f => f.FileType == "Drawing").ToList();
            IcDrawings.ItemsSource = drawings;
            LblNoDrawings.Visibility = drawings.Any() ? Visibility.Collapsed : Visibility.Visible;

            var techProcess = requests.RequestFiles.FirstOrDefault(f => f.FileType == "TechProcess");
            if (techProcess != null)
            {
                PanelTechProcessLoaded.Visibility = Visibility.Visible;
                PanelTechProcessUpload.Visibility = Visibility.Collapsed;
                LblTechProcessName.Text = techProcess.FileName;
                LblTechProcessDate.Text = $"Загружен: {techProcess.UploadedAt:dd.MM.yyyy HH:mm}";
            }
            else
            {
                PanelTechProcessLoaded.Visibility = Visibility.Collapsed;
                PanelTechProcessUpload.Visibility = Visibility.Visible;
            }

            LoadWorkshops();
            if (requests.WorkshopId.HasValue)
                CbWorkshop.SelectedValue = requests.WorkshopId;

            LoadMachineTypes();
            if (requests.MachineTypeId.HasValue)
                CbMachineType.SelectedValue = requests.MachineTypeId;

            if (requests.MachineModelId.HasValue)
                CbMachineModel.SelectedValue = requests.MachineModelId;

            IcToolRequests.ItemsSource = requests.ToolRequests.OrderByDescending(t => t.CreatedAt).ToList();

            LoadToolEngineerComments(requests.ToolRequests.Select(t => t.Id).ToList());

            bool isLocked = requests.Status != "В разработке у технолога"; 

            CbWorkshop.IsEnabled = !isLocked;
            CbMachineType.IsEnabled = !isLocked;
            CbMachineModel.IsEnabled = !isLocked;

            if (isLocked)
            {
                CbWorkshop.Opacity = 0.6;
                CbMachineType.Opacity = 0.6;
                CbMachineModel.Opacity = 0.6;
            }

            BtnCreateToolRequest.IsEnabled = !isLocked;
            if (isLocked)
            {
                BtnCreateToolRequest.Opacity = 0.6;
                BtnCreateToolRequest.ToolTip = "Редактирование заблокировано: заявка уже передана программисту";
                BtnCreateToolRequest.Foreground = Brushes.Black;
            }
            else
            {
                BtnCreateToolRequest.ToolTip = null;
            }

            IcHistory.ItemsSource = requests.RequestHistories
               .OrderBy(h => h.ChangedAt)
               .ToList();

            if (_isArchiveView || requests.IsRemoved)
                ApplyArchiveView();
        }

        private void ApplyArchiveView()
        {
            RequestArchiveHelper.ApplyArchiveBanner(this, LblStatus, LblStatus.Text);
            RequestArchiveUi.HideActions(BtnAccept, BtnForward, BtnCreateToolRequest);
            RequestArchiveUi.SetReadOnly(
                GrbPatameters, GrbTechProcessName, GrbCreateToolRequest,
                CbWorkshop, CbMachineType, CbMachineModel,
                PanelTechProcessUpload, BtnSaveTechProcess);
        }

        private void LoadToolEngineerComments(List<int> toolRequestIds)
        {
            if (!toolRequestIds.Any())
            {
                IcToolEngineerComments.ItemsSource = null;
                LblNoToolEngineerComments.Visibility = Visibility.Visible;
                return;
            }

            var comments = _context.Comments
                .Include(c => c.User)
                .Include(c => c.ToolRequest)
                .Where(c => c.ToolRequestId != null && toolRequestIds.Contains(c.ToolRequestId.Value))
                .OrderBy(c => c.CreatedAt)
                .ToList();

            IcToolEngineerComments.ItemsSource = comments;
            LblNoToolEngineerComments.Visibility = comments.Any()
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void LoadWorkshops()
        {
            CbWorkshop.ItemsSource = _context.Workshops.ActiveOnly().OrderBy(w => w.Name).ToList();
        }

        private void LoadMachineTypes()
        {
            var types = _context.MachineTypes.ActiveOnly().OrderBy(t => t.Name).ToList();
            CbMachineType.ItemsSource = types;
        }

        private void CbMachineType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbMachineType.SelectedValue is int typeId)
            {
                var models = _context.MachineModels
                    .ActiveOnly()
                    .Where(m => m.MachineTypeId == typeId)
                    .OrderBy(m => m.Name)
                    .ToList();
                CbMachineModel.ItemsSource = models;
                CbMachineModel.IsEnabled = models.Any();
            }
            else
            {
                CbMachineModel.ItemsSource = null;
                CbMachineModel.IsEnabled = false;
            }

            UpdateButtonsVisibility(_request.Status);
        }

        private void UpdateButtonsVisibility(string status)
        {
            BtnAccept.IsEnabled = status == "Создана";
            BtnAccept.Visibility = status == "Создана" ? Visibility.Visible : Visibility.Collapsed;
            GrbTechProcessName.Visibility = status == "Создана" ? Visibility.Collapsed : Visibility.Visible;
            GrbPatameters.Visibility = status == "Создана" ? Visibility.Collapsed : Visibility.Visible;
            GrbCreateToolRequest.Visibility = status == "Создана" ? Visibility.Collapsed : Visibility.Visible;

            bool hasTechProcess = _context.RequestFiles
                .Any(f => f.RequestId == _request.Id && f.FileType == "TechProcess");

            bool hasProcessingParams = CbWorkshop.SelectedValue != null &&
                                       CbMachineType.SelectedValue != null &&
                                       CbMachineModel.SelectedValue != null;

            BtnForward.IsEnabled = hasTechProcess && hasProcessingParams &&
                (status == "В разработке у технолога" || status == "Создана");

            BtnForward.Visibility = status == "В разработке у технолога" ? Visibility.Visible : Visibility.Collapsed;

            if (!BtnForward.IsEnabled && status != "В разработке у технолога")
            {
                string tooltip = "";
                if (!hasTechProcess) tooltip += "• Загрузите техпроцесс\n";
                if (!hasProcessingParams) tooltip += "• Укажите цех, тип и модель станка";
                BtnForward.ToolTip = tooltip.Trim();
            }
            else
            {
                BtnForward.ToolTip = null;
            }
        }

        private void BtnDownloadDrawing_Click(object sender, RoutedEventArgs e)
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

        private void BtnSelectTechProcess_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Title = "Выберите файл техпроцесса",
                Filter = "Документы|*.pdf;*.doc;*.docx;*.txt|Все файлы|*.*",
                CheckFileExists = true
            };

            if (openDlg.ShowDialog() == true)
            {
                _selectedTechProcessPath = openDlg.FileName;
                LblSelectedTechProcess.Text = System.IO.Path.GetFileName(_selectedTechProcessPath);
            }
        }

        private void BtnSaveTechProcess_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedTechProcessPath))
            {
                MessageBox.Show("Выберите файл техпроцесса", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string relativePath = FileHelper.SaveFileAndGetRelativePath(
                    _selectedTechProcessPath, _request.Id, "TechProcess");

                _context.RequestFiles.Add(new RequestFile
                {
                    RequestId = _request.Id,
                    FileName = System.IO.Path.GetFileName(_selectedTechProcessPath),
                    FilePath = relativePath,
                    FileType = "TechProcess",
                    UploadedById = Session.CurrentUser.Id,
                    UploadedAt = DateTime.Now
                });

                var req = _context.Requests.Find(_request.Id);
                if (req != null)
                {
                    req.TechProcessUploadedAt = DateTime.Now;
                    if (req.Status == "Создана")
                    {
                        req.Status = "В разработке у технолога";
                        AddHistoryEntry(req, null, "В разработке у технолога", "Технолог принял заявку в работу");
                    }
                }

                _context.SaveChanges();

                MessageBox.Show("✅ Техпроцесс успешно загружен", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                LoadDetails();
                _selectedTechProcessPath = null;
                LblSelectedTechProcess.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            var req = _context.Requests.Find(_request.Id);
            if (req == null || req.Status != "Создана") return;

            req.Status = "В разработке у технолога";
            req.TechnologistId = Session.CurrentUser.Id;

            AddHistoryEntry(req, "Создана", "В разработке у технолога", "Технолог принял заявку в работу");
            _context.SaveChanges();

            MessageBox.Show("✅ Заявка принята в работу", "Успех",
                          MessageBoxButton.OK, MessageBoxImage.Information);

            LoadDetails();
        }

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            var req = _context.Requests.Find(_request.Id);
            if (req == null) return;

            var techProcess = _context.RequestFiles
                .FirstOrDefault(f => f.RequestId == req.Id && f.FileType == "TechProcess");

            if (techProcess == null)
            {
                MessageBox.Show(
                    "❌ Нельзя передать заявку программисту без техпроцесса.\n" +
                    "Сначала загрузите файл техпроцесса в разделе «📋 Техпроцесс».",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            int? workshopId = CbWorkshop.SelectedValue as int?;
            if (!workshopId.HasValue)
            {
                MessageBox.Show(
                    "❌ Не выбран цех.\n" +
                    "Укажите цех в разделе «⚙️ Параметры обработки».",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                CbWorkshop.Focus();
                return;
            }

            int? machineTypeId = CbMachineType.SelectedValue as int?;
            if (!machineTypeId.HasValue)
            {
                MessageBox.Show(
                    "❌ Не выбран тип станка.\n" +
                    "Укажите тип станка в разделе «⚙️ Параметры обработки».",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                CbMachineType.Focus();
                return;
            }

            int? machineModelId = CbMachineModel.SelectedValue as int?;
            if (!machineModelId.HasValue)
            {
                MessageBox.Show(
                    "❌ Не выбрана модель станка.\n" +
                    "Укажите модель станка в разделе «⚙️ Параметры обработки».",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                CbMachineModel.Focus();
                return;
            }

            // 5. Проверка: модель принадлежит выбранному типу
            var model = _context.MachineModels.Find(machineModelId);
            if (model == null || model.MachineTypeId != machineTypeId)
            {
                MessageBox.Show(
                    "❌ Выбранная модель станка не соответствует указанному типу.\n" +
                    "Проверьте параметры обработки.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            req.WorkshopId = workshopId;
            req.MachineTypeId = machineTypeId;
            req.MachineModelId = machineModelId;

            var programmers = _context.Users
                .Where(u => u.Role.Name == "Программист" && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToList();

            if (!programmers.Any())
            {
                MessageBox.Show(
                    "❌ В системе нет активных программистов.\n" +
                    "Обратитесь к администратору.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var selectDlg = new Window
            {
                Title = "Выберите программиста",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var comboBox = new ComboBox
            {
                Margin = new Thickness(15),
                Width = 340,
                DisplayMemberPath = "FullName"
            };
            comboBox.ItemsSource = programmers;
            comboBox.SelectedIndex = 0;

            var btnOk = new Button
            {
                Content = "Передать",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 15, 10),
                Background = new SolidColorBrush(
                    Color.FromRgb(52, 152, 219)),
                Foreground = Brushes.White
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "Программист:",
                Margin = new Thickness(15, 10, 0, 5)
            });
            panel.Children.Add(comboBox);
            panel.Children.Add(btnOk);
            selectDlg.Content = panel;

            btnOk.Click += (s, args) =>
            {
                if (comboBox.SelectedItem is User programmer)
                {
                    try
                    {
                        req.ProgrammerId = programmer.Id;
                        req.Status = "Передано программисту";
                        req.ProgramUploadedAt = DateTime.Now; 

                        AddHistoryEntry(req, "В разработке у технолога", "Передано программисту",
                        $"Передано программисту {programmer.FullName}");

                        _context.SaveChanges();

                        MessageBox.Show(
                            $"✅ Заявка успешно передана программисту {programmer.FullName}\n" +
                            $"Цех: {CbWorkshop.Text}\nСтанок: {CbMachineType.Text} — {CbMachineModel.Text}",
                            "Успех",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        selectDlg.DialogResult = true;
                        selectDlg.Close();
                        LoadDetails();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"❌ Ошибка при передаче заявки:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            };

            selectDlg.ShowDialog();
        }

        private void BtnCreateToolRequest_Click(object sender, RoutedEventArgs e)
        {
            var toolReqWindow = new TechnologistCreateToolRequestWindow(_request.Id);
            toolReqWindow.Owner = this;
            toolReqWindow.Closed += (s, args) => LoadDetails();
            toolReqWindow.ShowDialog();
        }

        private void BtnDownloadTechProcess_Click(object sender, RoutedEventArgs e)
        {
            var techProcess = _context.RequestFiles
                .FirstOrDefault(f => f.RequestId == _request.Id && f.FileType == "TechProcess");

            if (techProcess != null)
                DownloadFile(techProcess);
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

        private void CbWorkshop_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            UpdateButtonsVisibility(_request.Status);

        private void CbMachineModel_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            UpdateButtonsVisibility(_request.Status);
    }
}
