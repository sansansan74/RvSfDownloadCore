using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RvSfDownloadCore.Util
{
    public static class StreamUtils
    {
        /// <summary>
        /// Считывает поток и возвращает массив байт
        /// </summary>
        /// <param name="input">поток</param>
        /// <param name="bufSize">начальный размер буфера в потоке для чтения</param>
        /// <returns>массив байтов с содержимым потока</returns>
        public static byte[] ReadToEndBytes(Stream input, int bufSize = 4096)
        {
            using (var ms = new MemoryStream(bufSize))
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Считывает поток и возвращает строку
        /// </summary>
        /// <param name="input">поток</param>
        /// <returns>строка с содержимым потока</returns>
        public static string ReadToEndString(Stream input)
        {
            using (var reader = new StreamReader(input))
            {
                return reader.ReadToEnd();
            }
        }

    }

}
