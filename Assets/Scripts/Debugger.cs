using UnityEngine;

namespace UnityService
{
    public class Debugger
    {
        public static void Log(string message)
        {
            Debug.Log($"{AppStrings.APP}: {message}");
        }
    }
}