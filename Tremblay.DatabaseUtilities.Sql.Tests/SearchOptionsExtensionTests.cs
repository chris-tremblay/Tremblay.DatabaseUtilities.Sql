using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tremblay.DatabaseUtilities.Sql.Tests.TestClasses;

namespace Tremblay.DatabaseUtilities.Sql.Tests
{
    [TestClass]
    public class SearchOptionsExtensionTests
    {
        [TestMethod]
        public void GenerateSearchString_SearchOptions_ReturnsValidString()
        {
            var searchOptions = new FakeSearchOptions
            {
                Ids = new[] {1},
                Name = "John"
            };

            var parameters = new List<object>();
            var searchString = searchOptions.GenerateSearchString(parameters);

            if (parameters.Count != 2)
                Assert.Fail("Invalid Parameters Length.");
                
            Assert.IsTrue(true);
        }
    }
}
