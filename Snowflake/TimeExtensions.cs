using System;

namespace Snowflake
{
    public static class System
    {
		/// <summary>
		/// Set InternalCurrentTimeMillis as default function to obtain current time 
		/// </summary>
        public static Func<long> currentTimeFunc = InternalCurrentTimeMillis;
 
		/// <summary>
		/// Obtain current time based using 'set' current time function
		/// </summary>
		/// <returns></returns>
        public static long CurrentTimeMillis()
        {
            return currentTimeFunc();
        }

		/// <summary>
		/// A way to override current time for testing purpose.
		/// </summary>
		/// <param name="func">A function that returns the desired current time</param>
		/// <returns>An action that resets currentTimeFunc</returns>
        public static IDisposable StubCurrentTime(Func<long> func)
        {
            currentTimeFunc = func;
            return new DisposableAction(() =>
            {
                currentTimeFunc = InternalCurrentTimeMillis;
            });  
        }

		/// <summary>
		/// A way to override current time for testing purpose.
		/// </summary>
		/// <param name="millis">Desired current time</param>
		/// <returns>An action that resets currentTimeFunc</returns>
        public static IDisposable StubCurrentTime(long millis)
        {
            currentTimeFunc = () => millis;
            return new DisposableAction(() =>
            {
                currentTimeFunc = InternalCurrentTimeMillis;
            });
        }

		/// <summary>
		/// Default epoch.
		/// </summary>
        private static readonly DateTime Jan1st1970 = new DateTime
           (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Default function that returns current time.
		/// </summary>
		/// <returns>Current time</returns>
        private static long InternalCurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }        
    }
}
