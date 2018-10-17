using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Tremblay.DatabaseUtilities.Sql.Attributes;

namespace Tremblay.DatabaseUtilities.Sql.Tests.TestClasses
{
    [Table("Users")]
    public class TestUser
    {
    
        [Ignore(SqlAction.Insert)]
        [PrimaryKey]
        public int? Id { get; set; }
        
        public string FirstName { get; set; }

        [Attributes.Column("Last Name")]
        public string LastName { get; set; }

        [Ignore(SqlAction.Insert, SqlAction.Update, SqlAction.Select, SqlAction.Delete)]
        public IEnumerable<int> IgnoredList { get; set; }


    }
}
