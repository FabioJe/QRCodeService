using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using Swashbuckle.AspNetCore.Annotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using static QRCoder.PayloadGenerator;

namespace QRCodeService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class QRCodeController : ControllerBase
    {
        private readonly ILogger<QRCodeController> _logger;
        private readonly IDataInterface dataInterface;

        public QRCodeController(ILogger<QRCodeController> logger, IDataInterface dataInterface)
        {
            _logger = logger;
            this.dataInterface = dataInterface;
        }

        private string? GetPayLoad(string refid, string secret)
        {
            var privateKey = dataInterface.GetPrivateKey(refid);
            if (privateKey is null) return null;
            var data = StringEncoder.Decrypt(secret, privateKey);
            var dataArray = data.Split("###");
            if (dataArray.Length == 2)
            {
                string dateText = dataArray.Last()[..19];
                DateTime dt = DateTime.ParseExact(dateText, "yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                if (dt < DateTime.UtcNow)
                    return null;
            }

            return dataArray.First();
        }

        private (ImageFormat Format, string ContentType) GetContentType(string format)
        {
            return format.ToLower() switch
            {
                "png" => (ImageFormat.Png, "image/png"),
                "jpeg" or "jpg" => (ImageFormat.Jpeg, "image/jpeg"),
                "bmp" => (ImageFormat.Bmp, "image/bmp"),
                "emf" => (ImageFormat.Emf, "image/emf"),
                "gif" => (ImageFormat.Gif, "image/gif"),
                "icon" => (ImageFormat.Icon, "image/x-icon"),
                "tiff" or "tif" => (ImageFormat.Tiff, "image/" + format),
                _ => (ImageFormat.Png, "image/png"),
            };
        }

        private QRCodeGenerator.ECCLevel GetECCLevel(string ecclevel)
        {
            return ecclevel.ToLower() switch
            {
                "q" => QRCodeGenerator.ECCLevel.Q,
                "h" => QRCodeGenerator.ECCLevel.H,
                "m" => QRCodeGenerator.ECCLevel.M,
                "l" => QRCodeGenerator.ECCLevel.L,
                _ => QRCodeGenerator.ECCLevel.Q
            };
        }

        [HttpGet("/QRImage/{refid}")]
        [SwaggerResponse((int)HttpStatusCode.OK, "The Image")]
        [SwaggerResponse((int)HttpStatusCode.NotFound, "QR Data Not found!")]
        public ActionResult GetQrCode([FromRoute] string refid, [FromQuery] string secret, [FromQuery] int pixels = 10, [FromQuery] string format = "png", [FromQuery] string? ecclevel = "Q", [FromQuery] bool withborder = false)
        {
            try
            {
                if (ecclevel is null)
                    ecclevel = "Q";
                var payLoad = GetPayLoad(refid, secret);
                if (payLoad is null) return NotFound();
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(payLoad, GetECCLevel(ecclevel));
                if (format.Equals("svg", StringComparison.InvariantCultureIgnoreCase))
                {
                    var svgQRCode = new SvgQRCode(qrCodeData);
                    var svgContent = svgQRCode.GetGraphic(pixels, Color.Black, Color.White, withborder);
                    return Content(svgContent, "image/svg+xml; charset=utf-8");
                }
                else
                {
                    var (Format, ContentType) = GetContentType(format);
                    var qrCode = new QRCode(qrCodeData);
                    var barcode = qrCode.GetGraphic(pixels, Color.Black, Color.White, withborder);
                    var outputStream = new MemoryStream();
                    barcode.Save(outputStream, Format);
                    outputStream.Seek(0, SeekOrigin.Begin);
                    return File(outputStream, ContentType);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.Message);
                return this.NotFound();
            }

        }


        [HttpPost("CreateOneTimeText")]
        [SwaggerResponse((int)HttpStatusCode.OK, "New APi Key Set", typeof(string))]
        [SwaggerResponse((int)HttpStatusCode.Forbidden, "API Key Error", typeof(string))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "Server Error", typeof(string))]
        public ActionResult GetOneTimeUrlText(string apiKey, string secret, string issuer, string label, int length = 6, string? algorithm = "SHA1", int period = 30, int hours = 8)
        {
            DateTime validUntil = DateTime.UtcNow.AddHours(hours);
            try
            {
                var publicKey = dataInterface.GetPublicKey(apiKey);
                if (publicKey is null)
                    return Forbid();
                if (string.IsNullOrEmpty(algorithm))
                    algorithm = "SHA1";
                OneTimePassword generator = new OneTimePassword()
                {
                    Secret = secret,
                    Issuer = issuer,
                    Label = label,
                    Type = OneTimePassword.OneTimePasswordAuthType.TOTP,
                    AuthAlgorithm = Enum.Parse<OneTimePassword.OneTimePasswordAuthAlgorithm>(algorithm),
                    Digits = length,
                    Period = period
                };
                string outText = generator.ToString() + "###" + validUntil.ToString("yyyy-MM-dd-HH-mm-ss");
                _logger.LogDebug(outText);
                string encryprtText = StringEncoder.Encrypt(outText, publicKey);
                _logger.LogDebug(encryprtText);
                return Ok(encryprtText);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.Message);
                return this.BadRequest(e.ToString());
            }
        }
        [HttpPost("CreateText")]
        public ActionResult GetText(string apiKey, string text, int hours = 86400)
        {
            DateTime validUntil = DateTime.UtcNow.AddHours(hours);
            try
            {
                var publicKey = dataInterface.GetPublicKey(apiKey);
                if (publicKey is null)
                    return Forbid();
                string outText = text + "###" + validUntil.ToString("yyyy-MM-dd-HH-mm-ss");
                _logger.LogDebug(outText);
                string encryprtText = StringEncoder.Encrypt(outText, publicKey);
                _logger.LogDebug(encryprtText);
                return Ok(encryprtText);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.Message);
                return this.BadRequest(e.ToString());
            }
        }
    }
}
