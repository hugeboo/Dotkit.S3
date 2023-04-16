using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    /// <summary>
    /// Сервис, обеспечивающий интерфейс к S3 хранилищу
    /// </summary>
    /// <remarks>
    /// Работает в рамках одного бакета
    /// </remarks>
    public interface IS3Service
    {
        /// <summary>
        /// Возвращает информацию о директории
        /// </summary>
        /// <param name="key">Путь к директории</param>
        /// <returns>Информация о директории</returns>
        Task<S3DirectoryInfo> GetDirectoryAsync(string key);

        /// <summary>
        /// Возвразает информацию о файле
        /// </summary>
        /// <param name="key">Путь к файлу</param>
        /// <returns>Инфорамция о файле</returns>
        Task<S3FileInfo> GetFileAsync(string key);
    }
}
