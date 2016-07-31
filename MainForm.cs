using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Web;
using System.Globalization;

namespace VkAudiosaver
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private static class VkData
        {
            //id зарегистрированного в вк приложения
            public const string clientId = "*******";
            //стандартная страница редиректа
            public const string redirectUri = "https://oauth.vk.com/blank.html";
            //битовая маска доступа, соотв. доступу к аудио
            public const int scope = 8;
            //ключ доступа к API
            public static string accessToken;
            //id залогинившегося пользователя
            public static string userId;
        }

        private List<Song> SongList;

        private void MainForm_Load(object sender, EventArgs e)
        {
            //авторизуемся
            Authorization();
        }

        private void browser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (e.Url.GetLeftPart(UriPartial.Path) == VkData.redirectUri)
            {
                browser.Visible = false;
                //при перенаправлении после авторизации извлекаем из url ключ доступа и id пользователя
                System.Collections.Specialized.NameValueCollection queryParams =
                                                    HttpUtility.ParseQueryString(e.Url.Fragment.Substring(1));
                VkData.accessToken = queryParams.Get("access_token");
                VkData.userId = queryParams.Get("user_id");

                //в случае успеха переходим к составлению списка
                if (queryParams.Get("error") == null) FillSongList();
                else
                {
                    //при ошибке выводим сообщение и снова пробуем авторизоваться
                    MessageBox.Show(queryParams.Get("error_description").Replace('_', ' '),
                        new CultureInfo("en-US").TextInfo.ToTitleCase(queryParams.Get("error").Replace('_', ' ')),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Authorization();
                }
            }
        }

        private void Authorization()
        {
            browser.Visible = true;
            //открытие диалога авторизации
            browser.Navigate(String.Format(
                "https://oauth.vk.com/authorize?client_id={0}&redirect_uri={1}&display=page&scope={2}&response_type=token&v=5.53",
                        VkData.clientId, VkData.redirectUri, VkData.scope));
        }

        private void FillSongList()
        {
            //получаем xml
            //парсим
            //заполняем список
        }
    }
}

class Song
{
    public Song(string a, string t)
    { artist = a; title = t; }

    private string artist;
    private string title;

    public override string ToString()
    {
        return artist + " – " + title;
    }
}