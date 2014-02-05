using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace TeamCityStatus
{
    interface IWebRequest
    {
        IObservable<WebResponse> Get(Uri uri);
        IObservable<XDocument> GetXml(Uri uri);
    }
}
