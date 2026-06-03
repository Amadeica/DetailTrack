using DetailTrack.Helpers;
using DetailTrack.Models;
using DetailTrack.SetupTechnicians.Windows;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DetailTrack.SetupTechnicians
{
    public partial class SetupTechnicianDashboard : Window
    {
        private ApplicationDbContext _context;

        public SetupTechnicianDashboard()
        {
            _context = new ApplicationDbContext();
            InitializeComponent();
            Closed += (s, e) => _context?.Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            LoadRequestsList();
        }

        private void LoadRequestsList(string statusFilter = null, string searchQuery = null)
        {
            var showRemoved = ChkShowRemoved.IsChecked == true;
            var query = _context.Requests
                .Include(r => r.Programmer)
                .Include(r => r.MachineModel)
                .Include(r => r.MachineType)
                .Where(r => r.SetupTechnicianId == Session.CurrentUser.Id ||
                           r.Status == "Передано наладчику")
                .Where(r => r.IsRemoved == showRemoved)
                .AsQueryable();

            // Фильтр по статусу
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "Все статусы")
            {
                query = query.Where(r => r.Status == statusFilter);
            }

            // Поиск по названию детали
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(r => r.DetailName.Contains(searchQuery));
            }

            DgRequests.ItemsSource = query
                .OrderByDescending(r => r.ProgramUploadedAt ?? r.CreatedAt)
                .ToList();

            BtnDeleteRequest.Visibility = showRemoved ? Visibility.Collapsed : Visibility.Visible;
            UpdateDeleteButtonState();
        }

        private void UpdateDeleteButtonState()
        {
            BtnDeleteRequest.IsEnabled = DgRequests.SelectedItem is Request r && !r.IsRemoved;
        }

        private void CbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbStatusFilter.SelectedItem is ComboBoxItem item)
            {
                LoadRequestsList(item.Content.ToString(), TxtSearch?.Text?.Trim());
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadRequestsList(
                CbStatusFilter.SelectedItem is ComboBoxItem statusItem ? statusItem.Content.ToString() : null,
                TxtSearch.Text.Trim());
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnSearch_Click(null, null);
        }

        private void BtnResetSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Clear();
            CbStatusFilter.SelectedIndex = 0;
            ChkShowRemoved.IsChecked = false;
            LoadRequestsList();
        }

        private void ChkShowRemoved_Changed(object sender, RoutedEventArgs e) =>
            LoadRequestsList(
                CbStatusFilter.SelectedItem is ComboBoxItem item ? item.Content.ToString() : null,
                TxtSearch?.Text?.Trim());

        private void BtnDeleteRequest_Click(object sender, RoutedEventArgs e)
        {
            if (DgRequests.SelectedItem is not Request selected) return;

            if (RequestArchiveHelper.TryArchiveRequest(_context, selected, this))
            {
                MessageBox.Show("✅ Заявка перемещена в архив", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRequestsList(
                    CbStatusFilter.SelectedItem is ComboBoxItem item ? item.Content.ToString() : null,
                    TxtSearch?.Text?.Trim());
            }
        }

        private void DgRequests_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            UpdateDeleteButtonState();

        private void DgRequests_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgRequests.SelectedItem is not Request selected) return;

            var details = new SetupTechnicianRequestDetailsWindow(selected, selected.IsRemoved);
            details.Owner = this;
            details.Closed += (s, args) => LoadRequestsList(
                CbStatusFilter.SelectedItem is ComboBoxItem item ? item.Content.ToString() : null,
                TxtSearch?.Text?.Trim());
            details.ShowDialog();
            DgRequests.SelectedItem = null;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Session.Clear();
            new MainWindow().Show();
            Close();
        }

    }
}
