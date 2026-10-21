using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ShoulderCamera {
    internal static class Program {
        [STAThread] static int Main(string[] args) {
            try {
#if RESEARCH
                if(args.Length==1 && args[0]=="--self-test"){CameraSession.SelfTest();Preferences.SelfTest();NumericInput.SelfTest();Configurations.SelfTest();Localizer.SelfTest();return 0;}
#endif
                bool ownsMutex;
                using(var mutex=new Mutex(true,"Local\\ShoulderCameraPrototype",out ownsMutex)) {
                if(!ownsMutex)throw new InvalidOperationException("Un panneau ou un essai Shoulder Camera est deja ouvert.");
#if RESEARCH
                if(args.Length==1 && args[0]=="--test-temporary") {
                    using(var session=CameraSession.Connect()) {
                        Console.WriteLine("CONNECTED: native camera configuration validated.");
                        try {
                            session.Apply(0.65f,0,false);
                            Console.WriteLine("TEMPORARY_TEST_APPLIED: local X 0.65 for walking/sprinting/idle/sneak; native aiming unchanged; restoring in 12 seconds.");
                            Thread.Sleep(12000);
                        }finally {session.Restore();Console.WriteLine("RESTORED: original native camera settings.");}
                    }
                    return 0;
                }
#endif
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
#if RESEARCH
                if((args.Length>=2 && args.Length<=4) && args[0]=="--render-preview") {
                    using(var form=new CameraPanel())using(var image=new Bitmap(form.Width,form.Height)) {
                        if(args.Length>=3)form.SelectProfile(int.Parse(args[2]));if(args.Length==4)form.PreviewLanguage(args[3]);
                        form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);
                        form.Show();Application.DoEvents();form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(args[1],ImageFormat.Png);
                    }
                    return 0;
                }
#endif
                if(args.Length!=0)throw new ArgumentException("Option inconnue.");
                Application.Run(new CameraPanel());return 0;
                }
            }catch(Exception error) {Console.Error.WriteLine(error.GetType().Name+": "+error.Message);return 1;}
        }
    }
}



