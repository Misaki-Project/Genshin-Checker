using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Genshin_Checker
{
    public partial class MainTray : Form
    {
        long sessionTime = 0;
        Window.TimerDisplay? TimerDisplay= null;
        Window.TimeGraph? TimeGraph= null;
        Window.RealTimeData? RealTimeData = null;
        public MainTray()
        {
            InitializeComponent();
            var cmds = System.Environment.GetCommandLineArgs();
            //�R�}���h���C��������񋓂���
            foreach (string cmd in cmds)
            {
                switch (cmd)
                {
                    case "-hide":
                        //IsHide = true;
                        break;
                }
            }
            //�A�v���̏�����&UI�̏�����
            App.ProcessTime.Instance.option.OnlyActiveWindow = true;
            App.ProcessTime.WatchDog = true;
            App.ProcessTime.Instance.SessionStart += TargetStart;
            App.ProcessTime.Instance.SessionEnd += TargetEnd;
            App.ProcessTime.Instance.ChangedState += ChangeState;
            notification.Icon = resource.icon.nahida;
            notification.Visible = true;
            //new Window.WebMiniBrowser(new("https://google.com"));
            //new BrowserApp.HoYoApp();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        }
        void TargetStart(object? sender, EventArgs e)
        {
            sessionTime = App.SessionCheck.Instance.Load();
        }
        void ChangeState(object? sender, App.ProcessTime.Result e)
        {
        }
        void TargetEnd(object? sender, App.ProcessTime.Result e)
        {
            notification.BalloonTipTitle = "���_�`�F�b�J�[";
            notification.BalloonTipText = $"�V�񂾎��� : {(int)e.SessionTime.TotalHours} ���� {e.SessionTime.Minutes:00} ��";
            notification.ShowBalloonTip(30000);

        }

        private void MainTray_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void �I��ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            App.ProcessTime.Instance.EmergencyReset();
            Close();
        }

        private async void notification_Click(object sender, EventArgs e)
        {
            if (TimerDisplay == null || TimerDisplay.IsDisposed)
            {
                TimerDisplay = new();
                TimerDisplay.WindowState = FormWindowState.Normal;
                TimerDisplay.Show();
                TimerDisplay.Activate();
            }
            else
            {
                TimerDisplay.Show();
                if (TimerDisplay.WindowState == FormWindowState.Minimized) TimerDisplay.WindowState = FormWindowState.Normal;
                TimerDisplay.Activate();
            }
        }

        private void MainTray_Load(object sender, EventArgs e)
        {

        }

        private void Delay_Tick(object sender, EventArgs e)
        {
            Hide();
        }

        private void �ڍ׃v���C�f�[�^ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (TimeGraph == null || TimeGraph.IsDisposed)
            {
                TimeGraph = new();
                TimeGraph.WindowState = FormWindowState.Normal;
                TimeGraph.Show();
                TimeGraph.Activate();
            }
            else
            {
                TimeGraph.Show();
                if (TimeGraph.WindowState == FormWindowState.Minimized) TimeGraph.WindowState = FormWindowState.Normal;
                TimeGraph.Activate();
            }
        }

        private void ���A���^�C���f�[�^ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (RealTimeData == null || RealTimeData.IsDisposed)
            {
                RealTimeData = new();
                RealTimeData.WindowState = FormWindowState.Normal;
                RealTimeData.Show();
                RealTimeData.Activate();
            }
            else
            {
                RealTimeData.Show();
                if (RealTimeData.WindowState == FormWindowState.Minimized) RealTimeData.WindowState = FormWindowState.Normal;
                RealTimeData.Activate();
            }
        }
    }
}