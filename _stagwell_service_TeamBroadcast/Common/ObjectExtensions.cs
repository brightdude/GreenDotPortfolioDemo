using Newtonsoft.Json;

namespace Breezy.Muticaster
{
    internal static class ObjectExtensions
    {
        public static string ToJsonString(this object source) => JsonConvert.SerializeObject(source);
    }
}
