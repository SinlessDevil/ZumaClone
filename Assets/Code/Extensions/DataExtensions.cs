using UnityEngine;

namespace Code.Extensions
{
    public static class DataExtensions
    {
        public static T ToDeserialize<T>(this string json) =>
            JsonUtility.FromJson<T>(json);

        public static string ToJson(this object self) =>
            JsonUtility.ToJson(self, true);

        public static bool ToBool(this int value) => value != 0;
        public static int ToInt(this bool value) => value ? 1 : 0;
    }
}