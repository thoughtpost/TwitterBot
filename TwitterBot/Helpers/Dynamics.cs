using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Thoughtpost.Helpers
{
    public class Dynamics
    {

        public static bool HasProperty(dynamic obj, string name)
        {
            if (obj == null) return false;

            Type objType = obj.GetType();

            if (objType == typeof(ExpandoObject))
            {
                return ((IDictionary<string, object>)obj).ContainsKey(name);
            }
            if (objType == typeof(Newtonsoft.Json.Linq.JObject))
            {
                return (obj[name] != null);
            }

            return objType.GetProperty(name) != null;
        }

        public static object GetObjectProperty(dynamic obj, string name)
        {
            var dict = (IDictionary<string, object>)obj;
            if (dict.Keys.Contains(name))
            {
                return dict[name];
            }
            return null;
        }

    }
}
