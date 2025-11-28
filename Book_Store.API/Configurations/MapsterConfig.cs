using Mapster;

namespace Book_Store.API.Configurations
{
    public static class MapsterConfig
    {
        public static void RegisterMapsterConfig(this IServiceCollection services)
        {
            TypeAdapterConfig<ApplicationUser, UserResponse>
                    .NewConfig()
                    .Map(d => d.FullName, s => $"{s.FirstName} {s.LastName}")
                    .TwoWays();

            TypeAdapterConfig<ApplicationUser, ApplicationUserResponse>
                    .NewConfig()
                    .Map(d => d.FullName, s => $"{s.FirstName} {s.LastName}")
                    .TwoWays();
        }
    }
}