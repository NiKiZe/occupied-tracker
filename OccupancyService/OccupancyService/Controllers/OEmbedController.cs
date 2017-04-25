using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Web;
using System.Web.Http;
using System.Xml.Serialization;
using OccupancyService.Models;

namespace OccupancyService.Controllers
{
    /// <summary>
    /// Implementation of a oEmber provider (http://oembed.com/)
    /// </summary>
    public class OEmbedController : ApiController
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">Required. The URL to retrieve embedding information for.</param>
        /// <param name="maxwidth">Optional. The maximum width of the embedded resource. Only applies to some resource types (as specified below). For supported resource types, this parameter must be respected by providers.</param>
        /// <param name="maxheight">Optional. The maximum height of the embedded resource. Only applies to some resource types (as specified below). For supported resource types, this parameter must be respected by providers.</param>
        /// <param name="format">Optional. 'json' or 'xml'. The required response format. When not specified, the provider can return any valid response format. When specified, the provider must return data in the request format, else return an error (see below for error codes).</param>
        /// <returns></returns>
        public HttpResponseMessage Get(string url = null, int? maxwidth = null, int? maxheight = null, string format = null)
        {
            if (format != "json" && format != "xml")
            {
                return new HttpResponseMessage(HttpStatusCode.NotImplemented);
            }

            var oembed = new OEmbed
            {
                Type = "rich",
                Version = "1.0",
                Title = "Toalett-status",
                ProviderName = "Caspeco Occupancy",
                ProviderUrl = "http://caspecooccupancy.azurewebsites.net/",
                Html = @"
<ul>
    <li>Test 1</li>
    <li>Test 2</li>
</ul>",
                Width = 400,
                Height = 300
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            switch (format)
            {
                case "json":
                    var stream = new MemoryStream();
                    var jsonSerializer = new DataContractJsonSerializer(typeof(OEmbed));
                    jsonSerializer.WriteObject(stream, oembed);
                    stream.Position = 0;
                    var sr = new StreamReader(stream);
                    response.Content = new StringContent(sr.ReadToEnd());
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    break;
                case "xml":
                    var xmlSerializerNamespaces = new XmlSerializerNamespaces();
                    xmlSerializerNamespaces.Add(string.Empty, string.Empty);
                    var xmlSerializer = new XmlSerializer(typeof(OEmbed));
                    StringWriter sw = new StringWriter();
                    xmlSerializer.Serialize(sw, oembed, xmlSerializerNamespaces);
                    response.Content = new StringContent(sw.ToString());
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                    break;
            }
            return response;
        }
    }
}