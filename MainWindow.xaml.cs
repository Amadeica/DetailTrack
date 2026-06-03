using DetailTrack.Admins;
using DetailTrack.Constructors;
using DetailTrack.Helpers;
using DetailTrack.Models;
using DetailTrack.Programmers;
using DetailTrack.SetupTechnicians;
using DetailTrack.Technologists;
using DetailTrack.ToolEngineers;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace DetailTrack
{
    public partial class MainWindow : Window
    {
        private ApplicationDbContext _context; 
        public MainWindow()
        {
            _context = new ApplicationDbContext();  
            InitializeComponent();
            TxtPassword.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) BtnLogin_Click(null, null); };
        }

        private void BtnLogin_Click(object? sender, RoutedEventArgs? e)
        {
            string login = TxtLogin.Text.Trim();
            string password = TxtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                LblError.Text = "Введите логин и пароль";
                return;
            }

            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Login == login && u.IsActive);

            bool passwordOk = user != null && user.PasswordHash == password;

            if (user == null || !passwordOk)
            {
                LblError.Text = "Неверный логин или пароль";
                TxtPassword.Clear();
                return;
            }

            Session.CurrentUser = user;
            OpenDashboardByRole(user.Role.Name);
        }

        private void OpenDashboardByRole(string roleName)
        {
            Window dashboard = roleName switch
            {
                "Администратор" => new AdminDashboard(),
                "Конструктор" => new ConstructorDashboard(),
                "Технолог" => new TechnologistDashboard(),
                "Программист" => new ProgrammerDashboard(),
                "Наладчик" => new SetupTechnicianDashboard(),
                "Инженер по инструменту" => new ToolEngineerDashboard(),
                _ => new MainWindow()
            };

            dashboard.Show();
            Close();
        }
    }
}