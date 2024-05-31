using Microsoft.Extensions.Configuration;
using NLog;

namespace RvSfDownloadCore.Infrastructure
{
    /// <summary>
    /// Инкапсулирует в себя инфраструктурную работу с:
    /// - чтениеи файла конфигурации
    /// - подключением логгера NLog
    /// - настройку DI-контейнера
    ///
    /// Осуществляет записи в лог о начале и конце работы программы.
    /// Отлавливает неперехваченные исключения и записывает их в лог.
    /// </summary>
    public class HostStarter
    {

        public static void Start(string[] args, Action<IServiceProvider, string[]> action)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            try
            {
                logger.Info("Start");

                CreateInfrastructure(args, action);

                logger.Info("Finish");
            }
            catch (Exception ex)
            {
                logger.Error($"Неперехваченная ошибка [{ex.Message}]");
                throw;
            }
            finally
            {
                LogManager.Flush();
                LogManager.Shutdown();
            }
        }

        private static void CreateInfrastructure(string[] args, Action<IServiceProvider, string[]> action)
        {
            IConfigurationRoot config = ConfigurationHelper.ReadJsonConfig("appsettings.json");

            IServiceProvider servicesProvider = RegisterServices.BuildDi(config);

            using (servicesProvider as IDisposable)
            {
                action(servicesProvider, args);
            }
        }
    }
}
