using Dynamo.Wpf.Extensions;
using SpeckleDynamoExtension.ViewModels;
using SpeckleDynamoExtension.Windows;
using System;
using System.Windows.Controls;

namespace SpeckleDynamoExtension
{
  /// <summary>
  /// The View Extension framework for Dynamo allows you to extend
  /// the Dynamo UI by registering custom MenuItems. A ViewExtension has 
  /// two components, an assembly containing a class that implements 
  /// IViewExtension, and an ViewExtensionDefintion xml file used to 
  /// instruct Dynamo where to find the class containing the
  /// IViewExtension implementation. The ViewExtensionDefinition xml file must
  /// be located in your [dynamo]\viewExtensions folder.
  /// 
  /// This sample demonstrates an IViewExtension implementation which 
  /// shows a modeless window when its MenuItem is clicked. 
  /// The Window created tracks the number of nodes in the current workspace, 
  /// by handling the workspace's NodeAdded and NodeRemoved events.
  /// </summary>
  public class SpeckleDynamoExtension : IViewExtension
  {

    private NodeManager _nodeManager = null;

    public void Dispose()
    {
    }

    public void Startup(ViewStartupParams p)
    {

    }

    public void Loaded(ViewLoadedParams viewLoadedParams)
    {
      //new node manager
      var nodeManagerViewModel = new NodeManagerViewModel(viewLoadedParams);
      _nodeManager = new NodeManager
      {
        Owner = viewLoadedParams.DynamoWindow,
        DataContext = nodeManagerViewModel
      };

      var speckleMenu = new MenuItem { Header = "Speckle" };

      var speckleAccountsMenu = new MenuItem { Header = "Manage Accounts" };
      var speckleNodeManagerMenu = new MenuItem { Header = "Manage Speckle Nodes" };
      var speckleSendReceiveNodesMenu = new MenuItem { Header = "Send/Receive nodes (experimental)" };
      

      var speckleNodeSender = new MenuItem { Header = "Node Sender" };
      var speckleNodeReceiver = new MenuItem { Header = "Node Receiver" };

      //accounts
      speckleAccountsMenu.Click += (sender, args) =>
      {
        //var viewModel = new SampleWindowViewModel(p);
        var window = new SpecklePopup.SignInWindow()
        {
          Owner = viewLoadedParams.DynamoWindow,
          WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        };
        window.Show();
      };

      //node manager
      speckleNodeManagerMenu.Click += (sender, args) =>
      {
        _nodeManager.Show();
      };

      //node sender click
      speckleNodeSender.Click += (sender, args) =>
      {
        var viewModel = new SenderViewModel(viewLoadedParams);
        var window = new Sender()
        {
          Owner = viewLoadedParams.DynamoWindow,
          DataContext = viewModel
      };
        window.ForceSend.Click += viewModel.Send_Click;
        window.Show();
      };

      //node receiver click
      speckleNodeReceiver.Click += (receiver, args) =>
      {
        var viewModel = new ReceiverViewModel(viewLoadedParams);
        var window = new Receiver()
        {
          Owner = viewLoadedParams.DynamoWindow,
          DataContext = viewModel,
        };
        window.StreamChanged += viewModel.StreamChanged;
        window.Show();
      };



      //sub menus
      speckleSendReceiveNodesMenu.Items.Add(speckleNodeReceiver);
      speckleSendReceiveNodesMenu.Items.Add(speckleNodeSender);

      //top level menus
      speckleMenu.Items.Add(speckleAccountsMenu);
      speckleMenu.Items.Add(speckleNodeManagerMenu);
      speckleMenu.Items.Add(speckleSendReceiveNodesMenu);

      viewLoadedParams.dynamoMenu.Items.Insert(viewLoadedParams.dynamoMenu.Items.Count - 1, speckleMenu);
    }

    public void Shutdown()
    {
    }

    public string UniqueId
    {
      get
      {
        return Guid.NewGuid().ToString();
      }
    }

    public string Name
    {
      get
      {
        return "Speckle Accounts Extension";
      }
    }

  }
}
