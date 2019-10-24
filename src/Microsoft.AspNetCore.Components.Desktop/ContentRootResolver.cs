using System;
using System.ComponentModel;
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

            var localPath = uri.LocalPath;
            if (!localPath.StartsWith('/'))
            {
                throw new ArgumentException($"Expected local path to start with '/', but received value '{uri.LocalPath}'");
            }
            localPath = localPath.Substring(1).Replace('/', Path.DirectorySeparatorChar);

            var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(root, localPath);

            if (File.Exists(filePath))
            {
                return File.OpenRead(filePath);
            }
            else
            {
                return null; // TODO: Is this how to indicate 404?
            }
        }
    }
}
