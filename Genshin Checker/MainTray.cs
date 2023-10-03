using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Genshin_Checker.App;
using Genshin_Checker.Window;
using Newtonsoft.Json;
using Genshin_Checker.Model.HoYoLab;
namespace Genshin_Checker
{
    public partial class MainTray : Form
    {
        long sessionTime = 0;
        Window.TimerDisplay? TimerDisplay= null;
        Window.TimeGraph? TimeGraph= null;
        Window.RealTimeData? RealTimeData = null;
        Window.SettingWindow? SettingWindow= null;
        public MainTray(bool safemode=false)
        {
            InitializeComponent();
            notification.Icon = resource.icon.nahida;
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
            if (Registry.GetValue("Config\\Setting", "IsCountBackground") == "True") App.ProcessTime.Instance.option.OnlyActiveWindow = false;
            App.ProcessTime.WatchDog = true;
            App.ProcessTime.Instance.SessionStart += TargetStart;
            App.ProcessTime.Instance.SessionEnd += TargetEnd;
            App.ProcessTime.Instance.ChangedState += ChangeState;
            notification.Icon = resource.icon.nahida;
            notification.Visible = true;
            //new Window.WebMiniBrowser(new("https://google.com"));
            //new BrowserApp.HoYoApp();
            Store.Accounts.Data.AccountAdded += AccountAdded;
            Store.Accounts.Data.Load();
            var name = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            Console.WriteLine("{0} {1}", name.Name, name.Version);

            versionNameToolStripMenuItem.Text = $"{name.Name} {name.Version}";
#if DEBUG
            versionNameToolStripMenuItem.Text += "(DEBUG)";
#endif
            if (safemode)
            {
                Trace.WriteLine("�y�x���z���݃Z�[�t���[�h�ł��B�A�v���͓ǂݎ���p�ɂȂ��Ă��܂��B");

                versionNameToolStripMenuItem.Text += "[Readonly]";
                Registry.IsReadOnly = true;
            }
        }

        void AccountAdded(object? sender, App.Account e)
        {
            e.RealTimeNote.Notification += Notification;
        }
        void TargetStart(object? sender, EventArgs e)
        {
            sessionTime = App.SessionCheck.Instance.Load();
            if (Option.Instance.Notification.IsGameStart)
            {
                notification.BalloonTipTitle = "���_�`�F�b�J�[";
                notification.BalloonTipText = $"���_�̋N�������m���܂����B";
                notification.ShowBalloonTip(30000);
            }
        }
        void ChangeState(object? sender, App.ProcessTime.Result e)
        {
        }
        void TargetEnd(object? sender, App.ProcessTime.Result e)
        {
            if (Option.Instance.Notification.IsGameEnd)
            {
                notification.BalloonTipTitle = "���_�`�F�b�J�[";
                notification.BalloonTipText = $"�V�񂾎��� : {(int)e.SessionTime.TotalHours} ���� {e.SessionTime.Minutes:00} ��";
                notification.ShowBalloonTip(30000);
            }

        }


        void Notification(object? sender, string e)
        {
            Trace.WriteLine(sender);
                notification.BalloonTipTitle = $"{sender}";
                notification.BalloonTipText = $"{e}";
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


        private void MainTray_Load(object sender, EventArgs e)
        {
            notification.Visible = true;
            notification.BalloonTipTitle = $"���_�`�F�b�J�[";
            notification.BalloonTipText = $"�N�����܂����B\n�^�X�N�g���C����J�����Ƃ��ł��܂��B";
            notification.ShowBalloonTip(30000);
        }

        private void Delay_Tick(object sender, EventArgs e)
        {
            Hide();
        }

        private void �ڍ׃v���C�f�[�^ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try { 
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
            catch (Exception ex)
            {
                var n = new ErrorMessage(ex.GetType().ToString(), ex.Message);
                n.ShowDialog(this);
            }
        }

        private void ���A���^�C���f�[�^ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try { 
            if (RealTimeData == null || RealTimeData.IsDisposed)
            {
                    if (Store.Accounts.Data.Count == 0)
                    {
                        var n = new ErrorMessage("�A�g���Ă���A�J�E���g�͂܂������悤�ł��B", "���萔�ł����A�ȉ��̑�����s���ĔF�؂��Ă��������B\n�ݒ�˃A�v���A�g��HoYoLab�Ƃ̘A�g");
                        n.ShowDialog(this);
                        return;
                    }
                    RealTimeData = new(Store.Accounts.Data[0]);
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
            catch (Exception ex)
            {
                var n = new ErrorMessage(ex.GetType().ToString(), ex.Message);
                n.ShowDialog(this);
            }
        }

        private void �ݒ�ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try { 
            if (SettingWindow == null || SettingWindow.IsDisposed)
            {
                SettingWindow = new();
                SettingWindow.WindowState = FormWindowState.Normal;
                SettingWindow.Show();
                SettingWindow.Activate();
            }
            else
            {
                SettingWindow.Show();
                if (SettingWindow.WindowState == FormWindowState.Minimized) SettingWindow.WindowState = FormWindowState.Normal;
                SettingWindow.Activate();
            }

            }
            catch (Exception ex)
            {
                var n = new ErrorMessage(ex.GetType().ToString(), ex.Message);
                n.ShowDialog(this);
            }
        }

        private void notification_Click(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
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
            }catch(Exception ex)
            {
                var n = new ErrorMessage(ex.GetType().ToString(), ex.Message);
                n.ShowDialog(this);
            }
        }

        private void testToolStripMenuItem_ClickAsync(object sender, EventArgs e)
        {
            /*Account account;
            if (Store.Accounts.Data.Count == 0) return;
            account = Store.Accounts.Data[0];
            var user = await account.GetServerAccounts(Account.Servers.os_asia);
            var nowabyss = await account.GetSpiralAbyss(true);
            var oldabyss = await account.GetSpiralAbyss(false);
            var index = await account.GetGameRecords();
            var realtime = await account.GetRealTimeNote();
            var character = await account.GetCharacters();
            var diaryinfo = await account.GetTravelersDiaryInfo();*/
            var a=new Window.TravelersDiary(Store.Accounts.Data[0]);
            a.Show();
            a.Activate();
        }
    }
}