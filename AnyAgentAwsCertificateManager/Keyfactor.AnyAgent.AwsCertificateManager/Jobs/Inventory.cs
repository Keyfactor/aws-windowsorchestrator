using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Enums;

[assembly: InternalsVisibleTo("Keyfactor.AnyAgent.AwsCertificateManager.Tests")]

namespace Keyfactor.AnyAgent.AwsCertificateManager.Jobs
{
    [Job(JobTypes.Inventory)]
    public class Inventory : AgentJob
    {

        protected internal virtual IAmazonCertificateManager AcmClient { get; set; }
        protected internal virtual ListCertificatesResponse AllCertificates { get; set; }
        protected internal virtual GetCertificateRequest GetCertificateRequest { get; set; }
        protected internal virtual GetCertificateResponse GetCertificateResponse { get; set; }


        public override AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory,
            SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            return PerformInventory(config, submitInventory);
        }
        
        private IAmazonCertificateManager ConfigureACMClient(AnyJobConfigInfo config)
        {

            string convertedRegion = Regex.Replace(config.Store.StorePath, @"\s+", "-").ToLower();
            string accessKey = config.Server.Username;
            string secretKey = config.Server.Password;

            // Uses the API credentials provided in the Job Info to configure the SDK
            IAmazonCertificateManager acmClient = new AmazonCertificateManagerClient(awsAccessKeyId: accessKey, awsSecretAccessKey: secretKey, region: RegionEndpoint.GetBySystemName(convertedRegion));
            return acmClient;
        }

        private string GetCertificateFromArn(string arn, IAmazonCertificateManager acmClient)
        {
            GetCertificateRequest = new GetCertificateRequest(arn);
            GetCertificateResponse = acmClient.GetCertificate(GetCertificateRequest);
            return GetCertificateResponse.Certificate;
        }

        //Remove Anchor Tags From Encoded Cert
        private string RemoveAnchors(string base64Cert)
        {
            return base64Cert.Replace("\r", "")
                .Replace("-----BEGIN CERTIFICATE-----\n", "")
                .Replace("\n-----END CERTIFICATE-----\n", "");
        }

        protected virtual AgentCertStoreInventoryItem BuildInventoryItem(string alias, IAmazonCertificateManager acmClient)
        {
            string certificate = GetCertificateFromArn(alias, acmClient);
            string base64Cert = RemoveAnchors(certificate);
            AgentCertStoreInventoryItem acsi = new AgentCertStoreInventoryItem()
            {
                Alias = alias,
                Certificates = new[] { base64Cert },
                ItemStatus = AgentInventoryItemStatus.Unknown,
                PrivateKeyEntry = true,
                UseChainLevel = false
            };

            return acsi;
        }

        private AnyJobCompleteInfo PerformInventory(AnyJobConfigInfo config, SubmitInventoryUpdate siu)
        {
            bool warningFlag=false;
            int totalCertificates = 0;
            StringBuilder sb = new StringBuilder();
            sb.Append("");
            try
            {
                AcmClient = ConfigureACMClient(config);
                ListCertificatesRequest req = new ListCertificatesRequest();

                //The Current Workaround For AWS Not Returning Certs Without A SAN
                List<String> keyTypes = new List<String> { KeyAlgorithm.RSA_1024, KeyAlgorithm.RSA_2048, KeyAlgorithm.RSA_4096, KeyAlgorithm.EC_prime256v1, KeyAlgorithm.EC_secp384r1, KeyAlgorithm.EC_secp521r1 };
                req.Includes = new Filters() { KeyTypes = keyTypes };

                //Only fetch certificates that have been issued at one point
                req.CertificateStatuses = new List<string> { CertificateStatus.ISSUED, CertificateStatus.INACTIVE, CertificateStatus.EXPIRED, CertificateStatus.REVOKED };
                req.MaxItems = 100;

                List<AgentCertStoreInventoryItem> inventoryItems = new List<AgentCertStoreInventoryItem>();
                
                do
                {
                    AllCertificates = AcmClient.ListCertificates(req); //Fetch batch of certificates from ACM API

                    totalCertificates += AllCertificates.CertificateSummaryList.Count;
                    Logger.DebugFormat($"Found {AllCertificates.CertificateSummaryList.Count} Certificates In Batch Amazon Certificate Manager Job.");

                    inventoryItems.AddRange(AllCertificates.CertificateSummaryList.Select(
                        c =>
                        {
                            try
                            {
                                return BuildInventoryItem(c.CertificateArn, AcmClient);
                            }
                            catch
                            {
                                Logger.WarnFormat($"Could not fetch the certificate: {c?.DomainName} associated with arn {c?.CertificateArn}.");
                                sb.Append($"Could not fetch the certificate: {c?.DomainName} associated with arn {c?.CertificateArn}.{Environment.NewLine}");
                                warningFlag = true;
                                return new AgentCertStoreInventoryItem();
                            }
                        }).Where(acsii => acsii?.Certificates != null).ToList());

                    req.NextToken = AllCertificates.NextToken;

                } while (AllCertificates.NextToken != null);

                Logger.DebugFormat($"Found {totalCertificates} Total Certificates In Amazon Certificate Manager Job.");

                siu.Invoke(inventoryItems);

                if (warningFlag)
                {
                    return Warning();
                }
                else
                {
                    return Success();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }
    }
}
