using Microsoft.AspNetCore.Mvc;
using QRCodeService.Models;
using System.Net;
using Swashbuckle.AspNetCore.Annotations;

namespace QRCodeService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private static Random random = new Random();

        private static string RandomString(int length, bool adv = false)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            if (adv)
                chars += ".-_,;:!";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private readonly ILogger<AdminController> _logger;
        private readonly IDataInterface dataInterface;

        public AdminController(ILogger<AdminController> logger, IDataInterface dataInterface)
        {
            _logger = logger;
            this.dataInterface = dataInterface;
        }

        [HttpPost("CreateNew")]
        [SwaggerResponse((int)HttpStatusCode.OK, "New APi Key Set", typeof(CreateApiKey))]
        [SwaggerResponse((int)HttpStatusCode.Forbidden, "Login Error", typeof(string))]
        public dynamic CreateNewInfo(string apikey)
        {
            if (!dataInterface.CheckAdminKey(apikey))
                return Forbid("API Key is Invaild!");
            var profId = RandomString(5);
            var apiKey = RandomString(70, true);
            var (publicKey, privateKey) = StringEncoder.GetNewKeyPair();
            dataInterface.InsertNewProfile(profId, apiKey, privateKey, publicKey);
            return new CreateApiKey(apiKey, publicKey, profId);
        }
    }
}
