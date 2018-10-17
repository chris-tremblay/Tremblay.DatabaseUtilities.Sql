using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tremblay.DatabaseUtilities.Sql.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute
    {
    
        #region Constructors

        public IgnoreAttribute(params SqlAction[] actions)
        {
            IgnoredActions = actions;
        }

        #endregion

        #region Properties

        public SqlAction[] IgnoredActions { get; }

        #endregion 

        #region Public Methods

        public bool IsIgnored(SqlAction action)
            => IgnoredActions?.Contains(action) == true;

        #endregion

    }
}
