using DetailTrack.Helpers;
using DetailTrack.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DetailTrack.Constructors.Windows
{
    public class FileItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType => "Drawing";
    }

    public partial class ConstructorCreateRequestWindow : Window
    {
        private ObservableCollection<FileItem> _attachedFiles = new();
        private ApplicationDbContext _context;

        public ConstructorCreateRequestWindow()
        {
            _context = new ApplicationDbContext();

            InitializeComponent();
            LoadTechnologists();
            IcFiles.ItemsSource = _attachedFiles;
        }

        private void LoadTechnologists()
        {
            var technologists = _context.Users
                .Where(u => u.Role.Name == "Технолог" && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToList();

            CbTechnologists.ItemsSource = technologists;

            if (technologists.Any())
                CbTechnologists.SelectedIndex = 0;
        }

        private void BtnAddFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите чертежи",
                Filter = "Чертежи и модели|*.dwg;*.png;*.jpg;*.jpeg;*.dxf;*.step;*.stp;*.iges;*.igs;*.pdf|Все файлы|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    if (!_attachedFiles.Any(f => f.FileName == Path.GetFileName(file)))
                    {
                        _attachedFiles.Add(new FileItem
                        {
                            FileName = Path.GetFileName(file),
                            FilePath = file
                        });
                    }
                }
            }
        }

        private void BtnRemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is FileItem file)
            {
                _attachedFiles.Remove(file);
            }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtDetailName.Text))
            {
                MessageBox.Show("Укажите название детали", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtDetailName.Focus();
                return;
            }

            if (CbTechnologists.SelectedItem == null)
            {
                MessageBox.Show("Выберите технолога", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_attachedFiles.Any())
            {
                var result = MessageBox.Show("Не прикреплены чертежи. Продолжить?",
                                           "Подтверждение",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }

            try
            {

                string requestNumber = GenerateRequestNumber();

                var newRequest = new Request
                {
                    RequestNumber = requestNumber,
                    DetailName = TxtDetailName.Text.Trim(),
                    Type = RbNew.IsChecked == true ? "Новая" : "Изменение",
                    Status = "Создана",
                    IsRemoved = false,
                    ConstructorId = Session.CurrentUser.Id,
                    TechnologistId = (int)CbTechnologists.SelectedValue,
                    Comment = TxtComment.Text.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.Requests.Add(newRequest);
                _context.SaveChanges();

                SaveAttachedFiles(newRequest.Id);

                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = newRequest.Id,
                    OldStatus = null,
                    NewStatus = "Создана",
                    Comment = "Заявка создана конструктором",
                    ChangedById = Session.CurrentUser.Id,
                    ChangedAt = DateTime.Now
                });
                _context.SaveChanges();

                MessageBox.Show($"Заявка {requestNumber} успешно создана!", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании заявки:\n{ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateRequestNumber()
        {
            const string prefix = "Z-";
            int maxNum = _context.Requests
                .Where(r => r.RequestNumber.StartsWith(prefix))
                .Select(r => r.RequestNumber)
                .AsEnumerable()
                .Select(rn => int.TryParse(rn[prefix.Length..], out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{prefix}{maxNum + 1}";
        }

        private void SaveAttachedFiles(int requestId)
        {
            foreach (var file in _attachedFiles)
            {
                string relativePath = FileHelper.SaveFileAndGetRelativePath(
                    file.FilePath,
                    requestId,
                    "Drawing");

                _context.RequestFiles.Add(new RequestFile
                {
                    RequestId = requestId,
                    ToolRequestId = null,
                    FileName = file.FileName,          
                    FilePath = relativePath,           
                    FileType = "Drawing",
                    UploadedById = Session.CurrentUser.Id,
                    UploadedAt = DateTime.Now
                });
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
