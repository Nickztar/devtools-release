using System;
using Newtonsoft.Json;

public class UpdateResult
{
    [JsonProperty("url")]
    public string Url { get; set; }
    [JsonProperty("version")]
    public string Version { get; set; }
    [JsonProperty("notes")]
    public string Notes { get; set; }
    [JsonProperty("pub_date")]
    public DateTime PublishDate { get; set; }
    [JsonProperty("signature")]
    public string Signature { get; set; }
}