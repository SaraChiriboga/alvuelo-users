namespace AlVueloUsers
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Usar AppShell como MainPage para respetar Shell.NavBarIsVisible = "False"
            MainPage = new AppShell();
        }
    }
}