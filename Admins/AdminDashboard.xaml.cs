using DetailTrack.Helpers;
using DetailTrack.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DetailTrack.Admins
{
    public partial class AdminDashboard : Window
    {
        private ApplicationDbContext _context;

        public AdminDashboard()
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
                MessageBox.Show("Сессия истекла", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                new MainWindow().Show(); Close(); return;
            }
            LblWelcome.Text = $"👨‍💼 {Session.CurrentUser.FullName}";
            LoadUserFilters();
            LoadUsers();
            LoadReferences();
        }

        private void LoadUserFilters()
        {
            CbFilterWorkshop.Items.Clear();
            CbFilterWorkshop.Items.Add(new ComboBoxItem { Content = "Все цеха", Tag = null });
            foreach (var w in _context.Workshops.ActiveOnly().OrderBy(x => x.Name))
                CbFilterWorkshop.Items.Add(new ComboBoxItem { Content = w.Name, Tag = w.Id });
            CbFilterWorkshop.SelectedIndex = 0;

            CbFilterSpecialization.Items.Clear();
            CbFilterSpecialization.Items.Add(new ComboBoxItem { Content = "Все специализации", Tag = null });
            foreach (var s in _context.Specializations.OrderBy(x => x.Name))
                CbFilterSpecialization.Items.Add(new ComboBoxItem { Content = s.Name, Tag = s.Id });
            CbFilterSpecialization.SelectedIndex = 0;
        }

        private static int? GetFilterId(ComboBox comboBox) =>
            (comboBox.SelectedItem as ComboBoxItem)?.Tag as int?;

        private void LoadUsers(string search = null)
        {
            search ??= TxtUserSearch.Text.Trim();

            var q = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Workshop)
                .Include(u => u.Specialization)
                .AsQueryable();

            if (ChkShowInactive.IsChecked != true)
                q = q.Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(u => u.Login.Contains(search) || u.FullName.Contains(search));

            var workshopId = GetFilterId(CbFilterWorkshop);
            if (workshopId.HasValue)
                q = q.Where(u => u.WorkshopId == workshopId.Value);

            var specializationId = GetFilterId(CbFilterSpecialization);
            if (specializationId.HasValue)
                q = q.Where(u => u.SpecializationId == specializationId.Value);

            DgUsers.ItemsSource = q.OrderBy(u => u.Login).ToList();
        }

        private void ChkShowInactive_Changed(object sender, RoutedEventArgs e) => LoadUsers();
        private void CbUserFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadUsers();
        private void BtnSearchUsers_Click(object sender, RoutedEventArgs e) => LoadUsers();
        private void TxtUserSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) LoadUsers();
        }

        private void BtnResetUsers_Click(object sender, RoutedEventArgs e)
        {
            TxtUserSearch.Clear();
            CbFilterWorkshop.SelectedIndex = 0;
            CbFilterSpecialization.SelectedIndex = 0;
            ChkShowInactive.IsChecked = false;
            LoadUsers(string.Empty);
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e) => OpenUserDialog(null);

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            var u = DgUsers.SelectedItem as User;
            if (u == null) { MessageBox.Show("Выберите пользователя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            OpenUserDialog(u);
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var u = DgUsers.SelectedItem as User;
            if (u == null) { MessageBox.Show("Выберите пользователя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (u.Id == Session.CurrentUser.Id) { MessageBox.Show("❌ Нельзя деактивировать самого себя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (MessageBox.Show($"Деактивировать пользователя {u.FullName}?", "Подтверждение",
                              MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                u.IsActive = false;
                _context.SaveChanges();
                LoadUsers();
                MessageBox.Show("✅ Пользователь деактивирован", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenUserDialog(User user)
        {
            var win = new Window
            {
                Title = user == null ? "Новый пользователь" : "Редактирование",
                Width = 480,
                Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };

            var grid = new Grid { Margin = new Thickness(24) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Form
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            var lblTitle = new TextBlock
            {
                Text = user == null ? "➕ Новый пользователь" : "✏️ Редактирование",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(lblTitle, 0);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel();
            Grid.SetRow(scroll, 1);
            scroll.Content = panel;

            void AddField(string label, Control control, bool required = false)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"{label}{(required ? " *" : "")}",
                    Style = (Style)FindResource("SectionTitle")
                });
                panel.Children.Add(control);
                panel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 16), Background = Brushes.Transparent });
            }

            var txtLogin = new TextBox { Style = (Style)FindResource("ModernTextBox") };
            var isNewUser = user == null;
            var txtPass = new PasswordBox
            {
                Style = (Style)FindResource("ModernPasswordBox"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var txtName = new TextBox { Style = (Style)FindResource("ModernTextBox") };
            var cbRole = new ComboBox { Style = (Style)FindResource("ModernComboBox"), DisplayMemberPath = "Name", SelectedValuePath = "Id" };
            var cbWorkshop = new ComboBox { Style = (Style)FindResource("ModernComboBox"), DisplayMemberPath = "Name", SelectedValuePath = "Id" };
            var cbSpec = new ComboBox { Style = (Style)FindResource("ModernComboBox"), DisplayMemberPath = "Name", SelectedValuePath = "Id" };
            var chkActive = new CheckBox { Content = "Активен", Margin = new Thickness(0, 4, 0, 12), IsChecked = true };

            cbRole.ItemsSource = _context.Roles.OrderBy(r => r.Name).ToList();
            cbWorkshop.ItemsSource = _context.Workshops.ActiveOnly().OrderBy(w => w.Name).ToList();
            cbSpec.ItemsSource = _context.Specializations.OrderBy(s => s.Name).ToList();

            AddField("Логин", txtLogin, true);
            AddField(isNewUser ? "Пароль" : "Новый пароль", txtPass, isNewUser);
            if (!isNewUser)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Оставьте пустым, чтобы не менять пароль. Минимум 6 символов.",
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 12)
                });
                panel.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 16), Background = Brushes.Transparent });
            }
            AddField("ФИО", txtName, true);
            AddField("Роль", cbRole, true);
            AddField("Цех", cbWorkshop);
            AddField("Специализация", cbSpec);
            panel.Children.Add(chkActive);

            if (user != null)
            {
                txtLogin.Text = user.Login; txtLogin.IsEnabled = false;
                txtName.Text = user.FullName;
                chkActive.IsChecked = user.IsActive;
                if (user.RoleId > 0) cbRole.SelectedValue = user.RoleId;
                cbWorkshop.SelectedValue = user.WorkshopId;
                cbSpec.SelectedValue = user.SpecializationId;
            }

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnCancel = new Button
            {
                Content = "Отмена",
                Width = 100,
                Margin = new Thickness(0, 0, 12, 0),
                Style = (Style)FindResource("PrimaryButton")
            };
            var btnSave = new Button { Content = "💾 Сохранить", Width = 120, Style = (Style)FindResource("SuccessButton") };
            btnPanel.Children.Add(btnCancel); btnPanel.Children.Add(btnSave);
            Grid.SetRow(btnPanel, 2);

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtName.Text))
                { MessageBox.Show("Заполните обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                var password = txtPass.Password;
                if (isNewUser && string.IsNullOrWhiteSpace(password))
                { MessageBox.Show("Укажите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!string.IsNullOrEmpty(password) && password.Length < 6)
                { MessageBox.Show("Пароль должен содержать не менее 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (_context.Users.Any(u => u.Login.Trim().ToLower() == txtLogin.Text.Trim().ToLower() && (user == null || u.Id != user.Id)))
                { MessageBox.Show("Логин уже занят", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                try
                {
                    if (isNewUser)
                    {
                        _context.Users.Add(new User
                        {
                            Login = txtLogin.Text.Trim(),
                            PasswordHash = password,
                            FullName = txtName.Text.Trim(),
                            RoleId = (int)cbRole.SelectedValue,
                            WorkshopId = (int)cbWorkshop.SelectedValue,
                            SpecializationId = (int)cbSpec.SelectedValue,
                            IsActive = chkActive.IsChecked ?? true,
                            CreatedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        user.FullName = txtName.Text.Trim();
                        user.RoleId = (int)cbRole.SelectedValue;
                        user.WorkshopId = (int)cbWorkshop.SelectedValue;
                        user.SpecializationId = (int)cbSpec.SelectedValue;
                        user.IsActive = chkActive.IsChecked ?? true;
                        if (!string.IsNullOrEmpty(password))
                            user.PasswordHash = password;
                    }
                    _context.SaveChanges();
                    win.DialogResult = true;
                    LoadUsers();
                }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            btnCancel.Click += (s, e) => win.Close();

            grid.Children.Add(lblTitle); grid.Children.Add(scroll); grid.Children.Add(btnPanel);
            win.Content = grid;
            win.ShowDialog();
        }

        private void LoadReferences()
        {
            LoadWorkshops();
            LoadMachineTypes();
            LoadMachineModels();
            DgSpecializations.ItemsSource = _context.Specializations.OrderBy(x => x.Name).ToList();
        }

        private void LoadWorkshops()
        {
            var showRemoved = ChkShowRemovedWorkshops.IsChecked == true;
            var q = _context.Workshops.Where(w => w.IsRemoved == showRemoved);
            DgWorkshops.ItemsSource = q.OrderBy(x => x.Name).ToList();
            SetReferenceToolbarState(showRemoved, BtnAddWorkshop, BtnEditWorkshop, BtnDeleteWorkshop, BtnRestoreWorkshop);
        }

        private void LoadMachineTypes()
        {
            var showRemoved = ChkShowRemovedMachineTypes.IsChecked == true;
            var q = _context.MachineTypes.Where(t => t.IsRemoved == showRemoved);
            DgMachineTypes.ItemsSource = q.OrderBy(x => x.Name).ToList();
            SetReferenceToolbarState(showRemoved, BtnAddMachineType, BtnEditMachineType, BtnDeleteMachineType, BtnRestoreMachineType);
        }

        private void LoadMachineModels(string search = null)
        {
            var showRemoved = ChkShowRemovedMachineModels.IsChecked == true;
            var q = _context.MachineModels
                .Include(x => x.MachineType)
                .Where(m => m.IsRemoved == showRemoved);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                q = q.Where(m => m.Name.Contains(term) || m.MachineType.Name.Contains(term));
            }

            DgMachineModels.ItemsSource = q.OrderBy(x => x.Name).ToList();
            SetReferenceToolbarState(showRemoved, BtnAddMachineModel, BtnEditMachineModel, BtnDeleteMachineModel, BtnRestoreMachineModel);
        }

        private static void SetReferenceToolbarState(bool showRemoved, Button btnAdd, Button btnEdit, Button btnDelete, Button btnRestore)
        {
            btnAdd.Visibility = showRemoved ? Visibility.Collapsed : Visibility.Visible;
            btnEdit.Visibility = showRemoved ? Visibility.Collapsed : Visibility.Visible;
            btnDelete.Visibility = showRemoved ? Visibility.Collapsed : Visibility.Visible;
            btnRestore.Visibility = showRemoved ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ChkShowRemovedWorkshops_Changed(object sender, RoutedEventArgs e) => LoadWorkshops();
        private void ChkShowRemovedMachineTypes_Changed(object sender, RoutedEventArgs e) => LoadMachineTypes();
        private void ChkShowRemovedMachineModels_Changed(object sender, RoutedEventArgs e) => LoadMachineModels(TxtMachineModelSearch?.Text?.Trim());
        private void BtnSearchMachineModels_Click(object sender, RoutedEventArgs e) => LoadMachineModels(TxtMachineModelSearch.Text.Trim());
        private void TxtMachineModelSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) LoadMachineModels(TxtMachineModelSearch.Text.Trim());
        }
        private void BtnResetMachineModels_Click(object sender, RoutedEventArgs e) { TxtMachineModelSearch.Clear(); LoadMachineModels(); }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) => OpenReferenceDialog((sender as Button)?.Tag?.ToString(), null);

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString();
            int? id = GetSelectedId(tag);
            if (id == null) { MessageBox.Show("Выберите запись", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            OpenReferenceDialog(tag, id);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString();
            int? id = GetSelectedId(tag);
            if (id == null) { MessageBox.Show("Выберите запись", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (MessageBox.Show("Удалить запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    switch (tag)
                    {
                        case "Workshop":
                            var ws = _context.Workshops.Find(id);
                            if (ws != null) ws.IsRemoved = true;
                            break;
                        case "MachineType":
                            var mt = _context.MachineTypes.Find(id);
                            if (mt != null) mt.IsRemoved = true;
                            break;
                        case "MachineModel":
                            var mm = _context.MachineModels.Find(id);
                            if (mm != null) mm.IsRemoved = true;
                            break;
                        case "Specialization":
                            _context.Specializations.Remove(_context.Specializations.Find(id));
                            break;
                    }
                    _context.SaveChanges();
                    LoadReferences();
                    MessageBox.Show("✅ Удалено", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (DbUpdateException)
                { MessageBox.Show("❌ Нельзя удалить: запись используется в других данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString();
            int? id = GetSelectedId(tag);
            if (id == null) { MessageBox.Show("Выберите запись", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (MessageBox.Show("Восстановить запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            switch (tag)
            {
                case "Workshop":
                    var ws = _context.Workshops.Find(id);
                    if (ws != null) ws.IsRemoved = false;
                    break;
                case "MachineType":
                    var mt = _context.MachineTypes.Find(id);
                    if (mt != null) mt.IsRemoved = false;
                    break;
                case "MachineModel":
                    var mm = _context.MachineModels.Find(id);
                    if (mm != null) mm.IsRemoved = false;
                    break;
            }
            _context.SaveChanges();
            LoadReferences();
            MessageBox.Show("✅ Запись восстановлена", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private int? GetSelectedId(string tag) => tag switch
        {
            "Workshop" => (DgWorkshops.SelectedItem as Workshop)?.Id,
            "MachineType" => (DgMachineTypes.SelectedItem as MachineType)?.Id,
            "MachineModel" => (DgMachineModels.SelectedItem as MachineModel)?.Id,
            "Specialization" => (DgSpecializations.SelectedItem as Specialization)?.Id,
            _ => null
        };

        private void OpenReferenceDialog(string tag, int? id)
        {
            bool isMachineModel = tag == "MachineModel";

            var win = new Window
            {
                Title = id == null ? "Добавить запись" : "Изменить запись",
                Width = 420,
                Height = isMachineModel ? 360 : 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };

            var sectionTitle = (Style)FindResource("SectionTitle");
            var panel = new StackPanel { Margin = new Thickness(24) };

            panel.Children.Add(new TextBlock
            {
                Text = id == null ? "➕ Новая запись" : "✏️ Редактирование",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Название *",
                Style = sectionTitle
            });

            var txtName = new TextBox
            {
                Style = (Style)FindResource("ModernTextBox"),
                Margin = new Thickness(0, 0, 0, 12)
            };
            panel.Children.Add(txtName);

            ComboBox? cbType = null;
            if (isMachineModel)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Тип станка *",
                    Style = sectionTitle,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                cbType = new ComboBox
                {
                    Style = (Style)FindResource("ModernComboBox"),
                    DisplayMemberPath = "Name",
                    SelectedValuePath = "Id",
                    Margin = new Thickness(0, 0, 0, 12)
                };
                cbType.ItemsSource = _context.MachineTypes.ActiveOnly().OrderBy(t => t.Name).ToList();
                panel.Children.Add(cbType);
            }

            if (id.HasValue)
            {
                switch (tag)
                {
                    case "Workshop": txtName.Text = _context.Workshops.Find(id)?.Name; break;
                    case "MachineType": txtName.Text = _context.MachineTypes.Find(id)?.Name; break;
                    case "MachineModel":
                        var m = _context.MachineModels.Find(id);
                        txtName.Text = m?.Name;
                        if (cbType != null) cbType.SelectedValue = m?.MachineTypeId;
                        break;
                    case "Specialization": txtName.Text = _context.Specializations.Find(id)?.Name; break;
                }
            }

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var btnCancel = new Button
            {
                Content = "Отмена",
                Width = 100,
                Margin = new Thickness(0, 0, 10, 0),
                Style = (Style)FindResource("PrimaryButton")
            };
            var btnSave = new Button
            {
                Content = "💾 Сохранить",
                Width = 120,
                Style = (Style)FindResource("SuccessButton")
            };

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Введите название", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (isMachineModel && cbType?.SelectedValue == null)
                {
                    MessageBox.Show("Выберите тип станка", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    switch (tag)
                    {
                        case "Workshop":
                            var ws = id.HasValue ? _context.Workshops.Find(id) : new Workshop { IsRemoved = false };
                            ws.Name = txtName.Text.Trim();
                            if (!id.HasValue) _context.Workshops.Add(ws);
                            break;
                        case "MachineType":
                            var mt = id.HasValue ? _context.MachineTypes.Find(id) : new MachineType { IsRemoved = false };
                            mt.Name = txtName.Text.Trim();
                            if (!id.HasValue) _context.MachineTypes.Add(mt);
                            break;
                        case "MachineModel":
                            var mm = id.HasValue ? _context.MachineModels.Find(id) : new MachineModel { IsRemoved = false };
                            mm.Name = txtName.Text.Trim();
                            mm.MachineTypeId = (int)cbType!.SelectedValue;
                            if (!id.HasValue) _context.MachineModels.Add(mm);
                            break;
                        case "Specialization":
                            var sp = id.HasValue ? _context.Specializations.Find(id) : new Specialization();
                            sp.Name = txtName.Text.Trim();
                            if (!id.HasValue) _context.Specializations.Add(sp);
                            break;
                    }
                    _context.SaveChanges();
                    win.DialogResult = true;
                    LoadReferences();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnCancel.Click += (s, e) => win.Close();

            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnSave);
            panel.Children.Add(btnPanel);

            win.Content = panel;
            win.ShowDialog();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Session.Clear(); new MainWindow().Show(); Close();
        }
    }
}