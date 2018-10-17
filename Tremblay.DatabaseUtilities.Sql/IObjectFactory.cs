using System.Data;

namespace Tremblay.DatabaseUtilities.Sql
{
    public interface IObjectFactory<TObjectType>
    {
        /// <summary>
        /// Creates an object of the specified type from a data reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        TObjectType FromDataReader(IDataReader reader);
    }
}

