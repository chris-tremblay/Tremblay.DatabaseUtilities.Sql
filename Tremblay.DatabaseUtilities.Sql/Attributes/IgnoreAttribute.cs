
using System;

namespace Tremblay.DatabaseUtilities.Sql.Attributes
{
    /// <summary>
    /// Instructs the SearchOptions extensions to ignore the specified property while generating a sql statement.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute
    {
            
    }
}
