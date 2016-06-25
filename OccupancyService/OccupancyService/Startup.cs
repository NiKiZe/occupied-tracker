using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using Owin;
using Swashbuckle.Application;

[assembly: OwinStartup(typeof(OccupancyService.Startup))]

namespace OccupancyService
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            // Set up Swagger
            config
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "OccupancyService");
                    c.IncludeXmlComments(GetXmlDocumentationPath());
                })
                .EnableSwaggerUi();

            // Set up Occupancy SignalR-hub
            app.MapSignalR();

            // Set up WebAPI 
            config.MapHttpAttributeRoutes(new CustomDirectRouteProvider());
            config.Routes.MapHttpRoute("DefaultApi", "{controller}/{id}", new { id = RouteParameter.Optional }
                );
            app.UseWebApi(config);
        }

        private string GetXmlDocumentationPath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var commentsFileName = "OccupancyService.XML";
            var commentsFile = Path.Combine(baseDirectory, "Documentation", commentsFileName);
            return commentsFile;
        }
    }
}
