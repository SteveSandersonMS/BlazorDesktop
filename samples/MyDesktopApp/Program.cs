using Microsoft.AspNetCore.Components.Desktop;

namespace MyDesktopApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ComponentsDesktop.Run<Startup>("wwwroot/index.html", form =>
            {
                form.Text = "MyDesktopApp";
            });
        }
    }
}
