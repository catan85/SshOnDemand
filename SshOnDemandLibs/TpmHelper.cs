using System;
using System.Collections.Generic;
using System.Text;
using Tpm2Lib;

namespace SshOnDemandLibs
{
    public class TpmHelper
    {
        // Alcuni riferimenti:
        // https://trustedcomputinggroup.org/wp-content/uploads/TCG_TPM2_r1p59_Part1_Architecture_pub.pdf
        // pagina 107
        // Gli handle sono delle entità che identificano la posizione in memoria del processore TPM
        // si tratta di valori unsigned a 32 bit
        // l'ottetto più significativo identifica il tipo di risorsa, 
        // gli altri 24 bit identificano invece l'indirizzo della risorsa di quel tipo
        // Possono essere sia in memoria RAM che in memoria Non Volatile
        // Quando si parla di NV ci si riferisce alla memoria non volatile

        // Di seguito un riassunto dei principali handles ed il relativo indirizzo nell'ottetto più significativo
        // 00 PCR sono i platform configuration registry, registri di configurazione specifici di un dispositivo
        // 01 NV index, memorie non volatili
        // 02 e 03 Session Handles, handle delle sessioni (ad esempio di una sessione HMAC)
        // 40 Permanent resources Handles (handle di risorse permanenti come ad esempio l'Owner, la Piattaforma,  
        //      l'Endorsment (chiave RSA che identifica il device stesso, di fabbrica) il valore di autorizzazione di lockout
        // 80 Transient object Handles (handle di risorse transienti). Sono risorse che possono essere caricate in memoria
        //      ram all'occorrenza quando un oggetto è utilizzato. Vengono usate anche per cambiare la persistenza di un oggetto
        //      tramite funzione TPM2_EvictControl(). All'avvio, al richiamo della funzione  TPM2_Startup() le risorse possono
        //      essere ripulite tramite la funzione TPM2_FlushContext()
        // 81 Persistent object Handles (handle di risorse persistenti). Sono oggetti che sono stati resi persistenti tramite la
        //      funzione TPM2_EvictControl(). Per poter rendere persistenete un oggetto è necessario avere la Platform Authorization
        //      o la Owner Authorization

        // https://trustedcomputinggroup.org/wp-content/uploads/TCG_TPM2_r1p59_Part1_Architecture_pub.pdf
        //  A pagina 110 una tabella degli indirizzi

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

        public byte[] SignHmac(byte[] signatureByteArray, int v)
        {
            throw new NotImplementedException();
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


        // Codice reperito dal seguente repo:
        // https://github.com/ms-iot/security/blob/master/Limpet/Limpet.NET/Limpet.cs

        private const UInt32 AIOTH_PERSISTED_URI_INDEX = 0x01400100;
        private const UInt32 AIOTH_PERSISTED_KEY_HANDLE = 0x81000100;
        private const UInt32 SRK_HANDLE = 0x81000001;
        private const UInt32 logicalDeviceId = 0;
        private const string hostName = "localhost";
        private const string deviceId = "device1";


        /// <summary>
        /// Funzione per il salvataggio della chiave privata da utilizzare per firmare con HMAC tramite TPM
        /// </summary>
        /// <param name="encodedHmacKey"></param>
        public static void SaveHmacKey(string encodedHmacKey)
        {
            // Definizione area di memoria non volatile nel TPM
            TpmHandle nvHandle = new TpmHandle(AIOTH_PERSISTED_URI_INDEX + logicalDeviceId);

            // Definizione dell'handle contenente l'Owner nel TPM
            TpmHandle ownerHandle = new TpmHandle(TpmRh.Owner);

            // Definizione dell'handle per la memorizzazione dell'oggetto HMAC
            TpmHandle hmacKeyHandle = new TpmHandle(AIOTH_PERSISTED_KEY_HANDLE + logicalDeviceId);

            // Definizione dell'handle della Storage Root Key, si tratta della chiave 
            // principale utilizzata per il salvataggio di altre chiavi. Ogni chiave salvata 
            // nel TPM infatti viene cifrata utilizzando la sua chiave "padre".
            // La SRK è la chiave più alta dell'albero
            TpmHandle srkHandle = new TpmHandle(SRK_HANDLE);
            UTF8Encoding utf8 = new UTF8Encoding();

            // dati descrittivi dell'host e del device id
            byte[] nvData = utf8.GetBytes(hostName + "/" + deviceId);

            // chiave privata che intendiamo memorizzare nel TPM
            byte[] hmacKey = System.Convert.FromBase64String(encodedHmacKey);

            // Apertura del TPM
            Tpm2Device tpmDevice = new TbsDevice();
            tpmDevice.Connect();
            var tpm = new Tpm2(tpmDevice);

            // Definizione dello store Non volatile
            // Il primo parametro è l'Owner TPM
            // il terzo parametro è la funzione HMAC che intendiamo salvare 
            // (NvPublic sta per Non volatile public area)
            tpm.NvDefineSpace(ownerHandle,
                              new byte[0],
                              new NvPublic(
                                  nvHandle,
                                  TpmAlgId.Sha256,
                                  NvAttr.Authwrite | NvAttr.Authread | NvAttr.NoDa,
                                  new byte[0],
                                  (ushort)nvData.Length));

            // Scrittura nello store non volatile della funzione HMAC
            tpm.NvWrite(nvHandle, nvHandle, nvData, 0);

            // Importazione della chiave HMAC sotto la Storage Root Key
            TpmPublic hmacPub;
            CreationData creationData;
            byte[] creationhash;
            TkCreation ticket;

            // Passaggio della chiave privata
            var sensitiveCreate = new SensitiveCreate(new byte[0], hmacKey);

            // Definizione dell'uso che si farà della chiave
            var tpmPublic = new TpmPublic(
                TpmAlgId.Sha256,
                ObjectAttr.UserWithAuth | ObjectAttr.NoDA | ObjectAttr.Sign,
                new byte[0],
                new KeyedhashParms(new SchemeHmac(TpmAlgId.Sha256)),
                new Tpm2bDigestKeyedhash());

            // Salvataggio della chiave privata nel tpm
            TpmPrivate hmacPrv = tpm.Create(
                srkHandle, 
                sensitiveCreate,   
                tpmPublic,
                new byte[0],
                new PcrSelection[0],
                out hmacPub,
                out creationData,
                out creationhash,
                out ticket);

            // Caricamento della chiave HMAC nel TPM
            TpmHandle loadedHmacKey = tpm.Load(srkHandle, hmacPrv, hmacPub);

            // Salvataggio della chiave nella memoria Non Volatile
            tpm.EvictControl(ownerHandle, loadedHmacKey, hmacKeyHandle);

            // Flush degli oggetti transienti dal tpm
            tpm.FlushContext(loadedHmacKey);
        }


        /// <summary>
        /// Funzione per la pulizia di una chiave e funzione HMAC precedentementi salvati nel TPM
        /// </summary>
        public static void CleanOldHmacKey()
        {
            // Apertura del TPM
            Tpm2Device tpmDevice = new TbsDevice();
            tpmDevice.Connect();
            var tpm = new Tpm2(tpmDevice);

            TpmHandle ownerHandle = new TpmHandle(TpmRh.Owner);
            TpmHandle nvHandle = new TpmHandle(AIOTH_PERSISTED_URI_INDEX + logicalDeviceId);
            TpmHandle hmacKeyHandle = new TpmHandle(AIOTH_PERSISTED_KEY_HANDLE + logicalDeviceId);

            // Undefine dello spazio utilizzato per la chiave HMAC
            tpm.NvUndefineSpace(ownerHandle, nvHandle);

            // Rimozione della funzione HMAC
            tpm.EvictControl(ownerHandle, hmacKeyHandle, hmacKeyHandle);
        }


        /// <summary>
        /// Funzione per la firma HMAC tramite TPM
        /// </summary>
        /// <param name="dataToSign"></param>
        /// <returns></returns>
        public static Byte[] SignHmac(Byte[] dataToSign)
        {
            TpmHandle hmacKeyHandle = new TpmHandle(AIOTH_PERSISTED_KEY_HANDLE + logicalDeviceId);
         
            int dataIndex = 0;
            Byte[] iterationBuffer;
            Byte[] hmac = { };

            // Se i valori da firmare sono < 1024 byte
            if (dataToSign.Length <= 1024)
            {
                try
                {
                    // Apertura del TPM
                    Tpm2Device tpmDevice = new TbsDevice();
                    tpmDevice.Connect();
                    var tpm = new Tpm2(tpmDevice);

                    // Calcolo dell'HMAC tramite la funzione salvata in precedenza
                    hmac = tpm.Hmac(hmacKeyHandle, dataToSign, TpmAlgId.Sha256);

                    // Dispose del TPM
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
                    // Apertura del TPM
                    Tpm2Device tpmDevice = new TbsDevice();
                    tpmDevice.Connect();
                    var tpm = new Tpm2(tpmDevice);

                    // Inizio della sequenza HMAC
                    Byte[] hmacAuth = new byte[0];
                    TpmHandle hmacHandle = tpm.HmacStart(hmacKeyHandle, hmacAuth, TpmAlgId.Sha256);

                    // ciclo su tutti i dati da firmare a blocchi da 1024 byte
                    while (dataToSign.Length > dataIndex + 1024)
                    {
                        // Repeat to update the hmac until we only hace <=1024 bytes left
                        iterationBuffer = new Byte[1024];
                        Array.Copy(dataToSign, dataIndex, iterationBuffer, 0, 1024);

                        // Caricamento dei dati nel tpm (calcolo parziale)
                        tpm.SequenceUpdate(hmacHandle, iterationBuffer);
                        dataIndex += 1024;
                    }

                    // Caricamento della parte finale 
                    iterationBuffer = new Byte[dataToSign.Length - dataIndex];
                    Array.Copy(dataToSign, dataIndex, iterationBuffer, 
                        0, dataToSign.Length - dataIndex);
                    TkHashcheck nullChk;

                    // Si finalizza l'HMAC con l'ultima parte dei dati
                    hmac = tpm.SequenceComplete(hmacHandle, iterationBuffer, 
                        TpmHandle.RhNull, out nullChk);

                    // Dispose del TPM
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
