using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CustomVison
{
    public partial class CustomVisionResult
    {
        [JsonProperty("id")]
        public string ID { get; set; }

       

        [JsonProperty("recognitionData")]
        public List<RecognitionData> recognitionData { get; set; }
    }

    public partial class RecognitionData
    {
        [JsonProperty("probability")]
        public double probability { get; set; }

       
        public string text { get; set; }
        public string marker { get; set; }

        [JsonProperty("boundingBox")]
        public BoundingBox boundingBox { get; set; }
    }

    public partial class BoundingBox
    {
        [JsonProperty("x")]
        public float x { get; set; }

        [JsonProperty("y")]
        public float y { get; set; }

        [JsonProperty("width")]
        public float width { get; set; }

        [JsonProperty("height")]
        public float height { get; set; }
    }
}
