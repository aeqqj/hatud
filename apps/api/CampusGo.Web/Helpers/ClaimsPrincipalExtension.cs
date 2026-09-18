using System.Security.Claims;

namespace CampusGo.Web.Helpers;

public static class ClaimsPrincipalExtensions
{
    /* This is an extension method for User validation otherwise you'd have to find user by 
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); 
        in every controller that uses it
    */
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

}