using System;

namespace GPS
{

	public enum NMEAState 
	{
		NP_STATE_SOM =				0,		// Search for start of message
		NP_STATE_CMD,						// Get command
		NP_STATE_DATA,						// Get data
		NP_STATE_CHECKSUM_1,				// Get first checksum character
		NP_STATE_CHECKSUM_2,				// get second checksum character
	};

	public struct GPGGAData
	{
		public int Hour;
		public int Minute;
		public int Second;
		public double Latitude;			
		public Cardinal LatitudeHemisphere;
		public double Longitude;		
		public Cardinal LongitudeHemisphere;
		public GPSQuality GPSQuality;			// 0 = fix not available, 1 = GPS sps mode, 2 = Differential GPS, SPS mode, fix valid, 3 = GPS PPS mode, fix valid
		public int NumberOfSatellitesInUse;
		public double HDOP;
		public double Altitude;			// Altitude: mean-sea-level (geoid) meters
		public int Count;

	}

	public enum GPSQuality : uint
	{
		FixNotAvailable = 0,
		GPS_SPSMode = 1,
		Differential_GPS_SPSMode_FixValid = 2,
		GPS_PPSMode_FixValid = 3
	}

	public enum Cardinal : uint
	{
		North = 0,
		South = 1,
		East = 2,
		West = 3,
	}

	public struct GPGSAData
	{
		public char Mode;					// M = manual, A = automaeltic 2D/3D
		public byte mFixMode;				// 1 = fix not available, 2 = 2D, 3 = 3D
		public int[] SatsInSolution; // ID of sats in solution
		public double PDOP;					//
		public double HDOP;					//
		public double VDOP;					//
		public int Count;					//	
	}


	public class GPGSVData
	{
		public uint TotalNumberOfMessages;			//
		public int SatellitesInView;		//
		public Satellites Satellites = new Satellites();			//
		public int Count;					//
	}

	public struct GPRMBData
	{
		public byte DataStatus;				// A = data valid, V = navigation receiver warning
		public double CrosstrackError;		// nautical miles
		public byte DirectionToSteer;		// L/R
		public string OriginWaypoint; // Origin Waypoint ID
		public string DestWaypoint; // Destination waypoint ID
		public double DestLatitude;			// destination waypoint latitude
		public double DestLongitude;			// destination waypoint longitude
		public double RangeToDest;			// Range to destination nautical mi
		public double BearingToDest;			// Bearing to destination, degrees true
		public double DestClosingVelocity;	// Destination closing velocity, knots
		public byte ArrivalStatus;			// A = arrival circle entered, V = not entered
		public int Count;					//
	}


	public struct GPRMCData
	{
		public int Hour;					//
		public int Minute;					//
		public int Second;					//
		public char DataValid;				// A = Data valid, V = navigation rx warning
		public double Latitude;				// current latitude
        public Cardinal LatitudeHemisphere;
        public double Longitude;				// current longitude
        public Cardinal LongitudeHemisphere;
        public double GroundSpeed;			// speed over ground, knots
		public double Course;				// course over ground, degrees true
		public int Day;					//
		public int Month;					//
		public int Year;					//
		public double MagVar;				// magnitic variation, degrees East(+)/West(-)
		public int Count;					//	
	
	
	}


	public struct GPZDAData
	{
		public byte Hour;					//
		public byte Minute;					//
		public byte Second;					//
		public byte Day;					// 1 - 31
		public byte Month;					// 1 - 12
		public int Year;					//
		public byte LocalZoneHour;			// 0 to +/- 13
		public byte LocalZoneMinute;		// 0 - 59
		public int Count;					//		
	}
}
