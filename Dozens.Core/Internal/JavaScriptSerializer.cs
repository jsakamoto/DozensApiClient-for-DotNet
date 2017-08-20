using System;
using Newtonsoft.Json;

namespace DozensAPI
{
    internal class JavaScriptSerializer
    {
        public T Deserialize<T>(string jsonText)
        {
            return JsonConvert.DeserializeObject<T>(jsonText);
        }

        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value);
        }
    }
}
