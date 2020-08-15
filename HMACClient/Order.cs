using System;
namespace HMACClient
{
    public class Order
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string ContactNumber { get; set; }
        public Boolean IsShipped { get; set; }
    }
}