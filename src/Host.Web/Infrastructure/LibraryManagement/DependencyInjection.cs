using Domain.LibraryManagement.Loans;
using Domain.LibraryManagement.Reservations;
using Application.Infrastructure.LibraryManagement.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Infrastructure.LibraryManagement;

public static class DependencyInjection
{
    public static IServiceCollection AddLibraryManagementServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ILoanRepository, EfLoanRepository>();
        services.AddScoped<IReservationRepository, EfReservationRepository>();

        return services;
    }
}

