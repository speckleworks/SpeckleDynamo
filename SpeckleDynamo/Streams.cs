extern alias DynamoNewtonsoft;
using DNJ = DynamoNewtonsoft::Newtonsoft.Json;

using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
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


    [DNJ.JsonIgnore]
    public ObservableCollection<SpeckleStream> UserStreams { get => _userStreams; set { _userStreams = value; NotifyPropertyChanged("UserStreams"); } }
    [DNJ.JsonIgnore]
    List<SpeckleStream> SharedStreams = new List<SpeckleStream>();
    [DNJ.JsonIgnore]
    [DNJ.JsonConverter(typeof(SpeckleClientConverter))]
    SpeckleApiClient Client;


    #endregion


    [DNJ.JsonConstructor]
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
      //caching streams
      if(DateTime.Now.Subtract(Globals.LastCheckedStreams).Seconds < 10)
      {
        UserStreams.Clear();
        UserStreams.AddRange(Globals.UserStreams);
        Transmitting = false;
        return;
      }

      Client.BaseUrl = RestApi;
      Client.AuthToken = AuthToken;
      Client.StreamsGetAllAsync("fields=streamId,name").ContinueWith(tsk =>
      {
        DispatchOnUIThread(() =>
        {
          UserStreams.Clear();
          UserStreams.AddRange(tsk.Result.Resources.ToList());
          Transmitting = false;
          Globals.LastCheckedStreams = DateTime.Now;
          Globals.UserStreams = UserStreams;
        });
      });
    }




    public override void Dispose()
    {
      if (Client != null)
        Client.Dispose();
      base.Dispose();
    }

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
