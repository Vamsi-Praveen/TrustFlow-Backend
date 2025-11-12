using MongoDB.Bson.Serialization.Attributes;
using TrustFlow.Core.Models;

public class Counter : BaseEntity
{
    [BsonElement("Identifier")]
    public string Identifier { get; set; } 

    [BsonElement("Seq")]
    public int Seq { get; set; }
}
