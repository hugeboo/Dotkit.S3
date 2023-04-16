using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    /// <summary>
    /// Конфигурация сервиса
    /// </summary>
    public sealed class S3Configuration
    {
        /// <summary>
        /// Адрес S3 хранилища
        /// </summary>
        public string ServiceURL { get; set; } = "https://s3.yandexcloud.net";

        /// <summary>
        /// Идентификатор доступа
        /// </summary>
        public string AccessKeyId { get; set; } = "YCAJEIzcBfUuI2bK_G3l4k4br";

        /// <summary>
        /// Секретный ключ
        /// </summary>
        public string SecretAccessKey { get; set; } = "YCNOYDJLZkFf292p-BZMrHLxsnuWzE2JCWCXlA1N";

        /// <summary>
        /// Регион
        /// </summary>
        public string Region { get; set; } = "us-east-1";// "ru-central1"; // us-east-1

        /// <summary>
        /// Имя бакета
        /// </summary>
        /// <remarks>
        /// Сервис работает в рамках одного бакета
        /// </remarks>
        public string BucketName { get; set; } = "test1-sesv";

        public override string ToString()
        {
            return $"{BucketName} - {ServiceURL} - {AccessKeyId}";
        }
    }
}
