using Microsoft.Toolkit.Forms.UI.Controls;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Microsoft.AspNetCore.Components.Desktop
{
    internal partial class RootForm : Form
    {
        public RootForm()
        {
            InitializeComponent();
        }

        public IPC CreateChannel(string hostHtmlPath)
        {
            var webView = CreateWindow(hostHtmlPath);
            var ipc = new IPC(webView);
            return ipc;
        }

        private WebView CreateWindow(string hostHtmlPath)
        {
            var wvc = new WebView();
            ((ISupportInitialize)wvc).BeginInit();
            wvc.Dock = DockStyle.Fill;
            Controls.Add(wvc);
            ((ISupportInitialize)wvc).EndInit();
            wvc.IsScriptNotifyAllowed = true;
            wvc.NavigateToLocalStreamUri(new Uri(hostHtmlPath, UriKind.Relative), new ContentRootResolver());
            return wvc;
        }
    }
}
