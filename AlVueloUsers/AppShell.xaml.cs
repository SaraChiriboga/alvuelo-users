namespace AlVueloUsers
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("RegistroPage", typeof(Views.RegistroPage));

        }
    }
}
