using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace OccupancyService.Controllers
{
    public class TestController : ApiController
    {
        /// <summary>
        /// Simple test, should return "OK"
        /// </summary>
        /// <remarks>
        /// Simple test, should return "OK"
        /// </remarks>
        [HttpGet]
        public HttpResponseMessage Test()
        {
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") };
        }
    }
}