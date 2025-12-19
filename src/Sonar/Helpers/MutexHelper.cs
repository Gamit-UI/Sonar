namespace Sonar.Helpers;

internal static class MutexHelper
{
    public static async Task<T> ExecuteOnceAsync<T>(string id, Func<Task<T>> task) where T : struct
    {
        try
        {
            var hasHandle = false;
            var mutexId = $"Global\\{{{id}}}";
            using var sequentialScheduler = new SequentialScheduler();
            using var mutex = new Mutex(initiallyOwned: false, mutexId, createdNew: out _);
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        hasHandle = mutex.WaitOne(millisecondsTimeout: 0, exitContext: false);
                    }
                    catch (AbandonedMutexException)
                    {
                        hasHandle = true;
                    }
                }, CancellationToken.None, TaskCreationOptions.None, sequentialScheduler);

                if (hasHandle == false)
                {
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine("The process is already running. Press any key to continue...");
                        Console.ReadLine();
                    }
                    
                    return default;
                }

                return await task();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (hasHandle)
                {
                    await Task.Factory.StartNew(() => mutex.ReleaseMutex(), CancellationToken.None, TaskCreationOptions.None, sequentialScheduler);
                }

                mutex.Dispose();
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Cannot run with limited privileges. Exiting...");
        }

        return default;
    }
}