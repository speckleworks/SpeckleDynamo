using System;
using System.Windows.Controls;
using Dynamo.Wpf.Extensions;

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
    private MenuItem speckleMenu;
    private MenuItem speckleAccountsMenu;
    private MenuItem speckleNodesSender;
    private MenuItem speckleNodesReceiver;


    public void Dispose()
    {
    }

    public void Startup(ViewStartupParams p)
    {

    }

    public void Loaded(ViewLoadedParams p)
    {
      speckleMenu = new MenuItem { Header = "Speckle" };
      speckleAccountsMenu = new MenuItem { Header = "Manage Accounts" };
      speckleNodesSender = new MenuItem { Header = "Send Nodes" };
      speckleNodesReceiver = new MenuItem { Header = "Receive Nodes" };

      //accounts
      speckleAccountsMenu.Click += (sender, args) =>
      {
              //var viewModel = new SampleWindowViewModel(p);
              var window = new SpecklePopup.MainWindow(false)
        {
          Owner = p.DynamoWindow
        };
        window.Left = window.Owner.Left + 400;
        window.Top = window.Owner.Top + 200;
        window.Show();
      };

      //sender
      speckleNodesSender.Click += (sender, args) =>
      {
        var viewModel = new SenderViewModel(p);
        var window = new Sender()
        {
          Owner = p.DynamoWindow,

        };
        window.DataContext = viewModel;
        window.Left = window.Owner.Left + 400;
        window.Top = window.Owner.Top + 200;
        window.ForceSend.Click += viewModel.Send_Click;
        window.Show();
      };

      //receiver
      speckleNodesReceiver.Click += (receiver, args) =>
      {
        var viewModel = new ReceiverViewModel(p);
        var window = new Receiver()
        {
          Owner = p.DynamoWindow,      
        };
        window.DataContext = viewModel;
        window.Left = window.Owner.Left;
        window.Top = window.Owner.Top + 200;
        window.StreamChanged += viewModel.StreamChanged;
        window.Show();
      };

      speckleMenu.Items.Add(speckleAccountsMenu);
      speckleMenu.Items.Add(speckleNodesSender);
      speckleMenu.Items.Add(speckleNodesReceiver);
      p.dynamoMenu.Items.Insert(p.dynamoMenu.Items.Count-1,speckleMenu);
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
