using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;

namespace SpeckleDynamo.Utils
{
  public class ObservableCollectionEx<T> : ObservableCollection<T> where T : INotifyPropertyChanged
  {
    // this collection also reacts to changes in its components' properties

    public ObservableCollectionEx() : base()
    {
      this.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ObservableCollectionEx_CollectionChanged);
    }

    void ObservableCollectionEx_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      if (e.Action == NotifyCollectionChangedAction.Remove)
      {
        foreach (T item in e.OldItems)
        {
          //Removed items
          item.PropertyChanged -= EntityViewModelPropertyChanged;
        }
      }
      else if (e.Action == NotifyCollectionChangedAction.Add)
      {
        foreach (T item in e.NewItems)
        {
          //Added items
          item.PropertyChanged += EntityViewModelPropertyChanged;
        }
      }
    }

    public void EntityViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      //This will get called when the property of an object inside the collection changes - note you must make it a 'reset' - dunno why
      NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
      OnCollectionChanged(args);
    }
  }
}
