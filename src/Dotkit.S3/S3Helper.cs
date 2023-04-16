﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    internal static class S3Helper
    {
        internal static string EncodeKey(string key)
        {
            return key.Replace('\\', '/');
        }

        internal static string DecodeKey(string key)
        {
            return key.Replace('/', '\\');
        }
    }
}
