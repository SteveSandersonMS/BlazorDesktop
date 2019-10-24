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
            if (!localPath.StartsWith('/'))
            {
                throw new ArgumentException($"Expected local path to start with '/', but received value '{uri.LocalPath}'");
            }
            localPath = localPath.Substring(1).Replace('/', Path.DirectorySeparatorChar);

            var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(root, "wwwroot", localPath);

            if (File.Exists(filePath))
            {
                return File.OpenRead(filePath);
            }
            else
            {
                throw new FileNotFoundException($"Local stream URI '{uri}' does not correspond to an existing file on disk", filePath);
            }
        }
    }
}
