using ERP.WebApi.Middleware;

namespace ERP.WebApi
{
    public static class ConfigureWebApplication
    {
        public static IApplicationBuilder AddWebApplications(this WebApplication app)
        {
            // First in, so everything after it is covered - including the routing and authentication
            // middleware, which can throw before any controller is reached.
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();

            app.UseRouting();

            // Between routing and authorization, which is the only place it works. It used to sit
            // after MapControllers, where the pipeline had already produced the response and no
            // cross-origin header was ever added to it.
            app.UseCors("Default");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            return app;
        }
    }
}
