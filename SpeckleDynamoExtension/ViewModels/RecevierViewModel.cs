using Dynamo.Core;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Newtonsoft.Json;
using SpeckleCore;
using SpeckleDynamoConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpeckleDynamoExtension
{
  public class ReceiverViewModel : NotificationObject, IDisposable
  {
    private ViewLoadedParams readyParams;

    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _streamId;
    private string _oldStreamId;
    private bool _transmitting = false;
    private string _message = "";

    private string _documentName = "none";
    private string _documentGuid = "none";


    public string RestApi { get => _restApi; set { _restApi = value; RaisePropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; RaisePropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; RaisePropertyChanged("Server"); } }
    public string StreamId { get => _streamId; set { _streamId = value; RaisePropertyChanged("StreamId"); } }
    public bool Transmitting { get => _transmitting; set { _transmitting = value; RaisePropertyChanged("Transmitting"); } }

    internal string AuthToken { get => _authToken; set { _authToken = value; RaisePropertyChanged("AuthToken"); } }
    public string Message { get => _message; set { _message = value; RaisePropertyChanged("Message"); } }

    public object CurrentWorkspace { get; private set; }

    public SpeckleApiClient myReceiver;
    private List<object> ConvertedObjects;
    private List<SpeckleObject> SpeckleObjects;
    private Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();


    public ReceiverViewModel(ViewLoadedParams p)
    {
      readyParams = p;

      var myForm = new SpecklePopup.MainWindow();

      //if default account exists form is closed automatically
      if (!myForm.HasDefaultAccount)
        myForm.ShowDialog();
      if (myForm.restApi != null && myForm.apitoken != null)
      {
        myReceiver = new SpeckleApiClient(myForm.restApi);

        Email = myForm.selectedEmail;
        Server = myForm.selectedServer;

        RestApi = myForm.restApi;
        AuthToken = myForm.apitoken;
      }
      else
      {
        Message = "Account selection failed.";
        Transmitting = false;
      }


    }

    internal void StreamChanged()
    {
      if (StreamId == _oldStreamId)
        return;
      Message = "Initializing...";
      Transmitting = true;
      _oldStreamId = StreamId;


      if (myReceiver != null)
        myReceiver.Dispose(true);

      if (StreamId == "")
      {
        ConvertedObjects = new List<object>();
        Message = "";
        Transmitting = false;
        return;
      }

      myReceiver = new SpeckleApiClient(RestApi, true);

      InitReceiverEventsAndGlobals();
      myReceiver.IntializeReceiver(StreamId, _documentName, "Dynamo", _documentGuid, AuthToken);
    }


    public virtual void OnWsMessage(object source, SpeckleEventArgs e)
    {
      //node disconnected before event was received
      if (string.IsNullOrEmpty(StreamId))
        return;
      Transmitting = true;
      switch ((string)e.EventObject.args.eventType)
      {
        case "update-global":
          UpdateGlobal();
          break;
        default:
          //CustomMessageHandler((string)e.EventObject.args.eventType, e);
          break;
      }
    }


    internal void InitReceiverEventsAndGlobals()
    {
      ObjectCache = new Dictionary<string, SpeckleObject>();

      SpeckleObjects = new List<SpeckleObject>();

      ConvertedObjects = new List<object>();

      if (myReceiver.IsConnected)
        UpdateGlobal();
      else
        myReceiver.OnReady += (sender, e) =>
        {
          UpdateGlobal();
        };

      myReceiver.OnWsMessage += OnWsMessage;

      myReceiver.OnError += (sender, e) =>
      {
        if (e.EventName == "websocket-disconnected")
          return;
        Message = e.EventName + ": " + e.EventData;
      };

    }


    public virtual void UpdateGlobal()
    {
      var getStream = myReceiver.StreamGetAsync(myReceiver.StreamId, null);
      getStream.Wait();

      Message = "Getting objects";



      var payload = getStream.Result.Resource.Objects.Select(obj => obj._id).ToArray(); //.Where(o => !ObjectCache.ContainsKey(o._id)).


      myReceiver.ObjectGetBulkAsync(payload, "omit=displayValue").ContinueWith(tres =>
      {
        //add to cache
        foreach (var x in tres.Result.Resources)
          ObjectCache[x._id] = x;

        // populate real objects
        SpeckleObjects.Clear();
        foreach (var obj in getStream.Result.Resource.Objects)
          SpeckleObjects.Add(ObjectCache[obj._id]);

        this.Message = "Converting objects";
        ConvertedObjects = SpeckleCore.Converter.Deserialise(SpeckleObjects);


        if (ConvertedObjects.Count == 0)
        {
          this.Message = "";
          Transmitting = false;
        }
        else
          this.Message = "Updating...";


        //add remove nodes/connectors
        foreach (var obj in ConvertedObjects)
        {
          if (obj is SpeckleNodeEvent)
          {
            var specklenodeevent = obj as SpeckleNodeEvent;
            //needs a dispatcher
            readyParams.DynamoWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
              var dynViewModel = readyParams.DynamoWindow.DataContext as DynamoViewModel;
              var dm = dynViewModel.Model as DynamoModel;

              var engine = dm.EngineController;
              var logger = engine != null ? engine.AsLogger() : null;

              var settings = new JsonSerializerSettings
              {
                Error = (sender, args) =>
                {
                  args.ErrorContext.Handled = true;
                  Console.WriteLine(args.ErrorContext.Error);
                },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented,
                Culture = CultureInfo.InvariantCulture,
                Converters = new List<JsonConverter>{
                    new ConnectorConverter(dm.Logger),
                        new WorkspaceReadConverter(dm.EngineController, dm.Scheduler, dm.NodeFactory, false, false),
                        new NodeReadConverter(dm.CustomNodeManager, dm.LibraryServices, false),
                        new TypedParameterConverter()
                  },
                ReferenceResolverProvider = () => { return new IdReferenceResolver(); }
              };

              var result = ReplaceTypeDeclarations(specklenodeevent.Json, true);
              var workspace = JsonConvert.DeserializeObject<SpeckleDynamoWorkspace>(result, settings);

              for (var i = 0; i < workspace.Nodes.Count; i++)
              {
                var node = workspace.Nodes[i];
                var nodeView = workspace.View.NodeViews.ElementAt(i);
                dm.ExecuteCommand(new DynamoModel.CreateNodeCommand(node, nodeView.X, nodeView.Y, false, false));
              }

              foreach (var conn in workspace.Connectors)
              {
               
                  dm.ExecuteCommand(new DynamoModel.MakeConnectionCommand(conn.Start.Owner.GUID.ToString(), conn.Start.Index, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
                  dm.ExecuteCommand(new DynamoModel.MakeConnectionCommand(conn.End.Owner.GUID.ToString(), conn.End.Index, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
                
              }

              this.Message = "Got data\n@" + DateTime.Now.ToString("HH:mm:ss");
              Transmitting = false;
            }));

          }
          else
          {
            this.Message = "Got data\n@" + DateTime.Now.ToString("HH:mm:ss");
            Transmitting = false;
          }
        }


      });




    }

    /// <summary>
    /// Strips $type references from the generated json, replacing them with 
    /// type names matching those expected by the server.
    /// </summary>
    /// <param name="json">The json to parse.</param>
    /// <param name="fromServer">A flag indicating whether this json is coming from the server, and thus
    /// needs to be converted back to its Json.net friendly format.</param>
    /// <returns></returns>
    internal static string ReplaceTypeDeclarations(string json, bool fromServer = false)
    {
      var result = json;

      if (fromServer)
      {
        var rgx2 = new Regex(@"ConcreteType");
        result = rgx2.Replace(result, "$type");
      }
      else
      {
        var rgx2 = new Regex(@"\$type");
        result = rgx2.Replace(result, "ConcreteType");
      }

      return result;
    }

    public void Dispose()
    {
      if (myReceiver != null)
        myReceiver.Dispose();
    }
  }

}
