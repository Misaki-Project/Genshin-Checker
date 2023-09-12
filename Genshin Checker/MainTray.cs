using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace Genshin_Checker
{
    public partial class MainTray : Form
    {
        long sessionTime = 0;
        bool IsExit = false;
        bool IsHide = false;
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
                        IsHide = true;
                        break;
                }
            }
            System.Drawing.Text.PrivateFontCollection pfc = new();
            
            //resource���̃t�H���g�̌Ăяo��
            byte[] fontBuf = resource.font.DSEG7Classic_BoldItalic;
            IntPtr fontBufPtr = Marshal.AllocCoTaskMem(fontBuf.Length);
            Marshal.Copy(fontBuf, 0, fontBufPtr, fontBuf.Length);
            pfc.AddMemoryFont(fontBufPtr, fontBuf.Length);
            Marshal.FreeCoTaskMem(fontBufPtr);
            SessionTime.Font = new(pfc.Families[0], 36, FontStyle.Bold | FontStyle.Italic);
            TotalSessionTime.Font = new(pfc.Families[0], 28, FontStyle.Bold | FontStyle.Italic);

            //�A�v���̏�����&UI�̏�����
            sessionTime = App.SessionCheck.Instance.Load();
            CurrentStatus.ForeColor = Color.Red;
            CurrentStatus.Text = "���N��";
            App.ProcessTime.Instance.option.OnlyActiveWindow = true;
            App.ProcessTime.WatchDog = true;
            App.ProcessTime.Instance.SessionStart += TargetStart;
            App.ProcessTime.Instance.SessionEnd += TargetEnd;
            App.ProcessTime.Instance.ChangedState += ChangeState;
            notification.Icon = resource.icon.nahida;
            notification.Visible = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (IsHide)
            {
                IsHide = false;
                Hide();
            }
            var time = App.ProcessTime.Instance.Session;
            SessionTime.Text = $"{((int)time.TotalHours)}:{time:mm\\:ss\\.ff}";
            var totaltime = new TimeSpan((sessionTime + time.Ticks));
            TotalSessionTime.Text = $"{((int)totaltime.TotalHours)}:{totaltime:mm\\:ss\\.ff}";
        }
        void TargetStart(object? sender, EventArgs e)
        {
            sessionTime = App.SessionCheck.Instance.Load();
        }
        void ChangeState(object? sender, App.ProcessTime.Result e)
        {
            this.Invoke(() =>
            {
                switch (e.State)
                {
                    case App.ProcessTime.ProcessState.NotRunning:
                        CurrentStatus.ForeColor = Color.Red;
                        CurrentStatus.Text = "���N��";
                        break;
                    case App.ProcessTime.ProcessState.Background:
                        CurrentStatus.ForeColor = Color.Orange;
                        CurrentStatus.Text = "�o�b�N�O���E���h";
                        break;
                    case App.ProcessTime.ProcessState.Foreground:
                        CurrentStatus.ForeColor = Color.Lime;
                        CurrentStatus.Text = "�A�N�e�B�u";
                        break;
                }
            });
        }
        void TargetEnd(object? sender, App.ProcessTime.Result e)
        {
            notification.BalloonTipTitle = "���_�`�F�b�J�[";
            notification.BalloonTipText = $"�V�񂾎��� : {(int)e.SessionTime.TotalHours} ���� {e.SessionTime.Minutes:00} ��";
            notification.ShowBalloonTip(30000);
            App.SessionCheck.Instance.Append(e.SessionTime.Ticks);

        }

        private void MainTray_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!IsExit)
            {
                e.Cancel = true;
                timer1.Stop();
                Hide();
            }

        }

        private void �I��ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (App.ProcessTime.Instance.CurrentProcessState != App.ProcessTime.ProcessState.NotRunning)
            {
                App.SessionCheck.Instance.Append(App.ProcessTime.Instance.Session.Ticks);
            }
            IsExit = true;
            Close();
        }

        private void notification_Click(object sender, EventArgs e)
        {
            Show();
            Activate();
            timer1.Start();
        }

        private void MainTray_Load(object sender, EventArgs e)
        {

        }
    }
}