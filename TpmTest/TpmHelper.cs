using System;
using System.Collections.Generic;
using System.Text;
using Tpm2Lib;

namespace TpmTest
{
    class TpmHelper
    {

        public static AuthValue authValue = new AuthValue(new byte[] { 22, 123, 22, 1, 33 });
        public static void SaveValueIntoTpm(int address, byte[] data, int length)
        {
            Tpm2Device tpmDevice;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                tpmDevice = new TbsDevice();
            }
            else
            {
                tpmDevice = new LinuxTpmDevice();
            }
            tpmDevice.Connect();

            var tpm = new Tpm2(tpmDevice);

            var ownerAuth = new AuthValue();
            TpmHandle nvHandle = TpmHandle.NV(address);

            tpm[ownerAuth]._AllowErrors().NvUndefineSpace(TpmHandle.RhOwner, nvHandle);

            AuthValue nvAuth = authValue;
            var nvPublic = new NvPublic(nvHandle, TpmAlgId.Sha1, NvAttr.Authwrite | NvAttr.Authread, new byte[0], (ushort)length);
            tpm[ownerAuth].NvDefineSpace(TpmHandle.RhOwner, nvAuth, nvPublic);

            tpm[nvAuth].NvWrite(nvHandle, nvHandle, data, 0);
            tpm.Dispose();
        }

        public static byte[] ReadValueFromTpm(int address, int length)
        {
            Tpm2Device tpmDevice;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                tpmDevice = new TbsDevice();
            }
            else
            {
                tpmDevice = new LinuxTpmDevice();
            }
            tpmDevice.Connect();
            var tpm = new Tpm2(tpmDevice);
            TpmHandle nvHandle = TpmHandle.NV(address);
            AuthValue nvAuth = authValue;
            byte[] newData = tpm[nvAuth].NvRead(nvHandle, nvHandle, (ushort)length, 0);
            tpm.Dispose();
            return newData;
        }

        private const UInt32 AIOTH_PERSISTED_URI_INDEX = 0x01400100;
        private const UInt32 AIOTH_PERSISTED_KEY_HANDLE = 0x81000100;
        private const UInt32 SRK_HANDLE = 0x81000001;
        private const UInt32 logicalDeviceId = 0;

        private const string hostName = "localhost";
        private const string deviceId = "device1";

        public static void SaveHmacKey(string encodedHmacKey)
        {
            TpmHandle nvHandle = new TpmHandle(AIOTH_PERSISTED_URI_INDEX + logicalDeviceId);
            TpmHandle ownerHandle = new TpmHandle(TpmRh.Owner);
            TpmHandle hmacKeyHandle = new TpmHandle(AIOTH_PERSISTED_KEY_HANDLE + logicalDeviceId);
            TpmHandle srkHandle = new TpmHandle(SRK_HANDLE);
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] nvData = utf8.GetBytes(hostName + "/" + deviceId);
            byte[] hmacKey = System.Convert.FromBase64String(encodedHmacKey);

            // Open the TPM
            Tpm2Device tpmDevice = new TbsDevice();
            tpmDevice.Connect();
            var tpm = new Tpm2(tpmDevice);

            // Define the store
   
            tpm.NvDefineSpace(ownerHandle,
                              new byte[0],
                              new NvPublic(nvHandle,
                                           TpmAlgId.Sha256,
                                           NvAttr.Authwrite | NvAttr.Authread | NvAttr.NoDa,
                                           new byte[0],
                                           (ushort)nvData.Length));

            // Write the store
            tpm.NvWrite(nvHandle, nvHandle, nvData, 0);

            // Import the HMAC key under the SRK
            TpmPublic hmacPub;
            CreationData creationData;
            byte[] creationhash;
            TkCreation ticket;
            TpmPrivate hmacPrv = tpm.Create(srkHandle,
                                            new SensitiveCreate(new byte[0],
                                                                hmacKey),
                                            new TpmPublic(TpmAlgId.Sha256,
                                                          ObjectAttr.UserWithAuth | ObjectAttr.NoDA | ObjectAttr.Sign,
                                                          new byte[0],
                                                          new KeyedhashParms(new SchemeHmac(TpmAlgId.Sha256)),
                                                          new Tpm2bDigestKeyedhash()),
                                            new byte[0],
                                            new PcrSelection[0],
                                            out hmacPub,
                                            out creationData,
                                            out creationhash,
                                            out ticket);

            // Load the HMAC key into the TPM
            TpmHandle loadedHmacKey = tpm.Load(srkHandle, hmacPrv, hmacPub);

            // Persist the key in NV
            tpm.EvictControl(ownerHandle, loadedHmacKey, hmacKeyHandle);

            // Unload the transient copy from the TPM
            tpm.FlushContext(loadedHmacKey);
        }

        public static Byte[] SignHmac(Byte[] dataToSign, uint address)
        {
            TpmHandle hmacKeyHandle = new TpmHandle(AIOTH_PERSISTED_KEY_HANDLE + logicalDeviceId);
         

            int dataIndex = 0;
            Byte[] iterationBuffer;
            Byte[] hmac = { };

            if (dataToSign.Length <= 1024)
            {
                try
                {
                    // Open the TPM
                    Tpm2Device tpmDevice = new TbsDevice();
                    tpmDevice.Connect();
                    var tpm = new Tpm2(tpmDevice);

                    // Calculate the HMAC in one shot
                    hmac = tpm.Hmac(hmacKeyHandle, dataToSign, TpmAlgId.Sha256);

                    // Dispose of the TPM
                    tpm.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return hmac;
                }
            }
            else
            {
                try
                {
                    // Open the TPM
                    Tpm2Device tpmDevice = new TbsDevice();
                    tpmDevice.Connect();
                    var tpm = new Tpm2(tpmDevice);

                    // Start the HMAC sequence
                    Byte[] hmacAuth = new byte[0];
                    TpmHandle hmacHandle = tpm.HmacStart(hmacKeyHandle, hmacAuth, TpmAlgId.Sha256);
                    while (dataToSign.Length > dataIndex + 1024)
                    {
                        // Repeat to update the hmac until we only hace <=1024 bytes left
                        iterationBuffer = new Byte[1024];
                        Array.Copy(dataToSign, dataIndex, iterationBuffer, 0, 1024);
                        tpm.SequenceUpdate(hmacHandle, iterationBuffer);
                        dataIndex += 1024;
                    }
                    // Finalize the hmac with the remainder of the data
                    iterationBuffer = new Byte[dataToSign.Length - dataIndex];
                    Array.Copy(dataToSign, dataIndex, iterationBuffer, 0, dataToSign.Length - dataIndex);
                    TkHashcheck nullChk;
                    hmac = tpm.SequenceComplete(hmacHandle, iterationBuffer, TpmHandle.RhNull, out nullChk);

                    // Dispose of the TPM
                    tpm.Dispose();
                }
                catch
                {
                    return hmac;
                }
            }

            return hmac;
        }
    }
}
