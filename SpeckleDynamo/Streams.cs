using Dynamo.Graph;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;
using SpeckleCore;
using SpeckleDynamo.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;

namespace SpeckleDynamo
{
  [NodeName("Speckle Streams")]
  [NodeDescription("Lists your existing Speckle streams for a specified account.")]
  [NodeCategory("Speckle.I/O")]

  //Output
  [OutPortNames("ID")]
  [OutPortTypes("string")]
  [OutPortDescriptions("Stream ID")]

  [IsDesignScriptCompatible]
  public class Streams : NodeModel, INotifyPropertyChanged
  {
    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _streamId;
    private bool _transmitting = true;
    private ObservableCollection<SpeckleStream> _userStreams = new ObservableCollection<SpeckleStream>();

    internal string AuthToken { get => _authToken; set { _authToken = value; NotifyPropertyChanged("AuthToken"); } }
    #region public properties
    public string RestApi { get => _restApi; set { _restApi = value; NotifyPropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; NotifyPropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; NotifyPropertyChanged("Server"); } }

    public string StreamId
    {
      get => _streamId; set
      {
        _streamId = value;
        NotifyPropertyChanged("StreamId");
        ExpireNode();

      }
    }
    public bool Transmitting { get => _transmitting; set { _transmitting = value; NotifyPropertyChanged("Transmitting"); } }


    [JsonIgnore]
    public ObservableCollection<SpeckleStream> UserStreams { get => _userStreams; set { _userStreams = value; NotifyPropertyChanged("UserStreams"); } }
    [JsonIgnore]
    List<SpeckleStream> SharedStreams = new List<SpeckleStream>();
    [JsonIgnore]
    [JsonConverter(typeof(SpeckleClientConverter))]
    SpeckleApiClient Client;


    #endregion


    [JsonConstructor]
    private Streams(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
    }

    public Streams()
    {
      RegisterAllPorts();
    }


    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      this.ClearErrorsAndWarnings();
      if (string.IsNullOrEmpty(StreamId))
        return Enumerable.Empty<AssociativeNode>();

      return new AssociativeNode[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildStringNode(StreamId)) };
    }

    public void ExpireNode()
    {
      OnNodeModified(forceExecute: true);
    }

    internal void AddedToDocument(object sender, System.EventArgs e)
    {
      //saved receiver
      if (Client != null)
      {
        AuthToken = Client.AuthToken;
        GetStreams();
        return;
      }
      var myForm = new SpecklePopup.MainWindow(true, true);
      //TODO: fix this it's crashing revit
      //myForm.Owner = Application.Current.MainWindow;
      this.DispatchOnUIThread(() =>
      {
        //if default account exists form is closed automatically
        if (!myForm.HasDefaultAccount)
          myForm.ShowDialog();
        if (myForm.restApi != null && myForm.apitoken != null)
        {
          Email = myForm.selectedEmail;
          Server = myForm.selectedServer;

          RestApi = myForm.restApi;
          AuthToken = myForm.apitoken;

          Client = new SpeckleApiClient();
          GetStreams();

        }
        else
        {
          Error("Account selection failed.");
        }
      });
    }

    private void GetStreams()
    {
      Client.BaseUrl = RestApi;
      Client.AuthToken = AuthToken;
      Client.StreamsGetAllAsync().ContinueWith(tsk =>
      {
        DispatchOnUIThread(() =>
        {
          UserStreams.Clear();
          UserStreams.AddRange(tsk.Result.Resources.ToList());
          Transmitting = false;
        });
      });
    }




    public override void Dispose()
    {
      if (Client != null)
        Client.Dispose();
      base.Dispose();
    }



    #region Serialization/Deserialization Methods

    protected override void SerializeCore(XmlElement element, SaveContext context)
    {
      base.SerializeCore(element, context); // Base implementation must be called.
      if (Client == null)
        return;

      //https://stackoverflow.com/questions/13674395/no-map-for-object-error-when-deserializing-object
      using (var input = new MemoryStream())
      {
        var formatter = new BinaryFormatter();
        formatter.Serialize(input, Client);
        input.Seek(0, SeekOrigin.Begin);

        using (MemoryStream output = new MemoryStream())
        using (DeflateStream deflateStream = new DeflateStream(output, CompressionMode.Compress))
        {
          input.CopyTo(deflateStream);
          deflateStream.Close();

          var client = Convert.ToBase64String(output.ToArray());

          var xmlDocument = element.OwnerDocument;
          var subNode = xmlDocument.CreateElement("Speckle");
          subNode.SetAttribute("speckleclient", client);
          //could be part of the sender
          subNode.SetAttribute("stream", StreamId);
          subNode.SetAttribute("email", Email);
          subNode.SetAttribute("server", Server);
          element.AppendChild(subNode);
        }
      }
    }

    protected override void DeserializeCore(XmlElement element, SaveContext context)
    {
      base.DeserializeCore(element, context); //Base implementation must be called.

      foreach (XmlNode subNode in element.ChildNodes)
      {
        if (!subNode.Name.Equals("Speckle"))
          continue;
        if (subNode.Attributes == null || (subNode.Attributes.Count <= 0))
          continue;

        // _coldStart = true;
        foreach (XmlAttribute attr in subNode.Attributes)
        {
          switch (attr.Name)
          {
            case "speckleclient":
              using (MemoryStream input = new MemoryStream(Convert.FromBase64String(attr.Value)))
              using (DeflateStream deflateStream = new DeflateStream(input, CompressionMode.Decompress))
              using (MemoryStream output = new MemoryStream())
              {
                deflateStream.CopyTo(output);
                deflateStream.Close();
                output.Seek(0, SeekOrigin.Begin);

                BinaryFormatter bformatter = new BinaryFormatter();
                Client = (SpeckleApiClient)bformatter.Deserialize(output);
                RestApi = Client.BaseUrl;
              }
              break;
            case "stream":
              StreamId = attr.Value;
              break;
            case "email":
              Email = attr.Value;
              break;
            case "server":
              Server = attr.Value;
              break;
            default:
              Log(string.Format("{0} attribute could not be deserialized for {1}", attr.Name, GetType()));
              break;
          }
        }

        break;
      }
    }

    #endregion

    public event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(String info)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(info));
      }
    }

  }
}
