using Microsoft.EntityFrameworkCore;

namespace RPGuild.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
