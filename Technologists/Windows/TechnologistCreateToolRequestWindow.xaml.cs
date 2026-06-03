using DetailTrack.Helpers;
using DetailTrack.Models;
using System.Windows;

namespace DetailTrack.Technologists.Windows
{
    public partial class TechnologistCreateToolRequestWindow : Window
    {
        private readonly int _mainRequestId;
        private ApplicationDbContext _context;

        public TechnologistCreateToolRequestWindow(int mainRequestId)
        {
            _mainRequestId = mainRequestId;
            _context = new ApplicationDbContext();

            InitializeComponent();
            this.Closed += (s, e) => _context?.Dispose();

            LoadRequestInfo();
        }

        private void LoadRequestInfo()
        {
            var req = _context.Requests.Find(_mainRequestId);
            if (req != null)
            {
                LblForRequest.Text = $"{req.RequestNumber} — {req.DetailName}";
            }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtToolName.Text))
            {
                MessageBox.Show("Укажите название инструмента", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtToolName.Focus();
                return;
            }

            try
            {
                var toolRequest = new ToolRequest
                {
                    MainRequestId = _mainRequestId,
                    ToolName = TxtToolName.Text.Trim(),
                    Status = "Создана",
                    RequestedById = Session.CurrentUser.Id,
                    Comment = TxtComment.Text.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.ToolRequests.Add(toolRequest);
                _context.SaveChanges();

                MessageBox.Show("✅ Заявка на инструмент создана", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
