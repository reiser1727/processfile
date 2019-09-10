using System;

namespace CSVProcessor.Domain
{
    public class StockHistory : BaseClass
    {

        public string PointOfSale { get; set; }

        public string Product { get; set; }

        public DateTime? Date { get; set; }

        public int? Stock { get; set; }

        public StockHistory() : base(nameof(StockHistory), new[]
                {
                    nameof(StockHistory.PointOfSale),
                    nameof(StockHistory.Product),
                    nameof(StockHistory.Date),
                    nameof(StockHistory.Stock)
                })
        { }
        
    }

}
