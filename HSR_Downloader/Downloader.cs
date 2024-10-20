using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HSR_DataDownloader
{
    public class Downloader
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly Logger logger;
        private readonly string destinationPath;

        public Downloader(Logger logger, string destinationPath)
        {
            this.logger = logger;
            this.destinationPath = destinationPath;
        }

        public async Task DownloadFilesAsync(string[] urls)
        {
            var tasks = new List<Task>();

            foreach (var url in urls)
            {
                tasks.Add(DownloadFileAsync(url));
                await Task.Delay(100);
            }

            await Task.WhenAll(tasks);
        }

        private async Task DownloadFileAsync(string url)
        {
            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsByteArrayAsync();
                var fileName = GetFileNameFromUrl(url);
                var filePath = Path.Combine(destinationPath, fileName);

                await File.WriteAllBytesAsync(filePath, content);
                logger.LogSuccess($"Downloaded {fileName}", false);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Error downloading {url}: {ex.Message}");
            }
        }

        private string GetFileNameFromUrl(string url)
        {
            return new Uri(url).Segments.Last();
        }
    }
}