using Microsoft.Toolkit.Forms.UI.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.AspNetCore.Components.Desktop
{
    internal partial class RootForm : Form
    {
        public RootForm(string hostHtmlPath)
        {
            InitializeComponent();

            var wvc = new WebView();
            ((ISupportInitialize)wvc).BeginInit();
            wvc.Dock = DockStyle.Fill;
            Controls.Add(wvc);
            ((ISupportInitialize)wvc).EndInit();

            wvc.AddInitializeScript(@"
                function notifyHost(type, data) {
                    window.external.notify(JSON.stringify({ type: type, data: data }));
                }

                window.addEventListener('error', function(event) {
                    notifyHost('error', { message: event.message, filename: event.filename, lineno: event.lineno, colno: event.colno });
                });
            ");
            wvc.ScriptNotify += HandleScriptNotify;

            wvc.IsScriptNotifyAllowed = true;
            wvc.NavigateToLocalStreamUri(new Uri(hostHtmlPath, UriKind.Relative), new ContentRootResolver());
        }

        private void HandleScriptNotify(object sender, WebViewControlScriptNotifyEventArgs e)
        {
            MessageBox.Show(e.Value);
        }
    }
}
