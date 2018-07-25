using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BigReminder365
{
    internal static class TokenCacheHelper
    {
        private static TokenCache s_usertokenCache;
        private static readonly object s_fileLock = new object();
        private static readonly string s_cacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin";

        public static TokenCache GetUserCache()
        {
            if (s_usertokenCache == null)
            {
                s_usertokenCache = new TokenCache();
                s_usertokenCache.SetBeforeAccess(BeforeAccessNotification);
                s_usertokenCache.SetAfterAccess(AfterAccessNotification);
            }
            return s_usertokenCache;
        }

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (s_fileLock)
            {
                args.TokenCache.Deserialize(File.Exists(s_cacheFilePath) ? ProtectedData.Unprotect(File.ReadAllBytes(s_cacheFilePath), null, DataProtectionScope.CurrentUser) : null);
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.TokenCache.HasStateChanged)
            {
                lock (s_fileLock)
                {
                    File.WriteAllBytes(s_cacheFilePath, ProtectedData.Protect(args.TokenCache.Serialize(), null, DataProtectionScope.CurrentUser));
                    args.TokenCache.HasStateChanged = false;
                }
            }
        }
    }
}
