using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using GitTfs.Core;

namespace GitTfs.VsCommon
{
    public static class Retry
    {
        public static void Do(Action action)
        {
            Do(action, TimeSpan.FromSeconds(30));
        }

        public static void Do(Action action, TimeSpan retryInterval, int retryCount = 100)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, retryCount);
        }

        public static T Do<T>(Func<T> action)
        {
            return Do(action, TimeSpan.FromSeconds(30));
        }

        public static T Do<T>(Func<T> action, TimeSpan retryInterval, int retryCount = 100)
        {
            var exceptions = new List<Exception>();

            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    return action();
                }
                catch (Microsoft.TeamFoundation.TeamFoundationServerException ex)
                {
                    Trace.WriteLine($"{ex.Message}: Retry #{retry + 1}/{retryCount} in {retryInterval.TotalSeconds} seconds.");
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
                catch (System.Net.WebException ex)
                {
                    Trace.WriteLine($"{ex.Message}: Retry #{retry + 1}/{retryCount} in {retryInterval.TotalSeconds} seconds.");
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
                catch (GitTfsException ex) // allows continue of catch (MappingConflictException e) throw as innerexception
                {
                    Trace.WriteLine($"{ex.Message}: Retry #{retry + 1}/{retryCount} in {retryInterval.TotalSeconds} seconds.");
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
            }

            throw new AggregateException(exceptions);
        }

        public static void DoWhile(Func<bool> action, int retryCount = 100)
        {
            DoWhile(action, TimeSpan.FromSeconds(30), retryCount);
        }

        public static void DoWhile(Func<bool> action, TimeSpan retryInterval, int retryCount = 100)
        {
            int count = 0;
            while (action())
            {
                count++;
                if (count > retryCount)
                    throw new GitTfsException("error: Action failed after " + retryCount + " retries!");
                Trace.WriteLine($"DoWhile: Retry #{count}/{retryCount} in {retryInterval.TotalSeconds} seconds.");
                Thread.Sleep(retryInterval);
            }
        }
    }
}
