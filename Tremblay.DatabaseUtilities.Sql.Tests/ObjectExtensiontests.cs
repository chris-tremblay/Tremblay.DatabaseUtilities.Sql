using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tremblay.DatabaseUtilities.Sql.Tests.TestClasses;

namespace Tremblay.DatabaseUtilities.Sql.Tests
{
    [TestClass]
    public class ObjectExtensionTests
    {
        [TestMethod]
        public void GenerateInsertString_User_ReturnsValidString()
        {
            var user = new TestUser
            {
                FirstName = "Chris",
                LastName = "Tremblay"
            };

            var parameters = new List<object>();
            var insert = user.GenerateInsertStatement(parameters);
            
            Assert.IsTrue(!string.IsNullOrEmpty(insert));
        }

        [TestMethod]
        public void GenerateUpdateString_User_ReturnsValidString()
        {
            var user = new TestUser
            {
                FirstName = "Chris",
                Id = 1,
                LastName = "Tremblay"
            };

            var parameters = new List<object>();
            var update = user.GenerateUpdateStatement(parameters);

            Assert.IsTrue(!string.IsNullOrEmpty(update));
        }
    }
}
