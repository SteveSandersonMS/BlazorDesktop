using Microsoft.JSInterop.Infrastructure;
using Microsoft.Toolkit.Forms.UI.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.AspNetCore.Components.Desktop
{
    internal partial class RootForm : Form
    {
        internal static string InitialUriAbsolute { get; private set; }
        internal static string BaseUriAbsolute { get; private set; }
        internal static DesktopJSRuntime DesktopJSRuntime { get; private set; }

        public RootForm()
        {
            InitializeComponent();
        }

        public async Task RunAsync(string hostHtmlPath)
        {
            var webView = CreateWindow(hostHtmlPath);
            var ipc = new IPC(webView);
            DesktopJSRuntime = new DesktopJSRuntime(ipc);
            await PerformHandshakeAsync(ipc);
            AttachJsInterop(ipc);
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

        private async Task PerformHandshakeAsync(IPC ipc)
        {
            // Do a two-way handshake with the browser, this ensures that the appropriate
            // interop handlers have been set up before control returns to the user.
            //
            // The handshake sequence looks like this:
            //
            // 1. dotnet starts listening for components:init
            // 2. dotnet sends components:init repeatedly
            // 3. electron starts listening for components:init
            // 4. electron sends a components:init once it has received one from dotnet - it's ready
            // 5. dotnet receives components:init - it's ready
            //
            // Because either side might take any amount of time to start listening,
            // step 3 can occur at any point prior to step 4. The whole process works
            // because steps 1, 2, 4, and 5 can only occur in that order

            var cts = new CancellationTokenSource();
            var incomingHandshakeCancellationToken = cts.Token;
            ipc.Once("components:init", args =>
            {
                var argsArray = (object[])args;
                InitialUriAbsolute = ((JsonElement)argsArray[0]).GetString();
                BaseUriAbsolute = ((JsonElement)argsArray[1]).GetString();
                cts.Cancel();
            });

            Log("Waiting for interop connection");
            while (!incomingHandshakeCancellationToken.IsCancellationRequested)
            {
                ipc.Send("components:init");

                try
                {
                    await Task.Delay(100, incomingHandshakeCancellationToken);
                }
                catch (TaskCanceledException)
                {
                }
            }

            Log("Interop connected");
        }

        private void AttachJsInterop(IPC ipc)
        {
            var desktopSynchronizationContext = new DesktopSynchronizationContext();

            SynchronizationContext.SetSynchronizationContext(desktopSynchronizationContext);

            ipc.On("BeginInvokeDotNetFromJS", args =>
            {
                desktopSynchronizationContext.Send(state =>
                {
                    var argsArray = (object[])state;
                    DotNetDispatcher.BeginInvokeDotNet(
                        DesktopJSRuntime,
                        new DotNetInvocationInfo(
                            assemblyName: (string)argsArray[1],
                            methodIdentifier: (string)argsArray[2],
                            dotNetObjectId: (long)argsArray[3],
                            callId: (string)argsArray[0]),
                        (string)argsArray[4]);
                }, args);
            });

            ipc.On("EndInvokeJSFromDotNet", args =>
            {
                desktopSynchronizationContext.Send(state =>
                {
                    var argsArray = (object[])state;
                    DotNetDispatcher.EndInvokeJS(
                        DesktopJSRuntime,
                        (string)argsArray[2]);
                }, args);
            });
        }

        private static void Log(string message)
        {
            var process = Process.GetCurrentProcess();
            Console.WriteLine($"[{process.ProcessName}:{process.Id}] out: " + message);
        }
    }
}
