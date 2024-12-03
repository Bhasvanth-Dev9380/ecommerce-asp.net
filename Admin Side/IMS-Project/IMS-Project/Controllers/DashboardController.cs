using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IMS_Project.Models;
using System.Data.Entity; // Use DbFunctions for date truncation

namespace IMS_Project.Controllers
{
    public class DashboardController : Controller
    {
        KahreedoEntities db = new KahreedoEntities();

        public ActionResult Index()
        {
            ViewBag.latestOrders = db.Orders.OrderByDescending(x => x.OrderID).Take(10).ToList();
            ViewBag.NewOrders = db.Orders.Where(a => a.DIspatched == false && a.Shipped == false && a.Deliver == false).Count();
            ViewBag.DispatchedOrders = db.Orders.Where(a => a.DIspatched == true && a.Shipped == false && a.Deliver == false).Count();
            ViewBag.ShippedOrders = db.Orders.Where(a => a.DIspatched == true && a.Shipped == true && a.Deliver == false).Count();
            ViewBag.DeliveredOrders = db.Orders.Where(a => a.DIspatched == true && a.Shipped == true && a.Deliver == true).Count();

            return View();
        }

        // Area Graph
        public JsonResult GetSalesPerDay()
        {
            var data = (from O in db.Orders
                        where O.OrderDate != null // Ensure OrderDate is not null
                        select new { date = DbFunctions.TruncateTime(O.OrderDate), O.TotalAmount } into a
                        group a by a.date into b
                        select new
                        {
                            period = b.Key,
                            sales = b.Sum(x => x.TotalAmount ?? 0) // Handle null TotalAmount
                        });

            List<AreaCharts> aa = new List<AreaCharts>();
            foreach (var item in data)
            {
                string date = item.period?.ToString("yyyy-MM-dd") ?? "Unknown"; // Safely handle null dates
                aa.Add(new AreaCharts() { period = date, sales = item.sales });
            }
            return Json(aa, JsonRequestBehavior.AllowGet);
        }

        // Circle Graph
        public JsonResult GetTopProductSales()
        {
            var dataforchart = (from OD in db.OrderDetails
                                join P in db.Products
                                on OD.ProductID equals P.ProductID
                                select new { P.Name, OD.Quantity } into row
                                group row by row.Name into g
                                select new
                                {
                                    label = g.Key,
                                    value = g.Sum(x => x.Quantity ?? 0) // Handle null Quantity
                                })
                              .OrderByDescending(x => x.value)
                              .Take(3);

            return Json(dataforchart, JsonRequestBehavior.AllowGet);
        }

        // Line Graph
        public JsonResult GetOrderPerDay()
        {
            var data = from O in db.Orders
                       where O.OrderDate != null // Ensure OrderDate is not null
                       select new { Odate = DbFunctions.TruncateTime(O.OrderDate), O.OrderID } into g
                       group g by g.Odate into col
                       select new
                       {
                           Order_Date = col.Key,
                           Count = col.Count(y => y.OrderID != null)
                       };

            List<LineCharts> aa = new List<LineCharts>();
            foreach (var item in data)
            {
                string date = item.Order_Date?.ToString("yyyy-MM-dd") ?? "Unknown"; // Safely handle null dates
                aa.Add(new LineCharts() { Date = date, Orders = item.Count });
            }
            return Json(aa, JsonRequestBehavior.AllowGet);
        }

        // Bar Graph
        public JsonResult GetSalesPerCountry()
        {
            var dataforBarchart = (from O in db.Orders
                                   join C in db.Customers
                                   on O.CustomerID equals C.CustomerID
                                   where C.Country != null && O.TotalAmount != null // Ensure no null values
                                   select new { C.Country, O.TotalAmount } into row
                                   group row by row.Country into g
                                   select new
                                   {
                                       Country = g.Key,
                                       Sales_Amount = g.Sum(x => x.TotalAmount ?? 0) // Handle null TotalAmount
                                   })
                              .OrderByDescending(x => x.Sales_Amount)
                              .Take(6);

            return Json(dataforBarchart, JsonRequestBehavior.AllowGet);
        }
    }

    // Define the AreaCharts class if it's not already defined elsewhere
    public class AreaCharts
    {
        public string period { get; set; }
        public decimal sales { get; set; }
    }

    // Define the LineCharts class if it's not already defined elsewhere
    public class LineCharts
    {
        public string Date { get; set; }
        public int Orders { get; set; }
    }
}
