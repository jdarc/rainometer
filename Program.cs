using System.Windows.Forms;

namespace Rainometer
{
    internal static class Program
    {
        private static void Main()
        {
            var notifyIcon = new NotifyIcon();
            notifyIcon.Icon = null;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenu = new ContextMenu(new[] {new MenuItem("Exit", (_, _) => Application.Exit())});

            async void Refresh(object _) => await Weather.Check(notifyIcon);
            var timer = new System.Threading.Timer(Refresh, null, 0, 60000);

            Application.ApplicationExit += (_, _) =>
            {
                timer.Dispose();
                notifyIcon.Dispose();
            };

            Application.Run();
        }
    }
}
