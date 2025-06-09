using OrderBookProcessor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.Utils
{
    internal class FileUtils
    {
        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else {
                throw new Exception($"File already exists in {path}");

            }
        }
        public static bool IsUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                     && (uri?.Scheme == BusinessConstant.Http)
                     || (uri?.Scheme == BusinessConstant.Https);
        }
        public static bool IsLocalFile(string path)
        {
            return File.Exists(path);
        }
        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                LogUtils.logInfo($"{path} is deleted");
            }
        }

        public static async Task WriteStringToFileAsync(string text, string filePath, int chunkSize = 32 * 1024)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(text);

            using (MemoryStream memoryStream = new MemoryStream(byteArray))
            using (FileStream fileStream = new FileStream(
                filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                byte[] buffer = new byte[chunkSize];
                int bytesRead;

                while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                }
            }
        }
    }
}
