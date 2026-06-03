using DetailTrack.Helpers;
using DetailTrack.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DetailTrack.ToolEngineers.Windows
{
    public partial class ToolEngineerRequestDetailsWindow : Window
    {
        private readonly ToolRequest _toolRequest;
        private ApplicationDbContext _context;

        public ToolEngineerRequestDetailsWindow(ToolRequest toolRequest)
        {
            _context = new ApplicationDbContext();
            _toolRequest = _context.ToolRequests.Find(toolRequest.Id)!;

            InitializeComponent();
            Closed += (s, e) => _context?.Dispose();

            LoadDetails();
        }

        private void LoadDetails()
        {
            _context.Entry(_toolRequest).Reload();
            var tr = _context.ToolRequests
                .Include(t => t.MainRequest)
                .Include(t => t.RequestedBy)
                .FirstOrDefault(t => t.Id == _toolRequest.Id);

            if (tr == null) { Close(); return; }

            Title = $"Заявка на инструмент: {tr.ToolName}";
            LblTitle.Text = $"{tr.ToolName}";
            LblStatus.Text = $"Текущий статус: {tr.Status}";

            // Информация
            LblToolName.Text = tr.ToolName;
            LblForDetail.Text = tr.MainRequest != null
                ? $"{tr.MainRequest.RequestNumber} — {tr.MainRequest.DetailName}"
                : "Не привязана к заявке";
            LblRequestedBy.Text = tr.RequestedBy?.FullName ?? "Неизвестно";
            LblComment.Text = tr.Comment;

            // Установка текущего статуса в ComboBox
            foreach (ComboBoxItem item in CbStatus.Items)
            {
                if (item.Content.ToString() == tr.Status)
                {
                    CbStatus.SelectedItem = item;
                    break;
                }
            }

            // Загрузка комментариев
            var comments = _context.Comments
                .Include(c => c.User)
                .Where(c => c.ToolRequestId == tr.Id)
                .OrderBy(c => c.CreatedAt)
                .ToList();
            IcComments.ItemsSource = comments;
        }

        private void BtnSaveStatus_Click(object sender, RoutedEventArgs e)
        {
            if (CbStatus.SelectedItem is not ComboBoxItem selectedItem)
            {
                MessageBox.Show("Выберите статус из списка", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newStatus = selectedItem.Content.ToString();
            var tr = _context.ToolRequests.Find(_toolRequest.Id);

            if (tr.Status == newStatus)
            {
                MessageBox.Show("Статус не изменился", "Информация",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string oldStatus = tr.Status;
            tr.Status = newStatus;
            _context.SaveChanges();

            // Если статус "Получен", фиксируем дату завершения
            if (newStatus == "Получен")
            {
                tr.CompletedAt = DateTime.Now;
                _context.SaveChanges();
            }

            LblStatus.Text = $"Текущий статус: {newStatus}";
            MessageBox.Show($"✅ Статус изменён на «{newStatus}»", "Успех",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAddComment_Click(object sender, RoutedEventArgs e)
        {
            string text = TxtCommentInput.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Введите текст комментария", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCommentInput.Focus();
                return;
            }

            _context.Comments.Add(new Comment
            {
                ToolRequestId = _toolRequest.Id,
                UserId = Session.CurrentUser.Id,
                Text = text,
                CreatedAt = DateTime.Now
            });

            _context.SaveChanges();
            TxtCommentInput.Clear();
            LoadDetails();

            // Прокрутка вниз к новому комментарию
            var scrollViewer = FindVisualChild<ScrollViewer>(IcComments);
            scrollViewer?.ScrollToEnd();
        }

        private void TxtComment_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                BtnAddComment_Click(null, null);
                e.Handled = true;
            }
        }

        // Вспомогательный метод для поиска ScrollViewer внутри ItemsControl
        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T value) return value;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
