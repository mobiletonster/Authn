using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Authn
{
    public class CustomConfigurationProvider: ConfigurationProvider
    {
        IMemoryCache _cache;
        public CustomConfigurationProvider()
        {
            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1024
            });
        }

        public override void Load()
        {
            //var data = _cache.Get<string>("ConnectionStrings:DefaultConnection");
            //if (data == null)
            //{
            //    data = "new crazy conn string";
            //}
            //var dict = new Dictionary<string, string>();
            //dict.Add("ConnectionStrings:DefaultConnection", data);
            //Data = dict;
            Data.Add("ConnectionStrings:DefaultConnection", "new crazy conn string for reals");
        }

        public override bool TryGet(string key, out string value)
        {
            
            if (Data.TryGetValue(key, out value))
            {
                value = "even crazier string than the first one.";
                return true;
            }
            return false;
        }
    }
}
