namespace RvSfDownloadCore.Helpers
{
    internal class Converter
    {
        /// <summary>
        /// Преобразует пару парамете=значение в структуру KeyValuePair<string, string>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static KeyValuePair<string, string> StringToKeyValuePair(string str)
        {
            var items = (str ?? string.Empty).Split("=", StringSplitOptions.RemoveEmptyEntries);
            if (items.Length > 0)
            {
                items[0] = items[0].Trim();
                if (string.IsNullOrEmpty(items[0]))
                    throw new Exception("Параметр имеет пустое имя");

                if (items.Length > 1)
                    return new KeyValuePair<string, string>(items[0], items[1].Trim());

                if (items.Length == 1)
                    return new KeyValuePair<string, string>(items[0], string.Empty);
            }

            throw new Exception("Пустой параметр в Url");
        }


        internal static Dictionary<string, string> ParamsStringToDictionary(string param)
        {
            var paramPairs = param
                .Split("&", StringSplitOptions.RemoveEmptyEntries)
                .Select(StringToKeyValuePair)
                .ToList();

            var paramDict = new Dictionary<string, string>();

            foreach (var pair in paramPairs)
            {
                paramDict.Add(pair.Key, pair.Value);
            }

            return paramDict;
        }
    }
}
