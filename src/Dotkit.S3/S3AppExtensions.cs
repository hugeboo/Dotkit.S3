using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    /// <summary>
    /// Расширения для внедрения зависимостей
    /// </summary>
    public static class S3AppExtensions
    {
        /// <summary>
        /// Фабрика сервиса
        /// </summary>
        /// <param name="configuration">Конфигурация сервиса</param>
        /// <returns>Экземпляр сервиса</returns>
        public static IS3Service CreateService(this S3Configuration configuration)
        {
            return new S3Service(configuration);
        }
    }
}
