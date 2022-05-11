using System;
using System.Net.Http;

namespace SalesAPP.Models
{
    public class SalesAPI
    {
        public HttpClient Initial()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:49649/");
            return client;
        }
    }
}
