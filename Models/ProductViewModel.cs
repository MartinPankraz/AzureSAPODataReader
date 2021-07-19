using System;

namespace AzureODataReader.Models
{
    public class ProductViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string Price { get; set; }
        public string ImageUrl { get; set; }
    
    }
}
