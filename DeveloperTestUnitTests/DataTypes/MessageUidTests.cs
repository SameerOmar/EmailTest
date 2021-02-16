using DeveloperTest.DataTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeveloperTestUnitTests.DataTypes
{
    [TestClass]
    public class MessageUidTests
    {
        [TestMethod]
        public void MessageUidStringConstructionTest()
        {
            // Arrange
            const string testString = "ThisIsATest";

            // Act
            var messageUid = new MessageUid(testString);

            // Assert
            Assert.AreEqual(testString, messageUid.GetUidAsString());
        }

        [TestMethod]
        public void MessageUidLongConstructionTest()
        {
            // Arrange
            const long testLong = 12345;

            // Act
            var messageUid = new MessageUid(testLong);

            // Assert
            Assert.AreEqual(testLong, messageUid.GetUidAsLong());
            Assert.AreEqual(testLong.ToString(), messageUid.GetUidAsString());
        }
    }
}