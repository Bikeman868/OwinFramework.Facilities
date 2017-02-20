using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace SampleWebSite.Extensions
{
    internal static class OwinRequestExtensions
    {
        public static Task<IDictionary<string, string>> ReadFormAsync(this IOwinRequest request)
        {
            // application/x-www-form-urlencoded
            // multipart/form-data

            /*
            * https://www.w3.org/TR/html401/interact/forms.html
            * 
            * This is the default content type. Forms submitted with this content type must be encoded as follows:
            * Control names and values are escaped. Space characters are replaced by `+', and then reserved characters are 
            * escaped as described in [RFC1738], section 2.2: Non-alphanumeric characters are replaced by `%HH', a percent 
            * sign and two hexadecimal digits representing the ASCII code of the character. Line breaks are represented 
            * as "CR LF" pairs (i.e., `%0D%0A').
            * The control names/values are listed in the order they appear in the document. The name is separated from the 
            * value by `=' and name/value pairs are separated from each other by `&'.
            */

            return Task.Factory.StartNew<IDictionary<string, string>>(() =>
            {
                var encoding = Encoding.UTF8;
                // TODO: look at Content-Type header to get actual encoding

                var reader = new StreamReader(request.Body, encoding);
                var content = reader.ReadToEnd();

                Func<string, string> decode = s =>
                {
                    s = s.Replace('+', ' ');
                    var regex = new Regex("#[0-9A-F][0-9A-F]");
                    return regex.Replace(s, m =>
                    {
                        var hexDigits = m.Value.Substring(1);
                        var asciiValue = Convert.ToByte(hexDigits, 16);
                        var ch = (char) asciiValue;
                        return new String(new[] { ch });
                    });
                };

                return content
                    .Split('&')
                    .Select(p =>
                    {
                        var e = p.Split('=');
                        return new
                        {
                            name = decode(e[0]),
                            value = decode(e[1])
                        };
                    })
                    .ToDictionary(e => e.name, e => e.value);
            });
        }
    }
}
