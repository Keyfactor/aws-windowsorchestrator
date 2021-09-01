using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;

namespace Keyfactor.AnyAgent.AwsCertificateManager.Jobs
{
    [Job(JobTypes.Management)]
    public class Management : AgentJob
    {
        private static String certStart = "-----BEGIN CERTIFICATE-----\n";
        private static String certEnd = "\n-----END CERTIFICATE-----";
        private static Func<String, String> pemify = (ss => ss.Length <= 64 ? ss : ss.Substring(0, 64) + "\n" + pemify(ss.Substring(64)));
        protected internal virtual IAmazonCertificateManager AcmClient { get; set; }
        protected internal virtual DescribeCertificateResponse DescribeCertificateResponse { get; set; }
        protected internal virtual ImportCertificateResponse IcrResponse { get; set; }
        protected internal virtual DeleteCertificateResponse DeleteResponse { get; set; }
        protected internal virtual AsymmetricKeyEntry KeyEntry { get; set; }
        

        public override AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory,
            SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            return PerformManagement(config);
        }

        private IAmazonCertificateManager ConfigureAcmClient(AnyJobConfigInfo config)
        {
            string convertedRegion = Regex.Replace(config.Store.StorePath, @"\s+", "-").ToLower();
            string accessKey = config.Server.Username;
            string secretKey = config.Server.Password;

            // Uses the API credentials provided in the Job Info to configure the SDK
            AcmClient = new AmazonCertificateManagerClient(awsAccessKeyId: accessKey, awsSecretAccessKey: secretKey, region: RegionEndpoint.GetBySystemName(convertedRegion));

            return AcmClient;
        }

        private AnyJobCompleteInfo PerformAddition(AnyJobConfigInfo config)
        {
            //Temporarily only performing additions
            try
            {
                using (IAmazonCertificateManager acmClient = ConfigureAcmClient(config))
                {
                    if (!String.IsNullOrWhiteSpace(config.Job.PfxPassword)) // This is a PFX Entry
                    {
                        if (!String.IsNullOrWhiteSpace(config.Job.Alias))
                        {
                            //ARN Provided, Verify It is Not A PCA/Amazon Issued Cert
                            DescribeCertificateResponse = acmClient.DescribeCertificate(config.Job.Alias);

                            if (DescribeCertificateResponse.Certificate.Type != CertificateType.IMPORTED)
                            {
                                return ThrowError(new Exception("Amazon Web Services Certificate Manager only supports overwriting user-imported certificates."), "Management/Add");
                            }
                        }

                        // Load PFX
                        byte[] pfxBytes = Convert.FromBase64String(config.Job.EntryContents);
                        Pkcs12Store p;
                        using (var pfxBytesMemoryStream = new MemoryStream(pfxBytes))
                        {
                            p = new Pkcs12Store(pfxBytesMemoryStream, config.Job.PfxPassword.ToCharArray());
                        }

                        // Extract private key
                        String alias;
                        String privateKeyString;
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (TextWriter streamWriter = new StreamWriter(memoryStream))
                            {
                                PemWriter pemWriter = new PemWriter(streamWriter);

                                alias = (p.Aliases.Cast<String>()).SingleOrDefault(a => p.IsKeyEntry(a));
                                AsymmetricKeyParameter publicKey = p.GetCertificate(alias).Certificate.GetPublicKey();

                                KeyEntry = p.GetKey(alias);//Dont really need alias?
                                if (KeyEntry == null)
                                {
                                    throw new Exception("Unable to retrieve private key");
                                }

                                AsymmetricKeyParameter privateKey = KeyEntry.Key;
                                AsymmetricCipherKeyPair keyPair = new AsymmetricCipherKeyPair(publicKey, privateKey);

                                pemWriter.WriteObject(keyPair.Private);
                                streamWriter.Flush();
                                privateKeyString = Encoding.ASCII.GetString(memoryStream.GetBuffer()).Trim().Replace("\r", "").Replace("\0", "");
                                memoryStream.Close();
                                streamWriter.Close();
                            }
                        }

                        String certPem = certStart + pemify(Convert.ToBase64String(p.GetCertificate(alias).Certificate.GetEncoded())) + certEnd;

                        //Create Memory Stream For Server Cert
                        ImportCertificateRequest icr;
                        using (MemoryStream serverCertStream = CertStringToStream(certPem))
                        {
                            using (MemoryStream privateStream = CertStringToStream(privateKeyString))
                            {
                                using (MemoryStream chainStream = GetChain(p, alias))
                                {
                                    icr = new ImportCertificateRequest();
                                    icr.Certificate = serverCertStream;
                                    icr.PrivateKey = privateStream;
                                    icr.CertificateChain = chainStream;
                                }
                            }
                        }

                        icr.CertificateArn = config.Job.Alias?.Length >= 20 ? config.Job.Alias.Trim() : null; //If an arn is provided, use it, this will perform a renewal/replace

                        IcrResponse = acmClient.ImportCertificate(icr);

                        // Ensure 200 Response
                        if (IcrResponse.HttpStatusCode == HttpStatusCode.OK)
                        {
                            return Success();
                        }
                        else
                        {
                            return ThrowError(new Exception("Failure"), "Management/Add");
                        }
                    }
                    else  // Non-PFX
                    {
                        return ThrowError(new Exception("Certificate Must Be A PFX"), "Management/Add");
                    }
                }
            }
            catch (Exception e)
            {
                return ThrowError(new Exception(e.Message), "Management/Add");

            }
        }

        private AnyJobCompleteInfo PerformRemoval(AnyJobConfigInfo config)
        {
            //Temporarily only performing additions
            try
            {
                using (IAmazonCertificateManager acmClient = ConfigureAcmClient(config))
                {
                    DeleteCertificateRequest deleteRequest = new DeleteCertificateRequest(config.Job.Alias);
                    DeleteResponse = acmClient.DeleteCertificate(deleteRequest);

                    if (DeleteResponse.HttpStatusCode == HttpStatusCode.OK)
                    {
                        return Success();
                    }
                    else
                    {
                        return ThrowError(new Exception("Failure"), "Management/Remove");
                    }
                }
            }
            catch (Exception e)
            {
                return ThrowError(new Exception(e.Message), "Management/Remove");
            }
        }

        private AnyJobCompleteInfo PerformManagement(AnyJobConfigInfo config)
        {
            AnyJobCompleteInfo complete = new AnyJobCompleteInfo()
            {
                Status = 4,
                Message = "Invalid Management Operation"
            };

            if (config.Job.OperationType.ToString() == "Add")
            {
                complete = PerformAddition(config);
            }
            else if (config.Job.OperationType.ToString() == "Remove")
            {
                complete = PerformRemoval(config);
            }

            return complete;
        }

        //Fetch and return the chain for a cert
        private static MemoryStream GetChain(Pkcs12Store store, string alias)
        {
            string ccs = "";

            X509CertificateEntry[] chain = store.GetCertificateChain(alias);

            foreach (X509CertificateEntry chainEntry in chain)
            {
                ccs += certStart + pemify(Convert.ToBase64String(chainEntry.Certificate.GetEncoded())) + certEnd + "\n";
            }

            return CertStringToStream(ccs);
        }

        //Convert String To MemoryStream
        private static MemoryStream CertStringToStream(string certString)
        {
            // Builds a MemoryStream from the Base64 Encoded String Representation of a cert
            byte[] certBytes = Encoding.ASCII.GetBytes(certString);
            return new MemoryStream(certBytes);
        }
    }
}
