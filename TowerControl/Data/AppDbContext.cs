using Microsoft.EntityFrameworkCore;
using TowerControl.Data.DTO;

namespace TowerControl.Data
{
    public sealed class AppDbContext : DbContext
    {
        public DbSet<PlaneDTO> Planes { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            // удаляем и создаем базу при каждом запуске приложения
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source = data.db");
        }
    }
}
