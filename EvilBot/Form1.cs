using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WindowsInput;
using System.Drawing.Imaging;
using MySql.Data.MySqlClient;
using AForge.Imaging.Filters;
using WindowScrape;
using AForge.Imaging;

namespace EvilBot
{
    public partial class Form1 : Form
    {
        // mouse event
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        // mouse flags
        public enum MouseEventFlags : uint
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010,
            WHEEL = 0x00000800,
            XDOWN = 0x00000080,
            XUP = 0x00000100
        }
        int t1;                             // contien le timestamp
        public static string db;
        public static string user;
        public static string pass;
        public static string hostdb;
        public string botname;

        public int url_id;                          // id du url choisie
        public string url_string;
        //public bool url_download = false;
        public bool url_rate = false;
        public bool url_g = false;
        public bool url_comment = false;
        public int url_loop = 0;

        public string account_email = "";
        public string account_password = "";
        public int account_id;                  // id du compte choisie

        public Form1()
        {
            InitializeComponent();
        }

        #region SobelFilter
        /*private Bitmap applySobelFilter(Bitmap ImageSource)
        {
            Bitmap tmpBitmap;
            // check pixel format
            if ((ImageSource.PixelFormat == PixelFormat.Format16bppGrayScale) ||
                 (Bitmap.GetPixelFormatSize(ImageSource.PixelFormat) > 32))
            {
                MessageBox.Show("The demo application supports only color images.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // free image
                ImageSource.Dispose();
                ImageSource = null;
            }
            else
            {
                // make sure the image has 24 bpp format
                if (ImageSource.PixelFormat != PixelFormat.Format24bppRgb)
                {
                    Bitmap temp = AForge.Imaging.Image.Clone(ImageSource, PixelFormat.Format24bppRgb);
                    ImageSource.Dispose();
                    ImageSource = temp;
                }
            }

            tmpBitmap = Grayscale.CommonAlgorithms.RMY.Apply(ImageSource);
            SobelEdgeDetector filter = new SobelEdgeDetector();
            tmpBitmap = filter.Apply(tmpBitmap);
            
            return tmpBitmap;
        }*/
        #endregion

        private void StartJob_Click(object sender, EventArgs e)
        {
            this.Location = new Point(0, 0);
            ReadConfigFile();
            // thread principal qui lance les étapes 1 par 1
            Thread bworkPrincipalT = new Thread(new ThreadStart(bworkPrincipal));
            bworkPrincipalT.Start();
            StartJob.Enabled = false;
        }
        #region change to gray color
        /*public Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
            new float[][]
            {
                new float[] {.3f, .3f, .3f, 0, 0},
                new float[] {.59f, .59f, .59f, 0, 0},
                new float[] {.11f, .11f, .11f, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
            });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }*/
        #endregion
        private Point lookForImageRoutine(Bitmap sub, int timeElapsed)
        {
            ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0);

            bool found = false;
            Point location = Point.Empty;
            t1 = ReturnTimeStamp();

            while (!this.IsDisposed)
            {
                Bitmap mainScreen = CaptureScreen();
                mainScreen = mainScreen.Clone(new Rectangle(new Point(0, 0), mainScreen.Size), PixelFormat.Format8bppIndexed);
                mainScreen.Save("done.png");
                sub = sub.Clone(new Rectangle(new Point(0, 0), sub.Size), PixelFormat.Format8bppIndexed);
                // compare two images
                TemplateMatch[] matchings = tm.ProcessImage(mainScreen, sub);
                // check similarity level
                if (matchings[0].Similarity > 0.90)
                {
                    // do something with quite similar images
                    location = matchings[0].Rectangle.Location;
                    found = true;
                }

                if (found)
                    break;

                // si la boucle reste plus de 1 minutes on dois intérompre le processus
                int t2 = ReturnTimeStamp();

                sendMessagetoConsole((t2 - t1).ToString());
                if (t2 - t1 > timeElapsed)
                    break;
            }
            return location;
        }

        #region lookForImageRoutine old
        /*private Point lookForImageRoutine(Bitmap sub, int timeElapsed, bool filter)
        {
            //////////////////////////////////
            
            //////////////////////////////////
            // look for a specified image inside another image in counted time
            bool found = false;
            Point location = Point.Empty;
            t1 = ReturnTimeStamp();

            while (!this.IsDisposed)
            {
                Bitmap captureScreen = (filter) ? applySobelFilter(CaptureScreen()) : CaptureScreen();
                captureScreen.Save("done.png");
                //if(timeElapsed == 120)
                //{
                    //Bitmap tmpBg = new Bitmap("bg.bmp");
                    //List<Point> points2 = GetSubPositions(tmpBg, sub);
                //}
                sub.Save("sub.png");
                List<Point> points = GetSubPositions(applySobelFilter(CaptureScreen()), sub);
                if (points.Count > 1)
                {
                    sendMessagetoConsole("there was many instance found\r");
                    location = points[0];
                    found = true;
                }
                else if (points.Count == 1)
                {
                    location = points[0];
                    found = true;
                }

                if (found)
                    break;

                // si la boucle reste plus de 1 minutes on dois intérompre le processus
                int t2 = ReturnTimeStamp();

                sendMessagetoConsole((t2 - t1).ToString());
                if (t2 - t1 > timeElapsed)
                    break;
            }

            return location;
        }*/
        #endregion

        private void bworkPrincipal()
        {
            // 1ere étape, qui détérmine si le ADV Genymotion est éxécuté ou pas, si non le lancer et attendre son apparition
            // recherche si l'application genymotion est lancé
            InputSimulator inputSimulator = new InputSimulator();
            MouseSimulator mouseSimulator = new MouseSimulator(inputSimulator);

            sendMessagetoConsole("Thread Started");

            /*Process[] liste = Process.GetProcessesByName("player");
            if (liste.Count() > 0)
            {
                foreach (var process in Process.GetProcessesByName("player"))
                {
                    process.Kill();
                    sendMessagetoConsole("STEP 1, genymotion found, the proccess will be killed\r");
                    sendMessagetoConsole("STEP 1, process killed\r");
                }
            }

            sendMessagetoConsole("STEP 1, no genymotion application found\r");
            sendMessagetoConsole("STEP 1, launching genymotion by cmd\r");*/
            ///////////////// fin nétoyage

            ///////// total des comptes disponibles
            MySqlConn mySqlConn = new MySqlConn();
            mySqlConn.cmd.CommandText = "select count(id) from accounts where verified_number ='1' & valid_account ='1'";
            mySqlConn.reader = mySqlConn.cmd.ExecuteReader();
            mySqlConn.reader.Read();
            int total_rows = Convert.ToInt16(mySqlConn.reader.GetString(0));
            if(total_rows == 0)
            {
                // aucun compte n'est disponible
                sendMessagetoConsole("STEP 1, no valide account found");
                sendMessagetoConsole("STEP 1, system will be interrupted");
            }
            //////////////////////////////////////////

            /////////  recherche d'un url libre a gérer
            mySqlConn = new MySqlConn();
            //mySqlConn.cmd.CommandText = "select * from submited_url where handled_bot_name ='' LIMIT 1";
            mySqlConn.cmd.CommandText = "select * from submited_url where submited_url.loop > 0 LIMIT 1";
            mySqlConn.reader = mySqlConn.cmd.ExecuteReader();
            if (mySqlConn.reader.Read())
            {
                url_id = Convert.ToInt16(mySqlConn.reader["id"]);
                url_string = mySqlConn.reader["url"].ToString();
                //url_download = Convert.ToBoolean(mySqlConn.reader["download"]);
                url_rate = Convert.ToBoolean(mySqlConn.reader["rate"]);
                url_g = Convert.ToBoolean(mySqlConn.reader["g"]);
                url_comment = Convert.ToBoolean(mySqlConn.reader["comment"]);
                int desired_loop = Convert.ToInt16(mySqlConn.reader["loop"]);

                // si l'utilisateur demande plus de download que ceux du nombre disponible, on lui affecte que le nombre disponible
                if (total_rows < desired_loop)
                    url_loop = total_rows;
                else
                    url_loop = desired_loop;
            }
            else
            {
                // aucun email n'est disponible pour le pomouvoir
                sendMessagetoConsole("STEP 1, no application to promote");
                sendMessagetoConsole("STEP 1, system will be interrupted");
            }
            mySqlConn.conn.Close();

            ///////////////////////////// on bloque l'url selectionné pour qu'aucun autre bot ne l'utilise
            /*mySqlConn = new MySqlConn();
            mySqlConn.cmd.CommandText = "update submited_url set handled_bot_name = '" + botname + "' where id = '" + url_id + "'";
            mySqlConn.reader = mySqlConn.cmd.ExecuteReader();
            mySqlConn.conn.Close();*/
            

            //////////////////////////////////// on choisie l'id de notre prochain compte a utiliser
            mySqlConn = new MySqlConn();
            mySqlConn.cmd.CommandText = "select id from accounts where verified_number ='1' & valid_account ='1' ORDER BY id LIMIT 1 OFFSET 0";
            mySqlConn.reader = mySqlConn.cmd.ExecuteReader();
            mySqlConn.reader.Read();
            account_id = Convert.ToInt16(mySqlConn.reader.GetString(0)) - 1;        // on soustrait 1 pour que la prochaine verification 'dans la boucle' puisse choisie ce meme id puisqu'il incremente a chaque fois avec OFFSET qui ne prend pas le numero choisie en compte mais le suivant, donc on dois soustraire pour le reselectionner
            mySqlConn.conn.Close();

            mySqlConn.Dispose();
            mySqlConn = null;

            //////////////////////////// début de la boucle
            for (int cnt = 0; cnt < url_loop; cnt++)
            {
                // on selectionne le 1er compte disponible selon l'id 
                mySqlConn = new MySqlConn();
                mySqlConn.cmd.CommandText = "select * from accounts where verified_number ='1' & valid_account ='1' ORDER BY id LIMIT 1 OFFSET " + account_id;
                mySqlConn.reader = mySqlConn.cmd.ExecuteReader();

                if (mySqlConn.reader.Read())
                {
                    account_id = Convert.ToInt16(mySqlConn.reader["id"]);
                    account_email = mySqlConn.reader["email"].ToString();
                    account_password = mySqlConn.reader["password"].ToString();
                }

                Process P = new Process();
                P.StartInfo.FileName = @"C:\Program Files\Genymobile\Genymotion\player.exe";
                P.StartInfo.Arguments = " --vm-name \"device1\"";
                P.StartInfo.UseShellExecute = true;
                P.Start();
                P.Close();

                Point location = Point.Empty;

                sendMessagetoConsole("STEP 1, genymotion launched\r");
                //goto finish;
                ///////////////////////////////////////////////////////////
                // recherche si l'émulateur a complétement démaré par la recherche de la presence de l'image du play store
                sendMessagetoConsole("STEP 2, check for android Play Store icon\r");
                
                location = lookForImageRoutine(Properties.Resources.play_store_icone, 120);

                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 2, Android Setting Icon found succeed\r");
                else
                {
                    sendMessagetoConsole("STEP 2, Exception: Play Store Icon not found\r");
                    sendMessagetoConsole("STEP 2, Exception: System will be interupted\r");
                    break;
                }
                
                Cursor.Position = location;
                sendMessagetoConsole("STEP 3, moved mouse to Play Stor Icon location succeeded\r");
                Mouse_Click();
                sendMessagetoConsole("STEP 4, simulated click on Play Stor icon succeeded\r");
                
                // attente de l'image du bouton existing account
                sendMessagetoConsole("STEP 5, look for \"Existing\" button\r");
                //Thread.Sleep(500);

                location = lookForImageRoutine(Properties.Resources.existing_account, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 5, button EXISTING found\r");
                else
                {
                    sendMessagetoConsole("STEP 5, Exception: EXISTING button not found\r");
                    sendMessagetoConsole("STEP 5, Exception: System will be interupted\r");
                    break;
                }

                //Cursor.Position = location;
                //Mouse_Click();
                sendMessagetoConsole("STEP 5, launching button\r");
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);
                
                //Thread.Sleep(500);
                sendMessagetoConsole("STEP 6, Looking for SIGN IN Icon\r");
                location = lookForImageRoutine(Properties.Resources.signin, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 6, SIGN IN page found\r");
                else
                {
                    sendMessagetoConsole("STEP 6, Exception: SIGN IN Icon not found\r");
                    sendMessagetoConsole("STEP 6, Exception: System will be interupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 7, injecting credentials\r");
                WriteText(account_email);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.TAB);
                WriteText(account_password);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);

                Thread.Sleep(500);
                ////////////////////////////////////////////////////////////////////////////////
                /*sendMessagetoConsole("STEP 8, Look for TERMS OF SERVICE Icon\r");
                location = lookForImageRoutine(Properties.Resources.terms_of_service, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 8, TERMS OF SERVICE page found\r");
                else
                {
                    sendMessagetoConsole("STEP 8, TERMS OF SERVICE page not found\r");
                    sendMessagetoConsole("STEP 8, Exception: System will be interupted\r");
                    break;
                }*/

                sendMessagetoConsole("STEP 9, navigating to next page\r");
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);

                
                //return;
                ////////////////////////////////////////////////////////////////////////////
                sendMessagetoConsole("STEP 10, Looking for GOOGLE SERVICES Icon\r");
                location = lookForImageRoutine(Properties.Resources.google_services, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 10, GOOGLE SERVICES page found\r");
                else
                {
                    sendMessagetoConsole("STEP 10, GOOGLE SERVICES page not found\r");
                    sendMessagetoConsole("STEP 10, Exception: System will be interupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 10, Navigation to next page\r");
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);

                //////////////////////////////////////////////////////////////////////////
                #region
                sendMessagetoConsole("STEP 11, Looking for cascad menu Icon\r");
                location = lookForImageRoutine(Properties.Resources.cascad_menu, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 11, cascad menu page found\r");
                else
                {
                    sendMessagetoConsole("STEP 11, cascad menu not found\r");
                    sendMessagetoConsole("STEP 11, Exception: System will be interupted\r");
                    break;
                }

                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 12, moved mouse to CASCAD MENU Icon location succeeded\r");
                Mouse_Click();
                sendMessagetoConsole("STEP 12, click mouse to CASCAD MENU Icon succeeded\r");
                
                //////////////////////////////////////////////////////////////////////////////////////
                sendMessagetoConsole("STEP 13, Looking for RESTORE PLAY STORE Icon\r");
                location = lookForImageRoutine(Properties.Resources.restore_play_store, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 13, RESTORE PLAY STORE page found\r");
                else
                {
                    sendMessagetoConsole("STEP 13, RESTORE PLAY STORE not found\r");
                    sendMessagetoConsole("STEP 13, Exception: System will be interupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 14, moving mouse to RESTORE PLAY STORE location\r");
                Cursor.Position = new Point(location.X + 150, location.Y);
                sendMessagetoConsole("STEP 15, mouse click on RESTORE PLAY STORE\r");
                mouse_event((uint)MouseEventFlags.LEFTDOWN, 0, 0, 0, 0);
                inputSimulator.Keyboard.Sleep(1000);
                mouse_event((uint)MouseEventFlags.LEFTUP, 0, 0, 0, 0);
                
                ////////////////////////////////////////////////
                sendMessagetoConsole("STEP 16, Looking for REMOVE FROM LISTE Icon\r");
                location = lookForImageRoutine(Properties.Resources.remove_from_liste, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 16, REMOVE FROM LISTE Icon found\r");
                else
                {
                    sendMessagetoConsole("STEP 16, REMOVE FROM LISTE Icon not found\r");
                    sendMessagetoConsole("STEP 16, Exception: System will be interrupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 17, moving cursor to REMOVE FROM LISTE icon\r");
                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 18, click mouse on REMOVE FROM LISTE icon\r");
                Mouse_Click();
                #endregion

                ////////////// écriture du lient directement dans la fennetre qui ouvrira le navigateur
                Thread.Sleep(1000);
                sendMessagetoConsole("STEP 19, look for SAY OK GOOGLE icon\r");
                location = lookForImageRoutine(Properties.Resources.say_ok_google, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 19, SAY OK GOOGLE Icon found\r");
                else
                {
                    sendMessagetoConsole("STEP 19, SAY OK GOOGLE Icon not found\r");
                    sendMessagetoConsole("STEP 19, Exception: System will be interupted\r");
                    break;
                }

                Cursor.Position = location;
                //Mouse_Click();
                sendMessagetoConsole("STEP 20, writing url\r");
                sendMessagetoConsole("STEP 20, typing url :" + url_string + "\r");
                inputSimulator.Keyboard.Sleep(1000);
                
                ///////// un probleme fait que le début de l'url "HTTPS" ne s'ecris pas entièrement bizarement
                // on va s'assurer qu'il s'ecris convenablement, apres on ecris le reste de la chaine
                inputSimulator.Keyboard.TextEntry('h');
                inputSimulator.Keyboard.Sleep(500);
                inputSimulator.Keyboard.TextEntry('t');
                inputSimulator.Keyboard.Sleep(500);
                inputSimulator.Keyboard.TextEntry('t');
                inputSimulator.Keyboard.Sleep(500);
                inputSimulator.Keyboard.TextEntry('p');
                inputSimulator.Keyboard.Sleep(500);
                inputSimulator.Keyboard.TextEntry('s');
                inputSimulator.Keyboard.Sleep(500);
                string second_part_in_url = url_string.Substring(5, url_string.Length - 5);
                WriteText(second_part_in_url);
                
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);
                
                
                //////////////////////////// download button
                location = lookForImageRoutine(Properties.Resources.install, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 21, INSTALL button found\r");
                else
                {
                    sendMessagetoConsole("STEP 21, INSTALL button not found\r");
                    sendMessagetoConsole("STEP 21, Exception: System will be interupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 22, move mouse to INSTALL button location\r");
                Cursor.Position = new Point(location.X + 10, location.Y + 10);
                sendMessagetoConsole("STEP 23, mouse click on INSTALL button\r");
                Mouse_Click();
                
                ////////////    accepte
                sendMessagetoConsole("STEP 24, looking for ACCEPTE button\r");
                location = lookForImageRoutine(Properties.Resources.accepte, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 24, ACCEPTE found\r");
                else
                {
                    sendMessagetoConsole("STEP 24, ACCEPTE button not found\r");
                    sendMessagetoConsole("STEP 24, Exception: System will be interupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 25, move mouse to ACCEPTE button location\r");
                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 26, mouse click on ACCEPTE button\r");
                Mouse_Click();
                sendMessagetoConsole("STEP 27, DOWNLOAD started\r");
                inputSimulator.Keyboard.Sleep(1000);
                
                ////////////////////////////////// wait for game to be downloaded
                sendMessagetoConsole("STEP 28, looking for OPEN button\r");
                location = lookForImageRoutine(Properties.Resources.open, 3600);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 28, OPEN button found\r");
                else
                {
                    sendMessagetoConsole("STEP 28, OPEN button not found\r");
                    sendMessagetoConsole("STEP 28, Exception: System will be interupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 28, game downloaded sucessfuly\r");

                // uninstaling app
                Cursor.Position = new Point(location.X - 150, location.Y);
                Mouse_Click();

                Thread.Sleep(500);

                // waiting app uninstalled
                sendMessagetoConsole("STEP 28, looking for ok button\r");
                location = lookForImageRoutine(Properties.Resources.ok, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 28, install button found\r");
                else
                {
                    sendMessagetoConsole("STEP 28, install button not found\r");
                    sendMessagetoConsole("STEP 28, Exception: System will be interupted\r");
                    break;
                }

                Cursor.Position = new Point(location.X, location.Y);
                Mouse_Click();

                Thread.Sleep(500);

                //// waiting install button
                sendMessagetoConsole("STEP 28, looking for install button\r");
                location = lookForImageRoutine(Properties.Resources.install, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 28, install button found\r");
                else
                {
                    sendMessagetoConsole("STEP 28, install button not found\r");
                    sendMessagetoConsole("STEP 28, Exception: System will be interupted\r");
                    break;
                }

                ////////////////// check if client need rating
                Thread.Sleep(2000);
                sendMessagetoConsole("STEP 29, check if client need rating\r");
                if (url_rate)
                {
                    sendMessagetoConsole("STEP 29, client need rating, look for stars\r");
                    for (int cnt2 = 0; cnt2 < 7; cnt2++)
                    {
                        inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
                        inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);
                    }

                    inputSimulator.Keyboard.Sleep(2000);

                    sendMessagetoConsole("STEP 29, looking 5 STARS icon\r");
                    location = lookForImageRoutine(Properties.Resources._5stars, 60);
                    if (location != Point.Empty)
                        sendMessagetoConsole("STEP 29, 5 STARS icon found\r");
                    else
                    {
                        sendMessagetoConsole("STEP 29, 5 STARS icon not found\r");
                        sendMessagetoConsole("STEP 29, Exception: System will be interupted\r");
                        break;
                    }

                    sendMessagetoConsole("STEP 30, moving cursor to the 5th Stars\r");
                    Cursor.Position = new Point(location.X + 190, location.Y + 52);
                    Mouse_Click();
                    sendMessagetoConsole("STEP 31, 5th Stars button clicked\r");

                    // validating rate
                    sendMessagetoConsole("STEP 32, looking SUBMIT label\r");
                    location = lookForImageRoutine(Properties.Resources.submit_comment, 60);
                    if (location != Point.Empty)
                        sendMessagetoConsole("STEP 32, SUBMIT label found\r");
                    else
                    {
                        sendMessagetoConsole("STEP 32, SUBMIT label not found\r");
                        sendMessagetoConsole("STEP 32, Exception: System will be interupted\r");
                        break;
                    }

                    sendMessagetoConsole("STEP 32, moving cursor to the SUBMIT button\r");
                    Cursor.Position = new Point(location.X, location.Y);
                    Mouse_Click();
                    sendMessagetoConsole("STEP 32, SUBMIT button clicked\r");

                    Thread.Sleep(500);
                    bool g = false;

                    for(int cnt2 = 0; cnt2 < 4; cnt2++)
                    {
                        inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
                        inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);
                    }

                    for (int cnt2 = 0; cnt2 < 15; cnt2++)
                    {
                        inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
                        inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);

                        sendMessagetoConsole("STEP 33, looking +G button\r");
                        location = lookForImageRoutine(Properties.Resources.g_, 4);
                        if (location != Point.Empty)
                        {
                            sendMessagetoConsole("STEP 33, +G button found\r");
                            g = true;
                            break;
                        }
                    }

                    if(g)
                    {
                        sendMessagetoConsole("STEP 33, moving cursor to the +G button\r");
                        Cursor.Position = new Point(location.X, location.Y);
                        Mouse_Click();
                        sendMessagetoConsole("STEP 33, +G button clicked\r");
                    }
                    else
                    {
                        sendMessagetoConsole("STEP 33, +G button not found\r");
                    }
                    sendMessagetoConsole("STEP 33, DOWNLOAD / RATING / G+ Finished\r");
                    sendMessagetoConsole("STEP 34, cleaning device steps\r");
                }

                //////////////////////////////////////////////////////////////////////////
                sendMessagetoConsole("STEP 35, Looking for cascad menu Icon\r");
                location = lookForImageRoutine(Properties.Resources.cascad_menu, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 36, cascad menu page found\r");
                else
                {
                    sendMessagetoConsole("STEP 36, cascad menu not found\r");
                    sendMessagetoConsole("STEP 36, Exception: System will be interupted\r");
                }

                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 37, moved mouse to CASCAD MENU Icon location succeeded\r");
                Mouse_Click();
                sendMessagetoConsole("STEP 38, click mouse to CASCAD MENU Icon succeeded\r");

                //////////////////////////////////////////////////////////////////////////////////////
                sendMessagetoConsole("STEP 39, Looking for RESTORE PLAY STORE Icon\r");
                location = lookForImageRoutine(Properties.Resources.restore_play_store, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 39, RESTORE PLAY STORE page found\r");
                else
                {
                    sendMessagetoConsole("STEP 39, RESTORE PLAY STORE not found\r");
                    sendMessagetoConsole("STEP 39, Exception: System will be interupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 40, moving mouse to RESTORE PLAY STORE location\r");
                Cursor.Position = new Point(location.X + 150, location.Y);
                sendMessagetoConsole("STEP 41, mouse click on RESTORE PLAY STORE\r");
                mouse_event((uint)MouseEventFlags.LEFTDOWN, 0, 0, 0, 0);
                inputSimulator.Keyboard.Sleep(1000);
                mouse_event((uint)MouseEventFlags.LEFTUP, 0, 0, 0, 0);

                ////////////////////////////////////////////////
                sendMessagetoConsole("STEP 42, Looking for REMOVE FROM LISTE Icon\r");
                location = lookForImageRoutine(Properties.Resources.remove_from_liste, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 42, REMOVE FROM LISTE Icon found\r");
                else
                {
                    sendMessagetoConsole("STEP 42, REMOVE FROM LISTE Icon not found\r");
                    sendMessagetoConsole("STEP 42, Exception: System will be interrupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 43, moving cursor to REMOVE FROM LISTE icon\r");
                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 44, click mouse on REMOVE FROM LISTE icon\r");
                Mouse_Click();

                finish:
                /// setting
                sendMessagetoConsole("STEP 45, Looking for SETTING Icon\r");
                location = lookForImageRoutine(Properties.Resources.setting, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 45, SETTING Icon found\r");
                else
                {
                    sendMessagetoConsole("STEP 45, SETTING Icon not found\r");
                    sendMessagetoConsole("STEP 45, Exception: System will be interrupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 46, moving cursor to SETTING icon\r");
                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 47, click mouse on SETTING icon\r");
                Mouse_Click();

                Thread.Sleep(2000);
                // descendre jusqu'au compte google
                sendMessagetoConsole("STEP 48, seeking setting button\r");
                for (int cnt2 = 0; cnt2 < 14; cnt2++)
                {
                    inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
                    inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);
                    Thread.Sleep(100);
                }

                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);

                Thread.Sleep(1000);

                sendMessagetoConsole("STEP 49, seeking google account button\r");
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);

                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);

                sendMessagetoConsole("STEP 50, seeking advanced setting account button\r");
                location = lookForImageRoutine(Properties.Resources.advanced_setting, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 50, advanced setting Icon found\r");
                else
                {
                    sendMessagetoConsole("STEP 50, advanced setting Icon not found\r");
                    sendMessagetoConsole("STEP 50, Exception: System will be interrupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 51, moving cursor to advanced setting icon\r");
                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 52, click mouse on SETTING icon\r");
                Mouse_Click();

                Thread.Sleep(500);
                sendMessagetoConsole("STEP 53, seeking REMOVE ACCOUNT button\r");
                location = lookForImageRoutine(Properties.Resources.remove_account, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 53, REMOVE ACCOUNT Icon found\r");
                else
                {
                    sendMessagetoConsole("STEP 53, REMOVE ACCOUNT Icon not found\r");
                    sendMessagetoConsole("STEP 53, Exception: System will be interrupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 54, moving cursor to REMOVE ACCOUNT icon\r");
                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 55, click mouse on REMOVE ACCOUNT icon\r");
                Mouse_Click();

                Thread.Sleep(1000);
                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.TAB);

                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.TAB);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.TAB);

                inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);

                sendMessagetoConsole("STEP 59, ACCOUNT removed successfuly\r");
                sendMessagetoConsole("STEP 60, remove account activity\r");
                //////////////////////////////////////////////////////////////////////////
                sendMessagetoConsole("STEP 61, Looking for cascad menu Icon\r");
                location = lookForImageRoutine(Properties.Resources.cascad_menu, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 61, cascad menu page found\r");
                else
                {
                    sendMessagetoConsole("STEP 61, cascad menu not found\r");
                    sendMessagetoConsole("STEP 61, Exception: System will be interupted\r");
                }

                sendMessagetoConsole("STEP 62, moved mouse to CASCAD MENU Icon location succeeded\r");
                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 63, click mouse to CASCAD MENU Icon succeeded\r");
                Mouse_Click();
                
                //////////////////////////////////////////////////////////////////////////////////////
                sendMessagetoConsole("STEP 64, Looking for Setting Icon\r");
                location = lookForImageRoutine(Properties.Resources.setting2, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 64, Setting found\r");
                else
                {
                    sendMessagetoConsole("STEP 64, Setting not found\r");
                    sendMessagetoConsole("STEP 64, Exception: System will be interupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 65, moving mouse to RESTORE Setting location\r");
                Cursor.Position = new Point(location.X + 150, location.Y);
                sendMessagetoConsole("STEP 66, mouse click on Setting\r");
                mouse_event((uint)MouseEventFlags.LEFTDOWN, 0, 0, 0, 0);
                inputSimulator.Keyboard.Sleep(1000);
                mouse_event((uint)MouseEventFlags.LEFTUP, 0, 0, 0, 0);

                ////////////////////////////////////////////////
                sendMessagetoConsole("STEP 42, Looking for REMOVE FROM LISTE Icon\r");
                location = lookForImageRoutine(Properties.Resources.remove_from_liste, 60);
                if (location != Point.Empty)
                    sendMessagetoConsole("STEP 42, REMOVE FROM LISTE Icon found\r");
                else
                {
                    sendMessagetoConsole("STEP 42, REMOVE FROM LISTE Icon not found\r");
                    sendMessagetoConsole("STEP 42, Exception: System will be interrupted\r");
                    break;
                }

                sendMessagetoConsole("STEP 43, moving cursor to REMOVE FROM LISTE icon\r");
                Cursor.Position = new Point(location.X, location.Y);
                sendMessagetoConsole("STEP 44, click mouse on REMOVE FROM LISTE icon\r");
                Mouse_Click();

                // decrementation de loop du l'url
                mySqlConn = new MySqlConn();
                mySqlConn.cmd.CommandText = "update submited_url set submited_url.loop = submited_url.loop - 1 where id = '" + url_id + "'";
                mySqlConn.reader = mySqlConn.cmd.ExecuteReader();

                mySqlConn.Dispose();
                mySqlConn = null;

                ///////// incrementation du compte choisie pour choisir un autre compte non utilisé
                account_id++;
            }
        }
        public Bitmap CaptureScreen()
        {
            var image = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
            var gfx = Graphics.FromImage(image);
            gfx.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
            return image;
        }
        public void sendMessagetoConsole(string message)
        {
            if (!this.IsDisposed)
            {
                this.BeginInvoke((Action)(() =>
                {
                    console.AppendText(message);
                    consoleMoveCursorInTheEnd();
                }));
            }
        }
        public void Mouse_Click()
        {
            mouse_event((uint)MouseEventFlags.LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(30);
            mouse_event((uint)MouseEventFlags.LEFTUP, 0, 0, 0, 0);
        }
        /*public bool FindBitmap(Bitmap bmpNeedle, Bitmap bmpHaystack, out Point location)
        {
            bmpHaystack = applySobelFilter(bmpHaystack);

            // cherche la presense d'une image a l'intérieur d'une autre image
            for (int outerX = 0; outerX < bmpHaystack.Width - bmpNeedle.Width; outerX++)
            {
                for (int outerY = 0; outerY < bmpHaystack.Height - bmpNeedle.Height; outerY++)
                {
                    for (int innerX = 0; innerX < bmpNeedle.Width; innerX++)
                    {
                        for (int innerY = 0; innerY < bmpNeedle.Height; innerY++)
                        {
                            Color cNeedle = bmpNeedle.GetPixel(innerX, innerY);
                            Color cHaystack = bmpHaystack.GetPixel(innerX + outerX, innerY + outerY);

                            if (cNeedle.R != cHaystack.R || cNeedle.G != cHaystack.G || cNeedle.B != cHaystack.B)
                            {
                                goto notFound;
                            }
                        }
                    }
                    location = new System.Drawing.Point(outerX, outerY);
                    return true;
                    notFound:
                    continue;
                }
            }
            location = System.Drawing.Point.Empty;
            return false;
        }*/
        public int ReturnTimeStamp()
        {
            return (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        public void consoleMoveCursorInTheEnd()
        {
            // mettre le curseur sur la fin du chatbox
            console.SelectionLength = 0;
            console.SelectionStart = console.TextLength;
            console.ScrollToCaret();
        }
        public void ReadConfigFile()
        {
            #region lecture du fichier config.ini
            string[] configFile = System.IO.File.ReadAllLines(@"Config.ini");
            for (int cnt = 0; cnt < configFile.Length; cnt++)
            {
                if (configFile[cnt] != string.Empty && configFile[cnt].Substring(0, 2) != "//" && configFile[cnt].IndexOf(':') != -1)
                {
                    string[] dataLine = configFile[cnt].Split(':');
                    dataLine[0] = dataLine[0].Replace(" ", "");
                    dataLine[1] = dataLine[1].Replace(" ", "");
                    
                    if (dataLine[0] == "db")
                        db = dataLine[1];
                    else if (dataLine[0] == "user")
                        user = dataLine[1];
                    else if (dataLine[0] == "pass")
                        pass = dataLine[1];
                    else if (dataLine[0] == "hostdb")
                        hostdb = dataLine[1];
                    else if (dataLine[0] == "botname")
                        botname = dataLine[1];
                }
            }
            #endregion
            Console.WriteLine("Variables chargés depuis le fichier Config.ini [ok]");
        }
        public class MySqlConn : System.IDisposable
        {
            // pour une créer une instance de connexion Mysql
            public IDbConnection conn;
            public IDbCommand cmd;
            public IDataReader reader;                 // pour créer des cmd SQL
            public MySqlDataReader readerSql = null;

            public MySqlConn()
            {
                MySqlOpenConn();
            }

            public void MySqlOpenConn()
            {
                if (conn != null)
                    conn.Close();
                
                // initialisation de la connexion
                string connStr = String.Format("server={0};user id={1}; password={2}; database={3}; pooling=false",
                                               hostdb, user, pass, db);
                try
                {
                    conn = new MySqlConnection(connStr);
                    conn.Open();
                    cmd = conn.CreateCommand();

                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error [0x0016] connecting to the server: " + ex.Message);
                }

            }

            public void MysqlDisconnect()
            {
                conn.Close();
            }

            public void Dispose()
            {
                this.MysqlDisconnect();
            }
        }
        public bool WriteText(string s)
        {
            InputSimulator inputSimulator = new InputSimulator();
            for (int cnt = 0; cnt < s.Length; cnt++)
            {
                inputSimulator.Keyboard.TextEntry(s[cnt]);
                inputSimulator.Keyboard.Sleep(new Random().Next(50, 100));
            }
            return false;
        }
        
        public static List<Point> GetSubPositions(Bitmap main, Bitmap sub)
        {
            List<Point> possiblepos = new List<Point>();
            int mainwidth = main.Width;
            int mainheight = main.Height;

            int subwidth = sub.Width;
            int subheight = sub.Height;

            int movewidth = mainwidth - subwidth;
            int moveheight = mainheight - subheight;

            BitmapData bmMainData = main.LockBits(new Rectangle(0, 0, mainwidth, mainheight), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmSubData = sub.LockBits(new Rectangle(0, 0, subwidth, subheight), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int bytesMain = Math.Abs(bmMainData.Stride) * mainheight;
            int strideMain = bmMainData.Stride;
            System.IntPtr Scan0Main = bmMainData.Scan0;
            byte[] dataMain = new byte[bytesMain];
            System.Runtime.InteropServices.Marshal.Copy(Scan0Main, dataMain, 0, bytesMain);

            int bytesSub = Math.Abs(bmSubData.Stride) * subheight;
            int strideSub = bmSubData.Stride;
            System.IntPtr Scan0Sub = bmSubData.Scan0;
            byte[] dataSub = new byte[bytesSub];
            System.Runtime.InteropServices.Marshal.Copy(Scan0Sub, dataSub, 0, bytesSub);

            for (int y = 0; y < moveheight; ++y)
            {
                for (int x = 0; x < movewidth; ++x)
                {
                    MyColor curcolor = GetColor(x, y, strideMain, dataMain);

                    foreach (var item in possiblepos.ToArray())
                    {
                        int xsub = x - item.X;
                        int ysub = y - item.Y;
                        if (xsub >= subwidth || ysub >= subheight || xsub < 0)
                            continue;

                        MyColor subcolor = GetColor(xsub, ysub, strideSub, dataSub);

                        if (!curcolor.Equals(subcolor))
                        {
                            possiblepos.Remove(item);
                        }
                    }

                    if (curcolor.Equals(GetColor(0, 0, strideSub, dataSub)))
                        possiblepos.Add(new Point(x, y));
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(dataSub, 0, Scan0Sub, bytesSub);
            sub.UnlockBits(bmSubData);

            System.Runtime.InteropServices.Marshal.Copy(dataMain, 0, Scan0Main, bytesMain);
            main.UnlockBits(bmMainData);

            return possiblepos;
        }

        private static MyColor GetColor(Point point, int stride, byte[] data)
        {
            return GetColor(point.X, point.Y, stride, data);
        }

        private static MyColor GetColor(int x, int y, int stride, byte[] data)
        {
            int pos = y * stride + x * 4;
            byte a = data[pos + 3];
            byte r = data[pos + 2];
            byte g = data[pos + 1];
            byte b = data[pos + 0];
            return MyColor.FromARGB(a, r, g, b);
        }

        struct MyColor
        {
            byte A;
            byte R;
            byte G;
            byte B;

            public static MyColor FromARGB(byte a, byte r, byte g, byte b)
            {
                MyColor mc = new MyColor();
                mc.A = a;
                mc.R = r;
                mc.G = g;
                mc.B = b;
                return mc;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MyColor))
                    return false;
                MyColor color = (MyColor)obj;
                if (color.A == this.A && color.R == this.R && color.G == this.G && color.B == this.B)
                    return true;
                return false;
            }
        }
    }
}
