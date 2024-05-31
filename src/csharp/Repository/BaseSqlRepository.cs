using Microsoft.Extensions.Logging;
using System.Data.SqlClient;

namespace RvSfDownloadCore.Repository
{
    /// <summary>
    /// Базовый класс MS SQL репозитория.
    /// Хранит строку подключения.
    /// Оборачивает вызов SQL-команды и пишет в лог
    ///     - начало команды
    ///     - завершение команды
    ///     - ошибку выполнения команды(если была)
    /// </summary>
    public class BaseSqlRepository
    {
        // IConfiguration config
        protected ILogger _logger;

        protected string? ConnectionString  = null;
        private SqlConnection? _sqlConnection;

        public BaseSqlRepository(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void SetConnectionString(string connectionString) => ConnectionString = connectionString;

        public void OpenConnection()
        {
            _sqlConnection = CreateNewSqlConnection();
            _sqlConnection.Open();
        }

        public void CloseConnection()
        {
            if (_sqlConnection == null)
                return;

            using (_sqlConnection)
            {
                ;
            }

            _sqlConnection = null;
        }

        protected SqlConnection CreateNewSqlConnection() => new SqlConnection(ConnectionString);

        protected void ExecSql(string Message, Action<SqlConnection> action)
        {
            if (_sqlConnection != null)
            {
                ExecSqlExec(Message, action, _sqlConnection);
            }
            else
            {
                using (var con = CreateNewSqlConnection())
                {
                    ExecSqlExec(Message, action, con);
                }
            }

        }

        protected void ExecSqlExec(string Message, Action<SqlConnection> action, SqlConnection con)
        {
            try
            {
                _logger.LogTrace($"Start [{Message}]");

                action(con);

                _logger.LogTrace($"Finish [{Message}]");
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, $"Ошибка [{Message}]. Exception=[{ex.Message}]");
                throw;
            }
        }
    }
}
