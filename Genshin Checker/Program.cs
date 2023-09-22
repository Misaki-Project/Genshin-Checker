namespace Genshin_Checker
{
    internal static class Program
    {
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
            System.Threading.Mutex mutex = new System.Threading.Mutex(false, mutexName);
            bool hasHandle = false;
            try
            {
                try
                {
                    //�~���[�e�b�N�X�̏��L����v������
                    hasHandle = mutex.WaitOne(0, false);
                }
                //.NET Framework 2.0�ȍ~�̏ꍇ
                catch (System.Threading.AbandonedMutexException)
                {
                    //�ʂ̃A�v���P�[�V�������~���[�e�b�N�X��������Ȃ��ŏI��������
                    hasHandle = true;
                }
                //�~���[�e�b�N�X�𓾂�ꂽ�����ׂ�
                if (hasHandle == false)
                {
                    //�����Ȃ������ꍇ�́A���łɋN�����Ă���Ɣ��f���ďI��
                    MessageBox.Show("���d�N���͂ł��܂���B","Genshin Checker",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    return;
                }

                //�͂��߂���Main���\�b�h�ɂ������R�[�h�����s
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                Application.Run(new MainTray());
            }
            finally
            {
                if (hasHandle)
                {
                    //�~���[�e�b�N�X���������
                    mutex.ReleaseMutex();
                }
                mutex.Close();
            }
        }
        //ThreadException�C�x���g�n���h��
        private static void Application_ThreadException(object sender,
            System.Threading.ThreadExceptionEventArgs e)
        {
            try
            {
                //�G���[���b�Z�[�W��\������
                var res=MessageBox.Show($"�A�v���P�[�V�����G���[���������܂����B\n�ċN�����܂����H\n\n--- �f�o�b�O��� ---\n{e.Exception.GetType()}\n{e.Exception.Message}\n--- StackTrace ---\n{e.Exception.StackTrace}\n--- StackTrace �I��� ---", "Genshin Checker �A�v���P�[�V�����G���[",MessageBoxButtons.YesNo,MessageBoxIcon.Stop);
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