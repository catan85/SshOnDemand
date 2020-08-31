using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiServer
{
    public class Startup
    {
        CyclicChecks cyclicChecks = new CyclicChecks();

        public Startup(IConfiguration configuration)
        {

            AppSettings settings = new AppSettings();

            Configuration = configuration;
            configuration.Bind(settings);

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                // I comandi seguenti sono per tornare i risultati in PascalCase invece che camelCase
                .AddJsonOptions(jsonOptions => jsonOptions.UseMemberCasing())
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            //services.AddAuthentication();
            //services.AddMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
        

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseHttpsRedirection();

            app.UseMvc();
        }
    }
}
