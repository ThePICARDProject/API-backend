using Hangfire;

namespace API_backend.Services.Hangfire
{
    public class HangfireConfig
    {

        public void Config()
        {
            //AddHangfire().AddHangfireServer(). ??

            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage("Database=Hangfire.Sample; Integrated Security=True;");  //replace database name
        }

        /***
         * @returns identifier #
         */
        public String test()
        {
            String Identifier = BackgroundJob.Enqueue(() => Console.WriteLine("Successful background job enqueue!"));
          

            using (var server = new BackgroundJobServer())
            {
                Console.ReadLine();
            }
            
            return Identifier;
        }
    }
}
