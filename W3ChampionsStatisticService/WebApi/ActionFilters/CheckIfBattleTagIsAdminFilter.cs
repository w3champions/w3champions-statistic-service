using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using W3ChampionsStatisticService.Ports;
using W3ChampionsStatisticService.WebApi.ExceptionFilters;

namespace W3ChampionsStatisticService.WebApi.ActionFilters
{
    public class CheckIfBattleTagIsAdminFilter : IAsyncActionFilter {
        private readonly IBlizzardAuthenticationService _blizzardAuthenticationService;

        public CheckIfBattleTagIsAdminFilter(IBlizzardAuthenticationService blizzardAuthenticationService)
        {
            _blizzardAuthenticationService = blizzardAuthenticationService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var queryString = HttpUtility.ParseQueryString(context.HttpContext.Request.QueryString.Value);
            if (queryString.AllKeys.Contains("authorization"))
            {
                var auth = queryString["authorization"];
                var res = await _blizzardAuthenticationService.GetUser(auth);
                if (
                    res != null
                    && !string.IsNullOrEmpty(res.battletag)
                    && Constants.ApprovedAdmins.Any(x => x.ToLower() == res.battletag.ToLower()))
                {
                    context.ActionArguments["battleTag"] = res.battletag;
                    await next.Invoke();
                }
            }

            var unauthorizedResult = new UnauthorizedObjectResult(new ErrorResult("Sorry H4ckerb0i"));
            context.Result = unauthorizedResult;
        }
    }
}