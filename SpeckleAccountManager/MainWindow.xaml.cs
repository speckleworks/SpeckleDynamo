using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace SpecklePopup
{
  public partial class MainWindow : Window
  {
    string serverName;
    public string restApi;
    public string apitoken;

    public string selectedEmail;
    public string selectedServer;
    public bool HasDefaultAccount = false;


    public MainWindow(bool isPopup = true)
    {
      InitializeComponent();

      //only visible in popupmode
      if (isPopup)
      {
        AccountsControl.ButonUseSelected.Visibility = Visibility.Visible;
        AccountsControl.ButonUseSelected.Click += ButtonUseSelected_Click;

        //skip popup if there's a default account!
        if (AccountsControl.accounts.Any(x => x.isDefault))
        {
          UseSelected(AccountsControl.accounts.First(x => x.isDefault));
          HasDefaultAccount = true;
        }
      }
      else
      {
       // AccountsControl.ButonUseSelected.Visibility = Visibility.Collapsed;
      }


      this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      this.DragRectangle.MouseDown += (sender, e) =>
      {
        this.DragMove();
      };




    }

    private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
    {
      this.DragMove();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }

    private void ButtonUseSelected_Click(object sender, RoutedEventArgs e)
    {
      if (!(AccountsControl.AccountListBox.SelectedIndex != -1))
      {
        MessageBox.Show("Please select an account first.");
        return;
      }
      UseSelected(AccountsControl.accounts[AccountsControl.AccountListBox.SelectedIndex]);
    }

    private void UseSelected(SpeckleAccount account)
    {
      restApi = account.restApi;
      apitoken = account.apiToken;
      selectedEmail = account.email;
      selectedServer = account.serverName;
      Close();
    }

  }
}
