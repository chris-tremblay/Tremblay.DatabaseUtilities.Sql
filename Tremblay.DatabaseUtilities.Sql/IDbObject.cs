using System.Data;

namespace Tremblay.DatabaseUtilities.Sql
{
    public interface IDbObject
    {
        object FromDataReader(IDataReader reader);
    }

    public interface IDbObject<TType> : IDbObject
    {
        TType FromDataReaderInstance(IDataReader reader);
    }
}