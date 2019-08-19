using Dynamo.Core;
using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Newtonsoft.Json;
using SpeckleCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;

namespace SpeckleDynamoExtension
{
  public class SenderViewModel : NotificationObject, IDisposable
  {
    private ViewLoadedParams readyParams;

    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _streamId;
    private bool _transmitting = true;
    private string _message = "Initialising...";

    private string _documentName = "none";
    private string _documentGuid = "none";
    private Timer DataSender;

    public string RestApi { get => _restApi; set { _restApi = value; RaisePropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; RaisePropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; RaisePropertyChanged("Server"); } }
    public string StreamId { get => _streamId; set { _streamId = value; RaisePropertyChanged("StreamId"); } }
    public bool Transmitting { get => _transmitting; set { _transmitting = value; RaisePropertyChanged("Transmitting"); } }

    internal string AuthToken { get => _authToken; set { _authToken = value; RaisePropertyChanged("AuthToken"); } }
    public string Message { get => _message; set { _message = value; RaisePropertyChanged("Message"); } }

    public SpeckleApiClient mySender;
    private List<Layer> BucketLayers = new List<Layer>();
    private List<object> BucketObjects = new List<object>();


    public SenderViewModel(ViewLoadedParams p)
    {
      readyParams = p;

      //account/form flow
      Account account = null;
      LocalContext.Init();
      try
      {
        //try getting default account, exception is thrownif none is set
        account = LocalContext.GetDefaultAccount();
      }
      catch (Exception ex)
      {
      }

      //show account selection window
      if (account == null)
      {
        //open window with isPopUp=true
        var signInWindow = new SpecklePopup.SignInWindow(true);
        signInWindow.ShowDialog();

        if (signInWindow.AccountListBox.SelectedIndex != -1)
        {
          account = signInWindow.accounts[signInWindow.AccountListBox.SelectedIndex];
        }
      }

      if (account != null)
      {
        mySender = new SpeckleApiClient(account.RestApi);

        Email = account.Email;
        Server = account.ServerName;

        RestApi = account.RestApi;
        AuthToken = account.Token;

        InitializeSender();
      }
      else
      {
        Message = "Account selection failed.";
        Transmitting = false;
      }
    }

    internal void Send_Click(object sender, RoutedEventArgs e)
    {
      Transmitting = true;
      Message = "Sending...";

      var dynViewModel = readyParams.DynamoWindow.DataContext as DynamoViewModel;
      var dm = dynViewModel.Model as DynamoModel;

      var sel = readyParams.CurrentWorkspaceModel.CurrentSelection;

      BucketObjects = new List<object>();

      var engine = dm.EngineController;
      var logger = engine != null ? engine.AsLogger() : null;
      var settings = new JsonSerializerSettings
      {
        Error = (sender2, args) =>
        {
          args.ErrorContext.Handled = true;
          Console.WriteLine(args.ErrorContext.Error);
        },
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Newtonsoft.Json.Formatting.Indented,
        Culture = CultureInfo.InvariantCulture,
        Converters = new List<JsonConverter>
        {
          new ConnectorConverter(null),
          new WorkspaceWriteConverter(null),
          new DummyNodeWriteConverter(),
          new TypedParameterConverter()
        },
        ReferenceResolverProvider = () => { return new IdReferenceResolver(); }
      };

      List<ConnectorModel> connectors = new List<ConnectorModel>();

      //add all the connectors in use just once
      foreach (var node in sel)
      {
        foreach (var conn in node.AllConnectors)
        {
          if (!connectors.Any(x => x.GUID == conn.GUID) && sel.Any(x => x.GUID == conn.Start.Owner.GUID) && sel.Any(x => x.GUID == conn.End.Owner.GUID))
            connectors.Add(conn);
        }
      }

      // using same structure of a WorkspaceModel to reuse built in dynamo json converters
      var workspace = new SpeckleDynamoWorkspace { Nodes = sel.ToList(), Connectors = connectors };
      workspace.View = new ExtraWorkspaceViewInfo
      {
        NodeViews = workspace.Nodes.Select(n => new ExtraNodeViewInfo
        {
          Id = n.GUID.ToString(),
          ShowGeometry = n.IsVisible,
          Excluded = n.IsFrozen,
          IsSetAsInput = n.IsSetAsInput,
          IsSetAsOutput = n.IsSetAsOutput,
          Name = n.Name,
          X = n.Position.X,
          Y = n.Position.Y
        }).ToList(),
        X = dynViewModel.CurrentSpaceViewModel.X,
        Y = dynViewModel.CurrentSpaceViewModel.Y,
        Zoom = dynViewModel.CurrentSpaceViewModel.Zoom
      };

      //sending the above as a string to use custom serialization settings
      //there might be a way to do that with native speckleabstracts
      var json = JsonConvert.SerializeObject(workspace, settings);
      var result = ReplaceTypeDeclarations(json);
      BucketObjects.Add(new SpeckleNodeEvent { Json = result });
      UpdateData();
    }



    private void InitializeSender()
    {
      mySender.IntializeSender(AuthToken, _documentName, "Dynamo", _documentGuid).ContinueWith(task =>
      {
          // ExpireNode();
        });


      mySender.OnReady += (sender, e) =>
      {
        StreamId = mySender.StreamId;
        this.Message = "";
        Transmitting = false;
      };

      mySender.OnError += (sender, e) =>
      {
        if (e.EventName == "websocket-disconnected")
          return;
        Message = e.EventName + ": " + e.EventData;
      };

      DataSender = new System.Timers.Timer(2000) { AutoReset = false, Enabled = false };
      DataSender.Elapsed += DataSender_Elapsed;
    }

    public void UpdateData()
    {
      BucketLayers = new List<Layer>{
      new Layer(
           "nodes",
           Guid.NewGuid().ToString(),
           "",
           1,
           0,
           0) };
      DataSender.Start();
    }

    private void DataSender_Elapsed(object sender, ElapsedEventArgs e)
    {
      var convertedObjects = Converter.Serialise(BucketObjects).ToList();

      this.Message = String.Format("Creating payloads");

      long totalBucketSize = 0;
      long currentBucketSize = 0;
      List<List<SpeckleObject>> objectUpdatePayloads = new List<List<SpeckleObject>>();
      List<SpeckleObject> currentBucketObjects = new List<SpeckleObject>();
      List<SpeckleObject> allObjects = new List<SpeckleObject>();

      foreach (SpeckleObject convertedObject in convertedObjects)
      {
        long size = Converter.getBytes(convertedObject).Length;
        currentBucketSize += size;
        totalBucketSize += size;
        currentBucketObjects.Add(convertedObject);

        if (currentBucketSize > 5e5) // restrict max to ~500kb; should it be user config? anyway these functions should go into core. at one point. 
        {
          Console.WriteLine("Reached payload limit. Making a new one, current  #: " + objectUpdatePayloads.Count);
          objectUpdatePayloads.Add(currentBucketObjects);
          currentBucketObjects = new List<SpeckleObject>();
          currentBucketSize = 0;
        }
      }

      // add  the last bucket 
      if (currentBucketObjects.Count > 0)
        objectUpdatePayloads.Add(currentBucketObjects);

      Console.WriteLine("Finished, payload object update count is: " + objectUpdatePayloads.Count + " total bucket size is (kb) " + totalBucketSize / 1000);

      if (objectUpdatePayloads.Count > 100)
      {
        Message = "This is a humongous update, in the range of ~50mb. For now, create more streams instead of just one massive one! Updates will be faster and snappier, and you can combine them back together at the other end easier.";
      }

      int k = 0;
      List<ResponseObject> responses = new List<ResponseObject>();
      foreach (var payload in objectUpdatePayloads)
      {
        this.Message = String.Format("Sending payload\n{0} / {1}", k++, objectUpdatePayloads.Count);

        responses.Add(mySender.ObjectCreateAsync(payload).GetAwaiter().GetResult());
      }

      this.Message = "Updating stream...";

      // create placeholders for stream update payload
      List<SpeckleObject> placeholders = new List<SpeckleObject>();
      foreach (var myResponse in responses)
        foreach (var obj in myResponse.Resources) placeholders.Add(new SpecklePlaceholder() { _id = obj._id });

      SpeckleStream updateStream = new SpeckleStream()
      {
        Layers = BucketLayers,
        Name = "Node Sender",
        Objects = placeholders
      };

      var response = mySender.StreamUpdateAsync(mySender.StreamId, updateStream).Result;

      mySender.BroadcastMessage("stream", mySender.StreamId, new { eventType = "update-global" });

      Message = "Data sent\n@" + DateTime.Now.ToString("HH:mm:ss");
      Transmitting = false;

    }

    public void Dispose()
    {
      //readyParams.CurrentWorkspaceModel.NodeAdded -= CurrentWorkspaceModel_NodeAdded;
      //readyParams.CurrentWorkspaceModel.NodeRemoved -= CurrentWorkspaceModel_NodeRemoved;
      //readyParams.CurrentWorkspaceModel.ConnectorAdded -= CurrentWorkspaceModel_ConnectorAdded;
      //readyParams.CurrentWorkspaceModel.ConnectorDeleted -= CurrentWorkspaceModel_ConnectorDeleted;

      if (mySender != null)
        mySender.Dispose();
    }


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
  }

  public class SpeckleNodeEvent
  {
    public string Json { get; set; }
  }
}
