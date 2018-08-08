﻿using System;
using System.Threading.Tasks;

#if !NETSTANDARD2_0 && !NET40
namespace ToolGood.ReadyGo3.Gadget.Caches
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Md5MemoryCacheService : MemoryCacheService
    {
        public override async Task<T> GetAsync<T>(string name, Func<Task<T>> func, int expiredSecond, string regionName)
        {
            name = getMd5String(name);
            return await base.GetAsync<T>(name, func, expiredSecond, regionName);

        }
        
    }
}
#endif
