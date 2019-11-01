using System;
using System.IO;
using System.Reflection;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;

namespace Microsoft.AspNetCore.Components.Desktop
{
    internal class ContentRootResolver : IUriToStreamResolver
    {
        public Stream UriToStream(Uri uri)
        {
            if (uri.Scheme != "ms-local-stream")
            {
                throw new ArgumentException($"{nameof(ContentRootResolver)} should only be used for local stream URIs");
            }

            Console.WriteLine($"Resolving local stream URI {uri}");

            var localPath = uri.LocalPath;
            var frameworkPrefix = "/_framework/";
            if (localPath.StartsWith(frameworkPrefix))
            {
                return SupplyFrameworkFile(uri, localPath.Substring(frameworkPrefix.Length));
            }
            else if (localPath.StartsWith('/'))
            {
                return SupplyContentFile(uri, localPath.Substring(1));
            }
            else
            {
                throw new ArgumentException($"Expected local path to start with '/', but received value '{uri.LocalPath}'");
            }
        }

        private Stream SupplyFrameworkFile(Uri uri, string localPath)
        {
            switch (localPath)
            {
                case "blazor.desktop.js":
                    return typeof(ContentRootResolver).Assembly.GetManifestResourceStream("Microsoft.AspNetCore.Components.Desktop.blazor.desktop.js");
                default:
                    throw new ArgumentException($"Unknown framework file: {uri}");
            }
        }

        private Stream SupplyContentFile(Uri uri, string localPath)
        {
            localPath = localPath.Replace('/', Path.DirectorySeparatorChar);

            var filePath = Path.Combine(ContentRoot.Value, localPath);

            if (File.Exists(filePath))
            {
                return File.OpenRead(filePath);
            }
            else
            {
                throw new FileNotFoundException($"Local stream URI '{uri}' does not correspond to an existing file on disk", filePath);
            }
        }

        private Lazy<string> ContentRoot = new Lazy<string>(() =>
        {
            var startDir = Directory.GetCurrentDirectory();
            var dir = startDir;
            while (!string.IsNullOrEmpty(dir))
            {
                var candidate = Path.Combine(dir, "wwwroot");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                dir = Path.GetDirectoryName(dir);
            }

            throw new DirectoryNotFoundException($"Could not find wwwroot in '{startDir}' or any ancestor directory");
        });
    }
}
