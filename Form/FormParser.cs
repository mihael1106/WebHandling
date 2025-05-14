using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Miki1106.WebHandling.Form
{
    public class FormParser
    {
        Dictionary<string, FormField> startingPointsData = new Dictionary<string, FormField>();
        private readonly byte[] formData;
        private byte[] formHeader;
        public FormParser(byte[] data)
        {
            formData = data;
            FindHeader();

            for (int i = 0; i < formData.Length - formHeader.Length - 2; i++)
            {
                if (i != 0)
                {
                    if (!(formData[i] == '\r' && formData[i + 1] == '\n'))
                        continue;
                    else
                        i += 2;
                }

                if (Compare(formData, i, formHeader))
                {
                    if (Compare(formData, i + formHeader.Length, Encoding.UTF8.GetBytes("--")))
                    {
                        if (WebHandler.debug)
                            Console.WriteLine("Got to end");
                        break;
                    }

                    int start = i + formHeader.Length + 2;
                    (int, string[]) properties = GetProperties(start);
                    Dictionary<string, string> fields = new Dictionary<string, string>();
                    foreach (string property in properties.Item2)
                    {
                        string[] split = property.Split(new char[] { ':' }, 2);
                        fields.Add(split[0], split[1].Substring(1));
                    }
                    string fieldName = null;
                    if (fields.TryGetValue("Content-Disposition", out string fieldData))
                    {
                        fieldName = GetField("name", fieldData);
                    }
                    if (fieldName == null)
                    {
                        Console.WriteLine("Couldnt find field name, SKIPPING");
                        continue;
                    }
                    if (WebHandler.debug)
                        Console.WriteLine($"Found field: \"{fieldName}\"");
                    int dataEnd = FindEnd(properties.Item1);
                    if (dataEnd < 0)
                    {
                        Console.WriteLine("Couldnt find end");
                        continue;
                    }

                    i = dataEnd - 2;

                    if (startingPointsData.ContainsKey(fieldName))
                    {
                        startingPointsData[fieldName].DataStart.Add(properties.Item1);
                        startingPointsData[fieldName].DataEnd.Add(dataEnd);
                        startingPointsData[fieldName].Fields.Add(fields);
                    }
                    else
                    {
                        startingPointsData.Add(fieldName, new FormField
                        {
                            DataStart = new List<int> { properties.Item1 },
                            Fields = new List<Dictionary<string, string>> { fields },
                            DataEnd = new List<int> { dataEnd }
                        });
                    }
                }
            }
        }

        public static string GetField(string name, string fieldData)
        {
            int lenght = fieldData.Length;
            for (int i = 0; i < lenght; i++)
            {
                if (fieldData.StartsWith(name + "=\""))
                {
                    fieldData = fieldData.Substring(name.Length + 2);
                    StringBuilder sb = new StringBuilder();
                    for (int j = 0; j < fieldData.Length; j++)
                    {
                        if (fieldData[j] == '\"')
                        {
                            return sb.ToString();
                        }
                        sb.Append(fieldData[j]);
                    }
                }
                fieldData = fieldData.Substring(1);
            }
            return "";
        }

        public byte[] GetRequest()
        {
            return formData;
        }

        private (int, string[]) GetProperties(int start)
        {
            List<string> properties = new List<string>();
            List<byte> data = new List<byte>();
            int i;
            for (i = start; i < formData.Length - 4; i++)
            {
                if (formData[i] == '\r' && formData[i + 1] == '\n' && formData[i + 2] == '\r' && formData[i + 3] == '\n')
                {
                    properties.Add(Encoding.UTF8.GetString(data.ToArray()));
                    data.Clear();
                    break;
                }
                if (formData[i] == '\r' && formData[i + 1] == '\n')
                {
                    properties.Add(Encoding.UTF8.GetString(data.ToArray()));
                    data.Clear();
                    i += 2;
                }
                data.Add(formData[i]);
            }
            return (i + 4, properties.ToArray());
        }

        private static bool Compare(byte[] bytes, int start, byte[] compare)
        {
            for (int i = 0; i < compare.Length; i++)
            {
                if (bytes[start + i] != compare[i])
                {
                    return false;
                }
            }
            return true;
        }

        private void FindHeader()
        {
            List<byte> header = new List<byte>();
            for (int i = 0; i < formData.Length - 1; i++)
            {
                if (formData[i] == '\r' && formData[i + 1] == '\n')
                {
                    break;
                }
                header.Add(formData[i]);
            }
            formHeader = header.ToArray();
        }

        private int FindEnd(int dataStart)
        {
            for (int i = dataStart; i < formData.Length - formHeader.Length; i++)
            {
                if (i != 0)
                {
                    if (!(formData[i] == '\r' && formData[i + 1] == '\n'))
                        continue;

                    i += 2;
                }

                if (Compare(formData, i, formHeader))
                {
                    return i - 2;
                }
            }
            return -1;
        }

        public T GetData<T>(string field, IFormMapper<T> parser)
        {
            return parser.Parse(new ParserInfo(field, startingPointsData, formData, formHeader));
        }
    }
}
