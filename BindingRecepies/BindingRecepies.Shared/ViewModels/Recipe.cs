using System;
using System.Collections.Generic;
using System.Text;
using Windows.Web.Http;

namespace BindingRecepies.ViewModels
{
    public class Recipe
    {
        public string Title { get; set; }

        public string Ingredients { get; set; }

        public string PreparationWay { get; set; }

        public string Time { get; set; }

        public string ImageURL { get; set; }
    }
}
