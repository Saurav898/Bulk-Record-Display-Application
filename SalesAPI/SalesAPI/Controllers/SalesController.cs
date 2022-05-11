using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using SalesAPI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace SalesAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SalesController : ControllerBase
    {
        SalesContext _context;
        public SalesController()
        {
            _context = new SalesContext();
        }

        [HttpGet("{pageNo}/{op}/{value1}/{value2}")]
        public IEnumerable<Record> Get(int pageNo, int op, decimal value1, decimal value2)
        {
            if (op == 0)
            {
                // Return List of Customer  
                var source = (from record in _context.Records 
                              select record).AsQueryable();
                var items = GetRecords(source, pageNo);
                return items;
            }
            else if (op == 1)
            {
                var source = (from record in _context.Records
                              where record.TotalRevenue > value1
                              select record).AsQueryable();
                var items = GetRecords(source, pageNo);
                return items;
            }
            else if (op == 2)
            {
                var source = (from record in _context.Records
                              where record.TotalRevenue < value1
                              select record).AsQueryable();
                var items = GetRecords(source, pageNo);
                return items;
            }
            else if (op == 3)
            {
                var source = (from record in _context.Records
                              where record.TotalRevenue >= value1 && record.TotalRevenue <= value2
                              select record).AsQueryable();
                var items = GetRecords(source, pageNo);
                return items;
            }
            else
            {
                var source = (from record in _context.Records
                              where record.TotalRevenue != value1
                              select record).AsQueryable();
                var items = GetRecords(source, pageNo);
                return items;
            }
        }

        [HttpPost]
        public async Task<ActionResult<Record>> PostBulkSalesRecord(Record record)
        {
            try
            {
                //Downloading file from URL
                WebClient webClient = new WebClient();
                webClient.DownloadFile(new Uri(""/*URL to be mentioned here*/), "myfile.zip"); 
                //Console.WriteLine("File Downloaded");

                //Unzipping the file
                ZipFile.ExtractToDirectory("myfile.zip", Directory.GetCurrentDirectory(), true);
                //Console.WriteLine("Extract Completed");

                //Getting the file path
                string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csv");

                //creating a temp table similar to one created in DB
                DataTable tblcsv = CreateTable();

                //Creating tasks and dividing the insertion into parts
                string[] readfile = System.IO.File.ReadAllText(files[0]).Split('\n');
                int length = readfile.Length;
                int length1 = 1;
                int length2 = (int)(0.25 * length);
                int length3 = (int)(0.5 * length);
                int length4 = (int)(0.75 * length);

                Task task1 = Task.Factory.StartNew(() => Insert(readfile, length1, length2));
                Task task2 = Task.Factory.StartNew(() => Insert(readfile, length2 + 1, length3));
                Task task3 = Task.Factory.StartNew(() => Insert(readfile, length3 + 1, length4));
                Task task4 = Task.Factory.StartNew(() => Insert(readfile, length4 + 1, length));
                Task.WaitAll(task1, task2, task3, task4);
                return Ok();
            }
            catch (Exception)
            {
                return NoContent();
            }

        }

        #region HelperMethods

        private void Insert(string[] readfile, int length1, int length2)
        {
            DataTable tblcsv = CreateTable();
            while (length1 < length2)
            {
                IEnumerable<string> ReadCSV = readfile.Skip(length1).Take(5000);
                foreach (string csvRow in ReadCSV)
                {
                    if (!string.IsNullOrEmpty(csvRow))
                    {
                        //Adding each row into datatable  
                        tblcsv.Rows.Add();
                        int count = 0;
                        foreach (string FileRec in csvRow.Split(','))
                        {
                            tblcsv.Rows[tblcsv.Rows.Count - 1][count] = FileRec;
                            count++;
                        }
                    }
                }
                length1 += 5000;
                InsertCSVRecords(tblcsv);
                Console.WriteLine("Inserted Records {0}", length1 - 1);
                tblcsv.Clear();
            }
        }
        private void InsertCSVRecords(DataTable csvdt)
        {
            SqlConnection con = new SqlConnection(@"data source = (LocalDb)\MSSQLLocalDB; Initial Catalog = Sales; Integrated Security = SSPI;");
            //creating object of SqlBulkCopy    
            SqlBulkCopy objbulk = new SqlBulkCopy(con);
            //assigning Destination table name    
            objbulk.DestinationTableName = "Records";
            //Mapping Table column    
            objbulk.ColumnMappings.Add("Region", "Region");
            objbulk.ColumnMappings.Add("Country", "Country");
            objbulk.ColumnMappings.Add("ItemType", "ItemType");
            objbulk.ColumnMappings.Add("SalesChannel", "SalesChannel");
            objbulk.ColumnMappings.Add("OrderPriority", "OrderPriority");
            objbulk.ColumnMappings.Add("OrderDate", "OrderDate");
            objbulk.ColumnMappings.Add("OrderID", "OrderID");
            objbulk.ColumnMappings.Add("ShipDate", "ShipDate");
            objbulk.ColumnMappings.Add("UnitsSold", "UnitsSold");
            objbulk.ColumnMappings.Add("UnitPrice", "UnitPrice");
            objbulk.ColumnMappings.Add("UnitCost", "UnitCost");
            objbulk.ColumnMappings.Add("TotalRevenue", "TotalRevenue");
            objbulk.ColumnMappings.Add("TotalCost", "TotalCost");
            objbulk.ColumnMappings.Add("TotalProfit", "TotalProfit");

            //inserting Datatable Records to DataBase    
            con.Open();
            objbulk.WriteToServer(csvdt);
            con.Close();
        }
        private DataTable CreateTable()
        {
            DataTable tblcsv =  new DataTable();

            tblcsv.Columns.Add("Region", typeof(string));
            tblcsv.Columns.Add("Country", typeof(string));
            tblcsv.Columns.Add("ItemType", typeof(string));
            tblcsv.Columns.Add("SalesChannel", typeof(string));
            tblcsv.Columns.Add("OrderPriority", typeof(string));
            tblcsv.Columns.Add("OrderDate", typeof(string));
            tblcsv.Columns.Add("OrderID", typeof(int));
            tblcsv.Columns.Add("ShipDate", typeof(string));
            tblcsv.Columns.Add("UnitsSold", typeof(int));
            tblcsv.Columns.Add("UnitPrice", typeof(decimal));
            tblcsv.Columns.Add("UnitCost", typeof(decimal));
            tblcsv.Columns.Add("TotalRevenue", typeof(decimal));
            tblcsv.Columns.Add("TotalCost", typeof(decimal));
            tblcsv.Columns.Add("TotalProfit", typeof(decimal));

            return tblcsv;
        }
        private List<Record> GetRecords(IQueryable<Record> source, int pageNo)
        {
            try
            {
                int count = source.Count();

                //Seeting the Page Size
                int pagesize = 500;
                PagingParameterModel pagingparametermodel = new PagingParameterModel()
                {
                    pageNumber = pageNo,
                    pageSize = pagesize,
                    _pageSize = pagesize
                };

                // Parameter is passed from Query string if it is null then it default Value will be pageNumber:1  
                int CurrentPage = pagingparametermodel.pageNumber;

                // Parameter is passed from Query string if it is null then it default Value will be pageSize:20  
                int PageSize = pagingparametermodel.pageSize;

                // Display TotalCount to Records to User  
                int TotalCount = count;

                // Calculating Totalpage by Dividing (No of Records / Pagesize)  
                int TotalPages = (int)Math.Ceiling(count / (double)PageSize);

                // Returns List of Customer after applying Paging   
                var items = source.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

                // if CurrentPage is greater than 1 means it has previousPage  
                var previousPage = CurrentPage > 1 ? "Yes" : "No";

                // if TotalPages is greater than CurrentPage means it has nextPage  
                var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

                // Object which we are going to send in header   
                var paginationMetadata = new
                {
                    totalCount = TotalCount,
                    pageSize = PageSize,
                    currentPage = CurrentPage,
                    totalPages = TotalPages,
                    previousPage,
                    nextPage
                };

                // Setting Header  
                HttpContext.Response.Headers.Add("Paging-Headers", JsonConvert.SerializeObject(paginationMetadata));
                // Returing List of Customers Collections  
                return items;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion


    }
}
