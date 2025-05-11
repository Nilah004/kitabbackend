using Microsoft.Extensions.DependencyInjection;

namespace Try_application.Services
{
    public static class EmailServiceRegistration
    {
        public static IServiceCollection AddEmailService(this IServiceCollection services)
        {
            services.AddTransient<IEmailService, EmailService>();
            return services;
        }
    }
}
