using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BigReminder365
{
    public partial class Form1 : Form
    {
        private static string s_clientId = "45cd0542-ba5f-4a13-bbac-66b0937a7052";
        private static string[] s_scopes = new string[] { "user.read", "calendars.read" };

        private static PublicClientApplication s_clientApp = new PublicClientApplication(s_clientId, "https://login.microsoftonline.com/common", TokenCacheHelper.GetUserCache());

        private string _calendarViewApiUrl = "https://graph.microsoft.com/v1.0/me/calendarview";        
        private CalendarResult _calendarData;
        private Timer _downloadTimer = new Timer();
        private Timer _checkEventTimer = new Timer();
        private Dictionary<string, ReminderForm> _formCache = new Dictionary<string, ReminderForm>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            Hide();
            _downloadTimer.Interval = (int)TimeSpan.FromMinutes(30).TotalMilliseconds;
            _downloadTimer.Tick += _downloadTimer_Tick;
            _checkEventTimer.Interval = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;
            _checkEventTimer.Tick += _checkEventTimer_Tick;

            _ = DownloadCalendarItemsAsync();

            _downloadTimer.Start();
            _checkEventTimer.Start();
        }

        private void _checkEventTimer_Tick(object sender, EventArgs e)
        {
            CheckForUpcomingEvent();
        }

        private void _downloadTimer_Tick(object sender, EventArgs e)
        {
            _ = DownloadCalendarItemsAsync();
        }

        private async Task DownloadCalendarItemsAsync()
        {
            AuthenticationResult authResult = null;

            try
            {
                authResult = await s_clientApp.AcquireTokenSilentAsync(s_scopes, s_clientApp.Users.FirstOrDefault());
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    authResult = await s_clientApp.AcquireTokenAsync(s_scopes);
                }
                catch (MsalException msalex)
                {
                    MessageBox.Show($"Error Acquiring Token:{Environment.NewLine}{msalex}", "BigReminder365");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Acquiring Token Silently:{Environment.NewLine}{ex}", "BigReminder365");
                return;
            }

            if (authResult != null)
            {
                try
                {
                    var url = $"{_calendarViewApiUrl}?startdatetime={DateTime.UtcNow.AddHours(-6).ToString("yyyy-MM-ddTHH:mm:00.000Z")}&enddatetime={DateTime.UtcNow.AddHours(6).ToString("yyyy-MM-ddTHH:mm:00.000Z")}";
                    _calendarData = JsonConvert.DeserializeObject<CalendarResult>(await GetHttpContentWithTokenAsync(url, authResult.AccessToken));
                    CheckForUpcomingEvent();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error getting new calendar items:{System.Environment.NewLine}{ex}", "BigReminder365");
                }
            }
        }

        private void CheckForUpcomingEvent()
        {
            if (_calendarData == null || _calendarData.value == null)
            {
                return;
            }

            foreach (var item in _calendarData.value.Where(p => !p.isAllDay))
            {
                var diff = item.start.dateTime - DateTime.UtcNow;
                if (diff.TotalMinutes < 11 && diff.TotalMinutes > -1)
                {
                    if (!_formCache.ContainsKey(item.id))
                    {
                        var rf = new ReminderForm();
                        _formCache.Add(item.id, rf);
                        rf.Show(item.subject, item.location?.displayName, diff);
                    }
                    else
                    {
                        _formCache[item.id].EnsureShow(diff);
                    }
                }
                else
                {
                    if (_formCache.ContainsKey(item.id))
                    {
                        _formCache[item.id].Close();
                        _formCache[item.id].Dispose();
                        _formCache.Remove(item.id);
                    }
                }
            }
        }

        public async Task<string> GetHttpContentWithTokenAsync(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
    }
}
