using System.Windows;
using System.Windows.Controls;

namespace DetailTrack.Helpers;

public static class RequestArchiveUi
{
    public static void SetReadOnly(params UIElement[] elements)
    {
        foreach (var element in elements)
        {
            switch (element)
            {
                case Control control:
                    control.IsEnabled = false;
                    control.Opacity = 0.6;
                    break;
                case Panel panel:
                    panel.IsEnabled = false;
                    break;
            }
        }
    }

    public static void HideActions(params UIElement[] elements)
    {
        foreach (var element in elements)
            element.Visibility = Visibility.Collapsed;
    }
}
