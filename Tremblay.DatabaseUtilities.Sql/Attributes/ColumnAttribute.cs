

using System;

namespace Tremblay.DatabaseUtilities.Sql.Attributes
{
    /// <summary>
    /// Contains information about the column that will be used in SQL generation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {

        #region Constructors

        public ColumnAttribute(string name, string equalityMethod = "=")
        {
            Name = name;

            EqualityMethod = equalityMethod;
        }

        #endregion

        #region Properties

        public string EqualityMethod { get; set; }

        public string Name { get; set; }

        #endregion 

    }
}
