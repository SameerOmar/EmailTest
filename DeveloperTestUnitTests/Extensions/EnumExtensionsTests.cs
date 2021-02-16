using DeveloperTest.Mesic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeveloperTestUnitTests.Extensions
{
    [TestClass]
    public class EnumExtensionsTests
    {
        [TestMethod]
        public void EnumGetDescriptionTest()
        {
            // Arrange

            // Act
            var description1 = TestEnum.Test1.GetDescription();
            var description2 = TestEnum.Test2.GetDescription();

            // Assert
            Assert.AreEqual("Test1", description1);
            Assert.AreEqual("Test2", description2);
        }

        private enum TestEnum
        {
            [System.ComponentModel.Description("Test1")]
            Test1,

            [System.ComponentModel.Description("Test2")]
            Test2
        }
    }
}