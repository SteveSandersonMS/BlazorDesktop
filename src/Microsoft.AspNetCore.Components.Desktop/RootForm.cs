using Microsoft.Toolkit.Forms.UI.Controls;
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

            wvc.IsScriptNotifyAllowed = true;
            wvc.NavigateToLocalStreamUri(new Uri(hostHtmlPath, UriKind.Relative), new ContentRootResolver());
        }
    }
}
