using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace VeeamFileHasher
{
    public class FileBlockService
    {
        public  async void FileBlockCalcHash(object request)
        {
            try
            {
                var requestUnboxing = (FileBlockServiceRequest)request;
                
                byte[] blockFileBytes = { };
                byte[] bufferForRead = { };
                var sizeBlock = requestUnboxing.EndByte - requestUnboxing.StartByte;
                var notEnoughMemory = true;
                
                while (notEnoughMemory)
                {
                    try
                    {
                        blockFileBytes = new byte[sizeBlock];
                        bufferForRead = sizeBlock > int.MaxValue ? new byte[int.MaxValue] : new byte[sizeBlock];
                        
                        notEnoughMemory = false;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(1);
                    }
                }

                await using (var fs = new FileStream(requestUnboxing.FilePath, FileMode.Open, FileAccess.Read)) 
                {
                    fs.Position = requestUnboxing.StartByte;
                    var actualRead = 0;
                    while (actualRead < sizeBlock)
                    {
                        int countRead;
                        if (sizeBlock - actualRead > int.MaxValue)
                            countRead = int.MaxValue;
                        else
                        {
                            countRead = (int)(sizeBlock - actualRead);
                            //bufferForRead = new byte[countRead];
                        }

                        var currentRead = fs.Read(bufferForRead, actualRead, countRead);

                        var j = 0;
                        for (var i = actualRead; i < countRead; i++)
                        {
                            blockFileBytes[i] = bufferForRead[j];
                            j++;
                        }

                        actualRead += currentRead;
                    }
                }

                var sb = new StringBuilder();
                // ReSharper disable once ConvertToUsingDeclaration
                using (var hash = SHA256.Create())            
                {
                   var result = hash.ComputeHash(blockFileBytes);
                    foreach (var b in result)
                        sb.Append(b.ToString("x2"));
                }
                
                Console.WriteLine($"Block № : {requestUnboxing.BlockNumber} , sha256 = {sb}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }
    }
}