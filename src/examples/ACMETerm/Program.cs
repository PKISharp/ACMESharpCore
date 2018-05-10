using System;
using System.IO;
using Terminal.Gui;

namespace ACMETerm
{
    class Program
    {
        static void Main(string[] args)
        {
            Application.Init();

            var top = Application.Top;

            var win = new Window(new Rect(0, 1, top.Frame.Width, top.Frame.Height - 1), "ACMETerm");
            top.Add(win);

            // Creates a menubar, the item "New" has a help menu.
            var menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_New Account", "Creates a new ACME registration Account", NewAccount),
                    new MenuItem ("_Open Account", "Open an existing ACME registration Account", OpenAccount),
                    new MenuItem ("_Close Account", "Close existing ACME registration Account", CloseAccount),
                    new MenuItem ("_Quit", "", () => ClickQuit())
                }),
                new MenuBarItem ("_Edit", new MenuItem [] {
                    new MenuItem ("_Copy", "", null),
                    new MenuItem ("C_ut", "", null),
                    new MenuItem ("_Paste", "", null)
                })
            });
            top.Add(menu);

            // Add some controls
            var ml = new Label(new Rect(3,17, 47, 1), "Mouse: ");
            win.Add(
                    new Label(3, 2, "Login: "),
                    new TextField(14, 2, 40, ""),
                    new Label(3, 4, "Password: "),
                    new TextField(14, 4, 40, "") { Secret = true },
                    new CheckBox(3, 6, "Remember me"),
                    new RadioGroup(3, 8, new[] { "_Personal", "_Company" }),
                    new Button(3, 14, "Ok"),
                    new Button(10, 14, "Cancel"),
                    new Label(3, 18, "Press ESC and 9 to activate the menubar"),
                    ml);

            int count = 0;
            Application.RootMouseEvent += delegate (MouseEvent ev) {
                ml.Text =  $"Mouse: ({ev.X},{ev.Y}) - {ev.Flags} {count++}";
            };

            Application.Run();

            void ClickQuit()
            {
                if (Quit ())
                {
                    //top.Running = false;
                    Application.RequestStop();
                }
            }
        }

        static void NewAccount()
        {
            var openDlg = new OpenDialog("New Account", "Select a directory to save your Account details:")
            {
                CanChooseDirectories = true,
                CanChooseFiles = false,
                CanCreateDirectories = true,
                DirectoryPath = Directory.GetCurrentDirectory(),
                FilePath = Directory.GetCurrentDirectory(),
            };
            Application.Run(openDlg);
        }

        static void OpenAccount()
        {
        }

        static void CloseAccount()
        { 
        }

        static bool Quit()
        {
            var n = MessageBox.Query (50, 7, "Quit ACMETerm", "Are you sure you want to quit this app?", "Yes", "No");
		    return n == 0;
        }
    }
}
