using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
namespace SummonersTable
{
    // Strict JSON: duplicate keys, unknown fields, nonfinite numbers and trailing input are errors.
    public static class ConfigJson
    {
        public static object Parse(string json){var p=new Parser(json);var v=p.Value();p.Space();if(p.i!=json.Length)throw p.Error("Unexpected trailing text");return v;}
        public static Dictionary<string,object> Object(object value)=>value as Dictionary<string,object>??throw new FormatException("Expected JSON object");
        public static T Read<T>(string json){var value=Parse(json);Validate(typeof(T),value,"$");return JsonUtility.FromJson<T>(json);}
        public static void Validate(Type type,object node,string path)
        {
            if(node==null)throw new FormatException(path+": null is not allowed");
            if(type==typeof(string)){if(!(node is string))throw new FormatException(path+": expected string");return;}
            if(type==typeof(bool)){if(!(node is bool))throw new FormatException(path+": expected boolean");return;}
            if(type.IsPrimitive||type.IsEnum)
            {if(!(node is double n)||double.IsNaN(n)||double.IsInfinity(n)||((type==typeof(int)||type.IsEnum)&&(n!=Math.Truncate(n)||n<int.MinValue||n>int.MaxValue)))throw new FormatException(path+": invalid number");return;}
            if(type==typeof(Vector2Int)){var vector=Object(node);if(vector.Count!=2||!vector.ContainsKey("x")||!vector.ContainsKey("y"))throw new FormatException(path+": expected integer x/y");Validate(typeof(int),vector["x"],path+".x");Validate(typeof(int),vector["y"],path+".y");return;}
            if(type.IsArray||type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(List<>))
            {var list=node as List<object>??throw new FormatException(path+": expected array");var element=type.IsArray?type.GetElementType():type.GetGenericArguments()[0];for(int i=0;i<list.Count;i++)Validate(element,list[i],path+"["+i+"]");return;}
            var map=Object(node);foreach(var pair in map)
            {var field=type.GetField(pair.Key,BindingFlags.Public|BindingFlags.Instance);if(field==null||field.IsNotSerialized)throw new FormatException(path+"."+pair.Key+": unknown parameter");Validate(field.FieldType,pair.Value,path+"."+pair.Key);}
        }
        public static string Write(object value)
        {
            if(value==null)return "null";if(value is string s){var b=new StringBuilder("\"");foreach(char c in s){switch(c){case '"':b.Append("\\\"");break;case '\\':b.Append("\\\\");break;case '\n':b.Append("\\n");break;case '\r':b.Append("\\r");break;case '\t':b.Append("\\t");break;default:if(c<32)b.Append("\\u"+((int)c).ToString("x4"));else b.Append(c);break;}}return b.Append('"').ToString();}
            if(value is bool boolean)return boolean?"true":"false";
            if(value is IDictionary<string,object> map){var items=new List<string>();foreach(var pair in map)items.Add(Write(pair.Key)+":"+Write(pair.Value));return "{"+string.Join(",",items)+"}";}
            if(value is IEnumerable list){var items=new List<string>();foreach(var item in list)items.Add(Write(item));return "["+string.Join(",",items)+"]";}
            if(value is IFormattable f)return f.ToString(null,CultureInfo.InvariantCulture);
            return JsonUtility.ToJson(value);
        }
        sealed class Parser
        {
            readonly string s;public int i;int depth;
            public Parser(string text){s=text??throw new ArgumentNullException(nameof(text));}
            public FormatException Error(string message)=>new FormatException(message+" at character "+i);
            public void Space(){while(i<s.Length&&char.IsWhiteSpace(s[i]))i++;}
            char Take(){if(i>=s.Length)throw Error("Unexpected end");return s[i++];}
            public object Value()
            {
                Space();if(++depth>64)throw Error("JSON nesting too deep");object result;char c=Take();
                if(c=='{')
                {var map=new Dictionary<string,object>();Space();if(i<s.Length&&s[i]=='}'){i++;result=map;}else{while(true){Space();if(Take()!='"')throw Error("Expected key");string key=String();Space();if(Take()!=':')throw Error("Expected colon");if(map.ContainsKey(key))throw Error("Duplicate key "+key);map[key]=Value();Space();char end=Take();if(end=='}')break;if(end!=',')throw Error("Expected comma");}result=map;}}
                else if(c=='['){var list=new List<object>();Space();if(i<s.Length&&s[i]==']')i++;else while(true){list.Add(Value());Space();char end=Take();if(end==']')break;if(end!=',')throw Error("Expected comma");}result=list;}
                else if(c=='"')result=String();
                else if(c=='t'){Literal("rue");result=true;}else if(c=='f'){Literal("alse");result=false;}else if(c=='n'){Literal("ull");result=null;}
                else {int start=--i;while(i<s.Length&&"-+0123456789.eE".IndexOf(s[i])>=0)i++;string n=s.Substring(start,i-start);if(!System.Text.RegularExpressions.Regex.IsMatch(n,@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?$")||!double.TryParse(n,NumberStyles.Float,CultureInfo.InvariantCulture,out double number)||double.IsInfinity(number))throw Error("Invalid number");result=number;}
                depth--;return result;
            }
            void Literal(string rest){foreach(char c in rest)if(Take()!=c)throw Error("Invalid literal");}
            string String(){var b=new StringBuilder();while(true){char c=Take();if(c=='"')return b.ToString();if(c<32)throw Error("Control character in string");if(c!='\\'){b.Append(c);continue;}switch(Take()){case '"':b.Append('"');break;case '\\':b.Append('\\');break;case '/':b.Append('/');break;case 'n':b.Append('\n');break;case 'r':b.Append('\r');break;case 't':b.Append('\t');break;case 'b':b.Append('\b');break;case 'f':b.Append('\f');break;case 'u':if(i+4>s.Length)throw Error("Invalid escape");b.Append((char)int.Parse(s.Substring(i,4),NumberStyles.HexNumber));i+=4;break;default:throw Error("Invalid escape");}}}
        }
    }
}
