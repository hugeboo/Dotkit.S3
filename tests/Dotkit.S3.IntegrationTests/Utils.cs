using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3.IntegrationTests
{
    internal static class Utils
    {
        internal static S3Configuration GetConfiguration()
        {
            return new S3Configuration
            {
                ServiceURL = "https://s3.yandexcloud.net",
                AccessKeyId = "YCAJEIzcBfUuI2bK_G3l4k4br",
                SecretAccessKey = "YCNOYDJLZkFf292p-BZMrHLxsnuWzE2JCWCXlA1N",
                BucketName = "test1-sesv"
            };
        }

        // Тестовая структура директорий:
        //
        // EnumerateTests
        // |
        // +-Level11
        // | |
        // | +-Level21
        // | +-Level22
        // | +-text11.txt
        // |
        // +-Level12
        // | |
        // | +-Level23
        // |   |
        // |   +-Level31
        // |   +-Level32
        // +-text00.txt

        public static IS3Service CreateEnumerateTestDirs()
        {
            var config = GetConfiguration();
            var service = config.CreateService();

            var root = service.GetDirectoryAsync("EnumerateTests").Result.CreateAsync().Result;

            var level11 = root.GetSubDirectoryAsync("Level11").Result.CreateAsync().Result;
            var level21 = level11.GetSubDirectoryAsync("Level21").Result.CreateAsync().Result;
            var level22 = level11.GetSubDirectoryAsync("Level22").Result.CreateAsync().Result;

            var level12 = root.GetSubDirectoryAsync("Level12").Result.CreateAsync().Result;
            var level23 = level12.GetSubDirectoryAsync("Level23").Result.CreateAsync().Result;
            var level31 = level23.GetSubDirectoryAsync("Level31").Result.CreateAsync().Result;
            var level32 = level23.GetSubDirectoryAsync("Level32").Result.CreateAsync().Result;

            var text00 = service.GetFileAsync("EnumerateTests\\text00.txt").Result.CreateTextAsync("test00").Result;
            var text11 = service.GetFileAsync("EnumerateTests\\Level11\\text11.txt").Result.CreateTextAsync("test11").Result;

            return service;
        }
    }
}
