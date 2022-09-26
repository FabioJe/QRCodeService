using QRCodeService.DataPools.FileSystem;
using QRCodeService.DataPools.MySql;

namespace QRCodeService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            //Todo Datainterface

            IDataInterface? dataInterface = null;
            var strType = builder.Configuration["StorageService"];
            string[] adminKeys = builder.Configuration.GetSection("AdminKeys").Get<string[]>();
            if (strType.Equals("file", StringComparison.CurrentCultureIgnoreCase))
            {
                dataInterface = new FileSystemStorage(builder.Configuration.GetConnectionString("File"), adminKeys);
            }
            else if (strType.Equals("mysql", StringComparison.CurrentCultureIgnoreCase))
            {
                dataInterface = new MySqlData(builder.Configuration.GetConnectionString("MySql"),builder.Configuration.GetConnectionString("MySqlPrefix"), adminKeys);
            } else
            {
                throw new Exception($"No known storage service ({strType}) specified! Use 'mysql' or 'file'!");
            }

            builder.Services.AddSingleton(y => dataInterface);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger / OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}