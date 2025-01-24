using Microsoft.EntityFrameworkCore;
using quote_cs.Models;

namespace quote_cs.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Quote> Quotes { get; set; }
    }
}
