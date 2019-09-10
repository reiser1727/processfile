using System;

namespace CSVProcessor.Transversal.IO
{
    public class DownloaderResult
    {

        public long Size { get; set; }
        public String FilePath { get; set; }
        public TimeSpan DownloadTime { get; set; }
        public int ParallelDownloads { get; set; }

    }
}
