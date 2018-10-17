using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tremblay.DatabaseUtilities.Sql.Tests.TestClasses
{
    [Table("Users")]
    public class FakeSearchOptions : ISearchOptions
    {

        [Attributes.Column("Id")]
        public IEnumerable<int> Ids { get; set; }

        [Attributes.Column("Name")]
        public string Name{ get; set; }

    }
}
