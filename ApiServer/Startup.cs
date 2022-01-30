using ApiServer.Infrastructure;
using ApiServer.Infrastructure.Models;
using ApiServer.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer
{
    public class Startup
    {
        readonly CyclicChecks cyclicChecks;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            AppSettings settings = new AppSettings();
            Configuration = configuration;
            configuration.Bind(settings);

            cyclicChecks = new CyclicChecks(settings);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.UseMemberCasing();
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApiServer50", Version = "v1" });
            });

            // biniding della classe AppSettings definita in Infrastructure con il json
            var config = new AppSettings();
            Configuration.Bind(config);
            services.AddSingleton(config);

            services.AddDbContext<sshondemandContext>(options =>
                    options.UseNpgsql(config.DbConnectionString));

            services.AddTransient<Ssh>();

            // esempio di aggiunta di un repository standard, senza customizzazioni
            services.AddTransient<BaseRepository<ClientConnection>>();

            // aggiunta di un repo custom
            services.AddTransient<ClientRepository>();
            services.AddTransient<ClientConnectionsRepository>();
            services.AddTransient<DeveloperAuthorizationsRepository>();
            services.AddTransient<DeviceRequestsRepository>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiServer50 v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
         
        

        }
    }
}
