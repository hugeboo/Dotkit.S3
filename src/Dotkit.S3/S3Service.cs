using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    internal sealed class S3Service : IS3Service, IDisposable
    {
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;

        public S3Service(S3Configuration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _bucketName = configuration.BucketName;
            var s3Config = new AmazonS3Config
            {
                ServiceURL = configuration.ServiceURL
            };
            _s3Client = new AmazonS3Client(configuration.AccessKeyId, configuration.SecretAccessKey, s3Config);
        }

        public void Dispose()
        {
            _s3Client.Dispose();
        }

        public async Task<S3DirectoryInfo> GetDirectoryInfoAsync(string key)
        {
            var di = new S3DirectoryInfo(_s3Client, _bucketName, key);
            await di.InitFromRemoteAsync();
            return di;
        }
    }
}
