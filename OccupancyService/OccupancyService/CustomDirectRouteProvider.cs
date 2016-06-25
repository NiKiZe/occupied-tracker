using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;

namespace OccupancyService
{
    public class CustomDirectRouteProvider : DefaultDirectRouteProvider
    {
        protected override IReadOnlyList<IDirectRouteFactory>
            GetActionRouteFactories(HttpActionDescriptor actionDescriptor)
        {
            // This method override is used to allow the router to find routes from inherited actions
            // use this provider to enable inhertiance for baseController.
            return actionDescriptor.GetCustomAttributes<IDirectRouteFactory>
                (true);
        }
    }
}