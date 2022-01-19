using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace VeeamFileHasher
{
    public class FileHandler
    {
        public void CalcHashForFileBlocks(string filePath, long blockSize)
        {
            try
            {
                var fileSize = new System.IO.FileInfo(filePath).Length;
                
                var blockCounts = fileSize / blockSize;
                var lastBlockSize = fileSize % blockSize;
                
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (lastBlockSize > 0)
                    blockCounts ++;

                var getOptimalThreadsCount = CheckOptimalCountThreads(blockSize, fileSize);
                var d = GC.GetTotalMemory(true);
                var listActiveThreads = new List<Thread>();
                var listLazyThreads = new List<KeyValuePair<Thread, FileBlockServiceRequest>>();
                long prevBlockPosition = 0;
                for (var i = 0; i < blockCounts; i++)
                {
                    var countRead = i < blockCounts - 1 ? blockSize : lastBlockSize;
                    if (i < getOptimalThreadsCount)
                    {
                        var newThread = new Thread(new FileBlockService().FileBlockCalcHash);
                        newThread.Start(new FileBlockServiceRequest
                        {
                            BlockNumber = i,
                            SizeBlock = countRead,
                            StartByte = prevBlockPosition,
                            FilePath = filePath
                        });
                        listActiveThreads.Add(newThread);
                    }
                    else
                    {
                        listLazyThreads.Add(new KeyValuePair<Thread, FileBlockServiceRequest>(
                            new Thread(new FileBlockService().FileBlockCalcHash), 
                            new FileBlockServiceRequest
                            {
                                BlockNumber = i,
                                SizeBlock = countRead,
                                StartByte = prevBlockPosition,
                                FilePath = filePath
                            } ));
                    }

                    prevBlockPosition += countRead;
                }

                while (true)
                {
                    if(listLazyThreads.Count == 0)
                        break;
                    
                    var i = 0;
                    while (true)
                    {
                        if(i>= listActiveThreads.Count)
                            break;
                        
                        if (listActiveThreads[i].IsAlive)
                        {
                            i++;
                            continue;
                        }
                        
                        listActiveThreads.Remove(listActiveThreads[i]);

                        var (key, value) = listLazyThreads.First();
                        key.Start(value);

                        listActiveThreads.Add(key);
                            
                        listLazyThreads.RemoveAt(0);
                    }
                }
                
                while (true)
                {
                    if (listActiveThreads.Count > 0 ||
                        listActiveThreads.All(activeThread => activeThread.ThreadState == ThreadState.Stopped))
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public long CheckOptimalCountThreads(long blockSize, long fileLength)
        {
            try
            {
                var blockCounts = fileLength / blockSize;
                var lastBlockSize = fileLength % blockSize;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (lastBlockSize > 0)
                    blockCounts++;

                var t = GC.GetTotalMemory(false);
                
                var listForLoadMemory = new List<byte[]>();
                for (var i = 0; i < blockCounts; i++)
                {
                    try
                    {
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (lastBlockSize > 0 && i == blockSize - 1)
                            listForLoadMemory.Add( new byte [lastBlockSize + lastBlockSize/2]);
                        else
                            listForLoadMemory.Add( new byte [blockSize + blockSize/2]);
                    }
                    catch (Exception)
                    {
                        listForLoadMemory.Clear();
                        return i / 2;
                    }
                }
                listForLoadMemory.Clear();
                var d = GC.GetTotalMemory(false);
                var sum = d - t;
                
                return blockCounts;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                var d = GC.GetTotalMemory(true);
            }
        }
    }
}