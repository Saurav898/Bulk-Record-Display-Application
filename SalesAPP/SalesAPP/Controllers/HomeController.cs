using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SalesAPP.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SalesAPP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        SalesAPI _api = new SalesAPI();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(Record record)
        {
            ErrorViewModel errorViewModel = new ErrorViewModel()
            {
                RequestId = "An error occured while inserting values in the Database. Please try again.!"
            };
            try
            {
                HttpClient client = _api.Initial();
                client.Timeout = TimeSpan.FromMinutes(10);
                var insertToDB = await client.PostAsJsonAsync("Sales", new Record());

                if (insertToDB.IsSuccessStatusCode)
                {
                    return RedirectToAction("GetRecords");
                }               
                return View("Error", errorViewModel);
            }
            catch (Exception)
            {
                return View("Error", errorViewModel);
            }
        }

        [HttpGet]
        public IActionResult GetRecords()
        {
            List<Record> records = ViewRecords(1, 0, 0, 0);
            if(records !=  null)
            {
                ViewData["SuccessMsg"] = "Inserting records in chunks of 5000 to Databse has been successful.";
                ViewData["pageNo"] = 1;
                return View("GetRecords", records);
            }
            ErrorViewModel errorViewModel = new ErrorViewModel()
            {
                RequestId = "Error Occured trying to retrieve data. Please try again.!"
            };
            return View("Error", errorViewModel);
        }

        [HttpPost]
        public IActionResult GetRecords(int PageNumber, int ddlOperator, decimal Value1, decimal Value2)
        {
            ErrorViewModel errorViewModel = new ErrorViewModel()
            {
                RequestId = "Error Occured trying to retrieve data. Please try again.!"
            };

            List<Record> records = new List<Record>();

            if (ddlOperator == 0)
            {
                records = ViewRecords(PageNumber, 0, 0, 0);
            }
                
            else if (ddlOperator == 1 || ddlOperator == 2 || ddlOperator == 4)
            {
                records = ViewRecords(PageNumber, ddlOperator, Value1, 0);
            }               
            else
            {
                records = ViewRecords(PageNumber, 3, Value1, Value2);
            }
                
            if (records!=null)
            {
                ViewData["SuccessMsg"] = "";
                ViewData["pageNo"] = PageNumber;
                ViewData["Value1"] = Value1;
                ViewData["Value2"] = Value2;
                return View("GetRecords", records);
            }
            return View("Error", errorViewModel);
        }

        #region HelperMethod
        private List<Record> ViewRecords(int pageNo, int ddlOperator, decimal Value1, decimal Value2)
        {
            try
            {
                List<Record> records = new List<Record>();
                HttpClient client = _api.Initial();
                var getRecords = client.GetAsync($"Sales/{pageNo}/{ddlOperator}/{Value1}/{Value2}");

                getRecords.Wait();
                var resGetRecords = getRecords.Result;
                int totalPages = (int)JObject.Parse(resGetRecords.Headers.GetValues("Paging-Headers").ToList()[0]).GetValue("totalPages");
                ViewData["TotalPages"] = totalPages;
                if (resGetRecords.IsSuccessStatusCode)
                {
                    var results = resGetRecords.Content.ReadAsStringAsync().Result;
                    records = JsonConvert.DeserializeObject<List<Record>>(results);
                }
                return records;
            }
            catch (Exception)
            {
                return null;
            }         
        }
        #endregion
    }
}
