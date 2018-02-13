using System.Web;
using System.Web.Mvc;

namespace VideoHostingStreamingAWS
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
