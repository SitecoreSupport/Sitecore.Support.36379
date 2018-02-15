using Sitecore.Commerce.Connect.CommerceServer;
using Sitecore.Commerce.Connect.CommerceServer.Caching;
using Sitecore.Commerce.Connect.CommerceServer.Events;
using Sitecore.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace Sitecore.Support.Commerce.Connect.CommerceServer.Events
{
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "This is a Sitecore EventHandler.")]
    public class CommerceCacheRefreshEventHandler
    {
        // Methods
        public virtual void OnCacheRefresh(object sender, EventArgs args)
        {
            if (args != null)
            {
                CacheRefreshEventArgs args2 = args as CacheRefreshEventArgs;
                if (args2 != null)
                {
                    ICommerceServerContextManager contextManager = CommerceTypeLoader.CreateInstance<ICommerceServerContextManager>();
                    #region Fix 36379
                    Sitecore.Support.Commerce.Connect.CommerceServer.Caching.CacheRefresh.Refresh(args2.CacheType, contextManager, args2.DatabaseName, args2.ItemID);
                    #endregion
                }
            }
        }

        public static void Run(CacheRefreshEvent @event)
        {
            CacheRefreshEventArgs args = new CacheRefreshEventArgs(@event.CacheType, @event.DatabaseName, @event.ItemID);
            object[] objArray1 = new object[] { args };
            Event.RaiseEvent("commercecacherefresh:remote", objArray1);
        }
    }

}