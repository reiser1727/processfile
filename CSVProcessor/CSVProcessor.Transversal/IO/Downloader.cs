using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CSVProcessor.Transversal.IO
{
    public class Downloader
    {

        private DownloaderResult Result { get; set; }

        private string Url { get; }

        private string DestinationFolderPath { get; }

        private int ParallelDownloads { get; }

        private bool ValidateSSL { get; }

        private ConcurrentDictionary<int, String> _tempFilesDictionary;

        public Downloader(string url, string destinationFolderPath, int parallelDownloads = 0, bool validateSSL = false)
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.MaxServicePointIdleTime = 1000;
            Result = new DownloaderResult();
            Url = url;
            DestinationFolderPath = destinationFolderPath;
            ParallelDownloads = parallelDownloads == 0 ? Environment.ProcessorCount : parallelDownloads;
            ValidateSSL = validateSSL;
        }

        public DownloaderResult Download()
        {
            if (!ValidateSSL)
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Result.Size = GetFileSize();
            if (Result.Size > 0)
            {
                Result.FilePath = GetFilePath();

                CleanOldFile(Result.FilePath);

                using (FileStream destinationStream = new FileStream(Result.FilePath, FileMode.Append))
                {
                    _tempFilesDictionary = new ConcurrentDictionary<int, String>();
                    ParallelDownload();
                    MergeTempFiles(destinationStream);
                    GC.Collect();

                }
            }
            return Result;
        }

        /// <summary>
        /// Obtenemos el tamaño del fichero
        /// </summary>
        /// <returns></returns>
        private long GetFileSize()
        {
            WebRequest webRequest = HttpWebRequest.Create(Url);
            webRequest.Method = "HEAD";
            long responseLength;
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                responseLength = long.Parse(webResponse.Headers.Get("Content-Length"));
            }
            return responseLength;
        }

        /// <summary>
        /// Obtenemos el path final del fichero
        /// </summary>
        /// <returns></returns>
        private string GetFilePath()
        {
            Uri uri = new Uri(Url);
            return Path.Combine(DestinationFolderPath, uri.Segments.Last());
        }

        /// <summary>
        /// Eliminamos el fichero posterior en caso de que exista
        /// </summary>
        /// <param name="filePath"></param>
        private void CleanOldFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Obtenemos los diferentes rangos del fichero que va a descargar cada hilo
        /// </summary>
        /// <returns></returns>
        private List<DownloaderRange> GetRanges()
        {
            List<DownloaderRange> ranges = new List<DownloaderRange>();
            for (int chunk = 0; chunk < ParallelDownloads - 1; chunk++)
            {
                var range = new DownloaderRange()
                {
                    Start = chunk * (Result.Size / ParallelDownloads),
                    End = ((chunk + 1) * (Result.Size / ParallelDownloads)) - 1
                };
                ranges.Add(range);
            }

            ranges.Add(new DownloaderRange()
            {
                Start = ranges.Any() ? ranges.Last().End + 1 : 0,
                End = Result.Size - 1
            });

            return ranges;
        }

        /// <summary>
        /// Realizamos la descarga del fichero
        /// </summary>
        private void ParallelDownload()
        {
            DateTime startTime = DateTime.Now;
            int index = 0;
            List<DownloaderRange> ranges = GetRanges();
            Parallel.ForEach(ranges, new ParallelOptions() { MaxDegreeOfParallelism = ParallelDownloads }, readRange =>
            {
                HttpWebRequest httpWebRequest = HttpWebRequest.Create(Url) as HttpWebRequest;
                httpWebRequest.Method = "GET";
                httpWebRequest.AddRange(readRange.Start, readRange.End);
                using (HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse)
                {
                    String tempFilePath = Path.GetTempFileName();
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
                    {
                        httpWebResponse.GetResponseStream().CopyTo(fileStream);
                        _tempFilesDictionary.TryAdd((int)index, tempFilePath);
                    }
                }
                index++;
            });

            Result.ParallelDownloads = index;
            Result.DownloadTime = DateTime.Now.Subtract(startTime);
        }

        /// <summary>
        /// Unimos todos los trozos del fichero en uno sólo y lo guardamos en la carpeta correspondiente
        /// </summary>
        /// <param name="destinationStream"></param>
        private void MergeTempFiles(FileStream destinationStream)
        {
            foreach (var tempFile in _tempFilesDictionary.OrderBy(b => b.Key))
            {
                byte[] tempFileBytes = File.ReadAllBytes(tempFile.Value);
                destinationStream.Write(tempFileBytes, 0, tempFileBytes.Length);
                File.Delete(tempFile.Value);
            }
        }

    }
}
