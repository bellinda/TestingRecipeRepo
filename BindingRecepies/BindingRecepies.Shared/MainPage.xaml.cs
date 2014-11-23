using BindingRecepies.ViewModels;
using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.ApplicationModel.Background;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Facebook;
using Facebook.Client;
using System.Dynamic;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BindingRecepies
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //public static List<Recipe> recipes = new List<Recipe>();
        public static List<string> imageURLs = new List<string>();

        public static List<AddressNode> addresses = new List<AddressNode>();
        public static List<string> titles = new List<string>();
        public static List<Restaurant> restaurants = new List<Restaurant>();

        private const string dbName = "FoofThings.db";

        public List<Recipe> recipes { get; set; }

        public ViewModel viewModel { get; set; }

        public static TextBlock LocationTextBlock;

        public static Image photoPlaceholder;

        public static WebView myWebView;

        //private MediaPlayer mediaplayer;

        public static MediaCapture mediacapture = new MediaCapture();
        private static string _ExtendedPermissions;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            viewModel = new ViewModel();

            photoPlaceholder = this.PhotoPreview;

            myWebView = this.webView;

            SendNotification("No internet connection!", "Turn it on to see", "a list of restorants near you", "Images/connection.png");

            

            //SetItemSourceOfRecipeList(viewModel, listView);

            //this.DataContext = viewModel;

            //GetAllRecepiesFromHttpRequest();

            //implement the geolocation here
            //string city = "Sofia";
            GetAllReastaurantsInCurrentPlace();

            this.DataContext = viewModel;

            //this.recepiesListBox.Height = this.RenderSize.Height / 2;
            //this.recepiesListBox.Background = new SolidColorBrush(Colors.Chartreuse);
        }

        private static void SendNotification(string mainMessage, string secondMessage, string thirdMessage, string imageSrc)
        {
            var notificationXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);
            string toastXmlString = "<toast>"
                               + "<visual version='1'>"
                               + "<binding template='toastImageAndText04'>"
                               + "<text id='1'>" + mainMessage + "</text>"     //No internet connection!
                               + "<text id='2'>" + secondMessage + "</text>" //Turn it on to see 
                               + "<text id='3'>" + thirdMessage + "</text>"   //a list of restorants near you
                               + "<image id='1' src='" + imageSrc + "' alt='image placeholder'/>"
                               + "</binding>"
                               + "</visual>"
                               + "</toast>";
            notificationXml.LoadXml(toastXmlString);         //var toeastElement = notificationXml.GetElementsByTagName("text");
            //toeastElement[0].AppendChild(notificationXml.CreateTextNode("This is Notification Message"));
            var toastNotification = new ToastNotification(notificationXml);
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
        }

        public static async void SetItemSourceOfRecipeList(ViewModel viewModel, Windows.UI.Xaml.Controls.ListView listBox)
        {
            List<Recipe> recipes = await GetAllRecepiesFromHttpRequest();
            viewModel.Recipes = recipes;
            listBox.ItemsSource = viewModel.Recipes;            
        }

        public static async Task<string> GetResponseString(string url)
        {
            string recieved = "";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            using (var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)))
            {
                using (var responseStream = response.GetResponseStream())
                {
                    Encoding eCofidication = Encoding.UTF8;
                    using (var sr = new StreamReader(responseStream, eCofidication))
                    {
                        recieved = await sr.ReadToEndAsync();
                    }
                }
            }

            return recieved;
        }

        public static async Task<string> GetCurrentPlaceName()
        {
            string AppName = "TestBla";
            Geolocator Locator = new Geolocator();
            Geoposition Position = await Locator.GetGeopositionAsync();

            HttpClient Client = new HttpClient();
            string Result = await Client.GetStringAsync(new Uri(string.Format("http://nominatim.openstreetmap.org/reverse?format=xml&zoom=18&lat={0}&lon={1}&application={2}", Position.Coordinate.Latitude.ToString(CultureInfo.InvariantCulture), Position.Coordinate.Longitude.ToString(CultureInfo.InvariantCulture), AppName)));

            XDocument ResultDocument = XDocument.Parse(Result);
            XElement AddressElement = ResultDocument.Root.Element("addressparts");

            string city = "";

            if (AddressElement.Element("city") == null && Position.Coordinate.Longitude >= 27.6 && Position.Coordinate.Longitude <= 28.6)
            {
                if (AddressElement.Element("suburb") != null)
                {
                    city = AddressElement.Element("suburb").Value;
                }
                else
                {
                    city = AddressElement.Element("town").Value;
                }
            }
            else if (AddressElement.Element("city") == null)
            {
                if (AddressElement.Element("town") != null)
                {
                    city = AddressElement.Element("town").Value;
                }
                string region = AddressElement.Element("county").Value.Substring(AddressElement.Element("county").Value.IndexOf(" ") + 1);
            }
            else
            {
                if (AddressElement.Element("county").Value.Contains("София-Град"))
                {
                    city = "София";
                }
                else
                {
                    city = AddressElement.Element("city").Value;
                }
            }

            string Country = AddressElement.Element("country").Value;

            return city;
        }

        public static string ConvertCyrillicLettersIntoLatin(string expression)
        {
            if (expression == "Велико Търново")
            {
                expression = "Veliko Tarnovo";
            } else if(expression == "София")
            {
                expression = "Sofia";
            }
            else
            {
                Dictionary<string, string> letters = new Dictionary<string, string>();
                letters.Add("А", "A");
                letters.Add("Б", "B");
                letters.Add("В", "V");
                letters.Add("Г", "G");
                letters.Add("Д", "D");
                letters.Add("Е", "E");
                letters.Add("Ж", "Zh");
                letters.Add("З", "Z");
                letters.Add("И", "I");
                letters.Add("Й", "I");
                letters.Add("К", "K");
                letters.Add("Л", "L");
                letters.Add("М", "M");
                letters.Add("Н", "N");
                letters.Add("О", "O");
                letters.Add("П", "P");
                letters.Add("Р", "R");
                letters.Add("У", "U");
                letters.Add("Ф", "F");
                letters.Add("Х", "H");
                letters.Add("Ц", "Tz");
                letters.Add("Ч", "Ch");
                letters.Add("Ш", "Sh");
                letters.Add("Щ", "Sht");
                letters.Add("С", "S");
                letters.Add("Т", "T");
                letters.Add("Ю", "Ju");
                letters.Add("Я", "Ya");
                letters.Add("а", "a");
                letters.Add("б", "b");
                letters.Add("в", "v");
                letters.Add("г", "g");
                letters.Add("д", "d");
                letters.Add("е", "e");
                letters.Add("ж", "zh");
                letters.Add("з", "z");
                letters.Add("и", "i");
                letters.Add("й", "i");
                letters.Add("к", "k");
                letters.Add("л", "l");
                letters.Add("м", "m");
                letters.Add("н", "n");
                letters.Add("о", "o");
                letters.Add("п", "p");
                letters.Add("р", "r");
                letters.Add("с", "s");
                letters.Add("т", "t");
                letters.Add("у", "u");
                letters.Add("ф", "f");
                letters.Add("х", "h");
                letters.Add("ц", "tz");
                letters.Add("ч", "ch");
                letters.Add("ш", "sh");
                letters.Add("щ", "sht");
                letters.Add("ъ", "y");
                letters.Add("ю", "ju");
                letters.Add("я", "ya");

                foreach (KeyValuePair<string, string> letter in letters)
                {
                    expression = expression.Replace(letter.Key, letter.Value);
                }
            }
            expression = expression.Replace(" ", "-");
            return expression;
        }

        public static bool IsConnectedToNetwork()
        {
            bool hasConnection = true;
            hasConnection = NetworkInterface.GetIsNetworkAvailable();
            return hasConnection;
        }

        public static async void GetAllReastaurantsInCurrentPlace()
        {
            var city = await GetCurrentPlaceName();

            city = ConvertCyrillicLettersIntoLatin(city);

            string url = "http://www.restaurant.bg/restoranti/v-" + city;

            if (IsConnectedToNetwork())
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument
                {
                    OptionFixNestedTags = true,
                    OptionAutoCloseOnEnd = true
                };

                string data = await GetResponseString(url);

                htmlDoc.LoadHtml(data);

                if (htmlDoc.DocumentNode != null)
                {
                    var titleNodes = htmlDoc.DocumentNode.DescendantsAndSelf("a").Where(
                            x => x.Attributes["title"] != null && x.HasChildNodes && x.ParentNode.OriginalName == "h3");

                    //x => x.HasChildNodes && x.ChildNodes.Count == 1 only the name of the city
                    var descriptions = htmlDoc.DocumentNode.DescendantsAndSelf("p").Where(x => x.HasChildNodes && x.FirstChild.OriginalName == "span");

                    var resultsCount = htmlDoc.DocumentNode.Descendants("p").Where(x => x.ParentNode.Name == "div" && x.ParentNode.Attributes["class"] != null && x.ParentNode.Attributes["class"].Value == "main-content-design");                      //("//*[@id=\"main-content\"]/div[2]/p");
                    int resultsNumber = 0;
                    foreach (var count in resultsCount)
                    {
                        resultsNumber = int.Parse(count.InnerText.Split(' ')[count.InnerText.Split(' ').Count() - 1]);
                        break;
                    }
                    //int resultsNumber = int.Parse(resultsCount.InnerText.Split(' ')[resultsCount.InnerText.Split(' ').Count() - 1]);
                    int pagesNumber = resultsNumber / 10;
                    if (resultsNumber % 10 != 0)
                    {
                        pagesNumber++;
                    }

                    int counter = 0;
                    //StringBuilder address = new StringBuilder();
                    AddressNode addressNode = new AddressNode();

                    for (int j = 0; j < descriptions.Count(); j++)
                    {
                        if (counter == 0)
                        {
                            addressNode.Address = descriptions.ElementAt(j).InnerText;
                        }
                        else if (counter == 1)
                        {
                            addressNode.Phone = descriptions.ElementAt(j).InnerText;
                        }
                        else if (counter == 2 || j == descriptions.Count() - 1)
                        {
                            addressNode.Email = descriptions.ElementAt(j).InnerText;
                            addresses.Add(addressNode);
                            addressNode = new AddressNode();
                            counter = -1;
                        }
                        counter++;
                    }

                    foreach (var htmlNode in titleNodes)
                    {
                        titles.Add(htmlNode.Attributes["title"].Value.Replace("&quot;", "\"").Replace("&amp;", "&"));
                    }

                    if (pagesNumber > 1)
                    {
                        for (int i = 2; i <= pagesNumber; i++)
                        {
                            url += "/page:" + i;
                            data = await GetResponseString(url);
                            htmlDoc.LoadHtml(data);

                            titleNodes = htmlDoc.DocumentNode.DescendantsAndSelf("a").Where(
                                                        x => x.Attributes["title"] != null && x.HasChildNodes && x.ParentNode.OriginalName == "h3");

                            descriptions = htmlDoc.DocumentNode.DescendantsAndSelf("p").Where(x => x.HasChildNodes && x.FirstChild.OriginalName == "span");

                            counter = 0;
                            addressNode = new AddressNode();

                            for (int j = 0; j < descriptions.Count(); j++)
                            {
                                if (counter == 0)
                                {
                                    addressNode.Address = descriptions.ElementAt(j).InnerText;
                                }
                                else if (counter == 1)
                                {
                                    addressNode.Phone = descriptions.ElementAt(j).InnerText;
                                }
                                else if (counter == 2 || j == descriptions.Count() - 1)
                                {
                                    addressNode.Email = descriptions.ElementAt(j).InnerText;
                                    addresses.Add(addressNode);
                                    addressNode = new AddressNode();
                                    counter = -1;
                                }
                                counter++;
                            }

                            foreach (var htmlNode in titleNodes)
                            {
                                titles.Add(htmlNode.Attributes["title"].Value.Replace("&quot;", "\"").Replace("&amp;", "&"));
                            }

                            url.Replace("/page:" + i, "");
                        }
                    }

                    for (int i = 0; i < titles.Count; i++)
                    {
                        //Console.WriteLine("{0} : {1}", titles[i], addresses[i]);
                        Restaurant rest = new Restaurant();
                        rest.Title = titles[i];
                        rest.Address = addresses[i].Address;
                        rest.Phone = addresses[i].Phone;
                        rest.Email = addresses[i].Email;
                        restaurants.Add(rest);
                    }
                }
            }
            else
            {
                //notify that there is no connection and can not load the restaurants
                SendNotification("No internet connection!", "Turn it on to see", "a list of restorants near you", "Images/connection.png");
            }
        }

        public static async Task<List<Recipe>> GetAllRecepiesFromHttpRequest()
        {
            List<Recipe> recipes = new List<Recipe>();
            string url = "http://www.bonapeti.bg/recepti/";

            var htmlDoc = new HtmlAgilityPack.HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };

            var recHtmlDoc = new HtmlAgilityPack.HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };

            for (int i = 1; i <= 114; i++)
            {
                string data = await GetResponseString(url + "?page=" + i);
                htmlDoc.LoadHtml(data);

                var titles = htmlDoc.DocumentNode.DescendantsAndSelf("a").Where(x => x.Attributes["title"] != null && x.ChildNodes.Count > 1 && x.Attributes["class"].Value.Contains("recipe_link"));

                var imageUrls = htmlDoc.DocumentNode.Descendants("div").Where(x => x.Attributes["class"] != null && (x.Attributes["class"].Value == "recipe_container" || x.Attributes["class"].Value == "user_recipe_container"));                   //("//*[contains(@class,'recipe_container')]");

                Recipe recipe = new Recipe();

                foreach (var imgUrl in imageUrls)
                {
                    if (imgUrl.Attributes["style"] != null)
                    {
                        string value = imgUrl.Attributes["style"].Value;
                        imageURLs.Add(value.Substring(value.IndexOf("'") + 1, value.LastIndexOf("'") - value.IndexOf("'") - 1));
                    }
                    else
                    {
                        imageURLs.Add("http://www.bonapeti.bg/images/user_nodish_big.jpg");
                    }
                }

                foreach (var title in titles)
                {
                    recipe = new Recipe();
                    recipe.Title = title.Attributes["title"].Value;
                    string recipeLink = title.Attributes["href"].Value;

                    string recData = await GetResponseString(recipeLink);
                    recHtmlDoc.LoadHtml(recData);
                    var timeNode = recHtmlDoc.DocumentNode.Descendants("div").Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value == "text");              //SelectSingleNode("//*[@id=\"printable_area\"]/div[4]/div[1]/div[2]");
                    foreach (var time in timeNode)
                    {
                        recipe.Time = time.InnerText.Trim();
                        break;
                    }
                    var ingredients = recHtmlDoc.DocumentNode.Descendants("table").Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value == "tbl_products");           //("//*[contains(@class,'last')]");
                    string ingredientsText = "";
                    foreach (var ingr in ingredients)
                    {
                        ingredientsText += ingr.InnerHtml.Trim();
                    }
                    recipe.Ingredients = ingredientsText.Replace("&nbsp;", "\n").
                                            Replace("\t", "").
                                            Replace("\n", "").
                                            Replace("<tr>", "").
                                            Replace("</tr>", "").
                                            Replace("<td class=\"last\">", "").
                                            Replace("<td class=\"last\" colspan=\"1\">", "").
                                            Replace("<td class=\"last\" colspan=\"2\">", "").
                                            Replace("<td class=\"last\" colspan=\"3\">", "").
                                            Replace("<td>", "").
                                            Replace("</td>", "").
                                            Replace("<br>", "\n").
                                            Replace("<span itemprop=\"ingredients\">", "").
                                            Replace("</span>", "").
                                            Replace("<b>", "\n").
                                            Replace("</b>", "").
                                            Replace("\n ", "\n").
                                            TrimEnd().TrimStart(',');

                    var preparations = recHtmlDoc.DocumentNode.Descendants("td").Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value == "stepDescription");  //("//*[contains(@class,'stepDescription')]");
                    StringBuilder prepWay = new StringBuilder();
                    foreach (var prep in preparations)
                    {
                        prepWay.Append(prep.InnerText.Trim());
                    }
                    recipe.PreparationWay = prepWay.ToString();

                    recipes.Add(recipe);
                }
                if (i != 114)
                {
                    recipes.RemoveAt(recipes.Count - 1);
                    recipes.RemoveAt(recipes.Count - 1);
                }
            }
            for (int i = 0; i < recipes.Count; i++)
            {
                recipes[i].ImageURL = imageURLs[i];
            }

            return recipes;
        }

        public static async Task CapturePhoto()
        {
            await mediacapture.InitializeAsync();
            //create photo encoding properties as JPEG and set the size that should be used for capturing
            var imageEncodingProperties = ImageEncodingProperties.CreateJpeg();
            imageEncodingProperties.Width = 640;
            imageEncodingProperties.Height = 480;

            //create new unique file in the pictures library and capture photo into it
            var photoStorageFile = await KnownFolders.PicturesLibrary.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);
            await mediacapture.CapturePhotoToStorageFileAsync(imageEncodingProperties, photoStorageFile);

            SendNotification("Photo captured", "check your images folder", "to view it", "Images/photo.png");

            //show the captured picture in an <Image />
            using (IRandomAccessStream fileStream = await photoStorageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                // Set the image source to the selected bitmap 
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.DecodePixelWidth = 600; //match the target Image.Width, not shown
                await bitmapImage.SetSourceAsync(fileStream);
                photoPlaceholder.Source = bitmapImage;
            }
            
            SharePhotoInFacebook(photoStorageFile.Path);
        }

        public static async void RegisterAudioBackgroundTask()
        {
            var isTaskRegistered = false;
            var taskName = "BakgroundAudioTask";

            foreach(var task in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
            {
                if(task.Value.Name == taskName)
                {
                    isTaskRegistered = true;
                    break;
                }
            }

            if(!isTaskRegistered)
            {
                var builder = new BackgroundTaskBuilder();

                builder.Name = taskName;
                builder.TaskEntryPoint = "BackgroundAudioTask.BackgroundAudioTask";
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.InternetAvailable, false));
#if WINDOWS_PHONE_APP
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
#endif
                BackgroundTaskRegistration task = builder.Register();
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            bool dbExists = await CheckDbAsync(dbName);
            if(!dbExists)
            {
                await CreateDataBaseAsync();
                await AddRecipesAsync();
            }

            SQLiteAsyncConnection dbCon = new SQLiteAsyncConnection(dbName);
            var query = dbCon.Table<Recipe>();
            recipes = await query.ToListAsync();

            viewModel.Recipes = recipes;
            this.listView.ItemsSource = viewModel.Recipes;

            RegisterAudioBackgroundTask();

            //bind the background music
#if WINDOWS_PHONE_APP

#endif

#if WINDOWS_APP
            //MediaElement media = this.audioMedia;
            //media.AudioCategory = AudioCategory.BackgroundCapableMedia;
            //media.Play();
            //SystemMediaTransportControls _systemMediaTransportControl = SystemMediaTransportControls.GetForCurrentView();
#endif
        }

        private async Task<bool> CheckDbAsync(string dbName)
        {
            bool dbExists = true;

            try
            {
                StorageFile sf = await ApplicationData.Current.LocalFolder.GetFileAsync(dbName);
            }
            catch (Exception)
            {
                dbExists = false;
            }

            return dbExists;
        }

        private async Task CreateDataBaseAsync()
        {
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(dbName);
            await conn.CreateTableAsync<Recipe>();
        }

        private async Task AddRecipesAsync()
        {
            var list = await GetAllRecepiesFromHttpRequest();

            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(dbName);
            await conn.InsertAllAsync(list);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await CapturePhoto();
        }

        private async void searchTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            string searchedString = this.searchTextBox.Text;

            SQLiteAsyncConnection dbCon = new SQLiteAsyncConnection(dbName);
            //string query = "SELECT * FROM Recipe WHERE LOWER(Title) LIKE %" + searchedString.ToLower() + "%";
            var query = dbCon.Table<Recipe>().Where(x => x.Title.Contains(searchedString));
            recipes = await query.ToListAsync();
         
            //recipes = await dbCon.QueryAsync<Recipe>(query);
            viewModel.Recipes = recipes;
            this.listView.ItemsSource = viewModel.Recipes;
        }

        private static async void SharePhotoInFacebook(string filePath)
        {

            dynamic parametersTry = new ExpandoObject();
            parametersTry.client_id = "662815490501841";
            parametersTry.redirect_uri = "https://www.facebook.com/connect/login_success.html";
            parametersTry.response_type = "token";
            parametersTry.display = "popup";
            if(!string.IsNullOrWhiteSpace(_ExtendedPermissions))
            {
                parametersTry.scope = _ExtendedPermissions;
            }
            var fb = new FacebookClient();
            Uri loginUrl = fb.GetLoginUrl(parametersTry);
            myWebView.Navigate(new Uri(loginUrl.AbsoluteUri, UriKind.Absolute));
            myWebView.NavigationCompleted += OnNavigationComplited(fb);
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 10);
            timer.Start();
            if(!timer.IsEnabled)
            {
                myWebView.Visibility = Visibility.Collapsed;
                SendNotification("Facebook login", "You are logged successfully", "", "Images/fb-login.png");
            }


            //var imgstream = File.OpenRead(filePath);

            await fb.PostTaskAsync("me/feed?message=Trying to post something on facebook", null);
            //{

            //    message = "trying to add a photo",
            //    file = new FacebookMediaStream
            //    {
            //        ContentType = "image/jpg",
            //        FileName = Path.GetFileName(filePath)

            //    }.SetValue(imgstream)


            //});



            fb.AppId = "662815490501841";
            fb.AppSecret = "8db5f3acd5ff48acc1d436d30d8409fa";
            dynamic loginParams = new ExpandoObject();
            loginParams.AppId = "662815490501841";
            loginParams.AppSecret = "8db5f3acd5ff48acc1d436d30d8409fa";
            loginParams.redirect_url = "http://localhost/Facebook/oauth/oauth-redirect.aspx";

            var loginUri = fb.GetLoginUrl(loginParams);

            //login
            //var fbSessionCLient = new FacebookSessionClient(fb.AppId);
            //fbSessionCLient.LoginAsync("user_about_me,read_stream,publish_stream,manage_pages");

            //var client = new FacebookClient(fbSessionCLient.CurrentSession.AccessToken);
            //string access_token = client.AccessToken;
            //client = new FacebookClient(access_token);

            //var mediaObject = new FacebookMediaObject
            //{
            //    FileName = System.IO.Path.GetFileName(filePath),
            //    ContentType = "image/jpg"
            //};

            //dynamic parameters = new ExpandoObject();
            //parameters.source = mediaObject;
            //parameters.message = "photo?";
            //parameters.access_token = access_token;

        }

        private static async Task<TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs>> OnNavigationComplited(FacebookClient fb)
        {
            await fb.PostTaskAsync("me/feed?message=Trying to post something on facebook", null);
            TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs> handler;
            return handler;
        }
    }
}