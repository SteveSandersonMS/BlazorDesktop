using System;
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
            Application.Run(form);
        }
    }
}
