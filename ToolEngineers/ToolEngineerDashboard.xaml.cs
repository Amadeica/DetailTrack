using DetailTrack.Helpers;
using DetailTrack.Models;
using DetailTrack.ToolEngineers.Windows;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DetailTrack.ToolEngineers
{
    public partial class ToolEngineerDashboard : Window
    {
        private ApplicationDbContext _context;

        public ToolEngineerDashboard()
        {
            _context = new ApplicationDbContext();
            InitializeComponent();
            Closed += (s, e) => _context?.Dispose();

            LoadDashboard();
        }

        private void LoadDashboard()
        {
            if (Session.CurrentUser == null)
            {
                MessageBox.Show("Сессия истекла. Выполните вход повторно.", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                new MainWindow().Show();
                Close();
                return;
            }

            LblWelcome.Text = $"Добро пожаловать, {Session.CurrentUser.FullName}!";
            LoadToolRequestsList();
        }

        private void LoadToolRequestsList()
        {
            string? statusFilter = null;
            string? searchQuery = TxtSearch.Text.Trim(); 

            if (CbStatusFilter.SelectedItem is ComboBoxItem item)
            {
                statusFilter = item.Content.ToString();
            }

            var query = _context.ToolRequests
                .AsNoTracking()
                .Include(t => t.MainRequest)
                .Include(t => t.RequestedBy)
                // Показываем заявки, назначенные текущему инженеру, и неназначенные
                .Where(t => t.AssignedEngineerId == Session.CurrentUser.Id || t.AssignedEngineerId == null)
                .AsQueryable();

            // Фильтр по статусу
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "Все статусы")
            {
                query = query.Where(t => t.Status == statusFilter);
            }

            // Поиск по названию инструмента или детали
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(t => t.ToolName.Contains(searchQuery) ||
                                         (t.MainRequest != null && t.MainRequest.DetailName.Contains(searchQuery)));
            }

            DgToolRequests.ItemsSource = query
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        private void CbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbStatusFilter.SelectedItem is ComboBoxItem item)
            {
                LoadToolRequestsList();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadToolRequestsList();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnSearch_Click(null, null);
        }

        private void BtnResetSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Clear();
            CbStatusFilter.SelectedIndex = 0;
            LoadToolRequestsList();
        }

        private void DgToolRequests_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgToolRequests.SelectedItem is ToolRequest selected)
            {
                // Автоматически назначаем себя, если заявка ещё не назначена
                if (selected.AssignedEngineerId == null)
                {
                    selected.AssignedEngineerId = Session.CurrentUser.Id;
                    _context.SaveChanges();
                }

                var details = new ToolEngineerRequestDetailsWindow(selected);
                details.Owner = this;
                details.Closed += (s, args) => LoadToolRequestsList();
                details.ShowDialog();
                DgToolRequests.SelectedItem = null;
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Session.Clear();
            new MainWindow().Show();
            Close();
        }
    }
}
