using Newtonsoft.Json;
using System.Collections.Generic;

namespace GafExplode.Json
{
    class GafFrameJson
    {
        public long Duration { get; set; }

        public long Unknown3 { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<GafLayerJson> Layers { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? OriginX { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? OriginY { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? TransparencyIndex { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ImageFileName { get; set; }
    }
}
