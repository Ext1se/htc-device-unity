using System;
using System.Collections.Generic;
using System.Text;
public static class StringExtentions
{/// <summary>
    /// Возвращает строку от искомого текста и до конца
    /// </summary>
    /// <param name="s"></param>
    /// <param name="value"></param>
    /// <returns>Возвращает строку от искомого текста и до конца</returns>
    public static string AfterInclude(this string s, string value, bool emptyIfNotExistsValue = true)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(value))
            return s.Substring(s.IndexOf(value));
        return emptyIfNotExistsValue ? "" : s;
    }
    /// <summary>
    /// Возвращает строку от последнего найденного искомого текста и до конца
    /// </summary>
    /// <param name="s"></param>
    /// <param name="value"></param>
    /// <returns>Возвращает пустую строку в случае отсутствия искомой строки</returns>
    public static string AfterLastInclude(this string s, string value, bool emptyIfNotExistsValue = true)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(value))
            return s.Substring(s.LastIndexOf(value));
        return emptyIfNotExistsValue ? "" : s;
    }

    public static string After(this string s, string value, bool emptyIfNotExistsValue = true)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(value))
            return s.Substring(s.IndexOf(value) + value.Length);
        return emptyIfNotExistsValue ? "" : s;
    }
    public static string AfterLast(this string s, string value, bool emptyIfNotExistsValue = true)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(value))
            return s.Substring(s.LastIndexOf(value) + value.Length);
        return emptyIfNotExistsValue ? "" : s;
    }
    /// <summary>
    /// Возвращает подстроку
    /// </summary>
    /// <param name="s"></param>
    /// <param name="startIndex"></param>
    /// <returns>Возвращает строку, если ее можно извлеч, 
    /// в противном случае возвращает пустую строку</returns>
    public static string GetSubstring(this string s, int startIndex)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (startIndex < s.Length)
            return s.Substring(startIndex);
        return "";
    }
    /// <summary>
    /// Возвращает подстроку
    /// </summary>
    /// <param name="s"></param>
    /// <param name="startIndex"></param>
    /// <param name="lenght"></param>
    /// <returns>Возвращает строку, если ее можно извлеч в указанном диапазоне, 
    /// в противном случае либо возвращает то что возможно, либо пустую строку</returns>
    public static string GetSubstring(this string s, int startIndex, int lenght)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (startIndex < s.Length)
        {
            if (startIndex + lenght < s.Length)
                return s.Substring(startIndex, lenght);
            else 
                return s.Substring(startIndex);
        }
        return "";
    }
    
    public static bool TryAfter(this string s, string value, out string result)
    {
        result = "";
        if (string.IsNullOrEmpty(s)) return false;
        if (s.Contains(value))
        {
            result = s.Substring(s.IndexOf(value) + value.Length);
            return true;
        }
        return false;
    }
    public static bool TryAfterLast(this string s, string value, out string result)
    {
        result = "";
        if (string.IsNullOrEmpty(s)) return false;
        if (s.Contains(value))
        {
            result = s.Substring(s.LastIndexOf(value) + value.Length);
            return true;
        }
        return false;
    }

    public static string Before(this string s, string value)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(value))
            return s.Substring(0, s.IndexOf(value));
        return "";
    }
    public static string BeforeLast(this string s, string value)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(value))
            return s.Substring(0, s.LastIndexOf(value));
        return "";
    }
    public static string BeforeInclude(this string s, string value)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(value))
            return s.Substring(0, s.IndexOf(value) + value.Length);
        return "";
    }
    public static string BeforeLastInclude(this string s, string value)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(value))
            return s.Substring(0, s.LastIndexOf(value) + value.Length);
        return "";
    }

    public static bool TryBefore(this string s, string value, out string result)
    {
        result = "";
        if (string.IsNullOrEmpty(s)) return false;
        if (s.Contains(value))
        {
            result = s.Substring(0, s.IndexOf(value));
            return true;
        }
        return false;
    }
    public static bool TryBeforeLast(this string s, string value, out string result)
    {
        result = "";
        if (string.IsNullOrEmpty(s))
            return false;
        if (s.Contains(value))
        {
            result = s.Substring(0, s.LastIndexOf(value));
            return true;
        }
        return false;
    }
    /// <summary>
    /// разбивает строки по указанному разделителю и удаляет пустые строки
    /// </summary>
    /// <param name="s"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string[] Split(this string s, string separator)
    {
        if (string.IsNullOrEmpty(s)) return new string[0];
        return s.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
    }
    /// <summary>
    /// удаляет пробелы вначале строки и в конце
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string TrimWhitespaceFrontEnd(this string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int indx = 0;
        while (indx < s.Length && s[indx] == ' ') indx++;
        s = s.Substring(indx);
        indx = s.Length;
        while (indx > 0 && s[indx - 1] == ' ') indx--;
        return s.Substring(0, indx);
    }

    public static int IndexOfAfter(this string s, string value)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        if (s.Contains(value))
            return s.IndexOf(value) + value.Length;
        return -1;
    }
    public static int IndexOfAfterLast(this string s, string value)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        if (s.Contains(value))
            return s.LastIndexOf(value) + value.Length;
        return -1;
    }
    public static int IndexOfBefore(this string s, string value)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        if (s.Contains(value))
            return s.IndexOf(value);
        return -1;
    }
    public static int IndexOfBeforeLast(this string s, string value)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        if (s.Contains(value))
            return s.LastIndexOf(value);
        return -1;
    }

    public static int ToInt(this string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int res;
        int.TryParse(s, out res);
        return res;
    }
    public static float ToFloat(this string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
        float res;
        float.TryParse(s.Replace(",", "."), System.Globalization.NumberStyles.Float, ci, out res);
        return res;
    }
    public static double ToDouble(this string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
        double res;
        double.TryParse(s.Replace(",", "."), System.Globalization.NumberStyles.Float, ci, out res);
        return res;
    }

    public static string Replace(this string s, string[] oldVals, string newVal)
    {
        for (int i = 0; i < oldVals.Length; i++)
        {
            s = s.Replace(oldVals[i], newVal);
        }
        return s;
    }
    
    /// <summary>
    /// перемешать список
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static void Shuffle<T>(this IList<T> list)
    {
        Random random = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    public delegate T ParseAction<T>(string s);
    /// <summary>
    /// Преобразовывает строку в нужный тип
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="s"></param>
    /// <param name="parseAction"></param>
    /// <returns></returns>
    public static T TryParse<T>(this string s, ParseAction<T> parseAction)
    {
        return parseAction(s);
    }

    public static byte[] EncodeFromUTF8(this string s)
    {
        return Encoding.UTF8.GetBytes(s);
    }
    public static byte[] Encode(this string s, string encodingName)
    {
        return Encoding.GetEncoding(encodingName).GetBytes(s);
    }
}
