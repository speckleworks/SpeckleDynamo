extern alias DynamoNewtonsoft;
using DNJ = DynamoNewtonsoft::Newtonsoft.Json;

using Type = System.Type;
using System.Reflection;
using System.Text.RegularExpressions;
using Dynamo.Logging;
using SpeckleCore;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.Compression;

namespace SpeckleDynamo.Serialization
{
  public class SpeckleClientConverter : DNJ.JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(SpeckleApiClient);
    }

    public override object ReadJson(DNJ.JsonReader reader, Type objectType, object existingValue, DNJ.JsonSerializer serializer)
    {
      // Fixes https://github.com/speckleworks/SpeckleDynamo/issues/61
      SpeckleCore.SpeckleInitializer.Initialize();
      // Carry on as usual (NOTE: we need to clean this up)
      SpeckleApiClient client = null;
      var obj = DNJ.Linq.JValue.Load(reader);
      try
      {
        var bytes = (byte[])obj;

        if (bytes != null)
        {
          using (MemoryStream input = new MemoryStream(bytes))
          using (DeflateStream deflateStream = new DeflateStream(input, CompressionMode.Decompress))
          using (MemoryStream output = new MemoryStream())
          {
            deflateStream.CopyTo(output);
            deflateStream.Close();
            output.Seek(0, SeekOrigin.Begin);

            BinaryFormatter bformatter = new BinaryFormatter();
            client = (SpeckleApiClient)bformatter.Deserialize(output);

          }
        }
      }
      catch  { 
        // null/empty receiver  
      }
      return client;
    }

    public override void WriteJson(DNJ.JsonWriter writer, object value, DNJ.JsonSerializer serializer)
    {
      var client = (SpeckleApiClient)value;
      //cannot remove auth token from saved files as below since this method gets triggered 
      //also when moving the node on the canvas etc...
      //client.AuthToken = "";

      using (var input = new MemoryStream())
      {
        var formatter = new BinaryFormatter();
        formatter.Serialize(input, client);
        input.Seek(0, SeekOrigin.Begin);

        using (MemoryStream output = new MemoryStream())
        using (DeflateStream deflateStream = new DeflateStream(output, CompressionMode.Compress))
        {
          input.CopyTo(deflateStream);
          deflateStream.Close();

          writer.WriteValue(output.ToArray());
        }
      }
    }
  }
}
