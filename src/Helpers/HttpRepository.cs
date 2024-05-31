using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace RvSfDownloadCore.Repository
{
    public class RvHttpRepository
    {
        private readonly ILogger _logger;
        private readonly int _downloadDelay;

        public RvHttpRepository(IConfiguration config, ILogger logger)
        {
            _logger = logger;
            _downloadDelay = Helpers.ParamsGetter.ReadNonEmptyIntConfigParam(config, _logger, "HttpSettings:DownloadDelay");
        }

        /// <summary>
        /// Конвертация параметров в Dictionary и вызов метода PostResponceBak(string reqUrl, Dictionary<string, string> parameters )
        /// </summary>
        /// <param name="reqUrl"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public byte[] PostResponce(string reqUrl, string param)
        {
            _logger.LogTrace($"WebRequest url=[{reqUrl}], param=[{param}], method=[POST]");

            Dictionary<string, string> paramDict = Helpers.Converter.ParamsStringToDictionary(param);

            return PostResponce(reqUrl, paramDict);
        }

        /// <summary>
        ///  Сделать возвращаемое имя файла с нормальной раскодировкой
        /// </summary>
        /// <param name="reqUrl"></param>
        /// <param name="param"></param>
        /// <returns></returns>

        private byte[] PostResponce(string reqUrl, Dictionary<string, string> parameters )
        {
            
            Classes.Sleeper.WaitOne(_downloadDelay);

            using (var client = new HttpClient())
            {
                var req = new HttpRequestMessage(HttpMethod.Post, reqUrl)
                {
                    Content = new FormUrlEncodedContent(parameters ?? new Dictionary<string, string>())
                };
                HttpResponseMessage responce = client.Send(req);
                var result = responce.Content.ReadAsByteArrayAsync().Result;

                
                _logger.LogTrace($"Код ответа от сервера {responce.StatusCode}");

                if (responce.StatusCode == HttpStatusCode.OK)
                {
                    return result;
                }
                _logger.LogError($"Ошибка чтения {reqUrl}. Код ответа от сервера {responce.StatusCode}");
                throw new Exception($"Код ответа от сервера {responce.StatusCode}");
            }
        }
    }
}
