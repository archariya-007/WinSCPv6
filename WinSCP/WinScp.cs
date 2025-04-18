using System;
using System.IO;

namespace WinSCP
{
    public class WinScp
    {
        //Target Framework: .NET 4.0, WinSCP.6.3.7 [04/2025] - including 2 more parameters to the method PerformFTP, sshPrivateKeyPath and privateKeyPassphrase
        //int protocol, string hostName, string userName, string password

        public string PerformFTP(bool isSecure, bool isActiveFtpMode, string hostName, string userName, string password, string fileName, string destinationFolder, int portNumber, string sshServerKey, string tempFolder)
        {
            string ftpLogMessage = string.Empty;
            string _sshPrivateKeyPath = @"\\bc-share-p01\EDIShare\Tools and Public keys\benefitExpress2048_private.ppk";
            string _privateKeyPassphrase = "whoami";
            string[] _sftp3HsaBank = new[] { "sftp3.hsabank.com" };
            try
            {
                //TODO: Create a bC4 staging svn repository and include this winSCP project repository.
                ftpLogMessage = "FTP Successful...\n";

                var protocol = Protocol.Ftp;
                var ftpMode = FtpMode.Passive;
                // var sourceFile = @"C:\BenefitsConnect\temp\" + fileName;
                // replaced with dynamic folder name:
                var sourceFile = tempFolder + fileName;

                if (isSecure)
                {
                    protocol = Protocol.Sftp;
                }

                if (isActiveFtpMode)
                {
                    ftpMode = FtpMode.Active;
                }

                //TODO:Come up with a simple algorythm that will set a default timeout based on file size
                //var timeOut = FunctionThatCalculatesTimeOut(sourceFile);
                // Setup session options
                var sessionOptions = new SessionOptions();
                // Setup session options
                sessionOptions.Protocol = protocol;
                sessionOptions.HostName = hostName;
                sessionOptions.UserName = userName;
                sessionOptions.Password = password;
                sessionOptions.PortNumber = portNumber;
                sessionOptions.FtpMode = ftpMode;
                sessionOptions.Timeout = new TimeSpan(0, 5, 0);     //5 minutes

                // including sshPrivateKeyPath and privateKeyPassphrase [04/2025]
                foreach (var sftp in _sftp3HsaBank)
                {
                    if (hostName == sftp)
                    {
                        sessionOptions.SshPrivateKeyPath = _sshPrivateKeyPath;
                        sessionOptions.PrivateKeyPassphrase = _privateKeyPassphrase;
                        break;
                    }
                }
                

                if (protocol == Protocol.Sftp)
                {
                    sessionOptions.SshHostKeyFingerprint = sshServerKey;
                }
                // else
                //{
                // sessionOptions.GiveUpSecurityAndAcceptAnySshHostKey = true;
                // }

                using (var session = new Session())
                {
                    // Create directory if it doesn't exist
                    string logDirectory = @"C:\WinSCPSessionLog";
                    if (!Directory.Exists(logDirectory))
                        Directory.CreateDirectory(logDirectory);

                    session.SessionLogPath = Path.Combine(logDirectory, "sessionlog.log");

                    // Connect
                    session.Open(sessionOptions);

                    // Upload files
                    var transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Automatic;
                    transferOptions.PreserveTimestamp = false;
                    // Russ: added bz some servers don't allow to rename the file once it was created.
                    transferOptions.ResumeSupport.State = TransferResumeSupportState.Off;

                    var cleanDestinationFolder = CleanFTPPath(destinationFolder);

                    var transferResult = session.PutFiles(sourceFile, cleanDestinationFolder, false, transferOptions);

                    // Throw on any error
                    transferResult.Check();

                    // Print results
                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                    {
                        ftpLogMessage += "The following file was delivered: " + transfer.FileName + "\n";
                        ftpLogMessage += "Time of delivery: " + DateTime.Now + "\n";
                    }
                }

                return ftpLogMessage;
            }
            catch (Exception exception)
            {
                ftpLogMessage = "Error Processing Scheduled Export. Error Message: " + exception.Message;
                return ftpLogMessage;
            }
        }

        private string CleanFTPPath(string path)
        {
            // Russ: added trimming as some folders contain spaces from copypasting
            var cleanPath = path.Trim();

            if (string.IsNullOrEmpty(cleanPath))
            {
                cleanPath = "/";
            }
            else
            {
                if (path.Contains("\\"))
                {
                    cleanPath = path.Replace("\\", "/");
                }

                if (!cleanPath.StartsWith("/"))
                {
                    cleanPath = "/" + cleanPath;
                }

                if (!cleanPath.EndsWith("/"))
                {
                    cleanPath = cleanPath + "/";
                }

            }

            return cleanPath;
        }
    }
}