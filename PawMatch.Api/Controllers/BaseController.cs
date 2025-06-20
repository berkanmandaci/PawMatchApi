using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace PawMatch.Api.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// JWT'den userId'yi çeken yardımcı fonksiyon (tüm controllerlarda kullanılabilir)
        /// </summary>
        protected int? GetUserIdFromClaims()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }
    }
} 