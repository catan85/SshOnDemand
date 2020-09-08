using Renci.SshNet;
using SshOnDemandLibs.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SshOnDemandLibs.Ssh
{
    public class SftpHelper
    {
        public void DownloadFile(SshConnectionData connectionData, string remoteFilePath, string localFilePath, out bool fileExists)
        {
            using (var sftp = new SftpClient(connectionData.Host, connectionData.Username, connectionData.Password))
            {
                fileExists = false;

                sftp.Connect();

                var remoteFolder = System.IO.Path.GetDirectoryName(remoteFilePath);
                var remoteFileName = System.IO.Path.GetFileName(remoteFilePath);

                var files = sftp.ListDirectory(remoteFolder);
                
                foreach (var file in files)
                {
                    if (Path.GetFileName(file.FullName) == remoteFileName)
                    {
                        fileExists = true;
                    }
                }

                using (Stream streamFile = File.OpenWrite(localFilePath))
                {
                    sftp.DownloadFile(remoteFilePath, streamFile);
                }
            }
        }

        public void UploadFile(SshConnectionData connectionData, string localFilePath, string remoteFilePath)
        {
            using (var sftp = new SftpClient(connectionData.Host, connectionData.Username, connectionData.Password))
            {
                sftp.Connect();
                var remoteFolder = System.IO.Path.GetDirectoryName(remoteFilePath);
                sftp.CreateDirectory(remoteFolder);//Create folder if necessary else skip

                using (Stream file1 = new FileStream(localFilePath, FileMode.Open))
                {
                    sftp.UploadFile(file1, remoteFilePath, null);
                }
      
            }
        }
    }
}
