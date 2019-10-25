using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.AspNetCore.Components.Desktop
{
    public static class ComponentsDesktop
    {
        internal static string InitialUriAbsolute { get; private set; }
        internal static string BaseUriAbsolute { get; private set; }
        internal static DesktopJSRuntime DesktopJSRuntime { get; private set; }
        internal static DesktopRenderer DesktopRenderer { get; private set; }

        public static void Run<TStartup>(string hostHtmlPath, Action<Form> configure = null)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new RootForm();
            configure?.Invoke(form);

            DesktopSynchronizationContext.UnhandledException += (sender, exception) =>
            {
                UnhandledException(exception);
            };

            Task.Factory.StartNew(async() =>
            {
                try
                {
                    var ipc = form.CreateChannel(hostHtmlPath);
                    await RunAsync<TStartup>(ipc);
                }
                catch (Exception ex)
                {
                    UnhandledException(ex);
                    throw;
                }
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            Application.Run(form);
        }

        private static void UnhandledException(Exception ex)
        {
            MessageBox.Show($"{ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static async Task RunAsync<TStartup>(IPC ipc)
        {
            DesktopJSRuntime = new DesktopJSRuntime(ipc);
            await PerformHandshakeAsync(ipc);
            AttachJsInterop(ipc);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(configure => configure.AddConsole());
            serviceCollection.AddSingleton<NavigationManager>(DesktopNavigationManager.Instance);
            serviceCollection.AddSingleton<IJSRuntime>(DesktopJSRuntime);
            serviceCollection.AddSingleton<INavigationInterception, DesktopNavigationInterception>();

            var startup = new ConventionBasedStartup(Activator.CreateInstance(typeof(TStartup)));
            startup.ConfigureServices(serviceCollection);

            var services = serviceCollection.BuildServiceProvider();
            var builder = new DesktopApplicationBuilder(services);
            startup.Configure(builder, services);

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();

            DesktopRenderer = new DesktopRenderer(services, ipc, loggerFactory);
            DesktopRenderer.UnhandledException += (sender, exception) =>
            {
                Console.Error.WriteLine(exception);
            };

            foreach (var rootComponent in builder.Entries)
            {
                _ = DesktopRenderer.AddComponentAsync(rootComponent.componentType, rootComponent.domElementSelector);
            }
        }

        private static async Task PerformHandshakeAsync(IPC ipc)
        {
            // Do a two-way handshake with the browser, this ensures that the appropriate
            // interop handlers have been set up before control returns to the user.
            //
            // The handshake sequence looks like this:
            //
            // 1. dotnet starts listening for components:init
            // 2. dotnet sends components:init repeatedly
            // 3. JS starts listening for components:init
            // 4. JS sends a components:init once it has received one from dotnet - it's ready
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

        private static void AttachJsInterop(IPC ipc)
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
                            assemblyName: ((JsonElement)argsArray[1]).GetString(),
                            methodIdentifier: ((JsonElement)argsArray[2]).GetString(),
                            dotNetObjectId: ((JsonElement)argsArray[3]).GetInt64(),
                            callId: ((JsonElement)argsArray[0]).GetString()),
                        ((JsonElement)argsArray[4]).GetString());
                }, args);
            });

            ipc.On("EndInvokeJSFromDotNet", args =>
            {
                desktopSynchronizationContext.Send(state =>
                {
                    var argsArray = (object[])state;
                    DotNetDispatcher.EndInvokeJS(
                        DesktopJSRuntime,
                        ((JsonElement)argsArray[2]).GetString());
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
