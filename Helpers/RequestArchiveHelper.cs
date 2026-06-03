using DetailTrack.Models;
using System.Windows;

namespace DetailTrack.Helpers;

public static class RequestArchiveHelper
{
    public static bool TryArchiveRequest(ApplicationDbContext context1, Request request, Window owner)
    {
        if (request.IsRemoved)
            return false;

        var message = $"Переместить заявку {request.RequestNumber} ({request.DetailName}) в архив?\n" +
                      "Заявка скроется из активного списка. Просмотр будет доступен через «Показать удалённые».";

        if (MessageBox.Show(owner, message, "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return false;

        var context = new ApplicationDbContext(); 

        var entity = context.Requests.Find(request.Id);
        if (entity == null)
            return false;

        entity.IsRemoved = true;
        context.SaveChanges();
        return true;
    }

    public static void ApplyArchiveBanner(Window window, UIElement statusLabel, string statusText)
    {
        if (!window.Title.StartsWith("[Архив]"))
            window.Title = $"[Архив] {window.Title}";

        if (statusLabel is System.Windows.Controls.TextBlock tb)
            tb.Text = $"{statusText} (архив — только просмотр)";
    }
}
