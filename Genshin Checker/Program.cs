using Genshin_Checker.Window;
using System.Linq.Expressions;
using Genshin_Checker.Window.Popup;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Globalization;
using Genshin_Checker.resource.Languages;

namespace Genshin_Checker
{
    internal static class Program
    {
        static System.Threading.Mutex? mutex = null;
        static bool HasHandle = false;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
#if !DEBUG
            //ThreadException�C�x���g�n���h����ǉ�
            Application.ThreadException +=
                new System.Threading.ThreadExceptionEventHandler(
                    Application_ThreadException);
#endif
            //����ݒ�
            if (CultureInfo.CurrentCulture.Name == "ja-JP"|| CultureInfo.CurrentCulture.Name == "ja")
            {
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ja-JP");
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ja-JP");
            }
            else
            {
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            }
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.EnableVisualStyles();
            ApplicationConfiguration.Initialize();
            bool ToastActivated = false;
            bool Override = false;
            string path = "";
            //�g�[�X�g�ʒm�̈������܂܂��ꍇ�͒ʒm���폜
            foreach (string cmd in Environment.GetCommandLineArgs())
            {
                switch (cmd)
                {
                    case "-ToastActivated":
                        ToastActivated= true;
                        break;
                    case "-Override":
                        Override = true; 
                        break;
                }
                if (cmd.StartsWith("Path:")){
                    path = cmd[5..];
                }
            }
            if (ToastActivated)
            {
                ToastNotificationManagerCompat.History.Clear();
                return;
            }
            if (Override)
            {
                var a = App.General.MovingData.WriteToApp(path).Result;
                string result;
                if (a != null) result = $"{a.GetType()} - {a.Message}\n{a}";
                else result = $"���p�����������܂����B";
                System.Diagnostics.Process.Start(Application.ExecutablePath,new List<string>() {$"Result:{result}"});
                return;
            }

            //Mutex�������߂�i�K���A�v���P�[�V�����ŗL�̕�����ɕύX���邱�ƁI�j
            string mutexName = "Genshin Checker";
            //Mutex�I�u�W�F�N�g���쐬����
            mutex = new System.Threading.Mutex(false, mutexName);
            HasHandle = false;
            try
            {
                try
                {
                    //�~���[�e�b�N�X�̏��L����v������
                    HasHandle = mutex.WaitOne(0, false);
                }
                //.NET Framework 2.0�ȍ~�̏ꍇ
                catch (System.Threading.AbandonedMutexException)
                {
                    //�ʂ̃A�v���P�[�V�������~���[�e�b�N�X��������Ȃ��ŏI��������
                    HasHandle = true;
                }
#if !DEBUG
                //�~���[�e�b�N�X�𓾂�ꂽ�����ׂ�
                if (!HasHandle)
                {
                    //�����Ȃ������ꍇ�́A���łɋN�����Ă���Ɣ��f���ďI��
                    var args = "";
                    foreach (var a in System.Environment.GetCommandLineArgs()) args += $"{a} ";
                    MessageBox.Show(string.Format(Localize.Error_Program_MultipleLaunched,args), "Genshin Checker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
#endif

                //�͂��߂���Main���\�b�h�ɂ������R�[�h�����s
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.Run(new MainTray(safemode: !HasHandle));
            }
            finally
            {
                try
                {
                    if (HasHandle)
                    {
                        //�~���[�e�b�N�X���������
                        mutex.ReleaseMutex();
                    }
                    mutex.Close();
                }
                catch { }
            }
        }
        //ThreadException�C�x���g�n���h��
        private static void Application_ThreadException(object sender,
            ThreadExceptionEventArgs e)
        {
            try
            {
                if (HasHandle) mutex?.ReleaseMutex();
                mutex?.Close();
            }
            catch{}
            try
            {
                new FatalError(e.Exception).ShowDialog();
            }
            catch
            {
                //�G���[���b�Z�[�W��\������
                var res = MessageBox.Show(string.Format(Common.ApplicationErrorTrace, e.Exception.GetType(), e.Exception.Message, e), Common.ApplicationError, MessageBoxButtons.YesNo, MessageBoxIcon.Stop);
                if (res == DialogResult.Yes)
                {
                    Application.Restart();
                }
            }
            finally
            {
                //�A�v���P�[�V�������I������
                Application.Exit();
            }
        }
    }
}