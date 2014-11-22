using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BindingRecepies.ViewModels
{
    public abstract class ViewModelBase: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if(this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }


    public class ViewModel : ViewModelBase
    {
        public List<Recipe> Recipes { get; set; }

        public ViewModel()
        {
            this.OnPropertyChanged("Recipes");
        }
    }
}
