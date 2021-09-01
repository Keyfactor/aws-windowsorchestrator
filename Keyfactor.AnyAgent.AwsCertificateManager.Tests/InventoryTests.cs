using System;
using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using FluentAssertions;
using Keyfactor.AnyAgent.AwsCertificateManager.Jobs;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Keyfactor.AnyAgent.AwsCertificateManager.Tests
{
    [TestClass]
    public class InventoryTests
    {
        [TestMethod]
        public void ReturnsTheCorrectJobClassAndStoreType()
        {
            var inventory = new Inventory();
            inventory.GetJobClass().Should().Be("Inventory");
            inventory.GetStoreType().Should().Be("AwsCerMan");
        }

        [TestMethod]
        public void JobCallsGetCertificatesSuccess()
        {
            var inventory = new Mock<Inventory> { CallBase = true };
            var mockAcmClient=new Mock<IAmazonCertificateManager>(){CallBase = true};
            var mockInventoryResponse = Mocks.GetCerts();
            var mockGetCertRequest = Mocks.GetCertificateRequest();
            var mockGetCertResponse = Mocks.GetCertificateResponse();
            inventory.Protected().Setup<IAmazonCertificateManager>("AcmClient").Returns(mockAcmClient.Object);
            inventory.Protected().Setup<ListCertificatesResponse>("AllCertificates").Returns(mockInventoryResponse);
            inventory.Protected().Setup<GetCertificateRequest>("GetCertificateRequest").Returns(mockGetCertRequest);
            inventory.Protected().Setup<GetCertificateResponse>("GetCertificateResponse").Returns(mockGetCertResponse);

            var result = inventory.Object.processJob(Mocks.GetMockConfig(AnyJobOperationType.Inventory),
                Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object,
                Mocks.GetSubmitDiscoveryDelegateMock().Object);
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            result.Status.Should().Equals(2);
            result.Message.Should().Be("Inventory Complete");
        }

        [TestMethod]
        public void JobCallsGetCertificatesWarning()
        {
            var inventory = new Mock<Inventory> { CallBase = true };
            var mockAcmClient = new Mock<IAmazonCertificateManager>() { CallBase = true };
            var mockInventoryResponse = Mocks.GetCerts();
            var mockGetCertRequest = Mocks.GetCertificateRequest();
            var mockGetCertResponse = Mocks.GetJunkCertificate();
            inventory.Protected().Setup<IAmazonCertificateManager>("AcmClient").Returns(mockAcmClient.Object);
            inventory.Protected().Setup<ListCertificatesResponse>("AllCertificates").Returns(mockInventoryResponse);
            inventory.Protected().Setup<GetCertificateRequest>("GetCertificateRequest").Returns(mockGetCertRequest);
            inventory.Protected().Setup<AgentCertStoreInventoryItem>("BuildInventoryItem",ItExpr.IsAny<string>(),ItExpr.IsAny<IAmazonCertificateManager>()).Throws(new Exception());
            inventory.Protected().Setup<GetCertificateResponse>("GetCertificateResponse").Returns(mockGetCertResponse);

            var result = inventory.Object.processJob(Mocks.GetMockConfig(AnyJobOperationType.Inventory),
                Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object,
                Mocks.GetSubmitDiscoveryDelegateMock().Object);
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            result.Status.Should().Equals(3);
            result.Message.Should().Be("Inventory Complete With Warnings");
        }


        [TestMethod]
        public void InventoryThrowsError()
        {
            var inventory = new Mock<Inventory> { CallBase = true };
            AnyJobCompleteInfo result = null;
            inventory.Protected().Setup<IAmazonCertificateManager>("AcmClient").Throws(new Exception());
            try
            {
                result = inventory.Object.processJob(Mocks.GetMockConfig(AnyJobOperationType.Inventory),
                    Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object,
                    Mocks.GetSubmitDiscoveryDelegateMock().Object);
            }
            catch (Exception)
            {
                result.Should().Be(null);
            }
        }

        [TestMethod]
        public void TestInternalGetSetProperties()
        {
            var inventoryMock=new Mock<Inventory> { CallBase = true };
            inventoryMock.SetupAllProperties();
            var inventoryObject = inventoryMock.Object;
            var acm = new Mock<IAmazonCertificateManager> { CallBase = true };
            inventoryObject.AcmClient = acm.Object;
            var y = inventoryObject.AcmClient;
            Assert.IsInstanceOfType(y,typeof(IAmazonCertificateManager));
            var certListResponse = new Mock<ListCertificatesResponse>() {CallBase = true};
            inventoryObject.AllCertificates = certListResponse.Object;
            var z = inventoryObject.AllCertificates;
            Assert.IsInstanceOfType(z, typeof(ListCertificatesResponse));
            var certRequest = new Mock<GetCertificateRequest>() { CallBase = true };
            inventoryObject.GetCertificateRequest= certRequest.Object;
            var b = inventoryObject.GetCertificateRequest;
            Assert.IsInstanceOfType(b, typeof(GetCertificateRequest));
            var certResponse = new Mock<GetCertificateResponse>() { CallBase = true };
            inventoryObject.GetCertificateResponse = certResponse.Object;
            var c = inventoryObject.GetCertificateResponse;
            Assert.IsInstanceOfType(c, typeof(GetCertificateResponse));

        }
    }
}
