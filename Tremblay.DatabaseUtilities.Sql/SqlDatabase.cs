using System.Data.SqlClient;

namespace Tremblay.DatabaseUtilities.Sql
{
    public class SqlDatabase : ISqlDatabase
    {

        #region Constructors

        public SqlDatabase()
        {
            
        }

        public SqlDatabase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        #endregion 

        #region Properties

        public string ConnectionString { get; set; }

        #endregion

        #region Public Methods

        public virtual SqlConnection CreateConnection()
            => new SqlConnection(ConnectionString);

        #endregion
            
    }
}