using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using System.Collections.Generic;
using SpeckleCore;
using System.Threading;
using System.Linq;
using System;
using System.Windows;
using System.ComponentModel;

namespace SpeckleDynamo
{

  [NodeName("Speckle Receiver")]
  [NodeDescription("Receives data from Speckle.")]
  [NodeCategory("Speckle.IO")]

  //Inputs
  [InPortNames("ID")]
  [InPortTypes("string")]
  [InPortDescriptions("Stream ID")]

  [IsDesignScriptCompatible]
  public class Receiver : NodeModel, INotifyPropertyChanged
  {
    private string _authToken;
    private string _restApi;
    private string _email;
    private string _server;
    private string _streamId;
    private string _oldStreamId;
    private string _message = "Initialising...";
    private bool _paused = false;

    private int elCount = 0;
    private int subsetCount = 0;
    private List<object> subset = new List<object>();

    private bool _registeringPorts = false;

    public string AuthToken { get => _authToken; set { _authToken = value; NotifyPropertyChanged("AuthToken"); } }
    public string RestApi { get => _restApi; set { _restApi = value; NotifyPropertyChanged("RestApi"); } }
    public string Email { get => _email; set { _email = value; NotifyPropertyChanged("Email"); } }
    public string Server { get => _server; set { _server = value; NotifyPropertyChanged("Server"); } }
    public string StreamId { get => _streamId; set
      { _streamId = value; NotifyPropertyChanged("StreamId");
      } }

    public string Message { get => _message; set { _message = value; NotifyPropertyChanged("Message"); } }
    public bool Paused { get => _paused; set { _paused = value; NotifyPropertyChanged("Paused"); NotifyPropertyChanged("Receiving"); } }
    //could instead use another value converter
    public bool Receiving { get => !_paused; }
    internal bool Expired = false;

    internal SpeckleApiClient myReceiver;
    List<Layer> Layers;
    List<SpeckleObject> SpeckleObjects;
    List<object> ConvertedObjects;

    private readonly SynchronizationContext _context;
    private bool hasNewData = false;


    private Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();

    public Receiver()
    {
      var hack = new ConverterHack();

      RegisterAllPorts();

      //for handling code execution on main thread, better ideas welcome
      _context = SynchronizationContext.Current;
    }

    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!hasNewData || _registeringPorts)
        return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
      else
      {
        Message = "Data received\n@" + DateTime.Now.ToString("HH:mm:ss");
        if (Layers == null || ConvertedObjects.Count == 0)
          return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };

        var associativeNodes = new List<AssociativeNode>();
        foreach (Layer layer in Layers)
        {

          subset = ConvertedObjects.GetRange((int)layer.StartIndex, (int)layer.ObjectCount);
          if (layer.Topology == "")
          {
            Functions.SpeckleTempData.AddLayerObjects(StreamId + layer.Guid, subset);
          }

          else
          {
            //HIC SVNT DRACONES
            var tree = new List<object>();
            var treeTopo = layer.Topology.Split(' ').Where(x => x != "");
            subsetCount = 0;
            foreach (var branch in treeTopo)
            {

              var branchTopo = branch.Split('-')[0].Split(';');
              var branchIndexes = new List<int>();
              foreach (var t in branchTopo)
                branchIndexes.Add(Convert.ToInt32(t));

              elCount = Convert.ToInt32(branch.Split('-')[1]);

              RecursivelyCreateNestedLists(tree, branchIndexes, 0);

            }

            object output;

            //if only one branch simplify output structure
            if (treeTopo.Count() == 1)
            {
              //if only one item simplify output structure
              if (tree[0] is List<object> && (tree[0] as List<object>).Count == 1)
                output = (tree[0] as List<object>)[0];
              else
                output = tree[0];
            }
             
            else
              output = tree;

            Functions.SpeckleTempData.AddLayerObjects(StreamId + layer.Guid, output);

          }



          var functionCall = AstFactory.BuildFunctionCall(
           new Func<string, object>(Functions.Functions.SpeckleOutput),
           new List<AssociativeNode>
           {
                AstFactory.BuildStringNode(StreamId+layer.Guid)
           });

          associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex((int)layer.OrderIndex), functionCall));

        }

        return associativeNodes;
      }
    }

    private void RecursivelyCreateNestedLists(List<object> tree, List<int> branchIndexes, int currentIndexPosition)
    {
      try
      {
        var index = branchIndexes[currentIndexPosition];
        //add missing branches
        while (tree.Count <= index || !(tree.ElementAt(index) is List<object>))
        {
          if (tree.Count > index)
            tree.Insert(index, new List<object>());
          else
            tree.Add(new List<object>());
        }
        //when reaching the end of the path just add the elements
        if (branchIndexes.Count == currentIndexPosition + 1)
        {
          for (int i = 0; i < elCount; i++)
            (tree[index] as List<object>).Add(subset[subsetCount + i]);
          subsetCount += elCount;


          return;
        }
        RecursivelyCreateNestedLists((tree[index] as List<object>), branchIndexes, currentIndexPosition + 1);

      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

    }

    public virtual void UpdateGlobal()
    {

      var getStream = myReceiver.StreamGetAsync(myReceiver.StreamId, null);
      getStream.Wait();

      NickName = getStream.Result.Resource.Name;
      Layers = getStream.Result.Resource.Layers.ToList();

      // TODO: Implement cache
      // we can safely omit the displayValue, since this is rhino!
      Message = "Getting objects";

      var payload = getStream.Result.Resource.Objects.Where(o => !ObjectCache.ContainsKey(o._id)).Select(obj => obj._id).ToArray();

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

        if (ConvertedObjects.Count != SpeckleObjects.Count)
        {
          Console.WriteLine("Some objects failed to convert.");
        }

        this.Message = "Updating...";

        _context.Post(UpdateOutputStructure, "");
        Message = "Got data\n@" + DateTime.Now.ToString("hh:mm:ss");
        //expire node on main thread
        hasNewData = true;
        _context.Post(ExpireNode, "");

      });
    }

    public virtual void UpdateMeta()
    {
      var result = myReceiver.StreamGetAsync(myReceiver.StreamId, "fields=name,layers").Result;

      NickName = result.Resource.Name;
      Layers = result.Resource.Layers.ToList();
      //run on main thread
      _context.Post(UpdateOutputStructure, "");
    }

    public virtual void UpdateChildren()
    {
      var result = myReceiver.StreamGetAsync(myReceiver.StreamId, "fields=children").Result;
      myReceiver.Stream.Children = result.Resource.Children;
    }

    public void UpdateOutputStructure()
    {
      List<Layer> toRemove, toAdd, toUpdate;
      toRemove = new List<Layer>();
      toAdd = new List<Layer>();
      toUpdate = new List<Layer>();

      Layer.DiffLayerLists(GetLayers(), Layers, ref toRemove, ref toAdd, ref toUpdate);

      foreach (Layer layer in toRemove)
      {
        var myparam = OutPortData.FirstOrDefault(item => { return item.NickName == layer.Name; });

        if (myparam != null)
          OutPortData.Remove(myparam);
      }

      foreach (var layer in toAdd)
      {
        OutPortData.Add(getGhParameter(layer));
      }

      foreach (var layer in toUpdate)
      {
        var myparam = OutPortData.FirstOrDefault(item => { return item.ToolTipString == layer.Guid; });
        myparam.NickName = layer.Name;
      }

      _registeringPorts = true;
      RegisterOutputPorts();
      ValidateConnections();
      _registeringPorts = false;
    }

    public List<Layer> GetLayers()
    {
      List<Layer> layers = new List<Layer>();
      int startIndex = 0;
      int count = 0;
      foreach (var myParam in OutPortData)
      {
        // NOTE: For gh receivers, we store the original guid of the sender component layer inside the parametr name.
        Layer myLayer = new Layer(
            myParam.NickName,
            myParam.ToolTipString,
            "",  //todo: check this
            0, //todo: check this
            startIndex,
            count);

        layers.Add(myLayer);
        // startIndex += myParam.VolatileDataCount;
        count++;
      }
      return layers;
    }


    private PortData getGhParameter(Layer param)
    {
      //guid stored in tooltip!
      PortData newParam = new PortData(param.Name, param.Guid);
      return newParam;
    }

    public void UpdateOutputStructure(object o)
    {
      UpdateOutputStructure();
    }
    private void ExpireNode(object o)
    {
      ExpireNode();
    }
    public void ExpireNode()
    {
      OnNodeModified(true);
    }

    internal void InitReceiverEventsAndGlobals()
    {
      ObjectCache = new Dictionary<string, SpeckleObject>();

      SpeckleObjects = new List<SpeckleObject>();

      ConvertedObjects = new List<object>();

      myReceiver.OnReady += (sender, e) =>
      {
        UpdateGlobal();
      };

      myReceiver.OnWsMessage += OnWsMessage;

      myReceiver.OnError += (sender, e) =>
      {
        if (e.EventName == "websocket-disconnected")
          return;
        throw new WarningException(e.EventName + ": " + e.EventData);
      };

    }

    internal void Stream_LostFocus(object sender, RoutedEventArgs e)
    {
      if (StreamId == _oldStreamId)
        return;
      _oldStreamId = StreamId;
      //TODO: check integrity of stream id? Maybe length comparison?
      Console.WriteLine("Changing streams...");

      if (myReceiver != null)
        myReceiver.Dispose(true);

      myReceiver = new SpeckleApiClient(RestApi, true);

      InitReceiverEventsAndGlobals();

      //TODO: get documentname and guid, not sure how... Maybe with an extension?
      myReceiver.IntializeReceiver(StreamId, "none", "Dynamo", "none", AuthToken);


    }

    internal void PromptAccountSelection(object sender, System.EventArgs e)
    {
      var myForm = new SpecklePopup.MainWindow();
      myForm.Owner = Application.Current.MainWindow;
      Application.Current.Dispatcher.BeginInvoke((Action)(() =>
      {
        //if default account exists form is closed automatically
        if (myForm.IsActive)
          myForm.ShowDialog();
        if (myForm.restApi != null && myForm.apitoken != null)
        {
          Email = myForm.selectedEmail;
          Server = myForm.selectedServer;

          RestApi = myForm.restApi;
          AuthToken = myForm.apitoken;

          Message = "";
        }
        else
        {
          Message = "Account selection failed.";
          return;
        }
      }));


    }

    internal void PausePlayButtonClick(object sender, RoutedEventArgs e)
    {
      Paused = !Paused;
      //if there's new data, get it on resume
      if (Expired && !Paused)
      {
        Expired = false;
        //TODO: instead, we could store it in a local cache and release it
        UpdateGlobal();
      }
    }

    public virtual void OnWsMessage(object source, SpeckleEventArgs e)
    {
      if (Paused)
      {
        Message = "Update available since " + DateTime.Now;
        Expired = true;
        return;
      }

      switch ((string)e.EventObject.args.eventType)
      {
        case "update-global":
          UpdateGlobal();
          break;
        case "update-meta":
          UpdateMeta();
          break;
        case "update-name":
          UpdateMeta();
          break;
        case "update-children":
          UpdateChildren();
          break;
        default:
          //CustomMessageHandler((string)e.EventObject.args.eventType, e);
          break;
      }
    }

    public override void Dispose()
    {
      if (myReceiver != null)
        myReceiver.Dispose();
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
