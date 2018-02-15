using global::CommerceServer.Core.Runtime.Caching;
using System;
using System.Collections;
using System.Globalization;
using System.Web;
using Sitecore.Commerce.Connect.CommerceServer.Catalog;
using Sitecore.Globalization;
using Sitecore.Configuration;
using Sitecore.Commerce.Connect.CommerceServer;
using Sitecore.Data;
using Sitecore.Caching;
using Sitecore.Data.Items;

namespace Sitecore.Support.Commerce.Connect.CommerceServer.Caching
{
    public static class CacheRefresh
    {
        // Fields
        private static DateTime? _lastCatalogCacheRefreshTimeUtc;
        private static object _lastCatalogCacheRefreshTimeUtcLock = new object();

        // Methods
        public static void Refresh(string cacheEventName, ICommerceServerContextManager contextManager, string databaseName = null, ID itemId = null)
        {
            object[] args = new object[] { cacheEventName };
            CommerceTrace.Current.Write(string.Format(CultureInfo.InvariantCulture, "Refreshing cache: {0}", args));
            bool flag = true;
            if (!string.IsNullOrWhiteSpace(cacheEventName))
            {
                if (cacheEventName.Equals("sitecore", StringComparison.OrdinalIgnoreCase))
                {
                    if (itemId != ID.Null)
                    {
                        flag = false;
                        RemoveItemFromSitecoreCaches(itemId, databaseName);
                    }
                }
                else
                {
                    string str = cacheEventName.ToLower(CultureInfo.InvariantCulture);
                    if (str == "catalogcache")
                    {
                        RefreshCatalogCaches(contextManager);
                        if (itemId != ID.Null)
                        {
                            flag = false;
                            RemoveItemFromSitecoreCaches(itemId, databaseName);
                        }
                    }
                    else if (str == "commercecaches")
                    {
                        RefreshCommerceCaches(contextManager);
                    }
                    else if (str == "sitetermcache")
                    {
                        RefreshSiteTermCaches(contextManager);
                    }
                    else if (str == "allcaches")
                    {
                        RefreshCatalogCaches(contextManager);
                        RefreshCommerceCaches(contextManager);
                        RefreshSiteTermCaches(contextManager);
                    }
                    else
                    {
                        RefreshSingleCache(cacheEventName, contextManager);
                    }
                }
            }
            if (flag)
            {
                RefreshSitecoreCaches(databaseName);
            }
        }

        public static void RefreshCatalogCaches(ICommerceServerContextManager contextManager)
        {
            LastCatalogCacheRefreshTimeUtc = new DateTime?(DateTime.UtcNow);
            contextManager.CatalogContext.RefreshCache();
            CatalogUtility.RefreshExternalIdCache(false);
            CatalogUtility.RefreshTemplateHierarchyCache(false);
        }

        private static void RefreshCommerceCaches(ICommerceServerContextManager contextManager)
        {
            CommerceCacheCollection cacheCollection = contextManager.Caches;
            if (cacheCollection != null)
            {
                foreach (CommerceCache commerceCache in cacheCollection)
                {
                    commerceCache.Refresh();
                }
            }

        }

        private static void RefreshSingleCache(string cacheName, ICommerceServerContextManager contextManager)
        {
            CommerceCacheCollection caches = contextManager.Caches;
            if (caches != null)
            {
                CommerceCache cache = caches[cacheName];
                if (cache != null)
                {
                    cache.Refresh();
                }
                else
                {
                    object[] args = new object[] { cacheName };
                    CommerceLog.Current.Error(string.Format(CultureInfo.InvariantCulture, "The given Commerce cache name does not exist: {0}", args), typeof(CacheRefresh));
                }
            }
        }

        public static void RefreshSitecoreCaches(string databaseName)
        {
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                Database database = Database.GetDatabase(databaseName);
                if (database != null)
                {
                    database.Caches.ItemCache.Clear();
                    database.Caches.DataCache.Clear();
                    database.Caches.StandardValuesCache.Clear();
                    database.Caches.PathCache.Clear();
                    database.Caches.ItemPathsCache.Clear();
                    database.Engines.TemplateEngine.Reset();
                }
            }
            else
            {
                foreach (Database database2 in Factory.GetDatabases())
                {
                    if (database2 != null)
                    {
                        database2.Caches.ItemCache.Clear();
                        database2.Caches.DataCache.Clear();
                        database2.Caches.StandardValuesCache.Clear();
                        database2.Caches.PathCache.Clear();
                        database2.Caches.ItemPathsCache.Clear();
                        database2.Engines.TemplateEngine.Reset();
                    }
                }
            }
        }

        private static void RefreshSiteTermCaches(ICommerceServerContextManager contextManager)
        {
            try
            {
                contextManager.ProfileContext.RefreshSiteTerms();
            }
            catch (Exception exception)
            {
                object[] objArray1 = new object[] { "sitetermcache" };
                string message = Translate.Text("Cannot refresh Commerce {0} cache", objArray1);
                CommerceLog.Current.Error(message, typeof(CacheRefresh), exception);
            }
        }

        public static void RemoveItemFromSitecoreCaches(ID itemId, string databaseName)
        {
            ID id = null;
            if (itemId != id)
            {
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    Database database = Factory.GetDatabase(databaseName, false);
                    if (database != null)
                    {
                        database.Caches.ItemCache.RemoveItem(itemId);
                        database.Caches.DataCache.RemoveItemInformation(itemId);
                        database.Caches.StandardValuesCache.RemoveKeysContaining(itemId.ToString());
                        database.Caches.PathCache.RemoveKeysContaining(itemId.ToString());
                        Item item = database.GetItem(itemId);
                        if (item != null)
                        {
                            database.Caches.ItemPathsCache.Remove(new ItemPathCacheKey(item.Paths.FullPath, itemId));
                        }
                    }
                }
                else
                {
                    foreach (Database database2 in Factory.GetDatabases())
                    {
                        if (database2 != null)
                        {
                            database2.Caches.ItemCache.RemoveItem(itemId);
                            database2.Caches.DataCache.RemoveItemInformation(itemId);
                            database2.Caches.StandardValuesCache.RemoveKeysContaining(itemId.ToString());
                            database2.Caches.PathCache.RemoveKeysContaining(itemId.ToString());
                            Item item2 = database2.GetItem(itemId);
                            if (item2 != null)
                            {
                                database2.Caches.ItemPathsCache.Remove(new ItemPathCacheKey(item2.Paths.FullPath, itemId));
                            }
                        }
                    }
                }
            }
        }

        // Properties
        public static DateTime? LastCatalogCacheRefreshTimeUtc
        {
            get { return _lastCatalogCacheRefreshTimeUtc; }

            private set
            {
                object obj2 = _lastCatalogCacheRefreshTimeUtcLock;
                lock (obj2)
                {
                    _lastCatalogCacheRefreshTimeUtc = value;
                }
            }
        }
    }






}