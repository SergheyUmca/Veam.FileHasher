using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace VeeamFileHasher
{
    public class FileHandler
    {
        private static readonly ConcurrentDictionary<long, ThreadStatus> BypassDictionary =
            new ConcurrentDictionary<long, ThreadStatus>(); 
        
        public void CalcHashForFileBlocks(string filePath, long blockSize)
        {
            var fileSize = new System.IO.FileInfo(filePath).Length;
                
            var blockCounts = fileSize / blockSize;
            var lastBlockSize = fileSize % blockSize;
            
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (lastBlockSize > 0)
                blockCounts ++;

            var getOptimalThreadsCount = CheckOptimalCountThreads(blockSize, fileSize);
            
            var listActiveThreads = new long[getOptimalThreadsCount];
            var listLazyThreads = new List<KeyValuePair<Thread, FileBlockServiceRequest>>();
            long prevBlockPosition = 0;
            for (var i = 0; i < blockCounts; i++)
            {
                BypassDictionary.TryAdd(i, 0);
                var countRead = i < blockCounts - 1 ? blockSize : lastBlockSize;
                if (i < getOptimalThreadsCount)
                {
                    var newThread = new Thread( new FileBlockService(BypassDictionary).FileBlockCalcHash);
                    newThread.Start(new FileBlockServiceRequest
                    {
                        BlockNumber = i,
                        SizeBlock = countRead,
                        StartByte = prevBlockPosition,
                        FilePath = filePath
                    });

                    listActiveThreads[i] = i;
                }
                else
                {
                    listLazyThreads.Add(new KeyValuePair<Thread, FileBlockServiceRequest>(
                        new Thread(new FileBlockService(BypassDictionary).FileBlockCalcHash), 
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
                    if(i == listActiveThreads.Length || listLazyThreads.Count == 0)
                        break;
                    
                    if (BypassDictionary[listActiveThreads[i]] != ThreadStatus.COMPLETE)
                    {
                        if (BypassDictionary[listActiveThreads[i]] == ThreadStatus.ERROR)
                        {
                            var countRead = listActiveThreads[i] < blockCounts - 1 ? blockSize : lastBlockSize;
                            listLazyThreads.Add(new KeyValuePair<Thread, FileBlockServiceRequest>(
                                new Thread(new FileBlockService(BypassDictionary).FileBlockCalcHash), 
                                new FileBlockServiceRequest
                                {
                                    BlockNumber = listActiveThreads[i],
                                    SizeBlock = countRead,
                                    StartByte = prevBlockPosition,
                                    FilePath = filePath
                                } ));
                        }

                        BypassDictionary.TryUpdate(listActiveThreads[i], ThreadStatus.INIT, ThreadStatus.ERROR);
                        
                        i++;
                        continue;
                    }

                    var (key, value) = listLazyThreads.First();
                    key.Start(value);

                    listActiveThreads[i] = value.BlockNumber;

                    listLazyThreads.RemoveAt(0);
                }
            }
            
            while (true)
            {
                if(BypassDictionary.Values.Any(v => v == ThreadStatus.START))
                    continue;
                
                break;
            }
        }


        private long CheckOptimalCountThreads(long blockSize, long fileLength)
        {
            try
            {
                var blockCounts = fileLength / blockSize;
                var lastBlockSize = fileLength % blockSize;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (lastBlockSize > 0)
                    blockCounts++;

                // ReSharper disable once CollectionNeverQueried.Local
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

                return blockCounts;
            }
            finally
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }
        }
    }
}