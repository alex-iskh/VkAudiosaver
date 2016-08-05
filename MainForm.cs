using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using System.Net;
using System.Xml;
using System.IO;
using System.Globalization;

namespace VkAudiosaver
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            SongList = new BindingList<Song>();
        }

        private static class VkData
        {
            //id зарегистрированного в вк приложения
            //его пожно получить, создав приложение на vk.com/editapp?act=create
            public const string clientId = "*******";
            //стандартная страница редиректа
            public const string redirectUri = "https://oauth.vk.com/blank.html";
            //битовая маска доступа, соотв. доступу к аудио
            public const int scope = 8;
            //ключ доступа к API
            public static string accessToken;
            //id залогинившегося пользователя
            public static string userId;
            //версия API
            public const string version = "5.53";
        }

        //список песен пельзователя
        private BindingList<Song> SongList;

        private void MainForm_Load(object sender, EventArgs e)
        {
            //привязываем listBox к списку песен
            songListBox.DataSource = SongList;

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
                else ShowError(new CultureInfo("en-US").TextInfo.ToTitleCase(queryParams.Get("error").Replace('_', ' ')),
                    queryParams.Get("error_description").Replace('_', ' '));
            }
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            //для выбора папки открываем диалоговое окно
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                //сохраняем в папку выбранные в списке песни в формате "исполнитель – композиция.mp3"
                using (WebClient client = new WebClient())
                {
                    foreach (Song song in songListBox.SelectedItems)
                        client.DownloadFile(song.Url,folderBrowser.SelectedPath + "\\" 
                            + string.Join("", song.ToString().Split(Path.GetInvalidFileNameChars())) + ".mp3");
                }
                MessageBox.Show("Аудиозаписи успешно загружены", "Загрузка завершена", MessageBoxButtons.OK);
            }
        }

        private void Authorization()
        {
            browser.Visible = true;
            //открытие диалога авторизации
            browser.Navigate(String.Format(
                "https://oauth.vk.com/authorize?client_id={0}&redirect_uri={1}&display=page&scope={2}&response_type=token&v={3}",
                      VkData.clientId, VkData.redirectUri, VkData.scope, VkData.version));
        }

        //добавляет в список песен до loadCount аудио
        //НА БУДУЩЕЕ: можно использовать при реализации кнопки "Показать еще"
        private void FillSongList()
        {
            const int loadCount = 100;
            //получаем xml с треками
            //offset определяет смещение выборки, поэтому привязан к числу эл-тов в списке
            XmlDocument songXml = new XmlDocument();
            songXml.Load(String.Format(
                "https://api.vk.com/method/audio.get.xml?owner_id={0}&need_user=0&offset={1}&count={2}&access_token={3}&v={4}",
                    VkData.userId, SongList.Count, loadCount, VkData.accessToken, VkData.version));

            //читаем xml и заполняем список, если не появилась ошибка
            XmlNode root = songXml.DocumentElement;
            if (root.Name == "response")
            {
                foreach (XmlNode node in root["items"].ChildNodes)
                    if (node.Name == "audio")
                        SongList.Add(new Song(node["artist"].InnerText, node["title"].InnerText, node["url"].InnerText));
            }
            else if (root.Name == "error")
                ShowError("Can't get song list", root["error_msg"].InnerText);
            else ShowError();
        }

        private void ShowError(string title = null, string descr = null)
        {
            //при ошибке выводим сообщение и снова пробуем авторизоваться
            if (title != null && descr != null)
                MessageBox.Show(descr, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else MessageBox.Show("Unknown error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Authorization();
        }
    }
}

class Song
{
    public Song(string a, string t, string u )
    { artist = a; title = t; downloadUrl = u; }

    private string artist;
    private string title;
    private string downloadUrl;

    public string Url { get { return downloadUrl; } }

    public override string ToString()
    {
        return artist + " – " + title;
    }
}