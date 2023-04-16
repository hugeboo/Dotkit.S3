using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    /// <summary>
    /// Операции с директорией в рамках бакета S3 хранилища.
    /// <inheritdoc/>
    /// </summary>
    public sealed class S3DirectoryInfo : IS3FileSystemInfo
    {
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;
        private readonly string _key;
        private readonly S3DirectoryInfo _root;

        /// <summary>
        /// "Корень" S3 хранилища
        /// </summary>
        public S3DirectoryInfo Root => _root;

        public bool Exists { get; private set; }

        public DateTime LastModifiedTime { get; private set; }

        public FileSystemType Type => FileSystemType.Directory;

        public string Extension => string.Empty;

        public string FullName => $"{_bucketName}:\\{_key}";

        public string Name
        {
            get
            {
                int num = _key.LastIndexOf('\\');
                int num2 = _key.LastIndexOf('\\', num - 1);
                return _key.Substring(num2 + 1, num - num2 - 1);
            }
        }

        /// <summary>
        /// Конструктор класса директории
        /// </summary>
        /// <param name="s3Client">Клиент для связи с S3 хранилищем</param>
        /// <param name="bucket">Имя бакета</param>
        /// <param name="key">Путь к директории</param>
        internal S3DirectoryInfo(IAmazonS3 s3Client, string bucket, string key) 
        {
            _s3Client = s3Client;
            _bucketName = bucket;
            _key = key;
            if (!_key.EndsWith("\\"))
            {
                _key = _key + "\\";
            }
            if (_key == "\\")
            {
                _key = string.Empty;
            }

            _root = new S3DirectoryInfo(_s3Client, _bucketName, "");
            _root.Exists = true;
        }

        /// <summary>
        /// Актуалзировать информацию (Exists, LastModifiedTime) из S3 хранилища
        /// </summary>
        internal async Task InitFromRemoteAsync()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = S3Helper.EncodeKey(_key),
                MaxKeys = 1
            };
            var response = await _s3Client.ListObjectsV2Async(request);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK && response.S3Objects.Count == 1)
            {
                Exists = true;
                LastModifiedTime = response.S3Objects[0].LastModified;
            }
            else
            {
                Exists = false;
                LastModifiedTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Получить родительскую директорию
        /// </summary>
        /// <returns>Родительская директория</returns>
        public async Task<S3DirectoryInfo> GetParentAsync()
        {
            int num = _key.LastIndexOf('\\');
            int num2 = _key.LastIndexOf('\\', num - 1);
            if (num2 == -1)
            {
                return Root;
            }
            else
            {
                string text = _key.Substring(0, num2);
                var di = new S3DirectoryInfo(_s3Client, _bucketName, text);
                await di.InitFromRemoteAsync();
                return di;
            }
        }

        /// <summary>
        /// Создать директорию
        /// </summary>
        /// <remarks>
        /// Если директория уже существует, то метод ничего не делает
        /// </remarks>
        public async Task CreateAsync()
        {
            var request = new PutObjectRequest 
            {
                BucketName = _bucketName, 
                Key = S3Helper.EncodeKey(_key)
            };
            await _s3Client.PutObjectAsync(request);

            await InitFromRemoteAsync();
        }

        public async Task DeleteAsync()
        {
            await DeleteAsync(false);
        }

        /// <summary>
        /// Удалить директорию
        /// </summary>
        /// <remarks>
        /// Если есть вложенные директории и <paramref name="recursive"/>==false, то метод ничего не делает
        /// </remarks>
        /// <param name="recursive">Удалить вложенные директории</param>
        public async Task DeleteAsync(bool recursive)
        {
            if (recursive)
            {
                var listObjectsRequest = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = S3Helper.EncodeKey(_key)
                };

                var deleteObjectsRequest = new DeleteObjectsRequest
                {
                    BucketName = _bucketName
                };

                ListObjectsV2Response? listObjectsResponse = null;
                do
                {
                    listObjectsResponse = await _s3Client.ListObjectsV2Async(listObjectsRequest);
                    foreach (S3Object item in listObjectsResponse.S3Objects.OrderBy((S3Object x) => x.Key))
                    {
                        deleteObjectsRequest.AddKey(item.Key);
                        if (deleteObjectsRequest.Objects.Count == 1000)
                        {
                            await _s3Client.DeleteObjectsAsync(deleteObjectsRequest);
                            deleteObjectsRequest.Objects.Clear();
                        }

                        listObjectsRequest.StartAfter = item.Key;
                    }
                }
                while (listObjectsResponse.IsTruncated);
                if (deleteObjectsRequest.Objects.Count > 0)
                {
                    await _s3Client.DeleteObjectsAsync(deleteObjectsRequest);
                }
            }

            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = S3Helper.EncodeKey(_key)
            };
            await _s3Client.DeleteObjectAsync(deleteObjectRequest);

            await InitFromRemoteAsync();
        }

        /// <summary>
        /// Получить поддиректорию
        /// </summary>
        /// <param name="name">Имя поддиректории</param>
        /// <returns>Поддиреткория</returns>
        public async Task<S3DirectoryInfo> GetSubDirectoryInfoAsync(string name)
        {
            var key = Path.Combine(_key, name);
            var di = new S3DirectoryInfo(_s3Client, _bucketName, key);
            await di.InitFromRemoteAsync();
            return di;
        }
    }
}
