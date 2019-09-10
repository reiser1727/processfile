using CSVProcessor.DataAccess;
using CSVProcessor.Domain;
using CSVProcessor.Transversal.IO;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace CSVProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            if (CheckConfiguration(configuration))
            {
                Console.WriteLine("Descargando fichero...");

                var downloader = new Downloader(configuration["FileURL:URL"], configuration["FileURL:Directory"]);
                var result = downloader.Download();

                Console.WriteLine($"Path: {result.FilePath}");
                Console.WriteLine($"Tamaño: {result.Size}bytes");
                Console.WriteLine($"Tiempo de Descarga: {result.DownloadTime.TotalSeconds}s");
                Console.WriteLine($"Hilos en Paralelo: {result.ParallelDownloads}");

                Console.WriteLine("Insertando datos en BBDD...");
                var reader = new CSVReader<StockHistory, StockHistoryMapping>(result.FilePath);
                var bulk = new BulkInsert<StockHistory>();
                bulk.Execute(configuration.GetConnectionString("StockBBDD"), reader.Read());
                Console.WriteLine("Proceso finalizado!");
                Console.ReadLine();
            }

        }

        private static bool CheckConfiguration(IConfigurationRoot configuration)
        {
            bool result = true;

            if (string.IsNullOrEmpty(configuration["FileURL:URL"]))
            {
                Console.WriteLine("Revise configuración de URL del fichero a descargar");
                result = false;
            }

            if (string.IsNullOrEmpty(configuration["FileURL:Directory"]))
            {
                Console.WriteLine("Revise configuración de carpeta destino del fichero a descargar");
                result = false;
            }

            if (string.IsNullOrEmpty(configuration.GetConnectionString("StockBBDD")))
            {
                Console.WriteLine("Revise configuración de base de datos");
                result = false;
            }

            return result;
        }
    }
}
