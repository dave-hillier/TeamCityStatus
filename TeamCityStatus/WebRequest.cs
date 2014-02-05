using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TeamCityStatus
{
    class AsyncWebRequest
    {
        public static IObservable<WebResponse> Get(Uri uri)
        {
            var request = HttpWebRequest.Create(uri);
            return Observable.FromAsyncPattern<WebResponse>(request.BeginGetResponse, request.EndGetResponse)();
        }

        // TODO: timed retry?
        public static IObservable<XDocument> GetXml(Uri uri)
        {
            Debug.WriteLine("Get {0}", uri);
            var obs = Get(uri);
            return obs.Select(rq => 
                {
                    var d = rq.GetResponseStream();
                    return XDocument.Load(d);
                });
        }
    }
}
