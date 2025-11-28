using Book_Store.API.Utitlies.DBInitilizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace Book_Store.API
{
    public static class AppConfiguration
    {
        public static void RegisterConfig(this IServiceCollection services, string connection)
        {
            services.AddDbContext<ApplicationDbContext>(option =>
            {

                option.UseSqlServer(connection);
            });

            services.AddIdentity<ApplicationUser, IdentityRole>(option =>
            {
                option.User.RequireUniqueEmail = true;
                option.Password.RequiredLength = 8;
                option.SignIn.RequireConfirmedEmail = true;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login"; // Default login path
                options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Default access denied path
            });

            services.AddTransient<IEmailSender, EmailSender>();


            services.AddScoped<IRepository<Category>, Repository<Category>>();
            services.AddScoped<IRepository<Book>, Repository<Book>>();
            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<IRepository<ApplicationUserOTP>, Repository<ApplicationUserOTP>>();
            services.AddScoped<IRepository<Cart>, Repository<Cart>>();
            services.AddScoped<IRepository<Promotion>, Repository<Promotion>>();
            services.AddScoped<IRepository<PromotionUsage>, Repository<PromotionUsage>>();
            services.AddScoped<IRepository<Order>, Repository<Order>>();


            services.AddScoped<IDBInitializer, DBInitializer>();

        }
    }
}
