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
    private MenuItem speckleAccountsMenu;

    public void Dispose()
    {
    }

    public void Startup(ViewStartupParams p)
    {
    }

    public void Loaded(ViewLoadedParams p)
    {
#if !DEBUG
      //check for updates in the background
      Api.Api.CheckForUpdates();
#endif

      speckleAccountsMenu = new MenuItem { Header = "Speckle Accounts" };
      speckleAccountsMenu.Click += (sender, args) =>
      {
       //var viewModel = new SampleWindowViewModel(p);
        var window = new SpecklePopup.MainWindow (false)
        {

          Owner = p.DynamoWindow
        };

        window.Left = window.Owner.Left + 400;
        window.Top = window.Owner.Top + 200;

              // Show a modeless window.
        window.Show();
      };
      p.AddMenuItem(MenuBarType.View, speckleAccountsMenu);
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
