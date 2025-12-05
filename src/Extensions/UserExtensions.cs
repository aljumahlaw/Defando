using Defando.Models;

namespace Defando.Extensions
{
    public static class UserExtensions
    {
        public static bool HasPermission(this User user, string permission)
        {
            if (user?.Role == null) return false;

            return user.Role switch
            {
                "admin" => true,
                "manager" => true,
                _ => false
            };
        }
    }
}
