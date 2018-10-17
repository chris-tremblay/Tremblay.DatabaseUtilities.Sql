using System.Data.SqlClient;

namespace Tremblay.DatabaseUtilities.Sql
{
    public interface ISqlDatabase
    {

        #region Properties
    
        string ConnectionString { get; set; }
        
        #endregion 

        #region Methods

        SqlConnection CreateConnection();

        #endregion

    }
}
