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
    public partial class ReminderForm : Form
    {
        private bool _isClosed = false;

        public ReminderForm()
        {
            InitializeComponent();
        }

        public void Show(string title, string location, TimeSpan timeToGo)
        {
            labelTitle.Text = title;
            labelLocation.Text = location;

            int min = (int)timeToGo.TotalMinutes;
            UpdateBackgroundColor(min);

            labelTime.Text = $"In {min} minutes";

            ShowDialog();
        }

        public void EnsureShow(TimeSpan timeToGo)
        {
            if (_isClosed)
            {
                return;
            }

            int min = (int)timeToGo.TotalMinutes;
            UpdateBackgroundColor(min);

            labelTime.Text = $"In {min} minutes";
            Show();
        }

        private void UpdateBackgroundColor(int minToGo)
        {
            if (minToGo < 5)
            {
                int c = minToGo == 4 ? 220 : minToGo == 3 ? 200 : minToGo == 2 ? 150 : minToGo == 1 ? 50 : 0;
                BackColor = Color.FromArgb(255, c, 0);
            }
        }

        private void buttonDismiss_Click(object sender, EventArgs e)
        {
            _isClosed = true;
            Close();
        }

        private void buttonSnooze_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void ReminderForm_Load(object sender, EventArgs e)
        {
            Location = new Point(Properties.Settings.Default.ReminderWindowX, Properties.Settings.Default.ReminderWindowY);
        }

        private void ReminderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.ReminderWindowX = Location.X;
            Properties.Settings.Default.ReminderWindowY = Location.Y;
            Properties.Settings.Default.Save();
        }
    }
}
