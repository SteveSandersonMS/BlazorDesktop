using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.AspNetCore.Components.Desktop
{
    public static class ComponentsDesktop
    {
        public static void Run<TStartup>(string hostHtmlPath, Action<Form> configure = null)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new RootForm();
            configure?.Invoke(form);

            Task.Factory.StartNew(async() =>
            {
                await form.RunAsync(hostHtmlPath);
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            Application.Run(form);
        }
    }
}
