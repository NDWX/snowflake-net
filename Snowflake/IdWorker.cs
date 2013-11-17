/** Copyright 2010-2012 Twitter, Inc.*/

/**
 * An object that generates IDs.
 * This is broken into a separate class in case
 * we ever want to support multiple worker threads
 * per process
 */

using System;

namespace Snowflake
{
    public class IdWorker
    {
		public const long Twepoch = 1356998400000L;

        const int WorkerIdBits = 5;
        const int DatacenterIdBits = 5;
        const int SequenceBits = 12;
        const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
        const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);

        private const int WorkerIdShift = SequenceBits;
        private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
        public const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        private long _sequence = 0L;
        private long _lastTimestamp = -1L;
		
		// synchronization object
		readonly object _lock = new Object();
	
        public IdWorker(long workerId, long datacenterId, long sequence = 0L) 
        {
            WorkerId = workerId;
            DatacenterId = datacenterId;
            _sequence = sequence;
		
            // sanity check for workerId
            if (workerId > MaxWorkerId || workerId < 0) 
            {
                throw new ArgumentException( String.Format("worker Id can't be greater than {0} or less than 0", MaxWorkerId) );
            }

			// sanity check for data centre id
            if (datacenterId > MaxDatacenterId || datacenterId < 0)
            {
                throw new ArgumentException( String.Format("datacenter Id can't be greater than {0} or less than 0", MaxDatacenterId));
            }
        }
	
        public long WorkerId {get; protected set;}
        public long DatacenterId {get; protected set;}

		/// <summary>
		/// Last generated sequence for the last timestamp.
		/// </summary>
		/// <remarks>On its own, this property does not convey any useful information.</remarks>
        public long Sequence
        {
            get { return _sequence; }
            internal set { _sequence = value; }
        }
	
		/// <summary>
		/// Generate next ID.
		/// </summary>
		/// <returns>Next ID.</returns>
        public virtual long NextId()
		{
			// declaration outside of synchronized code block, shorter lock time as result?
			long timestamp, id;

            lock(_lock) 
            {
				// get current timestamp				
				timestamp = System.CurrentTimeMillis(); // TimeGen();
				// reduced callstack by calling the method directly

                if (timestamp < _lastTimestamp) 
                {
                    //exceptionCounter.incr(1);
                    //log.Error("clock is moving backwards.  Rejecting requests until %d.", _lastTimestamp);
                    throw new InvalidSystemClock(String.Format(
                        "Clock moved backwards.  Refusing to generate id for {0} milliseconds", _lastTimestamp - timestamp));
                }
				
				if (_lastTimestamp == timestamp) // if still in the same milisecond as the last id generated
                {
					// strip away all '1's beyond the sequence bit length
					// this is a way of saying if it is larger than the sequence max-number, make it 0
                    _sequence = (_sequence + 1) & SequenceMask;

					if (_sequence == 0) //if sequence reached beyond maximum
                    {
						// wait for next milisecond
                        timestamp = TilNextMillis(_lastTimestamp);
                    }
                } 
				else // otherwise use 0 as the first sequence in the current (new) milisecond
				{
                    _sequence = 0;
                }

				// remember the (last) timestamp used to generate (this) id
                _lastTimestamp = timestamp;

                id = ((timestamp - Twepoch) << TimestampLeftShift) |
                         (DatacenterId << DatacenterIdShift) |
                         (WorkerId << WorkerIdShift) | _sequence;

			}

			return id;
        }

		/// <summary>
		/// Allows worker to wait until the next milisecond. This is normally because the max sequence number for the specified timestamp has been reached.
		/// </summary>
		/// <param name="lastTimestamp">Timestamp used to generate last ID.</param>
		/// <returns>New milisecond.</returns>
        protected virtual long TilNextMillis(long lastTimestamp)
        {
			long timestamp = System.CurrentTimeMillis(); // TimeGen();
			// reduced callstack by calling the method directly

            while (timestamp <= lastTimestamp) 
            {
				timestamp = System.CurrentTimeMillis(); // TimeGen();
            }

            return timestamp;
        }

		// Unnecessary (proxy) method.
		protected virtual long TimeGen()
		{
			return System.CurrentTimeMillis();
		}      
    }
}