using CSVProcessor.Domain;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyCsvParser;
using TinyCsvParser.Mapping;

namespace CSVProcessor.Transversal.IO
{
    public class CSVReader<T1, T2>
        where T1 : BaseClass, new()
        where T2 : CsvMapping<T1>, new()
    {

        public string Path { get; }

        public CSVReader(string path)
        {
            this.Path = path;
        }

        /// <summary>
        /// Realiza la lectura del fichero CSV de forma concurrente
        /// </summary>
        /// <returns></returns>
        public ConcurrentBag<T1> Read()
        {
            if (File.Exists(Path))
            {
                //Se emplean todos los hilos del procesador, no necesitamos mantener orden de los datos por lo que la lectura se puede realizar de forma más rápida
                CsvParserOptions csvParserOptions = new CsvParserOptions(true, ';', Environment.ProcessorCount, false);
                var csvParser = new CsvParser<T1>(csvParserOptions, new T2());
                var records = csvParser.ReadFromFile(Path, Encoding.UTF8);
                return ToConcurrentBag(records);
            }
            return new ConcurrentBag<T1>();
        }

        private ConcurrentBag<T1> ToConcurrentBag(ParallelQuery<CsvMappingResult<T1>> records)
        {
            ConcurrentBag<T1> bag = new ConcurrentBag<T1>();

            Parallel.ForEach(records, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, record =>
            {
                //Desechamos los elementos que no se han podido parsear debidos a diversas inconsistencias de datos
                if (record != null && record.Result != null)
                    bag.Add(record.Result);
            });
            GC.Collect();
            return bag;
        }

    }
}
