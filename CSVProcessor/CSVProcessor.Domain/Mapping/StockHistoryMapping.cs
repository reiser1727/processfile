using CSVProcessor.Domain;
using TinyCsvParser.Mapping;

namespace CSVProcessor
{
    public  class StockHistoryMapping : CsvMapping<StockHistory>
    {

        public StockHistoryMapping() : base()
        {
            MapProperty(0, x => x.PointOfSale);
            MapProperty(1, x => x.Product);
            MapProperty(2, x => x.Date);
            MapProperty(3, x => x.Stock);
        }

    }
}
