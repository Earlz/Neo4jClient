﻿using System.IO;
using Newtonsoft.Json;
using RestSharp.Serializers;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Neo4jClient.Serializer
{
    public class CustomJsonSerializer : ISerializer
    {
        public string ContentType { get; set; }
        public string DateFormat { get; set; }
        public string Namespace { get; set; }
        public string RootElement { get; set; }
        public NullValueHandling NullHandling {get; set;}

        public CustomJsonSerializer()
        {
            ContentType = "application/json";
            NullHandling = NullValueHandling.Ignore;
        }

        public string Serialize(object obj)
        {
            string str2;
            var serializer2 = new JsonSerializer
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullHandling,
                DefaultValueHandling = DefaultValueHandling.Include
            };
            var serializer = serializer2;
            using (var writer = new StringWriter())
            using (var writer2 = new JsonTextWriter(writer))
            {
                writer2.Formatting = Formatting.Indented;
                writer2.QuoteChar = '"';
                serializer.Serialize(writer2, obj);
                str2 = writer.ToString();
            }
            return str2;
        }
    }
}