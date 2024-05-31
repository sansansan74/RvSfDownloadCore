using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RvSfDownloadCore.Helpers
{
    internal class ParamsGetter
    {
        /// <summary>
        /// Чтение параметра типа string из конфигурационного файла по имени 
        /// и обработка исключений если параметр не указан или задан некорректно
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        /// <exception cref="BadParameterException"></exception>
        public static string ReadNonEmptyConfigParam(IConfiguration config, ILogger logger, string paramName)
        {
            var value = ReadConfigParam(config, paramName);

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            logger.LogError($"Не задано значение {paramName} в файле конфигурации");
            throw new BadParameterException(paramName);
        }

        /// <summary>
        /// Чтение параметра типа int из конфигурационного файла по имени 
        /// и обработка исключений если параметр не указан или задан некорректно
        /// </summary>
        public static int ReadNonEmptyIntConfigParam(IConfiguration config, ILogger logger, string paramName)
        {
            var strValue = ReadNonEmptyConfigParam(config, logger, paramName);

            if (int.TryParse(strValue, out var intValue))
            {
                return intValue;
            }

            //config.GetValue<int>(strValue);

            logger.LogError($"Значение {paramName} должно быть типа int");
            throw new BadParameterException(paramName);
        }

        /// <summary>
        /// Чтение параметра типа bool из конфигурационного файла по имени 
        /// и обработка исключений если параметр не указан или задан некорректно
        /// </summary>
        public static bool ReadNonEmptyBoolConfigParam(IConfiguration config, ILogger logger, string paramName)
        {
            var strValue = ReadNonEmptyConfigParam(config, logger, paramName);

            if ( bool.TryParse(strValue, out var intValue))
            {
                return intValue;
            }

            logger.LogError($"Значение {paramName} должно быть типа bool");
            throw new BadParameterException(paramName);
        }

        /// <summary>
        /// Чтение параметра типа bool из конфигурационного файла по имени 
        /// если такого параметра нет или тип параметра некорректен, то возвращает значение defaultValue
        /// </summary>
        public static bool ReadBoolConfigParamWithDefault(IConfiguration config, string paramName, bool defaultValue)
        {
            var strValue = config[paramName] ?? string.Empty;
            
            if (bool.TryParse(strValue, out var intValue))
            {
                return intValue;
            }

            return defaultValue;
        }

        private static string ReadConfigParam(IConfiguration config, string paramName)
            => (config[paramName] ?? String.Empty).Trim();
    }

    class BadParameterException : Exception
    {
        public BadParameterException(string paramName): base($"Ошибка параметра {paramName} в файле конфигурации")
        {
        }
    }
}
