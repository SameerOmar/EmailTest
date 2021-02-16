using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using DeveloperTest.DataTypes;
using DeveloperTest.Services;
using DeveloperTest.Workers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DeveloperTestUnitTests.Workers
{
    [TestClass]
    public class MainWorkerTests
    {
        private const int MaxConnectionsCount = 5;

        [TestMethod]
        public void StartWorkerConnectorsPoolTest()
        {
            // Arrange
            var worker = new MainWorker();
            var connector = GetConnectorMock();
            var working = true;

            worker.WorkerStopped += (sender, args) => { working = false; };

            // Act
            worker.Start(connector);

            // Assert
            var connectionPoolPropertyInfo = worker.GetType()
                .GetField("_connectorsList", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(connectionPoolPropertyInfo);

            var connectionPool = (List<IEmailServerConnector>) connectionPoolPropertyInfo.GetValue(worker);

            Assert.AreEqual(connectionPool.Count, MaxConnectionsCount);

            while (working)
            {
            }
        }

        [TestMethod]
        public void StartWorkerMessagesEnvelopDownloadedTest()
        {
            // Arrange
            var worker = new MainWorker();
            var connector = GetConnectorMock();
            var working = true;
            var newEmailInfoAddedRaised = false;

            worker.WorkerStopped += (sender, args) => { working = false; };

            // Act
            worker.Start(connector);

            // Assert
            worker.EmailInfoAdded += (sender, emailInfo) =>
            {
                Assert.IsNotNull(emailInfo);
                Assert.IsNotNull(emailInfo.MessageUid);
                Assert.AreEqual(1, emailInfo.MessageUid.GetUidAsLong());

                newEmailInfoAddedRaised = true;
            };

            while (working)
            {
            }

            Assert.IsTrue(newEmailInfoAddedRaised);
        }

        [TestMethod]
        public void StartWorkerMessagesBodyDownloadedTest()
        {
            // Arrange
            var worker = new MainWorker();
            var connector = GetConnectorMock();
            var working = true;
            var emailInfoBodyUpdatedRaised = false;

            worker.WorkerStopped += (sender, args) => { working = false; };

            // Act
            worker.Start(connector);

            // Assert
            worker.EmailInfoBodyUpdated += (sender, emailInfo) =>
            {
                Assert.IsNotNull(emailInfo);
                Assert.IsNotNull(emailInfo.MessageUid);
                Assert.AreEqual(1, emailInfo.MessageUid.GetUidAsLong());
                Assert.IsNotNull(emailInfo.EmailBody);
                Assert.AreEqual("html test", emailInfo.EmailBody.Html);
                Assert.AreEqual("text test", emailInfo.EmailBody.Text);

                emailInfoBodyUpdatedRaised = true;
            };

            while (working)
            {
            }

            Assert.IsTrue(emailInfoBodyUpdatedRaised);
        }

        private IEmailServerConnector GetConnectorMock()
        {
            var connector = new Mock<IEmailServerConnector>();


            connector.Setup(c => c.Connect(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<EncryptionProtocol>()))
                .Returns(true);
            connector.Setup(c => c.Login(It.IsAny<string>(), It.IsAny<SecureString>())).Returns(true);
            connector.Setup(c => c.GetMailEnvelope(It.IsAny<MessageUid>()))
                .Returns(new EmailEnvelop("Test", "test@test.com", "this is a test", DateTime.Now));
            connector.Setup(c => c.GetMessagesUids()).Returns(new List<MessageUid> {new MessageUid(1)});
            connector.Setup(c => c.Clone()).Returns(new Mock<IEmailServerConnector>().Object);
            connector.Setup(c => c.GetMailBodyStructure(It.IsAny<MessageUid>()))
                .Returns(new EmailBody(true, "html test", "text test"));

            return connector.Object;
        }
    }
}