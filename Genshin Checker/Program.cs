using Genshin_Checker.Window;
using System.Linq.Expressions;
using Genshin_Checker.Window.Popup;
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
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

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
                    MessageBox.Show("���d�N���͂ł��܂���B", "Genshin Checker", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                var res = MessageBox.Show($"�A�v���P�[�V�����G���[���������܂����B\n�ċN�����܂����H\n\n--- �f�o�b�O��� ---\n{e.Exception.GetType()}\n{e.Exception.Message}\n--- StackTrace ---\n{e.Exception.StackTrace}\n--- StackTrace �I��� ---", "Genshin Checker �A�v���P�[�V�����G���[", MessageBoxButtons.YesNo, MessageBoxIcon.Stop);
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