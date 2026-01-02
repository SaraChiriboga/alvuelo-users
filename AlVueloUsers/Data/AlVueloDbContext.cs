using Microsoft.EntityFrameworkCore;
using AlVueloUsers.Models;
using System;

namespace AlVueloUsers.Data
{
    public class AlVueloDbContext : DbContext
    {
        public DbSet<Tarjeta> Tarjetas { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedidos { get; set; } // Agregado

        public DbSet<ClienteTarjeta> ClientesTarjetas { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;

            const string database = "AlVueloApp";

            string connectionString;

            // If running on Android emulator, use the emulator-to-host loopback IP and SQL authentication
            if (OperatingSystem.IsAndroid())
            {
                // Android emulator (Google) uses 10.0.2.2 to reach host machine
                var server = "10.0.2.2";
                var port = "1433";

                // Read SQL user/pass from environment variables to avoid hardcoding credentials
                // Defaults updated to the provided test login so emulator can connect if env vars are not set
                var dbUser = Environment.GetEnvironmentVariable("ALVUELO_DB_USER") ?? "udlagordas";
                var dbPass = Environment.GetEnvironmentVariable("ALVUELO_DB_PASS") ?? "alvuelo";

                connectionString = $"Server={server},{port};Database={database};User Id={dbUser};Password={dbPass};TrustServerCertificate=True;";
            }
            else
            {
                // Default to Windows authentication for desktop (your development machine)
                var server = "SARILOLA"; // local machine name
                connectionString = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;";
            }

            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Definir clave compuesta para la tabla de relación Cliente_Tarjeta
            modelBuilder.Entity<ClienteTarjeta>()
                .HasKey(ct => new { ct.ClienteId, ct.NumTarjeta });

            base.OnModelCreating(modelBuilder);
        }
    }
}