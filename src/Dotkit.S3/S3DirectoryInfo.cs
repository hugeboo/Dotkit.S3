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
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

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
        private S3DirectoryInfo? _root = null;

        /// <summary>
        /// "Корень" S3 хранилища
        /// </summary>
        public S3DirectoryInfo Root
        {
            get
            {
                if (_root == null)
                {
                    _root = new S3DirectoryInfo(_s3Client, _bucketName, "");
                    _root.Exists = true;
                }
                return _root;
            }
        }

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
        }

        /// <summary>
        /// Актуалзировать информацию (Exists, LastModifiedTime) из S3 хранилища
        /// </summary>
        internal async Task UpdateFromRemoteAsync()
        {
            try
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
            catch(AmazonS3Exception ex)
            {
                if (string.Equals(ex.ErrorCode, "NotFound"))
                {
                    Exists = false;
                    LastModifiedTime = DateTime.MinValue;
                }
                throw;
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
                await di.UpdateFromRemoteAsync();
                return di;
            }
        }

        /// <summary>
        /// Создать директорию
        /// </summary>
        /// <remarks>
        /// Если директория уже существует, то метод ничего не делает
        /// </remarks>
        /// <returns>Ссылка на себя</returns>
        public async Task<S3DirectoryInfo> CreateAsync()
        {
            var request = new PutObjectRequest 
            {
                BucketName = _bucketName, 
                Key = S3Helper.EncodeKey(_key)
            };
            await _s3Client.PutObjectAsync(request);

            await UpdateFromRemoteAsync();

            return this;
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

            await UpdateFromRemoteAsync();
        }

        /// <summary>
        /// Получить поддиректорию
        /// </summary>
        /// <param name="name">Имя поддиректории</param>
        /// <returns>Поддиреткория</returns>
        public async Task<S3DirectoryInfo> GetSubDirectoryAsync(string name)
        {
            var key = Path.Combine(_key, name);
            var di = new S3DirectoryInfo(_s3Client, _bucketName, key);
            await di.UpdateFromRemoteAsync();
            return di;
        }

        /// <summary>
        /// Получить список поддиректорий
        /// </summary>
        /// <remarks>
        /// По каждой поддиректории будет отдельный запрос в S3 хранилище для инициализации полей
        /// </remarks>
        /// <returns>Список поддиректорий</returns>
        public async Task<List<S3DirectoryInfo>> GetDirectories()
        {
            var request = new ListObjectsV2Request 
            {
                BucketName = _bucketName, 
                Prefix = S3Helper.EncodeKey(_key), 
                Delimiter = "/"
            };
            var response = await _s3Client.ListObjectsV2Async(request);

            var lst = new List<S3DirectoryInfo>();
            foreach (var item in response.CommonPrefixes)
            {
                var di = new S3DirectoryInfo(_s3Client, _bucketName, S3Helper.DecodeKey(item));
                await di.UpdateFromRemoteAsync();
                lst.Add(di);
            }
            return lst;
        }

        /// <summary>
        /// Получить список файлов в директории
        /// </summary>
        /// <returns>Список файлов</returns>
        public async Task<List<S3FileInfo>> GetFiles()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = S3Helper.EncodeKey(_key),
                Delimiter = "/"
            };

            var lst = new List<S3FileInfo>();

            ListObjectsV2Response? response;
            do
            {
                response = await _s3Client.ListObjectsV2Async(request);
                foreach (S3Object item in response.S3Objects)
                {
                    var key = S3Helper.DecodeKey(item.Key);

                    if (string.Equals(_key, key, StringComparison.Ordinal) || !key.EndsWith("\\"))
                        continue;

                    var file = new S3FileInfo(_s3Client, _bucketName, key);
                    file.InitFromRemoteObject(item);

                    lst.Add(file);

                    request.StartAfter = item.Key;
                }
            }
            while (response.IsTruncated);

            return lst;
        }

        /// <summary>
        /// Получить список всего содержимого директории (поддиректрии + файлы)
        /// </summary>
        /// <returns>Список содержимого</returns>
        public async Task<List<IS3FileSystemInfo>> GetItems()
        {
            var lst = new List<IS3FileSystemInfo>();

            var dirs = await GetDirectories();
            lst.AddRange(dirs);

            var files = await GetFiles();
            lst.AddRange(files);

            return lst;
        }
    }
}
