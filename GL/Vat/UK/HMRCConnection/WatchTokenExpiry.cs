using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection
{
    class WatchTokenExpiry
    {
        long _expireTime;
        string _refreshToken;
        EventHandler expiredHandler;

        /// <summary>
        /// Creates new instance of the <see cref="WatchTokenExpiry"/> class. Will fire eventhandler to refresh OAuth token when <paramref name="expireTime"/> 
        /// </summary>
        /// <param name="expireTime"></param>
        /// <param name="refreshToken"></param>
        public WatchTokenExpiry()
        {
            _expireTime = Common.ExpiresIn;
            _refreshToken = Common.RefreshToken;
            //expiredHandler += RefreshAuthToken;
            //WatchExpiry();
        }

        async Task WatchExpiry()
        {
            //check every second until timer is zero
            while (_expireTime > 0)
            {
                Thread.Sleep(1000);
                _expireTime -= 1;
            }
            expiredHandler.Invoke(this, new EventArgs());
        }
    }
}
