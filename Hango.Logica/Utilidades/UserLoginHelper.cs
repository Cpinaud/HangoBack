using System.Security.Claims;

public static class UserLoginHelper
{
    public static int ObtenerIdUsuario(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null)
        {
            return int.Parse(userIdClaim.Value);
        }
        return 0;
    }
}